# Nächster Vorgang wie MD04 für den Shopfloor-Forecast (nur gelesen, T76)

Stand: 2026-11-19 08:43 (Uhrzeit mit `Get-Date` abgelesen; RFC-Lesungen 08:32 bis 08:43). Nur gelesen, per RFC (`RunSapProbe.ps1`, T76/100, DPAPI-Ablage).
P76 nicht berührt, keine GUI-Sitzung, nichts angelegt, nichts geändert.
Auftrag: Ingo 19.11.2026 (Paket Fabio Palma, Wunsch Sergio Rodrigues): im Shopfloor-Forecast je Fertigungsauftrag den
„Nächsten Vorgang“ wie in der MD04 zeigen.

## 0. Kurzfassung

1. **Die MD04-Spalte kommt in T76 nicht aus `ZXM61U01`.** `ZXM61U01` ist der Exit des Planungslaufs (`EXIT_SAPMM61X_001`,
   Erweiterung M61X0001, Projekt `ZPP00006`, „Planungslauf nach Disponenten“) und setzt nur `NO_PLANNING`. Die MD04-Spalte
   (`MDEZ-USEX1`) füllt die BAdI-Implementierung **`ZZ_MD_ADD_COL_EZPS`** (BAdI `MD_ADD_COL_EZPS`, Klasse
   `ZCL_IM_Z_MD_ADD_COL_EZPS`, Transport `T76K904735` „Zusatzspalten MD04“, 2012). Sie ruft für Fertigungsaufträge
   (`DELKZ = 'FE'`) den Funktionsbaustein **`Z_MD04_NEXT_OPERATION`** und setzt `VORNR / ARBPL` zusammen.
2. **Derselbe Baustein steckt schon im Dashboard-Set.** `ShopAufSet` liefert seit `T76K912906` (laut `NOTES.md` seit 16.11. in P76)
   die Felder `Vornr`, `Arbpl` und `Avo`, gefüllt mit `Z_MD04_NEXT_OPERATION`, aber nur im Zweig Typ VR/VZ und nur mit
   Filter `Avo eq 'X'`. Der Forecast fragt `ShopAufSet` ohne `Typ` ab (Standardzweig PA/FE), dort bleiben die Felder leer.
3. Für den Forecast genügt deshalb entweder **eine kleine DPC-Ergänzung ohne Modelländerung** (Variante A) oder sogar
   **gar keine SAP-Änderung** (Variante 0, zweiter Abruf Typ VR mit `Avo`). Ein neues Feld `NaechsterVorgang` in Struktur
   und MPC (Variante B) ist möglich, aber nicht nötig.

## 1. Welcher Code die MD04-Spalte füllt (belegt)

| Beleg (T76, RFC) | Wert |
| --- | --- |
| `TFDIR` | `EXIT_SAPMM61X_001` → `SAPLXM61` Include 01 (= `ZXM61U01`); `EXIT_SAPLM61R_001` → Include 03; `EXIT_SAPMM61R_001` → Include 04 |
| `abap-read LXM61U01` | `FUNCTION EXIT_SAPMM61X_001` … Export `NO_PLANNING`, `STOP_PLANNING` … `INCLUDE ZXM61U01.` |
| `abap-read LXM61U03` | `FUNCTION EXIT_SAPLM61R_001`, Export `EMDEZX_USEX1` … `INCLUDE ZXM61U03 .` (der Exit, der USEX1 füllen würde) |
| `abap-read LXM61U04` | `FUNCTION EXIT_SAPMM61R_001`, Export nur `BUTTON1_EZ` … (Spaltentitel), `INCLUDE ZXM61U04 .` |
| `TRDIR NAME LIKE 'ZXM61U%'` | nur `ZXM61U01` (angelegt I001067 18.03.2014). `ZXM61U03` und `ZXM61U04` gibt es nicht |
| `MODSAP` | M61X0001 = `EXIT_SAPLM61C_001`, `EXIT_SAPMM61X_001`; M61X0002 = `EXIT_SAPLM61R_001`, `EXIT_SAPMM61R_001` |
| `MODACT MEMBER LIKE 'M61%'` | nur `ZPP00006` → `M61X0001`; **M61X0002 ist in keinem Projekt** |
| `SXC_EXIT EXIT_NAME = 'MD_ADD_COL_EZPS'` | einzige Implementierung `ZZ_MD_ADD_COL_EZPS` |
| `SXC_ATTR` | `ZZ_MD_ADD_COL_EZPS` `ACTIVE = X` (I001067, 19.03.2012) |
| `SXC_CLASS` | Klasse `ZCL_IM_Z_MD_ADD_COL_EZPS`, Interface `IF_EX_MD_ADD_COL_EZPS` |
| `CROSS NAME = 'Z_MD04_NEXT_OPERATION'` | aufgerufen von `ZCL_IM_Z_MD_ADD_COL_EZPS======CM002`, `ZM_OFFENE_FAUF` (ZC22), `ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W` |

