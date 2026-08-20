using UnityEngine;
using Alkahest.Sim;
using Alkahest.Net;

namespace Alkahest.Game
{
    /// <summary>
    /// EL DIRECTOR DE SEMILLA CERO (Encargo G, CONTRATO_SEMILLA.md, playtest 40): la
    /// máquina de estados de los BEATS del arco guiado (contrato §1) --
    /// milagro → primera petición → el nombre se gana → fracaso forense → cuatro
    /// preguntas → final abierto. Escucha lo que ya existe
    /// (<see cref="SubstanceKnowledge"/>, <see cref="OrderSystem"/>, con sondeo barato
    /// por acumulador -- NUNCA por frame, NUNCA concatenando strings salvo en el
    /// instante exacto de un cambio de beat) y ordena: los textos EXACTOS del Maestro,
    /// los pedidos scriptados (<see cref="OrderSystem.EncolarPedidoGuiado"/>), las
    /// llamadas de destape (<see cref="SimLevelBuilder.DestaparSala"/>, API congelada
    /// del Encargo M) y el contador de autonomía post-final-abierto.
    ///
    /// GATE DURO DE ESCENA (contrato §2): Semilla 0 es SOLO de la escena de un
    /// jugador. <see cref="Init"/> comprueba <see cref="SimSync.EnEscena"/> ANTES de
    /// guardar ninguna referencia -- si alguna vez se llama desde la escena MULTI (no
    /// debería: el contrato dice que ahí no existe ni el botón ni el flag), esta clase
    /// se queda con `_sim == null` para siempre y <see cref="Update"/> corta en la
    /// primera línea, sin tocar nada más.
    ///
    /// QUIÉN LA INSTANCIA: <c>Game/AlkahestGameBootstrap.cs</c> (archivo del Encargo M
    /// en esta misma ronda, propiedad disjunta -- no se toca aquí). El enganche exacto
    /// que falta añadir ahí, DENTRO de <c>TrySpawn()</c> (nunca en TrySpawnRed, que es
    /// la rama multi), justo después de que existan `knowledge`/`orderSystem`:
    /// <code>
    /// if (AlkahestGameBootstrap.ModoSemillaCero)
    /// {
    ///     var semillaCero = new GameObject("SemillaCero").AddComponent&lt;SemillaCero&gt;();
    ///     semillaCero.Init(_sim, knowledge, orderSystem);
    /// }
    /// </code>
    /// Deliberadamente NO necesita una referencia a <see cref="Flask"/> por Init: la
    /// resuelve solo si algún día hiciera falta, con el mismo patrón defensivo de
    /// reintento en Update que ya usa <c>AlkahestGameBootstrap.Start/Update</c> para
    /// encontrar <see cref="AlkahestSim"/> -- así el orden de creación entre sistemas
    /// nunca puede romper este enganche.
    ///
    /// (Encargo Q, ronda "LA QUÍMICA CON NOMBRE REAL") IDEA DESCARTADA -- YA NO
    /// depende de <see cref="NamingUi"/> en absoluto: el beat 3 forzaba antes su
    /// apertura (<c>AbrirPorElMaestro</c>) para exigir un rito de bautizo. Con
    /// <see cref="Game.SubstanceKnowledge.NombreDe"/> devolviendo el nombre REAL
    /// para todo matId con identidad (<see cref="Alkahest.Sim.Universe.TieneIdentidadReal"/>)
    /// en Semilla Cero, el beat 3 pasó de "exigir que inventes un nombre" a "el
    /// Maestro te enseña el nombre real" -- una línea hablada, sin campo de texto,
    /// sin abrir ningún panel. Ver <see cref="EntrarNombreSeGana"/>. No reimplantar
    /// el forzado de NamingUi aquí sin releer docs/DISENO_QUIMICA_REAL.md §4.
    ///
    /// PEDIDOS SCRIPTADOS: en vez de <see cref="OrderSystem"/> generando nada por su
    /// cuenta, esta clase encola UN pedido activo cada vez
    /// (<see cref="OrderSystem.EncolarPedidoGuiado"/>, que sustituye el pedido entero) y
    /// sondea <c>OrderSystem.ActiveOrders[0].Completado</c> para avanzar de beat. Las
    /// cuatro preguntas del beat 5 reutilizan a propósito los tipos YA existentes de
    /// "LO QUE PERSISTE" (playtest 25) que se resuelven fuera de la Tolva
    /// (<see cref="OrderType.Conduce"/>/<see cref="OrderType.AguantaCalor"/>, cumplidos
    /// por <c>EnsayoMaestro</c>/<see cref="OrderSystem.CompletarEnsayo"/>, y
    /// <see cref="OrderType.FlotaInsoluble"/> por tabla en la Tolva): esa maquinaria ya
    /// existe y funciona, reinventarla aquí sería duplicar sin necesidad.
    /// </summary>
    public sealed class SemillaCero : MonoBehaviour
    {
        // =================================================================
        // LA MÁQUINA DE ESTADOS DE LOS BEATS (contrato §1).
        // =================================================================
        private enum Beat
        {
            /// <summary>Beat 1: esperando la primera hornada a fuego propio (sale la primera arena).</summary>
            Milagro,
            /// <summary>Beat 2: "Tráeme 25 de ese... 'X' tuyo" -- con el nombre REAL (Encargo Q; antes provisional).</summary>
            PrimeraPeticion,
            /// <summary>
            /// (Encargo Q, REESCRITO) Beat 3: el Maestro ENSEÑA el nombre real ("Eso es
            /// ARENA DE SÍLICE, aprendiz. Apúntalo.") -- ya no un rito de bautizo
            /// forzado; el pedido continúa AL INSTANTE con el nombre real (15 más). Ver
            /// <see cref="EntrarNombreSeGana"/>.
            /// </summary>
            NombreSeGana,
            /// <summary>Beat 4: "más tostado" -- la trampa de la banda de calcinación estrecha, ceniza, nota forense, reintento.</summary>
            FracasoTostado,
            /// <summary>Beat 5.1: "¿Puedes hacerlo MÁS DURO?" -- destapa la prensa.</summary>
            PreguntaPrensa,
            /// <summary>Beat 5.2: "¿Por qué esto queda ENCIMA?" -- destapa la columna.</summary>
            PreguntaColumna,
            /// <summary>Beat 5.3: "¿Esto CONDUCE?" -- destapa el banco de chispa.</summary>
            PreguntaChispa,
            /// <summary>
            /// (CONTRATO_TERMICA.md §3c, ENCARGO I, playtest 44) Beat 5.3½:
            /// "Todo lo tuestas. ¿Y si lo ENFRÍAS?" -- destapa la alcoba fría
            /// (SimLevelBuilder.SalaFria). Idea TEMPERATURA-FRÍA: completa la
            /// idea "temperatura" con su mitad fría (enmienda 4, "las 4+1").
            /// Nace ENTRE PreguntaChispa y PreguntaEnsayo por mandato textual
            /// del contrato -- el orden de las demás preguntas no cambia.
            /// </summary>
            PreguntaFrio,
            /// <summary>Beat 5.4: "¿DE VERDAD aguanta?" -- destapa el Ensayo, cierra el círculo del beat 4.</summary>
            PreguntaEnsayo,
            /// <summary>Beat 6: "No necesito nada más por hoy... Pero queda limo." Panel vacío. Solo queda contar la autonomía.</summary>
            FinalAbierto,
        }

