Option Explicit
' Ersetzt den Quelltext eines BESTEHENDEN transportierbaren Reports in SE38, beantwortet die
' Transportabfrage mit dem genannten Auftrag (nie Vorschlag), sichert und aktiviert.
' Angelegt 2026-10-07 fuer ZM_OFFENE_FAUF (Bedarfsverursacher), weil SapGuiSetReportSource.vbs
' keine Transportabfrage kennt.
'
' Aufruf: SapGuiSetReportSourceAuftrag.vbs <SitzungsIndex> <Programm> <Datei> <Auftrag>

Dim a, app, s, prog, datei, auftrag, fso, quelle, editor

Set s = GetObject("SAPGUI").GetScriptingEngine.Children(0).Children(CLng(WScript.Arguments(0)))
prog = WScript.Arguments(1)
datei = WScript.Arguments(2)
auftrag = WScript.Arguments(3)

If s.Info.SystemName <> "T76" Then
  WScript.Echo "ABBRUCH: Sitzung ist nicht T76 (" & s.Info.SystemName & ")"
  WScript.Quit 2
End If

Set fso = CreateObject("Scripting.FileSystemObject")
quelle = fso.OpenTextFile(datei, 1).ReadAll

Sub Auftragsabfrage()
  Dim w
  On Error Resume Next
  Set w = s.FindById("wnd[1]")
  If Err.Number <> 0 Then Exit Sub
  On Error GoTo 0
  WScript.Echo "DIALOG=" & w.Text
  On Error Resume Next
  s.FindById("wnd[1]/usr/ctxtKO008-TRKORR").Text = auftrag
  If Err.Number <> 0 Then
    WScript.Echo "ABBRUCH: unbekannter Dialog, kein Auftragsfeld"
    WScript.Quit 5
  End If
  On Error GoTo 0
  s.FindById("wnd[1]").SendVKey 0
  WScript.Sleep 1500
End Sub

s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE38"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2000
s.FindById("wnd[0]/usr/ctxtRS38M-PROGRAMM").Text = prog
s.FindById("wnd[0]/usr/radRS38M-FUNC_EDIT").Select
s.FindById("wnd[0]").SendVKey 6
WScript.Sleep 3000
Auftragsabfrage
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text

Set editor = s.FindById("wnd[0]/usr/cntlEDITOR/shellcont/shell")
editor.SelectAll
editor.ReplaceSelection quelle
WScript.Sleep 800

s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 2500
Auftragsabfrage
WScript.Echo "SICHERN=" & s.FindById("wnd[0]/sbar/pane[0]").Text

s.FindById("wnd[0]/tbar[1]/btn[27]").Press
WScript.Sleep 3500
WScript.Echo "AKTIVIEREN=" & s.FindById("wnd[0]/sbar/pane[0]").Text
On Error Resume Next
WScript.Echo "DIALOG_NACH_AKTIVIEREN=" & s.FindById("wnd[1]").Text
WScript.Quit 0
