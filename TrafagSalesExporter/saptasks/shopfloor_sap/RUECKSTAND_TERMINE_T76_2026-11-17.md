# Rückstandslisten: VF-Termin, WE-Termin, Verknüpfung und VkNr (T76, nur gelesen)

Stand: 2026-11-17. Auftrag Ingo („mach mal alles“, Punkt 3 des Ergänzungspakets Fabio 17.11.): nur lesend in T76 klären, aus
welchen SAP-Feldern „Wunschtermin“ (laut Anh Nhi der VF-Termin, „Eckentermin Verkauf“) und „Datum WE geplant“ (laut Anh Nhi der
WE-Termin als Eckendtermin des Produktionsauftrags) kommen, wie die Rückstandszeile mit dem Auftrag verknüpft wird, was „VkNr“ ist
und ob ein OData-Set die Felder schon liefert. Fachliche Vorgabe: `Ergaenzung_und_Pruefpunkte.md` (Projektordner, unversioniert)
Abschnitt 1, Zeile 15.

Zugang: nur RFC gegen T76/100 über `.tmp_sap_probe/RunSapProbe.ps1` mit der DPAPI-Ablage (`Test-SapPassword`: vorhanden seit
10.09.2026). Kein GUI-Scripting, keine Sitzung geöffnet, kein Passwort eingegeben. P76 nicht berührt. In SAP nichts geändert:
gelesen wurden `abap-read` (Quelltexte), `table-read` (DDIC- und Belegtabellen) und der lesende Baustein
`BAPI_MATERIAL_STOCK_REQ_LIST` (Bedarfs-/Bestandsliste wie MD04, ohne Planungslauf). ZC22 wurde bewusst **nicht** ausgeführt, weil
die Form `KAPA_BEDARFE` (Zeilen 774 bis 794) bei fehlendem Satz `MODIFY zckapa` schreibt. Die ZC22-Werte unten sind deshalb aus dem
Quelltext **nachgerechnet** (VBBE/VBEP gelesen, Regel aus Zeilen 740 bis 769 angewandt), nicht von der Liste abgelesen.

T76 ist eine Datenkopie (Stand etwa Frühjahr 2026). Die Beispielbelege sind T76-Belege; für P76 sind sie nur Hinweise.

## 0. Kurzfassung

1. **ZC22 hat keine Spalte „VF-Termin“ oder „WE-Termin“; der einzige Fundort dieser Kurztexte in T76 ist das Zusatzpaket GIB.**
   Geprüft sind die Texte aller 59 Datenelemente von `ZMM_UEB_FAUF` (DD04T, Abschnitt 2). Im Paket `/GIB/DC` („GIBSCM
   Basiskomponente DDIC“; Transaktion `/GIB/DCO` = Programm `/GIB/DCO_DISPOLISTE`) gibt es die Datenelemente
   `/GIB/DC_DATE_AV` „Verfügbarkeits-Termin laut MD04/MD05“, Kurztext **„VF-Termin“**, und `/GIB/DC_DATE_GR`
   „Wareneingangs-Termin laut MD04/MD05“, Kurztext **„WE-Termin“** (DD04T, Sprache D). Sie stecken in keiner DDIC-Struktur, und
   für die GIB-Reports gibt es in T76 weder Varianten (`VARID`) noch ALV-Layouts (`LTDX`). Ob Anh Nhi GIB benutzt oder die Begriffe
   nur so verwendet, ist offen; laut Auftrag arbeitet die Dispo mit ZC22.
2. **a) ZC22 „Wunschtermin“ und „Termin bestätigt“** kommen in 739 von 741 Fällen aus `VBBE-MBDAT` (Materialbereitstellungsdatum
   der offenen Verkaufsbedarfe), nicht aus `VBEP-EDATU` (Lieferdatum). Nur wenn es zur Position keinen VBBE-Satz gibt, nimmt ZC22
   `VBEP-EDATU` der lieferrelevanten Einteilungen. Die Regel ist „letzter Satz in Datenbankreihenfolge“, nicht „spätestes Datum“ und
   nicht „mit BMENG > 0“; bei mehreren voll bestätigten Einteilungen zeigt „Termin bestätigt“ die **erste**, „Wunschtermin“ die
   **letzte** (Abschnitt 3).
3. **b) Eckendtermin des FA** ist `AFKO-GLTRP`; `AFPO-DGLTP` ist bei allen 2'079 offenen FA des Werks 1100 im Fenster gleich
   `AFKO-GLTRP`. Bei Planaufträgen setzt ZC22 `PLAF-PEDTR` in dieselbe Spalte. Die Disposition (MD04) führt den Zugang des FA aber
   mit **Eckendtermin + WE-Bearbeitungszeit** (`AFPO-WEBAZ` bzw. `PLAF-WEBAZ`, Arbeitstage): FA 1241683 Eckende 24.04.2026,
   WEBAZ 2, MD04-Verfügbarkeit 28.04.2026. In MD04-Feldern ist das `DAT01` „Liefer-/Eckendtermin“ gegen `DAT00`
   „Zugangs-/Bedarfstermin“. Die Zuordnung „WE-Termin = DAT01, VF-Termin = DAT00“ ist eine Vermutung aus den GIB-Texten.
4. **c) Verknüpfung:** ZC22 verbindet nur über `AFPO-KDAUF/KDPOS` bzw. `PLAF-KDAUF/KDPOS`, nie über die Einteilung
   (`AFPO-KDEIN` ist bei allen 741 verknüpften FA `0000`). Alle FA und Planaufträge einer Position bekommen dieselben Termine. 1'338
   der 2'079 offenen FA haben gar keinen Kundenauftrag; für sie sind die Terminspalten leer, und der Weg zum Kunden geht nur über den
   Bedarfsverursacher (`ZZPEG_*`, MD-Pegging).
