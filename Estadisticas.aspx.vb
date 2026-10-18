Imports System.Globalization
Imports System.Web.Script.Serialization
Imports TaskManager.Datos
Imports TaskManager.Models
Imports TaskManager.Seguridad
Imports TaskManager.Utilidades

Public Class EstadisticasPagina
    Inherits PaginaBase

    Private Shared ReadOnly PeriodosPermitidos As Integer() = {7, 30, 90}
    Private Const PeriodoPorDefecto As Integer = 30
    Private Const MaxTarjetasExportacion As Integer = 5000

    Private _tablero As Tablero
    Private _dias As Integer
    Private _estadisticas As Estadisticas

    Protected Property PorcentajeSubtareas As Integer
    Protected ReadOnly Property DiasSinActividad As Integer = EstadisticasDatos.DiasSinActividad
    Protected Property DatosJson As String

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim id As Integer
        If Integer.TryParse(Request.QueryString("id"), id) Then _tablero = TablerosDatos.Obtener(id, UsuarioId)
        If _tablero Is Nothing Then Response.Redirect("~/Tableros.aspx")
        CType(Master, SiteMaster).TableroActual = _tablero.Id

        _dias = PeriodoPorDefecto
        Dim dias As Integer
        If Integer.TryParse(Request.QueryString("dias"), dias) AndAlso PeriodosPermitidos.Contains(dias) Then _dias = dias

        _estadisticas = EstadisticasDatos.Obtener(_tablero.Id, _dias)
        Mostrar()
    End Sub

    Protected Sub btnExportar_Click(sender As Object, e As EventArgs) Handles btnExportar.Click
        Dim est = _estadisticas
        Dim libro As New LibroExcel()

        libro.AgregarHoja("Resumen", {"Indicador", "Valor"}, New List(Of Object()) From {
            New Object() {"Tablero", _tablero.Nombre},
            New Object() {"Generado", DateTime.Now},
            New Object() {"Tarjetas", est.TotalTarjetas},
            New Object() {"Vencidas", est.Vencidas},
            New Object() {"Vencen en 7 días", est.VencenEstaSemana},
            New Object() {"Fecha límite más adelante", est.VencenMasAdelante},
            New Object() {"Sin fecha límite", est.SinFecha},
            New Object() {"Sin asignar", est.SinAsignar},
            New Object() {"Sin etiqueta", est.SinEtiqueta},
            New Object() {"Prioridad alta", est.PrioridadAlta},
            New Object() {"Sin cambios en " & EstadisticasDatos.DiasSinActividad & " días", est.SinActividad},
            New Object() {"Subtareas", est.SubtareasTotal},
            New Object() {"Subtareas completadas", est.SubtareasHechas},
            New Object() {"Mensajes de chat", est.Comentarios},
            New Object() {"Periodo de actividad (días)", est.Dias},
            New Object() {"Cambios en el periodo", est.TotalCambios},
            New Object() {"Tarjetas creadas en el periodo", est.CreadasPeriodo},
            New Object() {"Tarjetas creadas en el periodo anterior", est.CreadasPeriodoAnterior}
        })
        libro.AgregarHoja("Por lista", {"Lista", "Tarjetas"}, Filas(est.PorLista))
        libro.AgregarHoja("Por persona", {"Persona", "Tarjetas asignadas"}, Filas(est.PorPersona))
        libro.AgregarHoja("Por etiqueta", {"Etiqueta", "Tarjetas", "Vencidas", "Sin asignar"},
                          est.PorEtiqueta.Select(Function(d) New Object() {d.Etiqueta.Nombre, d.Tarjetas, d.Vencidas, d.SinAsignar}).
                              Concat({New Object() {"Sin etiqueta", est.SinEtiqueta, Nothing, Nothing}}))
        libro.AgregarHoja("Por prioridad", {"Prioridad", "Tarjetas"}, Filas(est.PorPrioridad))
        libro.AgregarHoja("Fechas límite", {"Estado", "Tarjetas"}, Filas(SegmentosFechas(est)))
        libro.AgregarHoja("Próximos vencimientos", {"Caso", "Tarjeta", "Lista", "Asignada a", "Prioridad", "Fecha límite"},
                          est.ProximosVencimientos.Select(Function(t) New Object() {
                              t.Caso, t.Titulo, t.NombreLista, t.NombreAsignado, Prioridad.Nombre(t.Prioridad), t.FechaLimite.Date}))
        libro.AgregarHoja("Antigüedad", {"Antigüedad", "Tarjetas"}, Filas(est.Antiguedad))
        libro.AgregarHoja("Actividad diaria", {"Día", "Cambios"},
                          est.CambiosPorDia.Select(Function(d) New Object() {d.Dia, d.Valor}))
        libro.AgregarHoja("Cambios por persona", {"Persona", "Cambios"}, Filas(est.CambiosPorPersona))
        libro.AgregarHoja("Cambios por tipo", {"Tipo", "Cambios"}, Filas(est.CambiosPorTipo))

        Dim tarjetas = BusquedaDatos.Buscar(UsuarioId,
            New FiltroBusqueda With {.TableroId = _tablero.Id, .Orden = FiltroBusqueda.OrdenTablero}, MaxTarjetasExportacion)
        ExportacionExcel.AgregarHojaTarjetas(libro, "Tarjetas", tarjetas.Tarjetas)

        EnviarArchivo(libro.ABytes(),
                      "estadisticas-tablero-" & _tablero.Id.ToString(CultureInfo.InvariantCulture) & "-" &
                      DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) & ".xlsx",
                      LibroExcel.TipoMime)
    End Sub

    Private Sub Mostrar()
        Dim est = _estadisticas
        Dim url = ResolveUrl("~/Estadisticas.aspx") & "?id=" & _tablero.Id.ToString(CultureInfo.InvariantCulture)

        litNombre.Text = _tablero.Nombre
        lnkVolver.HRef = "~/Tablero.aspx?id=" & _tablero.Id.ToString(CultureInfo.InvariantCulture)
        lnkBuscar.HRef = "~/Buscar.aspx?tablero=" & _tablero.Id.ToString(CultureInfo.InvariantCulture)

        litTotal.Text = Numero(est.TotalTarjetas)
        litVencidas.Text = Numero(est.Vencidas)
        phAvisoVencidas.Visible = est.Vencidas > 0
        litSinAsignar.Text = Numero(est.SinAsignar)
        litPrioridadAlta.Text = Numero(est.PrioridadAlta)
        litDetallePrioridadAlta.Text = If(est.TotalTarjetas = 0, "",
            Math.Round(100.0 * est.PrioridadAlta / est.TotalTarjetas).ToString(CultureInfo.CurrentCulture) & " % del total")
        litSinActividad.Text = Numero(est.SinActividad)
        phAvisoSinActividad.Visible = est.SinActividad > 0

        litSubtituloProximos.Text = String.Format(CultureInfo.CurrentCulture,
            "Vencidas y las que vencen en los próximos {0} días; las {1} más urgentes. Pulsa una para abrirla.",
            EstadisticasDatos.DiasProximos, EstadisticasDatos.MaxProximos)
        rptProximos.DataSource = est.ProximosVencimientos
        rptProximos.DataBind()
        phSinProximos.Visible = est.ProximosVencimientos.Count = 0
        litComentarios.Text = Numero(est.Comentarios)
        If est.SubtareasTotal = 0 Then
            PorcentajeSubtareas = 0
            litPorcentajeSubtareas.Text = "—"
            litDetalleSubtareas.Text = "Ninguna subtarea"
        Else
            PorcentajeSubtareas = CInt(Math.Round(100.0 * est.SubtareasHechas / est.SubtareasTotal))
            litPorcentajeSubtareas.Text = PorcentajeSubtareas.ToString(CultureInfo.CurrentCulture) & " %"
            litDetalleSubtareas.Text = String.Format(CultureInfo.CurrentCulture, "{0:N0} de {1:N0}", est.SubtareasHechas, est.SubtareasTotal)
        End If

        For Each enlace In {lnkDias7, lnkDias30, lnkDias90}
            Dim dias = CInt(enlace.ID.Substring("lnkDias".Length))
            enlace.NavigateUrl = url & "&dias=" & dias.ToString(CultureInfo.InvariantCulture)
            enlace.CssClass = If(dias = _dias, "activo", "")
            If dias = _dias Then enlace.Attributes("aria-current") = "true"
        Next
        litCambios.Text = Numero(est.TotalCambios)
        litMediaDiaria.Text = (CDbl(est.TotalCambios) / est.Dias).ToString("0.#", CultureInfo.CurrentCulture)
        litPersonasActivas.Text = Numero(est.CambiosPorPersona.Count)
        litCreadas.Text = Numero(est.CreadasPeriodo)
        litDetalleCreadas.Text = TextoComparacion(est.CreadasPeriodo, est.CreadasPeriodoAnterior, est.Dias)

        DatosJson = CrearJson(est)
    End Sub

    ''' <summary>Datos de las gráficas en JSON. "&lt;" se escapa para que el contenido no pueda cerrar el &lt;script&gt;.</summary>
    Private Function CrearJson(est As Estadisticas) As String
        Dim cultura = CultureInfo.CurrentCulture
        Dim datos = New Dictionary(Of String, Object) From {
            {"dias", est.Dias},
            {"porLista", est.PorLista.Select(Function(d) New With {.e = d.Etiqueta, .v = d.Valor})},
            {"porPrioridad", est.PorPrioridad.Select(Function(d) New With {.e = d.Etiqueta, .v = d.Valor, .clave = d.Clave})},
            {"antiguedad", est.Antiguedad.Select(Function(d) New With {.e = d.Etiqueta, .v = d.Valor})},
            {"porPersona", est.PorPersona.Select(Function(d) New With {.e = d.Etiqueta, .v = d.Valor, .gris = d.Clave = "sin-asignar"})},
            {"fechas", SegmentosFechas(est).Select(Function(d) New With {.e = d.Etiqueta, .v = d.Valor, .estado = d.Clave})},
            {"porDia", est.CambiosPorDia.Select(Function(d) New With {
                .corta = Formato.FechaDiaCorta(d.Dia),
                .larga = cultura.TextInfo.ToTitleCase(d.Dia.ToString("ddd", cultura)) & " " & Formato.FechaDia(d.Dia),
                .v = d.Valor})},
            {"cambiosPersona", est.CambiosPorPersona.Select(Function(d) New With {.e = d.Etiqueta, .v = d.Valor})},
            {"cambiosTipo", est.CambiosPorTipo.Select(Function(d) New With {.e = d.Etiqueta, .v = d.Valor})},
            {"porEtiqueta", DatosPorEtiqueta(est)}
        }
        Dim json = New JavaScriptSerializer().Serialize(datos)
        Return json.Replace("<", "\u003c").Replace(">", "\u003e").Replace("&", "\u0026")
    End Function

    ''' <summary>
    ''' Barras de «Tarjetas por etiqueta»: cada etiqueta con su color (solo para la muestra junto al nombre; la barra
    ''' usa el color de serie) y un enlace al buscador filtrado por ella; «Sin etiqueta» al final, en gris.
    ''' </summary>
    Private Function DatosPorEtiqueta(est As Estadisticas) As IEnumerable(Of Object)
        Dim buscar = ResolveUrl("~/Buscar.aspx") & "?tablero=" & _tablero.Id.ToString(CultureInfo.InvariantCulture) & "&etiqueta="
        Dim filas = est.PorEtiqueta.Select(Function(d) New With {
            .e = d.Etiqueta.Nombre, .v = d.Tarjetas, .gris = False,
            .color = d.Etiqueta.ColorSeguro, .url = buscar & HttpUtility.UrlEncode(d.Etiqueta.Nombre)}).ToList()
        If est.SinEtiqueta > 0 AndAlso filas.Count > 0 Then
            filas.Add(New With {.e = "Sin etiqueta", .v = est.SinEtiqueta, .gris = True, .color = "", .url = ""})
        End If
        Return filas.Cast(Of Object)()
    End Function

    ''' <summary>Fechas límite como segmentos de estado (Clave = estado para el color de estado).</summary>
    Private Shared Function SegmentosFechas(est As Estadisticas) As List(Of Dato)
        Return New List(Of Dato) From {
            New Dato With {.Etiqueta = "Vencidas", .Valor = est.Vencidas, .Clave = "critico"},
            New Dato With {.Etiqueta = "Vencen en 7 días", .Valor = est.VencenEstaSemana, .Clave = "aviso"},
            New Dato With {.Etiqueta = "Más adelante", .Valor = est.VencenMasAdelante, .Clave = "bien"},
            New Dato With {.Etiqueta = "Sin fecha", .Valor = est.SinFecha, .Clave = "neutro"}
        }
    End Function

    ''' <summary>Enlace que abre la tarjeta en el tablero (Tablero.aspx?id=N&amp;tarjeta=M).</summary>
    Protected Function UrlTarjeta(tarjetaId As Object) As String
        Return ResolveUrl("~/Tablero.aspx") & "?id=" & _tablero.Id.ToString(CultureInfo.InvariantCulture) &
               "&tarjeta=" & CInt(tarjetaId).ToString(CultureInfo.InvariantCulture)
    End Function

    ''' <summary>"▲ 3 más que en los 30 días anteriores (5)": la flecha y el texto dicen el sentido, no el color.</summary>
    Private Shared Function TextoComparacion(actual As Integer, anterior As Integer, dias As Integer) As String
        Dim diferencia = actual - anterior
        Dim periodo = String.Format(CultureInfo.CurrentCulture, "los {0} días anteriores ({1:N0})", dias, anterior)
        If diferencia = 0 Then Return "= Igual que en " & periodo
        Dim cantidad = Math.Abs(diferencia).ToString("N0", CultureInfo.CurrentCulture)
        Return If(diferencia > 0, "▲ " & cantidad & " más que en ", "▼ " & cantidad & " menos que en ") & periodo
    End Function

    Private Shared Function Filas(datos As IEnumerable(Of Dato)) As IEnumerable(Of Object())
        Return datos.Select(Function(d) New Object() {d.Etiqueta, d.Valor})
    End Function

    Private Shared Function Numero(valor As Integer) As String
        Return valor.ToString("N0", CultureInfo.CurrentCulture)
    End Function

End Class
