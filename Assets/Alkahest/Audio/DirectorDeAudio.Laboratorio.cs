using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Audio
{
    /// <summary>
    /// (R143) EL LABORATORIO SUENA.
    ///
    /// El director de audio se escribió para el TALLER, y sus voces están atadas a cosas del
    /// taller: los grifos, el lecho, la tolva, los encargos. En el laboratorio esas cosas no
    /// existen, así que el sistema arrancaba entero y correcto —director en escena, listener
    /// activo, cuatro bucles reproduciéndose— y **todos los volúmenes se quedaban en 0,00**. El
    /// mundo se veía y no se oía, y eso en un sandbox de investigación es peor que en un taller:
    /// aquí casi todo lo que importa pasa despacio y lejos (una gota cada varios segundos, un
    /// horno que tarda minutos), y el oído es lo que te dice que algo está pasando cuando no
    /// estás mirando.
    ///
    /// Este archivo le da al laboratorio sus propias voces, con los timbres que ya existían más
    /// uno nuevo (el goteo). No toca ninguna de las voces del taller: todo lo de aquí está
    /// gateado por `SimStepper.LabActivo`, así que en el taller es un no-op.
    ///
    /// Lo que suena, y por qué ese y no otro:
    ///  · EL AGUA CORRIENTE (bucle, timbre `GrifoLiquido`): el arroyo y el manantial. Se mide por
    ///    sondeo de celdas de agua cerca del jugador, no por contador global — el jugador tiene
    ///    que poder ALEJARSE del agua y dejar de oírla, que es la mitad de para qué sirve.
    ///  · EL VAPOR (bucle, timbre `GrifoGas`): el siseo de lo que hierve. Misma idea, sondeando
    ///    vapor y humo.
    ///  · EL GOTEO (one-shot, timbre nuevo): cada gota que cae del techo frío. Es LA señal de que
    ///    el alambique funciona, y llega antes que la vista porque el techo suele estar fuera de
    ///    plano. Se dispara con el contador `LabGoteos`, que ya existía para el libro.
    ///  · EL FUEGO (aporta al bucle que ya existe): en el taller solo cuenta `Fire`, y aquí eso
    ///    dejaba mudos el hogar y la brasa, que son la fuente de calor más habitual del
    ///    laboratorio y arden durante minutos sin una sola llama.
    /// </summary>
    public sealed partial class DirectorDeAudio
    {
        // ---- volúmenes (mismo criterio de mezcla que el resto: ver el presupuesto en el archivo principal) ----
        private const float VolLabAguaMax = 0.34f;
        private const float VolLabVaporMax = 0.26f;
        private const float VolLabGoteo = 0.42f;

        /// <summary>Cuántas celdas de la ventana hacen falta para que una voz llegue a su volumen máximo.</summary>
        private const float SaturacionLabAgua = 26f, SaturacionLabVapor = 14f;

        /// <summary>Radio en celdas del sondeo alrededor del jugador. Media pantalla: se oye lo que casi se ve.</summary>
        private const int RadioLabSondeo = 46;

        /// <summary>
        /// (R145, R23-5) Gotas por SEGUNDO, no por cuadro. El tope de «2 por cuadro» dejaba pasar
        /// hasta 120 por segundo a 60 fps, y junto a un alambique de treinta celdas eso no es un
        /// goteo: es una ametralladora. Se usa el limitador que el director ya tenía para todo lo
        /// demás, que además cuenta cuántas suprimió.
        /// </summary>
        private const float GotasPorSegundo = 6f;
        private Limitador _limGoteo;

        private AudioSource _labFuenteAgua, _labFuenteVapor;
        private float _labIntAgua, _labIntAguaObjetivo;
        private float _labIntVapor, _labIntVaporObjetivo;
        private long _labGoteosAnterior = -1;
        private float _labProximoSondeo;
        private int[] _labSondaDx, _labSondaDy;
        private float _proximoSondeoFuegoLab;

        /// <summary>Sondas relativas al jugador, calculadas UNA vez (el patrón de `ConstruirSondasFuego`).</summary>
        private const int NumSondasLab = 220;

        private void LabInit()
        {
            _labFuenteAgua = CrearFuenteBucle("Bucle_LabAgua", SintetizadorSfx.GrifoLiquido);
            _labFuenteVapor = CrearFuenteBucle("Bucle_LabVapor", SintetizadorSfx.GrifoGas);

            // Sondas en disco alrededor del jugador, deterministas por índice: no hace falta azar
            // y así dos sesiones sondean igual (importa para H7, donde comparamos partidas).
            _labSondaDx = new int[NumSondasLab];
            _labSondaDy = new int[NumSondasLab];
            for (int i = 0; i < NumSondasLab; i++)
            {
                // Espiral de Vogel: reparte puntos por el disco sin amontonarlos en el centro.
                float t = (i + 0.5f) / NumSondasLab;
                float r = Mathf.Sqrt(t) * RadioLabSondeo;
                float a = i * 2.39996323f; // ángulo áureo
                _labSondaDx[i] = Mathf.RoundToInt(Mathf.Cos(a) * r);
                _labSondaDy[i] = Mathf.RoundToInt(Mathf.Sin(a) * r);
            }
        }

        /// <summary>Llamado desde el Update del director. Fuera del laboratorio no hace nada.</summary>
        private void LabUpdate()
        {
            if (_sim == null || _sim.Stepper == null || !_sim.Stepper.LabActivo) return;

            _labProximoSondeo -= Time.deltaTime;
            if (_labProximoSondeo <= 0f) { _labProximoSondeo = IntervaloSondeo; LabSondear(); }

            _labIntAgua = Mathf.MoveTowards(_labIntAgua, _labIntAguaObjetivo, Time.deltaTime * 1.4f);
            _labIntVapor = Mathf.MoveTowards(_labIntVapor, _labIntVaporObjetivo, Time.deltaTime * 1.1f);
            if (_labFuenteAgua != null) _labFuenteAgua.volume = VolLabAguaMax * _labIntAgua * FactorBucles;
            if (_labFuenteVapor != null) _labFuenteVapor.volume = VolLabVaporMax * _labIntVapor * FactorBucles;

            LabSonarGoteos();
        }

        /// <summary>Cuenta agua y vapor en el disco de sondas alrededor del jugador.</summary>
        private void LabSondear()
        {
            if (_jugador == null || _labSondaDx == null) { _labIntAguaObjetivo = 0f; _labIntVaporObjetivo = 0f; return; }
            int jx = Mathf.RoundToInt(_jugador.position.x / SimRenderer.CellWorldSize);
            int jy = Mathf.RoundToInt(_jugador.position.y / SimRenderer.CellWorldSize);

            int agua = 0, vapor = 0;
            for (int i = 0; i < _labSondaDx.Length; i++)
            {
                int m = _sim.SampleMaterial(jx + _labSondaDx[i], jy + _labSondaDy[i]);
                if (m == MaterialId.Water) agua++;
                else if (m == MaterialId.Steam || m == MaterialId.Smoke) vapor++;
            }
            _labIntAguaObjetivo = Mathf.Clamp01(agua / SaturacionLabAgua);
            _labIntVaporObjetivo = Mathf.Clamp01(vapor / SaturacionLabVapor);
        }

        /// <summary>
        /// Una gota que cae, un «ploc». El contador `LabGoteos` ya existía para el libro de agua,
        /// así que el sonido no necesita ninguna regla nueva: se cuelga de una medida que ya se
        /// llevaba. Con tope por cuadro, porque un serpentín grande puede soltar decenas de gotas
        /// por segundo y eso dejaría de ser un goteo para ser un zumbido.
        /// </summary>
        private void LabSonarGoteos()
        {
            long ahora = _sim.Stepper.LabGoteos;
            if (_labGoteosAnterior < 0) { _labGoteosAnterior = ahora; return; } // línea base: no sonar el pasado.
            long nuevas = ahora - _labGoteosAnterior;
            _labGoteosAnterior = ahora;
            if (nuevas <= 0) return;
            // El limitador decide si suena; el pitch sale del `_rngVariacion` del director (System.Random
            // local, la convención de esta capa) y no de UnityEngine.Random.
            float pitch = 1f + ((float)_rngVariacion.NextDouble() * 2f - 1f) * 0.13f;
            DispararLimitado(ref _limGoteo, SintetizadorSfx.Goteo, GotasPorSegundo, VolLabGoteo, pitch);
        }

        /// <summary>
        /// En el laboratorio, el hogar y la brasa también son fuego. `SondearFuego` solo mira
        /// `Fire`, que es correcto en el taller pero deja mudo lo que más arde aquí: un hogar
        /// calienta durante toda la partida sin una sola llama, y una carbonera entera arde en
        /// sordina —por definición, SIN lengua de fuego— durante minutos. Se APORTA al objetivo
        /// que el sondeo general ya calculó en vez de sustituirlo, así que la llama sigue mandando
        /// cuando la hay.
        /// </summary>
        private void LabAportarFuego()
        {
            if (_sim == null || _sim.Stepper == null || !_sim.Stepper.LabActivo) return;
            if (_jugador == null || _labSondaDx == null) return;
            // (R145) A 12 Hz como el resto de sondeos, no cada cuadro: son 220 muestras y el
            // resultado no cambia entre dos cuadros consecutivos.
            if (Time.time < _proximoSondeoFuegoLab) return;
            _proximoSondeoFuegoLab = Time.time + IntervaloSondeo;
            int jx = Mathf.RoundToInt(_jugador.position.x / SimRenderer.CellWorldSize);
            int jy = Mathf.RoundToInt(_jugador.position.y / SimRenderer.CellWorldSize);

            int brasas = 0;
            for (int i = 0; i < _labSondaDx.Length; i++)
            {
                int m = _sim.SampleMaterial(jx + _labSondaDx[i], jy + _labSondaDy[i]);
                if (m == MaterialId.Hogar || m == MaterialId.Brasa) brasas++;
            }
            if (brasas <= 0) return;
            // Un hogar suena a lumbre baja, no a incendio: se topa en la mitad del máximo.
            float aporte = Mathf.Clamp01(brasas / 10f) * 0.5f;
            if (aporte > _intensidadFuegoObjetivo) _intensidadFuegoObjetivo = aporte;
        }
    }
}
