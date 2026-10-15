Option Explicit
' Liest eine klassische ABAP-Liste ueber alle Seiten (Bild ab, VKey 82) und gibt sie zeilenweise aus.
' Get-SapList.ps1 sieht nur den sichtbaren Ausschnitt. Angelegt 2026-10-12 (ZM_OFFENE_KDAUF_CMP).
'
' Aufruf: SapGuiListeAlleSeiten.vbs <SitzungsIndex> [MaxSeiten]

Dim a, app, s, usr, maxSeiten, seite, i, c, id, p, q, spalte, zeile, zeilen(), z, txt, letzte, erste
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
If s.Info.SystemName <> "T76" Then WScript.Echo "ABBRUCH: nicht T76" : WScript.Quit 2
If WScript.Arguments.Count > 1 Then maxSeiten = CInt(WScript.Arguments(1)) Else maxSeiten = 50

' an den Anfang der Liste
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
    Set c = usr.Children(CLng(i))
    id = c.Id
    p = InStrRev(id, "[")
    q = InStrRev(id, "]")
    If p > 0 And q > p And (c.Type = "GuiLabel" Or c.Type = "GuiTextField") Then
      spalte = CInt(Split(Mid(id, p + 1, q - p - 1), ",")(0))
      zeile = CInt(Split(Mid(id, p + 1, q - p - 1), ",")(1))
      If zeile <= 200 Then
        txt = c.Text
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
  letzte = erste
  s.FindById("wnd[0]").SendVKey 82
  WScript.Sleep 400
Next
