# Bauplan Phase 2: Materialanteil je Produktgruppe nach Materialgruppe

Stand: 2026-11-02, Bauplan, Entscheide Ingo zu F1/F2 und eigene Tabelle am Ende von Abschnitt 4. **Nachtrag 2026-11-02 11:15: gebaut.**
SAP-Schritte 1 bis 3 in T76 aktiv im Auftrag `T76K912785` (ZLO03 FIX 25, `ZSTR_ZLO_TOPDOWN`, MPC/DPC, Caches geleert), geprueft mit
41019 (`zlo03/FIX25_APP_EXPORT_2026-11-02.md`); Dashboard-Schritte 4 bis 6 in `d6c0287` (Lader im Einkauf-Lauf mit Probe vor dem
Lesen, eigene Tabelle `PurchasingMaterialShare.razor`, roter Knopf, Texte, Tests 1325/1325), deployed 2026-11-02 11:18 (`f36e1e7`). F3 hat sich erledigt: FIX 25 liegt im
selben Auftrag wie FIX 24. Offen: Freigabe und Import von `T76K912785` nach P76 (Ingo), Caches in P76 auf `travp762` leeren,
erster Einkauf-Lauf, Abgleich einer Produktgruppe mit Armin, Laufzeit in P76 messen. Auftrag Ingo „erst Bauplan“. Grundlage: Rechenweg von Armin (Nachtrag 2026-11-02 in
`NOTES.md`), Entscheide Ingo vom 2026-11-02, Quelltexte im Repo (gelesen, nicht in SAP gemessen). Zurueck: `NOTES.md`.

## 1. Was gerechnet wird

Je Produktgruppe, aufklappbar in drei Stufen **Produktgruppe -> Materialgruppe -> Einkaufsteil**:

```
Materialanteil(Produktgruppe, Materialgruppe, Einkaufsteil)
  = Summe ueber die VerkNr der Produktgruppe:
      bestaetigte Menge der VerkNr im Zeitraum            (ZC23: BMENG, „Termin bestaetigt“ im Zeitraum, Absagegrund leer)
    x Stuecklistenmenge des Einkaufsteils je 1 VerkNr     (ZLO03 Top-Down, alle Stufen, Stuecklisten-Einheit, ohne Textpositionen)
    x Stueckkosten des Einkaufsteils                      (Standardpreis / Preiseinheit, in Stuecklisten-Einheit)
  nur Einkaufsteile mit Einkaufsdisponent 001 bis 006
```

Entscheide Ingo 2026-11-02: Disponenten 001 bis 006, Basis „Termin bestaetigt“, Absagegrund leer, neuer Einstieg im Spend-Aufriss mit
rot markiertem Knopf, drei Stufen wie in Armins Text.

## 2. Was es schon gibt (Befund aus den Quelltexten)

| Baustein | Stand | Brauchbar fuer |
| --- | --- | --- |
| ZC23 `ZM_OFFENE_KDAUF`: „Termin bestaetigt“ = `S_MBDAT` auf `VBEP-EDATU`, Absagegrund `P_ABGRU` (leerer Bereich filtert nichts) | P76 | Schritt 1 |
| ZC23 waehlt **ohne Liefer- oder Fakturastatus** (`lfrel = 'X'`, `bmeng <> 0`); ein vergangener Zeitraum enthaelt also auch gelieferte und fakturierte Positionen (aus dem Code gelesen, nicht mit Daten belegt) | P76 | Basis „Termin bestaetigt“ passt |
| `Zc23Set` in `ZPOWERBI_EINKAUF_SRV` (SUBMIT mit `P_APP`, Transport `T76K912738`, P76 seit 16.10.): Pflichtfilter Werk, Datvon/Datbis (hoechstens 366 Tage), nur EQ-Filter; liefert je **Einteilung** u. a. `Matnr`, `Bmeng`, `Netwr1`, `Standardpreis`, `Dispo`, `DispoDes` (Produktgruppe), `Abgru` | P76 | Schritt 1, Absagegrund im Dashboard filtern, je VerkNr summieren |
| ZLO03 `Z_ZLO03_TURBO2` (FIX 24): Top-Down liefert je Komponente `idnrk`, `dispo`, `matkl`, `stueckkosten`, Menge je 1 VerkNr ueber alle Stufen (`COLLECT`) | T76 FIX 24, P76 bis FIX 23 | Schritt 2, aber nur als Excel: Kern `FORM process_main` fuellt globale Tabellen und laedt direkt herunter, kein OData, kein Memory-Export |
| `ZSTR_LZCODE_USAGESet` (alter Webservice `ZM_LZCODE20_OPT`): Top-Down Vknr, Kompnr, Menge, Stueckkosten | Stand P76 ungeprueft | **nicht** geeignet: ohne Disponent und Warengruppe der Komponente, alte Logik ohne FIX 23/24 (Einheit, Rundung) |
| Dashboard: Einstieg „Produktgruppe nach Verwendung : Warengruppe“ (Phase 1) | produktiv | Vorlage fuer Darstellung und Stufen; Basis dort Einkaufsvolumen |
| Dashboard-Caches | — | kein ZC23-Cache; `PurchasingComponentDispoCache` ohne Mengen; `MaterialUsageCache` nur fuer Nummern aus der Stuecklistenanalyse |

**Wichtiger Unterschied zu Armins Handrechnung:** In ZLO03 ist die Gesamt-„Bedarfsmenge“ eine Excel-Formel aus dem **historischen
Verbrauch** der VerkNr (`VTAB-VERBR` aus `ZMD04_CALC`) x Stuecklistenmenge, nicht die ZC23-Menge. Mit dem Entscheid „Basis Termin
bestaetigt“ rechnet die Automatik **ZC23-Menge x Stuecklistenmenge**. Die Zahlen weichen deshalb von Armins Excel ab; das vor dem
Abgleich mit Armin klaeren (Frage F1).

