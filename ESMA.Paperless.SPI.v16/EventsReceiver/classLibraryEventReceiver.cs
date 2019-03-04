using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;
using System.ComponentModel;
using System.Reflection;

namespace ESMA.Paperless.EventsReceiver.v16.EventsReceiver
{
    static class classLibraryEventReceiver
    {
        public enum ActionsEnum
        {
            [Description("New document version")]
            NewDocumentVersion,
            [Description("Document uploaded")]
            NewDocument,
            [Description("Document removed")]
            DocumentRemoved,
            [Description("Document moved")]
            DocumentMoved,
            [Description("Try remove document")]
            TryRemoveDocument
        }

        #region <INFORMATION>

        /// <summary>
        /// Get the name of the computer that is processing any workflow processing change.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Name of the host or client which is processing the workflow.</returns>
        public static string GetComputerName(HttpContext context)
        {
            try
            {
                System.Net.IPHostEntry host = new System.Net.IPHostEntry();
                host = System.Net.Dns.GetHostEntry(context.Request.ServerVariables["REMOTE_HOST"]);

                //Split out the host name from the FQDN

                return host.HostName.ToString();
            }
            catch
            {
                return System.Environment.MachineName;
            }
        }

        public static Dictionary<string, string> GetWFMetadata(SPListItem itemDoc, string wfid, SPWeb web, SPItemEventProperties properties)
        {
            Dictionary<string, string> metadataDictionary = new Dictionary<string, string>();

            try
            {
                SPList list = itemDoc.ParentList;
                SPQuery query = new SPQuery();

                query.ViewFields = "<FieldRef Name=\"WFID\"/><FieldRef Name=\"WFType\"/><FieldRef Name=\"WFStatus\"/><FieldRef Name=\"AssignedPerson\"/><FieldRef Name=\"ConfidentialWorkflow\"/><FieldRef Name=\"StepNumber\"/><FieldRef Name=\"WFLink\"/>";
                query.Query = "<Where><Eq><FieldRef Name='WFID'/><Value Type='Number'>" + wfid + "</Value></Eq></Where>";
                SPListItemCollection itemCollection = list.GetItems(query);

                if (itemCollection.Count > 0)
                {
                    SPListItem itmWF = itemCollection[0];
                    SPUser userAssigned = GetWorkflowCurrentStepResponsible(itmWF, web, wfid, properties);

                    metadataDictionary.Add("WFType", itmWF["WFType"].ToString()); //Text
                    metadataDictionary.Add("WFStatus", itmWF["WFStatus"].ToString()); //Choice
                    metadataDictionary.Add("StepNumber", itmWF["StepNumber"].ToString()); //Number

                    if (userAssigned != null)
                        metadataDictionary.Add("AssignedPerson", userAssigned.LoginName); //User
                    else
                        metadataDictionary.Add("AssignedPerson", null); //User

                    metadataDictionary.Add("ConfidentialWorkflow", itmWF["ConfidentialWorkflow"].ToString()); //Choice

                    //WFLInk
                    if (itmWF["WFLink"] != null)
                    {
                        SPFieldUrlValue fieldValue = new SPFieldUrlValue(itmWF["WFLink"].ToString());
                        string linkTitle = fieldValue.Description;
                        string linkUrl = fieldValue.Url;
                        metadataDictionary.Add("WFLink", linkUrl);
                    }
                    else
                        metadataDictionary.Add("WFLink", string.Empty);

                }
            }
            catch (Exception ex)
            {
                SaveErrorsLog_EventsReceiver(properties, wfid, "GetWFMetadata() " + ex.Message);
            }

            return metadataDictionary;
        }

        public static string GetActionDescription(string value)
        {
            Type type = typeof(ActionsEnum);
            MemberInfo[] enumInfo = type.GetMember(value);
            object[] attributes = enumInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            string description = ((DescriptionAttribute)attributes[0]).Description;
            return description;
        }

        /// <summary>
        /// Get workflow current step responsible.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <returns>SharePoint user object of the current step responsible</returns>
        public static SPUser GetWorkflowCurrentStepResponsible(SPListItem item, SPWeb Web, string wfid, SPItemEventProperties properties)
        {
            SPUser result = null;

            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsFieldWithStaticName("AssignedPerson") && item["AssignedPerson"] != null)
                    {
                        try
                        {
                            SPFieldUserValue userValue = new SPFieldUserValue(Web, item["AssignedPerson"].ToString());
                            if (userValue != null)
                                result = userValue.User;
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                SaveErrorsLog_EventsReceiver(properties, wfid, "GetWorkflowCurrentStepResponsible() " + ex.Message);
            }

            return result;
        }

