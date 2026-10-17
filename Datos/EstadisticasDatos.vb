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

        ''' <summary>Días sin cambios a partir de los que una tarjeta cuenta como «sin actividad».</summary>
        Public Const DiasSinActividad As Integer = 30
        ''' <summary>Horizonte (días desde hoy) y número máximo de filas de «Próximos vencimientos».</summary>
        Public Const DiasProximos As Integer = 14
        Public Const MaxProximos As Integer = 10

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
WHERE l.TableroId = @TableroId AND NOT EXISTS (SELECT 1 FROM TarjetaEtiquetas te WHERE te.TarjetaId = c.Id);

-- 10. Tarjetas por prioridad (NULL = sin prioridad)
SELECT CAST(c.Prioridad AS INT) AS Prioridad, COUNT(*) AS Cantidad
FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
WHERE l.TableroId = @TableroId
GROUP BY c.Prioridad;

-- 11. Antigüedad (días desde la creación), tarjetas sin actividad y creadas en el periodo y en el anterior
SELECT ISNULL(SUM(CASE WHEN DATEDIFF(DAY, c.FechaCreacion, @AhoraUtc) <= 7 THEN 1 ELSE 0 END), 0) AS Hasta7,
       ISNULL(SUM(CASE WHEN DATEDIFF(DAY, c.FechaCreacion, @AhoraUtc) BETWEEN 8 AND 30 THEN 1 ELSE 0 END), 0) AS Hasta30,
       ISNULL(SUM(CASE WHEN DATEDIFF(DAY, c.FechaCreacion, @AhoraUtc) BETWEEN 31 AND 90 THEN 1 ELSE 0 END), 0) AS Hasta90,
       ISNULL(SUM(CASE WHEN DATEDIFF(DAY, c.FechaCreacion, @AhoraUtc) > 90 THEN 1 ELSE 0 END), 0) AS Mas90,
       ISNULL(SUM(CASE WHEN c.FechaModificacion < @SinActividadUtc THEN 1 ELSE 0 END), 0) AS SinActividad,
       ISNULL(SUM(CASE WHEN c.FechaCreacion >= @DesdeUtc THEN 1 ELSE 0 END), 0) AS Creadas,
       ISNULL(SUM(CASE WHEN c.FechaCreacion >= @DesdeAnteriorUtc AND c.FechaCreacion < @DesdeUtc THEN 1 ELSE 0 END), 0) AS CreadasAntes
FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
WHERE l.TableroId = @TableroId;

-- 12. Próximos vencimientos: vencidas y las que vencen en los próximos días, las más urgentes primero
SELECT TOP (@MaxProximos) c.Id, c.Titulo, c.Caso, l.Titulo AS Lista, u.Nombre AS Asignado, c.FechaLimite,
       CAST(c.Prioridad AS INT) AS Prioridad
FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
LEFT JOIN Usuarios u ON u.Id = c.AsignadoA
WHERE l.TableroId = @TableroId AND c.FechaLimite IS NOT NULL AND c.FechaLimite < @FinProximos
ORDER BY c.FechaLimite, ISNULL(c.Prioridad, 4), c.Id;"

            Dim hoy = DateTime.Today
            Dim primerDia = hoy.AddDays(1 - dias)
            Dim est As New Estadisticas With {.Dias = dias}

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@Hoy", SqlDbType.DateTime2).Value = hoy
                cmd.Parameters.Add("@FinSemana", SqlDbType.DateTime2).Value = hoy.AddDays(8)
                Dim desdeUtc = DateTime.SpecifyKind(primerDia, DateTimeKind.Local).ToUniversalTime()
                cmd.Parameters.Add("@DesdeUtc", SqlDbType.DateTime2).Value = desdeUtc
                cmd.Parameters.Add("@DesdeAnteriorUtc", SqlDbType.DateTime2).Value = desdeUtc.AddDays(-dias)
                cmd.Parameters.Add("@AhoraUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow
                cmd.Parameters.Add("@SinActividadUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow.AddDays(-DiasSinActividad)
                cmd.Parameters.Add("@FinProximos", SqlDbType.DateTime2).Value = hoy.AddDays(DiasProximos + 1)
                cmd.Parameters.Add("@MaxProximos", SqlDbType.Int).Value = MaxProximos
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

                    rd.NextResult()
                    Dim porPrioridad As New Dictionary(Of Integer, Integer)   ' 0 = sin prioridad
                    While rd.Read()
                        porPrioridad(If(Db.Entero(rd, "Prioridad"), 0)) = rd.GetInt32(1)
                    End While
                    Dim cantidad = Function(p As Integer) If(porPrioridad.ContainsKey(p), porPrioridad(p), 0)
                    est.PorPrioridad = New List(Of Dato) From {
                        New Dato With {.Etiqueta = "Alta", .Valor = cantidad(Prioridad.Alta), .Clave = "alta"},
                        New Dato With {.Etiqueta = "Media", .Valor = cantidad(Prioridad.Media), .Clave = "media"},
                        New Dato With {.Etiqueta = "Baja", .Valor = cantidad(Prioridad.Baja), .Clave = "baja"},
                        New Dato With {.Etiqueta = "Sin prioridad", .Valor = cantidad(0), .Clave = "sin"}
                    }
                    est.PrioridadAlta = cantidad(Prioridad.Alta)

                    rd.NextResult()
                    rd.Read()
                    est.Antiguedad = New List(Of Dato) From {
                        New Dato With {.Etiqueta = "Hasta 7 días", .Valor = rd.GetInt32(0)},
                        New Dato With {.Etiqueta = "De 8 a 30 días", .Valor = rd.GetInt32(1)},
                        New Dato With {.Etiqueta = "De 31 a 90 días", .Valor = rd.GetInt32(2)},
                        New Dato With {.Etiqueta = "Más de 90 días", .Valor = rd.GetInt32(3)}
                    }
                    est.SinActividad = rd.GetInt32(4)
                    est.CreadasPeriodo = rd.GetInt32(5)
                    est.CreadasPeriodoAnterior = rd.GetInt32(6)

                    rd.NextResult()
                    While rd.Read()
                        est.ProximosVencimientos.Add(New TarjetaVencimiento With {
                            .Id = rd.GetInt32(0),
                            .Titulo = rd.GetString(1),
                            .Caso = Db.Texto(rd, "Caso"),
                            .NombreLista = rd.GetString(3),
                            .NombreAsignado = Db.Texto(rd, "Asignado"),
                            .FechaLimite = rd.GetDateTime(5),
                            .Prioridad = Db.Entero(rd, "Prioridad")})
                    End While
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
