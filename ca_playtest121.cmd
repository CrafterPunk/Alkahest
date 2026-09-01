@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 121: las tres opciones de movimiento jugables con F6 ===
git add -A
git commit -m "Ronda 121: LAS TRES OPCIONES DE MOVIMIENTO, JUGABLES - ApprenticeController.Modo rota con F6 entre A solo vuelo, B pies y vuelo (R118b) y C solo pies (Espacio salta con altura variable, Shift corre); el modo persiste y el atril lo avisa; TelemetriaMovimiento separa un bloque por modo y cuenta saltos; DISENO_MOVIMIENTO y HISTORIAL R121"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
