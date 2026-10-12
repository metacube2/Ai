' Im Dialog "Objektkatalogeintrag aendern": Aendern-Modus, Paket setzen, sichern; zeigt Folgedialog.
' Aufruf: <Sitzung> <Paket>
Set a = GetObject("SAPGUI")
Set s = a.GetScriptingEngine.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Quit 2
On Error Resume Next
If Not s.FindById("wnd[1]/usr/ctxtKO007-L_DEVCLASS").Changeable Then
  s.FindById("wnd[1]/tbar[0]/btn[6]").Press
  WScript.Sleep 1200
End If
On Error GoTo 0
s.FindById("wnd[1]/usr/ctxtKO007-L_DEVCLASS").Text = WScript.Arguments(1)
s.FindById("wnd[1]/tbar[0]/btn[0]").Press
WScript.Sleep 2000
On Error Resume Next
For k = 1 To 3
  Err.Clear
  WScript.Echo "wnd[" & k & "]=" & s.FindById("wnd[" & k & "]").Text
  If Err.Number <> 0 Then Exit For
Next
Err.Clear
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
