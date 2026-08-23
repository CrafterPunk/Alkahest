using System;
using UnityEngine;
using Alkahest.Sim;
using Alkahest.Net;
// (playtest 40, SEMILLA CERO) `using Alkahest.Game;`: este es el ÚNICO sitio
// del proyecto donde se crea el Universe/CellGrid de una partida, así que es
// donde tiene que decidirse si aplicar los overrides de autor y tapiar las
// salas -- ver CrearMundoInterno. `AlkahestGameBootstrap.ModoSemillaCero` no
// tenía otro archivo natural donde vivir esta lectura: ninguno de los dos
// encargos de CONTRATO_SEMILLA.md lista Game/AlkahestSim.cs explícitamente,
// pero es el punto de integración obligado (documentado en el informe de la
// ronda como decisión fuera de contrato).
using Alkahest.Game;

namespace Alkahest
{
    /// <summary>
    /// Orquestador de la simulación: crea el Universe/CellGrid/SimStepper,
    /// hace avanzar la simulación a 30Hz fijos (con un acumulador de
    /// Time.deltaTime, máx. 2 pasos por frame para no entrar en espiral de
    /// muerte si el frame tarda demasiado) y expone la API pública que
    /// usará el resto del juego (pintar materiales, samplear celdas,
    /// convertir mundo↔celda).
    /// </summary>
    [RequireComponent(typeof(SimRenderer))]
    public sealed class AlkahestSim : MonoBehaviour
    {
        [Tooltip("0 = elegir una seed aleatoria al arrancar.")]
        [SerializeField] private int seed = 0;

        /// <summary>
        /// Seed a usar en el PRÓXIMO Start() de este componente, fijada por
        /// Game/DayCycle.cs justo antes de recargar la escena (Título ->
        /// "Entrar al taller", o "Reintentar mismo universo"/"Nuevo
        /// universo" desde la pantalla final). Se consume una sola vez
        /// (vuelve a null tras leerse) para no afectar futuras recargas que
        /// no la fijen explícitamente. null = usar el campo `seed` del
        /// inspector (0 en ese caso = aleatoria, comportamiento de siempre).
        /// </summary>
        public static int? NextRunSeed;

        private const float FixedDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;

        private Universe _universe;
        private CellGrid _grid;
        private SimStepper _stepper;
        private SimRenderer _renderer;

        private float _accumulator;

        public Universe Universe => _universe;
        public CellGrid Grid => _grid;
        public SimStepper Stepper => _stepper;
        public SimRenderer Renderer => _renderer;

        /// <summary>
        /// Pausa la simulación (deja de consumir el acumulador de tiempo /
        /// dar pasos de <see cref="SimStepper"/>) sin tocar Time.timeScale.
        /// Usado por Game/DayCycle.cs durante los overlays de jornada
        /// (Título, intro de día, fin de día, pantalla final) para congelar
        /// el mundo mientras se muestra un menú. El renderizado (RenderFrame)
        /// también se salta mientras está en pausa: la textura simplemente
        /// deja de refrescarse, congelando el último frame visible.
        /// </summary>
        public bool Paused { get; set; }

        // =====================================================================
        // EL ESPEJO (playtest 28, POC multiplayer — CONTRATO_MULTI_POC.md)
        // =====================================================================
        // La sim vive SOLO en el anfitrión. El invitado lleva un ESPEJO: el
        // MISMO CellGrid y el MISMO SimRenderer, pero SIN SimStepper — nadie
        // simula nada ahí, el contenido de `mat[]` llega por red en bloques RLE
        // por chunk (ver Net/SimSync.cs) y este componente solo lo aplica y lo
        // manda a pintar.
        //
        // Por qué el espejo es un MODO de esta clase y no una clase aparte:
        // porque todo lo que el jugador invitado toca (frasco, cámara, HUD del
        // frasco, muestreo de materiales) habla con AlkahestSim y con nadie
        // más. Un "AlkahestSimEspejo" separado habría obligado a tocar todos
        // esos consumidores; así no cambia ni una línea de ninguno de ellos.
        //
        // La escena Lab CLÁSICA nunca entra por aquí: `ModoEspejo` se queda en
        // false y `SimSync.EnEscena` en false, así que Start() construye el
        // mundo como siempre, en la misma línea de siempre.
        // =====================================================================

        /// <summary>
        /// True en un invitado: no hay SimStepper, no se simula nada, y las
        /// llamadas a Paint* se reenvían al anfitrión además de aplicarse
        /// localmente (ver <see cref="ReenviarSiEspejo"/>).
        /// </summary>
        public bool ModoEspejo { get; private set; }

        /// <summary>
        /// Reloj del espejo. Sustituye a `SimStepper.Tick` en todo lo que en
        /// el invitado sigue necesitando un tick (despertar chunks, decidir
        /// qué redibuja SimRenderer). Avanza a los mismos 30 Hz que la sim
        /// real para que la cadencia de render sea idéntica.
        /// </summary>
        public uint TickEspejo { get; private set; }

        /// <summary>Tick vigente, venga del stepper real o del espejo. Lo usan Paint*/WakeChunk.</summary>
        private uint TickActual => _stepper != null ? _stepper.Tick : TickEspejo;

