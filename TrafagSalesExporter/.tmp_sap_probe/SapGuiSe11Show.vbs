Option Explicit
' Zeigt einen Datentyp in SE11 an und listet seine Felder beziehungsweise den
' Zeilentyp. Rein lesend.
'
' Aufruf: SapGuiSe11Show.vbs <Datentyp>

Dim typ, s, k, el, o, r, basis, tbl, feld, art

typ = WScript.Arguments(0)

Function SitzungHolen(tx)
  Dim a, app, c, s2, i, j, erste
  Set a = GetObject("SAPGUI")
  Set app = a.GetScriptingEngine
  Set erste = Nothing
  For i = 0 To app.Children.Count - 1
    Set c = app.Children(CLng(i))
    For j = 0 To c.Children.Count - 1
      Set s2 = c.Children(CLng(j))
      If s2.Info.SystemName = "T76" And s2.Info.Client = "100" Then
        If erste Is Nothing Then Set erste = s2
        If UCase(s2.Info.Transaction) = UCase(tx) Then
          Set SitzungHolen = s2
          Exit Function
        End If
      End If
    Next
  Next
  If erste Is Nothing Then WScript.Quit 4
  erste.FindById("wnd[0]/tbar[0]/okcd").Text = "/o" & tx
  erste.FindById("wnd[0]").SendVKey 0
  WScript.Sleep 2500
  Set SitzungHolen = SitzungHolen(tx)
End Function

Set s = SitzungHolen("SE11")
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE11"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2500
Set s = SitzungHolen("SE11")

' Feld "Datentyp" auf dem Einstiegsbild.
On Error Resume Next
s.FindById("wnd[0]/usr/radRSRD1-DDTYPE").Select
s.FindById("wnd[0]/usr/ctxtRSRD1-DDTYPE_VAL").Text = typ
On Error GoTo 0
' Anzeigen
s.FindById("wnd[0]").SendVKey 7
WScript.Sleep 3000

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text

' Alle Textfelder im Benutzerbereich ausgeben. Bei einem Tabellentyp steht dort
' der Zeilentyp, bei einer Struktur die Feldliste.
On Error Resume Next
For k = 0 To s.FindById("wnd[0]/usr").Children.Count - 1
  Set el = s.FindById("wnd[0]/usr").Children(CLng(k))
  If Len(Trim(el.Text)) > 0 And el.Type <> "GuiLabel" Then
    WScript.Echo "  " & el.Type & " " & el.Name & " = " & el.Text
  End If
Next
On Error GoTo 0
WScript.Quit 0
