Imports System.Data.SqlClient
Imports System.Globalization
Imports TaskManager.Models

Namespace Datos

    ''' <summary>
    ''' Etiquetas (#tags) de las tarjetas. Son de cada tablero y no se comparten entre tableros: todos los
    ''' métodos reciben el TableroId ya autorizado y lo usan en el WHERE. Una etiqueta se crea al usarla por primera vez en el tablero,
    ''' con el siguiente color de Etiqueta.Paleta, y se conserva aunque deje de usarse (sirve de sugerencia).
    ''' </summary>
    Public NotInheritable Class EtiquetasDatos

        Private Sub New()
        End Sub

        ''' <summary>Etiquetas del tablero con el número de tarjetas que las usan; las más usadas primero.</summary>
        Public Shared Function ListarDeTablero(tableroId As Integer) As List(Of Etiqueta)
            Const sql = "
SELECT e.Id, e.Nombre, e.Color, COUNT(te.TarjetaId) AS Usos
FROM Etiquetas e
LEFT JOIN TarjetaEtiquetas te ON te.EtiquetaId = e.Id
WHERE e.TableroId = @TableroId
GROUP BY e.Id, e.Nombre, e.Color
ORDER BY COUNT(te.TarjetaId) DESC, e.Nombre"

            Dim etiquetas As New List(Of Etiqueta)
            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        etiquetas.Add(New Etiqueta With {
                            .Id = rd.GetInt32(0), .Nombre = rd.GetString(1), .Color = rd.GetString(2), .Usos = rd.GetInt32(3)})
                    End While
                End Using
            End Using
            Return etiquetas
        End Function

        ''' <summary>
        ''' Nombres de etiqueta para el filtro del buscador: los del tablero indicado o, sin tablero, los de todos los
        ''' tableros del usuario (un nombre repetido en varios tableros sale una vez, sumando sus usos, con uno de sus colores).
        ''' </summary>
        Public Shared Function ListarParaBuscador(usuarioId As Integer, tableroId As Integer?) As List(Of Etiqueta)
            Const sql = "
WITH Accesibles AS (
    SELECT t.Id FROM Tableros t
    WHERE (@TableroId IS NULL OR t.Id = @TableroId)
      AND (t.PropietarioId = @UsuarioId
           OR EXISTS (SELECT 1 FROM MiembrosTablero m WHERE m.TableroId = t.Id AND m.UsuarioId = @UsuarioId))
)
SELECT MIN(e.Nombre) AS Nombre, MIN(e.Color) AS Color, COUNT(te.TarjetaId) AS Usos
FROM Etiquetas e
JOIN Accesibles a ON a.Id = e.TableroId
LEFT JOIN TarjetaEtiquetas te ON te.EtiquetaId = e.Id
GROUP BY e.Nombre
ORDER BY COUNT(te.TarjetaId) DESC, MIN(e.Nombre)"

            Dim etiquetas As New List(Of Etiqueta)
            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = Db.ONulo(tableroId)
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        etiquetas.Add(New Etiqueta With {.Nombre = rd.GetString(0), .Color = rd.GetString(1), .Usos = rd.GetInt32(2)})
                    End While
                End Using
            End Using
            Return etiquetas
        End Function

        ''' <summary>
        ''' Consulta de las etiquetas de las tarjetas del tablero: TarjetaId, Id, Nombre, Color (por nombre).
        ''' Requiere el parámetro @TableroId; opcionalmente se añade un filtro "AND c.Id = @TarjetaId".
        ''' </summary>
        Friend Const SqlEtiquetasDeTarjetas As String = "
SELECT te.TarjetaId, e.Id, e.Nombre, e.Color
FROM TarjetaEtiquetas te
JOIN Etiquetas e ON e.Id = te.EtiquetaId
JOIN Tarjetas c ON c.Id = te.TarjetaId
JOIN Listas l ON l.Id = c.ListaId
WHERE l.TableroId = @TableroId AND e.TableroId = @TableroId"

        ''' <summary>Lee el resultado de SqlEtiquetasDeTarjetas y añade cada etiqueta a su tarjeta.</summary>
        Friend Shared Sub LeerEnTarjetas(rd As SqlDataReader, tarjetas As IDictionary(Of Integer, Tarjeta))
            While rd.Read()
                Dim tarjeta As Tarjeta = Nothing
                If tarjetas.TryGetValue(rd.GetInt32(0), tarjeta) Then
                    tarjeta.Etiquetas.Add(New Etiqueta With {.Id = rd.GetInt32(1), .Nombre = rd.GetString(2), .Color = rd.GetString(3)})
                End If
            End While
        End Sub

        ''' <summary>
        ''' Deja en la tarjeta exactamente las etiquetas indicadas (creando en el tablero las que no existan)
        ''' dentro de la transacción de quien llama, que ya ha comprobado que la tarjeta es del tablero.
        ''' Devuelve el cambio para el historial, o Nothing si las etiquetas no han cambiado.
        ''' </summary>
        Friend Shared Function Asignar(cn As SqlConnection, tx As SqlTransaction, tarjetaId As Integer, tableroId As Integer,
                                       nombres As IEnumerable(Of String)) As HistorialTarjeta
            Const sqlNombres = "
SELECT e.Nombre FROM TarjetaEtiquetas te JOIN Etiquetas e ON e.Id = te.EtiquetaId
WHERE te.TarjetaId = @TarjetaId ORDER BY e.Nombre"
            ' El bloqueo evita que dos guardados simultáneos den el mismo color a dos etiquetas nuevas.
            Const sqlContar = "SELECT COUNT(*) FROM Etiquetas WITH (UPDLOCK, HOLDLOCK) WHERE TableroId = @TableroId"
            Const sqlUna = "
DECLARE @Id INT = (SELECT Id FROM Etiquetas WITH (UPDLOCK, HOLDLOCK) WHERE TableroId = @TableroId AND Nombre = @Nombre);
DECLARE @Creada BIT = 0;
IF @Id IS NULL
BEGIN
    INSERT INTO Etiquetas (TableroId, Nombre, Color) VALUES (@TableroId, @Nombre, @Color);
    SET @Id = SCOPE_IDENTITY();
    SET @Creada = 1;
END
IF NOT EXISTS (SELECT 1 FROM TarjetaEtiquetas WHERE TarjetaId = @TarjetaId AND EtiquetaId = @Id)
    INSERT INTO TarjetaEtiquetas (TarjetaId, EtiquetaId) VALUES (@TarjetaId, @Id);
SELECT @Id, @Creada;"

            Dim antes = LeerNombres(cn, tx, sqlNombres, tarjetaId)

            Dim total As Integer
            Using cmd As New SqlCommand(sqlContar, cn, tx)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                total = CInt(cmd.ExecuteScalar())
            End Using

            Dim ids As New List(Of Integer)
            For Each nombre In nombres.Where(AddressOf Etiqueta.EsNombreValido).Take(Etiqueta.MaximoPorTarjeta)
                Using cmd As New SqlCommand(sqlUna, cn, tx)
                    cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                    cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                    cmd.Parameters.Add("@Nombre", SqlDbType.NVarChar, Etiqueta.LongitudMaxima).Value = nombre
                    cmd.Parameters.Add("@Color", SqlDbType.NVarChar, 7).Value = Etiqueta.ColorPorIndice(total)
                    Using rd = cmd.ExecuteReader()
                        rd.Read()
                        ids.Add(rd.GetInt32(0))
                        If rd.GetBoolean(1) Then total += 1
                    End Using
                End Using
            Next

            ' Quita las que ya no están. Los nombres de parámetro (@E0, @E1...) los genera el código.
            Dim sqlQuitar = "DELETE FROM TarjetaEtiquetas WHERE TarjetaId = @TarjetaId"
            If ids.Count > 0 Then
                sqlQuitar &= " AND EtiquetaId NOT IN (" &
                             String.Join(", ", ids.Select(Function(id, i) "@E" & i.ToString(CultureInfo.InvariantCulture))) & ")"
            End If
            Using cmd As New SqlCommand(sqlQuitar, cn, tx)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                For i = 0 To ids.Count - 1
                    cmd.Parameters.Add("@E" & i.ToString(CultureInfo.InvariantCulture), SqlDbType.Int).Value = ids(i)
                Next
                cmd.ExecuteNonQuery()
            End Using

            Dim despues = LeerNombres(cn, tx, sqlNombres, tarjetaId)
            If antes.SequenceEqual(despues, StringComparer.CurrentCultureIgnoreCase) Then Return Nothing
            Return HistorialTarjeta.Nuevo(HistorialTarjeta.Editada, HistorialTarjeta.CampoEtiquetas,
                                          ATextoHistorial(antes), ATextoHistorial(despues))
        End Function

        Private Shared Function LeerNombres(cn As SqlConnection, tx As SqlTransaction, sql As String, tarjetaId As Integer) As List(Of String)
            Dim nombres As New List(Of String)
            Using cmd As New SqlCommand(sql, cn, tx)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        nombres.Add(rd.GetString(0))
                    End While
                End Using
            End Using
            Return nombres
        End Function

        Private Shared Function ATextoHistorial(nombres As List(Of String)) As String
            Return If(nombres.Count = 0, Nothing, String.Join(" ", nombres.Select(Function(n) "#" & n)))
        End Function

    End Class

End Namespace
