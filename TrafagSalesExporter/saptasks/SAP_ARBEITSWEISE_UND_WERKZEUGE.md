# SAP: Arbeitsweise und Werkzeuge

Stand: 2026-09-03

Diese Datei beantwortet eine einzige Frage: **Wie kommt eine neue Sitzung schnell zu
belastbaren SAP-Ergebnissen, ohne dieselben Sackgassen noch einmal zu durchlaufen?**
Sie ist bewusst aufgabenunabhaengig. Der fachliche Stand des ZZPRDAT-Auftrags steht in
`saptasks/zzprdat-kontext.md`.

Alles hier bezieht sich auf **T76, Mandant 100** (`travt762`). P76 ist Produktion und wird
nur nach ausdruecklicher Freigabe angefasst.

## 0. Start einer neuen Sitzung: ein Befehl, dann laeuft alles

**Voraussetzung ist einzig, dass SAP GUI mit einer angemeldeten T76/100-Sitzung offen ist.**
Danach genuegt ein Befehl, den **Ingo selbst ausfuehren muss**, weil er eine
Sicherheitsabfrage abschaltet und deshalb nicht von einem Agenten kommen darf. Aus der
Repository-Wurzel:

```
powershell -ExecutionPolicy Bypass -File .tmp_sap_probe\Set-SapScriptingWarnings.ps1 -Aus
```

Das Skript zeigt vorher und nachher die drei Werte, man sieht also sofort, ob es gewirkt
hat. Der Zielzustand ist `UserScripting 1`, `WarnOnAttach 0`, `WarnOnConnection 0`.

**Ab diesem Moment laeuft die gesamte SAP-Arbeit ohne einen einzigen Klick:** Anlegen und
Freigeben von Fertigungsauftraegen, Aendern von ABAP-Quelltext, Aktivieren von Klassen und
BAdI-Implementierungen, Ausfuehren von Reports und das Auslesen ihrer Listen. Der Agent
steuert die bereits angemeldeten Sitzungen fern.

Am Ende der Arbeit zuruecksetzen:

```
powershell -ExecutionPolicy Bypass -File .tmp_sap_probe\Set-SapScriptingWarnings.ps1 -Ein
```

Drei Stolpersteine, die sonst Zeit kosten:

* **Der Aufruf braucht das richtige Verzeichnis.** Aus `C:\Users\koi` heraus gibt es
  `.tmp_sap_probe` nicht, und PowerShell meldet nur, die Datei existiere nicht. Der Befehl
  hat dann nichts getan.
* **Wirken die Warnungen weiter, SAP Logon samt allen Sitzungen neu starten.** SAP GUI liest
  die Einstellung beim Verbindungsaufbau.
* **Das SAP-Passwort wird fuer diesen Weg nicht gebraucht.** Es gehoert zu SapProbe, das
  eine eigene RFC-Verbindung an der GUI vorbei aufbaut. Fuer alles, was ueber die
  Oberflaeche geht, ist es ueberfluessig. Wer die Struktur eines Objekts braucht, das die
  GUI nicht herausgibt, kommt oft auch mit `ASSIGN COMPONENT` zur Laufzeit ans Ziel; siehe
  Abschnitt 13.

### Wenn `Sessions=0` gemeldet wird: erst den Server pruefen, nicht den Arbeitsplatz

**Am 2026-09-10 zwei Stunden gekostet, deshalb hier ganz vorn.** `SapGuiInspect.vbs` meldete
`Connections=1 ... Sessions=0` — ohne jede Fehlermeldung. Das sieht aus wie ein
Arbeitsplatzproblem und ist keins: die drei Registrywerte standen richtig, Ingo hat sich
zweimal neu angemeldet, ohne Wirkung.

**Ursache war der serverseitige Profilparameter `sapgui/user_scripting`.** Er stand auf
`FALSE`, und dann gibt SAP die Sitzungen nicht an die Scripting-Schnittstelle heraus,
obwohl die Verbindung sichtbar bleibt.

Die Diagnose dauert einen Aufruf:

```powershell
cscript.exe //nologo '.tmp_sap_probe\SapGuiProbeSession.vbs'
```

Die Sonde fragt `DisabledByServer` an der Verbindung ab. Steht dort `Wahr`, ist es
zweifelsfrei der Server; alles Weitere am Arbeitsplatz ist verlorene Zeit. Gegenprobe ueber
RFC, ganz ohne GUI:

```powershell
powershell -ExecutionPolicy Bypass -File .tmp_sap_probe\RunSapProbe.ps1 `
  rfc-call TH_GET_PARAMETER --param "PARAMETER_NAME=sapgui/user_scripting" --quiet
