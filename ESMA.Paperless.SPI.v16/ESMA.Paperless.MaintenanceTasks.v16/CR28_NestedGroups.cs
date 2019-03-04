using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using System.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Office.Interop.Excel;
using System.Data;
using System.Drawing;

namespace ESMA.Paperless.MaintenanceTasks.v16
{
    class CR28_NestedGroups
    {

        public static void ReplaceADGroupsModule()
        {
            try
            {

                General.TraceHeader("*** Replace 'Active Directory Groups' *** -    Started at: " + System.DateTime.Now.ToString(), ConsoleColor.Green);
       

                //E:\ENISA\UpdateProcess
                string pathLogs = ConfigurationManager.AppSettings["pathLOGS"];
                General.CreateFolderXML(pathLogs);
                string urlWeb = ConfigurationManager.AppSettings["url"];
                string wfOrderParameter = ConfigurationManager.AppSettings["WFOrderListToUpdated"];
                string domain = ConfigurationManager.AppSettings["domain"];


                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite colsit = new SPSite(urlWeb))
                    {
                        SPWeb web = colsit.OpenWeb();
                        web.AllowUnsafeUpdates = true;

                        if (!string.IsNullOrEmpty(wfOrderParameter))
                        {

                            String[] wfOrderList = wfOrderParameter.Split(',');


                            foreach(var wfOrder in wfOrderList)
                            {
                                //RS WF Configuration
                                SPListItem wfTypeConf = SP.GetWFTypeConfiguration(web, wfOrder);
                                string wfTypeName =  wfTypeConf["Title"].ToString().Trim();
                                //RS WF Step Definitions (EmailReceiverGroup, StepBackupGroup, WFGroup)
                                SPListItemCollection wfTypeStepsCollection = SP.GetWFTypeStepDefinitions(web, wfTypeName);


                                if (wfTypeConf["WFLibraryURL"] != null)
                                {

                                    General.TraceInformation("- WF Type to update: '" + wfTypeName + "'.", ConsoleColor.Yellow);
                                    ReplaceADGroupsModule(web, wfTypeConf, wfTypeStepsCollection, wfTypeName, pathLogs, wfOrder, domain);
                                }
                                else
                                    General.TraceInformation("- WFType: '" + wfTypeName + "' does not have 'WFLibraryURL'.", ConsoleColor.Red);


                            }

                           


                            
                        }
                        else
                            General.TraceInformation("No WFOrder has been especified to be analysed.", ConsoleColor.Red);


                        web.AllowUnsafeUpdates = false;
                        web.Close();
                        web.Dispose();
                    }

                });
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }

        private static void ReplaceADGroupsModule(SPWeb web, SPListItem wfTypeConf, SPListItemCollection wfTypeStepsCollection, string wfTypeName, string pathLogs, string wfOrder, string domain)
        {
            try
            {
                SPList wfLibrary = SP.GetWFLibrary(web, wfTypeConf);

                if (wfLibrary != null)
                {
                    int wfTypeSteps = wfTypeStepsCollection.Count;
                    string initialSteps = GetInitialStepsGroups(wfTypeStepsCollection);
                    string initialEmailReceiverGroups = GetInitialEmailReceiverGroups(wfTypeStepsCollection, web);
                    string initialStepBackupGroups = GetInitialStepsBackupGroups(wfTypeStepsCollection);
                    Dictionary<string, SPUser> newBackupGroupsDictionary = GetNewBackupGroupsDictionary(wfTypeStepsCollection, web);
                    SPRoleDefinition roleDefinitionRSContributor = web.RoleDefinitions["RS Contribute"];
                    SPRoleDefinition roleDefinitionRSRead = web.RoleDefinitions["RS Read"];
                    
                    Dictionary<string, List<string>> wfInformationDictionary = new Dictionary<string,List<string>>();

                    //Total WFs
                    SPListItemCollection wfsCollection = SP.GetWFItems(web, wfLibrary);
                    General.TraceInformation("- Total WFs to review: '" + wfsCollection.Count.ToString() + "'.", ConsoleColor.White);

                    foreach (SPListItem wfItem in wfsCollection)
                    {
                        string wfid = string.Empty;
                        bool updateWFValues = false;
                        List<string> stepsToReplaceList = new List<string>();
                        
                        try
                        {
                            int wfSteps = GetNumberWFSteps(wfItem);
                            wfid = wfItem["WFID"].ToString();
                            string status = wfItem["WFStatus"].ToString();
                            string currentStep = wfItem["StepNumber"].ToString();
                            List<string> wfInitialParametersList = SetInitialParamametersList(wfItem);
                            string confidentialValue = wfItem["ConfidentialWorkflow"].ToString();
                            bool isConfidential = (string.IsNullOrEmpty(confidentialValue) || confidentialValue.ToUpper().Equals("NON RESTRICTED")) ? false : true;

                            General.TraceInformation("- WFID: " + wfid + " (" + status + ")", ConsoleColor.Gray);


                                //Only if the total steps are the same that there are defined in the RS Step Definitions List
                                if (wfSteps.Equals(wfTypeSteps))
                                {

                                    string currentInitialSteps = string.Empty;
                                    string currentInitialEmailReceiverGroups = string.Empty;
                                    string currentActorsSignedRole = string.Empty;
                                    string currentInitialBackupGroups = string.Empty;
                                    bool updateBackupGroup = false;


                                    if (wfItem["InitialSteps"] != null)
                                        currentInitialSteps = wfItem["InitialSteps"].ToString();

                                    if (wfItem["OtherInitialData"] != null)
                                        currentInitialEmailReceiverGroups = wfItem["OtherInitialData"].ToString();

                                    //ActorsSignedRole
                                    if (wfItem["WFActorsSignedRole"] != null)
                                        currentActorsSignedRole = wfItem["WFActorsSignedRole"].ToString();

                                    if (wfItem["InitialStepBackupGroups"] != null)
                                        currentInitialBackupGroups = wfItem["InitialStepBackupGroups"].ToString();


                                    UpdateValuesModule(initialSteps, initialStepBackupGroups, initialEmailReceiverGroups, currentInitialSteps, currentInitialBackupGroups, currentInitialEmailReceiverGroups, wfItem, ref updateWFValues, wfTypeStepsCollection, web, currentActorsSignedRole, domain, ref updateBackupGroup, wfid, currentStep);

                                    if (updateWFValues)
                                    {
                                        wfInitialParametersList.Add("TRUE");

                                        //Remove Print Document
                                        if (status.ToLower().Equals("closed") || status.ToLower().Equals("deleted"))
                                            RemovePrintedDocument(wfItem, wfTypeName, wfid, web);

                                        //Update Permissions (Restricted)
                                        if (updateBackupGroup.Equals(true))
                                            UpdatePermissionsModule(currentInitialBackupGroups, wfTypeStepsCollection, wfItem, web, status, isConfidential, roleDefinitionRSContributor, roleDefinitionRSRead, wfid, newBackupGroupsDictionary, stepsToReplaceList, domain, currentStep);


                                        General.TraceInformation("- WFID: '" + wfid + "' updated.", ConsoleColor.Green);
                                    }
                                    else
                                        wfInitialParametersList.Add("FALSE");
                                    

                                }
                                else
                                    wfInitialParametersList.Add("FALSE - Different Step Number. WF Steps: " + wfSteps);


                                wfInformationDictionary.Add(wfid, wfInitialParametersList);
                            
                        }
                        catch (Exception ex)
                        {
                            General.TraceException(ex);
                            General.TraceInformation("- Error WFID: '" + wfid + "' - URL: " + wfItem.Url, ConsoleColor.Red);
                        }

                    }

                      //Logs
                    CreateExcelFile(wfTypeName, wfInformationDictionary, pathLogs, wfOrder);

                    }
                else
                    General.TraceInformation("- WFType: '" + wfTypeName + "' does not have 'WF Library'.", ConsoleColor.Red);

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }

        private static int GetNumberWFSteps(SPListItem wfItem)
        {
            int totalSteps = 0;

            try
            {
                if (wfItem["InitialSteps"] != null)
                {
                    string initialSteps = wfItem["InitialSteps"].ToString();
                    string[] steps = Regex.Split(initialSteps, "&#");

                    totalSteps = steps.Count();
                   
                }
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return totalSteps;
        }

        private static void UpdateValuesModule(string initialSteps, string stepBackupGroups, string stepInitialEmailReceiverGroups, string currentInitialSteps, string currentBackupGroups, string currentInitialEmailReceiverGroups, SPListItem wfItem, ref bool updateWFValues, SPListItemCollection wfTypeStepsCollection, SPWeb web, string currentActorsSignedRole, string domain, ref bool updateBackupGroup, string wfid, string currentStep)
        {
            bool updateWfItem = false;
            string notificationsUpdatedValue = string.Empty;
            string roleUpdatedValue = string.Empty;
            string backupGroupUpdatedValue = string.Empty;

            try
            {
                //InitialSteps
                if (!initialSteps.Equals(currentInitialSteps))
                {
                    wfItem["InitialSteps"] = initialSteps;
                    updateWfItem = true;
                }


                //OtherInitialData
                if ((!stepInitialEmailReceiverGroups.Equals(currentInitialEmailReceiverGroups)) && (currentInitialEmailReceiverGroups.ToLower().Contains("true")))
                {
                    bool updateValue = UpdateOtherInitialData(stepInitialEmailReceiverGroups, currentInitialEmailReceiverGroups, ref notificationsUpdatedValue, wfTypeStepsCollection, wfItem, web);

                    if (updateValue.Equals(true))
                    {
                        wfItem["OtherInitialData"] = notificationsUpdatedValue;
                        updateWfItem = true;
                    }
                }

                //Actor Signed Role
                bool updateRole = UpdateActorsSignedRole(currentActorsSignedRole, ref roleUpdatedValue, wfTypeStepsCollection, wfItem, web, domain);

                if (updateRole.Equals(true))
                {
                    wfItem["WFActorsSignedRole"] = roleUpdatedValue;
                    updateWfItem = true;
                }

                //Initial Step Backup Groups
                updateBackupGroup = UpdateBackupGroups(currentBackupGroups, ref backupGroupUpdatedValue, wfTypeStepsCollection, wfItem, web);

                if (updateBackupGroup.Equals(true))
                {
                    wfItem["InitialStepBackupGroups"] = backupGroupUpdatedValue;
                    updateWfItem = true;
                }

                if (updateWfItem.Equals(true))
                {
                    using (new DisabledItemEventsScope())
                    {
                        try
                        {
                            wfItem.SystemUpdate();
                            updateWFValues = true;
                        }
                        catch
                        {
                            General.TraceInformation("*** ERROR: WFID: '" + wfid + "'. Values not updated.", ConsoleColor.Red);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }

        #region <INITIAL VALUES>

        private static List<string> SetInitialParamametersList(SPListItem wfItem)
        {
            List<string> wfInitialParametersList = new List<string>();

            try
            {
                if (wfItem["WFSubject"] != null)
                    wfInitialParametersList.Add(wfItem["WFSubject"].ToString());
                else
                    wfInitialParametersList.Add("");

                if (wfItem["InitialSteps"] != null)
                    wfInitialParametersList.Add(wfItem["InitialSteps"].ToString());
                else
                    wfInitialParametersList.Add("");

                if (wfItem["InitialStepBackupGroups"] != null)
                    wfInitialParametersList.Add(wfItem["InitialStepBackupGroups"].ToString());
                else
                    wfInitialParametersList.Add("");

                if (wfItem["OtherInitialData"] != null)
                    wfInitialParametersList.Add(wfItem["OtherInitialData"].ToString());
                else
                    wfInitialParametersList.Add("");

                if (wfItem["WFActorsSignedRole"] != null)
                    wfInitialParametersList.Add(wfItem["WFActorsSignedRole"].ToString());
                else
                    wfInitialParametersList.Add("");
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
            return wfInitialParametersList;
        }

        private static string GetInitialStepsGroups(SPListItemCollection stepCollection)
        {
            string groups = string.Empty;

            try
            {
                if (stepCollection != null)
                {
                    int count = 0;

                    foreach (SPListItem item in stepCollection)
                    {
                        if (item["WFGroup"] != null && item["StepNumber"] != null)
                        {
                            if (count.Equals(0))
                                groups += item["StepNumber"] + ";#" + item["WFGroup"].ToString();
                            else
                                groups += "&#" + item["StepNumber"] + ";#" + item["WFGroup"].ToString();
                        }

                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return groups;
        }

        private static string GetInitialEmailReceiverGroups(SPListItemCollection stepCollection, SPWeb Web)
        {
            string receiverGroups = string.Empty;

            try
            {
                if (stepCollection != null)
                {
                    int count = 0;

                    foreach (SPListItem item in stepCollection)
                    {
                        if (item["SendEmail"] != null && item["EmailReceiverGroup"] != null)
                        {
                            SPFieldUserValue groupValue = new SPFieldUserValue(Web, item["EmailReceiverGroup"].ToString());

                            if (count.Equals(0))
                                receiverGroups += item["StepNumber"] + ";#" + item["SendEmail"].ToString() + ";#" + groupValue.LookupId.ToString();
                            else
                                receiverGroups += "&#" + item["StepNumber"] + ";#" + item["SendEmail"].ToString() + ";#" + groupValue.LookupId.ToString();
                        }
                        else if (item["StepNumber"] != null)
                        {
                            if (count.Equals(0))
                                receiverGroups += item["StepNumber"] + ";#" + item["SendEmail"].ToString() + ";#" + string.Empty;
                            else
                                receiverGroups += "&#" + item["StepNumber"] + ";#" + item["SendEmail"].ToString() + ";#" + string.Empty;
                        }
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return receiverGroups;
        }

        private static string GetInitialStepsBackupGroups(SPListItemCollection stepCollection)
        {
            string backupGroups = string.Empty;

            try
            {
                if (stepCollection != null)
                {
                    int count = 0;
                    foreach (SPListItem item in stepCollection)
                    {
                        if (item["StepBackupGroup"] != null && item["StepNumber"] != null)
                        {
                            if (count.Equals(0))
                                backupGroups += item["StepNumber"] + ";#" + item["StepBackupGroup"].ToString();
                            else
                                backupGroups += "&#" + item["StepNumber"] + ";#" + item["StepBackupGroup"].ToString();
                        }

                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return backupGroups;
        }

        #endregion

        #region <OTHER INITIAL DATA>

        private static bool UpdateOtherInitialData(string stepInitialEmailReceiverGroups, string currentInitialEmailReceiverGroups, ref string notificationsUpdated, SPListItemCollection wfTypeStepsCollection, SPListItem wfItem, SPWeb web)
        {
            bool update = false;

            try
            {

                notificationsUpdated = GetUpdateEmailNotificationValues(currentInitialEmailReceiverGroups, wfTypeStepsCollection, wfItem, web);
                update = true;

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return update;
        }

        public static string GetUpdateEmailNotificationValues(string currentInitialEmailReceiverGroups, SPListItemCollection wfTypeStepsCollection, SPListItem wfItem, SPWeb web)
        {
            string notificationsUpdated = null;

            try
            {
              
                    string[] stepCurrentNotifAux = Regex.Split(currentInitialEmailReceiverGroups, "&#");

                    foreach (string inf in stepCurrentNotifAux)
                    {
                        string newValue = string.Empty;

                        if ((inf.Contains(";#")) && (inf.ToLower().Contains("true")))
                        {
                            string[] currentNotifications = Regex.Split(inf, ";#");
                            string stepNumber = currentNotifications[0];
                            string newGroupID = GetEmailReceiverGroupID(wfTypeStepsCollection, stepNumber, web);
                            newValue = stepNumber + ";#True;#" + newGroupID;

                        }

                       
                        if (string.IsNullOrEmpty(notificationsUpdated))
                        {
                            if (string.IsNullOrEmpty(newValue))
                                notificationsUpdated = inf;
                            else
                                notificationsUpdated = newValue;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(newValue))
                                notificationsUpdated = notificationsUpdated + "&#" + inf;
                            else
                                notificationsUpdated = notificationsUpdated + "&#" + newValue;
                        }
                        
                    }

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return notificationsUpdated;
        }

        public static string GetEmailReceiverGroupID(SPListItemCollection stepCollection, string stepNumber, SPWeb web)
        {
            string id = string.Empty;

            try
            {
                SPListItem item = stepCollection[Convert.ToInt32(stepNumber) - 1];

                if (item["EmailReceiverGroup"] != null)
                {
                    SPFieldUserValue groupValue = new SPFieldUserValue(web, item["EmailReceiverGroup"].ToString());
                    id = groupValue.LookupId.ToString();
                }
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return id;

        }

        #endregion

        #region <ACTORS SIGNED ROLE>

        private static bool UpdateActorsSignedRole(string currentActorsSignedRole, ref string roleUpdated, SPListItemCollection stepCollection, SPListItem wfItem, SPWeb web, string domain)
        {
            bool update = false;

            try
            {

                if (!string.IsNullOrEmpty(currentActorsSignedRole))
                {
                    string[] stepRoleAux = Regex.Split(currentActorsSignedRole, "&#");

                    foreach (string inf in stepRoleAux)
                    {
                        string newValue = string.Empty;

                        if (inf.Contains(";#"))
                        {
                            string[] currentStepInf = Regex.Split(inf, ";#");
                            string stepNumber = currentStepInf[0];
                            string userSigned = currentStepInf[1];
                            string currentRole = currentStepInf[2];
                            string newRole = GetGroupName(stepCollection, stepNumber, web, domain);

                            if (!currentRole.ToLower().Equals(newRole.ToLower()))
                            {
                                newValue = stepNumber + ";#" + userSigned + ";#" + newRole;
                                update = true;
                            }

                        }


                        if (string.IsNullOrEmpty(roleUpdated))
                        {
                            if (string.IsNullOrEmpty(newValue))
                                roleUpdated = inf;
                            else
                                roleUpdated = newValue;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(newValue))
                                roleUpdated = roleUpdated + "&#" + inf;
                            else
                                roleUpdated = roleUpdated + "&#" + newValue;
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return update;
        }

        public static string GetGroupName(SPListItemCollection stepCollection, string stepNumber, SPWeb web, string domain)
        {
            string groupName = string.Empty;

            try
            {
                SPListItem item = stepCollection[Convert.ToInt32(stepNumber) - 1];

                if (item["WFGroup"] != null)
                {
                    SPFieldUserValue groupValue = new SPFieldUserValue(web, item["WFGroup"].ToString());

                    if (groupValue != null)
                        groupName = groupValue.User.Name.ToString();

                    if (groupName.ToLower().Contains(domain.ToLower()))
                        groupName = groupName.ToLower().Replace(domain.ToLower() + "\\", null).Trim();
                }
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return groupName;

        }

        #endregion

        #region <BACKUP GROUPS>

        private static bool UpdateBackupGroups(string currentBackupGroups, ref string backupGroupUpdated, SPListItemCollection stepCollection, SPListItem wfItem, SPWeb web)
        {
            bool update = false;

            try
            {
                if (!string.IsNullOrEmpty(currentBackupGroups))
                {
                    string[] stepBackupAux = Regex.Split(currentBackupGroups, "&#");

                    foreach (string inf in stepBackupAux)
                    {
                        string newValue = string.Empty;

                        if (inf.Contains(";#"))
                        {
                            string[] currentStepInf = Regex.Split(inf, ";#");
                            string stepNumber = currentStepInf[0];
                            string idGroup = currentStepInf[1];
                            string currentBackupGroup = currentStepInf[2];
                            SPFieldUserValue backupGroup = GetBackupGroup(stepCollection, stepNumber, web);

                            if (backupGroup != null)
                            {
                                string newBackupGroup = backupGroup.User.Name.ToString();
                                string newBackupID = backupGroup.User.ID.ToString();

                                if (!currentBackupGroup.ToLower().Equals(newBackupGroup.ToLower()))
                                {
                                    newValue = stepNumber + ";#" + newBackupID + ";#" + newBackupGroup;
                                    update = true;
                                }
                            }

                        }


                        if (string.IsNullOrEmpty(backupGroupUpdated))
                        {
                            if (string.IsNullOrEmpty(newValue))
                                backupGroupUpdated = inf;
                            else
                                backupGroupUpdated = newValue;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(newValue))
                                backupGroupUpdated = backupGroupUpdated + "&#" + inf;
                            else
                                backupGroupUpdated = backupGroupUpdated + "&#" + newValue;
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return update;
        }

        public static SPFieldUserValue GetBackupGroup(SPListItemCollection stepCollection, string stepNumber, SPWeb web)
        {
            SPFieldUserValue backupGroupValue = null;

            try
            {
                SPListItem item = stepCollection[Convert.ToInt32(stepNumber) - 1];

                if (item["StepBackupGroup"] != null)
                   backupGroupValue = new SPFieldUserValue(web, item["StepBackupGroup"].ToString());

                
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return backupGroupValue;

        }

        #endregion

        #region <PRINT DOCUMENT>

        private static void RemovePrintedDocument(SPListItem wfItem, string wfTypeName, string wfid, SPWeb web)
        {
            try
            {
                string printedDocumentName = GeneratePrintDocumentName(wfTypeName.ToUpper().ToString(), wfid) + "_" + wfid + ".pdf";
                string WFURL = wfItem.Url;
                string urlPrintedFile = wfItem.Url + "/" + printedDocumentName;

                SPFile file = web.GetFile(urlPrintedFile);
                
                if (file.Exists)
                {
                    using (new DisabledItemEventsScope())
                    {
                        file.Delete();
                        General.TraceInformation("- PDF File Removed: '" + printedDocumentName + "'", ConsoleColor.Yellow);
                    }
                }
                else
                    General.TraceInformation("*** WARNING: WFID: '" + wfid + "'does not have the PRINTED document.", ConsoleColor.Blue);
                

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }

        private static string GeneratePrintDocumentName(string printedDocumentName, string wfid)
        {

            try
            {
                if (HasInvalidCharacter_ListName(printedDocumentName, wfid) == true)
                    printedDocumentName = ReplaceInvalidCharacter_ListName(printedDocumentName, wfid);

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return printedDocumentName;

        }

        private static bool HasInvalidCharacter_ListName(string listName, string wfid)
        {
            try
            {
                bool invalid = false;
                string[] listValues = new string[17] { "\\", "/", ":", "*", "?", "\"", "<", ">", "|", "{", "}", "#", "%", "~", "&amp;", "&", "." };
                string character = string.Empty;

                for (int i = 0; i < listValues.Length; i++)
                {
                    character = listValues[i].ToString();

                    if (listName.Contains(character))
                    {
                        invalid = true;
                        break;
                    }
                }

                return invalid;

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
                return false;
            }
        }

        private static string ReplaceInvalidCharacter_ListName(string listName, string wfid)
        {
            try
            {

                string[] listValues = new string[17] { "\\", "/", ":", "*", "?", "\"", "<", ">", "|", "{", "}", "#", "%", "~", "&amp;", "&", "." };
                string character = string.Empty;
                string listNameReplaced = listName;
                bool modified = false;

                string finalName = string.Empty;

                if (listNameReplaced.Contains(".."))
                {
                    listNameReplaced = listNameReplaced.Replace("..", ".");
                    modified = true;
                }

                for (int i = 0; i < listValues.Length; i++)
                {
                    character = listValues[i].ToString();

                    if (listNameReplaced.Contains(character))
                    {

                        #region <RULES>

                        if (listNameReplaced.StartsWith("~") || listNameReplaced.Contains("~"))
                        {
                            listNameReplaced = listNameReplaced.Replace("~", "_");
                            modified = true;
                        }

                        if ((listNameReplaced.Contains("&amp;")) || (listNameReplaced.Contains("&")))
                        {
                            if (listNameReplaced.Contains("&amp;"))
                                listNameReplaced = listNameReplaced.Replace("&amp;", "and");
                            else
                                listNameReplaced = listNameReplaced.Replace("&", "and");

                            modified = true;
                        }



                        if (listNameReplaced.Contains("#"))
                        {
                            listNameReplaced = listNameReplaced.Replace("#", null);
                            modified = true;
                        }

                        if (listNameReplaced.Contains("/"))
                        {
                            listNameReplaced = listNameReplaced.Replace("/", "-");
                            modified = true;
                        }

                        if (listNameReplaced.Contains("\\"))
                        {
                            listNameReplaced = listNameReplaced.Replace("\\", "-");
                            modified = true;
                        }

                        if (listNameReplaced.Contains(":"))
                        {
                            listNameReplaced = listNameReplaced.Replace(":", "-");
                            modified = true;
                        }

                        if (listNameReplaced.Contains("*"))
                        {
                            listNameReplaced = listNameReplaced.Replace("*", "-");
                            modified = true;
                        }

                        if (listNameReplaced.Contains("|"))
                        {
                            listNameReplaced = listNameReplaced.Replace("|", "-");
                            modified = true;
                        }

                        if (listNameReplaced.Contains("?"))
                        {
                            listNameReplaced = listNameReplaced.Replace("?", null);
                            modified = true;
                        }

                        if ((listNameReplaced.Contains("[")) || (listNameReplaced.Contains("]")))
                        {
                            if (listNameReplaced.Contains("["))
                            {
                                listNameReplaced = listNameReplaced.Replace("[", "(");
                            }
                            else
                            {
                                listNameReplaced = listNameReplaced.Replace("]", ")");
                            }

                            modified = true;
                        }

                        if ((listNameReplaced.Contains("<")) || (listNameReplaced.Contains(">")))
                        {
                            if (listNameReplaced.Contains("<"))
                            {
                                listNameReplaced = listNameReplaced.Replace("<", "(");
                            }
                            else
                            {
                                listNameReplaced = listNameReplaced.Replace(">", ")");
                            }

                            modified = true;
                        }

                        if ((listNameReplaced.Contains("{")) || (listNameReplaced.Contains("}")))
                        {
                            if (listNameReplaced.Contains("{"))
                            {
                                listNameReplaced = listNameReplaced.Replace("{", "(");
                            }
                            if (listNameReplaced.Contains("}"))
                            {
                                listNameReplaced = listNameReplaced.Replace("}", ")");
                            }

                            modified = true;
                        }


                        if (listNameReplaced.Contains("\""))
                        {
                            listNameReplaced = listNameReplaced.Replace("\"", "'");
                            modified = true;
                        }

                        if (listNameReplaced.Contains("%"))
                        {
                            listNameReplaced = listNameReplaced.Replace("%", null);
                            modified = true;
                        }


                        #endregion

                    }
                }

                if (modified == true)
                    finalName = listNameReplaced;
                else
                    finalName = listName;

                return finalName.Trim();
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
                return string.Empty;
            }
        }

        #endregion

        #region <EXCEL FILE>

        private static void CreateExcelFile(string wfTypeName, Dictionary<string, List<string>> wfInformationDictionary, string pathLogs, string wfOrder)
        {
            Microsoft.Office.Interop.Excel.Application excel;
            Microsoft.Office.Interop.Excel.Workbook worKbooK;
            Microsoft.Office.Interop.Excel.Worksheet worKsheeT;
            Microsoft.Office.Interop.Excel.Range celLrangE;

            try
            {
                excel = new Microsoft.Office.Interop.Excel.Application();
                excel.Visible = false;
                excel.DisplayAlerts = false;
                worKbooK = excel.Workbooks.Add(Type.Missing);


                worKsheeT = (Microsoft.Office.Interop.Excel.Worksheet)worKbooK.ActiveSheet;
                worKsheeT.Name = wfTypeName;

                System.Data.DataTable wfTable = CreateTable(wfInformationDictionary);

                int rowcount = 2;

                foreach (DataRow datarow in wfTable.Rows)
                {
                    rowcount += 1;
                    
                    for (int i = 1; i <= wfTable.Columns.Count; i++)
                    {

                        if (rowcount == 3)
                        {
                            worKsheeT.Cells[2, i] = wfTable.Columns[i - 1].ColumnName;

                        }

                        worKsheeT.Cells[rowcount, i] = datarow[i - 1].ToString();

                        if (rowcount > 3)
                        {
                            if (i == wfTable.Columns.Count)
                            {
                                if (rowcount % 2 == 0)
                                {
                                    celLrangE = worKsheeT.Range[worKsheeT.Cells[rowcount, 1], worKsheeT.Cells[rowcount, wfTable.Columns.Count]];
                                }

                            }
                        }

                    }

                }

                celLrangE = worKsheeT.Range[worKsheeT.Cells[1, 1], worKsheeT.Cells[rowcount, wfTable.Columns.Count]];
                celLrangE.EntireColumn.AutoFit();
                celLrangE = worKsheeT.Range[worKsheeT.Cells[1, 1], worKsheeT.Cells[2, wfTable.Columns.Count]];

                worKbooK.SaveAs(pathLogs + wfOrder + "_" + wfTypeName + ".xls");
                worKbooK.Close();
                excel.Quit();

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
            finally
            {
                worKsheeT = null;
                celLrangE = null;
                worKbooK = null;
            }  
        }


        public static System.Data.DataTable CreateTable(Dictionary<string, List<string>> wfInformationDictionary)
        {
            System.Data.DataTable table = new System.Data.DataTable();

            try
            {

                table.Columns.Add("WFID", typeof(string));
                table.Columns.Add("WFTitle", typeof(string));
                table.Columns.Add("InitialSteps", typeof(string));
                table.Columns.Add("InitialStepBackupGroups", typeof(string));
                table.Columns.Add("OtherInitialData", typeof(string));
                table.Columns.Add("ActorsSignedRole", typeof(string));
                table.Columns.Add("Updated", typeof(string));

                foreach (KeyValuePair<string, List<string>> kvp in wfInformationDictionary)
                {
                    table.Rows.Add(kvp.Key, kvp.Value[0], kvp.Value[1], kvp.Value[2], kvp.Value[3], kvp.Value[4], kvp.Value[5]);
                }

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
            return table;
        }  

        #endregion

        #region <PERMISSIONS>

        private static void UpdatePermissionsModule(string currentBackupGroups, SPListItemCollection stepCollection, SPListItem wfItem, SPWeb web, string status, bool isConfidential, SPRoleDefinition roleDefinitionRSContributor, SPRoleDefinition roleDefinitionRSRead, string wfid, Dictionary<string, SPUser> newBackupGroupsDictionary, List<string> stepsToReplaceList, string domain, string currentStep)
        {
     
            try
            {
                if (!string.IsNullOrEmpty(currentBackupGroups))
                {
                    Dictionary<string, SPUser> currentBackupGroupsDictionary = GetCurrentBackupGroupsList(currentBackupGroups, stepCollection, web, newBackupGroupsDictionary, ref stepsToReplaceList, domain, wfid);

                    if (stepsToReplaceList.Count > 0)
                    {

                        //NON-RESTRICTED
                        if (((!status.Equals("Closed")) && (!status.Equals("Deleted"))) && isConfidential.Equals(false))
                        {
                            //wfItem (Document Library)
                            RemoveBackupGroups(ref wfItem, currentBackupGroupsDictionary);
                            AddBackupGroupsLibrary(wfItem, roleDefinitionRSContributor, roleDefinitionRSRead, newBackupGroupsDictionary, stepsToReplaceList, currentStep);

                        }
                        else if (isConfidential.Equals(true))
                        {
                            //wfItem (Document Library)
                            RemoveBackupGroups(ref wfItem, currentBackupGroupsDictionary);

                            if ((!status.Equals("Closed")) && (!status.Equals("Deleted")))
                                AddBackupGroupsLibrary(wfItem, roleDefinitionRSContributor, roleDefinitionRSRead, newBackupGroupsDictionary, stepsToReplaceList, currentStep);
                            else
                                AddBackupGroupsRead(wfItem, roleDefinitionRSRead, newBackupGroupsDictionary, stepsToReplaceList);

                            //item (History)
                            SPListItem wfHistoryItem = SP.GetWFTHistoryItem(web, wfid);

                            if (wfHistoryItem != null)
                            {
                                RemoveBackupGroups(ref wfHistoryItem, currentBackupGroupsDictionary);
                                AddBackupGroupsRead(wfHistoryItem, roleDefinitionRSRead, newBackupGroupsDictionary, stepsToReplaceList);
                            }

                        }

                        if (isConfidential)
                            General.TraceInformation("- Permissions updated (Restricted)", ConsoleColor.Yellow);
                        else
                            General.TraceInformation("- Permissions updated (Non-Restricted)", ConsoleColor.Yellow);
                    }
                

                }

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }

        private static Dictionary<string, SPUser> GetCurrentBackupGroupsList(string currentBackupGroups, SPListItemCollection stepCollection, SPWeb web, Dictionary<string, SPUser> newBackupGroupsDictionary, ref List<string> stepsToReplaceList, string domain, string wfid)
        {
            Dictionary<string, SPUser> currentBackupGroupsDictionary = new Dictionary<string, SPUser>();

            try
            {
                string[] stepBackupAux = Regex.Split(currentBackupGroups, "&#");

                foreach (string inf in stepBackupAux)
                {
                    string newValue = string.Empty;

                    if (inf.Contains(";#"))
                    {
                        string[] currentStepInf = Regex.Split(inf, ";#");
                        string stepNumber = currentStepInf[0];
                        string idGroup = currentStepInf[1];
                        string currentBackupGroup = currentStepInf[2];
                       

                        if (!string.IsNullOrEmpty(currentBackupGroup))
                        {
                            if (newBackupGroupsDictionary.ContainsKey(stepNumber) && (!newBackupGroupsDictionary[stepNumber].ID.ToString().Equals(idGroup)))
                            {
                                if (!currentBackupGroupsDictionary.ContainsKey(idGroup))
                                {
                                    if (currentBackupGroup.ToLower().Contains(domain.ToLower()))
                                        currentBackupGroup = currentBackupGroup.ToLower().Replace(domain.ToLower() + "\\", null).Trim();

                                    try
                                    {
                                        SPUser backupGroup = web.EnsureUser(currentBackupGroup);

                                        if (backupGroup != null)
                                            currentBackupGroupsDictionary.Add(idGroup, backupGroup);
                                    }
                                    catch
                                    {
                                        General.TraceInformation("*** ERROR: WFID: '" + wfid + "' - Backup Group: '" + currentBackupGroup + "' does not exist.", ConsoleColor.Red);
                                    }
                                }

                                if (!stepsToReplaceList.Contains(stepNumber))
                                    stepsToReplaceList.Add(stepNumber);
                            }
                            
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return currentBackupGroupsDictionary;
        }

        private static Dictionary<string, SPUser> GetNewBackupGroupsDictionary(SPListItemCollection stepCollection, SPWeb web)
        {
            Dictionary<string, SPUser> newBackupGroupsDictionary = new Dictionary<string, SPUser>();

            try
            {
                foreach (SPListItem item in stepCollection)
                {
                    if (item["StepBackupGroup"] != null)
                    {
                        string stepNumber = item["StepNumber"].ToString();
                        SPFieldUserValue backupGroupValue = new SPFieldUserValue(web, item["StepBackupGroup"].ToString());

                        if (backupGroupValue != null)
                        {

                            if (!newBackupGroupsDictionary.ContainsKey(stepNumber))
                                newBackupGroupsDictionary.Add(stepNumber, backupGroupValue.User);
                        }
                        else
                            General.TraceInformation("*** ERROR: WFType: '" + item["Title"].ToString() + "' - Backup Group: '" + item["StepBackupGroup"].ToString() + "' does not exist.", ConsoleColor.Red);

                    }
                }
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return newBackupGroupsDictionary;
        }

        private static void RemoveBackupGroups(ref SPListItem wfItem, Dictionary<string, SPUser> currentBackupGroupsDictionary)
        {
            try
            {
                if (!wfItem.HasUniqueRoleAssignments)
                    wfItem.BreakRoleInheritance(true, true);

                SPRoleAssignmentCollection SPRoleAssign = wfItem.RoleAssignments;

                foreach (string id in currentBackupGroupsDictionary.Keys)
                {
                        SPRoleAssign.RemoveById(Convert.ToInt32(id));
                }
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }

        private static void AddBackupGroupsLibrary(SPListItem wfItem, SPRoleDefinition roleDefinitionRSContributor,SPRoleDefinition roleDefinitionRSRead,  Dictionary<string, SPUser> newBackupGroupsDictionary, List<string> stepsToReplaceList, string currentStep)
        {
            try
            {
                foreach (KeyValuePair<string, SPUser> kvp in newBackupGroupsDictionary)
                {
                    SPUser backupGroup = kvp.Value;
                    string stepNumber = kvp.Key;

                    if (stepsToReplaceList.Contains(stepNumber))
                    {
                        SPRoleAssignment roleAssignment = new SPRoleAssignment(backupGroup);

                        if (stepNumber.Equals("1") || stepNumber.Equals(currentStep))
                            roleAssignment.RoleDefinitionBindings.Add(roleDefinitionRSContributor);
                        else
                            roleAssignment.RoleDefinitionBindings.Add(roleDefinitionRSRead);

                        wfItem.RoleAssignments.Add(roleAssignment);
                    }
                }
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }

        private static void AddBackupGroupsRead(SPListItem wfItem, SPRoleDefinition roleDefinitionRSRead, Dictionary<string, SPUser> newBackupGroupsDictionary, List<string> stepsToReplaceList)
        {
            try
            {
                foreach (KeyValuePair<string, SPUser> kvp in newBackupGroupsDictionary)
                {
                    SPUser backupGroup = kvp.Value;
                    string stepNumber = kvp.Key;

                    if (stepsToReplaceList.Contains(stepNumber))
                    {
                        SPRoleAssignment roleAssignment = new SPRoleAssignment(backupGroup);
                        roleAssignment.RoleDefinitionBindings.Add(roleDefinitionRSRead);
                        wfItem.RoleAssignments.Add(roleAssignment);
                    }
                }
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }


        #endregion
    }
}
