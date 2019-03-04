<%@ Assembly Name="ESMA.Paperless.Reports.v16, Version=1.0.0.0, Culture=neutral, PublicKeyToken=18a60af8a64a6d12" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="RSWorkflowReportsUserControl.ascx.cs" Inherits="ESMA.Paperless.Reports.v16.RSWorkflowReports.RSWorkflowReportsUserControl" %>
<SharePoint:ScriptLink ID="RSJavascriptReference" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/RSJavascript.js" Localizable="false"></SharePoint:ScriptLink>
<SharePoint:ScriptLink ID="jquery_1_9_1_min" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/jquery-1.9.1.min.js" Localizable="false"></SharePoint:ScriptLink>

<SharePoint:ScriptLink ID="Chose" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/chosen.jquery.min.js" Localizable="false"></SharePoint:ScriptLink>
<SharePoint:ScriptLink ID="Prims" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/prism.js" Localizable="false"></SharePoint:ScriptLink>
<SharePoint:ScriptLink ID="JqueryUI" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/jquery-ui.min.js" Localizable="false"></SharePoint:ScriptLink>

<link id="Link2" rel="stylesheet" type="text/css" href="/_layouts/15/ESMA.Paperless.Design.v16/css/prism.css" />
<link id="Link1" rel="stylesheet" type="text/css" href="/_layouts/15/ESMA.Paperless.Design.v16/css/chosen.min.css" />
<link id="Link3" rel="stylesheet" type="text/css" href="/_layouts/15/ESMA.Paperless.Design.v16/css/jquery-ui.min.css" />

<style>
 .hideColumn {display:none;}
</style>


    <script type="text/javascript">
        $(document).ready(function () {

            $(".chosen_search_two").chosen({ disable_search: true });
            $(".chosen_search_actor").chosen({ allow_single_deselect: true });
            $(".chosen_search_created").chosen({ allow_single_deselect: true });
            $(".chosen_search_template").chosen({ disable_search: true });
            $(".chosen_search_type").chosen({ allow_single_deselect: true });
            $(".chosen_search_status").chosen({ allow_single_deselect: true });
            $(".chosen_search_role").chosen({ allow_single_deselect: true });
        });

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
                $("[id*=PanelMyTemplatesAuto]").show();
            }

            $("[id*=ddlOrder]").on('focus', function () {
                this.previous = this.value;
            }).change(function () {
                var changedRow = this.getAttribute('data-order');
                var previous = parseInt(this.previous);
                var value = parseInt(this.value);
                $("[id*=ddlOrder]").each(function () {
                    var row = this.getAttribute('data-order');
                    var rowValue = parseInt(this.value);
                    if (row != changedRow) {
                        if (value > previous) {
                            if (rowValue <= value && rowValue > previous) {
                                $(this).val(rowValue - 1);
                            }
                        }
                        else {
                            if (rowValue >= value && rowValue < previous) {
                                $(this).val(rowValue + 1);
                            }
                        }
                    }
                });
				this.previous = this.value;
            });

            if (sessionStorage.getItem("rightColumnWidth")) {
                $(".result_grid").width(sessionStorage.getItem('rightColumnWidth'));
            } else {
                sessionStorage.setItem('rightColumnWidth', $("#criteria").width());
            }
        }

        function LoadWindow() {
            $("[id*=PanelOrder]").dialog({
                title: "Customize Report Columns",
                appendTo: "form",
                resizable: false,
                height: "auto",
                width: 400,
                modal: true
            });
            return false;
        }

        function CloseWindow() {
            $("[id*=PanelOrder]").dialog('close');
            return false;
        }

        function ShowTemplate() {
            $("[id*=PanelMyTemplatesAuto]").toggle();
        }

        function OrderChecked() {
            if ($("[id*=cbSelect]:checkbox:checked").length == 0) {
                alert("Select at least one column");
                return false;
            } else {
                return true;
            }
        }

    </script>

    <input id="idDisplay" value="false" style="display:none"/>

<asp:Panel ID="PanelLoading" runat="server" CssClass="panel_loading">
    <div id="loading">

        <asp:UpdateProgress ID="updateProgressGrid" AssociatedUpdatePanelID="UpdatePanelGrid"  runat="server">
            <ProgressTemplate>

                <div style="text-align:center">
                    <img src="/_layouts/15/ESMA.Paperless.Design.v16/images/RSloading.gif" alt="Searching"/>
                </div>
            </ProgressTemplate>
        </asp:UpdateProgress>
    </div>
