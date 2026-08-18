@echo off
setlocal
title Limo Primordial - commit PLAYTEST 38 (informe del motor + Semilla Cero v2)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_managed_dlls.zip" del /f /q "_managed_dlls.zip"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 38: el informe del motor (medido, no estimado) + Semilla Cero v2 con las cinco enmiendas" -m "RONDA DE DIAGNOSTICO pedida por Cesar antes de congelar Semilla 0. EL BANCO HEADLESS (Tools~/BenchSim/Harness.cs): el SimStepper REAL corriendo fuera de Unity, compilado contra las DLLs de la propia build (regla 53). Numeros medidos: el peor caso sintetico -- MEDIO MUNDO de agua, 74.000 celdas activas -- cuesta 5,5 ms/tick de media y 11,6 de pico contra 33,3 de presupuesto a 30Hz; en juego real usamos el 2-5%%. Conclusion del informe: el cuello de botella del espectaculo no es el algoritmo, es que aun no le hemos pedido espectaculo. Menu de mejoras con costes en docs/INFORME_MOTOR.md; paquete recomendado ANTES de Semilla 0: particulas desprendidas + patina/manchas (la piedra quemada queda negra, lo mojado se oscurece) + gases con corrientes (~2 ms extra en el peor caso). Cuerpos rigidos estilo Noita: NO (caro, rompe el sync, no es nuestro juego)." -m "SEMILLA CERO v2 (docs/DISENO_SEMILLA_CERO.md): las cinco sugerencias externas ACEPTADAS y curadas -- (1) el bautizo se GANA: nombre provisional descriptivo ('sedimento celeste') y el rito llega cuando el MAESTRO se harta de decirlo ('no pienso seguir diciendo sedimento celeste: ponle nombre'); (2) el FRACASO FORENSE ascendido a LEY DE DISENO (regla 54: nada desaparece -- queda ceniza, tizne, gas y una nota en el diario; la ceniza es combustible malo); (3) cada maquina se desbloquea con su PREGUNTA literal como texto del pedido; (4) curriculo de 4 ideas profundas (temperatura, estado, densidad, conductividad) + 1 emergente (la historia del proceso) -- el resto queda fisicamente presente pero descurriculado; (5) final ABIERTO ('No necesito nada mas por hoy. ...Pero queda limo.') con el anzuelo silencioso del vasito del alambique goteando toda la sesion, y un contador local de acciones post-tutorial: la metrica reina del juego. Estructura final: milagro -> comprension -> pequeno fracaso -> comprension mayor -> autonomia. Orden propuesto: ronda de motor -> Semilla 0 -> playtest de medida."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