        public static SPList GetLogsList(string wfid, string wfType, SPWeb web, SPItemEventProperties properties)
        {
            SPList logsList = null;

            try
            {
                SPList list = web.GetListFromWebPartPageUrl(web.Url + "/Lists/WFConfiguration/AllItems.aspx");
                SPQuery query = new SPQuery();

                query.ViewFields = "<FieldRef Name=\"Title\"/><FieldRef Name=\"WFLogURL\"/>";
                query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + wfType + "</Value></Eq></Where>";
                SPListItemCollection itemCollection = list.GetItems(query);

                if (itemCollection.Count > 0)
                {
                    SPListItem itm = itemCollection[0];

                    if (itm["WFLogURL"] != null)
                        logsList = web.GetListFromWebPartPageUrl(itm["WFLogURL"].ToString());
                }

            }
            catch (Exception ex)
            {
                SaveErrorsLog_EventsReceiver(properties, wfid, "GetLogsList() " + ex.Message);
            }

            return logsList;

        }

        public static string GetDocumentLibraryURL(string defaultViewURL, string wfid, SPItemEventProperties properties)
        {
            string urlLibrary = string.Empty;

            try
            {

                if (defaultViewURL.Contains("/Forms/AllItems.aspx"))
                    urlLibrary = defaultViewURL.Replace("/Forms/AllItems.aspx", string.Empty);
                else
                    urlLibrary = defaultViewURL;
            }
            catch (Exception ex)
            {
                SaveErrorsLog_EventsReceiver(properties, wfid, "GetDocumentLibraryURL() " + ex.Message);
            }

            return urlLibrary;
        }

        #endregion

        #region <CR20 - REMOVE DOCUMENTS>

        public static void SaveLogsDeletingDocuments(string wfid, SPItemEventProperties properties, SPWeb web, SPUser editorUser, SPListItem item, bool hasPermissions)
        {
            try
            {
                if (item.ContentType.Name.Equals("Workflow Document") || item.ContentType.Name.Equals("Link to a Document") || item.ContentType.Name.Equals("Document"))
                {
                    string actionTaken = string.Empty;
                    string actionDetails = string.Empty;

                    if (hasPermissions)
                    {
                        actionTaken = GetActionDescription(ActionsEnum.DocumentRemoved.ToString());
                        actionDetails = "Removed document: " + item.File.Name;
                    }
                    else
                    {
                        actionTaken = GetActionDescription(ActionsEnum.TryRemoveDocument.ToString());
                        actionDetails = "Tried to remove document: " + item.File.Name;
                    }

                    CreateWorkflowLogModule(actionTaken, actionDetails, item, web, wfid, properties, editorUser);
                }
            }
            catch (Exception ex)
            {
                SaveErrorsLog_EventsReceiver(properties, wfid, "SaveLogsDeletingDocuments() " + ex.Message);
            }
        }

        public static bool PermissionsForRemovingDocument(SPListItem item, SPUser logginUser, SPWeb web, string wfid, SPItemEventProperties properties)
        {
            bool hasPermissions = false;

            try
            {
                SPFile file = item.File;
                //Login Name User
                string loginName = logginUser.LoginName;
                //Editor
                SPFieldUserValue editorUserValue = new SPFieldUserValue(web, item[SPBuiltInFieldId.Editor].ToString());
                SPUser editor = editorUserValue.User;
                //Total Versions
                string numberVersions = file.UIVersionLabel;

                if ((numberVersions.Equals("1.0")) && (editor.LoginName.Equals(loginName)))
                    hasPermissions = true;
                else if (IsTheInitiator(loginName, item, wfid, web, properties))
                    hasPermissions = true;
                else if ((!numberVersions.Equals("1.0")) && (editor.LoginName.Equals(loginName)))
                {
                    bool otherEditors = AreVersionsFromOtherUsers(file, web, loginName, wfid, properties);

                    if (otherEditors)
                        hasPermissions = false;
                    else
                        hasPermissions = true;

                }


            }
            catch (Exception ex)
            {
                SaveErrorsLog_EventsReceiver(properties, wfid, "PermissionsForRemovingDocument() " + ex.Message);
            }

            return hasPermissions;
        }

