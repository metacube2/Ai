Option Explicit
' Setzt einen Planauftrag ueber CO40 in einen Fertigungsauftrag um, gibt frei und sichert.
'
' Aufruf: SapGuiCo40Umsetzen.vbs <planauftrag> <auftragsart>
'
' CO40 ist neben MD04 der Weg, auf dem laut Adil Lahrach die realen Auftraege
' entstehen. Ein unbekannter Dialog bricht ab, statt blind bestaetigt zu werden.

Dim plnum, auart, s, meldung, nummer

plnum = WScript.Arguments(0)
auart = WScript.Arguments(1)

Function SitzungHolen(tx)
  Dim a, app, c, s2, i, j
  Set a = GetObject("SAPGUI")
  Set app = a.GetScriptingEngine
  For i = 0 To app.Children.Count - 1
    Set c = app.Children(CLng(i))
    For j = 0 To c.Children.Count - 1
      Set s2 = c.Children(CLng(j))
      If UCase(s2.Info.Transaction) = UCase(tx) And s2.Info.SystemName = "T76" And s2.Info.Client = "100" Then
        Set SitzungHolen = s2
        Exit Function
      End If
    Next
  Next
  WScript.Quit 2
End Function

Sub Dialoge(sess)
  Dim r, t, z
  For r = 1 To 15
    t = ""
    On Error Resume Next
    t = sess.FindById("wnd[1]").Text
    On Error GoTo 0
    If t = "" Then Exit Sub
    z = ""
    If InStr(1, t, "Materialstatuspr", vbTextCompare) > 0 Then z = "wnd[1]/usr/btnSPOP-VAROPTION1"
    If InStr(1, t, "Auftrag freigeben", vbTextCompare) > 0 Then z = "wnd[1]/usr/btnSPOP-VAROPTION1"
    If InStr(1, t, "Statusverarbeitung", vbTextCompare) > 0 Then z = "wnd[1]/usr/btnOPTION2"
    If UCase(t) = "INFORMATION" Then z = "wnd[1]/tbar[0]/btn[0]"
    If z = "" Then
      WScript.Echo "ABBRUCH: unbekannter Dialog '" & t & "'"
      WScript.Quit 9
    End If
    WScript.Echo "  Dialog '" & t & "' -> " & z
    On Error Resume Next
    sess.FindById(z).Press
    On Error GoTo 0
    WScript.Sleep 1800
  Next
End Sub

Set s = SitzungHolen("CO40")
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nCO40"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2500
Set s = SitzungHolen("CO40")

s.FindById("wnd[0]/usr/ctxtAFPOD-PLNUM").Text = plnum
s.FindById("wnd[0]/usr/ctxtAUFPAR-PP_AUFART").Text = auart
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 3500
Dialoge s
WScript.Echo "Kopf: " & s.FindById("wnd[0]").Text

' Freigeben
s.FindById("wnd[0]/tbar[1]/btn[25]").Press
WScript.Sleep 2500
Dialoge s
On Error Resume Next
WScript.Echo "Status: " & s.FindById("wnd[0]/usr/txtCAUFVD-STTXT").Text
On Error GoTo 0

' Sichern
s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 3000
Dialoge s

meldung = s.FindById("wnd[0]/sbar/pane[0]").Text
WScript.Echo "MELDUNG=" & meldung

nummer = ""
If InStr(meldung, "Nummer") > 0 Then
  nummer = Trim(Mid(meldung, InStr(meldung, "Nummer") + 7))
  If InStr(nummer, " ") > 0 Then nummer = Left(nummer, InStr(nummer, " ") - 1)
End If
WScript.Echo "AUFTRAGSNUMMER=" & nummer
WScript.Quit 0
