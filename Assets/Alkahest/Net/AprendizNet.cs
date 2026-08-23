using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Alkahest.Game;
using Alkahest.Sim;

namespace Alkahest.Net
{
    /// <summary>
    /// EL AVATAR EN RED (playtest 28, POC multiplayer). Acompaña al
    /// <see cref="ApprenticeController"/> dentro del prefab de jugador que
    /// genera <c>Editor/AlkahestNetSceneBuilder.cs</c> y hace tres cosas:
    ///
    ///  1) EL COLOR (mandato explícito de Cesar: *"cada jugador se distingue
    ///     por el color del personaje"*). Un `NetworkVariable&lt;byte&gt;` que
    ///     SOLO escribe el servidor, con la paleta de cuatro de
    ///     <see cref="ColoresJugador"/>: 0 dorado (el anfitrión, que siempre
    ///     es el cliente 0 y por tanto el primero en spawnear), 1 azul cielo,
    ///     2 verde, 3 magenta. El servidor asigna el índice LIBRE más bajo, no
    ///     un contador incremental: así, si un invitado se va y vuelve otro,
    ///     recupera el hueco en vez de quedarse sin color en la quinta
    ///     conexión de una partida de cuatro.
    ///
    ///  2) EL REPARTO DE CONTROL. Patrón del template
    ///     (`FriendsLoop.Demo.PlayerController`: <c>enabled = IsOwner</c>),
    ///     aplicado componente a componente porque aquí el avatar lleva media
    ///     capa de juego encima (frasco, cincel, mudanza, conocimiento, HUD).
    ///     Matiz deliberado: el ApprenticeController de los avatares REMOTOS
    ///     NO se desactiva del todo — se le quita el mando
    ///     (<see cref="ApprenticeController.ControlDelJugador"/>) pero se le
    ///     deja animarse. Un componente desactivado dejaría a los otros
    ///     aprendices como calcomanías rígidas deslizándose por el taller: las
    ///     alas dejarían de batir justo en el personaje al que el jugador
    ///     mira para saber que hay alguien más ahí.
    ///
    ///  3) EL CABLEADO TARDÍO. En el invitado, el avatar puede spawnear ANTES
    ///     de que exista el mundo (el espejo no nace hasta que llega el
    ///     snapshot con la seed del anfitrión, ver <see cref="SimSync"/>).
    ///     Frasco/conocimiento/HUD necesitan un `AlkahestSim` con Universe, así
    ///     que el cableado se reintenta en Update hasta que lo haya — el mismo
    ///     patrón defensivo que ya usan `AlkahestGameBootstrap` y `DevPalette`.
    ///
    /// DIVISIÓN DE TRABAJO DEL POC (contrato): los invitados VUELAN, ASPIRAN y
    /// VIERTEN. El Cincel solo se activa en el avatar del ANFITRIÓN: opera
    /// sobre la mampostería registrada del taller, que únicamente existe en
    /// el host. No es una limitación de red, es dónde vive la máquina.
    ///
    /// (playtest 30, MÁQUINAS EN RED) LA MUDANZA YA NO ES SOLO DEL
    /// ANFITRIÓN -- Cesar: "lo ideal es poder mudarlas para que cada quien se
    /// organice como quiera". El invitado la lleva también, pero opera sobre
    /// RÉPLICAS visuales (Net/MaquinaSync.cs, Net/MaquinaReplica.cs), nunca
    /// sobre un aparato real: mover una réplica manda una solicitud por RPC
    /// que el anfitrión valida y ejecuta sobre la máquina de verdad. Ver el
    /// docblock de <see cref="MaquinaSync"/> para el protocolo completo.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(ApprenticeController))]
    public sealed class AprendizNet : NetworkBehaviour
    {
        /// <summary>
        /// LA PALETA DE CUATRO. Tonos elegidos para leerse a la vez contra la
        /// mampostería oscura del taller (`SimRenderer.BackgroundColor`, casi
        /// negro ciruela) y contra la materia saturada (agua, fuego, limo
        /// oliva), y para distinguirse entre sí también en la esquina del HUD
        /// a 12 px. Son TINTES: multiplican el sprite procedural del aprendiz
        /// (morado claro desaturado), no lo sustituyen — el imp sigue siendo
        /// el mismo personaje, con la librea de su jugador.
        /// </summary>
        public static readonly Color[] ColoresJugador =
        {
            new Color(1.00f, 0.82f, 0.35f), // 0 DORADO — el anfitrión
            new Color(0.45f, 0.76f, 1.00f), // 1 AZUL CIELO
            new Color(0.48f, 0.94f, 0.55f), // 2 VERDE
            new Color(0.98f, 0.45f, 0.86f), // 3 MAGENTA
        };

