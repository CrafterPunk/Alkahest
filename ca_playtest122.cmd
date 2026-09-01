@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 122: desenterrar a pie (el que talla bajo sus pies ya no se hunde) ===
git add -A
git commit -m "Ronda 122: EL QUE TALLA BAJO SUS PIES - a pie, si la caja arranca dentro de solido se DESENTIERRA hacia arriba (media celda por paso, luego toda la columna) en vez de suspender la colision y hundirse hasta el fondo del mundo; el fondo del mundo cuenta como suelo; verificado en vivo (enterrado 14u -> superficie en un frame); HISTORIAL R122"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
