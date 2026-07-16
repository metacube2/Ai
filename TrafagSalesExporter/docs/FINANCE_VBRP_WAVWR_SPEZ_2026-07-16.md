# SAP OData Spezifikation: VBRP-WAVWR in FinanzdataSchweizOeSet (CH/AT Kostenbasis)

Stand: 2026-07-16
Zielgruppe: SAP-/ABAP-Team (Service-Owner von `ZPOWERBI_EINKAUF_SRV`)
App-Seite: noch nicht umgesetzt — bewusst zurueckgestellt, bis das Feld live ist und die
verbliebenen technischen Fragen in Abschnitt 5 beantwortet sind. Fachlich ist die Wahl
WAVWR (statt STPRS) bereits geklaert, siehe Abschnitt 4 — keine offene Andreas-Frage mehr.

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

## 4. Fachliche Klaerung: WAVWR vs. STPRS (2026-07-16, mit SAP-Entwickler geklaert)

**Vorab wichtig, damit hier keine falsche Aequivalenz entsteht:** `VBRP-WAVWR` ist
**nicht** dasselbe Konzept wie `MBEW-STPRS`, auch nicht naeherungsweise.

- `WAVWR` ist der **zum Zeitpunkt der Warenausgangsbuchung eingefrorene** Kostenwert —
  "was hat uns dieser Verkauf gekostet". Aendert sich nicht rueckwirkend.
- `STPRS` ist der **aktuelle** Standardpreis im Materialstamm — "was wuerde uns dieses
  Produkt heute kosten". Aendert sich bei jeder Preisaenderung (`MR21`/`CK24`); eine
  Neuberechnung derselben historischen Rechnung mit `STPRS` wuerde je nach Abfragezeit-
  punkt unterschiedliche Margen liefern.
- `WAVWR = Menge x (STPRS/PEINH)` gilt nur bei Preissteuerung `S` UND wenn seit dem
  Warenausgang keine Preisaenderung stattfand. Bei Preissteuerung `V` (gleitender
  Durchschnittspreis) entspricht `WAVWR` stattdessen `VERPR` zum Buchungszeitpunkt.
  Relevanz fuer uns gering: Bewertungskreis 1100 (CH) ist laut ABAP-Bericht vom 14.07.
  zu 100 % `VPRSV = S`; fuer 1200 (AT) noch nicht explizit erneut bestaetigt.
- `WAVWR` liegt in **Belegwaehrung** (`Waerk`), nicht in Hauswaehrung (`Hwaer`) — das
  beantwortet die fruehere Frage 2 (Waehrung) direkt.

**Damit ist auch geklaert, welcher Wert fuer UNSER Feature richtig ist — keine offene
Fachfrage an Andreas mehr:** Die Gruppenmarge/Finance Pruefbuch ist durchgehend eine
"Was hat uns dieser Verkauf gekostet"-Anwendung (Margenanalyse je Beleg, Nachweis-/
Audit-Sicht auf tatsaechlich gebuchte Transaktionen) — keine Preisfindungs- oder
Sortimentsanalyse. Fuer diesen Zweck ist `WAVWR` fachlich der korrekte Wert, nicht
`STPRS`. Das Ergebnis aendert sich bei einer spaeteren Neuberechnung nicht rueckwirkend,
was fuer ein Audit-/Pruefbuch-Feature sogar ein Vorteil ist (reproduzierbare historische
Margen). Falls jemals ein separates Pricing-/Sortiments-Feature entstehen sollte, waere
dort `STPRS` (weiterhin ueber `mbewSet`, mit dem bekannten Performance-Thema) die
richtige Wahl — betrifft aber nicht die Gruppenmarge.

## 5. Offene rein technische Fragen (SAP-Entwickler zu bestaetigen)

1. **Zeilensumme oder Stueckpreis?** Aus der SE16N-Stichprobe (Abschnitt 6, unten) folgt: bei
   `FKIMG=100` und `WAVWR=24'398.33` gegen `NETWR=47'125.00` (~52 % Kosten/Umsatz-
   Verhaeltnis) ist `WAVWR` eine **Zeilensumme**, kein Stueckpreis — waere es ein
   Stueckpreis, laege der Kostenwert absurd ueber dem Umsatz. **Zu bestaetigen.** Falls
   ja, muss die App `Wavwr / Fkimg` rechnen, um es analog zu allen anderen Laendern
   (z. B. DE: `NettoPreisGesamt - RohertragGesamt` / Menge) als Stueckpreis zu
   normalisieren — sonst liegt die Marge um den Mengenfaktor daneben (derselbe
   `PEINH`-Fallstrick wie bei MBEW-STPRS).
