@echo off
setlocal
title Limo Primordial - commit PLAYTEST 52 (Semilla Cero co-op guiada)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 52: SEMILLA CERO CO-OP GUIADA -- el arco entero del Maestro, con amigo" -m "La compartida deja de ser laboratorio destapado: el director de beats corre en el ANFITRION del multi (el invitado jamas lo instancia), las salas se tapian tambien alla (la piedra le llega al invitado por chunks), la VOZ DEL MAESTRO se replica (SaberSync, mismo panel dorado en ambos lados) y las FICHAS de descubrimiento se abren solas tambien en el invitado (el camino ya disparaba el teatro correcto -- auditado; el catch-up tardio entra por la cola con respiro). Bug real cazado de paso: las descripciones de pedido largas se truncaban a mitad de frase para el invitado -- recorte por bytes con elipsis. Decision documentada: las 6 estaciones nacen al arrancar la sesion (MaquinaSync no admite altas tardias -- deuda), pero sus salas siguen selladas de piedra hasta su beat: la guia la hace el muro. Compilado regla 53: 0 errores. Detalle: HANDOFF seccion Playtest 52."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
