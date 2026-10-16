Imports System.Globalization
Imports TaskManager.Datos
Imports TaskManager.Seguridad

Public Class LoginPagina
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack AndAlso User.Identity.IsAuthenticated Then
            Response.Redirect(FormsAuthentication.DefaultUrl)
        End If
    End Sub

    Protected Sub btnEntrar_Click(sender As Object, e As EventArgs) Handles btnEntrar.Click
        If Not IsValid Then Return

        Dim usuario = UsuariosDatos.ObtenerPorEmail(NormalizarEmail(txtEmail.Text))
        If Not Autenticacion.ValidarCredenciales(usuario, txtPassword.Text) Then
            lblError.Text = "Email o contraseña incorrectos."
            lblError.Visible = True
            Return
        End If

        Autenticacion.IniciarSesion(usuario, chkRecordar.Checked)
        ' GetRedirectUrl solo acepta ReturnUrl locales; si no, usa defaultUrl.
        Response.Redirect(FormsAuthentication.GetRedirectUrl(
            usuario.Id.ToString(CultureInfo.InvariantCulture), chkRecordar.Checked))
    End Sub

End Class
