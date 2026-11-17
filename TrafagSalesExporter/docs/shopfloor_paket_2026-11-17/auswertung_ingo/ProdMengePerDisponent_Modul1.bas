Attribute VB_Name = "Modul1"
Sub StartSAPDataimport()

DBName = "Provider=SQLOLEDB;Server=trch-app-mes.sqldb.trafagch.local;Database=TMS_MES;integrated security=sspi"
Set oDB = New ADODB.Connection
oDB.Open DBName
   
Set qt = New ADODB.Recordset
qt.Open "EXEC msdb.dbo.sp_start_job 'SAPTableImportP76';", oDB, adOpenForwardOnly, adLockReadOnly
'qt.Close

MsgBox "Auftrag gestartet"

End Sub


Sub StartRückmeldungenTeilieferung()

DBName = "Provider=SQLOLEDB;Server=trch-app-mes.sqldb.trafagch.local;Database=TMS_MES;integrated security=sspi"
Set oDB = New ADODB.Connection
oDB.Open DBName
   
Set qt = New ADODB.Recordset
qt.Open "EXEC msdb.dbo.sp_start_job 'SAPTimeticketP76EndOfDay';", oDB, adOpenForwardOnly, adLockReadOnly
'qt.Close

MsgBox "Auftrag gestartet"

End Sub

Sub StartRückmeldungen()

DBName = "Provider=SQLOLEDB;Server=trch-app-mes.sqldb.trafagch.local;Database=TMS_MES;integrated security=sspi"
Set oDB = New ADODB.Connection
oDB.Open DBName
   
Set qt = New ADODB.Recordset
qt.Open "EXEC msdb.dbo.sp_start_job 'SAPTimeticketP76';", oDB, adOpenForwardOnly, adLockReadOnly
'qt.Close

MsgBox "Auftrag gestartet"
End Sub


Sub ProdMenge()
Range("A10:AA10000").Value = ""
Application.Calculation = xlCalculationManual
If (Range("Monat") > 12 Or Not IsNumeric(Range("Monat"))) Or Range("Monat") <= 0 _
Or (Range("Jahr") > 2050 Or Not IsNumeric(Range("Jahr"))) Or Range("Jahr") <= 2012 Then
    MsgBox "Ungültige Auftragsnummer", vbCritical, "Eingabefehler"
    Exit Sub
End If

DBName = "Provider=SQLOLEDB;Server=trch-app-mes.sqldb.trafagch.local;Database=TMS_MES;integrated security=sspi"
Set oDB = New ADODB.Connection
oDB.Open DBName
   
Set qt = New ADODB.Recordset
qt.Open "EXEC [dbo].[getProdQtyPerDayOfMonthPerDisponent] @Test = False, @Month = " & Range("Monat") & " , @Year = " & Range("Jahr") & " , @ShopfloorID = '" & Range("ARBPL") & "'", oDB, adOpenForwardOnly, adLockReadOnly

    If qt.EOF = True Then
       Range("A10").Value = "Keine Daten vorhanden"
    Else
        ' !!!! Achtung kein MoveFirst ausführen ruft execute ein zweites Mal aus !!!!
        y = 1
        While qt.EOF <> True
            For f = 0 To qt.Fields.Count - 1
                Range("A10").Offset(y, f).Value = qt.Fields(f)
            Next
            y = y + 1
            qt.MoveNext
        Wend
    End If
qt.Close
Application.CalculateFull
Application.Calculation = xlCalculationAutomatic
End Sub

Sub ProdMengeCumulative()
Range("A10:L10000").Value = ""
Application.Calculation = xlCalculationManual
Application.ScreenUpdating = False


If Not IsDate(Range("Tag")) Then
    MsgBox "Ungültiger Tag", vbCritical, "Eingabefehler"
    Exit Sub
End If

DBName = "Provider=SQLOLEDB;Server=trch-app-mes.sqldb.trafagch.local;Database=TMS_MES;integrated security=sspi"
Set oDB = New ADODB.Connection
oDB.Open DBName
   
