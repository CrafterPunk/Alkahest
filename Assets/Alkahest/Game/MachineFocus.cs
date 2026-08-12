using System.Collections.Generic;
using UnityEngine;

namespace Alkahest.Game
{
    /// <summary>Aparato del taller que responde a la tecla E (placa ígnea, piedra gélida, grifo).</summary>
    public interface IMaquinaInteractiva
    {
        /// <summary>Punto del mundo desde el que se mide la cercanía del aprendiz.</summary>
        Vector3 PuntoFoco { get; }
        /// <summary>Radio (unidades de mundo) dentro del cual este aparato es operable.</summary>
        float RangoFoco { get; }
    }

    /// <summary>
    /// ÁRBITRO DE FOCO: de todos los aparatos que tienen al aprendiz dentro de
    /// su radio, solo UNO —el más cercano— responde a la tecla E y muestra su
    /// prompt.
    ///
    /// POR QUÉ EXISTE (reingeniería del espacio, playtest 4): los cinco grifos
    /// están ahora en una COLUMNA VERTICAL compacta, a una unidad de mundo unos
    /// de otros. Con el criterio antiguo ("¿está el jugador dentro de mi radio?")
    /// una sola pulsación de E habría abierto tres o cuatro grifos a la vez, y
    /// la pantalla habría mostrado cuatro rótulos "E — abrir" apilados. Con el
    /// árbitro, acercarse a la columna selecciona el grifo que tienes delante,
    /// y solo ese se anuncia.
    ///
    /// Implementación deliberadamente simple y sin sorpresas de orden de
    /// ejecución: los aparatos se registran al crearse y CUALQUIERA puede
    /// preguntar en su propio Update() quién es el foco; el resultado se cachea
    /// por frame, así que da igual quién pregunte primero. Con ~8 aparatos el
    /// coste es despreciable y no asigna memoria.
    /// </summary>
    public static class MachineFocus
    {
        private static readonly List<IMaquinaInteractiva> _maquinas = new List<IMaquinaInteractiva>(12);

        private static IMaquinaInteractiva _foco;
        private static int _frame = -1;

        public static void Registrar(IMaquinaInteractiva m)
        {
            if (m == null || _maquinas.Contains(m)) return;
            _maquinas.Add(m);
        }

        public static void Olvidar(IMaquinaInteractiva m)
        {
            if (m == null) return;
            _maquinas.Remove(m);
            if (ReferenceEquals(_foco, m)) { _foco = null; _frame = -1; }
        }

        /// <summary>
        /// Recarga de escena: los MonoBehaviour mueren pero esta lista estática
        /// no, así que hay que vaciarla o quedarían referencias a objetos
        /// destruidos. La llama Game/AlkahestGameBootstrap.cs antes de crear las
        /// máquinas de la partida nueva.
        /// </summary>
        public static void Limpiar()
        {
            _maquinas.Clear();
            _foco = null;
            _frame = -1;
            _vecesUsadaE = 0;
        }

        // -----------------------------------------------------------------
        // TUTORIAL DE LA TECLA E (fix playtest 7)
        // -----------------------------------------------------------------
        // Cesar: "indicarle al jugador todo el tiempo que necesita presionar la
        // E para interactuar es cansado y estorba; quizás solo la primera vez, y
        // luego una señal, un contorno de resalte o algo que indique que está a
        // distancia suficiente".
        //
        // "Pulsa E junto a un aparato" es UNA regla del juego, no una propiedad
        // de cada máquina: en cuanto la usas dos veces la sabes para siempre, da
        // igual si fue en un grifo o en la placa. Por eso el contador vive aquí
        // (en el árbitro que ya conoce a todos los aparatos) y no duplicado en
        // cada MonoBehaviour. A partir de <see cref="UsosParaAprender"/> usos, el
        // prompt de texto desaparece y solo queda el RESALTE del aparato
        // enfocado, que sigue diciendo "puedes actuar sobre este" sin ocupar
        // pantalla.
        //
        // Se reinicia en Limpiar() (una partida nueva vuelve a enseñar).

        private const int UsosParaAprender = 2;
        private static int _vecesUsadaE;

        /// <summary>¿Hay que seguir mostrando el prompt de texto "E — ..."? (solo las primeras veces)</summary>
        public static bool MostrarPromptE => _vecesUsadaE < UsosParaAprender;

        /// <summary>Lo llama cada aparato cuando el jugador pulsa E sobre él con éxito.</summary>
        public static void RegistrarUsoE()
        {
            if (_vecesUsadaE < UsosParaAprender) _vecesUsadaE++;
        }

        /// <summary>¿Es `m` el aparato enfocado por el aprendiz en este frame?</summary>
        public static bool EsFoco(IMaquinaInteractiva m, Transform jugador)
        {
            if (m == null || jugador == null) return false;
            return ReferenceEquals(Foco(jugador.position), m);
        }

        private static IMaquinaInteractiva Foco(Vector3 posJugador)
        {
            if (_frame == Time.frameCount) return _foco;
            _frame = Time.frameCount;
            _foco = null;

            float mejorD2 = float.MaxValue;
            for (int i = _maquinas.Count - 1; i >= 0; i--)
            {
                var m = _maquinas[i];
                // Red de seguridad: un MonoBehaviour destruido sigue en la lista
                // hasta su OnDestroy. Tipado como UnityEngine.Object, el
                // operador == de Unity detecta el objeto destruido (una
                // referencia de interfaz sola, no).
                UnityEngine.Object comoObjeto = m as UnityEngine.Object;
                if (comoObjeto == null)
                {
                    _maquinas.RemoveAt(i);
                    continue;
                }

                float d2 = (m.PuntoFoco - posJugador).sqrMagnitude;
                if (d2 > m.RangoFoco * m.RangoFoco) continue;
                if (d2 >= mejorD2) continue;

                mejorD2 = d2;
                _foco = m;
            }

            return _foco;
        }
    }
}
