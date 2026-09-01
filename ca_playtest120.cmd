@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 120: retoques de la caminata, atril de emotes por acordes, ancla de trabajo, telemetria, gestos nuevos ===
git add -A
git commit -m "Ronda 120: RETOQUES, EL ATRIL Y EL ANCLA - caminar con ciclo contiguo al arranque (adios al tropezon) y frenado de 6 cuadros; AtrilDeEmotes por acordes 1-4 + 1-4 (grupos automaticos desde los manifiestos, lista discreta con ventana de 2.6 s); ancla de trabajo (verter/aspirar clava al personaje) y TelemetriaMovimiento (a pie vs volando, desde donde trabaja, despegues/aterrizajes); hojas nuevas desde la canonica: flotar, saltar, girar, zombi, despertar; postproceso con recorte de cola estatica y etiqueta; docs R120"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
