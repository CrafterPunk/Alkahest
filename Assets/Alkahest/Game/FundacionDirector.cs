using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// (RONDA 60, GDD v0.3 §5 -- EL INICIO OSCURO, greybox) El director de la
    /// FUNDACIÓN: los primeros minutos de TEN THOUSAND YEARS, en gris, para
    /// medir la reacción de testers reales antes de gastar arte (decisión de
    /// Cesar, respuestas a la v0.2 del GDD).
    ///
    /// Guion (v0 = beats 0-3 + fin; el fogón propio, el vidrio y el estante
    /// llegan en la siguiente ronda de F1):
    ///   0. MIRAR    -- oscuridad. Solo se ve el rincón del Maestro: sus brasas
    ///                  y una GOTERA que cae sobre ellas y se evapora. La
    ///                  primera reacción del juego ocurre SOLA, antes de tocar
    ///                  nada. Sin HUD (DayCycle.HudSilenciado, gate propio).
    ///   1. SALUDO   -- acercarse al Maestro. Te PRESTA su frasco viejo (el
    ///                  HUD despierta). Primer verbo: social.
    ///   2. GOTERA   -- "atrápala": aspirar el goteo al vuelo (15 celdas) y
    ///                  traérselo. Al entregarlo, el Maestro DOMESTICA la
    ///                  gotera: deja de gotear y nace TU GRIFO sobre el cuenco.
    ///                  Los fenómenos no se eliminan: se domestican.
    ///   3. BARRO    -- el polvo de la orilla + el agua del cuenco = barbotina
    ///                  (química REAL de la sim: arcilla soluble por decreto).
    ///                  Traerle 12. Cierra el greybox.
    ///
    /// DISEÑO DE COMPLETADO: los pedidos se encolan en OrderSystem solo como
    /// CHECKLIST visible (EncolarPedidoGuiado); aquí no hay Buzón todavía, así
    /// que el director actualiza Order.Progreso él mismo espejando el contenido
    /// del frasco, y completa el beat por PROXIMIDAD al Maestro + contenido
    /// suficiente (el Maestro "toma" el material con Flask.Extraer). Cuando la
    /// fundación gane su Buzón de Entrada (GDD §6), este atajo se retira.
    ///
    /// LA VIÑETA: un óvalo de oscuridad centrado entre el jugador y las brasas
    /// cuyo radio crece un escalón por beat (greybox de la "ejecución visual
    /// del inicio oscuro" que Cesar quiere ver en gris -- GDD §14). Se dibuja
    /// con GUI.depth=50: DETRÁS de todos los HUD (depth 0) y de la voz del
    /// Maestro, encima del mundo.
    ///
    /// LAS BRASAS: 5 celdas mantenidas al rojo (temp >= BrasaRaw) cada frame,
    /// con WakeChunk explícito (regla 55b: un proceso térmico que no mueve
    /// materia debe despertarse solo). El chisporroteo es agua real de la
    /// gotera evaporándose contra ellas -- cero teatro, pura sim.
    /// </summary>
    public sealed class FundacionDirector : MonoBehaviour
    {
        private enum Beat { Mirar, Saludo, Gotera, Barro, Fogon, Vidrio, Estante, Fin }

        // (gate de DayCycle.DetectarPrimeraAccion) Mientras sea false, moverse
        // NO despierta el HUD en ModoFundacion: el encuadre cero se queda
        // limpio hasta que el Maestro presta el frasco. Estática por el mismo
        // motivo que las demás banderas de director (DayCycle la lee sin
        // referencia); se baja en OnDestroy.
        public static bool HudPermitido;

        private const byte BrasaRaw = 165;          // bien por encima de la ebullición: la gota chisporrotea y sube como vapor.
        private const float DripSegundos = 1.15f;   // una gota por segundo y pico: legible, hipnótica, no inunda.
        private const float FuegoSegundos = 0.9f;   // lengüita de fuego ocasional sobre las brasas (muere sola sin combustible: parpadeo).
        private const int CeldasGotera = 15;        // el pedido del beat 2.
        private const int CeldasBarro = 12;         // el pedido del beat 3.
        private const float DistCharla = 16f;       // celdas de distancia al centro de la mesa para "estar con el Maestro".
        // (RONDA 61, beats 4-6) EL FUEGO PROPIO, EL VIDRIO Y EL ESTANTE:
        private const int CeldasFogon = 12;         // turba que hay que cargar en el hogar vacío para encenderlo.
        private const int CeldasVidrio = 8;         // vidrio del beat 5.
        private const int TurbaPrestada = 20;       // el puñado del morral del Maestro (la veta asomada da el resto).
        // (VERIFICADO EN VIVO ronda 61, TRES iteraciones -- lección regla 50
        // en cascada) A 175 raw la turba del hogar se FUNDÍA; a 152, también;
        // y a 132 con una celda de fuego de ignición, TAMBIÉN: la celda de
        // Fire nace a su temperatura estable (~240) y la cascada térmica
        // (fundido caliente funde al vecino) gana a la combustión -- 13 de 14
        // turbas acabaron Fundido/Templado, cero ceniza, y hasta una arena se
        // fundió. La combustión abierta de la turba en pila NO es un camino
        // domable por temperatura de lecho en esta seed. SOLUCIÓN GREYBOX
        // (misma filosofía que el brasero del Crisol real, que tampoco quema
        // su combustible celda a celda): el hogar encendido CONSUME su turba
        // a ceniza por cadencia (TickCrisolPrimitivo, regla 54: la combustión
        // deja su residuo), el lecho se queda apenas TIBIO (118: teatro
        // térmico, bajo toda fusión), y la cima que ve TryCruce es la del
        // frente de llama (152), constante del fuego, no de la celda.
        private const byte FogonRaw = 118;
        private const byte FogonCimaRaw = 152;
        private const float ConsumoTurbaSegundos = 0.7f; // cadencia turba->ceniza del hogar encendido.
        private const float CrisolPrimitivoSegundos = 0.5f; // cadencia del cruce arena+ceniza->vidrio dentro del hogar encendido (1 celda por pulso: se VE trabajar).

        private AlkahestSim _sim;
        private OrderSystem _orders;
        private Flask _flask;
        private SubstanceKnowledge _saber; // (ronda 61) para el StorageRack del beat 6.
        private Transform _aprendiz;

        private Beat _beat = Beat.Mirar;
        private float _tBeat;          // segundos dentro del beat actual.
        private float _dripTimer, _fuegoTimer;
        private bool _goteraActiva = true;
        private bool _pistaMirarDicha;
        private bool _pedidoEncolado;  // el pedido del beat actual ya está en OrderSystem.
        // (ronda 61) El fuego propio y su crisol primitivo:
        private bool _fogonEncendido;
        private float _crisolTimer;
        private static readonly byte Barbotina = MaterialId.MatDe(1, EstadoMateria.Solucion);
        private static readonly byte ArenaSilice = MaterialId.MatDe(0, EstadoMateria.Polvo);
        private static readonly byte Turba = MaterialId.MatDe(Universe.SemillaCeroBaseTurbaIdx, EstadoMateria.Polvo);

        // La voz del Maestro (reutiliza el panel estático de SemillaCero, pt55).
        private string _maestroTexto;
        private float _maestroHasta;

        // La viñeta.
        private Texture2D _vineta;
        private float _radio;          // radio actual en px (lerp suave hacia _radioObjetivo).
        private float _radioObjetivo;

        public void Init(AlkahestSim sim, OrderSystem orders, Flask flask, SubstanceKnowledge saber, Transform aprendiz)
        {
            _sim = sim;
            _orders = orders;
            _flask = flask;
            _saber = saber;
            _aprendiz = aprendiz;
            HudPermitido = false;
            _radioObjetivo = UiStyles.S(240f);
            _radio = _radioObjetivo;
        }

        private void OnDestroy()
        {
            HudPermitido = false;
            if (_vineta != null) Destroy(_vineta);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;

            MantenerBrasas();
            TickGotera();

            _tBeat += Time.deltaTime;
            switch (_beat)
            {
                case Beat.Mirar: TickMirar(); break;
                case Beat.Saludo: TickSaludo(); break;
                case Beat.Gotera: TickPedidoDeEntrega(MaterialId.Water, CeldasGotera, CompletarGotera); break;
                case Beat.Barro: TickPedidoDeEntrega(Barbotina, CeldasBarro, CompletarBarro); break;
                case Beat.Fogon: TickFogon(); break;
                case Beat.Vidrio: TickPedidoDeEntrega(MaterialId.VidrioVerde, CeldasVidrio, CompletarVidrio); break;
                case Beat.Estante: TickEstante(); break;
                case Beat.Fin: break;
            }

            if (_fogonEncendido) TickCrisolPrimitivo();

            _radio = Mathf.Lerp(_radio, _radioObjetivo, Time.deltaTime * 1.6f);
        }

        // ------------------------------------------------------------------
        // Las brasas y la gotera (la física de fondo, viva en todos los beats).
        // ------------------------------------------------------------------
        private void MantenerBrasas()
        {
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            for (int x = SimLevelBuilder.FundacionBrasasX0; x <= SimLevelBuilder.FundacionBrasasX1; x++)
            {
                int idx = CellGrid.Idx(x, SimLevelBuilder.FundacionBrasasY);
                if (grid.temp[idx] < BrasaRaw) grid.temp[idx] = BrasaRaw;
            }
            grid.WakeChunk(SimLevelBuilder.FundacionBrasasX0, SimLevelBuilder.FundacionBrasasY, tick);
            grid.WakeChunk(SimLevelBuilder.FundacionBrasasX1, SimLevelBuilder.FundacionBrasasY, tick);

            // (ronda 61) El FUEGO PROPIO, una vez encendido, se mantiene igual
            // que las brasas del Maestro -- pero más caliente (FogonRaw=175 >=
            // pleno de los cruces): la turba cargada arde de verdad, deja
            // ceniza de verdad, y el vidrio es posible.
            if (_fogonEncendido)
            {
                for (int x = SimLevelBuilder.FundacionFogonX0; x <= SimLevelBuilder.FundacionFogonX1; x++)
                {
                    int idx = CellGrid.Idx(x, SimLevelBuilder.FundacionFogonY);
                    if (grid.temp[idx] < FogonRaw) grid.temp[idx] = FogonRaw;
                }
                grid.WakeChunk(SimLevelBuilder.FundacionFogonX0, SimLevelBuilder.FundacionFogonY, tick);
                grid.WakeChunk(SimLevelBuilder.FundacionFogonX1, SimLevelBuilder.FundacionFogonY, tick);
            }
        }

        /// <summary>Cuenta celdas de `mat` dentro del hogar del jugador (lecho + 5 filas de aire encima).</summary>
        private int ContarEnFogon(byte mat, out int cx, out int cy)
        {
            cx = -1; cy = -1;
            int n = 0;
            var grid = _sim.Grid;
            for (int x = SimLevelBuilder.FundacionFogonX0; x <= SimLevelBuilder.FundacionFogonX1; x++)
                for (int y = SimLevelBuilder.FundacionFogonY; y <= SimLevelBuilder.FundacionFogonY + 5; y++)
                    if (grid.GetMat(x, y) == mat)
                    {
                        n++;
                        if (cx < 0) { cx = x; cy = y; }
                    }
            return n;
        }

        /// <summary>
        /// (ronda 61) EL CRISOL PRIMITIVO: el hogar encendido aplica el cruce
        /// REAL arena de sílice + ceniza -> vidrio de botella
        /// (Universe.TryCruce, la MISMA tabla que usa el Crisol de verdad --
        /// cero física inventada; los decretos de la seed de autor ya
        /// aplicaron, ver AlkahestSim.CrearMundoInterno). Una celda por pulso
        /// (0.5s): la transformación SE VE trabajar. La ceniza la produce la
        /// propia turba ardiendo en el lecho -- el jugador solo suma la arena
        /// de la orilla.
        /// </summary>
        private float _consumoTimer;

        private void TickCrisolPrimitivo()
        {
            // 1) EL HOGAR CONSUME SU COMBUSTIBLE: turba -> ceniza, una celda
            // por cadencia (ver el docblock de FogonRaw: la combustión abierta
            // en pila no es domable -- el consumo dirigido ES la combustión
            // del hogar, y deja el residuo que manda la regla 54).
            _consumoTimer -= Time.deltaTime;
            if (_consumoTimer <= 0f)
            {
                _consumoTimer = ConsumoTurbaSegundos;
                if (ContarEnFogon(Turba, out int tx, out int ty) > 0)
                {
                    var g = _sim.Grid;
                    g.SetCell(tx, ty, MaterialId.Ash);
                    g.WakeChunk(tx, ty, _sim.Stepper != null ? _sim.Stepper.Tick : 0u);
                }
            }

            // 2) EL CRUCE DEL VIDRIO.
            _crisolTimer -= Time.deltaTime;
            if (_crisolTimer > 0f) return;
            _crisolTimer = CrisolPrimitivoSegundos;

            int nArena = ContarEnFogon(ArenaSilice, out int ax, out int ay);
            int nCeniza = ContarEnFogon(MaterialId.Ash, out int cx, out int cy);
            if (nArena == 0 || nCeniza == 0) return;

            if (!Universe.TryCruce(ArenaSilice, MaterialId.Ash, FogonCimaRaw, out byte producto, out _, out _)) return;

            var grid = _sim.Grid;
            grid.SetCell(ax, ay, producto);          // la arena se vuelve vidrio (conserva su temp: sigue en el fuego).
            grid.SetCell(cx, cy, MaterialId.Empty);  // la ceniza se consume como fundente.
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            grid.WakeChunk(ax, ay, tick);
            grid.WakeChunk(cx, cy, tick);
        }

        private void TickGotera()
        {
            if (!_goteraActiva) return;

            _dripTimer -= Time.deltaTime;
            if (_dripTimer <= 0f)
            {
                _dripTimer = DripSegundos;
                // PaintStable (regla 29: la gotera CREA materia): la gota nace
                // estable, cae, toca las brasas al rojo y se evapora -- la
                // primera reacción del juego, sin una línea de teatro.
                _sim.PaintStable(SimLevelBuilder.FundacionGoteraX, SimLevelBuilder.FundacionGoteraDripY, 0, MaterialId.Water);
            }

            _fuegoTimer -= Time.deltaTime;
            if (_fuegoTimer <= 0f)
            {
                _fuegoTimer = FuegoSegundos;
                // Lengüita de fuego sin combustible: muere en unos ticks. El
                // parpadeo resultante es el "glow" del greybox.
                _sim.PaintStable(SimLevelBuilder.FundacionBrasasX0 + 2, SimLevelBuilder.FundacionBrasasY + 1, 0, MaterialId.Fire);
            }
        }

        // ------------------------------------------------------------------
        // Beats.
        // ------------------------------------------------------------------
        private float DistAlMaestro()
        {
            float celda = SimRenderer.CellWorldSize;
            float mx = (SimLevelBuilder.FundacionMesaX0 + SimLevelBuilder.FundacionMesaX1) * 0.5f * celda;
            float my = (SimLevelBuilder.FundacionMesaTopY + 2) * celda;
            return Vector2.Distance(_aprendiz.position, new Vector2(mx, my)) / celda;
        }

        private void TickMirar()
        {
            if (!_pistaMirarDicha && _tBeat > 20f)
            {
                _pistaMirarDicha = true;
                MaestroDice("Acércate, aprendiz. Deja que te vea.", 6f);
            }
            if (_tBeat > 4f && DistAlMaestro() < DistCharla)
            {
                _beat = Beat.Saludo; _tBeat = 0f;
                MaestroDice("Ah. Llegaste. El mundo se apagó hace mucho -- solo quedan este fuego y yo. Toma: mi frasco viejo. Te lo PRESTO. Me lo devuelves cuando sepas hacerte uno.", 11f);
                HudPermitido = true;
                DayCycle.DespertarHudFundacion();
                _radioObjetivo = UiStyles.S(340f);
            }
        }

        private void TickSaludo()
        {
            if (_tBeat < 8f) return; // dejar leer el saludo.
            _beat = Beat.Gotera; _tBeat = 0f; _pedidoEncolado = false;
            MaestroDice("Primer favor: esa gotera me va a apagar el fuego un día. Atrápala al vuelo con el frasco y tráeme " + CeldasGotera + " sorbos.", 9f);
        }

        /// <summary>
        /// El patrón de los beats 2 y 3: encolar el pedido una vez, espejar el
        /// progreso del checklist con el contenido REAL del frasco, y completar
        /// por proximidad + contenido (el Maestro toma el material).
        /// </summary>
        private void TickPedidoDeEntrega(byte mat, int cantidad, System.Action completar)
        {
            if (!_pedidoEncolado)
            {
                _pedidoEncolado = true;
                string texto = _beat switch
                {
                    Beat.Gotera => "Atrapa la gotera: aspira el agua al vuelo y tráeme " + cantidad + " sorbos.",
                    Beat.Vidrio => "Arena de la orilla y la ceniza que deja tu fuego, JUNTAS dentro del hogar encendido -- el fuego pleno decide. Aspira el vidrio que nazca y tráeme " + cantidad + ".",
                    _ => "El polvo de la orilla se bebe el agua y se hace crema -- barbotina, la llaman los alfareros. Vierte polvo en el cuenco y tráeme " + cantidad + ".",
                };
                _orders.EncolarPedidoGuiado(OrderType.Guiado, cantidad, 0, texto, targetMat: mat);
            }

            int enFrasco = _flask.GetCount(mat);
            if (_orders.ActiveOrders.Count > 0)
            {
                var o = _orders.ActiveOrders[0];
                int progreso = Mathf.Min(enFrasco, cantidad);
                if (o.Progreso != progreso && !o.Completado) o.Progreso = progreso;
            }

            if (enFrasco >= cantidad && DistAlMaestro() < DistCharla)
            {
                _flask.Extraer(mat, cantidad, out _);
                if (_orders.ActiveOrders.Count > 0) _orders.ActiveOrders[0].Completado = true;
                completar();
            }
        }

        private void CompletarGotera()
        {
            _beat = Beat.Barro; _tBeat = 0f; _pedidoEncolado = false;
            _goteraActiva = false;

            // LA GOTERA DOMESTICADA: nace TU GRIFO, montado sobre el cuenco.
            // Mismo Init que los caños básicos del laboratorio (coste 0, ración
            // 45 -- regla de la inundación, pt26); IMovible como todos: V lo
            // recoloca, R lo devuelve aquí.
            var go = new GameObject("Grifo_Fundacion");
            var grifo = go.AddComponent<Dispenser>();
            grifo.Init(_sim, _aprendiz, SimLevelBuilder.FundacionCharcoX0 + 2, SimLevelBuilder.FundacionY0 + 12,
                MaterialId.Water, _orders, 0, false, 5, racionCeldas: 45);

            MaestroDice("¿Ves? La gotera no era una fuga: era un grifo sin domar. Ya es tuyo -- lo dejé sobre el cuenco. Los fenómenos no se eliminan, aprendiz: se DOMESTICAN.", 11f);
            _radioObjetivo = UiStyles.S(480f);
        }

        private void CompletarBarro()
        {
            _beat = Beat.Fogon; _tBeat = 0f; _pedidoEncolado = false;

            // El puñado de turba del morral -- y la veta asomada da el resto.
            _flask.Guardar(Turba, TurbaPrestada, 70);

            MaestroDice("Crema fina -- con esto se cuece tu frasco. Pero un taller no vive de fuego prestado: toma un puñado de turba de mi morral. El muro izquierdo esconde MÁS -- ¿ves el goteo pardo? Tállalo con el cincel (C). Carga el hogar vacío junto a la orilla.", 13f);
            _radioObjetivo = UiStyles.S(560f);
        }

        /// <summary>
        /// (ronda 61, beat 4) EL FUEGO PROPIO: el pedido se completa CARGANDO
        /// EL MUNDO, no el frasco -- el progreso espeja las celdas de turba
        /// dentro del hogar. Al llegar a CeldasFogon, el director lo enciende
        /// UNA vez (_fogonEncendido) y la física hace el resto: la turba arde
        /// (>=120 raw), deja ceniza, y esa ceniza es el fundente del beat 5.
        /// </summary>
        private void TickFogon()
        {
            if (!_pedidoEncolado)
            {
                _pedidoEncolado = true;
                _orders.EncolarPedidoGuiado(OrderType.Guiado, CeldasFogon, 0,
                    "Carga el hogar vacío (junto a la orilla de arena) con " + CeldasFogon + " de turba: viértela dentro. El fuego lo pongo yo... esta única vez.",
                    targetMat: Turba);
            }

            int enFogon = ContarEnFogon(Turba, out _, out _);
            if (_orders.ActiveOrders.Count > 0)
            {
                var o = _orders.ActiveOrders[0];
                int progreso = Mathf.Min(enFogon, CeldasFogon);
                if (o.Progreso != progreso && !o.Completado) o.Progreso = progreso;
            }

            if (enFogon >= CeldasFogon)
            {
                if (_orders.ActiveOrders.Count > 0) _orders.ActiveOrders[0].Completado = true;
                _fogonEncendido = true;
                // (Sin celda de Fire de ignición: nacía a ~240 y FUNDÍA la pila
                // entera por cascada -- ver el docblock de FogonRaw. El fuego
                // del hogar es el consumo por cadencia + el calor del lecho.)
                _beat = Beat.Vidrio; _tBeat = 0f; _pedidoEncolado = false;
                MaestroDice("¡Y ahí está! TU fuego. Míralo comerse la turba... y fíjate en lo que deja: la ceniza también cuenta. Nada se pierde del todo, aprendiz.", 10f);
                _radioObjetivo = UiStyles.S(650f);
            }
        }

        private void CompletarVidrio()
        {
            _beat = Beat.Estante; _tBeat = 0f;
            _orders.LimpiarPedidos();

            // EL PRIMER ESTANTE: lo levanta el Maestro entre su mesa y el muro
            // derecho -- 3 redomas, el StorageRack de siempre (aspirar/verter
            // en redoma ya funciona tal cual).
            var go = new GameObject("Estante_Fundacion");
            var estante = go.AddComponent<StorageRack>();
            estante.Init(_sim, _flask, _saber, _aprendiz,
                SimLevelBuilder.FundacionEstanteX0, SimLevelBuilder.FundacionEstanteX1,
                SimLevelBuilder.FundacionEstanteBaseY, visible: true, numRedomas: 3);

            MaestroDice("Vidrio de botella, del TUYO. Esto merece un sitio: te levanté un estante junto a mi mesa -- vierte en una redoma lo que quieras guardar.", 10f);
            _radioObjetivo = UiStyles.S(780f);
        }

        /// <summary>Beat 6: un respiro para ver el estante nacer, y el cierre -- que ahora ABRE la economía (ronda 62, F2): el tablón queda vivo y el greybox gana final abierto.</summary>
        private void TickEstante()
        {
            if (_tBeat < 9f) return;
            _beat = Beat.Fin; _tBeat = 0f;
            MaestroDice("Un fuego tuyo, barro tuyo, vidrio tuyo y dónde guardarlo. Esto ya es un taller, aprendiz. Y una cosa más: MI TABLÓN queda abierto -- junto a la mesa. Lo que produzcas, lo cambio. Lo que pidas... tarda, como todo lo que llega de lejos.", 16f);
            Trueque.Activar();
            _radioObjetivo = UiStyles.S(2400f); // amanece: fin del arco guiado; la economía sigue.
        }

        private void MaestroDice(string texto, float segundos)
        {
            _maestroTexto = texto;
            _maestroHasta = Time.time + segundos;
        }

        // ------------------------------------------------------------------
        // La viñeta + la voz.
        // ------------------------------------------------------------------
        private void OnGUI()
        {
            GUI.depth = 50; // detrás de todos los HUD (depth 0), encima del mundo.

            DibujarVineta();

            // La chapa "EL MAESTRO" sobre su mesa: en un greybox sin cuerpo ni
            // arte, los testers necesitan saber QUIÉN es el bulto junto al
            // fuego. Sobria (sin flecha, sin letrerote -- lección del pt54);
            // desaparece cuando el guion termina.
            if (_beat != Beat.Fin)
            {
                float celda = SimRenderer.CellWorldSize;
                var mesa = new Vector3((SimLevelBuilder.FundacionMesaX0 + SimLevelBuilder.FundacionMesaX1) * 0.5f * celda,
                    (SimLevelBuilder.FundacionMesaTopY + 1) * celda, 0f);
                UiStyles.PlacaMundo(mesa, "EL MAESTRO", new Color(0.92f, 0.86f, 0.7f, 0.85f), UiStyles.S(26f));
            }

            if (_maestroTexto != null && Time.time < _maestroHasta)
                SemillaCero.DibujarPanelMaestro(_maestroTexto);
        }

        private void DibujarVineta()
        {
            if (_radio > Screen.width + Screen.height) return; // ya amaneció del todo.
            if (_vineta == null) ConstruirVineta();

            var cam = Camera.main;
            if (cam == null) return;

            // Centro: entre el aprendiz y las brasas (sesgado al fuego), para
            // que el encuadre cero "mire" a la primera reacción.
            float celda = SimRenderer.CellWorldSize;
            var brasas = new Vector3((SimLevelBuilder.FundacionBrasasX0 + 2.5f) * celda,
                (SimLevelBuilder.FundacionBrasasY + 2) * celda, 0f);
            Vector3 foco = Vector3.Lerp(brasas, _aprendiz.position, _beat == Beat.Mirar ? 0.35f : 0.65f);
            Vector3 p = cam.WorldToScreenPoint(foco);
            float cx = p.x, cy = Screen.height - p.y;

            float r = _radio;
            var agujero = new Rect(cx - r, cy - r, r * 2f, r * 2f);
            GUI.DrawTexture(agujero, _vineta);

            // Los cuatro rectángulos macizos que completan la oscuridad
            // alrededor del óvalo.
            var negro = Texture2D.blackTexture;
            var prev = GUI.color; GUI.color = Color.black;
            if (agujero.yMin > 0) GUI.DrawTexture(new Rect(0, 0, Screen.width, agujero.yMin), negro);
            if (agujero.yMax < Screen.height) GUI.DrawTexture(new Rect(0, agujero.yMax, Screen.width, Screen.height - agujero.yMax), negro);
            if (agujero.xMin > 0) GUI.DrawTexture(new Rect(0, agujero.yMin, agujero.xMin, agujero.height), negro);
            if (agujero.xMax < Screen.width) GUI.DrawTexture(new Rect(agujero.xMax, agujero.yMin, Screen.width - agujero.xMax, agujero.height), negro);
            GUI.color = prev;
        }

        private void ConstruirVineta()
        {
            const int N = 256;
            _vineta = new Texture2D(N, N, TextureFormat.RGBA32, false);
            _vineta.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[N * N];
            float half = N * 0.5f;
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                    // Transparente en el centro, negro pleno del 62% hacia fuera
                    // (suavizado): el "óvalo de vela" del greybox.
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.98f, d));
                    px[y * N + x] = new Color32(0, 0, 0, (byte)(a * 255f));
                }
            }
            _vineta.SetPixels32(px);
            _vineta.Apply(false, true);
        }
    }
}
