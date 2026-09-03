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
s.FindById("wnd[1]/usr/ctxtTLIBG-AREA").Text = "ZPP_ZZPRDAT_TEST"
s.FindById("wnd[1]/usr/txtTLIBT-AREAT").Text = "ZZPRDAT Prototyp T76"
s.FindById("wnd[1]/tbar[0]/btn[0]").Press
WScript.Sleep 900
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar/pane[0]").Text
