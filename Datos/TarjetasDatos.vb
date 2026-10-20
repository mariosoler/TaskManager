Imports System.Data.SqlClient
Imports System.Globalization
Imports TaskManager.Models

Namespace Datos

    ''' <summary>
    ''' Operaciones sobre tarjetas. Todas filtran por el TableroId ya autorizado (vía Listas).
    ''' </summary>
    Public NotInheritable Class TarjetasDatos

        Private Sub New()
        End Sub

        ' Columnas que lee LeerTarjeta. No incluye Codigo (NVARCHAR(MAX)): en listados basta con TieneCodigo.
        Friend Const SqlColumnasTarjeta As String = "
       c.Id, c.ListaId, l.Titulo AS NombreLista, c.Titulo, c.Descripcion, c.Posicion, c.FechaLimite,
       c.AsignadoA, u.Nombre AS NombreAsignado, c.FechaCreacion, c.FechaEnLista, c.Caso, CAST(c.Prioridad AS INT) AS Prioridad,
       c.FechaModificacion, um.Nombre AS NombreModificador,
       CAST(CASE WHEN c.Codigo IS NULL OR c.Codigo = N'' THEN 0 ELSE 1 END AS BIT) AS TieneCodigo,
       (SELECT COUNT(*) FROM Comentarios co WHERE co.TarjetaId = c.Id) AS NumComentarios,
       (SELECT COUNT(*) FROM AdjuntosTarjeta ad WHERE ad.TarjetaId = c.Id) AS NumAdjuntos,
       (SELECT COUNT(*) FROM Subtareas s WHERE s.TarjetaId = c.Id) AS SubtareasTotal,
       (SELECT COUNT(*) FROM Subtareas s WHERE s.TarjetaId = c.Id AND s.Completada = 1) AS SubtareasHechas"

        Friend Const SqlFromTarjetas As String = "
FROM Tarjetas c
JOIN Listas l ON l.Id = c.ListaId
LEFT JOIN Usuarios u ON u.Id = c.AsignadoA
LEFT JOIN Usuarios um ON um.Id = c.ModificadoPor"

        Friend Const SqlSelectTarjetas As String = "SELECT" & SqlColumnasTarjeta & SqlFromTarjetas

        ''' <summary>
        ''' Tarjetas del tablero para enlazarlas desde una subtarea (Id, Titulo, Caso, NombreLista), en el orden
        ''' del tablero (listas y posición).
        ''' </summary>
        Public Shared Function ListarParaEnlazar(tableroId As Integer) As List(Of Tarjeta)
            Const sql = "
SELECT c.Id, c.Titulo, c.Caso, l.Titulo AS NombreLista
FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
WHERE l.TableroId = @TableroId
ORDER BY l.Posicion, c.Posicion, c.Id"

            Dim tarjetas As New List(Of Tarjeta)
            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    While rd.Read()
                        tarjetas.Add(New Tarjeta With {
                            .Id = rd.GetInt32(0), .Titulo = rd.GetString(1), .Caso = Db.Texto(rd, "Caso"), .NombreLista = rd.GetString(3)})
                    End While
                End Using
            End Using
            Return tarjetas
        End Function

        ''' <summary>Tarjeta completa (incluido Codigo) si pertenece al tablero; Nothing en caso contrario.</summary>
        Public Shared Function Obtener(tarjetaId As Integer, tableroId As Integer) As Tarjeta
            Dim sql = SqlSelectTarjetas & " WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId;
