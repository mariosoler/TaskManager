Imports System.Data.SqlClient
Imports TaskManager.Models

Namespace Datos

    ''' <summary>
    ''' Historial de cambios de las tarjetas. <see cref="Registrar"/> se llama siempre dentro de la
    ''' transacción de la operación que modifica la tarjeta, para que historial y datos no diverjan.
    ''' </summary>
    Public NotInheritable Class HistorialDatos

        Private Const LongitudValor As Integer = 500

        ''' <summary>
        ''' Minutos durante los que las ediciones seguidas de un mismo campo por el mismo usuario se agrupan
        ''' en una sola fila (el guardado automático del detalle guarda en cada pausa al escribir).
        ''' </summary>
        Private Const MinutosAgrupacion As Integer = 10

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
        ''' Una edición de un campo se agrupa con la anterior del mismo campo si es del mismo usuario, de hace
        ''' menos de <see cref="MinutosAgrupacion"/> minutos y nadie más ha tocado la tarjeta desde entonces:
        ''' se conserva el valor anterior original y se actualizan el valor nuevo y la fecha. Si con ello el
        ''' campo vuelve a su valor original, la fila se elimina (salvo descripción y código, que guardan
        ''' solo un extracto y no se pueden comparar con seguridad).
        ''' </summary>
        Friend Shared Sub Registrar(cn As SqlConnection, tx As SqlTransaction, tarjetaId As Integer, usuarioId As Integer,
                                    cambios As IEnumerable(Of HistorialTarjeta))
            Const sqlTarjeta = "UPDATE Tarjetas SET FechaModificacion = @Fecha, ModificadoPor = @UsuarioId WHERE Id = @TarjetaId"
            Const sqlCambio = "
DECLARE @Id INT = NULL;
IF @Agrupar = 1
    SELECT TOP (1) @Id = h.Id
    FROM HistorialTarjeta h
    WHERE h.TarjetaId = @TarjetaId AND h.UsuarioId = @UsuarioId AND h.Accion = @Accion AND h.Campo = @Campo
      AND h.Fecha >= DATEADD(MINUTE, -@Minutos, @Fecha)
      AND NOT EXISTS (SELECT 1 FROM HistorialTarjeta o
                      WHERE o.TarjetaId = @TarjetaId AND o.Id > h.Id AND o.UsuarioId <> @UsuarioId)
    ORDER BY h.Id DESC;

IF @Id IS NULL
    INSERT INTO HistorialTarjeta (TarjetaId, UsuarioId, Fecha, Accion, Campo, ValorAnterior, ValorNuevo)
    VALUES (@TarjetaId, @UsuarioId, @Fecha, @Accion, @Campo, @ValorAnterior, @ValorNuevo);
ELSE IF @BorrarSiVuelve = 1 AND EXISTS (SELECT 1 FROM HistorialTarjeta WHERE Id = @Id
         AND ISNULL(ValorAnterior, N'') COLLATE Latin1_General_BIN2 = ISNULL(@ValorNuevo, N'') COLLATE Latin1_General_BIN2)
    DELETE FROM HistorialTarjeta WHERE Id = @Id;
ELSE
    UPDATE HistorialTarjeta SET ValorNuevo = @ValorNuevo, Fecha = @Fecha WHERE Id = @Id;"

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
                    Dim agrupar = cambio.Accion = HistorialTarjeta.Editada AndAlso cambio.Campo IsNot Nothing
                    Dim esExtracto = cambio.Campo = HistorialTarjeta.CampoDescripcion OrElse cambio.Campo = HistorialTarjeta.CampoCodigo
                    cmd.Parameters.Add("@Agrupar", SqlDbType.Bit).Value = agrupar
                    cmd.Parameters.Add("@BorrarSiVuelve", SqlDbType.Bit).Value = agrupar AndAlso Not esExtracto
                    cmd.Parameters.Add("@Minutos", SqlDbType.Int).Value = MinutosAgrupacion
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
