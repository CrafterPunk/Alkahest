using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Net;

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
    ///
    /// (CONTRATO_RONDA50.md §3c, ENCARGO G, playtest 50) EL CAMINO SEÑALADO,
    /// SOLO SEMILLA CERO (contrato §3e: "el caótico NO cambia"): cada fila de
    /// encargo gana un segundo cuadrito -- una FLECHA (←/→/↓, ver
    /// <see cref="TryFlechaTolva"/>) hacia la Tolva, sin minimapa ni vector,
    /// solo para los tipos que de verdad se resuelven ahí (ver
    /// <see cref="VaALaTolva"/>). En caótico <c>anchoFlechaTolva</c> vale 0 y
    /// el layout queda idéntico al de siempre.
    ///
    /// (playtest 52, CO-OP GUIADO, mandato de Cesar) LA VOZ DEL MAESTRO, TAMBIÉN AQUÍ: la
    /// rama replicada (<see cref="OnGuiReplicado"/>) pinta el panel "EL MAESTRO" de
    /// <see cref="SemillaCero"/> con el texto que <c>Net/SaberSync.cs</c> le replica -- ver
    /// <see cref="ActualizarMaestroReplicado"/>. La flecha a la Tolva, en cambio, se documenta
    /// como OMITIDA en el invitado (ver el docblock de <see cref="TryFlechaTolva"/>): la Tolva
    /// no existe como GameObject en su proceso, así que no hay contra qué apuntar.
    /// </summary>
    public sealed class OrdersHud : MonoBehaviour
    {
        private OrderSystem _orderSystem;

        // -----------------------------------------------------------------
        // (CONTRATO_RONDA50.md §3c, ENCARGO G, playtest 50) EL CAMINO
        // SEÑALADO -- "OrdersHud muestra una FLECHA de dirección hacia la
        // Tolva... sin minimapa" (contrato, textual). Ver TryFlechaTolva.
        // -----------------------------------------------------------------
        private Transform _player;
        private DeliveryChute _tolva;

        // -----------------------------------------------------------------
        // (playtest 52, CO-OP GUIADO) LA VOZ DEL MAESTRO EN EL INVITADO -- ver
        // ActualizarMaestroReplicado/OnGuiReplicado. `_maestroTextoVisto` guarda el ÚLTIMO
        // valor de Net/SaberSync.cs::MaestroTexto ya procesado (comparación de struct, sin
        // alloc); solo al CAMBIAR se llama a `ToString()` y se calcula un `Time.time` LOCAL de
        // expiración -- de ahí en adelante el invitado cuenta su propio reloj sin volver a
        // preguntarle a la red cuánto queda (mismo criterio de "cero strings por frame salvo
        // cuando cambian de verdad" que el resto de este archivo).
        // -----------------------------------------------------------------
        private Unity.Collections.FixedString128Bytes _maestroTextoVisto;
        private string _maestroTextoLocal;
        private float _maestroHastaLocal;

        // -----------------------------------------------------------------
        // Expandir/plegar (ver docblock de la clase).
        // -----------------------------------------------------------------
        private const float PulsoSegundos = 4f; // "un momento": alcanza para leer una línea sin quedarse fijo en pantalla.
        private bool _expandidoManual;
        private float _autoExpandUntil = float.NegativeInfinity;

        /// <summary>Último estado GRUESO (0 sin tocar / 1 en progreso / 2 completo) visto por Id de encargo -- el Id sobrevive a un re-bautizo (OrderSystem.RefreshDescripciones sustituye la instancia pero conserva Id), así que renombrar una sustancia NUNCA dispara un pulso falso aquí.</summary>
        private readonly Dictionary<int, byte> _estadoPorId = new Dictionary<int, byte>(8);

        private bool Expandido => _expandidoManual || Time.time < _autoExpandUntil;

        /// <summary>(playtest 23) Lo que enseña el panel cuando aún no existe ningún encargo -- en el pivot no los hay hasta cavar hasta la Tolva. Cero strings por frame: es const.</summary>
        private const string TextoSinEncargos = "(nadie os ha oído todavía -- la Tolva del Maestro sigue sellada tras la roca, hacia la derecha)";

        /// <summary>
        /// (Encargo G, SEMILLA CERO, contrato §1 beat 6) EL PANEL VACÍO ES EL FINAL
        /// ABIERTO: la frase de arriba habla de una Tolva sellada tras roca que no existe
        /// en este modo, y contradiría el silencio deliberado del beat 6 ("sin encargo
        /// nuevo, panel de encargos vacío" -- no "todavía no me habéis encontrado"). Este
        /// modo también pasa por aquí en el breve hueco entre beats (p. ej. mientras el
        /// Maestro exige el nombre, antes de que SemillaCero encole el pedido con nombre).
        /// </summary>
        private const string TextoSinEncargosSemillaCero = "(nada por ahora)";

        private static string TextoVacio => AlkahestGameBootstrap.ModoSemillaCero ? TextoSinEncargosSemillaCero : TextoSinEncargos;

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

        // (L1, fuera el Favor) Aquí vivían _favorCacheado/_metaCacheada/_favorTexto,
        // el caché de la línea de Favor de la cabecera -- retirados con ella.

        /// <summary>
        /// Inyección de dependencias desde AlkahestGameBootstrap. `orderSystem`
        /// puede ser null (playtest 36, EL CAMINO DEL INVITADO): un invitado
        /// no tiene OrderSystem local (vive solo en el anfitrión, ver el
        /// docblock de <see cref="AlkahestGameBootstrap.TrySpawnRed"/>) --
        /// ver <see cref="ModoReplicado"/> para la rama que lo sustituye.
        /// </summary>
        public void Init(OrderSystem orderSystem)
        {
            _orderSystem = orderSystem;
        }

        /// <summary>
        /// (playtest 36, EL CAMINO DEL INVITADO) ¿Estamos pintando encargos
        /// LEÍDOS DE LA RED en vez de un <see cref="OrderSystem"/> propio?
        /// Solo tiene sentido sin OrderSystem local Y con una sesión de red
        /// viva (la escena clásica de un jugador nunca tiene SaberSync, así
        /// que esto es false ahí sin ni comprobar nada más).
        /// </summary>
        private bool ModoReplicado => _orderSystem == null && SaberSync.Instancia != null && SaberSync.Instancia.IsSpawned;

        private void Update()
        {
            if (DayCycle.InputLocked) return;
            if (_orderSystem == null && !ModoReplicado) return; // ni local ni red: nada que hacer todavía (se reintenta el próximo frame).

            var kb = Keyboard.current;
            if (kb != null && kb.oKey.wasPressedThisFrame && !UiStyles.EscribiendoTexto)
            {
                _expandidoManual = !_expandidoManual;
            }

            // El pulso automático de expansión (DetectarCambiosYPulsar) lee
            // OrderSystem.ActiveOrders directamente -- no tiene sentido en
            // modo replicado (read-only, ver el docblock de la clase de más
            // abajo): el invitado puede expandir/plegar a mano con O, pero
            // no hay "algo que anunciar" que este HUD pueda detectar sin
            // duplicar el registro completo de SaberSync solo para eso.
            if (_orderSystem != null) DetectarCambiosYPulsar();
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

        // (LIMPIEZA L1, ronda 60, GDD v0.3 §6 -- FUERA EL FAVOR) Aquí vivía
        // ActualizarFavorTexto(): la línea "N ★ → escalón" + su barra dorada, cabecera
        // del panel desde el playtest 8. El Favor salió del juego por decisión de
        // Cesar ("no nos sirve de momento ni como moneda ni como nada, fue un ensayo
        // antiguo"): la economía nueva es el TRUEQUE físico (GDD §6) y las recompensas
        // pasan a ser materia/obras/páginas del Libro Mayor. La MECÁNICA interna
        // (OrderSystem.Favor/AddFavor/TryGetNextTier) sigue viva SOLO como soporte del
        // modo caótico legado (bloqueado, pendiente de rediseño post-Fest) -- este HUD
        // simplemente ya no la enseña en ningún modo. Regla 15: si alguien quiere
        // "ver el Favor de vuelta", que lea esto primero.

        /// <summary>Fila de cache de texto para `o.Id`, creándola si es la primera vez y refrescando el progreso SOLO si cambió (nunca reconstruye si el frame no trajo nada nuevo).</summary>
        private FilaCache ObtenerFila(Order o)
        {
            if (!_filaCache.TryGetValue(o.Id, out var fila))
            {
                fila = new FilaCache { TextoRecompensa = "" }; // (L1, fuera el Favor) antes: "+" + o.Recompensa + " ★"
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
                // (Encargo G, SEMILLA CERO) el pedido guiado es una PREGUNTA del Maestro,
                // no una entrega genérica -- glifo propio en vez de caer al "N" de
                // NamedMaterial (regla 48 de CLAUDE.md: un estado nuevo necesita un verbo
                // visible propio).
                case OrderType.Guiado: color = UiStyles.Oro; glifo = "?"; break;
                default: color = UiStyles.Texto; glifo = "N"; break; // NamedMaterial.
            }
        }

        /// <summary>
        /// ¿Este tipo de encargo se resuelve DE VERDAD en la Tolva? Conduce y
        /// AguantaCalor NUNCA pasan por ahí (ver sus docblocks en
        /// Game/Order.cs -- se resuelven en el Banco de Chispa/el Ensayo vía
        /// <see cref="OrderSystem.CompletarEnsayo"/>): apuntar la flecha a la
        /// Tolva para esos dos confundiría en vez de enseñar. Todo lo demás
        /// (Guiado, FlotaInsoluble, y los tipos clásicos) sí se entrega ahí.
        /// </summary>
        private static bool VaALaTolva(OrderType tipo) => tipo != OrderType.Conduce && tipo != OrderType.AguantaCalor;

        /// <summary>
        /// (CONTRATO_RONDA50.md §3c) Glifo simple (←/→/↓), NUNCA una brújula
        /// ni un vector -- compara la posición MUNDO del aprendiz contra la
        /// de la Tolva (el transform de Game/DeliveryChute.cs, que ya se
        /// ancla al "centro del labio de la boca" en su propio Init: ver ese
        /// archivo) y elige el eje dominante. Referencias resueltas UNA vez
        /// con <c>FindAnyObjectByType</c> (mismo patrón defensivo que ya usa
        /// esta misma clase para <see cref="SaberSync"/> y que usa
        /// Game/DeliveryChute.cs para su propio <c>_player</c>) y cacheadas:
        /// esta clase no recibe ninguna de las dos por inyección.
        /// </summary>
        /// <summary>
        /// (playtest 52, CO-OP GUIADO, contrato §3: "la flecha del pt50 NO revienta en la rama
        /// replicada... si no hay DeliveryChute local resuelto, protégela con null-check y
        /// documenta si la omites") VERIFICADO: este método YA es a prueba de invitado -- el
        /// `if (_player == null || _tolva == null) return false;` de abajo cubre exactamente
        /// ese caso, así que llamarlo desde <see cref="OnGuiReplicado"/> nunca reventaría.
        ///
        /// DECISIÓN, DOCUMENTADA: aun así, <see cref="OnGuiReplicado"/> NO lo llama. La razón:
        /// <c>Game/DeliveryChute.cs</c> (la Tolva) solo se instancia en la rama `anfitrion` de
        /// <c>AlkahestGameBootstrap.TrySpawnRed</c> -- NUNCA existe como GameObject en el
        /// proceso del invitado (no es un `NetworkBehaviour`, y `Net/MaquinaSync.cs` -- el
        /// registro que sí replica posición de máquinas -- no tiene ningún `TipoMaquina` para
        /// ella), así que `_tolva` se quedaría en `null` PARA SIEMPRE en el invitado. Llamar a
        /// este método desde el invitado no reventaría, pero SÍ repetiría
        /// `FindAnyObjectByType&lt;DeliveryChute&gt;()` una vez por encargo expandido cada
        /// frame, buscando fruitlessly algo que jamás va a aparecer -- gasto que la disciplina
        /// de "polling barato" del proyecto no admite pagar sin ninguna esperanza de éxito.
        /// DEUDA para Fable: registrar la Tolva en Net/MaquinaSync.cs (archivo ajeno a este
        /// encargo) le daría al invitado una posición real contra la que apuntar la flecha --
        /// mientras tanto, el invitado en Semilla Cero ve los pedidos y su progreso, pero sin
        /// el glifo de dirección hacia la Tolva.
        /// </summary>
        private bool TryFlechaTolva(out string glifo)
        {
            glifo = null;
            if (_player == null) _player = FindAnyObjectByType<ApprenticeController>()?.transform;
            if (_tolva == null) _tolva = FindAnyObjectByType<DeliveryChute>();
            if (_player == null || _tolva == null) return false;

            Vector3 d = _tolva.transform.position - _player.position;
            float ax = Mathf.Abs(d.x), ay = Mathf.Abs(d.y);
            // "abajo" solo cuando el desnivel manda Y la Tolva está POR
            // DEBAJO del aprendiz -- nunca "arriba" (contrato: solo
            // izquierda/derecha/abajo).
            if (ay > ax && d.y < 0f) { glifo = "↓"; return true; }
            glifo = d.x >= 0f ? "→" : "←";
            return true;
        }

        // =================================================================
        // (ronda 56, LA VIDA ÚTIL DE LO DESCUBIERTO, CONTRATO_RONDA56.md §1c)
        // EL ENCARGO COMPUESTO: se pinta como UN bloque de 3 líneas (cabecera con
        // nombre corto + recompensa TOTAL, narrativa si expandido, y el checklist
        // "▫ etiqueta  n/total" por componente, atenuado con ✓ al completarse) en vez
        // de tres filas sueltas indistinguibles del resto del panel. Formato a
        // decisión de este encargo (contrato: "respetando el ancho real del panel,
        // medido") -- reutiliza <see cref="ObtenerFila"/> para el texto de progreso de
        // cada componente (mismo cache de "cero strings por frame salvo cuando
        // cambian de verdad" que ya usa el resto del archivo) y
        // <see cref="TryFlechaTolva"/>/<see cref="VaALaTolva"/> tal cual, sin
        // duplicar esa lógica: "la flecha al Buzón del pt50 aplica al compuesto
        // igual que a los Guiado" (contrato §1c, textual).
        // =================================================================

        /// <summary>Alto en píxeles del bloque completo de un encargo compuesto (cabecera + narrativa opcional + 3 líneas de checklist). Ver el docblock de la región de arriba.</summary>
        private float AltoBloqueCompuesto(Order cabecera, bool expandido, float interior, float altoLinea, float gapChico)
        {
            float alto = altoLinea; // cabecera: nombre corto + recompensa total.
            if (expandido)
                alto += gapChico + UiStyles.Alto(UiStyles.CuerpoTenue, cabecera.GrupoTextoLargo, interior);
            alto += gapChico + altoLinea * 3f; // 3 líneas de checklist, una por componente.
            return alto;
        }

        /// <summary>
        /// Dibuja el bloque de un encargo compuesto (ver el docblock de la región de
        /// arriba). `orders[i]`/`orders[i+1]`/`orders[i+2]` son las 3 hermanas del
        /// mismo grupo (garantía de <see cref="OrderSystem.EncolarCompuesto"/>): todas
        /// comparten GrupoNombreCorto/GrupoTextoLargo/GrupoRecompensaTotal, así que
        /// cualquiera sirve de cabecera -- se usa `orders[i]`.
        /// </summary>
        private void DibujarBloqueCompuesto(List<Order> orders, int i, ref float y, float x, float interior,
            float altoLinea, float gapChico, float anchoRecompensa, bool expandido, bool modoSemillaCero, float ladoCaja)
        {
            var cabecera = orders[i];

            // (L1, fuera el Favor) La cabecera del compuesto mostraba "+N ★" a la
            // derecha (GrupoRecompensaTotal) -- retirado; el nombre usa el ancho entero.
            GUI.Label(new Rect(x, y, interior, altoLinea), cabecera.GrupoNombreCorto, UiStyles.Titulo);
            y += altoLinea;

            if (expandido)
            {
                y += gapChico;
                // Narrativa completa: leída FRESCA cada frame (regla 13) -- aunque en esta
                // ronda el texto es constante, mismo criterio que el resto del panel para
                // Order.Descripcion (nunca se cachea el texto largo de origen).
                float altoTexto = UiStyles.Alto(UiStyles.CuerpoTenue, cabecera.GrupoTextoLargo, interior);
                GUI.Label(new Rect(x, y, interior, altoTexto), cabecera.GrupoTextoLargo, UiStyles.CuerpoTenue);
                y += altoTexto;
            }

            y += gapChico;
            float indent = UiStyles.S(4f);
            for (int k = 0; k < 3; k++)
            {
                var comp = orders[i + k];
                var fila = ObtenerFila(comp); // cache de "n/total"/"hecho" -- mismo criterio que las filas clásicas.
                string glifo = comp.Completado ? "✓" : "▫";
                string linea = glifo + " " + comp.GrupoEtiqueta + "  " + fila.TextoProgreso;

                float xLinea = x + indent;
                float anchoLinea = interior - indent;

                // (contrato §1c) LA FLECHA AL BUZÓN aplica al compuesto igual que a los
                // Guiado -- mismo criterio y mismos métodos que ya usa cada fila clásica.
                if (modoSemillaCero && !comp.Completado && VaALaTolva(comp.Tipo) && TryFlechaTolva(out string flechaGlifo))
                {
                    var flechaRect = new Rect(xLinea, y + (altoLinea - ladoCaja) * 0.5f, ladoCaja, ladoCaja);
                    UiStyles.Rellenar(flechaRect, UiStyles.Oro);
                    var previoColorFlecha = UiStyles.ChipMini.normal.textColor;
                    UiStyles.ChipMini.normal.textColor = new Color(0f, 0f, 0f, 0.82f);
                    GUI.Label(flechaRect, flechaGlifo, UiStyles.ChipMini);
                    UiStyles.ChipMini.normal.textColor = previoColorFlecha;
                    xLinea += ladoCaja + UiStyles.S(4f);
                    anchoLinea -= ladoCaja + UiStyles.S(4f);
                }

                GUI.Label(new Rect(xLinea, y, anchoLinea, altoLinea), linea,
                    comp.Completado ? UiStyles.CuerpoTenue : UiStyles.CuerpoLinea);
                y += altoLinea;
            }
        }

        private void OnGUI()
        {
            // (playtest 21, EL PIVOT) HudSilenciado, hermano de InputLocked
            // -- misma línea, ver el docblock de DayCycle.HudSilenciado.
            if (DayCycle.InputLocked || DayCycle.HudSilenciado) return;

            if (_orderSystem == null)
            {
                if (ModoReplicado) OnGuiReplicado();
                return;
            }

            UiStyles.Preparar();

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
            // (CONTRATO_RONDA50.md §3c) SOLO Semilla Cero reserva un segundo
            // cuadrito para la flecha (contrato §3e: "el caótico NO
            // cambia") -- en caótico `anchoFlechaTolva`=0 y el layout entero
            // queda BIT A BIT igual que antes de esta ronda.
            bool modoSemillaCero = AlkahestGameBootstrap.ModoSemillaCero;
            float anchoFlechaTolva = modoSemillaCero ? ladoCaja + UiStyles.S(4f) : 0f;
            float xTextoDesde = ladoCaja + anchoFlechaTolva + UiStyles.S(6f);
            float anchoTextoProgreso = interior - xTextoDesde - anchoRecompensa;

            float altoLinea = UiStyles.S(17f);
            float altoBarra = UiStyles.S(8f);
            float gapChico = UiStyles.S(3f);
            float gapFila = UiStyles.S(5f);

            // ---- 1) Medir ----
            // (L1, fuera el Favor) La cabecera medía además una línea "N ★ → escalón"
            // y su barra dorada (altoBarraFavor) -- retiradas, ver el bloque regla-15
            // donde vivía ActualizarFavorTexto.
            float alto = pad
                       + altoLinea                    // "ENCARGOS · O expande/pliega"
                       + gapFila;
            for (int i = 0; i < orders.Count; i++)
            {
                var orderMedido = orders[i];
                if (orderMedido.GrupoId != null)
                {
                    // (ronda 56, CONTRATO_RONDA56.md §1c) EL ENCARGO COMPUESTO: bloque de 3
                    // líneas en vez de una fila -- ver AltoBloqueCompuesto/DibujarBloqueCompuesto.
                    // Las 3 hermanas SIEMPRE llegan consecutivas (OrderSystem.EncolarCompuesto),
                    // así que se miden/dibujan de una sola vez y el índice salta las otras dos.
                    alto += AltoBloqueCompuesto(orderMedido, expandido, interior, altoLinea, gapChico) + gapFila;
                    i += 2;
                    continue;
                }
                alto += altoLinea; // fila compacta: caja + progreso + recompensa.
                if (expandido)
                {
                    alto += gapChico + altoBarra;
                    alto += gapChico + UiStyles.Alto(UiStyles.CuerpoTenue, orderMedido.Descripcion, interior - xTextoDesde);
                }
                alto += gapFila;
            }
            // (playtest 23, fix del reporte de Cesar "al presionar la O no se
            // desplegó el menú de misiones"): no era la tecla -- era que en el
            // pivot los encargos NO EXISTEN hasta cavar hasta la Tolva, y con
            // cero encargos este panel medía solo cabecera+Favor: se abría a
            // un panel casi vacío que no parecía "abierto". Con cero encargos
            // se mide (y dibuja, ver abajo) una línea que lo DICE, en vez de
            // callar -- regla 43: lo indistinguible de "no pasó nada" no
            // ocurrió.
            if (orders.Count == 0) alto += UiStyles.Alto(UiStyles.CuerpoTenue, TextoVacio, interior) + gapFila;
            alto += pad - (orders.Count > 0 ? gapFila : 0f); // el último encargo ya dejó el aire de abajo: no duplicarlo.

            var panel = new Rect(Screen.width - ancho - margen, margen, ancho, alto);
            UiStyles.Panel(panel);

            // ---- 2) Cabecera ----
            float x = panel.x + pad;
            float y = panel.y + pad;

            GUI.Label(new Rect(x, y, interior, altoLinea), expandido ? TituloExpandido : TituloColapsado, UiStyles.Titulo);
            y += altoLinea + gapFila;
            // (L1, fuera el Favor) Aquí se dibujaban la línea "N ★ → escalón" y la
            // barra dorada de progreso al siguiente tier -- retiradas (regla 15,
            // ver el bloque donde vivía ActualizarFavorTexto).

            // (playtest 23) Cero encargos: decirlo. Ver el comentario de la medición.
            if (orders.Count == 0)
            {
                float altoTexto = UiStyles.Alto(UiStyles.CuerpoTenue, TextoVacio, interior);
                GUI.Label(new Rect(x, y, interior, altoTexto), TextoVacio, UiStyles.CuerpoTenue);
                y += altoTexto + gapFila;
            }

            // ---- 3) Encargos: fila compacta siempre, detalle solo si expandido ----
            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                if (order.GrupoId != null)
                {
                    DibujarBloqueCompuesto(orders, i, ref y, x, interior, altoLinea, gapChico, anchoRecompensa,
                        expandido, modoSemillaCero, ladoCaja);
                    y += gapFila;
                    i += 2;
                    continue;
                }

                var fila = ObtenerFila(order);

                TipoVisual(order.Tipo, order.Completado, out Color colorCaja, out string glifo);
                var cajaRect = new Rect(x, y + (altoLinea - ladoCaja) * 0.5f, ladoCaja, ladoCaja);
                UiStyles.Rellenar(cajaRect, colorCaja);
                var previoColorChip = UiStyles.ChipMini.normal.textColor;
                UiStyles.ChipMini.normal.textColor = new Color(0f, 0f, 0f, 0.82f); // contraste oscuro sobre el cuadrito claro.
                GUI.Label(cajaRect, glifo, UiStyles.ChipMini);
                UiStyles.ChipMini.normal.textColor = previoColorChip;

                // (CONTRATO_RONDA50.md §3c) LA FLECHA A LA TOLVA -- segundo
                // cuadrito, mismo criterio visual que el de arriba, SOLO en
                // Semilla Cero y SOLO para encargos que de verdad se
                // resuelven en la Tolva (ver VaALaTolva/TryFlechaTolva). Sin
                // jugador/Tolva encontrados todavía (un frame de arranque),
                // el hueco reservado se queda en blanco -- no rompe el
                // layout, solo no dice nada ese frame.
                if (modoSemillaCero && !order.Completado && VaALaTolva(order.Tipo) && TryFlechaTolva(out string flechaGlifo))
                {
                    var flechaRect = new Rect(x + ladoCaja + UiStyles.S(3f), y + (altoLinea - ladoCaja) * 0.5f, ladoCaja, ladoCaja);
                    UiStyles.Rellenar(flechaRect, UiStyles.Oro);
                    var previoColorFlecha = UiStyles.ChipMini.normal.textColor;
                    UiStyles.ChipMini.normal.textColor = new Color(0f, 0f, 0f, 0.82f);
                    GUI.Label(flechaRect, flechaGlifo, UiStyles.ChipMini);
                    UiStyles.ChipMini.normal.textColor = previoColorFlecha;
                }

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

        // =================================================================
        // (playtest 36, EL CAMINO DEL INVITADO) READ-ONLY, ESTADO REPLICADO.
        // =================================================================
        /// <summary>
        /// Cache de texto por Id de encargo replicado -- MISMO criterio de
        /// "cero strings por frame salvo cuando cambian de verdad" que
        /// <see cref="FilaCache"/>/<see cref="ObtenerFila"/> usan para el
        /// modo local, aplicado a <see cref="SaberSync.EntradaOrden"/> en vez
        /// de a <see cref="Order"/> (la descripción llega como
        /// <c>FixedString128Bytes</c>, cuyo <c>ToString()</c> asigna: solo se
        /// llama cuando el valor cambió).
        /// </summary>
        private sealed class DescripcionCache
        {
            public Unity.Collections.FixedString128Bytes Ultima;
            public string Texto = "";
        }
        private readonly Dictionary<int, DescripcionCache> _descripcionCache = new Dictionary<int, DescripcionCache>(4);

        /// <summary>
        /// El invitado no tiene <see cref="OrderSystem"/> local (vive solo en
        /// el anfitrión): esta rama pinta el MISMO panel de arriba-derecha
        /// con lo que <see cref="Net.SaberSync"/> replica -- descripción,
        /// progreso, completado, recompensa por encargo, más el Favor
        /// compartido del taller. READ-ONLY de verdad: ni una sola línea de
        /// aquí puede escribir en <c>SaberSync</c> (la autoridad de los
        /// encargos, como la de la sim, es SIEMPRE el anfitrión). Sin el
        /// pulso automático de expansión (ver el docblock de <see cref="Update"/>);
        /// **O** sigue expandiendo/plegando a mano igual que en modo local.
        /// </summary>
        private void OnGuiReplicado()
        {
            var saber = SaberSync.Instancia;
            if (saber == null) return;

            // (playtest 52, CO-OP GUIADO, contrato §3: "en el invitado, OrdersHud pinta la
            // línea del Maestro con el MISMO estilo visual que usa SemillaCero.OnGUI en el
            // host") El invitado no tiene NINGUNA instancia de Game/SemillaCero.cs (su Init
            // se auto-veta fuera del anfitrión) -- lee la línea que Net/SaberSync.cs replicó
            // (ver SondearMaestro en ese archivo) y la pinta con
            // SemillaCero.DibujarPanelMaestro, el MISMO método estático que usa el host, sin
            // duplicar la aritmética del panel. Se actualiza antes de dibujar nada más: no
            // depende de `expandido`/Favor/encargos, es un canal aparte (panel centro-bajo,
            // no compite en layout con el de arriba-derecha).
            ActualizarMaestroReplicado(saber);
            if (_maestroTextoLocal != null && Time.time < _maestroHastaLocal)
                SemillaCero.DibujarPanelMaestro(_maestroTextoLocal);

            UiStyles.Preparar();
            bool expandido = Expandido;
            int n = saber.CountOrdenesReplicadas;

            float margen = UiStyles.S(10f);
            float pad = UiStyles.S(7f);
            float ancho = UiStyles.S(300f);
            float interior = ancho - pad * 2f;
            float ladoCaja = UiStyles.S(14f);
            float anchoRecompensa = UiStyles.S(48f);
            float xTextoDesde = ladoCaja + UiStyles.S(6f);
            float anchoTextoProgreso = interior - xTextoDesde - anchoRecompensa;

            float altoLinea = UiStyles.S(17f);
            float altoBarra = UiStyles.S(8f);
            float gapChico = UiStyles.S(3f);
            float gapFila = UiStyles.S(5f);

            // (L1, fuera el Favor) Aquí se leía saber.FavorReplicado para la línea
            // "N ★" del invitado -- retirada; la NetworkVariable sigue existiendo en
            // SaberSync (inofensiva) hasta la limpieza del caótico.
            float alto = pad + altoLinea + gapFila;
            for (int i = 0; i < n; i++)
            {
                alto += altoLinea;
                if (expandido)
                {
                    var e = saber.ObtenerOrdenReplicada(i);
                    alto += gapChico + altoBarra;
                    alto += gapChico + UiStyles.Alto(UiStyles.CuerpoTenue, DescripcionCacheada(e), interior - xTextoDesde);
                }
                alto += gapFila;
            }
            // (playtest 55, ronda B, "B4") TextoVacio, NO TextoSinEncargos a secas -- ver
            // TextoVacio arriba: en Semilla Cero, la clásica "la Tolva del Maestro sigue
            // sellada tras la roca, hacia la derecha" ya NO describe el mundo (la Tolva de
            // ese modo es el Buzón del Maestro desde el playtest 54, y ni siquiera está
            // "hacia la derecha") -- esta rama replicada (el invitado) se había quedado con
            // el texto clásico a secas mientras la rama local (arriba, línea 399) ya usaba
            // TextoVacio desde que existe ese distingo. Mismo criterio: el caótico no cambia
            // (TextoVacio ahí sigue devolviendo TextoSinEncargos), Semilla Cero ahora sí ve
            // "(nada por ahora)" también en el invitado.
            if (n == 0) alto += UiStyles.Alto(UiStyles.CuerpoTenue, TextoVacio, interior) + gapFila;
            alto += pad - (n > 0 ? gapFila : 0f);

            var panel = new Rect(Screen.width - ancho - margen, margen, ancho, alto);
            UiStyles.Panel(panel);

            float x = panel.x + pad;
            float y = panel.y + pad;

            GUI.Label(new Rect(x, y, interior, altoLinea), expandido ? TituloExpandido : TituloColapsado, UiStyles.Titulo);
            y += altoLinea + gapFila;

            if (n == 0)
            {
                float altoTexto = UiStyles.Alto(UiStyles.CuerpoTenue, TextoVacio, interior);
                GUI.Label(new Rect(x, y, interior, altoTexto), TextoVacio, UiStyles.CuerpoTenue);
                y += altoTexto + gapFila;
            }

            for (int i = 0; i < n; i++)
            {
                var e = saber.ObtenerOrdenReplicada(i);

                TipoVisual((OrderType)e.tipo, e.completado, out Color colorCaja, out string glifo);
                var cajaRect = new Rect(x, y + (altoLinea - ladoCaja) * 0.5f, ladoCaja, ladoCaja);
                UiStyles.Rellenar(cajaRect, colorCaja);
                var previoColorChip = UiStyles.ChipMini.normal.textColor;
                UiStyles.ChipMini.normal.textColor = new Color(0f, 0f, 0f, 0.82f);
                GUI.Label(cajaRect, glifo, UiStyles.ChipMini);
                UiStyles.ChipMini.normal.textColor = previoColorChip;

                float xTexto = x + xTextoDesde;
                string textoProgreso = e.completado ? "hecho" : (e.progreso + "/" + e.minCells);
                GUI.Label(new Rect(xTexto, y, anchoTextoProgreso, altoLinea), textoProgreso,
                    e.completado ? UiStyles.CuerpoTenue : UiStyles.CuerpoLinea);
                // (L1, fuera el Favor) Aquí se dibujaba "+N ★" (e.recompensa) -- retirado.
                y += altoLinea;

                if (expandido)
                {
                    y += gapChico;
                    float frac = e.completado ? 1f : Mathf.Clamp01((float)e.progreso / Mathf.Max(1, e.minCells));
                    UiStyles.Barra(new Rect(xTexto, y, anchoTextoProgreso + anchoRecompensa, altoBarra),
                        frac, e.completado ? UiStyles.Exito : UiStyles.Oro);
                    y += altoBarra + gapChico;

                    string desc = DescripcionCacheada(e);
                    float altoDesc = UiStyles.Alto(UiStyles.CuerpoTenue, desc, interior - xTextoDesde);
                    GUI.Label(new Rect(xTexto, y, interior - xTextoDesde, altoDesc), desc, UiStyles.CuerpoTenue);
                    y += altoDesc;
                }

                y += gapFila;
            }
        }

        /// <summary>
        /// (playtest 52, CO-OP GUIADO) Sincroniza <see cref="_maestroTextoLocal"/>/
        /// <see cref="_maestroHastaLocal"/> contra <see cref="SaberSync.MaestroTexto"/>/
        /// <see cref="SaberSync.MaestroSegundosRestantes"/> SOLO cuando el texto replicado
        /// cambió desde la última vez (comparación de <c>FixedString128Bytes</c>, sin alloc).
        /// El "hasta" se calcula UNA vez, en el instante del cambio, sumando los segundos
        /// restantes que el anfitrión publicó a un `Time.time` LOCAL -- de ahí en adelante el
        /// invitado cuenta su propio reloj (no vuelve a preguntar mientras el texto no
        /// cambie), igual que el host cuenta el suyo en `SemillaCero._maestroHasta`.
        /// </summary>
        private void ActualizarMaestroReplicado(SaberSync saber)
        {
            var actual = saber.MaestroTexto.Value;
            if (actual.Equals(_maestroTextoVisto)) return; // sin cambio desde el último frame.
            _maestroTextoVisto = actual;

            if (actual.Length == 0) { _maestroTextoLocal = null; return; }
            _maestroTextoLocal = actual.ToString();
            _maestroHastaLocal = Time.time + saber.MaestroSegundosRestantes.Value;
        }

        /// <summary>
        /// Solo llama a <c>FixedString128Bytes.ToString()</c> (asigna) la
        /// primera vez que se ve un Id o cuando su descripción cambió de
        /// verdad (comparación de struct, sin coste) -- mismo criterio de
        /// cero-alloc-por-frame que el resto del archivo. Necesario porque
        /// un re-bautizo SÍ puede cambiar la descripción de un encargo ya
        /// visto (regla 13 de CLAUDE.md, <c>OrderSystem.RefreshDescripciones</c>
        /// conserva el Id) -- cachear solo por Id, sin comparar el valor,
        /// serviría para siempre la frase VIEJA.
        /// </summary>
        private string DescripcionCacheada(SaberSync.EntradaOrden e)
        {
            if (!_descripcionCache.TryGetValue(e.id, out var cache))
            {
                cache = new DescripcionCache();
                _descripcionCache[e.id] = cache;
            }
            if (!cache.Ultima.Equals(e.descripcion))
            {
                cache.Ultima = e.descripcion;
                cache.Texto = e.descripcion.ToString();
            }
            return cache.Texto;
        }
    }
}
