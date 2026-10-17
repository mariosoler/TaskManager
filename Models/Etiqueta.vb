Imports System.Globalization

Namespace Models

    ''' <summary>
    ''' Etiqueta (#tag) de un tablero (tabla Etiquetas). El nombre se guarda sin "#" y es único por tablero
    ''' sin distinguir mayúsculas. El color se asigna automáticamente de <see cref="Paleta"/>.
    ''' </summary>
    Public Class Etiqueta

        Public Const LongitudMaxima As Integer = 50
        Public Const MaximoPorTarjeta As Integer = 10

        ''' <summary>
        ''' Colores en el orden en que se asignan a las etiquetas nuevas de un tablero (la primera, el primero...).
        ''' Todos validados con contraste AA (4,5:1) frente a su tinta (ver <see cref="TintaPara"/>).
        ''' </summary>
        Public Shared ReadOnly Paleta As IReadOnlyList(Of String) = New List(Of String) From {
            "#1f6fc5", "#e8710a", "#1f845a", "#7c4dcc", "#c2185b", "#f2b705",
            "#007a80", "#455a64", "#5c6bc0", "#8d6e63", "#c4380f"
        }.AsReadOnly()

        ' Letras y dígitos (de cualquier alfabeto), "_" y "-". Sin espacios: el espacio separa etiquetas.
        Private Shared ReadOnly NombreValido As New Regex("^[\p{L}\p{N}_\-]{1," & LongitudMaxima & "}$")
        Private Shared ReadOnly ColorValido As New Regex("^#[0-9a-fA-F]{6}$")

        Public Property Id As Integer
        Public Property Nombre As String
        Public Property Color As String

        ''' <summary>Número de tarjetas que la usan (solo en EtiquetasDatos.ListarDeTablero).</summary>
        Public Property Usos As Integer

        ''' <summary>Color de fondo seguro para CSS (si el guardado no es válido, el primero de la paleta).</summary>
        Public ReadOnly Property ColorSeguro As String
            Get
                Return If(Color IsNot Nothing AndAlso ColorValido.IsMatch(Color), Color.ToLowerInvariant(), Paleta(0))
            End Get
        End Property

        ''' <summary>Color del texto sobre el fondo de la etiqueta.</summary>
        Public ReadOnly Property Tinta As String
            Get
                Return TintaPara(ColorSeguro)
            End Get
        End Property

        ''' <summary>Atributo style de la etiqueta de color (valores ya validados: ColorSeguro y Tinta).</summary>
        Public ReadOnly Property EstiloCss As String
            Get
                Return "background-color: " & ColorSeguro & "; color: " & Tinta
            End Get
        End Property

        ''' <summary>Color de la paleta para la etiqueta número <paramref name="indice"/> (0, 1, 2...) de un tablero.</summary>
        Public Shared Function ColorPorIndice(indice As Integer) As String
            Return Paleta(Math.Abs(indice) Mod Paleta.Count)
        End Function

        ''' <summary>Blanco si contrasta al menos 4,5:1 con el fondo; si no, la tinta oscura de la aplicación.</summary>
        Public Shared Function TintaPara(fondo As String) As String
            Return If(Contraste(fondo, "#ffffff") >= 4.5, "#ffffff", "#212121")
        End Function

        Public Shared Function EsNombreValido(nombre As String) As Boolean
            Return nombre IsNot Nothing AndAlso NombreValido.IsMatch(nombre)
        End Function

        ''' <summary>
        ''' Extrae las etiquetas de un texto como "#PLANIFICADOR_CABINAS #urgente" (el "#" es opcional; se separan
        ''' por espacios o comas). Sin repetidas (sin distinguir mayúsculas) y como mucho <see cref="MaximoPorTarjeta"/>.
        ''' En <paramref name="ignoradas"/> devuelve las que no son válidas o sobran.
        ''' </summary>
        Public Shared Function Analizar(texto As String, ByRef ignoradas As List(Of String)) As List(Of String)
            Dim nombres As New List(Of String)
            ignoradas = New List(Of String)
            For Each trozo In If(texto, "").Split({" "c, ","c, ";"c, ControlChars.Tab, ControlChars.Cr, ControlChars.Lf},
                                                 StringSplitOptions.RemoveEmptyEntries)
                Dim nombre = trozo.TrimStart("#"c)
                If nombre.Length = 0 Then Continue For
                If Not EsNombreValido(nombre) OrElse nombres.Count >= MaximoPorTarjeta Then
                    ignoradas.Add(trozo)
                ElseIf Not nombres.Contains(nombre, StringComparer.CurrentCultureIgnoreCase) Then
                    nombres.Add(nombre)
                End If
            Next
            Return nombres
        End Function

        ''' <summary>Texto para el cuadro de etiquetas: "#A #B " (con espacio final para seguir escribiendo).</summary>
        Public Shared Function ATexto(etiquetas As IEnumerable(Of Etiqueta)) As String
            Dim texto = String.Join(" ", etiquetas.Select(Function(e) "#" & e.Nombre))
            Return If(texto.Length = 0, "", texto & " ")
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
