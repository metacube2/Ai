# ZZPRDAT: Anzeige des Produktionsdatums im Reiter „TRAFAG Daten“

Stand: 2026-11-06 08:15. Nur T76, Mandant 100. P76 wurde nicht angefasst.

Auftrag von Ingo am 2026-11-05/06: „mach neuen transport“ und „entsperre alles von georg dann ist es
einfacher“.

**Ergebnis kurz:** Georgs Sperre ist weg (SE03 `RSWBO099`), `ZXCO1U11` liegt im neuen Auftrag
`T76K912851` und ist aktiv, CO03 zeigt fuer Auftrag 1241847 im Reiter „TRAFAG Daten“ jetzt
11.11.2026.

**Bedingung vor der Freigabe von `T76K912851`:** In P76 `ZXCO1TOP` lesen. Die neue Zeile
`gs_aufk-zzprdat = i_caufvd-zzprdat.` braucht dort `DATA: gs_aufk TYPE ci_aufk.` und
`TABLES: ci_aufk`. In T76 stehen beide in `ZXCO1TOP`, dessen aktiver Stand von Georg ist (REPOSRC
MEEY_GEWA 20260318, offen in `T76K911784`, nicht importiert). Fehlt `gs_aufk` in P76, waere die
Funktionsgruppe `SAPLXCO1` nach dem Import syntaxfehlerhaft und CO01/CO02/CO03 brechen ab. Dann
entweder Georgs Objekte mitnehmen oder die Zeile mit `gs_aufk` streichen; fuer die Anzeige reicht
`ci_aufk-zzprdat`, weil das Bild nachweislich dieses Feld zeigt (Abschnitt 1). Entscheid Ingo.

## 1. Befund

Das Produktionsdatum wird korrekt in `AUFK-ZZPRDAT` geschrieben, und zwar allein durch das BAdI
`Z_ZZPRDAT_UPDATE` (Auftrag `T76K912490`). Der Reiter „TRAFAG Daten“ in CO02/CO03 zeigte es trotzdem
leer.

| Beleg | Wert |
| --- | --- |
| `AUFK` Auftrag `000001241847` | `AUART PP22`, `ZZPRDAT 20261111`, `ERDAT 20261105` |
| `AFKO` Auftrag `000001241847` | `GLTRP 20261111`, `FTRMI 20261105` |
| Bildfeld des Reiters (CO03-Dump vor der Aenderung) | Unterbild `SAPLXCO1:0100`, Feld `txtCI_AUFK-ZZPRDAT`, Inhalt leer (`co03_1241847_reiter_vorher.txt`) |
| Exit `EXIT_SAPLCOKO1_001`, Include `ZXCO1U11` | alles auskommentiert, auch `Move i_caufvd-zzprdat to ci_aufk-zzprdat` (`ZXCO1U11_vorher.abap`) |

Das Bild zeigt also `CI_AUFK-ZZPRDAT` (globale `TABLES: ci_aufk` in `ZXCO1TOP`), und niemand fuellt es.

## 2. Sperre von Georg und Entsperrung

`ZXCO1U11` war mit sechs weiteren Objekten in Georgs (MEEY_GEWA) offenem Auftrag gesperrt.

| Auftrag | Art | Status | Ziel | Mandant (`E070C`) | Text |
| --- | --- | --- | --- | --- | --- |
| `T76K911783` | K Workbench | D | P76 | 090 | PPCO0012 FAUF: Änd neu impl. / Progr #2 |
| `T76K911784` | S Aufgabe | D | – | 090 | (Aufgabe zu 783) |

Mandant 090 passt zur SAP-Meldung „nicht fuer diesen Mandanten bestimmt“ beim Speicherversuch in
Mandant 100 (wie LEARNINGS 2026-10-20 bei ZLO03); gemessen ist nur der Eintrag in `E070C`.

Gesperrte Objekte (`E071` der Aufgabe `T76K911784`, `LOCKFLAG X`, dazu sieben `TLOCK`-Saetze mit
`AUTHOR MEEY_GEWA`, `EDTFLAG X`): `ZXCO1O01`, `DYNP SAPLXCO10100`, `ZXCO1I01`, `ZXCO1U12`, `ZXCO1U11`,
`ZXCO1TOP`, `ZXCO1U06`. Stand vorher: `sperren_vorher.txt`. Die Includes liegen auch in aelteren,
freigegebenen Auftraegen (`T76K910879/880`, `T76K911110/111`, `T76K911392/393`, `T76K911396/397`);
dort ist nichts gesperrt.

