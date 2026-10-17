Namespace Models

    ''' <summary>
    ''' Prioridad de una tarjeta. Se guarda como número (1 = Alta, 2 = Media, 3 = Baja) para que ordenar
    ''' por prioridad sea ordenar por el valor; NULL = sin prioridad.
    ''' </summary>
    Public NotInheritable Class Prioridad
        Public Const Alta As Integer = 1
        Public Const Media As Integer = 2
        Public Const Baja As Integer = 3

        Private Sub New()
        End Sub

        Public Shared ReadOnly Property Todas As Integer() = {Alta, Media, Baja}

        Public Shared Function EsValida(valor As Integer) As Boolean
            Return valor >= Alta AndAlso valor <= Baja
        End Function

        ''' <summary>"Alta", "Media", "Baja" o Nothing.</summary>
        Public Shared Function Nombre(valor As Integer?) As String
            If Not valor.HasValue Then Return Nothing
            Select Case valor.Value
                Case Alta : Return "Alta"
                Case Media : Return "Media"
                Case Baja : Return "Baja"
                Case Else : Return Nothing
            End Select
        End Function

        ''' <summary>Clase CSS del color (rojo, amarillo, azul).</summary>
        Public Shared Function ClaseCss(valor As Integer?) As String
            If Not valor.HasValue Then Return ""
            Select Case valor.Value
                Case Alta : Return "prioridad-alta"
                Case Media : Return "prioridad-media"
                Case Baja : Return "prioridad-baja"
                Case Else : Return ""
            End Select
        End Function
    End Class

End Namespace
