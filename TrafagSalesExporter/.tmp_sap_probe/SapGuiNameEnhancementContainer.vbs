Option Explicit
Dim a, app, c, s, i, j, found
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE19" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then found = True: Exit For
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2
If InStr(1, s.FindById("wnd[2]").Text, "Erweiterungsimplementierung", vbTextCompare) = 0 Then WScript.Quit 4
s.FindById("wnd[2]/usr/txtG_ENHSTRU-ENHNAME").Text = "Z_ZZPRDAT_REL_TEST"
s.FindById("wnd[2]/usr/txtG_ENHSTRU-SHORTTEXT").Text = "ZZPRDAT Freigabe-Prototyp T76"
s.FindById("wnd[2]/tbar[0]/btn[0]").Press
WScript.Sleep 1200
On Error Resume Next
WScript.Echo "WND1=" & s.FindById("wnd[1]").Text
WScript.Echo "WND2=" & s.FindById("wnd[2]").Text
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar/pane[0]").Text
