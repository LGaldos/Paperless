<%@ Assembly Name="ESMA.Paperless.Webparts.v16, Version=1.0.0.0, Culture=neutral, PublicKeyToken=2ca9af153a3279a1" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="RSWorkflowAdvancedSearchUserControl.ascx.cs" Inherits="ESMA.Paperless.Webparts.v16.RSWorkflowAdvancedSearch.RSWorkflowAdvancedSearchUserControl" %>
<SharePoint:ScriptLink ID="RSJavascriptReference" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/RSJavascript.js" Localizable="false"></SharePoint:ScriptLink>
<SharePoint:ScriptLink ID="jquery_1_9_1_min" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/jquery-1.9.1.min.js" Localizable="false"></SharePoint:ScriptLink>


<SharePoint:ScriptLink ID="Chose" runat="server" Name="/_layouts/15/ESMA.Paperless.Design.v16/js/chosen.jquery.min.js" Localizable="false"></SharePoint:ScriptLink>
<SharePoint:ScriptLink ID="Prims" runat="server" Name="/_layouts/15/ESMA.Paperless.Design.v16/js/prism.js" Localizable="false"></SharePoint:ScriptLink>

<link id="Link2" rel="stylesheet" type="text/css" href="/_layouts/15/ESMA.Paperless.Design.v16/css/prism.css" />
<link id="Link1" rel="stylesheet" type="text/css" href="/_layouts/15/ESMA.Paperless.Design.v16/css/chosen.min.css" />

<script type="text/javascript">
    $(document).ready(function () {

        $(".chosen_search").chosen();
        $(".chosen_search_actor").chosen({ allow_single_deselect: true });
        $(".chosen_search_staff").chosen({ allow_single_deselect: true });
        $(".chosen-select_two").chosen({ disable_search: true });

        //Disable "ENTER" KEY
        //Commented because GSA users use "ENTER" KEY to execute the search
        //           $(document).on("keydown", function (event) {
        //                if (event.keyCode === 13) {
        //                    event.preventDefault();
        //                }
        //            });
        // resolve display message CR24
        $(document).on("keydown", function (event) {

            if (event.keyCode === 8) {

                activeObj = document.activeElement;
                if (document.activeElement.id == "" && activeObj.tagName != "INPUT") {
                    debugger;
                    event.preventDefault();
                }
            }

        });
    });

    var prm = Sys.WebForms.PageRequestManager.getInstance();
    prm.add_pageLoaded(pageLoaded)

    function pageLoaded() {

        $(".chosen_search").chosen();
        $(".chosen-select_two").chosen({ disable_search: true });
        $(".chosen_search_actor").chosen({ allow_single_deselect: true });
        $(".chosen_search_staff").chosen({ allow_single_deselect: true });
    }
</script>

<h1>Advanced Search</h1>