Fabios Angaben „`EXIT_SAPMM61R_001`, M61X0002, `ZXM61U01`“ passen also nicht zusammen: `EXIT_SAPMM61R_001` gehört zu
M61X0002, `ZXM61U01` aber zu `EXIT_SAPMM61X_001` (M61X0001). Für T76 ist belegt, dass M61X0002 nicht aktiv ist und die
Spalte aus dem BAdI kommt. Ob P76 genauso eingerichtet ist (BAdI aktiv, M61X0002 ohne Projekt), ist **nicht geprüft**
(Abschnitt 7).

### 1.1 Quelltext `ZXM61U01` (T76, vollständig)

```abap
  clear: no_planning, stop_planning.
  case user_key.
    when 'Z01'.
      select count(*) from z024d where werks = mt61d-werks
                                 and   dispo = mt61d-dispo.
      if sy-subrc ne 0.
        no_planning = 'X'.
      endif.
   when 'Z02'.
     select count(*) from z024m where werks = mt61d-werks
                                and   matnr = mt61d-matnr
                                and   dispo = mt61d-dispo.
      if sy-subrc ne 0.
        no_planning = 'X'.
      endif.
  endcase.
```

Das ist der Planungslauf-Exit (Benutzerschlüssel Z01/Z02, Tabellen `Z024D`/`Z024M`); mit dem nächsten Vorgang hat er nichts
zu tun.

**Stand gegen Fabios P76-Metadaten:** `VRSD` `ZXM61U01`: Version 00001 LAA 19.10.2023 14:17 (ohne Auftrag), aktive Version
00000 mit Auftrag `T76K907994`, LAA 29.07.2024 17:19. `E070`: `T76K907994` (K, freigegeben, LAA, 29.07.2024) mit Aufgabe
`T76K907995` („Tabelle für MRP-Lauf UserExit Z024M“). Das deckt sich mit Fabios „zuletzt geändert LAA 29.07.2024“ und
„angelegt 2014“ (`TRDIR` CDAT 18.03.2014, Ersttransport `T76K905376` „Planungslauf nach Disponenten“). **Vermutung:**
`ZXM61U01` ist in T76 und P76 gleich (gleicher Auftrag, freigegeben); belegt wäre es erst mit einem Versionsvergleich in P76.
`TRDIR-UDAT` in T76 zeigt 19.10.2023, nicht 29.07.2024; warum, ist nicht untersucht.

### 1.2 Quelltext der MD04-Spalte: `ZCL_IM_Z_MD_ADD_COL_EZPS`

`IF_EX_MD_ADD_COL_EZPS~ACTIVATE_ADD_COLUMNS` (CM001): `button1_ez = text-ez1.`, `ez1_mode = '3'.` (nur Spalte 1 aktiv).

