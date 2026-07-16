# SAP OData Spezifikation: VBRP-WAVWR in FinanzdataSchweizOeSet (CH/AT Kostenbasis)

Stand: 2026-07-16
Zielgruppe: SAP-/ABAP-Team (Service-Owner von `ZPOWERBI_EINKAUF_SRV`)
App-Seite: noch nicht umgesetzt — bewusst zurueckgestellt, bis das Feld live ist und die
drei offenen technischen Fragen in Abschnitt 4 beantwortet sind.

## 1. Zweck

Seit 2026-07-14 fuellt die App `StandardCost` fuer CH/AT ueber `mbewSet` (MBEW-STPRS,
Bewertungskreis 1100/1200). Dieser Scan ist seit dem Deploy des TR-AG-Gruppenkosten-
Features (2026-07-15) reproduzierbar haengen geblieben (siehe Nachtrag 2026-07-16 in
`docs/FINANCE_STANDARDKOSTEN_2026-07-14.md`). Alternative, direkt an der Fakturaposition:
`VBRP-WAVWR` (Kostenwert), bereits im ABAP-Analysebericht vom 14.07. mit 92.3 % Abdeckung
identifiziert, aber laut Report „im Z-Service nicht exponiert".

## 2. Live-Verifikation 2026-07-16

Gegen `$metadata` von `ZPOWERBI_EINKAUF_SRV` auf `travt762` geprueft (320'752 Zeichen
durchsucht): `Wavwr` kommt im gesamten Service **nicht** vor. Vollstaendige aktuelle
Property-Liste von `EntityType FinanzdataSchweizOe`:

```
Mandt, Bukrs, Gjahr, Vbeln, Posnr, Land1, Tsc, Fkdat, Fkart, Vbtyp, Kunnr, Name1,
Matnr, Arktx, Prodh, Fkimg, Vrkme, Kurrf, Waerk, Hwaer, NetwrDc, TaxHc, TaxDc,
NetwrHc, IsCredit, PartyClass, ErdatSrc, AedatSrc, CreatedAt, ChangedAt,
CreatedBy, ChangedBy, CustomerLand
```

Zum Vergleich die aktuelle `mbewSet`-Property-Liste (bestaetigt vorhanden, liefert aber
zu viele/irrelevante Materialien und haengt seit 2026-07-15 reproduzierbar):
`Mandt, Matnr, Bwkey, Bwtar, Lvorm, Lbkum, Salk3, Vprsv, Verpr, Stprs, Peinh, Bklas, ...`
(vollstaendig 100+ Felder, siehe Live-Check-Protokoll im Chat vom 2026-07-16).

## 3. Anforderung

- **Feld:** neue Property `Wavwr` (Typ `Edm.Decimal`, analog `NetwrHc`/`NetwrDc`) im
  bestehenden `EntityType FinanzdataSchweizOe`.
- **System:** **`travp762`** (Produktiv) — ausdruecklich nicht `travt762` (Test).
- **Quelle:** `VBRP-WAVWR` fuer dieselbe `Vbeln`/`Posnr`-Zeile, die der Service ohnehin
  schon liefert. Kein neuer Join noetig: `Vbeln`+`Posnr` sind der VBRP-Primaerschluessel
  (`MANDT+VBELN+POSNR`) und werden schon heute exponiert; die App speichert sie bereits
  als `InvoiceNumber`/`PositionOnInvoice` in jeder importierten Zeile.
- **Kein separates EntitySet, kein zusaetzlicher Request:** Wenn der zugrunde liegende
  ABAP-Code die Fakturaposition ohnehin schon liest (was er muss, um `Vbeln`/`Posnr`/
  `Fkdat`/`Kunnr`/`Matnr`/`Fkimg` zu liefern), ist `Wavwr` ein zusaetzliches Feld auf
  derselben Struktur — kein neuer Datenbankzugriff.

## 4. Offene technische Fragen (SAP-Entwickler zu bestaetigen)

1. **Zeilensumme oder Stueckpreis?** Aus der SE16N-Stichprobe (Abschnitt 5) folgt: bei
   `FKIMG=100` und `WAVWR=24'398.33` gegen `NETWR=47'125.00` (~52 % Kosten/Umsatz-
   Verhaeltnis) ist `WAVWR` eine **Zeilensumme**, kein Stueckpreis — waere es ein
   Stueckpreis, laege der Kostenwert absurd ueber dem Umsatz. **Zu bestaetigen.** Falls
   ja, muss die App `Wavwr / Fkimg` rechnen, um es analog zu allen anderen Laendern
   (z. B. DE: `NettoPreisGesamt - RohertragGesamt` / Menge) als Stueckpreis zu
   normalisieren — sonst liegt die Marge um den Mengenfaktor daneben (derselbe
   `PEINH`-Fallstrick wie bei MBEW-STPRS).
2. **Welche Waehrung?** Hauswaehrung (analog `Hwaer`/`NetwrHc`) oder Belegwaehrung
   (analog `Waerk`/`NetwrDc`)? Wird fuer `StandardCostCurrency` benoetigt.
