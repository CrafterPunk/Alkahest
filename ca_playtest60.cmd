@echo off
REM ca_playtest60.cmd -- ronda 60: tag del ultimo clasico + Limpieza L1 + greybox del inicio oscuro
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
REM El tag apunta al estado ANTERIOR a la limpieza (el push del playtest 59):
git tag -f ultimo-clasico HEAD
git add -A
git commit -m "Ronda 60: Limpieza L1 (Favor fuera del HUD, patentes apagadas) + EL INICIO OSCURO en greybox (ModoFundacion, BuildFundacion, FundacionDirector)"
git push origin main
git push origin ultimo-clasico
pause
