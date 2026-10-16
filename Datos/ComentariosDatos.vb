Imports System.Data.SqlClient
Imports TaskManager.Models

Namespace Datos

    Public NotInheritable Class ComentariosDatos

        Private Sub New()
        End Sub

        ''' <summary>Comentarios de la tarjeta (más recientes primero), si pertenece al tablero.</summary>
        Public Shared Function Listar(tarjetaId As Integer, tableroId As Integer) As List(Of Comentario)
            Const sql = "
SELECT co.Id, co.TarjetaId, co.UsuarioId, u.Nombre AS NombreUsuario, co.Texto, co.Fecha
FROM Comentarios co
JOIN Usuarios u ON u.Id = co.UsuarioId
JOIN Tarjetas c ON c.Id = co.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
WHERE co.TarjetaId = @TarjetaId AND l.TableroId = @TableroId
ORDER BY co.Fecha DESC, co.Id DESC"

            Dim comentarios As New List(Of Comentario)
            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        comentarios.Add(New Comentario With {
                            .Id = rd.GetInt32(0),
                            .TarjetaId = rd.GetInt32(1),
                            .UsuarioId = rd.GetInt32(2),
                            .NombreUsuario = rd.GetString(3),
                            .Texto = rd.GetString(4),
                            .Fecha = rd.GetDateTime(5)
                        })
                    End While
                End Using
            End Using
            Return comentarios
        End Function

        Public Shared Sub Crear(tarjetaId As Integer, tableroId As Integer, usuarioId As Integer, texto As String)
            Const sql = "
INSERT INTO Comentarios (TarjetaId, UsuarioId, Texto)
SELECT c.Id, @UsuarioId, @Texto
FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cmd.Parameters.Add("@Texto", SqlDbType.NVarChar, -1).Value = texto
                cn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Sub

    End Class

End Namespace
