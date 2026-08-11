using UnityEngine;

namespace Alkahest.Game
{
    /// <summary>
    /// HUD IMGUI de encargos activos, esquina superior derecha: "Favor: N ★"
    /// más la lista de encargos de la jornada con barra de progreso
    /// (marcados con ✓ si ya están completados). Solo se dibuja durante
    /// Playing (se oculta bajo cualquier overlay de DayCycle).
    /// </summary>
    public sealed class OrdersHud : MonoBehaviour
    {
        private const float WindowWidth = 280f;
        private const float RowHeight = 40f;
        private const float HeaderHeight = 54f;

        private OrderSystem _orderSystem;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(OrderSystem orderSystem)
        {
            _orderSystem = orderSystem;
        }

        private void OnGUI()
        {
            if (_orderSystem == null || DayCycle.InputLocked) return;

            float height = HeaderHeight + _orderSystem.ActiveOrders.Count * RowHeight;
            Rect windowRect = new Rect(Screen.width - WindowWidth - 12f, 12f, WindowWidth, height);
            GUILayout.BeginArea(windowRect, GUI.skin.box);

            GUILayout.Label($"Favor: {_orderSystem.Favor} ★");
            GUILayout.Space(4f);

            var orders = _orderSystem.ActiveOrders;
            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                string desc = Truncate(order.Descripcion, 34);

                if (order.Completado)
                {
                    GUILayout.Label($"✓ {desc}");
                    GUILayout.Space(RowHeight - 20f);
                    continue;
                }

                GUILayout.Label($"{desc}  ({order.Progreso}/{order.MinCells})");
                Rect barOuter = GUILayoutUtility.GetRect(WindowWidth - 24f, 10f);
                GUI.Box(barOuter, GUIContent.none);
                float frac = Mathf.Clamp01((float)order.Progreso / Mathf.Max(1, order.MinCells));
                Rect barInner = new Rect(barOuter.x + 1f, barOuter.y + 1f, (barOuter.width - 2f) * frac, barOuter.height - 2f);
                var prevColor = GUI.color;
                GUI.color = new Color(1f, 0.85f, 0.3f, 1f);
                GUI.DrawTexture(barInner, Texture2D.whiteTexture);
                GUI.color = prevColor;
            }

            GUILayout.EndArea();
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, max - 1) + "…";
        }
    }
}
