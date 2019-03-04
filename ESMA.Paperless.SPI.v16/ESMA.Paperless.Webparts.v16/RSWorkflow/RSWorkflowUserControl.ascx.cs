using System;
using System.Web;
using System.Collections;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebPartPages;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Utilities;
using System.Threading;
using System.Drawing;
using System.Net.Mail;
using System.Linq;

namespace ESMA.Paperless.Webparts.v16.RSWorkflow
{
    public partial class RSWorkflowUserControl : UserControl
    {

        List<DropDownList> groupDDLs;
        List<Label> groupLabels;
        RadioButtonList groupRadioButtons;
        Dictionary<string, string> parameters;
        Dictionary<string, string> prevDictionary;
        Dictionary<string, string> actorsDictionary;
        Dictionary<string, string> generalFieldsSessionDictionary;
        ListViewWebPart listViewWebPartMain;
        ListViewWebPart listViewWebPartAbac;
        ListViewWebPart listViewWebPartSupporting;
        ListViewWebPart listViewWebPartPaper;
        ListViewWebPart listViewWebPartSigned;
        string wftypeName;
        bool IsPageRefresh = false;
        

        #region OnPageLoad

        ///// <summary>
        ///// Page load
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        protected void Page_Load(object sender, EventArgs e)
        {

            string strError = "";
            try
            {
                IsPageRefresh = false;
                this.Page.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                try
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                        {
                            SPWeb Web = Site.OpenWeb();

                            if (!Web.AllowUnsafeUpdates)
                                Web.AllowUnsafeUpdates = true;


                            parameters = General.GetConfigurationParameters(Web);
                            SPUser loggedUser = General.GetRealCurrentSPUser(this.Page);

                            try
                            {

                                bool isRejection = IsRejectingPostback(this.Page);
                                InitiateSessionStatesAndWFID(Web, isRejection, IsPageRefresh);

                                if (HttpContext.Current.Session["FormWFType"] != null && HttpContext.Current.Session["FormWFID"] != null && loggedUser != null)
                                {

                                    ControlManagement.InitializeActorControls(ref groupDDLs, ref groupLabels, ref groupRadioButtons);
                                    InitiateButtonControls(HttpContext.Current.Session["FormWFID"].ToString());

                                    string wfid = HttpContext.Current.Session["FormWFID"].ToString();
                                    string wftypeOrder = HttpContext.Current.Session["FormWFType"].ToString();

                                    //this.wftypeName = WorkflowDataManagement.GetWorkflowTypeName(wftypeOrder, Web);
                                    SPListItem wfTypeConfiguration = WorkflowDataManagement.GetWorkflowTypeConfiguration(wftypeOrder, Web);
                                    this.wftypeName = wfTypeConfiguration["Title"].ToString();

                                    SPList docList = WorkflowDataManagement.GetWorkflowLibrary(wftypeName, Web);
                                    SPList logList = WorkflowDataManagement.GetWorkflowLogList(wftypeName, Web);

                                    if (!string.IsNullOrEmpty(wftypeName))
                                    {
                                        lblWorkflowType.Text = wftypeName;
                                        lblWorkflowID.Text = wfid;

                                        //CR24
                                        HiddenField hid = General.Controles.FindControlRecursive<HiddenField>(this.Page, "WFID_data");
                                        HttpContext.Current.Session["FormLinkToWorkFlowModified" + wfid] = hid.Value;

                                        //Try to get workflow item
                                        SPListItem item = WorkflowDataManagement.GetWorkflowItem(wfid, wftypeName, Web);

                                        if (item == null && HttpContext.Current.Session["FormWFNew"].ToString() != "1")
                                            General.ShowErrorTemplate(RSTemplateMessageType.Permissions_Required);
                                        else
                                        {

                                            if (item == null || (item != null && item.DoesUserHavePermissions(loggedUser, SPBasePermissions.ViewListItems)))
                                            {
                                                bool itemExists = false;
                                                bool itemIsOld = true;
                                                bool reassignToBackupActor = false; //ESMA-CR31-BackupGroups
                                                bool isBackupInitiator = false;
                                                bool isBackupResponsible = false;

                                                int currentStep = 0;

                                                //Create or get workflow item
                                                WorkflowDataManagement.WorkflowSetUpOnLoad(docList, Web, ref item, wfid, wfTypeConfiguration, HttpContext.Current.Session["FormCreating" + wfid], ref itemIsOld, ref itemExists, parameters, loggedUser, null, reassignToBackupActor, currentStep, false);
                                                currentStep = WorkflowDataManagement.GetWorkflowCurrentStep(item, Web, wfid);

                                                //Get workflow item base metadata
                                               
                                                GeneralFields.LoadStepsDescription(item, PlaceHolder_StepDescription, wfid, wftypeName, currentStep, Web, parameters);
                                              
                                                string status = WorkflowDataManagement.GetWorkflowStatus(item, Web, parameters, wfid);
                                                lblWorkflowStatus.Text = status;
                                               
                                                //General Fields
                                                InititateGeneralFieldsDictionary(wfid);

                                                if (parameters.ContainsKey("AD User") && parameters.ContainsKey("AD Password"))
                                                {
                                                    SPUser initiator = General.GetAuthor(wfid, item, Web);
                                                    //string author = General.GetAuthor(wfid, item);
                                                    string userAD = General.Decrypt(parameters["AD User"]);
                                                    string passwordAD = General.Decrypt(parameters["AD Password"]);
                                                    string domain = parameters["Domain"];
                                                    SPUser administratorUser = General.GetAdministratorUser(parameters, Web, wfid);
                                                    SPUser stepResponsible = WorkflowDataManagement.GetWorkflowCurrentStepResponsible(item, Web, wfid, domain);
                                                    List<string> groupNames = WorkflowDataManagement.GetGroupNames(item["InitialSteps"] != null ? item["InitialSteps"].ToString() : string.Empty, Web, wfid);
                                                    string confidentialValue = ddlConfidential.SelectedValue;
                                                    //ESMA-CR31-Backup Responsibles
                                                    Dictionary<string, string> actorsBackupDictionary = ControlManagement.GetStepBackupResponsibles(item, wfid, Web);
                                                    bool isBackupMember = WorkflowDataManagement.IsMemberOfBackupGroup(loggedUser, wfid, domain, actorsBackupDictionary, userAD, passwordAD, ref isBackupInitiator, ref isBackupResponsible, Convert.ToString(currentStep), parameters);
                                                    if (isBackupMember)
                                                        reassignToBackupActor = true;


                                                    //In case that the AssignedPerson is (DELETED) && NON Restricted
                                                    if ((confidentialValue.ToUpper().Equals("NON RESTRICTED")) && ((stepResponsible == null) || (stepResponsible.Name.ToLower().Contains("(deleted)"))) && (!status.ToLower().Equals(parameters["Status Closed"].ToLower())) && (!status.ToLower().Equals(parameters["Status Deleted"].ToLower())))
                                                        ReassignDefaultActor_NonConf(wfid, Web, currentStep, ref item, administratorUser, domain, userAD, passwordAD, loggedUser, groupNames, ref stepResponsible, confidentialValue, actorsBackupDictionary, status, reassignToBackupActor, false);


                                                    //Base processes for information and user load
                                                    BasicInitProcesses(item, Web, wfid, wftypeOrder, currentStep, status, userAD, passwordAD, itemIsOld, isRejection, itemExists, stepResponsible, loggedUser, logList, initiator, groupNames, actorsBackupDictionary, reassignToBackupActor, groupDDLs.Count, false);
                                                    
                                                    //GSA-CR26-Staff Ext Access
                                                    bool noAccess = false;
                                                    if (HttpContext.Current.Session["FormWFNew"].ToString() != "1")
                                                        noAccess = WorkflowDataManagement.IsMemberOfStaffExtGroup(wfid, parameters, loggedUser, userAD, passwordAD, DynamicUserListsPanel, currentStep.ToString());
                                                  

                                                    if (!noAccess)
                                                    {
                                                        //2016/4/21
                                                         
                                                        
                                                        //Enable or disable interface controls according to logged user profile and permissions
                                                        if (HttpContext.Current.Session["FormReject" + wfid] != null)
                                                            ControlManagement.EnableDisableUserInterface(status, currentStep, ref DynamicUserListsPanel, ref DynamicRadioButtonListPanel, ref btnSign, ref btnSign2, ref btnSave, ref btnSave2, ref btnOnHold, ref btnOnHold2, ref btnCancel, ref btnCancel2, ref btnDelete, ref btnDelete2, ref btnClose, ref btnClose2, ref HyperLinkPrint, ref btnAssign, ref btnAssign2, ref btnReject, ref btnReject2, ref ddlConfidential, ref groupRadioButtons, loggedUser, initiator, parameters, itemIsOld, true, ref PlaceHolder_PreviousComments, ref PlaceHolder_NewComments, ref lblCommentRequired, ref PlaceHolder_GFTable, ref DocsMainButtons, ref DocsAbacButtons, ref DocsSupportingButtons, ref DocsPaperButtons, ref DocsSignedButtons, ref lblDocumentsCheckedOutWarning, ref PanelCheckedOutWarning, Web, item, wfid, wftypeOrder, wftypeName, ref WFID_Textbox, ref WFID_buttonAdd, actorsBackupDictionary, domain, userAD, passwordAD);
                                                        else
                                                            ControlManagement.EnableDisableUserInterface(status, currentStep, ref DynamicUserListsPanel, ref DynamicRadioButtonListPanel, ref btnSign, ref btnSign2, ref btnSave, ref btnSave2, ref btnOnHold, ref btnOnHold2, ref btnCancel, ref btnCancel2, ref btnDelete, ref btnDelete2, ref btnClose, ref btnClose2, ref HyperLinkPrint, ref btnAssign, ref btnAssign2, ref btnReject, ref btnReject2, ref ddlConfidential, ref groupRadioButtons, loggedUser, initiator, parameters, itemIsOld, false, ref PlaceHolder_PreviousComments, ref PlaceHolder_NewComments, ref lblCommentRequired, ref PlaceHolder_GFTable, ref DocsMainButtons, ref DocsAbacButtons, ref DocsSupportingButtons, ref DocsPaperButtons, ref DocsSignedButtons, ref lblDocumentsCheckedOutWarning, ref PanelCheckedOutWarning, Web, item, wfid, wftypeOrder, wftypeName, ref WFID_Textbox, ref WFID_buttonAdd, actorsBackupDictionary, domain, userAD, passwordAD);


                                                        //Save actor lists control status
                                                        InititatePrevResponsibleDictionary(wfid);
                                                        InititateActorsDictionary(wfid, currentStep.ToString());


                                                        //CR20 (Comments Panel - Document Removed//Forbiden to remove documents)
                                                        DeletedDocumentsManagement(wfid, currentStep, logList, Web, loggedUser, status);

                                                        //CR23 -> Review (PAPBUG-142)
                                                        if (status.Equals("Closed"))
                                                        {

                                                            if (initiator.ToString().Contains(loggedUser.Name))
                                                                btnSaveClosedComments.Visible = false;
                                                            else
                                                                btnSaveClosedComments.Visible = true;

                                                            string commentClosed = WorkflowDataManagement.GetPreviousCommentClosed(Web, wfid, currentStep, logList, loggedUser);
                                                            TextBoxCommentsClosed.Text = commentClosed;
                                                            panel_Closed.Visible = true;
                                                        }

                                                        //ESMA-CR38-Close Warning PopUp
                                                        if ((HttpContext.Current.Session["ShowCloseWarningPopUp" + wfid] != null) && (HttpContext.Current.Session["ShowCloseWarningPopUp" + wfid].ToString().Equals("true")) && (loggedUser.ID.Equals(stepResponsible.ID)))
                                                            ShowPanelWarningCloseWorkflow(wfid);
                                                        else
                                                            HidePanelWarningCloseWorkflow(wfid);
                                                    }
                                                    else  //CR26
                                                    {
                                                        //Problem with Transfer... ERROR: Unable to evaluate expression because the code is optimized or a native frame is on top of the call stack
                                                        strError = "ACCESS DENIED. You are not granted with the required permissions to access this item.";

                                                        General.ShowErrorTemplate(RSTemplateMessageType.Permissions_Required);
                                                        //SPUtility.TransferToErrorPage(strError);

                                                    }

                                                }
                                                else
                                                {
                                                    string message = "The 'AD User' or 'AD Password' parameters are empty.";
                                                    General.saveErrorsLog("Page_Load() " + null, message);
                                                }
                                            }
                                            else if (item != null && !item.DoesUserHavePermissions(loggedUser, SPBasePermissions.ViewListItems))
                                            {
                                                General.ShowErrorTemplate(RSTemplateMessageType.Permissions_Required);
                                                General.saveErrorsLog("Page_Load", "!item.DoesUserHavePermissions");
                                            }
                                            //SPUtility.TransferToErrorPage("ACCESS DENIED. You are not granted with the required permissions to access this item.");
                                        }

                                    }
                                    else
                                    {
                                        General.ShowErrorTemplate(RSTemplateMessageType.Context_Url_No_Parameters);
                                        //SPUtility.TransferToErrorPage("ACCESS DENIED. The current context URL has no parameters or its parameters are not the correct ones.");
                                        General.saveErrorsLog("Page_Load", "else !string.IsNullOrEmpty(wftypeName)");
                                    }
                                    HttpContext.Current.Session["FormRefreshing" + wfid] = null;
                                }
                                else
                                    //SPUtility.TransferToErrorPage("ACCESS DENIED. The current context URL has no parameters or its parameters are not the correct ones.");
                                    General.ShowErrorTemplate(RSTemplateMessageType.Context_Url_No_Parameters);
                            }
                            catch (Exception ex)
                            {
                                General.saveErrorsLog("Page_Load", "Error1: " + ex.Message);

                                //CR26
                                if (!string.IsNullOrEmpty(strError))
                                    General.ShowErrorTemplate(RSTemplateMessageType.Permissions_Required);
                                else
                                    General.ShowErrorTemplate(RSTemplateMessageType.Context_Url_No_Parameters);
                            }
                            finally
                            {
                                if (Web.AllowUnsafeUpdates)
                                    Web.AllowUnsafeUpdates = false;

                                Web.Close();
                                Web.Dispose();
                            }
                        }
                    });
                }
                catch (Exception ex2)
                {
                    General.saveErrorsLog("Page_Load", "Error2: " + ex2.Message);

                    //CR26
                    if (!string.IsNullOrEmpty(strError))
                        General.ShowErrorTemplate(RSTemplateMessageType.Permissions_Required);
                    else
                        General.ShowErrorTemplate(RSTemplateMessageType.Context_Url_No_Parameters);
                }




            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "Page_Load " + ex.Message);
            }
        }



        //CR20
        private void ShowPanelDeleteFile(string WFID)
        {
            try
            {
                RSInterface.Enabled = false;
                panel_DeleteFile.Visible = true;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(WFID, "ShowPanelDeleteFile() " + ex.Message);

            }
        }

        //ESMA-CR38-Close Warning Pop Up
        private void ShowPanelWarningCloseWorkflow(string wfid)
        {
            try
            {
                RSInterface.Enabled = false;
                panel_WarningCloseWF.Visible = true;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "ShowPanelWarningCloseWorkflow() " + ex.Message);

            }
        }

        private void HidePanelWarningCloseWorkflow(string wfid)
        {
            try
            {
                RSInterface.Enabled = true;
                panel_WarningCloseWF.Visible = false;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "HidePanelWarningCloseWorkflow() " + ex.Message);

            }
        }

        private void DeletedDocumentsManagement(string wfid, int currentStep, SPList logList, SPWeb Web, SPUser loggedUser, string status)
        {
            try
            {
                if (!status.Equals("Draft"))
                {
                    string actionTakenDeletedFile = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.DocumentRemoved.ToString());
                    bool removeDocument = WorkflowDataManagement.CheckIfUserRemoveDocument(Web, wfid, logList, currentStep.ToString(), loggedUser, actionTakenDeletedFile);

                    if (removeDocument.Equals(true))
                        ShowPanelDeleteFile(wfid);
                    else
                    {
                        string actionTaken = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.TryRemoveDocument.ToString());
                        bool triedRemoveDocument = WorkflowDataManagement.CheckIfUserRemoveDocument(Web, wfid, logList, currentStep.ToString(), loggedUser, actionTaken);

                        if (triedRemoveDocument)
                        {
                            //Show Message
                            PanelForbiddenRemoveDocument.Visible = true;

                            //Save Comment
                            WorkflowDataManagement.SetCommentDeleteFile(Web, wfid, logList, currentStep.ToString(), loggedUser, "Informed Used", actionTaken);
                        }
                        else
                            PanelForbiddenRemoveDocument.Visible = false;

                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "DeletedDocumentsManagement() " + ex.Message);

            }

        }

        private void ReassignDefaultActor_NonConf(string wfid, SPWeb Web, int currentStep, ref SPListItem item, SPUser administratorUser, string domain, string userAD, string passwordAD, SPUser loggedUser, List<string> groupNames, ref SPUser stepResponsible, string confidentialValue, Dictionary<string, string> actorsBackupDictionary, string status, bool reassignToBackupActor, bool isSaving)
        {
            try
            {
                SPRoleDefinition roleDefinitionRSRead = SPContext.Current.Web.Site.RootWeb.RoleDefinitions["RS Read"];
                SPRoleDefinition roleDefinitionRSContributor = SPContext.Current.Web.Site.RootWeb.RoleDefinitions["RS Contribute"];
                SPRoleDefinition roleDefinitionRSFullControl = SPContext.Current.Web.Site.RootWeb.RoleDefinitions["RS Full Control"];

                stepResponsible = General.GetDefaultUserToReassign(groupNames[(currentStep - 1)], parameters, Web, administratorUser, wfid, domain, userAD, passwordAD);

                //Updating the WF Library (Permissions) + Step X Assigned To
                WorkflowDataManagement.SetWorkflowStepXAssignedTo(ref item, currentStep, Web, loggedUser, wfid, stepResponsible);
                WorkflowDataManagement.SetAssignedPersonWorkflow(ref item, stepResponsible, loggedUser, parameters, confidentialValue, wfid, actorsBackupDictionary, status, reassignToBackupActor, currentStep, isSaving);
                //Updating the WF History (Permissions)
                SPListItem wfHistorItem = WorkflowDataManagement.GetWorkflowHistoryRecord(wfid, Web);
                WorkflowDataManagement.SetWorkflowHistoryAssignedPerson(ref wfHistorItem, stepResponsible, loggedUser, Web, wfid, reassignToBackupActor,status, parameters);
                Permissions.SetStepResponsiblePermissionsNotConfid(ref wfHistorItem, stepResponsible, loggedUser, parameters, wfid, roleDefinitionRSRead, roleDefinitionRSContributor, roleDefinitionRSFullControl, status, actorsBackupDictionary, reassignToBackupActor, currentStep, isSaving);
               
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "ReassignDefaultActor_NonConf() " + ex.Message);

            }
        }

       
        /// <summary>
        /// Gets if the postback process was launched by Reject button.
        /// </summary>
        /// <param name="page"></param>
        /// <returns>True if postback by Reject button</returns>
        private bool IsRejectingPostback(Page page)
        {
            bool isRejecting = false;
            Control control = null;
            string ctrlname = page.Request.Params.Get("__EVENTTARGET");
            if (ctrlname != null && ctrlname != string.Empty)
            {
                control = page.FindControl(ctrlname);
            }
            else
            {
                foreach (string ctl in page.Request.Form)
                {
                    Control mycontrol = page.FindControl(ctl);
                    if (mycontrol is System.Web.UI.WebControls.Button)
                    {
                        Button btn = (Button)mycontrol;
                        if (btn.Text.ToUpper().Contains("REJECT"))
                            isRejecting = true;
                        break;
                    }
                }
            }
            return isRejecting;
        }

        /// <summary>
        /// Gets if the postback process was launched by Reject button.
        /// </summary>
        /// <param name="page"></param>
        /// <returns>True if postback by Reject button</returns>
        private bool IsCancelingPostback(Page page)
        {
            bool isRejecting = false;
            Control control = null;
            string ctrlname = page.Request.Params.Get("__EVENTTARGET");
            if (ctrlname != null && ctrlname != string.Empty)
            {
                control = page.FindControl(ctrlname);
            }
            else
            {
                foreach (string ctl in page.Request.Form)
                {
                    Control mycontrol = page.FindControl(ctl);
                    if (mycontrol is System.Web.UI.WebControls.Button)
                    {
                        Button btn = (Button)mycontrol;
                        if (btn.Text.ToUpper().Contains("CANCEL"))
                            isRejecting = true;
                        break;
                    }
                }
            }
            return isRejecting;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="IsPageRefresh"></param>
        private void IsPageRefreshed(ref bool IsPageRefresh, string wfOrder)
        {
            try
            {
                if (!this.Page.IsPostBack)
                {
                    if (ViewState["ViewStateId" + wfOrder] == null)
                    {
                        ViewState["ViewStateId" + wfOrder] = System.Guid.NewGuid().ToString();

                        if ((HttpContext.Current.Session["SessionId" + wfOrder] != ViewState["ViewStateId" + wfOrder]) && (HttpContext.Current.Session["SessionId" + wfOrder] != null))
                            IsPageRefresh = true;
                        else
                        {
                            HttpContext.Current.Session["SessionId" + wfOrder] = ViewState["ViewStateId" + wfOrder].ToString();
                            IsPageRefresh = false;
                        }
                    }
                    else if (ViewState["ViewStateId" + wfOrder] != HttpContext.Current.Session["SessionId" + wfOrder])
                        IsPageRefresh = true;
                }
                else
                {
                    if (ViewState["ViewStateId" + wfOrder] != HttpContext.Current.Session["SessionId" + wfOrder])
                        IsPageRefresh = true;
                    else
                        IsPageRefresh = false;
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "IsPageRefreshed() " + ex.Message);
            }
        }


        /// <summary>
        /// Launch thread safe processes
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <param name="wfid"></param>
        /// <param name="currentStep"></param>
        /// <param name="status"></param>
        /// <param name="userAD"></param>
        /// <param name="passwordAD"></param>
        /// <param name="itemIsOld"></param>
        /// <param name="isRejecting"></param>
        /// <param name="itemExists"></param>
        /// <param name="stepResponsible"></param>
        /// <param name="loggedUser"></param>
        protected void BasicInitProcesses(SPListItem item, SPWeb Web, string wfid, string wftypeOrder, int currentStep, string status, string userAD, string passwordAD, bool itemIsOld, bool isRejecting, bool itemExists, SPUser stepResponsible, SPUser loggedUser, SPList logList, SPUser initiator, List<string> groupNames, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, int totalSteps, bool isSaving)
        {
            try
            {
                //TODO Before thread processing
                Dictionary<string, string> actorsModified = (Dictionary<string, string>)HttpContext.Current.Session["FormActorsModifiedDictionary" + wfid];
                string previousComment = string.Empty;
                    
                  if (currentStep.Equals(1) && status.ToUpper().Equals(parameters["Status Draft"].ToUpper()))
                     previousComment = WorkflowDataManagement.GetPreviousComment(Web, wfid, currentStep, logList, loggedUser);
              

                object comment = HttpContext.Current.Session["FormMyComment" + wfid];
                object formRefreshing = HttpContext.Current.Session["FormRefreshing" + wfid];
                System.Web.HttpBrowserCapabilities browser = HttpContext.Current.Request.Browser;

                string selectedConfidentiality = string.Empty;
                if (HttpContext.Current.Session["FormConfidentialModified" + wfid] != null)
                    selectedConfidentiality = HttpContext.Current.Session["FormConfidentialModified" + wfid].ToString();

                if ((formRefreshing != null) || generalFieldsSessionDictionary == null)
                    HttpContext.Current.Session["FormGeneralFieldsDictionary" + wfid] = null;

                string contextWebUrl = SPContext.Current.Web.Url;

                //Load general field controls and values
                string documentLibraryTitle = item.ParentList.Title;
                int wfItemID = item.ID;
                InititateGeneralFieldsArea(Web, item, contextWebUrl, wfid, wftypeOrder, itemExists, formRefreshing, documentLibraryTitle, wfItemID, selectedConfidentiality, generalFieldsSessionDictionary);

                //Load workflow actor lists
                string initialSteps = item["InitialSteps"] != null ? item["InitialSteps"].ToString() : string.Empty;


                InitiateActors(Web, item, contextWebUrl, wfid, currentStep, status, userAD, passwordAD, itemIsOld, actorsModified, stepResponsible, loggedUser, initialSteps, initiator, documentLibraryTitle, wfItemID, actorsBackupDictionary, reassignToBackupActor, isSaving);

                //Load comment controls
                string logListTitle = logList.Title;
                Thread threadComments = new Thread(() => InitiateComments(contextWebUrl, wfid, wftypeOrder, currentStep, comment, previousComment, groupNames, status, logListTitle));
                threadComments.Start();

                ////Load print document link
                InitiatePrintDocumentURL(Web, contextWebUrl, documentLibraryTitle, wfid, wftypeOrder);

                //Waiting for...
                threadComments.Join();

                //Load libraries
                InitiateLibraries(contextWebUrl, wfid.Trim(), documentLibraryTitle, isRejecting, loggedUser, this.Page, browser, parameters, wftypeOrder);

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "BasicInitProcesses" + ex.Message);
            }
        }

        /// <summary>
        /// Initiate basic session states
        /// </summary>
        /// <param name="Web"></param>
        protected void InitiateSessionStatesAndWFID(SPWeb Web, bool isRejecting, bool IsPageRefresh)
        {
            try
            {
                bool enc = false;
                string wfid = string.Empty;

                //Get workflow type from URL
                if (!string.IsNullOrEmpty(HttpContext.Current.Request.QueryString["wftype"]))
                {
                    HttpContext.Current.Session["FormWFType"] = HttpContext.Current.Request.QueryString["wftype"];
                    enc = true;
                }
                else
                    HttpContext.Current.Session["FormWFType"] = null;

                //CR26
                if (!string.IsNullOrEmpty(HttpContext.Current.Request.QueryString["wfnew"]))
                    HttpContext.Current.Session["FormWFNew"] = HttpContext.Current.Request.QueryString["wfnew"];
                else
                    HttpContext.Current.Session["FormWFNew"] = "";


                if (enc)
                {
                    string wftype = HttpContext.Current.Request.QueryString["wftype"];

                    IsPageRefreshed(ref IsPageRefresh, wftype);

                    //Get workflow ID from URL
                    if (!string.IsNullOrEmpty(HttpContext.Current.Request.QueryString["wfid"]))
                    {
                        wfid = HttpContext.Current.Request.QueryString["wfid"];
                        HttpContext.Current.Session["FormWFID"] = wfid;

                        // CR24
                        if (HttpContext.Current.Session["FormLinkToWorkFlowModified" + wfid] == null)
                            HttpContext.Current.Session["FormLinkToWorkFlowModified" + wfid] = "";
                      

                        HttpContext.Current.Session["FormUrlRequest" + wfid] = HttpContext.Current.Request.RawUrl.ToString();

                        if (!this.Page.IsPostBack)
                        {
                           

                            if (HttpContext.Current.Request.UrlReferrer != null && Uri.EscapeUriString(HttpContext.Current.Request.UrlReferrer.ToString()).ToLower().Contains(parameters["New Workflows Page"].ToString().ToLower()))
                            {
                                if (HttpContext.Current.Session["FormCreating" + wfid] == null)
                                    InititateControlKeys(wfid.ToString());

                                HttpContext.Current.Session["FormCreating" + wfid] = "Creating";

                            }
                            else if (!WorkflowDataManagement.DoesWorkflowExists(wfid, WorkflowDataManagement.GetWorkflowTypeName(wftype, Web), Web))
                                SPUtility.TransferToErrorPage("ACCESS DENIED. The current context URL has no parameters or its parameters are not the correct ones.");
                        }

                    }
                    else
                        // CR 24
                        HttpContext.Current.Session["FormLinkToWorkFlowModified" + wfid] = "";

                }

                //If it is not postback
                if (!this.Page.IsPostBack)
                {
                    HttpContext.Current.Session["FormReject" + HttpContext.Current.Session["FormWFID"].ToString()] = null;

                    if (HttpContext.Current.Request.UrlReferrer == null || !HttpContext.Current.Request.Url.ToString().Equals(HttpContext.Current.Request.UrlReferrer.ToString()))
                        HttpContext.Current.Session["FormStartTime" + HttpContext.Current.Session["FormWFID"].ToString()] = System.DateTime.UtcNow;

                }

                //Get previous page
                if (HttpContext.Current.Request.UrlReferrer != null && HttpContext.Current.Session["FormPrevURL" + HttpContext.Current.Session["FormWFID"].ToString()] == null)
                    HttpContext.Current.Session["FormPrevURL" + HttpContext.Current.Session["FormWFID"].ToString()] = HttpContext.Current.Request.UrlReferrer.ToString();

            }
            catch
            {
                SPUtility.TransferToErrorPage("ACCESS DENIED. The current context URL has no parameters or its parameters are not the correct ones.");
            }
        }

        /// <summary>
        /// Initiate, populate and pre select actor dropdownlist
        /// </summary>
        protected void InitiateActors(SPWeb Web, SPListItem item, string contextWebUrl, string wfid, int currentStep, string status, string userAD, string passwordAD, bool itemIsOld, object actorsModified, SPUser stepResponsible, SPUser loggedUser, string initialSteps, SPUser author, string listTitle, int itemID, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, bool isSaving)
        {
            try
            {
                List<SPUser> groupOwners = WorkflowDataManagement.GetStepOwners(Web, wftypeName, wfid);
                List<string> groupNames = WorkflowDataManagement.GetGroupNames(initialSteps, Web, wfid);
                ControlManagement.SetActorArea(item, wfid, currentStep, groupNames, status, wftypeName, actorsModified, Web, groupOwners, groupDDLs, groupLabels, DynamicUserListsPanel, DynamicRadioButtonListPanel, groupRadioButtons, parameters, userAD, passwordAD);
                SetActorAreaEvents(currentStep, stepResponsible, Web, wfid);
                ControlManagement.PreSelectActorLists(itemIsOld, ref DynamicUserListsPanel, item, loggedUser, Web, parameters, actorsModified, groupNames, wfid, userAD, passwordAD, currentStep, actorsBackupDictionary, reassignToBackupActor, isSaving);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "InitiateActors " + ex.Message);
            }
        }

        /// <summary>
        /// Initiate confidential control and different general fields controls
        /// </summary>
        /// <param name="wfid"></param>
        /// <param name="itemExists"></param>
        /// <param name="Web"></param>
        /// <param name="item"></param>
        protected void InititateGeneralFieldsArea(SPWeb Web, SPListItem item, string contextWebUrl, string wfid, string wftypeOrder, bool itemExists, object refreshing, string listTitle, int itemID, string selectedConfidentiality, Dictionary<string, string> generalFieldsSessionDictionary)
        {

            try
            {
                GeneralFields.LoadGeneralFields(wfid, Web, item, wftypeName, wftypeOrder, generalFieldsSessionDictionary, refreshing, itemExists, PlaceHolder_GFTable, parameters);
                ControlManagement.InitConfidentialDDL(wfid, ref ddlConfidential, item, Web, parameters, this.Page.IsPostBack, selectedConfidentiality);
                ControlManagement.InitLinkToWorkFlow(wfid, ref WFID_data, item, Web, parameters, this.Page.IsPostBack);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "InititateGeneralFieldsArea " + ex.Message);
            }
        }

        /// <summary>
        /// Initiate or load previous comments
        /// </summary>
        /// <param name="wfid"></param>
        /// <param name="Web"></param>
        /// <param name="item"></param>
        protected void InitiateComments(string contextWebUrl, string wfid, string wftypeOrder, int currentStep, object comment, string previousComment, List<string> groupNames, string status, string logListName)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(contextWebUrl))
                    {
                        SPWeb Web = Site.OpenWeb();

                        Comments.LoadComments(wfid, comment, previousComment, Web, status, PlaceHolder_PreviousComments, PlaceHolder_NewComments, btnAssign.ClientID, btnAssign2.ClientID, DynamicRadioButtonListPanel.ClientID, lblCommentRequired.ClientID, groupNames, parameters, wftypeOrder, wftypeName, currentStep, Web.Lists[logListName]);

                        Web.Close();
                        Web.Dispose();
                    }
                });
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "InitiateComments " + ex.Message);
            }
        }

        /// <summary>
        /// Initiate a session state with the responsibles before step signing process.
        /// </summary>
        protected void InititatePrevResponsibleDictionary(string wfid)
        {
            try
            {
                //Before
                //-------------------------------------------------------------------------------------------------
                if (!this.Page.IsPostBack)
                {
                    prevDictionary = ControlManagement.GetStepResponsibles(DynamicUserListsPanel, false, wfid);
                    HttpContext.Current.Session["FormPrevDictionary" + wfid] = prevDictionary;
                }
                else
                {
                    prevDictionary = (Dictionary<string, string>)HttpContext.Current.Session["FormPrevDictionary" + wfid];
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "InititatePrevResponsibleDictionary " + ex.Message);
            }
        }

        /// <summary>
        /// Initiate a session state with the responsibles before step signing process.
        /// </summary>
        protected void InititateGeneralFieldsDictionary(string wfid)
        {
            try
            {
                if (!this.Page.IsPostBack)
                {
                    if (HttpContext.Current.Session["FormGeneralFieldsDictionary" + wfid] != null)
                        generalFieldsSessionDictionary = (Dictionary<string, string>)HttpContext.Current.Session["FormGeneralFieldsDictionary" + wfid];
                    else
                        HttpContext.Current.Session["FormGeneralFieldsDictionary" + wfid] = null;
                }
                else
                    generalFieldsSessionDictionary = (Dictionary<string, string>)HttpContext.Current.Session["FormGeneralFieldsDictionary" + wfid];
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "InititateGeneralFieldsDictionary " + ex.Message);
            }
        }

        /// <summary>
        /// Initiate session state with the actors that have been modified during step signing
        /// </summary>
        protected void InititateActorsDictionary(string wfid, string stepNumber)
        {
            try
            {
                if (!this.Page.IsPostBack)
                {
                    if (HttpContext.Current.Session["FormActorsModifiedDictionary" + wfid] != null)
                        actorsDictionary = ControlManagement.GetStepResponsibles(DynamicUserListsPanel, true, wfid);
                    else
                        actorsDictionary = ControlManagement.GetStepResponsibles(DynamicUserListsPanel, false, wfid);

                    HttpContext.Current.Session["FormActorsDictionary" + wfid] = actorsDictionary;
                }
                else
                {
                    actorsDictionary = (Dictionary<string, string>)HttpContext.Current.Session["FormActorsDictionary" + wfid];

                    if (HttpContext.Current.Session["FormActorsModifiedDictionary" + wfid] != null)
                    {
                        foreach (KeyValuePair<String, String> entry in (Dictionary<string, string>)HttpContext.Current.Session["FormActorsModifiedDictionary" + wfid])
                        {
                            //if ((actorsDictionary.ContainsKey(entry.Key)) && (!(actorsDictionary[entry.Key].Equals(entry.Value))))
                            //    actorsDictionary[entry.Key] = entry.Value.ToString();

                            try
                            {
                                if (actorsDictionary.Count > 0)
                                {
                                    if (actorsDictionary.ContainsKey(entry.Key))
                                    {
                                        if ((!string.IsNullOrEmpty(entry.Value)) && (!(actorsDictionary[entry.Key].Equals(entry.Value))))
                                            actorsDictionary[entry.Key] = entry.Value;
                                        else if (string.IsNullOrEmpty(entry.Value))
                                            actorsDictionary[entry.Key] = string.Empty;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                General.saveErrorsLog(wfid, "[" + stepNumber + "] InititateActorsDictionary - Foreach()-> Key: '" + entry.Key + "'. Value: '" + entry.Value + "'. " + ex.Message);
                                General.saveErrorsLog(wfid, "[" + stepNumber + "] InititateActorsDictionary - Foreach()-> Key: '" + entry.Key + "'. actorsDictionary: '" + (actorsDictionary[entry.Key]) + "'. " + ex.Message);
                                continue;
                            }
                        }
                    }

                    HttpContext.Current.Session["FormActorsDictionary" + wfid] = actorsDictionary;
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "InititateActorsDictionary() " + ex.Message);
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        protected void InititateControlKeys(string wfid)
        {
            try
            {
                HttpContext.Current.Session["FormMyComment" + wfid] = string.Empty;
                HttpContext.Current.Session["FormActorsModifiedDictionary" + wfid] = null;
                HttpContext.Current.Session["FormConfidentialModified" + wfid] = null;
                // CR24
                HttpContext.Current.Session["FormLinkToWorkFlowModified" + wfid] = null;
                HttpContext.Current.Session["FormGeneralFieldsDictionary" + wfid] = null;
                HttpContext.Current.Session["PageSession" + wfid] = null;
                //ESMA-CR38
                HttpContext.Current.Session["ShowCloseWarningPopUp" + wfid] = null;

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "InititateControlKeys() " + ex.Message);
            }
        }

        /// <summary>
        /// Initiate the URL for the printed document
        /// </summary>
        /// <param name="urlPrintedDocument"></param>
        /// <param name="Web"></param>
        protected void InitiatePrintDocumentURL(SPWeb Web, string contextWebUrl, string docLibName, string wfid, string wfOrder)
        {
            try
            {
              
                HyperLinkPrint.NavigateUrl = WorkflowDataManagement.GetURLPrintedDocument(Web.Lists[docLibName], wfid, wftypeName, Web, wfOrder);
                ButtonArea.Controls.Add(HyperLinkPrint);
                
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "InitiatePrintDocumentURL " + ex.Message);
            }
        }

        /// <summary>
        /// Initiate list view web parts within workflow form interface
        /// </summary>
        /// <param name="wftypeName"></param>
        /// <param name="Web"></param>
        /// <param name="isRejecting"></param>
        /// <param name="loggedUser"></param>
        /// <param name="page"></param>
        protected void  InitiateLibraries(string contextWebUrl, string wfid, string docListTitle, bool isRejecting, SPUser loggedUser, Page page, HttpBrowserCapabilities browser, Dictionary<string, string> parameters, string wftypeOrder)
        {
            try
            {
                string strUrls = string.Empty;
                string strViews = string.Empty;

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(contextWebUrl))
                    {
                        SPWeb Web = Site.OpenWeb();
                        SPList docList = Web.Lists[docListTitle];

                        string orderQuery = "<OrderBy><FieldRef Name='FileLeafRef' Ascending='True' /></OrderBy>";

                        SPView viewMain = docList.Views["Main"];
                        viewMain.Query = orderQuery;
                        SPView viewSupporting = docList.Views["Supporting"];
                        viewSupporting.Query = orderQuery;
                        SPView viewPaper = docList.Views["To be signed on paper"];
                        viewPaper.Query = orderQuery;
                        SPView viewABAC = null;
                        SPView viewSigned = null;


                        //CR37 - Move docs between tans -> Documentation Type values updated
                        foreach (SPView view in docList.Views)
                        {
                            if (view.Title.Equals("To be signed in ABAC"))
                            {
                                viewABAC = docList.Views["To be signed in ABAC"];
                                break;
                            }
                        }
                       
                        if (viewABAC == null)
                            viewABAC = docList.Views["ABAC"];

                        
                        foreach (SPView view in docList.Views)
                        {
                            if (view.Title.Equals("Signed"))
                            {
                                viewSigned = docList.Views["Signed"];
                                break;
                            }
                        }

                        if (viewSigned == null)
                            viewSigned = docList.Views["Paper signed docs"];


                        viewABAC.Query = orderQuery;
                        viewSigned.Query = orderQuery;

                        ControlManagement.InitDocumentLibrary(wfid, "Main", "Main", ref DocsMain, ref DocsMainButtons, ref listViewWebPartMain, docList, loggedUser, viewMain, Web, ref btnDocsMainTab, ref DocumentArea, parameters, wftypeOrder, ref strUrls, ref strViews, Web.Url);
                        ControlManagement.InitDocumentLibrary(wfid, "To be signed in ABAC", "ABAC", ref DocsAbac, ref DocsAbacButtons, ref listViewWebPartAbac, docList, loggedUser, viewABAC, Web, ref btnDocsABACTab, ref DocumentArea, parameters, wftypeOrder, ref strUrls, ref strViews, Web.Url);
                        ControlManagement.InitDocumentLibrary(wfid, "Supporting", "Supporting", ref DocsSupporting, ref DocsSupportingButtons, ref listViewWebPartSupporting, docList, loggedUser, viewSupporting, Web, ref btnDocsSupportingTab, ref DocumentArea, parameters, wftypeOrder, ref strUrls, ref strViews, Web.Url);
                        ControlManagement.InitDocumentLibrary(wfid, "To be signed on paper", "Paper", ref DocsPaper, ref DocsPaperButtons, ref listViewWebPartPaper, docList, loggedUser, viewPaper, Web, ref btnDocsPaperTab, ref DocumentArea, parameters, wftypeOrder, ref strUrls, ref strViews, Web.Url);
                        ControlManagement.InitDocumentLibrary(wfid, "Paper signed docs", "Signed", ref DocsSigned, ref DocsSignedButtons, ref listViewWebPartSigned, docList, loggedUser, viewSigned, Web, ref btnDocsSignedTab, ref DocumentArea, parameters, wftypeOrder, ref strUrls, ref strViews, Web.Url);

                        this.Page.ClientScript.RegisterStartupScript(typeof(Page), "Callout", "callout('" + strUrls + "','" + Web.Url + "','" + strViews + "');", true);

                        Web.Close();
                        Web.Dispose();
                    }
                });
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "InitiateLibraries " + ex.Message);
            }
        }


        protected void SetActorAreaEvents(int currentStep, SPUser stepResponsible, SPWeb Web, string wfid)
        {
            try
            {
                int count = 1;

                foreach (Control ctrl in DynamicUserListsPanel.Controls)
                {
                    try
                    {
                        if (ctrl is UpdatePanel)
                        {
                            UpdatePanel updPanel = (UpdatePanel)ctrl;
                            DropDownList ddl = (DropDownList)updPanel.Controls[0].Controls[0];

                            if (ddl != null)
                            {
                                if (count.Equals(currentStep) && (stepResponsible != null))
                                {
                                    ddl.SelectedIndexChanged += new EventHandler(ddlGroup_SelectedIndexChanged);
                                    ddl.Attributes.Add("onchange", "showWaitingDialog('ReassigningActor', '" + RSInterface.ClientID + "', '" + currentStep + "');");
                                    //ddl.Attributes.Add("onclick", "showWaitingDialog('ReassigningActor', '" + RSInterface.ClientID + "', '" + currentStep + "');");
                                }
                                else
                                    ddl.SelectedIndexChanged += new EventHandler(ddlGroupNoCurrent_SelectedIndexChanged);
                            }

                            count++;
                        }
                    }
                    catch { continue; }
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetActorAreaEvents " + ex.Message);
            }
        }

        #endregion

        #region ButtonEvents

        /// <summary>
        /// Redirect user and remove Session states
        /// </summary>
        public void GoBack(string wfid, Dictionary<string, string> parameters)
        {
            try
            {
                if (!IsCancelingPostback(this.Page) && parameters.ContainsKey("New Workflows Page") && HttpContext.Current.Session["FormPrevURL" + wfid] != null && HttpContext.Current.Session["FormPrevURL" + wfid].ToString().ToUpper().Contains(parameters["New Workflows Page"].ToUpper()))
                {
                    int sessionCount = 0;
                    try
                    {
                        sessionCount = HttpContext.Current.Session.Count;
                        for (int i = 0; i < sessionCount; i++)
                        {
                            try
                            {
                                if (HttpContext.Current.Session.Keys[i].ToUpper().StartsWith("FORM")) HttpContext.Current.Session[i] = null;
                            }
                            catch
                            {
                                continue;
                            }
                        }
                        SPUtility.Redirect(SPContext.Current.Web.Url, SPRedirectFlags.DoNotEndResponse, HttpContext.Current);
                    }
                    catch
                    {


                    }
                }
                else if (HttpContext.Current.Session["FormPrevURL" + wfid] != null && !string.IsNullOrEmpty(HttpContext.Current.Session["FormPrevURL" + wfid].ToString()))
                {
                    try
                    {
                        string url = HttpContext.Current.Session["FormPrevURL" + wfid].ToString();
                        int sessionCount = HttpContext.Current.Session.Count;
                        for (int i = 0; i < sessionCount; i++) { try { if (HttpContext.Current.Session.Keys[i].ToUpper().StartsWith("FORM"))  HttpContext.Current.Session[i] = null; } catch { continue; } }
                        if (!HttpContext.Current.Request.Url.AbsoluteUri.ToUpper().Contains(url.ToUpper()))
                            SPUtility.Redirect(url, SPRedirectFlags.DoNotEndResponse, HttpContext.Current);
                        else
                            SPUtility.Redirect(SPContext.Current.Web.Url, SPRedirectFlags.DoNotEndResponse, HttpContext.Current);
                    }
                    catch
                    {

                    }
                }
                else
                {
                    try
                    {
                        int sessionCount = HttpContext.Current.Session.Count;
                        for (int i = 0; i < sessionCount; i++) { try { if (HttpContext.Current.Session.Keys[i].ToUpper().StartsWith("FORM"))  HttpContext.Current.Session[i] = null; } catch { continue; } }
                        SPUtility.Redirect(SPContext.Current.Web.Url, SPRedirectFlags.DoNotEndResponse, HttpContext.Current);
                    }
                    catch
                    {

                    }
                }
            }
            catch
            {
                //General.saveErrorsLog(string.Empty, "GoBack() - " + ex.Message);
            }
        }

        /// <summary>
        /// Initiate workflow form buttons and its names and events
        /// </summary>
        /// <param name="urlPrintedDocument"></param>
        public void InitiateButtonControls(string wfid)
        {
            try
            {
                btnSave.Click += new EventHandler(btnSave_Click);
                btnSave.Enabled = true;
                btnSave.Visible = true;
                ButtonArea.Controls.Add(btnSave);
                btnSave.Text = "Save";
                
                btnSign.Click += new EventHandler(btnSign_Click);
                btnSign.Enabled = true;
                btnSign.Visible = true;
                ButtonArea.Controls.Add(btnSign);
                btnSign.Text = "Sign";
                btnAssign.Click += new EventHandler(btnAssign_Click);
                btnAssign.Enabled = true;
                btnAssign.Visible = true;
                ButtonArea.Controls.Add(btnAssign);
                btnAssign.Text = "Assign";
                btnReject.Click += new EventHandler(btnReject_Click);
                btnReject.Enabled = true;
                btnReject.Visible = true;
                ButtonArea.Controls.Add(btnReject);
                btnReject.Text = "Reject";
                //ESMA-CR32
                btnOnHold.Click += new EventHandler(btnOnHold_Click);
                btnOnHold.Enabled = true;
                btnOnHold.Visible = true;
                ButtonArea.Controls.Add(btnOnHold);
                btnOnHold.Text = "On Hold";
                btnDelete.Click += new EventHandler(btnDelete_Click);
                btnDelete.Enabled = true;
                btnDelete.Visible = true;
                ButtonArea.Controls.Add(btnDelete);
                btnDelete.Text = "Delete";
                btnCancel.Click += new EventHandler(btnCancel_Click);
                btnCancel.Enabled = true;
                btnCancel.Visible = true;
                ButtonArea.Controls.Add(btnCancel);
                btnCancel.Text = "Cancel";
                btnClose.Click += new EventHandler(btnClose_Click);
                btnClose.Enabled = true;
                btnClose.Visible = true;
                ButtonArea.Controls.Add(btnClose);
                btnClose.Text = "Close";
                btnSave2.Click += new EventHandler(btnSave_Click);
                btnSave2.Enabled = true;
                btnSave2.Visible = true;
                ButtonArea2.Controls.Add(btnSave2);
                btnSave2.Text = "Save";
                
                btnSign2.Click += new EventHandler(btnSign_Click);
                btnSign2.Enabled = true;
                btnSign2.Visible = true;
                ButtonArea2.Controls.Add(btnSign2);
                btnSign2.Text = "Sign";
                btnAssign2.Click += new EventHandler(btnAssign_Click);
                btnAssign2.Enabled = true;
                btnAssign2.Visible = true;
                ButtonArea2.Controls.Add(btnAssign2);
                btnAssign2.Text = "Assign";
                btnReject2.Click += new EventHandler(btnReject_Click);
                btnReject2.Enabled = true;
                btnReject2.Visible = true;
                ButtonArea2.Controls.Add(btnReject2);
                btnReject2.Text = "Reject";
                //ESMA-CR32
                btnOnHold2.Click += new EventHandler(btnOnHold_Click);
                btnOnHold2.Enabled = true;
                btnOnHold2.Visible = true;
                ButtonArea2.Controls.Add(btnOnHold2);
                btnOnHold2.Text = "On Hold";
                btnDelete2.Click += new EventHandler(btnDelete_Click);
                btnDelete2.Enabled = true;
                btnDelete2.Visible = true;
                ButtonArea2.Controls.Add(btnDelete2);
                btnDelete2.Text = "Delete";
                btnCancel2.Click += new EventHandler(btnCancel_Click);
                btnCancel2.Enabled = true;
                btnCancel2.Visible = true;
                ButtonArea2.Controls.Add(btnCancel2);
                btnCancel2.Text = "Cancel";
                btnClose2.Click += new EventHandler(btnClose_Click);
                btnCancel2.Enabled = true;
                btnCancel2.Visible = true;
                ButtonArea2.Controls.Add(btnClose2);
                btnClose2.Enabled = true;
                btnClose2.Visible = true;
                btnClose2.Text = "Close";

                //CR27
                btnCloseWarning.Click += new ImageClickEventHandler(btnCloseWarning_Click);

                ////CR34
                string idWorkflow = HttpContext.Current.Session["FormWFID"].ToString();
                string link = HttpContext.Current.Server.UrlEncode(HttpContext.Current.Request.Url.AbsoluteUri);
                SPUser loggedUser = General.GetRealCurrentSPUser(this.Page);
               
                string mail = "mailto:?";
                mail += "subject=";
                mail += parameters.ContainsKey("E-mail Workflow Subject") ? parameters["E-mail Workflow Subject"] : "Paperless – A new workflow has been forwarded to you: ID <WF ID>";
                mail += "&body=";
                mail += parameters.ContainsKey("E-mail Workflow Text") ? parameters["E-mail Workflow Text"] : "[Username] has forwarded workflow [WF URL] to you.%0A%0A Please click on the workflow URL to go to Paperless and view the full details.";

                if (mail.Contains("[WF ID]"))
                    mail = mail.Replace("[WF ID]", idWorkflow);

                if (mail.Contains("[Username]"))
                    mail = mail.Replace("[Username]", loggedUser.Name);

                if (mail.Contains("[WF Link]"))
                {
                    if (!string.IsNullOrEmpty(link))
                        mail = mail.Replace("[WF Link]", "<a href='" + link + "'>" + idWorkflow + "</a>");
                    else
                        mail = mail.Replace("[WF Link]", idWorkflow);
                }

                //WFLink - Not HTML
                if (mail.Contains("[WF URL]"))
                {
                    if (!string.IsNullOrEmpty(link))
                        mail = mail.Replace("[WF URL]", link);
                    else
                        mail = mail.Replace("[WF URL]", idWorkflow);
                }
          

               
                HyperLinkEmail.Attributes["href"] = mail;


                InitiateTabs();
                InitiateExpandCollapse();

                //lblDocumentsCheckedOutWarning.Visible = false;
                //PanelCheckedOutWarning.Visible = false;

                // CR 24
                PanelLinkToWFWarning.Visible = false;
                // FIN CR 24

                lblCommentRequired.ForeColor = Color.Red;
                //lblRejectionUserSelected.ForeColor = Color.White;

                //CR23
                btnSaveClosedComments.Click += new EventHandler(btnSaveClosedComments_Click);
                //CR20
                btnSaveDeleteFile.Click += new EventHandler(btnSaveDeleteFile_Click);
                //ESMA-CR38-Close Pop up warning message
                btnCancelWarningCloseWF.Click += new EventHandler(btnCancelWarningCloseWF_Click);
                btnAcceptWarningCloseWF.Click += new EventHandler(btnAcceptWarningCloseWF_Click);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "InitializeButtonControls() - " + ex.Message);
            }
        }

        /// <summary>
        /// Comment retention
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void txtComments_TextChanged(object sender, EventArgs e)
        {
            try
            {
                TextBox txt = (TextBox)sender;
                string wfid = HttpContext.Current.Session["FormWFID"].ToString();
                Comments.RetainControlValueMyComments(txt.Text, wfid);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "txtComments_TextChanged - " + ex.Message);
            }
        }

        /// <summary>
        /// Initiate workflow documentation area tabs and their scripts.
        /// </summary>
        public void InitiateTabs()
        {
            try
            {
                btnDocsMainTab.Value = "Main ({0})";
                btnDocsABACTab.Value = "ABAC ({0})";
                btnDocsSupportingTab.Value = "Supporting ({0})";
                btnDocsPaperTab.Value = "To be signed on paper ({0})";
                btnDocsSignedTab.Value = "Paper signed docs ({0})";

                btnDocsMainTab.Attributes.Add("onclick", "toggleTabs('" + btnDocsMainTab.ClientID + "', '" + btnDocsABACTab.ClientID + "','" + btnDocsSupportingTab.ClientID + "','" + btnDocsPaperTab.ClientID + "','" + btnDocsSignedTab.ClientID + "','ViewMain','ViewABAC','ViewSupporting','ViewPaper','ViewSigned');");
                btnDocsMainTab.Attributes.Add("class", "Clicked");
                btnDocsABACTab.Attributes.Add("onclick", "toggleTabs('" + btnDocsABACTab.ClientID + "', '" + btnDocsMainTab.ClientID + "','" + btnDocsSupportingTab.ClientID + "','" + btnDocsPaperTab.ClientID + "','" + btnDocsSignedTab.ClientID + "','ViewABAC','ViewMain','ViewSupporting','ViewPaper','ViewSigned');");
                btnDocsABACTab.Attributes.Add("class", "Initial");
                btnDocsSupportingTab.Attributes.Add("onclick", "toggleTabs('" + btnDocsSupportingTab.ClientID + "', '" + btnDocsABACTab.ClientID + "','" + btnDocsMainTab.ClientID + "','" + btnDocsPaperTab.ClientID + "','" + btnDocsSignedTab.ClientID + "','ViewSupporting','ViewABAC','ViewMain','ViewPaper','ViewSigned');");
                btnDocsSupportingTab.Attributes.Add("class", "Initial");
                btnDocsPaperTab.Attributes.Add("onclick", "toggleTabs('" + btnDocsPaperTab.ClientID + "', '" + btnDocsABACTab.ClientID + "','" + btnDocsSupportingTab.ClientID + "','" + btnDocsMainTab.ClientID + "','" + btnDocsSignedTab.ClientID + "','ViewPaper','ViewABAC','ViewSupporting','ViewMain','ViewSigned');");
                btnDocsPaperTab.Attributes.Add("class", "Initial");
                btnDocsSignedTab.Attributes.Add("onclick", "toggleTabs('" + btnDocsSignedTab.ClientID + "', '" + btnDocsABACTab.ClientID + "','" + btnDocsSupportingTab.ClientID + "','" + btnDocsMainTab.ClientID + "','" + btnDocsPaperTab.ClientID + "','ViewSigned','ViewABAC','ViewSupporting','ViewMain','ViewPaper');");
                btnDocsSignedTab.Attributes.Add("class", "Initial");

                try
                {
                    string cookieValue = this.Page.Request.Cookies["RSSelectedTab"].Value;
                    if (cookieValue.ToUpper().Equals(btnDocsMainTab.ClientID.ToUpper()))
                        this.Page.ClientScript.RegisterStartupScript(typeof(Page), "Toggling", "toggleTabs('" + btnDocsMainTab.ClientID + "', '" + btnDocsABACTab.ClientID + "','" + btnDocsSupportingTab.ClientID + "','" + btnDocsPaperTab.ClientID + "','" + btnDocsSignedTab.ClientID + "','ViewMain','ViewABAC','ViewSupporting','ViewPaper','ViewSigned');", true);
                    else if (cookieValue.ToUpper().Equals(btnDocsABACTab.ClientID.ToUpper()))
                        this.Page.ClientScript.RegisterStartupScript(typeof(Page), "Toggling", "toggleTabs('" + btnDocsABACTab.ClientID + "', '" + btnDocsMainTab.ClientID + "','" + btnDocsSupportingTab.ClientID + "','" + btnDocsPaperTab.ClientID + "','" + btnDocsSignedTab.ClientID + "','ViewABAC','ViewMain','ViewSupporting','ViewPaper','ViewSigned');", true);
                    else if (cookieValue.ToUpper().Equals(btnDocsSupportingTab.ClientID.ToUpper()))
                        this.Page.ClientScript.RegisterStartupScript(typeof(Page), "Toggling", "toggleTabs('" + btnDocsSupportingTab.ClientID + "', '" + btnDocsABACTab.ClientID + "','" + btnDocsMainTab.ClientID + "','" + btnDocsPaperTab.ClientID + "','" + btnDocsSignedTab.ClientID + "','ViewSupporting','ViewABAC','ViewMain','ViewPaper','ViewSigned');", true);
                    else if (cookieValue.ToUpper().Equals(btnDocsPaperTab.ClientID.ToUpper()))
                        this.Page.ClientScript.RegisterStartupScript(typeof(Page), "Toggling", "toggleTabs('" + btnDocsPaperTab.ClientID + "', '" + btnDocsABACTab.ClientID + "','" + btnDocsSupportingTab.ClientID + "','" + btnDocsMainTab.ClientID + "','" + btnDocsSignedTab.ClientID + "','ViewPaper','ViewABAC','ViewSupporting','ViewMain','ViewSigned');", true);
                    else if (cookieValue.ToUpper().Equals(btnDocsSignedTab.ClientID.ToUpper()))
                        this.Page.ClientScript.RegisterStartupScript(typeof(Page), "Toggling", "toggleTabs('" + btnDocsSignedTab.ClientID + "', '" + btnDocsABACTab.ClientID + "','" + btnDocsSupportingTab.ClientID + "','" + btnDocsMainTab.ClientID + "','" + btnDocsPaperTab.ClientID + "','ViewSigned','ViewABAC','ViewSupporting','ViewMain','ViewPaper');", true);
                    else
                        this.Page.ClientScript.RegisterStartupScript(typeof(Page), "Toggling", "toggleTabs('" + btnDocsMainTab.ClientID + "', '" + btnDocsABACTab.ClientID + "','" + btnDocsSupportingTab.ClientID + "','" + btnDocsPaperTab.ClientID + "','" + btnDocsSignedTab.ClientID + "','ViewMain','ViewABAC','ViewSupporting','ViewPaper','ViewSigned');", true);
                }
                catch
                {
                    this.Page.ClientScript.RegisterStartupScript(typeof(Page), "Toggling", "toggleTabs('" + btnDocsMainTab.ClientID + "', '" + btnDocsABACTab.ClientID + "','" + btnDocsSupportingTab.ClientID + "','" + btnDocsPaperTab.ClientID + "','" + btnDocsSignedTab.ClientID + "','ViewMain','ViewABAC','ViewSupporting','ViewPaper','ViewSigned');", true);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "InitiateTabs() - " + ex.Message);
            }
        }


        /// <summary>
        /// Initiate workflow expand collapse behaviour.
        /// </summary>
        public void InitiateExpandCollapse()
        {
            try
            {
                this.Page.ClientScript.RegisterStartupScript(typeof(Page), "TogglingAreas", "toggleAreasOnLoad('StepDescription','StepDescriptionTitle','StepImage','title_blue_step_description2','title_blue_step_description','NONE');", true);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "InitiateExpandCollapse() - " + ex.Message);
            }
        }

        /// <summary>
        /// Cancel button events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void btnCancel_Click(object sender, EventArgs e)
        {
            string wfid = string.Empty;

            try
            {
                bool fromRejecting = false;
                bool saveLog = false;


                if (HttpContext.Current.Session["FormWFType"] != null && HttpContext.Current.Session["FormWFID"] != null)
                {
                    string wftypeOrder = HttpContext.Current.Session["FormWFType"].ToString();
                    wfid = HttpContext.Current.Session["FormWFID"].ToString();

                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                        {
                            SPWeb Web = Site.OpenWeb();
                            Site.AllowUnsafeUpdates = true;
                            Web.AllowUnsafeUpdates = true;

                            SPUser loggedUser = General.GetRealCurrentSPUser(this.Page);
                            SPListItem item = WorkflowDataManagement.GetWorkflowItem(wfid, wftypeName, Web);
                            SPList logList = WorkflowDataManagement.GetWorkflowLogList(wftypeName, Web);
                            int currentStep = WorkflowDataManagement.GetWorkflowCurrentStep(item, Web, wfid);
                            string computerName = General.GetComputerName(HttpContext.Current);
                            DateTime startTime = (DateTime)HttpContext.Current.Session["FormStartTime" + wfid];
                            string status = WorkflowDataManagement.GetWorkflowStatus(item, Web, parameters, wfid);
                            HttpContext.Current.Session["SessionId"] = null;
                            bool isRejected = WorkflowDataManagement.IsRejectedWF(wfid, logList);
                            string userAD = General.Decrypt(parameters["AD User"]);
                            string passwordAD = General.Decrypt(parameters["AD Password"]);
                            string domain = parameters["Domain"];
                            //ESMA-CR31-Backup Responsibles
                            Dictionary<string, string> actorsBackupDictionary = ControlManagement.GetStepBackupResponsibles(item, wfid, Web);

                            if (HttpContext.Current.Session["FormReject" + wfid] != null && HttpContext.Current.Session["FormReject" + wfid].ToString().ToUpper().Equals("REJECTING") && parameters.ContainsKey("Status In Progress"))
                            {
                                saveLog = true;

                                RejectDocumentChanges(item, wfid, Web, loggedUser, currentStep, logList, Site);
                                RejectWorkflowLog(wfid, Web, loggedUser, logList, currentStep,item);
                                CancelRejection(item, ref fromRejecting, wfid, wftypeOrder, Web, loggedUser, actorsBackupDictionary, domain, userAD, passwordAD);
                            }
                            else if (HttpContext.Current.Session["FormCreating" + wfid] != null && HttpContext.Current.Session["FormCreating" + wfid].ToString().ToUpper().Equals("CREATING") && status.ToLower().Equals(parameters["Status Draft"]))
                            {
                                WorkflowDataManagement.DeleteAllLogsByWFID(wfid, Web, wftypeName, logList);
                                WorkflowDataManagement.RemoveWorkflowOnCreation(item, item.ParentList, Web, parameters, wfid, status);
                                WorkflowDataManagement.RemoveWorkflowHistoryOnCreation(item, item.ParentList, Web, parameters, wfid, status);
                            }
                            else if (HttpContext.Current.Session["FormStartTime" + wfid] != null && (HttpContext.Current.Session["FormStartTime" + wfid] is DateTime))
                            {
                                saveLog = true;
                                RejectDocumentChanges(item, wfid, Web, loggedUser, currentStep, logList, Site);
                                RejectWorkflowLog(wfid, Web, loggedUser, logList, currentStep, item);
                               
                            }


                            //Log taken action
                            if (saveLog.Equals(true))
                                WorkflowDataManagement.CreateWorkflowLog(wftypeOrder, wfid, currentStep, item["WFStatus"].ToString(), loggedUser, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Cancelled.ToString()), string.Empty, computerName, string.Empty, ddlConfidential.SelectedValue, logList, Web, parameters, loggedUser, true);

                            HttpContext.Current.Session["FormStartTime" + wfid] = DateTime.UtcNow;

                            Web.AllowUnsafeUpdates = false;
                            Site.AllowUnsafeUpdates = false;
                            Web.Close();
                            Web.Dispose();
                        }
                    });

                    if (!fromRejecting && HttpContext.Current.Session["FormWFID"] != null)
                        GoBack(HttpContext.Current.Session["FormWFID"].ToString(), parameters);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "btnCancel() - Redirect" + ex.Message);

            }
        }

        /// <summary>
        /// Save button events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void btnSave_Click(object sender, EventArgs e)
        {
            string wfid = string.Empty;
            bool isReassigment = false;

            try
            {

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                    {
                        SPWeb Web = Site.OpenWeb();
                        Web.AllowUnsafeUpdates = true;

                        SPUser loggedUser = General.GetRealCurrentSPUser(this.Page);

                        if (HttpContext.Current.Session["FormWFType"] != null && HttpContext.Current.Session["FormWFID"] != null && loggedUser != null)
                        {
                            bool isSaving = true;
                            bool isReassigning = false;
                            string computerName = General.GetComputerName(HttpContext.Current);
                            wfid = HttpContext.Current.Session["FormWFID"].ToString();
                            string wftypeOrder = HttpContext.Current.Session["FormWFType"].ToString();
                            HttpContext.Current.Session["SessionId"] = null;

                            SPListItem item = WorkflowDataManagement.GetWorkflowItem(wfid, wftypeName, Web);
                            SPList logList = WorkflowDataManagement.GetWorkflowLogList(wftypeName, Web);

                            int prevStep = WorkflowDataManagement.GetWorkflowCurrentStep(item, Web, wfid);
                            int currentStep = prevStep;
                            string prevConfidential = WorkflowDataManagement.GetWorkflowConfidentialValue(item, Web);
                            string prevLinkToWorkFlow = WFID_data.Value; //WorkflowDataManagement.GetWorkflowLinktoWorkFlowValue(item, Web);
                            string confidential = prevConfidential;
                            bool reassignToBackupActor = false; //ESMA-CR31-BackupGroups


                            string userAD = General.Decrypt(parameters["AD User"]);
                            string passwordAD = General.Decrypt(parameters["AD Password"]);
                            string domain = parameters["Domain"];

                            SPUser responsible = ControlManagement.GetStepResponsible(currentStep, Web, DynamicUserListsPanel, parameters, item, wfid, userAD, passwordAD);
                            string currentStatus = WorkflowDataManagement.GetWorkflowStatus(item, Web, parameters, wfid);

                            if (responsible != null || (currentStatus.ToUpper().Equals(parameters["Status Closed"].ToUpper())))
                            {

                                //ESMA-CR31-Backup Group
                                Dictionary<string, string> actorsBackupDictionary = ControlManagement.GetStepBackupResponsibles(item, wfid, Web);


                                //Change workflow for next step and if next step does not exist, close workflow.
                                JumpToNextStep(wfid, prevStep, wftypeName, Web, ref item, loggedUser, ref currentStep, ref currentStatus, ref confidential, ref responsible, true, userAD, passwordAD, prevLinkToWorkFlow, actorsBackupDictionary, reassignToBackupActor, wftypeOrder, logList, isReassigment);

                                #region Log


                                //Log confidentiality change
                                WorkflowDataManagement.LogConfidentialityChanges(item, logList, wfid, wftypeOrder, currentStatus, prevStep, computerName, prevConfidential, confidential, responsible, loggedUser, parameters, Web);

                                //Log actor re-assignement
                                LogActorChanging(wfid, wftypeOrder, currentStatus, wftypeName, prevStep, logList, loggedUser, loggedUser, Comments.GetMyComment(PlaceHolder_NewComments), Web, computerName);

                                //Log taken action
                                WorkflowDataManagement.CreateWorkflowLog(wftypeOrder, wfid, prevStep, currentStatus, responsible, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Saved.ToString()), string.Empty, computerName, string.Empty, ddlConfidential.SelectedValue, logList, Web, parameters, loggedUser, true);

                                //Log comment (Updating of the comment when the WF is Draft)
                                #region <Log comment>

                                SPListItem logItem = null;

                                if (currentStatus.ToUpper().Equals(parameters["Status Draft"].ToUpper()))
                                {
                                    logItem = WorkflowDataManagement.GetPreviousCommentObject(Web, wfid, prevStep, logList, loggedUser);

                                    if (logItem != null)
                                        WorkflowDataManagement.SetComment(logItem, false, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Saved.ToString()), Comments.GetMyComment(PlaceHolder_NewComments), wfid, responsible);
                                    else
                                        WorkflowDataManagement.CreateWorkflowLog(wftypeOrder, wfid, currentStep, currentStatus, responsible, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Commented.ToString()), WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Saved.ToString()), computerName, Comments.GetMyComment(PlaceHolder_NewComments), ddlConfidential.SelectedValue, logList, Web, parameters, loggedUser, !currentStatus.ToUpper().Equals(parameters["Status Draft"].ToUpper()));
                                }
                                else
                                    WorkflowDataManagement.CreateWorkflowLog(wftypeOrder, wfid, currentStep, currentStatus, responsible, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Commented.ToString()), WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Saved.ToString()), computerName, Comments.GetMyComment(PlaceHolder_NewComments), ddlConfidential.SelectedValue, logList, Web, parameters, loggedUser, !currentStatus.ToUpper().Equals(parameters["Status Draft"].ToUpper()));

                                #endregion

                                SPUser initiator = General.GetAuthor(wfid, item, Web);
                         


                                //Create reference in workflow histoy
                                if (currentStatus.ToUpper().Equals(parameters["Status Deleted"].ToUpper()) || currentStatus.ToUpper().Equals(parameters["Status Closed"].ToUpper()))
                                    WorkflowDataManagement.CreateAndSetWorkflowHistory(item, wfid, wftypeName, wftypeOrder, null, initiator, loggedUser, Web, currentStatus, GeneralFields.GetValue_CheckBox(parameters["GeneralColumn_1"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralAmount"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralColumn_2"], PlaceHolder_GFTable), GeneralFields.GetValue_DateTime(parameters["GeneralDeadline"], PlaceHolder_GFTable), ddlConfidential.SelectedValue, parameters, currentStep, actorsBackupDictionary, reassignToBackupActor, isSaving, isReassigning, currentStep);
                                else
                                    WorkflowDataManagement.CreateAndSetWorkflowHistory(item, wfid, wftypeName, wftypeOrder, responsible, initiator, loggedUser, Web, currentStatus, GeneralFields.GetValue_CheckBox(parameters["GeneralColumn_1"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralAmount"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralColumn_2"], PlaceHolder_GFTable), GeneralFields.GetValue_DateTime(parameters["GeneralDeadline"], PlaceHolder_GFTable), ddlConfidential.SelectedValue, parameters, currentStep, actorsBackupDictionary, reassignToBackupActor, isSaving, isReassigning, currentStep);

                                #endregion

                                //Save general fields
                                GeneralFields.SaveGeneralFields(wfid, wftypeOrder, wftypeName, item, Web, loggedUser, generalFieldsSessionDictionary, PlaceHolder_GFTable, parameters);

                                //CR23 - Closed Comments
                                if (currentStatus.ToUpper().Equals(parameters["Status Closed"].ToUpper()))
                                    btnSaveClosedComments_Click(sender, e);

                            }
                            else
                            {
                                General.saveErrorsLog(wfid, "btnSave - Logged User: " + loggedUser.LoginName + "[" + loggedUser.ID + "]");
                                General.saveErrorsLog(wfid, "btnSave  - Responsible: " + responsible.LoginName + "[" + responsible.ID + "]");
                                SPUtility.TransferToErrorPage("ACCESS DENIED. This action cannot be performed."); 
                            }
                            

                            HttpContext.Current.Session["FormStartTime" + HttpContext.Current.Session["FormWFID"].ToString()] = System.DateTime.UtcNow;

                        }



                        Web.AllowUnsafeUpdates = false;
                        Web.Close();
                        Web.Dispose();
                    }
                });

                //if(correct)
                //{
                if (HttpContext.Current.Session["FormWFID"] != null)
                    GoBack(HttpContext.Current.Session["FormWFID"].ToString(), parameters);

                //}
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "btnSave()" + ex.Message);

                if (HttpContext.Current.Session["FormWFID"] != null)
                    GoBack(HttpContext.Current.Session["FormWFID"].ToString(), parameters);
            }

        }

        /// <summary>
        /// On Hold button events (ESMA-CR32)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void btnOnHold_Click(object sender, EventArgs e)
        {
            string wfid = string.Empty;
            bool isReassigment = false;

            try
            {

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                    {
                        SPWeb Web = Site.OpenWeb();
                        Web.AllowUnsafeUpdates = true;

                        SPUser loggedUser = General.GetRealCurrentSPUser(this.Page);

                        if (HttpContext.Current.Session["FormWFType"] != null && HttpContext.Current.Session["FormWFID"] != null && loggedUser != null)
                        {
                            bool isSaving = true;
                            bool isReassigning = false;
                            bool reassignToBackupActor = false; //ESMA-CR31-BackupGroups
                            bool isBackupResponsible = false;
                            bool isBackupInitiator = false;
                            string computerName = General.GetComputerName(HttpContext.Current);
                            wfid = HttpContext.Current.Session["FormWFID"].ToString();
                            string wftypeOrder = HttpContext.Current.Session["FormWFType"].ToString();
                            HttpContext.Current.Session["SessionId"] = null;
                            string userAD = General.Decrypt(parameters["AD User"]);
                            string passwordAD = General.Decrypt(parameters["AD Password"]);
                            string domain = parameters["Domain"];

                            SPListItem item = WorkflowDataManagement.GetWorkflowItem(wfid, wftypeName, Web);
                            int prevStep = WorkflowDataManagement.GetWorkflowCurrentStep(item, Web, wfid);
                            int currentStep = prevStep;
                            string currentStatus = parameters["Status On Hold"];
                            string prevConfidential = WorkflowDataManagement.GetWorkflowConfidentialValue(item, Web);
                            string prevLinkToWorkFlow = WFID_data.Value; //WorkflowDataManagement.GetWorkflowLinktoWorkFlowValue(item, Web);
                            string confidential = prevConfidential;
                            SPUser responsible = ControlManagement.GetStepResponsible(currentStep, Web, DynamicUserListsPanel, parameters, item, wfid, userAD, passwordAD);
                            SPList logList = WorkflowDataManagement.GetWorkflowLogList(wftypeName, Web);


                                //ESMA-CR31-Backup Group
                                Dictionary<string, string> actorsBackupDictionary = ControlManagement.GetStepBackupResponsibles(item, wfid, Web);
                                bool isBackupMember = WorkflowDataManagement.IsMemberOfBackupGroup(loggedUser, wfid, domain, actorsBackupDictionary, userAD, passwordAD, ref isBackupInitiator, ref isBackupResponsible, Convert.ToString(currentStep), parameters);
                                if (isBackupMember)
                                    reassignToBackupActor = true;

                                if ((responsible != null && responsible.ID.Equals(loggedUser.ID)) || isBackupResponsible)
                                {

                                    //Change workflow for next step and if next step does not exist, close workflow.
                                    JumpToNextStep(wfid, prevStep, wftypeName, Web, ref item, loggedUser, ref currentStep, ref currentStatus, ref confidential, ref responsible, true, userAD, passwordAD, prevLinkToWorkFlow, actorsBackupDictionary, reassignToBackupActor, wftypeOrder, logList, isReassigment);


                                    #region <Log>

                                    //Log confidentiality change
                                    WorkflowDataManagement.LogConfidentialityChanges(item, logList, wfid, wftypeOrder, currentStatus, prevStep, computerName, prevConfidential, confidential, responsible, loggedUser, parameters, Web);

                                    //Log actor re-assignement
                                    LogActorChanging(wfid, wftypeOrder, currentStatus, wftypeName, prevStep, logList, loggedUser, loggedUser, Comments.GetMyComment(PlaceHolder_NewComments), Web, computerName);

                                    //Log taken action
                                    WorkflowDataManagement.CreateWorkflowLog(wftypeOrder, wfid, prevStep, currentStatus, responsible, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.OnHold.ToString()), string.Empty, computerName, string.Empty, ddlConfidential.SelectedValue, logList, Web, parameters, loggedUser, true);

                                    //Log comment
                                    WorkflowDataManagement.CreateWorkflowLog(wftypeOrder, wfid, currentStep, currentStatus, responsible, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Commented.ToString()), WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.OnHold.ToString()), computerName, Comments.GetMyComment(PlaceHolder_NewComments), ddlConfidential.SelectedValue, logList, Web, parameters, loggedUser, !currentStatus.ToUpper().Equals(parameters["Status Draft"].ToUpper()));

                                    #endregion

                                    SPUser initiator = General.GetAuthor(wfid, item, Web);

                                    //Create reference in workflow histoy
                                    if (currentStatus.ToUpper().Equals(parameters["Status Deleted"].ToUpper()) || currentStatus.ToUpper().Equals(parameters["Status Closed"].ToUpper()))
                                        WorkflowDataManagement.CreateAndSetWorkflowHistory(item, wfid, wftypeName, wftypeOrder, null, initiator, loggedUser, Web, currentStatus, GeneralFields.GetValue_CheckBox(parameters["GeneralColumn_1"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralAmount"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralColumn_2"], PlaceHolder_GFTable), GeneralFields.GetValue_DateTime(parameters["GeneralDeadline"], PlaceHolder_GFTable), ddlConfidential.SelectedValue, parameters, currentStep, actorsBackupDictionary, reassignToBackupActor, isSaving, isReassigning, currentStep);
                                    else
                                        WorkflowDataManagement.CreateAndSetWorkflowHistory(item, wfid, wftypeName, wftypeOrder, responsible, initiator, loggedUser, Web, currentStatus, GeneralFields.GetValue_CheckBox(parameters["GeneralColumn_1"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralAmount"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralColumn_2"], PlaceHolder_GFTable), GeneralFields.GetValue_DateTime(parameters["GeneralDeadline"], PlaceHolder_GFTable), ddlConfidential.SelectedValue, parameters, currentStep, actorsBackupDictionary, reassignToBackupActor, isSaving, isReassigning, currentStep);

                                    

                                    //Save general fields
                                    GeneralFields.SaveGeneralFields(wfid, wftypeOrder, wftypeName, item, Web, loggedUser, generalFieldsSessionDictionary, PlaceHolder_GFTable, parameters);
                                }
                                else
                                {
                                    SPUtility.TransferToErrorPage("ACCESS DENIED. This action cannot be performed.");
                                    General.saveErrorsLog(wfid, "btnOnHold - Logged User: " + loggedUser.LoginName + "[" + loggedUser.ID + "]");
                                    General.saveErrorsLog(wfid, "btnOnHold  - Responsible: " + responsible.LoginName + "[" + responsible.ID + "]");
                                }
                                
                            

                            HttpContext.Current.Session["FormStartTime" + HttpContext.Current.Session["FormWFID"].ToString()] = System.DateTime.UtcNow;

                        }


                        Web.AllowUnsafeUpdates = false;
                        Web.Close();
                        Web.Dispose();
                    }
                });

                //if(correct)
                //{
                if (HttpContext.Current.Session["FormWFID"] != null)
                    GoBack(HttpContext.Current.Session["FormWFID"].ToString(), parameters);

                //}
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "btnOnHold_Click()" + ex.Message);
            }

        }

        /// <summary>
        /// CR20
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void btnSaveDeleteFile_Click(object sender, EventArgs e)
        {
            try
            {
                string strCommentDeleteFile = Comments.GetMyCommentDeletedFile(TextBoxCommentsDeletedFile);

                if (!string.IsNullOrEmpty(strCommentDeleteFile))
                {
                    this.lblDeleteFileMandatory.Visible = false;

                    //Record comments
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                        {
                            SPWeb Web = Site.OpenWeb();
                            Web.AllowUnsafeUpdates = true;

                                SPUser loggedUser = General.GetRealCurrentSPUser(this.Page);

                                if (HttpContext.Current.Session["FormWFType"] != null && HttpContext.Current.Session["FormWFID"] != null && loggedUser != null)
                                {

                                    string wfid = HttpContext.Current.Session["FormWFID"].ToString();
                                    HttpContext.Current.Session["SessionId"] = null;

                                    SPListItem item = WorkflowDataManagement.GetWorkflowItem(wfid, wftypeName, Web);
                                    SPList logList = WorkflowDataManagement.GetWorkflowLogList(wftypeName, Web);
                                    string currentStep = "1";

                                    if (item["StepNumber"] != null)
                                        currentStep = item["StepNumber"].ToString();

                                    string actionTakenDeletedFile = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.DocumentRemoved.ToString());

                                    //Save Comment
                                    WorkflowDataManagement.SetCommentDeleteFile(Web, wfid, logList, currentStep, loggedUser, strCommentDeleteFile, actionTakenDeletedFile);

                                    panel_DeleteFile.Visible = false;
                                    RefreshPage();

                                }
                            
                            
                           
                            Web.AllowUnsafeUpdates = false;
                            Web.Close();
                            Web.Dispose();
                        }

                    });

                    RSInterface.Enabled = true;
                }
                else
                    this.lblDeleteFileMandatory.Visible = true;
                

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "btnSaveDeleteFile_Click() - " + ex.Message);
            }
        }

        //ESMA-CR38-Close Warning Message
        public void btnAcceptWarningCloseWF_Click(object sender, EventArgs e)
        {
            string wfid = string.Empty;

            try
            {
                wfid = HttpContext.Current.Session["FormWFID"].ToString();
                SignAction("btnAcceptWarningCloseWF_Click", false);
                HidePanelWarningCloseWorkflow(wfid);
             
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "btnAcceptWarningCloseWF() - Signing:" + ex.Message);
                SPUtility.TransferToErrorPage("ACCESS DENIED. This action cannot be performed.");
            }
        }

        public void btnCancelWarningCloseWF_Click(object sender, EventArgs e)
        {
            string wfid = string.Empty;

            try
            {
                wfid = HttpContext.Current.Session["FormWFID"].ToString();
                HidePanelWarningCloseWorkflow(wfid);
                HttpContext.Current.Session["ShowCloseWarningPopUp" + wfid] = "false";
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "btnCancelWarningCloseWF_Click - " + ex.Message);
            }

        }

        /// <summary>
        /// CR23 Save commented closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void btnSaveClosedComments_Click(object sender, EventArgs e)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                    {
                        SPWeb Web = Site.OpenWeb();
                        Web.AllowUnsafeUpdates = true;

                        if (!string.IsNullOrEmpty(TextBoxNewCommentsClosed.Text))
                        {

                            SPUser loggedUser = General.GetRealCurrentSPUser(this.Page);

                            if (HttpContext.Current.Session["FormWFType"] != null && HttpContext.Current.Session["FormWFID"] != null && loggedUser != null)
                            {
                                string computerName = General.GetComputerName(HttpContext.Current);
                                string wfid = HttpContext.Current.Session["FormWFID"].ToString();
                                string wftypeOrder = HttpContext.Current.Session["FormWFType"].ToString();
                                HttpContext.Current.Session["SessionId"] = null;

                                SPListItem item = WorkflowDataManagement.GetWorkflowItem(wfid, wftypeName, Web);

                                int prevStep = WorkflowDataManagement.GetWorkflowCurrentStep(item, Web, wfid);
                                int currentStep = prevStep;


                                SPList logList = WorkflowDataManagement.GetWorkflowLogList(wftypeName, Web);

                                WorkflowDataManagement.SetCommentClosed(Web, logList, wfid, wftypeOrder, loggedUser, ddlConfidential.SelectedValue, "Closed", computerName, WorkflowDataManagement.ActionsEnum.CommentedClosed.ToString(), currentStep, Comments.GetMyCommentClosed(TextBoxNewCommentsClosed), parameters);

                                TextBoxCommentsClosed.Text = TextBoxCommentsClosed.Text + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + " - <b> " + loggedUser.Name + " </b> - " + Comments.GetMyCommentClosed(TextBoxNewCommentsClosed);
                                TextBoxNewCommentsClosed.Text = "";

                            }

                        }
                        Web.AllowUnsafeUpdates = false;
                        Web.Close();
                        Web.Dispose();
                    }
                });


            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "btnSaveClosedComments_Click() - " + ex.Message);
            }
        }

        /// <summary>
        /// Sign button actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void btnSign_Click(object sender, EventArgs e)
        {
           string wfid = string.Empty;

           try
           {
               wfid = HttpContext.Current.Session["FormWFID"].ToString();
               SignAction("btnSign_Click", true);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "btnSign()" + ex.Message);
                SPUtility.TransferToErrorPage("ACCESS DENIED. This action cannot be performed.");
            }
        }

        public void SignAction(string btnName, bool displayMessage)
        {
            bool ConcurrentError = false;
            string wfid = string.Empty;
            bool sendEmail = false;
            bool isReassigment = false;

            try
            {

                SPUser responsible = null;
                SPUser loggedUser = General.GetRealCurrentSPUser(this.Page);
                wfid = HttpContext.Current.Session["FormWFID"].ToString();

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                    {
                        SPWeb Web = Site.OpenWeb();
                        Web.AllowUnsafeUpdates = true;

                        if (HttpContext.Current.Session["FormWFType"] != null && HttpContext.Current.Session["FormWFID"] != null && loggedUser != null)
                        {
                            bool isSaving = false;
                            bool isReassigning = false;
                            bool reassignToBackupActor = false; //ESMA-CR31-BackupGroups
                            bool isBackupResponsible = false;
                            bool isBackupInitiator = false;
                            string userAD = General.Decrypt(parameters["AD User"]);
                            string passwordAD = General.Decrypt(parameters["AD Password"]);
                            string domain = parameters["Domain"];
                            string computerName = General.GetComputerName(HttpContext.Current);
                            string wftypeOrder = HttpContext.Current.Session["FormWFType"].ToString();
                            SPListItem item = WorkflowDataManagement.GetWorkflowItem(wfid, wftypeName, Web);
                            responsible = WorkflowDataManagement.GetWorkflowCurrentStepResponsible(item, Web, wfid, domain);

                            SPList logList = WorkflowDataManagement.GetWorkflowLogList(wftypeName, Web);
                            int prevStep = WorkflowDataManagement.GetWorkflowCurrentStep(item, Web, wfid);
                            int currentStep = prevStep;

                            //ESMA-CR31-Backup Group
                            Dictionary<string, string> actorsBackupDictionary = ControlManagement.GetStepBackupResponsibles(item, wfid, Web);
                            bool isBackupMember = WorkflowDataManagement.IsMemberOfBackupGroup(loggedUser, wfid, domain, actorsBackupDictionary, userAD, passwordAD, ref isBackupInitiator, ref isBackupResponsible, Convert.ToString(currentStep), parameters);
                            if (isBackupMember)
                                reassignToBackupActor = true;


                            if ((responsible != null && responsible.ID.Equals(loggedUser.ID)) || isBackupResponsible)
                            {

                                string currentStatus = WorkflowDataManagement.GetWorkflowStatus(item, Web, parameters, wfid);
                                string prevStatus = currentStatus;
                                string prevConfidential = WorkflowDataManagement.GetWorkflowConfidentialValue(item, Web);
                                string prevLinktoworkflow = WorkflowDataManagement.GetWorkflowLinktoWorkFlowValue(item, Web, wfid);
                                string initialStepNotifications = WorkflowDataManagement.GetWorkflowInitialStepNotifications(item, Web, wfid);
                                string confidential = prevConfidential;
                                HttpContext.Current.Session["SessionId"] = null;

                                //ESMA-CR38-Close Warning Message
                                bool isLastStepToSign = CheckIsLastStepToSign(wfid, currentStatus, currentStep);
                              

                                if ((!isLastStepToSign) || (!displayMessage))
                                {
                                    HttpContext.Current.Session["ShowCloseWarningPopUp" + wfid] = "false";

                                    //Change workflow for next step
                                    JumpToNextStep(wfid, prevStep, wftypeName, Web, ref item, loggedUser, ref currentStep, ref currentStatus, ref confidential, ref responsible, false, userAD, passwordAD, prevLinktoworkflow, actorsBackupDictionary, reassignToBackupActor, wftypeOrder, logList, isReassigment);

                                    #region <Log>
                                    //Log confidentiality changing
                                    WorkflowDataManagement.LogConfidentialityChanges(item, logList, wfid, wftypeOrder, prevStatus, prevStep, computerName, prevConfidential, confidential, responsible, loggedUser, parameters, Web);

                                    //Log actor changing
                                    LogActorChanging(wfid, wftypeOrder, prevStatus, wftypeName, prevStep, logList, loggedUser, loggedUser, Comments.GetMyComment(PlaceHolder_NewComments), Web, computerName);
                                    //Log taken action
                                    WorkflowDataManagement.LogWorkflowActivityOnSigning(item, wfid, wftypeName, wftypeOrder, prevStep, Web, loggedUser, loggedUser, computerName, ddlConfidential.SelectedValue, Comments.GetMyComment(PlaceHolder_NewComments), parameters, currentStep);

                                    #endregion


                                    //CR31 new parameter saveActorsSign. Save in history
                                    WorkflowDataManagement.CreateAndSetWorkflowHistory(item, wfid, wftypeName, wftypeOrder, responsible, loggedUser, loggedUser, Web, currentStatus, GeneralFields.GetValue_CheckBox(parameters["GeneralColumn_1"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralAmount"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralColumn_2"], PlaceHolder_GFTable), GeneralFields.GetValue_DateTime(parameters["GeneralDeadline"], PlaceHolder_GFTable), ddlConfidential.SelectedValue, parameters, currentStep, actorsBackupDictionary, reassignToBackupActor, isSaving, isReassigning, currentStep);

                                    //Save general fields
                                    GeneralFields.SaveGeneralFields(wfid, wftypeOrder, wftypeName, item, Web, loggedUser, generalFieldsSessionDictionary, PlaceHolder_GFTable, parameters);

                                    //Send e-mail if step signing requires (ESMA CR26)
                                    SPFieldUserValue receiverGroupValue = WorkflowDataManagement.GetEmailReceiverGroup(item, prevStep, Web, wfid, ref sendEmail);

                                    if (sendEmail.Equals(true))
                                        WorkflowDataManagement.NotificationsModule(wfid, initialStepNotifications, prevStep, receiverGroupValue, Web, item, userAD, passwordAD, parameters, DynamicUserListsPanel);


                                    //Send e-mail notification if workflow is urgent
                                    if (GeneralFields.GetValue_CheckBox(parameters["GeneralColumn_1"], PlaceHolder_GFTable))
                                        General.SendUrgentNotification(Web, HttpContext.Current.Session["FormWFID"].ToString(), WorkflowDataManagement.GetWorkflowSubject(item, Web, wfid), responsible, parameters);

                                    //Check in docs
                                    DocumentLibraries.CheckInDocs(item.ParentList, wfid, Web, loggedUser);
                                }
                                else
                                {
                                    ShowPanelWarningCloseWorkflow(wfid);
                                    HttpContext.Current.Session["ShowCloseWarningPopUp" + wfid] = "true";
                                }

                            }
                            else
                            {
                                ConcurrentError = true;
                                HttpContext.Current.Session["ShowCloseWarningPopUp" + wfid] = "false";
                                General.saveErrorsLog(wfid, btnName + " - Logged User: " + loggedUser.LoginName + "[" + loggedUser.ID + "]");
                                General.saveErrorsLog(wfid, btnName + " - Responsible: " + responsible.LoginName + "[" + responsible.ID + "]");
                                SPUtility.TransferToErrorPage("ACCESS DENIED. This action cannot be performed.");
                                
                            }


                            HttpContext.Current.Session["FormStartTime" + HttpContext.Current.Session["FormWFID"].ToString()] = System.DateTime.UtcNow;

                        }
                        Web.AllowUnsafeUpdates = false;
                        Web.Close();
                        Web.Dispose();
                    }
                });

                try
                {

                    if (HttpContext.Current.Session["FormWFID"] != null && ((responsible != null && !loggedUser.ID.Equals(responsible.ID)) || responsible == null))
                        GoBack(HttpContext.Current.Session["FormWFID"].ToString(), parameters);
                    else
                    {
                        HttpContext.Current.Session["FormMyComment" + wfid] = "";
                        HttpContext.Current.Session["FormRefreshing" + wfid] = "Refreshing";
                        HttpContext.Current.Response.Redirect(HttpContext.Current.Request.Url.ToString(), false);
                    }

                }
                catch (Exception ex)
                {
                    General.saveErrorsLog(wfid, btnName + " - Redirect: " + ex.Message);
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, btnName + " - " + ex.Message);

                if (ConcurrentError)
                    SPUtility.TransferToErrorPage("ACCESS DENIED. This action cannot be performed.");
            }
        }

        /// <summary>
        /// Delete button actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void btnDelete_Click(object sender, EventArgs e)
        {
            bool ConcurrentError = false;
            string wfid = string.Empty;

            try
            {

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                    {
                        SPWeb Web = Site.OpenWeb();
                        Web.AllowUnsafeUpdates = true;


                        SPUser loggedUser = General.GetRealCurrentSPUser(this.Page);
                        string computerName = General.GetComputerName(HttpContext.Current);

                        if (HttpContext.Current.Session["FormWFType"] != null && HttpContext.Current.Session["FormWFID"] != null && loggedUser != null)
                        {
                            bool isSaving = false;
                            bool isReassigning = false;
                            bool reassignToBackupActor = false; //ESMA-CR31-BackupGroups
                            wfid = HttpContext.Current.Session["FormWFID"].ToString();
                            string wftypeOrder = HttpContext.Current.Session["FormWFType"].ToString();
                            SPListItem item = WorkflowDataManagement.GetWorkflowItem(wfid, wftypeName, Web);
                            string userAD = General.Decrypt(parameters["AD User"]);
                            string passwordAD = General.Decrypt(parameters["AD Password"]);
                            string domain = parameters["Domain"];
                            string confidentialValue = item["ConfidentialWorkflow"].ToString();

                            SPUser responsible = WorkflowDataManagement.GetWorkflowCurrentStepResponsible(item, Web, wfid, domain);
                            int currentStep = WorkflowDataManagement.GetWorkflowCurrentStep(item, Web, wfid);
                            
                            //ESMA-CR31-Backup Group
                            Dictionary<string, string> actorsBackupDictionary = ControlManagement.GetStepBackupResponsibles(item, wfid, Web);
                            bool isBackupResponsible = WorkflowDataManagement.IsMemberOfBackupResponsibleGroup(wfid, loggedUser, domain, actorsBackupDictionary, userAD, passwordAD, currentStep.ToString(), parameters);

                            if ((responsible != null && responsible.ID.Equals(loggedUser.ID)) || isBackupResponsible)
                            {
                                SPList logList = WorkflowDataManagement.GetWorkflowLogList(wftypeName, Web);
                                SPList docList = item.ParentList;

                                string status = WorkflowDataManagement.GetWorkflowStatus(item, Web, parameters, wfid);

                                if (parameters.ContainsKey("Status Draft") && status.ToUpper().Equals(parameters["Status Draft"].ToUpper()))
                                {
                                    WorkflowDataManagement.DeleteAllLogsByWFID(wfid, Web, wftypeName, logList);
                                    WorkflowDataManagement.RemoveWorkflowOnCreation(item, item.ParentList, Web, parameters, wfid, status);
                                    WorkflowDataManagement.RemoveWorkflowHistoryOnCreation(item, item.ParentList, Web, parameters, wfid, status);
 
                                }
                                else
                                {
                                    string comment = Comments.GetMyComment(PlaceHolder_NewComments);

                                    ChangeStatusToDeleted(ref item, Web, comment, wftypeName, status, computerName, logList, loggedUser, wftypeOrder, wfid, actorsBackupDictionary, reassignToBackupActor, isSaving, isReassigning);
                                    ChangeActionDetailsToDeleted(wftypeName, logList, Web, wfid);
                                }
                            }
                            else
                            {
                                ConcurrentError = true;
                                SPUtility.TransferToErrorPage("ACCESS DENIED. This action cannot be performed.");
                            }

                        }

                        Web.AllowUnsafeUpdates = false;
                        Web.Close();
                        Web.Dispose();
                    }
                });


                if (HttpContext.Current.Session["FormWFID"] != null)
                    GoBack(HttpContext.Current.Session["FormWFID"].ToString(), parameters);

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "btnDelete()" + ex.Message);

                if (ConcurrentError)
                    SPUtility.TransferToErrorPage("ACCESS DENIED. This action cannot be performed.");
            }

        }

        /// <summary>
        /// Process workflow deletion when workflow is In Progress
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <param name="comment"></param>
        /// <param name="wftypeName"></param>
        /// <param name="status"></param>
        /// <param name="computerName"></param>
        /// <param name="loggedUser"></param>
        /// <param name="wftypeOrder"></param>
        /// <param name="wfid"></param>
        /// <param name="userAD"></param>
        /// <param name="passwordAD"></param>
        public void ChangeStatusToDeleted(ref SPListItem item, SPWeb Web, string comment, string wftypeName, string status, string computerName, SPList logList, SPUser loggedUser, string wftypeOrder, string wfid, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, bool isSaving, bool isReassigning)
        {
            try
            {
                int currentStep = WorkflowDataManagement.GetWorkflowCurrentStep(item, Web, wfid);
                int nextStep = 0;
                Hashtable stepResponsibles = new Hashtable();
                WorkflowDataManagement.CreateWorkflowLog(wftypeOrder, wfid, currentStep, status, loggedUser, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Commented.ToString()), WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Deleted.ToString()), computerName, comment, ddlConfidential.SelectedValue, logList, Web, parameters, loggedUser, true);
                WorkflowDataManagement.CreateWorkflowLog(wftypeOrder, wfid, currentStep, parameters["Status Deleted"], loggedUser, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Deleted.ToString()), string.Empty, computerName, string.Empty, ddlConfidential.SelectedValue, logList, Web, parameters, loggedUser, true);
                //Workflow processing
                SetStepResponsibility(ref item, wfid, wftypeName, ref currentStep, ref nextStep, ref stepResponsibles, true, false, Web);
                WorkflowDataManagement.SetWorkflowItemFields(item, parameters["Status Deleted"], currentStep, stepResponsibles, null, ddlConfidential.SelectedValue, parameters, loggedUser, WFID_data.Value, wfid, Web, currentStep, actorsBackupDictionary, reassignToBackupActor, wftypeOrder, logList, isSaving);
                //Change workflow history reference
                WorkflowDataManagement.CreateAndSetWorkflowHistory(item, wfid, wftypeName, wftypeOrder, null, loggedUser, loggedUser, Web, parameters["Status Deleted"], GeneralFields.GetValue_CheckBox(parameters["GeneralColumn_1"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralAmount"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralColumn_2"], PlaceHolder_GFTable), GeneralFields.GetValue_DateTime(parameters["GeneralDeadline"], PlaceHolder_GFTable), ddlConfidential.SelectedValue, parameters, currentStep, actorsBackupDictionary, reassignToBackupActor, isSaving, isReassigning, currentStep);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "ChangeStatusToDeleted()" + ex.Message);
            }
        }

        public void ChangeActionDetailsToDeleted(string wftypeName, SPList logList, SPWeb Web, string wfid)
        {
            try
            {
                if (HttpContext.Current.Session["FormStartTime" + wfid] != null)
                {
                    DateTime startTime = (DateTime)HttpContext.Current.Session["FormStartTime" + wfid];
                    string actionTakenDeleted = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Deleted.ToString());
                    string actionTakenSigned = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Signed.ToString());

                    SPQuery query = new SPQuery();
                    query.Query = "<Where><And><Geq><FieldRef Name='Created' /><Value  IncludeTimeValue='TRUE' Type='DateTime'>" + SPUtility.CreateISO8601DateTimeFromSystemDateTime(startTime) + "</Value></Geq><And><Eq><FieldRef Name='WFID' /><Value Type='Text'>" + wfid + "</Value></Eq>"
                        + "<IsNotNull><FieldRef Name='WorkflowComment' /></IsNotNull></And></And></Where><OrderBy><FieldRef Name='ID' Ascending='False' /></OrderBy>";

                    SPListItemCollection itemCol = logList.GetItems(query);
                    SPListItem item = null;
                    bool modified = false;

                    if (itemCol != null && itemCol.Count > 0)
                    {
                        item = itemCol[0];

                        if (item["ActionDetails"] != null)
                        {
                            if (item["ActionDetails"].ToString().ToLower().Equals(actionTakenSigned.ToLower()))
                            {
                                item["ActionDetails"] = actionTakenDeleted;
                                modified = true;
                            }
                        }
                    }

                    if (modified)
                    {
                        try
                        {
                            item.Update();
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "ChangeActionDetailsToDeleted()" + ex.Message);
            }

        }

        /// <summary>
        /// Close button actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void btnClose_Click(object sender, EventArgs e)
        {
            string wfid = string.Empty;

            try
            {
                if (HttpContext.Current.Session["FormWFID"] != null)
                {
                    wfid = HttpContext.Current.Session["FormWFID"].ToString();
                    GoBack(HttpContext.Current.Session["FormWFID"].ToString(), parameters);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "btnClose()" + ex.Message);
            }

        }

        /// <summary>
        /// Reject button actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void btnReject_Click(object sender, EventArgs e)
        {
            bool ConcurrentError = false;
            string wfid = string.Empty;

            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                    {
                        SPWeb Web = Site.OpenWeb();

                        Web.AllowUnsafeUpdates = true;
                        groupRadioButtons.Visible = true;
                        DynamicUserListsPanel.Visible = false;
                        SPUser loggedUser = General.GetRealCurrentSPUser(this.Page);
                        HttpContext.Current.Session["SessionId"] = null;

                        if (HttpContext.Current.Session["FormWFType"] != null && HttpContext.Current.Session["FormWFID"] != null && loggedUser != null)
                        {
                            wfid = HttpContext.Current.Session["FormWFID"].ToString();
                            string wftypeOrder = HttpContext.Current.Session["FormWFType"].ToString();
                            SPListItem item = WorkflowDataManagement.GetWorkflowItem(wfid, wftypeName, Web);
                            int currentStep = WorkflowDataManagement.GetWorkflowCurrentStep(item, Web, wfid);
                            string userAD = General.Decrypt(parameters["AD User"]);
                            string passwordAD = General.Decrypt(parameters["AD Password"]);
                            string domain = parameters["Domain"];

                            SPUser responsible = WorkflowDataManagement.GetWorkflowCurrentStepResponsible(item, Web, wfid, domain);
                            //ESMA-CR31-Backup Responsibles
                            Dictionary<string, string> actorsBackupDictionary = ControlManagement.GetStepBackupResponsibles(item, wfid, Web);
                            bool isBackupResponsible = WorkflowDataManagement.IsMemberOfBackupResponsibleGroup(wfid, loggedUser, domain,  actorsBackupDictionary,  userAD,  passwordAD, currentStep.ToString(), parameters);

                            if ((responsible != null && responsible.ID.Equals(loggedUser.ID)) || isBackupResponsible)
                            {
                                
                                HttpContext.Current.Session["FormReject" + wfid] = "Rejecting";
                                
                                SPUser initiator = General.GetAuthor(wfid, item, Web);

                                ControlManagement.EnableDisableUserInterface(parameters["Status In Progress"], currentStep, ref DynamicUserListsPanel, ref DynamicRadioButtonListPanel, ref btnSign, ref btnSign2, ref btnSave, ref btnSave2, ref btnOnHold, ref btnOnHold2, ref btnCancel, ref btnCancel2, ref btnDelete, ref btnDelete2, ref btnClose, ref btnClose2, ref HyperLinkPrint, ref btnAssign, ref btnAssign2, ref btnReject, ref btnReject2, ref ddlConfidential, ref groupRadioButtons, loggedUser, initiator, parameters, true, true, ref PlaceHolder_PreviousComments, ref PlaceHolder_NewComments, ref lblCommentRequired, ref PlaceHolder_GFTable, ref DocsMainButtons, ref DocsAbacButtons, ref DocsSupportingButtons, ref DocsPaperButtons, ref DocsSignedButtons, ref lblDocumentsCheckedOutWarning, ref PanelCheckedOutWarning, Web, item, wfid, wftypeOrder, wftypeName, ref WFID_Textbox, ref WFID_buttonAdd, actorsBackupDictionary, domain, userAD, passwordAD);
                            }
                            else
                            {
                                ConcurrentError = true; 
                                General.saveErrorsLog(wfid, "btnReject - Logged User: " + loggedUser.LoginName + "[" + loggedUser.ID + "]");
                                General.saveErrorsLog(wfid, "btnReject  - Responsible: " + responsible.LoginName + "[" + responsible.ID + "]");
                                SPUtility.TransferToErrorPage("ACCESS DENIED. This action cannot be performed.");
                            }
                        }

                     

                        Web.AllowUnsafeUpdates = false;
                        Web.Close();
                        Web.Dispose();
                    }
                });
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "btnReject()" + ex.Message);

                if (ConcurrentError)
                    SPUtility.TransferToErrorPage("ACCESS DENIED. This action cannot be performed.");
                else if (HttpContext.Current.Session["FormWFID"] != null)
                    GoBack(HttpContext.Current.Session["FormWFID"].ToString(), parameters);
            }

        }

        /// <summary>
        /// Assign button actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void btnAssign_Click(object sender, EventArgs e)
        {
            string wfid = string.Empty;

            try
            {
                SPUser finalStepResponsible = null;
                SPUser loggedUser = General.GetRealCurrentSPUser(this.Page);
                wfid = HttpContext.Current.Session["FormWFID"].ToString();


                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                    {
                        SPWeb Web = Site.OpenWeb();
                        Web.AllowUnsafeUpdates = true;

                        string computerName = General.GetComputerName(HttpContext.Current);

                        if (HttpContext.Current.Session["FormWFType"] != null && HttpContext.Current.Session["FormWFID"] != null && loggedUser != null)
                        {
                            bool isReassigning = false;
                            bool isSaving = false;
                            bool reassignToBackupActor = false; //ESMA-CR31-BackupGroups
                            bool isBackupResponsible = false;
                            bool isBackupInitiator = false;
                            string userAD = General.Decrypt(parameters["AD User"]);
                            string passwordAD = General.Decrypt(parameters["AD Password"]);
                            string domain = parameters["Domain"];
                            string wftypeOrder = HttpContext.Current.Session["FormWFType"].ToString();
                            SPListItem item = WorkflowDataManagement.GetWorkflowItem(wfid, wftypeName, Web);
                            SPUser responsible = WorkflowDataManagement.GetWorkflowCurrentStepResponsible(item, Web, wfid, domain);
                            int currentStep = WorkflowDataManagement.GetWorkflowCurrentStep(item, Web, wfid);
                            
                            //ESMA-CR31-Backup Group
                            Dictionary<string, string> actorsBackupDictionary = ControlManagement.GetStepBackupResponsibles(item, wfid, Web);
                            bool isBackupMember = WorkflowDataManagement.IsMemberOfBackupGroup(loggedUser, wfid, domain, actorsBackupDictionary, userAD, passwordAD, ref isBackupInitiator, ref isBackupResponsible, Convert.ToString(currentStep), parameters);
                            if (isBackupMember)
                                reassignToBackupActor = true;

                            if ((responsible != null && responsible.ID.Equals(loggedUser.ID)) || isBackupResponsible)
                            {
                                string prevConfidential = WorkflowDataManagement.GetWorkflowConfidentialValue(item, Web);


                                //string confidential = prevConfidential;
                                string confidential = ddlConfidential.SelectedValue;
                                SPList logList = WorkflowDataManagement.GetWorkflowLogList(wftypeName, Web);
                               
                                string currentStatus = WorkflowDataManagement.GetWorkflowStatus(item, Web, parameters, wfid);
                                bool isRejection = HttpContext.Current.Session["FormReject" + wfid] != null && HttpContext.Current.Session["FormReject" + wfid].ToString().Equals("Rejecting");
                                HttpContext.Current.Session["SessionId"] = null;

                                //REVIEW!!!
                                SPUser prevResponsible = (SPUser)WorkflowDataManagement.GetWorkflowCurrentStepResponsible(item, Web, wfid, domain);

                                #region Log
                                //Log confidentiality changing
                                WorkflowDataManagement.LogConfidentialityChanges(item, logList, wfid, wftypeOrder, currentStatus, currentStep, computerName, prevConfidential, confidential, prevResponsible, loggedUser, parameters, Web);

                                //Log actor changing
                                LogActorChanging(wfid, wftypeOrder, currentStatus, wftypeName, currentStep, logList, prevResponsible, loggedUser, Comments.GetMyComment(PlaceHolder_NewComments), Web, computerName);

                                #endregion

                                #region Set Step and its responsible
                              
                                   
                                    Hashtable stepResponsibles = new Hashtable();
                                    int nextStep = int.Parse(groupRadioButtons.SelectedValue);

                                    WorkflowDataManagement.CreateWorkflowLog(wftypeOrder, wfid, currentStep, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Rejected.ToString()), prevResponsible, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Rejected.ToString()), string.Empty, computerName, string.Empty, ddlConfidential.SelectedValue, logList, Web, parameters, loggedUser, true);
                                    WorkflowDataManagement.CreateWorkflowLog(wftypeOrder, wfid, currentStep, currentStatus, prevResponsible, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Commented.ToString()), WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Rejected.ToString()), computerName, Comments.GetMyComment(PlaceHolder_NewComments), ddlConfidential.SelectedValue, logList, Web, parameters, loggedUser, true);

                                    SetStepResponsibility(ref item, wfid, wftypeName, ref currentStep, ref nextStep, ref stepResponsibles, true, true, Web);
                                    finalStepResponsible = ControlManagement.GetStepResponsible(nextStep, Web, DynamicUserListsPanel, parameters, item, wfid, userAD, passwordAD);
                                    WorkflowDataManagement.SetWorkflowItemFields(item, parameters["Status Rejected"], nextStep, stepResponsibles, finalStepResponsible, ddlConfidential.SelectedValue, parameters, loggedUser, WFID_data.Value, wfid, Web, currentStep, actorsBackupDictionary, reassignToBackupActor, wftypeOrder, logList, isSaving);

                                    //Change reference in workflow histoy
                                    WorkflowDataManagement.CreateAndSetWorkflowHistory(item, wfid, wftypeName, wftypeOrder, finalStepResponsible, loggedUser, loggedUser, Web, parameters["Status Rejected"], GeneralFields.GetValue_CheckBox(parameters["GeneralColumn_1"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralAmount"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralColumn_2"], PlaceHolder_GFTable), GeneralFields.GetValue_DateTime(parameters["GeneralDeadline"], PlaceHolder_GFTable), ddlConfidential.SelectedValue, parameters, currentStep, actorsBackupDictionary, reassignToBackupActor, isSaving, isReassigning, currentStep);

                                    //Save general fields
                                    GeneralFields.SaveGeneralFields(wfid, wftypeOrder, wftypeName, item, Web, loggedUser, generalFieldsSessionDictionary, PlaceHolder_GFTable, parameters);

                                    //Check in docs
                                    DocumentLibraries.CheckInDocs(item.ParentList, wfid, Web, loggedUser);
                             
                                #endregion

                                //Send e-mail notification to rejected step responsible
                                General.SendRejectionNotification(Web, HttpContext.Current.Session["FormWFID"].ToString(), WorkflowDataManagement.GetWorkflowSubject(item, Web, wfid), finalStepResponsible, parameters);
                            }
                            else
                            {
                                SPUtility.TransferToErrorPage("ACCESS DENIED. This action cannot be performed.");
                            }


                        }

              
                        Web.AllowUnsafeUpdates = false;
                        Web.Close();
                        Web.Dispose();
                    }
                });


                try
                {
                    if (HttpContext.Current.Session["FormWFID"] != null && ((finalStepResponsible != null && !loggedUser.ID.Equals(finalStepResponsible.ID)) || finalStepResponsible == null))
                        GoBack(HttpContext.Current.Session["FormWFID"].ToString(), parameters);
                    else
                    {
                        HttpContext.Current.Session["FormMyComment" + wfid] = "";
                        HttpContext.Current.Session["FormRefreshing" + wfid] = "Refreshing";
                        HttpContext.Current.Response.Redirect(HttpContext.Current.Request.Url.ToString(), false);
                    }
                }
                catch (Exception ex)
                {
                    General.saveErrorsLog(wfid, "btnAssign() - Redirect" + ex.Message);
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "btnAssign()" + ex.Message);
            }


        }

        /// <summary>
        /// Does nothing. It is only for the proper functioning of CR33
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ddlGroupNoCurrent_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                DropDownList ddl = (DropDownList)sender;
                ControlManagement.RetainControlValueActors(ddl.ID, ddl.SelectedValue);

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                    {
                        SPWeb Web = Site.OpenWeb();
                        Web.AllowUnsafeUpdates = true;
                        Web.Close();
                    }
                });

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "ddlGroupNoCurrent_SelectedIndexChanged()" + ex.Message);
            }
        }
        /// <summary>
        /// Reassigning actions. Same actions as Sign button actions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ddlGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            string wfid = string.Empty;

            try
            {

                DropDownList ddl = (DropDownList)sender;
                ControlManagement.RetainControlValueActors(ddl.ID, ddl.SelectedValue);

                    string ddlStepNumber = ControlManagement.GetStepNumber_by_ddlID(ddl.ID);
                    int currentStep = 1;
                    int prevStep = 1;
                    bool sendEmail = false;

                    //using (new SPMonitoredScope("Monitored 'ddlGroup_SelectedIndexChanged'"))
                    //{

                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                        {
                            SPWeb Web = Site.OpenWeb();
                            Web.AllowUnsafeUpdates = true;

                            SPUser loggedUser = General.GetRealCurrentSPUser(this.Page);

                            if (HttpContext.Current.Session["FormWFType"] != null && HttpContext.Current.Session["FormWFID"] != null && loggedUser != null)
                            {
                                bool isSaving = false;
                                bool isReassigning = true;
                                bool reassignToBackupActor = false; //ESMA-CR31-BackupGroups
                                string computerName = General.GetComputerName(HttpContext.Current);
                                wfid = HttpContext.Current.Session["FormWFID"].ToString();
                                string wftypeOrder = HttpContext.Current.Session["FormWFType"].ToString();
                                HttpContext.Current.Session["FormRefreshing" + wfid] = "Refreshing";
                                //RS Configuration Parameters
                                string userAD = General.Decrypt(parameters["AD User"]);
                                string passwordAD = General.Decrypt(parameters["AD Password"]);
                                string domain = parameters["Domain"];
                                string emailComunSubject = parameters["E-mail Signed Subject"];
                                string emailComunBody = parameters["E-mail Signed Text"];

                                SPListItem item = WorkflowDataManagement.GetWorkflowItem(wfid, wftypeName, Web);
                                SPList logList = WorkflowDataManagement.GetWorkflowLogList(wftypeName, Web);
                                prevStep = WorkflowDataManagement.GetWorkflowCurrentStep(item, Web, wfid);
                                currentStep = prevStep;
                                string currentStatus = WorkflowDataManagement.GetWorkflowStatus(item, Web, parameters, wfid);
                                string prevStatus = currentStatus;
                                string prevConfidential = WorkflowDataManagement.GetWorkflowConfidentialValue(item, Web);
                                string confidential = prevConfidential;
                                string prevLinktoworkflow = WorkflowDataManagement.GetWorkflowLinktoWorkFlowValue(item, Web, wfid);
                                SPUser prevResponsible = WorkflowDataManagement.GetWorkflowCurrentStepResponsible(item, Web, wfid, domain);
                                SPUser responsible = ControlManagement.GetStepResponsible(currentStep, Web, DynamicUserListsPanel, parameters, item, wfid, userAD, passwordAD);
                                   

                                if (ddlStepNumber.Equals(currentStep.ToString()) && (!currentStatus.ToLower().Equals(parameters["Status Closed"].ToLower())))
                                {
                                    HttpContext.Current.Session["FormMyComment" + wfid] = string.Empty;

                                        //ESMA-CR38-Close Warning Message
                                        bool isLastStepToSign = CheckIsLastStepToSign(wfid, currentStatus, currentStep);

                                        if (!isLastStepToSign || responsible != null)
                                        {
                                            HttpContext.Current.Session["ShowCloseWarningPopUp" + wfid] = "false";

                                            //ESMA-CR31-Backup Group
                                            Dictionary<string, string> actorsBackupDictionary = ControlManagement.GetStepBackupResponsibles(item, wfid, Web);
                                            string wfSubject = WorkflowDataManagement.GetWorkflowSubject(item, Web, wfid);

                                            #region <Set Step and its responsible>

                                          
                                            //Change workflow for next step
                                            if (responsible == null)
                                                JumpToNextStep(wfid, prevStep, wftypeName, Web, ref item, loggedUser, ref currentStep, ref currentStatus, ref confidential, ref responsible, false, userAD, passwordAD, prevLinktoworkflow, actorsBackupDictionary, reassignToBackupActor, wftypeOrder, logList, isReassigning);
                                            else
                                                JumpToNextStep(wfid, prevStep, wftypeName, Web, ref item, loggedUser, ref currentStep, ref currentStatus, ref confidential, ref responsible, true, userAD, passwordAD, prevLinktoworkflow, actorsBackupDictionary, reassignToBackupActor, wftypeOrder, logList, isReassigning);

                                            #region <Log>
                                            //Log confidentiality changing
                                            WorkflowDataManagement.LogConfidentialityChanges(item, logList, wfid, wftypeOrder, prevStatus, prevStep, computerName, prevConfidential, confidential, responsible, loggedUser, parameters, Web);

                                            //Log actor changing
                                            LogActorChanging(wfid, wftypeOrder, prevStatus, wftypeName, prevStep, logList, loggedUser, loggedUser, Comments.GetMyComment(PlaceHolder_NewComments), Web, computerName);
                                            //Log taken action
                                            WorkflowDataManagement.CreateWorkflowLog(wftypeOrder, wfid, currentStep, currentStatus, prevResponsible, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Commented.ToString()), WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.ActorReAssigned.ToString()), computerName, Comments.GetMyComment(PlaceHolder_NewComments), ddlConfidential.SelectedValue, logList, Web, parameters, loggedUser, true);

                                            //to solve PAPBUG-147
                                            HttpContext.Current.Session["FormMyComment" + wfid] = "";
                                            Comments.DeleteMyComment(PlaceHolder_NewComments);
                                            // end PAPBUG-147

                                            #endregion


                                            //Change reference in workflow histoy
                                            WorkflowDataManagement.CreateAndSetWorkflowHistory(item, wfid, wftypeName, wftypeOrder, responsible, loggedUser, loggedUser, Web, currentStatus, GeneralFields.GetValue_CheckBox(parameters["GeneralColumn_1"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralAmount"], PlaceHolder_GFTable), GeneralFields.GetValue_TextBox(parameters["GeneralColumn_2"], PlaceHolder_GFTable), GeneralFields.GetValue_DateTime(parameters["GeneralDeadline"], PlaceHolder_GFTable), ddlConfidential.SelectedValue, parameters, currentStep, actorsBackupDictionary, reassignToBackupActor, isSaving, isReassigning, currentStep);

                                            //Save general fields
                                            GeneralFields.SaveGeneralFields(wfid, wftypeOrder, wftypeName, item, Web, loggedUser, generalFieldsSessionDictionary, PlaceHolder_GFTable, parameters);

                                            //Send e-mail if step signing requires
                                            SPFieldUserValue receiverGroupValue = WorkflowDataManagement.GetEmailReceiverGroup(item, prevStep, Web, wfid, ref sendEmail);
                                            General.SendEmailGeneralManagement(wfid, item, Web, wfSubject, emailComunSubject, emailComunBody, userAD, passwordAD, parameters, DynamicUserListsPanel, receiverGroupValue);

                                            //Send e-mail notification if workflow is urgent
                                            if (GeneralFields.GetValue_CheckBox(parameters["GeneralColumn_1"], PlaceHolder_GFTable))
                                                General.SendUrgentNotification(Web, wfid, wfSubject, responsible, parameters);

                                            //Check in docs
                                            DocumentLibraries.CheckInDocs(item.ParentList, wfid, Web, loggedUser);

                                            #endregion

                                        }
                                        else
                                        {
                                            ShowPanelWarningCloseWorkflow(wfid);
                                            HttpContext.Current.Session["ShowCloseWarningPopUp" + wfid] = "true";
                                        }
                                   
                                }
                                else
                                {
                                    HttpContext.Current.Session["ShowCloseWarningPopUp" + wfid] = "false";
                                    SPUtility.TransferToErrorPage("ACCESS DENIED. This action cannot be performed.");
                                }
                            }

                            Web.AllowUnsafeUpdates = false;
                            Web.Close();
                            Web.Dispose();
                        }
                    });

                    try
                    {
                        if (ddlStepNumber.Equals(prevStep.ToString()))
                            HttpContext.Current.Response.Redirect(HttpContext.Current.Request.Url.ToString(), false);
                    }
                    catch (Exception ex) { General.saveErrorsLog(wfid, "ddlGroup_SelectedIndexChanged() - Response.Redirect: " + ex.Message); }
                
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "ddlGroup_SelectedIndexChanged() - " + ex.Message);
            }


        }

        public void btnCloseWarning_Click(object sender, ImageClickEventArgs e)
        {
            try
            {
                HttpContext.Current.Session["WFIDLINKS"] = true;

                lblDocumentsCheckedOutWarning.Visible = false;
                PanelCheckedOutWarning.Visible = false;

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "btnCloseWarning_Click() - " + ex.Message);
            }

        }

        public void WFID_buttonAdd_Click(object sender, EventArgs e)
        {
            try
            {
                string WFID = HttpContext.Current.Session["FormWFID"].ToString();

                // Get LinkToWorkflow controls
                TextBox wfidToLinkTextbox = General.Controles.FindControlRecursive<TextBox>(this.Page, "WFID_Textbox");
                HiddenField wfidToLinkDataHidden = General.Controles.FindControlRecursive<HiddenField>(this.Page, "WFID_data");                

                string WFIDtoLink = wfidToLinkTextbox.Text.Trim();

                if (WFIDtoLink == WFID)
                {
                    showErrorMessageWFID("It is not possible to link this workflow to its own workflow number.");
                }
                else
                {
                    bool ExistAnyWFID = WorkflowDataManagement.DoesWorkflowExists(WFIDtoLink);

                    if (ExistAnyWFID)
                    {
                        SPWeb web = new SPSite(SPContext.Current.Site.Url).OpenWeb();
                        string type = String.Empty;

                        if (WorkflowDataManagement.DoesWorkflowExists(WFIDtoLink, web))
                        {
                            SPSecurity.RunWithElevatedPrivileges(delegate()
                            {
                                using (SPSite elevatedSite = new SPSite(SPContext.Current.Web.Url))
                                {
                                    using (SPWeb elevatedWeb = elevatedSite.OpenWeb())
                                    {
                                        type = WorkflowDataManagement.GetWorkflowTypeByWFID(WFIDtoLink, elevatedWeb);
                                    }
                                }
                            });

                            string final = WFIDtoLink + ":" + type;

                            if (!wfidToLinkDataHidden.Value.Contains(WFIDtoLink))
                            {
                                if (string.IsNullOrEmpty(wfidToLinkDataHidden.Value)) wfidToLinkDataHidden.Value += final;
                                else wfidToLinkDataHidden.Value += "|" + final;
                            }
                        }
                        else
                        {
                            showErrorMessageWFID("Sorry, you cannot link confidential workflows you don´t have access to.");
                        }
                    }
                    else
                    {
                        showErrorMessageWFID("The linked workfow ID does not correspond to an existing one.");
                    }
                }

                // CR 24
                HttpContext.Current.Session["FormLinkToWorkFlowModified" + WFID] = wfidToLinkDataHidden.Value;
                // FIN CR 24
                wfidToLinkTextbox.Text = "";
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "WFID_buttonAdd_Click() - " + ex.Message);
            }
        }

        // CR 24
        private void showErrorMessageWFID(string txt)
        {
            lblLinkToWFWarning.Visible = true;
            PanelLinkToWFWarning.Visible = true;
            lblLinkToWFWarning.Text = txt;
        }

        // FIN CR 24
        #endregion

        #region Reusable methods for button actions

        /// <summary>
        /// Process step changing for workflow item.
        /// </summary>
        /// <param name="prevStep"></param>
        /// <param name="wftypeName"></param>
        /// <param name="Web"></param>
        /// <param name="item"></param>
        /// <param name="realEditor"></param>
        /// <param name="currentStep"></param>
        /// <param name="currentStatus"></param>
        /// <param name="confidential"></param>
        /// <param name="responsible"></param>
        /// <param name="isSaving"></param>
        /// <param name="userAD"></param>
        /// <param name="passwordAD"></param>
        private void JumpToNextStep(string wfid, int prevStep, string wftypeName, SPWeb Web, ref SPListItem item, SPUser realEditor, ref int currentStep, ref string currentStatus, ref string confidential, ref SPUser responsible, bool isSaving, string userAD, string passwordAD, string LinkToWorkFlow, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, string wftypeOrder, SPList logList, bool isReassigning)
        {
            try
            {
                if (!currentStatus.ToUpper().Equals(parameters["Status On Hold"].ToUpper()))
                    currentStatus = WorkflowDataManagement.GetWorkflowStatus(item, Web, parameters, wfid);
                //ESMA-CR32-"On hold" button
                else if (isReassigning.Equals(true) && currentStatus.ToUpper().Equals(parameters["Status On Hold"].ToUpper()))
                    currentStatus = parameters["Status In Progress"];

                int nextStep = 0;
                Hashtable stepResponsibles = new Hashtable();
                SetStepResponsibility(ref item, wfid, wftypeName, ref currentStep, ref nextStep, ref stepResponsibles, isSaving, false, Web);
                int actualStep = currentStep;
                currentStep = nextStep;
                responsible = ControlManagement.GetStepResponsible(currentStep, Web, DynamicUserListsPanel, parameters, item, wfid, userAD, passwordAD);
                confidential = ddlConfidential.SelectedValue;
                LinkToWorkFlow = WFID_data.Value;
                
                if (!currentStatus.ToUpper().Equals(parameters["Status Deleted"].ToUpper()) && !currentStatus.ToUpper().Equals(parameters["Status Closed"].ToUpper()))
                {
                    if (currentStep > groupDDLs.Count && parameters.ContainsKey("Status Closed"))
                        WorkflowDataManagement.SetWorkflowItemFields(item, parameters["Status Closed"], groupDDLs.Count, stepResponsibles, null, confidential, parameters, realEditor, LinkToWorkFlow, wfid, Web, actualStep, actorsBackupDictionary, reassignToBackupActor, wftypeOrder, logList, isSaving);
                    else if (isSaving)
                        WorkflowDataManagement.SetWorkflowItemFields(item, currentStatus, nextStep, stepResponsibles, responsible, confidential, parameters, realEditor, LinkToWorkFlow, wfid, Web, actualStep, actorsBackupDictionary, reassignToBackupActor, wftypeOrder, logList, isSaving);
                    else
                        WorkflowDataManagement.SetWorkflowItemFields(item, parameters["Status In Progress"], nextStep, stepResponsibles, responsible, confidential, parameters, realEditor, LinkToWorkFlow, wfid, Web, actualStep, actorsBackupDictionary, reassignToBackupActor, wftypeOrder, logList, isSaving);
                }
                else if (currentStatus.ToUpper().Equals(parameters["Status Closed"].ToUpper()))
                    WorkflowDataManagement.SetWorkflowItemFields(item, currentStatus, groupDDLs.Count, stepResponsibles, null, confidential, parameters, realEditor, LinkToWorkFlow, wfid, Web, actualStep, actorsBackupDictionary, reassignToBackupActor, wftypeOrder, logList, isSaving);
                else
                    WorkflowDataManagement.SetWorkflowItemFields(item, currentStatus, prevStep, stepResponsibles, null, confidential, parameters, realEditor, LinkToWorkFlow, wfid, Web, actualStep, actorsBackupDictionary, reassignToBackupActor, wftypeOrder, logList, isSaving);

                currentStatus = WorkflowDataManagement.GetWorkflowStatus(item, Web, parameters, wfid);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "JumpToNextStep() - " + ex.Message);
            }
        }


        private void LogActorChanging(string wfid, string wftypeOrder, string currentStatus, string wftypeName, int prevStep, SPList logList, SPUser responsible, SPUser realEditor, string comment, SPWeb Web, string computer)
        {
            try
            {
                if ((currentStatus.ToUpper().Equals(parameters["Status In Progress"].ToUpper())) || (currentStatus.ToUpper().Equals(parameters["Status On Hold"].ToUpper())|| (currentStatus.ToUpper().Equals(parameters["Status Rejected"].ToUpper()))))
                {
                    Dictionary<string, string> postList = ControlManagement.GetStepResponsibles(DynamicUserListsPanel, false, wfid);
                    Dictionary<string, string> differences = CompareDictionaries(postList, wfid);

                    if (differences != null)
                        foreach (KeyValuePair<String, String> entry in differences)
                        {
                            //delete re-assigned repeat comments PAPBUG-119
                            string commentAdd = string.Empty;
                            if (int.Parse(entry.Key) == prevStep)
                                commentAdd = comment;

                            WorkflowDataManagement.CreateWorkflowLog(wftypeOrder, wfid, prevStep, currentStatus, responsible, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.ActorReAssigned.ToString()), "Step: " + entry.Key + ". Previous actor: " + prevDictionary[entry.Key] + ". Current actor: " + entry.Value + ".", computer, comment, ddlConfidential.SelectedValue, logList, Web, parameters, realEditor, true);
                        }

                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "LogActorChanging() - " + ex.Message);
            }
        }

        //ESMA-CR38-Close Warning message
        private bool CheckIsLastStepToSign(string wfid, string currentStatus, int currentStep)
        {
            bool isLastStep = true;
            int count = 1;

            try
            {
                if (currentStep < groupDDLs.Count)
                {

                    foreach (Control control in DynamicUserListsPanel.Controls)
                    {
                        if (control is UpdatePanel)
                        {
                            if (count > currentStep)
                            {
                                UpdatePanel up = (UpdatePanel)control;
                                DropDownList ddl = (DropDownList)up.Controls[0].Controls[0];

                                if ((ddl != null) && (!string.IsNullOrEmpty(ddl.SelectedValue)))
                                {
                                    isLastStep = false;
                                    break;
                                }
                            }

                            count++;
                        }
                    }

                }
                else if (currentStep.Equals(groupDDLs.Count))
                    isLastStep = false;
              


            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "CheckIsLastStepToSign() - " + ex.Message);
            }
            
            return isLastStep;
        }



        ///// <summary>
        /// Process workflow activity
        /// </summary>
        /// <param name="currentStep"></param>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <param name="responsible"></param>
        /// <param name="realEditor"></param>
        /// <param name="wftypeName"></param>
        /// <param name="prevStep"></param>
        /// <param name="currentStatus"></param>
        /// <param name="computerName"></param>
        public void SetWorkflowStatusOrClose(ref int currentStep, string wfid, string wftypeOrder, ref SPListItem item, SPList logList, SPWeb Web, SPUser responsible, SPUser realEditor, int prevStep, ref string currentStatus, string computerName)
        {
            try
            {
                if (currentStep > groupDDLs.Count && parameters.ContainsKey("Status Closed"))
                {
                    WorkflowDataManagement.SetWorkflowStatus(ref item, parameters["Status Closed"], Web, parameters, realEditor);
                    WorkflowDataManagement.SetWorkflowStep(ref item, groupDDLs.Count, Web, realEditor, wfid);
                    currentStep = WorkflowDataManagement.GetWorkflowCurrentStep(item, Web, wfid);
                    WorkflowDataManagement.CreateWorkflowLog(wftypeOrder, wfid, prevStep, parameters["Status Closed"], responsible, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Finished.ToString()), string.Empty, computerName, string.Empty, ddlConfidential.SelectedValue, logList, Web, parameters, realEditor, true);
                    currentStatus = WorkflowDataManagement.GetWorkflowStatus(item, Web, parameters, wfid);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetWorkflowStatusOrClose() - " + ex.Message);
            }
        }

       
        #endregion

        /// <summary>
        /// Set workflow information and step responsibles according to selected values in workflow form
        /// </summary>
        /// <param name="item"></param>
        /// <param name="wftypeName"></param>
        /// <param name="Web"></param>
        /// <param name="realEditor"></param>
        public void SetStepResponsibility(ref SPListItem item, string wfid, string wftypeName, ref int currentStep, ref int nextStep, ref Hashtable stepResponsibles, bool isNotSigning, bool isRejecting, SPWeb Web)
        {
            try
            {
                int count = 1;
                bool nextStepFound = false;
           

                foreach (Control control in DynamicUserListsPanel.Controls)
                {
                    if (control is UpdatePanel)
                    {
                        UpdatePanel up = (UpdatePanel)control;

                        string fieldName = "Step_x0020_" + count.ToString() + "_x0020_Assigned_x0020_To";

                        if (item.ParentList.Fields.ContainsFieldWithStaticName(fieldName))
                        {
                            
                            DropDownList ddl = (DropDownList)up.Controls[0].Controls[0];
                            SPUser user = null;

                            if (ddl != null && !string.IsNullOrEmpty(ddl.SelectedValue))
                            {
                                try
                                {
                                    string selectedUser = ((parameters.ContainsKey("Domain")) ? parameters["Domain"] + "\\" : String.Empty) + ddl.SelectedValue;
                                    user = Web.EnsureUser(selectedUser);
                                }
                                catch
                                {
                                    user = General.GetSPUserObject(item, fieldName, wfid, Web);
                                }

                                if (isRejecting)
                                {
                                    nextStepFound = true;
                                }
                                else if (!nextStepFound && ((isNotSigning && count >= currentStep) || count > currentStep))
                                {
                                    nextStep = count;
                                    nextStepFound = true;
                                }                                
                            }
                            stepResponsibles.Add(fieldName, user);

                        }

                        count++;
                    }
                }

                if (!nextStepFound)
                    nextStep = count;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetStepResponsibility() - " + ex.Message);
            }
        }


        public Dictionary<string, string> CompareDictionaries(Dictionary<string, string> postDictionary, string wfid)
        {
            Dictionary<string, string> differences = new Dictionary<string, string>();

            try
            {
                if (prevDictionary != null && postDictionary != null && prevDictionary.Count.Equals(postDictionary.Count))
                {
                    foreach (KeyValuePair<String, String> entry in prevDictionary)
                    {
                        try
                        {
                            if (!entry.Value.ToUpper().Equals(postDictionary[entry.Key].ToUpper()))
                                differences.Add(entry.Key, postDictionary[entry.Key]);

                        }
                        catch (Exception ex)
                        {
                            General.saveErrorsLog(wfid, "CompareDictionaries KeyValuePair()" + ex.Message);
                            continue;
                        }
                    }
                }
                else
                    differences = null;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "CompareDictionaries " + ex.Message);
            }

            return differences;
        }

        /// <summary>
        /// Cancel rejection and its workflow form controls status
        /// </summary>
        /// <param name="fromRejecting"></param>
        /// <param name="Web"></param>
        /// <param name="loggedUser"></param>
        public void CancelRejection(SPListItem item, ref bool fromRejecting, string wfid, string wftypeOrder, SPWeb Web, SPUser loggedUser, Dictionary<string, string> actorsBackupDictionary, string domain, string userAD, string passwordAD)
        {
            try
            {
                HttpContext.Current.Session["FormReject" + wfid] = null;

                fromRejecting = true;
                int currentStep = WorkflowDataManagement.GetWorkflowCurrentStep(item, Web, wfid);
                DynamicUserListsPanel.Visible = true;
                DynamicRadioButtonListPanel.Visible = false;
                
                SPUser initiator = General.GetAuthor(wfid, item, Web);

                ControlManagement.EnableDisableUserInterface(parameters["Status In Progress"], currentStep, ref DynamicUserListsPanel, ref DynamicRadioButtonListPanel, ref btnSign, ref btnSign2, ref btnSave, ref btnSave2, ref btnOnHold, ref btnOnHold2, ref btnCancel, ref btnCancel2, ref btnDelete, ref btnDelete2, ref btnClose, ref btnClose2, ref HyperLinkPrint, ref btnAssign, ref btnAssign2, ref btnReject, ref btnReject2, ref ddlConfidential, ref groupRadioButtons, loggedUser, initiator, parameters, true, false, ref PlaceHolder_PreviousComments, ref PlaceHolder_NewComments, ref lblCommentRequired, ref PlaceHolder_GFTable, ref DocsMainButtons, ref DocsAbacButtons, ref DocsSupportingButtons, ref DocsPaperButtons, ref DocsSignedButtons, ref lblDocumentsCheckedOutWarning, ref PanelCheckedOutWarning, Web, item, wfid, wftypeOrder, wftypeName, ref WFID_Textbox, ref WFID_buttonAdd, actorsBackupDictionary, domain, userAD, passwordAD);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "CancelRejection " + ex.Message);
            }
        }

       
        /// <summary>
        /// Reject document changes during step signing.
        /// </summary>
        /// <param name="wftypeName"></param>
        /// <param name="Web"></param>
        /// <param name="loggedUser"></param>
        public void RejectDocumentChanges(SPListItem item, string wfid, SPWeb Web, SPUser loggedUser, int currentStep, SPList logsList, SPSite Site)
        {
            try
            {
                List<string> docsRemovedList = new List<string>();
               

                //Restore from Recycle Bin
                if (WorkflowDataManagement.HasRemovedRecentlyDocs(wfid, logsList, currentStep, ref docsRemovedList, loggedUser, item))
                    DocumentLibraries.RestoreDocFromRecycleBin(wfid, loggedUser, docsRemovedList, Site);
                //Remove Documents Created in the current Step
                DocumentLibraries.RemoveRecentlyCreatedDocs(item.ParentList, wfid, Web, loggedUser, currentStep, item);
                //Restore the correct version
                DocumentLibraries.RestoreRecentDocVersionsModule(item.ParentList, wfid, Web, loggedUser, currentStep, item);
                

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "RejectDocumentChanges() - " + ex.Message);
            }
        }

        public void RejectWorkflowLog(string wfid, SPWeb Web, SPUser loggedUser, SPList logsList, int currentStep, SPListItem item)
        {
            try
            {
                WorkflowDataManagement.RemoveRecentlyLogs(wfid, Web, logsList, currentStep, loggedUser, item);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "RejectWorkflowLog() - " + ex.Message);
            }
        }


        //CR20
        public static void RefreshPage()
        {
            try
            {
                HttpContext.Current.Response.Redirect(HttpContext.Current.Request.Url.ToString(), false);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "RefreshPage() - " + ex.Message);
            }
        }

        protected void ForbiddenRemoveDocument_Click(object sender, ImageClickEventArgs e)
        {
            PanelForbiddenRemoveDocument.Visible = false;
        }


    }
}
