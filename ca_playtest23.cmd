@echo off
setlocal
title ChaosAlchemy - commit PLAYTEST 23 (la cadena completa)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"

echo === limpiando lock si lo hay ===
if exist ".git\index.lock" del /f /q ".git\index.lock"

echo === borrando zips del despliegue ===
if exist "_to_delete_pt23.zip" del /f /q "_to_delete_pt23.zip"
if exist "_to_delete_docs23.zip" del /f /q "_to_delete_docs23.zip"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 23: la cadena completa - descubrir, transformar, capacidad nueva, preguntas nuevas" -m "FABLE DE VUELTA EN DIRECCION. El encargo de Cesar: una version inicial sencilla y legible que ya permita sentir una pequena cadena real de descubrimiento -> transformacion -> nueva capacidad -> nueva experimentacion. Compilado y arrancado SIN ERRORES en el Unity real via MCP antes de entregar." -m "EL DIAGNOSTICO CON NUMEROS -- sus dos 'trabados' eran el mismo bug: (1) el temperamento inicial se sorteaba uniforme 0..1, asi que la mitad de las partidas nacian con criatura FRIA; una fria contenta empuja su anillo hacia raw 30 (-60C) y el capullo solo avanza por encima de la banda de cultivo: capullo muerto, partida trabada. (2) La herencia desviaba +-0.16 sobre un progenitor cualquiera: la cria era casi el padre, 'nacio lo mismo pero mas pequena'. Y el dato clave: el frio YA podia congelar agua en toda semilla (raw 30 < freezesAt 52..67) -- la capacidad existia, faltaba el camino y el cartel." -m "LA REGLA DE LA RONDA (48): cada estado y cada temperamento necesita un VERBO visible y un CONSUMIDOR real. El calor tenia consumidor (el capullo); el frio no tenia ninguno, por eso era un callejon. Ahora tiene dos: el HIELO (universal, vocabulario, determinista) y las leyes con condicion=Frio de cada semilla. En la primera semilla probada tras el cambio, 4 de las 6 leyes sorteadas exigian frio: la cria fria es la llave de la mayor parte de la quimica de ese universo." -m "LO HECHO: la primera criatura SIEMPRE nace calida (0.72-0.90 por semilla; la sala tiene un solo consumidor termico y pide calor); el primer capullo SIEMPRE da cria FRIA (0.08-0.25; la generacion 1 ensena el EJE entero, la herencia fina +-0.16 queda intacta para las generaciones 2+); rotulos en VERBOS con accion ('congela lo que la rodea', 'hambrienta -- viertele nutriente', y el capullo por fin dice POR QUE no avanza: 'detenido -- hace demasiado frio aqui'); fuera el monton de nutriente del suelo (hacia pensar en loot de Minecraft; ahora tu primer acto es alimentarla TU desde el cano); encargos del pivot nuevos ('algo helado a -5C' = tu cria fria, y si bautizaste algo el Maestro te lo pide POR SU NOMBRE -- bautizar gana valor mecanico) en vez de los imposibles inflamable/80C que esperaban tras la roca; y fix de la O: con cero encargos el panel ahora dice 'nadie os ha oido todavia' en vez de abrirse casi vacio e indistinguible de no abrirse." -m "EL GUION ESPERADO DE LA PARTIDA: despierta hambrienta -> el rotulo te dice que hacer -> la alimentas -> se enciende e irradia -> viertes agua -> exuda algo nuevo -> lo bautizas -> el capullo se agrieta -> nace la cria FRIA, azul -> le acercas agua -> HIELO -> cavas hasta la Tolva -> te pide exactamente hielo y lo que tu bautizaste, por su nombre. Cada eslabon ensena el siguiente." -m "Direccion y codigo: Fable 5, sin agentes (cambios quirurgicos en 6 archivos). Documentacion en HANDOFF seccion Playtest 23 y regla 48 de CLAUDE.md."

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
