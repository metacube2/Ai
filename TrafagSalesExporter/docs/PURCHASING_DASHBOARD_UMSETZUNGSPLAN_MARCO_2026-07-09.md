# Einkaufscockpit: Umsetzungsplan Anforderungen Marco (2026-07-09)

Zweck: Diese Datei ist der Arbeitsauftrag fuer die naechsten Sessions (Modell: Opus). Grundlage
ist die Anforderungs-Mail von Marco (Einkauf) vom 2026-07-09 nach dem gemeinsamen Review vom
2026-07-08. Jede Anforderung ist gegen den tatsaechlichen Code- und Datenstand gemappt
(machbar sofort / braucht SAP-Erweiterung / braucht Klaerung) und in Phasen priorisiert.

Referenzdokumente:

- `docs/PURCHASING_DASHBOARD_2026-06-05.md` (laufende Hauptdoku, Nachtraege bis 2026-07-08)
- `docs/PURCHASING_DASHBOARD_KORREKTUREN_2026-07-06.md` (Formel-Review K1-K6/M7-M10; K1-K6, M8-M10
  umgesetzt, 139/139 Tests gruen, noch nicht deployed)

Arbeitsregeln (aus persona.md / bisheriger Praxis):

- Nach jeder Aenderung: `dotnet test TrafagSalesExporter.sln --verbosity minimal` muss gruen sein.
- Neue Logik mit Tests absichern (analog `PurchasingDashboardServiceTests`).
- `docs/PURCHASING_DASHBOARD_2026-06-05.md` per Nachtrag aktualisieren, nicht umschreiben.
- Kein Deploy ohne Ruecksprache mit Ingo. Schema-Aenderungen nur ueber die bestehende
  Schema-Maintenance (Spalten ergaenzen, keine Migration).
- Neue OData-Felder NIE blind in `$select` aufnehmen: zuerst `$metadata` bzw. `$top=1` pruefen
  (Praezedenz: `Bsart`/`Meins` existierten nicht -> HTTP 400).
- Marco's eigener Vorschlag gilt als Leitplanke: **mit einer Sicht anfangen, dann Schritt fuer
  Schritt ausbauen.** Nicht alles parallel anreissen.

---

## Ist-Stand Datenbasis (verifiziert im Code, Stand 2026-07-09)

Geladen via `PurchasingDataRefreshService` (Full Load + Delta):

| Quelle | Felder | Cache |
|---|---|---|
| `EKKOSet` | Ebeln, Bedat, Aedat, Lifnr, Bukrs, Konnr, Waers, Wkurs | `PurchasingEkkoCache` (+ SupplierName aus LFA1) |
| `EKPOSet` | Ebeln, Ebelp, Matnr, Txz01, Matkl, Menge, Ktmng, Netwr, Loekz, Bukrs, Werks | `PurchasingEkpoCache` (+ Mstae aus MARA) |
| `eketSet` | Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng | `PurchasingEketCache` |
| `MARA001Set` | Matnr, Mstae | Status-Map -> EKPO.Mstae |
| `LFA1Set` | Lifnr, Name1 | Namens-Map -> EKKO.SupplierName |

Im Service `ZPOWERBI_EINKAUF_SRV` laut Metadaten-Befund 2026-07-02 zusaetzlich vorhanden, aber
noch nicht angebunden: `mbew` (MBEW, u.a. STPRS Standardkosten) und `KNA1`.

Nicht im Service vorhanden (bekannt): `EKKO.Bsart`, `EKPO.Meins`. Ungeprueft: `EKPO.Elikz`,
LFA1-Adressfelder (`Land1`, `Regio`, `Ort01`), MARC-Felder (`Dispo`, `Eisbe`), EKBE, RESB, QMEL.

---

## Anforderungs-Mapping: Aufrisse (Dimensionen)

