<%@ Assembly Name="$SharePoint.Project.AssemblyFullName$" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="RSWorkflowMyReportsUserControl.ascx.cs" Inherits="ESMA.Paperless.Reports.v16.RSWorkflowMyReports.RSWorkflowMyReportsUserControl" %>
<script type="text/javascript">
    $(document).ready(function () {
        // Set View HTML links to open in new window
        $('a:contains("View Report")').filter(function () {
            return $(this).text() == "View Report";
        }).attr('target', '_blank');
    });
</script>
<h1>My Reports</h1>
<asp:Panel ID="informationMessagePanel" runat="server" Visible="false" CssClass="information_message">
    <asp:Literal ID="informationMesage" runat="server"></asp:Literal>
</asp:Panel>
<asp:Panel ID="MyReportsPanel" runat="server"></asp:Panel>