using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// [ChaosAlchemy] Fondo del taller (primer pase de presentación M5):
    /// un quad opaco DETRÁS del quad de la simulación con una textura generada:
    /// gradiente vertical cálido (ciruela oscura arriba → casi negro abajo),
    /// viñeta radial y un grano sutil de piedra. Cero assets externos.
    /// </summary>
    public sealed class WorkshopBackdrop : MonoBehaviour
    {
        private const int TexW = 256;
        private const int TexH = 144;

        private void Start()
        {
            var tex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var top = new Color(0.141f, 0.106f, 0.184f);    // ciruela oscura
            var bottom = new Color(0.055f, 0.043f, 0.055f); // casi negro cálido
            var px = new Color32[TexW * TexH];

            for (int y = 0; y < TexH; y++)
            {
                float ty = y / (float)(TexH - 1);
                var row = Color.Lerp(bottom, top, ty);
                for (int x = 0; x < TexW; x++)
                {
                    float nx = x / (float)(TexW - 1) - 0.5f;
                    float ny = ty - 0.55f;
                    float vig = Mathf.Clamp01(1f - (nx * nx * 2.6f + ny * ny * 2.2f));
                    float grain = ((x * 73856093 ^ y * 19349663) & 15) / 255f - 0.03f;
                    var c = row * (0.55f + 0.45f * vig);
                    px[y * TexW + x] = new Color(
                        Mathf.Clamp01(c.r + grain),
                        Mathf.Clamp01(c.g + grain),
                        Mathf.Clamp01(c.b + grain * 0.8f), 1f);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "WorkshopBackdrop";
            Destroy(quad.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.mainTexture = tex;
            quad.GetComponent<MeshRenderer>().material = mat;

            // Mismo tamaño que el mundo de la sim, medio metro por detrás del quad celular.
            float worldW = CellGrid.W * SimRenderer.CellWorldSize;
            float worldH = CellGrid.H * SimRenderer.CellWorldSize;
            quad.transform.position = new Vector3(worldW * 0.5f, worldH * 0.5f, 0.5f);
            quad.transform.localScale = new Vector3(worldW, worldH, 1f);
        }
    }
}
