' P76Se16nExport.vbs - NUR LESEN. Seit 2026-11-11 (Wochenabgleich IC 1100/1200).
' Zieht eine Tabelle ueber SE16N in P76/100 in der EIGENEN Lesesitzung als Tab-Textdatei auf den PC
' (Ergebnisgrid > %PC > "Text mit Tabulatoren"). Speichern auf dem PC ist fuer SAP rein lesend.
' SE16N statt SE16, wenn die Tabelle im SE16-Selektionsbild nur Schluesselfelder anbietet (EINA, KONP, VBRK ...).
'
' Aufruf (aus PowerShell, Paketordner):
'   cscript //nologo werkzeug\P76Se16nExport.vbs <volle Sitzungs-Id> <TABELLE> <Zieldatei> [Optionen...]
' Optionen:
'   FELD=wert | FELD=von..bis    Selektion (Feldname technisch, Wert in externer Form, Datum TT.MM.JJJJ)
'   FELD@datei.txt               Mehrfachselektion: Einzelwerte aus Textdatei (eine Zeile je Wert, CRLF)
'   AUS=F1,F2,...                nur diese Felder ausgeben (Haken "Ausgabe"); ohne AUS alle Felder
'   MAX=n                        maximale Trefferzahl (Vorgabe 999999)
' Zusaetzlich entsteht <Zieldatei>.felder mit Feldname;Kurztext der ausgegebenen Spalten in Reihenfolge.
' Schutz:
'   - Sitzung muss P76/100 sein und SE16N oder SE16 zeigen; ses[0] wird abgewiesen.
'     Die Id kommt aus "P76Lesen.vbs neu:SE16" (SITZUNG=...). Aus SE16 wird mit /nSE16N gewechselt.
'   - Der Haken "Eintraege pflegen" (GD-EDIT) wird geprueft und muss leer sein, sonst Abbruch.
'   - Jedes unerwartete Fenster fuehrt zum Abbruch; es wird nichts bestaetigt ausser den eigenen
'     Dialogen (Mehrfachselektion, Format, Dateiname).
' Am Ende steht die Sitzung wieder im SE16N-Einstiegsbild. Zum Schliessen mit P76SitzungSchliessen.vbs
' vorher "P76Lesen.vbs ses:<id> ok:/nSE16" ausfuehren.
Option Explicit
Dim app, s, sesId, tab, ziel, i, arg, p, feld, wert, maxSel, fso, sel, multi, aus, ausListe, k
Dim tcId, felderTxt, treffer
Set fso = CreateObject("Scripting.FileSystemObject")
sesId = WScript.Arguments(0) : tab = UCase(WScript.Arguments(1)) : ziel = WScript.Arguments(2)
If Right(sesId, 7) = "/ses[0]" Then Fehler "ses[0] gehoert dem Benutzer"
Set app = GetObject("SAPGUI").GetScriptingEngine
Set s = app.FindById(sesId)
Pruefe
tcId = "wnd[0]/usr/subTAB_SUB:SAPLSE16N:0121/tblSAPLSE16NSELFIELDS_TC"
maxSel = "999999"
Set sel = CreateObject("Scripting.Dictionary")
Set multi = CreateObject("Scripting.Dictionary")
aus = ""
For i = 3 To WScript.Arguments.Count - 1
  arg = WScript.Arguments(i)
  If UCase(Left(arg, 4)) = "MAX=" Then
    maxSel = Mid(arg, 5)
  ElseIf UCase(Left(arg, 4)) = "AUS=" Then
    aus = "," & UCase(Mid(arg, 5)) & ","
  ElseIf InStr(arg, "@") > 0 And (InStr(arg, "=") = 0 Or InStr(arg, "@") < InStr(arg, "=")) Then
    p = InStr(arg, "@") : multi(UCase(Left(arg, p - 1))) = Mid(arg, p + 1)
  Else
    p = InStr(arg, "=") : sel(UCase(Left(arg, p - 1))) = Mid(arg, p + 1)
  End If
Next

