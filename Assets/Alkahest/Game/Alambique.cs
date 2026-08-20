using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Net;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// EL ALAMBIQUE — archivo NUEVO (LA ALQUIMIA VISIBLE, encargo de Cesar:
    /// "que el vapor se pueda ATRAPAR" + "que los materiales sirvan para
    /// FABRICAR nuevos instrumentos" + "que el embellecimiento sea fruto del
    /// progreso").
    ///
    /// =====================================================================
    /// QUÉ ES
    /// =====================================================================
    /// Una campana de piedra con la bóveda interior FRÍA a propósito y un
    /// matraz de recogida justo debajo. El vapor real que sube (del Crisol,
    /// hirviendo limo o soluciones, ver Game/Crisol.cs) entra por el hueco
    /// abierto de abajo, se enfría al tocar la bóveda y CONDENSA solo (el
    /// motor ya sabe hacerlo: <see cref="MaterialDef.condensesAt"/>/
    /// <see cref="MaterialDef.condensesInto"/> de Steam -&gt; Water, ver
    /// Sim/SimStepper.ApplyPhase/ProcessGas) — el agua cae por su propio peso
    /// y se acumula en el matraz. No hay ningún E que pulsar para que
    /// "funcione": una vez construido, destila solo, siempre, como cualquier
    /// pieza de infraestructura pasiva del taller.
    ///
    /// =====================================================================
    /// FRUTO DEL PROGRESO: NACE COMO OBRA PENDIENTE
    /// =====================================================================
    /// Es el PRIMER instrumento del taller que el jugador FABRICA con
    /// materiales, en vez de encontrarlo ya construido (mandato de Cesar:
    /// "que el embellecimiento sea fruto del progreso"). Por eso su ciclo de
    /// vida tiene DOS fases:
    ///
    ///  1) <see cref="Fase.ObraPendiente"/>: nace como una SILUETA FANTASMA
    ///     (rectángulo translúcido, ver <see cref="BuildVisualGhost"/>) sobre
    ///     un pequeño PLINTO de piedra YA TALLADO desde el génesis del mundo
    ///     (<see cref="TallarEnPlano"/>, llamado por
    ///     Sim/SimLevelBuilder.BuildCuartoIntimo) — el sitio existe, la
    ///     máquina no. El rótulo dice "construible: N celdas de cerámico" y
    ///     el jugador VIERTE cerámico (con el frasco, cualquiera de las 5
    ///     variantes de esta seed — es una PROPIEDAD del estado, no de una
    ///     base concreta) sobre el plinto: <see cref="SondearZonaDeObra"/>
    ///     lo consume celda a celda y lo acumula. Al llegar al umbral, E
    ///     construye de verdad.
    ///  2) <see cref="Fase.Construido"/>: <see cref="CompletarConstruccion"/>
    ///     talla la mampostería REAL en caliente (<see cref="TallarEnCaliente"/>,
    ///     regla 29 de CLAUDE.md: esto CREA materia, así que usa PaintStable,
    ///     nunca Paint), sustituye el fantasma por los sprites completos y
    ///     desde ese momento en adelante se comporta como cualquier otra
    ///     estación del taller (IMovible, registro anticincel).
    ///
    /// EL CERÁMICO ES DELIBERADAMENTE "el material más difícil" (encargo):
    /// exige Polvo -&gt; Prensa (Compactar) -&gt; Compacto -&gt; Crisol (hornada de
    /// fuego PLENO, "ceramizando") -&gt; Cerámico — la cadena entera del taller.
    /// Que la RECOMPENSA de completarla sea un instrumento nuevo, no solo un
    /// número en el diario, es literalmente la frase de Cesar convertida en
    /// mecánica.
    ///
    /// <see cref="CeramicoRequerido"/> = 30 (el número que pide el encargo,
    /// SIN rebajar a 20): una sola hornada bien alimentada del Crisol satura
    /// su cámara entera (117 celdas) de Compacto de una vez, así que 30 no es
    /// un grind adicional — es "una hornada de sobra" una vez el jugador ya
    /// sabe ceramizar. Si el playtest real dice lo contrario, este es el
    /// único número que hay que tocar.
    ///
    /// =====================================================================
    /// GEOMETRÍA Y EMPLAZAMIENTO
    /// =====================================================================
    /// DECISIÓN (el encargo ofrecía dos sitios válidos: "aire arriba del
    /// crisol" o "zona propia"; se elige el primero): el alambique se ancla
    /// en la MISMA X que el Crisol (<see cref="SimLevelBuilder.AlambiqueX"/>
    /// = <see cref="SimLevelBuilder.CrisolX"/>), bien por encima de la boca
    /// embudada de su chimenea (que abre 182..206 en su fila más alta, ver
    /// Game/Crisol.cs) y del alcance visual de su humo (~y=213). El vapor que
    /// Game/Crisol.cs emite sobre la cubeta (ver
    /// Crisol.EmitirVaporCubeta) sube por ese mismo hueco de aire YA
    /// EXCAVADO por el cuarto (Sim/SimLevelBuilder.BuildCuartoIntimo excava
    /// TODO el interior del cuarto a Empty antes de que cada estación talle
    /// su propia mampostería, así que la columna entera CrisolX..y=240 es
    /// aire libre salvo lo que las estaciones añadan) sin depender de que el
    /// gas deambule en horizontal (ProcessGas sube 1 celda/tick si el hueco
    /// de encima está vacío — determinista, sin necesitar suerte).
    ///
    /// La CAMPANA (bóveda) y el MATRAZ comparten el mismo ancho por
    /// simplicidad de mampostería (dos rects apilados, el de abajo con SUELO
    /// y sin techo, el de arriba con TECHO y sin suelo, fila a fila
    /// contiguos): la lectura de "domo ancho sobre cuello estrecho" la da el
    /// SPRITE (campana redondeada arriba, vidrio de matraz abajo), no la
    /// física — mismo criterio que separa forma visual de hueco real en
    /// Crisol (VueloCuerpo).
    /// </summary>
    public sealed class Alambique : MonoBehaviour, IMaquinaInteractiva, IMovibleAnclaEsquina, IMaquinaUsableRemota
    {
        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        private const float ProximityRange = 4.0f;

        // -----------------------------------------------------------------
        // GEOMETRÍA. Públicas: Sim/SimLevelBuilder.cs las lee para tallar el
        // plinto y registrar su obra anticincel (mismo patrón que
        // Crisol.CamaraAncho, etc.).
        // -----------------------------------------------------------------
        public const int MuroGrosor = 2;
        /// <summary>Ancho interior de la campana Y del matraz (comparten ancho, ver docblock de la clase).</summary>
        public const int DomoAncho = 9;
        public const int MatrazAncho = DomoAncho;
        public const int DomoAlto = 9;
        /// <summary>9x5 = 45 celdas de capacidad -- el mismo número que una RACIÓN de caño (Dispenser), para que "un matraz lleno" se lea con la misma vara que el resto del taller.</summary>
        public const int MatrazAlto = 5;

        /// <summary>Celdas de cerámico (cualquiera de las 5 variantes de la seed) que hacen falta para construir. Ver el docblock de la clase para el porqué de 30 y no 20.</summary>
        public const int CeramicoRequerido = 30;

        /// <summary>
        /// DECISIÓN NO PEDIDA EXPLÍCITAMENTE POR EL ENCARGO, necesaria para
        /// que la física cierre: el piso del matraz TIENE que ser sólido de
        /// punta a punta durante <see cref="Fase.ObraPendiente"/> (si no,
        /// el cerámico vertido se cuela y cae al Crisol de abajo -- ver
        /// <see cref="SondearZonaDeObra"/>). Pero un piso sólido de punta a
        /// punta PARA SIEMPRE sería una válvula de un solo sentido rota:
        /// el vapor que sube del Crisol (misma X, ver
        /// <see cref="SimLevelBuilder.AlambiqueX"/>) jamás podría entrar.
        /// Por eso, SOLO al completar la construcción
        /// (<see cref="CompletarConstruccion"/>), se abre un VENT -- una
        /// franja central del piso, angosta a propósito (5 de 9 celdas,
        /// deja 2 columnas sólidas por lado como "orilla" donde el agua
        /// condensada se posa) -- por la que el vapor real sigue subiendo
        /// (`ProcessGas` intenta diagonal/lateral cuando lo bloquea el
        /// resto del piso, así que encuentra el hueco) y por la que una
        /// fracción del agua ya condensada puede recolarse de vuelta (el
        /// mismo motor no distingue "sube" de "baja": es la física real de
        /// un grid de celdas, no una fuga de diseño). Aceptado como
        /// comportamiento: un alambique real tampoco es 100% eficiente.
        /// </summary>
        public const int VentAncho = 5;

        /// <summary>Temperatura raw a la que el domo empuja su interior -- literal del encargo ("empuje térmico de sondeo hacia raw ~45"). raw45 = -30°C, muy por debajo de Steam.condensesAt en CUALQUIER seed (raw 80..99, ver Universe.Create/waterBoilC): la condensación queda garantizada, no es un "a veces".</summary>
        private const int FrioTargetRaw = 45;
        /// <summary>Mismo paso por tick que HeatPlate.TempStepPerTick -- "patrón HeatPlate invertido" del encargo, literalmente el mismo número con el signo dado la vuelta por el propio clamp de <see cref="ApplyFrioTick"/>.</summary>
        private const int FrioStepPerTick = 5;

        private const float SondeoObraSeg = 0.25f; // mismo orden de magnitud que AffordanceGlow.ProbeIntervalSeconds: sondeo barato, nunca por frame.

        private enum Fase { ObraPendiente, Construido }

        private AlkahestSim _sim;
        private Transform _player;

        private int _anchorX, _baseY;
        private int _matX0, _matX1, _matY0, _matY1;
        private int _domX0, _domX1, _domY0, _domY1;
        private int _outX0, _outX1, _outY0, _outY1;
        private Vector3 _centro, _centroDomo, _centroMatraz;

        private Fase _fase = Fase.ObraPendiente;
        private int _ceramicoAcumulado;
        private float _sondeoAcc;
        private float _accumulator;

        /// <summary>Handle en SimLevelBuilder.ObraDelTaller -- SOLO se pide una vez construido (antes no hay mampostería real que proteger más allá del plinto, que el propio plano ya registra en BuildCuartoIntimo). Ver Game/Crisol.cs para el mismo patrón.</summary>
        private int _handleObra = -1;

        // ---- Visual ----
        private GameObject _ghostGo;
        private SpriteRenderer _ghostSprite;
        private SpriteRenderer _resalte;
        private float _alfaResalte;
        private int _aguaAcumuladaVista; // último conteo de celdas de Water en el matraz, refrescado por sondeo (rótulo).

        private const float RangoNombrePleno = 3.4f;
        private const float RangoNombreDesvanece = 4.6f;
        private bool _yaConocida;
        private const string ChapaNombre = "el alambique";
        private const string Verbo = "atrapa el vapor · destila";

        public Vector3 PuntoFoco => _centroMatraz;
        public float RangoFoco => ProximityRange;

        // ---- IMovible (solo tiene efecto real una vez Construido: Mudanza no lo registra antes, ver CompletarConstruccion) ----
        public Vector3 CentroMundo => _centro;
        public Vector2 TamanoMundo => new Vector2(
            (_outX1 - _outX0 + 1) * SimRenderer.CellWorldSize,
            (_outY1 - _outY0 + 1) * SimRenderer.CellWorldSize);
        public Vector2Int AnclaCelda => new Vector2Int(_outX0, _baseY);

        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            int span = _outX1 - _outX0 + 1;
            int alto = _outY1 - _outY0 + 1;
            return anclaCelda.x >= 1 && anclaCelda.x + span - 1 <= CellGrid.W - 2
                && anclaCelda.y >= 1 && anclaCelda.y + alto - 1 <= CellGrid.H - 2;
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap. `anchorX` = SimLevelBuilder.AlambiqueX.</summary>
        public void Init(AlkahestSim sim, Transform player, int anchorX)
        {
            _sim = sim;
            _player = player;
            _anchorX = anchorX;
            _baseY = SimLevelBuilder.AlambiqueBaseY;

            RecalcularRegiones();
            BuildVisualGhost();
            MachineFocus.Registrar(this); // el prompt "E -- construir" mientras es obra pendiente; se olvida en CompletarConstruccion (ya no hay nada que pulsar).

            // (fix Cesar playtest 33: "el alambique no se puede mover") RAÍZ
            // DEL BUG: antes de este fix, `Mudanza.RegistrarMovible` SOLO se
            // llamaba en CompletarConstruccion -- mientras el alambique era
            // obra pendiente (el estado en el que Cesar lo probó, plinto
            // recién tallado, cerámico sin pagar todavía) Mudanza ni siquiera
            // SABÍA que este objeto existía: V no encontraba nada que
            // agarrar ahí, sin ningún aviso que lo explicara. El fix lo
            // registra AQUÍ, en Init, así que es movible en las DOS fases
            // (ver Reposicionar, que ahora bifurca por `_fase`).
            //
            // BUG SECUNDARIO (mismo síntoma que Game/ColumnaEnsayo.cs corrigió
            // en el playtest 32): Sim/SimLevelBuilder.BuildCuartoIntimo YA
            // registra el rect del plinto en ObraDelTaller (necesita
            // protegerlo del cincel desde el génesis, antes de que exista
            // ninguna instancia) pero descartaba el handle devuelto. Antes de
            // este fix, CompletarConstruccion llamaba a un SEGUNDO
            // `RegistrarObra` con el rect completo (matraz+domo) -- eso deja
            // el handle del plinto HUÉRFANO en ObraDelTaller para siempre (un
            // rect fantasma, nunca actualizado, nunca limpiado) mientras el
            // aparato real queda protegido por un handle distinto. El fix
            // aquí RECLAMA el mismo handle del plinto (mismo patrón que
            // ColumnaEnsayo.Init con HallarObraExacta) y CompletarConstruccion
            // ahora lo hace CRECER con ActualizarObra en vez de crear uno
            // nuevo -- un único handle vivo durante toda la vida del aparato.
            Alambique.PlintoRect(_anchorX, _baseY, out int plintoX0, out int plintoY0, out int plintoX1, out int plintoY1);
            int handleExistente = SimLevelBuilder.HallarObraExacta(plintoX0, plintoY0, plintoX1, plintoY1);
            _handleObra = handleExistente >= 0 ? handleExistente : SimLevelBuilder.RegistrarObra(plintoX0, plintoY0, plintoX1, plintoY1);
            Mudanza.RegistrarMovible(this);

            // =================================================================
            // (playtest 40, SEMILLA CERO, CONTRATO_SEMILLA.md §3 "El vasito")
            // =================================================================
            // El alambique normal exige pagar 30 celdas de cerámico -- un
            // material que el arco de Semilla Cero no enseña a fabricar hasta
            // bien entrado el beat 5 (la Prensa, que lo compacta, sigue
            // tapiada hasta la primera pregunta del jugador). El diseño pide
            // que gotee "desde el minuto 0, sin intervención" (el anzuelo
            // silencioso del final abierto, DISENO_SEMILLA_CERO.md enmienda
            // 5: "desde el beat 2 el alambique ha estado goteando... sin que
            // NADIE lo mencione jamás") -- así que en este modo se construye
            // solo, de una vez, sin pasar por la obra pendiente ni por el
            // rótulo "construible: N celdas de cerámico". El resto del
            // aparato (destilado, vasito, rótulo) sigue siendo EXACTAMENTE el
            // mismo código que en modo caótico: solo se salta el peaje de
            // construcción.
            if (AlkahestGameBootstrap.ModoSemillaCero)
            {
                _ceramicoAcumulado = CeramicoRequerido;
                CompletarConstruccion();
            }
        }

        private void OnDestroy()
        {
            MachineFocus.Olvidar(this);
            Mudanza.OlvidarMovible(this);
        }

        // =================================================================
        // HUELLA — misma aritmética para la instancia y para el tallado
        // estático (patrón exacto de Game/Crisol.cs).
        // =================================================================
        private struct Huella
        {
            public int MatX0, MatX1, MatY0, MatY1;
            public int DomX0, DomX1, DomY0, DomY1;
            public int OutX0, OutX1, OutY0, OutY1;
        }

        private static Huella Calcular(int anchorX, int baseY)
        {
            Huella h;
            h.MatX0 = anchorX - MatrazAncho / 2;
            h.MatX1 = h.MatX0 + MatrazAncho - 1;
            h.MatY0 = baseY + 1;
            h.MatY1 = h.MatY0 + MatrazAlto - 1;

            h.DomX0 = anchorX - DomoAncho / 2;
            h.DomX1 = h.DomX0 + DomoAncho - 1;
            h.DomY0 = h.MatY1 + 1; // contiguo: nada de muro entre matraz y domo, es UN solo hueco vertical (ver docblock).
            h.DomY1 = h.DomY0 + DomoAlto - 1;

            h.OutX0 = Mathf.Min(h.MatX0, h.DomX0) - MuroGrosor;
            h.OutX1 = Mathf.Max(h.MatX1, h.DomX1) + MuroGrosor;
            h.OutY0 = baseY;
            h.OutY1 = h.DomY1 + 1; // incluye el techo del domo.
            return h;
        }

        private void RecalcularRegiones()
        {
            var h = Calcular(_anchorX, _baseY);
            _matX0 = h.MatX0; _matX1 = h.MatX1; _matY0 = h.MatY0; _matY1 = h.MatY1;
            _domX0 = h.DomX0; _domX1 = h.DomX1; _domY0 = h.DomY0; _domY1 = h.DomY1;
            _outX0 = h.OutX0; _outX1 = h.OutX1; _outY0 = h.OutY0; _outY1 = h.OutY1;

            float c = SimRenderer.CellWorldSize;
            _centro = new Vector3((_outX0 + (_outX1 - _outX0 + 1) * 0.5f) * c, (_outY0 + (_outY1 - _outY0 + 1) * 0.5f) * c, 0f);
            transform.position = _centro;
            _centroMatraz = new Vector3((_matX0 + MatrazAncho * 0.5f) * c, (_matY0 + MatrazAlto * 0.5f) * c, 0f);
            _centroDomo = new Vector3((_domX0 + DomoAncho * 0.5f) * c, (_domY0 + DomoAlto * 0.5f) * c, 0f);
        }

        /// <summary>
        /// (nivel/génesis) Talla SOLO el PLINTO -- la losa de piedra donde el
        /// jugador vierte el cerámico mientras el alambique es obra
        /// pendiente. A propósito NO es la mampostería completa (a
        /// diferencia de Crisol/Prensa/etc., que nacen construidos del
        /// todo): este instrumento nace a medias porque es "fruto del
        /// progreso", ver el docblock de la clase. El resto lo talla
        /// <see cref="TallarEnCaliente"/> cuando el jugador paga el
        /// cerámico.
        /// </summary>
        public static void TallarEnPlano(CellGrid grid, int anchorX, int baseY)
        {
            var h = Calcular(anchorX, baseY);
            for (int x = h.MatX0 - MuroGrosor; x <= h.MatX1 + MuroGrosor; x++)
                if (CellGrid.InBounds(x, baseY)) grid.SetCell(x, baseY, MaterialId.Stone);
        }

        /// <summary>Rect exterior del plinto (X0,Y0,X1,Y1), para que Sim/SimLevelBuilder.cs pueda registrarlo en ObraDelTaller sin duplicar la aritmética a mano.</summary>
        public static void PlintoRect(int anchorX, int baseY, out int x0, out int y0, out int x1, out int y1)
        {
            var h = Calcular(anchorX, baseY);
            x0 = h.MatX0 - MuroGrosor; y0 = baseY; x1 = h.MatX1 + MuroGrosor; y1 = baseY;
        }

        /// <summary>Talla el matraz Y el domo EN CALIENTE (regla 29: PaintStable, esto CREA piedra que no existía). Llamado UNA vez, desde <see cref="CompletarConstruccion"/>, y de nuevo desde <see cref="Reposicionar"/> tras borrar la huella vieja.</summary>
        private void TallarEnCaliente()
        {
            TallarRecintoCaliente(_matX0, _matX1, _matY0, _matY1, suelo: true, techo: false);
            TallarRecintoCaliente(_domX0, _domX1, _domY0, _domY1, suelo: false, techo: true);
            AbrirVentDelMatraz(); // SOLO aquí, y SOLO en caliente -- ver el docblock de VentAncho: el piso de la fase ObraPendiente (TallarEnPlano) se queda cerrado del todo a propósito.
        }

        /// <summary>Abre la franja central del piso del matraz por la que sube el vapor real -- ver el docblock de <see cref="VentAncho"/>. `Paint` a Empty, no `PaintStable`: esto QUITA piedra que <see cref="TallarRecintoCaliente"/> acaba de poner (regla 29).</summary>
        private void AbrirVentDelMatraz()
        {
            int ventX0 = _anchorX - VentAncho / 2;
            int ventX1 = ventX0 + VentAncho - 1;
            int y = _matY0 - 1; // la fila del piso (ver TallarRecintoCaliente, suelo -> y0-1).
            for (int x = ventX0; x <= ventX1; x++)
                _sim.Paint(x, y, 0, MaterialId.Empty);
        }

        private void TallarRecintoCaliente(int x0, int x1, int y0, int y1, bool suelo, bool techo)
        {
            if (suelo)
                for (int x = x0 - MuroGrosor; x <= x1 + MuroGrosor; x++)
                    _sim.PaintStable(x, y0 - 1, 0, MaterialId.Stone);

            int wallY0 = suelo ? y0 - 1 : y0;
            for (int y = wallY0; y <= y1; y++)
            {
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.PaintStable(x0 - t, y, 0, MaterialId.Stone);
                    _sim.PaintStable(x1 + t, y, 0, MaterialId.Stone);
                }
            }

            _sim.PaintRect(x0, y0, x1 - x0 + 1, y1 - y0 + 1, MaterialId.Empty);

            if (techo)
                for (int x = x0 - MuroGrosor; x <= x1 + MuroGrosor; x++)
                    _sim.PaintStable(x, y1 + 1, 0, MaterialId.Stone);
        }

        /// <summary>Borra la mampostería vieja (matraz+domo+techo+plinto: TODO, para no dejar piedra fantasma -- misma disciplina que Crisol.BorrarEnCaliente, playtest 29). Vía Paint a Empty (regla 29: esto QUITA materia, no la crea).</summary>
        private void BorrarEnCaliente(Huella h)
        {
            BorrarRecintoCaliente(h.MatX0, h.MatX1, h.MatY0, h.MatY1, suelo: true);
            BorrarRecintoCaliente(h.DomX0, h.DomX1, h.DomY0, h.DomY1, suelo: false);
            for (int x = h.DomX0 - MuroGrosor; x <= h.DomX1 + MuroGrosor; x++)
                _sim.Paint(x, h.DomY1 + 1, 0, MaterialId.Empty); // techo del domo.
        }

        private void BorrarRecintoCaliente(int x0, int x1, int y0, int y1, bool suelo)
        {
            if (suelo)
                for (int x = x0 - MuroGrosor; x <= x1 + MuroGrosor; x++)
                    _sim.Paint(x, y0 - 1, 0, MaterialId.Empty);
            int wallY0 = suelo ? y0 - 1 : y0;
            for (int y = wallY0; y <= y1; y++)
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.Paint(x0 - t, y, 0, MaterialId.Empty);
                    _sim.Paint(x1 + t, y, 0, MaterialId.Empty);
                }
        }

        /// <summary>Talla SOLO el plinto EN CALIENTE (regla 29: PaintStable) -- equivalente en caliente de <see cref="TallarEnPlano"/>, usado por <see cref="Reposicionar"/> mientras el alambique sigue en <see cref="Fase.ObraPendiente"/> (mover un instrumento a medio construir NO debe tallar de golpe el matraz/domo que el jugador todavía no ha pagado).</summary>
        private void TallarPlintoCaliente()
        {
            Alambique.PlintoRect(_anchorX, _baseY, out int x0, out int y0, out int x1, out int y1);
            for (int x = x0; x <= x1; x++)
                _sim.PaintStable(x, y0, 0, MaterialId.Stone);
        }

        /// <summary>Equivalente en caliente inverso de <see cref="TallarPlintoCaliente"/> -- borra el plinto en la posición VIEJA antes de moverlo.</summary>
        private static void BorrarPlintoCalienteEn(AlkahestSim sim, int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x <= x1; x++)
                sim.Paint(x, y0, 0, MaterialId.Empty);
        }

        /// <summary>
        /// (fix Cesar playtest 33) Bifurca por <see cref="_fase"/>: en
        /// <see cref="Fase.ObraPendiente"/> solo el PLINTO viaja (borra el
        /// viejo, talla el nuevo, hace crecer el MISMO handle reclamado en
        /// Init) -- el matraz/domo/vent siguen sin existir hasta que el
        /// jugador complete la construcción, exactamente igual que si nunca
        /// se hubiera movido. En <see cref="Fase.Construido"/> se comporta
        /// como cualquier otra estación (borra TODA la mampostería real,
        /// talla la nueva, vent incluido -- ver <see cref="TallarEnCaliente"/>).
        /// </summary>
        public void Reposicionar(Vector2Int anclaCelda)
        {
            if (_fase == Fase.ObraPendiente)
            {
                Alambique.PlintoRect(_anchorX, _baseY, out int viejoX0, out int viejoY0, out int viejoX1, out int viejoY1);
                BorrarPlintoCalienteEn(_sim, viejoX0, viejoY0, viejoX1, viejoY1); // 1) borrar el plinto VIEJO.

                int dxP = anclaCelda.x - _outX0;
                int dyP = anclaCelda.y - _baseY;
                _anchorX += dxP;
                _baseY += dyP;
                RecalcularRegiones(); // mueve `transform.position` -- el fantasma (hijo con SetParent(transform,false) a offset local cero, ver BuildVisualGhost) viaja SOLO, sin recrear nada (regla 36: nunca Init/BuildVisual* para mover).
                TallarPlintoCaliente(); // 2) tallar el plinto nuevo.

                Alambique.PlintoRect(_anchorX, _baseY, out int nuevoX0, out int nuevoY0, out int nuevoX1, out int nuevoY1);
                SimLevelBuilder.ActualizarObra(_handleObra, nuevoX0, nuevoY0, nuevoX1, nuevoY1); // 3) actualizar el registro anticincel (mismo handle de siempre).
                return;
            }

            BorrarEnCaliente(Calcular(_anchorX, _baseY)); // 1) borrar la huella VIEJA.

            int dx = anclaCelda.x - _outX0;
            int dy = anclaCelda.y - _baseY;
            _anchorX += dx;
            _baseY += dy;
            RecalcularRegiones();
            TallarEnCaliente(); // 2) tallar la nueva -- regla 36: nunca volver a llamar a Init/BuildVisual*.

            SimLevelBuilder.ActualizarObra(_handleObra, _outX0, _outY0, _outX1, _outY1); // 3) actualizar el registro anticincel.
        }

        // =================================================================
        // BUCLE
        // =================================================================
        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return;

            if (_fase == Fase.ObraPendiente)
            {
                _sondeoAcc += Time.deltaTime;
                if (_sondeoAcc >= SondeoObraSeg)
                {
                    _sondeoAcc -= SondeoObraSeg;
                    SondearZonaDeObra();
                }

                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                    && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && !AlbumReal.Abierto && EstaEnfocada()) // (integración pt50, regla 12) la ficha modal también bloquea E.
                {
                    IntentarConstruir();
                    MachineFocus.RegistrarUsoE(); // (ENCARGO N) sin cambios: cuenta como uso aunque falte cerámico -- ver el docblock de IntentarConstruir.
                }

                ActualizarGhost();
            }
            else
            {
                _accumulator += Time.deltaTime;
                int steps = 0;
                while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
                {
                    ApplyFrioTick();
                    _accumulator -= TickDt;
                    steps++;
                }
                if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;

                _sondeoAcc += Time.deltaTime;
                if (_sondeoAcc >= SondeoObraSeg)
                {
                    _sondeoAcc -= SondeoObraSeg;
                    SondearMatrazParaRotulo();
                }

                if (_resalte != null)
                {
                    float objetivo = EstaEnfocada() ? 0.35f + 0.15f * Mathf.Sin(Time.time * 3f) : 0f;
                    _alfaResalte = Mathf.MoveTowards(_alfaResalte, objetivo, 6f * Time.deltaTime);
                    _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, _alfaResalte);
                }
            }
        }

        private bool EstaEnfocada() => MachineFocus.EsFoco(this, _player);

        /// <summary>
        /// (fase ObraPendiente) Barre el hueco del plinto/matraz buscando
        /// cerámico (cualquiera de las 5 variantes de esta seed --
        /// <see cref="EstadoMateria.Ceramico"/> es un ESTADO, no una base
        /// concreta) y lo consume celda a celda hasta el umbral. Sondeo con
        /// acumulador (<see cref="SondeoObraSeg"/>), nunca cada tick.
        /// </summary>
        private void SondearZonaDeObra()
        {
            if (_ceramicoAcumulado >= CeramicoRequerido) return;
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;

            for (int y = _matY0; y <= _matY1 && _ceramicoAcumulado < CeramicoRequerido; y++)
            {
                for (int x = _matX0; x <= _matX1 && _ceramicoAcumulado < CeramicoRequerido; x++)
                {
                    byte m = grid.GetMat(x, y);
                    if (m == MaterialId.Empty) continue;
                    if (!MaterialId.EsBaseEstado(m) || MaterialId.EstadoDe(m) != EstadoMateria.Ceramico) continue;
                    grid.SetCell(x, y, MaterialId.Empty);
                    grid.WakeChunk(x, y, tick);
                    _ceramicoAcumulado++;
                }
            }
        }

        /// <summary>
        /// (ENCARGO N, playtest 43) EL HANDLER COMPARTIDO DE E -- sin
        /// cambios de lógica, solo la firma pasa de `void` a `bool`. El
        /// gate "solo mientras ObraPendiente" vive en `Update()` (el E ni
        /// se comprueba en <see cref="Fase.Construido"/>, y `MachineFocus.Olvidar`
        /// se llama en <see cref="CompletarConstruccion"/> así que un
        /// invitado tampoco vería nunca "E — usar" sobre un alambique ya
        /// construido, ver <see cref="MaquinaReplica.EsUsableRemota"/> más
        /// el estado del arbitraje) -- <see cref="UsarPorRed"/> replica ese
        /// mismo gate explícitamente, no confía solo en que la réplica no
        /// lo ofrezca.
        /// </summary>
        private bool IntentarConstruir()
        {
            if (_ceramicoAcumulado < CeramicoRequerido)
            {
                Rotular("faltan " + (CeramicoRequerido - _ceramicoAcumulado) + " celdas de cerámico", UiStyles.Aviso);
                return false;
            }
            CompletarConstruccion();
            return true;
        }

        // =================================================================
        // (ENCARGO N, playtest 43, CONTRATO_PARIDAD.md §2a/§2b) EL GANCHO REMOTO
        // =================================================================
        bool IMaquinaUsableRemota.UsarPorRed() => _fase == Fase.ObraPendiente && IntentarConstruir();

        /// <summary>
        /// (DECISIÓN fuera de contrato, documentada en el informe de la
        /// ronda) El Alambique no calza limpio en Trabajando/FuegoEncendido/
        /// ResultadoListo: destila SIEMPRE, en silencio, sin un "en curso"
        /// que anunciar. Se reutiliza <see cref="EstadoVivoBits.ResultadoListo"/>
        /// para "hay agua destilada esperando en el matraz" -- es la lectura
        /// más cercana a "algo REPOSA en la cubeta esperando recogida" (§1)
        /// que tiene este aparato, y así la réplica del invitado también
        /// destella "ven a recoger" cuando el matraz tiene algo.
        /// </summary>
        byte IMaquinaUsableRemota.EstadoVivoRed() =>
            _fase == Fase.Construido && _aguaAcumuladaVista > 0 ? EstadoVivoBits.ResultadoListo : (byte)0;

        private void CompletarConstruccion()
        {
            TallarEnCaliente();
            DestruirGhost();
            BuildVisualReal();

            _fase = Fase.Construido;
            MachineFocus.Olvidar(this); // ya no hace falta E: destila solo, siempre.

            // (fix Cesar playtest 33) Hace CRECER el MISMO handle reclamado en
            // Init (el del plinto) hasta cubrir la huella completa
            // (matraz+domo+techo) en vez de registrar un segundo handle --
            // ver el docblock de Init, "BUG SECUNDARIO", para el porqué: un
            // segundo RegistrarObra aquí dejaba el handle del plinto huérfano
            // en ObraDelTaller para siempre. `Mudanza.RegistrarMovible` YA se
            // llamó en Init (este aparato es movible desde el primer frame,
            // en las dos fases) -- llamarlo otra vez aquí sería un no-op
            // (RegistrarMovible ya se defiende de duplicados), así que se quita.
            var h = Calcular(_anchorX, _baseY);
            SimLevelBuilder.ActualizarObra(_handleObra, h.OutX0, h.OutY0, h.OutX1, h.OutY1);

            Rotular(null, UiStyles.Exito);
            Debug.Log("[ChaosAlchemy] El alambique se ha construido: atrapa el vapor y destila.");
        }

        /// <summary>
        /// (fase Construido) Empuja el interior del DOMO hacia
        /// <see cref="FrioTargetRaw"/> -- "patrón HeatPlate invertido" del
        /// encargo. Solo toca celdas de arquetipo Gas (el Steam que acaba de
        /// entrar): dejar caer la deliberación sobre el Water ya condensado
        /// evita el efecto colateral de congelarlo en tránsito (raw45 está
        /// por debajo de Ice.freezesAt en cualquier seed, ver Universe.Create)
        /// -- lo que sale del alambique es agua destilada, no una carámbano
        /// accidental.
        /// </summary>
        private void ApplyFrioTick()
        {
            var grid = _sim.Grid;
            var universe = _sim.Universe;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;

            for (int y = _domY0; y <= _domY1; y++)
            {
                for (int x = _domX0; x <= _domX1; x++)
                {
                    int idx = CellGrid.Idx(x, y);
                    byte m = grid.GetMat(idx);
                    if (m == MaterialId.Empty) continue;
                    if (universe.Get(m).archetype != MaterialArchetype.Gas) continue;

                    int cur = grid.temp[idx];
                    int next = cur < FrioTargetRaw ? cur : Mathf.Max(FrioTargetRaw, cur - FrioStepPerTick);
                    // (nunca sube: el domo solo enfría -- si algo ya está más frío que el objetivo, se deja tal cual, no se calienta hacia él.)
                    grid.temp[idx] = (byte)next;
                    grid.WakeChunk(x, y, tick);
                }
            }
        }

        /// <summary>Cuenta el agua ya destilada en el matraz, para el rótulo -- puramente informativo, sondeo barato (45 celdas como mucho).</summary>
        private void SondearMatrazParaRotulo()
        {
            var grid = _sim.Grid;
            int n = 0;
            for (int y = _matY0; y <= _matY1; y++)
                for (int x = _matX0; x <= _matX1; x++)
                    if (grid.GetMat(x, y) == MaterialId.Water) n++;
            _aguaAcumuladaVista = n;
        }

        // =================================================================
        // VISUAL
        // =================================================================
        /// <summary>
        /// La silueta FANTASMA (mandato del encargo): un rectángulo
        /// translúcido con la huella EXTERIOR completa (domo+matraz), tinte
        /// frío de "plano" -- no pretende ser el sprite final (eso es
        /// <see cref="BuildVisualReal"/>, y otro encargo lo embellecerá
        /// después, ver el docblock de la clase): solo tiene que comunicar
        /// "aquí va algo" mientras el jugador paga el cerámico.
        /// </summary>
        private void BuildVisualGhost()
        {
            float c = SimRenderer.CellWorldSize;
            int span = _outX1 - _outX0 + 1;
            int alto = _outY1 - _outY0 + 1;

            _ghostGo = new GameObject("AlambiqueFantasma");
            _ghostGo.transform.SetParent(transform, false);
            _ghostGo.transform.position = new Vector3(_centro.x, _centro.y, 0f);
            _ghostSprite = MaquinariaSprites.CrearCapa(_ghostGo.transform, "Sprite", MaquinariaSprites.Solido(), 18,
                span * c, alto * c);
            _ghostSprite.color = new Color(0.75f, 0.90f, 1f, 0.18f);

            _resalte = MaquinariaSprites.CrearCapa(_ghostGo.transform, "Resalte", MaquinariaSprites.Solido(), 17,
                span * c * 1.05f, alto * c * 1.05f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);
        }

        private void ActualizarGhost()
        {
            if (_ghostSprite != null)
            {
                float pulso = 0.14f + 0.08f * Mathf.Sin(Time.time * 1.4f);
                _ghostSprite.color = new Color(0.75f, 0.90f, 1f, pulso);
            }
            if (_resalte != null)
            {
                float objetivo = EstaEnfocada() ? 0.35f + 0.15f * Mathf.Sin(Time.time * 4f) : 0f;
                _alfaResalte = Mathf.MoveTowards(_alfaResalte, objetivo, 6f * Time.deltaTime);
                _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, _alfaResalte);
            }
        }

        private void DestruirGhost()
        {
            if (_ghostGo != null) Destroy(_ghostGo);
            _ghostGo = null;
            _ghostSprite = null;
            _resalte = null; // BuildVisualReal crea su propio resalte -- ver ahí.
        }

        /// <summary>
        /// El instrumento YA CONSTRUIDO. La piedra real (matraz+domo) ya la
        /// dibuja SimRenderer sola (es CellGrid.Stone de verdad, con su
        /// sillería -- ver SimRenderer.ComputeCellColor, regla 19): esta capa
        /// solo añade el VIDRIO (mismo factory y mismo sortingOrder que
        /// Game/ColumnaEnsayo.cs, "se ve la materia detrás") para leer domo y
        /// matraz como instrumento de laboratorio y no como dos agujeros en
        /// la roca.
        /// </summary>
        private void BuildVisualReal()
        {
            float c = SimRenderer.CellWorldSize;
            const int ordenVidrio = -7; // mismo valor que ColumnaEnsayo.OrdenVidrio: entre el fondo (-10) y el sprite de la sim (-5).

            var vidrioDomoGo = new GameObject("AlambiqueVidrioDomo");
            vidrioDomoGo.transform.SetParent(transform, false);
            vidrioDomoGo.transform.position = _centroDomo;
            MaquinariaSprites.CrearCapa(vidrioDomoGo.transform, "Sprite", MaquinariaSprites.VidrioPanel(DomoAncho, DomoAlto),
                ordenVidrio, DomoAncho * c, DomoAlto * c);

            var vidrioMatrazGo = new GameObject("AlambiqueVidrioMatraz");
            vidrioMatrazGo.transform.SetParent(transform, false);
            vidrioMatrazGo.transform.position = _centroMatraz;
            MaquinariaSprites.CrearCapa(vidrioMatrazGo.transform, "Sprite", MaquinariaSprites.VidrioPanel(MatrazAncho, MatrazAlto),
                ordenVidrio, MatrazAncho * c, MatrazAlto * c);

            // Zuncho de latón en el cuello (frontera domo/matraz): el mismo
            // truco que las bandas de ColumnaEnsayo -- "un tonel se ciñe con
            // aros" -- para que se lea como UN instrumento de dos cámaras, no
            // como dos cajas apiladas por accidente.
            var zunchoGo = new GameObject("AlambiqueZuncho");
            zunchoGo.transform.SetParent(transform, false);
            zunchoGo.transform.position = new Vector3(_centro.x, (_domY0) * c, 0f);
            var zuncho = MaquinariaSprites.CrearCapa(zunchoGo.transform, "Sprite", MaquinariaSprites.Solido(), -6,
                (DomoAncho + 2 * MuroGrosor) * c, 1f * c);
            zuncho.color = new Color(0.80f, 0.62f, 0.24f, 0.9f);

            _resalte = MaquinariaSprites.CrearCapa(transform, "Resalte", MaquinariaSprites.Solido(), 16,
                (_outX1 - _outX0 + 1) * c * 1.05f, (_outY1 - _outY0 + 1) * c * 1.05f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);
        }

        // =================================================================
        // RÓTULOS (español latino, tuteo)
        // =================================================================
        private string _aviso;
        private Color _avisoColor = UiStyles.Aviso;
        private float _avisoHasta;

        private void Rotular(string texto, Color color)
        {
            _aviso = texto;
            _avisoColor = color;
            _avisoHasta = texto != null ? Time.time + 3.5f : 0f;
        }

        private string EtiquetaObraPendiente()
        {
            if (_aviso != null && Time.time < _avisoHasta) return _aviso;
            if (_ceramicoAcumulado >= CeramicoRequerido) return "listo · E para construir el alambique";
            return "construible: " + CeramicoRequerido + " celdas de cerámico · llevas " + _ceramicoAcumulado;
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return;

            if (_fase == Fase.ObraPendiente)
            {
                float cerca = UiStyles.Cercania(_centro, _player, RangoNombrePleno + 2f, RangoNombreDesvanece + 2f);
                if (cerca <= 0f) return;
                UiStyles.Preparar();
                UiStyles.PlacaMundo(_centro, EtiquetaObraPendiente(),
                    new Color(UiStyles.Aviso.r, UiStyles.Aviso.g, UiStyles.Aviso.b, UiStyles.Aviso.a * cerca), UiStyles.S(0f));

                if (MachineFocus.MostrarPromptE && EstaEnfocada() && !UiStyles.RatonOcupado && _ceramicoAcumulado >= CeramicoRequerido)
                {
                    UiStyles.PlacaMundo(_centro, "E — construir",
                        new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, cerca), -UiStyles.S(14f));
                }
                return;
            }

            float cercaNombre = UiStyles.Cercania(_centro, _player, RangoNombrePleno, RangoNombreDesvanece);
            if (cercaNombre <= 0f) return;
            if (!_yaConocida && cercaNombre >= 0.98f) _yaConocida = true;

            UiStyles.Preparar();
            Color tenue = UiStyles.TextoTenue;
            UiStyles.PlacaMundo(_centroDomo, Verbo, new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercaNombre), UiStyles.S(6f));
            if (!_yaConocida)
                UiStyles.PlacaMundo(_centroDomo, ChapaNombre, new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercaNombre), UiStyles.S(18f));
            if (_aguaAcumuladaVista > 0)
                UiStyles.PlacaMundo(_centroMatraz, "agua destilada: " + _aguaAcumuladaVista,
                    new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercaNombre), -UiStyles.S(14f));
        }
    }
}
