Imports System.Globalization

Namespace Utilidades

    ''' <summary>Utilidades de presentación compartidas por las páginas.</summary>
    Public NotInheritable Class Formato

        Private Sub New()
        End Sub

        ''' <summary>Hasta dos iniciales en mayúsculas ("Ana López" -> "AL").</summary>
        Public Shared Function Iniciales(nombre As Object) As String
            Dim texto = TryCast(nombre, String)
            If String.IsNullOrWhiteSpace(texto) Then Return ""
            Dim palabras = texto.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
            Return String.Concat(palabras.Take(2).Select(Function(p) Char.ToUpper(p(0), CultureInfo.CurrentCulture)))
        End Function

        ''' <summary>Convierte una fecha guardada en UTC a la hora local del servidor.</summary>
        Public Shared Function FechaLocal(valor As Object) As String
            If Not TypeOf valor Is DateTime Then Return ""
            Return ALocal(CDate(valor)).ToString("g", CultureInfo.CurrentCulture)
        End Function

        ''' <summary>Fecha UTC en formato compacto: la hora si es de hoy ("14:32"); si no, el día ("17 oct").</summary>
        Public Shared Function FechaCorta(valor As Object) As String
            If Not TypeOf valor Is DateTime Then Return ""
            Dim local = ALocal(CDate(valor))
            Return local.ToString(If(local.Date = DateTime.Today, "HH:mm", "d MMM"), CultureInfo.CurrentCulture)
        End Function

        ''' <summary>
        ''' URL de un recurso estático con su versión ("/Content/site.css?v=638...") para que el navegador
        ''' descargue de nuevo CSS/JS cuando cambian, en lugar de usar la copia en caché.
        ''' </summary>
        Public Shared Function UrlConVersion(rutaVirtual As String) As String
            Dim url = VirtualPathUtility.ToAbsolute(rutaVirtual)
            Dim archivo = HttpContext.Current.Server.MapPath(rutaVirtual)
            If Not System.IO.File.Exists(archivo) Then Return url
            Return url & "?v=" & System.IO.File.GetLastWriteTimeUtc(archivo).Ticks.ToString(CultureInfo.InvariantCulture)
        End Function

        Private Shared Function ALocal(utc As DateTime) As DateTime
            Return DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime()
        End Function

    End Class

End Namespace
