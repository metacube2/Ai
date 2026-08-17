# Unterrouter Finance

Zurueck: `router.md`. Stand: 2026-08-17.

Finance Cockpit, Soll/Ist, Formeln, Marge, Standardkosten, Supplier, Journal,
Marktsegmente.

## Zuerst laden

| Bedarf | Datei |
| --- | --- |
| Kurzstand, Regeln, offene Fachpunkte | `docs/rag/FINANCE.md` |
| Zuletzt gepruefte Live-Zahlen (hat Vorrang vor Notizen) | `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md` |
| **Was ist noch offen?** | `docs/Issue_Log_Konsolidiert_2026-08-12.tsv`, dazu `docs/FINANCE_OFFENE_PUNKTE_2026-08-12.md` |

## Nach Thema

| Thema | Datei |
| --- | --- |
| Fachentscheide fuer Net Sales Actuals | `docs/FINANCE_ENTSCHEIDE.md` |
| Zeilenmechanik, Umrechnung, Marge, Filter | `docs/rag/FINANCE_FORMELN.md` |
| Detailregeln je Land | `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md` |
| Prozessablauf, Audit-CSV, Sales_All, Pruefbuch | `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` |
| Technischer Datenfluss end to end | `docs/FINANCE_DATENFLUSS_ANDREAS_2026-06-08.md` |
| Waehrungs- und Kursworkflow | `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md` |
| Gruppenmarge, Fachlogik und Kostenwaehrungsschalter | `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` |
| **Standardkosten, Kostenbasis, Konzernkosten TR AG/IT/IN** | `docs/FINANCE_STANDARDKOSTEN.md` |
| **Supplier-Klassifikation, Laenderstatus, CH-Werkstamm-Fallback** | `docs/FINANCE_SUPPLIER.md` |
| Hauptbuch-Import und EntitySet `FinanzJournalSet` | `docs/FINANCE_JOURNAL.md` |
| SAP-Spezifikation WAVWR/NETWR_HC | `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` |
| Aufbau und Formeln der Nachweis-Excel | `docs/FINANCE_DASHBOARD_NACHWEIS_2026-06-17.md` |
| Schulung fuer Anwender, Keyuser und Revision | `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md` |
| Budget-CHF-Fragen an den Finanzchef | `docs/FINANCE_BUDGET_CHF_FRAGEN_FINANZCHEF_2026-06-15.md` |
| **Marktsegmente, Railway, Marktumfrage** | `docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md` |

## Stimmt eine Anzeige nicht?

| Frage | Datei |
| --- | --- |
| Pruefbuch-Marge, Statusfarbe, Status „Konzernkosten fehlen", GUI gegen Excel | `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md` |
| Welche Indikatoren echt rechnen, fehlende Sollwerte, Waehrungsmischung, Pivot-Filter | `docs/FINANCE_INDIKATOREN_PRUEFUNG_2026-08-07.md` |
| UK 2025: Stueckpreis statt Zeilenwert, Faktor 9 | `docs/FINANCE_UK2025_WERTFEHLER_2026-08-10.md` |

## Fallen in diesem Ast

- **Gruppenmargen-Statustexte nie umbenennen, ohne
  `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md` Abschnitt 5a zu lesen.** Der Statustext
  `"OK"` steht zusaetzlich als Zeichenkette in der Excel-Formel des Nachweises; eine
  Umbenennung laesst dort **still** alle Margen leer. Kein Compiler und kein Test schlaegt
  an. Statuswerte gehoeren ausschliesslich in `Services/GroupMarginStatuses.cs`.
- **Fuellgrade nie mit `Spalte > 0`.** `StandardCost` ist TEXT, `CAST(... AS REAL)`
  verwenden.
- **`Sales_All_*.xlsx` ist Nachweis und Export, nicht die Live-Quelle der Reiter.** Die
  Dashboards lesen bevorzugt `Sales_ProcessedMergeInput_*.csv`.

## Querverweise in Nachbaraeste

- Fehlende Felder je Land, Exporte: `docs/router/standortdaten.md`
- SAP-Seite, ABAP, Produktsparten: `docs/router/sap.md`
- Deploy einer Finance-Aenderung: `docs/router/plattform.md`
