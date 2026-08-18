using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// [ChaosAlchemy · playtest 25, CONTRATO_PERSISTE.md §6.3] Registro
    /// GLOBAL de las operaciones de máquina que transforman materia
    /// (Crisol/Prensa/BancoChispa, encargo B) -- la unidad trazable de una
    /// patente. API CONGELADA que llama B:
    ///
    /// <code>Hornada.RegistrarOp(string maquina, byte matEntrada, byte matSalida, string condicion);</code>
    ///
    /// ESTÁTICA A PROPÓSITO (como pide el contrato): no hay "una Hornada por
    /// lote", hay UN registro por partida. Ring buffer FIJO de las últimas
    /// <see cref="RingSize"/> ops (struct, array preasignado): RegistrarOp no
    /// asigna nada -- los strings de <c>maquina</c>/<c>condicion</c> son
    /// literales o cacheados por el llamante, aquí solo se guarda la
    /// referencia.
    ///
    /// LIMITACIÓN v0 (documentada, aceptada por el contrato): el registro es
    /// GLOBAL, no por-lote. Si el jugador intercala dos procesos distintos
    /// (por ejemplo: mete polvo A en el crisol, lo saca a medias, mete polvo
    /// B, vuelve con A...) la cadena que se congela al patentar puede
    /// mezclar pasos de los dos. Es award barato y deliberado: separar
    /// "lotes" de verdad exigiría que cada máquina llevara su propio
    /// identificador de lote y que las celdas lo arrastraran consigo (un
    /// campo por celda que HOY no existe y que el contrato de LO QUE
    /// PERSISTE no pide para v0 -- ver docs/DISENO_LO_QUE_PERSISTE.md §6,
    /// "Registro por HORNADA" se corta aquí a propósito).
    ///
    /// PATENTAR: cuando <see cref="RegistrarOp"/> produce un
    /// <c>matSalida</c> (base,estado) que NUNCA se había producido esta
    /// partida (<see cref="MaterialId.EsBaseEstado"/>, verificado con A),
    /// se CONGELA automáticamente la cadena de hasta
    /// <see cref="PasosPorPatente"/> ops más recientes del ring buffer
    /// (incluida la que acaba de disparar el descubrimiento) en una
    /// <see cref="Patente"/> nueva, sin nombre todavía. El aviso en pantalla
    /// ("PATENTE DISPONIBLE", estilo LEY DESCUBIERTA) y el bautizo viven en
    /// SubstanceKnowledge.cs/JournalHud.cs -- este archivo solo GUARDA datos
    /// y expone <see cref="PatentesVersion"/> para que esos dos sepan cuándo
    /// hay algo nuevo que mostrar, mismo patrón que
    /// SubstanceKnowledge.NamingVersion/LeyesVersion.
    /// </summary>
    public static class Hornada
    {
        /// <summary>Ring buffer de operaciones: potencia de 2 para máscara barata, tamaño exacto del contrato.</summary>
        private const int RingSize = 8;

        /// <summary>"Hasta 4 hacia atrás" (contrato §6.4): la cadena congelada de una patente nunca es más larga que esto.</summary>
        private const int PasosPorPatente = 4;

        /// <summary>
        /// (fix Cesar playtest 33, "LA MUERTE DEL AUTO-PATENTE DE 1 PASO")
        /// Cesar, literal: *"caliento la primera arena y me sale 'has
        /// descubierto un procedimiento'... patentar solo debería valer la
        /// pena para procesos de AL MENOS 2 pasos"*. Antes de este fix,
        /// <see cref="CongelarPatente"/> congelaba con
        /// <c>Mathf.Min(PasosPorPatente, _ringCount)</c>, que en el PRIMER
        /// descubrimiento de la partida vale 1 (el ring buffer solo lleva un
        /// paso escrito) -- un procedimiento de una sola línea no es un
        /// "procedimiento", es literalmente lo que se acaba de hacer una vez.
        /// </summary>
        private const int MinPasosParaPatente = 2;

        /// <summary>Tope de patentes registrables en una partida -- ~40 variantes base×estado posibles (MaterialId.BasesCount*8), 16 es margen amplio sin reservar de más. PÚBLICA a propósito: JournalHud.cs dimensiona su array de entradas de PROCEDIMIENTOS contra este mismo número, para no duplicar el "16" como magia en dos archivos.</summary>
        public const int MaxPatentes = 16;

        /// <summary>Un paso congelado dentro de una patente: qué máquina, con qué entrada/salida y bajo qué condición.</summary>
        public struct PasoPatente
        {
            public string Maquina;
            public byte MatEntrada;
            public byte MatSalida;
            public string Condicion;
        }

        /// <summary>Una patente ya congelada (bautizada o no). El array de pasos está en orden cronológico (el más antiguo primero).</summary>
        public struct Patente
        {
            public byte MatResultado;
            public PasoPatente[] Pasos;
            public string Nombre; // null hasta bautizar.
        }

        // -----------------------------------------------------------------
        // Ring buffer de ops (RegistrarOp no asigna nada).
        // -----------------------------------------------------------------
        private static readonly PasoPatente[] _ring = new PasoPatente[RingSize];
        private static int _ringHead; // próximo índice a escribir.
        private static int _ringCount; // cuántas entradas válidas hay (<=RingSize).

        // Qué (base,estado) ya se han producido esta partida -- indexado por
        // matId, tamaño MaterialId.Count (58 con Limo+bases×estados de A).
        private static readonly bool[] _producido = new bool[MaterialId.Count];

        private static readonly Patente[] _patentes = new Patente[MaxPatentes];
        private static int _patentesCount;

        /// <summary>Sube cada vez que se congela una patente NUEVA o se (re)bautiza una existente -- mismo patrón que SubstanceKnowledge.NamingVersion/LeyesVersion, para que JournalHud/SubstanceKnowledge sepan cuándo reconstruir texto cacheado.</summary>
        public static int PatentesVersion { get; private set; }

        /// <summary>
        /// Reinicio de partida (playtest 25): estática = sobrevive a un
        /// reload de escena si el dominio no se recarga, así que
        /// AlkahestGameBootstrap debe llamar a esto al arrancar una partida
        /// nueva -- mismo criterio que MachineFocus.Limpiar().
        /// </summary>
        public static void Limpiar()
        {
            _ringHead = 0;
            _ringCount = 0;
            System.Array.Clear(_producido, 0, _producido.Length);
            _patentesCount = 0;
            PatentesVersion = 0;
        }

        /// <summary>API CONGELADA (contrato §6.3): la llama B desde Crisol/Prensa/BancoChispa cada vez que una operación transforma materia de verdad.</summary>
        public static void RegistrarOp(string maquina, byte matEntrada, byte matSalida, string condicion)
        {
            _ring[_ringHead] = new PasoPatente { Maquina = maquina, MatEntrada = matEntrada, MatSalida = matSalida, Condicion = condicion };
            _ringHead = (_ringHead + 1) % RingSize;
            if (_ringCount < RingSize) _ringCount++;

            // (playtest 25) Solo (base,estado) reales patentan -- el limo, el
            // agua, la piedra o cualquier vocabulario de taller que pase por
            // una máquina (p.ej. la prensa "resistiendo" sobre piedra) nunca
            // dispara una patente: no son el descubrimiento que el diseño
            // quiere premiar.
            if (matSalida >= MaterialId.Count || !MaterialId.EsBaseEstado(matSalida)) return;
            if (_producido[matSalida]) return; // ya se produjo esta partida -- solo la PRIMERA vez patenta.

            _producido[matSalida] = true;
            CongelarPatente(matSalida);
        }

        /// <summary>Congela los últimos hasta <see cref="PasosPorPatente"/> pasos del ring buffer (el más reciente es la op que acaba de disparar el descubrimiento) en una Patente nueva, sin nombre.</summary>
        private static void CongelarPatente(byte matResultado)
        {
            if (_patentesCount >= MaxPatentes) return; // defensivo: no debería agotarse en una partida real.

            int pasos = Mathf.Min(PasosPorPatente, _ringCount);
            // (fix Cesar playtest 33) Ver el docblock de MinPasosParaPatente:
            // una cadena de un solo paso nunca patenta. `_producido[matResultado]`
            // ya se marcó true en RegistrarOp ANTES de llamar aquí, así que
            // este material no vuelve a intentarlo -- correcto: si la PRIMERA
            // vez que se ve fue una cadena de 1 paso, patentarlo más tarde
            // (cuando el ring buffer ya tenga más historia) describiría una
            // cadena que no fue la que realmente lo produjo la primera vez.
            if (pasos < MinPasosParaPatente) return;
            var arr = new PasoPatente[pasos];
            // _ring es circular; el índice recién escrito es (_ringHead - 1 + RingSize) % RingSize,
            // y de ahí hacia atrás están los pasos anteriores, del más antiguo al más nuevo.
            int startIdx = (_ringHead - pasos + RingSize) % RingSize;
            for (int i = 0; i < pasos; i++)
            {
                arr[i] = _ring[(startIdx + i) % RingSize];
            }

            _patentes[_patentesCount] = new Patente { MatResultado = matResultado, Pasos = arr, Nombre = null };
            _patentesCount++;
            PatentesVersion++;
        }

        public static int PatenteCount => _patentesCount;

        /// <summary>Patente por índice (0..PatenteCount-1), en orden de descubrimiento. Struct: copia barata, sin allocs.</summary>
        public static Patente GetPatente(int index) => _patentes[index];

        /// <summary>Bautiza (o renombra) una patente por índice. Nombre vacío/solo-espacios "olvida" el nombre, igual que SubstanceKnowledge.Bautizar.</summary>
        public static void BautizarPatente(int index, string nombre)
        {
            if (index < 0 || index >= _patentesCount) return;
            _patentes[index].Nombre = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();
            PatentesVersion++;
        }

        /// <summary>¿Hay al menos una patente registrada (bautizada o no)? Contrato §6.1: es el criterio de <see cref="OrderType.Procedimiento"/>.</summary>
        public static bool TieneAlMenosUnaPatente() => _patentesCount > 0;

        /// <summary>
        /// (fix Cesar playtest 33, "LA MUERTE DEL AUTO-PATENTE DE 1 PASO",
        /// puntos c/d) ¿Ya tienen TODOS nombre -- el resultado Y cada
        /// entrada/salida de cada paso de la cadena -- los materiales de la
        /// patente `index`? Cesar, literal: *"la patente dice 'material
        /// ????'... mejor que el BAUTIZO venga al frente en vez de las
        /// patentes"*. Esta es la señal que <see cref="SubstanceKnowledge"/>
        /// usa para RETRASAR el anuncio "¡NUEVO PROCEDIMIENTO!" (y que
        /// Game/JournalHud.cs usa para decidir si ofrece el botón "Bautizar"
        /// de la patente o el aviso "bautiza sus ingredientes para
        /// patentarlo") hasta que la ficha se pueda leer sin ningún "???".
        /// </summary>
        public static bool IngredientesBautizados(int index, SubstanceKnowledge saber)
        {
            if (index < 0 || index >= _patentesCount || saber == null) return false;
            var patente = _patentes[index];
            if (!saber.EstaBautizado(patente.MatResultado)) return false;

            var pasos = patente.Pasos;
            for (int i = 0; i < pasos.Length; i++)
            {
                if (!saber.EstaBautizado(pasos[i].MatEntrada)) return false;
                if (!saber.EstaBautizado(pasos[i].MatSalida)) return false;
            }
            return true;
        }

        // (playtest 25) DESCARTADO A PROPÓSITO (regla 15 de CLAUDE.md): hubo
        // aquí un `PatenteSinBautizarMasReciente()` pensado para abrir la
        // ventana de bautizo automáticamente sobre la última patente sin
        // nombre. Se retiró SIN llegar a tener un consumidor real -- el
        // contrato solo pide un botón en la propia ficha de PROCEDIMIENTOS
        // (JournalHud.DrawEntrada), que ya resuelve "cuál bautizar" con la
        // elección explícita del jugador (clic en SU ficha), no con una
        // "más reciente" adivinada por el sistema. Si algún día hace falta
        // un atajo de teclado que salte a la próxima patente sin bautizar,
        // este es el sitio natural para reintroducirlo -- no antes de que
        // exista quien lo llame (regla 48).
    }
}
