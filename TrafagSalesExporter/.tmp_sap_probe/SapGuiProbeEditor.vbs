Option Explicit
' Probiert die Lesemoeglichkeiten des ABAP-Editors durch, ohne etwas zu schreiben.

Dim a, app, c, s, i, j, found, ed, v

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
found = False
For i = 0 To app.Children.Count - 1
  Set c = app.Children(CLng(i))
  For j = 0 To c.Children.Count - 1
    Set s = c.Children(CLng(j))
    If UCase(s.Info.Transaction) = "SE19" And s.Info.SystemName = "T76" And s.Info.Client = "100" Then
      found = True
      Exit For
    End If
  Next
  If found Then Exit For
Next
If Not found Then WScript.Quit 2

WScript.Echo "FENSTER=" & s.FindById("wnd[0]").Text
Set ed = s.FindById("wnd[0]/usr/subEDITORSUBSCREEN:SAPLEDITOR_START:8430/cntlEDITOR/shellcont/shell")

On Error Resume Next

Err.Clear
v = ed.GetUnprotectedTextPart(0)
If Err.Number = 0 Then
  WScript.Echo "UNPROTECTED_0_LEN=" & Len(v)
  WScript.Echo "UNPROTECTED_0=[" & v & "]"
Else
  WScript.Echo "GetUnprotectedTextPart(0) FEHLER: " & Err.Description
End If

Err.Clear
WScript.Echo "NUM_UNPROTECTED=" & ed.NumberOfUnprotectedTextParts
If Err.Number <> 0 Then WScript.Echo "NumberOfUnprotectedTextParts FEHLER: " & Err.Description

Err.Clear
ed.SelectAll
If Err.Number = 0 Then
  WScript.Echo "SELECTALL=ok"
Else
  WScript.Echo "SelectAll FEHLER: " & Err.Description
End If

Err.Clear
v = ed.SelectedText
If Err.Number = 0 Then
  WScript.Echo "SELECTEDTEXT_LEN=" & Len(v)
  WScript.Echo "SELECTEDTEXT=[" & v & "]"
Else
  WScript.Echo "SelectedText FEHLER: " & Err.Description
End If

WScript.Quit 0
