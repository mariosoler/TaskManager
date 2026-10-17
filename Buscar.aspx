<%@ Page ValidateRequestMode="Disabled" Title="Buscar tareas" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Buscar.aspx.vb" Inherits="TaskManager.BuscarPagina" %>
<%@ Import Namespace="TaskManager.Utilidades" %>

<asp:Content ID="cContenido" ContentPlaceHolderID="Contenido" runat="server">
    <div class="pagina pagina-ancha">
        <h1>Buscar tareas</h1>

        <asp:Panel ID="pnlFiltros" runat="server" CssClass="filtros" DefaultButton="btnBuscar">
            <div class="filtros-basicos">
                <div class="campo campo-texto">
                    <asp:Label runat="server" AssociatedControlID="txtTexto" Text="Texto" />
                    <asp:TextBox ID="txtTexto" runat="server" MaxLength="400" placeholder="Palabras del título, caso, descripción…" />
                </div>
                <div class="campo">
                    <asp:Label runat="server" AssociatedControlID="ddlTablero" Text="Tablero" />
                    <asp:DropDownList ID="ddlTablero" runat="server" AutoPostBack="true" />
                </div>
                <div class="campo">
                    <asp:Label runat="server" AssociatedControlID="ddlAsignado" Text="Asignada a" />
                    <asp:DropDownList ID="ddlAsignado" runat="server" />
                </div>
                <div class="campo">
                    <asp:Label runat="server" AssociatedControlID="ddlEstadoFecha" Text="Fecha límite" />
                    <asp:DropDownList ID="ddlEstadoFecha" runat="server">
                        <asp:ListItem Value="" Text="Cualquiera" />
                        <asp:ListItem Value="vencidas" Text="Vencidas" />
                        <asp:ListItem Value="hoy" Text="Vencen hoy" />
                        <asp:ListItem Value="semana" Text="Hoy y próximos 7 días" />
                        <asp:ListItem Value="con" Text="Con fecha" />
                        <asp:ListItem Value="sin" Text="Sin fecha" />
                    </asp:DropDownList>
                </div>
                <div class="filtros-acciones">
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar" CssClass="boton" ValidationGroup="Busqueda" />
                    <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar" CssClass="boton boton-claro" CausesValidation="false" />
                </div>
            </div>

            <details class="filtros-avanzados"<%= If(AvanzadaAbierta, " open=""open""", "") %>>
                <summary>Búsqueda avanzada</summary>
                <div class="filtros-rejilla">
                    <fieldset class="campo campo-ancho">
                        <legend>Buscar el texto en</legend>
                        <asp:CheckBox ID="chkEnTitulo" runat="server" Text="Título y caso" Checked="true" />
                        <asp:CheckBox ID="chkEnDescripcion" runat="server" Text="Descripción" Checked="true" />
                        <asp:CheckBox ID="chkEnCodigo" runat="server" Text="Código" />
                        <asp:CheckBox ID="chkEnComentarios" runat="server" Text="Comentarios" />
                        <asp:CheckBox ID="chkEnSubtareas" runat="server" Text="Subtareas" />
                    </fieldset>
                    <div class="campo">
                        <asp:Label runat="server" AssociatedControlID="ddlLista" Text="Lista" />
                        <asp:DropDownList ID="ddlLista" runat="server" />
                    </div>
                    <div class="campo">
                        <asp:Label runat="server" AssociatedControlID="txtCaso" Text="Nº de caso exacto" />
                        <asp:TextBox ID="txtCaso" runat="server" MaxLength="20" inputmode="numeric" />
                        <asp:RegularExpressionValidator ID="revCaso" runat="server" ControlToValidate="txtCaso" ValidationGroup="Busqueda"
                            ValidationExpression="^\s*[0-9]{1,20}\s*$" ErrorMessage="Solo números." Display="Dynamic" CssClass="validacion" />
                    </div>
                    <div class="campo">
                        <asp:Label runat="server" AssociatedControlID="ddlCambiadaPor" Text="Con cambios de" />
                        <asp:DropDownList ID="ddlCambiadaPor" runat="server" />
                    </div>
                    <div class="campo campo-rango">
                        <asp:Label runat="server" AssociatedControlID="txtLimiteDesde" Text="Fecha límite entre" />
                        <span><asp:TextBox ID="txtLimiteDesde" runat="server" TextMode="Date" /> y <asp:TextBox ID="txtLimiteHasta" runat="server" TextMode="Date" /></span>
                    </div>
                    <div class="campo campo-rango">
                        <asp:Label runat="server" AssociatedControlID="txtCreadaDesde" Text="Creada entre" />
                        <span><asp:TextBox ID="txtCreadaDesde" runat="server" TextMode="Date" /> y <asp:TextBox ID="txtCreadaHasta" runat="server" TextMode="Date" /></span>
                    </div>
                    <div class="campo campo-rango">
                        <asp:Label runat="server" AssociatedControlID="txtModificadaDesde" Text="Modificada entre" />
                        <span><asp:TextBox ID="txtModificadaDesde" runat="server" TextMode="Date" /> y <asp:TextBox ID="txtModificadaHasta" runat="server" TextMode="Date" /></span>
                    </div>
                    <div class="campo">
                        <asp:Label runat="server" AssociatedControlID="ddlSubtareas" Text="Subtareas" />
                        <asp:DropDownList ID="ddlSubtareas" runat="server">
                            <asp:ListItem Value="" Text="Cualquiera" />
                            <asp:ListItem Value="pendientes" Text="Con subtareas pendientes" />
                            <asp:ListItem Value="completas" Text="Todas completadas" />
                            <asp:ListItem Value="sin" Text="Sin subtareas" />
                        </asp:DropDownList>
                    </div>
                    <div class="campo">
                        <asp:Label runat="server" AssociatedControlID="ddlCodigo" Text="Código" />
                        <asp:DropDownList ID="ddlCodigo" runat="server">
                            <asp:ListItem Value="" Text="Cualquiera" />
                            <asp:ListItem Value="si" Text="Con código" />
                            <asp:ListItem Value="no" Text="Sin código" />
                        </asp:DropDownList>
                    </div>
                    <div class="campo">
                        <asp:Label runat="server" AssociatedControlID="ddlOrden" Text="Ordenar por" />
                        <asp:DropDownList ID="ddlOrden" runat="server">
                            <asp:ListItem Value="modificada" Text="Última modificación" />
                            <asp:ListItem Value="creada" Text="Fecha de creación" />
                            <asp:ListItem Value="limite" Text="Fecha límite" />
                            <asp:ListItem Value="caso" Text="Nº de caso" />
                            <asp:ListItem Value="titulo" Text="Título" />
                            <asp:ListItem Value="tablero" Text="Tablero y lista" />
                        </asp:DropDownList>
                    </div>
                </div>
            </details>
        </asp:Panel>

        <div class="resultados-cabecera">
            <p class="resultados-resumen"><asp:Literal ID="litResumen" runat="server" Mode="Encode" /></p>
            <asp:Button ID="btnExportar" runat="server" Text="Exportar a Excel" CssClass="boton boton-claro" ValidationGroup="Busqueda" />
        </div>

        <asp:Repeater ID="rptResultados" runat="server">
            <HeaderTemplate>
                <div class="tabla-contenedor">
                <table class="tabla-datos">
                    <thead>
                        <tr><th>Caso</th><th>Título</th><th>Tablero · lista</th><th>Asignada a</th><th>Fecha límite</th><th class="num">Subtareas</th><th>Modificada</th></tr>
                    </thead>
                    <tbody>
            </HeaderTemplate>
            <ItemTemplate>
                        <tr>
                            <td class="col-caso"><%#: If(Eval("Tarjeta.Caso") Is Nothing, "", "#" & CStr(Eval("Tarjeta.Caso"))) %></td>
                            <td class="col-titulo">
                                <a href='<%#: UrlTarjeta(CType(Container.DataItem, TaskManager.Models.TarjetaEncontrada)) %>'><%#: Eval("Tarjeta.Titulo") %></a>
                                <asp:PlaceHolder runat="server" Visible='<%# CBool(Eval("Tarjeta.TieneCodigo")) %>'><span class="info info-codigo" title="Tiene código">&lt;/&gt;</span></asp:PlaceHolder>
                                <asp:PlaceHolder runat="server" Visible='<%# CInt(Eval("Tarjeta.NumComentarios")) > 0 %>'><span class="info" title="Comentarios">&#128172; <%# Eval("Tarjeta.NumComentarios") %></span></asp:PlaceHolder>
                            </td>
                            <td><%#: Eval("NombreTablero") %><span class="secundario"><%#: Eval("Tarjeta.NombreLista") %></span></td>
                            <td><%#: If(Eval("Tarjeta.NombreAsignado"), "—") %></td>
                            <td><%# Formato.HtmlFechaLimite(Eval("Tarjeta.FechaLimite")) %></td>
                            <td class="num"><%# If(CInt(Eval("Tarjeta.SubtareasTotal")) = 0, "—", Eval("Tarjeta.SubtareasHechas").ToString() & "/" & Eval("Tarjeta.SubtareasTotal").ToString()) %></td>
                            <td class="col-fecha"><%#: Formato.FechaLocal(Eval("Tarjeta.FechaModificacion")) %><span class="secundario"><%#: Eval("Tarjeta.NombreModificador") %></span></td>
                        </tr>
            </ItemTemplate>
            <FooterTemplate>
                    </tbody>
                </table>
                </div>
            </FooterTemplate>
        </asp:Repeater>

        <asp:PlaceHolder ID="phSinResultados" runat="server" Visible="false">
            <p class="vacio">No hay tareas que coincidan con la búsqueda.</p>
        </asp:PlaceHolder>
    </div>
</asp:Content>
