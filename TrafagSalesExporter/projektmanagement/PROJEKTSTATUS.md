# Projektstatus Ingo Kohler

Stand: 2026-09-11

Diese Datei ist die **fuehrende Aufgabenliste** fuer das persoenliche
Projektmanagement. Sie ersetzt `kontext.txt` (2013 Zeilen ChatGPT-Protokoll vom
05.05. bis 10.08.2026). Alle weiteren Verlaeufe und Erledigungen werden ab jetzt
hier eingetragen, nicht mehr in einem Chatverlauf.

`kontext.txt` bleibt als Rohquelle liegen, ist aber **abgeloest** und wird nicht
mehr gepflegt.

Abgrenzung: Das Finance Dashboard hat ein eigenes, feineres Issue-Log unter
`docs/Issue_Log_Konsolidiert_2026-08-12.tsv`. Diese Datei hier fuehrt die
uebergeordneten Arbeitspakete; sie verweist auf das Issue-Log, dupliziert es aber
nicht.

---

## 1. Offene Aufgaben

| ID | Thema | Verantwortlich | Prioritaet | Status | Naechster Schritt | Letztes Update |
|---|---|---|---|---|---|---|
| PM-01 | ZLO03: fehlende Materialien und falsche Mengen | Ingo | Hoch | Umsetzung liegt vor, Transport offen | Diagnoselauf `p_diag` und Regressionstest, danach Transport nach B76 | 2026-08-14 |
| PM-02 | ZC12: Fehler bei Nullmengen | Ingo | Mittel | **Vorfrage am 2026-09-04 beantwortet: `ZC12` zeigt auf `ZM_ABGLEICH_KTSCH`, der Quelltext nennt sich selbst falsch `Z_ABGLEICH_KTSCH`. `fmt_quan` gehoert zum eigenen Neuaufbau vom 28.-30.04.2026, also Testluecke statt Regression. Die Trace-Infrastruktur ist Ingos Einbau vom 27.05.2026 und von Anfang an auskommentiert** | Die vorbereitete Fassung `saptasks/zc12/ZM_ABGLEICH_KTSCH_nachher.abap` einspielen — sie ist **noch nicht im System** —, danach den Nullmengenfall tracen und den Testfall um Vorgabewert null erweitern | 2026-09-07 |
| PM-03 | ZZPRDAT: Produktionsdatum am Fertigungsauftrag | Ingo | Hoch | **Der am 2026-09-07 gefundene Konstruktionsfehler ist am selben Tag behoben: `BEFORE_UPDATE` prueft jetzt, ob der Auftrag vor diesem Sichern schon freigegeben war, und laesst Altauftraege in Ruhe. Alle sieben Wege plus Altauftragsfall und Write-once sind am 2026-09-07 auf dem korrigierten Stand nachgemessen, Klasse liegt in der Aufgabe `T76K912491`. Auftrag weiter nicht freigegeben** | Anschreiben absenden: Empfaengeradressen eintragen und den Text aus `docs/ZZPRDAT_Mail_Abnahme_2026-09-04.html` in den Outlook-Entwurf kopieren; danach Abnahme durch Lucas Castro und Florian Waechter, Marco prueft Etikett und Typenschild und dabei neu auch den Zeitpunkt des Drucks gegenueber V2. Erst zuletzt den Auftrag freigeben | 2026-09-07 |
| PM-04 | Einkaufsdashboard: Spend mit Drilldown | Ingo | Mittel | Weitgehend erledigt; die zwei SAP-Restpunkte stehen unveraendert offen. Seit 2026-09-03 ist das Dashboard durch einen filterabhaengigen Snapshot beschleunigt, seit 2026-09-10 sind verstaendlichere Kennzahlen und die SAP-Quellenhilfe produktiv | Zwei SAP-Nacharbeiten anstossen; zusaetzlich die **nicht ausgelieferten drei Statusbeschriftungen** nachziehen und die neuen Texte produktiv sichtpruefen | 2026-09-11 |
| PM-05 | Finance: alle Daten in einem zentralen Excel | Ingo | Mittel | Produktiv, laufende Detailarbeit. Die fuenf Ausgleichsfelder sind seit 2026-09-10 08:53 produktiv, `Finance_All` am selben Tag neu erzeugt | Ueber das Finance-Issue-Log weiterfuehren; CH/AT-Option und die Bedeutung von `date paid` mit Andreas entscheiden | 2026-09-11 |
| PM-07 | HR: automatische Auswertung der REXX-Files | Ingo | Mittel | Wartet auf externe Firma | Fertigstellung des automatischen Exporters abwarten, danach Anbindung/Auswertung planen | 2026-08-19 |
| PM-08 | Railway: Auswertung fuer Rohail Munir (DE), Termin 2026-09-08 | Ingo | **Hoch, Termin ueberschritten** | **Blocker 1 ist am 2026-09-09 geloest**, der deutsche Verkaufsbestand traegt jetzt fachliche Kundennummern und Namen. Blocker 2 besteht nur noch fuer die anderen Standorte fort | Neues Zieldatum mit Rohail vereinbaren; 30 Gutschriftenzeilen und drei Doppelnummern klaeren; Patrik die 171 uebrigen Vorschlaege pruefen lassen | 2026-09-11 |

---

## 2. Details je offener Aufgabe

### PM-01 ZLO03: fehlende Materialien und falsche Mengen

Aufgenommen am 2026-07-27. Im Chatprotokoll stand der Punkt bis zuletzt als
„Klaerung offen". Das ist ueberholt, im Repository liegt bereits eine
vollstaendige Diagnose und ein korrigiertes Programm.

Zwei Ursachen sind belegt und behoben:

1. `ZPOWERBI_VC_TXT-MENGE` ist als `SOBJID` typisiert, also CHAR 40 ohne
   Konvertierungsexit. Die alte Zuweisung interpretierte den String nach der
   Dezimaldarstellung des jeweiligen Benutzers aus SU3. Derselbe Report lieferte
   je nach Benutzer Mengen um den Faktor 1000 daneben, ohne Fehlermeldung.
   Behoben mit `FORM parse_menge_str` als FIX 10.
2. `KOMPNR` und `MATNR` haben in derselben Tabelle unterschiedliche Domaenen.
   `MATNR` traegt den Konvertierungsexit MATN1 und damit fuehrende Nullen,
   `KOMPNR` nicht. Joins trafen bei rein numerischen Komponentennummern ins
   Leere. Aufgefallen ist es nie, weil die von Hand geprueften Komponenten
   Buchstabenpraefixe haben. Behoben mit FIX 11.

**Der eigentliche Blocker** steht in `zlo03/BEFUND_SYSTEMABGLEICH_2026-08-03.md`:
Die Transaktion `ZLO03` startet nicht `ZM_LZCODE20_OPT`, sondern
`Z_ZLO03_TURBO2`. Das laufende Programm enthaelt nur die Fixes 1, 2, 4 und 5.
Die Fixes 10 bis 18 sind also geschrieben, aber **nicht produktiv wirksam**.

Kommunikationspflicht vor der Auslieferung: Die Spalte `Exklusiv` hat kuenftig
drei statt zwei Werte. Neu ist `?` fuer nicht entscheidbare Faelle. Ann-Katrin
Michel muss das wissen, bevor sie die naechste Auswertung interpretiert, denn
ein falsch gesetztes `X` fuehrt zur Kuendigung eines noch benoetigten
Mengenkontrakts. Falsch-negativ ist harmlos, falsch-positiv nicht.

