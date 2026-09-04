using System;
using System.Collections.Generic;

namespace Alkahest.Sim
{
    /// <summary>
    /// (R130) Un parámetro del laboratorio: lo que el panel enseña y lo que un
    /// preset guarda. El VALOR vive en un campo estático de LabParams (lectura
    /// directa en el hot path); Leer/Escribir son los delegados que el panel y
    /// los presets usan. Puro C#: cero API de Unity (regla 56 y banco headless).
    /// </summary>
    public sealed class LabParam
    {
        public string Clave;      // "agua.evapBase" -- estable, es la clave del preset.
        public string Nombre;     // lo que se lee en el panel.
        public string Grupo;      // pestaña.
        public string Unidad;     // "u/visita", "raw (2 °C)", "%", "celdas/s"...
        public string Ayuda;      // en palabras sencillas, plegable en el panel.
        public float Def, Min, Max;
        public bool Entero;
        public bool RequiereReconstruir; // true = solo aplica al reconstruir el mundo (se marca en el panel).
        public Func<float> Leer;
        public Action<float> Escribir;
        public bool EsDefault => Math.Abs(Leer() - Def) < 1e-4f;
    }

    /// <summary>
    /// (R130, LABORATORIO DE LEYES) TODOS los números de los sistemas nuevos,
    /// como estáticos (el stepper los lee cada tick sin indirección) y con un
    /// REGISTRO paralelo (nombre, significado, default, rango, unidad, ayuda)
    /// para el panel y los presets. Unidades: una "visita" = una pasada de
    /// LabCampos sobre la celda = cada 8 ticks (0.27 s a 30 Hz); 255 unidades
    /// de humedad/carga = una celda entera de agua/finos; "raw" = temperatura
    /// interna (°C = raw*2-120, ambiente 70).
    ///
    /// REGLA: ningún número de física del laboratorio se escribe suelto en el
    /// stepper; todos pasan por aquí para que sean observables y capturables.
    /// </summary>
    public static class LabParams
    {
        public static readonly List<LabParam> Registro = new List<LabParam>();

        // ---------------- TIEMPO ----------------
        public static int PresupuestoMs = 20;

        // ---------------- AGUA / VAPOR ----------------
        public static int EvapBase = 1;
        public static int EvapPorGrado = 1;
        public static int SatBase = 60;
        public static int SatPorGrado = 4;
        public static int VaporDifusion = 4;
        public static int VaporAscenso = 12;
        public static int CondensaRate = 24;
        public static int VaporVida = 180;
        public static int VaporCondensaC = 10;
        public static int HumedadInicialPct = 60;
        public static int Latente = 4;

        // ---------------- SEDIMENTO ----------------
        public static int TurbidezFuente = 40;
        public static int Decantacion = 6;
        public static int FactorMovilPct = 25;
        public static int ReposoMovil = 3;
        public static int DepositoUmbral = 200;
        public static int DepositoReposo = 24;
        public static int Mezcla = 8;
        public static int ErosionPct = 6;
        public static int ErosionCargaMax = 64;
        public static int ErosionArcillaPct = 1;

        // ---------------- SUELO ----------------
        public static int PermArena = 40;
        public static int PermGrava = 90;
        public static int PermSedimento = 12;
        public static int PermCeniza = 30;
        public static int PermFibra = 30;
        public static int PermArcilla = 2;
        public static int PermArenisca = 30;
        public static int Infiltracion = 32;
        public static int Percolacion = 24;
        public static int Capilaridad = 4;
        public static int CapilarArriba = 16;
        public static int Secado = 3;
        public static int ColmatacionPct = 100;
        public static int CompactReposo = 200;
        public static int CompactPct = 2;
        public static int CompactHumMin = 100;
        public static int CompactHumMax = 230;
        public static int CompactVecinos = 4;
        public static int AblandaHum = 250;
        public static int TerracotaRaw = 150;
        public static int TerracotaHumMax = 30;
        public static int AbonoCeniza = 128;

        // ---------------- FUEGO / FUENTES ----------------
        public static int HogarRaw = 170;
        public static int HogarCalor = 40;
        public static int FrioRaw = 30;
        public static int FrioPotencia = 20;
        public static int FibraMojadaMin = 100;
        public static int Caudal = 24;
        public static int FuenteTempRaw = 70;
        public static int VidaHumo = 400;
        public static int PermCarbon = 20;
        public static int VidrioRaw = 200;
        public static int VidrioVisitas = 60;

        // ---------------- TÉRMICA ----------------
        public static int TermicaPropia = 1;
        public static int KAire = 4, KAgua = 8, KRoca = 2, KPolvo = 3, KGas = 6, KArcilla = 2;
        public static int CAire = 1, CAgua = 4, CRoca = 3, CPolvo = 2;
        public static int Conveccion = 1;
        public static int TiroAmbienteTicks = 32;

        // ---------------- PRESIÓN ----------------
        public static int PresionActiva = 1;
        public static int PresionCadaTicks = 2;
        public static int PresionCeldasPorPaso = 4;
        public static int DesnivelMin = 3;
        public static int PresionMinCeldas = 6;

