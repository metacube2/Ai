# Markdown-Konsistenzprüfung vom 29.09.2026

Auftrag: „prüfe alle md auf inkonsistenzen“. Nur Prüfung und Bericht; keine Bereinigung der bestehenden Fachdokumentation, keine Änderung der Anwendung und kein Deployment.

## Bereinigung durch Claude am 29.09.2026

Auf Ingos Auftrag sind die Befunde am selben Tag nachgeführt worden. Ingos Vorgaben: Standardkosten
einstufig mit Schnitt, IT-Konzernkosten-Status ermitteln, UK-Kostenquelle korrigieren, Spanien auf
Sage stellen, Kursregeln und aktive Spanien-Version klären, HR prüfen. Die offenen Fragen hat
Claude rein lesend gegen Code und Produktivdaten-Kopien geklärt.

| Befund | Ergebnis |
| --- | --- |
| 1 Standardkosten | Folgefrage als am 09.09. beantwortet markiert; offen bleibt nur der MARC-Fallback |
| 2 ES/UK | ES auf Sage 200 gestellt, UK mit Kostenspalte. **Abweichung vom Bericht:** `EinstandsPreis` in `DatabaseSeedService.cs` Zeile 974 gehört zum deutschen Excel-Mapping, nicht zu Spanien; Spanien liest `PrecioCoste` in unserer Export-SQL |
| 3 IT/IN-Konzernkosten | seit 25.08. produktiv (`b83ee84`), in `rag/FINANCE_FORMELN.md` und `rag/DEPLOYMENT.md` nachgeführt |
| 4 Mask/Convert | Convert als Code-Standard in beiden Dateien richtiggestellt |
| 5, 6 Kursregeln | drei getrennte Regeln in `rag/FINANCE_FORMELN.md` 3b beschrieben; Prozessablauf und Standardkosten 7c korrigiert; falsche Code-Kommentare gehören zu `ISS-018.1` |
| 7 Supplier | Vorrang und Kostenquellen präzisiert |
| 8 Indien CM | Tabellenzeile als überholt markiert |
| 9 Spanien | 7-Tage-Version ist aktiv und entschieden; `ISS-004.2` erledigt. Neu entdeckt: seit Mitte Juli kein Spanien-Import mehr (`ISS-020`) |
| 10 Juli-Livedatei | als historische Messreferenz eingestuft |
| 11 Einkauf | 60-Minuten-Regel im Kurzstand |
| 12 HR | Swiss-Prüfdatei mit Kopfhinweis und sieben Überholt-Vermerken; `HR_KPI.md` und HR-Router ergänzt |
| 13 baum.md | neun Dateien und dieser Bericht nachgetragen |
| 14 Archiv | gelöscht in `835b317` am 01.09., per `git show` wiederherstellbar; Wiederherstellung entscheidet Ingo |
| 15 Ladeweg | `docs/rag/init.md` an `router.md` angeglichen; Umfangszusagen korrigiert |

## Ergebnis und Prüfumfang

**Die Dokumentation ist nicht durchgängig konsistent.** Besonders relevant sind falsche aktuelle Aussagen über Standardkosten, widersprüchliche Kursbeschreibungen und offene Punkte, die an anderer Stelle bereits entschieden sind. Die Nachführung vom 29.09. hat wichtige Stellen berichtigt, aber nicht alle widersprechenden Absätze.

Bestandsaufnahme vor Erstellung dieses Berichts: **117 Projekt-Markdown-Dateien, 33’748 Zeilen**, einschließlich Tools, Whisper-Anleitungen, Projektmanagement, SAP, RAG und Koordinationshistorie. Alle Dateien wurden maschinell vollständig für Bestand, Überschriften und Markdown-Dateiverweise gelesen. Themenübergreifende Textsuchen und gezielte fachliche Gegenprüfungen erfolgten über die Themenrouter, aktuelle Entscheide, Issue-Log und ausgewählte Codepfade. Schwerpunkt der vertieften Prüfung: die im Gespräch angesprochene Kostenlogik sowie Finance, HR, Einkauf und Status-/Navigationsregeln.