Set qt = New ADODB.Recordset
qt.Open "EXEC [dbo].[getCumulativeQtyOfPeriod] @Test = False, @StartDate = '" & Range("Tag").Value & " 00:00:00' , @EndDate = '" & Range("Tag").Value & " 23:59:59' ", oDB, adOpenForwardOnly, adLockReadOnly

    If qt.EOF = True Then
       Range("A10").Value = "Keine Daten vorhanden"
    Else
        ' !!!! Achtung kein MoveFirst ausführen ruft execute ein zweites Mal aus !!!!
        y = 1
        While qt.EOF <> True
            For f = 0 To qt.Fields.Count - 1
                
                If f = 0 Then
                    Range("A9").Offset(y, f).Value = TimeValue(qt.Fields(f))
                    Range("A9").Offset(y, f).NumberFormat = "hh:mm:ss"
                Else
                    Range("A9").Offset(y, f).Value = Val(qt.Fields(f))
                    Range("A9").Offset(y, f).NumberFormat = "0"
                End If
            Next
            y = y + 1
            qt.MoveNext
        Wend
    End If
qt.Close
Application.ScreenUpdating = True
Application.CalculateFull
Application.Calculation = xlCalculationAutomatic

End Sub


Sub Check()
Range("A10:J1000").Value = ""
Application.Calculation = xlCalculationManual
If (Len(Trim(Range("Auftrag"))) <> 6 Or Not IsNumeric(Range("Auftrag"))) And Len(Trim(Range("Auftrag"))) <> 0 Then
    MsgBox "Ungültige Auftragsnummer", vbCritical, "Eingabefehler"
    Exit Sub
End If

DBName = "Provider=SQLOLEDB;Server=trch-app-mes.sqldb.trafagch.local;Database=TMS_MES;integrated security=sspi"
Set oDB = New ADODB.Connection
oDB.Open DBName
   
Set qt = New ADODB.Recordset
qt.Open "SELECT R.[Auftragsnummer],R.[VORNR],R.[PSMNG],R.[RUECK],R.[Total],R.[OK],R.[FAIL],R.[RÜCK_OK],R.[RÜCK_FAIL] FROM [TMS_MES].[P76].[AuftragsRückmeldungenOnlyProd] AS R INNER JOIN [TMS_MES].[P76].[Auftrag] as A ON A.Auftragsnummer = R.Auftragsnummer" & _
  " WHERE R.[Auftragsnummer] LIKE '%" & Right(Trim(Range("Auftrag")), 12) & "' AND A.StatusID = 'PROD' ORDER BY Auftragsnummer, VORNR ", oDB, adOpenForwardOnly, adLockReadOnly

    If qt.EOF = True Then
       Range("A10").Value = "Keine Daten vorhanden"
    Else
        ' !!!! Achtung kein MoveFirst ausführen ruft execute ein zweites Mal aus !!!!
        y = 1
        While qt.EOF <> True
            For f = 0 To qt.Fields.Count - 1
                Range("A10").Offset(y, f).Value = qt.Fields(f)
            Next
            y = y + 1
            qt.MoveNext
        Wend
    End If
qt.Close
Application.CalculateFull
Application.Calculation = xlCalculationAutomatic
End Sub

Sub CheckPaused()

Range("A10:J1000").Value = ""
Application.Calculation = xlCalculationManual
'If (Len(Trim(Range("Auftrag"))) <> 6 Or Not IsNumeric(Range("Auftrag"))) And Len(Trim(Range("Auftrag"))) <> 0 Then
'    MsgBox "Ungültige Auftragsnummer", vbCritical, "Eingabefehler"
'    Exit Sub
'End If

DBName = "Provider=SQLOLEDB;Server=trch-app-mes.sqldb.trafagch.local;Database=TMS_MES;integrated security=sspi"
Set oDB = New ADODB.Connection
oDB.Open DBName
   
