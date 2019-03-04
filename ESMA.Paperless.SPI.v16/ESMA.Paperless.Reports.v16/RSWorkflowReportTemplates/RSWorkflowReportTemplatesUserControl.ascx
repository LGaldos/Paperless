<%@ Assembly Name="ESMA.Paperless.Reports.v16, Version=1.0.0.0, Culture=neutral, PublicKeyToken=18a60af8a64a6d12" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="RSWorkflowReportTemplatesUserControl.ascx.cs" Inherits="ESMA.Paperless.Reports.v16.RSWorkflowReportTemplates.RSWorkflowReportTemplatesUserControl" %>
<SharePoint:ScriptLink ID="jquery_1_9_1_min" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/jquery-1.9.1.min.js" Localizable="false"></SharePoint:ScriptLink>
<SharePoint:ScriptLink ID="Chose" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/chosen.jquery.min.js" Localizable="false"></SharePoint:ScriptLink>
<link id="Link1" rel="stylesheet" type="text/css" href="/_layouts/15/ESMA.Paperless.Design.v16/css/chosen.min.css" />

<script type="text/javascript">
    var prm = Sys.WebForms.PageRequestManager.getInstance();
    prm.add_pageLoaded(pageLoaded)

    function pageLoaded() {
        $(".chosen_search_two").chosen({ disable_search: true });
        $(".chosen_search_actor").chosen({ allow_single_deselect: true });
        $(".chosen_search_created").chosen({ allow_single_deselect: true });
        $(".chosen_search_template").chosen({ disable_search: true });
        $(".chosen_search_type").chosen({ allow_single_deselect: true });
        $(".chosen_search_status").chosen({ allow_single_deselect: true });
        $(".chosen_search_role").chosen({ allow_single_deselect: true });
        if ($("[id*=cbAutoReport]").attr('checked')) {
            $("[id*=PanelMyTemplatesAuto]").css('display', 'inline-block');
        }
    }

    function TemplateDeleteConfirmation() {
        return confirm("Are you sure you want to delete this Report Template?");
    }

    function ShowTemplate() {
		if ($("[id*=PanelMyTemplatesAuto]").is(':visible'))
			$("[id*=PanelMyTemplatesAuto]").css('display','none');
		else
			$("[id*=PanelMyTemplatesAuto]").css('display','inline-block');
        //$("[id*=PanelMyTemplatesAuto]").toggle();
    }

</script>
<h1>Report Templates</h1>

<asp:Panel ID="informationMessagePanel" runat="server" Visible="false" CssClass="information_message">
    <asp:Literal ID="informationMesage" runat="server"></asp:Literal>
</asp:Panel>
<!-- ==== -->
<!-- GRID -->
<!-- ==== -->
<asp:Panel ID="ReportTemplatesResultsPanel" runat="server" CssClass="result_grid templates_results">
	<asp:GridView ID="gvReportTemplates" runat="server" AllowPaging="True" PageSize="8" AutoGenerateColumns="false" OnRowDataBound="gvReportTemplates_RowDataBound"  OnPageIndexChanging="gvReportTemplates_PageIndexChanging" ShowHeaderWhenEmpty="True" EmptyDataText="No Report Templates Found" HeaderStyle-CssClass="header_background" AlternatingRowStyle-CssClass="result_grid_even" BorderWidth="0">
		<Columns>
			<asp:BoundField DataField="ID" Visible="false" />
			<asp:TemplateField HeaderText="Name">
				<ItemTemplate>
					<asp:HyperLink ID="hlTemplateDetail" runat="server" />
				</ItemTemplate>
			</asp:TemplateField>
			<asp:BoundField DataField="Author" HeaderText="Created By" />
			<asp:BoundField DataField="Created" HeaderText="Created" DataFormatString="{0:d}" />
		</Columns>
		<PagerSettings FirstPageImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/RSArrowLeftLight.gif"
			LastPageImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/RSArrowRightLight.gif"
			NextPageImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/RSArrowRightDark.gif"
			NextPageText="Next Page"
			PreviousPageImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/RSArrowLeftDark.gif"
			PreviousPageText="Previous Page" />
	</asp:GridView>
