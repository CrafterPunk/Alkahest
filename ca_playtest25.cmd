@echo off
setlocal
title ChaosAlchemy - commit PLAYTEST 25 (LO QUE PERSISTE)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"

echo === limpiando lock si lo hay ===
if exist ".git\index.lock" del /f /q ".git\index.lock"

echo === retirando MareaDirector del indice y borrando restos del despliegue ===
if exist "_to_delete_marea" rmdir /s /q "_to_delete_marea"
if exist "Assets\Alkahest\Game\MareaDirector.cs" del /f /q "Assets\Alkahest\Game\MareaDirector.cs"
if exist "Assets\Alkahest\Game\MareaDirector.cs.meta" del /f /q "Assets\Alkahest\Game\MareaDirector.cs.meta"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 25: LO QUE PERSISTE - el eje cambia de fabricar a descubrir que dura; LA MAREA retirada" -m "LA DIRECCION DE CESAR, integrada: el juego deja de ser 'aprende como fabricar' y pasa a ser 'descubre QUE PERSISTE ante calor, frio, presion, agua y chispa'. Toda propiedad es observable y manipulable; el orden de las operaciones importa; toda semilla garantiza al menos una solucion persistente; los procesos se patentan; los pedidos piden propiedades, no objetos. Diseno completo en docs/DISENO_LO_QUE_PERSISTE.md (los 8 puntos de Cesar respondidos), contrato en docs/CONTRATO_PERSISTE.md. LA MAREA (playtest 24) queda RETIRADA del codigo entera por decision de Cesar: revert quirurgico a playtest 23 + MareaDirector borrado; los docs se quedan como archivo de la decision." -m "EL RETICULO DE ESTADOS (el corazon): 5 materias base por semilla x 8 estados (Polvo/Fundido/Templado/Recocido/Compacto/Ceramico/Calcinado/Solucion), cada estado un MaterialId propio (18..57, Count 17->58), generados por tabla. EL HISTORIAL VIVE EN EL ESTADO: materiales markovianos, grafo no conmutativo -- fundir y prensar escupe liquido (nada); prensar y hornear da ceramico (el techo). Enfriar RAPIDO en el mundo = Templado (frajil a la prensa); enfriar LENTO dentro del crisol = Recocido (ductil). Dos ejes de legibilidad: la base se reconoce por el tono, el estado por el tratamiento fijo entre universos." -m "EL LIMO PRIMIGENIO: el cano ex-nutriente gotea una suspension turbia de la que desciende TODA la materia base del universo (la criatura queda APARCADA, no borrada: spawns comentados, archivos intactos -- volvera como organismo-solucion). Calentarlo lo separa: cada celda precipita el polvo de una base por sorteo determinista con pesos por seed. El primer gesto del juego ya es el juego." -m "LAS MAQUINAS: CRISOL con rescoldo propio (raw 120: hierve todo lo acuoso en toda seed) y temperatura maxima decidida por el COMBUSTIBLE cargado (la progresion termica es descubrimiento, no un dial) -- calcina en banda sostenida, ceramiza el compacto, recuece lo fundido que muere dentro. PRENSA fisica (compacta/revienta/ESCUPE liquidos). BANCO DE CHISPA: la lampara delata la conductividad, LA propiedad invisible. Columna de cristal para estratificar/disolver/flotar. ENSAYO DEL MAESTRO junto a la Tolva: el pedido de calor se ensaya A LA VISTA con estrellas por margen real (el espectro de soluciones, desde el pedido 2; el fallo dice COMO murio la muestra)." -m "PEDIDOS = ARCO FIJO de 5 de uno en uno (el arco ES el tutorial): separar limo -> aguantar el rojo -> encender la lampara -> flotar sin disolverse -> el PROCEDIMIENTO por escrito (paga doble). HORNADA (ring de 8 ops) + PATENTES v0: el primer (base,estado) jamas producido congela una patente en la seccion PROCEDIMIENTOS del diario, bautizable. SOLVER DE GARANTIA en Universe.Create: BFS con la escalera termica hervir->calcinar->combustible->fundir, 3 garantias verificadas en toda seed con log ('Persistencia: ganador=N a K pasos') -- los pedidos imposibles son estructuralmente imposibles." -m "INTEGRACION (Fable sobre 3 encargos Sonnet paralelos, 15 archivos, ~1500 lineas): reglas 50 y 51 aplicadas -- tier0 118->120 y separacion del limo 150->112 (dos numeros mios del contrato mal calibrados; el agente A senalo la inconsistencia en vez de obedecerla), y el GANADOR FUNDIDO cazado en el primer arranque leyendo el log del solver (la garantia debe cuantificar sobre estados ENTREGABLES, regla 51 nueva). Compilado 0 errores / 0 warnings a la primera en el Unity real via MCP; 40s de play sin excepcion; solver verificado en dos seeds reales. Documentacion: HANDOFF seccion Playtest 25, CLAUDE.md reglas 49-51 y estado nuevo."

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
