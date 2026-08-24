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
    /// (RONDA 74, corrección de Cesar) SIN AUTOFILL TODAVÍA: el depósito
    /// llega con solo un culo de agua y la tarea final del prólogo es
    /// LLENARLO — el verbo, una vez más, ahora al revés (verter DENTRO de
    /// algo tuyo). El rellenado automático y el tubo de conexión infinita
    /// llegarán juntos más adelante (segunda referencia de Cesar,
    /// docs/ref/deposito_agua_ref2_tuberia_autofill.png): entonces este
    /// tanque ganará su tubería lateral al suelo y el goteo lento. La
    /// fábrica MaquinariaSprites.TanqueTubo ya existe esperando ese día.
    ///
    /// Regla 36: Init/Aparecer NO son idempotentes; el depósito no es
    /// IMovible (emerge donde el plano lo decreta y ahí se queda).
    /// </summary>
    public sealed class DepositoDeAgua : MonoBehaviour
    {
        // ---- Los números del esqueleto (afinables) ----
        private const float EmergerSeg = 2.4f;        // duración de la salida de la tierra.
        private const float PolvoCadaSeg = 0.22f;     // ritmo del polvo que suelta al emerger.
        private const int CargaInicialCeldas = 14;    // el culo de agua con el que emerge (~2 filas): suficiente para entender QUÉ es, insuficiente para no llenarlo.
        private const float CargaInicialSeg = 0.09f;  // cadencia de esa carga (cae dentro del vidrio, se ve acomodarse).

        private AlkahestSim _sim;
        private int _x0, _x1, _y0, _y1;               // muros en x0/x1; interior x0+1..x1-1, y0..y1.
        private Transform _cuerpo;                    // todas las capas visuales cuelgan de aquí (para el ascenso).
        private float _alturaMundo;

        private enum Fase { Oculto, Emergiendo, CargaInicial, Listo }
        private Fase _fase = Fase.Oculto;
        private float _tFase;
        private float _polvoTimer, _rellenoTimer;
        private int _polvoIdx;

        private static readonly byte Lodo = MaterialId.MatDe(1, EstadoMateria.Polvo); // los terrones que suelta al emerger.

        /// <summary>Ya asentado (muros en la sim, carga inicial en marcha o hecha).</summary>
        public bool Asentado => _fase == Fase.CargaInicial || _fase == Fase.Listo;
        /// <summary>La carga inicial terminó: el tanque espera a que el jugador lo LLENE (no se repone solo — R74).</summary>
        public bool CargaLista => _fase == Fase.Listo;

        /// <summary>Agua REAL dentro del vidrio (el director la cuenta para el LLÉNALO. final).</summary>
        public int AguaDentro()
        {
            if (_sim == null || _sim.Grid == null || _fase == Fase.Oculto || _fase == Fase.Emergiendo) return 0;
            int n = 0;
            var grid = _sim.Grid;
            for (int x = _x0 + 1; x < _x1; x++)
                for (int y = _y0; y <= _y1; y++)
                    if (grid.GetMat(x, y) == MaterialId.Water) n++;
            return n;
        }
        /// <summary>Centro del tanque en el mundo (ancla de la placa del director).</summary>
        public Vector3 CentroMundo()
        {
            float c = SimRenderer.CellWorldSize;
            return new Vector3((_x0 + _x1 + 1) * 0.5f * c, (_y0 + 8) * c, 0f);
        }

        public void Init(AlkahestSim sim)
        {
            _sim = sim;
            _x0 = SimLevelBuilder.FundacionDepositoX0;
            _x1 = SimLevelBuilder.FundacionDepositoX1;
            _y0 = SimLevelBuilder.FundacionDepositoY0;
            _y1 = SimLevelBuilder.FundacionDepositoY1;

            float c = SimRenderer.CellWorldSize;
            int spanVisual = (_x1 - _x0 + 1) + 2;      // 1 celda de vuelo a cada lado.
            int altoVisual = (_y1 - _y0 + 1) + 6;      // banda alta + domo por encima de los muros.
            _alturaMundo = altoVisual * c;

            _cuerpo = new GameObject("DepositoCuerpo").transform;
            _cuerpo.SetParent(transform, false);
            float cx = (_x0 + _x1 + 1) * 0.5f * c;

            // (R74) EL TUBO TRASERO SE RETIRÓ de esta versión: insinuaba un
            // rellenado que ya no existe. Vuelve como tubería lateral
            // completa junto con el autofill (ver docblock de clase).

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
            float baseY = _y0 * c; // borde inferior del tanque en el mundo.
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

        private SpriteRenderer _marcoSr;

        private void Update()
        {
            if (_sim == null || _sim.Grid == null || _fase == Fase.Oculto) return;
            if (DayCycle.InputLocked) return; // (revisión Opus 73 #1) con la pausa delante, la emergencia y el llenado esperan (la sim está congelada).
            float dt = Time.deltaTime;
            _tFase += dt;

            switch (_fase)
            {
                case Fase.Emergiendo: TickEmerger(dt); break;
                case Fase.CargaInicial: TickCargaInicial(dt); break;
                case Fase.Listo: break; // (R74) no se repone solo: llenarlo es TU tarea.
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
            float t = Mathf.Clamp01(_tFase / EmergerSeg);
            float ease = 1f - (1f - t) * (1f - t) * (1f - t); // cúbico: frena al asentarse.
            _cuerpo.localPosition = new Vector3(0f, -_alturaMundo * (1f - ease), 0f);

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
                SimLevelBuilder.RegistrarObra(_x0, _y0, _x1, _y1);
                if (_marcoSr != null) _marcoSr.sortingOrder = Capas.MaquinaFrente; // el armazón pasa a ser el vidrio delante del agua.

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
            _rellenoTimer -= dt;
            if (_rellenoTimer > 0f) return;
            _rellenoTimer = CargaInicialSeg;

            if (AguaDentro() >= CargaInicialCeldas)
            {
                _fase = Fase.Listo;
                return;
            }

            // Alterna las dos columnas centrales: el chorro serpentea un pelo.
            int cx = ((_x0 + _x1) / 2) + (_polvoIdx++ & 1);
            _sim.PaintStable(cx, _y1, 0, MaterialId.Water);
        }
    }
}
