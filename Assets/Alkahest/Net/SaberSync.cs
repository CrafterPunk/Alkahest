using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Alkahest.Game;
using Alkahest.Sim;

namespace Alkahest.Net
{
    /// <summary>
    /// EL SABER COMPARTIDO (playtest 36, EL CAMINO DEL INVITADO). Hermano de
    /// <see cref="SimSync"/> -- MISMA idea, aplicada a lo que sabe el taller
    /// en vez de a lo que hay pintado en él.
    ///
    /// =====================================================================
    /// EL HUECO QUE ESTO CIERRA
    /// =====================================================================
    /// `Game/SubstanceKnowledge.cs` vive en CADA avatar (host y cada
    /// invitado tienen su propia instancia, cableada en
    /// <see cref="AprendizNet.Cablear"/>) y antes de esta ronda su
    /// <c>Update()</c> completo se saltaba en un invitado porque
    /// <c>AlkahestSim.Stepper</c> es SIEMPRE null en el espejo (regla de oro
    /// del netcode: la sim JAMÁS corre fuera del anfitrión) -- ver el fix en
    /// ese archivo. Con ese fix, un invitado SÍ descubre en persona lo que
    /// aspira o mira fijo, pero hay dos cosas que NUNCA puede ver por sí
    /// mismo, por diseño:
    ///   (a) LAS LEYES: solo se presencian vía el ring buffer de
    ///       `SimStepper.Events`, que solo existe en el anfitrión.
    ///   (b) LO QUE OTRO JUGADOR (o el anfitrión) YA SABÍA ANTES de que este
    ///       invitado se conectara -- un invitado que entra tarde no tiene
    ///       forma de "haber estado ahí" para las 40 celdas de un
    ///       descubrimiento ajeno.
    /// Y una tercera, de autoridad: EL BAUTIZO tiene que resolverlo el
    /// anfitrión (es la única fuente de verdad del taller, igual que la sim),
    /// no cada cliente por su cuenta -- si dos jugadores bautizan lo mismo
    /// con nombres distintos casi a la vez, tiene que ganar uno solo.
    ///
    /// =====================================================================
    /// EL PROTOCOLO
    /// =====================================================================
    /// FUENTE DE VERDAD: el `SubstanceKnowledge` del ANFITRIÓN (el mismo
    /// criterio de "quien tiene la sim manda" que ya aplica
    /// <see cref="MaquinaSync"/> a las máquinas) -- <see cref="_conocimientoHost"/>,
    /// resuelto perezosamente igual que el resto del proyecto resuelve
    /// dependencias tardías (`AprendizNet.Local` puede tardar unos Update en
    /// existir). Cada <see cref="IntervaloSondeoSeg"/> (0.5s, mismo
    /// presupuesto que `MaquinaSync`: son decenas de materiales, no cientos
    /// de chunks) el anfitrión compara el estado de su conocimiento contra lo
    /// último publicado y difunde SOLO lo nuevo:
    ///   - <see cref="_descubiertos"/>: `NetworkList&lt;byte&gt;` de matId,
    ///     SOLO CRECE (un material nunca se "des-descubre").
    ///   - <see cref="_nombres"/>: `NetworkList&lt;EntradaNombre&gt;`, una
    ///     entrada por IDENTIDAD nombrable (un material innominado
    ///     individual, o el representante de una base×estado -- nunca 8
    ///     entradas por la misma base, ver <see cref="IdentidadDeNombre"/> y
    ///     la regla "una base = un nombre" de CLAUDE.md/playtest 25); se
    ///     ACTUALIZA en sitio en un re-bautizo, igual que
    ///     <see cref="MaquinaSync"/> actualiza una entrada de su registro al
    ///     mudar una máquina.
    ///   - <see cref="_leyes"/>: `NetworkList&lt;byte&gt;` de índice de ley
    ///     (0..Universe.Leyes.Length-1, siempre &lt;255), SOLO CRECE.
    /// Un invitado que se conecta TARDE reconstruye TODO desde estas tres
    /// listas en cuanto su propio conocimiento existe (mismo patrón de
    /// "catch-up" que <see cref="MaquinaSync.OnNetworkSpawn"/> usa para su
    /// registro), y de ahí en adelante aplica cada cambio en vivo vía
    /// `OnListChanged` -- los tres `Aplicar*Remoto` de `SubstanceKnowledge`
    /// son IDEMPOTENTES a propósito, así que reconstruir y recibir en vivo
    /// pueden solaparse sin duplicar nada.
    ///
    /// BAUTIZO DE UN INVITADO: T abre el rito NORMAL (`Game/NamingUi.cs`,
    /// sin cambios de comportamiento -- sigue siendo local, instantáneo,
    /// habla con SU PROPIO `SubstanceKnowledge`) y al confirmar, en vez de
    /// (o además de) aplicarlo localmente, manda <see cref="PedirBautizo"/>
    /// -- un Rpc al servidor con el nombre. El anfitrión lo aplica sobre SU
    /// `SubstanceKnowledge` (la autoridad) y lo empuja YA al registro
    /// (<see cref="SolicitarBautizoRpc"/>, sin esperar el sondeo de 0.5s --
    /// mismo criterio que <see cref="MaquinaSync.SolicitarMudanzaRpc"/> tras
    /// aceptar una mudanza), que lo devuelve a TODOS -- incluido quien lo
    /// pidió, que ya lo tenía puesto de forma optimista y solo ve la
    /// confirmación llegar al mismo valor.
    ///
    /// =====================================================================
    /// EL SITIO: HERMANO DE SimSync, SIN TOCAR EL EDITOR
    /// =====================================================================
    /// `Editor/AlkahestNetSceneBuilder.cs` (el único sitio donde nace el
    /// GameObject "AlkahestSimSync" con su `NetworkObject`) NO está en la
    /// lista de archivos permitidos de esta ronda. Un `NetworkBehaviour`
    /// puede vivir en CUALQUIER GameObject que ya tenga un `NetworkObject`
    /// spawneado por NGO -- no hace falta que sea el suyo propio -- así que
    /// <see cref="SimSync.Awake"/> añade este componente al SUYO PROPIO en
    /// tiempo de ejecución (`gameObject.AddComponent&lt;SaberSync&gt;()`) si
    /// todavía no está. Es seguro porque `Awake()` de TODOS los componentes
    /// de la escena corre mucho antes de que `NetworkManager.StartHost()/
    /// StartClient()` se llame (el jugador todavía tiene que pasar por el
    /// lobby de Steam/`SessionCoordinator`) -- que es el momento en que NGO
    /// de verdad recopila los `NetworkBehaviour` de un objeto de escena para
    /// spawnearlo. Ver el docblock de <see cref="SimSync"/> para el resto del
    /// patrón (los dos comparten Awake por la misma razón: reparar en
    /// runtime lo que el Editor fuera de alcance no puede cablear).
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class SaberSync : NetworkBehaviour
    {
        public static SaberSync Instancia { get; private set; }

        // -----------------------------------------------------------------
        // EL REGISTRO DE NOMBRES -- INetworkSerializable a mano, mismo
        // patrón que Net/MaquinaSync.EntradaMaquina. FixedString128Bytes (no
        // 64): un nombre de jugador cabe de sobra en 40 caracteres (el tope
        // real, ver Game/NamingUi.cs -- GUI.TextField(..., 40, ...)), pero en
        // UTF-8 una tilde/ñ ocupa 2 bytes y 40 caracteres acentuados pueden
        // llegar a 80 bytes -- 128 deja margen real, no solo nominal (regla
        // del encargo: "strings replicados con límite de tamaño").
        // -----------------------------------------------------------------
        public struct EntradaNombre : INetworkSerializable, System.IEquatable<EntradaNombre>
        {
            public byte matId; // identidad: matId individual, o el representante Polvo de una base (ver IdentidadDeNombre).
            public FixedString128Bytes nombre;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref matId);
                serializer.SerializeValue(ref nombre);
            }