        // ---------------- LUZ ----------------
        public static int LuzCadaTicks = 16;
        public static int LuzDecayAire = 8;
        public static int LuzDecayCielo = 1;
        public static int LuzDecayAgua = 20;
        public static int LuzDecayPlanta = 12;
        public static int LuzDecayHumo = 24;
        /// <summary>Columnas de la boca del cielo (las fija el plano, no el panel).</summary>
        public static int LuzCieloX0 = -1, LuzCieloX1 = -1;

        // ---------------- PLANTAS ----------------
        public static int GerminaPorMil = 2;
        public static int PlantaHumedadMin = 60;
        public static int PlantaLuzMin = 40;
        public static int PlantaBebe = 6;
        public static int PlantaCrecerSavia = 120;
        public static int PlantaPasaSavia = 40;
        public static int PlantaAltoMax = 14;
        public static int PlantaRamaPct = 8;
        public static int PlantaMarchitaVisitas = 40;
        public static int PlantaFertilidadBonusPct = 50;
        public static int PlantaTranspira = 2;
        public static int PlantaAbonoMuerte = 40;

        // ---------------- CUERPOS ----------------
        public static int CuerposActivos = 1;
        public static int FracturaCaida = 6;
        public static int Golpes = 3;
        public static int FracturaPct = 35;

