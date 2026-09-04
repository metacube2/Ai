Option Explicit
' Traegt im Dialog "Objektkatalogeintrag anlegen" das Paket ein und bestaetigt.
' Anschliessend fragt SAP nach einem Aenderungsauftrag; dieser Dialog wird nur
' gemeldet, nicht beantwortet, damit die Auftragswahl ein sichtbarer Schritt bleibt.
'
' Aufruf: SapGuiPaketZuordnen.vbs <TX> <Paket> [<Fenster, Standard wnd[2]>]

Dim tx, paket, fenster, a, app, c, s, i, j, found, k, el

tx = UCase(WScript.Arguments(0))
paket = WScript.Arguments(1)
If WScript.Arguments.Count > 2 Then fenster = WScript.Arguments(2) Else fenster = "wnd[2]"

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
If Not found Then WScript.Quit 2

If InStr(1, s.FindById(fenster).Text, "Objektkatalog", vbTextCompare) = 0 Then
  WScript.Echo "ABBRUCH: " & fenster & " ist nicht der Objektkatalog, sondern: " & s.FindById(fenster).Text
  WScript.Quit 3
End If

WScript.Echo "Objekt: " & s.FindById(fenster & "/usr/txtKO007-L_PGMID").Text & " " & _
             s.FindById(fenster & "/usr/ctxtKO007-L_OBJECT").Text & " " & _
             s.FindById(fenster & "/usr/txtKO007-L_OBJ_NAME").Text

s.FindById(fenster & "/usr/ctxtKO007-L_DEVCLASS").Text = paket
s.FindById(fenster & "/tbar[0]/btn[0]").Press
WScript.Sleep 3000

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text

On Error Resume Next
For k = 1 To 3
  Err.Clear
  WScript.Echo "wnd[" & k & "]=" & s.FindById("wnd[" & k & "]").Text
  If Err.Number <> 0 Then Exit For
Next
WScript.Quit 0