            public bool Equals(EntradaNombre o) => matId == o.matId && nombre.Equals(o.nombre);
        }

        /// <summary>Una fila de encargo replicada -- ver Game/OrdersHud.cs, rama "sin OrderSystem local". FixedString128Bytes por la misma razón que EntradaNombre: las descripciones libres (DescribirNamedMaterial etc.) pueden llevar un nombre bautizado por el jugador dentro.</summary>
        public struct EntradaOrden : INetworkSerializable, System.IEquatable<EntradaOrden>
        {
            public int id;
            public FixedString128Bytes descripcion;
            public byte tipo; // OrderType, ver Game/Order.cs -- Net/ no depende de Game.OrderType como tipo, solo del byte.
            public int progreso;
            public int minCells;
            public int recompensa;
            public bool completado;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref id);
                serializer.SerializeValue(ref descripcion);
                serializer.SerializeValue(ref tipo);
                serializer.SerializeValue(ref progreso);
                serializer.SerializeValue(ref minCells);
                serializer.SerializeValue(ref recompensa);
                serializer.SerializeValue(ref completado);
            }

            public bool Equals(EntradaOrden o) =>
                id == o.id && descripcion.Equals(o.descripcion) && tipo == o.tipo && progreso == o.progreso &&
                minCells == o.minCells && recompensa == o.recompensa && completado == o.completado;
        }

        // Mismo criterio de permisos que Net/MaquinaSync.cs en todos los
        // NetworkList de este archivo: todos leen, SOLO el servidor escribe
        // (es la fuente de verdad del taller, igual que la sim).
        private readonly NetworkList<byte> _descubiertos = new NetworkList<byte>(
            readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        private readonly NetworkList<EntradaNombre> _nombres = new NetworkList<EntradaNombre>(
            readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        private readonly NetworkList<byte> _leyes = new NetworkList<byte>(
            readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        private readonly NetworkList<EntradaOrden> _ordenes = new NetworkList<EntradaOrden>(
            readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

        /// <summary>Favor del anfitrión (economía compartida del taller) -- OrdersHud lo pinta en modo replicado igual que pinta `OrderSystem.Favor` en modo local.</summary>
        public readonly NetworkVariable<int> FavorReplicado = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private const float IntervaloSondeoSeg = 0.5f;
        private float _acumuladorSondeo;

        // ---- ANFITRIÓN: caché de "ya publicado" para no reescanear en vano ----
        private SubstanceKnowledge _conocimientoHost;
        private OrderSystem _ordenesHost;
        private AlkahestSim _sim;
        private bool[] _descubiertoEnviado;
        private string[] _nombreEnviadoCache; // indexado por matId, solo se usa en las identidades representativas.
        private bool[] _leyEnviada;

        // ---- INVITADO: aplicación al conocimiento local ----
        private SubstanceKnowledge _conocimientoLocal;
        private bool _catchUpHecho;

        // =================================================================
        // Ciclo de vida
        // =================================================================

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Debug.LogWarning("[ChaosAlchemy][Red] Ya existe un SaberSync en la escena; se destruye el duplicado.");
                Destroy(this);
                return;
            }

            Instancia = this;
            _descubiertoEnviado = new bool[MaterialId.Count];
            _nombreEnviadoCache = new string[MaterialId.Count];
            _leyEnviada = new bool[64]; // ninguna semilla llega a 64 leyes (núcleo 7 + 5-8 sorteadas + 1 crecimiento, ver Universe.cs) -- techo con margen amplio, no un cálculo ajustado.
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _sim = FindAnyObjectByType<AlkahestSim>();

            if (!IsServer)
            {
                _descubiertos.OnListChanged += AlCambiarDescubiertos;
                _nombres.OnListChanged += AlCambiarNombres;
                _leyes.OnListChanged += AlCambiarLeyes;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer)
            {
                _descubiertos.OnListChanged -= AlCambiarDescubiertos;
                _nombres.OnListChanged -= AlCambiarNombres;
                _leyes.OnListChanged -= AlCambiarLeyes;
            }
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsSpawned) return;
            if (IsServer) { TickAnfitrion(); return; }
            TickInvitado();
        }

        // =================================================================
        // ANFITRIÓN
        // =================================================================

        private void TickAnfitrion()
        {
            if (_conocimientoHost == null)
            {
                var avatar = AprendizNet.Local; // el propio avatar del anfitrión en SU proceso -- ver el docblock de la clase.
                if (avatar != null) _conocimientoHost = avatar.GetComponent<SubstanceKnowledge>();
            }
            if (_ordenesHost == null) _ordenesHost = FindAnyObjectByType<OrderSystem>();

            _acumuladorSondeo += Time.deltaTime;
            if (_acumuladorSondeo < IntervaloSondeoSeg) return;
            _acumuladorSondeo -= IntervaloSondeoSeg;

            SondearConocimiento();
            SondearOrdenes();
        }

        /// <summary>Identidad de red de `matId` para el registro de nombres: él mismo si es individual, o el representante Polvo de su base si es una base×estado -- "una base = un nombre" (regla 25/playtest 25 de CLAUDE.md), nunca 8 entradas idénticas.</summary>
        private static byte IdentidadDeNombre(byte matId)
        {
            if (MaterialId.EsBaseEstado(matId)) return (byte)(MaterialId.BaseEstado0 + MaterialId.BaseDe(matId) * 8);
            return matId;
        }

        private void SondearConocimiento()
        {
            if (_conocimientoHost == null) return;

            for (int m = 1; m < MaterialId.Count; m++)
            {
                byte matId = (byte)m;

                if (!_descubiertoEnviado[matId] && _conocimientoHost.EsDescubierto(matId))
                {
                    _descubiertoEnviado[matId] = true;
                    _descubiertos.Add(matId);
                }

                byte identidad = IdentidadDeNombre(matId);
                if (identidad != matId) continue; // no es la forma representativa de su base -- se salta (ver IdentidadDeNombre).

                string nombre = _conocimientoHost.NombreDe(matId);
                if (nombre == "???") continue;
                if (_nombreEnviadoCache[identidad] == nombre) continue;
                UpsertNombre(identidad, nombre);
            }

            var leyes = _sim != null && _sim.Universe != null ? _sim.Universe.Leyes : null;
            int nLeyes = leyes != null ? Mathf.Min(leyes.Length, _leyEnviada.Length) : 0;
            for (int i = 0; i < nLeyes; i++)
            {
                if (!_leyEnviada[i] && _conocimientoHost.LeyDescubierta(i))
                {
                    _leyEnviada[i] = true;
                    _leyes.Add((byte)i);
                }
            }
        }

        /// <summary>Añade o actualiza en sitio la entrada de `identidad` en <see cref="_nombres"/> -- mismo patrón que Net/MaquinaSync busca-y-actualiza su registro tras una mudanza aceptada.</summary>
        private void UpsertNombre(byte identidad, string nombre)
        {
            _nombreEnviadoCache[identidad] = nombre;
            var entrada = new EntradaNombre { matId = identidad, nombre = new FixedString128Bytes(nombre) };

            for (int i = 0; i < _nombres.Count; i++)
            {
                if (_nombres[i].matId == identidad) { _nombres[i] = entrada; return; }
            }
            _nombres.Add(entrada);
        }

        private void SondearOrdenes()
        {
            if (_ordenesHost == null) return;

            FavorReplicado.Value = _ordenesHost.Favor;

            var activos = _ordenesHost.ActiveOrders;
            // (cero allocs) Reescribir el registro entero por valor cuando
            // cambia CUALQUIER cosa es más barato de razonar que un diff
            // fino (como mucho hay un puñado de encargos activos a la vez,
            // ver OrderSystem.ActiveOrders) y evita dejar entradas huérfanas
            // si un encargo se completa y el arco lo sustituye por otro con
            // el mismo hueco de lista pero Id distinto.
            bool distinto = activos.Count != _ordenes.Count;
            if (!distinto)
            {
                for (int i = 0; i < activos.Count; i++)
                {
                    var o = activos[i];
                    var e = _ordenes[i];
                    // (fix compilación, único error del playtest 36: CS0030)
                    // FixedString128Bytes no tiene cast a string -- la
                    // comparación correcta y SIN alloc es al revés: comparar
                    // contra el string con el propio Equals de FixedString
                    // (que compara bytes UTF-8 contra el string sin crear
                    // basura), nunca ToString() por fila por sondeo.
                    if (e.id != o.Id || e.progreso != o.Progreso || e.completado != o.Completado ||
                        !e.descripcion.Equals(RecortarDescripcion(o.Descripcion)))
                    {
                        distinto = true;
                        break;
                    }
                }
            }
            if (!distinto) return;

            _ordenes.Clear();
            for (int i = 0; i < activos.Count; i++)
            {
                var o = activos[i];
                string desc = o.Descripcion ?? "";
                desc = RecortarDescripcion(desc); // límite de tamaño (FixedString128Bytes, margen UTF-8) -- MISMO recorte que usa la comparación de cambios de arriba: si difirieran, un encargo con descripción larga se re-difundiría cada sondeo para siempre.
                _ordenes.Add(new EntradaOrden
                {
                    id = o.Id,
                    descripcion = new FixedString128Bytes(desc),
                    tipo = (byte)o.Tipo,
                    progreso = o.Progreso,
                    minCells = o.MinCells,
                    recompensa = o.Recompensa,
                    completado = o.Completado,
                });
            }
        }

        /// <summary>
        /// El bautizo de un invitado, aplicado con autoridad. Mismo criterio
        /// que <see cref="MaquinaSync.SolicitarMudanzaRpc"/>: valida poco
        /// (aquí no hay "cabe/no cabe", cualquier nombre no vacío es válido,
        /// la propia UI ya lo comprobó) y empuja el resultado YA al
        /// registro, sin esperar el sondeo de 0.5s.
        /// </summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SolicitarBautizoRpc(byte matId, FixedString128Bytes nombre)
        {
            if (!IsServer || _conocimientoHost == null) return;
            if (matId >= MaterialId.Count) return;

            string texto = nombre.ToString();
            _conocimientoHost.Bautizar(matId, texto);

            byte identidad = IdentidadDeNombre(matId);
            string nombreAplicado = _conocimientoHost.NombreDe(matId);
            if (nombreAplicado != "???")
            {
                _descubiertoEnviado[matId] = true; // bautizar implica descubrir (ver SubstanceKnowledge.Bautizar) -- refleja eso también en la caché de "ya enviado" de _descubiertos.
                if (!ContieneDescubierto(matId)) _descubiertos.Add(matId);
                UpsertNombre(identidad, nombreAplicado);
            }
        }

        private bool ContieneDescubierto(byte matId)
        {
            for (int i = 0; i < _descubiertos.Count; i++) if (_descubiertos[i] == matId) return true;
            return false;
        }

        /// <summary>
        /// Wrapper público estático para que <see cref="NamingUi"/> (un
        /// MonoBehaviour normal, no puede invocar un <c>[Rpc]</c> directo)
        /// pida un bautizo. Mismo patrón que <see cref="MaquinaSync.PedirMudanza"/>.
        /// No hace nada si no hay sesión de red o si somos el anfitrión (el
        /// anfitrión ya aplicó su propio bautizo localmente, sin viaje de
        /// ida y vuelta -- ver Game/NamingUi.cs).
        /// </summary>
        public static void PedirBautizo(byte matId, string nombre)
        {
            var s = Instancia;
            if (s == null || !s.IsSpawned || s.IsServer) return;
            if (string.IsNullOrWhiteSpace(nombre)) return;

            string limpio = nombre.Trim();
            if (limpio.Length > 40) limpio = limpio.Substring(0, 40); // mismo tope que GUI.TextField en Game/NamingUi.cs.
            s.SolicitarBautizoRpc(matId, new FixedString128Bytes(limpio));
        }

        // =================================================================
        // INVITADO: catch-up + aplicación en vivo
        // =================================================================

        private void TickInvitado()
        {
            if (_conocimientoLocal == null)
            {
                var avatar = AprendizNet.Local;
                if (avatar != null) _conocimientoLocal = avatar.GetComponent<SubstanceKnowledge>();
                if (_conocimientoLocal == null) return;
            }

            if (_catchUpHecho) return;

            // ENTRA TARDE UN INVITADO -> RECIBE TODO EL SABER YA PUBLICADO
            // (contrato de esta ronda): mismo patrón de reconstrucción que
            // Net/MaquinaSync.OnNetworkSpawn usa para su registro, pero
            // diferido hasta que el conocimiento LOCAL exista (a diferencia
            // de las réplicas de máquina, aquí el destino es un componente
            // del avatar propio, que puede tardar unos Update en cablearse,
            // ver AprendizNet.Cablear).
            for (int i = 0; i < _descubiertos.Count; i++) _conocimientoLocal.AplicarDescubrimientoRemoto(_descubiertos[i]);
            for (int i = 0; i < _nombres.Count; i++) AplicarNombreLocal(_nombres[i]);
            for (int i = 0; i < _leyes.Count; i++) _conocimientoLocal.AplicarLeyRemota(_leyes[i]);
            _catchUpHecho = true;
        }

        private void AplicarNombreLocal(EntradaNombre e)
        {
            if (_conocimientoLocal == null) return;
            _conocimientoLocal.AplicarNombreRemoto(e.matId, e.nombre.ToString());
        }

        /// <summary>El recorte canónico de una descripción de encargo para caber en FixedString128Bytes (120 chars, margen UTF-8). ÚNICO punto de verdad: lo usan el volcado y la comparación de cambios -- ver el comentario del fix CS0030.</summary>
        private static string RecortarDescripcion(string desc)
        {
            return (desc != null && desc.Length > 120) ? desc.Substring(0, 120) : desc;
        }

        private void AlCambiarDescubiertos(NetworkListEvent<byte> ev)
        {
            if (IsServer || _conocimientoLocal == null) return;
            if (ev.Type == NetworkListEvent<byte>.EventType.Add || ev.Type == NetworkListEvent<byte>.EventType.Value)
                _conocimientoLocal.AplicarDescubrimientoRemoto(ev.Value);
        }

        private void AlCambiarNombres(NetworkListEvent<EntradaNombre> ev)
        {
            if (IsServer || _conocimientoLocal == null) return;
            if (ev.Type == NetworkListEvent<EntradaNombre>.EventType.Add || ev.Type == NetworkListEvent<EntradaNombre>.EventType.Value)
                AplicarNombreLocal(ev.Value);
        }

        private void AlCambiarLeyes(NetworkListEvent<byte> ev)
        {
            if (IsServer || _conocimientoLocal == null) return;
            if (ev.Type == NetworkListEvent<byte>.EventType.Add || ev.Type == NetworkListEvent<byte>.EventType.Value)
                _conocimientoLocal.AplicarLeyRemota(ev.Value);
        }

        // =================================================================
        // LECTURA PÚBLICA PARA Game/OrdersHud.cs (rama "sin OrderSystem
        // local", ver el docblock de esa clase). Solo lectura -- ningún
        // consumidor de aquí puede escribir el registro, la autoridad sigue
        // siendo el anfitrión.
        // =================================================================
        public int CountOrdenesReplicadas => _ordenes.Count;
        public EntradaOrden ObtenerOrdenReplicada(int i) => _ordenes[i];
    }
}
