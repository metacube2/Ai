' T76: GET im Gateway Client (/IWFND/GW_CLIENT) der EIGENEN Sitzung, nur lesend.
' Prueft die gemerkte Id aus t76_eigene_sitzung.txt (wie SapGuiT76EditorLesen.vbs), bricht bei ses[0] ab.
' Gibt Dauer, Statuscode, Begruendung und content-length aus und schreibt den Antwortrumpf (UTF-8) in <Datei>.
' Angelegt 2026-11-26 (Shopfloor Welle 2, saptasks/shopfloor_sap/PLAN_WELLE2_2026-11-25.md Abschnitt 11).
' Aufruf: SapGuiT76GatewayGet.vbs <ses> <RequestUri> <Datei>   (Aufruf nur aus PowerShell, nie aus Git Bash)
Option Explicit
Dim app, con, i, ses, fso, idFile, eigeneId, ed, t0, g, r, nm, sh, txt, aus, st
Set app = GetObject("SAPGUI").GetScriptingEngine
Set fso = CreateObject("Scripting.FileSystemObject")
idFile = fso.BuildPath(fso.GetParentFolderName(WScript.ScriptFullName), "t76_eigene_sitzung.txt")
Set con = Nothing
For i = 0 To app.Children.Count - 1
  If app.Children(CLng(i)).Children.Count > 0 Then
    If app.Children(CLng(i)).Children(0).Info.SystemName = "T76" And app.Children(CLng(i)).Children(0).Info.Client = "100" Then Set con = app.Children(CLng(i))
  End If
Next
If con Is Nothing Then WScript.Echo "Keine T76/100-Verbindung": WScript.Quit 1
eigeneId = Trim(fso.OpenTextFile(idFile).ReadLine)
If CLng(WScript.Arguments(0)) = 0 Then WScript.Echo "ABBRUCH: ses[0]": WScript.Quit 1
Set ses = con.Children(CLng(WScript.Arguments(0)))
If ses.Id <> eigeneId Then WScript.Echo "ABBRUCH: nicht die eigene Sitzung": WScript.Quit 1
If InStr(ses.findById("wnd[0]").Text, "Gateway Client") = 0 Then WScript.Echo "ABBRUCH: Gateway Client ist nicht das aktive Bild": WScript.Quit 2
ses.findById("wnd[0]/usr/radRB_GET").Select
Set ed = ses.findById("wnd[0]/usr/cntlURI_AREA/shellcont/shell")
ed.Text = WScript.Arguments(1)
t0 = Timer
ses.findById("wnd[0]").SendVKey 8
WScript.Echo "DAUER_S=" & FormatNumber(Timer - t0, 2)
On Error Resume Next
Set g = ses.findById("wnd[0]/usr/cntlGUI_AREA/shellcont/shell/shellcont[1]/shell")
For r = 0 To g.RowCount - 1
  nm = g.GetCellValue(r, "NAME")
  If nm = "~status_code" Or nm = "~status_reason" Or nm = "content-length" Then WScript.Echo nm & "=" & g.GetCellValue(r, "VALUE")
Next
Err.Clear
' Antwortrumpf: AbapEditor in shellcont[3] (Anfragerumpf in shellcont[2]); .Text liefert nichts, deshalb zeilenweise
' ueber GetLineCount/GetLineText (gemessen 26.11.: ShopRmSet 20 Zeilen JSON)
aus = ""
Dim n, k, cand
For Each cand In Array(3, 2)
  txt = ""
  Set sh = Nothing
  Set sh = ses.findById("wnd[0]/usr/cntlGUI_AREA/shellcont/shell/shellcont[" & cand & "]/shell")
  If Not sh Is Nothing Then
    n = sh.GetLineCount()
    For k = 1 To n
      txt = txt & sh.GetLineText(CLng(k)) & vbLf
    Next
  End If
  Err.Clear
  If Len(Trim(Replace(txt, vbLf, ""))) > Len(Trim(Replace(aus, vbLf, ""))) Then aus = txt
Next
On Error GoTo 0
Set st = CreateObject("ADODB.Stream")
st.Type = 2: st.Charset = "utf-8": st.Open
st.WriteText aus
st.SaveToFile WScript.Arguments(2), 2
st.Close
WScript.Echo "RUMPF_ZEICHEN=" & Len(aus) & " DATEI=" & WScript.Arguments(2)
