@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 118: la estampa camina, respira y recoge ===
git add -A
git commit -m "Ronda 118: LA ESTAMPA COBRA VIDA - tres hojas de cuadros salidas del arnes (Mixamo - Blender pose video - Wan Animate 2 local - postproceso alfa+ciclo+manifiesto): reposo (Happy Idle, ciclo), caminar (arranque 5 + ciclo 17 desde la referencia canonica) y recoger (Picking Up de perfil, una pasada, tecla G); HojaDeCuadros.cs corta sprites en runtime desde Resources/Personaje/Anim (escala sola a la talla 1.2u, base, pingpong, intro); ApprenticeController: a pie con gravedad y paso 1.1 u/s, W/Espacio despega, S aterriza (vuelo intacto), pies asentados, pose quieta = cuadro canonico, arranque/frenado corto, sin parpadeo; docs R118 a-f + ROADMAP 2.6 + DIRECCION_DE_ARTE 3"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
