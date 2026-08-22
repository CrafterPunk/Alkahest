@echo off
REM ca_playtest69.cmd -- ronda 69: fantasma "es obra del taller" (reserva de la mufla + terrazas/pilastras), colision AABB exacta al tamano real del imp, sandwich MachineBack-Sim-MachineFront piloto en el Crisol
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
git add -A
git commit -m "Ronda 69: fix 'es obra del taller' fantasma (ReservasDelPlano aparte del anticincel; terrazas/pilastras tallables), colision AABB exacta 4.2x6.0 celdas medida contra el sprite, y sandwich del recipiente (FondoInterior -8 + RebordeRecipiente 35) piloto en camara y cesto del Crisol; verificado en runtime y con capturas"
git push origin main
pause
