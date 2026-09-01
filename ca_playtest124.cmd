@echo off
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
echo === RONDA 124: la piel de roca (marching squares sobre la roca madre, sim intacta) ===
git add -A
git commit -m "Ronda 124: LA PIEL DE ROCA - prueba visual: contorno organico tipo marching squares SOLO sobre Stone, dibujado por debajo de la sim (la materia sigue cuadrada y la sim intacta); 4 capas (sombra+halo, relleno con masa interna, bandas suelo/pared/techo + tinta, decoracion procedural), textura de roca procedural, actualizacion por hash de chunk (0.035 ms/chunk); F7 rota 5 niveles, Ctrl+F7 cueva de muestra; SimRenderer.OcultarRoca; nota docs/PRUEBA_PIEL_DE_ROCA.md + comparacion docs/ref; ROADMAP 2.7; HISTORIAL R124 (incluye la R123 - evaluacion 2D vs 2.5D - si aun no se habia subido)"
git push
echo.
echo === Listo. Este script ya se puede borrar (o mover a _to_delete). ===
pause
