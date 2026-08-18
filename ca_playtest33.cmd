@echo off
setlocal
title ChaosAlchemy - commit PLAYTEST 33 (cimientos + arquitectura del interiorista)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 33: cimientos soberanos + la arquitectura del interiorista + cota por zona" -m "LOS BUGS DE CIMIENTOS (reporte de Cesar, causa raiz confirmada): las terrazas del 32 se tallaban ANTES de que las estaciones se registraran en ObraDelTaller -- el sistema de proteccion miraba un registro VACIO (maquinas enterradas, suelo con formita rara). Fix: registro desde el genesis + Init reclama el handle. PLATAFORMA SOBERANA: cada estacion aplana su huella+2 antes de tallar y al mudarse RESTAURA el suelo al nivel de la losa -- se acabo el rastro de bedrock. El foco del banco aterrizo (un +12f magico sin relacion con el travesano, regla 47; ahora una sola fuente de verdad + cordon visual). Y AVISO DE BAUTIZO al descubrir: banner 'ALGO NUEVO -- pulsa T para bautizarlo' por la cola de LEY DESCUBIERTA." -m "LA ARQUITECTURA (Opus con ojos, 3 ciclos jugando, brief de Cesar 'como el mejor disenador de interiores'): el cuarto crece (140..378 x 168..262, boveda hasta 274) con SEIS casquetes parabolicos y nervios sobre pilastras, TRES claraboyas ciegas con haz frio, tres aparejos de silleria por zona, dovelas, vigas, cadenas. NUEVE BALDAS fisicas con mensulas inclinadas de laton (los solidos se APOYAN de verdad): la linea sobre el horno que amaba Cesar es ahora una galeria de 33 celdas, mas repisas en las alturas para exhibir hallazgos. ZONIFICACION: humeda -> fuego (crisol+alambique) -> fuerza (prensa) -> ESCALINATA -> alcoba de observacion (columna+chispa juntas bajo la boveda alta) -> atrio -> ENSAYO casado con la Tolva en el vestibulo de entrega. LA LUZ DEJO DE SER STICKER: LuzDeMuro recortada a la mamposteria real con corte duro arriba ('el contenedor brillando sin incluir el techo'), halos 46->15/11, caida 2.2->3.6, latido atado a la intensidad real del hogar." -m "LA COTA POR ZONA (cierre de la deuda del arquitecto): BaseYDeEstacion(anclaX) -- la alcoba de observacion vive a +6 sobre la escalinata y el atrio del Ensayo a +3; resuelta POR ANCLA, no por tipo (regla 47); al mudar, cada estacion aplana a la cota donde cae. Verificado jugando: alcoba en alto, terrazas escalonadas, luz nacida del horno, 0 errores. Deuda declarada: PARTICULAS (el haz de claraboya las pide), objetos pequenos en la pared izquierda, mensulas de baldas cortas. Documentacion en HANDOFF seccion Playtest 33."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
