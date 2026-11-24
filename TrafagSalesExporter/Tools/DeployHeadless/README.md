# DeployHeadless: Sicherungskopie des Deploy-Werkzeugs

Stand: 2026-11-24 mittags. Seit `95c5ad5` sichert die App die Datenbank selbst (samstags 03:00, Dateien `trafag_exporter.db.before-appsicherung-*.bak`); das Werkzeug erkennt sie am Namen und macht dann keine Wochensicherung mehr, Code unveraendert.

Auftrag Ingo: Das Deploy-Werkzeug soll auf GitHub liegen, damit es bei einem Laptop-Ausfall nicht verloren geht.

**Aufgerufen wird weiterhin `.tmp_tools/DeployHeadless`** (siehe `CLAUDE.md` Abschnitt 5 und `docs/rag/DEPLOYMENT.md`).
Dieser Ordner ist nur eine Kopie von `Program.cs` und `DeployHeadless.csproj`, weil `.tmp_tools/` in `.gitignore` steht.
Wer das Werkzeug dort aendert, kopiert beide Dateien hierher und committet sie.

Wiederherstellen nach einem Ausfall:

1. `Program.cs` und `DeployHeadless.csproj` nach `.tmp_tools/DeployHeadless/` kopieren.
2. Die `.csproj` verweist relativ auf `..\..\Tools\DeployConsole\DeployConsole.csproj` (versioniert) und auf
   `Services/PurchasingRunLog.cs` im Release-Worktree (`PurchasingRunLogFile`, fester Pfad unter `C:\TMP\...`).
   Ebenso steht der Release-Worktree als `ReleaseRoot` fest in `Program.cs`. Beide Pfade an den neuen Release-Worktree anpassen.
3. Aufruf wie in `CLAUDE.md` Abschnitt 5.

Gepruefte Inhalte: keine Passwoerter oder Tokens. `AccessPasswords` und `admin/passwoerter` sind nur Pruefmarken, die das
Werkzeug im veroeffentlichten Stand sucht. Das Ziel `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$` ist fest eingetragen.
