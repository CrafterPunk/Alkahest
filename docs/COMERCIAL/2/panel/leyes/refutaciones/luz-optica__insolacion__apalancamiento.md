# Refutación · `insolacion` (luz-optica) · lente apalancamiento

**Veredicto: REFUTADO como ley propia.** Sobrevive un apéndice de dos líneas de `haz-y-espejo`,
no una ley con escalera.

## 1. La escalera no sobrevive a la geometría del propio nivel

La regla escribe `tope = ambient + LuzSolRaw × sol/255` con `sol` = potencia **restante** del rayo.
Los peldaños 100/136/172/208 suponen `sol = 255`, es decir, el rayo aterrizando en la boca misma.
En el nivel real (`SimLevelBuilder.Laboratorio.cs:163-164`: boca `Aire(118,273,124,286)`, lecho
hasta y=249) el rayo cruza 36 celdas de aire a `LuzDecayCielo = 1` y aterriza con **P = 219**. Con
`LuzSolRaw = 36` y ambiente 64 la escalera queda en **94 / 125 / 156 / 187**: dos soles NO prenden
fibra (`ApplyPhase`, `SimStepper.cs:685`, exige `t > 130` estricto) y cuatro NO vidrian ni prenden
carbón (200). Quedan dos peldaños: «seca» y «hogar». Y el hogar ya existe.

Un rayo enrutado pierde además 8 por cuña y 4 por celda de vidrio: con dos codos y ocho celdas
aterriza a ~171 y aporta 24 raw, no 36. La ventana válida de `X = LuzSolRaw × P/255` es
34 < X < 45 (±14 %) y la atenuación por trayecto varía un 27 % dentro del laboratorio:
**ningún escalar da los cuatro peldaños al rayo vertical y al enrutado a la vez**, que es justo
el caso que la ley promete. El «cualquier valor en 35..41» es falso.

## 2. Dos de los cinco cruces son falsos por el código

- **«Dos soles queman una hoja».** `LabLuzDecay` (`SimStepper.Laboratorio.cs:1276-1284`): la planta
  TRANSMITE con decaimiento 12, recibe `sol = 12` → tope +1. Solo se calienta el sustrato; el
  huerto muere de sequía, que es el cruce 1 repetido.
- **«El charco hierve desde el fondo».** El agua también transmite (`sol = 20`, tope +2). Calienta la
  roca de debajo, y de ahí al agua va por `LabFlujoTermico` con `k = min(KRoca 2, KAgua 8)` y
  `CAgua = 4`: `(172−T)×2/256 = 0`, forzado a +1 cada 8 ticks contra tres vecinos de aire a k=4.
  El charco se estabiliza en ~80-85 raw; hierve a 110.
- **Hielo:** `meltsAt ≈ 62 raw` (`Universe.cs:807`) y la cámara alta está a 64: el espejo de hielo
  solo vive pegado a un núcleo frío; lo decide el frío, no el sol.

## 3. El peldaño que sí funcionaría rompe una frontera medida

Los peldaños 1-3 reproducen el hogar (`HogarRaw` 170: hierve, seca, cuece a 150, prende yesca, no
carbón), ya gratuito y eterno (`DISENO_FUEGO.md` F4). El peldaño 4 (208) es lo que `LabHogar`
(`SimStepper.Laboratorio.cs:922-931`, R136 C1) eliminó a propósito: «arena con ceniza sobre el
hogar se volvía VIDRIO sin horno… la frontera entre vivir y fabricar». El horno solar reinstaura
ese bug por geometría, sin combustible, recinto, humo ni tolva: quita los costes que hacen
interesante al fuego (`DISENO_FUEGO.md` §2.3: «no hay motivo para construir nada»).

## 4. Cuenta de decisiones nuevas

De las cinco declaradas: (1) «qué va bajo la vertical» no es decisión sin espejos, porque la
vertical es la ÚNICA luz y el huerto tiene que estar ahí; (2) y (3) son decisiones del candidato
1; (4) «calentar sin humo ni combustible» es el hogar; (5) el charco no hierve. **Queda un cruce
genuino: luz × secado** (la franja iluminada es la franja seca; contrapeso real a R135, porque
`LabSecarHacia` ya escala con `temp − 70`). La «conservación luz→calor» es decorativa: en aire el
rayo deja `sol = 1` (tope +0); la regla efectiva es «se calienta la celda donde aterriza el rayo».

**Riesgo subestimado:** la franja queda fijada ~30 raw sobre ambiente (34 raw cada 16 ticks
contra ~2 de pérdida por visita) en la cámara afinada a 64 para que condense el alambique; la
mitigación «solo 7 columnas» son las 7 columnas que iluminan el huerto.

## 5. Versión mínima viable (apéndice de `haz-y-espejo`, 2-3 días)

Contar **rayos, no potencia**: `tope = min(HogarRaw, ambient + LuzSolRaw × nRayos)`, escrito con
`LabCalentarHasta(cuanto = HogarCalor)` en la celda de aterrizaje; sin `_labSol`, sin atenuación
por trayecto, sin peldaño industrial. `LuzSolRaw` en 35..45 (bench: un rayo seca sin hervir; el
tope hace el resto). Quedan dos cruces: secado del lecho y hogar enrutable sin humo. La distinción
fibra/planta a dos rayos es filo de navaja (35 es el único entero válido en los climas 64 y 70)
y se abandona como promesa.

**Corregido:** apalancamiento 4 (la ley) / 5 (el apéndice); tuning 6; coste real 3 semanas
(1,5 del candidato 1 del que depende + 1 + barrido de geometrías y medida de deriva).