```

**Behebung:** `RZ11`, Parameter `sapgui/user_scripting`, `Wert aendern` auf `TRUE`. Der
Parameter ist dynamisch, wirkt also sofort — aber **nur fuer neu aufgebaute Verbindungen**.
Eine offene Verbindung traegt weiter `DisabledByServer=Wahr`, weil SAP GUI den Wert beim
Verbindungsaufbau liest. Die Verbindung muss geschlossen und neu angemeldet werden; eine
neue Sitzung mit `/o` genuegt nicht.

**Warum es wiederkommt.** RZ11 zeigte am 2026-09-10 alle drei Stufen auf `FALSE`:
Kernel-Default, Default-Profil und Instanz-Profil, resultierende Quelle `Kernel-Default`.
Der Parameter steht also in **keiner** Profildatei. Eine Aenderung ueber RZ11 gilt bis zum
naechsten Neustart der Instanz und faellt dann still auf `FALSE` zurueck. Genau das ist
zwischen dem 2026-09-04 und dem 2026-09-10 passiert, ohne dass am Arbeitsplatz etwas
geaendert wurde. Fuer dauerhaftes Arbeiten gehoert `sapgui/user_scripting = TRUE` ueber
`RZ10` ins Instanzprofil; das ist eine bewusste Sicherheitsentscheidung und Basis-Arbeit.

**Merksatz:** die Registry am Arbeitsplatz und der Profilparameter am Server sind zwei
getrennte Schalter. Beide muessen an sein. Die Registry meldet sich mit einer Warnung oder
`Erlaubnis verweigert`, der Server meldet sich mit **Schweigen**.

### Was ohne GUI-Scripting trotzdem geht

Der RFC-Weg ueber SapProbe ist davon voellig unberuehrt und braucht nur die
Passwortablage. Am 2026-09-10 liess sich damit die komplette Vorpruefung fuer das
CH/AT-Journal erledigen: `field-exists` fuer 25 DDIC-Felder, `table-read` mit `--fields`,
`--where` und `--rowcount` fuer Buchungskreise und Stichproben, `abap-read` fuer den
Quelltext von `Z_TRAFAG_DACH_EXPORT` und `rfc-call` fuer Profilparameter und Serverliste.

**Falle dabei:** `table-read` ohne `--fields` und `--where` liefert eine **abgeschnittene**
Liste ohne Hinweis darauf. `T001` sah so aus, als gaebe es nur zehn Buchungskreise und
keinen oesterreichischen; mit `--where "BUKRS IN ('1100','1200')"` stand `1200` sofort da.
Immer filtern, nie aus einer ungefilterten Liste auf Nichtexistenz schliessen.

**Zweite Falle:** `abap-write` scheitert an `RPY_PROGRAM_INSERT` mit `PERMISSION_ERROR`.
Der RFC-Benutzer darf keine Programme anlegen. Reports anlegen geht nur ueber die GUI.

## 1. Die drei Zugangswege und wann welcher taugt

Es gibt drei Wege ins System. Sie unterscheiden sich vor allem darin, was sie **kosten**,
nicht darin, was sie koennen.

| Weg | Kann | Kostet | Wann nehmen |
|---|---|---|---|
| **ABAP-Report, den Ingo ausfuehrt** | Lesen und Rechnen ueber beliebige Tabellen, sauber formatierte Beweisausgabe | einmal Code schreiben, dann ein Klick | **Erste Wahl fuer jede Auswertung.** |
| **SAP-GUI-Scripting** (`.tmp_sap_probe/*.vbs`) | Navigieren, Felder fuellen, Knoepfe druecken, Objekte anlegen und aktivieren | kein Passwort, aber viele Rundreisen und einige harte Grenzen | Fuer **Schreiboperationen** in SE19/SE24/SE37. |
| **SapProbe ueber RFC** (`.tmp_sap_probe/bin/.../SapProbe.exe`) | Tabellen lesen, Funktionsbausteine, ABAP lesen und syntaxpruefen | **jedes Mal eine Passworteingabe durch Ingo** | Nur wenn kein Report moeglich ist. |

Dazu kommt ein vierter, der in der Praxis der schnellste ueberhaupt ist:

> **Ingo einen Screenshot schicken lassen.** Er kann Snips einfuegen, und das Modell kann
> Bilder lesen. Fuer alles, was am Bildschirm steht und ueber die Scripting-Schnittstelle
> nicht herauskommt, ist das die guenstigste Loesung. Am 2026-09-03 hat genau das die
> Signatur von `AT_RELEASE` und die Fehlerliste der Syntaxpruefung geliefert, nachdem
> mehrere Skriptversuche daran gescheitert waren.

**Merksatz aus dem Lauf vom 2026-09-03:** Je weiter die Arbeit fortschritt, desto besser
wurde das Verhaeltnis von Aufwand zu Ergebnis, weil der Schwerpunkt von GUI-Fernsteuerung
zu *Report schreiben, Ingo fuehrt aus, Ausgabe auswerten* wanderte. Diese Reihenfolge
gleich zu Beginn zu waehlen spart die meiste Zeit.

## 2. Harte Grenzen des GUI-Scriptings

Diese Punkte sind gemessen, nicht vermutet. Sie kosten sonst jedes Mal mehrere Versuche.

* **Der ABAP-Editor laesst sich nicht auslesen.** `shell.Text` liefert nur den Namen des
  Steuerelements (`SAPGUI.AbapEditor.1`). `GetUnprotectedTextPart`,
  `NumberOfUnprotectedTextParts` und `SelectedText` gibt es auf diesem Control nicht.
  Schreiben geht, Lesen nicht. Wer den Quelltext sehen muss, laesst sich einen Screenshot
  geben.
* **`SelectAll` plus `ReplaceSelection` ersetzt den *gesamten* Puffer**, einschliesslich
  der Rahmenzeilen. Der einzufuegende Text muss deshalb `METHOD ... ENDMETHOD`
  beziehungsweise `FUNCTION ... ENDFUNCTION` **enthalten**. Fehlen sie, meldet SAP vier
  Fehler der Art „Zwischen CLASS ... IMPLEMENTATION und ENDCLASS duerfen nur Methoden
  definiert werden" — eine Meldung, die man leicht falsch als Feldproblem deutet.
* **`DoubleClickCurrentCell` gibt es auf `GuiTableControl` hier nicht.** Stattdessen die
  Zelle fokussieren und `SendVKey 2` senden.
* **Statusmeldungen sind unzuverlaessig.** Nach einer Syntaxpruefung im Class Builder
  bleiben `sbar.MessageType` und `sbar/pane[0]` leer, obwohl eine Fehlerliste am Bildschirm
  steht. Verlass ist nur auf **Zustandsfelder**, etwa `RSEXSCRN-ACTIVE` in SE19 oder
  `DY0200_STATUS` im Class Builder.
* **Scripting muss lokal eingeschaltet sein.** Wie, steht in Abschnitt 0. Bleiben
  `WarnOnAttach` und `WarnOnConnection` auf `1`, wird der erste Zugriff nach einer Pause
  mit „Erlaubnis verweigert: 'a.GetScriptingEngine'" abgewiesen; ein Wiederholen desselben
  Aufrufs genuegt dann. Genau das macht unbeaufsichtigtes Arbeiten unmoeglich und ist der
  Grund fuer den Freischaltbefehl.
* **Eine geschlossene Sitzung macht gehaltene Referenzen ungueltig.** Nach `/i` meldet der
  naechste Zugriff „Das aufgerufene Objekt wurde von den Clients getrennt". Der Aufruf muss
  dann einfach wiederholt werden, dann wird die COM-Verbindung neu aufgebaut.
* **Nach dem Aktivieren einer Klasse brechen laufende Sitzungen ab.** Wer eine Transaktion
  offen hat, die die alte Fassung geladen hat, bekommt beim naechsten Schritt
  `LOAD_PROGRAM_CLASS_MISMATCH`. Die Sitzung mit `/i` schliessen und neu oeffnen; nur ein
  frischer interner Modus laedt die neue Version.

## 3. Fallen beim Aktivieren

### Aktivieren aus SE38: der Dialog „Fehler beim Aktivieren" kommt auch bei WARNUNGEN

Am 2026-09-10 gemessen. Der Dialog heisst „Fehler beim Aktivieren", enthaelt aber einen
HTML-Bereich und drei Knoepfe `Aktivieren`, `Bearbeiten`, `Abbrechen`. Er erscheint
**auch dann, wenn es nur Warnungen sind**. Der HTML-Inhalt ist ueber die
Scripting-Schnittstelle nicht lesbar.

Erkennen, was wirklich vorliegt: `Aktivieren` druecken und danach den Programmstatus
messen. Steht `wnd[0]/usr/txtSTATUS_TEXT` auf `aktiv`, waren es Warnungen. Bei echten
Syntaxfehlern aktiviert SAP nicht.

**Der Knopf heisst in SE38 `tbar[1]/btn[27]`, nicht `btn[21]`.** `btn[21]` gibt es dort
nicht; der Aufruf scheitert mit „The control could not be found by id".

### `abap-check --source-file` ist unbrauchbar, `abap-check` ohne Datei prueft die AKTIVE Version

Zwei Fallen an derselben Stelle, beide am 2026-09-10 gemessen:

* **Ohne `--source-file`** prueft der Befehl die **aktive** Version aus dem Repository.
  Solange die aktive Version ein leerer Rumpf ist und der neue Quelltext nur inaktiv
  vorliegt, meldet er `Syntax status OK`, obwohl der Quelltext Fehler hat. Das hat eine
  Stunde gekostet.
* **Mit `--source-file`** meldet er `ERROR subrc 4` ohne jede Detailangabe, und zwar
  **auch fuer einen trivialen, fehlerfreien Dreizeiler**. Der Befund ist damit wertlos.
  Kontrollprobe immer mit einem minimalen Report machen, bevor man auf eine Fehlermeldung
  hin den eigenen Quelltext umbaut.

**Der zuverlaessige Weg zur Fehlermeldung:** Report ausfuehren, `ST22` oeffnen, `Heute`
druecken, das ALV-Grid mit `SapGuiGridLesen.vbs` auslesen, den eigenen Dump ueber
`UNAME` und Uhrzeit finden, Zeile markieren und mit `SendVKey 2` oeffnen. Der Langtext
nennt Include, **Zeilennummer** und Fehlertext im Klartext. So wurden am 2026-09-10 zwei
Fehler gefunden, die vorher nicht sichtbar waren:

| Fehler | Ursache |
| --- | --- |
| `"TIT01" was already declared.` | `SELECTION-SCREEN ... WITH FRAME TITLE tit01` deklariert die Titelvariable **selbst**. Eine eigene `DATA`-Deklaration dafuer kollidiert. Titel stattdessen in `INITIALIZATION` fuellen. |
| Syntaxfehler in `WRITE` | `WRITE: / 'Text', lv_a + lv_b.` — **`WRITE` vertraegt keinen Rechenausdruck.** Vorher in eine Variable summieren. |

Beide Fehler brechen erst zur **Laufzeit** ab, beim Aufbau des Selektionsbildes durch
`SAPLALDB`, nicht beim Aktivieren. Ein Report kann also aktiv sein und trotzdem beim
ersten Start dumpen.

### SE11: DDIC-Struktur per Scripting anlegen

Am 2026-09-10 fuer `ZSTR_FIN_JOURNAL` gemacht. Ablauf, der funktioniert:

1. `/nSE11`, Radiobutton `RSRD1-DDTYPE` waehlen, Namen in `RSRD1-DDTYPE_VAL`, **F5**.
2. Im Dialog „Typ ... anlegen" `radD_100-STRU` waehlen, `tbar[0]/btn[0]`.
3. **Kurzbeschreibung `DD02D-DDTEXT` zuerst setzen**, sonst bricht das Sichern ab.
4. Felder eintragen: `SapGuiStrukturFelder.vbs <SitzungsIndex> <Datei>`, Datei je Zeile
   `FELDNAME;KOMPONENTENTYP`.
5. Aktivieren mit `tbar[1]/btn[27]`, Paket zuordnen, Auftrag bestaetigen.

**Vorher jedes Datenelement pruefen**, nicht raten:

```powershell
powershell -ExecutionPolicy Bypass -File .tmp_sap_probe\RunSapProbe.ps1 `
  table-read DD04L --fields ROLLNAME --where "ROLLNAME = 'NETDT'" --rowcount 2 --quiet
```

Am 2026-09-10 gab es `FAEDE` nicht; die Suche ueber `DD04T` mit
`ROLLNAME LIKE 'FAED%'` lieferte nur FI-CA-Elemente. Richtig fuer das klassische
Nettofaelligkeitsdatum ist **`NETDT`**.

#### Waehrungsfelder brauchen eine Referenz, sonst scheitert die Aktivierung

Fehlermeldung im Aktivierungsprotokoll:

```
ZSTR_FIN_JOURNAL-DMBTR (Bitte Referenz-Tabelle UND Referenz-Feld angeben)
```

Jedes `CURR`-Feld braucht auf dem Reiter **`tabpREFF`** („Waehrungs-/Mengenfelder")
Referenztabelle und Referenzfeld auf ein `CUKY`-Feld. Bei einer Struktur zeigt die
Referenz auf die **Struktur selbst**, etwa `DMBTR -> ZSTR_FIN_JOURNAL-HWAER` und
`WRBTR -> ZSTR_FIN_JOURNAL-WAERS`. Dafuer gibt es `SapGuiWaehrungsref.vbs`.

**Die Element-Ids dort raten nicht.** Gemessen am 2026-09-10:

| | |
| --- | --- |
| Subscreen | `ssubTS_SCREEN:SAPLSD41:2103`, nicht `2303` |
| Reitername | `tabpREFF`, nicht `tabpENTR` |
| Referenztabelle | `txtDD03P_D-REFTABLE[4,r]` |
| Referenzfeld | `txtDD03P_D-REFFIELD[5,r]` |

Immer erst `SapGuiDumpSession.vbs` auf den Reiter und die Spaltenindizes ablesen.

#### Das Aktivierungsprotokoll ist lesbar, anders als der Editor

Bei SE11 erscheint nach einem Fehler ein Dialog „Bei der Aktivierung sind Fehler
aufgetreten -> siehe Protokoll". Der Knopf `wnd[1]/tbar[0]/btn[0]` oeffnet es, und
`Get-SapList.ps1` liest es im Klartext. **Das ist der Unterschied zu SE38**, wo der
Fehler nur ueber den ST22-Kurzdump herauskommt.

#### VBScript liest Dateien mit LF nicht

`Split(txt.ReadAll, vbCrLf)` liefert bei einer Datei mit reinen LF-Zeilenenden **ein
einziges Element**, und die Schleife schreibt still nichts. Erst
`Replace(inhalt, vbCrLf, vbLf)` und dann auf `vbLf` splitten. Im Repository erzeugte
Hilfsdateien haben oft LF.

### SEGW per Scripting: der Projektbaum ist ein `SAP.TableTreeControl`

Am 2026-09-10 erschlossen. SEGW hat keine flachen Dynpro-Elemente, sondern eine
verschachtelte Splitter-Oberflaeche. Ein `SapGuiDumpSession.vbs` zeigt fast nur
`GuiContainerShell` und `GuiSplitterShell`; die Nutzlast steckt in zwei Shells:

| Shell | Bedeutung |
| --- | --- |
| `SAP.TableTreeControl.1` | der Projektbaum |
| `SAP.Toolbar.1` | die Werkzeugleiste darueber |

Die Baum-Id auf T76 lautet:

```
wnd[0]/usr/shellcont/shell/shellcont[0]/shellcont/shell/shellcont[1]/shell
```

Sie ist lang und positionsabhaengig, also **nicht** hart verdrahten, sondern aus dem
Dump ueber den Text `SAP.TableTreeControl.1` holen.

**`SapGuiBaumLesen.vbs` kann diesen Typ nicht** und stirbt mit „Bad index type for
collection access". Dafuer gibt es `SapGuiTableTree.vbs`, das ueber
`GetAllNodeKeys`, `GetNodeTextByKey` und `ExpandNode` arbeitet.

**Die Knotenschluessel sind rechtsbuendig aufgefuellte Zeichenketten**, nicht Zahlen:
der Projektknoten heisst `"          1"` mit fuehrenden Leerzeichen. Beim Aufklappen
exakt so uebergeben, sonst passiert nichts.

Aufgeklappt zeigt das Projekt vier Aeste: `Data Model`, `Serviceimplementierung`,
`Laufzeitartefakte`, `Serviceverwaltung`.

### Class Builder SE24 per Scripting: was geht und wo es hakt

Am 2026-09-10 erschlossen und am 2026-09-11 **zu Ende gebracht**: beide
Redefinitionen fuer das Journal-EntitySet sind so gesetzt, geschrieben und
aktiviert worden, ohne dass jemand von Hand klicken musste.

#### Klasse oeffnen

```
/nSE24, Feld ctxtSEOCLASS-CLSNAME setzen, dann F6 (Aendern), nicht Enter.
```

Reiter der Klassenpflege: `tabsCTS/tabpTAB_CLSD` Eigenschaften, `tabpTAB_IREL`
Interfaces, `tabpTAB_ATT` Attribute, **`tabpTAB_MTD` Methoden**, `tabpTAB_EVT`
Ereignisse, `tabpTAB_TYP` Typen, `tabpTAB_ALI` Aliasse.

Die Methodentabelle liegt unter
`tabpTAB_MTD/ssubCSS:SAPLSEOD:0253/tblSAPLSEODMC`, Spalte 0 ist
`txtDY_0253-CPDNAME`.

#### Der Redefinieren-Knopf heisst `btnPUSH_REDEFINE`

Er steht **nicht** in einer Werkzeugleiste, sondern als Druckknopf im Subscreen
der Methoden:

```
tabpTAB_MTD/ssubCSS:SAPLSEOD:0253/btnPUSH_REDEFINE
```

Daneben liegen `PUSH_PARAMETERS`, `PUSH_EXCEPTIONS`, `PUSH_EDITOR` (Quelltext),
`PUSH_DETAIL`, `PUSH_NEWLINE`, `PUSH_DELLINE`, `PUSH_SORT`, `PUSH_FIND`,
`PUSH_NEXT` und `PUSH_UNDO_REDEFINITION`. In den Menues gibt es **kein**
Redefinieren; `Springen > menu[7]/menu[1]` heisst zwar „Redefinitionen", ist
aber nur eine Anzeige.

#### Die eigentliche Falle: man traegt den Namen gar nicht ein

Der naheliegende Weg ist, den Methodennamen in eine freie Zeile zu schreiben und
dann `PUSH_REDEFINE` zu druecken. Das ist falsch, und SAP meldet dann:

```
Es wird bereits ein(e) Komponente DEFINE von Klasse /IWBEP/CL_MGW_ABS_MODEL geerbt
```

Diese Meldung liest sich wie „geht nicht", heisst aber woertlich, was sie sagt:
die Komponente ist bereits geerbt, also steht sie **schon in der Tabelle**. Der
richtige Weg ist deshalb:

> Die geerbte Zeile in der Methodentabelle suchen, den Cursor mit `SetFocus` auf
> ihr Namensfeld setzen und `PUSH_REDEFINE` druecken. Nichts eintippen.

Am 2026-09-10 hat das zwei Anlaeufe gekostet, weil die Fehlermeldung als
Bedienfehler beim Tippen gedeutet wurde statt als Hinweis auf die vorhandene
Zeile. Die Folgemeldung „The method got an invalid argument" kam nur daher, dass
danach in eine Zeile geschrieben wurde, die gar keine Eingabezeile ist.

Zwei Dinge, an denen die Suche scheitert, wenn man sie nicht bedenkt:

* **Die Tabelle ist laenger als das Fenster.** Im `MPC_EXT` stehen 36 Methoden
  bei 25 sichtbaren Zeilen, im `DPC_EXT` sind es **398**. Die gesuchte Zeile lag
  dort auf Position 47, also erst auf der zweiten Seite. Wer nur die sichtbare
  Seite absucht, findet sie nicht und legt dann faelschlich eine neue Methode an.
* **Interfacemethoden heissen mit vollem Namen**, hier
  `/IWBEP/IF_MGW_APPL_SRV_RUNTIME~GET_ENTITYSET`. Unter `GET_ENTITYSET` allein
  findet man nichts.

Skript dafuer: `SapGuiRedefinierenZeile.vbs` (Sitzungsindex, Methodenname). Es
blaettert durch die ganze Tabelle, raeumt Reste eines frueheren Fehlversuchs aus
freien Zeilen und drueckt den Knopf im selben COM-Prozess.

#### Nach dem Redefinieren steht man schon im Methodeneditor

Der Class Builder fragt zuerst „Aenderungen sichern?" und danach nach dem
Transportauftrag, und springt anschliessend **von selbst** in den Editor der
neuen Methode. Die Methodentabelle ist dann nicht mehr am Bildschirm.

Ein Skript, das jetzt erst die Tabelle sucht, bricht mit „Methodentabelle nicht
gefunden" ab, obwohl alles in Ordnung ist. Deshalb zwei getrennte Skripte:

| Lage | Skript |
| --- | --- |
| Klasse offen, Reiter Methoden, Editor noch zu | `SapGuiKlassenMethodeQuelltext.vbs <Sitzung> <Methode> <Datei>` |
| Editor bereits offen (direkt nach dem Redefinieren) | `SapGuiMethodeneditorSchreiben.vbs <Sitzung> <Methode> <Datei>` |

Beide ersetzen den Puffer per `SelectAll` plus `ReplaceSelection`, pruefen vorher
`txtDY0200_CPDNAME` gegen den erwarteten Methodennamen und sichern mit
`tbar[0]/btn[11]`.

#### Aktivieren: der Dialog kommt auf dem falschen Reiter hoch

Aktiviert wird die **ganze** Klasse ueber das SE24-Einstiegsbild: Klassenname
eintragen, `tbar[1]/btn[27]` (`Strg+F3`). Dann erscheint „Inaktive Objekte von
KOI" — und zwar auf dem Reiter **„Lokale Objekte"**, wo die Tabelle leer ist.
Die eigenen Objekte liegen unter „Transportierbare Objekte"
(`tabsACT_TAB_STRIP/tabpTRANSPORT`, Subscreen `0201`).

`SapGuiWorklistSelect.vbs` stellt den Reiter sogar wieder auf „Lokal" zurueck und
meldet dann `ZEILEN_GESAMT=0`. Das ist **kein** Beleg dafuer, dass nichts zu
aktivieren waere. Neues Skript: `SapGuiInaktiveMarkieren.vbs <Sitzung>
<pruefen|aktivieren> <Name> [...]`. Es setzt den Reiter selbst, drueckt zuerst
„Alles entmarkieren" (`tbar[0]/btn[21]`), blaettert durch die ganze Liste und
markiert nur ueber Namensabgleich. Leerzeilen kommen dabei als **Reihe von
Unterstrichen** zurueck, nicht als Leerstring; ungefiltert werden sie als fremde
Objekte gezaehlt. Der Modus `pruefen` zeigt die Auswahl, ohne zu aktivieren.

Bei einer Aktivierung aus dem Einstiegsbild heraus enthaelt der Arbeitsvorrat nur
die Objekte **dieser** Klasse. Fuer zwei Klassen also zweimal einsteigen.

#### Aus der Bash-Shell heraus werden Transaktionscodes verstuemmelt

Wird ein Skript aus der Git-Bash gestartet, wandelt MSYS jedes Argument, das wie
ein Unix-Pfad aussieht, in einen Windows-Pfad um. Aus `/nSE24` wird dabei
`C:/Program Files/Git/nSE24`, und SAP meldet:

```
Der Funktionscode C:/P ist nicht unterstuetzt
```

Das sieht nach einem SAP-Problem aus und ist keines. Auch Element-Ids wie
`/app/con[0]/...` und OData-Pfade sind betroffen. Abhilfe am Anfang jedes
Bash-Aufrufs:

```bash
export MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*'
```

Bei OData-URIs zusaetzlich `$` maskieren (`\$filter`, `\$top`), sonst ersetzt die
Shell sie durch leere Variablen. Aus PowerShell heraus tritt beides nicht auf.

#### OData pruefen, ohne sich anzumelden: `/IWFND/GW_CLIENT`

OData von aussen scheitert an der Anmeldung. Basic-Auth liefert `401`
(2026-08-18, mit von Ingo uebergebenem Passwort), und
`Invoke-WebRequest -UseDefaultCredentials` ebenfalls `401` (2026-09-11). Weitere
Anmeldeversuche sind wegen des Sperrrisikos zu unterlassen.

**`/IWFND/GW_CLIENT` laeuft innerhalb der bestehenden SAP-Sitzung** und braucht
deshalb gar keine zweite Anmeldung. Damit ist jede Gegenprobe an einem Service
ohne Passwort moeglich:

```
Request-URI: wnd[0]/usr/cntlURI_AREA/shellcont/shell   (GuiShell, SubType TextEdit)
             .Text = "/sap/opu/odata/sap/<SRV>/<Set>?$top=1&$format=json"
             SelectAll/ReplaceSelection gibt es hier NICHT, direkt .Text setzen
Ausfuehren:  F8 oder tbar[1]/btn[8]
Statuscode:  cntlGUI_AREA/shellcont/shell/shellcont[1]/shell  (GridView)
             Zeile mit NAME = "~status_code", dazu "~status_reason", "content-length"
```

**Was nicht geht:** der Antwortrumpf. Er steht in einem `AbapEditor`- und einem
`HTMLControl`-Bereich, und beide geben ueber `.Text` nur ihren Steuerelementnamen
zurueck. Der Menuepunkt „SAP Gateway Client > Auf PC herunterladen" hilft nicht,
er sichert den **Testfall** (Request), nicht die Antwort. Fuer den Inhalt bleibt
der Screenshot oder der Browser mit angemeldeter Sitzung.

Statuscode und `content-length` reichen aber fuer die wichtigsten Aussagen: ob
das Set existiert, ob es Daten liefert und ob die uebrigen Sets nach einer
Aenderung am generischen Dispatcher noch laufen.

#### Gateway-Fehler stehen in `/IWFND/ERROR_LOG`

Ein `HTTP 500` sagt fuer sich nichts. Der Klartext steht in
`/IWFND/ERROR_LOG`, im unteren Grid
(`cntlGUI_AREA/shellcont/shell/shellcont[1]/shell`) unter `..ERROR_INFO`. Am
2026-09-11 stand dort in einem Satz, was eine lange Fehlersuche gewesen waere:
„Eigenschaft (externer Name) 'Bukrs' fuer Entitaet 'FinanzJournal' nicht
gefunden."

#### Gateway-Modell im Code: die generierte Schwestermethode ist die Vorlage

`bind_structure( )` **legt keine Properties an.** Es verknuepft nur bereits
vorhandene Properties mit den Feldern einer ABAP-Struktur. Wer sich darauf
verlaesst und danach `get_property( )` ruft, bekommt `HTTP 500`.

Richtig ist, jede Property einzeln anzulegen und erst danach zu binden:

```abap
lo_property = lo_entity_type->create_property(
                iv_property_name  = 'Bukrs'
                iv_abap_fieldname = 'BUKRS' ).
lo_property->set_is_key( ).
lo_property->set_type_edm_string( ).
lo_property->set_maxlength( iv_max_length = 4 ).
...
lo_entity_type->bind_structure( iv_structure_name   = 'ZSTR_FIN_JOURNAL'
                                iv_bind_conversions = 'X' ).
```

**Die Vorlage nicht erinnern, sondern lesen.** Die generierte `*_MPC`-Klasse
desselben Service enthaelt je Entity eine Methode `DEFINE_<NAME>`, und die zeigt
die im System gueltige Schreibweise. Ueber RFC:

```powershell
powershell -ExecutionPolicy Bypass -File .tmp_sap_probe\RunSapProbe.ps1 `
  abap-read ZCL_ZPOWERBI_EINKAUF_MPC======CM01M --quiet   # DEFINE_FINANZDATASCHWEIZOE
```

Dort steht auch die Umrechnung der DDIC-Typen, die man sonst raten wuerde:

| DDIC | OData |
| --- | --- |
| `CHAR`, `NUMC`, `CUKY` | `set_type_edm_string( )` + `set_maxlength( Laenge )` |
| `DATS` | `set_type_edm_datetime( )` + `set_precison( 7 )` |
| `CURR` | `set_type_edm_decimal( )` + `set_precison( 3 )` + `set_maxlength( Laenge + 1 )` |

Das „+1" und die feste 3 bei `CURR` sind eine Eigenheit von SEGW, belegt an
`NETWR_DC` (`CURR 15,2` -> `3/16`) und `WAVWR_DC` (`CURR 13,2` -> `3/14`).
`QUAN` bekommt dagegen kein „+1". Die Methode heisst wirklich `set_precison`,
mit fehlendem `i`.

#### `set_sortable`, `set_filterable` und `set_pageable` sind reine Metadaten

In `ZPOWERBI_EINKAUF_SRV` steht auf `EKKOSet` das Feld `Ebeln` auf
`set_sortable( abap_false )` **und** `set_filterable( abap_false )`, das Set
selbst auf `set_pageable( abap_false )`. Der produktive Einkaufslader fragt es
trotzdem mit `$orderby=Ebeln`, `$filter` und `$top`/`$skip` ab, seit Monaten
ohne Fehler.

**Das Gateway weist solche Anfragen also nicht ab.** Es reicht die Option nur
nicht an den Data Provider durch. Daher kommt der seit 2026-08-18 notierte
Satz, `MARA001Set` und `mbewSet` wuerden `$top`, `$skip` und `$orderby`
„ignorieren": sie bekommen die Optionen gar nicht erst zu sehen.

Zwei praktische Folgerungen:

* Ein fehlendes Kennzeichen bricht **nichts**, es macht die Option nur wirkungslos.
  Wer also fuerchtet, ein `$orderby` laufe auf `400`, kann sich das sparen.
* Wer Paging oder Filter wirklich braucht, muss beides tun: das Kennzeichen
  setzen **und** `is_paging` beziehungsweise `it_filter_select_options` im Data
  Provider auswerten. Nur das Kennzeichen zu setzen bringt nichts, und nur
  auszuwerten auch nicht, weil dann nichts ankommt.

#### Wichtig: ABAP aus Klassen IST lesbar, nur nicht ueber den Editor

Die Aussage „der ABAP-Editor laesst sich nicht auslesen" gilt fuer das
**GUI-Steuerelement**. Ueber RFC geht es sehr wohl, auch fuer Klassen:

```powershell
# Klassenpool: Name auf 30 Zeichen mit = auffuellen, dann CP
powershell -ExecutionPolicy Bypass -File .tmp_sap_probe\RunSapProbe.ps1 `
  abap-read ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CP --quiet

# Methodenimplementierungen: dasselbe Schema mit CM001, CM002, CM00A ...
# Deklarationen: CU (public), CO (protected), CI (private)
powershell -ExecutionPolicy Bypass -File .tmp_sap_probe\RunSapProbe.ps1 `
  abap-read ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM002 --quiet
```

So wurde am 2026-09-10 die Signatur von
`/IWBEP/IF_MGW_APPL_SRV_RUNTIME~GET_ENTITYSET` aus dem Include
`/IWBEP/IF_MGW_APPL_SRV_RUNTIMEiu` gelesen statt geraten, und es liessen sich
bestehende Implementierungen wie `MARA001SET_GET_ENTITYSET` als Vorlage ansehen.

**Damit ist Gegenlesen moeglich**, was bei Klassenaenderungen der entscheidende
Unterschied ist: schreiben ueber die GUI, pruefen ueber RFC.

### Mehrere Sitzungen mit demselben Transaktionscode

Die Skripte suchen die Sitzung ueber den Transaktionscode. Wird `/nSE38` in eine zweite
Sitzung geschickt, tragen **beide** denselben Code, und Klicks landen in der falschen.
Am 2026-09-10 passiert, nachdem ST22 in `ses[1]` geoeffnet und spaeter mit `/nSE38`
umgeparkt worden war: die Aktivierung meldete beharrlich „Kein Dialog offen", waehrend
der Dialog in `ses[0]` stand.

`SapGuiAktivieren.vbs` nimmt deshalb den **Sitzungsindex** als erstes Argument statt des
Transaktionscodes. Bei Zweifeln vorher `SapGuiInspect.vbs` laufen lassen und den Index
ablesen.

### `RFC_READ_TABLE` schneidet still ab und scheitert an breiten Tabellen

`table-read` ohne `--fields` und `--where` liefert eine **abgeschnittene** Liste ohne
Hinweis. `T001` sah so aus, als gaebe es nur zehn Buchungskreise und keinen
oesterreichischen; mit `--where "BUKRS IN ('1100','1200')"` stand `1200` sofort da.
Nie aus einer ungefilterten Liste auf Nichtexistenz schliessen.

Bei sehr breiten Tabellen wie `BKPF` und `BSEG` scheitert der Aufruf ganz, weil
`RFC_READ_TABLE` eine Zeilenbreite von 512 Byte nicht ueberschreiten darf. Mit
`--fields` auf die gebrauchten Spalten begrenzen, dann geht es.

### `abap-write` legt nur an, es ersetzt nicht

`RPY_PROGRAM_INSERT` meldet `ALREADY_EXISTS`, sobald das Programm existiert, und beim
allerersten Versuch `PERMISSION_ERROR`. Quelltext bestehender Programme wird ueber
`SapGuiSetReportSource.vbs` gesetzt, neue Programme werden in SE38 mit `F5` angelegt.

### Beim Anlegen schlaegt SAP den zuletzt benutzten Transportauftrag vor

Der Dialog „Abfrage transportierbarer Workbench-Auftrag" war am 2026-09-10 mit
`T76K912490` vorbelegt — dem **ZZPRDAT-Auftrag, der auf die fachliche Abnahme wartet**.
Ein neues Objekt waere still dort hineingelaufen und haette den abzunehmenden Umfang
veraendert. **Immer pruefen, was im Feld `KO008-TRKORR` steht**, und mit
`tbar[0]/btn[8]` („Auftrag anlegen", F8) einen eigenen anlegen.

* **Der Dialog „Inaktive Objekte von KOI" enthaelt fremde Objekte.** Am 2026-09-03 lagen
  dort neben den eigenen auch `Z_KOI_AI_PUR`, `Z_TEST3_KD`, `Z_VCSTUFEN`,
  `ZMM_REPLACE_UL_TEXTS`, `ZM_LZCODE10_OPT4`, `ZTEST5`, `Z_IKO_TEST_UL` und weitere
  unfertige Objekte desselben Benutzers. Sie mitzuaktivieren wuerde fremde,
  unfertige Arbeit scharf schalten.
  **Regel: immer ueber Namensabgleich markieren, nie ueber Zeilennummern, und die
  Markierung anschliessend zurueckmessen.** Die eigenen Zeilen liegen direkt neben
  fremden; jede Listenaenderung verschiebt die Indizes.
  Skripte dafuer: `SapGuiWorklistSelect.vbs` (Modus `waehle` und `liste`).
* **Aus dem Methodeneditor aktiviert man nur die Methode.** Die uebrigen
  Interface-Methoden bleiben inaktiv, und SAP warnt elfmal „Es fehlt die Implementierung
  der Methode ...". Die Klasse als Ganzes aktiviert man ueber das **SE24-Einstiegsbild**:
  Klassenname eintragen, `Strg+F3`. Erst dann ist die Klasse vollstaendig.
  Das ist kein Schoenheitsfehler: SAP ruft `AT_SAVE`, `IN_UPDATE` und `BEFORE_UPDATE` bei
  jedem Sichern eines Fertigungsauftrags auf.
* **Eine BAdI-Implementierung aktiviert man in SE19 zweimal.** Der erste Druck auf
  `Strg+F3` meldet nur „Implementierungsklasse ... enthaelt Warnungen" und aktiviert
  **nicht**; das Kennzeichen `RSEXSCRN-ACTIVE` bleibt auf `inaktiv`. Der zweite Druck
  meldet „wurde aktiviert". Immer `RSEXSCRN-ACTIVE` zurueckmessen.
* **Rueckfall:** `Strg+F4` in SE19 deaktiviert eine BAdI-Implementierung sofort.
* **Funktionsbaustein vor Aufrufer aktivieren.** Solange der gerufene Baustein inaktiv ist
  (heute `Z_ZZPRDAT_SET`), meldet die Syntaxpruefung des Aufrufers, er existiere nicht. Das
  verdeckt die eigentlich interessanten Fehler.
* **Eine neue klassische BAdI-Implementierung ist erst gesichert, wenn eine Methode
  angefasst wurde.** SE19 legt die Implementierungsklasse nicht beim Sichern des Kopfes an,
  sondern beim ersten Oeffnen einer Methode auf dem Reiter „Interface". Wer vorher das Paket
  zuordnet, bekommt „Objekttyp ZCL_IM__… ist nicht vorhanden", und ein Sprung auf das
  Einstiegsbild verwirft die halbfertige Implementierung wieder — am 2026-09-04 passiert.
  Reihenfolge: anlegen, Kurztext, Reiter „Interface", Methode mit `F2` oeffnen, die
  Rueckfrage „Sichern" mit Ja beantworten.
* **SAP migriert dabei auf eine Erweiterungsimplementierung.** Es erscheint „Implementierung
  … wird migriert" und danach „Erweiterungsimplementierung auswaehlen oder neu anlegen".
  `wnd[1]/tbar[0]/btn[8]` legt eine neue an; sie braucht einen eigenen Namen, ein eigenes
  Paket und landet als `ENHO` im selben Transport.
* **Die halbfertige Implementierung vertraegt keine Navigation.** `SapGuiSetBadiMethodSource.vbs`
  springt zu Beginn auf `/nSE19` zurueck und ist deshalb erst brauchbar, wenn die
  Implementierung existiert. Fuer den Zustand davor gibt es
  `SapGuiBadiMethodeHier.vbs`, das ohne Navigation auf dem aktuellen Bild arbeitet.

## 4. Fachliche Fallen in den Auftragsdaten

* **`JEST`/`I0002` allein ist kein Freigabenachweis.** Bei abgeschlossenen Auftraegen ist
  `I0002` inaktiv gesetzt. Auftrag `1194970` war am 03.07.2025 nachweislich freigegeben,
  wurde von einem reinen JEST-Check aber als „nie freigegeben" gemeldet.
  **Verlaesslich ist `AFKO-FTRMI`, das Ist-Freigabedatum.** Am besten beides pruefen.
* **Auftragsart `PP21` traegt den Kurztext „Simulationsauftrag", ist aber der reale
  Fertigungsauftragstyp.** Von 34 produktiv erzeugten Auftraegen aus Marcos Liste sind
  24 `PP21` und 10 `PP22`. Der Kurztext darf nicht zum Ausweichen auf einen anderen Typ
  verleiten.
* **Werk ist `1100` (Trafag AG).** Referenzstammdaten aus Auftrag `1194970`:
  Material `36385`, Werk `1100`, Auftragsart `PP21`, Menge `1 ST`, Produktionsversion
  `1001`, Lagerort `0001`.

## 5. PowerShell-Fallen auf diesem Rechner

* **`natives.exe 2>&1 | ...` bricht ab.** Windows PowerShell 5.1 verpackt jede
  stderr-Zeile eines nativen Programms in einen Fehlerdatensatz (`NativeCommandError`) und
  setzt `$?` auf `False`, auch bei Exitcode 0. `uv` und `dotnet` schreiben Fortschritt auf
  stderr. Also **nicht umleiten**.
* **`Tee-Object` kennt kein `-LiteralPath`,** nur `-FilePath`.
* **Fenster fuer Passworteingaben** muessen mit `Start-Process powershell.exe -File ...`
  geoeffnet werden. Wird die Ausgabe des Werkzeugs in eine Pipeline geleitet, verschluckt
  sie den Passwort-Prompt und das Fenster wirkt leer.

## 6. Skriptbestand unter `.tmp_sap_probe/`

Der Ordner ist versioniert. Eine `.gitignore` darin haelt Buildausgaben, Protokolle,
Bildschirmabzuege und die Steuerelementbaeume draussen; der Quellcode ist im Repository.

### Grundwerkzeuge, immer zuerst

| Skript | Zweck |
|---|---|
| `SapGuiInspect.vbs` | listet alle offenen Sitzungen mit System, Mandant, Transaktion |
| `SapGuiDumpSession.vbs <TX> <Datei>` | schreibt den Steuerelementbaum einer Sitzung in eine Datei. **Das Arbeitspferd**, weil daraus alle Element-Ids stammen |
| `SapGuiInspectMainToolbar.vbs <TX>` | listet die Knoepfe der Anwendungsleiste mit Tooltip |
| `SapGuiInspectDialogButtons.vbs` | dasselbe fuer die Schaltflaechen eines Modaldialogs |
| `SapGuiInspectTopWindows.vbs` | zeigt, welche Fenster einer Sitzung offen sind |

### Bedienen

| Skript | Zweck |
|---|---|
| `SapGuiPressButton.vbs <TX> <ElementId>` | Knopf druecken, meldet Fenster, Statustyp, Meldung und offenen Dialog |
| `SapGuiSelectElement.vbs <TX> <Id> [...]` | Reiter, Radiobutton oder Ankreuzfeld waehlen |
| `SapGuiReadFields.vbs <TX> <Id> [...]` | Werte einzelner Elemente lesen. Noetig fuer Ankreuzfelder, deren Zustand der Baum nicht zeigt |
| `SapGuiSendVKey.vbs <TX> <VKey> [Fenster]` | Funktionstaste senden. 0=Enter, 3=F3, 7=F7, 8=F8, 11=Sichern, 12=Abbrechen, 26=Strg+F2 |
| `SapGuiCommand.vbs <TX> <Befehl>` | Kommandofeld: `/nXXXX`, `/oXXXX`, `/i` zum Schliessen einer Sitzung |
| `SapGuiOpenNewTransaction.vbs <TX>` | neue Sitzung per `/o` |
| `SapGuiSwitchTransaction.vbs <von> <nach>` | Transaktion in derselben Sitzung wechseln |
| `SapGuiBringToFront.vbs <Titelteil>` | Sitzung in den Vordergrund holen, damit Ingo sie snippen kann |
| `SapGuiFeldSetzen.vbs <TX> <Id> <Wert>` | Eingabefeld mit `SetFocus` davor und Rueckmessung danach. **Der Normalfall fuer Felder in Modaldialogen** |
| `SapGuiComboSetzen.vbs <TX> <Id> <Schluessel>` | Auswahlliste ueber den Schluessel setzen, nicht ueber den Anzeigetext. Umlaute ueberleben den Weg ueber die PowerShell-Kommandozeile nicht |
| `SapGuiFokus.vbs <TX> <Id> [VKey]` | Cursor auf ein Element setzen und optional eine Taste senden. In klassischen Listen haengt die Wirkung eines Knopfes an der Cursorzeile, und Labels lassen sich nicht „auswaehlen" |

### Auswerten

| Skript | Zweck |
|---|---|
| `Get-SapList.ps1 -Transaktion <TX>` | setzt eine klassische ABAP-Liste aus den `GuiLabel`-Koordinaten zu lesbaren Zeilen zusammen. **Der wichtigste Zeitsparer** |
| `SapGuiReadEditor.vbs <TX> <Datei>` | Versuch, den ABAP-Editor zu lesen. **Funktioniert nicht**, bewusst als Beleg behalten |
| `SapGuiProbeEditor.vbs` | zeigt, welche Lesemethoden das Editor-Control nicht kennt. Ebenfalls ein Beleg |
| `SapGuiProbeSe38Editor.vbs [Datei]` | dasselbe fuer SE38, und zwar fuer die Eigenschaft `Text`, die in der aelteren Sonde fehlte. Ergebnis: 19 Zeichen, also nur der Steuerelementtitel. Damit ist die Luecke geschlossen, der Editor gibt den Quelltext auf **keinem** Weg heraus |
| `SapGuiBaumLesen.vbs <TX> <BaumId>` | einen `GuiTree` vollstaendig auslesen, etwa Objektlisten in SE01/SE09 |
| `SapGuiGridLesen.vbs <TX> [GridId] [MaxZeilen]` | ein **ALV-Grid** ueber `RowCount`, `ColumnOrder` und `GetCellValue` auslesen. Ohne `GridId` meldet es die gefundenen Grid-Ids. Noetig ueberall dort, wo `Get-SapList.ps1` „Keine Listenzeilen gefunden" sagt, obwohl sichtbar Daten am Bildschirm stehen |
| `SapGuiGridMarkieren.vbs <TX> <GridId> <Zeilen>` | Zeilen eines ALV-Grids ueber `SelectedRows` markieren, nullbasiert und kommagetrennt. Der Vergleich in der Versionsverwaltung erwartet genau zwei markierte Zeilen |
| `SapGuiTabelleSuchen.vbs <TX> <TabId> <Text> [Feld] [VKey]` | in einer `GuiTableControl` seitenweise nach einem Text suchen, optional Cursor setzen und Taste senden. **Noetig bei MD04**, wo nur fuenf Zeilen sichtbar sind und ein Dump so aussieht, als fehle die Zeile |
| `SapGuiTabelleMarkieren.vbs <TX> <TabId> [Zeile\|alle]` | Zeilen einer `GuiTableControl` markieren und zurueckmessen, etwa die Planauftragsliste in CO41 |

### Entwicklungsobjekte aendern und aktivieren

| Skript | Zweck |
|---|---|
| `SapGuiSetBadiMethodSource.vbs <Impl> <Methode> <Datei>` | Methodenquelltext einer BAdI-Implementierung aus einer Datei setzen und sichern |
| `SapGuiShowBadiMethod.vbs <Impl> <Methode>` | Methode nur anzeigen, ohne zu aendern |
| `SapGuiSetFunctionSourceFromFile.vbs <FB> <Datei>` | Quelltext eines Funktionsbausteins aus einer Datei setzen und sichern |
| `SapGuiActivateFunction.vbs` | Funktionsbaustein aktivieren (oeffnet den Arbeitsvorrat) |
| `SapGuiActivateClass.vbs` | Klasse als Ganzes ueber das SE24-Einstiegsbild aktivieren |
| `SapGuiActivateBadiImpl.vbs` | BAdI-Implementierung aktivieren und `RSEXSCRN-ACTIVE` zurueckmessen |
| `SapGuiDeactivateBadi.vbs <Impl>` | **Rueckfallschalter.** Implementierung deaktivieren, mit Statuskontrolle |
| `SapGuiActivateBadi.vbs <Impl>` | Gegenstueck dazu, Implementierung aktivieren. Nimmt den Namen als Argument. Das aeltere `SapGuiActivateBadiImpl.vbs` hat den Namen `Z_ZZPRDAT_AT_RELEASE` fest eingebaut und steigt bei jedem anderen **still mit Code 5** aus |
| `SapGuiWorklistWaehlen.vbs <TX> <LOCAL\|TRANSPORT> [Namen...]` | Dialog „Inaktive Objekte", Reiter waehlen, genau die genannten Objekte markieren und alles andere demarkieren. Ohne Namen wird nur aufgelistet. Ersetzt `SapGuiWorklistSelect.vbs` und `SapGuiWorklistTransport.vbs`, deren Namensfilter fest verdrahtet sind |
| `SapGuiBadiMethodeHier.vbs <Methode> <Datei>` | wie oben, aber **ohne Navigation** auf dem gerade offenen SE19-Bild. Noetig, solange die Implementierung noch nicht gesichert ist |
| `SapGuiSetReportSource.vbs <Prog> <Datei>` | Quelltext eines **bestehenden** Reports ersetzen, sichern und aktivieren. Legt keinen Report an |
| `SapGuiPaketZuordnen.vbs <TX> <Paket> [Fenster]` | Dialog „Objektkatalogeintrag anlegen" beantworten. Die anschliessende Auftragsfrage bleibt bewusst offen |
| `SapGuiWorklistSelect.vbs <TX> waehle\|liste` | Dialog „Inaktive Objekte", Reiter **Lokale Objekte** ($TMP) |
| `SapGuiWorklistTransport.vbs <TX> waehle\|liste` | derselbe Dialog, Reiter **Transportierbare Objekte**. Sobald die Objekte in `ZPP1` liegen, stehen sie nur noch dort |
| `SapGuiInaktiveMarkieren.vbs <Sitzung> pruefen\|aktivieren <Name> [...]` | derselbe Dialog, aber setzt den Reiter selbst, entmarkiert zuerst alles und blaettert durch die ganze Liste. `pruefen` zeigt die Auswahl, ohne zu aktivieren |
| `SapGuiRedefinierenZeile.vbs <Sitzung> <Methode>` | geerbte Methode in SE24 redefinieren, ueber die **vorhandene** Zeile statt ueber eine Neuanlage |
| `SapGuiKlassenMethodeQuelltext.vbs <Sitzung> <Methode> <Datei>` | Methodenquelltext im Class Builder setzen und sichern, Methode wird in der Tabelle gesucht |
| `SapGuiMethodeneditorSchreiben.vbs <Sitzung> <Methode> <Datei>` | dasselbe, wenn der Editor bereits offen ist (direkt nach dem Redefinieren) |
| `SapGuiMethodenTabelle.vbs <Sitzung> [Scroll] [Suchname]` | Methodentabelle in SE24 auflisten oder eine Methode ueber alle Seiten suchen |
| `SapGuiFensterDump.vbs <Sitzung> [Fenster] [Tiefe]` | rekursiver Dump eines Fensters mit Id, Typ, Text und Tooltip. Erste Wahl, wenn eine Maske unbekannt ist |
| `SapGuiGatewayClient.vbs <Sitzung> <RequestUri> <Datei>` | GET im Gateway Client absetzen. Liefert den HTTP-Status; der Antwortrumpf bleibt unlesbar |

### Zustand pruefen

| Skript | Zweck |
|---|---|
| `SapGuiBadiStatus.vbs <Impl>` | ist die BAdI-Implementierung aktiv? |
| `SapGuiFunctionStatus.vbs <FB>` | ist der Funktionsbaustein aktiv? |
| `SapGuiEnhImplStatus.vbs <Name>` | Status der uebergeordneten Erweiterungsimplementierung |
| `SapGuiSe11Show.vbs <Datentyp>` | Datentyp in SE11 anzeigen |
| `SapGuiSe18Show.vbs <BAdI>` | BAdI-Definition anzeigen, etwa fuer „mehrfach nutzbar" und „filterabhaengig" |
| `SapGuiSm13.vbs [Benutzer]` | Verbuchungsauftraege des Tages |
| `SapGuiSt22.vbs [Benutzer]` | Kurzdumps des Tages |

### Fertigungsauftraege

| Skript | Zweck |
|---|---|
| `SapGuiRunFlow.vbs co01 <matnr> <werks> <auart> <menge> <ende> <ja\|nein>` | Auftrag anlegen, wahlweise freigeben, sichern. **Ein Aufruf statt sieben** |
| `SapGuiRunFlow.vbs co02release <aufnr>` | bestehenden Auftrag freigeben und sichern |
| `SapGuiRunFlow.vbs status <aufnr>` | Auftragsstatus lesen, ohne zu aendern |
| `SapGuiCo02ChangeEnd.vbs <aufnr> <TT.MM.JJJJ>` | Eckendtermin verschieben und sichern. Der Write-once-Nachweis |
| `SapGuiCo01Start.vbs`, `SapGuiCo01FillHeader.vbs` | Einzelschritte von CO01, falls `SapGuiRunFlow` an einer neuen Maske haengt |

### SapProbe ueber RFC

| Skript | Zweck |
|---|---|
| `SapCredential.ps1` | `Set-SapPassword`, `Test-SapPassword`, `Get-SapPassword`, `Remove-SapPassword`. Nur T76 |
| `RunSapProbe.ps1 <Argumente>` | SapProbe ohne Passwortabfrage, wenn eine Ablage existiert |
| `RunSapProbeInteractive.ps1` | aeltere Fassung mit Fenster und Eingabe |
| `Set-SapScriptingWarnings.ps1 -Aus\|-Ein` | Warnfenster von SAP GUI Scripting schalten. **Muss Ingo selbst ausfuehren** |
| `SapGuiProbeSession.vbs` | Diagnose bei `Sessions=0`: fragt `DisabledByServer` je Verbindung ab. `Wahr` heisst serverseitig abgeschaltet, siehe Abschnitt 0 |
| `SapGuiAktivieren.vbs <SitzungsIndex> <Name...>` | markiert genau die genannten Objekte im Dialog „Inaktive Objekte" und aktiviert sie **in einer COM-Sitzung**. Nimmt den Sitzungsindex, nicht den Transaktionscode |
| `SapGuiDialogKnoepfe.vbs <TX> [Fenster]` | Knoepfe eines Modaldialogs mit Tooltip, fuer beliebiges Fenster. Ersetzt `SapGuiInspectDialogButtons.vbs`, das fest auf SE19 und `wnd[1]` verdrahtet ist |
| `SapGuiSitzungSchliessen.vbs <ConnIdx> <SesIdx>` | Sitzung ueber ihren Index schliessen |

### Aufgabenspezifisch, nur als Vorlage lesen

`SapGuiRunZzprdatCheck.vbs` fuellt das Selektionsbild des Nachweisreports und fuehrt ihn
aus. Der Report heisst seit dem 2026-09-04 im System `Z_ZZPRDAT_CHECK` und liegt im Paket
`ZPP1`; der frueher dort verwendete `$TMP`-Report `ZTESTQQ` ist geloescht. Die uebrigen
`*Zzprdat*`- und `RunPpwr*`-Skripte stammen aus einzelnen Arbeitsschritten und sind
nicht allgemein verwendbar; sie zeigen aber, wie ein bestimmter Dialog bedient wird.
`probe_*.ps1` und `diag_password.ps1` sind Altbestand aus frueheren Untersuchungen.

**Anzupassen bei einer neuen Aufgabe:** Die Namensfilter in `SapGuiWorklistSelect.vbs`
sind fest auf die ZZPRDAT-Objekte gesetzt. Ohne Anpassung markiert das Skript fuer eine
andere Aufgabe gar nichts, was sicher, aber wirkungslos ist.

### Weitere Einzelschritt-Skripte, aus dem Lauf vom 2026-09-03

Sie machen jeweils einen einzelnen Schritt und sind vor allem als Vorlage nuetzlich, wenn
dieselbe Maske noch einmal bedient werden muss.

| Skript | Zweck |
|---|---|
| `SapGuiCheckFunction.vbs`, `SapGuiCheckMethod.vbs` | Syntaxpruefung in SE37 beziehungsweise im Class Builder ausloesen |
| `SapGuiSetFunctionSource.vbs` | aeltere Fassung mit fest eingebautem Quelltext; besser `SapGuiSetFunctionSourceFromFile.vbs` |
| `SapGuiSetUpdateAndOpenSource.vbs` | Verarbeitungsart eines Bausteins auf Verbuchung stellen und den Quelltextreiter oeffnen |
| `SapGuiListInactiveObjects.vbs` | Dialog „Inaktive Objekte" vollstaendig auflisten, mit Markierungsstand |
| `SapGuiSelectOwnInactiveObjects.vbs` | fruehere Auswahlfassung; besser `SapGuiWorklistSelect.vbs` |
| `SapGuiChooseLocalObject.vbs`, `SapGuiChooseSe37GroupLocal.vbs` | im Objektkatalog „Lokales Objekt" waehlen, also `$TMP` ohne Transport |
| `SapGuiStartFunctionCreate.vbs`, `SapGuiFillFunctionCreate.vbs` | Funktionsbaustein anlegen |
| `SapGuiStartFunctionGroupCreate.vbs`, `SapGuiFillFunctionGroupCreate.vbs` | Funktionsgruppe anlegen |
| `SapGuiFillFunctionImports.vbs` | Importparameter eines Bausteins eintragen |
| `SapGuiStartEnhancementContainer.vbs`, `SapGuiNameEnhancementContainer.vbs`, `SapGuiSelectEnhancementContainer.vbs`, `SapGuiInspectEnhancementSelection.vbs` | Erweiterungsimplementierung anlegen und zuordnen |
| `SapGuiStartZzprdatBadiCreate.vbs`, `SapGuiNameZzprdatBadi.vbs`, `SapGuiSaveZzprdatBadiHeader.vbs` | BAdI-Implementierung anlegen und benennen |
| `SapGuiOpenBadiInterfaceTab.vbs`, `SapGuiOpenAtRelease.vbs`, `SapGuiSetAtReleaseSource.vbs` | Interface-Reiter und Methode `AT_RELEASE` |
| `SapGuiSwitchSe37ToSe80.vbs`, `SapGuiOpenT76.vbs` | Navigationshilfen |

Die `RunZzprdat*`- und `RunPpwr*`-Skripte oeffnen jeweils ein Fenster fuer die
Passworteingabe und fuehren dann eine feste Folge von SapProbe-Aufrufen aus. Seit es
`SapCredential.ps1` gibt, ist `RunSapProbe.ps1` der bessere Weg. `probe_*.ps1` und
`diag_password.ps1` sind Altbestand aus frueheren Untersuchungen.

## 7. Empfohlener Ablauf fuer eine neue Aufgabe

1. `docs/AGENT_COORDINATION.md` lesen und eintragen.
2. Diese Datei und die fachliche Arbeitsdatei lesen.
3. `cscript //nologo .tmp_sap_probe\SapGuiInspect.vbs` — welche Sitzungen sind offen?
4. **Fuer alles Lesende zuerst pruefen, ob ein ABAP-Report die Frage beantwortet.**
   Wenn ja: Report schreiben, im Chat posten, Ingo legt ihn in SE38 als `$TMP` an und
   fuehrt ihn aus. Vorlage: `saptasks/zzprdat/produktiv/Z_ZZPRDAT_CHECK.abap`.
5. Erst fuer Schreiboperationen zum GUI-Scripting greifen, und dort jede Aenderung
   ueber ein Zustandsfeld zurueckmessen.
6. Am Ende `UserScripting` wieder auf `0`, Doku nachfuehren, committen.

## 8. Fertige Befehlsfolgen zum Kopieren

Alle Aufrufe laufen aus der Repository-Wurzel. Sie setzen voraus, dass SAP GUI mit einer
angemeldeten T76/100-Sitzung offen ist und `UserScripting` auf `1` steht.

### Lage feststellen

```powershell
cscript.exe //nologo '.tmp_sap_probe\SapGuiInspect.vbs'
cscript.exe //nologo '.tmp_sap_probe\SapGuiOpenNewTransaction.vbs' SE38
```

### Einen Bildschirm verstehen

Das ist der Einstieg in **jede** neue Maske. Ohne den Baum kennt man die Element-Ids nicht.

```powershell
$o = "$PWD\.tmp_sap_probe\dump.txt"
cscript.exe //nologo '.tmp_sap_probe\SapGuiDumpSession.vbs' CO01 $o | Out-Null
Select-String -Path $o -Pattern '/usr/ctxt|/usr/txt|wnd\[1\]' | ForEach-Object { $_.Line.Trim() }
cscript.exe //nologo '.tmp_sap_probe\SapGuiInspectMainToolbar.vbs' CO01
```

### Fertigungsauftrag anlegen und freigeben — der schnelle Weg

`SapGuiRunFlow.vbs` erledigt den ganzen Vorgang in **einem** Aufruf und beantwortet die
Zwischendialoge selbst:

```powershell
# anlegen und freigeben; gibt am Ende AUFTRAGSNUMMER=... aus
cscript.exe //nologo '.tmp_sap_probe\SapGuiRunFlow.vbs' co01 '36385' '1100' 'PP21' '1' '09.10.2026' 'ja'

# anlegen ohne Freigabe (fuer Tests, bei denen erst spaeter freigegeben wird)
cscript.exe //nologo '.tmp_sap_probe\SapGuiRunFlow.vbs' co01 '36385' '1100' 'PP21' '1' '09.10.2026' 'nein'

# bestehenden Auftrag freigeben und sichern
cscript.exe //nologo '.tmp_sap_probe\SapGuiRunFlow.vbs' co02release '1241803'

# nur den Status lesen, ohne etwas zu aendern
cscript.exe //nologo '.tmp_sap_probe\SapGuiRunFlow.vbs' status '1241804'
```

Am 2026-09-03 gemessen: aus sieben Einzelaufrufen wurde einer, mit derselben Wirkung.

**Wichtige Eigenschaft:** Ein Dialog, den die Regeltabelle nicht kennt, wird **nicht** blind
weggeklickt. Der Ablauf bricht ab, meldet Titel und verfuegbare Schaltflaechen und
ueberlaesst die Entscheidung dem Menschen. Blindklicken in einem gemeinsam genutzten
Testsystem ist die gefaehrlichste Abkuerzung, die es gibt. Wer eine neue Maske trifft,
ergaenzt die Funktion `DialogRegel` um eine Zeile.

Bekannte Dialoge und ihre Antworten:

| Dialogtitel | Antwort |
|---|---|
| Materialstatuspruefung | Ja, Komponente uebernehmen |
| Information | Weiter |
| Auftrag freigeben | freigeben, oder Abbrechen im Modus `nein` |
| Statusverarbeitung: Freigeben | Weiter |

### Fertigungsauftrag anlegen und freigeben — Einzelschritte

Nur noch noetig, wenn der Ablauf oben an einer neuen Maske haengt. Die Dialoge kommen in
dieser Reihenfolge.

```powershell
# 1. Einstiegsbild: Material, Werk, Auftragsart
cscript.exe //nologo '.tmp_sap_probe\SapGuiCo01Start.vbs' '36385' '1100' 'PP21'

# 2. Kopf: Menge und Eckendtermin. Kein Wochenende waehlen, sonst Warnung.
cscript.exe //nologo '.tmp_sap_probe\SapGuiCo01FillHeader.vbs' '1' '02.10.2026'

# 3. "Materialstatuspruefung": Komponente trotzdem uebernehmen
cscript.exe //nologo '.tmp_sap_probe\SapGuiPressButton.vbs' CO01 'wnd[1]/usr/btnSPOP-VAROPTION1'

# 4. "Information: Fehlende Materialverfuegbarkeit" bestaetigen
cscript.exe //nologo '.tmp_sap_probe\SapGuiPressButton.vbs' CO01 'wnd[1]/tbar[0]/btn[0]'

# 5. "Auftrag freigeben": freigeben (btnCANCEL statt VAROPTION1 sichert OHNE Freigabe)
cscript.exe //nologo '.tmp_sap_probe\SapGuiPressButton.vbs' CO01 'wnd[1]/usr/btnSPOP-VAROPTION1'

# 6. "Statusverarbeitung: Freigeben" mit Weiter schliessen
cscript.exe //nologo '.tmp_sap_probe\SapGuiPressButton.vbs' CO01 'wnd[1]/usr/btnOPTION2'

# 7. Sichern. Die Auftragsnummer steht in der Statusmeldung.
cscript.exe //nologo '.tmp_sap_probe\SapGuiPressButton.vbs' CO01 'wnd[0]/tbar[0]/btn[11]'
```

Die Freigabe-Schaltflaeche im Kopf ist `wnd[0]/tbar[1]/btn[25]` (Strg+F1). Den Auftragsstatus
liest man aus `wnd[0]/usr/txtCAUFVD-STTXT`; `FREI` heisst freigegeben.

**Achtung:** Auftragsart `PP21` gibt beim Sichern automatisch frei. Der Dialog „Auftrag
freigeben" erscheint auch dann, wenn man die Freigabe nie angestossen hat. Wer bewusst
einen unfreigegebenen Auftrag braucht, drueckt dort `wnd[1]/usr/btnCANCEL`; die Meldung
lautet dann „Freigabe abgelehnt", der Auftrag wird trotzdem gesichert.

### Bestehenden Auftrag freigeben (CO02)

```powershell
cscript.exe //nologo '.tmp_sap_probe\SapGuiOpenNewTransaction.vbs' CO02
# Auftragsnummer steht meist schon im Feld; sonst wnd[0]/usr/ctxtCAUFVD-AUFNR setzen
cscript.exe //nologo '.tmp_sap_probe\SapGuiSendVKey.vbs' CO02 0
cscript.exe //nologo '.tmp_sap_probe\SapGuiPressButton.vbs' CO02 'wnd[0]/tbar[1]/btn[25]'
cscript.exe //nologo '.tmp_sap_probe\SapGuiPressButton.vbs' CO02 'wnd[1]/usr/btnSPOP-VAROPTION1'
cscript.exe //nologo '.tmp_sap_probe\SapGuiPressButton.vbs' CO02 'wnd[0]/tbar[0]/btn[11]'
```

### Report ausfuehren und die Liste auslesen

Der Report heisst im System **`Z_ZZPRDAT_CHECK`** und liegt im Paket `ZPP1`; die Vorlage im
Repository ist `saptasks/zzprdat/produktiv/Z_ZZPRDAT_CHECK.abap`. Bis zum 2026-09-04 hiess
er `ZTESTQQ` und lag in `$TMP`; dieser Report ist geloescht.

```powershell
# Falls die Liste noch offen ist: mit F3 zurueck auf das Selektionsbild
cscript.exe //nologo '.tmp_sap_probe\SapGuiSendVKey.vbs' SE38 3
cscript.exe //nologo '.tmp_sap_probe\SapGuiRunZzprdatCheck.vbs' '1241802' '1241803'
```

**Das Auslesen der Listenausgabe ist der Trick, der am meisten Zeit spart.** Eine klassische
ABAP-Liste besteht aus `GuiLabel`-Elementen mit Spalten- und Zeilenkoordinate. Dieses
Snippet setzt sie wieder zu lesbaren Zeilen zusammen:

```powershell
$o = "$PWD\.tmp_sap_probe\liste.txt"
cscript.exe //nologo '.tmp_sap_probe\SapGuiDumpSession.vbs' SE38 $o | Out-Null
$zeilen = @{}
foreach ($l in (Get-Content $o)) {
  if ($l -match 'lbl\[(\d+),(\d+)\].*\| Text=(.*)$') {
    $sp=[int]$Matches[1]; $ze=[int]$Matches[2]; $tx=$Matches[3]
    if (-not $zeilen.ContainsKey($ze)) { $zeilen[$ze] = @{} }
    $zeilen[$ze][$sp] = $tx
  }
}
foreach ($ze in ($zeilen.Keys | Sort-Object)) {
  $s = ""
  foreach ($sp in ($zeilen[$ze].Keys | Sort-Object)) {
    $s += (" " * [Math]::Max(0, $sp - $s.Length)) + $zeilen[$ze][$sp]
  }
  if ($s.Trim()) { $s }
}
```

Damit laesst sich jede klassische `WRITE`-Liste vollstaendig auswerten, ohne Ingo um einen
Screenshot zu bitten. Fuer ALV-Grids gilt das nicht; dort ist `GetCellValue` der Weg.

### Quelltext aus dem System holen, wenn der Editor ihn nicht hergibt

Der ABAP-Editor ist ueber die Scripting-Schnittstelle nicht auslesbar, auch nicht ueber die
Eigenschaft `Text`. Wer aber `SapGuiSetReportSource.vbs` oder
`SapGuiSetBadiMethodSource.vbs` benutzt, ersetzt den **gesamten** Puffer und braucht deshalb
zwingend den exakten Ist-Stand — sonst zerstoert das Zurueckschreiben das Objekt.

Der Weg ohne Passwort ist ein Wegwerfreport in `$TMP`, der `READ REPORT` macht und das
Ergebnis mit `gui_download` auf einen **festen** Pfad schreibt. Fest deshalb, weil ein
Dateidialog ein Windows-Fenster waere, das kein Skript bedienen kann. Fuer einen
Methodenrumpf einer Klasse braucht es zuerst den Namen des Includes:

```abap
data ls_key type seocpdkey.
ls_key-clsname = 'ZCL_IM__ZZPRDAT_UPDATE'.
ls_key-cpdname = 'IF_EX_WORKORDER_UPDATE~BEFORE_UPDATE'.
lv_prog = cl_oo_classname_service=>get_method_include( mtdkey = ls_key ).
read report lv_prog into lt_src.
```

Am 2026-09-07 lieferte das `ZCL_IM__ZZPRDAT_UPDATE========CM008`, 50 Zeilen. Die Vorlage
liegt als `saptasks/zc12/Z_KOI_SRC_DUMP.abap` im Repository; das Objekt selbst ist nach
Gebrauch geloescht worden.

**Zwei Fallen dabei.** Erstens fragt SAP GUI beim Schreiben auf die Platte nach, und diese
Sicherheitsabfrage ist ein Windows-Dialog: Die Sitzung meldet danach „Die fuer diesen Vorgang
erforderlichen Daten sind noch nicht verfuegbar", bis jemand sie bestaetigt. Zweitens schreibt
`gui_download` mit `codepage = '4110'` UTF-8, waehrend `SapGuiSetReportSource.vbs` die Datei
frueher in der ANSI-Codepage gelesen hat. Deutsche Umlaute waeren dabei still zu
Buchstabensalat geworden; das Skript liest seit dem 2026-09-07 ueber `ADODB.Stream` als UTF-8.

### Versionsverwaltung eines Reports auswerten

Am 2026-09-04 fuer `ZM_ABGLEICH_KTSCH` durchlaufen. Der Weg beantwortet drei Fragen ohne
eine einzige Aenderung am Objekt: wer hat wann geaendert, welcher Transport gehoert dazu,
und **in welchen Versionen kommt ein bestimmter Bezeichner ueberhaupt vor**.

```powershell
# SE38, Programm eintragen, Quelltext anzeigen
cscript.exe //nologo '.tmp_sap_probe\SapGuiFeldSetzen.vbs' SE38 'wnd[0]/usr/ctxtRS38M-PROGRAMM' 'ZM_ABGLEICH_KTSCH'
cscript.exe //nologo '.tmp_sap_probe\SapGuiSelectElement.vbs' SE38 'wnd[0]/usr/radRS38M-FUNC_EDIT'
cscript.exe //nologo '.tmp_sap_probe\SapGuiPressButton.vbs' SE38 'wnd[0]/usr/btnSHOP'

# Hilfsmittel -> Versionen -> Versionsverwaltung
cscript.exe //nologo '.tmp_sap_probe\SapGuiSelectElement.vbs' SE38 'wnd[0]/mbar/menu[3]/menu[12]/menu[0]'

# Die Versionsliste ist ein ALV-Grid, kein klassisches Listenbild
$grid = 'wnd[0]/usr/cntlCONT/shellcont/shell/shellcont[0]/shell'
cscript.exe //nologo '.tmp_sap_probe\SapGuiGridLesen.vbs' SE38 $grid 60
```

**Menuepunkte brauchen `SapGuiSelectElement.vbs`, nicht `SapGuiPressButton.vbs`.** Ein
`GuiMenu` kennt `Press` nicht; der Aufruf endet mit „Das Objekt unterstuetzt diese
Eigenschaft oder Methode nicht".

**Der eigentliche Zeitsparer ist die Suche ueber alle Versionen**, Knopf `tbar[1]/btn[9]`
(F9). Das Suchfeld ist ein Control, kein Textfeld; `SapGuiFeldSetzen.vbs` setzt es trotzdem.
Danach traegt die Spalte `COMPARE` bei den Treffern das Symbol `Gefunden`. So laesst sich in
einem Durchgang datieren, **wann ein Bezeichner ins Programm kam**, ohne einen einzigen
Zeilenvergleich zu oeffnen.

**Sie sucht in den geaenderten Zeilen, nicht im ganzen Text der Version.** Der Dialog sagt es
selbst: „Suchen Sie nach Aenderungen innerhalb von Versionen". Am 2026-09-04 belegt: die Suche
nach `report z_abgleich_ktsch` meldet nur Version 3, obwohl der Zeilenvergleich die Zeilen 1
bis 116 von Version 4 als unveraendert gegenueber Version 3 ausweist, die Anweisung dort also
ebenfalls steht. „Gefunden" heisst **eingefuehrt oder geaendert**, „nicht gefunden" heisst
**unveraendert**, nicht „nicht vorhanden". Wer daraus „kommt nur in dieser Version vor" liest,
zieht falsche Schluesse. Fuer die Frage „ist der Bezeichner ueberhaupt drin" ist der
Zeilenvergleich die Quelle.

```powershell
cscript.exe //nologo '.tmp_sap_probe\SapGuiPressButton.vbs' SE38 'wnd[0]/tbar[1]/btn[9]'
cscript.exe //nologo '.tmp_sap_probe\SapGuiFeldSetzen.vbs' SE38 'wnd[1]/usr/cntlCONT_TEXT/shellcont/shell' 'fmt_quan'
cscript.exe //nologo '.tmp_sap_probe\SapGuiPressButton.vbs' SE38 'wnd[1]/tbar[0]/btn[0]'
```

Der Zeilenvergleich braucht genau zwei markierte Zeilen und `tbar[1]/btn[8]` (F8). Sein
Ergebnis ist wieder eine klassische Liste, also `Get-SapList.ps1`, und sie ist lang:
weiterblaettern mit VKey `82`.

```powershell
cscript.exe //nologo '.tmp_sap_probe\SapGuiGridMarkieren.vbs' SE38 $grid '1,2'
cscript.exe //nologo '.tmp_sap_probe\SapGuiPressButton.vbs' SE38 'wnd[0]/tbar[1]/btn[8]'
& '.\.tmp_sap_probe\Get-SapList.ps1' -Transaktion SE38
```

**Wenn ploetzlich kein Aufruf mehr durchkommt, zuerst die Verbindung pruefen, nicht das
Skript.** Am 2026-09-04 blieb mitten in einer Reihe von Suchlaeufen jeder Aufruf haengen, bis
hin zu `SapGuiInspect.vbs`. Der Verdacht fiel zuerst auf einen offenen Modaldialog; tatsaechlich
war der VPN abgebrochen und mit ihm die SAP-Sitzung. `SapGuiInspect.vbs` meldet dann
`Connections=0`, und nach der Neuanmeldung steht die Verbindung zuerst mit `Client=000` und
leerem Benutzer auf dem Anmeldebild — alle Skripte hier suchen aber gezielt `T76` mit
`Client=100` und finden noch nichts. Eine Warteschleife auf `Client=100 User=...` erspart das
Raten.

### Objekte in einem echten Paket statt in `$TMP` anlegen

Der Ablauf ist bei jedem Objekttyp derselbe: anlegen, sichern, **Paket** eintragen,
**Transportauftrag** bestaetigen. Zwei Dialoge, immer in dieser Reihenfolge. Am 2026-09-04
in T76/100 fuer Paket `ZPP1` und Auftrag `T76K912490` durchlaufen.

```powershell
# Paketdialog "Objektkatalogeintrag anlegen" beantworten.
# Das Fenster ist je nach Verschachtelung wnd[1], wnd[2] oder wnd[3].
cscript.exe //nologo '.tmp_sap_probe\SapGuiPaketZuordnen.vbs' SE19 ZPP1 'wnd[2]'

# Danach "Abfrage transportierbarer Workbench-Auftrag".
# Ein passender Auftrag steht meist schon im Feld KO008-TRKORR; dann genuegt btn[0].
# Beim allerersten Objekt legt btn[8] einen neuen Auftrag an.
cscript.exe //nologo '.tmp_sap_probe\SapGuiPressButton.vbs' SE19 'wnd[2]/tbar[0]/btn[0]'
```

**Report anlegen** geht nicht mit `SapGuiSetReportSource.vbs`, das nur bestehende Reports
aendert. In SE38: Name setzen, `btnNEW` druecken, im Eigenschaftenbild `RS38M-REPTI`
fuellen **und** die Auswahlliste `TRDIR-SUBC` auf Schluessel `1` („Ausfuehrbares Programm")
setzen. Ohne Typ kommt „Bitte geben Sie einen Wert an", und die Meldung nennt das Feld
nicht. Erst danach sichern, Paket, Auftrag; dann den Quelltext einspielen.

**Aktivieren:** Sobald die Objekte in einem echten Paket liegen, stehen sie im Dialog
„Inaktive Objekte" auf dem Reiter **Transportierbare Objekte**, nicht mehr unter „Lokale
Objekte". Wer weiter `SapGuiWorklistSelect.vbs` benutzt, markiert nichts und der Dialog
bleibt scheinbar wirkungslos stehen. Dafuer gibt es `SapGuiWorklistTransport.vbs`.

**Nachweisen, was im Auftrag steckt:** SE01, Reiter „Einzelanzeige", Auftragsnummer,
„Anzeigen". Die Objektliste steckt nicht im Auftrag, sondern in seiner Aufgabe: mit
`SapGuiFokus.vbs SE01 '<Label der Aufgabenzeile>' 2` hineinspringen, dann Reiter „Objekte".
Die Tabelle liest man ueber `TRE071X-OBJECT`, `TRE071X-OBJ_NAME` und `TRE071X-OBJ_DESCRI`.

### Planauftrag umsetzen: MD04 und CO41

Beide brauchen einen Planauftrag aus MD11.

```powershell
cscript.exe //nologo '.tmp_sap_probe\SapGuiMd11Profil.vbs'
cscript.exe //nologo '.tmp_sap_probe\SapGuiMd11Anlegen.vbs' '36385' '1100' '1' '19.10.2026'
```

**MD04:** Material und Werk stehen meist schon im Einstiegsbild, Enter zeigt die Liste. Die
Tabelle zeigt nur fuenf Zeilen; der Planauftrag steht weiter unten und wird mit
`SapGuiTabelleSuchen.vbs` gefunden und mit `F2` geoeffnet. Im Detailfenster fuehrt
`wnd[1]/tbar[0]/btn[25]` („-> FertAuftr") in die CO01-Maske; danach laufen dieselben
Dialoge wie beim Anlegen.

```powershell
$tab = 'wnd[0]/usr/subINCLUDE1XX:SAPMM61R:0750/tblSAPMM61RTC_EZ'
cscript.exe //nologo '.tmp_sap_probe\SapGuiTabelleSuchen.vbs' MD04 $tab '2406064' 'MDEZ-EXTRA' 2
cscript.exe //nologo '.tmp_sap_probe\SapGuiPressButton.vbs' MD04 'wnd[1]/tbar[0]/btn[25]'
```

**Achtung, Sitzungskollision:** Die Umsetzung aus MD04 laeuft intern unter dem
Transaktionscode **CO40**. Alle Skripte hier suchen die Sitzung ueber den
Transaktionscode und nehmen die **erste** passende. Ist noch eine aeltere CO40- oder
CO01-Sitzung offen, landen die Klicks dort. Erst die alte Sitzung mit `/n` parken, dann
weiterarbeiten. Am 2026-09-04 hat das zwei Rundreisen gekostet.

**CO41:** Werk steht meist schon da, Material eintragen, `F8`. Die Trefferliste ist eine
`GuiTableControl`; Zeile markieren, dann `wnd[0]/tbar[1]/btn[20]` („Umsetzen"). Die
Rueckfrage „Fehlende Verfuegbarkeit" wird mit `wnd[1]/usr/btnDY_VAROPTION1` bejaht.

```powershell
cscript.exe //nologo '.tmp_sap_probe\SapGuiFeldSetzen.vbs' CO41 'wnd[0]/usr/ctxtCAUFVD-MATNR' '36385'
cscript.exe //nologo '.tmp_sap_probe\SapGuiSendVKey.vbs' CO41 8
cscript.exe //nologo '.tmp_sap_probe\SapGuiTabelleMarkieren.vbs' CO41 'wnd[0]/usr/tblSAPLCOUPTCTRL_0200' '0'
cscript.exe //nologo '.tmp_sap_probe\SapGuiPressButton.vbs' CO41 'wnd[0]/tbar[1]/btn[20]'
```

**CO41 gibt nicht frei.** Der Auftrag entsteht und wird gesichert, aber ohne Freigabe. Wer
den Freigabetrigger messen will, gibt anschliessend ueber CO02 oder COHV frei.

**Nachtrag vom 2026-09-07 zu den aufgabenspezifischen Skripten.** Drei davon sind auf einen
bestimmten Bildschirmzustand zugeschnitten und brechen sonst ab, ohne dass der Grund am
Fehlertext ablesbar waere:

* `SapGuiMd11Anlegen.vbs` erwartet das Anlagebild „Planauftrag anlegen: Lagerauftrag". Nach
  jedem gesicherten Planauftrag steht MD11 wieder auf dem Einstiegsbild, deshalb vor **jedem**
  weiteren Anlegen erneut `SapGuiMd11Profil.vbs` aufrufen.
* `SapGuiCo40Umsetzen.vbs` braucht **zwei** Argumente, Planauftrag und Auftragsart. Fehlt das
  zweite, endet es mit „Index ausserhalb des gueltigen Bereichs".
* `SapGuiCohvSammelfreigabe.vbs` ist an der Ruecklesung der Funktionsauswahl gescheitert. Der
  Bildschirm selbst ist in Ordnung: Reiter „Massenbearbeitung - Freigabe" waehlen, Funktion
  steht dann schon auf „Freigabe", Auftragsbereich auf dem Reiter „Selektion" setzen, `F8`,
  Zeile mit `SapGuiGridMarkieren.vbs` markieren, dann Menue „Massenbearbeitung → Ausfuehren"
  und den Dialog „Auftrag freigeben" bestaetigen.

Beim Anlegen ueber CO01 fragt SAP fuer Material 36385 nach dem Materialstatus der Komponente
`D24057`. `SapGuiRunFlow.vbs` beantwortet das selbst; wer von Hand klickt, bekommt den Dialog
scheinbar endlos wieder, weil der Ablauf ohne den passenden naechsten Schritt an derselben
Stelle stehen bleibt.

### Kleine Regeln, die Rundreisen sparen

* Nach jedem Knopfdruck meldet `SapGuiPressButton.vbs` bereits Fenster, Statustyp,
  Statusmeldung und offenen Dialog. Ein zusaetzlicher Dump ist meist unnoetig.
* Erscheint „The control could not be found by id", hat sich die Maske zwischen Dump und
  Klick geaendert. Dann neu dumpen statt den Aufruf zu wiederholen.
* Eingabefelder behalten ihre Werte zwischen Aufrufen. CO02 hat die zuletzt bearbeitete
  Auftragsnummer meist schon stehen, SE38 den zuletzt ausgefuehrten Report.
* Nach dem Sichern springt CO01 auf das Einstiegsbild zurueck und **leert die Felder**.
  Fuer den naechsten Auftrag wieder mit `SapGuiCo01Start.vbs` beginnen.

## 9. Passwort einmal statt bei jedem Lauf

SapProbe fragt von sich aus bei jedem Aufruf nach dem Passwort. In einer Analysesitzung
sind das schnell ein Dutzend Unterbrechungen, bei denen jemand am Rechner sitzen muss.

`SapCredential.ps1` legt das Passwort einmalig mit der Windows-Datenschutz-API (DPAPI) ab.
Der Schluessel haengt am Windows-Konto und am Rechner: Die Datei ist anderswo wertlos. Sie
liegt unter `%LOCALAPPDATA%\TrafagSap\` und **nicht** im Repository.

```powershell
. .\.tmp_sap_probe\SapCredential.ps1
Set-SapPassword       # einmalig, verdeckte Eingabe
Test-SapPassword      # zeigt, ob und seit wann etwas abgelegt ist
Remove-SapPassword    # loescht die Ablage wieder
```

Danach laeuft SapProbe ohne Rueckfrage:

```powershell
.\.tmp_sap_probe\RunSapProbe.ps1 system-info
.\.tmp_sap_probe\RunSapProbe.ps1 table-read AUFK 5 "AUFNR EQ '000001241804'"
```

Ist nichts abgelegt, fragt `RunSapProbe.ps1` einmal fuer diesen Lauf und speichert nichts.

**Absichtliche Grenze: nur T76.** Fuer das Produktivsystem P76 wird kein Passwort
gespeichert; `Get-SapPassword -System 'P76'` bricht mit einer Meldung ab. Diese Grenze ist
hart im Code verankert und nicht ueber einen Parameter aufhebbar. Ein gespeichertes
Produktivpasswort auf einem Arbeitsplatz ist ein anderes Risiko als eine Eingabe pro Lauf,
und diese Entscheidung soll nicht beilaeufig fallen.

## 10. Was bewusst *nicht* nach ABAP verlagert wurde

Der urspruengliche Gedanke war, das Anlegen und Freigeben eines Testauftrags per
`BAPI_PRODORD_CREATE` und `BAPI_PRODORD_RELEASE` in einen Report zu packen. Das ist
bewusst unterblieben, aus zwei Gruenden:

1. **Der Nutzen ist weg.** Seit `SapGuiRunFlow.vbs` existiert, kostet der Dialogweg genau
   einen Aufruf. Ein BAPI-Report waere nicht schneller.
2. **Der Beweiswert waere geringer.** Die Mailkette beschreibt einen Fehler im
   Dialogweg ueber CO01, CO02, CO40, COHV, MD04 und CO41. Ein BAPI-Aufruf durchlaeuft nicht
   zwingend dieselben BAdI-Aufrufe. Ein gruener BAPI-Test waere kein Nachweis fuer den Weg,
   um den es fachlich geht.

Nach ABAP gehoert deshalb alles **Messende**: `Z_ZZPRDAT_CHECK` liest und urteilt, das
Anlegen bleibt im Dialog. Diese Aufteilung ist die schnellste und zugleich die mit dem
hoechsten Beweiswert.

## 11. Diagnosesonden richtig bauen

Am 2026-09-03 ist eine Diagnose ins Leere gelaufen, weil die Sonde von genau den Daten
abhing, die sie pruefen sollte. Der Fehler ist lehrreich genug, um ihn festzuhalten.

Gemessen werden sollte, ob eine BAdI-Methode ueberhaupt laeuft. Die Sonde uebernahm dafuer
die Auftragsnummer aus der Schnittstellenstruktur und schrieb ein Erkennungsdatum in genau
diesen Auftrag. War die Nummer in der Struktur leer, machte `CONVERSION_EXIT_ALPHA_INPUT`
daraus zwoelf Nullen, das `UPDATE` traf null Saetze, und das Ergebnis sah exakt so aus wie
„die Methode wurde nie gerufen".

**Regel: Eine Sonde darf keinen Eingabewert verwenden, dessen Richtigkeit sie gerade
klaeren soll.** Sie schreibt in ein festes, vorher bekanntes Ziel:

```abap
METHOD if_ex_workorder_update~at_save.
  CALL FUNCTION 'Z_PP_PRDDAT_SET' IN UPDATE TASK
    EXPORTING
      iv_aufnr  = '000001241805'   " fester Testauftrag
      iv_prddat = '19000102'.      " fachlich unmoegliche Marke
ENDMETHOD.
```

Zwei weitere Punkte, die sich bewaehrt haben:

* **Je Methode eine eigene Marke und ein eigenes Ziel.** Dann beantwortet ein einziger
  Testlauf mehrere Fragen gleichzeitig, statt sie nacheinander abzuarbeiten. Jeder
  Testauftrag verbraucht einen erstmaligen Freigabeuebergang und ist danach verbraucht.
* **Ein fachlich unmoegliches Datum als Marke**, etwa `01.01.1900`. Es kann nicht zufaellig
  entstehen und ist im Nachweisreport sofort als Diagnosewert erkennbar.

## 12. Die Warnfenster von SAP GUI Scripting

Mit `WarnOnAttach = 1` und `WarnOnConnection = 1` fragt SAP GUI bei jedem Anhaengen eines
Skripts nach. Das macht unbeaufsichtigtes Arbeiten unmoeglich und aeussert sich zwischendurch
als Laufzeitfehler `Erlaubnis verweigert: 'a.GetScriptingEngine'`; ein Wiederholen desselben
Aufrufs genuegt dann meist.

Abschalten, damit im Hintergrund gearbeitet werden kann:

```powershell
$p = 'HKCU:\Software\SAP\SAPGUI Front\SAP Frontend Server\Security'
Set-ItemProperty $p WarnOnAttach 0 -Type DWord
Set-ItemProperty $p WarnOnConnection 0 -Type DWord
```

**Zurueck am Ende der SAP-Arbeit, zusammen mit `UserScripting`:**

```powershell
$p = 'HKCU:\Software\SAP\SAPGUI Front\SAP Frontend Server\Security'
Set-ItemProperty $p WarnOnAttach 1 -Type DWord
Set-ItemProperty $p WarnOnConnection 1 -Type DWord
Set-ItemProperty $p UserScripting 0 -Type DWord
```

Diese Aenderung schaltet eine Sicherheitsabfrage ab und muss deshalb **vom Benutzer selbst**
ausgefuehrt werden. Ohne die Warnungen haengt sich jedes Skript ohne Rueckfrage an eine
angemeldete SAP-Sitzung an. In einer Umgebung, in der auch produktive Systeme angemeldet
sein koennen, ist das keine Nebensaechlichkeit.

## 13. Wenn eine Struktur nicht auslesbar ist: dynamischer Komponentenzugriff

Am 2026-09-03 wurde die Zeilenstruktur von `COBAI_T_HEADER` gebraucht, um `BEFORE_UPDATE`
des BAdI `WORKORDER_UPDATE` zu implementieren. Sie liegt in einer **Typgruppe**, nicht als
DDIC-Struktur. SE11 springt dorthin, der Quelltext steht in einem ABAP-Editor, und der ist
ueber die Scripting-Schnittstelle nicht lesbar. Auch die Signaturtabelle im Class Builder
erscheint nicht im Steuerelementbaum.

Der Ausweg braucht die Struktur gar nicht:

```abap
FIELD-SYMBOLS: <ls_kopf>  TYPE any,
               <lv_aufnr> TYPE any,
               <lv_gltrp> TYPE any.

LOOP AT it_header ASSIGNING <ls_kopf>.
  UNASSIGN: <lv_aufnr>, <lv_gltrp>.
  ASSIGN COMPONENT 'AUFNR' OF STRUCTURE <ls_kopf> TO <lv_aufnr>.
  CHECK sy-subrc = 0.
  ASSIGN COMPONENT 'GLTRP' OF STRUCTURE <ls_kopf> TO <lv_gltrp>.
  CHECK sy-subrc = 0.
  " ...
ENDLOOP.
```

`ASSIGN COMPONENT` loest den Feldnamen zur Laufzeit auf. Der Uebersetzer muss die Struktur
nicht kennen, und ein fehlendes Feld fuehrt nicht zum Abbruch, sondern zu `sy-subrc <> 0`.
Die Feldnamen selbst waren aus `AUFK` und `AFKO` ohnehin bekannt.

**Das ist kein Notbehelf, sondern oft die bessere Wahl**, wenn eine Schnittstelle nur
gelesen wird: Sie bleibt auch dann uebersetzbar, wenn SAP die Struktur in einem Release
erweitert.

Kosten: Der Zugriff ist nicht typgeprueft. Deshalb sollten die Werte sofort in getypte
Variablen uebernommen werden, wie oben in `lv_aufnr` und `lv_prddat`.