        // Índices de sala de la API congelada de M (contrato §3): 0=prensa,
        // 1=columna, 2=chispa, 3=ensayo. Constantes locales solo por legibilidad --
        // SimLevelBuilder.DestaparSala los toma como `int` posicional, no un enum propio.
        private const int SalaPrensa = 0;
        private const int SalaColumna = 1;
        private const int SalaChispa = 2;
        private const int SalaEnsayo = 3;
        /// <summary>(CONTRATO_TERMICA.md §3c) Quinta sala, sumada por el ENCARGO I este round -- mismo valor que <see cref="SimLevelBuilder.SalaFria"/>.</summary>
        private const int SalaFria = 4;

        // -----------------------------------------------------------------
        // CANTIDADES Y RECOMPENSAS DEL ARCO. El contrato fija el número exacto SOLO
        // del beat 2 (25) y da el orden de magnitud del resto ("15-20 el resto");
        // los valores concretos de abajo son DECISIÓN de este encargo, documentada
        // en el informe de la ronda -- no hay balance de Favor/desenlace clásico
        // que proteger aquí (Semilla 0 no corre el ciclo de jornadas de 3 días).
        // -----------------------------------------------------------------
        private const int Beat2Cantidad = 25, Beat2Recompensa = 20;
        private const int Beat3Cantidad = 15, Beat3Recompensa = 25;
        private const int Beat4Cantidad = 15, Beat4Recompensa = 30;
        private const int Beat5CantidadPrensa = 10;
        private const int Beat5CantidadColumna = 10;
        private const int Beat5Recompensa = 20;
        private const int Beat5RecompensaEnsayo = 25;
        /// <summary>(CONTRATO_TERMICA.md §3c) Cantidad EXACTA fijada por el contrato ("cantidad 8"); recompensa por orden de magnitud con sus hermanas Prensa/Columna (mismo tipo de pedido: Guiado, se resuelve en la Tolva).</summary>
        private const int Beat5CantidadFrio = 8;
        private const int Beat5RecompensaFrio = 20;

        /// <summary>Cadencia de sondeo de TODA la máquina de estados (discovery/pedidos/autonomía) -- nunca por frame.</summary>
        private const float SondeoSeg = 0.4f;

        private AlkahestSim _sim;
        private SubstanceKnowledge _knowledge;
        private OrderSystem _orders;
        // (Encargo Q) `_namingUi`/`_forzarNamingUiPendiente` RETIRADOS: vivían aquí
        // solo para forzar la apertura de NamingUi en la transición beat 2→3 (ver el
        // docblock de la clase). El beat 3 reescrito ya no abre ningún panel -- el
        // Maestro enseña el nombre real con una línea hablada (MaestroDice), nada más.

