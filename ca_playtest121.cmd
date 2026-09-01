@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 121: tres modos de movimiento con F6 + paquete de sensacion de plataformas ===
git add -A
git commit -m "Ronda 121: LAS TRES OPCIONES DE MOVIMIENTO, JUGABLES - ApprenticeController.Modo rota con F6 entre A solo vuelo, B pies y vuelo (R118b) y C solo pies (Espacio salta con altura variable, Shift corre); el modo persiste y el atril lo avisa; TelemetriaMovimiento separa un bloque por modo y cuenta saltos; paquete de sensacion de plataformas para C y a pie en B (aceleracion-frenado 0.07 s, salto 2.2u con caida 1.7x, corte al soltar, apice suave, coyote y buffer 0.12 s, squash de aterrizaje, paso 1.5 y correr 2.6); DISENO_MOVIMIENTO y HISTORIAL R121-121b"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
