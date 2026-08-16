using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// EL CRISOL (LO QUE PERSISTE, encargo B, §5.1 de CONTRATO_PERSISTE.md). La
    /// máquina central del laboratorio: una cámara de piedra con una cubeta
    /// donde el jugador vierte el limo/las bases de la seed y una tolva
    /// lateral donde ceba combustible. Nunca está apagado del todo (regla 44
    /// de CLAUDE.md, "un recurso perdible es una trampa" — aquí aplicada al
    /// revés: un aparato que NUNCA puede quedar frío-muerto): sin combustible
    /// empuja igualmente hacia su "rescoldo propio" (<see
    /// cref="Universe.CrisolTier0Raw"/>, raw 118 — hierve limo y agua, no
    /// funde nada).
    ///
    /// -----------------------------------------------------------------------
    /// SPRITES: SOLO MaquinariaSprites EXISTENTE (Game/MaquinariaSprites.cs NO
    /// está en el alcance de este encargo, "NADA MÁS" del contrato) — así que
    /// el chasis del crisol y de la tolva REUTILIZAN ChasisPlaca/
    /// ResistenciasPlaca (el mismo latón+carboncillo remachado de HeatPlate),
    /// tintados de un tono más oscuro/mineral para leerse como mampostería de
    /// horno en vez de una placa plana. No se inventa geometría nueva: es la
    /// misma fábrica, otro tinte y otra escala — calca el idioma, no lo repite
    /// literal.
    ///
    /// -----------------------------------------------------------------------
    /// DECISIÓN (fuera del contrato, documentada): LA CUBETA Y LA TOLVA SON
    /// MAMPOSTERÍA PROPIA DEL CRISOL, TALLADA EN Init()
    /// -----------------------------------------------------------------------
    /// El contrato §4.5 solo congela un ANCLA de una celda (`SimLevelBuilder.
    /// CrisolX`, suelo `CuartoY0+2`) para los tres aparatos de este encargo —
    /// a diferencia de las cubas/bandeja del taller clásico, que SimLevelBuilder
    /// excava como recipientes completos (`DrawUShape`) ANTES de que HeatPlate/
    /// ChillStone existan. Sim/SimLevelBuilder.cs es de otro encargo (A) y NO
    /// está en el alcance de este archivo, así que si el Crisol se limitara a
    /// leer una región lógica sin paredes de verdad, un líquido vertido en la
    /// cubeta se derramaría por el suelo abierto del cuarto en el primer tick.
    /// La solución: el propio Crisol talla su mampostería (muros/suelo de
    /// Piedra de 1 celda, mucho más fina que el WallThickness=3 de una cuba
    /// grande — es un caldero, no una cuba) alrededor de la cubeta y la tolva
    /// en <see cref="Init"/>, vía <see cref="AlkahestSim.PaintStable"/> (regla
    /// 22/29 de CLAUDE.md: quien CREA materia de la nada usa PaintStable, no
    /// Paint) — el mismo patrón que ya usa Game/Cincel.cs para tallar piedra,
    /// solo que aquí ocurre UNA vez, programáticamente, al construir la
    /// escena. Si el encargo A resulta haber tallado ya un hueco en ese punto
    /// exacto, esta talla es un no-op idéntico (Piedra sobre Piedra); si dejó
    /// roca maciza (lo esperable, dado que el contrato solo le exige el
    /// ancla), esta talla es la que abre el hueco de verdad.
    /// </summary>
    public sealed class Crisol : MonoBehaviour, IMaquinaInteractiva, IMovible
    {
        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        /// <summary>Radio de interacción con E (misma escala que HeatPlate/ChillStone/Dispenser).</summary>
        private const float ProximityRange = 3.2f;

        // -----------------------------------------------------------------
        // GEOMETRÍA (contrato §5.1: "cubeta ~7x5 celdas sobre su base" +
        // "tolva lateral ~3x3"). Grosor de muro deliberadamente de 1 celda
        // (frente al WallThickness=3 de las cubas grandes): un crisol es un
        // caldero compacto, no una cuba de taller.
        // -----------------------------------------------------------------
        private const int CubetaAncho = 7;
        private const int CubetaAlto = 5;
        private const int TolvaAncho = 3;
        private const int TolvaAlto = 3;
        private const int MuroGrosor = 1;
        /// <summary>Separación entre el muro derecho de la cubeta y el muro izquierdo de la tolva: son DOS recintos distintos del mismo aparato, no uno solo partido en dos.</summary>
        private const int HuecoEntreCubetaYTolva = 2;

        private AlkahestSim _sim;
        private Transform _player;
        private SubstanceKnowledge _conocimiento;

        // Ancla y bloque de fila del suelo (contrato §4.5: "suelo y=CuartoY0+2").
        private int _anchorX;
        private int _baseY;

        // Cubeta (interior, sin contar los muros de 1 celda que la rodean).
        private int _cubX0, _cubX1, _cubY0, _cubY1;
        // Tolva (interior).
        private int _tolX0, _tolX1, _tolY0, _tolY1;

        private Vector3 _centro;

        private float _accumulator;

        // ---- Empuje térmico (calcado del patrón de HeatPlate.ApplyHeatTick) ----
        private const int TempStepPerTick = 5;
        private byte _targetRaw; // objetivo actual del empuje: CrisolTier0Raw o TempCombustibleRaw del combustible activo.

        // ---- Consumo de combustible: 1 celda cada ~6s mientras haya alguna en la tolva ----
        private const int FuelConsumeTicks = 180; // 6s a 30Hz.
        private int _fuelTicksRestantes;
        private byte _fuelMatActivo; // Empty = sin combustible ardiendo ahora mismo.

        // ---- Recocido: carrera contra SimStepper.ApplyPhase (freezesInto=Templado del mundo) ----
        // El mundo transforma Fundido->Templado en cuanto temp <= SolidificaRaw
        // (ver Universe.cs, transición de MaterialDef que escribe el encargo A).
        // El Crisol tiene que interceptar la MISMA celda ANTES de que cruce ese
        // umbral exacto: por eso comprueba en CADA tick de física (no en el
        // sondeo de 0.8s, demasiado espaciado para ganar una carrera de un
        // único raw) con un margen de +4 raw por encima del umbral del mundo,
        // así que el Crisol siempre convierte primero, mientras la celda sigue
        // enfriando hacia abajo y el mundo todavía no ha visto temp<=umbral.
        private const int RecocidoMarginRaw = 4;
        /// <summary>¿Hay ahora mismo al menos un Fundido en la cubeta enfriando hacia el recocido (sin combustible activo)? Lo actualiza RecocidoScan; lo lee EtiquetaEstado para el rótulo "enfriando despacio — recocerá" sin volver a escanear la cubeta desde OnGUI.</summary>
        private bool _hayFundidoEnfriando;
        /// <summary>¿Tiene la cubeta algún material dentro ahora mismo? Lo actualiza ApplyHeatTick; distingue "hirviendo" (algo cociéndose a fuego lento) de "cargadme combustible (E)" (cubeta vacía, esperando).</summary>
        private bool _cubetaTieneContenido;

        // ---- Sondeo de transformaciones dirigidas (~0.8s, acumulador — nunca por frame) ----
        private const float ProbeInterval = 0.8f;
        private const int ProbeTicks = 24; // 0.8s * 30Hz.
        private const float DwellSecondsNeeded = 20f; // "sostenido ≥ ~20s" (contrato §5.1).
        private int _probeTickCounter;

        // Calcinación: Polvo sostenido en banda [CalcinacionRaw, FusionRaw) del MISMO base.
        private int _calcinaBaseActivo = -1; // -1 = sin candidato.
        private float _calcinaDwell;

        // Ceramización: Compacto sostenido con temp >= CeramizaRaw del MISMO base.
        private int _ceramizaBaseActivo = -1;
        private float _ceramizaDwell;

        private SpriteRenderer _resistenciasCubeta;
        private SpriteRenderer _resistenciasTolva;
        private SpriteRenderer _resalte;
        private float _alfaResalte;

        private const float RangoEstadoPleno = 5.0f;
        private const float RangoEstadoDesvanece = 6.5f;
        private const float RangoNombrePleno = 2.6f;
        private const float RangoNombreDesvanece = 3.6f;
        private bool _yaConocida;
        private string _chapaEstado;
        private const string ChapaNombre = "el crisol";

        public Vector3 PuntoFoco => _centro;
        public float RangoFoco => ProximityRange;

        // ---- IMovible (contrato: "los tres aparatos son IMovible, mudanza V/R gratis") ----
        public Vector3 CentroMundo => _centro;
        public Vector2 TamanoMundo => new Vector2(
            (_tolX1 + MuroGrosor - (_cubX0 - MuroGrosor) + 1) * SimRenderer.CellWorldSize,
            (CubetaAlto + 2) * SimRenderer.CellWorldSize);
        /// <summary>Ancla: esquina inferior izquierda del muro exterior de la cubeta + fila del suelo. La tolva viaja SIEMPRE relativa a esto (offset constante), ver Reposicionar.</summary>
        public Vector2Int AnclaCelda => new Vector2Int(_cubX0 - MuroGrosor, _baseY);

        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            int spanTotal = (_tolX1 + MuroGrosor) - (_cubX0 - MuroGrosor) + 1;
            int x0 = anclaCelda.x, x1 = x0 + spanTotal - 1;
            int yTop = anclaCelda.y + CubetaAlto + MuroGrosor;
            return x0 >= 1 && x1 <= CellGrid.W - 2 && anclaCelda.y >= 1 && yTop <= CellGrid.H - 2;
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap. `anchorX` = SimLevelBuilder.CrisolX (contrato §4.5).</summary>
        public void Init(AlkahestSim sim, Transform player, SubstanceKnowledge conocimiento, int anchorX)
        {
            _sim = sim;
            _player = player;
            _conocimiento = conocimiento;

            _anchorX = anchorX;
            // Contrato §4.5: "suelo y=CuartoY0+2" para los emplazamientos de las
            // tres máquinas nuevas.
            _baseY = SimLevelBuilder.CuartoY0 + 2;

            RecalcularRegiones();
            TallarMamposteria();
            BuildVisual();
            UpdateVisualTint();
            RebuildChapaEstado();
            _targetRaw = Universe.CrisolTier0Raw;

            MachineFocus.Registrar(this);
            Mudanza.RegistrarMovible(this);
        }

        private void RecalcularRegiones()
        {
            _cubX0 = _anchorX - CubetaAncho / 2;
            _cubX1 = _cubX0 + CubetaAncho - 1;
            _cubY0 = _baseY + 1;
            _cubY1 = _cubY0 + CubetaAlto - 1;

            _tolX0 = _cubX1 + MuroGrosor + HuecoEntreCubetaYTolva + MuroGrosor;
            _tolX1 = _tolX0 + TolvaAncho - 1;
            _tolY0 = _baseY + 1;
            _tolY1 = _tolY0 + TolvaAlto - 1;

            float celda = SimRenderer.CellWorldSize;
            float centroX = (_cubX0 - MuroGrosor + (_tolX1 + MuroGrosor - (_cubX0 - MuroGrosor) + 1) * 0.5f) * celda;
            float centroY = (_baseY + (CubetaAlto + 1) * 0.5f) * celda;
            _centro = new Vector3(centroX, centroY, 0f);
            transform.position = _centro;
        }

        /// <summary>Talla la mampostería propia del crisol (ver DECISIÓN en el doc de la clase): muros y suelo de Piedra de 1 celda alrededor de cubeta y tolva, interior vaciado a Empty.</summary>
        private void TallarMamposteria()
        {
            CarveBasin(_cubX0, _cubX1, _cubY0, _cubY1);
            CarveBasin(_tolX0, _tolX1, _tolY0, _tolY1);
        }

        private void CarveBasin(int x0, int x1, int y0, int y1)
        {
            // Suelo (una fila bajo el interior) + muros laterales de 1 celda,
            // abierto por arriba (se vierte desde el frasco, como cualquier
            // cubeta del proyecto).
            for (int x = x0 - MuroGrosor; x <= x1 + MuroGrosor; x++)
            {
                _sim.PaintStable(x, y0 - 1, 0, MaterialId.Stone);
            }
            for (int y = y0 - 1; y <= y1; y++)
            {
                _sim.PaintStable(x0 - MuroGrosor, y, 0, MaterialId.Stone);
                _sim.PaintStable(x1 + MuroGrosor, y, 0, MaterialId.Stone);
            }
            _sim.PaintRect(x0, y0, x1 - x0 + 1, y1 - y0 + 1, MaterialId.Empty);
        }

        private void OnDestroy()
        {
            MachineFocus.Olvidar(this);
            Mudanza.OlvidarMovible(this);
        }

        /// <summary>Mudanza (contrato: "los tres aparatos son IMovible"): reposiciona TODO el aparato (cubeta+tolva) manteniendo el offset relativo entre ambas, sin volver a llamar a Init/BuildVisual (mismo motivo que HeatPlate.Reposicionar).</summary>
        public void Reposicionar(Vector2Int anclaCelda)
        {
            int dx = anclaCelda.x - (_cubX0 - MuroGrosor);
            int dy = anclaCelda.y - _baseY;
            _anchorX += dx;
            _baseY += dy;
            RecalcularRegiones();
            TallarMamposteria(); // la nueva ubicación necesita su propia mampostería (misma razón que Init).
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return;

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && EstaEnfocada())
            {
                // DECISIÓN (fuera del contrato: §5.1 no define una acción de
                // E para el crisol, a diferencia de la Prensa/el Banco). El
                // crisol es autónomo -- calienta y transforma solo, sondeado
                // -- así que E aquí no dispara nada mecánico; sirve para que
                // el jugador "atienda" el aparato (foco visual + cuenta como
                // uso aprendido de la tecla, regla compartida con las demás
                // máquinas) sin necesitar una acción propia.
                MachineFocus.RegistrarUsoE();
            }

            _accumulator += Time.deltaTime;
            int steps = 0;
            while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
            {
                ApplyHeatTick();
                RecocidoScan();
                _probeTickCounter++;
                if (_probeTickCounter >= ProbeTicks)
                {
                    _probeTickCounter = 0;
                    ProbeTransformaciones();
                }
                _accumulator -= TickDt;
                steps++;
            }
            if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;

            ActualizarResalte();
        }

        private bool EstaEnfocada() => MachineFocus.EsFoco(this, _player);

        // -----------------------------------------------------------------
        // CALOR: rescoldo propio o combustible, empuje calcado del patrón de
        // HeatPlate.ApplyHeatTick (objetivo + rampa por tick, sin decaída por
        // fila porque aquí toda la cubeta es UNA sola cámara cerrada, no
        // filas sobre una placa abierta).
        // -----------------------------------------------------------------
        private void ApplyHeatTick()
        {
            ActualizarCombustible();

            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            byte target = _targetRaw;
            bool hayContenido = false;

            for (int y = _cubY0; y <= _cubY1; y++)
            {
                for (int x = _cubX0; x <= _cubX1; x++)
                {
                    if (!CellGrid.InBounds(x, y)) continue;
                    int idx = CellGrid.Idx(x, y);
                    if (grid.GetMat(idx) != MaterialId.Empty) hayContenido = true;
                    int cur = grid.temp[idx];
                    int next = cur < target ? Mathf.Min(target, cur + TempStepPerTick) : Mathf.Max(target, cur - TempStepPerTick);
                    grid.temp[idx] = (byte)next;
                    grid.WakeChunk(x, y, tick);
                }
            }
            _cubetaTieneContenido = hayContenido;
        }

        /// <summary>Busca combustible en la tolva y actualiza <see cref="_targetRaw"/>. Consume 1 celda cada <see cref="FuelConsumeTicks"/> ticks (~6s) mientras haya alguna presente.</summary>
        private void ActualizarCombustible()
        {
            var universe = _sim.Universe;
            if (universe == null) return;

            int foundX = -1, foundY = -1;
            byte foundMat = MaterialId.Empty;
            var grid = _sim.Grid;
            for (int y = _tolY0; y <= _tolY1 && foundX < 0; y++)
            {
                for (int x = _tolX0; x <= _tolX1; x++)
                {
                    byte m = grid.GetMat(x, y);
                    if (m == MaterialId.Empty) continue;
                    if (!universe.EsCombustible(m)) continue;
                    foundX = x; foundY = y; foundMat = m;
                    break;
                }
            }

            if (foundX < 0)
            {
                // Sin combustible en la tolva: rescoldo propio, el crisol
                // NUNCA está muerto (regla 44 de CLAUDE.md, anti-trampa).
                _fuelMatActivo = MaterialId.Empty;
                _fuelTicksRestantes = 0;
                _targetRaw = Universe.CrisolTier0Raw;
                return;
            }

            _fuelMatActivo = foundMat;
            _targetRaw = universe.TempCombustibleRaw(foundMat);

            if (_fuelTicksRestantes <= 0) _fuelTicksRestantes = FuelConsumeTicks;
            _fuelTicksRestantes--;
            if (_fuelTicksRestantes <= 0)
            {
                grid.SetCell(foundX, foundY, MaterialId.Empty);
                grid.WakeChunk(foundX, foundY, _sim.Stepper != null ? _sim.Stepper.Tick : 0u);
            }
        }

        /// <summary>Etiqueta legible de la condición térmica actual, para las tres condiciones que registra Hornada (contrato §5.1: "tier0"/"combustible:&lt;nombre&gt;").</summary>
        private string CondicionCalor()
        {
            if (_fuelMatActivo == MaterialId.Empty) return "tier0";
            string nombre = _conocimiento != null ? _conocimiento.NombreParaHud(_fuelMatActivo) : "???";
            return $"combustible:{nombre}";
        }

        // -----------------------------------------------------------------
        // RECOCIDO: ver el bloque de doc de la clase sobre la carrera. Corre
        // en CADA tick de física (no en el sondeo de 0.8s) precisamente para
        // ganarla con margen.
        // -----------------------------------------------------------------
        private void RecocidoScan()
        {
            var universe = _sim.Universe;
            if (universe == null) return;
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            bool hayFundido = false;

            for (int y = _cubY0; y <= _cubY1; y++)
            {
                for (int x = _cubX0; x <= _cubX1; x++)
                {
                    byte mat = grid.GetMat(x, y);
                    if (!MaterialId.EsBaseEstado(mat)) continue;
                    if (MaterialId.EstadoDe(mat) != EstadoMateria.Fundido) continue;

                    hayFundido = true;
                    int baseIdx = MaterialId.BaseDe(mat);
                    int umbral = universe.SolidificaRaw(baseIdx) + RecocidoMarginRaw;
                    int idx = CellGrid.Idx(x, y);
                    if (grid.temp[idx] > umbral) continue;

                    byte recocido = MaterialId.MatDe(baseIdx, EstadoMateria.Recocido);
                    grid.SetCell(idx, recocido, resetAux: false);
                    grid.WakeChunk(x, y, tick);
                    Hornada.RegistrarOp("crisol", mat, recocido, "lento");
                }
            }

            // "Enfriando despacio -- recocerá" (contrato §5.1): SOLO se lee así
            // mientras no hay combustible activo empujando la cubeta hacia
            // arriba -- es la condición literal del contrato ("cuando el
            // combustible se agota y la cubeta ENFRÍA"). Con combustible
            // activo, un Fundido presente se lee como "fundiendo" (ver
            // EtiquetaEstado), aunque técnicamente siga vivo el mismo scan.
            _hayFundidoEnfriando = hayFundido && _fuelMatActivo == MaterialId.Empty;
        }

        // -----------------------------------------------------------------
        // SONDEO (~0.8s, acumulador de ticks — nunca por frame): calcinación
        // y ceramización. La fusión NO se implementa aquí a propósito: el
        // mundo ya funde Polvo->Fundido vía meltsAt (Sim/Universe.cs,
        // MaterialDef.Polvo.meltsAt = FusionRaw(base)) en cuanto ApplyPhase
        // ve temp>=umbral, y duplicar esa comparación aquí sería la MISMA
        // lógica en dos sitios (contrato §5.1: "el crisol solo ACELERA el
        // sondeo... limítate a calentar"). El Crisol se limita a mantener la
        // cubeta caliente; ApplyPhase hace el resto solo.
        // -----------------------------------------------------------------
        private void ProbeTransformaciones()
        {
            var universe = _sim.Universe;
            if (universe == null) return;

            ProbeCalcinacion(universe);
            ProbeCeramizacion(universe);
        }

        /// <summary>Polvo sostenido ≥20s en banda [CalcinacionRaw,FusionRaw) del MISMO base -> Calcinado. "Sin arrays por celda" (contrato): un candidato de base único por vez, exige que TODO el Polvo presente en la cubeta sea de ese base y esté en banda.</summary>
        private void ProbeCalcinacion(Universe universe)
        {
            var grid = _sim.Grid;
            int candidato = -1;
            bool rota = false;
            bool hayAlguna = false;

            for (int y = _cubY0; y <= _cubY1 && !rota; y++)
            {
                for (int x = _cubX0; x <= _cubX1; x++)
                {
                    byte mat = grid.GetMat(x, y);
                    if (!MaterialId.EsBaseEstado(mat)) continue;
                    if (MaterialId.EstadoDe(mat) != EstadoMateria.Polvo) continue;

                    int baseIdx = MaterialId.BaseDe(mat);
                    if (candidato < 0) candidato = baseIdx;
                    else if (candidato != baseIdx) { rota = true; break; }

                    hayAlguna = true;
                    byte t = grid.temp[CellGrid.Idx(x, y)];
                    byte calcinaMin = universe.CalcinacionRaw(baseIdx);
                    byte fusionMax = universe.FusionRaw(baseIdx);
                    if (t < calcinaMin || t >= fusionMax) { rota = true; break; }
                }
            }

            if (!hayAlguna || rota || candidato != _calcinaBaseActivo)
            {
                _calcinaBaseActivo = hayAlguna && !rota ? candidato : -1;
                _calcinaDwell = 0f;
                if (_calcinaBaseActivo < 0) return;
            }

            _calcinaDwell += ProbeInterval;
            if (_calcinaDwell < DwellSecondsNeeded) return;

            // Cumplido: TODA la banda de Polvo(base) de la cubeta pasa a Calcinado.
            byte polvoMat = MaterialId.MatDe(_calcinaBaseActivo, EstadoMateria.Polvo);
            byte calcinadoMat = MaterialId.MatDe(_calcinaBaseActivo, EstadoMateria.Calcinado);
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            for (int y = _cubY0; y <= _cubY1; y++)
            {
                for (int x = _cubX0; x <= _cubX1; x++)
                {
                    int idx = CellGrid.Idx(x, y);
                    if (grid.GetMat(idx) != polvoMat) continue;
                    grid.SetCell(idx, calcinadoMat, resetAux: false);
                    grid.WakeChunk(x, y, tick);
                }
            }
            Hornada.RegistrarOp("crisol", polvoMat, calcinadoMat, CondicionCalor());
            _calcinaDwell = 0f;
            _calcinaBaseActivo = -1;
        }

        /// <summary>Compacto sostenido ≥20s con temp>=CeramizaRaw (si≠0, algunas bases no ceramizan) del MISMO base -> Ceramico. Mismo criterio de "un candidato" que ProbeCalcinacion.</summary>
        private void ProbeCeramizacion(Universe universe)
        {
            var grid = _sim.Grid;
            int candidato = -1;
            bool rota = false;
            bool hayAlguna = false;

            for (int y = _cubY0; y <= _cubY1 && !rota; y++)
            {
                for (int x = _cubX0; x <= _cubX1; x++)
                {
                    byte mat = grid.GetMat(x, y);
                    if (!MaterialId.EsBaseEstado(mat)) continue;
                    if (MaterialId.EstadoDe(mat) != EstadoMateria.Compacto) continue;

                    int baseIdx = MaterialId.BaseDe(mat);
                    byte ceramizaMin = universe.CeramizaRaw(baseIdx);
                    if (ceramizaMin == 0) continue; // esta base no ceramiza (contrato §3).

                    if (candidato < 0) candidato = baseIdx;
                    else if (candidato != baseIdx) { rota = true; break; }

                    hayAlguna = true;
                    byte t = grid.temp[CellGrid.Idx(x, y)];
                    if (t < ceramizaMin) { rota = true; break; }
                }
            }

            if (!hayAlguna || rota || candidato != _ceramizaBaseActivo)
            {
                _ceramizaBaseActivo = hayAlguna && !rota ? candidato : -1;
                _ceramizaDwell = 0f;
                if (_ceramizaBaseActivo < 0) return;
            }

            _ceramizaDwell += ProbeInterval;
            if (_ceramizaDwell < DwellSecondsNeeded) return;

            byte compactoMat = MaterialId.MatDe(_ceramizaBaseActivo, EstadoMateria.Compacto);
            byte ceramicoMat = MaterialId.MatDe(_ceramizaBaseActivo, EstadoMateria.Ceramico);
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            for (int y = _cubY0; y <= _cubY1; y++)
            {
                for (int x = _cubX0; x <= _cubX1; x++)
                {
                    int idx = CellGrid.Idx(x, y);
                    if (grid.GetMat(idx) != compactoMat) continue;
                    grid.SetCell(idx, ceramicoMat, resetAux: false);
                    grid.WakeChunk(x, y, tick);
                }
            }
            Hornada.RegistrarOp("crisol", compactoMat, ceramicoMat, CondicionCalor());
            _ceramizaDwell = 0f;
            _ceramizaBaseActivo = -1;
        }

        // -----------------------------------------------------------------
        // VISUAL: chasis + resistencias de HeatPlate REUTILIZADOS (ver
        // DECISIÓN de sprites en el doc de la clase), un juego para la cubeta
        // y otro más pequeño para la tolva. Tintados de mineral/carboncillo
        // en vez del naranja de la placa para leerse como mampostería.
        // -----------------------------------------------------------------
        private void BuildVisual()
        {
            float celda = SimRenderer.CellWorldSize;

            int spanCubeta = _cubX1 - _cubX0 + 1;
            float anchoCubeta = spanCubeta * celda;
            float altoCubeta = (CubetaAlto + 1) * celda; // + el muro/suelo de 1 celda.
            Vector3 centroCubeta = new Vector3((_cubX0 + spanCubeta * 0.5f) * celda, (_baseY + (CubetaAlto + 1) * 0.5f) * celda, 0f);

            int spanTolva = _tolX1 - _tolX0 + 1;
            float anchoTolva = spanTolva * celda;
            float altoTolva = (TolvaAlto + 1) * celda;
            Vector3 centroTolva = new Vector3((_tolX0 + spanTolva * 0.5f) * celda, (_baseY + (TolvaAlto + 1) * 0.5f) * celda, 0f);

            var cubetaGo = new GameObject("CrisolCubetaChasis");
            cubetaGo.transform.SetParent(transform, false);
            cubetaGo.transform.position = centroCubeta;
            _resalte = MaquinariaSprites.CrearCapa(cubetaGo.transform, "Resalte", MaquinariaSprites.ChasisPlaca(spanCubeta), 16,
                anchoCubeta * 1.15f, altoCubeta * 1.35f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);
            MaquinariaSprites.CrearCapa(cubetaGo.transform, "Chasis", MaquinariaSprites.ChasisPlaca(spanCubeta), 18, anchoCubeta, altoCubeta);
            _resistenciasCubeta = MaquinariaSprites.CrearCapa(cubetaGo.transform, "Rescoldo", MaquinariaSprites.ResistenciasPlaca(spanCubeta), 19,
                anchoCubeta, altoCubeta);

            var tolvaGo = new GameObject("CrisolTolvaChasis");
            tolvaGo.transform.SetParent(transform, false);
            tolvaGo.transform.position = centroTolva;
            MaquinariaSprites.CrearCapa(tolvaGo.transform, "Chasis", MaquinariaSprites.ChasisPlaca(spanTolva), 18, anchoTolva, altoTolva);
            _resistenciasTolva = MaquinariaSprites.CrearCapa(tolvaGo.transform, "Brasas", MaquinariaSprites.ResistenciasPlaca(spanTolva), 19,
                anchoTolva, altoTolva);
        }

        private void UpdateVisualTint()
        {
            if (_resistenciasCubeta != null) _resistenciasCubeta.color = ColorRescoldo();
            if (_resistenciasTolva != null)
            {
                bool ardiendo = _fuelMatActivo != MaterialId.Empty;
                _resistenciasTolva.color = ardiendo
                    ? new Color(1f, 0.55f, 0.20f, 1f)
                    : new Color(0.32f, 0.24f, 0.20f, 1f);
            }
        }

        private Color ColorRescoldo()
        {
            // Rescoldo propio (tier0): ámbar tenue, siempre encendido. Con
            // combustible activo, el rescoldo sube a blanco-naranja: la
            // misma lectura que HeatPlate.ARDIENTE.
            bool conCombustible = _fuelMatActivo != MaterialId.Empty;
            float pulso = 0.82f + 0.18f * Mathf.Sin(Time.time * (conCombustible ? 6f : 2.2f));
            return conCombustible
                ? new Color(1f, 0.50f * pulso, 0.20f * pulso, 1f)
                : new Color(0.62f * pulso + 0.10f, 0.30f * pulso, 0.14f * pulso, 1f);
        }

        private void ActualizarResalte()
        {
            UpdateVisualTint(); // el tinte del rescoldo late cada frame como HeatPlate.AnimarResistencias.
            if (_resalte == null) return;
            float objetivo = EstaEnfocada() ? 0.60f + 0.20f * Mathf.Sin(Time.time * 4f) : 0f;
            _alfaResalte = Mathf.MoveTowards(_alfaResalte, objetivo, 6f * Time.deltaTime);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, _alfaResalte);
        }

        /// <summary>Rótulo con verbo (contrato §5.1): "hirviendo"/"calcinando"/"fundiendo"/"enfriando despacio — recocerá"/"cargadme combustible (E)". Se reconstruye solo cuando cambia el estado dominante (regla de cero asignaciones por frame en OnGUI).</summary>
        private void RebuildChapaEstado()
        {
            _chapaEstado = EtiquetaEstado();
        }

        /// <summary>
        /// Prioridad de lectura (DECISIÓN: el contrato da los cinco verbos
        /// pero no el orden entre ellos):
        ///  1) recociendo (la carrera ya se ganó y sigue viva la condición
        ///     "sin combustible, con Fundido presente") gana a todo lo demás:
        ///     es el momento de mayor tensión narrativa ("se me va a poner
        ///     dócil si no meto más leña").
        ///  2) calcinando/ceramizando: el sondeo lleva ≥1 ciclo contando.
        ///  3) fundiendo: hay combustible activo empujando de verdad.
        ///  4) cubeta vacía y sin combustible: invitación explícita a cebar.
        ///  5) hirviendo: el resto -- rescoldo propio con algo dentro,
        ///     cociéndose a fuego lento (raw 118, hierve pero no funde nada).
        /// </summary>
        private string EtiquetaEstado()
        {
            if (_hayFundidoEnfriando) return "enfriando despacio — recocerá";
            if (_calcinaBaseActivo >= 0 || _ceramizaBaseActivo >= 0) return "calcinando";
            if (_fuelMatActivo != MaterialId.Empty) return "fundiendo";
            if (!_cubetaTieneContenido) return "cargadme combustible (E)";
            return "hirviendo";
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return;

            float cercaniaEstado = UiStyles.Cercania(_centro, _player, RangoEstadoPleno, RangoEstadoDesvanece);
            float cercaniaNombre = UiStyles.Cercania(_centro, _player, RangoNombrePleno, RangoNombreDesvanece);
            if (cercaniaEstado <= 0f && cercaniaNombre <= 0f) return;
            if (!_yaConocida && cercaniaNombre >= 0.98f) _yaConocida = true;

            UiStyles.Preparar();

            // El rótulo se recalcula aquí (barato: comparación de enteros, no
            // asignación de string salvo que cambie el mensaje) porque el
            // estado del crisol cambia por sondeo/consumo de combustible, no
            // por una pulsación de tecla como en HeatPlate — no hay un único
            // punto de "CycleState" desde el que refrescarlo.
            string etiquetaAhora = EtiquetaEstado();
            if (etiquetaAhora != _chapaEstado) _chapaEstado = etiquetaAhora;

            Color color = _fuelMatActivo != MaterialId.Empty ? UiStyles.Peligro : UiStyles.Aviso;
            UiStyles.PlacaMundo(_centro, _chapaEstado, new Color(color.r, color.g, color.b, color.a * cercaniaEstado), -UiStyles.S(17f));

            if (!_yaConocida)
            {
                Color tenue = UiStyles.TextoTenue;
                UiStyles.PlacaMundo(_centro, ChapaNombre, new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercaniaNombre), -UiStyles.S(34f));
            }

            if (MachineFocus.MostrarPromptE && EstaEnfocada() && !UiStyles.RatonOcupado)
            {
                UiStyles.PlacaMundo(_centro, "E — atended el crisol",
                    new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, cercaniaNombre), -UiStyles.S(34f));
            }
        }
    }
}
