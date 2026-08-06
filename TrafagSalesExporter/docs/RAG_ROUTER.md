# RAG Router

Stand: 2026-08-06

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
11. **Bevor ein Gruppenmargen-Statustext geaendert oder ein neuer eingefuehrt wird:**
    `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md` Abschnitt 5a lesen. Der Statustext `"OK"`
    steht zusaetzlich als Zeichenkette in der Excel-Formel des Nachweises; eine Umbenennung
    laesst dort STILL alle Margen leer — kein Compiler, kein Test schlaegt an. Statuswerte
    selbst gehoeren ausschliesslich in `Services/GroupMarginStatuses.cs`.

## Themenverzeichnis

| Thema | Standard laden |
| --- | --- |
| Aktueller Produktiv-/Projektstand | `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`, danach `docs/rag/PROJECT.md` |
| Finance Cockpit, Soll/Ist, Regeln, Laender | `docs/rag/FINANCE.md` |
| Finance Formeln, Waehrung, Marge, Filter | `docs/rag/FINANCE_FORMELN.md` |
| Finance Prozess, Audit-CSV, Sales_All, Pruefbuch | `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` |
| **Stimmt eine Finance-Anzeige? Pruefbuch-Marge, Statusfarbe, Status „Konzernkosten fehlen", GUI gegen zentrales Excel** | `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md` |
| Manual Import UK/ES/DE | `docs/rag/MANUAL_IMPORT.md` |
| **Export-SQL DE/ES gehoert UNS** (Feld fehlt = Query liest es nicht) | `docs/rag/MANUAL_IMPORT.md` Abschnitt „Skripthoheit" |
| Spanien Buchungsdatum/PostingDate | `docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md` |
| **Indien: Sales Type, Eigenfertigung, Supplier/Preferred Vendor TRIN** | `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` |
| **Abfrage auf einem Standortsystem, das nur der Server erreicht** | `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` Abschnitt 5, Skript `docs/analyse/Run-ServerAnalysis.ps1` |
| **Innenumsatz / Konzerngesellschaft als Kunde / Doppelzaehlung** | `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` Abschnitt 4a (offene Frage an Andreas) |
| HR KPI | `docs/rag/HR_KPI.md` |
| Einkauf, Spend, Drilldown, Bestellungen, Kontrakte | `docs/rag/PURCHASING.md` |
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

- Aktuelle Aenderungen: `lastchange.md`
- Detailquellen, Werkzeugbefehle und Suchbegriffe: `docs/RAG_DETAIL_INDEX.md`
- Einordnung aktiver/historischer Dokumente:
  `docs/MD_DOKUMENTENSTATUS_2026-05-20.md`
- Historie/Audit: `docs/raw_md_archive/`
- Vollstaendiger vorheriger Routerstand:
  `docs/raw_md_archive/RAG_ROUTER_ARCHIV_2026-07-31.md`
