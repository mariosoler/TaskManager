Imports System.Data.SqlClient
Imports TaskManager.Models

Namespace Datos

    ''' <summary>
    ''' Adjuntos de las tarjetas (tabla AdjuntosTarjeta). Todas las operaciones comprueban que la tarjeta pertenece al
    ''' TableroId ya autorizado. Añadir o eliminar un adjunto se registra en el historial de la tarjeta.
    ''' Los archivos los guarda y borra Utilidades.AdjuntosTarjeta; aquí solo se guarda su nombre.
    ''' </summary>
    Public NotInheritable Class AdjuntosDatos

        Private Sub New()
        End Sub

        Private Const SqlColumnas As String = "
SELECT a.Id, a.TarjetaId, a.Archivo, a.Nombre, a.EsImagen, a.Tamano, a.SubidoPor, u.Nombre AS NombreSubidoPor, a.FechaSubida
FROM AdjuntosTarjeta a
JOIN Tarjetas c ON c.Id = a.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
JOIN Usuarios u ON u.Id = a.SubidoPor"

        ''' <summary>
        ''' Tabla @Adjuntos (Archivo) de los lotes que eliminan tarjetas, listas o tableros: se rellena antes del DELETE
        ''' y se devuelve al final para borrar después los archivos (ver LeerArchivos).
        ''' </summary>
        Friend Const SqlDeclararAdjuntos As String = "DECLARE @Adjuntos TABLE (Archivo NVARCHAR(100));"

        Public Shared Function Listar(tarjetaId As Integer, tableroId As Integer) As List(Of AdjuntoTarjeta)
            Const sql = SqlColumnas & "
WHERE a.TarjetaId = @TarjetaId AND l.TableroId = @TableroId
ORDER BY a.FechaSubida, a.Id"

            Dim adjuntos As New List(Of AdjuntoTarjeta)
            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        adjuntos.Add(Leer(rd))
                    End While
                End Using
            End Using
            Return adjuntos
        End Function

        ''' <summary>Adjunto si <paramref name="usuarioId"/> es propietario o miembro de su tablero (para servirlo); si no, Nothing.</summary>
        Public Shared Function ObtenerParaUsuario(adjuntoId As Integer, usuarioId As Integer) As AdjuntoTarjeta
            Const sql = SqlColumnas & "
JOIN Tableros t ON t.Id = l.TableroId
WHERE a.Id = @AdjuntoId
  AND (t.PropietarioId = @UsuarioId
       OR EXISTS (SELECT 1 FROM MiembrosTablero m WHERE m.TableroId = t.Id AND m.UsuarioId = @UsuarioId))"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@AdjuntoId", SqlDbType.Int).Value = adjuntoId
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    Return If(rd.Read(), Leer(rd), Nothing)
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Añade el adjunto (archivo ya guardado) si la tarjeta es del tablero y no ha llegado al máximo.
        ''' Devuelve False si no se ha añadido (quien llama borra entonces el archivo).
        ''' </summary>
        Public Shared Function Crear(tarjetaId As Integer, tableroId As Integer, adjunto As AdjuntoTarjeta,
                                     usuarioId As Integer, maximo As Integer) As Boolean
            ' El bloqueo evita que dos subidas a la vez pasen del máximo.
            Const sql = "
INSERT INTO AdjuntosTarjeta (TarjetaId, Archivo, Nombre, EsImagen, Tamano, SubidoPor)
OUTPUT INSERTED.Id
SELECT c.Id, @Archivo, @Nombre, @EsImagen, @Tamano, @UsuarioId
FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId
  AND (SELECT COUNT(*) FROM AdjuntosTarjeta WITH (UPDLOCK, HOLDLOCK) WHERE TarjetaId = c.Id) < @Maximo"

            Using cn = Db.CrearConexion()
                cn.Open()
                Using tx = cn.BeginTransaction()
                    Using cmd As New SqlCommand(sql, cn, tx)
                        cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                        cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                        cmd.Parameters.Add("@Archivo", SqlDbType.NVarChar, 100).Value = adjunto.Archivo
                        cmd.Parameters.Add("@Nombre", SqlDbType.NVarChar, 255).Value = adjunto.Nombre
                        cmd.Parameters.Add("@EsImagen", SqlDbType.Bit).Value = adjunto.EsImagen
                        cmd.Parameters.Add("@Tamano", SqlDbType.Int).Value = adjunto.Tamano
                        cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                        cmd.Parameters.Add("@Maximo", SqlDbType.Int).Value = maximo
                        If cmd.ExecuteScalar() Is Nothing Then Return False
                    End Using
                    HistorialDatos.Registrar(cn, tx, tarjetaId, usuarioId,
                                             HistorialTarjeta.Nuevo(HistorialTarjeta.AdjuntoAgregado, valorNuevo:=adjunto.Nombre))
                    tx.Commit()
                End Using
            End Using
            Return True
        End Function

        ''' <summary>Elimina el adjunto si su tarjeta es del tablero; devuelve el nombre del archivo a borrar o Nothing.</summary>
        Public Shared Function Eliminar(adjuntoId As Integer, tarjetaId As Integer, tableroId As Integer, usuarioId As Integer) As String
            Const sql = "
DELETE a
OUTPUT DELETED.Archivo, DELETED.Nombre
FROM AdjuntosTarjeta a
JOIN Tarjetas c ON c.Id = a.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
WHERE a.Id = @AdjuntoId AND a.TarjetaId = @TarjetaId AND l.TableroId = @TableroId"

            Using cn = Db.CrearConexion()
                cn.Open()
                Using tx = cn.BeginTransaction()
                    Dim archivo As String = Nothing, nombre As String = Nothing
                    Using cmd As New SqlCommand(sql, cn, tx)
                        cmd.Parameters.Add("@AdjuntoId", SqlDbType.Int).Value = adjuntoId
                        cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                        cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                        Using rd = cmd.ExecuteReader()
                            If rd.Read() Then
                                archivo = rd.GetString(0)
                                nombre = rd.GetString(1)
                            End If
                        End Using
                    End Using
                    If archivo Is Nothing Then Return Nothing
                    HistorialDatos.Registrar(cn, tx, tarjetaId, usuarioId,
                                             HistorialTarjeta.Nuevo(HistorialTarjeta.AdjuntoEliminado, valorNuevo:=nombre))
                    tx.Commit()
                    Return archivo
                End Using
            End Using
        End Function

        ''' <summary>Lee los archivos que devuelve un lote de borrado al final (ver SqlDeclararAdjuntos).</summary>
        Friend Shared Function LeerArchivos(cmd As SqlCommand) As List(Of String)
            Dim archivos As New List(Of String)
            Using rd = cmd.ExecuteReader()
                While rd.Read()
                    archivos.Add(rd.GetString(0))
                End While
            End Using
            Return archivos
        End Function

        Private Shared Function Leer(rd As SqlDataReader) As AdjuntoTarjeta
            Return New AdjuntoTarjeta With {
                .Id = rd.GetInt32(0),
                .TarjetaId = rd.GetInt32(1),
                .Archivo = rd.GetString(2),
                .Nombre = rd.GetString(3),
                .EsImagen = rd.GetBoolean(4),
                .Tamano = rd.GetInt32(5),
                .SubidoPor = rd.GetInt32(6),
                .NombreSubidoPor = rd.GetString(7),
                .FechaSubida = rd.GetDateTime(8)
            }
        End Function

    End Class

End Namespace
