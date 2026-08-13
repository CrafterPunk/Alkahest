using System;
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
    ///
    /// (fix playtest 13) FIRMA VISUAL DEL CONTENIDO: el reporte del jugador
    /// fue literal — "al llenar las botellas estos patrones no se notan ni se
    /// animan sus contenidos, lo que lo hace más dependiente del nombre". El
    /// contenido de una redoma dejó de ser un tinte plano de
    /// <c>MaterialDef.baseColor</c> sobre la máscara blanca de
    /// <see cref="MaquinariaSprites.ContenidoRedoma"/>: ahora se genera una
    /// textura por código que reproduce color+patrón+borde del material (ver
    /// <see cref="FirmaVisualFabrica"/> al fondo de este archivo), igual que
    /// ya hace <c>JournalHud.CrearMiniatura</c> para el catálogo del diario —
    /// la idea central es que UNA SOLA celda basta para dibujar la firma
    /// entera (se REGENERA a la escala que haga falta, no es una foto de lo
    /// que hay en el mundo), así que documentar ya no exige producir de más.
    /// Generada UNA VEZ POR MATERIAL (nunca por frame) y cacheada en
    /// <see cref="_firmaSprites"/>; liberada en <see cref="OnDestroy"/>.
    ///
    /// ANIMACIÓN BARATA: si <c>ritmoAnim&gt;0</c> se pregeneran hasta
    /// <see cref="FirmaVisualFabrica.AnimFrames"/> fotogramas (variando la
    /// fase de las ondas/Voronoi/sectores) y <see cref="ActualizarAnimacionContenidos"/>
    /// se limita a ALTERNAR qué Sprite ya existente muestra cada
    /// SpriteRenderer, a <see cref="FirmaVisualFabrica.AnimFps"/> — nunca se
    /// reconstruye una textura en Update. Con <c>ritmoAnim==0</c> el array
    /// de fotogramas tiene longitud 1: el índice siempre cae en el mismo
    /// Sprite y la redoma queda QUIETA de verdad, tal como pide el jugador
    /// implícitamente ("no todo debe moverse").
    ///
    /// REGLA 19 (borde Difuso sobre el MUNDO): a diferencia de la miniatura
    /// de <c>JournalHud</c> (que vive sobre el pergamino opaco del diario y
    /// SÍ puede bajar alfa), el contenido de una redoma se dibuja sobre el
    /// taller real — así que aquí el borde Difuso oscurece hacia
    /// <see cref="SimRenderer.BackgroundColor"/> en vez de tocar el canal
    /// alfa (ver <see cref="FirmaVisualFabrica.ApplyBorde"/>), exactamente el
    /// mismo criterio que ya sigue <c>SimRenderer.ComputeCellColor</c>.
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

            // (fix playtest 13) Qué Sprite de firma visual está mostrando AHORA
            // MISMO esta redoma -- para que ActualizarAnimacionContenidos solo
            // reasigne SpriteRenderer.sprite cuando de verdad cambia el
            // material o el fotograma de animación, nunca cada frame a ciegas.
            public byte MatSpriteActual;
            public int FrameSpriteActual = -1;

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

        // -----------------------------------------------------------------
        // FIRMA VISUAL DEL CONTENIDO (fix playtest 13, ver docblock de la
        // clase). _firmaSprites/_firmaTexturas indexados por MaterialId:
        // generados la PRIMERA vez que un material entra en cualquier
        // redoma, cacheados para siempre (nunca por frame), compartidos
        // entre las 5 redomas -- guardar el mismo material dos veces no
        // regenera nada. _mascaraAlpha/_mascaraEsBorde son la silueta de la
        // redoma (la máscara de MaquinariaSprites.ContenidoRedoma, leída UNA
        // vez) reutilizada como estarcido para CUALQUIER material.
        // -----------------------------------------------------------------
        private readonly Sprite[][] _firmaSprites = new Sprite[MaterialId.Count][];
        private readonly Texture2D[][] _firmaTexturas = new Texture2D[MaterialId.Count][];
        private byte[] _mascaraAlpha;
        private bool[] _mascaraEsBorde;
        private int _mascaraAncho;
        private int _mascaraAlto;

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

        /// <summary>
        /// (fix playtest 13) Igual disciplina de memoria que
        /// JournalHud.OnDestroy: un Texture2D/Sprite creado por código no se
        /// libera solo con destruir este GameObject, y DayCycle.RestartRun
        /// recarga la escena (StorageRack se recrea entero) en cada universo
        /// nuevo -- sin este bucle se acumularían huérfanas, hasta
        /// MaterialId.Count * AnimFrames texturas más por partida.
        /// </summary>
        private void OnDestroy()
        {
            if (_instancia == this) _instancia = null;

            for (int m = 0; m < _firmaTexturas.Length; m++)
            {
                var texturas = _firmaTexturas[m];
                if (texturas == null) continue;
                for (int f = 0; f < texturas.Length; f++)
                {
                    if (texturas[f] != null) Destroy(texturas[f]);
                }
            }
            for (int m = 0; m < _firmaSprites.Length; m++)
            {
                var sprites = _firmaSprites[m];
                if (sprites == null) continue;
                for (int f = 0; f < sprites.Length; f++)
                {
                    if (sprites[f] != null) Destroy(sprites[f]);
                }
            }
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

            // (fix playtest 13) Silueta de la redoma, leída UNA vez y compartida
            // por CUALQUIER material que pase por cualquiera de las 5 redomas --
            // ver PrepararMascaraFirma.
            PrepararMascaraFirma();
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

            // (fix playtest 13) El tinte plano de baseColor sobre la máscara
            // blanca se sustituye por la FIRMA VISUAL generada (ver
            // ActualizarAnimacionContenidos/ObtenerFirmaSprites más abajo): el
            // Sprite ya lleva el color+patrón+borde reales por téxel, así que
            // aquí solo queda fijar blanco puro (sin tintar de más) con la
            // MISMA opacidad de vidrio que ya tenía esta redoma.
            r.Contenido.color = new Color(1f, 1f, 1f, 0.94f);
            if (r.Tapon != null) r.Tapon.color = new Color(1f, 1f, 1f, 1f);
        }

        /// <summary>
        /// (fix playtest 13) Lee UNA VEZ la máscara de
        /// MaquinariaSprites.ContenidoRedoma (silueta blanca opaca / vacío
        /// transparente que ya diseña ese archivo de solo lectura) y precalcula
        /// qué téxeles caen "cerca del borde de la silueta" -- el equivalente,
        /// para una forma de botella, del chequeo de "vecino vacío" que
        /// SimRenderer hace por celda de grid y JournalHud.ApplyBordeMini hace
        /// por distancia al canto de un swatch cuadrado. Aquí no hay ni grid de
        /// sim ni swatch cuadrado: el borde real es la silueta de vidrio, así
        /// que se mide contra ELLA. Coste: un escaneo de vecindad de bandaBorde
        /// téxeles por téxel opaco, sobre ~66x162 téxeles -- una única vez por
        /// partida (StorageRack.Init), nunca por frame ni por material.
        /// </summary>
        private void PrepararMascaraFirma()
        {
            if (_mascaraAlpha != null) return;

            var maskTex = MaquinariaSprites.ContenidoRedoma().texture;
            _mascaraAncho = maskTex.width;
            _mascaraAlto = maskTex.height;

            var pixeles = maskTex.GetPixels32();
            _mascaraAlpha = new byte[pixeles.Length];
            for (int i = 0; i < pixeles.Length; i++) _mascaraAlpha[i] = pixeles[i].a;

            const int bandaBorde = 5; // ~8% del ancho de la redoma: mismo orden de magnitud que el 10% que usa JournalHud.
            _mascaraEsBorde = new bool[pixeles.Length];
            for (int y = 0; y < _mascaraAlto; y++)
            {
                for (int x = 0; x < _mascaraAncho; x++)
                {
                    int i = y * _mascaraAncho + x;
                    if (_mascaraAlpha[i] == 0) continue; // fuera de la silueta: no es "borde", es vacío.

                    bool esBorde = false;
                    for (int oy = -bandaBorde; oy <= bandaBorde && !esBorde; oy++)
                    {
                        int yy = y + oy;
                        if (yy < 0 || yy >= _mascaraAlto) { esBorde = true; break; }
                        for (int ox = -bandaBorde; ox <= bandaBorde; ox++)
                        {
                            int xx = x + ox;
                            if (xx < 0 || xx >= _mascaraAncho || _mascaraAlpha[yy * _mascaraAncho + xx] == 0)
                            {
                                esBorde = true;
                                break;
                            }
                        }
                    }
                    _mascaraEsBorde[i] = esBorde;
                }
            }
        }

        /// <summary>
        /// Fotogramas de firma visual cacheados para `matId` (ver docblock de
        /// la clase): se generan LA PRIMERA VEZ que hacen falta y nunca más.
        /// Longitud 1 si ritmoAnim==0 (quieto de verdad); si no,
        /// FirmaVisualFabrica.AnimFrames fotogramas con la fase desplazada.
        /// </summary>
        private Sprite[] ObtenerFirmaSprites(byte matId)
        {
            // Defensivo (mismo criterio que JournalHud.ObtenerMiniatura): matId
            // es un byte (0..255) pero el array está indexado a MaterialId.Count
            // -- no debería pasar nunca en el flujo normal (r.Mat siempre viene
            // de un material real de este universo), pero mejor no reventar el
            // índice si algún día algo lo pisa.
            if (matId >= _firmaSprites.Length) return null;

            var existente = _firmaSprites[matId];
            if (existente != null) return existente;

            var def = _sim.Universe.Get(matId);
            int frames = def.ritmoAnim > 0 ? FirmaVisualFabrica.AnimFrames : 1;
            var sprites = new Sprite[frames];
            var texturas = new Texture2D[frames];

            for (int f = 0; f < frames; f++)
            {
                var px = FirmaVisualFabrica.GenerarPixeles(_mascaraAncho, _mascaraAlto, def, f,
                    _mascaraAlpha, _mascaraEsBorde, sobreMundo: true);

                var tex = new Texture2D(_mascaraAncho, _mascaraAlto, TextureFormat.RGBA32, false, false)
                {
                    filterMode = FilterMode.Point, // mismo criterio que toda textura generada del proyecto: pixel-art nítido.
                    wrapMode = TextureWrapMode.Clamp,
                    name = "FirmaRedoma_" + def.devName + "_" + f,
                };
                tex.SetPixels32(px);
                tex.Apply(false, true); // makeNoLongerReadable=true: solo se pinta con SpriteRenderer, nunca se relee desde CPU.
                texturas[f] = tex;

                sprites[f] = Sprite.Create(tex, new Rect(0f, 0f, _mascaraAncho, _mascaraAlto),
                    new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
            }

            _firmaTexturas[matId] = texturas;
            _firmaSprites[matId] = sprites;
            return sprites;
        }

        /// <summary>
        /// (fix playtest 13) Barato A PROPÓSITO: no reconstruye NINGUNA
        /// textura aquí -- solo decide, para cada redoma con contenido, qué
        /// Sprite YA EXISTENTE del array cacheado le toca mostrar este
        /// instante (un reloj global de fotogramas a AnimFps, compartido por
        /// las 5 redomas) y reasigna SpriteRenderer.sprite SOLO si cambió el
        /// material o el índice de fotograma respecto al frame anterior. Se
        /// llama cada Update, incluso con el input bloqueado (ver la llamada
        /// en Update): es puramente visual, así que nunca conviene que se
        /// note un parón al abrir el diario o bautizar algo.
        /// </summary>
        private void ActualizarAnimacionContenidos()
        {
            if (_mascaraAlpha == null) return; // aún no se ha construido el mueble (Init no ha corrido).

            int frameGlobal = Mathf.FloorToInt(Time.time * FirmaVisualFabrica.AnimFps);
            for (int i = 0; i < NumRedomas; i++)
            {
                var r = _redomas[i];
                if (r == null || r.Cantidad <= 0 || r.Mat == MaterialId.Empty) continue;

                var sprites = ObtenerFirmaSprites(r.Mat);
                if (sprites == null || sprites.Length == 0) continue; // defensivo, ver ObtenerFirmaSprites.
                int idx = frameGlobal % sprites.Length; // longitud 1 -> siempre 0: quieto de verdad con ritmoAnim==0.
                if (r.Mat == r.MatSpriteActual && idx == r.FrameSpriteActual) continue;

                r.Contenido.sprite = sprites[idx];
                r.MatSpriteActual = r.Mat;
                r.FrameSpriteActual = idx;
            }
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

            // (fix playtest 13) La animación de las redomas es puramente visual
            // y NO consume input: se mantiene corriendo SIEMPRE (incluso con el
            // diario abierto, bautizando o durante un overlay de jornada) para
            // que nunca se note un parón al volver a poder interactuar -- a
            // diferencia de todo lo demás en este Update, que sí son atajos del
            // MUNDO y respetan las guardas de abajo (regla 12 de CLAUDE.md).
            ActualizarAnimacionContenidos();

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

    // =======================================================================
    // FIRMA VISUAL POR CÓDIGO (fix playtest 13) — compartida por StorageRack
    // (redomas, sobre el MUNDO) y FlaskHud (filas del panel + chip de
    // bloqueo, sobre UI). Réplica ADAPTADA de la técnica de
    // JournalHud.CrearMiniatura (archivo de solo lectura en este encargo:
    // ObtenerMiniatura/CrearMiniatura/Apply*Mini son privados y ese archivo
    // no se puede tocar) — mismo despacho por PatronMorfologico/
    // BordeMorfologico, mismo lenguaje de "modular brillo+saturación", pero
    // con DOS adaptaciones que JournalHud no necesitaba:
    //   1) ANIMACIÓN: cada patrón acepta una `fase` (un desplazamiento barato
    //      análogo al `drift` que ya usa SimRenderer para Vetas/Celdas con
    //      ritmoAnim>0, pero aquí NO depende de un tick de sim en vivo —
    //      son fotogramas PREGENERADOS, ver AnimFrames) para que las firmas
    //      con ritmoAnim>0 se perciban vivas sin regenerar textura en vivo.
    //   2) ESTARCIDO OPCIONAL: `maskAlpha` permite pintar sobre una silueta
    //      arbitraria (la redoma) en vez de asumir un swatch cuadrado macizo
    //      (`maskAlpha=null` = swatch cuadrado, lo que usa FlaskHud).
    //
    // DUPLICACIÓN CONOCIDA (para la próxima ronda, ver también el resumen
    // del encargo): los siete Apply*() de patrón y los helpers de hash/
    // Voronoi de aquí son, a propósito, muy parecidos a los Apply*Mini() de
    // JournalHud — JournalHud es de solo lectura en este encargo y sus
    // métodos son privados, así que no había otra forma de reutilizarlos
    // sin tocar ese archivo. Debería vivir en un ÚNICO sitio compartido
    // (p.ej. `Game/FirmaVisualFabrica.cs` propio, o si algún día hace falta
    // desde Sim/, un helper no-MonoBehaviour en Sim/ que tanto SimRenderer
    // como esta fábrica y JournalHud consuman) — no se mueve aquí porque
    // JournalHud.cs no es un archivo modificable en este encargo.
    // =======================================================================
    internal static class FirmaVisualFabrica
    {
        /// <summary>Fotogramas pregenerados por material cuando ritmoAnim&gt;0 (2-4 sugeridos por el encargo; 4 da margen para que el "salto" entre fotogramas no se note brusco).</summary>
        public const int AnimFrames = 4;

        /// <summary>Fotogramas por segundo a los que se ALTERNA (nunca se regenera) el Sprite/Texture2D mostrado — barato: solo cambia qué recurso YA EXISTENTE se referencia.</summary>
        public const int AnimFps = 6;

        /// <summary>
        /// "Tick sintético" que avanza cada fotograma pregenerado (no hay tick
        /// de sim real aquí, ver docblock de la clase): calibrado para que un
        /// material de ritmoAnim medio muestre un desplazamiento del orden de
        /// su propio período de patrón a lo largo de los AnimFrames fotogramas
        /// — ni tan poco que no se note, ni tan mucho que parpadee sin sentido.
        /// </summary>
        private const int FaseStepBase = 30;

        /// <summary>
        /// Genera los píxeles de UN fotograma de la firma visual de `def`
        /// sobre un lienzo `w`x`h`. `maskAlpha` (opcional, tamaño w*h) es un
        /// estarcido: alpha==0 dibuja transparente sin más (fuera de la
        /// silueta de la redoma); si es null se asume un swatch cuadrado
        /// macizo con el alfa propio de `def.baseColor` (uso de FlaskHud).
        /// `esBordeMask` (tamaño w*h) marca qué téxeles cuentan como "borde"
        /// para BordeMorfologico — lo calcula el llamante porque la noción de
        /// "borde" depende de la forma (silueta de botella vs. canto de
        /// swatch cuadrado). `sobreMundo` decide cómo se resuelve el borde
        /// Difuso (ver <see cref="ApplyBorde"/> y la regla 19 de CLAUDE.md).
        /// Llamada SOLO al generar un fotograma nuevo (una vez por material,
        /// nunca por frame) — el bucle interno no es hot-path.
        /// </summary>
        public static Color32[] GenerarPixeles(int w, int h, MaterialDef def, int frameIdx,
            byte[] maskAlpha, bool[] esBordeMask, bool sobreMundo)
        {
            var px = new Color32[w * h];
            int fase = def.ritmoAnim > 0 ? frameIdx * FaseStepBase : 0;
            Color32 baseColor = def.baseColor;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    byte maskA = maskAlpha != null ? maskAlpha[i] : (byte)255;
                    if (maskA == 0) { px[i] = default; continue; }

                    byte r = baseColor.r, g = baseColor.g, b = baseColor.b;
                    // Sobre la redoma (maskAlpha!=null) se pinta opaco: la
                    // translucidez de "vidrio" la aporta SpriteRenderer.color
                    // por fuera (ver StorageRack.ActualizarRedoma). Sobre el
                    // swatch cuadrado del HUD se respeta el alfa de diseño
                    // propio del material (mismo criterio que JournalHud).
                    byte a = maskAlpha != null ? (byte)255 : baseColor.a;

                    if (def.colorJitter > 0)
                    {
                        int j = (int)(Hash2D(x, y, def.semillaPatron) % (uint)(def.colorJitter * 2 + 1)) - def.colorJitter;
                        r = ClampByte(r + j);
                        g = ClampByte(g + j);
                        b = ClampByte(b + j);
                    }

                    if (def.patronFuerza > 0) ApplyPatron(x, y, w, h, def, fase, frameIdx, ref r, ref g, ref b);

                    bool esBorde = esBordeMask != null && esBordeMask[i];
                    if (esBorde && def.borde != BordeMorfologico.Neto) ApplyBorde(x, y, def, sobreMundo, ref r, ref g, ref b, ref a);

                    if (def.emision > 0)
                    {
                        int amt = def.emision * 2 / 5;
                        r = ClampByte(r + amt);
                        g = ClampByte(g + amt);
                        b = ClampByte(b + amt);
                    }

                    px[i] = new Color32(r, g, b, a);
                }
            }
            return px;
        }

        /// <summary>Despacho por familia morfológica — mismo criterio que JournalHud.ApplyPatronMini.</summary>
        private static void ApplyPatron(int x, int y, int w, int h, MaterialDef def, int fase, int frameIdx, ref byte r, ref byte g, ref byte b)
        {
            switch (def.patron)
            {
                case PatronMorfologico.Vetas: ApplyVetas(x, y, def, fase, ref r, ref g, ref b); break;
                case PatronMorfologico.Manchas: ApplyManchas(x, y, def, fase, ref r, ref g, ref b); break;
                case PatronMorfologico.Laberinto: ApplyLaberinto(x, y, def, fase, ref r, ref g, ref b); break;
                case PatronMorfologico.Celdas: ApplyCeldas(x, y, def, fase, ref r, ref g, ref b); break;
                case PatronMorfologico.Dendritas: ApplyDendritas(x, y, w, h, def, fase, ref r, ref g, ref b); break;
                case PatronMorfologico.Pulso: ApplyPulso(x, y, w, h, def, fase, ref r, ref g, ref b); break;
                case PatronMorfologico.Motas: ApplyMotas(x, y, def, frameIdx, ref r, ref g, ref b); break;
                // Liso no llega aquí (gate patronFuerza>0 en GenerarPixeles).
            }
        }

        /// <summary>Vetas: bandas senoidales deformadas. `fase` desplaza la banda -- mármol que "se asienta" muy despacio, coherente con ritmoAnim capado bajo para esta familia (Universe.Create).</summary>
        private static void ApplyVetas(int x, int y, MaterialDef def, int fase, ref byte r, ref byte g, ref byte b)
        {
            int periodo = 5 + def.patronEscala;
            int warp = (int)(Hash2D(x / 3, y / 3, def.semillaPatron) % 41) - 20;
            int tiltY = 1 + (def.semillaPatron % 3);
            int drift = (int)(((uint)fase * (uint)def.ritmoAnim) >> 10);
            double faseOnda = (x + y * tiltY + warp + drift) * (Math.PI * 2.0 / periodo);
            int onda = (int)Math.Round(Math.Sin(faseOnda) * 127.0);
            Modulate(ref r, ref g, ref b, onda * def.patronFuerza / 255);
        }

        /// <summary>Manchas: discos de concentración alrededor de puntos jitterados. `fase` desliza el MUESTREO en X -- las manchas "flotan" como una balsa, sin recalcular los puntos semilla.</summary>
        private static void ApplyManchas(int x, int y, MaterialDef def, int fase, ref byte r, ref byte g, ref byte b)
        {
            int celda = 4 + def.patronEscala;
            int driftX = (int)(((uint)fase * (uint)def.ritmoAnim) >> 9);
            int d2 = DistanciaMinimaAPunto2(x + driftX, y, celda, def.semillaPatron + 30, out _);
            int radio = Mathf.Max(1, celda / 2);
            int d = (int)Math.Sqrt(d2);
            int t01 = Mathf.Clamp(100 - d * 100 / radio, -60, 100);
            Modulate(ref r, ref g, ref b, t01 * def.patronFuerza / 200);
        }

        /// <summary>Laberinto: dos ondas perpendiculares entrelazadas. `fase` desplaza una de las dos -- serpentinas que reptan, no bandas rectas.</summary>
        private static void ApplyLaberinto(int x, int y, MaterialDef def, int fase, ref byte r, ref byte g, ref byte b)
        {
            int periodo = 5 + def.patronEscala;
            int warp = (int)(Hash2D(x / 4, y / 4, def.semillaPatron + 60) % 61) - 30;
            int drift = (int)(((uint)fase * (uint)def.ritmoAnim) >> 10);
            double fx = (x + warp * 0.2 + drift) * (Math.PI * 2.0 / periodo);
            double fy = (y - warp * 0.2) * (Math.PI * 2.0 / periodo);
            double banda = Math.Sin(fx) * Math.Cos(fy);
            int onda = (int)Math.Round(banda * 127.0);
            Modulate(ref r, ref g, ref b, onda * def.patronFuerza / 255);
        }

        /// <summary>Celdas: teselas tipo Voronoi con borde marcado. `fase` desliza el MUESTREO en X, igual criterio (y mismo divisor &gt;&gt;9) que el driftX de SimRenderer.ApplyCeldas -- todo el campo se desliza como una balsa de espuma.</summary>
        private static void ApplyCeldas(int x, int y, MaterialDef def, int fase, ref byte r, ref byte g, ref byte b)
        {
            int celda = 5 + def.patronEscala;
            int driftX = (int)(((uint)fase * (uint)def.ritmoAnim) >> 9);
            int sx = x + driftX;
            int mejorD2 = DistanciaMinimaAPunto2(sx, y, celda, def.semillaPatron + 90, out uint mejorId);
            int segundoD2 = SegundaDistanciaAPunto2(sx, y, celda, def.semillaPatron + 90);
            int diff = (int)(Math.Sqrt(segundoD2) - Math.Sqrt(mejorD2));
            int bandaBorde = Mathf.Max(2, celda / 3);

            int amt;
            if (diff < bandaBorde)
            {
                int t01 = diff * 100 / bandaBorde;
                amt = -((100 - t01) * def.patronFuerza / 100);
            }
            else
            {
                int tono = (int)(mejorId % 41) - 20;
                amt = tono * def.patronFuerza / 255;
            }
            Modulate(ref r, ref g, ref b, amt);
        }

        /// <summary>Dendritas: ramas radiales desde el centro del lienzo. `fase` ROTA el abanico de sectores -- crecimiento que gira despacio, en vez de recalcular ramas.</summary>
        private static void ApplyDendritas(int x, int y, int w, int h, MaterialDef def, int fase, ref byte r, ref byte g, ref byte b)
        {
            float cx = w * 0.5f, cy = h * 0.5f;
            float dx = x - cx, dy = y - cy;
            float radio = Mathf.Sqrt(dx * dx + dy * dy);
            if (radio < 0.5f) { Modulate(ref r, ref g, ref b, def.patronFuerza / 3); return; }

            float radioMax = Mathf.Min(w, h) * 0.5f;
            float driftDeg = (((uint)fase * (uint)def.ritmoAnim) >> 10) % 360u;
            float anguloDeg = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + driftDeg + 180f, 360f) - 180f;

            int sectores = 7 + (def.semillaPatron % 5);
            float anguloSector = 360f / sectores;
            int sector = Mathf.FloorToInt((anguloDeg + 180f) / anguloSector);
            uint h2 = Hash2D(sector, 0, def.semillaPatron + 120);
            float largoRama = radioMax * (0.35f + (h2 % 100) / 100f * 0.65f);
            float centroSectorDeg = sector * anguloSector - 180f + anguloSector * 0.5f;
            float distAngular = Mathf.Abs(Mathf.DeltaAngle(anguloDeg, centroSectorDeg));

            if (radio <= largoRama && distAngular <= 9f)
            {
                float t01 = 1f - radio / Mathf.Max(1f, largoRama);
                Modulate(ref r, ref g, ref b, (int)(t01 * def.patronFuerza));
            }
            else
            {
                Modulate(ref r, ref g, ref b, -(def.patronFuerza / 6));
            }
        }

        /// <summary>Pulso: anillos concéntricos desde el centro del lienzo. `fase` empuja la onda -- "late" de verdad entre fotogramas, en vez de solo mostrar anillos fijos.</summary>
        private static void ApplyPulso(int x, int y, int w, int h, MaterialDef def, int fase, ref byte r, ref byte g, ref byte b)
        {
            float cx = w * 0.5f, cy = h * 0.5f;
            float radio = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            int periodo = 4 + def.patronEscala;
            int drift = (int)(((uint)fase * (uint)def.ritmoAnim) >> 10);
            double faseOnda = radio * (Math.PI * 2.0 / periodo) + drift * (Math.PI * 2.0 / 256.0);
            int onda = (int)Math.Round(Math.Sin(faseOnda) * 127.0);
            Modulate(ref r, ref g, ref b, onda * def.patronFuerza / 255);
        }

        /// <summary>Motas: destellos dispersos, aditivo puro. `frameIdx` cambia la sal del hash -- las motas que se encienden CAMBIAN de fotograma a fotograma, el propio "centelleo" que pedía el reporte.</summary>
        private static void ApplyMotas(int x, int y, MaterialDef def, int frameIdx, ref byte r, ref byte g, ref byte b)
        {
            uint h = Hash2D(x, y, def.semillaPatron + 150 + frameIdx * 41);
            if ((h % 11) != 0) return;
            r = ClampByte(r + def.patronFuerza);
            g = ClampByte(g + def.patronFuerza);
            b = ClampByte(b + def.patronFuerza);
        }

        /// <summary>
        /// Borde morfológico sobre los téxeles marcados por `esBordeMask`
        /// (ver GenerarPixeles). Halo/Escarcha replican JournalHud tal cual;
        /// Difuso BIFURCA por `sobreMundo` -- regla 19 de CLAUDE.md: nunca
        /// bajar alfa sobre el MUNDO (mosaico duro contra el fondo del
        /// taller, ver el comentario largo de SimRenderer.ComputeCellColor),
        /// así que sobre una redoma se oscurece hacia
        /// SimRenderer.BackgroundColor en vez de tocar el canal alfa; sobre
        /// el panel opaco del HUD (sobreMundo=false) sí se puede bajar alfa,
        /// exactamente como ya hace JournalHud.ApplyBordeMini.
        /// </summary>
        private static void ApplyBorde(int x, int y, MaterialDef def, bool sobreMundo, ref byte r, ref byte g, ref byte b, ref byte a)
        {
            switch (def.borde)
            {
                case BordeMorfologico.Halo:
                    Modulate(ref r, ref g, ref b, 40);
                    break;

                case BordeMorfologico.Escarcha:
                    if ((Hash2D(x, y, def.semillaPatron + 211) % 3) == 0)
                    {
                        r = ClampByte(r + 80);
                        g = ClampByte(g + 80);
                        b = ClampByte(b + 80);
                    }
                    break;

                case BordeMorfologico.Difuso:
                    if ((Hash2D(x, y, def.semillaPatron + 217) % 2) == 0)
                    {
                        if (sobreMundo)
                        {
                            r = LerpByte(r, BgColor32.r, 0.55f);
                            g = LerpByte(g, BgColor32.g, 0.55f);
                            b = LerpByte(b, BgColor32.b, 0.55f);
                        }
                        else
                        {
                            a = (byte)(a * 55 / 100);
                        }
                    }
                    break;
            }
        }

        /// <summary>Mismo color que SimRenderer.BackgroundColor (fuente única de verdad, no un hex duplicado a mano), precalculado una vez a Color32 para que el borde Difuso no reconvierta un Color de punto flotante por téxel.</summary>
        private static readonly Color32 BgColor32 = (Color32)SimRenderer.BackgroundColor;

        /// <summary>Mismo lenguaje que SimRenderer.ModulatePattern/JournalHud.ModulateMini: desplaza brillo con un empujón de saturación "gratis" al aclarar, sin tocarla al oscurecer.</summary>
        private static void Modulate(ref byte r, ref byte g, ref byte b, int signedAmt)
        {
            int mean = (r + g + b) / 3;
            int sat = signedAmt > 0 ? signedAmt / 3 : 0;
            r = ClampByte(r + signedAmt + (r - mean) * sat / 128);
            g = ClampByte(g + signedAmt + (g - mean) * sat / 128);
            b = ClampByte(b + signedAmt + (b - mean) * sat / 128);
        }

        /// <summary>Distancia al cuadrado al punto-semilla más cercano de una rejilla jitterada (feature point de Voronoi barato). Usada por Manchas y Celdas.</summary>
        private static int DistanciaMinimaAPunto2(int x, int y, int celda, int semilla, out uint idGanador)
        {
            int gx = x / celda, gy = y / celda;
            int mejorD2 = int.MaxValue;
            uint mejorId = 0;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int cx = gx + ox, cy = gy + oy;
                    uint h = Hash2D(cx, cy, semilla);
                    int px = cx * celda + (int)(h % (uint)celda);
                    int py = cy * celda + (int)((h >> 8) % (uint)celda);
                    int dx = x - px, dy = y - py;
                    int d2 = dx * dx + dy * dy;
                    if (d2 < mejorD2) { mejorD2 = d2; mejorId = h; }
                }
            }
            idGanador = mejorId;
            return mejorD2;
        }

        /// <summary>Distancia al cuadrado al SEGUNDO punto-semilla más cercano (para marcar la costura entre teselas de Celdas).</summary>
        private static int SegundaDistanciaAPunto2(int x, int y, int celda, int semilla)
        {
            int gx = x / celda, gy = y / celda;
            int mejorD2 = int.MaxValue, segundoD2 = int.MaxValue;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int cx = gx + ox, cy = gy + oy;
                    uint h = Hash2D(cx, cy, semilla);
                    int px = cx * celda + (int)(h % (uint)celda);
                    int py = cy * celda + (int)((h >> 8) % (uint)celda);
                    int dx = x - px, dy = y - py;
                    int d2 = dx * dx + dy * dy;
                    if (d2 < mejorD2) { segundoD2 = mejorD2; mejorD2 = d2; }
                    else if (d2 < segundoD2) segundoD2 = d2;
                }
            }
            return segundoD2;
        }

        private static byte ClampByte(int v) => (byte)(v < 0 ? 0 : (v > 255 ? 255 : v));

        /// <summary>Interpola un canal de byte hacia `to` una fracción `t` -- usado por el borde Difuso sobre el mundo (oscurece hacia BackgroundColor, regla 19).</summary>
        private static byte LerpByte(byte from, byte to, float t) => (byte)Mathf.RoundToInt(from + (to - from) * t);

        /// <summary>Hash entero estable de 2 coordenadas + sal, réplica local de SimRenderer.Hash2D/JournalHud.MiniHash2D (ambos privados en archivos de solo lectura aquí) para no depender de ellos.</summary>
        private static uint Hash2D(int x, int y, int salt)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)salt * 2654435761u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return h;
            }
        }
    }
}
