Option Explicit
' Legt einen Planauftrag in MD11 an. Nur T76/100.
'
' Aufruf: SapGuiMd11Anlegen.vbs <matnr> <werk> <menge> <endtermin TT.MM.JJJJ>
'
' Vorbedingung: MD11 steht auf dem Bild "Planauftrag anlegen: Lagerauftrag",
' also Profil LA bereits gewaehlt.
'
' Der Planauftrag ist die Vorstufe fuer die Umsetzungswege MD04, CO40 und CO41.
' Welches Profil dem realen Ablauf entspricht, ist eine Stammdatenfrage; LA ist
' hier bewusst als neutraler technischer Test gewaehlt.

Dim matnr, werk, menge, ende, s, b1, b2, runde, titel

matnr = WScript.Arguments(0)
werk  = WScript.Arguments(1)
menge = WScript.Arguments(2)
ende  = WScript.Arguments(3)

Function SitzungHolen(tx)
  Dim a, app, c, s2, i, j
  Set a = GetObject("SAPGUI")
  Set app = a.GetScriptingEngine
  For i = 0 To app.Children.Count - 1
    Set c = app.Children(CLng(i))
    For j = 0 To c.Children.Count - 1
      Set s2 = c.Children(CLng(j))
      If UCase(s2.Info.Transaction) = UCase(tx) And s2.Info.SystemName = "T76" And s2.Info.Client = "100" Then
        Set SitzungHolen = s2
        Exit Function
      End If
    Next
  Next
  WScript.Quit 2
End Function

Set s = SitzungHolen("MD11")

b1 = "wnd[0]/usr/tabsTABTC/tabpTAB01/ssubINCLUDE1XX:SAPLM61O:0711/subINCLUDE711_1:SAPLM61O:0802"
b2 = "wnd[0]/usr/tabsTABTC/tabpTAB01/ssubINCLUDE1XX:SAPLM61O:0711/subINCLUDE711_2:SAPLM61O:0810"

s.FindById("wnd[0]/usr/ctxtPLAF-MATNR").Text = matnr
s.FindById("wnd[0]/usr/ctxtPLAF-BERID").Text = werk   ' Dispobereich entspricht dem Werk
s.FindById(b2 & "/ctxtPLAF-PWWRK").Text = werk
s.FindById(b1 & "/txtPLAF-GSMNG").Text = menge
s.FindById(b1 & "/ctxtPLAF-PEDTR").Text = ende
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 3000

' Zwischendialoge melden, aber nicht blind wegklicken.
For runde = 1 To 6
  titel = ""
  On Error Resume Next
  titel = s.FindById("wnd[1]").Text
  On Error GoTo 0
  If titel = "" Then Exit For
  WScript.Echo "  Dialog: " & titel
  On Error Resume Next
  s.FindById("wnd[1]/tbar[0]/btn[0]").Press
  On Error GoTo 0
  WScript.Sleep 1500
Next

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text

' Sichern
s.FindById("wnd[0]/tbar[0]/btn[11]").Press
WScript.Sleep 3000
WScript.Echo "NACH_SICHERN=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNGSTYP=" & s.FindById("wnd[0]/sbar").MessageType
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
On Error Resume Next
WScript.Echo "DIALOG=" & s.FindById("wnd[1]").Text
WScript.Quit 0