**Das ist keine vollständige fachliche Abnahme jeder einzelnen Aussage in 33’748 Zeilen.** Nicht jede historische SAP-Messung, externe Norm, Mitarbeiterangabe oder ERP-Zahl wurde erneut unabhängig gemessen. Bei nicht vertieft geprüften Dokumenten bedeutet ein fehlender Befund nicht „fehlerfrei“.

Nicht enthalten sind Fremdpakete/Lizenzen in `site-packages`, Build-Verzeichnisse, temporäre Werkzeuge und Git-Interna. `.md.raw`, ZIP, DOCX, XLSX und SVG sind keine Markdown-Dateien und wurden nicht inhaltlich vollgeprüft. Die drei eigenen Whisper-Anleitungen sind enthalten. Es wurden keine Internetquellen oder SAP-Systeme abgefragt; externe Fachbehauptungen werden hier nicht neu bewertet.

Reproduzierbare Bestandsprüfung: `.tmp_tools/MdReview0929/Scan.ps1`, Ergebnis `.tmp_tools/MdReview0929/scan.json`. Das JSON enthält die vollständige Dateiliste mit Zeilenzahl, Kopfstand, Überschriften und Verweiskandidaten. Der neue Bericht selbst ist im ursprünglichen Bestand von 117 Dateien noch nicht enthalten.

## 1. Standardkosten: dieselbe Entscheidung gleichzeitig erledigt und offen

**Priorität hoch; direkter innerer Widerspruch.**

`docs/FINANCE_STANDARDKOSTEN.md:420` sagt ausdrücklich: Am 09.09. entschieden, Schweizer STPRS nicht generell unabhängig von der liefernden Gesellschaft verwenden. Bereits die nächste Tabellenzeile (`:421`) verlangt weiterhin Andreas’ Entscheidung genau zu dieser Frage.

Geltender Stand: Abschnitt 12 derselben Datei, `FINANCE_STANDARDKOSTEN_SCHNITT_ANDREAS_2026-09-09.md` und `ISS-007.3` sind einig: **Schnitt nach der ersten internen Lieferstufe**. Die Kostenquelle `DeliveringEntityCosts` ist der Standard. Die Schweizer Vergleichsoption ist eine gesonderte Alternative, keine rekursive Lieferkettenrechnung.

Korrekturvorschlag: In Zeile 421 nur den noch gültigen MARC-Fallback-Entscheid erhalten; die erledigte Folgefrage explizit als am 09.09. beantwortet kennzeichnen. Das erklärt einen Teil der Verwirrung im Gespräch „mit Schnitt oder wie bisher“: Der Schnitt war bereits das Standardverhalten.

## 2. Ländertabelle nennt Spanien als B1 und UK ohne Kostenquelle

**Priorität hoch; bestätigte falsche Quellenbeschreibung.**

`docs/FINANCE_STANDARDKOSTEN.md:107` führt ES gemeinsam mit IT/IN/US/FR unter B1 `INV1.StockPrice`/`RIN1.StockPrice`. `:108` nennt für UK „keine — Sage liefert keine Kostenspalte“, Füllgrad 0 %.

Gegenbelege:

- Spanien wird über Sage angebunden, siehe `docs/STANDORT_ES_SAGE.md` und Standortrouter. Das Mapping in `Services/DatabaseSeedService.cs:974` verwendet `EinstandsPreis` und die Währung.
- UK besitzt ein Mapping `Standard cost` / `Standard Cost Currency`, `Services/DatabaseSeedService.cs:892`. `docs/FINANCE_FELDLUECKEN.md:82` und `ISS-007` dokumentieren bereits im August einen hohen Kostenfüllgrad.

Korrekturvorschlag: Spanien aus der B1-Zeile herausnehmen; UK als vorhandene Kostenquelle führen. Alte Füllgrade nur mit Messdatum und Grundgesamtheit nennen. **Keine neue aktuelle Prozentzahl aus diesen alten Messungen ableiten.**

## 3. Formel-Kurzdatei bezeichnet IT-/IN-Konzernkosten weiterhin als offen

**Priorität hoch; bestätigter Status- und Quellenwiderspruch.**

`docs/rag/FINANCE_FORMELN.md:139`: TR IT/TR IN als interner Lieferant „weiterhin offen“, weil `PrdStdCst`/`AvgPrice` nicht befüllt seien. `:135` nennt für B1 lokale Kosten aus `OITM`-Preisfeldern.

