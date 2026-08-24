@echo off
REM ca_playtest71.cmd -- ronda 71: tres puertas del menu (Prologo / Modo Normal-Semilla Cero / Modo Caotico), eslogan del GDD, unificacion de nombres (ChaosAlchemy->TenThousandYears en 34 archivos, menus "Ten Thousand Years/", builds nuevas, productName), velocidad -40% con rampa imperceptible, y fondo+parallax en el prologo.
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
git add -A
git commit -m "Ronda 71: menu de tres puertas (Prologo/Modo Normal-Semilla Cero/Modo Caotico, mismos flags y puntos de inicio), eslogan oficial del GDD en espanol, unificacion ChaosAlchemy->TenThousandYears (logs, clips, prefs, menus de Unity, builds, productName), velocidad del imp -40%% (6.7) con aceleracion imperceptible (96), y el prologo gana WorkshopBackdrop (la causa de 'no vi el parallax'); verificado con sondas y captura del menu nuevo"
git push origin main
pause
