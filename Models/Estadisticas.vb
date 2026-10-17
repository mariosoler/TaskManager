Namespace Models

    ''' <summary>Un valor de una gráfica: etiqueta (texto del usuario, codificar al mostrar) y cantidad.</summary>
    Public Class Dato
        Public Property Etiqueta As String
        Public Property Valor As Integer

        ''' <summary>Marca opcional para el estilo (p. ej. "sin-asignar" se pinta en gris).</summary>
        Public Property Clave As String
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

        ' --- Actividad (historial) ---
        Public Property Dias As Integer
        Public Property TotalCambios As Integer
        Public Property CambiosPorDia As New List(Of DatoDia)
        Public Property CambiosPorPersona As New List(Of Dato)
        Public Property CambiosPorTipo As New List(Of Dato)
    End Class

End Namespace
