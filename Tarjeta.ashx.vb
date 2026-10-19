Imports System.Globalization
Imports System.Web.Script.Serialization
Imports TaskManager.Datos
Imports TaskManager.Models
Imports TaskManager.Seguridad
Imports TaskManager.Utilidades

''' <summary>
''' Guardado sin recargar del detalle de una tarjeta (JSON), usado por Scripts/detalle-tarjeta.js.
'''   POST tablero, tarjeta, titulo, caso, descripcion, prioridad, fechaLimite, asignado, etiquetas, codigo
'''        -> guarda los campos con TarjetasDatos.Actualizar (historial incluido)
'''   POST accion=subtarea, tablero, tarjeta, subtarea, completada (1/0)
'''        -> marca o desmarca la subtarea con SubtareasDatos.CambiarEstado
'''   POST accion=subtarea-texto, tablero, tarjeta, subtarea, texto
'''        -> cambia el texto de una subtarea no enlazada (SubtareasDatos.CambiarTexto, con historial); devuelve el texto guardado
'''   POST accion=subtareas-orden, tablero, tarjeta, orden (Ids separados por comas)
'''        -> pone las subtareas en ese orden (SubtareasDatos.Reordenar; no va al historial)
'''   POST accion=etiqueta, tablero, tarjeta (0 al crear una tarjeta), nombre, color
'''        -> crea en el tablero la etiqueta con el color elegido (EtiquetasDatos.Crear) y la devuelve
'''           ({ etiqueta: { n, c, f, t, u } }); color = clave de ColorEtiqueta (sólido o degradado); no modifica la tarjeta: se asigna al guardarla.
''' Las demás devuelven el texto de última modificación de la tarjeta.
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

        If String.Equals(valores("accion"), "etiqueta", StringComparison.Ordinal) Then
            Dim nombre = If(valores("nombre"), "").Trim().TrimStart("#"c)
            Dim color = valores("color")
            If Not Etiqueta.EsNombreValido(nombre) Then
                Terminar(respuesta, 400, "Nombre de etiqueta no válido: solo letras, números, _ y -, hasta " &
                         Etiqueta.LongitudMaxima & " caracteres.")
                Return
            End If
            If Not Etiqueta.EsColorDePaleta(color) Then
                Terminar(respuesta, 400, "Elige uno de los colores propuestos.")
                Return
            End If
            ' Crear usa el TableroId autorizado: la etiqueta solo existe en este tablero.
            Dim creada = EtiquetasDatos.Crear(tableroId, nombre, color)
            respuesta.Write(New JavaScriptSerializer().Serialize(New With {
                .ok = True,
                .etiqueta = New With {.n = creada.Nombre, .c = creada.ColorSeguro, .f = creada.FondoCss, .t = creada.Tinta, .u = creada.Usos}
            }))
            Return
        End If

        If String.Equals(valores("accion"), "subtarea", StringComparison.Ordinal) Then
            Dim subtareaId As Integer
            If Not Integer.TryParse(valores("subtarea"), subtareaId) Then
                Terminar(respuesta, 400, "Petición incorrecta.")
                Return
            End If
            ' CambiarEstado incluye el TableroId en el WHERE y solo registra historial si el estado cambia.
            SubtareasDatos.CambiarEstado(subtareaId, tableroId, valores("completada") = "1", usuarioId)
            Responder(respuesta, tarjetaId, tableroId, Nothing)
            Return
        End If

        If String.Equals(valores("accion"), "subtarea-texto", StringComparison.Ordinal) Then
            Dim subtareaId As Integer
            Dim texto = If(valores("texto"), "").Trim()
            If Not Integer.TryParse(valores("subtarea"), subtareaId) OrElse texto.Length = 0 Then
                Terminar(respuesta, 400, "Escribe el texto de la subtarea.")
                Return
            End If
            If texto.Length > 300 Then texto = texto.Substring(0, 300)
            ' CambiarTexto incluye el TableroId en el WHERE, no toca las enlazadas y solo registra si el texto cambia.
            SubtareasDatos.CambiarTexto(subtareaId, tableroId, texto, usuarioId)
            Responder(respuesta, tarjetaId, tableroId, Nothing, texto)
            Return
        End If

        If String.Equals(valores("accion"), "subtareas-orden", StringComparison.Ordinal) Then
            Dim orden = SubtareasDatos.LeerOrden(valores("orden"))
            If orden Is Nothing Then
                Terminar(respuesta, 400, "Petición incorrecta.")
                Return
            End If
            ' Reordenar comprueba que la tarjeta es del tablero; Ids de otras tarjetas no tienen efecto.
            SubtareasDatos.Reordenar(tarjetaId, tableroId, orden)
            Responder(respuesta, tarjetaId, tableroId, Nothing)
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
        Responder(respuesta, tarjetaId, tableroId, EdicionTarjeta.AvisoEtiquetas(ignoradas))
    End Sub

    Private Shared Sub Responder(respuesta As HttpResponse, tarjetaId As Integer, tableroId As Integer, aviso As String,
                                 Optional texto As String = Nothing)
        Dim tarjeta = TarjetasDatos.Obtener(tarjetaId, tableroId)
        If tarjeta Is Nothing Then
            Terminar(respuesta, 404, "La tarjeta ya no existe.")
            Return
        End If
        respuesta.Write(New JavaScriptSerializer().Serialize(New With {
            .ok = True,
            .aviso = aviso,
            .texto = texto,
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
