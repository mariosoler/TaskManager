Imports System.Globalization

Namespace Seguridad

    ''' <summary>
    ''' Página base: protege el ViewState frente a CSRF y expone el usuario autenticado.
    ''' </summary>
    Public Class PaginaBase
        Inherits System.Web.UI.Page

        Protected Overrides Sub OnInit(e As EventArgs)
            MyBase.OnInit(e)
            ' Vincula el ViewState al usuario: un postback forjado desde otra sesión no es válido.
            If User.Identity.IsAuthenticated Then ViewStateUserKey = User.Identity.Name
        End Sub

        Protected ReadOnly Property UsuarioId As Integer
            Get
                Return Integer.Parse(User.Identity.Name, CultureInfo.InvariantCulture)
            End Get
        End Property

        ''' <summary>Texto recortado y limitado a la longitud de la columna.</summary>
        Protected Shared Function Texto(valor As String, longitudMaxima As Integer) As String
            Dim limpio = If(valor, "").Trim()
            Return If(limpio.Length > longitudMaxima, limpio.Substring(0, longitudMaxima), limpio)
        End Function

        Protected Shared Function NormalizarEmail(valor As String) As String
            Return If(valor, "").Trim().ToLowerInvariant()
        End Function

    End Class

End Namespace
