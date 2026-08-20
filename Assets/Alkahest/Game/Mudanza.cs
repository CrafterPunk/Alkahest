using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;
using Alkahest.Net;

namespace Alkahest.Game
{
    /// <summary>
    /// Aparato del taller que el modo Mudanza puede agarrar y recolocar.
    /// Implementado por <see cref="HeatPlate"/>, <see cref="ChillStone"/> y
    /// <see cref="Dispenser"/>. (fix Cesar playtest 33) Game/StorageRack.cs
    /// TAMBIÉN lo implementa desde esta ronda ("REDOMAS MOVIBLES", tarea 3
    /// del encargo) -- el aviso viejo de este párrafo ("el estante NO tiene
    /// que ser movible todavía") queda desactualizado a propósito, no
    /// borrado (regla 15 de CLAUDE.md): documenta que hubo una ronda entera
    /// en la que fue una decisión consciente, no un olvido.
    ///
    /// EL CONTRATO ES DELIBERADAMENTE FINO: Mudanza trata cada aparato de
    /// forma OPACA -- nunca lee sus campos privados, nunca sabe si es una
    /// placa, una piedra o un grifo. Todo lo que necesita para agarrar,
    /// previsualizar, soltar o cancelar sale de estos cinco miembros.
    ///
    /// (playtest 29, encargo D) También lo implementan las CINCO estaciones
    /// del taller -- Crisol/Prensa/BancoChispa/ColumnaEnsayo/EnsayoMaestro --
    /// vía <see cref="IMovibleAnclaEsquina"/> (ver ese docblock).
    ///
    /// (playtest 30, MÁQUINAS EN RED, Net/MaquinaSync.cs) Y TAMBIÉN lo
    /// implementa <see cref="Alkahest.Net.MaquinaReplica"/>: la réplica visual que un
    /// invitado ve en vez de la máquina real. Esta clase (Mudanza) NO
    /// distingue una réplica de un aparato de verdad -- ni falta que le hace,
    /// ese es justo el punto del contrato "opaco" de arriba. La diferencia
    /// entera vive en cómo cada uno implementa <see cref="Reposicionar"/>: el
    /// aparato real se mueve ahí mismo; la réplica manda una solicitud por
    /// RPC y solo se mueve de verdad si el anfitrión (dueño de la máquina de
    /// verdad) la acepta. Por eso Net/AprendizNet.cs ya no desactiva esta
    /// clase para el avatar de un invitado (sí sigue desactivando el
    /// Cincel, que talla la sim autoritativa -- eso no tiene equivalente de
    /// red en este POC).
    /// </summary>
    public interface IMovible
    {
        /// <summary>Centro del mundo del aparato AHORA MISMO -- para medir distancia al agarrar y como punto de referencia inicial.</summary>
        Vector3 CentroMundo { get; }

        /// <summary>Tamaño aproximado del footprint en unidades de mundo (ancho, alto). Alimenta la silueta GENÉRICA de arrastre de Mudanza (un rectángulo teñido, no una copia del sprite real) -- no hace falta que sea pixel-perfect.</summary>
        Vector2 TamanoMundo { get; }

        /// <summary>
        /// Celda de anclaje actual. La semántica es propia de cada aparato
        /// (HeatPlate/ChillStone: esquina X0 del chasis + fila del suelo;
        /// Dispenser: celda de montaje del caño) -- Mudanza NUNCA la
        /// interpreta, solo la usa para (a) calcular el offset de arrastre al
        /// agarrar (para que el punto donde clicaste dentro del aparato se
        /// conserve mientras lo llevas) y (b) pasarla de vuelta, sin tocarla,
        /// a <see cref="Reposicionar"/>.
        /// </summary>
        Vector2Int AnclaCelda { get; }

        /// <summary>
        /// ¿Cabría el aparato en esa celda de anclaje sin que su geometría
        /// toque el marco protegido del mundo (fila/columna 0 y W-1/H-1, el
        /// mismo marco que Sim/SimLevelBuilder.FillBorder pinta)? Cada
        /// aparato conoce su propio footprint -- Mudanza no lo recalcula, solo
        /// pregunta. Puramente informativo: NO comprueba si hay una cubeta ahí
        /// debajo (eso Mudanza lo resuelve por su cuenta muestreando la
        /// grilla, ver Game/Mudanza.cs) ni el alcance del jugador (eso también
        /// lo resuelve Mudanza, es lo mismo para cualquier aparato).
        /// </summary>
        bool CabeEnAncla(Vector2Int anclaCelda);

        /// <summary>
        /// Mueve el aparato DE VERDAD a la celda de anclaje indicada.
        /// Reutiliza los hijos ya creados (nunca crea ni destruye
        /// GameObjects -- ver el docblock de la implementación en cada
        /// aparato para el porqué exacto) y nunca vuelve a llamar a Init.
        /// Mudanza la invoca UNA sola vez, al soltar con éxito -- mientras el
        /// aparato "va en la mano" solo se mueve la silueta genérica, el
        /// aparato real no se toca hasta que el sitio es válido.
        /// </summary>
        void Reposicionar(Vector2Int anclaCelda);
    }