        private void Awake()
        {
            _renderer = GetComponent<SimRenderer>();
        }

        private void Start()
        {
            // (playtest 28) EN LA ESCENA MULTI EL MUNDO NO NACE AQUÍ. Cuando
            // hay un SimSync en escena, quien decide qué mundo se construye —y
            // con qué seed— es la sesión: el anfitrión sortea una seed nueva al
            // pulsar ANFITRIÓN, y el invitado NO PUEDE construir nada hasta
            // saber la seed del anfitrión (los materiales, sus colores y su
            // química se generan POR SEMILLA: con otra seed, el espejo
            // enseñaría un mundo de otro universo). Ver Net/SimSync.cs.
            if (SimSync.EnEscena)
            {
                Debug.Log("[ChaosAlchemy] Escena MULTI: la creación del mundo espera a la sesión (ver Net/SimSync.cs).");
                return;
            }

            CrearMundo(0, false);
        }

        /// <summary>
        /// Construye el mundo del ANFITRIÓN: universo, grid, plano y stepper,
        /// exactamente igual que la escena de un jugador. `seed` 0 = el
        /// comportamiento clásico (campo del inspector, o aleatoria).
        /// </summary>
        public void CrearMundoAnfitrion(int seedDeLaSesion) => CrearMundo(seedDeLaSesion, false);

        /// <summary>
        /// Construye el ESPEJO de un invitado con la seed que mandó el
        /// anfitrión: mismo universo (mismos materiales, mismos colores,
        /// mismas leyes) y mismo plano de partida, pero sin stepper. El plano
        /// se construye igual a propósito: así el invitado ya tiene la piedra
        /// del taller antes incluso de aplicar el snapshot, y lo que llega por
        /// red solo tiene que corregir lo que ha cambiado desde entonces.
        /// </summary>
        public void CrearMundoEspejo(int seedDelAnfitrion) => CrearMundo(seedDelAnfitrion, true);

        /// <summary>
        /// Marca este sim como espejo ANTES de que llegue la seed. Lo llama
        /// SimSync en cuanto sabe que es cliente: a partir de aquí, cualquier
        /// Paint* que llegue (por ejemplo de un frasco cableado antes de
        /// tiempo) ya sabe que tiene que viajar al anfitrión.
        /// </summary>
        public void PrepararEspejo()
        {
            ModoEspejo = true;
        }

        private void CrearMundo(int seedPedida, bool espejo)
        {
            if (_grid != null)
            {
                Debug.LogWarning("[ChaosAlchemy] CrearMundo llamado dos veces: se ignora la segunda (SimRenderer.Init NO es idempotente, regla 36).");
                return;
            }

            ModoEspejo = espejo;
            if (seedPedida != 0) seed = seedPedida;

            CrearMundoInterno(espejo);
        }

