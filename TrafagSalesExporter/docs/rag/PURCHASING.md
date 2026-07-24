# RAG Einkauf

Stand: 2026-07-22

Kurzdatei fuer das Einkaufs-Dashboard (Spend, offene Bestellungen, Kontrakte, Lieferanten).
Detail-/Historien-Doku: `docs/PURCHASING_DASHBOARD_2026-06-05.md` (Hauptdoku mit Nachtraegen).

## Kurzstand

- NEU 2026-07-24 (DEPLOYED, DLL 12:37, Commit `c44ae28`): Warengruppen-**Texte** (T023T) ergaenzt.
  Ingo hat den SAP-Export direkt als Listenausgabe geliefert (Sprache DE, ~72 Codes, `WGBEZ` SAP-
  seitig auf 20 Zeichen abgeschnitten, `WGBEZ60` leer). Neue Klasse
  `Services/PurchasingMaterialGroupTextCatalog.cs` (statisches Dictionary, KEIN DB-Upload/Schema-
  Aenderung, da nur ich diese Liste pflege) loest Matkl/MaraMatkl-Codes ueberall dort auf, wo sie
  als Chart-/KPI-/Kaskaden-Label erscheinen, auf "Code - Text" auf (z.B. "20.05.00 - Baelge").
  Unbekannte/kuenftige Codes bleiben roher Code - verschwinden nie, neue Zeilen einfach an die
  Liste anhaengen sobald Ingo weitere `Matkl;Wgbez`-Werte liefert. Verdrahtet an 6 Stellen:
  `MaterialGroupSpendRows`-Chart, `TopMaterialGroupLabel`-KPI, Lieferant/WG/Jahr-Drilldown
  (`ExecuteSupplierGroupYearRowsAsync`), Kaskade + Region-Kuchen im Spend-Aufriss
  (`ExecuteSpendCascadeRowsAsync`/`ExecuteRegionByMaterialGroupRowsAsync`), Live-Vorschau ohne
  Cache (`ApplyEkpoMetrics`). Der alte UI-Hinweis "MARA-MATKL liefert SAP noch nicht" in
  `PurchasingDashboard.razor` war seit dem Full Load vom 24.07. falsch (MaraMatkl 80,7% gefuellt)
  und wurde korrigiert. `277/277` Tests gruen.
- VOLLLADUNG 24.07.2026 LIVE GEPRUEFT (Kopie der Prod-DB, danach geloescht): `SupplierCountry`
  100% gefuellt (175'355/175'355), `MaraAbc` 78% klassifiziert (A 54'423/B 43'424/C 87'241),
  `MaraXyz` 65% (X 67'495/Y 32'472/Z 54'561), `MaraMatkl` 80,7% (191'356/237'217, davon nur noch
  ~15% Sammelgruppe `01`). Reiter Spend-Aufriss (Region-Kuchen, ABC, XYZ) zeigt damit jetzt echte
  Daten statt Leer-Hinweis - die Warnungen dort sind datengetrieben (`if Rows leer/unbekannt`),
  verschwinden also automatisch, kein Code-Fix noetig.
- NEU 2026-07-24 (DEPLOYED, DLL 10:47, Commit `4e7861d`): Neuer Reiter `/einkauf/aufriss` „Spend-Aufriss" (eigene
  Komponente `PurchasingSpendExplorer.razor`, Nav `purchasing-breakdown`), damit der abzunehmende
  `Spend`-Reiter unangetastet bleibt. Drei Sichten: (1) mehrstufige Kaskade Lieferant -> Warengruppe
  -> Artikel (aufklappbar, je Ebene gedeckelt `[40,15,10]` + „uebrige"-Restzeile, Elternsumme =
  Summe Kinder; nutzt vorhandene Cache-Daten -> zeigt SOFORT echte Zahlen); (2) Region-Kuchen je
  Warengruppe; (3) Volumen nach ABC/XYZ. Region/ABC/XYZ sind bis zum naechsten Full-Load leer
  (SupplierCountry/MaraAbc/MaraXyz noch 0 %), mit ehrlichen UI-Hinweisen. Aggregationen in
  `PurchasingDashboardService` (`ExecuteSpendCascadeRowsAsync`, `ExecuteRegionByMaterialGroupRowsAsync`,
  ABC/XYZ-Charts), laufen nur beim Datenladen. NICHT umgesetzt: flexible Einstiegsdimension,
  Produktgruppen-Aufriss (ZC23-Mapping fehlt). `272/272` Tests gruen.
