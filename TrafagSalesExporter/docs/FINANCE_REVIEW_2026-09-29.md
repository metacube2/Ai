# Finanz-Dashboard: Konsistenz- und Logikprüfung

Stand: 29.09.2026. Auftrag von Ingo: prüfen, ob alles konsistent ist und gröbere oder logische Fehler bestehen; ausdrücklich nur Analyse und Bericht.

## Ergebnis

**Das Finanz-Dashboard ist nicht durchgängig konsistent.** Sechs technische Befunde betreffen die Währungsrechnung, die Beschriftung und die Vollständigkeit der Exporte. Dazu kommen fachlich relevante Risiken bei Gutschriften, kostenlosen Waren, fehlenden Kosten und Periodenabgrenzung.

Besonders wichtig: Der CHF-Anzeigeschalter kann bei unverändertem Kursprofil die Gruppenmarge ändern, ohne das Prüfbuch entsprechend zu ändern. Der Prüfbuch-Export liefert außerdem nur die bereits gekürzten Detaildaten, obwohl die Oberfläche ihn für eine vollständige Nachrechnung empfiehlt.

Die Befunde des Berichts vom 07.09. wurden am heutigen Code erneut geprüft. Sie sind nicht allein aus der alten Dokumentation übernommen. Zusätzlich bestätigt wurden der CHF-Anzeigeschalter, die falsche Stückkostenwährung im Prüfbuch und die Exportkappung.

Es wurden keine Anwendungscodes, Fachregeln, Produktivdaten oder Einstellungen verändert. Kein App-Start, Commit oder Deployment. Neu sind dieser Bericht und ein isoliertes Diagnoseprogramm; die vorgeschriebene Agentenkoordination wurde aktualisiert.

## Gegenprüfung durch Claude am 29.09.2026

Auf Ingos Wunsch hat Claude jeden Befund dieses Berichts rein lesend gegen den Code
nachgeprüft. **Alle Befunde stehen so im Code, keiner ist als bewusster Entscheid
dokumentiert.** Die Zeilenangaben stimmen, vereinzelt um ein bis vier Zeilen verschoben. Die
Rechenbeispiele sind im Code nachvollzogen, aber nicht ausgeführt.

| Befund | Urteil | Einordnung |
| --- | --- | --- |
| A1 CHF-Schalter ändert die Marge, das Prüfbuch nicht | bestätigt | wirkt produktiv; der Hinweistext in `ManagementCockpitService.cs` ab Zeile 646, alle Sichten folgten demselben Kursprofil, stimmt mit Schalter nicht |
| A2 Umweg über Tageskurs bei Jahresendprofil, 54 CHF bei `Mask` | bestätigt, latent | wirkt nur mit Jahresendprofil oder `Mask`; produktiv laufen Tageskurs und `Convert`. Die Priorität „hoch" ist deshalb überzeichnet. Das Nachweis-Excel nutzt denselben Converter (`ExcelExportService.cs` Zeile 430), dort nicht weiter geprüft |
| A3 CHF-Summen mit lokalem Währungslabel | bestätigt, sichtbar | beim Filtern auf ein Land oder eine Währung ohne CHF-Schalter |
| A4 Lokaler Stückpreis mit Währung der Konzernkosten | bestätigt, sichtbar | Prüfbuchspalte, Excel-Export und `StandardCostChf`; `CostBasisChf` und `MarginChf` stimmen |
| A5 Prüfbuch-Export höchstens 1'000 Zeilen | bestätigt | `Take(1000)` in `ManagementCockpitService.cs` Zeile 702 f.; betrifft beide Cockpit-Exportknöpfe, beim Prüfbuch auch das zweite Blatt mit den Gruppenmargen-Details. Das Nachweis-Excel ist nicht betroffen (`ExcelExportService.cs` ab Zeile 299, ohne Kappung) |
| A6 DB-Summe addiert lokale Beträge ungerechnet | bestätigt, latent | nur wenn `StandardCostVariable` befüllt ist |
| B1 Menge 0 mit Wertgutschrift ergibt positive Marge | Code bestätigt, Ausmass datenabhängig | bisher nur benannt, nicht entschieden |
| B2 Kostenlose Ware fällt samt Kosten heraus | Code bestätigt, fachlich offen | bisher nur als Zählproblem behandelt |
| B3.1 Interner Lieferant ohne Konzernkosten meldet „OK" | Code bestätigt, fachlich offen | offen bleibt die Zeile nur bei Sales Type LRD; deckt sich mit dem offenen Punkt aus ISS-003 |
| B3.2 Leere Kostenwährung wird nicht erkannt | bestätigt | das Prüfbuch setzt die Verkaufswährung ein |
| B4 Extraktionsdatum als Periodendatum | Code bestätigt | als Rückfall in `docs/FINANCE_ENTSCHEIDE.md` dokumentiert, also bekanntes Risiko |
| B5 Marktsegment grenzt die Periode anders ab als Finance | bestätigt | die Spanien-Regel (Rechnungsdatum zuerst) fehlt in der Marktsegment-Sicht |
| C RAG-Datei teilweise veraltet | bestätigt | siehe unten |

