@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 72: la gran depuracion para trabajar con un externo ===
git add -A
git commit -m "Ronda 72: depuracion para externos - README nuevo, docs/ESTADO.md con planes (estructural+escenificacion), CLAUDE.md compacto (reglas R7-R59), 27 docs historicos a docs/archivo/ (HANDOFF -> HISTORIAL_RONDAS), builds y cmds viejos a _to_delete/, ultimo vestigio de menu Alkahest/ corregido"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
