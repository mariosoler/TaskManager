<%@ Page ValidateRequestMode="Disabled" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Tablero.aspx.vb" Inherits="TaskManager.TableroPagina" %>
<%@ Import Namespace="TaskManager.Utilidades" %>

<asp:Content ID="cContenido" ContentPlaceHolderID="Contenido" runat="server">
  <asp:Panel ID="pnlTablero" runat="server" CssClass="tablero-pagina">
    <div class="tablero-cabecera">
        <div>
            <a href="Tableros.aspx" class="volver">&larr; Mis tableros</a>
            <%-- Título con Editar, Fondo y Listas ocultas al lado (Buscar y Estadísticas están en el menú superior) --%>
            <div class="tablero-titulo-fila">
            <h1 id="h1Titulo" runat="server"><span id="spnTitulo" runat="server" class="titulo-texto"><asp:Literal ID="litNombre" runat="server" Mode="Encode" /></span></h1>
            <%-- Edición del nombre al pulsar el título (propietario o Admin), en Scripts/tablero.js --%>
            <asp:Panel ID="pnlTituloEdicion" runat="server" CssClass="titulo-edicion oculto" DefaultButton="btnGuardarTitulo">
                <asp:TextBox ID="txtTitulo" runat="server" MaxLength="150" CssClass="titulo-input" aria-label="Nombre del tablero" />
                <asp:Button ID="btnGuardarTitulo" runat="server" Text="Guardar" CssClass="oculto" CausesValidation="false" />
            </asp:Panel>
            <div class="tablero-enlaces">
                <%-- Listas que este usuario ha ocultado (solo para él): pulsar una la vuelve a mostrar --%>
                <asp:PlaceHolder ID="phListasOcultas" runat="server" Visible="false">
                    <details class="tablero-fondo">
                        <summary>&#128065; Listas ocultas (<asp:Literal ID="litListasOcultas" runat="server" />)</summary>
                        <div class="tablero-fondo-panel listas-ocultas-panel">
                            <span class="fondo-seccion">Ocultas solo para ti. Pulsa una para mostrarla.</span>
                            <asp:Repeater ID="rptListasOcultas" runat="server">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lnkMostrarLista" runat="server" CssClass="lista-oculta" CausesValidation="false"
                                        CommandName="MostrarLista" CommandArgument='<%# Eval("Id") %>' ToolTip="Mostrar la lista">
                                        <span class="lista-oculta-titulo"><%#: Eval("Titulo") %></span>
                                        <span class="lista-contador"><%# Eval("Tarjetas.Count") %></span>
                                    </asp:LinkButton>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </details>
                </asp:PlaceHolder>
                <asp:PlaceHolder ID="phEditarTablero" runat="server">
                    <details class="tablero-fondo">
                        <summary>&#9998; Editar</summary>
                        <asp:Panel ID="pnlEditarTablero" runat="server" CssClass="tablero-fondo-panel" DefaultButton="btnGuardarNombre">
                            <asp:Label runat="server" AssociatedControlID="txtNombreTablero" Text="Nombre" />
                            <asp:TextBox ID="txtNombreTablero" runat="server" MaxLength="150" />
                            <asp:RequiredFieldValidator ID="rfvNombreTablero" runat="server" ControlToValidate="txtNombreTablero"
                                ValidationGroup="EditarTablero" ErrorMessage="Escribe un nombre." Display="Dynamic" CssClass="validacion" />
                            <asp:Label runat="server" AssociatedControlID="txtDescripcionTablero" Text="Descripción" />
                            <asp:TextBox ID="txtDescripcionTablero" runat="server" MaxLength="500" TextMode="MultiLine" Rows="2" />
                            <div class="form-linea">
                                <asp:Button ID="btnGuardarNombre" runat="server" Text="Guardar" CssClass="boton" ValidationGroup="EditarTablero" />
                            </div>
                        </asp:Panel>
                    </details>
                </asp:PlaceHolder>
                <asp:PlaceHolder ID="phFondo" runat="server">
                    <details class="tablero-fondo">
                        <summary>&#128444; Fondo</summary>
                        <div class="tablero-fondo-panel tablero-fondo-panel-ancho">
                            <span class="fondo-seccion">Colores y fotos</span>
                            <div class="fondos-predef">
                                <asp:Repeater ID="rptFondos" runat="server">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lnkFondo" runat="server" CommandName="Elegir" CommandArgument='<%# Eval("Clave") %>'
                                            CausesValidation="false" ToolTip='<%# Eval("Nombre") %>'><span class="solo-lectores"><%#: Eval("Nombre") %></span></asp:LinkButton>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                            <span class="fondo-seccion">O sube tu imagen</span>
                            <asp:FileUpload ID="fuFondo" runat="server" accept="image/jpeg,image/png,image/gif,image/webp" />
                            <small>JPG, PNG, GIF o WebP. Máximo 5 MB.</small>
                            <div class="form-linea">
                                <asp:Button ID="btnSubirFondo" runat="server" Text="Aplicar" CssClass="boton" CausesValidation="false" />
                                <asp:LinkButton ID="lnkQuitarFondo" runat="server" CssClass="boton boton-claro" CausesValidation="false">Quitar fondo</asp:LinkButton>
                            </div>
                        </div>
                    </details>
                </asp:PlaceHolder>
            </div>
            </div>
            <p class="tablero-descripcion"><asp:Literal ID="litDescripcion" runat="server" Mode="Encode" /></p>
        </div>

        <%-- Búsqueda rápida: filtra las tarjetas mientras se escribe, sin recargar (Scripts/busqueda-rapida.js) --%>
        <div class="busqueda-rapida" role="search">
            <span class="busqueda-rapida-icono" aria-hidden="true">&#128269;</span>
            <input type="search" id="busquedaRapida" class="busqueda-rapida-campo" autocomplete="off" spellcheck="false"
                placeholder="Buscar en el tablero…  (#104512 o #ETIQUETA)" aria-label="Buscar tarjetas en el tablero"
                aria-describedby="busquedaRapidaResultado" />
            <span id="busquedaRapidaResultado" class="busqueda-rapida-resultado" aria-live="polite"></span>
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
            <%-- Añadir miembro: sugerencias de usuarios mientras se escribe (Scripts/miembros.js + Usuarios.ashx) --%>
            <asp:Panel ID="pnlAgregarMiembro" runat="server" CssClass="agregar-miembro" DefaultButton="btnAgregarMiembro">
                <span class="agregar-miembro-icono" aria-hidden="true">&#128100;</span>
                <asp:TextBox ID="txtEmailMiembro" runat="server" MaxLength="200" CssClass="agregar-miembro-campo"
                    placeholder="Añadir miembro: nombre o email" aria-label="Añadir miembro: nombre o email"
                    autocomplete="off" spellcheck="false" role="combobox" aria-autocomplete="list"
                    aria-expanded="false" aria-controls="miembroSugerencias" />
                <asp:Button ID="btnAgregarMiembro" runat="server" Text="Añadir" CssClass="boton agregar-miembro-boton" CausesValidation="false" />
                <ul id="miembroSugerencias" class="miembro-sugerencias" role="listbox" aria-label="Usuarios sugeridos" hidden="hidden"></ul>
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
                        <span class="lista-contador" data-total='<%# Eval("Tarjetas.Count") %>'><%# Eval("Tarjetas.Count") %></span>
                        <asp:PlaceHolder runat="server" Visible='<%# Not CType(Eval("Orden"), TaskManager.Models.OrdenLista).EsManual %>'>
                            <span class="lista-orden" title='<%#: "Ordenada para ti por: " & CType(Eval("Orden"), TaskManager.Models.OrdenLista).Texto %>'><%#: CType(Eval("Orden"), TaskManager.Models.OrdenLista).Corto %></span>
                        </asp:PlaceHolder>
                        <%-- Menú de la lista: ordenar las tarjetas y ocultarla (preferencias solo de este usuario) --%>
                        <details class="lista-menu">
                            <summary title="Opciones de la lista" aria-label="Opciones de la lista">&#8943;</summary>
                            <div class="lista-menu-panel">
                                <span class="lista-menu-seccion">Ordenar tarjetas (solo para ti)</span>
                                <asp:Repeater ID="rptOrdenes" runat="server" DataSource='<%# TaskManager.Models.OrdenLista.Opciones %>' OnItemCommand="rptOrdenes_ItemCommand">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lnkOrden" runat="server" CausesValidation="false" CommandName="Ordenar"
                                            CommandArgument='<%# ArgumentoOrden(Container) %>'
                                            CssClass='<%# If(EsOrdenActual(Container), "lista-menu-opcion activa", "lista-menu-opcion") %>'><%#: Eval("Texto") %></asp:LinkButton>
                                    </ItemTemplate>
                                </asp:Repeater>
                                <span class="lista-menu-separador"></span>
                                <asp:LinkButton ID="lnkOcultarLista" runat="server" CssClass="lista-menu-opcion" CausesValidation="false"
                                    CommandName="OcultarLista" CommandArgument='<%# Eval("Id") %>'>&#128065; Ocultar lista (solo para ti)</asp:LinkButton>
                            </div>
                        </details>
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

                    <%-- data-orden: la lista está ordenada por fecha (Scripts/tablero.js no reordena dentro de ella) --%>
                    <div class="tarjetas" data-lista-id='<%# Eval("Id") %>' data-orden='<%#: CType(Eval("Orden"), TaskManager.Models.OrdenLista).Clave %>'>
                        <asp:Repeater ID="rptTarjetas" runat="server" DataSource='<%# Eval("Tarjetas") %>'>
                            <ItemTemplate>
                                <article class='<%# "tarjeta " & TaskManager.Models.Prioridad.ClaseCss(CType(Eval("Prioridad"), Integer?)) %>' draggable="true" tabindex="0" data-tarjeta-id='<%# Eval("Id") %>'
                                    data-caso='<%#: Eval("Caso") %>' data-etiquetas='<%#: NombresEtiquetas(Container.DataItem) %>' data-texto='<%#: TextoBusqueda(Container.DataItem) %>'>
                                    <span class="tarjeta-titulo">
                                        <span class="tarjeta-id" title="ID de la tarea"><%# Eval("Id") %></span>
                                        <asp:PlaceHolder runat="server" Visible='<%# Eval("Caso") IsNot Nothing %>'>
                                            <span class="tarjeta-caso" title="Caso">#<%#: Eval("Caso") %></span>
                                        </asp:PlaceHolder>
                                        <%#: Eval("Titulo") %>
                                    </span>
                                    <asp:Repeater ID="rptEtiquetasTarjeta" runat="server" DataSource='<%# Eval("Etiquetas") %>'>
                                        <HeaderTemplate><span class="tarjeta-etiquetas"></HeaderTemplate>
                                        <ItemTemplate><span class="etiqueta-tag" style='<%#: Eval("EstiloCss") %>'><%#: Eval("Nombre") %></span></ItemTemplate>
                                        <FooterTemplate></span></FooterTemplate>
                                    </asp:Repeater>
                                    <span class="tarjeta-info">
                                        <%# Formato.HtmlPrioridad(Eval("Prioridad")) %>
                                        <%# Formato.HtmlFechaLimite(Eval("FechaLimite")) %>
                                        <asp:PlaceHolder runat="server" Visible='<%# CInt(Eval("SubtareasTotal")) > 0 %>'>
                                            <span class='<%# If(CInt(Eval("SubtareasHechas")) = CInt(Eval("SubtareasTotal")), "info info-subtareas completas", "info info-subtareas") %>'
                                                title="Subtareas completadas">&#9745; <%# Eval("SubtareasHechas") %>/<%# Eval("SubtareasTotal") %></span>
                                        </asp:PlaceHolder>
                                        <asp:PlaceHolder runat="server" Visible='<%# CBool(Eval("TieneCodigo")) %>'>
                                            <span class="info info-codigo" title="Tiene código">&lt;/&gt;</span>
                                        </asp:PlaceHolder>
                                        <span class="info info-modificada" title='<%#: TextoModificacion(Eval("FechaModificacion"), Eval("NombreModificador")) %>'>&#9998; <%#: Formato.FechaCorta(Eval("FechaModificacion")) %></span>
                                        <asp:PlaceHolder runat="server" Visible='<%# CInt(Eval("NumComentarios")) > 0 %>'>
                                            <span class="info" title="Mensajes de chat">&#128172; <%# Eval("NumComentarios") %></span>
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

  </asp:Panel>

    <asp:Panel ID="pnlDetalle" runat="server" Visible="false" CssClass="modal-fondo" DefaultButton="btnGuardarTarjeta">
        <div class="modal modal-con-chat" role="dialog" aria-modal="true" aria-label="Detalle de la tarjeta">
          <div class="modal-principal">
            <%-- Título: parece texto y se edita al pulsarlo (cuadro de texto con aspecto de título) --%>
            <div class="modal-cabecera">
                <div class="campo campo-titulo-tarjeta">
                    <asp:Label runat="server" AssociatedControlID="txtTituloTarjeta" Text="Título" CssClass="solo-lectores" />
                    <asp:TextBox ID="txtTituloTarjeta" runat="server" MaxLength="200" CssClass="titulo-tarjeta"
                        placeholder="Título de la tarjeta" ToolTip="Pulsa para editar el título" />
                    <asp:RequiredFieldValidator ID="rfvTituloTarjeta" runat="server" ControlToValidate="txtTituloTarjeta" ValidationGroup="Detalle"
                        ErrorMessage="El título es obligatorio." Display="Dynamic" CssClass="validacion" />
                </div>
                <asp:LinkButton ID="btnCerrarDetalle" runat="server" CssClass="modal-cerrar" ToolTip="Cerrar (Esc)" CausesValidation="false">&times;</asp:LinkButton>
            </div>
            <p class="modal-subtitulo"><asp:Literal ID="litDetallePrefijo" runat="server" /> <strong><asp:Literal ID="litDetalleLista" runat="server" Mode="Encode" /></strong>
                <asp:Literal ID="litFechasTarjeta" runat="server" Mode="Encode" /></p>

            <%-- Avisos (Scripts/detalle-tarjeta.js): fecha límite superada y sin asignar, con icono y texto --%>
            <div class="campos-fila">
                <div class="campo campo-caso">
                    <asp:Label runat="server" AssociatedControlID="txtCaso" Text="Caso" />
                    <asp:TextBox ID="txtCaso" runat="server" MaxLength="20" inputmode="numeric" placeholder="Nº" />
                    <asp:RegularExpressionValidator ID="revCaso" runat="server" ControlToValidate="txtCaso" ValidationGroup="Detalle"
                        ValidationExpression="^\s*[0-9]{1,20}\s*$" ErrorMessage="Solo números." Display="Dynamic" CssClass="validacion" />
                </div>
                <div class="campo">
                    <asp:Label runat="server" AssociatedControlID="ddlPrioridad" Text="Prioridad" />
                    <asp:DropDownList ID="ddlPrioridad" runat="server">
                        <asp:ListItem Value="" Text="Sin prioridad" />
                        <asp:ListItem Value="1" Text="Alta" />
                        <asp:ListItem Value="2" Text="Media" />
                        <asp:ListItem Value="3" Text="Baja" />
                    </asp:DropDownList>
                </div>
                <div class="campo campo-fecha-limite">
                    <asp:Label runat="server" AssociatedControlID="txtFechaLimite">Fecha límite <span class="campo-alerta">&#9888; Vencida</span></asp:Label>
                    <asp:TextBox ID="txtFechaLimite" runat="server" TextMode="Date" />
                </div>
                <div class="campo campo-asignado">
                    <asp:Label runat="server" AssociatedControlID="ddlAsignado">Asignada a <span class="campo-alerta">&#9888; Sin asignar</span></asp:Label>
                    <asp:DropDownList ID="ddlAsignado" runat="server" />
                </div>
            </div>

            <div class="campo">
                <asp:Label runat="server" AssociatedControlID="txtDescripcionTarjeta" Text="Descripción" />
                <asp:TextBox ID="txtDescripcionTarjeta" runat="server" TextMode="MultiLine" Rows="5" />
            </div>

            <%-- Etiquetas: Scripts/etiquetas.js las pinta dentro del cuadro y pide el color de las nuevas --%>
            <div class="campo campo-etiquetas">
                <asp:Label runat="server" AssociatedControlID="txtEtiquetas" Text="Etiquetas" />
                <asp:TextBox ID="txtEtiquetas" runat="server" MaxLength="600" autocomplete="off" spellcheck="false"
                    placeholder="#PLANIFICADOR_CABINAS #urgente" aria-describedby="ayudaEtiquetas" />
                <small id="ayudaEtiquetas" class="campo-ayuda">Escribe para ver las etiquetas del tablero; espacio o Enter para añadirla. Si es nueva, eliges su color. Letras, números, _ y -. Máximo 10.</small>
            </div>

            <div class="seccion">
                <h3>Subtareas <span class="seccion-nota" id="progresoSubtareas"><asp:Literal ID="litProgresoSubtareas" runat="server" /></span></h3>
                <asp:Repeater ID="rptSubtareas" runat="server">
                    <HeaderTemplate><ul class="subtareas"></HeaderTemplate>
                    <ItemTemplate>
                        <%-- Scripts/detalle-tarjeta.js marca/desmarca sin recargar (data-subtarea-id); sin JS, AutoPostBack --%>
                        <li class='<%# If(CBool(Eval("Completada")), "subtarea completada", "subtarea") %>' data-subtarea-id='<%# Eval("Id") %>'>
                            <asp:HiddenField ID="hfSubtareaId" runat="server" Value='<%# Eval("Id") %>' />
                            <asp:CheckBox ID="chkSubtarea" runat="server" Checked='<%# Eval("Completada") %>'
                                AutoPostBack="true" OnCheckedChanged="chkSubtarea_CheckedChanged" />
                            <%-- HtmlSubtarea codifica el texto (y pone el enlace si la subtarea enlaza a otra tarjeta) --%>
                            <asp:Label runat="server" AssociatedControlID="chkSubtarea" Text='<%# HtmlSubtarea(Container.DataItem) %>' />
                            <asp:LinkButton ID="lnkEliminarSubtarea" runat="server" CssClass="subtarea-eliminar" ToolTip="Eliminar subtarea"
                                CommandName="Eliminar" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">&times;</asp:LinkButton>
                        </li>
                    </ItemTemplate>
                    <FooterTemplate></ul></FooterTemplate>
                </asp:Repeater>
                <asp:Panel ID="pnlNuevaSubtarea" runat="server" CssClass="form-linea nueva-subtarea" DefaultButton="btnAgregarSubtarea">
                    <asp:TextBox ID="txtNuevaSubtarea" runat="server" MaxLength="300" placeholder="Añadir una subtarea…" />
                    <%-- Id de la tarjeta a enlazar (lo rellena Scripts/subtareas-enlace.js; el servidor lo valida) --%>
                    <asp:HiddenField ID="hfSubtareaEnlace" runat="server" />
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
                <%-- Al editar, Scripts/detalle-tarjeta.js oculta Guardar y muestra aquí el estado del guardado automático --%>
                <span id="estadoGuardado" class="guardado-estado" aria-live="polite"></span>
                <asp:Button ID="btnGuardarTarjeta" runat="server" Text="Guardar" CssClass="boton" ValidationGroup="Detalle" />
                <asp:Button ID="btnEliminarTarjeta" runat="server" Text="Eliminar tarea" CssClass="boton boton-peligro" CausesValidation="false"
                    OnClientClick="return confirm('¿Eliminar esta tarjeta?');" />
            </div>

            <%-- Solo al editar una tarjeta existente --%>
            <asp:PlaceHolder ID="phHistorial" runat="server">
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

          <%-- Chat de la tarjeta: envío y actualización sin recargar en Scripts/chat.js (Chat.ashx). --%>
          <aside class="chat" aria-label="Chat de la tarjeta">
            <div class="chat-cabecera">
                <h3>Chat</h3>
                <span class="seccion-nota" id="chatContador"></span>
            </div>
            <asp:PlaceHolder ID="phChat" runat="server">
                <div class="chat-mensajes" id="chatMensajes" aria-live="polite">
                    <p class="chat-vacio">Todavía no hay mensajes. ¡Escribe el primero!</p>
                    <asp:Repeater ID="rptComentarios" runat="server">
                        <ItemTemplate>
                            <div class='<%# ClaseMensaje(Eval("UsuarioId")) %>' data-id='<%# Eval("Id") %>'>
                                <span class="avatar avatar-pequeno" title='<%#: Eval("NombreUsuario") %>'><%#: Formato.Iniciales(Eval("NombreUsuario")) %></span>
                                <div class="chat-burbuja">
                                    <div class="chat-meta"><strong><%#: Eval("NombreUsuario") %></strong> <span class="fecha"><%#: Formato.FechaLocal(Eval("Fecha")) %></span></div>
                                    <p><%#: Eval("Texto") %></p>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
                <p class="chat-aviso" id="chatAviso" hidden="hidden"></p>
                <div class="chat-escribir">
                    <asp:TextBox ID="txtComentario" runat="server" TextMode="MultiLine" Rows="2" MaxLength="2000" placeholder="Escribe un mensaje…" aria-label="Mensaje" />
                    <asp:Button ID="btnComentar" runat="server" Text="Enviar" CssClass="boton" CausesValidation="false" />
                </div>
                <p class="chat-ayuda">Enter para enviar · Mayús+Enter para saltar de línea</p>
            </asp:PlaceHolder>
            <asp:PlaceHolder ID="phChatNuevo" runat="server" Visible="false">
                <p class="chat-vacio">El chat estará disponible cuando crees la tarjeta.</p>
            </asp:PlaceHolder>
          </aside>
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
            cerrar: '<%= btnCerrarDetalle.ClientID %>',
            titulo: '<%= h1Titulo.ClientID %>',
            tituloEdicion: '<%= pnlTituloEdicion.ClientID %>',
            tituloTexto: '<%= txtTitulo.ClientID %>',
            tituloGuardar: '<%= btnGuardarTitulo.ClientID %>'
        };
    </script>
    <%-- Etiquetas para las sugerencias (solo con el detalle abierto); "<", ">" y "&" van escapados --%>
    <script type="application/json" id="etiquetas-datos"><%= EtiquetasJson %></script>
    <%-- Colores para las etiquetas nuevas: valores fijos del código (Etiqueta.Paleta) --%>
    <script type="application/json" id="etiquetas-paleta"><%= PaletaJson %></script>
    <script>
        window.TaskManagerEtiquetas = {
            campo: '<%= txtEtiquetas.ClientID %>',
            datos: 'etiquetas-datos',
            paleta: 'etiquetas-paleta',
            url: '<%= ResolveUrl("~/Tarjeta.ashx") %>',
            token: '<%= TokenChat %>',
            tablero: <%= TableroIdTexto %>,
            tarjeta: <%= TarjetaEdicionTexto %>
        };
    </script>
    <script src="<%= Formato.UrlConVersion("~/Scripts/etiquetas.js") %>"></script>
    <script>window.TaskManagerBusquedaRapida = { campo: 'busquedaRapida', resultado: 'busquedaRapidaResultado', tablero: <%= TableroIdTexto %> };</script>
    <script src="<%= Formato.UrlConVersion("~/Scripts/busqueda-rapida.js") %>"></script>
    <asp:PlaceHolder ID="phScriptMiembros" runat="server">
    <script>
        window.TaskManagerMiembros = {
            url: '<%= ResolveUrl("~/Usuarios.ashx") %>',
            tablero: <%= TableroIdTexto %>,
            campo: '<%= txtEmailMiembro.ClientID %>',
            boton: '<%= btnAgregarMiembro.ClientID %>',
            lista: 'miembroSugerencias'
        };
    </script>
    <script src="<%= Formato.UrlConVersion("~/Scripts/miembros.js") %>"></script>
    </asp:PlaceHolder>
    <script src="<%= Formato.UrlConVersion("~/Scripts/markdown.js") %>"></script>
    <script src="<%= Formato.UrlConVersion("~/Scripts/tablero.js") %>"></script>
    <asp:PlaceHolder ID="phScriptDetalle" runat="server" Visible="false">
    <script>
        window.TaskManagerDetalle = {
            url: '<%= ResolveUrl("~/Tarjeta.ashx") %>',
            token: '<%= TokenChat %>',
            tablero: <%= TableroIdTexto %>,
            tarjeta: <%= TarjetaEdicionTexto %>,
            modal: '<%= pnlDetalle.ClientID %>',
            cerrar: '<%= btnCerrarDetalle.ClientID %>',
            cerrarUnico: '<%= btnCerrarDetalle.UniqueID %>',
            guardar: '<%= btnGuardarTarjeta.ClientID %>',
            estado: 'estadoGuardado',
            campos: {
                titulo: '<%= txtTituloTarjeta.ClientID %>',
                caso: '<%= txtCaso.ClientID %>',
                descripcion: '<%= txtDescripcionTarjeta.ClientID %>',
                prioridad: '<%= ddlPrioridad.ClientID %>',
                fechaLimite: '<%= txtFechaLimite.ClientID %>',
                asignado: '<%= ddlAsignado.ClientID %>',
                etiquetas: '<%= txtEtiquetas.ClientID %>',
                codigo: '<%= txtCodigo.ClientID %>'
            }
        };
    </script>
    <script src="<%= Formato.UrlConVersion("~/Scripts/detalle-tarjeta.js") %>"></script>
    <%-- Tarjetas que se pueden enlazar desde una subtarea; "<", ">" y "&" van escapados --%>
    <script type="application/json" id="tarjetas-enlazables"><%= TarjetasEnlazablesJson %></script>
    <script>
        window.TaskManagerEnlaceSubtarea = {
            campo: '<%= txtNuevaSubtarea.ClientID %>',
            boton: '<%= btnAgregarSubtarea.ClientID %>',
            enlace: '<%= hfSubtareaEnlace.ClientID %>',
            datos: 'tarjetas-enlazables'
        };
    </script>
    <script src="<%= Formato.UrlConVersion("~/Scripts/subtareas-enlace.js") %>"></script>
    </asp:PlaceHolder>
    <script>
        window.TaskManagerChat = {
            url: '<%= ResolveUrl("~/Chat.ashx") %>',
            token: '<%= TokenChat %>',
            tablero: <%= TableroIdTexto %>,
            tarjeta: <%= TarjetaChatTexto %>,
            texto: '<%= txtComentario.ClientID %>',
            boton: '<%= btnComentar.ClientID %>'
        };
    </script>
    <script src="<%= Formato.UrlConVersion("~/Scripts/chat.js") %>"></script>
</asp:Content>
