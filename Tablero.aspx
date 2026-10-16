<%@ Page ValidateRequestMode="Disabled" Title="Tablero" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Tablero.aspx.vb" Inherits="TaskManager.TableroPagina" %>
<%@ Import Namespace="TaskManager.Utilidades" %>

<asp:Content ID="cContenido" ContentPlaceHolderID="Contenido" runat="server">
    <div class="tablero-cabecera">
        <div>
            <a href="Tableros.aspx" class="volver">&larr; Mis tableros</a>
            <h1><asp:Literal ID="litNombre" runat="server" Mode="Encode" /></h1>
            <p class="tablero-descripcion"><asp:Literal ID="litDescripcion" runat="server" Mode="Encode" /></p>
        </div>

        <div class="miembros">
            <asp:Repeater ID="rptMiembros" runat="server">
                <ItemTemplate>
                    <span class="miembro" title='<%#: Eval("Email") %>'>
                        <span class="avatar"><%#: Formato.Iniciales(Eval("Nombre")) %></span>
                        <span><%#: Eval("Nombre") %> <small>(<%#: Eval("Rol") %>)</small></span>
                        <asp:LinkButton ID="lnkQuitar" runat="server" CssClass="miembro-quitar" ToolTip="Quitar del tablero"
                            CommandName="Quitar" CommandArgument='<%# Eval("UsuarioId") %>' CausesValidation="false"
                            Visible='<%# PuedeQuitarMiembro(Eval("Rol")) %>'
                            OnClientClick="return confirm('¿Quitar a este miembro del tablero?');">&times;</asp:LinkButton>
                    </span>
                </ItemTemplate>
            </asp:Repeater>
            <asp:Panel ID="pnlAgregarMiembro" runat="server" CssClass="form-linea" DefaultButton="btnAgregarMiembro">
                <asp:TextBox ID="txtEmailMiembro" runat="server" MaxLength="200" placeholder="Email del nuevo miembro" />
                <asp:Button ID="btnAgregarMiembro" runat="server" Text="Añadir" CssClass="boton boton-claro" CausesValidation="false" />
            </asp:Panel>
        </div>
    </div>

    <asp:Label ID="lblMensaje" runat="server" CssClass="mensaje" Visible="false" EnableViewState="false" />

    <div class="tablero">
        <asp:Repeater ID="rptListas" runat="server">
            <ItemTemplate>
                <section class="lista">
                    <header class="lista-cabecera">
                        <h2><%#: Eval("Titulo") %></h2>
                        <span class="lista-contador"><%# Eval("Tarjetas.Count") %></span>
                        <span class="lista-acciones">
                            <asp:LinkButton ID="lnkIzquierda" runat="server" ToolTip="Mover a la izquierda" CausesValidation="false"
                                CommandName="MoverIzquierda" CommandArgument='<%# Eval("Id") %>'>&#9664;</asp:LinkButton>
                            <asp:LinkButton ID="lnkDerecha" runat="server" ToolTip="Mover a la derecha" CausesValidation="false"
                                CommandName="MoverDerecha" CommandArgument='<%# Eval("Id") %>'>&#9654;</asp:LinkButton>
                            <asp:LinkButton ID="lnkEliminarLista" runat="server" ToolTip="Eliminar lista" CausesValidation="false"
                                CommandName="EliminarLista" CommandArgument='<%# Eval("Id") %>' CssClass="peligro"
                                OnClientClick="return confirm('¿Eliminar la lista y todas sus tarjetas?');">&times;</asp:LinkButton>
                        </span>
                    </header>

                    <div class="tarjetas" data-lista-id='<%# Eval("Id") %>'>
                        <asp:Repeater ID="rptTarjetas" runat="server" DataSource='<%# Eval("Tarjetas") %>'>
                            <ItemTemplate>
                                <article class="tarjeta" draggable="true" tabindex="0" data-tarjeta-id='<%# Eval("Id") %>'>
                                    <span class="tarjeta-titulo">
                                        <asp:PlaceHolder runat="server" Visible='<%# Eval("Caso") IsNot Nothing %>'>
                                            <span class="tarjeta-caso" title="Caso">#<%#: Eval("Caso") %></span>
                                        </asp:PlaceHolder>
                                        <%#: Eval("Titulo") %>
                                    </span>
                                    <span class="tarjeta-info">
                                        <%# HtmlFechaLimite(Eval("FechaLimite")) %>
                                        <asp:PlaceHolder runat="server" Visible='<%# CInt(Eval("SubtareasTotal")) > 0 %>'>
                                            <span class='<%# If(CInt(Eval("SubtareasHechas")) = CInt(Eval("SubtareasTotal")), "info info-subtareas completas", "info info-subtareas") %>'
                                                title="Subtareas completadas">&#9745; <%# Eval("SubtareasHechas") %>/<%# Eval("SubtareasTotal") %></span>
                                        </asp:PlaceHolder>
                                        <asp:PlaceHolder runat="server" Visible='<%# CBool(Eval("TieneCodigo")) %>'>
                                            <span class="info info-codigo" title="Tiene código">&lt;/&gt;</span>
                                        </asp:PlaceHolder>
                                        <span class="info info-modificada" title='<%#: TextoModificacion(Eval("FechaModificacion"), Eval("NombreModificador")) %>'>&#9998; <%#: Formato.FechaCorta(Eval("FechaModificacion")) %></span>
                                        <asp:PlaceHolder runat="server" Visible='<%# CInt(Eval("NumComentarios")) > 0 %>'>
                                            <span class="info" title="Comentarios">&#128172; <%# Eval("NumComentarios") %></span>
                                        </asp:PlaceHolder>
                                        <asp:PlaceHolder runat="server" Visible='<%# Eval("NombreAsignado") IsNot Nothing %>'>
                                            <span class="avatar avatar-pequeno" title='<%#: Eval("NombreAsignado") %>'><%#: Formato.Iniciales(Eval("NombreAsignado")) %></span>
                                        </asp:PlaceHolder>
                                    </span>
                                </article>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                    <asp:LinkButton ID="lnkAgregarTarjeta" runat="server" CssClass="nueva-tarjeta" CausesValidation="false"
                        CommandName="AgregarTarjeta" CommandArgument='<%# Eval("Id") %>'>+ Añadir una tarjeta</asp:LinkButton>
                </section>
            </ItemTemplate>
        </asp:Repeater>

        <asp:Panel ID="pnlNuevaLista" runat="server" CssClass="lista lista-nueva" DefaultButton="btnAgregarLista">
            <asp:TextBox ID="txtNuevaLista" runat="server" MaxLength="150" placeholder="+ Añadir otra lista" />
            <asp:Button ID="btnAgregarLista" runat="server" Text="Añadir lista" CssClass="boton" CausesValidation="false" />
        </asp:Panel>
    </div>

    <%-- Acciones lanzadas desde Scripts/tablero.js (arrastrar y abrir tarjeta) --%>
    <asp:HiddenField ID="hfTarjetaId" runat="server" />
    <asp:HiddenField ID="hfListaDestino" runat="server" />
    <asp:HiddenField ID="hfIndice" runat="server" />
    <asp:LinkButton ID="lnkMoverTarjeta" runat="server" CssClass="oculto" CausesValidation="false" />
    <asp:LinkButton ID="lnkAbrirTarjeta" runat="server" CssClass="oculto" CausesValidation="false" />

    <asp:Panel ID="pnlDetalle" runat="server" Visible="false" CssClass="modal-fondo" DefaultButton="btnGuardarTarjeta">
        <div class="modal" role="dialog" aria-modal="true" aria-labelledby="tituloDetalle">
            <div class="modal-cabecera">
                <h2 id="tituloDetalle"><asp:Literal ID="litTituloModal" runat="server" /></h2>
                <asp:LinkButton ID="btnCerrarDetalle" runat="server" CssClass="modal-cerrar" ToolTip="Cerrar (Esc)" CausesValidation="false">&times;</asp:LinkButton>
            </div>
            <p class="modal-subtitulo">En la lista <strong><asp:Literal ID="litDetalleLista" runat="server" Mode="Encode" /></strong>
                <asp:Literal ID="litFechasTarjeta" runat="server" Mode="Encode" /></p>

            <div class="campos-fila">
                <div class="campo campo-caso">
                    <asp:Label runat="server" AssociatedControlID="txtCaso" Text="Caso" />
                    <asp:TextBox ID="txtCaso" runat="server" MaxLength="20" inputmode="numeric" placeholder="Nº" />
                    <asp:RegularExpressionValidator ID="revCaso" runat="server" ControlToValidate="txtCaso" ValidationGroup="Detalle"
                        ValidationExpression="^\s*[0-9]{1,20}\s*$" ErrorMessage="Solo números." Display="Dynamic" CssClass="validacion" />
                </div>
                <div class="campo">
                    <asp:Label runat="server" AssociatedControlID="txtTituloTarjeta" Text="Título" />
                    <asp:TextBox ID="txtTituloTarjeta" runat="server" MaxLength="200" />
                    <asp:RequiredFieldValidator ID="rfvTituloTarjeta" runat="server" ControlToValidate="txtTituloTarjeta" ValidationGroup="Detalle"
                        ErrorMessage="El título es obligatorio." Display="Dynamic" CssClass="validacion" />
                </div>
            </div>

            <div class="campo">
                <asp:Label runat="server" AssociatedControlID="txtDescripcionTarjeta" Text="Descripción" />
                <asp:TextBox ID="txtDescripcionTarjeta" runat="server" TextMode="MultiLine" Rows="5" />
            </div>

            <div class="campos-fila">
                <div class="campo">
                    <asp:Label runat="server" AssociatedControlID="txtFechaLimite" Text="Fecha límite" />
                    <asp:TextBox ID="txtFechaLimite" runat="server" TextMode="Date" />
                </div>
                <div class="campo">
                    <asp:Label runat="server" AssociatedControlID="ddlAsignado" Text="Asignada a" />
                    <asp:DropDownList ID="ddlAsignado" runat="server" />
                </div>
            </div>

            <div class="seccion">
                <h3>Subtareas <span class="seccion-nota"><asp:Literal ID="litProgresoSubtareas" runat="server" /></span></h3>
                <asp:Repeater ID="rptSubtareas" runat="server">
                    <HeaderTemplate><ul class="subtareas"></HeaderTemplate>
                    <ItemTemplate>
                        <li class='<%# If(CBool(Eval("Completada")), "subtarea completada", "subtarea") %>'>
                            <asp:HiddenField ID="hfSubtareaId" runat="server" Value='<%# Eval("Id") %>' />
                            <asp:CheckBox ID="chkSubtarea" runat="server" Checked='<%# Eval("Completada") %>'
                                AutoPostBack="true" OnCheckedChanged="chkSubtarea_CheckedChanged" />
                            <asp:Label runat="server" AssociatedControlID="chkSubtarea" Text='<%#: Eval("Texto") %>' />
                            <asp:LinkButton ID="lnkEliminarSubtarea" runat="server" CssClass="subtarea-eliminar" ToolTip="Eliminar subtarea"
                                CommandName="Eliminar" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">&times;</asp:LinkButton>
                        </li>
                    </ItemTemplate>
                    <FooterTemplate></ul></FooterTemplate>
                </asp:Repeater>
                <asp:Panel ID="pnlNuevaSubtarea" runat="server" CssClass="form-linea nueva-subtarea" DefaultButton="btnAgregarSubtarea">
                    <asp:TextBox ID="txtNuevaSubtarea" runat="server" MaxLength="300" placeholder="Añadir una subtarea…" />
                    <asp:Button ID="btnAgregarSubtarea" runat="server" Text="Añadir" CssClass="boton boton-claro" CausesValidation="false" />
                </asp:Panel>
            </div>

            <div class="seccion">
                <h3><asp:Label runat="server" AssociatedControlID="txtCodigo" Text="Código" /></h3>
                <%-- Editor markdown: barra y vista previa en Scripts/markdown.js --%>
                <div class="editor-md" data-editor-md>
                    <div class="editor-md-barra">
                        <span class="editor-md-pestanas">
                            <button type="button" data-modo="escribir">Escribir</button>
                            <button type="button" data-modo="vista">Vista previa</button>
                        </span>
                        <span class="editor-md-herramientas">
                            <button type="button" data-md="titulo" title="Título">H</button>
                            <button type="button" data-md="negrita" title="Negrita (Ctrl+B)"><b>B</b></button>
                            <button type="button" data-md="cursiva" title="Cursiva (Ctrl+I)"><i>I</i></button>
                            <button type="button" data-md="tachado" title="Tachado"><s>S</s></button>
                            <button type="button" data-md="codigo" title="Código en línea">&lt;/&gt;</button>
                            <button type="button" data-md="bloque" title="Bloque de código">{ }</button>
                            <button type="button" data-md="lista" title="Lista">&#8226;</button>
                            <button type="button" data-md="numerada" title="Lista numerada">1.</button>
                            <button type="button" data-md="cita" title="Cita">&#8220;</button>
                            <button type="button" data-md="enlace" title="Enlace">&#128279;</button>
                        </span>
                    </div>
                    <asp:TextBox ID="txtCodigo" runat="server" TextMode="MultiLine" Rows="12" CssClass="editor-md-texto"
                        spellcheck="false" placeholder="Escribe en markdown. Usa ``` para bloques de código." />
                    <div class="editor-md-vista markdown" hidden="hidden"></div>
                </div>
            </div>

            <div class="modal-acciones">
                <asp:Button ID="btnGuardarTarjeta" runat="server" Text="Guardar" CssClass="boton" ValidationGroup="Detalle" />
                <asp:Button ID="btnEliminarTarjeta" runat="server" Text="Eliminar tarjeta" CssClass="boton boton-peligro" CausesValidation="false"
                    OnClientClick="return confirm('¿Eliminar esta tarjeta?');" />
            </div>

            <%-- Solo al editar una tarjeta existente --%>
            <asp:PlaceHolder ID="phComentarios" runat="server">
            <h3>Comentarios</h3>
            <div class="nuevo-comentario">
                <asp:TextBox ID="txtComentario" runat="server" TextMode="MultiLine" Rows="2" placeholder="Escribe un comentario…" />
                <asp:Button ID="btnComentar" runat="server" Text="Comentar" CssClass="boton boton-claro" CausesValidation="false" />
            </div>
            <asp:Repeater ID="rptComentarios" runat="server">
                <ItemTemplate>
                    <div class="comentario">
                        <div class="comentario-cabecera">
                            <span class="avatar avatar-pequeno"><%#: Formato.Iniciales(Eval("NombreUsuario")) %></span>
                            <strong><%#: Eval("NombreUsuario") %></strong>
                            <span class="fecha"><%#: Formato.FechaLocal(Eval("Fecha")) %></span>
                        </div>
                        <p><%#: Eval("Texto") %></p>
                    </div>
                </ItemTemplate>
            </asp:Repeater>

            <details class="historial" open="open">
                <summary><h3>Historial de cambios <span class="seccion-nota"><asp:Literal ID="litNumCambios" runat="server" /></span></h3></summary>
                <asp:Repeater ID="rptHistorial" runat="server">
                    <HeaderTemplate><ol class="historial-lista"></HeaderTemplate>
                    <ItemTemplate>
                        <li class="historial-item">
                            <span class="avatar avatar-pequeno"><%#: Formato.Iniciales(Eval("NombreUsuario")) %></span>
                            <span class="historial-texto">
                                <strong><%#: Eval("NombreUsuario") %></strong>
                                <%-- DescribirCambio devuelve HTML con todos los valores del usuario ya codificados --%>
                                <%# DescribirCambio(CType(Container.DataItem, TaskManager.Models.HistorialTarjeta)) %>
                                <span class="fecha"><%#: Formato.FechaLocal(Eval("Fecha")) %></span>
                            </span>
                        </li>
                    </ItemTemplate>
                    <FooterTemplate></ol></FooterTemplate>
                </asp:Repeater>
            </details>
            </asp:PlaceHolder>
        </div>
    </asp:Panel>
</asp:Content>

<asp:Content ID="cScripts" ContentPlaceHolderID="Scripts" runat="server">
    <script>
        window.TaskManagerTablero = {
            tarjeta: '<%= hfTarjetaId.ClientID %>',
            lista: '<%= hfListaDestino.ClientID %>',
            indice: '<%= hfIndice.ClientID %>',
            mover: '<%= lnkMoverTarjeta.UniqueID %>',
            abrir: '<%= lnkAbrirTarjeta.UniqueID %>',
            cerrar: '<%= btnCerrarDetalle.ClientID %>'
        };
    </script>
    <script src="<%= Formato.UrlConVersion("~/Scripts/markdown.js") %>"></script>
    <script src="<%= Formato.UrlConVersion("~/Scripts/tablero.js") %>"></script>
</asp:Content>