2. **Vorzeichen bei Gutschriften/Retouren:** Kommt `WAVWR` bei einem Gutschriftsbeleg
   schon negativ zurueck (analog `NETWR`), oder ist es immer positiv und die App muesste
   das Vorzeichen selbst ueber `IsCredit`/Belegart herleiten?
3. **AT/Bewertungskreis 1200:** Ist dort `VPRSV` ueberwiegend ebenfalls `S`, oder gibt es
   nennenswerten `V`-Anteil? (Nur zur Einordnung, aendert nichts an der Grundsatzwahl
   WAVWR — siehe Abschnitt 4.)

## 6. SE16N-Referenzdaten (2026-07-16, vom SAP-Entwickler geliefert)

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

## 7. Was die App daraus machen wuerde (sobald Abschnitt 5 geklaert ist)

| Zielfeld | Ableitung (Entwurf, noch nicht umgesetzt) |
| --- | --- |
| `StandardCost` | `Z.Wavwr / Z.Fkimg` (falls Zeilensumme, siehe Frage 1) |
| `StandardCostCurrency` | `Z.Waerk` (Belegwaehrung, siehe Abschnitt 4) |

Der bestehende `mbewSet`-CH/AT-Scan (`SapGatewayStandardCostReader` +
`StandardCostEnricher.ValuationAreaByCountry`) wuerde dadurch fuer die **lokale**
CH/AT-Kostenbasis abgeloest. **Nicht betroffen:** die TR-AG-Konzernkosten-Logik
(`GroupStandardCosts`, seit 2026-07-15 fuer Lieferant „Trafag AG" ueber alle Laender
hinweg) — die bleibt ein separater, zusaetzlicher Mechanismus und wird durch `Wavwr`
nicht ersetzt. `Wavwr` loest „CH/AT braucht ueberhaupt eine Kostenbasis"; die
TR-AG-Logik loest „interner Lieferant TR AG braucht Konzernkosten statt lokaler Kosten".

## 8. Warum das das Performance-Problem strukturell loest

`FinanzdataSchweizOeSet` liefert fuer `ZSCHWEIZ` zuverlaessig **40'292 Zeilen**
(mehrfach bestaetigt, zuletzt 46.0s Laufzeit). `mbewSet` filtert nur nach Bewertungskreis
(nicht danach, ob ein Material je verkauft wurde) und liefert **~68'011 Materialien**
(65'447 fuer Bewertungskreis 1100 + 2'564 fuer 1200, laut ABAP-Analysebericht). Der
eigentliche Gewinn ist aber nicht die kleinere Zahl, sondern dass `Wavwr` **keine
zusaetzliche Abfrage** waere — es haengt sich an eine Abfrage, die heute schon
zuverlaessig laeuft. Der komplette `mbewSet`-Aufruf (der seit 2026-07-15 haengt) wuerde
dadurch fuer die CH/AT-Kostenbasis komplett entfallen, statt durch einen gleich grossen
Scan ersetzt zu werden.

## 9. Abnahme (sobald Feld live ist)

1. `GET .../FinanzdataSchweizOeSet?$format=json&$top=5` liefert Zeilen mit `Wavwr`.
2. Stichprobe: `Wavwr` gegen `NETWR`/`FKIMG` derselben Belegzeile in SE16N plausibilisiert
   (Kosten/Umsatz-Verhaeltnis im erwarteten Rahmen, kein Faktor-100-Fehler durch
   Zeilensumme-vs-Stueckpreis-Verwechslung).
3. Danach App-seitig: neues Feld-Mapping ergaenzen, `mbewSet`-CH/AT-Pfad ablösen,
   Tests ergaenzen, Live-Stichprobe wie am 2026-07-15/16 wiederholen (Filter
   `SupplierName LIKE '%Trafag AG%'` bzw. allgemeine CH/AT-Kostenquote gegen die
   erwarteten ~92-96 % pruefen).

## 10. Zusammenhang mit offenen Punkten

- Loest NICHT die Fragen A/B an Andreas (Kostenart, liefernde vs. verkaufende
  Gesellschaft) — das bleibt ein fachlicher Entscheid, siehe
  `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`.
- Loest NICHT das separate Problem „CH/AT sieht 2026 nicht" (`Sites.SapServiceUrl` zeigt
  auf `travt762` statt `travp762`) — das ist eine reine App-Konfigurationsaenderung,
  keine SAP-Aenderung, sollte aber in derselben Abstimmung mit adressiert werden.

## 11. Umsetzungsfortschritt 2026-07-16 (SAP-Seite)

