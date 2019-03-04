<%@ Assembly Name="ESMA.Paperless.Reports.v16, Version=1.0.0.0, Culture=neutral, PublicKeyToken=18a60af8a64a6d12" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="RSWorkflowReportViewerUserControl.ascx.cs" Inherits="ESMA.Paperless.Reports.v16.RSWorkflowReportViewer.RSWorkflowReportViewerUserControl" %>
<SharePoint:ScriptLink ID="jquery_1_9_1_min" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/jquery-1.9.1.min.js" Localizable="false"></SharePoint:ScriptLink>
<link id="LinkStyles" rel="stylesheet" type="text/css" href="/_layouts/15/ESMA.Paperless.Design.v16/css/RSReportsStyles.css"></link>
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
    });

    function resizeIframe(obj) {
        obj.style.height = obj.contentWindow.document.body.scrollHeight + 'px';
        obj.style.width = screen.width + 'px';
    }
</script>
<div id="container">
    <h1>Report Viewer</h1>
    <asp:PlaceHolder ID="ControlContainer" runat="server"/>
</div>
