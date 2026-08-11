@echo off
cd /d "%~dp0"
echo === add === > commit_log.txt
git add -A >> commit_log.txt 2>&1
echo === commit === >> commit_log.txt
git commit -m "Fixes del playtest 1: fuego vivo y frio que se apaga" -m "- El hielo ya no inyecta frio a vecinos (la zona fria era autosostenida y 'la piedra seguia enfriando apagada')" -m "- Retorno a temperatura ambiente 4x mas rapido" -m "- Fuego: ignicion por contacto 50%, vida 80 ticks, llama sostenida mientras toque combustible" -m "- Nutrient y Vivium inflamables: ahora hay cosas que prender (y cultivos que proteger)" -m "- HANDOFF.md actualizado con los hallazgos del playtest" >> commit_log.txt 2>&1
echo === push === >> commit_log.txt
git push origin main >> commit_log.txt 2>&1
git log --oneline -2 >> commit_log.txt 2>&1
echo LISTO >> commit_log.txt
del %~f0
