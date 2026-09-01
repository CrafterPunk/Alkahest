using UnityEngine;

namespace Alkahest.Game
{
    /// <summary>
    /// (R120) TRES NÚMEROS QUE DECIDEN MÁS QUE TRES DEBATES (DISENO_MOVIMIENTO §4):
    /// cuánto tiempo pasa el jugador a pie vs volando, desde dónde trabaja
    /// (verter/aspirar con los pies en el suelo o en el aire) y cuántas veces
    /// despega y aterriza. Contadores estáticos, solo del jugador local; se
    /// imprimen cada 2 minutos y al cerrar la sesión con la etiqueta
    /// [Telemetría movimiento] (en builds va al Player.log). No toca la sim,
    /// no viaja por red, no se guarda: es un termómetro de playtest.
    /// </summary>
    public static class TelemetriaMovimiento
    {
        private const float CadaSeg = 120f;
        private static float _aPie, _volando, _ultimoInforme;
        private static int _verterSuelo, _verterAire, _aspirarSuelo, _aspirarAire, _despegues, _aterrizajes, _gestos;

        public static void Tick(bool aPie, float dt)
        {
            if (aPie) _aPie += dt; else _volando += dt;
            if (Time.time - _ultimoInforme >= CadaSeg && _aPie + _volando > 30f)
            {
                _ultimoInforme = Time.time;
                Debug.Log(Informe());
            }
        }

        public static void Verter(bool enSuelo) { if (enSuelo) _verterSuelo++; else _verterAire++; }
        public static void Aspirar(bool enSuelo) { if (enSuelo) _aspirarSuelo++; else _aspirarAire++; }
        public static void Despegue() => _despegues++;
        public static void Aterrizaje() => _aterrizajes++;
        public static void Gesto() => _gestos++;

        public static string Informe()
        {
            float total = Mathf.Max(0.001f, _aPie + _volando);
            return string.Format(
                "[Telemetría movimiento] a pie {0:0}% ({1:0} s) · volando {2:0}% ({3:0} s) · verter: suelo {4} / aire {5} · aspirar: suelo {6} / aire {7} · despegues {8} · aterrizajes {9} · gestos {10}",
                100f * _aPie / total, _aPie, 100f * _volando / total, _volando, _verterSuelo, _verterAire, _aspirarSuelo, _aspirarAire, _despegues, _aterrizajes, _gestos);
        }

        /// <summary>Al cerrar la sesión (lo llama el aprendiz local en OnDestroy).</summary>
        public static void Cerrar()
        {
            if (_aPie + _volando > 5f) Debug.Log(Informe() + " · FIN");
        }
    }
}
