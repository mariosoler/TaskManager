Namespace Models

    Public Class Lista
        Public Property Id As Integer
        Public Property TableroId As Integer
        Public Property Titulo As String
        Public Property Posicion As Integer
        Public Property Tarjetas As New List(Of Tarjeta)
    End Class

End Namespace
