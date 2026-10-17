Imports System.Globalization
Imports TaskManager.Models

Namespace Utilidades

    ''' <summary>
    ''' Convierte los valores del detalle de una tarjeta (texto tal cual llega del formulario) en una
    ''' <see cref="Tarjeta"/> lista para guardar. La usan el botón Guardar de Tablero.aspx y el
    ''' guardado automático de Tarjeta.ashx, para que ambos validen exactamente igual.
    ''' </summary>
    Public NotInheritable Class EdicionTarjeta

        Public Const LongitudMaximaCodigo As Integer = 200000
        Private Shared ReadOnly SoloDigitos As New Regex("^[0-9]+$")

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Devuelve la tarjeta, o Nothing si no se puede guardar (el motivo va en <paramref name="errorTexto"/>).
        ''' Las etiquetas no válidas se descartan y se devuelven en <paramref name="ignoradas"/>.
        ''' </summary>
        Public Shared Function Leer(titulo As String, caso As String, fechaLimite As String, asignado As String,
                                    prioridad As String, descripcion As String, codigo As String, etiquetas As String,
                                    ByRef errorTexto As String, ByRef ignoradas As List(Of String)) As Tarjeta
            errorTexto = Nothing
            ignoradas = New List(Of String)

            Dim datos As New Tarjeta With {.Titulo = Recortar(titulo, 200)}
            If datos.Titulo.Length = 0 Then
                errorTexto = "El título es obligatorio."
                Return Nothing
            End If

            ' [0-9] y no \d: \d acepta dígitos de otros alfabetos que la restricción CHECK rechaza.
            Dim textoCaso = Recortar(caso, 20)
            If textoCaso.Length > 0 AndAlso Not SoloDigitos.IsMatch(textoCaso) Then
                errorTexto = "El caso solo admite números."
                Return Nothing
            End If
            datos.Caso = If(textoCaso.Length = 0, Nothing, textoCaso)

            Dim fecha As DateTime
            If DateTime.TryParseExact(fechaLimite, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, fecha) Then
                datos.FechaLimite = fecha
            End If

            ' El asignado se vuelve a comprobar al guardar (solo usuarios con acceso al tablero).
            Dim usuario As Integer
            If Integer.TryParse(asignado, NumberStyles.None, CultureInfo.InvariantCulture, usuario) Then datos.AsignadoA = usuario

            Dim valorPrioridad As Integer
            If Integer.TryParse(prioridad, NumberStyles.None, CultureInfo.InvariantCulture, valorPrioridad) AndAlso
               Models.Prioridad.EsValida(valorPrioridad) Then
                datos.Prioridad = valorPrioridad
            End If

            datos.Descripcion = Recortar(descripcion, 20000).Replace(vbCrLf, vbLf)
            ' El código no se recorta: los espacios iniciales (sangría) son significativos.
            Dim textoCodigo = If(codigo, "").Replace(vbCrLf, vbLf)
            If textoCodigo.Length > LongitudMaximaCodigo Then textoCodigo = textoCodigo.Substring(0, LongitudMaximaCodigo)
            datos.Codigo = If(textoCodigo.Trim().Length = 0, Nothing, textoCodigo)

            datos.Etiquetas = Etiqueta.Analizar(etiquetas, ignoradas).Select(Function(n) New Etiqueta With {.Nombre = n}).ToList()
            Return datos
        End Function

        ''' <summary>Aviso para las etiquetas descartadas (Nothing si no hay).</summary>
        Public Shared Function AvisoEtiquetas(ignoradas As List(Of String)) As String
            If ignoradas Is Nothing OrElse ignoradas.Count = 0 Then Return Nothing
            Return "No se han guardado estas etiquetas (solo letras, números, _ y -, hasta " &
                   Etiqueta.LongitudMaxima & " caracteres y " & Etiqueta.MaximoPorTarjeta & " por tarjeta): " &
                   String.Join(" ", ignoradas)
        End Function

        Private Shared Function Recortar(valor As String, longitudMaxima As Integer) As String
            Dim limpio = If(valor, "").Trim()
            Return If(limpio.Length > longitudMaxima, limpio.Substring(0, longitudMaxima), limpio)
        End Function

    End Class

End Namespace
