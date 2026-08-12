# RAG Einkauf

Stand: 2026-08-12

Kanonischer Live-Abgleich fuer den Einkauf-Delta-Status:
`docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`. Bei Abweichungen hat dieser
direkt gepruefte Produktivstand Vorrang.

Kurzdatei fuer Spend, offene Bestellungen, Kontrakte und Lieferanten. Historie
und technische Details: `docs/PURCHASING_DASHBOARD_2026-06-05.md`.

## Kurzstand

- Direkte Produktgruppenquelle aus SAP am 2026-08-11 produktiv deployed und am
  2026-08-12 nach SAP-Aktivierung live abgeschlossen. Full Load und Delta lesen
  `ZDISPO_GRPSet` + `ZDISPO_SPARTSet`, laden atomar und verwenden keinen
  Excel-/manuellen Fallback. Live: `$metadata` HTTP 200 mit `62` Sets,
  `ZDISPO_GRPSet` `45` Zeilen, `ZDISPO_SPARTSet` `22` Zeilen. Der produktive
  Delta endete um `10:03:42 MESZ` mit `Success`; der Cache enthaelt danach
  `45` SAP-OData-Regeln und `0` Nicht-SAP-/Excel-Regeln. Offen: Texte fuer `D1`
  und `D5` in `ZDISPO_SPART` pflegen und SEGW-Key von `ZDISPO_GRP` auf
  `DISPO_KZ + DISPO` korrigieren. Details:
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
- Umsetzungsplan: `docs/PURCHASING_DASHBOARD_UMSETZUNGSPLAN_MARCO_2026-07-09.md`
- Formel-Korrekturen: `docs/PURCHASING_DASHBOARD_KORREKTUREN_2026-07-06.md`
- Marco-Review: `docs/PURCHASING_DASHBOARD_REVIEW_MARCO_2026-07-10.md`
- Wuensche Einkaufssitzung: `docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-23.md`
- Nachfolgesitzung 2026-07-30: `docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-30.md`
- Produktgruppen-/ABC-XYZ-Entscheid 2026-08-06:
  `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`