Set qt = New ADODB.Recordset
qt.Open "SELECT P76.V_DeviceInProcess.SensorID, P76.V_DeviceInProcess.Auftragsnummer, P76.V_DeviceInProcess.FortlNr, P76.V_DeviceInProcess.VORNR, " & _
                    "P76.LinkVorgänge.StatusID , P76.LinkVorgänge.Date, P76.LinkVorgänge.ShopfloorId,TMS_ASICTX.dbo.Auswertung.Bewertung, P76.V_DeviceInProcess.Typencode " & _
                    "FROM     TMS_ASICTX.dbo.Lauf INNER JOIN " & _
                    "TMS_ASICTX.dbo.Geraet ON TMS_ASICTX.dbo.Lauf.SeriennummerGeraet = TMS_ASICTX.dbo.Geraet.Seriennummer AND " & _
                    "TMS_ASICTX.dbo.Lauf.LaufNr = TMS_ASICTX.dbo.Geraet.LetzteLaufNr INNER JOIN " & _
                    "TMS_ASICTX.dbo.Los ON TMS_ASICTX.dbo.Lauf.Losdaten = TMS_ASICTX.dbo.Los.Losdaten INNER JOIN " & _
                    "TMS_ASICTX.dbo.Auswertung ON TMS_ASICTX.dbo.Lauf.LaufNr = TMS_ASICTX.dbo.Auswertung.LaufNr RIGHT OUTER JOIN " & _
                    "P76.LinkVorgänge ON TMS_ASICTX.dbo.Los.Vornr = P76.LinkVorgänge.VORNR RIGHT OUTER JOIN " & _
                    "P76.V_DeviceInProcess ON TMS_ASICTX.dbo.Geraet.Sensorinformation = P76.V_DeviceInProcess.SensorID AND " & _
                    "P76.LinkVorgänge.VORNR = P76.V_DeviceInProcess.VORNR And P76.LinkVorgänge.LinkID = P76.V_DeviceInProcess.LinkID " & _
                    "ORDER BY P76.V_DeviceInProcess.Auftragsnummer, P76.V_DeviceInProcess.FortlNr " _
                      , oDB, adOpenForwardOnly, adLockReadOnly

    If qt.EOF = True Then
       Range("A10").Value = "Keine Daten vorhanden"
    Else
        ' !!!! Achtung kein MoveFirst ausführen ruft execute ein zweites Mal aus !!!!
        y = 1
        While qt.EOF <> True
            For f = 0 To qt.Fields.Count - 1
                Range("A10").Offset(y, f).Value = qt.Fields(f)
            Next
            y = y + 1
            qt.MoveNext
        Wend
    End If
qt.Close
Application.CalculateFull
Application.Calculation = xlCalculationAutomatic
End Sub

Sub InsertFAUF()

Range("A10:E1000").Value = ""
If Len(Trim(Range("Auftrag"))) <> 6 Or Not IsNumeric(Range("Auftrag")) Then
    MsgBox "Ungültige Auftragsnummer", vbCritical, "Eingabefehler"
    Exit Sub
End If
If Len(Trim(Range("Station"))) < 5 Then
    MsgBox "Ungültige Station", vbCritical, "Eingabefehler"
    Exit Sub
End If

DBName = "Provider=SQLOLEDB;Server=trch-app-mes.sqldb.trafagch.local;Database=TMS_MES;integrated security=sspi"
Set oDB = New ADODB.Connection
oDB.Open DBName
   
Set qt = New ADODB.Recordset
Set mat = New ADODB.Recordset
qt.Open "INSERT INTO [TMS_MES].[P76].[Auftrag] ([Auftragsnummer],[StatusID],[StartStation]) " & _
        "Values (( '000000" & Trim(Range("Auftrag")) & "'),'PROD','" & Trim(Range("Station")) & "')", oDB, adOpenForwardOnly, adLockReadOnly

MsgBox "Auftrag eröffnet"
End Sub


Sub StartFAUF()

If Len(Trim(Range("Auftrag"))) <> 6 Or Not IsNumeric(Range("Auftrag")) Then
    MsgBox "Ungültige Auftragsnummer", vbCritical, "Eingabefehler"
    Exit Sub
End If
If Len(Trim(Range("Station"))) < 5 Then
    MsgBox "Ungültige Station", vbCritical, "Eingabefehler"
    Exit Sub
End If

DBName = "Provider=SQLOLEDB;Server=trch-app-mes.sqldb.trafagch.local;Database=TMS_MES;integrated security=sspi"
Set oDB = New ADODB.Connection
oDB.Open DBName
   