3. **Vorzeichen bei Gutschriften/Retouren:** Kommt `WAVWR` bei einem Gutschriftsbeleg
   schon negativ zurueck (analog `NETWR`), oder ist es immer positiv und die App muesste
   das Vorzeichen selbst ueber `IsCredit`/Belegart herleiten?

## 5. SE16N-Referenzdaten (2026-07-16, vom SAP-Entwickler geliefert)

22 Datensaetze direkt aus `VBRP` (Tabelle, nicht OData) bestaetigen: `WAVWR` ist real
befuellt mit plausiblen Werten, z. B.:

| VBELN | WAVWR | POSNR | FKIMG | NETWR | Matnr |
| --- | ---: | --- | ---: | ---: | --- |
| 90011092 | 24'398.33 | 10 | 100 | 47'125.00 | 32184 |
| 90011715 | 6'440.03 | 10 | 25 | 12'851.25 | 32184 |
| 90014001 | 6'063.97 | 10 | 25 | 10'281.30 | 32184 |

(Stichprobe zeigt Belege aus 2003/2004 — SE16N-Standardsortierung nach `VBELN` aufsteigend,
keine Aussage ueber Aktualitaet der Daten, nur ueber Feldbefuellung.)

Wichtig: Dieser Auszug bestaetigt nur, dass `WAVWR` in der **Tabelle** `VBRP` gefuellt ist
(SE16N liest die DB-Tabelle direkt). Er sagt nichts darueber aus, ob das Feld im
**OData-Service** exponiert ist — das ist laut Abschnitt 2 aktuell nicht der Fall.

## 6. Was die App daraus machen wuerde (sobald Abschnitt 4 geklaert ist)

| Zielfeld | Ableitung (Entwurf, noch nicht umgesetzt) |
| --- | --- |
| `StandardCost` | `Z.Wavwr / Z.Fkimg` (falls Zeilensumme, siehe Frage 1) |
| `StandardCostCurrency` | `Z.Hwaer` oder `Z.Waerk`, je nach Antwort auf Frage 2 |

Der bestehende `mbewSet`-CH/AT-Scan (`SapGatewayStandardCostReader` +
`StandardCostEnricher.ValuationAreaByCountry`) wuerde dadurch fuer die **lokale**
CH/AT-Kostenbasis abgeloest. **Nicht betroffen:** die TR-AG-Konzernkosten-Logik
(`GroupStandardCosts`, seit 2026-07-15 fuer Lieferant „Trafag AG" ueber alle Laender
hinweg) — die bleibt ein separater, zusaetzlicher Mechanismus und wird durch `Wavwr`
nicht ersetzt. `Wavwr` loest „CH/AT braucht ueberhaupt eine Kostenbasis"; die
TR-AG-Logik loest „interner Lieferant TR AG braucht Konzernkosten statt lokaler Kosten".

## 7. Warum das das Performance-Problem strukturell loest

`FinanzdataSchweizOeSet` liefert fuer `ZSCHWEIZ` zuverlaessig **40'292 Zeilen**
(mehrfach bestaetigt, zuletzt 46.0s Laufzeit). `mbewSet` filtert nur nach Bewertungskreis
(nicht danach, ob ein Material je verkauft wurde) und liefert **~68'011 Materialien**
(65'447 fuer Bewertungskreis 1100 + 2'564 fuer 1200, laut ABAP-Analysebericht). Der
eigentliche Gewinn ist aber nicht die kleinere Zahl, sondern dass `Wavwr` **keine
zusaetzliche Abfrage** waere — es haengt sich an eine Abfrage, die heute schon
zuverlaessig laeuft. Der komplette `mbewSet`-Aufruf (der seit 2026-07-15 haengt) wuerde
dadurch fuer die CH/AT-Kostenbasis komplett entfallen, statt durch einen gleich grossen
Scan ersetzt zu werden.

## 8. Abnahme (sobald Feld live ist)

1. `GET .../FinanzdataSchweizOeSet?$format=json&$top=5` liefert Zeilen mit `Wavwr`.
2. Stichprobe: `Wavwr` gegen `NETWR`/`FKIMG` derselben Belegzeile in SE16N plausibilisiert
   (Kosten/Umsatz-Verhaeltnis im erwarteten Rahmen, kein Faktor-100-Fehler durch
   Zeilensumme-vs-Stueckpreis-Verwechslung).
3. Danach App-seitig: neues Feld-Mapping ergaenzen, `mbewSet`-CH/AT-Pfad ablösen,
   Tests ergaenzen, Live-Stichprobe wie am 2026-07-15/16 wiederholen (Filter
   `SupplierName LIKE '%Trafag AG%'` bzw. allgemeine CH/AT-Kostenquote gegen die
   erwarteten ~92-96 % pruefen).

## 9. Zusammenhang mit offenen Punkten

- Loest NICHT die Fragen A/B an Andreas (Kostenart, liefernde vs. verkaufende
  Gesellschaft) — das bleibt ein fachlicher Entscheid, siehe
  `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`.
- Loest NICHT das separate Problem „CH/AT sieht 2026 nicht" (`Sites.SapServiceUrl` zeigt
  auf `travt762` statt `travp762`) — das ist eine reine App-Konfigurationsaenderung,
  keine SAP-Aenderung, sollte aber in derselben Abstimmung mit adressiert werden.
