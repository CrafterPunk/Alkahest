@echo off
setlocal
title ChaosAlchemy - commit PLAYTEST 21 (el pivot: el laboratorio vivo)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"

echo === limpiando lock si lo hay ===
if exist ".git\index.lock" del /f /q ".git\index.lock"

echo === borrando los zips del despliegue (no deben entrar en el commit) ===
if exist "_to_delete_pivot.zip" del /f /q "_to_delete_pivot.zip"
if exist "_to_delete_pv2.zip" del /f /q "_to_delete_pv2.zip"
if exist "_pivot.zip" del /f /q "_pivot.zip"
if exist "_pv2.zip" del /f /q "_pv2.zip"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 21: EL PIVOT - el laboratorio vivo" -m "PIVOT CONTROLADO pedido por Cesar: en vez de arrancar en un laboratorio tecnico lleno de instrumentos, el juego empieza pequeno, intimo y calido, conviviendo con una criatura viva. Su criterio de exito, literal: 'no solo estoy mezclando materiales, estoy conviviendo con algo vivo, cuidandolo, probandolo, y descubriendo lentamente que clase de laboratorio extrano estoy construyendo'" -m "LA IDEA QUE LO ATA TODO: el taller grande NO desaparece, esta ENTERRADO. Se arranca en una camara excavada de 110x42 celdas y el resto del mundo 768x288 queda relleno de piedra. Con eso el inicio es pequeno y limpio (no hay nada que ensenar porque no hay nada excavado), el mapa grande se conserva entero, y EL CINCEL pasa de curiosidad a motor del juego. El desbloqueo de areas por niveles ya no necesita ningun sistema: es piedra y un cincel" -m "LA CRIATURA (el Rescoldo, Game/Criatura.cs) no es una maquina con piel de bicho: SU CARNE ES VIVIUM DE VERDAD. Su cuerpo son celdas reales que crecen con el sistema dendritico existente, su silueta la decide el habito sorteado por semilla, su piel sale de la firma visual del universo, y su malestar ya estaba simulado (fuera de banda se DUERME y el juego la desatura; a 120C hierve y a 150C arde). Puedes matarla, y por eso cuidarla significa algo. Cuatro estados y ningun numero en pantalla: latido, color y zarcillos que se orientan hacia ti cuando te acercas" -m "SE CALIENTA A SI MISMA, y eso resolvio el problema mas grave de la ronda: con el ambiente uniforme a 20C, solo el 39 por ciento de las semillas tienen la banda de crecimiento abierta, asi que en 6 de cada 10 partidas la criatura no habria crecido NUNCA. Rescoldo significa brasa: empuja calor a su celda y alrededor, dentro de la banda de la semilla y con techo muy por debajo de donde hierve. Funciona en el 100% de las semillas sin reintroducir clima por zona (regla 31)" -m "Y DE AHI SALE EL BUCLE CENTRAL, encajando dos piezas que ya existian: alimentas a la criatura, se pone contenta, CALIENTA MAS Y MAS LEJOS, y el capullo cercano avanza. Cuidar produce vida. No esta cableado: el capullo lee la temperatura de SU celda y el calor llega por difusion, asi que funcionara igual con cualquier otra fuente" -m "EL CAPULLO (Game/Capullo.cs): 5 fases de grieta como unico indicador (nada de barras), avanza solo con calor sostenido, y ECLOSIONA en una cria que usa la misma clase" -m "LA DIGESTION usa la quimica generada del playtest 18 en tres escalones: primero busca una ley de ESTA semilla cuyo reactivo sea lo que le diste, si no cae a AfinidadDelUniverso, si no a una heuristica por Edicto. La criatura es un atajo vivo a las leyes del mundo" -m "BUG ENCONTRADO MIRANDO EL JUEGO CORRER: una masa amarilla enorme dominaba la pantalla. NO era el crecimiento del cuerpo (techo de ~29 celdas, descartado con numeros). Era la digestion realimentandose sola: el producto se pintaba fuera del radio de sondeo, pero es materia SIMULADA, y cuando sale Acid (liquido, amarillo-verdoso) la fisica lo esparce, vuelve a entrar en el radio y rearma el ciclo cada 4.9s para siempre. Arreglado como diseno: la digestion es un ACTO y no un motor, nunca digiere su propio producto, y el cuerpo tiene TALLA (14-40 celdas)" -m "TAMBIEN: sesion SIN RELOJ; apertura silenciosa (fuera el panel de 'Jornada 1 de 3' que hablaba de entregar en una Tolva sepultada tras 23 celdas de roca); LOS ENCARGOS NO EXISTEN hasta que cavas hasta la Tolva, porque el Maestro no puede pedirte nada mientras no haya un agujero por donde hablarte; HudSilenciado para que la primera pantalla este limpia; encuadre de 45 celdas de alto en vez de 100 para que la sala llene la pantalla; composicion en DIORAMA leyendose de izquierda a derecha; y fondo oscuro y liso para que la unica luz calida sea la criatura" -m "PRIMERA RONDA DEL PROYECTO COMPILADA Y VERIFICADA POR MI en el Unity real via MCP (refresco de assets, compilacion completa, consola: cero errores y cero warnings, tambien en runtime), y primera vez que he podido ver el juego corriendo y corregir a partir de lo que vi con mis ojos en vez de a ciegas" -m "AVISO: el sandbox de la nube se reinicio a mitad de esta ronda y revirtio el repo CINCO playtests sin avisar (regla 6b). Se detecto porque un agente reporto que una API del playtest 18 no existia. Recuperado desde GitHub conservando lo nuevo. Sin ese aviso se habrian desplegado copias obsoletas encima del trabajo bueno, que es exactamente la regresion de la regla 26" -m "Plan de diseno completo con los 10 puntos que pidio Cesar: docs/PIVOT_LABORATORIO_VIVO.md. Direccion, contrato de API congelado, integracion y verificacion visual: Opus 5. Codigo: Sonnet 5 en encargos con propiedad de archivos disjunta."

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
