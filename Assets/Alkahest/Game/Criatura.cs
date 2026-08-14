using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// [ChaosAlchemy · pivot playtest 21] EL RESCOLDO — la criatura del
    /// laboratorio íntimo. Ficción (decidida por Cesar): estaba ahí, dormida
    /// en la roca; nadie te la dio. El juego empieza con ella desmayada de
    /// hambre.
    ///
    /// SU CARNE ES VIVIUM DE VERDAD (no un sprite entero): un CORAZÓN
    /// generado (un bulbo, ver <see cref="SerSprites.MascaraCorazon"/>)
    /// anclado en su cuna, y un pequeño sembrado real de
    /// <see cref="MaterialId.Vivium"/> en la simulación (<see cref="SembrarCuerpoInicial"/>)
    /// que el sistema de crecimiento YA EXISTENTE (SimStepper.GrowthTick,
    /// Sim/, NO TOCADO por este encargo) hace prosperar solo cuando hay
    /// Nutrient cerca y la temperatura cae en Universe.VivGrowMinRaw..MaxRaw.
    /// Esta clase NO simula el crecimiento: lo SIEMBRA una vez y luego
    /// SONDEA el resultado (ver <see cref="SondearComida"/>) para bajar el
    /// hambre cuando detecta que el cuerpo ha crecido de verdad. Se eligió
    /// esta opción (frente a "convertir Nutrient a Vivium a mano con
    /// PaintStable cada vez que come") porque así el crecimiento real
    /// respeta el hábito dendrítico/mata/disperso que ya decide
    /// SimStepper.VivGrowthModeFor por la firma de esta semilla: la forma en
    /// la que el cuerpo del Rescoldo se expande YA varía por universo sin
    /// que este archivo tenga que saber nada de esa lógica.
    ///
    /// SU PIEL SALE DE LA SEMILLA: la máscara del corazón se pasa a
    /// <see cref="FirmaVisualFabrica.GenerarPixeles"/> (Game/StorageRack.cs,
    /// internal en este ensamblado — mismo patrón de uso que
    /// StorageRack.ObtenerFirmaSprites/ActualizarAnimacionContenidos: los
    /// fotogramas se generan UNA vez y se cachean, Update solo alterna cuál
    /// se muestra) con el MaterialDef real de Vivium de este universo, así
    /// que cada partida tiene un corazón con un color/patrón/borde propios,
    /// gratis.
    ///
    /// CUATRO ESTADOS (<see cref="Estado"/>), NINGUNO ES UNA BARRA NI UN
    /// NÚMERO: se leen en TRES canales nada más (tope de scope deliberado):
    ///   1) LATIDO — transform.localScale pulsando (ver <see cref="ActualizarLatido"/>);
    ///      el RITMO (frecuencia/amplitud) comunica el estado. Es el canal
    ///      más importante: un latido creíble es el 80% de "esto está vivo".
    ///   2) COLOR — SpriteRenderer.color, vía un cruce de opacidad entre dos
    ///      capas pre-horneadas (Vivo/Dormida, esta última una réplica
    ///      exacta del criterio de desaturación que SimRenderer YA aplica al
    ///      Vivium dormido — ver <see cref="SerSprites.Desaturar"/>). Nunca
    ///      se regenera una textura en Update.
    ///   3) ZARCILLOS — 4 filamentos fisicamente anclados al contorno del
    ///      corazón que se mecen y, si el aprendiz se acerca, se orientan
    ///      hacia él (ver <see cref="ActualizarZarcillos"/>).
    ///
    /// HALO: <see cref="SerSprites.HaloCalido"/>/<see cref="SerSprites.HaloFrio"/>,
    /// sortingOrder 100+ (por encima de toda la maquinaria del taller, que
    /// llega a ~60), cruzados en alpha y escalados según "vitalidad" — ver
    /// <see cref="ActualizarHalo"/>, llamado desde LateUpdate a petición
    /// explícita del encargo ("siguiendo al corazón").
    ///
    /// DIGERIR (decisión de Cesar): ver <see cref="SondearDigestionYAmenaza"/>
    /// y <see cref="CompletarDigestion"/>. El producto lo decide
    /// <see cref="EscogerProductoDigestion"/> en TRES escalones sobre la
    /// química real de esta semilla -- <see cref="Universe.Leyes"/> primero,
    /// <see cref="Universe.AfinidadDelUniverso"/> después, el Edicto como
    /// último recurso -- documentados en detalle en el docblock de ese
    /// método. (Nota histórica: la primera entrega de este encargo se
    /// escribió contra un sandbox que había quedado revertido cinco rondas
    /// sin que nadie lo supiera, así que concluyó por grep que esa API no
    /// existía. Sí existe -- se construyó en el playtest 18 -- y esta
    /// versión ya la usa.)
    ///
    /// LA DIGESTIÓN ES UN ACTO, NO UN MOTOR (playtest 21, fix crítico --
    /// "el Rescoldo se come la habitación", ver <see cref="_ultimoProductoDigestion"/>
    /// para el diagnóstico trazado con números y la corrección completa):
    /// nunca vuelve a contar como comida lo último que ella misma exudó, así
    /// que repetir el ciclo exige que el jugador traiga algo genuinamente
    /// distinto. El techo de <see cref="TallaMaxCeldas"/> (ver ese bloque
    /// CONFIG) es un segundo cinturón de seguridad sobre el CUERPO, pedido
    /// explícito de Cesar aunque no era la causa del bulto real.
    ///
    /// SE CALIENTA A SÍ MISMA (playtest 21, fix crítico -- ver
    /// <see cref="ApplyCalorTick"/> para el porqué completo, las cifras
    /// medidas y el diseño): el HALO cálido de arriba no es decoración, es
    /// calor de verdad -- esta clase empuja temperatura real a
    /// AlkahestSim.Grid, más lejos y más fuerte cuanto más Contenta está, lo
    /// justo para mantenerse dentro de Universe.VivGrowMinRaw..MaxRaw de ESTA
    /// semilla sin depender de que haya una placa térmica cerca (la sala
    /// íntima no tiene ninguna a propósito). Esto es también lo que hace
    /// físico el vínculo con <see cref="Capullo"/> (sin cambios en ese
    /// archivo): "alimentas -> se pone contenta -> calienta más -> el
    /// capullo cercano avanza" sale solo de que el capullo ya leía
    /// temperatura real de su propia celda.
    /// </summary>
    public sealed class Criatura : MonoBehaviour
    {
        /// <summary>La criatura PRINCIPAL de la partida (la que NO nació de un capullo), o null. La usa Capullo para colocar a la cría cerca de su progenitor.</summary>
        public static Criatura Principal { get; private set; }

        private enum Estado { Hambrienta, Contenta, Aletargada, Asustada }

        // -----------------------------------------------------------------
        // CONFIG — tamaño/mundo
        // -----------------------------------------------------------------
        private const float AnchoMundoCorazon = 1.0f;
        private const float AltoMundoCorazon = AnchoMundoCorazon * SerSprites.CorazonH / SerSprites.CorazonW;
        // (playtest 21, SEGUNDA pasada: "más finos, más cortos" -- ver
        // ActualizarZarcillos/AnclasFrac para el resto del arreglo) bajados
        // de 0.085/0.62 -- a esta escala, junto con AnclasFrac corregido más
        // abajo y baseW de SerSprites.MascaraZarcillo ya afinado, el brote
        // deja de leerse como "asta de ciervo".
        private const float AnchoMundoZarcillo = 0.060f;
        private const float AltoMundoZarcillo = 0.52f;
        private const float EscalaCria = 0.6f; // "más pequeña" (contrato): la cría es un 60% del tamaño adulto en TODO (corazón+zarcillos+halo, escalados juntos vía Transform.localScale del root).

        private const int NumZarcillos = 4;
        // Magnitud del ángulo de reposo (grados desde "arriba") por zarcillo
        // -- 4 VALORES DISTINTOS (playtest 21, corrección de arte: "la
        // simetría perfecta mata lo orgánico"), no 2 alturas espejadas.
        private static readonly float[] AngulosBaseAbs = { 16f, 46f, 22f, 38f };
        // Punto de anclaje de cada zarcillo en fracción del cuerpo (x,y desde
        // el centro, +y=arriba) -- las 4 alturas son DISTINTAS entre sí
        // (antes eran 2 alturas espejadas por lado), sobre el perfil
        // semiancho-por-fila real de SerSprites.MascaraCorazon (t=0.55/0.60/
        // 0.68/0.74 de la altura -- la banda de "hombros" del bulbo, entre la
        // panza y la cúpula).
        //
        // SEGUNDA PASADA (playtest 21, "despegados del cuerpo... un hueco
        // entre ellas y el bulbo"): la primera pasada puso estos valores "a
        // ojo" en el prototipo -- demasiado CERCA del centro (0.29-0.34)
        // frente al borde real del cuerpo en esas filas. Como el Filamento
        // se dibuja DETRÁS del corazón (sortingOrder 44 &lt; 45/46 del
        // corazón), el tramo del zarcillo entre su ancla y el borde real de
        // la silueta queda OCULTO tras el bulbo -- con el ancla tan metida
        // hacia el centro, ese tramo oculto era largo, y al girar el ángulo
        // de reposo (Contenta gira poco, casi vertical) el punto donde el
        // brote por fin asoma por el contorno cae lejos del cuerpo: eso es
        // el "hueco", no la máscara en sí. Recalculado con la fórmula real de
        // semiancho-por-fila de MascaraCorazon evaluada en cada t (ver
        // /tmp/ser_sprites_proto.py del informe para la cuenta):
        //   t=0.55 -> semiBase/w=0.483   t=0.74 -> semiBase/w=0.408
        //   t=0.60 -> semiBase/w=0.463   t=0.68 -> semiBase/w=0.429
        // Anclas puestas al 90% de esos bordes reales (margen de seguridad
        // hacia DENTRO, nunca hacia fuera: la asimetría/ruido de la semilla
        // mueve el borde real ±10-16% de un lado a otro, y un ancla que caiga
        // fuera del borde en la semilla más estrecha reintroduciría el mismo
        // hueco) -- así el brote nace ya solapado con el contorno en vez de
        // enterrado muy adentro.
        private static readonly Vector2[] AnclasFrac =
        {
            new Vector2(-0.43f, 0.05f), new Vector2(-0.37f, 0.24f),
            new Vector2(0.42f, 0.10f), new Vector2(0.39f, 0.18f),
        };
        // Longitud INTRÍNSECA de cada zarcillo (multiplica el escalar de
        // longitud por estado, ver ActualizarZarcillos) -- 3-4 magnitudes
        // distintas (pedido explícito), no 4 copias idénticas.
        private static readonly float[] LongitudBaseFrac = { 0.72f, 1.05f, 0.86f, 1.20f };

        // -----------------------------------------------------------------
        // CONFIG — hambre / comida
        // -----------------------------------------------------------------
        private const float IntervaloSondeo = 0.4f; // comida/digestión/amenaza: sondeadas, NUNCA escaneadas cada frame.
        private const float TasaHambrePorSeg = 1f / 70f;
        private const float UmbralHambrienta = 0.5f;
        private const float ComioFactorPorCelda = 0.05f;
        private const int RadioSondeoComidaCeldas = 16;
        private const int AlturaCuerpoCeldas = 2; // celdas por encima del suelo de la cuna donde vive el cuerpo/corazón.

        // -----------------------------------------------------------------
        // CONFIG — TALLA del cuerpo (playtest 21, fix crítico -- "el
        // Rescoldo se come la habitación", ver el informe de la ronda). NO
        // era la causa real del bulto (la causa real vivía en digestión, ver
        // el bloque CONFIG — digestión más abajo) pero Cesar lo pidió
        // explícito de todos modos: "una criatura que crece sin límite no es
        // una criatura, es moho". Crecer tiene que significar "está más
        // sana", no "ocupa más sitio" -- por eso el objetivo de celdas
        // depende de <see cref="_saludPromedio"/> (media móvil LENTA de
        // 1-hambre, nunca el hambre instantáneo: un solo bocado no debe
        // disparar ni podar el cuerpo de golpe) y nunca de cuánto Nutrient
        // haya alrededor. Aplicado en <see cref="SondearComida"/> vía
        // <see cref="PodarExcesoCuerpo"/>.
        // -----------------------------------------------------------------
        private const int TallaMinCeldas = 14; // apenas por encima del disco inicial (radio 2 -> 13 celdas, ver SembrarCuerpoInicial): una criatura recién sembrada u hambrienta no se poda de golpe.
        private const int TallaMaxCeldas = 40; // techo "sana": un disco de 40 celdas mide ~7 de diámetro -- de sobra pequeño para la cuna (~20 celdas de ancho, contrato) y deja ver el capullo al lado.
        private const float TallaSuavizadoSeg = 45f; // la media móvil tarda ~45s en seguir un salto de salud completo -- crecer/podar es un proceso lento y perceptible, no un salto en el primer sondeo.

        // -----------------------------------------------------------------
        // CONFIG — digestión (solo si !esCria)
        // -----------------------------------------------------------------
        private const int RadioSondeoDigestionCeldas = 6;
        private const int UmbralCeldasDigestion = 6;
        private const float DigestionDuracionSeg = 4.5f;
        private const int DigestionMaxCeldasConsumidas = 40;
        private const int DigestionRadioProducto = 2;
        private const int DigestionOffsetProductoCeldas = 9;

        // -----------------------------------------------------------------
        // CONFIG — amenaza (Asustada)
        // -----------------------------------------------------------------
        private const int RadioAmenazaCeldas = 10;

        // -----------------------------------------------------------------
        // CONFIG — AUTOCALENTAMIENTO ("EL RESCOLDO SE CALIENTA A SÍ MISMO",
        // playtest 21, fix crítico -- ver docblock de <see cref="ApplyCalorTick"/>
        // para el porqué completo y las cifras que lo respaldan).
        // -----------------------------------------------------------------
        private const float TickDtCalor = 1f / 30f;
        private const int MaxStepsPerFrameCalor = 2;
        private const int TempStepPerTickCalor = 4;

        /// <summary>
        /// Techo absoluto de seguridad ("acota siempre por arriba", pedido
        /// explícito): el Vivium hierve a CellGrid.CToRaw(120)=120raw y arde a
        /// CellGrid.CToRaw(150)=135raw (Sim/Universe.cs, MaterialDef real de
        /// Vivium -- estas cifras coinciden EXACTAS con las que dio Cesar, no
        /// son inventadas aquí). La criatura JAMÁS empuja por encima de este
        /// valor, pase lo que pase con la banda de la semilla: ni siquiera el
        /// peor caso posible (growMaxC=75°C, shift=+15 sin Frío Fértil ->
        /// CellGrid.CToRaw(75)≈98raw) se acerca, así que este techo es un
        /// cinturón de seguridad, no algo que la banda normal vaya a rozar
        /// nunca.
        /// </summary>
        private const byte TechoSeguridadRaw = 110;

        /// <summary>
        /// Perfil de empuje por distancia Chebyshev (celdas) al centro del
        /// cuerpo -- índice = distancia, valor = % de
        /// <see cref="TempStepPerTickCalor"/>. Verificado NUMÉRICAMENTE antes
        /// de escribir esto (réplica exacta en Python del algoritmo de
        /// SimStepper.DiffuseTemperature, NUNCA modificado -- regla 9 de
        /// CLAUDE.md): un radio de empuje pequeño (~4 celdas, el tamaño del
        /// propio cuerpo) NO calienta nada más allá de esas 4 celdas por
        /// difusión sola, ni siquiera esperando 30 minutos simulados -- el
        /// "barrido de ambiente" de DiffuseTemperature tira de cada celda de
        /// vuelta hacia CellGrid.AmbientRaw de forma constante e independiente
        /// del tiempo transcurrido, así que crea un apantallamiento duro.
        /// Conclusión medida: para que el calor llegue de verdad a las 10-14
        /// celdas donde el otro encargo coloca el capullo hermano, el empuje
        /// tiene que EXTENDERSE activamente hasta ahí -- no basta con esperar
        /// a que difunda solo desde un núcleo pequeño. Con este perfil (nunca
        /// cae del 35%) y TempStepPerTickCalor=4, las distancias 10/12/14
        /// alcanzan y MANTIENEN estable el objetivo en menos de un minuto
        /// simulado incluso en el peor caso de banda de la semilla
        /// (growMinC=45°C -> 82raw, la banda más alta posible sin Frío
        /// Fértil).
        /// </summary>
        private static readonly int[] PerfilCalorPct =
        {
            100, 95, 90, 85, 80, 75, 70, 65, 60, 55, 50, 45, 40, 35, 35,
        };

        /// <summary>Alcance PLENO (celdas): solo Contenta lo usa entero -- cubre las 10-14 celdas donde el otro encargo coloca el capullo (verificado, ver <see cref="PerfilCalorPct"/>).</summary>
        private const int RadioCalorPleno = 14;
        /// <summary>Alcance de recuperación (Aletargada: intentando volver a la banda tras enfriarse de verdad, p.ej. por una fuente de frío externa).</summary>
        private const int RadioCalorRecuperacion = 7;
        /// <summary>
        /// Alcance NÚCLEO: SIEMPRE activo, pase lo que pase (Hambrienta,
        /// Asustada) -- el "rescoldo mínimo" del que nunca se puede caer del
        /// todo. Garantiza que la propia celda de la criatura y las
        /// inmediatas se mantengan en el borde bajo de SU banda de
        /// crecimiento aunque esté hambrienta o asustada, así que siempre
        /// puede recuperarse sola en cuanto vuelva a comer -- nunca un estado
        /// irreversible de "ya la mataste sin darte cuenta" (pedido
        /// explícito).
        /// </summary>
        private const int RadioCalorNucleo = 4;

        private float _accCalor;

        // -----------------------------------------------------------------
        // Inyectado por Init
        // -----------------------------------------------------------------
        private AlkahestSim _sim;
        private Transform _jugador;
        private bool _esCria;
        private int _celdaCunaX, _celdaCunaY;

        // -----------------------------------------------------------------
        // Visual
        // -----------------------------------------------------------------
        private Transform _pivoteLatido;
        private SpriteRenderer _corazonVivoSr;
        private SpriteRenderer _corazonDormidoSr;
        private Sprite[] _framesVivo;
        private Sprite[] _framesDormido;
        private Texture2D[] _texVivo;
        private Texture2D[] _texDormido;
        private int _frameActual = -1;

        private readonly Transform[] _zarcillos = new Transform[NumZarcillos];
        private readonly SpriteRenderer[] _zarcilloSr = new SpriteRenderer[NumZarcillos];
        private readonly float[] _zarcilloFaseSway = new float[NumZarcillos];
        private readonly float[] _zarcilloLongitudActual = new float[NumZarcillos];
        private Texture2D _texZarcillo;

        private Transform _haloRoot;
        private SpriteRenderer _haloCalidoSr;
        private SpriteRenderer _haloFrioSr;

        // -----------------------------------------------------------------
        // Estado runtime
        // -----------------------------------------------------------------
        private Estado _estado = Estado.Hambrienta;
        private float _hambre = 0.8f; // arranca CASI hambrienta (ficción: desmayada de hambre), no al 100% para que el jugador tenga un pelín de margen antes del primer aviso visual.
        private float _faseLatido;
        private float _freqLatidoActual = 0.5f;
        private float _ampLatidoActual = 0.03f;
        private float _escalaBaseActual = 1f; // encoge el cuerpo entero en Asustada -- ver ActualizarLatido.
        private float _vitalidadActual = 0.35f;
        private float _pulsoExtra; // 0..1, decae -- bump momentáneo (comer/exudar).

        private float _haloRadioActual = 1f, _haloCalidezActual = 0.3f, _haloIntensidadActual = 0.5f;

        private float _accPoll;
        private int _ultimoConteoVivium = -1;
        private float _saludPromedio; // media móvil de (1-hambre), mueve TallaMinCeldas..MaxCeldas -- ver el bloque CONFIG — TALLA.

        private bool _amenazaCerca;
        private Vector2 _direccionAmenazaNorm = Vector2.up;

        private bool _digestionHabilitada;
        private bool _digestionEnCurso;
        private byte _digestionMatEntrada;
        private float _digestionTimer;
        private readonly int[] _conteoDigestion = new int[MaterialId.Count];

        /// <summary>
        /// EL FIX CRÍTICO (playtest 21 -- "el Rescoldo se come la
        /// habitación", ver el informe completo). Lo último que ESTA
        /// criatura exudó (<see cref="MaterialId.Empty"/> si aún no ha
        /// digerido nada). Se excluye SIEMPRE del sondeo de digestión (ver
        /// <see cref="SondearDigestionYAmenaza"/>) y, si resulta ser
        /// <see cref="MaterialId.Acid"/>, también del sondeo de amenaza.
        ///
        /// DIAGNÓSTICO CONFIRMADO (no solo plausible -- se trazó el sondeo
        /// con los números reales de este archivo): el producto se pintaba a
        /// <see cref="DigestionOffsetProductoCeldas"/>=9 celdas del cuerpo,
        /// FUERA del radio de sondeo <see cref="RadioSondeoDigestionCeldas"/>=6
        /// -- así que NO se autodigería en el mismo tick, como pretendía el
        /// comentario original de <see cref="CompletarDigestion"/>. Pero el
        /// producto es materia SIMULADA de verdad (Liquid/Gas reales, no un
        /// sprite) que la propia física del mundo (SimStepper, Sim/, no
        /// tocado) esparce por gravedad/difusión -- en un cuarto CERRADO de
        /// ~20 celdas de ancho, una fracción de esa materia vuelve a entrar
        /// en el radio de 6 celdas en segundos, el sondeo (cada
        /// <see cref="IntervaloSondeo"/>=0.4s, para siempre) la cuenta como
        /// "comida nueva" y rearma la digestión ELLA SOLA. Cada ciclo completo
        /// (hasta 4.9s: 4.5s de digestión + hasta 0.4s de sondeo) PINTA otro
        /// disco de producto sin que el jugador haga nada -- un motor
        /// perpetuo, no el ACTO que pide Cesar, y en un par de minutos (~24
        /// ciclos) eso es un cuarto entero de materia. El caso concreto que
        /// se vio en pantalla encaja exacto: <see cref="MaterialId.Acid"/> es
        /// Liquid (se esparce solo, Sim/Universe.cs línea ~607) y su
        /// baseColor real es (182,204,46) -- amarillo-verdoso, el mismo tono
        /// del bulto reportado -- y está en <see cref="CandidatosDigestion"/>
        /// y en el pool de <see cref="Edicto.MateriaIrascible"/>, así que es
        /// un producto plausible en cualquier semilla que caiga en ese
        /// Edicto o cuya química converja en Acid por afinidad.
        ///
        /// LA HIPÓTESIS DEL CRECIMIENTO DEL CUERPO SE DESCARTÓ CON NÚMEROS,
        /// no solo se dio por buena (regla 30): GrowthTick (Sim/SimStepper.cs)
        /// solo engendra Vivium nuevo consumiendo un Nutrient ORTOGONALMENTE
        /// ADYACENTE, y el montón de Nutrient de esta sala son 16 celdas
        /// fijas (SimLevelBuilder.PlaceNutrienteMound, 4x4, encargo A) que
        /// NADIE repone -- así que el cuerpo NUNCA podía superar
        /// ~13 (disco inicial) + 16 (todo el montón consumido) ≈ 29 celdas,
        /// muy lejos de "domina el encuadre". El techo de TALLA de más abajo
        /// se añade de todos modos porque Cesar lo pidió explícito, no
        /// porque fuera la causa.
        ///
        /// EL FIX: la digestión es un ACTO, no un motor -- exige comida
        /// GENUINAMENTE DISTINTA de lo último que la propia criatura exudó
        /// para poder repetirse. Un único campo basta (no hace falta
        /// recordar un historial): mientras el producto anterior siga siendo
        /// lo único no-Nutrient/Vivium alrededor, <see cref="_conteoDigestion"/>
        /// nunca llega a <see cref="UmbralCeldasDigestion"/> y no se rearma.
        /// En cuanto el jugador echa algo REALMENTE distinto, digiere, y el
        /// campo se actualiza a ESE producto -- así que un vaivén A→B→A→B
        /// tampoco puede sostenerse solo: cada ciclo excluye exactamente lo
        /// que él mismo acaba de exudar, nunca lo que exudó dos ciclos atrás.
        /// </summary>
        private byte _ultimoProductoDigestion = MaterialId.Empty;

        // ===================================================================
        // API pública (firma CONGELADA por CONTRATO_PIVOT.md)
        // ===================================================================

        /// <summary>celdaCunaX/Y = celda de SUELO sobre la que se asienta. esCria = true para la que sale del capullo (más pequeña, sin digestión todavía).</summary>
        public void Init(AlkahestSim sim, Transform jugador, int celdaCunaX, int celdaCunaY, bool esCria = false)
        {
            _sim = sim;
            _jugador = jugador;
            _celdaCunaX = celdaCunaX;
            _celdaCunaY = celdaCunaY;
            _esCria = esCria;
            _digestionHabilitada = !esCria;

            if (!esCria) Principal = this;

            BuildVisuals();
            SembrarCuerpoInicial();
        }

        private void OnDestroy()
        {
            if (Principal == this) Principal = null;
            LiberarTexturas();
        }

        // ===================================================================
        // Construcción visual (UNA vez; Update solo mueve/tiñe/alterna sprite).
        // ===================================================================
        private void BuildVisuals()
        {
            float celda = SimRenderer.CellWorldSize;
            float baseX = (_celdaCunaX + 0.5f) * celda;
            float baseY = (_celdaCunaY + 1) * celda; // encima del suelo de la cuna.

            transform.position = new Vector3(baseX, baseY, 0f);
            transform.localScale = _esCria ? Vector3.one * EscalaCria : Vector3.one;

            var pivoteGo = new GameObject("PivoteLatido");
            _pivoteLatido = pivoteGo.transform;
            _pivoteLatido.SetParent(transform, false);
            _pivoteLatido.localPosition = new Vector3(0f, AltoMundoCorazon * 0.5f, 0f);

            BuildCorazon();
            BuildZarcillos();
            BuildHalo();
        }

        private void BuildCorazon()
        {
            int w = SerSprites.CorazonW, h = SerSprites.CorazonH;
            int seedSilueta = _sim != null ? _sim.Universe.Seed + (_esCria ? 555 : 0) : 91;
            byte[] mask = SerSprites.MascaraCorazon(w, h, seedSilueta);
            bool[] esBorde = SerSprites.CalcularBorde(mask, w, h, 2);

            var def = _sim.Universe.Get(MaterialId.Vivium);
            int frames = def.ritmoAnim > 0 ? FirmaVisualFabrica.AnimFrames : 1;
            _framesVivo = new Sprite[frames];
            _framesDormido = new Sprite[frames];
            _texVivo = new Texture2D[frames];
            _texDormido = new Texture2D[frames];

            for (int f = 0; f < frames; f++)
            {
                var px = FirmaVisualFabrica.GenerarPixeles(w, h, def, f, mask, esBorde, sobreMundo: true);
                // Playtest 21, corrección de arte: FUERA el estallido radial
                // (AplicarNucleoCalido, "pelota de playa") -- el interior
                // viene ENTERO de FirmaVisualFabrica salvo por la BRASA (un
                // núcleo pequeño y descentrado, "si solo haces una cosa de
                // esta lista, haz esta") y el VOLUMEN (luz arriba/sombra
                // abajo, "ahora es plano").
                SerSprites.AplicarBrasa(px, w, h, mask, seedSilueta);
                SerSprites.AplicarVolumen(px, w, h, mask);
                _framesVivo[f] = SerSprites.CrearSprite(px, w, h, new Vector2(0.5f, 0.5f),
                    "ChaosAlchemyCorazonVivo_" + f, out _texVivo[f]);

                var pxDormido = SerSprites.Desaturar(px, 0.55f);
                _framesDormido[f] = SerSprites.CrearSprite(pxDormido, w, h, new Vector2(0.5f, 0.5f),
                    "ChaosAlchemyCorazonDormido_" + f, out _texDormido[f]);
            }

            _corazonVivoSr = MaquinariaSprites.CrearCapa(_pivoteLatido, "CorazonVivo", _framesVivo[0], 45, AnchoMundoCorazon, AltoMundoCorazon);
            _corazonDormidoSr = MaquinariaSprites.CrearCapa(_pivoteLatido, "CorazonDormido", _framesDormido[0], 46, AnchoMundoCorazon, AltoMundoCorazon);
            _corazonDormidoSr.color = new Color(1f, 1f, 1f, 1f - _vitalidadActual);
        }

        private void BuildZarcillos()
        {
            int w = SerSprites.ZarcilloW, h = SerSprites.ZarcilloH;
            byte[] mask = SerSprites.MascaraZarcillo(w, h);
            Sprite sprZarcillo = SerSprites.SpriteDeMascara(mask, w, h, new Vector2(0.5f, 0f), "ChaosAlchemyZarcillo", out _texZarcillo);

            for (int i = 0; i < NumZarcillos; i++)
            {
                var ancla = new GameObject("Zarcillo_" + i);
                ancla.transform.SetParent(_pivoteLatido, false);
                ancla.transform.localPosition = new Vector3(
                    AnclasFrac[i].x * AnchoMundoCorazon, AnclasFrac[i].y * AltoMundoCorazon, 0f);

                var sr = MaquinariaSprites.CrearCapa(ancla.transform, "Filamento", sprZarcillo, 44, AnchoMundoZarcillo, AltoMundoZarcillo);
                sr.transform.localPosition = Vector3.zero;

                _zarcillos[i] = ancla.transform;
                _zarcilloSr[i] = sr;
                _zarcilloFaseSway[i] = i * 1.7f; // desfase para que no laten todos a la vez.
                _zarcilloLongitudActual[i] = 1f;
            }
        }

        private void BuildHalo()
        {
            var haloGo = new GameObject("HaloRoot");
            _haloRoot = haloGo.transform;
            _haloRoot.SetParent(transform, false); // NO es hijo de PivoteLatido: el halo no debe latir con el corazón, solo seguirlo (LateUpdate).

            _haloFrioSr = MaquinariaSprites.CrearCapa(_haloRoot, "HaloFrio", SerSprites.HaloFrio(), 100, 1f, 1f);
            _haloCalidoSr = MaquinariaSprites.CrearCapa(_haloRoot, "HaloCalido", SerSprites.HaloCalido(), 101, 1f, 1f);
        }

        private void SembrarCuerpoInicial()
        {
            if (_sim == null) return;
            // "Su carne es Vivium de verdad": una semilla real en la sim,
            // anclada a la cuna. PaintStable (regla 22/29): nace estable, no
            // hereda la temperatura que hubiera antes en esas celdas. A
            // partir de aquí, SimStepper.GrowthTick (Sim/, no tocado) hace
            // el resto en cuanto haya Nutrient cerca y temperatura en banda.
            int y = _celdaCunaY + AlturaCuerpoCeldas;
            _sim.PaintStable(_celdaCunaX, y, _esCria ? 1 : 2, MaterialId.Vivium);
            _ultimoConteoVivium = -1; // fuerza a SondearComida a fijar la línea base en el primer sondeo, sin contar esta siembra como "comida".
        }

        private void LiberarTexturas()
        {
            LiberarArray(_texVivo);
            LiberarArray(_texDormido);
            if (_texZarcillo != null) Destroy(_texZarcillo);
            // Las texturas del halo son estáticas/compartidas (SerSprites) y
            // se generan UNA vez para todo el proceso: nunca se destruyen
            // aquí (mismo criterio que MaquinariaSprites._cache).
        }

        private static void LiberarArray(Texture2D[] texturas)
        {
            if (texturas == null) return;
            for (int i = 0; i < texturas.Length; i++)
                if (texturas[i] != null) Destroy(texturas[i]);
        }

        // ===================================================================
        // Update / LateUpdate
        // ===================================================================
        private void Update()
        {
            if (_sim == null) return;
            float dt = Time.deltaTime;

            _hambre = Mathf.Clamp01(_hambre + TasaHambrePorSeg * dt); // corre siempre, cría incluida.
            _pulsoExtra = Mathf.MoveTowards(_pulsoExtra, 0f, dt / 0.7f);

            _accPoll += dt;
            if (_accPoll >= IntervaloSondeo)
            {
                _accPoll -= IntervaloSondeo;
                SondearComida();
                SondearDigestionYAmenaza();
            }
            if (_digestionEnCurso)
            {
                _digestionTimer -= dt;
                if (_digestionTimer <= 0f) CompletarDigestion();
            }

            ActualizarEstado();
            ActualizarLatido(dt);
            ActualizarColor(dt);
            ActualizarZarcillos(dt);
            ActualizarCalorPropio(dt);
        }

        private void LateUpdate()
        {
            ActualizarHalo(Time.deltaTime);
        }

        // ===================================================================
        // Comida (ver docblock de la clase para el criterio elegido).
        // ===================================================================
        private void SondearComida()
        {
            int cx = _celdaCunaX;
            int cyBase = _celdaCunaY + AlturaCuerpoCeldas;
            int r = RadioSondeoComidaCeldas;
            int conteo = 0;
            for (int dy = -r; dy <= r; dy++)
            {
                int y = cyBase + dy;
                for (int dx = -r; dx <= r; dx++)
                {
                    if (_sim.SampleMaterial(cx + dx, y) == MaterialId.Vivium) conteo++;
                }
            }

            if (_ultimoConteoVivium >= 0 && conteo > _ultimoConteoVivium)
            {
                int comidas = conteo - _ultimoConteoVivium;
                _hambre = Mathf.Clamp01(_hambre - comidas * ComioFactorPorCelda);
                _pulsoExtra = 1f; // el primer estirón visible: un pulso de latido + color, no un número.
            }
            _ultimoConteoVivium = conteo;

            // TALLA (ver bloque CONFIG — TALLA arriba): media móvil lenta de
            // "qué tan bien alimentada está", nunca el hambre instantáneo.
            // Si el cuerpo simulado (SimStepper.GrowthTick, Sim/, no tocado
            // por este encargo) creció por encima del objetivo de ESTA
            // salud, se poda el sobrante -- "crecer" vuelve a significar
            // "más sana", no "ocupa más sitio".
            _saludPromedio = Mathf.MoveTowards(_saludPromedio, 1f - _hambre, IntervaloSondeo / TallaSuavizadoSeg);
            int tallaObjetivo = Mathf.RoundToInt(Mathf.Lerp(TallaMinCeldas, TallaMaxCeldas, _saludPromedio));
            if (conteo > tallaObjetivo) PodarExcesoCuerpo(cx, cyBase, r, conteo - tallaObjetivo);
        }

        /// <summary>
        /// Poda el cuerpo simulado hasta bajar de la TALLA objetivo (ver
        /// SondearComida y el bloque CONFIG — TALLA): recorre anillos
        /// concéntricos de FUERA hacia DENTRO (distancia Chebyshev, mismo
        /// criterio que <see cref="ApplyCalorTick"/>) convirtiendo Vivium
        /// sobrante en Empty vía <see cref="AlkahestSim.Paint"/> -- NUNCA
        /// PaintStable (regla 22/29 al revés: esto BORRA materia que ya
        /// existía, no crea materia de la nada). Podar por fuera primero deja
        /// el cuerpo compacto alrededor de la cuna en vez de agujerearlo por
        /// el centro.
        /// </summary>
        private void PodarExcesoCuerpo(int cx, int cyBase, int radioMax, int exceso)
        {
            for (int radio = radioMax; radio >= 0 && exceso > 0; radio--)
            {
                for (int dy = -radio; dy <= radio && exceso > 0; dy++)
                {
                    int y = cyBase + dy;
                    for (int dx = -radio; dx <= radio && exceso > 0; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radio) continue; // solo el anillo actual -- los radios mayores ya se visitaron.
                        int x = cx + dx;
                        if (_sim.SampleMaterial(x, y) != MaterialId.Vivium) continue;
                        _sim.Paint(x, y, 0, MaterialId.Empty);
                        exceso--;
                    }
                }
            }
        }

        // ===================================================================
        // Digestión + amenaza (un único barrido, sondeado, comparte coste).
        // ===================================================================
        private void SondearDigestionYAmenaza()
        {
            int cx = _celdaCunaX;
            int cyBase = _celdaCunaY + AlturaCuerpoCeldas;

            _amenazaCerca = false;
            Vector2 sumaAmenaza = Vector2.zero;
            int nAmenaza = 0;

            bool medirDigestion = _digestionHabilitada && !_digestionEnCurso;
            if (medirDigestion) System.Array.Clear(_conteoDigestion, 0, _conteoDigestion.Length);

            int r = Mathf.Max(RadioAmenazaCeldas, medirDigestion ? RadioSondeoDigestionCeldas : 0);
            int rAmenaza2 = RadioAmenazaCeldas * RadioAmenazaCeldas;
            int rDig2 = RadioSondeoDigestionCeldas * RadioSondeoDigestionCeldas;

            for (int dy = -r; dy <= r; dy++)
            {
                int y = cyBase + dy;
                for (int dx = -r; dx <= r; dx++)
                {
                    int d2 = dx * dx + dy * dy;
                    int mat = _sim.SampleMaterial(cx + dx, y);

                    // AMENAZA: Fire/Acid cerca asustan -- salvo que el Acid
                    // sea justo lo que la propia criatura acaba de exudar
                    // (ver _ultimoProductoDigestion): si no, un Rescoldo que
                    // digiere algo y produce Acid se asusta de SU PROPIO
                    // producto y queda encerrado en Asustada sin que el
                    // jugador haga nada.
                    if (d2 <= rAmenaza2 && (mat == MaterialId.Fire
                        || (mat == MaterialId.Acid && mat != _ultimoProductoDigestion)))
                    {
                        sumaAmenaza += new Vector2(dx, dy);
                        nAmenaza++;
                    }
                    // DIGESTIÓN: nunca Empty/Stone/Vivium/Nutrient (su propia
                    // comida) NI _ultimoProductoDigestion -- lo último que
                    // ESTA criatura exudó (fix crítico, ver el docblock de
                    // ese campo: sin esta última exclusión, el producto podía
                    // derivar de vuelta a este radio y rearmar la digestión
                    // solo, un motor perpetuo que se come el cuarto).
                    if (medirDigestion && d2 <= rDig2
                        && mat != MaterialId.Empty && mat != MaterialId.Stone
                        && mat != MaterialId.Vivium && mat != MaterialId.Nutrient
                        && mat != _ultimoProductoDigestion
                        && mat < MaterialId.Count)
                    {
                        _conteoDigestion[mat]++;
                    }
                }
            }

            if (nAmenaza > 0)
            {
                _amenazaCerca = true;
                var dir = (sumaAmenaza / nAmenaza);
                _direccionAmenazaNorm = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.up;
            }

            if (medirDigestion)
            {
                byte mejor = MaterialId.Empty;
                int mejorN = 0;
                for (int m = 1; m < _conteoDigestion.Length; m++)
                {
                    if (_conteoDigestion[m] > mejorN) { mejorN = _conteoDigestion[m]; mejor = (byte)m; }
                }
                if (mejorN >= UmbralCeldasDigestion)
                {
                    _digestionEnCurso = true;
                    _digestionMatEntrada = mejor;
                    _digestionTimer = DigestionDuracionSeg;
                }
            }
        }

        private void CompletarDigestion()
        {
            int cx = _celdaCunaX;
            int cy = _celdaCunaY + AlturaCuerpoCeldas;

            ConsumirSustancia(_digestionMatEntrada, cx, cy, RadioSondeoDigestionCeldas, DigestionMaxCeldasConsumidas);

            byte producto = EscogerProductoDigestion(_sim.Universe, _digestionMatEntrada);
            // Se exuda a un lado (no encima de donde se comió): que se note
            // que algo SALE, no que la sustancia original solo desaparece.
            _sim.PaintStable(cx + DigestionOffsetProductoCeldas, cy, DigestionRadioProducto, producto);

            // FIX CRÍTICO (ver el docblock de _ultimoProductoDigestion): a
            // partir de ahora este producto queda excluido del propio
            // sondeo -- la digestión es un ACTO, exige que el jugador traiga
            // algo genuinamente distinto para repetirse, nunca se rearma sola.
            _ultimoProductoDigestion = producto;

            _pulsoExtra = 1f;
            _digestionEnCurso = false;
        }

        /// <summary>Consume, celda a celda (nunca un disco a ciegas -- eso borraría también el cuerpo de Vivium sembrado en el mismo punto), hasta `maxCeldas` de `mat` dentro de un cuadrado de `radio`.</summary>
        private void ConsumirSustancia(byte mat, int cx, int cy, int radio, int maxCeldas)
        {
            int consumidas = 0;
            for (int dy = -radio; dy <= radio && consumidas < maxCeldas; dy++)
            {
                int y = cy + dy;
                for (int dx = -radio; dx <= radio && consumidas < maxCeldas; dx++)
                {
                    int x = cx + dx;
                    if (_sim.SampleMaterial(x, y) != mat) continue;
                    _sim.Paint(x, y, 0, MaterialId.Empty);
                    consumidas++;
                }
            }
        }

        /// <summary>
        /// Elige el producto de la digestión en TRES ESCALONES, coherente con
        /// la química REAL de esta semilla y NUNCA aleatorio (mismo universo
        /// + misma entrada siempre da la misma salida). Orden pedido por
        /// Cesar, literal: "primero busca una ley cuyo reactivo sea lo que
        /// le diste; si no hay, cae a la afinidad; si tampoco, a tu
        /// heurística actual".
        ///
        /// ESCALÓN 1 — LEY: recorre <see cref="Universe.Leyes"/> (invariante
        /// del contrato: para i &lt; Reactions.Count, Leyes[i] describe
        /// EXACTAMENTE Reactions.At(i); el índice
        /// <see cref="Universe.LeyCrecimientoIndice"/> es la ley de
        /// crecimiento del Vivium y se salta a propósito -- no es una
        /// reacción de contacto con dos reactivos, es SimStepper.GrowthTick).
        /// Si `matEntrada` es el reactivo `a` o `b` de alguna ley, exuda
        /// EXACTAMENTE lo que esa ley produce de ese lado (`productoA` o
        /// `productoB`). Así la criatura se convierte en un atajo vivo a la
        /// química de esta partida: comer lo mismo que alimenta una ley real
        /// enseña esa ley sin que el jugador tenga que montar el aparato.
        ///
        /// ESCALÓN 2 — AFINIDAD: si ninguna ley aplica, cae a
        /// <see cref="Universe.AfinidadDelUniverso"/> -- el material (o los
        /// dos) hacia los que esta semilla "tira" de verdad (~54% de las
        /// leyes sorteadas de esta partida convergen ahí, ver el comentario
        /// de ese campo en Sim/Universe.cs). Un hash determinista de
        /// (semilla, entrada) elige entre los 1-2 afines si hace falta.
        ///
        /// ESCALÓN 3 — HEURÍSTICA (respaldo): si ninguno de los dos anteriores
        /// resolvió nada (afinidad vacía/null, rarísimo pero posible), cae al
        /// criterio de la primera entrega: <see cref="Universe.ActiveEdicto"/>
        /// decide un subconjunto preferido, con el pool completo de
        /// candidatos innominados como último respaldo.
        ///
        /// En los tres escalones, un producto <see cref="MaterialId.Empty"/>,
        /// el propio <see cref="MaterialId.Vivium"/> (digerir algo y exudar
        /// más carne propia no se lee como digestión, se lee como que no
        /// pasó nada) o el mismo `matEntrada` (exudar lo mismo que se comió
        /// tampoco se lee como digestión) se descartan y se sigue buscando.
        /// </summary>
        private static byte EscogerProductoDigestion(Universe universo, byte matEntrada)
        {
            // ESCALÓN 1: ley cuyo reactivo sea lo que se le dio de comer.
            var leyes = universo.Leyes;
            if (leyes != null)
            {
                for (int i = 0; i < leyes.Length; i++)
                {
                    if (i == universo.LeyCrecimientoIndice) continue; // no es una reacción de contacto de digestión.
                    var ley = leyes[i];
                    byte producto;
                    if (ley.a == matEntrada) producto = ley.productoA;
                    else if (ley.b == matEntrada) producto = ley.productoB;
                    else continue;
                    if (EsProductoValido(producto, matEntrada)) return producto;
                }
            }

            // ESCALÓN 2: afinidad de esta semilla.
            byte[] afinidad = universo.AfinidadDelUniverso;
            if (afinidad != null && afinidad.Length > 0)
            {
                byte elegido = ElegirExcluyendo(afinidad, matEntrada, universo.Seed, matEntrada);
                if (EsProductoValido(elegido, matEntrada)) return elegido;
            }

            // ESCALÓN 3: heurística de respaldo (criterio de la primera
            // entrega, ver el informe) -- el Edicto activo ya resume "hacia
            // dónde tira" la física de esta run cuando ni la ley ni la
            // afinidad resolvieron nada.
            byte[] preferidos;
            switch (universo.ActiveEdicto)
            {
                case Edicto.FrioFertil:
                    preferidos = new byte[] { MaterialId.CrystalSeed, MaterialId.Crystal };
                    break;
                case Edicto.MateriaIrascible:
                    preferidos = new byte[] { MaterialId.Acid, MaterialId.Azoth };
                    break;
                default: // DensidadInvertida
                    preferidos = new byte[] { MaterialId.Slime, MaterialId.Azoth };
                    break;
            }

            byte resultado = ElegirExcluyendo(preferidos, matEntrada, universo.Seed, matEntrada);
            if (EsProductoValido(resultado, matEntrada)) return resultado;
            return ElegirExcluyendo(CandidatosDigestion, matEntrada, universo.Seed, (byte)(matEntrada + 77));
        }

        /// <summary>Un producto de digestión válido: no Empty (no se lee como digestión), no Vivium (no exuda más carne propia) y distinto de lo que se comió (si no, parece que no pasó nada).</summary>
        private static bool EsProductoValido(byte producto, byte matEntrada) =>
            producto != MaterialId.Empty && producto != MaterialId.Vivium && producto != matEntrada;

        private static readonly byte[] CandidatosDigestion =
            { MaterialId.Azoth, MaterialId.CrystalSeed, MaterialId.Crystal, MaterialId.Slime, MaterialId.Acid };

        private static byte ElegirExcluyendo(byte[] lista, byte excluir, int seed, byte sal)
        {
            var filtrados = new byte[lista.Length];
            int n = 0;
            for (int i = 0; i < lista.Length; i++)
                if (lista[i] != excluir) filtrados[n++] = lista[i];
            if (n == 0) return MaterialId.Empty;

            unchecked
            {
                uint h = (uint)seed * 2654435761u + (uint)sal * 668265263u + 0x9E3779B9u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return filtrados[(int)(h % (uint)n)];
            }
        }

        // ===================================================================
        // Estado
        // ===================================================================
        private void ActualizarEstado()
        {
            byte tempRaw = _sim.SampleTempRaw(_celdaCunaX, _celdaCunaY + AlturaCuerpoCeldas);
            bool enBanda = tempRaw >= _sim.Universe.VivGrowMinRaw && tempRaw <= _sim.Universe.VivGrowMaxRaw;

            if (_amenazaCerca) _estado = Estado.Asustada;
            else if (!enBanda) _estado = Estado.Aletargada;
            else if (_hambre > UmbralHambrienta) _estado = Estado.Hambrienta;
            else _estado = Estado.Contenta;
        }

        // ===================================================================
        // AUTOCALENTAMIENTO — "EL RESCOLDO SE CALIENTA A SÍ MISMO" (playtest
        // 21, fix crítico). Mismo patrón de acumulador que HeatPlate/
        // ChillStone.cs (TickDtCalor=1/30f, MaxStepsPerFrameCalor=2): el
        // ritmo real de simulación es fijo, Update solo alimenta el
        // acumulador con Time.deltaTime.
        // ===================================================================
        private void ActualizarCalorPropio(float dt)
        {
            if (_sim == null || _sim.Grid == null || _sim.Universe == null) return;

            _accCalor += dt;
            int steps = 0;
            while (_accCalor >= TickDtCalor && steps < MaxStepsPerFrameCalor)
            {
                ApplyCalorTick();
                _accCalor -= TickDtCalor;
                steps++;
            }
            if (_accCalor > TickDtCalor * MaxStepsPerFrameCalor) _accCalor = TickDtCalor * MaxStepsPerFrameCalor;
        }

        /// <summary>
        /// EL RESCOLDO SE CALIENTA A SÍ MISMO (playtest 21, fix crítico --
        /// "sin esto no hay pivot"). Medido: el ambiente del mundo es
        /// uniforme a CellGrid.AmbientRaw=70raw=20°C (regla 31 de CLAUDE.md:
        /// no se reintroduce clima por zona) y la banda de crecimiento del
        /// Vivium es Universe.VivGrowMinRaw..MaxRaw = 30+shift..60+shift °C
        /// con shift~U(-15,15) -- solo ~39% de las semillas la tienen abierta
        /// a 20°C ambiente, y la sala íntima no tiene ninguna placa térmica a
        /// propósito, así que en 6 de cada 10 partidas la criatura no podía
        /// crecer NUNCA.
        ///
        /// Ahora "Rescoldo" significa BRASA de verdad, no solo el nombre: la
        /// criatura empuja temperatura a su propia celda y a las de
        /// alrededor, mismo patrón que <see cref="HeatPlate.ApplyHeatTick"/>/
        /// ChillStone (escribe _sim.Grid.temp[] directamente + WakeChunk,
        /// clamp-hacia-objetivo por tick -- "empuja poco y a menudo en vez de
        /// mucho de golpe"), con el objetivo y el alcance leídos así:
        ///
        ///  · OBJETIVO: siempre dentro de la banda de ESTA semilla
        ///    (Universe.VivGrowMinRaw/MaxRaw, nunca una constante), y siempre
        ///    acotado por <see cref="TechoSeguridadRaw"/> -- muy por debajo
        ///    de donde el Vivium hierve o arde (ver doc de esa constante):
        ///    la criatura NUNCA puede calentarse hasta matarse.
        ///
        ///  · ALCANCE: depende del ESTADO. Contenta empuja fuerte y LEJOS
        ///    (<see cref="RadioCalorPleno"/> = 14 celdas, verificado con
        ///    números -- ver <see cref="PerfilCalorPct"/> -- que sí llega
        ///    hasta donde el otro encargo coloca el capullo hermano).
        ///    Aletargada intenta recuperar la banda a un alcance intermedio
        ///    (<see cref="RadioCalorRecuperacion"/>). Hambrienta y Asustada
        ///    se quedan en el NÚCLEO (<see cref="RadioCalorNucleo"/>): su
        ///    propia celda y las inmediatas, apuntando solo al borde BAJO de
        ///    la banda -- el "rescoldo mínimo" del que siempre se puede
        ///    recuperar sola en cuanto vuelva a comer. Nunca cero, nunca
        ///    irreversible: un juego íntimo no puede tener un estado de "ya
        ///    la mataste sin darte cuenta".
        ///
        /// Esto también es lo que hace físico de verdad el vínculo con el
        /// capullo (Capullo.cs, SIN CAMBIOS -- ya lee temperatura real de su
        /// propia celda vía _sim.SampleTempRaw, agnóstico de dónde viene ese
        /// calor): solo cuando la criatura está Contenta el empuje llega tan
        /// lejos, así que "alimentas -> se pone contenta -> calienta más ->
        /// el capullo avanza" sale de encajar dos piezas que ya existían, no
        /// de un "if (criatura.contenta) capullo++" cableado a mano.
        /// </summary>
        private void ApplyCalorTick()
        {
            var universo = _sim.Universe;
            int minRaw = universo.VivGrowMinRaw;
            int maxRawSeguro = Mathf.Min((int)universo.VivGrowMaxRaw, (int)TechoSeguridadRaw);
            if (maxRawSeguro < minRaw) maxRawSeguro = minRaw; // banda degenerada -- defensivo, no debería pasar con una semilla válida.
            int rango = Mathf.Max(1, maxRawSeguro - minRaw);

            float fraccionObjetivo, fraccionStep;
            int radio;
            switch (_estado)
            {
                case Estado.Contenta:
                    fraccionObjetivo = 0.85f; fraccionStep = 1.0f; radio = RadioCalorPleno;
                    break;
                case Estado.Aletargada:
                    fraccionObjetivo = 0.45f; fraccionStep = 0.65f; radio = RadioCalorRecuperacion;
                    break;
                case Estado.Asustada:
                    fraccionObjetivo = 0.22f; fraccionStep = 0.45f; radio = RadioCalorNucleo;
                    break;
                default: // Hambrienta -- el rescoldo mínimo, ver doc del método.
                    fraccionObjetivo = 0.12f; fraccionStep = 0.45f; radio = RadioCalorNucleo;
                    break;
            }

            byte target = (byte)Mathf.Clamp(minRaw + Mathf.RoundToInt(rango * fraccionObjetivo), minRaw, maxRawSeguro);
            int stepBase = Mathf.Max(1, Mathf.RoundToInt(TempStepPerTickCalor * fraccionStep));

            int cx = _celdaCunaX;
            int cy = _celdaCunaY + AlturaCuerpoCeldas;
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;

            for (int dy = -radio; dy <= radio; dy++)
            {
                int y = cy + dy;
                for (int dx = -radio; dx <= radio; dx++)
                {
                    int dist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                    if (dist > radio) continue;
                    int x = cx + dx;
                    if (!CellGrid.InBounds(x, y)) continue;

                    int step = Mathf.Max(1, stepBase * PerfilCalorPct[dist] / 100);
                    int idx = CellGrid.Idx(x, y);
                    int cur = grid.temp[idx];
                    int next = cur < target ? Mathf.Min(target, cur + step) : Mathf.Max(target, cur - step);
                    grid.temp[idx] = (byte)next;
                    grid.WakeChunk(x, y, tick);
                }
            }
        }

        // ===================================================================
        // CANAL 1: LATIDO — el más importante (ver docblock de la clase).
        // ===================================================================
        private void ActualizarLatido(float dt)
        {
            float freqObjetivo, ampObjetivo, escalaBaseObjetivo;
            switch (_estado)
            {
                case Estado.Hambrienta: freqObjetivo = 0.55f; ampObjetivo = 0.032f; escalaBaseObjetivo = 1f; break;
                case Estado.Contenta: freqObjetivo = 1.05f; ampObjetivo = 0.055f; escalaBaseObjetivo = 1f; break;
                case Estado.Aletargada: freqObjetivo = 0.15f; ampObjetivo = 0.014f; escalaBaseObjetivo = 1f; break;
                // Asustada: rápido y corto -- corazón acelerado, no "grande" -- Y
                // el CUERPO se encoge un poco (pedido explícito: "el cuerpo un
                // poco más pequeño"), no solo los zarcillos (ver ActualizarZarcillos).
                default: freqObjetivo = 2.30f; ampObjetivo = 0.030f; escalaBaseObjetivo = 0.86f; break;
            }

            _freqLatidoActual = Mathf.MoveTowards(_freqLatidoActual, freqObjetivo, dt * 0.6f);
            _ampLatidoActual = Mathf.MoveTowards(_ampLatidoActual, ampObjetivo, dt * 0.15f);
            _escalaBaseActual = Mathf.MoveTowards(_escalaBaseActual, escalaBaseObjetivo, dt * 0.5f);

            _faseLatido += _freqLatidoActual * dt * Mathf.PI * 2f;
            if (_faseLatido > Mathf.PI * 2f) _faseLatido -= Mathf.PI * 2f;

            float amp = _ampLatidoActual + _pulsoExtra * 0.10f;
            float escala = _escalaBaseActual * (1f + amp * Mathf.Sin(_faseLatido));
            _pivoteLatido.localScale = new Vector3(escala, escala, 1f);
        }

        // ===================================================================
        // CANAL 2: COLOR — cruce Vivo/Dormida (mismo lenguaje de desaturación
        // que SimRenderer usa para Vivium dormido, ver SerSprites.Desaturar).
        // ===================================================================
        private void ActualizarColor(float dt)
        {
            float objetivo;
            switch (_estado)
            {
                case Estado.Contenta: objetivo = 1f; break;
                case Estado.Hambrienta: objetivo = 0.55f; break;
                case Estado.Asustada: objetivo = 0.85f; break;
                default: objetivo = 0.05f; break; // Aletargada
            }
            _vitalidadActual = Mathf.MoveTowards(_vitalidadActual, objetivo, dt * 0.35f);

            int frameIdx = _framesVivo.Length <= 1 ? 0
                : Mathf.FloorToInt(Time.time * FirmaVisualFabrica.AnimFps) % _framesVivo.Length;
            if (frameIdx != _frameActual)
            {
                _corazonVivoSr.sprite = _framesVivo[frameIdx];
                _corazonDormidoSr.sprite = _framesDormido[frameIdx];
                _frameActual = frameIdx;
            }

            var c = _corazonDormidoSr.color;
            c.a = 1f - _vitalidadActual;
            _corazonDormidoSr.color = c;
        }

        // ===================================================================
        // CANAL 3: ZARCILLOS — el más barato con más retorno afectivo (pedido
        // explícito del encargo: "hazlo bien").
        // ===================================================================
        private const float RadioAtencionZarcillos = 3.2f;

        private void ActualizarZarcillos(float dt)
        {
            Vector3 origen = _pivoteLatido.position;
            Vector2 aJugador = _jugador != null ? (Vector2)(_jugador.position - origen) : Vector2.zero;
            float distJugador = aJugador.magnitude;
            float pesoJugador = distJugador > 0.001f ? Mathf.Clamp01(1f - distJugador / RadioAtencionZarcillos) : 0f;
            float anguloJugadorDeg = distJugador > 0.001f ? Mathf.Atan2(aJugador.x, aJugador.y) * Mathf.Rad2Deg : 0f;
            float anguloAmenazaDeg = _amenazaCerca
                ? Mathf.Atan2(_direccionAmenazaNorm.x, _direccionAmenazaNorm.y) * Mathf.Rad2Deg + 180f
                : 0f;

            float vitalidadColor = Mathf.Lerp(0.35f, 1f, _vitalidadActual);
            var def = _sim.Universe.Get(MaterialId.Vivium);
            Color32 colorBase32 = def.baseColor;
            // OJO (bug real encontrado releyendo antes de entregar): Color.r/g/b son
            // floats 0..1, NO bytes -- hay que promediar en espacio Color32 (bytes)
            // y solo convertir a Color (0..1) al final, o "gray" sale siempre 0.
            int gray = (colorBase32.r + colorBase32.g + colorBase32.b) / 3;
            Color colorBase = colorBase32; // conversión implícita Color32->Color (normaliza a 0..1) SOLO aquí.
            Color colorZarcillo = Color.Lerp(new Color(gray / 255f, gray / 255f, gray / 255f, 1f), colorBase, vitalidadColor);
            colorZarcillo.a = 0.92f;

            for (int i = 0; i < NumZarcillos; i++)
            {
                bool derecha = i >= NumZarcillos / 2;
                float signo = derecha ? 1f : -1f;

                float anguloReposo, swayAmp, swayFreq, longitud;
                switch (_estado)
                {
                    case Estado.Contenta:
                        // (playtest 21, SEGUNDA pasada: "enormes... astas de
                        // ciervo") longitud bajada de 1f a 0.8f y swayAmp de
                        // 9f a 6f -- combinado con AnchoMundoZarcillo/
                        // AltoMundoZarcillo/AnclasFrac más ajustados arriba,
                        // ya no son las cuñas gruesas y separadas del bulbo
                        // que se reportaron.
                        anguloReposo = AngulosBaseAbs[i] * signo; swayAmp = 6f; swayFreq = 0.35f; longitud = 0.8f;
                        break;
                    case Estado.Hambrienta:
                        anguloReposo = (AngulosBaseAbs[i] + 55f) * signo; swayAmp = 3f; swayFreq = 0.18f; longitud = 0.85f;
                        break;
                    case Estado.Aletargada:
                        anguloReposo = (AngulosBaseAbs[i] + 45f) * signo; swayAmp = 1.2f; swayFreq = 0.10f; longitud = 0.75f;
                        break;
                    default:
                        // Asustada (playtest 21, CORREGIDO: el bug real era que
                        // esta rama dejaba el ángulo a menos de la mitad de su
                        // reposo normal -- seguía apuntando arriba y afuera, solo
                        // que un poco menos, y eso se lee como sorpresa festiva,
                        // no como miedo). Encogerse de verdad: el ángulo se acerca
                        // a 180° (colgando hacia ABAJO) del MISMO lado que su
                        // ancla -- pegados al costado del cuerpo, no cruzando al
                        // otro lado (que se leería como un nudo, no como encogerse).
                        anguloReposo = (150f + AngulosBaseAbs[i] * 0.3f) * signo;
                        swayAmp = 4f; swayFreq = 1.3f; longitud = 0.6f;
                        break;
                }
                longitud *= LongitudBaseFrac[i]; // 3-4 longitudes intrínsecas distintas, ver el array.

                float sway = Mathf.Sin(Time.time * swayFreq * Mathf.PI * 2f + _zarcilloFaseSway[i]) * swayAmp;
                float anguloFinal = anguloReposo + sway;

                if (_estado == Estado.Asustada && _amenazaCerca)
                    anguloFinal = Mathf.LerpAngle(anguloFinal, anguloAmenazaDeg, 0.5f);
                else if (pesoJugador > 0f)
                    anguloFinal = Mathf.LerpAngle(anguloFinal, anguloJugadorDeg, pesoJugador * 0.6f);

                // OJO signo (sin compilador/editor a mano para verificar el sentido
                // visual): anguloFinal usa la convención "positivo = hacia +X/derecha"
                // (misma que anguloJugadorDeg, ambas vía Atan2(x,y)). Si en el editor
                // el zarcillo derecho se inclina hacia la IZQUIERDA, basta con quitar
                // este signo negativo (cambiar a `anguloFinal` sin más) -- es un ajuste
                // cosmético de una línea, no una reestructuración.
                _zarcillos[i].localRotation = Quaternion.Euler(0f, 0f, -anguloFinal);

                _zarcilloLongitudActual[i] = Mathf.MoveTowards(_zarcilloLongitudActual[i], longitud, dt * 1.5f);
                _zarcillos[i].localScale = new Vector3(1f, _zarcilloLongitudActual[i], 1f);

                _zarcilloSr[i].color = colorZarcillo;
            }
        }

        // ===================================================================
        // HALO — llamado desde LateUpdate ("siguiendo al corazón").
        // ===================================================================
        private void ActualizarHalo(float dt)
        {
            float radioObjetivo, calidezObjetivo, intensidadObjetivo;
            switch (_estado)
            {
                case Estado.Contenta: radioObjetivo = 1.6f; calidezObjetivo = 1f; intensidadObjetivo = 0.95f; break;
                case Estado.Hambrienta: radioObjetivo = 0.6f; calidezObjetivo = 0.15f; intensidadObjetivo = 0.7f; break;
                case Estado.Aletargada: radioObjetivo = 0.35f; calidezObjetivo = 0.10f; intensidadObjetivo = 0.35f; break;
                default: radioObjetivo = 0.55f; calidezObjetivo = 0.5f; intensidadObjetivo = 0.75f; break; // Asustada
            }

            _haloRadioActual = Mathf.MoveTowards(_haloRadioActual, radioObjetivo, dt * 0.9f);
            _haloCalidezActual = Mathf.MoveTowards(_haloCalidezActual, calidezObjetivo, dt * 0.7f);
            _haloIntensidadActual = Mathf.MoveTowards(_haloIntensidadActual, intensidadObjetivo, dt * 0.7f);

            float parpadeo = 1f;
            if (_estado == Estado.Asustada)
                parpadeo = 1f + (Mathf.PerlinNoise(Time.time * 9f, 0.5f) - 0.5f) * 0.35f;

            // (ver SerSprites.ConstruirHalo, CORREGIDO playtest 21: el alfa ya
            // vuelve a 0 al 78% del radio inscrito de la textura -- no hay
            // "canto duro" que esconder, verificado compositando sobre un
            // fondo que no es negro puro). diametroMundo=4.6 se conserva de
            // la primera entrega (decide cuánto ANILLO visible de tinte cae
            // alrededor del cuerpo, no un borde a esconder): a esa escala el
            // pico de tinte (30% del radio de la textura) cae a poco más de
            // 1 unidad de mundo del corazón en Contenta, un halo perceptible
            // sin devorar la cámara íntima pequeña.
            float diametroMundo = 4.6f;
            float escala = diametroMundo * _haloRadioActual * parpadeo;

            _haloRoot.position = _pivoteLatido.position;
            _haloRoot.localScale = new Vector3(escala, escala, 1f);

            var cCalido = _haloCalidoSr.color;
            cCalido.a = _haloIntensidadActual * _haloCalidezActual;
            _haloCalidoSr.color = cCalido;

            var cFrio = _haloFrioSr.color;
            cFrio.a = _haloIntensidadActual * (1f - _haloCalidezActual);
            _haloFrioSr.color = cFrio;
        }
    }
}
