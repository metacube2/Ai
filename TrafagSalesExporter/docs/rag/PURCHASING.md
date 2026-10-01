# RAG Einkauf

Stand: 2026-10-01 (**neu Verwendung & Risiko** `/logistik/verwendung-risiko`, nur LZ-Code-Komponenten, `docs/LOGISTIK_STUECKLISTE_VERWENDUNG_RISIKO_2026-10-01.md`; **neu Logistik live** `/logistik/live`: Kommissionierung und Produktion aus SAP alle 30 s, nur bei offener Seite, ohne Personendaten, Sets `LogTaSet`/`LogLiefSet`/`LogRueckSet` in P76, `docs/LOGISTIK_LIVE_2026-10-01.md`; nach dem Auffrischen sofort alter Stand mit Hinweis `4e2b55a`; Texte D1/D5 `3407868`; vorher 2026-09-30: Rueckmeldung Armin zum Tempo, `PLATTFORM_TEMPO_2026-09-28.md` 10: viel schneller, beim Auffrischen noch langsam, Vorschlag offen; ZRL2-Konditionsart AT in P76, `docs/SAP_ZRL2_MENGENRABATT_AT_2026-09-30.md`; Codex-Reparaturen E1/E2/C1/C2 produktiv seit 10:07; Lagerwert-Verlauf und Ladezeit vom 2026-09-28; uebriger Kurzstand vom 2026-09-03)

Live-Abgleich vom Juli fuer den Einkauf-Delta-Status:
`docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`.
Das ist eine **historische Messreferenz vom Juli 2026**, kein Vorrang fuer heute: sie
kennt zum Beispiel nur Schweizer Konzernkosten. Vorrang hat nach `router.md` Regel 1 immer der
juengste direkt gepruefte Beleg zum jeweiligen Thema. Der Einkauf-Lauf ist seit dem 2026-09-28 wieder
nachgewiesen (`docs/PLATTFORM_TEMPO_2026-09-28.md`, `ISS-017`).

Kurzdatei fuer Spend, offene Bestellungen, Kontrakte und Lieferanten. Historie
und technische Details: `docs/PURCHASING_DASHBOARD_2026-06-05.md`.

## Kurzstand

- **2026-09-29: Review-Reparaturen von Codex, produktiv seit 10:07** (`eafd1c5`, `c1fdfa2`).
  Der Jahres-Spend kommt aus einer eigenen, ungekappten Jahresaggregation statt aus der Summe
  der Top 10; das Diagramm bleibt Top 10 (E1). Die Top-Warengruppe nutzt wie Diagramm und Matrix
  zuerst `MARA.MATKL` (E2). Bei unvollstaendigem Cache entstehen keine SAP-Live-Stichproben-KPIs
  mehr (C1). Fehlende Wechselkurse werden gezaehlt und ausdruecklich gewarnt; der 1:1-Rueckfall
  bleibt bis zu einem Fachentscheid, betroffene Werte gelten nicht als belastbar (C2).
  Bericht: `docs/HR_EINKAUF_REVIEW_2026-09-29.md`.
- **Geltende Betriebsregel seit 2026-09-28:** Der Snapshot gilt 60 Minuten
  (`Services/PurchasingDashboardSnapshotCache.cs`, `Lifetime`), ein abgelaufener Stand wird
  sofort geliefert und im Hintergrund neu gerechnet, beim Start wird vorgewaermt
  (`docs/PLATTFORM_TEMPO_2026-09-28.md`). Der folgende Punkt ist der Verlauf.
