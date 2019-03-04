<%@ Assembly Name="ESMA.Paperless.Webparts.v16, Version=1.0.0.0, Culture=neutral, PublicKeyToken=2ca9af153a3279a1" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="RSWorkflowUserControl.ascx.cs" Inherits="ESMA.Paperless.Webparts.v16.RSWorkflow.RSWorkflowUserControl" %>
<SharePoint:ScriptLink ID="RSJavascriptReference" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/RSJavascript.js" Localizable="false"></SharePoint:ScriptLink>
<SharePoint:ScriptLink ID="jquery_1_9_1_min" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/jquery-1.9.1.min.js" Localizable="false"></SharePoint:ScriptLink>
<SharePoint:ScriptLink ID="CR24Code" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/CR24Code.js" Localizable="false"></SharePoint:ScriptLink>

<SharePoint:ScriptLink ID="Chose" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/chosen.jquery.min.js" Localizable="false"></SharePoint:ScriptLink>
<SharePoint:ScriptLink ID="Prims" runat="server" name="/_layouts/15/ESMA.Paperless.Design.v16/js/prism.js" Localizable="false"></SharePoint:ScriptLink>
<SharePoint:ScriptLink ID="inplview" runat="server" name="/_layouts/15/inplview.js" Localizable="false"></SharePoint:ScriptLink>

<link id="Link2" rel="stylesheet" type="text/css" href="/_layouts/15/ESMA.Paperless.Design.v16/css/prism.css" />
<link id="Link1" rel="stylesheet" type="text/css" href="/_layouts/15/ESMA.Paperless.Design.v16/css/chosen.min.css" />


