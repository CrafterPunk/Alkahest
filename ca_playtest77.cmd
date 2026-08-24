@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 77: herramientas de Cesar (overlay del cincel) + verificacion multi R76b ===
git add -A
git commit -m "Ronda 77: overlay del cincel (guardar forma como plano desde F3, reaplicado tras BuildFundacion, respeta obra y zona del derrumbe), mapa de zonas del prologo en Scene view, copiar celda y captura con coordenadas, rumor de cascada por distancia, juego libre cierra por conducta. R76b: la recarga post-sesion espera el shutdown asincrono de NGO (verificado en vivo el ciclo host-caida-recarga-rehost)"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
