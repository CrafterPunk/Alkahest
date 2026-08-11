using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>Transformaciones "notables" que el aprendiz puede haber presenciado, una por SimEventType.</summary>
    [Flags]
    public enum WitnessFlags : byte
    {
        None = 0,
        Arder = 1 << 0,       // SimEventType.Ignite
        Cristalizar = 1 << 1, // SimEventType.Crystallize
        Crecer = 1 << 2,      // SimEventType.Grow
        Disolverse = 1 << 3,  // SimEventType.Dissolve
        Hervir = 1 << 4,      // SimEventType.Boil
        Congelarse = 1 << 5,  // SimEventType.Freeze
    }

    /// <summary>
    /// "Qué sabe el aprendiz" sobre cada material del universo, por
    /// materialId: si lo ha descubierto, qué nombre le ha puesto (null =
    /// "???" todavía) y qué transformaciones le ha visto sufrir.
    ///
    /// Descubrimiento: cualquier material que entre en el Frasco (sondeo de
    /// los conteos de <see cref="Flask"/> cada <see cref="FlaskPollInterval"/>
    /// segundos) O que el jugador mantenga bajo el cursor
    /// <see cref="HoverDiscoverSeconds"/> segundos seguidos.
    ///
    /// Presenciar: se consume el ring buffer de eventos notables de
    /// SimStepper cada frame (con un "lastSeenHead" propio, igual que
    /// recomienda su doc-comment), traduciendo cada SimEventType al
    /// material FUENTE del evento. Nota: "dentro de la vista de cámara" es
    /// trivialmente cierto siempre en esta build (una única cámara fija
    /// ortográfica que encuadra todo el nivel), así que no se comprueba.
    /// </summary>
    public sealed class SubstanceKnowledge : MonoBehaviour
    {
        private const float FlaskPollInterval = 0.5f;
        private const float HoverDiscoverSeconds = 1f;

        private AlkahestSim _sim;
        private Flask _flask;

        private readonly bool[] _discovered = new bool[MaterialId.Count];
        private readonly string[] _playerName = new string[MaterialId.Count];
        private readonly WitnessFlags[] _witness = new WitnessFlags[MaterialId.Count];

        private float _flaskPollTimer;
        private int _lastEventHead;

        private byte _hoverMatId;
        private float _hoverTimer;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, Flask flask)
        {
            _sim = sim;
            _flask = flask;
        }

        public bool EsDescubierto(byte matId) => matId < MaterialId.Count && _discovered[matId];

        /// <summary>Nombre puesto por el jugador, o "???" si todavía no se ha bautizado (o el id es inválido).</summary>
        public string NombreDe(byte matId)
        {
            if (matId >= MaterialId.Count) return "???";
            return _playerName[matId] ?? "???";
        }

        /// <summary>Pone/quita el nombre de un material. Nombre vacío o solo espacios equivale a "olvidarlo" (vuelve a mostrar "???").</summary>
        public void Bautizar(byte matId, string nombre)
        {
            if (matId >= MaterialId.Count) return;
            _playerName[matId] = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();
            _discovered[matId] = true; // bautizar implica conocerlo.
        }

        public WitnessFlags WitnessOf(byte matId) => matId < MaterialId.Count ? _witness[matId] : WitnessFlags.None;

        public bool Vio(byte matId, WitnessFlags flag) => (WitnessOf(matId) & flag) != 0;

        public int CountDiscovered()
        {
            int n = 0;
            for (int m = 1; m < MaterialId.Count; m++) if (_discovered[m]) n++;
            return n;
        }

        public int CountNamed()
        {
            int n = 0;
            for (int m = 1; m < MaterialId.Count; m++) if (_playerName[m] != null) n++;
            return n;
        }

        private void Update()
        {
            if (_sim == null || _sim.Stepper == null) return;

            PollFlask();
            PollHover();
            ConsumeEvents();
        }

        private void PollFlask()
        {
            if (_flask == null) return;

            _flaskPollTimer += Time.deltaTime;
            if (_flaskPollTimer < FlaskPollInterval) return;
            _flaskPollTimer = 0f;

            for (int m = 1; m < MaterialId.Count; m++)
            {
                if (_discovered[m]) continue;
                if (_flask.GetCount((byte)m) > 0) _discovered[m] = true;
            }
        }

        private void PollHover()
        {
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null || _sim == null)
            {
                _hoverTimer = 0f;
                return;
            }

            Vector2 screenPos = mouse.position.ReadValue();
            var ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var groundPlane = new Plane(Vector3.forward, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float enter))
            {
                _hoverTimer = 0f;
                return;
            }

            Vector3 world = ray.GetPoint(enter);
            Vector2Int cell = _sim.WorldToCell(world);
            if (!CellGrid.InBounds(cell.x, cell.y))
            {
                _hoverTimer = 0f;
                return;
            }

            byte matId = (byte)_sim.SampleMaterial(cell.x, cell.y);
            if (matId == MaterialId.Empty || matId == MaterialId.Stone)
            {
                _hoverTimer = 0f;
                return;
            }

            if (matId != _hoverMatId)
            {
                _hoverMatId = matId;
                _hoverTimer = 0f;
            }

            _hoverTimer += Time.deltaTime;
            if (_hoverTimer >= HoverDiscoverSeconds)
            {
                _discovered[matId] = true;
            }
        }

        private void ConsumeEvents()
        {
            var stepper = _sim.Stepper;
            var events = stepper.Events;
            int head = stepper.EventHead;

            int i = _lastEventHead;
            int steps = 0;
            while (i != head && steps < SimStepper.EventBufferSize)
            {
                var e = events[i];
                ApplyWitness(e.type, e.matId);
                i = (i + 1) & (SimStepper.EventBufferSize - 1);
                steps++;
            }
            _lastEventHead = head;
        }

        private void ApplyWitness(SimEventType type, byte matId)
        {
            if (matId >= MaterialId.Count) return;

            WitnessFlags flag;
            switch (type)
            {
                case SimEventType.Ignite: flag = WitnessFlags.Arder; break;
                case SimEventType.Boil: flag = WitnessFlags.Hervir; break;
                case SimEventType.Freeze: flag = WitnessFlags.Congelarse; break;
                case SimEventType.Crystallize: flag = WitnessFlags.Cristalizar; break;
                case SimEventType.Grow: flag = WitnessFlags.Crecer; break;
                case SimEventType.Dissolve: flag = WitnessFlags.Disolverse; break;
                default: return;
            }

            _witness[matId] |= flag;
        }

        /// <summary>Chip corto en español para una única flag de presenciado (usado por JournalHud).</summary>
        public static string ChipLabel(WitnessFlags flag)
        {
            switch (flag)
            {
                case WitnessFlags.Arder: return "arde";
                case WitnessFlags.Cristalizar: return "cristaliza";
                case WitnessFlags.Crecer: return "crece";
                case WitnessFlags.Disolverse: return "se disuelve";
                case WitnessFlags.Hervir: return "hierve";
                case WitnessFlags.Congelarse: return "se congela";
                default: return "";
            }
        }
    }
}