<input id="idDisplay" value="false" style="display: none" />
<div>
    <div id="criteria" class="criteria">
        <div class="row_style">
            <div class="label_column">
                <asp:Label ID="lblSearchByTitle" runat="server" Text="Workflow Subject:"
                    CssClass="label_span"></asp:Label>
            </div>
            <div class="input_column">
                <asp:TextBox ID="txtTitle" runat="server" CssClass="input_text" />
            </div>
        </div>
        <!--From Created to Modified -->
        <div class="row_style">
            <div class="label_column1">
		      <asp:Label ID="lblModifiedPeriod" runat="server" Text="Modified:" CssClass="label_span"/>
	        </div>
            <div class="input_column1_date">

                            <div class="label_span_date">
                                <asp:Label ID="lblFrom" runat="server" Text="From:"/>
                            </div>

                            <div class="input__fecha">
		                        <SharePoint:DateTimeControl ID="dtFrom" LocaleId=2057 CssClassTextBox="input_text" runat="server" DateOnly="True"  EnableViewState="true"/>
                                <asp:CompareValidator id="validateDatedtFrom" runat="server" ControlToValidate="dtFrom$dtFromDate" Type="Date" Operator="DataTypeCheck" ErrorMessage="Please enter a valid date" Display="Dynamic" ForeColor="Red"></asp:CompareValidator>
                            </div>

	                        <div class="label_span_date" >
                                <asp:Label ID="lblTo" runat="server" Text="To:"/>&nbsp;
	                        </div>

	                        <div class="input__fecha">
		                        <SharePoint:DateTimeControl ID="dtTo" LocaleId=2057 CssClassTextBox="input_text" runat="server" DateOnly="True" Enabled="True" EnableViewState="true" />
                                <asp:CompareValidator id="validateDatedtTo" runat="server" ControlToValidate="dtTo$dtToDate" Type="Date" Operator="DataTypeCheck" ErrorMessage="Please enter a valid date" Display="Dynamic" ForeColor="Red"></asp:CompareValidator>
	                        </div>

                        </div>
        </div>
         <!--From Deadline (ESMA-CR01) -->
          <div class="row_style">
            <div class="label_column1">
		       <asp:Label ID="lblDeadlinePeriod" runat="server" Text="Deadline:" CssClass="label_span"/>
	        </div>
            <div class="input_column1_date">

                            <div class="label_span_date">
                                <asp:Label ID="lblDeadlineFrom" runat="server" Text="From:"/>
                            </div>

                            <div class="input__fecha">
		                        <SharePoint:DateTimeControl ID="dtDeadlineFrom" LocaleId=2057 CssClassTextBox="input_text" runat="server" DateOnly="True"  EnableViewState="true"/>
                                <asp:CompareValidator id="validateDatedtDeadlineFrom" runat="server" ControlToValidate="dtDeadlineFrom$dtDeadlineFromDate" Type="Date" Operator="DataTypeCheck" ErrorMessage="Please enter a valid date" Display="Dynamic" ForeColor="Red"></asp:CompareValidator>
                            </div>

	                        <div class="label_span_date" >
                                <asp:Label ID="lblDeadlineTo" runat="server" Text="To:"/>&nbsp;
	                        </div>

	                        <div class="input__fecha">
		                        <SharePoint:DateTimeControl ID="dtDeadlineTo" LocaleId=2057 CssClassTextBox="input_text" runat="server" DateOnly="True" Enabled="True" EnableViewState="true" />
                                <asp:CompareValidator id="validateDatedtDeadlineTo" runat="server" ControlToValidate="dtDeadlineTo$dtDeadlineToDate" Type="Date" Operator="DataTypeCheck" ErrorMessage="Please enter a valid date" Display="Dynamic" ForeColor="Red"></asp:CompareValidator>
	                        </div>

                        </div>
        </div>
        <div class="row_style">
            <div class="label_column">
                <asp:Label ID="lblWFID" runat="server" Text="Workflow ID:" CssClass="label_span" />
            </div>
            <div class="input_column">
                <asp:TextBox ID="txtID" runat="server" CssClass="input_text" />&nbsp;
            </div>
        </div>
        <div class="row_style">
            <div class="label_column">
                <asp:Label ID="lblType" runat="server" CssClass="label_span" Text="Workflow Type:" />
            </div>
            <div class="input_column">
                <asp:DropDownList ID="ddlType" runat="server" Style="display: none" CssClass="input_select chosen_search" />
            </div>
        </div>
        <div class="row_style">
            <div class="label_column">
                <asp:Label ID="lblStatus" runat="server" CssClass="label_span" Text="Workflow Status:" />
            </div>
            <div class="input_column">
                <asp:DropDownList ID="ddlStatus" runat="server" Style="display: none" CssClass="input_select chosen_search" />
            </div>
        </div>
        <div class="row_style">
            <div class="label_column">
                <asp:Label ID="lblActor" runat="server" CssClass="label_span" Text="Actor:" />
            </div>
            <div class="input_column">
                <asp:DropDownList ID="ddlActor" runat="server" Style="display: none" CssClass="input_select chosen_search_actor" />
            </div>
        </div>
        <div class="row_style">
            <div class="label_column">
                <asp:Label ID="lblRole" runat="server" CssClass="label_span" Text="Role in workflow:" />
            </div>
            <div class="input_column">
                <asp:DropDownList ID="ddlRole" runat="server" Style="display: none" CssClass="input_select chosen_search" />
            </div>
        </div>
        <div class="row_style">
            <div class="label_column">
                <asp:Label ID="lblStaff" runat="server" CssClass="label_span" Text="Staff Name:" />
            </div>
            <div class="input_column">
                <asp:DropDownList ID="ddlStaff" runat="server" Style="display: none" CssClass="input_select chosen_search_staff" />
            </div>
        </div>
        <div class="row_style">
            <div class="label_column">
                <asp:Label ID="lblUrgent" runat="server" CssClass="label_span" Text="Urgent:"></asp:Label>
            </div>
            <div class="input_column">
                <asp:DropDownList ID="ddlUrgent" Style="display: none" CssClass="input_select chosen-select_two"
                    runat="server">
                    <asp:ListItem>All</asp:ListItem>
                    <asp:ListItem>Yes</asp:ListItem>
                    <asp:ListItem>No</asp:ListItem>
                </asp:DropDownList>
            </div>
        </div>
        <div class="row_style">
            <div class="label_column">
                <asp:Label ID="lblVAT" runat="server" CssClass="label_span" Text="VAT:"></asp:Label>
            </div>
            <div class="input_column">
                <asp:DropDownList ID="ddlVAT" Style="display: none" CssClass="input_select chosen-select_two"
                    runat="server">
                    <asp:ListItem>All</asp:ListItem>
                    <asp:ListItem>Yes</asp:ListItem>
                    <asp:ListItem>No</asp:ListItem>
                </asp:DropDownList>
            </div>
        </div>
        <div class="row_style">
            <div class="label_column">
                <asp:Label ID="lblABAC" runat="server" CssClass="label_span" Text="ABAC Commitment:"></asp:Label>
            </div>
             <div class="input_column">
                <asp:TextBox ID="txtABAC" runat="server" CssClass="input_text" />&nbsp;
            </div>
        </div>
        <div class="row_style">
            <div class="label_column">
                <asp:Label ID="lblContractor" runat="server" CssClass="label_span" Text="Contractor:"></asp:Label>
            </div>
             <div class="input_column">
                <asp:TextBox ID="txtContractor" runat="server" CssClass="input_text" />&nbsp;
            </div>
        </div>
        <div class="row_style">
            <div class="label_column">
                <asp:Label ID="lblFWCReference" runat="server" CssClass="label_span" Text="FWC Reference:"></asp:Label>
            </div>
             <div class="input_column">
                <asp:DropDownList ID="ddlFWCReference" runat="server" Style="display: none" CssClass="input_select chosen_search_actor" />
            </div>
        </div>
        <div class="row_style">
            <div class="label_column">
                <asp:Label ID="lblIncident" runat="server" CssClass="label_span" Text="Incident Tick:"></asp:Label>
            </div>
            <div class="input_column">
                <asp:DropDownList ID="ddlIncident" Style="display: none" CssClass="input_select chosen-select_two"
                    runat="server">
                    <asp:ListItem>All</asp:ListItem>
                    <asp:ListItem>Yes</asp:ListItem>
                    <asp:ListItem>No</asp:ListItem>
                </asp:DropDownList>
            </div>
        </div>
        <div class="row_style">
            <div class="label_column">
                <asp:Label ID="lblKeyword" runat="server" Text="Keyword in all fields:" CssClass="label_span"></asp:Label>
            </div>
            <div class="input_column">
                <asp:TextBox ID="txtKeyWord" runat="server" CssClass="input_text" />
            </div>
        </div>
             <!-- CR31 style input-column-->
        <div class="row_style">
            <div class="label_column">
                <asp:Label ID="lblSignedByMe" runat="server" CssClass="label_span" Text="Signed by me WF:"></asp:Label>
            </div>
            <div class="input_column" style="margin-top: 10px;">
                <asp:CheckBox ID="cbSignedByMe" runat="server" />
            </div>
        </div>

        <div class="row_style">
            <div class="label_column3">
                <asp:Label ID="lblKeywordWarning" runat="server" Text="<strong>Note:</strong> Search by <strong>keyword in all fields</strong> may take up to one minute."
                    CssClass="label_span"></asp:Label>
            </div>
        </div>
    </div>

    <div class="update_area">
        <div class="row_loading" id="loading">
            <asp:UpdateProgress ID="updateProgressGrid" AssociatedUpdatePanelID="updatePanelGrid" runat="server">
                <ProgressTemplate><div><asp:Label ID="lblLoading" CssClass="label_span" Text = "Loading... " runat="server" /></div></ProgressTemplate>
            </asp:UpdateProgress>
		</div>
		<div>
            <asp:UpdatePanel ID="updatePanelGrid" runat="server">
                <Triggers>
                    <asp:AsyncPostBackTrigger ControlID="btnSearch" EventName="Click"/>
                </Triggers>
                <ContentTemplate>
                    <div class="col_buttons">
		                <asp:Button ID="btnSearch" CssClass="btn_grey" runat="server" Text="Search"/>
	                </div>
                    <div id="results">
			            <div class="row_result">
			                <asp:Label ID="lblResults" CssClass="label_span" runat="server" Visible="False" />
                        </div>
                        <div class="result_grid">
			                <asp:GridView ID="gvResults" runat="server" Visible="False" AllowPaging="True" AllowSorting="True" OnSorting="gvResults_Sorting" OnPageIndexChanging="gvResults_PageIndexChanging" OnRowDataBound="gvResults_RowDataBound" PagerStyle-CssClass="pager" PagerStyle-Font-Bold="true" BorderWidth="0">
				                <PagerSettings FirstPageImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/RSArrowLeftLight.gif"
					                LastPageImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/RSArrowRightLight.gif"
					                NextPageImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/RSArrowRightDark.gif"
					                NextPageText="Next Page"
					                PreviousPageImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/RSArrowLeftDark.gif"
					                PreviousPageText="Previous Page" />
			                </asp:GridView>
			            </div>
                    </div>
                </ContentTemplate>
            </asp:UpdatePanel>
       </div>
	</div>
</div>
