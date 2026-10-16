Namespace Models

    ''' <summary>Subtarea con casilla de una tarjeta. Serializable para guardarla en ViewState mientras se crea la tarjeta.</summary>
    <Serializable>
    Public Class Subtarea
        Public Property Id As Integer
        Public Property TarjetaId As Integer
        Public Property Texto As String
        Public Property Completada As Boolean
        Public Property Posicion As Integer
    End Class

End Namespace
