Imports System.Data.SqlClient
Imports TaskManager.Models

Namespace Datos

    ''' <summary>
    ''' Mensajes del chat de cada tarjeta (tabla Comentarios). No se registran en el historial
    ''' ni cambian la fecha de modificación: son conversación, no cambios de la tarjeta.
    ''' </summary>
    Public NotInheritable Class ComentariosDatos

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Mensajes de la tarjeta en orden cronológico (si pertenece al tablero).
        ''' Con <paramref name="desdeId"/> solo devuelve los posteriores a ese mensaje (sondeo del chat).
        ''' </summary>
        Public Shared Function Listar(tarjetaId As Integer, tableroId As Integer, Optional desdeId As Integer = 0) As List(Of Comentario)
            Const sql = "
SELECT co.Id, co.TarjetaId, co.UsuarioId, u.Nombre AS NombreUsuario, co.Texto, co.Fecha
FROM Comentarios co
JOIN Usuarios u ON u.Id = co.UsuarioId
JOIN Tarjetas c ON c.Id = co.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
WHERE co.TarjetaId = @TarjetaId AND l.TableroId = @TableroId AND co.Id > @DesdeId
ORDER BY co.Id"

            Dim comentarios As New List(Of Comentario)
            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@DesdeId", SqlDbType.Int).Value = desdeId
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

        ''' <summary>Publica un mensaje y devuelve su Id, o Nothing si la tarjeta no pertenece al tablero.</summary>
        Public Shared Function Crear(tarjetaId As Integer, tableroId As Integer, usuarioId As Integer, texto As String) As Integer?
            Const sql = "
INSERT INTO Comentarios (TarjetaId, UsuarioId, Texto)
OUTPUT INSERTED.Id
SELECT c.Id, @UsuarioId, @Texto
FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cmd.Parameters.Add("@Texto", SqlDbType.NVarChar, -1).Value = texto
                cn.Open()
                Dim id = cmd.ExecuteScalar()
                Return If(id Is Nothing, CType(Nothing, Integer?), CInt(id))
            End Using
        End Function

    End Class

End Namespace
