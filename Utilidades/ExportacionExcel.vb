Imports System.Globalization
Imports TaskManager.Models

Namespace Utilidades

    ''' <summary>Hojas de Excel comunes a la exportación del buscador y de las estadísticas.</summary>
    Public NotInheritable Class ExportacionExcel

        Private Sub New()
        End Sub

        ''' <summary>Columna de la hoja de tarjetas. Clave = columna del buscador a la que corresponde (ColumnaBusqueda);
        ''' "titulo" = siempre; Nothing = solo en la exportación completa (no hay columna así en pantalla).</summary>
        Private NotInheritable Class ColumnaExcel
            Public Property Clave As String
            Public Property Encabezado As String
            Public Property Valor As Func(Of TarjetaEncontrada, Object)
            ''' <summary>Texto libre o con ceros a la izquierda: al copiar, se pega en Excel como texto (ni número, ni fecha, ni fórmula).</summary>
            Public Property EsTexto As Boolean
        End Class

        Private Shared Function Columna(clave As String, encabezado As String, valor As Func(Of TarjetaEncontrada, Object),
                                        Optional esTexto As Boolean = False) As ColumnaExcel
            Return New ColumnaExcel With {.Clave = clave, .Encabezado = encabezado, .Valor = valor, .EsTexto = esTexto}
        End Function

        ''' <summary>
        ''' Hoja con una fila por tarjeta. Con <paramref name="columnas"/> (claves de ColumnaBusqueda: las visibles en el
        ''' buscador) solo salen el título y las columnas de Excel de esas claves; sin ellas (Nothing), todas.
        ''' </summary>
        Public Shared Sub AgregarHojaTarjetas(libro As LibroExcel, nombre As String, tarjetas As IEnumerable(Of TarjetaEncontrada),
                                              Optional columnas As IEnumerable(Of String) = Nothing)
            Dim todas = ColumnasTarjetas(DateTime.Today)

            Dim elegidas = todas
            If columnas IsNot Nothing Then
                Dim visibles = New HashSet(Of String)(ColumnaBusqueda.Validar(columnas))
                elegidas = todas.Where(Function(c) c.Clave = "titulo" OrElse (c.Clave IsNot Nothing AndAlso visibles.Contains(c.Clave))).ToList()
            End If

            Dim filas = tarjetas.Select(Function(e) elegidas.Select(Function(c) c.Valor(e)).ToArray())
            libro.AgregarHoja(nombre, elegidas.Select(Function(c) c.Encabezado).ToArray(), filas)
        End Sub

        ' ----- «Copiar al portapapeles» del buscador (Scripts/buscar.js) -----
        ' Mismas columnas que el Excel, salvo las que no tienen columna en pantalla; el script copia las visibles.

        ''' <summary>
        ''' Columnas que se pueden copiar: k = clave de ColumnaBusqueda (el título, "titulo"), n = encabezado,
        ''' t = texto (en Excel no se convierte en número, fecha ni fórmula: conserva los ceros del caso).
        ''' </summary>
        Public Shared Function ColumnasPortapapeles() As IEnumerable(Of Object)
            Return ColumnasTarjetas(DateTime.Today).Where(Function(c) c.Clave IsNot Nothing).
                Select(Function(c) CObj(New With {.k = c.Clave, .n = c.Encabezado, .t = c.EsTexto})).ToList()
        End Function

        ''' <summary>
        ''' Valores de una tarjeta para el portapapeles, en el orden de <see cref="ColumnasPortapapeles"/>, como texto:
        ''' fechas en el formato corto de la referencia cultural (Excel las reconoce al pegar), sin hora la fecha límite.
        ''' </summary>
        Public Shared Function FilaPortapapeles(tarjeta As TarjetaEncontrada) As String()
            Return ColumnasTarjetas(DateTime.Today).Where(Function(c) c.Clave IsNot Nothing).
                Select(Function(c) TextoPortapapeles(c.Valor(tarjeta), c.Clave = "limite")).ToArray()
        End Function

        Private Shared Function TextoPortapapeles(valor As Object, soloDia As Boolean) As String
            If valor Is Nothing Then Return ""
            If TypeOf valor Is DateTime Then Return CDate(valor).ToString(If(soloDia, "d", "g"), CultureInfo.CurrentCulture)
            If TypeOf valor Is Integer Then Return CInt(valor).ToString(CultureInfo.InvariantCulture)
            Return Convert.ToString(valor, CultureInfo.CurrentCulture)
        End Function

        Private Shared Function ColumnasTarjetas(hoy As DateTime) As List(Of ColumnaExcel)
            Return New List(Of ColumnaExcel) From {
                Columna("id", "ID", Function(e) e.Tarjeta.Id),
                Columna("caso", "Caso", Function(e) e.Tarjeta.Caso, esTexto:=True),
                Columna("titulo", "Título", Function(e) e.Tarjeta.Titulo, esTexto:=True),
                Columna("etiquetas", "Etiquetas", Function(e) String.Join(", ", e.Tarjeta.Etiquetas.Select(Function(et) et.Nombre)), esTexto:=True),
                Columna("prioridad", "Prioridad", Function(e) Prioridad.Nombre(e.Tarjeta.Prioridad), esTexto:=True),
                Columna("tablero", "Tablero", Function(e) e.NombreTablero, esTexto:=True),
                Columna("tablero", "Lista", Function(e) e.Tarjeta.NombreLista, esTexto:=True),
                Columna("asignado", "Asignada a", Function(e) e.Tarjeta.NombreAsignado, esTexto:=True),
                Columna("limite", "Fecha límite", Function(e) If(e.Tarjeta.FechaLimite.HasValue, CObj(e.Tarjeta.FechaLimite.Value.Date), Nothing)),
                Columna("limite", "Estado fecha", Function(e) EstadoFecha(e.Tarjeta.FechaLimite, hoy), esTexto:=True),
                Columna("subtareas", "Subtareas hechas", Function(e) e.Tarjeta.SubtareasHechas),
                Columna("subtareas", "Subtareas total", Function(e) e.Tarjeta.SubtareasTotal),
                Columna("adjuntos", "Adjuntos", Function(e) e.Tarjeta.NumAdjuntos),
                Columna(Nothing, "Mensajes de chat", Function(e) e.Tarjeta.NumComentarios),
                Columna(Nothing, "Tiene código", Function(e) e.Tarjeta.TieneCodigo),
                Columna(Nothing, "Descripción", Function(e) e.Tarjeta.Descripcion, esTexto:=True),
                Columna("creada", "Creada", Function(e) ALocal(e.Tarjeta.FechaCreacion)),
                Columna("modificada", "Modificada", Function(e) ALocal(e.Tarjeta.FechaModificacion)),
                Columna("modificada", "Modificada por", Function(e) e.Tarjeta.NombreModificador, esTexto:=True)
            }
        End Function

        Public Shared Function EstadoFecha(fechaLimite As DateTime?, hoy As DateTime) As String
            If Not fechaLimite.HasValue Then Return "Sin fecha"
            Dim dia = fechaLimite.Value.Date
            If dia < hoy Then Return "Vencida"
            If dia <= hoy.AddDays(7) Then Return "Vence en 7 días"
            Return "Más adelante"
        End Function

        ''' <summary>Fecha UTC a hora local, redondeada al minuto (para que Excel la muestre limpia).</summary>
        Private Shared Function ALocal(utc As DateTime) As DateTime
            Dim local = DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime()
            Return New DateTime(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0)
        End Function

    End Class

End Namespace