Bewusst nicht behoben, weil fachlich zu klaeren: die funktionslose Spalte
Elternmaterial, die fehlende Rekursion bei der Exklusivitaet, sowie die
Differenz 12 gegen 21 VKNR im Bottom-Up. Fuer den Vergleich CS15 42 gegen ZLO03
21 fehlen Sandro Moltisantis CS15-Einstellungen, also Werk, Stichtag,
Stuecklistenverwendung und ein- oder mehrstufig.

Quellen: `zlo03/CLAUDE.md`, `zlo03/BEFUND_SYSTEMABGLEICH_2026-08-03.md`,
`zlo03/ZM_LZCODE20_OPT.abap`.

### PM-02 ZC12: Fehler bei Nullmengen

Aufgenommen am 2026-07-27, Codeanalyse nachgetragen am 2026-08-14, **am 2026-09-04 in
T76/100 rein lesend am System geprueft**.

**Die Vorfrage ist beantwortet, und die Antwort korrigiert einen Namen.** `ZC12` ist eine
Reporttransaktion und zeigt auf das Programm **`ZM_ABGLEICH_KTSCH`**, Paket `ZPP1`,
Transaktionstext „Abgleich Textvorlagen", Selektionsbild 1000. Das bisher in dieser Datei
gefuehrte **`Z_ABGLEICH_KTSCH` existiert in T76 nicht**; SE38 meldet „Das Programm
Z_ABGLEICH_KTSCH ist nicht vorhanden". Die Codeanalyse unten bleibt gueltig, sie gehoert nur
zu `ZM_ABGLEICH_KTSCH`.

**Woher der falsche Name kommt, ist ebenfalls geklaert, und er war nicht erfunden.** Der
Quelltext selbst traegt seit dem Neuaufbau den Kopf `*& Report  Z_ABGLEICH_KTSCH` und die
Anweisung `report z_abgleich_ktsch.`, waehrend das Objekt `ZM_ABGLEICH_KTSCH` heisst. Wer den
Quelltext liest, liest den falschen Namen. Das ist derselbe Fall wie beim CH/AT-Exportreport,
wo `REPORT`-Kopf und Dateiname `Z_TRAFAG_SCHWEIZ_EXPORT` sagen und das System
`Z_TRAFAG_DACH_EXPORT` kennt. Beim Suchen im System zaehlt ausschliesslich der Objektname.

Eigenschaften des Programms, am 2026-09-04 aus SE38 gelesen: angelegt am 03.03.2003 von
`I001067`, letzte Aenderung am **27.05.2026 durch `KOI`**, Status aktiv und „Produktives
Kundenprogramm", Typ ausfuehrbares Programm.

**Die Versionsverwaltung raeumt mit der Arbeitshypothese auf.** Es gibt vier Versionen und
keine einzige vom 30.04.2026:

| Version | Datum | Benutzer | Auftrag | Kurztext |
|---|---|---|---|---|
| aktiv | 27.05.2026 06:21 | `KOI` | — | identisch mit Version 4, per Vergleich belegt |
| 4 | 27.05.2026 06:48 | `KOI` | `T76K912063` | `ZV12` |
| 3 | 18.05.2026 13:18 | `LAA` | `T76K911927` | `Repair_zc12` |
| 2 | 20.04.2026 13:06 | `LAA` | `T76K911893` | `DENY_ZM_ABGLEICH_KTSCH` |
| 1 | 20.04.2026 12:15 | `LAA` | — | — |

**Version 3 ist kein Reparaturpflaster, sondern der komplette Neuaufbau.** Der Vergleich von
Version 3 gegen Version 2 zeigt eine vollstaendige Ersetzung: Version 2 bestand im Kern aus
dem Vermerk „Deaktiviert wegen Fehlfunktion. 20.04.2026, Adil Lahrach", Version 3 bringt den
neuen Report mit ALV-Grid, eigenen Typen und der ganzen heutigen Struktur. Die Kopfhistorie
**innerhalb** von Version 3 datiert diese Arbeit selbst:

```
*&   20.04.2026  A.Lahrach   Deaktiviert wegen Fehlfunktion (alt)
*&   28.04.2026  I.Kohler    Neuaufbau (PLNAL-Fix, ALV)
*&   29.04.2026  I.Kohler    Umbau auf CL_GUI_ALV_GRID + Splitter
*&   30.04.2026  I.Kohler    Variante B: Editor zeigt KTSCH-Vorlage (CA10) ...
```

**Damit ist die urspruengliche Arbeitshypothese bestaetigt, nicht widerlegt.** Die
Trailing-Null-Logik gehoert zum Neuaufbau vom 28. bis 30.04.2026 und war bei der Freigabe am
18.05.2026 bereits enthalten. Adil hat sie transportiert, gebaut hat sie Ingo. Es ist keine
Regression gegen einen funktionierenden neuen Stand, sondern eine Testluecke: der Nullmengenfall
war im Testfall nicht enthalten. Dass die Datenbank keine Version vom 30.04. kennt, liegt nur
daran, dass Versionen erst beim Transport entstehen; die Aprilarbeit steckt in Version 3.

**Falle bei der Versionssuche, am 2026-09-04 aufgefallen:** Die Suche in der
Versionsverwaltung markiert die Versionen, in denen der Text in **geaenderten** Zeilen
vorkommt, nicht jede Version, die ihn enthaelt. Belegt an `report z_abgleich_ktsch`: die Suche
meldet nur Version 3, obwohl der Zeilenvergleich die Zeilen 1 bis 116 von Version 4 als
unveraendert gegenueber Version 3 ausweist, die Anweisung dort also ebenfalls steht. Wer die
Trefferliste als „kommt nur dort vor" liest, zieht falsche Schluesse.

**Der Blocker „das Tracing ist tot" hat einen anderen Ursprung als gedacht.** `p_debug`
erscheint erst in Version 4, und der Zeilenvergleich von Version 4 gegen
Version 3 zeigt die Zeile als **eingefuegt und bereits auskommentiert**:
`"p_debug as checkbox default ' '.` Mit derselben Aenderung kamen `gv_trace_file`,
`gv_trace_open`, `lv_trace_line` sowie die Aufrufe `perform trace_open`, `perform trace` und
`perform trace_close` hinzu. Die ganze Trace-Infrastruktur ist also am **27.05.2026 von
`KOI`** eingebaut und nie scharf geschaltet worden. Sie zu reaktivieren aendert nichts an
Adils Stand und braucht keine Abstimmung mit ihm; sie einzuschalten ist eine Aenderung an
Ingos eigenem Code.

Unter der Annahme, dass es dasselbe Programm ist:

**Fehlerbild.** Kein Dump. `fmt_quan` entfernt die Trailing-Nullen, aus `0.000`
wird `0.` und daraus `0`. Diese nackte Null geht in das BDC-Feld `PLPOD-VGWnn`.
`CA02` quittiert das je nach Vorgabewertschluessel mit einer E-Meldung aus der
Vorgabewert- und Einheitenpruefung. Die Meldung landet in `bdc_transaction` in
`lt_msg`, `gv_err_cnt` steigt, die Zeile erscheint im Fehlerlog und es folgt ein
`continue`. Es ist also ein stilles Ueberspringen mit Eintrag im
Fehlerprotokoll, kein Abbruch.

**Blocker fuer die Verifikation: das Tracing ist tot.** In `trace_open` steht
nach dem Kommentar „Nur im Debug-Modus tracen" ein hartes `return.`, und
`p_debug` ist im Selektionsbild auskommentiert. Es existieren deshalb ueberhaupt
keine Trace-Logs zum Nachschauen. Fuer die Diagnose muss zuerst `p_debug`
reaktiviert werden.

**Tabellen und Felder.**