' 1. frisches SE16N-Einstiegsbild (setzt alte Selektionen zurueck)
If Not Gibt("wnd[1]") Is Nothing Then Fehler "unerwartetes Fenster: " & s.FindById("wnd[1]").Text
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE16N"
s.FindById("wnd[0]").SendVKey 0 : WaitIdle
Pruefe
If Gibt("wnd[0]/usr/ctxtGD-TAB") Is Nothing Then Fehler "SE16N-Einstieg nicht erreicht"
s.FindById("wnd[0]/usr/ctxtGD-TAB").Text = tab
s.FindById("wnd[0]").SendVKey 0 : WaitIdle
If Not Gibt("wnd[1]") Is Nothing Then Fehler "unerwartetes Fenster: " & s.FindById("wnd[1]").Text
If s.FindById("wnd[0]/usr/chkGD-EDIT").Selected Then Fehler "Eintraege pflegen ist angehakt"
s.FindById("wnd[0]/usr/txtGD-MAX_LINES").Text = maxSel
' 2026-11-16 (Hinweis F. Palma): Ausgabe-Haken mit einem Klick setzen/entfernen statt jede Zeile einzeln.
' Mit AUS= zuerst "Alle Mark. loeschen" (Umsch+F6), danach nur die gewuenschten Felder anhaken; ohne AUS "Alle markieren".
' Knopf ueber den Tooltip suchen (P76 16.11.2026: btn[18] "Alle Mark. loeschen", btn[13] "Alle markieren").
If aus <> "" Then KnopfTooltip "Alle Mark. l" Else KnopfTooltip "Alle markieren"

' 2. Selektionsfelder durchgehen
Dim tc, nRows, vis, pos, r, fname, done, cell, ftxt
Set done = CreateObject("Scripting.Dictionary")
felderTxt = ""
Set tc = s.FindById(tcId)
nRows = tc.RowCount
pos = 0
Do While pos < nRows
  Set tc = s.FindById(tcId)
  If tc.VerticalScrollbar.Position <> pos Then
    tc.VerticalScrollbar.Position = pos : WaitIdle
    Set tc = s.FindById(tcId)
  End If
  vis = tc.VisibleRowCount
  For r = 0 To vis - 1
    If pos + r >= nRows Then Exit For
    fname = ""
    On Error Resume Next
    fname = UCase(Trim(tc.FindById("txtGS_SELFIELDS-FIELDNAME[6," & r & "]").Text))
    ftxt = Trim(tc.FindById("txtGS_SELFIELDS-SCRTEXT_M[0," & r & "]").Text)
    On Error GoTo 0
    If fname <> "" And Not done.Exists(fname) Then
      done(fname) = 1
      If sel.Exists(fname) Then
        wert = sel(fname)
        If InStr(wert, "..") > 0 Then
          Zelle(tc, "LOW", 2, r).Text = Left(wert, InStr(wert, "..") - 1)
          Zelle(tc, "HIGH", 3, r).Text = Mid(wert, InStr(wert, "..") + 2)
        Else
          Zelle(tc, "LOW", 2, r).Text = wert
        End If
        sel.Remove fname
      End If
      Set cell = Nothing
      On Error Resume Next
      Set cell = tc.FindById("chkGS_SELFIELDS-MARK[5," & r & "]")
      On Error GoTo 0
      If Not cell Is Nothing Then
        ' Alle Haken sind schon gesetzt (ohne AUS) bzw. entfernt (mit AUS): nur die AUS-Felder anhaken
        If aus = "" Or InStr(aus, "," & fname & ",") > 0 Then
          If Not cell.Selected Then cell.Selected = True
          felderTxt = felderTxt & fname & ";" & ftxt & vbCrLf
        ElseIf cell.Selected Then
          cell.Selected = False      ' Absicherung, falls der Knopf einen Haken nicht entfernt hat
        End If
      End If
    End If
  Next
  pos = pos + vis
Loop
If sel.Count > 0 Then Fehler "Felder nicht gefunden: " & Join(sel.Keys, ",")

' 3. Mehrfachselektionen (eigene Schleife, weil der Dialog die Tabelle neu aufbaut)
Dim mk
For Each mk In multi.Keys
  MehrfachAusDatei mk, multi(mk)
Next

' 4. Ausfuehren
s.FindById("wnd[0]").SendVKey 8 : WaitIdle
Pruefe
If Not Gibt("wnd[1]") Is Nothing Then Fehler "unerwartetes Fenster nach F8: " & s.FindById("wnd[1]").Text & " / " & PopupText()
If Not Gibt("wnd[0]/usr/ctxtGD-TAB") Is Nothing Then
  Dim st, f : st = s.FindById("wnd[0]/sbar").Text
  Set f = fso.CreateTextFile(ziel, True) : f.WriteLine "KEINE_TREFFER " & st : f.Close
  WScript.Echo "TREFFER=0 STATUS=" & st & " DATEI=" & ziel
  WScript.Quit 0
End If
treffer = s.FindById("wnd[0]/titl").Text & " | " & s.FindById("wnd[0]/sbar").Text

