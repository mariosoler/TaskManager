Imports System.Globalization
Imports TaskManager.Seguridad

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

        ''' <summary>Etiqueta HTML de fecha límite (sin texto del usuario); en rojo si ya ha vencido.</summary>
        Public Shared Function HtmlFechaLimite(valor As Object) As String
            If Not TypeOf valor Is DateTime Then Return ""
            Dim fecha = CDate(valor)
            Dim clase = If(fecha.Date < DateTime.Today, "info info-fecha vencida", "info info-fecha")
            Return String.Format(CultureInfo.InvariantCulture, "<span class=""{0}"" title=""Fecha límite: {1}"">&#128197; {2}</span>",
                                 clase, HttpUtility.HtmlEncode(FechaDia(fecha)), HttpUtility.HtmlEncode(FechaDiaCorta(fecha)))
        End Function

        ''' <summary>Día sin hora en el formato de fecha del usuario ("17/10/2026"). Para fechas sin hora (no UTC).</summary>
        Public Shared Function FechaDia(fecha As DateTime) As String
            Return fecha.ToString(UsuarioActual.FormatoFecha().Larga, CultureInfo.CurrentCulture)
        End Function

        ''' <summary>Día sin año en el formato del usuario ("17 oct", "17/10"...).</summary>
        Public Shared Function FechaDiaCorta(fecha As DateTime) As String
            Return fecha.ToString(UsuarioActual.FormatoFecha().Corta, CultureInfo.CurrentCulture)
        End Function

        ''' <summary>Etiqueta HTML de prioridad: color + texto (el color nunca va solo). Vacía si no tiene.</summary>
        Public Shared Function HtmlPrioridad(valor As Object) As String
            If Not TypeOf valor Is Integer Then Return ""
            Dim p = CInt(valor)
            Dim nombre = Models.Prioridad.Nombre(p)
            If nombre Is Nothing Then Return ""
            Return String.Format(CultureInfo.InvariantCulture, "<span class=""etiqueta-prioridad {0}"" title=""Prioridad {1}"">{2}</span>",
                                 Models.Prioridad.ClaseCss(p), nombre.ToLowerInvariant(), nombre)
        End Function

        ''' <summary>
        ''' "Prioridad alta" solo para lectores de pantalla (en el tablero la prioridad se ve en la franja de color
        ''' del lateral de la tarjeta, y el color no puede ser la única forma de transmitirla). Vacía si no tiene.
        ''' </summary>
        Public Shared Function HtmlPrioridadLectores(valor As Object) As String
            If Not TypeOf valor Is Integer Then Return ""
            Dim nombre = Models.Prioridad.Nombre(CInt(valor))
            If nombre Is Nothing Then Return ""
            Return "<span class=""solo-lectores"">Prioridad " & nombre.ToLowerInvariant() & "</span>"
        End Function

        ''' <summary>Fecha guardada en UTC, en hora local del servidor y con el formato del usuario ("17/10/2026 14:32").</summary>
        Public Shared Function FechaLocal(valor As Object) As String
            If Not TypeOf valor Is DateTime Then Return ""
            Return ALocal(CDate(valor)).ToString(UsuarioActual.FormatoFecha().Larga & " HH:mm", CultureInfo.CurrentCulture)
        End Function

        ''' <summary>"Modificada el 17/10/2026 14:32 por Ana" (texto plano: codificar al mostrar).</summary>
        Public Shared Function TextoModificacion(fecha As Object, nombre As Object) As String
            Dim texto = "Modificada el " & FechaLocal(fecha)
            Dim quien = TryCast(nombre, String)
            Return If(String.IsNullOrEmpty(quien), texto, texto & " por " & quien)
        End Function

        ''' <summary>Fecha UTC en formato compacto: la hora si es de hoy ("14:32"); si no, el día sin año ("17 oct").</summary>
        Public Shared Function FechaCorta(valor As Object) As String
            If Not TypeOf valor Is DateTime Then Return ""
            Dim local = ALocal(CDate(valor))
            If local.Date = DateTime.Today Then Return local.ToString("HH:mm", CultureInfo.CurrentCulture)
            Return FechaDiaCorta(local)
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

        ''' <summary>
        ''' Versión de la aplicación ("0.9.0-beta"), tomada de AssemblyInformationalVersion en
        ''' My Project/AssemblyInfo.vb: es el único sitio donde hay que cambiarla.
        ''' </summary>
        Public Shared Function VersionApp() As String
            Dim ensamblado = GetType(Formato).Assembly
            Dim atributo = DirectCast(Attribute.GetCustomAttribute(ensamblado, GetType(System.Reflection.AssemblyInformationalVersionAttribute)),
                                      System.Reflection.AssemblyInformationalVersionAttribute)
            If atributo IsNot Nothing Then Return atributo.InformationalVersion
            Return ensamblado.GetName().Version.ToString(3)
        End Function

        Private Shared Function ALocal(utc As DateTime) As DateTime
            Return DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime()
        End Function

    End Class

End Namespace