        /// <summary>Nombres de los cuatro colores, para el HUD de sesión.</summary>
        public static readonly string[] NombresColor = { "dorado", "azul cielo", "verde", "magenta" };

        /// <summary>El avatar de ESTE jugador (null hasta que spawnea el suyo).</summary>
        public static AprendizNet Local { get; private set; }

        /// <summary>
        /// Todos los avatares vivos, en orden de aparición. Registro propio en
        /// vez de un `FindObjectsByType` por frame: lo consulta el HUD de
        /// sesión cada OnGUI y el reparto de colores en cada spawn.
        /// </summary>
        public static readonly List<AprendizNet> Todos = new List<AprendizNet>();

        /// <summary>
        /// Índice de color del jugador (0..3). Lo escribe el SERVIDOR al
        /// spawnear; todos lo leen. Mismos permisos que el `DisplayName` de
        /// `FriendsLoop.Demo.PlayerIdentity`, que es el NetworkVariable de
        /// referencia de este proyecto.
        /// </summary>
        public readonly NetworkVariable<byte> IndiceColor =
            new NetworkVariable<byte>(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        /// <summary>Separación horizontal (unidades de mundo) entre los puntos de aparición de los cuatro aprendices, para que no nazcan uno dentro de otro.</summary>
        private const float SeparacionSpawn = 1.1f;

        private ApprenticeController _aprendiz;
        private Flask _frasco;
        private Cincel _cincel;
        private Mudanza _mudanza;
        private SubstanceKnowledge _conocimiento;
        private FlaskHud _hudFrasco;

        private bool _cableado;

        /// <summary>
        /// ¿Ya se le inyectó el AlkahestSim a las herramientas de este avatar?
        /// Lo consulta `AlkahestGameBootstrap.TrySpawnRed` antes de montar las
        /// máquinas del anfitrión: varias reciben el `SubstanceKnowledge` del
        /// avatar y el orden de Update entre los dos componentes no está
        /// garantizado.
        /// </summary>
        public bool Cableado => _cableado;

        private void Awake()
        {
            _aprendiz = GetComponent<ApprenticeController>();
            _frasco = GetComponent<Flask>();
            _cincel = GetComponent<Cincel>();
            _mudanza = GetComponent<Mudanza>();
            _conocimiento = GetComponent<SubstanceKnowledge>();
            _hudFrasco = GetComponent<FlaskHud>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!Todos.Contains(this)) Todos.Add(this);
            IndiceColor.OnValueChanged += AlCambiarColor;

            if (IsServer)
            {
                IndiceColor.Value = (byte)PrimerColorLibre();
            }

            AplicarTinteActual();

            if (IsOwner)
            {
                Local = this;
                ApprenticeController.AprendizLocal = _aprendiz;
                ColocarEnPuntoDeAparicion();

                // La escena MULTI no tiene ciclo de jornadas (ni Título, ni
                // reloj): sin esto, DayCycle.InputLocked se quedaría en `true`
                // — su valor inicial — y el frasco de este jugador ignoraría
                // todos los clics para siempre.
                DayCycle.ForzarDesbloqueoSesion();
            }
            else
            {
                // Avatar de OTRO jugador: nada de input local. Ver el punto 2
                // del docblock de la clase para por qué el controlador se
                // queda vivo y solo pierde el mando.
                if (_aprendiz != null) _aprendiz.ControlDelJugador = false;
                if (_frasco != null) _frasco.enabled = false;
                if (_cincel != null) _cincel.enabled = false;
                if (_mudanza != null) _mudanza.enabled = false;
                if (_conocimiento != null) _conocimiento.enabled = false;
                if (_hudFrasco != null) _hudFrasco.enabled = false;
            }
        }

        public override void OnNetworkDespawn()
        {
            IndiceColor.OnValueChanged -= AlCambiarColor;
            Todos.Remove(this);

            if (Local == this)
            {
                Local = null;
                if (ApprenticeController.AprendizLocal == _aprendiz) ApprenticeController.AprendizLocal = null;
            }

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (_cableado || !IsOwner || !IsSpawned) return;

            var sim = FindAnyObjectByType<AlkahestSim>();
            if (sim == null || sim.Universe == null || sim.Grid == null) return;

            Cablear(sim);
        }

