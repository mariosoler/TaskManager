Imports System.Data.SqlClient
Imports TaskManager.Models

Namespace Datos

    Public Enum ResultadoMiembro
        Agregado = 0
        UsuarioNoExiste = 1
        YaEsMiembro = 2
    End Enum

    Public NotInheritable Class TablerosDatos

        Private Sub New()
        End Sub

        ' Tableros a los que tiene acceso @UsuarioId (propietario o miembro), con su rol.
        Private Const SqlTablerosDeUsuario As String = "
SELECT t.Id, t.Nombre, t.Descripcion, t.PropietarioId, t.FechaCreacion, t.Fondo,
       CASE WHEN t.PropietarioId = @UsuarioId THEN N'Propietario' ELSE m.Rol END AS Rol
FROM Tableros t
LEFT JOIN MiembrosTablero m ON m.TableroId = t.Id AND m.UsuarioId = @UsuarioId
WHERE (t.PropietarioId = @UsuarioId OR m.UsuarioId IS NOT NULL)"

        Public Shared Function ListarDeUsuario(usuarioId As Integer) As List(Of Tablero)
            Dim sql = SqlTablerosDeUsuario & " ORDER BY t.FechaCreacion DESC, t.Id DESC"
            Dim tableros As New List(Of Tablero)

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        tableros.Add(LeerTablero(rd))
                    End While
                End Using
            End Using
            Return tableros
        End Function

        ''' <summary>Devuelve el tablero si el usuario tiene acceso; Nothing en caso contrario.</summary>
        Public Shared Function Obtener(tableroId As Integer, usuarioId As Integer) As Tablero
            Dim sql = SqlTablerosDeUsuario & " AND t.Id = @TableroId"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    Return If(rd.Read(), LeerTablero(rd), Nothing)
                End Using
            End Using
        End Function

        ''' <summary>Crea el tablero con su propietario como Admin y tres listas iniciales.</summary>
        Public Shared Function Crear(nombre As String, descripcion As String, propietarioId As Integer) As Integer
            Const sql = "
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @Id INT;
INSERT INTO Tableros (Nombre, Descripcion, PropietarioId) VALUES (@Nombre, @Descripcion, @PropietarioId);
SET @Id = SCOPE_IDENTITY();
INSERT INTO MiembrosTablero (TableroId, UsuarioId, Rol) VALUES (@Id, @PropietarioId, N'Admin');
INSERT INTO Listas (TableroId, Titulo, Posicion) VALUES (@Id, N'Por hacer', 0), (@Id, N'En curso', 1), (@Id, N'Hecho', 2);
COMMIT;
SELECT @Id;"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@Nombre", SqlDbType.NVarChar, 150).Value = nombre
                cmd.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 500).Value = Db.ONulo(descripcion)
                cmd.Parameters.Add("@PropietarioId", SqlDbType.Int).Value = propietarioId
                cn.Open()
                Return CInt(cmd.ExecuteScalar())
            End Using
        End Function

        ''' <summary>
        ''' Elimina el tablero (solo su propietario) con todo su contenido. En <paramref name="archivosAdjuntos"/> devuelve
        ''' los archivos de los adjuntos de sus tarjetas, para borrarlos después.
        ''' </summary>
        Public Shared Function Eliminar(tableroId As Integer, propietarioId As Integer, ByRef archivosAdjuntos As List(Of String)) As Boolean
            Const sql = "
