Option Explicit
' Offener Dialog "Abfrage transportierbarer Workbench-Auftrag": vorher eine Information (z. B. "Auftrag bereits
' freigegeben") mit Enter schliessen, dann einen NEUEN Workbench-Auftrag mit Kurztext anlegen und uebernehmen.
' Angelegt 2026-10-05 (Logistik live Teil C), weil T76K912660 schon freigegeben war.
'
' Aufruf: SapGuiAuftragNeuImDialog.vbs <SitzungsIndex> <Kurztext>

Dim a, app, s, w, k
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2

On Error Resume Next
Set w = Nothing
Set w = s.FindById("wnd[2]")
If Not w Is Nothing Then
  WScript.Echo "INFO=" & s.FindById("wnd[2]/usr/txtMESSTXT1").Text & " " & s.FindById("wnd[2]/usr/txtMESSTXT2").Text
  s.FindById("wnd[2]/tbar[0]/btn[0]").Press
  WScript.Sleep 1500
End If
Err.Clear
On Error GoTo 0

Set w = s.FindById("wnd[1]")
If InStr(1, w.Text, "Auftrag", vbTextCompare) = 0 Then WScript.Echo "ABBRUCH: kein Auftragsdialog: " & w.Text : WScript.Quit 3
s.FindById("wnd[1]/tbar[0]/btn[8]").Press
WScript.Sleep 2000
s.FindById("wnd[2]/usr/txtKO013-AS4TEXT").Text = WScript.Arguments(1)
s.FindById("wnd[2]/tbar[0]/btn[0]").Press
WScript.Sleep 2000
WScript.Echo "AUFTRAG=" & s.FindById("wnd[1]/usr/ctxtKO008-TRKORR").Text
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
