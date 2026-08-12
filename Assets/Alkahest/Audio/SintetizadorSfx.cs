using UnityEngine;

namespace Alkahest.Audio
{
    /// <summary>
    /// Fábrica ESTÁTICA de <see cref="AudioClip"/> sintetizados por código: el
    /// proyecto es CERO ASSETS (ver CLAUDE.md), así que aquí no se importa ni
    /// un solo .wav — cada clip nace como un array de floats en [-1,1] que se
    /// sube a un AudioClip con <see cref="AudioClip.Create"/> + SetData.
    ///
    /// CACHÉ: cada clip se construye UNA sola vez (propiedad `??=` sobre un
    /// campo estático) la primera vez que alguien lo pide -- en la práctica,
    /// la primera vez que <c>Audio/DirectorDeAudio.Init()</c> los toca, al
    /// arrancar la partida. Nunca se regenera.
    ///
    /// DETERMINISMO: <see cref="_rng"/> es un System.Random LOCAL a esta
    /// clase, usado solo para el jitter de síntesis (desafinado suave, ruido,
    /// posiciones de granos). Esto es capa de PRESENTACIÓN (sonido), no
    /// simulación -- nunca se usa para decidir nada que afecte a Sim/, así
    /// que no rompe el determinismo del juego (mismo criterio que el jitter
    /// de color de Sim/Universe.cs, que también usa un System.Random propio
    /// solo en la creación).
    ///
    /// DIRECCIÓN DE SONIDO (ver encargo): nada de senos puros pelados ni
    /// ruido blanco crudo -- todo pasa por al menos un paso-bajo de un polo
    /// antes de salir de aquí, y todo one-shot empieza y acaba en silencio
    /// (envolvente con ataque/caída, nunca un buffer que arranca o corta en
    /// seco -> clic).
    /// </summary>
    public static class SintetizadorSfx
    {
        /// <summary>Frecuencia de muestreo para los BUCLES largos (ambiente, fuego, grifos): bajada de 44.1kHz a 22.05kHz porque nadie necesita fidelidad de estudio para un lecho de ruido, y generar ~4s de bucle a 44.1kHz duplicaría el coste de arranque sin beneficio audible.</summary>
        private const int SR_LOOP = 22050;
        /// <summary>Frecuencia de muestreo para los ONE-SHOT (cortos, mayoría &lt;0.6s): calidad completa, coste de síntesis irrelevante a esta duración.</summary>
        private const int SR_ONESHOT = 44100;

        // Semilla fija: reproducible entre sesiones (útil al iterar sobre el
        // timbre) sin que importe lo más mínimo para el determinismo del
        // juego (ver doc de la clase).
        private static readonly System.Random _rng = new System.Random(20260812);

        // -----------------------------------------------------------------
        // Caché de clips (una entrada por sonido pedido en el encargo).
        // -----------------------------------------------------------------
        private static AudioClip _lechoAmbiental;
        private static AudioClip _fuegoBucle;
        private static AudioClip _grifoLiquido;
        private static AudioClip _grifoPolvo;
        private static AudioClip _grifoGas;
        private static AudioClip _aspirar;
        private static AudioClip _verter;
        private static AudioClip _ignicion;
        private static AudioClip _cristalizarCongelar;
        private static AudioClip _tolvaTraga;
        private static AudioClip _bautizar;
        private static AudioClip _encargoCompletado;
        private static AudioClip _finDeJornada;

