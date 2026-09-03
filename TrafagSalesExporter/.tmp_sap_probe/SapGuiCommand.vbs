Option Explicit
' Setzt einen Befehl in das Kommandofeld einer Sitzung und fuehrt ihn aus.
' Aufruf: SapGuiCommand.vbs <TX> <Befehl>
'
' Nuetzliche Befehle:
'   /nXXXX  Transaktion in derselben Sitzung starten
'   /oXXXX  Transaktion in neuer Sitzung starten
'   /i      aktuelle Sitzung schliessen
'
' Eine Sitzung zu schliessen und neu zu oeffnen ist der Weg aus
' LOAD_PROGRAM_CLASS_MISMATCH: Wird eine Klasse aktiviert, waehrend eine Sitzung
' die alte Fassung geladen hat, bricht diese Sitzung mit diesem Fehler ab. Nur
' ein frischer interner Modus laedt die neue Version.

Dim tx, befehl, a, app, c, s, i, j, found

tx = UCase(WScript.Arguments(0))
befehl = WScript.Arguments(1)

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = tx And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then
  WScript.Echo "Keine Sitzung mit Transaktion " & tx
  WScript.Quit 2
End If

s.FindById("wnd[0]/tbar[0]/okcd").Text = befehl
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2500

On Error Resume Next
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
On Error GoTo 0
WScript.Quit 0
