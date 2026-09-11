Option Explicit
' Dumpt rekursiv alle Elemente eines Fensters mit Id, Typ, Text und Tooltip.
' Aufruf: SapGuiFensterDump.vbs <SitzungsIndex> [Fenster] [MaxTiefe]
Dim a, app, c, s, wurzel, tiefe
Dim sesIdx, fenster
sesIdx = CInt(WScript.Arguments(0))
If WScript.Arguments.Count > 1 Then fenster = WScript.Arguments(1) Else fenster = "wnd[1]"
If WScript.Arguments.Count > 2 Then tiefe = CInt(WScript.Arguments(2)) Else tiefe = 8

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set c = app.Children(0)
Set s = c.Children(CLng(sesIdx))

On Error Resume Next
Set wurzel = s.FindById(fenster)
If Err.Number <> 0 Then WScript.Echo "Fenster " & fenster & " nicht da" : WScript.Quit 2
Err.Clear
On Error GoTo 0

Gehe wurzel, 0

Sub Gehe(el, lvl)
  Dim k, kind, txt, tt, typ, n
  txt = "" : tt = "" : typ = ""
  On Error Resume Next
  typ = el.Type
  txt = el.Text
  tt = el.Tooltip
  Err.Clear
  On Error GoTo 0
  WScript.Echo Space(lvl * 2) & el.Id & " [" & typ & "] " & Left(txt, 60) & " {" & Left(tt, 40) & "}"
  If lvl >= tiefe Then Exit Sub
  n = -1
  On Error Resume Next
  n = el.Children.Count
  Err.Clear
  On Error GoTo 0
  If n > 0 Then
    For k = 0 To n - 1
      On Error Resume Next
      Set kind = el.Children(CLng(k))
      If Err.Number = 0 Then
        Err.Clear
        On Error GoTo 0
        Gehe kind, lvl + 1
      Else
        Err.Clear
        On Error GoTo 0
      End If
    Next
  End If
End Sub
