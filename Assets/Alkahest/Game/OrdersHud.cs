using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Alkahest.Game
{
    /// <summary>
    /// HUD de encargos activos (arriba-derecha): Favor actual + escalón vigente
    /// en una línea, más una fila COMPACTA por encargo (progreso + recompensa).
    /// Solo se dibuja durante Playing.
    ///
    /// REESCRITO tras el playtest 16: "los encargos ahora se sienten como que
    /// estorban un espacio construible" (el taller pasa a 3x2 pantallas y va a
    /// ser editable por el jugador -- el espacio de pantalla es superficie de
    /// construcción, no solo estética) + "tiene un texto muy largo que podría
    /// recortarse" + "doble espaciado innecesario". Decisión de diseño (no
    /// ocultar del todo -- los encargos son el objetivo del jugador -- ni
    /// meterlos solo en el libro -- que tapa la pantalla entera y no sirve
    /// para consultar de reojo):
    ///
    ///  · MISMO CRITERIO QUE YA APLICA EL PROYECTO EN TODAS PARTES (el prompt
    ///    "E" que enseña MachineFocus.MostrarPromptE, el rótulo de nombre de
    ///    aparato que enseña HeatPlate/ChillStone._yaConocida): el HUD encoge
    ///    a medida que el jugador aprende. Por defecto el panel vive
    ///    COLAPSADO -- una línea por encargo con SOLO el progreso ("49/60"),
    ///    un cuadrito de color+glifo que identifica de qué va (ver
    ///    <see cref="TipoVisual"/>) y la recompensa. Nada de la frase larga.
    ///  · Se EXPANDE solo (la descripción completa + una barra por encargo)
    ///    un <see cref="PulsoSegundos"/> cuando hay algo que anunciar: al
    ///    aparecer un encargo nuevo (día recién empezado) o al cambiar de
    ///    ESTADO GRUESO (sin tocar -> en progreso -> completo, no en cada
    ///    celda individual entregada -- si no, cualquier ráfaga de vertido
    ///    lo dejaría abierto sin parar, que es justo el "estorbo" del que se
    ///    queja el playtest). Ver <see cref="DetectarCambiosYPulsar"/>.
    ///  · La tecla <b>O</b> ("Órdenes") lo expande/pliega a voluntad y el
    ///    estado se recuerda el resto de la partida (persiste en el propio
    ///    MonoBehaviour, que sobrevive los cambios de jornada -- solo un
    ///    RestartRun completo lo resetea). Comprobada contra la tabla de
    ///    atajos de docs/HANDOFF.md "Playtest 10": libre. Guarda mínima igual
    ///    que H (pistas) -- solo UiStyles.EscribiendoTexto, no hace falta
    ///    JournalHud.Abierto porque este panel no toca el mundo y el libro ya
    ///    lo tapa visualmente al dibujarse con GUI.depth más bajo (regla 12).
    ///
    /// PENDIENTE (fuera de alcance de esta ronda: JournalHud.cs y
    /// OrderSystem.cs no son archivos editables aquí): la descripción
    /// COMPLETA de cada encargo debería vivir también en el libro (sección de
    /// encargos), pero engancharla ahí exige tocar JournalHud (para pintar
    /// esa sección) y probablemente exponer algo nuevo desde OrderSystem --
    /// ninguno de los dos se toca en esta ronda. Mientras tanto, la
    /// descripción larga sigue viva y consultable en el modo EXPANDIDO de
    /// este mismo panel (tecla O), que es la única superficie disponible hoy.
    ///
    /// Regla 13 (CLAUDE.md): <c>order.Descripcion</c> se lee FRESCO cada
    /// OnGUI en el modo expandido (nunca se cachea) para no romper el
    /// recálculo por SubstanceKnowledge.NamingVersion -- solo se cachean los
    /// textos cortos que ESTE archivo construye (progreso/recompensa/Favor),
    /// y solo se reconstruyen cuando el valor de origen cambia de verdad (ver
    /// <see cref="FilaCache"/>/<see cref="ActualizarFavorTexto"/>): nada de
    /// concatenar strings en OnGUI cuando el texto no cambió desde el frame
    /// anterior.
    /// </summary>
    public sealed class OrdersHud : MonoBehaviour
    {
        private OrderSystem _orderSystem;

        // -----------------------------------------------------------------
        // Expandir/plegar (ver docblock de la clase).
        // -----------------------------------------------------------------
        private const float PulsoSegundos = 4f; // "un momento": alcanza para leer una línea sin quedarse fijo en pantalla.
        private bool _expandidoManual;
        private float _autoExpandUntil = float.NegativeInfinity;

        /// <summary>Último estado GRUESO (0 sin tocar / 1 en progreso / 2 completo) visto por Id de encargo -- el Id sobrevive a un re-bautizo (OrderSystem.RefreshDescripciones sustituye la instancia pero conserva Id), así que renombrar una sustancia NUNCA dispara un pulso falso aquí.</summary>
        private readonly Dictionary<int, byte> _estadoPorId = new Dictionary<int, byte>(8);

        private bool Expandido => _expandidoManual || Time.time < _autoExpandUntil;

        private const string TituloColapsado = "ENCARGOS  ·  O expande";
        private const string TituloExpandido = "ENCARGOS  ·  O pliega";

        // -----------------------------------------------------------------
        // Cache de texto corto por encargo (regla de cero strings en OnGUI).
        // -----------------------------------------------------------------
        private sealed class FilaCache
        {
            public int Progreso = -1;
            public bool Completado;
            public string TextoProgreso = "";
            public string TextoRecompensa = ""; // Recompensa es readonly en Order: se construye UNA vez y ya no cambia.
        }
        private readonly Dictionary<int, FilaCache> _filaCache = new Dictionary<int, FilaCache>(8);

        private int _favorCacheado = int.MinValue;
        private int _metaCacheada = -1;
        private string _favorTexto = "";

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(OrderSystem orderSystem)
        {
            _orderSystem = orderSystem;
        }

        private void Update()
        {
            if (_orderSystem == null || DayCycle.InputLocked) return;

            var kb = Keyboard.current;
            if (kb != null && kb.oKey.wasPressedThisFrame && !UiStyles.EscribiendoTexto)
            {
                _expandidoManual = !_expandidoManual;
            }

            DetectarCambiosYPulsar();
        }

        /// <summary>
        /// Un pulso de expansión automática cuando hay ALGO QUE ANUNCIAR: un
        /// Id de encargo nunca visto (día recién empezado) o que cruza a un
        /// estado grueso distinto (empieza a progresar / se completa). NO se
        /// dispara por cada celda entregada -- solo por el CAMBIO de estado --
        /// para que verter una ráfaga larga no deje el panel expandido todo
        /// el rato, que es justo el estorbo del que se quejó el playtest.
        /// </summary>
        private void DetectarCambiosYPulsar()
        {
            var orders = _orderSystem.ActiveOrders;
            for (int i = 0; i < orders.Count; i++)
            {
                var o = orders[i];
                byte estado = (byte)(o.Completado ? 2 : (o.Progreso > 0 ? 1 : 0));
                if (!_estadoPorId.TryGetValue(o.Id, out byte anterior) || anterior != estado)
                {
                    _estadoPorId[o.Id] = estado;
                    _autoExpandUntil = Time.time + PulsoSegundos;
                }
            }
        }

        /// <summary>Reconstruye <see cref="_favorTexto"/> SOLO si Favor o el escalón vigente cambiaron desde el frame anterior.</summary>
        private void ActualizarFavorTexto()
        {
            int favor = _orderSystem.Favor;
            bool hayEscalon = OrderSystem.TryGetNextTier(favor, out int meta, out string nombre);
            int metaEfectiva = hayEscalon ? meta : -1;
            if (favor == _favorCacheado && metaEfectiva == _metaCacheada) return;

            _favorCacheado = favor;
            _metaCacheada = metaEfectiva;
            _favorTexto = hayEscalon
                ? favor + " ★  →  " + nombre + " " + meta
                : favor + " ★  ·  máximo";
        }

        /// <summary>Fila de cache de texto para `o.Id`, creándola si es la primera vez y refrescando el progreso SOLO si cambió (nunca reconstruye si el frame no trajo nada nuevo).</summary>
        private FilaCache ObtenerFila(Order o)
        {
            if (!_filaCache.TryGetValue(o.Id, out var fila))
            {
                fila = new FilaCache { TextoRecompensa = "+" + o.Recompensa + " ★" };
                _filaCache[o.Id] = fila;
            }
            if (fila.Progreso != o.Progreso || fila.Completado != o.Completado)
            {
                fila.Progreso = o.Progreso;
                fila.Completado = o.Completado;
                fila.TextoProgreso = o.Completado ? "hecho" : (o.Progreso + "/" + o.MinCells);
            }
            return fila;
        }

        /// <summary>
        /// Color + glifo del cuadrito que identifica de qué va cada encargo
        /// (glifos ASCII simples, no exóticos -- misma cautela de fuente que
        /// ya documentaba este archivo con ✓/•/★). Completado SIEMPRE pisa el
        /// glifo de tipo por un ✓ verde: en cuanto está hecho, de qué iba deja
        /// de importar.
        /// </summary>
        private static void TipoVisual(OrderType tipo, bool completado, out Color color, out string glifo)
        {
            if (completado) { color = UiStyles.Exito; glifo = "✓"; return; }
            switch (tipo)
            {
                case OrderType.Flammable: color = UiStyles.Peligro; glifo = "F"; break;
                case OrderType.Hot: color = UiStyles.Aviso; glifo = "H"; break;
                case OrderType.Cold: color = UiStyles.Frio; glifo = "C"; break;
                case OrderType.Grows: color = UiStyles.Exito; glifo = "V"; break;
                case OrderType.CrystalSolid: color = UiStyles.Oro; glifo = "X"; break;
                default: color = UiStyles.Texto; glifo = "N"; break; // NamedMaterial.
            }
        }

        private void OnGUI()
        {
            // (playtest 21, EL PIVOT) HudSilenciado, hermano de InputLocked
            // -- misma línea, ver el docblock de DayCycle.HudSilenciado.
            if (_orderSystem == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return;

            UiStyles.Preparar();
            ActualizarFavorTexto();

            bool expandido = Expandido;
            var orders = _orderSystem.ActiveOrders;

            // ---- Medidas (fix playtest 16: apretadas contra el "doble
            // espaciado" del reporte, pero SIN volver al bug del playtest 3 --
            // aquí cada rect sigue midiéndose con CalcHeight/lineHeight real,
            // nunca con un alto fijo, así que apretar el aire entre secciones
            // no puede volver a recortar una frase). ----
            float margen = UiStyles.S(10f);
            float pad = UiStyles.S(7f);
            float ancho = UiStyles.S(300f);
            float interior = ancho - pad * 2f;
            float ladoCaja = UiStyles.S(14f);
            float anchoRecompensa = UiStyles.S(48f);
            float xTextoDesde = ladoCaja + UiStyles.S(6f);
            float anchoTextoProgreso = interior - xTextoDesde - anchoRecompensa;

            float altoLinea = UiStyles.S(17f);
            float altoBarraFavor = UiStyles.S(5f);
            float altoBarra = UiStyles.S(8f);
            float gapChico = UiStyles.S(3f);
            float gapFila = UiStyles.S(5f);

            // ---- 1) Medir ----
            float alto = pad
                       + altoLinea                    // "ENCARGOS · O expande/pliega"
                       + gapChico + altoLinea          // Favor + escalón, una sola línea
                       + gapChico + altoBarraFavor
                       + gapFila;
            for (int i = 0; i < orders.Count; i++)
            {
                alto += altoLinea; // fila compacta: caja + progreso + recompensa.
                if (expandido)
                {
                    alto += gapChico + altoBarra;
                    alto += gapChico + UiStyles.Alto(UiStyles.CuerpoTenue, orders[i].Descripcion, interior - xTextoDesde);
                }
                alto += gapFila;
            }
            alto += pad - (orders.Count > 0 ? gapFila : 0f); // el último encargo ya dejó el aire de abajo: no duplicarlo.

            var panel = new Rect(Screen.width - ancho - margen, margen, ancho, alto);
            UiStyles.Panel(panel);

            // ---- 2) Cabecera ----
            float x = panel.x + pad;
            float y = panel.y + pad;

            GUI.Label(new Rect(x, y, interior, altoLinea), expandido ? TituloExpandido : TituloColapsado, UiStyles.Titulo);
            y += altoLinea + gapChico;

            // Favor + escalón vigente en una sola línea (balance playtest 8:
            // la meta reescalada al escalón todavía no alcanzado -- ver
            // ActualizarFavorTexto/OrderSystem.TryGetNextTier).
            GUI.Label(new Rect(x, y, interior, altoLinea), _favorTexto, UiStyles.Numero);
            y += altoLinea + gapChico;

            float fracFavor = _metaCacheada > 0 ? (float)_orderSystem.Favor / _metaCacheada : 1f;
            UiStyles.Barra(new Rect(x, y, interior, altoBarraFavor), fracFavor, UiStyles.Oro);
            y += altoBarraFavor + gapFila;

            // ---- 3) Encargos: fila compacta siempre, detalle solo si expandido ----
            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                var fila = ObtenerFila(order);

                TipoVisual(order.Tipo, order.Completado, out Color colorCaja, out string glifo);
                var cajaRect = new Rect(x, y + (altoLinea - ladoCaja) * 0.5f, ladoCaja, ladoCaja);
                UiStyles.Rellenar(cajaRect, colorCaja);
                var previoColorChip = UiStyles.ChipMini.normal.textColor;
                UiStyles.ChipMini.normal.textColor = new Color(0f, 0f, 0f, 0.82f); // contraste oscuro sobre el cuadrito claro.
                GUI.Label(cajaRect, glifo, UiStyles.ChipMini);
                UiStyles.ChipMini.normal.textColor = previoColorChip;

                float xTexto = x + xTextoDesde;
                GUI.Label(new Rect(xTexto, y, anchoTextoProgreso, altoLinea), fila.TextoProgreso,
                    order.Completado ? UiStyles.CuerpoTenue : UiStyles.CuerpoLinea);
                GUI.Label(new Rect(xTexto + anchoTextoProgreso, y, anchoRecompensa, altoLinea), fila.TextoRecompensa, UiStyles.Numero);
                y += altoLinea;

                if (expandido)
                {
                    y += gapChico;
                    float frac = order.Completado ? 1f : Mathf.Clamp01((float)order.Progreso / Mathf.Max(1, order.MinCells));
                    UiStyles.Barra(new Rect(xTexto, y, anchoTextoProgreso + anchoRecompensa, altoBarra),
                        frac, order.Completado ? UiStyles.Exito : UiStyles.Oro);
                    y += altoBarra + gapChico;

                    // Descripción completa: leída FRESCA cada frame (regla 13, ver docblock de la clase).
                    float altoDesc = UiStyles.Alto(UiStyles.CuerpoTenue, order.Descripcion, interior - xTextoDesde);
                    GUI.Label(new Rect(xTexto, y, interior - xTextoDesde, altoDesc), order.Descripcion, UiStyles.CuerpoTenue);
                    y += altoDesc;
                }

                y += gapFila;
            }
        }
    }
}
