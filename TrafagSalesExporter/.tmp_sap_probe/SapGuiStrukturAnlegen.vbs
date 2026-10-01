Option Explicit
' Legt in SE11 eine neue DDIC-Struktur an: Name, Typ "Struktur", Kurzbeschreibung.
' Felder danach mit SapGuiStrukturFelder.vbs, Aktivieren mit tbar[1]/btn[27].
' Bricht ab, wenn die Struktur schon existiert (dann kommt kein Anlegedialog).
'
' Aufruf: SapGuiStrukturAnlegen.vbs <SitzungsIndex> <Name> <Kurzbeschreibung>

Dim a, app, s, nameS, text
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
nameS = UCase(WScript.Arguments(1))
text = WScript.Arguments(2)

If s.Info.SystemName <> "T76" Then
  WScript.Echo "ABBRUCH: Sitzung ist " & s.Info.SystemName & ", nicht T76."
  WScript.Quit 2
End If

s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE11"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 1500
s.FindById("wnd[0]/usr/radRSRD1-DDTYPE").Select
s.FindById("wnd[0]/usr/ctxtRSRD1-DDTYPE_VAL").Text = nameS
s.FindById("wnd[0]").SendVKey 5
WScript.Sleep 1500

On Error Resume Next
Dim dlg
Set dlg = s.FindById("wnd[1]")
If Err.Number <> 0 Then
  WScript.Echo "ABBRUCH: kein Anlegedialog. Meldung: " & s.FindById("wnd[0]/sbar/pane[0]").Text
  WScript.Quit 3
End If
On Error GoTo 0

s.FindById("wnd[1]/usr/radD_100-STRU").Select
s.FindById("wnd[1]/tbar[0]/btn[0]").Press
WScript.Sleep 1500

s.FindById("wnd[0]/usr/txtDD02D-DDTEXT").Text = text
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
