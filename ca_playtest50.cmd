@echo off
setlocal
title Limo Primordial - commit PLAYTEST 50 (EL REWORK DE SEMILLA CERO)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_managed_dlls.zip" del /f /q "_managed_dlls.zip"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 50: EL REWORK DE SEMILLA CERO -- guion compacto, camino ensenado, fichas que tu cierras, placas mono-funcion, y SEMILLA CERO COMPARTIDA en multi" -m "Mandato de Cesar: rework profundo para elevar la calidad. FISICA: la emision termica gana DIRECCION -- la estufa jamas enfria, la nevera jamas calienta (la placa-termostato que hacia las dos cosas era irreal y Cesar la cazo). UN SOLO FUEGO EN EL MINUTO 0: la placa de calor ya no nace al arranque de seed 0 -- nace JUNTO a la piedra gelida en el beat del frio (la leccion de temperatura llega con el par completo). FICHAS QUE TU CIERRAS: cada descubrimiento abre su ficha-vitrina en pantalla (nombre real + resena) y espera tu cierre -- nada se desvanece sin leerse; la cola cede el turno al cierre. GUION COMPACTO: cantidades 25/15/15 -> 10/8/8 y preguntas a 6; LA TOLVA CERCANA dentro del cuarto (la lejana ya no existe en seed 0), late cuando hay pedido, su rotulo ensena el gesto (vierte con clic derecho), el primer pedido trae el consejo completo y el HUD del pedido lleva FLECHA hacia la Tolva. El 5.4 ya no presume 'resista': 'algo que sobreviva al rojo sin arder ni fundirse -- lo bien cocido aguanta'. Y LA TERCERA SECCION: boton ANFITRION -- SEMILLA CERO compartida en el lobby multi (mundo 777002 + quimica real + veta + salas destapadas, sin guion: laboratorio para pruebas en simultaneo; el invitado hereda nombres reales y cruces via la seed del handshake). Costura pt48 cerrada: CrearMundoAnfitrion con try/catch ruidoso. Regla 12 barrida: 10 archivos de input respetan la ficha modal. Compilado regla 53: 0 errores. Contrato: docs/CONTRATO_RONDA50.md; detalle: HANDOFF seccion Playtest 50."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
