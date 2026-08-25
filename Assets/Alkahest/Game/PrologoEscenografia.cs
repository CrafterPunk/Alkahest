using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// (RONDA 75 — LA ESCENIFICACIÓN) LA ESCENOGRAFÍA DEL PRÓLOGO: el punto
    /// de contacto entre la ESCENA (lo que Cesar y su hermano tocan con el
    /// ratón) y el CÓDIGO (que dirige el prólogo). Vive como objeto de la
    /// escena AlkahestLab — el generador lo crea si falta y NUNCA lo pisa si
    /// ya existe (los generadores validan, no arrasan: ronda 75).
    ///
    /// LA MATRIZ DE AUTORIDAD (quién manda sobre qué — el corazón de la
    /// arquitectura híbrida; la copia narrativa vive en docs/ESTADO.md):
    ///  · ESCENA (este componente y sus hijos):
    ///     - marcador MAESTRO: posición y escala de la silueta Y el punto de
    ///       los triggers de proximidad (VEN., TOMA., entregas). Mover el
    ///       marcador mueve al Maestro entero, visual y jugable.
    ///     - marcador DEPÓSITO: dónde emerge el tanque (se ajusta a la
    ///       grilla de celdas al arrancar).
    ///     - el objeto WorkshopBackdrop de la escena (posición/tinte del
    ///       fondo, y su sprite horneado si existe).
    ///  · ASSET (<see cref="GuionDelPrologo"/>): textos, cantidades,
    ///    tiempos, triggers de distancia, radios de luz, caudales, layout de
    ///    la UI.
    ///  · CÓDIGO: los beats y su orden, la sim entera, el plano tallado
    ///    (cascada/poza/cráter/cuenco — SimLevelBuilder es la verdad), la
    ///    química, el networking. El código LEE la escena; jamás escribe en
    ///    los marcadores.
    ///
    /// Si un marcador falta (escena vieja, sandbox), el código cae a las
    /// constantes históricas de SimLevelBuilder: el juego nunca se rompe por
    /// una escena incompleta — la escenificación es REVERSIBLE.
    /// </summary>
    public sealed class PrologoEscenografia : MonoBehaviour
    {
        [Tooltip("El guion completo del prólogo (textos, cantidades, tiempos). Crear vía Assets > Create > Ten Thousand Years, o con el menú 6 de hornear.")]
        public GuionDelPrologo guion;

        [Header("Marcadores (mover/escalar EN LA ESCENA)")]
        [Tooltip("Posición y escala del Maestro: silueta + triggers de proximidad. Hijo con SpriteRenderer = su visual (horneado); sin hijo, el código genera la silueta.")]
        public Transform maestro;
        [Tooltip("Dónde emerge el depósito de agua (base del tanque; se ajusta a celdas al arrancar).")]
        public Transform deposito;
        [Tooltip("(R83, capítulo 2) Dónde emerge el SILO del lodo (base-centro, huella 6x9). Sin marcador: el hueco medido entre poza y cráter (x386-391).")]
        public Transform deposito2;

        [Header("Arte horneado (opcional — si falta, el código lo genera igual)")]
        [Tooltip("Prefab visual del tanque (hijos: 'Fondo' detrás de la sim, 'Marco' delante). Solo la piel: el agua y los muros siguen siendo sim.")]
        public GameObject depositoVisualPrefab;

        [Header("Retoques de roca (R77 — el overlay del cincel)")]
        [Tooltip("La forma que Cesar guardó desde la paleta dev (F3 → GUARDAR FORMA COMO PLANO). Se reaplica sobre el plano en cada arranque del prólogo. Borrar el asset (o desasignarlo) = plano virgen.")]
        public PlanoOverlay planoOverlay;

        [Tooltip("Dibujar en la vista de escena los rectángulos de TODAS las zonas funcionales del plano del prólogo (cascada, poza, derrumbe, cuenco, depósito...). Solo editor; apágalo si estorba.")]
        public bool mostrarZonasDelPlano = true;

        /// <summary>La instancia viva de la escena (cacheada por los consumidores en Init, jamás por frame).</summary>
        public static PrologoEscenografia Buscar() => Object.FindAnyObjectByType<PrologoEscenografia>();

        /// <summary>El guion efectivo: el asset si está; si no, una instancia en memoria con los defaults de código (fallback invisible).</summary>
        public static GuionDelPrologo GuionEfectivo(PrologoEscenografia esc)
        {
            if (esc != null && esc.guion != null) return esc.guion;
            return ScriptableObject.CreateInstance<GuionDelPrologo>();
        }

#if UNITY_EDITOR
        // Los marcadores se ven en la vista de escena aunque no tengan
        // sprite: un rombo con etiqueta por cada uno, y la huella del
        // depósito (8x13 celdas) para colocarlo sin adivinar.
        private void OnDrawGizmos()
        {
            if (maestro != null)
            {
                Gizmos.color = new Color(1f, 0.75f, 0.3f, 0.9f);
                Gizmos.DrawWireSphere(maestro.position, 0.25f);
                UnityEditor.Handles.Label(maestro.position + Vector3.up * 0.4f, "MAESTRO");
            }
            if (deposito != null)
            {
                Gizmos.color = new Color(0.4f, 0.75f, 1f, 0.9f);
                float c = SimRenderer.CellWorldSize;
                Gizmos.DrawWireCube(deposito.position + new Vector3(0f, 6.5f * c, 0f), new Vector3(8f * c, 13f * c, 0f));
                UnityEditor.Handles.Label(deposito.position + Vector3.up * 1.6f, "DEPÓSITO");
            }
            if (deposito2 != null)
            {
                Gizmos.color = new Color(0.75f, 0.55f, 0.35f, 0.9f);
                float c = SimRenderer.CellWorldSize;
                Gizmos.DrawWireCube(deposito2.position + new Vector3(0f, 4.5f * c, 0f), new Vector3(6f * c, 9f * c, 0f));
                UnityEditor.Handles.Label(deposito2.position + Vector3.up * 1.2f, "SILO (lodo)");
            }

            if (mostrarZonasDelPlano) DibujarZonasDelPlano();
        }

        // =================================================================
        // (RONDA 77, herramientas para Cesar) EL MAPA DE LAS ZONAS: todos
        // los rectángulos funcionales del plano del prólogo, leídos de las
        // MISMAS constantes que consume SimLevelBuilder (regla 24: jamás
        // contra prosa) y dibujados en la vista de escena con su etiqueta.
        // Para perfilar la roca sin adivinar coordenadas: se ve dónde NO
        // morder (fontanería, receptores) y dónde vive cada beat.
        // =================================================================
        private static void Zona(int x0, int y0, int x1, int y1, Color color, string nombre)
        {
            float c = SimRenderer.CellWorldSize;
            var min = new Vector3(x0 * c, y0 * c, 0f);
            var size = new Vector3((x1 - x0 + 1) * c, (y1 - y0 + 1) * c, 0f);
            Gizmos.color = color;
            Gizmos.DrawWireCube(min + size * 0.5f, size);
            UnityEditor.Handles.Label(min + new Vector3(0f, size.y + 0.12f, 0f), nombre);
        }

        private static void DibujarZonasDelPlano()
        {
            var agua = new Color(0.35f, 0.7f, 1f, 0.8f);
            var piedra = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            var fuego = new Color(1f, 0.55f, 0.25f, 0.8f);
            var peligro = new Color(1f, 0.3f, 0.3f, 0.85f);
            var util = new Color(0.6f, 1f, 0.6f, 0.7f);

            // El contorno de la caverna entera (referencia general).
            Zona(SimLevelBuilder.FundacionX0, SimLevelBuilder.FundacionY0,
                 SimLevelBuilder.FundacionX1, SimLevelBuilder.FundacionY1, piedra, "CAVERNA (interior)");

            // El camino del agua.
            Zona(SimLevelBuilder.FundacionManantialX - 1, SimLevelBuilder.FundacionManantialY,
                 SimLevelBuilder.FundacionManantialX, SimLevelBuilder.FundacionManantialY + 1, agua, "manantial");
            Zona(SimLevelBuilder.FundacionRepisaAX0, SimLevelBuilder.FundacionRepisaAY,
                 SimLevelBuilder.FundacionRepisaAX1, SimLevelBuilder.FundacionRepisaAY + 1, agua, "repisa A (obra)");
            Zona(SimLevelBuilder.FundacionRepisaBX0 - 1, SimLevelBuilder.FundacionRepisaBY,
                 SimLevelBuilder.FundacionRepisaBX1, SimLevelBuilder.FundacionRepisaBY + 3, agua, "repisa B + labio (obra)");
            Zona(SimLevelBuilder.FundacionCharcoX0, SimLevelBuilder.FundacionY0 - SimLevelBuilder.FundacionPozoHondo,
                 SimLevelBuilder.FundacionCharcoX1, SimLevelBuilder.FundacionY0 - 1, agua, "poza de la cascada");

            // El derrumbe (lo talla el director en runtime: NO capturar aquí).
            Zona(SimLevelBuilder.FundacionDerrumbeX - 4, SimLevelBuilder.FundacionY1 - 1,
                 SimLevelBuilder.FundacionDerrumbeX + 4, SimLevelBuilder.FundacionY1 + 6, peligro, "grieta del derrumbe (runtime)");
            Zona(SimLevelBuilder.FundacionCraterX0, SimLevelBuilder.FundacionY0 - SimLevelBuilder.FundacionPozoHondo,
                 SimLevelBuilder.FundacionCraterX1, SimLevelBuilder.FundacionY0 + 1, peligro, "cráter del lodo (runtime)");

            // El taller del Maestro.
            Zona(SimLevelBuilder.FundacionCuencoX0, SimLevelBuilder.FundacionY0 - SimLevelBuilder.FundacionPozoHondo,
                 SimLevelBuilder.FundacionCuencoX1, SimLevelBuilder.FundacionY0 - 1, util, "cuenco de entregas (obra)");
            Zona(SimLevelBuilder.FundacionMesaX0, SimLevelBuilder.FundacionY0,
                 SimLevelBuilder.FundacionMesaX1, SimLevelBuilder.FundacionMesaTopY, piedra, "mesa del Maestro (obra)");
            Zona(SimLevelBuilder.FundacionBrasasX0 - 1, SimLevelBuilder.FundacionY0,
                 SimLevelBuilder.FundacionBrasasX1 + 1, SimLevelBuilder.FundacionY0 + 3, fuego, "hogar de brasas (obra)");
            Zona(SimLevelBuilder.FundacionDepositoX0, SimLevelBuilder.FundacionDepositoY0,
                 SimLevelBuilder.FundacionDepositoX1, SimLevelBuilder.FundacionDepositoY1, agua, "depósito (emerge aquí)");

            // Lo del jugador.
            Zona(SimLevelBuilder.FundacionFogonX0, SimLevelBuilder.FundacionFogonY,
                 SimLevelBuilder.FundacionFogonX1, SimLevelBuilder.FundacionFogonY + 3, fuego, "fogón del jugador");
            Zona(SimLevelBuilder.FundacionVetaX, SimLevelBuilder.FundacionVetaY0,
                 SimLevelBuilder.FundacionVetaX + 1, SimLevelBuilder.FundacionVetaY1, util, "veta de turba");
            Zona(SimLevelBuilder.FundacionEstanteX0, SimLevelBuilder.FundacionEstanteBaseY,
                 SimLevelBuilder.FundacionEstanteX1, SimLevelBuilder.FundacionEstanteBaseY + 2, util, "sitio del estante");
            Zona(SimLevelBuilder.FundacionSalidaX0, SimLevelBuilder.FundacionY0,
                 SimLevelBuilder.FundacionSalidaX1, SimLevelBuilder.FundacionY0 + 5, util, "nicho de salida");
            Zona(SimLevelBuilder.FundacionAprendizX - 1, SimLevelBuilder.FundacionAprendizY - 1,
                 SimLevelBuilder.FundacionAprendizX + 1, SimLevelBuilder.FundacionAprendizY + 1, util, "spawn del aprendiz");
        }
#endif
    }
}
