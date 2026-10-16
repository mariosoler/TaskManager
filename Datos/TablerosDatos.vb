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
SELECT t.Id, t.Nombre, t.Descripcion, t.PropietarioId, t.FechaCreacion,
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

        ''' <summary>Elimina el tablero (y en cascada su contenido). Solo el propietario puede hacerlo.</summary>
        Public Shared Function Eliminar(tableroId As Integer, propietarioId As Integer) As Boolean
            Const sql = "DELETE FROM Tableros WHERE Id = @TableroId AND PropietarioId = @PropietarioId"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@PropietarioId", SqlDbType.Int).Value = propietarioId
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

        ''' <summary>Quita un miembro (nunca al propietario) y desasigna sus tarjetas del tablero.</summary>
        Public Shared Sub QuitarMiembro(tableroId As Integer, usuarioId As Integer)
            Const sql = "
SET XACT_ABORT ON;
BEGIN TRAN;
DELETE m FROM MiembrosTablero m
JOIN Tableros t ON t.Id = m.TableroId
WHERE m.TableroId = @TableroId AND m.UsuarioId = @UsuarioId AND t.PropietarioId <> @UsuarioId;
IF @@ROWCOUNT > 0
    UPDATE c SET AsignadoA = NULL
    FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
    WHERE l.TableroId = @TableroId AND c.AsignadoA = @UsuarioId;
COMMIT;"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
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
                .Rol = rd.GetString(rd.GetOrdinal("Rol"))
            }
        End Function

    End Class

End Namespace
