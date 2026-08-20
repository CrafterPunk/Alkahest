using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// EL ANCLAJE — archivo NUEVO (playtest 33, feedback de Cesar sobre el
    /// sistema de baldas: *"créale a las baldas un sistema de construcción:
    /// donde quiera colocar cositas para sostener las cosas que voy
    /// obteniendo... unos cuadraditos pequeños que sea el nuevo sistema"*).
    ///
    /// =====================================================================
    /// QUÉ ES
    /// =====================================================================
    /// EL CUADRADO: una pieza IMovible de 2x2 celdas que el jugador agarra
    /// con Mudanza (V) y suelta donde quiera. Es, literalmente, el mismo
    /// patrón que las cinco estaciones y el Alambique -- "plataforma
    /// soberana en miniatura" -- reducido a su expresión mínima: no tiene
    /// verbo, ni foco de E, ni HUD propio salvo el rótulo del depósito (ver
    /// <see cref="SpawnDeposito"/>). Su única función es SER PIEDRA donde el
    /// jugador la ponga, con un remate de latón encima para que se lea como
    /// una pieza deliberada y no como cincelado a mano.
    ///
    /// =====================================================================
    /// LA REGLA DE SUSTITUCIÓN (decisión de diseño, "OR BETTER" del encargo)
    /// =====================================================================
    /// El encargo ofrecía dos opciones: "no debería poder colocarse sobre
    /// bedrock" O, mejor, "que lo sustituya, así el jugador puede tallar
    /// esquinas perfectas". Se elige la segunda -- es estrictamente más
    /// flexible y no exige ningún caso especial en <see cref="CabeEnAncla"/>:
    /// el anclaje se puede soltar en CUALQUIER celda dentro del marco del
    /// mundo, sea aire, piedra u otra cosa.
    ///
    /// Al TALLAR (<see cref="TallarAqui"/>), cada una de las <see cref="Lado"/>
    /// x <see cref="Lado"/> celdas se sondea ANTES de tocarla:
    ///   · si YA es <see cref="MaterialId.Stone"/>, se marca
    ///     <c>_origenEraPiedra[i]=true</c> y NO SE TOCA -- tallar piedra sobre
    ///     piedra no cambiaría nada, así que el registro basta para saber qué
    ///     hacer al quitarlo.
    ///   · si no, se marca <c>false</c> y se CREA piedra nueva con
    ///     <see cref="AlkahestSim.PaintStable"/> (regla 29 de CLAUDE.md: esto
    ///     CREA materia que no existía).
    /// Al BORRAR (<see cref="BorrarAqui"/>, llamado por <see cref="Reposicionar"/>
    /// antes de tallar la nueva posición), se recorre el mismo registro: las
    /// celdas que YA eran piedra se dejan intactas (no dejan agujero: seguían
    /// siendo la roca original) y solo las que el anclaje creó de la nada
    /// vuelven a <see cref="MaterialId.Empty"/> vía <see cref="AlkahestSim.Paint"/>
    /// (regla 29: esto QUITA lo que se había creado).
    /// </summary>
    public sealed class Anclaje : MonoBehaviour, IMovibleAnclaEsquina
    {
        /// <summary>Lado del cuadrado, en celdas. Público: Sim/SimLevelBuilder.cs no lo necesita (los anclajes no se tallan en el génesis, ver el docblock de la clase), pero Game/Balda.cs sí lo usa para dimensionar sus dos remates decorativos con la MISMA medida (regla 39/47 de CLAUDE.md: una sola fuente de verdad).</summary>
        public const int Lado = 2;

        private const float RangoNombrePleno = 3.0f;
        private const float RangoNombreDesvanece = 4.2f;
        private const string TextoRotuloDeposito = "anclajes — llévalos donde los necesites (V)";

        private AlkahestSim _sim;
        private Transform _player;

        private int _x0, _y0; // esquina inferior izquierda -- IMovibleAnclaEsquina.
        private Vector3 _centro;

        /// <summary>Por celda del cuadrado (orden y*Lado+x, relativo a _x0/_y0): ¿ya era piedra ANTES de que este anclaje la tallara? Ver el docblock de la clase, "LA REGLA DE SUSTITUCIÓN".</summary>
        private readonly bool[] _origenEraPiedra = new bool[Lado * Lado];

        /// <summary>Handle en SimLevelBuilder.ObraDelTaller -- mismo patrón que Game/ColumnaEnsayo.cs/Game/Alambique.cs (anti-cincel).</summary>
        private int _handleObra = -1;

        // ---- Rótulo del depósito (ver SpawnDeposito): solo la PRIMERA
        // instancia de la pila lo dibuja, para no repetir el mismo texto seis
        // veces apiladas. ----
        private bool _esPrimeroDeLaPila;
        private Vector3 _centroPila;

        // ---- IMovibleAnclaEsquina ----
        public Vector3 CentroMundo => _centro;
        public Vector2 TamanoMundo => new Vector2(Lado * SimRenderer.CellWorldSize, Lado * SimRenderer.CellWorldSize);
        public Vector2Int AnclaCelda => new Vector2Int(_x0, _y0);

        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            // Sin excepción de "hay piedra debajo": la regla de sustitución
            // (ver docblock de la clase) hace que CUALQUIER celda dentro del
            // marco protegido del mundo sea un sitio válido.
            return anclaCelda.x >= 1 && anclaCelda.x + Lado - 1 <= CellGrid.W - 2
                && anclaCelda.y >= 1 && anclaCelda.y + Lado - 1 <= CellGrid.H - 2;
        }

        /// <summary>Inyección de dependencias -- llamado por <see cref="SpawnDeposito"/>. `esPrimeroDeLaPila`/`centroPila` solo los usa el rótulo (ver OnGUI); el resto de la clase los ignora.</summary>
        public void Init(AlkahestSim sim, Transform player, int x0, int y0, bool esPrimeroDeLaPila, Vector3 centroPila)
        {
            _sim = sim;
            _player = player;
            _x0 = x0;
            _y0 = y0;
            _esPrimeroDeLaPila = esPrimeroDeLaPila;
            _centroPila = centroPila;

            RecalcularCentro();
            TallarAqui();

            // Mismo patrón de reclamo que Game/ColumnaEnsayo.cs/Game/Alambique.cs:
            // si esta MISMA celda ya estuviera registrada (no debería, un
            // anclaje nace en aire libre), se reclama en vez de duplicar.
            int existente = SimLevelBuilder.HallarObraExacta(_x0, _y0, _x0 + Lado - 1, _y0 + Lado - 1);
            _handleObra = existente >= 0 ? existente : SimLevelBuilder.RegistrarObra(_x0, _y0, _x0 + Lado - 1, _y0 + Lado - 1);

            Mudanza.RegistrarMovible(this);
            BuildVisual();
        }

        private void RecalcularCentro()
        {
            float c = SimRenderer.CellWorldSize;
            _centro = new Vector3((_x0 + Lado * 0.5f) * c, (_y0 + Lado * 0.5f) * c, 0f);
            transform.position = _centro;
        }

        /// <summary>Talla las <see cref="Lado"/>x<see cref="Lado"/> celdas en `_x0,_y0` -- ver "LA REGLA DE SUSTITUCIÓN" en el docblock de la clase.</summary>
        private void TallarAqui()
        {
            for (int dy = 0; dy < Lado; dy++)
            {
                for (int dx = 0; dx < Lado; dx++)
                {
                    int x = _x0 + dx, y = _y0 + dy;
                    int i = dy * Lado + dx;
                    bool eraPiedra = _sim.SampleMaterial(x, y) == MaterialId.Stone;
                    _origenEraPiedra[i] = eraPiedra;
                    if (!eraPiedra) _sim.PaintStable(x, y, 0, MaterialId.Stone); // CREA piedra nueva (regla 29).
                }
            }
        }

        /// <summary>Deshace <see cref="TallarAqui"/> en `x0,y0` (la posición VIEJA, antes de moverse) -- las celdas que ya eran piedra se dejan tal cual, nunca dejan hueco.</summary>
        private void BorrarAqui(int x0, int y0)
        {
            for (int dy = 0; dy < Lado; dy++)
            {
                for (int dx = 0; dx < Lado; dx++)
                {
                    int i = dy * Lado + dx;
                    if (_origenEraPiedra[i]) continue; // ya era piedra: no se toca, no deja agujero.
                    _sim.Paint(x0 + dx, y0 + dy, 0, MaterialId.Empty); // QUITA lo que este anclaje había creado (regla 29).
                }
            }
        }

        public void Reposicionar(Vector2Int anclaCelda)
        {
            int viejoX0 = _x0, viejoY0 = _y0;
            BorrarAqui(viejoX0, viejoY0); // 1) borrar en la posición VIEJA, con el registro de origen todavía intacto.

            _x0 = anclaCelda.x;
            _y0 = anclaCelda.y;
            RecalcularCentro();
            TallarAqui(); // 2) tallar en la nueva -- regla 36: nunca Init/BuildVisual otra vez, solo Reposicionar.

            SimLevelBuilder.ActualizarObra(_handleObra, _x0, _y0, _x0 + Lado - 1, _y0 + Lado - 1); // 3) actualizar el registro anticincel.
        }

        private void OnDestroy()
        {
            Mudanza.OlvidarMovible(this);
        }

        private void BuildVisual()
        {
            float c = SimRenderer.CellWorldSize;
            MaquinariaSprites.CrearCapa(transform, "Sprite", MaquinariaSprites.CuadradoAnclaje(), 18, Lado * c, Lado * c);
        }

        /// <summary>
        /// (playtest 33) EL DEPÓSITO: seis anclajes EXTRA, apilados en fila
        /// cerca de la estantería de redomas (elección del encargo: "en un
        /// sitio visible del taller"), listos para que el jugador se los
        /// lleve donde haga falta. Se deriva de
        /// <see cref="SimLevelBuilder.EstanteX0"/>/<see cref="SimLevelBuilder.EstanteBaseY"/>
        /// (regla 39/47 de CLAUDE.md: nunca coordenadas sueltas) con un
        /// desnivel vertical fijo que los deja muy por debajo de la galería
        /// de baldas de esa misma zona (`SimLevelBuilder.BaldaPlanes[0]`,
        /// y=228) y muy por encima del propio mueble de redomas -- en el aire
        /// abierto del cuarto, sin tocar mampostería existente.
        ///
        /// Llamado UNA sola vez (ver el flag <see cref="_yaCreado"/>) desde
        /// <see cref="Mudanza.Init"/> -- el único punto de la capa jugable que
        /// es un archivo PERMITIDO en este encargo y que recibe una
        /// referencia a <see cref="AlkahestSim"/>. Ver el docblock de
        /// Mudanza.Init para por qué ahí y no en AlkahestGameBootstrap.cs
        /// (prohibido en este encargo) y por qué el guardado es doblemente
        /// seguro (flag estático + host-only).
        /// </summary>
        private static bool _yaCreado;

        /// <summary>
        /// (integración pt54, LA FUGA DE PARIDAD) Resetea la guarda estática
        /// "ya creadas". Sin esto, quien entrara SEGUNDO a un modo dentro del
        /// mismo proceso (solo→multi o viceversa, sin recarga de dominio) se
        /// quedaba sin estos muebles: la causa raíz de "las dos versiones de
        /// la seed 0 no son iguales" que reportó Cesar. Lo llama el bootstrap
        /// junto a MachineFocus.Limpiar() en cada arranque de mundo.
        /// </summary>
        public static void ResetGuardaEstatica() { _yaCreado = false; }

        private const int DepositoDesnivel = 14; // celdas por encima de EstanteBaseY -- ver doc de arriba.
        private const int DepositoCount = 6;
        private const int DepositoPaso = Lado + 1; // 2 celdas de anclaje + 1 de respiro entre cada uno.

        public static void SpawnDeposito(AlkahestSim sim, Transform player)
        {
            if (_yaCreado) return;
            _yaCreado = true;

            int baseX = SimLevelBuilder.EstanteX0;
            int baseY = SimLevelBuilder.EstanteBaseY + DepositoDesnivel;

            float c = SimRenderer.CellWorldSize;
            var centroPila = new Vector3((baseX + DepositoPaso * (DepositoCount - 1) * 0.5f + Lado * 0.5f) * c, (baseY + Lado + 2f) * c, 0f);

            for (int i = 0; i < DepositoCount; i++)
            {
                var go = new GameObject("AnclajeDeposito" + i);
                var a = go.AddComponent<Anclaje>();
                a.Init(sim, player, baseX + i * DepositoPaso, baseY, i == 0, centroPila);
            }
        }

        private void OnGUI()
        {
            if (!_esPrimeroDeLaPila || _sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return;

            float cercania = UiStyles.Cercania(_centroPila, _player, RangoNombrePleno + 4f, RangoNombreDesvanece + 4f);
            if (cercania <= 0f) return;

            UiStyles.Preparar();
            Color tenue = UiStyles.TextoTenue;
            UiStyles.PlacaMundo(_centroPila, TextoRotuloDeposito, new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercania), UiStyles.S(20f));
        }
    }
}
