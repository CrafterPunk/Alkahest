@echo off
setlocal
title Limo Primordial - commit PLAYTEST 49 (el ritmo, el ensayo que ensena, y las dos placas de la foto)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_managed_dlls.zip" del /f /q "_managed_dlls.zip"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 49: EL RITMO Y EL ENSAYO QUE ENSENA + las dos placas de la foto (Opus con ojos)" -m "El 'nada aguanta el rojo' NO era la fisica (verificado en vivo: 45/45 celdas de ladrillo molido sobreviven al 177): eran dos trampas mudas -- el frasco conserva la temperatura (el carbon recien horneado se encendia solo en la bandeja) y los liquidos se templan en el viaje (la brea llegaba como ambar). Fixes: el altar TEMPLA la muestra antes de juzgarla; el veredicto de muerte nombra en que se convirtio la muestra; a los 2 fallos el Maestro suelta la pista (diario / lo cocido / en frio); y la CERAMICA pasa a ser el techo de resistencia (estaba invertida: 180 contra 188 de su propio polvo). RITMO: el teatro de los descubrimientos sale de a UNO con 10s de respiro y nunca detras de un panel abierto -- el registro sigue siendo inmediato, nada se pierde. PLACAS (Opus con ojos, 4 ciclos con capturas en el PC real): DOS aparatos inconfundibles como en la foto de Cesar -- placa de calor = losa de fundicion oscura con serpentin incandescente pulido y bornes de laton; placa fria = regleta metalica clara con dientes-prisma de escarcha, sin violeta. Compilado regla 53: 0 errores. Detalle: HANDOFF seccion Playtest 49."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
