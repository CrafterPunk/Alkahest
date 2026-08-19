using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Net;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// EL ENSAYO DEL MAESTRO — reconstruido en el PLAYTEST 27
    /// (docs/CONTRATO_TALLER_GRANDE.md, mandatos 1-3).
    ///
    /// =====================================================================
    /// EL VEREDICTO DE CESAR SOBRE EL DEL PLAYTEST 26
    /// =====================================================================
    /// *"Jajaja, esto ya es lamentable, lo mismo con el embudo feo."* Y tenía
    /// razón: el aparato que se supone que es EL EXAMEN del juego -- el sitio
    /// donde el Maestro dictamina si lo que has hecho persiste -- medía 5x4
    /// celdas y llevaba el mismo embudo decorativo que todos los demás.
    ///
    /// Reconstruido como lo que tiene que ser: **UN ALTAR**.
    ///  · Un **DAIS** de piedra de 23x6 que levanta la muestra por encima de
    ///    la línea de trabajo del resto del taller: aquí no se produce, aquí
    ///    se juzga, y eso se lee en que está más alto que todo lo demás.
    ///  · Una **BANDEJA ABIERTA** de <see cref="PlintoAncho"/>x<see cref="PlintoAltoInterior"/>
    ///    = 15x5 = **75 celdas** encima del dais (antes 3x4 = 12), enmarcada
    ///    en latón. Sin embudo: al Ensayo se le PRESENTA la muestra, no se le
    ///    vierte a ciegas.
    ///  · Dos **COLUMNAS** y un **DOSEL** de latón con colgantes
    ///    (<see cref="MaquinariaSprites.Dosel"/>) que enmarcan el conjunto.
    ///    Es lo único del taller que no parece maquinaria, y ésa es la idea.
    ///  · **ACUSE DE RECIBO** al presentar la muestra y **LATIDO** mientras
    ///    el ensayo corre (mandato 3), más el rescoldo del brasero
    ///    incorporado, que sube de verdad durante los 5 segundos de calor.
    ///
    /// LA MECÁNICA NO CAMBIA (contrato: forma, capacidad y feedback):
    ///  · AGUANTACALOR: calienta la muestra A LA VISTA hasta
    ///    <c>Universe.TempEnsayoCalorRaw</c> durante <see cref="RampSeconds"/>
    ///    y cuenta supervivientes del dominante: ≥60% intactas = cumplido.
    ///    Estrellas por MARGEN REAL de <c>Universe.UmbralPersistenciaRaw</c>.
    ///  · CONDUCE: instantáneo, consulta <c>Universe.Conductividad</c>.
    ///  · FALLO: el pedido NO se consume y el rótulo dice CÓMO murió la
    ///    muestra -- el fallo es información, no un "no".
    ///
    /// Firma de <see cref="Init"/> CONGELADA (contrato §6.5 del playtest 25):
    /// <c>Init(AlkahestSim, OrderSystem, Transform)</c>; `SubstanceKnowledge`
    /// se busca con <c>FindAnyObjectByType</c> (regla 1 de CLAUDE.md), mismo
    /// patrón perezoso que DeliveryChute.
    /// </summary>
    public sealed class EnsayoMaestro : MonoBehaviour, IMaquinaInteractiva, IMovibleAnclaEsquina, IMaquinaUsableRemota
    {
        // -----------------------------------------------------------------
        // GEOMETRÍA (playtest 27). Públicas: las lee Sim/SimLevelBuilder.cs.
        // -----------------------------------------------------------------
        /// <summary>Ancho del hueco de la bandeja del examen. 3 -&gt; 15.</summary>
        public const int PlintoAncho = 15;
        /// <summary>Alto del hueco de la bandeja. 4 -&gt; 5. 15x5 = 75 celdas (antes 12).</summary>
        public const int PlintoAltoInterior = 5;
        public const int MuroGrosor = 2;
        /// <summary>Celdas que el DAIS sobresale por cada lado del marco de la bandeja.</summary>
        public const int DaisVuelo = 2;
        /// <summary>Altura del dais sobre el suelo del cuarto: lo que levanta el examen por encima de la línea de trabajo del resto del taller.</summary>
        public const int DaisAlto = 6;
        /// <summary>Ancho de cada columna del dosel.</summary>
        public const int ColumnaAncho = 3;
        /// <summary>Filas del HOGAR incorporado: un nicho tallado DENTRO del dais, sellado por piedra por los cuatro lados (ver <see cref="TallarEnPlano"/>). Mismo criterio que los hogares del Crisol: unas brasas sueltas sobre la piedra se leen como grava roja derramada; metidas en su nicho se leen como la boca de un fuego.</summary>
        public const int HogarFilas = 2;
        /// <summary>Altura de las columnas del dosel sobre el suelo del cuarto.</summary>
        public const int ColumnaAlto = 30;

        // -----------------------------------------------------------------
        // SUELO SOBERANO (playtest 32, mismo patrón que Game/ColumnaEnsayo.cs
        // -- ver sus docblocks de AplanarPlataforma/RestaurarSueloBase. Igual
        // que la Columna, el Ensayo NUNCA talla su propia fila `baseY` (el
        // dais arranca en `baseY+1`, ver Calcular): es la losa compartida.
        // -----------------------------------------------------------------
        private const int PlataformaMargen = 2;
        private const int PlataformaProfundidad = 6;

        private const float ProximityRange = 3.4f;

        /// <summary>Duración del calentamiento visible antes de evaluar supervivientes.</summary>
        private const float RampSeconds = 5f;
        private const int TempStepPerTick = 8;
        private const float FraccionSupervivenciaMinima = 0.6f;
        private const int MargenDosEstrellas = 15;
        private const int MargenTresEstrellas = 30;

        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 4;
        private const float RotuloResultadoSeg = 5f;

        private AlkahestSim _sim;
        private OrderSystem _orders;
        private Transform _player;
        private SubstanceKnowledge _knowledge;

        private int _plintoX, _baseY;
        private int _x0, _x1, _y0, _y1;      // interior útil de la bandeja.
        private int _daisX0, _daisX1;
        private int _outX0, _outX1, _outY0, _outY1;
        private Vector3 _centro, _centroRotulo;

        /// <summary>(playtest 29) Handle en <see cref="SimLevelBuilder.ObraDelTaller"/> -- ver el docblock gemelo en Game/Crisol.cs (`_handleObra`).</summary>
        private int _handleObra = -1;

        private float _accumulator;

        private enum Fase { Ocioso, Calentando }
        private Fase _fase = Fase.Ocioso;
        private float _calentandoHasta;
        private byte _calentandoDominante;
        private int _calentandoN0;

        private string _rotulo;
        private Color _rotuloColor = UiStyles.Oro;
        private float _rotuloHasta;

        private SpriteRenderer _resalte, _latidoTrabajo, _destelloMarco, _brasas;

        // (playtest 31) EL ALTAR ES LO ÚNICO QUE SE ENCIENDE EN SU RINCÓN:
        // mientras el plinto calienta la muestra, la luz sube del hogar y
        // baña el dosel. Es el momento más ceremonial del juego y hasta esta
        // ronda ocurría en penumbra plana.
        private MaquinariaSprites.Luz _luzHogar;
        private float _alfaResalte;
        private int _celdasBandejaPrev;

        private readonly MaquinariaSprites.Destello _acuse = new MaquinariaSprites.Destello();
        private readonly MaquinariaSprites.AffordanceGlow _pulsoTrabajo = new MaquinariaSprites.AffordanceGlow();

        private const float RangoNombrePleno = 3.2f;
        private const float RangoNombreDesvanece = 4.4f;
        private bool _yaConocida;

        public Vector3 PuntoFoco => _centro;
        public float RangoFoco => ProximityRange;

        // ---- IMovible (playtest 29, "la última estructura no la puedo
        // mover" -- Cesar). Mismo patrón que Game/Crisol.cs: el ancla es la
        // esquina inferior izquierda del rect EXTERIOR (dosel incluido), que
        // es exactamente lo que mide TamanoMundo -- así la sombra de
        // Game/Mudanza.cs puede alinearse a la huella real (ver
        // IMovibleAnclaEsquina en ese archivo).
        public Vector3 CentroMundo => _centro;
        public Vector2 TamanoMundo => new Vector2(
            (_outX1 - _outX0 + 1) * SimRenderer.CellWorldSize,
            (_outY1 - _outY0 + 1) * SimRenderer.CellWorldSize);
        public Vector2Int AnclaCelda => new Vector2Int(_outX0, _outY0);

        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            int span = _outX1 - _outX0 + 1;
            int alto = _outY1 - _outY0 + 1;
            return anclaCelda.x >= 1 && anclaCelda.x + span - 1 <= CellGrid.W - 2
                && anclaCelda.y >= 1 && anclaCelda.y + alto - 1 <= CellGrid.H - 2;
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap. FIRMA CONGELADA.</summary>
        public void Init(AlkahestSim sim, OrderSystem orders, Transform jugador)
        {
            _sim = sim;
            _orders = orders;
            _player = jugador;
            _knowledge = FindAnyObjectByType<SubstanceKnowledge>();

            _plintoX = SimLevelBuilder.EnsayoPlintoX;
            _baseY = SimLevelBuilder.BaseYDeEstacion(SimLevelBuilder.EnsayoPlintoX); // (playtest 33) cota por zona -- ver BaseYDeEstacion.

            RecalcularRegion();
            BuildVisual();

            MachineFocus.Registrar(this);
            // (playtest 29) El registro anticincel lo hace la INSTANCIA, no
            // TallarEnPlano -- ver Sim/SimLevelBuilder.cs, bloque "OBRA MOVIBLE".
            // (playtest 32, FIX) `TallarEnPlano` YA registró este MISMO rect
            // (para que AdornarCuarto lo viera al tallar terrazas, ver
            // SimLevelBuilder.HallarObraExacta) -- aquí se RECLAMA ese handle
            // en vez de crear uno nuevo huérfano.
            int handleExistente = SimLevelBuilder.HallarObraExacta(_outX0, _outY0, _outX1, _outY1);
            _handleObra = handleExistente >= 0 ? handleExistente : SimLevelBuilder.RegistrarObra(_outX0, _outY0, _outX1, _outY1);
            Mudanza.RegistrarMovible(this);
        }

        /// <summary>
        /// (playtest 29) Extraído de Init para que <see cref="Reposicionar"/>
        /// también pueda recalcular la geometría tras mover el ancla.
        /// TAMBIÉN arrastra los sprites hijos: `BuildVisual` los parenta con
        /// `SetParent(transform, false)` y luego fija su POSICIÓN ABSOLUTA
        /// (world-space) -- Unity la convierte en un `localPosition` fijo
        /// relativo al padre EN ESE MOMENTO. Mover `transform.position` aquí
        /// arrastra a todos los hijos por el mismo delta sin recalcular cada
        /// sprite a mano (mismo mecanismo que usa Game/Crisol.cs, que sí
        /// fijaba `transform.position` desde el playtest 19; este archivo no
        /// lo hacía porque hasta ahora nunca necesitó moverse).
        /// </summary>
        private void RecalcularRegion()
        {
            var h = Calcular(_plintoX, _baseY);
            _x0 = h.BanX0; _x1 = h.BanX1; _y0 = h.BanY0; _y1 = h.BanY1;
            _daisX0 = h.DaisX0; _daisX1 = h.DaisX1;
            _outX0 = h.OutX0; _outX1 = h.OutX1; _outY0 = h.OutY0; _outY1 = h.OutY1;

            float c = SimRenderer.CellWorldSize;
            _centro = new Vector3((_x0 + PlintoAncho * 0.5f) * c, (_y0 + PlintoAltoInterior * 0.5f) * c, 0f);
            _centroRotulo = new Vector3(_centro.x, (_y1 + 3f) * c, 0f);
            transform.position = _centro;
        }

        private void OnDestroy()
        {
            MachineFocus.Olvidar(this);
            Mudanza.OlvidarMovible(this);
        }

        // ---- Huella compartida por la instancia y el tallado del plano ----
        private struct Huella
        {
            public int BanX0, BanX1, BanY0, BanY1;   // hueco de la bandeja.
            public int DaisX0, DaisX1, DaisY0, DaisY1;
            public int OutX0, OutX1, OutY0, OutY1;
        }

        private static Huella Calcular(int plintoX, int baseY)
        {
            Huella h;
            h.DaisY0 = baseY + 1;
            h.DaisY1 = baseY + DaisAlto;            // el dais llega hasta aquí, macizo.
            h.BanY0 = h.DaisY1 + 2;                 // +1 = el suelo de la bandeja; el hueco empieza en +2.
            h.BanY1 = h.BanY0 + PlintoAltoInterior - 1;
            h.BanX0 = plintoX - PlintoAncho / 2;
            h.BanX1 = h.BanX0 + PlintoAncho - 1;
            h.DaisX0 = h.BanX0 - MuroGrosor - DaisVuelo;
            h.DaisX1 = h.BanX1 + MuroGrosor + DaisVuelo;
            h.OutX0 = h.DaisX0 - ColumnaAncho;
            h.OutX1 = h.DaisX1 + ColumnaAncho;
            h.OutY0 = baseY;
            h.OutY1 = baseY + ColumnaAlto - 1;
            return h;
        }

        /// <summary>
        /// (playtest 32, encargo A) Mismo AplanarPlataforma que Game/Crisol.cs
        /// -- misma convención que Game/ColumnaEnsayo.cs (baseY INCLUIDO en
        /// el colchón de piedra, no en el vaciado). Aquí hace ESPECIAL falta:
        /// el DAIS (el bloque que de verdad sostiene la bandeja) arranca en
        /// `baseY+1` y jamás toca `baseY` -- si esta plataforma vaciara esa
        /// fila iban a quedar dos alturas de suelo distintas bajo el altar
        /// (piedra bajo las dos columnas del dosel, que SÍ re-tallan `baseY`
        /// -- ver el bucle de columnas más abajo -- pero un agujero justo
        /// bajo el dais, que no). Manteniendo `baseY` siempre sólida se evita
        /// el problema entero sin tener que saber qué sub-tramo lo repinta.
        /// </summary>
        private static void AplanarPlataforma(CellGrid grid, int outX0, int outX1, int baseY, int outY1)
        {
            int x0 = outX0 - PlataformaMargen;
            int x1 = outX1 + PlataformaMargen;
            for (int x = x0; x <= x1; x++)
            {
                for (int y = baseY - PlataformaProfundidad; y <= baseY; y++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Stone);
                for (int y = baseY + 1; y <= outY1; y++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Empty);
            }
        }

        /// <summary>Talla el altar entero (dais + bandeja + las dos columnas del dosel) sobre el CellGrid del plano.</summary>
        public static void TallarEnPlano(CellGrid grid, int plintoX, int baseY)
        {
            var h = Calcular(plintoX, baseY);

            AplanarPlataforma(grid, h.OutX0, h.OutX1, baseY, h.OutY1); // (playtest 32) SIEMPRE lo primero.

            // El DAIS: bloque macizo que levanta el examen.
            for (int y = h.DaisY0; y <= h.DaisY1; y++)
                for (int x = h.DaisX0; x <= h.DaisX1; x++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Stone);

            // La BANDEJA sobre el dais: su suelo + dos muros + hueco.
            for (int x = h.BanX0 - MuroGrosor; x <= h.BanX1 + MuroGrosor; x++)
                if (CellGrid.InBounds(x, h.BanY0 - 1)) grid.SetCell(x, h.BanY0 - 1, MaterialId.Stone);
            for (int y = h.BanY0 - 1; y <= h.BanY1; y++)
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    if (CellGrid.InBounds(h.BanX0 - t, y)) grid.SetCell(h.BanX0 - t, y, MaterialId.Stone);
                    if (CellGrid.InBounds(h.BanX1 + t, y)) grid.SetCell(h.BanX1 + t, y, MaterialId.Stone);
                }
            for (int y = h.BanY0; y <= h.BanY1; y++)
                for (int x = h.BanX0; x <= h.BanX1; x++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Empty);

            // EL HOGAR INCORPORADO: nicho de fuego dentro del dais, bajo la
            // bandeja. Sellado por construcción (piedra del dais arriba, abajo
            // y a los lados), así que no puede tragarse ninguna muestra.
            for (int y = h.DaisY0 + 1; y <= h.DaisY0 + HogarFilas; y++)
                for (int x = h.BanX0; x <= h.BanX1; x++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Empty);

            // Las dos COLUMNAS del dosel, del suelo a lo alto.
            for (int y = baseY; y <= h.OutY1; y++)
                for (int k = 0; k < ColumnaAncho; k++)
                {
                    if (CellGrid.InBounds(h.OutX0 + k, y)) grid.SetCell(h.OutX0 + k, y, MaterialId.Stone);
                    if (CellGrid.InBounds(h.OutX1 - k, y)) grid.SetCell(h.OutX1 - k, y, MaterialId.Stone);
                }

            // (playtest 29) Este método es estático y corre antes de que
            // exista ninguna instancia. (playtest 32, FIX) Por eso SÍ hace
            // falta registrar aquí: SimLevelBuilder.AdornarCuarto corre
            // dentro del mismo BuildCuartoIntimo, DESPUÉS de este tallado
            // pero ANTES de que exista ninguna instancia -- si el registro
            // esperara a `Init` (otro frame), AdornarCuarto tallaría
            // terrazas como si el Ensayo no existiera. `Init` RECLAMA este
            // mismo handle (ver SimLevelBuilder.HallarObraExacta). Mismo
            // rect exacto (el nicho del hogar queda dentro de él).
            SimLevelBuilder.RegistrarObra(h.OutX0, h.OutY0, h.OutX1, h.OutY1);
        }

        /// <summary>Equivalente EN CALIENTE de <see cref="AplanarPlataforma"/> -- ver su docblock (misma convención que Game/ColumnaEnsayo.cs: `_baseY` en el colchón de piedra, no en el vaciado).</summary>
        private void AplanarPlataformaCaliente()
        {
            int x0 = _outX0 - PlataformaMargen;
            int x1 = _outX1 + PlataformaMargen;
            int ancho = x1 - x0 + 1;
            for (int y = _baseY - PlataformaProfundidad; y <= _baseY; y++)
                for (int x = x0; x <= x1; x++)
                    _sim.PaintStable(x, y, 0, MaterialId.Stone);
            _sim.PaintRect(x0, _baseY + 1, ancho, _outY1 - (_baseY + 1) + 1, MaterialId.Empty);
        }

        /// <summary>Misma geometría EN CALIENTE (regla 29: PaintStable). Solo la usa <see cref="Reposicionar"/> (Mudanza).</summary>
        private void TallarEnCaliente()
        {
            var h = Calcular(_plintoX, _baseY);

            AplanarPlataformaCaliente(); // (playtest 32) SIEMPRE lo primero.

            for (int y = h.DaisY0; y <= h.DaisY1; y++)
                for (int x = h.DaisX0; x <= h.DaisX1; x++)
                    _sim.PaintStable(x, y, 0, MaterialId.Stone);

            for (int x = h.BanX0 - MuroGrosor; x <= h.BanX1 + MuroGrosor; x++)
                _sim.PaintStable(x, h.BanY0 - 1, 0, MaterialId.Stone);
            for (int y = h.BanY0 - 1; y <= h.BanY1; y++)
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.PaintStable(h.BanX0 - t, y, 0, MaterialId.Stone);
                    _sim.PaintStable(h.BanX1 + t, y, 0, MaterialId.Stone);
                }
            _sim.PaintRect(h.BanX0, h.BanY0, PlintoAncho, PlintoAltoInterior, MaterialId.Empty);

            for (int y = h.DaisY0 + 1; y <= h.DaisY0 + HogarFilas; y++)
                _sim.PaintRect(h.BanX0, y, h.BanX1 - h.BanX0 + 1, 1, MaterialId.Empty);

            for (int y = h.OutY0; y <= h.OutY1; y++)
                for (int k = 0; k < ColumnaAncho; k++)
                {
                    _sim.PaintStable(h.OutX0 + k, y, 0, MaterialId.Stone);
                    _sim.PaintStable(h.OutX1 - k, y, 0, MaterialId.Stone);
                }
        }

        /// <summary>
        /// (playtest 29, encargo B) Borra la mampostería VIEJA de la huella
        /// `h` -- ver el docblock gemelo en Game/Crisol.cs (`BorrarEnCaliente`)
        /// para el criterio general. Aquí el DAIS entero se borra completo
        /// (es un bloque SÓLIDO, no un contenedor -- no hay "interior" que
        /// preservar, ni siquiera el nicho del hogar, puro teatro sellado
        /// igual que en el Crisol): si había una muestra puesta a examen en
        /// la bandeja de encima, al desaparecer el dais que la sostenía cae
        /// sola por gravedad, tal cual pide el encargo B.
        /// </summary>
        private void BorrarEnCaliente(Huella h)
        {
            for (int y = h.DaisY0; y <= h.DaisY1; y++)
                for (int x = h.DaisX0; x <= h.DaisX1; x++)
                    _sim.Paint(x, y, 0, MaterialId.Empty);

            // La BANDEJA: suelo + dos muros -- NUNCA el hueco (podría tener
            // una muestra puesta a examen).
            for (int x = h.BanX0 - MuroGrosor; x <= h.BanX1 + MuroGrosor; x++)
                _sim.Paint(x, h.BanY0 - 1, 0, MaterialId.Empty);
            for (int y = h.BanY0 - 1; y <= h.BanY1; y++)
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.Paint(h.BanX0 - t, y, 0, MaterialId.Empty);
                    _sim.Paint(h.BanX1 + t, y, 0, MaterialId.Empty);
                }

            // Las dos columnas del dosel, EXCLUYENDO la fila `h.OutY0`
            // (=baseY, la losa compartida del cuarto -- jamás piedra del mundo).
            for (int y = h.OutY0 + 1; y <= h.OutY1; y++)
                for (int k = 0; k < ColumnaAncho; k++)
                {
                    _sim.Paint(h.OutX0 + k, y, 0, MaterialId.Empty);
                    _sim.Paint(h.OutX1 - k, y, 0, MaterialId.Empty);
                }
        }

        /// <summary>(playtest 32, fix "rastro de bedrock") Ver el docblock gemelo en Game/ColumnaEnsayo.cs (`RestaurarSueloBase`) -- `h.OutY0` (=baseY) entra en el rango a limpiar porque esta estación lo trata como parte del COLCHÓN de piedra (ver AplanarPlataforma), no como su propio suelo de recinto.</summary>
        private void RestaurarSueloBase(Huella h)
        {
            int sueloBase = SimLevelBuilder.CuartoY0 + SimLevelBuilder.WallThickness - 1;
            int x0 = h.OutX0 - PlataformaMargen;
            int x1 = h.OutX1 + PlataformaMargen;
            for (int y = sueloBase + 1; y <= h.OutY0; y++)
                for (int x = x0; x <= x1; x++)
                    _sim.Paint(x, y, 0, MaterialId.Empty);
        }

        public void Reposicionar(Vector2Int anclaCelda)
        {
            var huellaVieja = Calcular(_plintoX, _baseY);
            BorrarEnCaliente(huellaVieja); // 1) BORRAR la mampostería vieja, con la huella de ANTES de tocar el ancla.
            RestaurarSueloBase(huellaVieja); // (playtest 32) limpia cualquier pedestal elevado que esta instancia hubiera dejado ahí.

            _plintoX += anclaCelda.x - _outX0;
            _baseY = anclaCelda.y;
            RecalcularRegion();
            TallarEnCaliente(); // 2) TALLAR la nueva. regla 36: NUNCA volver a llamar a Init/BuildVisual para mover.

            SimLevelBuilder.ActualizarObra(_handleObra, _outX0, _outY0, _outX1, _outY1); // 3) ACTUALIZAR el registro anticincel.
        }

        // =================================================================
        // VISUAL
        // =================================================================
        private void BuildVisual()
        {
            float c = SimRenderer.CellWorldSize;
            var h = Calcular(_plintoX, _baseY);

            // ---- Las dos columnas del dosel, vestidas de sillería.
            var sillar = MaquinariaSprites.Sillar(ColumnaAncho, ColumnaAlto);
            for (int lado = 0; lado < 2; lado++)
            {
                int x0 = lado == 0 ? _outX0 : _outX1 - ColumnaAncho + 1;
                var go = new GameObject(lado == 0 ? "EnsayoColumnaIzq" : "EnsayoColumnaDer");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3((x0 + ColumnaAncho * 0.5f) * c, (_baseY + ColumnaAlto * 0.5f) * c, 0f);
                MaquinariaSprites.CrearCapa(go.transform, "Sprite", sillar, 18, ColumnaAncho * c, ColumnaAlto * c);
            }

            // ---- El DAIS, sillería también: se tiene que leer como obra
            // labrada, no como el mismo suelo de siempre un poco más alto.
            int spanDais = _daisX1 - _daisX0 + 1;
            var daisGo = new GameObject("EnsayoDais");
            daisGo.transform.SetParent(transform, false);
            daisGo.transform.position = new Vector3((_daisX0 + spanDais * 0.5f) * c, (h.DaisY0 + DaisAlto * 0.5f) * c, 0f);
            MaquinariaSprites.CrearCapa(daisGo.transform, "Sprite", MaquinariaSprites.Sillar(spanDais, DaisAlto), 18,
                spanDais * c, DaisAlto * c);

            // ---- EL HOGAR INCORPORADO, dentro de su nicho del dais: el
            // Ensayo calienta de verdad, y esto es lo que lo cuenta mientras
            // lo hace. Va DENTRO del hueco tallado (segunda pasada: fuera de
            // él se leía como grava roja derramada sobre la piedra).
            int anchoHogar = _x1 - _x0 + 1;
            var hogarGo = new GameObject("EnsayoHogar");
            hogarGo.transform.SetParent(transform, false);
            hogarGo.transform.position = new Vector3(_centro.x, (h.DaisY0 + 1 + HogarFilas * 0.5f) * c, 0f);
            _brasas = MaquinariaSprites.CrearCapa(hogarGo.transform, "Brasas",
                MaquinariaSprites.LechoBrasas(anchoHogar, HogarFilas), 19, anchoHogar * c, HogarFilas * c);
            _brasas.color = new Color(0.14f, 0.09f, 0.07f, 1f);

            // (playtest 31) La luz del examen + la sombra que apoya el dais
            // en el suelo del taller.
            _luzHogar = MaquinariaSprites.Luz.Crear(transform, "LuzEnsayo",
                new Vector3(_centro.x, (h.DaisY0 + 2f) * c, 0f), 40f * c, new Color(1f, 0.62f, 0.26f));
            MaquinariaSprites.Sombra(transform, new Vector3(_centro.x, (_baseY - 0.3f) * c, 0f),
                (_outX1 - _outX0 + 6) * c, 4f * c, 0.42f);

            // ---- LA BANDEJA DEL EXAMEN: marco de latón, hueco transparente.
            int spanBandeja = PlintoAncho + 2 * MuroGrosor; // 19
            int altoBandeja = PlintoAltoInterior + 1;       // 6
            float anchoW = spanBandeja * c, altoW = altoBandeja * c;
            var marco = MaquinariaSprites.MarcoBandeja(spanBandeja, altoBandeja);
            var bandejaGo = new GameObject("EnsayoBandeja");
            bandejaGo.transform.SetParent(transform, false);
            bandejaGo.transform.position = new Vector3((_x0 - MuroGrosor + spanBandeja * 0.5f) * c, (h.BanY0 - 1 + altoBandeja * 0.5f) * c, 0f);

            _resalte = MaquinariaSprites.CrearCapa(bandejaGo.transform, "Resalte", marco, 14, anchoW * 1.12f, altoW * 1.22f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);
            _latidoTrabajo = MaquinariaSprites.CrearCapa(bandejaGo.transform, "LatidoTrabajo", marco, 15, anchoW * 1.06f, altoW * 1.12f);
            _latidoTrabajo.color = new Color(1f, 0.5f, 0.18f, 0f);
            MaquinariaSprites.CrearCapa(bandejaGo.transform, "Marco", marco, 19, anchoW, altoW);
            _destelloMarco = MaquinariaSprites.CrearCapa(bandejaGo.transform, "Acuse", marco, 22, anchoW, altoW);
            _destelloMarco.color = new Color(1f, 1f, 0.9f, 0f);

            // ---- EL DOSEL: el arco de latón con colgantes que corona las dos
            // columnas. Lo que dice, sin una palabra, "esto no es una máquina".
            int spanDosel = _outX1 - _outX0 + 1;
            var doselGo = new GameObject("EnsayoDosel");
            doselGo.transform.SetParent(transform, false);
            doselGo.transform.position = new Vector3((_outX0 + spanDosel * 0.5f) * c, (_baseY + ColumnaAlto + 4f) * c, 0f);
            MaquinariaSprites.CrearCapa(doselGo.transform, "Sprite", MaquinariaSprites.Dosel(spanDosel, 12), 20,
                spanDosel * c, 12f * c);
        }

        private bool EstaEnfocada() => MachineFocus.EsFoco(this, _player);

        private void Update()
        {
            if (_sim == null || _sim.Grid == null || _orders == null) return;
            if (DayCycle.InputLocked) return;

            if (_knowledge == null) _knowledge = FindAnyObjectByType<SubstanceKnowledge>();

            SondearBandeja();
            _acuse.Avanzar(Time.deltaTime);
            _pulsoTrabajo.Trabajando = _fase == Fase.Calentando;
            ActualizarVisual();

            if (_fase == Fase.Calentando)
            {
                _accumulator += Time.deltaTime;
                int steps = 0;
                while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
                {
                    ApplyCalentamientoTick();
                    _accumulator -= TickDt;
                    steps++;
                }
                if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;

                if (Time.time >= _calentandoHasta) EvaluarAguantaCalor();
                return; // mientras se calienta, E no dispara otro ensayo encima.
            }

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && EstaEnfocada())
            {
                TryEnsayo();
                MachineFocus.RegistrarUsoE(); // (ENCARGO N) sin cambios: cuenta como uso aunque no haya pedido activo -- ver el docblock de TryEnsayo.
            }
        }

        /// <summary>Acuse de recibo (mandato 3): la bandeja destella cuando le presentas una muestra.</summary>
        private void SondearBandeja()
        {
            var grid = _sim.Grid;
            int n = 0;
            for (int y = _y0; y <= _y1; y++)
                for (int x = _x0; x <= _x1; x++)
                    if (grid.mat[CellGrid.Idx(x, y)] != MaterialId.Empty) n++;
            if (n > _celdasBandejaPrev) _acuse.Disparar();
            _celdasBandejaPrev = n;
        }

        private void ActualizarVisual()
        {
            bool calentando = _fase == Fase.Calentando;
            if (_brasas != null)
            {
                float t = calentando ? Mathf.Clamp01(1f - (_calentandoHasta - Time.time) / RampSeconds) : 0f;
                float pulso = 0.82f + 0.18f * Mathf.Sin(Time.time * (calentando ? 7f : 2f));
                float i = calentando ? Mathf.Lerp(0.30f, 1f, t) : 0.05f; // en frío, casi negro (segunda pasada, mismo criterio que el Crisol).
                _brasas.color = new Color(Mathf.Min(1f, 0.5f + 0.7f * i) * pulso, (0.15f + 0.42f * i) * pulso, (0.06f + 0.12f * i) * pulso, 1f);
            }
            // (playtest 31) La luz SIGUE al hogar (misma `t` y misma fase que
            // las brasas): imposible que el halo diga "encendido" con el
            // hogar negro.
            {
                float tLuz = calentando ? Mathf.Clamp01(1f - (_calentandoHasta - Time.time) / RampSeconds) : 0f;
                if (calentando) _luzHogar?.Latir(0.16f + 0.26f * tLuz, 0.05f, 1.1f, 0.21f);
                else _luzHogar?.Intensidad(0.035f); // rescoldo del altar: se adivina, no alumbra.
            }

            if (_latidoTrabajo != null)
                _latidoTrabajo.color = new Color(1f, 0.5f, 0.18f, _pulsoTrabajo.AlfaTrabajo * 0.55f);
            if (_destelloMarco != null)
                _destelloMarco.color = new Color(1f, 1f, 0.9f, _acuse.Alfa);
            if (_resalte != null)
            {
                float objetivo = EstaEnfocada() ? 0.55f + 0.20f * Mathf.Sin(Time.time * 4f) : 0f;
                _alfaResalte = Mathf.MoveTowards(_alfaResalte, objetivo, 6f * Time.deltaTime);
                _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, _alfaResalte);
            }
        }

        private void ApplyCalentamientoTick()
        {
            var grid = _sim.Grid;
            byte objetivo = _sim.Universe.TempEnsayoCalorRaw;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;

            for (int x = _x0; x <= _x1; x++)
            {
                for (int y = _y0; y <= _y1; y++)
                {
                    int idx = CellGrid.Idx(x, y);
                    if (grid.mat[idx] == MaterialId.Empty) continue;

                    int actual = grid.temp[idx];
                    int diff = objetivo - actual;
                    int paso = Mathf.Clamp(diff, -TempStepPerTick, TempStepPerTick);
                    if (paso == 0) continue;
                    grid.temp[idx] = (byte)Mathf.Clamp(actual + paso, 0, 255);
                    grid.WakeChunk(x, y, tick);
                }
            }
        }

        /// <summary>
        /// (ENCARGO N, playtest 43) EL HANDLER COMPARTIDO DE E -- sin
        /// cambios de lógica, solo la firma pasa de `void` a `bool`. El
        /// chequeo "mientras calienta, E no dispara otro ensayo" vive en
        /// `Update()` (con un `return` que ni siquiera llega a comprobar el
        /// teclado), así que ese caso lo replica <see cref="UsarPorRed"/>
        /// aparte (ver su docblock) en vez de duplicarse aquí dentro.
        /// </summary>
        private bool TryEnsayo()
        {
            Order objetivo = BuscarOrdenEnsayoActiva();
            if (objetivo == null)
            {
                Rotular("no hay ningún pedido de calor o chispa activo ahora mismo", UiStyles.TextoTenue);
                return false;
            }

            if (!MuestraDominante(out byte matId, out int n0) || n0 == 0)
            {
                Rotular("presenta una muestra en la bandeja antes de pulsar E", UiStyles.Aviso);
                return false;
            }

            if (objetivo.Tipo == OrderType.Conduce)
            {
                EvaluarConduce(matId);
                return true;
            }

            _calentandoDominante = matId;
            _calentandoN0 = n0;
            _calentandoHasta = Time.time + RampSeconds;
            _fase = Fase.Calentando;
            Rotular("calentando la muestra al rojo del crisol...", UiStyles.Aviso);
            return true;
        }

        // =================================================================
        // (ENCARGO N, playtest 43, CONTRATO_PARIDAD.md §2a/§2b) EL GANCHO REMOTO
        // =================================================================

        /// <summary>
        /// `Update()` ni siquiera comprueba el teclado mientras
        /// <see cref="Fase.Calentando"/> corre ("mientras se calienta, E no
        /// dispara otro ensayo encima") -- ese mismo silencio se replica
        /// aquí ANTES de llamar a <see cref="TryEnsayo"/>, o un invitado
        /// podría reiniciar un calentamiento en curso que el anfitrión jamás
        /// permitiría.
        /// </summary>
        bool IMaquinaUsableRemota.UsarPorRed() => _fase != Fase.Calentando && TryEnsayo();

        byte IMaquinaUsableRemota.EstadoVivoRed() => _fase == Fase.Calentando ? EstadoVivoBits.Trabajando : (byte)0;

        private Order BuscarOrdenEnsayoActiva()
        {
            if (_orders == null) return null;
            var lista = _orders.ActiveOrders;
            for (int i = 0; i < lista.Count; i++)
            {
                var o = lista[i];
                if (o.Completado) continue;
                if (o.Tipo == OrderType.AguantaCalor || o.Tipo == OrderType.Conduce) return o;
            }
            return null;
        }

        /// <summary>Buffer de conteo REUTILIZADO entre llamadas (cero allocs en los sondeos).</summary>
        private readonly int[] _conteoBuf = new int[MaterialId.Count];

        private bool MuestraDominante(out byte matId, out int count) => ConteoDominante(out matId, out count);

        private bool ConteoDominante(out byte matId, out int count)
        {
            System.Array.Clear(_conteoBuf, 0, _conteoBuf.Length);
            var grid = _sim.Grid;
            for (int x = _x0; x <= _x1; x++)
            {
                for (int y = _y0; y <= _y1; y++)
                {
                    byte m = grid.mat[CellGrid.Idx(x, y)];
                    if (m == MaterialId.Empty || m == MaterialId.Stone || m >= MaterialId.Count) continue;
                    _conteoBuf[m]++;
                }
            }

            matId = 0;
            count = 0;
            for (int i = 1; i < _conteoBuf.Length; i++)
            {
                if (_conteoBuf[i] > count)
                {
                    count = _conteoBuf[i];
                    matId = (byte)i;
                }
            }
            return count > 0;
        }

        private int ContarMaterial(byte matId)
        {
            int n = 0;
            var grid = _sim.Grid;
            for (int x = _x0; x <= _x1; x++)
                for (int y = _y0; y <= _y1; y++)
                    if (grid.mat[CellGrid.Idx(x, y)] == matId) n++;
            return n;
        }

        private void EvaluarConduce(byte matId)
        {
            byte conductividad = _sim.Universe.Conductividad(matId);
            if (conductividad >= 2)
            {
                _orders.CompletarEnsayo(OrderType.Conduce, 2f);
                Rotular("¡la lámpara arde a pleno brillo! -- ★★", UiStyles.Exito);
                _knowledge?.RegistrarObservacionPropiedad(matId, "encendió la lámpara del Ensayo a pleno brillo");
            }
            else if (conductividad == 1)
            {
                _orders.CompletarEnsayo(OrderType.Conduce, 1f);
                Rotular("condujo a duras penas -- ★", UiStyles.Exito);
                _knowledge?.RegistrarObservacionPropiedad(matId, "condujo a duras penas en el Ensayo");
            }
            else
            {
                Rotular("ni un parpadeo -- no conduce nada", UiStyles.Peligro);
                _knowledge?.RegistrarObservacionPropiedad(matId, "no conduce: la lámpara ni parpadeó en el Ensayo");
            }
        }

        private void EvaluarAguantaCalor()
        {
            _fase = Fase.Ocioso;

            int supervivientes = ContarMaterial(_calentandoDominante);
            float fraccion = _calentandoN0 > 0 ? supervivientes / (float)_calentandoN0 : 0f;

            if (fraccion >= FraccionSupervivenciaMinima)
            {
                byte umbral = _sim.Universe.UmbralPersistenciaRaw(_calentandoDominante);
                int margen = umbral - _sim.Universe.TempEnsayoCalorRaw;

                float factor;
                string estrellas;
                if (margen >= MargenTresEstrellas) { factor = 2f; estrellas = "★★★"; }
                else if (margen >= MargenDosEstrellas) { factor = 1.5f; estrellas = "★★"; }
                else { factor = 1f; estrellas = "★"; }

                _orders.CompletarEnsayo(OrderType.AguantaCalor, factor);
                Rotular("¡aguantó el rojo del crisol! -- " + estrellas, UiStyles.Exito);
                _knowledge?.RegistrarObservacionPropiedad(_calentandoDominante, "aguantó el rojo del crisol en el Ensayo (" + estrellas + ")");
            }
            else
            {
                string motivo = DescribirMuerte();
                Rotular(motivo, UiStyles.Peligro);
                _knowledge?.RegistrarObservacionPropiedad(_calentandoDominante, "no aguantó el Ensayo: " + motivo);
            }
        }

        /// <summary>El rótulo dice CÓMO murió la muestra, por ARQUETIPO -- nunca revela el nombre interno de algo innominado (reglas 13/17).</summary>
        private string DescribirMuerte()
        {
            if (!ConteoDominante(out byte matId, out int count) || count == 0)
                return "no quedó nada de la muestra: se consumió por completo en el calor";

            var def = _sim.Universe.Get(matId);
            switch (def.archetype)
            {
                case MaterialArchetype.Liquid: return "se fundió a mitad del ensayo";
                case MaterialArchetype.Gas: return "se evaporó en el calor";
                case MaterialArchetype.Fire: return "ardió hasta consumirse";
                default: return "no aguantó lo bastante: solo una fracción sobrevivió";
            }
        }

        private void Rotular(string texto, Color color)
        {
            _rotulo = texto;
            _rotuloColor = color;
            _rotuloHasta = Time.time + RotuloResultadoSeg;
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return;

            UiStyles.Preparar();

            if (_fase == Fase.Calentando)
            {
                int segundos = Mathf.CeilToInt(Mathf.Max(0f, _calentandoHasta - Time.time));
                UiStyles.EtiquetaMundo(_centroRotulo, "calentando... (" + segundos + "s)", UiStyles.Aviso, UiStyles.S(10f));
                return;
            }

            if (Time.time < _rotuloHasta && _rotulo != null)
            {
                UiStyles.EtiquetaMundo(_centroRotulo, _rotulo, _rotuloColor, UiStyles.S(10f));
                return;
            }

            float cercaniaNombre = UiStyles.Cercania(_centro, _player, RangoNombrePleno, RangoNombreDesvanece);
            if (!_yaConocida && cercaniaNombre >= 0.98f) _yaConocida = true;
            if (!_yaConocida && cercaniaNombre > 0f)
            {
                Color tenue = UiStyles.TextoTenue;
                UiStyles.PlacaMundo(_centroRotulo, "el ensayo del maestro",
                    new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercaniaNombre), -UiStyles.S(6f));
            }

            if (MachineFocus.MostrarPromptE && EstaEnfocada() && !UiStyles.RatonOcupado)
            {
                UiStyles.EtiquetaMundo(_centroRotulo, "E — someter la muestra al Ensayo del Maestro", UiStyles.Oro, UiStyles.S(10f));
            }
        }
    }
}
