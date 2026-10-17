Imports System.Globalization
Imports TaskManager.Datos
Imports TaskManager.Models
Imports TaskManager.Seguridad

''' <summary>«Mi perfil»: el usuario cambia su nombre y el formato en que ve las fechas.</summary>
Public Class PerfilPagina
    Inherits PaginaBase

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If IsPostBack Then Return

        Dim perfil = UsuarioActual.Perfil()
        If perfil Is Nothing Then
            ' La cuenta ya no existe: se cierra la sesión.
            FormsAuthentication.SignOut()
            Response.Redirect(FormsAuthentication.LoginUrl)
            Return
        End If

        CargarFormatos()
        litEmail.Text = perfil.Email
        txtNombre.Text = perfil.Nombre
        ddlFormatoFecha.SelectedValue = FormatoFecha.Obtener(perfil.FormatoFecha).Clave
    End Sub

    Protected Sub btnGuardar_Click(sender As Object, e As EventArgs) Handles btnGuardar.Click
        If Not IsValid Then Return

        Dim nombre = Texto(txtNombre.Text, 100)
        If nombre.Length = 0 Then
            MostrarMensaje("Escribe tu nombre.", esError:=True)
            Return
        End If
        ' Solo se aceptan las claves de la lista blanca.
        Dim formato = FormatoFecha.Buscar(ddlFormatoFecha.SelectedValue)
        If formato Is Nothing Then
            MostrarMensaje("Elige un formato de fecha de la lista.", esError:=True)
            Return
        End If

        UsuariosDatos.ActualizarPerfil(UsuarioId, nombre, formato.Clave)
        UsuarioActual.Olvidar()
        txtNombre.Text = nombre
        MostrarMensaje("Cambios guardados.", esError:=False)
    End Sub

    ''' <summary>Cada opción muestra la fecha de hoy con ese formato: "17/10/2026 (17 oct)".</summary>
    Private Sub CargarFormatos()
        Dim hoy = DateTime.Today
        For Each formato In FormatoFecha.Todos
            Dim ejemplo = String.Format(CultureInfo.CurrentCulture, "{0} ({1})",
                                        hoy.ToString(formato.Larga, CultureInfo.CurrentCulture),
                                        hoy.ToString(formato.Corta, CultureInfo.CurrentCulture))
            If formato Is FormatoFecha.Predeterminado Then ejemplo = "Predeterminado: " & ejemplo
            ddlFormatoFecha.Items.Add(New ListItem(ejemplo, formato.Clave))
        Next
    End Sub

    Private Sub MostrarMensaje(texto As String, esError As Boolean)
        lblMensaje.Text = HttpUtility.HtmlEncode(texto)
        lblMensaje.CssClass = If(esError, "mensaje mensaje-error", "mensaje mensaje-ok")
        lblMensaje.Visible = True
    End Sub

End Class
