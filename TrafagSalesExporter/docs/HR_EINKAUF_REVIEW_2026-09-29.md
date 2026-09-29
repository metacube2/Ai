# HR- und Einkaufscockpit: Konsistenz- und Logikreview

Stand: 2026-09-29. Urspruenglicher Auftrag: **nur Analyse und Bericht**.
Nachfolgeauftrag am selben Tag: technische Fehler ohne neuen Fachentscheid reparieren,
offene Fachfragen mit vorhandenen MD-Anforderungen abgleichen. **Lokal umgesetzt, nicht deployed.**

## Reparaturstand nach Folgeauftrag

Die folgenden Analyseabschnitte dokumentieren den Zustand **vor** der Reparatur.

| Befund | Lokaler Stand nach Reparatur |
| --- | --- |
| H1 | Von/Bis hat auch fuer Berechnungsjahr und Vergleichskacheln Vorrang vor dem Jahresfeld |
| H2 | Monat/Quartal/YTD erhalten vollstaendige Kalenderperioden aus strukturgefilterten Austritten; HC verwendet auch vor dem Von-Datum ausgeschiedene Personen. Auswahlzaehler bleibt auf den freien Bereich begrenzt |
| H3 | Verlaesslichkeitsflag bis in Personenquoten und deren Druckansicht durchgereicht |
| H4 | Neuester-Jahr-Default nur bei Erstinitialisierung ohne gespeicherte Auswahl und ohne Datumsbereich. Bewusst geleertes Jahr bleibt leer |
| H5 | Take(100)/Take(250) vor den Pagern entfernt. PDF ist weiterhin Seitendruck, kein Voll-Export; dieser Umfang steht nun im Druckkopf |
| E1 | Eigene ungekappte Jahresaggregation; Diagramm bleibt Top 10 |
| E2 | Top-Warengruppe priorisiert MARA.MATKL wie Diagramm und Matrix |
| C1 | Unvollstaendiger Cache erzeugt keine SAP-Live-Stichproben-KPIs mehr. Hinweis auf regulaeren Ladelauf; bestehende Simulationen bleiben als solche gekennzeichnet |
| C2 | Bestandsweite Fehlkurszaehlung und ausdrueckliche Warnung in UI/Servicemeldung. **Bewertungsregel nicht geaendert:** 1:1-Rueckfall bleibt bis zum Fachentscheid, betroffene Werte gelten nicht als belastbar |
| C3/C4 | Keine Quelldaten ersetzt und keine neue Fachregel erfunden. Beschreibung der Managementsicht korrigiert: Namen verborgen, Personenbezug bleibt |

Validierung: **138/138 gezielte HR-/Purchasing-/Supply-Chain-Tests bestanden**, sechs neue
Regressionstests plus erweiterter Absenztest; Release-Build erfolgreich mit bestehenden
Warnungen. Erster neuer Testlauf: ein reiner Testformatfehler (`4.0` statt bestehendem
Headcount-Format `4`), Erwartung korrigiert, anschliessend alle Tests gruen.
Kein produktiver Sichttest, kein Commit, kein Deploy. Finance-Dateien und Projektkonfiguration
unberuehrt; keine pauschale Gesamtsuite parallel zur fremden Finance-Arbeit gestartet.

### Abgleich offener Punkte: vorhandene Anforderung statt Doppelspur

Vor der Umsetzung gelesen: HR-/Einkaufsrouter, `HR_KPI.md`, Einkaufsanforderungshistorie,
relevante Passagen der Hauptdoku und HR-Fachpruefung; Issue-Log nach bestehenden Punkten
durchsucht. Keine neuen Issue-IDs oder parallelen Aufgabenlisten angelegt.

