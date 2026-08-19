@echo off
setlocal
title Limo Primordial - commit PLAYTEST 42 (hotfix multi: el StartHost mudo ahora se explica solo)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_managed_dlls.zip" del /f /q "_managed_dlls.zip"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 42: hotfix multi -- el 'StartHost devolvio false' mudo ahora diagnostica su causa" -m "Captura de Cesar: ANFITRION decia 'Abri tu taller en modo LOCAL: puedes jugar' y justo debajo 'Algo fallo: StartHost devolvio false' -- contradictorio y sin causa accionable. Causa mas probable en una prueba de dos ventanas: la otra ventana ya era anfitriona del puerto 7777 (o un proceso viejo lo retenia) y el bind UDP de UTP falla con un false mudo. FIX en SessionCoordinator.StartHost(local): (1) guarda de sesion-a-medio-cerrar (NGO escuchando con el coordinador Offline -> se cierra y se pide UN reintento); (2) sonda UDP de usar-y-tirar sobre el 7777 ANTES de intentar (PuertoUdpLibre): si esta ocupado, el error dice exactamente que hacer -- 'si esta es tu SEGUNDA ventana pulsa UNIRME (solo una puede ser ANFITRION); si no hay otra, un proceso viejo retiene el puerto'. Y TallerSesionHud redacta el aviso del fallback DESPUES de intentar el arranque: si el local no abrio, ya no promete 'puedes jugar' -- remite al motivo de abajo. Para probar en el exe: rehacer la build (menu Alkahest -> 4). Compilado regla 53: 0 errores."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
