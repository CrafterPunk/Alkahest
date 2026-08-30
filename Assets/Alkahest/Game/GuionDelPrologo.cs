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

        [Header("Enseñar el cursor (R81: el haz de presentación y el aro de la boca)")]
        [Tooltip("Duración total del haz de presentación al recibir el frasco (se estira hasta el cursor, sostiene y se recoge — una sola vez). Arranca 0.35 s después del aterrizaje para no competir con el HUD naciendo (revisión Opus #8).")]
        public float hazPresentacionSeg = 2.0f;
        [Tooltip("Alfa máxima del aro de la boca del frasco (el anillo tenue en el cursor durante los pasos de aspirar/verter).")]
        public float aroAlfa = 0.4f;

        [Header("El mantén y el recuerdo del aspirar (R79b, aprobados por Cesar)")]
        [Tooltip("Succión CONTINUA mínima para que la ficha de aspirar confirme: el gesto aprendido es el mantén real, no un clic afortunado.")]
        public float aspirarHoldSeg = 0.6f;
        [Tooltip("Segundos sin aspirar nada durante el juego libre (y aún en la zona del agua) antes de que la ficha-recuerdo 'CLIC IZQ — mantén' reaparezca UNA vez.")]
        public float recordatorioAspirarSeg = 8f;
        [Tooltip("Cuánto vive la ficha-recuerdo si el jugador no aspira: se desvanece sola, sin celebración.")]
        public float recordatorioDuraSeg = 6f;

        [Header("Tiempos (segundos)")]
        public float despertarPausaSeg = 1.4f;
        public float trasTutorialSeg = 0.8f;
        public float entregaFrascoSeg = 0.95f;
        // (R81, revisión Opus #8) `trasTomaSeg` (1.3) se RENOMBRA a
        // `trasTomaRespiroSeg` (2.6) a propósito — regla 58: el asset ya
        // serializa el nombre viejo con 1.3 y PISARÍA el default; con el
        // nombre nuevo el asset viejo lo ignora. El respiro creció porque
        // ahora contiene el HAZ DE PRESENTACIÓN entero (retraso 0.35 +
        // hazPresentacionSeg): el beat no debe cortar el gesto a la mitad.
        public float trasTomaRespiroSeg = 2.6f;
        // (R77) `juegoLibreSeg` (14 s a reloj) se RENOMBRA a
        // `juegoLibreTopeSeg` a propósito: el asset ya guardado en el
        // proyecto serializa el nombre viejo con 14, y un campo serializado
        // PISA el default de código (regla 58) — con el nombre nuevo el
        // asset viejo simplemente lo ignora y el tope arranca en su valor
        // pensado para el cierre por conducta.
        [Tooltip("(R77) Tope de seguridad del juego libre con agua: si el jugador ni juega ni se va, TRÁELA. llega igual a estos segundos. El cierre NORMAL es por conducta (ver juegoLibreMinSeg / juegoLibreAlejarseCeldas).")]
        public float juegoLibreTopeSeg = 45f;
        [Tooltip("(R77) Mínimo de juego libre antes de que alejarse cuente como 'terminé de jugar'.")]
        public float juegoLibreMinSeg = 5f;
        public float lodoLibreSeg = 22f;
        public float vozHoldSeg = 2.1f;
        public float derrumbePausaSeg = 2.2f;

        [Header("Triggers de distancia (celdas)")]
        public float distCharla = 16f;
        public float distZonaAgua = 26f;
        [Tooltip("(R77) A esta distancia de la poza, alejarse cierra el juego libre por CONDUCTA (el jugador dejó el agua y siguió su camino).")]
        public float juegoLibreAlejarseCeldas = 34f;

        [Header("La luz (radios de viñeta por tramo, px escalados)")]
        public float radioDespertar = 180f;
        public float radioVen = 260f;
        public float radioToma = 330f;
        // (R81, revisión Opus, nota final) `radioAgua` (440) se RENOMBRA a
        // `radioAguaLuz` (615) — regla 58, mismo motivo que arriba. Con 440
        // el negro total empezaba a 4.29 u y el alcance del frasco mide 6 u:
        // el último tercio del haz real (incluido su corte rojo de "fuera de
        // alcance") se jugaba A OSCURAS. 615 hace que las 6 u quepan justas
        // en el óvalo horizontal.
        public float radioAguaLuz = 615f;
        public float radioTaller = 540f;
        [Tooltip("(R87, Cesar: 'la luz debería dejar de ser focal en algún momento… propongo que sea después de que cae el lodo, para que se ponga a jugar') Radio al ABRIRSE la luz cuando el lodo ya cayó: amplio para jugar libre, con un resto de penumbra en los bordes — el amanecer pleno sigue siendo del final.")]
        public float radioLodoJuego = 1500f;
        public float radioAmanecer = 2400f;
        // (R79, feedback de Cesar: "la luz pierde muy rápido el track del
        // personaje") El VEN. ya no estira la luz hacia el fuego (el bias
        // 0.62 de la R73 dejaba al jugador en el borde de su propio óvalo en
        // cuanto se movía): ahora la luz es casi toda del jugador y el rumbo
        // lo señala la LUCECITA del Maestro (campos de abajo).
        [Tooltip("(R79) Cuánto de la luz es del JUGADOR durante el VEN. (1 = pegada a él; hacia 0 = estirada al fuego). Antes 0.62 y perdía al jugador.")]
        public float luzBiasVen = 0.92f;
        [Tooltip("(R79) Radio en px escalados de la lucecita del área del Maestro durante el VEN. (el indicador de 'algo ocurre allá').")]
        public float lucecitaRadioPx = 52f;
        [Tooltip("(R79; recalibrada R82 por Cesar: '50% menos intensa') Alfa máxima de la lucecita (parpadea con el fuego).")]
        public float lucecitaAlfa = 0.3f;
        [Tooltip("(R82, Cesar) Vida de la lucecita del VEN.: aparece con la palabra y se DESVANECE en ~este tiempo — después, la chapa EL MAESTRO queda de referencia.")]
        public float lucecitaVidaSeg = 1.0f;

        [Header("La cascada")]
        public float manantialSeg = 0.14f;
        public int manantialCeldas = 2;
        public int pozaLlenaCeldas = 48;
        [Tooltip("(R77) Volumen base del rumor de la cascada (bucle GrifoLiquido), antes de distancia y del volumen de efectos.")]
        public float cascadaVolumen = 0.4f;
        [Tooltip("(R77; recalibrado R81) Radio audible del rumor, en celdas desde la MITAD de la caída (caída cuadrática, como los grifos). Con 55 desde el manantial el volumen era 0 hasta CON LOS PIES EN LA POZA (revisión Opus #3, medido).")]
        public float cascadaRadioAudibleCeldas = 95f;

        [Header("El derrumbe y la gotera de lodo")]
        public int lodoBurstCeldas = 26;
        public float lodoSeepSeg = 0.4f;
        public int lodoMonticuloTope = 70;
        public int lodoMonticuloResume = 50;

        [Header("El depósito")]
        public float depositoEmergerSeg = 2.4f;
        public int depositoCargaInicial = 14;

        [Header("El capítulo 2 (R83): el silo del lodo")]
        [Tooltip("Meta de lodo (lodo + barbotina) del LLÉNALO. del silo. Interior del silo = 78 celdas (gemelo del tanque desde R84).")]
        public int llenarDeposito2Meta = 24;
        [Tooltip("Tope de espera de una emergencia antes de seguir el arco aunque el jugador no se aparte (revisión Opus A9).")]
        public float emergerTopeSeg = 12f;
        [Tooltip("La placa dice la verdad cuando la meta está BLOQUEADA por materia ajena ('· sobra AGUA — aspírala'). Apagable si estorba (decisión pendiente de Cesar sobre el contador honesto — aquí es mínimo: solo habla si la meta es imposible).")]
        public bool placaAvisaEstorbo = true;

        [Header("El REORDEN (R84, fase B1 del capítulo 2)")]
        [Tooltip("La palabra del Maestro que abre la cinemática del orden.")]
        public string vozOrden = "ORDEN.";
        [Tooltip("Duración del BARRIDO que recoge el desastre (el frente avanza de izquierda a derecha por la caverna).")]
        public float reordenBarridoSeg = 2.4f;
        [Tooltip("Duración del fundido del fondo: la ruina cede al muro del taller profundo.")]
        public float fondoTransicionSeg = 3.5f;

        [Header("El refill (R85, fase B2): los tubos que tocan el suelo")]
        [Tooltip("Cadencia del goteo de reabastecimiento (segundos por gota). La gota nace en el TOPE del vidrio y CAE a la vista (Cesar R88: 'se tiene que ver cayendo desde arriba, lento'). Misma cadencia para agua y lodo.")]
        public float refillSeg = 0.8f;
        // (R91) `refillTope` se RENOMBRA a `refillTopeCeldas` — regla 58 en
        // su variante de HOT-RELOAD: el objeto vivo del editor serializó el
        // 36 de la R88 a través de las recompilaciones (el asset del disco
        // jamás tuvo el campo) y el tanque de Cesar se CLAVABA en la mitad
        // ("cuando llegan a la mitad dejan de llenarse" — su reporte era
        // exactamente este fantasma). Nombre nuevo = memoria vieja ignorada.
        [Tooltip("(R91, Cesar: 'hasta el TOPE aunque no se visualice la última sección') Tope del refill = el vidrio ENTERO (72). La gota visible cae por el centro mientras hay caída; el resto se completa EN SILENCIO. Cadencia cuadrática 0.8→~6.4 s: el llenado ronda los 3 minutos.")]
        // (R112, R15) RETIRADO: el tope del refill ya no se configura — es
        // Capacidad() leída del vidrio real (el 72 quedó fósil cuando los
        // reservorios crecieron a 276 en la R110 y el goteo se plantaba a un
        // cuarto). El campo sobrevive solo para no romper el asset serializado.
        public int refillTopeCeldas = 72;
        [Tooltip("(R88) Duración del ENCAJE del tubo: la columna de cobre empuja desde el subsuelo y asienta con overshoot.")]
        public float tuboInstalarSeg = 0.7f;

        [Header("UI (proporciones de pantalla)")]
        [Tooltip("Altura del centro de la voz del Maestro, como fracción del alto de pantalla desde arriba.")]
        public float vozAlturaFrac = 0.24f;
        [Tooltip("Tamaño de fuente de la voz, como fracción del alto de pantalla.")]
        public float vozTamFrac = 0.056f;
        [Tooltip("Píxeles (escalados) bajo el aprendiz donde flotan las fichas del tutorial.")]
        public float fichasOffsetPx = 64f;
    }
}
