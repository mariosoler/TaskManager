Namespace Models

    ''' <summary>Búsqueda guardada del buscador (tabla BusquedasGuardadas).</summary>
    Public Class BusquedaGuardada
        Public Property Id As Integer
        Public Property UsuarioId As Integer
        Public Property NombreUsuario As String
        Public Property Nombre As String

        ''' <summary>Filtro serializado con FiltroBusqueda.ACriterios.</summary>
        Public Property Criterios As String

        ''' <summary>Visible también para los compañeros del autor (usuarios con algún tablero en común).</summary>
        Public Property Compartida As Boolean

        ''' <summary>Fecha en UTC.</summary>
        Public Property FechaCreacion As DateTime

        ''' <summary>True si la creó el usuario que la consulta (solo él puede borrarla).</summary>
        Public Property EsPropia As Boolean
    End Class

End Namespace