    /// <summary>
    /// (playtest 29, encargo D) Marca a los aparatos cuya
    /// <see cref="IMovible.AnclaCelda"/> es EXACTAMENTE la esquina inferior
    /// izquierda del rect que mide <see cref="IMovible.TamanoMundo"/> -- las
    /// CINCO estaciones de la línea del taller (Crisol/Prensa/Columna/
    /// Chispa/Ensayo): su ancla es la esquina de su propio rect exterior, el
    /// mismo que tallan (ver `_outX0,_outY0` en cada una). NO es cierto para
    /// `Dispenser` (el ancla es la celda de la boquilla, no una esquina de su
    /// footprint) ni para `Criatura`/`Capullo` (el ancla es la celda de cuna/
    /// repisa) -- esos archivos NO se tocan en este encargo (fuera de
    /// Game/Crisol.cs, Game/Prensa.cs, Game/BancoChispa.cs,
    /// Game/ColumnaEnsayo.cs, Game/EnsayoMaestro.cs, Game/Mudanza.cs y
    /// Sim/SimLevelBuilder.cs fue "el resto de Game/", prohibido).
    ///
    /// POR QUÉ EXISTE: sin esta garantía, Mudanza NO tiene forma de saber
    /// dónde caería la huella real de un aparato genérico -- solo conoce su
    /// TAMAÑO, no su forma de anclarse -- así que la única opción honesta
    /// para el caso general es dibujar la silueta centrada en el CURSOR (ver
    /// <see cref="ActualizarVisuales"/>). Con la garantía de esta interfaz,
    /// en cambio, Mudanza SÍ puede calcular la esquina/centro exactos donde
    /// aterrizaría el aparato (`AnclaCelda` convertida a mundo + medio
    /// `TamanoMundo`) y pegar la sombra ahí -- que es la "sombra alineada a
    /// su ancla, cubriendo la huella real" que pide el encargo. Interfaz
    /// vacía (marcador puro, sin miembros nuevos): cero coste, cero allocs.
    /// </summary>
    public interface IMovibleAnclaEsquina : IMovible { }

    /// <summary>
    /// EL MODO MUDANZA (playtest 19, "taller movible"): agarrar un aparato del
    /// banco y recolocarlo donde quiera el jugador. Cesar, la noche antes de
    /// probar esto: *"ya tengo ganas de mover las cosas a mi antojo porque
    /// hacer las pruebas es cansado donde todo está separado... debería
    /// transmitir la sensación de estar yo en un lugar pequeño"*. Es el paso
    /// 5 de la fase acordada (CLAUDE.md): cámara -> taller a pantallas ->
    /// química por semilla -> comportamiento por semilla -> TALLER MOVIBLE ->
    /// mundo persistente. El Cincel (playtest 16) ya deja tallar/rellenar
    /// bedrock; Mudanza es la segunda pieza: los APARATOS (grifos, placas,
    /// piedra fría) ahora se pueden recolocar sin volver a montar la escena.
    /// El estante de redomas se queda quieto esta ronda (otro encargo es
    /// dueño de Game/StorageRack.cs ahora mismo).
    ///
    /// -----------------------------------------------------------------------
    /// CERO COORDENADAS LITERALES (aviso crítico del encargo)
    /// -----------------------------------------------------------------------
    /// Otro encargo está COMPACTANDO el plano del taller en paralelo
    /// (Sim/SimLevelBuilder.cs, de solo lectura aquí). Por eso este archivo no
    /// escribe ni un solo número de posición a mano: los límites del mundo
    /// salen de CellGrid.W/H, el alcance de Flask.ReachWorld, y "dónde está
    /// cada aparato" nunca se pregunta -- lo pregunta CADA aparato de sí
    /// mismo a través de IMovible.
    ///
    /// -----------------------------------------------------------------------
    /// DISEÑO: LA SILUETA SE MUEVE, EL APARATO NO -- HASTA QUE SE SUELTA BIEN
    /// -----------------------------------------------------------------------
    /// Mientras el jugador "lleva" un aparato, NADA del aparato real cambia:
    /// sigue en su sitio de siempre, con sus hijos intactos. Lo único que
    /// sigue al cursor es una silueta GENÉRICA (un rectángulo del tamaño de
    /// <see cref="IMovible.TamanoMundo"/>, teñido verde/rojo) creada UNA vez
    /// en <see cref="BuildVisuals"/> -- exactamente el mismo patrón que
    /// Game/Cincel.cs (anillo/haz creados una vez, Update solo mueve/tiñe,
    /// cero asignaciones por frame). Solo al soltar con un sitio VÁLIDO se
    /// llama a <see cref="IMovible.Reposicionar"/>, UNA sola vez: el aparato
    /// "teleporta" a su nuevo sitio en ese instante, sin animación de
    /// arrastre en tiempo real.
    ///
    /// Esto no es una limitación de tiempo, es la decisión correcta: si se
    /// moviera el aparato REAL en cada frame mientras se arrastra,
    /// Reposicionar (que mueve transform.position, recalcula centros/anclas
    /// de rótulo y, en el caso general, podría tener que reasignar un sprite)
    /// se llamaría decenas de veces por segundo por nada -- y CANCELAR (R)
    /// sería "deshacer todos esos movimientos", con el riesgo de que algo se
    /// quedara desincronizado a mitad de camino. Con la silueta de por medio,
    /// cancelar es LITERALMENTE gratis: como el aparato nunca se movió, no
    /// hay nada que deshacer (ver <see cref="CancelarYSoltar"/>).
    ///
    /// -----------------------------------------------------------------------
    /// AGARRAR / SOLTAR: CLICS, NO PULSACIÓN MANTENIDA (a diferencia de
    /// Flask/Cincel)
    /// -----------------------------------------------------------------------
    /// El frasco y el cincel actúan mientras el botón está PULSADO
    /// (mouse.leftButton.isPressed, un chorro/disco continuo). Mudanza no
    /// vierte ni talla nada: es un simple "agarra" / "suelta", así que un
    /// clic (wasPressedThisFrame) por acción es lo natural -- "clic izq. sobre
    /// el aparato más cercano al cursor" para agarrar, "clic izq. otra vez"
    /// para soltar. Con el botón mantenido no pasa nada especial (no hay
    /// tick que acumular, a diferencia de aspirar/tallar), así que no hace
    /// falta ningún acumulador de Time.deltaTime en esta clase.
    ///
    /// -----------------------------------------------------------------------
    /// VALIDEZ DEL SITIO: alcance + dentro del mundo, NUNCA "hay que haber
    /// cubeta"
    /// -----------------------------------------------------------------------
    /// Verde = dentro de <see cref="Flask.ReachWorld"/> del jugador Y
    /// <see cref="IMovible.CabeEnAncla"/> confirma que no se sale del marco
    /// protegido del mundo. Rojo = cualquiera de las dos falla, y SOLTAR SE
    /// RECHAZA (no es solo un color, de verdad no suelta el aparato ahí --
    /// alcance y bordes del mundo son límites duros, igual que en Flask/
    /// Cincel). "¿Hay una cubeta de piedra debajo?" NO es parte de esa
    /// validez -- el Cincel existe justo para que el jugador se construya el
    /// recipiente él mismo DESPUÉS de mover el aparato, así que exigirlo aquí
    /// contradiría la propia razón de ser del Cincel. En vez de bloquear, se
    /// avisa con un RÓTULO aparte (ver <see cref="OnGUI"/>, "sin cubeta aquí")
    /// que no cambia el color de la silueta ni impide soltar.
    ///
    /// -----------------------------------------------------------------------
    /// LOS TRES MODOS EXCLUSIVOS (Frasco / Cincel / Mudanza) -- Y LO QUE
    /// SIGUE SIN CERRAR
    /// -----------------------------------------------------------------------
    /// Game/Flask.cs (de mi propiedad en este encargo) ya cede tanto a
    /// Cincel.ModoActivo como -- desde este mismo encargo -- a
    /// <see cref="ModoActivo"/>. Esta clase cede a Cincel.ModoActivo (ver
    /// Update: ni activa el modo con V, ni actúa, ni pinta nada mientras el
    /// cincel está en la mano; si el jugador pulsa C MIENTRAS ya lleva un
    /// aparato agarrado, Mudanza se apaga sola y suelta la referencia el
    /// frame siguiente -- gratis, porque soltar sin mover es soltar la
    /// silueta, no el aparato real, ver el bloque de arriba).
    ///
    /// LOS TRES MODOS SON EXCLUYENTES, Y LA EXCLUSIÓN YA ES SIMÉTRICA: el
    /// frasco cede ante los dos (guarda en Flask.Update), Mudanza cede ante el
    /// Cincel (ver Update), y el Cincel llama a <see cref="ForzarSalida"/> al
    /// encenderse. Ese último lado lo cerró el director al integrar la ronda:
    /// este encargo no era dueño de Game/Cincel.cs y lo dejó anotado como
    /// hueco, con lo que durante un rato la exclusión estuvo coja —
    /// pulsar C con la mudanza activa dejaba los dos modos en pie el mismo
    /// frame, con un aparato colgando del cursor mientras el cincel ya
    /// tallaba. Si algún día hay un cuarto modo, que use la misma puerta.
    ///
    /// DESHACER ES UNA TECLA, Y ESO ES LO QUE HACE SEGURO EXPERIMENTAR:
    /// **R** con algo agarrado cancela ese arrastre; **R con las manos
    /// vacías devuelve TODOS los aparatos a su sitio de fábrica**. Existe
    /// porque mover un aparato fuera de su recipiente no rompe nada visible
    /// pero sí vuelve imposibles encargos enteros EN SILENCIO (la piedra
    /// gélida fuera de la bandeja sigue enfriando, solo que ya no donde el
    /// Maestro sembró la semilla). La respuesta no podía ser prohibirlo
    /// —Cesar pidió justo lo contrario— sino que volver atrás fuera trivial.
    /// Ver <see cref="_anclasDeFabrica"/>.
    /// </summary>
    [RequireComponent(typeof(ApprenticeController))]
    public sealed class Mudanza : MonoBehaviour
    {
        /// <summary>¿Lleva el aprendiz la mudanza en la mano ahora mismo? Mismo patrón que Cincel.ModoActivo: un flag estático de solo lectura hacia fuera que otras clases (Flask) consultan para ceder el turno.</summary>
        public static bool ModoActivo { get; private set; }

