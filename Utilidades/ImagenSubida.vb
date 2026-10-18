Imports System.IO

Namespace Utilidades

    ''' <summary>
    ''' Comprobaciones comunes de las imágenes que suben los usuarios (fondos de tablero y fotos de tarjeta):
    ''' formato por la firma del contenido y carpeta configurada en Web.config.
    ''' </summary>
    Friend NotInheritable Class ImagenSubida

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Extensión según la firma del contenido (jpg, png, gif, webp) o Nothing si no es uno de esos formatos.
        ''' Se mira el contenido, no la extensión ni el tipo que envía el navegador. SVG no: puede llevar scripts.
        ''' </summary>
        Public Shared Function DetectarExtension(b As Byte()) As String
            If b Is Nothing OrElse b.Length < 12 Then Return Nothing
            If b(0) = &HFF AndAlso b(1) = &HD8 AndAlso b(2) = &HFF Then Return "jpg"
            If b(0) = &H89 AndAlso b(1) = &H50 AndAlso b(2) = &H4E AndAlso b(3) = &H47 AndAlso
               b(4) = &HD AndAlso b(5) = &HA AndAlso b(6) = &H1A AndAlso b(7) = &HA Then Return "png"
            If b(0) = &H47 AndAlso b(1) = &H49 AndAlso b(2) = &H46 AndAlso b(3) = &H38 Then Return "gif"
            If b(0) = &H52 AndAlso b(1) = &H49 AndAlso b(2) = &H46 AndAlso b(3) = &H46 AndAlso
               b(8) = &H57 AndAlso b(9) = &H45 AndAlso b(10) = &H42 AndAlso b(11) = &H50 Then Return "webp"
            Return Nothing
        End Function

        Public Shared Function TipoMime(nombre As String) As String
            Select Case Path.GetExtension(nombre)
                Case ".png" : Return "image/png"
                Case ".gif" : Return "image/gif"
                Case ".webp" : Return "image/webp"
                Case Else : Return "image/jpeg"
            End Select
        End Function

        ''' <summary>
        ''' Carpeta física de appSettings <paramref name="clave"/>: ruta absoluta o virtual "~/...". Si falta o sigue con
        ''' el marcador de publicación ([...]), lanza un error que dice qué configurar.
        ''' </summary>
        Public Shared Function Carpeta(clave As String, contenido As String, ejemploAbsoluta As String, ejemploVirtual As String) As String
            Dim ruta = ConfigurationManager.AppSettings(clave)
            If String.IsNullOrWhiteSpace(ruta) Then
                Throw New ConfigurationErrorsException(
                    "Falta appSettings '" & clave & "' en Web.config con la carpeta de " & contenido & ".")
            End If
            ruta = ruta.Trim()
            If ruta.StartsWith("~/", StringComparison.Ordinal) Then Return HttpContext.Current.Server.MapPath(ruta)
            ' Una ruta relativa (o el marcador sin sustituir) acabaría en la carpeta del proceso de IIS.
            If Not Path.IsPathRooted(ruta) OrElse ruta.Contains("[") Then
                Throw New ConfigurationErrorsException(
                    "Configura '" & clave & "' en Web.config con una ruta absoluta (p. ej. " & ejemploAbsoluta & ") o " & ejemploVirtual & ".")
            End If
            Return ruta
        End Function

        ''' <summary>Borra el archivo si existe; un fallo no impide seguir (queda huérfano, ya no lo referencia nada).</summary>
        Public Shared Sub BorrarArchivo(ruta As String)
            If ruta Is Nothing Then Return
            Try
                File.Delete(ruta)
            Catch ex As IOException
            Catch ex As UnauthorizedAccessException
            End Try
        End Sub

    End Class

End Namespace