**Am schwersten wiegen A1, A3, A4 und A5**, weil sie in der heutigen Produktiveinstellung
wirken und für Anwender sichtbar sind. A5 ist besonders heikel, weil Oberfläche und Doku mit
dem Cockpit-Export eine Vollständigkeit versprechen, die er nicht hat.

**Widersprüchlicher Doku-Stand, nicht nachgeführt:** `docs/rag/FINANCE_FORMELN.md`
Zeilen 21 bis 23 und `docs/FINANCE_INDIKATOREN_PRUEFUNG_2026-08-07.md` Abschnitt 2h stellen
den Cockpit-Excel-Export als vollständigen Weg zur Nachrechnung dar. Das ist nach A5 falsch;
die Korrektur gehört zu einem Reparaturauftrag.

## Prüfstand und Aussagegrenzen

Die produktive Datei `\\tragvapp401\BiDashboard$\BiDashboard.dll` wurde nur lesend geprüft. SHA256:

```text
ADFE606DBC1490C3BB596484C151EEDDA55E5DDBBD8FB8BC4948F09FD5C71980
```

Das stimmt mit dem dokumentierten Release `02bd0cd` vom 28.09.2026 überein. Die betroffenen Berechnungsdateien und die Cockpit-Seite unterscheiden sich im Arbeitsbaum nicht von diesem Release. Die unkommittierte Finance_All-Arbeit wurde nicht verändert und ist nicht Bestandteil dieses Produktivnachweises.

Produktive Einstellungen, am 29.09. direkt per ReadOnly-Verbindung gelesen:

| Einstellung | Wert |
| --- | --- |
| Kostenwährungsumrechnung | `Convert` |
| CHF-Kursprofil | `CurrentDailyRate` |
| Interne Kostenquelle | `DeliveringEntityCosts` |
| Audit-CSV als Zentralquelle | aktiviert |
| Aktive Finance-Referenzen | 17 für 2025, davon 14 mit Wert; keine für 2026 |

Die Rechenbeispiele unten sind **synthetische Testfälle**, keine Unternehmensbeträge. Geprüft wurden echte Services mit SQLite im Arbeitsspeicher, die Anzeige- und Exportpfade im Code sowie ausgewählte produktive Einstellungen. Keine vollständige Prüfung aller aktuellen Verkaufs-CSV, kein Browser-Abnahmetest, kein Abgleich gegen sämtliche ERP-Belege oder das Hauptbuch. Betroffene Produktivbeträge und Häufigkeiten sind nicht quantifiziert.

## A. Bestätigte technische Fehler

### A1 – CHF-Anzeigeschalter ändert die Bewertungsbasis der Gruppenmarge

**Priorität: hoch. Auch mit dem aktuell produktiven Tageskursprofil reproduziert.**

Beispiel: Umsatz 100 EUR, Konzernkosten 60 CHF, Jahresendkurs 0,90 und Tageskurs 0,80 EUR→CHF.

| Ansicht bei Kursprofil Tageskurs | Umsatz CHF | Kosten CHF | Marge CHF |
| --- | ---: | ---: | ---: |
| Gruppenmarge, CHF-Schalter aus | 80 | 60 | 20 |
| Gruppenmarge, CHF-Schalter an | 90 | 60 | 30 |
| Prüfbuch, in beiden Fällen | 80 | 60 | 20 |

Ursache: `AnalyzeFinanceSummaryAsync` konvertiert beim CHF-Schalter die Arbeitszeilen vorab mit dem Jahresendkurs. Die Gruppenmarge verwendet diese bereits umgerechneten `scopedRows`; das Prüfbuch verwendet die vorher gesicherten Originalzeilen und das eingestellte Kursprofil. Die Gruppenmarge sieht anschließend bereits CHF und kann den ursprünglichen Tageskursbezug nicht mehr herstellen.

