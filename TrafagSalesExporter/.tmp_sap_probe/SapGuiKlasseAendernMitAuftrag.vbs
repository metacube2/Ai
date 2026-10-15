Option Explicit
' Oeffnet eine Klasse in SE24 im Aendern-Modus (F6) und beantwortet die
' Transportabfrage mit einem AUSDRUECKLICH genannten Auftrag, nie mit dem
' Vorschlag. Danach steht der Reiter Methoden offen.
'
' Aufruf: SapGuiKlasseAendernMitAuftrag.vbs <SitzungsIndex> <Klasse> <Auftrag>

Dim a, app, s, klasse, auftrag, k, vorschlag
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
' Verbindung T76 suchen statt fest die erste zu nehmen (2026-10-15: P76 war Verbindung 0)
Dim ci_, c_, cT76_
Set cT76_ = Nothing
For ci_ = 0 To app.Children.Count - 1
  Set c_ = app.Children(CLng(ci_))
  If c_.Children.Count > 0 Then
    If c_.Children(0).Info.SystemName = "T76" Then Set cT76_ = c_
  End If
Next
If cT76_ Is Nothing Then
  WScript.Echo "ABBRUCH: keine T76-Verbindung offen"
  WScript.Quit 2
End If
Set s = cT76_.Children(CLng(WScript.Arguments(0)))
klasse = UCase(WScript.Arguments(1))
auftrag = UCase(WScript.Arguments(2))

If s.Info.SystemName <> "T76" Then
  WScript.Echo "ABBRUCH: Sitzung ist " & s.Info.SystemName & ", nicht T76."
  WScript.Quit 2
End If

s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE24"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 1500
s.FindById("wnd[0]/usr/ctxtSEOCLASS-CLSNAME").Text = klasse
s.FindById("wnd[0]").SendVKey 6
WScript.Sleep 2500

On Error Resume Next
Dim dlgText
dlgText = s.FindById("wnd[1]").Text
If Err.Number = 0 Then
  On Error GoTo 0
  If InStr(1, dlgText, "Auftrag", vbTextCompare) = 0 Then
    WScript.Echo "ABBRUCH: unerwarteter Dialog: " & dlgText
    WScript.Quit 3
  End If
  vorschlag = s.FindById("wnd[1]/usr/ctxtKO008-TRKORR").Text
  WScript.Echo "VORSCHLAG=" & vorschlag
  s.FindById("wnd[1]/usr/ctxtKO008-TRKORR").Text = auftrag
  s.FindById("wnd[1]/tbar[0]/btn[0]").Press
  WScript.Sleep 2500
Else
  Err.Clear
  On Error GoTo 0
  WScript.Echo "KEINE_TRANSPORTABFRAGE"
End If

On Error Resume Next
For k = 1 To 2
  Err.Clear
  WScript.Echo "wnd[" & k & "]=" & s.FindById("wnd[" & k & "]").Text
  If Err.Number <> 0 Then Exit For
Next
Err.Clear
s.FindById("wnd[0]/usr/tabsCTS/tabpTAB_MTD").Select
WScript.Sleep 1500
WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