        // -----------------------------------------------------------------
        // REGISTRO DE APARATOS MOVIBLES (mismo patrón que Game/MachineFocus.cs,
        // pero para un propósito distinto: MachineFocus arbitra QUIÉN responde
        // a E; esta lista solo sirve para que Mudanza sepa qué aparatos existen
        // sin tener que buscarlos con FindAnyObjectByType uno a uno. HeatPlate/
        // ChillStone/Dispenser se registran en su propio Init y se olvidan en
        // su propio OnDestroy -- exactamente el mismo ciclo de vida que ya
        // usan con MachineFocus.Registrar/Olvidar.
        // -----------------------------------------------------------------
        private static readonly List<IMovible> _movibles = new List<IMovible>(8);

        /// <summary>
        /// (playtest 19) EL SITIO DE FÁBRICA DE CADA APARATO, guardado en el
        /// mismo momento en que se registra — es decir, la posición que le dio
        /// <see cref="AlkahestGameBootstrap"/> leyendo el plano, antes de que
        /// el jugador haya podido tocar nada.
        ///
        /// POR QUÉ EXISTE (y es la pieza que hace que la mudanza sea segura de
        /// usar): mover un aparato fuera de su recipiente NO rompe nada de
        /// forma visible, pero SÍ vuelve imposibles encargos enteros en
        /// silencio. Si la piedra gélida deja de estar sobre la bandeja fría,
        /// el frío sigue funcionando —solo que ya no cae donde el Maestro
        /// sembró la semilla—, así que "algo helado" y "la piedra que crece en
        /// la bandeja" dejan de ser cumplibles y **el juego no dice por qué**.
        /// Lo mismo con una placa ígnea fuera de su cuba y el retoño de vivium.
        ///
        /// La respuesta NO es prohibirle mover cosas: Cesar pidió justo lo
        /// contrario (*"ya tengo ganas de mover las cosas a mi antojo"*), y una
        /// herramienta que te impide equivocarte tampoco te deja descubrir. La
        /// respuesta es que **deshacer sea trivial**: con la tecla R y las
        /// manos vacías, todo vuelve a su sitio de fábrica. Se experimenta sin
        /// miedo porque el camino de vuelta es una tecla.
        /// </summary>
        private static readonly List<Vector2Int> _anclasDeFabrica = new List<Vector2Int>(8);

