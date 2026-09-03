Option Explicit
' Aktiviert die BAdI-Implementierungsklasse als Ganzes ueber das SE24-Einstiegsbild.
' Damit werden auch die leeren Implementierungen der uebrigen Interface-Methoden aktiv.
' Ohne sie warnt SAP mit "Es fehlt die Implementierung der Methode ..." und die Klasse
' waere unvollstaendig, obwohl SAP AT_SAVE, IN_UPDATE und BEFORE_UPDATE bei jedem
' Sichern eines Fertigungsauftrags aufruft.

Dim klasse, a, app, c, s, i, j, found

klasse = "ZCL_IM__ZZPRDAT_AT_RELEASE"

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE24" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

If InStr(1, s.FindById("wnd[0]").Text, "Einstieg", vbTextCompare) = 0 Then
  WScript.Echo "SE24 steht nicht auf dem Einstiegsbild: " & s.FindById("wnd[0]").Text
  WScript.Quit 4
End If

s.FindById("wnd[0]/usr/ctxtSEOCLASS-CLSNAME").Text = klasse
s.FindById("wnd[0]/tbar[1]/btn[27]").Press
WScript.Sleep 3000

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "STATUS_TYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar/pane[0]").Text
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
WScript.Quit 0
