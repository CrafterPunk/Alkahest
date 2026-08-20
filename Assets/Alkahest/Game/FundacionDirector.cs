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
        private enum Beat { Mirar, Saludo, Gotera, Barro, Fin }

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

        private AlkahestSim _sim;
        private OrderSystem _orders;
        private Flask _flask;
        private Transform _aprendiz;

        private Beat _beat = Beat.Mirar;
        private float _tBeat;          // segundos dentro del beat actual.
        private float _dripTimer, _fuegoTimer;
        private bool _goteraActiva = true;
        private bool _pistaMirarDicha;
        private bool _pedidoEncolado;  // el pedido del beat actual ya está en OrderSystem.
        private static readonly byte Barbotina = MaterialId.MatDe(1, EstadoMateria.Solucion);

        // La voz del Maestro (reutiliza el panel estático de SemillaCero, pt55).
        private string _maestroTexto;
        private float _maestroHasta;

        // La viñeta.
        private Texture2D _vineta;
        private float _radio;          // radio actual en px (lerp suave hacia _radioObjetivo).
        private float _radioObjetivo;

        public void Init(AlkahestSim sim, OrderSystem orders, Flask flask, Transform aprendiz)
        {
            _sim = sim;
            _orders = orders;
            _flask = flask;
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
                case Beat.Fin: break;
            }

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
                string texto = _beat == Beat.Gotera
                    ? "Atrapa la gotera: aspira el agua al vuelo y tráeme " + cantidad + " sorbos."
                    : "El polvo de la orilla se bebe el agua y se hace crema -- barbotina, la llaman los alfareros. Vierte polvo en el cuenco y tráeme " + cantidad + ".";
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
            _beat = Beat.Fin; _tBeat = 0f;
            _orders.LimpiarPedidos();
            MaestroDice("Crema fina. Con esto se cuece un frasco de verdad -- el tuyo. Hasta aquí el ensayo de hoy: dime qué sentiste al fundar tu taller.", 14f);
            _radioObjetivo = UiStyles.S(2400f); // la oscuridad se retira: fin del greybox.
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
