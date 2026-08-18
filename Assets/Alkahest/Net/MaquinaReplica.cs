using UnityEngine;
using Alkahest.Game;
using Alkahest.Sim;

namespace Alkahest.Net
{
    /// <summary>
    /// LA RÉPLICA VISUAL de una máquina, del lado del invitado (ver el
    /// docblock de <see cref="MaquinaSync"/> para el protocolo completo).
    /// Vive SOLO en el espejo de un invitado -- el anfitrión nunca crea una
    /// de estas (tiene la máquina real).
    ///
    /// SOLO-VISUAL de verdad: sin `Update` de simulación, sin lectura de la
    /// grilla, sin `IMaquinaInteractiva` (no responde a E, no tiene HUD de
    /// proceso). Lo único "activo" que tiene son (a) un `Lerp` hacia la
    /// posición que publica <see cref="MaquinaSync"/> y (b) el contrato
    /// <see cref="IMovible"/>, que la hace agarrable por
    /// <see cref="Mudanza"/> EXACTAMENTE como si fuera la máquina real --
    /// Mudanza no sabe ni le importa la diferencia (ver el docblock de esa
    /// interfaz: "Mudanza trata cada aparato de forma OPACA"). Toda la
    /// diferencia entre mover una máquina real y mover una réplica vive
    /// AQUÍ, en <see cref="Reposicionar"/>: en vez de tocar nada, pide
    /// permiso por red.
    /// </summary>
    public sealed class MaquinaReplica : MonoBehaviour, IMovible
    {
        /// <summary>Qué tan rápido converge el Lerp hacia el objetivo (1/seg, suavizado exponencial -- ver Update). No es crítico: solo estética de "se desliza", cualquier valor entre 4 y 10 se lee bien.</summary>
        private const float LerpVelocidad = 6f;

        private byte _tipo;
        private byte _indice;

        private Vector2 _tamanoMundo;

        /// <summary>Última celda de anclaje CONFIRMADA por el anfitrión (nunca la candidata mientras se arrastra/espera respuesta).</summary>
        private Vector2Int _anclaConfirmada;

        /// <summary>Último centro de mundo CONFIRMADO -- adonde vuelve la réplica si el anfitrión rechaza una mudanza (ver AlRechazar).</summary>
        private Vector3 _centroConfirmado;

        /// <summary>Hacia dónde converge el Lerp AHORA MISMO -- puede ser el fantasma optimista de un arrastre en curso, ver Reposicionar.</summary>
        private Vector3 _centroObjetivo;

        /// <summary>Posición visual actual (lo que el jugador ve, y lo que expone CentroMundo -- ver el porqué en el docblock de esa propiedad).</summary>
        private Vector3 _centroActual;

        private SpriteRenderer _sr;
        private string _nombre;

        /// <summary>Llamado por MaquinaSync justo después de instanciar el GameObject -- nunca dos veces (una réplica vive tanto como su GameObject).</summary>
        public void Inicializar(MaquinaSync.EntradaMaquina e)
        {
            _tipo = e.tipo;
            _indice = e.indice;
            _tamanoMundo = new Vector2(e.tamanoX, e.tamanoY);
            _anclaConfirmada = new Vector2Int(e.anclaX, e.anclaY);
            _centroConfirmado = new Vector3(e.centroX, e.centroY, 0f);
            _centroObjetivo = _centroConfirmado;
            _centroActual = _centroConfirmado;
            transform.position = _centroActual;

            _sr = MaquinariaSprites.ConstruirVisualEstatico(transform, _tipo, _tamanoMundo);
            _nombre = NombreEstacion(_tipo);

            // MISMO registro que usan HeatPlate/ChillStone/Dispenser/las
            // cinco estaciones en su propio Init (ver Game/Mudanza.cs,
            // RegistrarMovible): en el proceso de un invitado ninguna máquina
            // real existe (AlkahestGameBootstrap.TrySpawnRed se detiene antes
            // de crearlas, ver ese archivo), así que esta lista SOLO contiene
            // réplicas -- cero conflicto con el anfitrión, que corre en un
            // proceso completamente aparte.
            Mudanza.RegistrarMovible(this);
        }

        private void OnDestroy()
        {
            Mudanza.OlvidarMovible(this);
        }

        private void Update()
        {
            // Suavizado exponencial (independiente de framerate): a
            // LerpVelocidad=6 converge al ~95% en medio segundo tanto a 30
            // como a 144 fps. "Lerp suave" del encargo, sin más ceremonia.
            float k = 1f - Mathf.Exp(-LerpVelocidad * Time.deltaTime);
            _centroActual = Vector3.Lerp(_centroActual, _centroObjetivo, k);
            transform.position = _centroActual;
        }

