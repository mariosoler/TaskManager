Imports TaskManager.Datos
Imports TaskManager.Models
Imports TaskManager.Seguridad

Public Class RegistroPagina
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack AndAlso User.Identity.IsAuthenticated Then
            Response.Redirect(FormsAuthentication.DefaultUrl)
        End If
    End Sub

    Protected Sub btnRegistrar_Click(sender As Object, e As EventArgs) Handles btnRegistrar.Click
        If Not IsValid Then Return

        Dim nombre = Texto(txtNombre.Text, 100)
        Dim email = NormalizarEmail(txtEmail.Text)
        Dim password = txtPassword.Text

        ' Los validadores ya lo comprueban; se repite por si el cliente los omite.
        If nombre.Length = 0 OrElse email.Length = 0 OrElse email.Length > 200 Then
            MostrarError("Revisa el nombre y el email.")
            Return
        End If
        If password.Length < 8 OrElse password.Length > 128 OrElse password <> txtConfirmar.Text Then
            MostrarError("Revisa la contraseña.")
            Return
        End If

        Dim id = UsuariosDatos.Crear(nombre, email, PasswordHasher.Hash(password))
        If Not id.HasValue Then
            MostrarError("Ya existe una cuenta con ese email.")
            Return
        End If

        Autenticacion.IniciarSesion(New Usuario With {.Id = id.Value, .Nombre = nombre, .Email = email}, False)
        Response.Redirect(FormsAuthentication.DefaultUrl)
    End Sub

    Private Sub MostrarError(mensaje As String)
        lblError.Text = mensaje
        lblError.Visible = True
    End Sub

End Class
