@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 85: fugas selladas + barrido visible + FASE B2 (estanteria, tubos, refill) ===
git add -A
git commit -m "Ronda 85: los recipientes sellan su propio suelo (fuga diagonal cazada por Cesar) y la retirada RESTAURA el lecho; RetirarDeGolpe limpia el tope del paso 1; barrido con proteccion DEL DUENO (la poza entrega su lodo, el crater su agua) + frente de luz visible + logs por paso; FASE B2: estanteria central x386-401 (montantes a y188, paso de vuelo libre, obra por pieza), renacer reubicado en bahias (agua abajo y146, lodo arriba y167), tubos laterales que tocan el suelo con cobre propio (flancos por semantica: agua-poza, lodo-crater; revision Opus) y refill 0.8s hasta 60; verificado en vivo con dos ciclos completos y capturas"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
