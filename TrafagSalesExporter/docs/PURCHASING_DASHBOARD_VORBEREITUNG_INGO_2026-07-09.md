# Einkaufsdashboard: Vorbereitung nicht-umsetzbarer Punkte (2026-07-09)

Zweck: Der Umsetzungsplan `PURCHASING_DASHBOARD_UMSETZUNGSPLAN_MARCO_2026-07-09.md` enthaelt
Teile, die **nicht im Code allein** loesbar sind, weil sie externe Inputs brauchen: CSV-Referenz-
listen von dir, SAP-Metadaten-Checks (nur mit Live-Zugriff) oder neue SAP-Service-Objekte
(SAP-Team). Diese Datei sagt dir konkret, **was du vorbereiten musst** und in welchem Format,
damit die naechste Session es direkt einbauen kann.

Bereits im Code erledigt (diese Session, 152/152 Tests gruen, noch kein Deploy):

- Phase 1.1 Ueberfaellige Lieferpositionen: eigene KPI (Wert/Menge/Anzahl) + Drilldown-Liste,
  sichtbar in `Offene Bestellungen` und `Ideen > Liefertermin-Risiko`.
- Phase 1.2 Preisentwicklung je Artikel: Top-8-Artikel nach Spend, mengengewichteter
  Ø-Stueckpreis je Jahr, YoY-Trend (gestiegen/gesunken), sichtbar in `Ideen > Preisabweichung`.
- Phase 1.5 Kontrakt-KPI als "Naeherung (nur Konnr-Abrufe)" gekennzeichnet.

Alles Uebrige braucht Vorbereitung -> unten.

---

## NACHTRAG 2026-07-09 (spaeter): OData-Service-Umfang bekannt — Bild aenderte sich

Ingo hat das Service-Dokument von `ZPOWERBI_EINKAUF_SRV`
(`http://travt762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/`) geliefert.
**Ergebnis: Fast alle Tabellen, die ich fuer Phase 2/3 dem SAP-Team zuschreiben wollte, sind
bereits als EntitySet freigeschaltet.** Damit sind diese Punkte NICHT SAP-Team-blockiert, sondern
nur noch Loader-/Dashboard-Arbeit (mache ich, sobald ein Full Load die Felder gefuellt hat).

Verfuegbare, einkaufsrelevante EntitySets und ihre Plan-Zuordnung:

| EntitySet (Tabelle) | Schaltet frei | Bisher gedacht |
|---|---|---|
| `MARCSet` (MARC) | Phase 1.4 Disponent (DISPO) + Sicherheitsbestand (EISBE), Aufriss 2 | war "ZC23-CSV noetig" -> **CSV entfaellt** |
| `LFA1Set` (LFA1) | Phase 2.1 Beschaffungsregion (LAND1/REGIO/ORT01) | war "OData-Check" -> **verfuegbar** |
| `mbewSet` (MBEW) | Phase 2.3 Lagerbestand/Standardkosten (STPRS/SALK3/LBKUM) | war "OData-Check" -> **verfuegbar** |
| `EKBESet` (EKBE) | Phase 3.1 Liefertermintreue (WE-BUDAT vs. EKET-EINDT) | war "SAP-Team" -> **verfuegbar** |
| `qmelSet`/`qmfeSet`/`qmmaSet`/`qmurSet` + `zpowerbi_qm*` | Phase 3.2 Reklamations-/Qualitaetsquote, PPM | war "QM-CSV-Export noetig" -> **direkt aus OData** |
| `ekabSet` (EKAB, Abrufdokumentation) | K4 echte Kontraktabrufe/Rahmenvertragsnutzung | war "SAP-Team Kontraktbelege" -> **teilweise verfuegbar** |
| `mdbsSet` (MDBS) | offene Bestellmengen je Material (Materialsicht) | neu nutzbar |
| `MKPFSet`/`MSEGSet` (Materialbelege) | feste Zu-/Abgaenge, WE/WA-Bewegungen | Alternative zu RESB |
| `MAKTSet` (MAKT) | Artikeltexte (Anzeige Materialnummer) | nice-to-have |
| `stkoSet`/`stpoSet`/`mastSet` (Stueckliste) | Marcos Aufriss 2 "Produktionsmaterialbezug aus Stueckliste" | neu nutzbar |
| `mverSet` (MVER), `afpoSet` (AFPO) | Verbrauch / Sekundaerbedarf-Naeherung | neu nutzbar |
| `MARA001Set`, `EKKOSet`, `EKPOSet`, `eketSet` | bereits im Einsatz | — |

**Echte verbleibende Luecken (NICHT im Service):**