Set qt = New ADODB.Recordset
Set mat = New ADODB.Recordset
qt.Open "UPDATE [TMS_MES].[P76].[Auftrag] SET [StatusID] = 'PROD' WHERE " _
& "[Auftragsnummer] = " & " '000000" & Trim(Range("Auftrag")) & "' AND StartStation = '" _
& Trim(Range("Station")) & "'", oDB, adOpenForwardOnly, adLockReadOnly

RefreshFAUF

End Sub

Sub SperreFAUF()

If Len(Trim(Range("Auftrag"))) <> 6 Or Not IsNumeric(Range("Auftrag")) Then
    MsgBox "Ungültige Auftragsnummer", vbCritical, "Eingabefehler"
    Exit Sub
End If
If Len(Trim(Range("Station"))) < 5 Then
    MsgBox "Ungültige Station", vbCritical, "Eingabefehler"
    Exit Sub
End If

DBName = "Provider=SQLOLEDB;Server=trch-app-mes.sqldb.trafagch.local;Database=TMS_MES;integrated security=sspi"
Set oDB = New ADODB.Connection
oDB.Open DBName
   
Set qt = New ADODB.Recordset
Set mat = New ADODB.Recordset
qt.Open "UPDATE [TMS_MES].[P76].[Auftrag] SET [StatusID] = 'SPERR' WHERE " _
& "[Auftragsnummer] = " & " '000000" & Trim(Range("Auftrag")) & "' AND StartStation = '" _
& Trim(Range("Station")) & "'", oDB, adOpenForwardOnly, adLockReadOnly

RefreshFAUF

End Sub
Sub LöscheFAUF()
If Len(Trim(Range("Auftrag"))) <> 6 Or Not IsNumeric(Range("Auftrag")) Then
    MsgBox "Ungültige Auftragsnummer", vbCritical, "Eingabefehler"
    Exit Sub
End If
If Len(Trim(Range("Station"))) < 5 Then
    MsgBox "Ungültige Station", vbCritical, "Eingabefehler"
    Exit Sub
End If

DBName = "Provider=SQLOLEDB;Server=trch-app-mes.sqldb.trafagch.local;Database=TMS_MES;integrated security=sspi"
Set oDB = New ADODB.Connection
oDB.Open DBName
   
Set qt = New ADODB.Recordset
Set mat = New ADODB.Recordset
qt.Open "UPDATE [TMS_MES].[P76].[Auftrag] SET [StatusID] = 'LÖVM' WHERE " _
& "[Auftragsnummer] = " & " '000000" & Trim(Range("Auftrag")) & "' AND StartStation = '" _
& Trim(Range("Station")) & "'", oDB, adOpenForwardOnly, adLockReadOnly

RefreshFAUF

End Sub

Sub RefreshFAUF()

Application.Calculation = xlCalculationManual
Range("A10:M1000").Value = ""

DBName = "Provider=SQLOLEDB;Server=trch-app-mes.sqldb.trafagch.local;Database=TMS_MES;integrated security=sspi"
Set oDB = New ADODB.Connection
oDB.Open DBName
oDB.Execute "[TMS_MES].[dbo].importProdorders 'False'"

Set qt = New ADODB.Recordset
qt.Open "SELECT a.Auftragsnummer, " & _
"a.StatusID, " & _
"a.StartStation, " & _
"afpo.MATNR, " & _
"mara.ZZTYP, " & _
"pm.FEHL_MAT, " & _
"f.FERTHINWEIS " & _
"FROM P76.Auftrag as a " & _
"INNER JOIN SAP.P76.AFPO as afpo ON a.Auftragsnummer = afpo.AUFNR " & _
"INNER JOIN SAP.P76.MARA as mara ON afpo.MATNR = mara.MATNR " & _
"LEFT OUTER JOIN P76.ProdOrderHasMat as pm ON pm.AUFNR = a.Auftragsnummer " & _
"LEFT OUTER JOIN P76.V_Prod_Auftrag_Fertigungshinweis as f ON a.Auftragsnummer = f.Auftragsnummer " & _
"WHERE     (a.StatusID <> N'ENDE' AND a.StatusID <> N'LÖVM') " & _
"ORDER BY CASE WHEN a.StatusID = 'PROD' THEN 'A' ELSE a.StatusID END DESC, CASE WHEN f.FERTHINWEIS IS NULL THEN 1 ELSE 0 END, a.Auftragsnummer" _
, oDB, adOpenForwardOnly, adLockReadOnly