Fundstellen: `Services/ManagementCockpitService.cs:471`, `:499`, `:549`, `:618`, `:627`; Schalter `Components/Pages/ManagementCockpit.razor:49`.

Konsequenz: Zwei als CHF ausgewiesene Margen widersprechen sich allein durch einen Anzeigeschalter. Empfehlung für eine spätere Reparatur: Bewertungsrechnung aus Originalbeträgen und Anzeigeumrechnung sauber trennen. Das ist hier nicht umgesetzt.

### A2 – Jahresendprofil rechnet Kosten über einen Tageskurs-Umweg

**Priorität: hoch. Bedingt durch Profilwahl; aktuell ist produktiv Tageskurs eingestellt.**

Mit demselben Beispiel liefert das Jahresendprofil:

| Ausgabe | Kosten CHF | Marge CHF |
| --- | ---: | ---: |
| Gruppenmarge | 67,50 | 22,50 |
| Prüfbuch | 60,00 | 30,00 |

Die Gruppenmarge rechnet `60 CHF / 0,80 × 0,90 = 67,50 CHF`. Das Prüfbuch übernimmt die ursprünglichen 60 CHF korrekt. Ein CHF-Ausgangswert darf sich durch den Umweg über EUR nicht ändern.

Ein zweiter Pfad desselben Problems betrifft `Mask`: Die nicht umgerechneten 60 CHF werden mit dem Umsatzkurs multipliziert und als 54 CHF ausgegeben. Die Marge wird in der Oberfläche zwar maskiert, die Kostenkachel bleibt dennoch falsch.

Fundstellen: `Services/ManagementCockpitService.cs:1538`, `:1550`, `:1807`, `:1856`; `Services/GroupMarginCostCurrencyConverter.cs:43` und `:65`.

Gegenprobe: Tageskurs + Convert + CHF-Schalter aus ergibt für dieses Beispiel in beiden Sichten korrekte Kosten 60 und Marge 20 CHF. Nicht jede Kombination ist falsch.

### A3 – CHF-Summen tragen bei einem Land die lokale Währungsbeschriftung

**Priorität: hoch. Unabhängig vom Jahresendprofil möglich.**

`BuildGroupMarginSummary` addiert `SalesValueChf` und `CostBasisValueChf`, übernimmt aber bei genau einer Verkaufswährung deren Label. Die Kacheln verwenden dieses Label unverändert.

Reproduktion: 100 EUR bei Kurs 0,90 ergeben die Anzeige **90 EUR** statt **90 CHF**. Entsprechend betrifft der Fehler auch die Kosten- und Margensumme.

Fundstellen: `Services/ManagementCockpitService.cs:1610`, `:1632`; `Components/Pages/ManagementCockpit.razor:1143`, `:1151`, `:3010`.

### A4 – Prüfbuch beschriftet lokale Stückkosten mit der Konzernkostenwährung

**Priorität: mittel bis hoch für die Nachvollziehbarkeit. Neu reproduziert.**

Wenn der Konzernkostentreffer eine andere Währung hat als der importierte lokale Standardpreis, vermischt das Prüfbuch beide Quellen:

- Importierter Standardpreis: 70 EUR.
- Verwendete Konzernkosten: 60 CHF.
- Angezeigter Standardpreis im Prüfbuch: **70 CHF**.
- `StandardCostChf`: **70**, obwohl die Umrechnung des lokalen Standardpreises zum Test-Jahresendkurs 63 CHF wäre.

`StandardCost = row.StandardCost` behält den lokalen Wert, `StandardCostCurrency` stammt dagegen aus `basis.CostCurrency`, also der ausgewählten Konzernkostenbasis. Die eigentlichen Zeilenkosten im Prüfbuch können gleichzeitig richtig sein.

Fundstellen: `Services/ManagementCockpitService.cs:1807`, `:1843`; Anzeige `Components/Pages/ManagementCockpit.razor:804`.

Eine spätere Korrektur muss lokalen Stückpreis und ausgewählte Kostenbasis jeweils mit ihrer eigenen Währung zeigen.

### A5 – Excel-Detailausgaben sind ebenfalls auf 1’000 Zeilen gekürzt

**Priorität: hoch für Abstimmung und Nachweis. Neu bestätigt.**

