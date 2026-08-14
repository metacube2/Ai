# RAG Router

Stand: 2026-08-14

Zweck: kurzer Einstieg fuer die Kontextauswahl. Nur die zum Thema genannten
Kurzdateien laden; Detailquellen erst bei Bedarf ueber
`docs/RAG_DETAIL_INDEX.md` aufloesen.

## Vorrang und Lade-Regel

1. Bei UK-2025, Supplier-Feldern, `GroupStandardCosts`, Einkauf-Delta oder
   widerspruechlichen Altangaben zuerst
   `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md` laden.
2. Direkt gepruefte Live-Fakten haben Vorrang vor historischen Arbeitsnotizen.
3. Danach genau eine passende Kurzdatei aus `docs/rag/` laden.
4. `lastchange.md` nur fuer den aktuellen Aenderungsstand hinzunehmen.
5. Detail-, Sitzungs- und Auditdokumente nur ueber
   `docs/RAG_DETAIL_INDEX.md` nachladen.
6. SAP-/HANA-Fakten nicht aus Erinnerung oder alten Messungen ableiten:
   Live-Werkzeuge gemaess Detailindex verwenden und Ergebnis nachdokumentieren.
7. Bei einem fehlenden Feld in DE oder ES ZUERST pruefen, ob unsere eigene Export-SQL es
   ueberhaupt liest, bevor der Standort gefragt wird — die Queries in
   `AlphaplanExportPackage/` und `SageSpainExportPackage/` sind unsere. Sonst wird eine
   Bitte an die falsche Stelle gerichtet (passiert 2026-08-03 bei DE).
8. **Bevor ein Standort um Stammdatenpflege gebeten wird: pruefen, ob die Quelle die
   Information anderswo schon liefert.** Zweimal in einer Woche war die Bitte gegenstandslos —
   2026-08-03 bei DE (unsere Query las die Spalte nicht) und 2026-08-05 bei IN (das Feld
   `OITM."U_Tasc_ST"` beantwortet die Frage fuer 93 % der Artikel). Ein Standort, der
   ueberfluessige Pflege geliefert bekommt, nimmt die naechste Bitte nicht mehr ernst.
9. Bei Indien/TRIN, Eigenfertigung, Supplier oder „Sales Type" gilt
   `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` vor den Feldluecken-Dateien vom Juli.
   Abschnitt 3 in `docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md` ist ueberholt.
