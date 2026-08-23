@echo off
REM ca_playtest70.cmd -- ronda 70 (domingo de ajustes): maquinas atravesables, esquinas suaves (chaflan octagonal + deslizamiento asistido), parallax 3% del fondo, titulo TEN THOUSAND YEARS + telon oscuro. Incluye ademas el fix 69g del multi (fuga ModoFundacion) que no alcanzo el push anterior.
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
git add -A
git commit -m "Ronda 70: el imp ya no colisiona con las maquinas (ObraDelTaller excluida de la caja), esquinas suaves (caja octagonal + corner correction de 1.5 celdas), parallax 3%% del muro de fondo, titulo TEN THOUSAND YEARS con telon opaco en el menu; + fix 69g del multi (fuga de ModoFundacion al espejo/anfitrion) rescatado del push anterior; verificado con sondas en runtime y captura del titulo"
git push origin main
pause
