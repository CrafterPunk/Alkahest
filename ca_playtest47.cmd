@echo off
setlocal
title Limo Primordial - commit PLAYTEST 47 (FASE A: recetas cruzadas, renames, resistencias, menu y volumen)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_managed_dlls.zip" del /f /q "_managed_dlls.zip"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 47: FASE A completa -- las recetas de la humanidad (mortero, clinker, hormigon, vidrio de botella, lejia, esmaltado), renames de la auditoria, resistencias anotadas, y el juego por fin tiene menu y volumen" -m "GO de Cesar al plan del INFORME_REALIDAD (contrato docs/CONTRATO_FASE_A.md). LA MEZCLA EN CUBETA: si la camara del crisol lleva dos materiales (dominante + secundario >=20%%), la tabla de cruces decide UNA transformacion: cal apagada+arena->MORTERO 'amasando' - caliza+arcilla a fuego pleno->CLINKER 'cociendo clinker' - clinker+arena->HORMIGON 'fraguando' (22-28s de fraguado) - arena+ceniza->VIDRIO DE BOTELLA 'fundiendo con fundente' (funde a banda MAS BAJA que la arena pura: la leccion real del fundente, verde por el hierro de la ceniza) - ceniza+agua->LEJIA 'lixiviando' - bizcocho+arena->ESMALTADO. Seis materiales nuevos (Count 59->65) con identidad real completa y pagina 7 del album 'MEZCLAS DEL OFICIO' con las recetas como preguntas. Los 4 renames de la auditoria de realidad: sal de estampido (decrepitacion real), caliza prensada, cal sobrecocida (con la pista del cruce en su reseña), ambar de brea. RESISTENCIAS ANOTADAS (primer paso del principio 'todo camino da algo'): 'resiste este fuego' y 'resiste la prensa' quedan en la ficha -- el resultado negativo tambien es botin. MENU: AJUSTES en el titulo con volumen general y de efectos (persistidos), PAUSA con Escape (escalera de guardas documentada; un jugador congela la sim, multi no por garantia estructural), VOLVER AL TITULO limpio en ambos modos. Compilado regla 53: 0 errores. FASE A LISTA PARA LA PRUEBA COMPLETA DE CESAR."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