**Weg:** SE03, Knoten `RSWBO099` „Objekte entsperren (Expertentool)“ (Report-Titel in `TRDIRT` belegt,
Quelltext gelesen). Selektion: `TRKORR`, nur „Objektliste entsperren“ (`P_UNLOCK`, fest X);
„Objekte versionieren“, „Auf Freigegeben setzen“ (`P_SETREL`) und „Transport von Kopien erzeugen“
bewusst leer. Der Report ruft `TRINT_UNLOCK_REQUEST` mit `iv_set_to_released = P_SETREL`, gibt also
nichts frei. Er lief aus Mandant 100 ohne Einwand (Berechtigung `TABL`/`UNLK` vorhanden).

1. Lauf mit `T76K911783`: Meldung „Auftrag/Aufgabe T76K911783 wurde entsperrt“. Die Sperren blieben,
   weil sie an der Aufgabe haengen; nur der Zeitstempel von 783 aenderte sich (`AS4DATE 20261106`,
   `AS4TIME 080535`).
2. Lauf mit `T76K911784`: Meldung „Auftrag/Aufgabe T76K911784 wurde entsperrt“.

Skripte dazu (neu, `.tmp_sap_probe/`): `SapGuiSe03Knoten.vbs <Ses> RSWBO099`, dann
`SapGuiRswbo099Entsperren.vbs <Ses> fuellen <Auftrag>`, `... starten`, `... knopf
wnd[1]/usr/btnSPOP-VAROPTION1`, `... knopf wnd[1]/tbar[0]/btn[0]`; Baum lesen mit
`SapGuiBaumLesenSitzung.vbs`.

**Beleg nachher** (`sperren_nachher.txt`): `TLOCK` fuer 783/784 und fuer `HIKEY LIKE '%ZXCO1%'` leer;
`E071` der Aufgabe weiter alle sieben Zeilen, `LOCKFLAG` jetzt leer; `E070` beider Auftraege weiter
Status `D` (nicht freigegeben). Georgs Auftrag ist nicht geloescht, nicht freigegeben, keine Objekte
entfernt. Er bleibt mit seiner Objektliste bestehen, sperrt aber nichts mehr.

## 3. Neuer Auftrag und Aenderung

| | |
| --- | --- |
| Auftrag | `T76K912851` „ZZPRDAT: Anzeige Produktionsdatum im Reiter TRAFAG Daten“, Workbench, Ziel P76, Mandant 100, KOI |
| Aufgabe | `T76K912852` |
| Objekt | `LIMU REPS ZXCO1U11` (einziges Objekt) |

Der Speicherdialog schlug `T76K912839` (ZLO03 FIX 26) vor; der wurde nicht genommen, sondern mit
`SapGuiAuftragNeu.vbs` der neue Auftrag angelegt.

Inhalt: unveraendert plus vier Zeilen oben (`ZXCO1U11_neu.abap`):

```abap
* ZZPRDAT 2026-11-05 (Ingo): nur ANZEIGE des Produktionsdatums im Reiter TRAFAG Daten.
* Geschrieben wird AUFK-ZZPRDAT ausschliesslich durch das BAdI Z_ZZPRDAT_UPDATE (T76K912490).
ci_aufk-zzprdat = i_caufvd-zzprdat.
gs_aufk-zzprdat = i_caufvd-zzprdat.
```

Aktiviert ueber „Inaktive Objekte“ nur mit `ZXCO1U11` (das fremde inaktive `Z_REICHW` blieb
unmarkiert), Meldung „Objekt(e) wurde(n) aktiviert“, kein Fehlerdialog.

**Rueckleseabgleich:** `abap-read ZXCO1U11` nach `nachher_ZXCO1U11.abap`; `diff` gegen
`ZXCO1U11_neu.abap` (Zeilenenden normalisiert) Exitcode 0, gegen `ZXCO1U11_vorher.abap` nur die vier
Zeilen. `REPOSRC ZXCO1U11` nur `R3STATE A` (KOI, 20261106 081101), kein Eintrag in `DWINACTIV`. Die
uebrigen sechs Objekte sind unveraendert (abap-read gegen `vorher_*.abap`, alle Exitcode 0).

