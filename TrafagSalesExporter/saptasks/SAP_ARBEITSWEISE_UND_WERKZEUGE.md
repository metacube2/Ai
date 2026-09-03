# SAP: Arbeitsweise und Werkzeuge

Stand: 2026-09-03

Diese Datei beantwortet eine einzige Frage: **Wie kommt eine neue Sitzung schnell zu
belastbaren SAP-Ergebnissen, ohne dieselben Sackgassen noch einmal zu durchlaufen?**
Sie ist bewusst aufgabenunabhaengig. Der fachliche Stand des ZZPRDAT-Auftrags steht in
`saptasks/zzprdat-kontext.md`.

Alles hier bezieht sich auf **T76, Mandant 100** (`travt762`). P76 ist Produktion und wird
nur nach ausdruecklicher Freigabe angefasst.

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
* **Scripting muss lokal eingeschaltet sein.** `HKCU\Software\SAP\SAPGUI Front\SAP Frontend
  Server\Security\UserScripting` steht normalerweise auf `0`. Fuer eine Arbeitssitzung auf
  `1` setzen und **danach wieder auf `0`**. `WarnOnAttach` und `WarnOnConnection` bleiben
  auf `1`; deshalb kann der erste Zugriff nach einer Pause mit „Erlaubnis verweigert"
  abgewiesen werden und muss einmal wiederholt werden.

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

Aufgabenspezifisch fuer ZZPRDAT: `SapGuiOpenAtRelease.vbs`, `SapGuiSetAtReleaseSource.vbs`,
`SapGuiCheckMethod.vbs`, `SapGuiActivateClass.vbs`, `SapGuiActivateBadiImpl.vbs`,
`SapGuiListInactiveObjects.vbs`.

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
