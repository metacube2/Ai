# ZZPRDAT: Weg vom Testaufbau zum Transport

Stand: 2026-09-07. **Der Transport ist angelegt und gefuellt, die Objekte sind neu in `ZPP1`
gebaut und auf allen sieben Wegen nachgetestet. Freigegeben ist der Auftrag nicht** — das
geschieht erst nach der fachlichen Abnahme. Technisch offen ist nichts mehr.

Der fachliche Stand und der Analyseverlauf stehen in `saptasks/zzprdat-kontext.md`, das
Dokument fuer den Fachbereich in `docs/ZZPRDAT_Loesung_2026-09-03.docx`.

## 1. Der Transportauftrag

| | |
|---|---|
| Auftrag | **`T76K912490`** |
| Aufgabe | `T76K912491` (Entwicklung/Korrektur, KOI) |
| Kurztext | ZZPRDAT: Produktionsdatum bei Auftragsfreigabe |
| Typ | Workbench-Auftrag, Ziel `P76` |
| Status | **aenderbar, bewusst nicht freigegeben** |
| Paket | `ZPP1` |

Objektliste, am 2026-09-04 aus SE01 gelesen:

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

1. **Fachliche Abnahme** durch Lucas Castro, Florian Waechter und Marco Di Menco anhand von
   `docs/ZZPRDAT_Loesung_2026-09-03.docx`.
2. **Marco prueft Etikett und Typenschild**, also ob beide dasselbe Feld verwenden.

Das Dokument enthaelt seit dem 2026-09-07 das Kapitel **„So testen Sie es selbst"**: drei
Testfaelle (Freigabe setzt das Datum, das Datum bleibt bei Terminverschiebung stehen, die
Wege ohne Bildschirmmaske) und eine Tabelle, welche Beobachtung ein Fehler waere und welche
nicht. Vorher stand dort nur, was **wir** gemessen haben, also kein Weg fuer jemanden, der
es selbst nachvollziehen will. Erzeugt wird das Dokument aus
`saptasks/zzprdat/erzeuge_doku.py`; Aenderungen gehoeren in das Skript, nicht in die
`.docx`.

Technisch offen ist nichts mehr. MD04, CO41 und die Auftragsart `PP22` sind am 2026-09-04
gemessen und stehen in der Tabelle oben. Die Disposition muss dafuer nicht mehr um
Testfaelle gebeten werden; Fabio Palmas Bitte, bei PP-Aenderungen einbezogen zu werden,
gilt weiterhin fuer den Import nach P76.

## 7. Risiko im Produktivsystem und wie man es begrenzt

Nach dem Import laeuft der Verbuchungsbaustein bei **jedem** Sichern eines
Fertigungsauftrags in P76. Das ist gewollt, verdient aber Aufmerksamkeit:

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