- Performancefix produktiv seit 2026-09-03: Alle 15 Routen des Einkaufsdashboards teilen
  pro Filter einen damals 15 Minuten gueltigen Snapshot (maximal 32 Filter, Single-Flight).
  Erfolgreiche Full- und Delta-Ladeprozesse invalidieren den Cache. Vorher brauchten selbst
  warme direkte Aufrufe von `/einkauf` und `/einkauf/aufriss` jeweils rund `9-11 s`.
  Nach der einmaligen Kaltberechnung (`83.17 s` direkt nach Neustart) lieferten alle 14
  weiteren Einkaufsrouten HTTPS `200` in `0.05-0.14 s`. Der SQLite-3.53-Planer benoetigt
  fuer die Artikelpreistrend-Abfrage eine materialisierte CTE; dies ist mit Tests und einer
  Produktionskopie abgesichert. Commits `756e931`, `2444731`; `674/674` Tests gruen.

- Direkte Produktgruppenquelle aus SAP am 2026-08-11 produktiv deployed und am
  2026-08-12 nach SAP-Aktivierung live abgeschlossen. Full Load und Delta lesen
  `ZDISPO_GRPSet` + `ZDISPO_SPARTSet`, laden atomar und verwenden keinen
  Excel-/manuellen Fallback. Live: `$metadata` HTTP 200 mit `62` Sets,
  `ZDISPO_GRPSet` `45` Zeilen, `ZDISPO_SPARTSet` `22` Zeilen. Der produktive
  Delta endete um `10:03:42 MESZ` mit `Success`; der Cache enthaelt danach
  `45` SAP-OData-Regeln und `0` Nicht-SAP-/Excel-Regeln. Offen *(ueberholt am 2026-10-01, siehe unten)*: Texte fuer `D1`
  und `D5` in `ZDISPO_SPART` pflegen und SEGW-Key von `ZDISPO_GRP` auf
  `DISPO_KZ + DISPO` korrigieren. *Stand 2026-10-01:* D1/D5 haben im Dashboard Ersatztexte (`3407868`, produktiv 11:21): D1 = `FP_DICHTESEN` (DISPO_KZ DS), D5 = `FP_DICHTESEN1/2` (DS1, DS2), Vorgabe Ingo, SAP vorerst nicht pflegen; ein spaeter in SAP gepflegter Text gewinnt. `ZDISPO_SPART` hat Auslieferungsklasse `A`, Pflege also direkt im System (SM30), kein Transport. SEGW-Key: Empfehlung, nicht umzusetzen (kein Nutzen fuer das Cockpit, Neugenerierung des Service mit Einkauf, Journal und HR noetig); Entscheid Ingo offen. Details:
  `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md`.

- Produktiv deployed und verifiziert am 2026-08-07 08:40 MESZ (Commit `eef6374`,
  `449/449` Tests): SECHS Indikatoren zeigten eine erfundene oder falsch
  beschriftete Zahl und sind behoben. `Lieferanten` hat keine Bewertungsquelle —
  `Performance Score` und `Qualitaet` stehen jetzt auf `-` statt auf einer
  Konstante aus zwoelf Simulationszeilen bzw. dem Literal `"offen"`;
  `Preisindikator` zeigt den mengengewichteten Ø-Stueckpreis des juengsten
  Jahres mit Vorjahresveraenderung statt des Gesamt-Spends. Die Idee
  `Lieferantenrisiko` steht auf `Konzept` statt `berechenbar`. Im Reiter
  `Kontrakte` verwenden Kachel, Diagramm und `Top Verpflichtung` jetzt DIESELBE
  Grundmenge (`EKKO.Konnr`), der Rueckfall auf alle offenen Bestellungen bzw.
  auf Simulationsbalken ist weg, und `Faelligkeit` heisst `Letztes
  Bestelldatum` (der Wert ist `MAX(EKKO.Bedat)`). Auf allen fuenf
  Supply-Chain-Reitern zaehlen die Prioritaetsbalken vor dem Schalter
  `Nur Handlungsbedarf`; vorher stand `Ohne akuten Hinweis` im Standardaufruf
  garantiert auf `0`. `Fehlwert CHF` weist fehlende Stueckkosten aus, statt sie
  als bewertete `0` in die Summe laufen zu lassen. Details:
  `docs/EINKAUF_INDIKATOREN_PRUEFUNG_2026-08-07.md`.
