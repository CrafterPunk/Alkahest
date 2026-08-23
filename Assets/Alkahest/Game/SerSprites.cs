using UnityEngine;

namespace Alkahest.Game
{
    /// <summary>
    /// [TenThousandYears · pivot playtest 21, ENCARGO B "LOS SERES"] Fábrica de
    /// SILUETAS y texturas generadas por código para el Rescoldo (Criatura)
    /// y su Capullo: el cuerpo (un bulbo/tubérculo ASIMÉTRICO con cuello,
    /// NO un corazón -- ver el docblock de <see cref="MascaraCorazon"/> para
    /// la corrección de arte que llevó a esta forma), los zarcillos (brotes
    /// finos que terminan en punta, no cintas) y la cáscara del capullo
    /// (óvalo cerrado + 5 fases de grieta). También el halo de luz/oscuridad
    /// que convierte "un cuarto oscuro" en "una cosa viva iluminando un
    /// cuarto oscuro".
    ///
    /// MISMO PATRÓN QUE MaquinariaSprites.VidrioRedoma (léelo antes de tocar
    /// esto): la silueta se construye una vez, en Color32[]/byte[] planos, a
    /// la MISMA escala x3 de téxeles del proyecto (<see cref="Escala"/>), y
    /// se cachea. La firma visual de la SEMILLA (color+patrón+borde reales
    /// de esta partida) NO se genera aquí: eso lo hace
    /// <see cref="FirmaVisualFabrica"/> (Game/StorageRack.cs, internal en
    /// este ensamblado) a partir de la MÁSCARA que aquí se construye — el
    /// mismo patrón de uso que StorageRack.ObtenerFirmaSprites, documentado
    /// con detalle en Criatura.cs/Capullo.cs. La ÚNICA decoración que este
    /// archivo añade ENCIMA de esa firma es <see cref="AplicarBrasa"/> (un
    /// núcleo cálido pequeño y descentrado) + <see cref="AplicarVolumen"/>
    /// (luz arriba/sombra abajo) -- nunca un patrón que compita con el de la
    /// semilla.
    ///
    /// CONVENCIÓN DE FILAS: igual que VidrioRedoma, el índice 0 de los
    /// arrays generados aquí es la parte de ABAJO del sprite resultante
    /// (Texture2D.SetPixels32 empieza en la esquina inferior izquierda) —
    /// el cuerpo nace con el cuello/raíz abajo (donde se apoya, hundido, en
    /// la cuna) y la cúpula redondeada arriba; el capullo nace con el
    /// remate redondeado abajo (donde se apoya en la repisa) y el remate
    /// estrecho arriba.
    ///
    /// PASE MINIMALISTA (esta ronda, ver el informe -- Cesar: "no quiero que
    /// te martirices en cómo se ve... solo con que se perciba un estilo
    /// gráfico minimalista es suficiente"). El objetivo dejó de ser bonito y
    /// pasó a ser LEGIBLE: silueta clara, contraste alto, MENOS detalle, no
    /// más -- a la escala real de juego el cuerpo mide pocas decenas de
    /// píxeles, así que la textura fina (ruido de alta frecuencia en la
    /// silueta, jitter de borde de 1 téxel, sub-ramas de grieta) deja de
    /// leerse como "orgánico" y se convierte en ruido indistinguible de un
    /// artefacto de compresión. Se quitó ornamento (ver MascaraCorazon y
    /// AplicarGrietas, cada uno documenta su propio recorte) y se subió el
    /// contraste interior a cambio (AplicarVolumen). El HALO
    /// (<see cref="HaloLuz"/>, playtest 22: unificado en una sola forma que
    /// el llamante tiñe en runtime -- ver el docblock de esa sección más
    /// abajo) SÍ se toca en la ronda del temperamento, pero solo en CÓMO se
    /// usa (posición, capas, tinte): la forma en sí (el bulto de alfa) es
    /// la MISMA que Cesar ya validó en el playtest 21, sin retocar.
    /// </summary>
    public static class SerSprites
    {
        /// <summary>Misma densidad de téxeles/celda que MaquinariaSprites (playtest 6): sin esto, a la escala real del mundo (CellWorldSize=0.1) estas siluetas se verían "a bloques".</summary>
        public const int Escala = 3;
        private static int S(int v) => v * Escala;

