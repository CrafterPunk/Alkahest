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
    /// (R99, Cesar: "el contorno de los contenedores no es exacto, no lo
    /// cubre con su tubo de refill") El aparato que NO es un rectángulo
    /// puede dictar su propia silueta: un polígono RECTILÍNEO (solo aristas
    /// horizontales/verticales, como todo lo que talla la sim) en unidades
    /// de MUNDO, sentido HORARIO con el eje Y hacia arriba. Mudanza lo usa
    /// únicamente para el trazo del contorno punteado (DibujarContornos) —
    /// la detección de agarre, la silueta de arrastre y CabeEnAncla siguen
    /// midiendo con CentroMundo/TamanoMundo, que es honesto: la mano agarra
    /// el cuerpo, no el tubo. Devolver false = "hoy soy un rectángulo"
    /// (fallback al rect visual genérico), para que el aparato no tenga que
    /// duplicar la fórmula del rect cuando su accesorio aún no existe.
    /// </summary>
    public interface IMovibleSilueta : IMovible
    {
        bool PerimetroVisual(List<Vector2> puntos);

        /// <summary>
        /// (R100, Cesar: "el cuadrado verde no tiene la forma exacta del
        /// contorno") La MISMA silueta pero RELATIVA al ancla visual (el
        /// (0,0) es la esquina del rect que mide TamanoMundo) y con el
        /// espejo PEDIDO — no el vigente: la silueta de arrastre enseña lo
        /// que va a pasar al soltar, no lo que hay. Mudanza la rasteriza a
        /// una textura de 1 px por celda al agarrar y al espejar (L).
        /// </summary>
        bool SiluetaRelativa(bool espejado, List<Vector2> puntos);
    }

    /// <summary>
    /// (R100, Cesar: "presioné la L para espejar pero no vi nada") El
    /// aparato con un accesorio de flanco (hoy: el tubo de refill del
    /// depósito) que puede vivir a izquierda o derecha. La práctica
    /// estándar de todo modo de colocación (RimWorld/Factorio/Sims rotan
    /// con R/tecla mientras llevas el fantasma): L espeja — llevándolo, el
    /// deseo queda PENDIENTE y la silueta voltea al instante; sobre un
    /// candidato sin agarrar, se espeja EN SITIO. El deseo se honra si hay
    /// aire en ese flanco; si no, el accesorio se queda y el aviso lo dice.
    /// </summary>
    public interface IMovibleEspejable : IMovible
    {
        /// <summary>¿El accesorio vive HOY en el flanco izquierdo?</summary>
        bool EspejadoHoy { get; }

        /// <summary>El deseo del jugador (izquierda = true). Lo escribe Mudanza; Reposicionar lo honra si hay aire.</summary>
        bool EspejoPendiente { get; set; }

        /// <summary>Espeja EN SITIO (sin mudanza) honrando EspejoPendiente. Devuelve si el accesorio quedó en el flanco deseado.</summary>
        bool AplicarEspejoAhora();
    }

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

        /// <summary>
        /// (R98, dirección Opus) EL ESTADO COMO FLOAT: 0..1, entrada en
        /// 0.22 s y salida en 0.14 s (salir siempre más rápido: nunca
        /// esperas para dejar un modo). Gobierna TODO el vestido del modo —
        /// la vista de plano (SimRenderer.TinteMudanza), el fundido cruzado
        /// retícula↔puntero (FlaskHud lo lee), los contornos y la placa.
        /// MoveTowards y no Lerp: llega a 0 y 1 EXACTOS, así "no dibujar
        /// nada" es EstadoT &lt;= 0 limpio, sin epsilon.
        /// </summary>
        public static float EstadoT { get; private set; }
        // (R98) El segundo float: el foco del agarre. Al levantar algo, su
        // contorno se disuelve (0.12 s) y el censo del resto retrocede a 40%;
        // al soltar, todo vuelve (0.15 s). Evento menor que entrar al modo:
        // números deliberadamente más cortos.
        private float _focoT;

        // (R98) El candidato bajo el cursor, calculado UNA vez por frame en
        // Update (nunca en OnGUI: corre 2+ veces por frame) y consumido por
        // IntentarAgarrar Y por el puntero — la mano no puede mentir sobre
        // lo que el clic va a hacer: es el mismo código.
        private IMovible _candidato;
        private bool _candidatoEnAlcance;
        private bool _sitioValidoCache; // validez del sitio mientras se lleva algo (la calcula ActualizarVisuales).

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

        // (RONDA 76, revisión Opus #5) EL MODO ES DE LA SESIÓN, NO DEL
        // PROCESO: la estática quedaba encendida tras la recarga de escena
        // del fin de sesión multi — recuperable con V, pero una sesión no debe heredar el modo de otra.
        private void OnDestroy()
        {
            // Solo el componente ACTIVO (el del jugador local) es dueño del
            // modo: los avatares remotos llevan este componente apagado
            // (AprendizNet.Cablear) y su Destroy al desconectarse un amigo no
            // debe apagar el TUYO a mitad de sesión.
            if (enabled) ModoActivo = false;
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;

            // (R98) Los relojes del estado corren SIEMPRE (también saliendo
            // del modo: el fade-out vive aquí), y la vista de plano se
            // escribe suavizada (smoothstep inline, cero allocs).
            if (enabled)
            {
                EstadoT = Mathf.MoveTowards(EstadoT, ModoActivo ? 1f : 0f, Time.deltaTime / (ModoActivo ? 0.22f : 0.14f));
                _focoT = Mathf.MoveTowards(_focoT, _llevando != null ? 1f : 0f, Time.deltaTime / (_llevando != null ? 0.12f : 0.15f));
                SimRenderer.TinteMudanza = EstadoT * EstadoT * (3f - 2f * EstadoT);
            }

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

            // (R98) El censo del candidato para el puntero y el agarre.
            _candidato = null;
            _candidatoEnAlcance = false;
            if (_llevando == null && _hasCursorWorld)
                _candidato = BuscarCandidato(out _candidatoEnAlcance);

            bool clicEsteFrame = mouse != null && mouse.leftButton.wasPressedThisFrame && !ratonCapturado;
            bool cancelarEsteFrame = kb != null && kb.rKey.wasPressedThisFrame;
            bool espejarEsteFrame = kb != null && kb.lKey.wasPressedThisFrame && !UiStyles.EscribiendoTexto;

            if (_llevando == null)
            {
                // (playtest 19) R con las manos vacías = deshacer del todo.
                // Misma tecla que cancelar un arrastre a propósito: para el
                // jugador es el mismo gesto ("devuélvelo"), solo cambia el
                // alcance según lleve algo o no.
                if (cancelarEsteFrame) DevolverTodoASuSitio();
                else if (clicEsteFrame) IntentarAgarrar();
                else if (espejarEsteFrame) EspejarEnSitio(); // (R100) L sobre un candidato: espeja SIN mudanza.
            }
            else
            {
                if (cancelarEsteFrame) CancelarYSoltar();
                else if (clicEsteFrame) IntentarSoltar();
                else if (espejarEsteFrame) EspejarLlevado(); // (R100) L llevando: el deseo queda pendiente y la silueta voltea YA.
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
        /// <summary>
        /// (R98) EL CENSO DEL CANDIDATO, extraído de IntentarAgarrar: el
        /// aparato más cercano al cursor dentro de su hitbox, con la misma
        /// limpieza de destruidos de siempre. Lo consumen el AGARRE y el
        /// PUNTERO en el mismo frame — una sola pasada, una sola verdad.
        /// </summary>
        private IMovible BuscarCandidato(out bool enAlcance)
        {
            enAlcance = false;
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
            if (mejor != null) enAlcance = DentroDeAlcance(mejor.CentroMundo);
            return mejor;
        }

        private void IntentarAgarrar()
        {
            if (!_hasCursorWorld)
            {
                if (_flask != null) _flask.Avisar("apunta dentro del taller para agarrar algo");
                return;
            }

            // (R98) La misma pasada que ya vio el puntero este frame.
            var mejor = _candidato;

            if (mejor == null)
            {
                if (_flask != null) _flask.Avisar("nada que agarrar ahí");
                return;
            }

            if (!_candidatoEnAlcance)
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
            // (R100) El espejo del arrastre arranca COMO ESTÁ el aparato hoy
            // (la silueta no miente al agarrar) y la forma se hornea una vez.
            var espejable = mejor as IMovibleEspejable;
            _espejoLlevando = espejable != null && espejable.EspejadoHoy;
            ConstruirFormaSilueta();
            if (_flask != null)
                _flask.Avisar(espejable != null
                    ? "agarrado — clic suelta, L espeja, R cancela"
                    : "agarrado — clic izq. suelta, R cancela");
        }

        /// <summary>
        /// (R100) L mientras llevas algo: alterna el deseo de flanco. La
        /// silueta voltea EN ESTE FRAME (se rehornea la forma) — la práctica
        /// de todo modo de colocación: el fantasma responde a la tecla al
        /// instante, el mundo espera al soltar.
        /// </summary>
        private void EspejarLlevado()
        {
            if (!(_llevando is IMovibleEspejable))
            {
                if (_flask != null) _flask.Avisar("este aparato no tiene lado — nada que espejar");
                return;
            }
            _espejoLlevando = !_espejoLlevando;
            ConstruirFormaSilueta();
            if (_flask != null)
                _flask.Avisar(_espejoLlevando ? "espejado — el tubo al flanco izquierdo" : "espejado — el tubo al flanco derecho");
        }

        /// <summary>
        /// (R100) L sobre un candidato con las manos vacías: espejo EN SITIO,
        /// sin agarrar — el accesorio salta de flanco ahí mismo si hay aire.
        /// </summary>
        private void EspejarEnSitio()
        {
            var esp = _candidato as IMovibleEspejable;
            if (esp == null)
            {
                if (_flask != null) _flask.Avisar(_candidato == null
                    ? "apunta a un aparato — L espeja su accesorio"
                    : "este aparato no tiene lado — nada que espejar");
                return;
            }
            if (!_candidatoEnAlcance)
            {
                if (_flask != null) _flask.Avisar("demasiado lejos — acércate para espejarlo");
                return;
            }
            esp.EspejoPendiente = !esp.EspejadoHoy;
            bool logrado = esp.AplicarEspejoAhora();
            if (_flask != null)
                _flask.Avisar(logrado
                    ? (esp.EspejadoHoy ? "espejado — el tubo al flanco izquierdo" : "espejado — el tubo al flanco derecho")
                    : "no hay aire en el otro flanco — despeja ese lado primero");
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

            // (RONDA 68, mandato 2.5D de Cesar: "una máquina solo puede
            // colocarse cuando existe superficie estructural válida debajo...
            // no quiero máquinas flotando sobre irregularidades absurdas")
            // SOLO para las ESTACIONES (IMovibleAnclaEsquina: su ancla ES la
            // esquina inferior-izquierda de su huella real, garantía del
            // pt29): la fila bajo la huella debe ser >=70% roca madre / piso
            // estructural / obra. Los aparatos de PARED (grifos: su ancla es
            // la boquilla) y los seres (criatura/capullo: ancla de cuna)
            // quedan fuera a propósito -- su ancla no describe una huella de
            // suelo y exigirles piso rompería su colocación de siempre.
            if (!ApoyoFirme(_llevando, anclaCandidata))
            {
                if (_flask != null) _flask.Avisar("necesita apoyo firme — construye piso o roca debajo (cincel: C, luego X)");
                return;
            }

            // (R100) El deseo de flanco viaja con el soltar: Reposicionar lo
            // honra si hay aire (y si no, el tubo se queda donde pueda).
            var espejable = _llevando as IMovibleEspejable;
            if (espejable != null) espejable.EspejoPendiente = _espejoLlevando;

            _llevando.Reposicionar(anclaCandidata);
            MaquinaSync.PedirLiberar(_llevando); // (fix Cesar playtest 33, MULTI) el cerrojo se suelta AQUÍ, con el aparato ya en su sitio final -- no antes.
            if (_flask != null) _flask.Avisar("colocado");
            _llevando = null;
        }

        /// <summary>
        /// (RONDA 68, mandato 2.5D de Cesar; R100, la cirugía) "Una máquina
        /// solo puede colocarse cuando existe superficie estructural válida
        /// debajo". SOLO estaciones IMovibleAnclaEsquina (su ancla ES la
        /// esquina de su huella): la fila bajo la huella debe ser >=70% roca/
        /// piso/obra. (R100, Cesar: "puedo colocar las cosas volando con un
        /// poco de paciencia") EL APARATO NO SE APOYA EN SÍ MISMO: como el
        /// aparato real se queda plantado mientras lo llevas (ver docblock),
        /// su propio muro inferior era Stone+obra a un renglón de altura — se
        /// colocaba, se volvía a agarrar, una línea más arriba, y así hasta
        /// la torre voladora. Toda celda dentro de su rect visual ACTUAL
        /// cuenta como evaluada pero JAMÁS como apoyo.
        /// </summary>
        private bool ApoyoFirme(IMovible m, Vector2Int anclaCandidata)
        {
            if (!(m is IMovibleAnclaEsquina)) return true; // aparatos de pared/seres: su ancla no describe huella de suelo.
            float cM = SimRenderer.CellWorldSize;
            int anchoCeldas = Mathf.Max(1, Mathf.RoundToInt(m.TamanoMundo.x / cM));
            Vector2Int anclaHoy = m.AnclaCelda;
            int wHoy = Mathf.Max(1, Mathf.RoundToInt(m.TamanoMundo.x / cM));
            int hHoy = Mathf.Max(1, Mathf.RoundToInt(m.TamanoMundo.y / cM));
            int apoyo = 0, evaluadas = 0;
            for (int i = 0; i < anchoCeldas; i++)
            {
                int xx = anclaCandidata.x + i, yy = anclaCandidata.y - 1;
                if (!CellGrid.InBounds(xx, yy)) continue;
                evaluadas++;
                bool propio = xx >= anclaHoy.x && xx < anclaHoy.x + wHoy
                           && yy >= anclaHoy.y && yy < anclaHoy.y + hHoy;
                if (propio) continue; // tu propia mampostería no es suelo.
                int mat = _sim.SampleMaterial(xx, yy);
                if (mat == MaterialId.Stone || mat == MaterialId.PisoEstructural || SimLevelBuilder.EsObraDelTaller(xx, yy)) apoyo++;
            }
            return evaluadas == 0 || apoyo * 100 >= evaluadas * 70;
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

        // =================================================================
        // (R100) LA SOMBRA CON FORMA: al agarrar (y al espejar con L) se
        // hornea UNA textura de 1 px por celda con la silueta exacta del
        // aparato (SiluetaRelativa: la L con su tubo, del lado pedido) y el
        // preview la usa en vez del 1x1 estirado — "el cuadrado verde no
        // tiene la forma exacta del contorno" muere aquí. Cero costo por
        // frame: la textura solo se rehornea al agarrar o al pulsar L.
        // =================================================================
        private Texture2D _formaTex;
        private Sprite _formaSprite;
        private Vector2 _formaOffset;   // bbox.min RELATIVO al ancla candidata (mundo).
        private Vector2 _formaTam;      // tamaño mundo del bbox horneado.
        private bool _formaViva;        // ¿el preview lleva forma propia este arrastre?
        private bool _espejoLlevando;   // deseo de flanco del arrastre en curso (L lo alterna).
        private static readonly List<Vector2> _formaPts = new List<Vector2>(8);

        private static bool DentroDePoligono(List<Vector2> poli, Vector2 p)
        {
            bool dentro = false;
            for (int i = 0, j = poli.Count - 1; i < poli.Count; j = i++)
            {
                if ((poli[i].y > p.y) != (poli[j].y > p.y)
                    && p.x < (poli[j].x - poli[i].x) * (p.y - poli[i].y) / (poli[j].y - poli[i].y) + poli[i].x)
                    dentro = !dentro;
            }
            return dentro;
        }

        private void ConstruirFormaSilueta()
        {
            _formaViva = false;
            if (_previewSr == null) return;
            var conSilueta = _llevando as IMovibleSilueta;
            _formaPts.Clear();
            if (conSilueta == null || !conSilueta.SiluetaRelativa(_espejoLlevando, _formaPts) || _formaPts.Count < 4)
            {
                _previewSr.sprite = MaquinariaSprites.Solido(); // el rect genérico de siempre.
                return;
            }
            float c = SimRenderer.CellWorldSize;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < _formaPts.Count; i++)
            {
                minX = Mathf.Min(minX, _formaPts[i].x); maxX = Mathf.Max(maxX, _formaPts[i].x);
                minY = Mathf.Min(minY, _formaPts[i].y); maxY = Mathf.Max(maxY, _formaPts[i].y);
            }
            int w = Mathf.Max(1, Mathf.RoundToInt((maxX - minX) / c));
            int h = Mathf.Max(1, Mathf.RoundToInt((maxY - minY) / c));
            if (_formaTex == null || _formaTex.width != w || _formaTex.height != h)
            {
                if (_formaSprite != null) Destroy(_formaSprite);
                if (_formaTex != null) Destroy(_formaTex);
                _formaTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                _formaSprite = null;
            }
            var px = new Color32[w * h];
            var lleno = new Color32(255, 255, 255, 255);
            var nada = new Color32(0, 0, 0, 0);
            for (int iy = 0; iy < h; iy++)
                for (int ix = 0; ix < w; ix++)
                {
                    var centroCelda = new Vector2(minX + (ix + 0.5f) * c, minY + (iy + 0.5f) * c);
                    px[iy * w + ix] = DentroDePoligono(_formaPts, centroCelda) ? lleno : nada;
                }
            _formaTex.SetPixels32(px);
            _formaTex.Apply(false, false);
            if (_formaSprite == null)
                _formaSprite = Sprite.Create(_formaTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f / c);
            _previewSr.sprite = _formaSprite;
            _formaOffset = new Vector2(minX, minY);
            _formaTam = new Vector2(w * c, h * c);
            _formaViva = true;
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
            // (R100) LA SILUETA NO MIENTE: el apoyo firme entra a la validez
            // visual — antes la sombra se pintaba verde sobre el aire y el
            // soltar la desmentía con un aviso. Verde = se puede soltar AQUÍ,
            // con las tres verdades juntas (mundo + alcance + suelo).
            bool valido = dentroDelMundo && dentroDeAlcance && (!_hasCursorCell || ApoyoFirme(_llevando, anclaCandidata));
            _sitioValidoCache = valido; // (R98) el puntero (mano cerrada verde/roja) habla con ESTA misma verdad.

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
                if (_formaViva)
                {
                    // (R100) LA SOMBRA CON FORMA: el sprite horneado por
                    // ConstruirFormaSilueta (la L con su tubo, ya espejada si
                    // L lo pidió) — su bbox se pega al ancla candidata igual
                    // que la sombra rectangular de siempre.
                    _previewTr.position = new Vector3(
                        anclaCandidata.x * c + _formaOffset.x + _formaTam.x * 0.5f,
                        anclaCandidata.y * c + _formaOffset.y + _formaTam.y * 0.5f,
                        _cursorWorld.z);
                    _previewTr.localScale = Vector3.one;
                }
                else
                {
                    _previewTr.position = new Vector3(
                        anclaCandidata.x * c + tamano.x * 0.5f,
                        anclaCandidata.y * c + tamano.y * 0.5f,
                        _cursorWorld.z);
                    _previewTr.localScale = new Vector3(Mathf.Max(0.02f, tamano.x), Mathf.Max(0.02f, tamano.y), 1f);
                }
            }
            else
            {
                _previewTr.position = _cursorWorld; // la silueta sigue al cursor -- se puede soltar "en el aire" (ver docblock de la clase).
                _previewTr.localScale = _formaViva
                    ? Vector3.one
                    : new Vector3(Mathf.Max(0.02f, tamano.x), Mathf.Max(0.02f, tamano.y), 1f);
            }

            Color32 colorBase = valido ? ColorValido : ColorInvalido;
            float alfa = valido ? AlfaPreviewValido : AlfaPreviewInvalido;
            _previewSr.color = new Color(colorBase.r / 255f, colorBase.g / 255f, colorBase.b / 255f, alfa);

            // "Sin cubeta aquí" (ver docblock, "VALIDEZ DEL SITIO"): NO afecta
            // al color ni bloquea soltar, solo enciende el rótulo de OnGUI.
            _sinCubeta = valido && _hasCursorCell
                && _sim.SampleMaterial(anclaCandidata.x, anclaCandidata.y) != MaterialId.Stone;
        }

        // =================================================================
        // (R98, dirección Opus completa) EL ESTADO HECHO VISIBLE — cuatro
        // piezas, un solo color (azul-frío 120/168/196 = LA MUDANZA):
        //   · EL PUNTERO: retícula IMGUI propia (cero Cursor.SetCursor: el
        //     juego no usa cursor de hardware en 50 archivos y S() no
        //     escala uno). Tres formas pixel-art 13x13 con hotspot FIJO en
        //     (6,6): mano abierta (hay agarrable), mano cerrada (llevando),
        //     cruceta (mover). El contorno SIEMPRE en el casi-negro del imp
        //     y el puño SIEMPRE de latón: anclas que no se tiñen; el relleno
        //     habla — latón=puedes, rojo=no llegas, verde/rojo=el veredicto
        //     del sitio (ColorValido/ColorInvalido LITERALES: la mano y la
        //     silueta dicen lo mismo porque SON lo mismo).
        //   · LOS CONTORNOS PUNTEADOS (R99, tras el veredicto "tosco y sin
        //     gracia"): cada IMovible lleva su trazo de delineante (guion
        //     S(5), hueco S(4)) sobre su SILUETA REAL — el polígono de
        //     IMovibleSilueta si el aparato dicta uno (el depósito incluye
        //     su tubo de refill: forma de L), o el rect visual genérico si
        //     no. Y el trazo DESFILA: hormigas marchantes de manual
        //     (Photoshop/RimWorld — la práctica universal para "esto está
        //     seleccionable/en proceso"), un periodo S(9) por segundo en
        //     sentido horario, TRASLACIÓN pura — el veto R81 #13 sigue en
        //     pie: el alfa jamás pulsa, lo que se mueve es la tinta. El
        //     candidato bajo el cursor cambia su corrida a LATÓN (el mismo
        //     latón del puño y del relleno "puedes" del puntero: un solo
        //     vocabulario) y sube a 0.85; el resto en alcance 0.55, lejos
        //     0.28 — escalones, sin degradado: regla, no iluminación.
        //     Nacen con stagger (0.035 s, cap 0.28) y mueren juntos.
        //   · Al AGARRAR (_focoT): el contorno del agarrado se disuelve
        //     dentro de su silueta (0.12 s) y el censo del resto retrocede
        //     a 40% — el cuarto entero cuenta el gesto.
        //   · LA VISTA DE PLANO vive en SimRenderer.TinteMudanza (el
        //     sustrato se enfría a pizarra; las máquinas quedan a color: el
        //     plano nace del contraste). La viñeta manda intacta: el tinte
        //     es sprite (-5), muy por debajo de su IMGUI (50).
        // =================================================================
        private static Texture2D[] _punteroTex; // [abierta c/r/p, cerrada c/r/p, cruceta c/r]
        private static readonly Color PunteroContorno = new Color(0x16 / 255f, 0x10 / 255f, 0x1E / 255f); // ApprenticeController.ColOutline literal.
        private static readonly Color PunteroLaton = new Color(214f / 255f, 176f / 255f, 96f / 255f);     // ColBrassLight literal.

        private static Texture2D MascaraPuntero(bool[,] celdas)
        {
            var tex = new Texture2D(13, 13, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[13 * 13];
            for (int y = 0; y < 13; y++)
                for (int x = 0; x < 13; x++)
                    px[(12 - y) * 13 + x] = celdas[x, y] ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0); // fila 0 = arriba (convención de diseño) → textura invertida.
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        private static bool[,] ContornoDe(bool[,] cuerpo, bool[,] prohibido = null)
        {
            var c = new bool[13, 13];
            for (int y = 0; y < 13; y++)
                for (int x = 0; x < 13; x++)
                {
                    if (cuerpo[x, y]) continue;
                    if (prohibido != null && prohibido[x, y]) continue;
                    bool vecino = (x > 0 && cuerpo[x - 1, y]) || (x < 12 && cuerpo[x + 1, y])
                               || (y > 0 && cuerpo[x, y - 1]) || (y < 12 && cuerpo[x, y + 1]);
                    if (vecino) c[x, y] = true;
                }
            return c;
        }

        private static void ConstruirPunteros()
        {
            _punteroTex = new Texture2D[8];

            // MANO ABIERTA: dedos 3/5/7/9 (los centrales más largos), pulgar, palma, puño de latón.
            var rel = new bool[13, 13];
            foreach (var (dx, desde) in new[] { (3, 2), (5, 1), (7, 1), (9, 2) })
                for (int y = desde; y <= 5; y++) rel[dx, y] = true;
            rel[2, 6] = true; rel[1, 7] = true; rel[1, 8] = true; // el pulgar.
            for (int x = 3; x <= 9; x++) for (int y = 5; y <= 9; y++) rel[x, y] = true;
            for (int x = 4; x <= 8; x++) rel[x, 10] = true; // redondeo.
            var puno = new bool[13, 13];
            for (int x = 4; x <= 8; x++) puno[x, 11] = true;
            for (int x = 5; x <= 7; x++) puno[x, 12] = true;
            var cuerpo = new bool[13, 13];
            for (int x = 0; x < 13; x++) for (int y = 0; y < 13; y++) cuerpo[x, y] = rel[x, y] || puno[x, y];
            _punteroTex[0] = MascaraPuntero(ContornoDe(cuerpo));
            _punteroTex[1] = MascaraPuntero(rel);
            _punteroTex[2] = MascaraPuntero(puno);

            // MANO CERRADA: mismo sobre-silueta y mismo puño — solo cambia el agarre.
            rel = new bool[13, 13];
            for (int x = 3; x <= 9; x++) for (int y = 4; y <= 10; y++) rel[x, y] = true;
            for (int x = 3; x <= 4; x++) for (int y = 6; y <= 8; y++) rel[x, y] = true; // pulgar cruzado.
            cuerpo = new bool[13, 13];
            for (int x = 0; x < 13; x++) for (int y = 0; y < 13; y++) cuerpo[x, y] = rel[x, y] || puno[x, y];
            var contC = ContornoDe(cuerpo);
            foreach (var yn in new[] { 5, 7, 9 }) // los tres pliegues de nudillo: lo que la hace "cerrada" a 26 px.
                for (int x = 4; x <= 8; x++) { contC[x, yn] = true; rel[x, yn] = false; }
            _punteroTex[3] = MascaraPuntero(contC);
            _punteroTex[4] = MascaraPuntero(rel);
            _punteroTex[5] = MascaraPuntero(puno);

            // CRUCETA: 4 puntas de flecha, hueco central con punto exacto.
            rel = new bool[13, 13];
            for (int y = 1; y <= 11; y++) rel[6, y] = true;
            for (int x = 1; x <= 11; x++) rel[x, 6] = true;
            rel[6, 0] = true; for (int x = 5; x <= 7; x++) rel[x, 1] = true; for (int x = 4; x <= 8; x++) rel[x, 2] = true;
            rel[6, 12] = true; for (int x = 5; x <= 7; x++) rel[x, 11] = true; for (int x = 4; x <= 8; x++) rel[x, 10] = true;
            rel[0, 6] = true; for (int y = 5; y <= 7; y++) rel[1, y] = true; for (int y = 4; y <= 8; y++) rel[2, y] = true;
            rel[12, 6] = true; for (int y = 5; y <= 7; y++) rel[11, y] = true; for (int y = 4; y <= 8; y++) rel[10, y] = true;
            var hueco = new bool[13, 13];
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0) { rel[6 + dx, 6 + dy] = false; hueco[6 + dx, 6 + dy] = true; } // 8 vecinos fuera; (6,6) queda: punto exacto + cruz.
            _punteroTex[6] = MascaraPuntero(ContornoDe(rel, hueco));
            _punteroTex[7] = MascaraPuntero(rel);
        }

        private void DibujarPuntero()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (_punteroTex == null) ConstruirPunteros();
            Vector2 mp = mouse.position.ReadValue();
            var gui = new Vector2(mp.x, Screen.height - mp.y);

            int baseIdx; Color relleno; float alfa;
            if (_llevando != null)
            {
                baseIdx = 3;
                relleno = _sitioValidoCache ? (Color)ColorValido : (Color)ColorInvalido;
                alfa = 1.00f;
            }
            else if (_candidato != null)
            {
                baseIdx = 0;
                relleno = _candidatoEnAlcance ? PunteroLaton : (Color)ColorInvalido;
                alfa = 0.95f;
            }
            else
            {
                baseIdx = 6;
                relleno = new Color(IconoMudanza.r / 255f, IconoMudanza.g / 255f, IconoMudanza.b / 255f);
                alfa = 0.85f;
            }
            alfa *= EstadoT;

            int zoom = Mathf.Max(2, Mathf.RoundToInt(UiStyles.Escala * 2f)); // entero SIEMPRE: pixel-art nítido con Point.
            var r = new Rect(gui.x - 6 * zoom, gui.y - 6 * zoom, 13 * zoom, 13 * zoom); // hotspot (6,6) en las TRES: el puntero jamás salta al cambiar de forma.
            var prev = GUI.color;
            GUI.color = new Color(PunteroContorno.r, PunteroContorno.g, PunteroContorno.b, alfa);
            GUI.DrawTexture(r, _punteroTex[baseIdx]);
            GUI.color = new Color(relleno.r, relleno.g, relleno.b, alfa);
            GUI.DrawTexture(r, _punteroTex[baseIdx + 1]);
            if (baseIdx < 6)
            {
                GUI.color = new Color(PunteroLaton.r, PunteroLaton.g, PunteroLaton.b, alfa);
                GUI.DrawTexture(r, _punteroTex[baseIdx + 2]);
            }
            GUI.color = prev;
        }

        // (R99) Búferes del trazo — reusados por frame, cero allocs en OnGUI.
        private static readonly List<Vector2> _poliMundo = new List<Vector2>(8);
        private static readonly List<Vector2> _poliPantalla = new List<Vector2>(8);
        private static readonly List<Vector2> _poliTmp = new List<Vector2>(8);

        /// <summary>
        /// (R99) Infla un polígono RECTILÍNEO horario (Y hacia arriba) hacia
        /// afuera: cada arista se desplaza `aire` por su normal exterior (la
        /// IZQUIERDA del avance, con este winding) y los vértices se
        /// recruzan — trivial con aristas de ejes: la vertical fija X, la
        /// horizontal fija Y. El cóncavo del hombro del tubo sale gratis.
        /// </summary>
        private static readonly bool[] _aristaVertical = new bool[16];

        private static void InflarPoligono(List<Vector2> p, float aire)
        {
            int n = p.Count;
            if (n > _aristaVertical.Length) return; // jamás en la práctica (L = 6 vértices).
            _poliTmp.Clear();
            // 1) TODO se mide sobre el polígono ORIGINAL antes de tocar un solo
            //    vértice (la primera versión mutaba en sitio y el test de
            //    verticalidad leía vértices ya inflados: polígono corrupto).
            for (int i = 0; i < n; i++)
            {
                Vector2 a = p[i], b = p[(i + 1) % n];
                _aristaVertical[i] = Mathf.Abs(b.x - a.x) < 1e-4f; // arista i = p[i]→p[i+1].
                Vector2 d = b - a;
                float len = d.magnitude;
                if (len < 1e-5f) { _poliTmp.Add(a); continue; }
                d /= len;
                _poliTmp.Add(a + new Vector2(-d.y, d.x) * aire); // exterior = izquierda del avance (horario, Y arriba).
            }
            // 2) vértice i = cruce de la arista prev (desplazada) con la arista i
            //    (desplazada): la vertical fija X, la horizontal fija Y.
            for (int i = 0; i < n; i++)
            {
                int prev = (i - 1 + n) % n;
                p[i] = _aristaVertical[prev]
                    ? new Vector2(_poliTmp[prev].x, _poliTmp[i].y)
                    : new Vector2(_poliTmp[i].x, _poliTmp[prev].y);
            }
        }

        private void DibujarContornos()
        {
            var cam = Camera.main;
            if (cam == null) return;
            float grosor = Mathf.Max(1f, Mathf.Round(UiStyles.Escala)); // la expresión EXACTA de la retícula: línea de delineante.
            float guion = UiStyles.S(5f), huecoG = UiStyles.S(4f), aire = UiStyles.S(2f), margen = UiStyles.S(24f);
            float periodo = guion + huecoG;
            // (R99) LAS HORMIGAS: la corrida entera desfila UN periodo por
            // segundo — el paso del delineante paciente, no una alarma. La
            // fase es global: todos los aparatos marchan al mismo compás.
            float fase = Mathf.Repeat(Time.time * periodo, periodo);
            var azul = new Color(IconoMudanza.r / 255f, IconoMudanza.g / 255f, IconoMudanza.b / 255f);
            float restoFactor = Mathf.Lerp(1f, 0.40f, _focoT);
            float pxPorMundo = Screen.height / (2f * cam.orthographicSize);
            float aireMundo = aire / Mathf.Max(1e-3f, pxPorMundo);

            int vivo = 0; // (R99) el stagger cuenta VISIBLES, no huecos de la lista.
            for (int i = 0; i < _movibles.Count; i++)
            {
                var m = _movibles[i];
                var comoObjeto = m as UnityEngine.Object;
                if (comoObjeto == null) continue; // la limpieza real vive en BuscarCandidato.

                Vector3 centro = m.CentroMundo;

                // 1) LA SILUETA en mundo: la propia del aparato (con tubo y
                //    todo) o el rect visual genérico, siempre horaria.
                _poliMundo.Clear();
                var conSilueta = m as IMovibleSilueta;
                if (conSilueta == null || !conSilueta.PerimetroVisual(_poliMundo) || _poliMundo.Count < 4)
                {
                    _poliMundo.Clear();
                    Vector2 tam = m.TamanoMundo;
                    float hx = tam.x * 0.5f, hy = tam.y * 0.5f;
                    _poliMundo.Add(new Vector2(centro.x - hx, centro.y - hy));
                    _poliMundo.Add(new Vector2(centro.x - hx, centro.y + hy));
                    _poliMundo.Add(new Vector2(centro.x + hx, centro.y + hy));
                    _poliMundo.Add(new Vector2(centro.x + hx, centro.y - hy));
                }
                InflarPoligono(_poliMundo, aireMundo);

                // 2) A pantalla (GUI: Y hacia abajo) + culling por caja.
                if (!ProyectarPoliAPantalla(cam, margen)) continue;

                // 3) EL ALFA (escalones R98) y EL COLOR: el candidato bajo el
                //    cursor marcha en LATÓN — la misma promesa que el puntero.
                bool esCandidatoVivo = _llevando == null && ReferenceEquals(m, _candidato) && _candidatoEnAlcance;
                float alfa = esCandidatoVivo ? 0.85f : (DentroDeAlcance(centro) ? 0.55f : 0.28f);
                if (ReferenceEquals(m, _llevando)) alfa *= 1f - _focoT;   // el agarrado se disuelve en su silueta.
                else alfa *= restoFactor;                                  // el censo retrocede mientras llevas algo.
                // El stagger de ENTRADA (un plano revelándose); la salida muere junta sobre EstadoT.
                // (R99, EL BUG DEL CENSO MUDO: el techo del retraso era 0.28 s con un presupuesto
                // total de EstadoT*0.22 ≤ 0.22 — todo índice ≥ 7 quedaba en alfa 0 PARA SIEMPRE;
                // con la estantería registrada (16 piezas antes que los tanques) el censo entero
                // desaparecía. Techo 0.13: el último entra justo cuando EstadoT clava el 1.)
                float ti = ModoActivo
                    ? Mathf.Clamp01((EstadoT * 0.22f - Mathf.Min(0.035f * vivo, 0.13f)) / 0.09f)
                    : EstadoT;
                vivo++;
                alfa *= ti;
                if (alfa <= 0.01f) continue;
                Color baseCol = esCandidatoVivo ? PunteroLaton : azul;
                var c = new Color(baseCol.r, baseCol.g, baseCol.b, alfa);

                // 4) LA MARCHA: una sola corrida de guiones recorre el
                //    perímetro entero con fase compartida — los guiones
                //    CRUZAN las esquinas partiéndose (mitad en cada arista),
                //    que es justo lo que vende el desfile.
                MarcharPoliEnPantalla(c, fase, guion, huecoG, grosor);
            }
        }

        /// <summary>(R100, extraído) _poliMundo → _poliPantalla (GUI: Y hacia abajo). False = detrás de cámara o fuera del encuadre + margen.</summary>
        private static bool ProyectarPoliAPantalla(Camera cam, float margen)
        {
            _poliPantalla.Clear();
            float bx0 = float.MaxValue, bx1 = float.MinValue, by0 = float.MaxValue, by1 = float.MinValue;
            for (int k = 0; k < _poliMundo.Count; k++)
            {
                Vector3 sp = cam.WorldToScreenPoint(new Vector3(_poliMundo[k].x, _poliMundo[k].y, 0f));
                if (sp.z < 0f) return false;
                var q = new Vector2(sp.x, Screen.height - sp.y);
                _poliPantalla.Add(q);
                bx0 = Mathf.Min(bx0, q.x); bx1 = Mathf.Max(bx1, q.x);
                by0 = Mathf.Min(by0, q.y); by1 = Mathf.Max(by1, q.y);
            }
            return bx1 >= -margen && bx0 <= Screen.width + margen && by1 >= -margen && by0 <= Screen.height + margen;
        }

        /// <summary>(R100, extraído) La corrida de guiones sobre _poliPantalla — fase 0 = trazo quieto (destino), fase viva = hormigas (mudanza).</summary>
        private static void MarcharPoliEnPantalla(Color c, float fase, float guion, float huecoG, float grosor)
        {
            float periodo = guion + huecoG;
            float s = 0f;
            int nPts = _poliPantalla.Count;
            for (int k = 0; k < nPts; k++)
            {
                Vector2 a = _poliPantalla[k], b = _poliPantalla[(k + 1) % nPts];
                float len = (b - a).magnitude;
                if (len < 0.5f) continue;
                Vector2 d = (b - a) / len;
                // guiones del patrón global [fase + j*periodo, +guion) que tocan [s, s+len).
                float j0 = Mathf.Floor((s - fase - guion) / periodo);
                for (float j = j0; ; j++)
                {
                    float g0 = fase + j * periodo;
                    if (g0 >= s + len) break;
                    float s0 = Mathf.Max(s, g0), s1 = Mathf.Min(s + len, g0 + guion);
                    if (s1 <= s0) continue;
                    Vector2 q = a + d * (s0 - s);
                    float w = s1 - s0;
                    if (Mathf.Abs(d.x) > 0.5f)
                        UiStyles.Rellenar(new Rect(Mathf.Min(q.x, q.x + d.x * w), q.y - grosor * 0.5f, w, grosor), c);
                    else
                        UiStyles.Rellenar(new Rect(q.x - grosor * 0.5f, Mathf.Min(q.y, q.y + d.y * w), grosor, w), c);
                }
                s += len;
            }
        }

        /// <summary>
        /// (R100, Cesar: "los cuadros blancos... están bien básicos y se
        /// sobreponen") Un marco punteado ESTÁTICO de mundo, para terceros —
        /// los slots del Acomodo del director lo usan en vez de sus cajas
        /// llenas. El mismo trazo del delineante del censo, pero SIN marcha:
        /// el desfile es de la mudanza viva; el destino espera quieto (dos
        /// lenguajes, cero confusión). Llamar desde OnGUI.
        /// </summary>
        public static void MarcarRectMundo(float wx0, float wy0, float wx1, float wy1, Color color, float alfa)
        {
            _poliMundo.Clear();
            _poliMundo.Add(new Vector2(wx0, wy0));
            _poliMundo.Add(new Vector2(wx0, wy1));
            _poliMundo.Add(new Vector2(wx1, wy1));
            _poliMundo.Add(new Vector2(wx1, wy0));
            MarcarPoliListo(color, alfa);
        }

        /// <summary>
        /// (R101, Cesar: "las siluetas no tienen la misma dimensión ni forma
        /// de los contornos") El marco punteado quieto acepta el POLÍGONO
        /// entero — el director marca cada slot con la L exacta del
        /// contenedor en su pose final (tubo espejado y todo), el mismo
        /// dialecto rectilíneo horario de IMovibleSilueta. Llamar desde OnGUI.
        /// </summary>
        public static void MarcarPoligonoMundo(List<Vector2> puntosMundo, Color color, float alfa)
        {
            _poliMundo.Clear();
            for (int i = 0; i < puntosMundo.Count; i++) _poliMundo.Add(puntosMundo[i]);
            MarcarPoliListo(color, alfa);
        }

        private static void MarcarPoliListo(Color color, float alfa)
        {
            if (alfa <= 0.01f || _poliMundo.Count < 4) return;
            var cam = Camera.main;
            if (cam == null) return;
            float pxPorMundo = Screen.height / (2f * cam.orthographicSize);
            InflarPoligono(_poliMundo, UiStyles.S(2f) / Mathf.Max(1e-3f, pxPorMundo));
            if (!ProyectarPoliAPantalla(cam, UiStyles.S(24f))) return;
            float grosor = Mathf.Max(1f, Mathf.Round(UiStyles.Escala));
            MarcharPoliEnPantalla(new Color(color.r, color.g, color.b, alfa), 0f, UiStyles.S(5f), UiStyles.S(4f), grosor);
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked) return;
            if (EstadoT <= 0f) return; // (R98) MoveTowards llega a 0 exacto: sin epsilon.

            UiStyles.Preparar();

            // (R98) El censo de lo movible — debajo de todo lo demás del modo.
            DibujarContornos();

            // (R93) LA PLACA DE ESTADO sobre la cabeza, latiendo — el único
            // latido permanente del modo (los contornos son arquitectura:
            // quietos). Su alfa viaja con EstadoT.
            if (enabled && _apprentice != null)
            {
                float pulso = (0.68f + 0.20f * Mathf.Sin(Time.time * 3.1f)) * EstadoT;
                var cabeza = _apprentice.transform.position + new Vector3(0f, 0.72f, 0f);
                UiStyles.PlacaMundo(cabeza,
                    _llevando == null ? "MUDANZA — clic agarra · V sale" : "MUDANZA — clic suelta · R cancela",
                    new Color(IconoMudanza.r / 255f, IconoMudanza.g / 255f, IconoMudanza.b / 255f, pulso),
                    UiStyles.S(10f));
            }

            if (_llevando != null && _hasCursorWorld && _sinCubeta)
                UiStyles.PlacaMundo(_cursorWorld, "sin cubeta aquí — tendrás que construirla",
                    UiStyles.Aviso, UiStyles.S(30f));

            // (R98) EL PUNTERO — lo último: encima de todo lo del modo. La
            // retícula del frasco se apaga debajo con (1 - EstadoT) en
            // FlaskHud: fundido cruzado, nunca dos punteros. Sobre una
            // redoma vuelve la retícula normal (ahí el clic es otro verbo).
            if (!StorageRack.RatonSobreRedoma()) DibujarPuntero();
        }
    }
}
