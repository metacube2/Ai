# ZLO03: „offene MK“ zu hoch, Abrufe mit gleicher Menge werden nur einmal abgezogen (B64344)

Stand: 2026-11-25 (Analyse, nichts geaendert; Fix wartet auf Freigabe Ingo). Anlass: Mail Armin 25.11. 11:03
„ZLO03 / offene Mengenkontraktmenge“: ZLO03 Top-Down VKNR 61227 zeigt fuer **B64344** offene MK **8'456** St
(Wert offene MK 164'046.4), ME3M zeigt offene Zielmenge **2'125** St.

## 1. Woher die Zahl kommt

`Z_ZLO03_TURBO2` rechnet die offene Kontraktmenge nicht selbst. Es liest `ZMD04_CALC-MKMNG` und `-OMKWR`
(`fill_ktab`, Zeile ~1891) und rechnet nur in die Stuecklisten-Einheit um (FIX 23, hier ST = ST). Die Tabelle fuellt der
Job **`Z_LO03_MD04_BATCH`**, FORM `calc_mkmng`. Quelltext aus T76/100 (Stand im Kopf 04.08.2026):
`Z_LO03_MD04_BATCH_T76_2026-11-25.abap`. Das Programm lag bisher nicht im Repo.

Rechnung je Kontraktposition: `KTMNG` minus Summe `EKAB-MENGE`, negativ = 0, Summe je Material. Mit Schalter
`p_ktgue` (Vorschlag an) zaehlen nur Kontrakte, die **heute gueltig** sind (`KDATB`/`KDATE`), ohne Loeschkennzeichen am Kopf;
Positionen ohne Loeschkennzeichen oder gesperrt (`S`), ohne Endlieferung, nicht statistisch.

## 2. Ursache

Die Abrufe werden so gelesen:

```abap
SELECT konnr, ktpnr, menge
  FROM ekab
  INTO TABLE @gt_ekab_cache
  FOR ALL ENTRIES IN @gt_ekpo_cache
  WHERE konnr = @gt_ekpo_cache-ebeln
    AND ktpnr = @gt_ekpo_cache-ebelp.
```

`FOR ALL ENTRIES` entfernt gleiche Ergebniszeilen. Die Feldliste enthaelt den Abrufbeleg (`EBELN`, `EBELP`) nicht,
also sind zwei Abrufe desselben Kontrakts mit gleicher Menge fuer die Datenbank dieselbe Zeile. **Jede Abrufmenge wird je
Kontraktposition nur einmal abgezogen.** Bei festen Losgroessen (hier 576 St) bleibt fast der ganze Kontrakt „offen“.

Das Gateway-Set `EinkKontraktSet` (Dashboard, „Offener Kontraktwert“) hat den Fehler **nicht**: es liest `ebeln ebelp`
mit, im Quelltext steht dazu ein Kommentar (`saptasks/copa_verkauf/DPC_EXT_GET_ENTITYSET_neu_2026-11-13.abap` Zeile 353).

## 3. Nachgerechnet

Kontraktpositionen B64344, Werk 1100: 21, davon 18 mit Loeschkennzeichen `L` (T76 `EKPO` = Armins ME3M-Liste). Es zaehlen
nur drei. Abrufe in T76 `EKAB` (T76 ist ein aelterer Abzug: 46010268 noch ohne Abrufe, 46009844 mit Zielmenge 5'000):

| Kontrakt | gueltig bis | Abrufe | Zielmenge / abgerufen (P76, ME3M Armin) | offen richtig | offen mit Fehler |
| --- | --- | --- | --- | --- | --- |
| 46009174/10 | 31.12.2025 | 9 x 576, 1 x 811 | 6'000 / 5'995 | 5 | faellt wegen Gueltigkeit weg |
| 46009844/10 | 30.11.2026 | 8 x 576, 1 x 392 (P76: 371), 2 x 0 geloescht | 4'979 / 4'979 | 0 | 4'979 − (576 + 371 + 0) = **4'032** |
| 46010268/10 | 30.06.2027 | P76: 2'880 = 5 x 576 | 5'000 / 2'880 | 2'120 | 5'000 − 576 = **4'424** |

4'032 + 4'424 = **8'456**, genau Armins Wert aus ZLO03. Richtig waeren 2'120 (mit Gueltigkeitsfilter) bzw. 2'125 (ME3M,
inklusive der 5 St des abgelaufenen Kontrakts 46009174).

**Was belegt ist und was nicht:** Quelltext und `EKPO`/`EKAB`/`EKKO` sind aus **T76** gelesen (RFC, nur lesend). Die
P76-Zahlen stammen aus Armins Bildschirmfoto. P76 selbst ist **nicht** gelesen: dass dort dieselbe Programmfassung laeuft,
der Job mit `p_ktgue` an laeuft und 46010268 fuenf Abrufe zu 576 hat, ist aus der stueckgenauen Uebereinstimmung
geschlossen. Der Job ist in T76 noch nicht testweise gelaufen; erwartet fuer B64344 in T76 heute: mit Fehler 9'032
(4'032 + 5'000), richtig 5'000.

`ZMD04_CALC` in T76 fuer B64344: `MKMNG` 1'392, `OMKWR` 27'297.12, berechnet 02.12.2025 09:36. Der Wert passt weder zur
heutigen T76-Datenlage noch zum Fehlerbild und ist nicht erklaert (der Job laeuft in T76 nicht regelmaessig).

## 4. Tragweite

- Betroffen ist jedes Material, dessen Kontrakt mindestens zwei Abrufe mit gleicher Menge hat. Die Abweichung geht immer
  nach oben („offene MK“ und „Wert offene MK“ zu hoch). Anzahl betroffener Materialien: nicht gezaehlt.
- Folge fuer den Phase-Out: Die Liste zeigt Kontraktmengen als offen, die laengst abgerufen sind.
- Dashboard: Die Kachel „Offener Kontraktwert“ ist nicht betroffen (eigene Rechnung, siehe 2). `Mkmng`/`Omkwr` aus
  `ZloTopDownSet` werden im Dashboard gespeichert (`MaterialUsageDataRefreshService`); eine Anzeige habe ich nicht gefunden.

## 5. Gleiches Muster an drei weiteren Stellen desselben Jobs (nicht nachgerechnet)

| Stelle | Feldliste | fehlendes Schluesselfeld | moegliche Wirkung |
| --- | --- | --- | --- |
| `MDBS` (offene Bestellungen, `OMENG`) | matnr, ebeln, ebelp, menge, wemng, umrez, umren | `ETENR` | zwei Einteilungen einer Position mit gleicher Menge und gleichem Wareneingang zaehlen einmal: offene Bestellmenge zu tief |
| `MSEG`/`MKPF` (Verbrauch Kundeneinzelbestand) | mblnr, mjahr, matnr, werks, bwart, shkzg, menge | `ZEILE` | zwei Belegzeilen gleicher Menge im selben Beleg zaehlen einmal: Verbrauch zu tief |
| `AFPO` (offene Fertigungsauftraege) | matnr, aufnr, psmng, wemng | `POSNR` | nur bei mehreren Positionen je Auftrag, selten |

Ob diese drei in den Daten tatsaechlich Abweichungen erzeugen, ist offen. Sie werden nicht stillschweigend mitgefixt.

## 6. Vorschlag Fix (wartet auf Freigabe)

1. **Fix A (Armins Fall):** in `Z_LO03_MD04_BATCH` Typ `ty_ekab` und die `EKAB`-Feldliste um `ebeln`, `ebelp` ergaenzen.
   Zwei Zeilen. `calc_mkmng` bleibt unveraendert.
2. Test in T76: Job mit `s_matnr = B64344`, Testlauf (`p_test`), vorher und nachher; erwartet 9'032 -> 5'000.
   Danach Stichprobe mit weiteren Materialien gegen ME3M/ME3L.
3. Eigener Transport, Freigabe und Import nach P76 durch Ingo. Wirksam in ZLO03 nach dem naechsten Lauf des Jobs.
4. **Entscheid Armin:** Soll ein abgelaufener, nicht geloeschter Kontrakt mit Restmenge mitzaehlen (wie ME3M, hier 5 St
   aus 46009174) oder nur heute gueltige (heutiges Verhalten des Jobs mit `p_ktgue`)?
5. **Entscheid Ingo:** die drei Stellen aus Abschnitt 5 im selben Transport mitnehmen (aendert `OMENG` und Verbrauch bei
   betroffenen Materialien) oder zuerst zaehlen, wie viele Materialien es trifft.
