Imports System.Globalization

Namespace Models

    ''' <summary>
    ''' Color de una etiqueta: sólido ("#1f6fc5") o degradado ("grad:oceano", dos colores a 135°). Es una lista blanca:
    ''' Etiquetas.Color guarda la <see cref="Clave"/> y solo se aceptan las de <see cref="Todos"/>. Cada color lleva la
    ''' tinta (color del texto) con contraste AA (4,5:1) frente a todo su fondo (en los degradados, frente a los dos
    ''' extremos); si se añade uno, comprobarlo así.
    ''' </summary>
    Public NotInheritable Class ColorEtiqueta

        Public Const PrefijoDegradado As String = "grad:"
        Public Const LongitudMaximaClave As Integer = 30

        ''' <summary>Valor guardado en Etiquetas.Color: "#rrggbb" o "grad:clave".</summary>
        Public ReadOnly Property Clave As String
        Public ReadOnly Property Nombre As String
        ''' <summary>Color del fondo (en un degradado, el primero): sirve de muestra donde solo cabe un color.</summary>
        Public ReadOnly Property Desde As String
        ''' <summary>Segundo color del degradado; Nothing si es sólido.</summary>
        Public ReadOnly Property Hasta As String
        Public ReadOnly Property Tinta As String

        Private Sub New(clave As String, nombre As String, desde As String, hasta As String)
            Me.Clave = clave
            Me.Nombre = nombre
            Me.Desde = desde
            Me.Hasta = hasta
            Tinta = If(hasta Is Nothing, TintaPara(desde), TintaPara(desde, hasta))
        End Sub

        Public ReadOnly Property EsDegradado As Boolean
            Get
                Return Hasta IsNot Nothing
            End Get
        End Property

        ''' <summary>Valor CSS del fondo (generado aquí con colores de la lista blanca, nunca con texto del usuario).</summary>
        Public ReadOnly Property FondoCss As String
            Get
                Return If(EsDegradado, "linear-gradient(135deg, " & Desde & ", " & Hasta & ")", Desde)
            End Get
        End Property

        Private Shared Function S(hex As String, nombre As String) As ColorEtiqueta
            Return New ColorEtiqueta(hex, nombre, hex, Nothing)
        End Function

        Private Shared Function G(clave As String, nombre As String, desde As String, hasta As String) As ColorEtiqueta
            Return New ColorEtiqueta(PrefijoDegradado & clave, nombre, desde, hasta)
        End Function

        ''' <summary>
        ''' Colores sólidos, en el orden en que se asignan automáticamente a las etiquetas nuevas de un tablero
        ''' (las 11 primeras son las de siempre, para no cambiar el orden en los tableros que ya existían).
        ''' </summary>
        Public Shared ReadOnly Solidos As IReadOnlyList(Of ColorEtiqueta) = New List(Of ColorEtiqueta) From {
            S("#1f6fc5", "Azul"), S("#e8710a", "Naranja"), S("#1f845a", "Verde"), S("#7c4dcc", "Violeta"),
            S("#c2185b", "Frambuesa"), S("#f2b705", "Amarillo"), S("#007a80", "Turquesa"), S("#455a64", "Gris pizarra"),
            S("#5c6bc0", "Índigo"), S("#8d6e63", "Marrón"), S("#c4380f", "Teja"),
            S("#c62828", "Rojo"), S("#880e4f", "Granate"), S("#6a1b9a", "Púrpura"), S("#0d47a1", "Azul marino"),
            S("#4fc3f7", "Celeste"), S("#00838f", "Cian oscuro"), S("#2e7d32", "Verde bosque"), S("#9ccc65", "Lima"),
            S("#fff176", "Limón"), S("#ffb74d", "Melocotón"), S("#ff8a65", "Coral"), S("#f48fb1", "Rosa"),
            S("#b39ddb", "Lavanda"), S("#80cbc4", "Menta"), S("#cfd8dc", "Gris claro"), S("#37474f", "Grafito"),
            S("#212121", "Negro")
        }.AsReadOnly()

        ''' <summary>Degradados (solo se eligen a mano al crear la etiqueta; nunca se asignan automáticamente).</summary>
        Public Shared ReadOnly Degradados As IReadOnlyList(Of ColorEtiqueta) = New List(Of ColorEtiqueta) From {
            G("oceano", "Océano", "#0d47a1", "#00838f"), G("bosque", "Bosque", "#1b5e20", "#00695c"),
            G("uva", "Uva", "#4a148c", "#ad1457"), G("noche", "Noche", "#1a237e", "#6a1b9a"),
            G("fuego", "Fuego", "#b71c1c", "#bf360c"), G("evergreen", "Evergreen", "#004746", "#00796b"),
            G("pizarra", "Pizarra", "#263238", "#546e7a"), G("atardecer", "Atardecer", "#ad1457", "#bf360c"),
            G("aurora", "Aurora", "#ffcc80", "#f8bbd0"), G("limonada", "Limonada", "#fff59d", "#c5e1a5"),
            G("cielo", "Cielo", "#b3e5fc", "#d1c4e9"), G("brisa", "Brisa", "#b2dfdb", "#dcedc8"),
            G("arena", "Arena", "#ffe0b2", "#ffccbc"), G("algodon", "Algodón", "#f8bbd0", "#e1bee7")
        }.AsReadOnly()

        ''' <summary>Todos los colores que se pueden elegir: primero los sólidos y después los degradados.</summary>
        Public Shared ReadOnly Todos As IReadOnlyList(Of ColorEtiqueta) = Solidos.Concat(Degradados).ToList().AsReadOnly()

        ''' <summary>Color de la lista blanca con esa clave (sin distinguir mayúsculas), o Nothing.</summary>
        Public Shared Function Buscar(clave As String) As ColorEtiqueta
            If String.IsNullOrEmpty(clave) Then Return Nothing
            Return Todos.FirstOrDefault(Function(c) String.Equals(c.Clave, clave, StringComparison.OrdinalIgnoreCase))
        End Function

        ''' <summary>Color sólido cualquiera (ya validado como #rrggbb), para etiquetas antiguas con otro color.</summary>
        Friend Shared Function DeHex(hex As String) As ColorEtiqueta
            Dim valor = hex.ToLowerInvariant()
            Return New ColorEtiqueta(valor, valor, valor, Nothing)
        End Function

        ''' <summary>Blanco si contrasta al menos 4,5:1 con todos los colores del fondo; si no, la tinta oscura.</summary>
        Public Shared Function TintaPara(ParamArray fondos As String()) As String
            Return If(fondos.All(Function(f) Contraste(f, "#ffffff") >= 4.5), "#ffffff", "#212121")
        End Function

        Private Shared Function Contraste(a As String, b As String) As Double
            Dim la = Luminancia(a), lb = Luminancia(b)
            Return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05)
        End Function

        Private Shared Function Luminancia(hex As String) As Double
            Dim canal = Function(i As Integer)
                            Dim v = Integer.Parse(hex.Substring(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0
                            Return If(v <= 0.03928, v / 12.92, Math.Pow((v + 0.055) / 1.055, 2.4))
                        End Function
            Return 0.2126 * canal(1) + 0.7152 * canal(3) + 0.0722 * canal(5)
        End Function

    End Class

End Namespace