5. **d) Materialspalte in ZC22** ist `AFPO-MATNR` bzw. `PLAF-MATNR` (über `IAFPO`, Zeilen 434, 485, 515), nicht `VBAP-MATNR`. Bei
   allen 741 verknüpften FA ist `AFPO-MATNR = VBAP-MATNR`. „VkNr“ = Materialnummer stützen zwei Belege: das Feld `VKNR` der Struktur
   `ZSTR_ZLO_TOPDOWN` (`ZloTopDownSet`) hat das Datenelement `MATNR` (neben `IDNRK` für die Komponente), und der P76-Variantentext
   `/PPA EKX ALLE` „Aufträge EKx alle VkNr“ selektiert Material 1 bis 99999 (`P76_BEFUND_2026-11-13.md` 1.3).
6. **e) OData:** `ShopAufSet` liefert nur `Datum` = `AFKO-GLTRP` bzw. `PLAF-PEDTR`, keinen Kundenauftrag und keinen Kundentermin.
   `Zc23Set` und `ZPOWERBI_KDAUFSet` liefern je bestätigter Einteilung `EDATU`, `KETDAT` und (Zc23Set) einen zugeordneten FA;
   `VBEPSet` hat weder `EDATU` noch `MBDAT`; `AFPOSet` hat `Kdauf`, `Kdpos`, `Dgltp`, `Webaz`. Keines der geprüften Sets liefert
   `VBBE-MBDAT`, Planaufträge mit Kundenauftrag oder `WEBAZ`-verschobene Zugangstermine, und die Basis-MPC hat keine Entität für
   VBBE, PLAF oder AFKO (Abschnitt 7).

## 1. Einziger Fundort von „VF-Termin“ und „WE-Termin“ in T76

ZC22: Die Texte aller 59 Datenelemente von `ZMM_UEB_FAUF` (DD04T, Sprache D) enthalten weder „VF“ noch „WE-“ (Abschnitt 2).

Suche in DD04T (Sprache D, aktive Fassung), zuerst nur Z/Y-Namensraum: kein Treffer. Danach ohne Namensraumfilter mit
`SCRTEXT_S/SCRTEXT_M/REPTEXT LIKE '%VF%'` und `DDTEXT` mit „ermin“ oder „atum“: drei Treffer, davon passt nur `/GIB/DC_DATE_AV`
(die beiden anderen, `ARUN_OTDBD` und `FSH_OTDBD`, „Versandfälligkeitsdatum“, Kurztext VSF, nicht). Eine zweite Abfrage über die
Datumselemente `/GIB/%` lieferte die Geschwister dazu:

| Datenelement | Langtext | Kurz / mittel | Paket (TADIR) | aus Abfrage |
| --- | --- | --- | --- | --- |
| `/GIB/DC_DATE_AV` | Verfügbarkeits-Termin laut MD04/MD05 | VF-Termin | `/GIB/DC` | „VF“ über alle Namensräume |
| `/GIB/DC_DATE_GR` | Wareneingangs-Termin laut MD04/MD05 | WE-Termin | `/GIB/DC` | `/GIB/%` mit Termin/Datum |
| `/GIB/DC_DATE_REQ` | Bedarfs-Termin laut MD04/MD05 | Bedarfs-Termin | `/GIB/DC` | `/GIB/%` mit Termin/Datum |

GIB ist in T76 installiert: 364 Transaktionen `/GIB/%` in `TSTC`, darunter `/GIB/DCO` und `/GIB/DCC_SIM` (Programm
`/GIB/DCO_DISPOLISTE`), `/GIB/DCO_DATA`, `/GIB/DCO_PEG_BUILD`. Paket `/GIB/DCO` heisst „GIBSCX Operations“ (TDEVCT).

Wie GIB die beiden Termine füllt, ist **nicht gelesen**: `/GIB/DC_DATE_AV` und `/GIB/DC_DATE_GR` kommen in keiner DDIC-Struktur vor
(DD03L über alle Tabellen leer), und der Verwendungsnachweis `WBCROSSGT` hat für sie keine Einträge. Die GIB-Struktur für
Kundenaufträge `/GIB/DCO_KAUF_FK` („Datenbeschaffung“) enthält `EDATU`, `VDATU`, `MBDAT`, `DATE_REQ` (`/GIB/DC_DATE_REQ`),
`DATE_REQ_ST`, `BMENG`, `FAUFN` (Fertigungsauftrag) und den MD04-Satz (`DAT00`, `DAT01`, `DAT02`, `WEBAZ`, `DELKZ`, `KDAUF`,
`KDPOS`), aber kein Feld vom Typ `DC_DATE_AV`. Für Reports `/GIB/DCO%` gibt es in T76 keine Varianten (`VARID`) und für `/GIB/%`
keine ALV-Layouts (`LTDX`); dieselben Abfragen liefern für `ZM_OFFENE_FAUF` Treffer, sind also richtig gestellt. Ob GIB in P76
benutzt wird, ist offen. **Vermutung:** Wenn Anh Nhi die Begriffe aus GIB hat, sind „VF-Termin“ und „WE-Termin“ dort Spalten für
Zugangselemente (FA, Planauftrag, Bestellung), also die Daten, die MD04 für diese Elemente zeigt.

Zur Einordnung, gelesen in DD04T: `DAT00` „Zugangs-/Bedarfstermin“, `DAT01` „Liefer-/Eckendtermin“, `DAT02`
„Start-/Freigabetermin“; im BAPI heissen sie `AVAIL_DATE` (`DAT00`), `FINISH_DATE` (`DAT01`), `START_DATE` (`DAT02`) (DD03L
`BAPI_MRP_IND_LINES`).