' 5. Export ueber das Grid-Menue (Exportieren > Lokale Datei). Der OK-Code %PC oeffnet im SE16N-Grid nur einen
'    nicht skriptbaren HTML-Dialog; ueber das Grid-Menue kommt der klassische Formatdialog (getestet P76 11.11.2026).
Dim grid
Set grid = Gibt("wnd[0]/shellcont/shell")
If grid Is Nothing Then Set grid = Gibt("wnd[0]/usr/cntlRESULT_LIST/shellcont/shell")
If grid Is Nothing Then Fehler "Ergebnisgrid nicht gefunden"
grid.PressToolbarContextButton "&MB_EXPORT"
grid.SelectContextMenuItem "&PC"
WaitIdle
If Gibt("wnd[1]/usr/subSUBSCREEN_STEPLOOP:SAPLSPO5:0150/sub:SAPLSPO5:0150/radSPOPLI-SELFLAG[1,0]") Is Nothing Then Fehler "Formatdialog fehlt: " & PopupText()
s.FindById("wnd[1]/usr/subSUBSCREEN_STEPLOOP:SAPLSPO5:0150/sub:SAPLSPO5:0150/radSPOPLI-SELFLAG[1,0]").Select
s.FindById("wnd[1]/tbar[0]/btn[0]").Press : WaitIdle
If Gibt("wnd[1]/usr/ctxtDY_PATH") Is Nothing Then Fehler "Dateidialog fehlt"
s.FindById("wnd[1]/usr/ctxtDY_PATH").Text = fso.GetParentFolderName(ziel) & "\"
s.FindById("wnd[1]/usr/ctxtDY_FILENAME").Text = fso.GetFileName(ziel)
s.FindById("wnd[1]/tbar[0]/btn[11]").Press : WaitIdle
If Not Gibt("wnd[1]") Is Nothing Then Fehler "Fenster nach Download: " & s.FindById("wnd[1]").Text
Dim ff : Set ff = fso.CreateTextFile(ziel & ".felder", True) : ff.Write felderTxt : ff.Close
WScript.Echo "ERGEBNIS=" & treffer
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar").Text & " DATEI=" & ziel

' 6. zurueck zum Einstieg
s.FindById("wnd[0]").SendVKey 3 : WaitIdle
WScript.Quit 0

' ---------------------------------------------------------------
Function Zelle(tc, teil, col, r)
  Set Zelle = Nothing
  On Error Resume Next
  Set Zelle = tc.FindById("ctxtGS_SELFIELDS-" & teil & "[" & col & "," & r & "]")
  If Zelle Is Nothing Then Set Zelle = tc.FindById("txtGS_SELFIELDS-" & teil & "[" & col & "," & r & "]")
  On Error GoTo 0
  If Zelle Is Nothing Then Fehler "Zelle " & teil & " Zeile " & r & " fehlt"
End Function

