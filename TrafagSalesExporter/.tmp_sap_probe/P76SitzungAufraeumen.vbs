' P76SitzungAufraeumen.vbs - NUR die EIGENE P76-Lesesitzung, nur Abbrechen/Zurueck. Seit 2026-11-11.
' Zweck: nach einem abgebrochenen Export bleiben in der eigenen Sitzung Dialoge (Mehrfachselektion, Dateidialog) offen.
' Dieses Skript schliesst sie mit "Abbrechen" (F12) - nur wenn der Knopf tatsaechlich "Abbrechen" heisst - und geht mit F3 zurueck
' zum Einstiegsbild, damit P76SitzungSchliessen.vbs die Sitzung schliessen kann. Es sichert und bestaetigt nichts.
' Aufruf: cscript //nologo werkzeug\P76SitzungAufraeumen.vbs <volle Sitzungs-Id>
' Schutz: nur P76/100, nie ses[0], nur Transaktion SE16/SE16N. Meldet bei jedem Abweichen und tut dann nichts.
Option Explicit
Dim a, app, s, id, n, w, b, tip
id = WScript.Arguments(0)
If Right(id, 7) = "/ses[0]" Then WScript.Echo "ABGEWIESEN ses[0] gehoert dem Benutzer" : WScript.Quit 5
Set a = GetObject("SAPGUI") : Set app = a.GetScriptingEngine
Set s = app.FindById(id)
If s.Info.SystemName <> "P76" Or s.Info.Client <> "100" Then WScript.Echo "ABGEWIESEN nicht P76/100" : WScript.Quit 5
If UCase(s.Info.Transaction) <> "SE16" And UCase(s.Info.Transaction) <> "SE16N" Then WScript.Echo "ABGEWIESEN Sitzung zeigt " & s.Info.Transaction : WScript.Quit 5
' Fenster von oben nach unten abbrechen (wnd[3] .. wnd[1])
Dim k
For k = 3 To 1 Step -1
  Set w = Nothing
  On Error Resume Next
  Set w = s.FindById("wnd[" & k & "]", False)
  On Error GoTo 0
  If Not w Is Nothing Then
    WScript.Echo "FENSTER wnd[" & k & "]=" & w.Text
    Set b = Nothing
    On Error Resume Next
    Set b = s.FindById("wnd[" & k & "]/tbar[0]/btn[12]", False)
    On Error GoTo 0
    If b Is Nothing Then WScript.Echo "KEIN_ABBRECHEN_KNOPF wnd[" & k & "]" : WScript.Quit 6
    tip = b.Tooltip
    If InStr(tip, "Abbrechen") = 0 Then WScript.Echo "ABBRECHEN_UNBEKANNT " & tip : WScript.Quit 6
    b.Press
    WScript.Sleep 800
  End If
Next
For k = 1 To 4
  If UCase(s.Info.Transaction) = "SE16" Then
    If Not s.FindById("wnd[0]/usr/ctxtDATABROWSE-TABLENAME", False) Is Nothing Then Exit For
  End If
  s.FindById("wnd[0]").SendVKey 3
  WScript.Sleep 800
Next
WScript.Echo "AUFGERAEUMT TX=" & s.Info.Transaction
