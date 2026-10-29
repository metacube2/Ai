# ZLO03 FIX 23: Mengen und Stückkosten in der Einheit der Stückliste (2026-10-29)

Stand: 2026-10-29. T76 aktiv, Auftrag `T76K912781`, nicht freigegeben.

## 1. Meldung

Armin, 29.10.: „ZLO03: VerkNr. 41019; Komponente H05039 = 1 Pack à CHF 7.40 StdPr.; wenn ich nun die Bedarfsmenge * StdPreis
rechne bekomme ich zu hohe Werte. Der StdPr sollte immer pro 1 St. sein sofern der Bezug pro St. läuft.“

## 2. Befund (T76 per RFC gelesen)

| Quelle | Wert |
| --- | --- |
| `MARA` H05039 | Basismengeneinheit **KAR**, HALB |
| `MARM` H05039 | ST: UMREZ 1 / UMREN 600, also 1 KAR = 600 ST |
| `MBEW` 1100 | VPRSV S, STPRS 8.48 je KAR, PEINH 1 (P76 laut Armin 7.40) |
| `STPO`, `ZPOWERBI_VC_TXT` | Position in **ST** (z. B. 1 ST je Kopfmaterial) |

Ursache: ZLO03 rechnete die Stücklistenmenge mit `UNIT_CONVERSION_SIMPLE` von ST in die Basiseinheit KAR um. Der Baustein kennt
nur allgemeine Einheiten (T006), nicht die materialbezogene Umrechnung aus MARM; ST und KAR sind beide einheitenlos und wurden
1:1 umgesetzt. Die Bedarfsmenge stand damit zahlenmässig in ST, die Stückkosten aber je KAR, Lager und Bedarfsplanung (MD04)
in KAR. Bedarfsmenge × Stückkosten war bei H05039 600-mal zu hoch.

## 3. Umsetzung (Entscheid Ingo: alles in Stücklisten-Einheit)

Quelltext `Z_ZLO03_TURBO2_FIX23_EINHEIT_2026-10-29.abap` (Vorstand FIX 22, in T76 vorher gegengelesen: identisch). Top-Down und, auf Wunsch Ingo vom selben Tag, Bottom-Up.

- `load_bom_units`: je Komponente die häufigste Einheit in `ZPOWERBI_VC_TXT`; weicht sie von der Basiseinheit ab und kennt MARM
  sie, ist sie die Zieleinheit (UMREZ/UMREN gespeichert, kein gerundeter Faktor).
- `bom_unit_menge`: Stücklistenmenge bleibt in der Zieleinheit; eine Zeile in einer anderen Alternativeinheit geht über die
  Basiseinheit; unbekannte Einheit wie bisher allgemein.
- `apply_bom_unit` (nur Ausgabe): Verbrauch, Lagerbestand, Zugänge, Abgänge, Bewertungsbestand, offene Menge, MK, Endbestand,
  Mindestbestand, feste Losgrösse und Sicherheitsbestand × UMREN / UMREZ; Stückkosten aus dem ungerundeten Preis
  (STPRS bzw. VERPR / PEINH) × UMREZ / UMREN mit bis zu 6 Nachkommastellen; Spalte Mengeneinheit zeigt die Zieleinheit.
  Werte in CHF bleiben unverändert. Komponenten ohne abweichende Einheit sind nicht betroffen.
- **Bottom-Up:** Zeilen sind dort die Kopfmaterialien mit eigenen Stammdaten (stimmig in ihrer Einheit); die Spalte der
  gesuchten Komponente bekommt die Stücklistenmenge über MARM und in der Einheitenzeile die Stücklisten-Einheit statt der
  Basiseinheit (vorher „KAR“ über Mengen in ST, auch in der Summenzeile).

## 4. Einspielen und Test (eigene Sitzung T76/100)

| Schritt | Ergebnis |
| --- | --- |
| Auftrag | `T76K912777` (FIX 22) war schon freigegeben; neuer Workbench-Auftrag **`T76K912781`** „ZLO03 FIX 23 Top-Down in Stuecklisten-Einheit“ (der erste, längere Text scheiterte an 60 Zeichen) |
| Aktivieren | nur `Z_ZLO03_TURBO2`, „Objekt(e) wurde(n) aktiviert“ |
| Rückgelesen | identisch mit der Patchdatei (4'019 Zeilen, 13 Stellen `FIX 23`) |
| Nachtrag Bottom-Up | im selben Auftrag gesichert und aktiviert, rückgelesen identisch (4'021 Zeilen); Lauf Bottom-Up `H05039`: 988 × 36, Einheitenzeile der Spalte H05039 ST, Mengen je Kopfmaterial in ST (40731 = 1, 41019 = 3), Summenzeile über 985 Kopfmaterialien |
| Lauf Top-Down Material `41019` | 97 × 139; H05039: Mengeneinheit ST, Stückkosten 0.014133 (= 8.48 / 600), Lagerbestand 4'659.6 ST × 0.014133 = 65.85 ≈ Wert Endbestand 65.86 CHF, feste Losgrösse 12'000 ST (20 KAR), Sicherheitsbestand 1'800 ST (3 KAR), Bedarf 3 ST je Gerät |

## 5. Offen

1. Freigabe und Import `T76K912781` nach P76 (Ingo), nach `T76K912742`, `T76K912752` und `T76K912777`.
2. Rückmeldung an Armin; in P76 ergibt H05039 dann 7.40 / 600 = 0.012333 CHF je ST.
3. ~~Bottom-Up ist unverändert.~~ *Erledigt 2026-10-29: Komponentenspalte im Bottom-Up ebenfalls in Stücklisten-Einheit.*
