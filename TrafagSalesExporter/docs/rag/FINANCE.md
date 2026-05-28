# RAG Finance

Stand: 2026-05-27

## Kurzstand

- Fuehrende Sicht: `Finance Summary`.
- `Finance Summary` nutzt dieselbe `FinanceRuleEngine` wie das zentrale Excel.
- `Management Analyse` bleibt Diagnose-/Plausibilitaetssicht, nicht fuehrende Finance-Zahl.
- `Management Analyse` hat zusaetzliche Finance-Reiter fuer Laender, Datenstatus, Abweichungen, Gutschriften-Kandidaten und Datenqualitaet.
- Filter fuer Jahr, Land und Waehrung wirken auf das Finance-Endergebnis.
- Standard-Ist bleibt inklusive Positionen; Intercompany/2nd-party wird separat ausgewiesen.

## Wichtige Regeln

- Hauswaehrung des Landessystems ist fuehrend.
- Wertbasis ist Nettofakturawert pro Position.
- Jahresabgrenzung ueber `PostingDate`, Fallback `InvoiceDate`, danach `ExtractionDate`.
- Gutschriften/Storno laufen als negative Beleg-/Positionszeilen.
- Budget-CHF ist Kontroll-/Reporting-Kandidat, nicht Standardabgleich.

## Offene Fachpunkte

- DE: Finance/Munir muss bestaetigen, welche Kundenlaender/Filter zum offiziellen DE-Ist gehoeren.
- IT: Nach neuem IT-Export pruefen, ob die vollstaendige `Trafag Italia`-Summe sichtbar wird.
- ES: Differenz zu Rhino/check.xlsx bleibt fachlich zu klaeren.

## Management-Analyse-Reiter

- `Finance Summary`: KPI-Karten und Summen wie im zentralen Excel.
- `Laender`: Ist, Soll, Differenz, Status, Quelle und TSC je Land/Waehrung.
- `Datenstatus`: Standortbestand, letzte Speicherung, letzter Export, Manual-Import-Hinweise.
- `Abweichungen`: Soll/Ist-Abweichungen sortiert nach Betrag.
- `Gutschriften`: technische Kandidaten ueber negative Werte und erkennbare Belegtypen/-nummern.
- `Datenqualitaet`: fehlende Materialnummern, ProductGroup, Waehrung, Kunde, Datum, Nullwerte und ausgeschlossene Zeilen.

## Land-Kurzindex

| Land | Kurzregel |
| --- | --- |
| CH/AT | SAP OData `ZSCHWEIZ`, Trennung ueber Buchungskreis/Reporting-Land |
| DE | Alphaplan Excel, `NettoPreisGesamtX`, 2025-Zwang |
| ES | Sage CSV, `ImporteNeto`, REC/Credit negativ |
| IT | Hauswaehrung, `Trafag Italia` ausgeschlossen, Duplikatlogik fuer leeres Supplier country |
| UK | Sage/Manual Excel, GBP, `[Sales Price/Value] * [Quantity]`, Credit Notes negativ |
| IN | INR als Hauswaehrung |

## Rohquellen Nur Bei Bedarf

- Entscheide: `docs/FINANCE_ENTSCHEIDE.md`, `entscheide.md`
- Formeln je Land: `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md`
- IT Detail: `docs/FINANCE_IT_VORGEHEN_2026-05-18.md`
- UK Korrektur: `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md`
- ES Detail: `SAGE_SPAIN_EXPORT_2026-05-05.md`
- alter Finance-Handoff: `docs/raw_md_archive/HISTORY_CANONICAL.md.raw`
