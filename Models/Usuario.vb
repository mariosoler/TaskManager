Namespace Models

    Public Class Usuario
        Public Property Id As Integer
        Public Property Nombre As String
        Public Property Email As String
        Public Property PasswordHash As String
        Public Property FechaCreacion As DateTime
        ''' <summary>Clave de <see cref="FormatoFecha"/>; Nothing = predeterminado.</summary>
        Public Property FormatoFecha As String
    End Class

End Namespace