| Thema | Bereits dokumentiert wo? | Konsequenz |
| --- | --- | --- |
| 8.4 Stunden pro Krankheitstag | `HR_KPI.md`, Abschnitte 7/8: ausdruecklich noch nicht bestaetigt | Bestehende Frage beibehalten, keine neue Definition |
| Periodengenaue Rexx-Absenzen | `HR_KPI.md` 8.1; `docs/rag/HR_KPI.md` nennt bereits Call/Mailentwurf an Sonja | Im vorhandenen HR-Call klaeren, kein zweiter Arbeitsauftrag |
| Alte/abweichende HR-Quellstaende | `HR_KPI.md` 8.2 | Bestehenden Aktualisierungsbedarf bestaetigt, keine erneute Datenerhebung angefordert |
| Personenquote bei Teilzeit | `HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md`, Abschnitt 4: Sollzeit/Teilzeit schon als Pruefpunkt | Dort konkret auf die Personenquote beziehen; keine bestaetigte neue Formel gefunden |
| Anonyme Managementsicht | `docs/rag/HR_KPI.md`: bisher nur Namen ausblenden; HR-Fachpruefung 10/11: Rollen, Aggregation und Mindestgruppen bereits offen | Keine Freigabe fuer eine echte anonyme Aggregatsicht gefunden. Nur irrefuehrenden Hilfetext korrigiert, Rechte und Datenumfang unveraendert |
| Alttermine/offene Bestellungen | `PURCHASING_DASHBOARD_2026-06-05.md`, 27.08., Abschnitt 1 und Marco-Abgleich; Anforderungshistorie Abschnitt 3 | Bewusste zeitunabhaengige Offen-Sicht nicht entfernen; SAP-Bereinigung bleibt bestehendes Fachthema |
| Belegkurs gegen internen Kurs | Hauptdoku, 27.08., Marco-/Armin-Abgleich | Bereits als Entscheid dokumentiert, nicht als neue Anforderung aufnehmen |
| Fehlender Wechselkurs | Hauptdoku, 27.08., Abschnitt 3: theoretischer Fallback bereits erwaehnt, damals kein betroffener Beleg | Warnung umgesetzt; keine freigegebene Ersatzkursregel gefunden. C2 bleibt die Referenz, keine zweite Aufgabe |
| Lagerwert/MB5L | `EINKAUF_LAGERWERT_2026-08-18.md`, Abschnitte 6a.2/7; Anforderungshistorie Abschnitt 3 | Einmalige bestehende Fachabnahme, keine neue Funktion/Automatisierung. Wochenverlauf Weg A ist bereits entschieden |

Die seit 28.09. bestaetigten HR-Regeln (61 Tage, Reminderprofile, GLZ-/Ferienampeln,
Quartalsrate x 4, Fluktuationsdefinition und Ausschluesse) wurden nicht erneut zur
Entscheidung gestellt und bleiben unveraendert.

## Ergebnis

**Nicht durchgehend konsistent.** Die Grundmodelle sind nachvollziehbar, aber es gibt
reproduzierbare Fehler bei HR-Zeitraeumen und Detailanzeigen sowie bei Einkaufs-Teilsummen
und Warengruppen. Gruene bestehende Tests sind keine fachliche Gesamtfreigabe.

Vorrang haben H1/H2 (Fluktuation), H3 (scheinbar belastbare Krankenquote) und E1
(unvollstaendiger Jahres-Spend). Die Befunde unten unterscheiden bestaetigte Codefehler,
bedingte Fehlerpfade und fachliche bzw. datenbedingte Grenzen.

## Pruefumfang und Nachweise

- Einstieg ueber `router.md`, HR-/Einkaufsrouter und deren Fachbeschreibungen.
- Gegenpruefung der HR-Import-/Berechnungslogik, Filter und Razor-Tabellen einschliesslich
  Druckpfad; Einkauf SQL-Aggregationen, Spend-Matrix, Warengruppen, offene Bestellungen,
  CHF-Bewertung, Cache und Lagerwert; ergaenzend Supply-Chain-Filter und Datenbegrenzung.
- **132/132 bestehende gezielte Tests bestanden**, 0 uebersprungen. Filter:
  `FullyQualifiedName~HrKpiServiceTests|FullyQualifiedName~Purchasing|FullyQualifiedName~SupplyChain`.
  Release-Build mit bestehenden Compiler-/Analyzerwarnungen; kein App-Start.
