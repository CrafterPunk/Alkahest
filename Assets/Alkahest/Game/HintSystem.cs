using UnityEngine;

namespace Alkahest.Game
{
    /// <summary>
    /// [ChaosAlchemy] Onboarding suave (feedback del playtest 1: "no me quedó claro
    /// cómo jugar"). Muestra pistas rotatorias abajo-centro SOLO durante los primeros
    /// minutos de juego real (desde el primer desbloqueo de input). Sin estado, sin
    /// tracking de progreso: contenido mínimo que enseña los verbos y el objetivo.
    /// </summary>
    public sealed class HintSystem : MonoBehaviour
    {
        private const int WindowId = 837482;
        private const float SecondsPerHint = 11f;
        private const float TotalSeconds = 150f; // ~2.5 min de pistas y se calla para siempre

        private static readonly string[] Hints =
        {
            "Muévete con WASD · aspira con CLIC IZQUIERDO · vierte con CLIC DERECHO",
            "Pulsa E junto a un GRIFO de la pared izquierda para abrir el caudal",
            "El Maestro paga por EFECTOS, no por recetas: mira los pedidos (arriba a la derecha)",
            "¿\"Algo que arda\"? El aceite arde... y lo vivo también, si lo provocas",
            "Se entrega VERTIENDO en la TOLVA DEL MAESTRO, en el muro derecho →",
            "Las PLACAS bajo las cubas calientan (E las regula) · la piedra azul enfría",
            "Pulsa T para BAUTIZAR una sustancia con tu nombre · J abre tu diario",
        };

        private float _playSeconds;
        private bool _everUnlocked;
        private GUIStyle _style;

        private void Update()
        {
            if (!DayCycle.InputLocked)
            {
                _everUnlocked = true;
                _playSeconds += Time.deltaTime;
            }
        }

        private void OnGUI()
        {
            if (!_everUnlocked || DayCycle.InputLocked) return;
            if (_playSeconds >= TotalSeconds) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                };
                _style.normal.textColor = new Color(1f, 0.92f, 0.75f);
            }

            int i = Mathf.Min((int)(_playSeconds / SecondsPerHint), Hints.Length - 1);
            float w = Mathf.Min(640f, Screen.width - 40f);
            var rect = new Rect((Screen.width - w) * 0.5f, Screen.height - 64f, w, 44f);
            GUI.Box(rect, Hints[i], _style);
        }
    }
}