10. Arbeitsregeln, Tests und fachliche Grenzen: `persona.md`.
10a. **Bei Statusfragen („ist X noch offen?") NIE aus einer datierten Arbeitsnotiz
    antworten.** Gueltig ist `docs/Issue_Log_Konsolidiert_2026-08-12.tsv`; darueber hinaus
    zaehlt nur eine eigene Live-Messung. Am 2026-08-12 waren zwei Punkte in den MDs als
    offen gefuehrt, die produktiv laengst erledigt waren, und ein hoher Punkt fehlte ganz.
    Dateien mit einem `UEBERHOLT`-Block oben sind Historie und beantworten keine Statusfrage.
10b. **Fuellgrade nie mit `Spalte > 0` messen.** `StandardCost` und `PostingDate` sind
    TEXT-Spalten; in SQLite ist Text groesser als jede Zahl, das ergibt falsche 100 %.
    `CAST(... AS REAL)` verwenden.
11. **Bevor ein Gruppenmargen-Statustext geaendert oder ein neuer eingefuehrt wird:**
    `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md` Abschnitt 5a lesen. Der Statustext `"OK"`
    steht zusaetzlich als Zeichenkette in der Excel-Formel des Nachweises; eine Umbenennung
    laesst dort STILL alle Margen leer — kein Compiler, kein Test schlaegt an. Statuswerte
    selbst gehoeren ausschliesslich in `Services/GroupMarginStatuses.cs`.

12. **Vor jeder Aenderung, parallelen Agentenarbeit, projektweitem Build oder Deploy:**
    zuerst `docs/AGENT_COORDINATION.md` lesen. Aktive Bereiche und Reservierungen
    beachten, den eigenen Bereich vor der Arbeit eintragen und beim Abschluss mit
    Status, Dateien und Nachweis aktualisieren. Freie und historische Eintraege nicht
    mit aktuell laufender Arbeit verwechseln.

## Themenverzeichnis

| Thema | Standard laden |
| --- | --- |
| Aktueller Produktiv-/Projektstand | `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`, danach `docs/rag/PROJECT.md` |
| **Persoenliche Aufgabenliste Ingo, „woran arbeite ich gerade", ZLO03/ZC12/ZZPRDAT-Arbeitsstand, Auftraggeber und Termine** | `projektmanagement/PROJEKTSTATUS.md` (fuehrend, IDs `PM-01` ff.). Finance-Details NICHT von dort beantworten, dafuer gilt das Issue-Log |
| Finance Cockpit, Soll/Ist, Regeln, Laender | `docs/rag/FINANCE.md` |
| Finance Formeln, Waehrung, Marge, Filter | `docs/rag/FINANCE_FORMELN.md` |
| Finance Prozess, Audit-CSV, Sales_All, Pruefbuch | `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` |
| **Marktsegmente, Railway, Marktumfrage, Segment am Kunden statt am Produkt** | `docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md` |
| **Was ist im Finance Dashboard noch offen? Issue-Log-Status, Todo-Liste** | `docs/Issue_Log_Konsolidiert_2026-08-12.tsv` (Status je Punkt), dazu `docs/FINANCE_OFFENE_PUNKTE_2026-08-12.md` (Begruendung und Fallen) |
| **Stimmt eine Finance-Anzeige? Pruefbuch-Marge, Statusfarbe, Status „Konzernkosten fehlen", GUI gegen zentrales Excel** | `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md` |
| Manual Import UK/ES/DE | `docs/rag/MANUAL_IMPORT.md` |
| **Export-SQL DE/ES gehoert UNS** (Feld fehlt = Query liest es nicht) | `docs/rag/MANUAL_IMPORT.md` Abschnitt „Skripthoheit" |
| Spanien Buchungsdatum/PostingDate | `docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md` |
| **Indien: Sales Type, Eigenfertigung, Supplier/Preferred Vendor TRIN** | `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` |
| **Abfrage auf einem Standortsystem, das nur der Server erreicht** | `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` Abschnitt 5, Skript `docs/analyse/Run-ServerAnalysis.ps1` |
| **Innenumsatz / Konzerngesellschaft als Kunde / Doppelzaehlung** | `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` Abschnitt 4a (offene Frage an Andreas) |
| HR KPI | `docs/rag/HR_KPI.md` |
| Einkauf, Spend, Drilldown, Bestellungen, Kontrakte | `docs/rag/PURCHASING.md` |
| **Einkauf/Logistik Materialdisposition, Fehlteile, Deckung, Materialabhaengigkeit, Dispositionspruefung, Lieferperformance** | `docs/EINKAUF_LOGISTIK_SUPPLY_CHAIN_REITER_2026-08-06.md` |
| **Einkauf Produktgruppen, ZC23/Disponent, Mehrfachverwendung, ABC/XYZ-Nutzen** | `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md` |
| Oberflaechensprachen, Einkauf-Lokalisierung, Projektsuite | `docs/EINKAUF_LOKALISIERUNG_PROJEKTSUITE_2026-08-01.md` |
| Deployment/IIS | `docs/rag/DEPLOYMENT.md` |
| Admin/Startseite | `docs/rag/ADMIN.md` |
| Architektur | `docs/rag/ARCHITECTURE.md` |
| Produktmapping/Group Sales Report | `docs/rag/PRODUCT_MAPPING.md` |
| Logistik-Stuecklisten-Dashboard, Top-Down/Bottom-Up | `docs/LOGISTIK_STUECKLISTEN_DASHBOARD_2026-08-01.md` |
| ZLO03/Stuecklistenanalyse-Webservice/ABAP | `docs/abap/README_LZCODE_WEBSERVICE.md` |
| Ansprechpartner und Standortempfaenger | `docs/ANSPRECHPARTNER.md` |
| 180-Tage-Roadmap | `docs/INGO_TODOS_180_TAGE_2026-06-18.md` |
| Live-Pruefung SAP ERP oder SAP B1/HANA | `docs/RAG_DETAIL_INDEX.md`, Abschnitt `Live-Werkzeuge` |

## Weitere Navigation

- Agentenstatus, Dateireservierungen und parallele Arbeit:
  `docs/AGENT_COORDINATION.md`
- Aktuelle Aenderungen: `lastchange.md`
- Detailquellen, Werkzeugbefehle und Suchbegriffe: `docs/RAG_DETAIL_INDEX.md`
- Einordnung aktiver/historischer Dokumente:
  `docs/MD_DOKUMENTENSTATUS_2026-05-20.md`
- ABGELOEST, nicht laden: `projektmanagement/kontext.txt` ist ein
  ChatGPT-Rohprotokoll vom 05.05. bis 10.08.2026 und beantwortet keine
  Statusfrage. Es fuehrte am 2026-08-14 zwei Punkte falsch als offen, die
  laengst erledigt beziehungsweise anders blockiert waren. Gueltig ist
  `projektmanagement/PROJEKTSTATUS.md`.
- Historie/Audit: `docs/raw_md_archive/`
- Vollstaendiger vorheriger Routerstand:
  `docs/raw_md_archive/RAG_ROUTER_ARCHIV_2026-07-31.md`
