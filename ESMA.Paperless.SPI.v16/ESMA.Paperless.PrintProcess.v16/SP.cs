using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using System.Configuration;
using System.Data;
using System.IO;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using System.Web;

namespace ESMA.Paperless.PrintProcess.v16
{
    class SP
    {
       
        #region <FOLDER>

        public static bool ExistPrintDocument(string printedDocumentName, SPWeb Web, string WFID, string urlWF)
        {
            bool existDocument = false;

            try
            {
                string urlPrintedFile = General.CombineURL(WFID, urlWF, printedDocumentName);   
                SPFile file = Web.GetFile(urlPrintedFile);

                if (file.Exists)
                    existDocument = true;
                else
                    existDocument = false;
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "ExistPrintDocument() - " + ex.Message.ToString());
            }

            return existDocument;
        }

        public static List<string> GetPrintDocuments(SPList WFLibrary)
        {
            List<String> printedwfs = new List<String>();

            try
            {
                SPQuery query = new SPQuery();
                query.ViewFields = "<FieldRef Name=\"WFID\"/>";
                query.ViewAttributes = "Scope=\"Recursive\"";
                query.Query = "<Where><Eq><FieldRef Name='DocumentationType' /><Value Type='Lookup'>Printed Document</Value></Eq></Where>";

                SPListItemCollection docCollection = WFLibrary.GetItems(query);

                foreach (SPListItem item in docCollection)
                {
                    printedwfs.Add(item["WFID"].ToString());
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(string.Empty, "GetPrintDocuments() - " + ex.Message.ToString());
            }

            return printedwfs;
        }

        public static SPFolder GetWFIDFolder(string WFID, SPWeb Web, SPList myList, string printedDocumentName)
        {

            try
            {
                SPFolder folder = null;
                string urlList = General.GetDocumentLibraryURL(myList.DefaultViewUrl);
                string urlWF = General.CombineURL(WFID, urlList, WFID);

                folder = Web.GetFolder(urlWF);

                return folder;
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetWFIDFolder() - " + ex.Message.ToString());
                return null;
            }
        }

        #endregion

        #region <SEARCH WORKFLOW INFORMATION>

        //InitialGeneralFields -> Values
        public static void GetInitialGeneralFieldsValues(string WFID, ref Dictionary<string,string> generalFieldsDictionary, SPListItem item)
        {
            string confidentialColumnName = "ConfidentialWorkflow";
            
            try
            {

                if (item["InitialGeneralFields"] != null)
                {
                    //Adding the "Confidential" Field
                    string generalFields = confidentialColumnName + ";#" + item["InitialGeneralFields"].ToString();

                    string[] generalFieldsColumnName = Regex.Split(generalFields, ";#");

                    foreach (string columnName in generalFieldsColumnName)
                    {
                        try
                        {
                            if (item.Fields.ContainsField(columnName))
                            {
                                SPField internalField = item.Fields.GetFieldByInternalName(columnName);

                                if (internalField != null)
                                {
                                    string displayName = internalField.Title.ToString();

                                    if (!generalFieldsDictionary.ContainsKey(displayName))
                                    {
                                        if (item[columnName] != null)
                                        {
                                            string value = item[columnName].ToString();
                                            generalFieldsDictionary.Add(displayName, value);
                                        }
                                        else
                                        {
                                            generalFieldsDictionary.Add(displayName, string.Empty);
                                        }
                                    }
                                }
                            }
                        }
                        catch(Exception ex)
                        {
                            General.SaveErrorsLog(WFID, "GetInitialGeneralFieldsValues() - GF: '" + columnName + "' " + ex.Message.ToString());
                            continue; 
                        }
                    }

                }
              
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "SearchGeneralFieldsValues() - " + ex.Message.ToString());
       
            }

        }

