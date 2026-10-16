@echo off
rem Oeffnet die P76-Lesepruefung (Shopfloor-Transporte) in einem sichtbaren Fenster; Passwort tippt Ingo selbst.
cd /d "%~dp0.."
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0P76Pruefung_Shopfloor_2026-10-16.ps1"
echo.
pause
