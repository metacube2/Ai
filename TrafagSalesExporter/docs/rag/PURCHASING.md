# RAG Einkauf

Stand: 2026-07-22

Kurzdatei fuer das Einkaufs-Dashboard (Spend, offene Bestellungen, Kontrakte, Lieferanten).
Detail-/Historien-Doku: `docs/PURCHASING_DASHBOARD_2026-06-05.md` (Hauptdoku mit Nachtraegen).

## Kurzstand

- NEU 2026-07-23: Reiter `Spend` hat jetzt ein zweites Balkendiagramm "Volumen nach Warengruppe"
  (PowerBI-Seite "Diagramm Vol./WG"). Bewusst im Spend-Reiter (Volumenanalyse), nicht bei
  Lieferanten (Bewertung/Performance). Gleiche COALESCE-WG-Logik und gleicher Zeitraum-/
  Spend-Filter wie die bestehende Lieferant-Matrix. Mit ehrlichem UI-Hinweis zur Datenlage (WG
  aktuell aus Beleg, MARA-MATKL fehlt noch, siehe offener Punkt). Rein C#/Razor, kein SAP-
  Roundtrip. Power-BI-Gegenstuecke, die bewusst NICHT nachgebaut wurden: Kuchen Lieferant (durch
  Top-Lieferanten-Balken abgedeckt), Kuchen Region (Lieferantenland fehlt im Cache - LFA1 laedt
  nur Name1, nicht Land1; waere ein eigener SAP-/Cache-Ausbau).
- KONFIGURATION GEAENDERT 2026-07-22: Zentrale SAP-URL (`SourceSystemDefinitions.CentralServiceUrl`,
  Code `SAP`) von `travt762` (TEST) auf `travp762` (PROD) umgestellt (Anlass: Logistik/
  Stuecklistenanalyse brauchte echte Daten). Betrifft Einkauf (Site `PURCHASING_SAP`, kein eigener
  Override) — Finance CH/AT (`ZSCHWEIZ`) hat einen eigenen Site-Override und bleibt auf travt762.
  Details/Backup-Pfad: `lastchange.md`. VOR DEM NAECHSTEN EINKAUF-FULL-LOAD: mit Marco/Andreas
  abstimmen (laufende 18-Mio-Abnahme, Datenbestand wechselt), 401-Auth-Status gegen travp762 neu
  pruefen (zuletzt 2026-07-09 fehlgeschlagen), `Bstyp`/`Bsart`/`Elikz`-Verfuegbarkeit auf P
  gegenchecken (zuletzt 2026-07-10 gefehlt).
- BLOCKER GEFUNDEN 2026-07-22: Erster Full-Load-Versuch gegen travp762 brach fuer ALLE EntitySets
  (auch `EKKOSet`) mit `SYNTAX_ERROR` ab — Ursache NICHT Auth/Feldmodell, sondern die
  ZLO03-DPC_EXT-Methoden vom 2026-07-21, die aus der auf P nicht existierenden Tabelle `ZAT_VC`
  lasen und damit die ganze DPC_EXT-Klasse des Service unkompilierbar machten. Einkauf-Cache blieb
  dank Guardrail unveraendert (Stand 2026-07-17). Korrigierte Methodenruempfe (Quelle jetzt
  `ZPOWERBI_VC_TXT`) liegen bereit; erst nach manuellem Einfuegen/Aktivieren auf P (+
  `/IWFND/CACHE_CLEANUP`) sind Loads gegen P wieder moeglich. Details:
  `docs/abap/README_LZCODE_WEBSERVICE.md` Nachtrag 2026-07-22.
- Arbeitsweise (Marco, Feedback-Runde 2026-07-17): EIN Punkt nach dem anderen fertig machen — aktuell Reiter `Spend`; naechster Reiter erst nach Marcos Abnahme.
- DEPLOYED 2026-07-17 (Commit `3a4efb5`, `257/257` Tests): Spend-Drilldown Lieferant -> Warengruppe/Jahr in der Matrix `Kaskadierung Lieferant / Jahr` (Pivot-artig aufklappbar, Drill-Summen exakt = Lieferantenzeile, Zeitraumfilter wirkt auf beide Ebenen). Warengruppe nach Marcos Vorgabe aus dem MATERIALSTAMM (`MARA-MATKL`, neue additive Cache-Spalte `PurchasingEkpoCache.MaraMatkl`), Fallback Beleg-Warengruppe mit UI-Hinweis — `Matkl` ist aktuell in KEINEM MARA-EntityType des SAP-Service; SAP-Erweiterungsanfrage: `maracalc` um `Matkl` ergaenzen, danach app-seitig nur `$select` erweitern.
- PRODUKTIONSKRITISCHER FIX 2026-07-17 (gleicher Deploy): SAP hat das MARA-Set umgebaut — `MARA001Set` exponiert `Mstae` nicht mehr (`$select` -> 404); der Einkauf-Full-Load/Delta waere beim naechsten Lauf komplett fehlgeschlagen (so geschah es am 02.07., unbemerkt). `LoadMaterialStatusMapAsync` liest jetzt das neue `maracalcSet`; Achtung: das Set ignoriert `$top`/`$skip` (wie `mbewSet`) — bewusst EIN ungepagter Request.
- PRODUKTIVDATEN 2026-07-17: Full Load erfolgreich (`EKKO=172'914, EKPO=234'083, EKET=242'734, MARA-Status=67'665, LFA1-Namen=6'747`). `SupplierName` 99.99 % gefuellt — Spend-Matrix zeigt jetzt NAMEN statt Nummern (verifiziert: `66952 -> BEPRO AG`). Vorher 0 %: der einzige Load seit dem LFA1-Fix (02.07.) war am MARA-404 gescheitert, BEVOR er LFA1 erreichte.
- Spend-Regeln (Marco-Review, deployed 10.07.): Beleg-Mix-Trennung (nur echte Bestellungen `Bstyp F`, `Bsart <> UB`); historischer Spend filtert NICHT nach heutigem Materialstatus (nur stornierte Positionen `Loekz` raus); offene Werte/Verpflichtungen sind Stand-heute und zeitraumunabhaengig, schliessen MSTAE 98/99 und `Elikz='X'` aus; CHF-Bewertung ueber `Waers`/`Wkurs`.

## Offene Punkte

- WG-DATENLAGE 2026-07-23 (an Prod-Cache gemessen, Stand Load 17.07.): Warengruppe im
  Einkauf-Cache faktisch unbrauchbar bis zur SAP-Erweiterung. `MaraMatkl` zu 0 % gefuellt
  (0/234'083), `Matkl` (Beleg-WG) zwar 99,98 % gefuellt, ABER 233'048/234'046 (99,6 %) in der
  Sammelgruppe `01`. Das neue "Volumen nach Warengruppe"-Diagramm (siehe Kurzstand) zeigt daher
  aktuell fast nur eine Saeule; strukturell korrekt, aussagekraeftig erst nach `Matkl` im
  `maracalcSet`. Verstaerkt den bestehenden offenen Punkt "Matkl in maracalc aufnehmen".
- ERLEDIGT 2026-07-22: Zentrale SAP-Quelle Einkauf zeigt jetzt auf `travp762` (Prod) statt `travt762` (Test) — siehe Kurzstand oben. Full Load mit Marco/Andreas abstimmen, bevor er gefahren wird. Zusatzrisiko travp762 weiterhin offen: `Bstyp`/`Bsart`/`Elikz` fehlten dort zuletzt im OData-Modell (Probe 2026-07-10), 401-Auth-Status zuletzt 2026-07-09 ungeklaert — beides vor dem naechsten Full Load neu pruefen.
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
