@echo off
rem Doppelklick-Starter fuer Setup-AgentMonitor.ps1 (umgeht die Skript-Sperre nur fuer diesen Aufruf)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Setup-AgentMonitor.ps1" %*
echo.
pause
