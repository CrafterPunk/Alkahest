using UnityEngine;
using UnityEngine.InputSystem;
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
    public sealed class EnsayoMaestro : MonoBehaviour, IMaquinaInteractiva
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
        private int _outX0, _outX1;
        private Vector3 _centro, _centroRotulo;

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
        private float _alfaResalte;
        private int _celdasBandejaPrev;

        private readonly MaquinariaSprites.Destello _acuse = new MaquinariaSprites.Destello();
        private readonly MaquinariaSprites.AffordanceGlow _pulsoTrabajo = new MaquinariaSprites.AffordanceGlow();

        private const float RangoNombrePleno = 3.2f;
        private const float RangoNombreDesvanece = 4.4f;
        private bool _yaConocida;

        public Vector3 PuntoFoco => _centro;
        public float RangoFoco => ProximityRange;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap. FIRMA CONGELADA.</summary>
        public void Init(AlkahestSim sim, OrderSystem orders, Transform jugador)
        {
            _sim = sim;
            _orders = orders;
            _player = jugador;
            _knowledge = FindAnyObjectByType<SubstanceKnowledge>();

            _plintoX = SimLevelBuilder.EnsayoPlintoX;
            _baseY = SimLevelBuilder.CuartoY0 + 2;

            var h = Calcular(_plintoX, _baseY);
            _x0 = h.BanX0; _x1 = h.BanX1; _y0 = h.BanY0; _y1 = h.BanY1;
            _daisX0 = h.DaisX0; _daisX1 = h.DaisX1;
            _outX0 = h.OutX0; _outX1 = h.OutX1;

            float c = SimRenderer.CellWorldSize;
            _centro = new Vector3((_x0 + PlintoAncho * 0.5f) * c, (_y0 + PlintoAltoInterior * 0.5f) * c, 0f);
            _centroRotulo = new Vector3(_centro.x, (_y1 + 3f) * c, 0f);

            BuildVisual();
            MachineFocus.Registrar(this);
        }

        private void OnDestroy()
        {
            MachineFocus.Olvidar(this);
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

        /// <summary>Talla el altar entero (dais + bandeja + las dos columnas del dosel) sobre el CellGrid del plano.</summary>
        public static void TallarEnPlano(CellGrid grid, int plintoX, int baseY)
        {
            var h = Calcular(plintoX, baseY);

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

            SimLevelBuilder.RegistrarObra(h.OutX0, h.OutY0, h.OutX1, h.OutY1); // el nicho del hogar queda dentro de este rect.
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
                MachineFocus.RegistrarUsoE();
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

        private void TryEnsayo()
        {
            Order objetivo = BuscarOrdenEnsayoActiva();
            if (objetivo == null)
            {
                Rotular("no hay ningún pedido de calor o chispa activo ahora mismo", UiStyles.TextoTenue);
                return;
            }

            if (!MuestraDominante(out byte matId, out int n0) || n0 == 0)
            {
                Rotular("presenta una muestra en la bandeja antes de pulsar E", UiStyles.Aviso);
                return;
            }

            if (objetivo.Tipo == OrderType.Conduce)
            {
                EvaluarConduce(matId);
                return;
            }

            _calentandoDominante = matId;
            _calentandoN0 = n0;
            _calentandoHasta = Time.time + RampSeconds;
            _fase = Fase.Calentando;
            Rotular("calentando la muestra al rojo del crisol...", UiStyles.Aviso);
        }

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