Dagegen beschreibt `docs/FINANCE_STANDARDKOSTEN.md:156` die Implementierung seit 25.08. und ab `:183` den jüngsten positiven Beleg-Stückpreis. `Services/HanaQueryService.cs:528` und `:595` lesen `StockPrice` der Belegposition. Die vorhandene Konzernkostenbildung nutzt diese Daten für IT und IN.

Korrekturvorschlag: Zwischen „angebundene Belegkosten“, „nicht verwendbare bestimmte Artikelstammfelder“ und „offener Bewertungsentscheid“ unterscheiden. Leere Stammfelder bedeuten hier nicht, dass keine Kostenquelle existiert.

## 4. Mask wird trotz Convert-Code weiterhin als Code-Default bezeichnet

**Priorität hoch; unmittelbar gegen Code widerlegt.**

`docs/rag/FINANCE_FORMELN.md:162` nennt `Mask` als „Code-Default, produktiv nicht eingestellt“. Das ist auch nach der Nachführung vom 29.09. falsch.

`Models/ExportSettings.cs:28` initialisiert `GroupMarginCostCurrencyMode` mit `Convert`. `Services/GroupMarginCostCurrencyConverter.cs:42` fällt ebenfalls auf Convert zurück, sofern nicht ausdrücklich Mask gewählt ist. Im Finance-Review vom heutigen Tag war Convert zusätzlich produktiv gelesen worden.

Auch `docs/FINANCE_GRUPPENMARGE_2026-06-16.md:68` beschreibt noch Mask als Default; dort muss die alte Regel zeitlich eingeordnet werden.

Korrekturvorschlag: Convert ist seit dem Beschluss vom 27.08. der Code-Standard; Mask ist eine bewusst wählbare Ausnahme. Historischen früheren Default entsprechend kennzeichnen.

## 5. Drei widersprechende Aussagen zur CHF-Kurslogik

**Priorität hoch; unmittelbar für Nachrechnung relevant.**

| Stelle | Aussage | Gegenbefund |
| --- | --- | --- |
| `docs/rag/FINANCE_FORMELN.md:90` | Kursdatum aus Belegzeile, ausdrücklich kein fixer 31.12. | Die Finance-CHF-Anzeige verwendet in `ManagementCockpitService` unter anderem den Jahresendkurs je Zeile; Gruppenmarge/Prüfbuch haben zusätzlich das gewählte Kursprofil. Allgemeine Analyse und Finance-Sicht dürfen nicht zusammen beschrieben werden. |
| `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md:317` | Produktiv Jahreskurs, Profilschalter nur lokal vorbereitet und nicht deployed | Die produktive Einstellung wurde am 29.09. mit `CurrentDailyRate` gelesen. Der Schalter ist Bestandteil des ausgelieferten Stands. |
| `docs/FINANCE_STANDARDKOSTEN.md:549`, `:570` | Kursprofil wirkt einheitlich auf alle Ausgaben | Der Review vom 29.09. reproduziert gerade unterschiedliche Werte zwischen Gruppenmarge und Prüfbuch sowie beim CHF-Anzeigeschalter. Das ist ein Ziel, keine erfüllte Zusicherung. |

Korrekturvorschlag: Pro Auswertung einen eigenen Satz zu Eingabewährung, Kursdatum und Behandlung fehlender Kurse. Die offenen Fehler A1/A2 des Finance-Reviews sichtbar referenzieren. Der heutige globale Warnhinweis in der RAG-Datei ersetzt diese konkreten Korrekturen nicht.

## 6. Prozessbeschreibung führt bereits behobene Fehler als offen

**Priorität mittel; falsche Reparaturaufträge möglich.**

`docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md:323` nennt die Originalmarge bei Währungsabweichung als Mischrechnung und die Pivotumrechnung aller Jahre zum ausgewählten Jahreskurs als offene Punkte.