        // -----------------------------------------------------------------
        // Lienzos (ver el prototipo /tmp/ser_sprites_proto.py del informe:
        // estas medidas son las que se juzgaron a ojo antes de portar nada).
        // -----------------------------------------------------------------
        public const int CorazonW = 30 * Escala;
        public const int CorazonH = 28 * Escala;
        public const int ZarcilloW = 7 * Escala;
        public const int ZarcilloH = 46 * Escala;
        public const int CapulloW = 34 * Escala;
        public const int CapulloH = 52 * Escala;

        /// <summary>Fases de grieta del capullo, 0 (intacto) .. 4 (a punto de abrirse). Coincide con Capullo.FasesGrieta.</summary>
        public const int FasesGrieta = 5;

        // ===================================================================
        // CUERPO DEL RESCOLDO — bulbo/tubérculo asimétrico (playtest 21,
        // corrección de arte; SIMPLIFICADO otra vez en el pase MINIMALISTA
        // de esta ronda, ver el informe: "el objetivo ya no es bonito, es
        // LEGIBLE... si algo te obliga a elegir entre más bonito y más
        // legible a tamaño pequeño, elige legible"). NO es una curva
        // matemática cerrada: es el MISMO patrón semiancho-por-fila que
        // MaquinariaSprites.VidrioRedoma y MascaraCapullo, con UNA sola
        // ondulación de baja frecuencia (antes tres, superpuestas -- a la
        // escala real de juego, unas pocas decenas de píxeles, las dos
        // frecuencias altas no se leían como "orgánico": eran ruido fino
        // indistinguible de un jitter de un solo téxel, así que estorbaban
        // más de lo que aportaban) y partido en semiancho-izquierdo/
        // semiancho-derecho independientes para que la asimetría siga
        // siendo real (un costado más lleno), no decorativa.
        //
        // TAMBIÉN FUERA (mismo pase minimalista): el jitter de borde de 1
        // téxel ("tallado a mano") que vivía al final de este método --
        // agujeros de 1 píxel sembrados al azar en el contorno. La silueta
        // es el canal MÁS importante para leer "esto es una criatura" de un
        // vistazo (regla de legibilidad de este pase); un contorno LIMPIO y
        // continuo se reconoce de un vistazo, un contorno con mordiscos
        // aleatorios de 1 téxel se lee como aliasing/ruido de compresión, no
        // como textura tallada. El contorno REAL (la asimetría + la
        // hendidura) ya es suficiente "mano" sin ese ruido encima.
        //
        // RECHAZADO antes de este método (ver el informe, comparación
        // vieja/nueva): la curva implícita de corazón (x²+y²-1)³-x²y³≤0.
        // Cesar, mirando las imágenes: "se lee como un escudo heráldico o
        // un corazón de San Valentín con un sol dentro". El nombre da la
        // pista -- Rescoldo = brasa, un cuerpo frío con una brasa dentro
        // (ver <see cref="AplicarBrasa"/>) -- no un símbolo de amor.
        //
        // El nombre del método se conserva ("MascaraCorazon") por
        // ESTABILIDAD DE LA API interna de este mismo archivo/Criatura.cs
        // (sin compilador a mano, renombrar en cascada es puro riesgo sin
        // beneficio para el jugador): lo que cambió es la FORMA que dibuja,
        // documentada aquí.
        // ===================================================================
        public static byte[] MascaraCorazon(int w, int h, int seed)
        {
            var alpha = new byte[w * h];
            var rng = new System.Random(seed);

            // Asimetría REAL (un costado más lleno que el otro en TODA la
            // altura, no solo un wobble): lean desplaza el eje central,
            // fullSide+asymAmt reparten más ancho a un lado que al otro.
            float lean = ((float)rng.NextDouble() * 2f - 1f) * 0.05f;
            float fullSide = rng.NextDouble() < 0.5 ? 1f : -1f;
            float asymAmt = 0.10f + (float)rng.NextDouble() * 0.06f; // 0.10..0.16

            // Ruido de baja frecuencia sobre semi(y) (UNA sola ondulación
            // suave, no una curva perfecta -- ver el docblock del método,
            // pase minimalista: dos frecuencias altas menos) + UNA hendidura
            // localizada en un costado (irregularidad real, no ruido).
            float nfase1 = (float)rng.NextDouble() * 6.2832f;
            float dentT = 0.30f + (float)rng.NextDouble() * 0.28f;
            float dentSide = rng.NextDouble() < 0.5 ? 1f : -1f;
            const float dentWidth = 0.09f;
            float dentDepth = 0.12f + (float)rng.NextDouble() * 0.09f;

            for (int y = 0; y < h; y++)
            {
                // y=0 es ABAJO (cuello/raíz, se hunde en la cuna), y=h-1
                // arriba (cúpula) -- misma convención de fila que el resto
                // del archivo (ver docblock de la clase).
                float t = h <= 1 ? 0f : y / (float)(h - 1); // 0 abajo .. 1 arriba

                float semiBase;
                if (t < 0.08f)
                {
                    float k = t / 0.08f;
                    semiBase = w * (0.085f + 0.055f * k); // cuello: estrecho, se abre rápido -- NUNCA un vértice en punta.
                }
                else if (t < 0.46f)
                {
                    float k = (t - 0.08f) / 0.38f;
                    float ease = k * k * (3f - 2f * k);
                    semiBase = w * (0.14f + 0.36f * ease); // panza: crece hasta el máximo (más ancho ABAJO, pedido explícito).
                }
                else if (t < 0.80f)
                {
                    float k = (t - 0.46f) / 0.34f;
                    float ease = k * k * (3f - 2f * k);
                    semiBase = w * (0.50f - 0.10f * ease); // entra en la cúpula.
                }
                else
                {
                    float k = (t - 0.80f) / 0.20f;
                    semiBase = w * 0.40f * Mathf.Cos(k * Mathf.PI * 0.5f); // remate REDONDEADO -- nunca un borde recto arriba.
                }

                float wob = 1f + 0.06f * Mathf.Sin(t * 5f + nfase1); // una sola ondulación (amplitud subida de 0.05 a 0.06 para compensar las dos que se quitaron -- la silueta sigue sin ser un óvalo perfecto).
                semiBase *= wob;

                float semiL = semiBase * (1f - asymAmt * fullSide);
                float semiR = semiBase * (1f + asymAmt * fullSide);

                float dd = Mathf.Abs(t - dentT);
                if (dd < dentWidth)
                {
                    float k = 1f - dd / dentWidth;
                    float reduce = dentDepth * k * k * w;
                    if (dentSide > 0f) semiR = Mathf.Max(w * 0.03f, semiR - reduce);
                    else semiL = Mathf.Max(w * 0.03f, semiL - reduce);
                }

                float cx = w * 0.5f + lean * w;
                int x0 = Mathf.RoundToInt(cx - semiL);
                int x1 = Mathf.RoundToInt(cx + semiR);
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(w - 1, x1); x++)
                    alpha[y * w + x] = 255;
            }

