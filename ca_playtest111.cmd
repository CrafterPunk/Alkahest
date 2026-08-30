@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 111: la rueda aprende a volver a casa ===
git add -A
git commit -m "Ronda 111: LA RUEDA APRENDE A VOLVER A CASA - paso de zoom 0.28 a 0.065 (una muesca ya no salta 0.69 de factor: ~9 muescas cubren el alejar) + RETEN DEL DEFECTO (cruzar el 1.0 frena exacto en la vista por defecto, el siguiente clic continua), un tick mas de cercania (tope 64 celdas, defecto sigue en 80), velocidad 4.0 a 4.8 (+20 por ciento pedido tras la prueba); la escala x1.6 queda sellada para probar (el mundo grande vendra de contenido, no de mas inflacion)"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
