using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// [TenThousandYears · reingeniería del espacio] Fábrica de sprites generados
    /// por código para los APARATOS del taller (placa ígnea, piedra gélida,
    /// grifos, redomas de la estantería).
    ///
    /// POR QUÉ EXISTE (playtest 4): todas las máquinas eran la misma cosa —un
    /// Sprite de 1x1 píxel blanco estirado y tintado— así que ninguna "parecía
    /// nada". Aquí cada aparato tiene su propia silueta dibujada píxel a píxel:
    /// chasis remachados con resistencias naranjas, bloques de escarcha con
    /// cristales azules, caños con boquilla, y tubos de vidrio con tapón.
    ///
    /// REGLAS QUE RESPETA (ver CLAUDE.md):
    ///  · Cero assets y cero Shader.Find: todo son Texture2D generadas +
    ///    SpriteRenderer (el shader de sprites SÍ sobrevive a la build; un
    ///    Shader.Find("URP/Unlit") no — ese fue el bug del playtest 2).
    ///  · Los sprites se crean con pixelsPerUnit = 1, así que un sprite de
    ///    (w x h) píxeles mide (w x h) unidades de mundo: <see cref="CrearCapa"/>
    ///    lo escala luego al hueco exacto que le corresponde. Eso permite
    ///    dibujar la textura en la resolución que convenga al DETALLE (remaches,
    ///    facetas) sin pelearse con la escala del mundo.
    ///  · Las texturas se generan UNA vez por tamaño distinto y se cachean:
    ///    las dos cubas comparten el mismo chasis.
    /// </summary>
    public static class MaquinariaSprites
    {
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> _cache =
            new System.Collections.Generic.Dictionary<string, Sprite>(16);

        // =================================================================
        // [Playtest 26, CONTRATO_LEGIBILIDAD.md §1.5] EL AFFORDANCE GLOW —
        // IMPLEMENTACIÓN CENTRAL ÚNICA
        // =================================================================
        /// <summary>
        /// "La boca te contesta": cuando el jugador está a ≤10 celdas con el
        /// frasco cargado de un material M, la boca de una máquina PULSA
        /// suave (halo, ~1 Hz) SOLO si M le sirve a esa boca. Es la respuesta
        /// literal a la duda de Cesar en el playtest 25 ("¿meto limo en
        /// todas?"): el taller señala, no explica con texto.
        ///
        /// UNA instancia por BOCA (embudo, brasero, lecho, ranura, plinto);
        /// cada máquina posee tantas instancias como bocas tenga (el Crisol,
        /// dos: embudo + brasero) y llama a <see cref="Sondear"/> UNA vez por
        /// Update, con su propio delegado de "sirve" CACHEADO (nunca una
        /// lambda nueva por llamada -- ver el doc de <see cref="Sondear"/>).
        ///
        /// COSTE (contrato): el sondeo de proximidad+material corre cada
        /// <see cref="ProbeIntervalSeconds"/> (~0.25s, acumulador propio,
        /// JAMÁS por frame); el resultado se cachea en <see cref="Activo"/>.
        /// El pulso visual (<see cref="Alfa"/>) SÍ se recalcula cada frame,
        /// pero es solo un `Mathf.Sin(Time.time...)` -- cero allocs, cero
        /// asignaciones de string/color nuevas (el llamante decide el color
        /// final, este helper solo entrega el alfa 0..1).
        /// </summary>
        public sealed class AffordanceGlow
        {
            // (playtest 27, VEREDICTO DE CESAR sobre el playtest 26) EL PULSO
            // POR PROXIMIDAD SE APAGA: "el latido del embudo parece mucho más
            // una indicación de FUNCIONAMIENTO; podríamos usarlo como
            // indicación de funcionamiento en una versión más avanzada, de
            // momento hay que cortar su uso como aviso de proximidad". Tenía
            // razón: un pulso se lee universalmente como "esto está ENCENDIDO/
            // trabajando", no como "esto acepta lo que llevas". La clase se
            // CONSERVA entera (regla 15) porque su destino ya está decidido:
            // latir mientras la máquina TRABAJA (hornada en curso, prensada,
            // análisis). Mientras ProximidadActiva sea false, Activo nunca
            // enciende por cercanía+material.
            public const bool ProximidadActiva = false;

            public const float ProbeIntervalSeconds = 0.25f;
            private const float RangoCeldas = 10f;
            private const float PulsoHz = 1f;

            private float _timer;
            private bool _activo;

            /// <summary>¿La última pasada del sondeo dice que esta boca sirve para el material que lleva el jugador ahora mismo?</summary>
            public bool Activo => ProximidadActiva && _activo;

            /// <summary>Alfa del pulso 0..1 para este frame (seno sobre Time.time) -- 0 si <see cref="Activo"/> es false. Recalculado cada frame pero sin allocs: pura aritmética.</summary>
            public float Alfa => Activo ? (0.5f + 0.5f * Mathf.Sin(Time.time * PulsoHz * Mathf.PI * 2f)) : 0f; // Activo (no _activo): respeta el interruptor ProximidadActiva de arriba.

            /// <summary>
            /// Avanza el acumulador y, si toca sondear (~0.25s), recalcula
            /// <see cref="Activo"/>: jugador a ≤10 celdas de `bocaMundo` Y el
            /// frasco lleva un material no vacío que `sirve` acepta.
            /// Llamar UNA vez por Update de la máquina con `Time.deltaTime`
            /// real. `sirve` debe ser un delegado YA CREADO (campo cacheado
            /// en la máquina, asignado una vez en Init -- un método de
            /// instancia convertido a `Func&lt;byte,bool&gt;` asigna una sola
            /// vez, no en cada llamada) -- nunca una lambda literal aquí, o
            /// el sondeo generaría basura cada 0.25s.
            /// </summary>
            public void Sondear(float deltaTime, Vector3 bocaMundo, Transform jugador, Flask frasco, System.Func<byte, bool> sirve)
            {
                _timer += deltaTime;
                if (_timer < ProbeIntervalSeconds) return;
                _timer -= ProbeIntervalSeconds;

                if (jugador == null || frasco == null || sirve == null) { _activo = false; return; }

                float celda = SimRenderer.CellWorldSize;
                float distCeldas = Vector3.Distance(jugador.position, bocaMundo) / celda;
                if (distCeldas > RangoCeldas) { _activo = false; return; }

                byte mat = frasco.MaterialDominante();
                if (mat == MaterialId.Empty) { _activo = false; return; }

                _activo = sirve(mat);
            }

            // =============================================================
            // (playtest 27) EL DESTINO APROBADO DE ESTA CLASE: "ESTOY
            // TRABAJANDO". Cesar cerró la discusión del 26 diciendo que un
            // pulso se lee como funcionamiento, no como affordance -- así
            // que el mismo mecanismo (seno sobre Time.time, cero allocs)
            // pasa a significar EXACTAMENTE eso. Lo conduce la máquina:
            // pone `Trabajando = true` mientras corre una hornada / una
            // prensada / un análisis, y tinta su capa de trabajo con
            // <see cref="AlfaTrabajo"/>.
            //
            // Es un pulso DISTINTO del de proximidad a propósito: más
            // rápido (1.6 Hz frente a 1 Hz) y con suelo de alfa 0.35 (nunca
            // llega a apagarse del todo) -- "esto está encendido y
            // respirando", no "esto parpadea a ver si me haces caso".
            // =============================================================
            private const float PulsoTrabajoHz = 1.6f;

            /// <summary>¿La máquina dueña de esta boca está TRABAJANDO ahora mismo? Lo escribe la máquina, no el sondeo.</summary>
            public bool Trabajando;

            /// <summary>Alfa 0..1 del latido de trabajo (0 si no trabaja). Suelo 0.35: respira, no parpadea.</summary>
            public float AlfaTrabajo => Trabajando
                ? 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Time.time * PulsoTrabajoHz * Mathf.PI * 2f))
                : 0f;
        }

        /// <summary>
        /// (playtest 27, mandato 3 del CONTRATO_TALLER_GRANDE) EL ACUSE DE
        /// RECIBO. "Cuando la materia entra donde debe, la máquina lo ACUSA":
        /// un DESTELLO corto del marco, no un pulso sostenido (eso es
        /// <see cref="AffordanceGlow.AlfaTrabajo"/>, y confundir los dos era
        /// justo el error del playtest 26). Una instancia por boca; la
        /// máquina llama a <see cref="Disparar"/> cuando detecta que la
        /// cámara tiene materia que antes no tenía, y tinta el marco con
        /// <see cref="Alfa"/> cada frame (pura aritmética, cero allocs).
        /// </summary>
        public sealed class Destello
        {
            private const float DuracionSeg = 0.55f;
            private float _restante;

            public void Disparar() => _restante = DuracionSeg;

            /// <summary>Avanza el destello. Llamar una vez por Update con Time.deltaTime.</summary>
            public void Avanzar(float deltaTime)
            {
                if (_restante <= 0f) return;
                _restante -= deltaTime;
                if (_restante < 0f) _restante = 0f;
            }

            /// <summary>Alfa 0..1: sube de golpe y cae en rampa cúbica (un destello, no un fundido lineal -- misma lección que la regla 28).</summary>
            public float Alfa
            {
                get
                {
                    if (_restante <= 0f) return 0f;
                    float t = _restante / DuracionSeg;
                    return t * t * t;
                }
            }
        }

        // (fix playtest 6: baja resolución) Una celda de sim mide 0.1 unidades de
        // mundo; con la cámara acercada eso son ~7-8 px de pantalla en 1080p. Las
        // texturas de aquí se generaban a ~2 téxeles/celda (chasis, bloques) o menos
        // (caño, redomas): al escalarlas al hueco de mundo exacto (ver CrearCapa), un
        // solo téxel ocupaba varios píxeles de pantalla y los bordes de las máquinas
        // se veían "a bloques", igual de chunky que la piedra sin sillería. Escala=3
        // multiplica el lienzo de cada textura (y cada offset/periodo "de diseño"
        // calibrado a mano en la resolución original, vía el helper S) para llegar a
        // >=6 téxeles/celda en ambos ejes SIN tocar el tamaño de mundo de ningún
        // sprite (CrearCapa sigue escalando al ancho/alto que pasa el llamante) ni
        // las firmas públicas. El diseño (proporciones, remaches, cantos con luz/
        // sombra) es el mismo; solo se dibuja con más téxeles.
        private const int Escala = 3;
        private static int S(int v) => v * Escala;

        /// <summary>
        /// Instancia un sprite generado como hijo de `padre`, con orden de
        /// dibujo `orden`, escalado para ocupar exactamente `anchoMundo` x
        /// `altoMundo` unidades (los sprites se crean a 1 píxel = 1 unidad).
        /// </summary>
        public static SpriteRenderer CrearCapa(Transform padre, string nombre, Sprite sprite, int orden,
            float anchoMundo, float altoMundo)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = orden;
            go.transform.localScale = new Vector3(
                anchoMundo / sprite.rect.width,
                altoMundo / sprite.rect.height, 1f);
            return sr;
        }

        // =================================================================
        // LAS DOS PLACAS DE ZONA (playtest 49, "OPUS CON OJOS" — rediseño
        // sobre la FOTO DE REFERENCIA de Cesar)
        // =================================================================
        // EL PROBLEMA QUE RESUELVE ESTA PASADA (feedback literal del
        // playtest 48): *"no sé por qué la placa de calor y frío es el
        // mismo, parece que perdiste registro del que te pedí en la foto"*.
        // Tenía razón, y la causa era medible, no de gusto: tras el 48 las
        // dos placas compartían LITERALMENTE la misma construcción —
        // w = span*2*Escala, h = S(14), gradiente vertical de roca, juntas
        // de sillería cada S(9), banda clara en el canto superior— y solo se
        // diferenciaban por el TINTE (pardo vs. azul) y por unos detalles de
        // 2-3 téxeles (brasas tenues dentro de un nicho / dientes finos).
        // MEDIDAS REALES, LEÍDAS EN VIVO EN EL EDITOR (regla 39, no de la
        // prosa de nadie): las DOS placas miden **8 x 3 CELDAS** de mundo
        // (0.80 x 0.30 unidades). HeatPlate/ChillStone.Init recortan a
        // FootprintFraction=0.4 con SUELO de 8 celdas, y la alcoba fría solo
        // tiene 8 de ancho (SimLevelBuilder.AlcobaFriaAncho), así que las dos
        // caen en el suelo: son hermanas EXACTAS en huella. Por tanto la
        // textura sale siempre en el CLAMP MÍNIMO, 48x42 téxeles = 6
        // téxeles/celda en X y 14 en Y (de ahí toda la corrección de
        // anisotropía de este bloque). Y la cámara del juego, medida en
        // partida: ortho 5.57 sobre 1024 px de alto = **9.2 px por celda**,
        // o sea la placa entera son 73 x 27 PÍXELES en pantalla: todo lo que
        // se juegue por debajo de ~1 celda de detalle NO EXISTE para el
        // jugador (regla 52). Dos objetos con la misma silueta, el mismo
        // valor tonal y el mismo grano se leen como el mismo objeto pintado
        // de otro color — que es exactamente lo que reportó.
        //
        // CRITERIO NUEVO: la diferencia tiene que vivir en los tres canales
        // que sobreviven a 30 px, y en los tres a la vez:
        //   1. SILUETA — frío: peine de DIENTES que rompen el borde
        //      superior; calor: losa MACIZA de borde recto con dos bornes
        //      que sobresalen en los extremos.
        //   2. VALOR — frío: cuerpo CLARO (acero azulado, 0x5A6676..0xB8CCDD en el
        //      canto); calor: cuerpo OSCURO (fundición parda, ~0x2A1F18) con
        //      UNA línea incandescente dentro. Claro contra oscuro se
        //      distingue incluso en una miniatura en blanco y negro.
        //   3. GRANO — frío: ranurado VERTICAL de disipador (mecanizado);
        //      calor: sillería/fundición horizontal con nicho recesado.
        //
        // FIDELIDAD A LA FOTO DE REFERENCIA: la placa fría es "una regleta
        // metálica gris-azulada con dientes triangulares claros apuntando
        // hacia arriba" (bandeja de hielo / sierra de escarcha); la placa de
        // calor es "una losa oscura con una resistencia en zigzag
        // roja-naranja dentro y terminales naranjas en los extremos".
        //
        // ---------------------------------------------------------------
        // CRONOLOGÍA DEL SERPENTÍN (regla 27 de CLAUDE.md: LEER ANTES DE
        // VOLVER A QUITARLO — es la tercera vez que esta pieza cambia)
        // ---------------------------------------------------------------
        //  1) Playtest 4: la placa nace como chasis metálico remachado con
        //     una ventana y un serpentín en ZIGZAG de rojo PLANO.
        //  2) Playtest 47b: Cesar lo llama "la N roja horrible".
        //  3) Playtest 48: se retira ENTERO (chasis + serpentín) y se
        //     sustituye por losa de piedra con lecho de brasas.
        //  4) Playtest 49 (ESTA pasada): Cesar aclara que la queja del (2)
        //     era EL ACABADO, no el concepto — *"la idea de resistencia /
        //     estufa me gustaba"*— y que el 48 borró el ADN de su foto. El
        //     serpentín VUELVE, pero PULIDO: ya no es una polilínea de rojo
        //     saturado de 1 téxel de grosor, es un TUBO de sección elíptica
        //     con incandescencia GRADUAL horneada en la propia textura
        //     (núcleo blanco-amarillo -> cuerpo ámbar -> halo rojo profundo,
        //     ver <see cref="SerpentinPlaca"/>) y trazado sobre una SINUSOIDE
        //     muestreada a 1/4 de téxel, no sobre esquinas de 90°.
        //  LO QUE NO VUELVE del (1), y por qué (regla 15): el CHASIS
        //  METÁLICO REMACHADO. La queja de fondo del 47b/48 sobre el
        //  lenguaje visual sigue siendo cierta —el taller entero es piedra,
        //  latón y hierro forjado— así que el cuerpo se queda en FUNDICIÓN/
        //  piedra oscura y solo los BORNES son latón. Tampoco vuelve el rojo
        //  plano: el color se hornea con la misma lógica de incandescencia
        //  que usa SimRenderer para el fuego real (playtest 41: nunca un
        //  rojo sólido sin mezcla; el centro de una brasa TIENDE AL BLANCO).
        //  Y el LECHO DE BRASAS del 48 se retira de la PLACA (sigue vivo e
        //  intacto en <see cref="LechoBrasas"/>, el hogar del Crisol, que es
        //  donde una cama de brasas sí tiene sentido físico: ahí hay
        //  combustible sólido ardiendo; en una placa de zona no hay nada que
        //  arda, hay un elemento que se pone al rojo).
        // =================================================================

        /// <summary>
        /// LA LOSA DE LA PLACA DE CALOR: cuerpo de fundición/piedra oscura
        /// con un NICHO recesado a lo ancho (donde vive el serpentín, ver
        /// <see cref="SerpentinPlaca"/>) y dos BORNES de latón en los
        /// extremos —los "terminales naranjas" de la foto de referencia—,
        /// que son hardware y por eso NO se tiñen con el estado (viven en
        /// esta capa, no en la del serpentín: un borne no se enfría).
        ///
        /// Es deliberadamente lo CONTRARIO de <see cref="BloqueGelido"/> en
        /// los tres canales del criterio de arriba: valor oscuro, grano
        /// horizontal, borde superior RECTO y macizo (la cuba se apoya ahí).
        /// Comparte con ella la FÓRMULA DE TAMAÑO (w = span*2*Escala,
        /// h = S(14)) porque las dos son hermanas en proporción: mismo
        /// grosor, misma huella, aparatos de la misma familia.
        /// </summary>
        public static Sprite LosaPlaca(int spanCeldas)
        {
            string clave = "losaplaca" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Mathf.Clamp(spanCeldas * 2 * Escala, S(16), S(512));
            int h = S(14);
            var px = new Color32[w * h];

            // Fundición parda: OSCURA de arriba abajo (el canal "valor" del
            // criterio). El punto más claro de esta losa (0x5A4436, luminancia
            // ~0x48) sigue siendo más oscuro que el punto MÁS OSCURO del
            // cuerpo de la piedra gélida en su franja de acero (0x5A6676,
            // luminancia ~0x64): no hay solape tonal entre las dos, ni en su
            // pixel más favorable.
            Color32 hierroAlto = new Color32(0x5A, 0x44, 0x36, 255);
            Color32 hierroBajo = new Color32(0x1B, 0x13, 0x0F, 255);
            Color32 nicho = new Color32(0x10, 0x0A, 0x08, 255);   // boca del nicho: casi negra, para que el serpentín tenga contra qué brillar.
            Color32 filete = new Color32(0x7A, 0x54, 0x36, 255);  // canto superior TIBIO donde se apoya la cuba.
            Color32 sombra = new Color32(0x0C, 0x08, 0x06, 255);

            // Geometría del nicho y de los bornes, derivada del ancho real
            // (regla 39: nada de números fijos que dejen de encajar cuando
            // el aparato cambie de huella o cuando lo pida la réplica de red
            // con span=10).
            int margen = Mathf.Max(Escala, w / 40);
            int anchoBorne = Mathf.Clamp(w / 12, S(2), S(5));
            int nichoX0 = margen + anchoBorne + Escala;
            int nichoX1 = w - nichoX0;
            int nichoY0 = S(2), nichoY1 = S(11);

            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                Color32 fila = Color32.Lerp(hierroBajo, hierroAlto, t * t);
                for (int x = 0; x < w; x++)
                {
                    Color32 c = fila;

                    // Grano de fundición: juntas HORIZONTALES largas (una
                    // cada S(4) filas), no la sillería vertical de la piedra
                    // gélida -- el canal "grano" del criterio.
                    if ((y % S(4)) == 0) c = Color32.Lerp(c, hierroBajo, 0.45f);

                    // Nicho recesado, con bisel: una fila clara justo debajo
                    // del labio superior del nicho vende la profundidad.
                    if (y >= nichoY0 && y < nichoY1 && x >= nichoX0 && x < nichoX1)
                    {
                        c = nicho;
                        if (y >= nichoY1 - Escala) c = Color32.Lerp(nicho, hierroAlto, 0.30f);
                        if (y < nichoY0 + Escala) c = Color32.Lerp(nicho, sombra, 0.6f);
                    }

                    px[y * w + x] = c;
                }
            }

            // BORNES DE LATÓN en los dos extremos (la foto: "terminales
            // naranjas en los extremos"). Sobresalen por arriba del filete:
            // son los dos únicos puntos donde la silueta de esta placa NO es
            // una recta, y están en los extremos, que es donde el ojo los
            // encuentra sin buscarlos.
            int borneY0 = S(1), borneY1 = h - Escala;
            for (int lado = 0; lado < 2; lado++)
            {
                int bx0 = lado == 0 ? margen : w - margen - anchoBorne;
                for (int y = borneY0; y < borneY1; y++)
                {
                    float tb = (y - borneY0) / (float)Mathf.Max(1, borneY1 - borneY0 - 1);
                    Color32 cb = Color32.Lerp(LatonBajo, LatonAlto, tb * tb);
                    for (int x = bx0; x < bx0 + anchoBorne; x++)
                    {
                        if (x < 0 || x >= w) continue;
                        Color32 c = cb;
                        if (x == bx0) c = LatonBajo;                       // canto en sombra
                        else if (x == bx0 + anchoBorne - 1) c = LatonBajo;
                        px[y * w + x] = c;
                    }
                    // Anillos del borne (dos gargantas oscuras): lo que hace
                    // que se lea "poste roscado" y no "barra amarilla".
                    if (y == borneY0 + S(3) || y == borneY0 + S(6))
                        for (int x = bx0; x < bx0 + anchoBorne; x++)
                            if (x >= 0 && x < w) px[y * w + x] = Laton;
                }
                MarcarRemache(px, w, h, bx0 + anchoBorne / 2, borneY1 - Escala, LatonAlto);
            }

            // Filete de contacto (canto superior) y sombra de apoyo. Van al
            // final para que pisen a la fundición pero NO a los bornes: el
            // filete se dibuja solo entre los dos bornes.
            for (int y = h - S(2); y < h; y++)
                for (int x = margen + anchoBorne; x < w - margen - anchoBorne; x++)
                    px[y * w + x] = filete;
            for (int y = 0; y < S(1); y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = sombra;

            s = Crear(px, w, h, "TenThousandYearsLosaPlaca");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// EL SERPENTÍN DE LA PLACA DE CALOR (playtest 49; sustituye al
        /// <c>LechoBrasasPlaca</c> del playtest 48, ver la CRONOLOGÍA DEL
        /// SERPENTÍN arriba): la resistencia de estufa de la foto de
        /// referencia, pero PULIDA.
        ///
        /// CÓMO SE PULE una resistencia dibujada por código, que era la queja
        /// real de Cesar ("horrible" el zigzag CRUDO, no la idea):
        ///  · TRAZADO CONTINUO, no polilínea. La línea guía es una SINUSOIDE
        ///    muestreada cada 1/4 de téxel, así que no hay esquinas de 90°
        ///    ni escalones de aliasing: el tubo curva.
        ///  · SECCIÓN ELÍPTICA CORREGIDA POR ANISOTROPÍA. La textura tiene
        ///    ~6 téxeles por celda en X y ~14 en Y (w = span*2*Escala sobre
        ///    span celdas; h = S(14)=42 sobre WallThickness=3 celdas), así
        ///    que un pincel CIRCULAR en téxeles saldría 2,33x más ancho que
        ///    alto en el mundo -- una mancha, no un tubo. El pincel es una
        ///    elipse con <c>ry = 2.33 * rx</c> para que el tubo salga
        ///    REDONDO en pantalla.
        ///  · INCANDESCENCIA HORNEADA EN LA TEXTURA, no un tinte plano. El
        ///    llamante tiñe la capa entera con UN color por estado
        ///    (Game/HeatPlate.cs::ColorResistencia), así que si la textura
        ///    fuera blanca el resultado sería un rojo uniforme -- justo el
        ///    defecto del zigzag viejo. Aquí el gradiente va DENTRO:
        ///    núcleo casi blanco (255,252,244) -> cuerpo ámbar (255,214,150)
        ///    -> halo rojo (255,120,45) con alfa en caída. Multiplicado por
        ///    el tinte ARDIENTE (1, 0.52, 0.22) da núcleo naranja pálido y
        ///    borde rojo profundo: incandescencia real, misma lógica que la
        ///    brasa de SimRenderer (playtest 41), nunca neón.
        ///  · BLOOM DE CALOR: una segunda pasada mucho más ancha y de alfa
        ///    bajísima (<=52) que solo escribe donde no hay tubo. Es lo que
        ///    hace que el nicho entero parezca caliente en vez de contener
        ///    un alambre. Vive en ESTA capa, así que late con la misma
        ///    animación que el tubo: cero coste de animación aparte.
        ///
        /// El serpentín arranca y termina EN LOS BORNES de
        /// <see cref="LosaPlaca"/> (tramos rectos fuera del nicho, con la
        /// fase de la sinusoide clavada a 0 en los extremos), así que el
        /// circuito se lee completo: borne -> resistencia -> borne.
        /// </summary>
        public static Sprite SerpentinPlaca(int spanCeldas)
        {
            string clave = "serpentinplaca" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Mathf.Clamp(spanCeldas * 2 * Escala, S(16), S(512));
            int h = S(14);
            var px = new Color32[w * h];   // transparente por defecto.

            // MISMA geometría que talla LosaPlaca (duplicada a propósito, en
            // dos funciones estáticas puras: pasar un struct de layout entre
            // ellas costaría una asignación por llamada y estas dos SIEMPRE
            // se piden con el mismo span desde Game/HeatPlate.cs::BuildVisual).
            int margen = Mathf.Max(Escala, w / 40);
            int anchoBorne = Mathf.Clamp(w / 12, S(2), S(5));
            int nichoX0 = margen + anchoBorne + Escala;
            int nichoX1 = w - nichoX0;

            float xIni = margen + anchoBorne * 0.5f;      // centro del borne izquierdo
            float xFin = w - margen - anchoBorne * 0.5f;  // centro del borne derecho
            float centroY = (S(2) + S(11)) * 0.5f;

            // Número ENTERO de periodos dentro del nicho: así la sinusoide
            // entra y sale con fase 0 y empalma sin codo con los tramos
            // rectos que van a los bornes.
            int anchoNicho = Mathf.Max(1, nichoX1 - nichoX0);
            int periodos = Mathf.Clamp(Mathf.RoundToInt(anchoNicho / (float)S(6)), 2, 40);
            float periodo = anchoNicho / (float)periodos;

            // Amplitud y grosor: la amplitud llena el nicho sin tocar sus
            // labios (nicho = 27 téxeles de alto; amplitud 7 + ry 5.5 = 12.5
            // desde el centro, deja ~1 téxel de aire arriba y abajo).
            float amp = h / 5.6f;           // 7.5 téxeles = 0.54 celdas de mundo.
            float rx = Escala * 0.68f;      // 2.04 téxeles = 0.34 celdas de radio en X.
            float ry = rx * ((h / 3f) / (2f * Escala)); // x2.33: corrección de anisotropía -> tubo REDONDO en pantalla.

            // Búfer de INTENSIDAD (0..1 por téxel): el trazado pasa muchas
            // veces por el mismo píxel (muestreo a 1/4 de téxel) y lo que
            // vale es el MÁXIMO, no el último. Sin este búfer un lóbulo
            // exterior de una muestra posterior pisaría el núcleo ya escrito
            // por otra -- el tubo saldría moteado.
            var inten = new float[w * h];

            // --- Pasada 1: el TUBO incandescente ---
            RecorrerSerpentin(px, inten, w, h, xIni, xFin, nichoX0, nichoX1, centroY, amp, periodo, rx, ry, false);
            // --- Pasada 2: BLOOM ancho, SOLO donde no hay tubo ---
            RecorrerSerpentin(px, inten, w, h, xIni, xFin, nichoX0, nichoX1, centroY, amp, periodo, rx * 3.2f, ry * 2.4f, true);

            s = Crear(px, w, h, "TenThousandYearsSerpentinPlaca");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// Recorre la línea guía del serpentín muestreando cada 1/4 de téxel
        /// y estampa un pincel elíptico con caída cuadrática. `bloom` a true
        /// dibuja el velo de calor (alfa baja, NO pisa lo ya escrito); a
        /// false dibuja el tubo (siempre gana). Método privado de una sola
        /// llamada por pasada: sin asignaciones (todo aritmética sobre el
        /// array que le pasan).
        /// </summary>
        private static void RecorrerSerpentin(Color32[] px, float[] inten, int w, int h,
            float xIni, float xFin, int nichoX0, int nichoX1,
            float centroY, float amp, float periodo, float rx, float ry, bool bloom)
        {
            int radX = Mathf.CeilToInt(rx), radY = Mathf.CeilToInt(ry);
            for (float fx = xIni; fx <= xFin; fx += 0.25f)
            {
                // Fuera del nicho (tramos que van a los bornes) la guía es
                // RECTA: la fase se clava a 0, que es justo el valor de la
                // sinusoide en los dos bordes del nicho -> empalme sin codo.
                float fase = Mathf.Clamp(fx, nichoX0, nichoX1) - nichoX0;
                float cy = centroY + amp * Mathf.Sin(fase / periodo * Mathf.PI * 2f);

                int cx = Mathf.RoundToInt(fx);
                for (int dy = -radY; dy <= radY; dy++)
                {
                    int yy = Mathf.RoundToInt(cy) + dy;
                    if (yy < 0 || yy >= h) continue;
                    for (int dx = -radX; dx <= radX; dx++)
                    {
                        int xx = cx + dx;
                        if (xx < 0 || xx >= w) continue;

                        float ex = (xx - fx) / rx, ey = (yy - cy) / ry;
                        float d2 = ex * ex + ey * ey;
                        if (d2 >= 1f) continue;
                        float i = 1f - d2;               // 0 en el borde, 1 en el eje.

                        int idx = yy * w + xx;
                        if (i <= inten[idx]) continue;   // ver el búfer de intensidad en SerpentinPlaca.

                        if (bloom)
                        {
                            if (px[idx].a != 0 && inten[idx] > 0f) continue; // el tubo manda.
                            inten[idx] = i;
                            px[idx] = new Color32(255, 132, 52, (byte)(i * 52f));
                            continue;
                        }
                        inten[idx] = i;

                        Color32 c;
                        // Tres bandas de incandescencia (calibradas mirando el
                        // sprite renderizado a escala de juego, no a ojo sobre
                        // el hex): NÚCLEO casi blanco muy fino, CUERPO ámbar y
                        // un RIBETE rojo profundo ancho -- el ribete es lo que
                        // vende "metal al rojo" en vez de "línea naranja".
                        if (i >= 0.82f)
                        {
                            float k = (i - 0.82f) / 0.18f;
                            c = new Color32(255, (byte)(232 + 20 * k), (byte)(196 + 52 * k), 255);
                        }
                        else if (i >= 0.38f)
                        {
                            float k = (i - 0.38f) / 0.44f;
                            c = new Color32(255, (byte)(136 + 96 * k), (byte)(48 + 148 * k), 255);
                        }
                        else
                        {
                            float k = i / 0.38f;
                            c = new Color32(255, (byte)(104 + 32 * k), (byte)(30 + 18 * k), (byte)(70f + 185f * k));
                        }
                        px[idx] = c;
                    }
                }
            }
        }

        // =================================================================
        // PIEDRA GÉLIDA
        // =================================================================

        /// <summary>
        /// EL CUERPO DE LA PLACA FRÍA (rediseñado en el playtest 49, ver el
        /// bloque "LAS DOS PLACAS DE ZONA" arriba): la REGLETA METÁLICA
        /// gris-azulada de la foto de referencia. Tres franjas de abajo
        /// arriba:
        ///  · sombra de apoyo (2 téxeles),
        ///  · CUERPO DE ACERO FRÍO con ranurado VERTICAL de disipador —
        ///    claro (0x5A6676 en la base, 0xB8CCDD en el canto), que es el canal "valor"
        ///    que la separa de la losa parda de <see cref="LosaPlaca"/>: la
        ///    fría es el objeto CLARO del taller, la de calor el OSCURO;
        ///  · LABIO DE ESCARCHA (blanco azulado) y, por encima, la GARGANTA
        ///    en penumbra de donde brotan los dientes
        ///    (<see cref="CristalesGelidos"/>). La garganta es OSCURA a
        ///    propósito: sin ella los dientes blancos no tendrían contra qué
        ///    recortarse y volveríamos al problema del playtest 48.
        ///
        /// Ya NO usa la sillería vertical de piedra del 48 (idéntica a la de
        /// la placa de calor, que es lo que hacía que se leyeran iguales):
        /// el ranurado es MECANIZADO —surco oscuro + filo brillante, como
        /// las aletas de un disipador— y corre en el sentido contrario a las
        /// juntas horizontales de la fundición hermana.
        /// </summary>
        public static Sprite BloqueGelido(int spanCeldas)
        {
            string clave = "gelido" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            // MISMA fórmula de tamaño que LosaPlaca: hermanas en proporción.
            int w = Mathf.Clamp(spanCeldas * 2 * Escala, S(16), S(512));
            int h = S(14);
            var px = new Color32[w * h];

            // CALIBRADO CONTRA CAPTURA EN VIVO (regla 52): con aceroBajo en
            // 0x39424F y curva cuadrática, los 9 px de cuerpo que quedan bajo
            // los dientes salían MÁS OSCUROS que la roca del taller y la placa
            // fría perdía justo el canal que la separa de su hermana (el
            // VALOR). El suelo del degradado sube a 0x5A6676 y la curva pasa a
            // LINEAL: la mitad inferior de la placa se queda en tonos medios
            // claros de punta a punta.
            Color32 aceroAlto = new Color32(0xB8, 0xCC, 0xDD, 255);
            Color32 aceroBajo = new Color32(0x5A, 0x66, 0x76, 255);
            Color32 surco = new Color32(0x24, 0x2C, 0x38, 255);
            Color32 filo = new Color32(0xC6, 0xD8, 0xE8, 255);
            Color32 labio = new Color32(0xB4, 0xCB, 0xDE, 255);     // escarcha del labio (por DEBAJO de la vena tintada de los dientes: la que 'enciende' con el estado es esa, no esta)
            Color32 garganta = new Color32(0x26, 0x31, 0x40, 255);  // penumbra de donde brotan los dientes (azul, NUNCA negra: un hueco negro se lee como un agujero en el sprite)
            Color32 sombra = new Color32(0x0E, 0x12, 0x18, 255);

            int cuerpoY1 = S(7);            // techo del acero (21 de 42 téxeles = media placa)
            int labioY1 = cuerpoY1 + S(1);  // 3 téxeles de labio de escarcha (21..23)
            int pasoAleta = Mathf.Max(2, Escala + 1); // ranurado cada 4 téxeles = 0.67 celdas: MECANIZADO fino.
                                                      // A S(3)=9 (1.5 celdas) el ranurado salía tan grueso que
                                                      // volvía a leerse como SILLERÍA -- exactamente el defecto
                                                      // del playtest 48 que esta ronda vino a corregir. Medido en
                                                      // vivo: la placa real mide 8 CELDAS (ver la nota de tamaño
                                                      // en el docblock), o sea 5 bloques de 1.5 celdas. Cinco
                                                      // bloques es mampostería; doce surcos finos es metal.

            for (int y = 0; y < h; y++)
            {
                float t = Mathf.Clamp01(y / (float)Mathf.Max(1, cuerpoY1 - 1));
                Color32 fila = Color32.Lerp(aceroBajo, aceroAlto, t);
                for (int x = 0; x < w; x++)
                {
                    Color32 c;
                    if (y >= labioY1)
                    {
                        // Garganta: se oscurece hacia arriba (el fondo del
                        // hueco donde crece la escarcha).
                        float tg = (y - labioY1) / (float)Mathf.Max(1, h - labioY1 - 1);
                        c = Color32.Lerp(garganta, sombra, tg * 0.75f);
                    }
                    else if (y >= cuerpoY1)
                    {
                        c = labio;
                        if (y == cuerpoY1) c = Color32.Lerp(labio, aceroAlto, 0.45f);
                    }
                    else
                    {
                        c = fila;
                        int m = x % pasoAleta;
                        // Contraste BAJO a propósito: subido (0.85/0.55, como
                        // salió la primera pasada) el ranurado se comía la
                        // placa y se leía como una empalizada de barrotes que
                        // competía con los dientes. Aquí es textura, no forma.
                        if (m == 0) c = Color32.Lerp(c, surco, 0.38f);            // surco
                        else if (m == 1) c = Color32.Lerp(c, filo, 0.14f);        // filo iluminado de la aleta
                    }

                    if (y < S(1)) c = sombra;
                    px[y * w + x] = c;
                }
            }

            s = Crear(px, w, h, "TenThousandYearsBloqueGelido");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// LOS DIENTES DE ESCARCHA (rediseñados en el playtest 49): el rasgo
        /// que la foto de referencia pide literalmente —"dientes triangulares
        /// claros apuntando hacia arriba, como sierra de escarcha"— y el
        /// único de las dos placas que ROMPE LA SILUETA. Por eso son grandes:
        /// ~1.43 celdas de alto y ~1.0 de ancho en la base (13 x 9 px a
        /// escala de juego), no los 2-3 téxeles del playtest 48, que a 9 px
        /// por celda simplemente no llegaban al ojo (regla 52).
        ///
        /// Cada diente es un PRISMA, no un triángulo plano: cara izquierda
        /// casi blanca, cuerpo azul hielo, cara derecha en sombra. Tres
        /// alturas alternas (1.00 / 0.62 / 0.82) para que el peine se lea
        /// como escarcha crecida y no como una cremallera. Las proporciones
        /// están CORREGIDAS POR ANISOTROPÍA igual que el serpentín hermano
        /// (~6 téxeles/celda en X contra ~14 en Y): la base mide 6 téxeles y
        /// la altura 20, que en el mundo son 1.0 x 1.43 celdas -- un
        /// triángulo, no una aguja aplastada.
        ///
        /// Encima del labio se siembra ESCARCHA menuda (téxeles sueltos de
        /// alfa baja, hash determinista de la posición -- nunca
        /// UnityEngine.Random, regla de oro del proyecto) para que el borde
        /// no sea una línea perfecta de fábrica.
        /// </summary>
        public static Sprite CristalesGelidos(int spanCeldas)
        {
            string clave = "cristales" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Mathf.Clamp(spanCeldas * 2 * Escala, S(16), S(512));
            int h = S(14);
            var px = new Color32[w * h];

            int baseY = S(7);                        // arranca en el labio de BloqueGelido (misma fila)
            int alturaMax = h - baseY - 1;           // 20 téxeles = 1.43 celdas de mundo
            int semiBase = Mathf.Max(2, Escala);              // 3 téxeles -> base de 6 = 1.0 celda
            int paso = Mathf.Max(semiBase * 2 + 2, semiBase * 8 / 3); // 8 téxeles = 1.33 celdas entre puntas
            // CALIBRADO CONTRA LA MEDIDA REAL, no contra la prosa (regla 39):
            // el aparato mide 8 CELDAS de ancho (Init lo recorta a
            // FootprintFraction con suelo de 8, y la alcoba fría solo tiene 8
            // -- SimLevelBuilder.AlcobaFriaAncho), así que la textura sale
            // SIEMPRE en el clamp mínimo, 48x42 téxeles. Con base 8 y paso 12
            // (primer intento de esta ronda) salían CUATRO dientes en toda la
            // placa y se leían como cuatro montañas; con base 6 y paso 8 salen
            // SEIS y ya se lee "sierra". A 9.2 px/celda medidos en vivo eso es
            // un diente de 9 x 13 px: por encima del umbral de la regla 52.

            for (int cx = paso / 2; cx < w - semiBase; cx += paso)
            {
                int idx = cx / paso;
                float f = (idx % 3 == 0) ? 1f : (idx % 3 == 1 ? 0.86f : 0.93f);  // irregularidad LEVE: la foto pide una sierra, no una cresta de montañas.
                int altura = Mathf.Max(3, Mathf.RoundToInt(alturaMax * f));

                for (int y = 0; y < altura; y++)
                {
                    int semi = Mathf.RoundToInt(semiBase * (1f - y / (float)altura));
                    int yy = baseY + y;
                    if (yy >= h) break;
                    for (int dx = -semi; dx <= semi; dx++)
                    {
                        int x = cx + dx;
                        if (x < 0 || x >= w) continue;

                        Color32 c;
                        if (semi > 0 && dx == -semi)
                            c = new Color32(0xFA, 0xFD, 0xFF, 255);              // arista iluminada
                        else if (semi > 0 && dx == semi)
                            c = new Color32(0x8E, 0xB6, 0xD6, 210);              // cara en sombra
                        else if (dx < 0)
                            c = new Color32(0xE4, 0xF2, 0xFC, 250);              // cara al sol
                        else
                            c = new Color32(0xB6, 0xD6, 0xEE, 235);              // cara opuesta

                        px[yy * w + x] = c;
                    }
                }
            }

            // Vena que une los dientes por la base: el peine tiene que
            // leerse como UNA pieza (escarcha corrida), no como dientes
            // sueltos flotando sobre el labio.
            for (int t = 0; t < Escala; t++)
            {
                int y = baseY + t;
                if (y < 0 || y >= h) continue;
                for (int x = Escala; x < w - Escala; x++)
                    if (px[y * w + x].a == 0)
                        px[y * w + x] = new Color32(0xDC, 0xEC, 0xF8, (byte)(200 - t * 45));
            }

            // ESCARCHA MENUDA determinista sobre la garganta (hash de la
            // posición, cero UnityEngine.Random).
            for (int y = baseY + Escala; y < h; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    if (px[y * w + x].a != 0) continue;
                    uint hash = (uint)(x * 73856093 ^ y * 19349663);
                    if ((hash % 29u) != 0u) continue;
                    px[y * w + x] = new Color32(0xE8, 0xF4, 0xFF, (byte)(60 + hash % 55u));
                }
            }

            s = Crear(px, w, h, "TenThousandYearsCristalesGelidos");
            _cache[clave] = s;
            return s;
        }

        // =================================================================
        // GRIFO
        // =================================================================

        /// <summary>Caño de pared: brida atornillada al pilar + tubo + boquilla acampanada mirando a la derecha.</summary>
        public static Sprite CanoGrifo()
        {
            const string clave = "cano";
            if (_cache.TryGetValue(clave, out var s)) return s;

            // (fix playtest 6: baja resolución) el caño NO se parametriza por
            // spanCeldas -- su tamaño de mundo lo fija Dispenser.cs a 8x5 celdas
            // fijas, así que 34x20 px daba ~4.25/4 téxeles por celda. Escalado por
            // Escala llega a ~12/cell, muy por encima del mínimo de 6.
            int w = S(34), h = S(20);
            var px = new Color32[w * h];

            // Latón CLARO a propósito (playtest 5): el caño vive pegado a un
            // pilar de piedra oscura en el borde izquierdo de la pantalla; con la
            // paleta anterior (0x8A6C36) se fundía con el fondo y el jugador no
            // encontraba los grifos.
            Color32 latonAlto = new Color32(0xF2, 0xD3, 0x8C, 255);
            Color32 laton = new Color32(0xBD, 0x93, 0x47, 255);
            Color32 latonBajo = new Color32(0x70, 0x56, 0x2A, 255);

            // Brida vertical pegada al muro (izquierda).
            for (int y = S(2); y < h - S(2); y++)
                for (int x = 0; x < S(6); x++)
                    px[y * w + x] = (x < S(2)) ? latonBajo : (y > h / 2 ? latonAlto : laton);

            // Tubo horizontal: filete de luz en el canto superior (fila 8 del
            // diseño original), sombra en el canto inferior (fila 12).
            for (int y = S(8); y < S(13); y++)
                for (int x = S(5); x < w - S(8); x++)
                    px[y * w + x] = (y < S(9)) ? latonAlto : ((y >= S(12)) ? latonBajo : laton);

            // Codo + boquilla acampanada que mira hacia ABAJO (por ahí cae el caudal).
            for (int y = S(3); y < S(13); y++)
                for (int x = w - S(10); x < w - S(4); x++)
                    px[y * w + x] = (x < w - S(10) + Escala) ? latonAlto : laton;
            for (int oldY = 0; oldY <= 4; oldY++)
            {
                int ancho = S(3 + (4 - oldY) / 2);
                for (int sub = 0; sub < Escala; sub++)
                {
                    int y = S(oldY) + sub;
                    for (int dx = -ancho; dx <= ancho; dx++)
                    {
                        int x = w - S(7) + dx;
                        if (x < 0 || x >= w) continue;
                        px[y * w + x] = (dx <= -ancho + Escala) ? latonAlto : ((dx >= ancho - Escala) ? latonBajo : laton);
                    }
                }
            }

            // Volante de apertura sobre el tubo: aro de luz alrededor del disco oscuro.
            for (int y = S(13); y < S(18); y++)
                for (int x = S(9); x < S(16); x++)
                {
                    bool borde = (y < S(14) || y >= S(17) || x < S(10) || x >= S(15));
                    px[y * w + x] = borde ? latonAlto : latonBajo;
                }

            s = Crear(px, w, h, "TenThousandYearsCanoGrifo");
            _cache[clave] = s;
            return s;
        }

        // =================================================================
        // REDOMAS (Game/StorageRack.cs)
        // =================================================================

        /// <summary>Vidrio de la redoma: paredes translúcidas, cuello y reflejo. El contenido se dibuja DETRÁS (orden menor).</summary>
        public static Sprite VidrioRedoma()
        {
            const string clave = "vidrio";
            if (_cache.TryGetValue(clave, out var s)) return s;

            // (fix playtest 6: baja resolución) sin spanCeldas: se escala el lienzo
            // entero por Escala y se re-deriva cada fila a partir de su fila
            // "de diseño" original (oldY = y/Escala) -- un nearest-neighbor fiel a
            // mano que preserva exactamente el contorno del cuerpo panzudo/cuello.
            int w = S(22), h = S(54);
            var px = new Color32[w * h];

            Color32 vidrio = new Color32(0xCB, 0xE4, 0xEE, 90);
            Color32 cantoVidrio = new Color32(0xE8, 0xF6, 0xFF, 165);
            Color32 reflejo = new Color32(255, 255, 255, 130);

            for (int y = 0; y < h; y++)
            {
                int oldY = y / Escala;
                // Cuerpo panzudo abajo (oldY<38), cuello estrecho arriba.
                int semi = oldY < 38 ? 10 : 5;
                if (oldY < 4) semi = 8 - (3 - oldY);            // base redondeada
                if (oldY >= 38 && oldY < 42) semi = 10 - (oldY - 38); // hombro cónico
                semi = S(semi);

                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    bool pared = Mathf.Abs(dx) >= semi - Escala || y == 0;
                    px[y * w + x] = pared ? cantoVidrio : vidrio;
                }
            }

            // Reflejo vertical en el tercio izquierdo del cuerpo.
            for (int y = S(6); y < S(36); y++)
            {
                int oldY = y / Escala;
                int x = w / 2 - S(6);
                px[y * w + x] = reflejo;
                if (oldY % 7 != 0 && x + Escala < w) px[y * w + x + Escala] = new Color32(255, 255, 255, 60);
            }

            s = Crear(px, w, h, "TenThousandYearsVidrioRedoma");
            _cache[clave] = s;
            return s;
        }

        /// <summary>Máscara del CONTENIDO de la redoma: blanco opaco con la silueta interior del vidrio; se tinta con el material y se recorta por altura desde abajo.</summary>
        public static Sprite ContenidoRedoma()
        {
            const string clave = "contenido";
            if (_cache.TryGetValue(clave, out var s)) return s;

            // (fix playtest 6: baja resolución) mismo w/h y misma técnica de
            // oldY = y/Escala que VidrioRedoma -- el margen de 1 unidad "de diseño"
            // respecto al vidrio (9/4/7 vs. 10/5/8) se conserva exacto para que el
            // contenido siga encajando justo dentro del cristal.
            int w = S(22), h = S(54);
            var px = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                int oldY = y / Escala;
                int semi = oldY < 38 ? 9 : 4;
                if (oldY < 4) semi = 7 - (3 - oldY);
                if (oldY >= 38 && oldY < 42) semi = 9 - (oldY - 38);
                semi = S(semi);

                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    px[y * w + x] = new Color32(255, 255, 255, 255);
                }
            }

            s = Crear(px, w, h, "TenThousandYearsContenidoRedoma");
            _cache[clave] = s;
            return s;
        }

        /// <summary>Tapón de corcho de la redoma.</summary>
        public static Sprite TaponRedoma()
        {
            const string clave = "tapon";
            if (_cache.TryGetValue(clave, out var s)) return s;

            // (fix playtest 6: baja resolución) misma técnica oldY = y/Escala.
            int w = S(14), h = S(10);
            var px = new Color32[w * h];

            Color32 corcho = new Color32(0xA8, 0x7A, 0x48, 255);
            Color32 corchoAlto = new Color32(0xC9, 0x9A, 0x62, 255);
            Color32 corchoBajo = new Color32(0x6E, 0x4C, 0x2A, 255);

            for (int y = 0; y < h; y++)
            {
                int oldY = y / Escala;
                // Tronco de cono invertido: más ancho arriba.
                int semi = S(4 + oldY / 4);
                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    px[y * w + x] = (y >= h - S(2)) ? corchoAlto : (dx >= semi - Escala ? corchoBajo : corcho);
                }
            }

            s = Crear(px, w, h, "TenThousandYearsTaponRedoma");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// (R93, Cesar: "cuando le entrega su frasco al personaje le entrega
        /// el diseño de la redoma antigua; debería entregarle algo que se
        /// parezca más a lo que lleva en la mano") EL TARRO DE MANO: el
        /// frasco que el TOMA. hace volar de la mesa a tu mano, dibujado con
        /// la MISMA silueta que el tarro decorativo del aprendiz
        /// (ApprenticeController.GenerateCarriedFlaskTexture: panza redonda
        /// de vidrio, licor ámbar abajo, tapa de latón) — lo que recibes ES
        /// lo que llevarás.
        /// </summary>
        public static Sprite TarroDeMano()
        {
            const string clave = "tarroDeMano";
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = S(16), h = S(21);
            var px = new Color32[w * h];

            // La paleta del tarro del aprendiz (ColGlass/ColLiquid/ColBrass*).
            Color32 vidrio = new Color32(0xBD, 0xD8, 0xE4, 150);
            Color32 canto = new Color32(0xE4, 0xF2, 0xFB, 200);
            Color32 licor = new Color32(0xE0, 0xA8, 0x4E, 235);
            Color32 latonAlto = new Color32(0xD8, 0xB0, 0x6A, 255);
            Color32 laton = new Color32(0xA8, 0x7E, 0x3A, 255);
            Color32 latonBajo = new Color32(0x6E, 0x50, 0x24, 255);
            Color32 reflejo = new Color32(255, 255, 255, 140);

            float cx = w * 0.5f - 0.5f;
            float cyPanza = S(8), rx = 6.4f * Escala, ry = 7.6f * Escala;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - cx) / rx, ny = (y - cyPanza) / ry;
                    float d = nx * nx + ny * ny;
                    if (d > 1f) continue;
                    bool pared = d > 0.72f;
                    // El licor ámbar reposa en el tercio bajo de la panza.
                    px[y * w + x] = pared ? canto : (y <= S(6) ? licor : vidrio);
                }
            // La tapa de latón (tronco corto, más ancha arriba — como la del aprendiz).
            for (int y = h - S(5); y < h; y++)
            {
                float t = (y - (h - S(5))) / (float)S(4);
                int semi = Mathf.RoundToInt(Mathf.Lerp(3.2f * Escala, 2.2f * Escala, 1f - t));
                for (int x = Mathf.RoundToInt(cx) - semi; x <= Mathf.RoundToInt(cx) + semi; x++)
                {
                    if (x < 0 || x >= w) continue;
                    px[y * w + x] = y >= h - S(1) ? latonAlto : (y <= h - S(4) ? latonBajo : laton);
                }
            }
            // El reflejo del vidrio, tercio izquierdo.
            for (int y = S(4); y < S(12); y++)
            {
                int x = Mathf.RoundToInt(cx) - S(3);
                if (x >= 0 && x < w && px[y * w + x].a > 0) px[y * w + x] = reflejo;
            }

            s = Crear(px, w, h, "TenThousandYearsTarroDeMano");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// (R93, la OBRA del prólogo) EL CINCEL como pieza de regalo: la
        /// herramienta que el Maestro hace volar a tu mano antes del ALZA. —
        /// hoja de acero gris con filo claro, virola de latón y mango oscuro.
        /// Misma familia material que el resto del taller (latón + piedra).
        /// </summary>
        public static Sprite CincelHerramienta()
        {
            const string clave = "cincelHerr";
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = S(8), h = S(22);
            var px = new Color32[w * h];

            Color32 acero = new Color32(0x9A, 0xA4, 0xAA, 255);
            Color32 aceroAlto = new Color32(0xC8, 0xD2, 0xD8, 255);
            Color32 filo = new Color32(0xE8, 0xEF, 0xF4, 255);
            Color32 laton = new Color32(0xA8, 0x7E, 0x3A, 255);
            Color32 latonAlto = new Color32(0xD8, 0xB0, 0x6A, 255);
            Color32 mango = new Color32(0x4A, 0x36, 0x22, 255);
            Color32 mangoAlto = new Color32(0x6E, 0x50, 0x30, 255);

            int cx = w / 2;
            for (int y = 0; y < h; y++)
            {
                int semi;
                Color32 c;
                if (y < S(3)) { semi = S(2) - (S(3) - 1 - y) / 2; c = filo; }                    // la punta: cuña que se afila.
                else if (y < S(11)) { semi = S(2); c = acero; }                                  // la hoja.
                else if (y < S(14)) { semi = S(3); c = laton; }                                  // la virola.
                else { semi = S(2); c = mango; }                                                // el mango.
                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = cx + dx;
                    if (x < 0 || x >= w) continue;
                    Color32 final = c;
                    if (dx == -semi)
                    {
                        // Luz de canto por el flanco izquierdo, por familia.
                        if (y < S(11)) final = aceroAlto;
                        else if (y < S(14)) final = latonAlto;
                        else final = mangoAlto;
                    }
                    px[y * w + x] = final;
                }
            }

            s = Crear(px, w, h, "TenThousandYearsCincelHerramienta");
            _cache[clave] = s;
            return s;
        }

        /// <summary>Listón de madera del estante con anillos de latón: el mueble donde se apoyan las redomas.</summary>
        public static Sprite ListonEstante(int anchoPx)
        {
            string clave = "liston" + anchoPx;
            if (_cache.TryGetValue(clave, out var s)) return s;

            // (fix playtest 6: baja resolución) el ANCHO en téxeles lo fija el
            // llamante (StorageRack.cs, no editable) como texelesMundo*20 -- no
            // podemos subir su densidad desde aquí sin tocar esa firma/llamada. La
            // ALTURA sí es enteramente interna: h=10 para 0.24 unidades de mundo
            // (~2.4 celdas) daba ~4.2 téxeles/celda; se escala por Escala.
            int w = Mathf.Clamp(anchoPx, 16, 512);
            int h = S(10);
            var px = new Color32[w * h];

            Color32 maderaAlta = new Color32(0x5A, 0x3F, 0x2C, 255);
            Color32 madera = new Color32(0x3E, 0x2B, 0x1E, 255);
            Color32 maderaBaja = new Color32(0x24, 0x18, 0x12, 255);

            for (int y = 0; y < h; y++)
            {
                Color32 fila = y >= h - S(2) ? maderaAlta : (y < S(2) ? maderaBaja : madera);
                for (int x = 0; x < w; x++)
                {
                    // Veta: rayas largas y suaves.
                    bool veta = ((x * 7 + y * 31) % 23) == 0;
                    px[y * w + x] = veta ? Color32.Lerp(fila, maderaAlta, 0.5f) : fila;
                }
            }

            s = Crear(px, w, h, "TenThousandYearsListonEstante");
            _cache[clave] = s;
            return s;
        }

        // =================================================================
        // [Playtest 26, CONTRATO_LEGIBILIDAD.md §1] LA GRAMÁTICA VISUAL —
        // familias de sprites compartidas por las 5 estaciones nuevas
        // (Crisol/Prensa/BancoChispa/Columna/Ensayo). Mismo criterio de
        // Escala/S(...) y cacheo que el resto de la fábrica.
        // =================================================================

        /// <summary>
        /// GRAMÁTICA §1.1: EMBUDO = ENTRADA DE MATERIA. Boca ancha arriba,
        /// se cierra hacia un pico estrecho abajo (perfil t*t: se abre rápido
        /// cerca de la boca, como un embudo de verdad, no un cono recto) --
        /// latón, MISMO sprite-familia en TODA máquina que reciba materia del
        /// frasco (Crisol/Prensa/BancoChispa/Ensayo): se aprende una vez.
        /// </summary>
        public static Sprite Embudo(int spanCeldas)
        {
            string clave = "embudo" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Mathf.Clamp(spanCeldas * 2 * Escala, S(16), S(512));
            int h = S(14);
            var px = new Color32[w * h];

            Color32 latonAlto = new Color32(0xF2, 0xD3, 0x8C, 255);
            Color32 laton = new Color32(0xBD, 0x93, 0x47, 255);
            Color32 latonBajo = new Color32(0x70, 0x56, 0x2A, 255);

            int spoutSemi = Mathf.Max(S(1), w / 10);
            int mouthSemi = w / 2 - S(1);

            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1); // 0 abajo (pico), 1 arriba (boca).
                int semi = Mathf.RoundToInt(Mathf.Lerp(spoutSemi, mouthSemi, t * t));
                bool remateBoca = y >= h - S(2); // remate de la boca, ligeramente más ancho que el cuerpo del embudo.
                if (remateBoca) semi = mouthSemi + S(1);

                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    bool bordeIzq = dx <= -semi + Escala;
                    bool bordeDer = dx >= semi - Escala;
                    Color32 c = remateBoca ? latonAlto : (bordeIzq ? latonAlto : (bordeDer ? latonBajo : laton));
                    px[y * w + x] = c;
                }
            }

            s = Crear(px, w, h, "TenThousandYearsEmbudo");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// GRAMÁTICA §1.2: BRASERO = ENTRADA DE COMBUSTIBLE, la ÚNICA otra
        /// boca que existe -- otra forma (cesto ovalado, no un embudo),
        /// otra altura, otro color (hierro oscuro, no latón): jamás se
        /// confunde. Silueta de bowl con barrotes verticales (rejilla del
        /// cesto) que dejan asomar el rescoldo de dentro (capa
        /// <see cref="LechoBrasas"/>, reutilizada por el llamante
        /// —Game/Crisol.cs, Game/EnsayoMaestro.cs—, tintada de ámbar/naranja.
        /// NOTA (playtest 49): este cref apuntaba a <c>LechoBrasasPlaca</c>,
        /// la variante que el playtest 48 hizo para la placa de calor y que
        /// el 49 retiró al volver el serpentín (ver la CRONOLOGÍA DEL
        /// SERPENTÍN arriba). El lecho de brasas del HOGAR —este— nunca se
        /// tocó: es el bueno y sigue siendo el que usan Crisol y Ensayo).
        /// </summary>
        public static Sprite Brasero(int spanCeldas)
        {
            string clave = "brasero" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Mathf.Clamp(spanCeldas * 2 * Escala, S(16), S(512));
            int h = S(14);
            var px = new Color32[w * h];

            Color32 hierroAlto = new Color32(0x58, 0x50, 0x4C, 255);
            Color32 hierro = new Color32(0x2A, 0x26, 0x24, 255);
            Color32 hierroBajo = new Color32(0x14, 0x12, 0x12, 255);

            int maxSemi = w / 2 - S(1);
            int barrotePeriodo = Mathf.Max(1, S(3));

            for (int y = S(1); y < h - S(1); y++)
            {
                // Perfil de bowl: máximo ancho a media altura (seno), NUNCA
                // rectangular -- la silueta por sí sola ya lee "cesto", no
                // "chasis" (grammar §1.2: "otra forma que un embudo").
                float t = (y - S(1)) / (float)(h - S(2) - 1);
                float bulge = Mathf.Sin(t * Mathf.PI);
                int semi = Mathf.RoundToInt(Mathf.Lerp(maxSemi * 0.35f, maxSemi, bulge));

                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    bool barrote = (x % barrotePeriodo) == 0; // rejilla vertical del cesto.
                    bool bordeIzq = dx <= -semi + Escala;
                    Color32 c = barrote ? hierroBajo : (bordeIzq ? hierroAlto : hierro);
                    px[y * w + x] = c;
                }
            }

            // Patas cortas: dos tacos macizos bajo el cesto.
            int pataSemi = Mathf.Max(S(1), maxSemi / 3);
            for (int y = 0; y < S(1); y++)
            {
                for (int dx = -pataSemi; dx <= pataSemi; dx++)
                {
                    int xIzq = w / 2 - maxSemi / 2 + dx;
                    int xDer = w / 2 + maxSemi / 2 + dx;
                    if (xIzq >= 0 && xIzq < w) px[y * w + xIzq] = hierroBajo;
                    if (xDer >= 0 && xDer < w) px[y * w + xDer] = hierroBajo;
                }
            }

            s = Crear(px, w, h, "TenThousandYearsBrasero");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// GRAMÁTICA §1.3: CUBETA ENMARCADA. Marco de latón de 2px con
        /// remate en las cuatro esquinas -- overlay transparente por dentro
        /// (se dibuja SOBRE el chasis del recipiente, escalado un poco más
        /// grande, mismo patrón que el "_resalte" de foco ya existente en
        /// Crisol/Prensa/BancoChispa). Lee "contenedor", no "agujero en el
        /// suelo": lo que el playtest 25 pedía explícitamente para todo
        /// recipiente de trabajo.
        /// </summary>
        public static Sprite MarcoContenedor(int spanCeldas)
        {
            string clave = "marco" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Mathf.Clamp(spanCeldas * 2 * Escala, S(16), S(512));
            int h = S(14);
            var px = new Color32[w * h]; // transparente por defecto.

            Color32 laton = new Color32(0xE8, 0xC4, 0x7A, 255);
            Color32 latonBrillo = new Color32(0xFF, 0xEE, 0xC0, 255);
            int grosor = S(2);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool borde = x < grosor || x >= w - grosor || y < grosor || y >= h - grosor;
                    if (borde) px[y * w + x] = laton;
                }
            }

            int remate = S(3);
            MarcarRemateCuadrado(px, w, h, 0, 0, remate, latonBrillo);
            MarcarRemateCuadrado(px, w, h, w - remate, 0, remate, latonBrillo);
            MarcarRemateCuadrado(px, w, h, 0, h - remate, remate, latonBrillo);
            MarcarRemateCuadrado(px, w, h, w - remate, h - remate, remate, latonBrillo);

            s = Crear(px, w, h, "TenThousandYearsMarcoContenedor");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// GRAMÁTICA §1.4 (Crisol): CHIMENEA. Tubo de hierro montado sobre el
        /// chasis de la cubeta, con remate acampanado arriba -- el CUERPO
        /// estático; las bocanadas de humo son <see cref="Humo"/>, animadas
        /// por Crisol.cs SOLO mientras quema combustible.
        /// </summary>
        public static Sprite Chimenea(int spanCeldas)
        {
            string clave = "chimenea" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Mathf.Clamp(spanCeldas * Escala, S(10), S(200));
            int h = S(16);
            var px = new Color32[w * h];

            Color32 hierroAlto = new Color32(0x52, 0x4A, 0x46, 255);
            Color32 hierro = new Color32(0x2E, 0x28, 0x26, 255);
            Color32 hierroBajo = new Color32(0x16, 0x12, 0x12, 255);

            int semiTubo = Mathf.Max(S(1), w / 4);
            for (int y = 0; y < h; y++)
            {
                bool remate = y >= h - S(3); // boca superior, ligeramente más ancha.
                int semi = remate ? semiTubo + S(1) : semiTubo;
                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    bool borde = Mathf.Abs(dx) >= semi - Mathf.Max(1, Escala / 2);
                    px[y * w + x] = remate ? hierroAlto : (borde ? hierroBajo : hierro);
                }
            }

            s = Crear(px, w, h, "TenThousandYearsChimenea");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// GRAMÁTICA §1.4 (Crisol): una voluta de humo -- blob radial suave,
        /// semitransparente. Crisol.cs instancia varias, animando posición y
        /// alfa de cada una por código (nunca frames de textura: es UN solo
        /// sprite reusado con transform distinto, cero allocs de textura por
        /// bocanada).
        /// </summary>
        public static Sprite Humo()
        {
            const string clave = "humo";
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = S(10), h = S(10);
            var px = new Color32[w * h];
            float cx = w / 2f, cy = h / 2f, r = w / 2f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    float a = Mathf.Clamp01(1f - dist);
                    a *= a; // caída suave hacia el borde: lee como voluta, no como disco duro.
                    px[y * w + x] = new Color32(210, 205, 200, (byte)(a * 200));
                }
            }

            s = Crear(px, w, h, "TenThousandYearsHumo");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// GRAMÁTICA §1.4 (Prensa): HUSILLO. Vástago vertical con filetes de
        /// rosca periódicos -- "nadie ha dudado jamás de qué hace un tornillo
        /// de banco" (contrato). Se monta ESTÁTICO sobre la mandíbula (la
        /// pieza que se mueve es la mandíbula misma, ya animada por
        /// Prensa.cs).
        /// </summary>
        public static Sprite Husillo(int spanCeldas)
        {
            string clave = "husillo" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Mathf.Clamp(Mathf.Max(S(8), spanCeldas * Escala / 2), S(8), S(200));
            int h = S(20);
            var px = new Color32[w * h];

            Color32 hierroAlto = new Color32(0x6A, 0x62, 0x5C, 255);
            Color32 hierro = new Color32(0x3A, 0x34, 0x30, 255);
            Color32 hierroBajo = new Color32(0x1C, 0x18, 0x16, 255);

            int semiVastago = Mathf.Max(S(1), w / 5);
            int periodo = Mathf.Max(1, S(3));
            for (int y = 0; y < h; y++)
            {
                bool rosca = (y % periodo) == 0; // filete de rosca: una línea clara cada `periodo` filas.
                for (int dx = -semiVastago; dx <= semiVastago; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    bool borde = Mathf.Abs(dx) >= semiVastago - Mathf.Max(1, Escala / 2);
                    px[y * w + x] = rosca ? hierroAlto : (borde ? hierroBajo : hierro);
                }
            }

            s = Crear(px, w, h, "TenThousandYearsHusillo");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// GRAMÁTICA §1.4 (Banco de chispa): ARCO. Zigzag determinista (sin
        /// UnityEngine.Random -- regla de oro del proyecto) núcleo blanco +
        /// halo cian, pensado para estirarse horizontalmente entre los dos
        /// electrodos. BancoChispa.cs lo activa (alfa>0) SOLO mientras
        /// analiza y la conductividad leída es ≥1 -- si no conduce, no hay
        /// arco (la ausencia es el dato).
        /// </summary>
        public static Sprite Arco()
        {
            const string clave = "arco";
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = S(40), h = S(14);
            var px = new Color32[w * h]; // transparente.

            Color32 nucleo = new Color32(255, 255, 255, 255);
            Color32 halo = new Color32(140, 200, 255, 140);

            int centroY = h / 2;
            int prevY = centroY;
            int paso = Mathf.Max(1, S(2));
            for (int x = 0; x < w; x++)
            {
                int quiebro = ((x / paso) % 2 == 0) ? S(2) : -S(2);
                int y = Mathf.Clamp(centroY + quiebro, S(2), h - S(2) - 1);
                int yMin = Mathf.Min(prevY, y), yMax = Mathf.Max(prevY, y);
                for (int yy = yMin; yy <= yMax; yy++)
                {
                    px[yy * w + x] = nucleo;
                    if (yy - 1 >= 0) px[(yy - 1) * w + x] = halo;
                    if (yy + 1 < h) px[(yy + 1) * w + x] = halo;
                }
                prevY = y;
            }

            s = Crear(px, w, h, "TenThousandYearsArco");
            _cache[clave] = s;
            return s;
        }

        // =================================================================
        // [Playtest 27, CONTRATO_TALLER_GRANDE] EL TALLER GRANDE — la
        // familia de sprites de las estaciones-EDIFICIO.
        //
        // POR QUÉ EXISTE. Veredicto de Cesar sobre el playtest 26: "cajitas
        // ilegibles", "el embudo diminuto y horrible FLOTANDO", "otro embudo
        // feo que no es boquilla y sin capacidad". Las estaciones del 26
        // reutilizaban tres sprites (ChasisPlaca -- renombrada LosaPlaca en
        // el playtest 48, ver "PLACA DE CALOR" más arriba --/Embudo/
        // MarcoContenedor) estirados a cualquier proporción: un chasis de 14 téxeles de alto
        // escalado a 6 celdas de mundo se ve como una barra, no como un
        // aparato. Esta familia se dibuja con DOS parámetros (span y alto en
        // celdas), así que cada pieza nace con la proporción del hueco que
        // va a ocupar y ninguna se deforma.
        //
        // REGLA DE DENSIDAD (heredada del fix de baja resolución del
        // playtest 6): 2 téxeles de diseño por celda x Escala=3 = 6
        // téxeles/celda en AMBOS ejes. <see cref="Tex"/> es la única forma
        // de convertir celdas a téxeles en esta sección -- si alguien
        // hardcodea un alto fijo, vuelve la barra estirada.
        //
        // EL EMBUDO DECORATIVO ESTÁ PROHIBIDO desde esta ronda (mandato 2
        // del contrato). <see cref="Embudo"/> se CONSERVA (regla 15) porque
        // el Crisol sigue teniendo una boca de vertido de verdad, pero ahí
        // el embudo es MAMPOSTERÍA TALLADA (Sim/SimLevelBuilder.cs, paredes
        // diagonales de piedra que embudan la materia hacia la cámara) y
        // este sprite solo pone el LABIO de latón que la remata
        // (<see cref="LabioBoca"/>). Las estaciones que reciben DEPOSITANDO
        // (prensa, chispa, ensayo) llevan <see cref="MarcoBandeja"/>: una
        // bandeja abierta enmarcada, jamás un embudo que no embuda.
        // =================================================================

        /// <summary>Celdas -&gt; téxeles del lienzo, a 2 téxeles de diseño por celda x <see cref="Escala"/> (= 6 téxeles/celda). ÚNICA conversión permitida en la familia del playtest 27: ver el bloque de doc de arriba.</summary>
        private static int Tex(int celdas) => Mathf.Clamp(celdas * 2 * Escala, S(6), S(400));

        // ---- Paleta compartida de la familia (latón / carboncillo / piedra) ----
        private static readonly Color32 LatonAlto = new Color32(0xF4, 0xD8, 0x93, 255);
        private static readonly Color32 Laton = new Color32(0xC1, 0x97, 0x4B, 255);
        private static readonly Color32 LatonBajo = new Color32(0x6E, 0x53, 0x28, 255);
        // (playtest 27, SEGUNDA PASADA — visto jugando) EL HIERRO SUBE DE
        // VALOR. Con 0x302A27 sobre el vacío del cuarto (que se dibuja casi
        // negro) la panza del Crisol y el cesto del brasero eran INVISIBLES
        // mientras estaban vacíos: solo se veía el filete de latón, o sea un
        // alambre. Es la misma clase de fallo que la regla 52 ("el color de
        // un material se juzga contra sus vecinos EN PANTALLA, no en el hex
        // del código"), aquí aplicada a la maquinaria. Subido a 0x453D38 con
        // luz 0x6E6560: sigue siendo carboncillo, pero se ve.
        private static readonly Color32 HierroAlto = new Color32(0x6E, 0x65, 0x60, 255);
        private static readonly Color32 Hierro = new Color32(0x45, 0x3D, 0x38, 255);
        private static readonly Color32 HierroBajo = new Color32(0x21, 0x1D, 0x1A, 255);
        private static readonly Color32 PiedraAlta = new Color32(0x6C, 0x62, 0x58, 255);
        private static readonly Color32 Piedra = new Color32(0x45, 0x3E, 0x37, 255);
        private static readonly Color32 PiedraBaja = new Color32(0x23, 0x1F, 0x1B, 255);

        // -----------------------------------------------------------------
        // LA LECCIÓN QUE HACE FALTA ANTES DE LEER LAS DOS SIGUIENTES: **UN
        // SPRITE DE MÁQUINA NO PUEDE TAPAR SU PROPIA CÁMARA.** Los chasis del
        // playtest 26 (`ChasisPlaca` estirado sobre la cubeta) se dibujan con
        // sortingOrder 18, o sea DELANTE del sprite del mundo (-5): el
        // material que el jugador vertía dentro quedaba OCULTO tras la chapa
        // de su propia máquina. Es la mitad de "no recibo ningún feedback"
        // del veredicto de Cesar, y no se ve leyendo el código -- solo
        // jugando (regla 52).
        //
        // Desde el playtest 27, toda pieza que envuelva un recinto se dibuja
        // con el HUECO TRANSPARENTE, recortado con las MISMAS medidas
        // (muro/suelo en celdas) con las que Sim/SimLevelBuilder talló la
        // mampostería. Así el sprite viste los muros reales y la cámara real
        // se ve por dentro.
        // -----------------------------------------------------------------

        /// <summary>
        /// EL CRISOL: panza de hierro remachada con dos zunchos de latón,
        /// silueta PANZUDA de verdad (ancho máximo al 40% de la altura, como
        /// un caldero) y la CÁMARA RECORTADA A TRANSPARENTE -- ver la nota de
        /// arriba. `muroCeldas`/`sueloCeldas` tienen que ser los MISMOS que
        /// usó la mampostería, o el hueco del dibujo y el hueco de verdad no
        /// coincidirán.
        /// </summary>
        public static Sprite PanzaCrisol(int spanCeldas, int altoCeldas, int muroCeldas, int sueloCeldas)
        {
            string clave = "panza" + spanCeldas + "x" + altoCeldas + "m" + muroCeldas + "s" + sueloCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(spanCeldas), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            int maxSemi = w / 2 - S(1);

            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1); // 0 abajo, 1 arriba.
                // Perfil de caldero: base recogida, panza máxima al 40% de la
                // altura, hombros que se cierran un poco hacia la boca.
                float perfil = t < 0.4f
                    ? Mathf.Lerp(0.66f, 1f, t / 0.4f)
                    : Mathf.Lerp(1f, 0.88f, (t - 0.4f) / 0.6f);
                int semi = Mathf.RoundToInt(maxSemi * perfil);

                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    int borde = Mathf.Max(1, Escala);
                    Color32 c;
                    if (dx <= -semi + borde) c = HierroAlto;      // luz por la izquierda.
                    else if (dx >= semi - borde) c = HierroBajo;  // sombra por la derecha.
                    else c = Hierro;
                    px[y * w + x] = c;
                }
            }

            // Dos zunchos de latón (los aros que ciñen la panza) + remaches.
            int[] zunchos = { Mathf.RoundToInt(h * 0.26f), Mathf.RoundToInt(h * 0.58f) };
            for (int zi = 0; zi < zunchos.Length; zi++)
            {
                int y0 = zunchos[zi];
                for (int y = y0; y < y0 + Mathf.Max(1, Escala); y++)
                {
                    if (y < 0 || y >= h) continue;
                    for (int x = 0; x < w; x++)
                    {
                        if (px[y * w + x].a == 0) continue;
                        px[y * w + x] = (x < w / 2) ? LatonAlto : Laton;
                    }
                }
                for (int x = S(3); x < w - S(3); x += S(7))
                {
                    if (px[y0 * w + x].a == 0) continue;
                    MarcarRemache(px, w, h, x, y0 - Escala, LatonAlto);
                }
            }

            RecortarCamara(px, w, h, spanCeldas, altoCeldas, muroCeldas, sueloCeldas, LatonAlto);

            s = Crear(px, w, h, "TenThousandYearsPanzaCrisol");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// EL BRASERO (entrada de COMBUSTIBLE, la ÚNICA otra boca del taller):
        /// cesto de hierro negro con barrotes verticales y tres patas, CHATO y
        /// ANCHO -- todo lo contrario del crisol, que es alto y panzudo. Se
        /// distingue a treinta celdas de distancia por silueta y por color, sin
        /// leer nada. Cámara recortada como <see cref="PanzaCrisol"/>: dentro
        /// se ve el combustible que le echas.
        /// </summary>
        public static Sprite CestoBrasero(int spanCeldas, int altoCeldas, int muroCeldas, int sueloCeldas)
        {
            string clave = "cesto" + spanCeldas + "x" + altoCeldas + "m" + muroCeldas + "s" + sueloCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(spanCeldas), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            // TERCERA PASADA (visto jugando): el perfil anterior se estrechaba
            // al 62% en la base, y como el recorte de la cámara se lleva el
            // centro, lo que quedaba a la vista eran DOS TRAPECIOS SUELTOS --
            // el brasero se leía como dos columnas rotas, no como un cesto.
            // Ahora las paredes son casi RECTAS (0.90 -> 1.0, solo un ligero
            // acampanado en el remate), y dos AROS horizontales cruzan de lado
            // a lado por debajo de la boca: los aros son lo que ata las dos
            // paredes en un solo objeto para el ojo.
            int maxSemi = w / 2 - S(1);
            int barrote = Mathf.Max(Escala, w / 26);
            int periodo = Mathf.Max(barrote * 3, w / 9);

            for (int y = 0; y < h; y++)
            {
                float t = y / (float)Mathf.Max(1, h - 1);
                int semi = Mathf.RoundToInt(Mathf.Lerp(maxSemi * 0.90f, maxSemi, t * t));
                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    bool esBarrote = (x % periodo) < barrote;
                    Color32 c = esBarrote ? HierroBajo : Hierro;
                    if (dx <= -semi + Escala) c = HierroAlto;
                    if (y >= h - Escala) c = HierroAlto; // canto del cesto.
                    px[y * w + x] = c;
                }
            }

            // Los dos AROS: bandas macizas que cruzan el cesto ENTERO. Se
            // dibujan antes del recorte de la cámara a propósito -- el recorte
            // se lleva su tramo central, pero dejan un tacón grueso en cada
            // pared a la misma altura, y eso basta para que el ojo cierre la
            // figura. (Un aro completo taparía el combustible: lo que hay
            // dentro tiene que verse.)
            int aroGrosor = Mathf.Max(Escala, h / 12);
            int[] arosY = { Mathf.RoundToInt(h * 0.22f), Mathf.RoundToInt(h * 0.52f) };
            for (int i = 0; i < arosY.Length; i++)
                for (int y = arosY[i]; y < arosY[i] + aroGrosor; y++)
                    for (int x = 0; x < w; x++)
                    {
                        if (y < 0 || y >= h) continue;
                        if (px[y * w + x].a == 0) continue;
                        px[y * w + x] = (x < w / 2) ? HierroAlto : Hierro;
                    }

            RecortarCamara(px, w, h, spanCeldas, altoCeldas, muroCeldas, sueloCeldas, HierroAlto);

            s = Crear(px, w, h, "TenThousandYearsCestoBrasero");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// Recorta a TRANSPARENTE el rectángulo que ocupa la cámara real
        /// (inset de `muroCeldas` a cada lado y `sueloCeldas` por abajo,
        /// abierto por arriba) y le deja un filete de luz de un téxel en los
        /// tres cantos interiores -- así el hueco se lee como un hueco
        /// TALLADO y no como un agujero del dibujo. Ver la nota "un sprite de
        /// máquina no puede tapar su propia cámara".
        /// </summary>
        private static void RecortarCamara(Color32[] px, int w, int h, int spanCeldas, int altoCeldas,
            int muroCeldas, int sueloCeldas, Color32 filete)
        {
            if (spanCeldas <= 2 * muroCeldas || altoCeldas <= sueloCeldas) return;
            int x0 = Mathf.RoundToInt(w * muroCeldas / (float)spanCeldas);
            int x1 = w - 1 - x0;
            int y0 = Mathf.RoundToInt(h * sueloCeldas / (float)altoCeldas);
            if (x1 <= x0 || y0 >= h) return;

            for (int y = y0; y < h; y++)
                for (int x = x0; x <= x1; x++)
                    px[y * w + x] = default;

            for (int y = y0; y < h; y++)
            {
                for (int k = 0; k < Escala; k++)
                {
                    Pintar(px, w, h, x0 - 1 - k, y, k == 0 ? filete : Hierro);
                    Pintar(px, w, h, x1 + 1 + k, y, k == 0 ? filete : HierroBajo);
                }
            }
            for (int x = x0 - Escala; x <= x1 + Escala; x++)
                for (int k = 0; k < Escala; k++) Pintar(px, w, h, x, y0 - 1 - k, k == 0 ? filete : Hierro);
        }

        /// <summary>
        /// LECHO DE BRASAS: manchas irregulares (deterministas, hash de
        /// posición -- nunca UnityEngine.Random) en blanco puro, para que el
        /// llamante lo tinte de ámbar y lo haga respirar. Es lo que se ve
        /// DENTRO del brasero y en el fondo de la panza: un fuego que se ve
        /// respirar (contrato §5), no una barra naranja.
        /// </summary>
        public static Sprite LechoBrasas(int spanCeldas, int altoCeldas)
        {
            string clave = "brasas" + spanCeldas + "x" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(spanCeldas), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            int r = Mathf.Max(2, Mathf.Min(w, h) / 7); // radio del carbón.
            int paso = Mathf.Max(3, r * 2);
            for (int cy = r; cy < h; cy += paso)
            {
                for (int cx = r; cx < w; cx += paso)
                {
                    // Desplazamiento pseudoaleatorio determinista por carbón.
                    uint hash = (uint)(cx * 73856093 ^ cy * 19349663);
                    int jx = (int)(hash % 5) - 2;
                    int jy = (int)((hash >> 8) % 5) - 2;
                    int rr = r - (int)((hash >> 16) % 2);
                    byte brillo = (byte)(170 + (hash >> 20) % 86); // 170..255: brasas de distinta vida.

                    for (int y = -rr; y <= rr; y++)
                    {
                        for (int x = -rr; x <= rr; x++)
                        {
                            if (x * x + y * y > rr * rr) continue;
                            int xx = cx + jx + x, yy = cy + jy + y;
                            if (xx < 0 || xx >= w || yy < 0 || yy >= h) continue;
                            px[yy * w + xx] = new Color32(255, 255, 255, brillo);
                        }
                    }
                }
            }

            s = Crear(px, w, h, "TenThousandYearsLechoBrasas");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// BANDEJA ABIERTA ENMARCADA (mandato 2 del contrato: "si una máquina
        /// no recibe por vertido a cámara, lleva una BANDEJA ABIERTA amplia y
        /// enmarcada"). Marco de latón grueso con cartelas en las cuatro
        /// esquinas y un LABIO volado en el borde superior -- transparente por
        /// dentro (se dibuja SOBRE el hueco real de la mampostería, así que
        /// enmarca la materia que hay dentro en vez de taparla). Proporción
        /// correcta por construcción: recibe span Y alto.
        /// </summary>
        // =================================================================
        // (RONDA 69) EL SÁNDWICH DEL RECIPIENTE: MachineBack -> Sim ->
        // MachineFront (mandato 2.5D de la ronda 66, diferido hasta ahora).
        // Dos piezas por recipiente:
        //  · FondoInterior -- panel OPACO del interior, en
        //    Capas.MaquinaFondoInterior (-8): se ve a través de las celdas
        //    VACÍAS de la cámara (la sim pinta el vacío transparente), y
        //    convierte "un agujero por el que se ve la pared del cuarto" en
        //    "el interior del aparato". La materia de la sim (-5) lo tapa
        //    donde hay carga: exactamente el orden correcto sin hacer nada.
        //  · RebordeRecipiente -- la pared CERCANA del recipiente, en
        //    Capas.MaquinaFrente (35): una banda baja que solapa las
        //    primeras filas de la carga, como cuando miras una olla desde
        //    un poco arriba y tu propio borde te come la base del contenido.
        //    BAJA a propósito (2 filas de 9): las reacciones siguen siendo
        //    protagonistas (mandato de la ronda 66) -- el reborde CONTIENE,
        //    no tapa.
        // =================================================================
        /// <summary>Panel opaco del interior de un recipiente (va DETRÁS de la sim, orden <c>Capas.MaquinaFondoInterior</c>). Refractario oscuro con oclusión en bordes y suelo, luz tenue entrando por la boca, e hiladas horizontales apenas insinuadas.</summary>
        public static Sprite FondoInterior(int spanCeldas, int altoCeldas)
        {
            string clave = "fondoint" + spanCeldas + "x" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(spanCeldas), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            // Más oscuro que HierroBajo: es una CAVIDAD, no una superficie.
            var fondo = new Color32(0x17, 0x13, 0x11, 255);
            var fondoLuz = new Color32(0x24, 0x1E, 0x19, 255);   // donde la luz de la boca alcanza.
            var fondoOcl = new Color32(0x0D, 0x0B, 0x0A, 255);   // esquinas y suelo (oclusión).
            int margen = Mathf.Max(Escala, Mathf.Min(w, h) / 8); // franja de oclusión perimetral.

            for (int y = 0; y < h; y++)
            {
                float t = y / (float)Mathf.Max(1, h - 1); // 0 suelo, 1 boca.
                for (int x = 0; x < w; x++)
                {
                    bool ocl = x < margen || x >= w - margen || y < margen;
                    Color32 c = ocl ? fondoOcl : (t > 0.55f ? fondoLuz : fondo);
                    // Hiladas del refractario: una línea tenue cada ~4 celdas,
                    // solo en la zona no ocluida (que se INSINÚE, no que dibuje).
                    if (!ocl && (y % S(8)) < Mathf.Max(1, Escala / 2)) c = fondoLuz;
                    px[y * w + x] = c;
                }
            }

            s = Crear(px, w, h, "TenThousandYearsFondoInterior");
            _cache[clave] = s;
            return s;
        }

        /// <summary>La pared CERCANA de un recipiente (va DELANTE de la sim, orden <c>Capas.MaquinaFrente</c>): banda maciza de hierro con canto superior iluminado -- el borde sobre el que "miras dentro". Solapa las primeras filas de la carga.</summary>
        public static Sprite RebordeRecipiente(int spanCeldas, int altoCeldas)
        {
            string clave = "reborde" + spanCeldas + "x" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(spanCeldas), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color32 c;
                    if (y >= h - Escala) c = HierroAlto;          // el canto que recibe la luz: la línea que dice "borde".
                    else if (y >= h - S(2)) c = Hierro;
                    else c = HierroBajo;                           // la panza del borde, en sombra hacia abajo.
                    px[y * w + x] = c;
                }
            }

            // Remaches espaciados sobre el canto -- el mismo vocabulario de
            // los zunchos de la panza, para que el reborde se lea como parte
            // del MISMO aparato y no como una franja pegada.
            for (int x = S(2); x < w - S(2); x += S(6))
                MarcarRemache(px, w, h, x, h - S(1), HierroAlto);

            s = Crear(px, w, h, "TenThousandYearsRebordeRecipiente");
            _cache[clave] = s;
            return s;
        }

        // =================================================================
        // (RONDA 73, el prólogo rehecho) EL DEPÓSITO DE AGUA — la recompensa
        // del prólogo, construido sobre la FOTO DE REFERENCIA de Cesar: un
        // cilindro de vidrio con armazón de COBRE (tapa en domo con pomo,
        // bandas remachadas arriba y abajo, montantes laterales) y pátina de
        // cardenillo. Tres piezas, sándwich 2.5D completo:
        //  · TanqueFondo  -> DETRÁS de la sim (Capas.MaquinaFondoInterior):
        //    la cavidad fría del vidrio; el agua REAL de la sim se ve contra
        //    ella.
        //  · TanqueMarco  -> DELANTE de la sim (Capas.MaquinaFrente): el
        //    armazón entero con la ventana de vidrio TRANSLÚCIDA (alfa bajo
        //    + brillo diagonal): el agua queda visualmente DENTRO.
        //  · TanqueTubo   -> el tubo trasero que asoma junto a la base: la
        //    insinuación de que el depósito está CONECTADO al subsuelo y se
        //    rellena solo. (La tubería lateral completa hasta el suelo es la
        //    segunda entrega de la referencia de Cesar — vendrá con el
        //    autofill definitivo.)
        // La paleta del cobre es propia (más rojiza que el latón de la
        // familia) porque la referencia manda: cobre envejecido, no latón.
        // =================================================================
        private static readonly Color32 CobreAlto = new Color32(0xE8, 0xA5, 0x6B, 255);
        private static readonly Color32 Cobre = new Color32(0xA9, 0x66, 0x3C, 255);
        private static readonly Color32 CobreBajo = new Color32(0x5E, 0x37, 0x22, 255);
        private static readonly Color32 Cardenillo = new Color32(0x5F, 0x8F, 0x7A, 255); // la pátina verde del cobre viejo.

        /// <summary>¿Lleva pátina este téxel? Hash determinista posicional (regla: cero Random) — manchas pequeñas, ~12% de la superficie de cobre.</summary>
        private static bool Patina(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h >> 8) & 0xFF) < 31;
        }

        /// <summary>El armazón frontal completo del depósito (ver bloque de arriba). `spanCeldas`/`altoCeldas` = huella TOTAL del sprite; la ventana de vidrio deja ver la sim entre las bandas.</summary>
        public static Sprite TanqueMarco(int spanCeldas, int altoCeldas)
        {
            string clave = "tanquemarco" + spanCeldas + "x" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(spanCeldas), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            // Proporciones (en fracciones del alto, calcadas de la foto):
            int baseAlto = S(4);                 // zócalo + banda baja remachada.
            int bandaAlta0 = h - S(12);          // banda alta remachada.
            int bandaAlta1 = h - S(8);
            int domo0 = bandaAlta1;              // el domo con su pomo ocupa lo que queda.
            int montante = S(4);                 // ancho de cada montante lateral.
            int ventana0 = montante, ventana1 = w - montante;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color32 c;
                    bool cobre = false;

                    if (y < baseAlto)
                    {
                        // ZÓCALO: banda maciza, canto superior iluminado,
                        // remaches cada ~6px en su fila media.
                        cobre = true;
                        c = y >= baseAlto - Escala ? CobreAlto : (y < Escala ? CobreBajo : Cobre);
                        if (y >= baseAlto / 2 - Escala / 2 && y < baseAlto / 2 + Escala && (x + S(1)) % S(3) < Escala)
                            c = CobreAlto; // remache.
                    }
                    else if (y >= domo0)
                    {
                        // EL DOMO: se estrecha en dos escalones + pomo central.
                        int dy = y - domo0;
                        int alto = h - domo0;
                        float t = dy / (float)Mathf.Max(1, alto - 1);
                        float semi = Mathf.Lerp(w * 0.46f, w * 0.16f, t * t * 1.6f);
                        bool pomo = t > 0.62f && Mathf.Abs(x - w * 0.5f) <= w * 0.10f;
                        bool dentro = Mathf.Abs(x - w * 0.5f) <= semi || pomo;
                        if (!dentro) continue;
                        cobre = true;
                        c = x < w * 0.5f - semi * 0.5f ? CobreAlto : (x > w * 0.5f + semi * 0.6f ? CobreBajo : Cobre);
                        if (pomo && t > 0.9f) c = CobreAlto;
                    }
                    else if (y >= bandaAlta0 && y < bandaAlta1)
                    {
                        // BANDA ALTA remachada (tapa la boca abierta de la sim:
                        // el chorro del rellenado "entra por la tapa").
                        cobre = true;
                        c = y >= bandaAlta1 - Escala ? CobreAlto : (y < bandaAlta0 + Escala ? CobreBajo : Cobre);
                        int med = (bandaAlta0 + bandaAlta1) / 2;
                        if (y >= med - Escala / 2 && y < med + Escala && (x + S(1)) % S(3) < Escala)
                            c = CobreAlto;
                    }
                    else if (x < ventana0 || x >= ventana1)
                    {
                        // MONTANTES laterales.
                        cobre = true;
                        bool izq = x < ventana0;
                        int d = izq ? x : w - 1 - x;
                        c = d < Escala ? CobreBajo : (d >= montante - Escala ? CobreAlto : Cobre);
                    }
                    else
                    {
                        // LA VENTANA DE VIDRIO: translúcida (la sim se ve
                        // detrás). Un brillo diagonal y un canto frío junto a
                        // los montantes — vidrio, no agujero.
                        int rel = x - ventana0;
                        int anchoV = ventana1 - ventana0;
                        bool brillo = (rel + y) % Mathf.Max(1, w) < S(3) && rel > anchoV / 6 && rel < anchoV / 2;
                        bool canto = rel < Escala || rel >= anchoV - Escala;
                        if (brillo) c = new Color32(0xCF, 0xE4, 0xE8, 62);
                        else if (canto) c = new Color32(0x9F, 0xB8, 0xBC, 48);
                        else c = new Color32(0x8A, 0xA6, 0xAE, 22);
                    }

                    if (cobre && Patina(x, y)) c = Cardenillo;
                    px[y * w + x] = c;
                }
            }

            s = Crear(px, w, h, "TenThousandYearsTanqueMarco");
            _cache[clave] = s;
            return s;
        }

        /// <summary>La cavidad del depósito (DETRÁS de la sim): más fría que la del horno — vidrio en sombra, con una insinuación vertical de reflejo.</summary>
        public static Sprite TanqueFondo(int spanCeldas, int altoCeldas)
        {
            string clave = "tanquefondo" + spanCeldas + "x" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(spanCeldas), h = Tex(altoCeldas);
            var px = new Color32[w * h];
            var fondo = new Color32(0x14, 0x19, 0x1C, 255);     // azul-gris profundo, no pardo: es vidrio, no refractario.
            var fondoLuz = new Color32(0x20, 0x2A, 0x2E, 255);
            var fondoOcl = new Color32(0x0B, 0x0E, 0x10, 255);
            int margen = Mathf.Max(Escala, Mathf.Min(w, h) / 8);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool ocl = x < margen || x >= w - margen || y < margen;
                    bool reflejo = x > w / 5 && x < w / 5 + S(2);
                    px[y * w + x] = ocl ? fondoOcl : (reflejo ? fondoLuz : fondo);
                }
            }

            s = Crear(px, w, h, "TenThousandYearsTanqueFondo");
            _cache[clave] = s;
            return s;
        }

        /// <summary>El tubo trasero del depósito: un caño corto de cobre oscuro con dos juntas, asomando de la tierra tras la base — dice "esto se rellena solo" sin robarle el diseño a la tubería lateral futura.</summary>
        public static Sprite TanqueTubo(int altoCeldas)
        {
            string clave = "tanquetubo" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(2), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color32 c = x < Escala ? CobreAlto : (x >= w - Escala ? CobreBajo : Cobre);
                    // Dos juntas anulares (más anchas que el caño un téxel a
                    // cada lado no se puede sin cambiar w: se marcan a valor).
                    if (y % S(5) < Escala) c = CobreBajo;
                    if (Patina(x + 61, y)) c = Cardenillo; // sal posicional distinta a la del marco.
                    px[y * w + x] = c;
                }
            }

            s = Crear(px, w, h, "TenThousandYearsTanqueTubo");
            _cache[clave] = s;
            return s;
        }

        // (R86/R88, regla 15) `TanqueTuboLateral` (caño fino R85) y
        // `TanqueMarcoConTubo` (armazón con tubo horneado R86) SE RETIRARON:
        // el primero por veto de Cesar ("no con ese cosito"), el segundo
        // porque la dirección de escena R88 exige que el tubo sea una PIEZA
        // SUELTA que el Maestro instala A LA VISTA tras el renacer (empuja
        // desde el suelo, encaja con un clank). El marco vuelve a ser el
        // clásico y el tubo vive abajo, en TanqueTuboGrueso.

        /// <summary>
        /// (R88 — la referencia de la tubería de Cesar) EL TUBO GRUESO como
        /// PIEZA PROPIA: columna de cobre de 3 celdas de ancho, mismo cobre y
        /// misma pátina del tanque ("del mismo material"), pie remachado que
        /// PISA el suelo, dos anillos-abrazadera, y TAPÓN ROSCADO arriba.
        /// La instala DepositoDeAgua.InstalarTubo (sube desde el subsuelo y
        /// encaja junto al hombro del tanque — teatralidad del REORDEN R88).
        /// </summary>
        public static Sprite TanqueTuboGrueso(int altoCeldas)
        {
            string clave = "tanquetubogrueso" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(3), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            int tapon0 = h - S(4);                    // el tapón corona los últimos 2 celdas.
            int[] anillos = { Mathf.RoundToInt(h * 0.30f), Mathf.RoundToInt(h * 0.62f) };

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color32 c;
                    if (y >= tapon0)
                    {
                        // EL TAPÓN ROSCADO: anillos horizontales alternados,
                        // 1 px metido por flanco (pieza torneada).
                        if (x < Escala / 2 || x >= w - Escala / 2) continue;
                        c = ((y - tapon0) / Mathf.Max(1, Escala)) % 2 == 0 ? CobreAlto : CobreBajo;
                    }
                    else
                    {
                        // EL CUERPO: sombreado cilíndrico (luz a la izquierda),
                        // pie con remaches EN EL SUELO (y=0: Cesar dixit).
                        c = x < Escala ? CobreAlto : (x >= w - Escala ? CobreBajo : Cobre);
                        if (y < S(2))
                        {
                            c = y < Escala ? CobreBajo : CobreAlto;                       // el zócalo del pie.
                            if ((x + S(1)) % S(2) < Escala && y >= Escala) c = Cobre;     // remaches.
                        }
                        else
                        {
                            foreach (int ya in anillos)
                                if (y >= ya && y < ya + S(2))
                                    c = y < ya + Escala ? CobreAlto : CobreBajo;           // anillo-abrazadera.
                        }
                    }
                    if (Patina(x + 53, y)) c = Cardenillo;                                 // la MISMA pátina de la familia.
                    px[y * w + x] = c;
                }
            }

            s = Crear(px, w, h, "TenThousandYearsTanqueTuboGrueso");
            _cache[clave] = s;
            return s;
        }

        public static Sprite MarcoBandeja(int spanCeldas, int altoCeldas)
        {
            string clave = "bandeja" + spanCeldas + "x" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(spanCeldas), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            int grosor = Mathf.Max(Escala, Mathf.Min(w, h) / 10);
            int labio = Mathf.Max(Escala, grosor + Escala);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool lateral = x < grosor || x >= w - grosor;
                    bool fondo = y < grosor;
                    bool remateArriba = y >= h - labio;
                    if (!lateral && !fondo && !remateArriba) continue;

                    Color32 c;
                    if (remateArriba) c = (y >= h - Escala) ? LatonAlto : Laton; // labio volado: la línea que dice "aquí se deposita".
                    else if (x < grosor) c = LatonAlto;   // canto izquierdo iluminado.
                    else if (x >= w - grosor) c = LatonBajo;
                    else c = Laton;
                    px[y * w + x] = c;
                }
            }

            // Cartelas de esquina (escuadras): un taco macizo + su diagonal.
            int cart = Mathf.Max(Escala * 2, Mathf.Min(w, h) / 5);
            MarcarRemateCuadrado(px, w, h, 0, 0, cart, LatonAlto);
            MarcarRemateCuadrado(px, w, h, w - cart, 0, cart, LatonAlto);
            MarcarRemateCuadrado(px, w, h, 0, h - cart, cart, LatonAlto);
            MarcarRemateCuadrado(px, w, h, w - cart, h - cart, cart, LatonAlto);
            for (int d = 0; d < cart; d++)
            {
                int inv = cart - 1 - d;
                Pintar(px, w, h, cart + d, cart - 1 - d + Escala, LatonBajo);
                Pintar(px, w, h, w - 1 - cart - d, cart - 1 - d + Escala, LatonBajo);
                Pintar(px, w, h, cart + d, h - cart + inv - Escala, LatonBajo);
                Pintar(px, w, h, w - 1 - cart - d, h - cart + inv - Escala, LatonBajo);
            }

            s = Crear(px, w, h, "TenThousandYearsMarcoBandeja");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// LABIO DE BOCA: el remate de latón que corona la boca de vertido
        /// TALLADA EN PIEDRA del Crisol (mandato 2: el embudo es geometría,
        /// no sprite). Una banda ancha con el canto superior enrollado y dos
        /// cuernos que caen a los lados -- se lee como el borde de un
        /// tragante, y marca sin ambigüedad la línea por la que se vierte.
        /// </summary>
        public static Sprite LabioBoca(int spanCeldas, int altoCeldas)
        {
            string clave = "labio" + spanCeldas + "x" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(spanCeldas), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            int rollo = Mathf.Max(Escala, h / 3); // grosor del canto enrollado.
            for (int y = h - rollo; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = (y - (h - rollo)) / (float)Mathf.Max(1, rollo - 1);
                    px[y * w + x] = t > 0.6f ? LatonAlto : (t > 0.25f ? Laton : LatonBajo);
                }
            }

            // Cuernos: los extremos bajan describiendo una caída suave, así
            // el labio "abraza" la boca de piedra en vez de flotar sobre ella.
            int cuerno = Mathf.Max(Escala * 2, w / 12);
            for (int i = 0; i < cuerno; i++)
            {
                float t = i / (float)Mathf.Max(1, cuerno - 1);
                int caida = Mathf.RoundToInt((1f - t) * (h - rollo));
                for (int y = h - rollo - caida; y < h - rollo; y++)
                {
                    Pintar(px, w, h, i, y, Laton);
                    Pintar(px, w, h, w - 1 - i, y, LatonBajo);
                }
            }

            // Remaches a lo largo del labio: le dan escala de pieza forjada.
            for (int x = cuerno + S(2); x < w - cuerno - S(2); x += S(8))
                MarcarRemache(px, w, h, x, h - rollo + Escala, LatonAlto);

            s = Crear(px, w, h, "TenThousandYearsLabioBoca");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// MANDÍBULA DE LA PRENSA: bloque macizo de hierro con DIENTES en su
        /// cara inferior y dos pernos de latón. Los dientes son lo que hace
        /// que, quieta, ya se lea como "esto baja y aplasta".
        /// </summary>
        public static Sprite MandibulaPrensa(int spanCeldas, int altoCeldas)
        {
            string clave = "mandibula" + spanCeldas + "x" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(spanCeldas), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            int dientes = Mathf.Max(Escala * 2, h / 3);
            for (int y = dientes; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color32 c = Hierro;
                    if (y >= h - Escala) c = HierroAlto;                 // canto superior con luz.
                    else if (x < Escala || x >= w - Escala) c = HierroBajo;
                    px[y * w + x] = c;
                }
            }

            // Dientes: onda triangular en la cara de golpeo.
            int periodo = Mathf.Max(Escala * 2, w / 10);
            for (int x = 0; x < w; x++)
            {
                int fase = x % periodo;
                int mitad = Mathf.Max(1, periodo / 2);
                int alto = fase < mitad ? fase : periodo - 1 - fase;
                int yTope = dientes - Mathf.RoundToInt(alto * dientes / (float)mitad);
                for (int y = yTope; y < dientes; y++)
                    Pintar(px, w, h, x, y, (y < yTope + Escala) ? HierroBajo : Hierro);
            }

            // Pernos de latón: la pieza cuelga del husillo por aquí.
            int pernoY = h - Mathf.Max(Escala * 2, h / 3);
            MarcarRemateCuadrado(px, w, h, w / 4, pernoY, Mathf.Max(Escala, h / 6), Laton);
            MarcarRemateCuadrado(px, w, h, 3 * w / 4, pernoY, Mathf.Max(Escala, h / 6), Laton);

            s = Crear(px, w, h, "TenThousandYearsMandibulaPrensa");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// VOLANTE (rueda de radios) de latón, cuadrado, PENSADO PARA GIRAR:
        /// el llamante rota su Transform mientras la prensa baja ("un tornillo
        /// que gira de verdad al prensar", contrato §5). Aro + cubo + 6 radios:
        /// con menos radios la rotación no se percibe, con más se emborrona.
        /// </summary>
        public static Sprite Volante(int diamCeldas)
        {
            string clave = "volante" + diamCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(diamCeldas), h = w;
            var px = new Color32[w * h];
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float rExt = w * 0.5f - Escala;
            float rInt = rExt - Mathf.Max(Escala, w / 12f);
            float rCubo = w * 0.16f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d <= rCubo) { px[y * w + x] = Laton; continue; }
                    if (d >= rInt && d <= rExt)
                    {
                        px[y * w + x] = (dy > 0f) ? LatonAlto : LatonBajo; // luz arriba, sombra abajo.
                        continue;
                    }
                    if (d < rInt)
                    {
                        // Seis radios: ángulo módulo 60º dentro de un margen.
                        float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 360f;
                        float m = Mathf.Repeat(ang, 60f);
                        float grosorGrados = Mathf.Max(6f, 260f / Mathf.Max(1f, d));
                        if (m < grosorGrados || m > 60f - grosorGrados) px[y * w + x] = Laton;
                    }
                }
            }

            s = Crear(px, w, h, "TenThousandYearsVolante");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// VIDRIO de la Columna de Ensayo (mandato 5: muros de PIEDRA con
        /// VIDRIO VISUAL delante). Panel translúcido verdoso con un BRILLO
        /// DIAGONAL y dos cantos claros. Va con sortingOrder entre el fondo
        /// (-10) y el sprite del mundo (-5): la materia que cae dentro se
        /// dibuja ENCIMA, así que se ve a través del cristal, que es
        /// justamente lo que hace que se lea como cristal.
        /// </summary>
        public static Sprite VidrioPanel(int spanCeldas, int altoCeldas)
        {
            string clave = "vidriopanel" + spanCeldas + "x" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(spanCeldas), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            Color32 cuerpo = new Color32(0xB6, 0xD6, 0xCE, 44);
            Color32 canto = new Color32(0xE2, 0xF4, 0xF0, 120);
            Color32 brillo = new Color32(255, 255, 255, 96);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool esCanto = x < Escala || x >= w - Escala;
                    px[y * w + x] = esCanto ? canto : cuerpo;
                }
            }

            // Brillo diagonal: dos bandas paralelas que cruzan el panel.
            int anchoBanda = Mathf.Max(Escala, w / 9);
            for (int y = 0; y < h; y++)
            {
                int x0 = Mathf.RoundToInt(w * 0.18f + y * 0.35f);
                for (int k = 0; k < anchoBanda; k++) Pintar(px, w, h, x0 + k, y, brillo);
                int x1 = x0 + anchoBanda * 2;
                for (int k = 0; k < Mathf.Max(1, anchoBanda / 2); k++) Pintar(px, w, h, x1 + k, y, new Color32(255, 255, 255, 52));
            }

            s = Crear(px, w, h, "TenThousandYearsVidrioPanel");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// ELECTRODO del Banco de Chispa: pie de porcelana (aislante claro),
        /// vástago de latón y punta de cobre. Vertical, para plantarlo a cada
        /// lado de la bandeja -- entre las dos puntas salta
        /// <see cref="Arco"/>.
        /// </summary>
        public static Sprite Electrodo(int anchoCeldas, int altoCeldas)
        {
            string clave = "electrodo" + anchoCeldas + "x" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(anchoCeldas), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            Color32 porcelana = new Color32(0xCF, 0xC6, 0xB4, 255);
            Color32 porcelanaSombra = new Color32(0x8E, 0x86, 0x77, 255);
            Color32 cobre = new Color32(0xD8, 0x7A, 0x44, 255);

            int aislante = Mathf.RoundToInt(h * 0.34f);
            int punta = Mathf.RoundToInt(h * 0.12f);

            for (int y = 0; y < h; y++)
            {
                int semi;
                Color32 dentro, izq, der;
                if (y < aislante)
                {
                    // Pie acampanado con dos gargantas (perfil de aislador).
                    float t = y / (float)Mathf.Max(1, aislante - 1);
                    float onda = 0.78f + 0.22f * Mathf.Cos(t * Mathf.PI * 4f);
                    semi = Mathf.RoundToInt((w * 0.5f - Escala) * onda);
                    dentro = porcelana; izq = porcelana; der = porcelanaSombra;
                }
                else if (y >= h - punta)
                {
                    semi = Mathf.Max(Escala, Mathf.RoundToInt(w * 0.22f));
                    dentro = cobre; izq = cobre; der = new Color32(0x8C, 0x47, 0x24, 255);
                }
                else
                {
                    semi = Mathf.Max(Escala, Mathf.RoundToInt(w * 0.28f));
                    dentro = Laton; izq = LatonAlto; der = LatonBajo;
                }

                for (int dx = -semi; dx <= semi; dx++)
                {
                    int x = w / 2 + dx;
                    if (x < 0 || x >= w) continue;
                    px[y * w + x] = dx <= -semi + Escala ? izq : (dx >= semi - Escala ? der : dentro);
                }
            }

            s = Crear(px, w, h, "TenThousandYearsElectrodo");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// LÁMPARA del Banco: ampolla de vidrio sobre casquillo de latón, con
        /// el FILAMENTO en blanco puro dentro (el llamante tinta la capa del
        /// filamento según la conductividad leída). Dos capas separadas para
        /// que el vidrio no cambie de color al encenderse -- lo que brilla es
        /// el hilo, no el cristal.
        /// </summary>
        public static Sprite AmpollaLampara(int diamCeldas)
        {
            string clave = "ampolla" + diamCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(diamCeldas), h = Tex(diamCeldas) * 3 / 2;
            var px = new Color32[w * h];

            // (segunda pasada) El vidrio de la lámpara subió de alfa 70/150 a
            // 120/235 y el canto se engordó: a 7 celdas de diámetro sobre
            // fondo negro, la ampolla anterior se leía como un HUEVO DE PIEDRA
            // gris. Una lámpara tiene que parecer de vidrio incluso apagada, o
            // el instrumento de lectura del Banco no se reconoce.
            Color32 vidrio = new Color32(0xC6, 0xDE, 0xE8, 120);
            Color32 vidrioCanto = new Color32(0xF2, 0xFB, 0xFF, 235);
            int casquillo = Mathf.RoundToInt(h * 0.22f);
            float cx = (w - 1) * 0.5f;
            float cyAmp = casquillo + (h - casquillo) * 0.5f;
            float rx = w * 0.5f - Escala, ry = (h - casquillo) * 0.5f - Escala;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (y < casquillo)
                    {
                        int semi = Mathf.RoundToInt(w * 0.3f);
                        if (Mathf.Abs(x - cx) <= semi)
                            px[y * w + x] = ((y / Mathf.Max(1, Escala)) % 2 == 0) ? Laton : LatonBajo; // rosca del casquillo.
                        continue;
                    }
                    float dx = (x - cx) / rx, dy = (y - cyAmp) / ry;
                    float d = dx * dx + dy * dy;
                    if (d > 1f) continue;
                    px[y * w + x] = d > 0.68f ? vidrioCanto : vidrio;
                }
            }

            s = Crear(px, w, h, "TenThousandYearsAmpollaLampara");
            _cache[clave] = s;
            return s;
        }

        /// <summary>Filamento de la lámpara: espiral blanca centrada en la ampolla (misma huella que <see cref="AmpollaLampara"/> para poder apilarlas sin calcular offsets). Se tinta con SpriteRenderer.color.</summary>
        public static Sprite FilamentoLampara(int diamCeldas)
        {
            string clave = "filamento" + diamCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(diamCeldas), h = Tex(diamCeldas) * 3 / 2;
            var px = new Color32[w * h];

            int casquillo = Mathf.RoundToInt(h * 0.22f);
            int y0 = casquillo + Mathf.RoundToInt((h - casquillo) * 0.22f);
            int y1 = casquillo + Mathf.RoundToInt((h - casquillo) * 0.78f);
            int semi = Mathf.Max(Escala, w / 5);
            int periodo = Mathf.Max(Escala * 2, (y1 - y0) / 4);

            for (int y = y0; y <= y1; y++)
            {
                int fase = (y - y0) % periodo;
                int mitad = Mathf.Max(1, periodo / 2);
                float t = fase < mitad ? fase / (float)mitad : (periodo - fase) / (float)mitad;
                int x = w / 2 + Mathf.RoundToInt(Mathf.Lerp(-semi, semi, t));
                for (int k = 0; k < Escala; k++) Pintar(px, w, h, x + k, y, new Color32(255, 255, 255, 255));
            }
            // Los dos pies del filamento, hasta el casquillo.
            for (int y = casquillo; y < y0; y++)
            {
                Pintar(px, w, h, w / 2 - semi, y, new Color32(255, 255, 255, 190));
                Pintar(px, w, h, w / 2 + semi, y, new Color32(255, 255, 255, 190));
            }

            s = Crear(px, w, h, "TenThousandYearsFilamentoLampara");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// DOSEL del Ensayo del Maestro: arco de latón con clave central y
        /// una hilera de colgantes. Es lo único de este idioma visual que no
        /// es maquinaria -- y ésa es la idea: el Ensayo NO es una máquina, es
        /// un altar donde se dictamina (contrato §5, "un pedestal de examen
        /// con dosel").
        /// </summary>
        public static Sprite Dosel(int spanCeldas, int altoCeldas)
        {
            string clave = "dosel" + spanCeldas + "x" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(spanCeldas), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            int colgante = Mathf.Max(Escala * 2, h / 4);
            float cx = (w - 1) * 0.5f;
            float rx = w * 0.5f - Escala;
            float ry = (h - colgante) - Escala;
            int grosor = Mathf.Max(Escala, h / 8);

            // Arco: elipse hueca desde la base del arco hacia arriba.
            for (int y = colgante; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / rx;
                    float dy = (y - colgante) / Mathf.Max(1f, ry);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float dIntX = (x - cx) / Mathf.Max(1f, rx - grosor);
                    float dIntY = (y - colgante) / Mathf.Max(1f, ry - grosor);
                    float dInt = Mathf.Sqrt(dIntX * dIntX + dIntY * dIntY);
                    if (d <= 1f && dInt >= 1f)
                        px[y * w + x] = (x < cx) ? LatonAlto : Laton;
                }
            }

            // Clave central: un taco más grueso en el vértice.
            MarcarRemateCuadrado(px, w, h, w / 2 - grosor, h - grosor * 2, grosor * 2, LatonAlto);

            // Colgantes: flecos cortos que cuelgan del arranque del arco.
            int paso = Mathf.Max(Escala * 3, w / 12);
            for (int x = paso / 2; x < w; x += paso)
            {
                int largo = colgante - ((x / paso) % 2 == 0 ? 0 : Escala * 2);
                for (int y = colgante - largo; y < colgante; y++)
                    for (int k = 0; k < Escala; k++) Pintar(px, w, h, x + k, y, (y < colgante - largo + Escala) ? LatonAlto : LatonBajo);
            }

            s = Crear(px, w, h, "TenThousandYearsDosel");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// SILLAR: un bloque de piedra labrada con junta, para vestir los
        /// pilares/jambas/plintos que la mampostería del plano ya talló en la
        /// grilla. No sustituye a la piedra real (que es la que contiene la
        /// materia): la SUBRAYA, para que un pilar de una estación no se
        /// confunda con la roca del fondo.
        /// </summary>
        public static Sprite Sillar(int spanCeldas, int altoCeldas)
        {
            string clave = "sillar" + spanCeldas + "x" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(spanCeldas), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            int hilada = Mathf.Max(Escala * 2, h / 6);
            int junta = Mathf.Max(1, Escala / 2);
            int piezaW = Mathf.Max(Escala * 4, w / 3);

            for (int y = 0; y < h; y++)
            {
                int fila = y / hilada;
                int desfase = (fila % 2 == 0) ? 0 : piezaW / 2; // aparejo a soga: hiladas alternas desplazadas.
                for (int x = 0; x < w; x++)
                {
                    bool juntaH = (y % hilada) < junta;
                    bool juntaV = ((x + desfase) % piezaW) < junta;
                    Color32 c = Piedra;
                    if (juntaH || juntaV) c = PiedraBaja;
                    else if ((y % hilada) >= hilada - junta) c = PiedraAlta; // luz en el canto alto de cada sillar.
                    px[y * w + x] = c;
                }
            }

            s = Crear(px, w, h, "TenThousandYearsSillar");
            _cache[clave] = s;
            return s;
        }

        // =================================================================
        // (playtest 33) EL HERRAJE DE LAS BALDAS -- "BRACITOS INCLINADOS"
        // =================================================================
        // Encargo literal de Cesar: *"Arriba del horno hay una LÍNEA que me
        // gustó mucho porque puedo APOYAR COSAS: idealmente créale como
        // BRACITOS INCLINADOS para que pueda sostener más cosas"*.
        //
        // REPARTO DE RESPONSABILIDAD (la regla que ordena esta familia): la
        // PIEDRA de la balda la talla el plano (Sim/SimLevelBuilder.Repisas,
        // 2 filas macizas: eso es lo que SOSTIENE de verdad, ver regla 7) y
        // estos sprites son lo que la balda PARECE. Ninguna ménsula existe en
        // la grilla a propósito: una diagonal de piedra real sería una trampa
        // para los polvos que resbalan por su cara y, encima, el jugador con
        // el cincel vería una "escalera" pidiendo que la piquen -- el mismo
        // veredicto que Cesar dio de la Columna del playtest 26.
        // =================================================================

        /// <summary>
        /// MÉNSULA INCLINADA: el bracito que sostiene una balda. Escuadra de
        /// madera oscura con el canto de latón -- espalda vertical (pegada al
        /// muro), cabeza horizontal (bajo la balda) e HIPOTENUSA, que es lo
        /// único que la hace leerse como un apoyo y no como una caja. Nace
        /// mirando a la DERECHA (espalda a la izquierda); para la del otro
        /// extremo, el llamante invierte `localScale.x` -- así hay un solo
        /// sprite en caché para las dos.
        /// </summary>
        public static Sprite MensulaInclinada(int anchoCeldas, int altoCeldas)
        {
            string clave = "mensula" + anchoCeldas + "x" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(anchoCeldas), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            int grosor = Mathf.Max(2 * Escala, w / 5); // grueso del alma de madera.
            for (int y = 0; y < h; y++)
            {
                // La hipotenusa: la madera solo existe por debajo de la recta
                // que va de la esquina alta-derecha a la baja-izquierda.
                // (y=0 es ABAJO en el espacio de estos lienzos.)
                int limiteDer = (w - 1) * y / Mathf.Max(1, h - 1);
                for (int x = 0; x < w; x++)
                {
                    bool espalda = x < grosor;                      // pegada al muro.
                    bool cabeza = y >= h - grosor;                  // bajo la balda.
                    bool cuerpo = x <= limiteDer;
                    if (!(espalda || cabeza || cuerpo)) continue;

                    Color32 c = Hierro;
                    // Bisel: el canto de arriba y el de la diagonal reciben luz.
                    if (cabeza && y >= h - Mathf.Max(1, grosor / 3)) c = LatonAlto;
                    else if (cuerpo && x >= limiteDer - Escala) c = Laton;
                    else if (espalda && x < Escala) c = HierroBajo;
                    else if (((x / Escala) + (y / Escala)) % 7 == 0) c = HierroAlto; // veta de la madera oscura.
                    px[y * w + x] = c;
                }
            }

            s = Crear(px, w, h, "TenThousandYearsMensula");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// FILO DE BALDA: el listón de latón que remata el canto de una
        /// repisa, con sus remaches. Se dibuja DELANTE de la piedra real de
        /// la balda (que ya la pinta Sim/SimRenderer) y es lo que la separa
        /// de "una roca que se quedó ahí": una arista brillante horizontal a
        /// la altura del ojo lee inmediatamente como mueble.
        /// </summary>
        public static Sprite FiloBalda(int spanCeldas)
        {
            string clave = "filobalda" + spanCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(spanCeldas), h = S(4);
            var px = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                Color32 fila = y >= h - 1 ? LatonAlto : (y >= h / 2 ? Laton : LatonBajo);
                for (int x = 0; x < w; x++) px[y * w + x] = fila;
            }
            // Remaches cada ~6 celdas.
            int paso = Mathf.Max(S(6), w / 8);
            for (int x = paso / 2; x < w; x += paso)
                for (int k = 0; k < Mathf.Max(1, Escala / 2); k++)
                    Pintar(px, w, h, x + k, h / 2, LatonAlto);

            s = Crear(px, w, h, "TenThousandYearsFiloBalda");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// (playtest 33, SEGUNDA PASADA -- visto jugando) LA PIEDRA DE LA
        /// BALDA. La primera versión confiaba en que la losa real de la sim
        /// (`SimLevelBuilder.Repisas`, 2 filas de Stone) se viera sola. NO SE
        /// VE: a la distancia de juego son ~10 px de piedra oscura contra una
        /// pared de sillería igual de oscura, así que lo único que llegaba al
        /// ojo era el filo de latón y sus dos bracitos -- una barra amarilla
        /// sobre dos patas, o sea UNA MESA. Es la regla 52 otra vez (el color
        /// de algo se juzga contra sus vecinos EN PANTALLA, no en el hex) y la
        /// misma lección que subió el hierro de las máquinas en el playtest
        /// 27.
        ///
        /// Este sprite se dibuja ENCIMA de esa losa, con su mismo tamaño: cara
        /// superior clara (la que recibe la luz y donde se posan las cosas),
        /// canto frontal medio y una línea de sombra dura debajo. No sustituye
        /// a la piedra -- la piedra es la que sostiene -- la hace legible.
        /// </summary>
        public static Sprite BaldaPiedra(int spanCeldas, int altoCeldas)
        {
            string clave = "balda" + spanCeldas + "x" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(spanCeldas), h = Mathf.Max(S(4), altoCeldas * 2 * Escala);
            var px = new Color32[w * h];

            int caraAlto = Mathf.Max(2, h / 3);
            int piezaW = Mathf.Max(S(5), w / 6);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color32 c;
                    if (y >= h - caraAlto) c = PiedraAlta;              // cara superior: la que se ve iluminada.
                    else if (y >= caraAlto / 2) c = Piedra;             // canto frontal.
                    else c = PiedraBaja;                                // el bajo de la losa, en sombra.
                    if (x % piezaW == 0 && y < h - 1) c = PiedraBaja;   // despiece: la balda está hecha de losas, no de una pieza.
                    if (y == h - 1) c = new Color32(0x8A, 0x7E, 0x70, 255); // arista alta, el brillo del canto.
                    px[y * w + x] = c;
                }
            }

            s = Crear(px, w, h, "TenThousandYearsBaldaPiedra");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// (fix Cesar playtest 33) EL CUADRADO DE ANCLAJE: la pieza de latón
        /// que corona <see cref="Anclaje"/> y remata los dos extremos de
        /// <see cref="Balda"/> -- REEMPLAZA a <see cref="MensulaInclinada"/>
        /// en las baldas (que se queda sin llamadores ahí, regla 15 de
        /// CLAUDE.md: el factory sigue vivo, no se borra, solo deja de
        /// tener quien lo pida desde Game/WorkshopBackdrop.cs porque
        /// `Sim/SimLevelBuilder.Repisas` ahora está vacío). Placa cuadrada
        /// con bisel claro/oscuro (mismo lenguaje que el resto del taller) y
        /// un remache circular grande centrado -- lo que se lee como "una
        /// pieza clavada", no como una caja lisa.
        /// </summary>
        public static Sprite CuadradoAnclaje()
        {
            const string clave = "cuadradoanclaje";
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Tex(2), h = Tex(2);
            var px = new Color32[w * h];
            int borde = Mathf.Max(1, w / 10);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool enBorde = x < borde || x >= w - borde || y < borde || y >= h - borde;
                    px[y * w + x] = enBorde ? LatonBajo : (y >= h - h / 3 ? LatonAlto : Laton);
                }
            }

            // El remache central: dos discos concéntricos por distancia al
            // cuadrado (sin raíz cuadrada, mismo criterio barato que el resto
            // de esta familia de sprites -- se genera una vez y se cachea).
            int cx = w / 2, cy = h / 2, r = Mathf.Max(2, w / 5);
            int r2 = r * r, r2Interior = r2 * 2 / 3;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dx = x - cx, dy = y - cy;
                    int d2 = dx * dx + dy * dy;
                    if (d2 > r2) continue;
                    px[y * w + x] = d2 <= r2Interior ? LatonAlto : LatonBajo;
                }
            }

            s = Crear(px, w, h, "TenThousandYearsCuadradoAnclaje");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// (playtest 33) EL HAZ DE UNA CLARABOYA: un cono de luz fría que se
        /// abre hacia abajo, con los bordes deshilachados por un ruido estable
        /// y la intensidad muriendo con la profundidad. Blanco (lo tinta el
        /// llamante). Se dibuja DELANTE de la sim, como los halos, porque
        /// tiene que caer SOBRE la piedra y sobre las máquinas -- un haz
        /// pintado en el fondo se quedaría detrás de todo, que es justo lo que
        /// pasó en la primera pasada de esta ronda.
        ///
        /// Y sí, es luz que viene de arriba, que es lo que esta misma ronda
        /// prohíbe en el resto del taller: la diferencia es que ESTA tiene
        /// dueño físico visible -- el pozo está tallado de verdad en la bóveda
        /// (`SimLevelBuilder.ClaraboyaColumnas`) y se ve de dónde entra.
        /// </summary>
        public static Sprite HazClaraboya(int anchoCeldas, int altoCeldas)
        {
            string clave = "haz" + anchoCeldas + "x" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Mathf.Clamp(anchoCeldas * Escala, S(8), S(300));
            int h = Mathf.Clamp(altoCeldas * Escala, S(8), S(300));
            var px = new Color32[w * h];
            float cx = (w - 1) * 0.5f;

            for (int y = 0; y < h; y++)
            {
                // y=0 abajo: el haz es ancho y débil; arriba (y=h-1), estrecho
                // y fuerte -- es donde está el pozo.
                float prof = 1f - y / (float)(h - 1);            // 0 arriba, 1 abajo.
                float medio = Mathf.Lerp(w * 0.16f, w * 0.5f, prof);
                float fuerza = Mathf.Pow(1f - prof, 1.35f);      // se apaga al caer.
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Abs(x - cx) / medio;
                    if (d >= 1f) continue;
                    float a = fuerza * Mathf.Pow(1f - d * d, 1.6f);
                    // Deshilachado: un poco de ruido estable en el borde, para
                    // que el haz no tenga contorno de cono dibujado.
                    uint hn = (uint)(x * 374761393) ^ (uint)(y * 668265263);
                    hn ^= hn >> 13; hn *= 0x5bd1e995u; hn ^= hn >> 15;
                    a *= 0.86f + ((hn & 31u) / 31f) * 0.14f;
                    if (a <= 0.01f) continue;
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }

            s = Crear(px, w, h, "TenThousandYearsHazClaraboya");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// CADENA COLGANTE con su gancho: eslabones alternos (uno de frente,
        /// uno de canto) y una U de hierro al final. Cuelga del techo en los
        /// vanos vacíos -- es lo que da ESCALA a una bóveda alta: sin nada
        /// colgando, 95 celdas de alto y 60 se ven igual.
        /// </summary>
        public static Sprite CadenaColgante(int altoCeldas)
        {
            string clave = "cadena" + altoCeldas;
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = S(4), h = Tex(altoCeldas);
            var px = new Color32[w * h];

            int eslabon = Mathf.Max(S(2), h / Mathf.Max(4, altoCeldas));
            int cx = w / 2;
            for (int y = 0; y < h; y++)
            {
                bool deFrente = ((y / eslabon) & 1) == 0;
                int medio = deFrente ? w / 2 : Mathf.Max(1, w / 6);
                bool borde = (y % eslabon) < Mathf.Max(1, eslabon / 4) || (y % eslabon) >= eslabon - Mathf.Max(1, eslabon / 4);
                for (int d = -medio; d <= medio; d++)
                {
                    int x = cx + d;
                    if (x < 0 || x >= w) continue;
                    bool aro = borde || Mathf.Abs(d) >= medio - Mathf.Max(1, Escala / 2);
                    if (!aro) continue;
                    px[y * w + x] = (d < 0) ? HierroAlto : Hierro;
                }
            }

            // El gancho: una U abierta en las últimas filas.
            int gh = Mathf.Min(h / 6, S(4));
            for (int y = 0; y < gh; y++)
            {
                int r = gh - y;
                Pintar(px, w, h, cx - r, y, HierroAlto);
                Pintar(px, w, h, cx + r, y, Hierro);
                if (y == 0) for (int d = -r; d <= r; d++) Pintar(px, w, h, cx + d, y, Hierro);
            }

            s = Crear(px, w, h, "TenThousandYearsCadena");
            _cache[clave] = s;
            return s;
        }

        /// <summary>Burbuja: anillo claro con centro tenue -- lo que sube por la cámara del Crisol mientras corre una hornada (el progreso VISIBLE que pide el contrato §4).</summary>
        public static Sprite Burbuja()
        {
            const string clave = "burbuja";
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = S(8), h = S(8);
            var px = new Color32[w * h];
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f, r = w * 0.5f - 1f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    if (d > 1f) continue;
                    byte a = d > 0.66f ? (byte)235 : (byte)70; // anillo marcado, interior casi vacío.
                    px[y * w + x] = new Color32(255, 255, 255, a);
                }
            }

            s = Crear(px, w, h, "TenThousandYearsBurbuja");
            _cache[clave] = s;
            return s;
        }

        // =================================================================
        // (playtest 31, ILUMINACIÓN DE ÁNIMO) HALOS DE LUZ Y SOMBRAS PROPIAS
        // =================================================================
        // Cesar, sobre el taller: "iluminación... que el jugador quiera pasar
        // horas ahí". El proyecto no tiene (ni quiere) luces reales: la sim
        // es una textura en Point y el fondo otra, así que una Light2D de URP
        // ni siquiera las tocaría. Lo que SÍ funciona -- y es lo que hacen
        // los juegos 2D de esta familia -- es un sprite radial encima:
        //
        //  · EL HALO se dibuja DELANTE de la sim (sortingOrder 40 vs. -5) con
        //    alfa baja y color cálido: al componer sobre un cuadro ya
        //    oscurecido (SimRenderer.TinteGlobal), la zona iluminada sube de
        //    brillo Y de temperatura de color, que es exactamente lo que hace
        //    una hoguera en una cueva. No es aditivo real (haría falta un
        //    material, y Shader.Find está prohibido desde el playtest 2), pero
        //    a estas alfas la diferencia perceptual es mínima y el coste es
        //    CERO: un SpriteRenderer quieto al que solo se le cambia el alfa.
        //  · LA SOMBRA es el mismo sprite invertido en color (negro) y
        //    achatado, colocado bajo el cuerpo de cada estación: es lo que
        //    hace que una máquina se APOYE en el suelo en vez de estar pegada
        //    con celo sobre la piedra (el "programmer art" del playtest 26).
        //
        // REGLA: los halos son GameObjects hijos de la MÁQUINA (así la
        // mudanza los arrastra sin código extra, ver regla 36) y jamás tocan
        // el renderer de celdas.
        // =================================================================

        /// <summary>Orden de dibujo de los halos: delante de la sim (-5) y de toda la maquinaria (14..23), detrás del aprendiz (50) y del haz del frasco (60).</summary>
        public const int OrdenHalo = 40;
        /// <summary>Orden de las sombras propias: delante de la sim (para que se vean sobre la piedra) pero detrás del cuerpo de la máquina.</summary>
        public const int OrdenSombra = 10;

        /// <summary>
        /// Halo radial blanco con caída suave (se tinta con
        /// SpriteRenderer.color). El exponente 2.2 de la caída no es
        /// decorativo: con caída lineal el borde del halo se ve como un
        /// círculo dibujado, y con caída cuadrática pura el centro se apaga
        /// demasiado pronto -- 2.2 es el punto en que el halo "no tiene
        /// borde" pero sigue teniendo un núcleo claro.
        ///
        /// =============================================================
        /// (playtest 33) 2.2 -&gt; **3.6**: EL FIN DEL STICKER
        /// =============================================================
        /// Veredicto literal de Cesar sobre la luz del 31/32: *"la LUZ sobre
        /// el horno no es mala pero se ve OMNIPRESENTE, parece PEGADA EN LA
        /// PANTALLA, no se siente parte del horno... que no parezca un
        /// STICKER pegado"*. Tenía dos causas y esta es la general (la
        /// específica del Crisol se arregla en Game/Crisol.cs, bajando los
        /// diámetros y añadiendo luz de muro recortada).
        ///
        /// LA CAUSA: con exponente 2.2, a la mitad del radio el halo todavía
        /// conserva el 22% de su alfa, y al 75% del radio un 6% -- o sea que
        /// un halo de 46 celdas de diámetro seguía tiñendo de naranja TODO lo
        /// que hubiera a 17 celdas del hogar, techo incluido. Un disco tan
        /// grande y tan uniforme no se lee como "luz que emite algo", se lee
        /// como una capa encima del cuadro, que es literalmente lo que Cesar
        /// describió. Con 3.6: 8% a mitad de radio y 1% a tres cuartos -- el
        /// núcleo sigue igual de claro (el llamante no tiene que resubir
        /// ningún alfa) pero la luz MUERE cerca de su fuente, que es lo que
        /// hace una hoguera.
        ///
        /// POR QUÉ SE ARREGLA AQUÍ Y NO EN CADA MÁQUINA: este sprite lo
        /// comparten TODAS las fuentes del taller (hogar y brasero del
        /// Crisol, lámpara del Banco de Chispa, vidrio de la Columna, hogar
        /// del Ensayo, destellos de las redomas del estante). El mismo
        /// defecto lo tenían todas -- Cesar pidió explícitamente aplicar el
        /// criterio también a la lámpara y a las redomas -- y varios de esos
        /// archivos no son editables en este encargo. Un cambio en la FÁBRICA
        /// los alcanza a todos sin tocar una línea suya, que además es lo
        /// correcto: la forma de la caída de la luz es una decisión del
        /// taller, no de cada aparato.
        /// </summary>
        public static Sprite Halo()
        {
            const string clave = "halo";
            if (_cache.TryGetValue(clave, out var s)) return s;

            int lado = S(32); // 96 téxeles: de sobra para que la caída no muestre bandas al escalar a 20-30 celdas de mundo.
            var px = new Color32[lado * lado];
            float c = (lado - 1) * 0.5f, r = lado * 0.5f;

            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float dx = (x - c) / r, dy = (y - c) / r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d >= 1f) continue;
                    float a = Mathf.Pow(1f - d, 3.6f);
                    px[y * lado + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            s = Crear(px, lado, lado, "TenThousandYearsHalo");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// (playtest 33) LUZ DE MURO: el otro lado del fin del sticker.
        ///
        /// Criterio nuevo que pidió Cesar: *"la luz del crisol ILUMINA SUS
        /// PROPIAS PAREDES... quizás el contenedor de paredes brillando SIN
        /// INCLUIR EL TECHO porque no tiene"*. Un halo redondo no puede hacer
        /// eso: es isótropo, así que reparte la misma luz hacia la
        /// mampostería (que sí existe) y hacia el aire de encima (donde no hay
        /// nada que iluminar). Esto es un RECTÁNGULO con caída lateral suave y
        /// **corte duro por arriba**: el gradiente sube desde la base, alcanza
        /// su máximo en el tercio inferior y se apaga del todo justo en el
        /// borde superior del sprite -- el llamante hace coincidir ese borde
        /// con la CORNISA REAL de su máquina, así que por encima no queda ni
        /// un téxel encendido. La luz tiene DUEÑO FÍSICO: baña el cuerpo de
        /// hierro y la piedra del horno, y deja el techo en penumbra.
        ///
        /// `sesgoAbajo` (0..1) decide cuánto se concentra en la base: 1 = casi
        /// todo en la primera cuarta parte (una boca de fuego), 0.4 = repartido
        /// por el cuerpo (un cuerpo que irradia).
        /// </summary>
        public static Sprite LuzDeMuro(int spanCeldas, int altoCeldas, float sesgoAbajo = 0.62f)
        {
            string clave = "luzmuro" + spanCeldas + "x" + altoCeldas + "s" + Mathf.RoundToInt(sesgoAbajo * 100f);
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = Mathf.Clamp(spanCeldas * Escala, S(8), S(400));
            int h = Mathf.Clamp(altoCeldas * Escala, S(6), S(400));
            var px = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                // Vertical: sube rápido desde la base, cae a CERO EXACTO en la
                // fila de arriba (el corte que impide que la luz toque el techo).
                float ty = y / (float)(h - 1);
                float pico = Mathf.Lerp(0.45f, 0.12f, sesgoAbajo); // dónde está el máximo.
                float v = ty <= pico
                    ? Mathf.Pow(ty / Mathf.Max(0.001f, pico), 0.55f)
                    : Mathf.Pow(1f - (ty - pico) / Mathf.Max(0.001f, 1f - pico), 1.9f);

                for (int x = 0; x < w; x++)
                {
                    // Horizontal: coseno recortado -- llega vivo hasta los muros
                    // laterales y se apaga fuera, sin dibujar un borde recto.
                    float tx = (x / (float)(w - 1)) * 2f - 1f;
                    float lat = Mathf.Pow(Mathf.Clamp01(1f - tx * tx), 0.85f);
                    float a = v * lat;
                    if (a <= 0.004f) continue;
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }

            s = Crear(px, w, h, "TenThousandYearsLuzDeMuro");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// Sombra propia: elipse achatada muy difusa (se tinta de negro con
        /// alfa por el llamante). Achatada 1:3 porque la luz del taller viene
        /// de arriba -- una sombra redonda bajo una máquina se lee como un
        /// agujero, no como un apoyo.
        /// </summary>
        public static Sprite SombraSuave()
        {
            const string clave = "sombrasuave";
            if (_cache.TryGetValue(clave, out var s)) return s;

            int w = S(32), h = S(11);
            var px = new Color32[w * h];
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            float rx = w * 0.5f, ry = h * 0.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / rx, dy = (y - cy) / ry;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d >= 1f) continue;
                    float a = Mathf.Pow(1f - d, 1.6f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            s = Crear(px, w, h, "TenThousandYearsSombraSuave");
            _cache[clave] = s;
            return s;
        }

        /// <summary>
        /// UNA fuente de luz del taller. La crea la máquina en su BuildVisual
        /// (hija de su transform: la mudanza la arrastra sola) y la conduce
        /// desde su ActualizarVisual con <see cref="Intensidad"/> o
        /// <see cref="Latir"/>. Cero allocs por frame: solo escribe un Color
        /// en un SpriteRenderer que ya existe.
        /// </summary>
        public sealed class Luz
        {
            private readonly SpriteRenderer _sr;
            private readonly Color _color;

            private Luz(SpriteRenderer sr, Color color) { _sr = sr; _color = color; }

            /// <summary>
            /// `diametroMundo` es el DIÁMETRO del halo en unidades de mundo
            /// (multiplicar celdas por SimRenderer.CellWorldSize). Nace
            /// apagado: que encienda es decisión de la máquina, nunca del
            /// constructor -- una lámpara que se enciende sola al construirse
            /// miente sobre el estado del aparato (misma lección que el
            /// "cargadme combustible" del playtest 26).
            /// </summary>
            public static Luz Crear(Transform padre, string nombre, Vector3 posicionMundo, float diametroMundo, Color color)
            {
                var go = new GameObject(nombre);
                go.transform.SetParent(padre, false);
                go.transform.position = posicionMundo;
                var sr = CrearCapa(go.transform, "Halo", Halo(), OrdenHalo, diametroMundo, diametroMundo);
                sr.color = new Color(color.r, color.g, color.b, 0f);
                return new Luz(sr, color);
            }

            /// <summary>Igual que <see cref="Crear"/> pero con el halo achatado (fuentes alargadas: una ranura, un lecho de brasas, un fuste de vidrio).</summary>
            public static Luz CrearOvalada(Transform padre, string nombre, Vector3 posicionMundo, float anchoMundo, float altoMundo, Color color)
            {
                var go = new GameObject(nombre);
                go.transform.SetParent(padre, false);
                go.transform.position = posicionMundo;
                var sr = CrearCapa(go.transform, "Halo", Halo(), OrdenHalo, anchoMundo, altoMundo);
                sr.color = new Color(color.r, color.g, color.b, 0f);
                return new Luz(sr, color);
            }

            /// <summary>
            /// (playtest 33) LUZ CON DUEÑO FÍSICO: en vez de un disco, un
            /// rectángulo que cubre EXACTAMENTE la mampostería del propio
            /// aparato y se apaga a cero en su borde superior -- ver
            /// <see cref="LuzDeMuro"/>. `posicionMundo` es el CENTRO del rect,
            /// así que el llamante lo calcula desde su huella real
            /// (`(outY0+outY1)/2`), nunca a ojo: si el sprite sobresaliera por
            /// arriba volveríamos a tener luz sobre un techo que no existe.
            /// </summary>
            public static Luz CrearMuro(Transform padre, string nombre, Vector3 posicionMundo,
                int spanCeldas, int altoCeldas, float anchoMundo, float altoMundo, Color color, float sesgoAbajo = 0.62f)
            {
                var go = new GameObject(nombre);
                go.transform.SetParent(padre, false);
                go.transform.position = posicionMundo;
                var sr = CrearCapa(go.transform, "Halo", LuzDeMuro(spanCeldas, altoCeldas, sesgoAbajo), OrdenHalo, anchoMundo, altoMundo);
                sr.color = new Color(color.r, color.g, color.b, 0f);
                return new Luz(sr, color);
            }

            /// <summary>Alfa fija 0..1 (0 = apagada).</summary>
            public void Intensidad(float alfa)
            {
                if (_sr == null) return;
                _sr.color = new Color(_color.r, _color.g, _color.b, Mathf.Clamp01(alfa));
            }

            /// <summary>
            /// Luz VIVA: `centro` ± `amplitud` a `hz`. El desfase por
            /// instancia (`desfase`) evita que todas las fuentes del taller
            /// respiren a la vez, que es lo que delata una animación por
            /// código -- un fuego y una lámpara no laten sincronizados.
            /// </summary>
            public void Latir(float centro, float amplitud, float hz, float desfase = 0f)
            {
                if (_sr == null) return;
                float a = centro + amplitud * Mathf.Sin((Time.time * hz + desfase) * Mathf.PI * 2f);
                _sr.color = new Color(_color.r, _color.g, _color.b, Mathf.Clamp01(a));
            }
        }

        /// <summary>
        /// Coloca una sombra propia bajo un cuerpo: hija de `padre` (la
        /// mudanza la arrastra), tintada de negro con la opacidad indicada.
        /// Devuelve el renderer por si el llamante quiere modularla.
        /// </summary>
        public static SpriteRenderer Sombra(Transform padre, Vector3 posicionMundo, float anchoMundo, float altoMundo, float opacidad)
        {
            var go = new GameObject("Sombra");
            go.transform.SetParent(padre, false);
            go.transform.position = posicionMundo;
            var sr = CrearCapa(go.transform, "Sprite", SombraSuave(), OrdenSombra, anchoMundo, altoMundo);
            sr.color = new Color(0f, 0f, 0f, opacidad);
            return sr;
        }

        /// <summary>Píxel suelto con guarda de límites -- azúcar interno de la familia del playtest 27.</summary>
        private static void Pintar(Color32[] px, int w, int h, int x, int y, Color32 c)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) return;
            px[y * w + x] = c;
        }

        /// <summary>Sprite blanco de 1x1 para barras/rellenos genéricos (se tinta con SpriteRenderer.color).</summary>
        public static Sprite Solido()
        {
            const string clave = "solido";
            if (_cache.TryGetValue(clave, out var s)) return s;
            s = Crear(new[] { new Color32(255, 255, 255, 255) }, 1, 1, "TenThousandYearsSolido");
            _cache[clave] = s;
            return s;
        }

        // =================================================================
        // (playtest 30, MÁQUINAS EN RED — Net/MaquinaSync.cs) RÉPLICA
        // VISUAL ESTÁTICA
        // =================================================================
        /// <summary>
        /// Convención de <c>tipoMaquina</c> compartida con
        /// <c>Alkahest.Net.MaquinaSync.TipoMaquina</c> (Net/MaquinaSync.cs).
        /// Este archivo es SOLO-AÑADIR en este encargo (Game/ no puede
        /// depender de Net/ sin invertir la dirección natural de referencias
        /// del proyecto -- Net/ SÍ conoce Game/, nunca al revés, ver
        /// Net/AprendizNet.cs), así que los valores se repiten aquí como
        /// constantes de byte EN VEZ de compartir el enum. Si algún día uno
        /// de los dos lados cambia de orden, este comentario es la costura
        /// que hay que actualizar a la vez.
        /// </summary>
        public const byte TipoCrisol = 0;
        public const byte TipoPrensa = 1;
        public const byte TipoBancoChispa = 2;
        public const byte TipoColumnaEnsayo = 3;
        public const byte TipoEnsayoMaestro = 4;
        public const byte TipoDispenser = 5;
        /// <summary>(fix Cesar playtest 33, sistema de baldas/anclajes) Nuevos tipos que entran al registro de Net/MaquinaSync.cs junto a los seis de siempre -- ver Game/Balda.cs/Game/Anclaje.cs.</summary>
        public const byte TipoBalda = 6;
        public const byte TipoAnclaje = 7;

        /// <summary>
        /// (playtest 36, EL CAMINO DEL INVITADO) Rack/Alambique/Pila entran
        /// AQUÍ, en el mismo archivo que quedó fuera de alcance en el
        /// playtest 34/35 (ver el docblock viejo de
        /// <see cref="ConstruirVisualEstatico"/>, conservado más abajo salvo
        /// por esta correción): antes caían al <c>default: Solido()</c>
        /// genérico -- un rectángulo blanco sin tintar, la mitad del reporte
        /// de Cesar ("las réplicas de estante/alambique/pilas son
        /// rectángulos blancos"). Mismos valores numéricos que
        /// <c>Net.MaquinaSync.TipoMaquina</c> (8/9/10) -- ver el docblock de
        /// esa clase para por qué se repiten en vez de compartir el enum.
        /// </summary>
        public const byte TipoRack = 8;
        public const byte TipoAlambique = 9;
        public const byte TipoPila = 10;

        /// <summary>
        /// (CONTRATO_TERMICA.md §1/§3b, ENCARGO I) LAS DOS PLACAS: valores
        /// CONGELADOS por el contrato entre encargos (11/12 EXACTOS, mismos
        /// que <c>Net.MaquinaSync.TipoMaquina.PlacaCalor/PlacaFria</c>).
        /// </summary>
        public const byte TipoPlacaCalor = 11;
        public const byte TipoPlacaFria = 12;

        /// <summary>
        /// UNA sola pieza representativa por tipo de estación, escalada
        /// EXACTA a <paramref name="tamanoMundo"/> vía <see cref="CrearCapa"/>
        /// (que reescala cualquier sprite ya generado al hueco de mundo que
        /// haga falta, sin regenerar texturas). Construye la RÉPLICA
        /// SOLO-VISUAL de un invitado (ver Net/MaquinaSync.cs y
        /// Net/MaquinaReplica.cs): NO es la rig completa multicapa de la
        /// máquina real (el Crisol real junta panza+cesto+labio+embudo en
        /// piezas separadas con offsets propios, la Prensa mandíbula+volante,
        /// etc.) -- reproducir esa geometría exacta aquí acoplaría este
        /// archivo a constantes PRIVADAS de cada Game/*.cs (que además cambian
        /// de ronda en ronda, ver el historial de Crisol.cs), y el encargo de
        /// la réplica es "SIN lógica, sin OnGUI de interacción" -- una silueta
        /// reconocible con la chapa del nombre encima ya cumple ese contrato.
        /// DECISIÓN: fidelidad reducida a UNA pieza por estación en vez de la
        /// rig completa; documentado aquí para que quien la eche en falta
        /// sepa por qué y dónde ampliarla si hace falta más adelante.
        ///
        /// (playtest 36) CUATRO tipos son COMPUESTOS de dos o tres piezas
        /// (Balda: piedra+cuadritos; Rack: listón+redomas; Alambique:
        /// domo+matraz) en vez de una sola -- lo pide el mueble real (ver los
        /// helpers <c>ConstruirReplica*</c> más abajo). NUNCA blanco sin
        /// tintar: toda pieza de aquí sale de la MISMA fábrica que usan las
        /// máquinas reales, con los colores reales del taller (latón/
        /// carboncillo/piedra/vidrio) horneados en la textura -- si algún día
        /// hace falta <see cref="Solido"/> en esta familia, SIEMPRE con
        /// <c>SpriteRenderer.color</c> puesto a un tono del taller, nunca
        /// blanco de fábrica (ver <see cref="ColorCarboncilloReplica"/>).
        /// Devuelve el <see cref="SpriteRenderer"/> creado (hijo nuevo de
        /// `padre`) por si el llamante quiere teñirlo o atenuarlo -- para los
        /// tipos compuestos es la pieza PRINCIPAL (piedra/listón/domo): las
        /// piezas secundarias son hijas del mismo `padre` y se mueven solas
        /// con él (mismo mecanismo que arrastra los halos/sombras de una
        /// máquina real, ver Reposicionar en Net/MaquinaReplica.cs), pero
        /// <see cref="MaquinaReplica.ActualizarDesdeRegistro"/> solo
        /// reescala la pieza devuelta -- aceptable porque, como documenta esa
        /// clase, "ninguna estación cambia de tamaño en este POC".
        /// </summary>
        public static SpriteRenderer ConstruirVisualEstatico(Transform padre, byte tipoMaquina, Vector2 tamanoMundo)
        {
            float ancho = Mathf.Max(0.02f, tamanoMundo.x);
            float alto = Mathf.Max(0.02f, tamanoMundo.y);

            switch (tipoMaquina)
            {
                case TipoBalda: return ConstruirReplicaBalda(padre, ancho, alto);
                case TipoRack: return ConstruirReplicaRack(padre, ancho, alto);
                case TipoAlambique: return ConstruirReplicaAlambique(padre, ancho, alto);
                // (integración pt55, B3: "marco dorado raro con grietas" en la
                // captura del invitado) MarcoBandeja(10,5) horneaba una textura
                // 2:1 que se estiraba a la huella real de la Pila (14x9 celdas,
                // ~1.56:1): el borde y las cartelas diagonales se deformaban y
                // se leían como "grietas". La proporción de la textura ahora
                // SIGUE a la huella real recibida.
                case TipoPila:
                {
                    int texW = Mathf.Clamp(Mathf.RoundToInt(ancho / Mathf.Max(0.02f, SimRenderer.CellWorldSize)), 6, 32);
                    int texH = Mathf.Clamp(Mathf.RoundToInt(alto / Mathf.Max(0.02f, SimRenderer.CellWorldSize)), 4, 32);
                    return CrearCapa(padre, "ReplicaVisualEstatica", MarcoBandeja(texW, texH), ReplicaVisualSortingOrder, ancho, alto);
                }
            }

            Sprite pieza;
            switch (tipoMaquina)
            {
                case TipoCrisol: pieza = PanzaCrisol(13, 9, 1, 1); break;
                case TipoPrensa: pieza = MandibulaPrensa(10, 6); break;
                case TipoBancoChispa: pieza = Electrodo(6, 10); break;
                case TipoColumnaEnsayo: pieza = VidrioPanel(4, 14); break;
                case TipoEnsayoMaestro: pieza = Dosel(10, 6); break;
                case TipoDispenser: pieza = CanoGrifo(); break;
                case TipoAnclaje: pieza = CuadradoAnclaje(); break;
                // (CONTRATO_TERMICA.md §3b, ENCARGO I; sprite actualizado en
                // el playtest 48, CONTRATO_RONDA48.md §3a) LAS DOS PLACAS: se
                // reutiliza la MISMA fábrica de sprites que ya usan las
                // máquinas reales (Game/HeatPlate.cs::BuildVisual /
                // Game/ChillStone.cs::BuildVisual) -- la losa de piedra
                // (ex-chasis metálico, ver LosaPlaca) basta como silueta
                // reconocible de UNA pieza (mismo criterio de fidelidad
                // reducida que el resto de esta familia); la fría usa el
                // bloque gélido en vez de los cristales sueltos porque como
                // pieza ÚNICA se lee mejor a escala de réplica (más
                // superficie, menos "puntitos").
                case TipoPlacaCalor: pieza = LosaPlaca(10); break;
                case TipoPlacaFria: pieza = BloqueGelido(10); break;
                // red de seguridad: tipo desconocido -> Solido() SIEMPRE
                // tintado de carboncillo (ver ColorCarboncilloReplica) -- nunca el
                // blanco de fábrica de Solido(), que es justo el bug que
                // motivó esta ronda para Rack/Alambique/Pila.
                default:
                    var sr = CrearCapa(padre, "ReplicaVisualEstatica", Solido(), ReplicaVisualSortingOrder, ancho, alto);
                    sr.color = ColorCarboncilloReplica;
                    return sr;
            }

            return CrearCapa(padre, "ReplicaVisualEstatica", pieza, ReplicaVisualSortingOrder, ancho, alto);
        }

        /// <summary>Tono de emergencia para <see cref="Solido"/> cuando esta familia lo usa como red de seguridad -- carboncillo del taller (mismo valor que <see cref="Hierro"/>), NUNCA blanco. Ver el docblock de <see cref="ConstruirVisualEstatico"/>.</summary>
        private static readonly Color ColorCarboncilloReplica = new Color(0x45 / 255f, 0x3D / 255f, 0x38 / 255f, 1f);

        /// <summary>
        /// (playtest 36) BALDA: la piedra tallada de <see cref="BaldaPiedra"/>
        /// más los dos "cuadritos" de latón en cada extremo
        /// (<see cref="CuadradoAnclaje"/>) que hacen que se lea como MUEBLE y
        /// no como una raya de roca (mismo lenguaje que
        /// Game/Balda.cs::BuildVisual) -- tamaño derivado del propio alto de
        /// la réplica (el remate real mide el DOBLE de la losa, "asomado"),
        /// nunca una constante absoluta.
        /// </summary>
        private static SpriteRenderer ConstruirReplicaBalda(Transform padre, float ancho, float alto)
        {
            var principal = CrearCapa(padre, "ReplicaVisualEstatica", BaldaPiedra(8, 1), ReplicaVisualSortingOrder, ancho, alto);

            float remate = Mathf.Min(alto * 2f, ancho * 0.22f);
            if (remate > 0.01f)
            {
                var izq = new GameObject("RemateIzq");
                izq.transform.SetParent(padre, false);
                izq.transform.position = padre.position + new Vector3(-ancho * 0.5f + remate * 0.5f, 0f, 0f);
                CrearCapa(izq.transform, "Sprite", CuadradoAnclaje(), ReplicaVisualSortingOrder + 1, remate, remate);

                var der = new GameObject("RemateDer");
                der.transform.SetParent(padre, false);
                der.transform.position = padre.position + new Vector3(ancho * 0.5f - remate * 0.5f, 0f, 0f);
                CrearCapa(der.transform, "Sprite", CuadradoAnclaje(), ReplicaVisualSortingOrder + 1, remate, remate);
            }

            return principal;
        }

        /// <summary>
        /// (playtest 36) RACK (estante de redomas): el LISTÓN
        /// (<see cref="ListonEstante"/>) ocupa la banda inferior y tres
        /// redomas de vidrio (<see cref="VidrioRedoma"/>, silueta vacía --
        /// una réplica no conoce el contenido de cada frasco real, y teñirlas
        /// al azar mentiría sobre qué hay en el estante) se paran encima --
        /// mismo lenguaje visual que Game/StorageRack.cs, reducido a UNA fila
        /// representativa (fidelidad reducida, ver el docblock de
        /// <see cref="ConstruirVisualEstatico"/>).
        /// </summary>
        private static SpriteRenderer ConstruirReplicaRack(Transform padre, float ancho, float alto)
        {
            float altoListon = Mathf.Min(alto * 0.22f, alto);
            var liston = CrearCapa(padre, "ReplicaVisualEstatica", ListonEstante(220), ReplicaVisualSortingOrder, ancho, altoListon);
            liston.transform.position = padre.position + new Vector3(0f, -alto * 0.5f + altoListon * 0.5f, 0f);

            float altoRedoma = alto - altoListon;
            if (altoRedoma > 0.01f)
            {
                const int n = 3;
                float anchoRedoma = Mathf.Min(ancho / (n * 1.6f), altoRedoma * 0.55f);
                float paso = ancho / (n + 1);
                for (int i = 0; i < n; i++)
                {
                    float x = -ancho * 0.5f + paso * (i + 1);
                    var go = new GameObject("Redoma_" + i);
                    go.transform.SetParent(padre, false);
                    go.transform.position = padre.position + new Vector3(x, -alto * 0.5f + altoListon + altoRedoma * 0.5f, 0f);
                    CrearCapa(go.transform, "Sprite", VidrioRedoma(), ReplicaVisualSortingOrder + 1, anchoRedoma, altoRedoma * 0.92f);
                }
            }

            return liston;
        }

        /// <summary>
        /// (playtest 36) ALAMBIQUE: DOMO arriba + MATRAZ abajo, los dos con
        /// <see cref="VidrioPanel"/> (mismo sprite que usa Game/Alambique.cs
        /// de verdad para las dos piezas) en las MISMAS proporciones
        /// 9:9/9:5 (DomoAlto/MatrazAlto) que el aparato real, así que una
        /// réplica alta y una baja se leen con la misma silueta
        /// domo-sobre-matraz que el jugador ya conoce.
        /// </summary>
        private static SpriteRenderer ConstruirReplicaAlambique(Transform padre, float ancho, float alto)
        {
            const float fracDomo = 9f / 14f; // DomoAlto / (DomoAlto+MatrazAlto), Game/Alambique.cs.
            float altoDomo = alto * fracDomo;
            float altoMatraz = alto - altoDomo;

            var domo = CrearCapa(padre, "ReplicaVisualEstatica", VidrioPanel(9, 9), ReplicaVisualSortingOrder, ancho * 0.8f, altoDomo);
            domo.transform.position = padre.position + new Vector3(0f, alto * 0.5f - altoDomo * 0.5f, 0f);

            var matraz = new GameObject("Matraz");
            matraz.transform.SetParent(padre, false);
            matraz.transform.position = padre.position + new Vector3(0f, -alto * 0.5f + altoMatraz * 0.5f, 0f);
            CrearCapa(matraz.transform, "Sprite", VidrioPanel(9, 5), ReplicaVisualSortingOrder, ancho, altoMatraz);

            return domo;
        }

        /// <summary>Orden de dibujo de la réplica: por debajo del aprendiz (47-50, ver Net/AprendizNet.cs/ApprenticeController) y de la silueta genérica de Mudanza (44) -- una réplica nunca debe tapar la sombra de arrastre que se dibuja sobre ella.</summary>
        private const int ReplicaVisualSortingOrder = 20;

        // =================================================================
        private static void MarcarRemache(Color32[] px, int w, int h, int cx, int cy, Color32 color)
        {
            // (fix playtest 6: baja resolución) el remache medía 2x2 téxeles fijos
            // en el diseño original (a 2 téxeles/celda); escalado por Escala para
            // que siga leyéndose como un remache y no un punto perdido en el metal.
            for (int dy = 0; dy < Escala; dy++)
            {
                for (int dx = 0; dx < Escala; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || x >= w || y < 0 || y >= h) continue;
                    px[y * w + x] = color;
                }
            }
        }

        /// <summary>Taco cuadrado macizo de `size` téxeles en (x0,y0) -- usado por <see cref="MarcoContenedor"/> para el "remate" de sus cuatro esquinas.</summary>
        private static void MarcarRemateCuadrado(Color32[] px, int w, int h, int x0, int y0, int size, Color32 color)
        {
            for (int dy = 0; dy < size; dy++)
            {
                for (int dx = 0; dx < size; dx++)
                {
                    int x = x0 + dx, y = y0 + dy;
                    if (x < 0 || x >= w || y < 0 || y >= h) continue;
                    px[y * w + x] = color;
                }
            }
        }

        /// <summary>Crea el sprite a 1 píxel = 1 unidad (ver doc de la clase) con filtrado Point y sin mipmaps.</summary>
        private static Sprite Crear(Color32[] px, int w, int h, string nombre)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = nombre,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
        }
    }
}
