# ZZPRDAT: Weg vom Testaufbau zum Transport

Stand: 2026-09-07. **Der Transport ist angelegt und gefuellt, die Objekte sind neu in `ZPP1`
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

**Zwei kleinere Punkte aus derselben Pruefung.** Erstens laeuft `Z_ZZPRDAT_SET` als V2 in
einer eigenen Datenbanktransaktion nach der Standardverbuchung. Der Auftrag ist damit kurz
gespeichert, waehrend `ZZPRDAT` noch fehlt. Fuer einen unmittelbar angestossenen Etiketten-
oder Typenschilddruck ist das ein offenes Integrationsrisiko; nachgewiesen ist kein
Druckfehler, gemessen wurde dieser Ablauf aber auch nie. Das gehoert zu Marcos Pruefung
dazu. Zweitens meldet `Z_ZZPRDAT_CHECK` jedes `ZZPRDAT` ungleich `GLTRP` als „eingefroren"
und zaehlt es unter „Write-once nachgewiesen"; ein schlicht falsch eingetragenes Datum
bekaeme dasselbe Urteil. Der Report ist ein Diagnosehilfsmittel, der Nachweis ist der
Vorher-Nachher-Test.

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
