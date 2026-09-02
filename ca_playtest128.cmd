@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 128: la Galeria afinada con el playtest de Cesar ===
git add -A
git commit -m "Ronda 128: GALERIA AFINADA - corredores a 16 celdas; cincel: piso fuera del ciclo de la C (X sigue vivo) y botones al estandar (izq talla, der construye); fogatas persistentes del curador; curador en F8 con frasco/cincel cediendo clics, estampa por radio solo-materia, anillo de radio en el cursor, R de texturas con aviso y sin curador; sin baldas/anclajes clasicos en la galeria y catalogo con placa ignea, placa gelida, cano de agua y balda; terrario solo vocabulario nombrado; HISTORIAL R128" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_018P9nGxPfRvRX98zeBn5cAC"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
