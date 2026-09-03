Option Explicit
' Fuellt im CO01-Kopf Gesamtmenge und Eckendtermin und bestaetigt mit Enter.
' Legt noch nichts an: gesichert wird erst spaeter ausdruecklich.

Dim menge, endtermin, a, app, c, s, i, j, found, basis

menge = WScript.Arguments(0)
endtermin = WScript.Arguments(1)

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "CO01" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

If InStr(1, s.FindById("wnd[0]").Text, "Fertigungsauftrag anlegen", vbTextCompare) = 0 Then
  WScript.Echo "Unerwartetes Fenster: " & s.FindById("wnd[0]").Text
  WScript.Quit 4
End If

basis = "wnd[0]/usr/tabsTABSTRIP_0115/tabpKOZE/ssubSUBSCR_0115:SAPLCOKO1:0120"

s.FindById(basis & "/txtCAUFVD-GAMNG").Text = menge
s.FindById(basis & "/ctxtCAUFVD-GLTRP").Text = endtermin
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 3000

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
WScript.Echo "MENGE=" & s.FindById(basis & "/txtCAUFVD-GAMNG").Text
WScript.Echo "ENDE=" & s.FindById(basis & "/ctxtCAUFVD-GLTRP").Text
WScript.Echo "ENDE_TERMINIERT=" & s.FindById(basis & "/ctxtCAUFVD-GLTRS").Text
WScript.Quit 0
