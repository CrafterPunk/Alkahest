@echo off
setlocal
title Limo Primordial - commit PLAYTEST 50b (la pistola humeante del multi: el hash cero)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 50b: LA PISTOLA HUMEANTE DEL MULTI -- el GlobalObjectIdHash cero de la escena generada era la causa raiz de TODA la saga 'StartHost devolvio false' (pt42/47b/50)" -m "El generador de la escena MULTI creaba los NetworkObject por codigo y guardaba el .unity en la misma pasada: un objeto sin persistir no tiene GlobalObjectId valido y su hash se serializaba en 0. Dos objetos a 0 -> NGO lanza dentro de HostServerInitialize, se traga la excepcion y StartHost devuelve false MUDO. Intermitente: recien generada la escena funciona (por eso mis verificaciones pasaban) y al reabrir Unity los ceros vuelven del archivo (por eso a Cesar le fallaba). Fix: SellarGlobalObjectIdHashes en el builder -- guardar primero, regenerar hashes, verificar unicidad, guardar otra vez, imprimir la tabla. Verificado en vivo: hashes unicos en el .unity y 'Anfitrion listo' a la primera. Detalle: HANDOFF seccion Playtest 50b."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