        // =================================================================
        // REGISTRO
        // =================================================================
        static LabParams()
        {
            // TIEMPO
            R("tiempo.presupuestoMs", "Presupuesto de sim por frame", "TIEMPO", "ms", 20, 5, 100, () => PresupuestoMs, v => PresupuestoMs = (int)v,
              "Cuando aceleras el tiempo (5x, 10x, 50x, 100x) el juego intenta correr más ticks por frame. Si un frame ya gastó estos milisegundos en simular, se corta y el tiempo que faltó se pierde. Súbelo si quieres más velocidad real a costa de fluidez.");

            // AGUA / VAPOR
            R("aire.humedadInicialPct", "Humedad del aire al nacer el mundo", "AGUA", "% de su saturación", 60, 0, 100, () => HumedadInicialPct, v => HumedadInicialPct = (int)v,
              "Con cuánta humedad arranca cada celda de aire, en porcentaje de SU PROPIA saturación (que depende de su temperatura: el aire frío de la cámara alta cabe menos que el templado del arroyo). Ninguna celda nace supersaturada, así que el mundo no llueve solo al empezar. Con 0 la cueva nace SECA y el primer vapor que produzcas se gasta entero en humedecer el aire antes de que ninguna pared pueda sudar; una cueva de verdad vive cerca de la saturación. Solo se aplica al CONSTRUIR el mundo.", true, true);
            R("agua.evapBase", "Evaporación base", "AGUA", "u/visita", 1, 0, 40, () => EvapBase, v => EvapBase = (int)v,
              "Cuánta agua pierde por evaporación una celda de superficie a 20 °C en cada visita (una visita = cada 8 ticks). 255 unidades = una celda entera. Solo evapora si el aire de encima no está saturado.");
            R("agua.evapPorGrado", "Evaporación por calor", "AGUA", "u/visita por raw", 1, 0, 10, () => EvapPorGrado, v => EvapPorGrado = (int)v,
              "Cuánto más evapora por cada unidad de temperatura (raw, 2 °C) por encima del ambiente. Con 1, agua a 60 °C evapora 20 unidades más por visita que a 20 °C.");
            R("vapor.satBase", "Saturación del aire a 20 °C", "AGUA", "u", 60, 0, 255, () => SatBase, v => SatBase = (int)v,
              "Cuánto vapor cabe en una celda de aire a temperatura ambiente. Por encima de esto el vapor sobrante CONDENSA sobre la superficie vecina.");
            R("vapor.satPorGrado", "Saturación por calor", "AGUA", "u por raw", 4, 0, 16, () => SatPorGrado, v => SatPorGrado = (int)v,
              "Cuánto más vapor cabe en el aire por cada raw (2 °C) por encima del ambiente, y cuánto menos por debajo. Aire caliente = esponja grande; aire frío = escurre.");
            R("vapor.difusion", "Difusión del vapor", "AGUA", "divisor", 4, 1, 16, () => VaporDifusion, v => VaporDifusion = (int)v,
              "El vapor se reparte entre celdas de aire vecinas: en cada visita se mueve (diferencia / 2·este número). Más bajo = se esparce más rápido.");
            R("vapor.ascenso", "Ascenso del vapor", "AGUA", "u/visita", 12, 0, 30, () => VaporAscenso, v => VaporAscenso = (int)v,
              "El vapor pesa menos que el aire: cuántas unidades suben a la celda de encima en cada visita si arriba hay menos. Es lo que acumula humedad bajo los techos. (R132) 6 → 12 tras medir: con 6, la humedad que sale del arroyo se queda en la galería y no llega nunca a la cámara alta.");
            R("vapor.condensaRate", "Ritmo de condensación", "AGUA", "u/visita", 24, 1, 80, () => CondensaRate, v => CondensaRate = (int)v,
              "Cuánto vapor sobrante puede pasar en una visita del aire a la superficie vecina (roca: rocío; poroso: humedad; agua: volumen). Cuando el rocío de una roca llega a 255, GOTEA.");
            R("vapor.vidaVapor", "Vida del vapor visible", "AGUA", "ticks", 180, 10, 255, () => VaporVida, v => { VaporVida = (int)v; VaporVidaCambiado = true; },
              "Cuántos ticks vive una celda de VAPOR VISIBLE (el gas blanco) antes de disolverse en humedad del aire. No se pierde agua: pasa al aire. Más vida = columnas de vapor más largas. (R132) 60 → 180 tras medir: del hogar a la cámara alta hay ~65 celdas de chimenea y con 60 ticks el vapor moría por el camino.");
            R("vapor.condensaC", "El vapor visible se vuelve agua a", "AGUA", "°C", 10, -40, 120, () => VaporCondensaC, v => { VaporCondensaC = (int)v; VaporVidaCambiado = true; },
              "Punto de rocío del VAPOR VISIBLE (el gas blanco): por debajo de esta temperatura, una celda de vapor se vuelve agua de golpe, donde esté. Es el motor del juego base, no la saturación del laboratorio. OJO: si lo pones por encima del ambiente de la cueva (20 °C), el vapor muere a dos celdas del fuego y NO PUEDE VIAJAR — la cadena hervir→subir→condensar arriba se rompe entera. Por debajo del ambiente, el vapor sube como gas, expira convertido en humedad del aire y condensa donde de verdad hace frío.\n\nDÓNDE LLUEVE LO DECIDE EL MAPA TÉRMICO, Y TÚ PUEDES MOVERLO: con los 10 °C de fábrica el vapor NO condensa en el arroyo (20 °C) ni en la cámara profunda (12 °C); solo en la cámara alta (8 °C), sobre un bloque de núcleo frío, o en cualquier rincón que enfríes por debajo de 10 °C.");
            R("termica.latente", "Calor latente", "AGUA", "raw por celda", 4, 0, 16, () => Latente, v => Latente = (int)v,
              "Evaporar enfría lo que se evapora y condensar calienta la superficie: cuántos raw (2 °C) cambia la temperatura por cada celda entera de agua que cambia de estado.");

            // SEDIMENTO
            R("sed.turbidezFuente", "Turbidez del manantial", "SEDIMENTO", "u", 40, 0, 255, () => TurbidezFuente, v => TurbidezFuente = (int)v,
              "Cuántos finos trae cada celda de agua que emite el manantial. 255 = tanta tierra como agua. Con 40, unas seis celdas de agua dejan una de sedimento al decantar (a 20 celdas/s de caudal, ~3 celdas de sedimento por segundo: la poza se ciega en unos minutos).");
            R("sed.decantacion", "Decantación", "SEDIMENTO", "u/visita", 6, 0, 40, () => Decantacion, v => Decantacion = (int)v,
              "Cuántos finos bajan de una celda de agua a la de debajo en cada visita cuando el agua está quieta. Los finos se acumulan en el fondo.");
            R("sed.factorMovil", "Decantación en agua que corre", "SEDIMENTO", "%", 25, 0, 100, () => FactorMovilPct, v => FactorMovilPct = (int)v,
              "Porcentaje de la decantación que ocurre mientras el agua se mueve. Bajo = el agua rápida arrastra los finos lejos; los deposita donde se frena.");
            R("sed.reposoMovil", "Umbral de quietud", "SEDIMENTO", "visitas", 3, 1, 30, () => ReposoMovil, v => ReposoMovil = (int)v,
              "Cuántas visitas sin moverse necesita una celda de agua para contar como quieta.");
            R("sed.depositoUmbral", "Umbral de depósito", "SEDIMENTO", "u", 200, 50, 255, () => DepositoUmbral, v => DepositoUmbral = (int)v,
              "Con cuántos finos una celda de agua del fondo, quieta, se convierte en SEDIMENTO (se pierde esa celda de agua: los finos ocupan su sitio).");
            R("sed.depositoReposo", "Quietud para depositar", "SEDIMENTO", "visitas", 24, 0, 100, () => DepositoReposo, v => DepositoReposo = (int)v,
              "Cuántas visitas quieta debe llevar la celda de fondo antes de depositar. (R133) 8 → 24 tras medir el churn del lecho: con 8, el agua depositaba y volvía a erosionar 6 279 veces en 6 000 ticks para un cambio NETO de 375 celdas — ruido de 17 a 1. Con 24 el neto es el mismo (372) con menos de la mitad de eventos, Y la poza decanta mejor (el agua sale un 61 % más limpia en vez de un 53 %): al agua le da tiempo a soltar los finos antes de que el fondo los fije. La erosión se deja alta a propósito: que el agua que corre arranque y la que se para suelte es lo que el jugador tiene que aprender.");
            R("sed.mezcla", "Mezcla lateral", "SEDIMENTO", "/256", 8, 0, 64, () => Mezcla, v => Mezcla = (int)v,
              "Cuánto se igualan los finos entre celdas de agua vecinas de lado en cada visita.");
            R("sed.erosionPct", "Erosión del sedimento", "SEDIMENTO", "%", 6, 0, 100, () => ErosionPct, v => ErosionPct = (int)v,
              "Probabilidad de que una celda de agua que ACABA DE MOVERSE arranque un sedimento vecino y lo convierta en agua turbia (carga 255). El agua limpia erosiona; la muy cargada no.");
            R("sed.erosionCargaMax", "Carga máxima para erosionar", "SEDIMENTO", "u", 64, 0, 255, () => ErosionCargaMax, v => ErosionCargaMax = (int)v,
              "Solo el agua con menos finos que esto puede erosionar. Es lo que hace que un canal se sature.");
            R("sed.erosionArcillaPct", "Erosión de la arcilla", "SEDIMENTO", "%", 1, 0, 100, () => ErosionArcillaPct, v => ErosionArcillaPct = (int)v,
              "Como la anterior pero para la arcilla compactada: mucho más resistente.");

            // SUELO
            R("suelo.permArena", "Permeabilidad de la arena", "SUELO", "0-255", 40, 0, 255, () => PermArena, v => PermArena = (int)v, "Qué tan fácil entra y baja el agua por la arena. 0 = impermeable.");
            R("suelo.permGrava", "Permeabilidad de la grava", "SUELO", "0-255", 90, 0, 255, () => PermGrava, v => PermGrava = (int)v, "La grava es gruesa: el agua pasa rápido y apenas se colmata.");
            R("suelo.permSedimento", "Permeabilidad del sedimento", "SUELO", "0-255", 12, 0, 255, () => PermSedimento, v => PermSedimento = (int)v, "El sedimento es fino: el agua entra despacio y se colmata rápido.");
            R("suelo.permCeniza", "Permeabilidad de la ceniza", "SUELO", "0-255", 30, 0, 255, () => PermCeniza, v => PermCeniza = (int)v, "La ceniza absorbe agua; mojada se vuelve abono del sustrato.");
            R("suelo.permFibra", "Permeabilidad de la fibra", "SUELO", "0-255", 30, 0, 255, () => PermFibra, v => PermFibra = (int)v, "La fibra seca empapa agua (y mojada no prende).");
            R("suelo.permArcilla", "Permeabilidad de la arcilla", "SUELO", "0-255", 2, 0, 255, () => PermArcilla, v => PermArcilla = (int)v, "La arcilla casi no deja pasar agua: un canal de arcilla es estanco.");
            R("suelo.permArenisca", "Permeabilidad de la arenisca", "SUELO", "0-255", 30, 0, 255, () => PermArenisca, v => PermArenisca = (int)v,
              "La arenisca es roca porosa: no cae, pero el agua la atraviesa despacio y sale LIMPIA por el otro lado (los finos se quedan dentro y la van colmatando). Es el filtro natural del laboratorio.");
            R("suelo.infiltracion", "Infiltración", "SUELO", "u/visita", 32, 0, 255, () => Infiltracion, v => Infiltracion = (int)v,
              "Cuánta agua pasa por visita de una celda de agua a un poroso vecino, a permeabilidad máxima y sin colmatar. Se multiplica por la permeabilidad y por (1 − colmatación)².");
            R("suelo.percolacion", "Percolación", "SUELO", "u/visita", 24, 0, 255, () => Percolacion, v => Percolacion = (int)v,
              "Cuánta agua baja por gravedad de un poroso al poroso de debajo por visita (a permeabilidad máxima).");
            R("suelo.capilaridad", "Capilaridad lateral", "SUELO", "/256", 4, 0, 64, () => Capilaridad, v => Capilaridad = (int)v,
              "Fracción de la diferencia de humedad que se iguala de lado entre porosos vecinos por visita.");
            R("suelo.capilarArriba", "Capilaridad hacia arriba", "SUELO", "/256", 16, 0, 64, () => CapilarArriba, v => CapilarArriba = (int)v,
              "Solo en materiales finos (sedimento, arcilla, ceniza): el agua sube un poco contra la gravedad. Es lo que humedece la superficie de un suelo con agua debajo — y por tanto lo que decide si algo puede arraigar. (R134) 2 → 16 tras medir: con 2, un suelo empapado subía 1 unidad por visita a su superficie mientras el aire se llevaba 3, así que la cara de CUALQUIER suelo tendía a cero por muy mojado que estuviera por dentro, y ninguna planta podía germinar. Con 16 la superficie se equilibra alrededor de 100 mientras quede agua abajo: el suelo se comporta como un suelo y no como una piedra mojada.");
            R("suelo.secado", "Secado al aire", "SUELO", "u/visita", 3, 0, 40, () => Secado, v => Secado = (int)v,
              "Cuánta agua pierde un poroso hacia una celda de aire vecina no saturada por visita. Con calor, más.");
            R("suelo.colmatacion", "Colmatación por infiltración", "SUELO", "%", 100, 0, 400, () => ColmatacionPct, v => ColmatacionPct = (int)v,
              "Qué parte de los finos del agua que se infiltra se quedan atrapados en el poroso (subiendo su colmatación, que frena la infiltración futura). Así un lecho de arena se sella solo.");
            R("suelo.compactReposo", "Quietud para compactar", "SUELO", "visitas", 200, 0, 255, () => CompactReposo, v => CompactReposo = (int)v,
              "Cuántas visitas sin moverse (255 ≈ 68 s) necesita un sedimento húmedo y enterrado para poder compactarse en ARCILLA.");
            R("suelo.compactPct", "Probabilidad de compactar", "SUELO", "%", 2, 0, 100, () => CompactPct, v => CompactPct = (int)v,
              "Una vez cumplida la quietud, probabilidad por visita de que el sedimento se vuelva arcilla.");
            R("suelo.compactHumMin", "Humedad mínima para compactar", "SUELO", "u", 100, 0, 255, () => CompactHumMin, v => CompactHumMin = (int)v, "El sedimento seco no se compacta: es polvo.");
            R("suelo.compactHumMax", "Humedad máxima para compactar", "SUELO", "u", 230, 0, 255, () => CompactHumMax, v => CompactHumMax = (int)v, "El sedimento empapado tampoco: es barro líquido.");
            R("suelo.compactVecinos", "Vecinos sólidos para compactar", "SUELO", "de 4", 4, 1, 4, () => CompactVecinos, v => CompactVecinos = (int)v,
              "Cuántos de los cuatro vecinos tienen que ser materia sólida para que un sedimento pueda compactarse en arcilla: es lo que significa \"enterrado\". (R134) Estaba escrito a mano en 3 dentro del stepper, y 3 los tiene también la CARA de un suelo (abajo y los dos lados), así que la superficie de cualquier huerto se volvía arcilla —que no es sustrato— y las plantas perdían la raíz. Con 4, la arcilla se forma bajo tierra, como en la realidad, y la cara del suelo sigue siendo suelo.");
            R("suelo.ablandaHum", "La arcilla se ablanda a", "SUELO", "u", 250, 0, 255, () => AblandaHum, v => AblandaHum = (int)v, "Si la arcilla se empapa hasta aquí, vuelve a ser sedimento (barro).");
            R("suelo.terracotaRaw", "Cocción de la arcilla", "SUELO", "raw (°C = raw·2−120)", 150, 100, 255, () => TerracotaRaw, v => TerracotaRaw = (int)v,
              "Temperatura a la que la arcilla SECA se vuelve terracota (150 raw = 180 °C). La terracota ya no se ablanda ni se erosiona.");
            R("suelo.terracotaHumMax", "Humedad máxima para cocer", "SUELO", "u", 30, 0, 255, () => TerracotaHumMax, v => TerracotaHumMax = (int)v, "La arcilla debe estar casi seca para cocerse; si no, primero se seca (y con calor se seca rápido).");
            R("suelo.abonoCeniza", "Abono de la ceniza", "SUELO", "u", 128, 0, 255, () => AbonoCeniza, v => AbonoCeniza = (int)v, "Cuánta fertilidad deja una celda de ceniza mojada en el sustrato vecino al disolverse.");

            // FUEGO / FUENTES
            R("fuego.hogarRaw", "Temperatura del hogar", "FUEGO", "raw", 170, 100, 255, () => HogarRaw, v => HogarRaw = (int)v,
              "La brasa eterna fija su celda a esta temperatura. (R135, F4) 220 → 170 raw (320 → 220 °C): el hogar es DOMÉSTICO. Hierve agua, seca, enciende, da luz y cuece la superficie de la arcilla, pero NO llega a la temperatura del vidrio ni cuece una vasija por dentro. Para eso hace falta llama contenida en un recinto — o sea, un horno que tú construyas. Es la frontera entre vivir y fabricar, y cuesta un número.");
            R("fuego.vidaHumo", "Vida del humo", "FUEGO", "ticks", 400, 20, 255, () => VidaHumo, v => { VidaHumo = (int)v; VaporVidaCambiado = true; },
              "Cuántos ticks vive una celda de humo antes de disolverse. En el juego son 200 (7 s); súbelo y una bolsa bajo un techo sin salida dura lo bastante para AHOGAR el fuego que la produce (el humo cuenta como aire gastado) y para oscurecer un claro. Es lo que convierte una chimenea en una necesidad en vez de un adorno.");
            R("suelo.permCarbon", "Permeabilidad del carbón", "FUEGO", "0-255", 20, 0, 255, () => PermCarbon, v => PermCarbon = (int)v,
              "El carbón absorbe agua, y mojado NO prende (fuego.fibraMojadaMin). Guardar el combustible bajo el agua o en un rincón húmedo es el granero del laboratorio: no hace falta un cofre, hace falta un charco.");
            R("fuego.hogarCalor", "Calor que inyecta el hogar", "FUEGO", "raw/visita", 40, 0, 80, () => HogarCalor, v => HogarCalor = (int)v, "Cuánto calienta a sus cuatro vecinos en cada visita, además de la difusión.");
            R("fuego.frioRaw", "Temperatura del núcleo frío", "FUEGO", "raw", 30, 0, 70, () => FrioRaw, v => FrioRaw = (int)v, "El bloque frío del catálogo fija su celda a esto (30 raw = −60 °C).");
            R("fuego.frioPotencia", "Frío que inyecta", "FUEGO", "raw/visita", 20, 0, 80, () => FrioPotencia, v => FrioPotencia = (int)v, "Cuánto enfría a sus vecinos por visita.");
            R("fuego.fibraMojadaMin", "Humedad que apaga la fibra", "FUEGO", "u", 100, 0, 255, () => FibraMojadaMin, v => FibraMojadaMin = (int)v, "Un combustible con más agua que esto no prende (ni por contacto ni por temperatura). Sécalo junto al hogar primero.");
            R("fuente.caudal", "Caudal del manantial", "FUEGO", "celdas/s", 24, 0, 150, () => Caudal, v => Caudal = (int)v, "Cuántas celdas de agua emite el manantial por segundo, repartidas entre sus celdas. El sumidero se traga todo lo que le llega.");
            R("fuente.tempRaw", "Temperatura del manantial", "FUEGO", "raw", 70, 0, 255, () => FuenteTempRaw, v => FuenteTempRaw = (int)v, "A qué temperatura nace el agua del manantial (70 = 20 °C). Prueba agua caliente: evapora sola.");

            // TÉRMICA
            R("fuego.vidrioRaw", "El vidrio pide", "FUEGO", "raw (°C = raw·2−120)", 200, 100, 255, () => VidrioRaw, v => VidrioRaw = (int)v,
              "Temperatura a la que la ARENA, con ceniza al lado como fundente, empieza a contar su tiempo al rojo. 200 raw = 280 °C. Es la frontera entre el fuego doméstico y el industrial: el hogar gratuito llega a 170 y una llama suelta cae a ~200 a dos celdas al aire libre. Solo un recinto aislado con carbón sostiene esto en un volumen — o sea, un horno que construyas tú.");
            R("fuego.vidrioVisitas", "Tiempo al rojo para vidriar", "FUEGO", "visitas", 60, 5, 255, () => VidrioVisitas, v => VidrioVisitas = (int)v,
              "Cuántas visitas seguidas (cada 8 ticks) tiene que aguantar la arena por encima de fuego.vidrioRaw, con ceniza al lado, para volverse vidrio. Si la temperatura cae o pierde el fundente, la cuenta se REINICIA: un horno que se enfría a ratos no vidria nunca, por mucho tiempo que pase.");
            R("termica.propia", "Térmica del laboratorio", "TÉRMICA", "0/1", 1, 0, 1, () => TermicaPropia, v => TermicaPropia = (int)v,
              "1 = difusión con conductividad y capacidad por material y convección del aire (la del laboratorio). 0 = la difusión uniforme del juego (todo conduce igual).");
            R("termica.kAire", "Conductividad del aire", "TÉRMICA", "0-16", 4, 0, 16, () => KAire, v => KAire = (int)v, "Qué tan rápido pasa el calor entre celdas de aire (4 = como el juego).");
            R("termica.kAgua", "Conductividad del agua", "TÉRMICA", "0-16", 8, 0, 16, () => KAgua, v => KAgua = (int)v, "El agua reparte el calor rápido dentro de sí.");
            R("termica.kRoca", "Conductividad de la roca", "TÉRMICA", "0-16", 2, 0, 16, () => KRoca, v => KRoca = (int)v, "La roca conduce despacio: aísla.");
            R("termica.kPolvo", "Conductividad de los polvos", "TÉRMICA", "0-16", 3, 0, 16, () => KPolvo, v => KPolvo = (int)v, "Arena, sedimento, ceniza, grava.");
            R("termica.kGas", "Conductividad de los gases", "TÉRMICA", "0-16", 6, 0, 16, () => KGas, v => KGas = (int)v, "Vapor y humo.");
            R("termica.kArcilla", "Conductividad de arcilla/terracota", "TÉRMICA", "0-16", 2, 0, 16, () => KArcilla, v => KArcilla = (int)v, "");
            R("termica.cAire", "Capacidad del aire", "TÉRMICA", "1-8", 1, 1, 8, () => CAire, v => CAire = (int)v, "Cuánto calor hace falta para cambiar la temperatura del aire: poco, cambia rápido.");
            R("termica.cAgua", "Capacidad del agua", "TÉRMICA", "1-8", 4, 1, 8, () => CAgua, v => CAgua = (int)v, "El agua tarda en calentarse y en enfriarse (una poza es un termo).");
            R("termica.cRoca", "Capacidad de la roca", "TÉRMICA", "1-8", 3, 1, 8, () => CRoca, v => CRoca = (int)v, "La roca guarda el calor: sigue tibia cuando el fuego se apaga.");
            R("termica.cPolvo", "Capacidad de los polvos", "TÉRMICA", "1-8", 2, 1, 8, () => CPolvo, v => CPolvo = (int)v, "");
            R("termica.conveccion", "Convección del aire", "TÉRMICA", "0/1", 1, 0, 1, () => Conveccion, v => Conveccion = (int)v, "1 = el aire caliente sube (el techo se calienta antes que el suelo; las chimeneas tiran).");
            R("termica.tiroAmbiente", "Tirón hacia ambiente", "TÉRMICA", "ticks", 32, 8, 256, () => TiroAmbienteTicks, v => TiroAmbienteTicks = (int)v,
              "Cada cuántos ticks cada celda se acerca 1 raw hacia los 20 °C de fondo (la cueva disipa). Más ticks = el calor y el frío duran más.");

            // PRESIÓN
            R("presion.activa", "Presión hidrostática", "PRESIÓN", "0/1", 1, 0, 1, () => PresionActiva, v => PresionActiva = (int)v,
              "1 = los cuerpos de agua conectados igualan sus niveles (vasos comunicantes, sifones, fuentes artesianas). 0 = el agua solo cae y se esparce.");
            R("presion.cadaTicks", "Cada cuántos ticks", "PRESIÓN", "ticks", 2, 1, 30, () => PresionCadaTicks, v => PresionCadaTicks = (int)v, "Frecuencia del paso de presión. Más alto = más barato y más lento.");
            R("presion.celdasPorPaso", "Celdas por paso y cuerpo", "PRESIÓN", "celdas", 4, 1, 32, () => PresionCeldasPorPaso, v => PresionCeldasPorPaso = (int)v, "Cuántas celdas de agua puede subir cada cuerpo de agua en cada paso: la velocidad con la que se igualan los niveles.");
            R("presion.desnivelMin", "Desnivel mínimo", "PRESIÓN", "celdas", 3, 1, 20, () => DesnivelMin, v => DesnivelMin = (int)v, "Diferencia de altura entre superficies del mismo cuerpo a partir de la cual actúa la presión.");
            R("presion.minCeldas", "Tamaño mínimo del cuerpo", "PRESIÓN", "celdas", 6, 1, 200, () => PresionMinCeldas, v => PresionMinCeldas = (int)v, "Charcos más pequeños que esto no cuentan.");

            // LUZ
            R("luz.cadaTicks", "Recalcular luz cada", "LUZ", "ticks", 16, 1, 64, () => LuzCadaTicks, v => LuzCadaTicks = (int)v, "La luz se recalcula entera cada tantos ticks (medido: ~5 ms cada vez sobre el mundo entero — Opus, hito H5: acotar al área del laboratorio).");
            R("luz.decayAire", "Caída de la luz en el aire", "LUZ", "u/celda", 8, 1, 64, () => LuzDecayAire, v => LuzDecayAire = (int)v, "Cuánto pierde la luz por celda de aire en cualquier dirección (8 → alcanza ~30 celdas desde una hoguera).");
            R("luz.decayCielo", "Caída de la luz del cielo hacia abajo", "LUZ", "u/celda", 1, 0, 64, () => LuzDecayCielo, v => LuzDecayCielo = (int)v, "La luz que entra por la boca del cielo cae casi sin perder al bajar en vertical.");
            R("luz.decayAgua", "Caída de la luz en el agua", "LUZ", "u/celda", 20, 1, 128, () => LuzDecayAgua, v => LuzDecayAgua = (int)v, "");
            R("luz.decayHumo", "Caída de la luz en el humo", "LUZ", "u/celda", 24, 1, 128, () => LuzDecayHumo, v => LuzDecayHumo = (int)v,
              "Cuánta luz se come cada celda de humo. (R135) Un fuego bajo un techo sin salida llena el cuarto de humo, el humo tapa la luz del cielo y las plantas de debajo se marchitan: una consecuencia que castiga y enseña a la vez, sin una sola regla dedicada.");
            R("luz.decayPlanta", "Caída de la luz en las plantas", "LUZ", "u/celda", 12, 1, 128, () => LuzDecayPlanta, v => LuzDecayPlanta = (int)v,
              "Las plantas se dan sombra entre sí, y sobre todo A SÍ MISMAS: la luz del cielo baja, así que cada celda de tallo le quita esto a la que tiene debajo. Es el freno del crecimiento (regla 40): una planta deja de subir cuando su propia punta se queda a oscuras. (R134) 40 → 12 tras medir el claro de la cámara alta: con 40 ninguna pasaba de 4 celdas de alto aunque el tope fuese 14. Con 12, bajo la boca del cielo (luz 216) llegan al tope y en el borde del claro (luz 48) apenas levantan una celda: la vegetación toma la forma de la luz.");

            // PLANTAS
            R("planta.germinaPorMil", "Germinación espontánea", "PLANTAS", "‰/visita", 2, 0, 100, () => GerminaPorMil, v => GerminaPorMil = (int)v, "Por cada visita, probabilidad por mil de que un sustrato húmedo e iluminado con aire encima brote solo.");
            R("planta.humedadMin", "Humedad mínima del sustrato", "PLANTAS", "u", 60, 0, 255, () => PlantaHumedadMin, v => PlantaHumedadMin = (int)v, "Por debajo no germina ni bebe.");
            R("planta.luzMin", "Luz mínima", "PLANTAS", "u", 40, 0, 255, () => PlantaLuzMin, v => PlantaLuzMin = (int)v, "Por debajo no germina ni crece.");
            R("planta.bebe", "Lo que bebe la raíz", "PLANTAS", "u/visita", 6, 0, 40, () => PlantaBebe, v => PlantaBebe = (int)v, "Agua que la raíz saca del sustrato en cada visita y guarda como savia.");
            R("planta.crecerSavia", "Savia para crecer", "PLANTAS", "u", 120, 0, 255, () => PlantaCrecerSavia, v => PlantaCrecerSavia = (int)v, "Savia que necesita la punta para añadir una celda.");
            R("planta.pasaSavia", "Savia que sube por celda", "PLANTAS", "u/visita", 40, 0, 255, () => PlantaPasaSavia, v => PlantaPasaSavia = (int)v, "");
            R("planta.altoMax", "Altura máxima", "PLANTAS", "celdas", 14, 1, 60, () => PlantaAltoMax, v => PlantaAltoMax = (int)v, "");
            R("planta.ramaPct", "Probabilidad de ramificar", "PLANTAS", "%", 8, 0, 100, () => PlantaRamaPct, v => PlantaRamaPct = (int)v, "");
            R("planta.marchitaVisitas", "Visitas sin savia para morir", "PLANTAS", "visitas", 40, 1, 255, () => PlantaMarchitaVisitas, v => PlantaMarchitaVisitas = (int)v, "Una celda de planta sin savia durante tantas visitas se seca: se vuelve FIBRA (combustible) y cae.");
            R("planta.fertilidadBonus", "Bonus de fertilidad", "PLANTAS", "%", 50, 0, 300, () => PlantaFertilidadBonusPct, v => PlantaFertilidadBonusPct = (int)v,
              "Cuánto más rápido crece una planta sobre sustrato con fertilidad 255 (ceniza, plantas muertas). (R134) El bonus se aplica a lo que BEBE la raíz, no a un contador de velocidad aparte: es local, es barato y se ve igual (más savia por visita = la punta alcanza antes lo que necesita para crecer).");
            R("planta.transpira", "Transpiración", "PLANTAS", "u/visita", 2, 0, 40, () => PlantaTranspira, v => PlantaTranspira = (int)v,
              "Cuánta savia suelta al aire cada celda de planta por visita (solo si el aire no está saturado). Es el SUMIDERO que hace que una planta muera cuando el suelo se seca: sin él, la savia se quedaría dentro para siempre y ninguna planta se marchitaría nunca. Y tiene consecuencia: un rincón plantado HUMEDECE el aire, así que condensa más. Una planta es una bomba que sube agua del suelo al aire.");
            R("planta.abonoMuerte", "Abono de la planta muerta", "PLANTAS", "u", 40, 0, 255, () => PlantaAbonoMuerte, v => PlantaAbonoMuerte = (int)v,
              "Cuánta fertilidad deja una planta al sustrato que tenía debajo cuando se marchita. Es lo que hace que un sitio donde ya creció algo sea mejor para lo siguiente.");

            // CUERPOS
            R("cuerpo.activo", "Cuerpos cohesionados", "CUERPOS", "0/1", 1, 0, 1, () => CuerposActivos, v => CuerposActivos = (int)v, "1 = la roca suelta cae como bloque entero y se fractura.");
            R("cuerpo.fracturaCaida", "Caída que fractura", "CUERPOS", "celdas", 6, 1, 40, () => FracturaCaida, v => FracturaCaida = (int)v, "Un bloque que cae más de esto se rompe al aterrizar.");
            R("cuerpo.golpes", "Golpes de cincel", "CUERPOS", "golpes", 3, 1, 10, () => Golpes, v => Golpes = (int)v, "Golpes de cincel que aguanta una celda de roca suelta antes de desprenderse.");
            R("cuerpo.fracturaPct", "Fracción que se rompe", "CUERPOS", "%", 35, 0, 100, () => FracturaPct, v => FracturaPct = (int)v, "Al fracturarse, porcentaje de celdas del bloque que se vuelven grava.");
        }

