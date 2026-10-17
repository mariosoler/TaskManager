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

        ''' <summary>Tableros.Fondo de un fondo predeterminado: "predef:&lt;clave&gt;" (si no, es un archivo subido).</summary>
        Public Const PrefijoPredefinido As String = "predef:"

        ''' <summary>
        ''' Fondos predeterminados: degradados de color y fotos genéricas de Content/fondos (ver LICENCIA.txt).
        ''' Lista blanca: solo se aceptan estas claves, y el CSS sale de aquí, nunca del usuario.
        ''' </summary>
        Public Shared ReadOnly Predefinidos As IReadOnlyList(Of FondoPredefinido) = New List(Of FondoPredefinido) From {
            FondoPredefinido.Color("evergreen", "Evergreen", "#004746", "#287f82"),
            FondoPredefinido.Color("oceano", "Océano", "#0b3d91", "#1f8fd6"),
            FondoPredefinido.Color("lavanda", "Lavanda", "#3f2b96", "#a06cd5"),
            FondoPredefinido.Color("naranja", "Naranja", "#b23a1d", "#f39c34"),
            FondoPredefinido.Color("frambuesa", "Frambuesa", "#8e1d55", "#e2557c"),
            FondoPredefinido.Color("pizarra", "Pizarra", "#1f2933", "#52606d"),
            FondoPredefinido.Color("pradera", "Pradera", "#2f5d1e", "#7fb33a"),
            FondoPredefinido.Foto("bosque-lago", "Bosque y lago"),
            FondoPredefinido.Foto("bosque", "Bosque"),
            FondoPredefinido.Foto("montanas", "Montañas"),
            FondoPredefinido.Foto("cascada", "Cascada"),
            FondoPredefinido.Foto("atardecer", "Atardecer"),
            FondoPredefinido.Foto("desierto", "Desierto"),
            FondoPredefinido.Foto("costa", "Costa"),
            FondoPredefinido.Foto("ciudad-noche", "Ciudad de noche")
        }.AsReadOnly()

        ''' <summary>Fondo predeterminado guardado en Tableros.Fondo, o Nothing si no es uno de la lista.</summary>
        Public Shared Function BuscarPredefinido(fondo As String) As FondoPredefinido
            If fondo Is Nothing OrElse Not fondo.StartsWith(PrefijoPredefinido, StringComparison.Ordinal) Then Return Nothing
            Dim clave = fondo.Substring(PrefijoPredefinido.Length)
            Return Predefinidos.FirstOrDefault(Function(f) f.Clave = clave)
        End Function

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
            If ruta.StartsWith("~/", StringComparison.Ordinal) Then Return HttpContext.Current.Server.MapPath(ruta)
            ' Una ruta relativa (o el marcador [RUTA_CARPETA_FONDOS] sin sustituir) acabaría en la carpeta del proceso de IIS.
            If Not Path.IsPathRooted(ruta) OrElse ruta.Contains("[") Then
                Throw New ConfigurationErrorsException(
                    "Configura '" & ClaveConfiguracion & "' en Web.config con una ruta absoluta (p. ej. D:\Datos\TaskManager\Fondos) o ~/App_Data/Fondos.")
            End If
            Return ruta
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

        ''' <summary>
        ''' Valor CSS de background-image del fondo (predeterminado o subido), o vacío si no tiene.
        ''' Con <paramref name="miniatura"/>, las fotos predeterminadas usan su versión reducida (fichas de "Mis tableros").
        ''' Todo lo genera el código (solo caracteres seguros).
        ''' </summary>
        Public Shared Function ImagenCss(tableroId As Integer, nombre As String, Optional miniatura As Boolean = False) As String
            Dim predefinido = BuscarPredefinido(nombre)
            If predefinido IsNot Nothing Then Return predefinido.ImagenCss(miniatura)
            Dim direccion = Url(tableroId, nombre)
            Return If(direccion.Length = 0, "", "url('" & direccion & "')")
        End Function

        ''' <summary>Atributo style con el fondo, o vacío.</summary>
        Public Shared Function Estilo(tableroId As Integer, nombre As String, Optional miniatura As Boolean = False) As String
            Dim imagen = ImagenCss(tableroId, nombre, miniatura)
            Return If(imagen.Length = 0, "", "background-image: " & imagen)
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

    ''' <summary>Fondo predeterminado: un degradado de dos colores o una foto de Content/fondos.</summary>
    Public NotInheritable Class FondoPredefinido

        Public ReadOnly Property Clave As String
        Public ReadOnly Property Nombre As String
        Public ReadOnly Property EsFoto As Boolean
        Private ReadOnly _degradado As String

        Private Sub New(clave As String, nombre As String, esFoto As Boolean, degradado As String)
            Me.Clave = clave
            Me.Nombre = nombre
            Me.EsFoto = esFoto
            _degradado = degradado
        End Sub

        Friend Shared Function Color(clave As String, nombre As String, desde As String, hasta As String) As FondoPredefinido
            Return New FondoPredefinido(clave, nombre, False, "linear-gradient(135deg, " & desde & ", " & hasta & ")")
        End Function

        Friend Shared Function Foto(clave As String, nombre As String) As FondoPredefinido
            Return New FondoPredefinido(clave, nombre, True, Nothing)
        End Function

        ''' <summary>Valor que se guarda en Tableros.Fondo.</summary>
        Public ReadOnly Property Valor As String
            Get
                Return FondosTablero.PrefijoPredefinido & Clave
            End Get
        End Property

        ''' <summary>Valor CSS de background-image.</summary>
        Public Function ImagenCss(Optional miniatura As Boolean = False) As String
            If Not EsFoto Then Return _degradado
            Return "url('" & VirtualPathUtility.ToAbsolute("~/Content/fondos/" & Clave & If(miniatura, "-mini", "") & ".jpg") & "')"
        End Function

    End Class

End Namespace
