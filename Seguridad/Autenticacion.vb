Imports System.Globalization
Imports TaskManager.Models

Namespace Seguridad

    ''' <summary>
    ''' Sesión con Forms Authentication. El nombre del ticket es el Id del usuario
    ''' y UserData guarda su nombre para mostrarlo.
    ''' </summary>
    Public NotInheritable Class Autenticacion

        ' Hash de una contraseña aleatoria: se verifica contra él cuando el email no existe,
        ' para que la respuesta tarde lo mismo y no revele qué emails están registrados.
        Private Shared ReadOnly HashFicticio As New Lazy(Of String)(
            Function() PasswordHasher.Hash(Guid.NewGuid().ToString()))

        Private Sub New()
        End Sub

        Public Shared Function ValidarCredenciales(usuario As Usuario, password As String) As Boolean
            If usuario Is Nothing Then
                PasswordHasher.Verificar(password, HashFicticio.Value)
                Return False
            End If
            Return PasswordHasher.Verificar(password, usuario.PasswordHash)
        End Function

        Public Shared Sub IniciarSesion(usuario As Usuario, recordar As Boolean)
            Dim ahora = DateTime.Now
            Dim ticket As New FormsAuthenticationTicket(
                1,
                usuario.Id.ToString(CultureInfo.InvariantCulture),
                ahora,
                ahora.Add(FormsAuthentication.Timeout),
                recordar,
                usuario.Nombre,
                FormsAuthentication.FormsCookiePath)

            Dim cookie As New HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket)) With {
                .HttpOnly = True,
                .Secure = FormsAuthentication.RequireSSL,
                .Path = FormsAuthentication.FormsCookiePath
            }
            If recordar Then cookie.Expires = ticket.Expiration

            HttpContext.Current.Response.Cookies.Add(cookie)
        End Sub

        Public Shared Function NombreUsuarioActual() As String
            Dim identidad = TryCast(HttpContext.Current.User?.Identity, FormsIdentity)
            Return If(identidad?.Ticket.UserData, "")
        End Function

    End Class

End Namespace
