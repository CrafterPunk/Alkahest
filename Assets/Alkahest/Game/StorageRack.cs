using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// LA ESTANTERÍA DE REDOMAS — "tubos donde guardar las mezclas etiquetadas"
    /// (petición literal del playtest 4).
    ///
    /// Cinco redomas de vidrio sobre un listón de madera del estante superior.
    /// Cada redoma guarda UN SOLO material (hasta <see cref="CapacidadRedoma"/>
    /// celdas) y conserva su temperatura, igual que el frasco:
    ///
    ///   · CLIC DERECHO apuntando a una redoma  -> VERTER del frasco a la redoma.
    ///     Si está vacía, adopta el material del que más lleves; si ya tiene
    ///     dueño, solo acepta más de ESE material.
    ///   · CLIC IZQUIERDO apuntando a una redoma -> ASPIRAR de vuelta al frasco.
    ///   · Cada redoma llena enseña SIEMPRE su cantidad; el NOMBRE completo
    ///     (SubstanceKnowledge.NombreParaHud) solo se dibuja en la que señala
    ///     el ratón o, si no, en la más cercana al aprendiz — 5 redomas en
    ///     ~13 celdas no dejan sitio para cinco nombres largos a la vez (fix
    ///     playtest 7; detalle en la sección "Etiquetas" más abajo).
    ///
    /// POR QUÉ IMPORTA AL DISEÑO: es el almacén VISIBLE del conocimiento del
    /// grupo (decisiones §11-§13). El frasco es memoria a corto plazo y se
    /// mezcla; la estantería es la despensa etiquetada donde el "azoth
    /// cristalizado" que tanto costó fabricar espera al encargo del día
    /// siguiente. Es también el único sitio del taller donde ver de un vistazo
    /// los nombres que el grupo ha inventado, escritos sobre la materia real.
    ///
    /// Nota de input: mientras el cursor está sobre una redoma, esta clase
    /// CAPTURA el ratón (ver <see cref="RatonSobreRedoma"/>) y Game/Flask.cs
    /// ignora los clics — si no, verter sobre el estante pintaría material
    /// suelto encima del mueble.
    /// </summary>
    public sealed class StorageRack : MonoBehaviour
    {
        public const int CapacidadRedoma = 300;
        public const int NumRedomas = 5;

        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        /// <summary>Celdas transferidas por tick (~360/s: llenar una redoma entera cuesta menos de un segundo, pero se ve el líquido subir).</summary>
        private const int TransferPorTick = 12;

        /// <summary>Alcance del aprendiz para operar la estantería (unidades de mundo).</summary>
        private const float RangoJugador = 4.5f;

        // Medidas de una redoma en unidades de mundo.
        private const float RedomaAncho = 0.62f;
        private const float RedomaAlto = 1.52f;
        private const float RedomaSeparacion = 1.18f;
        /// <summary>Semi-anchura del área sensible al cursor (algo más generosa que el vidrio).</summary>
        private const float RadioRatonX = 0.40f;

        private sealed class Redoma
        {
            public byte Mat;
            public int Cantidad;
            public int SumaTemp;
            public float MundoX;
            public SpriteRenderer Contenido;
            public Transform ContenidoTr;
            public SpriteRenderer Tapon;
            public float BaseY;

            public byte TempMedia => Cantidad > 0
                ? (byte)Mathf.Clamp(SumaTemp / Cantidad, 0, 255)
                : CellGrid.AmbientRaw;

            // Caches de las etiquetas de OnGUI (fix playtest 7 — "no construir
            // strings cuando el texto no cambia"): solo se reconstruyen si
            // cambia el nombre, la cantidad o el hueco disponible en pantalla,
            // no en CADA frame como hacía la concatenación original.
            public string CantidadCacheTexto;
            public int CantidadCacheValor = int.MinValue;

            public string NombreCacheTexto;
            public string NombreCacheFuente;
            public int NombreCacheCantidad = int.MinValue;
            public float NombreCacheAncho = -1f;
        }

        private static StorageRack _instancia;

        private AlkahestSim _sim;
        private Flask _frasco;
        private SubstanceKnowledge _saber;
        private Transform _jugador;

        private readonly Redoma[] _redomas = new Redoma[NumRedomas];
        private float _accumulator;

        private int _hover = -1;
        private int _hoverFrame = -1;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, Flask frasco, SubstanceKnowledge saber, Transform jugador,
            int cellX0, int cellX1, int cellYBase)
        {
            _sim = sim;
            _frasco = frasco;
            _saber = saber;
            _jugador = jugador;
            _instancia = this;

            BuildVisual(cellX0, cellX1, cellYBase);
        }

        private void OnDestroy()
        {
            if (_instancia == this) _instancia = null;
        }

        // -----------------------------------------------------------------
        // Construcción del mueble
        // -----------------------------------------------------------------
        private void BuildVisual(int cellX0, int cellX1, int cellYBase)
        {
            float celda = SimRenderer.CellWorldSize;
            float izq = cellX0 * celda;
            float der = (cellX1 + 1) * celda;
            float baseY = cellYBase * celda;

            transform.position = new Vector3((izq + der) * 0.5f, baseY + RedomaAlto * 0.5f, 0f);

            // Listón de madera: el mueble. Va DETRÁS de las redomas.
            var liston = MaquinariaSprites.CrearCapa(transform, "Liston",
                MaquinariaSprites.ListonEstante(Mathf.RoundToInt((der - izq) * 20f)), 17,
                der - izq, 0.24f);
            liston.transform.position = new Vector3((izq + der) * 0.5f, baseY + RedomaAlto * 0.55f, 0f);

            // Cinco redomas centradas sobre el listón.
            float anchoTotal = RedomaSeparacion * (NumRedomas - 1);
            float x0 = (izq + der) * 0.5f - anchoTotal * 0.5f;

            for (int i = 0; i < NumRedomas; i++)
            {
                var r = new Redoma
                {
                    Mat = MaterialId.Empty,
                    MundoX = x0 + i * RedomaSeparacion,
                    BaseY = baseY,
                };

                // Contenido DETRÁS del vidrio (orden menor): se ve "dentro".
                r.Contenido = MaquinariaSprites.CrearCapa(transform, $"Contenido_{i}",
                    MaquinariaSprites.ContenidoRedoma(), 21, RedomaAncho, RedomaAlto);
                r.ContenidoTr = r.Contenido.transform;
                r.Contenido.color = new Color(1f, 1f, 1f, 0f);

                var vidrio = MaquinariaSprites.CrearCapa(transform, $"Vidrio_{i}",
                    MaquinariaSprites.VidrioRedoma(), 22, RedomaAncho, RedomaAlto);
                vidrio.transform.position = new Vector3(r.MundoX, baseY + RedomaAlto * 0.5f, 0f);

                r.Tapon = MaquinariaSprites.CrearCapa(transform, $"Tapon_{i}",
                    MaquinariaSprites.TaponRedoma(), 23, RedomaAncho * 0.58f, RedomaAlto * 0.13f);
                r.Tapon.transform.position = new Vector3(r.MundoX, baseY + RedomaAlto * 1.0f, 0f);
                r.Tapon.color = new Color(1f, 1f, 1f, 0.75f);

                _redomas[i] = r;
                ActualizarRedoma(r);
            }
        }

        /// <summary>
        /// Ajusta el nivel visible del líquido/polvo: el sprite del contenido se
        /// recorta desde ABAJO escalándolo y bajando su pivote, de forma que la
        /// redoma se llena de verdad conforme entra materia.
        /// </summary>
        private void ActualizarRedoma(Redoma r)
        {
            if (r.Contenido == null) return;

            if (r.Cantidad <= 0 || r.Mat == MaterialId.Empty)
            {
                r.Contenido.color = new Color(1f, 1f, 1f, 0f);
                if (r.Tapon != null) r.Tapon.color = new Color(1f, 1f, 1f, 0.45f);
                return;
            }

            float frac = Mathf.Clamp01((float)r.Cantidad / CapacidadRedoma);
            // Un dedo de contenido siempre visible aunque quede poquísimo.
            float altura = Mathf.Lerp(0.10f, 1f, frac);

            var baseEscala = RedomaAlto / MaquinariaSprites.ContenidoRedoma().rect.height;
            var e = r.ContenidoTr.localScale;
            r.ContenidoTr.localScale = new Vector3(e.x, baseEscala * altura, 1f);
            r.ContenidoTr.position = new Vector3(r.MundoX, r.BaseY + RedomaAlto * altura * 0.5f, 0f);

            Color32 c = _sim.Universe.Get(r.Mat).baseColor;
            r.Contenido.color = new Color(c.r / 255f, c.g / 255f, c.b / 255f, 0.94f);
            if (r.Tapon != null) r.Tapon.color = new Color(1f, 1f, 1f, 1f);
        }

        // -----------------------------------------------------------------
        // Captura del ratón (consultada por Game/Flask.cs)
        // -----------------------------------------------------------------

        /// <summary>
        /// ¿Está el cursor sobre alguna redoma en ESTE frame? Se calcula bajo
        /// demanda y se cachea por Time.frameCount, así que da igual el orden en
        /// el que Unity llame a los Update() de Flask y de esta clase.
        /// </summary>
        public static bool RatonSobreRedoma()
        {
            if (_instancia == null) return false;
            return _instancia.RedomaBajoCursor() >= 0;
        }

        /// <summary>
        /// (fix playtest 7 — CAUSA RAÍZ de "bauticé algo y no se ve en la
        /// botella, y encima me pisó otro nombre") Material de la redoma bajo
        /// el cursor, o Empty si no hay ninguna (o está vacía). NamingUi.cs lo
        /// consulta ANTES de su lógica normal: sin esto, apuntar con el ratón
        /// a una redoma no sirve de nada porque las redomas son un mueble
        /// puramente visual —no ocupan celdas de la simulación— así que el
        /// muestreo de <c>AlkahestSim.SampleMaterial</c> bajo el cursor cae
        /// siempre sobre la PIEDRA del listón (MaterialId.Stone) y NamingUi
        /// se repliega a "el material dominante del FRASCO", que puede ser
        /// una sustancia completamente distinta a la guardada en la redoma
        /// que el jugador está señalando. Resultado exacto del playtest: se
        /// bautiza lo que hay en el frasco (aparece en el panel del frasco,
        /// que consulta el mismo diccionario por materialId) mientras la
        /// redoma se queda en "???", y si el frasco llevaba un material YA
        /// bautizado antes, ese nombre previo queda pisado sin querer.
        /// </summary>
        public static byte MaterialBajoCursor()
        {
            if (_instancia == null) return MaterialId.Empty;
            int i = _instancia.RedomaBajoCursor();
            if (i < 0) return MaterialId.Empty;
            var r = _instancia._redomas[i];
            return r != null ? r.Mat : MaterialId.Empty;
        }

        private int RedomaBajoCursor()
        {
            if (_hoverFrame == Time.frameCount) return _hover;
            _hoverFrame = Time.frameCount;
            _hover = -1;

            if (_sim == null || DayCycle.InputLocked || Alkahest.Dev.DevPalette.IsOpen) return _hover;

            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return _hover;

            Vector2 screenPos = mouse.position.ReadValue();
            var ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var plano = new Plane(Vector3.forward, Vector3.zero);
            if (!plano.Raycast(ray, out float enter)) return _hover;

            Vector3 mundo = ray.GetPoint(enter);
            for (int i = 0; i < NumRedomas; i++)
            {
                var r = _redomas[i];
                if (r == null) continue;
                if (Mathf.Abs(mundo.x - r.MundoX) > RadioRatonX) continue;
                if (mundo.y < r.BaseY - 0.15f || mundo.y > r.BaseY + RedomaAlto + 0.15f) continue;
                _hover = i;
                break;
            }
            return _hover;
        }

        // -----------------------------------------------------------------
        // Lógica
        // -----------------------------------------------------------------
        private void Update()
        {
            if (_sim == null || _frasco == null) return;
            if (DayCycle.InputLocked || Alkahest.Dev.DevPalette.IsOpen) return;

            int i = RedomaBajoCursor();
            var mouse = Mouse.current;
            if (i < 0 || mouse == null) { _accumulator = 0f; return; }

            bool guardar = mouse.rightButton.isPressed;
            bool recuperar = mouse.leftButton.isPressed;
            if (!guardar && !recuperar) { _accumulator = 0f; return; }

            if (!JugadorCerca(_redomas[i]))
            {
                _frasco.Avisar("demasiado lejos de la estantería");
                _accumulator = 0f;
                return;
            }

            _accumulator += Time.deltaTime;
            int steps = 0;
            while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
            {
                if (guardar) GuardarEnRedoma(_redomas[i]);
                else RecuperarDeRedoma(_redomas[i]);
                _accumulator -= TickDt;
                steps++;
            }
            if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;
        }

        private bool JugadorCerca(Redoma r)
        {
            if (_jugador == null) return true;
            Vector2 centro = new Vector2(r.MundoX, r.BaseY + RedomaAlto * 0.5f);
            Vector2 p = _jugador.position;
            return (p - centro).sqrMagnitude <= RangoJugador * RangoJugador;
        }

        private void GuardarEnRedoma(Redoma r)
        {
            if (r.Cantidad >= CapacidadRedoma)
            {
                _frasco.Avisar("redoma llena (" + CapacidadRedoma + ")");
                return;
            }

            // Una redoma = un solo material: si ya tiene dueño, solo acepta más
            // de lo mismo; si está vacía, adopta lo que más lleves en el frasco.
            byte mat = r.Mat != MaterialId.Empty ? r.Mat : _frasco.MaterialDominante();
            if (mat == MaterialId.Empty)
            {
                _frasco.Avisar("frasco vacío — aspira algo primero");
                return;
            }
            if (_frasco.GetCount(mat) <= 0)
            {
                _frasco.Avisar("esta redoma guarda " + NombreDe(mat) + " · el frasco no lleva");
                return;
            }

            int cabe = Mathf.Min(TransferPorTick, CapacidadRedoma - r.Cantidad);
            int n = _frasco.Extraer(mat, cabe, out byte tempRaw);
            if (n <= 0) return;

            r.Mat = mat;
            r.Cantidad += n;
            r.SumaTemp += tempRaw * n;
            ActualizarRedoma(r);
        }

        private void RecuperarDeRedoma(Redoma r)
        {
            if (r.Cantidad <= 0 || r.Mat == MaterialId.Empty)
            {
                _frasco.Avisar("redoma vacía");
                return;
            }

            byte temp = r.TempMedia;
            int n = _frasco.Guardar(r.Mat, Mathf.Min(TransferPorTick, r.Cantidad), temp);
            if (n <= 0)
            {
                _frasco.Avisar("frasco lleno — vacíalo (Q) o vierte antes");
                return;
            }

            r.Cantidad -= n;
            r.SumaTemp -= temp * n;
            if (r.SumaTemp < 0) r.SumaTemp = 0;
            if (r.Cantidad <= 0) { r.Cantidad = 0; r.SumaTemp = 0; r.Mat = MaterialId.Empty; }
            ActualizarRedoma(r);
        }

        private string NombreDe(byte matId)
        {
            if (_saber != null) return _saber.NombreParaHud(matId);
            return SubstanceKnowledge.NombreComun(matId) ?? "???";
        }

        // -----------------------------------------------------------------
        // Etiquetas
        //
        // (fix playtest 7 — "las etiquetas no caben") Con 5 redomas metidas en
        // ~13 celdas cada una, un nombre bautizado largo ("que verga") centrado
        // sobre CADA botella se come a la vecina. La solución, sin mover ni una
        // redoma: la CANTIDAD (indicador barato de "hay algo aquí y cuánto") se
        // dibuja SIEMPRE que la redoma tenga contenido — el color del líquido ya
        // viene dado por el propio sprite de <see cref="ActualizarRedoma"/>, que
        // se apaga a alfa 0 cuando está vacía, así que vacía/llena ya se
        // distinguen sin leer nada. El NOMBRE completo, en cambio, solo se
        // dibuja en UNA redoma a la vez: la que señala el ratón si hay alguna, o
        // si no la más cercana al aprendiz (con <see cref="UiStyles.Cercania"/>,
        // igual que el resto de rótulos del taller) — así nunca hay dos nombres
        // largos compitiendo por el mismo hueco. Y si aun así no cabe en el
        // espacio libre hasta la redoma vecina, se trunca con "…".
        // -----------------------------------------------------------------
        private const float RangoNombrePleno = RangoJugador;
        private const float RangoNombreDesvanece = RangoJugador + 2.5f;
        /// <summary>Margen de seguridad para no tocar el hueco disponible hasta la redoma vecina.</summary>
        private const float FraccionHuecoUsable = 0.88f;

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked) return;

            UiStyles.Preparar();
            int hover = RedomaBajoCursor();

            // La más cercana al aprendiz entre las que tienen contenido (para
            // decidir quién enseña el nombre completo cuando el ratón no señala
            // ninguna redoma).
            int cercana = -1;
            float mejorD2 = float.MaxValue;
            if (_jugador != null)
            {
                Vector2 pj = _jugador.position;
                for (int i = 0; i < NumRedomas; i++)
                {
                    var r = _redomas[i];
                    if (r == null || r.Cantidad <= 0) continue;
                    Vector2 centro = new Vector2(r.MundoX, r.BaseY + RedomaAlto * 0.5f);
                    float d2 = (pj - centro).sqrMagnitude;
                    if (d2 < mejorD2) { mejorD2 = d2; cercana = i; }
                }
            }
            int etiquetaIdx = hover >= 0 && _redomas[hover] != null && _redomas[hover].Cantidad > 0 ? hover : cercana;

            for (int i = 0; i < NumRedomas; i++)
            {
                var r = _redomas[i];
                if (r == null) continue;

                var cima = new Vector3(r.MundoX, r.BaseY + RedomaAlto, 0f);

                if (r.Cantidad > 0)
                {
                    // Indicador PERMANENTE: cantidad, siempre visible (criterio
                    // "vacía se distingue de llena de un vistazo" — el color lo
                    // aporta el propio líquido del sprite).
                    UiStyles.PlacaMundo(cima, CantidadTexto(r), i == hover ? UiStyles.Oro : UiStyles.TextoTenue, UiStyles.S(11f));

                    if (i == etiquetaIdx)
                    {
                        float alfa = i == hover ? 1f : UiStyles.Cercania(cima, _jugador, RangoNombrePleno, RangoNombreDesvanece);
                        if (alfa > 0.02f)
                        {
                            Color c = i == hover ? UiStyles.Oro : UiStyles.Texto;
                            float ancho = HuecoDisponiblePx(i);
                            // Ancla en un punto de MUNDO distinto al de la cantidad
                            // (no solo un desplazarPx distinto): así el hueco entre
                            // ambas placas escala igual que el resto del taller con
                            // el zoom/resolución y nunca quedan pegadas o solapadas.
                            var cimaNombre = cima + Vector3.up * 0.24f;
                            UiStyles.PlacaMundo(cimaNombre, EtiquetaNombre(r, ancho), c, UiStyles.S(11f), alfa);
                        }
                    }
                }
                else if (i == hover)
                {
                    UiStyles.PlacaMundo(cima, "redoma vacía", UiStyles.TextoTenue, UiStyles.S(11f));
                }
            }

            // Instrucción de uso: solo cuando apuntas a una redoma y no estás ya
            // trasvasando (misma regla que las placas — el prompt no estorba la
            // acción en curso).
            if (hover >= 0 && !UiStyles.RatonOcupado)
            {
                var r = _redomas[hover];
                var pie = new Vector3(r.MundoX, r.BaseY, 0f);
                UiStyles.PlacaMundo(pie, "clic der. guardar · clic izq. recuperar", UiStyles.Oro, -UiStyles.S(13f));
            }
        }

        /// <summary>Texto de la cantidad, cacheado: solo se reconstruye si el número cambió.</summary>
        private static string CantidadTexto(Redoma r)
        {
            if (r.CantidadCacheValor == r.Cantidad && r.CantidadCacheTexto != null) return r.CantidadCacheTexto;
            r.CantidadCacheValor = r.Cantidad;
            r.CantidadCacheTexto = r.Cantidad.ToString();
            return r.CantidadCacheTexto;
        }

        /// <summary>
        /// Ancho en píxeles de pantalla disponible para el nombre de la redoma
        /// `idx` sin invadir a su vecina más próxima (proyectando las posiciones
        /// de mundo con la cámara — la cámara del taller es FIJA, así que esto
        /// es barato y estable frame a frame). 999 si no hay cámara (defensivo).
        /// </summary>
        private float HuecoDisponiblePx(int idx)
        {
            var cam = Camera.main;
            if (cam == null) return 999f;

            float aquiX = cam.WorldToScreenPoint(new Vector3(_redomas[idx].MundoX, 0f, 0f)).x;
            float distIzq = idx > 0 ? aquiX - cam.WorldToScreenPoint(new Vector3(_redomas[idx - 1].MundoX, 0f, 0f)).x : 999f;
            float distDer = idx < NumRedomas - 1 ? cam.WorldToScreenPoint(new Vector3(_redomas[idx + 1].MundoX, 0f, 0f)).x - aquiX : 999f;
            return Mathf.Min(distIzq, distDer) * FraccionHuecoUsable * 2f; // *2: la placa se centra, así que dispone de hueco a ambos lados.
        }

        /// <summary>Nombre + cantidad de la redoma, cacheado y truncado con "…" si no cabe en `anchoMaxPx`.</summary>
        private string EtiquetaNombre(Redoma r, float anchoMaxPx)
        {
            string nombre = NombreDe(r.Mat);
            if (r.NombreCacheFuente == nombre && r.NombreCacheCantidad == r.Cantidad
                && r.NombreCacheAncho == anchoMaxPx && r.NombreCacheTexto != null)
            {
                return r.NombreCacheTexto;
            }

            string completo = nombre + "  " + r.Cantidad;
            r.NombreCacheTexto = Truncar(UiStyles.ChipMini, completo, anchoMaxPx);
            r.NombreCacheFuente = nombre;
            r.NombreCacheCantidad = r.Cantidad;
            r.NombreCacheAncho = anchoMaxPx;
            return r.NombreCacheTexto;
        }

        /// <summary>Recorta `texto` con UiStyles.Ancho hasta que quepa en `anchoMaxPx`, añadiendo "…".</summary>
        private static string Truncar(GUIStyle estilo, string texto, float anchoMaxPx)
        {
            if (string.IsNullOrEmpty(texto) || UiStyles.Ancho(estilo, texto) <= anchoMaxPx) return texto;

            string acortado = texto;
            while (acortado.Length > 1 && UiStyles.Ancho(estilo, acortado + "…") > anchoMaxPx)
            {
                acortado = acortado.Substring(0, acortado.Length - 1);
            }
            return acortado + "…";
        }
    }
}
