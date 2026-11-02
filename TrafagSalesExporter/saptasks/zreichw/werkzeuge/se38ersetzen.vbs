Option Explicit
Dim app, c, ci, s, strom, quelle, ed
Set app = GetObject("SAPGUI").GetScriptingEngine
For ci = 0 To app.Children.Count - 1
  If app.Children(CLng(ci)).Children.Count > 0 Then
    If app.Children(CLng(ci)).Children(0).Info.SystemName = "T76" Then Set c = app.Children(CLng(ci))
  End If
Next
Set s = c.Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Or CLng(WScript.Arguments(0)) = 0 Then WScript.Echo "ABBRUCH": WScript.Quit 2
If InStr(s.FindById("wnd[0]").Text, WScript.Arguments(1)) = 0 Then WScript.Echo "ABBRUCH: falsches Bild " & s.FindById("wnd[0]").Text: WScript.Quit 2
Set strom = CreateObject("ADODB.Stream")
strom.Type = 2: strom.Charset = "utf-8": strom.Open: strom.LoadFromFile WScript.Arguments(2)
quelle = strom.ReadText: strom.Close
WScript.Echo "QUELLE_ZEICHEN=" & Len(quelle)
Set ed = s.FindById("wnd[0]/usr/cntlEDITOR/shellcont/shell")
ed.SelectAll
ed.ReplaceSelection quelle
WScript.Sleep 800
s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 2500
On Error Resume Next
WScript.Echo "FENSTER1=" & s.FindById("wnd[1]").Text
WScript.Echo "SBAR=" & s.FindById("wnd[0]/sbar").Text