        /// <summary>true cuando el panel cambió VaporVida: AlkahestSim/LabPanel lo aplican al MaterialDef del vapor y lo bajan.</summary>
        public static bool VaporVidaCambiado;

        private static void R(string clave, string nombre, string grupo, string unidad, float def, float min, float max,
            Func<float> leer, Action<float> escribir, string ayuda, bool entero = true, bool reconstruir = false)
        {
            Registro.Add(new LabParam
            {
                Clave = clave, Nombre = nombre, Grupo = grupo, Unidad = unidad, Def = def, Min = min, Max = max,
                Leer = leer, Escribir = escribir, Ayuda = ayuda, Entero = entero, RequiereReconstruir = reconstruir,
            });
        }

        public static LabParam Buscar(string clave)
        {
            for (int i = 0; i < Registro.Count; i++) if (Registro[i].Clave == clave) return Registro[i];
            return null;
        }

        public static void RestaurarDefaults()
        {
            for (int i = 0; i < Registro.Count; i++) Registro[i].Escribir(Registro[i].Def);
        }

        /// <summary>Saturación de vapor del aire a una temperatura raw (ver satBase/satPorGrado).</summary>
        public static int Saturacion(int tempRaw)
        {
            int s = SatBase + (tempRaw - CellGrid.AmbientRaw) * SatPorGrado;
            if (s < 4) s = 4; else if (s > 255) s = 255;
            return s;
        }
    }
}
