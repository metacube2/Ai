Option Explicit
'=======================================================================
' Zaehlt Eintraege einer Tabelle in SE16N (nur lesend, Knopf "Anzahl Eintraege").
' Arbeitet in einer BEREITS OFFENEN SE16N-Sitzung; navigiert nicht weg und sichert nichts.
'
' Aufruf: SapGuiSe16nAnzahl.vbs <Verbindung> <Sitzung> <Tabelle> <Feld=Von[:Bis]> [<Feld=Von[:Bis]> ...]
' Beispiel: ... 1 1 AFRU ERSDA=01.10.2026 ERZET=14:00:00:23:59:59  (Bis nach dem ersten ":" bei Daten,
' bei Uhrzeiten Von und Bis mit "-" trennen: ERZET=14:00:00-23:59:59)
'=======================================================================
Dim a, app, s, t, i, j, p, feld, von, bis, pos, gefunden, r, k, txt

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set s = app.Children(CLng(WScript.Arguments(0))).Children(CLng(WScript.Arguments(1)))
If s.Info.Transaction <> "SE16N" Then WScript.Echo "ABBRUCH: Sitzung ist nicht SE16N" : WScript.Quit 2

s.FindById("wnd[0]/usr/ctxtGD-TAB").Text = UCase(WScript.Arguments(2))
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 1200
' Alte Eingaben weg (Strg+F1), damit nichts aus dem letzten Lauf mitfiltert.
s.FindById("wnd[0]/tbar[1]/btn[25]").Press
WScript.Sleep 800

For i = 3 To WScript.Arguments.Count - 1
  p = Split(WScript.Arguments(i), "=")
  feld = UCase(p(0))
  von = p(1) : bis = ""
  If InStr(p(1), "-") > 0 Then
    von = Split(p(1), "-")(0) : bis = Split(p(1), "-")(1)
  End If
  gefunden = False
  Set t = s.FindById("wnd[0]/usr/subTAB_SUB:SAPLSE16N:0121/tblSAPLSE16NSELFIELDS_TC")
  pos = 0
  Do While pos < t.RowCount And Not gefunden
    t.VerticalScrollbar.Position = pos
    Set t = s.FindById("wnd[0]/usr/subTAB_SUB:SAPLSE16N:0121/tblSAPLSE16NSELFIELDS_TC")
    For r = 0 To t.VisibleRowCount - 1
      On Error Resume Next
      txt = ""
      txt = t.GetCell(r, 7).Text
      On Error GoTo 0
      If UCase(Trim(txt)) = feld Then
        t.GetCell(r, 3).Text = von
        If bis <> "" Then t.GetCell(r, 4).Text = bis
        gefunden = True
        Exit For
      End If
    Next
    pos = pos + t.VisibleRowCount
  Loop
  If Not gefunden Then WScript.Echo "ABBRUCH: Feld " & feld & " nicht gefunden" : WScript.Quit 3
Next

s.FindById("wnd[0]/tbar[1]/btn[7]").Press
WScript.Sleep 2500
On Error Resume Next
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
Dim dlg
Set dlg = Nothing
Set dlg = s.FindById("wnd[1]")
If Not dlg Is Nothing Then
  WScript.Echo "DIALOG=" & dlg.Text
  For k = 0 To dlg.Children(CLng(dlg.Children.Count - 1)).Children.Count - 1
    WScript.Echo "  " & dlg.FindById("usr").Children(CLng(k)).Text
  Next
  dlg.FindById("tbar[0]/btn[0]").Press
End If
