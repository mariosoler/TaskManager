Imports System.Data.SqlClient
Imports TaskManager.Models

Namespace Datos

    ''' <summary>Estadísticas de un tablero ya autorizado.</summary>
    Public NotInheritable Class EstadisticasDatos

        ' Agrupación de acciones del historial en tipos legibles (orden de presentación).
        Private Shared ReadOnly TiposDeCambio As New List(Of KeyValuePair(Of String, String())) From {
            New KeyValuePair(Of String, String())("Edición de campos", {HistorialTarjeta.Editada}),
            New KeyValuePair(Of String, String())("Subtareas", {HistorialTarjeta.SubtareaAgregada, HistorialTarjeta.SubtareaCompletada,
                                                                 HistorialTarjeta.SubtareaReabierta, HistorialTarjeta.SubtareaEliminada}),
            New KeyValuePair(Of String, String())("Cambio de lista", {HistorialTarjeta.Movida}),
            New KeyValuePair(Of String, String())("Comentarios", {HistorialTarjeta.Comentada}),
            New KeyValuePair(Of String, String())("Creación", {HistorialTarjeta.Creada})
        }

        Private Sub New()
        End Sub

        Public Shared Function Obtener(tableroId As Integer, dias As Integer) As Estadisticas
            Const sql = "
-- 1. Estado de las tarjetas
SELECT COUNT(*) AS Total,
       ISNULL(SUM(CASE WHEN c.FechaLimite < @Hoy THEN 1 ELSE 0 END), 0) AS Vencidas,
       ISNULL(SUM(CASE WHEN c.FechaLimite >= @Hoy AND c.FechaLimite < @FinSemana THEN 1 ELSE 0 END), 0) AS Semana,
       ISNULL(SUM(CASE WHEN c.FechaLimite >= @FinSemana THEN 1 ELSE 0 END), 0) AS MasAdelante,
       ISNULL(SUM(CASE WHEN c.FechaLimite IS NULL THEN 1 ELSE 0 END), 0) AS SinFecha,
       ISNULL(SUM(CASE WHEN c.AsignadoA IS NULL THEN 1 ELSE 0 END), 0) AS SinAsignar
FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
WHERE l.TableroId = @TableroId;

-- 2. Subtareas y comentarios
SELECT (SELECT COUNT(*) FROM Subtareas s JOIN Tarjetas c ON c.Id = s.TarjetaId JOIN Listas l ON l.Id = c.ListaId
        WHERE l.TableroId = @TableroId) AS SubtareasTotal,
       (SELECT COUNT(*) FROM Subtareas s JOIN Tarjetas c ON c.Id = s.TarjetaId JOIN Listas l ON l.Id = c.ListaId
        WHERE l.TableroId = @TableroId AND s.Completada = 1) AS SubtareasHechas,
       (SELECT COUNT(*) FROM Comentarios co JOIN Tarjetas c ON c.Id = co.TarjetaId JOIN Listas l ON l.Id = c.ListaId
        WHERE l.TableroId = @TableroId) AS Comentarios;

-- 3. Tarjetas por lista (en el orden del tablero, incluidas las vacías)
SELECT l.Titulo, COUNT(c.Id) AS Cantidad
FROM Listas l LEFT JOIN Tarjetas c ON c.ListaId = l.Id
WHERE l.TableroId = @TableroId
GROUP BY l.Id, l.Titulo, l.Posicion
ORDER BY l.Posicion, l.Id;

-- 4. Tarjetas por persona asignada
SELECT u.Nombre, COUNT(*) AS Cantidad
FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId JOIN Usuarios u ON u.Id = c.AsignadoA
WHERE l.TableroId = @TableroId
GROUP BY u.Id, u.Nombre
ORDER BY Cantidad DESC, u.Nombre;

-- 5. Cambios por hora UTC (se agrupan por día local en .NET)
SELECT DATEADD(HOUR, DATEDIFF(HOUR, 0, h.Fecha), 0) AS Hora, COUNT(*) AS Cantidad
FROM HistorialTarjeta h JOIN Tarjetas c ON c.Id = h.TarjetaId JOIN Listas l ON l.Id = c.ListaId
WHERE l.TableroId = @TableroId AND h.Fecha >= @DesdeUtc
GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, h.Fecha), 0);

-- 6. Cambios por persona
SELECT u.Nombre, COUNT(*) AS Cantidad
FROM HistorialTarjeta h JOIN Tarjetas c ON c.Id = h.TarjetaId JOIN Listas l ON l.Id = c.ListaId
JOIN Usuarios u ON u.Id = h.UsuarioId
WHERE l.TableroId = @TableroId AND h.Fecha >= @DesdeUtc
GROUP BY u.Id, u.Nombre
ORDER BY Cantidad DESC, u.Nombre;

-- 7. Cambios por acción
SELECT h.Accion, COUNT(*) AS Cantidad
FROM HistorialTarjeta h JOIN Tarjetas c ON c.Id = h.TarjetaId JOIN Listas l ON l.Id = c.ListaId
WHERE l.TableroId = @TableroId AND h.Fecha >= @DesdeUtc
GROUP BY h.Accion;

-- 8. Tarjetas por etiqueta (solo las que tienen alguna tarjeta), con vencidas y sin asignar
SELECT e.Nombre, e.Color, COUNT(*) AS Tarjetas,
       SUM(CASE WHEN c.FechaLimite < @Hoy THEN 1 ELSE 0 END) AS Vencidas,
       SUM(CASE WHEN c.AsignadoA IS NULL THEN 1 ELSE 0 END) AS SinAsignar
FROM Etiquetas e
JOIN TarjetaEtiquetas te ON te.EtiquetaId = e.Id
JOIN Tarjetas c ON c.Id = te.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
WHERE e.TableroId = @TableroId AND l.TableroId = @TableroId
GROUP BY e.Id, e.Nombre, e.Color
ORDER BY Tarjetas DESC, e.Nombre;

-- 9. Tarjetas sin ninguna etiqueta
SELECT COUNT(*) FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
WHERE l.TableroId = @TableroId AND NOT EXISTS (SELECT 1 FROM TarjetaEtiquetas te WHERE te.TarjetaId = c.Id);"

            Dim hoy = DateTime.Today
            Dim primerDia = hoy.AddDays(1 - dias)
            Dim est As New Estadisticas With {.Dias = dias}

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@Hoy", SqlDbType.DateTime2).Value = hoy
                cmd.Parameters.Add("@FinSemana", SqlDbType.DateTime2).Value = hoy.AddDays(8)
                cmd.Parameters.Add("@DesdeUtc", SqlDbType.DateTime2).Value =
                    DateTime.SpecifyKind(primerDia, DateTimeKind.Local).ToUniversalTime()
                cn.Open()

                Using rd = cmd.ExecuteReader()
                    rd.Read()
                    est.TotalTarjetas = rd.GetInt32(0)
                    est.Vencidas = rd.GetInt32(1)
                    est.VencenEstaSemana = rd.GetInt32(2)
                    est.VencenMasAdelante = rd.GetInt32(3)
                    est.SinFecha = rd.GetInt32(4)
                    est.SinAsignar = rd.GetInt32(5)

                    rd.NextResult()
                    rd.Read()
                    est.SubtareasTotal = rd.GetInt32(0)
                    est.SubtareasHechas = rd.GetInt32(1)
                    est.Comentarios = rd.GetInt32(2)

                    rd.NextResult()
                    est.PorLista = LeerDatos(rd)

                    rd.NextResult()
                    est.PorPersona = LeerDatos(rd)
                    If est.SinAsignar > 0 Then
                        est.PorPersona.Add(New Dato With {.Etiqueta = "Sin asignar", .Valor = est.SinAsignar, .Clave = "sin-asignar"})
                    End If

                    rd.NextResult()
                    Dim porDia = Enumerable.Range(0, dias).ToDictionary(Function(i) primerDia.AddDays(i), Function(i) 0)
                    While rd.Read()
                        Dim diaLocal = DateTime.SpecifyKind(rd.GetDateTime(0), DateTimeKind.Utc).ToLocalTime().Date
                        If porDia.ContainsKey(diaLocal) Then porDia(diaLocal) += rd.GetInt32(1)
                    End While
                    est.CambiosPorDia = porDia.Select(Function(p) New DatoDia With {.Dia = p.Key, .Valor = p.Value}).ToList()
                    est.TotalCambios = est.CambiosPorDia.Sum(Function(d) d.Valor)

                    rd.NextResult()
                    est.CambiosPorPersona = LeerDatos(rd)

                    rd.NextResult()
                    Dim porAccion As New Dictionary(Of String, Integer)
                    While rd.Read()
                        porAccion(rd.GetString(0)) = rd.GetInt32(1)
                    End While
                    est.CambiosPorTipo = TiposDeCambio.Select(Function(t) New Dato With {
                        .Etiqueta = t.Key,
                        .Valor = t.Value.Sum(Function(a) If(porAccion.ContainsKey(a), porAccion(a), 0))
                    }).ToList()

                    rd.NextResult()
                    While rd.Read()
                        est.PorEtiqueta.Add(New DatoEtiqueta With {
                            .Etiqueta = New Etiqueta With {.Nombre = rd.GetString(0), .Color = rd.GetString(1)},
                            .Tarjetas = rd.GetInt32(2), .Vencidas = rd.GetInt32(3), .SinAsignar = rd.GetInt32(4)})
                    End While

                    rd.NextResult()
                    rd.Read()
                    est.SinEtiqueta = rd.GetInt32(0)
                End Using
            End Using
            Return est
        End Function

        Private Shared Function LeerDatos(rd As SqlDataReader) As List(Of Dato)
            Dim datos As New List(Of Dato)
            While rd.Read()
                datos.Add(New Dato With {.Etiqueta = rd.GetString(0), .Valor = rd.GetInt32(1)})
            End While
            Return datos
        End Function

    End Class

End Namespace
