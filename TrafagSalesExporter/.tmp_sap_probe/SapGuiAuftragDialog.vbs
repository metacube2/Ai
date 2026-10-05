Option Explicit
' Beantwortet einen offenen Dialog "Abfrage transportierbarer Workbench-Auftrag" mit einem
' AUSDRUECKLICH genannten Auftrag (nie mit dem Vorschlag) und meldet Statuszeile und Folgefenster.
' Angelegt 2026-10-05 (Logistik live, T76K912658).
'
' Aufruf: SapGuiAuftragDialog.vbs <SitzungsIndex> <Auftrag>

Dim a, app, s, w, k
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2

Set w = s.FindById("wnd[1]")
If InStr(1, w.Text, "Auftrag", vbTextCompare) = 0 Then
  WScript.Echo "ABBRUCH: kein Auftragsdialog, sondern: " & w.Text
  WScript.Quit 3
End If
WScript.Echo "VORSCHLAG=" & s.FindById("wnd[1]/usr/ctxtKO008-TRKORR").Text
s.FindById("wnd[1]/usr/ctxtKO008-TRKORR").Text = WScript.Arguments(1)
s.FindById("wnd[1]/tbar[0]/btn[0]").Press
WScript.Sleep 3000
On Error Resume Next
For k = 1 To 2
  Err.Clear
  WScript.Echo "wnd[" & k & "]=" & s.FindById("wnd[" & k & "]").Text
  If Err.Number <> 0 Then Exit For
Next
Err.Clear
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
