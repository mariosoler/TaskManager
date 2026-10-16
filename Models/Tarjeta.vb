Namespace Models

    Public Class Tarjeta
        Public Property Id As Integer
        Public Property ListaId As Integer
        Public Property NombreLista As String
        Public Property Titulo As String
        Public Property Descripcion As String
        Public Property Posicion As Integer
        Public Property FechaLimite As DateTime?
        Public Property AsignadoA As Integer?
        Public Property NombreAsignado As String
        Public Property NumComentarios As Integer
        Public Property FechaCreacion As DateTime

        ''' <summary>Número de caso (solo dígitos) o Nothing.</summary>
        Public Property Caso As String

        ''' <summary>Apartado de código en markdown. Solo se carga al abrir la tarjeta (TarjetasDatos.Obtener).</summary>
        Public Property Codigo As String
        Public Property TieneCodigo As Boolean

        Public Property SubtareasTotal As Integer
        Public Property SubtareasHechas As Integer

        ''' <summary>Última modificación (UTC) y quién la hizo (Nothing en tarjetas anteriores al historial).</summary>
        Public Property FechaModificacion As DateTime
        Public Property NombreModificador As String
    End Class

End Namespace