        //InitialSteps
        public static List<string> GetWorkflowInitialSteps(SPListItem item, string WFID)
        {
            List<string> groupNames = new List<string>();

            try
            {
                if (item["InitialSteps"] != null)
                {
                    string[] steps = Regex.Split(item["InitialSteps"].ToString(), "&#");

                    int count = 0;

                    foreach (string step in steps)
                    {
                        string[] stepRecord = Regex.Split(steps[count].ToString(), ";#");
                        groupNames.Add(stepRecord[2].Split('\\')[1]);
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetWorkflowInitialSteps() - " + ex.Message);
            }

            return groupNames;
        }

        //Step X Assigned To
        public static string GetWorkflowStepAssignedTo(SPListItem item, string WFID, int stepNumber)
        {
            string responsibleUser = string.Empty;

            try
            {
                string fieldName = "Step " + stepNumber.ToString() + " Assigned To";

                if (item[fieldName] != null)
                    responsibleUser = item[fieldName].ToString();
                
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetWorkflowStepAssignedTo() - " + ex.Message);
            }

            return responsibleUser;
        }

        //InitialStepDescriptions
        public static Dictionary<int, string> GetWorkflowInitialStepDescriptions(SPListItem item, string WFID)
        {
            Dictionary<int, string> descriptionDictionary = new Dictionary<int, string>();
            List<string> listDescription = new List<string>();

            try
            {
                if (item["InitialStepDescriptions"] != null)
                {
                    string initialStepDescriptionValue = item["InitialStepDescriptions"].ToString();
                    string[] descriptions = Regex.Split(initialStepDescriptionValue, ";#");
                   
                    
                    int contStep = 0;

                    foreach (string description in descriptions)
                    {
                        //string plainText = SPHttpUtility.ConvertSimpleHtmlToText(description, description.Length);
                        string plainText = Regex.Split(description, "%#")[0];
                        plainText = plainText.Replace("</p><p>", "\r\n").Replace("<p>", "").Replace("</p>", "");
                        plainText = SPHttpUtility.ConvertSimpleHtmlToText(plainText, plainText.Length);
                        
                        if (contStep != 0)
                            descriptionDictionary.Add(contStep, plainText);
                        
                        contStep = contStep + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetWorkflowInitialStepDescriptions() " + ex.Message);
            }
            return descriptionDictionary;
        }

        #endregion

        #region <SEARCH COMMENTS>

        //Previous Comments
        public static List<List<string>> GetPreviousComments(SPWeb Web, string WFID, SPList logList, Dictionary<string, string> parameters, List<string> groupNamesList)
        {
            List<List<string>> comments = new List<List<string>>();
 
            try
            {
               string actionTakenCommented = "Commented";
               string actionTakenActionReassigned = "Action re-assigned";
               string actionTakenConfidentiality = "Restriction changed";
               string actionTakenDeletedFile = "Document removed"; //CR20
               string actionCancelled = "Cancelled"; //CR20
             

               SPQuery query = new SPQuery();
               query.Query = @"<Where><And>"
                             + "<Eq><FieldRef Name='WFID' /><Value Type='Number'>" + WFID + "</Value></Eq>"
                             + "<And>"
                             + "<Or><Eq><FieldRef Name='ActionTaken' /><Value Type='Choice'>" + actionTakenCommented + "</Value></Eq>"
                             + "<And>"
                             + "<Or><Eq><FieldRef Name='ActionTaken' /><Value Type='Choice'>" + actionTakenActionReassigned + "</Value></Eq>"
                             + "<Or><Eq><FieldRef Name='ActionTaken' /><Value Type='Choice'>" + actionTakenDeletedFile + "</Value></Eq>"
                             + "<Eq><FieldRef Name='ActionTaken' /><Value Type='Choice'>" + actionTakenConfidentiality + "</Value></Eq>"
                             + "</Or></Or>"
                             + "<Neq><FieldRef Name='WFStatus' /><Value Type='Text'>Draft</Value></Neq>"
                             + "</And></Or>"
                             + "<And>"
                             + "<IsNotNull><FieldRef Name='ActionDetails' /></IsNotNull>"
                             + "<Neq><FieldRef Name='ActionDetails' /><Value Type='Text'>" + actionTakenActionReassigned + "</Value></Neq>"
                             + "</And></And></And></Where>"
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

               foreach (SPListItem logRecord in logRecordCollection)
               {
                   List<string> comment = new List<string>();
                   
                   if ((logRecord["ActionDetails"] != null) && (!(logRecord["ActionDetails"].ToString().StartsWith(actionCancelled + "."))))
                        SetCommentsToShow(WFID, Web, logRecord, ref comments, groupNamesList, parameters, actionTakenDeletedFile);
               }
                        

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetPreviousComments() - " + ex.Message);
            }

            return comments;
        }

        private static void SetCommentsToShow(string WFID, SPWeb Web, SPListItem logRecordItem, ref List<List<string>> previousComments, List<string> groupNamesList, Dictionary<string, string> parameters, string actionTakenDeletedFile)
        {
            try
            {
                List<string> comment = new List<string>();
                int stepNumber = 0;
                string actionTakenReassigned = "Action re-assigned";
                string actionConfidentialityChanged = "Restriction changed";
                string actionDetailLaunched = "Launched";


                stepNumber = logRecordItem["StepNumber"] != null ? int.Parse(logRecordItem["StepNumber"].ToString()) : 0;

                if (groupNamesList.Count() > (stepNumber - 1))
                {
                    string stepGroupName = Form.GetGroupNameDefinition(groupNamesList[stepNumber - 1], parameters, WFID);


                    if (stepNumber > 0)
                    {

                        string actionDetails = logRecordItem["ActionDetails"].ToString();
                        string actionTaken = string.Empty;

                        if (logRecordItem["ActionTaken"] != null)
                            actionTaken = logRecordItem["ActionTaken"].ToString().ToUpper();

                        //COMMENT[0] - CREATED DATE
                        //-----------------------------------------------------------------------------
                        if (actionDetails.ToUpper().Equals(actionDetailLaunched.ToUpper()))
                            comment.Add(string.Format("{0:dd/MM/yyyy HH:mm:ss}", logRecordItem["Modified"]));
                        else
                            comment.Add(string.Format("{0:dd/MM/yyyy HH:mm:ss}", logRecordItem["Created"]));  // [IR22765] Dates of the creation and launch > Add to solve error date when launched a WF


                        //COMMENT[1] - USER
                        //-----------------------------------------------------------------------------
                        SPFieldUserValue userValue = new SPFieldUserValue(Web, logRecordItem["Author"].ToString());
                        string userName = userValue.LookupValue.ToString();
                        comment.Add(stepGroupName + " - " + userName);

                        //COMMENT[2] - COMMENT
                        //-----------------------------------------------------------------------------
                        if ((logRecordItem["WorkflowComment"] != null))
                        {
                            string replacedComment = logRecordItem["WorkflowComment"].ToString();

                            if (replacedComment.Contains("SIGNED: "))
                                replacedComment = replacedComment.Replace("SIGNED: ", string.Empty);

                            //delete re-assigned repeat comments PAPBUG-119
                            if (actionTaken.Equals(actionTakenReassigned.ToUpper()))
                            {
                                if (!actionDetails.Contains("STEP: " + logRecordItem["StepNumber"].ToString()))
                                    replacedComment = "";
                            }

                            comment.Add(replacedComment);
                        }

                        else if (actionTaken.Equals(actionConfidentialityChanged.ToUpper()))
                            comment.Add(actionConfidentialityChanged);
                        else
                            comment.Add(string.Empty);

                        //COMMENT[3] - DESCRIPTION
                        //-----------------------------------------------------------------------------
                        if (actionTaken.Equals(actionTakenReassigned.ToUpper()))
                        {
                            comment.Add(GetReassignementComment(actionDetails, groupNamesList, parameters, userValue.User, Web, WFID));
                            comment[1] = string.Empty;
                        }
                        //CR20
                        else if (actionTaken.Equals(actionTakenDeletedFile.ToUpper()))
                        {
                            string commentToShow = string.Empty;

                            if (logRecordItem["WorkflowComment"] != null)
                                commentToShow = logRecordItem["ActionDetails"].ToString() + ". Comment: " + logRecordItem["WorkflowComment"].ToString();
                            else
                                commentToShow = logRecordItem["ActionDetails"].ToString();

                            comment.Add(commentToShow);
                        }
                        else
                            comment.Add(logRecordItem["ActionDetails"] != null ? actionDetails : string.Empty);

                        //COMMENT ADD TO COMMENT LIST
                        previousComments.Add(comment);
                    }
                }
                
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "SetCommentsToShow() - " + ex.Message);
            }
        }

        //RS37 -> Re-assigned Comments
        public static string GetReassignementComment(string commentDetail, List<string> groupNamesList, Dictionary<string, string> parameters, SPUser loggedUser, SPWeb Web, string WFID)
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
                string stepGroupName = Form.GetGroupNameDefinition(groupNamesList[stepNumber - 1], parameters, WFID);

                if (parameters.ContainsKey("Domain"))
                {
                    string domain = parameters["Domain"].ToUpper();
                    SPUser user1 = null;
                    SPUser user2 = null;
                    string user1Name = string.Empty;
                    string user2Name = string.Empty;

                    Web.AllowUnsafeUpdates = true;

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
                    else if ((user1 != null && user2 == null) || (user1 == null && user2 != null))
                        detail = loggedUser.Name + " re-assigned step " + stepGroupName + " from " + user1Name + " to " + user2Name;
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetReassignementComment() - " + ex.Message);
            }

            return detail;
        }


        //Previous Comments Closure (CR37)
        public static List<List<string>> GetPreviousCommentsClosure(string WFID, SPList logList)
        {
            List<List<string>> comments = new List<List<string>>();

            try
            {
                    string actionTakenCommentClosed = "CommentedClosed";

                    SPQuery query = new SPQuery();
                    query.Query = @"<Where><And>"
                                  + "<Eq><FieldRef Name='WFID'/><Value Type='Text'>" + WFID + "</Value></Eq>"
                                  + "<Eq><FieldRef Name='ActionTaken'/><Value Type='Choice'>" + actionTakenCommentClosed + "</Value></Eq>"
                                  + "</And></Where>"
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
                            List<string> comment = new List<string>();
                            comment.Add(logRecord["Created"].ToString());
                            comment.Add(logRecord["Author"].ToString().Split('#')[1]);
                            comment.Add(logRecord["WorkflowComment"].ToString());
                            comments.Add(comment);
                        }
                    }
  
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetPreviousCommentsClosure() - " + ex.Message);
            }

            return comments;
        }