Im heutigen Code wird die Originalmarge bei maskierter Währung nicht ausgegeben (`ManagementCockpitService`, `BuildFinanceAuditLedgerRows`). Die Vorumrechnung verwendet `new DateTime(row.Year, 12, 31)`, also das Jahr der jeweiligen Zeile. Es bestehen andere Kursfehler aus dem aktuellen Review; diese machen die beiden alten Beschreibungen nicht wieder richtig.

Korrekturvorschlag: Die historischen Fehler als behoben kennzeichnen und auf die tatsächlich offenen A1/A2 verweisen.

## 7. Supplier-Doku ist beim Vorrang und bei Kostenquellen missverständlich

**Priorität mittel.**

`docs/FINANCE_SUPPLIER.md:19` sagt „explizit gepflegter Supplier hat immer Vorrang“. Im Kontext der Fallback-Tabelle ist der Vorrang gegenüber MARC plausibel; als allgemeine Regel ist die Formulierung zu weit. Die aktuelle Reihenfolge lautet CH/AT-Regel, Sales Type, expliziter Supplier, Fallback. `docs/FINANCE_STANDARDKOSTEN.md:39` erklärt den Sales-Type-Vorrang seit 27.08.

`:30` derselben Supplier-Datei sagt außerdem, Konzernkosten kämen „weiterhin ausschließlich aus MBEW/GroupStandardCosts“. `GroupStandardCosts` ist die gemeinsame Tabelle; MBEW ist nur die Schweizer Quelle, IT/IN kommen aus B1-Belegkosten.

Korrekturvorschlag: „Vorrang gegenüber dem Fallback“ schreiben und Datenhaltung von den drei fachlichen Kostenquellen trennen. Diese Aussage ist keine neue Änderung der einstufigen Regel.

## 8. Indien-Dokument enthält eine widersprechende Pflegeaufforderung

**Priorität mittel; Risiko unnötiger Rückfragen an Indien.**

`docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md:106` bezeichnet `CM` ohne Supplier als Pflegefall. Im selben Dokument erklärt der spätere Abschnitt 3a ausdrücklich: CM ist Eigenfertigung im Auftrag, kein Vorlieferant nötig, **kein Pflegefall**. Der Nachtrag vom 27.08. entscheidet zusätzlich den Vorrang von Sales Type bei widersprechenden Supplier-Feldern.

Korrekturvorschlag: Die frühere Klassifikation unmittelbar an der Tabellenzeile als überholt kennzeichnen. Historische Messzahlen können bleiben; die alte Handlungsaufforderung darf nicht als aktueller Auftrag erscheinen.

## 9. Spanien: zwei entgegengesetzte Arbeitsaufträge

**Priorität hoch; Statuskonflikt, der nicht allein technisch entschieden werden sollte.**

- `projektmanagement/Wochen_Todo.tsv`, ISS-004.2: erledigt, Entscheid 02.09., 7-Tage-Version bleibt; keine weitere Aktion bei Santi.
- `docs/Issue_Log_Konsolidiert_2026-08-12.tsv`, ISS-004.2: läuft, Santi soll 35-Tage-Version einspielen, einziger offener Schritt.
- `docs/STANDORT_ES_SAGE.md:149` fordert ebenfalls die 35-Tage-Version.

Der Konflikt ist bereits in der Koordination vom 29.09. benannt, aber nicht aufgelöst. Die Codeentscheidung „InvoiceDate für Spanien“ allein beantwortet die Frage nach dem Exportfenster nicht.

Korrekturvorschlag: Den maßgeblichen Entscheid zum Exportfenster bestätigen und danach Issue-Log, Wochen-Todo und Standortdokument zusammenführen. Bis dahin den Konflikt ausdrücklich anzeigen, statt eine Seite still als erledigt zu setzen.

## 10. Alte Messdatei wird weiterhin als vorrangiger aktueller Stand angeboten

**Priorität mittel bis hoch für künftige Analysen.**

`docs/rag/PROJECT.md:5`, `docs/rag/PURCHASING.md:5`, Finance-Router und `baum.md:42` weisen der Datei `AKTUELLER_LIVEDATEN_STAND_2026-07-31.md` eine besondere aktuelle Vorrangstellung zu. Dort stehen jedoch ausdrücklich Messungen vom Juli, unter anderem nur Schweizer Konzernkosten und damals noch unbestätigte Delta-Wirkung.

