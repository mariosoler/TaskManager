Imports TaskManager.Models

Namespace Utilidades

    ''' <summary>Hojas de Excel comunes a la exportación del buscador y de las estadísticas.</summary>
    Public NotInheritable Class ExportacionExcel

        Private Sub New()
        End Sub

        Public Shared Sub AgregarHojaTarjetas(libro As LibroExcel, nombre As String, tarjetas As IEnumerable(Of TarjetaEncontrada))
            Dim hoy = DateTime.Today
            Dim encabezados = {"Caso", "Título", "Etiquetas", "Prioridad", "Tablero", "Lista", "Asignada a", "Fecha límite", "Estado fecha",
                               "Subtareas hechas", "Subtareas total", "Mensajes de chat", "Tiene código", "Descripción",
                               "Creada", "Modificada", "Modificada por"}

            Dim filas = tarjetas.Select(Function(e)
                                            Dim t = e.Tarjeta
                                            Return New Object() {
                                                t.Caso, t.Titulo, String.Join(", ", t.Etiquetas.Select(Function(et) et.Nombre)), Prioridad.Nombre(t.Prioridad), e.NombreTablero, t.NombreLista, t.NombreAsignado,
                                                If(t.FechaLimite.HasValue, CObj(t.FechaLimite.Value.Date), Nothing),
                                                EstadoFecha(t.FechaLimite, hoy),
                                                t.SubtareasHechas, t.SubtareasTotal, t.NumComentarios, t.TieneCodigo,
                                                t.Descripcion,
                                                ALocal(t.FechaCreacion), ALocal(t.FechaModificacion), t.NombreModificador}
                                        End Function)
            libro.AgregarHoja(nombre, encabezados, filas)
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
