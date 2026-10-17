Namespace Models

    ''' <summary>
    ''' Formatos de fecha que puede elegir cada usuario en su perfil. Es una lista blanca: en
    ''' Usuarios.FormatoFecha se guarda solo la clave (NULL = predeterminado) y los patrones salen de aquí.
    ''' </summary>
    Public NotInheritable Class FormatoFecha

        ''' <summary>Clave guardada en la base de datos ("" = predeterminado, se guarda como NULL).</summary>
        Public ReadOnly Property Clave As String

        ''' <summary>Patrón de la fecha completa (historial, chat, buscador...).</summary>
        Public ReadOnly Property Larga As String

        ''' <summary>Patrón compacto, sin año (fechas límite de las tarjetas, ejes de las gráficas).</summary>
        Public ReadOnly Property Corta As String

        Private Sub New(clave As String, larga As String, corta As String)
            Me.Clave = clave
            Me.Larga = larga
            Me.Corta = corta
        End Sub

        ''' <summary>El de siempre: 17/10/2026, y en las tarjetas 17 oct.</summary>
        Public Shared ReadOnly Predeterminado As New FormatoFecha("", "dd/MM/yyyy", "d MMM")

        Public Shared ReadOnly Todos As IReadOnlyList(Of FormatoFecha) = New List(Of FormatoFecha) From {
            Predeterminado,
            New FormatoFecha("numerico", "dd/MM/yyyy", "dd/MM"),
            New FormatoFecha("abreviado", "d MMM yyyy", "d MMM"),
            New FormatoFecha("largo", "d 'de' MMMM 'de' yyyy", "d 'de' MMMM"),
            New FormatoFecha("iso", "yyyy-MM-dd", "MM-dd"),
            New FormatoFecha("eeuu", "MM/dd/yyyy", "MM/dd")
        }

        ''' <summary>Formato de la clave; Nothing si no está en la lista (vacía o Nothing = predeterminado).</summary>
        Public Shared Function Buscar(clave As String) As FormatoFecha
            If String.IsNullOrEmpty(clave) Then Return Predeterminado
            Return Todos.FirstOrDefault(Function(f) f.Clave = clave)
        End Function

        ''' <summary>Formato de la clave, o el predeterminado si no es válida.</summary>
        Public Shared Function Obtener(clave As String) As FormatoFecha
            Return If(Buscar(clave), Predeterminado)
        End Function

    End Class

End Namespace
