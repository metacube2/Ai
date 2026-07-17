# RAG Einkauf

Stand: 2026-07-17

Kurzdatei fuer das Einkaufs-Dashboard (Spend, offene Bestellungen, Kontrakte, Lieferanten).
Detail-/Historien-Doku: `docs/PURCHASING_DASHBOARD_2026-06-05.md` (Hauptdoku mit Nachtraegen).

## Kurzstand

- Arbeitsweise (Marco, Feedback-Runde 2026-07-17): EIN Punkt nach dem anderen fertig machen — aktuell Reiter `Spend`; naechster Reiter erst nach Marcos Abnahme.
- DEPLOYED 2026-07-17 (Commit `3a4efb5`, `257/257` Tests): Spend-Drilldown Lieferant -> Warengruppe/Jahr in der Matrix `Kaskadierung Lieferant / Jahr` (Pivot-artig aufklappbar, Drill-Summen exakt = Lieferantenzeile, Zeitraumfilter wirkt auf beide Ebenen). Warengruppe nach Marcos Vorgabe aus dem MATERIALSTAMM (`MARA-MATKL`, neue additive Cache-Spalte `PurchasingEkpoCache.MaraMatkl`), Fallback Beleg-Warengruppe mit UI-Hinweis — `Matkl` ist aktuell in KEINEM MARA-EntityType des SAP-Service; SAP-Erweiterungsanfrage: `maracalc` um `Matkl` ergaenzen, danach app-seitig nur `$select` erweitern.
- PRODUKTIONSKRITISCHER FIX 2026-07-17 (gleicher Deploy): SAP hat das MARA-Set umgebaut — `MARA001Set` exponiert `Mstae` nicht mehr (`$select` -> 404); der Einkauf-Full-Load/Delta waere beim naechsten Lauf komplett fehlgeschlagen (so geschah es am 02.07., unbemerkt). `LoadMaterialStatusMapAsync` liest jetzt das neue `maracalcSet`; Achtung: das Set ignoriert `$top`/`$skip` (wie `mbewSet`) — bewusst EIN ungepagter Request.
- PRODUKTIVDATEN 2026-07-17: Full Load erfolgreich (`EKKO=172'914, EKPO=234'083, EKET=242'734, MARA-Status=67'665, LFA1-Namen=6'747`). `SupplierName` 99.99 % gefuellt — Spend-Matrix zeigt jetzt NAMEN statt Nummern (verifiziert: `66952 -> BEPRO AG`). Vorher 0 %: der einzige Load seit dem LFA1-Fix (02.07.) war am MARA-404 gescheitert, BEVOR er LFA1 erreichte.
- Spend-Regeln (Marco-Review, deployed 10.07.): Beleg-Mix-Trennung (nur echte Bestellungen `Bstyp F`, `Bsart <> UB`); historischer Spend filtert NICHT nach heutigem Materialstatus (nur stornierte Positionen `Loekz` raus); offene Werte/Verpflichtungen sind Stand-heute und zeitraumunabhaengig, schliessen MSTAE 98/99 und `Elikz='X'` aus; CHF-Bewertung ueber `Waers`/`Wkurs`.

## Offene Punkte

- Zentrale SAP-Quelle Einkauf zeigt auf `travt762` (TEST) statt `travp762` (Prod) — Umstellung mit Marco/Andreas abstimmen (Datenbestand aendert sich mitten in der 18-Mio-Abnahme), nicht eigenmaechtig. Zusatzrisiko travp762: `Bstyp`/`Bsart`/`Elikz` fehlen dort noch im OData-Modell (Probe 2026-07-10) — vor einem Wechsel P-Modell-Rollout abwarten.
- SAP-Erweiterung: `Matkl` in `maracalc` aufnehmen (fuer echte Materialstamm-Warengruppe im Drilldown).
- Abnahme-Checks Marco: 18-Mio-Offenwert gegen SAP, WKURS-Richtung an echtem Fremdwaehrungsbeleg.
- ABC/XYZ: Weg seit 2026-07-17 klar (ABC = `MARC-MAABC`, Sicht O2; XYZ separate Tabelle; vorhandener SAP-Report extrahiert beides) — Umsetzung erst nach Spend-Abnahme.
- Referenzlisten von Ingo: Warengruppen-Texte (T023T-Export) und ZC23-Disponentengruppen (fuer Aufriss Verwendung/Disponenten 001-005).

## Rohquellen Nur Bei Bedarf

- Hauptdoku mit allen Nachtraegen: `docs/PURCHASING_DASHBOARD_2026-06-05.md`
- Umsetzungsplan/Phasen (Marco-Anforderungen): `docs/PURCHASING_DASHBOARD_UMSETZUNGSPLAN_MARCO_2026-07-09.md`
- Formel-Korrekturen K1-K6/M7-M10: `docs/PURCHASING_DASHBOARD_KORREKTUREN_2026-07-06.md`
- Marco-Review im Detail: `docs/PURCHASING_DASHBOARD_REVIEW_MARCO_2026-07-10.md`
- Review-Vorbereitung Ingo: `docs/PURCHASING_DASHBOARD_VORBEREITUNG_INGO_2026-07-09.md`
