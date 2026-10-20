@echo off
rem Oeffnet die P76-Lesepruefung Z_PACK_REGULATION (RoHS/REACH/PFAS, zweiter Lauf) in einem sichtbaren Fenster; Passwort tippt Ingo selbst.
cd /d "%~dp0.."
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0P76Pruefung_PackRegulation.ps1"
echo.
pause
