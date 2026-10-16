Option Explicit
' Redefiniert eine geerbte Methode in SE24. Eintragen, Fokus setzen und den
' Knopf druecken passieren in EINER COM-Sitzung: getrennte Prozesse loesen
' dazwischen ein Enter aus, und SAP wertet den Namen dann als NEUE Methode
' ("Es wird bereits ein(e) Komponente ... geerbt").
' Aufruf: SapGuiRedefinieren.vbs <SitzungsIndex> <Methodenname>
Dim a, app, c, s, sesIdx, sub_, tab, r, frei, name, mtd

sesIdx = CInt(WScript.Arguments(0))
mtd = UCase(WScript.Arguments(1))

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set c = app.Children(0)
Set s = c.Children(CLng(sesIdx))

sub_ = "wnd[0]/usr/tabsCTS/tabpTAB_MTD/ssubCSS:SAPLSEOD:0253"
tab = sub_ & "/tblSAPLSEODMC"

On Error Resume Next
Set tab = s.FindById(tab)
If Err.Number <> 0 Then
  WScript.Echo "Methodentabelle nicht gefunden: " & Err.Description
  WScript.Quit 2
End If
Err.Clear
On Error GoTo 0

' Erste leere Zeile suchen
frei = -1
For r = 0 To tab.VisibleRowCount - 1
  name = ""
  On Error Resume Next
  name = Trim(s.FindById(sub_ & "/tblSAPLSEODMC/txtDY_0253-CPDNAME[0," & r & "]").Text)
  Err.Clear
  On Error GoTo 0
  If name = "" And frei < 0 Then frei = r
Next

If frei < 0 Then
  WScript.Echo "Keine freie Zeile sichtbar"
  WScript.Quit 3
End If
WScript.Echo "FREIE_ZEILE=" & frei

' Name eintragen, Cursor daraufsetzen, Redefinieren druecken - ohne Enter dazwischen
s.FindById(sub_ & "/tblSAPLSEODMC/txtDY_0253-CPDNAME[0," & frei & "]").Text = mtd
s.FindById(sub_ & "/tblSAPLSEODMC/txtDY_0253-CPDNAME[0," & frei & "]").SetFocus
s.FindById(sub_ & "/btnPUSH_REDEFINE").Press
WScript.Sleep 2000

On Error Resume Next
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar").Text
WScript.Echo "TYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
If Err.Number <> 0 Then WScript.Echo "DIALOG=(keiner)"
On Error GoTo 0
