' Variante fuer DATENBANKTABELLEN (2026-10-15, ZPOWERBI_KDAUF): Subscreen 2201, Spalten DD03D-FIELDNAME[0] und DD03D-ROLLNAME[3].
Option Explicit
' Traegt Felder in die SE11-Strukturpflege ein: Feldname und Komponententyp.
' Die Tabelle zeigt nur wenige Zeilen; deshalb wird nach jedem Block
' gescrollt statt blind auf Zeilenindizes zu setzen.
'
' Aufruf: SapGuiStrukturFelder.vbs <SitzungsIndex> <Datei> [ErsteZeile]
' Datei: je Zeile "FELDNAME;KOMPONENTENTYP"

Dim a, app, c, s, sesIdx, fso, datei, txt, zeilen, i, r
Dim basis, tbl, feldId, typId, paar, sichtbar, geschrieben

sesIdx = CInt(WScript.Arguments(0))
datei = WScript.Arguments(1)

Set fso = CreateObject("Scripting.FileSystemObject")
Set txt = fso.OpenTextFile(datei, 1)
Dim inhalt
inhalt = txt.ReadAll
inhalt = Replace(inhalt, vbCrLf, vbLf)
inhalt = Replace(inhalt, vbCr, vbLf)
zeilen = Split(inhalt, vbLf)
txt.Close

Set a = GetObject("SAPGUI")
Set app = a.GetScriptingEngine
' Verbindung T76 suchen statt fest die erste zu nehmen (2026-10-15: P76 war Verbindung 0)
Dim ci_, c_
Set c = Nothing
For ci_ = 0 To app.Children.Count - 1
  Set c_ = app.Children(CLng(ci_))
  If c_.Children.Count > 0 Then
    If c_.Children(0).Info.SystemName = "T76" Then Set c = c_
  End If
Next
If c Is Nothing Then
  WScript.Echo "ABBRUCH: keine T76-Verbindung offen"
  WScript.Quit 2
End If
Set s = c.Children(CLng(sesIdx))

basis = "wnd[0]/usr/tabsTAB_STRIP/tabpDEF/ssubTS_SCREEN:SAPLSD41:2201/tblSAPLSD41TC0"
Set tbl = s.FindById(basis)
sichtbar = tbl.VisibleRowCount
WScript.Echo "Sichtbare Zeilen der Tabelle: " & sichtbar

geschrieben = 0
r = 0
Dim zielZeile
zielZeile = CLng(0)
' Optionales drittes Argument: erste Zielzeile (Felder ANHAENGEN statt ab Zeile 0 ueberschreiben, 2026-10-07).
If WScript.Arguments.Count > 2 Then zielZeile = CLng(WScript.Arguments(2))
For i = 0 To UBound(zeilen)
  If Trim(zeilen(i)) <> "" And Left(Trim(zeilen(i)), 1) <> "#" Then
    paar = Split(Trim(zeilen(i)), ";")

    ' Ist die Zielzeile nicht mehr sichtbar, blaettern. Die Zeile im Fenster wird
    ' aus der TATSAECHLICHEN Scrollposition berechnet: SAP laesst nicht beliebig
    ' weit blaettern, und die fruehere Annahme "Position + sichtbar, dann Zeile 0"
    ' hat am 2026-09-30 bei ZSTR_HR_KPI die neunte Zeile (TEILK) mit der zehnten
    ' ueberschrieben.
    If zielZeile - tbl.VerticalScrollbar.Position >= sichtbar Then
      tbl.VerticalScrollbar.Position = zielZeile
      Set tbl = s.FindById(basis)
    End If
    r = zielZeile - tbl.VerticalScrollbar.Position

    feldId = basis & "/txtDD03D-FIELDNAME[0," & r & "]"
    typId  = basis & "/ctxtDD03D-ROLLNAME[3," & r & "]"

    On Error Resume Next
    s.FindById(feldId).Text = paar(0)
    If Err.Number <> 0 Then
      WScript.Echo "FEHLER Feldname " & paar(0) & ": " & Err.Description
      WScript.Quit 3
    End If
    s.FindById(typId).Text = paar(1)
    If Err.Number <> 0 Then
      WScript.Echo "FEHLER Typ " & paar(1) & " zu " & paar(0) & ": " & Err.Description
      WScript.Quit 4
    End If
    On Error GoTo 0

    geschrieben = geschrieben + 1
    zielZeile = zielZeile + 1
  End If
Next

WScript.Echo "GESCHRIEBEN=" & geschrieben
s.FindById("wnd[0]").SendVKey 0
WScript.Sleep 1500
On Error Resume Next
WScript.Echo "STATUS=" & s.FindById("wnd[0]/sbar").Text
WScript.Echo "TYP=" & s.FindById("wnd[0]/sbar").MessageType
On Error GoTo 0
