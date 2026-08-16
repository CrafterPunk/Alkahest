@echo off
setlocal
title ChaosAlchemy - commit PLAYTEST 26 (el taller que se explica solo)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"

echo === limpiando lock si lo hay ===
if exist ".git\index.lock" del /f /q ".git\index.lock"

echo === borrando restos del despliegue ===
if exist "_to_delete_pt26" rmdir /s /q "_to_delete_pt26"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 26: el taller que se explica solo - gramatica visual, affordance glow, la linea, la racion" -m "EL FEEDBACK DE CESAR (playtest 25): las maquinas no comunicaban donde entra la materia, donde queda el resultado ni que acepta cada una ('no queda claro donde van las cosas... si deberia meter limo en todas'); mejorar SIN CARTELES, para el publico de a pie; y los consejos aturdian (rapidos, no releibles, sin poder saltar). Contrato docs/CONTRATO_LEGIBILIDAD.md, dos encargos Sonnet (M maquinas+plano, H consejos+diario) + fixes de integracion y VERIFICACION EN VIVO de Fable jugando la build con capturas en el PC de Cesar." -m "LA GRAMATICA VISUAL (vale para toda maquina futura): EMBUDO de laton = entrada de materia; BRASERO de hierro con rescoldo = entrada de combustible (la unica otra boca); CUBETA ENMARCADA = aqui queda el resultado; el VERBO EN EL CUERPO (chimenea que humea solo al quemar, mandibulas+husillo, electrodos+arco+lampara, pedestal ceremonial del Ensayo). Y el AFFORDANCE GLOW: con el frasco cargado, la boca que ACEPTA ese material LATE suave al acercarte -- la duda de Cesar la contesta el taller senalando, sin un cartel. Verificado en vivo: el embudo del crisol latiendo con limo en el frasco, prensa/chispa/ensayo apagados." -m "LA LINEA DEL TALLER: el cuarto crece a la izquierda (ancho 110->126) y las estaciones se ordenan como el proceso: fuentes (cada cano con su PILA) -> crisol -> prensa -> columna de vidrio -> banco de chispa -> ensayo -> tolva. Crudo->transformar->forzar->observar->revelar->examinar->entregar: la geografia ES el tutorial. Toda la mamposteria centralizada en SimLevelBuilder via TallarEnPlano estaticos (regla 47). El cano de limo gano voladizo propio (12 vs 5): sin el, ambos chorros caian por la misma columna." -m "LO QUE SOLO SE VIO JUGANDO (regla 52 nueva): 20 segundos de grifo abierto sobre el suelo corrido inundaban el laboratorio -> LA RACION (los canos del laboratorio sirven ~45 celdas por apertura y se cierran solos, chapa 'servido -- E para mas'); y el limo pardo se camuflaba con la piedra a escala de juego -> verde oliva turbio. Ninguna de las dos cosas era visible en el codigo: se cazaron con capturas jugando la build real." -m "CONSEJOS: 12s por consejo, N = siguiente (sin destripar en el diario lo saltado), H = ocultar, contador 'consejo 3/10', seccion CONSEJOS releible en el diario (el hook PistasMostradas del playtest 10 por fin con consumidor), placa callada con el libro abierto, y la N de DevPalette ahora exige paleta abierta (conflicto cazado por el encargo H). Compilado 0 errores / 0 warnings; diario de 4 pestanas verificado en pantalla. Documentacion en HANDOFF seccion Playtest 26 y regla 52 de CLAUDE.md."

echo === push ===
git push origin main

echo.
echo === COMPROBACION DEL PUSH (mira esto antes de cerrar) ===
git status -sb | head -1
git log --oneline -3
echo.
echo ============================================
echo  LISTO
echo ============================================
pause