        public static void RegistrarMovible(IMovible m)
        {
            if (m == null || _movibles.Contains(m)) return;
            _movibles.Add(m);
            _anclasDeFabrica.Add(m.AnclaCelda); // los dos arrays van SIEMPRE en paralelo, ver OlvidarMovible.
        }

        public static void OlvidarMovible(IMovible m)
        {
            if (m == null) return;
            int i = _movibles.IndexOf(m);
            if (i < 0) return;
            // Se quitan por ÍNDICE y de los dos a la vez: si alguna vez se
            // desalinean, R devolvería cada aparato al sitio de otro, que es
            // peor que no tener R.
            _movibles.RemoveAt(i);
            _anclasDeFabrica.RemoveAt(i);
        }

        /// <summary>
        /// Devuelve TODOS los aparatos a su sitio de fábrica. Lo dispara la
        /// tecla R con las manos vacías (con algo agarrado, R cancela ese
        /// arrastre — es el mismo gesto de "deshacer" en los dos casos).
        /// Ver el docblock de <see cref="_anclasDeFabrica"/> para el porqué.
        /// </summary>
        private void DevolverTodoASuSitio()
        {
            int movidos = 0;
            for (int i = 0; i < _movibles.Count && i < _anclasDeFabrica.Count; i++)
            {
                var m = _movibles[i];
                if (m == null) continue;
                var comoObjeto = m as Object;
                if (comoObjeto == null) continue; // destruido: lo limpiará la pasada de IntentarAgarrar.
                if (m.AnclaCelda == _anclasDeFabrica[i]) continue; // ya estaba en su sitio.
                // (fix Cesar playtest 33, MULTI) R con las manos vacías no le
                // arranca a otro jugador lo que tiene agarrado en ESE
                // instante: se salta cualquier aparato con el cerrojo puesto
                // por otro cliente y sigue con el resto -- "todo vuelve a su
                // sitio" no debería incluir "menos lo que alguien más está
                // usando ahora mismo".
                if (MaquinaSync.EstaBloqueadoPorOtro(m)) continue;
                m.Reposicionar(_anclasDeFabrica[i]);
                movidos++;
            }

            if (_flask == null) return;
            _flask.Avisar(movidos > 0
                ? "el taller vuelve a su sitio"
                : "nada que devolver: todo está donde estaba");
        }

        private const int PreviewSortingOrder = 44; // por debajo del aprendiz (47-50) y del haz/anillo del Cincel (38-40 están más lejos aún, así que nunca coinciden).
        private const int ModeIconSortingOrder = 62;   // justo por encima del icono de modo del Cincel (61): si algún día coincidieran (no deberían, son mutuamente excluyentes), Mudanza gana.

        private const float AlfaPreviewValido = 0.40f;
        private const float AlfaPreviewInvalido = 0.34f;
        private const float ModeIconAlpha = 0.95f;

        // Verde/rojo, exactamente como pide el encargo -- tonos de mundo (no
        // UiStyles.*, que son de UI), en la misma familia que Game/Cincel.cs.
        private static readonly Color32 ColorValido = new Color32(108, 196, 110, 255);
        private static readonly Color32 ColorInvalido = new Color32(219, 84, 71, 255);
        // Azul-latón: mismo lenguaje de "latón" que Flask/Cincel para el icono
        // de modo, pero con un tinte distinto (más frío) para que "llevo la
        // mudanza" nunca se confunda de un vistazo con "llevo el cincel"
        // (BrassBase dorado) aunque, por diseño, jamás estén encendidos a la vez.
        private static readonly Color32 IconoMudanza = new Color32(120, 168, 196, 255);

        private AlkahestSim _sim;
        private ApprenticeController _apprentice;
        private Flask _flask; // solo para Avisar(): mismo canal de feedback compartido que usa Cincel.

        private bool _hasCursorWorld;
        private Vector3 _cursorWorld;
        private bool _hasCursorCell;
        private Vector2Int _cursorCell;

        // Grazia de agarre (unidades de mundo) sumada al radio del propio
        // aparato: los grifos son estrechos (0.8 unidades de ancho, ver
        // Dispenser.TamanoMundo), sin margen sería casi imposible clicarlos.
        private const float AgarreGraciaWorld = 0.35f;

        private IMovible _llevando;
        private Vector2Int _offsetArrastreCeldas; // AnclaCelda del aparato MENOS la celda del cursor en el momento de agarrar -- se conserva mientras se arrastra, así el punto donde clicaste dentro del aparato no "salta" al centro.
        private bool _sinCubeta; // el sitio candidato es válido pero no hay piedra debajo -- ver OnGUI.

        private Transform _previewTr;
        private SpriteRenderer _previewSr;
        private SpriteRenderer _modeIconSr;