| # | Aufriss (Marco) | Status | Datenbasis / Weg |
|---|---|---|---|
| 1 | Zeitperiode | **vorhanden** | Von/Bis Monat auf `EKKO.Bedat`; offene Positionen seit K3 mit eigener Periode |
| 2 | Verwendung / Produktgruppe ueber Disponentengruppe (ZC23-Tabelle von Ingo) | **fehlt — Referenzliste** | EKPO hat kein Dispo-Feld. Weg A: `MARC.Dispo` via OData (Metadaten-Check noetig). Weg B (pragmatisch): ZC23-Disponentengruppen-Tabelle als CSV/Excel-Referenzliste importieren und ueber `Matnr`/`Werks` joinen. Ingo liefert die Tabelle. |
| 3 | Materialgruppe | **teilweise** | `EKPO.Matkl` (Code) vorhanden; **Texte fehlen** (PBIX nutzte `Data (2).WG komplett`, EntitySet 404). Loesung: Referenzliste (T023T-Export) als Upload-Tabelle `PurchasingMaterialGroupRef`, Fallback Code. |
| 4 | Kreditor | **vorhanden** | `Lifnr` + LFA1-`Name1` (produktiv Full Load noetig, siehe Phase 0) |
| 5 | Beschaffungsregion | **fehlt — SAP-Check** | LFA1-Adresse: pruefen ob `LFA1Set` `Land1`/`Regio`/`Ort01` liefert. Falls ja: Spalten in EKKO-Cache bzw. Lieferanten-Map ergaenzen. Region = Land, spaeter gruppierbar (EU/Asien/...). |
| 6 | Materialnummer | **vorhanden** | `Matnr` + `Txz01` |
| 7 | ABC-/XYZ | **Weg geklaert, spaeter** | Update Feedback-Runde 2026-07-17: ABC-Kennzeichen = `MARC-MAABC` (Sicht O2); XYZ liegt in separater Tabelle; vorhandener SAP-Report kann beides extrahieren. Umsetzung erst nach Abnahme Spend-Reiter (Marcos Ein-Punkt-nach-dem-anderen-Regel). |

## Anforderungs-Mapping: Kennzahlen Beschaffungstransaktionen (Disponenten 001-005)

| KPI (Marco) | Status | Bemerkung |
|---|---|---|
| Kreditorenumsatz [CHF] | **vorhanden** | Spend je Lieferant inkl. CHF-Bewertung (K1). WKURS-Richtung noch gegen echten Fremdwaehrungsbeleg verifizieren. |
| Offene Bestellungen [CHF] nach Liefertermin | **vorhanden (nach K3)** | Offener Bestellwert mit Faelligkeitssicht; M7 (`Elikz`) offen -> Werte tendenziell zu hoch. |
| Ueberfaellige Lieferpositionen | **teilweise** | Risiko-Bucket `Ueberfaellig` existiert. Ausbau: eigene KPI-Karte + Drilldown-Liste (Beleg, Position, Lieferant, Eindt, offene Menge/Wert). Nur vorhandene Daten -> sofort machbar. |
| Offene Mengenkontrakte [CHF] nach Ablaufdatum | **fehlt — SAP-Erweiterung** | K4 grenzt Abrufe ueber `Konnr` ab. Echte Kontraktbelege (Bstyp='K') mit Laufzeit `KDATB`/`KDATE` liefert der Service nicht. SAP-Team: Kontraktkoepfe/-positionen (oder `Bstyp`/`Bsart` + `Kdate`) in den Service aufnehmen. Bis dahin: Konnr-Abrufsicht als Naeherung, in UI als solche gekennzeichnet. |
| Disponenten-Filter 001-005 | **fehlt** | Haengt an Aufriss 2 (Dispo je Material). Gleiche Loesung (MARC oder ZC23-Referenzliste). |

## Anforderungs-Mapping: Lager [Periodenende]

| KPI (Marco) | Status | Bemerkung |
|---|---|---|
| Lagerbestand [CHF] Stichtag | **fehlt — mittel** | `mbew` ist im Service vorhanden (Nebenbefund 2026-07-02). MBEW liefert aktuellen Bestandwert (`Lbkum`/`Salk3`), aber **nur aktuell**, keine Stichtags-Historie. Historik braeuchte MBEWH. Phase-2-Vorschlag: aktuellen Bestand anbinden, Stichtags-Historie selbst aufbauen (taeglicher/monatlicher Snapshot in eigene Tabelle ab Anbindung). |
| Feste Zugaenge [CHF] (offene Bestellungen) | **vorhanden** | = offener Bestellwert/Zulauf (K3). |
| Feste Abgaenge [CHF] (Reservationen, Sekundaerbedarfe) | **fehlt — SAP-Erweiterung** | RESB nicht im Service. SAP-Team anfragen. Phase 3. |
| Sicherheitsbestand [CHF] | **fehlt — SAP-Check** | `MARC.Eisbe` x Bewertungspreis (MBEW.Stprs/Verpr). MARC nicht im Service -> pruefen/anfragen. Phase 2/3. |

## Anforderungs-Mapping: Lieferantenperformance