        private void CrearMundoInterno(bool espejo)
        {
            if (espejo)
            {
                // EL ESPEJO NO ELIGE SU SEED, LA OBEDECE. `NextRunSeed` (la
                // que fija DayCycle entre partidas) se ignora aquí a
                // propósito: si quedara un valor suelto de una partida
                // anterior de un jugador, el invitado construiría OTRO
                // universo -- mismos ids de material, colores y química
                // distintos -- y el espejo enseñaría el mundo del anfitrión
                // pintado con la paleta equivocada, sin ningún error visible.
                if (seed == 0)
                {
                    Debug.LogError("[ChaosAlchemy] Espejo sin seed del anfitrión: el universo no puede coincidir. Reconéctate.");
                }
            }
            else if (NextRunSeed.HasValue)
            {
                seed = NextRunSeed.Value;
                NextRunSeed = null;
                Debug.Log($"[ChaosAlchemy] Seed fijada por DayCycle para esta run: {seed}");
            }
            else if (seed == 0)
            {
                seed = Environment.TickCount;
                Debug.Log($"[Alkahest] Seed no especificada, usando seed aleatoria: {seed}");
            }

            _universe = Universe.Create(seed);
            // (playtest 40, SEMILLA CERO, CONTRATO_SEMILLA.md §3) La pasada de
            // overrides de autor corre AQUÍ, justo después de la generación
            // normal, antes de construir plano ni stepper -- así todo lo que
            // lee `_universe` después (el plano, las máquinas, el diario) ya
            // ve el universo final.
            //
            // (CONTRATO_RONDA50.md §4b, ENCARGO M, SEMILLA CERO COMPARTIDA)
            // YA NO es "nunca en el espejo": ese comentario describía la
            // única realidad de antes de esta ronda (Semilla Cero solo
            // existía en la escena de un jugador). Ahora el LOBBY multi
            // (Net/TallerSesionHud.cs, botón "ANFITRIÓN — SEMILLA CERO
            // compartida") puede poner `ModoSemillaCero` en `true` también en
            // la escena MULTI, y el INVITADO tiene que aplicar los MISMOS
            // overrides sobre SU PROPIO `_universe` del espejo -- si no, su
            // Universe local tendría los ids/colores correctos (misma seed)
            // pero las propiedades/identidades reales de autor NO (Universe
            // es un objeto por-proceso, los overrides lo mutan en sitio, y
            // cada lado de la red construye el suyo, ver Net/SimSync.cs). Es
            // Net/SimSync.cs quien pone el flag en el invitado ANTES de
            // llamar a CrearMundoEspejo, justo al detectar
            // `seed == Universe.SemillaCero` en la cabecera del snapshot
            // (ver SimSync.AlRecibirChunks) -- así que para cuando se llega
            // aquí el flag YA está en su valor final para este proceso, y
            // basta con leerlo sin distinguir host/invitado/un jugador.
            // (RONDA 60) ModoFundacion comparte la seed de autor Y sus decretos
            // (regla 57: lo que el guion promete no depende del sorteo) -- la
            // barbotina del beat 3, p. ej., existe porque el Override 6b hace
            // soluble a la arcilla POR DECRETO.
            if (AlkahestGameBootstrap.ModoSemillaCero || AlkahestGameBootstrap.ModoFundacion)
                Universe.AplicarOverridesSemillaCero(_universe);
            _grid = new CellGrid();
            // (playtest 21, EL PIVOT) La partida arranca en el CUARTO ÍNTIMO,
            // no en el taller clásico -- "el cuarto íntimo pasa a ser EL
            // juego", decisión de Cesar, CONTRATO_PIVOT.md. `BuildTestLevel`
            // NO se borra (el taller grande sigue entero, solo que ahora
            // ENTERRADO bajo la piedra que rellena `BuildCuartoIntimo`): la
            // rama existe aquí, en el ÚNICO sitio del proyecto donde se
            // decide qué plano construir, para el día en que el taller
            // clásico vuelva a excavarse de verdad en vez de generarse de
            // fábrica ya abierto.
            // (RONDA 60, GDD v0.3 §5) LA FUNDACIÓN construye SU plano (mundo
            // casi vacío) en vez del cuarto íntimo. Mismo criterio que la rama
            // BuildTestLevel de arriba: este es el único sitio del proyecto
            // donde se decide qué plano construir.
            if (AlkahestGameBootstrap.ModoFundacion) SimLevelBuilder.BuildFundacion(_grid);
            else SimLevelBuilder.BuildCuartoIntimo(_grid);
            // (RONDA 69g, diagnóstico del "multi roto") LA LÍNEA DE LA VERDAD:
            // una sola línea que dice exactamente QUÉ mundo construyó este
            // proceso y con qué flags. Cuando un invitado vea el mundo "raro",
            // comparar esta línea entre las dos consolas responde en segundos
            // si los dos lados construyeron el mismo universo -- la fuga de
            // ModoFundacion (ver Net/SimSync.cs, ronda 69g) se habría cazado
            // al primer vistazo con esto en pantalla.
            Debug.Log("[ChaosAlchemy] Mundo construido: plano=" +
                (AlkahestGameBootstrap.ModoFundacion ? "FUNDACION" : "CUARTO") +
                " seed=" + seed +
                " espejo=" + espejo +
                " semillaCero=" + AlkahestGameBootstrap.ModoSemillaCero +
                " fundacion=" + AlkahestGameBootstrap.ModoFundacion +
                " overrides=" + (AlkahestGameBootstrap.ModoSemillaCero || AlkahestGameBootstrap.ModoFundacion));
            // (playtest 40, SEMILLA CERO) Tapiado de las cuatro salas por
            // pregunta -- API CONGELADA (SimLevelBuilder.TapiarSalasSemillaCero,
            // ver su docblock). Después de BuildCuartoIntimo (necesita que las
            // cinco estaciones ya hayan tallado su mampostería y registrado su
            // rect en ObraDelTaller) y antes de crear el stepper/renderer (no
            // hace falta ninguno de los dos: solo escribe CellGrid).
            //
            // (CONTRATO_RONDA50.md §4b, ENCARGO M) `&& !SimSync.EnEscena` fue
            // NUEVO en el playtest 50: el laboratorio compartido de entonces
            // pedía "TODAS las salas DESTAPADAS" (contrato, textual) -- un
            // banco de pruebas simultáneas, sin arco.
            //
            // REVERTIDO (playtest 52, CO-OP GUIADO, mandato literal de Cesar:
            // "que la Semilla 0 en multiplayer escale igual como tienes
            // pensado para la versión solo player"). El laboratorio destapado
            // deja de ser el diseño vigente: `Game/SemillaCero.cs` (Encargo
            // único de la ronda 52) ahora SÍ se instancia en el anfitrión del
            // multi (`TrySpawnRed`, rama `anfitrion`, ver ese archivo) y es
            // quien va destapando sala a sala con `SimLevelBuilder.DestaparSala`
            // según el jugador avanza por los beats -- exactamente el mismo
            // mecanismo que el modo un jugador. Con el gate `!SimSync.EnEscena`
            // puesto, el anfitrión NUNCA tapiaba nada y el director destapaba
            // salas que ya estaban abiertas (inofensivo pero inútil) mientras
            // las máquinas seguían naciendo TODAS de golpe en TrySpawnRed (ver
            // ese archivo, playtest 52): el arco guiado quedaba mudo pese a
            // que el director sí corría. Quitar el gate es lo que hace CIERTA
            // la frase "salas tapiadas" en multi. `!espejo` sigue excluyendo
            // al invitado por su propia cuenta (nunca construye nada de plano
            // por decisión propia, ver el docblock de esta clase) -- este
            // archivo no está en la lista de archivos exclusivos del Encargo
            // único de la ronda 52 (`Game/SemillaCero.cs`, `Game/
            // AlkahestGameBootstrap.cs`, `Game/SubstanceKnowledge.cs`,
            // `Net/SaberSync.cs`, `Game/OrdersHud.cs`); esta única línea se
            // toca aquí porque el mandato de esa ronda la señala
            // explícitamente ("el gate... se REVIERTE para que el host
            // tapie") y sin ella el resto del encargo no puede cumplirse --
            // documentado como decisión fuera de contrato en el informe de
            // esa ronda.
            if (!espejo && AlkahestGameBootstrap.ModoSemillaCero) SimLevelBuilder.TapiarSalasSemillaCero(this);

            // EL ESPEJO NO TIENE STEPPER. No es que esté pausado: NO EXISTE.
            // Es la garantía estructural de que un invitado no puede simular
            // por su cuenta y desincronizarse — no hay nada que ejecutar.
            _stepper = espejo ? null : new SimStepper(_universe, _grid);

            if (_renderer == null)
            {
                Debug.LogError("[Alkahest] AlkahestSim requiere un componente SimRenderer en el mismo GameObject.");
                enabled = false;
                return;
            }

            _renderer.Init(_universe, _grid);

            Debug.Log($"[Alkahest] Universo creado con seed {seed}{(espejo ? " (ESPEJO: sin stepper, la sim vive en el anfitrión)" : "")}. " +
                      $"Grid {CellGrid.W}x{CellGrid.H}, chunks {CellGrid.ChunksX}x{CellGrid.ChunksY}.");
            Debug.Log($"[Alkahest] Edicto de este universo ({_universe.ActiveEdicto}): {_universe.EdictoDescripcion}");
        }

