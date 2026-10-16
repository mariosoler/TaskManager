Imports System.Data.SqlClient
Imports TaskManager.Models

Namespace Datos

    ''' <summary>
    ''' Operaciones sobre tarjetas. Todas filtran por el TableroId ya autorizado (vía Listas).
    ''' </summary>
    Public NotInheritable Class TarjetasDatos

        Private Sub New()
        End Sub

        ' No incluye Codigo (NVARCHAR(MAX)): en la vista del tablero basta con TieneCodigo.
        Friend Const SqlSelectTarjetas As String = "
SELECT c.Id, c.ListaId, l.Titulo AS NombreLista, c.Titulo, c.Descripcion, c.Posicion, c.FechaLimite,
       c.AsignadoA, u.Nombre AS NombreAsignado, c.FechaCreacion, c.Caso,
       CAST(CASE WHEN c.Codigo IS NULL OR c.Codigo = N'' THEN 0 ELSE 1 END AS BIT) AS TieneCodigo,
       (SELECT COUNT(*) FROM Comentarios co WHERE co.TarjetaId = c.Id) AS NumComentarios,
       (SELECT COUNT(*) FROM Subtareas s WHERE s.TarjetaId = c.Id) AS SubtareasTotal,
       (SELECT COUNT(*) FROM Subtareas s WHERE s.TarjetaId = c.Id AND s.Completada = 1) AS SubtareasHechas
FROM Tarjetas c
JOIN Listas l ON l.Id = c.ListaId
LEFT JOIN Usuarios u ON u.Id = c.AsignadoA"

        ''' <summary>Tarjeta completa (incluido Codigo) si pertenece al tablero; Nothing en caso contrario.</summary>
        Public Shared Function Obtener(tarjetaId As Integer, tableroId As Integer) As Tarjeta
            Dim sql = SqlSelectTarjetas & " WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId;
SELECT Codigo FROM Tarjetas WHERE Id = @TarjetaId;"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    If Not rd.Read() Then Return Nothing
                    Dim tarjeta = LeerTarjeta(rd)
                    rd.NextResult()
                    If rd.Read() Then tarjeta.Codigo = Db.Texto(rd, "Codigo")
                    Return tarjeta
                End Using
            End Using
        End Function

        ' @AsignadoA solo si tiene acceso al tablero de la lista "l"; si no, NULL.
        Private Const SqlAsignadoValido As String = "
    CASE
        WHEN @AsignadoA IS NULL THEN NULL
        WHEN EXISTS (SELECT 1 FROM MiembrosTablero m WHERE m.TableroId = l.TableroId AND m.UsuarioId = @AsignadoA)
          OR EXISTS (SELECT 1 FROM Tableros t WHERE t.Id = l.TableroId AND t.PropietarioId = @AsignadoA) THEN @AsignadoA
        ELSE NULL END"

        ''' <summary>
        ''' Crea la tarjeta al final de la lista (si la lista pertenece al tablero) junto con sus subtareas,
        ''' en una transacción. Usa Titulo, Descripcion, FechaLimite, AsignadoA, Caso y Codigo de <paramref name="datos"/>.
        ''' </summary>
        Public Shared Sub Crear(listaId As Integer, tableroId As Integer, datos As Tarjeta, subtareas As IEnumerable(Of Subtarea))
            Dim sql = "
INSERT INTO Tarjetas (ListaId, Titulo, Descripcion, Posicion, FechaLimite, AsignadoA, Caso, Codigo)
OUTPUT INSERTED.Id
SELECT l.Id, @Titulo, @Descripcion,
       ISNULL((SELECT MAX(Posicion) + 1 FROM Tarjetas WHERE ListaId = l.Id), 0),
       @FechaLimite," & SqlAsignadoValido & ", @Caso, @Codigo
