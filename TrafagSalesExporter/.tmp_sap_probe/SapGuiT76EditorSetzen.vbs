' T76: Inhalt des ABAP-Editors der eigenen Sitzung durch eine UTF-8-Datei ersetzen (SelectAll + ReplaceSelection).
' Sichert NICHT. Angelegt 2026-11-06 (IC Staffelbasis, VOFM-Editor).
' Aufruf: SapGuiT76EditorSetzen.vbs <ses> <Datei> [EditorId]
Option Explicit
Dim app, con, i, ses, fso, idFile, eigeneId, ed, edId, st, quelle
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
Set st = CreateObject("ADODB.Stream")
st.Type = 2: st.Charset = "utf-8": st.Open
st.LoadFromFile WScript.Arguments(1)
quelle = st.ReadText: st.Close
quelle = Replace(quelle, vbCrLf, vbLf)
quelle = Replace(quelle, vbLf, vbCrLf)
edId = "wnd[0]/usr/cntlEDITOR/shellcont/shell"
If WScript.Arguments.Count > 2 Then edId = WScript.Arguments(2)
Set ed = ses.findById(edId)
ed.SelectAll
ed.ReplaceSelection quelle
WScript.Echo "ZEILEN_NACHHER=" & ed.GetLineCount()
WScript.Echo "SBAR(" & ses.findById("wnd[0]/sbar").MessageType & ")=" & ses.findById("wnd[0]/sbar").Text