Set sf = New ADODB.Recordset
sf.Open "EXEC [dbo].[getProdOrderFromShopFloor] @Test = false, @Arbeitsplatz = N'" & Trim(Range("Station")) & "'", oDB, adOpenForwardOnly, adLockReadOnly

If qt.EOF = True Then
   Range("A10").Value = "Keine Daten vorhanden"
Else
    ' !!!! Achtung kein MoveFirst ausführen ruft execute ein zweites Mal aus !!!!
    y = 1
    While qt.EOF <> True
        For f = 0 To qt.Fields.Count - 1
            Range("A10").Offset(y, f).Value = qt.Fields(f)
        Next
        y = y + 1
        qt.MoveNext
    Wend
End If

qt.Close


If Not sf.EOF = True Then
    ' !!!! Achtung kein MoveFirst ausführen ruft execute ein zweites Mal aus !!!!
    y = 1
    While sf.EOF <> True
        For f = 0 To sf.Fields.Count - 1
            Range("J10").Offset(y, f).Value = sf.Fields(f)
        Next
        y = y + 1
        sf.MoveNext
    Wend
End If
sf.Close
Application.CalculateFull
Application.Calculation = xlCalculationAutomatic
End Sub

Sub GetMesslauf()

Application.Calculation = xlCalculationManual
Range("A10:E1000").Value = ""

If (Range("Messplatz") = "C" Or _
Range("Messplatz") = "B") Then
    DBName = "Provider=SQLOLEDB;Server=TRAFAG6;Database=TMS_AnalogMessen;integrated security=sspi"
Else
    DBName = "Provider=SQLOLEDB;Server=TRAFAG6;Database=AnalogTransmitter_a;integrated security=sspi"
End If
Set oDB = New ADODB.Connection
oDB.Open DBName

Set qt = New ADODB.Recordset
If (Range("Messplatz") = "C" Or _
Range("Messplatz") = "B") Then

    qt.Open "SELECT  DISTINCT    Auswertung.Platznummer + 1 AS Platz ,Gerät.Seriennummer, Auswertung.Bewertung, Gerät.Auftragsnummer, Gerät.Sensorinformation " & _
    "FROM         Auswertung INNER JOIN  Gerät ON Auswertung.Seriennummer = Gerät.Seriennummer INNER JOIN " & _
                 "GeräteProLos ON Auswertung.Losdaten = GeräteProLos.Losdaten " & _
    "WHERE (GeräteProLos.Serienummer = " & Range("MesslaufNr").Value & " ) ORDER BY Platz", oDB, adOpenForwardOnly, adLockReadOnly
Else
    qt.Open "SELECT  DISTINCT    Auswertung.Platznummer + 1 AS Platz ,Geraet.Seriennummer, Auswertung.Bewertung, Geraet.Auftragsnummer, Geraet.Sensorinformation " & _
    "FROM         Auswertung INNER JOIN  Geraet ON Auswertung.Seriennummer = Geraet.Seriennummer INNER JOIN " & _
                 "GeraeteProLos ON Auswertung.Losdaten = GeraeteProLos.Losdaten " & _
    "WHERE (GeraeteProLos.Serienummer = " & Range("MesslaufNr").Value & " ) ORDER BY Platz", oDB, adOpenForwardOnly, adLockReadOnly
End If

If qt.EOF = True Then
   Range("A10").Value = "Keine Daten vorhanden"
Else
    ' !!!! Achtung kein MoveFirst ausführen ruft execute ein zweites Mal aus !!!!
    y = 1
    While qt.EOF <> True
        For f = 0 To qt.Fields.Count - 1
            Range("A10").Offset(y, f).Value = qt.Fields(f)
        Next
        y = y + 1
        qt.MoveNext
    Wend
