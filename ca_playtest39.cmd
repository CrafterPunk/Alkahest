@echo off
setlocal
title Limo Primordial - commit PLAYTEST 39 (la ronda de motor: fuego persistente, brasas, gases, patina, particulas)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_managed_dlls.zip" del /f /q "_managed_dlls.zip"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 39: la ronda de motor -- el fuego como proceso (combustion persistente + brasas), gases con corrientes, patina y la capa de particulas" -m "COMBUSTION PERSISTENTE (contrato docs/CONTRATO_MOTOR.md, encargo S): el combustible ES la celda que arde -- MaterialDef gana 7 parametros (reserva/ritmo/calor/humo/propagacion/lengua/residuo), el estado ardiendo vive en aux, el Fire de siempre pasa a ser LA LENGUA VISIBLE, el agua sigue mandando. Un charco de aceite encendido arde ~32s por celda consumiendose desde el borde (verificado tick a tick). Los calcinados combustibles de la seed se conectan por primera vez a la ignicion real. BRASA (MaterialId 58): la vejez del fuego -- rescoldo 8-12s que emite calor, reenciende inflamables, muere a Ash (agua la mata al instante); el brasero del Crisol ahora arde y abrasa DE VERDAD sin quitarle autoridad a los tiers. GASES: deriva termica hacia el calor + bolsas bajo boveda (como medio-decaimiento acotado: la version por-movimiento era matematicamente inmortal, regla 55 nueva). PATINA: CellGrid.patina escrita/leida SOLO por el renderer (tizne junto al fuego, mojado que se seca, bovedas tiznadas por humo) -- cero costo en el tick, funciona para invitados sin trafico. REACCIONES DIRIGIDAS: 1/2 en las cubetas de las maquinas, 1/8 en el mundo." -m "CAPA DE PARTICULAS (encargo F, Game/ParticulasFx.cs nuevo): decorativas no-sim, ring de 4096 preasignado, overlay 768x288 con texeles sucios, presupuesto de 64 nacimientos/frame -- salpicaduras, chispas, motas del crisol, polvo, vaho, ascuas de brasa; ganchos episodicos al ring de eventos no-destructivo nuevo (Ignite/Boil/Ember). BANCO con 6o escenario INCENDIO SOSTENIDO: delta de TODA la ronda +2-8%% por escenario (medido contra baseline git-stash en la misma sesion; el sandbox hoy corre 3,5x mas lento que la maquina del informe), headroom 1,5-4,8x aun ahi. Regla 55: todo proceso vivo debe demostrarse MORTAL (balance ganancia-decaimiento) y DESPIERTO (WakeChunk sobre si mismo). Compilado con el rig fiel a Unity (regla 53): 0 errores."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
