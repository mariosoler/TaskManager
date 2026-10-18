Namespace Models

    Public Class Lista
        Public Property Id As Integer
        Public Property TableroId As Integer
        Public Property Titulo As String
        Public Property Posicion As Integer
        Public Property Tarjetas As New List(Of Tarjeta)

        ''' <summary>Preferencias del usuario que mira el tablero (PreferenciasLista): oculta y orden de sus tarjetas.</summary>
        Public Property Oculta As Boolean
        Public Property Orden As OrdenLista = OrdenLista.Manual
    End Class

End Namespace