## 3. Bauschritte

### SAP (T76, danach Transport nach P76)

1. **ZLO03 aufrufbar machen**, nach dem Muster von ZC23/`Zc23Set`: Schalter `P_APP` in `Z_ZLO03_TURBO2`; mit `P_APP` exportiert
   `process_main` die Top-Down-Ergebnisse (VerkNr, Komponente, Menge je 1 VerkNr, Disponent, Warengruppe, Stueckkosten, Einheit) ins
   Memory statt herunterzuladen. Kein zweiter Rechenweg, damit Dashboard und ZLO03 dieselben Zahlen zeigen.
2. **Neues Set `ZloTopDownSet`** in `ZPOWERBI_EINKAUF_SRV` (Struktur im Paket wie `ZSTR_ZC23_APP`, MPC `DEFINE`, DPC `GET_ENTITYSET`):
   Pflichtfilter Werk und eine oder mehrere VerkNr (EQ), SUBMIT mit `P_APP`, Import aus dem Memory. Filter auf die Komponenten-Disponenten
   001 bis 006 schon in SAP, damit die Antwort klein bleibt.
3. **Reihenfolge der Transporte:** ZLO03 steckt mit FIX 24 im offenen Auftrag `T76K912785`; die Aenderung entweder dort mitnehmen oder
   nach dessen Freigabe in einen neuen Auftrag. MPC/DPC vorher auf Sperren in offenen Auftraegen pruefen (LEARNINGS 2026-10-08, 2026-10-16).
   Nach dem Import in P76 Gateway-Caches auf `travp762` leeren.

### Dashboard

4. **Laden im Einkauf-Lauf (taeglich):** `Zc23Set` fuer das laufende Jahr (366-Tage-Grenze beachten), Zeilen mit leerem `Abgru` behalten,
   je VerkNr und Monat `Bmeng` summieren -> neue Tabelle `PurchasingZc23Cache`. Danach fuer die VerkNr mit Menge `ZloTopDownSet` in
   Bloecken -> `PurchasingBomDemandCache` (VerkNr, Komponente, Menge je 1 VerkNr, Disponent, Warengruppe, Stueckkosten).
   Schreibt nur bei vollstaendigem Lauf (wie die bestehenden Caches).
5. **Neuer, eigener Aufriss „Materialanteil je Produktgruppe“** als aufklappbare Tabelle in einer **eigenen Komponente** (Entscheid
   Ingo 2026-11-02: der bestehende Spend-Aufriss bleibt unveraendert, falls etwas nicht passt). Ein rot markierter Knopf im Kopf des
   Spend-Aufrisses oeffnet ihn; `PurchasingSpendExplorer.razor` bekommt nur diesen Knopf, keine neue Perspektive in seiner Logik. Stufen Produktgruppe -> Materialgruppe -> Einkaufsteil, Zeitraum des Aufrisses auf „Termin
   bestaetigt“, Schalter „Nur Einkaufsteile 001 bis 006“ wirkt wie bei allen Einstiegen. Ueber der Tabelle steht deutlich die Basis
   („Materialanteil aus bestaetigten Kundenauftraegen x Stueckliste, nicht Einkaufsvolumen“).
6. Texte in allen Sprachen inkl. Berndeutsch, Tests (Rechnung, Absagegrund, 006, Stufen), Handbuch Kapitel Einkauf.

### Pruefung vor dem Freigeben

7. Eine Produktgruppe mit Armin gegen seine Handrechnung abgleichen, Unterschied Verbrauch vs. ZC23-Menge ausweisen.
8. Laufzeit messen: `Zc23Set` ohne Material fuer zehn Monate (ZC23 Q2 dauerte 13,5 s fuer einen kleineren Ausschnitt) und ZLO03 fuer
   alle VerkNr eines Jahres; bei Bedarf als Hintergrundlauf in SAP mit Ergebnistabelle statt SUBMIT im Dialog (600-s-Grenze, LEARNINGS
   2026-10-12).

## 4. Offene Fragen

| Nr | Frage | An |
| --- | --- | --- |
| F1 | Menge: bestaetigte ZC23-Menge x Stueckliste (Entscheid „Termin bestaetigt“) statt ZLO03-Verbrauch, wie in Armins Excel? Abweichung akzeptiert? | Ingo / Armin |
| F2 | Mehrstufigkeit: zaehlen Einkaufsteile in Unterbaugruppen voll mit (ZLO03 summiert alle Stufen; Befund G4 „Mehrstufigkeit teilweise“ in `zlo03/CLAUDE.md` noch offen gegen CS12)? | Ingo |
| F3 | Erst nach dem P76-Import von FIX 24 bauen (ungerundete Stueckkosten, Textpositionen)? Empfehlung: ja | Ingo |

**Entscheide Ingo 2026-11-02:** F1 ja, Menge = bestaetigte ZC23-Menge x Stueckliste, die Abweichung zu Armins Excel (Verbrauch) ist
gewollt. F2 ja, die Summe ueber alle Stuecklistenstufen reicht. Darstellung als eigene, aufklappbare Tabelle (Schritt 5). F3 offen.

## 5. Aufwand (Schaetzung)

SAP: ein bis zwei Tage (Schalter in ZLO03, Struktur, Set, Tests in T76, Transport). Dashboard: ein bis zwei Tage (zwei Caches, Einstieg,
Texte, Tests). Dazu Abgleich mit Armin und Laufzeitmessung in P76.
