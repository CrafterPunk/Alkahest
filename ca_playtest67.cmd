@echo off
REM ca_playtest67.cmd -- ronda 66: falsa profundidad 2.5D v1 (capas, labio de roca, piso estructural, colision, cincel corto con LOS)
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
git add -A
git commit -m "Ronda 66: falsa profundidad 2.5D v1 -- Capas.cs, labio frontal de roca, sombreado de masa (interior/esquinas), PISO ESTRUCTURAL (material 65, X en el cincel, reemplaza roca), colision real del aprendiz, cincel corto 22 celdas con linea de vision, corral retirado"
git push origin main
pause
