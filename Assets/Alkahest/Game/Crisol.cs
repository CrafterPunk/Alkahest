using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// EL CRISOL — reconstruido entero en el PLAYTEST 27
    /// (docs/CONTRATO_TALLER_GRANDE.md, mandatos 1-4).
    ///
    /// =====================================================================
    /// POR QUÉ SE REHIZO: EL VEREDICTO DE CESAR SOBRE EL PLAYTEST 26
    /// =====================================================================
    /// Textual: *"no sé por qué dice 'cargadme combustible'; no entiendo por
    /// qué hay una N brillando -- me hace pensar que ya tiene combustible y
    /// que algo está prendido; mucho menos sé dónde poner el combustible;
    /// peor aún, AHÍ NO CABE NADA; y más grave: yo al inicio NO TENGO
    /// combustible... sin embargo ése es el mensaje más persistente"*. Y
    /// sobre el interior: *"seca, tuesta y no sé qué más, pero todo lo hace
    /// RÁPIDO, y cada vez que le tiro limo saco 4 cosas de colores que me
    /// aturden"*.
    ///
    /// Cinco fallos distintos, cinco respuestas:
    ///  1. AHÍ NO CABE NADA -> la cámara pasa de 7x5=35 celdas a
    ///     <see cref="CamaraAncho"/>x<see cref="CamaraAlto"/> = 13x9 = **117
    ///     celdas**: una ración entera de caño (45) cabe con holgura.
    ///  2. NO SÉ DÓNDE PONER EL COMBUSTIBLE -> el brasero deja de ser una
    ///     cubetita pegada al crisol y pasa a ser un CESTO DE HIERRO APARTE,
    ///     6 celdas a la derecha, chato y ancho donde el crisol es alto y
    ///     panzudo (<see cref="MaquinariaSprites.CestoBrasero"/>): dos bocas
    ///     que no se parecen en nada.
    ///  3. "CARGADME COMBUSTIBLE" DE ENTRADA -> el rótulo en reposo ya no
    ///     pide nada: dice lo que el aparato ES ("fuego bajo · vierte y
    ///     prueba"). El crisol arranca con su rescoldo propio
    ///     (<see cref="Universe.CrisolTier0Raw"/>) y el brasero arranca
    ///     FRÍO Y VACÍO, visualmente apagado.
    ///  4. LA N BRILLANDO -> era el pulso de proximidad del playtest 26. Se
    ///     apagó (`AffordanceGlow.ProximidadActiva=false`) y el mismo
    ///     mecanismo pasa a significar lo que todo el mundo lee en un pulso:
    ///     ESTOY TRABAJANDO (<see cref="MaquinariaSprites.AffordanceGlow.AlfaTrabajo"/>,
    ///     encendido solo mientras corre una hornada).
    ///  5. TODO RÁPIDO Y CUATRO COSAS DE GOLPE -> ver el bloque siguiente.
    ///
    /// =====================================================================
    /// EL CAMBIO DE CAUSALIDAD: **HORNADAS** (mandato 4, diseño cerrado)
    /// =====================================================================
    /// El crisol del 25/26 era un CAMPO: mantenía la cubeta caliente todo el
    /// rato y sondeaba transformaciones cada 0.8s. Consecuencia inevitable:
    /// cascada (el limo se separaba, el polvo resultante se calcinaba, el
    /// calcinado seguía...) y ninguna de las tres cosas se veía ocurrir.
    ///
    /// Desde el playtest 27 el crisol es un HORNO POR HORNADAS:
    ///  · En REPOSO **no empuja temperatura ninguna**. Eso es lo que hace
    ///    estructuralmente imposible la cascada: sin batch no hay calor, y
    ///    sin calor no hay una segunda transformación. (Es también la razón
    ///    de que el rótulo en reposo no pida nada: no está esperando leña,
    ///    está esperando una orden.)
    ///  · **E enciende UNA hornada.** Se decide EN EL MOMENTO DEL ENCENDIDO
    ///    qué transformación va a ocurrir (material dominante de la cámara x
    ///    temperatura disponible, ver <see cref="DecidirHornada"/>) y ya no
    ///    cambia: una pasada, una transformación, siempre.
    ///  · La hornada corre <see cref="HornadaSegundos"/> segundos a ritmo
    ///    VISIBLE: el rescoldo sube, las burbujas suben, el cesto ruge y la
    ///    silueta entera late. Nada ocurre "de golpe".
    ///  · Al acabar, el crisol **REPOSA CON EL RESULTADO DENTRO**, y lo
    ///    MANTIENE a una temperatura en la que ese resultado es estable
    ///    (<see cref="TempReposoPara"/>) hasta que el jugador lo recoge. Ese
    ///    "recoger y volver a pasar" es EL gesto del juego (decisión de
    ///    Cesar), y por eso el resultado tiene que seguir ahí, intacto,
    ///    cuando vuelvas.
    ///
    /// LA CARRERA CONTRA `SimStepper.ApplyPhase`, RESUELTA SIN CARRERA. El
    /// crisol del 26 tenía un `RecocidoScan` que corría en CADA tick de
    /// física para ganarle por 4 raw al templado del mundo -- una carrera
    /// invisible que el jugador no podía ni ver ni entender (y justo el tipo
    /// de mecanismo que la regla 49 obliga a mirar con lupa). Ya no existe:
    ///  · Durante el 88% de la hornada el objetivo térmico se CLAMPEA por
    ///    debajo del umbral del mundo (<see cref="TechoSeguroPara"/>), así
    ///    que el mundo no puede transformar nada antes de tiempo.
    ///  · RECOCER es ahora una hornada explícita (metes Fundido, pulsas E):
    ///    el crisol lo sostiene JUSTO por encima de su punto de
    ///    solidificación durante toda la pasada y lo convierte él al final.
    ///  · TEMPLAR sigue siendo del mundo, y sigue siendo el contraste del
    ///    diseño: sacas el Fundido con el frasco y lo viertes FUERA, donde se
    ///    enfría de golpe. Enfriar dentro = Recocido; enfriar fuera =
    ///    Templado. Dos gestos distintos, los dos visibles.
    ///
    /// **UNA BASE POR HORNADA, ELEGIDA POR LA TEMPERATURA** (mandato 4). El
    /// limo ya no se separa en el mundo (Sim/SimStepper.cs retiró
    /// `ProcessLimoSeparacion` esta ronda). Lo separa el crisol, y saca UNA
    /// sola base: la MÁS ALTA cuya banda <see cref="Universe.ExtraccionRaw"/>
    /// quepa en la temperatura de esta hornada. Con el fuego bajo sale
    /// siempre la primera (su banda está por debajo de `CrisolTier0Raw` en
    /// toda seed, garantizado por el solver); las demás exigen combustibles
    /// mejores. Es LITERALMENTE la intuición que Cesar formuló solo -- *"pensé
    /// que estaría en relación al nivel de combustible, siendo que algunos
    /// llegan a temperaturas más altas"* -- convertida en la mecánica.
    ///
    /// EL COMBUSTIBLE SE CONSUME POR HORNADA, no por reloj. Una celda de
    /// combustible = una pasada. El playtest 26 quemaba una cada 6s aunque no
    /// estuvieras haciendo nada, lo que hacía imposible planificar.
    ///
    /// =====================================================================
    /// GEOMETRÍA (mandato 1 y 2)
    /// =====================================================================
    /// La mampostería la talla Sim/SimLevelBuilder.cs vía
    /// <see cref="TallarEnPlano"/> (regla 47, sin cambios desde el 26); las
    /// medidas viven aquí y son una sola fuente de verdad.
    ///
    ///        ╱‾‾‾‾‾‾‾‾‾ boca embudada, 11 filas ‾‾‾‾‾‾‾‾‾╲   <- y 190
    ///       ╱   (las paredes DE PIEDRA se abren 6 celdas   ╲
    ///      ╱     por lado: la geometría embuda de verdad)   ╲ <- y 180
    ///     │███  cámara 13x9 = 117 celdas  ███│      ╭─────╮
    ///     │███████████████████████████████████│     │cesto│  <- brasero
    ///     └───────────────────────────────────┘     ╰─────╯      y 171..176
    ///
    /// NADA DE EMBUDOS FLOTANTES (mandato 2, el error que "mató la
    /// gramática" en el 26): el embudo del crisol es MAMPOSTERÍA TALLADA con
    /// paredes diagonales -- lo que vierte de verdad -- y el sprite solo pone
    /// el LABIO de latón que lo remata
    /// (<see cref="MaquinariaSprites.LabioBoca"/>) más las guías de latón que
    /// forran la rampa. Las estaciones que reciben DEPOSITANDO (prensa,
    /// chispa, ensayo) no llevan embudo ninguno.
    /// </summary>
    public sealed class Crisol : MonoBehaviour, IMaquinaInteractiva, IMovibleAnclaEsquina
    {
        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 3;
        /// <summary>Radio de foco. 3.2 -&gt; 4.0 (playtest 27): el aparato mide 41 celdas de ancho, así que tiene que responder también desde su brasero (a 22 celdas de la cámara) -- MachineFocus se queda con el MÁS CERCANO, y desde el brasero el Crisol sigue ganando a la Prensa por 2 celdas.</summary>
        private const float ProximityRange = 4.0f;

        // -----------------------------------------------------------------
        // GEOMETRÍA (playtest 27, mandato 1). Todas PÚBLICAS: las lee
        // Sim/SimLevelBuilder.cs para tallar el plano y para documentar las
        // huelgas entre estaciones.
        // -----------------------------------------------------------------
        /// <summary>Ancho del hueco interior de la cámara. 7 -&gt; 13.</summary>
        public const int CamaraAncho = 13;
        /// <summary>Alto del hueco interior de la cámara. 5 -&gt; 9. 13x9 = 117 celdas (la ración de un caño son 45).</summary>
        public const int CamaraAlto = 9;
        /// <summary>Grosor del muro de piedra. 1 -&gt; 2: un muro de una celda en un aparato de 40 de ancho se lee como una raya, no como obra.</summary>
        public const int MuroGrosor = 2;
        /// <summary>Filas de la BOCA EMBUDADA sobre la cámara.</summary>
        public const int BocaFilas = 11;
        /// <summary>Cuánto se abre cada pared de la boca, en celdas, de abajo a arriba. La boca acaba midiendo 13+2*6 = 25 celdas de luz.</summary>
        public const int BocaVuelo = 6;
        /// <summary>Ancho del hueco interior del brasero.</summary>
        public const int BraseroAncho = 5;
        /// <summary>Alto del hueco interior del brasero: CHATO a propósito (la cámara mide 9) -- las dos bocas no se parecen ni en silueta ni en altura.</summary>
        public const int BraseroAlto = 6;
        /// <summary>
        /// (segunda pasada) Celdas que el SPRITE del cuerpo sobresale de la
        /// mampostería por cada lado. El muro de piedra mide 2 celdas: a la
        /// escala de juego eso son ~16 px de chapa, y la panza vacía se leía
        /// como un ALAMBRE alrededor de un agujero. Con el vuelo, la pared
        /// visible del crisol pasa a 2+3 = 5 celdas (~40 px) y la panza puede
        /// ABOMBARSE fuera de la piedra, que es lo que la hace parecer un
        /// caldero. El sprite recorta su cámara con las medidas EXACTAS del
        /// hueco real, así que sigue sin tapar nada de lo que hay dentro.
        /// </summary>
        public const int VueloCuerpo = 3;
        /// <summary>Lo mismo para el cesto del brasero (menos vuelo: es una pieza más pequeña y no debe competir con la panza).</summary>
        public const int VueloCesto = 2;
        /// <summary>Filas del HOGAR: el nicho de fuego tallado BAJO la cámara y bajo el cesto. Sellado por construcción (piedra arriba, piedra a los lados, roca debajo) -- no le puede caer nada dentro; es teatro puro, y es lo que hace que "fuego bajo" sea una descripción y no una promesa.</summary>
        public const int HogarFilas = 2;
        /// <summary>
        /// Celdas de aire entre el muro derecho de la cámara y el muro
        /// izquierdo del brasero. 10 -&gt; **6** (segunda pasada, visto
        /// jugando): con 10, el cesto quedaba a 4 celdas de la jamba de la
        /// PRENSA y a 12 de la cámara del Crisol -- o sea que se leía como
        /// parte de la prensa. La duda original de Cesar ("mucho menos sé
        /// dónde poner el combustible") habría sobrevivido intacta. Con 6, el
        /// cesto se mete debajo del flanco derecho de la boca embudada: los
        /// dos recintos se leen como UN horno con DOS bocas, y quedan 8
        /// celdas de aire limpio hasta la Prensa.
        /// </summary>
        public const int BraseroSeparacion = 6;

        // ---- Compatibilidad de nombres (regla 15: se documenta lo que se
        // retira, no se borra en silencio). Sim/SimLevelBuilder.cs del
        // playtest 26 documentaba las huelgas citando CubetaAncho/TolvaAncho/
        // HuecoEntreCubetaYTolva. Se conservan como alias EXACTOS de las
        // medidas nuevas para que ningún comentario ni llamante quede
        // colgando, y para que nadie reintroduzca los valores viejos.
        public const int CubetaAncho = CamaraAncho;
        public const int CubetaAlto = CamaraAlto;
        public const int TolvaAncho = BraseroAncho;
        public const int TolvaAlto = BraseroAlto;
        public const int HuecoEntreCubetaYTolva = BraseroSeparacion;

        // -----------------------------------------------------------------
        // HORNADA (mandato 4)
        // -----------------------------------------------------------------
        /// <summary>Duración de una hornada. El contrato pide 8-12s "con progreso que se ve": 10 es el centro, y es el tiempo en el que da tiempo a MIRAR sin aburrirse.</summary>
        private const float HornadaSegundos = 10f;
        /// <summary>Fracción de la hornada durante la que el objetivo térmico se mantiene POR DEBAJO del umbral del mundo (ver <see cref="TechoSeguroPara"/>): así ninguna transformación ocurre antes de tiempo y no hay ninguna carrera invisible.</summary>
        private const float FraccionConTecho = 0.88f;
        /// <summary>Cuánto sube/baja la temperatura de la cámara por tick de física mientras corre una hornada.</summary>
        private const int TempStepPerTick = 5;
        /// <summary>Margen por encima del punto de solidificación al que el crisol sostiene un Fundido durante la hornada de RECOCIDO -- lo justo para que el mundo no lo temple por su cuenta antes de que acabe la pasada.</summary>
        private const int MargenRecocido = 3;
        /// <summary>Margen por debajo del umbral del mundo al que se clampea la rampa durante <see cref="FraccionConTecho"/>.</summary>
        private const int MargenTecho = 2;

        private enum Fase { Reposo, Corriendo, Lista }

        private AlkahestSim _sim;
        private Transform _player;
        private SubstanceKnowledge _conocimiento;

        private int _anchorX;
        private int _baseY;

        // Cámara y brasero (interiores, sin muros).
        private int _camX0, _camX1, _camY0, _camY1;
        private int _braX0, _braX1, _braY0, _braY1;
        private int _bocaY0, _bocaY1;
        private int _outX0, _outX1, _outY0, _outY1;

        /// <summary>
        /// (playtest 29) Handle del rect anticincel de ESTA instancia en
        /// <see cref="SimLevelBuilder.ObraDelTaller"/> -- lo devuelve
        /// <see cref="SimLevelBuilder.RegistrarObra"/> al registrarse en
        /// <see cref="Init"/> (no en <see cref="TallarEnPlano"/>, que es
        /// estático y corre ANTES de que exista esta instancia: ver el
        /// bloque "OBRA MOVIBLE" en Sim/SimLevelBuilder.cs para el porqué del
        /// diseño). <see cref="Reposicionar"/> lo usa para actualizar el rect
        /// en vez de dejar el viejo protegido para siempre.
        /// </summary>
        private int _handleObra = -1;

        private Vector3 _centro, _centroCamara, _centroBrasero, _centroBoca;
        private float _accumulator;

        private Fase _fase = Fase.Reposo;
        private float _hornadaT;
        private byte _hornadaEntrada, _hornadaSalida;
        private byte _hornadaCima;      // temperatura que alcanza esta hornada.
        private byte _hornadaTecho;     // clampeo durante FraccionConTecho.
        private string _hornadaCondicion;
        private string _hornadaVerbo;
        private byte _targetRaw;        // objetivo térmico ACTUAL (0 = no empujar nada: reposo).
        private byte _reposoRaw;        // temperatura de mantenimiento del resultado (fase Lista).

        private byte _fuelMat;          // combustible presente en el cesto ahora mismo (Empty = ninguno).
        private bool _cestoArdiendo;    // ¿arde AHORA? Solo durante una hornada alimentada.

        private bool _camaraTieneAlgo;
        private byte _dominanteCamara;

        // ---- Visual ----
        private SpriteRenderer _resalte;
        private SpriteRenderer _latidoTrabajo;
        private SpriteRenderer _brasasHogar, _brasasCesto;
        private SpriteRenderer _destelloCamara, _destelloCesto;
        private float _alfaResalte;
        private const int Burbujas = 6;
        private readonly SpriteRenderer[] _burbujas = new SpriteRenderer[Burbujas];
        private const int HumoPuffs = 4;
        private const float HumoCicloSeg = 2.4f;
        private readonly SpriteRenderer[] _humo = new SpriteRenderer[HumoPuffs];
        private Vector3 _humoOrigen;

        // Acuse de recibo (mandato 3): destello del marco al entrar materia.
        private readonly MaquinariaSprites.Destello _acuseCamara = new MaquinariaSprites.Destello();
        private readonly MaquinariaSprites.Destello _acuseCesto = new MaquinariaSprites.Destello();
        private int _celdasCamaraPrev, _celdasCestoPrev;

        // El pulso de la clase AffordanceGlow, en su destino aprobado: TRABAJANDO.
        private readonly MaquinariaSprites.AffordanceGlow _pulsoTrabajo = new MaquinariaSprites.AffordanceGlow();

        private const float RangoEstadoPleno = 7.0f;
        private const float RangoEstadoDesvanece = 9.0f;
        private const float RangoNombrePleno = 3.4f;
        private const float RangoNombreDesvanece = 4.6f;
        private bool _yaConocida;
        private const string ChapaNombre = "el crisol";

        public Vector3 PuntoFoco => _centroCamara;
        public float RangoFoco => ProximityRange;

        // ---- IMovible ----
        public Vector3 CentroMundo => _centro;
        public Vector2 TamanoMundo => new Vector2(
            (_outX1 - _outX0 + 1) * SimRenderer.CellWorldSize,
            (_outY1 - _outY0 + 1) * SimRenderer.CellWorldSize);
        /// <summary>Ancla: esquina inferior izquierda del rect EXTERIOR del horno (boca incluida). Todo lo demás viaja relativo a esto.</summary>
        public Vector2Int AnclaCelda => new Vector2Int(_outX0, _baseY);

        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            int span = _outX1 - _outX0 + 1;
            int alto = _outY1 - _outY0 + 1;
            return anclaCelda.x >= 1 && anclaCelda.x + span - 1 <= CellGrid.W - 2
                && anclaCelda.y >= 1 && anclaCelda.y + alto - 1 <= CellGrid.H - 2;
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap. FIRMA SIN CAMBIOS. `anchorX` = SimLevelBuilder.CrisolX.</summary>
        public void Init(AlkahestSim sim, Transform player, SubstanceKnowledge conocimiento, int anchorX)
        {
            _sim = sim;
            _player = player;
            _conocimiento = conocimiento;

            _anchorX = anchorX;
            _baseY = SimLevelBuilder.CuartoY0 + 2;

            RecalcularRegiones();
            BuildVisual();
            _targetRaw = 0; // REPOSO: el crisol no empuja nada hasta que enciendas una hornada.

            MachineFocus.Registrar(this);
            // (playtest 29) El registro anticincel lo hace la INSTANCIA, no
            // TallarEnPlano -- ver el docblock de _handleObra y el bloque
            // "OBRA MOVIBLE" en Sim/SimLevelBuilder.cs.
            _handleObra = SimLevelBuilder.RegistrarObra(_outX0, _outY0 - HogarFilas, _outX1, _outY1);
            Mudanza.RegistrarMovible(this);
        }

        // =================================================================
        // HUELLA — una sola aritmética, compartida por la instancia y por el
        // tallado del plano (para que el dibujo y la piedra jamás difieran).
        // =================================================================
        private struct Huella
        {
            public int CamX0, CamX1, CamY0, CamY1;
            public int BraX0, BraX1, BraY0, BraY1;
            public int BocaY0, BocaY1;
            public int OutX0, OutX1, OutY0, OutY1;
        }

        private static Huella Calcular(int anchorX, int baseY)
        {
            Huella h;
            h.CamX0 = anchorX - CamaraAncho / 2;
            h.CamX1 = h.CamX0 + CamaraAncho - 1;
            h.CamY0 = baseY + 1;
            h.CamY1 = h.CamY0 + CamaraAlto - 1;

            h.BocaY0 = h.CamY1 + 1;
            h.BocaY1 = h.BocaY0 + BocaFilas - 1;

            h.BraX0 = h.CamX1 + MuroGrosor + BraseroSeparacion + MuroGrosor;
            h.BraX1 = h.BraX0 + BraseroAncho - 1;
            h.BraY0 = baseY + 1;
            h.BraY1 = h.BraY0 + BraseroAlto - 1;

            h.OutX0 = h.CamX0 - MuroGrosor - BocaVuelo;
            h.OutX1 = h.BraX1 + MuroGrosor;
            h.OutY0 = baseY;
            h.OutY1 = h.BocaY1 + 1;
            return h;
        }

        /// <summary>Cuánto se ha abierto la boca en la fila `i` (0 = la primera sobre la cámara). Progresión entera para que la rampa se vea escalonada y tallada, no interpolada.</summary>
        private static int VueloEnFila(int i) => (BocaVuelo * (i + 1) + BocaFilas / 2) / BocaFilas;

        private void RecalcularRegiones()
        {
            var h = Calcular(_anchorX, _baseY);
            _camX0 = h.CamX0; _camX1 = h.CamX1; _camY0 = h.CamY0; _camY1 = h.CamY1;
            _braX0 = h.BraX0; _braX1 = h.BraX1; _braY0 = h.BraY0; _braY1 = h.BraY1;
            _bocaY0 = h.BocaY0; _bocaY1 = h.BocaY1;
            _outX0 = h.OutX0; _outX1 = h.OutX1; _outY0 = h.OutY0; _outY1 = h.OutY1;

            float c = SimRenderer.CellWorldSize;
            _centro = new Vector3((_outX0 + (_outX1 - _outX0 + 1) * 0.5f) * c,
                                  (_outY0 + (_outY1 - _outY0 + 1) * 0.5f) * c, 0f);
            transform.position = _centro;
            _centroCamara = new Vector3((_camX0 + CamaraAncho * 0.5f) * c, (_camY0 + CamaraAlto * 0.5f) * c, 0f);
            _centroBrasero = new Vector3((_braX0 + BraseroAncho * 0.5f) * c, (_braY0 + BraseroAlto * 0.5f) * c, 0f);
            _centroBoca = new Vector3((_camX0 + CamaraAncho * 0.5f) * c, (_bocaY1 + 1f) * c, 0f);
            _humoOrigen = new Vector3((_camX1 + BocaVuelo - 0.5f) * c, (_bocaY1 + 11f) * c, 0f);
        }

        /// <summary>Talla el horno completo (cámara + boca embudada + cesto del brasero) sobre el CellGrid del plano. Construcción de nivel: `SetCell`, no `PaintStable` (regla 29 es para runtime).</summary>
        public static void TallarEnPlano(CellGrid grid, int anchorX, int baseY)
        {
            var h = Calcular(anchorX, baseY);

            // Cámara: suelo + dos muros + interior vaciado.
            TallarRecinto(grid, h.CamX0, h.CamX1, h.CamY0, h.CamY1);

            // LA BOCA EMBUDADA: en cada fila las paredes se separan un poco
            // más, así que la materia que caiga dentro de la boca RESBALA
            // hacia la cámara -- el embudo es la piedra, no un sprite.
            for (int i = 0; i < BocaFilas; i++)
            {
                int y = h.BocaY0 + i;
                int vuelo = VueloEnFila(i);
                int izq = h.CamX0 - vuelo;
                int der = h.CamX1 + vuelo;
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    if (CellGrid.InBounds(izq - t, y)) grid.SetCell(izq - t, y, MaterialId.Stone);
                    if (CellGrid.InBounds(der + t, y)) grid.SetCell(der + t, y, MaterialId.Stone);
                }
                for (int x = izq; x <= der; x++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Empty);
            }

            // El cesto del brasero, aparte.
            TallarRecinto(grid, h.BraX0, h.BraX1, h.BraY0, h.BraY1);

            // (segunda pasada) LOS DOS HOGARES: sendos nichos de fuego bajo la
            // cámara y bajo el cesto, tallados en la losa del cuarto (filas
            // baseY-2..baseY-1). Quedan SELLADOS por construcción -- piedra
            // encima (la fila baseY, el suelo del recinto), piedra a los lados
            // (el resto de la losa) y roca maciza debajo (la losa del cuarto
            // solo llega hasta CuartoY0) -- así que nada puede caer dentro ni
            // salir: son puro teatro. Antes, las brasas se dibujaban sueltas
            // SOBRE el suelo y se leían como grava roja derramada.
            TallarHogar(grid, h.CamX0, h.CamX1, baseY);
            TallarHogar(grid, h.BraX0, h.BraX1, baseY);

            // (playtest 29) El registro anticincel YA NO SE HACE AQUÍ: este
            // método es estático y corre UNA vez desde
            // SimLevelBuilder.BuildCuartoIntimo, ANTES de que exista ninguna
            // instancia de Crisol que pueda guardarse el handle para
            // actualizarlo luego al Reposicionar. Lo registra `Init` (ver
            // `_handleObra`) -- mismo rect EXACTO (`h.OutX0, h.OutY0 -
            // HogarFilas, h.OutX1, h.OutY1`), porque `Init` vuelve a llamar a
            // `Calcular` con el mismo `anchorX`/`baseY` que usó este tallado.
        }

        /// <summary>Vacía el nicho de fuego bajo un recinto (ver el comentario de <see cref="TallarEnPlano"/> para por qué queda sellado y no es una trampa para la materia).</summary>
        private static void TallarHogar(CellGrid grid, int x0, int x1, int baseY)
        {
            for (int y = baseY - HogarFilas; y <= baseY - 1; y++)
                for (int x = x0; x <= x1; x++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Empty);
        }

        private static void TallarRecinto(CellGrid grid, int x0, int x1, int y0, int y1)
        {
            for (int x = x0 - MuroGrosor; x <= x1 + MuroGrosor; x++)
                if (CellGrid.InBounds(x, y0 - 1)) grid.SetCell(x, y0 - 1, MaterialId.Stone);
            for (int y = y0 - 1; y <= y1; y++)
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    if (CellGrid.InBounds(x0 - t, y)) grid.SetCell(x0 - t, y, MaterialId.Stone);
                    if (CellGrid.InBounds(x1 + t, y)) grid.SetCell(x1 + t, y, MaterialId.Stone);
                }
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Empty);
        }

        /// <summary>Misma talla que <see cref="TallarEnPlano"/> pero EN CALIENTE (regla 29: PaintStable). Solo la usa <see cref="Reposicionar"/> (Mudanza).</summary>
        private void TallarEnCaliente()
        {
            TallarRecintoCaliente(_camX0, _camX1, _camY0, _camY1);
            for (int i = 0; i < BocaFilas; i++)
            {
                int y = _bocaY0 + i;
                int vuelo = VueloEnFila(i);
                int izq = _camX0 - vuelo, der = _camX1 + vuelo;
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.PaintStable(izq - t, y, 0, MaterialId.Stone);
                    _sim.PaintStable(der + t, y, 0, MaterialId.Stone);
                }
                _sim.PaintRect(izq, y, der - izq + 1, 1, MaterialId.Empty);
            }
            TallarRecintoCaliente(_braX0, _braX1, _braY0, _braY1);
        }

        private void TallarRecintoCaliente(int x0, int x1, int y0, int y1)
        {
            for (int x = x0 - MuroGrosor; x <= x1 + MuroGrosor; x++) _sim.PaintStable(x, y0 - 1, 0, MaterialId.Stone);
            for (int y = y0 - 1; y <= y1; y++)
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.PaintStable(x0 - t, y, 0, MaterialId.Stone);
                    _sim.PaintStable(x1 + t, y, 0, MaterialId.Stone);
                }
            _sim.PaintRect(x0, y0, x1 - x0 + 1, y1 - y0 + 1, MaterialId.Empty);
        }

        /// <summary>
        /// (playtest 29, encargo B) Borra la mampostería VIEJA de la huella
        /// `h` -- <see cref="Reposicionar"/> la llama con la huella de ANTES
        /// de mover el ancla, justo antes de tallar la nueva. Espejo de
        /// <see cref="TallarRecinto"/>/<see cref="TallarEnCaliente"/> con dos
        /// diferencias a propósito:
        ///  1. Escribe <see cref="MaterialId.Empty"/> vía <c>_sim.Paint</c>
        ///     en vez de Stone vía PaintStable -- esto no CREA materia, la
        ///     QUITA (regla 29 de CLAUDE.md), el mismo camino que usa
        ///     Game/Cincel.cs al tallar piedra a vacío.
        ///  2. NUNCA toca la fila `y0-1` de cada recinto (la losa COMPARTIDA
        ///     de todo el cuarto, <c>SimLevelBuilder.BuildCuartoFloor</c> --
        ///     "jamás piedra del mundo", encargo B) ni el interior de cámara/
        ///     brasero (puede tener materia dentro: "el contenido... queda
        ///     donde está", mismo encargo -- cae solo por gravedad en cuanto
        ///     el muro que lo contenía desaparece). Solo desaparecen los
        ///     MUROS propios que esta máquina inventó sobre esa losa.
        /// </summary>
        private void BorrarEnCaliente(Huella h)
        {
            BorrarRecintoCaliente(h.CamX0, h.CamX1, h.CamY0, h.CamY1);
            for (int i = 0; i < BocaFilas; i++)
            {
                int y = h.BocaY0 + i;
                int vuelo = VueloEnFila(i);
                int izq = h.CamX0 - vuelo, der = h.CamX1 + vuelo;
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.Paint(izq - t, y, 0, MaterialId.Empty);
                    _sim.Paint(der + t, y, 0, MaterialId.Empty);
                }
                // La fila interior de la boca (izq..der) ya es Empty por
                // diseño -- el embudo talla aire, nunca piedra -- así que no
                // hace falta (ni conviene: podría tener materia cayendo)
                // tocarla aquí.
            }
            BorrarRecintoCaliente(h.BraX0, h.BraX1, h.BraY0, h.BraY1);
        }

        /// <summary>Muros de un recinto, EXCLUYENDO la fila `y0-1` (la losa compartida del cuarto) y sin tocar el interior -- ver <see cref="BorrarEnCaliente"/>.</summary>
        private void BorrarRecintoCaliente(int x0, int x1, int y0, int y1)
        {
            for (int y = y0; y <= y1; y++)
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.Paint(x0 - t, y, 0, MaterialId.Empty);
                    _sim.Paint(x1 + t, y, 0, MaterialId.Empty);
                }
        }

        private void OnDestroy()
        {
            MachineFocus.Olvidar(this);
            Mudanza.OlvidarMovible(this);
        }

        public void Reposicionar(Vector2Int anclaCelda)
        {
            // (playtest 29, encargo B) 1) BORRAR la mampostería vieja, con la
            // huella de ANTES de tocar el ancla -- si se calculara después de
            // mover _anchorX/_baseY, `Calcular` devolvería la huella NUEVA y
            // borraríamos el sitio equivocado.
            BorrarEnCaliente(Calcular(_anchorX, _baseY));

            int dx = anclaCelda.x - _outX0;
            int dy = anclaCelda.y - _baseY;
            _anchorX += dx;
            _baseY += dy;
            RecalcularRegiones();
            TallarEnCaliente(); // 2) TALLAR la nueva. regla 36: NUNCA volver a llamar a Init/BuildVisual para mover.

            // 3) ACTUALIZAR el registro anticincel -- mismo rect que Init
            // registró, con la geometría YA recalculada.
            SimLevelBuilder.ActualizarObra(_handleObra, _outX0, _outY0 - HogarFilas, _outX1, _outY1);
        }

        // =================================================================
        // BUCLE
        // =================================================================
        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return;

            SondearCamara();

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && EstaEnfocada())
            {
                IntentarEncender();
                MachineFocus.RegistrarUsoE();
            }

            _accumulator += Time.deltaTime;
            int steps = 0;
            while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
            {
                if (_fase == Fase.Corriendo)
                {
                    _hornadaT += TickDt;
                    ActualizarObjetivoHornada();
                    if (_hornadaT >= HornadaSegundos) CerrarHornada();
                }
                EmpujarTemperatura();
                _accumulator -= TickDt;
                steps++;
            }
            if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;

            _acuseCamara.Avanzar(Time.deltaTime);
            _acuseCesto.Avanzar(Time.deltaTime);
            _pulsoTrabajo.Trabajando = _fase == Fase.Corriendo;
            ActualizarVisual();
        }

        private bool EstaEnfocada() => MachineFocus.EsFoco(this, _player);

        /// <summary>
        /// Una pasada barata por cámara y cesto: material dominante, si hay
        /// algo, y el ACUSE DE RECIBO (mandato 3) cuando el número de celdas
        /// ocupadas SUBE -- que es exactamente "acaba de entrar materia por
        /// donde debía". No hace falta comparar celda a celda: subir de
        /// ocupación solo puede venir de que algo ha entrado.
        /// </summary>
        private void SondearCamara()
        {
            var grid = _sim.Grid;
            int nCam = 0, nCesto = 0;
            byte dominante = MaterialId.Empty;
            int mejor = 0;

            for (int y = _camY0; y <= _camY1; y++)
            {
                for (int x = _camX0; x <= _camX1; x++)
                {
                    byte m = grid.GetMat(x, y);
                    if (m == MaterialId.Empty) continue;
                    nCam++;
                    int cuenta = 0;
                    for (int y2 = _camY0; y2 <= _camY1; y2++)
                        for (int x2 = _camX0; x2 <= _camX1; x2++)
                            if (grid.GetMat(x2, y2) == m) cuenta++;
                    if (cuenta > mejor) { mejor = cuenta; dominante = m; }
                }
            }

            byte fuel = MaterialId.Empty;
            var universe = _sim.Universe;
            for (int y = _braY0; y <= _braY1; y++)
            {
                for (int x = _braX0; x <= _braX1; x++)
                {
                    byte m = grid.GetMat(x, y);
                    if (m == MaterialId.Empty) continue;
                    nCesto++;
                    if (fuel == MaterialId.Empty && universe != null && universe.EsCombustible(m)) fuel = m;
                }
            }

            if (nCam > _celdasCamaraPrev) _acuseCamara.Disparar();
            if (nCesto > _celdasCestoPrev) _acuseCesto.Disparar();
            _celdasCamaraPrev = nCam;
            _celdasCestoPrev = nCesto;

            _camaraTieneAlgo = nCam > 0;
            _dominanteCamara = dominante;
            _fuelMat = fuel;

            // El resultado ya no está solo en la cámara: o el jugador lo
            // recogió (cámara vacía) o le echó materia nueva encima. En los
            // dos casos el crisol vuelve a REPOSO, para que el rótulo diga la
            // verdad ("cargado · E para encender") en vez de seguir anunciando
            // una hornada que ya no describe lo que hay dentro.
            if (_fase == Fase.Lista && (!_camaraTieneAlgo || _dominanteCamara != _hornadaSalida)) VolverAReposo();
        }

        private void VolverAReposo()
        {
            _fase = Fase.Reposo;
            _targetRaw = 0;
            _cestoArdiendo = false;
            _hornadaT = 0f;
        }

        // =================================================================
        // ENCENDER UNA HORNADA
        // =================================================================
        private void IntentarEncender()
        {
            if (_fase == Fase.Corriendo) return; // ya está trabajando: E no hace nada (y el rótulo lo dice).
            var universe = _sim.Universe;
            if (universe == null) return;

            if (!_camaraTieneAlgo || _dominanteCamara == MaterialId.Empty)
            {
                Rotular("la cámara está vacía · vierte algo dentro", UiStyles.TextoTenue);
                return;
            }

            // Temperatura DISPONIBLE en esta pasada: el rescoldo propio si el
            // cesto está vacío, o la del combustible cargado si lo hay. El
            // crisol nunca está muerto (regla 44 al revés), pero tampoco
            // trabaja solo.
            byte cima = _fuelMat != MaterialId.Empty
                ? universe.TempCombustibleRaw(_fuelMat)
                : Universe.CrisolTier0Raw;

            if (!DecidirHornada(universe, _dominanteCamara, cima,
                    out byte salida, out string condicion, out string verbo, out byte objetivo))
            {
                Rotular("este fuego no le hace nada · prueba otro combustible", UiStyles.Aviso);
                return;
            }

            _hornadaEntrada = _dominanteCamara;
            _hornadaSalida = salida;
            _hornadaCondicion = condicion;
            _hornadaVerbo = verbo;
            _hornadaCima = objetivo;
            _hornadaTecho = TechoSeguroPara(universe, _hornadaEntrada, objetivo);
            _hornadaT = 0f;
            _fase = Fase.Corriendo;

            // El combustible se gasta AL ENCENDER: una celda, una pasada.
            if (_fuelMat != MaterialId.Empty)
            {
                ConsumirUnaCeldaDeCombustible();
                _cestoArdiendo = true;
            }
            Rotular(null, UiStyles.Aviso);
        }

        private void ConsumirUnaCeldaDeCombustible()
        {
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            for (int y = _braY0; y <= _braY1; y++)
            {
                for (int x = _braX0; x <= _braX1; x++)
                {
                    if (grid.GetMat(x, y) != _fuelMat) continue;
                    grid.SetCell(x, y, MaterialId.Empty);
                    grid.WakeChunk(x, y, tick);
                    return;
                }
            }
        }

        /// <summary>
        /// LA REGLA DE UNA SOLA TRANSFORMACIÓN. Dado el material dominante y
        /// la temperatura que esta pasada puede alcanzar, decide QUÉ va a
        /// pasar -- una cosa, decidida antes de empezar y ya inmutable. Si
        /// devuelve false, no hay hornada posible y el rótulo lo dice.
        /// </summary>
        private bool DecidirHornada(Universe universe, byte entrada, byte cima,
            out byte salida, out string condicion, out string verbo, out byte objetivo)
        {
            salida = MaterialId.Empty; condicion = null; verbo = null; objetivo = cima;

            // --- LIMO: extracción por temperatura, UNA base por hornada ---
            if (entrada == MaterialId.Limo)
            {
                int elegida = -1;
                for (int b = 0; b < MaterialId.BasesCount; b++)
                    if (universe.ExtraccionRaw(b) <= cima && (elegida < 0 || universe.ExtraccionRaw(b) > universe.ExtraccionRaw(elegida)))
                        elegida = b;
                if (elegida < 0) return false; // no debería pasar: el solver garantiza una banda por debajo de tier0.

                salida = MaterialId.MatDe(elegida, EstadoMateria.Polvo);
                condicion = CondicionCalor();
                verbo = "extrayendo";
                return true;
            }

            if (!MaterialId.EsBaseEstado(entrada)) return false;

            int baseIdx = MaterialId.BaseDe(entrada);
            switch (MaterialId.EstadoDe(entrada))
            {
                case EstadoMateria.Polvo:
                    if (cima >= universe.FusionRaw(baseIdx))
                    {
                        salida = MaterialId.MatDe(baseIdx, EstadoMateria.Fundido);
                        condicion = CondicionCalor(); verbo = "fundiendo";
                        return true;
                    }
                    if (cima >= universe.CalcinacionRaw(baseIdx))
                    {
                        salida = MaterialId.MatDe(baseIdx, EstadoMateria.Calcinado);
                        condicion = CondicionCalor(); verbo = "calcinando";
                        // Se calcina POR DEBAJO de la fusión: si el fuego da de
                        // sobra, el objetivo se queda a medio camino de la banda
                        // en vez de pasarse (y fundirlo, que sería otra cosa).
                        objetivo = (byte)Mathf.Min(cima, Mathf.Max(universe.CalcinacionRaw(baseIdx), universe.FusionRaw(baseIdx) - 4));
                        return true;
                    }
                    return false;

                case EstadoMateria.Compacto:
                    byte ceramiza = universe.CeramizaRaw(baseIdx);
                    if (ceramiza == 0 || cima < ceramiza) return false;
                    salida = MaterialId.MatDe(baseIdx, EstadoMateria.Ceramico);
                    condicion = CondicionCalor(); verbo = "ceramizando";
                    return true;

                case EstadoMateria.Fundido:
                    // RECOCER: la hornada de enfriado lento. No necesita
                    // combustible -- de hecho el fuego sobra: lo que hace el
                    // crisol es SOSTENER la pieza justo por encima de su punto
                    // de solidificación mientras se ordena por dentro.
                    salida = MaterialId.MatDe(baseIdx, EstadoMateria.Recocido);
                    condicion = "recocido lento"; verbo = "recociendo";
                    objetivo = (byte)Mathf.Min(255, universe.SolidificaRaw(baseIdx) + MargenRecocido);
                    return true;

                case EstadoMateria.Solucion:
                    byte evapora = universe.UmbralPersistenciaRaw(entrada); // == el punto de ebullición de su disolvente.
                    if (cima < evapora) return false;
                    salida = MaterialId.MatDe(baseIdx, EstadoMateria.Polvo);
                    condicion = CondicionCalor(); verbo = "evaporando";
                    return true;

                default:
                    return false; // Templado/Recocido/Calcinado/Cerámico: el crisol ya no puede hacerles nada. Eso es información.
            }
        }

        /// <summary>
        /// El techo térmico que impide que <c>SimStepper.ApplyPhase</c>
        /// transforme la carga ANTES de que acabe la pasada -- lo que mataba
        /// el "ritmo visible". Es el umbral que el MUNDO usaría sobre el
        /// material de entrada, menos un margen; si el mundo no tiene nada
        /// que decir sobre ese material, no hay techo (255).
        /// </summary>
        private byte TechoSeguroPara(Universe universe, byte entrada, byte cima)
        {
            if (!MaterialId.EsBaseEstado(entrada)) return 255;
            int baseIdx = MaterialId.BaseDe(entrada);
            if (MaterialId.EstadoDe(entrada) == EstadoMateria.Polvo)
            {
                int techo = universe.FusionRaw(baseIdx) - MargenTecho;
                return (byte)Mathf.Clamp(Mathf.Min(cima, techo), 0, 255);
            }
            return 255;
        }

        /// <summary>
        /// La temperatura a la que el crisol MANTIENE el resultado mientras
        /// reposa (mandato 4: "el resultado queda en la cubeta, INTOCADO,
        /// hasta que el jugador lo recoge"). Se elige para que el mundo no
        /// pueda transformar lo que acaba de salir: por debajo de su fusión
        /// si es un sólido, por encima de su solidificación si es un fundido.
        /// </summary>
        private byte TempReposoPara(Universe universe, byte salida)
        {
            if (!MaterialId.EsBaseEstado(salida)) return CellGrid.AmbientRaw;
            int baseIdx = MaterialId.BaseDe(salida);
            if (MaterialId.EstadoDe(salida) == EstadoMateria.Fundido)
                return (byte)Mathf.Min(255, universe.SolidificaRaw(baseIdx) + MargenRecocido * 2);
            return (byte)Mathf.Max(CellGrid.AmbientRaw, universe.FusionRaw(baseIdx) - 12);
        }

        private void ActualizarObjetivoHornada()
        {
            float t = Mathf.Clamp01(_hornadaT / HornadaSegundos);
            byte cima = _hornadaCima;
            byte techo = _hornadaTecho;
            // Rampa visible: la temperatura SUBE durante toda la pasada (el
            // rescoldo se ve subir con ella) pero clampeada bajo el techo
            // seguro hasta el último tramo, cuando por fin se suelta.
            byte objetivoLibre = (byte)Mathf.RoundToInt(Mathf.Lerp(CellGrid.AmbientRaw, cima, Mathf.Min(1f, t / FraccionConTecho)));
            _targetRaw = t < FraccionConTecho ? (byte)Mathf.Min(objetivoLibre, techo) : cima;
        }

        private void CerrarHornada()
        {
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            int convertidas = 0;

            for (int y = _camY0; y <= _camY1; y++)
            {
                for (int x = _camX0; x <= _camX1; x++)
                {
                    int idx = CellGrid.Idx(x, y);
                    if (grid.GetMat(idx) != _hornadaEntrada) continue;
                    grid.SetCell(idx, _hornadaSalida, resetAux: false);
                    grid.WakeChunk(x, y, tick);
                    convertidas++;
                }
            }

            if (convertidas > 0) Hornada.RegistrarOp("crisol", _hornadaEntrada, _hornadaSalida, _hornadaCondicion);

            _fase = Fase.Lista;
            _cestoArdiendo = false;
            _reposoRaw = TempReposoPara(_sim.Universe, _hornadaSalida);
            _targetRaw = _reposoRaw;
            Rotular(null, UiStyles.Exito);
        }

        /// <summary>Empuja la temperatura de la cámara hacia <see cref="_targetRaw"/>. Con `_targetRaw` a 0 (REPOSO) no toca NADA: sin ese silencio no habría "una transformación por hornada".</summary>
        private void EmpujarTemperatura()
        {
            if (_targetRaw == 0) return;
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            int target = _targetRaw;

            for (int y = _camY0; y <= _camY1; y++)
            {
                for (int x = _camX0; x <= _camX1; x++)
                {
                    if (!CellGrid.InBounds(x, y)) continue;
                    int idx = CellGrid.Idx(x, y);
                    if (grid.GetMat(idx) == MaterialId.Empty) continue; // el aire no se calienta: lo que arde es la carga.
                    int cur = grid.temp[idx];
                    int next = cur < target ? Mathf.Min(target, cur + TempStepPerTick) : Mathf.Max(target, cur - TempStepPerTick);
                    grid.temp[idx] = (byte)next;
                    grid.WakeChunk(x, y, tick);
                }
            }
        }

        private string CondicionCalor()
        {
            if (_fuelMat == MaterialId.Empty) return "fuego bajo";
            string nombre = _conocimiento != null ? _conocimiento.NombreParaHud(_fuelMat) : "???";
            return "combustible:" + nombre;
        }

        // =================================================================
        // VISUAL
        // =================================================================
        private void BuildVisual()
        {
            float c = SimRenderer.CellWorldSize;

            // ---- El cuerpo: panza de hierro sobre los muros REALES de la
            // cámara, con el hueco recortado a transparente para que se vea
            // dentro (ver la nota de MaquinariaSprites: un sprite de máquina
            // no puede tapar su propia cámara).
            // (segunda pasada) El sprite ABOMBA VueloCuerpo celdas por fuera
            // de la piedra y recorta su cámara con `muroDibujado`, así que la
            // transparencia sigue cayendo EXACTAMENTE sobre el hueco real.
            int muroDibujado = MuroGrosor + VueloCuerpo;             // 5
            int spanCuerpo = CamaraAncho + 2 * muroDibujado;         // 23
            int altoCuerpo = CamaraAlto + 2;                         // 11
            float anchoCuerpo = spanCuerpo * c, altoCuerpoW = altoCuerpo * c;
            Vector3 posCuerpo = new Vector3((_camX0 + CamaraAncho * 0.5f) * c, (_baseY + altoCuerpo * 0.5f) * c, 0f);

            var cuerpoGo = new GameObject("CrisolCuerpo");
            cuerpoGo.transform.SetParent(transform, false);
            cuerpoGo.transform.position = posCuerpo;

            var panza = MaquinariaSprites.PanzaCrisol(spanCuerpo, altoCuerpo, muroDibujado, 1);

            _resalte = MaquinariaSprites.CrearCapa(cuerpoGo.transform, "Resalte", panza, 14, anchoCuerpo * 1.10f, altoCuerpoW * 1.14f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);
            _latidoTrabajo = MaquinariaSprites.CrearCapa(cuerpoGo.transform, "LatidoTrabajo", panza, 15, anchoCuerpo * 1.06f, altoCuerpoW * 1.08f);
            _latidoTrabajo.color = new Color(1f, 0.55f, 0.18f, 0f);
            MaquinariaSprites.CrearCapa(cuerpoGo.transform, "Panza", panza, 18, anchoCuerpo, altoCuerpoW);
            _destelloCamara = MaquinariaSprites.CrearCapa(cuerpoGo.transform, "AcuseCamara",
                MaquinariaSprites.MarcoBandeja(spanCuerpo, altoCuerpo), 22, anchoCuerpo, altoCuerpoW);
            _destelloCamara.color = new Color(1f, 1f, 0.9f, 0f);

            // ---- EL FUEGO, DEBAJO DEL PUCHERO. Es la imagen que todo el
            // mundo entiende sin que nadie se la explique, y es lo que hace
            // que "fuego bajo" en el rótulo sea una descripción y no una
            // promesa.
            var hogarGo = new GameObject("CrisolHogar");
            hogarGo.transform.SetParent(transform, false);
            hogarGo.transform.position = new Vector3(posCuerpo.x, (_baseY - HogarFilas * 0.5f) * c, 0f);
            _brasasHogar = MaquinariaSprites.CrearCapa(hogarGo.transform, "Brasas",
                MaquinariaSprites.LechoBrasas(CamaraAncho, HogarFilas), 17, CamaraAncho * c, HogarFilas * c);

            // ---- LA BOCA: guías de latón forrando la rampa de piedra (una
            // por fila y lado: 22 tejas pequeñas, creadas una vez) + el labio
            // que la corona. Sin ellas la boca embudada es piedra sobre
            // piedra y se pierde contra la roca del fondo.
            var bocaGo = new GameObject("CrisolBoca");
            bocaGo.transform.SetParent(transform, false);
            bocaGo.transform.position = Vector3.zero;
            var teja = MaquinariaSprites.Solido();
            for (int i = 0; i < BocaFilas; i++)
            {
                int y = _bocaY0 + i;
                int vuelo = VueloEnFila(i);
                Color tono = (i % 2 == 0) ? new Color(0.76f, 0.59f, 0.29f, 1f) : new Color(0.55f, 0.42f, 0.20f, 1f);
                var izq = MaquinariaSprites.CrearCapa(bocaGo.transform, "GuiaIzq" + i, teja, 19, 1f * c, 1f * c);
                izq.transform.position = new Vector3((_camX0 - vuelo - 0.5f) * c, (y + 0.5f) * c, 0f);
                izq.color = tono;
                var der = MaquinariaSprites.CrearCapa(bocaGo.transform, "GuiaDer" + i, teja, 19, 1f * c, 1f * c);
                der.transform.position = new Vector3((_camX1 + vuelo + 1.5f) * c, (y + 0.5f) * c, 0f);
                der.color = tono * 0.8f;
            }
            int spanLabio = CamaraAncho + 2 * BocaVuelo + 2 * MuroGrosor; // 29
            var labioGo = new GameObject("CrisolLabio");
            labioGo.transform.SetParent(transform, false);
            labioGo.transform.position = new Vector3(_centroCamara.x, (_bocaY1 + 1f) * c, 0f);
            MaquinariaSprites.CrearCapa(labioGo.transform, "Sprite", MaquinariaSprites.LabioBoca(spanLabio, 3), 20, spanLabio * c, 3f * c);

            // ---- Chimenea + bocanadas (solo mientras arde combustible).
            var chimeneaGo = new GameObject("CrisolChimenea");
            chimeneaGo.transform.SetParent(transform, false);
            chimeneaGo.transform.position = new Vector3((_camX1 + BocaVuelo - 0.5f) * c, (_bocaY1 + 6f) * c, 0f);
            MaquinariaSprites.CrearCapa(chimeneaGo.transform, "Sprite", MaquinariaSprites.Chimenea(3), 19, 3f * c, 10f * c);
            for (int i = 0; i < HumoPuffs; i++)
            {
                var humoGo = new GameObject("Humo" + i);
                humoGo.transform.SetParent(transform, false);
                var sr = MaquinariaSprites.CrearCapa(humoGo.transform, "Sprite", MaquinariaSprites.Humo(), 23, 3f * c, 3f * c);
                sr.color = new Color(0.82f, 0.80f, 0.78f, 0f);
                _humo[i] = sr;
            }

            // ---- Burbujas dentro de la cámara mientras corre la hornada.
            for (int i = 0; i < Burbujas; i++)
            {
                var bgo = new GameObject("Burbuja" + i);
                bgo.transform.SetParent(transform, false);
                var sr = MaquinariaSprites.CrearCapa(bgo.transform, "Sprite", MaquinariaSprites.Burbuja(), 21, 1.2f * c, 1.2f * c);
                sr.color = new Color(1f, 0.9f, 0.7f, 0f);
                _burbujas[i] = sr;
            }

            // ---- EL CESTO DEL BRASERO, aparte y con otra silueta.
            int muroCesto = MuroGrosor + VueloCesto;       // 4
            int spanCesto = BraseroAncho + 2 * muroCesto;  // 13
            int altoCesto = BraseroAlto + 2;               // 8
            float anchoCestoW = spanCesto * c, altoCestoW = altoCesto * c;
            var cestoGo = new GameObject("CrisolBrasero");
            cestoGo.transform.SetParent(transform, false);
            cestoGo.transform.position = new Vector3((_braX0 + BraseroAncho * 0.5f) * c, (_baseY + altoCesto * 0.5f) * c, 0f);
            MaquinariaSprites.CrearCapa(cestoGo.transform, "Cesto",
                MaquinariaSprites.CestoBrasero(spanCesto, altoCesto, muroCesto, 1), 18, anchoCestoW, altoCestoW);
            _destelloCesto = MaquinariaSprites.CrearCapa(cestoGo.transform, "AcuseCesto",
                MaquinariaSprites.MarcoBandeja(spanCesto, altoCesto), 22, anchoCestoW, altoCestoW);
            _destelloCesto.color = new Color(1f, 0.85f, 0.6f, 0f);

            var cestoHogarGo = new GameObject("CrisolBraseroHogar");
            cestoHogarGo.transform.SetParent(transform, false);
            cestoHogarGo.transform.position = new Vector3((_braX0 + BraseroAncho * 0.5f) * c, (_baseY - HogarFilas * 0.5f) * c, 0f);
            _brasasCesto = MaquinariaSprites.CrearCapa(cestoHogarGo.transform, "Brasas",
                MaquinariaSprites.LechoBrasas(BraseroAncho, HogarFilas), 17, BraseroAncho * c, HogarFilas * c);
            _brasasCesto.color = new Color(0.16f, 0.14f, 0.13f, 1f); // ARRANCA APAGADO (mandato 4): frío y vacío.
        }

        private void ActualizarVisual()
        {
            float c = SimRenderer.CellWorldSize;
            bool corriendo = _fase == Fase.Corriendo;
            float t = corriendo ? Mathf.Clamp01(_hornadaT / HornadaSegundos) : 0f;

            // El hogar: rescoldo tenue en reposo, sube con la hornada.
            if (_brasasHogar != null)
            {
                float pulso = 0.82f + 0.18f * Mathf.Sin(Time.time * (corriendo ? 6.5f : 2.0f));
                // (segunda pasada) EN FRÍO, CASI NEGRO. Con 0.20, el hogar
                // apagado seguía siendo un puñado de puntos rojos, y un fuego
                // que se ve encendido cuando NO lo está miente sobre el estado
                // de la máquina -- el mismo pecado que el "cargadme
                // combustible" del 26. 0.06 deja un rescoldo que solo se
                // adivina de cerca.
                float intensidad = corriendo ? Mathf.Lerp(0.30f, 1f, t) : (_fase == Fase.Lista ? 0.18f : 0.06f);
                // TERCERA PASADA (visto jugando otra vez): el color se
                // interpola DESDE EL CARBÓN APAGADO, no desde una base
                // naranja. La fórmula anterior partía de r=0.55 aunque la
                // intensidad fuese 0.06, así que el hogar frío seguía siendo
                // un puñado de ascuas rojas -- o sea que la máquina seguía
                // diciendo "estoy encendida" cuando no lo estaba, que es
                // exactamente el pecado que esta ronda vino a corregir.
                Color carbon = new Color(0.17f, 0.13f, 0.11f);
                Color fuego = new Color(1f, 0.55f, 0.18f);
                Color mezcla = Color.Lerp(carbon, fuego, intensidad);
                _brasasHogar.color = new Color(mezcla.r * pulso, mezcla.g * pulso, mezcla.b * pulso, 1f);
            }

            // El cesto: negro mientras no arda; blanco-naranja mientras arde.
            if (_brasasCesto != null)
            {
                if (_cestoArdiendo)
                {
                    float p = 0.8f + 0.2f * Mathf.Sin(Time.time * 9f);
                    _brasasCesto.color = new Color(1f, 0.62f * p, 0.24f * p, 1f);
                }
                else _brasasCesto.color = new Color(0.16f, 0.14f, 0.13f, 1f);
            }

            if (_latidoTrabajo != null)
                _latidoTrabajo.color = new Color(1f, 0.55f, 0.18f, _pulsoTrabajo.AlfaTrabajo * 0.55f);
            if (_destelloCamara != null)
                _destelloCamara.color = new Color(1f, 1f, 0.9f, _acuseCamara.Alfa);
            if (_destelloCesto != null)
                _destelloCesto.color = new Color(1f, 0.85f, 0.6f, _acuseCesto.Alfa);

            // Burbujas: solo mientras corre, subiendo por la cámara.
            for (int i = 0; i < Burbujas; i++)
            {
                var sr = _burbujas[i];
                if (sr == null) continue;
                if (!corriendo) { sr.color = new Color(1f, 0.9f, 0.7f, 0f); continue; }
                float fase = Mathf.Repeat(Time.time * 0.55f + i / (float)Burbujas, 1f);
                float px = _camX0 + 1.5f + (CamaraAncho - 3f) * ((i * 2.7f) % 1f);
                float py = _camY0 + fase * (CamaraAlto - 1f);
                sr.transform.position = new Vector3(px * c, py * c, 0f);
                sr.color = new Color(1f, 0.92f, 0.74f, (1f - fase) * 0.85f * Mathf.Lerp(0.4f, 1f, t));
            }

            // Humo: solo mientras el cesto arde de verdad (el verbo en el cuerpo).
            for (int i = 0; i < HumoPuffs; i++)
            {
                var sr = _humo[i];
                if (sr == null) continue;
                if (!_cestoArdiendo) { sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f); continue; }
                float fase = Mathf.Repeat(Time.time / HumoCicloSeg + i / (float)HumoPuffs, 1f);
                sr.transform.position = _humoOrigen + new Vector3(Mathf.Sin(fase * Mathf.PI * 2f + i) * c * 1.2f, fase * c * 12f, 0f);
                sr.transform.localScale = Vector3.one * (0.6f + fase * 1.4f) * (c * 3f) / sr.sprite.rect.width;
                sr.color = new Color(0.82f, 0.80f, 0.78f, (1f - fase) * 0.7f);
            }

            if (_resalte != null)
            {
                float objetivo = EstaEnfocada() ? 0.55f + 0.20f * Mathf.Sin(Time.time * 4f) : 0f;
                _alfaResalte = Mathf.MoveTowards(_alfaResalte, objetivo, 6f * Time.deltaTime);
                _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, _alfaResalte);
            }
        }

        // =================================================================
        // RÓTULOS (español latino, tuteo — mandato 6)
        // =================================================================
        private string _aviso;
        private Color _avisoColor = UiStyles.Aviso;
        private float _avisoHasta;

        private void Rotular(string texto, Color color)
        {
            _aviso = texto;
            _avisoColor = color;
            _avisoHasta = texto != null ? Time.time + 3.5f : 0f;
        }

        /// <summary>
        /// El rótulo de la CÁMARA. Prioridades, en orden:
        ///  1) un aviso reciente (te acabo de decir por qué no ha pasado nada);
        ///  2) la hornada en curso, con su verbo y su cuenta atrás;
        ///  3) hornada lista -- el resultado te espera;
        ///  4) hay carga y no está encendido -- E;
        ///  5) reposo vacío: NO PIDE NADA (el fallo del 26), describe.
        /// </summary>
        private string EtiquetaCamara()
        {
            if (_aviso != null && Time.time < _avisoHasta) return _aviso;
            if (_fase == Fase.Corriendo)
            {
                int quedan = Mathf.CeilToInt(Mathf.Max(0f, HornadaSegundos - _hornadaT));
                return _hornadaVerbo + "… " + quedan + "s";
            }
            if (_fase == Fase.Lista) return "hornada lista · recógela con el frasco";
            if (_camaraTieneAlgo) return "cargado · E para encender la hornada";
            return "fuego bajo · vierte y prueba";
        }

        private string EtiquetaCesto()
        {
            if (_cestoArdiendo) return "ardiendo";
            if (_fuelMat == MaterialId.Empty) return "brasero · vacío";
            string nombre = _conocimiento != null ? _conocimiento.NombreParaHud(_fuelMat) : "???";
            return nombre + " · listo para arder";
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return;

            float cercEstado = UiStyles.Cercania(_centroCamara, _player, RangoEstadoPleno, RangoEstadoDesvanece);
            float cercNombre = UiStyles.Cercania(_centroCamara, _player, RangoNombrePleno, RangoNombreDesvanece);
            float cercCesto = UiStyles.Cercania(_centroBrasero, _player, RangoEstadoPleno, RangoEstadoDesvanece);
            if (cercEstado <= 0f && cercNombre <= 0f && cercCesto <= 0f) return;
            if (!_yaConocida && cercNombre >= 0.98f) _yaConocida = true;

            UiStyles.Preparar();

            if (cercEstado > 0f)
            {
                Color color = _fase == Fase.Corriendo ? UiStyles.Peligro
                            : (_fase == Fase.Lista ? UiStyles.Exito
                            : (_aviso != null && Time.time < _avisoHasta ? _avisoColor : UiStyles.Aviso));
                UiStyles.PlacaMundo(_centroBoca, EtiquetaCamara(),
                    new Color(color.r, color.g, color.b, color.a * cercEstado), -UiStyles.S(6f));
            }

            // La chapa del brasero cuelga DE SU PROPIA BOCA, nunca de la del
            // crisol: el playtest 26 puso los dos mensajes en el mismo sitio y
            // por eso "cargadme combustible" parecía hablar de la cubeta.
            if (cercCesto > 0f)
            {
                Color colorCesto = _cestoArdiendo ? UiStyles.Peligro : UiStyles.TextoTenue;
                UiStyles.PlacaMundo(_centroBrasero, EtiquetaCesto(),
                    new Color(colorCesto.r, colorCesto.g, colorCesto.b, colorCesto.a * cercCesto), -UiStyles.S(24f));
            }

            if (!_yaConocida && cercNombre > 0f)
            {
                Color tenue = UiStyles.TextoTenue;
                UiStyles.PlacaMundo(_centroBoca, ChapaNombre, new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercNombre), -UiStyles.S(23f));
            }

            if (MachineFocus.MostrarPromptE && EstaEnfocada() && !UiStyles.RatonOcupado && _fase != Fase.Corriendo)
            {
                UiStyles.PlacaMundo(_centroBoca, "E — encender la hornada",
                    new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, cercNombre), -UiStyles.S(23f));
            }
        }
    }
}