- Reproduzierbare Zusatzdiagnose: `.tmp_tools/HrPurchasingReview0929/Review.csproj`.
  Sie verwendet vorhandene kuenstliche Testdateien, temporaere XLSX und SQLite im Speicher,
  ruft die echten Services auf und veraendert keine Produktivdaten.
  Aufruf: `dotnet run --project .tmp_tools/HrPurchasingReview0929/Review.csproj -c Release`.
- Produktiv-DLL `\\tragvapp401\BiDashboard$\BiDashboard.dll` direkt gelesen:
  SHA256 `ADFE606DBC1490C3BB596484C151EEDDA55E5DDBBD8FB8BC4948F09FD5C71980`.
  Entspricht dem bereits verifizierten Release `02bd0cd`; die geprueften Kernservices und
  HR-/Einkaufsseiten haben gegen diesen Release keine Differenz.
- Keine vollstaendige Browser-/PDF-Abnahme, kein vollstaendiger SAP-Abgleich und keine
  Quantifizierung der Auswirkungen auf echte Personal- oder Einkaufsbetraege.
  Fremde Finance_All-Arbeit und parallele Doku-Korrekturen bleiben unberuehrt.

## A. Bestaetigte Befunde HR

### H1 — Hoch: Datumsbereich hat nicht ueberall Vorrang vor dem Jahr

`Services/HrKpi/HrKpiDashboardBuilder.cs:1160` bevorzugt in
`ResolveTurnoverPeriodScope` zuerst `options.Year`. Dagegen bevorzugt
`MatchesLeaverDateFilter` ab Zeile 1245 den Von-/Bis-Bereich. Der Stichtag kommt wiederum
aus `ToDate`. So entstehen ein Zaehler aus dem Datumsbereich und Monats-Headcounts aus
einem anderen Jahr. Auch Vergleichsjahr und Monats-/Quartalskennzahlen koennen abweichen.

**Reproduktion:** dieselben Testdaten und Maerz 2025, einmal mit zusaetzlich gesetztem
Jahr 2024, einmal ohne Jahr:

| Anzeige | Jahr 2024 + Maerz 2025 | Nur Maerz 2025 |
| --- | ---: | ---: |
| HC Basis YTD, angezeigt gerundet | 1.7 | 2.8 |
| Austritte YTD | 1 | 1 |
| Fluktuation YTD | 60.0 % | 35.3 % |

Die Datumsbeschriftung ist in beiden Faellen `01.01.-31.03.`. Fuer den dokumentierten
Vorrang des Datumsbereichs muss das Ergebnis identisch sein.
**Technisch korrigierbar ohne neue Fachregel:** Zeitraum einmal aufloesen und einheitlich verwenden.

### H2 — Hoch: YTD verliert Austritte vor dem Von-Datum

`BuildTurnoverMetrics` ab Zeile 554 bildet YTD aus bereits datumsgefilterten `leavers`.
Die YTD-Kachel behauptet dennoch, ab 1. Januar zu zaehlen. Auch die HC-Intervalle verwenden
eine vorgefilterte Austrittsliste; frueher im Jahr ausgeschiedene Personen fehlen dann im Nenner.
Analog koennen Monats-/Quartalswerte nur einen Ausschnitt ihres beschrifteten Zeitraums enthalten.

**Reproduktion:** zwei relevante Austritte, 15. Januar und 15. Juni 2025, gleicher Stichtag 30. Juni:

| Filter | Austritte YTD | Fluktuation YTD | angezeigter Zeitraum |
| --- | ---: | ---: | --- |
| 01.01.-30.06. | 2 | 51.1 % | 01.01.-30.06. |
| 01.04.-30.06. | 1 | 26.1 % | 01.01.-30.06. |

**Technisch korrigierbar gemaess vorhandener YTD-Definition:** Strukturfilter beibehalten,
YTD/Quartal/Monat jeweils aus der dafuer passenden vollstaendigen Zeitbasis berechnen.
Die Kennzahl `Fluktuation Auswahl` darf weiterhin dem frei gewaehlten Bereich folgen.

### H3 — Hoch: Krankenquote nur in der Kachel maskiert, nicht in der Detailtabelle

