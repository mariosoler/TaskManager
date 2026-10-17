<%@ Page ValidateRequestMode="Disabled" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Perfil.aspx.vb" Inherits="TaskManager.PerfilPagina" %>

<asp:Content ID="cContenido" ContentPlaceHolderID="Contenido" runat="server">
    <div class="pagina pagina-estrecha">
        <h1>Mi perfil</h1>

        <asp:Label ID="lblMensaje" runat="server" CssClass="mensaje" Visible="false" EnableViewState="false" />

        <asp:Panel ID="pnlPerfil" runat="server" CssClass="perfil" DefaultButton="btnGuardar">
            <div class="campo">
                <label>Email</label>
                <p class="perfil-dato"><asp:Literal ID="litEmail" runat="server" Mode="Encode" /></p>
            </div>

            <div class="campo">
                <asp:Label runat="server" AssociatedControlID="txtNombre" Text="Nombre" />
                <asp:TextBox ID="txtNombre" runat="server" MaxLength="100" autocomplete="name" />
                <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre"
                    ErrorMessage="Escribe tu nombre." Display="Dynamic" CssClass="validacion" />
                <span class="campo-ayuda">Es el nombre que ven los demás en los tableros, el chat y el historial.</span>
            </div>

            <div class="campo">
                <asp:Label runat="server" AssociatedControlID="ddlFormatoFecha" Text="Formato de las fechas" />
                <asp:DropDownList ID="ddlFormatoFecha" runat="server" />
                <span class="campo-ayuda">Se usa en toda la aplicación: tarjetas, historial, chat, buscador y estadísticas.
                    Entre paréntesis, cómo se ve en las tarjetas.</span>
            </div>

            <div class="perfil-acciones">
                <asp:Button ID="btnGuardar" runat="server" Text="Guardar cambios" CssClass="boton" />
            </div>
        </asp:Panel>
    </div>
</asp:Content>
