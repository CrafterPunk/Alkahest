@echo off
setlocal
title ChaosAlchemy - commit PLAYTEST 31 (maquinas en red + la alquimia visible) [SUBIR PRIMERO]
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add (tramo 31: red + alquimia; los archivos compartidos con el tramo 32 viajan aqui) ===
git add Assets/Alkahest/Net Assets/Alkahest/Game/Mudanza.cs Assets/Alkahest/Editor/AlkahestNetSceneBuilder.cs Assets/Alkahest/Game/Alambique.cs Assets/Alkahest/Game/Alambique.cs.meta Assets/Alkahest/Game/Crisol.cs Assets/Alkahest/Sim/SimStepper.cs Assets/Alkahest/Sim/Universe.cs Assets/Alkahest/Sim/SimLevelBuilder.cs Assets/Alkahest/Game/AlkahestGameBootstrap.cs Assets/Alkahest/Game/MaquinariaSprites.cs Assets/Alkahest/Game/StorageRack.cs Assets/Alkahest/Scenes 2>nul

echo === commit ===
git commit -m "Playtest 31: maquinas en red + LA ALQUIMIA VISIBLE (vispera de la demo, tramo 1 de 2)" -m "MAQUINAS EN RED (sim solo-host intacta): MaquinaSync publica el registro de las 7 estaciones (NetworkList, 22 bytes/entrada); los invitados ven REPLICAS visuales con chapa y pueden MUDARLAS -- V manda SolicitarMudanzaRpc, el host valida con CabeEnAncla y ejecuta, el registro replica a todos; rechazo con vuelta a sitio. LA ALQUIMIA VISIBLE: FUEGO REAL en el brasero (llamas de verdad pintadas sobre el combustible mientras arde la hornada, contenidas por la mamposteria; se extinguen solas al agotarse); EVAPORACION visible (celdas de Steam sobre la cubeta al extraer/evaporar); EL ALAMBIQUE -- primer instrumento FABRICADO: nace como obra pendiente ('construible: 30 celdas de ceramico'), viertes el material mas dificil del reticulo y E lo construye; su domo frio condensa el vapor que entra (fisica existente condensesAt) y gotea al matraz -- atrapa el vapor, destila; DISOLUCION con chispazo (morph 255 + firma Motas/Difuso de la Solucion); y el ESTANTE DE REDOMAS de vuelta (acomodar tus muestras, definir estrategia). Fix definitivo del NRE intermitente de la build (NetworkConfig nace null en editor: se crea explicito). NOTA: este commit comparte archivos con el tramo 32 (identidad); compilable garantizado tras subir AMBOS en orden."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO - AHORA EJECUTA ca_playtest32.cmd
pause
