Imports System.Data.SqlClient

Namespace Datos

    ''' <summary>
    ''' Acceso común a SQL Server. La cadena "TaskManager" se lee de connectionStrings.config
    ''' (archivo local excluido de git, referenciado desde Web.config).
    ''' </summary>
    Public NotInheritable Class Db

        Private Sub New()
        End Sub

        Public Shared Function CrearConexion() As SqlConnection
            Dim cadena = ConfigurationManager.ConnectionStrings("TaskManager")
            If cadena Is Nothing OrElse String.IsNullOrWhiteSpace(cadena.ConnectionString) Then
                Throw New ConfigurationErrorsException(
                    "Falta la cadena de conexión 'TaskManager'. Copia connectionStrings.example.config como connectionStrings.config y rellénalo.")
            End If
            Return New SqlConnection(cadena.ConnectionString)
        End Function

        ' Renumera Posicion (0..n-1) de las tarjetas de una lista. Requiere el parámetro @ListaId.
        Friend Const SqlRenumerarTarjetas As String = "
WITH o AS (SELECT Posicion, ROW_NUMBER() OVER (ORDER BY Posicion, Id) - 1 AS N
           FROM Tarjetas WHERE ListaId = @ListaId)
UPDATE o SET Posicion = N;"

        ' Renumera Posicion (0..n-1) de las listas de un tablero. Requiere el parámetro @TableroId.
        Friend Const SqlRenumerarListas As String = "
WITH o AS (SELECT Posicion, ROW_NUMBER() OVER (ORDER BY Posicion, Id) - 1 AS N
           FROM Listas WHERE TableroId = @TableroId)
UPDATE o SET Posicion = N;"

        Public Shared Function Texto(rd As SqlDataReader, columna As String) As String
            Dim i = rd.GetOrdinal(columna)
            Return If(rd.IsDBNull(i), Nothing, rd.GetString(i))
        End Function

        Public Shared Function Fecha(rd As SqlDataReader, columna As String) As DateTime?
            Dim i = rd.GetOrdinal(columna)
            If rd.IsDBNull(i) Then Return Nothing
            Return rd.GetDateTime(i)
        End Function

        Public Shared Function Entero(rd As SqlDataReader, columna As String) As Integer?
            Dim i = rd.GetOrdinal(columna)
            If rd.IsDBNull(i) Then Return Nothing
            Return rd.GetInt32(i)
        End Function

        ''' <summary>Convierte un valor opcional en el valor del parámetro (DBNull si no tiene valor).</summary>
        Public Shared Function ONulo(Of T As Structure)(valor As T?) As Object
            Return If(valor.HasValue, CObj(valor.Value), DBNull.Value)
        End Function

        Public Shared Function ONulo(valor As String) As Object
            Return If(String.IsNullOrEmpty(valor), CObj(DBNull.Value), valor)
        End Function

        ''' <summary>Violación de clave única o primaria.</summary>
        Public Shared Function EsDuplicado(ex As SqlException) As Boolean
            Return ex.Number = 2627 OrElse ex.Number = 2601
        End Function

    End Class

End Namespace
