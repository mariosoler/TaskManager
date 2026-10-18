Imports TaskManager.Seguridad
Imports TaskManager.Utilidades

Public Class SiteMaster
    Inherits System.Web.UI.MasterPage

    ''' <summary>
    ''' Tablero en el que está el usuario (ya autorizado por la página: Tablero.aspx o Estadisticas.aspx).
    ''' Con él, «Buscar» del menú superior busca en ese tablero y aparece «Estadísticas».
    ''' </summary>
    Public Property TableroActual As Integer?

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        lnkEstilos.Href = Formato.UrlConVersion("~/Content/site.css")
        ' Con versión (?v=fecha del archivo): los navegadores guardan el favicon en caché con mucha insistencia.
        Dim logo = Formato.UrlConVersion("~/Content/logo.svg")
        lnkIcono.Href = logo
        imgLogo.Src = logo
        Dim autenticado = Page.User IsNot Nothing AndAlso Page.User.Identity.IsAuthenticated
        phUsuario.Visible = autenticado
        phNavegacion.Visible = autenticado
    End Sub

    ''' <summary>La pestaña del navegador muestra siempre "TaskManager", en todas las páginas.</summary>
    Protected Sub Page_PreRender(sender As Object, e As EventArgs) Handles Me.PreRender
        Page.Title = "TaskManager"
        ' En PreRender y no en Load: si el usuario acaba de cambiar su nombre en «Mi perfil», ya sale el nuevo.
        If TableroActual.HasValue Then
            Dim id = TableroActual.Value.ToString(Globalization.CultureInfo.InvariantCulture)
            lnkNavBuscar.HRef = "~/Buscar.aspx?tablero=" & id
            lnkNavEstadisticas.HRef = "~/Estadisticas.aspx?id=" & id
            lnkNavEstadisticas.Visible = True
            If TypeOf Page Is EstadisticasPagina Then lnkNavEstadisticas.Attributes("aria-current") = "page"
        End If
        If phUsuario.Visible Then
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
