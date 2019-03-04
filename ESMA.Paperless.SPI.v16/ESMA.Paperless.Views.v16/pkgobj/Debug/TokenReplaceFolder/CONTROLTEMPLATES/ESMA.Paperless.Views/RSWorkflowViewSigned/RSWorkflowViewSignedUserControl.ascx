<%@ Assembly Name="ESMA.Paperless.Views.v16, Version=1.0.0.0, Culture=neutral, PublicKeyToken=9df3bfc1eb45232a" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="RSWorkflowViewSignedUserControl.ascx.cs" Inherits="ESMA.Paperless.Views.RSWorkflowViewSigned.RSWorkflowViewSignedUserControl" %>
<h1>
	Workflows signed by me:
</h1>
<div class="grid-workflow">
	<SharePoint:SPGridView ID="gvSigned" runat="server" AllowFiltering="True" 
		AllowSorting="True" AllowPaging="True" AutoGenerateSelectButton="false" 
		AutoGenerateColumns="false" DisplayGroupFieldName="false" EnableViewState="false" HeaderStyle-CssClass="ms-viewheadertr ms-vhltr">
		
			<AlternatingRowStyle CssClass="ms-alternatingstrong" />
			<Columns>
				<SharePoint:SPBoundField DataField="WFID" HeaderText="Workflow ID" SortExpression="WFID" HeaderStyle-CssClass="ms-vh2" ItemStyle-CssClass="ms-vb2 style_wfid"></SharePoint:SPBoundField>
				<asp:HyperLinkField DataTextField="WFLink" HeaderText="Link" SortExpression="WFLink" HeaderStyle-CssClass="ms-vh2" />
				<SharePoint:SPBoundField DataField="WFSubject" HeaderText="Workflow Subject" SortExpression="WFSubject" HeaderStyle-CssClass="ms-vh2" ></SharePoint:SPBoundField>
				<SharePoint:SPBoundField DataField="SignedDateText" HeaderText="Signed Date" SortExpression="SignedDate" HeaderStyle-CssClass="ms-vh2"></SharePoint:SPBoundField>
				<SharePoint:SPBoundField DataField="Amount" HeaderText="Amount" SortExpression="Amount" ItemStyle-Width="70px" HeaderStyle-CssClass="ms-vh2"></SharePoint:SPBoundField>
				<SharePoint:SPBoundField DataField="Rejection" AccessibleHeaderText="false" HeaderStyle-CssClass="ms-vh2"></SharePoint:SPBoundField>
				<SharePoint:SPBoundField DataField="WFStatus" HeaderText="Workflow Status" SortExpression="WFStatus" HeaderStyle-CssClass="ms-vh2"></SharePoint:SPBoundField> 
				<SharePoint:SPBoundField DataField="WFType" HeaderText="Workflow Type" SortExpression="WFType" HeaderStyle-CssClass="ms-vh2"></SharePoint:SPBoundField>
				<SharePoint:SPBoundField DataField="AssignedPerson" HeaderText="Assigned Person" SortExpression="AssignedPerson" HeaderStyle-CssClass="ms-vh2"></SharePoint:SPBoundField>
				<SharePoint:SPBoundField DataField="Urgent" HeaderText="Urgent" SortExpression="Urgent" HeaderStyle-CssClass="ms-vh2"></SharePoint:SPBoundField>
				<SharePoint:SPBoundField DataField="CreatedText" HeaderText="Created" SortExpression="Created" HeaderStyle-CssClass="ms-vh2"></SharePoint:SPBoundField>
				<SharePoint:SPBoundField DataField="Author" HeaderText="Created By" SortExpression="Author" HeaderStyle-CssClass="ms-vh2"></SharePoint:SPBoundField>
				<SharePoint:SPBoundField DataField="ConfidentialWorkflow" HeaderText="Restricted" SortExpression="ConfidentialWorkflow" HeaderStyle-CssClass="ms-vh2"></SharePoint:SPBoundField>
				<SharePoint:SPBoundField DataField="WFLinkText" HeaderText="Link" Visible="false" HeaderStyle-CssClass="ms-vh2"></SharePoint:SPBoundField>
				<SharePoint:SPBoundField DataField="WFRejectionText" HeaderText="Rejection" Visible="false" HeaderStyle-CssClass="ms-vh2"></SharePoint:SPBoundField>
				<SharePoint:SPBoundField DataField="SignedDate" HeaderText="Signed Date" SortExpression="SignedDate" Visible="false" HeaderStyle-CssClass="ms-vh2"></SharePoint:SPBoundField>
                <SharePoint:SPBoundField DataField="Created" HeaderText="Created" SortExpression="Created" Visible="false" HeaderStyle-CssClass="ms-vh2"></SharePoint:SPBoundField>
			</Columns>
		   
		 <SelectedRowStyle CssClass="ms-selectednav" Font-Bold="True" />
		 <PagerStyle HorizontalAlign="Center" VerticalAlign="Bottom" />
			
	</SharePoint:SPGridView>
</div>
<asp:Panel ID="Panel_Paging" runat="server" CssClass="panel_paging">
    <SharePoint:SPGridViewPager ID="newPager" GridViewId="gvSigned" Visible="true" runat="server"></SharePoint:SPGridViewPager>
</asp:Panel>
