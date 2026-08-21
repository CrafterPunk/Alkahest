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
        // El panel (tablón + Libro Mayor) -- RONDA 63b, ELEVADO A LA LÍNEA DEL
        // BAUTIZO por mandato de Cesar ("que lo tome Opus y lo eleve a la
        // calidad del menú bautizar... para que se mantenga en la línea
        // gráfica"): el mismo vocabulario EXACTO de Game/NamingUi.cs --
        // UiStyles.PanelRito (vitela ahumada + marco de latón con cantoneras),
        // TituloRito con Espaciar() (capital lapidaria), FileteRombo, muestra
        // del MATERIAL enmarcada en latón por fila (como la muestra del rito:
        // el jugador VE lo que pide), Ceremonial para la línea de tienda, y
        // los botones del skin vestido. Nada de hex propios: paleta
        // Oro/Laton/Pergamino de UiStyles, una sola fuente de verdad.
        // -----------------------------------------------------------------
        private static readonly Color Scrim = new Color(0.051f, 0.035f, 0.024f, 0.55f); // el velo del pase 62b (directiva 2).

        private string[] _lineasOferta;
        private GUIStyle _stTab, _stTabActiva, _stCuerpo, _stTenue, _stEtiqueta;

        private void ConstruirEstilos()
        {
            if (_stTab != null) return;
            _stTab = new GUIStyle(UiStyles.Boton) { fontSize = Mathf.RoundToInt(UiStyles.S(10f)) };
            _stTab.normal.textColor = UiStyles.TextoTenue;
            _stTabActiva = new GUIStyle(UiStyles.Boton) { fontSize = Mathf.RoundToInt(UiStyles.S(10f)) };
            _stTabActiva.normal.textColor = UiStyles.Oro;
            _stCuerpo = new GUIStyle(UiStyles.Cuerpo) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            _stTenue = new GUIStyle(UiStyles.CuerpoTenue) { alignment = TextAnchor.MiddleLeft, wordWrap = true };
            _stEtiqueta = new GUIStyle(UiStyles.CuerpoTenue) { alignment = TextAnchor.MiddleLeft };
            _stEtiqueta.normal.textColor = UiStyles.Laton;
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

            // El velo (directiva 2 del pase 62b): asienta el rito sobre la noche.
            Filete(new Rect(0, 0, Screen.width, Screen.height), Scrim);

            float w = UiStyles.S(440f);
            float pad = UiStyles.S(22f);          // el mismo aire interior del BAUTIZO.
            float filaAlto = UiStyles.S(26f);     // filas más altas: caben las muestras enmarcadas.
            float x0 = (Screen.width - w) * 0.5f;
            float y0 = UiStyles.S(86f);           // anclado arriba: el sujeto queda debajo, visible.

            // ---- medir el alto por contenido.
            float altoTitulo = UiStyles.TituloRito.lineHeight;
            float alto = UiStyles.S(18f) + altoTitulo + UiStyles.S(9f) + UiStyles.S(14f); // título + filete rombo.
            alto += UiStyles.S(24f) + UiStyles.S(10f); // pestañas.
            if (!_pestanaLibro)
            {
                alto += UiStyles.Alto(UiStyles.Ceremonial, LineaTienda, w - pad * 2f) + UiStyles.S(8f);
                alto += _catalogo.Length * (filaAlto + UiStyles.S(6f));
                alto += UiStyles.S(18f) + UiStyles.S(4f);
                alto += Mathf.Max(1, _numPendientes) * UiStyles.S(16f);
            }
            else
            {
                alto += 7 * UiStyles.S(17f) + UiStyles.S(20f);
            }
            alto += UiStyles.S(40f) + pad; // gesto de cierre.

            var r0 = new Rect(x0, y0, w, alto);
            UiStyles.PanelRito(r0); // vitela ahumada + marco de latón con cantoneras: EL panel del rito.

            float x = r0.x + pad, y = r0.y + UiStyles.S(18f);
            float interior = w - pad * 2f;

            // ---- TÍTULO: capital lapidaria espaciada con su filete, como el BAUTIZO.
            GUI.Label(new Rect(x, y, interior, altoTitulo), UiStyles.Espaciar("EL TABLÓN"), UiStyles.TituloRito);
            y += altoTitulo + UiStyles.S(9f);
            UiStyles.FileteRombo(r0.x + r0.width * 0.5f, y, interior * 0.80f, UiStyles.Laton);
            y += UiStyles.S(14f);

            // ---- Pestañas del skin vestido, la activa en ORO.
            float tabW = (interior - UiStyles.S(10f)) * 0.5f, tabH = UiStyles.S(24f);
            if (GUI.Button(new Rect(x, y, tabW, tabH), "TRUEQUE", _pestanaLibro ? _stTab : _stTabActiva)) _pestanaLibro = false;
            if (GUI.Button(new Rect(x + tabW + UiStyles.S(10f), y, tabW, tabH), "LIBRO MAYOR", _pestanaLibro ? _stTabActiva : _stTab)) _pestanaLibro = true;
            Filete(new Rect(_pestanaLibro ? x + tabW + UiStyles.S(10f) : x, y + tabH + 1f, tabW, 2f), UiStyles.Oro);
            y += tabH + UiStyles.S(10f);

            if (!_pestanaLibro)
            {
                // La línea de tienda, CEREMONIAL: el tono del rito, no un tooltip.
                float altoLinea = UiStyles.Alto(UiStyles.Ceremonial, LineaTienda, interior);
                GUI.Label(new Rect(x, y, interior, altoLinea), LineaTienda, UiStyles.Ceremonial);
                y += altoLinea + UiStyles.S(8f);

                // Filas con LA MUESTRA enmarcada en latón (la línea del bautizo:
                // el jugador VE la materia que pide, no solo su nombre).
                float lado = UiStyles.S(20f);
                float colTexto = interior - lado - UiStyles.S(10f) - UiStyles.S(70f) - UiStyles.S(14f);
                for (int i = 0; i < _catalogo.Length; i++)
                {
                    var rFila = new Rect(x, y, interior, filaAlto);
                    bool cerrada = _catalogo[i].PaginaCerrada;

                    var marco = new Rect(rFila.x, rFila.y + (filaAlto - lado) * 0.5f, lado, lado);
                    UiStyles.Rellenar(marco, new Color(0f, 0f, 0f, 0.55f));
                    var dentro = new Rect(marco.x + 2f, marco.y + 2f, marco.width - 4f, marco.height - 4f);
                    Color cMat = _sim != null && _sim.Universe != null ? (Color)_sim.Universe.Get(_catalogo[i].Mat).baseColor : Color.gray;
                    if (cerrada) cMat = Color.Lerp(cMat, Color.black, 0.55f);
                    UiStyles.Rellenar(dentro, cMat);
                    UiStyles.MarcoLaton(marco, UiStyles.Laton, cerrada ? 0.35f : 0.85f);

                    GUI.Label(new Rect(marco.xMax + UiStyles.S(10f), rFila.y, colTexto, filaAlto), _lineasOferta[i],
                        cerrada ? _stTenue : _stCuerpo);

                    if (!cerrada)
                    {
                        var rBtn = new Rect(rFila.xMax - UiStyles.S(70f), rFila.y + (filaAlto - UiStyles.S(22f)) * 0.5f,
                            UiStyles.S(70f), UiStyles.S(22f));
                        if (GUI.Button(rBtn, "Pedir", UiStyles.Boton)) Pedir(i);
                    }
                    y += filaAlto + UiStyles.S(6f);
                }

                // EN CAMINO: etiqueta de latón con su hilo.
                Filete(new Rect(x, y + UiStyles.S(2f), interior, 1f), UiStyles.LatonOscuro);
                GUI.Label(new Rect(x, y + UiStyles.S(5f), interior, UiStyles.S(14f)), "EN CAMINO", _stEtiqueta);
                y += UiStyles.S(18f) + UiStyles.S(4f);
                if (_numPendientes == 0)
                {
                    GUI.Label(new Rect(x, y, interior, UiStyles.S(16f)), "(nada — el buzón de salida espera tu primer pedido)", _stTenue);
                    y += UiStyles.S(16f);
                }
                for (int i = 0; i < _numPendientes; i++)
                {
                    var p = _pendientes[i];
                    int seg = Mathf.Max(0, Mathf.CeilToInt(p.ListoEn - Time.time));
                    GUI.Label(new Rect(x, y, interior, UiStyles.S(16f)), "· " + _catalogo[p.OfertaIdx].Nombre + " — " + seg + "s", _stCuerpo);
                    y += UiStyles.S(16f);
                }
            }
            else
            {
                float lh = UiStyles.S(17f);
                GUI.Label(new Rect(x, y, interior, lh), "LO QUE YA ES TUYO", _stEtiqueta); y += lh;
                GUI.Label(new Rect(x, y, interior, lh), "El grifo (la gotera domada) · tu fogón · el vidrio · el primer estante", _stCuerpo); y += lh + UiStyles.S(8f);
                GUI.Label(new Rect(x, y, interior, lh), "EL HORIZONTE", _stEtiqueta); y += lh;
                GUI.Label(new Rect(x, y, interior, lh), "· La página de la CALIZA — cambia 20 de vidrio en este tablón.", _stCuerpo); y += lh;
                GUI.Label(new Rect(x, y, interior, lh), "· El matraz grande — se abre con la caliza leída.", _stTenue); y += lh + UiStyles.S(8f);
                GUI.Label(new Rect(x, y, interior, lh), "...y 32 páginas más que aún no puedes leer.", _stTenue); y += lh;
            }

            // ---- EL GESTO DE CIERRE: botón del skin, mismo tamaño que los del rito.
            var rCerrar = new Rect(r0.x + (r0.width - UiStyles.S(150f)) * 0.5f, r0.yMax - pad - UiStyles.S(30f),
                UiStyles.S(150f), UiStyles.S(30f));
            if (GUI.Button(rCerrar, "Cerrar (E)", UiStyles.Boton)) _panelAbierto = false;
        }

        private const string LineaTienda = "Se paga del frasco. Lo que pides, tarda — y llega al buzón de salida.";
    }
}
