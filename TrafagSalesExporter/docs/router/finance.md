# Unterrouter Finance

Zurueck: `router.md`. Stand: 2026-09-09.

Finance Cockpit, Soll/Ist, Formeln, Marge, Standardkosten, Supplier, Journal,
Marktsegmente.

## Zuerst laden

| Bedarf | Datei |
| --- | --- |
| Kurzstand, Regeln, offene Fachpunkte | `docs/rag/FINANCE.md` |
| Fachprüfung und konkrete Umsetzungspakete vom 07.09.2026 | `docs/FINANCE_FACHPRUEFUNG_2026-09-07.md` |
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
| **TR IT Bewertungsmethode: warum Moving Average technisch nicht umstellbar ist** (Antwort Paola vom 2026-09-04, Primaerquelle) | `docs/FINANCE_IT_BEWERTUNGSMETHODE_PAOLA_2026-09-04.md` |
| **TR IT Referenzkost je Artikel** aus Bestandswert geteilt durch Bestandsmenge: Andreas' Vorschlag vom 2026-09-09, was davon schon produktiv ist, vorbereitetes Messpaket, Abgrenzung gegen die Konzern-Herstellkosten, Antwortvorschlaege | `docs/FINANCE_IT_REFERENZKOSTEN_2026-09-09.md` |
| **Supplier-Klassifikation, Laenderstatus, CH-Werkstamm-Fallback** | `docs/FINANCE_SUPPLIER.md` |
| **Beides als Diagramm fuer Andreas**: Andreas' Grundregel, warum daraus vier Belegstufen werden, 17 Beispielzeilen mit markiertem Entscheidungsfeld, Kostenquellen, alle fuenf Schalter, Statuskette | `docs/FINANCE_LIEFERANT_STANDARDKOSTEN_WORKFLOW_2026-09-02.svg` |
| Hauptbuch-Import, live gepruefte B1-Feldbelegung, DueDate-Umsetzung und Zahlungs-/Ausgleichsdatum; CH/AT-EntitySet-Abgleich | `docs/FINANCE_JOURNAL.md` |
| **Journal fuer die Konsolidierung: Zielbild Andreas, Faelligkeitsdatum vorbereitet, Konzernkonto-Mapping offen, Aufbau von `Finance_All` mit Feldstatus** | `docs/FINANCE_JOURNAL_KONSOLIDIERUNG_ANDREAS_2026-09-08.md` |
| **Bahnmarkt Deutschland: produktiver Belegnachzug, 19 bestaetigte DE-Railway-Kunden, Rohail-Dateien und verbleibende Schluesselluecken** | `docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md` |
| SAP-Spezifikation WAVWR/NETWR_HC | `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` |
| Aufbau und Formeln der Nachweis-Excel | `docs/FINANCE_DASHBOARD_NACHWEIS_2026-06-17.md` |
| Schulung fuer Anwender, Keyuser und Revision | `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md` |
| Budget-CHF-Fragen an den Finanzchef | `docs/FINANCE_BUDGET_CHF_FRAGEN_FINANZCHEF_2026-06-15.md` |
| **Marktsegmente, Railway, Marktumfrage** | `docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md` |
| Railway-Excel-Export per Mausklick: Konzept, Blaetter, Defaultwerte | `docs/KONZEPT_RAILWAY_EXPORT_2026-09-01.md` |
| Railway-Export: Fragebogen zur Abstimmung mit Patrik | `docs/FRAGEBOGEN_RAILWAY_EXPORT_PATRIK_2026-09-01.md` |

## Stimmt eine Anzeige nicht?

| Frage | Datei |
| --- | --- |
| Pruefbuch-Marge, Statusfarbe, Status „Konzernkosten fehlen", GUI gegen Excel | `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md` |
| Welche Indikatoren echt rechnen, fehlende Sollwerte, Waehrungsmischung, Pivot-Filter | `docs/FINANCE_INDIKATOREN_PRUEFUNG_2026-08-07.md` |
| UK 2025: Stueckpreis statt Zeilenwert, Faktor 9 | `docs/FINANCE_UK2025_WERTFEHLER_2026-08-10.md` |
| Artikelbezeichnung beginnt mit `MS Shell Dlg` oder `Microsoft Sans Serif` (nur DE) | `docs/STANDORT_DE_ALPHAPLAN.md`, Abschnitt 8 |

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