Builder Zeilen 119-132 und 680ff markieren eine nicht periodisierbare Krankenquote korrekt
als `Zeitraum nicht bestimmbar`. `AggregateAbsencesByPerson` ab Zeile 454 berechnet trotzdem
`KrankenquoteMa`; `Components/HrKpi/HrKpiDashboardTabs.razor:122` zeigt sie ohne Maske als Prozent.

**Reproduktion:** undatierte Absenz von 8.4 Stunden, Filter Maerz 2025:
Kachel `Zeitraum nicht bestimmbar`, Personentabelle **4.76 %**.
Bei anderem Zeitfilter aendert sich die Personenquote, obwohl dieselben undatierten Stunden vorliegen.
Das widerspricht der Schutzmassnahme der Kachel und betrifft auch die gedruckte Ansicht.
**Technisch korrigierbar:** Verlaesslichkeitsstatus bis in Tabellen und Druck weiterreichen.

### H4 — Mittel: Jahr leeren wird beim Laden rueckgaengig gemacht

`Components/Pages/HrKpi.razor:92` bietet einen loeschbaren Jahresfilter. `LoadAsync` ab
Zeile 351 setzt jedes leere Jahr wieder auf das neueste Austrittsjahr und berechnet erneut.
Der Service unterstuetzt `Year = null` und hat dafuer Tests, die Oberflaeche behaelt diese
Auswahl aber nicht. Das ist auch fuer H1 relevant, weil die Seite selbst ein Jahr hinzufuegt.
**Technisch korrigierbar:** initialen Default von bewusst geloeschter Auswahl unterscheiden.

### H5 — Mittel: Vollstaendig wirkende Personentabellen sind vor dem Pager gekappt

`HrKpiDashboardTabs.razor:105`: Absenzen je Mitarbeiter nur `Take(100)`;
Zeile 468: Mitarbeitende nur `Take(250)`. Ein Pager erreicht ausgeschlossene Zeilen nicht.
Die zugehoerigen Kennzahlen rechnen mit dem ganzen gefilterten Bestand. Kein Hinweis auf
diese beiden Grenzen in den Tabellen. Eintritt erst oberhalb der jeweiligen Zeilenzahl;
produktive Betroffenenzahl wurde nicht ermittelt.

Zusaetzlich kopiert `wwwroot/js/download.js:29` beim PDF-Druck den bereits gerenderten DOM
(`element.outerHTML`); es gibt dort keine Neuberechnung einer vollstaendigen Personenliste.
Damit ist dieser Pfad kein nachgewiesener Vollstaendigkeits-Export. Ein Browser-/PDF-Test mit
mehreren Tabellenseiten bleibt erforderlich.
**Technisch korrigierbar:** vollstaendige Paging-Daten und explizit definierter Druckumfang.

## B. Bestaetigte Befunde Einkauf

### E1 — Hoch: „Spend aktuelles Jahr“ summiert nur die Top 10

`Services/PurchasingDashboardService.cs:585-597` begrenzt
`CurrentYearSupplierSpendRows` auf zehn Lieferanten. Die Detailkennzahl
`Components/Pages/PurchasingDashboard.razor:1850` summiert genau diese Liste, nennt sich
aber `Spend aktuelles Jahr`, ohne Top-10-Einschraenkung.

**Reproduktion:** 11 Lieferanten mit je CHF 100 im laufenden Jahr.
Gesamt-Spend und vollstaendige Matrix: **CHF 1'100**; Jahres-Detailkennzahl: **CHF 1'000**.
Die Hauptkennzahl `SpendChfSample` selbst ist dadurch nicht falsch.
**Technisch korrigierbar:** Jahresgesamtwert separat vollstaendig aggregieren;
Top-10-Liste nur fuer das Diagramm verwenden.

### E2 — Mittel: Warengruppen-Kennzahl und Diagramm verwenden unterschiedliche Felder

`PurchasingDashboardService.cs:491-498` verwendet fuer `TopMaterialGroupLabel` nur
`EKPO.Matkl`. Die Warengruppengrafik ab Zeile 520 und die Matrix verwenden dagegen
`MaraMatkl`, mit `Matkl` als Rueckfall. Der UI-Quellentext der Detailkennzahl nennt
bereits `MARA.MATKL / EKPO.MATKL` (Seite Zeile 1852).