| Zweck | Quelle |
| --- | --- |
| IST-Menge | `PLPO-VGW01` bis `VGW04`, Einheiten `VGE01` bis `VGE04`, gelesen in `select_data` nach `gt_plpo` und `ty_row` |
| SOLL-Menge | `popup_get_zeiten` ueber `POPUP_GET_VALUES`; im Debug-Pfad `run_debug` stattdessen aus `zzpp_vc_vorgabe`, Schluessel `KTSCH`, Felder `VGW01` bis `VGW04` und `VGE01` bis `VGE04` |
| Schreibpfad | BDC auf `SAPLCPDO/1200`, Subscreen `SAPLCPDO/1211` `DEFAULTVAL`, Felder `PLPOD-VGW01` bis `VGW04` |

**Neuer Fehler oder Regression.** Aus dem Code nicht entscheidbar, weil
`fmt_quan` keine eigene Datierung traegt und die Kopfhistorie am 2026-04-30
endet. Konkret pruefbar ueber `SE38` und die Versionsverwaltung sowie ueber die
Transportauftraege zwischen dem 2026-04-30 und dem 2026-05-18.

Arbeitshypothese: keine Regression, sondern eine Testluecke. Die
Trailing-Null-Logik stammt aus dem Neuaufbau von Ende April und war bei Adils
Freigabe am 2026-05-18 bereits enthalten. Wahrscheinlicher ist, dass sein
Testfall keine Zeile mit Vorgabewert null enthielt. Zeigt die Versionsverwaltung,
dass `fmt_quan` seit dem 2026-04-30 unveraendert ist, gilt das als bestaetigt.
Dann lautet die Antwort: der Fehler bestand von Anfang an, und die
Testabdeckung ist um den Nullmengenfall zu erweitern.

> **Am 2026-09-04 in der Versionsverwaltung bestaetigt.** Version 3 vom 18.05.2026 ist der
> Neuaufbau selbst, ihre eigene Kopfhistorie datiert ihn auf den 28. bis 30.04.2026. Die
> Antwort lautet also genau so: der Fehler bestand von Anfang an, und die Testabdeckung ist
> um den Nullmengenfall zu erweitern. Einzelheiten oben im Abschnitt zur Vorfrage.

Randnotiz zur Kopfhistorie: Adil erscheint dort nur mit dem Eintrag
`20.04.2026 A.Lahrach Deaktiviert wegen Fehlfunktion (alt)`.

**Stand 2026-09-07: die Aenderung ist vorbereitet, aber nicht eingespielt.** Ingo hat am
2026-09-07 entschieden „ja mach scharf". Der Quelltext liegt seither erstmals im Repository:

| Datei | Rolle |
|---|---|
| `saptasks/zc12/ZM_ABGLEICH_KTSCH_vorher.abap` | Ist-Stand aus T76, 1'941 Zeilen, Rueckfallpunkt |
| `saptasks/zc12/ZM_ABGLEICH_KTSCH_nachher.abap` | dieselbe Fassung mit zwei geaenderten Zeilen |
| `saptasks/zc12/Z_KOI_SRC_DUMP.abap` | Wegwerfhilfe, mit der der Quelltext geholt wurde; das Objekt ist geloescht |

Die zwei Aenderungen: `p_debug` wird entkommentiert, wobei der Punkt der `PARAMETERS`-Kette
zu einem Komma werden muss, sonst waere die Zeile eine eigene, syntaktisch falsche
Anweisung. Und in `trace_open` haengt das Tracing nicht mehr an einem nackten `return.`,
sondern an `IF p_debug <> 'X'.` — was der Kommentar darueber ohnehin schon behauptete.

**Warum es nicht eingespielt ist:** Der Aufruf, der den Quelltext zurueckschreibt, wurde vom
Berechtigungsfilter der Sitzung abgelehnt. Er muss von Hand abgesetzt werden, aus der
Repository-Wurzel:

```
cscript //nologo .tmp_sap_probe/SapGuiSetReportSource.vbs ZM_ABGLEICH_KTSCH saptasks/zc12/ZM_ABGLEICH_KTSCH_nachher.abap
```

Danach fragt SAP nach einem Transportauftrag, weil das Programm in `ZPP1` liegt. Vor dem
naechsten Schritt gehoert geprueft, ob der aktive Quelltext wirklich die beiden Zeilen
traegt; solange das nicht belegt ist, ist `p_debug` **nicht** scharf.

Nicht angefasst wurde `run_debug`. Die Form existiert vollstaendig, wird aber von niemandem
aufgerufen und ist damit toter Code. Sie an den Schalter zu haengen waere eine eigene
Entscheidung und gehoerte nicht zu „mach scharf".

Der Trace schreibt uebrigens mit `OPEN DATASET` nach
`/tmp/z_abgleich_ktsch_<user>_<zeitstempel>.log`, also auf den **Applikationsserver** und
nicht auf den Arbeitsplatz. Zum Lesen braucht es AL11 oder einen kleinen Lesereport.

### PM-03 ZZPRDAT: Produktionsdatum am Fertigungsauftrag

**Ergebnis vom 2026-09-03: in T76 geloest.** Das Produktionsdatum wird bei der Freigabe
gesetzt und bleibt bei einer spaeteren Terminverschiebung stehen, ohne dass der Reiter
„Trafag Daten" besucht wird. Damals auf vier Wegen nachgewiesen: CO01 (anlegen und freigeben in
einem Vorgang), CO02 (bestehenden Auftrag freigeben), COHV (Sammelfreigabe) und CO40
(Planauftrag umsetzen). Auftraege `1241812` bis `1241816`.

