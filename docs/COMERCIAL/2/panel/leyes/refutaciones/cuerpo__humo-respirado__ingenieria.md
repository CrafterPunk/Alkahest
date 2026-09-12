# Refutación de ingeniería · `humo-respirado` (lente cuerpo, C3)

**Veredicto: VIABLE CON AJUSTE.** Apalancamiento 6 (declaraba 7) · tuning 8 · coste 0,5 semanas sola, 0,25 si C1 aterriza antes.

## Lo que el código confirma

Todo lo citado existe y hace lo que dice. `LabRespira` (SimStepper.Laboratorio.cs:1052) ahoga con dos vecinos de humo; la sordina reparte humo a `(combustHumoPct+3)/4` (SimStepper.cs:885); fibra 16 y carbón 4 (Universe.Laboratorio.cs:90,172); `VidaHumo 255` llega a `Smoke.gasLifetime` y `ProcessGas` (SimStepper.cs:1488-1495) descuenta a mitad bajo techo: la bolsa vive unos 510 ticks y baja fila a fila. Nueve lecturas cada ocho ticks, incluso por cuatro cuerpos, no se miden. Sin escritura ni sal nueva, los siete hashes de los nueve escenarios de `LabBench` quedan idénticos al bit con el cuerpo dentro: test de no regresión gratis.

## Cuatro grietas, todas acotadas

**1. El espejo no lo vería.** El candidato cuelga la pasada de `LabPasadas()`, pero el invitado no tiene stepper (`_stepper = espejo ? null : …`, AlkahestSim.cs:338). `SimSync` espeja `mat[]` y no `temp[]`: justo lo único que C3 lee. La regla debe ser una función pura fuera del stepper, `PielSim.Respirar(byte[] mat, int x0, int y0)`, llamada desde `LabPasadas` en anfitrión y banco y desde `ActualizarEspejo()` cada `TickEspejo % 8`. Consecuencia buena: C3 es la única ley del cuerpo multijugador completa sin `NetworkVariable`, al contrario que C1 y C2, que dependen de `temp[]`.

**2. La tos está en el dominio equivocado.** `ApprenticeController.Update` (:468-490) ya tiene el patrón exacto («se ignora el input, la velocidad decae con la misma física»): la tos es una guarda más junto a `JournalHud.Abierto`. Pero «dos frames cada 32 ticks» mezcla frames con ticks: a ×10, 32 ticks son 3,2 frames y se corta el 60 % del control. Viñeta, tos y sonido van en tiempo de pared; solo el byte `Humo` vive en ticks.

**3. Colisión de nombres.** Ya existen `LabCuerpos()` (gancho vacío de roca suelta, :1290), `CuerposActivos`, `MsCuerpos` y el grupo `cuerpo.*`. Todo lo del avatar pasa a `Piel`: `PielSim`, `LabPiel()`, `HashPiel`, grupo `PIEL`.

**4. El escenario está mal descrito.** En `MontarCarbonera` (LabBench.cs:135) la cámara está llena de fibra hasta 220 y quedan dos filas libres; un cuerpo de 6,4×11,2 celdas no cabe dentro. «Quieto en la boca» solo puede ser sobre el techo: pies en y=224, a horcajadas de x=101, cabeza en las filas 232-235 donde pasa la pluma. Hay que fijar esas coordenadas, el control (x=115) y la chimenea de la variante ((120,223)). Y «el Humo máximo cae» es una HIPÓTESIS, no una aserción: una segunda boca da aire al fuego, sube la combustión y puede subir el humo total. El banco registra el número, como los goteos del alambique.

## Honestidad de la mitigación

«Un paso al lado y se despeja» no es lo que dicen los números: con `humoSuelta 4`, de 255 a 0 son 512 ticks, 17 s; bajar de la tos (200) cuesta 3,7 s. Un paso al lado deja de empeorar; despejarse es otra cosa. Es el único slider humano de verdad, y el banco acota su rango antes del playtest. La «rampa de hollín» existe como canal TIZNE de la pátina (SimRenderer.cs:950-955), pero es un byte por celda del renderer; el sprite se oscurece con `SpriteRenderer.color` desde `Humo`. URP está en el proyecto: la viñeta es un `Vignette` de Volume con intensidad escrita por `LabPiel.cs`, cero shaders.

## Por qué 6 y no 7

Es una ley de un solo sentido: sim → jugador. No ejecuta nada en la sim ni puede producir cruces emergentes nuevos, porque no escribe; revela (el humo se siente) y juzga poco (la tos). Sus decisiones dependen de que el jugador tenga que estar junto al fuego, que hoy es cierto por el frasco. Compra mucho onboarding por casi nada y hace legible el negativo del tiro, pero no transforma decisiones que no pasen ya por el humo.

## Versión mínima y benchmark

Byte `Humo` + viñeta; sin tos, sin sprite, sin sonido. La tos es el único «robo de control» y el riesgo que el propio documento nombra: entra tras un playtest. Benchmark: «carbonera con testigo», tres cuerpos guionados (boca, control, boca-con-chimenea) durante 9 000 ticks; se registran Humo máximo, tick en que cruza 200 y ticks de vuelta a 0; se asevera que `HashMat…HashLuz` de los nueve escenarios no se mueven y que `HashPiel` es reproducible.
