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
        End Class

        Private Shared Function Columna(clave As String, encabezado As String, valor As Func(Of TarjetaEncontrada, Object)) As ColumnaExcel
            Return New ColumnaExcel With {.Clave = clave, .Encabezado = encabezado, .Valor = valor}
        End Function

        ''' <summary>
        ''' Hoja con una fila por tarjeta. Con <paramref name="columnas"/> (claves de ColumnaBusqueda: las visibles en el
        ''' buscador) solo salen el título y las columnas de Excel de esas claves; sin ellas (Nothing), todas.
        ''' </summary>
        Public Shared Sub AgregarHojaTarjetas(libro As LibroExcel, nombre As String, tarjetas As IEnumerable(Of TarjetaEncontrada),
                                              Optional columnas As IEnumerable(Of String) = Nothing)
            Dim hoy = DateTime.Today
            Dim todas = New List(Of ColumnaExcel) From {
                Columna("id", "ID", Function(e) e.Tarjeta.Id),
                Columna("caso", "Caso", Function(e) e.Tarjeta.Caso),
                Columna("titulo", "Título", Function(e) e.Tarjeta.Titulo),
                Columna("etiquetas", "Etiquetas", Function(e) String.Join(", ", e.Tarjeta.Etiquetas.Select(Function(et) et.Nombre))),
                Columna("prioridad", "Prioridad", Function(e) Prioridad.Nombre(e.Tarjeta.Prioridad)),
                Columna("tablero", "Tablero", Function(e) e.NombreTablero),
                Columna("tablero", "Lista", Function(e) e.Tarjeta.NombreLista),
                Columna("asignado", "Asignada a", Function(e) e.Tarjeta.NombreAsignado),
                Columna("limite", "Fecha límite", Function(e) If(e.Tarjeta.FechaLimite.HasValue, CObj(e.Tarjeta.FechaLimite.Value.Date), Nothing)),
                Columna("limite", "Estado fecha", Function(e) EstadoFecha(e.Tarjeta.FechaLimite, hoy)),
                Columna("subtareas", "Subtareas hechas", Function(e) e.Tarjeta.SubtareasHechas),
                Columna("subtareas", "Subtareas total", Function(e) e.Tarjeta.SubtareasTotal),
                Columna("adjuntos", "Adjuntos", Function(e) e.Tarjeta.NumAdjuntos),
                Columna(Nothing, "Mensajes de chat", Function(e) e.Tarjeta.NumComentarios),
                Columna(Nothing, "Tiene código", Function(e) e.Tarjeta.TieneCodigo),
                Columna(Nothing, "Descripción", Function(e) e.Tarjeta.Descripcion),
                Columna("creada", "Creada", Function(e) ALocal(e.Tarjeta.FechaCreacion)),
                Columna("modificada", "Modificada", Function(e) ALocal(e.Tarjeta.FechaModificacion)),
                Columna("modificada", "Modificada por", Function(e) e.Tarjeta.NombreModificador)
            }

            Dim elegidas = todas
            If columnas IsNot Nothing Then
                Dim visibles = New HashSet(Of String)(ColumnaBusqueda.Validar(columnas))
                elegidas = todas.Where(Function(c) c.Clave = "titulo" OrElse (c.Clave IsNot Nothing AndAlso visibles.Contains(c.Clave))).ToList()
            End If

            Dim filas = tarjetas.Select(Function(e) elegidas.Select(Function(c) c.Valor(e)).ToArray())
            libro.AgregarHoja(nombre, elegidas.Select(Function(c) c.Encabezado).ToArray(), filas)
        End Sub

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
