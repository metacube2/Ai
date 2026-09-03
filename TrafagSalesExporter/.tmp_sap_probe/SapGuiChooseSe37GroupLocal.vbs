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
If s.FindById("wnd[2]").Text <> "Objektkatalogeintrag anlegen" Then WScript.Quit 4
If s.FindById("wnd[2]/usr/txtKO007-L_OBJ_NAME").Text <> "ZPP_ZZPRDAT_TEST" Then WScript.Quit 5
s.FindById("wnd[2]/tbar[0]/btn[7]").Press
WScript.Sleep 1200
WScript.Echo "TX=" & s.Info.Transaction
On Error Resume Next
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar/pane[0]").Text
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
