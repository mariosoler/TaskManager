Imports System.Globalization
Imports System.Text.RegularExpressions
Imports System.Web.Script.Serialization
Imports TaskManager.Datos
Imports TaskManager.Models
Imports TaskManager.Seguridad
Imports TaskManager.Utilidades

Public Class TableroPagina
    Inherits PaginaBase

    ' Tablero de la URL, cargado y autorizado en cada petición (también en los postbacks).
    Private _tablero As Tablero

    ' Detalle abierto al empezar la petición ("tarjeta/lista"); si sigue igual al final, no se anima de nuevo.
    Private _detalleAlCargar As String

    Private ReadOnly Property TableroId As Integer
        Get
            Return _tablero.Id
        End Get
    End Property

    Private Property TarjetaAbiertaId As Integer
        Get
            Return If(ViewState("TarjetaAbiertaId") Is Nothing, 0, CInt(ViewState("TarjetaAbiertaId")))
        End Get
        Set(value As Integer)
            ViewState("TarjetaAbiertaId") = value
        End Set
    End Property

    ' Lista en la que se está creando una tarjeta (modal en modo creación); 0 si no.
    Private Property ListaNuevaId As Integer
        Get
            Return If(ViewState("ListaNuevaId") Is Nothing, 0, CInt(ViewState("ListaNuevaId")))
        End Get
        Set(value As Integer)
            ViewState("ListaNuevaId") = value
        End Set
    End Property

    Private ReadOnly Property PuedeGestionarMiembros As Boolean
        Get
            Return _tablero.Rol = Tablero.RolPropietario OrElse _tablero.Rol = Tablero.RolAdmin
        End Get
    End Property

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim id As Integer
        If Integer.TryParse(Request.QueryString("id"), id) Then _tablero = TablerosDatos.Obtener(id, UsuarioId)
        If _tablero Is Nothing Then Response.Redirect("~/Tableros.aspx")
        If IsPostBack AndAlso pnlDetalle.Visible Then _detalleAlCargar = ClaveDetalle()

        ' Buscar en este tablero y Estadísticas están en el menú superior.
        CType(Master, SiteMaster).TableroActual = _tablero.Id

        If Not IsPostBack Then
            CargarTablero()
            ' Tablero.aspx?id=N&tarjeta=M abre directamente la tarjeta (enlaces del buscador).
            Dim tarjetaId As Integer
            If Integer.TryParse(Request.QueryString("tarjeta"), tarjetaId) Then AbrirTarjeta(tarjetaId)
        End If
    End Sub

    ' ----- Listas y tarjetas -----

    Protected Sub btnAgregarLista_Click(sender As Object, e As EventArgs) Handles btnAgregarLista.Click
        Dim titulo = Texto(txtNuevaLista.Text, 150)
        If titulo.Length > 0 Then ListasDatos.Crear(TableroId, titulo)
        txtNuevaLista.Text = ""
        CargarTablero()
    End Sub

    Protected Sub rptListas_ItemCommand(source As Object, e As RepeaterCommandEventArgs) Handles rptListas.ItemCommand
        Dim listaId As Integer
        If Not Integer.TryParse(CStr(e.CommandArgument), listaId) Then Return

        Select Case e.CommandName
            Case "AgregarTarjeta"
                AbrirNuevaTarjeta(listaId)
                Return
            Case "MoverIzquierda"
                ListasDatos.Mover(listaId, TableroId, -1)
            Case "MoverDerecha"
                ListasDatos.Mover(listaId, TableroId, 1)
            Case "EliminarLista"
                AdjuntosTarjeta.EliminarArchivos(ListasDatos.Eliminar(listaId, TableroId))
            Case "OcultarLista"
                ListasDatos.Ocultar(listaId, TableroId, UsuarioId, True)
        End Select
        CargarTablero()
    End Sub

    ''' <summary>«Listas ocultas» de la cabecera: volver a mostrar una lista (solo para este usuario).</summary>
    Protected Sub rptListasOcultas_ItemCommand(source As Object, e As RepeaterCommandEventArgs) Handles rptListasOcultas.ItemCommand
        Dim listaId As Integer
        If e.CommandName = "MostrarLista" AndAlso Integer.TryParse(CStr(e.CommandArgument), listaId) Then
            ListasDatos.Ocultar(listaId, TableroId, UsuarioId, False)
        End If
        CargarTablero()
    End Sub

    ''' <summary>Menú «⋯» de una lista: ordenar sus tarjetas (solo para este usuario). Argumento "listaId|clave".</summary>
    Protected Sub rptOrdenes_ItemCommand(source As Object, e As RepeaterCommandEventArgs)
        If e.CommandName <> "Ordenar" Then Return
        Dim partes = CStr(e.CommandArgument).Split("|"c)
        Dim listaId As Integer
        If partes.Length <> 2 OrElse Not Integer.TryParse(partes(0), NumberStyles.None, CultureInfo.InvariantCulture, listaId) Then Return
        Dim clave = partes(1)
        If clave.Length = 0 Then
            ListasDatos.CambiarOrden(listaId, TableroId, UsuarioId, Nothing)
        ElseIf OrdenLista.EsClaveValida(clave) Then
            ListasDatos.CambiarOrden(listaId, TableroId, UsuarioId, clave)
        End If
        CargarTablero()
    End Sub

    ''' <summary>Lista del elemento del repetidor de órdenes (anidado en el de listas).</summary>
    Private Shared Function ListaDeOrden(item As RepeaterItem) As Lista
        Return CType(CType(item.NamingContainer.NamingContainer, RepeaterItem).DataItem, Lista)
    End Function

    Protected Shared Function ArgumentoOrden(item As RepeaterItem) As String
        Return ListaDeOrden(item).Id.ToString(CultureInfo.InvariantCulture) & "|" & CType(item.DataItem, OrdenLista).Clave
    End Function

    Protected Shared Function EsOrdenActual(item As RepeaterItem) As Boolean
        Return ListaDeOrden(item).Orden Is item.DataItem
    End Function

    Protected Sub lnkMoverTarjeta_Click(sender As Object, e As EventArgs) Handles lnkMoverTarjeta.Click
        Dim tarjetaId, listaId, indice As Integer
        If Integer.TryParse(hfTarjetaId.Value, tarjetaId) AndAlso
           Integer.TryParse(hfListaDestino.Value, listaId) AndAlso
           Integer.TryParse(hfIndice.Value, indice) Then
            TarjetasDatos.Mover(tarjetaId, listaId, indice, TableroId, UsuarioId)
        End If
        CargarTablero()
    End Sub

    ' ----- Detalle de tarjeta -----

    Protected Sub lnkAbrirTarjeta_Click(sender As Object, e As EventArgs) Handles lnkAbrirTarjeta.Click
        Dim tarjetaId As Integer
        If Integer.TryParse(hfTarjetaId.Value, tarjetaId) Then AbrirTarjeta(tarjetaId)
    End Sub

    ' Guardar (editar) y «Crear tarea» (junto al título, al crear) hacen lo mismo.
    Protected Sub btnGuardarTarjeta_Click(sender As Object, e As EventArgs) Handles btnGuardarTarjeta.Click, btnCrearTarjeta.Click
        If Not IsValid Then Return
        ' Misma lectura y validación que el guardado automático (Tarjeta.ashx).
        Dim errorTexto As String = Nothing
        Dim ignoradas As List(Of String) = Nothing
        Dim datos = EdicionTarjeta.Leer(txtTituloTarjeta.Text, txtCaso.Text, txtFechaLimite.Text, ddlAsignado.SelectedValue,
                                        ddlPrioridad.SelectedValue, txtDescripcionTarjeta.Text, txtCodigo.Text, txtEtiquetas.Text,
                                        errorTexto, ignoradas)
        If datos Is Nothing Then Return

        Dim aviso = EdicionTarjeta.AvisoEtiquetas(ignoradas)
        If aviso IsNot Nothing Then MostrarMensaje(aviso, True)

        ' Desplegable «Lista» (encima del chat); Crear y CambiarLista comprueban que la lista es de este tablero.
        Dim listaElegida As Integer
        If Not Integer.TryParse(ddlLista.SelectedValue, NumberStyles.None, CultureInfo.InvariantCulture, listaElegida) Then listaElegida = 0

        If CreandoTarjeta Then
            Dim nuevaId = TarjetasDatos.Crear(If(listaElegida > 0, listaElegida, ListaNuevaId), TableroId, datos, SubtareasPendientes, UsuarioId)
            If nuevaId = 0 Then
                CerrarDetalle()
            Else
                ' El detalle sigue abierto con la tarjeta recién creada (ya en modo edición: guardado automático,
                ' chat y adjuntos), sin volver a animarse.
                SubtareasPendientes = Nothing
                AbrirTarjeta(nuevaId)
                _detalleAlCargar = ClaveDetalle()
            End If
        Else
            TarjetasDatos.Actualizar(TarjetaAbiertaId, TableroId, datos, UsuarioId)
            If listaElegida > 0 Then TarjetasDatos.CambiarLista(TarjetaAbiertaId, listaElegida, TableroId, UsuarioId)
            CerrarDetalle()
        End If
        CargarTablero()
    End Sub

    Protected Sub btnEliminarTarjeta_Click(sender As Object, e As EventArgs) Handles btnEliminarTarjeta.Click
        AdjuntosTarjeta.EliminarArchivos(TarjetasDatos.Eliminar(TarjetaAbiertaId, TableroId))
        CerrarDetalle()
        CargarTablero()
    End Sub

    Protected Sub btnCerrarDetalle_Click(sender As Object, e As EventArgs) Handles btnCerrarDetalle.Click
        CerrarDetalle()
        ' La tarjeta puede haberse guardado sola mientras estaba abierta (Tarjeta.ashx): se recarga el tablero.
        CargarTablero()
    End Sub

    ' Envío del chat sin JavaScript (o si falla el envío por AJAX de Scripts/chat.js).
    Protected Sub btnComentar_Click(sender As Object, e As EventArgs) Handles btnComentar.Click
        Dim mensaje = Texto(txtComentario.Text, 2000)
        If mensaje.Length > 0 Then
            ComentariosDatos.Crear(TarjetaAbiertaId, TableroId, UsuarioId, mensaje)
            txtComentario.Text = ""
            CargarComentarios()
            CargarTablero()
        End If
    End Sub

    ' ----- Etiquetas -----

    ''' <summary>
    ''' Etiquetas del tablero para las sugerencias de Scripts/etiquetas.js (n, c = color, f = fondo CSS, t = tinta, u = usos).
    ''' Solo las de este tablero: las etiquetas no se comparten entre tableros. Vacío si el detalle está cerrado.
    ''' </summary>
    Protected ReadOnly Property EtiquetasJson As String
        Get
            If Not pnlDetalle.Visible Then Return "[]"
            Dim etiquetas = EtiquetasDatos.ListarDeTablero(TableroId).Select(
                Function(e) New With {.n = e.Nombre, .c = e.ColorSeguro, .f = e.FondoCss, .t = e.Tinta, .u = e.Usos})
            Dim json = New JavaScriptSerializer().Serialize(etiquetas.ToList())
            Return json.Replace("<", "\u003c").Replace(">", "\u003e").Replace("&", "\u0026")
        End Get
    End Property

    ''' <summary>
    ''' Adjuntos de la tarjeta abierta para Scripts/adjuntos.js (ver AdjuntosHandler.DatosParaCliente). Vacío si el
    ''' detalle está cerrado o se está creando la tarjeta. Los signos menor, mayor y ampersand van escapados.
    ''' </summary>
    Protected ReadOnly Property AdjuntosJson As String
        Get
            If Not pnlDetalle.Visible OrElse CreandoTarjeta OrElse TarjetaAbiertaId <= 0 Then Return "[]"
            Dim json = New JavaScriptSerializer().Serialize(AdjuntosHandler.DatosParaCliente(AdjuntosDatos.Listar(TarjetaAbiertaId, TableroId)))
            Return json.Replace("<", "\u003c").Replace(">", "\u003e").Replace("&", "\u0026")
        End Get
    End Property

    ''' <summary>
    ''' Colores que se pueden elegir al crear una etiqueta (lista blanca ColorEtiqueta): k = clave que se envía,
    ''' c = color de muestra, f = fondo CSS, t = tinta, n = nombre, g = degradado. Primero van los sólidos.
    ''' </summary>
    Protected Shared ReadOnly Property PaletaJson As String
        Get
            Dim colores = ColorEtiqueta.Todos.Select(
                Function(c) New With {.k = c.Clave, .c = c.Desde, .f = c.FondoCss, .t = c.Tinta, .n = c.Nombre, .g = c.EsDegradado})
            Return New JavaScriptSerializer().Serialize(colores.ToList())
        End Get
    End Property

    ''' <summary>Etiquetas de la tarjeta separadas por espacios (data-etiquetas, para la búsqueda rápida).</summary>
    Protected Shared Function NombresEtiquetas(dato As Object) As String
        Return String.Join(" ", CType(dato, Tarjeta).Etiquetas.Select(Function(e) e.Nombre))
    End Function

    ''' <summary>Texto en el que busca la búsqueda rápida sin "#": título, descripción y persona asignada.</summary>
    Protected Shared Function TextoBusqueda(dato As Object) As String
        Dim tarjeta = CType(dato, Tarjeta)
        Return String.Join(" ", {tarjeta.Titulo, tarjeta.Descripcion, tarjeta.NombreAsignado}.Where(Function(t) Not String.IsNullOrEmpty(t)))
    End Function

    ' ----- Chat (configuración para Scripts/chat.js) -----

    Protected ReadOnly Property TokenChat As String
        Get
            Return TokenAntiCsrf.Generar(UsuarioId)
        End Get
    End Property

    Protected ReadOnly Property TableroIdTexto As String
        Get
            Return TableroId.ToString(CultureInfo.InvariantCulture)
        End Get
    End Property

    ''' <summary>Tarjeta cuyo chat está visible (0 si el detalle está cerrado o se está creando una tarjeta).</summary>
    Protected ReadOnly Property TarjetaChatTexto As String
        Get
            Dim visible = pnlDetalle.Visible AndAlso phChat.Visible
            Return If(visible, TarjetaAbiertaId, 0).ToString(CultureInfo.InvariantCulture)
        End Get
    End Property

    ''' <summary>Tarjeta abierta en modo edición (guardado automático); 0 si se está creando una o no hay detalle.</summary>
    Protected ReadOnly Property TarjetaEdicionTexto As String
        Get
            Return If(pnlDetalle.Visible AndAlso Not CreandoTarjeta, TarjetaAbiertaId, 0).ToString(CultureInfo.InvariantCulture)
        End Get
    End Property

    Protected Function ClaseMensaje(autorId As Object) As String
        Return If(TypeOf autorId Is Integer AndAlso CInt(autorId) = UsuarioId, "chat-mensaje propio", "chat-mensaje")
    End Function

    Private Sub AbrirTarjeta(tarjetaId As Integer)
        Dim tarjeta = TarjetasDatos.Obtener(tarjetaId, TableroId)
        If tarjeta Is Nothing Then
            CerrarDetalle()
            Return
        End If

        TarjetaAbiertaId = tarjeta.Id
        ListaNuevaId = 0
        MostrarDetalle(tarjeta, esNueva:=False)
        CargarComentarios()
        CargarActividad(tarjeta)
    End Sub

    Private Sub AbrirNuevaTarjeta(listaId As Integer)
        Dim nombreLista = ListasDatos.ObtenerTitulo(listaId, TableroId)
        If nombreLista Is Nothing Then
            CerrarDetalle()
            Return
        End If

        TarjetaAbiertaId = 0
        ListaNuevaId = listaId
        SubtareasPendientes = New List(Of Subtarea)
        MostrarDetalle(New Tarjeta With {.ListaId = listaId, .NombreLista = nombreLista, .Titulo = ""}, esNueva:=True)
    End Sub

    ''' <summary>Rellena y muestra el modal, en modo creación (esNueva) o edición.</summary>
    Private Sub MostrarDetalle(tarjeta As Tarjeta, esNueva As Boolean)
        litDetallePrefijo.Text = If(esNueva, "Nueva tarjeta en la lista", "En la lista")
        phDetalleId.Visible = Not esNueva
        ' Los adjuntos necesitan el Id de la tarjeta: al crearla solo se explica que se podrán añadir después.
        phAdjuntos.Visible = Not esNueva
        phAdjuntosNueva.Visible = esNueva
        litDetalleId.Text = tarjeta.Id.ToString(CultureInfo.InvariantCulture)
        ' Al crear, el botón es «Crear tarea», a la derecha del título; al editar, Guardar (abajo; el script lo oculta).
        btnCrearTarjeta.Visible = esNueva
        btnGuardarTarjeta.Visible = Not esNueva
        pnlDetalle.DefaultButton = If(esNueva, btnCrearTarjeta.ID, btnGuardarTarjeta.ID)
        btnEliminarTarjeta.Visible = Not esNueva
        phHistorial.Visible = Not esNueva
        phChat.Visible = Not esNueva
        phChatNuevo.Visible = esNueva

        litDetalleLista.Text = tarjeta.NombreLista
        litFechasTarjeta.Text = ""
        txtCaso.Text = If(tarjeta.Caso, "")
        txtTituloTarjeta.Text = tarjeta.Titulo
        txtDescripcionTarjeta.Text = If(tarjeta.Descripcion, "")
        ddlPrioridad.ClearSelection()
        Dim opcionPrioridad = ddlPrioridad.Items.FindByValue(
            If(tarjeta.Prioridad.HasValue, tarjeta.Prioridad.Value.ToString(CultureInfo.InvariantCulture), ""))
        If opcionPrioridad IsNot Nothing Then opcionPrioridad.Selected = True
        txtFechaLimite.Text = If(tarjeta.FechaLimite.HasValue,
                                 tarjeta.FechaLimite.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "")
        txtCodigo.Text = If(tarjeta.Codigo, "")
        txtEtiquetas.Text = Etiqueta.ATexto(tarjeta.Etiquetas)
        txtNuevaSubtarea.Text = ""
        txtComentario.Text = ""

        ddlAsignado.Items.Clear()
        ddlAsignado.Items.Add(New ListItem("Sin asignar", ""))
        For Each miembro In TablerosDatos.ListarMiembros(TableroId)
            ddlAsignado.Items.Add(New ListItem(miembro.Nombre, miembro.UsuarioId.ToString(CultureInfo.InvariantCulture)))
        Next
        If tarjeta.AsignadoA.HasValue Then
            Dim item = ddlAsignado.Items.FindByValue(tarjeta.AsignadoA.Value.ToString(CultureInfo.InvariantCulture))
            If item IsNot Nothing Then item.Selected = True
        End If

        ' Lista de la tarjeta (al crearla, aquella en la que se pulsó «+ Crear Tarea»).
        ddlLista.Items.Clear()
        For Each lista In ListasDatos.Listar(TableroId)
            ddlLista.Items.Add(New ListItem(lista.Titulo, lista.Id.ToString(CultureInfo.InvariantCulture)))
        Next
        Dim opcionLista = ddlLista.Items.FindByValue(tarjeta.ListaId.ToString(CultureInfo.InvariantCulture))
        If opcionLista IsNot Nothing Then opcionLista.Selected = True

        CargarSubtareas()
        pnlDetalle.Visible = True
        ' Al crear se empieza por el título; al abrir una existente, el título se edita al pulsarlo.
        If esNueva Then txtTituloTarjeta.Focus()
    End Sub

    Private Sub CargarComentarios()
        rptComentarios.DataSource = ComentariosDatos.Listar(TarjetaAbiertaId, TableroId)
        rptComentarios.DataBind()
    End Sub

    ' ----- Fechas e historial -----

    ''' <summary>Fechas de creación/modificación e historial de la tarjeta abierta.</summary>
    Private Sub CargarActividad(Optional tarjeta As Tarjeta = Nothing)
        If tarjeta Is Nothing Then tarjeta = TarjetasDatos.Obtener(TarjetaAbiertaId, TableroId)
        If tarjeta Is Nothing Then Return

        litFechasTarjeta.Text = String.Format(CultureInfo.CurrentCulture, " · Creada el {0} · {1}",
                                              Formato.FechaLocal(tarjeta.FechaCreacion),
                                              TextoModificacion(tarjeta.FechaModificacion, tarjeta.NombreModificador))

        Dim historial = HistorialDatos.Listar(TarjetaAbiertaId, TableroId)
        rptHistorial.DataSource = historial
        rptHistorial.DataBind()
        litNumCambios.Text = historial.Count.ToString(CultureInfo.InvariantCulture)
    End Sub

    ''' <summary>"Modificada el 17/10/2026 14:32 por Ana" (texto plano: codificar al mostrar).</summary>
    Protected Function TextoModificacion(fecha As Object, nombre As Object) As String
        Return Formato.TextoModificacion(fecha, nombre)
    End Function

    ''' <summary>
    ''' Frase HTML que describe un cambio del historial ("cambió el título de «a» a «b»").
    ''' Todos los valores procedentes de la base de datos se codifican aquí.
    ''' </summary>
    Protected Function DescribirCambio(cambio As HistorialTarjeta) As String
        Dim antes = cambio.ValorAnterior
        Dim despues = cambio.ValorNuevo

        Select Case cambio.Accion
            Case HistorialTarjeta.Creada
                Return "creó la tarjeta en " & Valor(despues)
            Case HistorialTarjeta.Movida
                Return "movió la tarjeta de " & Valor(antes) & " a " & Valor(despues)
            Case HistorialTarjeta.SubtareaAgregada
                Return "añadió la subtarea " & Valor(despues)
            Case HistorialTarjeta.SubtareaCompletada
                Return "completó la subtarea " & Valor(despues)
            Case HistorialTarjeta.SubtareaReabierta
                Return "desmarcó la subtarea " & Valor(despues)
            Case HistorialTarjeta.SubtareaEditada
                Return "cambió la subtarea " & Valor(antes) & " por " & Valor(despues)
            Case HistorialTarjeta.SubtareaEliminada
                Return "eliminó la subtarea " & Valor(despues)
            Case HistorialTarjeta.Comentada
                Return "comentó " & Valor(despues)
            Case HistorialTarjeta.AdjuntoAgregado
                Return "adjuntó " & Valor(despues)
            Case HistorialTarjeta.AdjuntoEliminado
                Return "eliminó el adjunto " & Valor(despues)
            Case HistorialTarjeta.FotoAgregada
                Return "añadió una foto"
            Case HistorialTarjeta.FotoEliminada
                Return "eliminó una foto"
            Case HistorialTarjeta.Editada
                Return DescribirEdicion(cambio.Campo, antes, despues)
            Case Else
                Return HttpUtility.HtmlEncode(cambio.Accion)
        End Select
    End Function

    Private Shared Function DescribirEdicion(campo As String, antes As String, despues As String) As String
        Select Case campo
            Case HistorialTarjeta.CampoTitulo
                Return "cambió el título de " & Valor(antes) & " a " & Valor(despues)
            Case HistorialTarjeta.CampoCaso
                Return Variante(antes, despues,
                                "puso el caso " & Valor("#" & despues),
                                "quitó el caso " & Valor("#" & antes),
                                "cambió el caso de " & Valor("#" & antes) & " a " & Valor("#" & despues))
            Case HistorialTarjeta.CampoFechaLimite
                Return Variante(antes, despues,
                                "puso la fecha límite el " & Valor(FechaLegible(despues)),
                                "quitó la fecha límite (era el " & Valor(FechaLegible(antes)) & ")",
                                "cambió la fecha límite del " & Valor(FechaLegible(antes)) & " al " & Valor(FechaLegible(despues)))
            Case HistorialTarjeta.CampoAsignado
                Return Variante(antes, despues,
                                "asignó la tarjeta a " & Valor(despues),
                                "quitó la asignación a " & Valor(antes),
                                "reasignó la tarjeta de " & Valor(antes) & " a " & Valor(despues))
            Case HistorialTarjeta.CampoDescripcion
                Return Variante(antes, despues, "añadió una descripción", "eliminó la descripción", "editó la descripción") &
                       Extracto(despues)
            Case HistorialTarjeta.CampoCodigo
                Return Variante(antes, despues, "añadió código", "eliminó el código", "editó el código") &
                       Extracto(despues)
            Case HistorialTarjeta.CampoPrioridad
                Return Variante(antes, despues,
                                "puso la prioridad " & Valor(despues),
                                "quitó la prioridad " & Valor(antes),
                                "cambió la prioridad de " & Valor(antes) & " a " & Valor(despues))
            Case HistorialTarjeta.CampoEtiquetas
                Return DescribirEtiquetas(antes, despues)
            Case Else
                Return "editó " & HttpUtility.HtmlEncode(campo)
        End Select
    End Function

    ''' <summary>"añadió la etiqueta #A y quitó #B" a partir de los valores "#A #B" del historial.</summary>
    Private Shared Function DescribirEtiquetas(antes As String, despues As String) As String
        Dim separar = Function(valor As String) If(valor, "").Split({" "c}, StringSplitOptions.RemoveEmptyEntries)
        Dim anteriores = separar(antes), nuevas = separar(despues)
        Dim anadidas = nuevas.Except(anteriores, StringComparer.CurrentCultureIgnoreCase).ToList()
        Dim quitadas = anteriores.Except(nuevas, StringComparer.CurrentCultureIgnoreCase).ToList()
        Dim lista = Function(etiquetas As List(Of String)) String.Join(", ", etiquetas.Select(Function(e) Valor(e)))

        Dim partes As New List(Of String)
        If anadidas.Count > 0 Then partes.Add(If(anadidas.Count = 1, "añadió la etiqueta ", "añadió las etiquetas ") & lista(anadidas))
        If quitadas.Count > 0 Then partes.Add(If(quitadas.Count = 1, "quitó la etiqueta ", "quitó las etiquetas ") & lista(quitadas))
        Return If(partes.Count = 0, "cambió las etiquetas", String.Join(" y ", partes))
    End Function

    ' Elige la frase según se haya puesto un valor, quitado o cambiado.
    Private Shared Function Variante(antes As String, despues As String, puesto As String, quitado As String, cambiado As String) As String
        If String.IsNullOrEmpty(antes) Then Return puesto
        If String.IsNullOrEmpty(despues) Then Return quitado
        Return cambiado
    End Function

    Private Shared Function Valor(texto As String) As String
        Return "<span class=""historial-valor"">«" & HttpUtility.HtmlEncode(If(texto, "")) & "»</span>"
    End Function

    Private Shared Function Extracto(texto As String) As String
        If String.IsNullOrEmpty(texto) Then Return ""
        Return "<span class=""historial-extracto"">" & HttpUtility.HtmlEncode(texto) & "</span>"
    End Function

    Private Shared Function FechaLegible(iso As String) As String
        Dim fecha As DateTime
        If DateTime.TryParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, fecha) Then
            Return Formato.FechaDia(fecha)
        End If
        Return iso
    End Function

    Private Sub CerrarDetalle()
        TarjetaAbiertaId = 0
        ListaNuevaId = 0
        SubtareasPendientes = Nothing
        pnlDetalle.Visible = False
    End Sub

    ' ----- Subtareas -----
    ' Al editar una tarjeta se guardan al momento; al crearla se acumulan en ViewState
    ' (SubtareasPendientes) y se insertan junto con la tarjeta. En ese modo el Id es el índice.

    Private Property SubtareasPendientes As List(Of Subtarea)
        Get
            Dim lista = TryCast(ViewState("SubtareasPendientes"), List(Of Subtarea))
            If lista Is Nothing Then
                lista = New List(Of Subtarea)
                ViewState("SubtareasPendientes") = lista
            End If
            Return lista
        End Get
        Set(value As List(Of Subtarea))
            ViewState("SubtareasPendientes") = value
        End Set
    End Property

    Private ReadOnly Property CreandoTarjeta As Boolean
        Get
            Return ListaNuevaId > 0
        End Get
    End Property

    Protected Sub btnAgregarSubtarea_Click(sender As Object, e As EventArgs) Handles btnAgregarSubtarea.Click
        ' Enlace a otra tarjeta (Scripts/subtareas-enlace.js): solo de este tablero y no la propia; el texto es su título.
        Dim enlazada As Tarjeta = Nothing
        Dim enlazadaId As Integer
        If Integer.TryParse(hfSubtareaEnlace.Value, NumberStyles.None, CultureInfo.InvariantCulture, enlazadaId) AndAlso
           enlazadaId <> TarjetaAbiertaId Then
            enlazada = TarjetasDatos.Obtener(enlazadaId, TableroId)
        End If
        hfSubtareaEnlace.Value = ""

        Dim textoSubtarea = Texto(If(enlazada IsNot Nothing, enlazada.Titulo, txtNuevaSubtarea.Text), 300)
        Dim idEnlace = If(enlazada IsNot Nothing, CType(enlazada.Id, Integer?), Nothing)
        If textoSubtarea.Length > 0 Then
            If CreandoTarjeta Then
                Dim pendientes = SubtareasPendientes
                pendientes.Add(New Subtarea With {.Texto = textoSubtarea, .TarjetaEnlazadaId = idEnlace})
                SubtareasPendientes = pendientes
            Else
                SubtareasDatos.Crear(TarjetaAbiertaId, TableroId, textoSubtarea, UsuarioId, idEnlace)
                CargarTablero()
                CargarActividad()
            End If
            txtNuevaSubtarea.Text = ""
        End If
        CargarSubtareas()
        txtNuevaSubtarea.Focus()
    End Sub

    Protected Sub rptSubtareas_ItemCommand(source As Object, e As RepeaterCommandEventArgs) Handles rptSubtareas.ItemCommand
        Dim id As Integer
        If e.CommandName <> "Eliminar" OrElse Not Integer.TryParse(CStr(e.CommandArgument), id) Then Return

        If CreandoTarjeta Then
            Dim pendientes = SubtareasPendientes
            If id >= 0 AndAlso id < pendientes.Count Then pendientes.RemoveAt(id)
            SubtareasPendientes = pendientes
        Else
            SubtareasDatos.Eliminar(id, TableroId, UsuarioId)
            CargarTablero()
            CargarActividad()
        End If
        CargarSubtareas()
    End Sub

    Protected Sub chkSubtarea_CheckedChanged(sender As Object, e As EventArgs)
        Dim chk = CType(sender, CheckBox)
        Dim hf = CType(chk.NamingContainer.FindControl("hfSubtareaId"), HiddenField)
        Dim id As Integer
        If Not Integer.TryParse(hf.Value, id) Then Return

        If CreandoTarjeta Then
            Dim pendientes = SubtareasPendientes
            If id >= 0 AndAlso id < pendientes.Count Then pendientes(id).Completada = chk.Checked
            SubtareasPendientes = pendientes
        Else
            SubtareasDatos.CambiarEstado(id, TableroId, chk.Checked, UsuarioId)
            CargarTablero()
            CargarActividad()
        End If
        CargarSubtareas()
    End Sub

    ''' <summary>
    ''' Editar el texto o reordenar las subtareas (Scripts/subtareas.js). Se usa al crear una tarjeta, con las
    ''' subtareas pendientes (el Id es su índice); al editar una existente el script usa Tarjeta.ashx, pero esto
    ''' también sirve. Valor de hfSubtareaAccion: "texto|índice o Id|nuevo texto" u "orden|2,0,1".
    ''' </summary>
    Protected Sub lnkSubtareaAccion_Click(sender As Object, e As EventArgs) Handles lnkSubtareaAccion.Click
        Dim partes = If(hfSubtareaAccion.Value, "").Split({"|"c}, 3)
        hfSubtareaAccion.Value = ""
        Dim id As Integer
        If partes.Length = 3 AndAlso partes(0) = "texto" AndAlso
           Integer.TryParse(partes(1), NumberStyles.None, CultureInfo.InvariantCulture, id) Then
            Dim nuevo = Texto(partes(2), 300)
            If nuevo.Length > 0 Then
                If CreandoTarjeta Then
                    Dim pendientes = SubtareasPendientes
                    ' Las enlazadas muestran el título de su tarjeta: no se editan.
                    If id < pendientes.Count AndAlso Not pendientes(id).TarjetaEnlazadaId.HasValue Then pendientes(id).Texto = nuevo
                    SubtareasPendientes = pendientes
                ElseIf SubtareasDatos.CambiarTexto(id, TableroId, nuevo, UsuarioId) Then
                    CargarTablero()
                    CargarActividad()
                End If
            End If
        ElseIf partes.Length = 2 AndAlso partes(0) = "orden" Then
            Dim orden = SubtareasDatos.LeerOrden(partes(1))
            If orden IsNot Nothing Then
                If CreandoTarjeta Then
                    ' Debe ser una permutación de los índices de las pendientes.
                    Dim pendientes = SubtareasPendientes
                    If orden.Count = pendientes.Count AndAlso orden.Distinct().Count() = orden.Count AndAlso
                       orden.All(Function(i) i < pendientes.Count) Then
                        SubtareasPendientes = orden.Select(Function(i) pendientes(i)).ToList()
                    End If
                Else
                    SubtareasDatos.Reordenar(TarjetaAbiertaId, TableroId, orden)
                End If
            End If
        End If
        CargarSubtareas()
    End Sub

    Private Sub CargarSubtareas()
        Dim subtareas As List(Of Subtarea)
        If CreandoTarjeta Then
            ' Las pendientes enlazadas muestran el título que tenía la tarjeta al enlazarla.
            subtareas = SubtareasPendientes.Select(
                Function(s, i) New Subtarea With {.Id = i, .Texto = s.Texto, .Completada = s.Completada,
                                                  .TarjetaEnlazadaId = s.TarjetaEnlazadaId,
                                                  .TituloEnlazada = If(s.TarjetaEnlazadaId.HasValue, s.Texto, Nothing)}).ToList()
        Else
            subtareas = SubtareasDatos.Listar(TarjetaAbiertaId, TableroId)
        End If

        rptSubtareas.DataSource = subtareas
        rptSubtareas.DataBind()
        Dim hechas = subtareas.Where(Function(s) s.Completada).Count()
        litProgresoSubtareas.Text = If(subtareas.Count = 0, "",
                                       String.Format(CultureInfo.InvariantCulture, "{0}/{1}", hechas, subtareas.Count))
    End Sub

    ''' <summary>
    ''' Texto de la subtarea ya codificado. Si enlaza a una tarjeta del tablero, su título actual subrayado con
    ''' enlace para abrirla (al crear una tarjeta, en otra pestaña para no perderla); si esa tarjeta ya no existe,
    ''' el título que tenía al enlazarla.
    ''' </summary>
    Protected Function HtmlSubtarea(dato As Object) As String
        Dim s = CType(dato, Subtarea)
        If Not s.TarjetaEnlazadaId.HasValue Then Return HttpUtility.HtmlEncode(s.Texto)
        If s.TituloEnlazada Is Nothing Then
            Return "<span class=""subtarea-enlace-roto"" title=""La tarjeta enlazada ya no existe"">" &
                   HttpUtility.HtmlEncode(s.Texto) & "</span>"
        End If
        Dim url = ResolveUrl("~/Tablero.aspx?id=" & TableroIdTexto & "&tarjeta=" &
                             s.TarjetaEnlazadaId.Value.ToString(CultureInfo.InvariantCulture))
        Return "<a class=""subtarea-enlace"" href=""" & HttpUtility.HtmlAttributeEncode(url) & """ title=""Abrir la tarjeta""" &
               If(CreandoTarjeta, " target=""_blank"" rel=""noopener""", "") & ">" &
               HttpUtility.HtmlEncode(s.TituloEnlazada) & "</a>"
    End Function

    ''' <summary>
    ''' Tarjetas del tablero que se pueden enlazar desde una subtarea (i = Id, t = título, c = caso, l = lista),
    ''' sin la que está abierta. Vacío si el detalle está cerrado. Los signos menor, mayor y ampersand van escapados.
    ''' </summary>
    Protected ReadOnly Property TarjetasEnlazablesJson As String
        Get
            If Not pnlDetalle.Visible Then Return "[]"
            Dim tarjetas = TarjetasEnlazables().
                Select(Function(t) New With {.i = t.Id, .t = t.Titulo, .c = If(t.Caso, ""), .l = t.NombreLista})
            Dim json = New JavaScriptSerializer().Serialize(tarjetas.ToList())
            Return json.Replace("<", "\u003c").Replace(">", "\u003e").Replace("&", "\u0026")
        End Get
    End Property

    ' Tarjetas del tablero (ListarParaEnlazar), leídas una vez por petición: las usan las subtareas y el chat.
    Private _tarjetasTablero As List(Of Tarjeta)

    ''' <summary>Tarjetas del tablero que se pueden enlazar (subtareas y chat): todas menos la abierta.</summary>
    Private Function TarjetasEnlazables() As IEnumerable(Of Tarjeta)
        If _tarjetasTablero Is Nothing Then _tarjetasTablero = TarjetasDatos.ListarParaEnlazar(TableroId)
        Return _tarjetasTablero.Where(Function(t) t.Id <> TarjetaAbiertaId)
    End Function

    ' Tarjeta enlazada en un mensaje del chat: [#123] (lo inserta el botón de enlace del chat, Scripts/chat-enlace.js).
    Private Shared ReadOnly EnlaceChat As New Regex("\[#([0-9]{1,9})\]", RegexOptions.CultureInvariant)

    ''' <summary>
    ''' Texto de un mensaje del chat ya codificado; cada [#N] de una tarjeta de este tablero se convierte en un enlace
    ''' con su ID y su título actual (lo mismo hace Scripts/chat.js con los mensajes nuevos). Los demás quedan como texto.
    ''' </summary>
    Protected Function HtmlMensaje(texto As Object) As String
        Dim codificado = HttpUtility.HtmlEncode(If(TryCast(texto, String), ""))
        If codificado.IndexOf("[#", StringComparison.Ordinal) < 0 Then Return codificado
        Dim titulos = TarjetasEnlazables().ToDictionary(Function(t) t.Id, Function(t) t.Titulo)
        Return EnlaceChat.Replace(codificado,
            Function(m)
                Dim id As Integer
                If Not Integer.TryParse(m.Groups(1).Value, NumberStyles.None, CultureInfo.InvariantCulture, id) OrElse
                   Not titulos.ContainsKey(id) Then Return m.Value
                Dim idTexto = id.ToString(CultureInfo.InvariantCulture)
                Dim url = ResolveUrl("~/Tablero.aspx?id=" & TableroIdTexto & "&tarjeta=" & idTexto)
                Return "<a class=""chat-enlace-tarjeta"" href=""" & HttpUtility.HtmlAttributeEncode(url) & """ title=""Abrir la tarjeta " &
                       idTexto & """><span class=""tarjeta-id"">" & idTexto & "</span>" & HttpUtility.HtmlEncode(titulos(id)) & "</a>"
            End Function)
    End Function

    ''' <summary>URL del tablero para los enlaces a tarjetas de Scripts/chat.js (se le añade "&amp;tarjeta=N").</summary>
    Protected ReadOnly Property UrlTableroTexto As String
        Get
            Return ResolveUrl("~/Tablero.aspx?id=" & TableroIdTexto)
        End Get
    End Property

    ' ----- Miembros -----

    Protected Sub btnAgregarMiembro_Click(sender As Object, e As EventArgs) Handles btnAgregarMiembro.Click
        If Not PuedeGestionarMiembros Then Return
        Dim email = NormalizarEmail(txtEmailMiembro.Text)
        If email.Length = 0 Then Return

        Select Case TablerosDatos.AgregarMiembro(TableroId, email)
            Case ResultadoMiembro.Agregado
                MostrarMensaje("Miembro añadido.", False)
                txtEmailMiembro.Text = ""
            Case ResultadoMiembro.YaEsMiembro
                MostrarMensaje("Ese usuario ya es miembro del tablero.", True)
            Case ResultadoMiembro.UsuarioNoExiste
                MostrarMensaje("No hay ningún usuario registrado con ese email.", True)
        End Select
        CargarTablero()
    End Sub

    Protected Sub rptMiembros_ItemCommand(source As Object, e As RepeaterCommandEventArgs) Handles rptMiembros.ItemCommand
        Dim miembroId As Integer
        If e.CommandName <> "Quitar" OrElse Not PuedeGestionarMiembros OrElse
           Not Integer.TryParse(CStr(e.CommandArgument), miembroId) Then Return

        TablerosDatos.QuitarMiembro(TableroId, miembroId, UsuarioId)
        If miembroId = UsuarioId Then Response.Redirect("~/Tableros.aspx")
        CargarTablero()
    End Sub

    Protected Function PuedeQuitarMiembro(rol As Object) As Boolean
        Return PuedeGestionarMiembros AndAlso CStr(rol) <> Tablero.RolPropietario
    End Function

    ' ----- Nombre y descripción del tablero (propietario o Admin) -----

    Protected Sub btnGuardarNombre_Click(sender As Object, e As EventArgs) Handles btnGuardarNombre.Click
        If Not PuedeGestionarMiembros OrElse Not IsValid Then Return
        Dim nombre = Texto(txtNombreTablero.Text, 150)
        If nombre.Length = 0 Then Return
        Dim descripcion = Texto(txtDescripcionTablero.Text, 500)

        If TablerosDatos.CambiarNombre(TableroId, nombre, descripcion, UsuarioId) Then
            _tablero.Nombre = nombre
            _tablero.Descripcion = If(descripcion.Length = 0, Nothing, descripcion)
            MostrarMensaje("Tablero actualizado.", False)
        End If
        CargarTablero()
    End Sub

    ' Nombre editado al pulsar el título (Scripts/tablero.js); la descripción no cambia.
    Protected Sub btnGuardarTitulo_Click(sender As Object, e As EventArgs) Handles btnGuardarTitulo.Click
        Dim nombre = Texto(txtTitulo.Text, 150)
        If PuedeGestionarMiembros AndAlso nombre.Length > 0 AndAlso nombre <> _tablero.Nombre AndAlso
           TablerosDatos.CambiarNombre(TableroId, nombre, _tablero.Descripcion, UsuarioId) Then
            _tablero.Nombre = nombre
        End If
        CargarTablero()
    End Sub

    ' ----- Fondo del tablero (propietario o Admin) -----

    Protected Sub rptFondos_ItemCommand(source As Object, e As RepeaterCommandEventArgs) Handles rptFondos.ItemCommand
        If e.CommandName = "Elegir" AndAlso PuedeGestionarMiembros Then
            ' Solo claves de la lista blanca de FondosTablero.
            Dim fondo = FondosTablero.BuscarPredefinido(FondosTablero.PrefijoPredefinido & CStr(e.CommandArgument))
            If fondo IsNot Nothing AndAlso fondo.Valor <> _tablero.Fondo Then AplicarFondo(fondo.Valor)
        End If
        CargarTablero()
    End Sub

    Protected Sub rptFondos_ItemDataBound(sender As Object, e As RepeaterItemEventArgs) Handles rptFondos.ItemDataBound
        Dim fondo = TryCast(e.Item.DataItem, FondoPredefinido)
        If fondo Is Nothing Then Return
        Dim enlace = CType(e.Item.FindControl("lnkFondo"), LinkButton)
        ' Variable CSS y no "background-image": ASP.NET envuelve ese valor en url(...) y estropea los degradados.
        enlace.Style("--fondo-imagen") = fondo.ImagenCss(miniatura:=True)
        Dim activo = fondo.Valor = _tablero.Fondo
        enlace.CssClass = If(activo, "fondo-opcion activo", "fondo-opcion")
        enlace.Attributes("aria-pressed") = If(activo, "true", "false")
    End Sub

    Protected Sub btnSubirFondo_Click(sender As Object, e As EventArgs) Handles btnSubirFondo.Click
        If Not PuedeGestionarMiembros Then Return
        If Not fuFondo.HasFile Then
            MostrarMensaje("Elige una imagen para el fondo.", True)
        ElseIf fuFondo.PostedFile.ContentLength > FondosTablero.TamanoMaximo Then
            MostrarMensaje("La imagen supera los 5 MB.", True)
        Else
            Dim nombre = FondosTablero.Guardar(TableroId, fuFondo.FileBytes)
            If nombre Is Nothing Then
                MostrarMensaje("El archivo no es una imagen JPG, PNG, GIF o WebP.", True)
            Else
                AplicarFondo(nombre)
            End If
        End If
        CargarTablero()
    End Sub

    Protected Sub lnkQuitarFondo_Click(sender As Object, e As EventArgs) Handles lnkQuitarFondo.Click
        If PuedeGestionarMiembros Then AplicarFondo(Nothing)
        CargarTablero()
    End Sub

    ''' <summary>Guarda el nuevo fondo (o ninguno) y borra el archivo anterior.</summary>
    Private Sub AplicarFondo(nombre As String)
        Dim anterior = _tablero.Fondo
        If TablerosDatos.CambiarFondo(TableroId, nombre, UsuarioId) Then
            _tablero.Fondo = nombre
            FondosTablero.Eliminar(anterior)
        Else
            FondosTablero.Eliminar(nombre)
        End If
    End Sub

    ' ----- Carga y formato -----

    Private Sub CargarTablero()
        litNombre.Text = _tablero.Nombre
        litDescripcion.Text = If(_tablero.Descripcion, "")

        ' Ocultar y ordenar son preferencias de cada usuario: las listas ocultas van al desplegable de la cabecera.
        Dim listas = ListasDatos.ListarConTarjetas(TableroId, UsuarioId)
        rptListas.DataSource = listas.Where(Function(l) Not l.Oculta).ToList()
        rptListas.DataBind()
        Dim ocultas = listas.Where(Function(l) l.Oculta).ToList()
        rptListasOcultas.DataSource = ocultas
        rptListasOcultas.DataBind()
        phListasOcultas.Visible = ocultas.Count > 0
        litListasOcultas.Text = ocultas.Count.ToString(CultureInfo.InvariantCulture)

        rptMiembros.DataSource = TablerosDatos.ListarMiembros(TableroId)
        rptMiembros.DataBind()
        pnlAgregarMiembro.Visible = PuedeGestionarMiembros
        phScriptMiembros.Visible = PuedeGestionarMiembros
        phFondo.Visible = PuedeGestionarMiembros
        If PuedeGestionarMiembros Then
            rptFondos.DataSource = FondosTablero.Predefinidos
            rptFondos.DataBind()
        End If
        phEditarTablero.Visible = PuedeGestionarMiembros
        txtNombreTablero.Text = _tablero.Nombre
        txtTitulo.Text = _tablero.Nombre
        txtDescripcionTablero.Text = If(_tablero.Descripcion, "")
        lnkQuitarFondo.Visible = _tablero.Fondo IsNot Nothing
    End Sub

    ' El fondo se aplica en cada petición (también en los postbacks que no recargan el tablero).
    Protected Overrides Sub OnPreRender(e As EventArgs)
        MyBase.OnPreRender(e)
        If _tablero Is Nothing Then Return

        ' Guardado automático y cierre al pulsar fuera: solo con el detalle abierto.
        phScriptDetalle.Visible = pnlDetalle.Visible
        ' Un postback dentro del detalle (añadir/eliminar subtarea…) no debe parecer que se cierra y se abre.
        ' (Se fija siempre: CssClass se guarda en el ViewState y no debe arrastrarse al abrir otra tarjeta.)
        Dim mismoDetalle = pnlDetalle.Visible AndAlso _detalleAlCargar IsNot Nothing AndAlso _detalleAlCargar = ClaveDetalle()
        pnlDetalle.CssClass = If(mismoDetalle, "modal-fondo sin-animacion", "modal-fondo")

        ' Título editable al pulsarlo: solo propietario o Admin (el resto no recibe el cuadro de texto).
        pnlTituloEdicion.Visible = PuedeGestionarMiembros
        If PuedeGestionarMiembros Then
            spnTitulo.Attributes("class") = "titulo-texto editable"
            spnTitulo.Attributes("role") = "button"
            spnTitulo.Attributes("tabindex") = "0"
            spnTitulo.Attributes("title") = "Pulsa para cambiar el nombre"
        End If

        If _tablero.Fondo Is Nothing Then Return
        ' Variable CSS (ver .tablero-pagina): Style("background-image") convertiría un degradado en url(linear-gradient(...)).
        pnlTablero.Style("--fondo-imagen") = FondosTablero.ImagenCss(TableroId, _tablero.Fondo)
        pnlTablero.CssClass = "tablero-pagina con-fondo"
    End Sub

    Private Function ClaveDetalle() As String
        Return TarjetaAbiertaId.ToString(CultureInfo.InvariantCulture) & "/" & ListaNuevaId.ToString(CultureInfo.InvariantCulture)
    End Function

    Private Sub MostrarMensaje(mensaje As String, esError As Boolean)
        lblMensaje.Text = HttpUtility.HtmlEncode(mensaje)
        lblMensaje.CssClass = If(esError, "mensaje mensaje-error", "mensaje mensaje-ok")
        lblMensaje.Visible = True
    End Sub

End Class