Drei unabhaengige Ursachen waren zu beheben, keine davon am Quelltext erkennbar: Der
SAP-Standard ueberschrieb den als V1 geschriebenen Wert aus seinem eigenen Puffer, weshalb
der Baustein auf V2 („Start verzoegert") umgestellt wurde. Literale an einen
Verbuchungsbaustein reissen die ganze Verbuchung mit, weshalb nur getypte Variablen
uebergeben werden. Und beim Anlegen mit sofortiger Freigabe traegt der Auftrag noch eine
temporaere Nummer, weshalb zusaetzlich `BEFORE_UPDATE` implementiert wurde.

Loesungsdokument fuer den Fachbereich: `docs/ZZPRDAT_Loesung_2026-09-03.docx`.
Vollstaendiger Verlauf und Messwerte: `saptasks/zzprdat-kontext.md`.

**Stand 2026-09-04: die Objekte sind transportfaehig gebaut, aber nichts ist nach P76
transportiert.** Sie liegen im Paket `ZPP1` und im Workbench-Auftrag **`T76K912490`**
(„ZZPRDAT: Produktionsdatum bei Auftragsfreigabe"), der bewusst **nicht freigegeben** ist.
Ingo hat vorgeschlagen, vor der Abnahme zu paketieren, damit der Fachbereich genau die
Objekte abnimmt, die spaeter ausgeliefert werden. Im Auftrag stecken `FUGR ZPP_ZZPRDAT`
(mit `Z_ZZPRDAT_SET`), `SXCI Z_ZZPRDAT_UPDATE`, `CLAS ZCL_IM__ZZPRDAT_UPDATE`,
`ENHO Z_ZZPRDAT` und `PROG Z_ZZPRDAT_CHECK`.

Der Nachtest auf diesem Stand lief am 2026-09-04 mit **neu angelegten** Auftraegen `1241817`
bis `1241823`, weil der Baustein nur schreibt, wo `ZZPRDAT` initial ist; die alten
Testauftraege haetten unabhaengig vom Code „gesetzt" gemeldet. Alle Wege bestanden,
Write-once ebenfalls. Die alten `$TMP`-Testobjekte sind geloescht, damit nicht zwei
Implementierungen desselben BAdI registrieren.

**Ebenfalls am 2026-09-04 gemessen und damit erledigt:** die Auftragsart `PP22` (`1241821`),
die Umsetzung ueber MD04 (`1241822`) und die Sammelumsetzung ueber CO41 (`1241823`). CO41
gibt dabei nicht frei — der Auftrag entsteht ohne Produktionsdatum, was richtig ist, und das
Datum kommt bei der spaeteren Freigabe. Die Disposition muss dafuer nicht mehr um Testfaelle
gebeten werden.

Abschalten geht weiter mit `Strg+F4` in SE19, auch nach einem Import. **Technisch ist nichts
mehr offen.** Es fehlen die fachliche Abnahme und Marcos Pruefung von Etikett und
Typenschild; danach getrennt die Freigabe des Auftrags und das Nachfuellen der Altbestaende.

**Befund vom 2026-09-07: die Loesung fuellt Altauftraege ungewollt nach.** Eine
Gegenpruefung durch ein zweites Modell hat gezeigt, dass `BEFORE_UPDATE` bei **jedem**
Sichern laeuft und nicht nur bei der Freigabe. Der Verbuchungsbaustein prueft anschliessend
nur, ob `AFKO-FTRMI` gefuellt und `AUFK-ZZPRDAT` leer ist. Beides trifft auf jeden laengst
freigegebenen Altauftrag zu, dessen Feld noch leer ist: Er bekommt beim naechsten
beliebigen Sichern den **heute** gueltigen Eckendtermin eingetragen, nicht den vom Tag
seiner Freigabe. Das widerspricht der Entscheidung, das Nachfuellen der Altbestaende zu
einem eigenen Schritt zu machen, und liefert bei verschobenen Terminen einen falschen
Wert. Aufgefallen ist es nicht, weil alle neun Messungen vom 2026-09-04 mit **neu**
angelegten Auftraegen liefen, bei denen Freigabe und erstes Sichern zusammenfallen.

**Am selben Tag behoben und gemessen.** `BEFORE_UPDATE` liest jetzt vor dem Registrieren
`AFKO-FTRMI`; steht dort schon ein Wert, war der Auftrag vorher freigegeben und wird
uebersprungen. Drei Messungen ueber RFC gegen `AUFK`: der kuenstlich geleerte Altauftrag
`1241819` bleibt beim Sichern leer, der neue Auftrag `1241824` bekommt weiterhin sein
Datum, und beim Verschieben des Termins bleibt es stehen. Die Klasse steckt laut `E071`
in der Aufgabe `T76K912491`, wandert also mit dem Auftrag mit. Einzelheiten, die beiden
Nebenbefunde zu V2 und zum Nachweisreport und die noch offene Wiederholung der uebrigen
sechs Wege stehen in `saptasks/ZZPRDAT_TRANSPORTPLAN.md` Abschnitt 6a.

**Das Anschreiben zur Abnahme ist am 2026-09-04 geschrieben, aber noch nicht versandt.** Es
nennt, was vorher falsch war, die drei Ursachen, die sieben gemessenen Wege, den
CO41-Sonderfall, die zwei erbetenen Rueckmeldungen und den Hinweis, dass in P76 nichts
geaendert ist. Der Outlook-Entwurf traegt Betreff und das angehaengte Loesungsdokument,
**aber weder Empfaenger noch Text**: fuer Lucas Castro, Florian Waechter, Marco Di Menco und
Fabio Palma sind in `docs/ANSPRECHPARTNER.md` keine Adressen hinterlegt, und Outlook
verwirft an diesem Arbeitsplatz jede Zuweisung an den Nachrichtentext stillschweigend
(gemessen und dokumentiert in `docs/router/plattform.md`). Der Text liegt deshalb als
`docs/ZZPRDAT_Mail_Abnahme_2026-09-04.html` daneben und muss einmal hineinkopiert werden.
Vor dem Versand sind also drei Handgriffe noetig: Adressen eintragen, Text einfuegen,
absenden.

Aufgenommen am 2026-07-27, urspruenglich als „BAdI-Kennzeichenfehler". Der Punkt
ist am 2026-08-10 praezisiert worden und heisst seither ZZPRDAT.

Ziel ist, dass das Produktionsdatum unabhaengig vom Dynpro immer gespeichert
wird, einmalig bei der Freigabe und danach write-once. Als Loesungsweg vorgesehen
sind das BAdI `WORKORDER_UPDATE` und ein neuer Baustein `Z_PP_PRDDAT_SET`.
(Namen ueberholt: gebaut wurde `Z_ZZPRDAT_SET` in der Funktionsgruppe `ZPP_ZZPRDAT`,
siehe oben. Das Ziel selbst gilt unveraendert und ist erreicht.)

**Abgleich am 2026-08-26.** Dieser Block stand noch auf dem Kenntnisstand vor der
SapProbe-Pruefung vom 2026-07-27. Vier Angaben waren ueberholt und sind hier
korrigiert.

1. **Die Ursache ist nicht der unbesuchte Tab, sondern fehlender Code.** Bisher
   stand hier, der Kundensubscreen der Erweiterung `PPCO0012` schreibe das Datum
   nur bei Tab-Besuch und Freigabe. Die Quelltextpruefung mit `abap-read` zeigt
   etwas anderes: die gesamte Schreiblogik ist auskommentiert. `ZXCO1U11` und
   `ZXCO1U12` sind Zeile fuer Zeile deaktiviert, `ZXCO1O01` hat einen leeren
   Rumpf, und `ZXCO1I01` enthaelt mit `MOVE-CORRESPONDING ci_aufk TO ci_aufk`
   einen No-op. Es gibt derzeit keinen einzigen aktiven Codepfad, der
   `AUFK-ZZPRDAT` schreibt. Die Tab-Bedingung beschreibt den beabsichtigten Code,
   nicht den vorhandenen. Folge fuer die Umsetzung: der Altcode taugt weder fuer
   die Trigger-Logik noch fuer die Feldzuordnung als Referenz, die
   Neuimplementierung wird vollstaendig aus der Anforderung abgeleitet.
2. **Der bisherige naechste Schritt ist erledigt und ergebnislos.** Die
   Aenderungsbelege zum Auftrag 1214608 wurden gelesen. `CDPOS` liefert zu
   Objektklasse `ORDER` und Objekt-ID `000001214608` keine Zeile, weder generell
   fuer `TABNAME = 'AUFK'` in T76 noch gezielt fuer `FNAME = 'ZZPRDAT'` in T76 und
   P76. Aenderungsbelege scheiden als Nachweisquelle aus. Dieser Schritt darf
   nicht laenger als Blocker gefuehrt werden.
3. **Der Referenzfall 1214608 traegt in dieser Form nicht.** Die hier frueher
   genannte Abweichung zwischen `DGLTP` mit dem 02.12.2025 und `ZZPRDAT` mit dem
   20.11.2025 gibt es live nicht. Am 2026-07-27 zeigt P76 Mandant 100 fuer diesen
   Auftrag `AFKO-GLTRP` und `AFPO-DGLTP` mit dem 08.01.2026 und `AUFK-ZZPRDAT`
   mit `00000000`, also leer.
4. **Kopf gegen Position ist fuer Einpositionsauftraege beantwortet.**
   `AFKO-GLTRP` und `AFPO-DGLTP` sind fuer alle vier Referenzauftraege 1214608,
   1216195, 1214481 und 1214062 auf T76 und P76 identisch. Als Quelle bleibt
   `AFKO-GLTRP` sinnvoll, weil das Etikett je Auftrag gedruckt wird. Nicht
   geprueft sind Mehrpositionsauftraege mit abweichenden Terminen.

Wirklich offen sind nur noch drei Punkte:

- **Trigger** mit Lucas Castro und Florian Waechter. Die Anforderung sagt „beim
  Auftragsstart", Adil hat „nach Freigabe" beobachtet. Bei automatischer Freigabe
  faellt beides zusammen, bei manueller nicht. Das ist der einzige verbliebene
  fachliche Blocker vor der Implementierung.
- **Quellfeld** `GLTRP` gegen `GLTRS` von Marco Di Menco oder Florian Waechter
  bestaetigen lassen. Die Anforderung sagt woertlich Eck-End-Termin, das ist
  `GLTRP`.
- **Neue Rueckfrage an Marco Di Menco**: Woher stammt der Auszug mit
  `ZZPRDAT = 20.11.2025` fuer 1214608, mit Ziehungsdatum und Report? Der
  Live-Stand widerspricht ihm. Die wahrscheinlichste Erklaerung ist Adils
  Kopierprogramm von Ende November 2025, weil der Exit nie geschrieben hat.

Danach folgen Implementierung, Test, Transport und die Nachbefuellung der
bestehenden Auftraege. Drei technische Punkte stehen dabei schon fest. Der eigene
Verbuchungsbaustein kann vor der Standard-CO-Verbuchung laufen und wird dann
wieder ueberschrieben; das Fehlerbild saehe genauso aus wie heute, deshalb gehoert
ein Diagnoselauf mit direktem Lesen nach dem Commit eingeplant und notfalls das
Ausweichen auf `IN_UPDATE`. Die exakte Signatur des BAdI ist releaseabhaengig und
muss im eigenen System gelesen werden; ein SAP-Community-Beitrag taugt als
Hinweis, nicht als Beleg. Und der Punkt „Altlogik entschaerfen" ist gegenstandslos,
solange der Exit auskommentiert bleibt: Er wird erst wieder relevant, wenn jemand
die Schreiblogik dort reaktiviert. Das Feld im Subscreen auf Anzeige zu setzen
bleibt trotzdem sinnvoll, damit write-once nicht ueber `CO02` aushebelbar ist.

Die Testmatrix mit acht Faellen in T76/100 ist unveraendert vollstaendig offen.
Kritisch sind `MD04` fuer die Schweiz, `CO41` fuer Tschechien, die automatische
Freigabe und der zweite Save nach einer Terminverschiebung.

Quelle im Repository: `saptasks/zzprdat-kontext.md`. Fuehrend sind dort die beiden
Nachtraege vom 2026-07-27; die Abschnitte 3 und 7 beschreiben den Stand davor.

### PM-04 Einkaufsdashboard: Spend mit Drilldown

Aufgenommen am 2026-07-27. Der Drilldown ist eingebaut. Die verbliebene
Anforderung war, Beschaffungs-Warengruppe und Produktgruppe belastbar ueber eine
SAP-Tabelle zu etablieren statt ueber abgeleitete Logik.

**Diese Anforderung ist am 2026-08-12 erfuellt worden**, was im Chatprotokoll
noch nicht steht. Beide SAP-EntitySets sind produktiv aktiv, das produktive
`$metadata` liefert HTTP 200 mit 62 EntitySets, `ZDISPO_GRPSet` liefert 45 Zeilen
und `ZDISPO_SPARTSet` 22 Zeilen. Der produktive Einkauf-Delta lief um 10:03:42
MESZ mit `Success`. Der Cache enthaelt danach 45 Regeln aus SAP OData und null
Regeln aus Excel oder anderen Quellen. Excel ist damit als Mappingquelle
vollstaendig abgeloest.

Zwei SAP-Nacharbeiten bleiben, beide ohne Betriebsauswirkung:

1. `ZDISPO_SPART` liefert fuer die Codes D1 und D5 keinen Text, die Anwendung
   zeigt deshalb den SAP-Code an.
2. `ZDISPO_GRP` hat in den produktiven Metadaten nur `DISPO` als Key, obwohl
   `DISPO` in neun Gruppen mehrfach vorkommt. SEGW sollte auf den
   zusammengesetzten Key `DISPO_KZ + DISPO` korrigiert werden.

Quelle: `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md`.

**Nachtrag 2026-09-11, Stand der beiden Restpunkte:** unveraendert offen, gegen
`docs/rag/PURCHASING.md` geprueft. Am Dashboard selbst ist seither zweierlei passiert.

Erstens ist es seit dem 2026-09-03 deutlich schneller: Alle 15 Routen teilen pro Filter
einen 15 Minuten gueltigen Snapshot, maximal 32 Filter, Single-Flight. Vorher brauchten
selbst warme Aufrufe von `/einkauf` und `/einkauf/aufriss` je rund 9 bis 11 Sekunden; nach
der einmaligen Kaltberechnung liefern alle weiteren Routen jetzt HTTPS 200 in 0,05 bis
0,14 Sekunden. Commits `756e931` und `2444731`, 674/674 Tests gruen.

Zweitens sind am 2026-09-10 um 09:40 verstaendlichere Kacheln, Status- und Filtertexte
sowie eine aufklappbare Quellenhilfe mit SAP-Feldern und Formeln produktiv gegangen
(Commit `d5bc321`, 691/691 Tests). **Dabei ist etwas liegengeblieben:** die zuletzt
geaenderten drei Statusbeschriftungen waren nicht committet und sind deshalb nicht
ausgeliefert. Produktiv steht weiterhin `Bestellwert im Zeitraum` statt `Gebuchter Spend`
und `Einteilungen im Zeitraum` statt `Einteilungen`, an vier Stellen in
`Components/Pages/PurchasingDashboard.razor` und sechs in
`Services/PurchasingUiTextCatalog.cs`. Ausserdem hat noch niemand die neuen Texte
produktiv angesehen.

### PM-05 Finance: alle Daten in einem zentralen Excel

Aufgenommen am 2026-07-27. Das zentrale Excel existiert produktiv und wird
taeglich erzeugt. Das Arbeitspaket ist damit im Kern erledigt, die verbleibende
Arbeit ist Detail- und Datenqualitaetsarbeit.

Diese Detailarbeit wird **nicht hier**, sondern im Finance-Issue-Log gefuehrt.
Dort stehen zwoelf Issues mit eigenem Owner und eigenem Status.

Die wichtigsten offenen Punkte von dort, nur als Verweis:

- Datenzufluss TR FR steht seit dem 2026-07-30, Antwort aus Frankreich fehlt.
- CH/AT-Herstellerregel: der dafuer gebaute Umschalter ist seit 31.08.2026 produktiv;
  der Default behaelt die bisherige Regel bei, die Fachentscheidung von Andreas steht noch aus.
- Moving Average bei TR IT mit Paola, Zieldatum Ende August 2026.
- Fachfreigabe der Gruppenmarge als fuehrender Abschlusswert.

Quellen: `docs/Issue_Log_Konsolidiert_2026-08-12.tsv` als Statusquelle,
`docs/FINANCE_OFFENE_PUNKTE_2026-08-12.md` als Begruendung,
`docs/rag/FINANCE.md` als fachlicher Einstieg.

**Nachtrag 2026-09-11.** Die Aufzaehlung darueber ist der Stand vom 2026-08-31 und in
zwei Punkten ueberholt. Die Bewertungsmethode von TR IT ist seit dem 2026-09-09
entschieden und zurueckgestellt: bestehende B1-Artikel lassen sich laut Paola nicht
umstellen, es braeuchte ein Neucodierungsprojekt ueber 31’600 Artikel. Und die fuenf
Ausgleichsfelder je Buchungszeile sind seit dem Deploy vom 2026-09-10 um 08:53 produktiv;
die Schemamigration hat das Journal von 29 auf 34 Spalten erweitert, 691/691 Tests gruen.
`Finance_All` ist am selben Tag neu erzeugt worden, 470’499 Buchungszeilen, `due date`
darin vollstaendig belegt. Offen ist dort nur noch der Fachentscheid von Andreas, was
`date paid` bedeuten soll.

Hinzugekommen ist ein Punkt, der vorher nicht sichtbar war: Am 2026-09-10 wurde gemessen,
dass die Segmentzuweisung **nur fuer Deutschland** automatisch laeuft, weil nur dort ein
Branchenfeld im Quellsystem steht. Der naechste Schritt ist kein Code, sondern ein
fachlicher Entscheid ueber rund 38 Branchenwerte. Siehe PM-08 und
`docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md` Abschnitt 18.

### PM-07 HR: automatische Auswertung der REXX-Files

Aufgenommen am 2026-08-19. Das bestehende HR Cockpit (siehe Historie unten,
Abschnitt „HR Cockpit") wurde 2026-05 einmalig aus REXX-Files aufgebaut
(Fluktuationsformel von Sonja, Prozentsaetze aus REXX, Plausibilisierung mit
Nadja). Das war ein einmaliger manueller Einbau, keine wiederkehrende
Aktualisierung.

**Neuer Auftrag:** Eine externe Firma baut einen automatischen Exporter fuer
die REXX-Files ein, sodass diese Dateien kuenftig periodisch aktualisiert
werden. Ziel ist eine automatische Auswertung dieser periodisch aktualisierten
REXX-Files im HR Cockpit, statt eines einmaligen manuellen Imports.

**Offen:** Zeitplan und technische Schnittstelle des externen Exporters
(Ablagepfad, Format, Frequenz) sind noch nicht bekannt; das ist Voraussetzung
fuer die technische Umsetzung der automatischen Auswertung.

---

### PM-08 Railway: Auswertung fuer Rohail Munir (DE), Termin 2026-09-08

> **Der Stand in diesem Abschnitt ist der vom 2026-08-27 und in wesentlichen Teilen
> ueberholt.** Blocker 1 ist am 2026-09-09 geloest, Blocker 2 nur noch fuer die anderen
> Standorte offen, und der Termin 2026-09-08 ist verstrichen. Der gueltige Stand steht im
> Nachtrag vom 2026-09-11 am Ende dieses Abschnitts. Die Messungen unten bleiben als
> datierte Belege stehen.

**Harter Termin.** Rohail Munir aus Deutschland hat am 2026-08-27 nachgefragt und braucht den
Export **bis spaetestens 2026-09-08**, um damit den Projektmanagement-Status zu praesentieren.
Das sind ab heute zwoelf Tage.

Worum es geht: Railway ist ein Marktsegment, also eine Kundenkategorie. Die Frage lautet, wie
viel Umsatz der Konzern mit der Bahnindustrie macht. Das Segment haengt am **Kunden**, nicht am
Produkt, weil derselbe Drucktransmitter in einen Zug oder in eine Werkzeugmaschine gehen kann.
Die Technik dafuer steht seit dem 2026-08-13 produktiv, siehe
`docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md`. Fachlicher Eigentuemer der Zuordnung ist Patrik aus
dem Vertrieb, von dem die Marktumfrage vom Mai 2026 stammt.

#### Ist-Stand, produktiv gemessen am 2026-08-27 (UEBERHOLT)

**Diese Tabelle ist der Stand vom 2026-08-27 und nicht mehr gueltig.** Der aktuelle
Stand steht im Nachtrag vom 2026-09-11 weiter unten: Deutschland ist zugeordnet, und
von 196 Zuordnungen sind 25 bestaetigt.

| Standort | Vorschlaege | bestaetigt |
| --- | ---: | ---: |
| TRCH | 81 | 0 |
| TRIT | 40 | 0 |
| TRFR | 17 | 0 |
| TRUK | 13 | 0 |
| TRES | 9 | 0 |
| TRAT | 8 | 0 |
| TRIN | 4 | 0 |
| TRUS | 1 | 0 |
| **TRDE** | **0** | **0** |

**Heute wuerde der Export leer bleiben.** Unbestaetigte Vorschlaege wirken bewusst nicht im
zentralen Excel, und bestaetigt ist bisher keiner der 173.

#### Blocker 1 (GELOEST am 2026-09-09): Deutschland kann gar nicht mitspielen — und genau von dort kommt die Anfrage

**Dieser Blocker ist erledigt.** Rohail Munirs Rechnungsliste vom 2026-09-09 lieferte die
fachliche Adressnummer, der produktive Nachzug lief am selben Tag um 14:37. Der folgende
Text ist der Befund vom 2026-08-27 und bleibt nur als datierter Beleg stehen; die
darin geforderte Erweiterung der Alphaplan-Query wurde nicht gebraucht. Gueltig ist der
Nachtrag vom 2026-09-11 am Ende dieses Abschnitts.

Das ist der kritische Punkt, weil der Anfragende selbst aus Deutschland kommt und mit hoher
Wahrscheinlichkeit deutsche Zahlen erwartet.

Gemessen am 2026-08-27 mit `.tmp_tools/CheckRailwayDe` (read-only): Bei **allen 7'526 deutschen
Verkaufszeilen fehlt der Kundenname**, ebenso das Kundenland. Nur die Kundennummer ist gefuellt.
Deutschland ist der einzige Standort mit dieser Luecke, die anderen acht haben durchgehend
Namen. Der Namensabgleich, aus dem die 173 Vorschlaege stammen, konnte fuer Deutschland deshalb
nichts finden — es gab nichts zu vergleichen.

**Das ist unsere Luecke, nicht die von Deutschland.** Laut `docs/FINANCE_FELDLUECKEN.md`
Abschnitt 6 selektiert die Alphaplan-Query die `RechnungsAdressenID`, loest sie aber nie zu
einem Namen auf. Gebraucht wird ein read-only Auszug aus `INFORMATION_SCHEMA.COLUMNS` der
Alphaplan-Datenbank, gefiltert auf `%Adress%`, `%Artikel%`, `%Liefer%`, `%Kunde%`. Danach kann
die Query selbst erweitert werden. **Keine Tabellennamen raten** — das war die Lehre aus
UK-2025.

Die Daten waeren da: Die Marktumfrage enthaelt `67` deutsche Zeilen mit genau den erwarteten
Namen, darunter DB Regio, DB Fahrzeuginstandhaltung, Bombardier Transportation,
AKW A+V Protec Rail und DEUTA-WERKE. Davon sind `27` mit gar keinem Verkaufskunden verknuepft,
die uebrigen mit TRIT (`18`), TRCH (`17`), TRAT (`3`), TRUK (`1`) und TRES (`1`).
**Mit TRDE ist keine einzige verknuepft**, weil die Verknuepfung ueber den Namen laeuft.

#### Blocker 2 (TEILWEISE ERLEDIGT): niemand hat bestaetigt

**Fuer Deutschland gilt das nicht mehr:** dort sind seit dem 2026-09-09 24 Bahnkunden
bestaetigt. Offen sind nur noch die 171 Vorschlaege der anderen acht Standorte. Der
folgende Text ist der Stand vom 2026-08-27; der Grundsatz darin, dass der Vertrieb und
nicht Ingo bestaetigt, gilt unveraendert weiter.

Kein technisches Problem, sondern ein Fachentscheid des Vertriebs. **Ingo hat am 2026-08-27
festgelegt: Patrik prueft vorher, ob die Zuordnung passt, oder macht sie gleich selbst.** Es
wird also nicht blind bestaetigt, und Ingo bestaetigt auch nicht stellvertretend — die
Segmentzuordnung ist eine Vertriebsentscheidung und bleibt dort. Sobald der erste Vorschlag
auf `/marktsegmente` bestaetigt ist, erscheint im Reiter `Ergebnis` der erste Bahnumsatz je Land
und Waehrung. Die 30 mengenstaerksten Vorschlaege decken rund zwei Drittel der betroffenen
Verkaufszeilen ab, Liste: `docs/Railway_Kundenpruefung_Patrik_2026-08-13.xlsx`, Anleitung:
`docs/Anleitung_Marktsegmente_Vertrieb_2026-08-13.docx`.

Offen bleibt dabei der Fachentscheid, ob breit einkaufende Kunden wie Siemens pauschal als
Railway gelten. Die Oberflaeche warnt ab vier Produktsparten, entscheiden muss der Vertrieb.

#### Was bis zum 2026-09-08 realistisch ist (UEBERHOLT, Termin verstrichen)

**Der Termin 2026-09-08 ist verstrichen, ohne dass etwas an Rohail versendet wurde.**
Die folgende Einschaetzung stammt vom 2026-08-27 und bleibt als datierter Beleg stehen.

Blocker 2 ist in Tagen loesbar, sobald Patrik die Zuordnung geprueft oder selbst gesetzt hat;
die Auswertung fuer acht Standorte steht dann sofort. Der erste Schritt ist deshalb, Patrik zu
bitten, mit der Liste der 30 mengenstaerksten Vorschlaege anzufangen. Blocker 1 haengt an einem Auszug aus dem deutschen Server und ist
nicht allein von hier aus zu erzwingen. Der Schritt sollte deshalb **sofort** angestossen
werden, nicht erst nach der Bestaetigungsrunde — die beiden Blocker lassen sich parallel
bearbeiten.

**Rohail Munir sollte frueh wissen, dass Deutschland moeglicherweise fehlt.** Ein Export, der
Deutschland stillschweigend mit null Bahnumsatz zeigt, waere schlechter als einer, der die
Luecke benennt — besonders in einer Praesentation zum Projektstatus. Genau dieser Fehler ist im
Repository schon dokumentiert: eine gefuellte Zahl ohne Vorbehalt ist gefaehrlicher als eine
sichtbar offene Position.

#### Nachtrag 2026-09-01: Excel-Export per Mausklick, PRODUKTIV DEPLOYED

`/marktsegmente` hat jetzt den Knopf `Export (Excel)`, der die Pruefmenge als Arbeitsdatei mit
acht Blaettern herunterlaedt. Deployed am 2026-09-01 um 09:21, `668/668` Release-Tests gruen.
Details: `docs/KONZEPT_RAILWAY_EXPORT_2026-09-01.md` und
`docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md` Abschnitt 15.

**Damit ist ein Werkzeug fuer Patrik da, die beiden Blocker oben sind es nicht.** Blocker 1
(Deutschland ohne Kundennamen) bleibt unveraendert offen, das Blatt `Datenluecken` weist ihn nur
aus. Blocker 2 (niemand hat bestaetigt) bleibt ebenfalls offen: Stand des Deploytags waren
weiterhin `0` von `173` Vorschlaegen bestaetigt. Der Export macht die Pruefung fuer Patrik
bequemer, nimmt sie ihm aber nicht ab.

#### Nachtrag 2026-09-11: Blocker 1 geloest, Termin ueberschritten

**Blocker 1 ist erledigt.** Rohail Munir hat am 2026-09-09 selbst geliefert, was fehlte:
`Rechnungen_20260909.xlsx` mit 60’539 Rechnungen, jeweils mit Rechnungsnummer und der
**fachlichen** `Adressnr._R`. Damit wird die interne `RechnungsAdressenID` als Bruecke gar
nicht mehr gebraucht. Geprueft deckt die Datei 7’531 der 7’615 deutschen Zeilen und wurde
unabhaengig gegen `docs/2025_DataExport_DE.xlsx` mit 99,66 Prozent bestaetigt.

Produktiv nachgezogen am 2026-09-09 um 14:37, 3’043 Zeilen in einer Transaktion: fachliche
Kundennummern von 4’549 auf **7’592 von 7’622**, bestaetigte Segmente von 19 auf 24,
`quick_check` `ok`, Zeilenzahl und `SalesPriceValue` unveraendert. **Der deutsche Bahnumsatz
ist damit erstmals belegbar:** 2025 rund 578’636 EUR, 2026 bis zum 09.09. rund 432’237 EUR.
Der fruehere Nachzug desselben Tages um 07:20 mit 4’549 Zeilen ist damit ueberholt.

Offen bleiben aus diesem Schritt 30 Gutschriftenzeilen ohne fachliche Nummer, weil Rohails
Datei keine Gutschriften enthaelt, sowie die 40 Zeilen der drei Doppelnummern Sonepar, EMS
und Magnetic Sense, die der Nachzug bewusst nicht angefasst hat. Details:
`docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md` Abschnitt 9.

**Blocker 2 besteht fort, aber nur noch ausserhalb Deutschlands.** Am 2026-09-10 produktiv
gemessen: von 196 Zuordnungen sind 25 bestaetigt, davon 24 aus TRDE mit der Quelle
`Alphaplan Kundenstamm / Branche`. Die uebrigen 171 sind weiterhin
Namensabgleich-Vorschlaege und warten auf Patrik.

**Der eigentliche Befund dieser Messung geht aber ueber PM-08 hinaus.** Deutschland laeuft
automatisch, weil dort ein Branchenfeld im Quellsystem steht; die anderen acht Standorte
haben keines. Fuellgrad `CustomerIndustry`: TRDE 7’433 Zeilen und 543 Kunden, TRFR 221/20,
TRIN 21/6, TRIT 10/2, und TRSE, TRUK, TRUS sowie ZSCHWEIZ **null** — wobei ZSCHWEIZ mit
52’276 Zeilen der groesste Standort ueberhaupt ist. Die 543 deutschen Kunden verteilen sich
auf 38 Branchenwerte und decken 97,9 Prozent des deutschen Umsatzes ab; genutzt werden
bisher nur die 24 mit `00 Bahn`, und in `CustomerMarketSegments` existiert ueberhaupt nur
ein einziger Segmentwert, `Railway`. **Der naechste Schritt ist deshalb kein Code, sondern
ein fachlicher Entscheid Branche auf Segment** ueber rund 38 Zeilen, beim Vertrieb
beziehungsweise bei Andreas. Nebenbei sind zwei Datenmaengel im deutschen Kundenstamm
aufgefallen, die neun Kunden betreffen: Nummer `52` traegt zwei Bezeichnungen, und zwei
Branchenwerte enden auf ein Komma. Details:
`docs/MARKTSEGMENTE_RAILWAY_2026-08-13.md` Abschnitt 18.

**Zum Termin:** der 2026-09-08 ist verstrichen, ohne dass etwas an Rohail versendet wurde.
Die korrigierte DE-Pruefmappe `Bahnmarkt_Rohail_2026-09-09.xlsx` liegt erzeugt und
rueckgelesen bereit. Ein neues Zieldatum ist mit Rohail zu vereinbaren; das entscheidet
Ingo, nicht diese Datei.

---

## 3. Erledigt

Verdichtetes Archiv aus `kontext.txt`. Ein Eintrag je abgeschlossenem Punkt.

### Trafag Management Reporting

| Datum | Punkt | Ergebnis |
|---|---|---|
| 2026-05-12 | ES Lesezugriff Sage | Geloest ueber CSV-Export statt Direktzugriff, damit waren Santis Sicherheitsbedenken gegenstandslos |
| 2026-05-12 | Durchsprache Santi Gomez | Stattgefunden |
| 2026-05-12 | Rhino Zugangsdaten | Von Marco erhalten |
| 2026-05-19 | Intercompany-Abgrenzungen | Mit Andreas Stoller geklaert |
| 2026-05-27 | DE File von Rohail Munir | Geliefert, nach 22 Tagen und zweimaligem Nachfassen |
| 2026-07-02 | Wechselkurse 2025 und 2026 im zentralen Excel pruefen | Geprueft |

### HR Cockpit

| Datum | Punkt | Ergebnis |
|---|---|---|
| 2026-05-12 | Fluktuationsformel von Sonja | Erhalten, nur Arbeitnehmerkuendigungen |
| 2026-05-15 | Prozentsaetze aus REXX | Eingebaut: Krankheit, Unfaelle, Soll-Arbeitszeit |
| 2026-05-15 | Fluktuation in PowerBI einbauen | Eingebaut |
| 2026-05-27 | Fluktuation plausibilisieren | Mit Nadjas Testkriterien abgeschlossen, Vergleich moeglich |

### SAP-Entwicklung

| Datum | Punkt | Ergebnis |
|---|---|---|
| 2026-08-20 | PM-06 PPWR und Stoffcompliance ueber SAP-Klassifizierung | Komplett erledigt laut Rueckmeldung Ingo (Pilotmaterialien zugeordnet, CL30N-Abnahme gefahren). Technische Anlage in T76/100 vorher schon per Tabellenzaehlung bestaetigt (`docs/PPWR_MANDANT_100_ANALYSE_2026-08-18.md`); Details zur finalen Pilotzuordnung/Abnahme sind in dieser Sitzung nicht selbst gemessen worden. P76-Transport bleibt gemaess Anlageprotokoll bis zur Fachfreigabe gesperrt. |
| 2026-05-15 | Mandant 200, Kundenzahlen faken | Von Fabio getestet |
| 2026-05-18 | ZC12 Test | Von Adil getestet und freigegeben |
| 2026-05-28 | ZLO03 Fakenummer 9999 bei Textposition | Erledigt, 23 Tage nach der urspruenglichen Deadline vom 05.05. |
| 2026-05-28 | ZLO03 Sternchen-Filter, fehlende VKNR | Erledigt |
| 2026-06-04 | Massupload Mappe | Erledigt |
| 2026-07-02 | Preiskondition Bruttopreis 9999 fuer Fabio | Erledigt |

### Organisation

| Datum | Punkt | Ergebnis |
|---|---|---|
| 2026-05-18 | Smartsheet Vollzugriff Philip Steiger | Erledigt, war seit April offen |
| 2026-06-04 | PowerBI-Lizenzen | Erledigt durch Gunther |
| 2026-06-04 | Uebersicht fuer den SAP-Koordinator | Erstellt |
| 2026-07-02 | Punkt 4 Vorschriften Lager | Erledigt |

### Verworfen

| Datum | Punkt | Grund |
|---|---|---|
| 2026-07-02 | Fake-Namen fuer alle Programme und Tabellen | Hinfaellig, der auftraggebende Vorgesetzte hat das Unternehmen verlassen. Zustaendig war Gunther |

---

## 4. Personen

| Person | Thema im bisherigen Verlauf |
|---|---|
| Andreas Stoller | Finance, Intercompany-Abgrenzung, Standardkosten, offene Fachentscheide zur Gruppenmarge |
| Santi Gomez | Spanien, Sage, CSV-Export |
| Rohail Munir | Deutschland, DE File; braucht die Railway-Auswertung bis 2026-09-08 (PM-08) |
| Marco | Rhino, CHF-Konsolidierung |
| Marco Di Menco | ZZPRDAT, Entscheidung Kopf- gegen Positionsebene |
| Nadja | HR Cockpit, Plausibilisierung der Fluktuation |
| Sonja | HR, Fluktuationsformel |
| Gunther | PowerBI-Lizenzen, ehemals Fake-Namen |
| Fabio | Test Mandant 200, Preiskondition Bruttopreis |
| Adil | Test ZC12 |
| Philip Steiger | Smartsheet |
| Patrik | Vertrieb, Marktsegment Railway; prueft die Kundenzuordnung oder setzt sie selbst |
| Ann-Katrin Michel | Fachanwenderin ZLO03, Phase-Out-Prozess |
| Sandro Moltisanti | ZLO03, Meldung CS15 42 gegen ZLO03 21 |
| Lucas Castro | ZZPRDAT, Trigger-Klaerung |
| Florian Waechter | ZZPRDAT, Trigger-Klaerung |
| Paola | Italien, Moving Average und Cost Run |

Belegte Laenderzuordnung gibt es nur fuer Spanien mit Santi und Deutschland mit
Rohail. Fuer die uebrigen Personen ist im Protokoll kein Land genannt. Die
Standortempfaenger stehen gepflegt in `docs/ANSPRECHPARTNER.md`.

---

## 5. Erfahrungen aus dem bisherigen Verlauf

Diese Punkte haben im Chatverlauf wiederholt Zeit gekostet und gehoeren deshalb
festgehalten.

**Ein Chatverlauf ist keine Aufgabenliste.** Genau deshalb existiert diese Datei.
Im Protokoll sind Punkte mehrfach neu aufgerollt worden, Erledigtes tauchte
wieder als offen auf, und die Statusfrage musste jedes Mal neu beantwortet
werden.

**Datumsangaben nie raten.** Am 2026-05-16 gab es einen Konflikt zwischen der
Nutzerangabe und dem Systemzeitstempel, richtig war der 18.05. Bei einer
Statusaussage gehoert das Datum belegt, nicht geschaetzt.

**Anforderung vor Code.** Mehrere Punkte, etwa Massupload Mappe und
Preiskondition 9999, waren zunaechst nur ein Stichwort. Erst die Rueckfragen
haben den Auftrag brauchbar gemacht. Dieselbe Regel steht als „Diagnose vor
Code" auch in `zlo03/CLAUDE.md`.

**Statusangaben gegen die Quelle pruefen.** Beim Abgleich am 2026-08-14 war
PM-04 laengst erledigt und PM-01 deutlich weiter, als das Protokoll auswies. Ein
Punkt gilt erst dann als offen, wenn die Quelle das bestaetigt.

**Ticketvokabular ist nicht Codevokabular.** Eine Repository-Suche nach den
Begriffen aus einem Ticket geht regelmaessig ins Leere, weil der Code andere
Woerter benutzt. Bei PM-02 heisst „Nullmenge" im Coding schlicht `VGW01` bis
`VGW04` gleich null, und die betroffene Routine heisst `fmt_quan`. Ein Personen-
name wie „Adil" steht ohnehin nur in der Kopfhistorie. Bei einer erfolglosen
Suche also zuerst fragen, wie der Sachverhalt im Code heissen wuerde, statt auf
„nicht vorhanden" zu schliessen.

---

## 6. Pflege dieser Datei

1. Statusaenderung immer mit Datum in der Tabelle unter Abschnitt 1 eintragen.
2. Ist ein Punkt erledigt, die Zeile aus Abschnitt 1 entfernen und als eine Zeile
   in Abschnitt 3 mit Datum und Ergebnis ablegen. Der Detailblock aus Abschnitt 2
   entfaellt dabei.
3. Neue Aufgaben bekommen die naechste freie Nummer, also ab PM-06. Nummern
   werden nicht wiederverwendet.
4. Finance-Details gehoeren in
   `docs/Issue_Log_Konsolidiert_2026-08-12.tsv`, nicht hierher.
5. Vor Arbeiten im Repository gilt zusaetzlich die Pflicht aus `CLAUDE.md`, also
   zuerst `docs/AGENT_COORDINATION.md` lesen und den eigenen Bereich eintragen.
