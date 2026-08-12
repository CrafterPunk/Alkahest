using UnityEngine;

namespace Alkahest.Game
{
    /// <summary>
    /// HUD de encargos activos (arriba-derecha): Favor actual y su progreso
    /// hacia la meta de victoria, más la lista de encargos de la jornada con
    /// barra de progreso. Solo se dibuja durante Playing.
    ///
    /// REESCRITO tras el playtest 3 ("se corrieron los textos, no se pueden
    /// apreciar"): antes cada encargo se recortaba a 34 caracteres y se dibujaba
    /// dentro de una fila de alto FIJO (40 px), así que la descripción se
    /// cortaba por los dos lados. Ahora el panel MIDE el texto (CalcHeight con
    /// word-wrap real) y crece lo que haga falta: nunca se recorta una frase.
    /// </summary>
    public sealed class OrdersHud : MonoBehaviour
    {
        private OrderSystem _orderSystem;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(OrderSystem orderSystem)
        {
            _orderSystem = orderSystem;
        }

        private void OnGUI()
        {
            if (_orderSystem == null || DayCycle.InputLocked) return;

            UiStyles.Preparar();

            float margen = UiStyles.S(10f);
            float pad = UiStyles.S(9f);
            float ancho = UiStyles.S(320f);
            float interior = ancho - pad * 2f;
            float sangria = UiStyles.S(16f);           // columna del marcador ✓ / •
            float anchoTexto = interior - sangria;
            float anchoRecompensa = UiStyles.S(52f);

            float altoLinea = UiStyles.S(19f);
            float altoBarra = UiStyles.S(10f);
            float altoBarraFavor = UiStyles.S(6f);

            var orders = _orderSystem.ActiveOrders;

            // ---- 1) Medir (nada de alturas fijas: el texto manda) ----
            float alto = pad
                       + altoLinea                                   // "ENCARGOS DEL MAESTRO"
                       + UiStyles.S(2f) + altoLinea                  // "Favor  N ★"
                       + UiStyles.S(3f) + altoBarraFavor
                       + UiStyles.S(8f);
            for (int i = 0; i < orders.Count; i++)
            {
                alto += UiStyles.Alto(UiStyles.Cuerpo, orders[i].Descripcion, anchoTexto);
                alto += UiStyles.S(4f) + altoLinea + UiStyles.S(9f);
            }
            // El último encargo ya dejó S(9) de aire debajo: no lo duplicamos.
            alto += orders.Count > 0 ? pad - UiStyles.S(9f) : pad;

            var panel = new Rect(Screen.width - ancho - margen, margen, ancho, alto);
            UiStyles.Panel(panel);

            // ---- 2) Cabecera ----
            float x = panel.x + pad;
            float y = panel.y + pad;

            GUI.Label(new Rect(x, y, interior, altoLinea), "ENCARGOS DEL MAESTRO", UiStyles.Titulo);
            y += altoLinea + UiStyles.S(2f);

            GUI.Label(new Rect(x, y, interior, altoLinea), "Favor", UiStyles.CuerpoTenue);
            GUI.Label(new Rect(x, y, interior, altoLinea),
                _orderSystem.Favor + " ★  (meta " + OrderSystem.WinFavorTarget + ")", UiStyles.Numero);
            y += altoLinea + UiStyles.S(3f);

            UiStyles.Barra(new Rect(x, y, interior, altoBarraFavor),
                (float)_orderSystem.Favor / OrderSystem.WinFavorTarget, UiStyles.Oro);
            y += altoBarraFavor + UiStyles.S(8f);

            // ---- 3) Encargos ----
            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                bool hecho = order.Completado;

                float altoDesc = UiStyles.Alto(UiStyles.Cuerpo, order.Descripcion, anchoTexto);

                // Glifos limitados a los que ya usa el resto del juego (✓ • ★):
                // la fuente por defecto de Unity no garantiza nada más exótico.
                GUI.Label(new Rect(x, y, sangria, altoLinea), hecho ? "✓" : "•",
                    hecho ? UiStyles.CuerpoTenue : UiStyles.Cuerpo);
                GUI.Label(new Rect(x + sangria, y, anchoTexto, altoDesc), order.Descripcion,
                    hecho ? UiStyles.CuerpoTenue : UiStyles.Cuerpo);
                y += altoDesc + UiStyles.S(4f);

                // Fila de progreso: barra + "12/60" + recompensa, cada cosa en su
                // columna (nada de texto encima de la barra: era ilegible).
                float anchoProgreso = UiStyles.S(58f);
                float anchoBarra = anchoTexto - anchoProgreso - anchoRecompensa;
                float frac = hecho ? 1f : Mathf.Clamp01((float)order.Progreso / Mathf.Max(1, order.MinCells));

                UiStyles.Barra(new Rect(x + sangria, y + (altoLinea - altoBarra) * 0.5f, anchoBarra, altoBarra),
                    frac, hecho ? UiStyles.Exito : UiStyles.Oro);
                GUI.Label(new Rect(x + sangria + anchoBarra, y, anchoProgreso - UiStyles.S(6f), altoLinea),
                    hecho ? "hecho" : order.Progreso + "/" + order.MinCells, UiStyles.CuerpoDer);
                GUI.Label(new Rect(x + sangria + anchoBarra + anchoProgreso, y, anchoRecompensa, altoLinea),
                    "+" + order.Recompensa + " ★", UiStyles.Numero);

                y += altoLinea + UiStyles.S(9f);
            }
        }
    }
}
