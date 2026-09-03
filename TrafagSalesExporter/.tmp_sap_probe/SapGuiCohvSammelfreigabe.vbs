Option Explicit
' Sammelfreigabe ueber COHV: selektiert einen Auftragsnummernbereich, waehlt die
' Massenbearbeitungsfunktion 130 (Freigabe), markiert alle gefundenen Zeilen und
' fuehrt aus.
'
' Aufruf: SapGuiCohvSammelfreigabe.vbs <von> <bis>
'
' Das ist der Weg, an dem die alte Dynpro-Loesung scheitern musste: In der
' Massenbearbeitung gibt es keinen Reiter "Trafag Daten", den man besuchen koennte.

Dim von, bis, s, selblock, mve, funktion, grid, runde, titel

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

Sub DialogeMelden(sess)
  Dim t
  t = ""
  On Error Resume Next
  t = sess.FindById("wnd[1]").Text
  On Error GoTo 0
  If t <> "" Then WScript.Echo "  offener Dialog: " & t
End Sub

Set s = SitzungHolen("COHV")
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nCOHV"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 3000
Set s = SitzungHolen("COHV")

selblock = "wnd[0]/usr/tabsTABSTRIP_SELBLOCK/tabpSEL_00/ssub%_SUBSCREEN_SELBLOCK:PPIO_ENTRY:1200"
mve = "wnd[0]/usr/tabsTABSTRIP_SELBLOCK/tabpMVE_00/ssub%_SUBSCREEN_SELBLOCK:SAPLCOWORK:0200"

' Auftragsbereich
s.FindById("wnd[0]/usr/tabsTABSTRIP_SELBLOCK/tabpSEL_00").Select
WScript.Sleep 600
s.FindById(selblock & "/ctxtS_AUFNR-LOW").Text = von
s.FindById(selblock & "/ctxtS_AUFNR-HIGH").Text = bis

' Funktion 130 = Freigabe
s.FindById("wnd[0]/usr/tabsTABSTRIP_SELBLOCK/tabpMVE_00").Select
WScript.Sleep 800
Set funktion = s.FindById(mve & "/cmbCOWORK_FCT_SETUP-FUNCT")
funktion.Key = "130"
WScript.Sleep 800
WScript.Echo "Funktion: " & funktion.Text

s.FindById("wnd[0]").SendVKey 8
WScript.Sleep 4000
WScript.Echo "Liste: " & s.FindById("wnd[0]").Text
DialogeMelden s

' Alle Zeilen markieren
Set grid = s.FindById("wnd[0]/usr/cntlCUSTOM/shellcont/shell/shellcont/shell")
WScript.Echo "Zeilen in der Liste: " & grid.RowCount
grid.SelectAll
WScript.Sleep 800

' Massenbearbeitung -> Ausfuehren
s.FindById("wnd[0]/mbar/menu[3]/menu[0]").Select
WScript.Sleep 4000

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
DialogeMelden s
WScript.Quit 0
