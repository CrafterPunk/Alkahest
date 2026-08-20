@echo off
setlocal
title Limo Primordial - commit PLAYTEST 47b (hotfix: el tipo envenenado del menu)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_managed_dlls.zip" del /f /q "_managed_dlls.zip"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 47b: hotfix del tipo envenenado -- el juego 'salia roto' (sin titulo, sin HUD) por PlayerPrefs en inicializadores estaticos" -m "Causa raiz: el encargo M del menu cargaba los volumenes con PlayerPrefs.GetFloat en inicializadores de CAMPO ESTATICO (DayCycle y DirectorDeAudio). Unity prohibe su API fuera del hilo principal en momentos controlados: el .cctor lanzaba UnityException, el tipo quedaba ENVENENADO (TypeInitializationException) y todo OnGUI que consultara DayCycle.InputLocked explotaba en cascada -- juego entero roto SIN un solo error de compilacion, porque es una restriccion de runtime que el compilador fiel (regla 53) no puede ver. Fix: centinela -1 + carga perezosa en el primer acceso (propiedades VolGeneral / VolumenEfectos). Barrido del proyecto: ningun otro inicializador estatico llama API de Unity. Regla 56 nueva en CLAUDE.md. Verificado EN VIVO: el titulo LIMO PRIMORDIAL vuelve a renderizar con el boton AJUSTES nuevo." -m "Archivos: Game/DayCycle.cs, Audio/DirectorDeAudio.cs, CLAUDE.md (regla 56), docs/HANDOFF.md (seccion 47b)."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
