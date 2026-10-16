Imports TaskManager.Seguridad
Imports TaskManager.Utilidades

Public Class SiteMaster
    Inherits System.Web.UI.MasterPage

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        lnkEstilos.Href = Formato.UrlConVersion("~/Content/site.css")
        Dim autenticado = Page.User IsNot Nothing AndAlso Page.User.Identity.IsAuthenticated
        phUsuario.Visible = autenticado
        If autenticado Then
            Dim nombre = Autenticacion.NombreUsuarioActual()
            litNombreUsuario.Text = nombre
            litIniciales.Text = Formato.Iniciales(nombre)
        End If
    End Sub

    Protected Sub lnkSalir_Click(sender As Object, e As EventArgs) Handles lnkSalir.Click
        FormsAuthentication.SignOut()
        Response.Redirect(FormsAuthentication.LoginUrl)
    End Sub

End Class
