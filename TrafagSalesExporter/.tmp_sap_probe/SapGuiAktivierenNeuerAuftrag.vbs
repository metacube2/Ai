Option Explicit
'=======================================================================
' Aktiviert das Objekt im aktuellen SE11-Bild (tbar[1]/btn[27]), ordnet das Paket zu und
' beantwortet die Transportabfrage. Ist <Auftrag> "NEU", wird ein neuer Workbench-Auftrag mit
' <Kurztext> angelegt; sonst wird der genannte Auftrag ausdruecklich eingetragen (nie der Vorschlag).
'
' Aufruf: SapGuiAktivierenNeuerAuftrag.vbs <Sitzung> <Paket> <Auftrag|NEU> [<Kurztext>]
'=======================================================================
Dim a, app, s, paket, auftrag, text, k, w
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
paket = WScript.Arguments(1)
auftrag = UCase(WScript.Arguments(2))
If WScript.Arguments.Count > 3 Then text = WScript.Arguments(3)
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2

s.FindById("wnd[0]/tbar[1]/btn[27]").Press
WScript.Sleep 2500

On Error Resume Next
Set w = Nothing
Set w = s.FindById("wnd[1]")
On Error GoTo 0
If Not w Is Nothing Then
  If InStr(1, w.Text, "Objektkatalog", vbTextCompare) > 0 Then
    s.FindById("wnd[1]/usr/ctxtKO007-L_DEVCLASS").Text = paket
    s.FindById("wnd[1]/tbar[0]/btn[0]").Press
    WScript.Sleep 2500
  End If
End If

On Error Resume Next
Set w = Nothing
Set w = s.FindById("wnd[1]")
On Error GoTo 0
If Not w Is Nothing Then
  If InStr(1, w.Text, "Auftrag", vbTextCompare) > 0 Then
    WScript.Echo "VORSCHLAG=" & s.FindById("wnd[1]/usr/ctxtKO008-TRKORR").Text
    If auftrag = "NEU" Then
      s.FindById("wnd[1]/tbar[0]/btn[8]").Press
      WScript.Sleep 2000
      s.FindById("wnd[2]/usr/txtKO013-AS4TEXT").Text = text
      s.FindById("wnd[2]/tbar[0]/btn[0]").Press
      WScript.Sleep 2000
    Else
      s.FindById("wnd[1]/usr/ctxtKO008-TRKORR").Text = auftrag
    End If
    WScript.Echo "AUFTRAG=" & s.FindById("wnd[1]/usr/ctxtKO008-TRKORR").Text
    s.FindById("wnd[1]/tbar[0]/btn[0]").Press
    WScript.Sleep 4000
  End If
End If

On Error Resume Next
For k = 1 To 2
  Err.Clear
  WScript.Echo "wnd[" & k & "]=" & s.FindById("wnd[" & k & "]").Text
  If Err.Number <> 0 Then Exit For
Next
Err.Clear
WScript.Echo "MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
