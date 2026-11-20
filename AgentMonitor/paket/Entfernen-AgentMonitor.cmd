@echo off
rem Doppelklick-Starter fuer Entfernen-AgentMonitor.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Entfernen-AgentMonitor.ps1" %*
echo.
pause
