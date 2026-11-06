# ZZPRDAT: Weg vom Testaufbau zum Transport

Stand: 2026-11-06 09:00. **Transport gebuendelt:** `T76K912851` (Anzeige `ZXCO1U11`) ist in `T76K912490` verschmolzen, Georgs sechs weitere XCO1-Objekte liegen in dessen Aufgabe `T76K912852`; Objektliste und Belege im Nachtrag 2026-11-06 zu Abschnitt 1. Nichts freigegeben, P76 nicht beruehrt. Frueherer Stand: 2026-10-26 (Technik unverändert seit 2026-09-07, am 26.10. live bestätigt, siehe Abschnitt 6a „Live-Pruefung vom 2026-10-26“). **Abnahmetest verschoben:** Abnahmetest mit Lucas, Marco, Adil und Ingo: war auf 12.10.2026 16:00 gelegt, am 12.10. um zwei Wochen verschoben (voraussichtlich Woche ab 26.10.2026, genauer Termin offen). Stand in T76 am 12.10. live geprüft: seit 07.09. unverändert, T76K912490/T76K912491 offen, BAdI Z_ZZPRDAT_UPDATE aktiv, Z_ZZPRDAT_SET als V2.

Stand der Technik: 2026-09-07. **Der Transport ist angelegt und gefuellt, die Objekte sind neu in `ZPP1`
gebaut und auf allen sieben Wegen nachgetestet. Freigegeben ist der Auftrag nicht** — das
geschieht erst nach der fachlichen Abnahme.

> **Am 2026-09-07 gefunden und noch am selben Tag behoben.** Eine Gegenpruefung hat einen
> Konstruktionsfehler gezeigt: `BEFORE_UPDATE` laeuft bei **jedem** Sichern, nicht nur bei
> der Freigabe, und fuellte damit Altauftraege ungewollt nach. Die Freigabepruefung ist
> eingebaut, aktiviert und mit drei Messungen belegt; die Klasse steckt in der Aufgabe
> `T76K912491` des Auftrags. Anschliessend ist die **komplette Reihe wiederholt** worden,
> alle sieben Wege plus der neue Altauftragsfall. Einzelheiten in Abschnitt 6a.

Der fachliche Stand und der Analyseverlauf stehen in `saptasks/zzprdat-kontext.md`, das
Dokument fuer den Fachbereich in `docs/ZZPRDAT_Loesung_2026-09-03.docx`.

## 1. Der Transportauftrag

| | |
|---|---|
| Auftrag | **`T76K912490`** |
| Aufgaben | `T76K912491` (Entwicklung/Korrektur, KOI): BAdI-Loesung; seit 2026-11-06 zusaetzlich `T76K912852` (Entwicklung/Korrektur, KOI): Exit-Includes und Bild der Funktionsgruppe XCO1 |
| Kurztext | ZZPRDAT: Produktionsdatum bei Auftragsfreigabe |
| Typ | Workbench-Auftrag, Ziel `P76` |
| Status | **aenderbar, bewusst nicht freigegeben** |
| Paket | `ZPP1` |

Objektliste, am 2026-09-04 aus SE01 gelesen (*ueberholt am 2026-11-06:* das ist nur noch die Aufgabe
`T76K912491`; die vollstaendige Liste steht im Nachtrag 2026-11-06 am Ende dieses Abschnitts):

| Typ | Objekt |
|---|---|
| `FUGR` | `ZPP_ZZPRDAT` (Funktionsgruppe) |
| `SXCI` | `Z_ZZPRDAT_UPDATE` (BAdI-Implementierung) |
| `CLAS` | `ZCL_IM__ZZPRDAT_UPDATE` (Implementierungsklasse) |
| `ENHO` | `Z_ZZPRDAT` (Erweiterungsimplementierung, Container) |
| `PROG` | `Z_ZZPRDAT_CHECK` (Nachweisreport) |

Alle fuenf Teile liegen in **einem** Auftrag, weil sie nur gemeinsam funktionieren. Der
Verbuchungsbaustein `Z_ZZPRDAT_SET` steckt in der Funktionsgruppe und wandert mit ihr,
einschliesslich seiner Verarbeitungsart.

