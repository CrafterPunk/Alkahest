@echo off
setlocal
title ChaosAlchemy - commit PLAYTEST 22 (herramientas vivas)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"

echo.
echo  ATENCION: esta ronda NO se pudo compilar antes de entregarla
echo  (se cayo el MCP de Unity). Dale Ctrl+R en Unity y comprueba que
echo  la consola esta limpia ANTES de ejecutar este script.
echo.
pause

echo === limpiando lock si lo hay ===
if exist ".git\index.lock" del /f /q ".git\index.lock"

echo === borrando los zips del despliegue ===
if exist "_to_delete_pt22.zip" del /f /q "_to_delete_pt22.zip"
if exist "_to_delete_docs22.zip" del /f /q "_to_delete_docs22.zip"
if exist "_to_delete_pivot.zip" del /f /q "_to_delete_pivot.zip"
if exist "_to_delete_pv2.zip" del /f /q "_to_delete_pv2.zip"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 22: HERRAMIENTAS VIVAS - las maquinas son criaturas" -m "LA TESIS LA ESCRIBIO CESAR: su referencia de arte lleva un panel rotulado 'HERRAMIENTAS VIVAS'. Las maquinas no son maquinas, son criaturas con temperamento que colocas donde las necesitas. Montar el laboratorio es ordenar tus instrumentos vivos; el cincel excava el espacio y las criaturas lo amueblan." -m "TEMPERAMENTO TERMICO POR INDIVIDUO, valor CONTINUO en la instancia y no en el material. Ese era el bug de fondo detras de su pregunta 'nacio lo mismo que tenia vivo, es probabilidad o asi es?': los dos seres son literalmente el mismo MaterialId.Vivium, asi que color, patron y habito de crecimiento salian de la SEMILLA DE LA PARTIDA y toda cria nacia clon de su padre. Con esto las criaturas sustituyen a la placa ignea y a la piedra gelida." -m "LA TRAMPA MORTAL, documentada porque es facil caer: si una criatura fria enfria su propia celda se sale de su banda de crecimiento, se duerme y no crece nunca mas. Se autodestruye. Por eso ApplyCalorTick separa dos radios: el NUCLEO mantiene SIEMPRE a la criatura dentro de su banda pase lo que pase, y solo el ALCANCE AMPLIO lleva el temperamento hacia fuera. Ese anillo exterior es lo que la convierte en instrumento; el nucleo es lo que la mantiene viva." -m "LA CRIA HEREDA CON DESVIACION del progenitor que la incubo (Criatura.MasCercanaA), no de un padre global ni con una tirada nueva. Criar deja de ser esperar y pasa a ser ORIENTAR: si crias uno caliente su cria tiende a caliente, y en varias generaciones afinas el temperamento que te falta." -m "CRIATURA Y CAPULLO SON MOVIBLES: implementan IMovible, la tecla V los agarra y la R los devuelve a su sitio. Era lo primero que Cesar reporto ('no puedo reacomodar el hijito que nacio')." -m "LOS DOS CANOS BASICOS VUELVEN, por su razon, que es la correcta: en un juego cuyo verbo es EXPERIMENTAR, un recurso que puedes perder para siempre es una trampa, y una fuente infinita es lo que permite equivocarse. Solo agua y nutriente, coste 0, montados en el muro izquierdo en columna como en su referencia de arte. El charco se movio de x=267 a x=250 para quedar justo debajo de las boquillas: deja de ser decorado y pasa a ser PILA DE RECOGIDA. La sala se lee de izquierda a derecha: canos+pila, criatura, capullo." -m "NO se reutilizo SpawnOneDispenser a proposito: deriva su sitio de TapMountX/TapFirstY/TapStepY, que son las coordenadas del banco de grifos del taller CLASICO, hoy enterrado a 30 celdas de la camara. Reutilizarlo habria plantado los dos canos dentro de la roca, invisibles y sin ningun error." -m "REGLAS NUEVAS (CLAUDE.md 44-47): 44) un recurso perdible para siempre es una trampa. 45) el rasgo de un individuo no puede vivir en su material. 46) una criatura que se enfria a si misma se mata. 47) no reutilizar una constante de posicion solo porque el nombre encaja." -m "Y una seccion nueva en el HANDOFF a peticion de Cesar, LA TENSION DE FONDO DEL PROYECTO, por si hay que pedir opinion desde fuera: como hacer legible un simulador profundo sin simplificarlo, y la hipotesis actual de que la via no es explicarlo sino DARLE UN CUERPO -- una criatura que tiene hambre enseña temperatura, quimica y crecimiento sin una linea de tutorial." -m "Ronda accidentada: tres reinicios del sandbox y un limite de cuota que corto a un agente. PENDIENTE: el halo como luz real, y la verificacion en el editor."

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