Der Service kürzt Prüfbuch und Gruppenmargendetails vor der Rückgabe mit `Take(1000)`. Der Exportknopf im Prüfbuch exportiert genau diese Liste, nach zusätzlichen Spaltenfiltern. Auch der Gruppenmargenexport übernimmt die gekürzten Details.

Reproduktion mit 1’002 relevanten Verkaufszeilen: Gesamtzähler jeweils 1’002, zurückgegebene Listen und damit Eingabe des UI-Exports jeweils nur 1’000. Nicht enthaltene Belege lassen sich auch durch den Spaltenfilter nicht wiederfinden.

Der UI-Hinweis empfiehlt ausdrücklich den Excel-Export für die vollständige Nachrechnung. Das widerspricht diesem Exportpfad.

Fundstellen: `Services/ManagementCockpitService.cs:702`; `Components/Pages/ManagementCockpit.razor:747`, `:2214`, `:2235`, `:2557`.

Abgrenzung: Dies betrifft die genannten Exportknöpfe des Management-Cockpits. Daraus folgt nicht, dass ein separat erzeugter Sales_All- oder Nachweisexport ebenfalls auf 1’000 Zeilen begrenzt ist. Eine fertige XLSX wurde für diesen Befund nicht erzeugt; geprüft wurden Service-Ergebnis und vollständiger Aufrufpfad zum Export.

### A6 – Deckungsbeitrag wird in der Konzernsumme nicht nach CHF umgerechnet

**Priorität: mittel; latenter Fehler, wenn variable Kosten geliefert werden.**

`SumContributionMargin` addiert die lokalen Deckungsbeiträge. Dieselbe Summary enthält ansonsten CHF-Werte. Bei mehreren Währungen werden unterschiedliche Währungen numerisch summiert.

Reproduktion: Umsatz 100 EUR und variable Kosten 20 EUR liefern 80 EUR DB. Bei Kurs 0,90 wäre die CHF-Summe 72; das Summary-Feld enthält 80. Bei genau einer Währung überlagert sich dies mit A3: Eine bloße Korrektur des Labels würde dann den DB falsch als CHF ausweisen.

Fundstellen: `Services/ManagementCockpitService.cs:1552`, `:1633`, `:1639`; `Components/Pages/ManagementCockpit.razor:3060`.

Der Test speist variable Kosten künstlich ein. Ob derzeit produktive Zeilen dieses Feld befüllen, wurde nicht neu gemessen. Fehlt der Split, bleibt der DB korrekt offen.

## B. Gröbere fachliche und logische Risiken

### B1 – Nullmenge und Wertgutschrift können positive Marge erzeugen

**Hohe fachliche Relevanz; Verhalten reproduziert, tatsächliche Belegarten nicht live quantifiziert.**

Menge 0 wird in `Magnitude` wie eine Einheit behandelt. Bei negativem Umsatz wird diese Kostenbasis zusätzlich negativ. Eine reine Preisgutschrift von −10 mit Menge 0 und Stückkosten 70 ergibt Kosten −70 und damit Marge **+60**.

Wenn keine Ware zurückkommt, wäre die Kostenrücknahme unbegründet; dann wäre die Marge −10. Eine echte Warenretoure ist anders zu behandeln. Der Code unterscheidet das anhand dieser Felder nicht ausreichend.

Fundstellen: `Services/GroupMarginCalculator.cs:72`, `:376`; `Services/ContributionMarginCalculator.cs:30`.

### B2 – Kostenlose Waren fallen mitsamt ihren Kosten aus der Margensicht

Die Aufnahmebedingung lautet `rawInclude && value != 0m`. Eine zusätzlich eingespielte Warenzeile mit Menge 1, Umsatz 0 und Konzernkosten 60 wird nicht in Gruppenmarge oder Prüfbuch aufgenommen; die Gesamtkosten bleiben unverändert.

Die neue Diagnose verwendet für den zweiten Aufruf einen frischen Service, damit dessen zehnsekündiger Cache die hinzugefügte Zeile nicht verstecken kann. Der Ausschluss ist damit als Filterwirkung bestätigt.

Fundstellen: `Services/ManagementCockpitService.cs:382`, `:1523`, `:1775`.

Wenn Gratiswaren, Muster oder Ersatzlieferungen zur betrachteten Marge gehören, wird diese dadurch zu hoch. Ob und welche Belegtypen einzubeziehen sind, bleibt eine fachliche Festlegung.

