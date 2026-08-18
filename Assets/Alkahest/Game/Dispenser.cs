using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Grifo del banco de trabajo: al activarlo (E cerca, alterna ON/OFF), emite
    /// un caudal constante de un material base por su boquilla, que cae en la
    /// PILA DE RECOGIDA del banco (ver Sim/SimLevelBuilder: los cinco grifos
    /// están en columna vertical sobre el mismo pilar y vierten todos al mismo
    /// sitio — antes colgaban del muro a cuatro alturas distintas y regaban el
    /// suelo hasta el borde inferior de la pantalla, "los caños llevan el juego
    /// al borde inferior").
    ///
    /// M4: algunos materiales tienen un coste de Favor POR ACTIVACIÓN
    /// (<see cref="favorCostPerActivation"/>, fijado desde AlkahestGameBootstrap:
    /// Agua/Arena 0, Aceite 2, Nutriente 5, Azoth 4). Se cobra una única vez al
    /// pasar de OFF a ON (no por tick).
    ///
    /// PROGRESIÓN (playtest 4: "¿puedo conseguir todo con 4 caños?"). No: sin
    /// Azoth no hay cristal, y sin cristal los encargos de las jornadas 2 y 3
    /// eran imposibles fuera de la paleta de dev. El grifo de Azoth existe desde
    /// el principio pero nace SELLADO (<see cref="Bloqueado"/>): lo abre el
    /// Maestro al empezar la jornada 2, junto con las otras muestras (ver
    /// Game/MasterSupplies.cs).
    ///
    /// -----------------------------------------------------------------------
    /// AVISO DE PÉRDIDA DE TRABAJO (restaurado playtest 7 / fix playtest 14)
    /// -----------------------------------------------------------------------
    /// En el commit e3fed6f (playtest 10) este archivo se SOBRESCRIBIÓ con una
    /// copia obsoleta anterior al playtest 7 durante un despliegue, y se perdió
    /// TODO ese trabajo: la chapa lateral permanente por grifo, el halo de
    /// resalte del aparato enfocado y el límite de dos usos del prompt "E — ...".
    /// La MISMA regresión también se llevó por delante <see cref="UiStyles.PlacaMundoLateral"/>
    /// y <see cref="UiStyles.Cercania"/> en UiStyles.cs -- ya RESTAURADOS ahí
    /// (fix playtest 14), así que este archivo los usa directamente en vez de
    /// una copia local. Se ha reconstruido aquí a partir de
    /// Sim/../restore/p9 (commit 2ef67e5, último bueno) fusionado A MANO con
    /// lo que sí se hizo después (ResolverNombre/dos clases de material,
    /// guardas de atajos). SEÑAL DE ALARMA si esto vuelve a perderse:
    /// <see cref="MachineFocus.MostrarPromptE"/> y
    /// <see cref="MachineFocus.RegistrarUsoE"/> quedan declarados en
    /// Game/MachineFocus.cs pero SIN NINGÚN llamante en este archivo (ni en
    /// ChillStone.cs/HeatPlate.cs) — si un grep de esos dos símbolos en
    /// Game/{Dispenser,ChillStone,HeatPlate}.cs vuelve a dar cero resultados,
    /// es la MISMA regresión otra vez.
    ///
    /// ---------------------------------------------------------------------
    /// TALLER MOVIBLE (playtest 19, Game/Mudanza.cs, tecla V)
    /// ---------------------------------------------------------------------
    /// Implementa <see cref="IMovible"/>: Mudanza puede agarrar este grifo y
    /// recolocarlo en cualquier celda dentro del alcance del jugador. El
    /// movimiento de verdad lo hace <see cref="Reposicionar"/>, que NO llama
    /// ni a <see cref="Init"/> ni a <see cref="BuildVisual"/> otra vez.
    /// PORQUÉ NO BuildVisual: MaquinariaSprites.CrearCapa siempre crea un
    /// GameObject nuevo -- una segunda llamada DUPLICARÍA el caño/el halo/
    /// la gota en vez de reemplazarlos. PORQUÉ NO Init: volver a llamarlo
    /// RESELLARÍA el grifo (reiniciaría <see cref="Bloqueado"/> y
    /// <c>favorCostPerActivation</c> a sus valores por defecto -- el sello
    /// de Azoth volvería a cerrarse de golpe) y NO resetea <c>_on</c>, así
    /// que un grifo abierto seguiría emitiendo, pero en la boquilla VIEJA,
    /// mientras el resto del estado se corrompe. <see cref="Reposicionar"/>
    /// solo toca lo posicional (transform, _spoutX/_spoutY, _anclaRotulo);
    /// Bloqueado/favorCostPerActivation/_on quedan completamente intactos.
    /// A diferencia de HeatPlate/ChillStone, el grifo no tiene un
    /// "spanCeldas" variable (CanoGrifo() es un único sprite cacheado, sin
    /// parámetro de ancho) -- así que Reposicionar nunca necesita
    /// reasignar ningún sprite, solo mover transform.position (Cano/Halo/
    /// Gota son hijos con localPosition relativo, así que se arrastran
    /// solos con él).
    /// </summary>
    public sealed class Dispenser : MonoBehaviour, IMaquinaInteractiva, IMovible
    {
        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;

        /// <summary>
        /// Radio de interacción con E. Mismo valor que ChillStone/HeatPlate
        /// (ESCALA COMPARTIDA DE CERCANÍA DEL TALLER, duplicada a propósito en
        /// los tres archivos: un único criterio de "cerca" para todo el taller).
        /// </summary>
        private const float ProximityRange = 3.2f;

        // ---------------------------------------------------------------
        // CHAPA PERMANENTE POR CARRIL (restaurado playtest 7): "'cerrar' del
        // agua está escrito sobre el grifo de arena". Los cinco grifos están en
        // columna a solo 1 unidad de mundo unos de otros, así que CUALQUIER
        // rótulo con desplazamiento VERTICAL desde el ancla de un grifo cae
        // sobre el cuerpo del vecino de arriba o abajo. La solución no es
        // desplazar más (a esa distancia ya no hay margen) sino no desplazar en
        // vertical NUNCA: la chapa vive a la DERECHA del propio grifo, a SU
        // misma altura. Con eso cada grifo tiene su propio carril horizontal y
        // dos chapas de grifos vecinos jamás se cruzan verticalmente por
        // diseño, sin depender de que quede hueco arriba o abajo.
        //
        // El pilar de piedra al que se atornillan los grifos ocupa las columnas
        // x=1..8 del mundo (Sim/SimLevelBuilder.TapPillarX0/X1) y está pegado
        // al borde izquierdo de la pantalla — no cabe una chapa ahí; a la
        // derecha hay pared vacía de sobra. Por eso la chapa va a la derecha.
        //
        // NOTA TÉCNICA (fix playtest 14): esta chapa usa directamente
        // UiStyles.PlacaMundoLateral (ancla a un LADO + parámetro de alfa que
        // desvanece TAMBIÉN el panel de fondo) y UiStyles.Cercania. Ambos
        // miembros de UiStyles.cs se perdieron en la MISMA regresión de este
        // archivo; una ronda anterior los reimplementó localmente aquí porque
        // UiStyles.cs era de solo lectura en ese momento, pero ya han sido
        // RESTAURADOS en UiStyles.cs (con sus firmas y documentación
        // originales del playtest 7) -- este archivo vuelve a llamar a los
        // originales compartidos, sin copia local, para no divergir con
        // ChillStone.cs/HeatPlate.cs.
        // ---------------------------------------------------------------
        private const float ChapaSeparacionPx = 16f;
        private const float PromptDesplazarYPx = -15f;
        /// <summary>Cercanía a la que la chapa en reposo pasa de discreta (0.45) a plena (1.0). Ver OnGUI.</summary>
        private const float ChapaCercaniaPleno = 2.6f;
        private const float ChapaCercaniaLejos = 5.5f;

        private const int EmitRatePerTick = 12;
        private const int SpoutRadius = 1;
        /// <summary>
        /// Voladizo del caño sobre el pilar (que es piedra maciza hasta la celda
        /// 8). 5 celdas: la boquilla sobresale CLARAMENTE del muro y la gota de
        /// color cuelga en aire libre, dentro de la pila de recogida
        /// (interior x 9..56). Antes eran 3 y el caño quedaba lamiendo la piedra.
        /// </summary>
        // (playtest 26, fix integración) De const a INSTANCIA: los dos caños del
        // laboratorio comparten pared (CanoMontajeX) y con un alcance único los
        // dos chorros caían por la MISMA columna de celdas -- el limo desembocaba
        // en la pila del agua, justo la ilegibilidad que esta ronda combate. El
        // caño de limo pide ahora un voladizo más largo por Init y su chorro cae
        // sobre SU pila. El default 5 conserva a todos los demás grifos idénticos.
        private int _spoutOffsetCells = 5;
        private const int SpoutOffsetCellsDefault = 5;

        // (playtest 26, LA RACIÓN) Con la línea del taller, el chorro cae a
        // SUELO ABIERTO y no a una cuba honda: 20 segundos de grifo abierto
        // inundaban el laboratorio entero (visto en la verificación con
        // capturas de esta misma ronda). Un grifo del laboratorio sirve ahora
        // una RACIÓN por apertura (~una pila colmada) y se cierra solo, con su
        // rótulo diciéndolo -- abrirlo otra vez sirve otra ración. racion=0
        // (default) = comportamiento clásico infinito: los grifos del taller
        // clásico y los versátiles no cambian en nada.
        private int _racionCeldas;
        private int _emitidasEstaApertura;
        /// <summary>Filas que baja el caudal respecto a la celda de anclaje: la boquilla dibujada cuelga por debajo del eje del caño, y el chorro tiene que nacer DE ELLA.</summary>
        private const int SpoutDropCells = 2;
        private const float InsufficientFavorFlashSeconds = 1.5f;
        /// <summary>Celdas que se miran hacia arriba buscando la superficie del charco cuando el caño queda sumergido.</summary>
        private const int OverflowSearchUp = 8;

        // Textos fijos del rótulo (playtest 6: "no hace falta que la
        // nomenclatura incluya la palabra grifo" — el jugador ya lo ve, es un
        // caño; el nombre del material basta). Los Debug.Log internos siguen
        // diciendo "grifo" a propósito: esos no son cara al jugador.
        // (restaurado playtest 7): "SELLADO" a secas — es el texto de la chapa
        // PERMANENTE ahora, no un aviso puntual.
        private const string ChapaSellada = "SELLADO";
        private const string AvisoSinFavor = "¡sin Favor suficiente!";
        private const string AvisoBloqueo = "el Maestro aún no os confía esto";

        [Tooltip("Coste en Favor de encender este grifo (una sola vez por activación). 0 = gratis.")]
        [SerializeField] private int favorCostPerActivation = 0;

        private AlkahestSim _sim;
        private Transform _player;
        private OrderSystem _orderSystem;
        /// <summary>
        /// (fix reclasificación de sustancias) Necesario para resolver el NOMBRE de verdad
        /// del material que dispensa este grifo -- ver ResolverNombre. No llega por Init()
        /// (cambiar esa firma exigiría tocar Game/AlkahestGameBootstrap.cs, fuera de las
        /// ARCHIVOS MODIFICABLES de este encargo), así que se resuelve con
        /// FindAnyObjectByType UNA sola vez en Init (nunca en Update/OnGUI): mismo patrón ya
        /// endorsado por el proyecto para localizar dependencias de escena (ver
        /// AlkahestGameBootstrap.Start, que hace lo mismo con AlkahestSim). Para cuando
        /// SpawnDispensers() corre, SubstanceKnowledge ya vive en el aprendiz (creado antes
        /// en TrySpawn), así que esto nunca falla en el flujo normal del juego.
        /// </summary>
        private SubstanceKnowledge _knowledge;
        private int _spoutX, _spoutY;
        private byte _matId;
        private bool _on;
        private float _accumulator;

        private float _insufficientFavorTimer;
        private float _bloqueoAvisoTimer;
        private bool _rebosando;

        private SpriteRenderer _cano;
        private SpriteRenderer _gota;
        private Transform _gotaTr;
        private Color _matColor = Color.white;
        private Vector3 _anclaRotulo;

        /// <summary>
        /// (restaurado playtest 7) Capa de RESALTE: una copia agrandada y teñida
        /// de oro del mismo sprite del caño, dibujada DETRÁS de él (sortingOrder
        /// menor), que asoma por los bordes como un halo. Sustituye al prompt
        /// "E — ..." permanente como señal de "estás lo bastante cerca para
        /// actuar aquí": se ve desde el otro lado del taller sin ocupar texto
        /// en pantalla ni parpadear (ver <see cref="_haloAlfaActual"/>).
        /// </summary>
        private SpriteRenderer _halo;
        private float _haloAlfaActual;

        // Chapas del rótulo, cacheadas: el coste de Favor es fijo tras Init y el
        // NOMBRE puede cambiar en cualquier momento (el jugador bautiza a mitad
        // de partida, regla 13) -- se reconstruyen en BuildVisual y cada vez que
        // SubstanceKnowledge.NamingVersion sube (ver RebuildChapas/Update), NUNCA
        // dentro de OnGUI: cero asignaciones de string por frame.
        private string _chapaCerrado;   // "AGUA" o "ACEITE  3★" - chapa permanente en reposo.
        private string _chapaAbierto;   // "AGUA · abierto" - chapa permanente, grifo ON.
        private string _chapaServido;   // (playtest 26, LA RACIÓN) "AGUA · servido - E para más": unos segundos tras el autocierre por ración.
        private float _servidoTimer;    // segundos restantes mostrando _chapaServido.
        private string _chapaRebosando; // "AGUA · rebosa" - chapa permanente, rebosando.
        private string _promptAbrir;    // "E — abrir" o "E — abrir (N Favor)".
        private int _lastNamingVersion = -1;

        /// <summary>Sellado por el Maestro: no se puede abrir todavía. Ver doc de la clase.</summary>
        public bool Bloqueado { get; private set; }

        /// <summary>Material que dispensa (lo consulta MasterSupplies para localizar el grifo de Azoth).</summary>
        public byte Material => _matId;

        // Foco de interacción: CRÍTICO aquí — los cinco grifos están en columna
        // a una unidad de mundo unos de otros, y sin árbitro una sola E abriría
        // varios a la vez (ver Game/MachineFocus.cs).
        public Vector3 PuntoFoco => _anclaRotulo;
        public float RangoFoco => ProximityRange;

        // ---------------------------------------------------------------
        // IMovible (playtest 19, ver doc de clase "TALLER MOVIBLE" y
        // Game/Mudanza.cs para el contrato completo).
        // ---------------------------------------------------------------

        /// <summary>
        /// Centro visual del caño (la brida se ancla en transform.position,
        /// pero el caño en sí cuelga con un offset local de 2.5 celdas MÁS
        /// la mitad del voladizo extra -- ver BuildVisual, campo `extra`).
        /// Se usa como hitbox de agarre, no como PuntoFoco (ese sigue siendo
        /// _anclaRotulo, sin cambios).
        /// </summary>
        public Vector3 CentroMundo => transform.position + new Vector3(
            2.5f * SimRenderer.CellWorldSize + VoladizoExtraMundo * 0.5f, 0f, 0f);

        /// <summary>
        /// (fix Cesar playtest 34, tarea "c") BOUNDS REALES DEL CAÑO CON SU
        /// VOLADIZO -- antes esto era un tamaño FIJO (8x5 celdas, el sprite
        /// base), que ignoraba <c>extra</c> (el estiramiento por
        /// <see cref="_spoutOffsetCells"/> que sí aplica BuildVisual al
        /// propio sprite, ver ese método). Un caño con voladizo mayor al
        /// default dibujaría una guía de mudanza más corta que su boquilla
        /// real -- inofensivo hoy (los dos caños del laboratorio volvieron al
        /// voladizo default 5 en el playtest 27), pero un bug real y
        /// silencioso para el primer caño que vuelva a pedir uno distinto
        /// (ver el docblock de <see cref="_spoutOffsetCells"/>). El grifo
        /// NUNCA incluye la pila en su footprint (ver el docblock de
        /// Game/Pila.cs, tarea "a": "el grifo se muda SOLO").
        /// </summary>
        public Vector2 TamanoMundo => new Vector2(8f * SimRenderer.CellWorldSize + VoladizoExtraMundo, 5f * SimRenderer.CellWorldSize);

        /// <summary>Unidades de mundo que el voladizo de ESTE caño estira el sprite respecto al default -- 0 para cualquier grifo con <see cref="_spoutOffsetCells"/>==<see cref="SpoutOffsetCellsDefault"/> (el caso normal). Mismo cálculo que `extra` en BuildVisual, factorizado aquí porque CentroMundo/TamanoMundo también lo necesitan.</summary>
        private float VoladizoExtraMundo => Mathf.Max(0f, (_spoutOffsetCells - SpoutOffsetCellsDefault) * SimRenderer.CellWorldSize);

        /// <summary>
        /// Celda de anclaje: mountCellX/mountCellY, la MISMA celda que recibía
        /// Init -- se DERIVA de _spoutX/_spoutY (que sí se guardan) restando
        /// los offsets fijos, en vez de guardar una copia nueva de
        /// mountCellX/mountCellY (que Init nunca almacenaba, ver el survey de
        /// este encargo).
        /// </summary>
        public Vector2Int AnclaCelda => new Vector2Int(_spoutX - _spoutOffsetCells, _spoutY + SpoutDropCells);

        /// <summary>
        /// ¿Cabría el grifo en esa celda de montaje sin que la boquilla, el
        /// radio de emisión (<see cref="SpoutRadius"/>) o la búsqueda de
        /// rebose (<see cref="OverflowSearchUp"/>) salgan del marco protegido
        /// del mundo? Réplica de los mismos offsets que usa
        /// <see cref="Reposicionar"/>/<see cref="EmitTick"/>, con un margen
        /// extra de una celda por seguridad. Puramente informativo -- Mudanza
        /// decide si bloquea el drop con esto.
        /// </summary>
        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            int spoutX = anclaCelda.x + _spoutOffsetCells;
            int spoutY = anclaCelda.y - SpoutDropCells;
            int izq = anclaCelda.x - 1;                                  // brida en voladizo hacia el mount.
            int der = spoutX + SpoutRadius + 1;                          // boquilla + radio de emisión.
            int abajo = spoutY - SpoutRadius - 1;                        // caudal cayendo bajo la boquilla.
            int arriba = spoutY + OverflowSearchUp + SpoutRadius + 1;    // búsqueda de rebose hacia arriba.
            return izq >= 1 && der <= CellGrid.W - 2 && abajo >= 1 && arriba <= CellGrid.H - 2
                && anclaCelda.y >= 1 && anclaCelda.y <= CellGrid.H - 2;
        }

        /// <summary>
        /// Inyección de dependencias desde AlkahestGameBootstrap.
        ///
        /// (fix Cesar playtest 34, "GRIFOS Y PILAS SE MUDAN POR SEPARADO")
        /// LOS CUATRO PARÁMETROS `pilaX0/pilaAncho/pilaAlto/pilaBaseY` DEL
        /// PLAYTEST 27 SE RETIRARON DE ESTA FIRMA. El grifo dibujaba el marco
        /// decorativo de su pila (<see cref="BuildPilaEnmarcada"/>) como HIJO
        /// de su propio transform, así que mover el grifo con Mudanza
        /// arrastraba el marco lejos de la cubeta de piedra real (que nunca
        /// se movía, tallada aparte por Sim/SimLevelBuilder) -- Cesar:
        /// "verifica qué pasa con su pila". Ese marco ahora es responsabilidad
        /// exclusiva de <see cref="Alkahest.Game.Pila"/> (archivo nuevo, ver su
        /// docblock), un IMovible independiente que se agarra y suelta con su
        /// propio gesto. <see cref="BuildPilaEnmarcada"/> se queda declarado
        /// SIN llamante (regla 15 de CLAUDE.md: documentar lo que se retira,
        /// no solo dejar de invocarlo) por si algún día un grifo necesita de
        /// nuevo un marco propio que NO sea una pila independiente.
        /// </summary>
        public void Init(AlkahestSim sim, Transform player, int mountCellX, int mountCellY, byte materialId,
            OrderSystem orderSystem = null, int favorCost = 0, bool bloqueado = false, int spoutOffsetCells = SpoutOffsetCellsDefault,
            int racionCeldas = 0)
        {
            _racionCeldas = racionCeldas;
            _emitidasEstaApertura = 0;
            _sim = sim;
            _player = player;
            _spoutOffsetCells = spoutOffsetCells;
            _spoutX = mountCellX + _spoutOffsetCells;
            _spoutY = mountCellY - SpoutDropCells;
            _matId = materialId;
            _orderSystem = orderSystem;
            favorCostPerActivation = favorCost;
            Bloqueado = bloqueado;
            _knowledge = FindAnyObjectByType<SubstanceKnowledge>(); // ver doc del campo.

            BuildVisual(mountCellX, mountCellY);
            MachineFocus.Registrar(this);
            Mudanza.RegistrarMovible(this); // (playtest 19) ver doc de clase "TALLER MOVIBLE".
        }

        private void OnDestroy()
        {
            MachineFocus.Olvidar(this);
            Mudanza.OlvidarMovible(this);
        }

        /// <summary>
        /// TALLER MOVIBLE (playtest 19, Game/Mudanza.cs): mueve el grifo YA
        /// CONSTRUIDO a una nueva celda de montaje, SIN volver a llamar a
        /// Init ni a BuildVisual -- ver el docblock de la clase para el
        /// porqué exacto de cada uno (en corto: BuildVisual DUPLICARÍA el
        /// caño/halo/gota; Init RESELLARÍA el grifo y perdería el estado
        /// _on). Solo toca lo posicional: _spoutX/_spoutY (de donde
        /// EmitTick lee para emitir, y de donde AnclaCelda los deriva de
        /// vuelta), transform.position (Cano/Halo/Gota son hijos con
        /// localPosition RELATIVO, así que se arrastran solos con él) y
        /// _anclaRotulo (el punto del que cuelgan PuntoFoco y las chapas de
        /// OnGUI). Bloqueado, favorCostPerActivation y _on -- justo los tres
        /// campos que Init resetearía -- no se tocan.
        /// </summary>
        public void Reposicionar(Vector2Int anclaCelda)
        {
            _spoutX = anclaCelda.x + _spoutOffsetCells;
            _spoutY = anclaCelda.y - SpoutDropCells;
            transform.position = _sim.CellToWorld(anclaCelda);

            float celda = SimRenderer.CellWorldSize;
            _anclaRotulo = transform.position + new Vector3(6.5f * celda, 0f, 0f);
        }

        /// <summary>
        /// Nombre de verdad de lo que da este grifo: bautizado &gt; común de taller &gt; "???"
        /// (fix reclasificación de sustancias). ANTES caía en `_sim.Universe.Get(_matId).devName`
        /// cuando NombreComun devolvía null, así que el grifo de Azoth mostraba literalmente
        /// "Azoth" -- el nombre INTERNO en inglés del devName -- para siempre, sin importar si
        /// el jugador ya lo había bautizado. Azoth es justo uno de los materiales reclasificados
        /// como "innominado" (ver Game/SubstanceKnowledge.cs): su chapa tiene que decir "???"
        /// hasta que se bautice, exactamente como el resto del HUD.
        /// </summary>
        private string ResolverNombre() => _knowledge != null
            ? _knowledge.NombreParaHud(_matId)
            : (SubstanceKnowledge.NombreComun(_matId) ?? "???"); // defensivo: nunca debería faltar en el flujo normal.

        /// <summary>Rompe el sello del Maestro: el grifo pasa a ser usable (jornada 2).</summary>
        public void Desbloquear()
        {
            if (!Bloqueado) return;
            Bloqueado = false;
            UpdateVisual();
            Debug.Log($"[ChaosAlchemy] El Maestro abre el grifo de {ResolverNombre()}.");
        }

        /// <summary>
        /// (playtest 27, CONTRATO_TALLER_GRANDE mandato 2) EL CAÑO ENMARCA SU
        /// PROPIA PILA. Sim/SimLevelBuilder talla las dos pilas de la estación
        /// de fuentes como cubetas de piedra de 14x9, pero una cubeta de
        /// piedra vacía sobre un suelo de piedra ES UN AGUJERO NEGRO: visto
        /// jugando, las dos pilas nuevas no se leían como recipientes, se
        /// leían como huecos del terreno. El resto del taller resuelve esto
        /// con <see cref="MaquinariaSprites.MarcoBandeja"/> (labio de latón
        /// volado + cartelas), y las fuentes tienen que hablar el mismo
        /// idioma o la gramática se rompe justo en la primera máquina que ve
        /// el jugador.
        ///
        /// POR QUÉ AQUÍ Y NO EN UNA CLASE NUEVA (RAZÓN ORIGINAL, playtest 27):
        /// la pila no existe sin su caño (es "dónde cae ESTE chorro"), así que
        /// el dueño natural del marco es el caño.
        ///
        /// (fix Cesar playtest 34) ESA RAZÓN DEJÓ DE SER CIERTA: la pila SÍ
        /// puede existir sin su caño desde que se convirtió en un IMovible
        /// independiente (ver <see cref="Alkahest.Game.Pila"/>) -- justamente
        /// porque, siendo hijo del transform del grifo, este marco se movía
        /// CON el grifo al mudarlo, dejando la cubeta de piedra real (que
        /// nunca se movía) sin su labio decorativo. RETIRADA SIN LLAMANTE
        /// (regla 15 de CLAUDE.md, mismo criterio que
        /// <c>Sim/SimLevelBuilder.PlaceCharco</c>): el método se queda
        /// definido, intacto, por si algún día un grifo vuelve a necesitar un
        /// marco propio que NO sea una pila independiente.
        /// </summary>
        private void BuildPilaEnmarcada(int pilaX0, int pilaBaseY, int pilaAncho, int pilaAlto)
        {
            float celda = SimRenderer.CellWorldSize;
            var go = new GameObject("PilaMarco");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3((pilaX0 + pilaAncho * 0.5f) * celda, (pilaBaseY + pilaAlto * 0.5f) * celda, 0f);
            var sr = MaquinariaSprites.CrearCapa(go.transform, "Sprite",
                MaquinariaSprites.MarcoBandeja(pilaAncho, pilaAlto), 19, pilaAncho * celda, pilaAlto * celda);
            sr.color = Color.white;
        }

        private void BuildVisual(int mountCellX, int mountCellY)
        {
            transform.position = _sim.CellToWorld(new Vector2Int(mountCellX, mountCellY));

            // Caño de latón generado (brida + tubo + boquilla + volante).
            //
            // TAMAÑO (fix del playtest 5, "no pude acceder a los grifos"): medía
            // 3.4 x 2.0 celdas = 17 x 10 px a 720p, seis veces más estrecho que
            // cualquier otra máquina del taller (la placa ígnea mide 260 px) y en
            // latón oscuro sobre piedra oscura, pegado al borde izquierdo. Era
            // literalmente invisible. Ahora mide 8 x 5 celdas (40 x 25 px) y sale
            // EN VOLADIZO: la brida muerde el pilar en la celda 8 y la boquilla
            // llega hasta la 15, con el caudal cayendo desde la 13.
            float celda = SimRenderer.CellWorldSize;
            // (playtest 26) El voladizo visual CRECE con el alcance real: un caño
            // con _spoutOffsetCells=12 estira su tubo (el sprite es procedural,
            // estirarlo alarga el tramo recto) y desplaza su centro, de modo que
            // la boquilla dibujada queda SIEMPRE sobre la columna real del chorro.
            float extra = (_spoutOffsetCells - SpoutOffsetCellsDefault) * celda;
            _cano = MaquinariaSprites.CrearCapa(transform, "Cano", MaquinariaSprites.CanoGrifo(), 19,
                8f * celda + extra, 5f * celda);
            _cano.transform.localPosition = new Vector3(2.5f * celda + extra * 0.5f, 0f, 0f);

            // (restaurado playtest 7) HALO de resalte: la misma silueta del
            // caño, ~1.22x más grande y DETRÁS de todo (sortingOrder 15 < 19 del
            // propio caño), teñida de oro. Al ser una copia más grande detrás
            // de la real, asoma por los bordes como un contorno luminoso. Nace
            // invisible (alfa 0); UpdateHalo() la enciende SOLO cuando este
            // grifo es el foco, con un latido suave. Sustituye al prompt
            // "E — ..." permanente (que "estorba" según el jugador) como señal
            // de "estás lo bastante cerca para actuar aquí".
            _halo = MaquinariaSprites.CrearCapa(transform, "Halo", MaquinariaSprites.CanoGrifo(), 15,
                (8f * celda + extra) * 1.22f, 5f * celda * 1.22f);
            _halo.transform.localPosition = _cano.transform.localPosition;
            _halo.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);

            // Gota de color: es lo que permite saber de un vistazo (y desde lejos)
            // qué da cada grifo y cuál está abierto -- ver UpdateVisual.
            var gotaGO = new GameObject("Gota");
            gotaGO.transform.SetParent(transform, false);
            // La gota cuelga justo bajo la boquilla, en aire libre: es la marca
            // de color que dice QUÉ da este grifo desde el otro lado del taller.
            gotaGO.transform.localPosition = new Vector3(_spoutOffsetCells * celda, -3.2f * celda, 0f);
            _gotaTr = gotaGO.transform;
            _gota = gotaGO.AddComponent<SpriteRenderer>();
            _gota.sprite = MaquinariaSprites.Solido();
            _gota.sortingOrder = 20;
            _gotaTr.localScale = new Vector3(0.26f, 0.26f, 1f);
            _matColor = _sim.Universe.Get(_matId).baseColor;
            _gota.color = _matColor;

            // (restaurado playtest 7) El ancla de la chapa (y del punto de foco)
            // NO va por encima del caño (eso era lo que hacía que el rótulo de
            // un grifo cayera sobre el vecino de la columna, a solo 1 unidad de
            // mundo de distancia vertical). Va junto al borde DERECHO del
            // cuerpo del caño —localPosition.x=2.5*celda más la mitad del ancho
            // del sprite (8*celda/2=4*celda) = 6.5*celda— a la MISMA altura que
            // el propio caño (y=0, el mount). Con esto la chapa se dibuja
            // SIEMPRE en el carril horizontal de su propio grifo y dos chapas
            // de grifos vecinos jamás se cruzan, sin depender de cuánto hueco
            // quede libre arriba o abajo.
            _anclaRotulo = transform.position + new Vector3(6.5f * celda, 0f, 0f);

            RebuildChapas();
            UpdateVisual();
        }

        /// <summary>
        /// Reconstruye las chapas cacheadas de texto. Se llama UNA vez en
        /// BuildVisual y de nuevo cada vez que <see cref="SubstanceKnowledge.NamingVersion"/>
        /// cambia (ver Update) -- así el jugador puede bautizar un material
        /// innominado a mitad de partida y ver la chapa del grifo actualizarse,
        /// sin reconstruir el string en cada OnGUI (regla de cero asignaciones
        /// por frame).
        /// </summary>
        private void RebuildChapas()
        {
            string nombreCorto = ResolverNombre().ToUpperInvariant();
            // (restaurado playtest 7) La chapa en reposo también informa del
            // coste en Favor de encenderlo, para que el jugador no lo descubra
            // a ciegas pulsando E: "ACEITE  3★".
            string sufijoCoste = favorCostPerActivation > 0 ? $"  {favorCostPerActivation}★" : "";
            _chapaCerrado = nombreCorto + sufijoCoste;
            _chapaAbierto = nombreCorto + " · abierto";
            _chapaServido = nombreCorto + " · servido — E para más"; // (playtest 26, LA RACIÓN)
            _chapaRebosando = nombreCorto + " · rebosa";
            _promptAbrir = favorCostPerActivation > 0 ? $"E — abrir ({favorCostPerActivation} Favor)" : "E — abrir";
            if (_knowledge != null) _lastNamingVersion = _knowledge.NamingVersion;
        }

        /// <summary>
        /// Estado del grifo legible a distancia: sellado = latón apagado y gota
        /// gris; cerrado = gota pequeña y mate; abierto = gota brillante que late.
        /// </summary>
        private void UpdateVisual()
        {
            if (_gota == null || _gotaTr == null) return;

            if (Bloqueado)
            {
                if (_cano != null) _cano.color = new Color(0.42f, 0.40f, 0.38f, 1f);
                _gota.color = new Color(0.35f, 0.34f, 0.36f, 0.7f);
                _gotaTr.localScale = new Vector3(0.18f, 0.18f, 1f);
                return;
            }

            // (restaurado playtest 7) Brillo extra en el propio caño cuando es
            // el foco: un pequeño empujón hacia el dorado, además del halo
            // detrás. Ni Shader.Find ni material nuevo, solo el tinte del
            // SpriteRenderer (regla de oro del repo: solo SpriteRenderer).
            if (_cano != null)
            {
                _cano.color = EstaEnfocado() ? Color.Lerp(Color.white, UiStyles.Oro, 0.18f) : Color.white;
            }

            if (_on)
            {
                float pulso = 0.85f + 0.15f * Mathf.Sin(Time.time * 9f);
                _gota.color = new Color(
                    Mathf.Clamp01(_matColor.r * 1.25f), Mathf.Clamp01(_matColor.g * 1.25f),
                    Mathf.Clamp01(_matColor.b * 1.25f), 1f);
                float s = 0.34f * pulso;
                _gotaTr.localScale = new Vector3(s, s, 1f);
            }
            else
            {
                _gota.color = new Color(_matColor.r, _matColor.g, _matColor.b, 0.8f);
                _gotaTr.localScale = new Vector3(0.24f, 0.24f, 1f);
            }
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return; // M4: título/intro/fin de día/pantalla final congelan el grifo.

            if (_servidoTimer > 0f) _servidoTimer -= Time.deltaTime; // (playtest 26, LA RACIÓN) la chapa "servido" caduca sola.

            // Regla 13: el jugador puede bautizar un innominado a mitad de
            // partida y la chapa de este grifo tiene que reflejarlo. Compara un
            // int (barato) cada frame; solo RECONSTRUYE el string cuando de
            // verdad cambió (ver RebuildChapas).
            if (_knowledge != null && _knowledge.NamingVersion != _lastNamingVersion) RebuildChapas();

            // (fix playtest 10) E es un atajo de una sola tecla: no puede robarle letras al
            // campo de bautizar ni competir con el diario a pantalla completa (ver el mismo
            // comentario en Game/ChillStone.cs/HeatPlate.cs). El caudal ya abierto sigue
            // emitiendo igual con el libro abierto -- solo se calla el toggle ON/OFF.
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && EstaEnfocado())
            {
                ToggleRequested();
            }

            if (_insufficientFavorTimer > 0f) _insufficientFavorTimer -= Time.deltaTime;
            if (_bloqueoAvisoTimer > 0f) _bloqueoAvisoTimer -= Time.deltaTime;

            if (_on)
            {
                _accumulator += Time.deltaTime;
                int steps = 0;
                while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
                {
                    EmitTick();
                    _accumulator -= TickDt;
                    steps++;
                }
                if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;
            }

            UpdateVisual();
            UpdateHalo(); // SIEMPRE, esté el grifo abierto o cerrado (ver doc del campo _halo).
        }

        /// <summary>
        /// (restaurado playtest 7) Latido del halo de resalte: sube a un pulso
        /// suave mientras el grifo es el foco, y baja a 0 en cuanto deja de
        /// serlo. SIEMPRE a través de MoveTowards (no un salto directo al valor
        /// objetivo) para que la entrada/salida del foco sea un fundido y no un
        /// parpadeo. Cero asignaciones por frame más allá del Color struct (no
        /// es un alloc de heap).
        /// </summary>
        private void UpdateHalo()
        {
            if (_halo == null) return;

            bool enfocado = !Bloqueado && EstaEnfocado();
            float objetivo = enfocado ? 0.65f + 0.20f * Mathf.Sin(Time.time * 4f) : 0f;
            _haloAlfaActual = Mathf.MoveTowards(_haloAlfaActual, objetivo, 6f * Time.deltaTime);
            _halo.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, _haloAlfaActual);
        }

        private void ToggleRequested()
        {
            if (Bloqueado)
            {
                _bloqueoAvisoTimer = InsufficientFavorFlashSeconds;
                return;
            }

            if (_on)
            {
                _on = false;
                _rebosando = false;
                // (restaurado playtest 7) Cuenta como "uso enseñado" de la E:
                // apagar el grifo es una acción con efecto, igual que encenderlo.
                MachineFocus.RegistrarUsoE();
                Debug.Log($"[ChaosAlchemy] Grifo de {_sim.Universe.Get(_matId).devName} -> OFF");
                return;
            }

            if (TryPayActivationCost())
            {
                _on = true;
                _emitidasEstaApertura = 0; // (playtest 26, LA RACIÓN) ración nueva por apertura -- ver el docblock de _racionCeldas.
                MachineFocus.RegistrarUsoE();
                Debug.Log($"[ChaosAlchemy] Grifo de {_sim.Universe.Get(_matId).devName} -> ON (coste {favorCostPerActivation} Favor).");
            }
            else
            {
                // (restaurado playtest 7) NO se registra uso: un intento
                // fallido por falta de Favor no enseña nada sobre cómo usar la E.
                _insufficientFavorTimer = InsufficientFavorFlashSeconds;
                Debug.Log($"[ChaosAlchemy] Grifo de {_sim.Universe.Get(_matId).devName}: sin Favor suficiente ({favorCostPerActivation} requerido).");
            }
        }

        private bool TryPayActivationCost()
        {
            if (favorCostPerActivation <= 0) return true;
            if (_orderSystem == null) return true; // defensivo: sin OrderSystem conectado no bloqueamos el grifo.
            return _orderSystem.SpendFavor(favorCostPerActivation);
        }

        /// <summary>¿Es ESTE el grifo que el aprendiz tiene delante? (ver Game/MachineFocus.cs)</summary>
        private bool EstaEnfocado() => MachineFocus.EsFoco(this, _player);

        /// <summary>
        /// (playtest 3: "el grifo de agua deja de funcionar cuando se llena")
        /// El caño emitía SOLO en las celdas vacías de su boca, así que en cuanto
        /// el charco subía hasta taparla el grifo parecía averiado. Ahora, si la
        /// boca está sumergida, busca la SUPERFICIE del charco unas pocas celdas
        /// más arriba y deja caer ahí una gota por tick: el nivel sigue subiendo
        /// (más despacio, como un rebose real) y el grifo nunca parece roto.
        /// </summary>
        private void EmitTick()
        {
            int budget = EmitRatePerTick;
            for (int dy = -SpoutRadius; dy <= SpoutRadius && budget > 0; dy++)
            {
                int y = _spoutY + dy;
                for (int dx = -SpoutRadius; dx <= SpoutRadius && budget > 0; dx++)
                {
                    if (dx * dx + dy * dy > SpoutRadius * SpoutRadius) continue;
                    int x = _spoutX + dx;
                    if (!CellGrid.InBounds(x, y)) continue;
                    if (_sim.SampleMaterial(x, y) != MaterialId.Empty) continue;

                    // (fix playtest 17) UN GRIFO NACE ESTABLE. Antes esto era
                    // `Paint`, que NO toca `temp`: la celda heredaba la
                    // temperatura que tuviera antes. Si la boquilla o la pila se
                    // habían enfriado alguna vez (la piedra gélida cerca, un
                    // charco frío previo, hielo que estuvo ahí), el agua RECIÉN
                    // SALIDA del grifo nacía congelada — el bug que Cesar
                    // reportó dos rondas seguidas como "el agua del grifo sale
                    // congelada", y que también explica su "en la hornilla se
                    // volvía agua pero los bordes se hacían hielo": llegaba ya
                    // helada y solo se derretía sobre el 40% que cubre la placa.
                    // Es exactamente la misma clase de fallo que "pintar hielo
                    // produce agua" (regla 22): materia creada de la nada tiene
                    // que nacer a una temperatura donde ese material sea ESTABLE.
                    _sim.PaintStable(x, y, 0, _matId);
                    budget--;
                    _emitidasEstaApertura++;
                }
            }

            // (playtest 26, LA RACIÓN) servida la ración, el grifo se cierra
            // solo. El contador se rearma al ABRIR (ver donde _on pasa a true),
            // no aquí: así "cerrar a mano a mitad de ración" no regala ración
            // extra al reabrir a medias.
            if (_racionCeldas > 0 && _emitidasEstaApertura >= _racionCeldas)
            {
                _on = false;
                _servidoTimer = 5f; // la chapa explica el autocierre -- sin esto parece un grifo roto (regla 43: un estado sin rótulo es indistinguible de un bug).
            }

            if (budget < EmitRatePerTick)
            {
                _rebosando = false;
                return;
            }

            for (int up = 1; up <= OverflowSearchUp; up++)
            {
                int y = _spoutY + up;
                if (!CellGrid.InBounds(_spoutX, y)) break;
                if (_sim.SampleMaterial(_spoutX, y) != MaterialId.Empty) continue;

                _sim.PaintStable(_spoutX, y, 0, _matId); // (fix playtest 17) mismo criterio que arriba: el rebose también nace estable.
                _rebosando = false;
                return;
            }

            _rebosando = true;
        }

        /// <summary>
        /// (restaurado playtest 7) Antes esta clase dibujaba dos "anillos"
        /// (estado/nombre) con <see cref="UiStyles.PlacaMundo"/> desplazado
        /// verticalmente desde encima del caño -- con los cinco grifos en
        /// columna a 1 unidad de mundo unos de otros, ese desplazamiento
        /// vertical caía sistemáticamente sobre el caño vecino ("'cerrar' del
        /// agua está escrito sobre el grifo de arena"). Ahora hay UNA sola
        /// chapa PERMANENTE por grifo, anclada al LADO con
        /// <see cref="UiStyles.PlacaMundoLateral"/> (nunca encima, ver
        /// <see cref="_anclaRotulo"/> en BuildVisual) en el carril horizontal de
        /// su propio caño, así que dos chapas de grifos vecinos son
        /// geométricamente incapaces de solaparse. El prompt "E — ..." ya no es
        /// permanente (MachineFocus.MostrarPromptE lo apaga tras las dos
        /// primeras veces del taller entero) y el resalte del foco lo asume el
        /// halo dorado (ver UpdateHalo).
        /// </summary>
        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return; // (playtest 21) HudSilenciado, hermano de InputLocked.

            UiStyles.Preparar();

            // Cercanía de la chapa en reposo: discreta de lejos (0.45), plena
            // de cerca (1.0). Los avisos puntuales y el grifo abierto van
            // siempre a alfa 1 (información urgente a cualquier distancia).
            float cercania = UiStyles.Cercania(_anclaRotulo, _player, ChapaCercaniaPleno, ChapaCercaniaLejos);

            // Contenido de la chapa PERMANENTE, por prioridad (solo una línea a
            // la vez). Los avisos puntuales (Favor insuficiente / intento sobre
            // un grifo sellado) ocupan el MISMO carril lateral en vez de un
            // rótulo aparte con su propio desplazamiento -- si tuvieran uno
            // propio reintroducirían el mismo bug de solape que motiva este fix.
            string texto;
            Color color;
            float alfa;

            if (_insufficientFavorTimer > 0f)
            {
                texto = AvisoSinFavor;
                color = UiStyles.Peligro;
                alfa = 1f; // el jugador acaba de pulsar E aquí mismo: siempre cerca.
            }
            else if (_bloqueoAvisoTimer > 0f)
            {
                texto = AvisoBloqueo;
                color = UiStyles.Peligro;
                alfa = 1f;
            }
            else if (Bloqueado)
            {
                texto = ChapaSellada;
                color = UiStyles.TextoTenue;
                alfa = 0.45f + 0.55f * cercania;
            }
            else if (_on)
            {
                texto = _rebosando ? _chapaRebosando : _chapaAbierto;
                color = _rebosando ? UiStyles.Aviso : UiStyles.Oro;
                alfa = 1f; // un grifo abierto es información urgente a cualquier distancia.
            }
            else
            {
                texto = _servidoTimer > 0f ? _chapaServido : _chapaCerrado;
                color = UiStyles.TextoTenue;
                alfa = 0.45f + 0.55f * cercania;
            }

            // A la DERECHA del caño (aLaIzquierda: false, ver doc de cabecera
            // sobre el pilar de piedra), en el carril horizontal de su propio
            // grifo (desplazarYPx=0). UiStyles.PlacaMundoLateral desvanece
            // TAMBIÉN el panel de fondo con `alfa`, no solo el texto.
            UiStyles.PlacaMundoLateral(_anclaRotulo, texto, color, UiStyles.S(ChapaSeparacionPx), 0f, alfa, aLaIzquierda: false);

            // PROMPT "E — ...": solo mientras el taller aún lo está enseñando
            // (MachineFocus.MostrarPromptE), solo sobre el caño enfocado y solo
            // con las manos libres. Va DEBAJO de la chapa, en el MISMO carril
            // lateral (mismo _anclaRotulo, desplazamiento negativo = hacia
            // abajo) -- nunca centrado sobre el aparato.
            if (!Bloqueado && MachineFocus.MostrarPromptE && EstaEnfocado() && !UiStyles.RatonOcupado)
            {
                string prompt = _on ? "E — cerrar" : _promptAbrir;
                UiStyles.PlacaMundoLateral(_anclaRotulo, prompt, UiStyles.Oro,
                    UiStyles.S(ChapaSeparacionPx), UiStyles.S(PromptDesplazarYPx), 1f, aLaIzquierda: false);
            }
        }
    }
}