        private Beat _beat = Beat.Milagro;
        /// <summary>La sustancia del arco entero: el Polvo (estado natal) de la base que ganó el solver de M, fijada en cuanto se descubre (beat 1 → 2).</summary>
        private byte _sustanciaPrincipal = MaterialId.Empty;
        private int _baseIdx;
        private byte _calcinadoPrincipal = MaterialId.Empty;
        private byte _compactoPrincipal = MaterialId.Empty;
        /// <summary>Edge-trigger: el comentario del Maestro sobre la ceniza solo se dice una vez por partida.</summary>
        private bool _cenizaComentada;

        // -----------------------------------------------------------------
        // (CONTRATO_TERMICA.md §3c, ENCARGO I) BEAT DEL FRÍO: rastreo del
        // hielo derretido -- ver el docblock de SondeoDerretidoHielo para el
        // porqué de la heurística (no hay SimEventType de fusión hoy).
        // -----------------------------------------------------------------
        /// <summary>Edge-trigger: la línea de "se te derritió" solo se dice una vez por partida, igual que la ceniza del beat 4.</summary>
        private bool _derretidoComentado;
        private int _hieloEnZonaFriaAntes;
        private int _progresoPedidoFrioAntes;

        private float _sondeoAcc;

        // -----------------------------------------------------------------
        // "EL MAESTRO HABLA": líneas sueltas que NO son un pedido (la demanda de
        // nombre, el comentario de la ceniza, la despedida del final abierto).
        // Un único mensaje activo a la vez -- construido UNA sola vez, en el
        // instante del cambio de beat, nunca en OnGUI (misma disciplina que
        // Game/SubstanceKnowledge.cs::EncolarLeyBanner).
        // -----------------------------------------------------------------
        private string _maestroTexto;
        private float _maestroHasta;

        // -----------------------------------------------------------------
        // CONTADOR DE AUTONOMÍA (contrato §1 beat 6, enmienda 5): hornadas,
        // bautizos, aspirados/vertidos POSTERIORES al final abierto -- la métrica
        // reina ("cuántos siguen jugando cuando ya nadie les pide nada"). Sin
        // poder tocar Crisol.cs/Flask.cs (propiedad de M), se aproxima con las
        // dos señales que SÍ están en mi propiedad: el total de manipulaciones
        // por sustancia de SubstanceKnowledge (aspirar/verter/hornada, ver su
        // docblock) y NamingVersion (bautizos/rebautizos) -- documentado como
        // decisión fuera de contrato explícita en el informe de la ronda.
        // -----------------------------------------------------------------
        private bool _autonomiaActiva;
        private int _autonomiaAcciones;
        private int _autonomiaAccionesEsteMinuto;
        private float _autonomiaMinutoAcc;
        private int _ultimaManipTotal;
        private int _ultimoNamingVersion;

        /// <summary>
        /// Acciones contadas desde el final abierto (beat 6). Público para que un F3
        /// futuro (Dev/DevPalette.cs, fuera de la propiedad de este encargo -- ver el
        /// informe de la ronda) pueda leerlo con <c>FindAnyObjectByType&lt;SemillaCero&gt;()</c>
        /// sin que este archivo tenga que saber nada de F3.
        /// </summary>
        public int AccionesPostFinalAbierto => _autonomiaAcciones;

        /// <summary>
        /// Inyección de dependencias desde AlkahestGameBootstrap (ver el docblock de la
        /// clase para el enganche exacto). GATE DURO (contrato §2): si la escena es la
        /// MULTI (<see cref="SimSync.EnEscena"/>), no se guarda ninguna referencia y esta
        /// instancia queda muda para siempre -- Semilla 0 es solo de un jugador.
        /// </summary>
        public void Init(AlkahestSim sim, SubstanceKnowledge knowledge, OrderSystem orderSystem)
        {
            if (SimSync.EnEscena) return;

            _sim = sim;
            _knowledge = knowledge;
            _orders = orderSystem;
        }

        private void Update()
        {
            if (_sim == null || _knowledge == null || _orders == null) return; // gate de escena, o Init aún no llamado.
            if (DayCycle.InputLocked) return;

            _sondeoAcc += Time.deltaTime;
            if (_sondeoAcc < SondeoSeg) return;
            _sondeoAcc -= SondeoSeg;

            SondeoAutonomia();

            switch (_beat)
            {
                case Beat.Milagro: SondeoMilagro(); break;
                case Beat.PrimeraPeticion: SondeoPrimeraPeticion(); break;
                case Beat.NombreSeGana: SondeoNombreSeGana(); break;
                case Beat.FracasoTostado: SondeoFracasoTostado(); break;
                case Beat.PreguntaPrensa: SondeoPreguntaPrensa(); break;
                case Beat.PreguntaColumna: SondeoPreguntaColumna(); break;
                case Beat.PreguntaChispa: SondeoPreguntaChispa(); break;
                case Beat.PreguntaFrio: SondeoPreguntaFrio(); break;
                case Beat.PreguntaEnsayo: SondeoPreguntaEnsayo(); break;
                case Beat.FinalAbierto: break; // nada más que hacer -- SondeoAutonomia ya corrió arriba.
            }
        }

        /// <summary>¿El único pedido activo (si lo hay) está completo? Semilla 0 nunca tiene más de uno a la vez (EncolarPedidoGuiado reemplaza la lista entera).</summary>
        private bool PedidoActivoCompletado()
        {
            var lista = _orders.ActiveOrders;
            return lista.Count > 0 && lista[0].Completado;
        }