1. **T023T (Warengruppen-Texte)** — kein EntitySet. **ERLEDIGT via Analyse-Report**: nur 20 Codes,
   alle mit Text (siehe CSV-Block in der Report-Ausgabe / Nachtrag Hauptdoku). Werden als
   Seed-Referenz eingebaut; kein weiterer Input noetig.
2. **T024D (Disponenten-Texte)** — kein EntitySet. Dispo-**Code** kommt ueber MARCSet (OData);
   Gruppen-Namen aus dem Analyse-Report erfasst (`001 rot/Einkauf` usw.). Ausreichend.
3. **RESB (Reservierungen/Sekundaerbedarf)** — kein EntitySet. "Feste Abgaenge" nur ueber
   AFPO/MDBS/MSEG annaeherbar; fuer die saubere Loesung RESB ins Service aufnehmen lassen.

**KRITISCHE SAP-Modell-Erweiterung (blockiert die korrekten Zahlen) — Analyse-Report 2026-07-09:**

Der Report zeigt: der Cache mischt Bestellungen (BSTYP=F/BSART=NB), Anfragen (A/AN, 3'117),
Kontrakte (K/MK, 2'766) und Umlagerungen (UB, 16); und 7,46 Mio des offenen Werts liegen auf
endgelieferten Positionen (ELIKZ=X). Um Spend/offene Werte korrekt zu trennen und Elikz abzuziehen,
muessen im OData-Modell (MPC) diese Properties ergaenzt werden:

- `EKKO-BSTYP` und `EKKO-BSART` (Trennung Bestellung/Anfrage/Kontrakt/Umlagerung — Marcos Forderung)
- `EKPO-ELIKZ` (Endlieferungskennzeichen — M7)
- moeglichst `EKPO-KTMNG` (Zielmenge, fuer Abrufquote)

`Waers`/`Wkurs`/`Konnr` sind bereits im Modell. `Bsart`/`Meins` warfen frueher HTTP 400, also fehlt
die Property definitiv -> MPC-Erweiterung noetig. **Zusaetzlich einmal `$metadata` schicken**, damit
ich die exakten Property-Namen der bereits verfuegbaren Sets (MARC/MBEW/EKBE/LFA1/QM) fuer die
Loader-`$select` kenne.

**Wichtig — Property-Ebene noch offen:** Das Service-DOKUMENT listet nur die EntitySets, nicht
die Felder je Entity-Typ. Ob z.B. `EKPOSet` die Property `Elikz`/`Ktmng` fuehrt oder `mbewSet`
`Stprs`, steht erst im `$metadata`. Zwei Wege, beide von Ingo:

- **(bevorzugt) das Analyse-ABAP `sap_purchasing_analyse_report.abap` laufen lassen** — liefert
  die echten Datenverteilungen (Waehrung/Konnr/Elikz/Dispo-Fuellgrad/MBEW/EKBE) plus die beiden
  fehlenden Text-CSVs (T023T/T024D). Damit baue ich die Logik richtig, ohne SAP-Zugriff.
- zusaetzlich einmal `ZPOWERBI_EINKAUF_SRV/$metadata` schicken -> dann kenne ich die exakten
  Property-Namen je EntitySet und kann die Loader-`$select` sauber setzen (kein 400 wie bei
  `Bsart`/`Meins`).

Konsequenz fuer den Plan: Die Abschnitte D (OData-Checks) und E (SAP-Team) unten sind groesstenteils
**erledigt/hinfaellig** — die Objekte sind da. Es bleibt: T023T/T024D-Texte, RESB, und der
Property-Check via ABAP-Report + `$metadata`. Der Rest ist meine Loader-/Dashboard-Arbeit.

---

## A. Phase 0 — Deploy + Full Load (operativ, keine Vorbereitung von Marco noetig)

### A0. Systemwechsel travt762 (Test) -> travp762 (Prod) und OData-Auth-Check (Stand 2026-07-09)

- **Wo die URL steht:** NICHT auf `Einkauf > Datenquellen` (das Feld dort ist nur ein optionales
  Override und bewusst leer). Die aktiv genutzte URL ist die zentrale SAP-URL:
  **Admin > Einstellungen > Quellsysteme > Eintrag `SAP` bearbeiten > Feld "Zentrale SAP Service URL"**.
  Dort `travt762` -> `travp762` aendern, "Uebernehmen" + "Quellsysteme speichern". Wirkt fuer alle
  SAP-Bereiche (Finance/HR/Einkauf), kein Deploy noetig.
- **Nach dem Wechsel:** Einkauf-Cache enthaelt noch Testdaten -> Full Load noetig (sonst alte Zahlen).
- **Offener OData-Auth-Punkt:** Direkter Basic-Auth-Check gegen
  `travp762:8000/.../ZPOWERBI_EINKAUF_SRV` gab `HTTP 401` (User `koi`). Netz ist erreichbar.
  Zu klaeren: Basic-Auth-Kodierung (Sonderzeichen im Passwort, UTF-8 vs. ISO-8859-1),
  User/Client-Berechtigung fuer den Service auf P, ggf. Sperre nach Fehlversuchen (SU01 pruefen).
  Risikofreier Gegentest: die EKKOSet/EKPOSet-URLs im Browser (SSO) aufrufen. Hilfsskript liegt
  unter `.tmp_sap_probe/probe_travp762_odata.ps1` (Passwort wird interaktiv abgefragt).
- **Metadaten-Cache:** Nach MPC-Aenderungen `/IWFND/CACHE_CLEANUP` (+ ggf. `/IWBEP/CACHE_CLEANUP`),
  sonst liefert `$select` auf neue Felder weiter 400.

Reihenfolge (aus der Server-Restore-Erfahrung in der Hauptdoku):

1. Deploy-Entscheid mit dir bestaetigen; dann publish auf
   `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\` (app_offline.htm setzen/entfernen).
2. Full Load NICHT direkt gegen die UNC-Server-DB: lokal gegen eine DB-Kopie fahren, dann
   fertige DB auf den Server kopieren. Beim Ersetzen `trafag_exporter.db-wal` und `-shm`
   passend mitsichern/entfernen (sonst "database disk image is malformed").
3. Nach dem Load pruefen: HTTP 200, Cache-Counts, Full-Load-Statusmeldung zeigt
   `LFA1-Namen=<n>` und `MARA-Status=<n>` > 0.

Vorzubereiten von dir/Marco fuer die Abnahme:

- [ ] Soll-Wert 18 Mio offener Wert gegen SAP (ME2M/ME2L) bestaetigen.
- [ ] Ein konkreter Monat + Lieferant mit Power-BI-Sollzahl fuer die Spend-Gegenpruefung.
- [ ] Ein echter Fremdwaehrungsbeleg (EKKO mit Waers <> CHF) + erwarteter CHF-Wert, um die
      WKURS-Richtung (multiplizieren vs. dividieren) zu verifizieren. Solange alle Belege CHF
      sind, ist K1 wirkungslos und kann nicht geprueft werden.

---

## B. Phase 1.3 — Warengruppen-Texte (CSV-Referenzliste von Ingo)

Problem: Der OData-Service liefert nur den Warengruppen-**Code** (`EKPO.Matkl`, z.B. `01`, `A100`),
keinen Text. PBIX nutzte `Data (2).WG komplett`; dieses EntitySet ist im Service 404.

Loesung: Referenzliste als CSV, die im Code als Upload-Tabelle `PurchasingMaterialGroupRef`
gefuehrt und ueber `Matkl` gejoint wird. Anzeige `Code - Text`, Fallback Code.

**Vorzubereiten: CSV-Export aus SAP-Tabelle T023T (Warengruppentexte), Sprache Deutsch.**

Format (UTF-8, Semikolon-getrennt, mit Kopfzeile):

```
Matkl;Wgbez
01;Rohmaterial Metall
A100;Elektronische Bauteile
...
```

- `Matkl`: exakt wie in EKPO (fuehrende Nullen so lassen, wie SAP sie im OData liefert — bitte
  einen Beispiel-Matkl aus dem Dashboard mit dem T023T-Wert abgleichen).
- `Wgbez`: Warengruppenbezeichnung (T023T-WGBEZ).
- Optional zusaetzliche Spalte fuer eine groebere Ebene (z.B. Waren-/Fertigproduktgruppe), falls
  Marco die Gruppierung "Waren- und Fertigproduktgruppen" braucht:

```
Matkl;Wgbez;Obergruppe
01;Rohmaterial Metall;Rohmaterial
A100;Elektronische Bauteile;Zukauf Elektronik
```

Ablage: `docs/refdata/warengruppen_texte.csv` (oder du nennst mir den Pfad).

---

## C. Phase 1.4 — Disponentengruppen / Aufriss 2 (ZC23-Tabelle von Ingo)

Problem: EKPO hat kein Disponenten-Feld. Marco will nach Disponenten 001-005 und nach
"Verwendung / Produktgruppe" (ueber Disponentengruppe aus ZC23) filtern.

Loesung: Referenzliste als Upload-Tabelle `PurchasingDisponentRef`, Join auf `EKPO.Matnr`
(+ optional `Werks`). Damit funktionieren Aufriss 2 und der Disponenten-Filter ohne SAP-Change.

**Vorzubereiten: Export deiner ZC23-Disponentenzuordnung als CSV.**

Format (UTF-8, Semikolon, Kopfzeile):

```
Matnr;Werks;Dispo;Dispogruppe;Produktgruppe
000000000000123456;1000;001;Produktion Sensorik;Sensoren
000000000000123457;1000;002;Produktion Ventile;Ventile
...
```

- `Matnr`: SAP-Materialnummer. Format egal — der Import normalisiert (Whitespace weg,
  Grossbuchstaben, fuehrende Nullen weg), genau wie der bestehende MARA/LFA1-Join. Du kannst
  also 18-stellig mit fuehrenden Nullen liefern.
- `Werks`: Werk (optional; leer lassen, wenn material-, nicht werksbezogen).
- `Dispo`: Disponentennummer (001-005 usw.).
- `Dispogruppe`: Klartext der Disponentengruppe (fuer Aufriss/Filter-Label).
- `Produktgruppe`: optionale zusaetzliche Verwendungs-/Produktgruppe (Marcos Aufriss 2), falls
  aus ZC23 oder der Stueckliste ableitbar. Leer lassen, wenn nicht verfuegbar.

Wichtig fuer die Umsetzbarkeit:

- Bitte pro Material EINE Zeile (oder pro Material+Werk). Wenn ein Material mehrere Disponenten
  haben kann, sag mir die Regel (z.B. "je Werk unterschiedlich" -> dann Matnr+Werks als
  Schluessel).
- Wenn die Zuordnung eigentlich am Materialstamm (MARC-DISPO) haengt und du sie lieber direkt
  aus SAP ziehst statt als CSV: siehe Phase 2.5 (MARC anbinden) — dann brauchst du keine CSV,
  aber einen SAP-Service-Change.

Ablage: `docs/refdata/disponenten.csv`.

---

## D. Phase 2 — SAP-Metadaten-Checks (nur mit SAP-Live-Zugriff; von Ingo auszufuehren)

Diese Punkte sind erst umsetzbar, nachdem geprueft ist, ob der Service
`ZPOWERBI_EINKAUF_SRV` die Felder/EntitySets liefert. Bitte fuehre die folgenden OData-Proben
aus (Browser mit SAP-Login oder Postman; Basic-Auth wie im Dashboard hinterlegt) und schick mir
je das Ergebnis (HTTP-Status + ob das Feld belegt ist). **NICHT blind in $select aufnehmen** —
`Bsart`/`Meins` warfen frueher HTTP 400.

Basis-URL: `<SAP-Service-Root>/ZPOWERBI_EINKAUF_SRV/`

1. **Beschaffungsregion (Aufriss 5) — LFA1-Adressfelder:**
   ```
   LFA1Set?$top=1&$format=json&$select=Lifnr,Name1,Land1,Regio,Ort01
   ```
   - HTTP 200 + Felder gefuellt -> Region/Land baubar (Spalte `SupplierCountry` in EKKO-Cache,
     Region-Aufriss im Dashboard).
   - HTTP 400 -> welche Felder gehen einzeln? Bitte `Land1` allein testen.

2. **Endlieferungskennzeichen (M7) — EKPO.Elikz:**
   ```
   EKPOSet?$top=1&$format=json&$select=Ebeln,Ebelp,Elikz
   ```
   - HTTP 200 -> ich nehme `Elikz` in $select/Schema/Offen-Formeln auf
     (`AND COALESCE(Elikz,'') <> 'X'`); senkt den offenen Wert -> VOR dem 18-Mio-Abgleich einbauen.
   - HTTP 400 -> als offener Punkt ans SAP-Team.

3. **Lagerbestand + Standardkosten — MBEW (mbew):**
   ```
   mbew?$top=1&$format=json
   ```
   (bzw. korrektes EntitySet aus `$metadata` — der Nebenbefund 2026-07-02 nannte `mbew`.)
   - Bitte pruefen, welche Felder kommen (erwartet u.a. `Matnr`, `Bwkey`/Bewertungskreis,
     `Lbkum` Menge, `Salk3` Wert, `Stprs` Standardpreis, `Verpr` gleitender Preis).
   - Ergebnis entscheidet: Lagerbestand CHF (aktuell) + Standardkosten fuer die offene
     Finance-Gruppenmarge. Historie (Stichtag) gibt es so nicht -> wir muessten ab Anbindung
     selbst snapshotten.

4. **Kontraktbelege (Marcos "Offene Mengenkontrakte mit Ablaufdatum"):**
   - Im `$metadata` nach einem Kontrakt-EntitySet suchen (Belegart K), Felder wie `Konnr`,
     `Kdatb`/`Kdate` (Laufzeit von/bis), Zielmenge/-wert.
   - Wenn nicht vorhanden -> Phase-2-Anfrage ans SAP-Team (siehe E).

Fuer jeden Punkt reicht mir: `Punkt-Nr -> HTTP 200/400 -> welche Felder gefuellt`. Dann sage ich
dir, was davon ich sofort einbauen kann.

---

## E. Phase 3 — Neue SAP-Objekte / QM-Export (SAP-Team-Anfrage bzw. fachliche Vorklaerung)

Diese Themen brauchen neue Datenquellen. Vorzubereiten: **Anfrage ans SAP-Team** bzw.
fachliche Definition mit Marco. Ich liste, was genau angefragt werden muss.

1. **Liefertermintreue [%] (Lieferantenperformance):**
   - Braucht das Ist-Wareneingangsdatum je Position. EKET hat nur die Menge (`Wemng`), kein
     WE-Datum. Sauber ueber **EKBE** (Bestellentwicklung/Historie).
   - SAP-Team-Anfrage: EntitySet fuer EKBE mit `Ebeln, Ebelp, Vgabe/Bewtp (WE = 'E' bzw. '1'),
     Budat (Buchungsdatum WE), Menge`.
   - Fachlich mit Marco: Toleranzfenster fuer "puenktlich" (z.B. +/- 0 Tage, oder frueh ok /
     spaet nicht). Ohne EKBE keine seriose Termintreue — bitte nicht schaetzen.

2. **Qualitaets-/Reklamationsquote [%], PPM:**
   - QM-Meldungen (QMEL/QMFE) sind nicht im Service. Pragmatischer Weg wie bei Florian Waechter:
     automatisierter CSV-Export aus SAP-QM, taeglich, Import als eigene Cache-Tabelle.
   - Vorzubereiten (Marco/Einkauf): welche SAP-QM-Transaktion/Auswertung, welche Soll-Spalten
     (mind. Lieferant, gelieferte Menge, beanstandete Menge, Datum, ggf. Meldungsart), und wer
     den Export einrichtet.

3. **Feste Abgaenge / Sicherheitsbestand (Lager-Periodenende):**
   - Feste Abgaenge = Reservationen/Sekundaerbedarfe -> **RESB** (nicht im Service).
   - Sicherheitsbestand -> **MARC.Eisbe** x Bewertungspreis (MARC nicht im Service).
   - SAP-Team-Anfrage: RESB (`Matnr, Werks, Bdmng offene Reservierungsmenge, Bdter`) und MARC
     (`Matnr, Werks, Dispo, Eisbe`).
   - MARC loest zugleich Aufriss 2 dauerhaft (ersetzt die ZC23-CSV aus Phase 1.4).

4. **Standorte-uebergreifend (Marcos Phase 2):**
   - Erst nach Abnahme der CH-Sicht. Bukrs/Werks sind bereits im Cache; Architektur (`Sites`)
     ist vorbereitet. Keine SAP-Anfrage noetig, nur fachlicher Scope-Entscheid.

---

## F. Was zurueckkommen muss, damit es weitergeht

| Von | Was | Schaltet frei |
|---|---|---|
| Ingo | `warengruppen_texte.csv` (T023T) | Phase 1.3 Warengruppen-Texte + Aufriss 3 |
| Ingo | `disponenten.csv` (ZC23) | Phase 1.4 Aufriss 2 + Disponenten-Filter 001-005 |
| Ingo | Ergebnisse der 4 OData-Proben (Abschnitt D) | Phase 2 Region/Elikz/MBEW/Kontrakte |
| Ingo | Fremdwaehrungsbeleg + Soll-CHF | K1-Verifikation (WKURS-Richtung) |
| Marco | Soll-Zahlen (18 Mio, Monat+Lieferant vs. Power BI) | Abnahme Phase 0 |
| Marco | QM-Transaktion + Soll-Spalten | Phase 3.2 Reklamationsquote |
| Marco | Termintreue-Toleranzregel | Phase 3.1 Liefertermintreue |
| SAP-Team (via Ingo) | EKBE, RESB, MARC, Kontraktbelege im Service | Phase 3.1/3.3, Phase 2.4/2.5 |

Sobald CSVs bzw. OData-Probe-Ergebnisse da sind, baue ich Phase 1.3/1.4 (Referenzlisten-Import +
Join + Filter) und die freigeschalteten Phase-2-Punkte ein — jeweils mit Tests und Doku-Nachtrag.
