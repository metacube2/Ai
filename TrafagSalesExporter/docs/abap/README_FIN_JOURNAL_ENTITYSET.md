# Journal-EntitySet fuer CH/AT: Messung und Bauplan

Stand: 2026-09-10. System `T76/100` (travt762), das ist eine rund sechs Monate alte
Kopie der Produktion.

Zugehoerig: `docs/FINANCE_JOURNAL.md` (Feld-Mapping und Leser),
`saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md` (Werkzeuge und Fallen).

## 1. Warum es das braucht

Der Finance-Journalleser fuer CH/AT ist seit dem 2026-09-08 produktiv. Es fehlt nur die
OData-Quelle: keines der vorhandenen EntitySets taugt als Volljournal.

| EntitySet | warum nicht |
| --- | --- |
| `FinanzdataSchweizOeSet` | Faktura mit `Vbeln`, `Posnr`, `Matnr` — keine Buchhaltung |
| `bkpfSet` | nur Belegkoepfe, ohne `Buzei` kein Journal |
| `bsisSet` | nur **offene** Posten, damit stimmt kein Saldo; ohne `Dmbtr`, `Shkzg`, Kontotext, Profitcenter, Buchungstext, Faelligkeitsdatum |

## 2. Fachliche Vorgabe Andreas, 2026-09-10

> „ch hat viele line items mit zeitbuchungen sind es ueber 5 mio pro jahr. da ch aehnlich
> zu at laufen wird, wuerde ich fuer den ersten entwurf ch weglassen (oder potentiell ohne
> zeitbuchungen umsetzen)"

Umgesetzt: **zuerst nur Oesterreich, Buchungskreis 1200.** Der Pruefreport hat `1200` als
Vorgabe und laesst Belegart `CO` standardmaessig weg.

## 3. Gemessene Grundlagen

Alles rein lesend, ueber SapProbe (RFC) und den Report `ZFIN_JOURNAL_PRUEFUNG`.

### Buchungskreise

| BUKRS | Gesellschaft | Land | Hauswaehrung | Kontenplan |
| --- | --- | --- | --- | --- |
| 1100 | Trafag AG | CH | **CHF** | 1000 |
| 1200 | Trafag Ges.m.b.H. | AT | **EUR** | 1000 |

Zwei verschiedene Hauswaehrungen. Der Leser bildet das ueber `Hwaer` ab.
Bestaetigt aus dem Quelltext von `Z_TRAFAG_DACH_EXPORT`: „Buchungskreise 1100 (CH) und
1200 (AT)".

### DDIC-Felder

Alle 25 benoetigten Felder existieren (`field-exists` je Feld): `BKPF` mit `BUKRS`,
`BELNR`, `GJAHR`, `BUDAT`, `MONAT`, `BLART`, `XBLNR`, `STBLG`, `HWAER`, `WAERS`; `BSEG`
mit `BUZEI`, `HKONT`, `SHKZG`, `DMBTR`, `WRBTR`, `KOSTL`, `PRCTR`, `SGTXT`, `ZFBDT`,
`AUGDT`, `AUGBL`; `SKAT` mit `SAKNR`, `TXT50`, `SPRAS`, `KTOPL`.

### Mengengeruest Oesterreich, 2025 bis 2026, ohne CO-Belege

| | |
| --- | ---: |
| Belegkoepfe | 6'275 |
| Belegpositionen | 17'364 |
| Soll / Haben | 8'608 / 8'756 |

Belegarten: `RV` 1'207, `KR` 1'175, `WL` 1'179, `WA` 1'068, `DZ` 827, `KZ` 306, `SA` 267,
`AB` 110, `PR` 51, `KG` 31, `WE` 21, `RE` 18, `WI` 6, `DA` 4, `KA` 4, `DG` 1.

**Das ist eine handhabbare Menge.** Andreas' Sorge betrifft die Schweiz, nicht Oesterreich.

### Fuellgrade, 17'364 Positionen

| Feld | gefuellt | Anteil | Bedeutung fuer das Dashboard |
| --- | ---: | ---: | --- |
| `WRBTR` | 17'108 | 98,5 % | Betrag in Transaktionswaehrung, unkritisch |
| `SGTXT` | 3'903 | 22,5 % | Buchungstext, oft leer — normal |
| `AUGDT` | 3'844 | **22,1 %** | Ausgleichsdatum |
| `AUGBL` | 3'844 | **22,1 %** | Ausgleichsbeleg, identischer Fuellgrad |
| `ZFBDT` | 3'604 | **20,8 %** | Basis fuer das Faelligkeitsdatum |
| `KOSTL` | 2'360 | 13,6 % | Kostenstelle |
| `ZBD1T` | 2'257 | 13,0 % | Zahlungsziel in Tagen |
| **`PRCTR`** | **0** | **0,0 %** | **Profitcenter, durchgehend leer** |

