@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDAS 73+74: prologo rehecho + feedback del primer playtest ===
git add -A
git commit -m "Rondas 73+74: prologo rehecho (verbo, voz del Maestro, tutorial contextual, cascada, derrumbe de lodo, deposito) + feedback: fondo de ruina, bordes mordidos, pozos hondos, fuego a la derecha, deposito sin autofill con tarea LLENALO, fix CS0162 y fuga diagonal de la cascada"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
