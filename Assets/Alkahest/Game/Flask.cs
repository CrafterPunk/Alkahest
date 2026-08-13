using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// El frasco del aprendiz: aspira (LMB) y vierte (RMB) celdas de
    /// material de la simulación, y puede vaciarse de golpe (Q / botón
    /// central). Guarda hasta <see cref="Capacity"/> celdas como conteos
    /// por materialId.
    ///
    /// EL FRASCO ES UN TERMO (fix de progresión del playtest 4). Antes solo
    /// guardaba CUÁNTO llevaba de cada material, no a qué temperatura: al
    /// verter, la celda nacía a temperatura AMBIENTE. Consecuencia real: los
    /// encargos "algo helado (-5 °C o menos)" y "algo que queme al tacto
    /// (80 °C o más)" eran IMPOSIBLES de cumplir — no hay forma de calentar ni
    /// enfriar dentro de la boca de la Tolva, que consume lo vertido en el tick
    /// siguiente. Ahora el frasco lleva también la temperatura MEDIA de cada
    /// material (<see cref="_tempSum"/>) y la restituye al verter, vía
    /// AlkahestSim.PaintCell. Es además lo que la fantasía promete: si aspiras
    /// hielo, sigues llevando hielo.
    ///
    /// (fix playtest 10, reportes 7+8) DOS QUEJAS QUE ERAN LA MISMA: "manejar
    /// cursor Y personaje se siente antinatural" y "aspiro y me llevo restos de
    /// otro material sin querer". Ambas nacen de que la herramienta (el chorro
    /// de aspirado/vertido) es invisible y no discrimina. Tres cambios:
    ///  1) BLOQUEO DE MATERIAL: al pulsar aspirar se fija el material bajo el
    ///     cursor para TODA la pulsación (ver <see cref="BloquearMaterialBajoElCursor"/>).
    ///     Shift mantiene el comportamiento viejo (todo indiscriminado), para
    ///     limpiar destrozos. Esto es la respuesta real al reporte 8 — el
    ///     jugador proponía zoomear con el scroll para apuntar mejor, pero eso
    ///     (a) pelearía con el autoencuadre de cámara que costó dos playtests
    ///     arreglar (SimRenderer.FitMainCamera se ajusta al mundo ENTERO;
    ///     zoomear reintroduce exactamente la clase de bug de encuadre de los
    ///     playtests 5/6), (b) obliga a alternar "navegar" y "apuntar" en vez
    ///     de solo apuntar, y (c) no arregla nada: la herramienta seguiría sin
    ///     discriminar, solo se vería más grande. Bloquear el material lo
    ///     arregla de raíz sin una tecla nueva en el bucle principal.
    ///  2) EL HAZ (<see cref="UpdateWorldVisuals"/>): una línea del frasco al
    ///     cursor mientras se aspira/vierte — convierte "dos cosas que muevo"
    ///     (reporte 7) en "una herramienta que apunta". Se corta en el borde
    ///     del alcance y cambia de color si el cursor está fuera.
    ///  3) (retirado, playtest 11) Hubo un ANILLO DE ALCANCE: un aro tenue
    ///     alrededor del aprendiz que se encendía al acercarse al borde. El
    ///     jugador probó las dos cosas y pidió quitar solo esta: "el anillo
    ///     de alcance está feo, quítalo" (el haz y el bloqueo de material se
    ///     quedan, le encantaron). El límite de alcance (<see cref="ReachWorld"/>)
    ///     se sigue comunicando solo con el corte del haz en el borde y el
    ///     aviso "demasiado lejos" — no reintroducir el anillo pensando que
    ///     es una idea nueva, ya se probó y se descartó.
    ///
    /// Nota de determinismo/netcode: TODA mutación de la grilla pasa por
    /// AlkahestSim.Paint/PaintCell (nunca acceso directo a CellGrid), tal y como
    /// exige el resto del proyecto.
    /// </summary>
    [RequireComponent(typeof(ApprenticeController))]
    public sealed class Flask : MonoBehaviour
    {
        /// <summary>Mensaje corto de feedback para el HUD ("frasco vacío", "demasiado lejos"). Se autolimpia.</summary>
        public string Feedback { get; private set; } = "";
        public float FeedbackUntil { get; private set; }
        private void SetFeedback(string msg) { Feedback = msg; FeedbackUntil = Time.time + 1.5f; }

        /// <summary>Mismo canal de feedback, para que otros aparatos (la estantería de redomas) avisen junto al cursor y no en su propia esquina.</summary>
        public void Avisar(string msg) => SetFeedback(msg);

        public const int Capacity = 900;

        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;

        private const int SuckRadius = 4;
        private const int SuckRatePerTick = 30;
        private const int PourRadius = 2;
        private const int PourRatePerTick = 20;
        private const int DumpRadius = 4;
        /// <summary>Alcance máximo (unidades de mundo) desde el aprendiz. Público para que el HUD pinte la retícula en rojo cuando el cursor se sale.</summary>
        public const float ReachWorld = 6f;

        private AlkahestSim _sim;
        private ApprenticeController _apprentice;

        private readonly int[] _counts = new int[256];
        /// <summary>Suma de temperaturas raw de las celdas guardadas de cada material (media = _tempSum/_counts). Ver doc de la clase.</summary>
        private readonly int[] _tempSum = new int[256];
        private int _total;
        private byte[] _pourOrder; // ids 1..255 del universo, ordenados por densidad descendente (calculado una sola vez).

        private float _accumulator;
        private bool _hasCursor;
        private Vector2Int _cursorCell;
        private bool _hasCursorWorld;
        private Vector3 _cursorWorld;

        private SpriteRenderer _carryVisual;

        public int Total => _total;
        public int GetCount(byte matId) => _counts[matId];

        // ---------------------------------------------------------------------------------
        // BLOQUEO DE MATERIAL (fix playtest 10, reporte 8): ver doc de clase.
        // ---------------------------------------------------------------------------------
        private byte _lockedMaterial = MaterialId.Empty;
        private bool _hasLockedMaterial;

        /// <summary>¿Hay un material fijado para la pulsación de aspirado actual?</summary>
        public bool TieneMaterialBloqueado => _hasLockedMaterial;
        /// <summary>El material fijado (solo válido si <see cref="TieneMaterialBloqueado"/>). Usado por FlaskHud.</summary>
        public byte MaterialBloqueado => _lockedMaterial;
        /// <summary>True mientras se está aspirando de verdad (botón pulsado y cursor sobre la grilla). Usado por FlaskHud para mostrar el chip de material bloqueado SOLO mientras dura la acción.</summary>
        public bool EstaAspirando { get; private set; }
        /// <summary>True si Shift está manteniendo el modo "aspira todo indiscriminadamente" este frame. Usado por FlaskHud.</summary>
        public bool ModoIndiscriminado { get; private set; }

        /// <summary>Temperatura raw media del material guardado (ambiente si no llevas nada de él).</summary>
        public byte TempMediaDe(byte matId)
        {
            int c = _counts[matId];
            return c > 0 ? (byte)Mathf.Clamp(_tempSum[matId] / c, 0, 255) : CellGrid.AmbientRaw;
        }

        /// <summary>Material del que más llevas (Empty si el frasco está vacío). Usado por la estantería de redomas para saber qué guardar.</summary>
        public byte MaterialDominante()
        {
            byte mejor = MaterialId.Empty;
            int mejorConteo = 0;
            for (int m = 1; m < MaterialId.Count; m++)
            {
                if (_counts[m] > mejorConteo) { mejorConteo = _counts[m]; mejor = (byte)m; }
            }
            return mejor;
        }

        /// <summary>
        /// Saca hasta `cantidad` celdas de `matId` del frasco SIN tocar la
        /// grilla (transferencia frasco -&gt; contenedor, ver Game/StorageRack.cs).
        /// Devuelve cuántas salieron y a qué temperatura media iban.
        /// </summary>
        public int Extraer(byte matId, int cantidad, out byte tempRaw)
        {
            tempRaw = TempMediaDe(matId);
            if (matId == MaterialId.Empty || cantidad <= 0) return 0;

            int n = Mathf.Min(cantidad, _counts[matId]);
            if (n <= 0) return 0;

            _tempSum[matId] -= tempRaw * n;
            if (_tempSum[matId] < 0) _tempSum[matId] = 0;
            _counts[matId] -= n;
            _total -= n;
            return n;
        }

        /// <summary>Mete hasta `cantidad` celdas de `matId` en el frasco (respetando <see cref="Capacity"/>). Devuelve cuántas entraron.</summary>
        public int Guardar(byte matId, int cantidad, byte tempRaw)
        {
            if (matId == MaterialId.Empty || cantidad <= 0) return 0;

            int hueco = Capacity - _total;
            int n = Mathf.Min(cantidad, hueco);
            if (n <= 0) return 0;

            _counts[matId] += n;
            _tempSum[matId] += tempRaw * n;
            _total += n;
            return n;
        }

        private void Awake()
        {
            _apprentice = GetComponent<ApprenticeController>();
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim)
        {
            _sim = sim;
            BuildPourOrder();
            BuildCarryVisual();
            BuildBeamVisual();
        }

        private void BuildPourOrder()
        {
            int n = MaterialId.Count - 1;
            _pourOrder = new byte[n];
            for (int i = 0; i < n; i++) _pourOrder[i] = (byte)(i + 1); // salta Empty (0)

            // Insertion sort por densidad descendente: N es pequeño (~12) y esto
            // se ejecuta una única vez, así que no hace falta nada más elaborado.
            for (int i = 1; i < n; i++)
            {
                byte key = _pourOrder[i];
                short keyDensity = _sim.Universe.Get(key).density;
                int j = i - 1;
                while (j >= 0 && _sim.Universe.Get(_pourOrder[j]).density < keyDensity)
                {
                    _pourOrder[j + 1] = _pourOrder[j];
                    j--;
                }
                _pourOrder[j + 1] = key;
            }
        }

        private void BuildCarryVisual()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "AlkahestFlaskCarryTex" };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            var go = new GameObject("FlaskCarryVisual");
            go.transform.SetParent(transform, false);
            _carryVisual = go.AddComponent<SpriteRenderer>();
            _carryVisual.sprite = sprite;
            _carryVisual.sortingOrder = 60;
            _carryVisual.color = new Color(1f, 1f, 1f, 0f);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) { OcultarVisualesDeMundo(); return; } // M4: título/intro/fin de día/pantalla final congelan el frasco.

            var mouse = Mouse.current;
            var kb = Keyboard.current;
            // (fix playtest 2) Con la paleta dev (F3) abierta, el pincel manda: el frasco no actúa.
            if (Alkahest.Dev.DevPalette.IsOpen) { OcultarVisualesDeMundo(); return; }

            // (fix playtest 10) Mientras el jugador ESCRIBE un nombre (NamingUi), NINGÚN
            // atajo del frasco debe colarse: ni Q (vaciar) ni Shift (mayúsculas al
            // teclear, no "aspira todo"). Misma regla que ya obedecen M/H/T/E/J/F3/
            // flechas en el resto del proyecto (ver UiStyles.EscribiendoTexto).
            if (UiStyles.EscribiendoTexto) { OcultarVisualesDeMundo(); return; }

            // (fix playtest 10) Con el DIARIO abierto a pantalla completa el mundo
            // está tapado por el velo: aspirar o verter a ciegas detrás del libro
            // solo puede acabar en destrozo. Además el libro pagina con Re Pág /
            // Av Pág y el jugador tiene el ratón encima del papel, no del taller.
            if (JournalHud.Abierto) { OcultarVisualesDeMundo(); return; }

            // (playtest 16) EL CINCEL ES UN MODO, NO OTRO BOTÓN. Con la tecla C el
            // aprendiz cambia lo que lleva en la mano: o el frasco o el cincel,
            // nunca los dos. Sin esta guarda, tallar justo al borde de un charco
            // haría que el frasco empezara a aspirarlo a la vez que se pica la
            // piedra — dos herramientas actuando sobre el mismo clic. Ver la
            // cabecera de Game/Cincel.cs, que documenta el reparto de controles.
            if (Cincel.ModoActivo) { OcultarVisualesDeMundo(); return; }

            // La estantería de redomas captura el ratón cuando el cursor está
            // sobre una redoma: ahí los clics son "guardar/recuperar", no
            // "aspirar/verter" sobre la grilla (si no, verter sobre el estante
            // pintaría material suelto encima del mueble).
            bool ratonCapturado = StorageRack.RatonSobreRedoma();

            bool wantSuck = mouse != null && mouse.leftButton.isPressed && !ratonCapturado;
            bool wantPour = mouse != null && mouse.rightButton.isPressed && !ratonCapturado;
            bool wantDump = !ratonCapturado
                            && ((mouse != null && mouse.middleButton.wasPressedThisFrame)
                                || (kb != null && kb.qKey.wasPressedThisFrame));
            bool suckJustPressed = mouse != null && mouse.leftButton.wasPressedThisFrame && !ratonCapturado;
            // Modificador para aspirar TODO indiscriminadamente (limpiar destrozos):
            // se comprueba CADA frame, no solo al pulsar, así que se puede activar o
            // soltar Shift a mitad de una pulsación sin soltar el botón.
            bool indiscriminado = wantSuck && kb != null && kb.leftShiftKey.isPressed;

            _hasCursorWorld = TryGetCursorWorld(out _cursorWorld);
            _hasCursor = _hasCursorWorld && CeldaDesdeCursorMundo(out _cursorCell);

            if (suckJustPressed)
            {
                if (indiscriminado) { _hasLockedMaterial = false; _lockedMaterial = MaterialId.Empty; }
                else if (_hasCursor) BloquearMaterialBajoElCursor(_cursorCell);
                else { _hasLockedMaterial = false; _lockedMaterial = MaterialId.Empty; }
            }
            if (!wantSuck) { _hasLockedMaterial = false; _lockedMaterial = MaterialId.Empty; } // se libera al soltar

            EstaAspirando = wantSuck && _hasCursor;
            ModoIndiscriminado = indiscriminado;

            if (wantDump && _hasCursor)
            {
                DumpAll(_cursorCell);
            }

            _accumulator += Time.deltaTime;
            int steps = 0;
            while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
            {
                if (_hasCursor)
                {
                    if (wantSuck) TickSuck(_cursorCell, indiscriminado);
                    else if (wantPour) TickPour(_cursorCell);
                }
                _accumulator -= TickDt;
                steps++;
            }
            if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;

            UpdateCarryVisual();
            UpdateWorldVisuals(wantSuck, wantPour);
        }

        /// <summary>Raycast puro cámara-&gt;plano de mundo (z=0), sin comprobar límites de la grilla: lo usan tanto el aspirado/vertido (que sí necesita la celda) como el haz (que necesita el punto aunque caiga fuera de la grilla, p.ej. cursor en el borde de pantalla).</summary>
        private bool TryGetCursorWorld(out Vector3 world)
        {
            world = default;
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return false;

            Vector2 screenPos = mouse.position.ReadValue();
            var ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var groundPlane = new Plane(Vector3.forward, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float enter)) return false;

            world = ray.GetPoint(enter);
            return true;
        }

        /// <summary>Convierte <see cref="_cursorWorld"/> (ya calculado este frame) a celda de grilla, comprobando límites.</summary>
        private bool CeldaDesdeCursorMundo(out Vector2Int cell)
        {
            cell = _sim.WorldToCell(_cursorWorld);
            return CellGrid.InBounds(cell.x, cell.y);
        }

        // ---------------------------------------------------------------------------------
        // Bloqueo de material (fix playtest 10, reporte 8). Ver doc de clase.
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Al pulsar aspirar: fija qué material entra en el frasco durante TODA
        /// la pulsación. Si bajo el cursor no hay nada aspirable, busca el
        /// aspirable más cercano dentro del alcance con el mismo recorrido en
        /// anillos crecientes que usa <see cref="TickSuck"/> (misma sensación de
        /// "lo más próximo primero"). Si tampoco encuentra nada, no bloquea nada
        /// y esta pulsación no aspira — mismo silencio que ya tenía apuntar a una
        /// celda vacía sin nada alrededor, no hace falta inventar un aviso nuevo.
        /// </summary>
        private void BloquearMaterialBajoElCursor(Vector2Int cursor)
        {
            byte bajoElCursor = (byte)_sim.SampleMaterial(cursor.x, cursor.y);
            if (EsAspirable(bajoElCursor))
            {
                _lockedMaterial = bajoElCursor;
                _hasLockedMaterial = true;
                return;
            }

            Vector2Int apprenticeCell = _sim.WorldToCell(transform.position);
            float reachCellsSq = ReachCellsSq();

            for (int r = 0; r <= SuckRadius; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    int y = cursor.y + dy;
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dy * dy)) != r) continue;

                        int x = cursor.x + dx;
                        if (!CellGrid.InBounds(x, y)) continue;

                        int cdx = x - apprenticeCell.x, cdy = y - apprenticeCell.y;
                        if (cdx * cdx + cdy * cdy > reachCellsSq) continue;

                        byte m = (byte)_sim.SampleMaterial(x, y);
                        if (!EsAspirable(m)) continue;

                        _lockedMaterial = m;
                        _hasLockedMaterial = true;
                        return;
                    }
                }
            }

            _hasLockedMaterial = false;
            _lockedMaterial = MaterialId.Empty;
        }

        /// <summary>¿Es este material aspirable? Centraliza el MISMO filtro que ya aplicaba TickSuck (piedra nunca — es la arquitectura del taller; fuego nunca — quemaría el frasco) para que el bloqueo y el aspirado real jamás discrepen.</summary>
        private bool EsAspirable(byte matId)
        {
            if (matId == MaterialId.Empty || matId == MaterialId.Stone) return false;
            if (_sim.Universe.Get(matId).archetype == MaterialArchetype.Fire) return false;
            return true;
        }

        // ---------------------------------------------------------------------------------
        // Aspirar (LMB mantenido).
        // ---------------------------------------------------------------------------------
        private void TickSuck(Vector2Int cursor, bool indiscriminado)
        {
            Vector2Int apprenticeCell = _sim.WorldToCell(transform.position);
            float reachCellsSq = ReachCellsSq();

            // (playtest 3) Feedback explícito de los dos motivos por los que
            // "aspirar no hace nada": estar lejos o llevar el frasco lleno.
            int cdxCursor = cursor.x - apprenticeCell.x, cdyCursor = cursor.y - apprenticeCell.y;
            if (cdxCursor * cdxCursor + cdyCursor * cdyCursor > reachCellsSq)
            {
                SetFeedback("demasiado lejos — acércate");
                return;
            }
            if (_total >= Capacity)
            {
                SetFeedback("frasco lleno — vierte (clic der.) o vacía (Q)");
                return;
            }

            // Sin Shift, si esta pulsación no bloqueó ningún material (nada
            // aspirable bajo el cursor ni cerca al pulsar) no hay nada que
            // discriminar todavía: no se aspira nada este tick.
            if (!indiscriminado && !_hasLockedMaterial) return;

            int budget = SuckRatePerTick;

            // Anillos de distancia entera creciente desde el cursor: sensación de
            // "aspirado" que vacía primero las celdas más cercanas al centro.
            for (int r = 0; r <= SuckRadius && budget > 0; r++)
            {
                for (int dy = -r; dy <= r && budget > 0; dy++)
                {
                    int y = cursor.y + dy;
                    for (int dx = -r; dx <= r && budget > 0; dx++)
                    {
                        int d2 = dx * dx + dy * dy;
                        if (Mathf.RoundToInt(Mathf.Sqrt(d2)) != r) continue;

                        int x = cursor.x + dx;
                        if (!CellGrid.InBounds(x, y)) continue;

                        int cdx = x - apprenticeCell.x, cdy = y - apprenticeCell.y;
                        if (cdx * cdx + cdy * cdy > reachCellsSq) continue;

                        byte matId = (byte)_sim.SampleMaterial(x, y);
                        if (matId == MaterialId.Empty) continue;

                        // Filtro de aspirado (revisado en el playtest 3):
                        //  · PIEDRA: nunca. Es la arquitectura del taller (muros,
                        //    cubas, boca de la Tolva) — la queja "aspira las
                        //    barreras" no debe poder volver a pasar.
                        //  · FUEGO: nunca, y con aviso. Chupar una llama con un
                        //    frasco de cristal no aporta nada al juego y confunde;
                        //    es más legible que el fuego "te queme".
                        //  · Hielo y Cristal SÍ se aspiran aunque sean sólidos
                        //    estáticos: son materia que FABRICA el jugador y que
                        //    los encargos del Maestro piden entregar (cristal,
                        //    "algo helado"). Con el filtro antiguo por arquetipo
                        //    esos encargos eran literalmente imposibles.
                        if (matId == MaterialId.Stone) continue;
                        if (_sim.Universe.Get(matId).archetype == MaterialArchetype.Fire)
                        {
                            SetFeedback("¡el fuego te quemaría el frasco!");
                            continue;
                        }

                        // BLOQUEO DE MATERIAL (fix playtest 10, reporte 8): sin
                        // Shift, solo entra al frasco el material que quedó fijado
                        // al pulsar (ver BloquearMaterialBajoElCursor) — así
                        // "aspirar agua" ya no se lleva de paso unas pocas celdas
                        // de arena vecina sin querer.
                        if (!indiscriminado && matId != _lockedMaterial) continue;

                        // El frasco se lleva la TEMPERATURA con la materia (ver
                        // doc de la clase): es lo que hace posibles los encargos
                        // de frío y de calor.
                        _tempSum[matId] += _sim.SampleTempRaw(x, y);
                        _sim.Paint(x, y, 0, MaterialId.Empty);
                        _counts[matId]++;
                        _total++;
                        budget--;

                        if (_total >= Capacity) return;
                    }
                }
            }
        }

        // ---------------------------------------------------------------------------------
        // Verter (RMB mantenido).
        // ---------------------------------------------------------------------------------
        private void TickPour(Vector2Int cursor)
        {
            if (_total <= 0) { SetFeedback("frasco vacío — ASPIRA algo primero (clic izq.)"); return; }

            Vector2Int apprenticeCell = _sim.WorldToCell(transform.position);
            float reachCellsSq = ReachCellsSq();

            int cdxCursor = cursor.x - apprenticeCell.x, cdyCursor = cursor.y - apprenticeCell.y;
            if (cdxCursor * cdxCursor + cdyCursor * cdyCursor > reachCellsSq)
            {
                SetFeedback("demasiado lejos — acércate");
                return;
            }

            int budget = PourRatePerTick;

            // Materiales más "pesados" (mayor densidad) primero, como pide el diseño.
            for (int i = 0; i < _pourOrder.Length && budget > 0; i++)
            {
                byte matId = _pourOrder[i];
                if (_counts[matId] <= 0) continue;
                PourMaterial(matId, cursor, apprenticeCell, reachCellsSq, PourRadius, ref budget);
            }
        }

        private void PourMaterial(byte matId, Vector2Int cursor, Vector2Int apprenticeCell, float reachCellsSq, int radius, ref int budget)
        {
            for (int r = 0; r <= radius && budget > 0 && _counts[matId] > 0; r++)
            {
                for (int dy = -r; dy <= r && budget > 0 && _counts[matId] > 0; dy++)
                {
                    int y = cursor.y + dy;
                    for (int dx = -r; dx <= r && budget > 0 && _counts[matId] > 0; dx++)
                    {
                        int d2 = dx * dx + dy * dy;
                        if (Mathf.RoundToInt(Mathf.Sqrt(d2)) != r) continue;

                        int x = cursor.x + dx;
                        if (!CellGrid.InBounds(x, y)) continue;

                        int cdx = x - apprenticeCell.x, cdy = y - apprenticeCell.y;
                        if (cdx * cdx + cdy * cdy > reachCellsSq) continue;

                        if (_sim.SampleMaterial(x, y) != MaterialId.Empty) continue;

                        // Se restituye la temperatura media guardada: el hielo
                        // sale frío y la arena de la placa ardiente sale ardiendo.
                        byte tempRaw = TempMediaDe(matId);
                        _sim.PaintCell(x, y, matId, tempRaw);
                        _tempSum[matId] -= tempRaw;
                        if (_tempSum[matId] < 0) _tempSum[matId] = 0;
                        _counts[matId]--;
                        _total--;
                        budget--;
                    }
                }
            }
        }

        // ---------------------------------------------------------------------------------
        // Vaciar de golpe (Q / botón central).
        // ---------------------------------------------------------------------------------
        private void DumpAll(Vector2Int cursor)
        {
            if (_total <= 0) return;

            Vector2Int apprenticeCell = _sim.WorldToCell(transform.position);
            float reachCellsSq = ReachCellsSq();
            int cdx0 = cursor.x - apprenticeCell.x, cdy0 = cursor.y - apprenticeCell.y;
            if (cdx0 * cdx0 + cdy0 * cdy0 > reachCellsSq) { SetFeedback("demasiado lejos — acércate"); return; }

            for (int i = 0; i < _pourOrder.Length; i++)
            {
                byte matId = _pourOrder[i];
                if (_counts[matId] <= 0) continue;
                int budget = int.MaxValue;
                PourMaterial(matId, cursor, apprenticeCell, reachCellsSq, DumpRadius, ref budget);
            }

            // Vaciado instantáneo garantizado: lo que no cupo en celdas vacías cercanas se pierde.
            ClearFlask();
        }

        private void ClearFlask()
        {
            for (int i = 0; i < _pourOrder.Length; i++)
            {
                _counts[_pourOrder[i]] = 0;
                _tempSum[_pourOrder[i]] = 0;
            }
            _total = 0;
        }

        private float ReachCellsSq()
        {
            float reachCells = ReachWorld / SimRenderer.CellWorldSize;
            return reachCells * reachCells;
        }

        // ---------------------------------------------------------------------------------
        // Visual: un pequeño punto de color en CarryAnchor con lo que se lleva.
        // ---------------------------------------------------------------------------------
        private void UpdateCarryVisual()
        {
            if (_carryVisual == null) return;

            if (_total <= 0)
            {
                _carryVisual.color = new Color(1f, 1f, 1f, 0f);
                return;
            }

            Vector3 anchor = _apprentice != null ? _apprentice.CarryAnchor : transform.position;
            _carryVisual.transform.position = new Vector3(anchor.x, anchor.y, anchor.z - 0.02f);

            float frac = Mathf.Clamp01((float)_total / Capacity);
            _carryVisual.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.28f, frac);

            float r = 0f, g = 0f, b = 0f;
            for (int i = 0; i < _pourOrder.Length; i++)
            {
                byte matId = _pourOrder[i];
                int c = _counts[matId];
                if (c <= 0) continue;
                Color32 col = _sim.Universe.Get(matId).baseColor;
                float wgt = (float)c / _total;
                r += col.r / 255f * wgt;
                g += col.g / 255f * wgt;
                b += col.b / 255f * wgt;
            }
            _carryVisual.color = new Color(r, g, b, 0.9f);
        }

        // ===================================================================
        // EL HAZ (fix playtest 10, reportes 7+8; el anillo de alcance que
        // vivía junto a él se retiró en el playtest 11, ver doc de clase punto 3).
        // Ver doc de clase para el porqué de este diseño. Todo generado por
        // código, cero Shader.Find (solo SpriteRenderer), creado UNA vez en
        // Build*Visual() — Update() solo mueve/rota/escala/tiñe lo ya creado.
        // ===================================================================

        private const int BeamSortingOrder = 40;   // por debajo del Cuerpo del aprendiz (50) y de sus alas (47-49): nunca lo tapa.

        private const float BeamThicknessWorld = 0.045f;
        private const float BeamAlpha = 0.34f;      // "alfa bajo": es una guía, no un rayo láser.
        private const float PulseAlpha = 0.85f;
        private const float PulseSizeWorld = 0.09f;
        private const float PulseSpeed = 1.6f;      // ciclos/segundo que recorre el haz.

        // Latón tenue de mundo (NO UiStyles.Oro, que es color de UI): el tono
        // neutro del haz cuando no hay un material que lo tiña (Shift/indiscriminado).
        private static readonly Color32 BrassBase = new Color32(168, 126, 58, 255);
        private static readonly Color32 BeamColorAviso = new Color32(219, 84, 71, 255); // fuera de alcance: tono de aviso cálido-rojo, mundo (no UiStyles.Peligro, que es de UI).

        private Transform _beamRoot;
        private SpriteRenderer _beamLineSr;
        private Transform _beamPulseTr;
        private SpriteRenderer _beamPulseSr;

        private void BuildBeamVisual()
        {
            var lineaSprite = CrearSpriteBlanco1x1("AlkahestFlaskHazLinea", new Vector2(0f, 0.5f));
            var pulsoSprite = CrearSpritePulso();

            var rootGo = new GameObject("FlaskHaz");
            rootGo.transform.SetParent(transform, false);
            _beamRoot = rootGo.transform;

            var lineaGo = new GameObject("Linea");
            lineaGo.transform.SetParent(_beamRoot, false);
            _beamLineSr = lineaGo.AddComponent<SpriteRenderer>();
            _beamLineSr.sprite = lineaSprite;
            _beamLineSr.sortingOrder = BeamSortingOrder;
            _beamLineSr.color = new Color(0f, 0f, 0f, 0f);

            var pulsoGo = new GameObject("Pulso");
            pulsoGo.transform.SetParent(_beamRoot, false);
            _beamPulseTr = pulsoGo.transform;
            _beamPulseSr = pulsoGo.AddComponent<SpriteRenderer>();
            _beamPulseSr.sprite = pulsoSprite;
            _beamPulseSr.sortingOrder = BeamSortingOrder + 1;
            _beamPulseSr.color = new Color(0f, 0f, 0f, 0f);
        }

        private static Sprite CrearSpriteBlanco1x1(string nombre, Vector2 pivot01)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = nombre };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), pivot01, 1f);
        }

        /// <summary>Punto suave (gradiente radial) para la "cuenta" que recorre el haz insinuando la dirección del flujo. Bilinear a propósito: es un brillo difuso, no una silueta que deba leerse nítida.</summary>
        private static Sprite CrearSpritePulso()
        {
            const int n = 16;
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - c) / c, dy = (y - c) / c;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float a = 1f - d;
                    a *= a; // caída más suave hacia el borde.
                    px[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "AlkahestFlaskHazPulso" };
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        /// <summary>Apaga el haz de golpe. Se llama en los primeros `return` de Update (título/paleta dev/escribiendo texto) para que ningún visual de mundo se quede "pegado" en pantalla mientras el frasco no actúa.</summary>
        private void OcultarVisualesDeMundo()
        {
            if (_beamLineSr != null) _beamLineSr.color = new Color(0f, 0f, 0f, 0f);
            if (_beamPulseSr != null) _beamPulseSr.color = new Color(0f, 0f, 0f, 0f);
        }

        private void UpdateWorldVisuals(bool wantSuck, bool wantPour)
        {
            // --- Haz: solo existe mientras se aspira/vierte DE VERDAD (ver doc de
            // clase, punto 2) -- fuera de esos momentos sería justo el ruido
            // visual permanente del que ya se quejó el jugador en otro playtest. ---
            bool aspirando = wantSuck && _hasCursorWorld;
            bool virtiendo = wantPour && _hasCursorWorld && _total > 0;
            bool hazActivo = (aspirando && (ModoIndiscriminado || _hasLockedMaterial)) || virtiendo;

            if (!hazActivo || _apprentice == null || _beamRoot == null)
            {
                if (_beamLineSr != null) _beamLineSr.color = new Color(0f, 0f, 0f, 0f);
                if (_beamPulseSr != null) _beamPulseSr.color = new Color(0f, 0f, 0f, 0f);
                return;
            }

            Vector3 origen = _apprentice.CarryAnchor;
            Vector3 alcanceOrigen = transform.position;
            Vector3 delta = _cursorWorld - alcanceOrigen; delta.z = 0f;
            float distDesdeAprendiz = delta.magnitude;
            bool fueraDeAlcance = distDesdeAprendiz > ReachWorld;

            // El haz se CORTA en el borde del alcance en vez de seguir hasta el
            // cursor: así "fuera de alcance" se nota en el propio gesto de
            // apuntar, no solo al soltar el botón sin que pasara nada.
            Vector3 destino = fueraDeAlcance
                ? alcanceOrigen + delta.normalized * ReachWorld
                : _cursorWorld;

            Vector3 tramo = destino - origen; tramo.z = 0f;
            float largo = tramo.magnitude;
            if (largo < 0.0005f)
            {
                if (_beamLineSr != null) _beamLineSr.color = new Color(0f, 0f, 0f, 0f);
                if (_beamPulseSr != null) _beamPulseSr.color = new Color(0f, 0f, 0f, 0f);
                return;
            }

            float anguloDeg = Mathf.Atan2(tramo.y, tramo.x) * Mathf.Rad2Deg;
            _beamRoot.position = origen;
            _beamRoot.rotation = Quaternion.Euler(0f, 0f, anguloDeg);
            _beamLineSr.transform.localScale = new Vector3(largo, BeamThicknessWorld, 1f);

            Color32 colorBase = fueraDeAlcance ? BeamColorAviso : ColorDelHaz(aspirando);
            _beamLineSr.color = new Color(colorBase.r / 255f, colorBase.g / 255f, colorBase.b / 255f, BeamAlpha);

            // Pulso: recorre el haz insinuando la dirección del flujo -- HACIA
            // el frasco al aspirar, HACIA fuera al verter (ver doc de clase).
            float ciclo = Mathf.Repeat(Time.time * PulseSpeed, 1f);
            float t = aspirando ? 1f - ciclo : ciclo;
            _beamPulseTr.localPosition = new Vector3(t * largo, 0f, -0.01f);
            _beamPulseTr.localScale = Vector3.one * PulseSizeWorld;
            _beamPulseSr.color = new Color(colorBase.r / 255f, colorBase.g / 255f, colorBase.b / 255f, PulseAlpha);
        }

        /// <summary>Color del haz: el material bloqueado al aspirar, el dominante del frasco al verter, o el latón neutro de mundo si no hay ninguno concreto que mostrar (Shift/frasco vacío de un color puro).</summary>
        private Color32 ColorDelHaz(bool aspirando)
        {
            if (aspirando)
            {
                // ModoIndiscriminado (Shift) se comprueba PRIMERO: si a mitad de
                // una pulsación con material bloqueado el jugador mantiene Shift,
                // TickSuck ya ignora el bloqueo (aspira todo) -- el haz debe
                // reflejar eso, no seguir tiñéndose del material que se dejó de
                // discriminar.
                if (ModoIndiscriminado) return BrassBase;
                if (_hasLockedMaterial) return _sim.Universe.Get(_lockedMaterial).baseColor;
                return BrassBase;
            }
            byte dominante = MaterialDominante();
            return dominante != MaterialId.Empty ? _sim.Universe.Get(dominante).baseColor : BrassBase;
        }
    }
}
