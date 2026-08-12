# UK / England Quelle - Korrektur

Stand: 2026-05-18

## Wichtige Korrektur

England / UK ist fachlich **Sage**, nicht SAP B1.

Der bisher verwendete Name `UK_B1` bezeichnet im Projektkontext den SharePoint-Ordner bzw. die bisherige technische Quellreferenz. Er darf nicht so verstanden werden, dass England ueber SAP Business One / B1-HANA gelesen wird.

## Korrekte Einordnung

| Punkt | Korrektur |
| --- | --- |
| Land | UK / England |
| TSC | `TRUK` |
| Fachliches Quellsystem | Sage |
| App-Anschluss | `MANUAL_EXCEL` / SharePoint-Datei oder SharePoint-Ordner |
| SharePoint-Ordnername | aktuell `Import/Finance/UK_B1` |
| Nicht korrekt | England als SAP B1 / HANA-B1 interpretieren |

## Konsequenz fuer den Finance-Abgleich

UK darf nicht mit den B1-Regeln von FR / IT verglichen werden.

Insbesondere:

- keine Annahme, dass `DocTotal`, `VatSum`, `OINV`, `INV1`, `ORIN`, `RIN1` fuer UK gelten;
- keine B1-Belegkopf-Deduplizierung als fachliche Standarderklaerung fuer UK;
- UK ist wie Spanien/Deutschland eher als manuelle Sage-/Excel-/CSV-Quelle zu behandeln;
- die Mapping-Regel wurde auf `SageNetSales([Sales Price/Value], [Quantity], [Document Type], [DocumentType], [Type])` umgestellt. Sie rechnet weiterhin Stueckpreis mal Menge, erzwingt Credit Notes aber negativ, sobald der Sage-Export einen Credit-/Abono-/Gutschrift-Typ liefert.

## Korrekte UK-Prueffragen

1. Ist der Sage-Export fuer das ganze Jahr 2025 vollstaendig?
2. Ist `Sales Price/Value` ein Stueckpreis oder bereits ein Positionswert?
3. Sind Credit Notes / Gutschriften enthalten und korrekt negativ?
4. Gibt es Discounts, Freight, Charges oder sonstige Sage-Felder, die Rhino einbezieht?
5. Gibt es 2nd-party-/Intercompany-Kunden, die ausgeschlossen oder separat gezeigt werden sollen?
6. Ist `GBP` die korrekte Vergleichswaehrung?

## Nachtrag 2026-05-18: Sage-Netto-Logik

Die Sage-Logik wurde gegen die Sage-Dokumentation geschaerft:

- Fuehrend ist Netto ohne VAT/MwSt.
- Invoices und Credit Notes werden gemeinsam summiert.
- Credit Notes muessen negativ in die Summe laufen.
- UK bleibt bei `invoice date`, solange kein separates Sage-Buchungsdatum im Export vorhanden ist.

Aktueller Re-Export nach der Anpassung:

| Kennzahl | Wert |
| --- | ---: |
| Zeilen 2025 | `1'880` |
| Ist | `3'533'710.09 GBP` |
| Soll LC | `3'538'972.00 GBP` |
| Differenz | `-5'261.91 GBP` |

Die Zahl blieb unveraendert, weil die vorhandenen UK-Zeilen bereits negative Betragszeilen enthalten. Die neue Regel verhindert aber, dass kuenftige Sage-Credit-Notes mit positivem Betrag versehentlich als Umsatz addiert werden.

Der fruehere `CheckValue 3'749'865.00` wird fuer UK nicht mehr verwendet. UK ist GBP-Local-Currency und wird gegen `LocalCurrencyValue = 3'538'972.00 GBP` geprueft.

## Formulierung fuer CFO / Finance

```text
UK / England wird fachlich als Sage-Quelle behandelt. Der vorhandene Ordnername UK_B1 ist nur eine technische SharePoint-Bezeichnung und bedeutet nicht, dass UK aus SAP B1 gelesen wird. Die UK-Abweichung ist deshalb ueber Sage-Exportvollstaendigkeit, Mapping, Gutschriften, Discounts/Freight/Charges und 2nd-party-Abgrenzung zu klaeren, nicht ueber B1-Belegkopf-Deduplizierung.
```
