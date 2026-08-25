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
    /// algo tuyo). (R85, FASE B2) El día del autofill LLEGÓ, pero solo para
    /// el RENACER REUBICADO del REORDEN (InitFinal/InitSiloFinal): tubería
    /// lateral que toca el suelo (segunda referencia de Cesar,
    /// docs/ref/deposito_agua_ref2_tuberia_autofill.png) + goteo lento por
    /// ActivarRefill. Antes del reorden, llenar sigue siendo TU tarea.
    ///
    /// Regla 36: Init/Aparecer NO son idempotentes; el depósito no es
    /// IMovible (emerge donde el plano lo decreta y ahí se queda).
    /// </summary>
    public sealed class DepositoDeAgua : MonoBehaviour
    {
        // ---- Los números del esqueleto ----
        // (R75) Duración de la emergencia y carga inicial viven en el GUION
        // (GuionDelPrologo.asset, editable en Inspector); aquí solo cadencias
        // internas menores.
        private const float PolvoCadaSeg = 0.22f;     // ritmo del polvo que suelta al emerger.
        private const float CargaInicialSeg = 0.09f;  // cadencia de la carga (cae dentro del vidrio, se ve acomodarse).
        private GuionDelPrologo _g;

        private AlkahestSim _sim;
        private int _x0, _x1, _y0, _y1;               // muros en x0/x1; interior x0+1..x1-1, y0..y1.
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

        // (R85, FASE B2) EL RENACER REUBICADO: en la estantería el recipiente
        // vive ELEVADO, con un tubo lateral que toca el suelo y un goteo de
        // reabastecimiento (el primer refill infinito del juego). La
        // emergencia se ACORTA (~8 celdas): subir el alto entero atravesaría
        // la bahía de abajo — el tanque asoma desde detrás de su repisa.
        private bool _reubicado;
        private bool _tuboIzquierda;                  // flanco del tubo (y del inlet del goteo).
        private GameObject _tuboGo;
        private float _alturaEmergencia;              // cuánto sube el cuerpo al emerger (= _alturaMundo salvo reubicado).
        private bool _refillActivo;
        private float _refillTimer;

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
                for (int y = _y0; y <= _y1; y++)
                {
                    byte m = grid.GetMat(x, y);
                    if (m == _matDueno || (m != MaterialId.Empty && m == _matDuenoAlt)) n++;
                }
            return n;
        }

        /// <summary>Alias histórico (el tanque de agua nació contando solo agua). Consumidores viejos intactos.</summary>
        public int AguaDentro() => DelDueno();

        /// <summary>Celdas NO vacías dentro del vidrio (dueño + estorbo). Con Capacidad(), da la placa honesta del guion (Opus A5).</summary>
        public int Ocupado()
        {
            if (_sim == null || _sim.Grid == null || _fase == Fase.Oculto || _fase == Fase.Emergiendo) return 0;
            int n = 0;
            var grid = _sim.Grid;
            for (int x = _x0 + 1; x < _x1; x++)
                for (int y = _y0; y <= _y1; y++)
                    if (grid.GetMat(x, y) != MaterialId.Empty) n++;
            return n;
        }

        /// <summary>Celdas del interior del vidrio.</summary>
        public int Capacidad() => (_x1 - _x0 - 1) * (_y1 - _y0 + 1);

        /// <summary>Centro del recipiente en el mundo (ancla de la placa del director) — general para cualquier alto.</summary>
        public Vector3 CentroMundo()
        {
            float c = SimRenderer.CellWorldSize;
            return new Vector3((_x0 + _x1 + 1) * 0.5f * c, (_y0 + (_y1 - _y0 + 1) * 0.5f + 2f) * c, 0f);
        }

        /// <summary>El tanque de agua clásico (marcador `deposito`, huella 8x13, piel de prefab, carga inicial del guion).</summary>
        public void Init(AlkahestSim sim) => Init(sim, -1);

        /// <summary>(R84) Variante con carga inicial EXPLÍCITA: el renacer del REORDEN arranca con lo que el drenado + el barrido recogieron ("lo que sale primero es lo que tú derramaste").</summary>
        public void Init(AlkahestSim sim, int cargaInicial)
        {
            var escena = PrologoEscenografia.Buscar();
            InitInterno(sim, escena, escena != null ? escena.deposito : null,
                MaterialId.Water, MaterialId.Empty, cargaInicial: cargaInicial, esSilo: false,
                anchoHuella: 8, altoHuella: 13,
                fallbackX0: SimLevelBuilder.FundacionDepositoX0, fallbackY0: SimLevelBuilder.FundacionDepositoY0);
        }

        /// <summary>
        /// (R83; retallado R84 a 8x13 por Cesar) EL SILO DEL LODO: gemelo en
        /// tamaño del tanque de agua, dueño lodo+barbotina, SIN carga inicial
        /// y SIN piel de prefab (la piel horneada es la del tanque; el silo
        /// viste el fallback procedural hasta su sprite propio).
        /// </summary>
        public void InitSilo(AlkahestSim sim) => InitSilo(sim, 0);

        /// <summary>(R84) Variante con carga explícita para el renacer del REORDEN.</summary>
        public void InitSilo(AlkahestSim sim, int cargaInicial)
        {
            var escena = PrologoEscenografia.Buscar();
            // (R84, Cesar) MISMO TAMAÑO que el tanque (8x13): la distinción
            // vendrá por carteles/decoración, no por silueta. El interior
            // (x386-391) sigue cabiendo EXACTO en el aire medido poza|cráter;
            // los muros pisan los labios (ver FundacionSilo* en el plano).
            InitInterno(sim, escena, escena != null ? escena.deposito2 : null,
                Lodo, Barbotina, cargaInicial: cargaInicial, esSilo: true,
                anchoHuella: 8, altoHuella: 13,
                fallbackX0: SimLevelBuilder.FundacionSiloX0, fallbackY0: SimLevelBuilder.FundacionSiloY0);
        }

        /// <summary>
        /// (R85, FASE B2) El tanque de agua RENACIDO en la bahía BAJA de la
        /// estantería (marcador `depositoFinal`; sin él, las constantes del
        /// plano). Tubo lateral al suelo por la IZQUIERDA — revisión Opus
        /// B2 #5 (inversión semántica, medida en captura): el pie del tubo
        /// del AGUA aterriza HACIA LA POZA; el jugador lee "este bebe del
        /// charco" y es verdad en espíritu.
        /// </summary>
        public void InitFinal(AlkahestSim sim, int cargaInicial)
        {
            var escena = PrologoEscenografia.Buscar();
            InitInterno(sim, escena, escena != null ? escena.depositoFinal : null,
                MaterialId.Water, MaterialId.Empty, cargaInicial: cargaInicial, esSilo: false,
                anchoHuella: 8, altoHuella: 13,
                fallbackX0: SimLevelBuilder.FundacionBahiaX0, fallbackY0: SimLevelBuilder.FundacionBahiaBajaY0,
                reubicado: true, tuboIzquierda: true);
        }

        /// <summary>
        /// (R85, FASE B2) El silo del lodo RENACIDO en la bahía ALTA
        /// (marcador `deposito2Final`). Tubo LARGO al suelo por la DERECHA
        /// (revisión Opus B2 #5): su brida pisa el flanco del CRÁTER — el
        /// lodo bebe de la herida, no de la poza. Y la gotera del derrumbe
        /// cae derecha a su boca abierta: la herida alimenta el almacén por
        /// arriba Y por abajo hasta que la Fase C selle el techo.
        /// </summary>
        public void InitSiloFinal(AlkahestSim sim, int cargaInicial)
        {
            var escena = PrologoEscenografia.Buscar();
            InitInterno(sim, escena, escena != null ? escena.deposito2Final : null,
                Lodo, Barbotina, cargaInicial: cargaInicial, esSilo: true,
                anchoHuella: 8, altoHuella: 13,
                fallbackX0: SimLevelBuilder.FundacionBahiaX0, fallbackY0: SimLevelBuilder.FundacionBahiaAltaY0,
                reubicado: true, tuboIzquierda: false);
        }

        private void InitInterno(AlkahestSim sim, PrologoEscenografia escena, Transform marcador,
            byte matDueno, byte matDuenoAlt, int cargaInicial, bool esSilo,
            int anchoHuella, int altoHuella, int fallbackX0, int fallbackY0,
            bool reubicado = false, bool tuboIzquierda = false)
        {
            _sim = sim;
            _matDueno = matDueno;
            _matDuenoAlt = matDuenoAlt;
            _cargaInicial = cargaInicial;
            _esSilo = esSilo;
            _reubicado = reubicado;
            _tuboIzquierda = tuboIzquierda;

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
            // (R85) La emergencia CORTA del reubicado: subir el alto entero
            // (19 celdas) haría atravesar la bahía de abajo al silo alto —
            // el cuerpo asoma solo ~8 celdas desde detrás de su repisa.
            _alturaEmergencia = _reubicado ? Mathf.Min(_alturaMundo, 8f * c) : _alturaMundo;

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
            if (!_esSilo && escena != null && escena.depositoVisualPrefab != null)
            {
                var piel = Instantiate(escena.depositoVisualPrefab, _cuerpo);
                piel.name = "PielPrefab";
                piel.transform.localPosition = Vector3.zero;
                var marcoPrefab = piel.transform.Find("Marco");
                _marcoSr = marcoPrefab != null ? marcoPrefab.GetComponent<SpriteRenderer>() : null;
                if (_marcoSr != null) _marcoSr.sortingOrder = Capas.MaquinaFondoInterior + 2; // emerge OCULTO tras la roca (revisión Opus 73 #15).
                transform.position = new Vector3(cx, baseY, 0f);
                _cuerpo.localPosition = new Vector3(0f, -_alturaEmergencia, 0f);
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
            _marcoSr = MaquinariaSprites.CrearCapa(marcoGo.transform, "Sprite",
                MaquinariaSprites.TanqueMarco(spanVisual, altoVisual),
                Capas.MaquinaFondoInterior + 2, spanVisual * c, altoVisual * c);

            // Posiciones locales definitivas (relativas al centro del cuerpo):
            marcoGo.transform.localPosition = new Vector3(0f, altoVisual * 0.5f * c, 0f);
            fondoGo.transform.localPosition = new Vector3(0f, (_y1 - _y0 + 1) * 0.5f * c, 0f);

            // El cuerpo entero arranca ENTERRADO (invisible bajo el suelo);
            // Aparecer() lo hace emerger.
            transform.position = new Vector3(cx, baseY, 0f);
            _cuerpo.localPosition = new Vector3(0f, -_alturaEmergencia, 0f);
            _cuerpo.gameObject.SetActive(false);
        }

        /// <summary>Arranca la emergencia cinematográfica (la llama el director al completarse agua + lodo).</summary>
        public void Aparecer()
        {
            if (_fase != Fase.Oculto) return;
            _fase = Fase.Emergiendo;
            _tFase = 0f;
            _cuerpo.gameObject.SetActive(true);
            if (_reubicado && _tuboGo == null) CrearTubo();
        }

        /// <summary>
        /// (R85, FASE B2) EL TUBO LATERAL AL SUELO — visual puro, hijo del
        /// GO raíz (NO del cuerpo: el tubo no emerge, ya estaba ahí; el
        /// tanque "se conecta" al asomar). Nace oculto y se enciende cuando
        /// el recipiente ASIENTA (TickEmerger). DELANTE de la sim (y detrás
        /// del vidrio del marco): medido con captura — detrás de la sim, el
        /// montante de la estantería (madera EN la grilla) se tragaba el
        /// tubo del silo entero; delante, la tubería corre montada SOBRE el
        /// mueble, como en la referencia 2 de Cesar.
        /// </summary>
        private void CrearTubo()
        {
            float c = SimRenderer.CellWorldSize;
            int sueloY = SimLevelBuilder.FundacionY0;            // primera fila de aire: la brida PISA el suelo macizo de y139.
            int altoCeldas = (_y0 + 2) - sueloY;                 // caña hasta el codo, que entra al vidrio en y0..y0+1.
            _tuboGo = new GameObject("Tubo");
            _tuboGo.transform.SetParent(transform, false);
            var sr = MaquinariaSprites.CrearCapa(_tuboGo.transform, "Sprite",
                MaquinariaSprites.TanqueTuboLateral(altoCeldas),
                Capas.MaquinaFrente - 5, 4f * c, altoCeldas * c);
            sr.flipX = _tuboIzquierda;                            // dibujado con el codo a la izquierda (tubo al flanco derecho).
            float centroX = (_tuboIzquierda ? (_x0 - 1f) : (_x1 + 2f)) * c;
            _tuboGo.transform.position = new Vector3(centroX, (sueloY + altoCeldas * 0.5f) * c, 0f);
            _tuboGo.SetActive(false);
        }

        /// <summary>
        /// (R85) Enciende el goteo de reabastecimiento (cadencia y tope del
        /// guion): el tubo repone el material DUEÑO celda a celda por su
        /// columna del inlet hasta `refillTope`, y de ahí en más solo cubre
        /// lo que el jugador se lleve. El primer refill infinito del juego —
        /// llega EXCLUSIVAMENTE con el renacer reubicado (antes del REORDEN,
        /// llenar los recipientes es TU tarea: R74).
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
            _refillTimer = _g.refillSeg;
            if (DelDueno() >= _g.refillTope) return;              // lleno hasta el tope: el tubo descansa.
            int inlet = _tuboIzquierda ? _x0 + 1 : _x1 - 1;       // la columna interior del flanco del tubo.
            var grid = _sim.Grid;
            for (int y = _y0; y <= _y1; y++)                      // la celda LIBRE más baja del inlet: brota desde abajo, como del suelo.
            {
                if (grid.GetMat(inlet, y) != MaterialId.Empty) continue;
                _sim.PaintStable(inlet, y, 0, _matDueno);
                return;
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

        private SpriteRenderer _marcoSr;
        private int _obraHandle = -1; // (R84, Opus A8) el handle de RegistrarObra: sin él, la retirada dejaría obra FANTASMA anticincel (el fantasma R69).

        private void Update()
        {
            if (_sim == null || _sim.Grid == null || _fase == Fase.Oculto || _fase == Fase.Enterrado) return;
            if (DayCycle.InputLocked) return; // (revisión Opus 73 #1) con la pausa delante, la emergencia y el llenado esperan (la sim está congelada).
            float dt = Time.deltaTime;
            _tFase += dt;

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
            _rellenoTimer = 0.035f; // más brioso que el cuenco: hay hasta 78 celdas.

            var grid = _sim.Grid;
            for (int y = _y1; y >= _y0; y--)
            {
                for (int x = _x0 + 1; x < _x1; x++)
                {
                    byte m = grid.GetMat(x, y);
                    if (m == MaterialId.Empty) continue;
                    if (m == _matDueno || (m != MaterialId.Empty && m == _matDuenoAlt)) DrenadoDelDueno++;
                    _sim.Paint(x, y, 0, MaterialId.Empty);
                    return; // una celda por tick: se VE al mundo tomarlo.
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
        /// (R85) Muros fuera + suelo propio RESTAURADO + obra degenerada.
        /// Compartido por el drenado normal y la retirada de golpe. El suelo
        /// no se borra: se devuelve lo que había ANTES de sellarlo
        /// (_sueloPrevio, capturado al asentarse) — medido en vivo: borrarlo
        /// a Empty dejaba una ZANJA de 1 celda en el lecho de roca (el silo
        /// pisa bedrock en x386-391) con boca a la poza en x385.
        /// </summary>
        private void DesmontarEstructura()
        {
            for (int y = _y0; y <= _y1; y++)
            {
                _sim.Paint(_x0, y, 0, MaterialId.Empty);
                _sim.Paint(_x1, y, 0, MaterialId.Empty);
            }
            for (int x = _x0; x <= _x1; x++)
            {
                byte previo = _sueloPrevio != null ? _sueloPrevio[x - _x0] : MaterialId.Empty;
                if (previo == MaterialId.Empty) _sim.Paint(x, _y0 - 1, 0, MaterialId.Empty);
                else _sim.PaintStable(x, _y0 - 1, 0, previo); // regla 22: materia restaurada nace estable.
            }
            if (_obraHandle >= 0) SimLevelBuilder.ActualizarObra(_obraHandle, 0, 0, -1, -1);
        }
        private byte[] _sueloPrevio; // (R85) qué había bajo la huella antes del suelo propio — la retirada lo devuelve.

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
                    for (int y = _y0; y <= _y1; y++)
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
            _cuerpo.localPosition = new Vector3(0f, -_alturaEmergencia * ease, 0f);
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
            _cuerpo.localPosition = new Vector3(0f, -_alturaEmergencia * (1f - ease), 0f);

            _polvoTimer -= dt;
            if (_polvoTimer <= 0f && t < 0.85f)
            {
                _polvoTimer = PolvoCadaSeg;
                _polvoIdx++;
                // Terrones alternando flanco, con jitter determinista.
                int lado = (_polvoIdx & 1) == 0 ? _x0 - 1 : _x1 + 1;
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
                        return; // reintenta el próximo frame.
                }

                // Los muros del tanque entran a la sim (PaintStable, regla 22:
                // materia nueva nace a temperatura estable).
                for (int y = _y0; y <= _y1; y++)
                {
                    _sim.PaintStable(_x0, y, 0, MaterialId.Stone);
                    _sim.PaintStable(_x1, y, 0, MaterialId.Stone);
                }
                // (R85, FUGA CAZADA POR CESAR: "el lodo y el agua se filtran
                // cuando hay hueco abajo") EL RECIPIENTE SELLA SU PROPIO
                // SUELO: una fila de piso estructural bajo TODA la huella
                // (y0-1). Sin ella, un muro sobre un labio de vacío (poza,
                // cráter, terreno no plano) deja el escape DIAGONAL clásico
                // de los líquidos de falling-sand — el mismo mecanismo de la
                // fuga de la cascada en R74 ("la destrucción muerde; la
                // fontanería, no"). Además garantiza base plana en cualquier
                // terreno (Opus A3, ahora obligatorio y no aplazable).
                _sueloPrevio = new byte[_x1 - _x0 + 1]; // memoria del lecho: la retirada devuelve lo que el sello pisó.
                for (int x = _x0; x <= _x1; x++)
                {
                    _sueloPrevio[x - _x0] = _sim.Grid.GetMat(x, _y0 - 1);
                    _sim.PaintStable(x, _y0 - 1, 0, MaterialId.PisoEstructural);
                }
                _obraHandle = SimLevelBuilder.RegistrarObra(_x0, _y0 - 1, _x1, _y1); // (R84/R85) suelo incluido; el handle se GUARDA: la retirada lo degenera (Opus A8).
                if (_marcoSr != null) _marcoSr.sortingOrder = Capas.MaquinaFrente; // el armazón pasa a ser el vidrio delante del agua.
                if (_tuboGo != null) _tuboGo.SetActive(true); // (R85) el tanque ASENTÓ: el tubo al suelo se descubre con él.

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
