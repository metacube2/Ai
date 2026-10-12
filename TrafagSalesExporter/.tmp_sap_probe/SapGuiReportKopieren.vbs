Option Explicit
' Kopiert einen Report in SE38 (Kopieren, Strg+F5) als LOKALES Objekt ($TMP, ohne Transport).
' Schritt 1: Quelle eintragen, Kopieren druecken, Ziel eintragen, bestaetigen.
' Danach steht der Dialog "Programm ... kopieren" mit den Ankreuzfeldern offen; den beantwortet
' Schritt 2 (Modus "bestaetigen"), dann folgt der Objektkatalog -> SapGuiLokalesObjekt.vbs.
' Angelegt 2026-10-12 (ZM_OFFENE_KDAUF_REF/OPT).
'
' Aufruf: SapGuiReportKopieren.vbs <SitzungsIndex> start <Quelle> <Ziel>
'         SapGuiReportKopieren.vbs <SitzungsIndex> bestaetigen

Dim a, app, s, i, k
Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
Set s = app.Children(0).Children(CLng(WScript.Arguments(0)))
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2

Sub Melde(stufe)
  On Error Resume Next
  For k = 1 To 3
    Err.Clear
    WScript.Echo stufe & " wnd[" & k & "]=" & s.FindById("wnd[" & k & "]").Text
    If Err.Number <> 0 Then Exit For
  Next
  Err.Clear
  WScript.Echo stufe & " MELDUNG=" & s.FindById("wnd[0]/sbar/pane[0]").Text
  On Error GoTo 0
End Sub

If WScript.Arguments(1) = "start" Then
  s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nSE38"
  s.FindById("wnd[0]").SendVKey 0
  WScript.Sleep 2000
  s.FindById("wnd[0]/usr/ctxtRS38M-PROGRAMM").Text = WScript.Arguments(2)
  s.FindById("wnd[0]/tbar[1]/btn[29]").Press
  WScript.Sleep 2500
  Melde "NACH_KOPIEREN"
  ' Zielname: das zweite Eingabefeld im Dialog (Name schwankt je Release, deshalb suchen)
  Dim w, c, gesetzt
  Set w = s.FindById("wnd[1]/usr")
  gesetzt = False
  For i = 0 To w.Children.Count - 1
    Set c = w.Children(CLng(i))
    WScript.Echo "FELD " & c.Id & " TYP=" & c.Type & " TEXT=" & c.Text
  Next
  On Error Resume Next
  s.FindById("wnd[1]/usr/txtPROGRAM_GLOBAL").Text = WScript.Arguments(3)
  If Err.Number = 0 Then gesetzt = True
  Err.Clear
  If Not gesetzt Then
    s.FindById("wnd[1]/usr/ctxtRS38M-PROGRAMM_N").Text = WScript.Arguments(3)
    If Err.Number = 0 Then gesetzt = True
    Err.Clear
  End If
  On Error GoTo 0
  WScript.Echo "ZIEL_GESETZT=" & gesetzt
  If gesetzt Then
    s.FindById("wnd[1]").SendVKey 0
    WScript.Sleep 2500
    Melde "NACH_ZIEL"
  End If
Else
  ' Dialog mit den Ankreuzfeldern: alles auflisten, dann mit Enter kopieren
  Set w = s.FindById("wnd[1]/usr")
  For i = 0 To w.Children.Count - 1
    Set c = w.Children(CLng(i))
    If c.Type = "GuiCheckBox" Then
      WScript.Echo "KREUZ " & c.Id & " " & c.Text & " =" & c.Selected
    End If
  Next
  If WScript.Arguments.Count > 2 Then
    If WScript.Arguments(2) = "los" Then
      s.FindById("wnd[1]/usr/chkWITH_CUA_GLOBAL").Selected = True
      s.FindById("wnd[1]/usr/chkWITH_DYNPRO_GLOBAL").Selected = True
      s.FindById("wnd[1]/tbar[0]/btn[0]").Press
      WScript.Sleep 3000
      Melde "NACH_BESTAETIGEN"
    End If
  End If
End If
