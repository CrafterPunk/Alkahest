@echo off
setlocal
title Limo Primordial - commit PLAYTEST 48 (EL DESATASCO: la veta a la vista, el multi que falla limpio, los dos fuegos que se explican)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_managed_dlls.zip" del /f /q "_managed_dlls.zip"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 48: EL DESATASCO -- la veta de turba a la vista (Semilla Cero por fin completable), el multi que falla limpio, y los dos fuegos que se explican" -m "D1 EL ECLIPSE DE LA VETA (regla 57 nueva): la extraccion del limo elige la banda MAS ALTA <= cima y el override de Semilla Cero empujaba la base combustible por DEBAJO de la arena -- eclipse permanente: sin turba no hay carbon, sin carbon la calcinacion (130) era inalcanzable a rescoldo (120) y el beat 4 era IMPOSIBLE: nadie pudo haber completado el arco jamas. Fix: VETA DE TURBA tallada en el muro del cuarto intimo (tallala con C, el brasero la come) + ESCALERA POR DECRETO: rescoldo 120->arena(100), turba 130->ARCILLA(124), ceniza 145->CALIZA(136), carbon ~185->SAL(158), con log de arranque y verificacion ARGMAX real. D2: la arena ya NO se disuelve (el 'tinte' sin nombre que viste era la solucion de arena, fisicamente falsa y con nombre pobre -- doble falta). D3: StartHost false ahora cierra TODO y captura la excepcion real de NGO; tres guardas en HandleLobbyJoined (el juego se unia a SU PROPIO lobby como invitado: por eso 'Estas en el taller de otro' + mundo de ruido gris); LastError caduca; boton JUGAR SOLO EN ESTE PC; pausa y AJUSTES desde el primer frame del lobby multi. L: fuera la resistencia zigzag roja ('la N roja horrible') -> losa de piedra con lecho de brasas que laten; rotulos de oficio (PLACA DE CALOR entibia la ZONA / el crisol TRANSFORMA por hornadas); brasero del crisol +63%% de area con pila visible y rotulo de carga camara/cesto. Compilado regla 53: 0 errores. Contrato: docs/CONTRATO_RONDA48.md; detalle: HANDOFF seccion Playtest 48."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
