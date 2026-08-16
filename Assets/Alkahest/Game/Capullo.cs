using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// [ChaosAlchemy · pivot playtest 21] EL CAPULLO — sellado, al lado del
    /// Rescoldo. Mismo generador de silueta que <see cref="Criatura"/> (bulbo
    /// por semiancho-de-fila, ver <see cref="SerSprites.MascaraCapullo"/>)
    /// pero cerrado y ovoide (sin cuello), y la MISMA firma visual de la
    /// semilla que el corazón (MaterialDef de Vivium) con una variación:
    /// mezclada hacia un tono de cáscara fijo (<see cref="SerSprites.MatizarHaciaCascara"/>)
    /// para que se lea como PARIENTE del Rescoldo, no como otra sustancia.
    ///
    /// RESPIRA muy despacio (latido de amplitud mínima, siempre activo,
    /// independiente del progreso — ver <see cref="ActualizarRespiracion"/>).
    ///
    /// SE AGRIETA POR FASES: <see cref="SerSprites.FasesGrieta"/> (5)
    /// texturas horneadas UNA vez (0=intacto .. 4=a punto de abrirse, ver
    /// <see cref="SerSprites.AplicarGrietas"/>). Las grietas SON el
    /// indicador de progreso — nada de barras ni números.
    ///
    /// EL PROGRESO LO MUEVE EL CALOR, NO EL RELOJ (decisión explícita del
    /// encargo): acumula solo mientras la temperatura de su celda está por
    /// encima de un umbral tibio. Se reutiliza
    /// <see cref="Universe.VivGrowMinRaw"/> como ese umbral — es LITERALMENTE
    /// "la temperatura mínima en la que la vida de esta semilla crece", así
    /// que "hace falta calor de cultivo para que el capullo madure" es
    /// coherente con lo que el jugador ya aprende cultivando Vivium, sin
    /// inventar una constante nueva sin relación con el resto del sistema.
    ///
    /// ECLOSIONA (decisión de Cesar) al llegar al final: instancia una
    /// <see cref="Criatura"/> con esCria=true cerca del progenitor que la
    /// incubó (el más cercano vivo ahora mismo -- ver
    /// <see cref="Criatura.MasCercanaA"/>; si no hay ninguno, junto al
    /// propio capullo) y desaparece tras un pulso breve — ver
    /// <see cref="Eclosionar"/>.
    ///
    /// HERENCIA DE TEMPERAMENTO CON DESVIACIÓN (playtest 22, decisión de
    /// Cesar: "hereda con desviación, no tirada nueva" -- ver
    /// <see cref="HeredarTemperamentoConDesviacion"/>). Se resuelve AQUÍ, en
    /// Eclosionar, porque solo Capullo conoce a la vez al progenitor Y el
    /// instante en que la cría nace; Criatura.Init recibe el resultado ya
    /// calculado a través de <see cref="Criatura.TemperamentoHeredadoPendiente"/>
    /// (su firma está CONGELADA por CONTRATO_PIVOT.md, no puede ganar un
    /// parámetro nuevo).
    ///
    /// SE PUEDE MOVER: implementa <see cref="IMovible"/> (playtest 22, "y se
    /// pueden mover"). El capullo no siembra nada en la sim (a diferencia de
    /// Criatura), así que moverlo es solo recolocar el sprite -- no hay
    /// cuerpo simulado del que preocuparse.
    /// </summary>
    public sealed class Capullo : MonoBehaviour, IMovible
    {
        /// <summary>
        /// (playtest 21, CORREGIDO MIRANDO EL JUEGO CORRER) Ancho del capullo en
        /// unidades de mundo. Estaba en 1.15 y en pantalla era un DESASTRE de
        /// composición: con la proporción alto/ancho del sprite salía un bulto
        /// de ~19 celdas de alto, casi la mitad de la altura de la sala entera
        /// (42 celdas) y **el doble de alto que la propia criatura** — que mide
        /// 1.0 de ancho. El capullo dominaba el encuadre y la criatura, que es
        /// la protagonista, quedaba de comparsa a su lado.
        ///
        /// 0.55 lo deja deliberadamente MÁS PEQUEÑO que el corazón. Es lo que
        /// tiene que ser: la criatura es quien está aquí y ahora, y el capullo
        /// es una promesa — algo pequeño y cerrado al lado de algo vivo. Si
        /// alguien lo vuelve a subir, que sea mirando la pantalla, no el número.
        /// </summary>
        private const float AnchoMundoCapullo = 0.55f;
        private const float AltoMundoCapullo = AnchoMundoCapullo * SerSprites.CapulloH / SerSprites.CapulloW;

        /// <summary>Mezcla hacia este tono fijo (cáscara/madera vieja): "pariente pero distinto" del corazón, sin depender de la semilla para la identidad de "esto es una cáscara".</summary>
        private static readonly Color32 TonoCascara = new Color32(150, 112, 70, 255);
        private const float MezclaCascara = 0.32f;

        /// <summary>2-4 minutos de CALOR SOSTENIDO (contrato) -- no de reloj: si se enfría, el progreso simplemente se detiene, así que la duración real de una partida puede ser mayor.</summary>
        private const float DuracionCalorSegundos = 180f;
        private const float IntervaloSondeo = 0.5f;

        private AlkahestSim _sim;
        private Transform _jugador;
        private int _celdaRepisaX, _celdaRepisaY;

        private Transform _pivote;
        private SpriteRenderer _sr;
        private Sprite[] _spritesPorFase;
        private Texture2D[] _texturasPorFase;
        private int _faseActual = -1;

        private float _progreso; // 0..1, solo avanza con calor.
        private float _accPoll;
        private float _faseRespiro;

        private bool _eclosionado;
        private float _tiempoTrasEclosion;
        private const float DesvanecerTrasEclosionSeg = 0.9f;
        private const float DestruirTrasEclosionSeg = 1.8f;

        /// <summary>celdaRepisaX/Y = celda de SUELO/repisa sobre la que se apoya.</summary>
        public void Init(AlkahestSim sim, Transform jugador, int celdaRepisaX, int celdaRepisaY)
        {
            _sim = sim;
            _jugador = jugador;
            _celdaRepisaX = celdaRepisaX;
            _celdaRepisaY = celdaRepisaY;

            BuildVisuals();
            Mudanza.RegistrarMovible(this); // (playtest 22, "y se pueden mover") ver el contrato IMovible en Game/Mudanza.cs.
        }

        private void OnDestroy()
        {
            Mudanza.OlvidarMovible(this);
            LiberarTexturas();
        }

        // ---------------------------------------------------------------------------------
        // IMOVIBLE (playtest 22, ver el contrato en Game/Mudanza.cs). El
        // capullo no siembra nada en la sim -- moverlo es solo recolocar el
        // sprite, no hay cuerpo simulado que podar/resembrar (a diferencia
        // de Criatura.Reposicionar).
        // ---------------------------------------------------------------------------------
        public Vector3 CentroMundo => _pivote != null ? _pivote.position : transform.position;
        public Vector2 TamanoMundo => new Vector2(AnchoMundoCapullo, AltoMundoCapullo);
        public Vector2Int AnclaCelda => new Vector2Int(_celdaRepisaX, _celdaRepisaY);

        /// <summary>Margen mínimo (el capullo no sondea nada por sí mismo -- solo necesita que su propio sprite, pequeño, no salga del marco protegido del mundo).</summary>
        private const int MargenMundoCapullo = 3;

        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            return anclaCelda.x - MargenMundoCapullo >= 1 && anclaCelda.x + MargenMundoCapullo <= CellGrid.W - 2
                && anclaCelda.y >= 1 && anclaCelda.y + MargenMundoCapullo <= CellGrid.H - 2;
        }

        public void Reposicionar(Vector2Int anclaCelda)
        {
            _celdaRepisaX = anclaCelda.x;
            _celdaRepisaY = anclaCelda.y;
            RecalcularTransform();
        }

        /// <summary>Extraído de BuildVisuals (playtest 22) para que Reposicionar pueda reutilizarlo sin volver a llamar a BuildVisuals/Init (regla 36).</summary>
        private void RecalcularTransform()
        {
            float celda = SimRenderer.CellWorldSize;
            float baseX = (_celdaRepisaX + 0.5f) * celda;
            float baseY = (_celdaRepisaY + 1) * celda;
            transform.position = new Vector3(baseX, baseY, 0f);
        }

        // ===================================================================
        // Construcción visual (UNA vez: 5 fotogramas de grieta horneados de
        // entrada, Update solo alterna cuál se muestra y pulsa la escala).
        // ===================================================================
        private void BuildVisuals()
        {
            RecalcularTransform();

            var pivoteGo = new GameObject("PivoteRespiro");
            _pivote = pivoteGo.transform;
            _pivote.SetParent(transform, false);
            _pivote.localPosition = new Vector3(0f, AltoMundoCapullo * 0.5f, 0f);

            int w = SerSprites.CapulloW, h = SerSprites.CapulloH;
            int seed = _sim != null ? _sim.Universe.Seed : 0;
            byte[] mask = SerSprites.MascaraCapullo(w, h);
            bool[] esBorde = SerSprites.CalcularBorde(mask, w, h, 2);

            var def = _sim.Universe.Get(MaterialId.Vivium);
            var pxBase = FirmaVisualFabrica.GenerarPixeles(w, h, def, 0, mask, esBorde, sobreMundo: true);
            SerSprites.MatizarHaciaCascara(pxBase, mask, MezclaCascara, TonoCascara);

            int nFases = SerSprites.FasesGrieta;
            _spritesPorFase = new Sprite[nFases];
            _texturasPorFase = new Texture2D[nFases];
            for (int fase = 0; fase < nFases; fase++)
            {
                var px = (Color32[])pxBase.Clone();
                SerSprites.AplicarGrietas(px, mask, w, h, fase, seed + 4242);
                _spritesPorFase[fase] = SerSprites.CrearSprite(px, w, h, new Vector2(0.5f, 0.5f),
                    "ChaosAlchemyCapulloFase_" + fase, out _texturasPorFase[fase]);
            }

            _sr = MaquinariaSprites.CrearCapa(_pivote, "Cascara", _spritesPorFase[0], 43, AnchoMundoCapullo, AltoMundoCapullo);
            _faseActual = 0;
        }

        private void LiberarTexturas()
        {
            if (_texturasPorFase == null) return;
            for (int i = 0; i < _texturasPorFase.Length; i++)
                if (_texturasPorFase[i] != null) Destroy(_texturasPorFase[i]);
        }

        // ===================================================================
        private void Update()
        {
            if (_sim == null) return;
            float dt = Time.deltaTime;

            if (_eclosionado)
            {
                ActualizarPostEclosion(dt);
                return;
            }

            ActualizarRespiracion(dt);

            _accPoll += dt;
            if (_accPoll >= IntervaloSondeo)
            {
                float dtSondeo = _accPoll;
                _accPoll = 0f;
                SondearCalor(dtSondeo);
            }
        }

        /// <summary>"Respira muy despacio": late SIEMPRE, sin relación con el progreso -- un capullo frío sigue vivo, solo no madura.</summary>
        private void ActualizarRespiracion(float dt)
        {
            const float freq = 0.12f;
            const float amp = 0.018f;
            _faseRespiro += freq * dt * Mathf.PI * 2f;
            if (_faseRespiro > Mathf.PI * 2f) _faseRespiro -= Mathf.PI * 2f;
            float escala = 1f + amp * Mathf.Sin(_faseRespiro);
            _pivote.localScale = new Vector3(escala, escala, 1f);
        }

        private void SondearCalor(float dtSondeo)
        {
            byte tempRaw = _sim.SampleTempRaw(_celdaRepisaX, _celdaRepisaY + 2);
            bool tibio = tempRaw > _sim.Universe.VivGrowMinRaw;
            if (tibio)
            {
                _progreso = Mathf.Clamp01(_progreso + dtSondeo / DuracionCalorSegundos);
            }

            int fase = Mathf.Clamp(Mathf.FloorToInt(_progreso * SerSprites.FasesGrieta), 0, SerSprites.FasesGrieta - 1);
            if (fase != _faseActual)
            {
                _sr.sprite = _spritesPorFase[fase];
                _faseActual = fase;
            }

            if (_progreso >= 1f) Eclosionar();
        }

        /// <summary>Sal arbitraria (distingue este hash de cualquier otro uso de XorShift en el proyecto): la DESVIACIÓN de herencia de temperamento, ver <see cref="HeredarTemperamentoConDesviacion"/>.</summary>
        private const uint SalHerenciaTemperamento = 0x7A11u;

        /// <summary>Cuánto puede desviarse la cría del temperamento de su progenitor, en una sola generación (ver el docblock de la clase, "HERENCIA DE TEMPERAMENTO CON DESVIACIÓN"). Perceptible pero acotado: dos generaciones seguidas en la misma dirección mueven el temperamento de un extremo a otro con claridad, sin que UNA sola tirada pueda hacerlo.</summary>
        private const float DesviacionMaxTemperamento = 0.16f;

        /// <summary>
        /// Instancia una Criatura(esCria:true) cerca del progenitor que la
        /// incubó -- la Criatura activa más cercana a ESTE capullo ahora
        /// mismo (<see cref="Criatura.MasCercanaA"/>, una aproximación
        /// razonable a "quien lo cuidó": normalmente es quien lo mantuvo
        /// tibio lo bastante para llegar hasta aquí, ver
        /// <see cref="Criatura.ApplyCalorTick"/>). Si no hay ninguna viva
        /// (caso límite), nace junto al propio capullo y sin progenitor de
        /// quien heredar -- <see cref="Criatura.SortearOHeredarTemperamento"/>
        /// cae entonces a un sorteo fresco por semilla, el mismo criterio que
        /// el Rescoldo original.
        /// </summary>
        private void Eclosionar()
        {
            if (_eclosionado) return;
            _eclosionado = true;

            int celdaX = _celdaRepisaX;
            int celdaY = _celdaRepisaY;
            var progenitor = Criatura.MasCercanaA(transform.position);
            if (progenitor != null)
            {
                var cp = _sim.WorldToCell(progenitor.transform.position);
                // -1 en Y: Criatura.Init ancla en la celda de SUELO (su
                // transform.position real queda un cell por ENCIMA de esa
                // celda, ver Criatura.BuildVisuals), así que hay que
                // deshacer ese +1 para volver a una celda de suelo válida.
                celdaX = cp.x + 3;
                celdaY = cp.y - 1;

                // HERENCIA CON DESVIACIÓN (playtest 22, decisión de Cesar:
                // "hereda con desviación, no tirada nueva" -- ver el
                // docblock de la clase). Se fija ANTES de Init porque su
                // firma está CONGELADA por CONTRATO_PIVOT.md.
                Criatura.TemperamentoHeredadoPendiente =
                    HeredarTemperamentoConDesviacion(progenitor.TemperamentoNormalizado, _sim.Universe.Seed);
            }

            var criaturaGo = new GameObject("Criatura (cría)");
            var cria = criaturaGo.AddComponent<Criatura>();
            cria.Init(_sim, _jugador, celdaX, celdaY, esCria: true);

            _tiempoTrasEclosion = 0f;
        }

        /// <summary>
        /// El temperamento del progenitor +/- una desviación pequeña pero
        /// PERCEPTIBLE, determinista (regla del proyecto: nunca
        /// UnityEngine.Random -- solo <see cref="XorShift"/>). tick=0
        /// CONSTANTE, (x,y)=la celda de la repisa donde ESTE capullo
        /// eclosiona ("el momento": única por instancia, y estable si el
        /// jugador no lo mueve entre sondeos), sal=la semilla del universo
        /// combinada con <see cref="SalHerenciaTemperamento"/> -- misma
        /// partida + mismo sitio + mismo instante siempre da la misma
        /// desviación.
        ///
        /// EJEMPLO REAL de dos generaciones (semilla=20, verificado
        /// ejecutando el mismo algoritmo fuera de Unity antes de escribir
        /// esto): madre templada (0.502) -> incuba un capullo en (315,175),
        /// desviación +0.115 -> cría1 templada pero ya tirando a caliente
        /// (0.617) -> esa cría1 incuba otro capullo en (320,170), desviación
        /// +0.154 -> cría2 CALIENTE (0.771, cruza el umbral de 0.65). Dos
        /// generaciones, elegidas por el jugador, mueven el temperamento de
        /// "templado" a "caliente" con claridad.
        /// </summary>
        private float HeredarTemperamentoConDesviacion(float delProgenitor, int seed)
        {
            var rng = XorShift.FromCell(0u, _celdaRepisaX, _celdaRepisaY, (uint)seed ^ SalHerenciaTemperamento);
            float desviacion = (rng.NextByte() / 255f * 2f - 1f) * DesviacionMaxTemperamento;
            return Mathf.Clamp01(delProgenitor + desviacion);
        }

        /// <summary>Un pulso de luz + temblor breve, luego se desvanece y el GameObject desaparece (contrato: "desaparece o queda como cáscara rota" -- se eligió desaparecer por alcance del encargo).</summary>
        private void ActualizarPostEclosion(float dt)
        {
            _tiempoTrasEclosion += dt;

            if (_tiempoTrasEclosion < DesvanecerTrasEclosionSeg)
            {
                float t = _tiempoTrasEclosion / DesvanecerTrasEclosionSeg;
                float temblor = (1f - t) * 0.05f;
                _pivote.localPosition = new Vector3(
                    Mathf.Sin(Time.time * 50f) * temblor,
                    AltoMundoCapullo * 0.5f, 0f);
                // Destello breve: el propio sprite de fase 4 ya trae grietas+brillo
                // horneados, así que aquí basta con sobreexponer un poco el tinte
                // (por encima de 1.0 lo recorta el material por defecto -> "flash").
                _sr.color = Color.Lerp(Color.white, new Color(1.6f, 1.5f, 1.3f, 1f), 1f - t);
            }
            else
            {
                float t = Mathf.Clamp01((_tiempoTrasEclosion - DesvanecerTrasEclosionSeg) / (DestruirTrasEclosionSeg - DesvanecerTrasEclosionSeg));
                var c = _sr.color;
                c.a = 1f - t;
                _sr.color = c;
                if (_tiempoTrasEclosion >= DestruirTrasEclosionSeg) Destroy(gameObject);
            }
        }
    }
}
