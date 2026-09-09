# Bahnmarkt-Pruefmappe Deutschland

Stand 09.09.2026. Fachlicher Nachweis: `docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md`
Abschnitt 9.

Baut `Bahnmarkt_DE_Kundenzuordnung_<Datum>.xlsx` aus vier Quellen. Das Werkzeug rechnet
nichts neu und schreibt nichts zurueck; es liest ausschliesslich.

## Quellen und warum genau diese

| Quelle | wofuer | warum nicht anders |
|---|---|---|
| SharePoint ueber `--sharepoint=<db>` | **die Verkaufszeilen selbst**, gelesen mit `ManualExcelDataSourceAdapter` | genau der Weg des produktiven Imports, also der aktuelle Stand |
| Standort-CSV `Sales_ProcessedMergeInput_TRDE_*.csv` | Gegenprobe auf Vollstaendigkeit und Finanzwerte | ein Archivstand, deshalb nicht die Grundlage |
| `Rechnungen_*.xlsx` von Rohail | `Rech.-Nr.` auf fachliche `Adressnr._R` | die einzige vorhandene Bruecke von der Rechnung zur fachlichen Kundennummer |
| `kundenstamm_DE_*.csv` | Kundenname, Land, Branche | Rohails Branchenspalte ist unbrauchbar, siehe unten |
| Alphaplan-Rohdateien | Ersatzquelle fuer die Artikelbezeichnung ohne SharePoint | wird mit dem produktiven Importdienst gelesen, nicht selbst bereinigt |

**Warum die CSV nicht die Grundlage sein darf:** Am 09.09.2026 fehlten dem um 07:22
abgelegten Archivstand bereits die Rechnungen `RE2611081` und `RE2611082` mit sieben
Positionen. Wer aus der CSV baut, liefert eine Mappe, die hinter dem Produktivstand
zurueckliegt, ohne dass es auffaellt.

## Aufruf

```
dotnet run --project Tools/BahnmarktDeWorkbook -c Release -- \
  <sales.csv> <rechnungen.xlsx> <kundenstamm.csv> <invoice_lines.csv[,weitere]> <ziel.xlsx> \
  [--sharepoint=<db>]
```

`--sharepoint=<db>` ist der Regelfall und macht den produktiven Leser zur **Zeilenquelle**.
Ohne diesen Schalter baut das Werkzeug ersatzweise aus der CSV und den Alphaplan-Rohdateien;
dann fehlen die Zeilen aus den taeglichen Delta-Staenden, und der Lauf bricht mit
verbleibendem RTF-Schriftrest ab. Die Datenbank wird nur lesend geoeffnet und liefert
allein die SharePoint-Konfiguration, keine Verkaufsdaten.

## Drei Abbruchbedingungen

1. **RTF-Schriftrest.** Bleibt in der Spalte `Name` auch nur eine Zelle mit
   `MS Shell Dlg` oder `Microsoft Sans Serif`, bricht der Lauf ab. Diese Bezeichnungen
   duerfen nicht zu Rohail gehen.
2. **Verlust einer Zuordnung.** Traegt eine Zeile im Quellbestand bereits eine fachliche
   Kundennummer und wuerde die Mappe sie leer lassen, bricht der Lauf ab. Ausloeser war ein
   echter Fall: Rohails Rechnungsliste enthaelt **keine Gutschriften**, und ein erster Lauf
   haette 54 `GS`-Zeilen ihre bereits belegte Nummer gekostet.
3. **Fehlende Zeile gegenueber dem Archivstand.** Fehlt eine Zeile der Vergleichs-CSV in
   der Mappe, bricht der Lauf ab. Zusaetzliche Zeilen sind dagegen erwartbar und werden nur
   gemeldet: sie sind Rechnungen, die seit dem Archivstand dazugekommen sind.

## Was bewusst nicht verwendet wird

Die Spalte `Branche` aus Rohails Rechnungsliste. In 5'785 Zeilen steht dort eine Zahl statt
einer Branche, ueberwiegend bei Adressen ohne gepflegte Stammbranche; Alphaplan verschiebt
beim Export die letzten Spalten. Die Branche kommt deshalb aus dem Kundenstamm. Die Spalten
`Adressnr._R`, `Rech.-Nr.`, `Name-1_A` und `Land_L` sind davon nicht betroffen.

Ebenso wenig wird die interne `RechnungsAdressenID` als Kundennummer verwendet. Ihr Bereich
9 bis 13990 ueberschneidet sich mit echten Adressnummern; eine Zuordnung darueber wuerde
Umsatz auf fremde Firmen buchen.

## Nachweisspalte

Jede Zeile der Mappe traegt ihren Zuordnungsnachweis:

| Wert | Bedeutung |
|---|---|
| `Belegnachweis und Rechnungsliste, uebereinstimmend` | zwei unabhaengige Quellen sagen dasselbe |
| `Rechnungsliste Rohail 09.09.` | neu zugeordnet, vorher nur interne ID |
| `frueherer Belegnachweis, Gutschrift ohne Eintrag in Rohails Rechnungsliste` | Nummer bleibt aus dem alten Nachweis |
| `Rechnungsliste Rohail 09.09., weicht vom frueheren Belegnachweis ab` | Doppelnummer, fachlich offen |
| `offen: Gutschrift ohne fachliche Nummer, nur interne Alphaplan-ID` | nicht zugeordnet |

## Gegenprobe nach dem Bau

Menge, `SalesPriceValue` und Waehrung jeder Zeile gegen die Vergleichs-CSV pruefen. Beim
Lauf vom 09.09.2026 waren alle 7'615 Zeilen des Archivstands gleich; die Mappe enthielt
zusaetzlich die sieben Positionen der beiden neueren Rechnungen.

## Der dauerhafte Weg

Dieses Werkzeug repariert einen Bestand, es loest die Ursache nicht.
`AlphaplanExportPackage/scripte/alphaplanExport.ps1` liest in Zeile 164 `FROM dbo.Belege`
ohne Verbindung zur Adresstabelle. Solange das so bleibt, ist jede Bruecke eine
Momentaufnahme und muss bei neuen Rechnungen wiederholt werden.