- Produktiv deployed und verifiziert am 2026-08-06 15:11 MESZ (Commit `01af1b8`,
  `446/446` Tests): fuenf additive
  Einkauf-/Logistik-Reiter fuer Materialdisposition, Bestellbedarf/Deckung,
  Materialabhaengigkeit, Dispositionspruefung und Lieferperformance-Datenstatus.
  Bestehende Reiter und Berechnungen bleiben unveraendert. Echte OTIF wird
  mangels Ist-Wareneingangsdatum bewusst nicht gerechnet. Details:
  `docs/EINKAUF_LOGISTIK_SUPPLY_CHAIN_REITER_2026-08-06.md`.
- ZDISPO-Ergaenzung produktiv deployt und verifiziert am 2026-08-06 13:57
  MESZ, Commit `0a8a4c9`, `435/435` Tests. Startseite und direkter
  Spend-Aufriss liefern HTTPS `200`. Produktiv stehen `45` ZDISPO-Zuordnungen
  aus `42` Mustern; die bestehende manuelle ZC23-Tabelle blieb unveraendert bei
  `0` Eintraegen. `105` ZLO03-Zeilen tragen einen Disponenten.
- Seit 2026-08-06 bietet der Spend-Aufriss historisch die Perspektive
  `Produktgruppe -> Lieferant -> Material`. Die Zuordnung folgt
  `EKPO-MATNR -> ZLO03 -> VknrDispo -> Produktname`. Der noch produktive Altstand
  wurde am 2026-08-11 ersetzt; produktiv akzeptiert die Strecke nur noch SAP OData.
  Mehrfach verwendete Komponenten
  werden summenerhaltend gleichmaessig `1/n` auf unterschiedliche
  Produktgruppen verteilt; unzugeordneter Spend bleibt sichtbar.
  Details: `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`.
- ABC und XYZ werden seit 2026-08-06 zusaetzlich als gemeinsame
  Massnahmenmatrix mit Spend, Material-/Lieferantenzahl und konkretem
  Pruefauftrag je Klasse ausgewertet. Es erfolgt keine automatische
  Dispositionsaenderung.
- Seit 2026-08-01 rendern Einkaufsdashboard und Einkaufs-Datenquellen nach
  einem Sprachwechsel sofort neu. Die Sprache ist pro Benutzersitzung getrennt.
  Ein expliziter Katalog deckt 77 dynamische Einkaufstexte in `es`, `it`, `hi`,
  `sq`, `tr` und `tlh` ab. Details und Testgrenzen:
  `docs/EINKAUF_LOKALISIERUNG_PROJEKTSUITE_2026-08-01.md`.
- Delta-Fix `66a34da` ist deployed und nicht mehr an `Sites.IsActive`
  gebunden. Beim Live-Check 2026-07-31 10:21 MESZ lag noch kein produktiver
  Delta-Lauf nach dem Deploy vor; korrekte Aussage bis zum Nachweis:
  **Fix deployed, Live-Wirkung offen.**
- Letzter verifizierter Full Load vom 2026-07-24: `SupplierCountry` 100 %,
  `MaraAbc` 78 %, `MaraXyz` 65 % und `MaraMatkl` 80,7 % gefuellt. Der
  Spend-Aufriss zeigt damit echte Region-/ABC-/XYZ-Daten.
- Die Spend-Matrix bietet Lieferant -> Warengruppe -> Material. Warengruppe
  kommt aus `MARA-MATKL`, mit Fallback auf die Beleg-Warengruppe. T023T-Texte
  werden ueber `PurchasingMaterialGroupTextCatalog` angezeigt; unbekannte
  Codes bleiben sichtbar.
