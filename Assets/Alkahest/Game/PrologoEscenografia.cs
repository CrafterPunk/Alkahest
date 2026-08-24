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

        [Header("Arte horneado (opcional — si falta, el código lo genera igual)")]
        [Tooltip("Prefab visual del tanque (hijos: 'Fondo' detrás de la sim, 'Marco' delante). Solo la piel: el agua y los muros siguen siendo sim.")]
        public GameObject depositoVisualPrefab;

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
        }
#endif
    }
}
