Imports System.Data.SqlClient
Imports TaskManager.Models

Namespace Datos

    Public NotInheritable Class UsuariosDatos

        Private Sub New()
        End Sub

        Public Shared Function ObtenerPorEmail(email As String) As Usuario
            Const sql = "SELECT Id, Nombre, Email, PasswordHash, FechaCreacion FROM Usuarios WHERE Email = @Email"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 200).Value = email
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    If Not rd.Read() Then Return Nothing
                    Return New Usuario With {
                        .Id = rd.GetInt32(0),
                        .Nombre = rd.GetString(1),
                        .Email = rd.GetString(2),
                        .PasswordHash = rd.GetString(3),
                        .FechaCreacion = rd.GetDateTime(4)
                    }
                End Using
            End Using
        End Function

        ''' <summary>Perfil del usuario (sin el hash de la contraseña), o Nothing si no existe.</summary>
        Public Shared Function ObtenerPorId(usuarioId As Integer) As Usuario
            Const sql = "SELECT Id, Nombre, Email, FechaCreacion, FormatoFecha FROM Usuarios WHERE Id = @UsuarioId"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    If Not rd.Read() Then Return Nothing
                    Return New Usuario With {
                        .Id = rd.GetInt32(0),
                        .Nombre = rd.GetString(1),
                        .Email = rd.GetString(2),
                        .FechaCreacion = rd.GetDateTime(3),
                        .FormatoFecha = Db.Texto(rd, "FormatoFecha")
                    }
                End Using
            End Using
        End Function

        ''' <summary>Cambia el nombre y el formato de fecha (Nothing = predeterminado) del usuario.</summary>
        Public Shared Sub ActualizarPerfil(usuarioId As Integer, nombre As String, formatoFecha As String)
            Const sql = "UPDATE Usuarios SET Nombre = @Nombre, FormatoFecha = @FormatoFecha WHERE Id = @UsuarioId"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cmd.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = nombre
                cmd.Parameters.Add("@FormatoFecha", SqlDbType.VarChar, 20).Value = Db.ONulo(formatoFecha)
                cn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        ''' <summary>
        ''' Usuarios con los que comparte algún tablero (incluido él mismo): los que puede elegir
        ''' en los filtros del buscador.
        ''' </summary>
        Public Shared Function ListarCompaneros(usuarioId As Integer) As List(Of Miembro)
            Const sql = "
WITH Accesibles AS (
    SELECT t.Id FROM Tableros t
    WHERE t.PropietarioId = @UsuarioId
       OR EXISTS (SELECT 1 FROM MiembrosTablero m WHERE m.TableroId = t.Id AND m.UsuarioId = @UsuarioId)
)
SELECT u.Id, u.Nombre, u.Email FROM Usuarios u
WHERE u.Id = @UsuarioId
   OR EXISTS (SELECT 1 FROM Tableros t JOIN Accesibles a ON a.Id = t.Id WHERE t.PropietarioId = u.Id)
   OR EXISTS (SELECT 1 FROM MiembrosTablero m JOIN Accesibles a ON a.Id = m.TableroId WHERE m.UsuarioId = u.Id)
ORDER BY u.Nombre"

            Dim usuarios As New List(Of Miembro)
            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        usuarios.Add(New Miembro With {.UsuarioId = rd.GetInt32(0), .Nombre = rd.GetString(1), .Email = rd.GetString(2)})
                    End While
                End Using
            End Using
            Return usuarios
        End Function

        ''' <summary>
        ''' Usuarios que se pueden añadir al tablero cuyo nombre o email contiene <paramref name="texto"/>
        ''' (sin distinguir mayúsculas ni acentos); primero los que empiezan por él. Excluye al propietario
        ''' y a los miembros actuales. La autorización (propietario o Admin) la comprueba quien llama.
        ''' </summary>
        Public Shared Function SugerirParaTablero(tableroId As Integer, texto As String, maximo As Integer) As List(Of Miembro)
            Const sql = "
SELECT TOP (@Maximo) u.Id, u.Nombre, u.Email
FROM Usuarios u
WHERE (u.Nombre COLLATE Latin1_General_CI_AI LIKE @Contiene ESCAPE '\'
       OR u.Email COLLATE Latin1_General_CI_AI LIKE @Contiene ESCAPE '\')
  AND NOT EXISTS (SELECT 1 FROM Tableros t WHERE t.Id = @TableroId AND t.PropietarioId = u.Id)
  AND NOT EXISTS (SELECT 1 FROM MiembrosTablero m WHERE m.TableroId = @TableroId AND m.UsuarioId = u.Id)
ORDER BY CASE WHEN u.Nombre COLLATE Latin1_General_CI_AI LIKE @Empieza ESCAPE '\'
                OR u.Email COLLATE Latin1_General_CI_AI LIKE @Empieza ESCAPE '\' THEN 0 ELSE 1 END,
         u.Nombre, u.Email"

            Dim patron = BusquedaDatos.EscaparLike(texto)
            Dim usuarios As New List(Of Miembro)
            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@Maximo", SqlDbType.Int).Value = maximo
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@Contiene", SqlDbType.NVarChar, 210).Value = "%" & patron & "%"
                cmd.Parameters.Add("@Empieza", SqlDbType.NVarChar, 210).Value = patron & "%"
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        usuarios.Add(New Miembro With {.UsuarioId = rd.GetInt32(0), .Nombre = rd.GetString(1), .Email = rd.GetString(2)})
                    End While
                End Using
            End Using
            Return usuarios
        End Function

        ''' <summary>Crea el usuario y devuelve su Id, o Nothing si el email ya está registrado.</summary>
        Public Shared Function Crear(nombre As String, email As String, passwordHash As String) As Integer?
            Const sql = "INSERT INTO Usuarios (Nombre, Email, PasswordHash) OUTPUT INSERTED.Id VALUES (@Nombre, @Email, @PasswordHash)"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = nombre
                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 200).Value = email
                cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 500).Value = passwordHash
                cn.Open()
                Try
                    Return CInt(cmd.ExecuteScalar())
                Catch ex As SqlException When Db.EsDuplicado(ex)
                    Return Nothing
                End Try
            End Using
        End Function

    End Class

End Namespace