        private void Update()
        {
            if (Paused) return;

            // (playtest 28) EL ESPEJO tiene su propio Update: no simula, pero
            // sí lleva el reloj y el mismo ciclo de sueño de chunks, porque de
            // los dos depende que SimRenderer redibuje lo justo (ver
            // ActualizarEspejo).
            if (ModoEspejo) { ActualizarEspejo(); return; }

            if (_stepper == null) return;

            _accumulator += Time.deltaTime;
            int steps = 0;
            while (_accumulator >= FixedDt && steps < MaxStepsPerFrame)
            {
                _stepper.Step();
                _accumulator -= FixedDt;
                steps++;
            }

            // Si nos quedamos muy atrás (editor pausado, spike grande...) no
            // dejamos que el acumulador crezca sin límite.
            if (_accumulator > FixedDt * MaxStepsPerFrame)
            {
                _accumulator = FixedDt * MaxStepsPerFrame;
            }

            if (steps > 0)
            {
                _renderer.RenderFrame(_stepper.Tick);
            }
        }

        /// <summary>
        /// El "tick" del invitado. Hace las DOS tareas de contabilidad que en
        /// el anfitrión hace <see cref="SimStepper.Step"/> y que
        /// <see cref="SimRenderer"/> necesita para no redibujar de más:
        ///
        ///  1) Avanza <see cref="TickEspejo"/> con el mismo acumulador de
        ///     30 Hz, así el render se refresca a la misma cadencia que en un
        ///     jugador (ni más, ni a tirones).
        ///  2) DUERME LOS CHUNKS QUIETOS, con el mismo criterio literal del
        ///     stepper (`chunkTouchedTick != tick` -> TickChunkIdle). Sin esto
        ///     todos los chunks del espejo se quedarían despiertos para
        ///     siempre —nadie los adormece— y SimRenderer repintaría la
        ///     pantalla entera cada frame en vez de solo lo que cambió.
        /// </summary>
        private void ActualizarEspejo()
        {
            if (_grid == null || _renderer == null) return;

            _accumulator += Time.deltaTime;
            int steps = 0;
            while (_accumulator >= FixedDt && steps < MaxStepsPerFrame)
            {
                TickEspejo++;
                DormirChunksQuietos();
                _accumulator -= FixedDt;
                steps++;
            }

            if (_accumulator > FixedDt * MaxStepsPerFrame)
            {
                _accumulator = FixedDt * MaxStepsPerFrame;
            }

            if (steps > 0)
            {
                _renderer.RenderFrame(TickEspejo);
            }
        }

        private void DormirChunksQuietos()
        {
            for (int cy = 0; cy < CellGrid.ChunksY; cy++)
            {
                for (int cx = 0; cx < CellGrid.ChunksX; cx++)
                {
                    int ci = CellGrid.ChunkIndex(cx, cy);
                    if (_grid.chunkTouchedTick[ci] != TickEspejo)
                    {
                        _grid.TickChunkIdle(cx, cy);
                    }
                }
            }
        }

