@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 118: la estampa camina (video - sprites - juego) ===
git add -A
git commit -m "Ronda 118: LA ESTAMPA CAMINA - primer ciclo de animacion del munreco salido del arnes (Mixamo Walking - Blender pose video - Wan Animate 2 local - postproceso alfa+ciclo - hoja de 17 cuadros a 16 fps); HojaDeCuadros.cs corta sprites en runtime desde Resources/Personaje/Anim (png+manifiesto, escala sola a la talla de la estampa 1.2u); ApprenticeController reproduce caminar con velocidad horizontal (ritmo 0.6-1.15x, bob a un cuarto) y vuelve a la estampa quieto; sin parpadeo (ojos-lampara en prompt y negativo); docs R118 + ROADMAP 2.6 + DIRECCION_DE_ARTE 3"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
