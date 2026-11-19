' P76Se16Export.vbs - NUR LESEN. Seit 2026-11-11 (Wochenabgleich IC 1100/1200).
' Zieht eine SE16-Tabelle in P76/100 in der EIGENEN Lesesitzung als Tab-Textdatei auf den PC
' (Liste > %PC > "Text mit Tabulatoren"). Speichern auf dem PC ist fuer SAP rein lesend.
'
' Aufruf (aus PowerShell, Paketordner):
'   cscript //nologo werkzeug\P76Se16Export.vbs <volle Sitzungs-Id> <TABELLE> <Zieldatei> [Selektionen...]
' Selektionen:
'   FELD=wert          Einzelwert (LOW)
'   FELD=von..bis      Intervall (LOW/HIGH)
'   FELD@datei.txt     Mehrfachselektion: Einzelwerte aus Textdatei (eine Zeile je Wert, CRLF),
'                      Import ueber den Knopf "Import aus Textdatei" im Mehrfachselektionsdialog
'   MAX=n              Maximale Trefferzahl (Vorgabe: leer = alle)
'   SPALTEN=F1,F2,...  nur diese Felder ausgeben (Einstellungen > Listaufbereitung > Feldauswahl)
'                      [nicht umgesetzt; bei Bedarf ergaenzen]
' Schutz:
'   - Sitzung muss P76/100 sein und SE16 zeigen (die Sitzungen des Benutzers zeigen andere TX);
'     ses[0] wird abgewiesen. Die Id kommt aus "P76Lesen.vbs neu:SE16" (SITZUNG=...).
'   - Es werden nur F3 (Zurueck), F8 (Ausfuehren), Enter und der OK-Code %PC benutzt.
'   - Jedes unerwartete Fenster wnd[1] fuehrt zum Abbruch (Ausgabe FEHLER ...), nichts wird bestaetigt.
' Ausgabe: TREFFER=<n> DATEI=<pfad> bzw. TREFFER=0 (Datei wird dann mit Kopfzeile "KEINE_TREFFER" geschrieben).
Option Explicit
Dim app, s, sesId, tab, ziel, i, arg, p, feld, wert, n, nameMap, k, t, titel, treffer, maxSel, fso
Set fso = CreateObject("Scripting.FileSystemObject")
sesId = WScript.Arguments(0) : tab = UCase(WScript.Arguments(1)) : ziel = WScript.Arguments(2)
If Right(sesId, 7) = "/ses[0]" Then Fehler "ses[0] gehoert dem Benutzer"
Set app = GetObject("SAPGUI").GetScriptingEngine
Set s = app.FindById(sesId)
Pruefe
maxSel = ""

' 1. zurueck zum Einstieg
For k = 1 To 5
  If Not Gibt("wnd[1]") Is Nothing Then Fehler "unerwartetes Fenster: " & s.FindById("wnd[1]").Text
  If Not Gibt("wnd[0]/usr/ctxtDATABROWSE-TABLENAME") Is Nothing Then Exit For
  s.FindById("wnd[0]").SendVKey 3 : WScript.Sleep 700
  Pruefe
Next
If Gibt("wnd[0]/usr/ctxtDATABROWSE-TABLENAME") Is Nothing Then Fehler "Einstiegsbild nicht erreicht"

' 2. Tabelle
s.FindById("wnd[0]/usr/ctxtDATABROWSE-TABLENAME").Text = tab
s.FindById("wnd[0]").SendVKey 0 : WScript.Sleep 800
If Gibt("wnd[0]/usr/txtMAX_SEL") Is Nothing Then s.FindById("wnd[0]").SendVKey 0 : WScript.Sleep 800
If Not Gibt("wnd[1]") Is Nothing Then Fehler "unerwartetes Fenster: " & s.FindById("wnd[1]").Text
If Gibt("wnd[0]/usr/txtMAX_SEL") Is Nothing Then Fehler "kein Selektionsbild: " & s.FindById("wnd[0]/sbar").Text
Pruefe

' 3. Feldnamen -> I<n>, alle Felder leeren
Set nameMap = CreateObject("Scripting.Dictionary")
For n = 1 To 80
  Set t = Gibt("wnd[0]/usr/txt%_I" & n & "_%_APP_%-TEXT")
  If t Is Nothing Then Exit For
  nameMap(UCase(Trim(t.Text))) = n
  SelFeld(n, "LOW").Text = ""
  SelFeld(n, "HIGH").Text = ""