Der SAP-Entwickler (Teil dieses Chats) hat direkt umgesetzt, schneller als der urspruengliche
Plan (kein separates `VBRPSet` als App-Quelle, sondern Erweiterung der bestehenden
Export-Pipeline nach `ZSCHWEIZ`):

- **`VBRPSet` probeweise per SEGW angelegt** (separates EntitySet, nicht die App-Quelle) —
  bestaetigt live: `Wavwr` funktioniert technisch, Wert stimmt exakt mit SE16N ueberein.
  Wichtiger Nebenbefund dabei: **`$top` wird von diesem Service generell nicht
  durchgesetzt** (`$top=1` lieferte 295'694'320 Zeichen / tausende Zeilen) — bestaetigt,
  dass ein ungefiltertes `VBRPSet` als App-Quelle genauso haengen wuerde wie `mbewSet`.
  Grund, warum der urspruengliche Plan (Feld in `FinanzdataSchweizOeSet`, das serverseitig
  bereits gefiltert ist) weiterhin richtig ist.
- **Report `Z_TRAFAG_SCHWEIZ_EXPORT`** (in ihrem System als `Z_TRAFAG_DACH_EXPORT`
  eingebunden, liest `VBRK`/`VBRP`/`KNA1`/`T001`, UPSERT nach `ZSCHWEIZ`) um `WAVWR_DC`
  erweitert und fuer Jahr 2026 gelaufen: **9'864 Zeilen verarbeitet.**
- **Coverage-Befund (2026, frisch verarbeitet):** 8'649 von 9'864 Zeilen mit echtem
  `WAVWR_DC`-Wert (~87.7 %), 1'215 mit `0`. Historischer Bestand (~30'642 Zeilen, andere
  Jahre) zeigt erwartungsgemaess durchgaengig `0`, weil noch nicht neu verarbeitet
  (Nachsorge-Rueckstand, kein Datenproblem).
- **Root Cause der echten Nullen gefunden:** Stichprobe zeigte `WAVWR_DC = 0` bei einem
  Material mit **gepflegtem** `STPRS` (`B53446`, `STPRS = 1.01`, `VPRSV = S`) — die
  urspruengliche Hypothese „STPRS fehlt" ist damit widerlegt. Wahrscheinlicher Grund:
  `WAVWR` wird nur bei **lieferbezogener Fakturierung** (echte Warenausgangsbuchung,
  `VGTYP='J'` in `VBRP`) gesetzt; bei auftragsbezogener Fakturierung ohne Lieferbezug
  gibt es keine Bestandsbuchung und damit keinen `WAVWR`-Wert, unabhaengig vom
  gepflegten Standardpreis. Noch nicht abschliessend per `VGTYP`-Vergleich verifiziert,
  aber mit den vorliegenden Daten (Materialien mit gepflegtem `STPRS`, trotzdem
  `WAVWR=0`) die schluessigste Erklaerung.
- **Konsequenz — Vorschlag SAP-Entwickler, umgesetzt:** `MBEW` zusaetzlich per direktem
  ABAP-Join (nicht ueber `mbewSet`/OData) in denselben Report eingebaut. Neues Feld
  **`STPRS_HC`** (Stueckpreis nach `PEINH`-Division, Hauswaehrung, aus `MBEW-STPRS`,
  Join-Schluessel `MATNR`+`BWKEY=BUKRS`) ergaenzt `WAVWR_DC` — beide Felder nebeneinander
  in `ZSCHWEIZ`. Das umgeht das `mbewSet`-Performance-Problem komplett (ABAP-seitiger
  Join ueber Schluessel ist performant, kein Massen-Scan noetig) und gibt uns fuer die
  ~12 % Zeilen ohne `WAVWR_DC` einen Fallback-Kandidaten. **`WAVWR_DC` bleibt die
  fuehrende Kostenbasis fuer die Gruppenmarge** (siehe Abschnitt 4); `STPRS_HC` ist
  zusaetzliches Feld, App-seitige Fallback-Logik noch nicht entschieden/umgesetzt.
  Vollstaendiger, aktueller Report-Code: `docs/abap/Z_TRAFAG_SCHWEIZ_EXPORT.abap`.
- **Offen:** Rueckwirkender Lauf fuer historische Jahre (2025 und frueher) noch nicht
  gemacht — daher noch keine belastbare Gesamt-Coverage ueber den vollen CH/AT-Bestand.
  `VGTYP`-Verifikation der Lieferbezug-Hypothese noch offen. SEGW-Ergaenzung von
  `Wavwr_Dc`/`Stprs_Hc` auf `FinanzdataSchweizOe` (statt nur `VBRPSet`) noch zu
  bestaetigen.
