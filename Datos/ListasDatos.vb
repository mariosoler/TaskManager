Imports System.Data.SqlClient
Imports TaskManager.Models

Namespace Datos

    ''' <summary>
    ''' Operaciones sobre listas. Todas reciben el TableroId ya autorizado y lo usan en el WHERE,
    ''' de modo que un Id manipulado de otro tablero no tiene efecto.
    ''' </summary>
    Public NotInheritable Class ListasDatos

        Private Sub New()
        End Sub

        ''' <summary>Listas del tablero ordenadas por posición, cada una con sus tarjetas.</summary>
        Public Shared Function ListarConTarjetas(tableroId As Integer) As List(Of Lista)
            Dim sql = "SELECT Id, TableroId, Titulo, Posicion FROM Listas WHERE TableroId = @TableroId ORDER BY Posicion, Id;" &
                      TarjetasDatos.SqlSelectTarjetas & " WHERE l.TableroId = @TableroId ORDER BY c.ListaId, c.Posicion, c.Id;" &
                      EtiquetasDatos.SqlEtiquetasDeTarjetas & " ORDER BY e.Nombre;"

            Dim listas As New List(Of Lista)
            Dim porId As New Dictionary(Of Integer, Lista)
            Dim tarjetas As New Dictionary(Of Integer, Tarjeta)

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        Dim lista As New Lista With {
                            .Id = rd.GetInt32(0),
                            .TableroId = rd.GetInt32(1),
                            .Titulo = rd.GetString(2),
                            .Posicion = rd.GetInt32(3)
                        }
                        listas.Add(lista)
                        porId(lista.Id) = lista
                    End While

                    rd.NextResult()
                    While rd.Read()
                        Dim tarjeta = TarjetasDatos.LeerTarjeta(rd)
                        tarjetas(tarjeta.Id) = tarjeta
                        Dim lista As Lista = Nothing
                        If porId.TryGetValue(tarjeta.ListaId, lista) Then lista.Tarjetas.Add(tarjeta)
                    End While

                    rd.NextResult()
                    EtiquetasDatos.LeerEnTarjetas(rd, tarjetas)
                End Using
            End Using
            Return listas
        End Function

        ''' <summary>Listas del tablero (sin tarjetas), ordenadas por posición.</summary>
        Public Shared Function Listar(tableroId As Integer) As List(Of Lista)
            Const sql = "SELECT Id, TableroId, Titulo, Posicion FROM Listas WHERE TableroId = @TableroId ORDER BY Posicion, Id"

            Dim listas As New List(Of Lista)
            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        listas.Add(New Lista With {.Id = rd.GetInt32(0), .TableroId = rd.GetInt32(1), .Titulo = rd.GetString(2), .Posicion = rd.GetInt32(3)})
                    End While
                End Using
            End Using
            Return listas
        End Function

        ''' <summary>Título de la lista si pertenece al tablero; Nothing en caso contrario.</summary>
        Public Shared Function ObtenerTitulo(listaId As Integer, tableroId As Integer) As String
            Const sql = "SELECT Titulo FROM Listas WHERE Id = @ListaId AND TableroId = @TableroId"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@ListaId", SqlDbType.Int).Value = listaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Return TryCast(cmd.ExecuteScalar(), String)
            End Using
        End Function

        Public Shared Sub Crear(tableroId As Integer, titulo As String)
            Const sql = "
INSERT INTO Listas (TableroId, Titulo, Posicion)
SELECT @TableroId, @Titulo, ISNULL(MAX(Posicion) + 1, 0) FROM Listas WHERE TableroId = @TableroId"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@Titulo", SqlDbType.NVarChar, 150).Value = titulo
                cn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Public Shared Sub Eliminar(listaId As Integer, tableroId As Integer)
            Dim sql = "
SET XACT_ABORT ON;
BEGIN TRAN;
DELETE FROM Listas WHERE Id = @ListaId AND TableroId = @TableroId;" &
                Db.SqlRenumerarListas & "
COMMIT;"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@ListaId", SqlDbType.Int).Value = listaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        ''' <summary>Intercambia la lista con su vecina (direccion = -1 izquierda, +1 derecha).</summary>
        Public Shared Sub Mover(listaId As Integer, tableroId As Integer, direccion As Integer)
            Dim sql = "
SET XACT_ABORT ON;
BEGIN TRAN;" &
                Db.SqlRenumerarListas & "
DECLARE @Pos INT = (SELECT Posicion FROM Listas WHERE Id = @ListaId AND TableroId = @TableroId);
DECLARE @Destino INT = @Pos + @Direccion;
IF @Pos IS NOT NULL AND @Destino >= 0
   AND @Destino < (SELECT COUNT(*) FROM Listas WHERE TableroId = @TableroId)
    UPDATE Listas SET Posicion = CASE WHEN Id = @ListaId THEN @Destino ELSE @Pos END
    WHERE TableroId = @TableroId AND (Id = @ListaId OR Posicion = @Destino);
COMMIT;"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@ListaId", SqlDbType.Int).Value = listaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@Direccion", SqlDbType.Int).Value = Math.Sign(direccion)
                cn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Sub

    End Class

End Namespace
