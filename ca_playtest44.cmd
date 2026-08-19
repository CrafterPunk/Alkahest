@echo off
setlocal
title Limo Primordial - commit PLAYTEST 44 (LA FISICA HONESTA: placas realistas, termometro, conversion por frentes)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
if exist "_managed_dlls.zip" del /f /q "_managed_dlls.zip"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 44: LA FISICA HONESTA -- placas de calor/frio realistas, termometro en grados, conversion por frentes, particulas baratas fuera" -m "Ronda nocturna autonoma 1/2 (contrato docs/CONTRATO_TERMICA.md). KILL-SWITCHES pedidos por Cesar: capa de particulas decorativas APAGADA por defecto (codigo integro, toggle dev en F3) y patina de mojado APAGADA (prometia una filtracion que la sim no hace; el tizne se queda). TERMICA: fisica compartida de placas EmisionTermica (falloff + empuje por diferencia estilo Newton + collar que evita que el frio inunde el cuarto -- el reporte historico 'el frio irradiaba mucho y el calor poquito' muere aqui), HELANDO recalibrada -80 a -26C con ventaja emergente sobre FRESCA, AlkahestSim.InyectarTemperatura (disciplina Paint), CONVERSION POR FRENTES en el crisol (las celdas convierten al alcanzar su banda local: el tostado se VE propagarse de a pocos, ~66 ticks el frente en el peor caso; CerrarHornada queda de garantia). MEDIDO headless: hervir a 3 celdas de ARDIENTE en 6-8 ticks, congelar junto a HELANDO en 22-87, gradiente a 12 celdas menor a 2 grados. TERMOMETRO (tecla G): lectura viva en C junto al cursor + hasta 3 sondas pinchadas con etiqueta viva -- verificado con ojos: -26 en placa, -4 a dos celdas, 20 lejos. Placas en los tres modos con replicas (tipos 11/12) y E remoto; en SEMILLA CERO la de calor desde el beat 1 y beat nuevo del FRIO ('Y si lo ENFRIAS?' -> quinta sala + 'Traeme HIELO -- apurate, que el frio no espera a nadie', con linea de fracaso si se derrite). Compilado regla 53: 0 errores (1 warning CS0162 anotado)."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
