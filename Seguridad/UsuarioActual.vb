Imports System.Globalization
Imports TaskManager.Datos
Imports TaskManager.Models

Namespace Seguridad

    ''' <summary>
    ''' Perfil del usuario autenticado (nombre y preferencias), leído de la base de datos una sola vez
    ''' por petición. Así un cambio en «Mi perfil» se ve al momento en todas las sesiones.
    ''' </summary>
    Public NotInheritable Class UsuarioActual

        Private Const ClaveCache As String = "TaskManager.UsuarioActual"

        Private Sub New()
        End Sub

        ''' <summary>Perfil del usuario de la petición; Nothing sin sesión o si la cuenta ya no existe.</summary>
        Public Shared Function Perfil() As Usuario
            Dim contexto = HttpContext.Current
            If contexto?.User Is Nothing OrElse Not contexto.User.Identity.IsAuthenticated Then Return Nothing
            If contexto.Items.Contains(ClaveCache) Then Return DirectCast(contexto.Items(ClaveCache), Usuario)

            Dim usuarioId As Integer
            Dim usuario As Usuario = Nothing
            If Integer.TryParse(contexto.User.Identity.Name, NumberStyles.None, CultureInfo.InvariantCulture, usuarioId) Then
                usuario = UsuariosDatos.ObtenerPorId(usuarioId)
            End If
            contexto.Items(ClaveCache) = usuario
            Return usuario
        End Function

        ''' <summary>Formato de fecha elegido por el usuario (el predeterminado sin sesión).</summary>
        Public Shared Function FormatoFecha() As FormatoFecha
            Return Models.FormatoFecha.Obtener(Perfil()?.FormatoFecha)
        End Function

        ''' <summary>Descarta el perfil leído en esta petición (tras guardarlo, para mostrar los datos nuevos).</summary>
        Public Shared Sub Olvidar()
            HttpContext.Current?.Items.Remove(ClaveCache)
        End Sub

    End Class

End Namespace