SELECT Codigo FROM Tarjetas WHERE Id = @TarjetaId;" &
                      EtiquetasDatos.SqlEtiquetasDeTarjetas & " AND c.Id = @TarjetaId ORDER BY e.Nombre;"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Using rd = cmd.ExecuteReader()
                    If Not rd.Read() Then Return Nothing
                    Dim tarjeta = LeerTarjeta(rd)
                    rd.NextResult()
                    If rd.Read() Then tarjeta.Codigo = Db.Texto(rd, "Codigo")
                    rd.NextResult()
                    EtiquetasDatos.LeerEnTarjetas(rd, New Dictionary(Of Integer, Tarjeta) From {{tarjeta.Id, tarjeta}})
                    Return tarjeta
                End Using
            End Using
        End Function

        ' @AsignadoA solo si tiene acceso al tablero de la lista "l"; si no, NULL.
        Private Const SqlAsignadoValido As String = "
    CASE
        WHEN @AsignadoA IS NULL THEN NULL
        WHEN EXISTS (SELECT 1 FROM MiembrosTablero m WHERE m.TableroId = l.TableroId AND m.UsuarioId = @AsignadoA)
          OR EXISTS (SELECT 1 FROM Tableros t WHERE t.Id = l.TableroId AND t.PropietarioId = @AsignadoA) THEN @AsignadoA
        ELSE NULL END"

        ''' <summary>
        ''' Crea la tarjeta al final de la lista (si la lista pertenece al tablero) junto con sus subtareas,
        ''' y sus etiquetas, en una transacción. Usa Titulo, Descripcion, FechaLimite, AsignadoA, Caso, Codigo,
        ''' Prioridad y Etiquetas de <paramref name="datos"/>. Devuelve el Id de la tarjeta nueva (0 si la lista no es del tablero).
        ''' </summary>
        Public Shared Function Crear(listaId As Integer, tableroId As Integer, datos As Tarjeta, subtareas As IEnumerable(Of Subtarea),
                                usuarioId As Integer) As Integer
            Dim sql = "
DECLARE @Nueva TABLE (Id INT);
INSERT INTO Tarjetas (ListaId, Titulo, Descripcion, Posicion, FechaLimite, AsignadoA, Caso, Codigo, Prioridad)
OUTPUT INSERTED.Id INTO @Nueva
SELECT l.Id, @Titulo, @Descripcion,
       ISNULL((SELECT MAX(Posicion) + 1 FROM Tarjetas WHERE ListaId = l.Id), 0),
       @FechaLimite," & SqlAsignadoValido & ", @Caso, @Codigo, @Prioridad
FROM Listas l
WHERE l.Id = @ListaId AND l.TableroId = @TableroId;
SELECT n.Id, l.Titulo FROM @Nueva n CROSS JOIN Listas l WHERE l.Id = @ListaId;"

            Using cn = Db.CrearConexion()
                cn.Open()
                Using tx = cn.BeginTransaction()
                    Dim tarjetaId As Integer
                    Dim nombreLista As String
                    Using cmd As New SqlCommand(sql, cn, tx)
                        cmd.Parameters.Add("@ListaId", SqlDbType.Int).Value = listaId
                        cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                        AgregarCampos(cmd, datos)
                        Using rd = cmd.ExecuteReader()
                            If Not rd.Read() Then Return 0  ' la lista no es de este tablero (rollback al salir)
                            tarjetaId = rd.GetInt32(0)
                            nombreLista = rd.GetString(1)
                        End Using
                    End Using

                    Dim posicion = 0
                    For Each subtarea In subtareas
                        SubtareasDatos.Insertar(cn, tx, tarjetaId, tableroId, subtarea, posicion)
                        posicion += 1
                    Next
                    EtiquetasDatos.Asignar(cn, tx, tarjetaId, tableroId, NombresEtiquetas(datos))
                    HistorialDatos.Registrar(cn, tx, tarjetaId, usuarioId,
                                             HistorialTarjeta.Nuevo(HistorialTarjeta.Creada, valorNuevo:=nombreLista))
                    tx.Commit()
                    Return tarjetaId
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Actualiza los datos editables y registra en el historial cada campo que cambie
        ''' (nada si no cambia ninguno). Solo se asigna a usuarios con acceso al tablero.
        ''' </summary>
        Public Shared Sub Actualizar(tarjetaId As Integer, tableroId As Integer, datos As Tarjeta, usuarioId As Integer)
            Const sqlActual = "
SELECT c.Titulo, c.Descripcion, c.FechaLimite, c.AsignadoA, u.Nombre AS NombreAsignado, c.Caso, c.Codigo,
       CAST(c.Prioridad AS INT) AS Prioridad
FROM Tarjetas c WITH (UPDLOCK)
JOIN Listas l ON l.Id = c.ListaId
LEFT JOIN Usuarios u ON u.Id = c.AsignadoA
WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId"

            Dim sqlActualizar = "
UPDATE c SET
    Titulo = @Titulo,
    Descripcion = @Descripcion,
    FechaLimite = @FechaLimite,
    Caso = @Caso,
    Codigo = @Codigo,
    Prioridad = @Prioridad,
    AsignadoA =" & SqlAsignadoValido & "
FROM Tarjetas c
JOIN Listas l ON l.Id = c.ListaId
WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId;
SELECT c.AsignadoA, u.Nombre AS NombreAsignado FROM Tarjetas c LEFT JOIN Usuarios u ON u.Id = c.AsignadoA WHERE c.Id = @TarjetaId;"

            Using cn = Db.CrearConexion()
                cn.Open()
                Using tx = cn.BeginTransaction()
                    Dim antes As Tarjeta
                    Using cmd As New SqlCommand(sqlActual, cn, tx)
                        cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                        cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                        Using rd = cmd.ExecuteReader()
                            If Not rd.Read() Then Return  ' no es de este tablero
                            antes = New Tarjeta With {
                                .Titulo = rd.GetString(0),
                                .Descripcion = Db.Texto(rd, "Descripcion"),
                                .FechaLimite = Db.Fecha(rd, "FechaLimite"),
                                .AsignadoA = Db.Entero(rd, "AsignadoA"),
                                .NombreAsignado = Db.Texto(rd, "NombreAsignado"),
                                .Caso = Db.Texto(rd, "Caso"),
                                .Codigo = Db.Texto(rd, "Codigo"),
                                .Prioridad = Db.Entero(rd, "Prioridad")
                            }
                        End Using
                    End Using

                    ' El asignado final puede diferir del pedido (si no tiene acceso se guarda NULL).
                    Dim despues = New Tarjeta With {
                        .Titulo = datos.Titulo, .Descripcion = datos.Descripcion, .FechaLimite = datos.FechaLimite,
                        .Caso = datos.Caso, .Codigo = datos.Codigo, .Prioridad = datos.Prioridad
                    }
                    Using cmd As New SqlCommand(sqlActualizar, cn, tx)
                        cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                        cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                        AgregarCampos(cmd, datos)
                        Using rd = cmd.ExecuteReader()
                            If rd.Read() Then
                                despues.AsignadoA = Db.Entero(rd, "AsignadoA")
                                despues.NombreAsignado = Db.Texto(rd, "NombreAsignado")
                            End If
                        End Using
                    End Using

                    Dim cambios = Diferencias(antes, despues)
                    Dim cambioEtiquetas = EtiquetasDatos.Asignar(cn, tx, tarjetaId, tableroId, NombresEtiquetas(datos))
                    If cambioEtiquetas IsNot Nothing Then cambios.Add(cambioEtiquetas)
                    HistorialDatos.Registrar(cn, tx, tarjetaId, usuarioId, cambios)
                    tx.Commit()
                End Using
            End Using
        End Sub

        ''' <summary>Un cambio "Editada" por cada campo distinto entre las dos versiones.</summary>
        Private Shared Function Diferencias(antes As Tarjeta, despues As Tarjeta) As List(Of HistorialTarjeta)
            Dim cambios As New List(Of HistorialTarjeta)

            Dim comparar = Sub(campo As String, anterior As String, nuevo As String, valorAnterior As String, valorNuevo As String)
                               If Not String.Equals(If(anterior, ""), If(nuevo, ""), StringComparison.Ordinal) Then
                                   cambios.Add(HistorialTarjeta.Nuevo(HistorialTarjeta.Editada, campo, valorAnterior, valorNuevo))
                               End If
                           End Sub

            comparar(HistorialTarjeta.CampoTitulo, antes.Titulo, despues.Titulo, antes.Titulo, despues.Titulo)
            comparar(HistorialTarjeta.CampoCaso, antes.Caso, despues.Caso, antes.Caso, despues.Caso)
            comparar(HistorialTarjeta.CampoPrioridad, Prioridad.Nombre(antes.Prioridad), Prioridad.Nombre(despues.Prioridad),
                     Prioridad.Nombre(antes.Prioridad), Prioridad.Nombre(despues.Prioridad))
            comparar(HistorialTarjeta.CampoDescripcion, antes.Descripcion, despues.Descripcion,
                     HistorialDatos.Extracto(antes.Descripcion), HistorialDatos.Extracto(despues.Descripcion))
            comparar(HistorialTarjeta.CampoFechaLimite, FechaIso(antes.FechaLimite), FechaIso(despues.FechaLimite),
                     FechaIso(antes.FechaLimite), FechaIso(despues.FechaLimite))
            If Not Nullable.Equals(antes.AsignadoA, despues.AsignadoA) Then
                cambios.Add(HistorialTarjeta.Nuevo(HistorialTarjeta.Editada, HistorialTarjeta.CampoAsignado,
                                                   antes.NombreAsignado, despues.NombreAsignado))
            End If
            comparar(HistorialTarjeta.CampoCodigo, antes.Codigo, despues.Codigo,
                     HistorialDatos.Extracto(antes.Codigo), HistorialDatos.Extracto(despues.Codigo))
            Return cambios
        End Function

        Private Shared Function NombresEtiquetas(datos As Tarjeta) As IEnumerable(Of String)
            Return If(datos.Etiquetas, New List(Of Etiqueta)).Select(Function(e) e.Nombre)
        End Function

        Private Shared Function FechaIso(fecha As DateTime?) As String
            Return If(fecha.HasValue, fecha.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Nothing)
        End Function

        Private Shared Sub AgregarCampos(cmd As SqlCommand, datos As Tarjeta)
            cmd.Parameters.Add("@Titulo", SqlDbType.NVarChar, 200).Value = datos.Titulo
            cmd.Parameters.Add("@Descripcion", SqlDbType.NVarChar, -1).Value = Db.ONulo(datos.Descripcion)
            cmd.Parameters.Add("@FechaLimite", SqlDbType.DateTime2).Value = Db.ONulo(datos.FechaLimite)
            cmd.Parameters.Add("@AsignadoA", SqlDbType.Int).Value = Db.ONulo(datos.AsignadoA)
            cmd.Parameters.Add("@Caso", SqlDbType.NVarChar, 20).Value = Db.ONulo(datos.Caso)
            cmd.Parameters.Add("@Codigo", SqlDbType.NVarChar, -1).Value = Db.ONulo(datos.Codigo)
            cmd.Parameters.Add("@Prioridad", SqlDbType.TinyInt).Value =
                If(datos.Prioridad.HasValue AndAlso Prioridad.EsValida(datos.Prioridad.Value), CObj(CByte(datos.Prioridad.Value)), DBNull.Value)
        End Sub

        ''' <summary>
        ''' Elimina la tarjeta (sus adjuntos, subtareas, etc. caen en cascada) y devuelve los archivos de sus adjuntos,
        ''' para que quien llama los borre (Utilidades.AdjuntosTarjeta.EliminarArchivos).
        ''' </summary>
        Public Shared Function Eliminar(tarjetaId As Integer, tableroId As Integer) As List(Of String)
            Dim sql = "
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @ListaId INT = (SELECT c.ListaId FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
                        WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId);
