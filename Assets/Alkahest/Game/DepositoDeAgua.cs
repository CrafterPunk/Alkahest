using System.Collections.Generic;
using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// (RONDA 73, el prólogo rehecho) EL DEPÓSITO DE AGUA — la primera
    /// recompensa del juego: al completar agua + lodo, "el mundo responde" y
    /// este tanque de vidrio y cobre EMERGE del suelo delante del jugador.
    /// Construido sobre la foto de referencia de Cesar (cilindro de vidrio,
    /// armazón de cobre con pátina, tapa en domo). Sustituye conceptualmente
    /// al grifo antiguo como fuente básica de agua.
    ///
    /// QUÉ ES POR DENTRO: agua REAL de la sim contenida entre dos columnas de
    /// piedra (sándwich 2.5D completo: TanqueFondo detrás de la sim, el agua
    /// en medio, TanqueMarco con su ventana translúcida delante). El jugador
    /// ASPIRA directamente de él apuntando al vidrio — no hay menú, no hay E:
    /// el verbo del juego funciona aquí igual que en la poza.
    ///
    /// (RONDA 74, corrección de Cesar) SIN AUTOFILL AL NACER: el depósito
    /// llega con solo un culo de agua y la tarea final del prólogo es
    /// LLENARLO — el verbo, una vez más, ahora al revés (verter DENTRO de
    /// algo tuyo). (R85/R86) El día del autofill LLEGÓ, pero solo para el
    /// RENACER del REORDEN (Init/InitSilo con `conTubo:true`): el armazón
    /// trae el TUBO GRUESO integrado de la referencia de Cesar
    /// (docs/ref/deposito_agua_ref2_tuberia_autofill.png) que toca el suelo,
    /// + goteo lento por ActivarRefill. Antes del reorden, llenar es TU tarea.
    ///
    /// Regla 36: Init/Aparecer NO son idempotentes. (R93, mandato de Cesar:
    /// "que él haga la MUDANZA de los dos cosos a esa plataforma") El "no es
    /// IMovible" histórico QUEDA DEROGADO a propósito para el mundo ordenado:
    /// tras el renacer del REORDEN, <see cref="HabilitarMudanza"/> registra el
    /// recipiente en el modo Mudanza (V) y <see cref="Reposicionar(int,int)"/>
    /// lo muda ENTERO — muros, fondo, contenido, obra, visual y tubo — a otra
    /// ancla. Antes del ORDEN sigue clavado donde el plano lo decreta.
    /// </summary>
    public sealed class DepositoDeAgua : MonoBehaviour, IMovible, IMovibleAnclaEsquina, IMovibleSilueta, IMovibleEspejable
    {
        // ---- Los números del esqueleto ----
        // (R75) Duración de la emergencia y carga inicial viven en el GUION
        // (GuionDelPrologo.asset, editable en Inspector); aquí solo cadencias
        // internas menores.
        private const float PolvoCadaSeg = 0.22f;     // ritmo del polvo que suelta al emerger.
        private const float CargaInicialSeg = 0.09f;  // cadencia de la carga (cae dentro del vidrio, se ve acomodarse).
        private GuionDelPrologo _g;

        private AlkahestSim _sim;
        // (R88, Cesar: "no necesitan piso ni de madera ni de tierra ni de
        // nada — son de algún metal y se entiende que ese es su piso") EL
        // FONDO VIVE DENTRO: la fila y0 es el fondo del propio recipiente
        // (piedra, tapada por el zócalo de cobre del sprite — 2 celdas de
        // banda), el interior útil es y0+1..y1, y el LECHO DEL MUNDO NO SE
        // TOCA JAMÁS (se acabó el piso pintado bajo la huella y con él la
        // memoria _sueloPrevio de la R85: no hay nada que restaurar).
        private int _x0, _x1, _y0, _y1;               // muros en x0/x1 (y0..y1+1); fondo en y0; interior x0+1..x1-1, y0+1..y1.
        private Transform _cuerpo;                    // todas las capas visuales cuelgan de aquí (para el ascenso).
        private float _alturaMundo;

        // (R84, FASE B1 — plan cap2 / Opus A8) Drenando/Hundiendo/Enterrado:
        // la retirada cinematográfica del REORDEN. Orden obligatorio: drenar
        // el contenido celda a celda (el mundo lo TOMA, no lo derrama) →
        // borrar los muros de la sim → degenerar el rect de obra (nada de
        // obra fantasma anticincel — el fantasma R69) → hundir el visual →
        // Enterrado (el director destruye el GO y recrea el recipiente).
        private enum Fase { Oculto, Emergiendo, CargaInicial, Listo, Drenando, Hundiendo, Enterrado }
        private Fase _fase = Fase.Oculto;
        private float _tFase;
        private float _polvoTimer, _rellenoTimer;
        private int _polvoIdx;

        private static readonly byte Lodo = MaterialId.MatDe(1, EstadoMateria.Polvo); // los terrones que suelta al emerger.
        private static readonly byte Barbotina = MaterialId.MatDe(1, EstadoMateria.Solucion); // el lodo MOJADO cuenta como lodo (lección del cuenco, revisión Opus 73 #2).

        // (R83, FASE A del capítulo 2) EL RECIPIENTE SE PARAMETRIZA: la misma
        // clase sirve al TANQUE de agua (8x13, prefab de piel, carga inicial
        // del guion) y al SILO de lodo (6x9, boca para lo que se AMONTONA,
        // nace VACÍO: el mundo no regala lo que te pide — plan cap2 / Opus
        // A1+C1). El material dueño deja de estar horneado en los conteos.
        private byte _matDueno = MaterialId.Water;
        private byte _matDuenoAlt = MaterialId.Empty; // forma alternativa que cuenta como dueño (barbotina para el silo).
        private int _cargaInicial = -1;               // -1 = la del guion (tanque de agua); 0 = nace vacío (silo).
        private bool _esSilo;

        // (R86, veto de Cesar a la estantería R85: "no quiero uno apilado
        // del otro… el Sprite completo con un tubo grueso, del mismo
        // material") EL TANQUE CON TUBO INTEGRADO: en el renacer del REORDEN
        // el recipiente vuelve A SU SITIO, pero su armazón ya trae el TUBO
        // GRUESO lateral de la referencia de Cesar (columna de cobre con
        // tapón roscado, pegada al flanco derecho, que TOCA EL SUELO — su
        // única queja de la referencia era que el tubo quedaba unos píxeles
        // arriba). El tubo dice "refill infinito" sin una palabra; el goteo
        // lo cumple. La estantería/bahías apiladas de la R85 quedan
        // RETIRADAS (regla 15): la lógica era sana (lodo arriba bebiendo de
        // la gotera) pero el guion nuevo APAGA las fuentes en el ORDEN — ya
        // no hay gotera que atrapar, y el mueble no pagaba su silueta.
        private bool _conTubo;
        private bool _refillActivo;
        private float _refillTimer;
        // (R88, dirección Opus) La INSTALACIÓN del tubo como acto: la
        // columna de cobre empuja desde el subsuelo (easing con overshoot) y
        // encaja junto al hombro — el director pone el CLANK (uno solo para
        // los dos tubos) y la primera gota. -1 = sin animación en curso.
        private GameObject _tuboGo;
        private float _tuboAnimT = -1f;
        private float _tuboAltoMundo;
        private bool _tuboInstalado;
        private bool _drenRapido;      // (R88) la anulación por decreto: el drenado entero en ~0.5 s, no celda a celda.
        private bool _cargaInstantanea; // (R88) el renacer sube YA LLENO: la carga se pinta entera al asentarse (nada de progress bar).

        /// <summary>(R88) El tubo terminó de encajar (listo para el clank + la primera gota del director).</summary>
        public bool TuboInstalado => _tuboInstalado;

        /// <summary>Ya asentado (muros en la sim, carga inicial en marcha o hecha).</summary>
        public bool Asentado => _fase == Fase.CargaInicial || _fase == Fase.Listo;
        /// <summary>La carga inicial terminó: el tanque espera a que el jugador lo LLENE (no se repone solo — R74).</summary>
        public bool CargaLista => _fase == Fase.Listo;
        /// <summary>(R84) La retirada terminó: muros fuera de la sim, obra degenerada, visual bajo tierra. El director puede destruir y recrear.</summary>
        public bool Enterrado => _fase == Fase.Enterrado;
        /// <summary>(R84) Celdas del DUEÑO que el drenado de la retirada recogió — el banco con el que renace el recipiente (Opus C2: recoger, no borrar).</summary>
        public int DrenadoDelDueno { get; private set; }

        // (R83, Opus A4) El rect real, para que el director EXCLUYA el silo
        // del conteo de montículo del cráter: el lodo atesorado no pausa la
        // gotera.
        public int X0 => _x0; public int X1 => _x1; public int Y0 => _y0; public int Y1 => _y1;

        /// <summary>Celdas del MATERIAL DUEÑO (más su forma alterna — barbotina en el silo) dentro del vidrio.</summary>
        public int DelDueno()
        {
            if (_sim == null || _sim.Grid == null || _fase == Fase.Oculto || _fase == Fase.Emergiendo) return 0;
            int n = 0;
            var grid = _sim.Grid;
            for (int x = _x0 + 1; x < _x1; x++)
                for (int y = _y0 + 1; y <= _y1; y++)
                {
                    byte m = grid.GetMat(x, y);
                    if (m == _matDueno || (m != MaterialId.Empty && m == _matDuenoAlt)) n++;
                }
            return n;
        }

        /// <summary>Alias histórico (el tanque de agua nació contando solo agua). Consumidores viejos intactos.</summary>
        public int AguaDentro() => DelDueno();

        // =================================================================
        // (R93) LA MUDANZA DE LOS RESERVORIOS — contrato IMovible, explícito
        // para no chocar con el método histórico CentroMundo() de la placa.
        // Solo existe para el mundo ordenado (post-renacer): HabilitarMudanza
        // lo registra; antes del ORDEN el recipiente es del plano, no tuyo.
        // =================================================================
        private bool _movible;
        // (R93) Todos los recipientes vivos: la guardia anti-solape de
        // CabeEnAncla los consulta (dos tanques no pueden fundir muros).
        private static readonly List<DepositoDeAgua> _todos = new List<DepositoDeAgua>(4);

        /// <summary>(R93) Entra al registro del modo Mudanza (V). Idempotente; solo con el recipiente Listo.</summary>
        public void HabilitarMudanza()
        {
            if (_movible || _fase != Fase.Listo) return;
            _movible = true;
            Mudanza.RegistrarMovible(this);
        }

        private void Awake() { if (!_todos.Contains(this)) _todos.Add(this); }

        private void OnDestroy()
        {
            _todos.Remove(this);
            if (_movible) Mudanza.OlvidarMovible(this);
        }

        // (R94, Cesar: "los contornos de la mudanza no calzan en el tamaño
        // de los sprites") EL CONTRATO SE MIDE EN VISUAL: TamanoMundo es el
        // rect del SPRITE (huella + 1 celda de vuelo por lado, alto + 6 con
        // banda y domo — los mismos spanVisual/altoVisual de InitInterno) y
        // AnclaCelda su esquina (x0-1, y0), como exige IMovibleAnclaEsquina.
        // Las conversiones de vuelta (+1 en x) viven en los wrappers.
        Vector3 IMovible.CentroMundo
        {
            get
            {
                float c = SimRenderer.CellWorldSize;
                return new Vector3((_x0 + _x1 + 1) * 0.5f * c, (_y0 + (_y1 - _y0 + 7) * 0.5f) * c, 0f);
            }
        }

        Vector2 IMovible.TamanoMundo
        {
            get
            {
                float c = SimRenderer.CellWorldSize;
                return new Vector2((_x1 - _x0 + 3) * c, (_y1 - _y0 + 7) * c); // spanVisual x altoVisual: el rect que se VE.
            }
        }

        Vector2Int IMovible.AnclaCelda => new Vector2Int(_x0 - 1, _y0); // esquina inferior izquierda del rect VISUAL.

        bool IMovible.CabeEnAncla(Vector2Int aVisual)
        {
            var a = new Vector2Int(aVisual.x + 1, aVisual.y); // visual → huella.
            int w = _x1 - _x0, alto = _y1 - _y0 + 1; // spans (el remate ocupa y+alto).
            if (a.x < 1 || a.x + w > CellGrid.W - 2 || a.y < 1 || a.y + alto > CellGrid.H - 2) return false;
            // (R93) ANTI-SOLAPE: dos recipientes no pueden compartir NI UNA
            // celda — los muros se fundirían y la mudanza del uno se tragaría
            // el interior del otro. Muro contra muro (huellas adyacentes sin
            // compartir columna) SÍ cabe: "uno al lado del otro" es legal.
            foreach (var otro in _todos)
            {
                if (otro == null || otro == this || otro._fase == Fase.Oculto || otro._fase == Fase.Enterrado) continue;
                if (a.x <= otro._x1 && a.x + w >= otro._x0 && a.y <= otro._y1 + 1 && a.y + alto >= otro._y0) return false;
            }
            return true;
        }

        void IMovible.Reposicionar(Vector2Int anclaVisual) => Reposicionar(anclaVisual.x + 1, anclaVisual.y); // visual → huella.

        // (R99, Cesar: "no lo cubre con su tubo de refill") LA SILUETA REAL:
        // el rect visual del cuerpo + el saliente del tubo grueso — una L,
        // solo cuando el tubo está INSTALADO y del flanco donde vive HOY
        // (el signo de su localPosition.x es la verdad). Números medidos de
        // InstalarTubo/Reposicionar: sprite de 3c centrado a ±(_x1+2.5 −
        // centro) ⇒ sobresale 2 celdas del borde visual; su tapón remata en
        // _y1+4 (altoCeldas = span+4 desde _y0), 3 celdas por debajo del
        // domo (_y1+7). Sentido HORARIO, Y arriba — el contrato exacto de
        // InflarPoligono. (R100) La versión RELATIVA es la fuente única: el
        // perímetro absoluto = relativa(flanco de hoy) + ancla visual.
        bool IMovibleSilueta.PerimetroVisual(List<Vector2> puntos)
        {
            if (!((IMovibleSilueta)this).SiluetaRelativa(EspejadoHoy, puntos)) return false;
            float c = SimRenderer.CellWorldSize;
            var ancla = new Vector2((_x0 - 1) * c, _y0 * c);
            for (int i = 0; i < puntos.Count; i++) puntos[i] += ancla;
            return true;
        }

        // (R100) La silueta RELATIVA al ancla visual (0,0 = esquina inferior
        // izquierda del rect que mide TamanoMundo), con el espejo PEDIDO —
        // la consume la sombra de arrastre de Mudanza, que enseña lo que va
        // a pasar al soltar (L voltea esta forma antes de que exista).
        bool IMovibleSilueta.SiluetaRelativa(bool espejado, List<Vector2> puntos)
        {
            if (_fase != Fase.Listo) return false;
            float c = SimRenderer.CellWorldSize;
            float span = (_x1 - _x0 + 3) * c;      // ancho visual.
            float domo = (_y1 - _y0 + 7) * c;      // alto visual.
            bool conSaliente = _tuboInstalado && _tuboGo != null;
            if (!conSaliente)
            {
                puntos.Add(new Vector2(0f, 0f));
                puntos.Add(new Vector2(0f, domo));
                puntos.Add(new Vector2(span, domo));
                puntos.Add(new Vector2(span, 0f));
                return true;
            }
            float tapon = (_y1 - _y0 + 4) * c;
            float saliente = 2f * c;
            if (!espejado) // tubo al flanco derecho.
            {
                puntos.Add(new Vector2(0f, 0f));
                puntos.Add(new Vector2(0f, domo));
                puntos.Add(new Vector2(span, domo));
                puntos.Add(new Vector2(span, tapon));
                puntos.Add(new Vector2(span + saliente, tapon));
                puntos.Add(new Vector2(span + saliente, 0f));
            }
            else // tubo al flanco izquierdo.
            {
                puntos.Add(new Vector2(-saliente, 0f));
                puntos.Add(new Vector2(-saliente, tapon));
                puntos.Add(new Vector2(0f, tapon));
                puntos.Add(new Vector2(0f, domo));
                puntos.Add(new Vector2(span, domo));
                puntos.Add(new Vector2(span, 0f));
            }
            return true;
        }

        // =================================================================
        // (R100, Cesar: "presioné la L para espejar pero no vi nada") EL
        // ESPEJO DEL TUBO: el jugador decide el flanco; el aire manda. La
        // preferencia PERSISTE (colocas espejado y espejado se queda hasta
        // que digas lo contrario) y el fallback de la R93 (flanco sepultado
        // → el otro) sigue vivo dentro de AplicarFlancoTubo.
        // =================================================================
        private bool _tuboPreferirIzquierda; // el deseo vigente; falso = derecha (el flanco de nacimiento, R86).

        public bool EspejadoHoy => _tuboGo != null && _tuboGo.transform.localPosition.x < 0f;

        public bool EspejoPendiente
        {
            get => _tuboPreferirIzquierda;
            set => _tuboPreferirIzquierda = value;
        }

        public bool AplicarEspejoAhora()
        {
            AplicarFlancoTubo();
            return EspejadoHoy == _tuboPreferirIzquierda;
        }

        /// <summary>
        /// (R100, extraído del bloque R93 de Reposicionar) El tubo elige
        /// flanco: honra la preferencia del jugador SI ese flanco tiene aire
        /// (muestra a la altura del hombro, _y0+3); si está tapado, cruza al
        /// otro con aire; si ninguno respira, se queda en el preferido — la
        /// marca del refill nunca desaparece.
        /// </summary>
        private void AplicarFlancoTubo()
        {
            if (_tuboGo == null || !_tuboInstalado || _sim == null || _sim.Grid == null) return;
            bool derLibre = _sim.Grid.GetMat(_x1 + 2, _y0 + 3) == MaterialId.Empty;
            bool izqLibre = _sim.Grid.GetMat(_x0 - 2, _y0 + 3) == MaterialId.Empty;
            bool izquierda = _tuboPreferirIzquierda ? (izqLibre || !derLibre) : (!derLibre && izqLibre);
            float cM = SimRenderer.CellWorldSize;
            float offX = ((_x1 + 2.5f) - (_x0 + _x1 + 1) * 0.5f) * cM;
            _tuboGo.transform.localPosition = new Vector3(izquierda ? -offX : offX, _tuboFinalY, 0f);
            var esc = _tuboGo.transform.localScale;
            esc.x = Mathf.Abs(esc.x) * (izquierda ? -1f : 1f);
            _tuboGo.transform.localScale = esc;
        }

        /// <summary>
        /// (R93) LA MUDANZA ENTERA: captura el contenido del vidrio (en orden
        /// de asentado, de abajo arriba), desmonta muros y fondo del sitio
        /// viejo, replanta la estructura en la ancla nueva, re-registra la
        /// obra en el MISMO handle (nada de obra fantasma), recoloca el
        /// contenido compactado desde el fondo y muda el visual entero (el
        /// tubo es hijo del raíz: viaja gratis). Si el jugador está parado en
        /// el destino, la piedra nace igual y "sale nadando" (doctrina
        /// ApprenticeController, pariente de la regla 38 — el mismo trato que
        /// el asentado del renacer tras su tope de 6 s).
        /// </summary>
        public void Reposicionar(int nx0, int ny0)
        {
            if (_fase != Fase.Listo || _sim == null || _sim.Grid == null) return;
            if (nx0 == _x0 && ny0 == _y0) { AplicarFlancoTubo(); return; } // (R100) soltar en el mismo sitio TAMBIÉN honra el espejo (agarrar, L, soltar donde estaba).

            var grid = _sim.Grid;
            var contenido = new List<byte>();
            for (int y = _y0 + 1; y <= _y1; y++)
                for (int x = _x0 + 1; x < _x1; x++)
                {
                    byte m = grid.GetMat(x, y);
                    if (m == MaterialId.Empty) continue;
                    contenido.Add(m);
                    _sim.Paint(x, y, 0, MaterialId.Empty);
                }

            for (int y = _y0; y <= _y1 + 1; y++)
            {
                _sim.Paint(_x0, y, 0, MaterialId.Empty);
                _sim.Paint(_x1, y, 0, MaterialId.Empty);
            }
            for (int x = _x0 + 1; x < _x1; x++)
                _sim.Paint(x, _y0, 0, MaterialId.Empty);

            int wSpan = _x1 - _x0, altoSpan = _y1 - _y0;
            _x0 = nx0; _x1 = nx0 + wSpan;
            _y0 = ny0; _y1 = ny0 + altoSpan;

            for (int y = _y0; y <= _y1 + 1; y++)
            {
                _sim.PaintStable(_x0, y, 0, MaterialId.Stone);
                _sim.PaintStable(_x1, y, 0, MaterialId.Stone);
            }
            for (int x = _x0 + 1; x < _x1; x++)
                _sim.PaintStable(x, _y0, 0, MaterialId.Stone);
            if (_obraHandle >= 0) SimLevelBuilder.ActualizarObra(_obraHandle, _x0, _y0, _x1, _y1 + 1);
            else _obraHandle = SimLevelBuilder.RegistrarObra(_x0, _y0, _x1, _y1 + 1);

            int i = 0;
            for (int y = _y0 + 1; y <= _y1 && i < contenido.Count; y++)
                for (int x = _x0 + 1; x < _x1 && i < contenido.Count; x++)
                    _sim.PaintStable(x, y, 0, contenido[i++]);

            float cM = SimRenderer.CellWorldSize;
            transform.position = new Vector3((_x0 + _x1 + 1) * 0.5f * cM, _y0 * cM, 0f);

            // (R93, cazado en la repasada; R100, extraído + preferencia) EL
            // TUBO ELIGE FLANCO: la preferencia del jugador (L) primero, el
            // aire manda — ver AplicarFlancoTubo.
            AplicarFlancoTubo();
        }

        /// <summary>Celdas NO vacías dentro del vidrio (dueño + estorbo). Con Capacidad(), da la placa honesta del guion (Opus A5).</summary>
        public int Ocupado()
        {
            if (_sim == null || _sim.Grid == null || _fase == Fase.Oculto || _fase == Fase.Emergiendo) return 0;
            int n = 0;
            var grid = _sim.Grid;
            for (int x = _x0 + 1; x < _x1; x++)
                for (int y = _y0 + 1; y <= _y1; y++)
                    if (grid.GetMat(x, y) != MaterialId.Empty) n++;
            return n;
        }

        /// <summary>Celdas del interior del vidrio (el fondo interno de y0 no cuenta: 6x12 = 72).</summary>
        public int Capacidad() => (_x1 - _x0 - 1) * (_y1 - _y0);

        /// <summary>Centro del recipiente en el mundo (ancla de la placa del director) — general para cualquier alto.</summary>
        public Vector3 CentroMundo()
        {
            float c = SimRenderer.CellWorldSize;
            return new Vector3((_x0 + _x1 + 1) * 0.5f * c, (_y0 + (_y1 - _y0 + 1) * 0.5f + 2f) * c, 0f);
        }

        /// <summary>El tanque de agua clásico (marcador `deposito`, huella 8x13, piel de prefab, carga inicial del guion).</summary>
        public void Init(AlkahestSim sim) => Init(sim, -1);

        /// <summary>(R84) Variante con carga inicial EXPLÍCITA: el renacer del REORDEN arranca con lo que el drenado + el barrido recogieron ("lo que sale primero es lo que tú derramaste"). (R86) `conTubo`: renace con el tubo grueso integrado (referencia de Cesar) — la marca visual del refill infinito.</summary>
        public void Init(AlkahestSim sim, int cargaInicial, bool conTubo = false, bool cargaInstantanea = false)
        {
            var escena = PrologoEscenografia.Buscar();
            InitInterno(sim, escena, escena != null ? escena.deposito : null,
                MaterialId.Water, MaterialId.Empty, cargaInicial: cargaInicial, esSilo: false,
                anchoHuella: 8, altoHuella: 13,
                fallbackX0: SimLevelBuilder.FundacionDepositoX0, fallbackY0: SimLevelBuilder.FundacionDepositoY0,
                conTubo: conTubo, cargaInstantanea: cargaInstantanea);
        }

        /// <summary>
        /// (R83; retallado R84 a 8x13 por Cesar) EL SILO DEL LODO: gemelo en
        /// tamaño del tanque de agua, dueño lodo+barbotina, SIN carga inicial
        /// y SIN piel de prefab (la piel horneada es la del tanque; el silo
        /// viste el fallback procedural hasta su sprite propio).
        /// </summary>
        public void InitSilo(AlkahestSim sim) => InitSilo(sim, 0);

        /// <summary>(R84) Variante con carga explícita para el renacer del REORDEN. (R86) `conTubo`: con el tubo grueso integrado.</summary>
        public void InitSilo(AlkahestSim sim, int cargaInicial, bool conTubo = false, bool cargaInstantanea = false, int xRenacer = -1)
        {
            var escena = PrologoEscenografia.Buscar();
            // (R84, Cesar) MISMO TAMAÑO que el tanque (8x13): la distinción
            // vendrá por carteles/decoración, no por silueta. El interior
            // (x386-391) sigue cabiendo EXACTO en el aire medido poza|cráter;
            // los muros pisan los labios (ver FundacionSilo* en el plano).
            // (R101) `xRenacer`: el ORDEN corre el silo a la izquierda (Cesar:
            // "que no se cruce con los contornos del lugar") — con override,
            // la autoridad del marcador de escenografía CEDE: el corrimiento
            // es una decisión del guion del renacer, no del plano del acto 1.
            InitInterno(sim, escena, xRenacer >= 0 ? null : (escena != null ? escena.deposito2 : null),
                Lodo, Barbotina, cargaInicial: cargaInicial, esSilo: true,
                anchoHuella: 8, altoHuella: 13,
                fallbackX0: xRenacer >= 0 ? xRenacer : SimLevelBuilder.FundacionSiloX0, fallbackY0: SimLevelBuilder.FundacionSiloY0,
                conTubo: conTubo, cargaInstantanea: cargaInstantanea);
        }

        // (R86, regla 15) InitFinal/InitSiloFinal — el renacer REUBICADO en
        // las bahías apiladas de la estantería (R85) — se RETIRARON: Cesar
        // vetó el apilado ("prefiero los dos reservorios, cada uno en su
        // sitio, con su tubo grueso") y el guion nuevo apaga las fuentes en
        // el ORDEN, así que la premisa del mueble (atrapar la gotera) murió
        // con ellas. Los marcadores depositoFinal/deposito2Final se fueron
        // con esto.

        private void InitInterno(AlkahestSim sim, PrologoEscenografia escena, Transform marcador,
            byte matDueno, byte matDuenoAlt, int cargaInicial, bool esSilo,
            int anchoHuella, int altoHuella, int fallbackX0, int fallbackY0,
            bool conTubo = false, bool cargaInstantanea = false)
        {
            _sim = sim;
            _matDueno = matDueno;
            _matDuenoAlt = matDuenoAlt;
            _cargaInicial = cargaInicial;
            _esSilo = esSilo;
            _conTubo = conTubo;
            _cargaInstantanea = cargaInstantanea;

            // (RONDA 75) AUTORIDAD DE POSICIÓN: el marcador de la escenografía
            // (base-centro), ajustado a celdas. Sin marcador: el sitio del
            // plano. La HUELLA es fija por tipo: la piel puede moverse, el
            // vidrio no cambia de capacidad desde la escena.
            _g = PrologoEscenografia.GuionEfectivo(escena);
            if (marcador != null)
            {
                float celdaM = SimRenderer.CellWorldSize;
                int cx0 = Mathf.RoundToInt(marcador.position.x / celdaM);
                int cy0 = Mathf.RoundToInt(marcador.position.y / celdaM);
                _x0 = cx0 - anchoHuella / 2; _x1 = _x0 + anchoHuella - 1;
                _y0 = cy0; _y1 = cy0 + altoHuella - 1;
            }
            else
            {
                _x0 = fallbackX0;
                _x1 = fallbackX0 + anchoHuella - 1;
                _y0 = fallbackY0;
                _y1 = fallbackY0 + altoHuella - 1;
            }

            float c = SimRenderer.CellWorldSize;
            int spanVisual = (_x1 - _x0 + 1) + 2;      // 1 celda de vuelo a cada lado.
            int altoVisual = (_y1 - _y0 + 1) + 6;      // banda alta + domo por encima de los muros.
            _alturaMundo = altoVisual * c;

            _cuerpo = new GameObject("DepositoCuerpo").transform;
            _cuerpo.SetParent(transform, false);
            float cx = (_x0 + _x1 + 1) * 0.5f * c;
            float baseY = _y0 * c; // borde inferior del tanque en el mundo.

            // (R74) EL TUBO TRASERO SE RETIRÓ de esta versión: insinuaba un
            // rellenado que ya no existe. Vuelve como tubería lateral
            // completa junto con el autofill (ver docblock de clase).

            // (RONDA 75) LA PIEL: si la escenografía trae el prefab visual
            // (horneado por el menú 6, editable en el editor), ÉL viste al
            // tanque — convención de hijos: "Fondo" (detrás de la sim) y
            // "Marco" (el armazón con vidrio; el director le baja el orden
            // mientras emerge y se lo sube al asentarse). Sin prefab, las
            // capas procedurales de siempre (fallback reversible).
            // (R88) En el renacer conTubo se usa la piel procedural: el
            // prefab horneado no sabe del tubo que se instalará al lado
            // (cuando Cesar hornee su sprite v2, este guard aprende).
            if (!_esSilo && !_conTubo && escena != null && escena.depositoVisualPrefab != null)
            {
                var piel = Instantiate(escena.depositoVisualPrefab, _cuerpo);
                piel.name = "PielPrefab";
                piel.transform.localPosition = Vector3.zero;
                var marcoPrefab = piel.transform.Find("Marco");
                _marcoSr = marcoPrefab != null ? marcoPrefab.GetComponent<SpriteRenderer>() : null;
                if (_marcoSr != null) _marcoSr.sortingOrder = Capas.MaquinaFondoInterior + 2; // emerge OCULTO tras la roca (revisión Opus 73 #15).
                transform.position = new Vector3(cx, baseY, 0f);
                _cuerpo.localPosition = new Vector3(0f, -_alturaMundo, 0f);
                _cuerpo.gameObject.SetActive(false);
                return; // la piel procedural de abajo no se construye.
            }

            // La cavidad (detrás de la sim).
            var fondoGo = new GameObject("Fondo");
            fondoGo.transform.SetParent(_cuerpo, false);
            MaquinariaSprites.CrearCapa(fondoGo.transform, "Sprite",
                MaquinariaSprites.TanqueFondo(_x1 - _x0 - 1, _y1 - _y0 + 1),
                Capas.MaquinaFondoInterior, (_x1 - _x0 - 1) * c, (_y1 - _y0 + 1) * c);

            // El armazón. (revisión Opus 73 #15) Mientras EMERGE se dibuja
            // DETRÁS de la sim (-6): la roca del suelo lo oculta de cintura
            // para abajo y "sale de la tierra" de verdad, en vez de deslizarse
            // por delante de la piedra. Al asentarse pasa a MaquinaFrente (35)
            // para hacer de vidrio delante del agua.
            var marcoGo = new GameObject("Marco");
            marcoGo.transform.SetParent(_cuerpo, false);
            // (R88) El marco es SIEMPRE el clásico: el tubo grueso es una
            // PIEZA SUELTA que el Maestro instala tras el renacer
            // (InstalarTubo) — la teatralidad manda sobre el horneado.
            _marcoSr = MaquinariaSprites.CrearCapa(marcoGo.transform, "Sprite",
                MaquinariaSprites.TanqueMarco(spanVisual, altoVisual),
                Capas.MaquinaFondoInterior + 2, spanVisual * c, altoVisual * c);

            // Posiciones locales definitivas (relativas al centro del cuerpo):
            marcoGo.transform.localPosition = new Vector3(0f, altoVisual * 0.5f * c, 0f);
            fondoGo.transform.localPosition = new Vector3(0f, (_y1 - _y0 + 1) * 0.5f * c, 0f);

            // El cuerpo entero arranca ENTERRADO (invisible bajo el suelo);
            // Aparecer() lo hace emerger.
            transform.position = new Vector3(cx, baseY, 0f);
            _cuerpo.localPosition = new Vector3(0f, -_alturaMundo, 0f);
            _cuerpo.gameObject.SetActive(false);
        }

        /// <summary>Arranca la emergencia cinematográfica (la llama el director al completarse agua + lodo).</summary>
        public void Aparecer()
        {
            if (_fase != Fase.Oculto) return;
            _fase = Fase.Emergiendo;
            _tFase = 0f;
            _cuerpo.gameObject.SetActive(true);
        }

        /// <summary>
        /// (R85/R86) Enciende el goteo de reabastecimiento (cadencia y tope
        /// del guion): el tubo repone el material DUEÑO celda a celda por la
        /// columna del inlet (el flanco derecho, donde vive el tubo grueso)
        /// hasta `refillTope`, y de ahí en más solo cubre lo que el jugador
        /// se lleve. El primer refill infinito del juego — llega
        /// EXCLUSIVAMENTE con el renacer del REORDEN (antes, llenar los
        /// recipientes es TU tarea: R74).
        /// </summary>
        public void ActivarRefill()
        {
            _refillActivo = true;
            _refillTimer = _g.refillSeg;
        }

        private void TickRefill(float dt)
        {
            if (!_refillActivo) return;
            _refillTimer -= dt;
            if (_refillTimer > 0f) return;
            int n = DelDueno();
            if (n >= _g.refillTopeCeldas) { _refillTimer = _g.refillSeg; return; } // el vidrio ENTERO (R91).
            // (R90/R91, Cesar: "caer más lento mientras más se llena… que el
            // total tarde ~3 minutos, pero HASTA EL TOPE") Cadencia
            // cuadrática 0.8 → ~6.4 s (factor 7: la integral de 14 a 72 da
            // ~180 s — la cuenta de los 3 minutos, hecha, no estimada).
            float t = Mathf.Clamp01(n / (float)Mathf.Max(1, _g.refillTopeCeldas));
            _refillTimer = _g.refillSeg * (1f + 7f * t * t);
            // (R90, Cesar) La gota nace en el TOPE y POR EL CENTRO del vidrio
            // — cae con física a la vista, por en medio, no pegada a un
            // flanco. (R91) Cuando ya no hay caída (la fila del tope se
            // ocupó), las últimas celdas se completan EN SILENCIO: "ya les
            // quedó claro el refill en la primera parte" — el tope se cumple
            // aunque la última sección no se visualice.
            int inlet = (_x0 + _x1) / 2;
            if (_sim.Grid.GetMat(inlet, _y1) == MaterialId.Empty)
            {
                _sim.PaintStable(inlet, _y1, 0, _matDueno);
                return;
            }
            for (int y = _y1; y >= _y0 + 1; y--)
                for (int x = _x0 + 1; x < _x1; x++)
                    if (_sim.Grid.GetMat(x, y) == MaterialId.Empty)
                    {
                        _sim.PaintStable(x, y, 0, _matDueno); // la cola silenciosa: rincón a rincón hasta el vidrio lleno.
                        return;
                    }
        }

        /// <summary>
        /// (R88, el encargo textual de Cesar: "no vi nada que me indicara que
        /// el Maestro colocó los refill") LA INSTALACIÓN DEL TUBO COMO ACTO:
        /// la columna de cobre (pieza propia, TanqueTuboGrueso) EMPUJA desde
        /// el subsuelo junto al flanco derecho y encaja en el hombro del
        /// tanque con overshoot — el director orquesta el polvo previo, el
        /// CLANK único y la primera gota (dirección Opus R88, tres tiempos).
        /// </summary>
        public void InstalarTubo()
        {
            if (!_conTubo || _tuboGo != null) return;
            float c = SimRenderer.CellWorldSize;
            int altoCeldas = (_y1 - _y0 + 1) + 3;                 // del pie al tapón, rozando el hombro del domo.
            _tuboAltoMundo = altoCeldas * c;
            _tuboGo = new GameObject("Tubo");
            _tuboGo.transform.SetParent(transform, false);        // hijo del RAÍZ: no comparte el ascenso del cuerpo.
            MaquinariaSprites.CrearCapa(_tuboGo.transform, "Sprite",
                MaquinariaSprites.TanqueTuboGrueso(altoCeldas),
                Capas.MaquinaFondoInterior + 2, 3f * c, altoCeldas * c); // DETRÁS de la sim mientras sube: emerge tapado por la roca, como el tanque.
            // Posición final: pegado al flanco derecho, el pie EN el suelo del tanque.
            _tuboGo.transform.localPosition = new Vector3(((_x1 + 2.5f) - (_x0 + _x1 + 1) * 0.5f) * c, (altoCeldas * 0.5f) * c, 0f);
            _tuboFinalY = _tuboGo.transform.localPosition.y;
            _tuboGo.transform.localPosition += new Vector3(0f, -_tuboAltoMundo, 0f); // arranca ENTERRADO.
            _tuboAnimT = 0f;
        }
        private float _tuboFinalY;
        private float _esperaJugadorT; // (R89) cuánto lleva el asentado esperando a que el jugador se aparte.

        private void TickTubo(float dt)
        {
            if (_tuboAnimT < 0f || _tuboGo == null) return;
            _tuboAnimT += dt;
            float dur = Mathf.Max(0.2f, _g.tuboInstalarSeg);
            float t = Mathf.Clamp01(_tuboAnimT / dur);
            // easeOutBack acotado: empuja, se pasa media celda y ASIENTA.
            float c1 = 1.30f, c3 = c1 + 1f;
            float e = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            var lp = _tuboGo.transform.localPosition;
            lp.y = _tuboFinalY - _tuboAltoMundo * (1f - e);
            _tuboGo.transform.localPosition = lp;
            if (t >= 1f)
            {
                lp.y = _tuboFinalY;
                _tuboGo.transform.localPosition = lp;
                var sr = _tuboGo.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = Capas.MaquinaFrente - 5; // instalado: al frente, pieza nítida.
                _tuboAnimT = -1f;
                _tuboInstalado = true;
            }
        }

        /// <summary>
        /// (R84, el REORDEN) Arranca la RETIRADA: drena su contenido celda a
        /// celda (contándolo en <see cref="DrenadoDelDueno"/>), borra sus
        /// muros de la sim, degenera su rect de obra y se hunde. Al quedar
        /// <see cref="Enterrado"/>, el director destruye este GO y recrea el
        /// recipiente donde toque, cargado con lo recogido.
        /// </summary>
        public void Retirar()
        {
            if (_fase != Fase.Listo && _fase != Fase.CargaInicial) return;
            _fase = Fase.Drenando;
            _tFase = 0f;
            _rellenoTimer = 0.2f;
        }

        /// <summary>
        /// (R88, dirección Opus: "no es un vaciado — es una ANULACIÓN") La
        /// retirada por decreto: el contenido entero se apaga en ~0.5 s (el
        /// drenado celda a celda de 2.7 s era íntimo para llenar jugando y
        /// ridículo para una orden). Mismo camino Drenando→Hundiendo, otro
        /// ritmo.
        /// </summary>
        public void RetirarRapido()
        {
            if (_fase != Fase.Listo && _fase != Fase.CargaInicial) return;
            _fase = Fase.Drenando;
            _drenRapido = true;
            _tFase = 0f;
            _rellenoTimer = 0f;
        }

        private SpriteRenderer _marcoSr;
        private int _obraHandle = -1; // (R84, Opus A8) el handle de RegistrarObra: sin él, la retirada dejaría obra FANTASMA anticincel (el fantasma R69).

        private void Update()
        {
            if (_sim == null || _sim.Grid == null || _fase == Fase.Oculto || _fase == Fase.Enterrado) return;
            if (DayCycle.InputLocked) return; // (revisión Opus 73 #1) con la pausa delante, la emergencia y el llenado esperan (la sim está congelada).
            float dt = Time.deltaTime;
            _tFase += dt;

            TickTubo(dt); // (R88) la instalación del tubo corre por encima de las fases.
            switch (_fase)
            {
                case Fase.Emergiendo: TickEmerger(dt); break;
                case Fase.CargaInicial: TickCargaInicial(dt); break;
                case Fase.Listo: TickRefill(dt); break; // (R74) sin refill activo no se repone solo: llenarlo es TU tarea. (R85) Reubicado con ActivarRefill: el tubo gotea.
                case Fase.Drenando: TickDrenar(dt); break;
                case Fase.Hundiendo: TickHundir(); break;
            }
        }

        /// <summary>
        /// (R84) EL MUNDO LO TOMA: la celda MÁS ALTA del interior se apaga a
        /// cadencia visible (el mismo lenguaje del cuenco del Maestro), y lo
        /// que era del dueño se CUENTA — es el banco del renacer, no un
        /// borrado (Opus C2). Vacío el vidrio: muros fuera, obra degenerada,
        /// y a hundirse.
        /// </summary>
        private void TickDrenar(float dt)
        {
            _rellenoTimer -= dt;
            if (_rellenoTimer > 0f) return;
            _rellenoTimer = _drenRapido ? 0.016f : 0.035f;
            int porTick = _drenRapido ? 3 : 1; // (R88) por decreto: ~72 celdas en ~0.4 s.

            var grid = _sim.Grid;
            for (int y = _y1; y >= _y0 + 1; y--)
            {
                for (int x = _x0 + 1; x < _x1; x++)
                {
                    byte m = grid.GetMat(x, y);
                    if (m == MaterialId.Empty) continue;
                    if (m == _matDueno || (m != MaterialId.Empty && m == _matDuenoAlt)) DrenadoDelDueno++;
                    _sim.Paint(x, y, 0, MaterialId.Empty);
                    if (--porTick <= 0) return; // (normal) una celda por tick: se VE al mundo tomarlo.
                }
            }

            // Vidrio vacío: muros Y SUELO PROPIO salen de la sim y la obra
            // se degenera (rect imposible: EsObraDelTaller lo ignora).
            DesmontarEstructura();
            if (_marcoSr != null) _marcoSr.sortingOrder = Capas.MaquinaFondoInterior + 2; // vuelve DETRÁS de la sim: se hunde tras la roca, como emergió.
            _fase = Fase.Hundiendo;
            _tFase = 0f;
        }

        /// <summary>
        /// (R85/R88) Muros + fondo interno fuera de la sim + obra
        /// degenerada. Compartido por el drenado normal y la retirada de
        /// golpe. (R88) Ya no hay lecho que restaurar: el fondo vive DENTRO
        /// de la huella (y0) y el mundo bajo el recipiente jamás se tocó —
        /// la memoria _sueloPrevio de la R85 se retiró con el piso postizo.
        /// </summary>
        private void DesmontarEstructura()
        {
            for (int y = _y0; y <= _y1 + 1; y++)
            {
                _sim.Paint(_x0, y, 0, MaterialId.Empty);
                _sim.Paint(_x1, y, 0, MaterialId.Empty);
            }
            for (int x = _x0 + 1; x < _x1; x++)
                _sim.Paint(x, _y0, 0, MaterialId.Empty);
            if (_obraHandle >= 0) SimLevelBuilder.ActualizarObra(_obraHandle, 0, 0, -1, -1);
            if (_tuboGo != null) _tuboGo.SetActive(false); // el tubo se hunde con su dueño.
        }

        /// <summary>
        /// (R85, fix del tope del REORDEN) Retirada INSTANTÁNEA para el
        /// camino de emergencia del director: si el tope del paso 1 dispara
        /// con este recipiente aún drenando/hundiéndose, ANTES de destruir el
        /// GO hay que desmontar la estructura — sin esto quedaban muros
        /// huérfanos + obra fantasma anticincel en mitad del taller.
        /// Devuelve lo drenado hasta ahora (el banco no pierde nada).
        /// </summary>
        public int RetirarDeGolpe()
        {
            if (_fase == Fase.Oculto || _fase == Fase.Enterrado) return DrenadoDelDueno;
            // Contar lo que quede dentro como drenado (el mundo lo toma de golpe).
            DrenadoDelDueno += DelDueno();
            if (_sim != null && _sim.Grid != null)
            {
                for (int x = _x0 + 1; x < _x1; x++)
                    for (int y = _y0 + 1; y <= _y1; y++)
                        if (_sim.Grid.GetMat(x, y) != MaterialId.Empty) _sim.Paint(x, y, 0, MaterialId.Empty);
                if (_fase != Fase.Hundiendo) DesmontarEstructura(); // en Hundiendo ya se desmontó.
            }
            _fase = Fase.Enterrado;
            if (_cuerpo != null) _cuerpo.gameObject.SetActive(false);
            return DrenadoDelDueno;
        }

        /// <summary>(R84) La emergencia en reversa: el cuerpo baja con el mismo easing y desaparece bajo el suelo.</summary>
        private void TickHundir()
        {
            float t = Mathf.Clamp01(_tFase / _g.depositoEmergerSeg);
            float ease = t * t * (3f - 2f * t);
            _cuerpo.localPosition = new Vector3(0f, -_alturaMundo * ease, 0f);
            if (t >= 1f)
            {
                _cuerpo.gameObject.SetActive(false);
                _fase = Fase.Enterrado;
            }
        }

        /// <summary>
        /// LA SALIDA DE LA TIERRA: el cuerpo sube con easing (rápido al
        /// inicio, asentándose al final) mientras suelta terrones de lodo por
        /// los flancos — la tierra que desplaza. Al asentarse, los MUROS
        /// entran a la sim (dos columnas de piedra, obra del taller: el imp
        /// no choca, el cincel no muerde) y empieza el primer llenado.
        /// </summary>
        private void TickEmerger(float dt)
        {
            float t = Mathf.Clamp01(_tFase / _g.depositoEmergerSeg);
            float ease = 1f - (1f - t) * (1f - t) * (1f - t); // cúbico: frena al asentarse.
            _cuerpo.localPosition = new Vector3(0f, -_alturaMundo * (1f - ease), 0f);

            _polvoTimer -= dt;
            if (_polvoTimer <= 0f && t < 0.85f)
            {
                _polvoTimer = PolvoCadaSeg;
                _polvoIdx++;
                // Terrones alternando flanco, con jitter determinista.
                // (R88, cazado por Cesar: "en la esquina inferior izquierda
                // del silo a veces parece que se filtra algo") El flanco del
                // silo da a la POZA: los terrones decorativos caían al pozo y
                // se leían como una fuga. Si bajo el flanco no hay suelo, ese
                // terrón NO se suelta — la tierra desplazada no vuela sobre
                // los huecos.
                // (R91, Cesar: "siempre quedan restos de agua y lodo entre
                // los contenedores… y eso mancha el reset") El RENACER es
                // LIMPIO: los tanques del mundo ordenado no escupen tierra
                // (los terrones eran justo esos restos). Solo la emergencia
                // del primer acto — el mundo aún salvaje — los suelta.
                int lado = (_polvoIdx & 1) == 0 ? _x0 - 1 : _x1 + 1;
                if (!_cargaInstantanea && _sim.Grid.GetMat(lado, _y0 - 1) != MaterialId.Empty)
                    _sim.PaintStable(lado, _y0 + 1 + (_polvoIdx % 3), 0, Lodo);
            }

            if (t >= 1f)
            {
                // (revisión Opus 73 #3) NUNCA EMPAREDAR AL JUGADOR: si su
                // caja (±0.21 u ≈ 4.2 celdas) solapa el sitio de los muros,
                // los muros ESPERAN — el tanque queda asentado en lo visual y
                // la piedra entra al frame en que el jugador se aparta. Sin
                // esta guarda, la piedra podía nacer DENTRO de su AABB y la
                // colisión rechazaba todo movimiento: soft-lock sin salida.
                var jugador = ApprenticeController.AprendizLocal != null
                    ? ApprenticeController.AprendizLocal.transform
                    : FindAnyObjectByType<ApprenticeController>()?.transform;
                if (jugador != null)
                {
                    float c2 = SimRenderer.CellWorldSize;
                    float px = jugador.position.x / c2, py = jugador.position.y / c2;
                    if (px > _x0 - 4f && px < _x1 + 4f && py > _y0 - 5f && py < _y1 + 5f)
                    {
                        // (R89, medido en vivo) LA ESPERA TIENE TOPE: un
                        // jugador QUIETO en el sitio congelaba el asentado
                        // para siempre (el tope del director cerraba el arco
                        // con un tanque SIN muros ni carga — peor que
                        // emparedarlo). A los 6 s los muros entran igual:
                        // la colisión suspende el frame que arranca dentro
                        // de sólido y SALES NADANDO (doctrina
                        // ApprenticeController, pariente de la regla 38).
                        _esperaJugadorT += dt;
                        if (_esperaJugadorT < 6f) return; // reintenta el próximo frame.
                    }
                }

                // Los muros del tanque entran a la sim (PaintStable, regla 22:
                // materia nueva nace a temperatura estable). (R86, LA
                // FILTRACIÓN DEL EXTREMO cazada por Cesar) Los muros suben
                // UNA FILA por encima del interior (y1+1): con muro e
                // interior rasos, la celda líquida del tope se escurría en
                // diagonal por encima. La boca sigue abierta para verter.
                for (int y = _y0; y <= _y1 + 1; y++)
                {
                    _sim.PaintStable(_x0, y, 0, MaterialId.Stone);
                    _sim.PaintStable(_x1, y, 0, MaterialId.Stone);
                }
                // (R88, Cesar: "no necesitan piso… son de metal y ese es su
                // piso") EL FONDO INTERNO: la fila y0, DENTRO de la huella y
                // tapada por el zócalo de cobre del sprite — sella el escape
                // diagonal de la R85 sin tocar EL LECHO DEL MUNDO (nada se
                // pinta ya en y0-1: se acabó el piso postizo y su memoria).
                for (int x = _x0 + 1; x < _x1; x++)
                    _sim.PaintStable(x, _y0, 0, MaterialId.Stone);
                _obraHandle = SimLevelBuilder.RegistrarObra(_x0, _y0, _x1, _y1 + 1); // fondo y remate incluidos; el handle se GUARDA (Opus A8).
                if (_marcoSr != null) _marcoSr.sortingOrder = Capas.MaquinaFrente; // el armazón pasa a ser el vidrio delante del agua.

                // (R88) EL RENACER SUBE YA LLENO: la carga entera se pinta al
                // asentarse (dirección Opus: la espera del llenado era un
                // progress bar, y un progress bar es lo contrario de una
                // declaración). El llenado celda a celda queda para la carga
                // inicial del primer acto (CargaInicial de siempre).
                if (_cargaInstantanea)
                {
                    int meta = _cargaInicial >= 0 ? _cargaInicial : _g.depositoCargaInicial;
                    int puestas = 0;
                    for (int y = _y0 + 1; y <= _y1 && puestas < meta; y++)
                        for (int x = _x0 + 1; x < _x1 && puestas < meta; x++)
                        {
                            _sim.PaintStable(x, y, 0, _matDueno);
                            puestas++;
                        }
                    _fase = Fase.Listo;
                    _tFase = 0f;
                    return;
                }

                _fase = Fase.CargaInicial;
                _tFase = 0f;
                _rellenoTimer = 0.4f; // medio respiro antes del primer chorro.
            }
        }

        /// <summary>
        /// (R74) LA CARGA INICIAL: el culo de agua con el que el tanque llega
        /// del subsuelo — cae dentro del vidrio celda a celda y se acomoda a
        /// la vista. Al terminar, el depósito queda LISTO y quieto: llenarlo
        /// de verdad es la última tarea del prólogo (LLÉNALO., director).
        /// </summary>
        private void TickCargaInicial(float dt)
        {
            // (R83) La meta de la carga es por-recipiente: el silo nace VACÍO
            // (_cargaInicial=0 → pasa a Listo al primer tick), el tanque usa
            // la del guion.
            int meta = _cargaInicial >= 0 ? _cargaInicial : _g.depositoCargaInicial;

            _rellenoTimer -= dt;
            if (_rellenoTimer > 0f) return;
            _rellenoTimer = CargaInicialSeg;

            if (DelDueno() >= meta)
            {
                _fase = Fase.Listo;
                return;
            }

            // Alterna las dos columnas centrales: el chorro serpentea un pelo.
            int cx = ((_x0 + _x1) / 2) + (_polvoIdx++ & 1);
            _sim.PaintStable(cx, _y1, 0, _matDueno);
        }
    }
}
