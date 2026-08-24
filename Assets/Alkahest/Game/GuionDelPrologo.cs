using UnityEngine;

namespace Alkahest.Game
{
    /// <summary>
    /// (RONDA 75 — LA ESCENIFICACIÓN) EL GUION DEL PRÓLOGO COMO ASSET.
    ///
    /// Hasta la ronda 74 estos números vivían como constantes en el bloque
    /// "EL GUION" de <see cref="FundacionDirector"/>. Ahora son un
    /// ScriptableObject editable desde el Inspector: Cesar (o su hermano)
    /// cambia una palabra, una cantidad o un tiempo SIN pedir código, y el
    /// cambio queda versionado como asset.
    ///
    /// AUTORIDAD (la regla de oro de la arquitectura híbrida, ronda 75):
    ///  · El ASSET manda sobre todos estos valores.
    ///  · Los inicializadores de campo de esta clase son la ÚNICA copia de
    ///    los defaults: si el asset falta (sandbox recién clonado, escena
    ///    vieja), el director crea una instancia en memoria con estos mismos
    ///    valores y el juego corre igual — el fallback es invisible.
    ///  · El CÓDIGO jamás escribe en el asset.
    ///
    /// Qué NO vive aquí (y por qué): la geometría tallada del plano
    /// (SimLevelBuilder — la sim es la verdad), los beats y su orden (lógica
    /// de juego), y las mecánicas del frasco (Flask). Esto es el guion, no el
    /// motor.
    /// </summary>
    [CreateAssetMenu(fileName = "GuionDelPrologo", menuName = "Ten Thousand Years/Guion del Prologo")]
    public sealed class GuionDelPrologo : ScriptableObject
    {
        [Header("Las palabras del Maestro (una por golpe de voz)")]
        public string vozVen = "VEN.";
        public string vozToma = "TOMA.";
        public string vozAgua = "AGUA.";
        public string vozTraela = "TRÁELA.";
        public string vozBien = "BIEN.";
        public string vozLodo = "LODO.";
        public string vozTraelo = "TRÁELO.";
        public string vozObserva = "OBSERVA.";
        public string vozLlenalo = "LLÉNALO.";

        [Header("Leyendas del tutorial contextual (fichas blancas)")]
        public string leyendaMover = "muévete";
        public string leyendaAspirar = "mantén — aspira el agua";
        public string leyendaVerter = "mantén — viértela donde quieras";

        [Header("Cantidades (celdas de materia real)")]
        public int aspirarMeta = 10;
        public int verterMeta = 6;
        public int entregaAguaMeta = 20;
        public int lodoProbarMeta = 8;
        public int entregaLodoMeta = 16;
        public int llenarDepositoMeta = 48;
        [Tooltip("Desplazamiento real (unidades de mundo) por dirección para confirmar cada tecla del WASD.")]
        public float moverMetaMundo = 0.5f;

        [Header("Tiempos (segundos)")]
        public float despertarPausaSeg = 1.4f;
        public float trasTutorialSeg = 0.8f;
        public float entregaFrascoSeg = 0.95f;
        public float trasTomaSeg = 1.3f;
        public float juegoLibreSeg = 14f;
        public float lodoLibreSeg = 22f;
        public float vozHoldSeg = 2.1f;
        public float derrumbePausaSeg = 2.2f;

        [Header("Triggers de distancia (celdas)")]
        public float distCharla = 16f;
        public float distZonaAgua = 26f;

        [Header("La luz (radios de viñeta por tramo, px escalados)")]
        public float radioDespertar = 180f;
        public float radioVen = 260f;
        public float radioToma = 330f;
        public float radioAgua = 440f;
        public float radioTaller = 540f;
        public float radioAmanecer = 2400f;

        [Header("La cascada")]
        public float manantialSeg = 0.14f;
        public int manantialCeldas = 2;
        public int pozaLlenaCeldas = 48;

        [Header("El derrumbe y la gotera de lodo")]
        public int lodoBurstCeldas = 26;
        public float lodoSeepSeg = 0.4f;
        public int lodoMonticuloTope = 70;
        public int lodoMonticuloResume = 50;

        [Header("El depósito")]
        public float depositoEmergerSeg = 2.4f;
        public int depositoCargaInicial = 14;

        [Header("UI (proporciones de pantalla)")]
        [Tooltip("Altura del centro de la voz del Maestro, como fracción del alto de pantalla desde arriba.")]
        public float vozAlturaFrac = 0.24f;
        [Tooltip("Tamaño de fuente de la voz, como fracción del alto de pantalla.")]
        public float vozTamFrac = 0.056f;
        [Tooltip("Píxeles (escalados) bajo el aprendiz donde flotan las fichas del tutorial.")]
        public float fichasOffsetPx = 64f;
    }
}
