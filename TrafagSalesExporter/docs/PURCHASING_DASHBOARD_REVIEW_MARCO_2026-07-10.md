# Einkaufsdashboard: Review-Mail Marco 2026-07-10 — Mapping und Massnahmen

Quelle: Mail Marco Widmer (Einkaufs-Koordinator) vom 2026-07-10 nach Durchsicht des produktiven
Cockpits. Dieses Dokument mappt jeden Punkt gegen den Code-Stand, dokumentiert die sofort
umgesetzten Korrekturen und fuehrt die groesseren Punkte als Arbeitsauftrag weiter
(ergaenzt `PURCHASING_DASHBOARD_UMSETZUNGSPLAN_MARCO_2026-07-09.md`, ersetzt ihn nicht).

Wichtig fuer die Einordnung: Marco sah den **alten produktiven Stand**. Mehrere seiner Punkte
waren lokal bereits gefixt, aber noch nicht deployed (Beleg-Mix-Trennung, Elikz, ueberfaellige
Positionen, Artikel-Preistrend). Der Abschnitt "Bereits gefixt vor dem Review" unten haelt das
fest, damit beim naechsten Termin nicht Doppeltes diskutiert wird.

---

## A. Sofort umgesetzt (diese Session, 157/157 Tests gruen, noch kein Deploy)

### A1. "Verpflichtungen" ist jetzt Stand-heute und zeitraumunabhaengig
- Marco: "muesste zeitraumunabhaengig sein und die Stand heute noch offenen Kontrakt- und
  Bestellwerte summieren."
- Vorher: Kachel zeigte den Konnr-Kontrakt-Restwert; offene Werte hatten eine Von-Untergrenze
  (`Eindt >= Von`), alte Rueckstaende fielen raus.
- Neu: Offene Positionen sind komplett **zeitraumunabhaengig** (weder Von- noch Bis-Grenze);
  die Kachel `Verpflichtungen` zeigt den offenen Bestell-/Abrufwert Stand heute
  (`OpenValueSample`) mit entsprechendem Untertitel. Der Zeitraumfilter wirkt nur noch auf
  Vergangenheits-KPIs (Spend, Bestellungen, Positionen).

### A2. Loeschkennzeichen wirkt nicht mehr auf den historischen Spend
- Marco: "Auch wenn heute ggf. ein Artikel Status 99 hat, wurde er vielleicht 2023 eingekauft
  und hat daher seine Berechtigung am Anteil Spend." — fachlich korrekt.
- Neu: Zwei getrennte Positionsfilter.
  - **Spend/Historie:** nur stornierte Positionen (`EKPO.Loekz`) bleiben draussen (nie
    beschafft); der heutige Materialstatus MARA-MSTAE 98/99 filtert NICHT mehr.
  - **Offene Werte/Zulauf:** weiterhin Loekz UND MSTAE 98/99 raus (fuer kuenftige Lieferungen
    ist ein heute auslaufendes Material relevant).
- Checkbox-Label und Statuszeile entsprechend praezisiert.

### A3. Kachel-Beschriebe EKPO/EKET
- Neu: `EKPO Positionen` -> "Anzahl Bestellpositionen im Zeitraum <Zeitraum>";
  `EKET Termine` -> "Anzahl Termineinteilungen im Zeitraum <Zeitraum>". (Marcos Formulierung.)

### A4. Register Lieferanten reagiert jetzt auf den Zeitraum
- Marco: "Scheinbar keine Funktion der Zeitraumeinschraenkung."
- Ursache gefunden: Das Chart zeigte hart den Spend des AKTUELLEN Jahres
  (`CurrentYearSupplierSpendRows`), unabhaengig vom Filter.
- Neu: Chart zeigt die Top-Lieferanten nach Spend im gewaehlten Zeitraum (`SpendChartRows`);
  Titel entsprechend umbenannt.

---

## B. Bereits gefixt vor dem Review (lokal, wartet auf Deploy + Full Load)

| Marco-Punkt | Status im lokalen Stand |
|---|---|
| "Register Kontrakte: Daten beziehen sich offenbar auf Bestellungen" | Beleg-Mix-Trennung umgesetzt (`Bstyp`/`Bsart`): Anfragen/Kontrakte/Umlagerungen raus aus Spend/Offen; Kontrakt-KPI als Konnr-Abruf-Naeherung gekennzeichnet. Wirkt real erst nach P-Modell-Rollout (`Bstyp/Bsart/Elikz` fehlen auf travp762 noch) + Full Load. |
| Offene Bestellungen = "erfasst, aber noch nicht geliefert" | Genau die implementierte Definition (`EKET.Menge > Wemng`), seit M7 zusaetzlich ohne endgelieferte Positionen (`Elikz='X'`, entfernt ~7.46 Mio Scheindeckung). |
| Zahlen-Verifikation Spend | CHF-Bewertung ueber `Waers`/`Wkurs` umgesetzt (65% der Belege sind EUR!); produktiv noch alter Stand. |

