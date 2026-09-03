Option Explicit
' Sammelfreigabe ueber COHV fuer einen Auftragsnummernbereich.
'
' Aufruf: SapGuiCohvRelease.vbs <von> <bis>
'
' COHV ist der Weg, an dem die alte Dynpro-Loesung scheitern musste: In der
' Massenbearbeitung gibt es keinen Reiter "Trafag Daten", den man besuchen koennte.
' Das Skript selektiert, waehlt alle gefundenen Auftraege und loest die Freigabe aus.

Dim von, bis, s, basis, k, el

von = WScript.Arguments(0)
bis = WScript.Arguments(1)

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
  WScript.Sleep 3000
  Set SitzungHolen = SitzungHolen(tx)
End Function

Set s = SitzungHolen("COHV")
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nCOHV"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 3000
Set s = SitzungHolen("COHV")

basis = "wnd[0]/usr/tabsTABSTRIP_SELBLOCK/tabpSEL_00/ssub%_SUBSCREEN_SELBLOCK:PPIO_ENTRY:1200"
s.FindById(basis & "/ctxtS_AUFNR-LOW").Text = von
s.FindById(basis & "/ctxtS_AUFNR-HIGH").Text = bis

' Ausfuehren
s.FindById("wnd[0]").SendVKey 8
WScript.Sleep 4000

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text

' Werkzeugleiste der Ergebnisliste ausgeben, damit die Freigabe-Schaltflaeche
' bekannt wird, ohne sie zu raten.
On Error Resume Next
For k = 0 To s.FindById("wnd[0]/tbar[1]").Children.Count - 1
  Set el = s.FindById("wnd[0]/tbar[1]").Children(CLng(k))
  WScript.Echo "  " & el.Id & " Text=" & el.Text & " Tooltip=" & el.Tooltip
Next
On Error GoTo 0
WScript.Quit 0
