Imports System.Globalization

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

        ' Prioridad: "1", "2", "3" (Models.Prioridad) o PrioridadSin
        Public Const PrioridadSin As String = "sin"

        ' Subtareas
        Public Const SubtareasSin As String = "sin"
        Public Const SubtareasPendientes As String = "pendientes"
        Public Const SubtareasCompletas As String = "completas"

        ' Orden (lista blanca; cualquier otro valor usa OrdenModificada)
        Public Const OrdenModificada As String = "modificada"
        Public Const OrdenCreada As String = "creada"
        Public Const OrdenLimite As String = "limite"
        Public Const OrdenPrioridad As String = "prioridad"
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
        Public Property Prioridad As String
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

        ' ----- Serialización para las búsquedas guardadas -----
        ' Formato de cadena de consulta (clave=valor&...) con claves fijas; los valores vacíos no se guardan.
        ' Al leer, los valores solo se interpretan (números, fechas yyyy-MM-dd, constantes): nunca se usan en SQL
        ' sin pasar por BusquedaDatos, que los trata como parámetros o contra listas blancas.

        ''' <summary>Serializa el filtro para guardarlo en BusquedasGuardadas.Criterios.</summary>
        Public Function ACriterios() As String
            Dim valores As New List(Of KeyValuePair(Of String, String))
            Dim agregar = Sub(clave As String, valor As String)
                              If Not String.IsNullOrEmpty(valor) Then valores.Add(New KeyValuePair(Of String, String)(clave, valor))
                          End Sub
            Dim fecha = Function(f As DateTime?) If(f.HasValue, f.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Nothing)
            Dim entero = Function(n As Integer?) If(n.HasValue, n.Value.ToString(CultureInfo.InvariantCulture), Nothing)
            Dim logico = Function(b As Boolean) If(b, "1", "0")

            agregar("texto", Texto)
            agregar("enTitulo", logico(EnTitulo))
            agregar("enDescripcion", logico(EnDescripcion))
            agregar("enCodigo", logico(EnCodigo))
            agregar("enComentarios", logico(EnComentarios))
            agregar("enSubtareas", logico(EnSubtareas))
            agregar("tablero", entero(TableroId))
            agregar("lista", entero(ListaId))
            agregar("asignado", Asignado)
            agregar("estadoFecha", EstadoFecha)
            agregar("prioridad", Prioridad)
            agregar("caso", Caso)
            agregar("limiteDesde", fecha(LimiteDesde))
            agregar("limiteHasta", fecha(LimiteHasta))
            agregar("creadaDesde", fecha(CreadaDesde))
            agregar("creadaHasta", fecha(CreadaHasta))
            agregar("modificadaDesde", fecha(ModificadaDesde))
            agregar("modificadaHasta", fecha(ModificadaHasta))
            agregar("cambiadaPor", entero(CambiadaPor))
            agregar("subtareas", Subtareas)
            agregar("codigo", If(ConCodigo.HasValue, logico(ConCodigo.Value), Nothing))
            agregar("orden", Orden)

            Return String.Join("&", valores.Select(Function(v) v.Key & "=" & HttpUtility.UrlEncode(v.Value)))
        End Function

        ''' <summary>Reconstruye un filtro guardado con ACriterios. Las claves desconocidas se ignoran.</summary>
        Public Shared Function DesdeCriterios(criterios As String) As FiltroBusqueda
            Dim v = HttpUtility.ParseQueryString(If(criterios, ""))
            Dim fecha = Function(clave As String) As DateTime?
                            Dim f As DateTime
                            If DateTime.TryParseExact(v(clave), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, f) Then Return f
                            Return Nothing
                        End Function
            Dim entero = Function(clave As String) As Integer?
                             Dim n As Integer
                             If Integer.TryParse(v(clave), NumberStyles.None, CultureInfo.InvariantCulture, n) Then Return n
                             Return Nothing
                         End Function
            Dim logico = Function(clave As String, porDefecto As Boolean) If(v(clave) Is Nothing, porDefecto, v(clave) = "1")

            Return New FiltroBusqueda With {
                .Texto = v("texto"),
                .EnTitulo = logico("enTitulo", True),
                .EnDescripcion = logico("enDescripcion", True),
                .EnCodigo = logico("enCodigo", False),
                .EnComentarios = logico("enComentarios", False),
                .EnSubtareas = logico("enSubtareas", False),
                .TableroId = entero("tablero"),
                .ListaId = entero("lista"),
                .Asignado = v("asignado"),
                .EstadoFecha = v("estadoFecha"),
                .Prioridad = v("prioridad"),
                .Caso = v("caso"),
                .LimiteDesde = fecha("limiteDesde"),
                .LimiteHasta = fecha("limiteHasta"),
                .CreadaDesde = fecha("creadaDesde"),
                .CreadaHasta = fecha("creadaHasta"),
                .ModificadaDesde = fecha("modificadaDesde"),
                .ModificadaHasta = fecha("modificadaHasta"),
                .CambiadaPor = entero("cambiadaPor"),
                .Subtareas = v("subtareas"),
                .ConCodigo = If(v("codigo") Is Nothing, CType(Nothing, Boolean?), v("codigo") = "1"),
                .Orden = If(v("orden"), OrdenModificada)
            }
        End Function
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
