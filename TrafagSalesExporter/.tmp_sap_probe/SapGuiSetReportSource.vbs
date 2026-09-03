Option Explicit
' Ersetzt den Quelltext eines Reports in SE38 aus einer Datei, sichert und aktiviert.
'
' Aufruf: SapGuiSetReportSource.vbs <Programm> <Datei>

Dim prog, datei, s, fso, quelle, editor

prog = WScript.Arguments(0)
datei = WScript.Arguments(1)

Set fso = CreateObject("Scripting.FileSystemObject")
If Not fso.FileExists(datei) Then
  WScript.Echo "Datei nicht gefunden: " & datei
  WScript.Quit 2
End If
quelle = fso.OpenTextFile(datei, 1).ReadAll

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

Set s = SitzungHolen("SE38")
s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE38"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2500
Set s = SitzungHolen("SE38")

s.FindById("wnd[0]/usr/ctxtRS38M-PROGRAMM").Text = prog
s.FindById("wnd[0]/usr/radRS38M-FUNC_EDIT").Select
' Aendern
s.FindById("wnd[0]").SendVKey 6
WScript.Sleep 3000

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text

Set editor = s.FindById("wnd[0]/usr/cntlEDITOR/shellcont/shell")
editor.SelectAll
editor.ReplaceSelection quelle
WScript.Sleep 600

s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 2000
WScript.Echo "SICHERN=" & s.FindById("wnd[0]/sbar/pane[0]").Text

' Aktivieren
s.FindById("wnd[0]/tbar[1]/btn[27]").Press
WScript.Sleep 3000
WScript.Echo "AKTIVIEREN=" & s.FindById("wnd[0]/sbar/pane[0]").Text
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
WScript.Quit 0
