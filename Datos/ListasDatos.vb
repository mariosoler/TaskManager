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

        ''' <summary>
        ''' Listas del tablero ordenadas por posición, cada una con sus tarjetas, con las preferencias de
        ''' <paramref name="usuarioId"/> (oculta y orden): las tarjetas ya vienen en el orden que ha elegido.
        ''' </summary>
        Public Shared Function ListarConTarjetas(tableroId As Integer, usuarioId As Integer) As List(Of Lista)
            Dim sql = "
SELECT l.Id, l.TableroId, l.Titulo, l.Posicion, CAST(ISNULL(p.Oculta, 0) AS BIT) AS Oculta, p.Orden
FROM Listas l
LEFT JOIN PreferenciasLista p ON p.ListaId = l.Id AND p.UsuarioId = @UsuarioId
WHERE l.TableroId = @TableroId ORDER BY l.Posicion, l.Id;" &
                      TarjetasDatos.SqlSelectTarjetas & " WHERE l.TableroId = @TableroId ORDER BY c.ListaId, c.Posicion, c.Id;" &
                      EtiquetasDatos.SqlEtiquetasDeTarjetas & " ORDER BY e.Nombre;"

            Dim listas As New List(Of Lista)
            Dim porId As New Dictionary(Of Integer, Lista)
            Dim tarjetas As New Dictionary(Of Integer, Tarjeta)

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        Dim lista As New Lista With {
                            .Id = rd.GetInt32(0),
                            .TableroId = rd.GetInt32(1),
                            .Titulo = rd.GetString(2),
                            .Posicion = rd.GetInt32(3),
                            .Oculta = rd.GetBoolean(4),
                            .Orden = OrdenLista.Buscar(Db.Texto(rd, "Orden"))
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
            For Each lista In listas
                lista.Tarjetas = lista.Orden.Aplicar(lista.Tarjetas)
            Next
            Return listas
        End Function

        ''' <summary>Oculta o vuelve a mostrar la lista solo para <paramref name="usuarioId"/>.</summary>
        Public Shared Sub Ocultar(listaId As Integer, tableroId As Integer, usuarioId As Integer, oculta As Boolean)
            Const sql = "
SET XACT_ABORT ON;
BEGIN TRAN;
IF EXISTS (SELECT 1 FROM Listas WHERE Id = @ListaId AND TableroId = @TableroId)
BEGIN
    UPDATE PreferenciasLista WITH (UPDLOCK, HOLDLOCK) SET Oculta = @Oculta WHERE UsuarioId = @UsuarioId AND ListaId = @ListaId;
    IF @@ROWCOUNT = 0
        INSERT INTO PreferenciasLista (UsuarioId, ListaId, Oculta) VALUES (@UsuarioId, @ListaId, @Oculta);
    " & SqlQuitarPreferenciaVacia & "
END
COMMIT;"
            GuardarPreferencia(sql, listaId, tableroId, usuarioId,
                               Sub(cmd) cmd.Parameters.Add("@Oculta", SqlDbType.Bit).Value = oculta)
        End Sub

        ''' <summary>
        ''' Orden de las tarjetas de la lista solo para <paramref name="usuarioId"/>: clave de OrdenLista
        ''' (validada por quien llama) o Nothing para el orden manual.
        ''' </summary>
        Public Shared Sub CambiarOrden(listaId As Integer, tableroId As Integer, usuarioId As Integer, orden As String)
            Const sql = "
SET XACT_ABORT ON;
BEGIN TRAN;
IF EXISTS (SELECT 1 FROM Listas WHERE Id = @ListaId AND TableroId = @TableroId)
BEGIN
    UPDATE PreferenciasLista WITH (UPDLOCK, HOLDLOCK) SET Orden = @Orden WHERE UsuarioId = @UsuarioId AND ListaId = @ListaId;
    IF @@ROWCOUNT = 0
        INSERT INTO PreferenciasLista (UsuarioId, ListaId, Orden) VALUES (@UsuarioId, @ListaId, @Orden);
    " & SqlQuitarPreferenciaVacia & "
END
COMMIT;"
            GuardarPreferencia(sql, listaId, tableroId, usuarioId,
                               Sub(cmd) cmd.Parameters.Add("@Orden", SqlDbType.VarChar, OrdenLista.LongitudMaximaClave).Value = Db.ONulo(orden))
        End Sub

        ' Una preferencia con los valores predeterminados (visible y orden manual) no hace falta guardarla.
        Private Const SqlQuitarPreferenciaVacia As String =
            "DELETE FROM PreferenciasLista WHERE UsuarioId = @UsuarioId AND ListaId = @ListaId AND Oculta = 0 AND Orden IS NULL;"

        Private Shared Sub GuardarPreferencia(sql As String, listaId As Integer, tableroId As Integer, usuarioId As Integer,
                                              parametro As Action(Of SqlCommand))
            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@ListaId", SqlDbType.Int).Value = listaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                parametro(cmd)
                cn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Sub

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

        ''' <summary>Elimina la lista con sus tarjetas y devuelve los archivos de sus adjuntos (para borrarlos después).</summary>
        Public Shared Function Eliminar(listaId As Integer, tableroId As Integer) As List(Of String)
            Dim sql = "
SET XACT_ABORT ON;
BEGIN TRAN;
" & AdjuntosDatos.SqlDeclararAdjuntos & "
INSERT INTO @Adjuntos
SELECT f.Archivo FROM AdjuntosTarjeta f JOIN Tarjetas c ON c.Id = f.TarjetaId JOIN Listas l ON l.Id = c.ListaId
WHERE l.Id = @ListaId AND l.TableroId = @TableroId;
DELETE FROM Listas WHERE Id = @ListaId AND TableroId = @TableroId;" &
                Db.SqlRenumerarListas & "
COMMIT;
SELECT Archivo FROM @Adjuntos;"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@ListaId", SqlDbType.Int).Value = listaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Return AdjuntosDatos.LeerArchivos(cmd)
            End Using
        End Function

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
