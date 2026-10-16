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
