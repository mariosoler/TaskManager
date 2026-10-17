Imports System.Data.SqlClient
Imports TaskManager.Models

Namespace Datos

    ''' <summary>
    ''' Búsquedas guardadas del buscador. Cada usuario ve las suyas y las compartidas por sus compañeros
    ''' (usuarios con algún tablero en común). Compartir una búsqueda no da acceso a nada: al aplicarla,
    ''' BusquedaDatos sigue limitando los resultados a los tableros de quien busca.
    ''' </summary>
    Public NotInheritable Class BusquedasGuardadasDatos

        Public Const LongitudMaximaNombre As Integer = 100
        Public Const LongitudMaximaCriterios As Integer = 4000

        Private Sub New()
        End Sub

        ' Búsquedas visibles para @UsuarioId: propias o compartidas por alguien con quien comparte tablero.
        Private Const SqlVisibles As String = "
WITH Accesibles AS (
    SELECT t.Id FROM Tableros t
    WHERE t.PropietarioId = @UsuarioId
       OR EXISTS (SELECT 1 FROM MiembrosTablero m WHERE m.TableroId = t.Id AND m.UsuarioId = @UsuarioId)
)
SELECT b.Id, b.UsuarioId, u.Nombre AS NombreUsuario, b.Nombre, b.Criterios, b.Compartida, b.FechaCreacion
FROM BusquedasGuardadas b
JOIN Usuarios u ON u.Id = b.UsuarioId
WHERE (b.UsuarioId = @UsuarioId
       OR (b.Compartida = 1
           AND (EXISTS (SELECT 1 FROM Tableros t JOIN Accesibles a ON a.Id = t.Id WHERE t.PropietarioId = b.UsuarioId)
                OR EXISTS (SELECT 1 FROM MiembrosTablero m JOIN Accesibles a ON a.Id = m.TableroId WHERE m.UsuarioId = b.UsuarioId))))"

        ''' <summary>Propias primero y luego las compartidas, por nombre.</summary>
        Public Shared Function Listar(usuarioId As Integer) As List(Of BusquedaGuardada)
            Dim sql = SqlVisibles & " ORDER BY CASE WHEN b.UsuarioId = @UsuarioId THEN 0 ELSE 1 END, b.Nombre, b.Id"
            Dim busquedas As New List(Of BusquedaGuardada)

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        busquedas.Add(Leer(rd, usuarioId))
                    End While
                End Using
            End Using
            Return busquedas
        End Function

        ''' <summary>Devuelve la búsqueda si el usuario puede verla; Nothing en caso contrario.</summary>
        Public Shared Function Obtener(busquedaId As Integer, usuarioId As Integer) As BusquedaGuardada
            Dim sql = SqlVisibles & " AND b.Id = @BusquedaId"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cmd.Parameters.Add("@BusquedaId", SqlDbType.Int).Value = busquedaId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    Return If(rd.Read(), Leer(rd, usuarioId), Nothing)
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Guarda la búsqueda del usuario. Si ya tiene una con ese nombre, la sustituye (criterios y si es compartida).
        ''' Devuelve el Id.
        ''' </summary>
        Public Shared Function Guardar(usuarioId As Integer, nombre As String, criterios As String, compartida As Boolean) As Integer
            Const sql = "
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @Id INT = (SELECT Id FROM BusquedasGuardadas WITH (UPDLOCK, HOLDLOCK)
                   WHERE UsuarioId = @UsuarioId AND Nombre = @Nombre);
IF @Id IS NULL
BEGIN
    INSERT INTO BusquedasGuardadas (UsuarioId, Nombre, Criterios, Compartida) VALUES (@UsuarioId, @Nombre, @Criterios, @Compartida);
    SET @Id = SCOPE_IDENTITY();
END
ELSE
    UPDATE BusquedasGuardadas SET Criterios = @Criterios, Compartida = @Compartida WHERE Id = @Id;
COMMIT;
SELECT @Id;"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cmd.Parameters.Add("@Nombre", SqlDbType.NVarChar, LongitudMaximaNombre).Value = nombre
                cmd.Parameters.Add("@Criterios", SqlDbType.NVarChar, LongitudMaximaCriterios).Value = criterios
                cmd.Parameters.Add("@Compartida", SqlDbType.Bit).Value = compartida
                cn.Open()
                Return CInt(cmd.ExecuteScalar())
            End Using
        End Function

        ''' <summary>Elimina la búsqueda. Solo su autor puede hacerlo.</summary>
        Public Shared Function Eliminar(busquedaId As Integer, usuarioId As Integer) As Boolean
            Const sql = "DELETE FROM BusquedasGuardadas WHERE Id = @BusquedaId AND UsuarioId = @UsuarioId"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@BusquedaId", SqlDbType.Int).Value = busquedaId
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cn.Open()
                Return cmd.ExecuteNonQuery() > 0
            End Using
        End Function

        Private Shared Function Leer(rd As SqlDataReader, usuarioId As Integer) As BusquedaGuardada
            Dim autorId = rd.GetInt32(rd.GetOrdinal("UsuarioId"))
            Return New BusquedaGuardada With {
                .Id = rd.GetInt32(rd.GetOrdinal("Id")),
                .UsuarioId = autorId,
                .NombreUsuario = rd.GetString(rd.GetOrdinal("NombreUsuario")),
                .Nombre = rd.GetString(rd.GetOrdinal("Nombre")),
                .Criterios = rd.GetString(rd.GetOrdinal("Criterios")),
                .Compartida = rd.GetBoolean(rd.GetOrdinal("Compartida")),
                .FechaCreacion = rd.GetDateTime(rd.GetOrdinal("FechaCreacion")),
                .EsPropia = autorId = usuarioId
            }
        End Function

    End Class

End Namespace
