@echo off
setlocal
title ChaosAlchemy - commit PLAYTEST 24 (LA MAREA)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"

echo === limpiando lock si lo hay ===
if exist ".git\index.lock" del /f /q ".git\index.lock"

echo === borrando zips del despliegue ===
if exist "_to_delete_pt24.zip" del /f /q "_to_delete_pt24.zip"
if exist "_deploy_pt24.zip" del /f /q "_deploy_pt24.zip"
if exist "_to_delete_docs24.zip" del /f /q "_to_delete_docs24.zip"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 24: LA MAREA - el mundo se digiere a si mismo; tu masticas en direccion contraria" -m "LA SUPER-MODIFICACION pedida por Cesar ('quiero probar tu vision... todo de golpe, confio en ti'). Direccion e integracion: Fable 5. Codigo: dos encargos Sonnet en paralelo sobre un contrato congelado (docs/CONTRATO_MAREA.md). Compilado y arrancado SIN ERRORES en el Unity real via MCP antes de entregar." -m "LA VISION: la afinidad de la semilla (playtest 18) deja de ser sabor y se convierte en EL ANTAGONISTA. Desde un CORAZON enterrado en el zocalo del sotano mana una MAREA oscura tintada con el color del material afin de la run -- la quimica de esta semilla hecha carne. Convierte lo que toca en si misma (los cuerpos y murallas de hielo, despacio: se VE venir), amortigua la temperatura a su alrededor, y la PIEDRA ES INMUNE: el cincel pasa de herramienta a FORTIFICACION. El fuego la quema con perdida. Y la unica cura es el ROCIO -- oro que brilla en la oscuridad -- que SOLO sale de la criatura digiriendo marea: le teme y la digiere A LA VEZ, sufre para fabricar la cura, y muere si la marea cubre su nucleo 9 segundos (su cuerpo se convierte en marea: engullida de verdad)." -m "EL ARCO: la marea despierta a las 12 celdas talladas con el cincel (abrir el camino a la Tolva son ~23: tu primer viaje al mundo YA la despierta -- 'el mundo tambien se abre hacia ti') o a los 300s. Tres pistas de arco por un canal prioritario del HintSystem. VICTORIA: llevar 24 celdas de Rocio (dos frascadas: exige el viaje por el pozo) hasta el corazon -> EL MUNDO SE AQUIETA. DERROTA: la marea despierta engulle a tu ultima criatura -> LA MAREA OS TRAGO. El desenlace clasico por Favor queda intacto para el modo cronometrado." -m "POR QUE ESTO ES UN JUEGO Y NO UN EXPERIMENTO: todo lo que ya existia gana proposito sin cambiar. El cincel fortifica, la mudanza es retirada, el taller clasico enterrado es un arsenal que reclamar (el aceite = arma de fuego), los tuneles que caves son riesgo, el capullo son vidas extra, y saber mas que el mundo (las leyes de TU semilla) es poder de verdad bajo un reloj de presion lento." -m "IMPLEMENTACION: MaterialId.Marea=17/Rocio=18/Count=19; ProcessMarea como proceso PROPIO de SimStepper (regla 33 intacta: ni ReactionEngine ni leyes); firma visual FIJA entre universos (excepcion documentada a la regla 17); MareaDirector con sondeos de 2s, jamas escaneos por frame. Tres fixes de integracion de Fable sobre los encargos: fluidity corregida a la escala REAL del motor (regla 50 nueva), el gate MareaActiva que el docblock prometia pero nadie cumplia (regla 49 nueva), y la pista del primer Rocio movida al momento de exudacion. Documentacion completa en HANDOFF seccion Playtest 24 y reglas 49-50 de CLAUDE.md."

echo === push ===
git push origin main

echo.
echo === COMPROBACION DEL PUSH (mira esto antes de cerrar) ===
git status -sb | head -1
git log --oneline -3
echo.
echo ============================================
echo  LISTO
echo ============================================
pause
