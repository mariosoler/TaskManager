Imports System.Data.SqlClient
Imports TaskManager.Models

Namespace Datos

    ''' <summary>
    ''' Subtareas (checklist) de una tarjeta. Todas las operaciones comprueban que la tarjeta
    ''' pertenece al TableroId ya autorizado.
    ''' </summary>
    Public NotInheritable Class SubtareasDatos

        Private Sub New()
        End Sub

        Public Shared Function Listar(tarjetaId As Integer, tableroId As Integer) As List(Of Subtarea)
            Const sql = "
SELECT s.Id, s.TarjetaId, s.Texto, s.Completada, s.Posicion
FROM Subtareas s
JOIN Tarjetas c ON c.Id = s.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
WHERE s.TarjetaId = @TarjetaId AND l.TableroId = @TableroId
ORDER BY s.Posicion, s.Id"

            Dim subtareas As New List(Of Subtarea)
            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        subtareas.Add(New Subtarea With {
                            .Id = rd.GetInt32(0),
                            .TarjetaId = rd.GetInt32(1),
                            .Texto = rd.GetString(2),
                            .Completada = rd.GetBoolean(3),
                            .Posicion = rd.GetInt32(4)
                        })
                    End While
                End Using
            End Using
            Return subtareas
        End Function

        ''' <summary>Añade la subtarea al final si la tarjeta pertenece al tablero.</summary>
        Public Shared Sub Crear(tarjetaId As Integer, tableroId As Integer, texto As String, usuarioId As Integer)
            Const sql = "
INSERT INTO Subtareas (TarjetaId, Texto, Posicion)
OUTPUT INSERTED.Id
SELECT c.Id, @Texto, ISNULL((SELECT MAX(Posicion) + 1 FROM Subtareas WHERE TarjetaId = c.Id), 0)
FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId"

            Using cn = Db.CrearConexion()
                cn.Open()
                Using tx = cn.BeginTransaction()
                    Using cmd As New SqlCommand(sql, cn, tx)
                        cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                        cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                        cmd.Parameters.Add("@Texto", SqlDbType.NVarChar, 300).Value = texto
                        If cmd.ExecuteScalar() Is Nothing Then Return
                    End Using
                    HistorialDatos.Registrar(cn, tx, tarjetaId, usuarioId,
                                             HistorialTarjeta.Nuevo(HistorialTarjeta.SubtareaAgregada, valorNuevo:=texto))
                    tx.Commit()
                End Using
            End Using
        End Sub

        ''' <summary>Inserta una subtarea dentro de una transacción ya abierta (al crear la tarjeta).</summary>
        Friend Shared Sub Insertar(cn As SqlConnection, tx As SqlTransaction, tarjetaId As Integer,
                                   texto As String, completada As Boolean, posicion As Integer)
            Const sql = "INSERT INTO Subtareas (TarjetaId, Texto, Completada, Posicion) VALUES (@TarjetaId, @Texto, @Completada, @Posicion)"

            Using cmd As New SqlCommand(sql, cn, tx)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@Texto", SqlDbType.NVarChar, 300).Value = texto
                cmd.Parameters.Add("@Completada", SqlDbType.Bit).Value = completada
                cmd.Parameters.Add("@Posicion", SqlDbType.Int).Value = posicion
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        ''' <summary>Marca o desmarca la subtarea; solo registra historial si el estado cambia de verdad.</summary>
        Public Shared Sub CambiarEstado(subtareaId As Integer, tableroId As Integer, completada As Boolean, usuarioId As Integer)
            Const sql = "
UPDATE s SET Completada = @Completada
OUTPUT INSERTED.TarjetaId, INSERTED.Texto
FROM Subtareas s
JOIN Tarjetas c ON c.Id = s.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
WHERE s.Id = @SubtareaId AND l.TableroId = @TableroId AND s.Completada <> @Completada"

            Dim accion = If(completada, HistorialTarjeta.SubtareaCompletada, HistorialTarjeta.SubtareaReabierta)
            EjecutarYRegistrar(sql, subtareaId, tableroId, usuarioId, accion,
                               Sub(cmd) cmd.Parameters.Add("@Completada", SqlDbType.Bit).Value = completada)
        End Sub

        Public Shared Sub Eliminar(subtareaId As Integer, tableroId As Integer, usuarioId As Integer)
            Const sql = "
DELETE s
OUTPUT DELETED.TarjetaId, DELETED.Texto
FROM Subtareas s
JOIN Tarjetas c ON c.Id = s.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
WHERE s.Id = @SubtareaId AND l.TableroId = @TableroId"

            EjecutarYRegistrar(sql, subtareaId, tableroId, usuarioId, HistorialTarjeta.SubtareaEliminada, Nothing)
        End Sub

        ' Ejecuta una sentencia con OUTPUT (TarjetaId, Texto) y, si afectó a una fila, registra la acción.
        Private Shared Sub EjecutarYRegistrar(sql As String, subtareaId As Integer, tableroId As Integer, usuarioId As Integer,
                                              accion As String, parametrosExtra As Action(Of SqlCommand))
            Using cn = Db.CrearConexion()
                cn.Open()
                Using tx = cn.BeginTransaction()
                    Dim tarjetaId As Integer
                    Dim texto As String
                    Using cmd As New SqlCommand(sql, cn, tx)
                        cmd.Parameters.Add("@SubtareaId", SqlDbType.Int).Value = subtareaId
                        cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                        parametrosExtra?.Invoke(cmd)
                        Using rd = cmd.ExecuteReader()
                            If Not rd.Read() Then Return
                            tarjetaId = rd.GetInt32(0)
                            texto = rd.GetString(1)
                        End Using
                    End Using
                    HistorialDatos.Registrar(cn, tx, tarjetaId, usuarioId, HistorialTarjeta.Nuevo(accion, valorNuevo:=texto))
                    tx.Commit()
                End Using
            End Using
        End Sub

    End Class

End Namespace
