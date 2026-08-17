@echo off
setlocal
title ChaosAlchemy - commit PLAYTEST 32 (IDENTIDAD: tipografia, bautizo, luz) [SUBIR SEGUNDO]
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"

echo === add (todo lo restante: identidad + fuentes + docs) ===
git add -A

echo === commit ===
git commit -m "Playtest 32: IDENTIDAD - tipografia con alma, el bautizo como rito, luz de fragua (vispera, tramo 2 de 2)" -m "OPUS 5 COMO DIRECTOR VISUAL CON OJOS PROPIOS (6 iteraciones desplegar->compilar->jugar->capturar->corregir en el Unity real). TIPOGRAFIA: Cinzel (titulos, lapidaria) + Alegreya (cuerpo, humanista), OFL, cargadas de Resources con fallback; GUI.skin.font hace que TODA la UI la herede; skin entero vestido: carboncillo con filo de laton, caret de oro -- adios menu de Windows XP. EL BAUTIZO ES UN RITO: vitela con marco de laton y cantoneras, B A U T I Z O en Cinzel, la muestra del material EN GRANDE con su firma visual real animada, linea ceremonial, Enter bautiza (probado en vivo: 'flor de niebla', 'arena solar'). ILUMINACION DE ANIMO: tinte calido global, halos de luz anclados a fuentes REALES (rescoldo, brasero ardiendo, lampara del banco solo si conduce, vidrio, redomas), sombras propias bajo cada estacion, y LA PARED: silleria con junta, biseles, patina, zocalo, cornisa, hornacinas y el rebote de la fragua -- el taller por fin es un SITIO. ROMPER LA LINEA: terrazas talladas entre estaciones (descubiertas preguntando al registro de obra, sin mover un ancla), pilastras con mensula, arco de medio punto sobre el pasillo a la Tolva. Builds de la vispera: Builds/ChaosAlchemyMulti (final) y Builds/Respaldo_TramoA_Multi (respaldo del tramo 1). Veredicto honesto del director en HANDOFF; proxima deuda elegida: PARTICULAS."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -3
echo LISTO - LA DEMO ESTA SERVIDA
pause
