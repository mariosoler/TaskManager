Imports System.Globalization
Imports System.Web.Script.Serialization
Imports TaskManager.Datos
Imports TaskManager.Models
Imports TaskManager.Seguridad
Imports TaskManager.Utilidades

''' <summary>
''' Guardado automático del detalle de una tarjeta (JSON), usado por Scripts/detalle-tarjeta.js.
'''   POST tablero, tarjeta, titulo, caso, descripcion, prioridad, fechaLimite, asignado, etiquetas, codigo
''' Guarda con TarjetasDatos.Actualizar (historial incluido) y devuelve el texto de última modificación.
''' Mismo patrón que Chat.ashx: el usuario debe pertenecer al tablero, el POST exige el token anti-CSRF
''' (cabecera X-Chat-Token) y que el origen sea esta misma web.
''' </summary>
Public Class TarjetaHandler
    Implements IHttpHandler

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

        If Not String.Equals(peticion.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) Then
            Terminar(respuesta, 405, "Método no permitido.")
            Return
        End If
        If Not OrigenPropio(peticion) OrElse Not TokenAntiCsrf.Validar(peticion.Headers("X-Chat-Token"), usuarioId) Then
            Terminar(respuesta, 403, "Petición no válida. Recarga la página.")
            Return
        End If

        ' Solo del cuerpo (nunca Request(...), que incluye cookies). Unvalidated: los textos admiten "<"
        ' (todo se codifica al mostrarlo).
        Dim valores = peticion.Unvalidated.Form
        Dim tableroId, tarjetaId As Integer
        If Not Integer.TryParse(valores("tablero"), tableroId) OrElse Not Integer.TryParse(valores("tarjeta"), tarjetaId) Then
            Terminar(respuesta, 400, "Petición incorrecta.")
            Return
        End If

        If TablerosDatos.Obtener(tableroId, usuarioId) Is Nothing Then
            Terminar(respuesta, 403, "No tienes acceso a este tablero.")
            Return
        End If

        Dim errorTexto As String = Nothing
        Dim ignoradas As List(Of String) = Nothing
        Dim datos = EdicionTarjeta.Leer(valores("titulo"), valores("caso"), valores("fechaLimite"), valores("asignado"),
                                        valores("prioridad"), valores("descripcion"), valores("codigo"), valores("etiquetas"),
                                        errorTexto, ignoradas)
        If datos Is Nothing Then
            Terminar(respuesta, 400, errorTexto)
            Return
        End If

        ' Actualizar incluye el TableroId en el WHERE: una tarjeta de otro tablero no se modifica.
        TarjetasDatos.Actualizar(tarjetaId, tableroId, datos, usuarioId)
        Dim tarjeta = TarjetasDatos.Obtener(tarjetaId, tableroId)
        If tarjeta Is Nothing Then
            Terminar(respuesta, 404, "La tarjeta ya no existe.")
            Return
        End If

        respuesta.Write(New JavaScriptSerializer().Serialize(New With {
            .ok = True,
            .aviso = EdicionTarjeta.AvisoEtiquetas(ignoradas),
            .modificacion = Formato.TextoModificacion(tarjeta.FechaModificacion, tarjeta.NombreModificador)
        }))
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
