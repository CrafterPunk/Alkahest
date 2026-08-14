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
    /// <see cref="Criatura"/> con esCria=true cerca de
    /// <see cref="Criatura.Principal"/> si existe (si no, junto al propio
    /// capullo) y desaparece tras un pulso breve — ver <see cref="Eclosionar"/>.
    /// </summary>
    public sealed class Capullo : MonoBehaviour
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
        }

        private void OnDestroy()
        {
            LiberarTexturas();
        }

        // ===================================================================
        // Construcción visual (UNA vez: 5 fotogramas de grieta horneados de
        // entrada, Update solo alterna cuál se muestra y pulsa la escala).
        // ===================================================================
        private void BuildVisuals()
        {
            float celda = SimRenderer.CellWorldSize;
            float baseX = (_celdaRepisaX + 0.5f) * celda;
            float baseY = (_celdaRepisaY + 1) * celda;
            transform.position = new Vector3(baseX, baseY, 0f);

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

        /// <summary>Instancia una Criatura(esCria:true) cerca de Criatura.Principal si existe (si no, junto al propio capullo) y se desvanece.</summary>
        private void Eclosionar()
        {
            if (_eclosionado) return;
            _eclosionado = true;

            int celdaX = _celdaRepisaX;
            int celdaY = _celdaRepisaY;
            if (Criatura.Principal != null)
            {
                var cp = _sim.WorldToCell(Criatura.Principal.transform.position);
                // -1 en Y: Criatura.Init ancla en la celda de SUELO (su
                // transform.position real queda un cell por ENCIMA de esa
                // celda, ver Criatura.BuildVisuals), así que hay que
                // deshacer ese +1 para volver a una celda de suelo válida.
                celdaX = cp.x + 3;
                celdaY = cp.y - 1;
            }

            var criaturaGo = new GameObject("Criatura (cría)");
            var cria = criaturaGo.AddComponent<Criatura>();
            cria.Init(_sim, _jugador, celdaX, celdaY, esCria: true);

            _tiempoTrasEclosion = 0f;
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
