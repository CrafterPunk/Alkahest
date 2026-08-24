@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 76: estabilidad multi + prologo solo-single ===
git add -A
git commit -m "Ronda 76: review Opus de estabilidad multi aplicado (recarga en fin de sesion involuntario, estaticas de sesion limpiadas en OnDestroy, temperatura del cliente no pisa la sim, cerrojos liberados al desconectar, R12 completa en replicas, reintento de snapshot, guardas prologo-solo-single) + leccion git --no-optional-locks"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
