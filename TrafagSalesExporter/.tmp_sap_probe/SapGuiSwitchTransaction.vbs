Option Explicit
Dim fromTransaction, toTransaction, a, app, c, s, i, j, found
fromTransaction = UCase(WScript.Arguments(0))
toTransaction = UCase(WScript.Arguments(1))
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = fromTransaction And s.Info.SystemName = "T76" And s.Info.Client = "100" Then found = True: Exit For
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/n" & toTransaction
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 1000
WScript.Echo "TX=" & s.Info.Transaction