        private void MaestroDice(string texto, float duracionSeg)
        {
            _maestroTexto = texto;
            _maestroHasta = Time.time + duracionSeg;
        }

        // =================================================================
        // BEAT 1 — EL MILAGRO.
        // =================================================================
        /// <summary>
        /// Busca la base×estado en Polvo (el estado NATAL, "sale la primera
        /// arena") que el jugador ya haya descubierto -- el banner "ALGO NUEVO" con el
        /// nombre provisional ya lo dispara <see cref="SubstanceKnowledge"/> sola (ver su
        /// enmienda 1); aquí solo se detecta la transición para arrancar el beat 2.
        /// </summary>
        private void SondeoMilagro()
        {
            byte polvo = EscanearBasePolvoDescubierta();
            if (polvo == MaterialId.Empty) return;

            _sustanciaPrincipal = polvo;
            _baseIdx = MaterialId.BaseDe(polvo);
            _beat = Beat.PrimeraPeticion;

            string nombre = _knowledge.NombreDe(_sustanciaPrincipal);
            string texto = "Tráeme " + Beat2Cantidad + " de ese... \"" + nombre + "\" tuyo.";
            _orders.EncolarPedidoGuiado(OrderType.Guiado, Beat2Cantidad, Beat2Recompensa, texto, targetMat: _sustanciaPrincipal);
            Debug.Log("[ChaosAlchemy][SemillaCero] beat 1→2: primera hornada lista (\"" + nombre + "\"). Petición a regañadientes: " + texto);
        }

        /// <summary>
        /// (ENCARGO V, CONTRATO_RONDA48.md §1a) FIJADA a
        /// <see cref="Universe.SemillaCeroBaseIdx"/> (arena) en vez de barrer
        /// "la primera base descubierta, sea cual sea" -- ANTES de la veta
        /// esto daba igual (la arena era, de hecho, lo primero que se podía
        /// sacar del limo), pero con la veta de turba tallable y aspirable
        /// DESDE EL ARRANQUE (Sim/SimLevelBuilder.cs::BuildVetaTurba), un
        /// jugador que aspire la veta ANTES de encender el crisol por
        /// primera vez habría descubierto la base 3 primero: el bucle
        /// genérico "b=0.. el primero descubierto" habría arrancado el arco
        /// entero con turba en vez de arena, descalibrando el override 2 de
        /// Universe (fusión/calcinación/techo de Ash, tallado a mano SOLO
        /// para <see cref="Universe.SemillaCeroBaseIdx"/>) y la propia
        /// designación de <see cref="Universe.SemillaCeroBaseTurbaIdx"/> como
        /// "el combustible que ya tienes cuando te piden tostar". Fijar el
        /// beat 1 a la base de diseño (no a "lo que sea que el jugador tocó
        /// primero") cierra ese hueco de raíz -- el aprendiz puede tallar y
        /// guardar turba desde el primer segundo sin romper el guion.
        /// </summary>
        private byte EscanearBasePolvoDescubierta()
        {
            byte polvo = MaterialId.MatDe(Universe.SemillaCeroBaseIdx, EstadoMateria.Polvo);
            return _knowledge.EsDescubierto(polvo) ? polvo : MaterialId.Empty;
        }

        // =================================================================
        // BEAT 2 — LA PRIMERA PETICIÓN.
        // =================================================================
        private void SondeoPrimeraPeticion()
        {
            if (!PedidoActivoCompletado()) return;
            EntrarNombreSeGana();
        }

        // =================================================================
        // BEAT 3 — EL NOMBRE SE GANA (Encargo Q, LA QUÍMICA CON NOMBRE REAL:
        // REESCRITO, docs/DISENO_QUIMICA_REAL.md §4: "el Maestro ya no exige
        // inventar un nombre -- te enseña el real"). ANTES (playtest 40): dos
        // fases -- exigir el rito de bautizo (forzando NamingUi) y ESPERAR a
        // que <c>EstaBautizado</c> se vuelva true antes de poder continuar el
        // pedido. AHORA: con <see cref="SubstanceKnowledge.NombreDe"/> ya
        // devolviendo el nombre REAL (<see cref="Alkahest.Sim.Universe.TieneIdentidadReal"/>)
        // y <c>NecesitaBautizo</c> siendo false para ese mismo matId en
        // Semilla Cero (ver el docblock de esa clase), no hay NADA que
        // esperar: el nombre YA está puesto, así que una sola fase basta --
        // el Maestro dice la línea y el pedido se encola en el mismo
        // instante, sin abrir ningún panel.
        // =================================================================
        private void EntrarNombreSeGana()
        {
            _beat = Beat.NombreSeGana;
            string nombre = _knowledge.NombreDe(_sustanciaPrincipal); // ya el nombre REAL (Encargo Q).
            MaestroDice("Eso es " + nombre.ToUpperInvariant() + ", aprendiz. Apúntalo.", 6f);

            string texto = "Ahora que sabes cómo se llama: tráeme " + Beat3Cantidad + " de tu \"" + nombre + "\".";
            _orders.EncolarPedidoGuiado(OrderType.Guiado, Beat3Cantidad, Beat3Recompensa, texto, targetMat: _sustanciaPrincipal);
            Debug.Log("[ChaosAlchemy][SemillaCero] beat 2→3: el Maestro enseña el nombre real (\"" + nombre + "\") -- el pedido continúa: " + texto);
        }