        private void Awake()
        {
            _apprentice = GetComponent<ApprenticeController>();
            _flask = GetComponent<Flask>();
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap, mismo patrón que Flask.Init/Cincel.Init.</summary>
        public void Init(AlkahestSim sim)
        {
            _sim = sim;
            BuildVisuals();

            SpawnBaldasYAnclajesSiCorresponde();
        }

        // -----------------------------------------------------------------
        // (fix Cesar playtest 33, sistema de baldas/anclajes) EL ÚNICO SITIO
        // DE ESTE ENCARGO DONDE SE PUEDE INYECTAR AlkahestSim
        // -----------------------------------------------------------------
        // Game/Balda.cs y Game/Anclaje.cs son GEOMETRÍA DEL MUNDO (como las
        // cinco estaciones), no herramientas del jugador -- deberían nacer
        // UNA vez desde AlkahestGameBootstrap.cs, junto a SpawnColumnaEnsayo/
        // SpawnAlambique. Ese archivo NO está en la lista de permitidos de
        // este encargo, así que no hay forma de añadir un `SpawnBaldas(...)`
        // ahí. Mudanza.Init SÍ es un archivo permitido y SÍ recibe
        // `AlkahestSim` -- pero es un componente POR AVATAR
        // (`[RequireComponent(typeof(ApprenticeController))]`, ver
        // Net/AprendizNet.cs::Cablear/AlkahestGameBootstrap.SpawnApprentice),
        // así que llamar aquí sin más duplicaría baldas/anclajes una vez por
        // jugador local. Dos guardas, cada una necesaria por su cuenta:
        //
        ///  1) `Balda.SpawnTodas`/`Anclaje.SpawnDeposito` llevan su PROPIO
        ///     flag estático "ya creado" (ver esos archivos) -- así que
        ///     aunque Init se llamara dos veces en el MISMO proceso, la
        ///     segunda es un no-op. Es la red de seguridad de fondo.
        ///  2) SOLO EL ANFITRIÓN (o la partida de un jugador, que es "su
        ///     propio anfitrión") talla de verdad: en un invitado, este
        ///     mismo método correría en SU proceso, con SU propio flag
        ///     estático a false la primera vez -- sin este segundo chequeo,
        ///     cada invitado tallaría su propia copia de las baldas/anclajes
        ///     EN SU GRID LOCAL, que ni siquiera es la autoritativa (la sim
        ///     vive solo en el anfitrión, ver Net/SimSync.cs). Un invitado
        ///     recibe las baldas/anclajes como <see cref="Alkahest.Net.MaquinaReplica"/>
        ///     (registro de Net/MaquinaSync.cs, tipos Balda/Anclaje) --
        ///     exactamente el mismo camino que ya usan las cinco estaciones
        ///     y los grifos, ver el docblock de esa clase.
        ///
        /// `SimSync.EsServidor` ya es la comprobación que usa
        /// AlkahestGameBootstrap.TrySpawnRed para esta misma decisión
        /// ("¿soy quien construye el taller de verdad?").
        ///
        /// (fix Cesar playtest 34, CAUSA RAÍZ CONFIRMADA DEL BUG "en un
        /// jugador no aparecen los soportes") EL PÁRRAFO DE ARRIBA MENTÍA:
        /// `SimSync.EsServidor` NO es trivialmente `true` en la escena
        /// clásica -- es `Instancia != null && Instancia.IsSpawned &&
        /// Instancia.IsServer` (ver Net/SimSync.cs), y en la escena SIN
        /// `SimSync` (la clásica de un jugador) `Instancia` es `null`, así
        /// que la expresión entera da `false`. El guardián que debía dejar
        /// pasar SIEMPRE al modo un jugador lo bloqueaba SIEMPRE: ni un
        /// jugador solo ni el anfitrión del multi (ahí SÍ hay `SimSync`,
        /// pero antes de que la sesión termine de spawnear `EsServidor`
        /// también puede ser `false` un frame) veían nunca baldas, anclajes
        /// ni pilas. La comprobación correcta distingue las DOS preguntas
        /// que `EsServidor` mezclaba en una sola: "¿hay escena multi?" (
        /// <see cref="SimSync.EnEscena"/>, true solo si el GameObject de
        /// SimSync existe en la escena) y, SOLO si la hay, "¿soy el
        /// anfitrión?" (<see cref="SimSync.EsServidor"/>). En la escena
        /// clásica `EnEscena` es `false` y la expresión entera pasa siempre,
        /// tal como se pretendía desde el principio.
        // -----------------------------------------------------------------
        private void SpawnBaldasYAnclajesSiCorresponde()
        {
            if (SimSync.EnEscena && !SimSync.EsServidor) return; // multi: solo el anfitrión talla. Clásico (EnEscena=false): siempre entra.
            Balda.SpawnTodas(_sim);
            Anclaje.SpawnDeposito(_sim, transform);
            // (fix Cesar playtest 34, "GRIFOS Y PILAS SE MUDAN POR SEPARADO",
            // tarea b) LAS PILAS: mismo patrón exacto que Balda -- geometría
            // del mundo, no herramienta del jugador, spawneada UNA vez con el
            // mismo guardián host-only/flag-estático que Balda/Anclaje. Antes
            // de esta ronda `Sim/SimLevelBuilder.BuildPilasFuentes` tallaba
            // las dos "U" de piedra directamente en el génesis, sin ningún
            // objeto que las representara: mover el grifo (que SÍ es
            // IMovible) no movía la pila -- eso también fue bug por omisión,
            // pero el síntoma que reportó Cesar es el opuesto ("la pila SIGUE
            // al grifo"), y la causa real de ESE síntoma es que
            // Game/Dispenser.cs dibujaba el marco decorativo de la pila como
            // HIJO de su propio transform (ver el docblock de
            // Dispenser.BuildPilaEnmarcada) -- un hijo se arrastra solo con
            // el padre. Con las pilas convertidas en objetos independientes
            // (ver Game/Pila.cs) las dos cosas quedan resueltas a la vez: el
            // grifo ya no tiene ningún hijo que represente a la pila, y la
            // pila tiene su propio Reposicionar.
            Pila.SpawnTodas(_sim);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;

            // Mismas guardas que Flask.Update/Cincel.Update, en el mismo
            // orden (regla 12 de CLAUDE.md).
            if (DayCycle.InputLocked) { OcultarVisuales(); return; }
            if (Alkahest.Dev.DevPalette.IsOpen) { OcultarVisuales(); return; }
            if (UiStyles.EscribiendoTexto) { OcultarVisuales(); return; }
            if (JournalHud.Abierto || AlbumReal.Abierto) { OcultarVisuales(); return; } // (integración pt50, regla 12) también la ficha modal del álbum.

            var kb = Keyboard.current;
            var mouse = Mouse.current;

            // Alternar modo (V). Ver el docblock de la clase, "LOS TRES MODOS
            // EXCLUSIVOS": mientras el Cincel está en la mano, V no hace nada
            // (con aviso) -- Mudanza nunca se activa por encima de él.
            if (kb != null && kb.vKey.wasPressedThisFrame)
            {
                if (Cincel.ModoActivo)
                {
                    if (_flask != null) _flask.Avisar("suelta antes el cincel (C)");
                }
                else
                {
                    ModoActivo = !ModoActivo;
                    if (!ModoActivo) CancelarYSoltar(); // salir del modo con algo en la mano: se suelta donde estaba (gratis, ver docblock).
                    if (_flask != null)
                    {
                        _flask.Avisar(ModoActivo
                            ? "mudanza en mano — clic izq. agarra/suelta, R cancela"
                            : "frasco en mano");
                    }
                    if (!ModoActivo) OcultarVisuales();
                }
            }

            if (!ModoActivo) { OcultarVisuales(); return; }

            // Red de seguridad: Game/Cincel.cs es de solo lectura en este
            // encargo y NO comprueba Mudanza.ModoActivo (ver docblock, "LO QUE
            // FALTA"). Si el jugador activa el cincel MIENTRAS ya lleva un
            // aparato agarrado, Mudanza se apaga sola en vez de dejar las dos
            // herramientas respondiendo a la vez.
            if (Cincel.ModoActivo)
            {
                CancelarYSoltar();
                ModoActivo = false;
                OcultarVisuales();
                return;
            }

            // Misma razón que Flask/Cincel: sobre una redoma del estante los
            // clics son "guardar/recuperar", nunca una acción de mundo.
            bool ratonCapturado = StorageRack.RatonSobreRedoma();

            _hasCursorWorld = TryGetCursorWorld(out _cursorWorld);
            _hasCursorCell = _hasCursorWorld && CeldaDesdeCursorMundo(out _cursorCell);

            bool clicEsteFrame = mouse != null && mouse.leftButton.wasPressedThisFrame && !ratonCapturado;
            bool cancelarEsteFrame = kb != null && kb.rKey.wasPressedThisFrame;

            if (_llevando == null)
            {
                // (playtest 19) R con las manos vacías = deshacer del todo.
                // Misma tecla que cancelar un arrastre a propósito: para el
                // jugador es el mismo gesto ("devuélvelo"), solo cambia el
                // alcance según lleve algo o no.
                if (cancelarEsteFrame) DevolverTodoASuSitio();
                else if (clicEsteFrame) IntentarAgarrar();
            }
            else
            {
                if (cancelarEsteFrame) CancelarYSoltar();
                else if (clicEsteFrame) IntentarSoltar();
            }

            ActualizarVisuales();
        }

        /// <summary>Mismo raycast puro cámara-&gt;plano z=0 que Flask.TryGetCursorWorld/Cincel.TryGetCursorWorld (privado en ambos, así que se repite aquí: es de solo lectura en este encargo).</summary>
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

        private bool CeldaDesdeCursorMundo(out Vector2Int cell)
        {
            cell = _sim.WorldToCell(_cursorWorld);
            return CellGrid.InBounds(cell.x, cell.y);
        }

        private bool DentroDeAlcance(Vector3 puntoMundo)
        {
            return _apprentice != null
                && Vector3.Distance(_apprentice.transform.position, puntoMundo) <= Flask.ReachWorld;
        }

        // ---------------------------------------------------------------------------------
        // AGARRAR: el aparato movible más cercano al cursor, dentro de su
        // propio radio de "hitbox" (TamanoMundo/2 + gracia) y del alcance del
        // jugador. Reutiliza el mismo criterio "más cercano gana" que
        // Game/MachineFocus.cs, pero sobre el CURSOR (no sobre el jugador,
        // ver el encargo: "clic izq. sobre el aparato más cercano al
        // cursor") y sobre la lista propia de Mudanza, no la de MachineFocus
        // (que es privada e inaccesible desde aquí).
        // ---------------------------------------------------------------------------------
        private void IntentarAgarrar()
        {
            if (!_hasCursorWorld)
            {
                if (_flask != null) _flask.Avisar("apunta dentro del taller para agarrar algo");
                return;
            }

            IMovible mejor = null;
            float mejorD2 = float.MaxValue;
            for (int i = _movibles.Count - 1; i >= 0; i--)
            {
                var m = _movibles[i];
                // Red de seguridad: un MonoBehaviour destruido sigue en la
                // lista hasta su OnDestroy (mismo razonamiento que
                // Game/MachineFocus.Foco -- ver ese archivo).
                var comoObjeto = m as UnityEngine.Object;
                if (comoObjeto == null)
                {
                    // (playtest 19) Los DOS arrays van en paralelo por índice:
                    // `_anclasDeFabrica[i]` es el sitio original de
                    // `_movibles[i]`. Esta limpieza quitaba solo de uno, y con
                    // eso bastaba para que la tecla R devolviera cada aparato
                    // al sitio de OTRO — que es bastante peor que no tener R.
                    // Cualquier borrado de esta lista tiene que tocar las dos.
                    _movibles.RemoveAt(i);
                    if (i < _anclasDeFabrica.Count) _anclasDeFabrica.RemoveAt(i);
                    continue;
                }

                float radio = Mathf.Max(m.TamanoMundo.x, m.TamanoMundo.y) * 0.5f + AgarreGraciaWorld;
                float d2 = (m.CentroMundo - _cursorWorld).sqrMagnitude;
                if (d2 > radio * radio) continue;
                if (d2 >= mejorD2) continue;

                mejorD2 = d2;
                mejor = m;
            }

            if (mejor == null)
            {
                if (_flask != null) _flask.Avisar("nada que agarrar ahí");
                return;
            }

            if (!DentroDeAlcance(mejor.CentroMundo))
            {
                if (_flask != null) _flask.Avisar("demasiado lejos — acércate");
                return;
            }

            if (!_hasCursorCell)
            {
                // No debería pasar (si el cursor está sobre un aparato del
                // taller, casi siempre cae dentro de la grilla), pero sin una
                // celda de cursor válida no hay forma de calcular un offset
                // de arrastre estable -- mejor negarse con aviso que arrastrar
                // desde un offset basura.
                if (_flask != null) _flask.Avisar("apunta dentro del taller para agarrar algo");
                return;
            }

            // (fix Cesar playtest 33, MULTI) EL CERROJO: "no es necesario que
            // otros vean el movimiento; basta con que impida que otro mueva
            // algo que alguien ya está moviendo, con un aviso". Solo tiene
            // efecto real para los tipos que viven en el registro de
            // Net/MaquinaSync.cs (las cinco estaciones/grifos de siempre +
            // Balda/Anclaje desde esta ronda) -- para cualquier otro IMovible
            // (HeatPlate/ChillStone/Criatura/Capullo/StorageRack/Alambique...)
            // Net.MaquinaSync.EstaBloqueadoPorOtro siempre devuelve false, así
            // que este bloque es un no-op transparente para ellos.
            if (MaquinaSync.EstaBloqueadoPorOtro(mejor))
            {
                if (_flask != null) _flask.Avisar("lo está moviendo otro alquimista");
                return;
            }
            MaquinaSync.PedirBloqueo(mejor);

            _llevando = mejor;
            _offsetArrastreCeldas = mejor.AnclaCelda - _cursorCell;
            if (_flask != null) _flask.Avisar("agarrado — clic izq. suelta, R cancela");
        }

        // ---------------------------------------------------------------------------------
        // SOLTAR: solo si el sitio candidato es válido (alcance + dentro del
        // mundo, ver docblock de la clase). Es la ÚNICA llamada a
        // IMovible.Reposicionar de toda esta clase.
        // ---------------------------------------------------------------------------------
        private void IntentarSoltar()
        {
            if (_llevando == null) return;

            if (!_hasCursorCell)
            {
                if (_flask != null) _flask.Avisar("apunta dentro del taller para soltarlo");
                return;
            }

            Vector2Int anclaCandidata = _cursorCell + _offsetArrastreCeldas;

            if (!DentroDeAlcance(_cursorWorld))
            {
                if (_flask != null) _flask.Avisar("demasiado lejos — acércate");
                return;
            }
            if (!_llevando.CabeEnAncla(anclaCandidata))
            {
                if (_flask != null) _flask.Avisar("no cabe ahí — se saldría del taller");
                return;
            }

            _llevando.Reposicionar(anclaCandidata);
            MaquinaSync.PedirLiberar(_llevando); // (fix Cesar playtest 33, MULTI) el cerrojo se suelta AQUÍ, con el aparato ya en su sitio final -- no antes.
            if (_flask != null) _flask.Avisar("colocado");
            _llevando = null;
        }

        /// <summary>
        /// Cancela el arrastre en curso (R, o salir del modo con V mientras se
        /// lleva algo). GRATIS por diseño: como el aparato real nunca se tocó
        /// mientras se arrastraba (solo la silueta genérica seguía al cursor,
        /// ver docblock de la clase), "cancelar" es simplemente soltar la
        /// referencia -- el aparato ya estaba, y sigue estando, donde estaba.
        /// No hay ningún Reposicionar que deshacer.
        /// </summary>
        private void CancelarYSoltar()
        {
            if (_llevando == null) return;
            MaquinaSync.PedirLiberar(_llevando); // (fix Cesar playtest 33, MULTI) cancelar también suelta el cerrojo -- si no, quedaría "agarrado" para siempre a ojos de los demás.
            if (_flask != null) _flask.Avisar("mudanza cancelada");
            _llevando = null;
        }

        /// <summary>
        /// (playtest 19) Apaga la mudanza desde fuera, soltando sin mover lo
        /// que llevara en la mano. Lo llama <see cref="Cincel"/> al encenderse:
        /// los tres modos (frasco / cincel / mudanza) son EXCLUYENTES, y la
        /// exclusión estaba a medias — esta clase sí cedía ante
        /// `Cincel.ModoActivo`, pero pulsar C con la mudanza activa dejaba a
        /// los dos en pie el mismo frame, con un aparato colgando del cursor
        /// mientras el cincel ya tallaba piedra.
        ///
        /// Es estático y sin argumentos a propósito: `Cincel` no tiene (ni
        /// debe tener) una referencia a esta clase, igual que esta no la tiene
        /// a `Cincel`. Si algún día hay un cuarto modo, que use esta misma
        /// puerta en vez de inventarse otra.
        /// </summary>
        public static void ForzarSalida()
        {
            if (!ModoActivo) return;
            ModoActivo = false;
            var instancia = FindAnyObjectByType<Mudanza>();
            if (instancia != null)
            {
                instancia.CancelarYSoltar();
                instancia.OcultarVisuales();
            }
        }

        // ===================================================================
        // VISUAL: silueta genérica de arrastre + icono de modo permanente.
        // Todo creado UNA vez en BuildVisuals(); Update() solo mueve/tiñe,
        // cero asignaciones por frame -- mismo patrón que Game/Cincel.cs.
        // ===================================================================

        private void BuildVisuals()
        {
            var sprite = MaquinariaSprites.Solido(); // 1x1 blanco, cacheado -- mismo sprite que usa Flask.CarryVisual/StorageRack.

            var previewGo = new GameObject("MudanzaSilueta");
            previewGo.transform.SetParent(transform, false);
            _previewTr = previewGo.transform;
            _previewSr = previewGo.AddComponent<SpriteRenderer>();
            _previewSr.sprite = sprite;
            _previewSr.sortingOrder = PreviewSortingOrder;
            _previewSr.color = new Color(0f, 0f, 0f, 0f);

            // Icono de modo: mismo lenguaje que Cincel.CincelIconoModo (un
            // "diamante" de latón junto al aprendiz), tinte distinto (ver
            // IconoMudanza) para que nunca se confunda con el del Cincel --
            // aunque, al ser modos mutuamente excluyentes, jamás coinciden.
            var iconoSprite = sprite;
            var iconoGo = new GameObject("MudanzaIconoModo");
            iconoGo.transform.SetParent(transform, false);
            iconoGo.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            iconoGo.transform.localScale = Vector3.one * 0.14f;
            _modeIconSr = iconoGo.AddComponent<SpriteRenderer>();
            _modeIconSr.sprite = iconoSprite;
            _modeIconSr.sortingOrder = ModeIconSortingOrder;
            _modeIconSr.color = new Color(0f, 0f, 0f, 0f);
        }

        /// <summary>Apaga todos los visuales de golpe. Se llama en los `return` tempranos de Update para que nada quede pegado en pantalla mientras Mudanza no está activa -- mismo patrón que Flask.OcultarVisualesDeMundo/Cincel.OcultarVisuales.</summary>
        private void OcultarVisuales()
        {
            if (_previewSr != null) _previewSr.color = new Color(0f, 0f, 0f, 0f);
            if (_modeIconSr != null) _modeIconSr.color = new Color(0f, 0f, 0f, 0f);
            _sinCubeta = false;
        }

        private void ActualizarVisuales()
        {
            // Icono de modo: encendido por estar en modo Mudanza, sin importar
            // si se lleva algo agarrado -- es el indicador permanente de "qué
            // llevas en la mano" (mismo criterio que el icono del Cincel).
            if (_modeIconSr != null)
            {
                Vector3 anchor = _apprentice != null ? _apprentice.CarryAnchor : transform.position;
                Vector3 iconPos = anchor + new Vector3(0f, 0.36f, -0.03f);
                _modeIconSr.transform.position = iconPos;
                _modeIconSr.color = new Color(IconoMudanza.r / 255f, IconoMudanza.g / 255f, IconoMudanza.b / 255f, ModeIconAlpha);
            }

            if (_llevando == null || !_hasCursorWorld)
            {
                if (_previewSr != null) _previewSr.color = new Color(0f, 0f, 0f, 0f);
                _sinCubeta = false;
                return;
            }

            Vector2Int anclaCandidata = _hasCursorCell ? _cursorCell + _offsetArrastreCeldas : _llevando.AnclaCelda;
            bool dentroDelMundo = _hasCursorCell && _llevando.CabeEnAncla(anclaCandidata);
            bool dentroDeAlcance = DentroDeAlcance(_cursorWorld);
            bool valido = dentroDelMundo && dentroDeAlcance;

            Vector2 tamano = _llevando.TamanoMundo;

            // (playtest 29, encargo D) LA SOMBRA ALINEADA: para las cinco
            // estaciones del taller (que implementan IMovibleAnclaEsquina,
            // ver ese docblock) la silueta se pega a la huella REAL que
            // tallaría Reposicionar -- esquina inferior izquierda =
            // anclaCandidata (en celdas -> mundo), centro = esquina + mitad
            // del tamaño. Es la MISMA aritmética que cada estación usa para
            // calcular su propio `_centro` a partir de su rect exterior (ver
            // Game/Crisol.cs `RecalcularRegiones`), así que a igual
            // anclaCandidata la sombra cae exactamente donde caerá la
            // mampostería nueva. Antes de esta ronda la silueta SIEMPRE
            // seguía al cursor sin tener en cuenta el offset de agarre --
            // Cesar: "la sombra... debería cubrirlas y servir para
            // reposicionarlas... pequeña y/o desalineada". Para el resto de
            // aparatos (Dispenser/Criatura/Capullo, sin esa garantía porque
            // su AnclaCelda no es una esquina) se conserva el comportamiento
            // ORIGINAL -- centrada en el cursor, "se puede soltar en el
            // aire" -- que es todo lo que Mudanza puede hacer sin conocer su
            // forma real de anclarse.
            if (_llevando is IMovibleAnclaEsquina && _hasCursorCell)
            {
                float c = SimRenderer.CellWorldSize;
                _previewTr.position = new Vector3(
                    anclaCandidata.x * c + tamano.x * 0.5f,
                    anclaCandidata.y * c + tamano.y * 0.5f,
                    _cursorWorld.z);
            }
            else
            {
                _previewTr.position = _cursorWorld; // la silueta sigue al cursor -- se puede soltar "en el aire" (ver docblock de la clase).
            }
            _previewTr.localScale = new Vector3(Mathf.Max(0.02f, tamano.x), Mathf.Max(0.02f, tamano.y), 1f);

            Color32 colorBase = valido ? ColorValido : ColorInvalido;
            float alfa = valido ? AlfaPreviewValido : AlfaPreviewInvalido;
            _previewSr.color = new Color(colorBase.r / 255f, colorBase.g / 255f, colorBase.b / 255f, alfa);

            // "Sin cubeta aquí" (ver docblock, "VALIDEZ DEL SITIO"): NO afecta
            // al color ni bloquea soltar, solo enciende el rótulo de OnGUI.
            _sinCubeta = valido && _hasCursorCell
                && _sim.SampleMaterial(anclaCandidata.x, anclaCandidata.y) != MaterialId.Stone;
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked) return;
            if (!ModoActivo || _llevando == null || !_hasCursorWorld || !_sinCubeta) return;

            UiStyles.Preparar();
            UiStyles.PlacaMundo(_cursorWorld, "sin cubeta aquí — tendrás que construirla",
                UiStyles.Aviso, UiStyles.S(30f));
        }
    }
}
