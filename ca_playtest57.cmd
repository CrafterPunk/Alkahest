@echo off
REM ca_playtest57.cmd -- barre y sube el playtest 57 (fix del beat de la columna + lo que venga)
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
git add -A
git commit -m "Playtest 57: el beat de la columna ya se explica -- experimento, criterio y lugar de entrega en el texto"
git push origin main
pause
