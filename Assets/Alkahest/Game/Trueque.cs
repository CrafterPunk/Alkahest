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
        // El panel (tablón + Libro Mayor) -- RONDA 62b, restilado entero por
        // las directivas 2 y 7-10 del pase de Opus-con-ojos (docs/HANDOFF.md):
        // scrim detrás, panel anclado arriba-centro (nunca sobre el sujeto),
        // "pergamino oscuro" #100c09 con doble filete dorado, título en
        // versales, pestañas con estado activo/inactivo, filas con botón
        // PEDIR alineado a su renglón, EN CAMINO degradado a etiqueta y
        // CERRAR (E) como chapa pequeña abajo-derecha. Layout por Rect
        // explícito (los botones de GUILayout derivaban del eje de su fila).
        // -----------------------------------------------------------------
        private static readonly Color PanelFondo = new Color(0.063f, 0.047f, 0.035f, 0.96f); // #100c09
        private static readonly Color Dorado = new Color(0.722f, 0.529f, 0.235f, 1f);        // #b8873c
        private static readonly Color DoradoTenue = new Color(0.722f, 0.529f, 0.235f, 0.6f);
        private static readonly Color Crema = new Color(0.937f, 0.886f, 0.776f, 1f);          // #efe2c6
        private static readonly Color CremaTitulo = new Color(0.910f, 0.835f, 0.659f, 1f);    // #e8d5a8
        private static readonly Color Scrim = new Color(0.051f, 0.035f, 0.024f, 0.55f);       // #0d0906 al 55% (directiva 2).

        private string[] _lineasOferta;
        private GUIStyle _stTitulo, _stTab, _stTabActiva, _stCuerpo, _stTenue, _stEtiqueta, _stBoton;

        private void ConstruirEstilos()
        {
            if (_stTitulo != null) return;
            _stTitulo = new GUIStyle(UiStyles.Titulo) { fontSize = Mathf.RoundToInt(UiStyles.S(13f)), alignment = TextAnchor.MiddleLeft };
            _stTitulo.normal.textColor = CremaTitulo;
            _stTab = new GUIStyle(UiStyles.Boton) { fontSize = Mathf.RoundToInt(UiStyles.S(9f)) };
            _stTab.normal.textColor = new Color(0.549f, 0.502f, 0.443f, 1f); // #8c8071 inactiva.
            _stTabActiva = new GUIStyle(UiStyles.Boton) { fontSize = Mathf.RoundToInt(UiStyles.S(9f)) };
            _stTabActiva.normal.textColor = new Color(0.949f, 0.894f, 0.769f, 1f); // #f2e4c4 activa.
            _stCuerpo = new GUIStyle(UiStyles.Cuerpo) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            _stCuerpo.normal.textColor = Crema;
            _stTenue = new GUIStyle(UiStyles.CuerpoTenue) { alignment = TextAnchor.MiddleLeft, wordWrap = true };
            _stEtiqueta = new GUIStyle(UiStyles.Titulo) { fontSize = Mathf.RoundToInt(UiStyles.S(9f)), alignment = TextAnchor.MiddleLeft };
            _stEtiqueta.normal.textColor = Dorado;
            _stBoton = new GUIStyle(UiStyles.Boton) { fontSize = Mathf.RoundToInt(UiStyles.S(10f)) };
            _stBoton.normal.textColor = CremaTitulo;
        }

        private void ConstruirLineas()
        {
            if (_lineasOferta == null) _lineasOferta = new string[_catalogo.Length];
            for (int i = 0; i < _catalogo.Length; i++)
            {
                var o = _catalogo[i];
                _lineasOferta[i] = o.Nombre + " ×" + o.Cantidad + " · " + o.PrecioCantidad + " vidrio · " +
                    Mathf.RoundToInt(o.EntregaSegundos) + "s · stock " + _stock[i];
            }
        }

        private static void Filete(Rect r, Color c)
        {
            var prev = GUI.color; GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private void OnGUI()
        {
            if (!EconomiaActiva || DayCycle.HudSilenciado) return;
            UiStyles.Preparar();
            ConstruirEstilos();
            GUI.depth = 10;

            // La chapa del mueble: sobre el nicho, atenuada con la luz local
            // (directiva 5, vía FundacionDirector.LuzEn).
            if (!_panelAbierto && DistAlTablon() < DistTablon)
            {
                float celda = SimRenderer.CellWorldSize;
                var pos = new Vector3((SimLevelBuilder.FundacionSalidaX0 + 2) * celda, (SimLevelBuilder.FundacionY0 + 6) * celda, 0f);
                float alfa = FundacionDirector.LuzEn(pos) * 0.85f;
                UiStyles.PlacaMundo(pos, "EL TABLÓN — E", new Color(0.92f, 0.86f, 0.7f, alfa), UiStyles.S(10f));
            }

            // El aviso del tendero: por la banda del Maestro (directiva 4).
            if (_aviso != null && Time.time < _avisoHasta)
                FundacionDirector.DibujarBandaMaestro(_aviso);

            if (!_panelAbierto) return;
            ConstruirLineas();

            // (directiva 2) EL SCRIM: un velo pardo tras el panel asienta la
            // lectura y nadie queda partido por un borde.
            Filete(new Rect(0, 0, Screen.width, Screen.height), Scrim);

            float w = UiStyles.S(420f);
            float pad = UiStyles.S(14f);
            float filaAlto = UiStyles.S(22f);
            float x0 = (Screen.width - w) * 0.5f;
            float y0 = UiStyles.S(96f); // anclado arriba: el sujeto queda debajo, visible.

            // ---- medir el alto por contenido (directiva 9: sin hueco muerto).
            float alto = pad + UiStyles.S(24f) + UiStyles.S(10f); // cabecera + filete.
            if (!_pestanaLibro)
            {
                alto += UiStyles.S(16f) + UiStyles.S(6f);                       // subtítulo.
                alto += _catalogo.Length * (filaAlto + UiStyles.S(4f));         // ofertas.
                alto += UiStyles.S(16f) + UiStyles.S(4f);                       // etiqueta EN CAMINO.
                alto += Mathf.Max(1, _numPendientes) * UiStyles.S(15f);         // pendientes (o el "(nada...)").
            }
            else
            {
                alto += 7 * UiStyles.S(16f) + UiStyles.S(18f);
            }
            alto += UiStyles.S(30f) + pad; // chapa cerrar.

            var r0 = new Rect(x0, y0, w, alto);
            // (directiva 9) Pergamino oscuro con DOBLE filete fino.
            Filete(new Rect(r0.x - 2f, r0.y - 2f, r0.width + 4f, r0.height + 4f), DoradoTenue);
            Filete(new Rect(r0.x - 1f, r0.y - 1f, r0.width + 2f, r0.height + 2f), Color.black);
            Filete(r0, PanelFondo);

            float x = r0.x + pad, y = r0.y + pad;
            float interior = w - pad * 2f;

            // ---- cabecera: título + pestañas con estado (directiva 7).
            GUI.Label(new Rect(x, y, interior * 0.45f, UiStyles.S(24f)), "EL TABLÓN", _stTitulo);
            float tabW = UiStyles.S(96f), tabH = UiStyles.S(20f);
            var rTab1 = new Rect(x + interior - tabW * 2f - UiStyles.S(6f), y + UiStyles.S(2f), tabW, tabH);
            var rTab2 = new Rect(x + interior - tabW, y + UiStyles.S(2f), tabW, tabH);
            Filete(rTab1, _pestanaLibro ? Color.clear : new Color(0.141f, 0.102f, 0.063f, 1f));
            Filete(rTab2, _pestanaLibro ? new Color(0.141f, 0.102f, 0.063f, 1f) : Color.clear);
            if (GUI.Button(rTab1, "TRUEQUE", _pestanaLibro ? _stTab : _stTabActiva)) _pestanaLibro = false;
            if (GUI.Button(rTab2, "LIBRO MAYOR", _pestanaLibro ? _stTabActiva : _stTab)) _pestanaLibro = true;
            y += UiStyles.S(24f) + UiStyles.S(2f);
            Filete(new Rect(x, y, interior, 1f), DoradoTenue);
            y += UiStyles.S(8f);

            if (!_pestanaLibro)
            {
                GUI.Label(new Rect(x, y, interior, UiStyles.S(16f)), "Se paga del frasco. Lo que pides, tarda — llega al buzón de salida.", _stTenue);
                y += UiStyles.S(16f) + UiStyles.S(6f);

                // (directiva 8) Filas por Rect: el botón vive centrado en SU renglón.
                float colTexto = interior - UiStyles.S(64f) - UiStyles.S(20f);
                for (int i = 0; i < _catalogo.Length; i++)
                {
                    var rFila = new Rect(x, y, interior, filaAlto);
                    GUI.Label(new Rect(rFila.x, rFila.y, colTexto, filaAlto), _lineasOferta[i],
                        _catalogo[i].PaginaCerrada ? _stTenue : _stCuerpo);
                    if (!_catalogo[i].PaginaCerrada)
                    {
                        var rBtn = new Rect(rFila.xMax - UiStyles.S(64f), rFila.y + (filaAlto - UiStyles.S(20f)) * 0.5f,
                            UiStyles.S(64f), UiStyles.S(20f));
                        if (GUI.Button(rBtn, "PEDIR", _stBoton)) Pedir(i);
                    }
                    y += filaAlto + UiStyles.S(4f);
                }

                // (directiva 10) EN CAMINO como etiqueta de sección, no letrerote.
                Filete(new Rect(x, y + UiStyles.S(2f), interior, 1f), new Color(0.227f, 0.184f, 0.133f, 1f));
                GUI.Label(new Rect(x, y + UiStyles.S(5f), interior, UiStyles.S(14f)), "EN CAMINO", _stEtiqueta);
                y += UiStyles.S(16f) + UiStyles.S(4f);
                if (_numPendientes == 0)
                {
                    GUI.Label(new Rect(x, y, interior, UiStyles.S(15f)), "(nada — el buzón de salida espera tu primer pedido)", _stTenue);
                    y += UiStyles.S(15f);
                }
                for (int i = 0; i < _numPendientes; i++)
                {
                    var p = _pendientes[i];
                    int seg = Mathf.Max(0, Mathf.CeilToInt(p.ListoEn - Time.time));
                    GUI.Label(new Rect(x, y, interior, UiStyles.S(15f)), "· " + _catalogo[p.OfertaIdx].Nombre + " — " + seg + "s", _stCuerpo);
                    y += UiStyles.S(15f);
                }
            }
            else
            {
                GUI.Label(new Rect(x, y, interior, UiStyles.S(16f)), "LO QUE YA ES TUYO", _stEtiqueta); y += UiStyles.S(16f);
                GUI.Label(new Rect(x, y, interior, UiStyles.S(16f)), "El grifo (la gotera domada) · tu fogón · el vidrio · el primer estante", _stCuerpo); y += UiStyles.S(16f) + UiStyles.S(6f);
                GUI.Label(new Rect(x, y, interior, UiStyles.S(16f)), "EL HORIZONTE", _stEtiqueta); y += UiStyles.S(16f);
                GUI.Label(new Rect(x, y, interior, UiStyles.S(16f)), "· La página de la CALIZA — cambia 20 de vidrio en este tablón.", _stCuerpo); y += UiStyles.S(16f);
                GUI.Label(new Rect(x, y, interior, UiStyles.S(16f)), "· El matraz grande — se abre con la caliza leída.", _stTenue); y += UiStyles.S(16f) + UiStyles.S(6f);
                GUI.Label(new Rect(x, y, interior, UiStyles.S(16f)), "...y 32 páginas más que aún no puedes leer.", _stTenue); y += UiStyles.S(16f);
            }

            // (directiva 10) CERRAR (E): chapa pequeña, abajo-derecha.
            var rCerrar = new Rect(r0.xMax - pad - UiStyles.S(96f), r0.yMax - pad - UiStyles.S(22f), UiStyles.S(96f), UiStyles.S(22f));
            if (GUI.Button(rCerrar, "CERRAR (E)", _stBoton)) _panelAbierto = false;
        }
    }
}
