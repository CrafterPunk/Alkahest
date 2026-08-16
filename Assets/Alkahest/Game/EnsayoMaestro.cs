using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// [ChaosAlchemy · playtest 25, CONTRATO_PERSISTE.md §6.2] EL ENSAYO DEL
    /// MAESTRO: el plinto junto a la boca del pasillo (constante
    /// <c>SimLevelBuilder.EnsayoPlintoX</c>, escrita por el encargo A en
    /// paralelo -- se referencia por nombre, nunca se inventa el valor).
    /// Vierte una muestra encima y pulsa E con un pedido AguantaCalor/Conduce
    /// activo:
    ///
    ///  · AGUANTACALOR: calienta la muestra A LA VISTA hasta
    ///    <c>Universe.TempEnsayoCalorRaw</c> durante <see cref="RampSeconds"/>
    ///    (el material brilla solo -- el motor YA sabe hacerlo vía
    ///    <c>emision</c>, no hay teatro que fingir aquí, solo calor de
    ///    verdad sobre celdas reales) y cuenta supervivientes del material
    ///    dominante: ≥60% intactas = cumplido. Estrellas por MARGEN REAL de
    ///    <c>Universe.UmbralPersistenciaRaw</c> sobre la temperatura pedida.
    ///  · CONDUCE: instantáneo, consulta <c>Universe.Conductividad</c> del
    ///    dominante.
    ///  · FALLO: el pedido NO se consume (nunca se llama a
    ///    <see cref="OrderSystem.CompletarEnsayo"/>) y el rótulo dice CÓMO
    ///    murió la muestra -- el fallo es información, no solo un "no".
    ///
    /// Firma de <see cref="Init"/> CONGELADA por el contrato (B la cablea en
    /// el bootstrap): <c>Init(AlkahestSim sim, OrderSystem orders, Transform
    /// jugador)</c> -- no lleva <c>SubstanceKnowledge</c> aunque el contrato
    /// §6.4 pide que el Ensayo anote observaciones ahí (mismo hook que
    /// BancoChispa). DECISIÓN de este encargo: se busca con
    /// <c>FindAnyObjectByType&lt;SubstanceKnowledge&gt;()</c> (regla 1 de
    /// CLAUDE.md la sanciona; mismo patrón perezoso que ya usa
    /// DeliveryChute.cs para encontrar al aprendiz sin que su Init lo reciba).
    ///
    /// LA CUBETA DEL PLINTO: un pequeño basín de <see cref="MaterialId.Stone"/>
    /// (StaticSolid, no cae -- regla 7 de CLAUDE.md) tallado con
    /// <c>AlkahestSim.PaintStable</c> en Init, para que la muestra vertida no
    /// se desparrame por el suelo del cuarto antes de poder ensayarla. Mismo
    /// material y mismo patrón (MuroGrosor=1, dos muros laterales) que las
    /// cubetas de Crisol.cs/Prensa.cs/BancoChispa.cs (encargo B, en
    /// paralelo): visualmente coherente en todo el cuarto.
    ///
    /// -----------------------------------------------------------------------
    /// PLAYTEST 26 (CONTRATO_LEGIBILIDAD.md, encargo M) — EnsayoMaestro.cs
    /// pasa a ser propiedad del MISMO encargo que MaquinariaSprites.cs, así
    /// que el párrafo de arriba ("losa de piedra... sin depender de
    /// MaquinariaSprites, propiedad de B") queda como ARCHIVO HISTÓRICO: esa
    /// restricción YA NO EXISTE. Tres cambios:
    /// (1) La mampostería del plinto YA NO se talla en Init() (regla 47:
    ///     SimLevelBuilder talla TODA la mampostería del plano) -- ver
    ///     <see cref="TallarEnPlano"/> (estático). Como el Ensayo NO es
    ///     IMovible (a diferencia de Crisol/Prensa/BancoChispa), no hay
    ///     Reposicionar que necesite seguir tallando en caliente: <see
    ///     cref="TallarCubeta"/> de instancia queda intacta sin llamantes
    ///     (regla 15 de CLAUDE.md: se comenta el porqué, no se borra).
    /// (2) LA GRAMÁTICA VISUAL (§1.4: "Ensayo = pedestal ceremonial más
    ///     alto, con brasero propio incorporado -- es EL examen: se ve
    ///     importante"): la losa plana de 1x1 se sustituye por un pedestal
    ///     construido con <see cref="MaquinariaSprites"/> (mismo idioma
    ///     latón+carboncillo del resto del cuarto): chasis alto + marco de
    ///     latón (§1.3) + embudo arriba (§1.1) + un brasero en miniatura
    ///     incorporado en la base (decorativo -- el Ensayo no quema
    ///     combustible propio, solo hace más intenso su rescoldo mientras
    ///     calienta A LA VISTA, eco visual del Crisol).
    /// (3) EL AFFORDANCE GLOW (§1.5: "Ensayo: hay pedido activo de
    ///     AguantaCalor/Conduce") usa el helper único
    ///     <see cref="MaquinariaSprites.AffordanceGlow"/> -- a diferencia de
    ///     las otras cuatro bocas, el predicado NO mira el material en sí
    ///     (cualquier muestra sirve para presentarse al examen), solo si hay
    ///     un pedido de ese tipo activo (ver <see cref="MaterialSirvePlinto"/>).
    /// </summary>
    public sealed class EnsayoMaestro : MonoBehaviour, IMaquinaInteractiva
    {
        // -----------------------------------------------------------------
        // Geometría del plinto (decisión de este encargo: A solo fija la X
        // vía SimLevelBuilder.EnsayoPlintoX; el resto del footprint es
        // propiedad de este archivo, igual que Crisol/Prensa/BancoChispa son
        // propiedad de B con solo su X heredada del plano). (playtest 26) De
        // privadas a PÚBLICAS: SimLevelBuilder las lee para tallar la misma
        // geometría en el plano.
        // -----------------------------------------------------------------
        public const int PlintoAncho = 5; // 3 de hueco interior + 2 muros de piedra (mismo criterio que las cubetas de B, ver docblock de la clase).
        public const int PlintoAltoInterior = 4; // celdas de hueco útil sobre el suelo.
        public const int MuroGrosor = 1;

        private const float ProximityRange = 3.0f;

        /// <summary>Cuánto dura el calentamiento visible antes de evaluar supervivientes (contrato: "~5s A LA VISTA").</summary>
        private const float RampSeconds = 5f;

        /// <summary>Empuje máximo de temperatura por tick hacia el objetivo -- mismo patrón que HeatPlate.ApplyHeatTick, sin caída por distancia (footprint pequeño, empuje uniforme).</summary>
        private const int TempStepPerTick = 8;

        /// <summary>≥60% de la muestra original todavía siendo el mismo material = pedido cumplido (contrato §6.2).</summary>
        private const float FraccionSupervivenciaMinima = 0.6f;

        /// <summary>Margen (raw) de UmbralPersistenciaRaw sobre la temperatura pedida para cada estrella (contrato §6.2).</summary>
        private const int MargenDosEstrellas = 15;
        private const int MargenTresEstrellas = 30;

        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 4; // hasta 5s de calor pueden acumularse tras un frame lento (ventana minimizada, etc.).

        private const float RotuloResultadoSeg = 4.5f;

        private AlkahestSim _sim;
        private OrderSystem _orders;
        private Transform _player;
        private SubstanceKnowledge _knowledge; // ver docblock de clase: buscado, no inyectado (Init congelada).

        private int _x0, _x1; // interior útil (sin muros).
        private int _y0, _y1;
        private Vector3 _centro;

        private float _accumulator;

        private enum Fase { Ocioso, Calentando }
        private Fase _fase = Fase.Ocioso;
        private float _calentandoHasta;
        private byte _calentandoDominante;
        private int _calentandoN0;

        private string _rotulo;
        private Color _rotuloColor = UiStyles.Oro;
        private float _rotuloHasta;

        // ---- Playtest 26 (contrato §1): gramática visual + affordance glow ----
        private Flask _flask; // solo lectura (contrato: "sin tocar Flask.cs").
        private MaquinariaSprites.AffordanceGlow _glowPlinto = new MaquinariaSprites.AffordanceGlow();
        private System.Func<byte, bool> _sirvePlinto;
        private SpriteRenderer _afordancePlinto;

        // Foco de interacción (Game/MachineFocus.cs): solo el aparato más
        // cercano responde a E.
        public Vector3 PuntoFoco => _centro;
        public float RangoFoco => ProximityRange;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap. FIRMA CONGELADA (contrato §6.5).</summary>
        public void Init(AlkahestSim sim, OrderSystem orders, Transform jugador)
        {
            _sim = sim;
            _orders = orders;
            _player = jugador;
            _knowledge = FindAnyObjectByType<SubstanceKnowledge>(); // ver docblock: Init no la trae, se busca (regla 1 de CLAUDE.md).
            _flask = jugador != null ? jugador.GetComponent<Flask>() : null;
            _sirvePlinto = MaterialSirvePlinto;

            int plintoX = SimLevelBuilder.EnsayoPlintoX;
            int suelo = SimLevelBuilder.CuartoY0 + 2; // (contrato §6.2/§4.5) "emplazamientos DENTRO del cuarto, suelo y=CuartoY0+2".

            int mitad = PlintoAncho / 2;
            _x0 = plintoX - mitad + MuroGrosor;
            _x1 = plintoX - mitad + PlintoAncho - 1 - MuroGrosor;
            _y0 = suelo;
            _y1 = suelo + PlintoAltoInterior - 1;

            float celda = SimRenderer.CellWorldSize;
            _centro = new Vector3((_x0 + _x1 + 1) * 0.5f * celda, (_y0 + _y1 + 1) * 0.5f * celda, 0f);

            // (playtest 26) YA NO se talla aquí -- SimLevelBuilder.BuildCuartoIntimo
            // llama a TallarEnPlano con esta misma geometría. TallarCubeta() se
            // conserva sin llamantes (regla 15): el Ensayo no es IMovible, así que
            // a diferencia de Crisol/Prensa/BancoChispa no hay Reposicionar que la
            // necesite en caliente -- ver el docblock de la clase.
            BuildVisual();

            MachineFocus.Registrar(this);
        }

        /// <summary>Contrato §1.5: "Ensayo: hay pedido activo de AguantaCalor/Conduce" -- a diferencia de las otras cuatro bocas, no mira `mat` (cualquier muestra sirve para presentarse), solo si hay un pedido del tipo correcto esperando.</summary>
        private bool MaterialSirvePlinto(byte mat) => BuscarOrdenEnsayoActiva() != null;

        private void OnDestroy()
        {
            MachineFocus.Olvidar(this);
        }

        /// <summary>Talla los dos muros de Stone (StaticSolid: no cae) que contienen la muestra -- suelo ya es piedra maciza del cuarto, no hace falta tallar debajo. Sin llamantes desde playtest 26 (ver el docblock de la clase); se conserva por regla 15.</summary>
        private void TallarCubeta(int muroXIzq, int muroXDer, int suelo)
        {
            if (_sim == null) return;
            for (int y = suelo; y <= suelo + PlintoAltoInterior - 1; y++)
            {
                _sim.PaintStable(muroXIzq, y, 0, MaterialId.Stone);
                _sim.PaintStable(muroXDer, y, 0, MaterialId.Stone);
            }
        }

        /// <summary>
        /// (playtest 26) Talla el plinto del Ensayo DIRECTAMENTE sobre el
        /// CellGrid del plano -- llamado por Sim/SimLevelBuilder.cs al
        /// construir el nivel. Misma geometría EXACTA que <see cref="Init"/>
        /// calculaba a mano (muros a `plintoX-mitad`/`plintoX-mitad+PlintoAncho-1`,
        /// suelo ya macizo del cuarto -- ver BuildCuartoFloor).
        /// </summary>
        public static void TallarEnPlano(CellGrid grid, int plintoX, int suelo)
        {
            int mitad = PlintoAncho / 2;
            int muroXIzq = plintoX - mitad;
            int muroXDer = plintoX - mitad + PlintoAncho - 1;
            for (int y = suelo; y <= suelo + PlintoAltoInterior - 1; y++)
            {
                if (CellGrid.InBounds(muroXIzq, y)) grid.SetCell(muroXIzq, y, MaterialId.Stone);
                if (CellGrid.InBounds(muroXDer, y)) grid.SetCell(muroXDer, y, MaterialId.Stone);
            }
        }

        /// <summary>
        /// GRAMÁTICA §1.4: pedestal ceremonial construido con MaquinariaSprites
        /// (mismo idioma latón+carboncillo del resto del cuarto) -- chasis alto
        /// + marco (§1.3) + embudo arriba (§1.1) + un brasero en miniatura
        /// incorporado en la base (decorativo, "se ve importante").
        /// </summary>
        private void BuildVisual()
        {
            float celda = SimRenderer.CellWorldSize;
            int span = _x1 - _x0 + 1 + MuroGrosor * 2;
            float ancho = span * celda;
            // Pedestal MÁS ALTO que las demás cubetas (contrato §1.4): el doble
            // de la altura interior habitual, para leerse como "el examen, se ve
            // importante" frente a las cubetas de trabajo normales.
            float altoPedestal = (PlintoAltoInterior + MuroGrosor) * 1.6f * celda;
            Vector3 centroPedestal = new Vector3(_centro.x, _y0 * celda - celda * 0.5f + altoPedestal * 0.5f, 0f);

            var pedestalGo = new GameObject("EnsayoPedestal");
            pedestalGo.transform.SetParent(transform, false);
            pedestalGo.transform.position = centroPedestal;

            // Affordance glow del plinto (§1.5): halo MÁS GRANDE, detrás (orden 14).
            _afordancePlinto = MaquinariaSprites.CrearCapa(pedestalGo.transform, "AfordancePlinto", MaquinariaSprites.Embudo(span), 14,
                ancho * 1.4f, altoPedestal * 1.5f);
            _afordancePlinto.color = new Color(UiStyles.Exito.r, UiStyles.Exito.g, UiStyles.Exito.b, 0f);

            MaquinariaSprites.CrearCapa(pedestalGo.transform, "Chasis", MaquinariaSprites.ChasisPlaca(span), 18, ancho, altoPedestal);
            // Marco de latón (§1.3, "cubeta enmarcada").
            MaquinariaSprites.CrearCapa(pedestalGo.transform, "MarcoLaton", MaquinariaSprites.MarcoContenedor(span), 19, ancho, altoPedestal);

            // Brasero incorporado (§1.4, decorativo): en miniatura, a la base del
            // pedestal -- eco visual del Crisol, "el examen usa calor de verdad".
            var brasilloGo = new GameObject("BraseroIncorporado");
            brasilloGo.transform.SetParent(pedestalGo.transform, false);
            brasilloGo.transform.localPosition = new Vector3(0f, -altoPedestal * 0.5f + celda * 0.8f, -0.01f);
            MaquinariaSprites.CrearCapa(brasilloGo.transform, "Rescoldo", MaquinariaSprites.ResistenciasPlaca(2), 19,
                ancho * 0.6f, celda * 1.4f);

            // Embudo (§1.1): montado arriba de la cámara de trabajo.
            var embudoGo = new GameObject("Embudo");
            embudoGo.transform.SetParent(pedestalGo.transform, false);
            embudoGo.transform.localPosition = new Vector3(0f, altoPedestal * 0.5f + celda * 1.2f, 0f);
            MaquinariaSprites.CrearCapa(embudoGo.transform, "Sprite", MaquinariaSprites.Embudo(span), 21, ancho * 0.75f, celda * 2.4f);
        }

        private bool EstaEnfocada() => MachineFocus.EsFoco(this, _player);

        private void Update()
        {
            if (_sim == null || _sim.Grid == null || _orders == null) return;
            if (DayCycle.InputLocked) return;

            if (_knowledge == null) _knowledge = FindAnyObjectByType<SubstanceKnowledge>(); // reintento perezoso, ver Init.

            // Contrato §1.5: sondeo cada ~0.25s (acumulador propio de AffordanceGlow),
            // corre SIEMPRE (también mientras Calentando -- el jugador puede seguir
            // acercándose con otra muestra para el próximo pedido).
            _glowPlinto.Sondear(Time.deltaTime, _centro, _player, _flask, _sirvePlinto);
            if (_afordancePlinto != null) _afordancePlinto.color = new Color(UiStyles.Exito.r, UiStyles.Exito.g, UiStyles.Exito.b, _glowPlinto.Alfa);

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
                return; // mientras se calienta, E no dispara un ensayo nuevo encima.
            }

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && EstaEnfocada())
            {
                TryEnsayo();
            }
        }

        /// <summary>Empuja la temperatura de la cubeta hacia Universe.TempEnsayoCalorRaw, tick a tick -- mismo patrón que HeatPlate.ApplyHeatTick (LIMITACIÓN compartida: escribe _sim.Grid.temp[] directamente, TODO(ChaosAlchemy) canalizar por una API dedicada).</summary>
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
                Rotular("vierte una muestra en la cubeta antes de pulsar E", UiStyles.Aviso);
                return;
            }

            if (objetivo.Tipo == OrderType.Conduce)
            {
                EvaluarConduce(matId);
                return;
            }

            // AguantaCalor: arranca el teatro físico (calienta A LA VISTA).
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

        /// <summary>
        /// Buffer de conteo por MaterialId, REUTILIZADO entre llamadas
        /// (nunca `new` dentro de MuestraDominante/DescribirMuerte): ninguno
        /// de los dos corre en el hot path por-frame de verdad (solo al
        /// pulsar E o al cerrar un calentamiento de ~5s), pero "cero allocs
        /// en los sondeos" es la verificación final del contrato y un
        /// array reusado cuesta lo mismo que uno local y no genera basura.
        /// </summary>
        private readonly int[] _conteoBuf = new int[MaterialId.Count];

        /// <summary>Material más frecuente (no vacío, no muro de piedra) dentro del interior de la cubeta, y cuántas celdas tiene.</summary>
        private bool MuestraDominante(out byte matId, out int count)
        {
            return ConteoDominante(out matId, out count);
        }

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

        /// <summary>Cuenta cuántas celdas del interior siguen siendo EXACTAMENTE `matId` -- usado para medir supervivientes tras el calentamiento.</summary>
        private int ContarMaterial(byte matId)
        {
            int n = 0;
            var grid = _sim.Grid;
            for (int x = _x0; x <= _x1; x++)
            {
                for (int y = _y0; y <= _y1; y++)
                {
                    if (grid.mat[CellGrid.Idx(x, y)] == matId) n++;
                }
            }
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
                // Fallo: el pedido NO se consume (nunca se llama a CompletarEnsayo).
                Rotular("ni un parpadeo -- no conduce nada", UiStyles.Peligro);
                _knowledge?.RegistrarObservacionPropiedad(matId, "no conduce: la lámpara ni parpadeó en el Ensayo");
            }
        }

        private void EvaluarAguantaCalor()
        {
            _fase = Fase.Ocioso;

            int survivientes = ContarMaterial(_calentandoDominante);
            float fraccion = _calentandoN0 > 0 ? survivientes / (float)_calentandoN0 : 0f;

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
                // Fallo: el pedido NO se consume. El rótulo dice CÓMO murió.
                string motivo = DescribirMuerte();
                Rotular(motivo, UiStyles.Peligro);
                _knowledge?.RegistrarObservacionPropiedad(_calentandoDominante, "no aguantó el Ensayo: " + motivo);
            }
        }

        /// <summary>(contrato §6.2: "el rótulo dice CÓMO murió la muestra") Sondea qué quedó donde estaba la muestra tras el calor y describe el destino por ARQUETIPO -- nunca revela el nombre interno de nada innominado (regla 13/17 de CLAUDE.md).</summary>
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
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return; // (playtest 21) HudSilenciado, hermano de InputLocked.

            UiStyles.Preparar();

            if (_fase == Fase.Calentando)
            {
                int segundos = Mathf.CeilToInt(Mathf.Max(0f, _calentandoHasta - Time.time));
                UiStyles.EtiquetaMundo(_centro, "calentando... (" + segundos + "s)", UiStyles.Aviso, UiStyles.S(30f));
                return;
            }

            if (Time.time < _rotuloHasta && _rotulo != null)
            {
                UiStyles.EtiquetaMundo(_centro, _rotulo, _rotuloColor, UiStyles.S(30f));
                return;
            }

            if (MachineFocus.MostrarPromptE && EstaEnfocada() && !UiStyles.RatonOcupado)
            {
                UiStyles.EtiquetaMundo(_centro, "E — someter la muestra al Ensayo del Maestro", UiStyles.Oro, UiStyles.S(30f));
            }
        }
    }
}
