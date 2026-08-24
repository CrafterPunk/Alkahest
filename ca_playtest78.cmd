@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 78: consulta de diseno (solidos, inventario, linea jugable) + todo lo pendiente ===
git add -A
git commit -m "Ronda 78: documento de diseno (almacenaje de solidos, softlock del deposito diagnosticado, inventario, game feel, linea jugable post-prologo). Incluye lo pendiente de las rondas 73-77 si no se habian subido"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