**Reproduktion:** alle Positionen `Matkl=OLD`, `MaraMatkl=NEW`:
Kennzahl nennt **OLD: CHF 1'100**, Diagramm **NEW**.
**Technisch korrigierbar gemaess bestehender Prioritaet:** denselben Dimensionsausdruck verwenden.

## C. Bedingte Fehler und fachliche Grenzen

### C1 — Einkauf: Notweg ohne Cache rechnet nach anderen Regeln

`PurchasingDashboardService.cs:354ff`: Ist eine der drei Cachetabellen leer, folgt der
Live-Stichprobenpfad ab Zeile 289. Dieser liest maximal 1'000 Zeilen, beginnt beim
aktuellen Kalenderjahr statt beim gewaehlten Zeitraum und selektiert am Kopf keine
Waehrung, keinen Kurs und keinen Kontraktbezug.
`ApplyEkpoMetrics` ab Zeile 769 summiert unkonvertierte Netto-Belegwerte in
`SpendChfSample`; `ApplyEketMetrics` ab Zeile 821 setzt Kontraktwert gleich offenem Wert.
Die normalen Loesch-/Belegtypfilter sind dort ebenfalls nicht gleich umgesetzt.

**Bestaetigter Fehlerpfad, nicht als aktuell produktiv ausgeloest behauptet.**
Minimal sichere Reparatur: keine vollstaendigen CHF-/Kontraktkennzahlen aus dieser
Stichprobe ausgeben. Ob der Notweg nur Diagnose bleibt oder vollstaendig ausgebaut wird,
ist eine Umfangsentscheidung, keine Aenderung der regulaeren Finanzformel.

### C2 — Einkauf: fehlender Fremdwaehrungskurs wird still als 1:1 behandelt

`ChfValueSql`, Zeile 187: Fremdwaehrung mit Kurs 0/leer faellt auf den Originalbetrag zurueck.
**Reproduktion:** EUR 1'100 und WKURS 0 ergeben **CHF 1'100**, ohne Kurswarnung in der
Servicemeldung. In der historischen Produktivmessung vom 27.08. traf dieser Fall nicht zu;
eine erneute Vollbestandsmessung wurde hier nicht durchgefuehrt. Daher ein datenabhaengiges
Risiko, kein Nachweis aktuell falsch umgerechneter Produktivbetraege.
Nicht still einen anderen Kurs erfinden: fehlende Bewertung sichtbar machen;
gegebenenfalls Ersatzkursregel fachlich freigeben lassen.

### C3 — HR: Datenstand ist nicht aktuell

Dateimetadaten des produktiven `hrdata`-Ordners am 29.09. direkt geprueft:

| Quelle | letzter Schreibzeitpunkt |
| --- | --- |
| Saldiperstichdatum.xlsx | 08.07.2026 10:13 |
| Exportkommengehen.xlsx | 08.07.2026 10:13 |
| Personalausgeschieden.xlsx | 08.07.2026 10:13 |
| Abwesenheitinstunden.xlsx | 08.07.2026 10:17 |
| HR_KPI_EXPORT.xlsx | 26.05.2026 11:32 |

Das ist der Dateistand, nicht der Nachweis einer fachlichen Exportperiode.
Aktuelle Datums-/Ampelregeln treffen somit auf alte und unterschiedlich alte Quellen.
Das kann eine Codereparatur allein nicht loesen: Exporte aktualisieren und gemeinsamen
Stichtag sowie Absenzperiode bestaetigen. Keine Personalinhalte fuer diese Pruefung kopiert.

### C4 — Fachliche bzw. organisatorische Entscheidungen

- **HR-Managementsicht:** Sie ersetzt Namen durch `Personalnr. <Nummer>`; die Nummer steht
  weiter in Tabellen, zusammen mit Organisation und Gesundheitskennzahlen
  (`HrKpiDashboardTabs.razor:107-122, 269-272, 470ff`). Die Hilfe behauptet dagegen,
  Personendaten seien anonymisiert (Zeile 574). Das ist keine Entfernung des Personenbezugs.
  Gewuenschten Empfaengerkreis und Detaillierungsgrad mit HR festlegen; daraus folgt,
  ob eine reine Aggregatsicht noetig ist. Keine rechtliche Gesamtbewertung vorgenommen.
