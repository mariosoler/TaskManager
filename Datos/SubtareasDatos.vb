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
        Public Shared Sub Crear(tarjetaId As Integer, tableroId As Integer, texto As String)
            Const sql = "
INSERT INTO Subtareas (TarjetaId, Texto, Posicion)
SELECT c.Id, @Texto, ISNULL((SELECT MAX(Posicion) + 1 FROM Subtareas WHERE TarjetaId = c.Id), 0)
FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@Texto", SqlDbType.NVarChar, 300).Value = texto
                cn.Open()
                cmd.ExecuteNonQuery()
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

        Public Shared Sub CambiarEstado(subtareaId As Integer, tableroId As Integer, completada As Boolean)
            Const sql = "
UPDATE s SET Completada = @Completada
FROM Subtareas s
JOIN Tarjetas c ON c.Id = s.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
WHERE s.Id = @SubtareaId AND l.TableroId = @TableroId"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@SubtareaId", SqlDbType.Int).Value = subtareaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@Completada", SqlDbType.Bit).Value = completada
                cn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Public Shared Sub Eliminar(subtareaId As Integer, tableroId As Integer)
            Const sql = "
DELETE s
FROM Subtareas s
JOIN Tarjetas c ON c.Id = s.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
WHERE s.Id = @SubtareaId AND l.TableroId = @TableroId"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@SubtareaId", SqlDbType.Int).Value = subtareaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Sub

    End Class

End Namespace
