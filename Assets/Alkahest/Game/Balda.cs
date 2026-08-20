using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// LA BALDA — archivo NUEVO (playtest 33, feedback de Cesar sobre
    /// `Sim/SimLevelBuilder.Repisas`: *"las baldas deberían ser más angostas...
    /// las ménsulas triangulares deberían ser unos cuadraditos pequeños"*).
    ///
    /// =====================================================================
    /// QUÉ CAMBIA RESPECTO A LA VIEJA `Repisas`
    /// =====================================================================
    ///  · DOS filas de piedra maciza -> UNA sola (más angosta, tal como pide
    ///    el encargo).
    ///  · Tramos largos (10-27 celdas) -> segmentos de hasta
    ///    <see cref="SimLevelBuilder.BaldaLargoMax"/> (12) celdas, con huecos
    ///    entre ellos (ver <see cref="SimLevelBuilder.BaldaPlanes"/>).
    ///  · Ménsulas triangulares de latón (`MaquinariaSprites.MensulaInclinada`,
    ///    montadas por Game/WorkshopBackdrop.cs) -> un CUADRADO de latón en
    ///    cada extremo (`MaquinariaSprites.CuadradoAnclaje`, el mismo sprite
    ///    que usa <see cref="Anclaje"/>), montado por ESTA clase -- la balda
    ///    ahora se viste a sí misma, como las cinco estaciones, en vez de que
    ///    Game/WorkshopBackdrop.cs itere una tabla ajena (ese archivo queda
    ///    intacto: `SimLevelBuilder.Repisas` se vacía, ver el docblock de esa
    ///    constante, así que su bucle de vestido de baldas pasa a ser un
    ///    no-op seguro).
    ///  · Movible como bloque único (IMovibleAnclaEsquina, mismo patrón que
    ///    Game/ColumnaEnsayo.cs): agarrar con V mueve la piedra Y sus dos
    ///    remates de una vez.
    ///
    /// LOS CUADRADOS DE LOS EXTREMOS SON DECORACIÓN, NO <see cref="Anclaje"/>
    /// POR SEPARADO: la "lectura" que pide el encargo ("estos anclajes evitan
    /// que se caiga") es del cuadrado como SÍMBOLO -- la piedra real de la
    /// balda ya no se cae nunca (regla 7 de CLAUDE.md: la piedra no tiene
    /// gravedad), así que un Anclaje real ahí no añadiría ninguna física
    /// nueva, solo duplicaría el registro de red y la lista de Mudanza por
    /// cada balda. Lo "movible por separado" que pide el encargo lo cumplen
    /// los <see cref="Anclaje"/> SUELTOS del depósito (ver
    /// Anclaje.SpawnDeposito): estos sí son piezas independientes que el
    /// jugador arrastra donde quiera para construir con ellas.
    /// </summary>
    public sealed class Balda : MonoBehaviour, IMovibleAnclaEsquina
    {
        /// <summary>Ancho del remate de cada extremo, en celdas -- igual que <see cref="Anclaje.Lado"/> (misma pieza visual, misma medida, regla 39/47 de CLAUDE.md).</summary>
        private const int RemateLado = Anclaje.Lado;

        private AlkahestSim _sim;

        private int _x0, _x1, _y; // fila única -- IMovibleAnclaEsquina ancla en (_x0, _y).
        private Vector3 _centro;

        private int _handleObra = -1;

        public Vector3 CentroMundo => _centro;
        public Vector2 TamanoMundo => new Vector2((_x1 - _x0 + 1) * SimRenderer.CellWorldSize, SimRenderer.CellWorldSize);
        public Vector2Int AnclaCelda => new Vector2Int(_x0, _y);

        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            int span = _x1 - _x0 + 1;
            return anclaCelda.x >= 1 && anclaCelda.x + span - 1 <= CellGrid.W - 2
                && anclaCelda.y >= 1 && anclaCelda.y <= CellGrid.H - 2;
        }

        /// <summary>
        /// (génesis) Talla la fila única de piedra de `x0` a `x1` en `y` y
        /// registra su obra anticincel -- mismo patrón que
        /// Game/ColumnaEnsayo.cs::TallarEnPlano: corre ANTES de que exista
        /// ninguna instancia (ver Sim/SimLevelBuilder.TallarRepisas), así que
        /// el registro tiene que pasar por aquí; <see cref="Init"/> lo
        /// RECLAMA después en vez de crear uno nuevo.
        /// </summary>
        public static void TallarEnPlano(CellGrid grid, int x0, int x1, int y)
        {
            for (int x = x0; x <= x1; x++)
                if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Stone);
            SimLevelBuilder.RegistrarObra(x0, y, x1, y);
        }

        /// <summary>Inyección de dependencias -- llamada por <see cref="SimLevelBuilder.BaldaPlanes"/>-driven spawn (ver Game/Mudanza.cs, `SpawnBaldasYAnclajes`).</summary>
        public void Init(AlkahestSim sim, int x0, int x1, int y)
        {
            _sim = sim;
            _x0 = x0; _x1 = x1; _y = y;

            RecalcularCentro();

            int existente = SimLevelBuilder.HallarObraExacta(_x0, _y, _x1, _y);
            _handleObra = existente >= 0 ? existente : SimLevelBuilder.RegistrarObra(_x0, _y, _x1, _y);

            Mudanza.RegistrarMovible(this);
            BuildVisual();
        }

        private void RecalcularCentro()
        {
            float c = SimRenderer.CellWorldSize;
            _centro = new Vector3((_x0 + (_x1 - _x0 + 1) * 0.5f) * c, (_y + 0.5f) * c, 0f);
            transform.position = _centro;
        }

        private void TallarEnCaliente()
        {
            for (int x = _x0; x <= _x1; x++)
                _sim.PaintStable(x, _y, 0, MaterialId.Stone); // CREA la piedra en el sitio nuevo (regla 29).
        }

        private void BorrarEnCaliente(int x0, int x1, int y)
        {
            for (int x = x0; x <= x1; x++)
                _sim.Paint(x, y, 0, MaterialId.Empty); // QUITA la piedra del sitio viejo (regla 29).
        }

        public void Reposicionar(Vector2Int anclaCelda)
        {
            BorrarEnCaliente(_x0, _x1, _y); // 1) borrar la fila VIEJA.

            int span = _x1 - _x0 + 1;
            _x0 = anclaCelda.x;
            _x1 = _x0 + span - 1;
            _y = anclaCelda.y;
            RecalcularCentro();
            TallarEnCaliente(); // 2) tallar la nueva -- regla 36: nunca Init/BuildVisual otra vez.

            SimLevelBuilder.ActualizarObra(_handleObra, _x0, _y, _x1, _y); // 3) actualizar el registro anticincel.
        }

        private void OnDestroy()
        {
            Mudanza.OlvidarMovible(this);
        }

        /// <summary>
        /// Instancia UNA vez todas las baldas de <see cref="SimLevelBuilder.BaldaPlanes"/>
        /// (la piedra YA la talló el génesis, ver <see cref="TallarEnPlano"/>
        /// vía Sim/SimLevelBuilder.TallarRepisas -- aquí solo se crea el
        /// MonoBehaviour que la viste y la hace movible). Guardado estático
        /// "ya creadas": llamado desde Game/Mudanza.cs, ver el docblock largo
        /// de ese archivo (`SpawnBaldasYAnclajesSiCorresponde`) para por qué
        /// hace falta esta guarda además de la de host-only que ya aplica el
        /// llamante.
        /// </summary>
        private static bool _todasCreadas;

        /// <summary>
        /// (integración pt54, LA FUGA DE PARIDAD) Resetea la guarda estática
        /// "ya creadas". Sin esto, quien entrara SEGUNDO a un modo dentro del
        /// mismo proceso (solo→multi o viceversa, sin recarga de dominio) se
        /// quedaba sin estos muebles: la causa raíz de "las dos versiones de
        /// la seed 0 no son iguales" que reportó Cesar. Lo llama el bootstrap
        /// junto a MachineFocus.Limpiar() en cada arranque de mundo.
        /// </summary>
        public static void ResetGuardaEstatica() { _todasCreadas = false; }

        public static void SpawnTodas(AlkahestSim sim)
        {
            if (_todasCreadas) return;
            _todasCreadas = true;

            var planes = SimLevelBuilder.BaldaPlanes;
            for (int i = 0; i < planes.Length; i++)
            {
                var p = planes[i];
                var go = new GameObject("Balda" + i);
                var b = go.AddComponent<Balda>();
                b.Init(sim, p.X0, p.X1, p.Y);
            }
        }

        /// <summary>
        /// La piedra real ya la dibuja SimRenderer sola (es CellGrid.Stone,
        /// regla 19); esta capa añade SOLO lo que la hace leerse como MUEBLE
        /// y no como una raya de roca: la losa clara de
        /// `MaquinariaSprites.BaldaPiedra` encima, un filo de latón bajo el
        /// canto, y los dos cuadrados de anclaje en los extremos (ver el
        /// docblock de la clase, "LOS CUADRADOS... SON DECORACIÓN").
        /// </summary>
        private void BuildVisual()
        {
            float c = SimRenderer.CellWorldSize;
            int span = _x1 - _x0 + 1;

            MaquinariaSprites.CrearCapa(transform, "Piedra", MaquinariaSprites.BaldaPiedra(span, 1), 18, span * c, c);

            var filoGo = new GameObject("Filo");
            filoGo.transform.SetParent(transform, false);
            filoGo.transform.position = new Vector3(_centro.x, (_y + 1f) * c, 0f);
            MaquinariaSprites.CrearCapa(filoGo.transform, "Sprite", MaquinariaSprites.FiloBalda(span), 19, span * c, c * 0.4f);

            // Los dos remates: pegados a cada extremo, integrados en la
            // horizontal de la balda (misma fila, ligeramente asomados sobre
            // el canto superior para que se lean como un remache clavado en
            // la piedra, no como flotando encima).
            float remateMundo = RemateLado * c;
            var remateIzqGo = new GameObject("RemateIzq");
            remateIzqGo.transform.SetParent(transform, false);
            remateIzqGo.transform.position = new Vector3((_x0 + RemateLado * 0.5f) * c, (_y + 0.5f) * c, 0f);
            MaquinariaSprites.CrearCapa(remateIzqGo.transform, "Sprite", MaquinariaSprites.CuadradoAnclaje(), 20, remateMundo, remateMundo);

            var remateDerGo = new GameObject("RemateDer");
            remateDerGo.transform.SetParent(transform, false);
            remateDerGo.transform.position = new Vector3((_x1 + 1 - RemateLado * 0.5f) * c, (_y + 0.5f) * c, 0f);
            MaquinariaSprites.CrearCapa(remateDerGo.transform, "Sprite", MaquinariaSprites.CuadradoAnclaje(), 20, remateMundo, remateMundo);
        }
    }
}
