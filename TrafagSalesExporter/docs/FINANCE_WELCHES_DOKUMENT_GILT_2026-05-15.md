# Finance: Welches Dokument gilt?

Stand: 2026-05-15

## Fuehrendes Dokument

Fuer den CFO-/Finance-Termin gilt:

```text
docs/FINANCE_CHEF_SUMMARY_2026-05-15.docx
```

Dieses Dokument ist die aktuellste CFO-Kurzfassung mit Ampel, Laendertabelle, Pruefquellen, Prioritaeten und empfohlenen Massnahmen.

## Geloeschte alte Fassung

Die alte Fassung `docs/FINANCE_CHEF_SUMMARY_2026-05-13.docx` wurde entfernt, weil sie durch die Version vom 2026-05-15 ersetzt ist.

## Entscheidbasis

Die fachlichen Entscheide stehen separat hier:

```text
entscheide.md
docs/FINANCE_ENTSCHEIDE.md
```

Kurzfassung der wichtigsten Entscheide:

| Thema | Entscheid |
| --- | --- |
| Fuehrende Waehrung | Hauswaehrung je Land |
| CHF-Sicht | Nur separat mit Budgetkursen |
| Aggregation | Pro Artikel bzw. Belegposition |
| Wertbasis | Nettofakturawert |
| Jahresabgrenzung 2025 | Buchungsdatum |
| Gutschriften / Storno | Separat ausweisen, positionsbasiert behandeln |
| Intercompany / 2nd-party | Separat klassifizieren und als Auswahl/Sicht fuehren |
| Indien | INR ist fuehrend |
| Italien | Hauswaehrung, Intercompany separat pruefen |

## Wichtig fuer Rueckfragen

Falls im Termin gefragt wird, ob die Berechnungslogik schon entschieden ist:

> Ja. Die Grundlogik ist entschieden: Hauswaehrung, Nettofakturawert, Buchungsdatum und Berechnung pro Belegposition. Offen sind vor allem Datenvollstaendigkeit, Intercompany-Abgrenzung, Budgetkursquelle und die fachliche Freigabe einzelner Laenderabweichungen.

## Was noch nicht final ist

| Thema | Status |
| --- | --- |
| IT | Kritisch; grosse Abweichung, Berechnungsart/IC/Deduplizierung pruefen |
| UK / EN | Hoch; Restdifferenz und Jahresvollstaendigkeit pruefen |
| DE | Hoch; finaler Jahresfile fehlt, Sample nicht verwenden |
| CH / AT | Hoch; Sollzuordnung und Trennung finalisieren |
| ES / AT | Mittel; kleinere Differenzen klaeren |
| FR / IN / US | Rechnerisch freigabefaehig |
