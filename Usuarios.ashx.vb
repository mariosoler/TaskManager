Imports System.Globalization
Imports System.Web.Script.Serialization
Imports TaskManager.Datos
Imports TaskManager.Models
Imports TaskManager.Utilidades

''' <summary>
''' Sugerencias de usuarios para «Añadir miembro» (JSON), usado por Scripts/miembros.js.
'''   GET ?tablero=N&amp;q=texto  -> como mucho <see cref="MaxSugerencias"/> usuarios {nombre, email, iniciales}
''' Solo lectura. Solo responde a quien puede gestionar los miembros del tablero (propietario o Admin), que es
''' quien puede añadirlos; exige al menos <see cref="MinCaracteres"/> caracteres.
''' </summary>
Public Class UsuariosHandler
    Implements IHttpHandler

    Private Const MaxSugerencias As Integer = 4
    Private Const MinCaracteres As Integer = 2
    Private Const MaxCaracteres As Integer = 100

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return True
        End Get
    End Property

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        Dim peticion = context.Request
        Dim respuesta = context.Response
        respuesta.ContentType = "application/json"
        respuesta.Cache.SetCacheability(HttpCacheability.NoCache)
        respuesta.Cache.SetNoStore()

        If context.User Is Nothing OrElse Not context.User.Identity.IsAuthenticated Then
            Terminar(respuesta, 401, "Tu sesión ha caducado. Recarga la página.")
            Return
        End If
        Dim usuarioId = Integer.Parse(context.User.Identity.Name, CultureInfo.InvariantCulture)

        If Not String.Equals(peticion.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) Then
            Terminar(respuesta, 405, "Método no permitido.")
            Return
        End If

        ' Parámetros solo de la URL (nunca Request(...), que incluye cookies).
        Dim tableroId As Integer
        If Not Integer.TryParse(peticion.QueryString("tablero"), tableroId) Then
            Terminar(respuesta, 400, "Petición incorrecta.")
            Return
        End If
        Dim tablero = TablerosDatos.Obtener(tableroId, usuarioId)
        If tablero Is Nothing OrElse (tablero.Rol <> Tablero.RolPropietario AndAlso tablero.Rol <> Tablero.RolAdmin) Then
            Terminar(respuesta, 403, "No puedes gestionar los miembros de este tablero.")
            Return
        End If

        Dim texto = If(peticion.Unvalidated.QueryString("q"), "").Trim()
        If texto.Length > MaxCaracteres Then texto = texto.Substring(0, MaxCaracteres)
        Dim usuarios = If(texto.Length < MinCaracteres, New List(Of Miembro),
                          UsuariosDatos.SugerirParaTablero(tableroId, texto, MaxSugerencias))

        respuesta.Write(New JavaScriptSerializer().Serialize(New With {
            .usuarios = usuarios.Select(Function(u) New With {
                .nombre = u.Nombre, .email = u.Email, .iniciales = Formato.Iniciales(u.Nombre)})
        }))
    End Sub

    Private Shared Sub Terminar(respuesta As HttpResponse, estado As Integer, mensaje As String)
        respuesta.StatusCode = estado
        respuesta.TrySkipIisCustomErrors = True
        respuesta.Write(New JavaScriptSerializer().Serialize(New With {.error = mensaje}))
    End Sub

End Class
