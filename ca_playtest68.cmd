@echo off
REM ca_playtest68.cmd -- ronda 68: pulido 2.5D (LOS por grosor, construir libre, caja 3x4, sin tiles oscuros, acabado del piso, apoyo de maquinas, fondo oscuro)
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
git add -A
git commit -m "Ronda 68: pulido del feedback pt67 -- LOS v2 por grosor (pulir salientes es legal), construir sin LOS, caja de colision 3x4, labio frontal apagado, acabado contextual del piso, apoyo estructural al soltar estaciones, fondo 26%% mas oscuro; verificado en modo caotico"
git push origin main
pause
