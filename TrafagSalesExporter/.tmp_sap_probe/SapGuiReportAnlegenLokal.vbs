Option Explicit
' Legt einen ausfuehrbaren Report in SE38 als LOKALES Objekt ($TMP, ohne Transport) an.
' Nur fuer Test- und Pruefreports. Angelegt 2026-10-05 (Z_LOG_KAP_TEST, Logistik live Teil C).
' Den Quelltext setzt danach SapGuiSetReportSource.vbs.
'
' Aufruf: SapGuiReportAnlegenLokal.vbs <SitzungsIndex> <Programm> <Titel>

Dim a, app, s, k, w

Sub Melde(stufe)
  Dim i
  On Error Resume Next
  For i = 1 To 2
    Err.Clear
    WScript.Echo stufe & " wnd[" & i & "]=" & s.FindById("wnd[" & i & "]").Text
    If Err.Number <> 0 Then Exit For
  Next
  Err.Clear
  WScript.Echo stufe & " MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
  On Error GoTo 0
End Sub

Function Fenster(i)
  Set Fenster = Nothing
  On Error Resume Next
  Set Fenster = s.FindById("wnd[" & i & "]")
  On Error GoTo 0
End Function

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2

s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE38"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2000
s.FindById("wnd[0]/usr/ctxtRS38M-PROGRAMM").Text = WScript.Arguments(1)
s.FindById("wnd[0]").SendVKey 5
WScript.Sleep 2500
Melde "NACH_ANLEGEN"

Set w = Fenster(1)
If w Is Nothing Then WScript.Echo "ABBRUCH: kein Eigenschaftendialog" : WScript.Quit 3
On Error Resume Next
s.FindById("wnd[1]/usr/txtRS38M-REPTI").Text = WScript.Arguments(2)
If Err.Number <> 0 Then WScript.Echo "TITELFELD nicht gefunden: " & Err.Description
Err.Clear
s.FindById("wnd[1]/usr/cmbTRDIR-SUBC").Key = "1"
If Err.Number <> 0 Then WScript.Echo "TYP nicht gesetzt: " & Err.Description
Err.Clear
On Error GoTo 0
' Sichern im Eigenschaftendialog
s.FindById("wnd[1]/tbar[0]/btn[0]").Press
WScript.Sleep 2500
Melde "NACH_EIGENSCHAFTEN"

Set w = Fenster(1)
If Not w Is Nothing Then
  If InStr(1, w.Text, "Objektkatalog", vbTextCompare) > 0 Or InStr(1, w.Text, "Paket", vbTextCompare) > 0 Then
    ' "Lokales Objekt" = $TMP ohne Transport
    s.FindById("wnd[1]/tbar[0]/btn[7]").Press
    WScript.Sleep 2500
  End If
End If
Melde "ENDE"
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
