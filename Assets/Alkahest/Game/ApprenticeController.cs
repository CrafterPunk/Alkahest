using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// El aprendiz: un personaje volador (sin plataformeo) que el jugador
    /// mueve libremente por el laboratorio con WASD/flechas. Si no se le
    /// asigna un sprite en el inspector, genera uno procedimentalmente (un
    /// pequeño imp encapuchado) para no depender de assets externos.
    ///
    /// AJUSTES DEL PLAYTEST 4 ("el muñeco y los vuelos largos cansan"):
    ///  · Velocidad +40% (8 -> 11.2 u/s). Cruzar el taller nuevo (25.6 u) pasa
    ///    de 4.8 s a 2.3 s, y ningún salto entre estaciones llega a un segundo.
    ///  · Bobbing a la mitad (0.08 -> 0.04): el cabeceo original, sumado al
    ///    vuelo libre, mareaba y desalineaba la retícula respecto al frasco.
    ///  · Sprite un 15% más pequeño (28 -> 33 px por unidad). Con la grilla a
    ///    256x144 las celdas se ven un 50% más grandes; el aprendiz ya no
    ///    necesita tamaño para leerse y así tapa menos materia.
    /// </summary>
    public sealed class ApprenticeController : MonoBehaviour
    {
        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 11.2f;
        [SerializeField] private float acceleration = 44f; // unidades/s^2 de suavizado hacia la velocidad objetivo (escalado con la velocidad nueva para que el arranque siga siendo igual de nítido)

        [Header("Visual")]
        [SerializeField] private Sprite customSprite; // si se asigna, se usa en vez del sprite generado
        [SerializeField] private int sortingOrder = 50;

        // Límites del mundo, derivados del tamaño real de la grilla de simulación
        // (CellGrid.W/H * SimRenderer.CellWorldSize == 25.6 x 14.4 tras la
        // reingeniería del espacio). Nunca hardcodear: se derivan solos.
        private const float WorldMinX = 0f;
        private const float WorldMinY = 0f;
        private const float WorldMaxX = CellGrid.W * SimRenderer.CellWorldSize;
        private const float WorldMaxY = CellGrid.H * SimRenderer.CellWorldSize;

        private const float BobFrequency = 2.4f;
        private const float BobAmplitude = 0.04f;  // playtest 4: la mitad que antes
        private const float VisualZOffset = -0.05f; // más cerca de la cámara que el quad de la sim (z=0), para quedar siempre por encima.
        private const float SpritePixelsPerUnit = 33f; // playtest 4: ~15% más pequeño (era 28)

        private Vector2 _velocity;
        private bool _facingRight = true;

        private Transform _visualTransform;
        private SpriteRenderer _spriteRenderer;

        /// <summary>Punto ~0.5 unidades por delante/abajo del aprendiz, donde el frasco muestra su contenido.</summary>
        public Vector3 CarryAnchor => transform.position + new Vector3(_facingRight ? 0.28f : -0.28f, -0.35f, 0f);

        private void Awake()
        {
            BuildVisual();
        }

        private void Update()
        {
            HandleMovement();
            HandleVisual();
        }

        private void HandleMovement()
        {
            var kb = Keyboard.current;
            Vector2 input = Vector2.zero;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            }
            if (input.sqrMagnitude > 1f) input.Normalize();

            Vector2 target = input * moveSpeed;
            _velocity = Vector2.MoveTowards(_velocity, target, acceleration * Time.deltaTime);

            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x + _velocity.x * Time.deltaTime, WorldMinX, WorldMaxX);
            pos.y = Mathf.Clamp(pos.y + _velocity.y * Time.deltaTime, WorldMinY, WorldMaxY);
            pos.z = 0f;
            transform.position = pos;

            if (input.x > 0.01f) _facingRight = true;
            else if (input.x < -0.01f) _facingRight = false;
        }

        private void HandleVisual()
        {
            if (_spriteRenderer != null) _spriteRenderer.flipX = !_facingRight;
            if (_visualTransform != null)
            {
                float bob = Mathf.Sin(Time.time * BobFrequency) * BobAmplitude;
                _visualTransform.localPosition = new Vector3(0f, bob, VisualZOffset);
            }
        }

        private void BuildVisual()
        {
            var go = new GameObject("Visual");
            go.transform.SetParent(transform, false);
            _visualTransform = go.transform;

            _spriteRenderer = go.AddComponent<SpriteRenderer>();
            _spriteRenderer.sortingOrder = sortingOrder;

            if (customSprite != null)
            {
                _spriteRenderer.sprite = customSprite;
            }
            else
            {
                var tex = GenerateApprenticeTexture();
                _spriteRenderer.sprite = Sprite.Create(
                    tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), SpritePixelsPerUnit);
            }
        }

        /// <summary>
        /// Dibuja a mano, píxel a píxel, una silueta sencilla de imp
        /// encapuchado: capucha redondeada, sombra de rostro con dos ojos
        /// brillantes, y una pequeña túnica que se estrecha hacia abajo.
        /// </summary>
        private static Texture2D GenerateApprenticeTexture()
        {
            const int w = 24, h = 28;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "AlkahestApprenticeTex",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[w * h];

            Color32 hood = new Color32(0x3A, 0x2B, 0x4F, 255);   // ciruela oscuro
            Color32 shadow = new Color32(0x22, 0x18, 0x30, 255); // sombra del rostro
            Color32 eye = new Color32(0xFF, 0xD8, 0x40, 255);    // amarillo cálido brillante

            float cx = w * 0.5f;
            float hoodCy = h * 0.72f;
            float hoodRx = w * 0.40f, hoodRy = h * 0.34f;
            float shadowCy = h * 0.64f;
            float shadowRx = w * 0.24f, shadowRy = h * 0.22f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color32 c = default; // transparente por defecto

                    bool inHood = y >= h * 0.40f && InEllipse(x, y, cx, hoodCy, hoodRx, hoodRy);

                    // Túnica: trapecio que se estrecha hacia abajo (pies más juntos que hombros).
                    float t = Mathf.InverseLerp(0f, h * 0.42f, y);
                    float robeHalfWidth = Mathf.Lerp(w * 0.16f, w * 0.34f, t);
                    bool inRobe = y < h * 0.46f && Mathf.Abs(x + 0.5f - cx) <= robeHalfWidth;

                    if (inHood || inRobe) c = hood;
                    if (inHood && InEllipse(x, y, cx, shadowCy, shadowRx, shadowRy)) c = shadow;

                    if (InEllipse(x, y, cx - w * 0.14f, shadowCy, w * 0.05f, h * 0.045f)) c = eye;
                    if (InEllipse(x, y, cx + w * 0.14f, shadowCy, w * 0.05f, h * 0.045f)) c = eye;

                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        private static bool InEllipse(float px, float py, float cx, float cy, float rx, float ry)
        {
            float dx = (px + 0.5f - cx) / rx;
            float dy = (py + 0.5f - cy) / ry;
            return dx * dx + dy * dy <= 1f;
        }
    }
}
