Imports System.Globalization
Imports System.IO
Imports System.Web.Script.Serialization
Imports TaskManager.Datos
Imports TaskManager.Models
Imports TaskManager.Seguridad
Imports TaskManager.Utilidades

''' <summary>
''' Adjuntos de las tarjetas, usado por Scripts/adjuntos.js:
'''   GET  ?id=N                                       -> el archivo (solo a propietario o miembros de su tablero)
'''   POST accion=subir, tablero, tarjeta, archivo     -> guarda el archivo (multipart) y devuelve los adjuntos
'''   POST accion=eliminar, tablero, tarjeta, adjunto  -> lo elimina y devuelve los adjuntos de la tarjeta
''' Mismo patrón que Tarjeta.ashx: el usuario debe pertenecer al tablero, el POST exige el token anti-CSRF
''' (cabecera X-Chat-Token) y que el origen sea esta misma web. El archivo se busca siempre por el nombre guardado
''' en la base de datos. Solo las imágenes comprobadas (JPG, PNG, GIF, WebP) se muestran en el navegador; cualquier
''' otro archivo se envía como descarga (application/octet-stream, nosniff y sandbox), para que no se ejecute aquí.
''' </summary>
Public Class AdjuntosHandler
    Implements IHttpHandler

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return True
        End Get
    End Property

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        Dim peticion = context.Request
        Dim respuesta = context.Response

        If context.User Is Nothing OrElse Not context.User.Identity.IsAuthenticated Then
            Terminar(respuesta, 401, "Tu sesión ha caducado. Recarga la página.")
            Return
        End If
        Dim usuarioId = Integer.Parse(context.User.Identity.Name, CultureInfo.InvariantCulture)

        If String.Equals(peticion.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) Then
            Servir(peticion, respuesta, usuarioId)
            Return
        End If
        If Not String.Equals(peticion.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) Then
            Terminar(respuesta, 405, "Método no permitido.")
            Return
        End If
        If Not OrigenPropio(peticion) OrElse Not TokenAntiCsrf.Validar(peticion.Headers("X-Chat-Token"), usuarioId) Then
            Terminar(respuesta, 403, "Petición no válida. Recarga la página.")
            Return
        End If

        ' Solo del cuerpo (nunca Request(...), que incluye cookies).
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

        Select Case valores("accion")
            Case "subir"
                Subir(peticion, respuesta, tableroId, tarjetaId, usuarioId)
            Case "eliminar"
                Dim adjuntoId As Integer
                If Not Integer.TryParse(valores("adjunto"), adjuntoId) Then
                    Terminar(respuesta, 400, "Petición incorrecta.")
                    Return
                End If
                ' Eliminar incluye el TableroId en el WHERE: un adjunto de otro tablero no se toca.
                Dim archivo = AdjuntosDatos.Eliminar(adjuntoId, tarjetaId, tableroId, usuarioId)
                If archivo IsNot Nothing Then AdjuntosTarjeta.EliminarArchivos({archivo})
                Responder(respuesta, tarjetaId, tableroId)
            Case Else
                Terminar(respuesta, 400, "Petición incorrecta.")
        End Select
    End Sub

    Private Shared Sub Subir(peticion As HttpRequest, respuesta As HttpResponse, tableroId As Integer, tarjetaId As Integer,
                             usuarioId As Integer)
        Dim subido = If(peticion.Files.Count > 0, peticion.Files(0), Nothing)
        If subido Is Nothing OrElse subido.ContentLength = 0 Then
            Terminar(respuesta, 400, "No se ha recibido ningún archivo (o está vacío).")
            Return
        End If
        If subido.ContentLength > AdjuntosTarjeta.TamanoMaximo Then
            Terminar(respuesta, 400, "El archivo ocupa más de " & (AdjuntosTarjeta.TamanoMaximo \ (1024 * 1024)) & " MB.")
            Return
        End If

        Dim contenido As Byte()
        Using ms As New MemoryStream(subido.ContentLength)
            subido.InputStream.CopyTo(ms)
            contenido = ms.ToArray()
        End Using

        Dim esImagen As Boolean
        Dim archivo = AdjuntosTarjeta.Guardar(tarjetaId, contenido, esImagen)
        If archivo Is Nothing Then
            Terminar(respuesta, 400, "No se ha podido guardar el archivo.")
            Return
        End If
        Dim adjunto As New AdjuntoTarjeta With {
            .Archivo = archivo, .Nombre = AdjuntosTarjeta.LimpiarNombre(subido.FileName),
            .EsImagen = esImagen, .Tamano = contenido.Length
        }
        ' Crear comprueba que la tarjeta es del tablero y el máximo de adjuntos; si no se añade, se borra el archivo.
        If Not AdjuntosDatos.Crear(tarjetaId, tableroId, adjunto, usuarioId, AdjuntosTarjeta.MaximoPorTarjeta) Then
            AdjuntosTarjeta.EliminarArchivos({archivo})
            Terminar(respuesta, 400, "No se ha podido añadir el archivo (como mucho " & AdjuntosTarjeta.MaximoPorTarjeta & " por tarjeta).")
            Return
        End If
        Responder(respuesta, tarjetaId, tableroId)
    End Sub

    Private Shared Sub Servir(peticion As HttpRequest, respuesta As HttpResponse, usuarioId As Integer)
        Dim adjuntoId As Integer
        Dim adjunto = If(Integer.TryParse(peticion.QueryString("id"), adjuntoId), AdjuntosDatos.ObtenerParaUsuario(adjuntoId, usuarioId), Nothing)
        Dim ruta = If(adjunto Is Nothing, Nothing, AdjuntosTarjeta.RutaArchivo(adjunto.Archivo))
        If ruta Is Nothing Then
            respuesta.StatusCode = 404
            Return
        End If

        respuesta.Headers("X-Content-Type-Options") = "nosniff"
        If adjunto.EsImagen AndAlso Not ruta.EndsWith(".bin", StringComparison.Ordinal) Then
            respuesta.ContentType = AdjuntosTarjeta.TipoMimeImagen(ruta)
            respuesta.Headers("Content-Disposition") = AdjuntosTarjeta.ContentDisposition("inline", adjunto.Nombre)
        Else
            ' Cualquier otro tipo: siempre descarga, nunca se interpreta en esta web.
            respuesta.ContentType = "application/octet-stream"
            respuesta.Headers("Content-Disposition") = AdjuntosTarjeta.ContentDisposition("attachment", adjunto.Nombre)
            respuesta.Headers("Content-Security-Policy") = "sandbox"
        End If
        ' Privada (requiere sesión). Un adjunto no cambia: se puede guardar en caché.
        respuesta.Cache.SetCacheability(HttpCacheability.Private)
        respuesta.Cache.SetMaxAge(TimeSpan.FromDays(30))
        respuesta.TransmitFile(ruta)
    End Sub

    ''' <summary>Adjuntos de la tarjeta (JSON para adjuntos.js; ver TableroPagina.AdjuntosJson).</summary>
    Private Shared Sub Responder(respuesta As HttpResponse, tarjetaId As Integer, tableroId As Integer)
        respuesta.ContentType = "application/json"
        respuesta.Cache.SetCacheability(HttpCacheability.NoCache)
        respuesta.Cache.SetNoStore()
        respuesta.Write(New JavaScriptSerializer().Serialize(New With {
            .ok = True,
            .adjuntos = DatosParaCliente(AdjuntosDatos.Listar(tarjetaId, tableroId))
        }))
    End Sub

    ''' <summary>
    ''' i = Id, u = URL, n = nombre original, t = tamaño (bytes), img = imagen (miniatura), a = quién lo subió,
    ''' f = cuándo (en el formato del usuario). Los textos se insertan en el cliente con textContent.
    ''' </summary>
    Public Shared Function DatosParaCliente(adjuntos As IEnumerable(Of AdjuntoTarjeta)) As Object
        Return adjuntos.Select(Function(a) New With {
            .i = a.Id, .u = AdjuntosTarjeta.Url(a.Id), .n = a.Nombre, .t = a.Tamano, .img = a.EsImagen,
            .a = a.NombreSubidoPor, .f = Formato.FechaLocal(a.FechaSubida)
        }).ToList()
    End Function

    ''' <summary>Si el navegador envía Origin, debe ser esta misma web.</summary>
    Private Shared Function OrigenPropio(peticion As HttpRequest) As Boolean
        Dim origen = peticion.Headers("Origin")
        If String.IsNullOrEmpty(origen) Then Return True
        Return String.Equals(origen.TrimEnd("/"c), peticion.Url.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)
    End Function

    Private Shared Sub Terminar(respuesta As HttpResponse, estado As Integer, mensaje As String)
        respuesta.ContentType = "application/json"
        respuesta.StatusCode = estado
        respuesta.TrySkipIisCustomErrors = True
        respuesta.Write(New JavaScriptSerializer().Serialize(New With {.error = mensaje}))
    End Sub

End Class
