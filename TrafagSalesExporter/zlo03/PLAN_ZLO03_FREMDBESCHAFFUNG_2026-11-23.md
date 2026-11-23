# Plan FIX 29: ZLO03 ohne Stuecklisten fremdbeschaffter Teile (Schalter)

Stand: 2026-11-23 nachmittags (gebaut und getestet in T76, Auftrag `T76K912940` nicht freigegeben, Abschnitt 7); davor 2026-11-23 (Plan, wartet auf Freigabe).

Auftrag Ingo 23.11.: Befund in `Downloads\Dashboard_Materialanteil pro Produktgruppe.xlsx` (Textfeld auf Blatt ZLO03), danach
„lese T76, mache Plan“ und „sonst ein Schalter, dass man beide Optionen hat (wenn es nicht technisch sehr komplex wird)“.
T76/100 nur gelesen (Agent sap-t76, eigene GUI-Sitzungen, kein RFC, kein Passwort); die tragenden Aussagen hat die Hauptsitzung an
den Exportdateien nachgeprueft. Rohdaten im Scratchpad der Sitzung (`zlo03_t76\`), nicht im Repo. P76 nicht gelesen.

Kennzeichnung: [B] in T76 gelesen, [R] im Repo belegt, [V] Vermutung.

## 1. Befund

ZLO03 loest auch die Stuecklisten fremdbeschaffter Teile auf. Deren Rohmaterial erscheint dadurch als Komponente des Geraets,
obwohl Trafag das Drehteil fertig einkauft. Im Dashboard „Materialanteil je Produktgruppe“ zaehlt dann beides: das Drehteil und
sein Rohmaterial (beide Disponent 001).

Beispiel 40731 (1 Stueck):

| Komponente | heute im Dashboard | richtig (wie CK11N) |
| --- | --- | --- |
| C17389 Zentralschraube 878X Delta P, 1 ST x 1.58 | 1.58 CHF | 1.58 CHF |
| H10040 Rundprofil 1.4305 D20, 50 MM = 0.05 M x 14.63 | 0.73 CHF | entfaellt |

Belege:

- MARC 1100 [B]: C17389, C17390, C18990, C22931, C15193 und H10040, H10045 haben `BESKZ = F`, `SOBSL` leer, Disponent 001;
  40731 ist `E`.
- MAST/STPO 1100 [B]: C17389 (Stueckliste 00019807) und C17390 (00020313) haben je eine Position H10040 (50 bzw. 53 MM),
  Verwendung 3, kalkulationsrelevant.
- Kalkulation C17389 [B] (KALNR 102183718, 01.03.2026): eine einzige CKIS-Zeile Typ I (Infosatz 5300015073, Lieferant 72725),
  keine Stueckliste. Kalkulation 40731 [B] (KALNR 102198272): C17389 als Materialposition 1.58 CHF, H10040 kommt nicht vor.
- Die Kalkulation loest nach Beschaffungsart auf [B]: `F` ohne Sonderbeschaffung = Einkaufspreis; `E` und `F` mit Lohnbearbeitung
  (SOBES 3) ueber die Stueckliste. Das sieht man an der Kalkulation der Komponente, nicht an der CKIS des Geraets.
- Generator `Z_VC_ZLO03_TXT` [B]: `CS_BOM_EXPL_MAT_V2` mit `capid PP01`, `mehrs X`, Werk 1100, ohne Abbruch nach Beschaffungsart.
- `ZPOWERBI_VC_TXT` [B]: je Zeile `STUFE`, Elternmaterial `KOM_MSTAE` und ID der Elternzeile `MAT_MSTAE`. Bei 40731:
  C17389 ID 953355 Stufe 1, H10040 ID 953406 Stufe 2 mit `MAT_MSTAE = 953355`.
- `Z_ZLO03_TURBO2` in T76 ist bis auf ein BOM-Zeichen identisch mit `zlo03/Z_ZLO03_TURBO2_FIX28_2026-11-12.abap` [B].

Umfang [B]: In Werk 1100 haben 1'866 Materialien mit `BESKZ F` eine Stueckliste (Verwendung 1 oder 3), davon 1'212 ohne
Loeschvormerkung (1'169 ohne Sonderbeschaffung, 43 Lohnbearbeitung 30). Viele davon sind Selbst-Stuecklisten, die FIX 28 schon
nicht zaehlt; die echte Wirkung ist kleiner [V]. Bei 40731 haengen 13 echte Kindzeilen unter einem F-Teil (u. a. H10040, H10221,
H10222, H10225, H10093, H20774, H20908, K80175 unter der Lohnbearbeitung C17416).

## 2. Loesung: Schalter in `Z_ZLO03_TURBO2` (Variante A, empfohlen)

Die Tabelle traegt die Elternkette (`MAT_MSTAE`), deshalb kann ZLO03 beim Lauf filtern. Generator, Tabelle und Nachtjob bleiben
unveraendert. Aufwand: **einfach bis mittel**, ein Programm und eine Zeile im Gateway.

1. Neuer Parameter `p_nofrm` (Ankreuzfeld „Stuecklisten fremdbeschaffter Teile nicht aufloesen“), **ab Werk leer**: fuer die
   Benutzer von ZLO03 bleibt alles wie heute (Phase-Out, Exklusivitaet).
2. `mat_mstae` in die beiden SELECTs auf `ZPOWERBI_VC_TXT` (Zeilen 743 und 751) und in `ty_powerbi` aufnehmen.
3. Neue FORM direkt nach dem Lesen: `BESKZ`/`SOBSL` aus MARC 1100 fuer alle Baugruppen-Zeilen lesen; je Zeile die Kette
   `MAT_MSTAE` hochlaufen und die Zeile entfernen, wenn ein Vorfahr fremdbeschafft ist (Regel nach E1). Das F-Teil selbst bleibt.
   Falle: `ID` ist NUMC 8, `MAT_MSTAE` CHAR 40 (Datenelement MATNR); Vergleich nach Normalisierung.
4. Nur im Top-Down (E2). Der Filter wirkt auf alles, was danach aus `lt_powerbi` entsteht (Mengen, Basismengen, Werte, App-Export).
5. Gateway `ZloTopDownSet` (`ZCL_ZPOWERBI_EINKAUF_DPC_EXT`): im `SUBMIT z_zlo03_turbo2` eine Zeile `P_NOFRM = X`. Kein
   Modellwechsel, kein Leeren der Gateway-Caches noetig [V].
6. Jede Aenderung mit `*** FIX 29:`-Kommentar, klassische ABAP-Syntax (`zlo03/CLAUDE.md`, Harte Regeln).

**Variante B, nicht empfohlen:** im Generator die Auflösung abbrechen oder ein Kennzeichen „unter Fremdteil“ schreiben. Das
braucht eine Tabellenerweiterung, Aenderungen an `Z_VC_ZLO03_TXT` und `Z_VC_ZLO03`, einen Neuaufbau (Nachtjob rund 4 h 43 in
P76 [R]) und trifft ohne Kennzeichen auch `Z_LO03_MD04_BATCH`, Bottom-Up, Exklusivitaet und die direkten Leser der Tabelle.

**Nicht betroffen von Variante A** [R]: Die Dashboard-Lader, die `ZPOWERBI_VC_TXT` direkt ueber andere Sets lesen
(`MaterialUsageDataRefreshService` Stuecklistenanalyse, `PurchasingComponentDispoLoader`), laufen am Schalter vorbei. Fuer den
Materialanteil reicht der Schalter; ob die Stuecklistenanalyse dieselbe Regel braucht, ist eine eigene Frage.

## 3. Entscheide Ingo

| Nr. | Frage | Vorschlag |
| --- | --- | --- |
| E1 | Faellt auch das Material unter **Lohnbearbeitung** (F/30, z. B. K80175 unter C17416) mit dem Schalter weg? Der Preis von C17416 enthaelt es schon (2.625 CHF je ST = Lohn + Beistellung), im Dashboard ist es heute also auch doppelt. | Ja, fuer den Schalter (Wert); ohne Schalter bleibt es sichtbar |
| E2 | Nur Top-Down und Dashboard, oder auch Bottom-Up? | Nur Top-Down (Bottom-Up braeuchte einen zusaetzlichen Vorfahren-Lesezugriff, mittel) |
| E3 | Gilt die Regel auch fuer die 55 Materialien mit `BESKZ X` (beides)? | Nein, nur `F` |
| E4 | Kurze P76-Leseprobe: ist `MAT_MSTAE` dort gefuellt (eine VKNR, ID gegen `MAT_MSTAE`)? Ohne sie wirkt der Schalter in P76 nicht. | Ja, vor dem Transport, nur lesend |
| E5 | Zusammenspiel mit der geplanten Kopie `Z_ZLO03_PCBA` (`zlo03/PLAN_ZLO03_PCBA_2026-11-18.md`), die fremdbeschaffte PCBA gerade aufloesen will | In der Kopie Schalter gesperrt, solange `P_PCBA = X` |

## 4. Test in T76

1. Ohne Schalter: Lauf 40731 Top-Down, Ausgabe gleich wie vor FIX 29 (Zeilen, Mengen, Werte, Exklusiv).
2. Mit Schalter: H10040, H10221, H10222, H10225, H10093, H20774, H20908 fehlen, C17389 und die uebrigen F-Teile bleiben; nach E1 auch K80175.
3. Summe Materialwert 40731 mit Schalter gegen CK11N (Kalkulation 102198272), Abweichungen erklaeren.
4. Zweites Geraet aus einer anderen Produktgruppe, dazu ein Lauf mit `P_APP` (wie aus dem Gateway).
5. Syntaxpruefung, Aktivierung in T76; Transport freigeben erst nach Ingos Ja, Import nach P76 macht Ingo.

## 5. Wirkung und Kommunikation

- Der Materialanteil je Produktgruppe sinkt um den doppelt gezaehlten Rohmaterialwert. Das sollte vorher jemand aus dem Einkauf
  (Armin) wissen, sonst sieht es wie ein Fehler aus.
- Das Dashboard holt die Werte beim naechsten Einkauf-Lauf (seit 23.11. um Mitternacht).
- Die Benutzer von ZLO03 sehen ohne Haken keinen Unterschied.

## 6. Was offen ist

- Welches Programm `ZPOWERBI_VC_TXT` in P76 fuellt (in T76 schreibt `Z_VC_ZLO03_TXT` nach `ZAT_VC_TXT`) [B/V].
- Die Gateway-Methode in T76 ist noch nicht gelesen und verglichen; das gehoert in den Bauschritt.

## 7. Bau und Test in T76 (2026-11-23)

Auftrag Ingo „leg los, prod ist auch offen fuer dich“; Vorschlaege E1 bis E5 uebernommen. Agent sap-t76, eigene Sitzungen, kein RFC,
kein Passwort. Die Hauptsitzung hat Quelltext, Gateway-Zeile und Testlisten an den Exportdateien nachgeprueft.

- **E4, P76 nur gelesen [B]:** `ZPOWERBI_VC_TXT` 40731 hat 122 Zeilen; Stufe 01 hat `MAT_MSTAE = 00000000`, alle 81 tieferen
  Zeilen zeigen auf eine Zeile der Stufe darueber (H10040 ID 01042711 -> C17389 ID 01042655). Der Schalter wirkt also auch in P76.
  *Zwischenfall:* Ein ad hoc angepasstes SE16N-Exportskript loeste in P76 einen Kurzdump `RAISE_EXCEPTION` in `SAPLSE16N` aus
  (23.11.2026 09:27:14, Benutzer KOI); nichts geaendert, danach mit SE16 gelesen.
- **Auftrag `T76K912940`** (Workbench, Ziel P76, nicht freigegeben): `LIMU REPS Z_ZLO03_TURBO2` und
  `LIMU METH ZCL_ZPOWERBI_EINKAUF_DPC_EXT GET_ENTITYSET`. Programm byte-gleich mit `Z_ZLO03_TURBO2_FIX29_2026-11-23.abap`
  (Syntax nur die bekannte POSIX-Warnung), DPC: genau eine neue Zeile `P_NOFRM = X` nach `P_APP`. Beide aktiv. Gateway-Caches nicht
  noetig (Modell unveraendert).
- *Erledigt 2026-11-23: Selektionstext „Fremdteile nicht auflösen“ (Entscheid Ingo), gesichert, aktiviert, `LIMU REPT Z_ZLO03_TURBO2` als dritter Eintrag in `T76K912940` (Status weiter D). Ein Ja/Nein-Fenster nach dem Sichern („ABAP - Pflege der Textelemente“, Text per Scripting nicht lesbar) wurde mit Nein beantwortet; gesichert war schon.* Vorher: **Offen: Selektionstext** fuer `P_NOFRM` (Feld 30 Zeichen; „Fremdbeschaffte Teile nicht auflösen“ hat 36). Auf dem Bild steht
  bis dahin „P_NOFRM“.
- **Test 40731 [B]:** ohne Haken 88 = 88 App-Zeilen und Excel 91 = 91 Zeilen, 0 Abweichungen. Mit Haken 76 Zeilen, es fehlen genau
  E00670, H10040, H10093, H10221, H10222, H10225, H20774, H20908, K50711, K80175, XXXXXX, ZC18907; keine verbleibende Menge
  geaendert. Dashboard-Lesart (Disponent 001 bis 006): **106.26 -> 93.04 CHF je Stueck**, Differenz = Summe der entfallenen Zeilen.
  Gateway-Weg (DPC-Aufruf) liefert danach 76 Zeilen ohne H10040.
- **Gegen CK11N 40731 [B]:** Rest-Differenz +2.44 CHF je Stueck bei den direkten Positionen, erklaert durch Mehrfachverwendung unter
  Eigenfertigungsteilen (C18298, C15316), einen Disponenten ausserhalb 001 bis 006 (H90077) und Rundung. Neu: ZLO03 rechnet den
  Komponentenausschuss nicht (H05140 5 %, C15399 4 %).
- **Zweites Geraet 31318 [B]:** 48 -> 36 Zeilen, 74.10 -> 59.78 CHF. **Fachlicher Befund:** Unter B65814 (F, ohne Sonderbeschaffung,
  Einkaufspreis 4.62 CHF) fallen Stuecklistenteile fuer 11.64 CHF weg (B65813 allein 8.80). Die Kalkulation von B65814 hat nur den
  Infosatz (462.48 CHF/100 ST), verhaelt sich also wie der Schalter. Vermutung: Beistellung ohne Kennzeichen Lohnbearbeitung oder
  veraltete Stueckliste (PCBA-Thema). Vor dem Import mit dem Einkauf klaeren.
- **Hinweis:** Excel mit Haken zeigt in der Spalte „Weitere VKNR“ teils eine andere Auswahl (unstabile Sortierung mit Schnitt bei 100,
  Vermutung); betrifft nur den Dialoglauf, nicht das Dashboard.
- **Uebrig in T76:** Testreport geloescht (Quelltext `zlo03/Z_KOI_ZLO_FIX29_TEST.abap`), vier erledigte Jobs `Z_KOI_ZLO_FIX29`.

**Naechste Schritte:** Selektionstext waehlen (dann REPT in den Auftrag), Befund B65814 bewerten und Armin informieren, `T76K912940`
freigeben und nach P76 importieren (Ingo). Das Dashboard zeigt die neuen Werte nach dem naechsten Einkauf-Lauf (Mitternacht).
