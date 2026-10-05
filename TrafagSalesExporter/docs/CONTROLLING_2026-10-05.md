# Unterreiter Controlling im Finance Cockpit

Stand: 2026-10-05, Code `cb08f18`, **produktiv 08:18**, von mir per Edge headless angesehen. Route `/finance-cockpit/controlling`,
Menü Finance Cockpit > Controlling.

## Herkunft

**Eigeninitiative, kein Auftraggeber.** Ingo fragte am 2026-10-05, ob es im Finance Cockpit Controlling gibt und ob das
Bisherige Controlling ist. Antwort: Datenbasis und Vertriebs-/Margencontrolling ja, klassisches Controlling (Budget,
Kostenstellen, Erfolgsrechnung, Forecast gegen Budget, Working Capital) nein. Auf die Frage, was mit den angebundenen
Daten ohne viele offene Fragen geht, baute ich die zwei sauberen Teile. Ingo: „bau das ein aber in neuem unterreiter
controlling, offene punkt aber bei fragen separat führen weil task in eigeninitiative eingebaut wurde kein auftraggeber“.

Folge: Die offenen Punkte stehen **nur hier und auf der Seite**, nicht im Issue-Log und keiner Person (auch nicht
Andreas) als Aufgabe zugeteilt.

| Frage | Entscheid |
| --- | --- |
| Zugang | ohne Finance-Passwort, wie der Reiter Verkauf (gleiche Summen je Gesellschaft dort schon offen). Ingo hat die Frage nicht beantwortet; bei Bedarf in `Components/Routes.razor` ergänzen |
| Datenbasis | dieselben Verkaufszeilen wie der Reiter Verkauf (`SalesDataService`: Finance-Regeln, ohne Konzernkunden, CHF zum Belegdatum), ergänzt um Wert in Lokalwährung und Währung |
| Zeiträume | Vergleich gleicher Zeiträume innerhalb der Daten (Daten ab 01.2025), wie im Verkauf |

## Was gebaut ist

| Teil | Rechnung (`ControllingAnalytics`) |
| --- | --- |
| **Umsatzbrücke gegen Vorjahr** (Wasserfall, Sicht Gesamt, je Gesellschaft, je Sparte; Tabelle je Gesellschaft) | je Gesellschaft und Artikel in Lokalwährung. **Menge** = Stückveränderung mal Durchschnittspreis Vorjahr; **Mix** = Artikelmengeneffekt minus Menge; **Preis** = Preisveränderung je Artikel mal aktuelle Menge zum Vorjahreskurs; **Neue/weggefallene Artikel** = nur in einem Zeitraum verkauft; **Währung** = aktueller Lokalumsatz mal Kursveränderung (Kurs je Währung und Zeitraum = CHF-Summe / Lokalsumme); **Übrige** = Rest (Zeilen ohne Stück, Gutschriften ohne Menge, Rundung). Die Summe aller Effekte ist genau die Veränderung |
| **Hochrechnung laufendes Jahr** je Gesellschaft und gesamt, Monatskurve gegen Vorjahr | Ist bis zum letzten vollständigen Monat plus Restmonate aus gleichem Monat im Vorjahr mal Wachstum seit Jahresbeginn (0.5 bis 2); verglichen mit dem vollen Vorjahr, wenn die Daten es ganz enthalten. Kein Budget |

Excel-Export mit Brücke je Gesellschaft und Hochrechnung. Tests `ControllingAnalyticsTests` (6).

## Offene Fragen (Eigeninitiative, keiner Person zugeteilt)

| Teil | Warum nicht gebaut |
| --- | --- |
| Debitoren und Überfälligkeit | Journal mit Fälligkeit und Ausgleich nur für FR, IT, US, IN, ohne Kundenfeld; welche Konten je Land Debitoren sind, ist nicht festgelegt |
| Vorräte und Lagerumschlag | Lagerwert nur CH und nur Einkaufsteile (Reiter Einkauf); passt nicht sauber zu den Warenkosten aus dem Verkauf |
| Margenentwicklung | mit der Gruppenmarge rechenbar, aber CHF-Kurs (`ISS-018.1`) und Kostenbasis nicht überall geklärt |
| Erfolgsrechnung je Gesellschaft | Konzernkonto-Zuordnung fehlt; DE, UK, ES ohne Buchhaltungsdaten; Journal CH/AT erst nach einem Ladelauf |
| Kostenstellen und Budget | Kostenstelle in allen Quellen leer; kein Budget, nur Umsatz-Sollwerte 2025 |

## Erste produktive Werte (2026-10-05 08:20)

- 01–09.2026 gegen 01–09.2025: **+16.0 Mio CHF** (73.6 → 89.6 Mio). Brücke gesamt: Menge +16.1, Mix −5.5, Preis −0.9,
  neue Artikel +23.6, weggefallene Artikel −15.5, Währung −2.0, Übrige +0.2 Mio.
- Hochrechnung 2026: **121.0 Mio** gegen Ist 2025 99.4 Mio (+22 %).
- **Befund:** Die grossen Blöcke „neue“ und „weggefallene Artikel“ (TRCH +14.6 / −9.5 Mio) zeigen, dass viele Umsätze
  auf Artikelnummern laufen, die nur in einem der beiden Zeiträume vorkommen (Varianten, Konfigurationen). Preis- und
  Mengeneffekt gelten nur für die Artikel, die in beiden Zeiträumen verkauft wurden. Zum Nachfragen bei Bedarf, nicht
  zugeteilt.

## Grenzen

- Mengen werden innerhalb einer Gesellschaft über alle Artikel summiert (Menge/Mix-Trennung ist dadurch grob,
  wie üblich bei Stückmischungen).
- Ein Artikel, der im Vorjahr nur mit Gutschrift oder ohne Menge vorkam, landet in „Übrige“.
- Hochrechnung ist eine Rechnung, keine Planung; Wachstum gilt für alle Restmonate gleich.
