@echo off
setlocal
title Limo Primordial - commit PLAYTEST 56 (la noche entera: 7 bugs + LA VIDA UTIL DE LO DESCUBIERTO)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_managed_dlls.zip" del /f /q "_managed_dlls.zip"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtests 55+56: la noche entera -- los 7 bugs del co-op y LA VIDA UTIL DE LO DESCUBIERTO (vertical slice)" -m "PT55, LOS 7 BUGS: VOLVER AL TITULO recarga la escena activa (la build multi no empaqueta la clasica); el invitado SIN fichas era una carrera de arranque real (el catch-up corria antes de que existiera su AlbumReal y la idempotencia lo silenciaba para siempre -- ahora hay TEATRO PENDIENTE persistente que nadie pierde); el panel cortado era GUIClip corrupto por una excepcion dentro del BeginArea de la Pausa (try/finally + clamp); el haz del frasco lo mataba AlbumReal.Abierto atascada tras morir su instancia (OnDestroy la baja + SALIR de la sesion recarga la escena y de paso cierra la fuga de re-host del pt53); chapa del grifo compensada 4 celdas; marco de la Pila con proporcion real; album con tooltips en las 8 cabeceras y sin textos cortados; el ambar al quemar turba ES quimica real (destilacion destructiva: asi se hacia la brea). PT56, LA VIDA UTIL: el loop pasa a descubrir-producir-almacenar-utilizar-necesitar -- al final del arco EL MUNDO pide: LOS VITRALES DE LA CAPILLA (40 vidrio / 12 barbotina / 20 mortero, checklist de 3 lineas, 60 estrellas) y al completarse UNA MUFLA DE VIDRIERO (24 ceramica / 16 vidrio / 12 mortero) que TALLA un segundo crisol de verdad; LA ALACENA se revela con la fase (6 casillas etiquetadas con nombre real y nivel visible, cap 300). Y la regla 57 cazada EN VIVO antes de zarpar: el pigmento original (licor pardo) y la barbotina del recetario pt51 eran INALCANZABLES (sorteo de solubilidad) -- Override 6b: solubilidad POR DECRETO segun la tabla de identidades. Compilado regla 53: 0 errores. Diseno: docs/DISENO_VIDA_UTIL.md; contrato: docs/CONTRATO_RONDA56.md; detalle: HANDOFF 55 y 56."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