| KPI (Marco) | Status | Bemerkung |
|---|---|---|
| Liefertermintreue [%] | **fehlt — SAP-Erweiterung** | Braucht Ist-Wareneingangsdatum je Position. EKET hat nur `Wemng` (Menge), kein WE-Datum. Sauber: `EKBE` (Bestellentwicklung, BEWTP='E', `Budat`) in den Service aufnehmen. Vergleich `EKBE.Budat` vs. `EKET.Eindt`. Ohne EKBE nicht seriös berechenbar — nicht naehern/simulieren. |
| Qualitaets-/Reklamationsquote [%], PPM | **fehlt — alternativer Weg** | QM-Meldungen (QMEL/QMFE) nicht im Service. Pragmatischer Weg analog Florian Waechter: automatisierter CSV-Export aus SAP-QM, Import als eigene Cache-Tabelle. Einkauf muss Transaktion/Soll-Spalten benennen (Punkt aus Review 2026-07-08). |
| Preisentwicklung | **vorhanden (nach M9)** | Mengengewichteter Durchschnitts-Stueckpreis je Jahr. Ausbau Phase 1: Serie je Artikel (Top-N nach Spend) analog PBIX-Vorlage. |
| Bestehender Performance Score | **klaeren** | Marco prueft, ob der Einkauf die Kennzahl braucht (Memo Lieferantenbewertung). Nicht weiter ausbauen bis Rueckmeldung. |

---

## Phasenplan

### Phase 0 — Fundament sichern (vor allem Neuen; sofort)

1. **Deploy des Korrektur-Stands** (K1-K6, M8-M10; 139/139 gruen) nach Freigabe durch Ingo.
2. **Einkauf-Full-Load** danach zwingend (fuellt `Waers`/`Wkurs`/`Konnr` + LFA1-Namen real). ERLEDIGT 2026-07-17: Load lief erfolgreich (nach Fix des zwischenzeitlich am `MARA001Set`-404 gescheiterten Loads vom 02.07.), `SupplierName` zu 99.99 % gefuellt, Stichprobe verifiziert.
   Grosse Loads lokal gegen DB-Kopie fahren, dann DB auf Server (siehe Hauptdoku, Abschnitt
   Server-Restore; WAL/SHM-Sidecars beachten).
3. **Abnahme-Checks aus dem Review 2026-07-08:** 18-Mio-Offenwert gegen SAP verifizieren,
   Lieferantenname statt Nummer im Lieferanten-Register, Zeitraumfilter-Verhalten.
4. **WKURS-Richtung** gegen einen echten Fremdwaehrungsbeleg pruefen (offener Punkt aus K1).

### Phase 1 — Beschaffungstransaktions-Sicht schaerfen (nur vorhandene Daten, kein SAP-Change)

Das ist die "eine Sicht" im Sinne von Marco: Kreditorenumsatz + offene Bestellungen + Ueberfaellige.

1. **Ueberfaellige Lieferpositionen** als eigene KPI-Karte + Drilldown-Tabelle
   (Lieferant, Beleg/Position, Artikel, Eindt, offene Menge, offener Wert CHF; Sortierung nach
   Wert). Datenbasis: EKET `Eindt < heute` und `Menge > Wemng`, aktiver Positionsfilter.
2. **Preisentwicklung je Artikel** (Top-N-Artikel nach Spend als Serien, Variante a aus M9) —
   entspricht der PBIX-Vorlage und Marcos "Kosten aktuell nur auf Artikelebene moeglich".
3. **Warengruppen-Texte via Referenzliste:** Upload-Tabelle (Matkl -> Text), Admin-Upload analog
   bestehender Referenzpflege; Anzeige `Code - Text`, Fallback Code. Ingo/Einkauf liefern
   T023T-Export als CSV.
4. **Disponentengruppen-Referenzliste (ZC23):** Upload-Tabelle (Matnr/Werks -> Disponent,
   Disponentengruppe), Join auf EKPO. Damit Aufriss 2 + Filter Disponenten 001-005 ohne
   SAP-Change. Ingo liefert die ZC23-Tabelle.
5. **UI-Konsistenz:** Kontrakt-KPI als "Abrufe zu Kontrakten (Naeherung)" kennzeichnen, solange
   keine echten Kontraktbelege angebunden sind (K4-Hinweis).

### Phase 2 — Gezielte SAP-Erweiterungen (je ein Metadaten-Check, kleine Eingriffe)

Reihenfolge nach Nutzen/Aufwand:

1. **LFA1-Adressfelder** (`Land1`, `Regio`, `Ort01`) -> Aufriss 5 Beschaffungsregion.
   Check: `LFA1Set?$top=1&$select=Lifnr,Name1,Land1`. Bei Erfolg: Namens-Map um Land erweitern,
   Spalte `SupplierCountry` in `PurchasingEkkoCache`, Region-Gruppierung im Dashboard.
2. **`EKPO.Elikz`** (M7 aus dem Korrektur-Review) -> Offen-Logik korrigieren
   (`AND COALESCE(Elikz,'') <> 'X'`). Wichtig VOR dem 18-Mio-Soll-Abgleich, da es offene Werte
   senkt.
