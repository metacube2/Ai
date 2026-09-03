Option Explicit
' Sendet eine Funktionstaste an ein Fenster der angegebenen Sitzung.
' Argument 1: Transaktionscode, Argument 2: VKey, Argument 3 optional: Fenster (Standard wnd[0]).
' VKey-Kurzliste: 0=Enter, 3=F3 zurueck, 8=F8, 11=Strg+S sichern, 12=F12 abbrechen, 26=Strg+F2.

Dim tx, vkey, fenster, a, app, c, s, i, j, found

tx = UCase(WScript.Arguments(0))
vkey = CLng(WScript.Arguments(1))
If WScript.Arguments.Count > 2 Then
  fenster = WScript.Arguments(2)
Else
  fenster = "wnd[0]"
End If

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = tx And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

s.FindById(fenster).SendVKey vkey
WScript.Sleep 2500

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
WScript.Echo "DIALOGTEXT=" & s.FindById("wnd[1]/usr").Text
WScript.Quit 0
