@echo off
setlocal
title ChaosAlchemy - commit PLAYTEST 20 (las 5 familias de patron que nunca funcionaron)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"

echo === limpiando lock si lo hay ===
if exist ".git\index.lock" del /f /q ".git\index.lock"

echo === borrando el zip del despliegue (no debe entrar en el commit) ===
if exist "_to_delete_pt20.zip" del /f /q "_to_delete_pt20.zip"
if exist "_pt20.zip" del /f /q "_pt20.zip"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 20: las cinco familias de patron que nunca funcionaron" -m "NOTA DE NOMENCLATURA: a partir de aqui el script lleva el numero del PLAYTEST que commitea. Antes iban desfasados (ca_commit15=playtest 14, ca_commit16=playtests 15-17, ca_commit17=playtest 18, ca_commit18=playtest 19) y eso hizo que Cesar dudara de si el commit 19 existia. Existia: es d1560f6, y esta en GitHub" -m "CESAR PROBO LA BUILD DEL 19: movio grifos y placas, vio el taller compacto, y dijo 'no encontre cambios en los niveles ni en la morfologia de las formas'. Tenia razon en lo segundo, y la culpa es de un error de reparto de archivos del director" -m "EL ERROR DE PLANIFICACION: se encargo bajar la escala de los patrones con SimRenderer.cs en un encargo y SimStepper.cs en otro. Pero la escala vive en LOS DOS: Vetas y Celdas son posicionales y las calcula el renderer, mientras que Manchas, Laberinto, Dendritas, Pulso y Motas salen de MorphTick. Cambiaron 2 de 8 familias; como la firma visual se sortea por semilla, lo mas probable es que sus materiales cayeran en las otras seis y no viera absolutamente nada. Ningun agente se equivoco: la particion no admitia hacer el trabajo completo y ninguno podia verlo desde su lado" -m "LO QUE APAREGIO AL IR A ARREGLARLO, que es mucho peor: CINCO FAMILIAS NUNCA FUNCIONARON COMO EL CODIGO DECIA. El campo morph es de UN SOLO valor por celda, y una reaccion-difusion biestable de un solo campo NO produce patrones de Turing: se homogeneiza siempre. Manchas y Laberinto colapsaban a un tinte casi plano en un charco acotado hiciera lo que hiciera patronEscala. Ni puntos, ni bandas, ni diferencia entre las dos. El comentario de SimRenderer que afirmaba lo contrario llevaba mintiendo desde el playtest 12" -m "Dendritas acababa cubriendo el charco entero por percolacion (se veia como un borron, no como agujas). Pulso NUNCA usaba patronEscala: multiplicador espacial fijo, periodo de ~51 celdas, mas grande que cualquier charco pequeno; era la unica de las ocho cuya escala no hacia nada en absoluto. Motas disparaba tan poco que era invisible mas del 90 por ciento del tiempo. O sea que la queja del playtest 19 ('necesito mucho material para ver las formas') no era de calibracion: cinco de las ocho familias estaban rotas desde que se introdujeron" -m "ARREGLADO: Manchas/Laberinto con diffDiv fuera de la zona inestable mas un ANCLAJE DE RUIDO ESTATICO por bloque, calculado con XorShift.FromCell(0u,...) con TICK CONSTANTE 0 y no _tick (si usara el tick el mapa cambiaria cada frame y el patron parpadearia). Verificado: 0 celdas cambian entre el turno 1500 y el 3000, hay estructura y es estable. Dendritas con semillas mas frecuentes mas un mapa estatico de origenes elegibles para que no lo cubra todo. Pulso reutiliza el periodo ya calibrado de Vetas/Celdas. Motas mucho mas frecuente" -m "LO QUE NO SE ARREGLO Y HAY QUE DECIRLO: Manchas y Laberinto siguen siendo gemelas. Ya no colapsan, pero se diferencian por brillo medio y no por forma. La distincion puntos-vs-bandas que el diseno prometia NUNCA fue mecanicamente posible con un solo campo. El arreglo de verdad es un Gray-Scott real de DOS campos, que exige tocar CellGrid: va al backlog como cambio estructural, no como afinado" -m "LOS ENCARGOS SI HABIAN BAJADO: la jornada 1 pide 32-40 y 42-54 celdas en vez de 60 y 80, y varia con la semilla. Estaba desplegado y verificado. Lo que fallo fue no darle los numeros exactos que tenia que ver en pantalla: un cambio que el jugador no puede distinguir de 'no paso nada' es, para el, un cambio que no ocurrio" -m "Y el crecimiento dendritico del vivium tampoco pudo verlo, pero no es un fallo: el vivium solo aparece en la JORNADA 2. Atajo para probarlo sin jugar la progresion: F3 abre la paleta de desarrollo, pintar vivium y nutriente en una cuba y encender la placa en banda" -m "REGLAS NUEVAS (CLAUDE.md 41-43): 41) una mecanica puede vivir en dos archivos y repartir mal la propiedad es un fallo del director, no de los agentes. 42) morph es un solo campo y eso limita lo que puede dibujar. 43) un cambio que el jugador no puede distinguir de 'no paso nada' es, para el, un cambio que no ocurrio" -m "Direccion, diagnostico y auditoria: Opus 5. Codigo: Sonnet 5. Verificado con replica en Python de las cinco funciones (hashes reales) y auditoria independiente de determinismo, doble bufer, allocs y rangos. Sin compilador de C# en el sandbox: la compilacion real esta pendiente del editor."

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
