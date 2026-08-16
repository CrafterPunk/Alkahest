using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// [ChaosAlchemy · playtest 24, "LA MAREA"] EL DIRECTOR DEL ARCO --
    /// CONTRATO_MAREA.md §4.2. No pinta nada, no toca la grilla directamente
    /// (salvo encender <see cref="SimStepper.MareaActiva"/>, el único gate
    /// que le pertenece): SOLO sondea, cada 2 segundos, y decide tres cosas
    /// -- cuándo DESPIERTA el corazón, cuándo se GANA (Rocío hasta el
    /// corazón) y cuándo se PIERDE (la marea despierta engulle a la última
    /// criatura).
    ///
    /// MISMO PATRÓN QUE TODA ESTA CAPA: sondeo con acumulador
    /// (<see cref="IntervaloSondeo"/>), NUNCA un escaneo por frame -- calcado
    /// del `IntervaloSondeo`/`_accPoll` de Game/Criatura.cs. Cero
    /// allocations: los tres barridos (victoria, primer Rocío, marea
    /// subiendo) son bucles `for` sobre `AlkahestSim.SampleMaterial`, ningún
    /// `new` por sondeo.
    ///
    /// REFERENCIAS COMPARTIDAS CON EL OTRO ENCARGO DE ESTA RONDA (Sim/,
    /// paralelo): <see cref="MaterialId.Marea"/>/<see cref="MaterialId.Rocio"/>
    /// y las cuatro constantes `SimLevelBuilder.CorazonMarea*` se escriben
    /// tal cual, EXACTAS, tal y como las fija CONTRATO_MAREA.md §2/§3.4 --
    /// hoy pueden no compilar todavía si ese encargo paralelo no ha
    /// terminado, pero el contrato es la única fuente de verdad compartida y
    /// NO se inventan sustitutos en este archivo.
    ///
    /// TRES PISTAS, UNA VEZ CADA UNA (CONTRATO_MAREA.md §4.5): el director
    /// es el ÚNICO que las dispara, siempre vía
    /// <see cref="HintSystem.EncolarPistaDeMarea"/> -- nunca las repite (los
    /// tres flags de "ya vista" son campos de instancia, y el director es un
    /// singleton de facto: uno por partida, creado en
    /// AlkahestGameBootstrap.TrySpawn).
    ///
    /// "UN LATIDO GRAVE" (al despertar, ver CONTRATO_MAREA.md §4.2): este
    /// encargo NO tiene propiedad de Audio/DirectorDeAudio.cs (fuera de la
    /// lista de archivos), así que el latido de audio queda ANOTADO aquí
    /// como trabajo pendiente para quien sí tenga esa propiedad -- el
    /// despertar SÍ se anuncia por completo vía el sistema de pistas
    /// (obligatorio del contrato), que es la mitad que este archivo puede
    /// cumplir por su cuenta.
    /// </summary>
    public sealed class MareaDirector : MonoBehaviour
    {
        // -----------------------------------------------------------------
        // CONFIG — despertar (CONTRATO_MAREA.md §4.2, primer párrafo).
        // -----------------------------------------------------------------
        private const int UmbralCeldasTalladasDespertar = 12;
        private const float UmbralSegundosJugadosDespertar = 300f;

        // -----------------------------------------------------------------
        // CONFIG — sondeo (nunca por frame, ver docblock de la clase).
        // -----------------------------------------------------------------
        private const float IntervaloSondeo = 2f;

        // -----------------------------------------------------------------
        // CONFIG — victoria/derrota.
        // -----------------------------------------------------------------
        private const int UmbralVictoriaRocioCeldas = 24;

        // -----------------------------------------------------------------
        // CONFIG — pista "la marea sube" (CONTRATO_MAREA.md §4.5, tercer
        // momento): "por encima de y = SotanoY1-20" -- en este mundo la
        // coordenada Y crece hacia ARRIBA (SurfaceFloorY0 = CellGrid.H/2 vive
        // POR ENCIMA de SotanoY1 en el plano, ver Sim/SimLevelBuilder.cs), así
        // que "sube hacia la superficie" es simplemente `y` creciendo.
        // -----------------------------------------------------------------
        private const int MargenAltura = 20;

        private AlkahestSim _sim;
        private DayCycle _dayCycle;
        private HintSystem _hints;

        private bool _despierta;
        private bool _terminada; // evita llamar dos veces a TerminarPartida si dos condiciones coinciden en el mismo sondeo.

        /// <summary>Segundos de partida JUGABLE acumulados (mismo criterio que HintSystem._playSeconds: solo cuenta mientras !DayCycle.InputLocked, nunca durante overlays de jornada/título).</summary>
        private float _segundosJugados;

        private float _accSondeo;

        // Las tres pistas del arco, una vez cada una (ver docblock de la clase).
        private bool _pistaPrimerRocioEnviada;
        private bool _pistaMareaSubeEnviada;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap, mismo patrón que el resto de esta capa.</summary>
        public void Init(AlkahestSim sim, DayCycle dayCycle, HintSystem hints)
        {
            _sim = sim;
            _dayCycle = dayCycle;
            _hints = hints;
        }

        private void Update()
        {
            if (_sim == null || _sim.Stepper == null || _dayCycle == null || _terminada) return;

            // "300s de partida jugable" (CONTRATO_MAREA.md §4.2): mismo
            // criterio que HintSystem._playSeconds -- el reloj del director
            // no corre mientras Título/intro/fin de jornada/pantalla final
            // tienen el input bloqueado.
            if (!DayCycle.InputLocked) _segundosJugados += Time.deltaTime;

            if (!_despierta) ComprobarDespertar();

            _accSondeo += Time.deltaTime;
            if (_accSondeo >= IntervaloSondeo)
            {
                _accSondeo -= IntervaloSondeo;
                Sondear();
            }
        }

        /// <summary>
        /// Despierta la marea UNA sola vez (<see cref="SimStepper.MareaActiva"/>
        /// = true) en cuanto se cumple CUALQUIERA de las dos condiciones del
        /// contrato: 12 celdas REALMENTE talladas (<see cref="Cincel.CeldasTalladas"/>,
        /// nunca rellenadas -- "has empezado a abrir el mundo: el mundo
        /// también se abre hacia ti") o 300s de partida jugable ("aunque no
        /// caves, el tiempo también despierta el corazón"). Anuncia por el
        /// sistema de pistas -- "nada de pantallazos", el contrato es
        /// explícito en que este momento no interrumpe con un overlay.
        /// </summary>
        private void ComprobarDespertar()
        {
            if (Cincel.CeldasTalladas < UmbralCeldasTalladasDespertar
                && _segundosJugados < UmbralSegundosJugadosDespertar) return;

            _despierta = true;
            _sim.Stepper.MareaActiva = true;
            if (_hints != null)
                _hints.EncolarPistaDeMarea("Algo se ha despertado abajo. El agua del fondo ya no es agua.");
            // Ver el docblock de la clase, "UN LATIDO GRAVE": el audio queda
            // fuera de la propiedad de este encargo, anotado ahí.
        }

        /// <summary>
        /// El único sondeo de esta clase, cada <see cref="IntervaloSondeo"/>=2s
        /// (CONTRATO_MAREA.md §4.2, "Sondeo"): victoria, derrota, y las dos
        /// pistas del arco que dependen de observar el mundo (la tercera,
        /// "al despertar", vive en <see cref="ComprobarDespertar"/>).
        /// </summary>
        private void Sondear()
        {
            // VICTORIA: celdas de Rocío dentro del rect del corazón.
            int rocioEnCorazon = ContarMaterialEnRect(MaterialId.Rocio,
                SimLevelBuilder.CorazonMareaX0, SimLevelBuilder.CorazonMareaX1,
                SimLevelBuilder.CorazonMareaY0, SimLevelBuilder.CorazonMareaY1);

            // "PRIMER ROCÍO" (fix de integración sobre §4.5): la pista se
            // dispara en cuanto CUALQUIER criatura exuda Rocío por primera
            // vez (Criatura.RocioExudado, marcado en CompletarDigestion, el
            // lugar donde ocurre de verdad) -- no cuando el Rocío llega al
            // corazón, que sería demasiado tarde para enseñar nada. Cero
            // coste: un bool estático, ningún barrido extra.
            if (!_pistaPrimerRocioEnviada && Criatura.RocioExudado)
            {
                _pistaPrimerRocioEnviada = true;
                if (_hints != null)
                    _hints.EncolarPistaDeMarea("Eso que exuda tu criatura HIERE a la marea. Recuérdalo.");
            }

            if (rocioEnCorazon >= UmbralVictoriaRocioCeldas)
            {
                _terminada = true;
                _dayCycle.TerminarPartida(victoria: true);
                return;
            }

            // DERROTA: la marea ya está despierta y no queda ninguna
            // criatura viva -- el sondeo de 2s es de sobra: no hace falta
            // reaccionar en el mismo frame en que muere la última.
            if (_despierta && Criatura.NumVivas == 0)
            {
                _terminada = true;
                _dayCycle.TerminarPartida(victoria: false);
                return;
            }

            // "LA MAREA SUBE": barrido ACOTADO a las columnas del sótano
            // (x = SotanoX0..SotanoX1 -- hoy la única franja donde la marea
            // puede existir, nace en el corazón dentro del sótano) buscando
            // la primera celda de Marea por encima de y = SotanoY1-MargenAltura.
            // Se apaga solo (el flag) en cuanto se dispara una vez: no hace
            // falta seguir barriendo tras la primera vez que sube.
            if (!_pistaMareaSubeEnviada && HayMareaPorEncimaDe(SimLevelBuilder.SotanoY1 - MargenAltura))
            {
                _pistaMareaSubeEnviada = true;
                if (_hints != null)
                    _hints.EncolarPistaDeMarea("La marea sube. La piedra la frena; el cincel ya no es solo una herramienta.");
            }
        }

        /// <summary>Cuenta celdas de `mat` dentro de un rectángulo inclusivo. Usado para el conteo de victoria (rect del corazón, ~22x6=132 celdas -- trivial cada 2s).</summary>
        private int ContarMaterialEnRect(byte mat, int x0, int x1, int y0, int y1)
        {
            int n = 0;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (_sim.SampleMaterial(x, y) == mat) n++;
            return n;
        }

        /// <summary>¿Hay alguna celda de Marea con y &gt; yUmbral, dentro de las columnas del sótano? Acotado a SotanoX0..SotanoX1/SotanoY1 (ver Sondear) -- unos pocos miles de celdas como mucho, y solo hasta la primera vez que encuentra una (el llamante apaga el sondeo con un flag tras el primer true).</summary>
        private bool HayMareaPorEncimaDe(int yUmbral)
        {
            for (int y = yUmbral + 1; y <= SimLevelBuilder.SotanoY1; y++)
            {
                for (int x = SimLevelBuilder.SotanoX0; x <= SimLevelBuilder.SotanoX1; x++)
                {
                    if (_sim.SampleMaterial(x, y) == MaterialId.Marea) return true;
                }
            }
            return false;
        }
    }
}
