@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 80: manten real del aspirar + ficha-recuerdo ===
git add -A
git commit -m "Ronda 80: la ficha de aspirar exige manten real (succion continua aspirarHoldSeg, latcheada) y ficha-recuerdo de un solo uso en el juego libre (reaparece tras recordatorioAspirarSeg sin aspirar, caduca sola con TutorialContextual.Desvanecer). Numeros en el guion"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