`IF_EX_MD_ADD_COL_EZPS~FILL_ADD_COLUMNS` (CM002), Zeilen 23 bis 43 wörtlich:

```abap
  if ie3mdpslx-delkz = 'FE'.
    call function 'Z_MD04_NEXT_OPERATION'
      exporting
        i_aufnr = ie3mdpslx-del12
      importing
        e_vornr = l_vornr
        e_arbpl = l_arbpl.

   if not l_vornr is initial.
     concatenate l_vornr l_arbpl into lv_column1 separated by ' / '.
   endif.

*** Re-enable button/column if data exists ***
* Column 1
    if cbutt1 = '0' and not lv_column1 is initial.
      cbutt1 = sv_ez1.
    endif.

********** Move data to user-fields **********
    move lv_column1 to emdezx_usex1.
  endif.
```

Planaufträge (`DELKZ` PA) und alle anderen Elemente bekommen nichts; das erklärt die leeren Zeilen in der MD04.
`MDEZ-USEX1` ist `CHAR 30` (DD03L, Datenelement `USEEX`).

Versionen: `TRDIR` aller Klassen-Includes und von `LZZMD04U01`/`LZZMD04TOP`: angelegt und zuletzt geändert I001067 am
19.03.2012 bzw. 21.03.2012; `VRSD` hat für diese Objekte keine Einträge. Transporte: `T76K904735` „Zusatzspalten MD04“ (CLAS,
FUGR `ZZMD04`, SXCI) und `T76K904743` (FUNC `Z_MD04_NEXT_OPERATION`), beide freigegeben 2012, dazu SAP-Upgrade-Aufträge
`SAPKCCC75x`/`SAPKCCD758`. **Vermutung:** seit 2012 unverändert, in P76 gleich.

### 1.3 Die Regel: `Z_MD04_NEXT_OPERATION`

`TFDIR`: `SAPLZZMD04`, Include `LZZMD04U01`, `FMODE` leer (nicht RFC-fähig). Quelltext in T76 heute zeilengleich mit
`saptasks/shopfloor_sap/Z_MD04_NEXT_OPERATION_T76_2026-11-13.abap` (`diff` rc 0, 51 Zeilen). Globale Tabellen `iafvc`,
`iafvv` in `LZZMD04TOP`. Entscheidende Zeilen:

```abap
  select single aufpl into l_aufpl from afko where aufnr = i_aufnr.
  ...
  select * from afvc into table iafvc where aufpl = l_aufpl
                                      and   steus ne 'PP00'.
  select * from afvv into table iafvv where aufpl = l_aufpl.
  sort iafvc by vornr.
  loop at iafvc.
    ...
    l_ist = iafvv-lmnga + iafvv-xmnga.
    l_pln = iafvv-mgvrg + iafvv-asvrg.
    if l_ist ge l_pln.                                      "Vorgangsmenge erreicht
      delete iafvc.                                         "Vorgang fertig
    else.
      select count(*) from jest where objnr = iafvc-objnr   "Vorgang Endrückgemeldet?
                                and   ( stat = 'I0009' or
                                        stat = 'I0013' )    "oder gelöscht
                                and   inact = ' '.
      if sy-subrc = 0.
        delete iafvc.                                       "Vorgang fertig
      endif.
    endif.
  endloop.
  read table iafvc index 1.
  if sy-subrc = 0.
    e_vornr = iafvc-vornr.
    select single arbpl from crhd into e_arbpl where objty = 'A'
                                               and   objid = iafvc-arbid.
  endif.
```

Die Regel in Sätzen:

1. Gelesen werden alle Vorgänge des Auftrags (`AFKO-AUFPL` → `AFVC`), ausser denen mit Steuerschlüssel `PP00`
   („Eigenfertigung ohne Rückmeldung“, `T430T` D).
