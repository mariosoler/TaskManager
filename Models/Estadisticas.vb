Namespace Models

    ''' <summary>Un valor de una gráfica: etiqueta (texto del usuario, codificar al mostrar) y cantidad.</summary>
    Public Class Dato
        Public Property Etiqueta As String
        Public Property Valor As Integer

        ''' <summary>Marca opcional para el estilo (p. ej. "sin-asignar" se pinta en gris).</summary>
        Public Property Clave As String
    End Class

    ''' <summary>Tarjetas de un tablero que llevan una etiqueta.</summary>
    Public Class DatoEtiqueta
        Public Property Etiqueta As Etiqueta
        Public Property Tarjetas As Integer
        Public Property Vencidas As Integer
        Public Property SinAsignar As Integer
    End Class

    ''' <summary>Tarjeta con fecha límite próxima o vencida (tabla «Próximos vencimientos»).</summary>
    Public Class TarjetaVencimiento
        Public Property Id As Integer
        Public Property Titulo As String
        Public Property Caso As String
        Public Property NombreLista As String
        Public Property NombreAsignado As String
        Public Property FechaLimite As DateTime
        Public Property Prioridad As Integer?
    End Class

    Public Class DatoDia
        Public Property Dia As DateTime   ' día local
        Public Property Valor As Integer
    End Class

    ''' <summary>Estadísticas de un tablero: estado actual + actividad del periodo elegido.</summary>
    Public Class Estadisticas
        ' --- Estado actual ---
        Public Property TotalTarjetas As Integer
        Public Property Vencidas As Integer
        Public Property VencenEstaSemana As Integer   ' hoy y los 7 días siguientes
        Public Property VencenMasAdelante As Integer
        Public Property SinFecha As Integer
        Public Property SinAsignar As Integer
        Public Property SubtareasTotal As Integer
        Public Property SubtareasHechas As Integer
        Public Property Comentarios As Integer
        Public Property PorLista As New List(Of Dato)
        Public Property PorPersona As New List(Of Dato)
        Public Property PorEtiqueta As New List(Of DatoEtiqueta)   ' solo etiquetas con tarjetas; las más usadas primero
        Public Property SinEtiqueta As Integer
        Public Property PorPrioridad As New List(Of Dato)          ' Clave = alta / media / baja / sin
        Public Property PrioridadAlta As Integer
        Public Property Antiguedad As New List(Of Dato)            ' tramos de días desde la creación
        Public Property SinActividad As Integer                    ' sin cambios en los últimos DiasSinActividad días
        Public Property ProximosVencimientos As New List(Of TarjetaVencimiento)

        ' --- Actividad (historial) ---
        Public Property Dias As Integer
        Public Property TotalCambios As Integer
        Public Property CreadasPeriodo As Integer                  ' tarjetas creadas en el periodo (que siguen existiendo)
        Public Property CreadasPeriodoAnterior As Integer          ' las mismas, en los Dias anteriores al periodo
        Public Property CambiosPorDia As New List(Of DatoDia)
        Public Property CambiosPorPersona As New List(Of Dato)
        Public Property CambiosPorTipo As New List(Of Dato)
    End Class

End Namespace