        private void SondeoNombreSeGana()
        {
            if (!PedidoActivoCompletado()) return;
            EntrarFracasoTostado();
        }

        // =================================================================
        // BEAT 4 — EL FRACASO FORENSE.
        // =================================================================
        /// <summary>
        /// (CONTRATO_RONDA48.md §1d, ENCARGO V) EL CONSEJO DE LA VETA. Aquí,
        /// y NUNCA antes: el contrato es explícito ("aparece con el beat 4,
        /// cuando el fuego propio deja de bastar"). Este es exactamente el
        /// instante en que el fuego propio (rescoldo, sin combustible) deja
        /// de bastar -- el Maestro acaba de pedir "más TOSTADO", algo que la
        /// banda de calcinación estrecha (Universe.AplicarOverridesSemillaCero,
        /// override 2) hace IMPOSIBLE a rescoldo. Texto EXACTO del contrato,
        /// sin parafrasear. Usa el canal ya existente de "EL MAESTRO HABLA"
        /// (<see cref="MaestroDice"/>) -- no HintSystem.cs, cuyo carrusel es
        /// un temporizador ciego al beat actual y no puede expresar "no
        /// antes de X" sin acoplarlo a esta máquina de estados (decisión
        /// documentada del encargo: el "canal del arco" que el contrato deja
        /// a elección de V).
        /// </summary>
        private void EntrarFracasoTostado()
        {
            _beat = Beat.FracasoTostado;
            _calcinadoPrincipal = MaterialId.MatDe(_baseIdx, EstadoMateria.Calcinado);
            _cenizaComentada = false;

            string nombre = _knowledge.NombreDe(_sustanciaPrincipal);
            // (ENCARGO V, contrato §1d) "La línea del beat 4 del Maestro puede
            // ganar UNA frase que apunte al brasero con lo tallado" -- literal:
            // se añade UNA cláusula al pedido ya existente, el resto de la
            // frase no cambia.
            string texto = "Más de eso, pero TOSTADO -- tráeme " + Beat4Cantidad + " de tu \"" + nombre + "\", bien calcinada. El brasero come lo que talles del muro.";
            _orders.EncolarPedidoGuiado(OrderType.Guiado, Beat4Cantidad, Beat4Recompensa, texto, targetMat: _calcinadoPrincipal);
            Debug.Log("[ChaosAlchemy][SemillaCero] beat 3→4: pide lo tostado -- la banda de calcinación es estrecha, el brasero recién alimentado se pasará de largo.");

            // (contrato §1d) El consejo de la veta, texto EXACTO, disparado
            // UNA vez al entrar en este beat -- ver el docblock del método.
            MaestroDice("Esa veta parda del muro es TURBA: tállala (C), el brasero la come.", 8f);
        }

        private void SondeoFracasoTostado()
        {
            // (enmienda 2) La nota forense en la ficha del diario YA la escribió
            // SubstanceKnowledge sola (ApplyWitness para Boil de la CA, y
            // RegistrarDestruccionPorHornada para la trampa REAL del beat 4, que es
            // una hornada Polvo→Ash de Crisol.DecidirHornada -- costura de
            // integración pt40) -- aquí solo falta la LÍNEA HABLADA del Maestro, una
            // vez, la primera vez que ocurre. Se vigilan LOS DOS materiales: la
            // trampa destruye el POLVO (la entrada de la hornada), no el calcinado;
            // y un calcinado re-tostado también puede morir.
            if (!_cenizaComentada && (_knowledge.FueDestruidoAAsh(_sustanciaPrincipal)
                                       || _knowledge.FueDestruidoAAsh(_calcinadoPrincipal)))
            {
                _cenizaComentada = true;
                MaestroDice("Eso es ceniza. Interesante... apunta a qué temperatura muere. Y guárdala: la ceniza también arde, mal, pero arde.", 8f);
                Debug.Log("[ChaosAlchemy][SemillaCero] beat 4: presenció la ceniza -- nota forense ya en el diario, el Maestro comenta.");
            }

            if (!PedidoActivoCompletado()) return;
            EntrarPreguntaPrensa();
        }

        // =================================================================
        // BEAT 5 — LAS PREGUNTAS (comprensión mayor). Eran cuatro; el
        // CONTRATO_TERMICA.md §3c (playtest 44, ENCARGO I) suma una quinta
        // ("¿Y si lo ENFRÍAS?") entre Chispa y Ensayo -- ver EntrarPreguntaFrio.
        // =================================================================
        private void EntrarPreguntaPrensa()
        {
            _beat = Beat.PreguntaPrensa;
            _compactoPrincipal = MaterialId.MatDe(_baseIdx, EstadoMateria.Compacto);
            SimLevelBuilder.DestaparSala(_sim, SalaPrensa); // la sala se destapa AL ACEPTARSE la pregunta (contrato §1 beat 5), no al completarla.
            _orders.EncolarPedidoGuiado(OrderType.Guiado, Beat5CantidadPrensa, Beat5Recompensa,
                "¿Puedes hacerlo MÁS DURO?", targetMat: _compactoPrincipal);
            Debug.Log("[ChaosAlchemy][SemillaCero] beat 4→5.1: se destapa la prensa -- idea ESTADO/proceso.");
        }

