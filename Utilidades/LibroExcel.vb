Imports System.Globalization
Imports System.IO
Imports System.IO.Compression
Imports System.Text
Imports System.Xml

Namespace Utilidades

    ''' <summary>
    ''' Genera un libro .xlsx (Office Open XML) sin dependencias externas.
    ''' - Fila de encabezado en negrita, inmovilizada y con autofiltro.
    ''' - Texto como "inlineStr": Excel nunca lo interpreta como fórmula (evita la inyección de
    '''   fórmulas tipo "=HYPERLINK(...)" que sí afecta a los CSV).
    ''' - Números como números y fechas como fechas de Excel (DateTime sin hora = fecha; con hora = fecha y hora).
    ''' </summary>
    Public Class LibroExcel

        Public Const TipoMime As String = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"

        Private Const MaxTextoCelda As Integer = 32767
        Private Const NsMain As String = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
        Private Const NsRel As String = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
        Private Const NsPkgRel As String = "http://schemas.openxmlformats.org/package/2006/relationships"

        ' Índices de estilo (cellXfs en styles.xml)
        Private Const EstiloEncabezado As Integer = 1
        Private Const EstiloFecha As Integer = 2
        Private Const EstiloFechaHora As Integer = 3

        Private Class Hoja
            Public Nombre As String
            Public Encabezados As String()
            Public Filas As List(Of Object())
        End Class

        Private ReadOnly _hojas As New List(Of Hoja)

        Public Sub AgregarHoja(nombre As String, encabezados As String(), filas As IEnumerable(Of Object()))
            _hojas.Add(New Hoja With {.Nombre = NombreHojaValido(nombre), .Encabezados = encabezados, .Filas = filas.ToList()})
        End Sub

        Public Function ABytes() As Byte()
            If _hojas.Count = 0 Then AgregarHoja("Hoja1", {""}, New List(Of Object()))
            Using ms As New MemoryStream()
                Using zip As New ZipArchive(ms, ZipArchiveMode.Create, leaveOpen:=True)
                    Escribir(zip, "[Content_Types].xml", AddressOf EscribirTiposContenido)
                    Escribir(zip, "_rels/.rels", AddressOf EscribirRelacionesPaquete)
                    Escribir(zip, "xl/workbook.xml", AddressOf EscribirLibro)
                    Escribir(zip, "xl/_rels/workbook.xml.rels", AddressOf EscribirRelacionesLibro)
                    EscribirTexto(zip, "xl/styles.xml", Estilos)
                    For i = 0 To _hojas.Count - 1
                        Dim hoja = _hojas(i)
                        Escribir(zip, "xl/worksheets/sheet" & (i + 1).ToString(CultureInfo.InvariantCulture) & ".xml",
                                 Sub(w) EscribirHoja(w, hoja))
                    Next
                End Using
                Return ms.ToArray()
            End Using
        End Function

        ' ----- Partes del paquete -----

        Private Shared Sub Escribir(zip As ZipArchive, ruta As String, contenido As Action(Of XmlWriter))
            Dim entrada = zip.CreateEntry(ruta, CompressionLevel.Optimal)
            Using s = entrada.Open(),
                  w = XmlWriter.Create(s, New XmlWriterSettings With {.Encoding = New UTF8Encoding(False)})
                w.WriteStartDocument(True)
                contenido(w)
                w.WriteEndDocument()
            End Using
        End Sub

        Private Sub EscribirTiposContenido(w As XmlWriter)
            Const ns = "http://schemas.openxmlformats.org/package/2006/content-types"
            w.WriteStartElement("Types", ns)
            EscribirDefault(w, ns, "rels", "application/vnd.openxmlformats-package.relationships+xml")
            EscribirDefault(w, ns, "xml", "application/xml")
            EscribirOverride(w, ns, "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")
            EscribirOverride(w, ns, "/xl/styles.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml")
            For i = 1 To _hojas.Count
                EscribirOverride(w, ns, "/xl/worksheets/sheet" & i.ToString(CultureInfo.InvariantCulture) & ".xml",
                                 "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")
            Next
            w.WriteEndElement()
        End Sub

        Private Shared Sub EscribirDefault(w As XmlWriter, ns As String, extension As String, tipo As String)
            w.WriteStartElement("Default", ns)
            w.WriteAttributeString("Extension", extension)
            w.WriteAttributeString("ContentType", tipo)
            w.WriteEndElement()
        End Sub

        Private Shared Sub EscribirOverride(w As XmlWriter, ns As String, parte As String, tipo As String)
            w.WriteStartElement("Override", ns)
            w.WriteAttributeString("PartName", parte)
            w.WriteAttributeString("ContentType", tipo)
            w.WriteEndElement()
        End Sub

        Private Shared Sub EscribirRelacionesPaquete(w As XmlWriter)
            w.WriteStartElement("Relationships", NsPkgRel)
            EscribirRelacion(w, "rId1", NsRel & "/officeDocument", "xl/workbook.xml")
            w.WriteEndElement()
        End Sub

        Private Sub EscribirRelacionesLibro(w As XmlWriter)
            w.WriteStartElement("Relationships", NsPkgRel)
            For i = 1 To _hojas.Count
                EscribirRelacion(w, "rId" & i.ToString(CultureInfo.InvariantCulture), NsRel & "/worksheet",
                                 "worksheets/sheet" & i.ToString(CultureInfo.InvariantCulture) & ".xml")
            Next
            EscribirRelacion(w, "rId" & (_hojas.Count + 1).ToString(CultureInfo.InvariantCulture), NsRel & "/styles", "styles.xml")
            w.WriteEndElement()
        End Sub

        Private Shared Sub EscribirRelacion(w As XmlWriter, id As String, tipo As String, destino As String)
            w.WriteStartElement("Relationship", NsPkgRel)
            w.WriteAttributeString("Id", id)
            w.WriteAttributeString("Type", tipo)
            w.WriteAttributeString("Target", destino)
            w.WriteEndElement()
        End Sub

        Private Sub EscribirLibro(w As XmlWriter)
            w.WriteStartElement("workbook", NsMain)
            w.WriteAttributeString("xmlns", "r", Nothing, NsRel)
            w.WriteStartElement("sheets", NsMain)
            For i = 1 To _hojas.Count
                w.WriteStartElement("sheet", NsMain)
                w.WriteAttributeString("name", _hojas(i - 1).Nombre)
                w.WriteAttributeString("sheetId", i.ToString(CultureInfo.InvariantCulture))
                w.WriteAttributeString("id", NsRel, "rId" & i.ToString(CultureInfo.InvariantCulture))
                w.WriteEndElement()
            Next
            w.WriteEndElement()
            w.WriteEndElement()
        End Sub

        Private Shared Sub EscribirTexto(zip As ZipArchive, ruta As String, xml As String)
            Dim entrada = zip.CreateEntry(ruta, CompressionLevel.Optimal)
            Using s = entrada.Open(), escritor As New StreamWriter(s, New UTF8Encoding(False))
                escritor.Write(xml)
            End Using
        End Sub

        ' Hoja de estilos fija (no contiene datos del usuario).
        Private Const Estilos As String = "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
                "<styleSheet xmlns=""" & NsMain & """>" &
                "<numFmts count=""1""><numFmt numFmtId=""164"" formatCode=""dd/mm/yyyy hh:mm""/></numFmts>" &
                "<fonts count=""2""><font><sz val=""11""/><name val=""Calibri""/></font>" &
                "<font><b/><sz val=""11""/><name val=""Calibri""/></font></fonts>" &
                "<fills count=""3""><fill><patternFill patternType=""none""/></fill><fill><patternFill patternType=""gray125""/></fill>" &
                "<fill><patternFill patternType=""solid""><fgColor rgb=""FFE9F2FF""/><bgColor indexed=""64""/></patternFill></fill></fills>" &
                "<borders count=""1""><border><left/><right/><top/><bottom/><diagonal/></border></borders>" &
                "<cellStyleXfs count=""1""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/></cellStyleXfs>" &
                "<cellXfs count=""4"">" &
                "<xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>" &
                "<xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>" &
                "<xf numFmtId=""14"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0"" applyNumberFormat=""1""/>" &
                "<xf numFmtId=""164"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0"" applyNumberFormat=""1""/>" &
                "</cellXfs>" &
                "<cellStyles count=""1""><cellStyle name=""Normal"" xfId=""0"" builtinId=""0""/></cellStyles>" &
                "</styleSheet>"

        Private Shared Sub EscribirHoja(w As XmlWriter, hoja As Hoja)
            Dim columnas = Math.Max(hoja.Encabezados.Length, If(hoja.Filas.Count = 0, 0, hoja.Filas.Max(Function(f) f.Length)))
            Dim ultimaFila = hoja.Filas.Count + 1

            w.WriteStartElement("worksheet", NsMain)

            ' Encabezado inmovilizado
            w.WriteStartElement("sheetViews", NsMain)
            w.WriteStartElement("sheetView", NsMain)
            w.WriteAttributeString("workbookViewId", "0")
            w.WriteStartElement("pane", NsMain)
            w.WriteAttributeString("ySplit", "1")
            w.WriteAttributeString("topLeftCell", "A2")
            w.WriteAttributeString("activePane", "bottomLeft")
            w.WriteAttributeString("state", "frozen")
            w.WriteEndElement()
            w.WriteEndElement()
            w.WriteEndElement()

            ' Anchos aproximados según el contenido
            If columnas > 0 Then
                w.WriteStartElement("cols", NsMain)
                For c = 0 To columnas - 1
                    Dim ancho = AnchoColumna(hoja, c)
                    w.WriteStartElement("col", NsMain)
                    w.WriteAttributeString("min", (c + 1).ToString(CultureInfo.InvariantCulture))
                    w.WriteAttributeString("max", (c + 1).ToString(CultureInfo.InvariantCulture))
                    w.WriteAttributeString("width", ancho.ToString(CultureInfo.InvariantCulture))
                    w.WriteAttributeString("customWidth", "1")
                    w.WriteEndElement()
                Next
                w.WriteEndElement()
            End If

            w.WriteStartElement("sheetData", NsMain)
            EscribirFila(w, 1, hoja.Encabezados.Cast(Of Object)().ToArray(), esEncabezado:=True)
            For i = 0 To hoja.Filas.Count - 1
                EscribirFila(w, i + 2, hoja.Filas(i), esEncabezado:=False)
            Next
            w.WriteEndElement()

            If columnas > 0 Then
                w.WriteStartElement("autoFilter", NsMain)
                w.WriteAttributeString("ref", "A1:" & LetraColumna(columnas - 1) & ultimaFila.ToString(CultureInfo.InvariantCulture))
                w.WriteEndElement()
            End If

            w.WriteEndElement()
        End Sub

        Private Shared Sub EscribirFila(w As XmlWriter, numero As Integer, valores As Object(), esEncabezado As Boolean)
            Dim fila = numero.ToString(CultureInfo.InvariantCulture)
            w.WriteStartElement("row", NsMain)
            w.WriteAttributeString("r", fila)
            For c = 0 To valores.Length - 1
                Dim valor = valores(c)
                If valor Is Nothing OrElse TypeOf valor Is DBNull Then Continue For

                w.WriteStartElement("c", NsMain)
                w.WriteAttributeString("r", LetraColumna(c) & fila)

                If TypeOf valor Is DateTime Then
                    Dim fecha = CDate(valor)
                    w.WriteAttributeString("s", If(fecha.TimeOfDay = TimeSpan.Zero, EstiloFecha, EstiloFechaHora).ToString(CultureInfo.InvariantCulture))
                    w.WriteElementString("v", NsMain, fecha.ToOADate().ToString("R", CultureInfo.InvariantCulture))
                ElseIf TypeOf valor Is Integer OrElse TypeOf valor Is Long OrElse TypeOf valor Is Double OrElse TypeOf valor Is Decimal Then
                    If esEncabezado Then w.WriteAttributeString("s", EstiloEncabezado.ToString(CultureInfo.InvariantCulture))
                    w.WriteElementString("v", NsMain, Convert.ToString(valor, CultureInfo.InvariantCulture))
                Else
                    Dim texto = If(TypeOf valor Is Boolean, If(CBool(valor), "Sí", "No"), Convert.ToString(valor, CultureInfo.CurrentCulture))
                    w.WriteAttributeString("t", "inlineStr")
                    If esEncabezado Then w.WriteAttributeString("s", EstiloEncabezado.ToString(CultureInfo.InvariantCulture))
                    w.WriteStartElement("is", NsMain)
                    w.WriteStartElement("t", NsMain)
                    w.WriteAttributeString("xml", "space", Nothing, "preserve")
                    w.WriteString(TextoValido(texto))
                    w.WriteEndElement()
                    w.WriteEndElement()
                End If
                w.WriteEndElement()
            Next
            w.WriteEndElement()
        End Sub

        ' ----- Utilidades -----

        ''' <summary>A, B, ..., Z, AA, AB, ...</summary>
        Friend Shared Function LetraColumna(indice As Integer) As String
            Dim letras = ""
            Dim n = indice + 1
            While n > 0
                Dim resto = (n - 1) Mod 26
                letras = ChrW(AscW("A"c) + resto) & letras
                n = (n - 1) \ 26
            End While
            Return letras
        End Function

        Private Shared Function AnchoColumna(hoja As Hoja, columna As Integer) As Integer
            Dim maximo = If(columna < hoja.Encabezados.Length, If(hoja.Encabezados(columna), "").Length + 2, 8)
            For Each fila In hoja.Filas.Take(500)
                If columna >= fila.Length OrElse fila(columna) Is Nothing Then Continue For
                Dim largo = If(TypeOf fila(columna) Is DateTime, 16, Convert.ToString(fila(columna), CultureInfo.CurrentCulture).Length + 1)
                maximo = Math.Max(maximo, largo)
            Next
            Return Math.Min(Math.Max(maximo, 8), 60)
        End Function

        ''' <summary>Quita caracteres no válidos en XML y limita a la longitud máxima de una celda.</summary>
        Private Shared Function TextoValido(texto As String) As String
            Dim sb As New StringBuilder(texto.Length)
            For i = 0 To texto.Length - 1
                Dim ch = texto(i)
                If Char.IsHighSurrogate(ch) AndAlso i + 1 < texto.Length AndAlso Char.IsLowSurrogate(texto(i + 1)) Then
                    sb.Append(ch).Append(texto(i + 1))
                    i += 1
                ElseIf XmlConvert.IsXmlChar(ch) Then
                    sb.Append(ch)
                End If
            Next
            Return If(sb.Length > MaxTextoCelda, sb.ToString(0, MaxTextoCelda), sb.ToString())
        End Function

        ''' <summary>Nombre de hoja válido: máx. 31 caracteres, sin : \ / ? * [ ] y único.</summary>
        Private Function NombreHojaValido(nombre As String) As String
            Dim limpio = New String(If(nombre, "").Where(Function(ch) ":\/?*[]".IndexOf(ch) < 0 AndAlso Not Char.IsControl(ch)).ToArray()).Trim("'"c, " "c)
            If limpio.Length = 0 Then limpio = "Hoja" & (_hojas.Count + 1).ToString(CultureInfo.InvariantCulture)
            If limpio.Length > 31 Then limpio = limpio.Substring(0, 31)
            Dim candidato = limpio
            Dim n = 2
            While _hojas.Any(Function(h) String.Equals(h.Nombre, candidato, StringComparison.OrdinalIgnoreCase))
                Dim sufijo = " (" & n.ToString(CultureInfo.InvariantCulture) & ")"
                candidato = limpio.Substring(0, Math.Min(limpio.Length, 31 - sufijo.Length)) & sufijo
                n += 1
            End While
            Return candidato
        End Function

    End Class

End Namespace
