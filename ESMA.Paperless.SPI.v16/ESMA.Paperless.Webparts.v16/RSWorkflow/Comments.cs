using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.SharePoint.Publishing.WebControls;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using System.ComponentModel;

using System.Configuration;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SharePoint.Utilities;

namespace ESMA.Paperless.Webparts.v16.RSWorkflow
{
    static class Comments
    {

        /// <summary>
        /// Load comments if workflow is not in draft status, otherwise, draws empty previous comments control
        /// </summary>
        /// <param name="wfid"></param>
        /// <param name="Web"></param>
        /// <param name="item"></param>
        /// <param name="PlaceHolder_PreviousComments"></param>
        /// <param name="parameters"></param>
        /// <param name="wfOrder"></param>
        /// <param name="wftypeName"></param>
        /// <param name="currentStep"></param>
        /// <returns>Last comment during draft status</returns>
        public static void LoadComments(string wfid, object comment, string previousComment, SPWeb Web, string status, PlaceHolder PlaceHolder_PreviousComments, PlaceHolder PlaceHolder_NewComments, string btnAssignClientID, string btnAssign2ClientID, string DynamicRadioButtonListPanelClientID, string lblCommentRequiredClientID, List<string> groupNames, Dictionary<string, string> parameters, string wfOrder, string wftypeName, int currentStep, SPList logList)
        {
            try
            {
                if (currentStep.Equals(1) && status.ToUpper().Equals(parameters["Status Draft"].ToUpper()))
                    Comments.DrawPreviousCommentsControl(null, PlaceHolder_PreviousComments, wfid);
                else
                    Comments.LoadPreviousComments(wfOrder, WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Commented.ToString()), wfid, PlaceHolder_PreviousComments, Web, logList, groupNames, parameters, wftypeName, currentStep);


                if (comment != null)
                    Comments.DrawMyCommentsControl(PlaceHolder_NewComments, btnAssignClientID, btnAssign2ClientID, DynamicRadioButtonListPanelClientID, lblCommentRequiredClientID, wfid, comment.ToString());
                else
                    Comments.DrawMyCommentsControl(PlaceHolder_NewComments, btnAssignClientID, btnAssign2ClientID, DynamicRadioButtonListPanelClientID, lblCommentRequiredClientID, wfid, previousComment);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "LoadComments - " + ex.Message);
            }
        }

        #region <MY COMMENTS>

        /// <summary>
        /// Initiate new comments text area
        /// </summary>
        /// <param name="commentsPlaceHolder"></param>
        /// <param name="wfid"></param>
        /// <param name="previousComment"></param>
        public static void DrawMyCommentsControl(PlaceHolder PlaceHolder_NewComments, string btnAssignClientID, string btnAssign2ClientID, string DynamicRadioButtonListPanelClientID, string lblCommentRequiredClientID, string wfid, string previousComment)
        {
            try
            {
                PlaceHolder_NewComments.Controls.Clear();

                TextBox txtHTML = new TextBox();
                txtHTML.ID = "MyCommentsTextBox";
                txtHTML.AutoPostBack = true;
                txtHTML.Text = previousComment;
                txtHTML.TextMode = TextBoxMode.MultiLine;
                txtHTML.TextChanged += new EventHandler(txtComments_TextChanged);
                string mandatoryCommentMessage = "It is mandatory to introduce the reason for rejecting the workflow.";
                string javascriptCode = "var radioButtonPanel = document.getElementById('" + DynamicRadioButtonListPanelClientID + "');if (radioButtonPanel.innerHTML.trim() !== ''){var newComment = document.getElementById('NewCommentsArea'); var newTextArea = newComment.getElementsByTagName('textarea'); var comment = newTextArea[0].innerHTML.trim(); if(document.getElementById('RejectionUserSelected').innerHTML !== '' && comment !==''){if(document.getElementById('" + btnAssignClientID + "')){document.getElementById('" + btnAssignClientID + "').disabled = false;document.getElementById('" + btnAssignClientID + "').className = 'btn_blue';} if(document.getElementById('" + btnAssign2ClientID + "')){document.getElementById('" + btnAssign2ClientID + "').disabled = false;document.getElementById('" + btnAssign2ClientID + "').className = 'btn_blue';} }else{if(document.getElementById('" + btnAssignClientID + "')){document.getElementById('" + btnAssignClientID + "').disabled = true;document.getElementById('" + btnAssignClientID + "').className = 'aspNetDisabled btn_blue';} if(document.getElementById('" + btnAssign2ClientID + "')){document.getElementById('" + btnAssign2ClientID + "').disabled = true;document.getElementById('" + btnAssign2ClientID + "').className = 'aspNetDisabled btn_blue';}}if (comment !==''){document.getElementById('" + lblCommentRequiredClientID + "').innerHTML = '';}else{document.getElementById('" + lblCommentRequiredClientID + "').innerHTML = '" + mandatoryCommentMessage + "';}}";
                txtHTML.Attributes.Add("onkeydown", javascriptCode + "return (event.keyCode!=13);");
                txtHTML.Attributes.Add("onchange", javascriptCode);
                txtHTML.Attributes.Add("onpaste", javascriptCode);
                txtHTML.Attributes.Add("oninput", javascriptCode);
                txtHTML.Attributes.Add("textInput", javascriptCode);
                txtHTML.Attributes.Add("onmouseover", javascriptCode);
                txtHTML.Attributes.Add("onblur", javascriptCode);
                txtHTML.Attributes.Add("onfocus", javascriptCode);
                txtHTML.Attributes.Add("onkeypress", javascriptCode);
                txtHTML.Attributes.Add("onkeyup", javascriptCode);

                //UpdatePanel
                //--------------------------------------------
                UpdatePanel updPanel = GeneralFields.DrawUpdatePanel(txtHTML.ID);
                updPanel.ContentTemplateContainer.Controls.Add(txtHTML);
                PlaceHolder_NewComments.Controls.Add(updPanel);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "DrawMyCommentsControl - " + ex.Message);
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void txtComments_TextChanged(object sender, EventArgs e)
        {
            try
            {
                TextBox txt = (TextBox)sender;
                string wfid = HttpContext.Current.Session["FormWFID"].ToString();
                RetainControlValueMyComments(txt.Text, wfid);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "txtComments_TextChanged - " + ex.Message);
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public static void RetainControlValueMyComments(string value, string wfid)
        {
            try
            {
                HttpContext.Current.Session["FormMyComment" + wfid] = value;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "RetainControlValueMyComments - " + ex.Message);
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="placeHolder"></param>
        /// <returns></returns>
        public static string GetMyComment(PlaceHolder newComments)
        {
            try
            {
                TextBox txt = (TextBox)newComments.FindControl("MyCommentsTextBox");
                return txt.Text.Trim();
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetMyComment - " + ex.Message);
                return string.Empty;
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="placeHolder"></param>
        /// <returns></returns>
        public static void DeleteMyComment(PlaceHolder newComments)
        {
            try
            {
                TextBox txt = (TextBox)newComments.FindControl("MyCommentsTextBox");
                txt.Text = "";
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "DeleteMyComment - " + ex.Message);
               
            }
        }

        #endregion

        #region <PREVIOUS COMMENTS>

        /// <summary>
        /// Load workflow previous comments and draw comments in HTML format within previous comments control
        /// </summary>
        /// <param name="order"></param>
        /// <param name="strCommentedAction"></param>
        /// <param name="wfid"></param>
        /// <param name="_placeHolder"></param>
        /// <param name="MyWeb"></param>
        /// <param name="LogList"></param>
        /// <param name="parameters"></param>
        /// <param name="wfName"></param>
        public static void LoadPreviousComments(string order, string strCommentedAction, string wfid, PlaceHolder _placeHolder, SPWeb MyWeb, SPList LogList, List<string> groupNames, Dictionary<string, string> parameters, string wfName, int currentStep)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                StringBuilder sbDecode = new StringBuilder();

                if (LogList != null)
                {
                    //CR20
                    string actionDeletedFile = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.DocumentRemoved.ToString());

                    List<List<string>> previousComments = GetPreviousComments(MyWeb, wfid, LogList, strCommentedAction, groupNames, parameters, wfName);
                    string actionConfidentialityChanged = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.ConfidentialityChanged.ToString());
                   
                   

                    if (!previousComments.Count.Equals(0))
                    {
                        sb.Append("<html>");
                        int cont = 0;

                        foreach (List<string> comment in previousComments)
                        {
                            if (cont.Equals(0))
                            {
                                if (comment[2].ToUpper().Equals(actionConfidentialityChanged.ToUpper()))
                                    sb.Append(comment[0] + " - <b><font color='red'>" + comment[1] + " " + comment[3] + "</font></b>");
                                //CR20
                                else if (comment[3].ToUpper().Equals(actionDeletedFile.ToUpper()))
                                {
                                    if (!string.IsNullOrEmpty(comment[2]))
                                        sb.Append(comment[0] + " - <b><font color='red'>" + comment[1] + " " + comment[4] + ". Comment: " + comment[2] + "</font></b>");
                                    else
                                        sb.Append(comment[0] + " - <b><font color='red'>" + comment[1] + " " + comment[4] + ".</font></b>");
                                }
                                else if (!string.IsNullOrEmpty(comment[2]))
                                    sb.Append(comment[0] + " - <b>" + comment[1] + " " + comment[3] + ": </b>" + comment[2]);
                                else
                                    sb.Append(comment[0] + " - <b>" + comment[1] + " " + comment[3] + " </b>");
                            }
                            else
                            {
                                if (comment[2].ToUpper().Equals(actionConfidentialityChanged.ToUpper()))
                                    sb.Append("<br>" + comment[0] + " - <b><font color='red'>" + comment[1] + " " + comment[3] + "</font></b>");
                                //CR20
                                else if (comment[3].ToUpper().Equals(actionDeletedFile.ToUpper()))
                                {
                                    if (!string.IsNullOrEmpty(comment[2]))
                                        sb.Append("<br>" + comment[0] + " - <b><font color='red'>" + comment[1] + " " + comment[4] + ". Comment: " + comment[2] + "</font></b>");
                                    else
                                        sb.Append("<br>" + comment[0] + " - <b><font color='red'>" + comment[1] + " " + comment[4] + ".</font></b>");
                                }
                                else if (!string.IsNullOrEmpty(comment[2]))
                                    sb.Append("<br>" + comment[0] + " - <b>" + comment[1] + " " + comment[3] + ": </b>" + comment[2]);
                                else
                                    sb.Append("<br>" + comment[0] + " - <b>" + comment[1] + " " + comment[3] + " </b>");
                            }

                            cont++;
                        }
                    }
                }

                DrawPreviousCommentsControl(sb, _placeHolder, wfid);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "loadPreviousComments() - " + ex.Message);
            }
        }
        
        /// <summary>
        /// Get previously added comments
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="WFID"></param>
        /// <param name="logList"></param>
        /// <param name="strCommentedAction"></param>
        /// <param name="parameters"></param>
        /// <param name="wfName"></param>
        /// <returns>Get previously added comments and each comment metadata</returns>
        public static List<List<string>> GetPreviousComments(SPWeb Web, string WFID, SPList logList, string strCommentedAction, List<string> groupNames, Dictionary<string, string> parameters, string wfName)
        {
            List<List<string>> comments = new List<List<string>>();

            try
            {
                string actionTakenCommented = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Commented.ToString());
                string actionTakenReassigned = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.ActorReAssigned.ToString());
                string actionConfidentialityChanged = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.ConfidentialityChanged.ToString());
                //CR20
                string actionTakenDeletedFile = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.DocumentRemoved.ToString());
                string actionCancelled = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Cancelled.ToString());
                string actionLaunched = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Launched.ToString());
      

                if (logList != null && parameters.ContainsKey("Status Draft"))
                {
                    try
                    {
                        SPQuery query = new SPQuery();
                        //CR20 CHANGE QUERY
                        query.Query = "<Where>"
                            + "<And><Eq><FieldRef Name='WFID' /><Value Type='Number'>" + WFID + "</Value></Eq>"
                            + "<And>"
                            + "<Or>"
                            + "<Eq><FieldRef Name='ActionTaken' /><Value Type='Choice'>" + actionTakenCommented + "</Value></Eq>"
                            + "<And><Or>"
                            + "<Eq><FieldRef Name='ActionTaken' /><Value Type='Choice'>" + actionTakenReassigned + "</Value></Eq>"
                            + "<Or>"
                            + "<Eq><FieldRef Name='ActionTaken' /><Value Type='Choice'>" + actionTakenDeletedFile + "</Value></Eq>"
                            + "<Eq><FieldRef Name='ActionTaken' /><Value Type='Choice'>" + actionConfidentialityChanged + "</Value></Eq>"
                            + "</Or>"
                            + "</Or>"
                            + "<Neq><FieldRef Name='WFStatus' /><Value Type='Text'>" + parameters["Status Draft"] + "</Value></Neq>"
                            + "</And></Or>"
                            + "<And>"
                            + "<IsNotNull><FieldRef Name='ActionDetails' /></IsNotNull>"
                            + "<Neq><FieldRef Name='ActionDetails' /><Value Type='Text'>" + actionTakenReassigned + "</Value></Neq>"
                            + "</And></And></And>"
                            + "</Where>"
                            + "<OrderBy><FieldRef Name='ID' Ascending='False' /></OrderBy>";

                        query.ViewFields = string.Concat(
                              "<FieldRef Name='WFID' />",
                              "<FieldRef Name='ActionTaken' />",
                              "<FieldRef Name='WFStatus' />",
                              "<FieldRef Name='ActionDetails' />",
                              "<FieldRef Name='Created' />",
                              "<FieldRef Name='StepNumber' />",
                              "<FieldRef Name='WorkflowComment' />",
                              "<FieldRef Name='Author' />");
                        query.ViewFieldsOnly = true; // Fetch only the data that we need

                        SPListItemCollection logRecordCollection = logList.GetItems(query);

                        if (!logRecordCollection.Count.Equals(0))
                        {
                            foreach (SPListItem logRecord in logRecordCollection)
                            {
                                if ((logRecord["ActionDetails"] != null) && (!(logRecord["ActionDetails"].ToString().StartsWith(actionCancelled + "."))))
                                    SetCommentsToShow(WFID, Web, logRecord, ref comments, groupNames, actionTakenCommented, parameters);
                            }

                            //Code commented to solved the issue PAPM-18 (JIRA)
                            //if (comments.Count.Equals(logRecordCollection.Count))
                            //{
                            //    int auxIndex = 0;
                            //    foreach (SPListItem logRecord in logRecordCollection)
                            //    {
                            //        if (!auxIndex.Equals(0) && logRecord["StepNumber"].Equals(logRecordCollection[auxIndex - 1]["StepNumber"]) && logRecord["ActionTaken"].Equals(actionTakenReassigned))
                            //            comments[auxIndex][2] = string.Empty;

                            //        auxIndex++;
                            //    }
                            //}
                        }
                    }
                    catch (Exception ex)
                    {
                        General.saveErrorsLog(WFID, "GetPreviousComments() - " + ex.Message);
                    }
                }
                else
                {
                    string message = "The list '" + logList.Title.ToString() + "' does not exist.";
                    General.saveErrorsLog(WFID, "GetPreviousComments() - " + message);
                }
                return comments;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(WFID, "GetPreviousComments() - " + ex.Message);
                return comments;
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wfid"></param>
        /// <param name="Web"></param>
        /// <param name="item"></param>
        /// <param name="previousComments"></param>
        /// <param name="actionTakenCommented"></param>
        private static void SetCommentsToShow(string wfid, SPWeb Web, SPListItem item, ref List<List<string>> previousComments, List<string> groupNames, string actionTakenCommented, Dictionary<string,string> parameters)
        {
            try
            {
                List<string> comment = new List<string>();
               
                string actionTakenReassigned = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.ActorReAssigned.ToString());
                string actionConfidentialityChanged = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.ConfidentialityChanged.ToString());
                string actionDetailLaunched = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Launched.ToString());
                //CR20
                string actionTakenDeletedFile = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.DocumentRemoved.ToString());

                int stepNumber = 0;
                stepNumber = item["StepNumber"]!=null?int.Parse(item["StepNumber"].ToString()):0;
                string stepGroupName = General.GetGroupName(groupNames[stepNumber - 1], parameters);

               

                if (stepNumber > 0)
                {
                    // [IR22765] Dates of the creation and launch > Add to solve error date when launched a WF
                   if (item["ActionDetails"].ToString().ToUpper().Equals(actionDetailLaunched.ToUpper()))
                        comment.Add(string.Format("{0:dd/MM/yyyy HH:mm:ss}", item["Modified"]));
                    else
                        comment.Add(string.Format("{0:dd/MM/yyyy HH:mm:ss}", item["Created"]));


                   SPUser user = General.GetSPUser(item, "Author", wfid, Web);


                    //COMMENT[1] - USER
                    comment.Add(stepGroupName + " - " + user.Name);

                    //COMMENT[2] - COMMENT
                    if ((item["WorkflowComment"] != null))
                    {
                        string replacedComment = item["WorkflowComment"].ToString();

                        if (replacedComment.Contains("SIGNED: "))
                            replacedComment = replacedComment.Replace("SIGNED: ", string.Empty);
                        //delete re-assigend repeat comments PAPBUG-119
                        if (item["ActionTaken"] != null && item["ActionTaken"].ToString().ToUpper().Equals(actionTakenReassigned.ToUpper()))
                        {
                            if (!item["ActionDetails"].ToString().ToUpper().Contains("STEP: " + item["StepNumber"].ToString()))
                                 replacedComment = "";
                        }
                        // end delete re-assigend repeat comments PAPBUG-119
                        comment.Add(replacedComment);
                    }
                    else if (item["ActionTaken"] != null && item["ActionTaken"].ToString().ToUpper().Equals(actionConfidentialityChanged.ToUpper()))
                        comment.Add(actionConfidentialityChanged);
                    else
                        comment.Add(string.Empty);

                    //COMMENT[3] - DESCRIPTION
                    if (item["ActionTaken"] != null && item["ActionTaken"].ToString().ToUpper().Equals(actionTakenReassigned.ToUpper()))
                    {
                        comment.Add(GetReassignementComment(item["ActionDetails"].ToString(), groupNames, parameters, user, Web, wfid));
                        comment[1] = string.Empty;
                    }
                    //CR20
                    else if (item["ActionTaken"] != null && item["ActionTaken"].ToString().ToUpper().Equals(actionTakenDeletedFile.ToUpper()))
                        comment.Add(actionTakenDeletedFile);
                    else
                        comment.Add(item["ActionDetails"] != null ? item["ActionDetails"].ToString() : string.Empty);

                    //COMMENT[4] - ACTION DETAILS
                    if (item["ActionTaken"] != null && item["ActionTaken"].ToString().ToUpper().Equals(actionTakenDeletedFile.ToUpper()))
                        comment.Add(item["ActionDetails"] != null ? item["ActionDetails"].ToString() : string.Empty);

                    //COMMENT ADD TO COMMENT LIST
                    previousComments.Add(comment);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetCommentsToShow1() - " + ex.Message);
            }
        }

        public static string GetReassignementComment(string commentDetail, List<string> groupNames, Dictionary<string, string> parameters, SPUser loggedUser, SPWeb Web, string wfid)
        {
            string detail = commentDetail;

            try
            {
                int indexColon = detail.IndexOf(":");
                int indexStop = detail.IndexOf(".");
                int indexPreviousActor = detail.IndexOf("Previous actor:");
                int indexCurrentActor = detail.IndexOf(". Current actor:");
                string loginName1 = detail.Substring(indexPreviousActor + "Previous actor:".Length);
                loginName1 = loginName1.Substring(0, loginName1.IndexOf(". Current actor:")).Trim();
                string loginName2 = detail.Substring(indexCurrentActor + ". Current actor:".Length);
                loginName2 = loginName2.Substring(0, loginName2.Length - 1).Trim();

                int stepNumber = 0;
                string step = detail.Substring(indexColon + 1, indexStop - indexColon - 1).Trim();
                int.TryParse(step, out stepNumber);
                string stepGroupName = General.GetGroupName(groupNames[stepNumber - 1], parameters);

                if (parameters.ContainsKey("Domain"))
                {
                    string domain = parameters["Domain"].ToUpper();
                    SPUser user1 = null;
                    SPUser user2 = null;
                    string user1Name = string.Empty;
                    string user2Name = string.Empty;

                    if (!string.IsNullOrEmpty(loginName1))
                    {
                        if (loginName1.ToUpper().Contains(domain))
                            user1 = Web.Site.RootWeb.EnsureUser(loginName1);
                        else
                            user1 = Web.Site.RootWeb.EnsureUser(domain + "\\" + loginName1);

                        if (user1 != null)
                            user1Name = user1.Name;
                    }
                    else
                        user1Name = "No Actor";

                    if (!string.IsNullOrEmpty(loginName2))
                    {
                        if (loginName2.ToUpper().Contains(domain))
                            user2 = Web.Site.RootWeb.EnsureUser(loginName2);
                        else
                            user2 = Web.Site.RootWeb.EnsureUser(domain + "\\" + loginName2);

                        if (user2 != null)
                            user2Name = user2.Name;
                    }
                    else
                        user2Name = "No Actor";

                    if (user1 != null && loggedUser.ID.Equals(user1.ID))
                        detail = user1Name + " re-assigned step " + stepGroupName + " to " + user2Name;
                    else if (user2 != null && loggedUser.ID.Equals(user2.ID))
                        detail = loggedUser.Name + " re-assigned step " + stepGroupName + " from " + user1Name + " to him/herself";
                    else if (user1 != null && user2 != null && !loggedUser.ID.Equals(user1.ID) && !loggedUser.ID.Equals(user2.ID))
                        detail = loggedUser.Name + " re-assigned step " + stepGroupName + " from " + user1Name + " to " + user2Name;
                    else if ((user1 != null && user2 == null) ||(user1 == null && user2 != null))
                        detail = loggedUser.Name + " re-assigned step " + stepGroupName + " from " + user1Name + " to " + user2Name;

                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetReassignementComment() - " + ex.Message);
            }

            return detail; 
        }

        /// <summary>
        /// Draw previous comment control and define its styles and data format
        /// </summary>
        /// <param name="value"></param>
        /// <param name="placeHolder"></param>
        /// <param name="wfid"></param>
        public static void DrawPreviousCommentsControl(StringBuilder value, PlaceHolder placeHolder, string wfid)
        {
            try
            {
                placeHolder.Controls.Clear();

                Label lblHTML = new Label();
                lblHTML.ID = "RichTextBoxPreviousComments";

                string pattern = @"(?<start><a[^>]*)(?<end>>)";
                string repl = @"${start} target=""_blank"" ${end}";

                if (value != null)
                {
                    lblHTML.Text = value.ToString();

                    if (value.ToString().Contains("<a href="))
                        lblHTML.Text = Regex.Replace(lblHTML.Text, pattern, repl);
                }

                placeHolder.Controls.Add(lblHTML);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "DrawPreviousCommentsControl() - " + ex.Message);
            }
        }
        #endregion
        
        //RS23
        #region <PREVIOUS COMMENTS CLOSED>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="placeHolder"></param>
        /// <returns></returns>
        public static string GetMyCommentClosed(TextBox TextBox)
        {
            try
            {
                TextBox txt = (TextBox)TextBox.FindControl("TextBoxNewCommentsClosed");
                return txt.Text.Trim().ToString();
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetMyCommentClosed - " + ex.Message);
                return string.Empty;
            }
        }
        #endregion

        #region <PREVIOUS COMMENTS DELETED FILE>
        //CR20
        /// <summary>
        /// 
        /// </summary>
        /// <param name="placeHolder"></param>
        /// <returns></returns>
        public static string GetMyCommentDeletedFile(TextBox TextBox)
        {
            try
            {
                TextBox txt = (TextBox)TextBox.FindControl("TextBoxCommentsDeletedFile");
                return txt.Text.Trim().ToString();
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetMyCommentDeletedFile - " + ex.Message);
                return string.Empty;
            }
        }
        #endregion

        //ESMA-CR31-BackupGroups
        public static void SetReassigningToBackupComment(string wfid, string status, Dictionary<string, string> parameters, string wftypeOrder, int currentStep, string confidentialValue, SPList logList, SPWeb Web, SPUser realEditor, SPListItem item)
        {
            string actionTaken = string.Empty;
            SPUser responsible = null;
            string fieldName = "Step_x0020_" + currentStep.ToString() + "_x0020_Assigned_x0020_To";

            try
            {
                if (status.ToLower().Equals(parameters["Status In Progress"].ToLower()))
                    actionTaken = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.BackupSigned.ToString());
                else if (status.ToLower().Equals(parameters["Status Rejected"].ToLower()))
                    actionTaken = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.BackupRejected.ToString());
                else
                    actionTaken = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.BackupOnHold.ToString());

                //Old assigned person
                if (item[fieldName] != null)
                    responsible = General.GetSPUser(item, fieldName, wfid, Web);
                

                string computerName = General.GetComputerName(HttpContext.Current);
                string actionDetails = "Previous actor: " + Permissions.GetOnlyUserAccount(responsible.LoginName, wfid).ToUpper() + ". Current actor: " + Permissions.GetOnlyUserAccount(realEditor.LoginName,wfid).ToUpper() + ".";


                //Log taken action
                WorkflowDataManagement.CreateWorkflowLog(wftypeOrder, wfid, currentStep, status, responsible, actionTaken, actionDetails, computerName, string.Empty, confidentialValue, logList, Web, parameters, realEditor, true);


            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetReassigningToBackupComment() " + ex.Message);
            }
        }

    }
}