<script type="text/javascript">
        $(document).ready(function () {


            $(".chosen-select").chosen({ disable_search: true, allow_single_deselect: true });
            $(".chosen-actors").chosen({ allow_single_deselect: true });
            $(".chosen-users").chosen({ allow_single_deselect: true });

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
            $(".chosen-select").chosen({ disable_search: true, allow_single_deselect: true });
            $(".chosen-actors").chosen({ allow_single_deselect: true });
            $(".chosen-users").chosen({ allow_single_deselect: true });
        }
</script>

<script type="text/javascript">
    var RSUrl = '<%=SPContext.Current.Web.Site.Url %>';
    
</script>

<input id="idDisplay" value="false" style="display:none"/>

    <div id="ReassigningActor" class="reassigning_screen" style="display:none">
        <img alt="" src="/_layouts/15/ESMA.Paperless.Design.v16/images/rsclock.png" />
        <asp:Label ID="lblReassigning" runat="server" CssClass="label_span" Text="Please, wait while the actor is being reassigned..."></asp:Label>
    </div>

<!-- CR20 -->
<asp:Panel ID="PanelForbiddenRemoveDocument" runat="server" CssClass="modal"  Visible="false">
    <div class="panel_checkout">
    <div>
        <asp:ImageButton ID="btnForbiddenRemoveDocument" runat="server" 
            ImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/RSIconClose.png" CssClass="btn_close_window" 
            OnClientClick="javascript:var a=0;"/>
        </div>
    <div class="fourth_row_warning">
        <asp:Image ID="imgForbiddenRemoveDocumentWarning" runat="server"  ImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/RSwarning.png" />
        <asp:Label ID="lblForbiddenRemoveDocument" CssClass="label_checkout_warning" runat="server" Text="Sorry, you cannot delete the document. Please, contact with the initiator."></asp:Label>
    </div>
    </div>
</asp:Panel>
  
<!-- CR 24 -->
<asp:Panel ID="PanelLinkToWFWarning" runat="server" CssClass="modal" Visible="false">
    <div class="panel_checkout">
        <div>
            <asp:ImageButton ID="btnLinkToWFWarningClose" runat="server" 
                ImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/RSIconClose.png" CssClass="btn_close_window" 
                OnClientClick="javascript:closeLinkToWFWarning(); return false;" />
            </div>
        <div class="fourth_row_warning">
            <asp:Image ID="imgLinkToWFWarning" runat="server"  ImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/RSwarning.png" />
            <asp:Label ID="lblLinkToWFWarning" CssClass="label_checkout_warning" runat="server" Text=""></asp:Label>
        </div>
    </div>
</asp:Panel>


<%--FIRST ROW--%>
<asp:Panel ID="RSInterface" DefaultButton="btnCancel" runat="server">
    <div class="content_first_row">
        <div class="subcontent_first_row">
	        <div class="first_row_col1">
                <table>
                    <tr>
                        <td class="first_row_col1_subcol1">
                            <table>
                                <tr>
                                    <td class="title_blue">
                                        Workflow Type
                                    </td>
                                </tr>
                                <tr>
                                    <td class="title_blue">
                                        Workflow ID
                                    </td>
                                </tr>
                                <tr>
                                    <td class="title_blue">
                                        Workflow Status
                                    </td>
                                </tr>
                            </table>
                        </td>
                        <td class="first_row_col1_subcol2">
                            <table>
                                <tr>
                                    <td class="title_black">
                                        <asp:Label ID="lblWorkflowType" runat="server" Text="" Visible="true"></asp:Label>
                                    </td>
                                </tr>
                                <tr>
                                    <td class="title_black">
                                        <asp:Label ID="lblWorkflowID" runat="server" Text="" Visible="true"></asp:Label>
                                    </td>
                                </tr>
                                <tr>
                                    <td class="title_black">
                                        <asp:Label ID="lblWorkflowStatus" runat="server" Text="" Visible="true"></asp:Label>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
		    </div>
	        <div class="first_row_col2">
                <asp:Panel ID="ButtonArea" runat="server">
                    <asp:HyperLink ID="HyperLinkPrint" runat="server" Target="_blank" CssClass="hyperlinkArea">
                        <asp:Image ID="PrintImage" CssClass="btn_print" runat="server" ImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/rsprinter.png" ToolTip="Print all workflow"/>
                    </asp:HyperLink>

                    <!-- CR34-->
                    <asp:HyperLink ID="HyperLinkEmail" runat="server" CssClass="btnMail">
                        <asp:Image ID="MailImage" CssClass="btnMail" runat="server" ImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/rsemail.png" ToolTip="Send e-mail"/>
                    </asp:HyperLink>
                   

                    <asp:Button ID="btnSave" CssClass="btn_blue" runat="server" Text="Save" />
                    <asp:Button ID="btnSign" CssClass="btn_blue" runat="server" Text="Sign" />
                    <asp:Button ID="btnAssign" CssClass="btn_blue" runat="server" Text="Assign" />
                    <asp:Button ID="btnReject" CssClass="btn_blue" runat="server" Text="Reject" />
                    <asp:Button ID="btnOnHold" CssClass="btn_blue" runat="server" Text="On hold" />
                    <asp:Button ID="btnDelete" CssClass="btn_blue" runat="server" Text="Delete" OnClientClick="return confirm('Are you sure you want to delete the workflow?');"/>
                    <asp:Button ID="btnCancel" CssClass="btn_blue" runat="server" Text="Cancel" />
                    <asp:Button ID="btnClose" CssClass="btn_blue" runat="server" Text="Close" />
                </asp:Panel>
	        </div>
        </div>
    </div>

    <div class="form_body">
        <%--SECOND ROW--%>
        <div class="content_second_row">
	        <div class="second_row_col1">
		        <div class="col_second_row_height">
                    <div>
                        <asp:Panel ID="PanelConfidential" CssClass="confidential" runat="server">
                            <asp:Label ID="lblConfidential" runat="server" CssClass="label-workflow" Text="Restricted: "></asp:Label>
                            <asp:UpdatePanel ID="updateConfidential" runat="server">
                                <ContentTemplate>
                                        <asp:DropDownList ID="ddlConfidential" style="display:none" CssClass="ddl_confidential chosen-select" runat="server" />                                    
                                </ContentTemplate>
                            </asp:UpdatePanel>
                        </asp:Panel>
                    </div>
                    <div>
                        <div class="general_field"><asp:PlaceHolder ID="PlaceHolder_Conf" runat="server" /></div>
                    </div>
                    <div>
                        <div class="general_field"><asp:PlaceHolder ID="PlaceHolder_GFTable" runat="server" /></div>
                    </div>
                    <div>
                        <asp:Panel ID="PanelLinkToWF" CssClass="confidential" runat="server" DefaultButton="WFID_buttonAdd">
                            <asp:Label ID="lblLinkToWorkFlow" runat="server" CssClass="label-workflow" Text="Link to workflow(s): "></asp:Label>
                            <div style="padding-top: 0.3em;">
                                <table class="tableWFID">
                                    <tr>
                                        <td class="celltxtBoxWFID">
                                            <asp:TextBox ID="WFID_Textbox" CssClass="input_text_general_field_LinkToWorkFlow" runat="server"></asp:TextBox>
                                        </td>
                                        <td class="cellbtnWFID">
                                            <asp:Button ID="WFID_buttonAdd" CssClass="btn_blue_litle_LinkToWorkFlow" runat="server" OnClick="WFID_buttonAdd_Click" Text="Add" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td colspan="2">
                                            <div id="WFID_Div" class="WFID_DivFatherLinkToWorkFlow"></div>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <asp:HiddenField ID="WFID_data" runat="server" />
                                            <script>
                                                var WFID_Data = $("[id$='<%=WFID_data.ClientID %>']").attr("id");
                                            </script>
                                        </td>
                                    </tr>
                                </table>
                                </div>
                        </asp:Panel>
                    </div>
		        </div>
	        </div>
	        <div class="second_row_col2">
		        <div class="col_second_row_height">
                    <div>
                        <asp:Panel ID="ActorArea" runat="server">
                            <table>
                                <tr>
                                    <td>
                                        <asp:Panel ID="DynamicUserListsPanel" runat="server" CssClass="actors"></asp:Panel>
                                    </td>
                                    <td>
                                        <asp:Panel ID="DynamicRadioButtonListPanel" CssClass="rejection_actors" runat="server">
                                        </asp:Panel>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <div ID="RejectionUserSelected"></div>
                                    </td>
                                </tr>
                            </table>
                        </asp:Panel>
                    </div>
		        </div>
	        </div>
        </div>

        <%--THIRD ROW--%>

        <div class="content_third_row">
            <div class="third_row_col1">
                <asp:Panel ID="StepDescriptionArea" runat="server">
		            <div id='StepDescriptionTitle' class="title_blue_step_description2">
			            Step Description
                        <img alt="" id="StepImage" class="expand-collapse" src="/_layouts/15/ESMA.Paperless.Design.v16/images/RSPlus.gif" style="cursor:pointer" onclick="toggleAreas('StepDescription','StepDescriptionTitle','StepImage','title_blue_step_description2','title_blue_step_description')" />
		            </div>
		            <div id="StepDescription" class="content_end_table" style="display:none">
                        <asp:PlaceHolder ID="PlaceHolder_StepDescription" runat="server" />
		            </div>
                </asp:Panel>
	        </div>
        </div>

        <%--FOURTH ROW--%>
        
        <div id="documentAreaDIV" class="content_fourth_row">
        <!--CR27 New ImageWarning, bntCloseWarning And PanelCheckedOutWarning-->
            <asp:Panel ID="PanelCheckedOutWarning" runat="server" CssClass="panel_checkout" Visible="false">
                <div>
                 
                   <asp:ImageButton ID="btnCloseWarning" runat="server" ImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/RSIconClose.png" CssClass="btn_close_window" OnClientClick="javascript:var a=0;"/>
                 </div>
                <div class="fourth_row_warning">
                    <asp:Image ID="ImageWarning" runat="server"  ImageUrl="/_layouts/15/ESMA.Paperless.Design.v16/images/RSwarning.png" />
                    <asp:Label ID="lblDocumentsCheckedOutWarning" CssClass="label_checkout_warning" runat="server" Text="Some documents are checked out. Some actions will remain disabled until these documents are checked-in."></asp:Label>
                </div>
            </asp:Panel>
            <div class="fourth_row_col1">
                <asp:Panel ID="DocumentArea" runat="server">
		            <div class="content_documents">
                        <table cellpadding="0" cellspacing="0">
                            <tr>
                                <td class="title_blue_documentation">
                                    Documents
                                </td>
                                <td class="tabs_column">
                                    <input type="button" id="btnDocsMainTab" runat="server" class="Clicked" value="Main documents"/>
                                    <input type="button" id="btnDocsABACTab" runat="server" class="Initial" value="To be signed on ABAC"/>
                                    <input type="button" id="btnDocsSupportingTab" runat="server" class="Initial" value="Supporting documents"/>
                                    <input type="button" id="btnDocsPaperTab" runat="server" class="Initial" value="To be signed in Paper"/>
                                    <input type="button" id="btnDocsSignedTab" runat="server" class="Initial" value="Signed documents"/>
                                </td>
                                <td class="tabs_blankspace">
                                </td>
                            </tr>
                        </table>
                        <div id="DocsViews">
                            <div id="ViewMain" style="display:block">
                                <table>
                                    <tr>
                                        <td>
                                            <asp:PlaceHolder ID="DocsMain" runat="server"/> 
                                        </td>
                                    </tr>
                                    <tr align="right">
                                        <td>
                                            <asp:PlaceHolder ID="DocsMainButtons" runat="server" />
                                        </td>
                                    </tr>
                                </table>
                            </div>
                            <div id="ViewABAC" style="display:none">
                                <table>
                                    <tr>
                                        <td>
                                            <asp:PlaceHolder ID="DocsAbac" runat="server"/>    
                                        </td>
                                    </tr>
                                    <tr align="right">
                                        <td>
                                            <asp:PlaceHolder ID="DocsAbacButtons" runat="server"/>          
                                        </td>
                                    </tr>
                                </table>
                            </div>
                            <div id="ViewSupporting" style="display:none">
                                <table>
                                    <tr>
                                        <td>
                                            <asp:PlaceHolder ID="DocsSupporting" runat="server"/>    
                                        </td>
                                    </tr>
                                    <tr align="right">
                                        <td>
                                            <asp:PlaceHolder ID="DocsSupportingButtons" runat="server"/>  
                                        </td>
                                    </tr>
                                </table>
                            </div>
                            <div id="ViewPaper" style="display:none">
                                <table>
                                    <tr>
                                        <td>
                                            <asp:PlaceHolder ID="DocsPaper" runat="server"/>  
                                        </td>
                                    </tr>
                                    <tr align="right">
                                        <td>
                                            <asp:PlaceHolder ID="DocsPaperButtons" runat="server"/> 
                                        </td>
                                    </tr>
                                </table>
                            </div>
                            <div id="ViewSigned" style="display:none">
                                <table>
                                    <tr>
                                        <td>
                                            <asp:PlaceHolder ID="DocsSigned" runat="server"/>  
                                        </td>
                                    </tr>
                                    <tr align="right">
                                        <td>
                                            <asp:PlaceHolder ID="DocsSignedButtons" runat="server"/> 
                                        </td>
                                    </tr>
                                </table>
                            </div>
                        </div>
		            </div>
                </asp:Panel>
            </div>
        </div>

        <%--FIFTH ROW--%>
        <div class="content_fifth_row">
            <div class="fifth_row_col1">
    	            <div id="PreviousCommentsTitle" class="title_blue_background">
			            Comments
		            </div>
                    <div class="comments-area">
		                <div class="comments-area-prev">
                            <div>
                                <asp:Label ID="lblPrevComments" CssClass="label_span_comments" runat="server" Text="Previous comments:"></asp:Label>
                            </div>
                            <div class="previous-comments" >
			                    <asp:PlaceHolder ID="PlaceHolder_PreviousComments" runat="server"></asp:PlaceHolder>
                            </div>
		                </div>
                        <div class="comments-area-new">
                            <div>
                                <asp:Label ID="lblMyComments" CssClass="label_span_comments" runat="server" Text="My comment:"></asp:Label>
                            </div>
                            <div id="NewCommentsArea" class="new-comments">
                                <asp:PlaceHolder ID="PlaceHolder_NewComments" runat="server"></asp:PlaceHolder>
                            </div>
                            <div>
                                <asp:Label ID="lblCommentRequired" runat="server" Text=""></asp:Label>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        <!--</div>-->
          <%--SIXTH ROW CR23--%>
    <asp:Panel ID="panel_Closed" runat ="server" Visible ="false">
        <div class="content_sixth_row">
            <div class="fifth_row_col1">
    	            <div id="Div1" class="title_blue_background2">
                         Comments after closure
		            </div>
                    <div class="comments-closed">
		                <div class="comments-area-closed">
                            <div class="title-height">
                                <asp:Label ID="lblPrevCommentsClosed" CssClass="label_span_comments" runat="server" Text="Previous comments closed:"></asp:Label>
                            </div>
                            <div class="closed-comments">
			                    <asp:Label id="TextBoxCommentsClosed" runat="server" CssClass="closed-comments-textarea" />
                            </div>
		                </div>
                        <div class="comments-area-new">
                            <div class="title-height">
                                 <div class="div_left" style="width:180px; margin-top:0px;" >
                                    <asp:Label ID="lblCommentsClosed" CssClass="label_span_comments" runat="server" Text="My comment for closed WF:"></asp:Label>
                                </div>
                                 <div class="div_right">
                                    <asp:Button ID="btnSaveClosedComments" runat="server" CssClass="btn_blue_little" Text="Save" />
                                </div>
                            </div>
                            <div id="NewCommentsClosedArea" class="new-comments-closed">
                                 <asp:textbox id="TextBoxNewCommentsClosed" runat="server" 
                                     CssClass="new-comments-closed-textarea" TextMode="MultiLine"/>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            </asp:Panel>
    </div>  
    <%--SEVENTH ROW--%>
        <div class="content_seventh_row">
        <div class="subcontent_seventh_row">
            <div class="seventh_row_col1">
                <asp:Panel ID="ButtonArea2" runat="server">
                    <asp:Button ID="btnSave2" runat="server" CssClass="btn_blue" Text="Save" />
                    <asp:Button ID="btnSign2" runat="server" CssClass="btn_blue" Text="Sign" />
                    <asp:Button ID="btnAssign2" runat="server" CssClass="btn_blue" Text="Assign" />
                    <asp:Button ID="btnReject2" runat="server" CssClass="btn_blue" Text="Reject" />
                    <asp:Button ID="btnOnHold2" CssClass="btn_blue" runat="server" Text="On hold" />
                    <asp:Button ID="btnDelete2" CssClass="btn_blue" runat="server" Text="Delete" OnClientClick="return confirm('Are you sure you want to delete the workflow?');"/>
                    <asp:Button ID="btnCancel2" CssClass="btn_blue" runat="server" Text="Cancel"/>
                    <asp:Button ID="btnClose2" runat="server" CssClass="btn_blue" Text="Close" />
                </asp:Panel>
            </div>
        </div>
    </div>
</asp:Panel>

 <%--CR20--%>
<asp:Panel ID="panel_DeleteFile" runat="server" class="panel_DeleteFileDisabled" Visible ="false">
              <div class="comments-area-deleted-file">
                    <div class="title-height-deleted-file">
                            <div class="div_left-deleted-file">
                                 <asp:Label ID="Label2" CssClass="label_span_comments-deleted-file " runat="server" Text="Please, introduce the reason for deleting the document"></asp:Label>
                            </div>
                            <div class="div_left-deleted-file mandatory">

                                <asp:Label ID="lblDeleteFileMandatory" CssClass="label_span_comments-deleted-file-mandatory" runat="server" Text="It is mandatory to introduce the reason for deleted the document." Visible =false></asp:Label>

                            </div>
                    </div>
                    <div id="Div2" class="new-comments-deleted-file">
                            <asp:textbox id="TextBoxCommentsDeletedFile" runat="server" 
                                CssClass="new-comments-closed-textarea fix-width" TextMode="MultiLine"/>
                    </div>       
                    <div class="div_right-deleted-file">
                        <asp:Button ID="btnSaveDeleteFile" runat="server" CssClass="btn_blue" Text="Save" />
                    </div> 
                 </div>    
        </asp:Panel>

 <%--CR38 Pop Up warning message--%>
<asp:Panel ID="panel_WarningCloseWF" runat="server" class="panel_DeleteFileDisabled" Visible ="false">
    <div class="comments-area-deleted-file">
        <div class="title-height-deleted-file">
            <div class="div_left-deleted-file">
                 <asp:Label ID="Label1" CssClass="label_span_comments-deleted-file " runat="server" Text="Warning message"></asp:Label>
            </div>
        </div>
        <div class="WarningCloseWF-msg">
                <asp:Label ID="lblWarningCloseWF" CssClass="label_span_warning-close-wf" runat="server" Text="By signing this step you will be closing this transaction since next steps are empty. Please confirm your action."></asp:Label>
           </div>
        <div class="div_right-deleted-file">
            <asp:Button ID="btnCancelWarningCloseWF" runat="server" CssClass="btn_blue" Text="Cancel" />
        </div>
        <div class="div_right-deleted-file">
            <asp:Button ID="btnAcceptWarningCloseWF" runat="server" CssClass="btn_blue" Text="Accept" />
        </div>
    </div>
</asp:Panel>
