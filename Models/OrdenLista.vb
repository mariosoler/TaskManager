Namespace Models

    ''' <summary>
    ''' Orden de las tarjetas de una lista que elige cada usuario para sí (PreferenciasLista.Orden). Es solo una
    ''' vista: no cambia la Posicion guardada (el orden manual, que ven los demás). Las claves son una lista blanca:
    ''' una clave desconocida o NULL = orden manual.
    ''' </summary>
    Public NotInheritable Class OrdenLista

        Public Const LongitudMaximaClave As Integer = 20

        ''' <summary>Clave guardada en PreferenciasLista.Orden ("" = manual).</summary>
        Public ReadOnly Property Clave As String
        ''' <summary>Texto del menú: "Fecha de creación (más recientes primero)".</summary>
        Public ReadOnly Property Texto As String
        ''' <summary>Texto corto para la cabecera de la lista: "Creación ↓".</summary>
        Public ReadOnly Property Corto As String

        Private ReadOnly _fecha As Func(Of Tarjeta, DateTime)
        Private ReadOnly _descendente As Boolean

        Private Sub New(clave As String, texto As String, corto As String, fecha As Func(Of Tarjeta, DateTime), descendente As Boolean)
            Me.Clave = clave
            Me.Texto = texto
            Me.Corto = corto
            _fecha = fecha
            _descendente = descendente
        End Sub

        Public ReadOnly Property EsManual As Boolean
            Get
                Return _fecha Is Nothing
            End Get
        End Property

        Public Shared ReadOnly Manual As New OrdenLista("", "Orden manual (arrastrando)", "", Nothing, False)

        ''' <summary>Opciones del menú «Ordenar tarjetas por», empezando por el orden manual.</summary>
        Public Shared ReadOnly Opciones As IReadOnlyList(Of OrdenLista) = New List(Of OrdenLista) From {
            Manual,
            New OrdenLista("creacion-desc", "Fecha de creación (más recientes primero)", "Creación ↓", Function(t) t.FechaCreacion, True),
            New OrdenLista("creacion-asc", "Fecha de creación (más antiguas primero)", "Creación ↑", Function(t) t.FechaCreacion, False),
            New OrdenLista("modificacion-desc", "Fecha de modificación (más recientes primero)", "Modificación ↓", Function(t) t.FechaModificacion, True),
            New OrdenLista("modificacion-asc", "Fecha de modificación (más antiguas primero)", "Modificación ↑", Function(t) t.FechaModificacion, False),
            New OrdenLista("entrada-desc", "Entrada en la lista (más recientes primero)", "Entrada ↓", Function(t) t.FechaEnLista, True),
            New OrdenLista("entrada-asc", "Entrada en la lista (más antiguas primero)", "Entrada ↑", Function(t) t.FechaEnLista, False)
        }.AsReadOnly()

        ''' <summary>Orden de la clave guardada; manual si es NULL o desconocida.</summary>
        Public Shared Function Buscar(clave As String) As OrdenLista
            If String.IsNullOrEmpty(clave) Then Return Manual
            Return If(Opciones.FirstOrDefault(Function(o) String.Equals(o.Clave, clave, StringComparison.Ordinal)), Manual)
        End Function

        Public Shared Function EsClaveValida(clave As String) As Boolean
            Return clave IsNot Nothing AndAlso Opciones.Any(Function(o) String.Equals(o.Clave, clave, StringComparison.Ordinal))
        End Function

        ''' <summary>Tarjetas en este orden (a igual fecha, por su posición manual).</summary>
        Public Function Aplicar(tarjetas As List(Of Tarjeta)) As List(Of Tarjeta)
            If EsManual Then Return tarjetas
            Dim ordenadas = If(_descendente, tarjetas.OrderByDescending(_fecha), tarjetas.OrderBy(_fecha))
            Return ordenadas.ThenBy(Function(t) t.Posicion).ThenBy(Function(t) t.Id).ToList()
        End Function

    End Class

End Namespace
