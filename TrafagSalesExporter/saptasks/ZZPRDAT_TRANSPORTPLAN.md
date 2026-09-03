# ZZPRDAT: Weg vom Testaufbau zum Transport

Stand: 2026-09-03

Diese Datei beantwortet eine Frage: **Was fehlt, damit die Loesung nach P76 kann?**
Der fachliche Stand und der Analyseverlauf stehen in `saptasks/zzprdat-kontext.md`,
das Dokument fuer den Fachbereich in `docs/ZZPRDAT_Loesung_2026-09-03.docx`.

## 1. Der Ist-Zustand: nichts davon ist transportfaehig

Am 2026-09-03 aus dem Objektkatalog `TADIR` in T76/100 gelesen:

| Typ | Objekt | Paket |
|---|---|---|
| `SXCI` | `Z_ZZPRDAT_AT_RELEASE` (BAdI-Implementierung) | `$TMP` |
| `CLAS` | `ZCL_IM__ZZPRDAT_AT_RELEASE` (Implementierungsklasse) | `$TMP` |
| `ENHO` | `Z_ZZPRDAT_REL_TEST` (Erweiterungsimplementierung) | `$TMP` |
| `FUGR` | `ZPP_ZZPRDAT_TEST` (Funktionsgruppe mit `Z_PP_PRDDAT_SET`) | `$TMP` |
| `PROG` | `ZTESTQQ` (Nachweisreport) | `$TMP` |

**`$TMP` heisst „lokales Objekt".** Solche Objekte sind nicht „noch nicht" transportfaehig,
sondern grundsaetzlich nicht: Sie erzeugen keinen Transportauftrag und koennen keinem
zugeordnet werden. Fuer den Testaufbau war das richtig, weil dabei nichts entstehen sollte,
das versehentlich mitwandert. Fuer produktiv ist es eine Sackgasse.

## 2. Das Zielpaket steht fest

Die bestehende PP-Erweiterung liegt im Paket **`ZPP1`**: das Erweiterungsprojekt `ZPP00012`
sowie saemtliche Includes `ZXCO1*`, angelegt von `I001067` und `MEEY_GEWA` (Georg Wagner).
Das ist damit auch das natuerliche Zuhause der neuen Objekte.

## 3. Zwei Wege, und warum der zweite besser ist

### Weg A: Objekte umhaengen

Ueber „Objektverzeichniseintrag aendern" laesst sich jedes Objekt von `$TMP` nach `ZPP1`
umsetzen; SAP fragt dann nach einem Transportauftrag. Schnell, aber es zementiert die
Testnamen.

### Weg B: sauber neu anlegen (empfohlen)

Drei der fuenf Objekte tragen Testnamen, die produktiv nichts zu suchen haben:
`ZPP_ZZPRDAT_TEST`, `Z_ZZPRDAT_REL_TEST` und `ZTESTQQ`. Funktionsgruppen und von SE19
erzeugte Klassen lassen sich **nicht umbenennen**; der Klassenname `ZCL_IM__…` wird aus dem
Namen der BAdI-Implementierung abgeleitet.

Neu anlegen kostet heute wenig, weil Code und Einstellungen bekannt und erprobt sind. Es ist
im Wesentlichen dieselbe Klickfolge wie am 2026-09-03, nur mit anderen Namen und Paket
`ZPP1` statt „Lokales Objekt".

Vorschlag fuer die Namen, abzustimmen mit Lucas Castro und Georg Wagner:

| Bisher | Vorschlag |
|---|---|
| `ZPP_ZZPRDAT_TEST` (Funktionsgruppe) | `ZPP_PRDDAT` |
| `Z_PP_PRDDAT_SET` (Funktionsbaustein) | unveraendert, der Name passt |
| `Z_ZZPRDAT_AT_RELEASE` (BAdI-Implementierung) | `Z_PP_PRDDAT_UPDATE` |
| `Z_ZZPRDAT_REL_TEST` (Erweiterungsimplementierung) | `Z_PP_PRDDAT` |
| `ZTESTQQ` (Nachweisreport) | `Z_PP_PRDDAT_CHECK` |

Der Report gehoert mit in den Transport. Er ist rein lesend und beantwortet im Betrieb die
Frage, ob das Produktionsdatum gesetzt ist und ob es bei Terminverschiebungen stehen bleibt.

## 4. Was in den Transport gehoert

Alles in **einen** Auftrag, weil die Teile nur gemeinsam funktionieren:

1. Funktionsgruppe samt Verbuchungsbaustein. **Verarbeitungsart „Start verzoegert" (V2) muss
   mitkommen**, sonst ueberschreibt der SAP-Standard den Wert wieder. Das ist eine
   Eigenschaft des Bausteins und wandert mit ihm.
2. Erweiterungsimplementierung (`ENHO`) als Container.
3. BAdI-Implementierung (`SXCI`) mit den Methoden `AT_RELEASE` und `BEFORE_UPDATE`.
4. Implementierungsklasse (`CLAS`).
5. Nachweisreport (`PROG`).

Die Quelltexte liegen versioniert unter `saptasks/zzprdat/`:
`Z_PP_PRDDAT_SET_V2.abap`, `AT_RELEASE_ZIEL.abap`, `BEFORE_UPDATE_ZIEL.abap`,
`AT_SAVE_LEER.abap` und `Z_ZZPRDAT_CHECK.abap`.

## 5. Was vor dem Transport erledigt sein muss

Der Transport ist der letzte Schritt, nicht der naechste.

1. **Fachliche Abnahme** durch Lucas Castro, Florian Waechter und Marco Di Menco anhand von
   `docs/ZZPRDAT_Loesung_2026-09-03.docx`.
2. **MD04 und CO41 messen.** Beide enden im selben Speichervorgang wie das bereits
   nachgewiesene CO40, gemessen ist es aber nicht. Mit der Disposition abstimmen; Fabio
   Palma hat ausdruecklich darum gebeten, bei PP-Aenderungen einbezogen zu werden.
3. **Auftragsart `PP22`** pruefen, sie kommt bei 10 der 34 Auftraege aus Marcos Liste vor.
4. **Marco prueft Etikett und Typenschild**, also ob beide dasselbe Feld verwenden.

## 6. Risiko im Produktivsystem und wie man es begrenzt

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
  Kurzdumps, und den Nachweisreport ueber die neu angelegten Auftraege laufen lassen.

## 7. Das Nachfuellen der Altbestaende ist ein eigener Schritt

Es gehoert **nicht** in denselben Transport und **nicht** in denselben Termin. Erst wenn die
Logik produktiv nachweislich greift, wird ein eigener Lauf entschieden, der ausschliesslich
leere `ZZPRDAT`-Felder fuellt und vorhandene Werte niemals ueberschreibt. Grundlage dafuer
ist die fachlich bestaetigte Quelle, nach heutigem Stand `AFKO-GLTRP`.

## 8. Was mit der alten Loesung geschieht

Das Erweiterungsprojekt `ZPP00012` mit den Includes `ZXCO1U11` und `ZXCO1U12` bleibt
unberuehrt; der dortige Code ist vollstaendig auskommentiert und laeuft nicht. Die neue
Loesung liegt an einer anderen Stelle und kollidiert nicht mit ihm. Ob die alten Includes
aufgeraeumt werden, ist eine Entscheidung fuer Georg Wagner und kein Teil dieses
Transports.
