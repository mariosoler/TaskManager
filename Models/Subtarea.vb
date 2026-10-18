Namespace Models

    ''' <summary>Subtarea con casilla de una tarjeta. Serializable para guardarla en ViewState mientras se crea la tarjeta.</summary>
    <Serializable>
    Public Class Subtarea
        Public Property Id As Integer
        Public Property TarjetaId As Integer
        Public Property Texto As String
        Public Property Completada As Boolean
        Public Property Posicion As Integer

        ''' <summary>Tarjeta del mismo tablero a la que enlaza (Nothing = subtarea normal).</summary>
        Public Property TarjetaEnlazadaId As Integer?

        ''' <summary>Título actual de la tarjeta enlazada; Nothing si ya no existe en el tablero (se muestra Texto).</summary>
        Public Property TituloEnlazada As String
    End Class

End Namespace
