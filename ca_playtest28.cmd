@echo off
setlocal
title ChaosAlchemy - commit PLAYTEST 28 (POC multiplayer)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"

echo === add (todo lo restante: la capa de red) ===
git add -A

echo === commit ===
git commit -m "Playtest 28: EL TALLER COMPARTIDO - POC multiplayer a 4 jugadores con colores" -m "Mandato de Cesar: primera prueba de concepto multiplayer; 4 jugadores si es posible (lo es: lobby maxPlayers=4); cada jugador distinguible por COLOR del personaje; decisiones no especificadas tomadas por la direccion (contrato docs/CONTRATO_MULTI_POC.md)." -m "ARQUITECTURA: la sim vive SOLO en el anfitrion; los clientes llevan un ESPEJO (AlkahestSim.ModoEspejo, sin stepper) sincronizado por chunks RLE a ~5Hz via CustomMessagingManager (mensaje AlkChunks: chunk de piedra maciza = 6 bytes; snapshot completo 6-15KB CON LA SEED en cabecera para que el espejo cree el mismo universo). Avatares: prefab generado por el editor con OwnerNetworkTransform + PlayerIdentity del template + Net/AprendizNet.cs (color replicado: DORADO anfitrion / AZUL CIELO / VERDE / MAGENTA). El frasco de los invitados funciona entero: lee del espejo, PREDICE en local y reenvia la pintura al host por lotes (sin prediccion duplicaba materia -- desviacion razonada del contrato). Maquinas/encargos/cincel/mudanza solo anfitrion: los invitados acarrean, el anfitrion hornea -- cooperacion real desde el POC." -m "ESCENA APARTE: menu 'Alkahest/2. Generar escena Lab MULTI' + 'Alkahest/4. Build MULTI Windows'; la escena clasica queda intacta (todo lo nuevo tras SimSync.EnEscena). Template FriendsLoop NO tocado. LA PRUEBA: build MULTI, abrir el exe DOS VECES con -transport local, ANFITRION en una y UNIRME local en la otra; con amigos: sin -transport, lobby de Steam e invitacion por overlay." -m "Red: Opus 5 (sin Unity disponible: disciplina de API calcando el template; las DUDA-API quedaron concentradas en la capa CustomMessagingManager de SimSync.cs). Direccion e integracion: Fable 5. Documentacion en HANDOFF seccion Playtest 28."

echo === push ===
git push origin main
echo.
echo === COMPROBACION DEL PUSH ===
git status -sb | head -1
git log --oneline -3
echo ============================================
echo  LISTO
echo ============================================
pause
