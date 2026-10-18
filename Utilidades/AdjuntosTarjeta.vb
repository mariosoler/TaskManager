Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions

Namespace Utilidades

    ''' <summary>
    ''' Archivos adjuntos de las tarjetas (de cualquier tipo). Se guardan en la carpeta indicada en Web.config
    ''' (appSettings "RutaAdjuntosTarjetas": ruta absoluta o virtual "~/...") con un nombre generado por el código
    ''' (nunca el del usuario) y se sirven con Adjuntos.ashx, que comprueba el acceso al tablero. Las imágenes
    ''' (JPG, PNG, GIF o WebP, comprobadas por la firma del contenido) se guardan con su extensión y se muestran en
    ''' miniatura; el resto se guarda como .bin y siempre se descarga (nunca se abre en el navegador).
    ''' </summary>
    Public NotInheritable Class AdjuntosTarjeta

        Public Const ClaveConfiguracion As String = "RutaAdjuntosTarjetas"
        Public Const TamanoMaximo As Integer = 10 * 1024 * 1024
        Public Const MaximoPorTarjeta As Integer = 30
        Public Const LongitudMaximaNombre As Integer = 255

        ' Nombres que genera Guardar: <tarjetaId>_<guid>.<ext>. Cualquier otro se rechaza (evita rutas ../).
        Private Shared ReadOnly NombreValido As New Regex("^[0-9]+_[0-9a-f]{32}\.(jpg|png|gif|webp|bin)$")

        Private Sub New()
        End Sub

        Public Shared Function Carpeta() As String
            Return ImagenSubida.Carpeta(ClaveConfiguracion, "los adjuntos de las tarjetas",
                                        "D:\Datos\TaskManager\Adjuntos", "~/App_Data/Adjuntos")
        End Function

        ''' <summary>
        ''' Guarda el archivo y devuelve el nombre generado, o Nothing si está vacío o pasa del tamaño máximo.
        ''' En <paramref name="esImagen"/> indica si es una imagen que se puede mostrar (por la firma del contenido).
        ''' </summary>
        Public Shared Function Guardar(tarjetaId As Integer, contenido As Byte(), ByRef esImagen As Boolean) As String
            esImagen = False
            If contenido Is Nothing OrElse contenido.Length = 0 OrElse contenido.Length > TamanoMaximo Then Return Nothing
            Dim extension = ImagenSubida.DetectarExtension(contenido)
            esImagen = extension IsNot Nothing

            Dim nombre = tarjetaId.ToString(CultureInfo.InvariantCulture) & "_" & Guid.NewGuid().ToString("N") & "." &
                         If(extension, "bin")
            Dim carpetaAdjuntos = Carpeta()
            Directory.CreateDirectory(carpetaAdjuntos)
            File.WriteAllBytes(Path.Combine(carpetaAdjuntos, nombre), contenido)
            Return nombre
        End Function

        ''' <summary>Ruta física del adjunto, o Nothing si el nombre no es válido o el archivo no existe.</summary>
        Public Shared Function RutaArchivo(nombre As String) As String
            If nombre Is Nothing OrElse Not NombreValido.IsMatch(nombre) Then Return Nothing
            Dim ruta = Path.Combine(Carpeta(), nombre)
            Return If(File.Exists(ruta), ruta, Nothing)
        End Function

        ''' <summary>Borra los archivos (de adjuntos ya quitados de la base de datos: al eliminarlos o al borrar su tarjeta).</summary>
        Public Shared Sub EliminarArchivos(nombres As IEnumerable(Of String))
            If nombres Is Nothing Then Return
            For Each nombre In nombres
                ImagenSubida.BorrarArchivo(RutaArchivo(nombre))
            Next
        End Sub

        ''' <summary>
        ''' Nombre original limpio para guardarlo y mostrarlo: sin ruta (algunos navegadores la envían), sin caracteres
        ''' de control y como mucho <see cref="LongitudMaximaNombre"/> caracteres. Solo se muestra codificado y como
        ''' nombre de descarga; nunca se usa para construir rutas.
        ''' </summary>
        Public Shared Function LimpiarNombre(nombre As String) As String
            Dim limpio = If(nombre, "")
            Dim barra = Math.Max(limpio.LastIndexOf("\"c), limpio.LastIndexOf("/"c))
            If barra >= 0 Then limpio = limpio.Substring(barra + 1)
            limpio = New String(limpio.Where(Function(c) Not Char.IsControl(c)).ToArray()).Trim()
            If limpio.Length = 0 Then limpio = "archivo"
            If limpio.Length > LongitudMaximaNombre Then
                Dim extension = Path.GetExtension(limpio)
                If extension.Length > 20 Then extension = ""
                limpio = limpio.Substring(0, LongitudMaximaNombre - extension.Length) & extension
            End If
            Return limpio
        End Function

        ''' <summary>
        ''' Cabecera Content-Disposition con el nombre original: versión ASCII (sin comillas ni barras) y versión UTF-8
        ''' codificada (RFC 5987) para los navegadores actuales.
        ''' </summary>
        Public Shared Function ContentDisposition(tipo As String, nombre As String) As String
            Dim ascii As New StringBuilder()
            For Each c In nombre
                ascii.Append(If(c < ChrW(32) OrElse c > ChrW(126) OrElse c = """"c OrElse c = "\"c OrElse c = "/"c, "_"c, c))
            Next
            Return tipo & "; filename=""" & ascii.ToString() & """; filename*=UTF-8''" & Uri.EscapeDataString(nombre)
        End Function

        Public Shared Function TipoMimeImagen(nombre As String) As String
            Return ImagenSubida.TipoMime(nombre)
        End Function

        ''' <summary>URL del adjunto (Adjuntos.ashx comprueba el acceso al servirlo).</summary>
        Public Shared Function Url(adjuntoId As Integer) As String
            Return VirtualPathUtility.ToAbsolute("~/Adjuntos.ashx") & "?id=" & adjuntoId.ToString(CultureInfo.InvariantCulture)
        End Function

    End Class

End Namespace
