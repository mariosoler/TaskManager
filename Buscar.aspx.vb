Imports System.Globalization
Imports TaskManager.Datos
Imports TaskManager.Models
Imports TaskManager.Seguridad
Imports TaskManager.Utilidades

Public Class BuscarPagina
    Inherits PaginaBase

    Private Const MaxEnPantalla As Integer = 200
    Private Const MaxExportacion As Integer = 5000
    Private Shared ReadOnly SoloDigitos As New Regex("^[0-9]+$")

    ''' <summary>El panel de búsqueda avanzada se muestra abierto si tiene algún filtro aplicado.</summary>
    Protected Property AvanzadaAbierta As Boolean

    ''' <summary>El desplegable "Guardar búsqueda" se muestra abierto si falló la validación del nombre.</summary>
    Protected Property GuardarAbierto As Boolean

    ''' <summary>Búsqueda guardada aplicada (Buscar.aspx?busqueda=N); 0 si ninguna.</summary>
    Protected Property BusquedaActivaId As Integer
        Get
            Return If(ViewState("BusquedaActivaId") Is Nothing, 0, CInt(ViewState("BusquedaActivaId")))
        End Get
        Set(value As Integer)
            ViewState("BusquedaActivaId") = value
        End Set
    End Property

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If IsPostBack Then Return

        CargarDesplegables()
        Dim busquedaId As Integer
        If Integer.TryParse(Request.QueryString("busqueda"), busquedaId) Then
            ' Buscar.aspx?busqueda=N: aplica una búsqueda guardada propia o compartida por un compañero.
            AplicarBusquedaGuardada(busquedaId)
        Else
            ' Buscar.aspx?tablero=N: búsqueda limitada a ese tablero (si el usuario tiene acceso).
            Seleccionar(ddlTablero, If(Request.QueryString("tablero"), "").Trim())
            CargarListas()
        End If
        CargarGuardadas()
        Buscar()
    End Sub

    Protected Sub ddlTablero_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ddlTablero.SelectedIndexChanged
        CargarListas()
        Buscar()
    End Sub

    Protected Sub btnBuscar_Click(sender As Object, e As EventArgs) Handles btnBuscar.Click
        If IsValid Then Buscar()
    End Sub

    Protected Sub btnLimpiar_Click(sender As Object, e As EventArgs) Handles btnLimpiar.Click
        Response.Redirect("~/Buscar.aspx")
    End Sub

    ' ----- Búsquedas guardadas -----

    Protected Sub btnGuardarBusqueda_Click(sender As Object, e As EventArgs) Handles btnGuardarBusqueda.Click
        Dim nombre = Texto(txtNombreBusqueda.Text, BusquedasGuardadasDatos.LongitudMaximaNombre)
        If Not IsValid OrElse nombre.Length = 0 Then
            GuardarAbierto = True
            Buscar()
            Return
        End If

        Dim criterios = LeerFiltro().ACriterios()
        If criterios.Length > BusquedasGuardadasDatos.LongitudMaximaCriterios Then
            MostrarMensaje("La búsqueda es demasiado larga para guardarla; acorta el texto.", True)
            GuardarAbierto = True
            Buscar()
            Return
        End If

        Dim id = BusquedasGuardadasDatos.Guardar(UsuarioId, nombre, criterios, chkCompartirBusqueda.Checked)
        Response.Redirect("~/Buscar.aspx?busqueda=" & id.ToString(CultureInfo.InvariantCulture))
    End Sub

    Protected Sub rptGuardadas_ItemCommand(source As Object, e As RepeaterCommandEventArgs) Handles rptGuardadas.ItemCommand
        Dim busquedaId As Integer
        If e.CommandName = "Eliminar" AndAlso Integer.TryParse(CStr(e.CommandArgument), busquedaId) Then
            If BusquedasGuardadasDatos.Eliminar(busquedaId, UsuarioId) Then
                MostrarMensaje("Búsqueda eliminada.", False)
                If busquedaId = BusquedaActivaId Then
                    BusquedaActivaId = 0
                    txtNombreBusqueda.Text = ""
                    chkCompartirBusqueda.Checked = False
                End If
            End If
        End If
        CargarGuardadas()
        Buscar()
    End Sub

    Private Sub CargarGuardadas()
        Dim guardadas = BusquedasGuardadasDatos.Listar(UsuarioId)
        rptGuardadas.DataSource = guardadas
        rptGuardadas.DataBind()
        rptGuardadas.Visible = guardadas.Count > 0
    End Sub

    ''' <summary>Pone en los controles los criterios de la búsqueda guardada, si el usuario puede verla.</summary>
    Private Sub AplicarBusquedaGuardada(busquedaId As Integer)
        Dim guardada = BusquedasGuardadasDatos.Obtener(busquedaId, UsuarioId)
        If guardada Is Nothing Then
            MostrarMensaje("La búsqueda guardada no existe o ya no está compartida contigo.", True)
            CargarListas()
            Return
        End If

        BusquedaActivaId = guardada.Id
        Title = "Buscar: " & HttpUtility.HtmlEncode(guardada.Nombre)
        If guardada.EsPropia Then
            ' Así, volver a guardar con el mismo nombre la actualiza.
            txtNombreBusqueda.Text = guardada.Nombre
            chkCompartirBusqueda.Checked = guardada.Compartida
        End If

        If Not AplicarFiltro(FiltroBusqueda.DesdeCriterios(guardada.Criterios)) Then
            MostrarMensaje("Algunos criterios de esta búsqueda no se han aplicado porque se refieren a tableros, listas o personas a los que no tienes acceso.", True)
        End If
    End Sub

    ''' <summary>
    ''' Pone el filtro en los controles. Cada valor se valida contra las opciones disponibles para el usuario
    ''' (sus tableros, listas y compañeros); devuelve False si alguno no se ha podido aplicar.
    ''' </summary>
    Private Function AplicarFiltro(filtro As FiltroBusqueda) As Boolean
        Dim completo = True
        Dim elegir = Sub(ddl As DropDownList, valor As String)
                         If Not Seleccionar(ddl, valor) Then completo = False
                     End Sub
        Dim entero = Function(n As Integer?) If(n.HasValue, n.Value.ToString(CultureInfo.InvariantCulture), "")
        Dim fecha = Function(f As DateTime?) If(f.HasValue, f.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "")

        txtTexto.Text = Texto(filtro.Texto, 400)
        chkEnTitulo.Checked = filtro.EnTitulo
        chkEnDescripcion.Checked = filtro.EnDescripcion
        chkEnCodigo.Checked = filtro.EnCodigo
        chkEnComentarios.Checked = filtro.EnComentarios
        chkEnSubtareas.Checked = filtro.EnSubtareas

        elegir(ddlTablero, entero(filtro.TableroId))
        CargarListas()
        elegir(ddlLista, entero(filtro.ListaId))
        elegir(ddlAsignado, filtro.Asignado)
        elegir(ddlEstadoFecha, filtro.EstadoFecha)
        elegir(ddlPrioridad, filtro.Prioridad)
        elegir(ddlCambiadaPor, entero(filtro.CambiadaPor))
        elegir(ddlSubtareas, filtro.Subtareas)
        elegir(ddlCodigo, If(filtro.ConCodigo.HasValue, If(filtro.ConCodigo.Value, "si", "no"), ""))
        elegir(ddlOrden, filtro.Orden)

        Dim caso = If(filtro.Caso, "")
        txtCaso.Text = If(caso.Length <= 20 AndAlso SoloDigitos.IsMatch(caso), caso, "")
        txtLimiteDesde.Text = fecha(filtro.LimiteDesde)
        txtLimiteHasta.Text = fecha(filtro.LimiteHasta)
        txtCreadaDesde.Text = fecha(filtro.CreadaDesde)
        txtCreadaHasta.Text = fecha(filtro.CreadaHasta)
        txtModificadaDesde.Text = fecha(filtro.ModificadaDesde)
        txtModificadaHasta.Text = fecha(filtro.ModificadaHasta)
        Return completo
    End Function

    ''' <summary>Selecciona el valor si existe en la lista; vacío = primera opción. Devuelve False si no existe.</summary>
    Private Shared Function Seleccionar(ddl As DropDownList, valor As String) As Boolean
        ddl.ClearSelection()
        Dim item = If(String.IsNullOrEmpty(valor), Nothing, ddl.Items.FindByValue(valor))
        If item IsNot Nothing Then
            item.Selected = True
            Return True
        End If
        If ddl.Items.Count > 0 Then ddl.Items(0).Selected = True
        Return String.IsNullOrEmpty(valor)
    End Function

    Protected Function TituloGuardada(guardada As BusquedaGuardada) As String
        Dim texto = If(guardada.EsPropia, "Tuya", "De " & guardada.NombreUsuario)
        If guardada.Compartida Then texto &= " · compartida con tus compañeros"
        Return texto & " · " & Formato.FechaLocal(guardada.FechaCreacion)
    End Function

    Private Sub MostrarMensaje(mensaje As String, esError As Boolean)
        lblMensaje.Text = HttpUtility.HtmlEncode(mensaje)
        lblMensaje.CssClass = If(esError, "mensaje mensaje-error", "mensaje mensaje-ok")
        lblMensaje.Visible = True
    End Sub

    ' ----- Exportación -----

    Protected Sub btnExportar_Click(sender As Object, e As EventArgs) Handles btnExportar.Click
        If Not IsValid Then Return
        Dim filtro = LeerFiltro()
        Dim resultado = BusquedaDatos.Buscar(UsuarioId, filtro, MaxExportacion)

        Dim libro As New LibroExcel()
        ExportacionExcel.AgregarHojaTarjetas(libro, "Tareas", resultado.Tarjetas)
        libro.AgregarHoja("Criterios", {"Criterio", "Valor"}, DescribirCriterios(resultado))
        EnviarArchivo(libro.ABytes(), "tareas-" & DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) & ".xlsx",
                      LibroExcel.TipoMime)
    End Sub

    Protected Overrides Sub OnPreRender(e As EventArgs)
        MyBase.OnPreRender(e)
        AvanzadaAbierta = ddlLista.SelectedValue <> "" OrElse txtCaso.Text.Trim() <> "" OrElse ddlCambiadaPor.SelectedValue <> "" OrElse
                          txtLimiteDesde.Text <> "" OrElse txtLimiteHasta.Text <> "" OrElse txtCreadaDesde.Text <> "" OrElse
                          txtCreadaHasta.Text <> "" OrElse txtModificadaDesde.Text <> "" OrElse txtModificadaHasta.Text <> "" OrElse
                          ddlSubtareas.SelectedValue <> "" OrElse ddlCodigo.SelectedValue <> "" OrElse
                          ddlOrden.SelectedIndex > 0 OrElse Not chkEnTitulo.Checked OrElse Not chkEnDescripcion.Checked OrElse
                          chkEnCodigo.Checked OrElse chkEnComentarios.Checked OrElse chkEnSubtareas.Checked
    End Sub

    Private Sub CargarDesplegables()
        ddlTablero.Items.Clear()
        ddlTablero.Items.Add(New ListItem("Todos mis tableros", ""))
        For Each tablero In TablerosDatos.ListarDeUsuario(UsuarioId)
            ddlTablero.Items.Add(New ListItem(tablero.Nombre, tablero.Id.ToString(CultureInfo.InvariantCulture)))
        Next

        Dim companeros = UsuariosDatos.ListarCompaneros(UsuarioId)
        ddlAsignado.Items.Clear()
        ddlAsignado.Items.Add(New ListItem("Cualquiera", ""))
        ddlAsignado.Items.Add(New ListItem("A mí", FiltroBusqueda.AsignadoYo))
        ddlAsignado.Items.Add(New ListItem("Sin asignar", FiltroBusqueda.AsignadoNadie))
        ddlCambiadaPor.Items.Clear()
        ddlCambiadaPor.Items.Add(New ListItem("Cualquiera", ""))
        For Each persona In companeros
            Dim id = persona.UsuarioId.ToString(CultureInfo.InvariantCulture)
            ddlAsignado.Items.Add(New ListItem(persona.Nombre, id))
            ddlCambiadaPor.Items.Add(New ListItem(persona.Nombre, id))
        Next
    End Sub

    Private Sub CargarListas()
        ddlLista.Items.Clear()
        ddlLista.Items.Add(New ListItem("Todas las listas", ""))
        Dim tableroId = TableroSeleccionado()
        If tableroId.HasValue AndAlso TablerosDatos.Obtener(tableroId.Value, UsuarioId) IsNot Nothing Then
            For Each lista In ListasDatos.Listar(tableroId.Value)
                ddlLista.Items.Add(New ListItem(lista.Titulo, lista.Id.ToString(CultureInfo.InvariantCulture)))
            Next
        End If
        ddlLista.Enabled = ddlLista.Items.Count > 1
        ddlLista.ToolTip = If(ddlLista.Enabled, "", "Elige primero un tablero")
    End Sub

    Private Sub Buscar()
        Dim resultado = BusquedaDatos.Buscar(UsuarioId, LeerFiltro(), MaxEnPantalla)
        rptResultados.DataSource = resultado.Tarjetas
        rptResultados.DataBind()
        rptResultados.Visible = resultado.Tarjetas.Count > 0
        phSinResultados.Visible = resultado.Tarjetas.Count = 0
        btnExportar.Visible = resultado.Total > 0

        If resultado.Total = 0 Then
            litResumen.Text = "Ninguna tarea encontrada."
        ElseIf resultado.Total = 1 Then
            litResumen.Text = "1 tarea encontrada."
        ElseIf resultado.Total <= resultado.Tarjetas.Count Then
            litResumen.Text = String.Format(CultureInfo.CurrentCulture, "{0:N0} tareas encontradas.", resultado.Total)
        Else
            litResumen.Text = String.Format(CultureInfo.CurrentCulture,
                "{0:N0} tareas encontradas; se muestran las {1:N0} primeras. Afina la búsqueda o exporta a Excel (hasta {2:N0}).",
                resultado.Total, resultado.Tarjetas.Count, MaxExportacion)
        End If
    End Sub

    Private Function LeerFiltro() As FiltroBusqueda
        Dim caso = Texto(txtCaso.Text, 20)
        Dim cambiadaPor As Integer
        Return New FiltroBusqueda With {
            .Texto = Texto(txtTexto.Text, 400),
            .EnTitulo = chkEnTitulo.Checked,
            .EnDescripcion = chkEnDescripcion.Checked,
            .EnCodigo = chkEnCodigo.Checked,
            .EnComentarios = chkEnComentarios.Checked,
            .EnSubtareas = chkEnSubtareas.Checked,
            .TableroId = TableroSeleccionado(),
            .ListaId = Entero(ddlLista.SelectedValue),
            .Asignado = ddlAsignado.SelectedValue,
            .EstadoFecha = ddlEstadoFecha.SelectedValue,
            .Prioridad = ddlPrioridad.SelectedValue,
            .Caso = If(SoloDigitos.IsMatch(caso), caso, Nothing),
            .LimiteDesde = Fecha(txtLimiteDesde.Text),
            .LimiteHasta = Fecha(txtLimiteHasta.Text),
            .CreadaDesde = Fecha(txtCreadaDesde.Text),
            .CreadaHasta = Fecha(txtCreadaHasta.Text),
            .ModificadaDesde = Fecha(txtModificadaDesde.Text),
            .ModificadaHasta = Fecha(txtModificadaHasta.Text),
            .CambiadaPor = If(Integer.TryParse(ddlCambiadaPor.SelectedValue, cambiadaPor), cambiadaPor, CType(Nothing, Integer?)),
            .Subtareas = ddlSubtareas.SelectedValue,
            .ConCodigo = If(ddlCodigo.SelectedValue = "si", True, If(ddlCodigo.SelectedValue = "no", False, CType(Nothing, Boolean?))),
            .Orden = ddlOrden.SelectedValue
        }
    End Function

    ''' <summary>Hoja "Criterios" del Excel: qué se buscó, cuándo y cuántos resultados.</summary>
    Private Function DescribirCriterios(resultado As ResultadoBusqueda) As List(Of Object())
        Dim filas As New List(Of Object()) From {
            New Object() {"Exportado", DateTime.Now},
            New Object() {"Tareas encontradas", resultado.Total},
            New Object() {"Tareas exportadas", resultado.Tarjetas.Count}
        }
        Dim agregar = Sub(nombre As String, valor As String)
                          If Not String.IsNullOrWhiteSpace(valor) Then filas.Add(New Object() {nombre, valor})
                      End Sub

        agregar("Texto", txtTexto.Text.Trim())
        If txtTexto.Text.Trim() <> "" Then
            Dim campos = {chkEnTitulo, chkEnDescripcion, chkEnCodigo, chkEnComentarios, chkEnSubtareas}.Where(Function(c) c.Checked).Select(Function(c) c.Text)
            agregar("Buscar en", String.Join(", ", campos))
        End If
        For Each ddl In {ddlTablero, ddlLista, ddlAsignado, ddlEstadoFecha, ddlPrioridad, ddlCambiadaPor, ddlSubtareas, ddlCodigo}
            If ddl.SelectedValue <> "" Then agregar(EtiquetaDe(ddl), ddl.SelectedItem.Text)
        Next
        agregar("Nº de caso", txtCaso.Text.Trim())
        agregar("Fecha límite entre", Rango(txtLimiteDesde.Text, txtLimiteHasta.Text))
        agregar("Creada entre", Rango(txtCreadaDesde.Text, txtCreadaHasta.Text))
        agregar("Modificada entre", Rango(txtModificadaDesde.Text, txtModificadaHasta.Text))
        agregar("Ordenar por", ddlOrden.SelectedItem.Text)
        Return filas
    End Function

    Private Function EtiquetaDe(ddl As DropDownList) As String
        Select Case ddl.ID
            Case "ddlTablero" : Return "Tablero"
            Case "ddlLista" : Return "Lista"
            Case "ddlAsignado" : Return "Asignada a"
            Case "ddlEstadoFecha" : Return "Fecha límite"
            Case "ddlPrioridad" : Return "Prioridad"
            Case "ddlCambiadaPor" : Return "Con cambios de"
            Case "ddlSubtareas" : Return "Subtareas"
            Case Else : Return "Código"
        End Select
    End Function

    Private Shared Function Rango(desde As String, hasta As String) As String
        Dim d = Fecha(desde), h = Fecha(hasta)
        If Not d.HasValue AndAlso Not h.HasValue Then Return Nothing
        Return If(d.HasValue, d.Value.ToString("d", CultureInfo.CurrentCulture), "…") & " – " &
               If(h.HasValue, h.Value.ToString("d", CultureInfo.CurrentCulture), "…")
    End Function

    Protected Function UrlTarjeta(encontrada As TarjetaEncontrada) As String
        Return ResolveUrl("~/Tablero.aspx") & "?id=" & encontrada.TableroId.ToString(CultureInfo.InvariantCulture) &
               "&tarjeta=" & encontrada.Tarjeta.Id.ToString(CultureInfo.InvariantCulture)
    End Function

    Private Function TableroSeleccionado() As Integer?
        Return Entero(ddlTablero.SelectedValue)
    End Function

    Private Shared Function Entero(valor As String) As Integer?
        Dim n As Integer
        If Integer.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, n) Then Return n
        Return Nothing
    End Function

    Private Shared Function Fecha(valor As String) As DateTime?
        Dim f As DateTime
        If DateTime.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, f) Then Return f
        Return Nothing
    End Function

End Class
