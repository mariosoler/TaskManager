Imports System.Globalization
Imports System.IO
Imports System.Text.RegularExpressions

Namespace Utilidades

    ''' <summary>
    ''' Imágenes de fondo de los tableros. Se guardan en la carpeta indicada en Web.config
    ''' (appSettings "RutaFondosTableros": ruta absoluta o virtual "~/...") y se sirven con Fondo.ashx,
    ''' que comprueba el acceso al tablero. El nombre del archivo lo genera el código, nunca el usuario.
    ''' </summary>
    Public NotInheritable Class FondosTablero

        Public Const ClaveConfiguracion As String = "RutaFondosTableros"
        Public Const TamanoMaximo As Integer = 5 * 1024 * 1024

        ' Nombres que genera Guardar: <tableroId>_<guid>.<ext>. Cualquier otro se rechaza (evita rutas ../).
        Private Shared ReadOnly NombreValido As New Regex("^[0-9]+_[0-9a-f]{32}\.(jpg|png|gif|webp)$")

        Private Sub New()
        End Sub

        Public Shared Function Carpeta() As String
            Dim ruta = ConfigurationManager.AppSettings(ClaveConfiguracion)
            If String.IsNullOrWhiteSpace(ruta) Then
                Throw New ConfigurationErrorsException(
                    "Falta appSettings '" & ClaveConfiguracion & "' en Web.config con la carpeta de los fondos de tablero.")
            End If
            ruta = ruta.Trim()
            Return If(ruta.StartsWith("~/", StringComparison.Ordinal), HttpContext.Current.Server.MapPath(ruta), ruta)
        End Function

        ''' <summary>
        ''' Guarda la imagen y devuelve el nombre de archivo generado, o Nothing si no es una imagen
        ''' JPEG, PNG, GIF o WebP (se comprueba el contenido, no la extensión ni el tipo que envía el navegador).
        ''' </summary>
        Public Shared Function Guardar(tableroId As Integer, contenido As Byte()) As String
            Dim extension = DetectarExtension(contenido)
            If extension Is Nothing OrElse contenido.Length > TamanoMaximo Then Return Nothing

            Dim nombre = tableroId.ToString(CultureInfo.InvariantCulture) & "_" & Guid.NewGuid().ToString("N") & "." & extension
            Dim carpetaFondos = Carpeta()
            Directory.CreateDirectory(carpetaFondos)
            File.WriteAllBytes(Path.Combine(carpetaFondos, nombre), contenido)
            Return nombre
        End Function

        ''' <summary>Ruta física del fondo, o Nothing si el nombre no es válido o el archivo no existe.</summary>
        Public Shared Function RutaArchivo(nombre As String) As String
            If nombre Is Nothing OrElse Not NombreValido.IsMatch(nombre) Then Return Nothing
            Dim ruta = Path.Combine(Carpeta(), nombre)
            Return If(File.Exists(ruta), ruta, Nothing)
        End Function

        Public Shared Sub Eliminar(nombre As String)
            Dim ruta = RutaArchivo(nombre)
            If ruta Is Nothing Then Return
            Try
                File.Delete(ruta)
            Catch ex As IOException
                ' Un archivo huérfano no impide seguir; ya no lo referencia ningún tablero.
            Catch ex As UnauthorizedAccessException
            End Try
        End Sub

        Public Shared Function TipoMime(nombre As String) As String
            Select Case Path.GetExtension(nombre)
                Case ".png" : Return "image/png"
                Case ".gif" : Return "image/gif"
                Case ".webp" : Return "image/webp"
                Case Else : Return "image/jpeg"
            End Select
        End Function

        ''' <summary>URL del fondo (Fondo.ashx); vacía si el tablero no tiene. El nombre en "v" invalida la caché al cambiarlo.</summary>
        Public Shared Function Url(tableroId As Integer, nombre As String) As String
            If String.IsNullOrEmpty(nombre) Then Return ""
            Return VirtualPathUtility.ToAbsolute("~/Fondo.ashx") & "?id=" & tableroId.ToString(CultureInfo.InvariantCulture) &
                   "&v=" & HttpUtility.UrlEncode(Path.GetFileNameWithoutExtension(nombre))
        End Function

        ''' <summary>Atributo style con el fondo, o vacío. La URL la genera el código (solo caracteres seguros).</summary>
        Public Shared Function Estilo(tableroId As Integer, nombre As String) As String
            Dim direccion = Url(tableroId, nombre)
            Return If(direccion.Length = 0, "", "background-image: url('" & direccion & "')")
        End Function

        ' Firma de los formatos admitidos. SVG no: puede llevar scripts.
        Private Shared Function DetectarExtension(b As Byte()) As String
            If b Is Nothing OrElse b.Length < 12 Then Return Nothing
            If b(0) = &HFF AndAlso b(1) = &HD8 AndAlso b(2) = &HFF Then Return "jpg"
            If b(0) = &H89 AndAlso b(1) = &H50 AndAlso b(2) = &H4E AndAlso b(3) = &H47 AndAlso
               b(4) = &HD AndAlso b(5) = &HA AndAlso b(6) = &H1A AndAlso b(7) = &HA Then Return "png"
            If b(0) = &H47 AndAlso b(1) = &H49 AndAlso b(2) = &H46 AndAlso b(3) = &H38 Then Return "gif"
            If b(0) = &H52 AndAlso b(1) = &H49 AndAlso b(2) = &H46 AndAlso b(3) = &H46 AndAlso
               b(8) = &H57 AndAlso b(9) = &H45 AndAlso b(10) = &H42 AndAlso b(11) = &H50 Then Return "webp"
            Return Nothing
        End Function

    End Class

End Namespace