Sub MehrfachAusDatei(fn, datei)
  Dim tc2, pos2, r2, n2, found, b
  found = False
  Set tc2 = s.FindById(tcId)
  tc2.VerticalScrollbar.Position = 0 : WaitIdle
  pos2 = 0
  Do While pos2 < s.FindById(tcId).RowCount And Not found
    Set tc2 = s.FindById(tcId)
    If tc2.VerticalScrollbar.Position <> pos2 Then tc2.VerticalScrollbar.Position = pos2 : WaitIdle : Set tc2 = s.FindById(tcId)
    For r2 = 0 To tc2.VisibleRowCount - 1
      n2 = ""
      On Error Resume Next
      n2 = UCase(Trim(tc2.FindById("txtGS_SELFIELDS-FIELDNAME[6," & r2 & "]").Text))
      On Error GoTo 0
      If n2 = fn Then
        ' 2026-11-12: ohne Fokus in der Zeile oeffnet der Knopf in P76 den Dialog nicht
        tc2.FindById("ctxtGS_SELFIELDS-LOW[2," & r2 & "]").SetFocus
        tc2.FindById("btnPUSH[4," & r2 & "]").Press : WaitIdle
        found = True : Exit For
      End If
    Next
    ' 2026-11-12: nach Press ist tc2 veraltet (Dialog offen) -> nur weiterzaehlen, wenn nicht gefunden
    If Not found Then pos2 = pos2 + tc2.VisibleRowCount
  Loop
  If Not found Then Fehler "Mehrfachfeld " & fn & " nicht gefunden"
  ' 2026-11-12: Dialog kommt in P76 teils erst nach >1 s -> bis 6 s warten
  For q = 1 To 20
    If Not Gibt("wnd[1]") Is Nothing Then Exit For
    WScript.Sleep 300
  Next
  If Gibt("wnd[1]") Is Nothing Then Fehler "Mehrfachselektion oeffnet nicht"
  ' Importknopf ueber den Tooltip suchen (2026-11-11: in P76 btn[23], nicht btn[21])
  Set b = Nothing
  Dim tb, q
  Set tb = Gibt("wnd[1]/tbar[0]")
  If Not tb Is Nothing Then
    For q = 0 To tb.Children.Count - 1
      If InStr(tb.Children(CLng(q)).Tooltip, "Import") > 0 Then Set b = tb.Children(CLng(q)) : Exit For
    Next
  End If
  If b Is Nothing Then
    ' 2026-11-12: Dialog "Feldweise Mehrfachselektion" (z. B. MARA-MATNR) hat keinen Datei-Import,
    ' nur "Upload aus Clipboard". Der Aufrufer muss die Werte vorher in die Zwischenablage legen
    ' (Umgebungsvariable SE16N_CLIPBOARD=1 als Bestaetigung).
    If CreateObject("WScript.Shell").Environment("PROCESS")("SE16N_CLIPBOARD") <> "1" Then Fehler "Importknopf fehlt im Mehrfachdialog (Clipboard-Weg nicht freigegeben)"
    For q = 0 To tb.Children.Count - 1
      If InStr(tb.Children(CLng(q)).Tooltip, "Clipboard") > 0 Then Set b = tb.Children(CLng(q)) : Exit For
    Next
    If b Is Nothing Then Fehler "Weder Import- noch Clipboard-Knopf im Mehrfachdialog"
    b.Press : WaitIdle : WScript.Sleep 1000
    If Not Gibt("wnd[2]") Is Nothing Then Fehler "Fenster nach Clipboard-Upload: " & s.FindById("wnd[2]").Text
    Set b = Gibt("wnd[1]/tbar[0]/btn[8]")
    If b Is Nothing Then Fehler "Uebernehmen fehlt"
    b.Press : WaitIdle : WScript.Sleep 1000
    If Not Gibt("wnd[1]") Is Nothing Then Fehler "Mehrfachselektion nicht geschlossen: " & s.FindById("wnd[1]").Text
    Exit Sub
  End If
  b.Press : WaitIdle
  If Gibt("wnd[2]/usr/ctxtDY_PATH") Is Nothing Then Fehler "Import-Dateidialog fehlt"
  s.FindById("wnd[2]/usr/ctxtDY_PATH").Text = fso.GetParentFolderName(datei) & "\"
  s.FindById("wnd[2]/usr/ctxtDY_FILENAME").Text = fso.GetFileName(datei)
  s.FindById("wnd[2]/tbar[0]/btn[0]").Press : WaitIdle
  If Not Gibt("wnd[2]") Is Nothing Then Fehler "Fenster nach Import: " & s.FindById("wnd[2]").Text
  Set b = Gibt("wnd[1]/tbar[0]/btn[8]")
  If b Is Nothing Then Fehler "Uebernehmen fehlt"
  b.Press : WaitIdle
  If Not Gibt("wnd[1]") Is Nothing Then Fehler "Mehrfachselektion nicht geschlossen: " & s.FindById("wnd[1]").Text
End Sub

Function PopupText()
  Dim o, t2
  t2 = ""
  On Error Resume Next
  Set o = s.FindById("wnd[1]/usr/txtMESSTXT1") : t2 = o.Text
  Set o = s.FindById("wnd[1]/usr/txtMESSTXT2") : t2 = t2 & " " & o.Text
  PopupText = t2
End Function

Function Gibt(id)
  On Error Resume Next
  Set Gibt = Nothing
  Set Gibt = s.FindById(id, False)
  If Err.Number <> 0 Then Set Gibt = Nothing
  Err.Clear
End Function

Sub KnopfTooltip(anfang)
  ' Drueckt den Knopf der Anwendungsleiste, dessen Tooltip mit "anfang" beginnt. Kein Treffer = Abbruch.
  Dim b
  For Each b In s.FindById("wnd[0]/tbar[1]").Children
    If b.Type = "GuiButton" Then
      If Left(b.Tooltip, Len(anfang)) = anfang Then
        b.Press : WaitIdle
        If Not Gibt("wnd[1]") Is Nothing Then Fehler "unerwartetes Fenster nach Knopf '" & anfang & "': " & s.FindById("wnd[1]").Text
        Exit Sub
      End If
    End If
  Next
  Fehler "Knopf mit Tooltip '" & anfang & "' nicht gefunden"
End Sub

Sub WaitIdle()
  Dim w
  WScript.Sleep 400
  For w = 1 To 2400
    If Not s.Busy Then Exit For
    WScript.Sleep 250
  Next
  WScript.Sleep 300
End Sub

Sub Pruefe()
  If s.Info.SystemName <> "P76" Or s.Info.Client <> "100" Then Fehler "nicht P76/100"
  If UCase(s.Info.Transaction) <> "SE16N" And UCase(s.Info.Transaction) <> "SE16" Then Fehler "Sitzung zeigt " & s.Info.Transaction
End Sub

Sub Fehler(txt)
  WScript.Echo "FEHLER " & txt
  WScript.Quit 9
End Sub
