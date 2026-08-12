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
    ///   · Sobre cada redoma llena, una mini-etiqueta con el NOMBRE que le
    ///     pusisteis (SubstanceKnowledge.NombreParaHud) y la cantidad.
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

        private int RedomaBajoCursor()
        {
            if (_hoverFrame == Time.frameCount) return _hover;
            _hoverFrame = Time.frameCount;
            _hover = -1;

            // (fix playtest 10) Guardar/recuperar en una redoma es un atajo del MUNDO como
            // aspirar/verter (mismo criterio, ver UiStyles.EscribiendoTexto/JournalHud.Abierto):
            // con el diario abierto a pantalla completa el velo NO bloquea los clics por sí solo
            // (GUI.DrawTexture no intercepta input, solo los controles interactivos lo hacen), así
            // que sin esta guarda se podía manipular la estantería "a través" del libro.
            if (_sim == null || DayCycle.InputLocked || Alkahest.Dev.DevPalette.IsOpen || UiStyles.EscribiendoTexto || JournalHud.Abierto) return _hover;

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
            // (fix playtest 10) Ver el mismo comentario en RedomaBajoCursor: atajo del MUNDO,
            // se calla mientras se escribe un nombre o con el diario abierto a pantalla completa.
            if (DayCycle.InputLocked || Alkahest.Dev.DevPalette.IsOpen || UiStyles.EscribiendoTexto || JournalHud.Abierto) return;

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
        // -----------------------------------------------------------------
        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked) return;

            UiStyles.Preparar();
            int hover = RedomaBajoCursor();

            for (int i = 0; i < NumRedomas; i++)
            {
                var r = _redomas[i];
                if (r == null) continue;

                var cima = new Vector3(r.MundoX, r.BaseY + RedomaAlto, 0f);

                if (r.Cantidad > 0)
                {
                    // Mini-etiqueta de la redoma llena: el nombre del grupo y
                    // cuánto queda. Esto es el "almacén visible del conocimiento".
                    Color c = i == hover ? UiStyles.Oro : UiStyles.Texto;
                    UiStyles.PlacaMundo(cima, NombreDe(r.Mat) + "  " + r.Cantidad, c, UiStyles.S(11f));
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
    }
}
