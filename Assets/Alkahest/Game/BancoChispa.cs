using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Net;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// EL BANCO DE CHISPA — reconstruido en el PLAYTEST 27
    /// (docs/CONTRATO_TALLER_GRANDE.md, mandatos 1-3).
    ///
    /// =====================================================================
    /// EL VEREDICTO DE CESAR SOBRE EL DEL PLAYTEST 26
    /// =====================================================================
    /// *"Aún más pequeña, como si hacerlo difícil de entender fuera tu
    /// objetivo... otro embudo feo que no es boquilla y sin capacidad."*
    ///
    /// Era el aparato más pequeño del taller (3x2 = 6 celdas de ranura) y
    /// llevaba embudo sin recibir nada por vertido -- el mismo pecado de la
    /// Prensa. Reconstruido:
    ///  · **BANDEJA ABIERTA** de <see cref="BandejaAncho"/>x<see cref="BandejaAlto"/>
    ///    = 13x5 = **65 celdas** (antes 6), enmarcada en latón
    ///    (<see cref="MaquinariaSprites.MarcoBandeja"/>), sin ningún embudo.
    ///  · **DOS ELECTRODOS DE VERDAD**, altos, sobre plintos de piedra a cada
    ///    lado -- pie de porcelana, vástago de latón, punta de cobre
    ///    (<see cref="MaquinariaSprites.Electrodo"/>). Entre sus puntas salta
    ///    el <see cref="MaquinariaSprites.Arco"/>, JUSTO ENCIMA de la
    ///    bandeja: se ve qué se está midiendo y con qué.
    ///  · **LA LÁMPARA SE VE DESDE EL OTRO LADO DEL TALLER**: una ampolla de
    ///    vidrio de 7 celdas con filamento propio, colgada de un pórtico de
    ///    latón sobre el banco. Es el instrumento de LECTURA del aparato --
    ///    la conductividad es la propiedad deliberadamente invisible del
    ///    juego, y esto es lo único que la delata, así que tenía que ser lo
    ///    más visible del banco, no un serpentín de 2 celdas.
    ///  · **ACUSE DE RECIBO** (mandato 3): el marco destella al recibir
    ///    muestra, y el banco LATE mientras analiza.
    ///
    /// LA MECÁNICA NO CAMBIA: E lee el material dominante de la bandeja,
    /// consulta <see cref="Universe.Conductividad"/> (0/1/2) y NO TRANSFORMA
    /// NADA. Es el único aparato de análisis puro del laboratorio: no
    /// transforma, revela.
    /// </summary>
    public sealed class BancoChispa : MonoBehaviour, IMaquinaInteractiva, IMovibleAnclaEsquina, IMaquinaUsableRemota
    {
        /// <summary>3.2 -&gt; 3.4: el banco mide 27 celdas y hay que poder atenderlo desde cualquiera de sus dos electrodos.</summary>
        private const float ProximityRange = 3.4f;

        // -----------------------------------------------------------------
        // GEOMETRÍA (playtest 27). Públicas: las lee Sim/SimLevelBuilder.cs.
        // -----------------------------------------------------------------
        /// <summary>Ancho del hueco de la bandeja. 3 -&gt; 13.</summary>
        public const int BandejaAncho = 13;
        /// <summary>Alto del hueco de la bandeja. 2 -&gt; 5. 13x5 = 65 celdas (antes 6).</summary>
        public const int BandejaAlto = 5;
        public const int MuroGrosor = 2;
        /// <summary>Ancho de cada plinto de piedra que sostiene un electrodo.</summary>
        public const int PlintoAncho = 5;
        /// <summary>Alto de los plintos: exactamente el labio de la bandeja, para que el banco tenga UNA línea horizontal de trabajo y no tres alturas distintas.</summary>
        public const int PlintoAlto = 6;
        /// <summary>Alto de los electrodos sobre su plinto.</summary>
        public const int ElectrodoAlto = 11;

        // ---- Alias históricos (regla 15): el playtest 26 llamaba "ranura" a
        // lo que hoy es una bandeja abierta. Se conservan para que ningún
        // comentario ni consumidor quede colgando.
        public const int RanuraAncho = BandejaAncho;
        public const int RanuraAlto = BandejaAlto;

        // -----------------------------------------------------------------
        // SUELO SOBERANO (playtest 32, mismo patrón que Game/Crisol.cs -- ver
        // sus docblocks de AplanarPlataforma/RestaurarSueloBase para el
        // porqué completo, "aparece un poco enterrada" / "rastro de bedrock").
        // -----------------------------------------------------------------
        private const int PlataformaMargen = 2;
        private const int PlataformaProfundidad = 6;

        // -----------------------------------------------------------------
        // LA LÁMPARA (playtest 32, fix "foco volando en el aire" -- ver el
        // docblock de <see cref="RecalcularRegion"/> junto a `_centroLampara`).
        // -----------------------------------------------------------------
        /// <summary>Diámetro de la ampolla (compartido por Filamento/Ampolla/Halo en BuildVisual y por el cálculo de altura de RecalcularRegion -- una sola fuente de verdad, ver regla 47 de CLAUDE.md: antes esta medida vivía DOS VECES, un `const int lamparaDiam = 7` local a BuildVisual y un "+12f" suelto en RecalcularRegion que no derivaba de nada).</summary>
        private const int LamparaDiametro = 7;
        /// <summary>Alto real del sprite de la ampolla/filamento en celdas -- MISMA fórmula que usa BuildVisual (`lamparaDiam * 1.5f`) para plantar los sprites; se repite aquí en vez de compartir un campo porque uno es const-time (BuildVisual) y el otro corre en RecalcularRegion antes de que exista ningún sprite.</summary>
        private const float LamparaAltoCeldas = LamparaDiametro * 1.5f;
        /// <summary>Celdas de cordón entre el travesaño y la CIMA de la ampolla -- lo que la ancla FÍSICAMENTE al pórtico en vez de dejarla flotando por encima. Corto a propósito: es un cordón, no una segunda lámpara colgando de otra.</summary>
        private const float LamparaColganteCeldas = 2f;
        /// <summary>Celdas sobre `_baseY+PlintoAlto+ElectrodoAlto` (la punta de los electrodos) donde cuelga el travesaño del pórtico -- ÚNICA fuente de verdad para BuildVisual (postes+travesaño) Y RecalcularRegion (_centroLampara): antes cada uno tenía su propio número ("+4f" aquí, "+12f" allá) y no coincidían, que es la causa raíz de "el foco vuela en el aire" (regla 47 de CLAUDE.md, no reutilizar/duplicar una constante de posición).</summary>
        private const float LamparaTravesanoOffsetCeldas = 4f;

        /// <summary>Cuánto se queda la lámpara encendida tras un análisis (teatro; el registro ocurre una vez al pulsar E).</summary>
        private const float BrilloDuracion = 3.5f;

        private AlkahestSim _sim;
        private Transform _player;
        private SubstanceKnowledge _conocimiento;

        private int _anchorX;
        private int _baseY;
        private int _banX0, _banX1, _banY0, _banY1;
        private int _outX0, _outX1, _outY0, _outY1;

        /// <summary>(playtest 29) Handle en <see cref="SimLevelBuilder.ObraDelTaller"/> -- ver el docblock gemelo en Game/Crisol.cs (`_handleObra`).</summary>
        private int _handleObra = -1;

        private Vector3 _centro, _centroBandeja, _centroRotulo, _centroLampara;

        private byte _ultimaConductividad;
        private float _brilloRestante;
        private string _chapaResultado;

        private SpriteRenderer _filamento, _resalte, _latidoTrabajo, _destelloMarco, _arco, _haloLampara;

        // (playtest 31, ILUMINACIÓN DE ÁNIMO) La lámpara del banco es la única
        // luz FRÍA del taller: el resto son fuegos. Que el cuarto cambie de
        // temperatura de color justo aquí es lo que hace que esta estación se
        // sienta un INSTRUMENTO y no otro horno. El halo pequeño de la propia
        // ampolla (_haloLampara, playtest 27) se conserva: este otro es el que
        // moja la piedra de alrededor.
        private MaquinariaSprites.Luz _luzLampara;
        private float _alfaResalte;
        private int _celdasBandejaPrev;

        private readonly MaquinariaSprites.Destello _acuse = new MaquinariaSprites.Destello();
        private readonly MaquinariaSprites.AffordanceGlow _pulsoTrabajo = new MaquinariaSprites.AffordanceGlow();

        private const float RangoEstadoPleno = 6.0f;
        private const float RangoEstadoDesvanece = 7.5f;
        private const float RangoNombrePleno = 3.2f;
        private const float RangoNombreDesvanece = 4.4f;
        private bool _yaConocida;

        public Vector3 PuntoFoco => _centroBandeja;
        public float RangoFoco => ProximityRange;

        public Vector3 CentroMundo => _centro;
        public Vector2 TamanoMundo => new Vector2(
            (_outX1 - _outX0 + 1) * SimRenderer.CellWorldSize,
            (_outY1 - _outY0 + 1) * SimRenderer.CellWorldSize);
        public Vector2Int AnclaCelda => new Vector2Int(_outX0, _baseY);

        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            int span = _outX1 - _outX0 + 1;
            int alto = _outY1 - _outY0 + 1;
            return anclaCelda.x >= 1 && anclaCelda.x + span - 1 <= CellGrid.W - 2
                && anclaCelda.y >= 1 && anclaCelda.y + alto - 1 <= CellGrid.H - 2;
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap. FIRMA SIN CAMBIOS. `anchorX` = SimLevelBuilder.BancoChispaX.</summary>
        public void Init(AlkahestSim sim, Transform player, SubstanceKnowledge conocimiento, int anchorX)
        {
            _sim = sim;
            _player = player;
            _conocimiento = conocimiento;
            _anchorX = anchorX;
            _baseY = SimLevelBuilder.BaseYDeEstacion(SimLevelBuilder.BancoChispaX); // (playtest 33) cota por zona -- ver BaseYDeEstacion.

            RecalcularRegion();
            BuildVisual();
            UpdateLamparaTint();

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

        private struct Huella
        {
            public int BanX0, BanX1, BanY0, BanY1;
            public int OutX0, OutX1, OutY0, OutY1;
        }

        private static Huella Calcular(int anchorX, int baseY)
        {
            Huella h;
            h.BanX0 = anchorX - BandejaAncho / 2;
            h.BanX1 = h.BanX0 + BandejaAncho - 1;
            h.BanY0 = baseY + 1;
            h.BanY1 = h.BanY0 + BandejaAlto - 1;
            h.OutX0 = h.BanX0 - MuroGrosor - PlintoAncho;
            h.OutX1 = h.BanX1 + MuroGrosor + PlintoAncho;
            h.OutY0 = baseY;
            h.OutY1 = baseY + PlintoAlto - 1;
            return h;
        }

        private void RecalcularRegion()
        {
            var h = Calcular(_anchorX, _baseY);
            _banX0 = h.BanX0; _banX1 = h.BanX1; _banY0 = h.BanY0; _banY1 = h.BanY1;
            _outX0 = h.OutX0; _outX1 = h.OutX1; _outY0 = h.OutY0; _outY1 = h.OutY1;

            float c = SimRenderer.CellWorldSize;
            _centro = new Vector3((_outX0 + (_outX1 - _outX0 + 1) * 0.5f) * c, (_outY0 + (_outY1 - _outY0 + 1) * 0.5f) * c, 0f);
            transform.position = _centro;
            _centroBandeja = new Vector3((_banX0 + BandejaAncho * 0.5f) * c, (_banY0 + BandejaAlto * 0.5f) * c, 0f);
            _centroRotulo = new Vector3(_centroBandeja.x, (_banY1 + 3f) * c, 0f);
            // (playtest 32, FIX "el foco vuela en el aire") ANTES: "+12f",
            // un número que no derivaba de nada (regla 47 de CLAUDE.md) --
            // el travesaño del pórtico (ver BuildVisual, `yTravesano`) cuelga
            // a SOLO +LamparaTravesanoOffsetCeldas(4), así que +12f dejaba la
            // ampolla 8 celdas POR ENCIMA del travesaño del que se supone que
            // cuelga: flotando sin nada que la sostenga. AHORA la ampolla
            // cuelga JUSTO DEBAJO del travesaño (mismo punto que usa
            // BuildVisual para plantarlo), separada solo por el cordón corto
            // que dibuja BuildVisual (ColganteLampara) -- ninguna de las dos
            // constantes puede volver a divergir porque las dos leen
            // LamparaTravesanoOffsetCeldas/LamparaColganteCeldas/
            // LamparaAltoCeldas, no un número suelto cada una.
            float yTravesano = _baseY + PlintoAlto + ElectrodoAlto + LamparaTravesanoOffsetCeldas;
            float yCentroLampara = yTravesano - LamparaColganteCeldas - LamparaAltoCeldas * 0.5f;
            _centroLampara = new Vector3(_centroBandeja.x, yCentroLampara * c, 0f);
        }

        /// <summary>(playtest 32, encargo A) Mismo AplanarPlataforma que Game/Crisol.cs: SIEMPRE lo primero, `baseY` SIEMPRE en el colchón de piedra (nunca en el vaciado -- el margen de <see cref="PlataformaMargen"/> más allá de los plintos no lo re-talla nadie), ver su docblock para el porqué completo.</summary>
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

        /// <summary>Talla la bandeja y los dos plintos sobre el CellGrid del plano.</summary>
        public static void TallarEnPlano(CellGrid grid, int anchorX, int baseY)
        {
            var h = Calcular(anchorX, baseY);

            AplanarPlataforma(grid, h.OutX0, h.OutX1, baseY, h.OutY1); // (playtest 32) SIEMPRE lo primero.

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

            // Los dos plintos: macizos hasta el labio de la bandeja, para que
            // el banco tenga UNA línea de trabajo continua.
            for (int y = baseY; y <= h.OutY1; y++)
                for (int k = 0; k < PlintoAncho; k++)
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
            // terrazas como si el Banco no existiera. `Init` RECLAMA este
            // mismo handle (ver SimLevelBuilder.HallarObraExacta).
            SimLevelBuilder.RegistrarObra(h.OutX0, h.OutY0, h.OutX1, h.OutY1);
        }

        /// <summary>Equivalente EN CALIENTE de <see cref="AplanarPlataforma"/> -- ver el docblock gemelo en Game/Crisol.cs (`AplanarPlataformaCaliente`). Misma convención de límites (`_baseY` en el colchón, vaciado desde `_baseY+1`).</summary>
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

        /// <summary>Misma geometría EN CALIENTE (regla 29). Solo la usa <see cref="Reposicionar"/> (Mudanza).</summary>
        private void TallarEnCaliente()
        {
            AplanarPlataformaCaliente(); // (playtest 32) SIEMPRE lo primero.
            for (int x = _banX0 - MuroGrosor; x <= _banX1 + MuroGrosor; x++) _sim.PaintStable(x, _banY0 - 1, 0, MaterialId.Stone);
            for (int y = _banY0 - 1; y <= _banY1; y++)
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.PaintStable(_banX0 - t, y, 0, MaterialId.Stone);
                    _sim.PaintStable(_banX1 + t, y, 0, MaterialId.Stone);
                }
            _sim.PaintRect(_banX0, _banY0, BandejaAncho, BandejaAlto, MaterialId.Empty);
            for (int y = _baseY; y <= _outY1; y++)
                for (int k = 0; k < PlintoAncho; k++)
                {
                    _sim.PaintStable(_outX0 + k, y, 0, MaterialId.Stone);
                    _sim.PaintStable(_outX1 - k, y, 0, MaterialId.Stone);
                }
        }

        /// <summary>(playtest 29, encargo B) Borra la mampostería VIEJA de la huella `h` -- ver el docblock gemelo en Game/Crisol.cs (`BorrarEnCaliente`) para el porqué exacto de cada exclusión.</summary>
        private void BorrarEnCaliente(Huella h)
        {
            for (int y = h.BanY0; y <= h.BanY1; y++)
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.Paint(h.BanX0 - t, y, 0, MaterialId.Empty);
                    _sim.Paint(h.BanX1 + t, y, 0, MaterialId.Empty);
                }
            // Los dos plintos, EXCLUYENDO la fila `h.OutY0` (=baseY, la losa
            // compartida -- jamás piedra del mundo).
            for (int y = h.OutY0 + 1; y <= h.OutY1; y++)
                for (int k = 0; k < PlintoAncho; k++)
                {
                    _sim.Paint(h.OutX0 + k, y, 0, MaterialId.Empty);
                    _sim.Paint(h.OutX1 - k, y, 0, MaterialId.Empty);
                }
        }

        /// <summary>(playtest 32, fix "rastro de bedrock") Ver el docblock gemelo en Game/Crisol.cs (`RestaurarSueloBase`).</summary>
        private void RestaurarSueloBase(Huella h)
        {
            int sueloBase = SimLevelBuilder.CuartoY0 + SimLevelBuilder.WallThickness - 1;
            int x0 = h.OutX0 - PlataformaMargen;
            int x1 = h.OutX1 + PlataformaMargen;
            for (int y = sueloBase + 1; y <= h.OutY0; y++)
                for (int x = x0; x <= x1; x++)
                    _sim.Paint(x, y, 0, MaterialId.Empty);
        }

        private void OnDestroy()
        {
            MachineFocus.Olvidar(this);
            Mudanza.OlvidarMovible(this);
        }

        public void Reposicionar(Vector2Int anclaCelda)
        {
            var huellaVieja = Calcular(_anchorX, _baseY);
            BorrarEnCaliente(huellaVieja); // 1) BORRAR la mampostería vieja.
            RestaurarSueloBase(huellaVieja); // (playtest 32) limpia cualquier pedestal elevado que esta instancia hubiera dejado ahí.

            _anchorX += anclaCelda.x - _outX0;
            _baseY = anclaCelda.y;
            RecalcularRegion();
            TallarEnCaliente(); // 2) TALLAR la nueva.

            SimLevelBuilder.ActualizarObra(_handleObra, _outX0, _outY0, _outX1, _outY1); // 3) ACTUALIZAR el registro anticincel.
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return;

            SondearBandeja();

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && EstaEnfocada())
            {
                Analizar();
                MachineFocus.RegistrarUsoE(); // (ENCARGO N) el E local SIEMPRE cuenta como "uso enseñado" aunque la bandeja esté vacía -- sin cambios de este encargo, ver el docblock de Analizar.
            }

            if (_brilloRestante > 0f)
            {
                _brilloRestante -= Time.deltaTime;
                if (_brilloRestante < 0f) _brilloRestante = 0f;
            }

            _acuse.Avanzar(Time.deltaTime);
            _pulsoTrabajo.Trabajando = _brilloRestante > 0f;

            UpdateLamparaTint();
            ActualizarVisual();
        }

        /// <summary>Cuenta la bandeja y dispara el ACUSE DE RECIBO (mandato 3) cuando entra materia.</summary>
        private void SondearBandeja()
        {
            var grid = _sim.Grid;
            int n = 0;
            for (int y = _banY0; y <= _banY1; y++)
                for (int x = _banX0; x <= _banX1; x++)
                    if (grid.GetMat(x, y) != MaterialId.Empty) n++;
            if (n > _celdasBandejaPrev) _acuse.Disparar();
            _celdasBandejaPrev = n;
        }

        private bool EstaEnfocada() => MachineFocus.EsFoco(this, _player);

        // -----------------------------------------------------------------
        // ANÁLISIS PURO (sin cambios de mecánica).
        // -----------------------------------------------------------------
        /// <summary>
        /// (ENCARGO N, playtest 43) EL HANDLER COMPARTIDO DE E -- sin
        /// cambios de lógica, solo la firma pasa de `void` a `bool` (el
        /// proximity check de `EstaEnfocada()` vive en `Update()`, fuera de
        /// este método, así que no había nada que extraer). Devuelve true
        /// incluso cuando la bandeja está vacía: el análisis "sí ocurrió"
        /// (hay un resultado que mostrar, "deja una muestra"), solo que no
        /// hay muestra que leer -- distinto de "no procedía" (ver
        /// <see cref="Alkahest.Net.IMaquinaUsableRemota"/>).
        /// </summary>
        private bool Analizar()
        {
            var universe = _sim.Universe;
            var grid = _sim.Grid;
            if (universe == null || grid == null) return false;

            byte dominanteMat = MaterialId.Empty;
            int dominanteCount = 0;
            for (int y = _banY0; y <= _banY1; y++)
            {
                for (int x = _banX0; x <= _banX1; x++)
                {
                    byte mat = grid.GetMat(x, y);
                    if (mat == MaterialId.Empty) continue;
                    int count = 0;
                    for (int y2 = _banY0; y2 <= _banY1; y2++)
                        for (int x2 = _banX0; x2 <= _banX1; x2++)
                            if (grid.GetMat(x2, y2) == mat) count++;
                    if (count > dominanteCount) { dominanteCount = count; dominanteMat = mat; }
                }
            }

            if (dominanteMat == MaterialId.Empty)
            {
                _ultimaConductividad = 0;
                _chapaResultado = "deja una muestra en la bandeja";
                _brilloRestante = BrilloDuracion;
                return true;
            }

            byte conductividad = universe.Conductividad(dominanteMat);
            _ultimaConductividad = conductividad;
            _brilloRestante = BrilloDuracion;

            _chapaResultado = conductividad switch
            {
                2 => "brillo pleno",
                1 => "brillo tenue",
                _ => "ni un parpadeo",
            };

            string observacion = conductividad >= 1 ? "encendió la lámpara" : "la lámpara ni parpadeó";
            if (_conocimiento != null) _conocimiento.RegistrarObservacionPropiedad(dominanteMat, observacion);
            // (integración pt40, SEMILLA CERO) El testigo de conductividad:
            // Game/SemillaCero.cs completa "¿Esto CONDUCE?" cuando la lámpara
            // dictó sentencia AQUÍ (la sala del Ensayo sigue tapiada en ese
            // beat). Global e inocuo fuera de Semilla 0: solo un máximo.
            if (_conocimiento != null) _conocimiento.RegistrarConductividadObservada(conductividad);
            return true;
        }

        // =================================================================
        // (ENCARGO N, playtest 43, CONTRATO_PARIDAD.md §2a/§2b) EL GANCHO REMOTO
        // =================================================================
        bool IMaquinaUsableRemota.UsarPorRed() => Analizar();

        /// <summary>
        /// bit4 LuzPlena (contrato §1: "lámpara del banco dictaminando") =
        /// brillo pleno (conductividad 2) AHORA MISMO (mientras dura
        /// <see cref="_brilloRestante"/>); Trabajando cubre el mismo lapso
        /// que <see cref="_pulsoTrabajo"/> ya usa localmente -- el aparato es
        /// instantáneo, pero el teatro (arco + lámpara) dura
        /// <see cref="BrilloDuracion"/> segundos, y ESO es lo que "trabaja".
        /// </summary>
        byte IMaquinaUsableRemota.EstadoVivoRed()
        {
            byte b = 0;
            if (_brilloRestante > 0f) b |= EstadoVivoBits.Trabajando;
            if (_brilloRestante > 0f && _ultimaConductividad == 2) b |= EstadoVivoBits.LuzPlena;
            return b;
        }

        // =================================================================
        // VISUAL
        // =================================================================
        private void BuildVisual()
        {
            float c = SimRenderer.CellWorldSize;

            // ---- Los dos plintos, vestidos de sillería.
            var sillar = MaquinariaSprites.Sillar(PlintoAncho, PlintoAlto);
            for (int lado = 0; lado < 2; lado++)
            {
                int x0 = lado == 0 ? _outX0 : _outX1 - PlintoAncho + 1;
                var go = new GameObject(lado == 0 ? "ChispaPlintoIzq" : "ChispaPlintoDer");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3((x0 + PlintoAncho * 0.5f) * c, (_baseY + PlintoAlto * 0.5f) * c, 0f);
                MaquinariaSprites.CrearCapa(go.transform, "Sprite", sillar, 18, PlintoAncho * c, PlintoAlto * c);
            }

            // ---- LA BANDEJA ABIERTA (mandato 2): marco transparente por
            // dentro, sin ningún embudo (esto no recibe por vertido).
            int spanBandeja = BandejaAncho + 2 * MuroGrosor; // 17
            int altoBandeja = BandejaAlto + 1;               // 6
            float anchoW = spanBandeja * c, altoW = altoBandeja * c;
            var marco = MaquinariaSprites.MarcoBandeja(spanBandeja, altoBandeja);
            var bandejaGo = new GameObject("ChispaBandeja");
            bandejaGo.transform.SetParent(transform, false);
            bandejaGo.transform.position = new Vector3((_banX0 - MuroGrosor + spanBandeja * 0.5f) * c, (_baseY + altoBandeja * 0.5f) * c, 0f);

            _resalte = MaquinariaSprites.CrearCapa(bandejaGo.transform, "Resalte", marco, 14, anchoW * 1.12f, altoW * 1.22f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);
            _latidoTrabajo = MaquinariaSprites.CrearCapa(bandejaGo.transform, "LatidoTrabajo", marco, 15, anchoW * 1.06f, altoW * 1.12f);
            _latidoTrabajo.color = new Color(0.55f, 0.85f, 1f, 0f);
            MaquinariaSprites.CrearCapa(bandejaGo.transform, "Marco", marco, 19, anchoW, altoW);
            _destelloMarco = MaquinariaSprites.CrearCapa(bandejaGo.transform, "Acuse", marco, 22, anchoW, altoW);
            _destelloMarco.color = new Color(0.9f, 1f, 1f, 0f);

            // ---- Los dos electrodos, plantados en la cara INTERIOR de cada
            // plinto: sus puntas quedan enfrentadas justo encima de la bandeja.
            const int electrodoAncho = 3;
            float yElectrodo = (_baseY + PlintoAlto + ElectrodoAlto * 0.5f) * c;
            float xIzq = (_outX0 + PlintoAncho - electrodoAncho * 0.5f) * c;
            float xDer = (_outX1 - PlintoAncho + electrodoAncho * 0.5f + 1f) * c;
            var electrodo = MaquinariaSprites.Electrodo(electrodoAncho, ElectrodoAlto);
            var elIzqGo = new GameObject("ChispaElectrodoIzq");
            elIzqGo.transform.SetParent(transform, false);
            elIzqGo.transform.position = new Vector3(xIzq, yElectrodo, 0f);
            MaquinariaSprites.CrearCapa(elIzqGo.transform, "Sprite", electrodo, 20, electrodoAncho * c, ElectrodoAlto * c);
            var elDerGo = new GameObject("ChispaElectrodoDer");
            elDerGo.transform.SetParent(transform, false);
            elDerGo.transform.position = new Vector3(xDer, yElectrodo, 0f);
            MaquinariaSprites.CrearCapa(elDerGo.transform, "Sprite", electrodo, 20, electrodoAncho * c, ElectrodoAlto * c);

            // ---- EL ARCO, entre las dos puntas y sobre la bandeja. Solo se
            // ve si la muestra conduce: la AUSENCIA de arco es el dato.
            var arcoGo = new GameObject("ChispaArco");
            arcoGo.transform.SetParent(transform, false);
            arcoGo.transform.position = new Vector3(_centroBandeja.x, (_baseY + PlintoAlto + ElectrodoAlto - 1f) * c, -0.02f);
            _arco = MaquinariaSprites.CrearCapa(arcoGo.transform, "Sprite", MaquinariaSprites.Arco(), 21,
                (xDer - xIzq) * 0.92f, 3f * c);
            _arco.color = new Color(0.75f, 0.90f, 1f, 0f);

            // ---- EL PÓRTICO DE LA LÁMPARA: dos montantes de latón desde los
            // plintos y un travesaño del que cuelga la ampolla. La lectura del
            // aparato tiene que verse desde lejos.
            var teja = MaquinariaSprites.Solido();
            float yTravesanoCeldas = _baseY + PlintoAlto + ElectrodoAlto + LamparaTravesanoOffsetCeldas;
            float yTravesano = yTravesanoCeldas * c;
            for (int lado = 0; lado < 2; lado++)
            {
                float x = lado == 0 ? (_outX0 + 1.5f) * c : (_outX1 - 0.5f) * c;
                var poste = MaquinariaSprites.CrearCapa(transform, lado == 0 ? "PosteIzq" : "PosteDer", teja, 17,
                    1f * c, yTravesano - (_baseY + PlintoAlto) * c);
                poste.transform.position = new Vector3(x, ((_baseY + PlintoAlto) * c + yTravesano) * 0.5f, 0f);
                poste.color = new Color(0.62f, 0.48f, 0.24f, 1f);
            }
            var travesano = MaquinariaSprites.CrearCapa(transform, "Travesano", teja, 17, (_outX1 - _outX0 + 1) * c, 1.4f * c);
            travesano.transform.position = new Vector3(_centro.x, yTravesano, 0f);
            travesano.color = new Color(0.72f, 0.56f, 0.28f, 1f);

            // ---- EL CORDÓN: la pieza que faltaba (playtest 32, fix "el foco
            // vuela en el aire"). Un tramo de latón, MISMO sprite que los
            // postes, del travesaño a la cima de la ampolla -- sin él la
            // ampolla quedaba a 8 celdas por encima de este travesaño, sin
            // NADA que la sostuviera visualmente. Nace del travesaño (sprite
            // continuo con él, mismo ancho de 1 celda que los postes) y
            // termina justo donde arranca la ampolla: ni un hueco.
            float yCimaAmpolla = (_centroLampara.y / c) + LamparaAltoCeldas * 0.5f;
            var cordon = MaquinariaSprites.CrearCapa(transform, "ColganteLampara", teja, 17,
                1f * c, (yTravesanoCeldas - yCimaAmpolla) * c);
            cordon.transform.position = new Vector3(_centroLampara.x, (yTravesanoCeldas + yCimaAmpolla) * 0.5f * c, 0f);
            cordon.color = new Color(0.62f, 0.48f, 0.24f, 1f);

            var lamparaGo = new GameObject("ChispaLampara");
            lamparaGo.transform.SetParent(transform, false);
            lamparaGo.transform.position = _centroLampara;
            // (segunda pasada) HALO: una lámpara que se enciende y no ilumina
            // NADA a su alrededor no se lee como una lámpara. Va detrás de
            // todo (orden 16) y solo se enciende con el resultado, así que
            // sigue siendo la AUSENCIA de luz lo que informa cuando no conduce.
            _haloLampara = MaquinariaSprites.CrearCapa(lamparaGo.transform, "Halo",
                MaquinariaSprites.Humo(), 16, LamparaDiametro * 3.4f * c, LamparaDiametro * 3.4f * c);
            _haloLampara.color = new Color(0.75f, 0.88f, 1f, 0f);
            _filamento = MaquinariaSprites.CrearCapa(lamparaGo.transform, "Filamento",
                MaquinariaSprites.FilamentoLampara(LamparaDiametro), 20, LamparaDiametro * c, LamparaAltoCeldas * c);
            MaquinariaSprites.CrearCapa(lamparaGo.transform, "Ampolla",
                MaquinariaSprites.AmpollaLampara(LamparaDiametro), 21, LamparaDiametro * c, LamparaAltoCeldas * c);

            // ---- (playtest 31) SOMBRA PROPIA bajo los dos plintos + LA LUZ
            // de la lámpara sobre el cuarto.
            MaquinariaSprites.Sombra(transform, new Vector3((_outX0 + PlintoAncho * 0.5f) * c, (_baseY - 0.3f) * c, 0f),
                PlintoAncho * 2.4f * c, 3.2f * c, 0.40f);
            MaquinariaSprites.Sombra(transform, new Vector3((_outX1 - PlintoAncho * 0.5f + 1f) * c, (_baseY - 0.3f) * c, 0f),
                PlintoAncho * 2.4f * c, 3.2f * c, 0.40f);
            _luzLampara = MaquinariaSprites.Luz.Crear(transform, "LuzLampara", _centroLampara,
                34f * c, new Color(0.68f, 0.86f, 1f));
        }

        private void UpdateLamparaTint()
        {
            if (_filamento == null) return;
            float t = BrilloDuracion > 0f ? Mathf.Clamp01(_brilloRestante / BrilloDuracion) : 0f;
            if (_ultimaConductividad == 0 || t <= 0f)
            {
                // (segunda pasada) Apagado, el filamento tira a ÁMBAR OSCURO,
                // no a gris: un hilo de tungsteno frío sigue siendo un hilo
                // metálico, y con el gris la ampolla entera se leía como un
                // huevo de piedra.
                _filamento.color = new Color(0.46f, 0.38f, 0.28f, 1f);
                if (_haloLampara != null) _haloLampara.color = new Color(0.75f, 0.88f, 1f, 0f);
                _luzLampara?.Intensidad(0f); // (playtest 31) apagada de verdad: la AUSENCIA de luz sigue siendo el dato.
                return;
            }

            float pulso = 0.85f + 0.15f * Mathf.Sin(Time.time * 5f);
            float intensidad = (_ultimaConductividad == 2 ? 1f : 0.45f) * t * pulso;
            _filamento.color = new Color(0.55f + 0.45f * intensidad, 0.70f + 0.30f * intensidad, 0.85f + 0.15f * intensidad, 1f);
            if (_haloLampara != null) _haloLampara.color = new Color(0.75f, 0.88f, 1f, intensidad * 0.42f);
            _luzLampara?.Intensidad(intensidad * 0.30f); // (playtest 31) la lámpara moja de azul la piedra de alrededor mientras dura la lectura.
        }

        private void ActualizarVisual()
        {
            if (_arco != null)
            {
                bool visible = _ultimaConductividad >= 1 && _brilloRestante > 0f;
                if (!visible) _arco.color = new Color(0.75f, 0.90f, 1f, 0f);
                else
                {
                    float t = Mathf.Clamp01(_brilloRestante / BrilloDuracion);
                    float parpadeo = 0.7f + 0.3f * Mathf.Sin(Time.time * 18f); // eléctrico: rápido, no un pulso suave.
                    _arco.color = new Color(0.75f, 0.90f, 1f, t * parpadeo);
                }
            }

            if (_latidoTrabajo != null)
                _latidoTrabajo.color = new Color(0.55f, 0.85f, 1f, _pulsoTrabajo.AlfaTrabajo * 0.45f);
            if (_destelloMarco != null)
                _destelloMarco.color = new Color(0.9f, 1f, 1f, _acuse.Alfa);

            if (_resalte != null)
            {
                float objetivo = EstaEnfocada() ? 0.55f + 0.20f * Mathf.Sin(Time.time * 4f) : 0f;
                _alfaResalte = Mathf.MoveTowards(_alfaResalte, objetivo, 6f * Time.deltaTime);
                _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, _alfaResalte);
            }
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return;

            float cercaniaEstado = UiStyles.Cercania(_centroBandeja, _player, RangoEstadoPleno, RangoEstadoDesvanece);
            float cercaniaNombre = UiStyles.Cercania(_centroBandeja, _player, RangoNombrePleno, RangoNombreDesvanece);
            if (cercaniaEstado <= 0f && cercaniaNombre <= 0f) return;
            if (!_yaConocida && cercaniaNombre >= 0.98f) _yaConocida = true;

            UiStyles.Preparar();

            if (_chapaResultado != null && _brilloRestante > 0f)
            {
                Color color = _ultimaConductividad >= 1 ? UiStyles.Exito : UiStyles.TextoTenue;
                UiStyles.PlacaMundo(_centroRotulo, _chapaResultado,
                    new Color(color.r, color.g, color.b, color.a * cercaniaEstado), -UiStyles.S(6f));
            }

            if (!_yaConocida)
            {
                Color tenue = UiStyles.TextoTenue;
                UiStyles.PlacaMundo(_centroRotulo, "el banco de chispa", new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercaniaNombre), -UiStyles.S(23f));
            }

            if (MachineFocus.MostrarPromptE && EstaEnfocada() && !UiStyles.RatonOcupado)
            {
                UiStyles.PlacaMundo(_centroRotulo, "E — analizar",
                    new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, cercaniaNombre), -UiStyles.S(23f));
            }
        }
    }
}