        public static AudioClip LechoAmbiental => _lechoAmbiental ??= ConstruirLechoAmbiental();
        public static AudioClip FuegoBucle => _fuegoBucle ??= ConstruirFuegoBucle();
        public static AudioClip GrifoLiquido => _grifoLiquido ??= ConstruirGrifoLiquido();
        public static AudioClip GrifoPolvo => _grifoPolvo ??= ConstruirGrifoPolvo();
        public static AudioClip GrifoGas => _grifoGas ??= ConstruirGrifoGas();
        public static AudioClip Aspirar => _aspirar ??= ConstruirAspirar();
        public static AudioClip Verter => _verter ??= ConstruirVerter();
        public static AudioClip Ignicion => _ignicion ??= ConstruirIgnicion();
        public static AudioClip CristalizarCongelar => _cristalizarCongelar ??= ConstruirCristalizarCongelar();
        public static AudioClip TolvaTraga => _tolvaTraga ??= ConstruirTolvaTraga();
        public static AudioClip Bautizar => _bautizar ??= ConstruirBautizar();
        public static AudioClip EncargoCompletado => _encargoCompletado ??= ConstruirEncargoCompletado();
        public static AudioClip FinDeJornada => _finDeJornada ??= ConstruirFinDeJornada();

        // ===================================================================
        // PRIMITIVAS REUTILIZABLES
        // ===================================================================

        private static float[] NuevoBuffer(float segundos, int sampleRate)
        {
            return new float[Mathf.Max(1, Mathf.RoundToInt(segundos * sampleRate))];
        }

        /// <summary>Ruido blanco crudo en [-1,1]. NUNCA se expone tal cual a un AudioClip final -- ver doc de la clase: siempre pasa por PasoBajo/PasoBajoBarrido antes de sonar.</summary>
        private static void Ruido(float[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = (float)(_rng.NextDouble() * 2.0 - 1.0);
        }

        /// <summary>Filtro paso-bajo de UN POLO (integrador con fuga): la primitiva que convierte ruido blanco crudo en algo cálido y opaco. cutoffHz constante en todo el buffer.</summary>
        private static void PasoBajo(float[] buffer, int sampleRate, float cutoffHz)
        {
            float rc = 1f / (2f * Mathf.PI * Mathf.Max(1f, cutoffHz));
            float dt = 1f / sampleRate;
            float alpha = dt / (rc + dt);
            float y = 0f;
            for (int i = 0; i < buffer.Length; i++)
            {
                y += alpha * (buffer[i] - y);
                buffer[i] = y;
            }
        }

        /// <summary>Igual que <see cref="PasoBajo"/> pero con el corte BARRIDO linealmente entre dos frecuencias a lo largo del buffer -- es lo que da el "whoosh" ascendente/descendente de aspirar/verter y la textura de la ignición.</summary>
        private static void PasoBajoBarrido(float[] buffer, int sampleRate, float cutoffInicioHz, float cutoffFinHz)
        {
            float dt = 1f / sampleRate;
            float y = 0f;
            int n = buffer.Length;
            for (int i = 0; i < n; i++)
            {
                float t = n > 1 ? i / (float)(n - 1) : 0f;
                float cutoff = Mathf.Lerp(cutoffInicioHz, cutoffFinHz, t);
                float rc = 1f / (2f * Mathf.PI * Mathf.Max(1f, cutoff));
                float alpha = dt / (rc + dt);
                y += alpha * (buffer[i] - y);
                buffer[i] = y;
            }
        }

        /// <summary>Suma (no reemplaza) un tono seno de frecuencia fija.</summary>
        private static void Seno(float[] buffer, int sampleRate, float freqHz, float amplitud)
        {
            double incremento = 2.0 * System.Math.PI * freqHz / sampleRate;
            double fase = 0.0;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] += amplitud * (float)System.Math.Sin(fase);
                fase += incremento;
            }
        }

        /// <summary>Seno con frecuencia deslizante (chirp lineal, fase continua): el "fum" grave de la ignición y el retumbo de la tolva nacen de aquí.</summary>
        private static void SenoDeslizante(float[] buffer, int sampleRate, float freqInicioHz, float freqFinHz, float amplitud)
        {
            double fase = 0.0;
            int n = buffer.Length;
            for (int i = 0; i < n; i++)
            {
                float t = n > 1 ? i / (float)(n - 1) : 0f;
                float freq = Mathf.Lerp(freqInicioHz, freqFinHz, t);
                fase += 2.0 * System.Math.PI * freq / sampleRate;
                buffer[i] += amplitud * (float)System.Math.Sin(fase);
            }
        }