- Finaler Praesentationsstand 2026-07-31: Tabellenkopf, Lieferanten,
  Warengruppen und Materialien sind fett (`700`); Lieferanten/Warengruppen
  `1.05rem`, Materialien `1rem`; dunkler Primaertext und staerkere
  Ebenenhintergruende. Code-Commits `4a3271b`, `f740eb9`, `4498bd4`.
- Nachtlauf Einkauf ist Delta; Full Load bleibt manuell. Die zentrale
  Einkaufsquelle zeigt auf `travp762`. Der Sales-Export bleibt von der
  Einkaufs-Pseudo-Site getrennt.
- Spend-Regeln: nur echte Bestellungen (`Bstyp F`, `Bsart <> UB`), stornierte
  Positionen (`Loekz`) aus historischem Spend ausgeschlossen; offene Werte
  sind Stand-heute, zeitraumunabhaengig und schliessen MSTAE 98/99 sowie
  `Elikz='X'` aus; CHF-Bewertung ueber `Waers`/`Wkurs`.
- Arbeitsweise aus dem Marco-Review: jeweils einen Reiter vollstaendig
  abnehmen, bevor der naechste erweitert wird.

- Lagerwert Einkaufsteile (Disponenten 001-005, `MBEW.SALK3`, Bewertungskreis 1100): Kachel
  produktiv und in der Datenbank gespeichert, Wert fachlich **noch nicht gegen MB5L
  abgeglichen**. Seit 2026-09-28 zusaetzlich ein **Verlauf je Kalenderwoche, produktiv seit
  2026-09-28 09:33**: Tagesstand je Lauf in `PurchasingStockValueHistory`, Anzeige Stand Ende Woche,
  erster Punkt 10.09.2026, kein Rueckblick. Der Einkauf-Lauf fiel vom 10.09. bis 28.09. aus (IIS-Leerlauf); seit 28.09. Nachhol-Delta und Wachhalten, am 28.09. um 13:19 erstmals wieder erfolgreich. Server-Einstellung offen als ISS-017. Details `docs/EINKAUF_LAGERWERT_2026-08-18.md` Abschnitt 13.

- **Ladezeit `/einkauf` gemessen 2026-09-28: 98,6 s bei laufendem Worker**, also kein
  Kaltstart, sondern die Neuberechnung nach Ablauf des 15-Minuten-Snapshots. Die fruehere
  Angabe „9-11 s warm" trifft diesen Fall nicht. Seit `4f2c62f` erscheint die Seite sofort mit
  Ladebalken; seit `d32a6aa` (produktiv 28.09. 11:25) liefert der Cache abgelaufene Staende sofort und die Standardansicht wird vorgewaermt; seit dem SQLite-Cache (28.09. 13:32) rechnet der Server sie in 24,4 s statt rund 100 s. Details in
  `docs/PLATTFORM_TEMPO_2026-09-28.md` Abschnitt 5.

## Offene Punkte

- Marco-Abnahme: Offenwert gegen SAP und WKURS-Richtung an einem echten
  Fremdwaehrungsbeleg pruefen.
- ZLO03-Full-Load und fachliche Summenabnahme an einem echten
  Mehrfachverwendungsfall; fehlende Produktnamen fuer `DISPO D1` und `D5`
  klaeren.
- SAP-SEGW-Key fuer `ZDISPO_GRP` von nur `DISPO` auf den zusammengesetzten Key
  `DISPO_KZ + DISPO` korrigieren.

## Rohquellen Nur Bei Bedarf

- Hauptdoku: `docs/PURCHASING_DASHBOARD_2026-06-05.md`
- Umsetzungsplan: `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`
- Formel-Korrekturen: `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`
- Marco-Review: `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`
- Wuensche Einkaufssitzung: `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`
- Nachfolgesitzung 2026-07-30: `docs/EINKAUF_ANFORDERUNGEN_HISTORIE.md`
- Produktgruppen-/ABC-XYZ-Entscheid 2026-08-06:
  `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`
