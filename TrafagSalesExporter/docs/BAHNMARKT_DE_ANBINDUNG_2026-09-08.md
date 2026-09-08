# Bahnmarkt Deutschland: Auswertung, Branchenfund und der fehlende Schluessel

Stand: 2026-09-08. Anlass war Rohail Munirs Anfrage nach der Datenaufbereitung fuer den
Bahnmarkt, mit dem Zusatz, dass Patrik die Zuordnung nicht gemacht hat und trotzdem etwas
vorzeigbar sein muss.

Ergaenzt `docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md` und
`docs/KONZEPT_RAILWAY_EXPORT_2026-09-01.md`, ersetzt beide nicht.

## 1. Was geliefert wurde

`Bahnmarkt_Datenaufbereitung_2026-09-08.xlsx` in der Repository-Wurzel, gelesen read-only
aus der produktiven Datenbank. Vier Blaetter: `Lesehinweis`, `Umsatz je Standort`,
`Bahnkunden` mit Autofilter und `Datenluecken`.

Bahnumsatz 2026 bis zum 08.09.:

| Standort | Kunden | Umsatz |
|---|---:|---:|
| TRIN | 1 | 26'298'606 INR |
| TRCH | 54 | 1'834'842 CHF |
| TRES | 7 | 641'928 EUR |
| TRIT | 28 | 281'087 EUR |
| TRFR | 12 | 230'594 EUR |
| TRUK | 8 | 208'194 GBP |
| TRAT | 5 | 176'169 EUR |

Bewusst **keine Gesamtsumme**: die Waehrungen stehen nebeneinander, eine Addition darueber
waere keine sinnvolle Zahl. Derselbe Fehlertyp, den die Finance-Fachpruefung vom
2026-09-07 als A1 und A3 im Cockpit gefunden hat.

**Vorbehalt in jeder Zeile:** Von 172 Zuordnungen ist genau **eine** bestaetigt, ein Kunde
bei TRIN. Die uebrigen 171 sind maschinelle Vorschlaege aus dem Namensabgleich mit der
Marktumfrage. Der Abgleich ist nachweislich fehlbar; er hat „BROT" auf „K.S. & BROTHERS"
gezogen. Die Spalte `Status` sagt das bei jeder Zeile, damit die Zahlen nicht als geprueft
weiterwandern.

## 2. Warum Deutschland fehlt, und warum das kein Datenproblem des Standorts ist

TRDE hat **null** Railway-Zuordnungen, obwohl die Anfrage aus Deutschland kommt. Ursache:
In allen 7'615 deutschen Verkaufszeilen ist `CustomerName` leer, die Kundennummer dagegen zu
100 Prozent gefuellt. Der Namensabgleich konnte dort nichts finden.

Die Ursache liegt **nicht** bei Alphaplan, sondern in unserem Export. `alphaplanExport.ps1`
liest `dbo.Belege` und die Positionen und gibt `RechnungsAdressenID` mit, loest den
Fremdschluessel aber nie auf die Adresstabelle auf. Der Name existiert also im Quellsystem
und wird nur nicht abgeholt.

Groesse des blinden Flecks: **359 deutsche Kunden mit 2'931'057 EUR in 2026**, dazu 424
Kunden mit 4'098'279 EUR in 2025 — vollstaendig ausserhalb der Bahnauswertung.

Ein Nebenbefund erklaert, warum trotzdem deutsche Firmen in der Auswertung auftauchen: In
der Marktumfrage ist **Deutschland mit 67 Eintraegen das groesste Land**. 40 davon sind mit
einem Verkaufskunden verknuepft, aber **kein einziger mit TRDE** — 18 haengen an Italien,
17 an der Schweiz, 3 an Oesterreich, je einer an UK und Spanien. Deutsche Bahnkunden sind
also erfasst, ihr Umsatz erscheint nur unter anderen Gesellschaften.

## 3. Der Fund: Alphaplan pflegt eine Branche

Rohail hat am 2026-09-08 einen Adressexport geliefert, 6'788 Adressen. Abgelegt als
`AlphaplanExportPackage/kundenstamm_DE_20260908.csv` mit Nummer, Name, Suchname, Land,
Branche und einem abgeleiteten Bahn-Kennzeichen.

**99 Adressen tragen als Branche `00 Bahn` oder `05 rw Railways / Bahntechnik`**, darunter
Deutsche Bahn, Siemens Mobility, Knorr-Bremse, Bombardier Transportation, Wabtec, MAHLE
Industrial Thermal Systems, Behr und Schoema.

Das ist **gepflegte Klassifizierung aus dem Quellsystem**, kein Namensabgleich. Fuer
Deutschland waere die Zuordnung damit belastbarer als in jedem anderen Standort — die
anderen acht haben kein solches Feld.

## 4. Der Schluessel passt nicht, und das ist belegt

Die naheliegende Idee, die Liste sofort als Nachschlagetabelle zu verwenden, traegt nicht.
Am 2026-09-08 gemessen:

| Seite | Wertebereich |
|---|---|
| Alphaplan `Adress-Nr.` | 10000 bis 601025, **kein einziger Wert unter 10000** |
| `CentralSalesRecords.CustomerNumber` bei TRDE | 10 bis 14016 |
| `Belege.RechnungsAdressenID` im lokalen Auszug | 0 bis 13990 |

Von 572 Kundennummern der Verkaufszeilen treffen 119 auf eine Adressnummer, also 20
Prozent. Diese Treffer liegen ausschliesslich im Ueberlappungsbereich 10000 bis 14016 und
sind **Zufall**. Wer darauf zuordnet, bucht Umsatz auf falsche Firmen.

Es sind zwei verschiedene Nummernkreise: unsere Verkaufszeilen tragen die interne
`AdressenID`, Rohails Export die fachliche `Adress-Nr.`. Es fehlt genau eine Bruecke
zwischen beiden.

## 5. Drei Wege zur Bruecke

1. **Interne ID mitexportieren.** Im Alphaplan-Exportassistenten heisst das Feld meist `ID`,
   `AdressenID` oder `Datensatz-Nr.`; es wird in der Maske nicht angezeigt, laesst sich aber
   anhaken. Schnellster Weg, kein Serverzugriff noetig.
2. **Rechnungsexport mit Belegnummer, Adress-Nr. und Name.** Unsere Zeilen tragen
   Belegnummern wie `RE2510000` und `GS2510095`; im lokalen Alphaplan-Auszug steht zu jeder
   Belegnummer die interne ID. Damit laesst sich die Bruecke selbst bauen.
3. **Dauerhaft: den Export erweitern.** `AlphaplanExportPackage/scripte/kundenname_ergaenzen.sql`
   enthaelt Schritt 1 zum Belegen der echten Tabellen- und Spaltennamen ueber
   `INFORMATION_SCHEMA` und Schritt 2 mit dem ergaenzten Kopfexport samt `LEFT JOIN` auf die
   Adresse. Rein lesend, laeuft nur auf dem deutschen Server (`localhost\SQL2012`, `ApDaten`).

Solange keiner der drei Wege gegangen ist, bleibt Deutschland ohne Namen und damit ohne
Bahnzuordnung.

## 6. Was danach moeglich wird

Namen an alle 7'615 deutschen Zeilen; Bahnkunden direkt aus der gepflegten Branche statt aus
Vorschlaegen; und im Reiter Marktsegmente fuer Deutschland bestaetigungsreife Zuordnungen
statt gar keiner. Fuer die uebrigen acht Standorte aendert das nichts — dort bleibt es bei
Patriks Pruefung der 171 Vorschlaege.
