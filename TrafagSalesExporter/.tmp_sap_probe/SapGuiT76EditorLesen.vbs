' T76: ABAP-Editor der eigenen Sitzung zeilenweise lesen (GetLineCount/GetLineText), nur lesend.
' Angelegt 2026-11-06 (IC Staffelbasis, VOFM-Editor). Aufruf: SapGuiT76EditorLesen.vbs <ses> [EditorId]
Option Explicit
Dim app, con, i, ses, fso, idFile, eigeneId, ed, n, edId
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
edId = "wnd[0]/usr/cntlEDITOR/shellcont/shell"
If WScript.Arguments.Count > 1 Then edId = WScript.Arguments(1)
Set ed = ses.findById(edId)
n = ed.GetLineCount()
WScript.Echo "ZEILEN=" & n
For i = 1 To n
  WScript.Echo i & ": " & ed.GetLineText(CLng(i))
Next
