Namespace Models

    ''' <summary>
    ''' Columnas opcionales de la tabla de resultados del buscador (el título se muestra siempre). Lista blanca:
    ''' las claves elegidas (casillas «Columnas», búsquedas guardadas, cookie) se validan siempre contra ella.
    ''' </summary>
    Public NotInheritable Class ColumnaBusqueda

        Public ReadOnly Property Clave As String
        Public ReadOnly Property Nombre As String
        Public ReadOnly Property PorDefecto As Boolean

        Private Sub New(clave As String, nombre As String, porDefecto As Boolean)
            Me.Clave = clave
            Me.Nombre = nombre
            Me.PorDefecto = porDefecto
        End Sub

        ''' <summary>En el orden de la tabla.</summary>
        Public Shared ReadOnly Todas As IReadOnlyList(Of ColumnaBusqueda) = New List(Of ColumnaBusqueda) From {
            New ColumnaBusqueda("id", "ID", False),
            New ColumnaBusqueda("caso", "Caso", True),
            New ColumnaBusqueda("etiquetas", "Etiquetas", True),
            New ColumnaBusqueda("prioridad", "Prioridad", True),
            New ColumnaBusqueda("tablero", "Tablero · lista", True),
            New ColumnaBusqueda("asignado", "Asignada a", True),
            New ColumnaBusqueda("limite", "Fecha límite", True),
            New ColumnaBusqueda("subtareas", "Subtareas", True),
            New ColumnaBusqueda("adjuntos", "Adjuntos", False),
            New ColumnaBusqueda("creada", "Creada", False),
            New ColumnaBusqueda("modificada", "Modificada", True)
        }

        Public Shared ReadOnly Property ClavesPorDefecto As List(Of String)
            Get
                Return Todas.Where(Function(c) c.PorDefecto).Select(Function(c) c.Clave).ToList()
            End Get
        End Property

        ''' <summary>
        ''' Deja solo las claves de la lista blanca, sin repetir y en el orden de la tabla. Nothing si no se indica
        ''' ninguna lista (= usar las predeterminadas); una lista vacía es válida (solo el título).
        ''' </summary>
        Public Shared Function Validar(claves As IEnumerable(Of String)) As List(Of String)
            If claves Is Nothing Then Return Nothing
            Dim elegidas = New HashSet(Of String)(claves, StringComparer.Ordinal)
            Return Todas.Where(Function(c) elegidas.Contains(c.Clave)).Select(Function(c) c.Clave).ToList()
        End Function

        ''' <summary>"caso.prioridad" → claves válidas (separadas por punto). Nothing si el texto es Nothing.</summary>
        Public Shared Function DesdeTexto(texto As String) As List(Of String)
            If texto Is Nothing Then Return Nothing
            Return Validar(texto.Split({"."c}, StringSplitOptions.RemoveEmptyEntries))
        End Function

        ''' <summary>Claves separadas por punto; "-" si no hay ninguna (para distinguirlo de «sin indicar»).</summary>
        Public Shared Function ATexto(claves As IEnumerable(Of String)) As String
            Dim texto = String.Join(".", claves)
            Return If(texto.Length = 0, "-", texto)
        End Function

    End Class

End Namespace
