Option Explicit
'=======================================================================
' Schickt einen Befehl in das Befehlsfeld einer Sitzung, adressiert ueber
' den SITZUNGSINDEX statt ueber den Transaktionscode.
'
' Warum nicht SapGuiCommand.vbs: das sucht die Sitzung ueber den
' Transaktionscode, den man mitgibt. Sobald ein Ablauf mehrfach die
' Transaktion wechselt, weiss man nicht mehr, wo man steht, und bekommt
' „Keine Sitzung mit Transaktion X". Am 2026-09-11 hat das eine ganze
' Schleife lahmgelegt, weil die Sitzung inzwischen auf /IWFND/GW_CLIENT
' stand statt auf der erwarteten Transaktion.
'
' Den Index nennt SapGuiInspect.vbs.
'
' Aufruf: SapGuiOkCode.vbs <SitzungsIndex> <Befehl>     z.B. 0 /nSE24
'=======================================================================
Dim a, app, c, s

If WScript.Arguments.Count < 2 Then
  WScript.Echo "Aufruf: SapGuiOkCode.vbs <SitzungsIndex> <Befehl>"
  WScript.Quit 1
End If

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set c = app.Children(0)
If CInt(WScript.Arguments(0)) >= c.Children.Count Then
  WScript.Echo "Sitzungsindex gibt es nicht, offen sind " & c.Children.Count
  WScript.Quit 2
End If
Set s = c.Children(CLng(WScript.Arguments(0)))

s.FindById("wnd[0]/tbar[0]/okcd").Text = WScript.Arguments(1)
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 3000

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
On Error Resume Next
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar").Text
Err.Clear
On Error GoTo 0