Neuere Messungen und Deploynachweise zu IT/IN-Kosten und Einkaufsläufen liegen vor. Der globale Router sagt korrekt, dass eine datierte Notiz kein Beleg für heute ist.

Korrekturvorschlag: Die Juli-Datei als historische Messreferenz führen. Vorrang an Datum, Thema und direktem Beleg festmachen, nicht dauerhaft am Dateinamen „AKTUELLER“.

## 11. Einkauf: 15-Minuten-Snapshot steht weiter im Kurzstand

**Priorität mittel; veraltete Betriebsbeschreibung.**

`docs/rag/PURCHASING.md:15` und die Hauptdoku ab `docs/PURCHASING_DASHBOARD_2026-06-05.md:1198` nennen 15 Minuten. `Services/PurchasingDashboardSnapshotCache.cs:25` und `docs/PLATTFORM_TEMPO_2026-09-28.md:100` belegen 60 Minuten sowie sofortige Rückgabe abgelaufener Stände mit Nachberechnung.

Die ältere Passage ist zeitlich dem September-3-Fix zugeordnet; sie ist deshalb als damaliger Befund nicht falsch. Im aktuellen Kurzstand fehlt aber die klare Ablösung des alten Verhaltens. Ähnlich steht dort noch „Delta-Live-Wirkung offen“ für einen Julibefund, obwohl spätere erfolgreiche Läufe beschrieben sind.

Korrekturvorschlag: Aktuelle Betriebsregel zuerst nennen; frühere Messungen ausdrücklich als Verlauf erhalten.

## 12. HR-Prüfdokument beschreibt überholte Regeln als „Aktueller Reiter“

**Priorität mittel; historische Fachprüfung ohne ausreichenden Statusübergang.**

`docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md:135` beschreibt Kurz/Lang anhand zweier Rexx-Felder; `:213` nennt ganzjährig Restferien bis 5 Tage grün und saisonale Logik als fehlend.

`docs/HR_KPI.md:160` und `:163` sowie `Services/HrKpi/HrKpiDashboardBuilder.cs:1348` belegen seit 28.09. die 61-Tage-Regel und die Quartalsregel für Restferien. Die alte Datei trägt zwar einen Maistand, wird im aktuellen HR-Router aber weiterhin direkt als Fachprüfung angeboten.

Korrekturvorschlag: Kopfhinweis auf die Umsetzung vom 28.09. und Kennzeichnung der erledigten Prüfpunkte. Die damalige Beurteilung und externe Quellen nicht umschreiben. Keine neue Prüfung externer HR-Normen wurde durchgeführt.

## 13. Vollständigkeitsindex ist nicht vollständig

**Priorität mittel; maschinell und per Dateibestand bestätigt.**

`baum.md` verspricht, jede Markdown-Datei genau einmal zu führen. Im geprüften Bestand fehlen neun vorhandene Projektdateien:

1. `docs/AGENT_COORDINATION_HISTORIE.md`
2. `docs/DATENQUELLEN_FIREWALL_TRAGVAPP401_2026-09-10.md`
3. `docs/FINANCE_FACHPRUEFUNG_2026-09-07.md`
4. `docs/UEBERGABE_2026-09-08.md`
5. `Tools/BahnmarktDeWorkbook/README.md`
6. `Tools/BahnWorkbookDb4/README.md`
7. `Tools/DeCustomerBackfill/README.md`
8. `Tools/DeCustomerMapping/README.md`
9. `Tools/Qdrant/README.md`

Der neue Prüfbericht ist zusätzlich noch nicht im Index eingetragen. Die neun oben sind der Befund vor seiner Erstellung.

## 14. Archivverweise führen auf lokal nicht vorhandene Dateien

**Priorität mittel; echter Navigationsbefund, kein Nachweis eines Datenverlusts.**

Die vier in `baum.md:173` bis `:176` als Archivdateien aufgeführten `.md`-Pfade existieren im aktuellen Workspace nicht. Auch `docs/raw_md_archive/HISTORY_CANONICAL.md.raw` ist lokal nicht vorhanden, obwohl `docs/rag/PROJECT.md` diesen Pfad als kanonische Detailhistorie empfiehlt.

