Imports System.Globalization
Imports TaskManager.Datos
Imports TaskManager.Utilidades

''' <summary>
''' Imagen de fondo de un tablero: GET ?id=N. Solo para propietario o miembros del tablero;
''' el archivo se busca por el nombre guardado en la base de datos, nunca por uno de la URL.
''' </summary>
Public Class FondoHandler
    Implements IHttpHandler

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return True
        End Get
    End Property

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        Dim respuesta = context.Response
        If context.User Is Nothing OrElse Not context.User.Identity.IsAuthenticated Then
            respuesta.StatusCode = 401
            Return
        End If
        Dim usuarioId = Integer.Parse(context.User.Identity.Name, CultureInfo.InvariantCulture)

        Dim tableroId As Integer
        Dim tablero = If(Integer.TryParse(context.Request.QueryString("id"), tableroId),
                         TablerosDatos.Obtener(tableroId, usuarioId), Nothing)
        Dim ruta = If(tablero Is Nothing, Nothing, FondosTablero.RutaArchivo(tablero.Fondo))
        If ruta Is Nothing Then
            respuesta.StatusCode = 404
            Return
        End If

        respuesta.ContentType = FondosTablero.TipoMime(ruta)
        ' Privada (requiere sesión); la URL lleva el nombre del archivo, así que un cambio de fondo no usa la caché.
        respuesta.Cache.SetCacheability(HttpCacheability.Private)
        respuesta.Cache.SetMaxAge(TimeSpan.FromDays(7))
        respuesta.TransmitFile(ruta)
    End Sub

End Class
