@echo off
setlocal
title ChaosAlchemy - commit PLAYTEST 27 (el taller grande + espanol latino)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_to_delete_pt27" rmdir /s /q "_to_delete_pt27"

echo === add SOLO los archivos del playtest 27 (el multiplayer va en el commit 28) ===
git add Assets/Alkahest/Game/AlkahestGameBootstrap.cs Assets/Alkahest/Game/BancoChispa.cs Assets/Alkahest/Game/Cincel.cs Assets/Alkahest/Game/ColumnaEnsayo.cs Assets/Alkahest/Game/ColumnaEnsayo.cs.meta Assets/Alkahest/Game/Crisol.cs Assets/Alkahest/Game/DayCycle.cs Assets/Alkahest/Game/Dispenser.cs Assets/Alkahest/Game/EnsayoMaestro.cs Assets/Alkahest/Game/HintSystem.cs Assets/Alkahest/Game/MaquinariaSprites.cs Assets/Alkahest/Game/MasterSupplies.cs Assets/Alkahest/Game/OrderSystem.cs Assets/Alkahest/Game/Prensa.cs Assets/Alkahest/Game/SubstanceKnowledge.cs Assets/Alkahest/Sim/SimLevelBuilder.cs Assets/Alkahest/Sim/SimStepper.cs Assets/Alkahest/Sim/Universe.cs Assets/Alkahest/Sim/SimRenderer.cs docs/CONTRATO_TALLER_GRANDE.md docs/HANDOFF.md CLAUDE.md

echo === commit ===
git commit -m "Playtest 27: el taller grande - Opus 5 con ojos, crisol por hornadas, espanol latino" -m "EL VEREDICTO DE CESAR sobre el 26: maquinas cajita ilegibles, embudos falsos flotantes, 'cargadme combustible' sin sentido, la columna una escalera sin verbo con muros REACTIVOS (Crystal+Azoth, bug real), y el crisol escupiendo 4 colores por pasada ('si me salen 4 cosas casi de golpe no entendi nada'). Y el encargo de metodo: 'apoyate en Opus 5 permitiendo que EL VEA'." -m "OPUS 5 CON OJOS PROPIOS, 3 ciclos de desplegar/compilar/jugar/capturar/corregir en el PC real: cuarto ampliado a 218x73; estaciones 6-20x mas grandes (crisol 37x24 con cubeta de 117 celdas, prensa-portico con volante que gira, columna 23x42 con muros de PIEDRA inerte y vidrio visual y su verbo propio en Game/ColumnaEnsayo.cs nuevo, banco de chispa con ampolla de filamento, ensayo hecho ALTAR con dosel); embudo TALLADO en piedra solo donde se vierte de verdad, bandeja abierta donde se deposita; fuentes con machon que separa los dos chorros y pilas de 70 celdas." -m "EL CRISOL POR HORNADAS (cambio de causalidad, diseno cerrado con el feedback de Cesar): en reposo no empuja temperatura (la cascada es estructuralmente imposible); E enciende UNA hornada de ~10s con progreso visible; el resultado REPOSA hasta recogerlo; recoger-y-volver-a-pasar es EL gesto del juego. Extraccion del limo POR TEMPERATURA (Universe.ExtraccionRaw, 5 bandas por seed): una hornada saca UNA sola base -- con fuego bajo la mas docil, con mejores combustibles las demas (la intuicion de Cesar confirmada como diseno). Solver con garantias nuevas G1-G4; combustibles 165..190 raw; ProcessLimoSeparacion retirado (regla 15)." -m "FIXES DIRECTOS: el pulso de affordance por proximidad APAGADO (conservado para 'maquina trabajando', su destino aprobado); LA OBRA DEL TALLER NO CEDE AL CINCEL (SimLevelBuilder.ObraDelTaller + guarda en TallarTick -- Cesar se llevo mamposteria creyendo tallar roca); cano de limo sin estirar. ESPANOL LATINO en todos los textos (regla 53 nueva): fuera vosotros/os/vuestro/imperativos -ad. Pistas reescritas al modelo de hornadas (12 pasos ejecutables)." -m "Direccion e integracion: Fable 5. Maquinas y sim: Opus 5 (con verificacion visual propia). Barrido de textos: Sonnet. Documentacion en HANDOFF secciones Playtest 27-28 y reglas 52-53 de CLAUDE.md."

echo === push ===
git push origin main
echo.
echo === COMPROBACION DEL PUSH ===
git status -sb | head -1
git log --oneline -3
echo ============================================
echo  LISTO - ahora puedes correr ca_playtest28.cmd si ya validaste el multi
echo ============================================
pause
