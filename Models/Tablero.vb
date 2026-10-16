Namespace Models

    Public Class Tablero
        Public Const RolPropietario As String = "Propietario"
        Public Const RolAdmin As String = "Admin"
        Public Const RolMiembro As String = "Miembro"

        Public Property Id As Integer
        Public Property Nombre As String
        Public Property Descripcion As String
        Public Property PropietarioId As Integer
        Public Property FechaCreacion As DateTime

        ''' <summary>Rol del usuario actual en el tablero (Propietario / Admin / Miembro).</summary>
        Public Property Rol As String
    End Class

End Namespace