**Die Verarbeitungsart ist der Punkt, an dem eine unbedachte Aenderung die Loesung still
kaputt macht:** `Z_ZZPRDAT_SET` ist Verbuchungsbaustein mit **„Start verzoegert" (V2)**, am
2026-09-04 nach der Aktivierung noch einmal gegengelesen. Bei V1 („Start sofort")
ueberschreibt der SAP-Standard den Wert wieder.

### Nachtrag 2026-11-06: alle ZZPRDAT-Objekte in `T76K912490` gebuendelt

Auftrag Ingo vom 2026-11-06: „nimm alle objekte in mein auftrag was mit dem zzprdat zu tun haben“.
Gearbeitet nur in T76/100, in einer eigenen GUI-Sitzung (danach geschlossen), ohne Passwort; nichts
freigegeben, P76 nicht beruehrt. Stand vorher und nachher als Dateien in
`saptasks/zzprdat/transport_2026-11-06/` (`e070_*`, `e071_*`, `tlock_*`).

**Schritt 1, Verschmelzen von `T76K912851` in `T76K912490`.** SE09, Auswahlliste der aenderbaren
Auftraege, Cursor auf `T76K912851`, Menue *Hilfsmittel > Reorganisieren > Auftraege verschmelzen...*
(Dialog `SAPLSTRI` 0800). Der Dialog sagt selbst: „Der Inhalt des ersten Auftrags wird komplett in den
zweiten Auftrag umgehaengt, danach wird der erste Auftrag geloescht.“ Erster Auftrag `T76K912851`,
zweiter `T76K912490`, *Pruefen* (F5) ohne Einwand, *Weiter*. Meldung: „Auftraege wurden verschmolzen
(verbleibender Auftrag: T76K912490)“. In der Einzelanzeige eines Auftrags steht derselbe Menuepunkt
unter *Auftrag/Aufgabe*, verlangt dort aber den Aendern-Modus („Funktion nur fuer aenderbare Auftraege
moeglich“).

| Beleg nach dem Verschmelzen | Wert |
| --- | --- |
| `E070`/`E07T` `T76K912851` | kein Satz mehr (Auftrag geloescht) |
| `E070` `T76K912852` | `S`, `D`, `STRKORR = T76K912490`, Text unveraendert „ZZPRDAT: Anzeige Produktionsdatum im Reiter TRAFAG Daten“ |
| `E071` `T76K912490` Pos. 1 | `CORR MERG T76K912851 20261106 083930 KOI` (Protokolleintrag von SAP) |
| `TLOCK` `ZXCO1U11` | weiter `T76K912852`, `KOI` |

**Schritt 2, Georgs sechs weitere Objekte in Aufgabe `T76K912852`.** Der Weg ueber *Objekte aufnehmen >
Frei gewaehlte Objekte* aus der Auftragsliste brach mit „Positionieren Sie den Cursor auf einem
aenderbaren Auftrag“ ab, ohne etwas aufzunehmen (per `E071` geprueft). Gegangen ist: Doppelklick auf die
Aufgabe, Reiter *Objekte*, *Anzeigen <-> Aendern* (Strg+F1), sechs Zeilen eingetragen, Enter (SAP
erkennt „Reportsource“ bzw. „Dynpro“), Sichern: „Aenderungen wurden gesichert“. **Die von Hand
eingetragenen Zeilen waren danach nicht gesperrt** (`LOCKFLAG` leer, kein `TLOCK`); erst *Alle markieren*
und *Objekt sperren* setzte die Sperre. Das Bild heisst in `E071` `SAPLXCO10100` ohne Leerraum, wie in
Georgs Aufgabe. Aufgabe 852 statt 491, damit alle Exit-Objekte der Funktionsgruppe XCO1 zusammen liegen
und 491 die BAdI-Loesung bleibt; transportiert wird ohnehin der ganze Auftrag.

**Objektliste von `T76K912490` am 2026-11-06 (`E071`, alle Aufgaben, `LOCKFLAG` X ausser dem Protokolleintrag):**

| Aufgabe | Pos. | Objekt | Herkunft |
| --- | --- | --- | --- |
| `T76K912490` (Kopf) | 1 | `CORR MERG T76K912851 20261106 083930 KOI` | Protokoll des Verschmelzens, kein transportiertes Objekt |
| `T76K912491` | 1 | `R3TR FUGR ZPP_ZZPRDAT` | BAdI-Loesung (seit 2026-09-04) |
| `T76K912491` | 2 | `R3TR SXCI Z_ZZPRDAT_UPDATE` | BAdI-Loesung |
| `T76K912491` | 3 | `R3TR CLAS ZCL_IM__ZZPRDAT_UPDATE` | BAdI-Loesung |
| `T76K912491` | 4 | `R3TR ENHO Z_ZZPRDAT` | BAdI-Loesung |
| `T76K912491` | 5 | `R3TR PROG Z_ZZPRDAT_CHECK` | Nachweisreport |
| `T76K912852` | 1 | `LIMU REPS ZXCO1U11` | Anzeige im Reiter (`EXIT_SAPLCOKO1_001`), KOI 2026-11-06, aus `T76K912851` |
| `T76K912852` | 2 | `LIMU REPS ZXCO1O01` | **neu 06.11.**, Georg 2026-03-18 (PBO Bild 0100) |
| `T76K912852` | 3 | `LIMU DYNP SAPLXCO10100` | **neu 06.11.**, Bild des Reiters „TRAFAG Daten“ |
| `T76K912852` | 4 | `LIMU REPS ZXCO1I01` | **neu 06.11.**, Georg 2026-03-18 (PAI Bild 0100) |
| `T76K912852` | 5 | `LIMU REPS ZXCO1U12` | **neu 06.11.**, Georg 2026-03-31 (`EXIT_SAPLCOKO1_002`, in T76 auskommentiert) |
| `T76K912852` | 6 | `LIMU REPS ZXCO1TOP` | **neu 06.11.**, Georg 2026-03-18 (`TABLES ci_aufk`, `DATA gs_aufk TYPE ci_aufk`) |
| `T76K912852` | 7 | `LIMU REPS ZXCO1U06` | **neu 06.11.**, Georg 2026-03-31 (`EXIT_SAPLCOZV_001`, zweiter Schreibweg) |

`TLOCK` danach: zwoelf Saetze, alle `KOI`; fuenf fuer 491, sieben fuer 852 (sechs `PROG`, ein `FUGX XCO1`
fuer das Bild), also dieselben Objekttypen, die vorher Georgs Aufgabe gesperrt hatte.

**Warum die sechs Objekte mitgehen.** Georgs offener Umbau der sieben Objekte (REPOSRC `MEEY_GEWA`
2026-03-18 und 2026-03-31, frueher in seiner Aufgabe `T76K911784`, nie freigegeben) ist in T76 aktiv;
gegen ihn ist die Anzeige getestet. `ZXCO1U11` setzt `gs_aufk-zzprdat`, und `gs_aufk` ist nur in
`ZXCO1TOP` deklariert. Fehlt das in P76, waere `SAPLXCO1` nach einem Import nur von `ZXCO1U11`
syntaxfehlerhaft und CO01/CO02/CO03 brechen ab (P76 nicht gelesen, deshalb vorsorglich mitgenommen).
Alle drei Exits haengen am selben Programm: `TFDIR` `EXIT_SAPLCOKO1_001` Include 11,
`EXIT_SAPLCOKO1_002` Include 12, `EXIT_SAPLCOZV_001` Include 06, jeweils `PNAME SAPLXCO1`. Mit den
sieben Objekten kommt genau der in T76 gepruefte Stand der Funktionsgruppe nach P76.

**Hinweis `ZXCO1U06`: das ist ein zweiter Schreibweg auf `ZZPRDAT`.** Der aktive Stand setzt bei
`header_imp-ftrmi = sy-datum` und leerem `zzprdat` den Wert `header_exp-zzprdat = header_imp-gltrp`
(`anzeige_2026-11-05/vorher_ZXCO1U06.abap`). Mit diesem Auftrag geht Georgs Stand vom 2026-03-31 nach
P76. Eine fruehere Fassung ist bereits am 2025-12-02 in `T76K911416` „PPCO0007 FAUF: ProdDatum
implement / Progr“ freigegeben worden (VRSD `ZXCO1U06` Version 0 mit `KORRNUM T76K911416`); der Weg kann
in P76 also schon laufen, sofern importiert (Vermutung, P76-Importstand nicht geprueft). Ob er neben dem
BAdI bleiben soll, entscheiden Ingo und Georg. Herausnehmen allein schaltet ihn nicht ab: Ohne
`ZXCO1U06` in 490 bliebe in P76 die Fassung von 2025 wirksam (falls importiert). Abschalten hiesse, den
Quelltext zu aendern; das ist nicht Teil dieses Auftrags.

**Rueckleseabgleich.** Georgs Auftrag ist unveraendert: `E070` `T76K911783`/`T76K911784` Status `D`,
Zeitstempel `20261106 080535`/`080644` wie vorher, `E071` der Aufgabe weiter dieselben sieben Zeilen mit
leerem `LOCKFLAG`. An den Quelltexten hat sich nichts geaendert: `abap-read` der sechs Includes gegen
`anzeige_2026-11-05/vorher_*.abap` bzw. `nachher_ZXCO1U11.abap`, `diff -B -w` jeweils Exitcode 0; REPOSRC-
Stempel aller `ZXCO1*` und `SAPLXCO1` wie vor der Arbeit.

**Pruefungen.** `abap-check SAPLXCO1`: „Syntax status OK, Error subrc 0“. Das prueft die aktive Fassung;
sie ist hier die massgebliche, weil `DWINACTIV` fuer `%XCO1%` leer ist. ST22 06.11.2026: der
Zaehler „Heute“ auf dem Einstiegsbild (Titel „Alle Mandanten“) zeigt 1 Laufzeitfehler; die Liste
dazu: 08:02:12 `CALL_FUNCTION_NOT_REMOTE` in `SAPLSIFP` (KOI, Mandant 100), der bekannte Leseversuch vor
der Anzeigeaenderung. Seither keiner.

**Folge fuer Georg:** Die sieben Objekte sind jetzt fuer KOI in `T76K912852` gesperrt. Georg kann sie
nicht mehr in seinem Auftrag aendern, und eine Freigabe von `T76K911783` duerfte an diesen Sperren
scheitern, weil die Objekte dort noch in der Liste stehen (Vermutung, nicht getestet). Mit
`T76K912490` geht ausserdem sein Zwischenstand mit, der teils halbfertig ist: in `ZXCO1O01` ist die
einzige Zeile von `Fill_Prod_Date` auskommentiert, `ZXCO1I01` enthaelt nur
`MOVE-CORRESPONDING ci_aufk TO ci_aufk`, `ZXCO1U12` ist ganz auskommentiert
(`anzeige_2026-11-05/vorher_*.abap`). Funktional harmlos, aber Georg sollte es wissen.

**Nur aufgelistet, nicht aufgenommen: was fuer die Wirkung in P76 sonst noch zaehlt.**

| Objekt | Stand in T76 (Beleg) | Bewertung |
| --- | --- | --- |
| CMOD `ZPP00012` (Erweiterung PPCO0012: `EXIT_SAPLCOKO1_001/002`, Bild-Exit `SAPLCOKO1 0900 SSCR115 -> SAPLXCO1 0100`, Menue-Exit `SAPLCOCU +COI`; `MODSAP`) | `MODATTR STATUS A`, aktiviert 2025-11-30 von MEEY_GEWA. Letzte Transporte beide am 2026-03-31 freigegeben: `T76K911390` „ZPP00012 deaktivieren /2“ um 14:32:11, dann `T76K911398` „ZPP00012 AKTIVIEREN /2“ um 14:32:30 | Nichts offen in T76. In P76 aktiv, wenn beide in dieser Reihenfolge importiert sind (Vermutung). **Vor dem Import in STMS die Importhistorie P76 fuer 911390/911398 ansehen**; ohne aktives Projekt zeigt der Reiter nichts |
| CMOD `ZPP00005` (PPCO0007: `EXIT_SAPLCOZV_001` -> `ZXCO1U06`) | `MODATTR STATUS A` seit 2013-09-27 (I001067), Transport `T76K905144` 2013 freigegeben | Nichts offen; das Projekt ist der Grund, warum `ZXCO1U06` ueberhaupt laeuft |
| `ZZPRDAT` in `AUFK`/`CAUFVD`/`COCI_AUFK` | `DD03L`: ueber `CI_AUFK` -> Append `ZCO_ORD1` (MEEY_GEWA 2025-11-30) -> Datenelement `ZCO_GLTRP` (DATS). Transporte `T76K910856` „PP Produktionsdatum fuer Typenschild“ (2025-07-18) und `T76K911394` (2025-11-30), beide freigegeben; `T76K911110` (2025-10-27) ebenfalls | Nichts offen in T76. Ob in P76 importiert: nicht geprueft; ohne das Feld waeren `Z_ZZPRDAT_SET` und die Includes in P76 nicht uebersetzbar |
| Hauptprogramm `SAPLXCO1` | REPOSRC MEEY_GEWA 2026-03-31 14:32:33, Inhalt nur die vier Standardzeilen `INCLUDE LXCO1TOP/LXCO1UXX/LXCO1F00/ZXCO1ZZZ`, in keinem Auftrag (`E071`), SAP-Objekt (`TADIR FUGS XCO1`, SAP) | Vermutung: Neugenerierung bei Georgs Aktivierung. Nicht aufnehmen |
| `ZXCO1ZZZ` | MEEY_GEWA 2025-10-14, in `T76K911110` freigegeben; bindet `ZXCO1F01`, `ZXCO1F02`, `zxco1i01`, `zxco1o01` ein | Nichts offen |
| `ZXCO1U01`, `ZXCO1U05`, `ZXCO1F01`, `ZXCO1F02` | letzte Aenderung 2025-07-02 oder frueher, alle freigegeben; kein Treffer fuer `zzprdat`, `prdat`, `gs_aufk`, `ci_aufk` im Quelltext | Gehoeren nicht zu ZZPRDAT |
| Weitere Bilder der Funktionsgruppe | `D020S PROG SAPLXCO1`: nur `0100` | Bild ist aufgenommen |

**Hinweis aus den T76-Versionen, kein P76-Beleg:** Der zuletzt freigegebene Stand von `ZXCO1TOP`,
`ZXCO1O01`, `ZXCO1U11`, `ZXCO1U12` und Bild 0100 ist laut VRSD der aus `T76K911396` „PPCO0012 FAUF: Aend
neu implement / Progr“ (2025-11-30), von `ZXCO1I01` der aus `T76K911110` (2025-10-27), von `ZXCO1U06`
der aus `T76K911416` (2025-12-02). Diesen Stand hat P76 vermutlich heute; Georgs Aenderungen vom Maerz
2026 kommen erst mit `T76K912490`.

## 2. Warum vor der Abnahme transportiert wurde

Ingo hat das am 2026-09-03 vorgeschlagen, und der Gedanke traegt: Der Fachbereich soll
genau die Objekte abnehmen, die spaeter nach P76 gehen. Wer erst abnimmt und danach
umbaut, nimmt einen anderen Stand ab als den, der ausgeliefert wird. Deshalb erst bauen,
dann nachtesten, dann vorlegen — und die Freigabe des Auftrags bleibt der letzte Schritt.

## 3. Wie gebaut wurde: Weg B, neu anlegen

Von den beiden Wegen — Objekte aus `$TMP` nach `ZPP1` umhaengen (Weg A) oder sauber neu
anlegen (Weg B) — wurde Weg B umgesetzt. Funktionsgruppen und von SE19 erzeugte Klassen
lassen sich nicht umbenennen, und der Klassenname `ZCL_IM__…` wird aus dem Namen der
BAdI-Implementierung abgeleitet. Die Testnamen waeren also mitgewandert.

Der Namensvorschlag aus der Fassung vom 2026-09-03 (`ZPP_PRDDAT`, `Z_PP_PRDDAT_UPDATE`,
`Z_PP_PRDDAT`, `Z_PP_PRDDAT_CHECK`) ist **ueberholt**. Ingo hat am 2026-09-03 die
Bezeichnung `ZZPRDAT` gewaehlt, damit Auftrag und Objekte denselben Namen tragen wie das
Feld. Gueltig sind die Namen aus Abschnitt 1.

## 4. Die `$TMP`-Testobjekte sind entfernt

Zwei aktive Implementierungen desselben BAdI wuerden beide registrieren, und ein Nachtest
liesse sich dann nicht mehr dem neuen Stand zuordnen. Deshalb am 2026-09-04 geloescht,
**vor** dem Nachtest:

| Objekt | Stand |
|---|---|
| `SXCI Z_ZZPRDAT_AT_RELEASE` | deaktiviert und geloescht |
| `CLAS ZCL_IM__ZZPRDAT_AT_RELEASE` | geloescht |
| `FUNC Z_PP_PRDDAT_SET` | geloescht |
| `PROG ZTESTQQ` | geloescht |
| `FUGR ZPP_ZZPRDAT_TEST` | **leer stehengeblieben** |

Die leere Funktionsgruppe liegt in `$TMP`, enthaelt keinen Baustein mehr und transportiert
nie. Sie ist kein Risiko, nur Restmuell; wer sie aufraeumen will, nimmt in SE37 „Springen →
FGruppenverwaltung → Gruppe loeschen".

## 5. Der Nachtest auf dem ZPP1-Stand

Alle Wege am 2026-09-04 mit **neu angelegten** Auftraegen gemessen. Neue Nummern sind
noetig, weil `Z_ZZPRDAT_SET` nur schreibt, wo `ZZPRDAT` noch initial ist: Auf den alten
Testauftraegen haette der Report auch dann „gesetzt" gemeldet, wenn gar kein Code gelaufen
waere.

| Auftrag | Weg | Vorher | Nachher | Urteil |
|---|---|---|---|---|
| 1241817 | CO01 ohne Freigabe, dann CO02 | `ZZPRDAT` initial, „noch nicht freigegeben" | 02.10.2026 | gesetzt, gleich GLTRP |
| 1241817 | danach Eckendtermin auf 20.10.2026 | 02.10.2026 | 02.10.2026 | eingefroren, GLTRP verschoben |
| 1241818 | CO01 mit Freigabe beim Sichern | — | 05.10.2026 | gesetzt, gleich GLTRP |
| 1241819 | COHV Sammelfreigabe | `ZZPRDAT` initial | 08.10.2026 | gesetzt, gleich GLTRP |
| 1241820 | CO40 aus Planauftrag 2406063 | — | 12.10.2026 | gesetzt, gleich GLTRP |
| 1241821 | **CO01 mit Auftragsart `PP22`** | — | 15.10.2026 | gesetzt, gleich GLTRP |
| 1241822 | **MD04**, Planauftrag 2406064 ueber „-> FertAuftr" | — | 19.10.2026 | gesetzt, gleich GLTRP |
| 1241823 | **CO41** Sammelumsetzung, Planauftrag 2406065 | umgesetzt ohne Freigabe, `ZZPRDAT` initial | — | korrekt: ohne Freigabe kein Datum |
| 1241823 | derselbe Auftrag, danach in CO02 freigegeben | `ZZPRDAT` initial | 22.10.2026 | gesetzt, gleich GLTRP |

**CO41 gibt nicht frei.** Die Sammelumsetzung erzeugt den Fertigungsauftrag und speichert
ihn, loest aber keine Freigabe aus. Der Nachweisreport meldet danach „noch nicht
freigegeben", und das ist richtig so: Ohne Freigabe darf kein Produktionsdatum entstehen.
Es entsteht bei der spaeteren Freigabe, gemessen ueber CO02. Wer das nicht weiss, koennte
den Zwischenstand faelschlich fuer einen Fehler halten.

Auftrag 1241818 ist der aussagekraeftigste Einzelfall: Beim Anlegen mit Freigabe traegt der
Auftrag noch eine temporaere Nummer (`%00000000001`), und nur `BEFORE_UPDATE` sieht die
endgueltige. Diese Methode gab es in der geloeschten `$TMP`-Fassung nicht. Der Treffer ist
damit eindeutig dem neuen Stand in `ZPP1` zuzurechnen.

## 6. Was vor der Freigabe des Auftrags noch offen ist

1. **Anschreiben absenden.** Der Outlook-Entwurf hat Betreff und Anhang, aber weder
   Empfaenger noch Text; die Adressen fehlen in `docs/ANSPRECHPARTNER.md`, und der Text
   liegt als `docs/ZZPRDAT_Mail_Abnahme_2026-09-04.html` daneben.
2. **Fachliche Abnahme** durch Lucas Castro, Florian Waechter und Marco Di Menco anhand von
   `docs/ZZPRDAT_Loesung_2026-09-03.docx`.
3. **Marco prueft Etikett und Typenschild**, also ob beide dasselbe Feld verwenden — und
   seit dem 2026-09-07 zusaetzlich **den Zeitpunkt**: der Verbuchungsbaustein laeuft als V2
   nach der Standardverbuchung, der Auftrag ist also einen Moment lang gespeichert, waehrend
   `ZZPRDAT` noch fehlt. Ob ein unmittelbar angestossener Druck das trifft, ist nicht
   gemessen und laesst sich nur im echten Druckablauf beantworten.

Das Dokument enthaelt seit dem 2026-09-07 das Kapitel **„So testen Sie es selbst"**: drei
Testfaelle (Freigabe setzt das Datum, das Datum bleibt bei Terminverschiebung stehen, die
Wege ohne Bildschirmmaske) und eine Tabelle, welche Beobachtung ein Fehler waere und welche
nicht. Vorher stand dort nur, was **wir** gemessen haben, also kein Weg fuer jemanden, der
es selbst nachvollziehen will. Erzeugt wird das Dokument aus
`saptasks/zzprdat/erzeuge_doku.py`; Aenderungen gehoeren in das Skript, nicht in die
`.docx`.

MD04, CO41 und die Auftragsart `PP22` sind am 2026-09-04 gemessen und stehen in der Tabelle
oben. Die Disposition muss dafuer nicht mehr um Testfaelle gebeten werden; Fabio Palmas
Bitte, bei PP-Aenderungen einbezogen zu werden, gilt weiterhin fuer den Import nach P76.

Die Tabelle oben ist der Stand vom 2026-09-04 und damit **vor** der Korrektur. Gueltig fuer
die Abnahme ist die wiederholte Reihe vom 2026-09-07 im folgenden Abschnitt.

## 6a. Befund vom 2026-09-07: Altauftraege werden ungewollt nachgefuellt

Der Befund stammt aus einer Gegenpruefung durch ein zweites Modell und ist an den Quellen
unter `saptasks/zzprdat/produktiv/` bestaetigt. Er trifft zu.

**Die Wirkungskette.** `BEFORE_UPDATE` ist keine Freigabemethode, sondern laeuft bei
**jedem** Sichern eines Fertigungsauftrags. Sie registriert `Z_ZZPRDAT_SET` fuer jeden Kopf
mit gefuellter Auftragsnummer und gefuelltem `GLTRP`, ohne zu pruefen, ob in diesem Vorgang
ueberhaupt freigegeben wurde. Der Baustein prueft anschliessend nur zwei Dinge: `AFKO-FTRMI`
ist gefuellt, und `AUFK-ZZPRDAT` ist noch leer.

Fuer einen Auftrag, der **irgendwann frueher** freigegeben wurde und dessen `ZZPRDAT` leer
ist, sind beide Bedingungen erfuellt. Beim naechsten beliebigen Sichern bekommt er also den
**heute gueltigen** Eckendtermin eingetragen. Das ist doppelt falsch:

1. Es widerspricht der ausdruecklichen Entscheidung, das Nachfuellen der Altbestaende zu
   einem eigenen, bewusst terminierten Schritt zu machen (Abschnitt 8).
2. Der eingetragene Wert ist nicht der Eckendtermin **zum Zeitpunkt der Freigabe**, sondern
   der heutige. Bei jedem Auftrag, dessen Termin nach der Freigabe verschoben wurde, ist er
   damit fachlich falsch.

**Warum es in den Tests nicht auffiel.** Alle neun Messungen vom 2026-09-04 liefen mit neu
angelegten Auftraegen, bei denen Freigabe und erstes Sichern zusammenfallen. Der Fall
„alter, laengst freigegebener Auftrag wird erneut gesichert" kam darin nicht vor. Die
Testreihe war auf die Frage „wird gesetzt und bleibt stehen" zugeschnitten und hat die
Frage „wird auch dann gesetzt, wenn gar nicht freigegeben wird" nie gestellt.

**Der Nachweisreport verdeckt es zusaetzlich.** Ein so nachgefuellter Altauftrag traegt
`ZZPRDAT` gleich `GLTRP` und erscheint deshalb als „gesetzt, gleich GLTRP", also als
Erfolgsfall.

**Vorschlag zur Behebung.** In `BEFORE_UPDATE` vor dem Registrieren den Zustand **vor**
diesem Sichern lesen: `SELECT SINGLE ftrmi FROM afko WHERE aufnr = lv_aufnr`. Zu diesem
Zeitpunkt steht dort noch der alte Stand, weil die Standardverbuchung erst danach schreibt.

* `FTRMI` ist bereits gefuellt: Der Auftrag war vor diesem Sichern schon freigegeben. Nicht
  registrieren. Genau das ist der Altauftragsfall.
* `FTRMI` ist leer oder es gibt noch keinen Satz: Dieses Sichern kann die Freigabe sein.
  Registrieren; der Baustein prueft anschliessend wie bisher an `FTRMI`, ob sie
  tatsaechlich stattgefunden hat.

Damit ist „Freigabeuebergang" sauber definiert als „vorher leer, nachher gefuellt", und das
Nachfuellen der Altbestaende bleibt der getrennte Schritt, als der er gedacht war.

### Umgesetzt und gemessen am 2026-09-07

Die Pruefung ist eingebaut, die Klasse ist aktiviert, und `E071` weist
`R3TR CLAS ZCL_IM__ZZPRDAT_UPDATE` in der Aufgabe `T76K912491` aus, also im bestehenden
Auftrag. Der Quelltextstand vor der Aenderung liegt als
`saptasks/zzprdat/system/BEFORE_UPDATE_vorher.abap` im Repository und ist der Rueckfallpunkt;
er wurde ueber `CL_OO_CLASSNAME_SERVICE=>GET_METHOD_INCLUDE` und `READ REPORT` auf
`ZCL_IM__ZZPRDAT_UPDATE========CM008` direkt aus dem System gelesen und war inhaltlich
identisch mit der Repository-Kopie.

Drei Messungen, alle ueber RFC gegen `AUFK` gelesen und damit unabhaengig vom Nachweisreport:

| Fall | Vorgehen | `ZZPRDAT` | Urteil |
|---|---|---|---|
| Altauftrag `1241819` | war freigegeben (`FTRMI` 04.09.2026), Feld kuenstlich geleert, dann Eckendtermin auf 26.10.2026 verschoben und gesichert | `00000000` | **bleibt leer, die Korrektur greift** |
| Neuer Auftrag `1241824` | CO01 anlegen und freigeben, Eckendtermin 13.11.2026 | `20261113` | Gegenprobe: die Logik arbeitet weiter |
| derselbe Auftrag | Eckendtermin auf 20.11.2026 verschoben und gesichert | `20261113` | Write-once haelt |

Die mittlere Zeile ist die wichtige Gegenprobe. Ohne sie waere „Feld bleibt leer" auch mit
einem abgeschalteten BAdI erklaerbar gewesen.

**Wie der Altauftragszustand entstanden ist:** Die beiden echten Altauftraege im System
(`9000005594`, `9000005518`, beide freigegeben mit leerem Feld) lassen sich nicht mehr
aendern, SAP meldet „Verändern ist nicht erlaubt". Der Zustand wurde deshalb auf dem eigenen
Testauftrag `1241819` hergestellt, mit dem Wegwerfreport `Z_KOI_ZZPRDAT_CLR` in `$TMP`, der
ausschliesslich Auftragsnummern der eigenen Testreihe akzeptiert. `1241819` traegt seither
kein Produktionsdatum mehr und einen auf den 26.10.2026 verschobenen Eckendtermin; als
Messpunkt der alten Reihe ist er damit verbraucht.

### Die komplette Reihe auf dem korrigierten Stand, 2026-09-07

Alle Werte direkt aus `AUFK` ueber RFC gelesen, nicht ueber `Z_ZZPRDAT_CHECK`. Der Report
haette den Altauftragsfall als Erfolg gemeldet und taugt deshalb hier nicht als Quelle.

| Auftrag | Weg | `ZZPRDAT` | Urteil |
|---|---|---|---|
| 1241819 | Altauftrag, freigegeben, Feld geleert, Eckendtermin verschoben und gesichert | `00000000` | bleibt leer, wie gewollt |
| 1241824 | CO01 anlegen und freigeben | 13.11.2026 | gesetzt |
| 1241824 | danach Eckendtermin auf 20.11.2026 | 13.11.2026 | Write-once haelt |
| 1241825 | CO01 ohne Freigabe, dann CO02 | 27.11.2026 | gesetzt |
| 1241826 | **Auftragsart `PP22`** | 04.12.2026 | gesetzt |
| 1241827 | **COHV Sammelfreigabe** | 09.12.2026 | gesetzt |
| 1241828 | **CO40** aus Planauftrag 2406066 | 15.12.2026 | gesetzt |
| 1241829 | **MD04** aus Planauftrag 2406067 ueber „-> FertAuftr" | 22.12.2026 | gesetzt |
| 1241830 | **CO41** Sammelumsetzung aus Planauftrag 2406068 | `00000000` | korrekt, CO41 gibt nicht frei |
| 1241830 | derselbe Auftrag danach in CO02 freigegeben | 23.12.2026 | gesetzt |

Damit sind alle sieben Wege plus der neue Altauftragsfall plus Write-once auf dem
korrigierten Stand belegt. Zeile 2 ist die Gegenprobe zu Zeile 1: ohne einen Auftrag, der
weiterhin ein Datum bekommt, waere „bleibt leer" auch mit einem abgeschalteten BAdI
erklaerbar.

Das Loesungsdokument fuer den Fachbereich hat seit dem 2026-09-07 ein eigenes Kapitel
„Nachmessung vom 7. September nach einer Korrektur" mit derselben Tabelle und der
Erklaerung, was falsch war und warum es die alte Testreihe nicht zeigen konnte.

**Zwei weitere Punkte aus derselben Pruefung.** Erstens laeuft `Z_ZZPRDAT_SET` als V2 in
einer eigenen Datenbanktransaktion nach der Standardverbuchung. Der Auftrag ist damit kurz
gespeichert, waehrend `ZZPRDAT` noch fehlt. Fuer einen unmittelbar angestossenen Etiketten-
oder Typenschilddruck ist das ein offenes Integrationsrisiko; nachgewiesen ist kein
Druckfehler, gemessen wurde dieser Ablauf aber auch nie. Das gehoert zu Marcos Pruefung
dazu. Zweitens meldete die bisherige Fassung von `Z_ZZPRDAT_CHECK` jedes `ZZPRDAT` ungleich
`GLTRP` als „eingefroren" und zaehlte es unter „Write-once nachgewiesen"; ein schlicht
falsch eingetragenes Datum bekam dasselbe Urteil. Dieser Reportfehler ist inzwischen
behoben, siehe folgenden Abschnitt. Der Nachweis bleibt der Vorher-Nachher-Test.

### Diagnosebericht korrigiert und in T76 aktiviert, 07.09.2026

Auf Ingos Auftrag „ansonst kannst du das fixe wenn nicht behoben“ hat Codex den
verbliebenen Reportfehler behoben. Die Freigabelogik und Auftragsdaten wurden dabei
nicht veraendert.

* Keine Erfolgsbehauptung aus einem Einzelstand: „Datum gleich aktuellem GLTRP“ und
  „Datum weicht von GLTRP ab“ beschreiben nur die beobachteten Werte.
* „Leer; Freigabe vorhanden“ ersetzt die pauschale Fehlermeldung. Altbestaende bleiben
  bewusst leer; auch eine noch ausstehende V2 ist aus diesem Einzelstand nicht erkennbar.
* Gefuellte Daten ohne Freigabenachweis werden separat ausgewiesen.
* Werk- und Leerfilter stehen jetzt in der Datenbankabfrage, **vor** `UP TO p_max ROWS`.
  Zuvor konnte das Limit alle passenden Auftraege abschneiden, bevor der Nachfilter griff.
* `p_max <= 0` wird im Selektionsbild abgewiesen. Der Hinweis zum Anlagedatum stellt klar,
  dass die Datumsgrenze auch bei eingegebener Auftragsnummer gilt.

Nachweise, direkt gegen T76/100:

| Pruefung | Ergebnis |
|---|---|
| Original vor Aenderung aus SAP gelesen | `saptasks/zzprdat/system/Z_ZZPRDAT_CHECK_vorher_20260907.abap`, 155 Zeilen |
| Aktivierung | Nur `Z_ZZPRDAT_CHECK` ausgewaehlt; fremdes `Z_REICHW` blieb unmarkiert |
| Syntaxpruefung des gespeicherten Reports | `Syntax status: OK`, `Error subrc: 0` |
| Aktiven Quelltext zurueckgelesen | `saptasks/zzprdat/system/Z_ZZPRDAT_CHECK_nachher_20260907.abap`, 184 Zeilen; textgleich zur produktiven Repository-Vorlage (Encoding/Zeilenenden normalisiert) |
| Auftraege 1241817 bis 1241830 | 14 Zeilen; 1241817/1241824 als abweichend, 1241819 als leer mit Freigabe, uebrige 11 gleich GLTRP |
| Werk 1100, nur leere Felder, maximal 1 Treffer im selben Bereich | 1241819 gefunden; die alte Reihenfolge haette zuerst 1241830 genommen und danach weggefiltert |
| `p_max = 0` | Fehlermeldung im Selektionsbild, kein unbeschraenkter Lauf |
| Transportzuordnung aus E071 | `T76K912491`, `R3TR PROG Z_ZZPRDAT_CHECK` |

Der wiederholbare lesende GUI-Test liegt in
`saptasks/zzprdat/Pruefe_Report_Selektion.vbs`; er erwartet das Selektionsbild des Reports.
Die Vorpruefung mit `abap-check --source-file` lieferte auch fuer das unveraenderte
Original einen Fehler ohne Details und war deshalb kein verwertbarer Syntaxnachweis.
Massgeblich sind die erfolgreiche Aktivierung, die Syntaxpruefung des gespeicherten
Reports und seine reale Ausfuehrung.

Kein Auftrag angelegt, freigegeben oder geaendert; keine BAdI-/V2-Aenderung, keine
Transportfreigabe und kein P76-Zugriff. Der Drucknachweis aus Abschnitt 6 bleibt offen.

### Live-Pruefung vom 2026-10-26: Loesung unveraendert, fremde Testauftraege

Vor dem verschobenen Abnahmetest am 26.10.2026 in T76/100 gemessen, ausschliesslich lesend
(RFC ueber SapProbe; in SAP nichts angelegt oder geaendert, P76 nicht beruehrt).

**Die Loesung ist seit dem 07.09.2026 unveraendert.**

| Pruefung | Ergebnis |
|---|---|
| Aktiver Quelltext gegen `saptasks/zzprdat/produktiv/` (Gross-/Kleinschreibung und Leerzeilen normalisiert) | `Z_ZZPRDAT_SET` (`LZPP_ZZPRDATU01`), `BEFORE_UPDATE` (`CM008`), `AT_RELEASE` (`CM009`) und `Z_ZZPRDAT_CHECK` identisch. `AT_SAVE` (`CM003`) ist in beiden Fassungen leer; nur der Erklaerkommentar steht allein im Repo |
| Letzte Aenderung laut `TRDIR` | alle ZZPRDAT-Includes spaetestens 07.09.2026, alle durch `KOI` |
| BAdI-Implementierung `Z_ZZPRDAT_UPDATE` (`SXC_ATTR`) | aktiv, Aktivierung 07.09.2026 |
| Transporte (`E070`) | `T76K912490` und `T76K912491` Status `D`, nicht freigegeben |
| Weitere Transporte mit ZZPRDAT-Objekten (`E071`) | keine |

**Nach dem 07.09. haben Fachbenutzer in T76 eigene Fertigungsauftraege angelegt.** Sie stehen
in keiner frueheren Notiz. Werte direkt aus `AUFK`/`AFKO`, Stand 26.10.2026:

| Auftrag | Art | Angelegt von / am | Freigabe (`FTRMI`) | Eckendtermin heute (`GLTRP`) | `ZZPRDAT` |
|---|---|---|---|---|---|
| 1241831 | PP22 | TAN, 08.09. | keine | 24.09.2026 | leer |
| 1241832 | PP22 | TAN, 08.09. | 08.09. | 11.09.2026 | 11.09.2026 |
| 1241840 | PP22 | TAN, 09.09. | 09.09. | 10.09.2026 | 10.09.2026 |
| 1241841 | PP21 | TAN, 10.09. | keine | 24.04.2026 | leer |
| 1241842 | PP22 | TAN, 10.09. | 10.09. | 30.04.2026 | 22.05.2026 |
| 1241843 | PP22 | TAN, 14.09. | 14.09. | 18.09.2026 | 18.09.2026 |
| 1241844 | PP21 | FPA, 23.09. | 23.09. | 12.10.2026 | 12.10.2026 |

Das Bild passt zum Sollverhalten: nicht freigegebene Auftraege bleiben leer, freigegebene
tragen ein Datum. Bei 1241842 weicht das Datum vom heutigen Eckendtermin ab; das ist mit
einer Terminverschiebung nach der Freigabe vereinbar (Write-once). **Nicht geprueft** ist,
ob es wirklich so ablief: Die Aenderungsbelege (`CDHDR`/`CDPOS`) wurden nicht gelesen, und
mit TAN und FPA ist nicht gesprochen worden. Wer die Benutzer sind, ist hier nicht belegt.

Ergebnis des Abnahmetests selbst: noch nicht eingetragen.

### Abnahmesitzung 2026-11-05/06: Reiter zeigt das Datum nicht, Georgs offener Umbau

*Erledigt 2026-11-06 08:11 in T76:* Beim Abnahmetest zeigte der Reiter „TRAFAG Daten“ bei Auftrag 1241847 kein Produktionsdatum, obwohl AUFK-ZZPRDAT 20261111 traegt (alle vier am 05.11. freigegebenen Auftraege korrekt gleich GLTRP). Ursache: In `ZXCO1U11` (EXIT_SAPLCOKO1_001, fuellt das Bildfeld `CI_AUFK-ZZPRDAT`) ist alles auskommentiert, auch die reine Anzeige. Der Include steckte in Georgs (MEEY_GEWA) offenem Auftrag `T76K911783` „PPCO0012 FAUF: Änd neu impl. / Progr #2“ (Mandant 090) mit sechs weiteren Objekten (`ZXCO1O01`, Bild `SAPLXCO1 0100`, `ZXCO1I01`, `ZXCO1U12`, `ZXCO1TOP`, `ZXCO1U06`). Auf Ingos Entscheid mit `RSWBO099` entsperrt (Aufgabe `T76K911784`, Auftrag bleibt bestehen), dann neuer Auftrag **`T76K912851`** nur mit `ZXCO1U11` (Anzeigezeilen `ci_aufk-zzprdat = i_caufvd-zzprdat.` und `gs_aufk-zzprdat = …`). CO03 1241847 zeigt jetzt 11.11.2026, Gegenprobe ohne Datum bleibt leer. Einzelheiten: `saptasks/zzprdat/anzeige_2026-11-05/NOTES.md`.

**Ueberholt damit:** die Aussage in Abschnitt 9, die alte Loesung sei „vollstaendig auskommentiert“ — das gilt nur fuer T76 und nur fuer Georgs offenen Stand, nicht fuer P76.

*Nachtrag 2026-11-06:* `T76K912851` gibt es nicht mehr, er ist in `T76K912490` verschmolzen, und die
sechs uebrigen Objekte liegen jetzt ebenfalls dort (Aufgabe `T76K912852`, Abschnitt 1 Nachtrag
2026-11-06). Damit ist Punkt 1 der folgenden Liste durch die Mitnahme von `ZXCO1TOP` erledigt; Punkt 2
wird zum Entscheid, ob `ZXCO1U06` mitgeht; Punkt 3 heisst jetzt, dass der auskommentierte T76-Stand von
`ZXCO1U12` einen in P76 etwa aktiven Rueckschreibweg ersetzen wuerde.

*Durchspieltest 2026-11-06 (Wunsch Ingo, eigene T76-Sitzung):* CO01 Material 36385, Werk 1100, PP22, 5 ST, Eckendtermin 20.11.2026, mit Freigabe -> Auftrag **1241850**, Status FREI FMAT ABRV; AUFK-ZZPRDAT 20261120 = AFKO-GLTRP, FTRMI 20261106. CO03 Reiter „TRAFAG Daten“ zeigt **20.11.2026** (Gegenprobe 1241847: 11.11.2026). Beobachtung: Beim ersten CO03 direkt nach dem Anlegen in derselben Sitzung war das Feld leer, beim erneuten Oeffnen gefuellt (vermutlich V2-Zeitpunkt bzw. Puffer); im Fachtest den Auftrag neu oeffnen. Schreibweg nicht unterscheidbar, solange `ZXCO1U06` in T76 aktiv ist.

*Erledigt 2026-11-06 in T76: `ZXCO1U12` repariert.* Befund Ingo im Test: Reiter wegklicken und wieder oeffnen -> Produktionsdatum weg. Ursache: `EXIT_SAPLCOKO1_002` (`ZXCO1U12`, Export `E_COCI_AUFK`) war in Georgs Stand komplett auskommentiert und gab beim Verlassen des Reiters ein leeres ZZPRDAT an den Auftrag zurueck; in CO02 waere es beim Sichern geloescht worden (BAdI fuellt nach der Freigabe nicht nach). Fix: `MOVE-CORRESPONDING ci_aufk TO e_coci_aufk.` in Aufgabe `T76K912852` von `T76K912490`, aktiv, zurueckgelesen identisch, Syntax `SAPLXCO1` OK. Test 1241850: CO03 drei Runden Reiterwechsel -> 20.11.2026 bleibt; CO02 drei Runden Reiterwechsel und Sichern -> AUFK-ZZPRDAT bleibt 20261120.

*Testnachweis fuer den Fachbereich 2026-11-06 (Wunsch Ingo):* `saptasks/zzprdat/testdoku_2026-11-06/ZZPRDAT_Testnachweis_2026-11-06.docx` (21 Screenshots, erzeugt mit `erzeuge_testnachweis.py`). Auftrag **1241851** (36385, PP22, 5 ST): vor Freigabe Reiter leer; nach Freigabe/Sichern ZZPRDAT 27.11.2026 = Eckendtermin (gleich beim ersten CO03); Reiterwechsel ok; CO02 Eckendtermin verschoben (11.12. -> SAP legt 14.12.2026, Fabrikkalender CH) und gesichert, ZZPRDAT bleibt 27.11.2026 (Write-once); `Z_ZZPRDAT_CHECK` bestaetigt.

**Vor jeder Freigabe von `T76K912490` offen (P76 lesend pruefen), Stand 2026-11-06 mit den Nachtraegen oben:**
1. `ZXCO1TOP` in P76: Stehen `tables: ... ci_aufk` und `DATA: gs_aufk TYPE ci_aufk` drin? Wenn nicht, wird `SAPLXCO1` nach dem Import syntaxfehlerhaft (CO01/CO02/CO03 brechen ab). Sonst `gs_aufk`-Zeile streichen bzw. TOP mitnehmen.
2. `ZXCO1U06` (EXIT_SAPLCOZV_001, Projekt `ZPP00005`): in T76 aktiv mit Georgs Stand 31.03.2026, schreibt `header_exp-zzprdat = header_imp-gltrp` bei Freigabe. **Zweiter Schreibweg neben dem BAdI** — die T76-Tests unterscheiden nicht, wer schreibt. Stand in P76 und Uebernahme von `HEADER_EXP-ZZPRDAT` klaeren (Georg).
3. `ZXCO1U12` (EXIT_SAPLCOKO1_002, Rueckgabe Bild -> Auftrag) in T76 auskommentiert; in P76 pruefen, ob er den Bildwert beim Sichern zurueckschreibt.
4. Georgs Auftrag `T76K911783` (jetzt ohne Sperren): mit Georg entscheiden.

## 7. Risiko im Produktivsystem und wie man es begrenzt

Nach dem Import wird `BEFORE_UPDATE` bei jedem Sichern aufgerufen, registriert den
Verbuchungsbaustein seit der Korrektur jedoch nur, wenn der bisherige Freigabenachweis
noch fehlt. `AT_RELEASE` registriert weiterhin bei der Freigabe. Zu beachten:

* **Ein Fehler im Baustein reisst die Verbuchung mit.** Genau das ist am 2026-09-03 in T76
  passiert, als versehentlich ein Literal statt einer getypten Variablen uebergeben wurde:
  „Verbuchung wurde abgebrochen". Die jetzige Fassung uebergibt ausschliesslich getypte
  Variablen, und das muss bei jeder kuenftigen Aenderung so bleiben.
* **Rueckfall ohne Transport moeglich.** In SE19 deaktiviert `Strg+F4` die
  BAdI-Implementierung sofort, auch im Produktivsystem. Das ist der Notausschalter, falls
  nach dem Import etwas auffaellt.
* **Nach dem Import kurz beobachten.** SM13 auf abgebrochene Verbuchungen und ST22 auf
  Kurzdumps, und `Z_ZZPRDAT_CHECK` ueber die neu angelegten Auftraege laufen lassen.

## 8. Das Nachfuellen der Altbestaende ist ein eigener Schritt

Es gehoert **nicht** in denselben Transport und **nicht** in denselben Termin. Erst wenn die
Logik produktiv nachweislich greift, wird ein eigener Lauf entschieden, der ausschliesslich
leere `ZZPRDAT`-Felder fuellt und vorhandene Werte niemals ueberschreibt. Grundlage dafuer
ist die fachlich bestaetigte Quelle, nach heutigem Stand `AFKO-GLTRP`.

## 9. Was mit der alten Loesung geschieht

*Ueberholt am 2026-11-06:* `ZXCO1U11` (jetzt mit zwei Anzeigezeilen) und `ZXCO1U12` sind seit dem
2026-11-06 Teil von `T76K912490` (Aufgabe `T76K912852`), zusammen mit `ZXCO1TOP`, `ZXCO1O01`,
`ZXCO1I01`, `ZXCO1U06` und Bild 0100; siehe Abschnitt 1 Nachtrag 2026-11-06. Der folgende Absatz
beschreibt den Stand vom September.

Das Erweiterungsprojekt `ZPP00012` mit den Includes `ZXCO1U11` und `ZXCO1U12` bleibt
unberuehrt; der dortige Code ist vollstaendig auskommentiert und laeuft nicht. Die neue
Loesung liegt an einer anderen Stelle und kollidiert nicht mit ihm. Ob die alten Includes
aufgeraeumt werden, ist eine Entscheidung fuer Georg Wagner und kein Teil dieses
Transports.

## 10. Anhang: der Ist-Zustand vor dem Umbau

Am 2026-09-03 aus dem Objektkatalog `TADIR` in T76/100 gelesen. Diese Tabelle ist Historie
und beschreibt nicht mehr den heutigen Stand:

| Typ | Objekt | Paket |
|---|---|---|
| `SXCI` | `Z_ZZPRDAT_AT_RELEASE` | `$TMP` |
| `CLAS` | `ZCL_IM__ZZPRDAT_AT_RELEASE` | `$TMP` |
| `ENHO` | `Z_ZZPRDAT_REL_TEST` | `$TMP` |
| `FUGR` | `ZPP_ZZPRDAT_TEST` | `$TMP` |
| `PROG` | `ZTESTQQ` | `$TMP` |

`$TMP` heisst „lokales Objekt". Solche Objekte sind nicht „noch nicht" transportfaehig,
sondern grundsaetzlich nicht: Sie erzeugen keinen Transportauftrag und koennen keinem
zugeordnet werden. Fuer den Testaufbau war das richtig, fuer produktiv eine Sackgasse.