### B3 – Fehlende Kosteninformationen können als belastbar erscheinen

Zwei getrennte Fälle:

1. Erkannter interner Lieferant ohne Konzernkostentreffer: Außerhalb der speziellen LRD-Regel fällt die Rechnung auf lokale Kosten zurück und meldet `OK`. Reproduktion IT←Trafag AG: lokale Kosten 70, Status `OK`, Quelle `Interner Standardpreis`. Der Schnitt nach der ersten internen Stufe vom 09.09. entscheidet nicht ausdrücklich, dass ein fehlender Kostenwert dieser Stufe durch den Einkaufspreis der verkaufenden Gesellschaft ersetzt werden darf.
2. Fehlende Kostenwährung: Der Converter erkennt nur dann eine Abweichung, wenn beide Währungen gefüllt sind. Kosten 60 mit leerer Kostenwährung und Umsatzwährung EUR werden nicht maskiert. Im Prüfbuch ersetzt ein Fallback die fehlende Kostenwährung durch die Umsatzwährung.

Fundstellen: `Services/GroupMarginCalculator.cs:353`, `:365`; `Services/GroupMarginCostCurrencyConverter.cs:50`; `Services/ManagementCockpitService.cs:1807`.

Ein dokumentierter Quellvertrag kann eine fehlende Währung gegebenenfalls erklären. Ohne solchen Nachweis ist sie keine belastbare Rechenbasis. Die Häufigkeit beider Fälle wurde nicht neu ermittelt.

### B4 – Extraktionsdatum kann eine scheinbare Umsatzperiode erzeugen

Ohne Rechnungs- und Buchungsdatum verwendet `FinanceRuleEngine` das Extraktionsdatum. Die Probe mit ES, Rechnungsnummer 0, Betrag 100 und ohne beide Belegdaten wird eingeschlossen und auf das Extraktionsdatum eingeordnet. Eine neue Extraktion kann damit die Periode einer weiterhin undatierten Zeile verändern.

Fundstelle: `Services/FinanceRuleEngine.cs:24`.

Das Extraktionsdatum allein belegt keine fachliche Umsatzperiode. Der beschlossene Rechnungsdatum-Vorrang für Spanien bleibt davon unberührt.

### B5 – Marktsegmente und Finance sind nicht vollständig abstimmbar

Marktsegmentseite und -export verwenden weiterhin `PostingDate ?? InvoiceDate ?? ExtractionDate` sowie ihre eigene Rohdatenauswahl. Die Finance-Regeln verwenden für Spanien vorrangig das Rechnungsdatum und zusätzliche Include/Exclude-Regeln.

Beispiel: Rechnung 31.12.2025, Buchung 02.01.2026 → Finance 2025, Marktsegment 2026. Das ist ein bestätigter Unterschied der Codepfade; die Zahl aktuell betroffener Belege ist offen.

Fundstellen: `Services/MarketSegmentPageService.cs:163`, `:393`; `Services/MarketSegmentExportService.cs:71`, `:165`; `Services/FinanceRuleEngine.cs:46`.

## C. Bekannte Grenzen und bewusst beschlossene Regeln

- **Keine Sollwerte für 2026:** Live bestätigt. Für 2026 ist somit keine vollständige Soll/Ist-Bestätigung möglich. Das ist eine Daten-/Referenzlücke, kein Beweis für falsche Ist-Umsätze. Der Code unterscheidet fehlende Referenz von `OK`.
- **Standortsumme ist keine automatische Konsolidierung:** Die Standard-Ist-Sicht enthält laut Code Innenumsätze und weist sie separat diagnostisch aus. Die Lieferantenklassifikation allein eliminiert keine konzerninternen Kundenumsätze. Diese Summe darf nicht ohne zusätzliche Abgrenzung als konsolidiertes Ergebnis interpretiert werden.
- **Schnitt nach der ersten internen Lieferstufe:** Am 09.09. ausdrücklich entschieden. Die akzeptierte Thermostat-Ungenauigkeit und die zurückgestellte zweite Stufe sind keine neu entdeckten Programmfehler. Grundlage: `FINANCE_STANDARDKOSTEN_SCHNITT_ANDREAS_2026-09-09.md`.
- **Aktuelle Kosten und Tageskurse:** Können Vorjahresmargen bei späteren Änderungen neu bewerten. Das ist von einer historischen Ist-Kostenrechnung zu unterscheiden, aber nicht automatisch ein Verstoß gegen die beschlossene Managementsicht.
- **Mixed-Summen:** Die lokale Ansicht kann unterschiedliche Währungen numerisch addieren und warnt davor. Keine belastbare Konzernsumme; bereits bekannte Einschränkung.
- **Dokumentation teilweise veraltet:** `rag/FINANCE_FORMELN.md` nennt unter anderem noch Mask als Default und frühere Supplier-/Kostenlücken. Produktiv ist Convert eingestellt; die erste interne Kostenstufe ist inzwischen entschieden. Auch der allgemeine Periodenhinweis im Service beschreibt den ES-Sonderfall nicht vollständig. Historische Hinweise dürfen nicht als heutiger Funktionsstand gelesen werden.