        public static bool IsTheInitiator(string loginName, SPListItem item, string wfid, SPWeb web, SPItemEventProperties properties)
        {
            bool isInitiator = false;

            try
            {
                string relativeURLLibrary = GetDocumentLibraryURL(item.ParentList.DefaultViewUrl, wfid, properties);
                string wfUrl = relativeURLLibrary + "/" + wfid;

                SPFolder wfFolder = web.GetFolder(wfUrl);
                SPListItem wfItem = wfFolder.Item;

                //Step 1 Assigned To
                SPFieldUserValue responsibleValue = new SPFieldUserValue(web, wfItem["Step 1 Assigned To"].ToString());
                SPUser responsible = responsibleValue.User;

                if (loginName.Equals(responsible.LoginName))
                    isInitiator = true;

            }
            catch (Exception ex)
            {
                SaveErrorsLog_EventsReceiver(properties, wfid, "IsTheInitiator() " + ex.Message);
            }

            return isInitiator;
        }

        public static bool AreVersionsFromOtherUsers(SPFile file, SPWeb web, string loginName, string wfid, SPItemEventProperties properties)
        {
            bool otherEditors = false;

            try
            {


                foreach (SPFileVersion version in file.Versions)
                {

                    if (!version.IsCurrentVersion)
                    {
                        //Editor
                        SPUser editorVersion = version.CreatedBy;

                        if (!editorVersion.LoginName.Equals(loginName))
                        {
                            otherEditors = true;
                            break;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                SaveErrorsLog_EventsReceiver(properties, wfid, "IsTheInitiator() " + ex.Message);
            }

            return otherEditors;
        }

        #endregion

        #region <CR37 - MOVE DOCUMENTS>

        public static void MoveDocumentModule(SPItemEventProperties properties, string wfid, string documentationTypeAfter, SPFile file, SPFolder folder, SPSite site, SPUser editorUser, string documentationTypeBefore)
        {
            try
            {
               

                if (!documentationTypeBefore.Equals(documentationTypeAfter))
                {
                    string newFileURL = folder.ParentFolder.Url + "/" + documentationTypeAfter + "/" + file.Name;
                    SPFolder destFolder = site.RootWeb.GetFolder(folder.ParentFolder.Url + "/" + documentationTypeAfter);

                    if (!destFolder.Exists)
                    {
                        if (documentationTypeAfter.Equals("ABAC"))
                            documentationTypeAfter = "To be signed in ABAC";
                        else if (documentationTypeAfter.Equals("Paper signed docs"))
                            documentationTypeAfter = "Signed";

                        destFolder = site.RootWeb.GetFolder(folder.ParentFolder.Url + "/" + documentationTypeAfter);
                        newFileURL = folder.ParentFolder.Url + "/" + documentationTypeAfter + "/" + file.Name;
                    }

                    if (ExistDocument(wfid, properties, destFolder, file.Name, site.RootWeb))
                    {
                        //Cancel Event
                        properties.Cancel = true;
                        properties.Status = SPEventReceiverStatus.CancelWithError;
                        properties.ErrorMessage = "There is already one document with this name at this location.";
                    }
                    else
                    {

                        file.MoveTo(newFileURL, SPMoveOperations.Overwrite);
                        UpdateFileMetadata(newFileURL, site.RootWeb, wfid, properties, editorUser, documentationTypeAfter);

                        //Cancel Event
                        properties.Cancel = true;
                        properties.Status = SPEventReceiverStatus.CancelNoError;

                        RecordTraceMoved(wfid, site, file.Item, editorUser, properties, documentationTypeBefore, documentationTypeAfter);
                    }

                }
                else
                    classLibraryEventReceiver.RecordTraceUpdated(wfid, site, file.Item, editorUser, properties);
              
                
            }
            catch (Exception ex)
            {
                SaveErrorsLog_EventsReceiver(properties, wfid, "MoveDocumentModule() " + ex.Message);
            }
        }

        public static void RecordTraceMoved(string wfid, SPSite site, SPListItem item, SPUser editorUser, SPItemEventProperties properties, string documentationTypeBefore, string documentationTypeAfter)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite elevatedSite = new SPSite(site.ID))
                    {
                        SPWeb web = elevatedSite.RootWeb;
                        web.AllowUnsafeUpdates = true;

                        string actionTaken = classLibraryEventReceiver.GetActionDescription(classLibraryEventReceiver.ActionsEnum.DocumentMoved.ToString());
                        string actionDetails = "Document moved : '" + item.File.Name + "'. [" + documentationTypeBefore + " -> " + documentationTypeAfter + "]";

                        classLibraryEventReceiver.CreateWorkflowLogModule(actionTaken, actionDetails, item, web, wfid, properties, editorUser);

                    }
                });
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, wfid, "WorkflowLibrayEventReceiver - RecordTraceMoved() " + ex.Message);
            }
        }

        public static void UpdateFileMetadata(string newURL, SPWeb web, string wfid, SPItemEventProperties properties, SPUser editorUser, string documentationTypeAfter)
        {
            try
            {
                SPFile dstFile = web.GetFile(newURL);
                SPListItem dstItem = (SPListItem)dstFile.Item;
                
                dstItem.ParentList.Fields[SPBuiltInFieldId.Modified].ReadOnlyField = false;
                dstItem.ParentList.Fields[SPBuiltInFieldId.Editor].ReadOnlyField = false;
                
                dstItem[SPBuiltInFieldId.Modified] = System.DateTime.Now;
                dstItem[SPBuiltInFieldId.Editor] = editorUser;
                dstItem["DocumentationType"] = documentationTypeAfter;
                
                //updates the item without creating another version of the item
                dstItem.UpdateOverwriteVersion();
                dstItem.ParentList.Fields[SPBuiltInFieldId.Modified].ReadOnlyField = true;
                dstItem.ParentList.Fields[SPBuiltInFieldId.Editor].ReadOnlyField = true;
            }
            catch(Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, wfid, "WorkflowLibrayEventReceiver - UpdateFileMetadata() " + ex.Message);
            }
        }

        public static bool ExistDocument(string wfid, SPItemEventProperties properties, SPFolder dstFolder, string fileName, SPWeb web)
        {
            bool existDocument = false;

            try
            {
                SPFile file = web.GetFile(string.Format("{0}/{1}", dstFolder.Url, fileName));

                if (file.Exists)
                    existDocument = true;
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, wfid, "WorkflowLibrayEventReceiver - ExistDocument(): '" + dstFolder.Url.ToString() + "/" + fileName + "'. " + ex.Message);
            }

            return existDocument;
        }

        #endregion


        #region <LOGS>


        public static void RecordTraceUpdated(string wfid, SPSite site, SPListItem item, SPUser editorUser, SPItemEventProperties properties)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite elevatedSite = new SPSite(site.ID))
                    {
                        SPWeb web = elevatedSite.RootWeb;
                        web.AllowUnsafeUpdates = true;
                  
                        string version = item.File.UIVersionLabel;

                        if (!version.Equals("1.0"))
                        {
                            string actionTaken = classLibraryEventReceiver.GetActionDescription(classLibraryEventReceiver.ActionsEnum.NewDocumentVersion.ToString());
                            string actionDetails = "New document version : " + item.File.Name + " {v." + version + "}";

                            classLibraryEventReceiver.CreateWorkflowLogModule(actionTaken, actionDetails, item, web, wfid, properties, editorUser);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, wfid, "WorkflowLibrayEventReceiver - RecordTraceUpdated() " + ex.Message);
            }
        }

        public static void CreateWorkflowLogModule(string actionTaken, string actionDetails, SPListItem item, SPWeb web, string wfid, SPItemEventProperties properties, SPUser editorUser)
        {
            Dictionary<string, string> WFDictionary = new Dictionary<string, string>();

            try
            {

                WFDictionary = GetWFMetadata(item, wfid, web, properties);

                int stepNumber = Convert.ToInt32(WFDictionary["StepNumber"]);
                string wfType = WFDictionary["WFType"];
                string wfLinkValue = WFDictionary["WFLink"];
                string status = WFDictionary["WFStatus"];
                string assignedPersonLogin = WFDictionary["AssignedPerson"];
                string computerName = GetComputerName(HttpContext.Current);
                string confidential = WFDictionary["ConfidentialWorkflow"];
                SPList logList = GetLogsList(wfid, wfType, web, properties);

                CreateWorkflowLog(wfid, stepNumber, status, assignedPersonLogin, actionTaken, actionDetails, computerName, null, confidential, logList, web, true, wfLinkValue, properties, editorUser);
            }
            catch (Exception ex)
            {
                SaveErrorsLog_EventsReceiver(properties, wfid, "CreateWorkflowLogModule() " + ex.Message);
            }
        }


        /// <summary>
        /// Create a workflow log record
        /// </summary>
        /// <param name="wfType"></param>
        /// <param name="wfTypeCode"></param>
        /// <param name="WFID"></param>
        /// <param name="stepNumber"></param>
        /// <param name="status"></param>
        /// <param name="assignedPerson"></param>
        /// <param name="actionTaken"></param>
        /// <param name="actionDetails"></param>
        /// <param name="computerName"></param>
        /// <param name="workflowComment"></param>
        /// <param name="isOldComment"></param>
        /// <param name="confidential"></param>
        /// <param name="web"></param>
        /// <param name="parameters"></param>
        /// <param name="realEditor"></param>
        public static void CreateWorkflowLog(string wfid, int stepNumber, string status, string assignedPersonLogin, string actionTaken, string actionDetails, string computerName, string workflowComment, string confidential, SPList list, SPWeb web, bool oldComment, string wfLinkValue, SPItemEventProperties properties, SPUser editorUser)
        {
            try
            {
                if (list != null)
                {

                    SPListItem item = list.Items.Add();

                    item["WFID"] = wfid;
                    item["StepNumber"] = stepNumber;
                    item["WFStatus"] = status;
                    item["ActionTaken"] = actionTaken;

                    //Single Line of Text
                    if (actionDetails.Length > 128)
                    {
                        actionDetails = actionDetails.Substring(0, 127);
                        item["ActionDetails"] = actionDetails;
                    }
                    else
                        item["ActionDetails"] = actionDetails;

                    item["ComputerName"] = computerName;
                    item["WorkflowComment"] = workflowComment;
                    item["ConfidentialWorkflow"] = confidential;
                    item["Author"] = editorUser;
                    item["Editor"] = editorUser;

                    //WFLInk
                    SPFieldUrlValue url = new SPFieldUrlValue();
                    url.Url = wfLinkValue;
                    url.Description = wfid;
                    item["WFLink"] = url;

                    //AssignedPerson
                    if (!string.IsNullOrEmpty(assignedPersonLogin))
                    {
                        try
                        {
                            item["AssignedPerson"] = web.EnsureUser(assignedPersonLogin);
                        }
                        catch
                        {
                            item["AssignedPerson"] = web.SiteUsers[assignedPersonLogin];
                        }
                    }



                    if (oldComment)
                        item["OldComment"] = 1;
                    else
                        item["OldComment"] = 0;


                    using (new DisabledItemEventsScope())
                    {
                        item.Update();
                    }

                }
            }
            catch (Exception ex)
            {
                SaveErrorsLog_EventsReceiver(properties, wfid, "CreateWorkflowLog() " + ex.Message);
            }
        }



        #endregion

        public static void SaveErrorsLog_EventsReceiver(SPItemEventProperties properties, string wfid, string message)
        {
            try
            {
                SPSite site = properties.Web.Site as SPSite;
                SPListItem item = properties.ListItem;


                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite elevatedSite = new SPSite(site.ID))
                    {
                        SPWeb web = elevatedSite.RootWeb;
                        web.AllowUnsafeUpdates = true;


                        string listErrorName = "RS Error Log";
                        SPList myList = web.Lists[listErrorName];

                        string _message = "[" + wfid + "] " + message;

                        if ((!string.IsNullOrEmpty(_message)) && (_message.Length > 128))
                            _message = _message.Substring(0, 127);

                        if (myList != null)
                        {
                            SPQuery query = new SPQuery();
                            query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + _message + "</Value></Eq></Where>";
                            query.ViewFields = string.Concat(
                              "<FieldRef Name='Title' />");
                            query.ViewFieldsOnly = true; // Fetch only the data that we need

                            SPListItemCollection itemCollection = myList.GetItems(query);
                            SPListItem itm = null;

                            if (itemCollection.Count > 0)
                            {
                                itm = itemCollection[0];
                                itm["Title"] = _message;
                                itm["RSQueryLog"] = message;
                            }
                            else
                            {
                                itm = myList.Items.Add();
                                itm["Title"] = _message;
                                itm["RSQueryLog"] = message;
                            }

                            try
                            {
                                itm.Update();
                            }
                            catch { }
                        }

                        if (web.AllowUnsafeUpdates)
                            web.AllowUnsafeUpdates = false;

                        web.Close();
                        web.Dispose();
                    }


                });

            }
            catch
            {

            }
        }


       

    }
}
