Option Explicit
' P76/100, NUR LESEN: ZC22 (ZM_OFFENE_FAUF) in einer EIGENEN neuen Sitzung ausfuehren, die Liste ueber alle Seiten
' ausgeben und die Sitzung schliessen. Ingos Sitzungen werden nicht angefasst. Angelegt 2026-11-09 fuer den Abgleich
' PPD-Verzug/Vorrat (Freigabe Ingo „darf ich ZC22 in P76 lesend laufen lassen“ – „ja“).
' ZC22 liest nur (Report ohne Verbuchung).
'
' Aufruf: P76Zc22Lesen.vbs <DispoVon> <DispoBis> <EckendterminBis TT.MM.JJJJ oder leer> <MatVon> <MatBis> [MaxSeiten]
Dim a, app, c, s, i, k, before, maxSeiten, seite, usr, ch, id, p, q, spalte, zeile, zeilen(), z, txt, letzte, erste
Set a = GetObject("SAPGUI") : Set app = a.GetScriptingEngine
Set c = Nothing
For i = 0 To app.Children.Count - 1
  If app.Children(CLng(i)).Children.Count > 0 Then
    If app.Children(CLng(i)).Children(0).Info.SystemName = "P76" And app.Children(CLng(i)).Children(0).Info.Client = "100" Then Set c = app.Children(CLng(i))
  End If
Next
If c Is Nothing Then WScript.Echo "KEINE_P76" : WScript.Quit 2
maxSeiten = 200
If WScript.Arguments.Count > 5 Then maxSeiten = CInt(WScript.Arguments(5))

before = c.Children.Count
c.Children(CLng(0)).CreateSession
For k = 1 To 40
  WScript.Sleep 300
  If c.Children.Count > before Then Exit For
Next
If c.Children.Count <= before Then WScript.Echo "KEINE_NEUE_SITZUNG" : WScript.Quit 3
Set s = c.Children(CLng(c.Children.Count - 1))
WScript.Sleep 1000
WScript.Echo "SITZUNG=" & s.Id
If s.Info.SystemName <> "P76" Then WScript.Echo "ABBRUCH nicht P76" : WScript.Quit 2

s.FindById("wnd[0]/tbar[0]/okcd").Text = "/nZC22"
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 2000
If s.Info.Program <> "ZM_OFFENE_FAUF" Then WScript.Echo "ABBRUCH Programm " & s.Info.Program : s.Parent.CloseSession s.Id : WScript.Quit 4
s.FindById("wnd[0]/usr/ctxtS_DISPO-LOW").Text = WScript.Arguments(0)
s.FindById("wnd[0]/usr/ctxtS_DISPO-HIGH").Text = WScript.Arguments(1)
s.FindById("wnd[0]/usr/ctxtS_GLTRP-LOW").Text = ""
s.FindById("wnd[0]/usr/ctxtS_GLTRP-HIGH").Text = WScript.Arguments(2)
s.FindById("wnd[0]/usr/ctxtS_MATNR-LOW").Text = WScript.Arguments(3)
s.FindById("wnd[0]/usr/ctxtS_MATNR-HIGH").Text = WScript.Arguments(4)
s.FindById("wnd[0]/usr/chkP_PLAF").Selected = True
On Error Resume Next
s.FindById("wnd[0]/usr/chkP_PEG").Selected = False
s.FindById("wnd[0]/usr/chkP_ELIKZ").Selected = False
On Error GoTo 0
s.FindById("wnd[0]/tbar[1]/btn[8]").Press
WScript.Sleep 3000
WScript.Echo "TITEL=" & s.FindById("wnd[0]").Text & " SBAR=" & s.FindById("wnd[0]/sbar").Text

On Error Resume Next
s.FindById("wnd[0]").SendVKey 80
On Error GoTo 0
WScript.Sleep 500
letzte = ""
For seite = 1 To maxSeiten
  Set usr = s.FindById("wnd[0]/usr")
  ReDim zeilen(200)
  For i = 0 To 200 : zeilen(i) = "" : Next
  For i = 0 To usr.Children.Count - 1
    Set ch = usr.Children(CLng(i))
    id = ch.Id
    p = InStrRev(id, "[") : q = InStrRev(id, "]")
    If p > 0 And q > p And (ch.Type = "GuiLabel" Or ch.Type = "GuiTextField") Then
      spalte = CInt(Split(Mid(id, p + 1, q - p - 1), ",")(0))
      zeile = CInt(Split(Mid(id, p + 1, q - p - 1), ",")(1))
      If zeile <= 200 Then
        txt = ch.Text
        z = zeilen(zeile)
        If Len(z) < spalte Then z = z & Space(spalte - Len(z))
        zeilen(zeile) = Left(z, spalte) & txt & Mid(z, spalte + Len(txt) + 1)
      End If
    End If
  Next
  erste = ""
  For i = 0 To 200
    If Trim(zeilen(i)) <> "" Then erste = erste & zeilen(i) & "|"
  Next
  If erste = letzte Then Exit For
  For i = 0 To 200
    If Trim(zeilen(i)) <> "" Then WScript.Echo RTrim(zeilen(i))
  Next
  WScript.Echo "--- Seite " & seite
  letzte = erste
  s.FindById("wnd[0]").SendVKey 82
  WScript.Sleep 400
Next
If WScript.Arguments.Count > 6 Then WScript.Echo "OFFEN " & s.Id : WScript.Quit 0
WScript.Echo "SCHLIESSE " & s.Id
s.Parent.CloseSession s.Id
