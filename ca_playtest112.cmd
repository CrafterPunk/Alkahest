@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 112: la gota gorda ===
git add -A
git commit -m "Ronda 112: LA GOTA GORDA - el refill vuelve a llenar el vidrio ENTERO: el tope deja de ser el refillTopeCeldas fosil del guion (72, asset serializado) y pasa a ser Capacidad() leida del vidrio real (276 desde la R110); el tiempo total se MANTIENE en los ~3 minutos de la R91 (misma curva cuadratica, mismos eventos) porque lo que crece es la gota: 4 celdas por evento en fila corta centrada en el inlet, con la cola silenciosa completando rincones - un chorro de a 4 para un tubo grueso"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
