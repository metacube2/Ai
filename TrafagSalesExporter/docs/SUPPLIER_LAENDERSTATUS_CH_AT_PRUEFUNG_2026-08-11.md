# Supplier-Status nach Ländern und CH/AT-Vorprüfung

**Stand:** 11.08.2026  
**Datenbasis:** `neu.xlsx` (96.233 Sales-Zeilen) sowie produktive `trafag_exporter.db` (read-only)  
**Zweck:** Issue 7 belastbar aktualisieren und Andreas nur noch die fachliche Restentscheidung vorlegen.

## Management Summary

- In allen drei Supplier-Feldern sind **18.241 von 96.233 Zeilen (19,0 %)** gefüllt; **77.992** sind leer. Diese globale Quote ist fachlich allein nicht aussagekräftig, weil die Länder unterschiedliche Quellen und Ersatzmerkmale besitzen.
- **UK ist vollständig (100 %)**. **Italien** ist mit **71,2 %** weit fortgeschritten. **Indien** hat nur **11,6 %** physisch gefüllte Supplier-Felder, ist aber über den aktuellen `Sales Type` zu **6.686 von 7.116 Zeilen (94,0 %)** funktional klassifizierbar.
- **CH/AT haben 0 % Supplier-Füllung aus strukturellem Grund:** Die Verkaufsfakturaquelle besitzt keinen Vorlieferanten. Die bestehende TSC-Regel klassifiziert CH/AT deshalb als `Intern / TR_AG`.
- Das ist **keine Zirkularreferenz**. Klassifikation und Kostenbasis sind getrennte, endliche Regeln. Das Risiko liegt woanders: Die pauschale Herstellerregel könnte einzelne zugekaufte Handels-/Ersatzteile ebenfalls als intern behandeln.
- Die CH/AT-Kostenbasis ist gut belegt: **47.350 von 48.932 Zeilen (96,8 %)** haben Standardkosten. Bei **18.068** Schweizer Fremdwährungszeilen folgt die Kostenwährung dem Beleg (`WAVWR_DC / FKIMG`); **104** positive Fremdwährungsfälle nutzen nachweisbar den CHF-Fallback.
- Die SAP-Einkaufshistorie markiert **1.191 Materialien / 5.910 CH/AT-Zeilen (12,1 %)** als Fremdbezugs-Prüfhinweis. **734 Materialien / 8.045 Zeilen** sind über Stückliste oder konzerninterne Vergleichsdaten als intern gestützt. Für **6.632 Materialien / 34.977 Zeilen** liefert der vorhandene Cache keinen direkten Beschaffungsnachweis; wegen des kleinen Stücklisten-Caches (105 Zeilen) ist das kein Gegenbeweis zur Eigenfertigung.

## Länderübersicht

| Land | TSC | Sales-Zeilen | alle 3 Supplier-Felder | Quote | Bewertung | Nächster Schritt |
| --- | --- | ---: | ---: | ---: | --- | --- |
| Schweiz | TRCH | 47.142 | 0 | 0,0 % | Verkaufsquelle ohne Supplier; Herstellerregel aktiv | Herstellerregel beibehalten, nur SAP-markierte Fremdbezugs-Ausnahmen mit Andreas bestätigen |
| Österreich | TRAT | 1.790 | 0 | 0,0 % | gleiche CH/AT-Quelllogik | gemeinsam mit CH entscheiden; keine pauschale Feldpflege |
| Deutschland | TRDE | 7.332 | 0 | 0,0 % | Supplier-Quelle/Mapping fehlt | Quellfeld und Mapping festlegen, neu laden und Quote nachmessen |
| Spanien | TRES | 5.697 | 0 | 0,0 % | Supplier-Quelle/Mapping fehlt | Antwort/Quellfeld bestätigen, Mapping ergänzen, neu laden |
| Frankreich | TRFR | 2.598 | 135 | 5,2 % | nur punktuell gefüllt | ungefüllte 2.463 Zeilen nach Quelle/Artikelstamm segmentieren |
| Indien | TRIN | 7.116 | 828 | 11,6 % | Supplier-Felder lückenhaft, aber Sales Type zu 94,0 % gefüllt | keine Massenpflege; nur 430 Zeilen ohne Sales Type und bekannte Widersprüche gezielt klären |
| Italien | TRIT | 19.955 | 14.208 | 71,2 % | weitgehend nachgezogen | verbleibende 5.747 Zeilen nach Ursache segmentieren und gezielt schließen |
| Vereinigtes Königreich | TRUK | 3.064 | 3.064 | 100,0 % | vollständig | nur Regression bei künftigen Exporten überwachen |
| USA | TRUS | 1.539 | 6 | 0,4 % | Supplier nahezu vollständig leer | Quellfeld/Mapping prüfen und belastbare Ersatzklassifikation suchen |

