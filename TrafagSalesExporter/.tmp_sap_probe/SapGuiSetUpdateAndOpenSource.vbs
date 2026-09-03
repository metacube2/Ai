Option Explicit
Dim a, app, c, s, i, j, found, base
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
If InStr(1, s.FindById("wnd[0]").Text, "Z_PP_PRDDAT_SET", vbTextCompare) = 0 Then WScript.Quit 4
base = "wnd[0]/usr/tabsFUNC_TAB_STRIP/tabpHEADER/ssubSCREEN_HEADER:SAPLSFUNCTION_BUILDER:3030/"
s.FindById(base & "radRS38L-VERBUCHER").Select
s.FindById(base & "radRS38L-UKIND1").Select
s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 700
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar/pane[0]").Text
s.FindById("wnd[0]/usr/tabsFUNC_TAB_STRIP/tabpSOURCE").Select
WScript.Sleep 700