Next

' 4. Selektionen
For i = 3 To WScript.Arguments.Count - 1
  arg = WScript.Arguments(i)
  If UCase(Left(arg, 4)) = "MAX=" Then
    maxSel = Mid(arg, 5)
  ElseIf InStr(arg, "@") > 0 And (InStr(arg, "=") = 0 Or InStr(arg, "@") < InStr(arg, "=")) Then
    p = InStr(arg, "@") : feld = UCase(Left(arg, p - 1)) : wert = Mid(arg, p + 1)
    If Not nameMap.Exists(feld) Then Fehler "Feld " & feld & " nicht im Selektionsbild"
    MehrfachAusDatei nameMap(feld), wert
  Else
    p = InStr(arg, "=") : feld = UCase(Left(arg, p - 1)) : wert = Mid(arg, p + 1)
    If Not nameMap.Exists(feld) Then Fehler "Feld " & feld & " nicht im Selektionsbild"
    If InStr(wert, "..") > 0 Then
      SelFeld(nameMap(feld), "LOW").Text = Left(wert, InStr(wert, "..") - 1)
      SelFeld(nameMap(feld), "HIGH").Text = Mid(wert, InStr(wert, "..") + 2)
    Else
      SelFeld(nameMap(feld), "LOW").Text = wert
    End If
  End If
Next
s.FindById("wnd[0]/usr/txtMAX_SEL").Text = maxSel
s.FindById("wnd[0]/usr/ctxtLIST_BRE").Text = "1023"

' 5. Ausfuehren
s.FindById("wnd[0]").SendVKey 8
WaitIdle
Pruefe
If Not Gibt("wnd[1]") Is Nothing Then Fehler "unerwartetes Fenster nach F8: " & s.FindById("wnd[1]").Text
titel = s.FindById("wnd[0]/titl").Text
If InStr(titel, "Treffer") = 0 Then
  If Not Gibt("wnd[0]/usr/txtMAX_SEL") Is Nothing Then
    Dim st : st = s.FindById("wnd[0]/sbar").Text
    Dim f : Set f = fso.CreateTextFile(ziel, True)
    f.WriteLine "KEINE_TREFFER " & st : f.Close
    WScript.Echo "TREFFER=0 STATUS=" & st & " DATEI=" & ziel
    WScript.Quit 0
  End If
  Fehler "kein Ergebnis: " & titel & " / " & s.FindById("wnd[0]/sbar").Text
End If
treffer = Trim(Mid(titel, InStr(titel, tab) + Len(tab)))
treffer = Trim(Replace(treffer, "Treffer", ""))

' 6. %PC
s.FindById("wnd[0]/tbar[0]/okcd").Text = "%PC"
s.FindById("wnd[0]").SendVKey 0 : WScript.Sleep 800
If Gibt("wnd[1]/usr/subSUBSCREEN_STEPLOOP:SAPLSPO5:0150/sub:SAPLSPO5:0150/radSPOPLI-SELFLAG[1,0]") Is Nothing Then Fehler "Formatdialog fehlt"
s.FindById("wnd[1]/usr/subSUBSCREEN_STEPLOOP:SAPLSPO5:0150/sub:SAPLSPO5:0150/radSPOPLI-SELFLAG[1,0]").Select
s.FindById("wnd[1]/tbar[0]/btn[0]").Press : WScript.Sleep 800
If Gibt("wnd[1]/usr/ctxtDY_PATH") Is Nothing Then Fehler "Dateidialog fehlt"
s.FindById("wnd[1]/usr/ctxtDY_PATH").Text = fso.GetParentFolderName(ziel) & "\"
s.FindById("wnd[1]/usr/ctxtDY_FILENAME").Text = fso.GetFileName(ziel)
s.FindById("wnd[1]/tbar[0]/btn[11]").Press
WaitIdle
If Not Gibt("wnd[1]") Is Nothing Then Fehler "Fenster nach Download: " & s.FindById("wnd[1]").Text
WScript.Echo "TREFFER=" & treffer & " STATUS=" & s.FindById("wnd[0]/sbar").Text & " DATEI=" & ziel

