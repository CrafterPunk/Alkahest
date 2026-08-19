@echo off
setlocal
title Limo Primordial - commit PLAYTESTS 44 y 45 (ronda nocturna completa: fisica honesta + quimica con nombre real)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_managed_dlls.zip" del /f /q "_managed_dlls.zip"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtests 44+45 (ronda nocturna): LA FISICA HONESTA (placas realistas, termometro, conversion por frentes, particulas baratas fuera -- ver HANDOFF pt44) + LA QUIMICA CON NOMBRE REAL -- arena de silice, vidrio, ceramica, carbon, salmuera; el album de figuritas con reseñas de trivia" -m "Ronda nocturna 2/2 (documento rector docs/DISENO_QUIMICA_REAL.md con la tabla canonica de 48 identidades). El reticulo YA fabricaba cosas reales -- el pivote es un CONTRATO DE IDENTIDAD sobre la seed 777002: cada material del arco con su mejor referente real (nombre + color creible + mini reseña de trivia verbatim de la tabla): arena de silice -> vidrio (templado) / vitroceramica; arcilla -> ceramica / adobe / barbotina; caliza -> cal viva / clinker; VETA VEGETAL -> carbon vegetal (el combustible garantizado ES carbon: se fabrica, no se regala); sal -> salmuera que conduce. El limo = LODO DE CANTERA (lodo mineral real; todo se separa por temperatura). Beat 3 reescrito: el Maestro ENSEÑA el nombre real ('Eso es ARENA DE SILICE, aprendiz. Apuntalo.') en vez de exigir inventarlo. EL ALBUM (tecla B + 5a pestaña del diario): arbol de figuritas por familias con siluetas grises que se llenan al descubrir, aristas con VERBO (la pista, jamas la receta), progreso N/M, medallon dorado pulsante al descubrir + ficha-vitrina con el lenguaje visual del rito de bautizo (swatch, nombre real, reseña, 'Anotado en tu album'). El conocimiento del mundo real como tutorial invisible: quien sabe que el vidrio nace de arena fundida, ya sabe jugar. El modo caotico conserva integro su sistema anonimo/bautizo. Compilado regla 53: 0 errores."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
