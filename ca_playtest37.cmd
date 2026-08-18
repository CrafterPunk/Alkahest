@echo off
setlocal
title Limo Primordial - commit PLAYTEST 37 (hotfix compilacion + el compilador del sandbox)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 37: hotfix del error de compilacion del 36 + nace el compilador Unity-fiel del sandbox (regla 53)" -m "EL ERROR (reporte de Cesar: 'All compiler errors have to be fixed'): UNA linea -- CS0030 en SaberSync:340, cast invalido (string)FixedString128Bytes en la comparacion de cambios de encargos. Despistaba que los menus de editor SI corrian: Unity mantiene los ensamblados del ultimo compile bueno, asi que la generacion de escenas funcionaba con codigo viejo mientras el nuevo no compilaba. Fix: comparacion por Equals sin alloc + RecortarDescripcion como unico punto de verdad del recorte a 120 chars (si el volcado y la comparacion recortaran distinto, un encargo largo se re-difundiria cada sondeo para siempre)." -m "LA HERRAMIENTA NUEVA (regla 53 de CLAUDE.md, el fin del 'despliega y reza'): COMPILADOR UNITY-FIEL EN EL SANDBOX -- las DLLs de la build real del juego (Builds/ChaosAlchemyMulti/Managed) + dotnet csc con los defines del proyecto. Encontro al primer intento el unico error real que tres auditorias de simbolos a mano no vieron. Desde ahora es OBLIGATORIO antes de todo despliegue. Verificado: 0 errores contra las DLLs reales de la build."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
