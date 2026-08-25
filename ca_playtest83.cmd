@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 83: capitulo 2 del prologo - plan + revision Opus + FASE A (el silo del lodo) ===
git add -A
git commit -m "Ronda 83: plan del capitulo 2 (PLAN_PROLOGO_CAP2.md) auditado por Opus (19 hallazgos integrados); FASE A: DepositoDeAgua parametrizado, SILO del lodo 6x9 en el hueco medido poza-crater, beats Recompensa2/LlenarDeposito2, placa honesta minima gateada por guion, monticulo excluye el silo, fix del anillo del cincel (mentia 60 vs 22 celdas) y docblock caduco retirado"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
