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
    ///
    /// -----------------------------------------------------------------------
    /// PLAYTEST 26 (CONTRATO_LEGIBILIDAD.md, encargo M) — DOS CAMBIOS
    /// -----------------------------------------------------------------------
    /// (1) LA MAMPOSTERÍA YA NO SE TALLA EN Init() (regla 15 de CLAUDE.md:
    ///     se comenta el porqué, no se borra el mecanismo). El reparto de
    ///     responsabilidades cambia (contrato §2, regla 47): ahora
    ///     Sim/SimLevelBuilder.cs talla la cubeta y la tolva del Crisol AL
    ///     CONSTRUIR EL PLANO, vía <see cref="TallarEnPlano"/> (estático,
    ///     opera directo sobre <see cref="CellGrid"/> con SetCell -- es
    ///     CONSTRUCCIÓN de nivel, no creación en juego, así que no aplica la
    ///     regla 29/PaintStable, que es para runtime). <see
    ///     cref="CarveBasin"/>/<see cref="TallarMamposteria"/> de INSTANCIA
    ///     SIGUEN VIVOS: Mudanza (tecla V/R, tercer archivo de este mismo
    ///     encargo) reubica el Crisol EN CALIENTE, y esa reubicación sí es
    ///     una talla en juego de verdad (ahí SÍ sigue aplicando PaintStable).
    ///     Las medidas (CubetaAncho/Alto, TolvaAncho/Alto, MuroGrosor,
    ///     HuecoEntreCubetaYTolva) pasan de privadas a públicas para que
    ///     SimLevelBuilder no las duplique a mano -- una sola fuente de
    ///     verdad de la GEOMETRÍA, aunque haya dos caminos de escritura
    ///     (plano vs. runtime) por la razón de arriba.
    /// (2) LA GRAMÁTICA VISUAL (contrato §1): la tolva deja de reutilizar
    ///     <see cref="MaquinariaSprites.ChasisPlaca"/> (que la confundía con
    ///     un embudo: "otra forma, otra altura, otro color" es la regla
    ///     §1.2) y pasa a <see cref="MaquinariaSprites.Brasero"/> (cesto de
    ///     hierro). La cubeta gana un <see cref="MaquinariaSprites.Embudo"/>
    ///     montado arriba (§1.1), un <see cref="MaquinariaSprites.MarcoContenedor"/>
    ///     (§1.3, "aquí queda el resultado") y una
    ///     <see cref="MaquinariaSprites.Chimenea"/> con bocanadas de
    ///     <see cref="MaquinariaSprites.Humo"/> SOLO mientras
    ///     <c>_fuelMatActivo != Empty</c> (§1.4: "el verbo vive en el
    ///     cuerpo"). El AFFORDANCE GLOW (§1.5) usa el helper único
    ///     <see cref="MaquinariaSprites.AffordanceGlow"/>: una instancia por
    ///     boca (embudo/brasero), sondeada en Update con un delegado
    ///     cacheado en Init (cero allocs por sondeo).
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
        // (playtest 26) De privadas a PÚBLICAS: Sim/SimLevelBuilder.cs las lee
        // para tallar esta misma geometría al construir el plano (ver
        // TallarEnPlano más abajo) -- una sola fuente de verdad del TAMAÑO,
        // aunque el QUIÉN ESCRIBE la piedra cambie según el momento (plano vs.
        // runtime, ver el docblock de la clase).
        public const int CubetaAncho = 7;
        public const int CubetaAlto = 5;
        public const int TolvaAncho = 3;
        public const int TolvaAlto = 3;
        public const int MuroGrosor = 1;
        /// <summary>Separación entre el muro derecho de la cubeta y el muro izquierdo de la tolva: son DOS recintos distintos del mismo aparato, no uno solo partido en dos.</summary>
        public const int HuecoEntreCubetaYTolva = 2;

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

        // ---- Playtest 26 (contrato §1): gramática visual + affordance glow ----
        private Vector3 _centroCubeta, _centroTolva;
        private Flask _flask; // leído SOLO (nunca modificado -- contrato: "sin tocar Flask.cs").
        private MaquinariaSprites.AffordanceGlow _glowEmbudo = new MaquinariaSprites.AffordanceGlow();
        private MaquinariaSprites.AffordanceGlow _glowBrasero = new MaquinariaSprites.AffordanceGlow();
        // Delegados CACHEADOS en Init (método de instancia -> Func, una sola
        // asignación de por vida): el sondeo de AffordanceGlow los reutiliza
        // cada ~0.25s sin generar basura (ver MaquinariaSprites.AffordanceGlow.Sondear).
        private System.Func<byte, bool> _sirveEmbudo;
        private System.Func<byte, bool> _sirveBrasero;
        private SpriteRenderer _afordanceEmbudo, _afordanceBrasero;
        // Chimenea: bocanadas de humo SOLO mientras arde combustible (§1.4).
        private const int HumoPuffs = 3;
        private const float HumoCicloSeg = 2.4f;
        private readonly SpriteRenderer[] _humo = new SpriteRenderer[HumoPuffs];
        private Vector3 _humoOrigen;

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
            _flask = player != null ? player.GetComponent<Flask>() : null; // ver docblock de la clase: solo LECTURA (MaterialDominante()), Flask.cs no se toca.
            _sirveEmbudo = MaterialSirveEmbudo;
            _sirveBrasero = MaterialSirveBrasero;

            RecalcularRegiones();
            // (playtest 26) YA NO se talla aquí -- ver el docblock de la clase,
            // "LA MAMPOSTERÍA YA NO SE TALLA EN Init()". SimLevelBuilder.
            // BuildCuartoIntimo llama a TallarEnPlano con esta misma anchorX/baseY
            // ANTES de que este componente exista siquiera; si por lo que sea el
            // plano NO se hubiera tallado (nivel clásico, tests), TallarMamposteria()
            // sigue viva y la sigue usando Reposicionar (Mudanza) -- ver ese método.
            BuildVisual();
            UpdateVisualTint();
            RebuildChapaEstado();
            _targetRaw = Universe.CrisolTier0Raw;

            MachineFocus.Registrar(this);
            Mudanza.RegistrarMovible(this);
        }

        /// <summary>Cualquier líquido o polvo no vacío -- contrato §1.5 ("Embudo del Crisol: cualquier líquido o polvo").</summary>
        private bool MaterialSirveEmbudo(byte mat)
        {
            if (mat == MaterialId.Empty || _sim?.Universe == null) return false;
            var arquetipo = _sim.Universe.Get(mat).archetype;
            return arquetipo == MaterialArchetype.Liquid || arquetipo == MaterialArchetype.Powder;
        }

        /// <summary>Contrato §1.5: "Brasero: Universe.EsCombustible(M)".</summary>
        private bool MaterialSirveBrasero(byte mat) => _sim?.Universe != null && _sim.Universe.EsCombustible(mat);

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

            // Bocas (contrato §1.5): el centro de cada recinto, usado por
            // AffordanceGlow para medir distancia jugador->boca.
            int spanCubeta = _cubX1 - _cubX0 + 1;
            _centroCubeta = new Vector3((_cubX0 + spanCubeta * 0.5f) * celda, (_baseY + (CubetaAlto + 1) * 0.5f) * celda, 0f);
            int spanTolva = _tolX1 - _tolX0 + 1;
            _centroTolva = new Vector3((_tolX0 + spanTolva * 0.5f) * celda, (_baseY + (TolvaAlto + 1) * 0.5f) * celda, 0f);

            // Origen del humo (mismo cálculo que BuildVisual): recomputado aquí
            // TAMBIÉN porque Reposicionar (Mudanza) llama a RecalcularRegiones
            // pero NO a BuildVisual (regla 36 de CLAUDE.md, BuildVisual no es
            // idempotente) -- sin esto, tras mover el Crisol el humo seguiría
            // saliendo de la posición VIEJA.
            float altoCubeta = (CubetaAlto + 1) * celda;
            float chimeneaOffsetX = (spanCubeta * celda) * 0.32f;
            float chimeneaAlto = celda * 3.2f;
            _humoOrigen = _centroCubeta + new Vector3(chimeneaOffsetX, altoCubeta * 0.5f + chimeneaAlto, 0f);
        }

        /// <summary>
        /// (playtest 26) Talla la mampostería del Crisol DIRECTAMENTE sobre el
        /// CellGrid del plano -- llamado por Sim/SimLevelBuilder.cs al construir
        /// el nivel (construcción, no creación en juego: usa grid.SetCell, no
        /// PaintStable/regla 29, que es para runtime). Misma geometría EXACTA
        /// que <see cref="CarveBasin"/> de instancia, que sigue viva para
        /// Reposicionar (Mudanza) -- ver el docblock de la clase.
        /// </summary>
        public static void TallarEnPlano(CellGrid grid, int anchorX, int baseY)
        {
            int cubX0 = anchorX - CubetaAncho / 2;
            int cubX1 = cubX0 + CubetaAncho - 1;
            int cubY0 = baseY + 1;
            int cubY1 = cubY0 + CubetaAlto - 1;

            int tolX0 = cubX1 + MuroGrosor + HuecoEntreCubetaYTolva + MuroGrosor;
            int tolX1 = tolX0 + TolvaAncho - 1;
            int tolY0 = baseY + 1;
            int tolY1 = tolY0 + TolvaAlto - 1;

            CarveBasinEnGrid(grid, cubX0, cubX1, cubY0, cubY1);
            CarveBasinEnGrid(grid, tolX0, tolX1, tolY0, tolY1);
        }

        private static void CarveBasinEnGrid(CellGrid grid, int x0, int x1, int y0, int y1)
        {
            for (int x = x0 - MuroGrosor; x <= x1 + MuroGrosor; x++)
            {
                if (CellGrid.InBounds(x, y0 - 1)) grid.SetCell(x, y0 - 1, MaterialId.Stone);
            }
            for (int y = y0 - 1; y <= y1; y++)
            {
                if (CellGrid.InBounds(x0 - MuroGrosor, y)) grid.SetCell(x0 - MuroGrosor, y, MaterialId.Stone);
                if (CellGrid.InBounds(x1 + MuroGrosor, y)) grid.SetCell(x1 + MuroGrosor, y, MaterialId.Stone);
            }
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Empty);
        }

        /// <summary>Talla la mampostería propia del crisol EN CALIENTE (ver DECISIÓN en el doc de la clase, y la nota de playtest 26 sobre por qué sigue viva): muros y suelo de Piedra de 1 celda alrededor de cubeta y tolva, interior vaciado a Empty. Solo la llama ya <see cref="Reposicionar"/> (Mudanza) -- Init ya NO la llama, ver el docblock de la clase.</summary>
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

            // Contrato §1.5: sondeo de proximidad+material cada ~0.25s (acumulador
            // propio de AffordanceGlow, ver MaquinariaSprites.cs) -- NUNCA por frame.
            _glowEmbudo.Sondear(Time.deltaTime, _centroCubeta, _player, _flask, _sirveEmbudo);
            _glowBrasero.Sondear(Time.deltaTime, _centroTolva, _player, _flask, _sirveBrasero);
            if (_afordanceEmbudo != null) _afordanceEmbudo.color = new Color(UiStyles.Exito.r, UiStyles.Exito.g, UiStyles.Exito.b, _glowEmbudo.Alfa);
            if (_afordanceBrasero != null) _afordanceBrasero.color = new Color(UiStyles.Exito.r, UiStyles.Exito.g, UiStyles.Exito.b, _glowBrasero.Alfa);

            AnimarHumo(); // contrato §1.4: bocanadas SOLO mientras arde combustible.
        }

        /// <summary>Contrato §1.4: la CHIMENEA suelta bocanadas de humo SOLO cuando el Crisol quema combustible (el estado del aparato, contado por el cuerpo, no por texto). Tres volutas en fases distintas de un ciclo de <see cref="HumoCicloSeg"/>, cada una sube y se desvanece -- pura aritmética sobre Time.time, cero allocs (los SpriteRenderer ya existen, solo se les mueve la posición/alfa).</summary>
        private void AnimarHumo()
        {
            bool ardiendo = _fuelMatActivo != MaterialId.Empty;
            for (int i = 0; i < HumoPuffs; i++)
            {
                var sr = _humo[i];
                if (sr == null) continue;
                if (!ardiendo) { sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f); continue; }

                float fase = Mathf.Repeat(Time.time / HumoCicloSeg + (i / (float)HumoPuffs), 1f);
                float subida = fase * (SimRenderer.CellWorldSize * 6f);
                float deriva = Mathf.Sin(fase * Mathf.PI * 2f + i) * (SimRenderer.CellWorldSize * 0.6f);
                float escala = 0.6f + fase * 1.1f; // la voluta crece al subir.
                float alfa = (1f - fase) * 0.75f; // se desvanece al alejarse.

                sr.transform.position = _humoOrigen + new Vector3(deriva, subida, 0f);
                sr.transform.localScale = Vector3.one * escala * (SimRenderer.CellWorldSize * 3f) / sr.sprite.rect.width;
                sr.color = new Color(210f / 255f, 205f / 255f, 200f / 255f, alfa);
            }
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
        // VISUAL (playtest 26, contrato §1 — LA GRAMÁTICA): la cubeta lleva
        // EMBUDO (§1.1, boca de materia) + MARCO (§1.3, "aquí queda el
        // resultado") + CHIMENEA con HUMO (§1.4, el verbo en el cuerpo); la
        // tolva deja el chasis de HeatPlate por BRASERO (§1.2, "otra forma,
        // otra altura, otro color" — jamás se confunde con un embudo). Las
        // resistencias (rescoldo/brasas) SIGUEN reutilizando
        // ResistenciasPlaca — el brillo cálido animado que ya funcionaba,
        // solo que ahora asoma dentro de la silueta de cesto en vez de la
        // placa rectangular.
        // -----------------------------------------------------------------
        private void BuildVisual()
        {
            float celda = SimRenderer.CellWorldSize;

            int spanCubeta = _cubX1 - _cubX0 + 1;
            float anchoCubeta = spanCubeta * celda;
            float altoCubeta = (CubetaAlto + 1) * celda; // + el muro/suelo de 1 celda.

            int spanTolva = _tolX1 - _tolX0 + 1;
            float anchoTolva = spanTolva * celda;
            float altoTolva = (TolvaAlto + 1) * celda;

            var cubetaGo = new GameObject("CrisolCubetaChasis");
            cubetaGo.transform.SetParent(transform, false);
            cubetaGo.transform.position = _centroCubeta;
            _resalte = MaquinariaSprites.CrearCapa(cubetaGo.transform, "Resalte", MaquinariaSprites.ChasisPlaca(spanCubeta), 16,
                anchoCubeta * 1.15f, altoCubeta * 1.35f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);
            // Affordance glow del embudo (§1.5): halo MÁS GRANDE que el resalte de
            // foco y detrás de él (orden 14 < 16), tintado de UiStyles.Exito -- dos
            // señales distintas, dos anillos concéntricos distintos.
            _afordanceEmbudo = MaquinariaSprites.CrearCapa(cubetaGo.transform, "AfordanceEmbudo", MaquinariaSprites.Embudo(spanCubeta), 14,
                anchoCubeta * 1.4f, altoCubeta * 1.6f);
            _afordanceEmbudo.color = new Color(UiStyles.Exito.r, UiStyles.Exito.g, UiStyles.Exito.b, 0f);
            MaquinariaSprites.CrearCapa(cubetaGo.transform, "Chasis", MaquinariaSprites.ChasisPlaca(spanCubeta), 18, anchoCubeta, altoCubeta);
            _resistenciasCubeta = MaquinariaSprites.CrearCapa(cubetaGo.transform, "Rescoldo", MaquinariaSprites.ResistenciasPlaca(spanCubeta), 19,
                anchoCubeta, altoCubeta);
            // Marco de latón (§1.3, "cubeta enmarcada"): overlay por encima del
            // chasis, sin cubrir el interior (transparente salvo el borde).
            MaquinariaSprites.CrearCapa(cubetaGo.transform, "MarcoCubeta", MaquinariaSprites.MarcoContenedor(spanCubeta), 21, anchoCubeta, altoCubeta);
            // Embudo funcional (§1.1): montado ARRIBA de la cámara de trabajo.
            var embudoGo = new GameObject("Embudo");
            embudoGo.transform.SetParent(cubetaGo.transform, false);
            embudoGo.transform.localPosition = new Vector3(0f, altoCubeta * 0.5f + celda * 1.2f, 0f);
            MaquinariaSprites.CrearCapa(embudoGo.transform, "Sprite", MaquinariaSprites.Embudo(spanCubeta), 20, anchoCubeta * 0.8f, celda * 2.4f);

            // Chimenea (§1.4): tubo montado sobre el hombro derecho del chasis de
            // la cubeta, con las bocanadas de humo saliendo por su boca.
            var chimeneaGo = new GameObject("Chimenea");
            chimeneaGo.transform.SetParent(cubetaGo.transform, false);
            float chimeneaOffsetX = anchoCubeta * 0.32f;
            float chimeneaAlto = celda * 3.2f;
            chimeneaGo.transform.localPosition = new Vector3(chimeneaOffsetX, altoCubeta * 0.5f + chimeneaAlto * 0.5f, 0f);
            MaquinariaSprites.CrearCapa(chimeneaGo.transform, "Sprite", MaquinariaSprites.Chimenea(2), 22, celda * 1.4f, chimeneaAlto);
            _humoOrigen = _centroCubeta + new Vector3(chimeneaOffsetX, altoCubeta * 0.5f + chimeneaAlto, 0f);
            for (int i = 0; i < HumoPuffs; i++)
            {
                var humoGo = new GameObject("Humo" + i);
                humoGo.transform.SetParent(transform, false); // fuera de cubetaGo: AnimarHumo mueve su POSICIÓN MUNDO directamente cada frame.
                var sr = MaquinariaSprites.CrearCapa(humoGo.transform, "Sprite", MaquinariaSprites.Humo(), 23, celda * 1.8f, celda * 1.8f);
                sr.color = new Color(0.82f, 0.80f, 0.78f, 0f);
                _humo[i] = sr;
            }

            var tolvaGo = new GameObject("CrisolTolvaChasis");
            tolvaGo.transform.SetParent(transform, false);
            tolvaGo.transform.position = _centroTolva;
            _afordanceBrasero = MaquinariaSprites.CrearCapa(tolvaGo.transform, "AfordanceBrasero", MaquinariaSprites.Brasero(spanTolva), 14,
                anchoTolva * 1.4f, altoTolva * 1.6f);
            _afordanceBrasero.color = new Color(UiStyles.Exito.r, UiStyles.Exito.g, UiStyles.Exito.b, 0f);
            MaquinariaSprites.CrearCapa(tolvaGo.transform, "Chasis", MaquinariaSprites.Brasero(spanTolva), 18, anchoTolva, altoTolva);
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
