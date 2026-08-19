@echo off
setlocal
title Limo Primordial - commit PLAYTEST 40 (SEMILLA CERO: la primera sesion como experiencia de autor)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_managed_dlls.zip" del /f /q "_managed_dlls.zip"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 40: SEMILLA CERO -- el arco de autor completo (milagro, nombre ganado, fracaso forense, cuatro preguntas, final abierto)" -m "EL ARCO (contrato docs/CONTRATO_SEMILLA.md, fuente unica del beat a beat; Game/SemillaCero.cs director): milagro con nombre provisional ('sedimento celeste', estado+color, GLOBAL: mejor que Base2Polvo en cualquier universo) -> 'Traeme 25 de ese... sedimento celeste tuyo' -> 'No pienso seguir diciendolo: ponle nombre' (bautizo como peticion del personaje) -> el tostado con TRAMPA (banda de calcinacion estrecha; el brasero tier1 se pasa -> ceniza + nota forense 'cerca de ~N se destruye' + la ceniza es combustible tier 0.5: fracasar alimenta el reintento) -> cuatro preguntas que destapan salas (MAS DURO/prensa, queda ENCIMA/columna, CONDUCE/chispa, DE VERDAD aguanta/ensayo) -> final abierto ('No necesito nada mas por hoy. ...Pero queda limo.') con el vasito del alambique lleno desde el minuto 0 sin que nadie lo mencione, y el CONTADOR DE AUTONOMIA (la metrica reina, log + F3). Pantalla de entrada: SEMILLA CERO -- tu primer taller / MODO CAOTICO con seed. El caotico y el multi NO cambian." -m "EL MUNDO: seed de autor 777002 congelada tras MEDIRLA con diagnosticos headless -- b0 calcinado conduce pleno (la respuesta ya viene cargada cuando llega la pregunta), b1 polvo flota insoluble (la columna obliga a descubrir la SEGUNDA arena, banda 122), TempEnsayo 177 mata al calcinado de b0 (umbral 170, el circulo forense se cierra) y el de b1 (188) pasa solo con fuego medido de ceniza porque el tier1 FUNDE b1: la leccion del fracaso reaparece sola al final. Tapiados de obra como puertas condenadas (API DestaparSala 0..3, maquinas sin spawnear hasta el destape: cero chapas a traves del muro). AUDITORIA DE INTEGRACION: interbloqueo duro de CONDUCE resuelto (la lampara del banco completa el pedido; antes solo el Ensayo, tapiado, podia) y el testigo forense reconectado (la trampa es de HORNADA, no de la CA: puente RegistrarDestruccionPorHornada). Compilado con el rig fiel a Unity (regla 53): 0 errores."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
