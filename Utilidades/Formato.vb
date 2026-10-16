Imports System.Globalization

Namespace Utilidades

    ''' <summary>Utilidades de presentación compartidas por las páginas.</summary>
    Public NotInheritable Class Formato

        Private Sub New()
        End Sub

        ''' <summary>Hasta dos iniciales en mayúsculas ("Ana López" -> "AL").</summary>
        Public Shared Function Iniciales(nombre As Object) As String
            Dim texto = TryCast(nombre, String)
            If String.IsNullOrWhiteSpace(texto) Then Return ""
            Dim palabras = texto.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
            Return String.Concat(palabras.Take(2).Select(Function(p) Char.ToUpper(p(0), CultureInfo.CurrentCulture)))
        End Function

        ''' <summary>Convierte una fecha guardada en UTC a la hora local del servidor.</summary>
        Public Shared Function FechaLocal(valor As Object) As String
            If Not TypeOf valor Is DateTime Then Return ""
            Return DateTime.SpecifyKind(CDate(valor), DateTimeKind.Utc).ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
        End Function

    End Class

End Namespace
