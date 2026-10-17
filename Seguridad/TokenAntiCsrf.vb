Imports System.Globalization
Imports System.Security.Cryptography
Imports System.Text

Namespace Seguridad

    ''' <summary>
    ''' Token contra CSRF para las peticiones AJAX (Chat.ashx). Va ligado al usuario y cifrado con la
    ''' machineKey del servidor: otra web no puede obtenerlo ni fabricarlo, y el de un usuario no sirve a otro.
    ''' </summary>
    Public NotInheritable Class TokenAntiCsrf

        Private Const Proposito As String = "TaskManager.Chat.v1"

        Private Sub New()
        End Sub

        Public Shared Function Generar(usuarioId As Integer) As String
            Dim datos = Encoding.UTF8.GetBytes(usuarioId.ToString(CultureInfo.InvariantCulture))
            Return HttpServerUtility.UrlTokenEncode(MachineKey.Protect(datos, Proposito))
        End Function

        Public Shared Function Validar(token As String, usuarioId As Integer) As Boolean
            If String.IsNullOrEmpty(token) Then Return False
            Try
                Dim cifrado = HttpServerUtility.UrlTokenDecode(token)
                If cifrado Is Nothing Then Return False
                Dim datos = MachineKey.Unprotect(cifrado, Proposito)
                Return datos IsNot Nothing AndAlso
                       Encoding.UTF8.GetString(datos) = usuarioId.ToString(CultureInfo.InvariantCulture)
            Catch ex As CryptographicException
                Return False
            Catch ex As FormatException
                Return False
            End Try
        End Function

    End Class

End Namespace
