<%@ Page ValidateRequestMode="Disabled" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Registro.aspx.vb" Inherits="TaskManager.RegistroPagina" %>

<asp:Content ID="cContenido" ContentPlaceHolderID="Contenido" runat="server">
    <asp:Panel ID="pnlRegistro" runat="server" CssClass="tarjeta-acceso" DefaultButton="btnRegistrar">
        <div class="acceso-marca">
            <img src="Content/logo.svg" alt="" class="acceso-logo" />
            <span class="logo-texto">Task<span>Manager</span></span>
        </div>
        <h1>Crear cuenta</h1>

        <asp:Label ID="lblError" runat="server" CssClass="mensaje mensaje-error" Visible="false" EnableViewState="false" />

        <div class="campo">
            <asp:Label runat="server" AssociatedControlID="txtNombre" Text="Nombre" />
            <asp:TextBox ID="txtNombre" runat="server" MaxLength="100" autocomplete="name" />
            <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre"
                ErrorMessage="Escribe tu nombre." Display="Dynamic" CssClass="validacion" />
        </div>

        <div class="campo">
            <asp:Label runat="server" AssociatedControlID="txtEmail" Text="Email" />
            <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="200" autocomplete="email" />
            <asp:RequiredFieldValidator ID="rfvEmail" runat="server" ControlToValidate="txtEmail"
                ErrorMessage="Escribe tu email." Display="Dynamic" CssClass="validacion" />
            <asp:RegularExpressionValidator ID="revEmail" runat="server" ControlToValidate="txtEmail"
                ValidationExpression="^\s*[^@\s]+@[^@\s]+\.[^@\s]+\s*$"
                ErrorMessage="El email no es válido." Display="Dynamic" CssClass="validacion" />
        </div>

        <div class="campo">
            <asp:Label runat="server" AssociatedControlID="txtPassword" Text="Contraseña (mínimo 8 caracteres)" />
            <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" MaxLength="128" autocomplete="new-password" />
            <asp:RequiredFieldValidator ID="rfvPassword" runat="server" ControlToValidate="txtPassword"
                ErrorMessage="Escribe una contraseña." Display="Dynamic" CssClass="validacion" />
            <asp:RegularExpressionValidator ID="revPassword" runat="server" ControlToValidate="txtPassword"
                ValidationExpression="^[\s\S]{8,128}$"
                ErrorMessage="La contraseña debe tener entre 8 y 128 caracteres." Display="Dynamic" CssClass="validacion" />
        </div>

        <div class="campo">
            <asp:Label runat="server" AssociatedControlID="txtConfirmar" Text="Repite la contraseña" />
            <asp:TextBox ID="txtConfirmar" runat="server" TextMode="Password" MaxLength="128" autocomplete="new-password" />
            <asp:CompareValidator ID="cvConfirmar" runat="server" ControlToValidate="txtConfirmar" ControlToCompare="txtPassword"
                ErrorMessage="Las contraseñas no coinciden." Display="Dynamic" CssClass="validacion" />
        </div>

        <asp:Button ID="btnRegistrar" runat="server" Text="Crear cuenta" CssClass="boton boton-ancho" />

        <p class="pie-acceso">¿Ya tienes cuenta? <a href="Login.aspx">Inicia sesión</a></p>
    </asp:Panel>
</asp:Content>