Umsatzwerte werden bewusst nicht über Länder addiert, weil sie in lokalen Währungen vorliegen.

## CH/AT: Was selbst geprüft wurde

### Grundmenge und Kostenbasis

| Kennzahl | Ergebnis |
| --- | ---: |
| CH/AT-Sales-Zeilen | 48.932 |
| unterschiedliche Materialien | 8.557 |
| Zeilen mit Standardkosten > 0 | 47.350 (96,8 %) |
| Zeilen mit zugeordneter Produktsparte | 48.752 (99,6 %) |
| TRCH-Fremdwährungszeilen | 18.723 |
| davon Kostenwährung = Belegwährung (WAVWR-Pfad) | 18.068 |
| davon Kostenwährung = CHF (Fallback-Kandidaten) | 655 |
| davon positiver CHF-Fallback | 104 |

Stichproben bestätigen den erwarteten Stückkostenpfad, beispielsweise Material `61645` mit `218,4032 EUR`, Material `64941` mit `147,2964 EUR` und Material `42703` mit `243,7517 USD`. Bei `B99999` wird dagegen der CHF-Fallback mit `0,23 CHF` verwendet. Die zentrale DB speichert nur den aufgelösten Stückpreis, nicht den ursprünglichen Rohwert `WAVWR_DC`; die Rohwertgleichheit mit `WAERK` ist deshalb lokal nicht erneut berechenbar, aber in der bestehenden SAP-Dokumentation bereits anhand von Originalzeilen bestätigt.

### Beschaffungsindizien

| Arbeitsklasse | Materialien | CH/AT-Zeilen | Anteil | Bedeutung |
| --- | ---: | ---: | ---: | --- |
| intern gut gestützt | 734 | 8.045 | 16,4 % | Stücklisten-/BOM-Indiz oder gleicher Artikel an anderem Standort nur mit Trafag/GFS-Supplier |
| Fremdbezugs-Prüfliste | 1.191 | 5.910 | 12,1 % | echter, nicht gelöschter Einkaufsbeleg zum gleichen Material; noch kein automatischer Beweis für Handelsware |
| ohne direkten Nachweis | 6.632 | 34.977 | 71,5 % | Cache liefert weder positives Eigenfertigungs- noch Fremdbezugsindiz; Herstellerstandard kann nach Freigabe greifen |

Die drei Klassen sind eine **Vorprüfung**, keine neue Produktivregel. Insbesondere ist die Einkaufshistorie ein Prüfhinweis: Ein Material kann eingekauft und trotzdem in einer anderen Ausprägung, als Ersatzteil oder im Rahmen der Fertigung verkauft werden.

### Priorisierte Fremdbezugs-Stichprobe für Andreas

| Material | Bezeichnung | CH/AT-Zeilen | Einkaufsnachweis / Supplier | Warum ansehen |
| --- | --- | ---: | --- | --- |
| E11221 | ASIC TRAFAG TX2a MLPQ32 | 71 | Presto Engineering France | klar zugekaufte Elektronikkomponente |
| E01389 | Schnappschalter Marquardt | 30 | Omni Ray / Marquardt | klar zugekauftes Bauteil |
| C13614 | Diagnostic Valve Block | 56 | Sole Solution / Hwajin | möglicher Handels-/Baugruppenfall |
| E11155 | ASIC TRAFAG TR5 | 18 | Aptasic u. a.; BESKZ F | stärkstes Fremdbezugsindiz |
| D34604 | Cover with opening coated | 35 | Fuchia Electron | zugekauftes mechanisches Teil |
| E11228 | ASIC TRAFAG TX2D | 22 | Presto Engineering France | klar zugekaufte Elektronikkomponente |
| F88103 | 8854 Transmitter EX | 65 | STS Sensor Technik Sirnach | fertiges Produkt, fachlich besonders relevant |
| D85031 | Metallbalg NG36 | 12 | Heitz GmbH | zugekauftes Teil |
| E11220 | ASIC TRAFAG TX1b | 13 | Presto / Aptasic | klar zugekaufte Elektronikkomponente |
| C15414 | Vessel Flange | 46 | Plattner / CPT | zugekauftes mechanisches Teil |
| R13025 | Gehäuse-Unterteil Industat | 4 | an TRIN: Somax Enterprise | einziger externer Kreuzstandort-Hinweis |

