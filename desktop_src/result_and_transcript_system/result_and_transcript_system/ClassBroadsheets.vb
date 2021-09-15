﻿'Imports Microsoft.Office.Interop.Excel
Imports ExcelInterop = Microsoft.Office.Interop.Excel

Public Class ClassBroadsheets
    Public Delegate Sub OnPropertyChanged(ByVal sende As Object, e As ClassAnswerEventArgs)
    Public Event myEventOnPropertyChanged(ByVal sende As Object, e As ClassAnswerEventArgs) 'support for events

    Public Delegate Sub OnProgressChanged(ByVal sender As Object, e As ClassAnswerEventArgs)
    Public Event myEventOnPrgressChanged(ByVal sender As Object, e As ClassAnswerEventArgs) 'support for events

    Public Sub New()
        Try
            'Me._excelWB.FileNameWB = PROG_DIRECTORY & "\templates\broadsheet.xltx"
            _broadsheetFileName = My.Application.Info.DirectoryPath & "\templates\broadsheet.xltx"
            '_excelWB = New ExcelInterop.Workbook

        Catch ex As Exception
            MsgBox("Cannot create Excel Automation Object" & vbCrLf & ex.Message)
        End Try
    End Sub
#Region "Broadsheet Properties"
    Private _broadsheetSemester As String = ""
    Private _strMATNO As String() = Nothing
    Private _broadsheetFileName As String = Nothing
    Private _progress As Integer = 0
    Private _excelWB As ExcelInterop.Workbook
    Private _regInfoCoursesFirstSem As List(Of String)
    Private _regInfoCoursesSecondSem As List(Of String)
    Private _processedBroadsheetFileName As String = Nothing
    Private _broadsheetDataDS As DataSet
    Public Property processedBroadsheetFileName() As String
        Get
            Return _processedBroadsheetFileName
        End Get
        Set(ByVal value As String)
            _processedBroadsheetFileName = value
        End Set
    End Property
    Public Property regInfoCoursesSecondSem() As List(Of String)
        Get
            Return _regInfoCoursesSecondSem
        End Get
        Set(ByVal value As List(Of String))
            _regInfoCoursesSecondSem = value
        End Set
    End Property
    Public Property regInfoCoursesFirstSem() As List(Of String)
        Get
            Return _regInfoCoursesFirstSem
        End Get
        Set(ByVal value As List(Of String))
            _regInfoCoursesFirstSem = value
        End Set
    End Property
    Public Property excelWB() As ExcelInterop.Workbook
        Get
            Return _excelWB
        End Get
        Set(ByVal value As ExcelInterop.Workbook)
            _excelWB = value
        End Set
    End Property
    Public Property progress() As Integer
        Get
            Return _progress
        End Get
        Set(ByVal value As Integer)
            _progress = value
            Dim e As New ClassAnswerEventArgs
            e.VariableChanged = True
            e.Ans = value
            RaiseEvent myEventOnPrgressChanged(Me, e) 'MATNOChanged
        End Set
    End Property
    Public Property broadsheetFileName() As String
        Get
            Return _broadsheetFileName
        End Get
        Set(ByVal value As String)
            _broadsheetFileName = value
            Dim e As New ClassAnswerEventArgs
            e.VariableChanged = True
            e.Ans = -1
            RaiseEvent myEventOnPropertyChanged(Me, e) 'MATNOChanged
        End Set
    End Property

    Public Property broadsheetDataDS() As DataSet
        Get
            Return _broadsheetDataDS
        End Get
        Set(ByVal value As DataSet)
            _broadsheetDataDS = value
            'Dim e As New ClassAnswerEventArgs
            'e.VariableChanged = True
            'e.Ans = -1
            'RaiseEvent myEventOnPropertyChanged(Me, e) 'ds
        End Set
    End Property
    Public Property strMATNO() As String()
        Get
            Return _strMATNO
        End Get
        Set(ByVal value As String())
            _strMATNO = value
            Dim e As New ClassAnswerEventArgs
            e.VariableChanged = True
            e.Ans = -1
            RaiseEvent myEventOnPropertyChanged(Me, e) 'MATNOChanged
        End Set
    End Property
    Public Property broadsheetSemester() As String
        Get
            Return _broadsheetSemester
        End Get
        Set(ByVal value As String)
            _broadsheetSemester = value
        End Set
    End Property