2. Ein Vorgang gilt als fertig, wenn die rückgemeldete Gut- plus Ausschussmenge (`AFVV-LMNGA + XMNGA`) die Vorgangsmenge plus
   geplanten Ausschuss (`AFVV-MGVRG + ASVRG`) erreicht, **oder** wenn der Vorgang aktiv den Systemstatus `I0009` RÜCK
   (endrückgemeldet) oder `I0013` LÖKZ trägt (`TJ02T` D). Teilrückgemeldet (`I0010` TRÜC) zählt nicht als fertig.
3. Der nächste Vorgang ist unter den nicht fertigen Vorgängen der mit der kleinsten Vorgangsnummer (`SORT BY vornr`, Index 1). Es gibt keine
   Unterscheidung nach Folge (`PLNFL`) oder Untervorgang (`SUMNR`) und keinen Blick auf den Kopfstatus des Auftrags
   (TECO, ABGS).
4. Ausgabe `E_VORNR` und `E_ARBPL` (`CRHD-ARBPL` zur `AFVC-ARBID`). Ohne offenen Vorgang bleiben beide leer, die MD04 zeigt
   dann nichts.
5. Unterscheidung Plan-/Fertigungsauftrag macht nicht der Baustein, sondern der Aufrufer (`DELKZ = 'FE'` in CM002; im DPC
   `typ = 'VF'`; in ZC22 `iafpo-objnp ne 'PLAF'`).
6. Format der MD04: `CONCATENATE l_vornr l_arbpl SEPARATED BY ' / '` → `0800 / DSQ72`, höchstens 30 Zeichen.
   *Vermutung:* bei leerem Arbeitsplatz steht „`0800 /`“ (Verhalten von `CONCATENATE` mit Trennzeichen nicht nachgelesen).