" & AdjuntosDatos.SqlDeclararAdjuntos & "
INSERT INTO @Adjuntos SELECT f.Archivo FROM AdjuntosTarjeta f WHERE f.TarjetaId = @TarjetaId AND @ListaId IS NOT NULL;
DELETE FROM Tarjetas WHERE Id = @TarjetaId AND ListaId = @ListaId;" &
                Db.SqlRenumerarTarjetas & "
COMMIT;
SELECT Archivo FROM @Adjuntos;"

            Using cn = Db.CrearConexion(), cmd As New SqlCommand(sql, cn)
                cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                cn.Open()
                Return AdjuntosDatos.LeerArchivos(cmd)
            End Using
        End Function

        ''' <summary>
        ''' Mueve la tarjeta a la lista destino, en la posición <paramref name="indice"/> (0 = primera),
        ''' contando solo las demás tarjetas de esa lista. Renumera origen y destino en una transacción.
        ''' Solo el cambio de lista se registra en el historial (reordenar dentro de la lista, no).
        ''' </summary>
        Public Shared Sub Mover(tarjetaId As Integer, listaDestinoId As Integer, indice As Integer, tableroId As Integer,
                                usuarioId As Integer)
            ' Se ejecuta dentro de la transacción del cliente; devuelve los nombres de origen y destino.
            Const sql = "
SET XACT_ABORT ON;
DECLARE @Origen INT = (SELECT c.ListaId FROM Tarjetas c JOIN Listas l ON l.Id = c.ListaId
                       WHERE c.Id = @TarjetaId AND l.TableroId = @TableroId);
IF @Origen IS NULL OR NOT EXISTS (SELECT 1 FROM Listas WHERE Id = @ListaDestinoId AND TableroId = @TableroId)
    RETURN;
DECLARE @Total INT = (SELECT COUNT(*) FROM Tarjetas WHERE ListaId = @ListaDestinoId AND Id <> @TarjetaId);
DECLARE @Pos INT = CASE WHEN @Indice < 0 THEN 0 WHEN @Indice > @Total THEN @Total ELSE @Indice END;
WITH o AS (SELECT Posicion, ROW_NUMBER() OVER (ORDER BY Posicion, Id) - 1 AS N
           FROM Tarjetas WHERE ListaId = @ListaDestinoId AND Id <> @TarjetaId)
UPDATE o SET Posicion = CASE WHEN N >= @Pos THEN N + 1 ELSE N END;
UPDATE Tarjetas SET ListaId = @ListaDestinoId, Posicion = @Pos,
       FechaEnLista = CASE WHEN @Origen <> @ListaDestinoId THEN SYSUTCDATETIME() ELSE FechaEnLista END
WHERE Id = @TarjetaId;
WITH o AS (SELECT Posicion, ROW_NUMBER() OVER (ORDER BY Posicion, Id) - 1 AS N
           FROM Tarjetas WHERE ListaId = @Origen)
UPDATE o SET Posicion = N;
SELECT (SELECT Titulo FROM Listas WHERE Id = @Origen) AS Origen,
       (SELECT Titulo FROM Listas WHERE Id = @ListaDestinoId) AS Destino,
       CAST(CASE WHEN @Origen <> @ListaDestinoId THEN 1 ELSE 0 END AS BIT) AS CambioDeLista;"

            Using cn = Db.CrearConexion()
                cn.Open()
                Using tx = cn.BeginTransaction()
                    Dim origen As String = Nothing, destino As String = Nothing
                    Dim cambioDeLista = False
                    Using cmd As New SqlCommand(sql, cn, tx)
                        cmd.Parameters.Add("@TarjetaId", SqlDbType.Int).Value = tarjetaId
                        cmd.Parameters.Add("@ListaDestinoId", SqlDbType.Int).Value = listaDestinoId
                        cmd.Parameters.Add("@Indice", SqlDbType.Int).Value = indice
                        cmd.Parameters.Add("@TableroId", SqlDbType.Int).Value = tableroId
                        Using rd = cmd.ExecuteReader()
                            If Not rd.Read() Then Return   ' tarjeta o lista de otro tablero
                            origen = rd.GetString(0)
                            destino = rd.GetString(1)
                            cambioDeLista = rd.GetBoolean(2)
                        End Using
                    End Using

                    If cambioDeLista Then
                        HistorialDatos.Registrar(cn, tx, tarjetaId, usuarioId,
                                                 HistorialTarjeta.Nuevo(HistorialTarjeta.Movida, valorAnterior:=origen, valorNuevo:=destino))
                    End If
                    tx.Commit()
                End Using
            End Using
        End Sub

        ''' <summary>
        ''' Desplegable «Lista» del detalle: mueve la tarjeta al final de otra lista del tablero (con historial, como
        ''' <see cref="Mover"/>). Si ya está en esa lista no hace nada (no la manda al final).
        ''' </summary>
        Public Shared Sub CambiarLista(tarjetaId As Integer, listaId As Integer, tableroId As Integer, usuarioId As Integer)
            Dim actual = Obtener(tarjetaId, tableroId)
            If actual Is Nothing OrElse actual.ListaId = listaId Then Return
            Mover(tarjetaId, listaId, Integer.MaxValue, tableroId, usuarioId)
        End Sub

        Friend Shared Function LeerTarjeta(rd As SqlDataReader) As Tarjeta
            Return New Tarjeta With {
                .Id = rd.GetInt32(rd.GetOrdinal("Id")),
                .ListaId = rd.GetInt32(rd.GetOrdinal("ListaId")),
                .NombreLista = rd.GetString(rd.GetOrdinal("NombreLista")),
                .Titulo = rd.GetString(rd.GetOrdinal("Titulo")),
                .Descripcion = Db.Texto(rd, "Descripcion"),
                .Posicion = rd.GetInt32(rd.GetOrdinal("Posicion")),
                .FechaLimite = Db.Fecha(rd, "FechaLimite"),
                .AsignadoA = Db.Entero(rd, "AsignadoA"),
                .NombreAsignado = Db.Texto(rd, "NombreAsignado"),
                .FechaCreacion = rd.GetDateTime(rd.GetOrdinal("FechaCreacion")),
                .FechaEnLista = rd.GetDateTime(rd.GetOrdinal("FechaEnLista")),
                .Caso = Db.Texto(rd, "Caso"),
                .Prioridad = Db.Entero(rd, "Prioridad"),
                .TieneCodigo = rd.GetBoolean(rd.GetOrdinal("TieneCodigo")),
                .NumComentarios = rd.GetInt32(rd.GetOrdinal("NumComentarios")),
                .NumAdjuntos = rd.GetInt32(rd.GetOrdinal("NumAdjuntos")),
                .SubtareasTotal = rd.GetInt32(rd.GetOrdinal("SubtareasTotal")),
                .SubtareasHechas = rd.GetInt32(rd.GetOrdinal("SubtareasHechas")),
                .FechaModificacion = rd.GetDateTime(rd.GetOrdinal("FechaModificacion")),
                .NombreModificador = Db.Texto(rd, "NombreModificador")
            }
        End Function

    End Class

End Namespace