        /// <summary>¿Esta réplica es la de (tipo, indice)? La usa MaquinaSync para encontrar a quién avisar de un rechazo.</summary>
        public bool Coincide(byte tipo, byte indice) => _tipo == tipo && _indice == indice;

        /// <summary>
        /// Llega el registro con datos frescos de la máquina real (mudanza
        /// aceptada -- propia o de otro jugador -- o sondeo periódico del
        /// anfitrión). Sobrescribe SIEMPRE lo confirmado y apunta el Lerp
        /// ahí: si había un fantasma optimista en vuelo, converge solo con
        /// el valor de verdad en cuanto llega (normalmente ya estaban muy
        /// cerca, porque la aproximación de <see cref="Reposicionar"/> usa
        /// la misma celda que se pidió).
        /// </summary>
        public void ActualizarDesdeRegistro(MaquinaSync.EntradaMaquina e)
        {
            if (e.tipo != _tipo || e.indice != _indice)
            {
                Debug.LogWarning("[ChaosAlchemy][Red] MaquinaReplica: la entrada del registro cambió de identidad -- no debería pasar (el registro solo crece), se ignora.");
                return;
            }

            _anclaConfirmada = new Vector2Int(e.anclaX, e.anclaY);
            _centroConfirmado = new Vector3(e.centroX, e.centroY, 0f);
            _centroObjetivo = _centroConfirmado;

            var tamanoNuevo = new Vector2(e.tamanoX, e.tamanoY);
            if (tamanoNuevo != _tamanoMundo)
            {
                // Ninguna estación cambia de tamaño en este POC (Reposicionar
                // solo traslada) -- por si acaso, se re-escala el sprite ya
                // creado en vez de crear uno nuevo (cero instanciar/destruir
                // de más, mismo criterio que Game/Mudanza.cs).
                _tamanoMundo = tamanoNuevo;
                if (_sr != null && _sr.sprite != null)
                {
                    _sr.transform.localScale = new Vector3(
                        Mathf.Max(0.02f, _tamanoMundo.x) / _sr.sprite.rect.width,
                        Mathf.Max(0.02f, _tamanoMundo.y) / _sr.sprite.rect.height, 1f);
                }
            }
        }

        /// <summary>El anfitrión rechazó la última mudanza pedida (ver MaquinaSync.MudanzaRechazadaRpc): el fantasma optimista vuelve a lo último confirmado. Vía Lerp, no de golpe -- "colocado" y "rechazado" se leen distinto si uno es instantáneo y el otro se desliza.</summary>
        public void AlRechazar()
        {
            _centroObjetivo = _centroConfirmado;
        }

        // =================================================================
        // IMovible -- ver Game/Mudanza.cs para el contrato completo. Se
        // implementa la interfaz PLANA (no IMovibleAnclaEsquina): DECISIÓN,
        // ver el docblock de la clase, "LA SOMBRA GENÉRICA". Mudanza cae
        // sola al camino "silueta centrada en el cursor" para cualquier
        // IMovible que no declare la variante de esquina -- funcionalmente
        // idéntico, solo cambia que la sombra de arrastre no se pega
        // pixel-perfect a la huella futura.
        // =================================================================

        public Vector3 CentroMundo => _centroActual; // la posición VISUAL (lo que el jugador ve y a lo que apunta), no la última confirmada -- así un agarre inmediato tras una mudanza mide distancia contra donde la réplica está de verdad en pantalla.
        public Vector2 TamanoMundo => _tamanoMundo;
        public Vector2Int AnclaCelda => _anclaConfirmada;

        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            // APROXIMACIÓN del lado del invitado (DECISIÓN, ver docblock de
            // la clase): solo comprueba que el footprint (derivado de
            // TamanoMundo) no se salga del marco protegido del mundo, con el
            // mismo margen de 1 celda que usan las cinco estaciones reales
            // (ver Game/Crisol.cs, CabeEnAncla). No conoce reglas propias de
            // cada máquina real (el Dispenser, por ejemplo, también
            // comprueba su radio de emisión y su búsqueda de rebose) --
            // puramente informativo para colorear la silueta de Mudanza. La
            // autoridad de verdad es siempre MaquinaSync.SolicitarMudanzaRpc
            // sobre la máquina REAL, del lado del anfitrión: si esta
            // aproximación dijera "sí" y el anfitrión dijera "no",
            // MudanzaRechazadaRpc corrige la réplica de todos modos (ver
            // AlRechazar) -- el peor caso es un "colocado" que un instante
            // después se desliza de vuelta, nunca un estado inconsistente.
            float c = SimRenderer.CellWorldSize;
            int span = Mathf.Max(1, Mathf.RoundToInt(_tamanoMundo.x / c));
            int alto = Mathf.Max(1, Mathf.RoundToInt(_tamanoMundo.y / c));
            return anclaCelda.x >= 1 && anclaCelda.x + span - 1 <= CellGrid.W - 2
                && anclaCelda.y >= 1 && anclaCelda.y + alto - 1 <= CellGrid.H - 2;
        }

