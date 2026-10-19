Imports System.Data.SqlClient
Imports System.Globalization
Imports TaskManager.Models

Namespace Datos

    ''' <summary>
    ''' Subtareas (checklist) de una tarjeta. Todas las operaciones comprueban que la tarjeta
    ''' pertenece al TableroId ya autorizado. Una subtarea puede enlazar a otra tarjeta del mismo tablero
    ''' (TarjetaEnlazadaId); el enlace también se comprueba contra el TableroId al guardarlo y al mostrarlo.
    ''' </summary>
    Public NotInheritable Class SubtareasDatos

        Private Sub New()
        End Sub

        ' Id de la tarjeta @Enlazada si es del tablero @TableroId (si no, NULL: se guarda como subtarea normal).
        Private Const SqlEnlaceValido As String = "
(SELECT e.Id FROM Tarjetas e JOIN Listas le ON le.Id = e.ListaId
 WHERE e.Id = @Enlazada AND le.TableroId = @TableroId)"

        Public Shared Function Listar(tarjetaId As Integer, tableroId As Integer) As List(Of Subtarea)
            Const sql = "
SELECT s.Id, s.TarjetaId, s.Texto, s.Completada, s.Posicion, s.TarjetaEnlazadaId, e.Titulo AS TituloEnlazada
FROM Subtareas s
JOIN Tarjetas c ON c.Id = s.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
LEFT JOIN (Tarjetas e JOIN Listas le ON le.Id = e.ListaId AND le.TableroId = @TableroId) ON e.Id = s.TarjetaEnlazadaId
WHERE s.TarjetaId = @TarjetaId AND l.TableroId = @TableroId
ORDER BY s.Posicion, s.Id"

            Dim subtareas As New List(Of Subtarea)
            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        subtareas.Add(New Subtarea With {
                            .Id = rd.GetInt32(0),
                            .TarjetaId = rd.GetInt32(1),
                            .Texto = rd.GetString(2),
                            .Completada = rd.GetBoolean(3),
                            .Posicion = rd.GetInt32(4),
                            .TarjetaEnlazadaId = Db.Entero(rd, "TarjetaEnlazadaId"),
                            .TituloEnlazada = Db.Texto(rd, "TituloEnlazada")
                        })
                    End While
                End Using
            End Using
            Return subtareas
        End Function

        ''' <summary>
        ''' Añade la subtarea al final si la tarjeta pertenece al tablero. Con <paramref name="tarjetaEnlazadaId"/>
        ''' enlaza a esa tarjeta (solo si es de este tablero y no es la propia; <paramref name="texto"/> = su título).
        ''' </summary>
        Public Shared Sub Crear(tarjetaId As Integer, tableroId As Integer, texto As String, usuarioId As Integer,
                                Optional tarjetaEnlazadaId As Integer? = Nothing)
            Const sql = "
INSERT INTO Subtareas (TarjetaId, Texto, Posicion, TarjetaEnlazadaId)
OUTPUT INSERTED.Id
SELECT c.Id, @Texto, ISNULL((SELECT MAX(Posicion) + 1 FROM Subtareas WHERE TarjetaId = c.Id), 0),
       CASE WHEN @Enlazada <> c.Id THEN " & SqlEnlaceValido & " END
FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId"

            Using cn = Db.CrearConexion()
                cn.Open()
                Using tx = cn.BeginTransaction()
                    Using cmd As New SqlCommand(sql, cn, tx)
                        cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                        cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                        cmd.Parameters.Add("@Texto", SqlDbType.NVarChar, 300).Value = texto
                        cmd.Parameters.Add("@Enlazada", SqlDbType.Int).Value = Db.ONulo(tarjetaEnlazadaId)
                        If cmd.ExecuteScalar() Is Nothing Then Return
                    End Using
                    HistorialDatos.Registrar(cn, tx, tarjetaId, usuarioId,
                                             HistorialTarjeta.Nuevo(HistorialTarjeta.SubtareaAgregada, valorNuevo:=texto))
                    tx.Commit()
                End Using
            End Using
        End Sub

        ''' <summary>
        ''' Inserta una subtarea dentro de una transacción ya abierta (al crear la tarjeta, que ya se ha comprobado
        ''' que es del tablero). El enlace solo se guarda si la tarjeta enlazada es de ese mismo tablero.
        ''' </summary>
        Friend Shared Sub Insertar(cn As SqlConnection, tx As SqlTransaction, tarjetaId As Integer, tableroId As Integer,
                                   subtarea As Subtarea, posicion As Integer)
            Const sql = "
INSERT INTO Subtareas (TarjetaId, Texto, Completada, Posicion, TarjetaEnlazadaId)
SELECT @TarjetaId, @Texto, @Completada, @Posicion, " & SqlEnlaceValido

            Using cmd As New SqlCommand(sql, cn, tx)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cmd.Parameters.Add("@Texto", SqlDbType.NVarChar, 300).Value = subtarea.Texto
                cmd.Parameters.Add("@Completada", SqlDbType.Bit).Value = subtarea.Completada
                cmd.Parameters.Add("@Posicion", SqlDbType.Int).Value = posicion
                cmd.Parameters.Add("@Enlazada", SqlDbType.Int).Value = Db.ONulo(subtarea.TarjetaEnlazadaId)
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        ''' <summary>Marca o desmarca la subtarea; solo registra historial si el estado cambia de verdad.</summary>
        Public Shared Sub CambiarEstado(subtareaId As Integer, tableroId As Integer, completada As Boolean, usuarioId As Integer)
            Const sql = "
