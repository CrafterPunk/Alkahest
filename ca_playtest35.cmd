@echo off
setlocal
title Limo Primordial - commit PLAYTEST 35 (los ajustes: apariciones, pilas separadas, F9 limpio)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 35: los dos 'no aparecen' eran espejos + pilas separadas de los grifos + F9 limpio" -m "LAS DOS DESAPARICIONES (reporte de Cesar, causas confirmadas): StorageRack y Alambique solo se spawneaban en TrySpawn (un jugador) -- en multi el anfitrion nunca los creaba; y los soportes nuevos (baldas/anclajes/deposito) se spawneaban tras un gate EsServidor que en la escena CLASICA (sin SimSync) es false -- bloqueaba siempre. Cada cosa vivia solo en el modo donde el otro la echaba de menos. Fix: spawns en TrySpawnRed + gate correcto (EnEscena && !EsServidor). Rack, alambique y pilas entran al registro de red con replicas y cerrojo; las replicas ganan nombres reales ('balda', 'el anclaje', 'el estante de redomas', 'el alambique', 'la pila') en vez de 'aparato'." -m "GRIFOS Y PILAS POR SEPARADO (pedido de Cesar): mover un grifo ya no arrastra su pila -- el marco era HIJO del transform del grifo; ahora las pilas son objetos propios (Game/Pila.cs, patron plataforma-soberana) movibles con V, y la guia de mudanza contiene los bounds REALES (el voladizo del cano incluido -- bug dormido en TamanoMundo). EL ESTANTE iba a aparecer FLOTANDO SOBRE LAS PILAS: sus constantes seguian apuntando a la posicion vieja de las fuentes (regla 47 de manual); ahora a la izquierda en alto sobre el crisol, encima del domo del alambique (derivado de constantes reales), como sugirio la captura de Cesar. F9 cierra DEL TODO el panel de sesion (quedaba una ventanita sobre el panel del FRASCO; ahora recordatorio de 3 segundos y silencio). Verificado en un jugador: 17 baldas + 6 anclajes + 2 pilas + estante + alambique en jerarquia, 0 errores. Documentacion en HANDOFF."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
