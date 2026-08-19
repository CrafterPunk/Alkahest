@echo off
setlocal
title Limo Primordial - commit PLAYTEST 43 (LA PARIDAD VIVA: el invitado usa, ve y oye; el frasco fluye)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_managed_dlls.zip" del /f /q "_managed_dlls.zip"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 43: LA PARIDAD VIVA -- el invitado usa las maquinas, las ve trabajar y las oye; el frasco fluye a 15Hz" -m "Reporte de la primera prueba real con un amigo (contrato docs/CONTRATO_PARIDAD.md, diagnostico antes de encargar): las replicas eran estatuas no-interactivas, el registro no llevaba estado vivo, el audio ni se spawneaba en el invitado y toda la difusion iba a 5Hz. ENCARGO N: interfaz IMaquinaUsableRemota implementada por las 7 maquinas (el cuerpo del E local extraido a un metodo compartido), SolicitarUsoServerRpc con validacion de cercania server-side (14 celdas del centro real), estadoVivo por bits en el registro replicado (Trabajando/Fuego/Listo/Sirviendo/Luz) sondeado a 4Hz solo-si-cambio, y las replicas ahora ofrecen 'E -- usar' (arbitraje del mas cercano), laten al trabajar, brillan con el brasero y destellan con resultado listo, con segunda linea de estado en la chapa. ENCARGO A: DirectorDeAudio en el invitado con MODO ESPEJO (sin stepper, el ambiente sale de observar la grilla replicada en la ventana de camara a 4Hz: crepitar/chapoteo/siseo), voces de grifo ancladas a las replicas encendidas por el bit Sirviendo, one-shots por transicion de estado; la pasada de prioridad de chunks (60 celdas de avatar) pasa de 6 a 2 ticks (~15Hz, peor caso realista ~155KB/s), el resto queda igual; Flask medido y NO tocado (su lote ya vaciaba por frame -- el cuello era la vuelta)." -m "BONUS del reporte esporadico 'restos de bedrock que no se pueden quitar': el rechazo del cincel por obra del taller era MUDO (jambas/marcos se leen como piedra normal) -- ahora avisa ('es obra del taller -- no cede; las estaciones se mueven con V') y el hover del F3 marca '· OBRA'; si se repite sin el aviso, la instrumentacion dira donde mirar. Compilado regla 53: 0 errores. La prueba de dos jugadores queda en manos de Cesar (checklist en HANDOFF/informes)."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