            // (pase minimalista, ver el docblock del método) SIN jitter de
            // borde de 1 téxel encima: el contorno sale LIMPIO de las curvas
            // de arriba -- a tamaño real un contorno continuo se reconoce de
            // un vistazo, un contorno mordido al azar se lee como ruido.
            return alpha;
        }

        /// <summary>
        /// LA BRASA (playtest 21, corrección de arte: "si solo haces una
        /// cosa de esta lista, haz esta"). Sustituye a AplicarNucleoCalido
        /// (RECHAZADO: un estallido radial grande y centrado que se leía
        /// como "pelota de playa"/"sol dentro de un corazón"). La brasa es
        /// pequeña y DESCENTRADA (nunca en el centro geométrico); su
        /// POSICIÓN sale del `seed` de la silueta (sin cambios). Es la ÚNICA
        /// decoración añadida encima de FirmaVisualFabrica -- el resto del
        /// interior (patrón/color/borde) viene entero de la semilla, sin
        /// tocar (pedido explícito: "el interior tiene que venir ENTERO de
        /// FirmaVisualFabrica").
        ///
        /// SESGO DE COLOR POR TEMPERAMENTO (playtest 22, nuevo parámetro
        /// `temperamento`, ver el bloque CONFIG — TEMPERAMENTO en
        /// Criatura.cs): dos tramos LINEALES sobre TRES anclas -- frío
        /// (t=0), templado (t=0.5), calor (t=1) -- en vez de un solo lerp de
        /// un extremo al otro. Se eligieron tres anclas y no dos a propósito:
        /// el punto medio ARITMÉTICO de "frío" y "calor" no cae en un gris
        /// neutro (con frío=(R-0.60,G+0.10,B+1.00) y calor=(R+1.00,G+0.55,
        /// B-0.35), el promedio sale (R+0.20,G+0.325,B+0.325) -- verde/cian
        /// de sobra visible, NO neutro), así que TEMPLADO necesita su propia
        /// ancla declarada (R+0.25,G+0.25,B+0.25: brillo parejo, sin sesgo
        /// de matiz) en vez de salir de la interpolación. CALOR conserva
        /// EXACTO el sesgo original (R+1.00/G+0.55/B-0.35 de `fuerza`) --
        /// "si solo haces una cosa de esta lista, haz esta" y el ámbar que
        /// Cesar ya validó en el playtest 21 no cambia. FRÍO es su
        /// simétrico razonable: B sube fuerte, R baja, G casi no se mueve
        /// (el hielo no es verde).
        /// </summary>
        public static void AplicarBrasa(Color32[] px, int w, int h, byte[] alpha, int seed, float temperamento, int fuerza = 150)
        {
            var rng = new System.Random(unchecked(seed * 7 + 4242));
            float cx = w * (0.50f + ((float)rng.NextDouble() * 2f - 1f) * 0.14f);
            float cy = h * (0.42f + ((float)rng.NextDouble() * 2f - 1f) * 0.08f); // ligeramente bajo del centro -- "brasa", no "sol".
            float radio = Mathf.Min(w, h) * 0.17f;

            float mR, mG, mB;
            if (temperamento < 0.5f)
            {
                float k = temperamento / 0.5f; // 0 (frío puro) .. 1 (templado)
                mR = Mathf.Lerp(-0.60f, 0.25f, k);
                mG = Mathf.Lerp(0.10f, 0.25f, k);
                mB = Mathf.Lerp(1.00f, 0.25f, k);
            }
            else
            {
                float k = (temperamento - 0.5f) / 0.5f; // 0 (templado) .. 1 (calor puro)
                mR = Mathf.Lerp(0.25f, 1.00f, k);
                mG = Mathf.Lerp(0.25f, 0.55f, k);
                mB = Mathf.Lerp(0.25f, -0.35f, k);
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (alpha[i] == 0) continue;
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / radio;
                    if (d >= 1f) continue;
                    float t = Mathf.Pow(1f - d, 2.2f);
                    var c = px[i];
                    c.r = (byte)Mathf.Clamp(c.r + fuerza * mR * t, 0f, 255f);
                    c.g = (byte)Mathf.Clamp(c.g + fuerza * mG * t, 0f, 255f);
                    c.b = (byte)Mathf.Clamp(c.b + fuerza * mB * t, 0f, 255f);
                    px[i] = c;
                }
            }
        }

        /// <summary>
        /// VOLUMEN (playtest 21, corrección de arte: "ahora es plano").
        /// Borde superior más claro, base más oscura: interpolación
        /// monótona de -sombra (fila 0, abajo) a +luz (fila h-1, arriba),
        /// una luz implícita cayendo desde arriba en vez de un color
        /// uniforme en toda la silueta. Magnitud SUBIDA en el pase
        /// minimalista de esta ronda (26/34 -> 34/44): con menos ornamento
        /// alrededor (ver MascaraCorazon/AplicarGrietas, simplificados en
        /// esta misma ronda) el contraste interior tiene que cargar más peso
        /// él solo para que el bulbo siga leyéndose con volumen a tamaño
        /// real -- "refuerza contorno y contraste interior" (pedido
        /// explícito), no más detalle, más contraste.
        /// </summary>
        public static void AplicarVolumen(Color32[] px, int w, int h, byte[] alpha, int luz = 34, int sombra = 44)
        {
            for (int y = 0; y < h; y++)
            {
                float t = h <= 1 ? 0f : y / (float)(h - 1); // 0 abajo, 1 arriba (misma convención de fila que MascaraCorazon).
                int delta = Mathf.RoundToInt(-sombra + (luz + sombra) * t);
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (alpha[i] == 0) continue;
                    var c = px[i];
                    c.r = (byte)Mathf.Clamp(c.r + delta, 0, 255);
                    c.g = (byte)Mathf.Clamp(c.g + delta, 0, 255);
                    c.b = (byte)Mathf.Clamp(c.b + delta, 0, 255);
                    px[i] = c;
                }
            }
        }

        /// <summary>
        /// Réplica EXACTA del criterio de SimRenderer.ComputeCellColor para
        /// Vivium dormido ("ligera desaturación hacia gris... sin un shader
        /// aparte"): gray=(r+g+b)/3, lerp 0.55. Se usa para bakear la
        /// variante "Dormida" del corazón UNA vez (nunca en Update) — ver
        /// Criatura.cs.
        /// </summary>
        public static Color32[] Desaturar(Color32[] px, float t)
        {
            var salida = new Color32[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                int gray = (c.r + c.g + c.b) / 3;
                salida[i] = new Color32(
                    LerpByte(c.r, (byte)gray, t),
                    LerpByte(c.g, (byte)gray, t),
                    LerpByte(c.b, (byte)gray, t),
                    c.a);
            }
            return salida;
        }

        // ===================================================================
        // ZARCILLOS
        // ===================================================================

        /// <summary>
        /// BROTE, no cinta (playtest 21, corrección de arte: "hoy son dos
        /// correas de ancho constante que parecen una pajarita"). Se afina
        /// de la base (fila 0, abajo -- el punto de anclaje/pivote) a una
        /// PUNTA DE VERDAD (fila h-1, arriba; tipW bajado de 0.045 a 0.018
        /// de w), con más curva (0.55 -> 0.75) para que no se lea como un
        /// segmento recto. Una sola máscara compartida por los 4 zarcillos:
        /// la variedad de LONGITUD la da Criatura.LongitudBaseFrac (escala
        /// distinta por instancia), la de posición/ángulo la da
        /// Criatura.AnclasFrac/AngulosBaseAbs -- la silueta en sí es igual
        /// para los 4, como antes.
        ///
        /// SEGUNDA PASADA (playtest 21, "los de Contenta parecen astas de
        /// ciervo: dos cuñas gruesas"): baseW bajado otra vez, de 0.34 a
        /// 0.27 de w -- el afinado de la primera pasada seguía dejando una
        /// base ancha de sobra a la escala real del mundo (Criatura.
        /// AnchoMundoZarcillo, también reducido, ver esa constante). El
        /// hueco respecto al bulbo NO estaba en esta máscara -- ver
        /// Criatura.AnclasFrac para el análisis numérico de dónde estaba de
        /// verdad.
        /// </summary>
        public static byte[] MascaraZarcillo(int w, int h)
        {
            var alpha = new byte[w * h];
            float baseW = w * 0.27f;
            float tipW = w * 0.018f;
            float cx0 = w * 0.5f;
            for (int y = 0; y < h; y++)
            {
                float t = h <= 1 ? 0f : y / (float)(h - 1); // 0 en la base, 1 en la punta
                float te = Mathf.Pow(t, 1.9f);
                float semi = tipW + (baseW - tipW) * (1f - te);
                float offset = Mathf.Sin(t * Mathf.PI * 1.05f) * (w * 0.75f) * Mathf.Pow(t, 1.3f);
                float cx = cx0 + offset;
                int x0 = Mathf.RoundToInt(cx - semi);
                int x1 = Mathf.RoundToInt(cx + semi);
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(w - 1, x1); x++)
                    alpha[y * w + x] = 255;
            }
            return alpha;
        }

        // ===================================================================
        // CAPULLO — óvalo cerrado (mismo patrón semiancho-por-fila que
        // VidrioRedoma, sin cuello) + 5 fases de grieta.
        // ===================================================================

        /// <summary>Silueta del capullo: remate redondeado abajo (se apoya en la repisa), panza, remate estrecho arriba.</summary>
        public static byte[] MascaraCapullo(int w, int h)
        {
            var alpha = new byte[w * h];
            float cx = w * 0.5f;
            for (int y = 0; y < h; y++)
            {
                float fracDesdeAbajo = h <= 1 ? 0f : y / (float)(h - 1);
                float t = 1f - fracDesdeAbajo; // 0 abajo (remate ancho), 1 arriba (remate estrecho)
                float semi;
                if (t < 0.12f) { float k = t / 0.12f; semi = (w * 0.30f) * Mathf.Sin(k * Mathf.PI * 0.5f); }
                else if (t < 0.62f) { float k = (t - 0.12f) / 0.50f; semi = w * (0.30f + 0.20f * k); }
                else if (t < 0.90f) { float k = (t - 0.62f) / 0.28f; semi = w * (0.50f - 0.06f * k); }
                else { float k = (t - 0.90f) / 0.10f; semi = (w * 0.44f) * Mathf.Cos(k * Mathf.PI * 0.5f); }

                int x0 = Mathf.RoundToInt(cx - semi);
                int x1 = Mathf.RoundToInt(cx + semi);
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(w - 1, x1); x++)
                    alpha[y * w + x] = 255;
            }
            return alpha;
        }

        /// <summary>Mezcla cada téxel de la silueta hacia un tono de cáscara fijo, una fracción `mezcla` (0..1): "pariente pero distinto" del corazón (misma firma de la semilla, con una variación).</summary>
        public static void MatizarHaciaCascara(Color32[] px, byte[] alpha, float mezcla, Color32 tonoCascara)
        {
            for (int i = 0; i < px.Length; i++)
            {
                if (alpha[i] == 0) continue;
                var c = px[i];
                px[i] = new Color32(
                    LerpByte(c.r, tonoCascara.r, mezcla),
                    LerpByte(c.g, tonoCascara.g, mezcla),
                    LerpByte(c.b, tonoCascara.b, mezcla),
                    c.a);
            }
        }

        /// <summary>
        /// Graba una red de grietas sobre `px` (mutado in-place), determinista
        /// para (semilla, fase): un paseo aleatorio que nunca sale de la
        /// silueta, oscureciendo la grieta y —desde la fase 3— dejando asomar
        /// un "brillo" (aclarado) en parte de sus téxeles, como si algo se
        /// viera a través de la fisura. Fase 0 no dibuja nada (cáscara
        /// intacta). Reseedear con el MISMO `semilla` para fases crecientes
        /// hace que la red de fases bajas quede aproximadamente contenida en
        /// las fases altas (progresión creíble, verificado en el prototipo
        /// Python) sin tener que guardar estado entre fases.
        ///
        /// SIMPLIFICADO en el pase MINIMALISTA de esta ronda (ver el informe):
        /// fuera las sub-ramas que salían de la grieta principal a mitad de
        /// camino -- a tamaño real esa telaraña fina de segundo nivel no se
        /// distinguía de ruido, solo ensuciaba la lectura de "cuántas fases
        /// lleva". El jitter de ángulo del trazo principal también baja
        /// (0.35 -> 0.22 rad): grietas más RECTAS se leen de un vistazo como
        /// fisuras, un garabato muy nervioso se lee como textura sin más.
        /// </summary>
        public static void AplicarGrietas(Color32[] px, byte[] alpha, int w, int h, int fase, int semilla)
        {
            if (fase <= 0) return;
            var rng = new System.Random(semilla);
            int nRamas = 1 + fase;

            for (int rama = 0; rama < nRamas; rama++)
            {
                float x = w * (0.30f + 0.40f * (float)rng.NextDouble());
                // cerca de ARRIBA (índice alto, y=0 es la base/abajo -- ver MascaraCapullo).
                float y = (h - 1) - h * (0.06f + 0.05f * (float)rng.NextDouble());
                float angulo = -Mathf.PI / 2f + ((float)rng.NextDouble() - 0.5f) * 0.6f; // mayormente hacia ABAJO
                float largo = h * (0.35f + 0.12f * fase) * (0.7f + 0.3f * (float)rng.NextDouble());
                int pasos = (int)largo;
                int grosor = fase < 3 ? 1 : 2;

                for (int s = 0; s < pasos; s++)
                {
                    angulo += ((float)rng.NextDouble() - 0.5f) * 0.22f;
                    x += Mathf.Cos(angulo);
                    y += Mathf.Sin(angulo);
                    int ix = Mathf.RoundToInt(x), iy = Mathf.RoundToInt(y);
                    if (ix < 0 || ix >= w || iy < 0 || iy >= h) break;
                    if (alpha[iy * w + ix] == 0) break;

                    for (int dx = -grosor; dx <= grosor; dx++)
                    {
                        for (int dy = -grosor; dy <= grosor; dy++)
                        {
                            if (dx * dx + dy * dy > grosor * grosor + 1) continue;
                            int xx = ix + dx, yy = iy + dy;
                            if (xx < 0 || xx >= w || yy < 0 || yy >= h) continue;
                            int ii = yy * w + xx;
                            if (alpha[ii] == 0) continue;
                            Oscurecer(px, ii, 90);
                            if (fase >= 3 && rng.NextDouble() < 0.5) Aclarar(px, ii, 120, 100, 60);
                        }
                    }
                }
            }
        }

        private static void Oscurecer(Color32[] px, int i, int cant)
        {
            var c = px[i];
            c.r = (byte)Mathf.Max(0, c.r - cant);
            c.g = (byte)Mathf.Max(0, c.g - cant);
            c.b = (byte)Mathf.Max(0, c.b - cant);
            px[i] = c;
        }

        private static void Aclarar(Color32[] px, int i, int cr, int cg, int cb)
        {
            var c = px[i];
            c.r = (byte)Mathf.Min(255, c.r + cr);
            c.g = (byte)Mathf.Min(255, c.g + cg);
            c.b = (byte)Mathf.Min(255, c.b + cb);
            px[i] = c;
        }

        // ===================================================================
        // BORDE (estarcido para FirmaVisualFabrica) — misma técnica que
        // StorageRack.PrepararMascaraFirma (banda de vecindad alrededor de la
        // silueta), replicada aquí porque ese método es privado y vive en un
        // archivo de otro encargo en la misma ronda. Llamada UNA vez por
        // silueta, nunca por frame.
        // ===================================================================
        public static bool[] CalcularBorde(byte[] alpha, int w, int h, int banda)
        {
            var esBorde = new bool[alpha.Length];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (alpha[i] == 0) continue;
                    bool borde = false;
                    for (int oy = -banda; oy <= banda && !borde; oy++)
                    {
                        int yy = y + oy;
                        if (yy < 0 || yy >= h) { borde = true; break; }
                        for (int ox = -banda; ox <= banda; ox++)
                        {
                            int xx = x + ox;
                            if (xx < 0 || xx >= w || alpha[yy * w + xx] == 0) { borde = true; break; }
                        }
                    }
                    esBorde[i] = borde;
                }
            }
            return esBorde;
        }

        // ===================================================================
        // HALO — LA FORMA de la luz (playtest 21, CORREGIDO dos veces antes
        // de esto; playtest 22, UNIFICADO en una sola textura).
        //
        // Intento 1 (rechazado): alpha=0 fijo en un radio r0 -> "rosquilla"
        // de color flotando lejos del corazón.
        // Intento 2 (rechazado, y el que se envió a Cesar): alpha CONTINUO
        // desde el centro pero SUBIENDO SIN BAJAR hasta quedar opaco/negro a
        // partir de r_edge=62% del radio inscrito -- eso deja el CUADRADO
        // ENTERO de la textura, más allá de r_edge (esquinas incluidas),
        // pintado de negro sólido uniforme. Contra un fondo negro puro es
        // invisible (por eso pasó la revisión); contra cualquier otra cosa
        // -- incluida la propia sala del juego, que NO es negro puro (ver
        // Sim/Universe.cs, comentario "GARANTÍA 3": (0.150,0.115,0.190)
        // arriba, (0.062,0.048,0.058) abajo) -- es un cuadrado duro y
        // perfectamente visible. Esto es lo que Cesar vio en
        // preview_sala_contenta.png Y lo que de verdad causaba la "caja
        // negra" que parecía salir del corazón en cuadricula_estados.png: NO
        // era un bug de alfa del corazón (verificado: FirmaVisualFabrica.
        // GenerarPixeles ya escribe `px[i]=default` -- a=0 -- fuera de la
        // máscara, y AplicarBrasa/AplicarVolumen/Desaturar respetan
        // alpha[i]==0), era este halo sentado detrás.
        //
        // Intento 3 (playtest 21, el que quedó en pie): el alfa es un BULTO
        // que SUBE y LUEGO BAJA otra vez a 0 -- nunca hay una meseta opaca.
        // Sube de una base continua (no cero: si tocara 0 en el propio
        // centro vuelve el bug de la "rosquilla" del intento 1) hasta un
        // pico a r_pico=30% del radio inscrito, y baja a alfa=0 en
        // r_cero=78% del radio inscrito -- CON MARGEN REAL antes del borde
        // del lienzo (100%) y de sobra antes de las ESQUINAS del cuadrado
        // (a 141% del radio inscrito). Nunca llega a negro opaco: el techo
        // de alfa es 0.88, y el color tira a un gris casi-negro (6,6,6), no
        // a negro puro, donde bump>0 -- aunque eso ya es irrelevante en la
        // práctica porque justo ahí el alfa ya es 0. DOS variantes
        // pregeneradas (cálida/fría), cruzadas en alfa por Criatura según el
        // ESTADO (Contenta=cálida, Aletargada=casi fría...).
        //
        // PLAYTEST 22, "EL HALO ES LUZ DE VERDAD": el intento 3 seguía
        // siendo un tinte flotando sobre TODO (sortingOrder 100+, por
        // encima de la maquinaria) -- "se ilumina cuando come pero no sé si
        // es fuente de luz" (Cesar). Dos cambios, NINGUNO en esta forma
        // (rPico/rCero/baseCentro se CONSERVAN intactos, ya verificados):
        //  1) La textura deja de pre-tintar cálido/frío -- ahora es casi
        //     BLANCA (240,238,232), y el color real (frío/templado/calor,
        //     el TEMPERAMENTO de la criatura, ver Criatura.
        //     ColorHaloDeTemperamento) se aplica en RUNTIME vía
        //     SpriteRenderer.color -- multiplicar por un tinte es gratis, no
        //     hace falta cachear tres variantes ni cruzar dos texturas.
        //  2) Criatura ya NO la dibuja en sortingOrder 100+: la sienta justo
        //     ENCIMA del sprite de la simulación (-5) y DEBAJO de todo lo
        //     demás, en DOS capas (núcleo pequeño+opaco, wash grande+suave)
        //     -- ver Criatura.BuildHalo/ActualizarHalo para el porqué
        //     completo. Esta función solo entrega la FORMA; de la posición y
        //     el tinte ya no sabe nada.
        // ===================================================================
        private const int HaloTex = 128;
        private static Sprite _haloLuz;
        private static Texture2D _haloLuzTex;

        /// <summary>La FORMA de la luz -- una sola textura, casi blanca, cacheada para siempre (no depende de la semilla ni del temperamento). El tinte real lo aplica el llamante vía SpriteRenderer.color -- ver el docblock de arriba.</summary>
        public static Sprite HaloLuz()
        {
            if (_haloLuz == null) _haloLuz = ConstruirHalo(out _haloLuzTex);
            return _haloLuz;
        }

        private static Sprite ConstruirHalo(out Texture2D textura)
        {
            int size = HaloTex;
            var px = new Color32[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;
            float rMax = size * 0.5f;          // radio inscrito del lienzo cuadrado.
            float rPico = rMax * 0.30f;         // el tinte alcanza su máximo aquí...
            float rCero = rMax * 0.78f;         // ...y ha vuelto a alfa 0 aquí -- MUCHO antes del borde (100%) y de las esquinas (141%).
            const float baseCentro = 0.30f;     // alfa relativo YA en el centro (dist=0): continuo, nunca 0 en medio (evita la "rosquilla" del intento 1).
            var gris = new Color32(240, 238, 232, 255); // casi blanco -- el llamante multiplica esto por su propio tinte (ver docblock).

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float bump;
                    if (dist <= rPico)
                    {
                        float t = rPico > 0f ? dist / rPico : 0f;
                        t = t * t * (3f - 2f * t); // smoothstep
                        bump = baseCentro + (1f - baseCentro) * t;
                    }
                    else if (dist < rCero)
                    {
                        float t = (dist - rPico) / (rCero - rPico);
                        t = t * t * (3f - 2f * t); // smoothstep
                        bump = 1f - t;
                    }
                    else
                    {
                        bump = 0f;
                    }

                    float a = Mathf.Pow(bump, 0.9f) * 0.88f; // techo <1: nunca opaco del todo.
                    px[y * size + x] = new Color32(gris.r, gris.g, gris.b, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            return CrearSprite(px, size, size, new Vector2(0.5f, 0.5f), "TenThousandYearsHaloLuz", out textura);
        }

        // ===================================================================
        // HELPERS DE TEXTURA/SPRITE (mismo criterio que
        // MaquinariaSprites.Crear: RGBA32, FilterMode.Point, sin mipmaps,
        // pixelsPerUnit=1). Con `out Texture2D` para que Criatura/Capullo
        // puedan liberar la textura en OnDestroy (mismo criterio que
        // StorageRack con sus firmas por material) -- un Sprite no libera su
        // Texture2D solo por destruirse el GameObject que lo usa.
        // ===================================================================
        public static Sprite CrearSprite(Color32[] px, int w, int h, Vector2 pivot, string nombre, out Texture2D textura)
        {
            textura = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = nombre,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            textura.SetPixels32(px);
            textura.Apply(false, false);
            return Sprite.Create(textura, new Rect(0, 0, w, h), pivot, 1f, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Sprite blanco opaco de una máscara (0/255 -> transparente/blanco): para siluetas que se tiñen en runtime vía SpriteRenderer.color en vez de llevar firma visual propia (los zarcillos, demasiado finos para que un patrón se note).</summary>
        public static Sprite SpriteDeMascara(byte[] alpha, int w, int h, Vector2 pivot, string nombre, out Texture2D textura)
        {
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++)
                px[i] = alpha[i] != 0 ? new Color32(255, 255, 255, 255) : default;
            return CrearSprite(px, w, h, pivot, nombre, out textura);
        }

        private static byte LerpByte(byte from, byte to, float t) => (byte)Mathf.RoundToInt(from + (to - from) * t);
    }
}
