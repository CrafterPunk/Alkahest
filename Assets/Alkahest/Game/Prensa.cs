using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// LA PRENSA — reconstruida en el PLAYTEST 27
    /// (docs/CONTRATO_TALLER_GRANDE.md, mandatos 1-3).
    ///
    /// =====================================================================
    /// EL VEREDICTO DE CESAR SOBRE LA DEL PLAYTEST 26
    /// =====================================================================
    /// *"Se ve horrible; el embudo diminuto y horrible FLOTANDO, que además
    /// parece la boquilla de entrada en referencia a la primera máquina, pero
    /// no... cuando descifras que tienes que tirar el material dentro de su
    /// cajita, sin recibir ningún feedback, su capacidad es tan pequeña que es
    /// imposible que no desborde la cantidad más pequeña que puedo tener."*
    ///
    /// Tres acusaciones, tres respuestas, y las tres son estructurales:
    ///
    ///  1. **EL EMBUDO FLOTANTE SE VA.** La Prensa no recibe por vertido: se
    ///     DEPOSITA en su lecho. El playtest 26 le puso un embudo igualmente
    ///     "porque toda máquina que recibe materia lleva embudo", y con eso
    ///     mató la gramática que quería fundar -- el embudo pasó a significar
    ///     "esto es una máquina" en vez de "por aquí se vierte". Ahora la
    ///     Prensa lleva **BANDEJA ABIERTA ENMARCADA**
    ///     (<see cref="MaquinariaSprites.MarcoBandeja"/>): un labio de latón
    ///     volado alrededor de un hueco que se ve entero. Se deposita porque
    ///     está abierta, no porque un cartel lo diga.
    ///  2. **CAPACIDAD.** El lecho pasa de 5x3 = 15 celdas a
    ///     <see cref="LechoAncho"/>x<see cref="LechoAlto"/> = 15x5 = **75
    ///     celdas**. La cantidad más pequeña que el jugador puede tener es una
    ///     ración de caño (45): ahora cabe, y sobra sitio para ver el
    ///     resultado.
    ///  3. **FEEDBACK AL RECIBIR** (mandato 3): el marco DESTELLA
    ///     (<see cref="MaquinariaSprites.Destello"/>) en cuanto entra materia
    ///     en el lecho. Y mientras la mandíbula baja, el aparato entero LATE
    ///     (<see cref="MaquinariaSprites.AffordanceGlow.AlfaTrabajo"/>, el
    ///     pulso reconvertido a "estoy trabajando") y **EL VOLANTE GIRA DE
    ///     VERDAD** -- contrato §5, "una prensa con un tornillo que gira de
    ///     verdad al prensar". Nadie ha dudado nunca de qué hace un tornillo
    ///     de banco que está girando.
    ///
    /// =====================================================================
    /// SILUETA: UN PÓRTICO, NO UNA CAJA
    /// =====================================================================
    /// Dos JAMBAS de piedra de 5 celdas de ancho y 20 de alto, un DINTEL que
    /// las une, y entre ellas la mandíbula colgando del husillo sobre el
    /// lecho. 31x23 celdas de huella frente a las 7x4 del 26. Desde el otro
    /// lado del taller se lee "algo pesado que cae", que es literalmente lo
    /// que hace.
    ///
    ///      ┌───────────── dintel ─────────────┐   y 190..192
    ///      │ ▓             ╤ volante        ▓ │
    ///      │ ▓             │ husillo        ▓ │
    ///      │ ▓          ███████ mandíbula   ▓ │   reposo a 6 del labio
    ///      │ ▓        ╔═══════════╗         ▓ │
    ///      └─▓────────╚ lecho 15x5╝─────────▓─┘   y 170..175
    ///       jamba                        jamba
    ///
    /// La MECÁNICA no cambia (contrato: solo forma, capacidad y feedback):
    /// E baja la mandíbula en 0.5s y aplica <see cref="Universe.Prensa"/>
    /// celda a celda -- Compactar/Reventar/Escupir/Resistir/Nada.
    /// </summary>
    public sealed class Prensa : MonoBehaviour, IMaquinaInteractiva, IMovibleAnclaEsquina
    {
        private enum State { Arriba, Bajando, Cooldown }

        /// <summary>3.2 -&gt; 3.6: el pórtico mide 29 celdas de ancho y hay que poder atenderlo desde cualquiera de sus dos jambas.</summary>
        private const float ProximityRange = 3.6f;

        // -----------------------------------------------------------------
        // GEOMETRÍA (playtest 27). Públicas: las lee Sim/SimLevelBuilder.cs.
        // -----------------------------------------------------------------
        /// <summary>Ancho del hueco del lecho. 5 -&gt; 15.</summary>
        public const int LechoAncho = 15;
        /// <summary>Alto del hueco del lecho. 3 -&gt; 5. 15x5 = 75 celdas (la ración de un caño son 45; el 26 tenía 15 y desbordaba SIEMPRE).</summary>
        public const int LechoAlto = 5;
        /// <summary>Grosor del muro del lecho. 1 -&gt; 2.</summary>
        public const int MuroGrosor = 2;
        /// <summary>Ancho de cada jamba de piedra del pórtico.</summary>
        public const int JambaAncho = 5;
        /// <summary>
        /// Altura del pórtico sobre el suelo, sin contar el dintel. 27 -&gt;
        /// **20** (segunda pasada, visto jugando): con 27 la mandíbula
        /// descansaba 19 celdas por encima del labio del lecho y se leía como
        /// "un martillo aparcado en el techo", no como una mandíbula sobre una
        /// bandeja. La relación que importa en una prensa es la DISTANCIA
        /// ENTRE LAS DOS PIEZAS QUE SE JUNTAN; el resto es decoración. Con 20,
        /// la mandíbula reposa a 6 celdas del labio (ver
        /// <see cref="RecalcularRecorridoMandibula"/>) y el husillo llena el
        /// hueco.
        /// </summary>
        public const int JambaAlto = 20;
        /// <summary>Filas del dintel que corona el pórtico.</summary>
        public const int DintelFilas = 3;
        /// <summary>Celdas de aire entre el muro del lecho y la cara interior de cada jamba.</summary>
        public const int LechoAJamba = 1;

        private const float PressDuration = 0.5f;
        private const float CooldownDuration = 2f;

        private AlkahestSim _sim;
        private Transform _player;

        private int _anchorX;
        private int _baseY;
        private int _lechoX0, _lechoX1, _lechoY0, _lechoY1;
        private int _outX0, _outX1, _outY0, _outY1;
        private int _jambaTopY, _dintelY0;

        /// <summary>(playtest 29) Handle en <see cref="SimLevelBuilder.ObraDelTaller"/> -- ver el docblock gemelo en Game/Crisol.cs (`_handleObra`).</summary>
        private int _handleObra = -1;

        private Vector3 _centro, _centroLecho, _centroRotulo;

        private State _state = State.Arriba;
        private float _stateTimer;

        private Transform _mandibulaTr;
        private Transform _volanteTr;
        private Vector3 _mandibulaArribaPos, _mandibulaAbajoPos;
        private float _giroVolante;

        private SpriteRenderer _resalte, _latidoTrabajo, _destelloMarco;
        private float _alfaResalte;
        private int _celdasLechoPrev;

        private readonly MaquinariaSprites.Destello _acuse = new MaquinariaSprites.Destello();
        private readonly MaquinariaSprites.AffordanceGlow _pulsoTrabajo = new MaquinariaSprites.AffordanceGlow();

        private const float RangoEstadoPleno = 6.0f;
        private const float RangoEstadoDesvanece = 7.5f;
        private const float RangoNombrePleno = 3.2f;
        private const float RangoNombreDesvanece = 4.4f;
        private bool _yaConocida;
        private string _chapaEstado;

        public Vector3 PuntoFoco => _centroLecho;
        public float RangoFoco => ProximityRange;

        public Vector3 CentroMundo => _centro;
        public Vector2 TamanoMundo => new Vector2(
            (_outX1 - _outX0 + 1) * SimRenderer.CellWorldSize,
            (_outY1 - _outY0 + 1) * SimRenderer.CellWorldSize);
        public Vector2Int AnclaCelda => new Vector2Int(_outX0, _baseY);

        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            int span = _outX1 - _outX0 + 1;
            int alto = _outY1 - _outY0 + 1;
            return anclaCelda.x >= 1 && anclaCelda.x + span - 1 <= CellGrid.W - 2
                && anclaCelda.y >= 1 && anclaCelda.y + alto - 1 <= CellGrid.H - 2;
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap. FIRMA SIN CAMBIOS. `anchorX` = SimLevelBuilder.PrensaX.</summary>
        public void Init(AlkahestSim sim, Transform player, int anchorX)
        {
            _sim = sim;
            _player = player;
            _anchorX = anchorX;
            _baseY = SimLevelBuilder.CuartoY0 + 2;

            RecalcularRegion();
            BuildVisual();
            PosicionarMandibula(1f);

            MachineFocus.Registrar(this);
            // (playtest 29) El registro anticincel lo hace la INSTANCIA, no
            // TallarEnPlano -- ver Sim/SimLevelBuilder.cs, bloque "OBRA MOVIBLE".
            _handleObra = SimLevelBuilder.RegistrarObra(_outX0, _outY0, _outX1, _outY1);
            Mudanza.RegistrarMovible(this);
        }

        // ---- Huella compartida por la instancia y el tallado del plano ----
        private struct Huella
        {
            public int LechoX0, LechoX1, LechoY0, LechoY1;
            public int OutX0, OutX1, OutY0, OutY1;
            public int JambaTopY, DintelY0;
        }

        private static Huella Calcular(int anchorX, int baseY)
        {
            Huella h;
            h.LechoX0 = anchorX - LechoAncho / 2;
            h.LechoX1 = h.LechoX0 + LechoAncho - 1;
            h.LechoY0 = baseY + 1;
            h.LechoY1 = h.LechoY0 + LechoAlto - 1;

            h.OutX0 = h.LechoX0 - MuroGrosor - LechoAJamba - JambaAncho;
            h.OutX1 = h.LechoX1 + MuroGrosor + LechoAJamba + JambaAncho;
            h.JambaTopY = baseY + JambaAlto - 1;
            h.DintelY0 = h.JambaTopY + 1;
            h.OutY0 = baseY;
            h.OutY1 = h.DintelY0 + DintelFilas - 1;
            return h;
        }

        private void RecalcularRegion()
        {
            var h = Calcular(_anchorX, _baseY);
            _lechoX0 = h.LechoX0; _lechoX1 = h.LechoX1; _lechoY0 = h.LechoY0; _lechoY1 = h.LechoY1;
            _outX0 = h.OutX0; _outX1 = h.OutX1; _outY0 = h.OutY0; _outY1 = h.OutY1;
            _jambaTopY = h.JambaTopY; _dintelY0 = h.DintelY0;

            float c = SimRenderer.CellWorldSize;
            _centro = new Vector3((_outX0 + (_outX1 - _outX0 + 1) * 0.5f) * c, (_outY0 + (_outY1 - _outY0 + 1) * 0.5f) * c, 0f);
            transform.position = _centro;
            _centroLecho = new Vector3((_lechoX0 + LechoAncho * 0.5f) * c, (_lechoY0 + LechoAlto * 0.5f) * c, 0f);
            _centroRotulo = new Vector3(_centroLecho.x, (_lechoY1 + 3f) * c, 0f);
        }

        /// <summary>Talla el pórtico entero (lecho + jambas + dintel) sobre el CellGrid del plano.</summary>
        public static void TallarEnPlano(CellGrid grid, int anchorX, int baseY)
        {
            var h = Calcular(anchorX, baseY);

            // Lecho: suelo + muros + interior vaciado.
            for (int x = h.LechoX0 - MuroGrosor; x <= h.LechoX1 + MuroGrosor; x++)
                if (CellGrid.InBounds(x, h.LechoY0 - 1)) grid.SetCell(x, h.LechoY0 - 1, MaterialId.Stone);
            for (int y = h.LechoY0 - 1; y <= h.LechoY1; y++)
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    if (CellGrid.InBounds(h.LechoX0 - t, y)) grid.SetCell(h.LechoX0 - t, y, MaterialId.Stone);
                    if (CellGrid.InBounds(h.LechoX1 + t, y)) grid.SetCell(h.LechoX1 + t, y, MaterialId.Stone);
                }
            for (int y = h.LechoY0; y <= h.LechoY1; y++)
                for (int x = h.LechoX0; x <= h.LechoX1; x++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Empty);

            // Las dos jambas del pórtico, macizas del suelo al dintel.
            for (int y = baseY; y <= h.JambaTopY; y++)
                for (int k = 0; k < JambaAncho; k++)
                {
                    if (CellGrid.InBounds(h.OutX0 + k, y)) grid.SetCell(h.OutX0 + k, y, MaterialId.Stone);
                    if (CellGrid.InBounds(h.OutX1 - k, y)) grid.SetCell(h.OutX1 - k, y, MaterialId.Stone);
                }

            // El dintel que las une por arriba: es lo que convierte dos
            // pilares sueltos en UNA máquina con boca.
            for (int y = h.DintelY0; y < h.DintelY0 + DintelFilas; y++)
                for (int x = h.OutX0; x <= h.OutX1; x++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Stone);

            // (playtest 29) El registro anticincel YA NO SE HACE AQUÍ -- lo
            // hace la INSTANCIA en Init (ver `_handleObra`), porque este
            // método es estático y corre ANTES de que exista ninguna
            // instancia que pueda guardarse el handle. Mismo rect exacto.
        }

        /// <summary>Misma geometría EN CALIENTE (regla 29: PaintStable). Solo la usa <see cref="Reposicionar"/> (Mudanza).</summary>
        private void TallarEnCaliente()
        {
            for (int x = _lechoX0 - MuroGrosor; x <= _lechoX1 + MuroGrosor; x++) _sim.PaintStable(x, _lechoY0 - 1, 0, MaterialId.Stone);
            for (int y = _lechoY0 - 1; y <= _lechoY1; y++)
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.PaintStable(_lechoX0 - t, y, 0, MaterialId.Stone);
                    _sim.PaintStable(_lechoX1 + t, y, 0, MaterialId.Stone);
                }
            _sim.PaintRect(_lechoX0, _lechoY0, LechoAncho, LechoAlto, MaterialId.Empty);

            for (int y = _baseY; y <= _jambaTopY; y++)
                for (int k = 0; k < JambaAncho; k++)
                {
                    _sim.PaintStable(_outX0 + k, y, 0, MaterialId.Stone);
                    _sim.PaintStable(_outX1 - k, y, 0, MaterialId.Stone);
                }
            for (int y = _dintelY0; y < _dintelY0 + DintelFilas; y++)
                for (int x = _outX0; x <= _outX1; x++)
                    _sim.PaintStable(x, y, 0, MaterialId.Stone);
        }

        /// <summary>(playtest 29, encargo B) Borra la mampostería VIEJA de la huella `h` -- ver el docblock gemelo en Game/Crisol.cs (`BorrarEnCaliente`) para el porqué exacto de cada exclusión.</summary>
        private void BorrarEnCaliente(Huella h)
        {
            // Muros del lecho -- EXCLUYENDO `h.LechoY0-1` (la losa
            // compartida del cuarto) y sin vaciar el interior (puede tener
            // materia: "el contenido... queda donde está", encargo B).
            for (int y = h.LechoY0; y <= h.LechoY1; y++)
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.Paint(h.LechoX0 - t, y, 0, MaterialId.Empty);
                    _sim.Paint(h.LechoX1 + t, y, 0, MaterialId.Empty);
                }

            // Las dos jambas, EXCLUYENDO la fila `h.OutY0` (=baseY, la losa
            // compartida -- jamás piedra del mundo).
            for (int y = h.OutY0 + 1; y <= h.JambaTopY; y++)
                for (int k = 0; k < JambaAncho; k++)
                {
                    _sim.Paint(h.OutX0 + k, y, 0, MaterialId.Empty);
                    _sim.Paint(h.OutX1 - k, y, 0, MaterialId.Empty);
                }

            // El dintel: genuinamente propio (muy por encima del piso), se borra entero.
            for (int y = h.DintelY0; y < h.DintelY0 + DintelFilas; y++)
                for (int x = h.OutX0; x <= h.OutX1; x++)
                    _sim.Paint(x, y, 0, MaterialId.Empty);
        }

        private void OnDestroy()
        {
            MachineFocus.Olvidar(this);
            Mudanza.OlvidarMovible(this);
        }

        public void Reposicionar(Vector2Int anclaCelda)
        {
            // 1) BORRAR la mampostería vieja, con la huella de ANTES de tocar el ancla.
            BorrarEnCaliente(Calcular(_anchorX, _baseY));

            _anchorX += anclaCelda.x - _outX0;
            _baseY = anclaCelda.y;
            RecalcularRegion();
            TallarEnCaliente(); // 2) TALLAR la nueva.
            RecalcularRecorridoMandibula();
            PosicionarMandibula(EstadoFraccion());

            // 3) ACTUALIZAR el registro anticincel.
            SimLevelBuilder.ActualizarObra(_handleObra, _outX0, _outY0, _outX1, _outY1);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return;

            SondearLecho();

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && EstaEnfocada()
                && _state == State.Arriba)
            {
                _state = State.Bajando;
                _stateTimer = 0f;
                MachineFocus.RegistrarUsoE();
            }

            _stateTimer += Time.deltaTime;
            switch (_state)
            {
                case State.Bajando:
                    if (_stateTimer >= PressDuration)
                    {
                        AplicarPrensada();
                        _state = State.Cooldown;
                        _stateTimer = 0f;
                    }
                    break;
                case State.Cooldown:
                    if (_stateTimer >= CooldownDuration) _state = State.Arriba;
                    break;
            }

            PosicionarMandibula(EstadoFraccion());
            _acuse.Avanzar(Time.deltaTime);
            _pulsoTrabajo.Trabajando = _state != State.Arriba;
            ActualizarVisual();
        }

        /// <summary>Cuenta el lecho y dispara el ACUSE DE RECIBO (mandato 3) cuando la ocupación SUBE: eso solo puede venir de que algo acaba de entrar.</summary>
        private void SondearLecho()
        {
            var grid = _sim.Grid;
            int n = 0;
            for (int y = _lechoY0; y <= _lechoY1; y++)
                for (int x = _lechoX0; x <= _lechoX1; x++)
                    if (grid.GetMat(x, y) != MaterialId.Empty) n++;
            if (n > _celdasLechoPrev) _acuse.Disparar();
            _celdasLechoPrev = n;
        }

        private float EstadoFraccion()
        {
            switch (_state)
            {
                case State.Bajando: return 1f - Mathf.Clamp01(_stateTimer / PressDuration);
                case State.Cooldown: return Mathf.Clamp01(_stateTimer / CooldownDuration);
                default: return 1f;
            }
        }

        private bool EstaEnfocada() => MachineFocus.EsFoco(this, _player);

        // -----------------------------------------------------------------
        // APLICAR (mecánica SIN CAMBIOS respecto al playtest 25/26).
        // -----------------------------------------------------------------
        private void AplicarPrensada()
        {
            var universe = _sim.Universe;
            var grid = _sim.Grid;
            if (universe == null || grid == null) return;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;

            byte dominanteMat = MaterialId.Empty;
            int dominanteCount = 0;
            for (int y = _lechoY0; y <= _lechoY1; y++)
            {
                for (int x = _lechoX0; x <= _lechoX1; x++)
                {
                    byte mat = grid.GetMat(x, y);
                    if (mat == MaterialId.Empty) continue;
                    int count = 0;
                    for (int y2 = _lechoY0; y2 <= _lechoY1; y2++)
                        for (int x2 = _lechoX0; x2 <= _lechoX1; x2++)
                            if (grid.GetMat(x2, y2) == mat) count++;
                    if (count > dominanteCount) { dominanteCount = count; dominanteMat = mat; }
                }
            }

            for (int y = _lechoY0; y <= _lechoY1; y++)
            {
                for (int x = _lechoX0; x <= _lechoX1; x++)
                {
                    byte mat = grid.GetMat(x, y);
                    if (mat == MaterialId.Empty) continue;
                    AplicarRespuesta(x, y, mat, universe, grid, tick);
                }
            }

            RespuestaPrensa respuestaDominante = dominanteMat != MaterialId.Empty ? universe.Prensa(dominanteMat) : RespuestaPrensa.Nada;
            byte salidaDominante = MaterialSalida(dominanteMat, respuestaDominante);
            Hornada.RegistrarOp("prensa", dominanteMat, salidaDominante, CondicionDe(respuestaDominante));
            _chapaEstado = RotuloDe(respuestaDominante);
        }

        private void AplicarRespuesta(int x, int y, byte mat, Universe universe, CellGrid grid, uint tick)
        {
            RespuestaPrensa resp = universe.Prensa(mat);
            switch (resp)
            {
                case RespuestaPrensa.Compactar:
                    if (MaterialId.EsBaseEstado(mat))
                    {
                        byte compacto = MaterialId.MatDe(MaterialId.BaseDe(mat), EstadoMateria.Compacto);
                        grid.SetCell(x, y, compacto, resetAux: false);
                        grid.WakeChunk(x, y, tick);
                    }
                    break;

                case RespuestaPrensa.Reventar:
                    if (MaterialId.EsBaseEstado(mat))
                    {
                        byte polvo = MaterialId.MatDe(MaterialId.BaseDe(mat), EstadoMateria.Polvo);
                        grid.SetCell(x, y, polvo, resetAux: false);
                        grid.WakeChunk(x, y, tick);
                    }
                    break;

                case RespuestaPrensa.Escupir:
                    EscupirCelda(x, y, grid, tick);
                    break;

                default:
                    break;
            }
        }

        private byte MaterialSalida(byte matEntrada, RespuestaPrensa resp)
        {
            if (matEntrada == MaterialId.Empty || !MaterialId.EsBaseEstado(matEntrada)) return matEntrada;
            switch (resp)
            {
                case RespuestaPrensa.Compactar: return MaterialId.MatDe(MaterialId.BaseDe(matEntrada), EstadoMateria.Compacto);
                case RespuestaPrensa.Reventar: return MaterialId.MatDe(MaterialId.BaseDe(matEntrada), EstadoMateria.Polvo);
                default: return matEntrada;
            }
        }

        /// <summary>La celda ESCUPIDA se desplaza a la celda libre lateral más cercana FUERA del lecho -- los líquidos no se comprimen. Con el lecho nuevo (15 de ancho) la búsqueda tiene que llegar más lejos que antes.</summary>
        private void EscupirCelda(int x, int y, CellGrid grid, uint tick)
        {
            const int MaxBusqueda = 32;
            int destX = -1;

            for (int d = 1; d <= MaxBusqueda && destX < 0; d++)
            {
                int xDer = _lechoX1 + MuroGrosor + d;
                if (CellGrid.InBounds(xDer, y) && grid.GetMat(xDer, y) == MaterialId.Empty) { destX = xDer; break; }
                int xIzq = _lechoX0 - MuroGrosor - d;
                if (CellGrid.InBounds(xIzq, y) && grid.GetMat(xIzq, y) == MaterialId.Empty) { destX = xIzq; break; }
            }

            if (destX < 0) return;

            grid.SwapCells(CellGrid.Idx(x, y), CellGrid.Idx(destX, y));
            grid.WakeChunk(x, y, tick);
            grid.WakeChunk(destX, y, tick);
        }

        private static string CondicionDe(RespuestaPrensa resp)
        {
            switch (resp)
            {
                case RespuestaPrensa.Compactar: return "compactar";
                case RespuestaPrensa.Reventar: return "reventar";
                case RespuestaPrensa.Escupir: return "escupir";
                case RespuestaPrensa.Resistir: return "resistir";
                default: return "nada";
            }
        }

        /// <summary>Rótulos en español latino, tuteo (mandato 6).</summary>
        private static string RotuloDe(RespuestaPrensa resp)
        {
            switch (resp)
            {
                case RespuestaPrensa.Compactar: return "compactó";
                case RespuestaPrensa.Reventar: return "reventó";
                case RespuestaPrensa.Escupir: return "escupió el líquido";
                case RespuestaPrensa.Resistir: return "resiste la prensa";
                default: return "nada que prensar aquí";
            }
        }

        // =================================================================
        // VISUAL
        // =================================================================
        private void BuildVisual()
        {
            float c = SimRenderer.CellWorldSize;

            // ---- Las dos jambas, vestidas de sillería para que no se
            // confundan con la roca del fondo (son obra, no cueva).
            var sillar = MaquinariaSprites.Sillar(JambaAncho, JambaAlto);
            for (int lado = 0; lado < 2; lado++)
            {
                int x0 = lado == 0 ? _outX0 : _outX1 - JambaAncho + 1;
                var go = new GameObject(lado == 0 ? "PrensaJambaIzq" : "PrensaJambaDer");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3((x0 + JambaAncho * 0.5f) * c, (_baseY + JambaAlto * 0.5f) * c, 0f);
                MaquinariaSprites.CrearCapa(go.transform, "Sprite", sillar, 18, JambaAncho * c, JambaAlto * c);
            }

            // ---- El dintel.
            int spanTotal = _outX1 - _outX0 + 1;
            var dintelGo = new GameObject("PrensaDintel");
            dintelGo.transform.SetParent(transform, false);
            dintelGo.transform.position = new Vector3((_outX0 + spanTotal * 0.5f) * c, (_dintelY0 + DintelFilas * 0.5f) * c, 0f);
            MaquinariaSprites.CrearCapa(dintelGo.transform, "Sprite", MaquinariaSprites.Sillar(spanTotal, DintelFilas), 18,
                spanTotal * c, DintelFilas * c);

            // ---- LA BANDEJA ABIERTA (mandato 2). El marco es transparente por
            // dentro: enmarca el lecho REAL, no lo tapa.
            int spanLecho = LechoAncho + 2 * MuroGrosor; // 19
            int altoLecho = LechoAlto + 1;               // 6 (interior + su suelo)
            float anchoLechoW = spanLecho * c, altoLechoW = altoLecho * c;
            Vector3 posLecho = new Vector3((_lechoX0 - MuroGrosor + spanLecho * 0.5f) * c, (_baseY + altoLecho * 0.5f) * c, 0f);
            var marco = MaquinariaSprites.MarcoBandeja(spanLecho, altoLecho);

            var lechoGo = new GameObject("PrensaLecho");
            lechoGo.transform.SetParent(transform, false);
            lechoGo.transform.position = posLecho;
            _resalte = MaquinariaSprites.CrearCapa(lechoGo.transform, "Resalte", marco, 14, anchoLechoW * 1.12f, altoLechoW * 1.22f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);
            _latidoTrabajo = MaquinariaSprites.CrearCapa(lechoGo.transform, "LatidoTrabajo", marco, 15, anchoLechoW * 1.06f, altoLechoW * 1.12f);
            _latidoTrabajo.color = new Color(1f, 0.6f, 0.25f, 0f);
            MaquinariaSprites.CrearCapa(lechoGo.transform, "Marco", marco, 19, anchoLechoW, altoLechoW);
            _destelloMarco = MaquinariaSprites.CrearCapa(lechoGo.transform, "Acuse", marco, 22, anchoLechoW, altoLechoW);
            _destelloMarco.color = new Color(1f, 1f, 0.9f, 0f);

            // ---- La mandíbula: bloque dentado que cae entre las jambas.
            int spanMandibula = LechoAncho + 2;
            const int altoMandibula = 4;
            var mandibulaGo = new GameObject("PrensaMandibula");
            mandibulaGo.transform.SetParent(transform, false);
            _mandibulaTr = mandibulaGo.transform;
            MaquinariaSprites.CrearCapa(mandibulaGo.transform, "Sprite",
                MaquinariaSprites.MandibulaPrensa(spanMandibula, altoMandibula), 20, spanMandibula * c, altoMandibula * c);
            RecalcularRecorridoMandibula();

            // ---- El husillo, del dintel a la mandíbula, y el VOLANTE que
            // gira de verdad mientras baja (contrato §5).
            float husilloBaseY = _lechoY1 + 7f;
            float husilloAlto = (_dintelY0 - husilloBaseY) * c;
            var husilloGo = new GameObject("PrensaHusillo");
            husilloGo.transform.SetParent(transform, false);
            husilloGo.transform.position = new Vector3(_centroLecho.x, husilloBaseY * c + husilloAlto * 0.5f, 0f);
            MaquinariaSprites.CrearCapa(husilloGo.transform, "Sprite", MaquinariaSprites.Husillo(6), 17, 2.4f * c, husilloAlto);

            var volanteGo = new GameObject("PrensaVolante");
            volanteGo.transform.SetParent(transform, false);
            volanteGo.transform.position = new Vector3(_centroLecho.x, (_dintelY0 + DintelFilas + 3.5f) * c, 0f);
            _volanteTr = volanteGo.transform;
            MaquinariaSprites.CrearCapa(volanteGo.transform, "Sprite", MaquinariaSprites.Volante(8), 21, 8f * c, 8f * c);
        }

        private void RecalcularRecorridoMandibula()
        {
            float c = SimRenderer.CellWorldSize;
            // Arriba: colgando bajo el dintel. Abajo: justo sobre el labio del
            // lecho. El recorrido se DERIVA de la geometría (no una constante
            // suelta como el 0.9 del playtest 26, que no tenía nada que ver
            // con el tamaño real del aparato).
            // (segunda pasada) El reposo se ancla al LECHO, no al dintel: lo
            // que tiene que leerse es "esto cae sobre aquello", así que la
            // distancia significativa es la que hay hasta el labio.
            float yArriba = (_lechoY1 + 6f) * c;
            float yAbajo = (_lechoY1 + 2.5f) * c;
            _mandibulaArribaPos = new Vector3(_centroLecho.x, yArriba, 0f);
            _mandibulaAbajoPos = new Vector3(_centroLecho.x, yAbajo, 0f);
        }

        private void PosicionarMandibula(float fraccionArriba)
        {
            if (_mandibulaTr == null) return;
            _mandibulaTr.position = Vector3.Lerp(_mandibulaAbajoPos, _mandibulaArribaPos, Mathf.Clamp01(fraccionArriba));
        }

        private void ActualizarVisual()
        {
            // EL VOLANTE GIRA: rápido y en el sentido de apretar mientras baja,
            // lento y al revés mientras vuelve. Es el único elemento del taller
            // cuyo movimiento CUENTA lo que está pasando por sí solo.
            if (_volanteTr != null)
            {
                float vel = _state == State.Bajando ? -520f : (_state == State.Cooldown ? 130f : 0f);
                if (vel != 0f)
                {
                    _giroVolante += vel * Time.deltaTime;
                    _volanteTr.localRotation = Quaternion.Euler(0f, 0f, _giroVolante);
                }
            }

            if (_latidoTrabajo != null)
                _latidoTrabajo.color = new Color(1f, 0.6f, 0.25f, _pulsoTrabajo.AlfaTrabajo * 0.5f);
            if (_destelloMarco != null)
                _destelloMarco.color = new Color(1f, 1f, 0.9f, _acuse.Alfa);

            if (_resalte != null)
            {
                float objetivo = EstaEnfocada() ? 0.55f + 0.20f * Mathf.Sin(Time.time * 4f) : 0f;
                _alfaResalte = Mathf.MoveTowards(_alfaResalte, objetivo, 6f * Time.deltaTime);
                _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, _alfaResalte);
            }
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return;

            float cercaniaEstado = UiStyles.Cercania(_centroLecho, _player, RangoEstadoPleno, RangoEstadoDesvanece);
            float cercaniaNombre = UiStyles.Cercania(_centroLecho, _player, RangoNombrePleno, RangoNombreDesvanece);
            if (cercaniaEstado <= 0f && cercaniaNombre <= 0f) return;
            if (!_yaConocida && cercaniaNombre >= 0.98f) _yaConocida = true;

            UiStyles.Preparar();

            string estado = _state != State.Arriba ? "prensando…" : _chapaEstado;
            if (estado != null)
            {
                Color col = _state != State.Arriba ? UiStyles.Peligro : UiStyles.Aviso;
                UiStyles.PlacaMundo(_centroRotulo, estado,
                    new Color(col.r, col.g, col.b, col.a * cercaniaEstado), -UiStyles.S(6f));
            }

            if (!_yaConocida)
            {
                Color tenue = UiStyles.TextoTenue;
                UiStyles.PlacaMundo(_centroRotulo, "la prensa", new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercaniaNombre), -UiStyles.S(23f));
            }

            if (MachineFocus.MostrarPromptE && EstaEnfocada() && !UiStyles.RatonOcupado && _state == State.Arriba)
            {
                UiStyles.PlacaMundo(_centroRotulo, "E — prensar",
                    new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, cercaniaNombre), -UiStyles.S(23f));
            }
        }
    }
}