- NEU 2026-07-23 (deployed): Nächtlicher Automatik-Lauf Einkauf = DELTA im planmässigen 03:00-Slot
  (`TimerBackgroundService.RunPurchasingDeltaAsync`, gegated auf Site `PURCHASING_SAP` IsActive,
  eigener Scope/try-catch). BEWUSST nicht im Nachhol-/Catch-up-Pfad (kein SAP-Lauf bei Deploy-
  Restart nach 03:00). Full Load bleibt MANUELL (Cache-Neuaufbau, mit Marco/Andreas abstimmen).
  Buttons "Full Load starten" + "Delta aktualisieren" jetzt auch auf `/einkauf/verbindungen`
  (Bereich "Datenladung"), zusaetzlich zum Button auf `/einkauf`. Ingo-Entscheid: "Delta nachts,
  Full auf Knopf". Haengt am selben `TimerEnabled`-Schalter wie der Finance-Nachtlauf.
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

- SAP-ERWEITERUNG ERLEDIGT 2026-07-23: `Matkl` (Materialstamm-WG) UND `Mstae` sind jetzt beide im
  `MARA001Set` (Ingo). Loader zurueck von `maracalcSet` auf `MARA001Set` umgestellt (ein Set, ein
  ungepagter Request; MARA001Set ignoriert $top/$skip/$filter, live verifiziert). `MaraMatkl` wird
  ab dem naechsten Einkauf-Full-Load aus `MARA001Set.Matkl` gefuellt. DATENLAGE Materialstamm-WG
  (an travp762 gemessen, 68'125 Materialien): 65 % leer, 24 % Dummy `01`, ~10 % echte Gruppen
  (61 distinct, z.B. TS_AUTO, 20.01.01). Wo leer, greift im Dashboard der COALESCE-Fallback auf die
  Beleg-WG (die zu 99,6 % `01` ist). Also besser als vorher (echte Gruppen erstmals sichtbar), aber
  die hohe Leerquote im Materialstamm bleibt ein SAP-Stammdaten-Thema. VOR-DEPLOY-STAND Cache:
  MaraMatkl noch 0 % (Load 17.07.), wird erst mit dem naechsten Full Load gefuellt.
- ERLEDIGT 2026-07-22: Zentrale SAP-Quelle Einkauf zeigt jetzt auf `travp762` (Prod) statt `travt762` (Test) — siehe Kurzstand oben. Full Load mit Marco/Andreas abstimmen, bevor er gefahren wird. Zusatzrisiko travp762 weiterhin offen: `Bstyp`/`Bsart`/`Elikz` fehlten dort zuletzt im OData-Modell (Probe 2026-07-10), 401-Auth-Status zuletzt 2026-07-09 ungeklaert — beides vor dem naechsten Full Load neu pruefen.
- SAP-Erweiterung: `Matkl` in `maracalc` aufnehmen (fuer echte Materialstamm-Warengruppe im Drilldown).
- Abnahme-Checks Marco: 18-Mio-Offenwert gegen SAP, WKURS-Richtung an echtem Fremdwaehrungsbeleg.
- ABC/XYZ: Weg seit 2026-07-17 klar (ABC = `MARC-MAABC`, Sicht O2; XYZ separate Tabelle; vorhandener SAP-Report extrahiert beides) — Umsetzung erst nach Spend-Abnahme.
- ERLEDIGT 2026-07-24: Warengruppen-Texte (T023T-Export) von Ingo geliefert und eingebaut, siehe
  Kurzstand oben. Offen bleibt: ZC23-Disponentengruppen (fuer Aufriss Verwendung/Disponenten 001-005).

## Rohquellen Nur Bei Bedarf

- Hauptdoku mit allen Nachtraegen: `docs/PURCHASING_DASHBOARD_2026-06-05.md`
- Umsetzungsplan/Phasen (Marco-Anforderungen): `docs/PURCHASING_DASHBOARD_UMSETZUNGSPLAN_MARCO_2026-07-09.md`
- Formel-Korrekturen K1-K6/M7-M10: `docs/PURCHASING_DASHBOARD_KORREKTUREN_2026-07-06.md`
- Marco-Review im Detail: `docs/PURCHASING_DASHBOARD_REVIEW_MARCO_2026-07-10.md`
- Review-Vorbereitung Ingo: `docs/PURCHASING_DASHBOARD_VORBEREITUNG_INGO_2026-07-09.md`
- Wuensche Einkaufssitzung 2026-07-23 (Aufriss/Drilldown, Produktgruppe via ZLO03, Region, ABC/XYZ): `docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-23.md`