        private void SondeoPreguntaPrensa()
        {
            if (!PedidoActivoCompletado()) return;
            EntrarPreguntaColumna();
        }

        private void EntrarPreguntaColumna()
        {
            _beat = Beat.PreguntaColumna;
            SimLevelBuilder.DestaparSala(_sim, SalaColumna);
            // (idea DENSIDAD) reutiliza OrderType.FlotaInsoluble tal cual (contrato §6.1
            // de CONTRATO_PERSISTE.md, ya resuelto por tabla en la Tolva) -- criterio
            // genérico del universo, no atado a _sustanciaPrincipal: la pregunta la
            // responde CUALQUIER cosa que flote sin disolverse, que es justo lo que la
            // columna enseña a observar.
            _orders.EncolarPedidoGuiado(OrderType.FlotaInsoluble, Beat5CantidadColumna, Beat5Recompensa,
                "¿Por qué esto queda ENCIMA?");
            Debug.Log("[ChaosAlchemy][SemillaCero] beat 5.1→5.2: se destapa la columna -- idea DENSIDAD.");
        }

        private void SondeoPreguntaColumna()
        {
            if (!PedidoActivoCompletado()) return;
            EntrarPreguntaChispa();
        }

        private void EntrarPreguntaChispa()
        {
            _beat = Beat.PreguntaChispa;
            SimLevelBuilder.DestaparSala(_sim, SalaChispa);
            // (idea CONDUCTIVIDAD) resuelto en el banco de chispa vía EnsayoMaestro ->
            // OrderSystem.CompletarEnsayo(OrderType.Conduce, ...) -- nunca en la Tolva
            // (MatchesOrder devuelve false a propósito para este tipo).
            _orders.EncolarPedidoGuiado(OrderType.Conduce, 1, Beat5RecompensaEnsayo, "¿Esto CONDUCE?");
            Debug.Log("[ChaosAlchemy][SemillaCero] beat 5.2→5.3: se destapa el banco de chispa -- idea CONDUCTIVIDAD.");
        }

        private void SondeoPreguntaChispa()
        {
            // (integración pt40) "¿Esto CONDUCE?" se responde EN EL BANCO, no en el
            // Ensayo: el pedido es OrderType.Conduce y su único completador clásico
            // (EnsayoMaestro) vive en la sala 3, tapiada hasta el beat siguiente --
            // interbloqueo duro sin esta vía. En cuanto la lámpara dicta sentencia
            // positiva (testigo MaxConductividadObservada, alimentado por
            // BancoChispa tras cada análisis), el director completa el pedido él
            // mismo. En Semilla 0 el banco solo existe desde este beat, así que
            // cualquier valor >0 se ganó respondiendo ESTA pregunta. Verificado en
            // la seed congelada: el calcinado del beat 4 conduce pleno (nivel 2) --
            // el jugador ya carga la respuesta encima cuando la pregunta llega.
            if (!PedidoActivoCompletado() && _knowledge.MaxConductividadObservada >= 1)
                _orders.CompletarEnsayo(OrderType.Conduce,
                    _knowledge.MaxConductividadObservada == 2 ? 2f : 1f);

            if (!PedidoActivoCompletado()) return;
            EntrarPreguntaFrio();
        }

        // =================================================================
        // (CONTRATO_TERMICA.md §3c, ENCARGO I, playtest 44) BEAT 5.3½ — LA
        // PREGUNTA DEL FRÍO. Entre PreguntaChispa y PreguntaEnsayo por
        // mandato textual del contrato: "esto COMPLETA la idea 'temperatura'
        // con su mitad fría" (enmienda 4, las 4+1).
        // =================================================================
        private void EntrarPreguntaFrio()
        {
            _beat = Beat.PreguntaFrio;
            SimLevelBuilder.DestaparSala(_sim, SalaFria);

            // (decisión de I) El sitio elegido para la alcoba fría
            // (Sim/SimLevelBuilder.cs, constantes AlcobaFria*) es una QUINTA
            // sala propia, tallada en frío desde el arranque (como las otras
            // cuatro) y tapiada hasta este momento -- nunca reutiliza una
            // sala YA abierta, así que el caso "obra tallada en caliente +
            // aviso" que contempla el contrato (§3c, "si su sitio queda en
            // sala ya abierta") NO aplica aquí: no hace falta ese camino.
            SondeoDerretidoHielo(); // arranca el rastreo desde cero -- ver su docblock.
            _derretidoComentado = false;

            MaestroDice("Todo lo tuestas. ¿Y si lo ENFRÍAS?", 6f);
            _orders.EncolarPedidoGuiado(OrderType.Guiado, Beat5CantidadFrio, Beat5RecompensaFrio,
                "Tráeme HIELO — y apúrate, que el frío no espera a nadie.", targetMat: MaterialId.Ice);
            Debug.Log("[ChaosAlchemy][SemillaCero] beat 5.3→5.3½: se destapa la alcoba fría -- idea TEMPERATURA-FRÍA (completa la mitad caliente del beat 4).");
        }