#End Region

    Function createBroadsheetData(course_dept_idr As String, session_idr As String, course_level As String, Optional isInterrop As Boolean = False) As DataSet
        'Algo
        '1. count courseCodes in result table = j
        '2. create dataset with j no of cols
        'for each col.name
        '   query students, left join results (convert null to NA -3
        '   transfer query result col to dataset col
        'next

        '#1 count courseCodes in result table = j
        Dim countC, countReg, countResultsBS As Integer
        Dim coursesDS, RegStudentsDS, FSBroadsheetDS As DataSet
        Dim strSQL, strSQLJoin As String
        Dim tmpStr, tmpStrMATNO, tmpStrCourseCode As String
        Dim tmpInt As Integer = -4
        strSQL = "SELECT Courses.course_code, Courses.course_level, Courses.course_unit, Courses.course_semester, Courses.course_dept_idr, Courses.course_order, Count(Courses.course_order) AS CountOfcourse_order
                  FROM Courses
                 GROUP BY Courses.course_level, Courses.course_code, Courses.course_unit, Courses.course_semester, Courses.course_dept_idr, Courses.course_order
                 HAVING (((Courses.course_semester)=1) AND ((Courses.course_dept_idr)={0}) AND ((Count(Courses.course_order))>0))
                 ORDER BY Courses.course_level, Courses.course_order;" 'and level
        'or
        'strSQL = "SELECT course_code,course_unit FROM QueryFS_Coursers_Ordered"
        coursesDS = mappDB.GetDataWhere(String.Format(strSQL, course_dept_idr), "Courses")
        countC = coursesDS.Tables(0).Rows.Count

        strSQL = "SELECT reg.matno FROM reg WHERE session_idr='{0}'"    'and level=course_level 
        RegStudentsDS = mappDB.GetDataWhere(String.Format(strSQL, session_idr), "Reg")
        countReg = RegStudentsDS.Tables(0).Rows.Count

        strSQLJoin = "SELECT Reg.MatNo, Last(Results.total) AS LastOftotal, Results.course_code_idr, 
                      Results.Session_idr  FROM Reg INNER JOIN Results ON Reg.MatNo = Results.matno GROUP BY Reg.MatNo, Results.course_code_idr,  
                      Results.Session_idr HAVING (((Results.course_code_idr)='{0}') AND ((Results.Session_idr)='{1}'));"



        '#2 Dataset creation 
        '--------------------------
        'Very good!
        Dim ds As New DataSet
        Dim dt As New DataTable
        Dim dr As DataRow
        Dim tmpCol As DataColumn    'Dim Coulumn, matnoCoulumn, nameCoulumn As DataColumn Dim caCoulumn, scoreCoulumn, examCoulumn, surnameCoulumn, otherNameCoulumn As DataColumn
        Dim dictCol, dictMATNO As New Dictionary(Of String, Integer)
        dt.TableName = "BroadsheetFS"

        'Create Fixed Cols TODO: put in sub
        tmpCol = New DataColumn("sn", Type.GetType("System.String")) '1
        dt.Columns.Add(tmpCol)
        tmpCol = New DataColumn("matno", Type.GetType("System.String")) '2
        dt.Columns.Add(tmpCol)
        tmpCol = New DataColumn("FullName", Type.GetType("System.String"))
        dt.Columns.Add(tmpCol)
        tmpCol = New DataColumn("OtherNames", Type.GetType("System.String"))
        dt.Columns.Add(tmpCol)
        tmpCol = New DataColumn("Surname", Type.GetType("System.String"))
        dt.Columns.Add(tmpCol)
        tmpCol = New DataColumn("RepeatAll", Type.GetType("System.String")) '6
        dt.Columns.Add(tmpCol)
        tmpCol = New DataColumn("RepeatFirstSem", Type.GetType("System.String")) '6 syst
        dt.Columns.Add(tmpCol)

        'Get mat nos
        For i = 0 To countReg - 1
            dictMATNO.Add(RegStudentsDS.Tables(0).Rows(i).Item("matno"), -4)   'add disting students
            'add em rows
            dr = dt.NewRow()
            dr("matno") = RegStudentsDS.Tables(0).Rows(i).Item("matno")
            dr("sn") = (i + 1).ToString
            dt.Rows.Add(dr)
        Next

        'worked TODO: let courses iterate like this in next loops
        For j = 0 To countC - 1 'create columns for courses 'TODO: 1st and second
            tmpCol = New DataColumn(coursesDS.Tables(0).Rows(j).Item("course_code"), Type.GetType("System.Int32"))
            dt.Columns.Add(tmpCol)
        Next


        'from broadsheet template courses start from col H = 8 (i.e 7 counting from 0)
        For jCourseCol = 7 To countC - 1 ' for each col(ie course code) put rows in result table in dictC
            'HINT: this wrong line gave me problem for days strSQLJoin = String.Format(strSQLJoin, dt.Columns(jCourseCol).ColumnName, session_idr) 
            tmpStr = String.Format(strSQLJoin, dt.Columns(jCourseCol).ColumnName, session_idr)   'SELECT ... LEFT JOIN

            FSBroadsheetDS = mappDB.GetDataWhere(tmpStr, "FSBroadsheetDS")

            'SELECT Reg.MatNo, Reg.session_idr, Results.total, Results.course_code_idr From Reg LEFT Join Results On Reg.MatNo = Results.matno'Where (((Reg.session_idr) = "2018/2019") And ((Results.course_code_idr) = "CPE301"));
            'TODO: posible duplicate MATNO in query if duplicate result is in result table
            'Approach 1: avoid duplicate in result table           'Approach 2: use agregate first or last           'Approach 3: use data structures list, array
            countResultsBS = FSBroadsheetDS.Tables(0).Rows.Count
            dictCol.Clear()

            For iBSRow = 0 To countResultsBS - 1
                tmpStr = FSBroadsheetDS.Tables(0).Rows(iBSRow).Item("matno")
                'Debug.Print(FSBroadsheetDS.Tables(0).Rows(i).Item(tmpCol.ColumnName))
                'Debug.Print(FSBroadsheetDS.Tables(0).Rows(i).Item("total"))
                If Not dictCol.ContainsKey(tmpStr) Then
                    dictCol.Add(tmpStr, FSBroadsheetDS.Tables(0).Rows(iBSRow).Item("LastOftotal"))   'add disting students
                Else
                    'todo: handle these duplicate results somehow
                End If
                'OR 'dictCol(tmpStr) = findMATNO(FSBroadsheetDS.Tables(0).Rows(i).Item("matno"), dictCol(i).key.name)   '
            Next

            'Debug.Print("MATNO: ")
            'Debug.Print(String.Join(", ", dictMATNO.Values.ToArray.ToString))
            'Debug.Print("Values: ")
            'Debug.Print(String.Join(", ", dictCol.Values.ToArray.ToString))
            'transfer  the matching scores to dictMATNO
            Dim colKeys() As String = dictMATNO.Keys.ToArray
            If dictCol.Count > 0 Then
                For Each colkey In colKeys
                    Dim tmpVal As Integer = dictMATNO(colkey)
                    Debug.Print(colkey & ": " & dictMATNO(colkey).ToString & ", score: " & tmpVal) 'dictMATNO.ContainsValue("4"),  dictMATNO.containskey("4")

                    If dictCol.ContainsKey(colkey) Then tmpVal = dictCol(colkey) Else tmpVal = -5 'change the value
                    dictMATNO(colkey) = tmpVal
                Next colkey


                'Update dataset with result values
                For iMainDS = 0 To dt.Rows.Count - 1
                    'dt.Rows(i).Item("matno") = colKeys(i)   'already there no need to rewrite
                    tmpStrCourseCode = dt.Columns(jCourseCol).ColumnName
                    tmpStrMATNO = dt.Rows(iMainDS).Item("matno")
                    If dictMATNO.ContainsKey(tmpStrMATNO) Then
                        'todo
                        dt.Rows(iMainDS).Item(tmpStrCourseCode) = dictMATNO(tmpStrMATNO) '.ToString & ", " & tmpStrCourseCode
                    Else
                        dt.Rows(iMainDS).Item(tmpStrCourseCode) = -7
                    End If
                Next

            Else
                'No need to iterate through scores because no result was found
            End If
            objBroadsheet.progress = jCourseCol / countC * 80
        Next    'end for  each col(ie course code)



        'Now Update result in Dataset
        If isInterrop Then  'TODO: impliment condition
            'fml stuff special consideration for excel 
        Else
            'compute every thing first
        End If

        tmpCol = New DataColumn("TCF_1", Type.GetType("System.String"))
        dt.Columns.Add(tmpCol)
        tmpCol = New DataColumn("TCP_1", Type.GetType("System.String"))
        dt.Columns.Add(tmpCol)
        tmpCol = New DataColumn("TCR_1", Type.GetType("System.String"))
        dt.Columns.Add(tmpCol)

        'others
        tmpCol = New DataColumn("Session", Type.GetType("System.String"))
        dt.Columns.Add(tmpCol)

        objBroadsheet.progress = 97 'update progress
        ds.Tables.Add(dt)

        Return ds
    End Function

    Function createDataSetHowTo(strArrayMATNO As String()) As DataSet
        '# How to perform Dataset creation  'Very good!
        '-------------------------
        Dim ds As New DataSet
        Dim dt As DataTable
        Dim dr As DataRow
        Dim snCoulumn, matnoCoulumn, nameCoulumn As DataColumn

        dt = New DataTable()
        snCoulumn = New DataColumn("SN", Type.GetType("System.Int32"))
        matnoCoulumn = New DataColumn("MATNO", Type.GetType("System.String"))

        dt.TableName = "Result"
        dt.Columns.Add(snCoulumn)
        dt.Columns.Add(matnoCoulumn)

        For i = 0 To strArrayMATNO.Length - 1
            dr = dt.NewRow()
            dr("SN") = i + 1
            dr("MATNO") = strArrayMATNO(i)
            dr("OtherNames") = "Firstname SURNAME"
            dt.Rows.Add(dr)
        Next
        ds.Tables.Add(dt)

        Return ds
    End Function

    Function openBroadsheetExcelWBInterop() As Boolean

        Dim strCriteria As String = String.Empty
        Dim startRow As Integer = 1
        Dim endRow As Integer = 200
        Dim r As Integer = 9  'starts at row 9
        Dim strSQLTemp As String = ""
        Dim i As Integer = 0
        ' Dim strArrayMATNO, strArrayNAME, strArrayCA, strArrayEXAM, strArrayTOTAL, strArraySURNAME, strArrayOtherNames As String()
        Try
            excelApp = New ExcelInterop.Application

            If _broadsheetFileName = Nothing Or Not System.IO.File.Exists(_broadsheetFileName) Then Exit Function 'todo
            excelWB = excelApp.Workbooks.Open(Me.broadsheetFileName)
            excelWS = CType(excelWB.Sheets(1), ExcelInterop.Worksheet)
            'show it
            excelApp.Visible = False

            Dim strCellContent As String = ""
            Dim MATNoFound As Boolean = False
            r = 8 'textbox
            'Search for MATNO Column
            For i = 1 To 20
                strCellContent = excelWS.Cells(i, ExcelColumns.colB).text
                If strCellContent.Contains("MAT") Then
                    startRow = i + 1
                    MATNoFound = True
                    Exit For
                End If
            Next
            If MATNoFound = False Then
                For i = 1 To 20
                    strCellContent = excelWS.Cells(i, ExcelColumns.colA).text
                    If strCellContent.Contains("MAT") Then
                        startRow = i + 1
                        MATNoFound = True
                        Exit For
                    End If
                Next
            End If
            objBroadsheet.progress = 30

            '-----------------------------------------
            '#  Pupulate broadsheet with data
            '------------------------------------------
            'objbroadsheet.sturecord.MATNO.sort()
            'Add serial numbers, matno etc
            For i = startRow To 50
                excelWS.Cells(i, ExcelColumns.colA) = i
                excelWS.Cells(i, ExcelColumns.colB) = i * 345 'TODO: place holder for MAT
                excelWS.Cells(i, ExcelColumns.colC) = "=D" & i & "+E" & i ' "First Name SURNAME" 'TODO: place holder Name
                excelWS.Cells(i, ExcelColumns.colD) = "First Name" & i
                excelWS.Cells(i, ExcelColumns.colE) = "SURNAME" & i
                excelWS.Cells(i, ExcelColumns.colF) = "CPE333/2/55, CPE311/3/75," 'TODO: place holder CO  objbroadsheet.sturecord(MATNO).co
                excelWS.Cells(i, ExcelColumns.colG) = "*79" 'TODO: place holder course  objbroadsheet.sturecord(MATNO).co
                excelWS.Cells(i, ExcelColumns.colH) = "80" 'objbroadsheet.sturecord(MATNO).COURSE002.score
                excelWS.Cells(i, ExcelColumns.colI) = "68" 'objbroadsheet.sturecord(MATNO).COURSE003.score
                '...


                excelWS.Cells(i, ExcelColumns.colO) = "3" 'objbroadsheet.sturecord(MATNO).COURSE003.FAILED1STsEM  or calcFailed(courses1stSem)
                excelWS.Cells(i, ExcelColumns.colP) = "24" 'objbroadsheet.sturecord(MATNO).COURSE003.Passed1STsEM  or calcPassed(courses1stSem)
                excelWS.Cells(i, ExcelColumns.colQ) = "27"


                excelWS.Cells(i, ExcelColumns.colAJ) = "3.579" 'GP AJ, Class AK, Status AL
                excelWS.Cells(i, ExcelColumns.colAK) = "2.1"
                excelWS.Cells(i, ExcelColumns.colAJ) = "A"


                excelWS.Cells(i, ExcelColumns.colAM) = "CPE211 (WAIVED)" 'cOURSE fAILED tRAILED
                If i Mod 2 = 0 Then objBroadsheet.progress = objBroadsheet.progress + 1
                GC.KeepAlive(Me)
            Next

            excelApp.Visible = True

            objBroadsheet.progress = 95 'update progress bar
            Return True
        Catch ex As Exception
            Throw ex
        End Try

    End Function
    Function serializeCourses(dt As DataTable) As String
        'todo: take courses order from datagrid (or excel) and save in a comma seperated string in sessions table
        'select * from cOURSES_ORDER
        Return ""
    End Function
    Function expandCourses(courses As String) As DataTable
        'TOD: consider using a dedicated table fo this
        'SESSION|100FS|200FS.....100SS|200LSS
        Return Nothing
    End Function
    Function generateFormulaCO() As String()
        'carry over
        'sample formula (full)
        '=IF(H12<>"", $H$7 & "/" & $H$9 & "/" & H12 & ", ","") & IF(I12<>"", $I$7 & "/" & $I$9 & "/" & I12 & ", ","") & IF(J12<>"", $J$7 & "/" & $J$9 & "/" & J12 & ", ","") & IF(K12<>"", $K$7 & "/" & $K$9 & "/" & K12 & ", ","") & IF(L12<>"", $L$7 & "/" & $L$9 & "/" & L12 & ", ","") & IF(M12<>"", $M$7 & "/" & $M$9 & "/" & M12 & ", ","") & IF(N12<>"", $N$7 & "/" & $N$9 & "/" & N12 & ", ","") & IF(O12<>"", $O$7 & "/" & $O$9 & "/" & O12 & ", ","") & IF(P12<>"", $P$7 & "/" & $P$9 & "/" & P12 & ", ","") & IF(Q12<>"", $Q$7 & "/" & $Q$9 & "/" & Q12 & ", ","") & IF(R12<>"", $R$7 & "/" & $R$9 & "/" & R12 & ", ","") & IF(S12<>"", $S$7 & "/" & $S$9 & "/" & S12 & ", ","") & IF(T12<>"", $T$7 & "/" & $T$9 & "/" & T12 & ", ","") & IF(AY12<>"", $AY$7 & "/" & $AY$9 & "/" & AY12 & ", ","") & IF(AZ12<>"", $AZ$7 & "/" & $AZ$9 & "/" & AZ12 & ", ","") & IF(BA12<>"", $BA$7 & "/" & $BA$9 & "/" & BA12 & ", ","") & IF(BB12<>"", $BB$7 & "/" & $BB$9 & "/" & BB12 & ", ","") & IF(BC12<>"", $BC$7 & "/" & $BC$9 & "/" & BC12 & ", ","") & IF(BD12<>"", $BD$7 & "/" & $BD$9 & "/" & BD12 & ", ","") & IF(BE12<>"", $BE$7 & "/" & $BE$9 & "/" & BE12 & ", ","") & IF(BF12<>"", $BF$7 & "/" & $BF$9 & "/" & BF12 & ", ","") & IF(BG12<>"", $BG$7 & "/" & $BG$9 & "/" & BG12 & ", ","") & IF(BH12<>"", $BH$7 & "/" & $BH$9 & "/" & BH12 & ", ","") & IF(BI12<>"", $BI$7 & "/" & $BI$9 & "/" & BI12 & ", ","") & IF(BJ12<>"", $BJ$7 & "/" & $BJ$9 & "/" & BJ12 & ", ","")
        Dim strRet(2) As String
        'iterate formula
        Dim maxCoursesFS As Integer = 55
        Dim maxCoursesSS As Integer = 55
        Dim startCol As Integer = 8 'TODO: Depends on template starting letter H = 8 (A=1, B=2, C=3
        Dim headRowCourse As Integer = 7
        Dim headRowCredit As Integer = 9
        Dim startRow As Integer = 10
        Dim Col As String = lettersToNum(startCol)

        'First Semester
        Dim strFml As String = "="
        For j = startCol To startCol + maxCoursesFS
            Col = lettersToNum(j)
            strFml = strFml & String.Format("IF({0}{1}<>``, ${0}${2} & `/` & ${0}${3} & `/` & {0}{1} & `, `,``)", Col, startRow, headRowCourse, headRowCredit) & " & "
        Next
        strFml = strFml.Replace("`"c, """"c)
        strFml = Trim(strFml)
        If strFml.EndsWith("&") Then strFml = strFml.Replace("&"c, "")  'remoe trailing &
        strFml = Trim(strFml)
        If strFml.EndsWith("&") Then strFml = strFml.Replace(","c, "")  'remoe trailing &
        strFml = Trim(strFml)
        Debug.Print(strFml)
        strRet(0) = strFml

        '2nd Semester  Skip 4 cols for TCF, TCP, TC and Repeat 2nd
        strFml = "="
        startCol = (startCol + maxCoursesFS + 4) - 1    'excellent
        For j = startCol To startCol + maxCoursesSS
            Col = lettersToNum(j)
            strFml = strFml & String.Format("IF({0}{1}<>``, ${0}${2} & `/` & ${0}${3} & `/` & {0}{1} & `, `,``)", Col, startRow, headRowCourse, headRowCredit) & " & "
        Next
        strFml = strFml.Replace("`"c, """"c)
        strFml = Trim(strFml)
        If strFml.EndsWith("&") Then strFml = strFml.Replace("&"c, "")  'remoe trailing &
        strFml = Trim(strFml)
        If strFml.EndsWith("&") Then strFml = strFml.Replace(","c, "")  'remoe trailing &
        strFml = Trim(strFml)
        Debug.Print(strFml)
        strRet(1) = strFml

        'Generate formula for TCR_first
        '=SUM(IF(BO10>data!$I$9,Broadsheet!$BO$9,0),IF(BP10>data!$I$9,Broadsheet!$BP$9,0),IF(BQ10>data!$I$9,Broadsheet!$BQ$9,0),IF(BR10>data!$I$9,Broadsheet!$BR$9,0),IF(BS10>data!$I$9,Broadsheet!$BS$9,0),IF(BT10>data!$I$9,Broadsheet!$BT$9,0),IF(BU10>data!$I$9,Broadsheet!$BU$9,0),IF(BV10>data!$I$9,Broadsheet!$BV$9,0),IF(BW10>data!$I$9,Broadsheet!$BW$9,0),IF(BX10>data!$I$9,Broadsheet!$BX$9,0),IF(BY10>data!$I$9,Broadsheet!$BY$9,0),IF(BZ10>data!$I$9,Broadsheet!$BZ$9,0),IF(CA10>data!$I$9,Broadsheet!$CA$9,0),IF(CB10>data!$I$9,Broadsheet!$CB$9,0),IF(DG10>data!$I$9,Broadsheet!$DG$9,0),IF(DH10>data!$I$9,Broadsheet!$DH$9,0),IF(DI10>data!$I$9,Broadsheet!$DI$9,0),IF(DJ10>data!$I$9,Broadsheet!$DJ$9,0),IF(DK10>data!$I$9,Broadsheet!$DK$9,0),IF(DL10>data!$I$9,Broadsheet!$DL$9,0),IF(DM10>data!$I$9,Broadsheet!$DM$9,0),IF(DN10>data!$I$9,Broadsheet!$DN$9,0),IF(DO10>data!$I$9,Broadsheet!$DO$9,0),IF(DP10>data!$I$9,Broadsheet!$DP$9,0),IF(DQ10>data!$I$9,Broadsheet!$DQ$9,0))
        '=SUM(IF(BO10>data!$I$9,Broadsheet!$BO$9,0),
        strFml = "="
        For j = startCol To startCol + maxCoursesFS
            Col = lettersToNum(j)
            strFml = strFml & String.Format("IF({0}{1}>data!$I$9, ${0}${2}, 0)+", Col, startRow, headRowCourse, headRowCredit) & " & "
        Next
        strFml = strFml.Replace("+"c, """"c)
        Debug.Print(strFml)



        'TCP
        '=SUM(IF(AK10>data!$I$9,Broadsheet!$AK$9,0),IF(AL10>data!$I$9,Broadsheet!$AL$9,0),IF(AM10>data!$I$9,Broadsheet!$AM$9,0),IF(AN10>data!$I$9,Broadsheet!$AN$9,0))
        Return strRet
    End Function
    Function updateExcelBroadSheetInterop(dv As DataView, resultfileNameValue As String, generatedBroadsheetFileName As String) As String
        Dim dt As DataTable = dv.ToTable
        Dim strCriteria As String = String.Empty
        Dim defaultStartRow As Integer = 1
        Dim startCol As Integer = 1
        Dim endCol As Integer = 150 'todo:check
        Dim firstSemStartCol As Integer = 8 'todo:check
        Dim firstSemEndCol As Integer = 8 + 55 'todo:check
        Dim secondSemStartCol As Integer = 8 + 55 + 3 'todo:check
        Dim secondSemEndCol As Integer = (8 + 55 + 3) + 55 'todo:check
        Dim headerRow As Integer = 8
        Dim endRow As Integer = 200
        Dim endRowTemplate As Integer = 280
        Dim startColLevel As Integer = 20
        Dim endColLevel As Integer = 7
        Dim startRow As Integer = 10  'starts at row 10
        Dim strSQLTemp As String = ""
        Dim i As Integer = 0
        Dim tmpStr As String = Nothing
        Dim tmpCount As Integer = 0

        Dim strCellContent As String = ""
        Dim strCellContent2 As String = ""

        Dim templateCourses1(55) As String
        Dim templateCourses2(55) As String

        Dim fn As String = Nothing

        'template matching numbers
        startRow = 10
        headerRow = 7

        'broadshet data numbers
        endRow = dt.Rows.Count

        Try
            excelApp = New ExcelInterop.Application
            If resultfileNameValue = Nothing Or Not System.IO.File.Exists(resultfileNameValue) Then Exit Function 'todo
            excelWB = excelApp.Workbooks.Open(resultfileNameValue)
            excelWS = CType(excelWB.Sheets(1), ExcelInterop.Worksheet)

            'The Courses in the template (Use assuming it overrides soft)
            tmpCount = templateCourses1.Count
            For j = firstSemStartCol To tmpCount - 1
                templateCourses1(i) = dt.Rows(headerRow).Item(j).ToString          'excelWS.Cells(headerRow, ExcelColumns.colH + j)
            Next
            Me.progress = 30 'update progress bar
        Catch ex As Exception
            Throw ex
        End Try


        excelApp.Visible = True

        MsgBox("Excel will be launched make sure it is running properlyy with no dialog box open then clic ok to continue")

        Dim strFormulaTC As String = "=H{0} + I{0} + J{0}" ' "=H9 + I9 + J9"
        Dim strFormulaFullName As String = "=D{0} + E{0}" ' D9+E9" wont need fml if template is good
        Dim endDGVCol As Integer
        endDGVCol = dt.Columns.Count

        If endDGVCol <> endCol Then
            MsgBox("The generated broadsheet data does not match the template",, "Warning!")
            endCol = endDGVCol
        End If

        'Delete extra rows early to make spreadsheet lighter
        excelWS.Rows(endRow & ":200").delete() 'e.g 60:200    excelWS.Cells.Range("A" & endRow & ":Z150").Delete()
        'resultfileNameValue = "Processed-" & Now & "-" & resultfileNameValue    'todo: avoid extension .xls
        'excelWS.SaveAs(resultfileNameValue)
        objBroadsheet.progress = 40
        'TODO: modifying cells when a dialog is open in excel (eg unlicenced version) throws error. tip sendkeyd(enter) on error
        For i = 0 To startRow - 1
            'headers The header rows dont come from datagrid view
            If i = 1 Then excelWS.Cells(i, 1) = "Department Name"
            If i = 2 Then excelWS.Cells(i, 1) = "Faculty Name"
            If i = 3 Then excelWS.Cells(i, 1) = "University  Name"
            If i = 4 Then excelWS.Cells(i, 1) = "2020/2021 Academic Session "
        Next


        'data row headings
        For j = 0 To endDGVCol - 1
            If Not (IsDBNull(dt.Rows(headerRow).Item(j)) Or dt.Columns(j).ColumnName Is Nothing) Then
                excelWS.Cells(headerRow + 1, j + 1) = dt.Columns(j).ColumnName 'excelWS.Cells(i, ExcelColumns.colA) = i
            Else
                excelWS.Cells(headerRow + 1, j + 1) = "Error"
            End If
            objBroadsheet.progress = j / endDGVCol * 60
        Next



        For i = startRow To endRow - 1
            'students info
            For j = 0 To firstSemStartCol - 1
                If Not (IsDBNull(dt.Rows(i).Item(j)) Or dt.Rows(i - startRow).Item(j).ToString Is Nothing) Then
                    excelWS.Cells(i, (j + 1)) = dt.Rows(i - startRow).Item(j).ToString
                ElseIf (j >= firstSemStartCol And j <= firstSemEndCol) Then
                    excelWS.Cells(i, (j + 1)) = "-6"    'String.Format(strFormulaFullName, i, i, i) 
                Else
                    'do nothing
                End If
            Next
            'first sem esults
            For j = firstSemStartCol To firstSemEndCol - 1  'first semester
                'excelWS.Cells(i + 1, j + 1) = dt.Rows(i).Item(j).ToString 'excelWS.Cells(i, ExcelColumns.colA) = i
                'String.Format(strFormulaFullName, i, i, i) 
            Next
            For j = secondSemStartCol To endCol - 1 '2nd semester less 
                excelWS.Cells(i, (j + 1)) = dt.Rows(i).Item(j).ToString 'excelWS.Cells(i, ExcelColumns.colA) = i
                'String.Format(strFormulaFullName, i, i, i) 
            Next
            'Pecific cols
            excelWS.Cells(i + 1, ExcelColumns.colZ + 1) = "GPA" '=calcGPA(array_of_scores)
            excelWS.Cells(i + 1, ExcelColumns.colY + 1) = "2.5"
            excelWS.Cells(i + 1, ExcelColumns.colQ + 1) = String.Format(strFormulaTC, i, i, i)  'gpa formula
            objBroadsheet.progress = i / endRow * 80
        Next






        'With excelWS.Cells.Range(_range).Borders(ExcelInterop.XlBordersIndex.xlEdgeLeft)
        '    excelWS.Cells.Range("B" & startRow & ":B" & r).Select() 'select rows
        'End With
        'excelWS.Cells.Range("B" & startRow & ":B" & r).Copy()
        'Debug.Print(My.Computer.Clipboard.GetData("range").ToString)
        'Call selectCells("A" & lastRow & ":Z150" )


        'clean up
        Try
            fn = broadsheetFileName & "_saved" & endRow & Rnd(5).ToString
            If Not System.IO.File.Exists(generatedBroadsheetFileName) Then
                excelWB.SaveAs(generatedBroadsheetFileName)
            Else
                Throw New Exception("RTPS Error: Excel File Already Exists!")
            End If
            'excelWB.Save()
            excelApp.Quit()

            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelWB)
            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelWS)
            'clean up variables
            objBroadsheet.progress = 90
            dt = Nothing
            startRow = Nothing
            excelWS = Nothing
            excelWB = Nothing
            excelApp = Nothing

            'to do track process id of excel and kill it
            GC.Collect() 'Best way to close excel NOTE: It works in release but youmay not notice in debug mode

        Catch ex As Exception
            MsgBox(ex.Message)

            'Some errors the object has disconnected with the client
        End Try


        'Scrap stuff--------------------


        Return fn
    End Function
    Function updateExcelBroadSheetInteropManualwithoutTemplateMatching(resultfileNameValue As String, dt As DataSet) As String
        Dim strCriteria As String = String.Empty
        Dim startRow As Integer = 1
        Dim headerRow As Integer = 8
        Dim endRow As Integer = 200
        Dim r As Integer = 9  'starts at row 9
        Dim strSQLTemp As String = ""
        Dim i As Integer = 0
        Dim strMATNO As String = Nothing
        'Dim strArrayMATNO, strArrayNAME, strArrayCA, strArrayEXAM, strArraySCORE, strArraySURNAME, strArrayOtherNames As String()

        Dim strCellContent As String = ""
        Dim strCellContent2 As String = ""
        Dim MATNoFound As Boolean = False
        Dim templateCourses(50) As String
        Dim fn As String = Nothing
        Try
            excelApp = New ExcelInterop.Application

            If resultfileNameValue = Nothing Or Not System.IO.File.Exists(resultfileNameValue) Then Exit Function 'todo
            excelWB = excelApp.Workbooks.Open(resultfileNameValue)
            excelWS = CType(excelWB.Sheets(1), ExcelInterop.Worksheet)


            r = 8 'textbox
            'Search for MATNO Column
            For i = 1 To 20
                strCellContent = excelWS.Cells(i, ExcelColumns.colB).text
                strCellContent2 = excelWS.Cells(i, ExcelColumns.colA).text
                If strCellContent.ToUpper.Contains("MAT") Or strCellContent2.ToUpper.Contains("MAT") Then
                    startRow = i + 2    'allow one for course credit
                    MATNoFound = True
                    Exit For
                End If
            Next
            headerRow = i
            'The Courses in the template (Use assuming it overrides soft)
            For j = 0 To templateCourses.Count - 1
                templateCourses(i) = excelWS.Cells(headerRow, ExcelColumns.colH + j)

            Next
            'check for last row
            'TODO: use a better method to check for usedrange
            'endRow = excelWS.UsedRange.Rows.Count
            'For i = startRow To 300
            '    strCellContent = excelWS.Cells(i, ExcelColumns.colB).text

            '    If strCellContent.ToString.Length = 0 Then
            '        endRow = i - 1
            '        Exit For
            '    End If
            'Next
            endRow = dt.Tables(0).Rows.Count
            Me.progress = 30 'update progress bar
        Catch ex As Exception
            Throw ex
        End Try


        excelApp.Visible = True 'TODO: check


        Dim strFormulaTC As String = "H{0} + I{0} + J{0}" ' "=H9 + I9 + J9"
        Dim strFormulaFullName As String = "D{0} + E{0}" ' D9+E9" wont need fml if template is good
        For i = startRow To 50
            excelWS.Cells(i, ExcelColumns.colA) = i
            excelWS.Cells(i, ExcelColumns.colB) = i * 345 'TODO: place holder for MAT
            excelWS.Cells(i, ExcelColumns.colC) = String.Format(strFormulaFullName, i, i, i) ' "First Name SURNAME" 'TODO: place holder Name
            excelWS.Cells(i, ExcelColumns.colD) = "First Name" & i

            excelWS.Cells(i, ExcelColumns.colE) = dt.Tables(0).Rows(i).Item("Surname") ' "SURNAME" & i
            excelWS.Cells(i, ExcelColumns.colF) = "CPE333/2/55, CPE311/3/75," 'TODO: place holder CO  objbroadsheet.sturecord(MATNO).co
            excelWS.Cells(i, ExcelColumns.colG) = "*79" 'TODO: place holder course  objbroadsheet.sturecord(MATNO).co
            excelWS.Cells(i, ExcelColumns.colH) = "80" 'objbroadsheet.sturecord(MATNO).COURSE002.score
            excelWS.Cells(i, ExcelColumns.colI) = "68"
            excelWS.Cells(i, ExcelColumns.colI) = dt.Tables(0).Rows(i).Item(templateCourses(0))
            '...

            'Total credits
            excelWS.Cells(i, ExcelColumns.colO) = "3" 'objbroadsheet.sturecord(MATNO).COURSE003.FAILED1STsEM  or calcFailed(courses1stSem)
            excelWS.Cells(i, ExcelColumns.colP) = "24" 'objbroadsheet.sturecord(MATNO).COURSE003.Passed1STsEM  or calcPassed(courses1stSem)
            excelWS.Cells(i, ExcelColumns.colQ) = String.Format(strFormulaTC, i, i, i)
        Next
        Debug.Print(excelWS.UsedRange.Rows.Count) ',  'excelWS.UsedRange.FillDown(), 'excelWS.UsedRange.Hidden, 'excelWS.UsedRange.RowHeight = 3
        'excelWS.UsedRange.ShrinkToFit, 'excelWS.UsedRange.Cells.Text  'excelApp.ExecuteExcel4Macro()  excelApp.Version
        Debug.Print(excelWS.UsedRange.Cells.Text)
        'If r > 6 Then Call selectCells("B9:B" & r)
        'With excelWS.Cells.Range(_range).Borders(ExcelInterop.XlBordersIndex.xlEdgeLeft)
        '    excelWS.Cells.Range("B" & startRow & ":B" & r).Select() 'select rows
        'End With
        'excelWS.Cells.Range("B" & startRow & ":B" & r).Copy()
        'Debug.Print(My.Computer.Clipboard.GetData("range").ToString)
        strMATNO = My.Computer.Clipboard.GetData("range")

        'clean up
        Try
            fn = broadsheetFileName & "_saved" & endRow & Rnd(5).ToString
            excelWB.SaveAs(broadsheetFileName & "_saved" & endRow & Rnd(5).ToString)
            excelApp.Quit()

            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelWB)
            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelWS)
            'clean up variables

            r = Nothing
            excelWS = Nothing
            excelWB = Nothing
            excelApp = Nothing

            GC.Collect() 'Best way to close excel NOTE: It works in release but youmay not notice in debug mode

        Catch ex As Exception
            MsgBox(ex.Message)
        End Try


        'Scrap stuff--------------------


        Return fn
    End Function

    Function iterateRegisteredCoursesAndAddToBroadsheetTemplate() As String()
        Dim firstSemCourses As List(Of String) = Nothing
        Dim secondSemCourses As List(Of String) = Nothing

        ' possibilities: secondSemCourses.Contains(), secondSemCourses.FindAll, secondSemCourses.ToLookup


        'firstsemcourses.sort ?   
        'for each matno in me.reginfo
        ' in not firstsemcourses.contains(Me._regInfoCoursesFirstSem.course(i)) then firstsemcourses.add(Me.reginfo.course(i))

        'next
        Return Nothing
    End Function

    'Function openBroadsheetExcelWB() As Boolean
    '    Me.excelWB.openBroadsheetExcelWB()

    '    Return True
    'End Function
    Sub ExcelPDF()
        'Dim workbook As New ExcelInterop.Workbook()
        'workbook.LoadFromFile("D:\test.xlsx")

        'workbook.ActiveSheet.ExportAsFixedFormat(xlTypePDF, "D:\test.pdf")




    End Sub
    'Option Strict Off 'Required for Late Binding
    Sub ExcelPDFLateBinding()
        Dim xl As Object
        xl = CreateObject("Excel.Application")
        Dim xwb As Object = xl.Workbooks.Open("D:\test.xlsx")
        xwb.ActiveSheet.ExportAsFixedFormat(0, "D:\sample.pdf")
        xl.Quit()
    End Sub
End Class
