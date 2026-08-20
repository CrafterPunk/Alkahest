@echo off
setlocal
title Limo Primordial - commit PLAYTEST 51 (el recetario del laboratorio y la placa honesta)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 51: EL RECETARIO DEL LABORATORIO y LA PLACA HONESTA" -m "La Semilla Cero compartida corria el arco clasico y su segundo pedido ('algo que aguante el rojo sin ceder') era de final de partida: confuso e inalcanzable -- Cesar lo cazo. Ahora el laboratorio multi tiene su RECETARIO propio de 5 pedidos que ensena la cadena temprana real: 10 arena de silice -> 8 turba (tallala con C) -> 6 carbon vegetal -> 6 arena tostada -> 6 barbotina. El pedido de calor del caotico se reescribe claro: 'Trae al ENSAYO algo que sobreviva al rojo sin arder ni fundirse -- lo bien cocido aguanta (ceramica, ladrillo)'. LA PLACA HONESTA: fuera el halo rojo flotante (las particulas reales del CA son el espectaculo), fuera el estado TIBIO del ciclo (fosil del vivium aparcado; queda Off<->Ardiente), y el rotulo por fin explica el oficio: 'Hierve, derrite y seca la ZONA -- transformar materia es oficio del CRISOL' (que no calcine arena a 320 es la fisica correcta del reticulo-por-hornada, solo faltaba el cartel). Compilado regla 53: 0 errores. Detalle: HANDOFF seccion Playtest 51."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
