Namespace Models

    ''' <summary>Un cambio en una tarjeta: quién, cuándo y qué (con valor anterior y nuevo).</summary>
    Public Class HistorialTarjeta
        ' Acciones
        Public Const Creada As String = "Creada"
        Public Const Editada As String = "Editada"
        Public Const Movida As String = "Movida"
        Public Const SubtareaAgregada As String = "SubtareaAgregada"
        Public Const SubtareaCompletada As String = "SubtareaCompletada"
        Public Const SubtareaReabierta As String = "SubtareaReabierta"
        Public Const SubtareaEliminada As String = "SubtareaEliminada"
        Public Const Comentada As String = "Comentario"
        Public Const AdjuntoAgregado As String = "AdjuntoAgregado"    ' valor nuevo = nombre del archivo
        Public Const AdjuntoEliminado As String = "AdjuntoEliminado"  ' valor nuevo = nombre del archivo
        ' Anteriores a los adjuntos (solo fotos); se siguen mostrando en el historial.
        Public Const FotoAgregada As String = "FotoAgregada"
        Public Const FotoEliminada As String = "FotoEliminada"

        ' Campos (solo con Accion = Editada)
        Public Const CampoTitulo As String = "Titulo"
        Public Const CampoCaso As String = "Caso"
        Public Const CampoDescripcion As String = "Descripcion"
        Public Const CampoFechaLimite As String = "FechaLimite"   ' valores en formato yyyy-MM-dd
        Public Const CampoAsignado As String = "AsignadoA"       ' valores = nombre del usuario
        Public Const CampoCodigo As String = "Codigo"
        Public Const CampoPrioridad As String = "Prioridad"     ' valores = nombre ("Alta", "Media", "Baja")
        Public Const CampoEtiquetas As String = "Etiquetas"     ' valores = "#A #B" (ordenadas por nombre)

        Public Property Id As Integer
        Public Property TarjetaId As Integer
        Public Property UsuarioId As Integer
        Public Property NombreUsuario As String

        ''' <summary>Fecha en UTC.</summary>
        Public Property Fecha As DateTime
        Public Property Accion As String
        Public Property Campo As String
        Public Property ValorAnterior As String
        Public Property ValorNuevo As String

        Public Shared Function Nuevo(accion As String, Optional campo As String = Nothing,
                                     Optional valorAnterior As String = Nothing, Optional valorNuevo As String = Nothing) As HistorialTarjeta
            Return New HistorialTarjeta With {.Accion = accion, .Campo = campo, .ValorAnterior = valorAnterior, .ValorNuevo = valorNuevo}
        End Function
    End Class

End Namespace
