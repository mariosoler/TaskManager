Namespace Models

    Public Class Comentario
        Public Property Id As Integer
        Public Property TarjetaId As Integer
        Public Property UsuarioId As Integer
        Public Property NombreUsuario As String
        Public Property Texto As String

        ''' <summary>Fecha en UTC.</summary>
        Public Property Fecha As DateTime
    End Class

End Namespace
