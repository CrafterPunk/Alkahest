@echo off
setlocal
title Limo Primordial - commit PLAYTEST 36 (paridad multi profunda)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 36: paridad multi profunda - el invitado por fin juega el mismo juego" -m "REVISION PROFUNDA pedida por Cesar tras su primera prueba real con invitado ('muchas cosas no se sincronizan... y mil cosas'). CUATRO CAUSAS RAIZ: (1) replicas BLANCAS: el switch de visuales no tenia casos para estante/alambique/pila y caia al rectangulo generico sin tintar -- ahora piezas reales tintadas (liston+redomas, domo+matraz, U enmarcada). (2) El empapelado de 'la balda' x17: las 23 replicas dibujaban su chapa SIEMPRE a opacidad plena sin filtro de cercania -- ahora por cercania como las maquinas reales, y balda/anclaje sin chapa jamas (mobiliario: la forma es el rotulo). (3) INVITADO SIN MENUS: la rama invitado del bootstrap terminaba en un return mudo tras el avatar; y SubstanceKnowledge.Update se apagaba ENTERO con Stepper null (el gate era correcto solo para ConsumeEvents). Nuevo Net/SaberSync.cs autoadjunto (sin regenerar escena): descubrimientos, nombres bautizados, leyes presenciadas, encargos y Favor replicados a todos; quien entra tarde recibe TODO; el bautizo de invitado viaja por ServerRpc y el host (autoridad) lo eco a todos; OrdersHud gana rama read-only replicada; el invitado spawnea HintSystem/NamingUi/JournalHud/OrdersHud. (4) EL CHORRO INTERMITENTE: la ruta RPC->Paint->dirty estaba SANA (auditada); el bug real era INANICION en la difusion de chunks (barrido circular ciego, presupuesto 96): ahora dos pasadas con prioridad a chunks cerca de CUALQUIER avatar conectado." -m "PARA PROBAR: recompilar (abrir Unity basta), menu Alkahest 4 (Build MULTI), y la prueba de dos ventanas de siempre. Documentacion en HANDOFF seccion Playtest 36."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
