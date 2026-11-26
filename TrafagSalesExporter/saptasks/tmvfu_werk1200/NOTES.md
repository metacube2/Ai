# TMVFU Werk 1200: Vorschlag Verfuegbarkeitspruefung Z2 auf 02 (Anfrage Fabio, 26.11.2026)

Stand: 2026-11-26 (Phase 1, nur gelesen in T76/100, Agent sap-t76, von der Hauptsitzung an den Rohdaten nachgeprueft; **nichts
geaendert, kein Auftrag**). Entscheid Ingo 26.11.: **erst Fabio fragen**, geaendert wird erst nach seiner Antwort. Rueckfrage an Fabio
als Text im Chat (von Ingo zu senden).

## 1. Anfrage Fabio (Mail 26.11.2026 15:28)

1. TMVFU Werk 1200 bei FERT, HALB, HAWA, LEER, VERP von Z2 auf 02 stellen (T76, Transport nach P76); in P76 (Fabio gelesen 26.11.)
   steht 1200 fuenfmal Z2, 1100 „ueberall 02“.
2. Dispositionsgruppe 0002 als Vorschlag je Materialart fuer Werk 1200?
3. Vorschlagswert je Werk fuer Einkaeufergruppe 120 und QM-Steuerschluessel Z003, oder nur Vorlagematerial/Erweiterung?
Die 12 Materialien mit Z2 in Werk 1200 (P76) bereinigt Fabio selbst.

## 2. Befunde T76/100 (belegt)

- **TMVFU**: MANDT, MTART, WERKS (Schluessel), MTVFP; transparent, mandantenabhaengig (CLIDEP X), Customizing (CONTFLAG C), Text
  „Verfuegbarkeitspruefung: Umfang (Vorschlagswerte)“. Pflege View `V_TMVFU`, Transaktion **OVZ3** (auch OVAV), IMG
  `SIMG_CFMENUOLSDOVZ3`. Transport: Customizing-Auftrag, `R3TR VDAT V_TMVFU`, E071K `TABU TMVFU` mit Schluessel Mandant+MTART+WERKS.

  | MTART | 0001 | 1000 | 1100 | 1200 |
  |---|---|---|---|---|
  | FERT | 02 | 02 | **Y2** | Z2 |
  | HALB | – | – | **Y2** | Z2 |
  | HAWA | 02 | 02 | 02 | Z2 |
  | LEER | 02 | 02 | 02 | Z2 |
  | VERP | 02 | 02 | 02 | Z2 |

- **Z2 ist bewusst eingefuehrt:** Auftrag `T76K910618` (Aufgabe `T76K910619`), Customizing, freigegeben, Ziel P76, 21.03.2025,
  Text „Anpassungen Verfuegbarkeitpruefung fuer Lagerartikel - AT“, Mandant 090: legt Pruefgruppe Z2 an (TMVF, TMVFT, T441V) und
  setzt die fuenf TMVFU-Schluessel `090FERT1200` bis `090VERP1200` auf Z2.
- **Werk 1100 ist in T76 nicht ueberall 02:** FERT/HALB = Y2 („Einzelbedarf Dispo“) aus dem **offenen** Auftrag `T76K912186`
  (Aufgabe `T76K912187`, Status D, Ziel P76, 11.06.2026, „PP: ATP-Pruefung - 20.05.2026“), der auch T441V-Regeln der Gruppe 02
  (`09002PP`, `09002ZP`) aendert. Geht er nach P76, gilt dort „02 wie 1100“ vermutlich nicht mehr (090 per RFC nicht lesbar).
- **Unterschied 02/Z2 (T441V):** 02 hat 22 Pruefregeln, Z2 9 (es fehlen u. a. PP, PS, IC, ZP, BB, DE); bei Regel A z. B. FERTP X
  „Alle“ gegen F „Nur freigegebene“, PLAFP A „Nur fixierte“ gegen leer.
