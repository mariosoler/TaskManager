Imports System.Globalization
Imports System.Web.Script.Serialization
Imports TaskManager.Datos
Imports TaskManager.Models
Imports TaskManager.Seguridad
Imports TaskManager.Utilidades

Public Class TableroPagina
    Inherits PaginaBase

    Private Shared ReadOnly SoloDigitos As New Regex("^[0-9]+$")
    Private Const LongitudMaximaCodigo As Integer = 200000

    ' Tablero de la URL, cargado y autorizado en cada petición (también en los postbacks).
    Private _tablero As Tablero

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

        Title = HttpUtility.HtmlEncode(_tablero.Nombre)
        Dim idTexto = _tablero.Id.ToString(CultureInfo.InvariantCulture)
        lnkBuscarTablero.HRef = "~/Buscar.aspx?tablero=" & idTexto
        lnkEstadisticas.HRef = "~/Estadisticas.aspx?id=" & idTexto

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
                ListasDatos.Eliminar(listaId, TableroId)
        End Select
        CargarTablero()
    End Sub

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

    Protected Sub btnGuardarTarjeta_Click(sender As Object, e As EventArgs) Handles btnGuardarTarjeta.Click
        If Not IsValid Then Return
        Dim datos As New Tarjeta With {.Titulo = Texto(txtTituloTarjeta.Text, 200)}
        If datos.Titulo.Length = 0 Then Return

        Dim caso = Texto(txtCaso.Text, 20)
        If caso.Length > 0 AndAlso Not SoloDigitos.IsMatch(caso) Then Return
        datos.Caso = If(caso.Length = 0, Nothing, caso)

        Dim fecha As DateTime
        If DateTime.TryParseExact(txtFechaLimite.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, fecha) Then
            datos.FechaLimite = fecha
        End If

        Dim usuario As Integer
        If Integer.TryParse(ddlAsignado.SelectedValue, usuario) Then datos.AsignadoA = usuario

        Dim valorPrioridad As Integer
        If Integer.TryParse(ddlPrioridad.SelectedValue, valorPrioridad) AndAlso Prioridad.EsValida(valorPrioridad) Then
            datos.Prioridad = valorPrioridad
        End If

        datos.Descripcion = Texto(txtDescripcionTarjeta.Text, 20000).Replace(vbCrLf, vbLf)
        ' El código no se recorta: los espacios iniciales (sangría) son significativos.
        Dim codigo = If(txtCodigo.Text, "").Replace(vbCrLf, vbLf)
        If codigo.Length > LongitudMaximaCodigo Then codigo = codigo.Substring(0, LongitudMaximaCodigo)
        datos.Codigo = If(codigo.Trim().Length = 0, Nothing, codigo)

        Dim ignoradas As List(Of String) = Nothing
        datos.Etiquetas = Etiqueta.Analizar(txtEtiquetas.Text, ignoradas).Select(Function(n) New Etiqueta With {.Nombre = n}).ToList()
        If ignoradas.Count > 0 Then
            MostrarMensaje("No se han guardado estas etiquetas (solo letras, números, _ y -, hasta " &
                           Etiqueta.LongitudMaxima & " caracteres y " & Etiqueta.MaximoPorTarjeta & " por tarjeta): " &
                           String.Join(" ", ignoradas), True)
        End If

        If ListaNuevaId > 0 Then
            TarjetasDatos.Crear(ListaNuevaId, TableroId, datos, SubtareasPendientes, UsuarioId)
        Else
            TarjetasDatos.Actualizar(TarjetaAbiertaId, TableroId, datos, UsuarioId)
        End If
        CerrarDetalle()
        CargarTablero()
    End Sub

    Protected Sub btnEliminarTarjeta_Click(sender As Object, e As EventArgs) Handles btnEliminarTarjeta.Click
        TarjetasDatos.Eliminar(TarjetaAbiertaId, TableroId)
        CerrarDetalle()
        CargarTablero()
    End Sub

    Protected Sub btnCerrarDetalle_Click(sender As Object, e As EventArgs) Handles btnCerrarDetalle.Click
        CerrarDetalle()
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
    ''' Etiquetas para las sugerencias de Scripts/etiquetas.js: las del tablero (n, c = color, t = tinta, u = usos)
    ''' y las de otros tableros del usuario (o = 1). Vacío si el detalle está cerrado.
    ''' </summary>
    Protected ReadOnly Property EtiquetasJson As String
        Get
            If Not pnlDetalle.Visible Then Return "[]"
            Dim propias = EtiquetasDatos.ListarDeTablero(TableroId).Select(
                Function(e) New With {.n = e.Nombre, .c = e.ColorSeguro, .t = e.Tinta, .u = e.Usos, .o = 0})
            Dim otras = EtiquetasDatos.ListarDeOtrosTableros(UsuarioId, TableroId).Select(
                Function(e) New With {.n = e.Nombre, .c = e.ColorSeguro, .t = e.Tinta, .u = 0, .o = 1})
            Dim json = New JavaScriptSerializer().Serialize(propias.Concat(otras).ToList())
            Return json.Replace("<", "<").Replace(">", ">").Replace("&", "&")
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

    ''' <summary>Atributo style de una etiqueta (colores ya validados por Etiqueta.ColorSeguro).</summary>
    Protected Shared Function EstiloEtiqueta(dato As Object) As String
        Dim etiqueta = CType(dato, Etiqueta)
        Return "background-color: " & etiqueta.ColorSeguro & "; color: " & etiqueta.Tinta
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
        MostrarDetalle(New Tarjeta With {.NombreLista = nombreLista, .Titulo = ""}, esNueva:=True)
    End Sub

    ''' <summary>Rellena y muestra el modal, en modo creación (esNueva) o edición.</summary>
    Private Sub MostrarDetalle(tarjeta As Tarjeta, esNueva As Boolean)
        litTituloModal.Text = If(esNueva, "Nueva tarjeta", "Detalle de la tarjeta")
        btnGuardarTarjeta.Text = If(esNueva, "Crear tarjeta", "Guardar")
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

        CargarSubtareas()
        pnlDetalle.Visible = True
        txtTituloTarjeta.Focus()
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
        Dim texto = "Modificada el " & Formato.FechaLocal(fecha)
        Dim quien = TryCast(nombre, String)
        Return If(String.IsNullOrEmpty(quien), texto, texto & " por " & quien)
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
            Case HistorialTarjeta.SubtareaEliminada
                Return "eliminó la subtarea " & Valor(despues)
            Case HistorialTarjeta.Comentada
                Return "comentó " & Valor(despues)
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
            Return fecha.ToString("d MMM yyyy", CultureInfo.CurrentCulture)
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
        Dim textoSubtarea = Texto(txtNuevaSubtarea.Text, 300)
        If textoSubtarea.Length > 0 Then
            If CreandoTarjeta Then
                Dim pendientes = SubtareasPendientes
                pendientes.Add(New Subtarea With {.Texto = textoSubtarea})
                SubtareasPendientes = pendientes
            Else
                SubtareasDatos.Crear(TarjetaAbiertaId, TableroId, textoSubtarea, UsuarioId)
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

    Private Sub CargarSubtareas()
        Dim subtareas As List(Of Subtarea)
        If CreandoTarjeta Then
            subtareas = SubtareasPendientes.Select(
                Function(s, i) New Subtarea With {.Id = i, .Texto = s.Texto, .Completada = s.Completada}).ToList()
        Else
            subtareas = SubtareasDatos.Listar(TarjetaAbiertaId, TableroId)
        End If

        rptSubtareas.DataSource = subtareas
        rptSubtareas.DataBind()
        Dim hechas = subtareas.Where(Function(s) s.Completada).Count()
        litProgresoSubtareas.Text = If(subtareas.Count = 0, "",
                                       String.Format(CultureInfo.InvariantCulture, "{0}/{1}", hechas, subtareas.Count))
    End Sub

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
            Title = HttpUtility.HtmlEncode(nombre)
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
            Title = HttpUtility.HtmlEncode(nombre)
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
        enlace.Style("background-image") = fondo.ImagenCss(miniatura:=True)
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

        rptListas.DataSource = ListasDatos.ListarConTarjetas(TableroId)
        rptListas.DataBind()

        rptMiembros.DataSource = TablerosDatos.ListarMiembros(TableroId)
        rptMiembros.DataBind()
        pnlAgregarMiembro.Visible = PuedeGestionarMiembros
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

        ' Título editable al pulsarlo: solo propietario o Admin (el resto no recibe el cuadro de texto).
        pnlTituloEdicion.Visible = PuedeGestionarMiembros
        If PuedeGestionarMiembros Then
            spnTitulo.Attributes("class") = "titulo-texto editable"
            spnTitulo.Attributes("role") = "button"
            spnTitulo.Attributes("tabindex") = "0"
            spnTitulo.Attributes("title") = "Pulsa para cambiar el nombre"
        End If

        If _tablero.Fondo Is Nothing Then Return
        pnlTablero.Style("background-image") = FondosTablero.ImagenCss(TableroId, _tablero.Fondo)
        pnlTablero.CssClass = "tablero-pagina con-fondo"
    End Sub

    Private Sub MostrarMensaje(mensaje As String, esError As Boolean)
        lblMensaje.Text = HttpUtility.HtmlEncode(mensaje)
        lblMensaje.CssClass = If(esError, "mensaje mensaje-error", "mensaje mensaje-ok")
        lblMensaje.Visible = True
    End Sub

End Class
