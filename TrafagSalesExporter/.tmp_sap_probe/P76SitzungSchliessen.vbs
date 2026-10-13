' Schliesst die eigene P76-Lesesitzung, nur wenn sie SE16/VA43/ME33K zeigt.
' Aufruf: cscript //nologo P76SitzungSchliessen.vbs /app/con[0]/ses[5]
Option Explicit
Dim a, app, s, id
id = WScript.Arguments(0)
Set a = GetObject("SAPGUI") : Set app = a.GetScriptingEngine
Set s = app.FindById(id)
If s.Info.SystemName = "P76" And (s.Info.Transaction = "SE16" Or s.Info.Transaction = "VA43" Or s.Info.Transaction = "ME33K") Then
  WScript.Echo "SCHLIESSE " & s.Id & " " & s.Info.Transaction
  s.Parent.CloseSession s.Id
Else
  WScript.Echo "NICHT_GESCHLOSSEN " & s.Info.Transaction
End If
