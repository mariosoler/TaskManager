<%@ Page ValidateRequestMode="Disabled" Title="Iniciar sesión" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Login.aspx.vb" Inherits="TaskManager.LoginPagina" %>

<asp:Content ID="cContenido" ContentPlaceHolderID="Contenido" runat="server">
    <asp:Panel ID="pnlLogin" runat="server" CssClass="tarjeta-acceso" DefaultButton="btnEntrar">
        <h1>Iniciar sesión</h1>
        <p class="subtitulo">Organiza tus tareas en tableros, listas y tarjetas.</p>

        <asp:Label ID="lblError" runat="server" CssClass="mensaje mensaje-error" Visible="false" EnableViewState="false" />

        <div class="campo">
            <asp:Label runat="server" AssociatedControlID="txtEmail" Text="Email" />
            <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="200" autocomplete="username" />
            <asp:RequiredFieldValidator ID="rfvEmail" runat="server" ControlToValidate="txtEmail"
                ErrorMessage="Escribe tu email." Display="Dynamic" CssClass="validacion" />
        </div>

        <div class="campo">
            <asp:Label runat="server" AssociatedControlID="txtPassword" Text="Contraseña" />
            <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" MaxLength="128" autocomplete="current-password" />
            <asp:RequiredFieldValidator ID="rfvPassword" runat="server" ControlToValidate="txtPassword"
                ErrorMessage="Escribe tu contraseña." Display="Dynamic" CssClass="validacion" />
        </div>

        <div class="campo campo-check">
            <asp:CheckBox ID="chkRecordar" runat="server" Text="Mantener la sesión iniciada" />
        </div>

        <asp:Button ID="btnEntrar" runat="server" Text="Entrar" CssClass="boton boton-ancho" />

        <p class="pie-acceso">¿No tienes cuenta? <a href="Registro.aspx">Regístrate</a></p>
    </asp:Panel>
</asp:Content>
