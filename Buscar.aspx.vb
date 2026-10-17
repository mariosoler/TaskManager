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

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If IsPostBack Then Return

        CargarDesplegables()
        ' Buscar.aspx?tablero=N: búsqueda limitada a ese tablero (si el usuario tiene acceso).
        Dim item = ddlTablero.Items.FindByValue(If(Request.QueryString("tablero"), "").Trim())
        If item IsNot Nothing Then
            ddlTablero.ClearSelection()
            item.Selected = True
        End If
        CargarListas()
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
        For Each ddl In {ddlTablero, ddlLista, ddlAsignado, ddlEstadoFecha, ddlCambiadaPor, ddlSubtareas, ddlCodigo}
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
