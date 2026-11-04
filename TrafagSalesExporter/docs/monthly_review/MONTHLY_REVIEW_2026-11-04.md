# Verkauf: Monthly Review Gesellschaften (Auftrag 2026-11-04)

Stand: 2026-11-04 13:10. **Nicht gebaut, pausiert.** Nur Auftrag, Entscheide, Spezifikation und Befunde; kein Code, kein Deploy.

## Auftrag und Entscheide

Ingo am 2026-11-04: „kannst du eine zusaetzliche Maske im Verkauf einbauen? Hier der Draft. maske.html“.
Entscheide (AskUserQuestion, 04.11.):

| Frage | Entscheid |
| --- | --- |
| Statische Kopie oder Nachbau? | **Gleich live nachbauen** (Blazor, Zahlen aus den vorhandenen Diensten statt aus dem Datenblock der Datei) |
| Sichtbarkeit | **Wie Verkauf: alle** (ohne Passwort, wie der Reiter Verkauf seit 2026-10-02) |

Folgt auf `docs/VERKAUF_2026-10-02.md`. Dort steht „Marge nicht im Reiter Verkauf“; fuer die Monthly Review gilt das nicht mehr,
sie zeigt Margen je Gesellschaft (Phase 2) aus der Gruppenmarge.

## Unterlagen

| Datei | Inhalt |
| --- | --- |
| `docs/monthly_review/maske_entwurf_2026-11-04.html` | Ingos Entwurf, Kopie von `maske.html` aus der Repo-Wurzel (204 KB, 3'164 Zeilen, Datenblock Stand Oktober 2026) |
| `docs/monthly_review/MASKE_SPEZIFIKATION_2026-11-04.md` | Vollstaendige Spezifikation des Entwurfs (742 Zeilen): Datenmodell, drei Ansichten `review`/`gaps`/`pbi`, Berechnungen mit Erwartungswerten, Diagramme, alle deutschen Texte, Abschnitt 6 mit hart codierten Annahmen und Fehlern des Entwurfs |

## Geplanter Bau (drei Phasen)

1. **Phase 1, live aus dem internen BI:** neuer Unterreiter im Verkauf (Route unter `verkauf/`, Eintrag in
   `Components/Sales/SalesTabs.razor`), eigener CSS-Block `mrv-`, Ansicht je Gesellschaft und Gruppe aus
   `SalesAnalytics.Monthly`, `.Forecast`, `.Customers`, `.Movements` und `ControllingAnalytics.Projection`.
   Vorgesehene Dateien laut Koordination: `Components/Sales/MonthlyReview/`, `Services/Sales/MonthlyReview*.cs`,
   Tests `TrafagSalesExporter.Tests/MonthlyReview*`, Uebersetzungen.
2. **Phase 2, Margen live:** DB je Gesellschaft aus `GroupMarginCalculator`.
3. **Phase 3, Quellen ausserhalb des BI:** TSC-Gesellschaften, Power BI (Sales Cockpit, RMA), Budget, Auftragseingang
   und die Lueckenliste „gaps“ zunaechst aus einer manuell gepflegten Datei.

Vor dem Bau Abschnitt 6 der Spezifikation lesen: der Entwurf rechnet mit festen Monaten (Oktober) und hat einzelne
Rechenfehler, die nicht nachgebaut werden duerfen.

## Befund: T&P-Dateien der Tochtergesellschaften (Frage Ingo 04.11.)

Frage: „Sind die T&P-Files der Tochtergesellschaften auch im Data Lake?“ Antwort nach Doku und Code (nur gelesen):

- **Nein, nicht im Dashboard bzw. internen BI.** Verkauf und Finance lesen nur TRCH, TRIN, TRIT, TRUS, TRUK, TRES, TRAT,
  TRDE, TRFR. Fuer die TSC-Gesellschaften TRJP, TRCZ, M/S, M/S A und GFS gibt es keinen Import; GFS kommt nur als fester
  Finance-Vergleichswert 2025 (`DatabaseSeedService`) und in der Erkennung interner Lieferanten (Gruppenmarge) vor.
- Der Entwurf selbst kennzeichnet sie als „Werte aus der TSC-Datei (Excel), nicht im internen BI“, Luecke **G17**
  („Zahlen kommen aus Excel – Anbindung ans BI klaeren“).
- **Offen:** ob Andreas' groesseres Data-Lake-Vorhaben (`docs/SITZUNG_ABSCHLUSSRUNDE_DASHBOARD_2026-11-03.md`) sie schon
  sammelt, und wo die T&P-Dateien liegen (z. B. SharePoint). Mit dem Ablageort liesse sich ein automatischer Import pruefen
  (Phase 3).

## Naechster Schritt

Phase 1 bauen, wenn Ingo es wieder aufnimmt. Wochen_Todo Zeile 150.
