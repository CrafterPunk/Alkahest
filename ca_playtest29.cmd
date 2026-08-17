@echo off
setlocal
title ChaosAlchemy - commit PLAYTEST 29 (gravedad con cohesion + fixes multi)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"

echo === limpiando restos de despliegues ===
if exist "_to_delete_pt28" rmdir /s /q "_to_delete_pt28"
if exist "_to_delete_fix28d" rmdir /s /q "_to_delete_fix28d"
if exist "_to_delete_pt26" rmdir /s /q "_to_delete_pt26"
if exist "_to_delete_pt27" rmdir /s /q "_to_delete_pt27"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 29: gravedad con cohesion - construir pasa a tener ingenieria + fixes del arranque multi" -m "GRAVEDAD CON COHESION (GO de Cesar tras evaluar pros/contras; su pregunta -- 'todo pixel necesita base o habra un principio de cohesion con apoyos sensatos?' -- se respondio eligiendo COHESION): un solido se sostiene si tiene apoyo debajo O si a menos de K celdas en horizontal, a traves de materia solida CONTINUA (un hueco corta la viga; los liquidos no transmiten carga: el hielo sigue flotando), alguien tiene apoyo directo. K fijo por material y cuenta una historia: ceramico 8 > compacto 6 > recocido 5 = cristal 5 > hielo 4 > templado 3 (lo fragil es fragil hasta para voladizar). La PIEDRA y la obra del taller JAMAS caen. Caida recta 1 celda/tick solo a vacio, sin deslizar de lado (un solido no es un polvo). Coste: solo chunks despiertos, escaneo acotado por K; lo asentado duerme como siempre. Regla 7 de CLAUDE.md matizada. VERIFICADO EN VIVO: templado pintado en el aire cayo en columnas rectas, se apilo sobre la obra de la prensa y se quedo quieto (sim 0.6-1.6ms)." -m "CONSECUENCIA DE JUEGO BUSCADA: construir tiene ingenieria -- vigas y voladizos sensatos si, alfombras flotantes no; el largo de una mensula depende DEL ESTADO con que la construyas (un puente de ceramica no es un puente de vidrio templado); lo fundido vertido en el aire ya no queda como calcomania flotante." -m "FIXES DEL ARRANQUE MULTI (el atasco de Cesar en la primera pantalla): (1) el prefab del avatar se registraba DOS veces (editor + runtime) y NGO ante el duplicate GlobalObjectIdHash invalidaba el registro entero -- ANFITRION parecia no hacer nada; ahora hay UN solo punto de registro (runtime, SimSync.Awake, condicional). (2) ANFITRION cae solo a taller LOCAL con aviso visible si Steam no esta abierto -- jugar en solitario dandole a ANFITRION es un camino valido. Escena y build MULTI regeneradas limpias. Tambien en este lote: la columna de ensayo con gravedad prestada (los solidos pintados ya caen dentro, patron de la Tolva regla 7), su lectura clara ('por ahora una sola capa (X) -- agrega otra cosa y E de nuevo'), y el limo renombrado a LIMO PRIMORDIAL para la prueba con amigos de Steam." -m "Pendiente decidido por Cesar: maquinas como objetos de red (mudanza para invitados) queda para la siguiente ronda ('con que el host pueda ahora me basta'). Direccion, codigo e integracion: Fable 5, verificacion visual propia en el Unity real. Documentacion en HANDOFF seccion Playtest 29 y regla 7 de CLAUDE.md."

echo === push ===
git push origin main
echo.
echo === COMPROBACION DEL PUSH (mira esto antes de cerrar) ===
git status -sb | head -1
git log --oneline -3
echo ============================================
echo  LISTO
echo ============================================
pause
