using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// (R131, hito H2) PRESETS Y SNAPSHOTS DEL LABORATORIO — el disco del
    /// laboratorio. Un PRESET es el estado de los 85 números de
    /// <see cref="LabParams"/> con nombre, fecha y nota; un SNAPSHOT es un
    /// preset MÁS la foto de lo que se veía y el libro mayor de ese instante,
    /// para que una medida pueda repetirse y contarse meses después.
    ///
    /// Todo vive en <c>Laboratorio/presets/</c> (fuera de Assets/, hermana de
    /// Galeria/): son datos del experimento, no assets del juego, y así Unity
    /// ni los importa. Herramienta de investigación, solo en ModoLaboratorio.
    ///
    /// JSON A MANO Y A PROPÓSITO: <c>JsonUtility</c> no serializa diccionarios
    /// y el formato del handoff es un mapa clave→número. Son 40 líneas de
    /// escritor y lector para un formato que un humano puede editar en el
    /// bloc de notas y versionar en git — que es justo lo que se le pide a un
    /// preset de laboratorio. El lector es TOLERANTE: una clave que ya no
    /// existe se ignora y una que falta se queda como está (así un preset
    /// viejo sigue cargando cuando se añade un parámetro nuevo), y lo cuenta.
    ///
    /// Regla 56: cero API de Unity en inicializadores estáticos — las rutas se
    /// resuelven en la primera llamada, no al cargar el tipo.
    /// </summary>
    public static class LabPresets
    {
        /// <summary>Nombre del preset que el panel escribe al arrancar con los valores de fábrica.</summary>
        public const string NombreDefaults = "_defaults";
        /// <summary>Lo último que pasó, para el pie del panel.</summary>
        public static string UltimoMensaje = "";

        private static string _carpeta;

        /// <summary>`Laboratorio/presets/` junto al proyecto (dataPath = .../Assets).</summary>
        public static string Carpeta
        {
            get
            {
                if (_carpeta == null)
                {
                    string raiz = Path.GetDirectoryName(Application.dataPath);
                    _carpeta = Path.Combine(raiz, "Laboratorio", "presets");
                }
                if (!Directory.Exists(_carpeta)) Directory.CreateDirectory(_carpeta);
                return _carpeta;
            }
        }

        public static string Ruta(string nombre) => Path.Combine(Carpeta, Sanear(nombre) + ".json");

        /// <summary>Nombres de preset disponibles, ordenados (los que empiezan por «_» al final: son los del sistema).</summary>
        public static List<string> Listar()
        {
            var lista = new List<string>();
            foreach (var f in Directory.GetFiles(Carpeta, "*.json"))
            {
                string n = Path.GetFileNameWithoutExtension(f);
                if (n.EndsWith("_libro", StringComparison.Ordinal)) continue; // el libro de un snapshot no es un preset.
                lista.Add(n);
            }
            lista.Sort((a, b) =>
            {
                bool sa = a.StartsWith("_", StringComparison.Ordinal), sb = b.StartsWith("_", StringComparison.Ordinal);
                if (sa != sb) return sa ? 1 : -1;
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            });
            return lista;
        }

        // =================================================================
        // GUARDAR
        // =================================================================

        /// <summary>Escribe el estado actual del registro como preset. Devuelve la ruta.</summary>
        public static string Guardar(string nombre, string nota)
        {
            var sb = new StringBuilder(4096);
            sb.Append("{\n");
            Campo(sb, "nombre", nombre); sb.Append(",\n");
            Campo(sb, "fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)); sb.Append(",\n");
            Campo(sb, "nota", nota ?? ""); sb.Append(",\n");
            sb.Append("  \"params\": {\n");
            var reg = LabParams.Registro;
            for (int i = 0; i < reg.Count; i++)
            {
                sb.Append("    ").Append(Cadena(reg[i].Clave)).Append(": ")
                  .Append(reg[i].Leer().ToString("0.####", CultureInfo.InvariantCulture));
                if (i < reg.Count - 1) sb.Append(',');
                sb.Append('\n');
            }
            sb.Append("  }\n}\n");
            string ruta = Ruta(nombre);
            File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(false));
            UltimoMensaje = "guardado «" + Sanear(nombre) + "» (" + reg.Count + " parámetros)";
            return ruta;
        }

        /// <summary>
        /// Escribe `_defaults.json` con los valores de FÁBRICA (no los actuales) si aún no existe
        /// —o si el registro ha crecido desde la última vez.
        /// (R138, R17) Antes solo miraba si el archivo existía, así que un `_defaults.json` viejo
        /// se quedaba para siempre: seguía diciendo `vidaHumo` 400 y no conocía
        /// `fuego.rendimientoCarbonPct`. El archivo que dice cuáles son los valores de fábrica no
        /// puede ser el único que no se entera de que la fábrica cambió.
        /// </summary>
        public static void EscribirDefaultsSiFalta()
        {
            string ruta = Ruta(NombreDefaults);
            if (File.Exists(ruta) && MismasClaves(ruta)) return;
            var reg = LabParams.Registro;
            var actuales = new float[reg.Count];
            for (int i = 0; i < reg.Count; i++) { actuales[i] = reg[i].Leer(); reg[i].Escribir(reg[i].Def); }
            Guardar(NombreDefaults, "valores de fábrica del laboratorio (los escribe el panel al arrancar; no lo edites a mano)");
            for (int i = 0; i < reg.Count; i++) reg[i].Escribir(actuales[i]); // no tocar la sesión en curso.
            UltimoMensaje = "escrito _defaults.json";
        }

        /// <summary>
        /// (R142, R19-4) ¿El archivo tiene EXACTAMENTE las claves del registro de hoy?
        /// Contar cuántas hay no basta: un parámetro renombrado deja el mismo número y el archivo
        /// viejo se quedaba para siempre, describiendo una fábrica que ya no existe.
        /// </summary>
        private static bool MismasClaves(string ruta)
        {
            try
            {
                var d = Parsear(File.ReadAllText(ruta));
                if (d == null || d.Count != LabParams.Registro.Count) return false;
                foreach (var p in LabParams.Registro) if (!d.ContainsKey(p.Clave)) return false;
                return true;
            }
            catch { return false; }
        }

        // =================================================================
        // CARGAR
        // =================================================================

        /// <summary>Lee el bloque «params» de un preset. null si el archivo no está.</summary>
        public static Dictionary<string, float> LeerParams(string nombre)
        {
            string ruta = Ruta(nombre);
            if (!File.Exists(ruta)) return null;
            return Parsear(File.ReadAllText(ruta));
        }

        /// <summary>Aplica un preset al registro. Devuelve cuántas claves se aplicaron; `desconocidas`/`ausentes` cuentan el desfase de versión.</summary>
        public static int Cargar(string nombre, out int desconocidas, out int ausentes)
        {
            desconocidas = 0; ausentes = 0;
            var vals = LeerParams(nombre);
            if (vals == null) { UltimoMensaje = "no encuentro «" + nombre + "»"; return 0; }
            int aplicados = 0;
            foreach (var kv in vals)
            {
                var p = LabParams.Buscar(kv.Key);
                if (p == null) { desconocidas++; continue; }
                float v = Mathf.Clamp(kv.Value, p.Min, p.Max);
                if (p.Entero) v = Mathf.Round(v);
                p.Escribir(v);
                aplicados++;
            }
            foreach (var p in LabParams.Registro) if (!vals.ContainsKey(p.Clave)) ausentes++;
            UltimoMensaje = "cargado «" + nombre + "»: " + aplicados + " aplicados"
                + (desconocidas > 0 ? ", " + desconocidas + " que ya no existen" : "")
                + (ausentes > 0 ? ", " + ausentes + " sin valor (se quedan como estaban)" : "");
            return aplicados;
        }

        // =================================================================
        // COMPARAR
        // =================================================================

        public struct Diferencia
        {
            public string Clave, Nombre, Unidad;
            public float Actual, Otro, Def;
            public bool HayOtro;
        }

        /// <summary>
        /// Los parámetros que hoy NO valen su default, y qué valor tenían en
        /// `contra` (si se pasa un preset). Es la respuesta a la única pregunta
        /// que se hace de verdad delante del panel: ¿qué he tocado yo?
        /// </summary>
        public static List<Diferencia> Comparar(string contra)
        {
            Dictionary<string, float> otros = string.IsNullOrEmpty(contra) ? null : LeerParams(contra);
            var res = new List<Diferencia>();
            foreach (var p in LabParams.Registro)
            {
                float actual = p.Leer();
                bool difDef = Mathf.Abs(actual - p.Def) > 1e-4f;
                float otro = 0f; bool hayOtro = false;
                if (otros != null && otros.TryGetValue(p.Clave, out otro)) hayOtro = true;
                bool difOtro = hayOtro && Mathf.Abs(actual - otro) > 1e-4f;
                if (!difDef && !difOtro) continue;
                res.Add(new Diferencia { Clave = p.Clave, Nombre = p.Nombre, Unidad = p.Unidad, Actual = actual, Otro = otro, Def = p.Def, HayOtro = hayOtro });
            }
            return res;
        }

        // =================================================================
        // SNAPSHOT = preset + PNG + libro
        // =================================================================

        /// <summary>
        /// Un botón, un nombre: deja `<nombre>.json` (el preset),
        /// `<nombre>.png` (lo que se veía) y `<nombre>_libro.json` (censo,
        /// libro mayor, tick y dónde estaba el muñeco). Devuelve la carpeta.
        /// </summary>
        public static string GuardarSnapshot(string nombre, string nota, AlkahestSim sim, Transform aprendiz)
        {
            string n = Sanear(nombre);
            Guardar(n, nota);
            Capturar(Path.Combine(Carpeta, n + ".png"));
            EscribirLibro(Path.Combine(Carpeta, n + "_libro.json"), n, sim, aprendiz);
            UltimoMensaje = "snapshot «" + n + "»: preset + png + libro";
            return Carpeta;
        }

        private static void EscribirLibro(string ruta, string nombre, AlkahestSim sim, Transform aprendiz)
        {
            var g = sim != null ? sim.Grid : null;
            var st = sim != null ? sim.Stepper : null;
            if (g == null || st == null) return;

            // Censo por material (solo lo que existe) e inventario de humedad.
            var censo = new int[MaterialId.Count];
            long invAgua = 0, invAire = 0, invPoroso = 0, invRoca = 0;
            for (int i = 0; i < g.mat.Length; i++)
            {
                byte m = g.mat[i];
                censo[m]++;
                int h = g.humedad[i];
                if (h == 0) continue;
                if (m == MaterialId.Water) invAgua += h;
                else if (m == MaterialId.Empty || LabMateriales.EsGasId(m)) invAire += h;
                else if (LabMateriales.EsPoroso(m)) invPoroso += h;
                else invRoca += h;
            }

            var sb = new StringBuilder(4096);
            sb.Append("{\n");
            Campo(sb, "nombre", nombre); sb.Append(",\n");
            Campo(sb, "fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)); sb.Append(",\n");
            sb.Append("  \"tick\": ").Append(st.Tick).Append(",\n");
            sb.Append("  \"multiplicador\": ").Append(sim.LabMultiplicador).Append(",\n");
            if (aprendiz != null)
            {
                float c = SimRenderer.CellWorldSize;
                sb.Append("  \"munheco\": { \"x\": ").Append(Mathf.RoundToInt(aprendiz.position.x / c))
                  .Append(", \"y\": ").Append(Mathf.RoundToInt(aprendiz.position.y / c)).Append(" },\n");
            }
            sb.Append("  \"libro\": {\n");
            Num(sb, "aguaEmitida", st.LabAguaEmitida, true);
            Num(sb, "aguaSumida", st.LabAguaSumida, true);
            Num(sb, "aguaSumidaU", st.LabAguaSumidaU, true);
            Num(sb, "evaporadoU", st.LabEvaporado, true);
            Num(sb, "condensadoU", st.LabCondensado, true);
            Num(sb, "goteos", st.LabGoteos, true);
            Num(sb, "infiltradoU", st.LabInfiltrado, true);
            Num(sb, "exudado", st.LabExudado, true);
            Num(sb, "depositado", st.LabDepositado, true);
            Num(sb, "erosionado", st.LabErosionado, true);
            Num(sb, "compactado", st.LabCompactado, true);
            Num(sb, "ablandado", st.LabAblandado, true);
            Num(sb, "cocido", st.LabCocido, true);
            Num(sb, "abonado", st.LabAbonado, true);
            Num(sb, "plantasNacidas", st.LabPlantasNacidas, true);
            Num(sb, "plantasMuertas", st.LabPlantasMuertas, true);
            Num(sb, "presionMovidas", st.LabPresionMovidas, true);
            Num(sb, "cuerposCaidos", st.LabCuerposCaidos, true);
            Num(sb, "fracturas", st.LabFracturas, true);
            Num(sb, "vidrio", st.LabVidrio, true);
            // (R138, R17) El fuego también va al snapshot: sin esto, un banco guardado no podía
            // reproducir ni la identidad de la carbonera ni el calor entregado.
            Num(sb, "combustibleQuemado", st.LabCombustibleQuemado, true);
            Num(sb, "combustibleCarbon", st.LabCombustibleCarbon, true);
            Num(sb, "unidadesRespiradas", st.LabUnidadesRespiradas, true);
            Num(sb, "calorFuego", st.LabCalorFuego, true);
            Num(sb, "calorCarbon", st.LabCalorCarbon, true);
            Num(sb, "calorLlamaNominal", st.LabCalorLlama, true);
            Num(sb, "calorNoSoltado", st.LabCalorNoSoltado, true);
            Num(sb, "reservaApagada", st.LabReservaApagada, true); // (R145, R23-10) la spec decía «snapshot incluido» y faltaba.
            Num(sb, "carbonizado", st.LabCarbonizado, true);
            Num(sb, "energiaCarbon", st.LabEnergiaCarbon, true);
            Num(sb, "rawFuego", st.LabRawFuego, true);
            Num(sb, "rawLlama", st.LabRawLlama, true);
            Num(sb, "rawBrasa", st.LabRawBrasa, true);
            Num(sb, "rawHogar", st.LabRawHogar, true);
            Num(sb, "rawFrio", st.LabRawFrio, true);
            Num(sb, "balanceU", st.LabBalanceU, false);
            sb.Append("  },\n");
            sb.Append("  \"inventarioU\": { \"agua\": ").Append(invAgua)
              .Append(", \"aire\": ").Append(invAire)
              .Append(", \"poroso\": ").Append(invPoroso)
              .Append(", \"roca\": ").Append(invRoca)
              .Append(", \"total\": ").Append(invAgua + invAire + invPoroso + invRoca).Append(" },\n");
            sb.Append("  \"censo\": {\n");
            bool primero = true;
            for (int m = 0; m < censo.Length; m++)
            {
                if (censo[m] == 0) continue;
                if (!primero) sb.Append(",\n");
                primero = false;
                var def = sim.Universe != null ? sim.Universe.Get((byte)m) : null;
                string dev = def != null && !string.IsNullOrEmpty(def.devName) ? def.devName : ("mat" + m);
                sb.Append("    ").Append(Cadena(dev)).Append(": ").Append(censo[m]);
            }
            sb.Append("\n  }\n}\n");
            File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(false));
        }

        /// <summary>Misma receta que GaleriaCurador.Capturar (URP: SubmitRenderRequest, con el Camera.Render de respaldo).</summary>
        private static void Capturar(string ruta)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var peticion = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, peticion)) RenderPipeline.SubmitRenderRequest(cam, peticion);
            else { var prevT = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = prevT; }
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }

        // =================================================================
        // JSON mínimo
        // =================================================================

        private static void Campo(StringBuilder sb, string clave, string valor)
        {
            sb.Append("  ").Append(Cadena(clave)).Append(": ").Append(Cadena(valor));
        }

        private static void Num(StringBuilder sb, string clave, long v, bool coma)
        {
            sb.Append("    ").Append(Cadena(clave)).Append(": ").Append(v);
            sb.Append(coma ? ",\n" : "\n");
        }

        private static string Cadena(string s)
        {
            var sb = new StringBuilder(s.Length + 8);
            sb.Append('"');
            foreach (char c in s)
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') { }
                else if (c == '\t') sb.Append("\\t");
                else sb.Append(c);
            }
            sb.Append('"');
            return sb.ToString();
        }

        /// <summary>
        /// Lee los pares «"clave": número» que hay DENTRO del bloque «params».
        /// No es un parser de JSON general y no pretende serlo: el formato lo
        /// escribe esta misma clase y lo único que puede tocar un humano son
        /// los números. Si el bloque no está, devuelve un mapa vacío.
        /// </summary>
        private static Dictionary<string, float> Parsear(string texto)
        {
            var res = new Dictionary<string, float>();
            int i = texto.IndexOf("\"params\"", StringComparison.Ordinal);
            if (i < 0) return res;
            i = texto.IndexOf('{', i);
            if (i < 0) return res;
            int fin = texto.IndexOf('}', i);
            if (fin < 0) fin = texto.Length;
            int p = i + 1;
            while (p < fin)
            {
                int a = texto.IndexOf('"', p);
                if (a < 0 || a >= fin) break;
                int b = texto.IndexOf('"', a + 1);
                if (b < 0 || b >= fin) break;
                string clave = texto.Substring(a + 1, b - a - 1);
                int dosPuntos = texto.IndexOf(':', b);
                if (dosPuntos < 0 || dosPuntos >= fin) break;
                int c = dosPuntos + 1;
                while (c < fin && (texto[c] == ' ' || texto[c] == '\t')) c++;
                int d = c;
                while (d < fin && texto[d] != ',' && texto[d] != '\n' && texto[d] != '}') d++;
                string num = texto.Substring(c, d - c).Trim();
                float v;
                if (float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) res[clave] = v;
                p = d + 1;
            }
            return res;
        }

        /// <summary>Un nombre de preset es un nombre de archivo: fuera lo que no puede serlo.</summary>
        public static string Sanear(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "sin_nombre";
            var sb = new StringBuilder(nombre.Length);
            foreach (char c in nombre.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.') sb.Append(c);
                else if (c == ' ') sb.Append('_');
            }
            string s = sb.ToString();
            if (s.Length == 0) return "sin_nombre";
            if (s.Length > 48) s = s.Substring(0, 48);
            return s;
        }
    }
}