End If

qt.Close

GetMESData
Application.CalculateFull
Application.Calculation = xlCalculationAutomatic
End Sub

Sub GetMESData()
Application.Calculation = xlCalculationManual
Range("H10:K1000").Value = ""

DBName = "Provider=SQLOLEDB;Server=trch-app-mes.sqldb.trafagch.local;Database=TMS_MES;integrated security=sspi"
Set oDB = New ADODB.Connection
oDB.Open DBName

x = 1
While Range("A10").Offset(x, 0).Value <> ""
    If Len(Range("D10").Offset(x, 0)) >= 6 And IsNumeric(Range("E10").Offset(x, 0)) Then
    
        Set qt = New ADODB.Recordset
        qt.Open "SELECT     P76.LinkVorgänge.StatusID, P76.SensorIDLink.SensorID, SAP.P76.AFVC.VORNR, SAP.P76.CRHD.ARBPL " & _
                 "FROM         SAP.P76.AFVC INNER JOIN " & _
                "SAP.P76.AFKO ON SAP.P76.AFVC.MANDT = SAP.P76.AFKO.MANDT AND SAP.P76.AFVC.AUFPL = SAP.P76.AFKO.AUFPL INNER JOIN " & _
                "P76.SensorIDLink ON SAP.P76.AFKO.AUFNR = P76.SensorIDLink.Auftragsnummer INNER JOIN " & _
                "SAP.P76.CRHD ON SAP.P76.AFVC.MANDT = SAP.P76.CRHD.MANDT AND SAP.P76.AFVC.ARBID = SAP.P76.CRHD.OBJID LEFT OUTER JOIN " & _
                "P76.LinkVorgänge ON SAP.P76.AFVC.VORNR = P76.LinkVorgänge.VORNR AND P76.SensorIDLink.LinkID = P76.LinkVorgänge.LinkID " & _
                "WHERE     (P76.SensorIDLink.Auftragsnummer = '000000" & Range("D10").Offset(x, 0).Text & "') " & _
                "AND (P76.SensorIDLink.FortlNr = '" & Range("E10").Offset(x, 0).Text & "') AND (SAP.P76.CRHD.ARBPL = N'" & Range("Station").Text & "') " _
                , oDB, adOpenForwardOnly, adLockReadOnly
        If qt.EOF <> True Then
            For f = 0 To qt.Fields.Count - 1
                Range("H10").Offset(x, f).Value = qt.Fields(f)
            Next
        End If
        qt.Close
    End If
    x = x + 1
Wend
Application.CalculateFull
Application.Calculation = xlCalculationAutomatic
End Sub

Sub SetMES()



DBName = "Provider=SQLOLEDB;Server=trch-app-mes.sqldb.trafagch.local;Database=TMS_MES;integrated security=sspi"
Set oDB = New ADODB.Connection
oDB.Open DBName
x = 1
While Range("A10").Offset(x, 0).Value <> ""
    If Len(Range("D10").Offset(x, 0)) >= 6 And IsNumeric(Range("E10").Offset(x, 0)) And Len(Range("G10").Offset(x, 0).Text) > 1 Then
            oDB.Execute _
            "Execute [updateLinkIDVorgang] " & _
            "@Test = 'False'  " & _
            ",@Auftragsnummer = '000000" & Range("D10").Offset(x, 0).Text & "' " & _
            ",@ID = '" & Range("I10").Offset(x, 0).Text & "' " & _
            ",@Vorgang = '" & Range("J10").Offset(x, 0).Text & "' " & _
            ",@StationId = '" & Range("K10").Offset(x, 0).Text & "' " & _
            ",@StatusID = '" & Range("G10").Offset(x, 0).Text & "' "


    End If
    x = x + 1
Wend



End Sub

Sub SetMessZuweisung()



If (Range("Messplatz") = "C" Or _
Range("Messplatz") = "B") Then
    DBName = "Provider=SQLOLEDB;Server=TRAFAG6;Database=TMS_AnalogMessen;integrated security=sspi"
Else
    DBName = "Provider=SQLOLEDB;Server=TRAFAG6;Database=AnalogTransmitter_a;integrated security=sspi"