        /// <summary>Onda triangular sumada (vía arcoseno de un seno: barata y sin aliasing perceptible a estas frecuencias). Timbre de latón una vez pasada por PasoBajo -- ver ConstruirEncargoCompletado.</summary>
        private static void Triangulo(float[] buffer, int sampleRate, float freqHz, float amplitud)
        {
            double incremento = 2.0 * System.Math.PI * freqHz / sampleRate;
            double fase = 0.0;
            for (int i = 0; i < buffer.Length; i++)
            {
                float s = (float)System.Math.Sin(fase);
                // 2/pi * asin(sin(x)) da un triángulo normalizado en [-1,1].
                buffer[i] += amplitud * (float)(2.0 / System.Math.PI * System.Math.Asin(s));
                fase += incremento;
            }
        }

        /// <summary>Seno con un desafinado suave y aleatorio en cents (capa de presentación, ver doc de la clase): usado para dar cuerpo de "coro" natural a los parciales de campana en vez de sonar a generador de tonos.</summary>
        private static void SenoDesafinado(float[] buffer, int sampleRate, float freqHz, float amplitud, float centavosMax)
        {
            float cents = (float)(_rng.NextDouble() * 2.0 - 1.0) * centavosMax;
            float freqReal = freqHz * Mathf.Pow(2f, cents / 1200f);
            Seno(buffer, sampleRate, freqReal, amplitud);
        }

