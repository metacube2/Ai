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
* **Funktionsbaustein vor Aufrufer aktivieren.** Solange `Z_PP_PRDDAT_SET` inaktiv ist,
  meldet die Syntaxpruefung des Aufrufers, der Baustein existiere nicht. Das verdeckt die
  eigentlich interessanten Fehler.

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

Wiederverwendbar und aufgabenunabhaengig:

| Skript | Zweck |
|---|---|
| `SapGuiInspect.vbs` | listet alle offenen Sitzungen mit System, Mandant, Transaktion |
| `SapGuiDumpSession.vbs <TX> <Datei>` | schreibt den kompletten Steuerelementbaum einer Sitzung in eine Datei; **das Arbeitspferd**, weil daraus alle Element-Ids stammen |
| `SapGuiInspectMainToolbar.vbs <TX>` | listet die Knoepfe der Anwendungsleiste mit Tooltip |
| `SapGuiPressButton.vbs <TX> <ElementId>` | drueckt einen Knopf und meldet Fenster, Status und offenen Dialog |
| `SapGuiOpenNewTransaction.vbs <TX>` | oeffnet eine neue Sitzung per `/o` |
| `SapGuiBringToFront.vbs <Titelteil>` | holt eine Sitzung in den Vordergrund, damit Ingo sie snippen kann |
| `SapGuiWorklistSelect.vbs <TX> waehle\|liste` | Dialog „Inaktive Objekte" auflisten oder eigene Objekte per Namensabgleich markieren |
| `SapGuiReadEditor.vbs <TX> <Datei>` | Versuch, den Editor zu lesen; **funktioniert nicht**, bewusst als Beleg behalten |

| `SapGuiSendVKey.vbs <TX> <VKey> [Fenster]` | Funktionstaste senden. 0=Enter, 3=F3 zurueck, 8=F8 ausfuehren, 11=Sichern, 12=Abbrechen, 26=Strg+F2 |

Aufgabenspezifisch fuer ZZPRDAT: `SapGuiOpenAtRelease.vbs`, `SapGuiSetAtReleaseSource.vbs`,
`SapGuiCheckMethod.vbs`, `SapGuiActivateClass.vbs`, `SapGuiActivateBadiImpl.vbs`,
`SapGuiListInactiveObjects.vbs`, `SapGuiCo01Start.vbs`, `SapGuiCo01FillHeader.vbs`,
`SapGuiRunZzprdatCheck.vbs`.

Die Namensfilter in `SapGuiWorklistSelect.vbs` sind fest auf die ZZPRDAT-Objekte gesetzt
und muessen fuer eine andere Aufgabe angepasst werden.

**Achtung, offene Luecke:** `.tmp_sap_probe/` ist nicht versioniert. Die Skripte und die
kompilierte `SapProbe.exe` liegen nur lokal auf diesem Arbeitsplatz. Ein neu geklontes
Repository hat sie nicht. Wer sie dauerhaft sichern will, muss die `.vbs`-Dateien in einen
versionierten Ordner verschieben; die Binaerdateien gehoeren nicht ins Repository.

## 7. Empfohlener Ablauf fuer eine neue Aufgabe

1. `docs/AGENT_COORDINATION.md` lesen und eintragen.
2. Diese Datei und die fachliche Arbeitsdatei lesen.
3. `cscript //nologo .tmp_sap_probe\SapGuiInspect.vbs` — welche Sitzungen sind offen?
4. **Fuer alles Lesende zuerst pruefen, ob ein ABAP-Report die Frage beantwortet.**
   Wenn ja: Report schreiben, im Chat posten, Ingo legt ihn in SE38 als `$TMP` an und
   fuehrt ihn aus. Vorlage: `saptasks/zzprdat/Z_ZZPRDAT_CHECK.abap`.
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

Der Report heisst im System **`ZTESTQQ`**; die Vorlage im Repository ist
`saptasks/zzprdat/Z_ZZPRDAT_CHECK.abap`.

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
