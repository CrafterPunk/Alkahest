using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
    /// SIN simulación propia (no lee la grilla, no corre un Update de sim):
    /// eso NO cambia. Lo único "activo" que tenía hasta el playtest 43 eran
    /// (a) un `Lerp` hacia la posición que publica <see cref="MaquinaSync"/>
    /// y (b) el contrato <see cref="IMovible"/> (agarrable por
    /// <see cref="Mudanza"/> EXACTAMENTE como si fuera la máquina real --
    /// Mudanza no sabe ni le importa la diferencia, ver el docblock de esa
    /// interfaz: "Mudanza trata cada aparato de forma OPACA"). Toda la
    /// diferencia entre mover una máquina real y mover una réplica vive
    /// AQUÍ, en <see cref="Reposicionar"/>: en vez de tocar nada, pide
    /// permiso por red.
    ///
    /// (ENCARGO N, playtest 43, CONTRATO_PARIDAD.md §0.1/§2a) "SIN
    /// `IMaquinaInteractiva`, no responde a E" DEJÓ DE SER CIERTO -- era
    /// justo el diagnóstico del reporte de Cesar ("mi amigo no podía abrir
    /// los grifos, activar las máquinas..."). La réplica sigue sin
    /// implementar `IMaquinaInteractiva` (esa interfaz es del lado
    /// anfitrión, ver Game/MachineFocus.cs, y pide un `Update` de sim que
    /// esta clase nunca tendrá) pero ahora SÍ escucha E cerca de sí misma:
    /// arbitra la más cercana con un registro estático PROPIO
    /// (<see cref="_usables"/>, mismo espíritu que MachineFocus pero sin
    /// tocar ese archivo -- fuera del alcance de este encargo) y, si gana el
    /// arbitraje, llama a <see cref="MaquinaSync.PedirUso"/>. La proximidad
    /// la valida ESTA réplica (con la posición YA replicada del propio
    /// invitado); el servidor vuelve a validarla con un radio generoso
    /// anti-teleuso (ver MaquinaSync.SolicitarUsoServerRpc) -- dos capas,
    /// ninguna confía ciegamente en la otra.
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

        // =================================================================
        // (ENCARGO N, playtest 43) ESTADO VIVO + ANIMACIÓN
        // =================================================================

        /// <summary>Último byte de <see cref="EstadoVivoBits"/> conocido -- lo escribe <see cref="Inicializar"/> (sin disparar evento: es el punto de partida, no un cambio) y <see cref="ActualizarDesdeRegistro"/> (SÍ dispara <see cref="MaquinaSync.AlCambiarEstadoMaquina"/> si cambió).</summary>
        private byte _estadoVivo;

        /// <summary>
        /// Color de base capturado justo tras <see cref="MaquinariaSprites.ConstruirVisualEstatico"/>
        /// -- para unas estaciones es blanco (el sprite ya lleva sus colores
        /// horneados), para el fallback genérico (Rack/Alambique/Pila/tipo
        /// desconocido) es <c>ColorCarboncilloReplica</c>. La animación de
        /// estado SIEMPRE parte de este valor y nunca lo pisa (solo lo
        /// mezcla con Lerp cada frame), o una réplica que dejó de estar
        /// "trabajando" se quedaría teñida para siempre.
        /// </summary>
        private Color _colorBase = Color.white;

        /// <summary>
        /// Reutiliza el MISMO lenguaje visual que las máquinas reales para
        /// "estoy trabajando" (contrato §2b: "mismo lenguaje que las
        /// máquinas reales... reutilizar, no inventar") -- el latido/seno de
        /// <see cref="MaquinariaSprites.AffordanceGlow.AlfaTrabajo"/>, aquí
        /// conducido por el bit Trabajando en vez de por un `Fase` local.
        /// </summary>
        private readonly MaquinariaSprites.AffordanceGlow _pulsoTrabajo = new MaquinariaSprites.AffordanceGlow();

        private static readonly Color ColorFuegoReplica = new Color(1f, 0.55f, 0.18f);   // mismo tono que Crisol._brasasCesto ardiendo.
        private static readonly Color ColorLamparaReplica = new Color(0.68f, 0.86f, 1f); // mismo tono que BancoChispa._luzLampara (halo frío).
        private static readonly Color ColorResultadoReplica = Color.white;               // el destello "ven a recoger": blanco puro, como Destello.cs.
        private const float ResultadoListoPeriodoSeg = 1.8f;
        private const float ResultadoListoFraccionVisible = 0.32f; // el destello ocupa el primer tercio del ciclo; el resto, apagado -- "periódico", no un pulso continuo.

        // =================================================================
        // (ENCARGO N, playtest 43, CONTRATO_PARIDAD.md §2a) E REMOTO
        // =================================================================

        /// <summary>
        /// Registro estático PROPIO de réplicas USABLES (las 7 del contrato,
        /// ver <see cref="EsUsableRemota"/>) -- mismo espíritu que
        /// Game/MachineFocus.cs (solo la más cercana responde a E) pero sin
        /// tocar ese archivo (fuera del alcance de este encargo: MachineFocus
        /// es para las máquinas REALES del anfitrión, y una réplica nunca es
        /// una de ellas). Balda/Anclaje/Rack/Pila NUNCA entran aquí: ver
        /// <see cref="EsUsableRemota"/>.
        /// </summary>
        private static readonly List<MaquinaReplica> _usables = new List<MaquinaReplica>(8);

        /// <summary>Rango (celdas) dentro del cual una réplica usable ofrece "E — usar" y arbitra. Valor único intermedio a los `ProximityRange` reales de las 7 estaciones (2.6..4.0, ver sus Game/*.cs) -- la réplica no conoce el radio exacto de la máquina real (es un campo privado de cada clase), así que usa una aproximación razonable y DOCUMENTADA; la autoridad de verdad es el radio anti-teleuso del servidor (MaquinaSync.RadioUsoRemotoCeldas), mucho más generoso.</summary>
        private const float RangoUsoPleno = 3.4f;

        /// <summary>Arbitraje recalculado UNA vez por frame (no una vez por réplica): el primer Update de este frame lo dispara, los siguientes lo ven ya resuelto.</summary>
        private static int _frameArbitrado = -1;
        private static MaquinaReplica _ganadoraEsteFrame;

        private bool _esGanadoraEsteFrame; // cacheado en Update, leído en OnGUI (que corre después, mismo frame).

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
            _colorBase = _sr != null ? _sr.color : Color.white; // ver el docblock del campo.
            _nombre = NombreEstacion(_tipo);
            _estadoVivo = e.estadoVivo; // punto de partida -- sin disparar AlCambiarEstadoMaquina (no es un CAMBIO, es la foto inicial).

            // MISMO registro que usan HeatPlate/ChillStone/Dispenser/las
            // cinco estaciones en su propio Init (ver Game/Mudanza.cs,
            // RegistrarMovible): en el proceso de un invitado ninguna máquina
            // real existe (AlkahestGameBootstrap.TrySpawnRed se detiene antes
            // de crearlas, ver ese archivo), así que esta lista SOLO contiene
            // réplicas -- cero conflicto con el anfitrión, que corre en un
            // proceso completamente aparte.
            Mudanza.RegistrarMovible(this);

            if (EsUsableRemota()) _usables.Add(this);
        }

        private void OnDestroy()
        {
            Mudanza.OlvidarMovible(this);
            _usables.Remove(this);
            if (_ganadoraEsteFrame == this) _ganadoraEsteFrame = null;
        }

        private void Update()
        {
            // Suavizado exponencial (independiente de framerate): a
            // LerpVelocidad=6 converge al ~95% en medio segundo tanto a 30
            // como a 144 fps. "Lerp suave" del encargo, sin más ceremonia.
            float k = 1f - Mathf.Exp(-LerpVelocidad * Time.deltaTime);
            _centroActual = Vector3.Lerp(_centroActual, _centroObjetivo, k);
            transform.position = _centroActual;

            ActualizarAnimacionEstado();

            if (EsUsableRemota())
            {
                ArbitrarSiHaceFalta();
                _esGanadoraEsteFrame = ReferenceEquals(_ganadoraEsteFrame, this);

                if (_esGanadoraEsteFrame && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                    && !DayCycle.InputLocked && !UiStyles.EscribiendoTexto && !JournalHud.Abierto)
                {
                    MaquinaSync.PedirUso(_tipo, _indice); // el servidor ejecuta IMaquinaUsableRemota.UsarPorRed() de verdad -- ver Net/MaquinaSync.cs.
                }
            }
            else _esGanadoraEsteFrame = false;
        }

        /// <summary>¿Este tipo de estación implementa <see cref="Alkahest.Net.IMaquinaUsableRemota"/> del lado del anfitrión? Balda(6)/Anclaje(7)/Rack(8)/Pila(10) son mobiliario -- nunca responden a E, ni local ni remoto. Mismos valores que <see cref="MaquinariaSprites"/>/<see cref="MaquinaSync.TipoMaquina"/> (ver el docblock de ese enum para el porqué de la duplicación de constantes).</summary>
        private bool EsUsableRemota()
        {
            switch (_tipo)
            {
                case MaquinariaSprites.TipoCrisol:
                case MaquinariaSprites.TipoPrensa:
                case MaquinariaSprites.TipoBancoChispa:
                case MaquinariaSprites.TipoColumnaEnsayo:
                case MaquinariaSprites.TipoEnsayoMaestro:
                case MaquinariaSprites.TipoDispenser:
                case (byte)MaquinaSync.TipoMaquina.Alambique:
                // (CONTRATO_TERMICA.md §1, ENCARGO I) Las dos placas SÍ
                // implementan IMaquinaUsableRemota del lado de T (§1 del
                // contrato: "T implementa IMaquinaUsableRemota... en ambas
                // placas") -- E remoto funciona igual que en las cinco
                // estaciones originales.
                case MaquinariaSprites.TipoPlacaCalor:
                case MaquinariaSprites.TipoPlacaFria:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Recalcula, UNA vez por frame, cuál de las réplicas usables (si
        /// alguna) es la MÁS CERCANA al avatar local dentro de
        /// <see cref="RangoUsoPleno"/> -- ver el docblock de <see cref="_usables"/>.
        /// Idempotente dentro del mismo frame (guardado por <see cref="_frameArbitrado"/>):
        /// con 7 réplicas como mucho, recorrer la lista una vez por Update de
        /// CADA una sería O(n²) -- trivial a esta escala, pero cero motivo
        /// para no evitarlo.
        /// </summary>
        private static void ArbitrarSiHaceFalta()
        {
            int frame = Time.frameCount;
            if (frame == _frameArbitrado) return;
            _frameArbitrado = frame;
            _ganadoraEsteFrame = null;

            var jugador = ApprenticeController.AprendizLocal;
            if (jugador == null) return;

            float celda = SimRenderer.CellWorldSize;
            float mejorDistCeldas = float.MaxValue;
            for (int i = 0; i < _usables.Count; i++)
            {
                var r = _usables[i];
                if (r == null) continue;
                float distCeldas = Vector3.Distance(r._centroActual, jugador.transform.position) / celda;
                if (distCeldas > RangoUsoPleno) continue;
                if (distCeldas < mejorDistCeldas) { mejorDistCeldas = distCeldas; _ganadoraEsteFrame = r; }
            }
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
                Debug.LogWarning("[TenThousandYears][Red] MaquinaReplica: la entrada del registro cambió de identidad -- no debería pasar (el registro solo crece), se ignora.");
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

            // (ENCARGO N, playtest 43, CONTRATO_PARIDAD.md §2b) "Y dispara
            // AlCambiarEstadoMaquina en ambos lados": este es el lado
            // invitado -- MaquinaSync.SondearEstadoVivo ya lo dispara en el
            // anfitrión al escribir el NetworkList. Solo si CAMBIÓ de verdad
            // (el registro también llega aquí por cambios de posición/
            // mudanza que no tocan estadoVivo, ver ConstruirEntrada en
            // Net/MaquinaSync.cs -- comparar evita un evento fantasma en
            // cada mudanza de un vecino).
            if (e.estadoVivo != _estadoVivo)
            {
                byte antes = _estadoVivo;
                _estadoVivo = e.estadoVivo;
                MaquinaSync.NotificarCambioEstado(_tipo, _indice, antes, _estadoVivo);
            }
        }

        /// <summary>
        /// LA ANIMACIÓN POR BITS (contrato §2b): reutiliza el lenguaje
        /// visual existente de MaquinariaSprites -- SpriteRenderer.color con
        /// seno, cero allocs (Color es struct, Color.Lerp no reserva). Como
        /// esta réplica es UNA sola pieza (ver ConstruirVisualEstatico), las
        /// cuatro señales se COMPONEN sobre <see cref="_colorBase"/> en vez
        /// de vivir en capas separadas como en la máquina real (que sí tiene
        /// sprites de brasero/cubeta/lámpara aparte) -- DECISIÓN explícita,
        /// documentada en el informe de la ronda. Orden de mezcla, del más
        /// sutil al más urgente (cada capa se aplica ENCIMA de la anterior):
        /// Trabajando (brillo) &lt; FuegoEncendido (tinte cálido) &lt;
        /// LuzPlena (tinte frío) &lt; ResultadoListo (destello blanco, "ven a
        /// recoger" es la información más urgente de las cuatro).
        /// </summary>
        private void ActualizarAnimacionEstado()
        {
            if (_sr == null) return;

            Color col = _colorBase;

            _pulsoTrabajo.Trabajando = (_estadoVivo & EstadoVivoBits.Trabajando) != 0;
            if (_pulsoTrabajo.Trabajando)
                col = Color.Lerp(col, Color.white, _pulsoTrabajo.AlfaTrabajo * 0.35f); // mismo latido que MaquinariaSprites.AffordanceGlow.AlfaTrabajo ya usa en las máquinas reales.

            if ((_estadoVivo & EstadoVivoBits.FuegoEncendido) != 0)
            {
                float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f); // mismo Hz que Crisol._brasasCesto ardiendo.
                col = Color.Lerp(col, ColorFuegoReplica, 0.30f + 0.25f * pulso);
            }

            if ((_estadoVivo & EstadoVivoBits.LuzPlena) != 0)
            {
                float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f); // más lento que el fuego: es un instrumento, no una llama (mismo criterio que BancoChispa).
                col = Color.Lerp(col, ColorLamparaReplica, 0.22f + 0.18f * pulso);
            }

            if ((_estadoVivo & EstadoVivoBits.ResultadoListo) != 0)
            {
                // PERIÓDICO, no un pulso continuo (contrato: "destello suave
                // periódico"): sube y baja dentro del primer tercio de cada
                // ciclo de 1.8s, el resto del ciclo queda en _colorBase puro
                // -- un latido de atención cada rato, no un tinte permanente.
                float fase = Mathf.Repeat(Time.time, ResultadoListoPeriodoSeg) / ResultadoListoPeriodoSeg;
                float destello = fase < ResultadoListoFraccionVisible
                    ? Mathf.Sin((fase / ResultadoListoFraccionVisible) * Mathf.PI) // sube y baja suave dentro de su ventana.
                    : 0f;
                col = Color.Lerp(col, ColorResultadoReplica, destello * 0.55f);
            }

            // Sirviendo: sin capa propia a propósito (contrato §2b: "nada
            // visual extra -- el chorro real ya se replica por chunks" -- el
            // agua/limo cayendo de la boquilla YA es la señal).

            _sr.color = col;
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
        //
        // (fix Cesar playtest 36, EL CAMINO DEL INVITADO) ANTES se dibujaba
        // SIEMPRE, a cualquier distancia -- con 17 baldas + 6 anclajes del
        // depósito (playtest 34/35) eso empapelaba la pantalla del invitado
        // ("la balda" x17, "e e e e" de los anclajes apretados 3 celdas entre
        // sí). DOS correcciones, mismo patrón que ya usan las máquinas reales
        // (ver ColumnaEnsayo.OnGUI, `UiStyles.Cercania` + `if (cercania <= 0f)
        // return;`, en vez del criterio "siempre 0.45..1" de Dispenser --
        // aquí SÍ interesa que desaparezca del todo: 23 muebles a la vez no
        // pueden quedarse todos discretamente visibles):
        //   (a) BALDA/ANCLAJE NUNCA TIENEN CHAPA. Son mobiliario, no
        //       estaciones -- la lectura la da su FORMA (piedra tallada /
        //       cuadrito de latón, ver Net/MaquinaSync.cs y
        //       Game/MaquinariaSprites.cs), no un rótulo. Es la causa raíz
        //       real de "e e e e": no era un bug de medir el ancho del
        //       texto, era que NINGÚN anclaje debía llevar chapa -- seis
        //       "el anclaje" apretados en el depósito (2 celdas de paso, ver
        //       Game/Anclaje.cs::DepositoPaso) se leen como letras sueltas
        //       aunque el texto se mida bien.
        //   (b) EL RESTO (estaciones + Rack/Alambique/Pila) SOLO por
        //       cercanía: 0 más allá de <see cref="RangoChapaDesvanece"/>,
        //       pleno dentro de <see cref="RangoChapaPleno"/>. Son pocos (7
        //       estaciones + 4 muebles nuevos), así que no hace falta el
        //       suelo de 0.45 que usan los grifos reales (esos SON estaciones
        //       con estado urgente que avisar; una réplica solo dice su
        //       nombre).
        // =================================================================
        private const float RangoChapaPleno = 3.2f;
        private const float RangoChapaDesvanece = 5.5f;

        private void OnGUI()
        {
            if (DayCycle.InputLocked) return;
            if (Alkahest.Dev.DevPalette.IsOpen) return;
            if (UiStyles.EscribiendoTexto) return;
            if (JournalHud.Abierto) return;
            if (_tipo == MaquinariaSprites.TipoBalda || _tipo == MaquinariaSprites.TipoAnclaje) return; // mobiliario: sin chapa, ver doc de arriba.

            var jugador = ApprenticeController.AprendizLocal;
            float cercania = UiStyles.Cercania(_centroActual, jugador != null ? jugador.transform : null, RangoChapaPleno, RangoChapaDesvanece);
            if (cercania <= 0f) return;

            UiStyles.Preparar();
            Color tenue = UiStyles.TextoTenue;
            // (integración pt55, B3 de la captura de Cesar: "el grifo" flotaba
            // lejos de su grifo) La red solo transmite CentroMundo, que en el
            // Dispenser real es el ANCLA DE AGARRE (transform + 2.5 celdas,
            // ver Dispenser.CentroMundo), mientras que su rótulo propio vive
            // en transform + 6.5 celdas (Dispenser._anclaRotulo): la réplica
            // pintaba la chapa 4 celdas lejos de donde la pinta el host. Se
            // compensa aquí con el MISMO delta, solo para grifos.
            Vector3 anclaChapa = _centroActual;
            if (_tipo == MaquinariaSprites.TipoDispenser)
                anclaChapa.x += 4f * SimRenderer.CellWorldSize; // el delta es EN X (ambos anclas del Dispenser real son horizontales, ver sus líneas 244/363).
            UiStyles.PlacaMundo(anclaChapa, _nombre, new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercania), UiStyles.S(30f));

            // (ENCARGO N, playtest 43, CONTRATO_PARIDAD.md §2b) SEGUNDA LÍNEA
            // DE ESTADO: textos FIJOS del cliente (nunca strings replicados
            // por red, el contrato lo pide explícitamente) leídos de los
            // mismos bits que anima ActualizarAnimacionEstado. Va ENCIMA del
            // nombre (offset mayor): es la información que cambia, el nombre
            // es el ancla fija.
            string textoEstado = TextoEstado(_estadoVivo);
            if (textoEstado != null)
            {
                UiStyles.PlacaMundo(_centroActual, textoEstado, new Color(UiStyles.Aviso.r, UiStyles.Aviso.g, UiStyles.Aviso.b, cercania), UiStyles.S(44f));
            }

            // (ENCARGO N, CONTRATO_PARIDAD.md §2a) "E — usar" SOLO si esta
            // réplica ganó el arbitraje de cercanía este frame (ver Update/
            // ArbitrarSiHaceFalta) -- las no-usables (EsUsableRemota==false)
            // nunca llegan aquí con _esGanadoraEsteFrame en true, porque
            // Update() nunca las mete en el arbitraje.
            if (_esGanadoraEsteFrame && !UiStyles.RatonOcupado)
            {
                UiStyles.PlacaMundo(_centroActual, "E — usar", UiStyles.Oro, UiStyles.S(16f));
            }
        }

        /// <summary>
        /// Los TEXTOS FIJOS de la segunda línea de la chapa (contrato §2b).
        /// Prioridad cuando hay varios bits a la vez: ResultadoListo (lo más
        /// urgente: "ven a buscarlo") &gt; Sirviendo &gt; Trabajando.
        /// FuegoEncendido/LuzPlena no tienen línea propia -- se leen en la
        /// ANIMACIÓN (tinte), no hace falta duplicarlos en texto (y
        /// "trabajando..." ya cubre el caso típico en que arden a la vez,
        /// ver Crisol: Trabajando y FuegoEncendido casi siempre coinciden).
        /// </summary>
        private static string TextoEstado(byte estado)
        {
            if (estado == 0) return null;
            if ((estado & EstadoVivoBits.ResultadoListo) != 0) return "¡listo — recoge!";
            if ((estado & EstadoVivoBits.Sirviendo) != 0) return "sirviendo";
            if ((estado & EstadoVivoBits.Trabajando) != 0) return "trabajando...";
            return null;
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
                // (CONTRATO_TERMICA.md §3b, ENCARGO I)
                case MaquinariaSprites.TipoPlacaCalor: return "la placa de calor";
                case MaquinariaSprites.TipoPlacaFria: return "la placa fría";
                default: return "aparato";
            }
        }
    }
}
