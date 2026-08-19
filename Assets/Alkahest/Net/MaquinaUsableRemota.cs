namespace Alkahest.Net
{
    /// <summary>
    /// ENCARGO N (LA PARIDAD VIVA, playtest 43, docs/CONTRATO_PARIDAD.md §1/§2a)
    /// EL GANCHO DE E REMOTO. Cada una de las siete estaciones del taller
    /// (Crisol/Prensa/BancoChispa/ColumnaEnsayo/EnsayoMaestro/Alambique/
    /// Dispenser) la implementa SOBRE SU CLASE REAL -- solo vive en el
    /// anfitrión, nunca en <see cref="MaquinaReplica"/> (que es
    /// SOLO-VISUAL, ver su docblock).
    ///
    /// <see cref="UsarPorRed"/> tiene que ejecutar EXACTAMENTE lo mismo que
    /// el handler de E LOCAL de esa máquina -- en la práctica, esto es
    /// literalmente el mismo método privado que el `Update()` de cada
    /// estación ya llama tras comprobar `EstaEnfocada()`: ese chequeo de
    /// PROXIMIDAD DEL ANFITRIÓN vive en `Update()`, no dentro del handler,
    /// así que el handler en sí NUNCA tuvo que refactorizarse para poder
    /// reutilizarlo aquí -- la proximidad del invitado la valida su propia
    /// réplica (ver <see cref="MaquinaReplica"/>) y la cordura del pedido la
    /// valida <see cref="MaquinaSync.SolicitarUsoServerRpc"/> del lado del
    /// servidor (radio generoso, anti-teleuso). Devuelve false si la acción
    /// no procede (cámara vacía, sin Favor, ya trabajando...) -- exactamente
    /// el mismo criterio de "no pasa nada" que el E local, para que un
    /// invitado nunca vea gastarse su pulsación en silencio de otra manera
    /// que como ya le pasaría al anfitrión.
    /// </summary>
    public interface IMaquinaUsableRemota
    {
        bool UsarPorRed();
        byte EstadoVivoRed();
    }

    /// <summary>
    /// LOS BITS DE <see cref="MaquinaSync.EntradaMaquina.estadoVivo"/> —
    /// CONGELADOS por el contrato §1 (docs/CONTRATO_PARIDAD.md): la posición
    /// exacta de cada bit es la API que consume el ENCARGO A (audio del
    /// invitado) en paralelo, así que no se reordenan ni se reutilizan para
    /// otra cosa. bits 5-7: reserva explícita del contrato, sin uso hoy.
    /// </summary>
    public static class EstadoVivoBits
    {
        /// <summary>Hornada/prensada/análisis EN CURSO (el pulso "estoy trabajando" que ya usan las máquinas reales vía MaquinariaSprites.AffordanceGlow).</summary>
        public const byte Trabajando = 1 << 0;
        /// <summary>El brasero del Crisol arde con llama/brasas de verdad (ver Crisol._cestoArdiendo).</summary>
        public const byte FuegoEncendido = 1 << 1;
        /// <summary>Hay un resultado REPOSANDO en la cubeta/matraz esperando que el jugador lo recoja.</summary>
        public const byte ResultadoListo = 1 << 2;
        /// <summary>El grifo está abierto (Dispenser._on).</summary>
        public const byte Sirviendo = 1 << 3;
        /// <summary>La lámpara del Banco de Chispa dictamina a pleno brillo (conductividad 2).</summary>
        public const byte LuzPlena = 1 << 4;
    }
}
