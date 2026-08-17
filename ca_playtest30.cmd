@echo off
setlocal
title ChaosAlchemy - commit PLAYTEST 30 (arranque multi blindado + zoom + menu + manual)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"

echo === limpiando restos de despliegues ===
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 30: arranque multi blindado + zoom adaptativo + menu renumerado + MANUAL DEL TALLER" -m "EL ERROR 'No hay una referencia a NetworkManager configurada en SessionCoordinator' (reporte de Cesar al generar la escena MULTI): el cableado de referencias del generador vivia AL FINAL del metodo -- cualquier excepcion intermedia dejaba el coordinador creado pero sin cablear, y la escena guardaba ese estado a medias; el error solo explotaba al pulsar ANFITRION. Doble blindaje: (1) las referencias criticas se asignan EN LA LINEA SIGUIENTE a crear el componente (sin ventana de fallo) y (2) red de seguridad en runtime: SimSync.Awake recablea por reflexion si la escena llego rota, con aviso de regenerarla. Nota alegre del mismo reporte: 'Steamworks inicializado correctamente. Jugador: CrafterPunk' -- el fix del steam_appid.txt funciono." -m "ZOOM 'VA MUY LENTO': la rueda reporta en DOS escalas segun dispositivo (+-120 por muesca o +-1) y la normalizacion fija /120 convertia la escala pequena en pasos microscopicos. Normalizacion adaptativa + paso 0.28: dos o tres muescas cubren todo el rango." -m "MENU ALKAHEST RENUMERADO (habia dos '2'): 1. Generar escena Lab (un jugador) / 2. Generar escena Lab MULTI (taller compartido) / 3. Build demo Windows (un jugador) / 4. Build MULTI Windows (taller compartido) / 5. Abrir carpeta de builds. Las dos escenas son SIEMPRE la ultima version: la de un jugador es la mesa de pruebas limpia (sin capa de sesion, itera rapido y aisla bugs de juego vs bugs de red); la MULTI es el juego real." -m "NUEVO docs/MANUAL_MAQUINAS.md: el manual del taller completo pedido por Cesar -- las cinco estaciones con su uso exacto (crisol por hornadas y su escalera de combustibles, prensa y sus cuatro respuestas, columna de ensayo explicada -- observa capas, no transforma --, banco de chispa, ensayo del Maestro con estrellas), los dos enfriamientos templado/recocido, la gravedad con cohesion para construir, mudanza/cincel, y el arco de partida en 7 pasos para ensenarselo a un amigo. Pendiente de la proxima ronda: maquinas como objetos de red (mudanza para invitados) y mas vidrio para la columna ('la maquina escalera')."

echo === push ===
git push origin main
echo.
echo === COMPROBACION DEL PUSH (mira esto antes de cerrar) ===
git status -sb | head -1
git log --oneline -3
echo ============================================
echo  LISTO
echo ============================================
pause