- **Mengen:** T76/100 Werk 1200: 2'496 von 2'580 MARC-Zeilen Z2, 73 mit 02, 11 leer. Fabio nennt fuer P76 12 (P76 nicht gelesen).
- **Mandant:** Customizing fuer P76 wird in **T76/090** gepflegt (E070C der TMVFU-Auftraege), 090 = Customizing-Mandant (T000
  CCCATEGORY C), 100 = Test. Unsere Regel „schreiben nur in T76/100“ reicht fuer einen P76-Transport nicht.
- **Sperren:** keine TLOCK; einziger offener Auftrag mit TMVFU ist `T76K912186/187` (nur 1100-Schluessel).

## 3. Antworten zu Punkt 2 und 3

- **Dispositionsgruppe:** Tabelle `TMW00` „Dispo-Vorschlagswerte pro Materialart“ (MANDT+MTART+WERKS, Feld DISGR), View `V_TMW00`,
  Transaktion **OMIG** („Dispositionsgruppe pro Materialart festlegen“). In T76/100 leer. Gruppe 0002 existiert fuer Werk 1200 (T438M).
  Ob OMIG beim Erweitern wirklich vorschlaegt: **nicht belegt**, in T76 zu testen; welche Materialarten, sagt Fabio.
- **Einkaeufergruppe 120 / QM-Steuerschluessel Z003:** kein Standard-Vorschlag je Werk gefunden (DDIC-Suche EKGRP bzw. SSQSS mit WERKS,
  Customizing-Tabellen). Wege: Vorlagematerial, oder Eigenentwicklung ueber BAdI `BADI_MATERIAL_REF` (Methode `CREATE_MATERIAL`,
  `E_MARCU`; in T76 keine Implementierung; ob beim Erweitern aufgerufen: Vermutung). Exit `EXIT_SAPLMGMU_001` (Projekt ZLO00001,
  `ZXMG0U02`) hat nur Importparameter, kann keine Werte setzen.

## 4. Plan Punkt 1 (bereit, nicht freigegeben)

- **Variante A (Bestand):** Pflege in T76/**090** ueber OVZ3 in eigenem Customizing-Auftrag, Ziel P76; braucht ausdrueckliche
  Freigabe Ingo fuer 090 und eine von Ingo angemeldete 090-Sitzung (oder Ingo/Verantwortlicher pflegt selbst, Agent liest nach).
- **Variante B (Test):** dieselben fuenf Werte in T76/100 in lokalem Auftrag „NICHT FREIGEBEN“; Test Fabio: Material nur in 1100,
  MM01 auf 1200 erweitern ohne Vorlage, Vorschlag 02 in Dispo 3 erwartet.
- Werte: FERT/HALB/HAWA/LEER/VERP 1200: Z2 -> 02; uebrige 13 Zeilen unveraendert (auch 1100 = Y2). Auftragstext „TMVFU Werk 1200
  Pruefgruppe Z2 auf 02 (Fabio)“. Ruecklesen: 18 Zeilen, nur diese fuenf anders; E071 `R3TR VDAT V_TMVFU`; E071K genau fuenf
  Schluessel. Rueckbau: Werte in OVZ3 zuruecksetzen.
- **Risiken:** fachliche Umkehr einer bewussten AT-Einstellung; Wechselwirkung mit `T76K912186`; bei B laufen 090/P76 auseinander;
  Wirkung nur auf neu erweiterte Materialien (Vermutung), bestehende MARC-Saetze unveraendert.

## 5. Offen

1. Antwort Fabio: Umkehr mit AT/Auftraggeber abgestimmt? Kennt er `T76K912186`? 12 gegen 2'496? Materialarten fuer 0002?
2. Danach Entscheid Ingo Variante A/B und OMIG-Test.

Rohdaten (nicht im Repo): Scratchpad der Sitzung 26.11. (`tmvfu_all.csv`, `e071k_*.csv`, `t441v_02_z2.csv`, `marc_1200.csv`).
