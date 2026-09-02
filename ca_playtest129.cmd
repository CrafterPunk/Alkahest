@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 129: velo de liquidos y menisco ===
git add -A
git commit -m "Ronda 129: VELO DE LIQUIDOS Y MENISCO - tercera textura solo-liquidos delante del aprendiz (orden 52, alfa 115, mismo color de la sim: el munheco se tinhe al meterse a la poza); menisco en la piel de roca (hebra clara del color del liquido a caballo de la tinta donde el contorno esta mojado) con segundo hash de orilla revisado solo en la ronda lenta (una poza con corriente no reconstruye malla cada tick); fix: la poza de la galeria se vaciaba por el corredor pozo-poza (piso del corredor a y82, sobre la linea de agua; verificada estanca); HISTORIAL R129" -m "Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_018P9nGxPfRvRX98zeBn5cAC"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