## 4. Test

CO03 (nur Anzeige) in eigener Sitzung, Reiter „TRAFAG Daten“:

| Auftrag | `AUFK-ZZPRDAT` | Reiter vorher | Reiter nachher |
| --- | --- | --- | --- |
| `1241847` | `20261111` | leer | `11.11.2026` (`co03_1241847_reiter_nachher.txt`) |
| `1194970` | `00000000` | – | leer (Gegenprobe: kein Scheindatum) |

ST22 06.11.2026, Benutzer KOI: ein Dump um 08:02:12 (`CALL_FUNCTION_NOT_REMOTE`, `SAPLSIFP`); das war
der Leseversuch `RPY_DYNPRO_READ` per RFC (nicht remote-faehig), vor der Aenderung (gesichert 08:11).
Nach der Aenderung kein Dump.

## 5. Fuer spaeter: vor einem Import nach P76

* **Georgs Zwischenstand der sieben Objekte ist in T76 aktiv, aber nicht in P76.** Wird nur
  `T76K912851` importiert, kommt nur `ZXCO1U11` mit. Vor dem Import P76 lesend vergleichen
  (`ZXCO1U11`, `ZXCO1U12`, `ZXCO1U06`, `ZXCO1O01`, `ZXCO1I01`, `ZXCO1TOP`, Bild 0100), besonders:
  * `ZXCO1U12` gehoert zu `EXIT_SAPLCOKO1_002` (`TFDIR` Include 12, `LXCO1U12`: Import `I_AUFNR`,
    Export `E_COCI_AUFK`), Erweiterung PPCO0012 (`MODSAP`). In T76 ist er auskommentiert. Waere in P76
    `MOVE ci_aufk-zzprdat TO e_coci_aufk-zzprdat` aktiv, ginge der jetzt gefuellte Bildwert beim Sichern
    aus CO02 an den Auftrag zurueck. Das waere derselbe Wert, aber ein zweiter Schreibweg neben dem
    BAdI. Vermutung, nicht geprueft.
  * `ZXCO1U06` gehoert zu `EXIT_SAPLCOZV_001` (`LXCO1U06`: Import `HEADER_IMP`, Export `NO_UPDATE`,
    `HEADER_EXP`), Erweiterung PPCO0007 im CMOD-Projekt `ZPP00005` (`MODACT`; `MODATTR STATUS A`).
    Der Quelltext (`REPOSRC` MEEY_GEWA 20260331) ist in T76 **aktiv** und setzt
    `header_exp-zzprdat = header_imp-gltrp`, wenn `ftrmi = sy-datum` und `zzprdat` leer ist. Das ist
    ein zweiter Schreibweg auf `ZZPRDAT` neben dem BAdI, im neuen Auftrag nicht enthalten. Ob SAP
    `HEADER_EXP-ZZPRDAT` in diesem Exit uebernimmt, ob P76 denselben Stand hat und ob der Weg bleiben
    soll, ist offen (Vermutung: Georgs Zwischenstand, nur in T76).
  * Das CMOD-Projekt `ZPP00012` (PPCO0012, Exits `EXIT_SAPLCOKO1_001/002`) ist aktiv
    (`MODATTR STATUS A`, MEEY_GEWA 20250702); die Aenderung wirkt also ohne weitere Aktivierung.
  * `ZXCO1TOP` muss in P76 `TABLES ci_aufk` und `DATA gs_aufk TYPE ci_aufk` enthalten, sonst ist
    `ZXCO1U11` dort nicht syntaxfehlerfrei.
* Das Bildfeld `CI_AUFK-ZZPRDAT` ist im Dump ein `GuiTextField`; ob es in CO02 eingabebereit ist,
  wurde nicht geprueft (Altnotiz `zzprdat-kontext.md` 7: Feld nur anzeigen).
* Georgs Auftrag `T76K911783` (Mandant 090) besteht weiter. Was mit ihm geschieht (loeschen,
  zuruecknehmen, freigeben), entscheiden Ingo und Georg.
