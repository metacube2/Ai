Option Explicit
' Schliesst eine Sitzung ueber ihren Index. Aufruf: <ConnIndex> <SessionIndex>
Dim a, app, c, s, ci, si
ci = CLng(WScript.Arguments(0))
si = CLng(WScript.Arguments(1))
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set c = app.Children(ci)
WScript.Echo "Sitzungen vorher=" & c.Children.Count
If si >= c.Children.Count Then WScript.Echo "Index zu gross" : WScript.Quit 2
Set s = c.Children(si)
WScript.Echo "Schliesse ses[" & si & "] TX=" & s.Info.Transaction
s.FindById("wnd[0]").SendVKey 0
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/i"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 800
Set c = app.Children(ci)
WScript.Echo "Sitzungen nachher=" & c.Children.Count