Kosten: 4 Lesezugriffe je Auftrag plus einer je offenem Vorgang; gemessen in T76 am 13.11. 7,40 ms je Auftrag
(2'197 Aufträge 16,2 s, `PLAN_LAYOUTFILTER_2026-11-13.md` 1.8).

## 2. Aufrufbarkeit ausserhalb der MD04

- Die Regel ist ein **eigener Funktionsbaustein** und braucht nur die Auftragsnummer (`I_AUFNR`, ohne Typ, also ohne
  Konvertierung mit führenden Nullen übergeben, wie im DPC aus `AFKO-AUFNR`). Werk und Material braucht sie nicht. Sie hängt
  nicht an `MDEZ`/`MDPS`; nur das Zusammensetzen des Textes steht im BAdI.
- Belegt aufgerufen von drei Stellen (`CROSS`, Abschnitt 1): MD04-BAdI, ZC22, DPC `ShopAufSet`.
- Nicht RFC-fähig (`TFDIR-FMODE` leer), also nicht direkt vom Dashboard aufrufbar, nur über ein Gateway-Set oder einen Report.

## 3. Wie das Dashboard die Aufträge heute holt

- `Services/Shopfloor/ShopfloorSapSync.cs` Zeilen 102 bis 104: `ShopAufSet` mit `$filter=Werks eq '<Werk>' and Datum eq
  '<heute>'`, ohne `Typ`, ohne `Avo`. Zeilen 420 bis 438 (`OrderRows`): `day`, `auftrag`, `material`, `menge`, `disponent`,
  **`["arbeitsplatz"] = string.Empty`**. Die Spalte „Arbeitsplatz“ der Forecast-Tabelle (`wwwroot/shopfloor/app.js` Zeile 1797,
  `o.arbeitsplatz`) bleibt deshalb aus SAP immer leer.
- DPC `ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W` (T76, heute gelesen), Zweig `ShopAufSet` Zeilen 646 bis 947:
  - Typ VR/VZ (Zeilen 724 bis 881): FA als `typ = 'VF'`, PA als `VP`; nach dem Paging Statuszeile immer, und **nur bei
    `Avo eq 'X'`** (Zeilen 869 bis 877) `Z_MD04_NEXT_OPERATION` → `vornr`, `arbpl`, `avo = 'X'`.
  - Ohne Typ (Standardzweig, Zeilen 882 bis 946, das ist der Forecast): Pflicht Werks und Datum (Datum zwischen heute − 60
    und heute + 120), PA mit `PEDTR` in [Datum, Datum + 60], FA mit `GLTRP` in [Datum, Datum + 60], `ELIKZ`/`DNREL` leer,
    `PSMNG > WEMNG`, `typ = 'FE'`, Menge `PSMNG − WEMNG`. **`Avo` wird hier nicht ausgewertet**, `Vornr`/`Arbpl` bleiben leer.
- Struktur `ZSTR_SHOP_AUF` (DD03L, T76): WERKS CHAR4, AUFNR CHAR12, TYP CHAR2, MATNR CHAR40, DISPO CHAR3, DATUM CHAR8,
  MENGE INT4, MEINS UNIT3, STTXT CHAR100, **ARBPL CHAR8, VORNR CHAR4, AVO CHAR1**.
- MPC `ZCL_ZPOWERBI_EINKAUF_MPC_EXT==CM001` (T76): Entity `ShopAuf` mit Properties `Arbpl` (Zeile 981), `Vornr` (987),
  `Avo` (993), `bind_structure( 'ZSTR_SHOP_AUF' iv_bind_conversions = abap_true )` (1000).
- Im Cockpit ist der Arbeitsplatzfilter `M*` eingeschaltet (Arbeitskopie `appsettings.json` Zeile 102 `"Zc22ArbplAusschluss": "M*"`;
  produktiv laut `docs/SHOPFLOOR_2026-10-07.md`, Stand 17.11. 12:30, „`M*` ein“, nicht selbst nachgesehen), der
  Vorrat-Abruf (Typ VR) fordert also heute schon `Avo eq 'X'` an (`ShopfloorSapVorrat.cs` Zeile 70, `ShopfloorSapSync.cs`
  Zeile 339). Er liefert `Vornr`/`Arbpl` für alle offenen FA des Werks; der Code liest daraus nur `Arbpl` für den Filter.

## 4. Plan (nichts angelegt)

Gemeinsam für alle Varianten: Die Logik bleibt `Z_MD04_NEXT_OPERATION`, unverändert, damit Forecast, ZC22 und MD04 dasselbe
zeigen. Anzeige im Dashboard als `Vornr + " / " + Arbpl`, leer wenn `Vornr` leer, wie CM002.

### Variante 0: ohne SAP-Änderung (nur Dashboard)

Im Forecast-Lauf zusätzlich `ShopAufSet` Typ VR mit `Avo eq 'X'` lesen (oder die Zeilen des Vorrat-Abrufs wiederverwenden,
falls beide im selben Lauf stehen) und je `Aufnr` den Text in die FE-Zeilen setzen. VR liefert alle offenen FA ohne
Datumsgrenze (erste offene Position je Auftrag), also eine Obermenge der Forecast-FA (gleiche Bedingungen `ELIKZ`/`DNREL`,
nur ohne Fenster und ohne `PSMNG > WEMNG`).
Kosten: Baustein für **alle** offenen FA des Werks (T76 2'197 → 16,2 s); P76-Menge nicht gemessen. Kein Transport.
Nachteil: teurer als nötig und an den Vorrat-Abruf gekoppelt.

### Variante A: DPC ergänzen, kein neues Feld (Empfehlung, Vorschlag)

Objekte: nur `ZCL_ZPOWERBI_EINKAUF_DPC_EXT->/IWBEP/IF_MGW_APPL_SRV_RUNTIME~GET_ENTITYSET`, eigener Block im Zweig
`ShopAufSet`, Standardzweig. Kein DDIC, kein MPC (Felder `Vornr`, `Arbpl`, `Avo` sind da), deshalb kein Cache-Leeren nötig
(im T76-Test trotzdem `/IWBEP`, dann `/IWFND`, schadet nicht).

Schritte (Phase 2 erst nach Freigabe, Muster `SAP_ARBEITSWEISE_UND_WERKZEUGE.md` Abschnitt 7):
1. Vorher: `E071`/`E070`/`TLOCK` auf die DPC-Klasse prüfen (heute 08:43: `E071` nennt die Klasse in 138 Aufträgen, laut `E070` alle 138
   freigegeben (`R`), zuletzt `T76K912906`/`T76K912907` am 16.11.; `TLOCK` `LOKEY LIKE '%ZPOWERBI_EINKAUF_DPC%'` ohne Treffer). Eigenen neuen
   T76-Auftrag anlegen, Text z. B. „Shopfloor: ShopAufSet Forecast naechster Vorgang“ (≤ 60 Zeichen).
2. Stand vorher sichern (`abap-read …CM01W` → `saptasks/shopfloor_sap/DPC_EXT_GET_ENTITYSET_2026-11-xx_vorgang_vorher.abap`).
3. Im Standardzweig nach dem Paging (vor dem letzten `copy_data_to_ref`, heute Zeile 945) einfügen:
   `IF lv_sa_avo = 'X'. LOOP AT lt_sa_out ASSIGNING <ls_sa_a> WHERE typ = 'FE'. CALL FUNCTION 'Z_MD04_NEXT_OPERATION'
   EXPORTING i_aufnr = <ls_sa_a>-aufnr IMPORTING e_vornr = <ls_sa_a>-vornr e_arbpl = <ls_sa_a>-arbpl. <ls_sa_a>-avo = 'X'.
   ENDLOOP. ENDIF.` (Variablen `lv_sa_avo` und `<ls_sa_a>` sind im Zweig schon deklariert und befüllt, Zeilen 689, 703, 716.)
   Nur nach dem Paging, damit nur die ausgegebenen Zeilen den Baustein kosten.
4. Sichern mit ausdrücklich eigenem Auftrag, aktivieren, zurücklesen, `diff` gegen vorher: nur der neue Block anders.
5. Test T76 Gateway Client: `ShopAufSet?$filter=Werks eq '1100' and Datum eq '<heute−60>' and Avo eq 'X'&$top=20`.
   Vorher in Phase 1 mit `table-read` zählen, ob T76 im Fenster [heute−60, heute] FA mit `DWERK = 1100`, `ELIKZ`/`DNREL` leer
   und `PSMNG > WEMNG` hat (nicht geprüft; grob gezählt liegen zwischen 20.09.2026 und 20.01.2027 nur 115 AFKO-Aufträge aller
   Werke und Zustände, die Testdaten enden etwa April). Die Aufträge aus Abschnitt 6 (Eckende April) kann der Standardzweig
   nicht liefern; sie über den bestehenden Weg `Typ eq 'VR' and Avo eq 'X'` gegenprüfen. Prüfung des neuen Blocks: dessen
   FE-Zeilen mit den VR-Zeilen derselben `Aufnr` vergleichen (gleicher Baustein, also gleiche `Vornr`/`Arbpl`).
   Erwartung: FE-Zeilen mit `Vornr`/`Arbpl`, PA leer; derselbe Aufruf ohne `Avo` unverändert. Laufzeit vorher/nachher notieren.
6. Rückbau: Block entfernen bzw. Stand „vorher“ zurückspielen, aktivieren.
7. Ingo: Freigabe und Import nach P76; danach die erste Abfrage nicht als Befund nehmen (LEARNINGS 16.11.).

Dashboard (eigener Commit): Filter in `ShopfloorSapSync.cs` Zeile 104 um ` and Avo eq 'X'` ergänzen; in `OrderRows` ein Feld
`naechster_vorgang` (oder `arbeitsplatz`) aus `Vornr`/`Arbpl` füllen; Spalte in der Forecast-Tabelle, neue UI-Texte mit allen
Übersetzungen; im selben Commit den `sap`-Block in `shopfloor_config.json` nachziehen (LEARNINGS 17.11.); Test für die
Zusammensetzung und für leere PA-Zeilen. *Vermutung:* Vor dem P76-Import würde `Avo` im Standardzweig einfach ignoriert (das
Property gibt es laut `NOTES.md` in P76 seit 16.11.; der heutige T76-Code wertet `Avo` im Standardzweig nicht aus), es käme
also kein Fehler, nur leere Felder.

Wichtig für „genau wie MD04“: Der Forecast speichert den Stand des letzten Abgleichs (04:00 und 12:45). Die MD04 rechnet bei
jedem Aufruf neu; nach einer Rückmeldung zeigt das Dashboard den neuen Vorgang erst nach dem nächsten Abgleich.

### Variante B: neues Feld `NaechsterVorgang` wie gefragt

Struktur `ZSTR_SHOP_AUF` um `NXTVG` CHAR30 (wie `MDEZ-USEX1`) erweitern, MPC `DEFINE` Property `NaechsterVorgang`, DPC füllt
es mit `CONCATENATE … SEPARATED BY ' / '` wie CM002 (in beiden Zweigen). Danach in T76 `/IWBEP`, dann `/IWFND` Cache leeren,
`$metadata` und Set prüfen; in P76 nach dem Import dasselbe (Ingo). Mehr Objekte (DDIC + MPC + DPC), Cache-Pflicht, gleicher
Nutzen wie A. Nur sinnvoll, wenn der Text exakt aus SAP kommen soll (z. B. für Power BI).

## 5. Aufwand SAP-Teil (Schätzung)

| Variante | Plan | Bau und Test T76 | Transport | Summe |
| --- | --- | --- | --- | --- |
| 0 | – | – | – | 0 h SAP (Dashboard 3–4 h) |
| A | 0,5 h | 1–1,5 h | 1 Auftrag, nur Klasse | **rund 2 h** (Dashboard 2–3 h) |
| B | 1 h | 2–3 h (SE11, MPC, DPC, Caches) | 1 Auftrag, Struktur + Klassen | rund 4 h |

Dazu Ingos Freigabe, Import und die Prüfung in P76.

## 6. Stichprobe T76 (Regel von Hand nachgerechnet)

Auswahl: `AFKO` mit `DISPO IN ('DS1','DS2','DSN')` und `GLTRP >= 20260301`: 318 Aufträge (DS1 219, DS2 99, **DSN 0**).
Davon offen nach `AFPO`: `ELIKZ` und `DNREL` leer, `WEMNG = 0`. Kopfstatus (`JEST` `OR…`): alle sechs `I0002` FREI, keiner
TECO/ABGS/LÖKZ; fünf zusätzlich `I0010` TRÜC. Gelesen `AFVC` (105 Vorgänge, `PLNFL` überall `000000`), `AFVV`, `JEST`
(`OV…`, Status I0009/I0010/I0013), `CRHD`. Rechnung mit `nachrechnen.py` (Scratchpad) genau nach Abschnitt 1.3.
Menge in ST (`AFVV-MEINH`), Vorgangsmenge = Auftragsmenge.

| Auftrag | Dispo | Material | Menge | Ablauf | Ergebnis `VORNR / ARBPL` |
| --- | --- | --- | ---: | --- | --- |
| 1240286 | DS1 | C42335 | 70 ST | 0001 bis 0450 je 70/70 und RÜCK; 0500 `PP00` (übersprungen); **0600 EL910 35/70, Status TRÜC** → offen | **0600 / EL910** |
| 1239156 | DS1 | C42439 | 52 ST | 0001 bis 0700 je 52/52 und RÜCK (0500 PP00); 0800 EL974 0/52 | **0800 / EL974** |
| 1239786 | DS1 | C17317 | 69 ST | 0001 bis 0920 (DSQ72) je 69/69 und RÜCK; 0960 DL220 0/69 | **0960 / DL220** |
| 1238149 | DS1 | C36554 | 60 ST | bis 0460 fertig (0010, 0300, 0400 PP00); 0500 DL220 0/60 | **0500 / DL220** |
| 1238932 | DS1 | C36554 | 60 ST | wie 1238149 | **0500 / DL220** |
| 1240813 | DS2 | 60848 | 60 ST | noch nichts rückgemeldet; erster Vorgang 0010 DL068 | **0010 / DL068** |

Beispiel 1240286 ausführlich: Vorgang 0500 (`PP00`, 35/70, TRÜC) fällt durch den Steuerschlüssel weg, nicht durch die Menge;
0600 (`PP01`) hat 35 + 0 < 70 + 0 und nur TRÜC, also offen und kleinste verbleibende Nummer.

## 7. Belegt, Vermutung, nicht geprüft

**Belegt (T76, 19.11. 08:32 bis 08:43):** Abschnitte 1 bis 3 und 6 mit den genannten Tabellen und Quelltextzeilen; `ZXM61U01` ist der
Planungslauf-Exit; die MD04-Spalte kommt aus `ZZ_MD_ADD_COL_EZPS` → `Z_MD04_NEXT_OPERATION`; `ShopAufSet` hat die Felder,
füllt sie im Forecast-Zweig aber nicht.

**Vermutung:** `ZXM61U01`, BAdI-Klasse und `Z_MD04_NEXT_OPERATION` sind in P76 gleich wie in T76 (gleiche freigegebene
Aufträge, seit 2012 bzw. 2024 unverändert). Format bei leerem Arbeitsplatz „`0800 /`“. Aufwandsschätzung.

**Nicht geprüft:**
- P76 (Auftrag: nur T76). Für Fabio/Ingo zu prüfen: in P76 `SE18`/`SE19` `MD_ADD_COL_EZPS` → `ZZ_MD_ADD_COL_EZPS` aktiv,
  `CMOD`: ist M61X0002 in einem Projekt aktiv (dann gäbe es dort `ZXM61U03`/`ZXM61U04`, und Fabios Angabe könnte für P76
  stimmen); Versionen von `LZZMD04U01` und `ZCL_IM_Z_MD_ADD_COL_EZPS` vergleichen.
- Die MD04 selbst (Bildschirm): die Stichprobe ist die Regel von Hand, kein Vergleich mit der MD04-Anzeige (GUI war
  ausgeschlossen).
- Spaltentitel `text-ez1` (Textpool der Klasse) nicht gelesen; Fabio nennt „Nächster Vorgang“.
- Laufzeit der Variante A in P76 und Zahl der FE-Zeilen im Forecast-Fenster in P76.

## 8. Nachtrag 19.11. 10:15: P76 durch Fabio gelesen

Fabio hat in P76 per SE16N gelesen: `SXC_ATTR` `ZZ_MD_ADD_COL_EZPS` ACTIVE = X (Definition `MD_ADD_COL_EZPS`, angelegt 2012);
`MODACT` M61X0001 im Projekt `ZPP00006`, `MODATTR` `ZPP00006` STATUS = A; `MODACT` M61X0002 ohne Treffer. Damit gilt der Befund aus
Abschnitt 0 auch fuer P76. Vorbehalt: Fabios eigene SAP-Learnings (19.11.) sagen, `MODACT` mit MEMBER-Filter habe auch bei einem
wirkenden Exit 0 Treffer geliefert; die Aussage „M61X0002 in keinem Projekt“ ist deshalb schwach belegt. Fuer die MD04-Spalte
spielt das keine Rolle. Der Quelltext von `Z_MD04_NEXT_OPERATION` in P76 ist nicht gelesen. Abnahme gegen die MD04: 20.11.
