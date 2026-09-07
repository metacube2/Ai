# Alphaplan Delta-Export — Korrektur & Einrichtung

Stand: 2026-06-24

## Was war das Problem?

Der tägliche Alphaplan-Export hat die SQL-Zugangsdaten **nicht selbstständig**
benutzt — man musste händisch das SA-Konto eingeben.

Ursachen:

1. **Hauptfehler:** Der Runner startete das Delta-Script in einem **neuen
   `powershell.exe`-Prozess** und versuchte, das Credential-Objekt als Parameter
   mitzugeben:
   `& powershell.exe -File ... -SqlCredential $cred`
   Ein `PSCredential` ist ein .NET-Objekt und lässt sich **nicht** über eine
   Prozessgrenze als Kommandozeilen-Argument übergeben — beim Delta-Script kam
   `$null` an. Folge: Rückfall auf `Get-Credential` (interaktive Abfrage), die im
   Task Scheduler fehlschlägt.
2. Pfade waren fest auf `C:\temp\...` verdrahtet.
3. Das ZIP hieß immer gleich (`AlphaplanDeltaExport.zip`) und überschrieb sich.

## Was wurde korrigiert?

In **`runAlphaplanDailyDelta.ps1`**:

- Das Delta-Script wird jetzt **im selben Prozess** aufgerufen
  (`& $DeltaScript -SqlCredential $cred ...`). Das Credential wird korrekt
  übergeben — keine erneute Abfrage mehr.
- Alle Pfade liegen relativ zum Script-Ordner (`$PSScriptRoot`) statt `C:\temp`.
- Das ZIP bekommt ein Datum im Namen:
  `AlphaplanDeltaExport_yyyyMMdd.zip` → überschreibt nichts mehr, lokal und im
  SharePoint-Zielordner.
- `-NoZip` an das Delta-Script: es baut kein eigenes (doppeltes) ZIP mehr,
  nur noch der Runner erzeugt das datierte Archiv.

## Welche Dateien musst du auf den DE-Server schicken?

| Datei | Aktion |
|---|---|
| **`runAlphaplanDailyDelta.ps1`** | **NEU/korrigiert — ersetzen** |
| `alphaplandeltaexport.ps1` | muss im selben Ordner liegen (unverändert; mitschicken, falls dort älter/fehlend) |

**NICHT mitschicken:**

- `alphaplan-sql-cred.xml` — diese Datei ist an Rechner + Windows-User gebunden
  (DPAPI-verschlüsselt). Eine fremde XML funktioniert auf dem Server nicht und
  enthält Zugangsdaten. **Die wird direkt auf dem Server neu erstellt** (siehe
  unten).
- `AlphaplanDeltaExport.zip`, `Neues Textdokument.txt` — Arbeits-/Testreste,
  nicht nötig.

> Beide `.ps1`-Dateien müssen im **selben Ordner** liegen. Der Runner findet das
> Delta-Script automatisch dort (kein fester Pfad mehr nötig).

## Einrichtung auf dem DE-Server (einmalig)

Wichtig: Die folgenden Schritte **auf dem DE-Server** ausführen und **als genau
dem Windows-Benutzer**, unter dem der Task-Scheduler-Job läuft (Service-Konto).
Sonst kommt wieder der Fehler „Schlüssel ist im angegebenen Status ungültig".

1. Beide `.ps1`-Dateien in einen Ordner legen, z. B.
   `C:\AlphaplanExport\` (Ordner frei wählbar).

2. Credential-Datei **dort** und **als Task-User** neu erstellen:

   ```powershell
   cd C:\AlphaplanExport
   $cred = Get-Credential   # SQL-Login für Alphaplan / ApDaten eingeben
   $cred | Export-Clixml -Path ".\alphaplan-sql-cred.xml"
   ```

3. Prüfen, dass die XML lesbar ist (als derselbe User, kein Fehler erwartet):

   ```powershell
   whoami
   $c = Import-Clixml ".\alphaplan-sql-cred.xml"
   $c.UserName
   ```

4. Manueller Testlauf:

   ```powershell
   powershell.exe -ExecutionPolicy Bypass -File "C:\AlphaplanExport\runAlphaplanDailyDelta.ps1"
   ```

   Erwartung: läuft ohne Passwortabfrage durch, erzeugt
   `AlphaplanDeltaExport_<datum>.zip` und lädt es per rclone hoch.
   Das Run-Log liegt unter `C:\AlphaplanExport\logs\`.

## Task Scheduler

- **Programm:** `powershell.exe`
- **Argumente:** `-ExecutionPolicy Bypass -File "C:\AlphaplanExport\runAlphaplanDailyDelta.ps1"`
- **Ausführen als:** derselbe User, der die XML in Schritt 2 erstellt hat.
- Option „Mit höchsten Privilegien ausführen" aktivieren, „Unabhängig von der
  Benutzeranmeldung ausführen" wählen.

## Optionale Parameter (Standardwerte normalerweise ok)

```powershell
-ServerInstance "localhost\SQL2012"
-Database       "ApDaten"
-DaysBack       7
-RcloneExe      "C:\Tools\rclone.exe"
-RcloneRemote   "trafag-bi:Import/Finance/Deutschland/AlphaplanRaw"
-NoCheckCertificate   # nur falls rclone Zertifikatsfehler meldet
```

## Wenn doch wieder „Schlüssel ist im angegebenen Status ungültig" kommt

Dann wurde die XML **nicht** als der ausführende User bzw. auf einem anderen
Rechner erstellt. Schritt 2 erneut ausführen — eingeloggt/laufend als exakt dem
Konto des Task-Jobs.
