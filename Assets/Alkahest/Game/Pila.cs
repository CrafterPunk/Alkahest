using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// LA PILA — archivo NUEVO (playtest 34, "ajustes finales" de Cesar,
    /// "GRIFOS Y PILAS SE MUDAN POR SEPARADO"). Cesar, sobre mover un grifo:
    /// *"verifica qué pasa con su pila (los cuadraditos donde cae el
    /// líquido)"*. Dos síntomas relacionados, dos causas distintas:
    ///
    ///  1) HOY MOVER EL GRIFO ARRASTRABA EL MARCO DE SU PILA. La "U" de
    ///     piedra de verdad (paredes+suelo, la cubeta donde cae el chorro)
    ///     la tallaba <see cref="SimLevelBuilder"/> directamente en el
    ///     génesis del mundo (nunca se movía). Pero el LABIO decorativo que
    ///     la enmarca (<c>MaquinariaSprites.MarcoBandeja</c>) lo dibujaba
    ///     <c>Game/Dispenser.cs::BuildPilaEnmarcada</c> como HIJO del propio
    ///     <c>transform</c> del grifo -- un hijo con `SetParent(transform,
    ///     false)` se arrastra SOLO cuando el padre se mueve (regla básica
    ///     de Unity, el mismo mecanismo que hace que el caño/halo/gota del
    ///     propio grifo lo sigan). Mover el grifo dejaba el marco colgando
    ///     en el sitio NUEVO -- sobre piedra que no es una cubeta -- mientras
    ///     la cubeta real se quedaba huérfana de marco en el sitio VIEJO.
    ///     FIX: Dispenser ya no dibuja ningún marco (ver el docblock de
    ///     <c>Dispenser.BuildPilaEnmarcada</c>, retirado sin llamante, regla
    ///     15 de CLAUDE.md); esta clase es la única dueña del marco de la
    ///     pila, y su GameObject no tiene ninguna relación de padre/hijo con
    ///     el grifo.
    ///
    ///  2) LA PILA NO ERA MOVIBLE POR SEPARADO. Cesar quiere poder colocar
    ///     el grifo Y su pila de recogida donde le convenga a cada uno, no
    ///     como una unidad rígida. Esta clase implementa
    ///     <see cref="IMovibleAnclaEsquina"/> con el MISMO patrón exacto de
    ///     Game/Balda.cs/Game/Anclaje.cs/Game/Alambique.cs ("plataforma
    ///     soberana en miniatura"): <see cref="TallarEnPlano"/> talla la "U"
    ///     en el génesis (llamado desde
    ///     <see cref="SimLevelBuilder.BuildPilasFuentes"/>, reemplazando el
    ///     <c>DrawUShape</c> directo que había ahí hasta esta ronda), la
    ///     instancia RECLAMA ese mismo handle de obra en <see cref="Init"/>
    ///     (nunca crea uno segundo -- mismo cuidado que
    ///     Game/Alambique.cs documenta contra el handle huérfano), y
    ///     <see cref="Reposicionar"/> sigue el patrón
    ///     borrar-la-vieja/tallar-la-nueva/ActualizarObra de Game/Balda.cs.
    ///
    /// LOS DOS CAÑOS SIGUEN VERTIENDO A LA CELDA QUE SIEMPRE VERTIERON
    /// (Dispenser no cambia su columna de caída): mover la pila NO mueve el
    /// chorro -- exactamente lo que pide el encargo ("el grifo se muda
    /// SOLO"). El jugador puede recolocar la pila bajo el chorro donde
    /// quiera, o dejar que el chorro caiga sobre suelo abierto si prefiere
    /// experimentar sin red (mismo criterio de libertad que ya defiende la
    /// regla 38 de CLAUDE.md: la herramienta no le impide equivocarse).
    /// </summary>
    public sealed class Pila : MonoBehaviour, IMovibleAnclaEsquina
    {
        private AlkahestSim _sim;

        // Esquina inferior izquierda del footprint EXTERIOR (paredes
        // incluidas) + dimensiones -- IMovibleAnclaEsquina ancla en
        // (_x0, _y0), igual que Game/Balda.cs/Game/StorageRack.cs.
        private int _x0, _y0, _ancho, _hondo, _muro;
        private Vector3 _centro;

        private int _handleObra = -1;

        public Vector3 CentroMundo => _centro;

        /// <summary>
        /// (fix Cesar playtest 34, tarea "c") LA GUÍA DE MUDANZA CONTIENE LA
        /// "U" COMPLETA: footprint EXTERIOR (paredes incluidas), no solo el
        /// hueco interior donde cae el líquido -- así la sombra que dibuja
        /// Game/Mudanza.cs deja ver de un vistazo si la pila entera cabe en
        /// el sitio candidato, no solo su boca.
        /// </summary>
        public Vector2 TamanoMundo => new Vector2(_ancho * SimRenderer.CellWorldSize, _hondo * SimRenderer.CellWorldSize);
        public Vector2Int AnclaCelda => new Vector2Int(_x0, _y0);

        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            return anclaCelda.x >= 1 && anclaCelda.x + _ancho - 1 <= CellGrid.W - 2
                && anclaCelda.y >= 1 && anclaCelda.y + _hondo - 1 <= CellGrid.H - 2;
        }

        /// <summary>
        /// (génesis) Talla la "U" de piedra completa (mismo <see cref="SimLevelBuilder.DrawUShape"/>
        /// de siempre) y registra su obra anticincel -- llamado desde
        /// <see cref="SimLevelBuilder.BuildPilasFuentes"/>, ANTES de que
        /// exista ninguna instancia (mismo patrón que
        /// Game/Balda.cs::TallarEnPlano/Game/ColumnaEnsayo.cs); <see cref="Init"/>
        /// RECLAMA el handle después en vez de registrar uno nuevo.
        /// </summary>
        public static void TallarEnPlano(CellGrid grid, int x0, int y0, int ancho, int hondo, int muro)
        {
            SimLevelBuilder.DrawUShape(grid, x0, y0, ancho, hondo, muro);
            SimLevelBuilder.RegistrarObra(x0, y0, x0 + ancho - 1, y0 + hondo - 1);
        }

        /// <summary>Inyección de dependencias -- llamada por <see cref="SpawnTodas"/> (ver Game/Mudanza.cs, `SpawnBaldasYAnclajesSiCorresponde`).</summary>
        public void Init(AlkahestSim sim, int x0, int y0, int ancho, int hondo, int muro)
        {
            _sim = sim;
            _x0 = x0; _y0 = y0; _ancho = ancho; _hondo = hondo; _muro = muro;

            RecalcularCentro();

            int existente = SimLevelBuilder.HallarObraExacta(_x0, _y0, _x0 + _ancho - 1, _y0 + _hondo - 1);
            _handleObra = existente >= 0 ? existente : SimLevelBuilder.RegistrarObra(_x0, _y0, _x0 + _ancho - 1, _y0 + _hondo - 1);

            Mudanza.RegistrarMovible(this);
            BuildVisual();
        }

        private void RecalcularCentro()
        {
            float c = SimRenderer.CellWorldSize;
            _centro = new Vector3((_x0 + _ancho * 0.5f) * c, (_y0 + _hondo * 0.5f) * c, 0f);
            transform.position = _centro;
        }

        /// <summary>Talla la "U" completa EN CALIENTE (regla 29 de CLAUDE.md: PaintStable, esto CREA piedra que no existía) -- mismas dos pasadas que <see cref="SimLevelBuilder.DrawUShape"/> (suelo de <c>_muro</c> filas + paredes laterales de <c>_muro</c> de ancho), reescritas por celda porque el tallado en caliente no puede tocar <c>CellGrid.SetCell</c> directamente (eso es construcción de nivel, no runtime).</summary>
        private void TallarEnCaliente()
        {
            int x1 = _x0 + _ancho - 1;
            int yTop = _y0 + _hondo - 1;

            for (int y = _y0; y < _y0 + _muro; y++)
                for (int x = _x0; x <= x1; x++)
                    _sim.PaintStable(x, y, 0, MaterialId.Stone);

            for (int y = _y0; y <= yTop; y++)
            {
                for (int t = 0; t < _muro; t++)
                {
                    _sim.PaintStable(_x0 + t, y, 0, MaterialId.Stone);
                    _sim.PaintStable(x1 - t, y, 0, MaterialId.Stone);
                }
            }
        }

        /// <summary>Borra el footprint EXTERIOR completo (paredes+suelo+interior) en la posición VIEJA -- lo que hubiera dentro (líquido, lo que sea) se va con la pila, mismo criterio que Game/Balda.cs/Game/Alambique.cs al mudarse. Vía Paint a Empty (regla 29: esto QUITA materia, no la crea).</summary>
        private void BorrarEnCaliente(int x0, int y0, int ancho, int hondo)
        {
            int x1 = x0 + ancho - 1;
            int y1 = y0 + hondo - 1;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    _sim.Paint(x, y, 0, MaterialId.Empty);
        }

        public void Reposicionar(Vector2Int anclaCelda)
        {
            BorrarEnCaliente(_x0, _y0, _ancho, _hondo); // 1) borrar la "U" VIEJA.

            _x0 = anclaCelda.x;
            _y0 = anclaCelda.y;
            RecalcularCentro();
            TallarEnCaliente(); // 2) tallar la nueva -- regla 36: nunca Init/BuildVisual otra vez.

            SimLevelBuilder.ActualizarObra(_handleObra, _x0, _y0, _x0 + _ancho - 1, _y0 + _hondo - 1); // 3) actualizar el registro anticincel.
        }

        private void OnDestroy()
        {
            Mudanza.OlvidarMovible(this);
        }

        /// <summary>
        /// El único vestido que necesita: el labio de latón enmarcado que
        /// ANTES dibujaba Game/Dispenser.cs sobre su propio transform (ver
        /// el docblock de la clase) -- misma pieza (<c>MarcoBandeja</c>),
        /// mismo sortingOrder (19), ahora como hijo de ESTE GameObject en
        /// vez del grifo, así que se mueve con la pila y solo con ella.
        /// </summary>
        private void BuildVisual()
        {
            float c = SimRenderer.CellWorldSize;
            var marco = MaquinariaSprites.CrearCapa(transform, "Marco",
                MaquinariaSprites.MarcoBandeja(_ancho, _hondo), 19, _ancho * c, _hondo * c);
            marco.color = Color.white;
        }

        /// <summary>
        /// Instancia UNA vez las dos pilas de <see cref="SimLevelBuilder.PilaPlanes"/>
        /// (la piedra YA la talló el génesis, ver <see cref="TallarEnPlano"/>
        /// vía <see cref="SimLevelBuilder.BuildPilasFuentes"/> -- aquí solo se
        /// crea el MonoBehaviour que las viste y las hace movibles). Guardado
        /// estático "ya creadas", llamado desde Game/Mudanza.cs con el MISMO
        /// guardián host-only que Balda/Anclaje -- ver el docblock de
        /// <c>Mudanza.SpawnBaldasYAnclajesSiCorresponde</c>.
        /// </summary>
        private static bool _todasCreadas;

        public static void SpawnTodas(AlkahestSim sim)
        {
            if (_todasCreadas) return;
            _todasCreadas = true;

            var planes = SimLevelBuilder.PilaPlanes;
            for (int i = 0; i < planes.Length; i++)
            {
                var p = planes[i];
                var go = new GameObject("Pila" + i);
                var pila = go.AddComponent<Pila>();
                pila.Init(sim, p.X0, p.Y0, p.Ancho, p.Hondo, p.Muro);
            }
        }
    }
}
