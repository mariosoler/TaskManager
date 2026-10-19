Imports System.Data.SqlClient
Imports System.Globalization
Imports TaskManager.Models

Namespace Datos

    ''' <summary>
    ''' Buscador de tarjetas en todos los tableros a los que tiene acceso el usuario.
    ''' La consulta se compone con fragmentos SQL FIJOS de este archivo; los valores del usuario
    ''' van siempre como parámetros (los nombres de parámetro los genera el código, nunca el usuario).
    ''' </summary>
    Public NotInheritable Class BusquedaDatos

        Private Const MaxPalabras As Integer = 10

        ' Orden: lista blanca de expresiones fijas.
        Private Shared ReadOnly Ordenes As New Dictionary(Of String, String) From {
            {FiltroBusqueda.OrdenModificada, "c.FechaModificacion DESC, c.Id DESC"},
            {FiltroBusqueda.OrdenCreada, "c.FechaCreacion DESC, c.Id DESC"},
            {FiltroBusqueda.OrdenLimite, "CASE WHEN c.FechaLimite IS NULL THEN 1 ELSE 0 END, c.FechaLimite, c.Id"},
            {FiltroBusqueda.OrdenPrioridad, "ISNULL(c.Prioridad, 4), CASE WHEN c.FechaLimite IS NULL THEN 1 ELSE 0 END, c.FechaLimite, c.Id"},
            {FiltroBusqueda.OrdenTitulo, "c.Titulo, c.Id"},
            {FiltroBusqueda.OrdenCaso, "CASE WHEN c.Caso IS NULL THEN 1 ELSE 0 END, LEN(c.Caso), c.Caso, c.Id"},
            {FiltroBusqueda.OrdenTablero, "t.Nombre, t.Id, l.Posicion, c.Posicion"}
        }

        Private Sub New()
        End Sub

        Public Shared Function Buscar(usuarioId As Integer, filtro As FiltroBusqueda, maximo As Integer) As ResultadoBusqueda
            Dim condiciones As New List(Of String)
            Using cn = Db.CrearConexion(), cmd As New SqlCommand()
                cmd.Connection = cn
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cmd.Parameters.Add("@Max", SqlDbType.Int).Value = maximo

                AgregarTexto(filtro, cmd, condiciones)

                If filtro.TableroId.HasValue Then
                    condiciones.Add("t.Id = @TableroId")
                    cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = filtro.TableroId.Value
                End If
                ' Varias listas: alguna de ellas. Nombres de parámetro generados por el código (@Lista0, @Lista1...).
                Dim listas = If(filtro.ListaIds, New List(Of Integer)).Distinct().Take(FiltroBusqueda.MaximoListas).ToList()
                If listas.Count > 0 Then
                    Dim parametrosLista As New List(Of String)
                    For i = 0 To listas.Count - 1
                        Dim p = "@Lista" & i.ToString(CultureInfo.InvariantCulture)
                        cmd.Parameters.Add(p, SqlDbType.Int).Value = listas(i)
                        parametrosLista.Add(p)
                    Next
                    condiciones.Add("c.ListaId IN (" & String.Join(", ", parametrosLista) & ")")
                End If

                Dim asignado As Integer
                Select Case filtro.Asignado
                    Case FiltroBusqueda.AsignadoYo
                        condiciones.Add("c.AsignadoA = @UsuarioId")
                    Case FiltroBusqueda.AsignadoNadie
                        condiciones.Add("c.AsignadoA IS NULL")
                    Case Else
                        If Integer.TryParse(filtro.Asignado, NumberStyles.None, CultureInfo.InvariantCulture, asignado) Then
                            condiciones.Add("c.AsignadoA = @Asignado")
                            cmd.Parameters.Add("@Asignado", SqlDbType.Int).Value = asignado
                        End If
                End Select

                ' FechaLimite guarda un día (sin hora), comparable con la fecha local de hoy.
                Dim hoy = DateTime.Today
                Select Case filtro.EstadoFecha
                    Case FiltroBusqueda.FechaVencidas
                        condiciones.Add("c.FechaLimite < @Hoy")
                    Case FiltroBusqueda.FechaHoy
                        condiciones.Add("c.FechaLimite = @Hoy")
                    Case FiltroBusqueda.FechaSemana
                        condiciones.Add("c.FechaLimite >= @Hoy AND c.FechaLimite < @FinSemana")   ' hoy y los 7 días siguientes
                    Case FiltroBusqueda.FechaSin
                        condiciones.Add("c.FechaLimite IS NULL")
                    Case FiltroBusqueda.FechaCon
                        condiciones.Add("c.FechaLimite IS NOT NULL")
                End Select
                cmd.Parameters.Add("@Hoy", SqlDbType.DateTime2).Value = hoy
                cmd.Parameters.Add("@FinSemana", SqlDbType.DateTime2).Value = hoy.AddDays(8)

                Dim prioridad As Integer
                If filtro.Prioridad = FiltroBusqueda.PrioridadSin Then
                    condiciones.Add("c.Prioridad IS NULL")
                ElseIf Integer.TryParse(filtro.Prioridad, NumberStyles.None, CultureInfo.InvariantCulture, prioridad) AndAlso
                       Models.Prioridad.EsValida(prioridad) Then
                    condiciones.Add("c.Prioridad = @Prioridad")
                    cmd.Parameters.Add("@Prioridad", SqlDbType.TinyInt).Value = CByte(prioridad)
                End If

                If Not String.IsNullOrEmpty(filtro.Caso) Then
                    condiciones.Add("c.Caso = @Caso")
                    cmd.Parameters.Add("@Caso", SqlDbType.NVarChar, 20).Value = filtro.Caso
                End If

                ' Fecha límite: días tal cual. Creación/modificación: se guardan en UTC → rango del día local.
                AgregarRango(condiciones, cmd, "c.FechaLimite", "@Limite", filtro.LimiteDesde, filtro.LimiteHasta, esUtc:=False)
                AgregarRango(condiciones, cmd, "c.FechaCreacion", "@Creada", filtro.CreadaDesde, filtro.CreadaHasta, esUtc:=True)
                AgregarRango(condiciones, cmd, "c.FechaModificacion", "@Modificada", filtro.ModificadaDesde, filtro.ModificadaHasta, esUtc:=True)

                If filtro.CambiadaPor.HasValue Then
                    condiciones.Add("EXISTS (SELECT 1 FROM HistorialTarjeta h WHERE h.TarjetaId = c.Id AND h.UsuarioId = @CambiadaPor)")
                    cmd.Parameters.Add("@CambiadaPor", SqlDbType.Int).Value = filtro.CambiadaPor.Value
                End If

                Select Case filtro.Subtareas
                    Case FiltroBusqueda.SubtareasSin
                        condiciones.Add("NOT EXISTS (SELECT 1 FROM Subtareas s WHERE s.TarjetaId = c.Id)")
                    Case FiltroBusqueda.SubtareasPendientes
                        condiciones.Add("EXISTS (SELECT 1 FROM Subtareas s WHERE s.TarjetaId = c.Id AND s.Completada = 0)")
                    Case FiltroBusqueda.SubtareasCompletas
                        condiciones.Add("EXISTS (SELECT 1 FROM Subtareas s WHERE s.TarjetaId = c.Id) " &
                                        "AND NOT EXISTS (SELECT 1 FROM Subtareas s WHERE s.TarjetaId = c.Id AND s.Completada = 0)")
                End Select

                If filtro.ConCodigo.HasValue Then
                    condiciones.Add(If(filtro.ConCodigo.Value,
                                       "c.Codigo IS NOT NULL AND c.Codigo <> N''",
                                       "(c.Codigo IS NULL OR c.Codigo = N'')"))
                End If

                AgregarEtiquetas(filtro, cmd, condiciones)

                Dim orden As String = Nothing
                If filtro.Orden Is Nothing OrElse Not Ordenes.TryGetValue(filtro.Orden, orden) Then
                    orden = Ordenes(FiltroBusqueda.OrdenModificada)
                End If

                ' Acceso: solo tableros de los que el usuario es propietario o miembro.
                ' El resultado va a una tabla temporal para leer después las etiquetas de esas mismas tarjetas.
                cmd.CommandText =
                    "SELECT TOP (@Max) COUNT(*) OVER () AS Total, ROW_NUMBER() OVER (ORDER BY " & orden & ") AS Fila," &
                    " t.Id AS TableroId, t.Nombre AS NombreTablero," &
                    TarjetasDatos.SqlColumnasTarjeta & "
INTO #Resultado" &
                    TarjetasDatos.SqlFromTarjetas & "
JOIN Tableros t ON t.Id = l.TableroId
WHERE (t.PropietarioId = @UsuarioId
       OR EXISTS (SELECT 1 FROM MiembrosTablero m WHERE m.TableroId = t.Id AND m.UsuarioId = @UsuarioId))" &
                    String.Concat(condiciones.Select(Function(c) vbLf & "  AND " & c)) & vbLf &
                    "ORDER BY " & orden & ";" & vbLf &
                    "SELECT * FROM #Resultado ORDER BY Fila;" & vbLf &
                    "SELECT te.TarjetaId, e.Id, e.Nombre, e.Color FROM #Resultado r" &
                    " JOIN TarjetaEtiquetas te ON te.TarjetaId = r.Id JOIN Etiquetas e ON e.Id = te.EtiquetaId" &
                    " WHERE e.TableroId = r.TableroId ORDER BY e.Nombre;" & vbLf &
                    "DROP TABLE #Resultado;"

                Dim resultado As New ResultadoBusqueda
                Dim porId As New Dictionary(Of Integer, Tarjeta)
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        resultado.Total = rd.GetInt32(rd.GetOrdinal("Total"))
                        Dim tarjeta = TarjetasDatos.LeerTarjeta(rd)
                        porId(tarjeta.Id) = tarjeta
                        resultado.Tarjetas.Add(New TarjetaEncontrada With {
                            .Tarjeta = tarjeta,
                            .TableroId = rd.GetInt32(rd.GetOrdinal("TableroId")),
                            .NombreTablero = rd.GetString(rd.GetOrdinal("NombreTablero"))
                        })
                    End While
                    rd.NextResult()
                    EtiquetasDatos.LeerEnTarjetas(rd, porId)
                End Using
                Return resultado
            End Using
        End Function

        ' Cada palabra debe aparecer (LIKE) en alguno de los campos elegidos.
        Private Shared Sub AgregarTexto(filtro As FiltroBusqueda, cmd As SqlCommand, condiciones As List(Of String))
            If String.IsNullOrWhiteSpace(filtro.Texto) Then Return
            Dim palabras = filtro.Texto.Split(New Char() {" "c, ControlChars.Tab}, StringSplitOptions.RemoveEmptyEntries).
                           Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxPalabras).ToList()

            For i = 0 To palabras.Count - 1
                Dim p = "@Palabra" & i.ToString(CultureInfo.InvariantCulture)
                cmd.Parameters.Add(p, SqlDbType.NVarChar, 410).Value = "%" & EscaparLike(palabras(i)) & "%"

                Dim campos As New List(Of String)
                If filtro.EnTitulo Then
                    campos.Add("c.Titulo LIKE " & p & " ESCAPE '\'")
                    campos.Add("c.Caso LIKE " & p & " ESCAPE '\'")
                End If
                If filtro.EnDescripcion Then campos.Add("c.Descripcion LIKE " & p & " ESCAPE '\'")
                If filtro.EnCodigo Then campos.Add("c.Codigo LIKE " & p & " ESCAPE '\'")
                If filtro.EnComentarios Then
                    campos.Add("EXISTS (SELECT 1 FROM Comentarios co WHERE co.TarjetaId = c.Id AND co.Texto LIKE " & p & " ESCAPE '\')")
                End If
                If filtro.EnSubtareas Then
                    campos.Add("EXISTS (SELECT 1 FROM Subtareas s WHERE s.TarjetaId = c.Id AND s.Texto LIKE " & p & " ESCAPE '\')")
                End If
                Dim sinAlmohadilla = palabras(i).TrimStart("#"c)
                If filtro.EnEtiquetas AndAlso sinAlmohadilla.Length > 0 Then
                    Dim pe = "@PalabraEtiqueta" & i.ToString(CultureInfo.InvariantCulture)
                    cmd.Parameters.Add(pe, SqlDbType.NVarChar, 410).Value = "%" & EscaparLike(sinAlmohadilla) & "%"
                    campos.Add("EXISTS (SELECT 1 FROM TarjetaEtiquetas te JOIN Etiquetas e ON e.Id = te.EtiquetaId " &
                               "WHERE te.TarjetaId = c.Id AND e.Nombre LIKE " & pe & " ESCAPE '\')")
                End If

                ' Sin campos marcados no puede haber coincidencias.
                condiciones.Add(If(campos.Count = 0, "1 = 0", "(" & String.Join(" OR ", campos) & ")"))
            Next
        End Sub

        ' Etiquetas por nombre (sin distinguir mayúsculas, por la intercalación). Los nombres de parámetro
        ' (@Etiqueta0, @Etiqueta1...) los genera el código; los valores van como parámetros.
        Private Shared Sub AgregarEtiquetas(filtro As FiltroBusqueda, cmd As SqlCommand, condiciones As List(Of String))
            Const deLaTarjeta = "SELECT 1 FROM TarjetaEtiquetas te JOIN Etiquetas e ON e.Id = te.EtiquetaId WHERE te.TarjetaId = c.Id"

            If filtro.ModoEtiquetas = FiltroBusqueda.EtiquetasSin Then
                condiciones.Add("NOT EXISTS (SELECT 1 FROM TarjetaEtiquetas te WHERE te.TarjetaId = c.Id)")
                Return
            End If

            Dim nombres = If(filtro.Etiquetas, New List(Of String)).
                          Where(AddressOf Etiqueta.EsNombreValido).
                          Distinct(StringComparer.CurrentCultureIgnoreCase).Take(Etiqueta.MaximoPorTarjeta).ToList()
            If nombres.Count = 0 Then Return

            Dim parametros As New List(Of String)
            For i = 0 To nombres.Count - 1
                Dim p = "@Etiqueta" & i.ToString(CultureInfo.InvariantCulture)
                cmd.Parameters.Add(p, SqlDbType.NVarChar, Etiqueta.LongitudMaxima).Value = nombres(i)
                parametros.Add(p)
            Next

            If filtro.ModoEtiquetas = FiltroBusqueda.EtiquetasAlguna Then
                condiciones.Add("EXISTS (" & deLaTarjeta & " AND e.Nombre IN (" & String.Join(", ", parametros) & "))")
            Else
                For Each p In parametros   ' todas
                    condiciones.Add("EXISTS (" & deLaTarjeta & " AND e.Nombre = " & p & ")")
                Next
            End If
        End Sub

        ''' <summary>Rango de días inclusivo. Para columnas UTC convierte los límites del día local a UTC.</summary>
        Private Shared Sub AgregarRango(condiciones As List(Of String), cmd As SqlCommand, columna As String, prefijo As String,
                                        desde As DateTime?, hasta As DateTime?, esUtc As Boolean)
            If desde.HasValue Then
                condiciones.Add(columna & " >= " & prefijo & "Desde")
                cmd.Parameters.Add(prefijo & "Desde", SqlDbType.DateTime2).Value = LimiteDia(desde.Value.Date, esUtc)
            End If
            If hasta.HasValue Then
                condiciones.Add(columna & " < " & prefijo & "Hasta")
                cmd.Parameters.Add(prefijo & "Hasta", SqlDbType.DateTime2).Value = LimiteDia(hasta.Value.Date.AddDays(1), esUtc)
            End If
        End Sub

        Private Shared Function LimiteDia(dia As DateTime, esUtc As Boolean) As DateTime
            Return If(esUtc, DateTime.SpecifyKind(dia, DateTimeKind.Local).ToUniversalTime(), dia)
        End Function

        ''' <summary>Escapa los comodines de LIKE (%, _, [) usando \ como carácter de escape.</summary>
        Friend Shared Function EscaparLike(texto As String) As String
            Return texto.Replace("\", "\\").Replace("%", "\%").Replace("_", "\_").Replace("[", "\[")
        End Function

    End Class

End Namespace