</asp:Panel>

 <asp:UpdatePanel ID="UpdatePanelGrid" runat="server" >
        <ContentTemplate>

 <div id="container">

    <div id="cleft">
    <h1><asp:Literal ID="lblTitleReports" runat="server" Text="New Report"></asp:Literal></h1>


            <asp:Panel ID="PanelMandatory" runat="server" Visible="false">
                <div class="row_style container__mandatory">
                    <asp:Label ID="lblMandatory" runat="server" Text="First date field is mandatory." Visible="true" ForeColor="Red"></asp:Label>
                </div>
            </asp:Panel>


            <asp:Panel ID="PanelCriteria" runat="server" Visible="true" Width="100%">
                <div id="criteria">
                    <h2>SELECT DATA</h2>
                    <div class="row_style">
	                    <div class="label_column1">
		                    <asp:Label ID="lblIntro" runat="server" Text="Created *:" CssClass="label_span"/>
	                    </div>
	                    <div class="input_column1_date">

                            <div class="label_span_date">
                                <asp:Label ID="lblFrom" runat="server" Text="From:"/>
                            </div>

                            <div class="input__fecha">
		                        <SharePoint:DateTimeControl ID="dtFirst" LocaleId=2057 CssClassTextBox="input_text" runat="server" DateOnly="True"  EnableViewState="true"/>
                                <asp:CompareValidator id="validateDatedtFirst" runat="server" ControlToValidate="dtFirst$dtFirstDate" Type="Date" Operator="DataTypeCheck" ErrorMessage="Please enter a valid date" Display="Dynamic" ForeColor="Red"></asp:CompareValidator> 
                            </div>

	                        <div class="label_span_date" >
                                <asp:Label ID="lblAnd" runat="server" Text="To:"/>&nbsp;
	                        </div>

	                        <div class="input__fecha">
		                        <SharePoint:DateTimeControl ID="dtLast" LocaleId=2057 CssClassTextBox="input_text" runat="server" DateOnly="True" Enabled="True" EnableViewState="true" />
                                <asp:CompareValidator id="validateDatedtLast" runat="server" ControlToValidate="dtLast$dtLastDate" Type="Date" Operator="DataTypeCheck" ErrorMessage="Please enter a valid date" Display="Dynamic" ForeColor="Red"></asp:CompareValidator> 
	                        </div>

                        </div>
                    </div>
                    <!--WF Type-->
                    <div class="row_style">
                        <div class="label_column1">
                            <asp:Label ID="lblType" runat="server" CssClass="label_span" Text="Workflow Type:" />
                        </div>
                        <div class="input_column1">
                            <asp:DropDownList ID="ddlType" style="display:none" CssClass="input_select chosen_search_type" runat="server" AutoPostBack="false"></asp:DropDownList>
                        </div>
                    </div>
                    <!--WF STATUS-->
                    <div class="row_style">
                        <div class="label_column1">
		                    <asp:Label ID="lblStatus" runat="server" CssClass="label_span" Text="Workflow Status:" />
                        </div>
                        <div class="input_column1_list">
                            <asp:DropDownList ID="ddlStatus" style="display:none" CssClass="input_select chosen_search_status" runat="server" AutoPostBack="false"></asp:DropDownList>
                        </div>
	                </div>
                    <!--ACTOR-->
                    <div class="row_style">
                        <div class="label_column1">
		                    <asp:Label ID="lblActor" runat="server" CssClass="label_span" Text="Actor:" />
                        </div>
                        <div class="input_column_person">
                            <asp:DropDownList ID="peActor" style="display:none" CssClass="input_select chosen_search_actor" runat="server" AutoPostBack="false"></asp:DropDownList>

                        </div>
	                </div>
                    <!--ROLE-->
                    <div class="row_style">
                        <div class="label_column1">
		                    <asp:Label ID="lblRole" runat="server" CssClass="label_span" Text="Role in workflow:" />
                        </div>
                        <div class="input_column1_list">
                            <asp:DropDownList ID="ddlRole" style="display:none" CssClass="input_select chosen_search_role" runat="server" AutoPostBack="false"></asp:DropDownList>
                        </div>
	                </div>
                    <!--RESTRICTED-->
                    <div class="row_style">
                        <div class="label_column1">
		                    <asp:Label ID="lblConfidential" runat="server" CssClass="label_span" Text="Restricted:"></asp:Label>
                        </div>
                        <div class="ddl_style">
                            <asp:DropDownList ID="ddlConfidential" style="display:none" runat="server" CssClass="input_select chosen_search_two">
                                <asp:ListItem Text="All" Value="All"></asp:ListItem>
                                <asp:ListItem Text="Restricted" Value="Restricted"></asp:ListItem>
                                <asp:ListItem Text="Non restricted" Value="Non restricted"></asp:ListItem>
                               <%-- <asp:ListItem Text="Group confidential" Value="Group confidential"></asp:ListItem>--%>
                            </asp:DropDownList>
                        </div>
	                </div>

                    <!--CREATED BY-->
                    <div class="row_style">
                        <div class="label_column1">
		                    <asp:Label ID="lblCreated" runat="server" CssClass="label_span" Text="Created by:" />
                        </div>
                        <div class="input_column_person">
                            <asp:DropDownList ID="peCreated" style="display:none" CssClass="input_select chosen_search_created"  runat="server" AutoPostBack="false">
                            </asp:DropDownList>
                        </div>
	                </div>

                    <!--OPEN AMOUNT RAL-->
                    <div class="row_style">
                        <div class="label_column1">
		                    <asp:Label ID="lblGFOpenAmountRAL" runat="server" CssClass="label_span" Text="Open Amount RAL:" />
                        </div>
                        <div class="input_column1">     
		                    <asp:TextBox ID="txtOpenAmountRAL"  runat="server" CssClass ="input_text"></asp:TextBox>
                        </div>
	                </div>

                    <!--AMOUNT CURRENT YEAR-->
                    <div class="row_style">
                        <div class="label_column1">
		                    <asp:Label ID="lblAmountCurrentYear" runat="server" CssClass="label_span" Text="Amount Current Year:" />
                        </div>
                        <div class="input_column1">     
		                    <asp:TextBox ID="txtAmountCurrentYear"  runat="server" CssClass ="input_text"></asp:TextBox>
                        </div>
	                </div>

                    <!--AMOUNT NEXT YEAR-->
                    <div class="row_style">
                        <div class="label_column1">
		                    <asp:Label ID="lblAmountNextYear" runat="server" CssClass="label_span" Text="Amount Next Year:" />
                        </div>
                        <div class="input_column1">     
		                    <asp:TextBox ID="txtAmountNextYear"  runat="server" CssClass ="input_text"></asp:TextBox>
                        </div>
	                </div>

                    <!--AMOUNT TO CANCEL-->
                    <div class="row_style">
                        <div class="label_column1">
		                    <asp:Label ID="lblAmountToCancel" runat="server" CssClass="label_span" Text="Amount To Cancel:" />
                        </div>
                        <div class="input_column1">     
		                    <asp:TextBox ID="txtAmountToCancel"  runat="server" CssClass ="input_text"></asp:TextBox>
                        </div>
	                </div>

                    <!--JUSTIFICATION-->
                    <div class="row_style">
                        <div class="label_column1">
		                    <asp:Label ID="lblJustification" runat="server" CssClass="label_span" Text="Justification:" />
                        </div>
                        <div class="input_column1">     
		                    <asp:TextBox ID="txtJustification"  runat="server" CssClass ="input_text"></asp:TextBox>
                        </div>
	                </div>

                    <!--GL ACCOUNT-->
                    <div class="row_style">
                        <div class="label_column1">
		                    <asp:Label ID="lblGLAccount" runat="server" CssClass="label_span" Text="GL Account:" />
                        </div>
                        <div class="input_column1">     
		                    <asp:TextBox ID="txtGLAccount"  runat="server" CssClass ="input_text"></asp:TextBox>
                        </div>
	                </div>

                    <!--BUDGET LINE-->
                    <div class="row_style">
                        <div class="label_column1">
		                    <asp:Label ID="lblBudgetLine" runat="server" CssClass="label_span" Text="Budget Line:" />
                        </div>
                        <div class="input_column1">     
		                    <asp:TextBox ID="txtBudgetLine"  runat="server" CssClass ="input_text"></asp:TextBox>
                        </div>
	                </div>

                    <!--WORKFLOW SUBJECT-->
                    <div class="row_style">
                        <div class="label_column1">
		                    <asp:Label ID="lblWFSubject" runat="server" CssClass="label_span" Text="Workflow Subject:" />
                        </div>
                        <div class="input_column1">     
		                    <asp:TextBox ID="txtWFSubject"  runat="server" CssClass ="input_text"></asp:TextBox>
                        </div>
	                </div>

                    <!--PERSONAL FILE-->
                    <div class="row_style">
                        <div class="label_column">
                            <asp:Label ID="lblPersonalFile" runat="server" CssClass="label_span" Text="Personal File:"></asp:Label>
                        </div>
                         <div class="input_column" style="margin-top: 10px;">
                            <asp:CheckBox ID="cbPersonalFile" runat="server" />
                        </div>
                    </div>

                    <!--FWC REFERENCE-->
                    <div class="row_style">
                        <div class="label_column">
                            <asp:Label ID="lblGFFWCReference" runat="server" CssClass="label_span" Text="FWC Reference:"></asp:Label>
                        </div>
                        <div class="input_column">
                            <asp:DropDownList ID="ddlGFFCWReference" Style="display: none" CssClass="input_select chosen-select_two" runat="server">
                            </asp:DropDownList>
                        </div>
                    </div>
                 
                    <!--KEYWORD-->
                    <div class="row_style">
                        <div class="label_column1">
		                    <asp:Label ID="lblFreeText" runat="server" Text="Keyword in all fields:" CssClass="label_span"></asp:Label>
                        </div>
                        <div class="input_column1">
		                    <asp:TextBox ID="txtFreeText"  runat="server" CssClass ="input_text"></asp:TextBox>
                        </div>
                     </div>

                    <div class="row_style">
		                <asp:Label ID="lblKeywordWarning" runat="server" Text="<strong>Note:</strong> Search by <strong>keyword in all fields</strong> may take up to one minute." CssClass="label_span"></asp:Label>
                    </div>

                </div>
            </asp:Panel>
        </div>

            <asp:Panel ID="PanelButtonsBottom" runat="server" Visible="true">
                <div class="col_buttons">                    
                    <asp:Button ID="btnCreateReport" CssClass="btn_grey" runat="server" Text="Generate Report" onclick="btnCreateReport_Click"/>
                    <asp:Button ID="btnClearFields" CssClass="btn_grey" runat="server" Text="Clear" OnClick="btnClearFields_Click" />
	            </div>
	        </asp:Panel>


            <asp:Panel ID="ResultsPanel" runat="server" Visible="false" CssClass="grid-workflow" >
                <h2 class="data-for-report-title">DATA FOR REPORT</h2>
		        <div class="row_result">
                    <asp:Label ID="lblResults" CssClass="label_span" runat="server" Visible="False" />

                </div>
                <div class="row_result">
                    <asp:Label ID="lblTimerJob" runat="server" Text ="Due to the large amount of data being processed, the report will automatically be saved in the 'My Reports' section when it has finished executing" Visible="false" Font-Italic="True"></asp:Label>
                </div>
				<div class="result_grid">
					<asp:GridView ID="gvReport" runat="server" AllowSorting="true" AutoGenerateColumns="false" Visible="false" PagerStyle-CssClass="pager" PagerSettings-Position="Bottom" OnPageIndexChanging="gvReport_PageIndexChanging" OnRowDataBound="gvReport_RowDataBound" OnSorting="gvReport_Sorting" HeaderStyle-CssClass="header_background" AlternatingRowStyle-CssClass="result_grid_even" BorderWidth="0">
						<PagerSettings FirstPageImageUrl="/_layouts/images/RSArrowLeftLight.gif"
								LastPageImageUrl="/_layouts/images/RSArrowRightLight.gif"
								NextPageImageUrl="/_layouts/images/RSArrowRightDark.gif"
								NextPageText="Next Page"
								PreviousPageImageUrl="/_layouts/images/RSArrowLeftDark.gif"
								PreviousPageText="Previous Page" />
					</asp:GridView>
				</div>
            </asp:Panel>

            <asp:Panel ID="PanelButtonsReport" runat="server" Visible="false" style="margin-top:0">
                    <div class="col_buttons">
                        <asp:Button ID="btnCustomize" CssClass="btn_grey" runat="server"
                            Text="Customize report outcome" Visible="true" style="width:200px; "
                             OnClientClick="return LoadWindow();" OnClick="btnOrderColumns_Click" />
                        <asp:Button ID="btnShowSteps" CssClass="btn_grey" runat="server"
                            Text="Show steps" Visible="true"
                            onclick="btnShowSteps_Click" />
                        <asp:Button ID="btnExportExcel" CssClass="btn_grey" runat="server" Text="Export to Excel" Visible="true"  OnClick="btnExportExcel_Click"  />
                        <asp:Button ID="btnSaveTemplate" CssClass="btn_grey" runat="server" Text="Save as Template" OnClick="btnSaveTemplate_Click"/>
                    </div>
            </asp:Panel>

            <!--ORDER PANEL-->
            <asp:Panel ID="PanelOrder" runat="server" style="display:none">
                <div class="row_style_order">
                    <asp:Label ID="lblTitleSort" runat="server" CssClass="label_span_order" Text="Columns"></asp:Label>
                </div>
                <div style="height:500px !important;overflow-y:scroll;">
                    <asp:GridView ID="gvSort" runat="server" AutoGenerateColumns ="False"
                            EnableModelValidation="True" CssClass="grid_order" CaptionAlign="Top"
                         DataKeyNames="DataField" AllowSorting="true" BorderWidth="0">
                        <Columns>
                            <asp:TemplateField HeaderText="Display" ControlStyle-ForeColor="#F1F1F2">
                                <ItemTemplate>
                                    <asp:CheckBox ID="cbSelect" runat="server" />
                                </ItemTemplate>
                                <ItemStyle Width="50px" />
                                <HeaderStyle Width="50px" />
                                <ControlStyle ForeColor="#F1F1F2" />
                            </asp:TemplateField>
                            <asp:BoundField DataField="Column" HeaderText="Column Name">
                                <ControlStyle Width="200px" />
                                <FooterStyle Width="200px" />
                                <HeaderStyle Width="200px" />
                                <ItemStyle Width="200px" />
                            </asp:BoundField>
                            <asp:TemplateField HeaderText="Position">
                                <ItemTemplate>
                                    <asp:DropDownList ID="ddlOrder" runat="server"></asp:DropDownList> <!--ORDER DROPDOWN-->
                                </ItemTemplate>
                                <ControlStyle Width="75px" />
                                <FooterStyle Width="75px" />
                                <HeaderStyle Width="75px" />
                                <ItemStyle Width="75px" />
                            </asp:TemplateField>
                            <asp:BoundField DataField="DataField" Visible="False"/>
                        </Columns>
                    </asp:GridView>
                </div>
                <div class="row_style_order">
                    <asp:Label ID="lblSort" runat="server" Text="Sort" CssClass="label_span_order"></asp:Label>
                </div>

                <div class="first_row_col1_order">
                    <div class="row_style">
                        <asp:Label ID="Label2" runat="server" Text="Sort by the column"></asp:Label>
                    </div>
                    <div class="row_style">
                        <asp:DropDownList ID="ddlColumnSort" runat="server">
                        </asp:DropDownList>
                    </div>
                    <div class="row_style_order">
                        <asp:RadioButtonList ID="rblOrder" runat="server">
                            <asp:ListItem Selected="True" Text="Show items in ascending order" Value="ASC"></asp:ListItem>
                            <asp:ListItem Selected="False" Text="Show items in descending order" Value="DESC"></asp:ListItem>
                        </asp:RadioButtonList>
                    </div>
                    <div>
                        <asp:CheckBox ID="cbColumnsSettingsSave" runat="server" Text="Save Settings" />
                    </div>

                    <div class="col_buttons">
                        <asp:Button ID="btnOrderColumns" CssClass="btn_grey" runat="server" Text="OK" Visible="true" OnClientClick="return OrderChecked();" OnClick="btnOrderColumns_Click"/>
                        <asp:Button ID="btnCancel" CssClass="btn_grey" runat="server" Text="Cancel" Visible="true" OnClientClick="return CloseWindow();"/>
                    </div>
                </div>
            </asp:Panel>
       </div>
       </ContentTemplate>
    </asp:UpdatePanel>
