@echo off
REM ca_playtest69.cmd -- REUNE TODO LO NO SUBIDO desde el 68 (git add -A barre las cinco pasadas de la ronda 69: a) obra fantasma + sandwich Crisol, b) colider vertical con bob, c) juice motas/respiracion/pitch, d) anticipacion y cierre + sorting, e) haz retractil + notoriedad +25%). Correr SOLO este, no hace falta orden.
cd /d C:\JuegosUnity\UnityAI_Test\Alkahest
git add -A
git commit -m "Ronda 69 completa (a-e): fix 'es obra del taller' fantasma (ReservasDelPlano; terrazas/pilastras tallables), colision AABB asimetrica al tamano real del imp con bob incluido, sandwich MachineBack-Sim-MachineFront piloto en el Crisol, y el juice de aspirar/verter completo: motas en transito con anticipacion-accion-resolucion, frasco que respira e inclina, sorting de profundidad, sonido con pitch de llenado, avisos limitados a 2 episodios y haz que se retrae al agotarse el flujo; verificado en runtime y con capturas; el juice vive en MULTI por construccion (mismo Flask.Init via AprendizNet.Cablear)"
git push origin main
pause
