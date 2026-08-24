using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Alkahest.Game;
using Alkahest.Sim;

namespace Alkahest.EditorTools
{
    /// <summary>
    /// (RONDA 75 — LA ESCENIFICACIÓN) EL HORNEADOR DEL PRÓLOGO: baja a
    /// ASSETS lo que hasta ahora solo existía como píxeles generados en
    /// runtime, para que Cesar y su hermano lo VEAN y lo RETOQUEN desde el
    /// editor:
    ///
    ///  · PNGs (SIEMPRE se reescriben desde el código — son el horneado de
    ///    las fábricas procedurales; si alguien retoca un PNG a mano en un
    ///    editor de imagen, que lo renombre o no vuelva a hornear):
    ///      Arte/Prologo/Maestro.png       (la silueta encapuchada)
    ///      Arte/Prologo/TanqueMarco.png   (el armazón del depósito)
    ///      Arte/Prologo/TanqueFondo.png   (su cavidad)
    ///      Arte/Prologo/RuinaFondo.png    (el telón de la ruina, 2304x864)
    ///  · DepositoVisual.prefab — SOLO si no existe (las ediciones de un
    ///    prefab son sagradas: ahí es donde se retoca la piel del tanque).
    ///  · GuionDelPrologo.asset — SOLO si no existe (ahí viven los textos y
    ///    números editables; ver Game/GuionDelPrologo.cs).
    ///
    /// Al final valida la escena Lab (menú 1) y ata los assets recién
    /// horneados a los campos que sigan VACÍOS — jamás pisa una referencia
    /// ya asignada. Correr este menú es idempotente y seguro.
    /// </summary>
    public static class PrologoBakeTools
    {
        private const string CarpetaArte = "Assets/Alkahest/Arte";
        private const string Carpeta = "Assets/Alkahest/Arte/Prologo";

        [MenuItem("Ten Thousand Years/6. Hornear arte del prologo (PNG + prefab + guion)", priority = 6)]
        public static void HornearPrologo()
        {
            AsegurarCarpetas();
            float c = SimRenderer.CellWorldSize;

            // 1) LOS PNG (desde las mismas fábricas que usa el runtime).
            var maestroSprite = EscribirSprite(FundacionDirector.ConstruirTexturaMaestro(),
                Carpeta + "/Maestro.png", 20f, new Vector2(0.5f, 0f));

            var marcoSprite = EscribirSprite(MaquinariaSprites.TanqueMarco(10, 19).texture,
                Carpeta + "/TanqueMarco.png", 60f, new Vector2(0.5f, 0.5f));

            var fondoSprite = EscribirSprite(MaquinariaSprites.TanqueFondo(6, 13).texture,
                Carpeta + "/TanqueFondo.png", 60f, new Vector2(0.5f, 0.5f));

            var ruinaTex = new Texture2D(WorkshopBackdrop.RuinaTexW, WorkshopBackdrop.RuinaTexH, TextureFormat.RGBA32, false);
            var px = new Color32[WorkshopBackdrop.RuinaTexW * WorkshopBackdrop.RuinaTexH];
            for (int y = 0; y < WorkshopBackdrop.RuinaTexH; y++)
                WorkshopBackdrop.PintarFilaRuina(px, y);
            ruinaTex.SetPixels32(px);
            ruinaTex.Apply(false, false);
            EscribirSprite(ruinaTex, Carpeta + "/RuinaFondo.png",
                WorkshopBackdrop.RuinaEscala / SimRenderer.CellWorldSize * 1f, Vector2.zero); // téxeles por celda / mundo por celda = px por unidad (30).
            Object.DestroyImmediate(ruinaTex);

            // 2) EL PREFAB DEL DEPÓSITO (solo si falta: sus ediciones mandan).
            string prefabPath = Carpeta + "/DepositoVisual.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                var root = new GameObject("DepositoVisual");
                var fondo = new GameObject("Fondo");
                fondo.transform.SetParent(root.transform, false);
                fondo.transform.localPosition = new Vector3(0f, 6.5f * c, 0f);
                var srF = fondo.AddComponent<SpriteRenderer>();
                srF.sprite = fondoSprite;
                srF.sortingOrder = Capas.MaquinaFondoInterior;

                var marco = new GameObject("Marco");
                marco.transform.SetParent(root.transform, false);
                marco.transform.localPosition = new Vector3(0f, 9.5f * c, 0f);
                var srM = marco.AddComponent<SpriteRenderer>();
                srM.sprite = marcoSprite;
                srM.sortingOrder = Capas.MaquinaFrente; // en juego, el director lo baja mientras el tanque emerge.

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
                Debug.Log("[TenThousandYears] Prefab creado: " + prefabPath);
            }

            // 3) EL GUION (solo si falta: sus valores son la autoridad).
            string guionPath = Carpeta + "/GuionDelPrologo.asset";
            if (AssetDatabase.LoadAssetAtPath<GuionDelPrologo>(guionPath) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GuionDelPrologo>(), guionPath);
                Debug.Log("[TenThousandYears] Guion creado: " + guionPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4) VALIDAR LA ESCENA y atar lo horneado a los campos vacíos.
            AlkahestSceneBuilder.GenerateLabScene();

            // 5) El Maestro VISIBLE en el editor: si su marcador aún no tiene
            // visual, se le cuelga la silueta horneada (hijo "Silueta"). En
            // juego, el director la respeta y no pinta la suya.
            var esc = Object.FindAnyObjectByType<PrologoEscenografia>();
            if (esc != null && esc.maestro != null && esc.maestro.GetComponentInChildren<SpriteRenderer>() == null && maestroSprite != null)
            {
                var vis = new GameObject("Silueta");
                vis.transform.SetParent(esc.maestro, false);
                var sr = vis.AddComponent<SpriteRenderer>();
                sr.sprite = maestroSprite;
                sr.sortingOrder = 30;
                EditorUtility.SetDirty(esc.maestro.gameObject);
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log("[TenThousandYears] HORNEADO COMPLETO: arte en " + Carpeta + ", escena validada y atada. Retoca marcadores/prefab/guion desde el editor; Play los respeta.");
        }

        private static void AsegurarCarpetas()
        {
            if (!AssetDatabase.IsValidFolder(CarpetaArte)) AssetDatabase.CreateFolder("Assets/Alkahest", "Arte");
            if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder(CarpetaArte, "Prologo");
        }

        /// <summary>Escribe la textura como PNG con importador de pixel-art (Point, sin compresión, PPU y pivote dados) y devuelve el Sprite importado.</summary>
        private static Sprite EscribirSprite(Texture2D tex, string path, float ppu, Vector2 pivot)
        {
            if (tex == null) { Debug.LogError("[TenThousandYears] Textura nula para " + path); return null; }
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.spritePixelsPerUnit = ppu;
            ti.maxTextureSize = 4096; // el telón de la ruina mide 2304 de ancho.
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
