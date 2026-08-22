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

        // -----------------------------------------------------------------
        // (RONDA 69d, pedido directo de Cesar) LOS AVISOS DE VACÍO/LLENO SE
        // CANSAN: "es cansadísimo que aparece cuando quieres tirar más
        // material y ya está vacío". Cada aviso vive por EPISODIOS (mantener
        // el botón refresca el mismo episodio ~2.5s, no gasta cupo) y tras
        // AvisosMax episodios se CALLA para siempre en esta sesión -- el
        // jugador nuevo lo ve un par de veces al inicio (a prueba de burros,
        // como pidió en la fundación) y el veterano deja de sufrirlo. Si
        // Cesar prefiere apagarlo del todo, AvisosMax=0 y listo.
        // -----------------------------------------------------------------
        private const int AvisosMax = 2;
        private int _avisosVacio, _avisosLleno;
        private float _avisoVacioEpisodioHasta, _avisoLlenoEpisodioHasta;

        private void AvisarLimitado(ref int contador, ref float episodioHasta, string msg)
        {
            if (Time.time <= episodioHasta) { SetFeedback(msg); return; } // mismo episodio: refresca sin gastar cupo.
            if (contador >= AvisosMax) return;
            contador++;
            episodioHasta = Time.time + 2.5f;
            SetFeedback(msg);
        }

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
            BuildMotasVisual(); // (ronda 69c) el juice de aspirar/verter -- ver el bloque EL VIAJE DE LA MATERIA.
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
            //
            // COSTURA CON EL ENCARGO A -- "B7" (playtest 55, ronda B): investigado a fondo
            // "el haz del frasco desapareció y no volvió" (reporte de Cesar). VEREDICTO: el
            // código del haz (esta guarda, UpdateWorldVisuals, BuildBeamVisual...) está
            // INTACTO desde que se retiró el anillo en el playtest 11 -- verificado contra
            // `git log`/tamaño en bytes de Game/Flask.cs commit a commit (regla 26): la única
            // caída de tamaño de todo el historial es la del propio playtest 11 (42790 ->
            // 38049 bytes), la retirada DOCUMENTADA del anillo (ver el punto 3 del docblock de
            // esta clase); de ahí en adelante el archivo solo crece. El orden de capas tampoco
            // cambió: BeamSortingOrder=40/41 sigue por debajo de las alas (47-49) y el cuerpo
            // (50) del aprendiz (Game/ApprenticeController.cs), por encima del mundo (-5,
            // Sim/SimRenderer.cs). Descartadas (b) regresión de código y (c) orden de capas.
            //
            // CAUSA REAL, (a): esta guarda es CORRECTA (regla 12 -- el frasco no puede actuar
            // con una vitrina modal en pantalla) y NO se toca. El problema es que
            // `AlbumReal.Abierto` puede quedarse ATASCADO en `true` PARA SIEMPRE, matando el
            // haz (y el resto de visuales de mundo, vía esta misma línea) en TODA sesión
            // futura hasta reiniciar el .exe entero. Mecanismo confirmado leyendo
            // Game/AlkahestGameBootstrap.cs y Game/AlbumReal.cs (sin editarlos, fuera de
            // alcance de este encargo): `AlbumReal.Abierto` es una propiedad ESTÁTICA que cada
            // instancia de `AlbumReal.Update()` recalcula como `_visible || _fichaAbierta`
            // cada frame -- se autocorrige SOLO SI esa instancia sigue viva y corriendo. La
            // fuga YA DOCUMENTADA en la sección "Playtest 53" de docs/HANDOFF.md ("host que
            // sale de la sesión y re-hostea SIN cerrar el juego") es exactamente el
            // escenario: `AlkahestGameBootstrap._spawned` (bootstrap.cs:103) no se resetea al
            // desconectar, así que un segundo `TrySpawnRed()` en el MISMO proceso (mismo
            // patrón de reporte que Cesar probó con su amigo) se salta ENTERO el bloque de
            // spawn -- incluida `SpawnAlbumReal` -- y deja viva, huérfana y sin nadie que la
            // reinicie, la instancia VIEJA de `AlbumReal` de la sesión anterior. Si esa
            // instancia tenía `_fichaAbierta`/`_visible` en `true` en el instante exacto de
            // salir (plausible: una ficha de descubrimiento se abre SOLA, pt50), su
            // `Update()` (que SÍ sigue corriendo, el GameObject nunca se destruye) mantiene
            // `AlbumReal.Abierto` en `true` para siempre -- invisible, porque su propio
            // `OnGUI` puede estar devolviendo temprano (`_sim`/`_knowledge` de la sesión
            // vieja), así que no hay ni panel en pantalla que el jugador pueda cerrar. El
            // WORKAROUND que ya documentaba el playtest 53 ("tras salir, VOLVER AL TÍTULO y
            // reentrar antes de re-hostear") estaba además ROTO por B1 en la build de reparto
            // (`SceneManager.LoadScene("AlkahestLab")` por nombre, ausente del build MULTI) --
            // arreglado esta misma ronda en Game/DayCycle.cs, así que el escape real vuelve a
            // funcionar, pero la fuga en sí sigue viva.
            //
            // NO SE TOCA `Game/AlbumReal.cs` (propiedad del Encargo A, fuera de mi lista) ni
            // se debilita esta guarda (regla 12 es innegociable) -- el fix real es que
            // `AlbumReal`/`AlkahestGameBootstrap` se limpien a sí mismos al salir de una
            // sesión (mismo trabajo pendiente que `_spawned`/MaquinaSync del playtest 53).
            // Reportado al director como costura entre A y B para cerrarlo junto con esa
            // deuda ya conocida.
            if (JournalHud.Abierto || AlbumReal.Abierto) { OcultarVisualesDeMundo(); return; } // (integración pt50, regla 12) AlbumReal.Abierto: la ficha modal de descubrimiento (ENCARGO F) se abre sola -- aspirar/verter con ella en pantalla era el hueco reportado.

            // (playtest 16) EL CINCEL ES UN MODO, NO OTRO BOTÓN. Con la tecla C el
            // aprendiz cambia lo que lleva en la mano: o el frasco o el cincel,
            // nunca los dos. Sin esta guarda, tallar justo al borde de un charco
            // haría que el frasco empezara a aspirarlo a la vez que se pica la
            // piedra — dos herramientas actuando sobre el mismo clic. Ver la
            // cabecera de Game/Cincel.cs, que documenta el reparto de controles.
            if (Cincel.ModoActivo) { OcultarVisualesDeMundo(); return; }

            // (playtest 19) LA MUDANZA TAMBIÉN ES UN MODO -- misma regla que el
            // Cincel de arriba, y esta guarda es justo la que Cincel.cs dejó
            // documentada como pendiente ("Game/Flask.cs es de solo lectura en
            // este encargo... Pendiente para una ronda futura con propiedad de
            // Flask.cs") pero para Mudanza, no para Cincel -- ESA guarda (la de
            // arriba, Cincel.ModoActivo) ya se añadió en una ronda anterior a
            // esta y sigue vigente sin cambios. REGLA DE LOS TRES MODOS
            // EXCLUSIVOS: Frasco / Cincel / Mudanza nunca responden dos a la
            // vez. Frasco cede a los otros dos (aquí). Mudanza cede al Cincel
            // (Game/Mudanza.cs, propio). El Cincel NO cede a Mudanza todavía --
            // Game/Cincel.cs sigue siendo de solo lectura en este encargo; ver
            // el docblock de Game/Mudanza.cs, sección "LOS TRES MODOS
            // EXCLUSIVOS", para el hueco que queda documentado.
            if (Mudanza.ModoActivo) { OcultarVisualesDeMundo(); return; }

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

            // (ronda 69d) ANTICIPACIÓN -> ACCIÓN -> RESOLUCIÓN (flancos de
            // los botones; ver el bloque de constantes AnticipacionSeg):
            //  · arranque de succión: inhale (el frasco se encoge un instante)
            //    y las motas esperan la ventana -- la celda YA se aspira.
            //  · soltar succión: pop de cierre (traga y asienta).
            //  · arranque de vertido: el tarro se ladea hacia el cursor (la
            //    inclinación ES la anticipación) y las motas esperan.
            //  · cortar el chorro: asentamiento suave.
            bool aspirandoAhora = wantSuck && _hasCursor;
            bool vertiendoAhora = !wantSuck && wantPour && _hasCursor && _total > 0;
            if (aspirandoAhora && !_aspirabaPrev) { _motasDesde = Time.time + AnticipacionSeg; _pulsoVel += InhaleImpulso; _ticksSinMover = 0; }
            if (!aspirandoAhora && _aspirabaPrev) _pulsoVel += SettleAspirar;
            if (vertiendoAhora && !_vertiaPrev) { _motasDesde = Time.time + AnticipacionSeg; _ticksSinMover = 0; }
            if (!vertiendoAhora && _vertiaPrev) _pulsoVel += SettleVerter;
            _aspirabaPrev = aspirandoAhora;
            _vertiaPrev = vertiendoAhora;
            _vertiendoVisual = vertiendoAhora;

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
                    if (wantSuck)
                    {
                        TickSuck(_cursorCell, indiscriminado);
                        // (ronda 69e) contabilidad del flujo para la retracción del haz.
                        if (_celdasJuiceTick > 0) _ticksSinMover = 0; else _ticksSinMover++;
                    }
                    else if (wantPour)
                    {
                        TickPour(_cursorCell);
                        if (_celdasJuiceTick > 0) _ticksSinMover = 0; else _ticksSinMover++;
                    }
                }
                _accumulator -= TickDt;
                steps++;
            }
            if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;

            UpdateCarryVisual();
            UpdateWorldVisuals(wantSuck, wantPour);
            ActualizarJuice(Time.deltaTime); // (ronda 69c) motas + muelle del pop. En los return tempranos lo llama OcultarVisualesDeMundo.
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
            _celdasJuiceTick = 0; // (ronda 69e) AL PRINCIPIO, antes de toda guarda: el conteo de "este tick movió algo" alimenta la retracción del haz (ver Update).
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
                AvisarLimitado(ref _avisosLleno, ref _avisoLlenoEpisodioHasta, "frasco lleno — vierte (clic der.) o vacía (Q)");
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

                        // (ronda 69c) 1 mota por cada ~CeldasPorMota celdas comidas,
                        // nacida en la PROPIA celda que acaba de desaparecer.
                        _celdasJuiceTick++;
                        if (_celdasJuiceTick % CeldasPorMota == 1) EmitirMota(x, y, matId, haciaFrasco: true);

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
            _celdasJuiceTick = 0; // (ronda 69e) al principio, antes de toda guarda -- ver TickSuck.
            if (_total <= 0) { AvisarLimitado(ref _avisosVacio, ref _avisoVacioEpisodioHasta, "frasco vacío — ASPIRA algo primero (clic izq.)"); return; }

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

            // (ronda 69c) Si este tick soltó materia de verdad, el frasco se
            // ENCOGE un pelín (impulso negativo del muelle): el gesto de
            // apretar el bote. Una vez por tick, no por celda.
            if (budget < PourRatePerTick) _pulsoVel += PulsoImpulsoVertido;
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

                        // (ronda 69c) mota de vertido: sale del frasco hacia la
                        // celda que acaba de nacer -- el chorro se VE viajar.
                        _celdasJuiceTick++;
                        if (_celdasJuiceTick % CeldasPorMota == 1) EmitirMota(x, y, matId, haciaFrasco: false);
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

            // (ronda 69c) El vaciado de golpe es el gesto más brusco del
            // frasco: ráfaga de motas (el pool de 24 la acota solo) y una
            // encogida fuerte del muelle -- se ve y se siente "soltarlo todo".
            _celdasJuiceTick = 0;
            _pulsoVel += PulsoImpulsoVertido * 3f;

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
            // (ronda 69c) El swatch respira con el muelle del juice: pop al
            // recibir una mota, encogida breve al verter. Ver ActualizarJuice.
            _carryVisual.transform.localScale = Vector3.one * (Mathf.Lerp(0.12f, 0.28f, frac) * (1f + _pulso));

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

        /// <summary>Apaga el haz de golpe. Se llama en los primeros `return` de Update (título/paleta dev/escribiendo texto) para que ningún visual de mundo se quede "pegado" en pantalla mientras el frasco no actúa. (ronda 69c) También avanza el juice: las motas en vuelo TERMINAN su viaje y se apagan aunque un modal congele el frasco -- si no, quedarían clavadas en el aire detrás del diario.</summary>
        private void OcultarVisualesDeMundo()
        {
            if (_beamLineSr != null) _beamLineSr.color = new Color(0f, 0f, 0f, 0f);
            if (_beamPulseSr != null) _beamPulseSr.color = new Color(0f, 0f, 0f, 0f);
            _vertiendoVisual = false; // (ronda 69d) modal en pantalla: el tarro vuelve a vertical.
            ActualizarJuice(Time.deltaTime);
        }

        // ===================================================================
        // (RONDA 69c) EL VIAJE DE LA MATERIA -- el juice de aspirar/verter.
        // Pedido de Cesar en el pt69: "es la actividad más recurrente del
        // juego y tiene que sentirse una delicia". Paquete elegido por él
        // (opciones 1+2+3 de la propuesta): motas en tránsito + frasco que
        // respira + sonido que cuenta el llenado (esto último vive en
        // Audio/DirectorDeAudio.cs, que ya observaba Flask.Total).
        //
        // LA IDEA (referencias: el Poltergust de Luigi's Mansion, la pistola
        // de terreno de Astroneer): hoy la materia se TELETRANSPORTA -- la
        // celda desaparece de la grilla y un contador sube. Las motas hacen
        // el viaje visible: al aspirar, cada puñado de celdas comidas suelta
        // una mota del COLOR del material que vuela en curva hasta la boca
        // del frasco, acelerando y encogiéndose (easeIn: la succión tira);
        // al verter, salen del frasco hacia el punto de vertido frenando al
        // llegar (easeOut: el chorro cae). Cuando una mota LLEGA al frasco,
        // el swatch da un "pop" (muelle _pulso) -- el frasco RECIBE.
        //
        // DISCIPLINA: capa 100% cosmética. Pool FIJO de sprites (cero allocs
        // por frame, regla de oro), la sim ni se entera (Paint/PaintCell
        // siguen siendo la única mutación), y el RNG es System.Random local
        // de presentación (mismo criterio documentado en
        // DirectorDeAudio._rngVariacion: nunca UnityEngine.Random, pero
        // tampoco necesita ser determinista entre sesiones -- no toca la
        // grilla). Si el pool se llena, la emisión se salta en silencio.
        // ===================================================================
        private const int MotasMax = 32; // (ronda 69e, "+25% de notoriedad") 24->32: sostiene la cadencia subida sin perder emisiones.
        private const int MotasOrdenDibujo = 42;      // sobre el haz (40/41), bajo las alas del imp (47+).
        // (ronda 69d, idea de Cesar: "en los últimos píxeles pasan por
        // delante del personaje/frente de la herramienta -- solo sorting,
        // engañar elegantemente al cerebro") El último ~22% del viaje hacia
        // el frasco (y el primero al salir) se dibuja DELANTE del cuerpo del
        // imp (50) y del tarro en mano (52), debajo del swatch de contenido
        // (60): la mota "cruza por delante" y desaparece EN la boca -- una
        // pizca de profundidad sin física extra.
        private const int MotasOrdenFrente = 53;
        private const float MotaTramoFrente = 0.22f;  // fracción del viaje pegada al frasco que se dibuja delante.
        // (ronda 69d, idea de Cesar: anticipación -> acción -> resolución)
        // Al EMPEZAR a aspirar o verter, las motas esperan ~100 ms mientras
        // el frasco hace su gesto de arranque (inhale / inclinación). SOLO
        // visual: las celdas se aspiran/vierten desde el primer tick, el
        // control no se retrasa ni un frame ("no metería delays reales que
        // hagan torpe el control", textual).
        private const float AnticipacionSeg = 0.10f;
        private const float InhaleImpulso = -2.1f;    // (69e: -1.6->-2.1) el frasco se ENCOGE al arrancar la succión (toma aire)...
        private const float SettleAspirar = 1.7f;     // (69e: 1.3->1.7) ...y al SOLTAR el botón, pop de cierre (traga y asienta).
        private const float SettleVerter = 1.0f;      // (69e: 0.8->1.0) cierre suave al cortar el chorro.
        private const float InclinacionMaxDeg = 14f;  // el tarro se LADEA hacia donde vierte (llega en ~65 ms: es la anticipación del chorro).
        private const float InclinacionVelDegSeg = 220f;
        // (ronda 69e, pedido de Cesar) EL HAZ RESPONDE AL FLUJO: "cuando
        // termine de aspirar todo de un elemento, la línea guía se debe
        // encoger hacia el frasco, para no seguir presionándole". Si el
        // tick de aspirar/verter lleva ~0.1s sin mover NI UNA celda (el
        // material bloqueado se agotó, el frasco está lleno/vacío, la zona
        // de vertido no tiene hueco...), el haz se RETRAE hacia el frasco
        // en ~0.25s -- la herramienta misma dice "ya está, suelta", sin
        // cartelito. En cuanto vuelve a fluir (o al re-pulsar), crece de
        // nuevo desde el frasco en ~80ms (lo que además le da al haz un
        // gesto de despliegue en cada arranque).
        private const float HazEncogerVelPorSeg = 4.5f;
        private const float HazCrecerVelPorSeg = 12f;
        private const int TicksSinMoverParaRetraer = 3; // ~0.1s de "no pasa nada" antes de encoger (evita parpadeo con flujos intermitentes).
        // (ronda 69e, "+25% de notoriedad" pedido por Cesar tras sentirlo:
        // "les falta un pelín más de notoriedad") motas más grandes, más
        // frecuentes, con viaje algo más largo y comba más generosa.
        private const float MotaDurMin = 0.20f;
        private const float MotaDurMax = 0.32f;
        private const float MotaTamano = 0.098f;      // ~1 celda de diámetro visual.
        private const int CeldasPorMota = 8;          // 1 mota por cada ~8 celdas movidas (30/tick aspirando = ~4 motas/tick).
        private const float PulsoResorte = 170f;      // muelle del "pop" del frasco.
        private const float PulsoAmortiguacion = 11f;
        private const float PulsoImpulsoLlegada = 2.8f;  // (69e: 2.2->2.8) pico ~ +21% de escala por mota recibida.
        private const float PulsoImpulsoVertido = -1.4f; // (69e: -1.1->-1.4) el frasco se ENCOGE al soltar (una vez por tick con vertido real).

        private SpriteRenderer[] _motaSr;
        private Vector3[] _motaDesde;
        private Vector3[] _motaHasta;
        private Vector2[] _motaComba;   // desvío lateral del punto de control de la curva.
        private float[] _motaT;
        private float[] _motaDur;
        private bool[] _motaViva;
        private bool[] _motaHaciaFrasco;
        private int _motaSiguiente;
        private int _celdasJuiceTick;   // celdas movidas en el tick en curso (espacia la emisión).
        private float _pulso, _pulsoVel;
        private float _motasDesde;      // Time.time antes del cual EmitirMota calla (ventana de anticipación).
        private bool _aspirabaPrev, _vertiaPrev; // flancos de los botones, para inhale/settle.
        private float _hazExtension;    // 0..1: fracción del haz desplegada desde el frasco (ronda 69e).
        private int _ticksSinMover;     // ticks consecutivos de aspirar/verter que no movieron ni una celda.
        private bool _vertiendoVisual;  // ¿hay chorro AHORA? (alimenta la inclinación; false en guardas modales).
        private float _inclinacion;     // grados actuales del tarro (suavizado hacia ±InclinacionMaxDeg).
        private readonly System.Random _rngJuice = new System.Random(unchecked(System.Environment.TickCount * 17 + 3));

        private void BuildMotasVisual()
        {
            var sprite = CrearSpritePulso(); // el mismo punto suave del haz: brillo difuso, no silueta.
            var rootGo = new GameObject("FlaskMotas");
            rootGo.transform.SetParent(transform, false);

            _motaSr = new SpriteRenderer[MotasMax];
            _motaDesde = new Vector3[MotasMax];
            _motaHasta = new Vector3[MotasMax];
            _motaComba = new Vector2[MotasMax];
            _motaT = new float[MotasMax];
            _motaDur = new float[MotasMax];
            _motaViva = new bool[MotasMax];
            _motaHaciaFrasco = new bool[MotasMax];

            for (int i = 0; i < MotasMax; i++)
            {
                var go = new GameObject("Mota" + i);
                go.transform.SetParent(rootGo.transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = MotasOrdenDibujo;
                sr.color = new Color(0f, 0f, 0f, 0f);
                _motaSr[i] = sr;
            }
        }

        /// <summary>Emite una mota desde/hacia la celda (cx,cy). El pool es round-robin SOLO sobre huecos libres: si las 24 están volando, esta emisión se pierde sin drama (es cosmética).</summary>
        private void EmitirMota(int cx, int cy, byte matId, bool haciaFrasco)
        {
            if (_motaSr == null) return;
            if (Time.time < _motasDesde) return; // (ronda 69d) ventana de anticipación: el gesto del frasco va primero, el flujo después.
            int idx = -1;
            for (int k = 0; k < MotasMax; k++)
            {
                int i = (_motaSiguiente + k) % MotasMax;
                if (!_motaViva[i]) { idx = i; break; }
            }
            if (idx < 0) return;
            _motaSiguiente = (idx + 1) % MotasMax;

            float c = SimRenderer.CellWorldSize;
            Vector3 celda = new Vector3((cx + 0.5f) * c, (cy + 0.5f) * c, -0.03f);
            Vector3 boca = _apprentice != null ? _apprentice.CarryAnchor : transform.position;
            boca.z = -0.03f;

            _motaDesde[idx] = haciaFrasco ? celda : boca;
            _motaHasta[idx] = haciaFrasco ? boca : celda;  // hacia el frasco el destino se re-lee VIVO cada frame (el imp se mueve); ver ActualizarJuice.
            // Comba lateral: perpendicular al trayecto, magnitud y lado al azar
            // -- es lo que convierte una línea en un "sorbo" con cuerpo.
            Vector3 tramo = _motaHasta[idx] - _motaDesde[idx];
            Vector2 perp = new Vector2(-tramo.y, tramo.x).normalized;
            float lado = _rngJuice.Next(0, 2) == 0 ? -1f : 1f;
            float comba = Mathf.Lerp(0.13f, 0.40f, (float)_rngJuice.NextDouble()); // (69e) arco más generoso.
            _motaComba[idx] = perp * (lado * comba);
            _motaT[idx] = 0f;
            _motaDur[idx] = Mathf.Lerp(MotaDurMin, MotaDurMax, (float)_rngJuice.NextDouble());
            _motaViva[idx] = true;
            _motaHaciaFrasco[idx] = haciaFrasco;

            Color32 col = _sim.Universe.Get(matId).baseColor;
            _motaSr[idx].color = new Color(col.r / 255f, col.g / 255f, col.b / 255f, 0.95f);
            _motaSr[idx].transform.position = _motaDesde[idx];
            _motaSr[idx].transform.localScale = Vector3.one * MotaTamano;
        }

        /// <summary>El corazón del juice, una vez por frame: avanza las motas por su curva, integra el muelle del "pop" y le pasa el pulso al tarro en mano (ApprenticeController.PulsoDelFrasco).</summary>
        private void ActualizarJuice(float dt)
        {
            // GUARDA DE ESTABILIDAD (cazada en la verificación de esta misma
            // ronda, no teórica): el muelle se integra con Euler explícito, y
            // con PulsoResorte=170 el método DIVERGE si dt >= ~0.15s
            // (dt·sqrt(k) > 2). Un frame con hitch (carga, GC, alt-tab) tiene
            // exactamente ese tamaño. Capar dt hace que en un hitch el juice
            // avance un pelín más lento -- invisible -- en vez de que el
            // frasco dé un latigazo entre los dos topes del clamp.
            if (dt > 0.05f) dt = 0.05f;

            // --- El muelle del pop (amortiguado, siempre vuelve a 0). ---
            _pulsoVel += (-PulsoResorte * _pulso - PulsoAmortiguacion * _pulsoVel) * dt;
            _pulso = Mathf.Clamp(_pulso + _pulsoVel * dt, -0.25f, 0.35f);
            if (_apprentice != null) _apprentice.PulsoDelFrasco(_pulso);

            // --- (ronda 69d) La inclinación del tarro al verter: se ladea
            // hacia el lado del cursor (llega a tope en ~65 ms, antes de la
            // primera mota) y vuelve sola a vertical al cortar el chorro. ---
            float inclinacionObjetivo = 0f;
            if (_vertiendoVisual && _hasCursorWorld && _apprentice != null)
                inclinacionObjetivo = _cursorWorld.x >= _apprentice.CarryAnchor.x ? -InclinacionMaxDeg : InclinacionMaxDeg;
            _inclinacion = Mathf.MoveTowards(_inclinacion, inclinacionObjetivo, InclinacionVelDegSeg * dt);
            if (_apprentice != null) _apprentice.InclinacionDelFrasco(_inclinacion);

            if (_motaSr == null) return;
            Vector3 boca = _apprentice != null ? _apprentice.CarryAnchor : transform.position;
            boca.z = -0.03f;

            for (int i = 0; i < MotasMax; i++)
            {
                if (!_motaViva[i]) continue;
                _motaT[i] += dt / _motaDur[i];
                if (_motaT[i] >= 1f)
                {
                    _motaViva[i] = false;
                    _motaSr[i].color = new Color(0f, 0f, 0f, 0f);
                    if (_motaHaciaFrasco[i]) _pulsoVel += PulsoImpulsoLlegada; // el frasco RECIBE: pop.
                    continue;
                }

                Vector3 a = _motaDesde[i];
                Vector3 b = _motaHaciaFrasco[i] ? boca : _motaHasta[i]; // destino vivo al aspirar: el frasco viaja con el imp.
                float t = _motaT[i];
                float e = _motaHaciaFrasco[i] ? t * t : 1f - (1f - t) * (1f - t); // succión ACELERA al llegar; vertido FRENA al caer.
                Vector3 m = (a + b) * 0.5f + (Vector3)_motaComba[i];
                Vector3 pos = Vector3.Lerp(Vector3.Lerp(a, m, e), Vector3.Lerp(m, b, e), e);
                _motaSr[i].transform.position = pos;

                float escala = _motaHaciaFrasco[i]
                    ? Mathf.Lerp(MotaTamano, MotaTamano * 0.45f, e)   // se encoge al entrar al frasco.
                    : Mathf.Lerp(MotaTamano * 0.55f, MotaTamano, e);  // crece al salir: gota que cae.
                _motaSr[i].transform.localScale = Vector3.one * escala;

                // (ronda 69d) El tramo pegado al frasco se dibuja DELANTE del
                // personaje y del tarro -- ver MotasOrdenFrente.
                int orden = _motaHaciaFrasco[i]
                    ? (t > 1f - MotaTramoFrente ? MotasOrdenFrente : MotasOrdenDibujo)
                    : (t < MotaTramoFrente ? MotasOrdenFrente : MotasOrdenDibujo);
                if (_motaSr[i].sortingOrder != orden) _motaSr[i].sortingOrder = orden;

                if (t > 0.85f) // desvanecer solo el último tramo (que el viaje se VEA entero).
                {
                    var col = _motaSr[i].color;
                    col.a = 0.95f * (1f - t) / 0.15f;
                    _motaSr[i].color = col;
                }
            }
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
                _hazExtension = 0f; // (ronda 69e) el próximo despliegue arranca DESDE el frasco.
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

            // (ronda 69e) LA RETRACCIÓN: si el flujo lleva ~0.1s seco (el
            // material se agotó, frasco lleno/vacío, sin hueco donde verter),
            // el haz se encoge hacia el frasco -- "ya está, suelta" dicho por
            // la herramienta, no por un cartelito. Crece rápido al (re)fluir.
            float extObjetivo = _ticksSinMover >= TicksSinMoverParaRetraer ? 0f : 1f;
            float extVel = extObjetivo > _hazExtension ? HazCrecerVelPorSeg : HazEncogerVelPorSeg;
            _hazExtension = Mathf.MoveTowards(_hazExtension, extObjetivo, extVel * Time.deltaTime);
            largo *= _hazExtension;

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
