Option Explicit
' Fuehrt die Syntaxpruefung im Class Builder aus und liest die Statuszeile vollstaendig.

Dim a, app, c, s, i, j, found

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE19" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

If s.FindById("wnd[0]/usr/txtDY0200_CPDNAME").Text <> "IF_EX_WORKORDER_UPDATE~AT_RELEASE" Then WScript.Quit 5

' Fokus in den Editor, damit die Pruefung sicher auf die Methode wirkt.
s.FindById("wnd[0]/usr/subEDITORSUBSCREEN:SAPLEDITOR_START:8430/cntlEDITOR/shellcont/shell").SetFocus
WScript.Sleep 300
s.FindById("wnd[0]").SendVKey 26   ' Strg+F2 = Pruefen
WScript.Sleep 3000

WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar").Text
WScript.Echo "MELDUNGSNUMMER=" & s.FindById("wnd[0]/sbar").MessageNumber
WScript.Echo "MELDUNGSID=" & s.FindById("wnd[0]/sbar").MessageId
WScript.Echo "STATUS_FELD=" & s.FindById("wnd[0]/usr/txtDY0200_STATUS").Text
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
WScript.Quit 0
