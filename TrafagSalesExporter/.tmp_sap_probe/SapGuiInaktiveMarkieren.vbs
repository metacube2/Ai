Option Explicit
'=======================================================================
' Markiert im Dialog "Inaktive Objekte" genau die genannten Objekte und
' aktiviert sie - alles in EINER COM-Sitzung.
'
' Gegenueber SapGuiAktivieren.vbs vier Unterschiede, die am 2026-09-10
' noetig wurden:
'  1. Der Reiter "Transportierbare Objekte" wird selbst gesetzt. Der Dialog
'     kommt auf "Lokale Objekte" hoch, und dort ist die Tabelle leer.
'     SapGuiWorklistSelect.vbs stellt den Reiter sogar wieder zurueck.
'  2. Vor dem Markieren wird "Alles entmarkieren" (Umsch+F9) gedrueckt.
'  3. Es wird geblaettert, aber nur solange die Scrollposition wirklich
'     vorrueckt. Sonst laeuft das Skript mehrfach ueber dieselbe Seite und
'     meldet dieselben Objekte doppelt.
'  4. Leerzeilen erscheinen als Reihe von Unterstrichen, nicht als
'     Leerstring. Sie werden ausgefiltert.
'
' Markiert wird ausschliesslich ueber Namensabgleich. Der Dialog enthaelt
' fremde, unfertige Objekte desselben Benutzers.
'
' Aufruf: SapGuiInaktiveMarkieren.vbs <SitzungsIndex> <pruefen|aktivieren> <Name> [...]
'=======================================================================
Dim a, app, c, s, sesIdx, modus, wanted, i, basis, tbl, pos, altpos, r, nam, kopf
Dim treffer, andere

sesIdx = CInt(WScript.Arguments(0))
modus = LCase(Trim(WScript.Arguments(1)))
Set wanted = CreateObject("Scripting.Dictionary")
For i = 2 To WScript.Arguments.Count - 1
  wanted.Add UCase(Trim(WScript.Arguments(i))), True
Next

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set c = app.Children(0)
Set s = c.Children(CLng(sesIdx))

On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
If Err.Number <> 0 Then WScript.Echo "Kein Dialog wnd[1] offen" : WScript.Quit 2
Err.Clear
s.FindById("wnd[1]/usr/tabsACT_TAB_STRIP/tabpTRANSPORT").Select
Err.Clear
On Error GoTo 0
WScript.Sleep 1200

basis = "wnd[1]/usr/tabsACT_TAB_STRIP/tabpTRANSPORT/ssubSCREEN_ACTIVATE:SAPLSEWORKINGAREA:0201/tblSAPLSEWORKINGAREAT_TRANSPORT"
Set tbl = Nothing
On Error Resume Next
Set tbl = s.FindById(basis)
Err.Clear
On Error GoTo 0
If tbl Is Nothing Then WScript.Echo "Transporttabelle nicht gefunden" : WScript.Quit 3

On Error Resume Next
s.FindById("wnd[1]/tbar[0]/btn[21]").Press
Err.Clear
On Error GoTo 0
WScript.Sleep 1200
Set tbl = s.FindById(basis)
WScript.Echo "GESAMT=" & tbl.RowCount & " SICHTBAR=" & tbl.VisibleRowCount

treffer = 0
andere = 0
pos = 0
Do
  Set tbl = s.FindById(basis)
  altpos = tbl.VerticalScrollbar.Position
  For r = 0 To tbl.VisibleRowCount - 1
    nam = ""
    On Error Resume Next
    nam = UCase(Trim(s.FindById(basis & "/txtWORK_ITEMS-OBJ_NAME[2," & r & "]").Text))
    Err.Clear
    On Error GoTo 0
    ' Leerzeilen kommen als Unterstrichreihe zurueck.
    If nam <> "" And Replace(nam, "_", "") <> "" Then
      kopf = nam
      If InStr(kopf, " ") > 0 Then kopf = Left(kopf, InStr(kopf, " ") - 1)
      If wanted.Exists(kopf) Then
        On Error Resume Next
        tbl.GetAbsoluteRow(altpos + r).Selected = True
        Err.Clear
        On Error GoTo 0
        treffer = treffer + 1
        WScript.Echo "MARKIERT    Zeile " & (altpos + r) & "  " & nam
      Else
        andere = andere + 1
        WScript.Echo "uebergangen Zeile " & (altpos + r) & "  " & nam
      End If
    End If
  Next
  If altpos + tbl.VisibleRowCount >= tbl.RowCount Then Exit Do
  On Error Resume Next
  tbl.VerticalScrollbar.Position = altpos + tbl.VisibleRowCount
  Err.Clear
  On Error GoTo 0
  Set tbl = s.FindById(basis)
  ' Rueckt die Position nicht vor, ist die Liste zu Ende.
  If tbl.VerticalScrollbar.Position <= altpos Then Exit Do
Loop

WScript.Echo "TREFFER=" & treffer & " FREMD=" & andere
If treffer = 0 Then WScript.Echo "Nichts markiert, kein Aktivieren." : WScript.Quit 4
If modus <> "aktivieren" Then WScript.Echo "Nur geprueft, nicht aktiviert." : WScript.Quit 0

s.FindById("wnd[1]/tbar[0]/btn[0]").Press
WScript.Sleep 5000

On Error Resume Next
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar").Text
WScript.Echo "TYP=" & s.FindById("wnd[0]/sbar").MessageType
Err.Clear
WScript.Echo "FOLGEDIALOG=" & s.FindById("wnd[1]").Text
If Err.Number <> 0 Then WScript.Echo "FOLGEDIALOG=(keiner)"
On Error GoTo 0