        private void SondeoPreguntaFrio()
        {
            SondeoDerretidoHielo();

            if (!PedidoActivoCompletado()) return;
            EntrarPreguntaEnsayo();
        }

        /// <summary>
        /// APROXIMACIÓN DOCUMENTADA (decisión de I, fuera de contrato
        /// estricto): no existe hoy un <see cref="Alkahest.Sim.SimEventType"/>
        /// para la fusión (<c>Sim/SimStepper.cs::ApplyPhase</c>, rama
        /// <c>meltsAt</c>, transforma la celda con <c>Transform(idx,
        /// def.meltsInto)</c> SIN pasar por <c>PushEvent</c> -- a diferencia
        /// de Freeze/Boil, que sí lo hacen; ver ese archivo, del ENCARGO T
        /// este round y fuera de mi alcance) así que
        /// <see cref="SubstanceKnowledge"/> no tiene forma de "presenciar" un
        /// derretido como sí presencia la ceniza
        /// (<see cref="SubstanceKnowledge.FueDestruidoAAsh"/>). Se aproxima
        /// contando el hielo (<see cref="MaterialId.Ice"/>) presente en la
        /// alcoba fría (<see cref="SimLevelBuilder.AlcobaFriaX0"/>/X1, zona
        /// pequeña y acotada -- nunca un barrido del mapa entero) cada
        /// sondeo (0.4s, jamás por frame): si la cantidad BAJA sin que el
        /// pedido activo haya recibido progreso nuevo en la misma ventana,
        /// la explicación con diferencia más probable es que se derritió
        /// solo (la única otra forma de que baje es que el propio jugador lo
        /// aspire -- y aspirarlo SÍ mueve el progreso del pedido). Falso
        /// positivo posible pero improbable (p.ej. tallar el hielo con el
        /// Cincel y tirarlo sin entregarlo) -- aceptable para una línea de
        /// sabor en un beat scriptado. DEUDA para Fable: mover esto a un
        /// <c>SimEventType.Melt</c> real + <c>WitnessFlags</c> propio en una
        /// ronda con <c>Sim/SimStepper.cs</c>/<c>Game/SubstanceKnowledge.cs</c>
        /// en alcance de este encargo.
        /// </summary>
        private void SondeoDerretidoHielo()
        {
            int hieloAhora = ContarHieloEnZonaFria();
            int progresoAhora = _orders.ActiveOrders.Count > 0 ? _orders.ActiveOrders[0].Progreso : 0;

            if (!_derretidoComentado && _hieloEnZonaFriaAntes > 0 &&
                hieloAhora < _hieloEnZonaFriaAntes && progresoAhora == _progresoPedidoFrioAntes)
            {
                _derretidoComentado = true;
                // (contrato §3c, textual) "el Maestro NO se burla dos veces igual: una línea... edge-trigger como la ceniza del beat 4".
                MaestroDice("...se te derritió. El frío es paciencia Y PRISA.", 7f);
                Debug.Log("[ChaosAlchemy][SemillaCero] beat frío: el hielo se derritió antes de llegar a la Tolva -- el Maestro comenta, una sola vez.");
            }

            _hieloEnZonaFriaAntes = hieloAhora;
            _progresoPedidoFrioAntes = progresoAhora;
        }

