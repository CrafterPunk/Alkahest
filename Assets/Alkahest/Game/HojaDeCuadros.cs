using UnityEngine;

namespace Alkahest.Game
{
    /// <summary>
    /// (R118) HOJA DE CUADROS: una animación del muñeco de remiendos hecha
    /// FUERA del editor (arnés de animación: Wan Animate 2 → postproceso.py)
    /// y traída como UNA textura + UN manifiesto JSON en Resources/Personaje/Anim/.
    /// Los sprites se cortan en runtime con Sprite.Create: sin Sprite Editor,
    /// sin Animator, sin .anim — el .meta de la hoja es el mismo que el de la
    /// estampa (talla de la ronda 108) y el manifiesto dice cuántos cuadros,
    /// a qué fps, dónde está el pivote y cuánto mide el personaje de pie en
    /// píxeles. Con eso la hoja se escala SOLA a la talla de la estampa
    /// (<see cref="TallaEstampaU"/> = 1.2 u = 12 celdas): si mañana el arnés
    /// genera a otra resolución, el muñeco sigue midiendo lo mismo en el juego.
    ///
    /// Ausente el asset, <see cref="Cargar"/> devuelve null y el aprendiz sigue
    /// con su estampa quieta (retén honesto, como el imp procedimental).
    /// </summary>
    public sealed class HojaDeCuadros
    {
        /// <summary>Alto del personaje DE PIE en unidades de mundo: el de la estampa (1200 px a 1000 px/u).</summary>
        public const float TallaEstampaU = 1.2f;

        [System.Serializable]
        private class Manifiesto
        {
            public string nombre;
            public int cuadros, columnas, filas, ancho, alto;
            public float fps;
            public bool loop;
            public int intro;
            public int @base;
            public bool pingpong;
            public float pivotX, pivotY;
            public int alturaPersonajePx;
        }

        public readonly string Nombre;
        public readonly Sprite[] Cuadros;
        public readonly float Fps;
        public readonly bool Loop;
        /// <summary>(R118c) Cuántos cuadros iniciales son el ARRANQUE (de la pose de la estampa al ciclo). El ciclo empieza en Cuadros[Intro]. Al parar se tocan al revés.</summary>
        public readonly int Intro;
        /// <summary>(R118f) Índice del cuadro BASE (la pose canónica: pies plantados, brazos abajo). Es lo que se muestra quieto sin hoja de reposo y volando.</summary>
        public readonly int Base;
        /// <summary>(R118f) Ida y vuelta en vez de ciclo: para reposos que no son periódicos (nunca hay corte).</summary>
        public readonly bool PingPong;
        public int CuadrosDelCiclo => Cuadros.Length - Intro;
        public Sprite CuadroBase => Cuadros[Base];

        private HojaDeCuadros(string nombre, Sprite[] cuadros, float fps, bool loop, int intro, int baseIdx, bool pingpong)
        {
            Nombre = nombre; Cuadros = cuadros; Fps = fps; Loop = loop; Intro = Mathf.Clamp(intro, 0, cuadros.Length - 1);
            Base = Mathf.Clamp(baseIdx, 0, cuadros.Length - 1); PingPong = pingpong;
        }

        /// <summary>Cuadro del ciclo para un tiempo t (en cuadros, acumulado): loop o ping-pong según la hoja. Ignora el arranque.</summary>
        public Sprite CuadroDelCiclo(float t)
        {
            int n = Mathf.Max(1, CuadrosDelCiclo);
            if (!PingPong || n < 3) return Cuadros[Intro + ((int)t) % n];
            int periodo = 2 * (n - 1);
            int k = ((int)t) % periodo;
            if (k >= n) k = periodo - k;
            return Cuadros[Intro + k];
        }

        /// <summary>Carga Resources/Personaje/Anim/&lt;nombre&gt;.png + &lt;nombre&gt;_manifiesto.json. Null si falta algo (y lo dice en consola una vez).</summary>
        public static HojaDeCuadros Cargar(string nombre)
        {
            var tex = Resources.Load<Texture2D>("Personaje/Anim/" + nombre);
            var txt = Resources.Load<TextAsset>("Personaje/Anim/" + nombre + "_manifiesto");
            if (tex == null || txt == null)
            {
                Debug.Log($"[TenThousandYears] HojaDeCuadros '{nombre}': sin asset (tex={(tex != null)}, manifiesto={(txt != null)}) — el muñeco se queda con la estampa quieta.");
                return null;
            }
            Manifiesto m;
            try { m = JsonUtility.FromJson<Manifiesto>(txt.text); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TenThousandYears] HojaDeCuadros '{nombre}': manifiesto ilegible ({e.Message}).");
                return null;
            }
            if (m == null || m.cuadros <= 0 || m.ancho <= 0 || m.alto <= 0 || m.alturaPersonajePx <= 0)
            {
                Debug.LogWarning($"[TenThousandYears] HojaDeCuadros '{nombre}': manifiesto incompleto.");
                return null;
            }
            // (R47/50) las unidades las define el consumidor: aquí el consumidor
            // es la talla de la estampa, y la hoja se pliega a ella.
            float ppu = m.alturaPersonajePx / TallaEstampaU;
            // Si Unity redujo la textura (maxTextureSize), los cuadros del
            // manifiesto ya no calzan: escalar rects y ppu por el mismo factor.
            float factor = tex.width / (float)(m.columnas * m.ancho);
            if (Mathf.Abs(factor - 1f) > 0.001f)
            {
                Debug.LogWarning($"[TenThousandYears] HojaDeCuadros '{nombre}': la textura llegó a {tex.width}px de {m.columnas * m.ancho} (factor {factor:0.###}) — revisar maxTextureSize del .meta. Compensado.");
                ppu *= factor;
            }
            float ancho = m.ancho * factor, alto = m.alto * factor;
            var cuadros = new Sprite[m.cuadros];
            var pivote = new Vector2(m.pivotX, m.pivotY);
            for (int i = 0; i < m.cuadros; i++)
            {
                int col = i % m.columnas, fila = i / m.columnas;
                // La hoja se arma de arriba a abajo; la textura de Unity nace abajo-izquierda.
                var rect = new Rect(col * ancho, tex.height - (fila + 1) * alto, ancho, alto);
                cuadros[i] = Sprite.Create(tex, rect, pivote, ppu, 0, SpriteMeshType.FullRect);
                cuadros[i].name = $"{nombre}_{i:00}";
            }
            Debug.Log($"[TenThousandYears] HojaDeCuadros '{nombre}': {m.cuadros} cuadros {m.ancho}x{m.alto} a {m.fps:0.#} fps (arranque {m.intro}), personaje {m.alturaPersonajePx}px → {ppu:0.#} px/u (talla {TallaEstampaU}u).");
            return new HojaDeCuadros(nombre, cuadros, m.fps > 0f ? m.fps : 16f, m.loop, m.intro, m.@base, m.pingpong);
        }
    }
}
