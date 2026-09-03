Option Explicit
Dim a, app, c, s, i, j, found
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE37" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then found = True: Exit For
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2
s.FindById("wnd[0]/usr/ctxtRS38L-NAME").Text = "Z_PP_PRDDAT_SET"
s.FindById("wnd[0]/usr/btnBUT2").Press
WScript.Sleep 900
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
