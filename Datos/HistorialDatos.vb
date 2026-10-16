Imports System.Data.SqlClient
Imports TaskManager.Models

Namespace Datos

    ''' <summary>
    ''' Historial de cambios de las tarjetas. <see cref="Registrar"/> se llama siempre dentro de la
    ''' transacción de la operación que modifica la tarjeta, para que historial y datos no diverjan.
    ''' </summary>
    Public NotInheritable Class HistorialDatos

        Private Const LongitudValor As Integer = 500

        Private Sub New()
        End Sub

        ''' <summary>Historial de la tarjeta (más reciente primero), si pertenece al tablero.</summary>
        Public Shared Function Listar(tarjetaId As Integer, tableroId As Integer) As List(Of HistorialTarjeta)
            Const sql = "
SELECT h.Id, h.TarjetaId, h.UsuarioId, u.Nombre AS NombreUsuario, h.Fecha, h.Accion, h.Campo, h.ValorAnterior, h.ValorNuevo
FROM HistorialTarjeta h
JOIN Usuarios u ON u.Id = h.UsuarioId
JOIN Tarjetas c ON c.Id = h.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
WHERE h.TarjetaId = @TarjetaId AND l.TableroId = @TableroId
ORDER BY h.Fecha DESC, h.Id DESC"

            Dim historial As New List(Of HistorialTarjeta)
            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        historial.Add(New HistorialTarjeta With {
                            .Id = rd.GetInt32(0),
                            .TarjetaId = rd.GetInt32(1),
                            .UsuarioId = rd.GetInt32(2),
                            .NombreUsuario = rd.GetString(3),
                            .Fecha = rd.GetDateTime(4),
                            .Accion = rd.GetString(5),
                            .Campo = Db.Texto(rd, "Campo"),
                            .ValorAnterior = Db.Texto(rd, "ValorAnterior"),
                            .ValorNuevo = Db.Texto(rd, "ValorNuevo")
                        })
                    End While
                End Using
            End Using
            Return historial
        End Function

        ''' <summary>
        ''' Inserta los cambios y actualiza FechaModificacion/ModificadoPor de la tarjeta.
        ''' No hace nada si no hay cambios.
        ''' </summary>
        Friend Shared Sub Registrar(cn As SqlConnection, tx As SqlTransaction, tarjetaId As Integer, usuarioId As Integer,
                                    cambios As IEnumerable(Of HistorialTarjeta))
            Const sqlTarjeta = "UPDATE Tarjetas SET FechaModificacion = @Fecha, ModificadoPor = @UsuarioId WHERE Id = @TarjetaId"
            Const sqlCambio = "
INSERT INTO HistorialTarjeta (TarjetaId, UsuarioId, Fecha, Accion, Campo, ValorAnterior, ValorNuevo)
VALUES (@TarjetaId, @UsuarioId, @Fecha, @Accion, @Campo, @ValorAnterior, @ValorNuevo)"

            Dim lista = cambios.ToList()
            If lista.Count = 0 Then Return
            Dim ahora = DateTime.UtcNow   ' misma marca de tiempo para la tarjeta y todas sus filas

            Using cmd As New SqlCommand(sqlTarjeta, cn, tx)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cmd.Parameters.Add("@Fecha", SqlDbType.DateTime2).Value = ahora
                cmd.ExecuteNonQuery()
            End Using

            For Each cambio In lista
                Using cmd As New SqlCommand(sqlCambio, cn, tx)
                    cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                    cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                    cmd.Parameters.Add("@Fecha", SqlDbType.DateTime2).Value = ahora
                    cmd.Parameters.Add("@Accion", SqlDbType.NVarChar, 30).Value = cambio.Accion
                    cmd.Parameters.Add("@Campo", SqlDbType.NVarChar, 30).Value = Db.ONulo(cambio.Campo)
                    cmd.Parameters.Add("@ValorAnterior", SqlDbType.NVarChar, LongitudValor).Value = Db.ONulo(Recortar(cambio.ValorAnterior))
                    cmd.Parameters.Add("@ValorNuevo", SqlDbType.NVarChar, LongitudValor).Value = Db.ONulo(Recortar(cambio.ValorNuevo))
                    cmd.ExecuteNonQuery()
                End Using
            Next
        End Sub

        Friend Shared Sub Registrar(cn As SqlConnection, tx As SqlTransaction, tarjetaId As Integer, usuarioId As Integer,
                                    cambio As HistorialTarjeta)
            Registrar(cn, tx, tarjetaId, usuarioId, {cambio})
        End Sub

        ''' <summary>Extracto para textos largos (descripción, código, comentarios).</summary>
        Friend Shared Function Extracto(texto As String, Optional longitud As Integer = 200) As String
            If String.IsNullOrEmpty(texto) Then Return Nothing
            Dim limpio = texto.Trim()
            Return If(limpio.Length > longitud, limpio.Substring(0, longitud) & "…", limpio)
        End Function

        Private Shared Function Recortar(valor As String) As String
            If valor Is Nothing OrElse valor.Length <= LongitudValor Then Return valor
            Return valor.Substring(0, LongitudValor - 1) & "…"
        End Function

    End Class

End Namespace
