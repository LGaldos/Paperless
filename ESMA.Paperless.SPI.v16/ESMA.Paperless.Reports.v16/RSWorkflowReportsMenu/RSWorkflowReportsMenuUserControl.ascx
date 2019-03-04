<%@ Assembly Name="$SharePoint.Project.AssemblyFullName$" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="RSWorkflowReportsMenuUserControl.ascx.cs" Inherits="ESMA.Paperless.Reports.v16.RSWorkflowReportsMenu.RSWorkflowReportsMenuUserControl" %>
<SharePoint:ScriptLink ID="jquery_1_9_1_min" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/jquery-1.9.1.min.js" Localizable="false"></SharePoint:ScriptLink>
<script type="text/javascript">
    $(document).ready(function () {
        var $selectedItem = $('.selected.ms-core-listMenu-item');
        if ($selectedItem.length > 0) {
            $selectedItem.removeClass("selected ms-core-listMenu-selected");
            $selectedItem.closest("li").removeClass("selected")
        }
        var $reportMenu = $('.menu-item-text').filter(function () { return $(this).text() === 'Reports'; })
        if ($reportMenu.length > 0) {
            $reportMenu.closest("a").addClass("selected ms-core-listMenu-selected");
            $reportMenu.closest("li").addClass("selected");
        }

        // Add class to Reports Menu Webpart
        var $menuwebpartzone = $("#ReportsMenu").closest(".ms-webpartzone-cell");
        if ($menuwebpartzone.length > 0) { //SharePoint 2013
            $menuwebpartzone.addClass("reports-menu-webpartzone");
            $menuwebpartzone.parent().addClass("reports-webpartzone");
        } else {
            $menuwebpartzone = $("#ReportsMenu").closest(".s4-wpcell-plain");
            if ($menuwebpartzone.length > 0) { //SharePoint 2010
                $menuwebpartzone.addClass("reports-menu-webpartzone");
                $menuwebpartzone.closest("table").addClass("reports-webpartzone");
            }
        }
    });
</script>
<div id="ReportsMenu">
    <ul>  
        <li><asp:HyperLink ID="ReportsMenuNew" runat="server" Text="New Report" NavigateUrl="/Pages/reports.aspx" ></asp:HyperLink></li>
        <li><asp:HyperLink ID="ReportsMenuTemplates" runat="server" Text="Report Templates" NavigateUrl="/Pages/reporttemplates.aspx" ></asp:HyperLink></li>
        <li><asp:HyperLink ID="ReportsMenuMyReports" runat="server" Text="My Reports" NavigateUrl="/Pages/myreports.aspx"></asp:HyperLink></li>
    </ul>
</div>