## Restentscheidung für Andreas

Andreas muss **nicht** alle offenen Materialien einzeln prüfen. Benötigt wird nur folgende fachliche Entscheidung:

> Gilt für Verkäufe von TRCH/TRAT die Herstellerregel `Intern / liefernde Gesellschaft TR_AG` auch dann, wenn zum verkauften Material ein externer Einkaufsbeleg existiert, oder sollen solche Materialien als Ausnahme nach `Extern` klassifiziert werden?

Empfohlene Abnahme:

1. Die obige Stichprobe, insbesondere `F88103` und `R13025`, fachlich einordnen.
2. Wenn externe Einkaufsbelege **keine** Ausnahme erzeugen, bleibt die heutige CH/AT-Regel bestehen; die 6.632 Cache-Lücken brauchen keine Einzelprüfung.
3. Wenn externe Einkaufsbelege eine Ausnahme erzeugen, zuerst eine fachliche Zusatzbedingung definieren (z. B. Materialart/Verkaufsrolle), nicht blind alle 1.191 Materialien umklassifizieren.
4. Optional den Stücklisten-Cache vollständig laden; aktuell enthält er nur 105 Zeilen und kann deshalb fehlende Eigenfertigungsindizien nicht ausschließen.

## Copy-ready: Nächster Schritt und Kommentar zu Issue 7

**Nächster Schritt:**  
Nach SAP-Aktivierung von ZDISPO den gemeinsamen App-Release deployen, den MARC-Werk-1100-Cache mit 66.047 Artikeln backfillen und die erwarteten 720 zusätzlichen Intern-Zuordnungen prüfen. Danach die verbleibenden 12.023 unklaren Fallback-Zeilen nach Land und Quellfeld bearbeiten. UK nur überwachen; für CH/AT mit Andreas weiterhin ausschließlich anhand der priorisierten SAP-Stichprobe entscheiden, ob Materialien mit externem Einkaufsbeleg eine Ausnahme zur Herstellerregel bilden.

**Kommentar:**  
Supplier-Felder sind je Land weiterhin unterschiedlich vollständig; UK ist vollständig, Italien und Indien sind nachgezogen. Für Fremdstandorte ohne Supplier und ohne Sales Type ist der neue, auf die alte Regel umschaltbare CH-Stammdaten-Fallback umgesetzt: Ein Treffer in MARC/Werk 1100 klassifiziert als Intern/TR_AG. Gegenüber MBEW bringt dies 720 zusätzliche Zeilen beziehungsweise 392 Materialien, ohne bisherige Treffer zu verlieren; 674 der 720 Zeilen betreffen Italien. Ein expliziter Supplier hat immer Vorrang. CH/AT sind kein Zirkularreferenzproblem; dort bleiben Klassifikation und Kostenbasis getrennt, und Andreas muss nur mögliche Fremdbezugs-Ausnahmen anhand der priorisierten Stichprobe entscheiden.

## Nachweise und Grenzen

- `neu.xlsx`, Blatt `Sales`, ausgelesen am 11.08.2026.
- Produktive `trafag_exporter.db`, ausschließlich read-only; Supplier-, Sales-Type-, Einkaufs- und Kostenmessung am 11.08.2026.
- `Services/GroupMarginSupplierClassifier.cs` und `Services/GroupMarginCalculator.cs` für die aktuelle Klassifikations- und Kostenlogik.
- `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` für den ursprünglichen SAP-Rohwertnachweis.
- `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` für die Indien-Ersatzklassifikation.
- Einschränkung: `MaterialUsageCache` enthält nur 105 Zeilen; fehlende BOM-Evidenz darf nicht als fehlende Eigenfertigung interpretiert werden.