---

## C. Geplante Features (aufgenommen / bestaetigt durch dieses Review)

### C1. Termintreue-Kachel (Einkauf Dashboard) — Phase 3.1, Prioritaet durch Marco bestaetigt
- Berechnung auf Ebene EKET/EKBE: Abweichung statistisches Lieferdatum vs. WE-Buchungsdatum.
- `EKBESet` ist im OData-Service vorhanden (97'193 WE-Zeilen verifiziert); Loader noch zu bauen.
- **Von Marco noetig:** die Bewertungsformel der bestehenden Lieferantenbewertung
  (Toleranzklassen/Punkteschema), damit die Kachel dieselbe Sprache spricht.

### C2. Spend-Drilldown mit Auswahlfeldern — Phase 1/2
- Auswahlfelder Lieferant, Warengruppe, Artikel im Register Spend.
- Zusaetzlich Selektion nach Produktegruppe Trafag ueber Disponentengruppe: `MARCSet` (Dispo)
  ist im Service verfuegbar -> Loader + Referenz (T024D-Namen aus Analyse-Report vorhanden);
  ZC23-CSV als Uebergang nicht mehr noetig.

### C3. Register Offene Bestellungen: Filter "Lieferdatum bis" + Dimensionen
- Grundsemantik jetzt korrekt (Stand heute offen, siehe A1). Als Ausbau: optionale Eingrenzung
  "Lieferdatum bis" sowie Auswahl nach Lieferant/Warengruppe/Produktegruppe/Artikel.

### C4. Register Kontrakte: echte Mengenkontrakte
- Ziel: Stand heute eroeffnete Mengenkontrakte (`Bstyp='K'`) mit offenen Kontraktmengen
  (`Ktmng` vs. Abrufe via `EKAB`/`Konnr`), Selektion "Laufzeitende bis".
- Datenlage: `Ktmng` liefert P bereits; `Bstyp` muss ins P-Modell; Laufzeit: `Kdatb` ist im
  EKKO-Modell vorhanden, **`Kdate` (Laufzeitende) fehlt** -> mit ins Modell aufnehmen.
- Bis dahin bleibt die Konnr-Abruf-Naeherung sichtbar und als solche beschriftet.

### C5. Lieferanten-Factsheet + Vergleich ("wie Galaxus")
- Bei Auswahl eines Lieferanten ein Uebersichtsblatt: Umsatzentwicklung, Preisentwicklung
  (Ø-Preis/Stk = Umsatz/gelieferte Menge), Qualitaet, Termintreue — als Gespraechsgrundlage.
- Ausbaustufe: Vergleich von 2+ Lieferanten nebeneinander.
- Datenbasis: Spend/Preis vorhanden; Termintreue braucht C1 (EKBE); Qualitaet braucht QM-Quelle
  (`qmel*`-Sets im Service vorhanden, Phase 3.2). Vorschlag: Factsheet-MVP zuerst mit
  Umsatz+Preis, Qualitaet/Termintreue als "in Aufbau" gekennzeichnet.

---

## D. Antworten auf Marcos Fragen (fuer die Rueckmeldung)

1. **"Was macht die Anzeige Verpflichtungen?"** — Bisher: offener Restwert der Abrufe zu
   Rahmenkontrakten im gefilterten Zeitraum (Konnr-Naeherung); durch die Von-Grenze fachlich
   verzerrt. Neu (nach naechstem Deploy): offener Bestell-/Abrufwert Stand heute,
   zeitraumunabhaengig — genau die von dir erwartete Semantik. Die Trennung "nur echte
   Mengenkontrakte" folgt mit C4.
2. **"Funktion Loeschkennzeichen unklar"** — Berechtigter Einwand; umgesetzt wie von dir
   beschrieben (A2). Storno-Positionen bleiben im Spend draussen, weil sie nie beschafft wurden.
3. **"Datenaggregierung offene Bestellungen unklar"** — Definition: erfasste, noch nicht
   (voll) gelieferte Bestellpositionen Stand heute; endgelieferte (`Elikz`) zaehlen nicht.

---

## E. Naechste Schritte

1. Deploy des Gesamtstands + Full Load (nach P-Modell-Rollout `Bstyp/Bsart/Elikz`; siehe
   `PURCHASING_DASHBOARD_VORBEREITUNG_INGO_2026-07-09.md` Abschnitt A0) — dann sieht Marco
   A1-A4 plus alle B-Punkte.
2. Marco: Bewertungsformel Lieferantenbewertung (C1) und Prioritaet C2-C5 nennen.
3. Ingo: `Kdate` (+ ggf. `EKAB`-Feldumfang) mit ins P-Modell nehmen (C4).
4. Umsetzung C1-C5 nach Prioritaet, jeweils mit Tests und Doku-Nachtrag.
