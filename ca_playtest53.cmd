@echo off
setlocal
title Limo Primordial - commit PLAYTEST 53 (el registro incremental: las maquinas ya no flotan sobre sus muros)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 53: EL REGISTRO INCREMENTAL -- las maquinas del co-op nacen por beat otra vez y ya no flotan sobre sus muros sellados" -m "La captura de Cesar mostro las 6 estaciones dibujadas ENCIMA de la piedra sellada (en co-op nacian todas al inicio porque el registro de MaquinaSync se publicaba una sola vez). Cura de raiz: MaquinaSync acepta ALTAS TARDIAS (el registro solo crece, sondeo de 0.5s que se auto-apaga al completar las 6; lado invitado cero codigo nuevo) y el co-op vuelve a spawnear cada estacion al destaparse su sala, igual que un jugador -- ventana muro-cae a replica-aparece de medio segundo. Deuda prioritaria confirmada con archivo:linea: re-hostear sin cerrar el juego fuga estado (workaround: VOLVER AL TITULO y reentrar antes de re-hostear). Compilado regla 53: 0 errores. Detalle: HANDOFF seccion Playtest 53."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
