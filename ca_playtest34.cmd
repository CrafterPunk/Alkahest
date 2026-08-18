@echo off
setlocal
title Limo Primordial - commit PLAYTEST 34 (el rebautizo, los anclajes, el taller que respira)
cd /d "C:\JuegosUnity\UnityAI_Test\Alkahest"
if exist ".git\index.lock" del /f /q ".git\index.lock"
for /d %%D in (_to_delete_*) do rmdir /s /q "%%D"

echo === add ===
git add -A

echo === commit ===
git commit -m "Playtest 34: LIMO PRIMORDIAL - el rebautizo, el sistema de anclajes, el taller que respira" -m "EL JUEGO SE LLAMA LIMO PRIMORDIAL (decision de Cesar): titulo nuevo con 'Todo lo que existe desciende del limo.'. EL SISTEMA DE ANCLAJES (Cesar diseno sin querer la CONSTRUCCION del juego): cuadraditos de laton 2x2 movibles que SON piedra tallada -- los solidos con gravedad se apoyan gratis, sustituyen bedrock al colocarse (esquinas perfectas) y no dejan hueco al quitarse; baldas de 1 fila con el cuadrito en los extremos, movibles por separado; deposito de 6 anclajes de sobra junto al estante. Multi: CERROJO de mudanza (quien agarra bloquea, otros ven aviso y posicion final -- decision de costo de Cesar). Redomas y alambique moviles (el alambique solo se registraba al completar la construccion, nunca en obra pendiente). MUERTE DEL AUTO-PATENTE DE 1 PASO: el bautizo sale al frente; patentar exige 2+ pasos y todos los ingredientes bautizados; el aviso de procedimiento abre el diario EN procedimientos." -m "EL ESPACIO (Opus con ojos, 4 ciclos): cuarto 80..378 x 136..262 (+25% ancho, +34% alto hacia ABAJO para ganar aire de cabeza), la boveda FUERA del encuadre inicial -- el techo se DESCUBRE volando (verificado jugando). Plano central de Cesar: transformar a la izquierda (crisol 102, prensa 158), FUENTES AL CENTRO (isla de doble machon), observar a la derecha (columna 260, chispa 302), ensayo 362 casado con la Tolva. FONDO UNICO (el aparejo de la prensa que eligio Cesar, contraste a la mitad) cubriendo TODO el mundo con penumbra por profundidad -- romper bedrock ya no muestra negro; ventanas con marco propio que el trabado no cruza; cadenas colgando de vigas DEL FONDO (no de piedra excavable); capiteles que flotaban 25 celdas aterrizados; luz de claraboya retirada a peticion de Cesar (no estaba lista). Verificado: LIMO PRIMORDIAL en pantalla, techo oculto al entrar, 0 errores. Rumbo STEAM NEXT FEST octubre 2026: ~60% -- faltan Steamworks propio, pase de audio y 30 min sin tutor. Documentacion en HANDOFF seccion Playtest 34."

echo === push ===
git push origin main
git status -sb | head -1
git log --oneline -2
echo LISTO
pause