</asp:Panel>

<!-- ============== -->
<!-- REPORT DETAILS -->
<!-- ============== -->
<asp:Panel ID="templateDataView" runat="server" Visible="false" CssClass="panel__reportDetails" >

    <div class="report__template--title"><asp:Literal ID="lblTemplateName" runat="server"></asp:Literal></div>

	<h2>REPORT DETAILS</h2>

    <div class="row_style margen--medio">
        <div class="label__detail">Created: </div>
        <div class="input_column"><asp:Label ID="lblLaunchPeriod" runat="server"></asp:Label></div>
    </div>

    <div class="row_style margen--medio">
        <div class="label__detail">Workflow Type: </div>
        <div class="input_column"><asp:Label ID="lblWFType" runat="server"></asp:Label></div>
    </div>

    <div class="row_style margen--medio">
        <div class="label__detail">Workflow Status: </div>
        <div class="input_column"><asp:Label ID="lblWFStatus" runat="server"></asp:Label></div>
    </div>

	<div class="row_style margen--medio">
        <div class="label__detail">Actor: </div>
        <div class="input_column"><asp:Label ID="lblActor" runat="server"></asp:Label></div>
    </div>

    <div class="row_style margen--medio">
        <div class="label__detail">Role in Workflow: </div>
        <div class="input_column"><asp:Label ID="lblWFRole" runat="server"></asp:Label></div>
    </div>


    <div class="row_style margen--medio">
        <div class="label__detail">Restricted: </div>
        <div class="input_column"><asp:Label ID="lblConfidential" runat="server"></asp:Label></div>
    </div>

    <div class="row_style margen--medio">
        <div class="label__detail">Created by: </div>
        <div class="input_column"><asp:Label ID="lblAuthor" runat="server"></asp:Label></div>
    </div>

    <div class="row_style margen--medio">
        <div class="label__detail">Keyword in all fields: </div>
        <div class="input_column"><asp:Label ID="lblKeyword" runat="server"></asp:Label></div>
    </div>

    <div class="row_style margen--medio">
        <div class="label__detail">Show Steps: </div>
        <div class="input_column"><asp:Label ID="lblShowSteps" runat="server"></asp:Label></div>
    </div>

    <!-- TEMPLATE DETAILS -->
	<h2>TEMPLATE DETAILS</h2>
    <div class="row_style margen--medio">
        <div class="label__detail">Shared with: </div>
        <div class="input_column"><asp:Label ID="lblTemplateShare" runat="server"></asp:Label></div>
    </div>
    <div class="row_style margen--medio">
        <div class="label__detail">Send by email regularly: </div>
        <div class="input_column"><asp:Label ID="lblTemplateNotify" runat="server"></asp:Label></div>
    </div>
   <div class="row_style margen--medio">
        <div class="label__detail">Period: </div>
        <div class="input_column"><asp:Label ID="lblTemplateNotifyPeriod" runat="server"></asp:Label></div>
    </div>
   <div class="row_style margen--medio">
        <div class="label__detail">Frequency: </div>
        <div class="input_column"><asp:Label ID="lblTemplateNotifyFrequency" runat="server"></asp:Label></div>
    </div>
   <div class="row_style margen--medio">
        <div class="label__detail">Recipients: </div>
        <div class="input_column"><asp:Label ID="lblTemplateNotifyRecipients" runat="server"></asp:Label></div>
    </div>

    <div id="templateDataViewButtons" class="col_buttons">
        <asp:Button ID="templateUse" runat="server" Text="Generate Report" OnClick="templateUse_Click" CssClass="btn_grey"/>
        <asp:Button ID="templateEdit" runat="server" Text="Edit Template" OnClick="templateEdit_Click" CssClass="btn_grey"/>
        <asp:Button ID="templateDelete" runat="server" Text="Delete Template" OnClick="templateDelete_Click" OnClientClick="return TemplateDeleteConfirmation();" CssClass="btn_grey"/>
    </div>
