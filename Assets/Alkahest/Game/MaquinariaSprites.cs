using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// [ChaosAlchemy · reingeniería del espacio] Fábrica de sprites generados
    /// por código para los APARATOS del taller (placa ígnea, piedra gélida,
    /// grifos, redomas de la estantería).
    ///
    /// POR QUÉ EXISTE (playtest 4): todas las máquinas eran la misma cosa —un
    /// Sprite de 1x1 píxel blanco estirado y tintado— así que ninguna "parecía
    /// nada". Aquí cada aparato tiene su propia silueta dibujada píxel a píxel:
    /// chasis remachados con resistencias naranjas, bloques de escarcha con
    /// cristales azules, caños con boquilla, y tubos de vidrio con tapón.
    ///
    /// REGLAS QUE RESPETA (ver CLAUDE.md):
    ///  · Cero assets y cero Shader.Find: todo son Texture2D generadas +
    ///    SpriteRenderer (el shader de sprites SÍ sobrevive a la build; un
    ///    Shader.Find("URP/Unlit") no — ese fue el bug del playtest 2).
    ///  · Los sprites se crean con pixelsPerUnit = 1, así que un sprite de
    ///    (w x h) píxeles mide (w x h) unidades de mundo: <see cref="CrearCapa"/>
    ///    lo escala luego al hueco exacto que le corresponde. Eso permite
    ///    dibujar la textura en la resolución que convenga al DETALLE (remaches,
    ///    facetas) sin pelearse con la escala del mundo.
    ///  · Las texturas se generan UNA vez por tamaño distinto y se cachean:
    ///    las dos cubas comparten el mismo chasis.
    /// </summary>
    public static class MaquinariaSprites
    {
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> _cache =
            new System.Collections.Generic.Dictionary<string, Sprite>(16);

        // =================================================================
        // [Playtest 26, CONTRATO_LEGIBILIDAD.md §1.5] EL AFFORDANCE GLOW —
        // IMPLEMENTACIÓN CENTRAL ÚNICA
        // =================================================================
        /// <summary>
        /// "La boca te contesta": cuando el jugador está a ≤10 celdas con el
        /// frasco cargado de un material M, la boca de una máquina PULSA
        /// suave (halo, ~1 Hz) SOLO si M le sirve a esa boca. Es la respuesta
        /// literal a la duda de Cesar en el playtest 25 ("¿meto limo en
        /// todas?"): el taller señala, no explica con texto.
        ///
        /// UNA instancia por BOCA (embudo, brasero, lecho, ranura, plinto);
        /// cada máquina posee tantas instancias como bocas tenga (el Crisol,
        /// dos: embudo + brasero) y llama a <see cref="Sondear"/> UNA vez por
        /// Update, con su propio delegado de "sirve" CACHEADO (nunca una
        /// lambda nueva por llamada -- ver el doc de <see cref="Sondear"/>).
        ///
        /// COSTE (contrato): el sondeo de proximidad+material corre cada
        /// <see cref="ProbeIntervalSeconds"/> (~0.25s, acumulador propio,
        /// JAMÁS por frame); el resultado se cachea en <see cref="Activo"/>.
        /// El pulso visual (<see cref="Alfa"/>) SÍ se recalcula cada frame,
        /// pero es solo un `Mathf.Sin(Time.time...)` -- cero allocs, cero
        /// asignaciones de string/color nuevas (el llamante decide el color
        /// final, este helper solo entrega el alfa 0..1).
        /// </summary>
        public sealed class AffordanceGlow
        {
            public const float ProbeIntervalSeconds = 0.25f;
            private const float RangoCeldas = 10f;
            private const float PulsoHz = 1f;

            private float _timer;
            private bool _activo;

            /// <summary>¿La última pasada del sondeo dice que esta boca sirve para el material que lleva el jugador ahora mismo?</summary>
            public bool Activo => _activo;

            /// <summary>Alfa del pulso 0..1 para este frame (seno sobre Time.time) -- 0 si <see cref="Activo"/> es false. Recalculado cada frame pero sin allocs: pura aritmética.</summary>
            public float Alfa => _activo ? (0.5f + 0.5f * Mathf.Sin(Time.time * PulsoHz * Mathf.PI * 2f)) : 0f;

            /// <summary>
            /// Avanza el acumulador y, si toca sondear (~0.25s), recalcula
            /// <see cref="Activo"/>: jugador a ≤10 celdas de `bocaMundo` Y el
            /// frasco lleva un material no vacío que `sirve` acepta.
            /// Llamar UNA vez por Update de la máquina con `Time.deltaTime`
            /// real. `sirve` debe ser un delegado YA CREADO (campo cacheado
            /// en la máquina, asignado una vez en Init -- un método de
            /// instancia convertido a `Func&lt;byte,bool&gt;` asigna una sola
            /// vez, no en cada llamada) -- nunca una lambda literal aquí, o
            /// el sondeo generaría basura cada 0.25s.
            /// </summary>
            public void Sondear(float deltaTime, Vector3 bocaMundo, Transform jugador, Flask frasco, System.Func<byte, bool> sirve)
            {
                _timer += deltaTime;
                if (_timer < ProbeIntervalSeconds) return;
                _timer -= ProbeIntervalSeconds;

                if (jugador == null || frasco == null || sirve == null) { _activo = false; return; }

                float celda = SimRenderer.CellWorldSize;
                float distCeldas = Vector3.Distance(jugador.position, bocaMundo) / celda;
                if (distCeldas > RangoCeldas) { _activo = false; return; }

                byte mat = frasco.MaterialDominante();
                if (mat == MaterialId.Empty) { _activo = false; return; }

                _activo = sirve(mat);
            }
        }

        // (fix playtest 6: baja resolución) Una celda de sim mide 0.1 unidades de
        // mundo; con la cámara acercada eso son ~7-8 px de pantalla en 1080p. Las
        // texturas de aquí se generaban a ~2 téxeles/celda (chasis, bloques) o menos
        // (caño, redomas): al escalarlas al hueco de mundo exacto (ver CrearCapa), un
        // solo téxel ocupaba varios píxeles de pantalla y los bordes de las máquinas
        // se veían "a bloques", igual de chunky que la piedra sin sillería. Escala=3
        // multiplica el lienzo de cada textura (y cada offset/periodo "de diseño"
        // calibrado a mano en la resolución original, vía el helper S) para llegar a
        // >=6 téxeles/celda en ambos ejes SIN tocar el tamaño de mundo de ningún
        // sprite (CrearCapa sigue escalando al ancho/alto que pasa el llamante) ni
        // las firmas públicas. El diseño (proporciones, remaches, cantos con luz/
        // sombra) es el mismo; solo se dibuja con más téxeles.
        private const int Escala = 3;
        private static int S(int v) => v * Escala;

        /// <summary>
        /// Instancia un sprite generado como hijo de `padre`, con orden de
        /// dibujo `orden`, escalado para ocupar exactamente `anchoMundo` x
        /// `altoMundo` unidades (los sprites se crean a 1 píxel = 1 unidad).
        /// </summary>
        public static SpriteRenderer CrearCapa(Transform padre, string nombre, Sprite sprite, int orden,
            float anchoMundo, float altoMundo)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = orden;
            go.transform.localScale = new Vector3(
                anchoMundo / sprite.rect.width,
                altoMundo / sprite.rect.height, 1f);
            return sr;
        }

        // =================================================================
        // PLACA ÍGNEA
        // =================================================================

        /// <summary>Chasis de la placa: metal oscuro remachado con una ventana recesada por la que asoman las resistencias.</summary>
        public static Sprite ChasisPlaca(int spanCeldas)
        {
            string clave = "chasis" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            // (fix playtest 6: baja resolución) espacio de diseño original: 2 téxeles
            // por celda de ancho, 14 fijos de alto (~4.7/celda con WallThickness=3
            // filas). Escalado por Escala=3 -> ~6-8 téxeles/celda mínimo.
            int w = Mathf.Clamp(spanCeldas * 2 * Escala, S(16), S(512));
            int h = S(14);
            var px = new Color32[w * h];

            Color32 metalAlto = new Color32(0x5C, 0x52, 0x54, 255);
            Color32 metalBajo = new Color32(0x2B, 0x24, 0x28, 255);
            Color32 ventana = new Color32(0x18, 0x12, 0x14, 255);
            Color32 remache = new Color32(0x9A, 0x86, 0x6A, 255);
            Color32 luzSuperior = new Color32(0x77, 0x6B, 0x6C, 255);

            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                Color32 fila = Color32.Lerp(metalBajo, metalAlto, t * t);
                for (int x = 0; x < w; x++)
                {
                    Color32 c = fila;

                    // Ventana recesada central (filas 4..9 en el diseño original): el
                    // hueco donde viven las resistencias. Sin ella el naranja flotaría
                    // sobre el metal.
                    if (y >= S(4) && y < S(10) && x >= S(3) && x < w - S(3)) c = ventana;

                    // Filete de luz en el canto superior (la cuba se apoya aquí).
                    if (y >= h - S(2)) c = luzSuperior;
                    // Sombra de contacto con el suelo.
                    if (y < S(2)) c = new Color32(0x14, 0x0F, 0x12, 255);

                    px[y * w + x] = c;
                }
            }

            // Remaches: dos hileras, cada 14 px (escalado), en los montantes laterales.
            for (int x = S(4); x < w - S(3); x += S(14))
            {
                MarcarRemache(px, w, h, x, S(2), remache);
                MarcarRemache(px, w, h, x, h - S(4), remache);
            }
            // Montantes de los extremos (patas del chasis).
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < S(3); x++)
                {
                    px[y * w + x] = metalAlto;
                    px[y * w + (w - 1 - x)] = metalAlto;
                }
            }

            s = Crear(px, w, h, "ChaosAlchemyChasisPlaca");
            _cache[clave] = s;
            return s;
        }

        /// <summary>Resistencias de la placa: serpentín blanco (se tinta según intensidad) dentro de la ventana del chasis.</summary>
        public static Sprite ResistenciasPlaca(int spanCeldas)
        {
            string clave = "resis" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            // (fix playtest 6: baja resolución) mismo Escala que ChasisPlaca -- ambas
            // capas se apilan y tienen que compartir densidad de téxeles.
            int w = Mathf.Clamp(spanCeldas * 2 * Escala, S(16), S(512));
            int h = S(14);
            var px = new Color32[w * h]; // transparente por defecto

            // Serpentín: onda triangular de periodo 10 px (escalado) entre las filas
            // 5 y 8 del diseño original, con grosor 2 px (escalado). Se lee como un
            // hilo de nicromo de verdad.
            int periodo = S(10);
            for (int x = S(3); x < w - S(3); x++)
            {
                int fase = x % periodo;
                int mitad = periodo / 2;
                int alto = fase < mitad ? fase : periodo - 1 - fase; // 0..mitad
                int y0 = S(5) + Mathf.RoundToInt(alto * 0.75f);      // ~S(5)..S(8)
                for (int t = 0; t < S(2); t++)
                {
                    int y = y0 + t;
                    if (y < 0 || y >= h) continue;
                    px[y * w + x] = new Color32(255, 255, 255, 255);
                }
            }

            // Bornes: dos tacos macizos en los extremos del serpentín.
            for (int y = S(4); y < S(10); y++)
            {
                for (int x = S(3); x < S(6); x++)
                {
                    px[y * w + x] = new Color32(255, 255, 255, 200);
                    px[y * w + (w - 1 - x)] = new Color32(255, 255, 255, 200);
                }
            }

            s = Crear(px, w, h, "ChaosAlchemyResistenciasPlaca");
            _cache[clave] = s;
            return s;
        }

        // =================================================================
        // PIEDRA GÉLIDA
        // =================================================================

        /// <summary>Bloque de la piedra gélida: roca pálida escarchada con vetas azules.</summary>
        public static Sprite BloqueGelido(int spanCeldas)
        {
            string clave = "gelido" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            // (fix playtest 6: baja resolución) mismo Escala que ChasisPlaca.
            int w = Mathf.Clamp(spanCeldas * 2 * Escala, S(16), S(512));
            int h = S(14);
            var px = new Color32[w * h];

            Color32 rocaAlta = new Color32(0x5E, 0x66, 0x74, 255);
            Color32 rocaBaja = new Color32(0x28, 0x2E, 0x38, 255);
            Color32 escarcha = new Color32(0x86, 0x9A, 0xAE, 255);
            int juntaAncho = Mathf.Max(1, Escala / 2);

            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                Color32 fila = Color32.Lerp(rocaBaja, rocaAlta, t * t);
                for (int x = 0; x < w; x++)
                {
                    Color32 c = fila;
                    // Grano de piedra tallada (bloques de ~9 px escalados con junta oscura).
                    if ((x % S(9)) < juntaAncho) c = Color32.Lerp(c, rocaBaja, 0.6f);
                    if (y >= h - S(2)) c = escarcha;          // capa de escarcha superior
                    if (y < S(2)) c = new Color32(0x12, 0x15, 0x1B, 255);
                    px[y * w + x] = c;
                }
            }

            s = Crear(px, w, h, "ChaosAlchemyBloqueGelido");
            _cache[clave] = s;
            return s;
        }

        /// <summary>Cristales de la piedra gélida: agujas blancas (se tintan de cian al activarse) brotando del bloque.</summary>
        public static Sprite CristalesGelidos(int spanCeldas)
        {
            string clave = "cristales" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            // (fix playtest 6: baja resolución) mismo Escala que ChasisPlaca.
            int w = Mathf.Clamp(spanCeldas * 2 * Escala, S(16), S(512));
            int h = S(14);
            var px = new Color32[w * h];

            // Agujas de altura alterna cada 8 px (escalado): la silueta dentada es lo
            // que hace que se lea "cristal" y no "barra pintada de azul".
            int paso = S(8);
            int baseY = S(4);
            for (int cx = S(5); cx < w - S(4); cx += paso)
            {
                int idxAguja = cx / paso;
                int altura = S((idxAguja % 3 == 0) ? 9 : (idxAguja % 3 == 1 ? 6 : 7));
                for (int y = 0; y < altura; y++)
                {
                    int semi = Mathf.Max(0, (altura - y) / 3);
                    for (int dx = -semi; dx <= semi; dx++)
                    {
                        int x = cx + dx;
                        if (x < 0 || x >= w) continue;
                        int yy = baseY + y;
                        if (yy >= h) continue;
                        byte a = (byte)(dx == -semi ? 255 : 205); // canto izquierdo más brillante
                        px[yy * w + x] = new Color32(255, 255, 255, a);
                    }
                }
            }

            // Vena horizontal que une las agujas por su base (fila baseY, alpha150,
            // y una banda justo debajo, alpha90 -- unifica visualmente las agujas).
            for (int x = S(3); x < w - S(3); x++)
            {
                for (int t = 0; t < Escala; t++)
                {
                    int yA = baseY + t;
                    if (yA < h) px[yA * w + x] = new Color32(255, 255, 255, 150);
                    int yB = baseY - Escala + t;
                    if (yB >= 0) px[yB * w + x] = new Color32(255, 255, 255, 90);
                }
            }

            s = Crear(px, w, h, "ChaosAlchemyCristalesGelidos");
            _cache[clave] = s;
            return s;
        }

        // =================================================================
        // GRIFO
        // =================================================================

        /// <summary>Caño de pared: brida atornillada al pilar + tubo + boquilla acampanada mirando a la derecha.</summary>
        public static Sprite CanoGrifo()
        {
            const string clave = "cano";
            if (_cache.TryGetValue(clave, out var s)) return s;

            // (fix playtest 6: baja resolución) el caño NO se parametriza por
            // spanCeldas -- su tamaño de mundo lo fija Dispenser.cs a 8x5 celdas
            // fijas, así que 34x20 px daba ~4.25/4 téxeles por celda. Escalado por
            // Escala llega a ~12/cell, muy por encima del mínimo de 6.
            int w = S(34), h = S(20);
            var px = new Color32[w * h];

            // Latón CLARO a propósito (playtest 5): el caño vive pegado a un
            // pilar de piedra oscura en el borde izquierdo de la pantalla; con la
            // paleta anterior (0x8A6C36) se fundía con el fondo y el jugador no
            // encontraba los grifos.
            Color32 latonAlto = new Color32(0xF2, 0xD3, 0x8C, 255);
            Color32 laton = new Color32(0xBD, 0x93, 0x47, 255);
            Color32 latonBajo = new Color32(0x70, 0x56, 0x2A, 255);

            // Brida vertical pegada al muro (izquierda).
            for (int y = S(2); y < h - S(2); y++)
                for (int x = 0; x < S(6); x++)
                    px[y * w + x] = (x < S(2)) ? latonBajo : (y > h / 2 ? latonAlto : laton);

            // Tubo horizontal: filete de luz en el canto superior (fila 8 del
            // diseño original), sombra en el canto inferior (fila 12).
            for (int y = S(8); y < S(13); y++)
                for (int x = S(5); x < w - S(8); x++)
                    px[y * w + x] = (y < S(9)) ? latonAlto : ((y >= S(12)) ? latonBajo : laton);

            // Codo + boquilla acampanada que mira hacia ABAJO (por ahí cae el caudal).
            for (int y = S(3); y < S(13); y++)
                for (int x = w - S(10); x < w - S(4); x++)
                    px[y * w + x] = (x < w - S(10) + Escala) ? latonAlto : laton;
            for (int oldY = 0; oldY <= 4; oldY++)
            {
                int ancho = S(3 + (4 - oldY) / 2);
                for (int sub = 0; sub < Escala; sub++)
                {
                    int y = S(oldY) + sub;
                    for (int dx = -ancho; dx <= ancho; dx++)
                    {
                        int x = w - S(7) + dx;
                        if (x < 0 || x >= w) continue;
                        px[y * w + x] = (dx <= -ancho + Escala) ? latonAlto : ((dx >= ancho - Escala) ? latonBajo : laton);
                    }
                }
            }

            // Volante de apertura sobre el tubo: aro de luz alrededor del disco oscuro.
            for (int y = S(13); y < S(18); y++)
                for (int x = S(9); x < S(16); x++)
                {
                    bool borde = (y < S(14) || y >= S(17) || x < S(10) || x >= S(15));
                    px[y * w + x] = borde ? latonAlto : latonBajo;
                }

            s = Crear(px, w, h, "ChaosAlchemyCanoGrifo");
            _cache[clave] = s;
            return s;
        }

        // =================================================================
        // REDOMAS (Game/StorageRack.cs)
        // =================================================================

        /// <summary>Vidrio de la redoma: paredes translúcidas, cuello y reflejo. El contenido se dibuja DETRÁS (orden menor).</summary>
        public static Sprite VidrioRedoma()
        {
            const string clave = "vidrio";
            if (_cache.TryGetValue(clave, out var s)) return s;

            // (fix playtest 6: baja resolución) sin spanCeldas: se escala el lienzo
            // entero por Escala y se re-deriva cada fila a partir de su fila
            // "de diseño" original (oldY = y/Escala) -- un nearest-neighbor fiel a
            // mano que preserva exactamente el contorno del cuerpo panzudo/cuello.
            int w = S(22), h = S(54);
            var px = new Color32[w * h];

            Color32 vidrio = new Color32(0xCB, 0xE4, 0xEE, 90);
            Color32 cantoVidrio = new Color32(0xE8, 0xF6, 0xFF, 165);
            Color32 reflejo = new Color32(255, 255, 255, 130);

            for (int y = 0; y < h; y++)
            {
                int oldY = y / Escala;
                // Cuerpo panzudo abajo (oldY<38), cuello estrecho arriba.
                int semi = oldY < 38 ? 10 : 5;
                if (oldY < 4) semi = 8 - (3 - oldY);            // base redondeada
                if (oldY >= 38 && oldY < 42) semi = 10 - (oldY - 38); // hombro cónico
                semi = S(semi);

                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    bool pared = Mathf.Abs(dx) >= semi - Escala || y == 0;
                    px[y * w + x] = pared ? cantoVidrio : vidrio;
                }
            }

            // Reflejo vertical en el tercio izquierdo del cuerpo.
            for (int y = S(6); y < S(36); y++)
            {
                int oldY = y / Escala;
                int x = w / 2 - S(6);
                px[y * w + x] = reflejo;
                if (oldY % 7 != 0 && x + Escala < w) px[y * w + x + Escala] = new Color32(255, 255, 255, 60);
            }

            s = Crear(px, w, h, "ChaosAlchemyVidrioRedoma");
            _cache[clave] = s;
            return s;
        }

        /// <summary>Máscara del CONTENIDO de la redoma: blanco opaco con la silueta interior del vidrio; se tinta con el material y se recorta por altura desde abajo.</summary>
        public static Sprite ContenidoRedoma()
        {
            const string clave = "contenido";
            if (_cache.TryGetValue(clave, out var s)) return s;

            // (fix playtest 6: baja resolución) mismo w/h y misma técnica de
            // oldY = y/Escala que VidrioRedoma -- el margen de 1 unidad "de diseño"
            // respecto al vidrio (9/4/7 vs. 10/5/8) se conserva exacto para que el
            // contenido siga encajando justo dentro del cristal.
            int w = S(22), h = S(54);
            var px = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                int oldY = y / Escala;
                int semi = oldY < 38 ? 9 : 4;
                if (oldY < 4) semi = 7 - (3 - oldY);
                if (oldY >= 38 && oldY < 42) semi = 9 - (oldY - 38);
                semi = S(semi);

                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    px[y * w + x] = new Color32(255, 255, 255, 255);
                }
            }

            s = Crear(px, w, h, "ChaosAlchemyContenidoRedoma");
            _cache[clave] = s;
            return s;
        }

        /// <summary>Tapón de corcho de la redoma.</summary>
        public static Sprite TaponRedoma()
        {
            const string clave = "tapon";
            if (_cache.TryGetValue(clave, out var s)) return s;

            // (fix playtest 6: baja resolución) misma técnica oldY = y/Escala.
            int w = S(14), h = S(10);
            var px = new Color32[w * h];

            Color32 corcho = new Color32(0xA8, 0x7A, 0x48, 255);
            Color32 corchoAlto = new Color32(0xC9, 0x9A, 0x62, 255);
            Color32 corchoBajo = new Color32(0x6E, 0x4C, 0x2A, 255);

            for (int y = 0; y < h; y++)
            {
                int oldY = y / Escala;
                // Tronco de cono invertido: más ancho arriba.
                int semi = S(4 + oldY / 4);
                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    px[y * w + x] = (y >= h - S(2)) ? corchoAlto : (dx >= semi - Escala ? corchoBajo : corcho);
                }
            }

            s = Crear(px, w, h, "ChaosAlchemyTaponRedoma");
            _cache[clave] = s;
            return s;
        }

        /// <summary>Listón de madera del estante con anillos de latón: el mueble donde se apoyan las redomas.</summary>
        public static Sprite ListonEstante(int anchoPx)
        {
            string clave = "liston" + anchoPx;
            if (_cache.TryGetValue(clave, out var s)) return s;

            // (fix playtest 6: baja resolución) el ANCHO en téxeles lo fija el
            // llamante (StorageRack.cs, no editable) como texelesMundo*20 -- no
            // podemos subir su densidad desde aquí sin tocar esa firma/llamada. La
            // ALTURA sí es enteramente interna: h=10 para 0.24 unidades de mundo
            // (~2.4 celdas) daba ~4.2 téxeles/celda; se escala por Escala.
            int w = Mathf.Clamp(anchoPx, 16, 512);
            int h = S(10);
            var px = new Color32[w * h];

            Color32 maderaAlta = new Color32(0x5A, 0x3F, 0x2C, 255);
            Color32 madera = new Color32(0x3E, 0x2B, 0x1E, 255);
            Color32 maderaBaja = new Color32(0x24, 0x18, 0x12, 255);

            for (int y = 0; y < h; y++)
            {
                Color32 fila = y >= h - S(2) ? maderaAlta : (y < S(2) ? maderaBaja : madera);
                for (int x = 0; x < w; x++)
                {
                    // Veta: rayas largas y suaves.
                    bool veta = ((x * 7 + y * 31) % 23) == 0;
                    px[y * w + x] = veta ? Color32.Lerp(fila, maderaAlta, 0.5f) : fila;
                }
            }

            s = Crear(px, w, h, "ChaosAlchemyListonEstante");
            _cache[clave] = s;
            return s;
        }

        // =================================================================
        // [Playtest 26, CONTRATO_LEGIBILIDAD.md §1] LA GRAMÁTICA VISUAL —
        // familias de sprites compartidas por las 5 estaciones nuevas
        // (Crisol/Prensa/BancoChispa/Columna/Ensayo). Mismo criterio de
        // Escala/S(...) y cacheo que el resto de la fábrica.
        // =================================================================

        /// <summary>
        /// GRAMÁTICA §1.1: EMBUDO = ENTRADA DE MATERIA. Boca ancha arriba,
        /// se cierra hacia un pico estrecho abajo (perfil t*t: se abre rápido
        /// cerca de la boca, como un embudo de verdad, no un cono recto) --
        /// latón, MISMO sprite-familia en TODA máquina que reciba materia del
        /// frasco (Crisol/Prensa/BancoChispa/Ensayo): se aprende una vez.
        /// </summary>
        public static Sprite Embudo(int spanCeldas)
        {
            string clave = "embudo" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Mathf.Clamp(spanCeldas * 2 * Escala, S(16), S(512));
            int h = S(14);
            var px = new Color32[w * h];

            Color32 latonAlto = new Color32(0xF2, 0xD3, 0x8C, 255);
            Color32 laton = new Color32(0xBD, 0x93, 0x47, 255);
            Color32 latonBajo = new Color32(0x70, 0x56, 0x2A, 255);

            int spoutSemi = Mathf.Max(S(1), w / 10);
            int mouthSemi = w / 2 - S(1);

            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1); // 0 abajo (pico), 1 arriba (boca).
                int semi = Mathf.RoundToInt(Mathf.Lerp(spoutSemi, mouthSemi, t * t));
                bool remateBoca = y >= h - S(2); // remate de la boca, ligeramente más ancho que el cuerpo del embudo.
                if (remateBoca) semi = mouthSemi + S(1);

                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    bool bordeIzq = dx <= -semi + Escala;
                    bool bordeDer = dx >= semi - Escala;
                    Color32 c = remateBoca ? latonAlto : (bordeIzq ? latonAlto : (bordeDer ? latonBajo : laton));
                    px[y * w + x] = c;
                }
            }

            s = Crear(px, w, h, "ChaosAlchemyEmbudo");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// GRAMÁTICA §1.2: BRASERO = ENTRADA DE COMBUSTIBLE, la ÚNICA otra
        /// boca que existe -- otra forma (cesto ovalado, no un embudo),
        /// otra altura, otro color (hierro oscuro, no latón): jamás se
        /// confunde. Silueta de bowl con barrotes verticales (rejilla del
        /// cesto) que dejan asomar el rescoldo de dentro (capa
        /// <see cref="ResistenciasPlaca"/>, reutilizada por el llamante,
        /// tintada de ámbar/naranja).
        /// </summary>
        public static Sprite Brasero(int spanCeldas)
        {
            string clave = "brasero" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Mathf.Clamp(spanCeldas * 2 * Escala, S(16), S(512));
            int h = S(14);
            var px = new Color32[w * h];

            Color32 hierroAlto = new Color32(0x58, 0x50, 0x4C, 255);
            Color32 hierro = new Color32(0x2A, 0x26, 0x24, 255);
            Color32 hierroBajo = new Color32(0x14, 0x12, 0x12, 255);

            int maxSemi = w / 2 - S(1);
            int barrotePeriodo = Mathf.Max(1, S(3));

            for (int y = S(1); y < h - S(1); y++)
            {
                // Perfil de bowl: máximo ancho a media altura (seno), NUNCA
                // rectangular -- la silueta por sí sola ya lee "cesto", no
                // "chasis" (grammar §1.2: "otra forma que un embudo").
                float t = (y - S(1)) / (float)(h - S(2) - 1);
                float bulge = Mathf.Sin(t * Mathf.PI);
                int semi = Mathf.RoundToInt(Mathf.Lerp(maxSemi * 0.35f, maxSemi, bulge));

                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    bool barrote = (x % barrotePeriodo) == 0; // rejilla vertical del cesto.
                    bool bordeIzq = dx <= -semi + Escala;
                    Color32 c = barrote ? hierroBajo : (bordeIzq ? hierroAlto : hierro);
                    px[y * w + x] = c;
                }
            }

            // Patas cortas: dos tacos macizos bajo el cesto.
            int pataSemi = Mathf.Max(S(1), maxSemi / 3);
            for (int y = 0; y < S(1); y++)
            {
                for (int dx = -pataSemi; dx <= pataSemi; dx++)
                {
                    int xIzq = w / 2 - maxSemi / 2 + dx;
                    int xDer = w / 2 + maxSemi / 2 + dx;
                    if (xIzq >= 0 && xIzq < w) px[y * w + xIzq] = hierroBajo;
                    if (xDer >= 0 && xDer < w) px[y * w + xDer] = hierroBajo;
                }
            }

            s = Crear(px, w, h, "ChaosAlchemyBrasero");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// GRAMÁTICA §1.3: CUBETA ENMARCADA. Marco de latón de 2px con
        /// remate en las cuatro esquinas -- overlay transparente por dentro
        /// (se dibuja SOBRE el chasis del recipiente, escalado un poco más
        /// grande, mismo patrón que el "_resalte" de foco ya existente en
        /// Crisol/Prensa/BancoChispa). Lee "contenedor", no "agujero en el
        /// suelo": lo que el playtest 25 pedía explícitamente para todo
        /// recipiente de trabajo.
        /// </summary>
        public static Sprite MarcoContenedor(int spanCeldas)
        {
            string clave = "marco" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Mathf.Clamp(spanCeldas * 2 * Escala, S(16), S(512));
            int h = S(14);
            var px = new Color32[w * h]; // transparente por defecto.

            Color32 laton = new Color32(0xE8, 0xC4, 0x7A, 255);
            Color32 latonBrillo = new Color32(0xFF, 0xEE, 0xC0, 255);
            int grosor = S(2);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool borde = x < grosor || x >= w - grosor || y < grosor || y >= h - grosor;
                    if (borde) px[y * w + x] = laton;
                }
            }

            int remate = S(3);
            MarcarRemateCuadrado(px, w, h, 0, 0, remate, latonBrillo);
            MarcarRemateCuadrado(px, w, h, w - remate, 0, remate, latonBrillo);
            MarcarRemateCuadrado(px, w, h, 0, h - remate, remate, latonBrillo);
            MarcarRemateCuadrado(px, w, h, w - remate, h - remate, remate, latonBrillo);

            s = Crear(px, w, h, "ChaosAlchemyMarcoContenedor");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// GRAMÁTICA §1.4 (Crisol): CHIMENEA. Tubo de hierro montado sobre el
        /// chasis de la cubeta, con remate acampanado arriba -- el CUERPO
        /// estático; las bocanadas de humo son <see cref="Humo"/>, animadas
        /// por Crisol.cs SOLO mientras quema combustible.
        /// </summary>
        public static Sprite Chimenea(int spanCeldas)
        {
            string clave = "chimenea" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Mathf.Clamp(spanCeldas * Escala, S(10), S(200));
            int h = S(16);
            var px = new Color32[w * h];

            Color32 hierroAlto = new Color32(0x52, 0x4A, 0x46, 255);
            Color32 hierro = new Color32(0x2E, 0x28, 0x26, 255);
            Color32 hierroBajo = new Color32(0x16, 0x12, 0x12, 255);

            int semiTubo = Mathf.Max(S(1), w / 4);
            for (int y = 0; y < h; y++)
            {
                bool remate = y >= h - S(3); // boca superior, ligeramente más ancha.
                int semi = remate ? semiTubo + S(1) : semiTubo;
                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    bool borde = Mathf.Abs(dx) >= semi - Mathf.Max(1, Escala / 2);
                    px[y * w + x] = remate ? hierroAlto : (borde ? hierroBajo : hierro);
                }
            }

            s = Crear(px, w, h, "ChaosAlchemyChimenea");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// GRAMÁTICA §1.4 (Crisol): una voluta de humo -- blob radial suave,
        /// semitransparente. Crisol.cs instancia varias, animando posición y
        /// alfa de cada una por código (nunca frames de textura: es UN solo
        /// sprite reusado con transform distinto, cero allocs de textura por
        /// bocanada).
        /// </summary>
        public static Sprite Humo()
        {
            const string clave = "humo";
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = S(10), h = S(10);
            var px = new Color32[w * h];
            float cx = w / 2f, cy = h / 2f, r = w / 2f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    float a = Mathf.Clamp01(1f - dist);
                    a *= a; // caída suave hacia el borde: lee como voluta, no como disco duro.
                    px[y * w + x] = new Color32(210, 205, 200, (byte)(a * 200));
                }
            }

            s = Crear(px, w, h, "ChaosAlchemyHumo");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// GRAMÁTICA §1.4 (Prensa): HUSILLO. Vástago vertical con filetes de
        /// rosca periódicos -- "nadie ha dudado jamás de qué hace un tornillo
        /// de banco" (contrato). Se monta ESTÁTICO sobre la mandíbula (la
        /// pieza que se mueve es la mandíbula misma, ya animada por
        /// Prensa.cs).
        /// </summary>
        public static Sprite Husillo(int spanCeldas)
        {
            string clave = "husillo" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Mathf.Clamp(Mathf.Max(S(8), spanCeldas * Escala / 2), S(8), S(200));
            int h = S(20);
            var px = new Color32[w * h];

            Color32 hierroAlto = new Color32(0x6A, 0x62, 0x5C, 255);
            Color32 hierro = new Color32(0x3A, 0x34, 0x30, 255);
            Color32 hierroBajo = new Color32(0x1C, 0x18, 0x16, 255);

            int semiVastago = Mathf.Max(S(1), w / 5);
            int periodo = Mathf.Max(1, S(3));
            for (int y = 0; y < h; y++)
            {
                bool rosca = (y % periodo) == 0; // filete de rosca: una línea clara cada `periodo` filas.
                for (int dx = -semiVastago; dx <= semiVastago; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    bool borde = Mathf.Abs(dx) >= semiVastago - Mathf.Max(1, Escala / 2);
                    px[y * w + x] = rosca ? hierroAlto : (borde ? hierroBajo : hierro);
                }
            }

            s = Crear(px, w, h, "ChaosAlchemyHusillo");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// GRAMÁTICA §1.4 (Banco de chispa): ARCO. Zigzag determinista (sin
        /// UnityEngine.Random -- regla de oro del proyecto) núcleo blanco +
        /// halo cian, pensado para estirarse horizontalmente entre los dos
        /// electrodos. BancoChispa.cs lo activa (alfa>0) SOLO mientras
        /// analiza y la conductividad leída es ≥1 -- si no conduce, no hay
        /// arco (la ausencia es el dato).
        /// </summary>
        public static Sprite Arco()
        {
            const string clave = "arco";
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = S(40), h = S(14);
            var px = new Color32[w * h]; // transparente.

            Color32 nucleo = new Color32(255, 255, 255, 255);
            Color32 halo = new Color32(140, 200, 255, 140);

            int centroY = h / 2;
            int prevY = centroY;
            int paso = Mathf.Max(1, S(2));
            for (int x = 0; x < w; x++)
            {
                int quiebro = ((x / paso) % 2 == 0) ? S(2) : -S(2);
                int y = Mathf.Clamp(centroY + quiebro, S(2), h - S(2) - 1);
                int yMin = Mathf.Min(prevY, y), yMax = Mathf.Max(prevY, y);
                for (int yy = yMin; yy <= yMax; yy++)
                {
                    px[yy * w + x] = nucleo;
                    if (yy - 1 >= 0) px[(yy - 1) * w + x] = halo;
                    if (yy + 1 < h) px[(yy + 1) * w + x] = halo;
                }
                prevY = y;
            }

            s = Crear(px, w, h, "ChaosAlchemyArco");
            _cache[clave] = s;
            return s;
        }

        /// <summary>Sprite blanco de 1x1 para barras/rellenos genéricos (se tinta con SpriteRenderer.color).</summary>
        public static Sprite Solido()
        {
            const string clave = "solido";
            if (_cache.TryGetValue(clave, out var s)) return s;
            s = Crear(new[] { new Color32(255, 255, 255, 255) }, 1, 1, "ChaosAlchemySolido");
            _cache[clave] = s;
            return s;
        }

        // =================================================================
        private static void MarcarRemache(Color32[] px, int w, int h, int cx, int cy, Color32 color)
        {
            // (fix playtest 6: baja resolución) el remache medía 2x2 téxeles fijos
            // en el diseño original (a 2 téxeles/celda); escalado por Escala para
            // que siga leyéndose como un remache y no un punto perdido en el metal.
            for (int dy = 0; dy < Escala; dy++)
            {
                for (int dx = 0; dx < Escala; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || x >= w || y < 0 || y >= h) continue;
                    px[y * w + x] = color;
                }
            }
        }

        /// <summary>Taco cuadrado macizo de `size` téxeles en (x0,y0) -- usado por <see cref="MarcoContenedor"/> para el "remate" de sus cuatro esquinas.</summary>
        private static void MarcarRemateCuadrado(Color32[] px, int w, int h, int x0, int y0, int size, Color32 color)
        {
            for (int dy = 0; dy < size; dy++)
            {
                for (int dx = 0; dx < size; dx++)
                {
                    int x = x0 + dx, y = y0 + dy;
                    if (x < 0 || x >= w || y < 0 || y >= h) continue;
                    px[y * w + x] = color;
                }
            }
        }

        /// <summary>Crea el sprite a 1 píxel = 1 unidad (ver doc de la clase) con filtrado Point y sin mipmaps.</summary>
        private static Sprite Crear(Color32[] px, int w, int h, string nombre)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = nombre,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
        }
    }
}
