using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// LA COLUMNA DE ENSAYO — archivo NUEVO del PLAYTEST 27
    /// (docs/CONTRATO_TALLER_GRANDE.md, mandato 5).
    ///
    /// =====================================================================
    /// POR QUÉ NACE ESTE ARCHIVO
    /// =====================================================================
    /// La Columna era la única estación SIN clase propia: el playtest 26 la
    /// dejó como pura mampostería en `Sim/SimLevelBuilder.cs` porque aquel
    /// encargo no podía crear archivos. El resultado fue exactamente lo que
    /// Cesar reportó: *"si se le puede llamar así a esa escalera sin
    /// terminar, es inentendible; aún no consigo saber qué hace; NO TIENE UN
    /// VERBO; sus materiales reaccionan con otros"*.
    ///
    /// Cuatro problemas, y tres de ellos no se podían arreglar sin una clase:
    ///  1. **NO TENÍA VERBO.** Ninguna otra estación se explica con texto,
    ///     pero todas tienen un rótulo de estado y un prompt de foco. La
    ///     Columna no tenía nada porque no había nadie que pudiera dibujarlo.
    ///     Ahora su verbo es **OBSERVAR**: al acercarte dice "columna de
    ///     ensayo — deja caer y observa", y con E te LEE EN VOZ ALTA lo que
    ///     hay dentro de arriba abajo ("aceite sobre agua sobre arena"). Es
    ///     el único aparato del taller cuyo resultado es una FRASE, porque su
    ///     mecánica (estratificar, disolver, flotar) ya ocurre sola: lo que
    ///     falta no es que pase algo, es leerlo.
    ///  2. **SUS MATERIALES REACCIONABAN CON OTROS.** Bug real, y de los
    ///     caros: los muros eran <see cref="MaterialId.Crystal"/>, que ES
    ///     reactivo con el Azoth del núcleo de leyes -- la columna podía
    ///     literalmente disolverse durante un experimento. Corregido en
    ///     `SimLevelBuilder.BuildColumnaEnsayo`: los muros son
    ///     <see cref="MaterialId.Stone"/>, que R2 del sorteo de química
    ///     excluye del pool de reactivos en TODA seed, y que
    ///     `SimLevelBuilder.ObraDelTaller` protege del cincel.
    ///  3. **NO PARECÍA VIDRIO** (era "una escalera sin terminar": dos líneas
    ///     verdes de 1 celda separadas por 3 de aire). El VIDRIO pasa a ser
    ///     un sprite translúcido con brillo diagonal
    ///     (<see cref="MaquinariaSprites.VidrioPanel"/>) DELANTE del fondo y
    ///     DETRÁS del mundo (sortingOrder <see cref="OrdenVidrio"/> = -7,
    ///     entre el fondo del taller en -10 y el sprite de la simulación en
    ///     -5): la materia que cae dentro se dibuja ENCIMA del cristal, que es
    ///     precisamente lo que hace que se lea como cristal y no como una
    ///     lámina de plástico tapándolo todo.
    ///  4. **ERA MINÚSCULA.** 5x22 con 3 celdas de hueco. Ahora el fuste tiene
    ///     13 celdas de hueco por 34 de alto = **442 celdas** de cámara de
    ///     observación, con TANQUE en la base (donde se vierten los líquidos
    ///     y donde se lee la estratificación) y BOCA ABOCINADA arriba (donde
    ///     se dejan caer las muestras). Las dos bocas se distinguen por forma
    ///     y por altura, sin un solo cartel.
    ///
    /// TODA LA MAMPOSTERÍA LA TALLA `Sim/SimLevelBuilder.BuildColumnaEnsayo`
    /// (regla 47: una sola fuente de verdad del plano). Esta clase solo lee
    /// sus constantes y pone encima el vidrio, los zunchos y la voz.
    /// </summary>
    public sealed class ColumnaEnsayo : MonoBehaviour, IMaquinaInteractiva
    {
        /// <summary>Entre el fondo del taller (-10) y el sprite de la simulación (-5): ver el punto 3 del docblock.</summary>
        private const int OrdenVidrio = -7;

        private const float ProximityRange = 3.4f;
        /// <summary>(tercera pasada) 3.4 -&gt; 2.6 / 5.0 -&gt; 3.6: la línea del verbo se leía desde media pantalla, encima de la Prensa y del Banco. Un rótulo que se ve siempre deja de señalar a nada.</summary>
        private const float RangoNombrePleno = 2.6f;
        private const float RangoNombreDesvanece = 3.6f;
        private const float RotuloSeg = 6f;
        /// <summary>Cuántas capas distintas nombra la lectura como mucho: tres es lo que cabe en una frase que se lee de un vistazo.</summary>
        private const int MaxCapasLeidas = 3;

        private AlkahestSim _sim;
        private Transform _player;
        private SubstanceKnowledge _conocimiento;

        private int _intX0, _intX1, _intY0, _intY1; // hueco interior del fuste.
        /// <summary>Centro del TANQUE (donde cuelgan los rótulos) y centro del FUSTE (el punto de foco). Son distintos a propósito: ver <see cref="PuntoFoco"/>.</summary>
        private Vector3 _centro, _centroFuste, _centroRotulo, _centroBoca;

        private string _lectura;
        private float _lecturaHasta;
        private SpriteRenderer _resalte;
        private float _alfaResalte;

        private readonly MaquinariaSprites.Destello _acuse = new MaquinariaSprites.Destello();
        private SpriteRenderer _destelloTanque;
        private int _celdasPrev;

        // Buffer de conteo REUTILIZADO (cero allocs en el sondeo por fila).
        private readonly int[] _conteoFila = new int[MaterialId.Count];

        /// <summary>
        /// (tercera pasada, visto jugando) EL FOCO ES EL CENTRO DEL FUSTE, no
        /// el del tanque. La columna mide 34 celdas de alto y el jugador la
        /// observa VOLANDO A SU ALTURA, no agachado en su base: con el foco en
        /// el tanque, a media columna el Banco de Chispa quedaba más cerca y
        /// se llevaba la E. Es la misma lección que MachineFocus ya aprendió
        /// con los grifos: el punto de foco de un aparato ALTO tiene que estar
        /// donde el jugador se pone para usarlo.
        /// </summary>
        public Vector3 PuntoFoco => _centroFuste;
        public float RangoFoco => ProximityRange;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap. La Columna no lleva ancla por parámetro: su sitio son las constantes `SimLevelBuilder.ColumnaX0/Ancho/Muro/Alto` (regla 47).</summary>
        public void Init(AlkahestSim sim, Transform player, SubstanceKnowledge conocimiento)
        {
            _sim = sim;
            _player = player;
            _conocimiento = conocimiento;

            _intX0 = SimLevelBuilder.ColumnaX0 + SimLevelBuilder.ColumnaMuro;
            _intX1 = SimLevelBuilder.ColumnaX0 + SimLevelBuilder.ColumnaAncho - 1 - SimLevelBuilder.ColumnaMuro;
            _intY0 = SimLevelBuilder.EstacionSueloY;
            _intY1 = _intY0 + SimLevelBuilder.ColumnaAlto - 1;

            float c = SimRenderer.CellWorldSize;
            _centro = new Vector3((_intX0 + (_intX1 - _intX0 + 1) * 0.5f) * c, (_intY0 + SimLevelBuilder.ColumnaTanqueAlto * 0.5f) * c, 0f);
            _centroFuste = new Vector3(_centro.x, (_intY0 + (_intY1 - _intY0 + 1) * 0.5f) * c, 0f);
            _centroRotulo = new Vector3(_centro.x, (_intY0 + SimLevelBuilder.ColumnaTanqueAlto + 3f) * c, 0f);
            _centroBoca = new Vector3(_centro.x, (_intY1 + SimLevelBuilder.ColumnaBocaFilas + 2f) * c, 0f);

            BuildVisual();
            MachineFocus.Registrar(this);
        }

        private void OnDestroy() => MachineFocus.Olvidar(this);

        private void BuildVisual()
        {
            float c = SimRenderer.CellWorldSize;
            int anchoInt = _intX1 - _intX0 + 1;
            int altoInt = _intY1 - _intY0 + 1;

            // ---- EL VIDRIO. Cubre el hueco entero del fuste, DETRÁS del
            // mundo: lo que caiga dentro se dibujará encima y se verá "a
            // través del cristal" (ver punto 3 del docblock).
            var vidrioGo = new GameObject("ColumnaVidrio");
            vidrioGo.transform.SetParent(transform, false);
            vidrioGo.transform.position = new Vector3(_centro.x, (_intY0 + altoInt * 0.5f) * c, 0f);
            MaquinariaSprites.CrearCapa(vidrioGo.transform, "Sprite",
                MaquinariaSprites.VidrioPanel(anchoInt, altoInt), OrdenVidrio, anchoInt * c, altoInt * c);

            // ---- ZUNCHOS DE LATÓN QUE CIÑEN EL FUSTE (segunda pasada del
            // playtest 27, visto jugando). La primera versión ponía un
            // nudillo a cada LADO cada 5 filas: catorce tacos en dos columnas
            // verticales, o sea **exactamente la "escalera sin terminar" que
            // Cesar odiaba** en el playtest 26, reinventada de cero. Un rasgo
            // que se repite en vertical a intervalos regulares SE LEE COMO
            // PELDAÑOS, da igual de qué esté hecho.
            //
            // Ahora son TRES bandas HORIZONTALES que cruzan la columna entera
            // (muros incluidos), a 1/4, 1/2 y 3/4 de la altura: un tonel se
            // ciñe con aros, y tres aros no se pueden confundir con una
            // escalera. Cruzan por delante del vidrio con alfa 0.8 -- se ve la
            // materia detrás, y ganan la lectura de "instrumento graduado".
            var teja = MaquinariaSprites.Solido();
            int spanFuste = SimLevelBuilder.ColumnaAncho;
            for (int i = 1; i <= 3; i++)
            {
                int y = _intY0 + (altoInt * i) / 4;
                var sr = MaquinariaSprites.CrearCapa(transform, "Zuncho" + i, teja, 19, (spanFuste + 2) * c, 1f * c);
                sr.transform.position = new Vector3(_centro.x, (y + 0.5f) * c, 0f);
                sr.color = new Color(0.78f, 0.61f, 0.31f, 0.80f);
            }

            // ---- EL TANQUE: marco de latón grueso alrededor de la base. Es
            // lo que dice "por aquí abajo se vierte y aquí abajo se mira".
            int spanTanque = SimLevelBuilder.ColumnaAncho;
            int altoTanque = SimLevelBuilder.ColumnaTanqueAlto + 1;
            var marco = MaquinariaSprites.MarcoBandeja(spanTanque, altoTanque);
            var tanqueGo = new GameObject("ColumnaTanque");
            tanqueGo.transform.SetParent(transform, false);
            tanqueGo.transform.position = new Vector3(_centro.x, (_intY0 - 1 + altoTanque * 0.5f) * c, 0f);
            _resalte = MaquinariaSprites.CrearCapa(tanqueGo.transform, "Resalte", marco, 14, spanTanque * c * 1.10f, altoTanque * c * 1.18f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);
            MaquinariaSprites.CrearCapa(tanqueGo.transform, "Marco", marco, 19, spanTanque * c, altoTanque * c);
            _destelloTanque = MaquinariaSprites.CrearCapa(tanqueGo.transform, "Acuse", marco, 22, spanTanque * c, altoTanque * c);
            _destelloTanque.color = new Color(0.85f, 1f, 0.95f, 0f);

            // ---- EL LABIO DE LA BOCA, coronando el abocinado de piedra: la
            // otra entrada, arriba, con otra forma. Igual que en el Crisol, el
            // embudo es la PIEDRA y el sprite solo lo remata (mandato 2).
            int spanBoca = SimLevelBuilder.ColumnaAncho + 2 * SimLevelBuilder.ColumnaBocaVuelo;
            var labioGo = new GameObject("ColumnaLabio");
            labioGo.transform.SetParent(transform, false);
            labioGo.transform.position = new Vector3(_centro.x, (_intY1 + SimLevelBuilder.ColumnaBocaFilas + 0.5f) * c, 0f);
            MaquinariaSprites.CrearCapa(labioGo.transform, "Sprite", MaquinariaSprites.LabioBoca(spanBoca, 3), 20, spanBoca * c, 3f * c);
        }

        private bool EstaEnfocada() => MachineFocus.EsFoco(this, _player);

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return;

            SondearTanque();
            _acuse.Avanzar(Time.deltaTime);

            // (playtest 28, reporte de Cesar) "pinté un sólido y no caía".
            // Regla 7 de CLAUDE.md: StaticSolid no tiene gravedad en el motor
            // (Cristal, hielo, y AHORA también templado/recocido/compacto/
            // cerámico -- media tabla de estados del retículo). Dentro del
            // fuste de ESTA columna eso rompía el verbo entero ("deja caer y
            // observa"... y no caía). Mismo remedio documentado que la Tolva
            // (DeliveryChute.ArrastreTick): la columna ARRASTRA hacia abajo,
            // celda sobre vacío, todo lo que esté en su fuste. Solo entra en
            // hueco VACÍO: la estratificación líquida (denso desplaza a
            // ligero) sigue siendo trabajo de la sim, intacta.
            _accArrastre += Time.deltaTime;
            while (_accArrastre >= ArrastreDt)
            {
                _accArrastre -= ArrastreDt;
                ArrastreTick();
            }

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && EstaEnfocada())
            {
                Observar();
                MachineFocus.RegistrarUsoE();
            }

            if (_destelloTanque != null) _destelloTanque.color = new Color(0.85f, 1f, 0.95f, _acuse.Alfa);
            if (_resalte != null)
            {
                float objetivo = EstaEnfocada() ? 0.55f + 0.20f * Mathf.Sin(Time.time * 4f) : 0f;
                _alfaResalte = Mathf.MoveTowards(_alfaResalte, objetivo, 6f * Time.deltaTime);
                _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, _alfaResalte);
            }
        }

        private float _accArrastre;
        private const float ArrastreDt = 1f / 15f; // 15 Hz: caída visible y serena, no teletransporte.

        /// <summary>Ver el comentario de Update: la gravedad prestada del fuste, calcada de DeliveryChute.ArrastreTick (regla 7).</summary>
        private void ArrastreTick()
        {
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            for (int x = _intX0; x <= _intX1; x++)
            {
                for (int y = _intY0 + 1; y <= _intY1; y++)
                {
                    int idx = CellGrid.Idx(x, y);
                    if (grid.mat[idx] == MaterialId.Empty) continue;
                    int belowIdx = CellGrid.Idx(x, y - 1);
                    if (grid.mat[belowIdx] != MaterialId.Empty) continue;
                    grid.SwapCells(idx, belowIdx);
                    grid.WakeChunk(x, y, tick);
                    grid.WakeChunk(x, y - 1, tick);
                }
            }
        }

        private void SondearTanque()
        {
            var grid = _sim.Grid;
            int n = 0;
            int yTope = Mathf.Min(_intY1, _intY0 + SimLevelBuilder.ColumnaTanqueAlto - 1);
            for (int y = _intY0; y <= yTope; y++)
                for (int x = _intX0; x <= _intX1; x++)
                    if (grid.GetMat(x, y) != MaterialId.Empty) n++;
            if (n > _celdasPrev) _acuse.Disparar();
            _celdasPrev = n;
        }

        /// <summary>
        /// EL VERBO: recorre el fuste DE ARRIBA ABAJO, se queda con el
        /// material dominante de cada fila y construye la frase de las capas
        /// distintas que encuentra ("aceite sobre agua sobre arena"). Solo
        /// corre al pulsar E -- nunca por frame -- así que puede permitirse
        /// construir una cadena.
        ///
        /// Nombra por <see cref="SubstanceKnowledge.NombreParaHud"/>, así que
        /// lo innominado sale como "???" hasta que el jugador lo bautice
        /// (reglas 13/17: el aparato no puede revelar lo que el HUD todavía
        /// esconde -- y aun así la frase sirve, porque lo que enseña es el
        /// ORDEN, no la identidad).
        /// </summary>
        private void Observar()
        {
            var grid = _sim.Grid;
            var sb = new System.Text.StringBuilder(64);
            byte ultima = MaterialId.Empty;
            int capas = 0;

            for (int y = _intY1; y >= _intY0 && capas < MaxCapasLeidas; y--)
            {
                System.Array.Clear(_conteoFila, 0, _conteoFila.Length);
                int mejor = 0;
                byte dominante = MaterialId.Empty;
                for (int x = _intX0; x <= _intX1; x++)
                {
                    byte m = grid.GetMat(x, y);
                    if (m == MaterialId.Empty || m >= MaterialId.Count) continue;
                    _conteoFila[m]++;
                    if (_conteoFila[m] > mejor) { mejor = _conteoFila[m]; dominante = m; }
                }
                // Una fila con cuatro gotas sueltas no es una capa: pedimos que
                // el dominante ocupe al menos un tercio del ancho del fuste.
                if (dominante == MaterialId.Empty || mejor * 3 < (_intX1 - _intX0 + 1)) continue;
                if (dominante == ultima) continue;

                if (capas > 0) sb.Append(" sobre ");
                sb.Append(_conocimiento != null ? _conocimiento.NombreParaHud(dominante) : "???");
                ultima = dominante;
                capas++;
            }

            // (playtest 28) "¿'una sola capa'? no entiendo" (Cesar): la
            // lectura de UNA capa ahora dice qué hacer con ella -- la columna
            // compara, y con una sola cosa dentro no hay comparación todavía.
            _lectura = capas == 0
                ? "la columna está vacía · deja caer algo por arriba"
                : (capas == 1
                    ? "por ahora una sola capa (" + sb + ") · agrega otra cosa y E de nuevo: te digo cuál queda encima"
                    : "de arriba abajo: " + sb);
            _lecturaHasta = Time.time + RotuloSeg;
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return;

            float cercania = UiStyles.Cercania(_centroFuste, _player, RangoNombrePleno, RangoNombreDesvanece);
            if (cercania <= 0f) return;

            UiStyles.Preparar();

            if (_lectura != null && Time.time < _lecturaHasta)
            {
                UiStyles.PlacaMundo(_centroRotulo, _lectura,
                    new Color(UiStyles.Frio.r, UiStyles.Frio.g, UiStyles.Frio.b, cercania), -UiStyles.S(6f));
                return;
            }

            // El verbo, sobrio, en el estilo de rótulo de foco del resto del
            // taller (mandato 5: "que el estar cerca muestre una línea sobria").
            Color tenue = UiStyles.TextoTenue;
            UiStyles.PlacaMundo(_centroRotulo, "columna de ensayo — deja caer y observa",
                new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercania), -UiStyles.S(6f));

            if (MachineFocus.MostrarPromptE && EstaEnfocada() && !UiStyles.RatonOcupado)
            {
                UiStyles.PlacaMundo(_centroRotulo, "E — observar",
                    new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, cercania), -UiStyles.S(23f));
            }
        }
    }
}
