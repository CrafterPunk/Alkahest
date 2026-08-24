@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 81: ensenar el cursor (haz de presentacion + aro de la boca + mira gateada), con revision Opus aplicada ===
git add -A
git commit -m "Ronda 81: mira oculta hasta recibir el frasco; haz de presentacion IMGUI sobre la vineta (una vez, mano->cursor, recortado al alcance); aro de la boca solo-aspirar con radio real, alcance y latido que decae; cascada por fin audible (ancla a mitad de caida + radio 95) y lucecita de la poza en Agua/Ir; Q ya no quema la ayuda; renombres R58 trasTomaRespiroSeg/radioAguaLuz; verdad de instancia del frasco contra hot-reload. 17 hallazgos de la revision Opus aplicados"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