        #endregion

        #region <SEARCH DOCUMENTS INFORMATION>

        public static int CountSubFolders(string WFID, SPWeb Web, SPList myList)
        {

            try
            {
                int numSubFolder = 0;

                SPFolder folder = null;
                string urlList = General.GetDocumentLibraryURL(myList.DefaultViewUrl);
                string urlWF = General.CombineURL(WFID, urlList, WFID);

                folder = Web.GetFolder(urlWF);

                if (folder != null)
                {
                    if (folder.ItemCount > 0)
                    {
                        //QUERY
                        SPQuery query = new SPQuery();
                        query.Folder = folder;
                        SPFolderCollection folderCollection = folder.SubFolders;

                        if (folderCollection.Count > 0)
                        {
                            foreach (SPFolder subfolder in folderCollection.Folder.SubFolders)
                            {
                                numSubFolder = folderCollection.Folder.SubFolders.Count; 
                            }
                        }

                    }
                }

                return numSubFolder;
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "CountSubFolders() - " + ex.Message.ToString());
                return 0;
            }
        }
    
        public static List<string> GetTitleDocuments(string WFID, SPFolder folder, string documentTypeSearched)
        {

            List<string> titleDocumentsName = new List<string>();

            try
            {
                if (folder.ItemCount > 0)
                {
                    //QUERY
                    SPQuery query = new SPQuery();
                    query.Folder = folder;
                    SPFolderCollection folderCollection = folder.SubFolders;

                    if (folderCollection.Count > 0)
                    {
                        foreach (SPFolder subfolder in folderCollection.Folder.SubFolders)
                        {
                            //Main
                            string subFolderName = subfolder.Name.ToString();
                            
                            if (subFolderName.ToLower() == documentTypeSearched.ToLower())
                            {
                                if (subfolder.ItemCount > 0)
                                {
                                    foreach (SPFile file in subfolder.Files)
                                    {
                                        string fileName = file.Item.Name;
                                        titleDocumentsName.Add(fileName);
                                    }

                                }

                                break;
                            }
                        }

                    }

                }

                return titleDocumentsName;
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetTitleDocuments() - " + ex.Message.ToString());
                return null;
            }
        }

        public static void SetDocumentsMetadata(ref SPListItem itm, string WFID, string documentationType, string stepNumber)
        {
            try
            {
                if (itm.Fields.ContainsFieldWithStaticName("DocumentationType"))
                {
                    itm["ContentTypeId"] = SP.GetIDContentType(itm.ParentList, WFID);
                    itm["WFID"] = WFID;
                    itm["DocumentationType"] = documentationType;
                    itm["StepNumber"] = stepNumber;
                }

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "SetDocumentsMetadata() - " + ex.Message.ToString());
            }
        }

        public static SPContentTypeId GetIDContentType(SPList list, string WFID)
        {
            SPContentTypeId id = new SPContentTypeId();

            try
            {
                foreach (SPContentType ct in list.ContentTypes)
                {
                    try
                    {
                        if (ct.Name.ToUpper().Equals("WORKFLOW DOCUMENT"))
                        {
                            id = ct.Id;
                            break;
                        }


                    }
                    catch { continue; }
                }

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetIDContentType - " + ex.Message.ToString());
            }

            return id;
        }

        public static List<string> GetDocumentationType(SPWeb Web)
        {
             List<string> documentationTypeList = new  List<string>();

            try
            {
                SPFieldChoice choices = new SPFieldChoice(Web.Fields, "DocumentationType");

                foreach (string choice in choices.Choices)
                {
                    if (choice != "(Empty)")
                        documentationTypeList.Add(choice);
                }

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(string.Empty, "GetDocumentationType - " + ex.Message.ToString());
            }

            return documentationTypeList;
        }


        #endregion

        #region <SEARCH LOGS>

        public static SPListItemCollection SearchWorkflowLogs(string WFID, SPWeb Web, SPList logList)
        {
            SPListItemCollection itemCol = null;

            try
            {
               
                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='WFID'/><Value Type='Text'>" + WFID + "</Value></Eq></Where><OrderBy><FieldRef Name='Created' Ascending='TRUE' /></OrderBy>";
                query.ViewFields = string.Concat(
                             "<FieldRef Name='WFID' />",
                             "<FieldRef Name='Created' />",
                             "<FieldRef Name='StepNumber' />",
                             "<FieldRef Name='WFStatus' />",
                             "<FieldRef Name='ActionTaken' />",
                             "<FieldRef Name='AssignedPerson' />",
                             "<FieldRef Name='ComputerName' />",
                             "<FieldRef Name='ActionDetails' />",
                             "<FieldRef Name='WorkflowComment' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need
                itemCol = logList.GetItems(query);
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "SearchWorkflowLogs() " + ex.Message);
            }

            return itemCol;
        }


        #endregion

      
        public static SPFieldType GetColumnType(SPWeb Web, string columnName, string WFName, string WFID)
        {
            try
            {
                SPField field = null;
                SPFieldType fieldType = 0;

                //Fields -> Column [Paperless]
                try
                {
                    field = Web.Fields[columnName];
                }
                //Fields -> Column [GKMF]
                catch
                {
                    field = Web.Site.RootWeb.Fields[columnName];
                }

                if (field != null)
                {
                    fieldType = field.Type;
                }
                else
                {
                    string message = "The Column '" + columnName + "' does not exist in '" + WFName + "'.";
                    General.SaveErrorsLog(WFID, "GetColumnType() - " + message);
                }

                return fieldType;

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetColumnType() - " + ex.Message.ToString());
                return 0;
            }

        }

       


    }
}
