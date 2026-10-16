Imports System.Globalization
Imports System.Security.Cryptography

Namespace Seguridad

    ''' <summary>
    ''' Hash de contraseñas con PBKDF2-SHA256 y sal aleatoria.
    ''' Formato almacenado: "PBKDF2-SHA256$iteraciones$salBase64$hashBase64".
    ''' </summary>
    Public NotInheritable Class PasswordHasher

        Private Const Algoritmo As String = "PBKDF2-SHA256"
        Private Const Iteraciones As Integer = 210000
        Private Const TamanoSal As Integer = 16
        Private Const TamanoHash As Integer = 32

        Private Sub New()
        End Sub

        Public Shared Function Hash(password As String) As String
            Dim sal(TamanoSal - 1) As Byte
            Using rng = RandomNumberGenerator.Create()
                rng.GetBytes(sal)
            End Using

            Dim derivado = Derivar(password, sal, Iteraciones, TamanoHash)
            Return String.Join("$", Algoritmo,
                               Iteraciones.ToString(CultureInfo.InvariantCulture),
                               Convert.ToBase64String(sal),
                               Convert.ToBase64String(derivado))
        End Function

        Public Shared Function Verificar(password As String, almacenado As String) As Boolean
            If String.IsNullOrEmpty(password) OrElse String.IsNullOrEmpty(almacenado) Then Return False

            Dim partes = almacenado.Split("$"c)
            If partes.Length <> 4 OrElse partes(0) <> Algoritmo Then Return False

            Dim iter As Integer
            If Not Integer.TryParse(partes(1), NumberStyles.None, CultureInfo.InvariantCulture, iter) OrElse iter <= 0 Then Return False

            Dim sal, esperado As Byte()
            Try
                sal = Convert.FromBase64String(partes(2))
                esperado = Convert.FromBase64String(partes(3))
            Catch ex As FormatException
                Return False
            End Try

            Dim calculado = Derivar(password, sal, iter, esperado.Length)
            Return IgualesEnTiempoConstante(calculado, esperado)
        End Function

        Private Shared Function Derivar(password As String, sal As Byte(), iter As Integer, longitud As Integer) As Byte()
            Using pbkdf2 As New Rfc2898DeriveBytes(password, sal, iter, HashAlgorithmName.SHA256)
                Return pbkdf2.GetBytes(longitud)
            End Using
        End Function

        Private Shared Function IgualesEnTiempoConstante(a As Byte(), b As Byte()) As Boolean
            Dim diferencia = a.Length Xor b.Length
            For i = 0 To Math.Min(a.Length, b.Length) - 1
                diferencia = diferencia Or (CInt(a(i)) Xor CInt(b(i)))
            Next
            Return diferencia = 0
        End Function

    End Class

End Namespace
