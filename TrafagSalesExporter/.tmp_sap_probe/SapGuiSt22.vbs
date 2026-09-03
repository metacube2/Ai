Option Explicit
' Listet in ST22 die Kurzdumps des heutigen Tages fuer einen Benutzer. Rein lesend.
' Aufruf: SapGuiSt22.vbs [Benutzer]

Dim benutzer, s, k, el

If WScript.Arguments.Count > 0 Then benutzer = WScript.Arguments(0) Else benutzer = "KOI"

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

Set s = SitzungHolen("ST22")
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nST22"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2500
Set s = SitzungHolen("ST22")

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text

' Auf dem Einstiegsbild alle Eingabefelder zeigen, damit die Feldnamen bekannt sind.
On Error Resume Next
For k = 0 To s.FindById("wnd[0]/usr").Children.Count - 1
  Set el = s.FindById("wnd[0]/usr").Children(CLng(k))
  If el.Type = "GuiTextField" Or el.Type = "GuiCTextField" Or el.Type = "GuiButton" Then
    WScript.Echo "  " & el.Type & " " & el.Name & " = " & el.Text
  End If
Next
On Error GoTo 0
WScript.Quit 0
