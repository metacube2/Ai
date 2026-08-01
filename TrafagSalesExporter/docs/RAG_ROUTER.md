# RAG Router

Stand: 2026-08-01

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
7. Arbeitsregeln, Tests und fachliche Grenzen: `persona.md`.

## Themenverzeichnis

| Thema | Standard laden |
| --- | --- |
| Aktueller Produktiv-/Projektstand | `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`, danach `docs/rag/PROJECT.md` |
| Finance Cockpit, Soll/Ist, Regeln, Laender | `docs/rag/FINANCE.md` |
| Finance Formeln, Waehrung, Marge, Filter | `docs/rag/FINANCE_FORMELN.md` |
| Finance Prozess, Audit-CSV, Sales_All, Pruefbuch | `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` |
| Manual Import UK/ES/DE | `docs/rag/MANUAL_IMPORT.md` |
| HR KPI | `docs/rag/HR_KPI.md` |
| Einkauf, Spend, Drilldown, Bestellungen, Kontrakte | `docs/rag/PURCHASING.md` |
| Oberflaechensprachen, Einkauf-Lokalisierung, Projektsuite | `docs/EINKAUF_LOKALISIERUNG_PROJEKTSUITE_2026-08-01.md` |
| Deployment/IIS | `docs/rag/DEPLOYMENT.md` |
| Admin/Startseite | `docs/rag/ADMIN.md` |
| Architektur | `docs/rag/ARCHITECTURE.md` |
| Produktmapping/Group Sales Report | `docs/rag/PRODUCT_MAPPING.md` |
| ZLO03/Stuecklistenanalyse-Webservice | `docs/abap/README_LZCODE_WEBSERVICE.md` |
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
