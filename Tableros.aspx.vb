Imports System.Globalization
Imports TaskManager.Datos
Imports TaskManager.Seguridad
Imports TaskManager.Utilidades

Public Class TablerosPagina
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then CargarTableros()
    End Sub

    Protected Sub btnCrear_Click(sender As Object, e As EventArgs) Handles btnCrear.Click
        If Not IsValid Then Return

        Dim nombre = Texto(txtNombre.Text, 150)
        If nombre.Length = 0 Then Return

        Dim id = TablerosDatos.Crear(nombre, Texto(txtDescripcion.Text, 500), UsuarioId)
        Response.Redirect("~/Tablero.aspx?id=" & id.ToString(CultureInfo.InvariantCulture))
    End Sub

    Protected Sub rptTableros_ItemCommand(source As Object, e As RepeaterCommandEventArgs) Handles rptTableros.ItemCommand
        Dim tableroId As Integer
        If e.CommandName = "Eliminar" AndAlso Integer.TryParse(CStr(e.CommandArgument), tableroId) Then
            Dim tablero = TablerosDatos.Obtener(tableroId, UsuarioId)
            If TablerosDatos.Eliminar(tableroId, UsuarioId) Then
                FondosTablero.Eliminar(tablero?.Fondo)
            Else
                lblMensaje.Text = "Solo el propietario puede eliminar el tablero."
                lblMensaje.Visible = True
            End If
        End If
        CargarTableros()
    End Sub

    Private Sub CargarTableros()
        rptTableros.DataSource = TablerosDatos.ListarDeUsuario(UsuarioId)
        rptTableros.DataBind()
    End Sub

End Class
