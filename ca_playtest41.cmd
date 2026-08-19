@echo off
setlocal
title Limo Primordial - commit PLAYTEST 41 (EL VAPOR VIVO: hervir de verdad, gas con rumbo, color que no miente)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_managed_dlls.zip" del /f /q "_managed_dlls.zip"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 41: EL VAPOR VIVO -- el agua hierve de verdad, el gas tiene rumbo y el color en caliente ya no miente" -m "TRES CAUSAS RAIZ del feedback de Cesar (contrato docs/CONTRATO_VAPOR.md, diagnostico antes de encargar): (1) el agua NUNCA tuvo rama de hornada -- DecidirHornada cortaba con EsBaseEstado; rama nueva 'hirviendo' (Water -> Steam real, la camara entera se vacia en vapor VISIBLE; hallazgo: el gas nacido por SetCell directo llegaba con aux==0 y moria en su primer tick, mismo bug que ProcessFire pt9, sembrado con sal 553). (2) El tinte termico fundia el color al 100%% hacia ambar -- ahora brasa ADITIVA + mezcla acotada a techo 0.45 (a 320 C la mezcla es 0.245: 'el azul, al blanco', nunca 'material amarillo distinto'). (3) La lateralidad del gas re-sorteaba direccion cada tick -- ahora RUMBO/VIENTO coherente por hash de baja frecuencia (sal 551: misma celda mantiene rumbo ~0.5s, bloques de 8x8 comparten corriente), ondulacion 30%% en ascenso libre (sal 549: nada de vertical perfecta), escape bajo techo diagonal->lateral con rumbo sostenido. MEDIDO con diagnosticos headless: dispersion lateral +52%%, escape bajo saliente +121%%, banco peor caso +3.5%% (escenarios sin gas planos)." -m "ENCARGO VISUAL (Opus con ojos, 2 pasadas desplegadas al PC real con la misma seed 187415343, capturas antes/despues): bocanadas de chimenea degradadas a acento (alfa 0.34, fade de entrada, tablas por indice de periodo/altura/rizo/deriva -- antes 4 bocanadas identicas desfasadas = carrusel), vaho reubicado a la SUPERFICIE del penacho (dentro de la masa era invisible), Alambique sin tocar (su vapor ES el gas real: verificado el ciclo columna->respiradero->'agua destilada: 7'). Verificado en vivo: agua + E -> 'hirviendo', columna que serpentea, se escora, hace champinon bajo la boveda y se embolsa. Deudas en HANDOFF (rotulo 'llevas 0' del alambique, grep de otros SetCell con gasLifetime). Compilado regla 53: 0 errores."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
