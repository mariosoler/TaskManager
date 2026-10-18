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

        ''' <summary>Cuándo entró la tarjeta en su lista actual (UTC): al crearla o al moverla a otra lista.</summary>
        Public Property FechaEnLista As DateTime

        ''' <summary>1 = Alta, 2 = Media, 3 = Baja (ver Models.Prioridad) o Nothing.</summary>
        Public Property Prioridad As Integer?

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

        ''' <summary>
        ''' Etiquetas (#tags) de la tarjeta, por nombre. Al guardar (TarjetasDatos.Crear/Actualizar) solo se usa
        ''' el Nombre y la lista sustituye a las que tuviera.
        ''' </summary>
        Public Property Etiquetas As New List(Of Etiqueta)
    End Class

End Namespace
