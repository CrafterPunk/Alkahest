@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 79: pulido del prologo (feedback de Cesar) + todo lo pendiente ===
git add -A
git commit -m "Ronda 79: pulido del prologo con feedback de juego real - turba del muro retirada (regla 15), luz pegada al jugador (luzBiasVen 0.92) + lucecita ambar del Maestro durante VEN., chapa EL MAESTRO solo tras la primera voz, cero descubrimiento/bautizo en ModoFundacion; _notas/ y _to_delete/ al gitignore (la consulta R78 pasa a hoja viva fuera del repo)"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