### Kontotexte

98 bebuchte Sachkonten, **alle 98 mit Text** in `SKAT`, Kontenplan 1000, Sprache `D`.
Kein Textproblem.

## 4. Drei Befunde, die den Bauplan bestimmen

1. **`PRCTR` ist in Oesterreich zu 0 Prozent gefuellt.** Das Feld ist im Leser als
   `Dimension2` vorgesehen. Es bleibt leer, und das ist kein Ladefehler, sondern der
   Quellzustand. Deckt sich mit dem Befund vom 2026-09-08, dass `ProfitCode` und
   `OcrCode2-5` auch bei B1 in FR/IT/US/IN leer sind.
2. **`AUGDT` und `AUGBL` haben denselben Fuellgrad, 22,1 Prozent.** Sie gehoeren zusammen
   und sind konsistent gepflegt. Leer heisst hier **nicht ausgeglichen**, nicht
   ungeladen — genau der Vorbehalt, den Andreas am 2026-09-10 bestaetigt hat.
3. **`Faedt` ist kein Tabellenfeld.** Das Nettofaelligkeitsdatum muss aus `ZFBDT` plus
   Zahlungsbedingung (`ZBD1T`/`ZBD2T`/`ZBD3T`) berechnet werden. Bei nur 20,8 Prozent
   `ZFBDT` bleibt `DueDate` fuer rund vier Fuenftel der Zeilen leer. Das ist zu erwarten,
   weil Faelligkeit nur bei offenen Posten fachlich sinnvoll ist, sollte aber niemanden
   ueberraschen, der die Spalte in `Finance_All` sieht.

## 5. Was das EntitySet liefern muss

Service `ZPOWERBI_EINKAUF_SRV`, Set `FinanzJournalSet` (dann ist keine Konfiguration
noetig, der Leser nimmt diesen Namen als Vorgabe). Jeder andere Name muss in
`Sites.SapEntitySet` fuer `ZSCHWEIZ` eingetragen werden.

**20 Pflichtfelder**, sonst verweigert der Leser mit Klartextmeldung:

```
Bukrs  Belnr  Gjahr  Buzei  Budat  Monat  Blart  Xblnr  Stblg  Hwaer
Waers  Hkont  HkontTxt  Shkzg  Dmbtr  Wrbtr  Kostl  Prctr  Sgtxt  Faedt
```

**Zwei optionale**, die den Ausgleich bringen: `Augdt`, `Augbl`. Fehlen sie, laeuft der
Import und die zwei Spalten bleiben leer.

Vier Punkte, die beim Bauen zaehlen:

1. **Eine Zeile je `Buzei`**, nicht je Beleg — daran scheiterte `bkpfSet`.
2. **Alle Belege, nicht nur offene** — daran scheiterte `bsisSet`.
3. **Kein Filter auf `Bukrs`** im Set; der Leser trennt CH und AT selbst. Fuer den ersten
   Entwurf kann die Datenmenge trotzdem ueber Oesterreich begrenzt werden, siehe
   Abschnitt 2.
4. **`HkontTxt` aus `SKAT`** mit Sprache und Kontenplan.

## 6. Was im System steht

| | |
| --- | --- |
| Report | `ZFIN_JOURNAL_PRUEFUNG`, aktiv |
| Paket | `ZPP` |
| Transportauftrag | **`T76K912530`** „Journal CH/AT: EntitySet fuer Finance Dashboard", **nicht freigegeben** |
| Quelle im Repository | `docs/abap/ZFIN_JOURNAL_PRUEFUNG.abap` |

Der Report ist rein lesend, nur `SELECT` und `WRITE`.

## 7. Offen

- Das EntitySet selbst bauen (SEGW, DPC_EXT, Runtime Objects, `/IWFND/CACHE_CLEANUP`).
- Fachentscheid: `IsManual = Blart 'SA'` ist bisher eine **Annahme**. In Oesterreich gibt
  es 267 `SA`-Belege. Ob weitere Belegarten als manuell gelten, entscheidet Andreas.
- Ob `PRCTR` mangels Pflege ganz aus dem Set fliegt oder als leere Spalte mitlaeuft.