SET XACT_ABORT ON;
BEGIN TRAN;
" & AdjuntosDatos.SqlDeclararAdjuntos & "
INSERT INTO @Adjuntos
SELECT f.Archivo FROM AdjuntosTarjeta f JOIN Tarjetas c ON c.Id = f.TarjetaId JOIN Listas l ON l.Id = c.ListaId
JOIN Tableros t ON t.Id = l.TableroId
WHERE t.Id = @TableroId AND t.PropietarioId = @PropietarioId;
DELETE FROM Tableros WHERE Id = @TableroId AND PropietarioId = @PropietarioId;
DECLARE @Borrado BIT = CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END;
COMMIT;
SELECT @Borrado;
SELECT Archivo FROM @Adjuntos;"

            archivosAdjuntos = New List(Of String)
            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@PropietarioId", SqlDbType.Int).Value = propietarioId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    rd.Read()
                    Dim borrado = rd.GetBoolean(0)
                    rd.NextResult()
                    While rd.Read()
                        archivosAdjuntos.Add(rd.GetString(0))
                    End While
                    Return borrado
                End Using
            End Using
        End Function

        ''' <summary>Cambia el nombre y la descripción del tablero. Solo propietario o Admin; devuelve True si se cambió.</summary>
        Public Shared Function CambiarNombre(tableroId As Integer, nombre As String, descripcion As String, usuarioId As Integer) As Boolean
            Const sql = "
UPDATE t SET Nombre = @Nombre, Descripcion = @Descripcion
FROM Tableros t
WHERE t.Id = @TableroId
  AND (t.PropietarioId = @UsuarioId
       OR EXISTS (SELECT 1 FROM MiembrosTablero m WHERE m.TableroId = t.Id AND m.UsuarioId = @UsuarioId AND m.Rol = N'Admin'))"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@Nombre", SqlDbType.NVarChar, 150).Value = nombre
                cmd.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 500).Value = Db.ONulo(descripcion)
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cn.Open()
                Return cmd.ExecuteNonQuery() > 0
            End Using
        End Function

        ''' <summary>
        ''' Cambia el fondo del tablero (Nothing = quitarlo). Solo propietario o Admin.
        ''' Devuelve True si se cambió; el archivo anterior lo borra quien llama.
        ''' </summary>
        Public Shared Function CambiarFondo(tableroId As Integer, fondo As String, usuarioId As Integer) As Boolean
            Const sql = "
UPDATE t SET Fondo = @Fondo
FROM Tableros t
WHERE t.Id = @TableroId
  AND (t.PropietarioId = @UsuarioId
       OR EXISTS (SELECT 1 FROM MiembrosTablero m WHERE m.TableroId = t.Id AND m.UsuarioId = @UsuarioId AND m.Rol = N'Admin'))"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@Fondo", SqlDbType.NVarChar, 100).Value = Db.ONulo(fondo)
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cn.Open()
                Return cmd.ExecuteNonQuery() > 0
            End Using
        End Function

        Public Shared Function ListarMiembros(tableroId As Integer) As List(Of Miembro)
            Const sql = "
SELECT u.Id, u.Nombre, u.Email,
       CASE WHEN t.PropietarioId = u.Id THEN N'Propietario' ELSE m.Rol END AS Rol
FROM Tableros t
JOIN Usuarios u ON u.Id = t.PropietarioId
                OR EXISTS (SELECT 1 FROM MiembrosTablero x WHERE x.TableroId = t.Id AND x.UsuarioId = u.Id)
LEFT JOIN MiembrosTablero m ON m.TableroId = t.Id AND m.UsuarioId = u.Id
WHERE t.Id = @TableroId
ORDER BY CASE WHEN t.PropietarioId = u.Id THEN 0 ELSE 1 END, u.Nombre"

            Dim miembros As New List(Of Miembro)
            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        miembros.Add(New Miembro With {
                            .UsuarioId = rd.GetInt32(0),
                            .Nombre = rd.GetString(1),
                            .Email = rd.GetString(2),
                            .Rol = rd.GetString(3)
                        })
                    End While
                End Using
            End Using
            Return miembros
        End Function

        Public Shared Function AgregarMiembro(tableroId As Integer, email As String) As ResultadoMiembro
            Const sql = "
DECLARE @UsuarioId INT = (SELECT Id FROM Usuarios WHERE Email = @Email);
IF @UsuarioId IS NULL
    SELECT 1;
ELSE IF EXISTS (SELECT 1 FROM MiembrosTablero WHERE TableroId = @TableroId AND UsuarioId = @UsuarioId)
     OR EXISTS (SELECT 1 FROM Tableros WHERE Id = @TableroId AND PropietarioId = @UsuarioId)
    SELECT 2;
ELSE
BEGIN
    INSERT INTO MiembrosTablero (TableroId, UsuarioId, Rol) VALUES (@TableroId, @UsuarioId, N'Miembro');
    SELECT 0;
END"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 200).Value = email
                cn.Open()
                Try
                    Return CType(CInt(cmd.ExecuteScalar()), ResultadoMiembro)
                Catch ex As SqlException When Db.EsDuplicado(ex)
                    Return ResultadoMiembro.YaEsMiembro
                End Try
            End Using
        End Function

        ''' <summary>
        ''' Quita un miembro (nunca al propietario) y desasigna sus tarjetas del tablero,
        ''' dejando constancia en el historial de cada tarjeta a nombre de <paramref name="actorId"/>.
        ''' </summary>
        Public Shared Sub QuitarMiembro(tableroId As Integer, usuarioId As Integer, actorId As Integer)
            Const sql = "
SET XACT_ABORT ON;
BEGIN TRAN;
DELETE m FROM MiembrosTablero m
JOIN Tableros t ON t.Id = m.TableroId
WHERE m.TableroId = @TableroId AND m.UsuarioId = @UsuarioId AND t.PropietarioId <> @UsuarioId;
IF @@ROWCOUNT > 0
BEGIN
    DECLARE @Ahora DATETIME2 = SYSUTCDATETIME();
    DECLARE @Afectadas TABLE (Id INT PRIMARY KEY);
    INSERT INTO @Afectadas
    SELECT c.Id FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
    WHERE l.TableroId = @TableroId AND c.AsignadoA = @UsuarioId;

    INSERT INTO HistorialTarjeta (TarjetaId, UsuarioId, Fecha, Accion, Campo, ValorAnterior, ValorNuevo)
    SELECT a.Id, @ActorId, @Ahora, N'Editada', N'AsignadoA', u.Nombre, NULL
    FROM @Afectadas a CROSS JOIN Usuarios u WHERE u.Id = @UsuarioId;

    UPDATE c SET AsignadoA = NULL, FechaModificacion = @Ahora, ModificadoPor = @ActorId
    FROM Tarjetas c JOIN @Afectadas a ON a.Id = c.Id;
END
COMMIT;"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cmd.Parameters.Add("@ActorId", SqlDbType.Int).Value = actorId
                cn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Shared Function LeerTablero(rd As SqlDataReader) As Tablero
            Return New Tablero With {
                .Id = rd.GetInt32(rd.GetOrdinal("Id")),
                .Nombre = rd.GetString(rd.GetOrdinal("Nombre")),
                .Descripcion = Db.Texto(rd, "Descripcion"),
                .PropietarioId = rd.GetInt32(rd.GetOrdinal("PropietarioId")),
                .FechaCreacion = rd.GetDateTime(rd.GetOrdinal("FechaCreacion")),
                .Fondo = Db.Texto(rd, "Fondo"),
                .Rol = rd.GetString(rd.GetOrdinal("Rol"))
            }
        End Function

    End Class

End Namespace