UPDATE s SET Completada = @Completada
OUTPUT INSERTED.TarjetaId, INSERTED.Texto
FROM Subtareas s
JOIN Tarjetas c ON c.Id = s.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
WHERE s.Id = @SubtareaId AND l.TableroId = @TableroId AND s.Completada <> @Completada"

            Dim accion = If(completada, HistorialTarjeta.SubtareaCompletada, HistorialTarjeta.SubtareaReabierta)
            EjecutarYRegistrar(sql, subtareaId, tableroId, usuarioId, accion,
                               Sub(cmd) cmd.Parameters.Add("@Completada", SqlDbType.Bit).Value = completada)
        End Sub

        ''' <summary>
        ''' Cambia el texto de una subtarea normal (las enlazadas muestran el título de su tarjeta y no se editan).
        ''' Registra en el historial el texto anterior y el nuevo; devuelve False si no ha cambiado nada.
        ''' </summary>
        Public Shared Function CambiarTexto(subtareaId As Integer, tableroId As Integer, texto As String, usuarioId As Integer) As Boolean
            Const sql = "
UPDATE s SET Texto = @Texto
OUTPUT INSERTED.TarjetaId, DELETED.Texto
FROM Subtareas s
JOIN Tarjetas c ON c.Id = s.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
WHERE s.Id = @SubtareaId AND l.TableroId = @TableroId AND s.TarjetaEnlazadaId IS NULL
  AND s.Texto COLLATE Latin1_General_BIN2 <> @Texto COLLATE Latin1_General_BIN2"

            Using cn = Db.CrearConexion()
                cn.Open()
                Using tx = cn.BeginTransaction()
                    Dim tarjetaId As Integer
                    Dim anterior As String
                    Using cmd As New SqlCommand(sql, cn, tx)
                        cmd.Parameters.Add("@SubtareaId", SqlDbType.Int).Value = subtareaId
                        cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                        cmd.Parameters.Add("@Texto", SqlDbType.NVarChar, 300).Value = texto
                        Using rd = cmd.ExecuteReader()
                            If Not rd.Read() Then Return False
                            tarjetaId = rd.GetInt32(0)
                            anterior = rd.GetString(1)
                        End Using
                    End Using
                    HistorialDatos.Registrar(cn, tx, tarjetaId, usuarioId,
                                             HistorialTarjeta.Nuevo(HistorialTarjeta.SubtareaEditada, valorAnterior:=anterior, valorNuevo:=texto))
                    tx.Commit()
                End Using
            End Using
            Return True
        End Function

        ''' <summary>Máximo de Ids aceptados al reordenar (una tarjeta no tiene tantas subtareas).</summary>
        Public Const MaximoAlReordenar As Integer = 500

        ''' <summary>"5,3,9" → lista de enteros; Nothing si el texto no es válido o vacío.</summary>
        Public Shared Function LeerOrden(texto As String) As List(Of Integer)
            Dim ids As New List(Of Integer)
            For Each parte In If(texto, "").Split(","c)
                Dim n As Integer
                If Not Integer.TryParse(parte, NumberStyles.None, CultureInfo.InvariantCulture, n) Then Return Nothing
                ids.Add(n)
            Next
            If ids.Count = 0 OrElse ids.Count > MaximoAlReordenar Then Return Nothing
            Return ids
        End Function

        ''' <summary>
        ''' Pone las subtareas de la tarjeta en el orden indicado (Ids); las que no vengan en la lista (p. ej. añadidas
        ''' por otro usuario mientras tanto) quedan detrás, en su orden. Ids de otras tarjetas no tienen efecto.
        ''' Reordenar no se registra en el historial (como reordenar tarjetas dentro de una lista).
        ''' </summary>
        Public Shared Sub Reordenar(tarjetaId As Integer, tableroId As Integer, orden As IEnumerable(Of Integer))
            If orden Is Nothing Then Return
            Dim ids = orden.Distinct().ToList()
            If ids.Count = 0 OrElse ids.Count > MaximoAlReordenar Then Return

            Using cn = Db.CrearConexion(), cmd As New SqlCommand()
                cmd.Connection = cn
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                ' Nombres de parámetro generados por el código (@Id0, @Id1...); los valores, como parámetros.
                Dim filas As New List(Of String)
                For i = 0 To ids.Count - 1
                    Dim p = "@Id" & i.ToString(CultureInfo.InvariantCulture)
                    cmd.Parameters.Add(p, SqlDbType.Int).Value = ids(i)
                    filas.Add("(" & p & ", " & i.ToString(CultureInfo.InvariantCulture) & ")")
                Next
                cmd.CommandText = "
WITH o AS (
    SELECT s.Id, ROW_NUMBER() OVER (ORDER BY CASE WHEN v.Pos IS NULL THEN 1 ELSE 0 END, v.Pos, s.Posicion, s.Id) - 1 AS NuevaPos
    FROM Subtareas s
    LEFT JOIN (VALUES " & String.Join(", ", filas) & ") v(Id, Pos) ON v.Id = s.Id
    WHERE s.TarjetaId = @TarjetaId
      AND EXISTS (SELECT 1 FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId)
)
UPDATE s SET Posicion = o.NuevaPos
FROM Subtareas s JOIN o ON o.Id = s.Id
WHERE s.Posicion <> o.NuevaPos"
                cn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Public Shared Sub Eliminar(subtareaId As Integer, tableroId As Integer, usuarioId As Integer)
            Const sql = "
DELETE s
OUTPUT DELETED.TarjetaId, DELETED.Texto
FROM Subtareas s
JOIN Tarjetas c ON c.Id = s.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
WHERE s.Id = @SubtareaId AND l.TableroId = @TableroId"

            EjecutarYRegistrar(sql, subtareaId, tableroId, usuarioId, HistorialTarjeta.SubtareaEliminada, Nothing)
        End Sub

        ' Ejecuta una sentencia con OUTPUT (TarjetaId, Texto) y, si afectó a una fila, registra la acción.
        Private Shared Sub EjecutarYRegistrar(sql As String, subtareaId As Integer, tableroId As Integer, usuarioId As Integer,
                                              accion As String, parametrosExtra As Action(Of SqlCommand))
            Using cn = Db.CrearConexion()
                cn.Open()
                Using tx = cn.BeginTransaction()
                    Dim tarjetaId As Integer
                    Dim texto As String
                    Using cmd As New SqlCommand(sql, cn, tx)
                        cmd.Parameters.Add("@SubtareaId", SqlDbType.Int).Value = subtareaId
                        cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                        parametrosExtra?.Invoke(cmd)
                        Using rd = cmd.ExecuteReader()
                            If Not rd.Read() Then Return
                            tarjetaId = rd.GetInt32(0)
                            texto = rd.GetString(1)
                        End Using
                    End Using
                    HistorialDatos.Registrar(cn, tx, tarjetaId, usuarioId, HistorialTarjeta.Nuevo(accion, valorNuevo:=texto))
                    tx.Commit()
                End Using
            End Using
        End Sub

    End Class

End Namespace