        /// <summary>
        /// Inyecta el AlkahestSim en las herramientas del avatar local. Es
        /// EXACTAMENTE lo que hacía `AlkahestGameBootstrap.SpawnApprentice` en
        /// la escena de un jugador, con el mismo orden (el conocimiento antes
        /// que el HUD del frasco, que lo necesita para enseñar el nombre que
        /// el jugador le puso a cada sustancia).
        /// </summary>
        private void Cablear(AlkahestSim sim)
        {
            if (_frasco != null) _frasco.Init(sim);

            if (_conocimiento != null && _frasco != null) _conocimiento.Init(sim, _frasco);
            if (_hudFrasco != null && _frasco != null) _hudFrasco.Init(sim, _frasco, _conocimiento);

            // El Cincel sigue SOLO en el anfitrión (ver docblock de la
            // clase): tallar mampostería escribe en la sim autoritativa, que
            // el invitado no tiene.
            //
            // (playtest 30, MÁQUINAS EN RED — Net/MaquinaSync.cs) LA MUDANZA
            // YA NO: desde esta ronda el invitado también la lleva. No opera
            // sobre la sim ni sobre un aparato real -- opera sobre las
            // RÉPLICAS visuales que construye MaquinaSync
            // (Net/MaquinaReplica.cs), que implementan el mismo contrato
            // IMovible que HeatPlate/ChillStone/Dispenser/las cinco
            // estaciones. `Mudanza.Init` es genérico (solo guarda `_sim` y
            // construye su propia silueta de arrastre, ver Game/Mudanza.cs):
            // es exactamente seguro llamarlo aquí igual que en el anfitrión,
            // la diferencia de comportamiento vive entera en
            // `MaquinaReplica.Reposicionar` (RPC en vez de mutar el aparato).
            if (IsServer)
            {
                if (_cincel != null) _cincel.Init(sim);
                if (_mudanza != null) _mudanza.Init(sim);
            }
            else
            {
                if (_cincel != null) _cincel.enabled = false;
                if (_mudanza != null) _mudanza.Init(sim);
            }

            _cableado = true;
            Debug.Log("[TenThousandYears][Red] Avatar local cableado (color " + DescribirColor() + ").");
        }

        /// <summary>
        /// Punto de aparición: el mismo del plano
        /// (<see cref="SimLevelBuilder.AprendizX"/>/<c>AprendizY</c>, la celda
        /// de aire del cuarto donde nace el aprendiz de siempre) desplazado
        /// según el id del cliente para que cuatro aprendices no nazcan
        /// solapados. Lo coloca el DUEÑO, no el servidor: el
        /// `OwnerNetworkTransform` del template es de autoridad del
        /// PROPIETARIO, así que una posición escrita por el servidor sobre un
        /// objeto ajeno no se replicaría.
        /// </summary>
        private void ColocarEnPuntoDeAparicion()
        {
            float celda = SimRenderer.CellWorldSize;
            float x = (SimLevelBuilder.AprendizX + 0.5f) * celda;
            float y = (SimLevelBuilder.AprendizY + 0.5f) * celda;

            int ranura = (int)(OwnerClientId % 4UL);
            x += (ranura - 1.5f) * SeparacionSpawn;

            transform.position = new Vector3(x, y, 0f);
        }

        /// <summary>Índice de color libre más bajo (0..3). Si los cuatro estuvieran ocupados, reparte por id de cliente en vez de dejarlo sin color.</summary>
        private int PrimerColorLibre()
        {
            for (int i = 0; i < ColoresJugador.Length; i++)
            {
                bool ocupado = false;
                for (int j = 0; j < Todos.Count; j++)
                {
                    var otro = Todos[j];
                    if (otro == null || otro == this) continue;
                    if (otro.IndiceColor.Value == i) { ocupado = true; break; }
                }

                if (!ocupado) return i;
            }

            return (int)(OwnerClientId % (ulong)ColoresJugador.Length);
        }

        private void AlCambiarColor(byte anterior, byte nuevo)
        {
            AplicarTinteActual();
        }

        private void AplicarTinteActual()
        {
            if (_aprendiz == null) return;
            _aprendiz.AplicarTinte(ColorActual);
        }

        /// <summary>Color de este jugador según su índice replicado.</summary>
        public Color ColorActual
        {
            get
            {
                int i = IndiceColor.Value;
                if (i < 0 || i >= ColoresJugador.Length) i = 0;
                return ColoresJugador[i];
            }
        }

        /// <summary>Nombre del color en español, para el HUD de sesión.</summary>
        public string DescribirColor()
        {
            int i = IndiceColor.Value;
            if (i < 0 || i >= NombresColor.Length) i = 0;
            return NombresColor[i];
        }
    }
}
