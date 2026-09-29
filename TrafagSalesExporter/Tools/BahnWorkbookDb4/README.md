# Bahn-Excel um Rechnungs-Rohertrag ergaenzen

Das Werkzeug erweitert die bestehende deutsche Bahn-Pruefmappe um die Belegkopfwerte
`Netto (SW)` und `RohertragEndSumme` aus Rohails Rechnungsliste. Die Bahnzuordnung kommt
ueber die fachliche Adressnummer und die reine Bahnbranche aus dem Alphaplan-Adressstamm.

```powershell
dotnet run --project Tools/BahnWorkbookDb4 -c Release -- `
  bahn.xlsx "Export_Adressen_20260908 (2).xlsx" Rechnungen_20260909.xlsx
```

Der Rohertrag bleibt sichtbar als fachlich noch nicht bestaetigter DB4-Kandidat. Die
Rechnungsliste enthaelt keine Gutschriften, und beide Quellen decken nur Deutschland ab.
Diese Grenzen stehen auch im Blatt `DB4_Hinweis` der erzeugten Mappe.