        public void Reposicionar(Vector2Int anclaCelda)
        {
            // EL FANTASMA LOCAL MIENTRAS CARGA (encargo, punto 3): la réplica
            // se mueve YA, de forma optimista -- tratando anclaCelda como la
            // esquina inferior izquierda del footprint (válido para las
            // cinco estaciones; aproximación razonable para el grifo, ver
            // DECISIÓN en el docblock de MaquinaSync.EntradaMaquina.centroX)
            // -- y en paralelo se pide permiso de verdad. En cuanto responda
            // el anfitrión (aceptando -> ActualizarDesdeRegistro, rechazando
            // -> AlRechazar) este valor optimista se sobreescribe con el de
            // verdad, así que un desajuste aquí dura, como mucho, un
            // round-trip de red.
            float c = SimRenderer.CellWorldSize;
            _centroObjetivo = new Vector3(
                anclaCelda.x * c + _tamanoMundo.x * 0.5f,
                anclaCelda.y * c + _tamanoMundo.y * 0.5f, 0f);

            MaquinaSync.PedirMudanza(_tipo, _indice, anclaCelda);
        }

        // =================================================================
        // LA CHAPA: UiStyles.PlacaMundo con las guardas estándar (mismas que
        // Game/Mudanza.OnGUI y el OnGUI de cualquier máquina real) -- sin
        // texto de estado ni prompt de interacción, solo el nombre: la
        // réplica no hace nada que explicar.
        // =================================================================
        private void OnGUI()
        {
            if (DayCycle.InputLocked) return;
            if (Alkahest.Dev.DevPalette.IsOpen) return;
            if (UiStyles.EscribiendoTexto) return;
            if (JournalHud.Abierto) return;

            UiStyles.Preparar();
            UiStyles.PlacaMundo(_centroActual, _nombre, UiStyles.TextoTenue, UiStyles.S(30f));
        }

        /// <summary>
        /// (fix Cesar playtest 34) ANTES, Balda/Anclaje (tipos 6/7, sumados
        /// en el playtest 33) no tenían caso propio aquí y caían al
        /// `default: "aparato"` genérico -- un invitado veía la chapa
        /// "aparato" sobre una balda o un anclaje, indistinguible de
        /// cualquier otra cosa sin nombre. Nombres reales para los cuatro
        /// tipos que faltaban (Balda/Anclaje/Rack/Alambique -- Cesar los pidió
        /// por nombre: "balda", "anclaje", "estante de redomas", "alambique").
        /// Rack/Alambique/Pila no tienen constante en MaquinariaSprites.cs
        /// (fuera de alcance de esta ronda, ver el docblock de
        /// <see cref="MaquinaSync.TipoMaquina"/>), así que se comparan
        /// directamente contra el enum de Net/ en vez de una constante de
        /// Game/ -- mismo valor numérico, dos rutas de acceso.
        /// </summary>
        private static string NombreEstacion(byte tipo)
        {
            switch (tipo)
            {
                case MaquinariaSprites.TipoCrisol: return "el crisol";
                case MaquinariaSprites.TipoPrensa: return "la prensa";
                case MaquinariaSprites.TipoBancoChispa: return "el banco de chispa";
                case MaquinariaSprites.TipoColumnaEnsayo: return "la columna de ensayo";
                case MaquinariaSprites.TipoEnsayoMaestro: return "el ensayo del maestro";
                case MaquinariaSprites.TipoDispenser: return "el grifo";
                case MaquinariaSprites.TipoBalda: return "la balda";
                case MaquinariaSprites.TipoAnclaje: return "el anclaje";
                case (byte)MaquinaSync.TipoMaquina.Rack: return "el estante de redomas";
                case (byte)MaquinaSync.TipoMaquina.Alambique: return "el alambique";
                case (byte)MaquinaSync.TipoMaquina.Pila: return "la pila";
                default: return "aparato";
            }
        }
    }
}
