using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// EL BANCO DE CHISPA (LO QUE PERSISTE, encargo B, §5.3 de
    /// CONTRATO_PERSISTE.md). Dos bornes y una RANURA (~3x2 celdas) entre
    /// ellos, con una lámpara encima. Con E: lee el material dominante de la
    /// ranura y enciende la lámpara según <see cref="Universe.Conductividad"/>
    /// (0 = nada, 1 = tenue, 2 = pleno). NO TRANSFORMA NADA — es el único
    /// aparato de análisis puro del laboratorio (diseño §2: "la lámpara
    /// enciende... es el único aparato de ANÁLISIS puro: no transforma,
    /// revela"). La conductividad es LA propiedad deliberadamente invisible: a
    /// simple vista una celda conductora y una que no lo es se ven exactamente
    /// igual (diseño §1.6) — solo esta lámpara la delata.
    ///
    /// -----------------------------------------------------------------------
    /// SPRITES: SOLO MaquinariaSprites EXISTENTE (mismo criterio que Crisol.cs
    /// y Prensa.cs). El chasis reutiliza ChasisPlaca; la lámpara reutiliza
    /// ResistenciasPlaca (el mismo serpentín, aquí leído como filamento) tintada
    /// de blanco-azulado en vez de ámbar; los dos bornes son un par de
    /// MaquinariaSprites.Solido() tintados de latón oscuro.
    ///
    /// -----------------------------------------------------------------------
    /// DECISIÓN (fuera del contrato, documentada): LA RANURA ES MAMPOSTERÍA
    /// PROPIA DEL BANCO, TALLADA EN Init() — mismo motivo y mismo patrón que
    /// Crisol.CarveBasin/Prensa.TallarLecho (ver esos docblocks).
    ///
    /// -----------------------------------------------------------------------
    /// PLAYTEST 26 (CONTRATO_LEGIBILIDAD.md, encargo M)
    /// -----------------------------------------------------------------------
    /// (1) La mampostería YA NO se talla en Init() (regla 15/regla 47:
    ///     SimLevelBuilder pasa a tallar TODA la mampostería del plano). La
    ///     DECISIÓN de arriba ("tallada en Init") queda como ARCHIVO
    ///     HISTÓRICO -- <see cref="TallarRanura"/> sigue viva SOLO para
    ///     Reposicionar (Mudanza, runtime); <see cref="TallarEnPlano"/>
    ///     (estático, CellGrid directo) es la que llama Sim/SimLevelBuilder.cs.
    /// (2) Gramática visual (§1): la ranura gana un
    ///     <see cref="MaquinariaSprites.Embudo"/> (§1.1) y un
    ///     <see cref="MaquinariaSprites.MarcoContenedor"/> (§1.3); los dos
    ///     bornes ganan un <see cref="MaquinariaSprites.Arco"/> visible SOLO
    ///     mientras el análisis revela conductividad ≥1 (§1.4: "electrodos +
    ///     ARCO visible al analizar + lámpara en su poste" -- la lámpara ya
    ///     existía). El AFFORDANCE GLOW (§1.5, "Ranura del Banco:
    ///     MaterialId.EsBaseEstado(M)") usa el helper único
    ///     <see cref="MaquinariaSprites.AffordanceGlow"/>.
    /// </summary>
    public sealed class BancoChispa : MonoBehaviour, IMaquinaInteractiva, IMovible
    {
        private const float ProximityRange = 3.2f;

        // Contrato §5.3: "RANURA (~3x2)". (playtest 26) De privadas a
        // PÚBLICAS: SimLevelBuilder las lee para tallar la misma geometría.
        public const int RanuraAncho = 3;
        public const int RanuraAlto = 2;
        public const int MuroGrosor = 1;

        /// <summary>Cuánto tiempo se queda la lámpara encendida tras un análisis, antes de apagarse otra vez (teatro visual; no afecta al registro, que ocurre una vez al pulsar E).</summary>
        private const float BrilloDuracion = 3f;

        private AlkahestSim _sim;
        private Transform _player;
        private SubstanceKnowledge _conocimiento;

        private int _anchorX;
        private int _baseY;
        private int _ranuraX0, _ranuraX1, _ranuraY0, _ranuraY1;

        private Vector3 _centro;
        private Vector3 _centroLampara;

        private byte _ultimaConductividad; // 0/1/2 del último análisis, para el brillo y el rótulo.
        private float _brilloRestante;
        private string _chapaResultado; // "ni un parpadeo" / etc., null = sin análisis todavía.

        private SpriteRenderer _lampara;
        private SpriteRenderer _resalte;
        private float _alfaResalte;

        // ---- Playtest 26 (contrato §1): gramática visual + affordance glow ----
        private Flask _flask; // solo lectura (contrato: "sin tocar Flask.cs").
        private MaquinariaSprites.AffordanceGlow _glowRanura = new MaquinariaSprites.AffordanceGlow();
        private System.Func<byte, bool> _sirveRanura;
        private SpriteRenderer _afordanceRanura;
        private SpriteRenderer _arco; // §1.4: visible SOLO mientras _ultimaConductividad>=1 && _brilloRestante>0.

        private const float RangoEstadoPleno = 5.0f;
        private const float RangoEstadoDesvanece = 6.5f;
        private const float RangoNombrePleno = 2.6f;
        private const float RangoNombreDesvanece = 3.6f;
        private bool _yaConocida;

        public Vector3 PuntoFoco => _centro;
        public float RangoFoco => ProximityRange;

        public Vector3 CentroMundo => _centro;
        public Vector2 TamanoMundo => new Vector2(
            (RanuraAncho + 2 * MuroGrosor) * SimRenderer.CellWorldSize,
            (RanuraAlto + 2 * MuroGrosor + 2) * SimRenderer.CellWorldSize); // +2: la lámpara asoma por encima del hueco.
        public Vector2Int AnclaCelda => new Vector2Int(_ranuraX0 - MuroGrosor, _baseY);

        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            int span = RanuraAncho + 2 * MuroGrosor;
            int x0 = anclaCelda.x, x1 = x0 + span - 1;
            int yTop = anclaCelda.y + RanuraAlto + MuroGrosor + 2;
            return x0 >= 1 && x1 <= CellGrid.W - 2 && anclaCelda.y >= 1 && yTop <= CellGrid.H - 2;
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap. `anchorX` = SimLevelBuilder.BancoChispaX (contrato §4.5).</summary>
        public void Init(AlkahestSim sim, Transform player, SubstanceKnowledge conocimiento, int anchorX)
        {
            _sim = sim;
            _player = player;
            _conocimiento = conocimiento;
            _anchorX = anchorX;
            _baseY = SimLevelBuilder.CuartoY0 + 2; // contrato §4.5.
            _flask = player != null ? player.GetComponent<Flask>() : null;
            _sirveRanura = MaterialSirveRanura;

            RecalcularRegion();
            // (playtest 26) YA NO se talla aquí -- SimLevelBuilder.BuildCuartoIntimo
            // llama a TallarEnPlano. TallarRanura() sigue viva SOLO para
            // Reposicionar (Mudanza), ver el docblock de la clase.
            BuildVisual();
            UpdateLamparaTint();

            MachineFocus.Registrar(this);
            Mudanza.RegistrarMovible(this);
        }

        /// <summary>Contrato §1.5: "Ranura del Banco: MaterialId.EsBaseEstado(M)".</summary>
        private bool MaterialSirveRanura(byte mat) => MaterialId.EsBaseEstado(mat);

        private void RecalcularRegion()
        {
            _ranuraX0 = _anchorX - RanuraAncho / 2;
            _ranuraX1 = _ranuraX0 + RanuraAncho - 1;
            _ranuraY0 = _baseY + 1;
            _ranuraY1 = _ranuraY0 + RanuraAlto - 1;

            float celda = SimRenderer.CellWorldSize;
            float centroX = (_ranuraX0 + RanuraAncho * 0.5f) * celda;
            float centroY = (_baseY + (RanuraAlto + 2) * 0.5f) * celda;
            _centro = new Vector3(centroX, centroY, 0f);
            transform.position = _centro;
        }

        /// <summary>Muros de Piedra de 1 celda + suelo alrededor de la ranura, interior vaciado EN CALIENTE (ver DECISIÓN en el doc de la clase). Solo la llama ya <see cref="Reposicionar"/> (Mudanza) -- Init ya NO la llama, ver el docblock de la clase.</summary>
        private void TallarRanura()
        {
            for (int x = _ranuraX0 - MuroGrosor; x <= _ranuraX1 + MuroGrosor; x++)
            {
                _sim.PaintStable(x, _ranuraY0 - 1, 0, MaterialId.Stone);
            }
            for (int y = _ranuraY0 - 1; y <= _ranuraY1; y++)
            {
                _sim.PaintStable(_ranuraX0 - MuroGrosor, y, 0, MaterialId.Stone);
                _sim.PaintStable(_ranuraX1 + MuroGrosor, y, 0, MaterialId.Stone);
            }
            _sim.PaintRect(_ranuraX0, _ranuraY0, RanuraAncho, RanuraAlto, MaterialId.Empty);
        }

        /// <summary>
        /// (playtest 26) Talla la ranura del Banco DIRECTAMENTE sobre el
        /// CellGrid del plano -- llamado por Sim/SimLevelBuilder.cs al
        /// construir el nivel (construcción, no PaintStable/regla 29, que es
        /// para runtime). Misma geometría EXACTA que <see cref="TallarRanura"/>
        /// de instancia.
        /// </summary>
        public static void TallarEnPlano(CellGrid grid, int anchorX, int baseY)
        {
            int ranuraX0 = anchorX - RanuraAncho / 2;
            int ranuraX1 = ranuraX0 + RanuraAncho - 1;
            int ranuraY0 = baseY + 1;
            int ranuraY1 = ranuraY0 + RanuraAlto - 1;

            for (int x = ranuraX0 - MuroGrosor; x <= ranuraX1 + MuroGrosor; x++)
                if (CellGrid.InBounds(x, ranuraY0 - 1)) grid.SetCell(x, ranuraY0 - 1, MaterialId.Stone);
            for (int y = ranuraY0 - 1; y <= ranuraY1; y++)
            {
                if (CellGrid.InBounds(ranuraX0 - MuroGrosor, y)) grid.SetCell(ranuraX0 - MuroGrosor, y, MaterialId.Stone);
                if (CellGrid.InBounds(ranuraX1 + MuroGrosor, y)) grid.SetCell(ranuraX1 + MuroGrosor, y, MaterialId.Stone);
            }
            for (int y = ranuraY0; y <= ranuraY1; y++)
                for (int x = ranuraX0; x <= ranuraX1; x++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Empty);
        }

        private void OnDestroy()
        {
            MachineFocus.Olvidar(this);
            Mudanza.OlvidarMovible(this);
        }

        public void Reposicionar(Vector2Int anclaCelda)
        {
            _anchorX = anclaCelda.x - MuroGrosor + RanuraAncho / 2;
            _baseY = anclaCelda.y;
            RecalcularRegion();
            TallarRanura();
            RecalcularCentroLampara();
        }

        private void RecalcularCentroLampara()
        {
            if (_lampara == null) return;
            float celda = SimRenderer.CellWorldSize;
            _centroLampara = new Vector3(_centro.x, (_baseY + RanuraAlto + MuroGrosor + 1.5f) * celda, 0f);
            _lampara.transform.position = _centroLampara;
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return;

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && EstaEnfocada())
            {
                Analizar();
                MachineFocus.RegistrarUsoE();
            }

            if (_brilloRestante > 0f)
            {
                _brilloRestante -= Time.deltaTime;
                if (_brilloRestante < 0f) _brilloRestante = 0f;
            }

            UpdateLamparaTint();
            ActualizarResalte();
            ActualizarArco();

            // Contrato §1.5: sondeo cada ~0.25s (acumulador propio de AffordanceGlow).
            _glowRanura.Sondear(Time.deltaTime, _centro, _player, _flask, _sirveRanura);
            if (_afordanceRanura != null) _afordanceRanura.color = new Color(UiStyles.Exito.r, UiStyles.Exito.g, UiStyles.Exito.b, _glowRanura.Alfa);
        }

        /// <summary>Contrato §1.4: "electrodos + ARCO visible al analizar". El arco solo se ve mientras el último análisis reveló conductividad ≥1 Y sigue dentro de la ventana de brillo (misma condición que la lámpara) -- si no conduce, no hay arco: la ausencia es el dato.</summary>
        private void ActualizarArco()
        {
            if (_arco == null) return;
            bool visible = _ultimaConductividad >= 1 && _brilloRestante > 0f;
            if (!visible) { _arco.color = new Color(_arco.color.r, _arco.color.g, _arco.color.b, 0f); return; }

            float t = BrilloDuracion > 0f ? Mathf.Clamp01(_brilloRestante / BrilloDuracion) : 0f;
            float parpadeo = 0.7f + 0.3f * Mathf.Sin(Time.time * 18f); // arco eléctrico: parpadeo rápido, no un pulso suave de 1Hz (eso es el affordance glow).
            _arco.color = new Color(0.75f, 0.90f, 1f, t * parpadeo);
        }

        private bool EstaEnfocada() => MachineFocus.EsFoco(this, _player);

        // -----------------------------------------------------------------
        // ANÁLISIS PURO (contrato §5.3): lee el material dominante de la
        // ranura, consulta Universe.Conductividad, NO TOCA LA GRILLA, y anota
        // la observación en el diario vía el hook de C.
        // -----------------------------------------------------------------
        private void Analizar()
        {
            var universe = _sim.Universe;
            var grid = _sim.Grid;
            if (universe == null || grid == null) return;

            // Tally del material dominante de la ranura (misma técnica que
            // Prensa.AplicarPrensada: región pequeña, O(n^2) sin asignaciones,
            // corre solo al pulsar E, no en el hot path).
            byte dominanteMat = MaterialId.Empty;
            int dominanteCount = 0;
            for (int y = _ranuraY0; y <= _ranuraY1; y++)
            {
                for (int x = _ranuraX0; x <= _ranuraX1; x++)
                {
                    byte mat = grid.GetMat(x, y);
                    if (mat == MaterialId.Empty) continue;
                    int count = 0;
                    for (int y2 = _ranuraY0; y2 <= _ranuraY1; y2++)
                        for (int x2 = _ranuraX0; x2 <= _ranuraX1; x2++)
                            if (grid.GetMat(x2, y2) == mat) count++;
                    if (count > dominanteCount) { dominanteCount = count; dominanteMat = mat; }
                }
            }

            if (dominanteMat == MaterialId.Empty)
            {
                _ultimaConductividad = 0;
                _chapaResultado = "nada en la ranura";
                _brilloRestante = BrilloDuracion;
                return;
            }

            byte conductividad = universe.Conductividad(dominanteMat);
            _ultimaConductividad = conductividad;
            _brilloRestante = BrilloDuracion;

            _chapaResultado = conductividad switch
            {
                2 => "brillo pleno",
                1 => "brillo tenue",
                _ => "ni un parpadeo",
            };

            // Contrato §5.3/§6.4: hook exacto de C, instancia (no estático).
            string observacion = conductividad >= 1 ? "encendió la lámpara" : "la lámpara ni parpadeó";
            if (_conocimiento != null) _conocimiento.RegistrarObservacionPropiedad(dominanteMat, observacion);
        }

        // -----------------------------------------------------------------
        // VISUAL: chasis (ChasisPlaca) + lámpara (ResistenciasPlaca tintada de
        // blanco-azulado, brilla según _ultimaConductividad mientras
        // _brilloRestante>0) + dos bornes (Solido tintado) -- ver DECISIÓN de
        // sprites en el doc de la clase.
        // -----------------------------------------------------------------
        private void BuildVisual()
        {
            float celda = SimRenderer.CellWorldSize;
            int span = RanuraAncho + 2 * MuroGrosor;
            float ancho = span * celda;
            float altoChasis = (RanuraAlto + 2 * MuroGrosor) * celda;

            var chasisGo = new GameObject("BancoChispaChasis");
            chasisGo.transform.SetParent(transform, false);
            chasisGo.transform.position = _centro;

            _resalte = MaquinariaSprites.CrearCapa(chasisGo.transform, "Resalte", MaquinariaSprites.ChasisPlaca(span), 16,
                ancho * 1.15f, altoChasis * 1.35f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);

            // Affordance glow de la ranura (§1.5): halo MÁS GRANDE, detrás (orden 14).
            _afordanceRanura = MaquinariaSprites.CrearCapa(chasisGo.transform, "AfordanceRanura", MaquinariaSprites.Embudo(span), 14,
                ancho * 1.4f, altoChasis * 1.7f);
            _afordanceRanura.color = new Color(UiStyles.Exito.r, UiStyles.Exito.g, UiStyles.Exito.b, 0f);

            MaquinariaSprites.CrearCapa(chasisGo.transform, "Chasis", MaquinariaSprites.ChasisPlaca(span), 18, ancho, altoChasis);
            // Marco de latón (§1.3, "cubeta enmarcada"): la ranura también es un recipiente de trabajo.
            MaquinariaSprites.CrearCapa(chasisGo.transform, "MarcoLaton", MaquinariaSprites.MarcoContenedor(span), 17, ancho, altoChasis);
            // Embudo (§1.1): montado arriba, sobre la propia ranura.
            var embudoGo = new GameObject("Embudo");
            embudoGo.transform.SetParent(chasisGo.transform, false);
            embudoGo.transform.localPosition = new Vector3(0f, altoChasis * 0.5f + celda * 1.1f, 0f);
            MaquinariaSprites.CrearCapa(embudoGo.transform, "Sprite", MaquinariaSprites.Embudo(span), 21, ancho * 0.75f, celda * 2.2f);

            // Dos bornes: pequeños tacos de latón oscuro a cada lado de la ranura.
            var sprite1x1 = MaquinariaSprites.Solido();
            float bornAncho = celda * 0.6f;
            float bornAlto = altoChasis * 0.7f;
            var bornIzqGo = new GameObject("BornIzquierdo");
            bornIzqGo.transform.SetParent(transform, false);
            bornIzqGo.transform.position = _centro + new Vector3(-ancho * 0.5f + bornAncho * 0.5f, 0f, -0.01f);
            var bornIzqSr = MaquinariaSprites.CrearCapa(bornIzqGo.transform, "Sprite", sprite1x1, 19, bornAncho, bornAlto);
            bornIzqSr.color = new Color(0.42f, 0.34f, 0.20f, 1f);

            var bornDerGo = new GameObject("BornDerecho");
            bornDerGo.transform.SetParent(transform, false);
            bornDerGo.transform.position = _centro + new Vector3(ancho * 0.5f - bornAncho * 0.5f, 0f, -0.01f);
            var bornDerSr = MaquinariaSprites.CrearCapa(bornDerGo.transform, "Sprite", sprite1x1, 19, bornAncho, bornAlto);
            bornDerSr.color = new Color(0.42f, 0.34f, 0.20f, 1f);

            // Arco (§1.4): estirado horizontalmente ENTRE los dos bornes, visible
            // solo al analizar con conductividad ≥1 (ver ActualizarArco).
            var arcoGo = new GameObject("Arco");
            arcoGo.transform.SetParent(transform, false);
            arcoGo.transform.position = _centro + new Vector3(0f, bornAlto * 0.15f, -0.02f);
            _arco = MaquinariaSprites.CrearCapa(arcoGo.transform, "Sprite", MaquinariaSprites.Arco(), 20, ancho - bornAncho, celda * 1.4f);
            _arco.color = new Color(0.75f, 0.90f, 1f, 0f);

            // Lámpara: encima del chasis (reutiliza el serpentín de Resistencias
            // como filamento).
            _centroLampara = new Vector3(_centro.x, (_baseY + RanuraAlto + MuroGrosor + 1.5f) * celda, 0f);
            var lamparaGo = new GameObject("BancoChispaLampara");
            lamparaGo.transform.SetParent(transform, false);
            lamparaGo.transform.position = _centroLampara;
            _lampara = MaquinariaSprites.CrearCapa(lamparaGo.transform, "Filamento", MaquinariaSprites.ResistenciasPlaca(RanuraAncho), 20,
                ancho * 0.7f, celda * 2f);
        }

        private void UpdateLamparaTint()
        {
            if (_lampara == null) return;
            float t = BrilloDuracion > 0f ? Mathf.Clamp01(_brilloRestante / BrilloDuracion) : 0f;
            if (_ultimaConductividad == 0 || t <= 0f)
            {
                _lampara.color = new Color(0.30f, 0.30f, 0.34f, 1f); // filamento apagado: gris frío.
                return;
            }

            float pulso = 0.85f + 0.15f * Mathf.Sin(Time.time * 5f);
            float intensidad = (_ultimaConductividad == 2 ? 1f : 0.45f) * t * pulso;
            _lampara.color = new Color(0.75f + 0.25f * intensidad, 0.85f + 0.15f * intensidad, 1f, 1f);
        }

        private void ActualizarResalte()
        {
            if (_resalte == null) return;
            float objetivo = EstaEnfocada() ? 0.60f + 0.20f * Mathf.Sin(Time.time * 4f) : 0f;
            _alfaResalte = Mathf.MoveTowards(_alfaResalte, objetivo, 6f * Time.deltaTime);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, _alfaResalte);
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return;

            float cercaniaEstado = UiStyles.Cercania(_centro, _player, RangoEstadoPleno, RangoEstadoDesvanece);
            float cercaniaNombre = UiStyles.Cercania(_centro, _player, RangoNombrePleno, RangoNombreDesvanece);
            if (cercaniaEstado <= 0f && cercaniaNombre <= 0f) return;
            if (!_yaConocida && cercaniaNombre >= 0.98f) _yaConocida = true;

            UiStyles.Preparar();

            if (_chapaResultado != null && _brilloRestante > 0f)
            {
                Color color = _ultimaConductividad >= 1 ? UiStyles.Exito : UiStyles.TextoTenue;
                UiStyles.PlacaMundo(_centro, _chapaResultado,
                    new Color(color.r, color.g, color.b, color.a * cercaniaEstado), -UiStyles.S(17f));
            }

            if (!_yaConocida)
            {
                Color tenue = UiStyles.TextoTenue;
                UiStyles.PlacaMundo(_centro, "el banco de chispa", new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercaniaNombre), -UiStyles.S(34f));
            }

            if (MachineFocus.MostrarPromptE && EstaEnfocada() && !UiStyles.RatonOcupado)
            {
                UiStyles.PlacaMundo(_centro, "E — analizar",
                    new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, cercaniaNombre), -UiStyles.S(34f));
            }
        }
    }
}