- **HR-Stunden/Teilzeit:** pauschale 8.4 Stunden pro Krankheitstag bleiben eine dokumentierte
  Annahme. Die Personenquote teilt durch Arbeitstage ohne individuellen FTE-Faktor,
  die Gesamtquote dagegen durch FTE x Arbeitstage. Bedeutung der Personenquote klaeren,
  bevor diese unabhaengig von H3 fachlich umdefiniert wird.
- **Einkauf-Altpositionen:** offene Werte sind bewusst zeitunabhaengig, einschliesslich alter
  Rueckstaende. Bereinigung/Abgrenzung gehoert zu Einkauf/SAP; nicht eigenmaechtig alte
  Bestellungen ausblenden. Die Zahlen aus der August-Messung wurden nicht als heutiger
  Bestand uebernommen.
- **Einkauf-Kurswahl:** Belegkurs gegen internen Planungskurs bleibt eine Fachfrage.
  C2 ist davon getrennt: ein fehlender Kurs ist nicht automatisch ein 1:1-Kurs.
- **Lagerwert:** Stichtagswert und Wochenverlauf sind keine historischen Monatsabschluesse.
  Eine SAP-MB5L-Abstimmung ist durch diese Codepruefung nicht ersetzt.

## D. Was in den geprueften Pfaden konsistent ist

- Einkaufs-Spend bleibt am Bestelldatum; offene Bestellungen bleiben zeitunabhaengig.
  Materialstatus 98/99 entfernt nicht nachtraeglich historischen Spend, wirkt aber in
  Offen-Sichten. Endlieferung und Bestelltyp werden im regulaeren Cachepfad beruecksichtigt.
- Lieferanten-Jahresmatrix und Spend-Aufriss sind nicht mehr auf die alten 40 Lieferanten
  bzw. 25 Materialien beschraenkt. E1 betrifft eine andere, weiterhin gedeckelte Liste.
- Kontrakt-Offenwert im regulaeren Cachepfad beschraenkt sich auf Belege mit Kontraktbezug;
  er ist nicht identisch mit jedem offenen Bestellwert. Ausnahme C1.
- Supply Chain addiert offene Bestellungen nicht nochmals zum SAP-Endbestand.
  Echte OTIF/Liefertreue wird mangels Ist-Wareneingangsdatum nicht vorgetaeuscht.
  Die 1'000er-Detailbegrenzung dort ist sichtbar erlaeutert; KPIs verwenden den vollen Umfang.
- HR verwendet Headcount statt FTE fuer Fluktuation, strukturelle Vergleichslisten fuer
  Vorjahr/gleitende Kennzahl sowie die dokumentierte 61-Tage-Klassifikation.
  Das bestaetigt die Grundrichtung, beseitigt aber H1/H2 nicht.
- Produktiver Einkaufsstatus read-only: letzter gelesener Erfolgsabschluss
  `2026-09-28T11:19:18.2722597Z` (13:19 Schweizer Zeit). Daraus allein wird kein neuer
  Ausfall am 29.09. abgeleitet.

## Empfohlene Reihenfolge

1. H1/H2/H3 und E1 mit eigenstaendigen Regressionstests reparieren.
2. H4/H5/E2 sowie sichtbare Warnung bzw. Sperre fuer C1/C2 nachziehen.
3. HR-Quellen aktualisieren; Absenzperiode und Managementsicht mit HR bestaetigen.
4. Produktive Fachabnahme mit identischem Zeitraum und Datengrundlage; bei HR auch
   mehrseitige PDF-Ausgabe, bei Einkauf Matrix-/Detailabstimmung und SAP-Stichprobe.

Der Bericht ist eine Fehleranalyse, keine Freigabe der gesamten Cockpits. Bestehende
Fachdokumente, Router und Issue-Log wurden wegen der parallelen Doku-Reservierung nicht
veraendert. Neue Befunde muessen dort nach Abstimmung eingetragen werden.