        /// <summary>Modulación de amplitud lenta (LFO multiplicativo): el burbujeo del grifo de líquido y el drift del de gas.</summary>
        private static void Tremolo(float[] buffer, int sampleRate, float freqHz, float profundidad)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                float lfo = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * freqHz * i / sampleRate);
                buffer[i] *= (1f - profundidad) + profundidad * lfo;
            }
        }

        /// <summary>
        /// Añade `numGranulos` ráfagas cortas de ruido con su propia envolvente
        /// en arco (sin clics), en posiciones aleatorias del buffer: es lo que
        /// da textura "granular" -- el polvo del grifo de arena, los chisporroteos
        /// del fuego, el retumbo grueso de la tolva.
        /// </summary>
        private static void AnadirGranulos(float[] buffer, int sampleRate, int numGranulos, float duracionGranoSeg, float amplitud)
        {
            int n = buffer.Length;
            int granoN = Mathf.Max(2, Mathf.RoundToInt(duracionGranoSeg * sampleRate));
            for (int g = 0; g < numGranulos; g++)
            {
                int maxInicio = Mathf.Max(1, n - granoN);
                int inicio = _rng.Next(0, maxInicio);
                for (int i = 0; i < granoN; i++)
                {
                    int idx = inicio + i;
                    if (idx >= n) break;
                    float t = i / (float)(granoN - 1);
                    float env = Mathf.Sin(Mathf.PI * t); // arco suave: 0 -> 1 -> 0, sin clics en los bordes del grano.
                    float muestra = ((float)_rng.NextDouble() * 2f - 1f) * amplitud * env;
                    buffer[idx] += muestra;
                }
            }
        }

        /// <summary>
        /// Envolvente ADSR simplificada para UN SOLO golpe: ataque lineal,
        /// luego caída en ease-out cuadrático que dura `caidaSeg` (recortada
        /// al hueco real que quede en el buffer) y, si sobra buffer después,
        /// SILENCIO explícito -- así un "soplo breve" de verdad se apaga
        /// antes de que acabe el clip en vez de estirarse para llenarlo.
        /// Garantiza "empieza y acaba en silencio" en cualquier caso.
        /// </summary>
        private static void AplicarEnvolvente(float[] buffer, int sampleRate, float ataqueSeg, float caidaSeg)
        {
            int n = buffer.Length;
            int ataqueN = Mathf.Clamp(Mathf.RoundToInt(ataqueSeg * sampleRate), 1, n);
            for (int i = 0; i < ataqueN; i++)
            {
                buffer[i] *= (i + 1) / (float)ataqueN;
            }
            int caidaInicio = ataqueN;
            int caidaNSolicitada = Mathf.Max(1, Mathf.RoundToInt(caidaSeg * sampleRate));
            int caidaN = Mathf.Min(caidaNSolicitada, n - caidaInicio);
            for (int i = caidaInicio; i < caidaInicio + caidaN; i++)
            {
                float t = caidaN > 1 ? (i - caidaInicio) / (float)(caidaN - 1) : 1f;
                float env = (1f - t) * (1f - t); // ease-out: cae rápido al principio, se apaga del todo al final.
                buffer[i] *= env;
            }
            for (int i = caidaInicio + caidaN; i < n; i++)
            {
                buffer[i] = 0f; // el buffer sobra más de lo que dura la caída pedida: silencio explícito, no residuo.
            }
        }

        /// <summary>
        /// Envolvente de UN PARCIAL de campana: ataque instantáneo + caída
        /// EXPONENCIAL que dura solo `caidaSeg` (no todo el buffer) y deja
        /// silencio puro después -- así los parciales altos, que decaen más
        /// rápido que el fundamental, no rellenan el resto del golpe con
        /// residuo audible.
        /// </summary>
        private static void AplicarEnvolventeParcial(float[] buffer, int sampleRate, float ataqueSeg, float caidaSeg)
        {
            int n = buffer.Length;
            int ataqueN = Mathf.Clamp(Mathf.RoundToInt(ataqueSeg * sampleRate), 1, n);
            int caidaN = Mathf.Clamp(Mathf.RoundToInt(caidaSeg * sampleRate), 1, Mathf.Max(1, n - ataqueN));

            for (int i = 0; i < ataqueN; i++)
            {
                buffer[i] *= (i + 1) / (float)ataqueN;
            }
            int caidaInicio = ataqueN;
            for (int i = caidaInicio; i < caidaInicio + caidaN && i < n; i++)
            {
                float t = caidaN > 1 ? (i - caidaInicio) / (float)(caidaN - 1) : 1f;
                buffer[i] *= Mathf.Exp(-4.2f * t);
            }
            for (int i = caidaInicio + caidaN; i < n; i++)
            {
                buffer[i] = 0f; // ya se apagó de sobra en la exponencial: silencio explícito, no aproximado.
            }
        }

        /// <summary>Normaliza el pico del buffer a `picoObjetivo` (evita clipping al sumar varias capas, y deja margen de mezcla consistente entre clips).</summary>
        private static void Normalizar(float[] buffer, float picoObjetivo)
        {
            float pico = 0f;
            for (int i = 0; i < buffer.Length; i++) pico = Mathf.Max(pico, Mathf.Abs(buffer[i]));
            if (pico < 1e-5f) return;
            float factor = picoObjetivo / pico;
            for (int i = 0; i < buffer.Length; i++) buffer[i] *= factor;
        }

        private static void Clamp(float[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++) buffer[i] = Mathf.Clamp(buffer[i], -1f, 1f);
        }

        /// <summary>destino[i] += origen[i] * peso, sin desfase.</summary>
        private static void Sumar(float[] destino, float[] origen, float peso)
        {
            int n = Mathf.Min(destino.Length, origen.Length);
            for (int i = 0; i < n; i++) destino[i] += origen[i] * peso;
        }

        /// <summary>Igual que <see cref="Sumar"/> pero desplazando `origen` `offsetMuestras` adelante (para el "roll" del acorde de latón: las tres notas no arrancan exactamente a la vez).</summary>
        private static void SumarConDesfase(float[] destino, float[] origen, float peso, int offsetMuestras)
        {
            for (int i = 0; i < origen.Length; i++)
            {
                int idx = i + offsetMuestras;
                if (idx < 0 || idx >= destino.Length) continue;
                destino[idx] += origen[i] * peso;
            }
        }

        /// <summary>
        /// Suaviza el punto de bucle de un buffer de ruido: mezcla las últimas
        /// `crossfadeSamples` muestras (lo que sonaría justo ANTES de volver al
        /// principio) con las primeras, para que el salto de bucle no se oiga
        /// como un clic. Técnica estándar de bucles de ruido.
        /// </summary>
        private static void SuavizarBucle(float[] buffer, int crossfadeSamples)
        {
            int n = buffer.Length;
            crossfadeSamples = Mathf.Clamp(crossfadeSamples, 1, n / 2);
            int colaInicio = n - crossfadeSamples;
            for (int i = 0; i < crossfadeSamples; i++)
            {
                float w = (i + 1) / (float)(crossfadeSamples + 1); // 0..1
                float mezcla = buffer[colaInicio + i] * (1f - w) + buffer[i] * w;
                buffer[i] = mezcla;
                buffer[colaInicio + i] = mezcla;
            }
        }

        private static AudioClip CrearClip(string nombre, float[] datosMono, int sampleRate)
        {
            var clip = AudioClip.Create(nombre, datosMono.Length, 1, sampleRate, false);
            clip.SetData(datosMono, 0);
            return clip;
        }

        // ===================================================================
        // CONSTRUCTORES DE CLIP (uno por sonido pedido en el encargo)
        // ===================================================================

        /// <summary>1) Lecho ambiental (bucle, muy bajo): ruido marrón muy filtrado (dos pasadas de paso-bajo) + zumbido grave de fragua (fundamental + un armónico débil). Es la sala, no un efecto.</summary>
        private static AudioClip ConstruirLechoAmbiental()
        {
            var buf = NuevoBuffer(4.5f, SR_LOOP);
            Ruido(buf);
            PasoBajo(buf, SR_LOOP, 220f);
            PasoBajo(buf, SR_LOOP, 130f); // segunda pasada: opaco de piedra, no aire.

            var zumbido = NuevoBuffer(4.5f, SR_LOOP);
            Seno(zumbido, SR_LOOP, 42f, 1f);
            Seno(zumbido, SR_LOOP, 84f, 0.30f); // armónico débil: evita que suene a tono de laboratorio puro.
            Sumar(buf, zumbido, 0.045f);

            Normalizar(buf, 0.5f);
            SuavizarBucle(buf, Mathf.RoundToInt(0.18f * SR_LOOP));
            return CrearClip("ChaosAlchemy_LechoAmbiental", buf, SR_LOOP);
        }

        /// <summary>3) Fuego (bucle): ruido filtrado + estallidos de amplitud de baja frecuencia (granos), otra pasada de paso-bajo para que los granos no arañen. Volumen/filtro real se automatizan en DirectorDeAudio según cuánto fuego hay.</summary>
        private static AudioClip ConstruirFuegoBucle()
        {
            var buf = NuevoBuffer(2.2f, SR_LOOP);
            Ruido(buf);
            PasoBajo(buf, SR_LOOP, 1400f);
            AnadirGranulos(buf, SR_LOOP, 26, 0.03f, 0.6f);
            PasoBajo(buf, SR_LOOP, 3200f);
            Normalizar(buf, 0.55f);
            SuavizarBucle(buf, Mathf.RoundToInt(0.12f * SR_LOOP));
            return CrearClip("ChaosAlchemy_FuegoBucle", buf, SR_LOOP);
        }

        /// <summary>2a) Grifo de líquido: banda de ruido filtrada de medio, con burbujeo (trémolo lento).</summary>
        private static AudioClip ConstruirGrifoLiquido()
        {
            var buf = NuevoBuffer(1.4f, SR_LOOP);
            Ruido(buf);
            PasoBajo(buf, SR_LOOP, 950f);
            Tremolo(buf, SR_LOOP, 4.2f, 0.30f);
            Normalizar(buf, 0.5f);
            SuavizarBucle(buf, Mathf.RoundToInt(0.10f * SR_LOOP));
            return CrearClip("ChaosAlchemy_GrifoLiquido", buf, SR_LOOP);
        }

        /// <summary>2b) Grifo de polvo: más agudo y granular (corte más alto + granos finos).</summary>
        private static AudioClip ConstruirGrifoPolvo()
        {
            var buf = NuevoBuffer(1.4f, SR_LOOP);
            Ruido(buf);
            PasoBajo(buf, SR_LOOP, 2300f);
            AnadirGranulos(buf, SR_LOOP, 40, 0.012f, 0.35f);
            PasoBajo(buf, SR_LOOP, 3800f); // suaviza los granos recién añadidos, sin perder el grano.
            Normalizar(buf, 0.45f);
            SuavizarBucle(buf, Mathf.RoundToInt(0.10f * SR_LOOP));
            return CrearClip("ChaosAlchemy_GrifoPolvo", buf, SR_LOOP);
        }

        /// <summary>2c) Grifo de gas: un siseo tenue (corte alto pero pico bajo, para que sea discreto por diseño, no solo por volumen de mezcla).</summary>
        private static AudioClip ConstruirGrifoGas()
        {
            var buf = NuevoBuffer(1.4f, SR_LOOP);
            Ruido(buf);
            PasoBajo(buf, SR_LOOP, 3600f);
            Tremolo(buf, SR_LOOP, 1.3f, 0.15f);
            Normalizar(buf, 0.30f);
            SuavizarBucle(buf, Mathf.RoundToInt(0.10f * SR_LOOP));
            return CrearClip("ChaosAlchemy_GrifoGas", buf, SR_LOOP);
        }

        /// <summary>4a) Aspirar: barrido ASCENDENTE (paso-bajo abriéndose de grave a agudo) sobre ruido -- sensación de "entra al frasco".</summary>
        private static AudioClip ConstruirAspirar()
        {
            var buf = NuevoBuffer(0.16f, SR_ONESHOT);
            Ruido(buf);
            PasoBajoBarrido(buf, SR_ONESHOT, 250f, 3200f);
            AplicarEnvolvente(buf, SR_ONESHOT, 0.008f, 0.15f);
            Normalizar(buf, 0.5f);
            return CrearClip("ChaosAlchemy_Aspirar", buf, SR_ONESHOT);
        }

        /// <summary>4b) Verter: barrido DESCENDENTE -- espejo exacto de Aspirar, sensación de "sale del frasco".</summary>
        private static AudioClip ConstruirVerter()
        {
            var buf = NuevoBuffer(0.16f, SR_ONESHOT);
            Ruido(buf);
            PasoBajoBarrido(buf, SR_ONESHOT, 3200f, 250f);
            AplicarEnvolvente(buf, SR_ONESHOT, 0.010f, 0.15f);
            Normalizar(buf, 0.5f);
            return CrearClip("ChaosAlchemy_Verter", buf, SR_ONESHOT);
        }

        /// <summary>5) Ignición: un "fum" grave y corto -- chirrido seno grave que cae de tono + un breve soplo de ruido filtrado descendente por encima, mezclados y normalizados juntos.</summary>
        private static AudioClip ConstruirIgnicion()
        {
            var buf = NuevoBuffer(0.32f, SR_ONESHOT);

            var golpe = NuevoBuffer(0.32f, SR_ONESHOT);
            SenoDeslizante(golpe, SR_ONESHOT, 130f, 55f, 1f);
            AplicarEnvolvente(golpe, SR_ONESHOT, 0.004f, 0.30f);

            var soplo = NuevoBuffer(0.32f, SR_ONESHOT);
            Ruido(soplo);
            PasoBajoBarrido(soplo, SR_ONESHOT, 2600f, 300f);
            AplicarEnvolvente(soplo, SR_ONESHOT, 0.003f, 0.15f);

            Sumar(buf, golpe, 0.9f);
            Sumar(buf, soplo, 0.35f);
            Normalizar(buf, 0.7f);
            Clamp(buf);
            return CrearClip("ChaosAlchemy_Ignicion", buf, SR_ONESHOT);
        }

        /// <summary>
        /// 6) Cristalizar/congelar: campanilla vítrea -- 4 parciales inarmónicos
        /// (ratios propios de campana, no de armónico entero) con desafinado
        /// suave y caída INDEPENDIENTE por parcial (los agudos mueren antes),
        /// suavizada con un paso-bajo final para que sea brillante pero nunca
        /// áspera. Normalizada BAJA a propósito: es el sonido con más riesgo
        /// de dispararse cientos de veces por segundo (ver limitador en
        /// DirectorDeAudio), así que tiene que sobrevivir bien a la repetición.
        /// </summary>
        private static AudioClip ConstruirCristalizarCongelar()
        {
            const float dur = 0.5f;
            var buf = NuevoBuffer(dur, SR_ONESHOT);

            float[] ratios = { 1f, 1.8f, 2.76f, 4.1f };
            float[] pesos = { 1f, 0.55f, 0.32f, 0.18f };
            float[] caidas = { 0.42f, 0.30f, 0.20f, 0.13f };
            const float fundamental = 1250f;

            for (int p = 0; p < ratios.Length; p++)
            {
                var parcial = NuevoBuffer(dur, SR_ONESHOT);
                SenoDesafinado(parcial, SR_ONESHOT, fundamental * ratios[p], 1f, 6f);
                AplicarEnvolventeParcial(parcial, SR_ONESHOT, 0.003f, caidas[p]);
                Sumar(buf, parcial, pesos[p]);
            }

            PasoBajo(buf, SR_ONESHOT, 6500f);
            Normalizar(buf, 0.55f);
            Clamp(buf);
            return CrearClip("ChaosAlchemy_CristalizarCongelar", buf, SR_ONESHOT);
        }

        /// <summary>7) La tolva traga: retumbo grave (chirrido seno descendente) + un puñado de granos de ruido muy filtrados encima, para la textura "granular" pedida.</summary>
        private static AudioClip ConstruirTolvaTraga()
        {
            const float dur = 0.42f;
            var buf = NuevoBuffer(dur, SR_ONESHOT);

            var retumbo = NuevoBuffer(dur, SR_ONESHOT);
            SenoDeslizante(retumbo, SR_ONESHOT, 150f, 65f, 1f);
            AplicarEnvolvente(retumbo, SR_ONESHOT, 0.010f, 0.36f);
            Sumar(buf, retumbo, 0.8f);

            var granos = NuevoBuffer(dur, SR_ONESHOT);
            AnadirGranulos(granos, SR_ONESHOT, 10, 0.02f, 0.9f);
            PasoBajo(granos, SR_ONESHOT, 700f);
            Sumar(buf, granos, 0.5f);

            Normalizar(buf, 0.6f);
            Clamp(buf);
            return CrearClip("ChaosAlchemy_TolvaTraga", buf, SR_ONESHOT);
        }

        /// <summary>8) Bautizar: dos notas cálidas ascendentes (triángulo filtrado), separadas por un hueco silencioso corto -- es el momento de descubrimiento del juego, tiene que sentirse bien.</summary>
        private static AudioClip ConstruirBautizar()
        {
            const float dur1 = 0.22f, hueco = 0.03f, dur2 = 0.26f;
            var buf = NuevoBuffer(dur1 + hueco + dur2, SR_ONESHOT);

            var nota1 = NuevoBuffer(dur1, SR_ONESHOT);
            Triangulo(nota1, SR_ONESHOT, 392.00f, 1f); // G4
            PasoBajo(nota1, SR_ONESHOT, 2600f);
            AplicarEnvolvente(nota1, SR_ONESHOT, 0.015f, dur1 - 0.015f);

            var nota2 = NuevoBuffer(dur2, SR_ONESHOT);
            Triangulo(nota2, SR_ONESHOT, 523.25f, 1f); // C5: cuarta ascendente, resolución cálida.
            PasoBajo(nota2, SR_ONESHOT, 2800f);
            AplicarEnvolvente(nota2, SR_ONESHOT, 0.015f, dur2 - 0.015f);

            SumarConDesfase(buf, nota1, 0.9f, 0);
            SumarConDesfase(buf, nota2, 0.9f, Mathf.RoundToInt((dur1 + hueco) * SR_ONESHOT));

            Normalizar(buf, 0.55f);
            return CrearClip("ChaosAlchemy_Bautizar", buf, SR_ONESHOT);
        }

        /// <summary>9) Encargo completado: acorde breve de tres notas (do-mi-sol) en triángulo filtrado (paso-bajo -&gt; timbre de latón), con un "roll" mínimo entre notas para que no suene a acorde de órgano.</summary>
        private static AudioClip ConstruirEncargoCompletado()
        {
            const float dur = 0.55f;
            var buf = NuevoBuffer(dur, SR_ONESHOT);
            float[] freqs = { 261.63f, 329.63f, 392.00f }; // C4 E4 G4

            for (int i = 0; i < freqs.Length; i++)
            {
                var nota = NuevoBuffer(dur, SR_ONESHOT);
                Triangulo(nota, SR_ONESHOT, freqs[i], 1f);
                PasoBajo(nota, SR_ONESHOT, 1900f); // ablanda los armónicos del triángulo hacia un timbre de metal, no de kazoo.
                AplicarEnvolvente(nota, SR_ONESHOT, 0.015f, dur - 0.015f);
                int offset = Mathf.RoundToInt(i * 0.018f * SR_ONESHOT); // roll de 18ms entre notas.
                SumarConDesfase(buf, nota, 0.55f, offset);
            }

            Normalizar(buf, 0.55f);
            Clamp(buf);
            return CrearClip("ChaosAlchemy_EncargoCompletado", buf, SR_ONESHOT);
        }

        /// <summary>10) Fin de jornada: una campana CON CUERPO -- misma técnica de parciales que CristalizarCongelar pero fundamental más grave, más parciales y caídas más largas, más un "cuerpo" de gong (seno grave sostenido) por debajo.</summary>
        private static AudioClip ConstruirFinDeJornada()
        {
            const float dur = 1.4f;
            var buf = NuevoBuffer(dur, SR_ONESHOT);

            float[] ratios = { 1f, 2f, 3.4f, 4.8f, 6.5f };
            float[] pesos = { 1f, 0.6f, 0.4f, 0.22f, 0.12f };
            float[] caidas = { 1.15f, 0.85f, 0.55f, 0.35f, 0.22f };
            const float fundamental = 262f; // C4: más grave que la campanilla de cristal, para que se lea como "más importante".

            for (int p = 0; p < ratios.Length; p++)
            {
                var parcial = NuevoBuffer(dur, SR_ONESHOT);
                SenoDesafinado(parcial, SR_ONESHOT, fundamental * ratios[p], 1f, 4f);
                AplicarEnvolventeParcial(parcial, SR_ONESHOT, 0.006f, caidas[p]);
                Sumar(buf, parcial, pesos[p]);
            }

            var cuerpo = NuevoBuffer(dur, SR_ONESHOT);
            Seno(cuerpo, SR_ONESHOT, fundamental * 0.5f, 1f);
            AplicarEnvolventeParcial(cuerpo, SR_ONESHOT, 0.02f, 1.2f);
            Sumar(buf, cuerpo, 0.35f);

            PasoBajo(buf, SR_ONESHOT, 5200f);
            Normalizar(buf, 0.65f);
            Clamp(buf);
            return CrearClip("ChaosAlchemy_FinDeJornada", buf, SR_ONESHOT);
        }
    }
}