        /// <summary>
        /// Aplica al espejo un chunk recibido del anfitrión, comprimido RLE
        /// (parejas material+cuenta, recorriendo el chunk por filas). Lo llama
        /// Net/SimSync.cs y NADIE más.
        ///
        /// Solo viaja `mat[]`: `temp[]` y `morph[]` no se sincronizan en el
        /// POC (ver el docblock de SimSync). Se usa `CellGrid.SetCell` en vez
        /// de escribir `mat` a pelo a propósito: así la celda nace con su
        /// semilla morfológica de siempre y el espejo enseña el patrón del
        /// material (congelado, pero presente) en vez de un tinte plano.
        /// </summary>
        public void AplicarChunkRemoto(int indiceChunk, byte[] rle, int parejas)
        {
            if (_grid == null || rle == null) return;
            if (indiceChunk < 0 || indiceChunk >= CellGrid.ChunksX * CellGrid.ChunksY) return;

            int cx = indiceChunk % CellGrid.ChunksX;
            int cy = indiceChunk / CellGrid.ChunksX;
            CellGrid.ChunkBounds(cx, cy, out int x0, out int y0, out int x1, out int y1);

            int pareja = 0;
            int restantesDeLaPareja = 0;
            byte materialActual = MaterialId.Empty;
            bool cambio = false;

            for (int y = y0; y < y1; y++)
            {
                int fila = y * CellGrid.W;
                for (int x = x0; x < x1; x++)
                {
                    if (restantesDeLaPareja <= 0)
                    {
                        if (pareja >= parejas) return; // paquete corto: se deja el resto del chunk como estaba
                        materialActual = rle[pareja * 2];
                        restantesDeLaPareja = rle[pareja * 2 + 1];
                        pareja++;
                        if (restantesDeLaPareja <= 0) return; // cuenta 0: paquete corrupto
                    }

                    int idx = fila + x;
                    if (_grid.mat[idx] != materialActual)
                    {
                        _grid.SetCell(idx, materialActual);
                        cambio = true;
                    }

                    restantesDeLaPareja--;
                }
            }

            // Despertar SOLO este chunk (no el vecindario 3x3 de WakeChunk): en
            // el espejo despertar sirve únicamente para que SimRenderer lo
            // repinte, y el anfitrión ya manda por separado todos los chunks
            // que de verdad cambiaron.
            if (cambio) _grid.WakeChunkIndex(cx, cy, TickEspejo);
        }

        // ---------------------------------------------------------------------------------
        // API pública para gameplay / dev tools.
        // ---------------------------------------------------------------------------------

        /// <summary>Id de material en (x,y), o Empty si está fuera de rango.</summary>
        public int SampleMaterial(int x, int y)
        {
            if (_grid == null || !CellGrid.InBounds(x, y)) return MaterialId.Empty;
            return _grid.GetMat(x, y);
        }

        /// <summary>Temperatura "raw" (0..255) en (x,y), o la ambiente si está fuera de rango. Ver CellGrid.RawToC.</summary>
        public byte SampleTempRaw(int x, int y)
        {
            if (_grid == null || !CellGrid.InBounds(x, y)) return CellGrid.AmbientRaw;
            return _grid.temp[CellGrid.Idx(x, y)];
        }

        // =====================================================================
        // MUTACIONES EN SESIÓN (playtest 28): EL INVITADO PIDE, EL ANFITRIÓN
        // MANDA — PERO EL INVITADO TAMBIÉN PINTA EN SU ESPEJO.
        // =====================================================================
        // El contrato pedía literalmente que en modo espejo las llamadas a
        // Paint* "NO tocaran el espejo" y solo se reenviaran al anfitrión. Se
        // implementó así primero y SE CORRIGIÓ, con esta razón (queda escrita
        // aquí, estilo regla 15, para que nadie la "arregle" de vuelta):
        //
        //   El frasco (Game/Flask.cs) LEE la grilla y ESCRIBE en la misma
        //   pasada: `_sim.Paint(x, y, 0, Empty)` es lo que hace que la celda
        //   aspirada desaparezca, y en el tick siguiente vuelve a mirar esas
        //   mismas celdas. Si el espejo no cambia hasta que el anfitrión
        //   responde (~200 ms, 6 ticks), el frasco aspira LA MISMA celda una y
        //   otra vez: un charco de 5 celdas llenaría el frasco con cientos, y
        //   al verterlas se estaría FABRICANDO MATERIA en el mundo
        //   autoritativo. No es un problema de latencia percibida, es
        //   duplicación de materia.
        //
        // Así que el espejo hace PREDICCIÓN LOCAL: aplica el cambio ya y lo
        // manda al anfitrión, que es el único que decide de verdad. Lo que
        // vuelva por el sync de chunks (5 Hz) sobreescribe la predicción sin
        // preguntar — el anfitrión siempre gana, que es lo que el contrato
        // protegía de raíz.
        // =====================================================================

        /// <summary>Si somos un espejo, manda esta mutación al anfitrión (además de aplicarla localmente como predicción).</summary>
        private void ReenviarSiEspejo(int x, int y, int radio, byte materialId, byte modo, byte tempRaw)
        {
            if (!ModoEspejo) return;
            SimSync.ReenviarPintura(x, y, radio, materialId, modo, tempRaw);
        }

