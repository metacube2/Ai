Option Explicit
' Fuellt das CO01-Einstiegsbild und wechselt in den Auftragskopf.
' Argumente: Material, Werk, Auftragsart.

Dim matnr, werks, auart, a, app, c, s, i, j, found

matnr = WScript.Arguments(0)
werks = WScript.Arguments(1)
auart = WScript.Arguments(2)

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

If InStr(1, s.FindById("wnd[0]").Text, "Einstieg", vbTextCompare) = 0 Then
  WScript.Echo "CO01 steht nicht auf dem Einstiegsbild: " & s.FindById("wnd[0]").Text
  WScript.Quit 4
End If

s.FindById("wnd[0]/usr/ctxtCAUFVD-MATNR").Text = matnr
s.FindById("wnd[0]/usr/ctxtCAUFVD-WERKS").Text = werks
s.FindById("wnd[0]/usr/ctxtAUFPAR-PP_AUFART").Text = auart
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 3000

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
WScript.Quit 0
