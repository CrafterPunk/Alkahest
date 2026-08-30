@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 109: el defecto es la intimidad ===
git add -A
git commit -m "Ronda 109: EL DEFECTO ES LA INTIMIDAD - la vista por defecto pasa de 90 a 80 celdas (lo que Cesar jugaba de facto pegado al tope de la rueda), reserva de zoom in hasta 72, Tab y el tope de alejar preservan las 198 celdas exactas de siempre (WideViewMultiplier 2.475); y la leccion de escala queda por escrito en DIRECCION_DE_ARTE: la proporcion personaje/mueble solo se corrige en celdas, jamas con zoom - maquetas de reservorios x1.35 y x1.6 entregadas, cirugia pendiente del veredicto"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
