@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 75: la escenificacion del prologo (arquitectura hibrida) ===
git add -A
git commit -m "Ronda 75: escenificacion del prologo - GuionDelPrologo.asset (textos/numeros en Inspector), escenografia con marcadores movibles (Maestro/Deposito), arte horneado a PNG + DepositoVisual.prefab, generador de escena pasa de arrasar a validar/completar (ediciones manuales sobreviven a builds), find-or-create del backdrop, matriz de autoridad en ESTADO.md"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