Nicht zu verwechseln: `Typ = 'VF'` in `ShopAufSet` heisst „Fertigungsauftrag“ (gegen `VP` Planauftrag), DPC `CM01W` Zeilen 774 und 786,
und hat mit dem VF-Termin nichts zu tun.

## 2. ZC22: welche Spalte aus welchem Feld

ZC22 startet `ZM_OFFENE_FAUF` (T76 `TSTC`). Der T76-Quelltext (`abap-read`, 1'142 Zeilen) ist nach Entfernen von Leerzeichen am
Zeilenende zeilengleich mit `docs/abap/ZM_OFFENE_FAUF.abap` (`diff` ohne Ausgabe). Es gibt keine Includes ausser der Struktur
`ZZBPTAB`. Die Liste kommt aus `REUSE_ALV_LIST_DISPLAY` mit `i_structure_name = 'ZMM_UEB_FAUF'` (Zeilen 115 bis 124); die
Spaltenüberschriften sind deshalb die Texte der Datenelemente (Feldkatalog ändert nur `STTXT` und Nullanzeige, Zeilen 895 bis 939).

| Spalte (DD04T, D) | Feld `ZMM_UEB_FAUF` (Datenelement) | Gefüllt aus | Quelltext |
| --- | --- | --- | --- |
| Eckendtermin | `GLTRP` (`CO_GLTRP`) | `AFKO-GLTRP`; bei Planauftrag `PLAF-PEDTR` | 465, 470, 517 |
| Eckend Eröffnung („Eckendtermin bei Auftragseröffnung“) | `GLTRP1` (`ZGLTRP`) | Z-Tabelle `ZCFAUF_GLTRP-GLTRP` („Fertigungsaufträge – Eckende bei Eröffnung“), nur FA | 602 bis 603 |
| Terminiertes Ende | `GLTRS` (`CO_GLTRS`) | `AFKO-GLTRS` (MOVE-CORRESPONDING) | 517 |
| Istendtermin | `GLTRI` (`CO_GLTRI`) | `AFPO-LTRMI` | 520 |
| Wunschtermin („W-Termin“) | `DATUM_W` (`ZZ_DATUM_W`) | `VBBE-MBDAT`, ersatzweise `VBEP-EDATU` | 740 bis 769 |
| Termin bestätigt („B-Termin“) | `DATUM_B` (`ZZ_DATUM_B`) | `VBBE-MBDAT`, ersatzweise `VBEP-EDATU` | 740 bis 769 |
| Kalenderwoche | `KWOCHE` | aus `AFKO-GLTRP` | 579 bis 587 |
| Kundenauftrag / Position | `KDAUF`, `KDPOS` | angezeigt aus `AUFK-KDAUF/KDPOS` (letzte Zuweisung), bei Planauftrag `AUFK` = `PLAF-KDAUF/KDPOS`; Termine und VBAP lesen mit `IAFPO-KDAUF/KDPOS` (`AFPO` bzw. `PLAF`) | 436 bis 437, 497 bis 498, 515, 518, 542 bis 543, 743 bis 744 |
| Material | `MATNR` | `AFPO-MATNR` bzw. `PLAF-MATNR` | 434, 485, 509 bis 515 |
| Erste Entnahme | `ZZMBDAT` (`ZZMBDAT`) | erstes `AUFM-BLDAT` mit `SHKZG = 'H'` | 799 bis 809 |
| Bedarfsverursacher | `ZZPEG_*` | `MD_PEGGING_NODIALOG`, erster Verursacher | 948 bis 1003 |

Z-Felder: VBEP, VBKD, AFPO, AFKO und PLAF haben keine Felder `ZZ%` (DD03L); VBAP hat `ZZLAENGE`, `ZZSP001…` usw., davon nimmt ZC22
nur Länge und Sonderwerte (Zeilen 546 bis 553), keinen Termin. Die einzige eigene Terminlogik ist `ZCFAUF_GLTRP` (Eckende bei
Eröffnung) und die Form `KDAUF_TERMINE`.

Für die `MOVE-CORRESPONDING`-Kette (Zeilen 509 bis 518, danach `ZCKAPA` in 789/792) wurde in DD03L geprüft, welche Quellen
gleichnamige Felder haben: `MATNR` in MARA, MARC, AFPO, ZCKAPA (alle mit demselben Wert, MARA und ZCKAPA werden mit
`IAFPO-MATNR` gelesen); `GLTRP` nur in AFKO; `DATUM_W`/`DATUM_B` in keiner Quelle; `KDAUF`/`KDPOS` in AFPO **und** AUFK, die
angezeigte Spalte kommt deshalb aus AUFK (Zeile 518 nach 515). Gegenprobe T76: Bei allen 2'079 offenen FA aus 3.2 ist
`AUFK-KDAUF/KDPOS` gleich `AFPO-KDAUF/KDPOS` (AUFK `AUTYP 10` im AUFNR-Bereich gelesen, 0 Abweichungen); die Spalte zeigt also
denselben Kundenauftrag, mit dem die Termine gelesen werden.

## 3. a) Kundenauftragstermine in ZC22 (Form `KDAUF_TERMINE`, Zeilen 740 bis 769)

### 3.1 Regel aus dem Quelltext

```
check not iafpo-kdauf is initial.                      " ohne Kundenauftrag: beide Spalten leer
select * from vbbe where vbeln = kdauf and posnr = kdpos.     " kein ORDER BY
  if vbbe-omeng gt 0.  datum_w = vbbe-mbdat.  omeng += vbbe-omeng.
  else.                datum_b = vbbe-mbdat.
  if datum_b is initial. datum_b = datum_w.
endselect.
if sy-subrc ne 0.                                     " nur wenn es keinen VBBE-Satz gibt
  select * from vbep where vbeln = kdauf and posnr = kdpos and lfrel = 'X'.   " kein ORDER BY
    if vbep-wmeng gt 0. datum_w = vbep-edatu. else. datum_b = vbep-edatu.
    if datum_b is initial. datum_b = datum_w.
  endselect.
```

(gekürzt aus Zeilen 741 bis 768). Danach filtert `S_MBDAT` auf `DATUM_B` (Zeile 590).

Was daraus folgt:

- **Hauptweg ist `VBBE`** („Vertriebsbedarfseinzelsätze“, DD02T). `VBBE-MBDAT` ist das Materialbereitstellungsdatum
  (DD04T `MBDAT`), nicht das Lieferdatum. `OMENG` ist laut DD04T die „offene Menge zur Bedarfsübergabe an die Disposition“, `VMENG`
  die „bestätigte Menge für Verfügbarkeitsprüfung“. VBBE enthält nur offene Bedarfe; gelieferte Einteilungen fehlen dort (Beispiel
  407598/10 unten: VBEP sechs Einteilungen, VBBE nur Einteilung 6; dass 2 bis 5 geliefert sind, ist eine Vermutung).
- **„Wunschtermin“** = `MBDAT` des letzten VBBE-Satzes mit `OMENG > 0`. **„Termin bestätigt“** = `MBDAT` des letzten Satzes mit
  `OMENG = 0`; gibt es keinen solchen, bleibt der Wert, den die erste Runde gesetzt hat, also `MBDAT` des **ersten** Satzes.
- „Letzter“ heisst Datenbankreihenfolge. Beide SELECTs haben kein `ORDER BY`; die Nachrechnung unten sortiert nach `ETENR` (Schlüssel
  `MANDT VBELN POSNR ETENR` in DD03L für VBBE und VBEP). Dass die Datenbank in dieser Reihenfolge liefert, ist eine Vermutung.
- **Ersatzweg `VBEP`** nur ohne VBBE-Satz: dann `EDATU` (Lieferdatum), nicht `MBDAT`, und „Termin bestätigt“ ist die Einteilung mit
  `WMENG = 0` und höchster `ETENR`, nicht das späteste Datum. `BMENG` prüft ZC22 nirgends.
- Die Einteilung des FA (`AFPO-KDEIN`) wird nicht verwendet. Alle FA und Planaufträge derselben Position bekommen dasselbe Paar.

### 3.2 Menge und Fenster der Nachrechnung

`AFPO` T76: `DWERK = '1100'`, `ELIKZ` und `DNREL` leer, `DGLTP` 01.03. bis 30.06.2026: **2'079** offene FA, davon **741** mit
`KDAUF` (448 Kundenaufträge). Dazu VBEP (1'742 Sätze) und VBBE (1'435 Sätze) dieser Aufträge gelesen.

| Befund | Anzahl FA |
| --- | ---: |
| Termine aus VBBE (Hauptweg) | 739 |
| Termine aus VBEP (kein VBBE-Satz) | 2 |
| Position hat mehr als eine Einteilung | 407 |
| Position hat mehr als eine Einteilung mit `BMENG > 0` | 11 |
| „Termin bestätigt“ = `MBDAT` der letzten Einteilung mit `BMENG > 0` (nach ETENR) | 715 |
| „Termin bestätigt“ = `EDATU` der letzten Einteilung mit `BMENG > 0` | 2 |
| VBEP-Sätze mit `EDATU` ≠ `MBDAT` | 1'729 von 1'742 |
| VBBE-Sätze mit `OMENG = 0` | 478 von 1'435 |

### 3.3 Beispiele (T76, nachgerechnet aus VBBE/VBEP, nicht von der ZC22-Liste)

Einteilungen als `ETENR | EDATU | MBDAT | WMENG | BMENG`, VBBE als `ETENR | MBDAT | OMENG | VMENG`, Mengen in ST.

**409313/10, zwei voll bestätigte Einteilungen** (FA 1241241, 1241680, 1241683; Planaufträge 2344122, 2372146, 2375521; Material
55441):

| Quelle | Inhalt |
| --- | --- |
| VBEP | 1 \| 24.04.2026 \| 22.04.2026 \| 60 \| 60; 2 \| 08.05.2026 \| 06.05.2026 \| 240 \| 240 |
| VBBE | 1 \| 22.04.2026 \| 60 \| 60; 2 \| 06.05.2026 \| 240 \| 240 |
| ZC22 Wunschtermin | **06.05.2026** (letzter Satz mit OMENG > 0) |
| ZC22 Termin bestätigt | **22.04.2026** (kein Satz mit OMENG = 0, also der erste) |
| MD04 (BAPI) | VC `0000409313/000010/0001` 22.04.2026, −60; VC `…/0002` 06.05.2026, −240 |

Alle drei FA und alle drei Planaufträge tragen in ZC22 dieses Paar, egal welche Einteilung sie decken. Der „letzte bestätigte
Liefertermin“ im Sinne von Anh Nhi wäre 08.05.2026 (EDATU) bzw. 06.05.2026 (MBDAT), ZC22 zeigt als „Termin bestätigt“ den ersten.

**410019/10, Teilbestätigung** (FA 1240731, 1241498):

| Quelle | Inhalt |
| --- | --- |
| VBEP | 1 \| 14.04. \| 17.04. \| 150 \| 0; 2 \| 28.04. \| 24.04. \| 0 \| 149; 3 \| 21.04. \| 17.04. \| 0 \| 1 |
| VBBE | 1 \| 17.04. \| 76 \| 0; 2 \| 24.04. \| 0 \| 76 |
| ZC22 | Wunschtermin **17.04.2026**, Termin bestätigt **24.04.2026** |

Hier ist die Einteilung mit der höchsten `ETENR` und `BMENG > 0` (3, 21.04.) nicht die mit dem spätesten Datum (2, 28.04.). ZC22
nimmt den offenen Bedarf (VBBE 2), also die richtige; Lieferdatum dazu 28.04.2026, MBDAT 24.04.2026.

**408683/10, Rest unbestätigt** (FA 1241588):

| Quelle | Inhalt |
| --- | --- |
| VBEP | 1 \| 30.03. \| 17.04. \| 1'000 \| 0; 2 \| 04.05. \| 22.04. \| 0 \| 600 |
| VBBE | 1 \| 17.04. \| 400 \| 0 |
| ZC22 | Wunschtermin **17.04.2026**, Termin bestätigt **17.04.2026** |

„Termin bestätigt“ zeigt hier ein Datum, obwohl die offenen 400 ST nicht bestätigt sind (`VMENG 0`); es ist der Wunschtermin aus
der Ersatzregel. Zudem ist der „Wunschtermin“ 17.04. nicht das Wunschlieferdatum des Kunden (`EDATU` der ersten Einteilung 30.03.).

**405474/10, Ersatzweg VBEP** (FA 1240082, kein VBBE-Satz):

| Quelle | Inhalt |
| --- | --- |
| VBEP | 1 \| 20.02. \| 25.03. \| 12 \| 0; 2 \| 27.03. \| 25.03. \| 0 \| 2; 3 \| 06.03. \| 04.03. \| 0 \| 10 |
| ZC22 | Wunschtermin **20.02.2026**, Termin bestätigt **06.03.2026** |

Hier zählt `EDATU`, und „Termin bestätigt“ ist Einteilung 3 (höchste ETENR), nicht das späteste bestätigte Datum 27.03.

**407598/10** (FA 1238833): VBEP Einteilungen 1 bis 6, 2 bis 6 je 60 ST bestätigt (20.03. bzw. 15.05.), VBBE nur Einteilung 6
(12.05., 60/60). ZC22: Wunschtermin und Termin bestätigt **12.05.2026** (MBDAT zu EDATU 15.05.2026).

### 3.4 Vergleich mit den bisherigen Vorschlägen in der Config

`Services/Shopfloor/shopfloor_config.json` schlägt für „Wunschtermin“ `VBEP-EDATU (1. Einteilung) bzw. VBAK-VDATU` vor. Die erste
Einteilung ist bei 410019, 408683, 405474 und 407598 die **unbestätigte Wunscheinteilung** (`WMENG > 0`, `BMENG = 0`), also das
Gegenteil eines „letzten bestätigten Liefertermins“; bei 409313 ist sie zwar bestätigt, aber nur die erste von zwei Lieferungen. `VBAK-VDATU` ist ein Kopfdatum und kennt keine Einteilung.
Beide Vorschläge passen nicht zu Anh Nhis Definition.

## 4. b) Eckendtermin, WE-Termin und Verfügbarkeit von FA und Planauftrag

| Was | Feld | Beleg |
| --- | --- | --- |
| ZC22 „Eckendtermin“, FA | `AFKO-GLTRP` | Zeilen 465, 517 |
| ZC22 „Eckendtermin“, Planauftrag | `PLAF-PEDTR` („Endtermin des Planauftrags“) | Zeile 470 |
| `AFPO-DGLTP` | Datenelement `CO_GLTRP`; bei **2'079 von 2'079** offenen FA (Fenster wie 3.2) gleich `AFKO-GLTRP` | AFKO `GLTRP` 01.02. bis 30.09.2026 gelesen und verglichen |
| `AFPO-LTRMP` („Liefertermin aus Planauftrag“) | weicht ab und ist teils leer (940 von 2'079 ungleich `DGLTP`), z. B. FA 1239110 `LTRMP` 04.06. gegen Eckende 17.04. | nicht verwenden |
| Eckende bei Eröffnung | `ZCFAUF_GLTRP-GLTRP`, z. B. FA 1223197 19.02.2026 gegen heute 28.04.2026 | ZC22-Spalte „Eckend Eröffnung“ |
| WE-Bearbeitungszeit | `AFPO-WEBAZ`, `PLAF-WEBAZ` (Arbeitstage) | Verteilung der 2'079 FA: 0 → 1'120, 1 → 129, 2 → 706, 3 → 107, 5 → 6, 17 → 11 |

**MD04-Nachweis** (`BAPI_MATERIAL_STOCK_REQ_LIST`, Material 55441, Werk 1100, T76):

| Element | `DAT01` Liefer-/Eckendtermin | `DAT00` Zugangs-/Bedarfstermin | Beleg Eckende | WEBAZ | Rechnung |
| --- | --- | --- | --- | ---: | --- |
| FA 1241241 | 22.04.2026 | 24.04.2026 | `AFKO-GLTRP` 22.04. | 2 | Mi + 2 AT = Fr |
| FA 1241680 | 23.04.2026 | 27.04.2026 | `AFKO-GLTRP` 23.04. | 2 | Do + 2 AT = Mo |
| FA 1241683 | 24.04.2026 | 28.04.2026 | `AFKO-GLTRP` 24.04. | 2 | Fr + 2 AT = Di |
| PA 2344122 | 24.04.2026 | 28.04.2026 | `PLAF-PEDTR` 24.04. | 2 | Fr + 2 AT = Di |
| PA 2372146 | 04.05.2026 | 06.05.2026 | `PLAF-PEDTR` 04.05. | 2 | Mo + 2 AT = Mi |

`DAT01` ist also das Eckende, `DAT00` das Eckende plus WE-Bearbeitungszeit. Sind „WE-Termin“ und „VF-Termin“ in GIB diese beiden
MD04-Daten (Vermutung, Abschnitt 1), dann ist für den FA **WE-Termin = `AFKO-GLTRP`** (Anh Nhis „Eckendtermin des
Produktionsauftrags“ stimmt damit überein) und **VF-Termin = `AFKO-GLTRP` + `AFPO-WEBAZ`**, beim Planauftrag entsprechend mit
`PLAF-PEDTR` und `PLAF-WEBAZ`. Bei rund der Hälfte der offenen FA (WEBAZ 0) sind beide gleich.

ZC22 nimmt nur Planaufträge mit `PLAF-UMSKZ = 'X'` und `BESKZ <> 'F'` (Zeilen 430 bis 431). Planaufträge haben in ZC22 keinen
Systemstatus und keinen `ZCFAUF_GLTRP`-Wert (Zeilen 597 bis 604).

## 5. c) Verknüpfung Rückstandszeile → Auftrag

- ZC22 geht vom Auftrag zum Kundenauftrag, nicht umgekehrt: `AFPO-KDAUF/KDPOS` bzw. `PLAF-KDAUF/KDPOS` (Zeilen 436 bis 437, 497 bis
  498), dann `VBAK`, `VBAP` (542 bis 543) und `KDAUF_TERMINE`. `AFPO-KDEIN` und `PLAF-KDEIN` (Einteilung) gibt es, sie sind aber bei
  allen 741 verknüpften FA `0000` und werden von ZC22 nicht gelesen.
- Die Beziehung Position → Aufträge ist 1:n. 409313/10 hat drei FA und drei Planaufträge; welcher Auftrag welche Einteilung deckt,
  steht in keinem Beleg, nur im MD04-Pegging.
- 1'338 von 2'079 offenen FA (64 %) haben keinen Kundenauftrag (Vermutung: Lagerfertigung oder Vorstufen; nicht gelesen). Für sie
  bleiben in ZC22 Wunschtermin und Termin bestätigt leer (Zeile 742). Ihr Kundenbezug kommt nur über den Bedarfsverursacher:
  `ZZPEG_DELNR/DELPS/DELET` (Auftrag, Position, **Einteilung**) und `ZZPEG_DAT00` (`MDRQ-DAT00`, Bedarfstermin), Zeilen 986 bis 995;
  bei Bedarf in einem anderen Werk ersetzt durch Umlagerungsbestellung oder BANF (Zeilen 1000 bis 1001). Vermutung: Für einen
  VC-Bedarf ist `ZZPEG_DAT00` das MD04-Datum der Kundenauftragszeile, also `MBDAT`; belegt ist nur, dass MD04 die VC-Zeile auf
  `MBDAT` führt (409313/10: MD04 22.04. = VBBE-MBDAT 22.04.), nicht die Ausgabe des Pegging-Bausteins.
- ZC23 (`ZM_OFFENE_KDAUF`) arbeitet umgekehrt je bestätigter Einteilung (`LFREL = 'X'`, `BMENG NE 0`, Zeilen 486 bis 490, sortiert
  `VBELN POSNR EDATU ETENR`, Zeile 644) und ordnet jeder Einteilung den ersten noch nicht vergebenen FA der Position zu (Zeilen 802 bis
  845). Das ist eine Reihenfolgeregel, kein Pegging.
- Die Rückstandsliste ist laut Feldliste „Liste (1 Zeile pro KA-Position)“ (`docs/shopfloor_paket_2026-10-16/1_Schnittstelle/feldliste.json`,
  Feld „VkNr“), und viele Stichproben haben ein leeres FA-Feld (`Ergaenzung_und_Pruefpunkte.md` Zeilen 21 bis 24). Für eine
  KA-Position ohne FA kommt der WE-Termin nur über `AFPO`/`PLAF` mit `KDAUF/KDPOS` oder über das Pegging.

**Zweite Lesart von „Datum WE geplant“ (offen):** In der Config steht `we_geplant` in der Gruppe „Fehlmaterial“ direkt nach der
Auswahl Print/Anschluss/Membrane/Keramikzelle/Sonstiges (`shopfloor_config.json` um Zeile 1154). Fabios Stichproben zeigen
verschiedene Werte für Eckendtermin und WE geplant (RS-SEH-26-004: 16.06. gegen 23.06.; RS-TR5-26-204: 01.10. gegen 24.09.). Dann
wäre „WE geplant“ der Wareneingang des **fehlenden Bauteils** (Bestellung oder FA der Komponente), eine andere Verknüpfung (Zeile →
Komponente → deren Zugang), die ZC22 nicht liefert. Anh Nhis Satz spricht für den FA der Zeile; die Platzierung spricht für das
Fehlteil. Die Stichproben selbst lassen sich in T76 nicht prüfen (FA 1249097 gibt es dort nicht, jünger als die Kopie).

## 6. d) Material und „VkNr“

- ZC22-Spalte „Material“ = `ZMM_UEB_FAUF-MATNR`, gefüllt über `MOVE-CORRESPONDING iafpo` (Zeile 515), `IAFPO-MATNR` =
  `AFPO-MATNR` bzw. `PLAF-MATNR` (Zeile 434); MARA und ZCKAPA werden mit demselben Wert gelesen (Zeilen 485, 775). `VBAP-MATNR`
  wird nicht in die Spalte geschrieben.
- Gegenprobe T76: bei allen **741** offenen FA mit Kundenauftrag ist `AFPO-MATNR = VBAP-MATNR` der Position (VBAP 1'172 Sätze der
  448 Aufträge gelesen, 0 Abweichungen). Für die Verknüpfung muss das Material also nicht zusätzlich verglichen werden; als Prüfung
  schadet es nicht.
- „VkNr“: ZC22 hat daneben „Alte Materialnummer“ (`MARA-BISMT`). Dass „VkNr“ die Materialnummer ist, stützen zwei Belege:
  `ZSTR_ZLO_TOPDOWN-VKNR` (Property `Vknr` von `ZloTopDownSet`) hat das Datenelement `MATNR`, die Komponente daneben heisst `IDNRK`
  (DD03L); und der P76-Variantentext `/PPA EKX ALLE` „Aufträge EKx alle VkNr“ selektiert Material 1 bis 99999
  (`P76_BEFUND_2026-11-13.md` Abschnitt 1.3). Gemeint sind danach die numerischen Materialnummern (z. B. 55441, intern
  `000000000000055441`); daneben gibt es alphanumerische Materialien wie F90183 oder C30115 (`P76_BEFUND_2026-11-13.md` 3.3), für die
  der Variantentext nichts sagt.

## 7. e) OData in `ZPOWERBI_EINKAUF_SRV`

Gelesen: `ZCL_ZPOWERBI_EINKAUF_MPC_EXT==CM001` (`DEFINE`, 3'062 Zeilen), die generierte Basis `ZCL_ZPOWERBI_EINKAUF_MPC======CM001`
(`DEFINE` mit 62 `define_*`-Aufrufen, darunter keiner für VBBE, PLAF oder AFKO) und daraus `DEFINE_VBEP` (`CM00G`), `DEFINE_AFPO` (`CM00S`), `DEFINE_VBAK` (`CM00F`),
`DEFINE_ZPOWERBI_KDAUF` (`CM01A`), dazu `ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W` (`GET_ENTITYSET`, 2'886 Zeilen). Kein Aufruf im
Gateway Client.

| Set | Termin- und Verknüpfungsfelder | Befund |
| --- | --- | --- |
| `ShopAufSet` (`ZSTR_SHOP_AUF`, 12 Felder) | `Datum` = `AFKO-GLTRP` (FA, Typ VF) bzw. `PLAF-PEDTR` (Typ VP), DPC `CM01W` Zeilen 777 und 789 | kein KDAUF/KDPOS, kein Kundentermin, kein WEBAZ |
| `Zc23Set` (`ZSTR_ZC23_APP`) | `Edatu` („Bestätigtes Datum“, `VBEP-EDATU` der Einteilung mit `BMENG ≠ 0`), `Ketdat` („Wunschlieferdatum“), `Vbeln`, `Posnr`, `Etenr`, `Aufnr`, `Gltrp`, `Matnr` | nur bestätigte Einteilungen; FA-Zuordnung nach Reihenfolge (Abschnitt 5); Pflichtfilter Werk/Datvon/Datbis, SUBMIT ZC23 je Aufruf (DPC ab Zeile 1536) |
| `ZPOWERBI_KDAUFSet` (Tabelle `ZPOWERBI_KDAUF`) | `Edatu`, `Ketdat`, `Gltrp`, `Gltri` (CHAR20), `Vbeln`, `Posnr`, `Matnr` | Ablage aus dem ZC23-Job `BI UPDATE`, kein ETENR/AUFNR |
| `VBEPSet` (Basis-MPC) | `Vbeln`, `Posnr`, `Etenr`, `Ettyp`, `Bmeng`, `Lmeng`, `Cmeng` … (16 Properties) | **kein `Edatu`, kein `Mbdat`, kein `Wmeng`**; `VBEPSET_GET_ENTITYSET` in DPC_EXT redefiniert (`SEOREDEF`) |
| `AFPOSet` (Basis-MPC, 100 Properties) | `Kdauf`, `Kdpos`, `Kdein`, `Matnr`, `Dgltp`, `Webaz`, `Ltrmp`, `Elikz` | Verknüpfung FA → KA und Eckende (über `Dgltp`) wären da; `AFPOSET_GET_ENTITYSET` in DPC_EXT redefiniert; Datenlieferung nicht geprüft |
| `LogRueckSet` (`ZSTR_LOG_RUECK`) | `Rueck`, `Rmzhl`, `Datum`, `Aufnr`, `Vornr`, `Arbpl`, `Ersda`, `Lmnga`, `Matnr`, `Gamng` … | Rückmeldungen, kein Kundenauftrag, kein Termin |
| `LogTaSet`, `LogLiefSet`, `LogKapSet` | Transportaufträge, Lieferungen (`Vbeln`, `Wadat`, `WadatIst`), Kapazität | kein Bezug zu FA-Terminen oder Kundenauftragseinteilungen |
| `ZloTopDownSet` (`ZSTR_ZLO_TOPDOWN`) | `Vknr` (Datenelement `MATNR`), `Idnrk`, `Menge`, `Dispo` | kein Termin; Beleg für d) |

Die übrigen eigenen Sets aus `MPC_EXT` (`FinanzJournalSet`, `HrKpiSet`, `HrAbsenzSet`, `EinkKontraktSet`, `EinkMatLzSet`,
`ShopZd05Set`, `ShopRmSet`, `ShopProdSet`, `QmMeldungSet`, `CoPaSumSet`) haben nach ihren Property-Listen keinen Bezug zu
Kundenauftragsterminen; `ShopProdSet` hat `Datum` = Istendtermin (`NOTES.md` Abschnitt 2). Keines der geprüften Sets liefert
`VBBE-MBDAT`, Planaufträge mit `KDAUF` oder einen GIB-Termin. Für ZC22-gleiche Kundentermine oder einen VF-Termin des FA
(`GLTRP + WEBAZ` nach Fabrikkalender) müsste ein Set erweitert werden (Schreibauftrag, erst Plan).

## 8. Was das für die Rückstandsliste heisst (Vorschlag, nicht entschieden)

1. **Erst klären, welches GIB-Feld Anh Nhi meint.** Mit einem Bildschirmfoto der GIB-Ansicht für eine aktuelle Rückstandszeile
   (Auftrag, Position, FA) lässt sich jede der Lesarten unten an einem Beleg prüfen.
2. „Wunschtermin“ / „Bestätigter Liefertermin (VF)“, drei Kandidaten mit Werten für 409313/10:
   - A, MD04-Datum des Verkaufsbedarfs (`MBDAT` der offenen bestätigten Einteilung): 22.04. bzw. 06.05.2026;
   - B, Lieferdatum der offenen bestätigten Einteilung (`VBEP-EDATU`, `BMENG > 0`, noch offen): 24.04. bzw. 08.05.2026;
   - C, VF-Termin des FA (`GLTRP + WEBAZ`): FA 1241683 28.04.2026.
   ZC22 zeigt 22.04. („Termin bestätigt“) und 06.05. („Wunschtermin“). Eine Übernahme der ZC22-Regel hätte die Fallen aus 3.3
   (erste statt letzte Einteilung, unbestätigter Rest als „bestätigt“). Die Beschriftung „Liefertermin“ passt nur zu B.
3. „Datum WE geplant“: für den FA der Zeile `AFKO-GLTRP` (= `AFPO-DGLTP`), für den Planauftrag `PLAF-PEDTR`; das ist ZC22 „Eckendtermin“
   und nach Abschnitt 4 vermutlich der GIB-„WE-Termin“. Ist das Fehlteil gemeint (Abschnitt 5), ist es eine andere Quelle.
4. Hat die Position mehrere FA/Planaufträge (409313/10), muss entschieden werden, welcher gilt (frühester offener, spätester, Pegging).

## 9. Offen und in P76 zu bestätigen

1. **Welche Ansicht und welches Feld Anh Nhi meint** (GIB `/GIB/DCO` oder MD04, „VF-Termin“ der Kundenauftragszeile oder des FA) und
   ob „Datum WE geplant“ der FA der Zeile oder das Fehlteil ist. Frage an Anh Nhi über Fabio, am besten mit Bildschirmfoto.
2. Ob GIB in P76 produktiv genutzt wird (Transaktion `/GIB/DCO`, Benutzer der Dispo) und wie GIB `VF-Termin`/`WE-Termin` füllt;
   der GIB-Quelltext wurde nicht gelesen (kein Verwendungsnachweis), Lesen bräuchte eine Freigabe für die GUI oder die
   GIB-Dokumentation.
3. In P76 mit einer aktuellen Rückstandszeile (Freigabe Ingo nötig): ZC22 für den Auftrag gegen VBBE/VBEP/AFKO lesen und die
   nachgerechnete Regel aus 3.1 bestätigen (auch die Datenbankreihenfolge ohne `ORDER BY`).
4. Ob die gelieferten Einteilungen wirklich der Grund sind, dass VBBE nur einen Teil der Einteilungen enthält (407598/10), ist
   nicht geprüft (Lieferungen nicht gelesen).
5. Fabrikkalender für `GLTRP + WEBAZ`: in T76 nur an Werktagen ohne Feiertag nachgerechnet; ein eigener Bau müsste den
   Fabrikkalender des Werks verwenden.

## 10. Kandidaten für `docs/rag/LEARNINGS.md` (nicht eingetragen)

- Ein Fachbegriff aus der Dispo („VF-Termin“) war in Z-Datenelementen und ZC22 nicht zu finden, wohl aber in einem Zusatzpaket
  (`/GIB/DC_DATE_AV`). Regel: Bei unbekannten Spaltenbegriffen DD04T über alle Namensräume nach `SCRTEXT_S/M` suchen, bevor ein
  Feld zugeordnet oder geraten wird.
- ZC22 „Wunschtermin“/„Termin bestätigt“ sind Materialbereitstellungsdaten aus VBBE, nicht Lieferdaten aus VBEP, und „Termin
  bestätigt“ zeigt bei mehreren bestätigten Einteilungen die erste. Regel: Einen Report-Termin nie nach der Spaltenüberschrift
  übernehmen, sondern die füllende Form lesen und an Positionen mit mehreren Einteilungen nachrechnen.
- `rfc-call` mit Tabellen-Ausgabe: die Ausgabe in eine Datei schreiben und mit `|` als Trenner parsen; `awk -F` mit `"[ ]*\\|[ ]*"`
  trennte in Git Bash an Leerzeichen (Warnung „escape sequence `\|`“), die Spalten waren verschoben.
- Wiederholt (Fall 2026-11-13, `P76_BEFUND_2026-11-13.md` 5): `RunSapProbe.ps1 rfc-call … | Select-Object -First 80` brach mit
  Exitcode 255 ab; erst die Ausgabe in eine Datei (`*> datei`) lieferte alles. Regel: SapProbe- und cscript-Ausgaben nie mit `-First`
  kappen.
- ZC22 zeigt Kundenauftrag/Position aus `AUFK`, liest die Termine aber mit `AFPO`/`PLAF`; bei Feldern, die in mehreren
  `MOVE-CORRESPONDING`-Quellen vorkommen, die Reihenfolge der Zuweisungen gegen DD03L prüfen, bevor man eine Spalte einem Feld
  zuordnet.

## 11. Arbeitsdateien (Scratchpad, nicht im Repo)

`ZM_OFFENE_FAUF_T76.abap`, `ZM_OFFENE_KDAUF_T76.abap`, `afpo_open.csv` (2'079 FA), `afko_win.csv`, `vbep.csv`, `vbbe.csv`, `emul.ps1`
(Nachbau `KDAUF_TERMINE`) und `emul.csv` (741 Zeilen), `md04_55441.txt` (BAPI-Ausgabe), `mpc*.abap`, `dpc_cm01w.abap`. Keine
Kundennamen gelesen (KNA1 nicht abgefragt).
