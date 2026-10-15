# ZC23: neue Spalte Materialanteil

Stand: 2026-10-15. Analyse abgeschlossen, nur lesend in T76. Noch nichts gebaut. Wie es weitergeht, entscheidet Ingo.

## Wunsch (Gespräch Ingo mit dem Anfragenden, Aufnahme 2026-10-15)

ZC23 (`ZM_OFFENE_KDAUF`) zeigt heute den **Standardwert** = Standardpreis (MBEW-STPRS) × Menge, über alle Positionen
rund 57,8 Mio. CHF Herstellkosten bei rund 71 Mio. Umsatz. Neu soll daneben der **Materialanteil in CHF** stehen: der Teil
der Herstellkosten, der auf eingekauftes Material entfällt. Arbeit, Gemeinkosten und Zuschläge rechnet der Anfragende
selbst als Differenz zum Standardwert. Vorbild ist die Preisliste Tschechien (ZM03), die genau diese Aufteilung macht.

Beispiel aus dem Gespräch (Stahlmembran, Material B8…04, Kalkulationslosgrösse 1'200): CK11N 1,81 je Stück, davon
Material 138,47 / 1'200 = 0,12 und Arbeit 1,68.

## Transaktionen

| Transaktion | Programm | Bemerkung |
| --- | --- | --- |
| ZM03 | `ZLO_CALC_CZ_PREISE` | „Berechnung Werksabgabepreise CZ“, Quelltext in `ZLO_CALC_CZ_PREISE.abap` (T76, 2022 von IN23914) |
| CK11N | `SAPLCKDI` (SAP-Standard) | Erzeugniskalkulation anlegen. „ZK11N“ aus dem Auftrag gibt es in T76 nicht (TSTC geprüft), gemeint ist CK11N |

## Wie ZM03 rechnet

1. Je Material die jüngste Standardkalkulation: KEKO mit `KALKA = '01'`, `TVERS = 1`, `KADKY ≤ Stichtag`, nicht gelöscht,
   ohne Fehlerstatus `KF`.
2. Positionen der Kalkulation aus CKIS (`LEDNR = '00'`), Wert `WRTFW_KPF` für die Kalkulationslosgrösse:
   - **Typ M/I (Material):** Zukaufteil (`MARC-BESKZ = 'F'`), ohne eigene Kalkulation oder aus einem anderen Standort gilt
     als **Material**. Eigengefertigte Halbfabrikate werden **nicht** als Material gezählt, sondern rekursiv aufgelöst
     (Tabelle `KALTAB`, Faktor Menge / Losgrösse der Unterkalkulation). Abbruch bei mehr als 1'000 Unterkalkulationen.
   - **Typ E (Eigenleistung):** Arbeit, getrennt nach tschechischen Arbeitsplätzen (CRHD `ARBPL LIKE 'CZ%'`) und übrigen.
   - **Typ F (Fremdleistung):** eigene Spalte.
   - Andere Typen (z. B. Gemeinkostenzuschläge) werden nicht gezählt.
3. Materialwert = Summe Material über alle Stufen / Losgrösse = **Materialanteil je Stück**.

Damit ist die Falle „Halbfabrikat enthält schon Arbeit“ gelöst: Der Materialanteil ist reines Zukaufmaterial über
alle Stufen.

## Vorschlag für ZC23

- Neue Felder `MATANTEIL_STK` (Materialanteil je Stück) und `MATANTEIL` (= `MATANTEIL_STK` × `BMENG`), Spalte
  „Materialanteil“ in der ALV-Ausgabe neben `STANDARDWERT`.
- Rechnung wie ZM03, aber über **alle** Arbeitsplätze (keine CZ-Trennung nötig), Stichtag heute.
- **Je Material nur einmal rechnen** und zwischenspeichern: ZC23 hat bis rund 16'000 Positionen, aber viel weniger
  verschiedene Materialien. Die Auflösung kostet je Material mehrere Tabellenzugriffe.
- Einbau in die schnelle Fassung (`ZM_OFFENE_KDAUF_OPT`, Stand von Transport `T76K912728`), also erst nach oder zusammen
  mit dessen Import nach P76. Danach Vergleich gegen ZM03 an einigen Materialien (Beispiel B8…04: 0,12).

## Offene Fragen an Ingo bzw. den Anfragenden

1. Kalkulation: die jüngste wie ZM03, oder die zum freigegebenen Standardpreis gehörende (kann bei Vormerkungen
   abweichen)? Vorschlag: die freigegebene, damit Standardwert und Materialanteil zusammenpassen.
2. Fremdleistungen (Typ F): zum Material zählen oder zur Arbeit? ZM03 führt sie separat.
3. Soll die Spalte auch in `ZPOWERBI_KDAUF` und damit ins Dashboard?
