using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// LA PRENSA (LO QUE PERSISTE, encargo B, §5.2 de CONTRATO_PERSISTE.md). Dos
    /// mandíbulas de piedra y un peso que cae sobre un LECHO (~5x3 celdas). Con
    /// E (y foco), la mandíbula cae en 0.5s y aplica <see cref="Universe.Prensa"/>
    /// a cada celda del lecho: Compactar/Reventar/Escupir/Resistir/Nada. Cero
    /// física de presión nueva (regla del contrato, y §8 del diseño: "la
    /// presión se EXPRESA físicamente... nunca como un número flotando") — es
    /// un evento local disparado por la tecla, no un campo simulado.
    ///
    /// -----------------------------------------------------------------------
    /// SPRITES: SOLO MaquinariaSprites EXISTENTE (mismo criterio que Crisol.cs,
    /// ver su docblock: Game/MaquinariaSprites.cs no está en el alcance de este
    /// encargo). El marco reutiliza ChasisPlaca (latón+carboncillo remachado);
    /// la mandíbula móvil reutiliza el mismo sprite, tintado más oscuro,
    /// animando su posición Y en vez de necesitar una silueta propia.
    ///
    /// -----------------------------------------------------------------------
    /// DECISIÓN (fuera del contrato, documentada): EL LECHO ES MAMPOSTERÍA
    /// PROPIA DE LA PRENSA, TALLADA EN Init() — MISMO MOTIVO Y MISMO PATRÓN QUE
    /// Crisol.CarveBasin (ver ese docblock): Sim/SimLevelBuilder.cs solo
    /// congela el ancla de una celda (`SimLevelBuilder.PrensaX`), así que la
    /// Prensa talla su propio recinto de Piedra de 1 celda de grosor alrededor
    /// del lecho vía <see cref="AlkahestSim.PaintStable"/> para que lo vertido
    /// no se derrame por el suelo abierto del cuarto.
    ///
    /// -----------------------------------------------------------------------
    /// PLAYTEST 26 (CONTRATO_LEGIBILIDAD.md, encargo M)
    /// -----------------------------------------------------------------------
    /// (1) La mampostería YA NO se talla en Init() (regla 15 de CLAUDE.md,
    ///     regla 47: SimLevelBuilder pasa a tallar TODA la mampostería del
    ///     plano). <see cref="TallarLecho"/> sigue viva para Reposicionar
    ///     (Mudanza, runtime); <see cref="TallarEnPlano"/> (estático, sobre
    ///     CellGrid directo) es la que llama Sim/SimLevelBuilder.cs al
    ///     construir el nivel -- mismo criterio que Game/Crisol.cs, ver su
    ///     docblock para el porqué de los dos caminos.
    /// (2) Gramática visual (§1): el lecho gana un
    ///     <see cref="MaquinariaSprites.Embudo"/> (§1.1, "toda boca que
    ///     recibe materia del frasco"), un
    ///     <see cref="MaquinariaSprites.MarcoContenedor"/> (§1.3) y un
    ///     <see cref="MaquinariaSprites.Husillo"/> montado sobre la
    ///     mandíbula (§1.4: "nadie ha dudado jamás de qué hace un tornillo de
    ///     banco"). El AFFORDANCE GLOW (§1.5, "Lecho de la Prensa:
    ///     Universe.Prensa(M) es Compactar o Reventar") usa el helper único
    ///     <see cref="MaquinariaSprites.AffordanceGlow"/>.
    /// </summary>
    public sealed class Prensa : MonoBehaviour, IMaquinaInteractiva, IMovible
    {
        private enum State { Arriba, Bajando, Cooldown }

        private const float ProximityRange = 3.2f;

        // Contrato §5.2: "lecho (~5x3)". (playtest 26) De privadas a PÚBLICAS:
        // SimLevelBuilder las lee para tallar la misma geometría en el plano
        // (ver TallarEnPlano más abajo).
        public const int LechoAncho = 5;
        public const int LechoAlto = 3;
        public const int MuroGrosor = 1;

        // Contrato §5.2: "anim 0.5s", "cooldown ~2s con la mandíbula arriba de nuevo".
        private const float PressDuration = 0.5f;
        private const float CooldownDuration = 2f;
        // Distancia de caída de la mandíbula sobre el lecho, en unidades de mundo.
        private const float RecorridoMandibula = 0.9f;

        private AlkahestSim _sim;
        private Transform _player;

        private int _anchorX;
        private int _baseY;
        private int _lechoX0, _lechoX1, _lechoY0, _lechoY1;

        private Vector3 _centro;

        private State _state = State.Arriba;
        private float _stateTimer;

        private Transform _mandibulaTr;
        private SpriteRenderer _mandibulaSr;
        private Vector3 _mandibulaArribaPos;
        private Vector3 _mandibulaAbajoPos;

        private SpriteRenderer _resalte;
        private float _alfaResalte;

        // ---- Playtest 26 (contrato §1): gramática visual + affordance glow ----
        private Flask _flask; // solo lectura (contrato: "sin tocar Flask.cs").
        private MaquinariaSprites.AffordanceGlow _glowLecho = new MaquinariaSprites.AffordanceGlow();
        private System.Func<byte, bool> _sirveLecho;
        private SpriteRenderer _afordanceLecho;

        private const float RangoEstadoPleno = 5.0f;
        private const float RangoEstadoDesvanece = 6.5f;
        private const float RangoNombrePleno = 2.6f;
        private const float RangoNombreDesvanece = 3.6f;
        private bool _yaConocida;
        private string _chapaEstado; // el veredicto de la última prensada ("compactó"/"reventó"/... ), null = nada que anunciar.

        public Vector3 PuntoFoco => _centro;
        public float RangoFoco => ProximityRange;

        public Vector3 CentroMundo => _centro;
        public Vector2 TamanoMundo => new Vector2(
            (LechoAncho + 2 * MuroGrosor) * SimRenderer.CellWorldSize,
            (LechoAlto + 2 * MuroGrosor) * SimRenderer.CellWorldSize);
        public Vector2Int AnclaCelda => new Vector2Int(_lechoX0 - MuroGrosor, _baseY);

        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            int span = LechoAncho + 2 * MuroGrosor;
            int x0 = anclaCelda.x, x1 = x0 + span - 1;
            int yTop = anclaCelda.y + LechoAlto + MuroGrosor;
            return x0 >= 1 && x1 <= CellGrid.W - 2 && anclaCelda.y >= 1 && yTop <= CellGrid.H - 2;
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap. `anchorX` = SimLevelBuilder.PrensaX (contrato §4.5).</summary>
        public void Init(AlkahestSim sim, Transform player, int anchorX)
        {
            _sim = sim;
            _player = player;
            _anchorX = anchorX;
            _baseY = SimLevelBuilder.CuartoY0 + 2; // contrato §4.5.
            _flask = player != null ? player.GetComponent<Flask>() : null;
            _sirveLecho = MaterialSirveLecho;

            RecalcularRegion();
            // (playtest 26) YA NO se talla aquí -- SimLevelBuilder.BuildCuartoIntimo
            // llama a TallarEnPlano con esta misma anchorX/baseY. TallarLecho()
            // sigue viva SOLO para Reposicionar (Mudanza), ver el docblock de la clase.
            BuildVisual();
            PosicionarMandibula(1f); // arranca arriba del todo.

            MachineFocus.Registrar(this);
            Mudanza.RegistrarMovible(this);
        }

        /// <summary>Contrato §1.5: "Lecho de la Prensa: Universe.Prensa(M) es Compactar o Reventar".</summary>
        private bool MaterialSirveLecho(byte mat)
        {
            if (_sim?.Universe == null) return false;
            var resp = _sim.Universe.Prensa(mat);
            return resp == RespuestaPrensa.Compactar || resp == RespuestaPrensa.Reventar;
        }

        private void RecalcularRegion()
        {
            _lechoX0 = _anchorX - LechoAncho / 2;
            _lechoX1 = _lechoX0 + LechoAncho - 1;
            _lechoY0 = _baseY + 1;
            _lechoY1 = _lechoY0 + LechoAlto - 1;

            float celda = SimRenderer.CellWorldSize;
            float centroX = (_lechoX0 + LechoAncho * 0.5f) * celda;
            float centroY = (_baseY + (LechoAlto + 2) * 0.5f) * celda;
            _centro = new Vector3(centroX, centroY, 0f);
            transform.position = _centro;
        }

        /// <summary>Muros de Piedra de 1 celda + suelo alrededor del lecho, interior vaciado EN CALIENTE (ver DECISIÓN en el doc de la clase). Solo la llama ya <see cref="Reposicionar"/> (Mudanza) -- Init ya NO la llama, ver el docblock de la clase.</summary>
        private void TallarLecho()
        {
            for (int x = _lechoX0 - MuroGrosor; x <= _lechoX1 + MuroGrosor; x++)
            {
                _sim.PaintStable(x, _lechoY0 - 1, 0, MaterialId.Stone);
            }
            for (int y = _lechoY0 - 1; y <= _lechoY1; y++)
            {
                _sim.PaintStable(_lechoX0 - MuroGrosor, y, 0, MaterialId.Stone);
                _sim.PaintStable(_lechoX1 + MuroGrosor, y, 0, MaterialId.Stone);
            }
            _sim.PaintRect(_lechoX0, _lechoY0, LechoAncho, LechoAlto, MaterialId.Empty);
        }

        /// <summary>
        /// (playtest 26) Talla el lecho de la Prensa DIRECTAMENTE sobre el
        /// CellGrid del plano -- llamado por Sim/SimLevelBuilder.cs al
        /// construir el nivel (construcción, no PaintStable/regla 29, que es
        /// para runtime). Misma geometría EXACTA que <see cref="TallarLecho"/>
        /// de instancia.
        /// </summary>
        public static void TallarEnPlano(CellGrid grid, int anchorX, int baseY)
        {
            int lechoX0 = anchorX - LechoAncho / 2;
            int lechoX1 = lechoX0 + LechoAncho - 1;
            int lechoY0 = baseY + 1;
            int lechoY1 = lechoY0 + LechoAlto - 1;

            for (int x = lechoX0 - MuroGrosor; x <= lechoX1 + MuroGrosor; x++)
                if (CellGrid.InBounds(x, lechoY0 - 1)) grid.SetCell(x, lechoY0 - 1, MaterialId.Stone);
            for (int y = lechoY0 - 1; y <= lechoY1; y++)
            {
                if (CellGrid.InBounds(lechoX0 - MuroGrosor, y)) grid.SetCell(lechoX0 - MuroGrosor, y, MaterialId.Stone);
                if (CellGrid.InBounds(lechoX1 + MuroGrosor, y)) grid.SetCell(lechoX1 + MuroGrosor, y, MaterialId.Stone);
            }
            for (int y = lechoY0; y <= lechoY1; y++)
                for (int x = lechoX0; x <= lechoX1; x++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Empty);
        }

        private void OnDestroy()
        {
            MachineFocus.Olvidar(this);
            Mudanza.OlvidarMovible(this);
        }

        public void Reposicionar(Vector2Int anclaCelda)
        {
            _anchorX = anclaCelda.x - MuroGrosor + LechoAncho / 2;
            _baseY = anclaCelda.y;
            RecalcularRegion();
            TallarLecho();
            PosicionarMandibula(_state == State.Bajando || _state == State.Cooldown ? EstadoFraccion() : 1f);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return;

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
            ActualizarResalte();

            // Contrato §1.5: sondeo cada ~0.25s (acumulador propio de AffordanceGlow).
            _glowLecho.Sondear(Time.deltaTime, _centro, _player, _flask, _sirveLecho);
            if (_afordanceLecho != null) _afordanceLecho.color = new Color(UiStyles.Exito.r, UiStyles.Exito.g, UiStyles.Exito.b, _glowLecho.Alfa);
        }

        /// <summary>Fracción 0(abajo)..1(arriba) de la mandíbula para el frame actual, según el estado.</summary>
        private float EstadoFraccion()
        {
            switch (_state)
            {
                case State.Bajando:
                    return 1f - Mathf.Clamp01(_stateTimer / PressDuration); // 1 -> 0.
                case State.Cooldown:
                    return Mathf.Clamp01(_stateTimer / CooldownDuration); // 0 -> 1 (sube de vuelta).
                default:
                    return 1f;
            }
        }

        private bool EstaEnfocada() => MachineFocus.EsFoco(this, _player);

        // -----------------------------------------------------------------
        // APLICAR: Universe.Prensa(mat) celda a celda del lecho (contrato §5.2).
        // -----------------------------------------------------------------
        private void AplicarPrensada()
        {
            var universe = _sim.Universe;
            var grid = _sim.Grid;
            if (universe == null || grid == null) return;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;

            // Tally del material DOMINANTE del lecho, ANTES de tocar nada (el
            // contrato pide "el material dominante del lecho", no el de una
            // celda cualquiera): lecho pequeño (15 celdas como mucho), tally
            // O(n^2) sin asignaciones -- barato porque esto corre una vez
            // cada 0.5s+2s, no en el hot path del stepper.
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

                // Resistir/Nada: la celda queda intocada (contrato §5.2).
                default:
                    break;
            }
        }

        /// <summary>Qué habría salido del material dominante tras su respuesta -- solo para el registro de Hornada, no vuelve a tocar la grilla (ya se tocó en AplicarRespuesta).</summary>
        private byte MaterialSalida(byte matEntrada, RespuestaPrensa resp)
        {
            if (matEntrada == MaterialId.Empty || !MaterialId.EsBaseEstado(matEntrada)) return matEntrada;
            switch (resp)
            {
                case RespuestaPrensa.Compactar: return MaterialId.MatDe(MaterialId.BaseDe(matEntrada), EstadoMateria.Compacto);
                case RespuestaPrensa.Reventar: return MaterialId.MatDe(MaterialId.BaseDe(matEntrada), EstadoMateria.Polvo);
                default: return matEntrada; // Escupir desplaza (mismo material), Resistir/Nada no cambian nada.
            }
        }

        /// <summary>La celda ESCUPIDA (contrato: "se DESPLAZA a la celda libre lateral más cercana fuera del lecho — los líquidos no se comprimen"). Busca en anillos crecientes a izquierda y derecha del lecho, misma fila; se queda con la más cercana de las dos direcciones.</summary>
        private void EscupirCelda(int x, int y, CellGrid grid, uint tick)
        {
            const int MaxBusqueda = 24;
            int destX = -1;

            for (int d = 1; d <= MaxBusqueda && destX < 0; d++)
            {
                int xDer = _lechoX1 + MuroGrosor + d;
                if (CellGrid.InBounds(xDer, y) && grid.GetMat(xDer, y) == MaterialId.Empty) { destX = xDer; break; }
                int xIzq = _lechoX0 - MuroGrosor - d;
                if (CellGrid.InBounds(xIzq, y) && grid.GetMat(xIzq, y) == MaterialId.Empty) { destX = xIzq; break; }
            }

            if (destX < 0) return; // sin hueco cercano: se queda donde está (raro, lecho pequeño y aire alrededor).

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

        private static string RotuloDe(RespuestaPrensa resp)
        {
            switch (resp)
            {
                case RespuestaPrensa.Compactar: return "compactó";
                case RespuestaPrensa.Reventar: return "reventó";
                case RespuestaPrensa.Escupir: return "escupió el líquido";
                case RespuestaPrensa.Resistir: return "resiste la prensa"; // contrato §5.2, texto literal.
                default: return "nada que prensar aquí";
            }
        }

        // -----------------------------------------------------------------
        // VISUAL (playtest 26, contrato §1): marco (ChasisPlaca) + mandíbula
        // (ChasisPlaca tintada, anima su posición Y) -- ver DECISIÓN de
        // sprites en el doc de la clase. Nuevo esta ronda: EMBUDO arriba
        // (§1.1), MARCO de latón sobre el lecho (§1.3), HUSILLO estático
        // sobre la mandíbula (§1.4) y el halo de affordance (§1.5).
        // -----------------------------------------------------------------
        private void BuildVisual()
        {
            float celda = SimRenderer.CellWorldSize;
            int span = LechoAncho + 2 * MuroGrosor;
            float ancho = span * celda;
            float altoMarco = (LechoAlto + 2 * MuroGrosor) * celda;

            var marcoGo = new GameObject("PrensaMarco");
            marcoGo.transform.SetParent(transform, false);
            marcoGo.transform.position = _centro;

            _resalte = MaquinariaSprites.CrearCapa(marcoGo.transform, "Resalte", MaquinariaSprites.ChasisPlaca(span), 16,
                ancho * 1.15f, altoMarco * 1.35f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);

            // Affordance glow del lecho (§1.5): halo MÁS GRANDE, detrás (orden 14).
            _afordanceLecho = MaquinariaSprites.CrearCapa(marcoGo.transform, "AfordanceLecho", MaquinariaSprites.Embudo(span), 14,
                ancho * 1.4f, altoMarco * 1.7f);
            _afordanceLecho.color = new Color(UiStyles.Exito.r, UiStyles.Exito.g, UiStyles.Exito.b, 0f);

            MaquinariaSprites.CrearCapa(marcoGo.transform, "Marco", MaquinariaSprites.ChasisPlaca(span), 18, ancho, altoMarco);
            // Marco de latón (§1.3, "cubeta enmarcada"): también aplica al lecho -- es un recipiente de trabajo como cualquier otro.
            MaquinariaSprites.CrearCapa(marcoGo.transform, "MarcoLaton", MaquinariaSprites.MarcoContenedor(span), 17, ancho, altoMarco);

            float altoMandibula = MuroGrosor * 2 * celda;
            _mandibulaArribaPos = _centro + new Vector3(0f, altoMarco * 0.5f + altoMandibula, 0f);
            _mandibulaAbajoPos = _mandibulaArribaPos - new Vector3(0f, RecorridoMandibula, 0f);

            var mandibulaGo = new GameObject("PrensaMandibula");
            mandibulaGo.transform.SetParent(transform, false);
            _mandibulaTr = mandibulaGo.transform;
            _mandibulaSr = MaquinariaSprites.CrearCapa(mandibulaGo.transform, "Mandibula", MaquinariaSprites.ChasisPlaca(span), 20,
                ancho, altoMandibula);
            _mandibulaSr.color = new Color(0.20f, 0.16f, 0.16f, 1f); // más oscura que el marco: la pieza que golpea.

            // Husillo (§1.4): vástago roscado estático sobre la posición de
            // reposo de la mandíbula -- "nadie ha dudado jamás de qué hace un
            // tornillo de banco" (contrato). No se anima: la pieza que se
            // mueve es la mandíbula, colgada de él.
            float husilloAlto = celda * 3f;
            var husilloGo = new GameObject("Husillo");
            husilloGo.transform.SetParent(transform, false);
            husilloGo.transform.position = _mandibulaArribaPos + new Vector3(0f, altoMandibula * 0.5f + husilloAlto * 0.5f, 0f);
            MaquinariaSprites.CrearCapa(husilloGo.transform, "Sprite", MaquinariaSprites.Husillo(span), 15, celda * 1.2f, husilloAlto);

            // Embudo (§1.1): montado en lo más alto, sobre el husillo -- la
            // materia entra por arriba, cae al lecho, la mandíbula la prensa.
            float embudoAlto = celda * 2.2f;
            var embudoGo = new GameObject("Embudo");
            embudoGo.transform.SetParent(transform, false);
            embudoGo.transform.position = husilloGo.transform.position + new Vector3(0f, husilloAlto * 0.5f + embudoAlto * 0.5f, 0f);
            MaquinariaSprites.CrearCapa(embudoGo.transform, "Sprite", MaquinariaSprites.Embudo(span), 21, ancho * 0.7f, embudoAlto);
        }

        private void PosicionarMandibula(float fraccionArriba)
        {
            if (_mandibulaTr == null) return;
            _mandibulaTr.position = Vector3.Lerp(_mandibulaAbajoPos, _mandibulaArribaPos, Mathf.Clamp01(fraccionArriba));
        }

        private void ActualizarResalte()
        {
            if (_resalte == null) return;
            float objetivo = EstaEnfocada() ? 0.60f + 0.20f * Mathf.Sin(Time.time * 4f) : 0f;
            _alfaResalte = Mathf.MoveTowards(_alfaResalte, objetivo, 6f * Time.deltaTime);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, _alfaResalte);
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return;

            float cercaniaEstado = UiStyles.Cercania(_centro, _player, RangoEstadoPleno, RangoEstadoDesvanece);
            float cercaniaNombre = UiStyles.Cercania(_centro, _player, RangoNombrePleno, RangoNombreDesvanece);
            if (cercaniaEstado <= 0f && cercaniaNombre <= 0f) return;
            if (!_yaConocida && cercaniaNombre >= 0.98f) _yaConocida = true;

            UiStyles.Preparar();

            if (_chapaEstado != null)
            {
                UiStyles.PlacaMundo(_centro, _chapaEstado,
                    new Color(UiStyles.Aviso.r, UiStyles.Aviso.g, UiStyles.Aviso.b, UiStyles.Aviso.a * cercaniaEstado), -UiStyles.S(17f));
            }

            if (!_yaConocida)
            {
                Color tenue = UiStyles.TextoTenue;
                UiStyles.PlacaMundo(_centro, "la prensa", new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercaniaNombre), -UiStyles.S(34f));
            }

            if (MachineFocus.MostrarPromptE && EstaEnfocada() && !UiStyles.RatonOcupado && _state == State.Arriba)
            {
                UiStyles.PlacaMundo(_centro, "E — prensar",
                    new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, cercaniaNombre), -UiStyles.S(34f));
            }
        }
    }
}