        /// <summary>Pinta un disco de radio `radius` centrado en (x,y) con el material indicado.</summary>
        public void Paint(int x, int y, int radius, byte materialId)
        {
            if (_grid == null) return;
            ReenviarSiEspejo(x, y, radius, materialId, SimSync.ModoPaint, 0);
            if (radius < 0) radius = 0;
            int r2 = radius * radius;

            for (int dy = -radius; dy <= radius; dy++)
            {
                int py = y + dy;
                if (py <= 0 || py >= CellGrid.H - 1) continue; // no pintar sobre el borde
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > r2) continue;
                    int px = x + dx;
                    if (px <= 0 || px >= CellGrid.W - 1) continue;

                    _grid.SetCell(px, py, materialId);
                    _grid.WakeChunk(px, py, TickActual);
                }
            }
        }

        /// <summary>
        /// Pinta UNA celda con material Y temperatura. Existe porque el Frasco
        /// del aprendiz conserva el frío/calor de lo que aspira (ver Game/Flask.cs):
        /// sin esto, verter hielo en la Tolva entregaba una celda a temperatura
        /// AMBIENTE y los encargos "algo helado" / "algo que queme al tacto"
        /// eran literalmente imposibles de cumplir.
        ///
        /// Igual que <see cref="Paint"/>, nunca escribe sobre el borde del mundo
        /// y despierta el chunk afectado.
        ///
        /// NO TOCAR el comportamiento de este método para arreglar el fix de
        /// playtest 13 de más abajo (<see cref="PaintStable"/>): Flask ya pasa
        /// la temperatura MEDIA correcta aquí, y esa ruta está validada desde
        /// el playtest 4. El fix de "materia creada de la nada nace inestable"
        /// vive en un método aparte a propósito.
        /// </summary>
        public void PaintCell(int x, int y, byte materialId, byte tempRaw)
        {
            if (_grid == null) return;
            ReenviarSiEspejo(x, y, 0, materialId, SimSync.ModoPaintCell, tempRaw);
            if (x <= 0 || x >= CellGrid.W - 1 || y <= 0 || y >= CellGrid.H - 1) return;

            int idx = CellGrid.Idx(x, y);
            _grid.SetCell(idx, materialId);
            _grid.temp[idx] = tempRaw;
            _grid.WakeChunk(x, y, TickActual);
        }

        // =====================================================================
        // (fix playtest 13) "Al seleccionar hielo, tiro agua."
        // =====================================================================
        // Diagnóstico confirmado leyendo Sim/SimStepper.cs (ApplyPhase) y
        // Sim/Universe.cs (mats[MaterialId.Ice]):
        //   - `Paint`/`SetCell` NUNCA tocan CellGrid.temp: una celda pintada
        //     hereda la temperatura que hubiera antes ahí, que en la enorme
        //     mayoría de casos es CellGrid.AmbientRaw (70 raw = 20°C, valor de
        //     partida de TODO el grid en el constructor de CellGrid).
        //   - El Hielo define `meltsAt = CToRaw(waterFreezeC + 5)`, y
        //     `waterFreezeC` varía por seed en [-20, 15] (Universe.Create).
        //     Eso deja `meltsAt` en el rango raw [52, 70] en CUALQUIER seed.
        //   - `ApplyPhase` funde con `t >= meltsAt` (SimStepper.cs línea ~397).
        //     Como AmbientRaw = 70 es el EXTREMO SUPERIOR de ese rango, la
        //     condición `70 >= meltsAt` es SIEMPRE verdadera, sin excepción de
        //     seed: el Hielo pintado con la paleta se funde a Agua en el
        //     primerísimo tick. Diagnóstico del reporte CONFIRMADO con números
        //     reales, no solo plausible.
        //
        // Arreglo GENERAL (no un parche solo para Hielo): al pintar materia de
        // la nada, la celda debe nacer a una temperatura en la que ESE
        // material sea estable, derivada de su propia MaterialDef. Ver
        // StableBirthTempRaw para el cálculo completo y por qué NO hacía
        // falta tocar Agua/Cristal/Vivium/etc (ya son estables en ambiente en
        // todo seed, la cuenta está en el comentario del método).
        //
        // Vive en un método NUEVO (PaintStable) y no en Paint/PaintCell a
        // propósito: Flask.cs usa Paint (para vaciar, MaterialId.Empty no
        // tiene transiciones así que da igual) y PaintCell (para verter,
        // donde la temperatura correcta es la del Frasco, no la de
        // estabilidad del material) -- ninguno de los dos caminos que usa
        // Flask debía cambiar de comportamiento. Comprobado con grep que los
        // únicos llamantes de Paint/PaintCell/PaintRect son Flask,
        // MasterSupplies, DeliveryChute, Dispenser y DevPalette (este último,
        // el único que pasa a usar PaintStable para el pincel; su borrador
        // sigue en Paint(..., MaterialId.Empty), sin cambios).
        // =====================================================================

        /// <summary>Colchón, en raw (1 raw = 2°C, ver CellGrid.RawToC), entre la temperatura de nacimiento corregida y el umbral de transición que la disparó. 10 raw = 20°C: de sobra para que el redondeo de CToRaw o el primer paso de difusión no la devuelvan al otro lado en el mismo tick.</summary>
        private const int StableBirthMarginRaw = 10;

        /// <summary>
        /// Temperatura "raw" a la que debe nacer una celda pintada de la nada
        /// para que <paramref name="def"/> sea ESTABLE justo al nacer (fix
        /// playtest 13, ver el bloque de comentario de arriba). Vale para
        /// CUALQUIER material, no solo Hielo:
        ///
        ///  - Sin ninguna transición de fase activa (meltsAt/boilsAt/
        ///    freezesAt/condensesAt en su sentinel "nunca"): AMBIENTE. Caso de
        ///    Stone/Sand/Oil/Nutrient/Vivium/Azoth/CrystalSeed/Slime/Acid y de
        ///    los subproductos Fire/Smoke/Ash (vida corta por gasLifetime,
        ///    pero sin transición de fase que ambiente pueda disparar).
        ///  - Con una cota SUPERIOR activa (meltsAt y/o boilsAt: ApplyPhase
        ///    las dispara con `t >= umbral`) que AMBIENTE ya cruzaría: nace
        ///    holgadamente por DEBAJO de la más baja de esas cotas. Único caso
        ///    real en el roster: Hielo (meltsAt en raw [52,70], ambiente=70
        ///    siempre lo cruza) -- nace en `meltsAt - margen`, es decir,
        ///    siempre 20°C por debajo de SU PROPIO punto de fusión, sea cual
        ///    sea la seed.
        ///  - Con una cota INFERIOR activa (freezesAt y/o condensesAt:
        ///    `t <= umbral`) que AMBIENTE ya cruzaría: nace holgadamente por
        ///    ENCIMA de la más alta. Único caso real: Vapor (condensesAt =
        ///    CToRaw(waterBoilC-40), raw [80,99] según seed, siempre por
        ///    encima de ambiente=70) -- nace en `condensesAt + margen`.
        ///  - Si AMBIENTE ya cae DENTRO de la banda (el caso normal: Agua
        ///    entre freezesAt[50,67] y boilsAt[100,119], ambiente=70 nunca
        ///    cruza ninguno de los dos en ningún seed; Cristal con
        ///    meltsAt=CToRaw(300)=210, muy por encima de 70): se deja AMBIENTE
        ///    tal cual. No hay razón para mover una temperatura que ya
        ///    funciona, y el panel de hover de DevPalette sigue leyendo "20°C"
        ///    para casi todo, como siempre.
        ///
        /// La comparación de "¿ambiente cruza el umbral?" es la MISMA que usa
        /// SimStepper.ApplyPhase (`>=`/`<=`, no una versión "por si acaso" con
        /// margen ya incluido): así el margen solo se aplica cuando hace falta
        /// corregir, y Agua/Cristal no se mueven un solo raw en ningún seed.
        /// </summary>
        private static byte StableBirthTempRaw(MaterialDef def)
        {
            int lower = int.MinValue; // cota inferior activa MÁS ALTA (freezesAt / condensesAt)
            if (def.freezesAt != short.MinValue) lower = Math.Max(lower, def.freezesAt);
            if (def.condensesAt != short.MinValue) lower = Math.Max(lower, def.condensesAt);

            int upper = int.MaxValue; // cota superior activa MÁS BAJA (meltsAt / boilsAt)
            if (def.meltsAt != short.MaxValue) upper = Math.Min(upper, def.meltsAt);
            if (def.boilsAt != short.MaxValue) upper = Math.Min(upper, def.boilsAt);

            int candidate = CellGrid.AmbientRaw;

            bool violaSuperior = upper != int.MaxValue && candidate >= upper;
            bool violaInferior = lower != int.MinValue && candidate <= lower;

            if (violaSuperior) candidate = upper - StableBirthMarginRaw;
            else if (violaInferior) candidate = lower + StableBirthMarginRaw;

            if (candidate < 0) candidate = 0;
            if (candidate > 255) candidate = 255;
            return (byte)candidate;
        }

        /// <summary>
        /// Igual que <see cref="Paint"/> (disco de radio `radius`), pero la
        /// celda nace a la temperatura de estabilidad de <paramref
        /// name="materialId"/> (ver <see cref="StableBirthTempRaw"/>) en vez
        /// de heredar lo que hubiera antes en la celda: materia creada DE LA
        /// NADA debe nacer siendo lo que la creó pretendía, no otra cosa un
        /// tick después (fix playtest 13, "pintar hielo produce agua").
        ///
        /// (playtest 17) YA NO ES "SOLO PARA LA PALETA DE DEV", como decía
        /// esta línea: `Game/Dispenser.EmitTick` es ahora el segundo
        /// consumidor, y por el mismo motivo — un grifo también crea materia
        /// de la nada, y con `Paint` el agua recién salida heredaba la
        /// temperatura del hueco (si la boquilla se había enfriado alguna vez,
        /// nacía congelada). REGLA GENERAL: si algo INTRODUCE materia en el
        /// mundo en vez de moverla, usa esto, no `Paint`. `Paint`/`PaintCell`/
        /// `PaintRect` siguen siendo lo correcto para lo que MUEVE materia que
        /// ya existía y lleva su propia temperatura consigo (Flask al verter,
        /// DeliveryChute, MasterSupplies).
        /// </summary>
        public void PaintStable(int x, int y, int radius, byte materialId)
        {
            if (_grid == null || _universe == null) return;
            ReenviarSiEspejo(x, y, radius, materialId, SimSync.ModoPaintStable, 0);
            if (radius < 0) radius = 0;
            int r2 = radius * radius;
            byte tempRaw = StableBirthTempRaw(_universe.Get(materialId));

            for (int dy = -radius; dy <= radius; dy++)
            {
                int py = y + dy;
                if (py <= 0 || py >= CellGrid.H - 1) continue; // no pintar sobre el borde
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > r2) continue;
                    int px = x + dx;
                    if (px <= 0 || px >= CellGrid.W - 1) continue;

                    int idx = CellGrid.Idx(px, py);
                    _grid.SetCell(idx, materialId);
                    _grid.temp[idx] = tempRaw;
                    _grid.WakeChunk(px, py, TickActual);
                }
            }
        }

        /// <summary>
        /// Rellena un rectángulo de celdas con un material. Usado por las
        /// "muestras del Maestro" de la jornada 2 (Game/MasterSupplies.cs) para
        /// dejar un saquito de semilla de cristal sobre el estante con una
        /// cantidad exacta y predecible (un disco de <see cref="Paint"/> no
        /// permite pedir "60 celdas").
        /// </summary>
        public void PaintRect(int x0, int y0, int width, int height, byte materialId)
        {
            if (_grid == null) return;
            // El reenvío empaqueta ancho y alto en los dos bytes libres del
            // registro de pintura (ver Net/SimSync.cs): 255 de tope, de sobra
            // para el único consumidor real (las muestras del Maestro).
            ReenviarSiEspejo(x0, y0, Mathf.Clamp(width, 0, 255), materialId,
                SimSync.ModoPaintRect, (byte)Mathf.Clamp(height, 0, 255));

            for (int y = y0; y < y0 + height; y++)
            {
                if (y <= 0 || y >= CellGrid.H - 1) continue;
                for (int x = x0; x < x0 + width; x++)
                {
                    if (x <= 0 || x >= CellGrid.W - 1) continue;
                    _grid.SetCell(CellGrid.Idx(x, y), materialId);
                    _grid.WakeChunk(x, y, TickActual);
                }
            }
        }

        /// <summary>
        /// (playtest 44, CONTRATO_TERMICA §2b) Escribe la temperatura de UNA
        /// celda SIN tocar su material -- el camino de la SIM que faltaba: el
        /// docblock de HeatPlate/ChillStone anotaba desde el playtest 4 la
        /// deuda de escribir <c>_sim.Grid.temp[]</c> a mano en vez de pasar
        /// por una API dedicada. Misma disciplina que <see cref="Paint"/>/
        /// <see cref="PaintCell"/>: no escribe sobre el borde del mundo (el
        /// marco de Stone es inmutable) y despierta el chunk afectado. A
        /// diferencia de <see cref="PaintCell"/> (que SÍ cambia el material)
        /// esto NUNCA toca <c>mat</c> -- es para quien EMPUJA calor/frío hacia
        /// materia que ya existe (las placas), nunca para crear materia.
        ///
        /// NO se reenvía al anfitrión en modo espejo (a diferencia de
        /// Paint/PaintCell/PaintStable/PaintRect): la temperatura NO viaja
        /// por la red hoy (ver <see cref="AplicarChunkRemoto"/>, "temp[] no
        /// se sincroniza en el POC") y las placas solo existen/corren en el
        /// ANFITRIÓN (regla del contrato de esta ronda) -- ModoEspejo nunca
        /// llama a este método en la práctica, así que no hay nada que
        /// predecir localmente ni que reenviar.
        /// </summary>
        public void InyectarTemperatura(int x, int y, byte tempRaw)
        {
            if (_grid == null) return;
            if (x <= 0 || x >= CellGrid.W - 1 || y <= 0 || y >= CellGrid.H - 1) return;
            int idx = CellGrid.Idx(x, y);
            _grid.temp[idx] = tempRaw;
            _grid.WakeChunk(x, y, TickActual);
        }

        public Vector2Int WorldToCell(Vector3 worldPos)
        {
            int cx = Mathf.FloorToInt(worldPos.x / SimRenderer.CellWorldSize);
            int cy = Mathf.FloorToInt(worldPos.y / SimRenderer.CellWorldSize);
            return new Vector2Int(cx, cy);
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            float wx = (cell.x + 0.5f) * SimRenderer.CellWorldSize;
            float wy = (cell.y + 0.5f) * SimRenderer.CellWorldSize;
            return new Vector3(wx, wy, 0f);
        }

        /// <summary>Fuerza un único tick de simulación + redibujado, ignorando el acumulador de Time.deltaTime. Pensado para el modo "single-step" de las dev tools.</summary>
        public void StepOnce()
        {
            if (_stepper == null || _renderer == null) return;
            _stepper.Step();
            _renderer.RenderFrame(_stepper.Tick);
        }
    }
}
