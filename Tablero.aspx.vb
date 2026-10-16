Imports System.Globalization
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
        If Not IsPostBack Then CargarTablero()
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

        datos.Descripcion = Texto(txtDescripcionTarjeta.Text, 20000).Replace(vbCrLf, vbLf)
        ' El código no se recorta: los espacios iniciales (sangría) son significativos.
        Dim codigo = If(txtCodigo.Text, "").Replace(vbCrLf, vbLf)
        If codigo.Length > LongitudMaximaCodigo Then codigo = codigo.Substring(0, LongitudMaximaCodigo)
        datos.Codigo = If(codigo.Trim().Length = 0, Nothing, codigo)

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

    Protected Sub btnComentar_Click(sender As Object, e As EventArgs) Handles btnComentar.Click
        Dim comentario = Texto(txtComentario.Text, 4000)
        If comentario.Length > 0 Then
            ComentariosDatos.Crear(TarjetaAbiertaId, TableroId, UsuarioId, comentario)
            txtComentario.Text = ""
            CargarComentarios()
            CargarActividad()
            CargarTablero()
        End If
    End Sub

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
        phComentarios.Visible = Not esNueva

        litDetalleLista.Text = tarjeta.NombreLista
        litFechasTarjeta.Text = ""
        txtCaso.Text = If(tarjeta.Caso, "")
        txtTituloTarjeta.Text = tarjeta.Titulo
        txtDescripcionTarjeta.Text = If(tarjeta.Descripcion, "")
        txtFechaLimite.Text = If(tarjeta.FechaLimite.HasValue,
                                 tarjeta.FechaLimite.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "")
        txtCodigo.Text = If(tarjeta.Codigo, "")
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
            Case Else
                Return "editó " & HttpUtility.HtmlEncode(campo)
        End Select
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

    ' ----- Carga y formato -----

    Private Sub CargarTablero()
        litNombre.Text = _tablero.Nombre
        litDescripcion.Text = If(_tablero.Descripcion, "")

        rptListas.DataSource = ListasDatos.ListarConTarjetas(TableroId)
        rptListas.DataBind()

        rptMiembros.DataSource = TablerosDatos.ListarMiembros(TableroId)
        rptMiembros.DataBind()
        pnlAgregarMiembro.Visible = PuedeGestionarMiembros
    End Sub

    Private Sub MostrarMensaje(mensaje As String, esError As Boolean)
        lblMensaje.Text = HttpUtility.HtmlEncode(mensaje)
        lblMensaje.CssClass = If(esError, "mensaje mensaje-error", "mensaje mensaje-ok")
        lblMensaje.Visible = True
    End Sub

    ''' <summary>Etiqueta de fecha límite (sin texto del usuario); en rojo si ya ha vencido.</summary>
    Protected Function HtmlFechaLimite(valor As Object) As String
        If Not TypeOf valor Is DateTime Then Return ""
        Dim fecha = CDate(valor)
        Dim clase = If(fecha.Date < DateTime.Today, "info info-fecha vencida", "info info-fecha")
        Return String.Format(CultureInfo.InvariantCulture, "<span class=""{0}"" title=""Fecha límite"">&#128197; {1}</span>",
                             clase, fecha.ToString("d MMM", CultureInfo.CurrentCulture))
    End Function

End Class