3. **`mbew` anbinden** -> Lagerbestand CHF aktuell + Standardkosten (STPRS ist zugleich die
   fehlende Quelle fuer die offene Finance-Gruppenmarge, Doppelnutzen). Snapshot-Tabelle fuer
   kuenftige Stichtagsbetrachtung gleich mitbauen (Periodenende-Werte ab jetzt historisieren).
4. **Kontraktbelege** beim SAP-Team anfragen: `Bstyp`/`Bsart`, Kontrakt-Laufzeit (`Kdatb`/`Kdate`),
   Zielwert -> echte "Offene Mengenkontrakte [CHF] mit Ablaufdatum". Loest auch K4-Zusatz
   (Umlagerungen UB aus Spend abgrenzen).
5. **MARC** (`Dispo`, `Eisbe`) anfragen -> ersetzt mittelfristig die ZC23-Referenzliste aus
   Phase 1 und liefert Sicherheitsbestand.

### Phase 3 — Neue Datenquellen / groessere Themen (fachliche Vorklaerung noetig)

1. **Liefertermintreue:** EKBE in den Service aufnehmen lassen (BEWTP='E', Budat, Menge).
   Kennzahl: Anteil puenktlicher WE-Positionen (Toleranzfenster mit Einkauf definieren).
2. **Qualitaet/Reklamation/PPM:** CSV-Export-Weg aus SAP-QM (Analogie Florian Waechter).
   Voraussetzung: Einkauf benennt Transaktion und Soll-Spalten (offener Punkt Review 2026-07-08).
3. **Feste Abgaenge** (RESB: Reservationen, Sekundaerbedarfe) -> komplette Lager-Periodenende-Sicht
   (Bestand + Zugaenge + Abgaenge + Sicherheitsbestand).
4. **Standorte-uebergreifend** (Marcos Phase 2): erst wenn Trafag-CH-Sicht abgenommen ist.
   Architektur ist vorbereitet (`Sites`, Bukrs/Werks im Cache).

---

## Offene Klaerungen (nicht Code — Ingo/Marco/SAP-Team)

| Punkt | Wer | Blockiert |
|---|---|---|
| ZC23-Disponentengruppen-Tabelle als CSV liefern | Ingo | Phase 1.4 |
| Warengruppen-Textliste (T023T-Export) liefern | Ingo/Einkauf | Phase 1.3 |
| Soll-Zahlen fuer Gegenpruefung (18 Mio offener Wert, ein Monat+Lieferant vs. Power BI) | Marco | Abnahme Phase 0 |
| Fachliche Definition Kontrakte (nur offene Kontrakte, keine Vermischung mit Bestellungen) | Marco + Ingo | Phase 2.4 |
| SAP-Service-Erweiterung: Kontraktfelder, MARC, EKBE, RESB | SAP-Team (Anfrage durch Ingo) | Phase 2.4/2.5, Phase 3 |
| QM-Transaktion + Soll-Spalten fuer Reklamationsquote | Marco/Einkauf | Phase 3.2 |
| Braucht der Einkauf den bestehenden Performance Score? | Marco (Memo) | — |
| ABC-/XYZ-Analyse | Ingo (nach Spend-Abnahme) | Weg klar seit 2026-07-17: MARC-MAABC (O2) + XYZ-Tabelle via vorhandenem Report |
| SAP-Erweiterung: `Matkl` in `maracalc` aufnehmen (MARA-MATKL fuer Spend-Drilldown) | SAP-Team (Anfrage durch Ingo) | Warengruppen-Drilldown zeigt bis dahin Beleg-Matkl |
| WKURS-Richtung an echtem Fremdwaehrungsbeleg | Ingo (SAP-Zugriff) | Phase 0.4 |

---

## Abnahme-Checks je Phase

- Immer: `dotnet test TrafagSalesExporter.sln --verbosity minimal` gruen; Doku-Nachtrag in
  `PURCHASING_DASHBOARD_2026-06-05.md`.
- Phase 0: 18-Mio-Offenwert plausibilisiert; Lieferantennamen sichtbar; Full-Load-Statusmeldung
  zeigt LFA1-/MARA-Zaehler > 0.
- Phase 1: Ueberfaellige-Liste stichprobenartig gegen SAP (ME2M/ME2L) pruefen; Disponenten-Filter
  001-005 reduziert Spend nachvollziehbar; Warengruppen zeigen Texte.
- Phase 2: Region-Aufriss summiert identisch zum Lieferanten-Aufriss; offener Wert nach Elikz-Fix
  erneut gegen Soll; Lagerbestand CHF gegen MB52/MBEW-Summe.
- Phase 3: Termintreue an 2-3 bekannten Lieferanten gegen SAP-Auswertung validieren, Toleranz
  mit Einkauf abgestimmt.
