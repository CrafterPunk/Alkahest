@echo off
setlocal
title Limo Primordial - commit PLAYTESTS 44-46 (fisica honesta + quimica real + album digno + informe de realidad)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_managed_dlls.zip" del /f /q "_managed_dlls.zip"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtests 44-46: LA FISICA HONESTA + LA QUIMICA CON NOMBRE REAL + EL ALBUM DIGNO DEL BAUTIZO + el informe de realidad" -m "PT44 (fisica): placas de calor/frio de vuelta con fisica compartida realista (falloff + empuje por diferencia + collar anti-inundacion: -26 en placa, -4 a dos celdas, 20 lejos, verificado con sondas), termometro tecla G con hasta 3 sondas vivas en grados, conversion POR FRENTES en el crisol (el tostado se propaga de a pocos), particulas decorativas y patina de mojado APAGADAS por pedido, beat del FRIO en Semilla Cero ('Y si lo ENFRIAS?' -> 'Traeme HIELO -- apurate'). PT45 (quimica real): tabla canonica de 48 identidades con nombre/color/reseña de trivia (arena de silice->vidrio, arcilla->ceramica, caliza->cal viva, veta vegetal->carbon, sal->salmuera; el limo = lodo de cantera), el Maestro ENSEÑA los nombres reales, ALBUM de figuritas (tecla B). PT46 (el album digno): librito de cuero y laton que late en luz (no mas medallon rojo), ficha-vitrina calcada de la anatomia del bautizo con firma visual real y contador '1 de N' (el solapamiento tenia DOS causas vistas en pantalla: album+ficha no excluyentes y verbos apilados con Overflow -- ambas muertas), album con PAGINAS (una familia por doble pagina, vitrinas + arbol de verbos), y el aprendiz ya no vuela detras del velo. INFORME DE REALIDAD (docs/INFORME_REALIDAD.md): auditoria de verdad entrada por entrada (82%% solido, 4 renames propuestos), maquinas = operaciones unitarias, recetas cruzadas dormidas (mortero/cemento/hormigon/vidrio verde/lejia/esmalte), principio TODO CAMINO DA ALGO, ranking de expansion (recetas cruzadas -> mena -> electrolisis -> fermentacion) y ley editorial: simplificar la realidad, jamas contradecirla. Compilado regla 53: 0 errores."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