Korrekturvorschlag: Tatsächlichen Aufbewahrungsort bzw. Git-Historie klären und Verweise berichtigen. Nicht behaupten, die Inhalte seien endgültig verloren.

Weitere unaufgelöste Namen sind **nicht automatisch defekte Links**: Viele stehen ausdrücklich in „zusammengeführt aus“-Listen oder als historische Dateinamen. Die Prüfung meldete 59 Verweiskandidaten; darin enthalten sind solche legitimen Nennungen, Kurzformen, ein Verweis auf den damals noch nicht angelegten Prüfbericht und Regex-Grenzfälle. Die Zahl 59 ist deshalb ausdrücklich keine Fehlerzahl.

## 15. Zwei verschiedene Ladereihenfolgen und eine zu weit gehende Kurzheitszusage

**Priorität mittel für Agentenarbeit.**

- `router.md` und Root-AGENTS verlangen `router.md → Unterrouter → Detaildatei`.
- `docs/rag/init.md:9` verlangt danach stattdessen `lastchange.md → RAG-Kurzdatei`; ein Unterrouter fehlt.
- Dieselbe Datei nennt `lastchange.md` kompakt und auf ungefähr sieben Tage begrenzt. Tatsächlich umfasst sie 2’023 Zeilen einschließlich Juli-Historie.
- Auch die Agentenkoordination bezeichnet sich als „letzte sieben Tage“, führt jedoch weiterhin den Kurzindex seit 02.09.

Korrekturvorschlag: Einen verbindlichen Ladeweg verwenden und Umfangszusagen an die tatsächliche Archivierung anpassen. Historische Einträge mit „nicht deployed“ sind bei einem späteren datierten Deploy kein Fehler; sie müssen jedoch als Historie erkennbar bleiben.

## Bereits konsistent oder ausdrücklich historisch

- Die einstufige Kostenentscheidung selbst ist in Abschnitt 12 der Standardkostendoku, im Entscheid vom 09.09., im Issue-Log und im vorhandenen Standardmodus konsistent. Keine mehrstufige Lieferkettenauflösung gefunden.
- Paolas alte Aussage „Bewertungsmethode nicht umstellbar“ ist in der zugehörigen Datei inzwischen am Kopf als überholt gekennzeichnet. Die Primärmail muss als Historie erhalten bleiben.
- Die 1’000er-Exportkappung ist in der heute aktualisierten Formeldatei und im Nachtrag zur Indikatorenprüfung inzwischen korrekt benannt. Das ist dort kein noch offener Dokumentationsbefund; der Programmfehler selbst bleibt offen.
- CH/AT-Journal: Testsystem-Implementierung und offener Produktivtransport werden in Journal-Hauptdoku und EntitySet-Doku unterschieden. Die doppelten Nachtragsabsätze sind redaktionell unsauber, aber „auf T76 gebaut“ und „in P76 noch nicht vorhanden“ widersprechen sich nicht.
- ZZPRDAT-Transportplan und Projektkurzstand beschreiben den nicht freigegebenen Transport übereinstimmend. Kein neuer SAP-Livestatus erhoben.
- Ein datierter Messwert von August und ein anderer von September sind allein noch keine Inkonsistenz.

## Empfehlung und Abschluss

Zuerst die aktuell führenden Texte bereinigen: `FINANCE_STANDARDKOSTEN.md`, `rag/FINANCE_FORMELN.md`, `FINANCE_SUPPLIER.md`, Prozessablauf und die Router. Danach veraltete Prüfpunkte in HR/Einkauf markieren und Index/Archivverweise reparieren. Eine fachliche Rückfrage ist insbesondere für den widersprüchlichen Spanien-Auftrag nötig; die übrigen hier konkret belegten Textkorrekturen lassen sich überwiegend aus vorhandenen Entscheiden und Code ableiten.

In diesem Auftrag wurden die Fachdokumente **nicht** korrigiert. Neu sind Bericht und lokales Prüfwerkzeug; die vorgeschriebene Agentenkoordination wurde aktualisiert. Keine Builds, Anwendungstests, Datenänderungen, Reindexierung, Commits oder Deployments. Eine Indexaktualisierung gehört erst zum beauftragten Bereinigungslauf.
