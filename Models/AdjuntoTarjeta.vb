Namespace Models

    ''' <summary>Archivo adjunto de una tarjeta (tabla AdjuntosTarjeta). El archivo está en la carpeta RutaAdjuntosTarjetas.</summary>
    Public Class AdjuntoTarjeta
        Public Property Id As Integer
        Public Property TarjetaId As Integer
        ''' <summary>Nombre del archivo generado por Utilidades.AdjuntosTarjeta.Guardar.</summary>
        Public Property Archivo As String
        ''' <summary>Nombre original (solo para mostrarlo, codificado, y como nombre de descarga).</summary>
        Public Property Nombre As String
        ''' <summary>Imagen JPG, PNG, GIF o WebP (comprobada por su contenido): se muestra en miniatura.</summary>
        Public Property EsImagen As Boolean
        Public Property Tamano As Integer
        Public Property SubidoPor As Integer
        Public Property NombreSubidoPor As String
        ''' <summary>Fecha en UTC.</summary>
        Public Property FechaSubida As DateTime
    End Class

End Namespace
