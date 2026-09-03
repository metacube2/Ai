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
If s.Children.Count >= 3 Then s.FindById("wnd[2]/tbar[0]/btn[0]").Press
WScript.Sleep 300
If s.Children.Count >= 2 Then s.FindById("wnd[1]/tbar[0]/btn[12]").Press
WScript.Sleep 300
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE80"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 1200
WScript.Echo "TX=" & s.Info.Transaction & " TITLE=" & s.FindById("wnd[0]").Text
