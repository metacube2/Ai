# Finance Dashboard - Kurzmemo fuer Andreas

Stand: 2026-06-01

## Aktueller Stand

- `Finance Summary` ist die fuehrende Sicht fuer Soll/Ist.
- `Management Analyse` ist die Diagnoseebene fuer Laender, Datenstatus, Abweichungen, Gutschriften, Datenqualitaet, Spartenanalyse und Rohdaten.
- Das Dashboard ist technisch produktiv nutzbar.
- Letzter dokumentierter Testlauf: `80/80` Tests gruen.
- Standard-Ist bleibt inklusive aller Positionen.
- Intercompany / 2nd-party wird separat ausgewiesen, aber nicht automatisch herausgerechnet.

## Sitzungsergebnis 2026-06-01

- Spanien hat laut Sitzung keine echte Ist-Abweichung.
- ES-Ist `3'082'320.18 EUR` ist fachlich plausibel.
- Der bisherige ES-Sollwert `3'102'333.61 EUR` war falsch bzw. wahrscheinlich ein Excel-/Referenzfehler.
- ES-Referenz 2025 ist technisch auf `3'082'320.18 EUR` korrigiert.
- Intercompany ist in einzelnen Standortzahlen anscheinend bereits bereinigt, muss aber pro Standort bestaetigt werden.
- `Management Analyse > Laender` zeigt nun IC/2nd-party und `Ist ohne IC` als Diagnose.
- Bei den 2025-Wechselkursen ist das Anwendungsdatum jetzt in den Settings konfigurierbar.
- In der Sparten-Finanzanalyse sind mehr als 90% nicht zugeordnet; Andreas sagt, das kann fachlich nicht stimmen.
- Der Materialabgleich normalisiert jetzt fuehrende Nullen und zeigt bei >=90% ungeklaerter Spartenabdeckung einen Warnhinweis.

## Fuehrende Regeln

| Thema | Regel |
| --- | --- |
| Vergleich | Je Land in Hauswaehrung |
| Wertbasis | Nettofakturawert pro Position |
| Jahresabgrenzung | `PostingDate`, sonst `InvoiceDate`, sonst `ExtractionDate` |
| Gutschriften / Storno | Negative Beleg-/Positionszeilen |
| CHF | Reporting-/Kontrollsicht, nicht Standardvergleich |
| Intercompany | Separat ausweisen, nicht still entfernen |

## Wichtig fuer die Diskussion

### 1. Lokaler Soll/Ist zuerst in Hauswaehrung

Beispiele:

- UK in `GBP`
- Indien in `INR`
- USA in `USD`
- EUR-Laender in `EUR`

Erst wenn die lokale Zahl stimmt, ist eine konsolidierte CHF-Sicht sinnvoll.

### 2. CHF als separate Management-Sicht

Offen ist der offizielle Kurstyp:

- Budgetkurs
- Monatskurs
- Transaktionskurs aus ERP
- Konzern-/Treasury-Kurs
- Stichtagskurs

Zusaetzlich offen:

- Auf welches Datum soll der Kurs fachlich final angewendet werden?
- `DocDate`?
- `PostingDate`?
- `InvoiceDate`?
- anderes Periodendatum?

Ohne offiziellen Kurstyp ist eine CHF-Zahl technisch berechenbar, aber fachlich nicht sauber verteidigbar.

Umsetzung: In den Settings gibt es `Wechselkurse anwenden auf`; die Rohdaten-Diagnose zeigt das verwendete Kursdatum an.

### 3. Kosten nicht mit Umsatzfreigabe vermischen

Kosten / Marge sollten als separate Ausbaustufe behandelt werden.

Zu klaeren:

- Standardkosten?
- Ist-Kosten?
- Group Cost?
- Budgetkosten?
- Finance-Kostentabelle?

Solange die Kostenquelle nicht freigegeben ist, sollte keine offizielle Marge ausgewiesen werden.

## Offene Laenderpunkte

| Land | Offener Punkt |
| --- | --- |
| DE | Welche Kundenlaender / Filter gehoeren offiziell zum deutschen Ist? |
| ES | Keine echte Ist-Abweichung laut Sitzung; Sollwert technisch auf `3'082'320.18 EUR` korrigiert |
| UK | Sage-Differenz ca. `-5.3k GBP`; Discounts, Freight, Charges und 2nd-party klaeren |
| IT | Fachliche Methode dokumentiert; neuer Export und finale Abgrenzung pruefen |
| CH / AT | Klaeren, ob `FKDAT` als Periodendatum akzeptiert ist |

## Offene Strukturpunkte

| Thema | Punkt |
| --- | --- |
| Intercompany | Pro Standort klaeren, ob IC bereits in der Quelle herausgerechnet ist; Dashboard zeigt IC-Diagnose |
| Wechselkurse | Kursanwendungsdatum ist konfigurierbar; fachliche Finalfreigabe fehlt |
| Spartenanalyse | >90% nicht zugeordnet ist fachlich unplausibel; Mapping / TR-AG-Referenz trotz technischer Normalisierung pruefen |

## Entscheidbedarf von Finance

Finance sollte pro Land bestaetigen:

- Quelle
- Datum
- Wertfeld
- Waehrung
- Filter
- Intercompany-Behandlung

Zusaetzlich braucht es Entscheide zu:

- offiziellem CHF-Kurstyp
- Datumsfeld fuer CHF-Kursanwendung
- Kurstabelle / Kursquelle
- Kostenumfang im Dashboard
- Behandlung kleiner Restabweichungen
- Korrektur ES-Sollwert
- Pruefung der Sparten-Zuordnung

## Kernaussage

Das technische Fundament steht. Die wichtigsten naechsten Punkte sind Referenzkorrekturen, fachliche Abgrenzungen und Mapping-Pruefungen, nicht primaer technische Grundlagenprobleme.
