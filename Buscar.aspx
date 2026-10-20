<%@ Page ValidateRequestMode="Disabled" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Buscar.aspx.vb" Inherits="TaskManager.BuscarPagina" %>
<%@ Import Namespace="TaskManager.Utilidades" %>

<asp:Content ID="cContenido" ContentPlaceHolderID="Contenido" runat="server">
    <div class="pagina pagina-ancha">
        <h1>Buscar tareas</h1>

        <asp:Label ID="lblMensaje" runat="server" CssClass="mensaje" Visible="false" EnableViewState="false" />

        <asp:Repeater ID="rptGuardadas" runat="server">
            <HeaderTemplate>
                <div class="guardadas">
                    <span class="guardadas-titulo">Búsquedas guardadas:</span>
            </HeaderTemplate>
            <ItemTemplate>
                    <span class='<%# "guardada" & If(CInt(Eval("Id")) = BusquedaActivaId, " activa", "") & If(CBool(Eval("EsPropia")), "", " guardada-sin-borrar") %>'
                        title='<%#: TituloGuardada(CType(Container.DataItem, TaskManager.Models.BusquedaGuardada)) %>'>
                        <a href='<%#: ResolveUrl("~/Buscar.aspx") & "?busqueda=" & Eval("Id").ToString() %>'>
                            <%# If(CBool(Eval("Compartida")), "&#128101; ", "") %><%#: Eval("Nombre") %>
                        </a>
                        <asp:PlaceHolder runat="server" Visible='<%# Not CBool(Eval("EsPropia")) %>'>
                            <span class="guardada-autor">· <%#: Eval("NombreUsuario") %></span>
                        </asp:PlaceHolder>
                        <asp:LinkButton ID="lnkEliminarGuardada" runat="server" CssClass="guardada-eliminar"
                            CommandName="Eliminar" CommandArgument='<%# Eval("Id") %>' CausesValidation="false"
                            Visible='<%# CBool(Eval("EsPropia")) %>' ToolTip="Eliminar búsqueda guardada"
                            OnClientClick="return confirm('¿Eliminar esta búsqueda guardada?');">&times;</asp:LinkButton>
                    </span>
            </ItemTemplate>
            <FooterTemplate>
                </div>
            </FooterTemplate>
        </asp:Repeater>

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
                <div class="campo">
                    <asp:Label runat="server" AssociatedControlID="ddlPrioridad" Text="Prioridad" />
                    <asp:DropDownList ID="ddlPrioridad" runat="server">
                        <asp:ListItem Value="" Text="Cualquiera" />
                        <asp:ListItem Value="1" Text="Alta" />
                        <asp:ListItem Value="2" Text="Media" />
                        <asp:ListItem Value="3" Text="Baja" />
                        <asp:ListItem Value="sin" Text="Sin prioridad" />
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
                        <asp:CheckBox ID="chkEnComentarios" runat="server" Text="Mensajes del chat" />
                        <asp:CheckBox ID="chkEnSubtareas" runat="server" Text="Subtareas" />
                        <asp:CheckBox ID="chkEnEtiquetas" runat="server" Text="Etiquetas" Checked="true" />
                    </fieldset>
                    <%-- Etiquetas: se pintan dentro del cuadro, con sugerencias al escribir (Scripts/etiquetas.js en modo filtro) --%>
                    <div class="campo campo-etiquetas campo-doble">
                        <asp:Label runat="server" AssociatedControlID="txtEtiquetasFiltro" Text="Etiquetas" />
                        <asp:TextBox ID="txtEtiquetasFiltro" runat="server" MaxLength="600" autocomplete="off" spellcheck="false"
                            placeholder="#PLANIFICADOR_CABINAS #IT_CASES" />
                    </div>
                    <div class="campo">
                        <asp:Label runat="server" AssociatedControlID="ddlModoEtiquetas" Text="Con las etiquetas" />
                        <asp:DropDownList ID="ddlModoEtiquetas" runat="server">
                            <asp:ListItem Value="todas" Text="Todas las indicadas" />
                            <asp:ListItem Value="alguna" Text="Alguna de las indicadas" />
                            <asp:ListItem Value="sin" Text="Sin ninguna etiqueta" />
                        </asp:DropDownList>
                    </div>
                    <%-- Listas: desplegable con casillas (varias a la vez = alguna de ellas); Scripts/buscar.js actualiza el resumen --%>
                    <div class="campo">
                        <span class="campo-titulo" id="tituloListas">Listas</span>
                        <details id="desplegableListas" class='<%= If(cblListas.Items.Count > 0, "desplegable-checks", "desplegable-checks deshabilitado") %>'>
                            <summary aria-labelledby="tituloListas resumenListas"><span id="resumenListas"><asp:Literal ID="litResumenListas" runat="server" Mode="Encode" /></span></summary>
                            <div class="desplegable-checks-panel">
                                <asp:CheckBoxList ID="cblListas" runat="server" RepeatLayout="UnorderedList" CssClass="checks-lista" />
                            </div>
                        </details>
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
                            <asp:ListItem Value="prioridad" Text="Prioridad (alta primero)" />
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
            <div class="resultados-acciones">
                <details class="guardar-busqueda"<%= If(GuardarAbierto, " open=""open""", "") %>>
                    <summary class="boton boton-claro">&#128190; Guardar búsqueda</summary>
                    <asp:Panel ID="pnlGuardarBusqueda" runat="server" CssClass="guardar-busqueda-panel" DefaultButton="btnGuardarBusqueda">
                        <asp:Label runat="server" AssociatedControlID="txtNombreBusqueda" Text="Nombre" />
                        <asp:TextBox ID="txtNombreBusqueda" runat="server" MaxLength="100" placeholder="p. ej. Vencidas del equipo" />
                        <asp:RequiredFieldValidator ID="rfvNombreBusqueda" runat="server" ControlToValidate="txtNombreBusqueda"
                            ValidationGroup="GuardarBusqueda" ErrorMessage="Escribe un nombre." Display="Dynamic" CssClass="validacion" />
                        <asp:CheckBox ID="chkCompartirBusqueda" runat="server" Text="Compartir con mis compañeros" />
                        <small>La verán los usuarios con los que compartes algún tablero. Cada uno solo obtiene tareas de sus propios tableros. Si ya tienes una búsqueda con ese nombre, se sustituye.</small>
                        <asp:Button ID="btnGuardarBusqueda" runat="server" Text="Guardar" CssClass="boton" ValidationGroup="GuardarBusqueda" />
                    </asp:Panel>
                </details>
                <%-- Columnas de los resultados: Scripts/buscar.js las muestra/oculta al momento y las recuerda (cookie) --%>
                <details class="guardar-busqueda selector-columnas">
                    <summary class="boton boton-claro">&#9638; Columnas</summary>
                    <div class="guardar-busqueda-panel">
                        <span class="fondo-seccion">Columnas de los resultados</span>
                        <asp:CheckBoxList ID="cblColumnas" runat="server" RepeatLayout="UnorderedList" CssClass="checks-lista" />
                        <small>El título se muestra siempre. El Excel exporta solo estas columnas. Se recuerdan en este navegador y se guardan con la búsqueda.</small>
                        <asp:Button ID="btnAplicarColumnas" runat="server" Text="Aplicar" CssClass="boton boton-claro solo-sin-js" CausesValidation="false" />
                    </div>
                </details>
                <%-- Copia las columnas visibles de los resultados, lista para pegar en Excel (Scripts/buscar.js la muestra si hay resultados) --%>
                <button type="button" id="btnCopiar" class="boton boton-claro" hidden="hidden">&#128203; Copiar en portapapeles</button>
                <asp:Button ID="btnExportar" runat="server" Text="Exportar a Excel" CssClass="boton boton-claro" ValidationGroup="Busqueda" />
            </div>
        </div>

        <asp:Repeater ID="rptResultados" runat="server">
            <HeaderTemplate>
                <div class="tabla-contenedor">
                <%-- data-ocultas: columnas que no se muestran (el CSS oculta sus celdas data-col) --%>
                <table class="tabla-datos tabla-resultados" id="tablaResultados" data-ocultas='<%#: ColumnasOcultas %>'>
                    <thead>
                        <tr><th data-col="id" class="num">ID</th><th data-col="caso">Caso</th><th>Título</th><th data-col="etiquetas">Etiquetas</th><th data-col="prioridad">Prioridad</th>
                            <th data-col="tablero">Tablero · lista</th><th data-col="asignado">Asignada a</th><th data-col="limite">Fecha límite</th>
                            <th data-col="subtareas" class="num">Subtareas</th><th data-col="adjuntos" class="num">Adjuntos</th><th data-col="creada">Creada</th><th data-col="modificada">Modificada</th></tr>
                    </thead>
                    <tbody>
            </HeaderTemplate>
            <ItemTemplate>
                        <%-- data-copia: valores para «Copiar en portapapeles», en el orden de ColumnasCopiaJson --%>
                        <tr data-copia='<%#: FilaCopia(Container.DataItem) %>'>
                            <td data-col="id" class="num"><%# Eval("Tarjeta.Id") %></td>
                            <td data-col="caso" class="col-caso"><%#: If(Eval("Tarjeta.Caso") Is Nothing, "", "#" & CStr(Eval("Tarjeta.Caso"))) %></td>
                            <td class="col-titulo">
                                <a href='<%#: UrlTarjeta(CType(Container.DataItem, TaskManager.Models.TarjetaEncontrada)) %>'><%#: Eval("Tarjeta.Titulo") %></a>
                                <asp:PlaceHolder runat="server" Visible='<%# CBool(Eval("Tarjeta.TieneCodigo")) %>'><span class="info info-codigo" title="Tiene código">&lt;/&gt;</span></asp:PlaceHolder>
                                <asp:PlaceHolder runat="server" Visible='<%# CInt(Eval("Tarjeta.NumComentarios")) > 0 %>'><span class="info" title="Mensajes de chat">&#128172; <%# Eval("Tarjeta.NumComentarios") %></span></asp:PlaceHolder>
                            </td>
                            <td data-col="etiquetas">
                                <asp:Repeater ID="rptEtiquetasResultado" runat="server" DataSource='<%# Eval("Tarjeta.Etiquetas") %>'>
                                    <HeaderTemplate><span class="tarjeta-etiquetas"></HeaderTemplate>
                                    <ItemTemplate><span class="etiqueta-tag" style='<%#: Eval("EstiloCss") %>'><%#: Eval("Nombre") %></span></ItemTemplate>
                                    <FooterTemplate></span></FooterTemplate>
                                </asp:Repeater>
                            </td>
                            <td data-col="prioridad"><%# Formato.HtmlPrioridad(Eval("Tarjeta.Prioridad")) %></td>
                            <td data-col="tablero"><%#: Eval("NombreTablero") %><span class="secundario"><%#: Eval("Tarjeta.NombreLista") %></span></td>
                            <td data-col="asignado"><%#: If(Eval("Tarjeta.NombreAsignado"), "—") %></td>
                            <td data-col="limite"><%# Formato.HtmlFechaLimite(Eval("Tarjeta.FechaLimite")) %></td>
                            <td data-col="subtareas" class="num"><%# If(CInt(Eval("Tarjeta.SubtareasTotal")) = 0, "—", Eval("Tarjeta.SubtareasHechas").ToString() & "/" & Eval("Tarjeta.SubtareasTotal").ToString()) %></td>
                            <td data-col="adjuntos" class="num"><%# If(CInt(Eval("Tarjeta.NumAdjuntos")) = 0, "—", Eval("Tarjeta.NumAdjuntos").ToString()) %></td>
                            <td data-col="creada" class="col-fecha"><%#: Formato.FechaLocal(Eval("Tarjeta.FechaCreacion")) %></td>
                            <td data-col="modificada" class="col-fecha"><%#: Formato.FechaLocal(Eval("Tarjeta.FechaModificacion")) %><span class="secundario"><%#: Eval("Tarjeta.NombreModificador") %></span></td>
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

<asp:Content ID="cScripts" ContentPlaceHolderID="Scripts" runat="server">
    <%-- Etiquetas para las sugerencias del filtro; "<", ">" y "&" van escapados --%>
    <script type="application/json" id="etiquetas-datos"><%= EtiquetasJson %></script>
    <script>window.TaskManagerEtiquetas = { campo: '<%= txtEtiquetasFiltro.ClientID %>', datos: 'etiquetas-datos', modo: 'filtro' };</script>
    <script src="<%= Formato.UrlConVersion("~/Scripts/etiquetas.js") %>"></script>
    <%-- Columnas de «Copiar en portapapeles» (textos fijos del código) --%>
    <script type="application/json" id="copia-columnas"><%= ColumnasCopiaJson %></script>
    <script>
        window.TaskManagerBuscar = {
            listas: 'desplegableListas',
            resumenListas: 'resumenListas',
            columnas: '<%= cblColumnas.ClientID %>',
            tabla: 'tablaResultados',
            cookie: '<%= CookieColumnas %>',
            copiar: 'btnCopiar',
            copiaColumnas: 'copia-columnas'
        };
    </script>
    <script src="<%= Formato.UrlConVersion("~/Scripts/buscar.js") %>"></script>
</asp:Content>
