Namespace Models

    ''' <summary>
    ''' Criterios del buscador de tareas. Las fechas "Desde/Hasta" son días locales completos
    ''' (Hasta incluido). Los valores vacíos o Nothing no filtran.
    ''' </summary>
    Public Class FiltroBusqueda
        ' Valores especiales de Asignado (si no, es el Id del usuario)
        Public Const AsignadoYo As String = "yo"
        Public Const AsignadoNadie As String = "nadie"

        ' EstadoFecha
        Public Const FechaVencidas As String = "vencidas"
        Public Const FechaHoy As String = "hoy"
        Public Const FechaSemana As String = "semana"
        Public Const FechaSin As String = "sin"
        Public Const FechaCon As String = "con"

        ' Subtareas
        Public Const SubtareasSin As String = "sin"
        Public Const SubtareasPendientes As String = "pendientes"
        Public Const SubtareasCompletas As String = "completas"

        ' Orden (lista blanca; cualquier otro valor usa OrdenModificada)
        Public Const OrdenModificada As String = "modificada"
        Public Const OrdenCreada As String = "creada"
        Public Const OrdenLimite As String = "limite"
        Public Const OrdenTitulo As String = "titulo"
        Public Const OrdenCaso As String = "caso"
        Public Const OrdenTablero As String = "tablero"

        ' Texto: todas las palabras deben aparecer en alguno de los campos elegidos.
        Public Property Texto As String
        Public Property EnTitulo As Boolean = True        ' incluye el número de caso
        Public Property EnDescripcion As Boolean = True
        Public Property EnCodigo As Boolean
        Public Property EnComentarios As Boolean
        Public Property EnSubtareas As Boolean

        Public Property TableroId As Integer?
        Public Property ListaId As Integer?
        Public Property Asignado As String
        Public Property EstadoFecha As String
        Public Property Caso As String

        Public Property LimiteDesde As DateTime?
        Public Property LimiteHasta As DateTime?
        Public Property CreadaDesde As DateTime?
        Public Property CreadaHasta As DateTime?
        Public Property ModificadaDesde As DateTime?
        Public Property ModificadaHasta As DateTime?

        ''' <summary>Tarjetas en cuyo historial aparece este usuario.</summary>
        Public Property CambiadaPor As Integer?
        Public Property Subtareas As String
        Public Property ConCodigo As Boolean?
        Public Property Orden As String = OrdenModificada
    End Class

    ''' <summary>Una tarjeta encontrada por el buscador, con su tablero.</summary>
    Public Class TarjetaEncontrada
        Public Property Tarjeta As Tarjeta
        Public Property TableroId As Integer
        Public Property NombreTablero As String
    End Class

    Public Class ResultadoBusqueda
        Public Property Tarjetas As New List(Of TarjetaEncontrada)

        ''' <summary>Total de coincidencias (puede ser mayor que Tarjetas.Count si se ha limitado).</summary>
        Public Property Total As Integer
    End Class

End Namespace
