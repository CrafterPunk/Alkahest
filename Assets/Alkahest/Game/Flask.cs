using System.Collections.Generic;
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

        // ---------------------------------------------------------------------------------
        // "Ya lo sabes" (fix playtest 7): "el mensaje de que no tengo algo en el
        // frasco... también estorba y es cansado después de que ya lo sabes; a
        // veces solo clickeas y te sale ese mensaje por todo el mapa". El fallo
        // (lejos, vacío, lleno...) se repite constantemente porque el jugador
        // clickea sin pensarlo — pero el TEXTO solo aporta la primera vez.
        //
        // Registro por TIPO de mensaje (clave = el propio texto, así cada aviso
        // "aprende" por separado: que ya sepas "frasco vacío" no calla "demasiado
        // lejos"). Dictionary creado UNA sola vez, nunca dentro de OnGUI/Update.
        // Las 3 primeras veces que se dispara un texto concreto se muestra
        // normal; a partir de ahí NO se vuelve a mostrar como texto — pero la
        // acción fallida sigue teniendo respuesta: un destello corto (~0.15 s,
        // ver DestelloIntensidad) del panel del frasco en UiStyles.Aviso,
        // pintado por FlaskHud. Nada de texto, nada de globos por el mapa; y
        // "hice clic y no pasó nada" deja de ser cierto porque el destello SÍ
        // ocurre.
        //
        // Se reinicia solo: es un campo de INSTANCIA (no PlayerPrefs), y Flask
        // no sobrevive a un reload de escena (RestartRun recarga la escena
        // entera), así que cada partida nueva arranca con el registro vacío sin
        // necesidad de limpiarlo a mano.
        private const int VecesAntesDeCallar = 3;
        private const float DestelloDuracion = 0.15f;

        private readonly Dictionary<string, int> _vecesMostrado = new Dictionary<string, int>();
        private float _destelloUntil;

        /// <summary>Intensidad 0..1 del destello silencioso del panel del frasco (0 = sin destello). Lo pinta FlaskHud.</summary>
        public float DestelloIntensidad => Mathf.Clamp01((_destelloUntil - Time.time) / DestelloDuracion);

        /// <summary>
        /// `repetitivo` = true (por defecto) para los regaños de una acción
        /// fallida ("demasiado lejos", "frasco vacío"...): esos son los que se
        /// callan tras <see cref="VecesAntesDeCallar"/> repeticiones DEL MISMO
        /// texto. `repetitivo` = false para información real de algo que SÍ
        /// ocurrió (p.ej. una confirmación): esos se muestran siempre.
        /// </summary>
        private void SetFeedback(string msg, bool repetitivo = true)
        {
            if (repetitivo)
            {
                int veces = _vecesMostrado.TryGetValue(msg, out int v) ? v : 0;
                if (veces >= VecesAntesDeCallar)
                {
                    _destelloUntil = Time.time + DestelloDuracion;
                    return; // ya lo sabe: nada de texto, solo el destello mudo.
                }
                _vecesMostrado[msg] = veces + 1;
            }

            Feedback = msg;
            FeedbackUntil = Time.time + 1.5f;
        }

        /// <summary>Mismo canal de feedback, para que otros aparatos (la estantería de redomas) avisen junto al cursor y no en su propia esquina.</summary>
        public void Avisar(string msg, bool repetitivo = true) => SetFeedback(msg, repetitivo);

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

        private SpriteRenderer _carryVisual;

        public int Total => _total;
        public int GetCount(byte matId) => _counts[matId];

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
            if (DayCycle.InputLocked) return; // M4: título/intro/fin de día/pantalla final congelan el frasco.

            var mouse = Mouse.current;
            var kb = Keyboard.current;
            // (fix playtest 2) Con la paleta dev (F3) abierta, el pincel manda: el frasco no actúa.
            if (Alkahest.Dev.DevPalette.IsOpen) return;

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

            _hasCursor = TryGetCursorCell(out _cursorCell);

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
                    if (wantSuck) TickSuck(_cursorCell);
                    else if (wantPour) TickPour(_cursorCell);
                }
                _accumulator -= TickDt;
                steps++;
            }
            if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;

            UpdateCarryVisual();
        }

        private bool TryGetCursorCell(out Vector2Int cell)
        {
            cell = default;
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return false;

            Vector2 screenPos = mouse.position.ReadValue();
            var ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var groundPlane = new Plane(Vector3.forward, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float enter)) return false;

            Vector3 world = ray.GetPoint(enter);
            cell = _sim.WorldToCell(world);
            return CellGrid.InBounds(cell.x, cell.y);
        }

        // ---------------------------------------------------------------------------------
        // Aspirar (LMB mantenido).
        // ---------------------------------------------------------------------------------
        private void TickSuck(Vector2Int cursor)
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
    }
}
