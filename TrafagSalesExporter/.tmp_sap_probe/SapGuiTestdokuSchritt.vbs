Option Explicit
' Ein Schritt in EIGENER T76/100-Sitzung, gefunden ueber die Sitzungsnummer (Info.SessionNumber),
' nie ueber einen festen Index. Angelegt 2026-11-06 fuer den ZZPRDAT-Testnachweis mit Screenshots.
' Aufruf: SapGuiTestdokuSchritt.vbs <SessionNumber> <befehl> [args]
'   info                      Fenstertitel, Transaktion, Statusleiste, offene Dialoge mit Text
'   okcode <code>             OK-Code setzen und Enter
'   set <id> <wert>           Feldinhalt setzen (kein Enter)
'   press <id>                Knopf druecken
'   vkey <n> [wnd]            virtuelle Taste an Fenster (Standard wnd[0])
'   select <id>               Reiter/Element waehlen
'   get <id>                  Text eines Elements lesen
'   shot <wnd> <pfad.png>     Bild des Fensters (wnd[0] vorher maximieren)
'   dump <wnd> [tiefe]        Element-Ids rekursiv
'   schliessen                eigene Sitzung mit /i schliessen
Dim app, con, s, i, j, nr, cmd, w
nr = CStr(WScript.Arguments(0))
cmd = LCase(WScript.Arguments(1))
Set app = GetObject("SAPGUI").GetScriptingEngine
Set s = Nothing
For i = 0 To app.Children.Count - 1
  Set con = app.Children(CLng(i))
  For j = 0 To con.Children.Count - 1
    If con.Children(CLng(j)).Info.SystemName = "T76" And con.Children(CLng(j)).Info.Client = "100" Then
      If CStr(con.Children(CLng(j)).Info.SessionNumber) = nr Then Set s = con.Children(CLng(j))
    End If
  Next
Next
If s Is Nothing Then WScript.Echo "FEHLER: keine T76/100-Sitzung mit Nummer " & nr : WScript.Quit 2
' Schutz 2026-11-06: Sitzungen 1-3 gehoerten bei Anlage Ingo bzw. anderen Sitzungen; nie benutzen.
If nr = "1" Or nr = "2" Or nr = "3" Then WScript.Echo "Sitzung " & nr & " ist nicht die eigene" : WScript.Quit 2
If s.Info.SystemName <> "T76" Or s.Info.Client <> "100" Then WScript.Echo "nicht T76/100" : WScript.Quit 2

Select Case cmd
  Case "info"
    Info
  Case "okcode"
    s.FindById("wnd[0]/tbar[0]/okcd").Text = WScript.Arguments(2)
    s.FindById("wnd[0]").SendVKey 0
    WScript.Sleep 2500
    Info
  Case "set"
    s.FindById(WScript.Arguments(2)).Text = WScript.Arguments(3)
    WScript.Echo "gesetzt " & WScript.Arguments(2) & " = " & s.FindById(WScript.Arguments(2)).Text
  Case "press"
    s.FindById(WScript.Arguments(2)).Press
    WScript.Sleep 2500
    Info
  Case "vkey"
    If WScript.Arguments.Count > 3 Then w = WScript.Arguments(3) Else w = "wnd[0]"
    s.FindById(w).SendVKey CInt(WScript.Arguments(2))
    WScript.Sleep 2500
    Info
  Case "select"
    s.FindById(WScript.Arguments(2)).Select
    WScript.Sleep 2500
    Info
  Case "get"
    WScript.Echo WScript.Arguments(2) & " = [" & s.FindById(WScript.Arguments(2)).Text & "]"
  Case "shot"
    w = WScript.Arguments(2)
    If w = "wnd[0]" Then
      s.FindById("wnd[0]").Maximize
      WScript.Sleep 800
      WScript.Echo "Iconic=" & s.FindById("wnd[0]").Iconic
    End If
    ' "PNG" als Text liefert hier still BMP; ImageType 2 = PNG
    WScript.Echo "HardCopy=" & s.FindById(w).HardCopy(WScript.Arguments(3), 2)
  Case "dump"
    Dim tiefe
    If WScript.Arguments.Count > 3 Then tiefe = CInt(WScript.Arguments(3)) Else tiefe = 12
    Gehe s.FindById(WScript.Arguments(2)), 0, tiefe
  Case "schliessen"
    s.FindById("wnd[0]/tbar[0]/okcd").Text = "/i"
    s.FindById("wnd[0]").SendVKey 0
    WScript.Sleep 1000
    WScript.Echo "geschlossen: Sitzung " & nr
  Case Else
    WScript.Echo "unbekannter Befehl " & cmd : WScript.Quit 1
End Select
WScript.Quit 0

Sub Info()
  Dim k, t, u, n
  WScript.Echo "SITZUNG=" & s.Id & " NR=" & s.Info.SessionNumber & " TX=" & s.Info.Transaction & " PROG=" & s.Info.Program & " DYNPRO=" & s.Info.ScreenNumber
  WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
  WScript.Echo "SBAR(" & s.FindById("wnd[0]/sbar").MessageType & ")=" & s.FindById("wnd[0]/sbar").Text
  For k = 1 To 3
    t = ""
    On Error Resume Next
    t = s.FindById("wnd[" & k & "]").Text
    If Err.Number = 0 And t <> "" Then
      WScript.Echo "wnd[" & k & "]=" & t
      Set u = s.FindById("wnd[" & k & "]/usr")
      For n = 0 To u.Children.Count - 1
        WScript.Echo "   " & u.Children(CLng(n)).Id & " [" & u.Children(CLng(n)).Type & "] " & u.Children(CLng(n)).Text
      Next
    End If
    Err.Clear
    On Error GoTo 0
  Next
End Sub

Sub Gehe(el, lvl, maxt)
  Dim k, n, typ, txt
  typ = "" : txt = ""
  On Error Resume Next
  typ = el.Type
  txt = el.Text
  Err.Clear
  On Error GoTo 0
  WScript.Echo Space(lvl * 2) & el.Id & " [" & typ & "] " & Left(txt, 60)
  If lvl >= maxt Then Exit Sub
  n = -1
  On Error Resume Next
  n = el.Children.Count
  Err.Clear
  On Error GoTo 0
  If n > 0 Then
    For k = 0 To n - 1
      Gehe el.Children(CLng(k)), lvl + 1, maxt
    Next
  End If
End Sub
