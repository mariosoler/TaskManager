<%@ Page ValidateRequestMode="Disabled" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Estadisticas.aspx.vb" Inherits="TaskManager.EstadisticasPagina" %>
<%@ Import Namespace="TaskManager.Utilidades" %>

<asp:Content ID="cContenido" ContentPlaceHolderID="Contenido" runat="server">
    <div class="pagina pagina-ancha viz-root">
        <div class="pagina-cabecera">
            <div>
                <a id="lnkVolver" runat="server" class="volver-oscuro">&larr; Volver al tablero</a>
                <h1>Estadísticas <span class="titulo-secundario"><asp:Literal ID="litNombre" runat="server" Mode="Encode" /></span></h1>
            </div>
            <div class="cabecera-acciones">
                <a id="lnkBuscar" runat="server" class="boton boton-claro">Buscar en este tablero</a>
                <asp:Button ID="btnExportar" runat="server" Text="Exportar a Excel" CssClass="boton" />
            </div>
        </div>

        <h2 class="seccion-titulo">Estado actual</h2>
        <div class="kpis">
            <div class="kpi">
                <span class="kpi-etiqueta">Tarjetas</span>
                <span class="kpi-valor"><asp:Literal ID="litTotal" runat="server" /></span>
            </div>
            <div class="kpi">
                <span class="kpi-etiqueta">Vencidas</span>
                <span class="kpi-valor"><asp:Literal ID="litVencidas" runat="server" /></span>
                <asp:PlaceHolder ID="phAvisoVencidas" runat="server">
                    <span class="estado"><span class="estado-icono estado-critico" aria-hidden="true">&#9888;</span> Requieren atención</span>
                </asp:PlaceHolder>
            </div>
            <div class="kpi">
                <span class="kpi-etiqueta">Sin asignar</span>
                <span class="kpi-valor"><asp:Literal ID="litSinAsignar" runat="server" /></span>
            </div>
            <div class="kpi">
                <span class="kpi-etiqueta">Prioridad alta</span>
                <span class="kpi-valor"><asp:Literal ID="litPrioridadAlta" runat="server" /></span>
                <span class="kpi-detalle"><asp:Literal ID="litDetallePrioridadAlta" runat="server" /></span>
            </div>
            <div class="kpi">
                <span class="kpi-etiqueta">Sin cambios en <%= DiasSinActividad %> días</span>
                <span class="kpi-valor"><asp:Literal ID="litSinActividad" runat="server" /></span>
                <asp:PlaceHolder ID="phAvisoSinActividad" runat="server">
                    <span class="estado"><span class="estado-icono estado-aviso" aria-hidden="true">&#9719;</span> Pueden estar olvidadas</span>
                </asp:PlaceHolder>
            </div>
            <div class="kpi">
                <span class="kpi-etiqueta">Subtareas completadas</span>
                <span class="kpi-valor"><asp:Literal ID="litPorcentajeSubtareas" runat="server" /></span>
                <span class="kpi-detalle"><asp:Literal ID="litDetalleSubtareas" runat="server" /></span>
                <span class="medidor" role="meter" aria-valuemin="0" aria-valuemax="100" aria-valuenow="<%= PorcentajeSubtareas %>" aria-label="Subtareas completadas">
                    <span class="medidor-relleno" style="width: <%= PorcentajeSubtareas %>%"></span>
                </span>
            </div>
            <div class="kpi">
                <span class="kpi-etiqueta">Mensajes de chat</span>
                <span class="kpi-valor"><asp:Literal ID="litComentarios" runat="server" /></span>
            </div>
        </div>

        <div class="rejilla-graficas">
            <figure class="grafica" id="grafica-por-lista">
                <figcaption><h3>Tarjetas por lista</h3><p class="grafica-subtitulo">Reparto del total; con más de 6 listas, las menores se agrupan en «Otras listas»</p></figcaption>
                <div class="grafica-cuerpo"></div>
            </figure>
            <figure class="grafica" id="grafica-por-prioridad">
                <figcaption><h3>Tarjetas por prioridad</h3><p class="grafica-subtitulo">Mismos colores que en el tablero</p></figcaption>
                <div class="grafica-cuerpo"></div>
            </figure>
            <figure class="grafica grafica-ancha" id="grafica-fechas">
                <figcaption><h3>Fechas límite</h3><p class="grafica-subtitulo">Reparto de las tarjetas según su fecha límite</p></figcaption>
                <div class="grafica-cuerpo"></div>
            </figure>
            <figure class="grafica grafica-ancha" id="tabla-proximos">
                <figcaption><h3>Próximos vencimientos</h3><p class="grafica-subtitulo"><asp:Literal ID="litSubtituloProximos" runat="server" Mode="Encode" /></p></figcaption>
                <asp:Repeater ID="rptProximos" runat="server">
                    <HeaderTemplate>
                        <div class="tabla-contenedor tabla-incrustada">
                        <table class="tabla-datos">
                            <thead><tr><th>Caso</th><th>Tarjeta</th><th>Lista</th><th>Asignada a</th><th>Prioridad</th><th>Fecha límite</th></tr></thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                                <tr>
                                    <td class="col-caso"><%#: Eval("Caso") %></td>
                                    <td class="col-titulo"><a href='<%#: UrlTarjeta(Eval("Id")) %>'><%#: Eval("Titulo") %></a></td>
                                    <td><%#: Eval("NombreLista") %></td>
                                    <td><%#: If(Eval("NombreAsignado"), "—") %></td>
                                    <td><%# Formato.HtmlPrioridad(Eval("Prioridad")) %></td>
                                    <td class="col-fecha"><%# Formato.HtmlFechaLimite(Eval("FechaLimite")) %></td>
                                </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                        </div>
                    </FooterTemplate>
                </asp:Repeater>
                <asp:PlaceHolder ID="phSinProximos" runat="server">
                    <p class="grafica-vacia">No hay tarjetas vencidas ni que venzan pronto.</p>
                </asp:PlaceHolder>
            </figure>
            <figure class="grafica" id="grafica-por-persona">
                <figcaption><h3>Tarjetas por persona asignada</h3></figcaption>
                <div class="grafica-cuerpo"></div>
            </figure>
            <figure class="grafica" id="grafica-antiguedad">
                <figcaption><h3>Antigüedad de las tarjetas</h3><p class="grafica-subtitulo">Tiempo desde que se crearon</p></figcaption>
                <div class="grafica-cuerpo"></div>
            </figure>
            <figure class="grafica grafica-ancha" id="grafica-por-etiqueta">
                <figcaption><h3>Tarjetas por etiqueta</h3><p class="grafica-subtitulo">Una tarjeta cuenta en cada una de sus etiquetas. Pulsa una etiqueta para ver sus tarjetas en el buscador.</p></figcaption>
                <div class="grafica-cuerpo"></div>
            </figure>
        </div>

        <h2 class="seccion-titulo">Actividad</h2>
        <div class="filtro-periodo" role="group" aria-label="Periodo de actividad">
            <span class="filtro-periodo-etiqueta">Periodo:</span>
            <asp:HyperLink ID="lnkDias7" runat="server" Text="Últimos 7 días" />
            <asp:HyperLink ID="lnkDias30" runat="server" Text="Últimos 30 días" />
            <asp:HyperLink ID="lnkDias90" runat="server" Text="Últimos 90 días" />
        </div>
        <div class="kpis">
            <div class="kpi">
                <span class="kpi-etiqueta">Cambios</span>
                <span class="kpi-valor"><asp:Literal ID="litCambios" runat="server" /></span>
            </div>
            <div class="kpi">
                <span class="kpi-etiqueta">Media diaria</span>
                <span class="kpi-valor"><asp:Literal ID="litMediaDiaria" runat="server" /></span>
            </div>
            <div class="kpi">
                <span class="kpi-etiqueta">Personas con actividad</span>
                <span class="kpi-valor"><asp:Literal ID="litPersonasActivas" runat="server" /></span>
            </div>
            <div class="kpi">
                <span class="kpi-etiqueta">Tarjetas creadas</span>
                <span class="kpi-valor"><asp:Literal ID="litCreadas" runat="server" /></span>
                <span class="kpi-detalle"><asp:Literal ID="litDetalleCreadas" runat="server" Mode="Encode" /></span>
            </div>
        </div>
        <div class="rejilla-graficas">
            <figure class="grafica grafica-ancha" id="grafica-por-dia">
                <figcaption><h3>Cambios por día</h3></figcaption>
                <div class="grafica-cuerpo"></div>
            </figure>
            <figure class="grafica" id="grafica-cambios-persona">
                <figcaption><h3>Cambios por persona</h3></figcaption>
                <div class="grafica-cuerpo"></div>
            </figure>
            <figure class="grafica" id="grafica-cambios-tipo">
                <figcaption><h3>Cambios por tipo</h3><p class="grafica-subtitulo">Reparto de los cambios del periodo</p></figcaption>
                <div class="grafica-cuerpo"></div>
            </figure>
        </div>
    </div>
</asp:Content>

<asp:Content ID="cScripts" ContentPlaceHolderID="Scripts" runat="server">
    <%-- Datos de las gráficas: JSON con "<" escapado (no puede cerrar el <script>). Los textos se insertan con textContent. --%>
    <script type="application/json" id="datos-graficas"><%= DatosJson %></script>
    <script src="<%= Formato.UrlConVersion("~/Scripts/graficas.js") %>"></script>
</asp:Content>
