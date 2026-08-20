using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// (RONDA 62, GDD v0.3 §6-§7 -- F2: LA ECONOMÍA, greybox) EL TRUEQUE del
    /// Maestro de doble entrada: el TABLÓN de precios físico junto a su mesa,
    /// el BUZÓN DE SALIDA donde aterrizan tus pedidos, el stock por tiempo y
    /// los tiempos de entrega. Sin moneda: se paga con materia DEL FRASCO
    /// (pagar vacía lo que produjiste -- la tesis del almacenamiento físico).
    ///
    /// Las cinco reglas selladas del GDD §6, y su línea (regla 49):
    ///  · Tablón fijo por semilla ......... el catálogo es const para 777002.
    ///  · Stock limitado por ciclo ........ StockActual + CicloSegundos.
    ///  · Nunca vende descubrimientos ..... el catálogo solo lista materiales
    ///    que la fundación YA enseñó a usar (turba/arena/arcilla) + páginas
    ///    del Libro Mayor aún cerradas (visibles, no comprables).
    ///  · Un solo precio por objeto ....... pagas los requisitos y es tuyo
    ///    (la dualidad alquilar/poseer quedó aparcada, v0.3).
    ///  · Lo que pides TARDA .............. EntregaSegundos por entrada; los
    ///    encargos del mundo hacia ti siguen sin deadline JAMÁS.
    ///
    /// EL LIBRO MAYOR v0 (§7) vive como segunda pestaña del mismo panel:
    /// historial (lo desbloqueado por el arco), el horizonte (la siguiente
    /// página con su requisito legible) y la promesa contada ("...y N páginas
    /// más que aún no puedes leer"). Cero pantallas nuevas fuera del mundo:
    /// el panel solo se abre JUNTO al tablón (proximidad + E).
    ///
    /// Se ACTIVA al cerrar el arco de la fundación (FundacionDirector, beat
    /// Fin -> <see cref="Activar"/>): el greybox gana final abierto -- produce,
    /// cambia, pide, espera, recibe. El intendente de co-op nace aquí.
    /// </summary>
    public sealed class Trueque : MonoBehaviour
    {
        // -----------------------------------------------------------------
        // El catálogo de 777002 (fijo por semilla -- v1 constantes; cuando la
        // economía se generalice, saldrá de un sorteo con solver como todo).
        // -----------------------------------------------------------------
        private struct Oferta
        {
            public string Nombre;       // lo que ves en el tablón.
            public byte Mat;            // lo que llega al buzón de salida.
            public int Cantidad;        // cuántas celdas llegan.
            public byte PrecioMat;      // con qué se paga (del frasco).
            public int PrecioCantidad;
            public float EntregaSegundos;
            public int StockPorCiclo;   // cuántas veces por ciclo se puede pedir.
            public bool PaginaCerrada;  // visible en el Libro Mayor, aún no comprable.
        }

        private const float CicloSegundos = 180f;  // el stock se repone cada 3 min de juego.
        private const float DistTablon = 10f;      // celdas: estar "junto al tablón".
        private const int MaxPendientes = 4;

        private static readonly byte Turba = MaterialId.MatDe(Universe.SemillaCeroBaseTurbaIdx, EstadoMateria.Polvo);
        private static readonly byte Arena = MaterialId.MatDe(0, EstadoMateria.Polvo);
        private static readonly byte Arcilla = MaterialId.MatDe(1, EstadoMateria.Polvo);
        private static readonly byte Caliza = MaterialId.MatDe(2, EstadoMateria.Polvo);

        private readonly Oferta[] _catalogo =
        {
            new Oferta { Nombre = "TURBA (combustible)", Mat = Turba, Cantidad = 15, PrecioMat = MaterialId.VidrioVerde, PrecioCantidad = 6, EntregaSegundos = 75f, StockPorCiclo = 2 },
            new Oferta { Nombre = "ARENA de sílice", Mat = Arena, Cantidad = 15, PrecioMat = MaterialId.VidrioVerde, PrecioCantidad = 4, EntregaSegundos = 50f, StockPorCiclo = 2 },
            new Oferta { Nombre = "ARCILLA", Mat = Arcilla, Cantidad = 12, PrecioMat = MaterialId.VidrioVerde, PrecioCantidad = 4, EntregaSegundos = 50f, StockPorCiclo = 2 },
            new Oferta { Nombre = "CALIZA (página cerrada)", Mat = Caliza, Cantidad = 10, PrecioMat = MaterialId.VidrioVerde, PrecioCantidad = 8, EntregaSegundos = 90f, StockPorCiclo = 1, PaginaCerrada = true },
        };

        private struct Pendiente
        {
            public int OfertaIdx;
            public float ListoEn; // Time.time en que aterriza en el buzón de salida.
        }

        /// <summary>La economía existe solo tras el cierre del arco (beat Fin). Estática con reset en OnDestroy, patrón de todas las banderas de director.</summary>
        public static bool EconomiaActiva { get; private set; }

        private AlkahestSim _sim;
        private Flask _flask;
        private Transform _aprendiz;

        private readonly int[] _stock = new int[8];
        private readonly Pendiente[] _pendientes = new Pendiente[MaxPendientes];
        private int _numPendientes;
        private float _cicloTimer;
        private bool _panelAbierto;
        private bool _pestanaLibro; // false=TABLÓN, true=LIBRO MAYOR.
        private string _aviso; private float _avisoHasta;
        private float _ePrevio; // anti-rebote de E.

        public void Init(AlkahestSim sim, Flask flask, Transform aprendiz)
        {
            _sim = sim; _flask = flask; _aprendiz = aprendiz;
            for (int i = 0; i < _catalogo.Length; i++) _stock[i] = _catalogo[i].StockPorCiclo;
            _cicloTimer = CicloSegundos;
        }

        private void OnDestroy() => EconomiaActiva = false;

        public static void Activar() => EconomiaActiva = true;

        private void Update()
        {
            if (!EconomiaActiva) return;

            // Reponer stock por tiempo (regla sellada: por tiempo, ciclos cortos).
            _cicloTimer -= Time.deltaTime;
            if (_cicloTimer <= 0f)
            {
                _cicloTimer = CicloSegundos;
                for (int i = 0; i < _catalogo.Length; i++) _stock[i] = _catalogo[i].StockPorCiclo;
            }

            // Entregas que maduran: aterrizan como MATERIA REAL en el buzón de salida.
            for (int i = _numPendientes - 1; i >= 0; i--)
            {
                if (Time.time < _pendientes[i].ListoEn) continue;
                var o = _catalogo[_pendientes[i].OfertaIdx];
                EntregarEnBuzonSalida(o);
                _pendientes[i] = _pendientes[_numPendientes - 1];
                _numPendientes--;
                Avisar("Llegó tu pedido: " + o.Nombre + " · está en el buzón de salida.");
            }

            // E junto al tablón abre/cierra el panel (anti-rebote simple; el
            // tablón no pasa por MachineFocus: no es un aparato, es un mueble
            // del Maestro -- distancia corta para no pisarle la E al estante).
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame && !UiStyles.EscribiendoTexto && !JournalHud.Abierto
                && Time.time - _ePrevio > 0.2f && DistAlTablon() < DistTablon)
            {
                _ePrevio = Time.time;
                _panelAbierto = !_panelAbierto;
            }
            if (_panelAbierto && DistAlTablon() >= DistTablon) _panelAbierto = false; // alejarse lo cierra.
        }

        private float DistAlTablon()
        {
            float celda = SimRenderer.CellWorldSize;
            float tx = (SimLevelBuilder.FundacionSalidaX0 + SimLevelBuilder.FundacionSalidaX1) * 0.5f * celda;
            float ty = (SimLevelBuilder.FundacionY0 + 3) * celda;
            return Vector2.Distance(_aprendiz.position, new Vector2(tx, ty)) / celda;
        }

        /// <summary>La materia pedida aterriza FÍSICAMENTE en el nicho del buzón de salida (PaintStable, regla 29: el trueque CREA materia en tu mundo).</summary>
        private void EntregarEnBuzonSalida(Oferta o)
        {
            int x0 = SimLevelBuilder.FundacionSalidaX0;
            int ancho = SimLevelBuilder.FundacionSalidaX1 - x0 + 1;
            for (int i = 0; i < o.Cantidad; i++)
                _sim.PaintStable(x0 + (i % ancho), SimLevelBuilder.FundacionY0 + 1 + (i / ancho), 0, o.Mat);
        }

        private void Avisar(string msg) { _aviso = msg; _avisoHasta = Time.time + 6f; }

        private void Pedir(int idx)
        {
            var o = _catalogo[idx];
            if (o.PaginaCerrada) { Avisar("Esa página del Libro Mayor sigue cerrada."); return; }
            if (_stock[idx] <= 0) { Avisar("Sin stock hasta el próximo ciclo -- el Maestro también tiene límites."); return; }
            if (_numPendientes >= MaxPendientes) { Avisar("El morral del Maestro está lleno de encargos tuyos: espera una entrega."); return; }
            if (_flask.GetCount(o.PrecioMat) < o.PrecioCantidad) { Avisar("Te falta el pago en el frasco: " + o.PrecioCantidad + " de vidrio de botella."); return; }

            _flask.Extraer(o.PrecioMat, o.PrecioCantidad, out _); // pagar VACÍA lo producido: la tesis, en un gesto.
            _stock[idx]--;
            _pendientes[_numPendientes++] = new Pendiente { OfertaIdx = idx, ListoEn = Time.time + o.EntregaSegundos };
            Avisar("Pedido anotado. " + o.Nombre + " llega en " + Mathf.RoundToInt(o.EntregaSegundos) + "s al buzón de salida.");
        }

        // -----------------------------------------------------------------
        // El panel (tablón + Libro Mayor). IMGUI sobrio, cero allocs gordos:
        // strings cortos por interacción, no por frame (los labels de oferta
        // se construyen al abrir).
        // -----------------------------------------------------------------
        private string[] _lineasOferta;
        private void ConstruirLineas()
        {
            if (_lineasOferta == null) _lineasOferta = new string[_catalogo.Length];
            for (int i = 0; i < _catalogo.Length; i++)
            {
                var o = _catalogo[i];
                _lineasOferta[i] = o.Nombre + " ×" + o.Cantidad + "  ·  precio " + o.PrecioCantidad + " vidrio  ·  " +
                    Mathf.RoundToInt(o.EntregaSegundos) + "s  ·  stock " + _stock[i];
            }
        }

        private void OnGUI()
        {
            if (!EconomiaActiva || DayCycle.HudSilenciado) return;
            UiStyles.Preparar();
            GUI.depth = 10;

            // La chapa del mueble (sobria, pt54): solo de cerca y con el panel cerrado.
            if (!_panelAbierto && DistAlTablon() < DistTablon)
            {
                float celda = SimRenderer.CellWorldSize;
                var pos = new Vector3((SimLevelBuilder.FundacionSalidaX0 + 2) * celda, (SimLevelBuilder.FundacionY0 + 5) * celda, 0f);
                UiStyles.PlacaMundo(pos, "EL TABLÓN — E", new Color(0.92f, 0.86f, 0.7f, 0.85f), UiStyles.S(20f));
            }

            // El aviso del Maestro-tendero.
            if (_aviso != null && Time.time < _avisoHasta)
                SemillaCero.DibujarPanelMaestro(_aviso);

            if (!_panelAbierto) return;
            ConstruirLineas();

            float w = UiStyles.S(430f), h = UiStyles.S(300f);
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyles.Panel(r);
            GUILayout.BeginArea(new Rect(r.x + UiStyles.S(12f), r.y + UiStyles.S(10f), w - UiStyles.S(24f), h - UiStyles.S(20f)));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_pestanaLibro ? "· TABLÓN ·" : "[ TABLÓN ]", UiStyles.Boton)) _pestanaLibro = false;
            if (GUILayout.Button(_pestanaLibro ? "[ LIBRO MAYOR ]" : "· LIBRO MAYOR ·", UiStyles.Boton)) _pestanaLibro = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(UiStyles.S(6f));

            if (!_pestanaLibro)
            {
                GUILayout.Label("Se paga del frasco. Lo pedido llega al buzón de salida — lo que pides, tarda.", UiStyles.CuerpoTenue);
                GUILayout.Space(UiStyles.S(4f));
                for (int i = 0; i < _catalogo.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(_lineasOferta[i], _catalogo[i].PaginaCerrada ? UiStyles.CuerpoTenue : UiStyles.Cuerpo);
                    if (!_catalogo[i].PaginaCerrada && GUILayout.Button("pedir", UiStyles.Boton, GUILayout.Width(UiStyles.S(64f))))
                        Pedir(i);
                    GUILayout.EndHorizontal();
                }
                GUILayout.Space(UiStyles.S(6f));
                GUILayout.Label("EN CAMINO:", UiStyles.Titulo);
                if (_numPendientes == 0) GUILayout.Label("(nada — el buzón de salida espera tu primer pedido)", UiStyles.CuerpoTenue);
                for (int i = 0; i < _numPendientes; i++)
                {
                    var p = _pendientes[i];
                    int seg = Mathf.Max(0, Mathf.CeilToInt(p.ListoEn - Time.time));
                    GUILayout.Label("· " + _catalogo[p.OfertaIdx].Nombre + " — " + seg + "s", UiStyles.Cuerpo);
                }
            }
            else
            {
                GUILayout.Label("LO QUE YA ES TUYO:", UiStyles.Titulo);
                GUILayout.Label("· El grifo (la gotera domada)   · Tu fogón   · El vidrio de botella   · El primer estante", UiStyles.Cuerpo);
                GUILayout.Space(UiStyles.S(6f));
                GUILayout.Label("EL HORIZONTE:", UiStyles.Titulo);
                GUILayout.Label("· La página de la CALIZA — se abre cuando tu taller haya cambiado 20 de vidrio en este tablón.", UiStyles.Cuerpo);
                GUILayout.Label("· El matraz grande — se abre con la caliza leída.", UiStyles.CuerpoTenue);
                GUILayout.Space(UiStyles.S(6f));
                GUILayout.Label("...y 32 páginas más que aún no puedes leer.", UiStyles.CuerpoTenue);
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("cerrar (E)", UiStyles.Boton)) _panelAbierto = false;
            GUILayout.EndArea();
        }
    }
}