' 7. zurueck zum Einstieg
For k = 1 To 3
  If Not Gibt("wnd[0]/usr/ctxtDATABROWSE-TABLENAME") Is Nothing Then Exit For
  s.FindById("wnd[0]").SendVKey 3 : WScript.Sleep 700
  Pruefe
Next
WScript.Quit 0

' ---------------------------------------------------------------
Sub MehrfachAusDatei(nr, datei)
  Dim btn, w
  Set btn = Gibt("wnd[0]/usr/btn%_I" & nr & "_%_APP_%-VALU_PUSH")
  If btn Is Nothing Then Fehler "kein Mehrfachselektionsknopf fuer I" & nr
  btn.Press : WScript.Sleep 800
  Set w = Gibt("wnd[1]")
  If w Is Nothing Then Fehler "Mehrfachselektion oeffnet nicht"
  ' vorhandene Werte loeschen (Shift+F4 = "Ganze Selektion loeschen" im Dialog)
  w.SendVKey 16 : WScript.Sleep 500
  ' Import aus Textdatei: Knopf ueber den Tooltip suchen (Position ist nicht fest; in P76 btn[23], Umsch+F11)
  Dim impK : Set impK = ImportKnopf()
  If impK Is Nothing Then Fehler "Importknopf fehlt im Mehrfachdialog"
  impK.Press : WScript.Sleep 800
  If Gibt("wnd[2]/usr/ctxtDY_PATH") Is Nothing Then Fehler "Import-Dateidialog fehlt"
  s.FindById("wnd[2]/usr/ctxtDY_PATH").Text = fso.GetParentFolderName(datei) & "\"
  s.FindById("wnd[2]/usr/ctxtDY_FILENAME").Text = fso.GetFileName(datei)
  s.FindById("wnd[2]/tbar[0]/btn[0]").Press : WScript.Sleep 1200
  If Not Gibt("wnd[2]") Is Nothing Then Fehler "Fenster nach Import: " & s.FindById("wnd[2]").Text
  ' Uebernehmen (F8)
  s.FindById("wnd[1]/tbar[0]/btn[8]").Press : WScript.Sleep 800
  If Not Gibt("wnd[1]") Is Nothing Then Fehler "Mehrfachselektion nicht geschlossen"
End Sub

' Sucht in der Symbolleiste des Mehrfachdialogs (wnd[1]) den Knopf "Import aus Textfile" ueber den Tooltip.
' (2026-11-11: in P76 ist es btn[23], nicht btn[21]; die Position nicht fest verdrahten.)
Function ImportKnopf()
  Dim tb, q, bb
  Set ImportKnopf = Nothing
  Set tb = Gibt("wnd[1]/tbar[0]")
  If tb Is Nothing Then Exit Function
  For q = 0 To tb.Children.Count - 1
    Set bb = tb.Children(CLng(q))
    If InStr(bb.Tooltip, "Import") > 0 Then Set ImportKnopf = bb : Exit Function
  Next
End Function

Function SelFeld(nr, teil)
  Set SelFeld = Gibt("wnd[0]/usr/ctxtI" & nr & "-" & teil)
  If SelFeld Is Nothing Then Set SelFeld = Gibt("wnd[0]/usr/txtI" & nr & "-" & teil)
  If SelFeld Is Nothing Then Fehler "Feld I" & nr & "-" & teil & " fehlt"
End Function

Function Gibt(id)
  On Error Resume Next
  Set Gibt = Nothing
  Set Gibt = s.FindById(id, False)
  If Err.Number <> 0 Then Set Gibt = Nothing
  Err.Clear
End Function

Sub WaitIdle()
  Dim w
  WScript.Sleep 500
  For w = 1 To 1200
    If Not s.Busy Then Exit For
    WScript.Sleep 250
  Next
  WScript.Sleep 300
End Sub

Sub Pruefe()
  If s.Info.SystemName <> "P76" Or s.Info.Client <> "100" Then Fehler "nicht P76/100"
  If UCase(s.Info.Transaction) <> "SE16" Then Fehler "Sitzung zeigt " & s.Info.Transaction & ", nicht SE16"
End Sub

Sub Fehler(txt)
  WScript.Echo "FEHLER " & txt
  WScript.Quit 9
End Sub
