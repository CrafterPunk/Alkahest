@echo off
REM ca_playtest71b.cmd -- solo si ya corriste ca_playtest71.cmd antes de esta pasada: velocidad blindada contra el prefab (fuera SerializeField), parallax 8%, builds en ventana 1600x900 redimensionable.
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
git add -A
git commit -m "Ronda 71b: fuera [SerializeField] de moveSpeed/acceleration (el prefab de red serializaba 11.2/44 y pisaba el 6.7 del codigo en MULTI), parallax 3%%->8%% (el 3%% era imperceptible contra ladrillo uniforme), y las builds arrancan en ventana 1600x900 redimensionable con Alt+Enter para fullscreen (fijado en los build scripts)"
git push origin main
pause