End If

Set oDB = New ADODB.Connection
oDB.Open DBName
x = 1
While Range("A10").Offset(x, 0).Value <> ""
    If Len(Range("D10").Offset(x, 0)) >= 6 And IsNumeric(Range("E10").Offset(x, 0)) Then
    If (Range("Messplatz") = "C" Or _
            Range("Messplatz") = "B") Then
                oDB.Execute _
                "Update Gerät " & _
                "SET Auftragsnummer = '" & Range("D10").Offset(x, 0).Text & "' " & _
                ",Sensorinformation = '" & Range("E10").Offset(x, 0).Text & "' " & _
                "Where Seriennummer = '" & Range("B10").Offset(x, 0).Text & "' "
            Else
                oDB.Execute _
                "Update Geraet " & _
                "SET Auftragsnummer = '" & Range("D10").Offset(x, 0).Text & "' " & _
                ",Sensorinformation = '" & Range("E10").Offset(x, 0).Text & "' " & _
                "Where Seriennummer = '" & Range("B10").Offset(x, 0).Text & "' "
            End If
    End If
    x = x + 1
Wend



End Sub

Sub DLZUpdate()
Sheets("DLZ Daten (KW)").Range("A2:D50000").Value = ""
Application.Calculation = xlCalculationManual


DBName = "Provider=SQLOLEDB;Server=trch-app-mes.sqldb.trafagch.local;Database=TMS_MES;integrated security=sspi"
Set oDB = New ADODB.Connection
oDB.Open DBName
   
Set qt = New ADODB.Recordset
qt.Open "" & _
"SELECT   JJJJKW,SensorID,CASE WHEN [0795] IS NULL THEN [0800]-[0100] ELSE ([0800]-[0795])+([0700]-[0100]) END AS DLZTOTAL ,CASE WHEN [0795] IS NULL THEN [0800]-[0100] ELSE ([0800]-[0795])+([0700]-[0100]) END /3600 AS DLZTOTAL_H " & _
"FROM (SELECT     ID.SensorID, CASE WHEN V.VORNR = '0790' THEN '0795' ELSE V.VORNR END AS VORNR, STR(YEAR(ID.Date),4,0)+'/'+RIGHT('0'+CAST(master.dbo.ISOweek(ID.Date) AS VARCHAR(2)),2) AS JJJJKW , (DATEDIFF(SECOND, ID.Date, V.Date) - CASE WHEN (DATEPART(dw, V.Date) - DATEPART(dw, ID.Date)) > 7 THEN 24*3600 ELSE 0 END)  AS Timeline " & _
"FROM P76.LinkVorgänge AS V INNER JOIN " & _
"     P76.SensorIDLink AS ID ON V.LinkID = ID.LinkID AND ID.Date >=  '01.01.2014' " & _
") p " & _
"PIVOT " & _
"( " & _
"    AVG (Timeline) FOR VORNR IN ([0100],[0200],[0300],[0400],[0500],[0600],[0700],[0795],[0800]) " & _
") AS pvt " & _
"WHERE [0800] IS NOT NULL AND CASE WHEN [0795] IS NULL THEN [0800]-[0100] ELSE ([0800]-[0795])+([0700]-[0100]) END  IS NOT NULL " & _
"Order by JJJJKW,SensorID " _
, oDB, adOpenForwardOnly, adLockReadOnly

    If qt.EOF = True Then
       Sheets("DLZ Daten (KW)").Range("A1").Value = "Keine Daten vorhanden"
    Else
        ' !!!! Achtung kein MoveFirst ausführen ruft execute ein zweites Mal aus !!!!
        y = 1
        While qt.EOF <> True
            For f = 0 To qt.Fields.Count - 1
                Sheets("DLZ Daten (KW)").Range("A1").Offset(y, f).Value = qt.Fields(f)
            Next
            y = y + 1
            qt.MoveNext
        Wend
    End If
qt.Close
Application.CalculateFull
Application.Calculation = xlCalculationAutomatic
Sheets("DLZ Daten (KW)").PivotTables("PivotTable1").PivotCache.Refresh

End Sub
