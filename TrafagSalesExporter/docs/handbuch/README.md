# Benutzerhandbuch (betriebswirtschaftlich)

Stand: 2026-10-06

Anwenderhandbuch aller Cockpit-Module aus betriebswirtschaftlicher Sicht: je Seite „Wozu",
„Was man sieht", „Wie gerechnet wird", „Einsatz im Arbeitsalltag" (nach Rolle) und
„Grenzen und Fallstricke", dazu je Kapitel ein Kennzahlen-Glossar. Auftrag Ingo 2026-10-06,
im Trafag-CI, als Word-Datei und als eigener Reiter auf oberster Menueebene.

## Wo was liegt

| Was | Ort |
|---|---|
| Quelle (eine Datei je Kapitel, `00_` bis `08_`) | `wwwroot/handbuch/kapitel/*.md` |
| Word-Datei (Download im Reiter) | `wwwroot/handbuch/Trafag_Cockpit_Handbuch.docx` |
| Schreibvorgabe fuer Kapitel (Format, Aufbau, Ton) | `docs/handbuch/VORGABE.md` |
| Generator Markdown nach Word | `Tools/HandbuchDocx` |
| Reiter im Cockpit | `/handbuch`, `Components/Pages/UserManual.razor`, Parser `Services/UserManualContent.cs`, Menueeintrag `user-manual` (Sortierung 5, nach Home) |

Der Reiter liest dieselben Markdown-Dateien wie der Generator. Wer ein Kapitel aendert, muss
deshalb nur die Word-Datei neu erzeugen; die Webansicht ist nach dem Deploy automatisch aktuell.

## Word-Datei neu erzeugen

Aus dem Projektordner:

    dotnet run --project Tools/HandbuchDocx -- wwwroot/handbuch/kapitel wwwroot/handbuch/Trafag_Cockpit_Handbuch.docx wwwroot/trafag.jpg

Danach die Datei einmal in Word oeffnen, Inhaltsverzeichnis aktualisieren und speichern (oder
per Word-COM: `TablesOfContents.Update()`, `Save()`), damit das Verzeichnis gefuellt ist und
Word beim Oeffnen nicht nachfragt. Am 2026-10-06 so gemacht: 105 Seiten.

CI: Orange `#C8501E`, dunkel `#B5481B`, Schrift Open Sans, Logo `wwwroot/trafag.jpg` auf
Titelseite und in der Kopfzeile, wie der Cockpit-Skin seit 2026-10-01.

## Entstehung und Pruefstand

Die Kapitel sind am 2026-10-06 aus Code (Razor-Seiten, Services) und Fachdoku erarbeitet
worden, Stand Commit `3ad0dae`. Nicht live gepruefte Aussagen sind im Text als solche markiert.
Bei der Erarbeitung aufgefallen und nicht behoben:

- Einkauf, Preisentwicklung: Titel und Kennzahl sagen „Min. Stueckpreis", gerechnet wird der
  mengengewichtete Durchschnitt (`PurchasingDashboardService`, um Zeile 650). Im Kapitel als
  Fallstrick beschrieben.
- Weltlage Radar: Seitentext sagt „Summe der Punkte", der Code mittelt seit 2026-10-05 ueber
  die Themen. Im Kapitel nach Code beschrieben.
- Netzwerk: einige Beschriftungen sagen noch 30 Tage aktiv, der Code rechnet seit 2026-10-05
  mit 44 Tagen.

Bei neuen Seiten oder geaenderten Rechnungen das betroffene Kapitel nachfuehren und die
Word-Datei neu erzeugen.