## D. Nachweise und Verifikation

1. Bestehende Diagnose `.tmp_tools/FinanceReview0907`: erneut gegen den heutigen Code ausgeführt. Bestätigt A2, A3, A6, Nullmengen-Gutschrift, Kostenfallback, fehlende Kostenwährung und undatierten ES-Fall. Die Assertions dieses alten Programms erwarten den Fehlerzustand; Exitcode 0 bedeutet dort ausdrücklich nicht fachliche Korrektheit.
2. Neue Diagnose `.tmp_tools/FinanceReview0929`: echte öffentliche Service-Aufrufe mit isolierter SQLite-In-Memory-Datenbank, kein Webhost, keine Produktivverbindung. Bestätigt A1–A5 sowie kostenlose Waren mit frischem Service.
3. Relevante bestehende Tests: **238 bestanden, 0 fehlgeschlagen, 0 übersprungen**. Gruppen: FinanceRuleEngine, GroupMargin, CurrencyExchangeRateService, ContributionMarginCalculator, B1GroupStandardCostBuilder, FinanceReconciliationService, ManagementCockpitService. Grüne Bestandstests widerlegen die hier zusätzlich geprüften Kombinationen nicht.
4. Diagnose-Build erfolgreich; vorhandene Razor-/Analyzerwarnungen. Keine neuen Regressionstests und keine Fehlerbehebung im Rahmen dieses Auftrags.
5. Produktive DLL und ausgewählte Einstellungen/Referenzen nur lesend geprüft. Die erste Settings-Abfrage enthielt einen falschen Spaltennamen und wurde verworfen; die korrigierte Abfrage lieferte die oben dokumentierten Werte.

Reproduktion der ergänzenden Analyse:

```powershell
dotnet run --project .tmp_tools/FinanceReview0929 -c Release --no-restore --verbosity quiet
```

Gemessene Kernausgabe:

```text
LABEL: 90.00 EUR; expected 90 CHF
YEAR_END: group cost/margin=67.50000/22.50000; ledger=60.00/30.00
LEDGER_UNIT: imported cost=70 EUR; displayed=70.0 CHF; StandardCostChf=70.0
FREE: underlying rows=2, included details=1, contains free=False
MASK: group cost=54.000, ledger cost=60.00
DAILY: group cost/margin=60.00000/20.00000; ledger=60.00/20.00
DAILY_WITH_CHF_TOGGLE: group sales/cost/margin=90.00/60.00/30.00; ledger=80.00/60.00/20.00
CAP: ledger total=1002, returned/export input=1000; group total=1002, returned/export input=1000
```

## E. Empfohlene Reihenfolge, ohne Umsetzungsauftrag

1. CHF-Kurswege und Beschriftungen gemeinsam korrigieren: A1–A4. Der gleiche Beleg muss unter demselben Profil in Gruppenmarge und Prüfbuch übereinstimmen.
2. Vollständige Exportdaten unabhängig von der UI-Kappung bereitstellen: A5. Mit mehr als 1’000 Zeilen und einem Beleg außerhalb der ersten 1’000 prüfen.
3. DB-Währungsrechnung vor Nutzung befüllter variabler Kosten absichern: A6.
4. B1–B5 anhand aktueller Quellbelege quantifizieren und fachlich entscheiden; keine pauschale Behandlung aller Gutschriften oder internen Lieferanten.

Bis zur Klärung sind insbesondere CHF-Margen zwischen Ansichten und aus den genannten UI-Detailausgaben abgeleitete Vollständigkeitsnachweise nicht verlässlich abstimmbar. Dieser Bericht erteilt keine Freigabe für Änderungen oder ein Deployment.
