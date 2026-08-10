@echo off
cd /d "%~dp0"
echo === add === > commit_log.txt
git add -A >> commit_log.txt 2>&1
echo === commit === >> commit_log.txt
git commit -m "M1+M2: nucleo de simulacion celular + capa de interaccion" -m "- Autómata: polvos, líquidos con densidad, gases, fuego, temperatura y fases, chunks dormidos (0.3-0.6 ms/tick)" -m "- Fix fuego: extinción solo sumergido; llama pegada al combustible" -m "- Aprendiz volador, frasco aspirar/verter, placas de calor/frío, grifos, HUD" -m "- DECISIONS.md (las 20 decisiones de la Fase 1) y SIM_NOTES.md" >> commit_log.txt 2>&1
echo === push === >> commit_log.txt
git push origin main >> commit_log.txt 2>&1
echo === estado === >> commit_log.txt
git log --oneline -3 >> commit_log.txt 2>&1
echo LISTO >> commit_log.txt
del %~f0
