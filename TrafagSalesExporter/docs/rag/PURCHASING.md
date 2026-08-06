# RAG Einkauf

Stand: 2026-08-06

Kanonischer Live-Abgleich fuer den Einkauf-Delta-Status:
`docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`. Bei Abweichungen hat dieser
direkt gepruefte Produktivstand Vorrang.

Kurzdatei fuer Spend, offene Bestellungen, Kontrakte und Lieferanten. Historie
und technische Details: `docs/PURCHASING_DASHBOARD_2026-06-05.md`.

## Kurzstand

- Produktiv deployt und verifiziert am 2026-08-06 12:31 MESZ, Commit
  `bb009bf`, `435/435` Tests. Startseite und direkter Spend-Aufriss liefern
  HTTP `200`; `VknrDispo` und `PurchasingProductGroupMap` sind produktiv
  vorhanden. `105` ZLO03-Zeilen tragen einen Disponenten, die echte ZC23-Map
  hat noch `0` Eintraege.
- Seit 2026-08-06 bietet der Spend-Aufriss die Perspektive
  `Produktgruppe -> Lieferant -> Material`. Die Zuordnung folgt
  `EKPO-MATNR -> ZLO03 -> VknrDispo -> ZC23`. Mehrfach verwendete Komponenten
  werden summenerhaltend gleichmaessig `1/n` auf unterschiedliche
  Produktgruppen verteilt; unzugeordneter Spend bleibt sichtbar. Die echten
  ZC23-Texte sind noch einzuspielen, bis dahin wird `Disponent <Code>` gezeigt.
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

- Produktiven Delta-Lauf nach `66a34da` nachweisen (`PurchasingSyncState.Mode
  = Delta`, aktualisiertes Cache-Enddatum und Nachklassifizierungszahl).
- Marco-Abnahme: Offenwert gegen SAP und WKURS-Richtung an einem echten
  Fremdwaehrungsbeleg pruefen.
- Echte ZC23-Referenzliste in `PurchasingProductGroupMap` einspielen, danach
  ZLO03-Full-Load und fachliche Summenabnahme an einem Mehrfachverwendungsfall.

## Rohquellen Nur Bei Bedarf

- Hauptdoku: `docs/PURCHASING_DASHBOARD_2026-06-05.md`
- Umsetzungsplan: `docs/PURCHASING_DASHBOARD_UMSETZUNGSPLAN_MARCO_2026-07-09.md`
- Formel-Korrekturen: `docs/PURCHASING_DASHBOARD_KORREKTUREN_2026-07-06.md`
- Marco-Review: `docs/PURCHASING_DASHBOARD_REVIEW_MARCO_2026-07-10.md`
- Wuensche Einkaufssitzung: `docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-23.md`
- Nachfolgesitzung 2026-07-30: `docs/PURCHASING_DASHBOARD_WUENSCHE_EINKAUF_2026-07-30.md`
- Produktgruppen-/ABC-XYZ-Entscheid 2026-08-06:
  `docs/PURCHASING_PRODUKTGRUPPEN_ABCXYZ_2026-08-06.md`