FROM Listas l
WHERE l.Id = @ListaId AND l.TableroId = @TableroId"

            Using cn = Db.CrearConexion()
                cn.Open()
                Using tx = cn.BeginTransaction()
                    Dim tarjetaId As Object
                    Using cmd As New SqlCommand(sql, cn, tx)
                        cmd.Parameters.Add("@ListaId", SqlDbType.Int).Value = listaId
                        cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                        AgregarCampos(cmd, datos)
                        tarjetaId = cmd.ExecuteScalar()
                    End Using
                    If tarjetaId Is Nothing Then Return  ' la lista no es de este tablero (rollback al salir)

                    Dim posicion = 0
                    For Each subtarea In subtareas
                        SubtareasDatos.Insertar(cn, tx, CInt(tarjetaId), subtarea.Texto, subtarea.Completada, posicion)
                        posicion += 1
                    Next
                    tx.Commit()
                End Using
            End Using
        End Sub

        ''' <summary>Actualiza los datos editables. Solo se asigna a usuarios con acceso al tablero.</summary>
        Public Shared Sub Actualizar(tarjetaId As Integer, tableroId As Integer, datos As Tarjeta)
            Dim sql = "
UPDATE c SET
    Titulo = @Titulo,
    Descripcion = @Descripcion,
    FechaLimite = @FechaLimite,
    Caso = @Caso,
    Codigo = @Codigo,
    AsignadoA =" & SqlAsignadoValido & "
FROM Tarjetas c
JOIN Listas l ON l.Id = c.ListaId
WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                AgregarCampos(cmd, datos)
                cn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Shared Sub AgregarCampos(cmd As SqlCommand, datos As Tarjeta)
            cmd.Parameters.Add("@Titulo", SqlDbType.NVarChar, 200).Value = datos.Titulo
            cmd.Parameters.Add("@Descripcion", SqlDbType.NVarChar, -1).Value = Db.ONulo(datos.Descripcion)
            cmd.Parameters.Add("@FechaLimite", SqlDbType.DateTime2).Value = Db.ONulo(datos.FechaLimite)
            cmd.Parameters.Add("@AsignadoA", SqlDbType.Int).Value = Db.ONulo(datos.AsignadoA)
            cmd.Parameters.Add("@Caso", SqlDbType.NVarChar, 20).Value = Db.ONulo(datos.Caso)
            cmd.Parameters.Add("@Codigo", SqlDbType.NVarChar, -1).Value = Db.ONulo(datos.Codigo)
        End Sub

        Public Shared Sub Eliminar(tarjetaId As Integer, tableroId As Integer)
            Dim sql = "
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ListaId INT = (SELECT c.ListaId FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
                        WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId);
DELETE FROM Tarjetas WHERE Id = @TarjetaId AND ListaId = @ListaId;" &
                Db.SqlRenumerarTarjetas & "
COMMIT;"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        ''' <summary>
        ''' Mueve la tarjeta a la lista destino, en la posición <paramref name="indice"/> (0 = primera),
        ''' contando solo las demás tarjetas de esa lista. Renumera origen y destino en una transacción.
        ''' </summary>
        Public Shared Sub Mover(tarjetaId As Integer, listaDestinoId As Integer, indice As Integer, tableroId As Integer)
            Const sql = "
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @Origen INT = (SELECT c.ListaId FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
                       WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId);
IF @Origen IS NULL OR NOT EXISTS (SELECT 1 FROM Listas WHERE Id = @ListaDestinoId AND TableroId = @TableroId)
BEGIN
    ROLLBACK;
    RETURN;
END;
DECLARE @Total INT = (SELECT COUNT(*) FROM Tarjetas WHERE ListaId = @ListaDestinoId AND Id <> @TarjetaId);
DECLARE @Pos INT = CASE WHEN @Indice < 0 THEN 0 WHEN @Indice > @Total THEN @Total ELSE @Indice END;
WITH o AS (SELECT Posicion, ROW_NUMBER() OVER (ORDER BY Posicion, Id) - 1 AS N
           FROM Tarjetas WHERE ListaId = @ListaDestinoId AND Id <> @TarjetaId)
UPDATE o SET Posicion = CASE WHEN N >= @Pos THEN N + 1 ELSE N END;
UPDATE Tarjetas SET ListaId = @ListaDestinoId, Posicion = @Pos WHERE Id = @TarjetaId;
WITH o AS (SELECT Posicion, ROW_NUMBER() OVER (ORDER BY Posicion, Id) - 1 AS N
           FROM Tarjetas WHERE ListaId = @Origen)
UPDATE o SET Posicion = N;
COMMIT;"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@ListaDestinoId", SqlDbType.Int).Value = listaDestinoId
                cmd.Parameters.Add("@Indice", SqlDbType.Int).Value = indice
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Friend Shared Function LeerTarjeta(rd As SqlDataReader) As Tarjeta
            Return New Tarjeta With {
                .Id = rd.GetInt32(rd.GetOrdinal("Id")),
                .ListaId = rd.GetInt32(rd.GetOrdinal("ListaId")),
                .NombreLista = rd.GetString(rd.GetOrdinal("NombreLista")),
                .Titulo = rd.GetString(rd.GetOrdinal("Titulo")),
                .Descripcion = Db.Texto(rd, "Descripcion"),
                .Posicion = rd.GetInt32(rd.GetOrdinal("Posicion")),
                .FechaLimite = Db.Fecha(rd, "FechaLimite"),
                .AsignadoA = Db.Entero(rd, "AsignadoA"),
                .NombreAsignado = Db.Texto(rd, "NombreAsignado"),
                .FechaCreacion = rd.GetDateTime(rd.GetOrdinal("FechaCreacion")),
                .Caso = Db.Texto(rd, "Caso"),
                .TieneCodigo = rd.GetBoolean(rd.GetOrdinal("TieneCodigo")),
                .NumComentarios = rd.GetInt32(rd.GetOrdinal("NumComentarios")),
                .SubtareasTotal = rd.GetInt32(rd.GetOrdinal("SubtareasTotal")),
                .SubtareasHechas = rd.GetInt32(rd.GetOrdinal("SubtareasHechas"))
            }
        End Function

    End Class

End Namespace