</asp:Panel>

<asp:Panel ID="templateDataEdit" runat="server" Visible="false">
    <asp:Panel ID="PanelMandatory" runat="server" Visible="false">
        <div class="row_style container__mandatory">
            <asp:Label ID="lblMandatory" runat="server" Text="First date field is mandatory." Visible="true" ForeColor="Red"></asp:Label>
        </div>
    </asp:Panel>

    <div id="criteria2" style="float:left;width:50%" class="panel__reportDetails">
        <div class="row_style margen--medio">
            <div class="label__detail">Created: </div>
            <div class="input_column"><asp:Label ID="lblCriteriaLaunchPeriod" runat="server"></asp:Label></div>
        </div>

        <div class="row_style margen--medio">
            <div class="label__detail">Workflow Type: </div>
            <div class="input_column"><asp:Label ID="lblCriteriaWFType" runat="server"></asp:Label></div>
        </div>

        <div class="row_style margen--medio">
            <div class="label__detail">Workflow Status: </div>
            <div class="input_column"><asp:Label ID="lblCriteriaWFStatus" runat="server"></asp:Label></div>
        </div>

	    <div class="row_style margen--medio">
            <div class="label__detail">Actor: </div>
            <div class="input_column"><asp:Label ID="lblCriteriaActor" runat="server"></asp:Label></div>
        </div>

        <div class="row_style margen--medio">
            <div class="label__detail">Role in Workflow: </div>
            <div class="input_column"><asp:Label ID="lblCriteriaRole" runat="server"></asp:Label></div>
        </div>


        <div class="row_style margen--medio">
            <div class="label__detail">Restricted: </div>
            <div class="input_column"><asp:Label ID="lblCriteriaRestricted" runat="server"></asp:Label></div>
        </div>

        <div class="row_style margen--medio">
            <div class="label__detail">Created by: </div>
            <div class="input_column"><asp:Label ID="lblCriteriaCreatedBy" runat="server"></asp:Label></div>
        </div>

        <div class="row_style margen--medio">
            <div class="label__detail">Keyword in all fields: </div>
            <div class="input_column"><asp:Label ID="lblCriteriaKeyword" runat="server"></asp:Label></div>
        </div>

        <div class="row_style margen--medio">
            <div class="label__detail">Show Steps: </div>
            <div class="input_column"><asp:Label ID="lblCriteriaShowSteps" runat="server"></asp:Label></div>
        </div>

        <div id="templateDataEditButtons" class="col_buttons">
            <asp:Button ID="Button1" runat="server" Text="Save template" OnClick="templateEditAccept_Click" CssClass="btn_grey" />
            <asp:Button ID="Button2" runat="server" Text="Cancel" OnClick="templateEditCancel_Click" CssClass="btn_grey" />
        </div>
    </div>

    <div id="cright">
        <asp:Panel ID="AllTemplates" runat ="server">
            <asp:Panel ID="PanelNameTemplate" runat="server" CssClass="panel_template1" >
                <asp:Panel ID="TamplateErrorMessagePanel" CssClass="row_style container__mandatory" Visible="false" runat="server">
                    <asp:Label ID="lblTemplateMandatory" runat="server" Text="The template already exists." ForeColor="Red"></asp:Label>
                </asp:Panel>

                <div class="row_style">
                         <div class="label_column2" style="FONT-WEIGHT: bold;">
                            <asp:Label ID="lblNameTemplate" runat="server" Text="Template Name(*): " CssClass="label_span" style="width:20%;"></asp:Label>
                        </div>
                        <div class="ddl_style">
                            <asp:TextBox ID="txtNameTemplate" runat="server" CssClass="input_text_template"></asp:TextBox>
                        </div>
                </div>

                <div class="row_style">

                    <div class="label_column2" style="margin-top:1%;">
                        <asp:Label ID="lblShareUsers" runat="server" CssClass="label_span" Text="Share Template:" />
                    </div>
                    <div class="input_column_person">
		                <SharePoint:PeopleEditor ID="peShareUsers" runat="server" SelectionSet="User" IsValid="true" AllowTypeIn="true" MultiSelect="true" ShowEntityDisplayTextInTextBox="true" PlaceButtonsUnderEntityEditor="false" />
                    </div>
                </div>
                <div class="row_style">
                    <div class="label_column2" style="width:30%">
		                <asp:Label ID="lblAutoReport" runat="server" Text="Send by e-mail regularly:" CssClass="label_span"></asp:Label>
                        <asp:CheckBox ID="cbAutoReport" runat="server" Checked ="false" OnClick="JavaScript:ShowTemplate();" />
                    </div>

                </div>
            </asp:Panel>

             <asp:Panel ID="PanelMyTemplatesAuto" runat="server" CssClass="panel_auto" style="display:none">
                <div class="row_style_auto">
	                <div class="label_column_auto">
		                <asp:Label ID="lblStartDate" runat="server" Text="Start Date *:" CssClass="label_span"/>
	                </div>
	                <div class="input_column_auto">
		                <SharePoint:DateTimeControl ID="dtStart" LocaleId=2057 CssClassTextBox="input_text" runat="server" DateOnly="True" />
                        <asp:CompareValidator id="validateDatedtStart" runat="server" ControlToValidate="dtStart$dtStartDate" Type="Date" Operator="DataTypeCheck" ErrorMessage="Please enter a valid date" Display="Dynamic" ForeColor="Red"></asp:CompareValidator> 
	                </div>
	                <div class="label_column_auto">
		                <asp:Label ID="lblEndDate" runat="server" Text="End Date (optional):" CssClass="label_span"/>&nbsp;
	                </div>
	                <div class="input_column_auto">
		                <SharePoint:DateTimeControl ID="dtEnd" LocaleId=2057 CssClassTextBox="input_text" runat="server" DateOnly="True" Enabled="True" />
                        <asp:CompareValidator id="validateDatedtEnd" runat="server" ControlToValidate="dtEnd$dtEndDate" Type="Date" Operator="DataTypeCheck" ErrorMessage="Please enter a valid date" Display="Dynamic" ForeColor="Red"></asp:CompareValidator> 
	                </div>
                </div>

                <div class="row_style_auto">
	                <div class="label_column_auto">
                        <asp:Label ID="lblFrecuency" runat="server" Text="Frequency *:" CssClass="label_span"/>
		            </div>
                    <div class="input_column_auto">
                        <asp:RadioButtonList ID="rblFrecuency" runat="server">
                            <asp:ListItem Text="Daily" Value ="Daily"></asp:ListItem>
                            <asp:ListItem Text="Weekly" Value ="Weekly"></asp:ListItem>
                            <asp:ListItem Text="Monthly" Value ="Monthly"></asp:ListItem>
                            <asp:ListItem Text="Yearly" Value ="Yearly"></asp:ListItem>
                        </asp:RadioButtonList>
                    </div>
                </div>
                 <div class="row_style_auto">
	                <div class="label_column_auto">
                        <asp:Label ID="lblRecipients" runat="server" Text="Report Recipients *:" CssClass="label_span"/>
		            </div>

                    <div class="input_column_person_recipients input_column_auto">
		                <SharePoint:PeopleEditor ID="peRecipients" runat="server" SelectionSet="User" IsValid="true" AllowTypeIn="true" MultiSelect="true" ShowEntityDisplayTextInTextBox="true" PlaceButtonsUnderEntityEditor="false" />
                    </div>

                </div>
             </asp:Panel>
        </asp:Panel>
    </div>
</asp:Panel>