        /// <summary>Barrido ACOTADO (nunca el mapa entero) del interior de la alcoba fría -- ver el docblock de <see cref="SondeoDerretidoHielo"/>. ~6x8 celdas, sondeado a 2.5Hz: coste despreciable frente al resto de esta clase.</summary>
        private int ContarHieloEnZonaFria()
        {
            if (_sim == null || _sim.Grid == null) return 0;

            int baseY = SimLevelBuilder.BaseYDeEstacion(SimLevelBuilder.AlcobaFriaX0);
            int x0 = SimLevelBuilder.AlcobaFriaX0 + 1;
            int x1 = SimLevelBuilder.AlcobaFriaX1 - 1;
            int y0 = baseY + 1;
            int y1 = baseY + SimLevelBuilder.AlcobaFriaMuroAlto;

            var grid = _sim.Grid;
            int n = 0;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (CellGrid.InBounds(x, y) && grid.GetMat(x, y) == MaterialId.Ice) n++;
                }
            }
            return n;
        }

        private void EntrarPreguntaEnsayo()
        {
            _beat = Beat.PreguntaEnsayo;
            SimLevelBuilder.DestaparSala(_sim, SalaEnsayo);
            // (idea TEMPERATURA, cierra el círculo del beat 4) resuelto en el Ensayo vía
            // OrderSystem.CompletarEnsayo(OrderType.AguantaCalor, ...).
            _orders.EncolarPedidoGuiado(OrderType.AguantaCalor, 1, Beat5RecompensaEnsayo, "¿DE VERDAD aguanta?");
            Debug.Log("[ChaosAlchemy][SemillaCero] beat 5.3→5.4: se destapa el Ensayo -- idea TEMPERATURA, cierra el fracaso del beat 4.");
        }

        private void SondeoPreguntaEnsayo()
        {
            if (!PedidoActivoCompletado()) return;
            EntrarFinalAbierto();
        }

        // =================================================================
        // BEAT 6 — EL FINAL ABIERTO.
        // =================================================================
        private void EntrarFinalAbierto()
        {
            _beat = Beat.FinalAbierto;
            _orders.ActiveOrders.Clear(); // contrato: "sin encargo nuevo, panel de encargos vacío".
            MaestroDice("No necesito nada más por hoy. ...Pero queda limo.", 9f);
            Debug.Log("[ChaosAlchemy][SemillaCero] beat 5.4→6: FINAL ABIERTO. El alambique sigue goteando; nadie lo pidió. Arranca el contador de autonomía.");

            _ultimaManipTotal = SumaManipulaciones();
            _ultimoNamingVersion = _knowledge.NamingVersion;
            _autonomiaAcciones = 0;
            _autonomiaAccionesEsteMinuto = 0;
            _autonomiaMinutoAcc = 0f;
            _autonomiaActiva = true;
        }

        private int SumaManipulaciones()
        {
            int total = 0;
            for (int m = 1; m < MaterialId.Count; m++) total += _knowledge.ManipulacionesDe((byte)m);
            return total;
        }

        /// <summary>
        /// Log a consola por cada acción detectada + un resumen cada 60s (contrato §1
        /// beat 6: "se loguea a consola cada acción y un resumen por minuto"). Solo corre
        /// tras <see cref="EntrarFinalAbierto"/> (guard <see cref="_autonomiaActiva"/>),
        /// ya sondeado con acumulador desde <see cref="Update"/> (cada
        /// <see cref="SondeoSeg"/>, nunca por frame).
        /// </summary>
        private void SondeoAutonomia()
        {
            if (!_autonomiaActiva) return;

            int manipActual = SumaManipulaciones();
            int deltaManip = manipActual - _ultimaManipTotal;
            if (deltaManip > 0)
            {
                _ultimaManipTotal = manipActual;
                _autonomiaAcciones += deltaManip;
                _autonomiaAccionesEsteMinuto += deltaManip;
                Debug.Log("[ChaosAlchemy][SemillaCero] autonomía: +" + deltaManip + " manipulación(es) tras el final abierto (total " + _autonomiaAcciones + ").");
            }

            int naming = _knowledge.NamingVersion;
            if (naming != _ultimoNamingVersion)
            {
                int deltaNaming = naming - _ultimoNamingVersion;
                _ultimoNamingVersion = naming;
                _autonomiaAcciones += deltaNaming;
                _autonomiaAccionesEsteMinuto += deltaNaming;
                Debug.Log("[ChaosAlchemy][SemillaCero] autonomía: bautizaste/renombraste algo tras el final abierto (total " + _autonomiaAcciones + ").");
            }

            _autonomiaMinutoAcc += SondeoSeg;
            if (_autonomiaMinutoAcc >= 60f)
            {
                _autonomiaMinutoAcc -= 60f;
                Debug.Log("[ChaosAlchemy][SemillaCero] autonomía: resumen del minuto -- " + _autonomiaAccionesEsteMinuto + " acción(es) (total " + _autonomiaAcciones + ").");
                _autonomiaAccionesEsteMinuto = 0;
            }
        }

        // =================================================================
        // "EL MAESTRO HABLA" -- panel propio, centro-bajo (deja libre el 0.30f que
        // usa Game/SubstanceKnowledge.cs para "LEY DESCUBIERTA"/"ALGO NUEVO", y las
        // esquinas donde viven encargos/frasco/pistas).
        // =================================================================
        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return;
            if (_maestroTexto == null || Time.time >= _maestroHasta) return;
            if (UiStyles.EscribiendoTexto || JournalHud.Abierto) return; // no competir con el rito de bautizo ni con el libro.

            UiStyles.Preparar();

            float pad = UiStyles.S(14f);
            float acento = UiStyles.S(4f);
            float ancho = Mathf.Clamp(Screen.width - UiStyles.S(160f), UiStyles.S(360f), UiStyles.S(640f));
            float interior = ancho - pad * 2f - acento;

            float altoTitulo = UiStyles.Titulo.lineHeight;
            float altoCuerpo = UiStyles.Alto(UiStyles.CuerpoCentrado, _maestroTexto, interior);
            float alto = pad + altoTitulo + UiStyles.S(4f) + altoCuerpo + pad;

            var panel = new Rect((Screen.width - ancho) * 0.5f, Screen.height * 0.62f, ancho, alto);
            UiStyles.Panel(panel, UiStyles.TintaFuerte, UiStyles.Oro);
            UiStyles.Rellenar(new Rect(panel.x, panel.y, acento, panel.height), UiStyles.Oro);

            GUI.Label(new Rect(panel.x + acento + pad, panel.y + pad, interior, altoTitulo), "EL MAESTRO", UiStyles.Titulo);
            GUI.Label(new Rect(panel.x + acento + pad, panel.yMax - pad - altoCuerpo, interior, altoCuerpo), _maestroTexto, UiStyles.CuerpoCentrado);
        }
    }
}
