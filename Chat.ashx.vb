Imports System.Globalization
Imports System.Web.Script.Serialization
Imports TaskManager.Datos
Imports TaskManager.Models
Imports TaskManager.Seguridad
Imports TaskManager.Utilidades

''' <summary>
''' Chat de una tarjeta (JSON), usado por Scripts/chat.js.
'''   GET  ?tablero=N&amp;tarjeta=M&amp;desde=K   -> mensajes con Id &gt; K
'''   POST tablero, tarjeta, desde, texto    -> publica el mensaje y devuelve los nuevos
''' Cada petición comprueba que el usuario pertenece al tablero; el POST exige además el token
''' anti-CSRF (cabecera X-Chat-Token) y que el origen sea esta misma web.
''' </summary>
Public Class ChatHandler
    Implements IHttpHandler

    Private Const MaxTexto As Integer = 2000

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

        Dim esPost = String.Equals(peticion.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
        If Not esPost AndAlso Not String.Equals(peticion.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) Then
            Terminar(respuesta, 405, "Método no permitido.")
            Return
        End If

        ' Los parámetros se leen solo de la URL (GET) o del cuerpo (POST), nunca de cookies.
        Dim valores = If(esPost, peticion.Unvalidated.Form, peticion.QueryString)
        Dim tableroId, tarjetaId, desde As Integer
        If Not Integer.TryParse(valores("tablero"), tableroId) OrElse Not Integer.TryParse(valores("tarjeta"), tarjetaId) Then
            Terminar(respuesta, 400, "Petición incorrecta.")
            Return
        End If
        Integer.TryParse(valores("desde"), desde)

        If TablerosDatos.Obtener(tableroId, usuarioId) Is Nothing Then
            Terminar(respuesta, 403, "No tienes acceso a este tablero.")
            Return
        End If

        If esPost Then
            If Not OrigenPropio(peticion) OrElse Not TokenAntiCsrf.Validar(peticion.Headers("X-Chat-Token"), usuarioId) Then
                Terminar(respuesta, 403, "Petición no válida. Recarga la página.")
                Return
            End If
            ' Unvalidated: el chat admite "<" (todo se codifica al mostrarlo).
            Dim texto = If(valores("texto"), "").Trim()
            If texto.Length = 0 Then
                Terminar(respuesta, 400, "El mensaje está vacío.")
                Return
            End If
            If texto.Length > MaxTexto Then texto = texto.Substring(0, MaxTexto)
            If Not ComentariosDatos.Crear(tarjetaId, tableroId, usuarioId, texto).HasValue Then
                Terminar(respuesta, 404, "La tarjeta ya no existe.")
                Return
            End If
        End If

        Dim mensajes = ComentariosDatos.Listar(tarjetaId, tableroId, desde).Select(
            Function(m) New With {
                .id = m.Id,
                .autor = m.NombreUsuario,
                .iniciales = Formato.Iniciales(m.NombreUsuario),
                .propio = m.UsuarioId = usuarioId,
                .texto = m.Texto,
                .fecha = Formato.FechaLocal(m.Fecha)
            })
        respuesta.Write(New JavaScriptSerializer().Serialize(New With {.mensajes = mensajes}))
    End Sub

    ''' <summary>Si el navegador envía Origin, debe ser esta misma web.</summary>
    Private Shared Function OrigenPropio(peticion As HttpRequest) As Boolean
        Dim origen = peticion.Headers("Origin")
        If String.IsNullOrEmpty(origen) Then Return True
        Return String.Equals(origen.TrimEnd("/"c), peticion.Url.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)
    End Function

    Private Shared Sub Terminar(respuesta As HttpResponse, estado As Integer, mensaje As String)
        respuesta.StatusCode = estado
        respuesta.TrySkipIisCustomErrors = True
        respuesta.Write(New JavaScriptSerializer().Serialize(New With {.error = mensaje}))
    End Sub

End Class
