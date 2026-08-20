@echo off
setlocal
title Limo Primordial - commit PLAYTEST 54 (sobriedad y paridad: el buzon del Maestro)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 54: SOBRIEDAD Y PARIDAD -- el Buzon del Maestro, la mitad de soportes, y la fuga que hacia distintos al solo y al multi" -m "Cesar comparo capturas: las dos versiones de la seed 0 no eran iguales -- causa raiz cazada: Balda/Anclaje/Pila guardaban banderas estaticas 'ya creadas' sin reset, y quien entraba SEGUNDO a un modo en el mismo proceso se quedaba sin muebles. Fix: ResetGuardaEstatica en los tres, llamado en ambos arranques. SOBRIEDAD: soportes a la mitad (galerias 10->5, baldas 17->8, repartidas), redomas del estante FUERA de la seed 0 (mobiliario del caotico), y la Tolva cercana renace como EL BUZON DEL MAESTRO: elevado y lateral junto al alambique, buzon de piedra con marco de laton fino y relieve de pergamino (comunica 'entregas' sin letras) -- fuera las jambas doradas, la flecha flotante y el letrerote del camino; rotulo solo de proximidad y halo sutil. El crisol vuelve a mandar el minuto 0. Compilado regla 53: 0 errores. Detalle: HANDOFF seccion Playtest 54."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
