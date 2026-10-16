<%@ Page ValidateRequestMode="Disabled" Title="Mis tableros" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Tableros.aspx.vb" Inherits="TaskManager.TablerosPagina" %>

<asp:Content ID="cContenido" ContentPlaceHolderID="Contenido" runat="server">
    <div class="pagina">
        <h1>Mis tableros</h1>

        <asp:Label ID="lblMensaje" runat="server" CssClass="mensaje mensaje-error" Visible="false" EnableViewState="false" />

        <div class="rejilla-tableros">
            <asp:Repeater ID="rptTableros" runat="server">
                <ItemTemplate>
                    <div class="tablero-ficha">
                        <a class="tablero-ficha-enlace" href='<%# "Tablero.aspx?id=" & Eval("Id").ToString() %>'>
                            <h2><%#: Eval("Nombre") %></h2>
                            <p><%#: Eval("Descripcion") %></p>
                            <span class="etiqueta-rol"><%#: Eval("Rol") %></span>
                        </a>
                        <asp:LinkButton ID="lnkEliminar" runat="server" CssClass="tablero-ficha-eliminar"
                            CommandName="Eliminar" CommandArgument='<%# Eval("Id") %>' CausesValidation="false"
                            Visible='<%# CStr(Eval("Rol")) = TaskManager.Models.Tablero.RolPropietario %>'
                            ToolTip="Eliminar tablero"
                            OnClientClick="return confirm('¿Eliminar este tablero con todas sus listas y tarjetas?');">&times;</asp:LinkButton>
                    </div>
                </ItemTemplate>
            </asp:Repeater>

            <asp:Panel ID="pnlNuevo" runat="server" CssClass="tablero-ficha tablero-nuevo" DefaultButton="btnCrear">
                <h2>Crear tablero</h2>
                <asp:TextBox ID="txtNombre" runat="server" MaxLength="150" placeholder="Nombre del tablero" />
                <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre" ValidationGroup="Nuevo"
                    ErrorMessage="Escribe un nombre." Display="Dynamic" CssClass="validacion" />
                <asp:TextBox ID="txtDescripcion" runat="server" MaxLength="500" TextMode="MultiLine" Rows="2" placeholder="Descripción (opcional)" />
                <asp:Button ID="btnCrear" runat="server" Text="Crear" CssClass="boton" ValidationGroup="Nuevo" />
            </asp:Panel>
        </div>
    </div>
</asp:Content>
