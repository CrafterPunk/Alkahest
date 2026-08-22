using UnityEngine;

namespace Alkahest.Game
{
    /// <summary>
    /// (RONDA 66, dirección 2.5D de Cesar) LA TABLA ÚNICA DE PLANOS VISUALES.
    ///
    /// El juego siempre tuvo una escalera de sortingOrder implícita que nadie
    /// gobernaba (fondo -10, sim -5, máquinas 15-21, halos 40+, aprendiz 50,
    /// frasco en mano 60). Esta clase la NOMBRA y le añade los planos nuevos
    /// de la falsa profundidad. Regla 39: quien necesite un orden lo LEE de
    /// aquí; los valores ya validados NO se renumeraron (los archivos viejos
    /// siguen con sus literales -- migrarlos es limpieza, no urgencia; los
    /// planos NUEVOS nacen ya leyendo esta tabla).
    ///
    /// LOS 6 NIVELES DE LA DIRECCIÓN (mandato de Cesar, ronda 66):
    ///   Nivel 0 · FondoProfundo   -- pared oscura, arcos, decoración pasiva.
    ///   Nivel 1 · ParedUtil       -- repisas, carteles, lámparas, tubería de fondo.
    ///   Nivel 2 · AlmacenajeAtras -- botellas/frascos APOYADOS en repisas
    ///                                (ligeramente delante de su repisa).
    ///   Nivel 3 · EL PLANO DE JUEGO -- la simulación (-5), overlays de fx
    ///                                (-4/-3), máquinas (15..21), halos (40+),
    ///                                el APRENDIZ (50). Aquí vive todo lo real.
    ///   Nivel 4 · ArquitecturaFrente -- el LABIO de la roca madre y los
    ///                                cantos frontales de piso/plataformas:
    ///                                lo único que puede tapar parcialmente
    ///                                al personaje. La colisión NUNCA viene
    ///                                de aquí: es solo pintura (la física
    ///                                sigue siendo la grilla).
    ///   Nivel 5 · Foreground      -- cadenas, vigas, polvo ambiental (futuro).
    ///
    /// El vidrio frontal de recipientes (MachineBack -> Sim -> MachineFront)
    /// usa MaquinaFrente: delante de la sim y del cuerpo de su máquina, detrás
    /// del aprendiz -- el líquido se ve DENTRO sin 3D.
    /// </summary>
    public static class Capas
    {
        // Nivel 0
        public const int FondoProfundo = -30;      // más atrás que el backdrop actual (-10): reservado para arcos/sombras futuras.
        public const int Backdrop = -10;           // WorkshopBackdrop (valor histórico, documentado aquí).
        // Nivel 1
        public const int ParedUtil = -20;
        // Nivel 2
        public const int AlmacenajeAtras = -16;
        // Nivel 3 (los valores históricos, nombrados)
        /// <summary>
        /// (RONDA 69, el sándwich MachineBack -> Sim -> MachineFront) El
        /// FONDO INTERIOR de un recipiente: el panel que se ve POR DETRÁS de
        /// la materia, a través de las celdas vacías de la cámara. Tiene que
        /// vivir entre el backdrop (-10) y la simulación (-5): más atrás
        /// taparía la pared del cuarto sin ganar nada, más adelante taparía
        /// la propia carga. Es la mitad trasera del sándwich; la delantera
        /// es <see cref="MaquinaFrente"/>.
        /// </summary>
        public const int MaquinaFondoInterior = -8;
        public const int Simulacion = -5;          // SimRenderer.BuildQuad.
        public const int FxOverlay = -4;           // ParticulasFx / capas de criatura.
        public const int MaquinaAtras = 14;        // detrás del cuerpo de máquina (18).
        public const int MaquinaBase = 17;
        public const int MaquinaCuerpo = 18;
        public const int MaquinaDetalle = 21;
        public const int MaquinaFrente = 35;       // vidrio frontal de recipientes (nuevo).
        public const int Halos = 40;
        public const int Personaje = 50;           // ApprenticeController (campo serializado, valor histórico).
        // Nivel 4
        public const int ArquitecturaFrente = 55;  // el labio de la roca (SimRenderer._frontTexture). Tapa al personaje, no al frasco en mano.
        // Nivel 5
        public const int Foreground = 58;
        public const int CarryEnMano = 60;         // Flask._carryVisual (histórico): lo que llevas en la mano se ve SIEMPRE.
    }
}
