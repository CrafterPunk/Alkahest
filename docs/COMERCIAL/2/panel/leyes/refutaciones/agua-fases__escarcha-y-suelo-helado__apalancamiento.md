# Refutación · `escarcha-y-suelo-helado` (lente agua-fases, candidato 3) · eje: apalancamiento

**Veredicto: viable con ajuste.** Las guardas de un byte y la ventana de hielo son baratas y se
verifican en banco. Lo que no sobrevive al código es el titular: **la escarcha que crece y amuralla
no puede formarse**, y el «grado del núcleo» ya existe como slider. De cinco decisiones declaradas
queda una, sin medir.

## 1. La escarcha cae: es granizo, y ya lo es hoy

`Transform` (SimStepper.cs:707) conserva `temp`; `LabGotear` (Laboratorio l. 321) nace el agua a la
temperatura del núcleo (30 raw) y `ApplyPhase` (SimStepper.cs:650) la hiela en su primer
`ProcessIfNeeded` (≤ 60). Ese hielo es `caeSolido, cohesionCeldas 4` (Universe.cs:802, playtest 29) y
`SolidoTieneApoyo` (SimStepper.cs:1379-1396) solo admite apoyo por **debajo** o por ménsula hasta una
celda apoyada: un cristal colgado del techo no lo tiene y baja una celda por tick. `LabNacerHielo`
usa `LabVecinoVacio(abajoPrimero, permitirArriba:false)` (l. 298-314): nace debajo del serpentín y
cae igual. **Ni carámbanos ni sábana**: el alambique de R141 granizaba antes de la ley y graniza
después. `Goteos` cuenta llamadas a `LabGotear`, no líquido, y ningún benchmark contó `Freeze`: los
«900 goteos» ya son 900 perdigones. Con el núcleo en el suelo queda una costra de una celda que
avanza de lado y nunca sube. Para que «amuralle» hace falta una regla de adherencia nueva, y esa
regla contradice R7 y reabre un playtest.

## 2. Decisiones contadas

1. *Serpentín sobre o bajo 0 °C.* Existe: `fuego.frioRaw` recorre 0..70 (LabParams.cs:251). Y por
   geometría: toda roca impermeable gotea a **su** temperatura (l. 383, 321); una placa de piedra
   pegada al núcleo o a dos celdas decide líquido o granizo sin ningún número. El (e) por celda exige
   un verbo que no hay (LabPanel no toca `aux`; `SetCell` lo pone a 0, CellGrid.cs:239-242): un
   núcleo pintado nacería a −120 °C.
2. *Picar o dejar amurallar.* No hay muralla (§1).
3. *Proteger el lecho.* Solo si el sedimento baja de 60 raw. `LabFrio` (l. 993-1001) inyecta −20 a
   sus cuatro vecinos y nada más; el lecho a diez celdas vive entre 62 y 70. Bajo 900 perdigones la
   superficie puede enfriarse: plausible, sin medir, y es la misma decisión «distancia al núcleo»
   que R135 y R148 ya imponen por agua y por luz.
4. *Construir con hielo.* El hielo pintado nace a `meltsAt − 10` = 52 raw (playtest 13) y a 70 de
   ambiente sube 1 raw por visita (K 2, C 4): se funde en unos 3 s. «El fuego lo abre» ya es verdad.
5. *Invernadero frío.* Depende del candidato 1 (sellar la evaporación); solo, es una ventana que
   dura mientras toque el núcleo.

Cruces reales: cuatro leyes tocadas (condensación, poroso, planta, luz) más el cincel, pero el
producto es «hielo como sumidero de vapor + tres guardas». Y el cincel escrito no pica:
Cincel.cs:468-473 nace el producto a la temperatura del hielo, agua a ≤ 62 que `ApplyPhase`
devuelve a hielo al tick siguiente.

## 3. Tuning que esconde

La «meseta» no es propiedad de la ley: es el perfil térmico de una cadena de hielo (KRoca 2 contra
KAire 4, CAgua 4, FrioPotencia 20, Tiro 32) y «Δ < 2 % en 3000 ticks» es el botón disfrazado.
Peor: el hielo condensable es siempre el vecino más frío, así que roba al lecho el rocío que
R135-R150 midieron; el huerto pierde riego mientras la costra crece y esas referencias cambian de
significado sin que nadie lo decida.

## 4. Ajuste mínimo con más apalancamiento

- **Antes de escribir nada**: contar `SimEventType.Freeze` en «alambique» y «arco largo» (cero
  líneas). Todo el valor del candidato se reordena alrededor de ese hecho.
- (a') Ice condensable, y `LabNacerHielo` **solo donde el recién nacido tendría apoyo** (reutilizar
  `SolidoTieneApoyo`): costra sobre suelos y contra muros apoyados, nunca carámbanos. Respeta R7.
- (b) y (c) como están: seis líneas.
- (d) `ProductoDeTalla(Ice)` nace a `meltsAt` (62) o queda `Empty`.
- Retirar (e): el slider y la placa ya dan el grado.
- Banco: costra en meseta; `LabInfiltrado = 0` hacia poroso helado; gemelas de planta a 55 y 70
  raw; luz bajo dos hielos.

Media semana así. Tal como está escrito (adherencia nueva, verbo por celda, remedir R141 y R148)
son semana y media, y compra menos.

**Apalancamiento corregido: 4. Tuning: 7. Coste: 1,5 semanas (0,5 en la versión mínima).**
