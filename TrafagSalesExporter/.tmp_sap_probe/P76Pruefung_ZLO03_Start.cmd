@echo off
rem Oeffnet die P76-Lesepruefung ZLO03/B63485 in einem sichtbaren Fenster; Passwort tippt Ingo selbst.
cd /d "%~dp0.."
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0P76Pruefung_ZLO03_B63485.ps1"
echo.
pause
