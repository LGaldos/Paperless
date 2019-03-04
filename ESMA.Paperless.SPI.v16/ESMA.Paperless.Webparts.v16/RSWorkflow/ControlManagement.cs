using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using Microsoft.SharePoint;
using System.Web.UI;
using Microsoft.SharePoint.WebControls;
using System.Web;
using Microsoft.SharePoint.WebPartPages;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Web.UI.HtmlControls;
using Microsoft.SharePoint.Utilities;
using System.Collections;
using System.Text.RegularExpressions;
using System.Reflection;

namespace ESMA.Paperless.Webparts.v16.RSWorkflow
{
    public static class ControlManagement
    {
        #region Actors

        /// <summary>
        /// Initialize the controls related to workflow actors management
        /// </summary>
        public static void InitializeActorControls(ref List<DropDownList> groupDDLs, ref List<Label> groupLabels, ref RadioButtonList groupRadioButtons)
        {
            groupDDLs = new List<DropDownList>();
            groupLabels = new List<Label>();
            groupRadioButtons = new RadioButtonList();
            groupRadioButtons.ID = "rbl_UsersToReject";
            groupRadioButtons.Text = string.Empty;
            groupRadioButtons.Visible = false;
        }

        /// <summary>
        /// Add workflow actors to workflow step lists
        /// </summary>
        public static void PopulateActorLists(string wfid, int currentStep, string groupName, object actorsModified, Dictionary<string, string> groupUsers, ref List<DropDownList> groupDDLs, ref List<Label> groupLabels, string stepNumber, SPWeb Web, Dictionary<string, string> parameters, List<SPUser> owners)
        {
            try
            {
                if (groupUsers != null)
                {
                    DropDownList ddlGroup = new DropDownList();
                    ddlGroup.ID = "ddl" + stepNumber + "_" + wfid + "_" + groupName;
                    ddlGroup.AutoPostBack = true;
                    ddlGroup.SelectedIndexChanged += new EventHandler(ddlGroupRetain_SelectedIndexChanged);
                    int stepNumberInt = int.Parse(stepNumber);

                    Label lblGroup = new Label();

                    IEnumerable<KeyValuePair<string, string>> items = from pair in groupUsers orderby pair.Value ascending select pair;
                    groupUsers = items.ToDictionary<KeyValuePair<string, string>, string, string>(pair => pair.Key, pair => pair.Value);

                    ddlGroup.DataSource = groupUsers;
                    ddlGroup.DataTextField = "Value";
                    ddlGroup.DataValueField = "Key";
                    ddlGroup.DataBind();

                    if (!stepNumberInt.Equals(1))
                        ddlGroup.Items.Insert(0, string.Empty);

                    if (IsActorModified_byStep(stepNumber, wfid, actorsModified))
                        ddlGroup.SelectedValue = GetValueActorModified(stepNumber, wfid, actorsModified);
                    else if (owners.Count >= stepNumberInt)
                    {
                        SPUser userToSelect = owners[stepNumberInt - 1];

                        if (userToSelect != null)
                        {
                            string userLoginNameAlt = userToSelect.LoginName;
                            string userNameAlt = userToSelect.Name;

                            General.GetUserData(ref userLoginNameAlt, ref userNameAlt);

                            if (ddlGroup.Items.FindByValue(userLoginNameAlt.ToUpper()) != null)
                            {
                                ListItem listItem = ddlGroup.Items.FindByValue(userLoginNameAlt.ToUpper());
                                ddlGroup.SelectedIndex = ddlGroup.Items.IndexOf(listItem);
                            }
                        }
                        else
                        {
                            ddlGroup.SelectedIndex = 0;
                        }
                    }

                    EditActorListIDsAndNames(groupName, ref ddlGroup, ref lblGroup, stepNumber, parameters);

                    groupDDLs.Add(ddlGroup);
                    groupLabels.Add(lblGroup);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "PopulateActorLists " + ex.Message);
            }
        }

        /// <summary>
        /// Select the responsible actors of each workflow step
        /// </summary>
        public static void PreSelectActorLists(bool itemIsOld, ref Panel placeholder, SPListItem item, SPUser loggedUser, SPWeb Web, Dictionary<string, string> parameters, object actorsModified, List<string> groupNames, string wfid, string userAD, string passwordAD, int currentStep, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, bool isSaving)
        {
            try
            {
                int count = 1;
                string domain = parameters["Domain"];
                string currentStatus = WorkflowDataManagement.GetWorkflowStatus(item, Web, parameters, wfid);
                SPUser administratorUser = General.GetAdministratorUser(parameters, Web, wfid);
                bool isConfidential = false;
                string confidentialValue = string.Empty;

                if (item["ConfidentialWorkflow"] != null)
                {
                    isConfidential = item["ConfidentialWorkflow"].ToString().ToUpper().Equals("RESTRICTED") ? true : false;
                    confidentialValue = item["ConfidentialWorkflow"].ToString();
                }

                foreach (Control control in placeholder.Controls)
                {
                    try
                    {
                        if (control is UpdatePanel)
                        {
                            UpdatePanel up = (UpdatePanel)control;
                            DropDownList ddl = (DropDownList)up.Controls[0].Controls[0];

                            //If item has been previously created.
                            if (itemIsOld && item != null)
                            {
                                if (IsActorModified_byStep(count.ToString(), wfid, actorsModified))
                                    ddl.SelectedValue = GetValueActorModified(count.ToString(), wfid, actorsModified);
                                else
                                {
                                    //Step {N} Assigned To field stores the responsible of {N} workflow step
                                    string fieldName = "Step_x0020_" + count.ToString() + "_x0020_Assigned_x0020_To";
                                    bool responsibleExist = false;
                                    SPUser responsibleUser = null;
                                    string responsibleName = string.Empty;
                                    string groupName = groupNames[(count - 1)].ToString();
                                    

                                    if (item.Fields.ContainsField(fieldName))
                                    {
                                       
                                        if (item[fieldName] != null)
                                        {
                                            //Get workflow step responsible user and check if it exists in Active Directory
                                            responsibleUser = General.GetSPUserObject(item, fieldName, wfid, Web);

                                            if (responsibleUser != null)
                                            {
                                                responsibleName = responsibleUser.Name;
                                                string userAccount = responsibleUser.LoginName;
                                                responsibleExist = CheckIfResponsibleUserExist(wfid, domain, userAD, passwordAD, userAccount);


                                                try
                                                {
                                                    //ESMA-CR31-Backup Groups
                                                    bool isBackupInitiator = false;
                                                    bool isBackupResponsible = false;
                                                    bool isBackupMember = WorkflowDataManagement.IsMemberOfBackupGroup(responsibleUser, wfid, domain, actorsBackupDictionary, userAD, passwordAD, ref isBackupInitiator, ref isBackupResponsible, Convert.ToString(count), parameters);


                                                    if (!responsibleExist)
                                                        Actions_UserNotExist(wfid, Web, ddl, domain, userAD, passwordAD, parameters, count, currentStep, currentStatus, ref item, administratorUser, fieldName, responsibleUser, responsibleName, groupName, confidentialValue, isConfidential, userAccount, isBackupResponsible, actorsBackupDictionary, reassignToBackupActor, isSaving);
                                                    else
                                                        Actions_UserExists(wfid, Web, ddl, domain, userAD, passwordAD, parameters, count, currentStep, currentStatus, ref item, administratorUser, fieldName, responsibleUser, responsibleName, groupName, confidentialValue, isConfidential, userAccount, isBackupResponsible, actorsBackupDictionary, reassignToBackupActor, isSaving);

                                                }
                                                catch
                                                {
                                                    count++;

                                                    if (responsibleExist)
                                                        General.saveErrorsLog(wfid, "PreSelectActorLists. Error in corresponding actions. ResponsibleExist:YES. Step: " + count + ". ResponsibleUser: " + responsibleName + " - GroupName: " + groupName);
                                                    else
                                                        General.saveErrorsLog(wfid, "PreSelectActorLists. Error in corresponding actions. ResponsibleExist:NO. Step: " + count + ". ResponsibleUser: " + responsibleName + " - GroupName: " + groupName);

                                                    ddl.SelectedIndex = 0;

                                                    continue;
                                                }

                                            }
                                            else
                                                ddl.SelectedIndex = 0;
                                        }
                                        else
                                            ddl.SelectedIndex = 0;
                                    }
                                }
                            }


                            //Item is being created>
                            else if (count.Equals(1))
                                LoadInitiatorWF(wfid, count, parameters, ddl, loggedUser);
                           
                            count++;
                        }
                    }
                    catch (Exception ex)
                    {
                        count++;
                        General.saveErrorsLog(wfid, "PreSelectActorLists-1 " + ex.Message);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "PreSelectActorLists " + ex.Message);
            }
        }

        private static void LoadInitiatorWF(string wfid, int count, Dictionary<string, string> parameters, DropDownList ddl, SPUser loggedUser)
        {
            try
            {
                //Find user in workflow step actors list and select it
                string initiatorLoginName = Permissions.GetOnlyUserAccount(loggedUser.LoginName, wfid);

                if (!string.IsNullOrEmpty(initiatorLoginName))
                {
                    if (ddl.Items.FindByValue(initiatorLoginName.ToUpper()) != null)
                    {
                        ListItem listItem = ddl.Items.FindByValue(initiatorLoginName.ToUpper());
                        ddl.SelectedIndex = ddl.Items.IndexOf(listItem);
                    }
                }
                else
                {
                    General.saveErrorsLog(wfid, "LoadInitiatorWF() - Initiator is NULL. LoggedUser: " + loggedUser.LoginName);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "LoadInitiatorWF() " + ex.Message);
            }
        }

        private static bool CheckIfReassignToDefaultUser(int step, int currentStep, bool isConfidential, string status, Dictionary<string, string> parameters)
        {
            return !(isConfidential || status.ToUpper() == parameters["Status Closed"].ToUpper() || status.ToUpper() == parameters["Status Deleted"].ToUpper() || step < currentStep || (currentStep.Equals(1) && status.ToUpper().Equals(parameters["Status Draft"].ToUpper())));
        }

        private static bool CheckIfResponsibleUserExist(string wfid, string domain, string userAD, string passwordAD, string userAccount)
        {
            bool responsibleExist = false;

            try
            {

                if (!string.IsNullOrEmpty(userAccount))
                    responsibleExist = General.ExistUserAD(userAccount, domain, wfid, userAD, passwordAD);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "CheckIfResponsibleUserExist() " + ex.Message);
            }

            return responsibleExist;
        }

        private static void Actions_UserNotExist(string wfid, SPWeb Web, DropDownList ddl, string domain, string userAD, string passwordAD, Dictionary<string, string> parameters, int count, int currentStep, string currentStatus, ref SPListItem item, SPUser administratorUser, string fieldName, SPUser responsibleUser, string responsibleName, string groupName, string confidentialValue, bool isConfidential, string userAccount, bool isBackupResponsible, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, bool isSaving)
        {
            try
            {
                //Update the Display Name
                General.UpdateUserNameToDeleted(Web, ref responsibleName, wfid);

                if (CheckIfReassignToDefaultUser(count, currentStep, isConfidential, currentStatus, parameters) && !isBackupResponsible)
                    Actions_Reassigning(wfid, Web, ddl, domain, userAD, passwordAD, count, parameters, groupName, administratorUser, currentStatus, currentStep, confidentialValue, fieldName, ref item, actorsBackupDictionary, reassignToBackupActor, isSaving);
                else
                    LoadResponsibleDDL(wfid, ddl, parameters, count, responsibleName, responsibleUser, groupName);


            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "Actions_UserNotExist() " + ex.Message);
            }
        }

        private static void Actions_UserExists(string wfid, SPWeb Web, DropDownList ddl, string domain, string userAD, string passwordAD, Dictionary<string, string> parameters, int count, int currentStep, string currentStatus, ref SPListItem item, SPUser administratorUser, string fieldName, SPUser responsibleUser, string responsibleName, string groupName, string confidentialValue, bool isConfidential, string userAccount, bool isBackupResponsible, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, bool isSaving)
        {
            try
            {
                //Update the Display Name
                if (responsibleName.ToLower().Contains("(deleted)"))
                    General.UpdateActiveUserName(Web, ref  responsibleName, wfid);

                userAccount = General.GetOnlyUserAccount(userAccount);
                //Find user in workflow step actors list and select it
                ListItem listItem = ddl.Items.FindByValue(userAccount.ToUpper());

                if (listItem != null)
                {
                    int indexOf = ddl.Items.IndexOf(listItem);
                    if (indexOf >= 0)
                        ddl.SelectedIndex = indexOf;
                }
                else
                {
                    //The user has been moved to other Paperless group.
                    if (CheckIfReassignToDefaultUser(count, currentStep, isConfidential, currentStatus, parameters) && !isBackupResponsible)
                        Actions_Reassigning(wfid, Web, ddl, domain, userAD, passwordAD, count, parameters, groupName, administratorUser, currentStatus, currentStep, confidentialValue, fieldName, ref item, actorsBackupDictionary, reassignToBackupActor, isSaving);
                    else
                        LoadResponsibleDDL(wfid, ddl, parameters, count, responsibleName, responsibleUser, groupName);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "Actions_UserExists() " + ex.Message);
            }
        }


        private static void Actions_Reassigning(string wfid, SPWeb Web, DropDownList ddl, string domain, string userAD, string passwordAD, int count, Dictionary<string, string> parameters, string groupName, SPUser administratorUser, string currentStatus, int currentStep, string confidentialValue, string fieldName, ref SPListItem item, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, bool isSaving)
        {
            try
            {
                //News Step -> Go to default User
                SPUser defaultUser = General.GetDefaultUserToReassign(groupName, parameters, Web, administratorUser, wfid, domain, userAD, passwordAD);

                if (defaultUser != null)
                {
                    ReassigningToDefaultUserModule_SharePoint(Web, ref item, parameters, count, wfid, fieldName, currentStatus, currentStep, groupName, confidentialValue, defaultUser, actorsBackupDictionary, reassignToBackupActor, isSaving);
                    ReassigningToDefaultUserModule_UI(ddl, parameters, wfid, defaultUser);
                }
                else
                {
                    ddl.SelectedIndex = 0;
                    General.saveErrorsLog(wfid, "Actions_Reassigning() - Step '" + count + "' - All DefaultUsers (Default Group + Admin User) are NULL.");
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "Actions_Reassigning()" + ex.Message);
            }
        }

        private static void LoadResponsibleDDL(string wfid, DropDownList ddl, Dictionary<string, string> parameters, int count, string responsibleName, SPUser responsibleUser, string groupName)
        {
            try
            {

                string userLogin = Permissions.GetOnlyUserAccount(responsibleUser.LoginName, wfid);

                if (!string.IsNullOrEmpty(userLogin))
                {
                    userLogin = General.GetOnlyUserAccount(userLogin);
                    ListItem listItem = ddl.Items.FindByValue(userLogin.ToUpper());

                    //if (listItem == null)
                    //    General.saveErrorsLog(wfid, "LoadResponsibleDDL - Step '" + count + "'  Responsible: " + responsibleName + "(" + userLogin + ") is not member of " + groupName);

                    ddl.Items.Add(new ListItem(responsibleName, userLogin.ToUpper()));
                    ddl.SelectedValue = userLogin.ToUpper();
                }
                else
                {
                    ddl.SelectedIndex = 0;
                    General.saveErrorsLog(wfid, "LoadResponsibleDDL- Actions_UserNotExist() - Step '" + count + "' - doesn't have to be reassigned but responsible account is not found. Responsible: " + responsibleName + "(" + userLogin + ")");
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "LoadResponsibleDDL()" + ex.Message);
            }
        }

        
       


        /// <summary>
        /// Change workflow step actor to default actor if workflow step actor does not exist in Active Directory
        /// </summary>
        public static void ReassigningToDefaultUserModule_SharePoint(SPWeb Web, ref SPListItem item, Dictionary<string, string> parameters, int count, string wfid, string fieldName, string status, int currentStep, string groupName, string confidentialValue, SPUser defaultUser, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, bool isSaving)
        {
            try
            {

                //TBC
                bool unsafeUpdates = Web.AllowUnsafeUpdates;
                Web.AllowUnsafeUpdates = true;

                SPUser editorUser = General.GetSPUserObject(item, "Editor", wfid, Web);
                DateTime modifiedDate = Convert.ToDateTime(item["Modified"]);

                //AssignedPerson
                if (currentStep.Equals(count))
                {
                    WorkflowDataManagement.SetAssignedPersonWorkflow(ref item, defaultUser, editorUser, parameters, confidentialValue, wfid, actorsBackupDictionary, status, reassignToBackupActor, currentStep, isSaving);
                    WorkflowDataManagement.SetAssignedPersonWorkflowHistory(Web, defaultUser, editorUser, modifiedDate, wfid, reassignToBackupActor, status, parameters, confidentialValue, actorsBackupDictionary, currentStep, isSaving);
                }

                //Step X Assigned To
                WorkflowDataManagement.SetWorkflowStepResponsible(ref item, defaultUser, editorUser, fieldName, modifiedDate, parameters, confidentialValue, wfid, actorsBackupDictionary, status, reassignToBackupActor, currentStep, isSaving);

                Web.AllowUnsafeUpdates = unsafeUpdates;

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "ReassigningToDefaulUserModule_SharePoint()" + ex.Message);
            }
        }

        private static void ReassigningToDefaultUserModule_UI(DropDownList ddl, Dictionary<string, string> parameters, string wfid, SPUser defaultUser)
        {
            try
            {
                string auxLogName = Permissions.GetOnlyUserAccount(defaultUser.LoginName, wfid).ToUpper();
                ListItem listItemAux = ddl.Items.FindByValue(auxLogName);

                if (listItemAux != null)
                {
                    int indexOf = ddl.Items.IndexOf(listItemAux);
                    if (indexOf >= 0)
                        ddl.SelectedIndex = indexOf;
                }
                else
                {
                    ddl.Items.Add(new ListItem(defaultUser.Name, auxLogName));
                    ddl.SelectedValue = auxLogName;
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "ReassigningToDefaultUserModule_UI() " + ex.Message);
            }
        }



        #region <RETAIN CONFIDENTIAL VALUE>

        /// <summary>
        /// Store the selected value fot confidential configuration.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ddlConfidential_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                DropDownList ddl = (DropDownList)sender;
                string wfid = HttpContext.Current.Session["FormWFID"].ToString();
                HttpContext.Current.Session["FormConfidentialModified" + wfid] = ddl.SelectedValue;
            }
            catch
            {
            }
        }
        
        #endregion

        #region <RETAIN ACTOR VALUES>

        /// <summary>
        /// Store the selected actors during step signing and workflow step responsibility changing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ddlGroupRetain_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                DropDownList ddl = (DropDownList)sender;
                RetainControlValueActors(ddl.ID, ddl.SelectedValue);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "ddlGroupRetain_SelectedIndexChanged() - " + ex.Message);
            }
        }

        /// <summary>
        /// Store in a session state a string dictionary with all workflow step responsibles.
        /// </summary>
        /// <param name="ddlID"></param>
        /// <param name="value"></param>
        public static void RetainControlValueActors(string ddlID, string value)
        {
            string wfid = string.Empty;

            try
            {
                Dictionary<string, string> ControlKeys;

                string stepNumber = GetStepNumber_by_ddlID(ddlID);
                wfid = ddlID.Split('_')[1];

                if (HttpContext.Current.Session["FormActorsModifiedDictionary" + wfid] != null)
                {
                    ControlKeys = (Dictionary<string, string>)HttpContext.Current.Session["FormActorsModifiedDictionary" + wfid];

                    if (ControlKeys.ContainsKey(stepNumber))
                        ControlKeys[stepNumber] = value;
                    else
                        ControlKeys.Add(stepNumber, value);
                }
                else
                {
                    ControlKeys = new Dictionary<string, string>();
                    ControlKeys.Add(stepNumber, value);
                }

                HttpContext.Current.Session["FormActorsModifiedDictionary" + wfid] = ControlKeys;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "RetainControlValueActors() - " + ex.Message);
            }
        }

        /// <summary>
        /// Get modified workflow step responsible by step number.
        /// </summary>
        /// <param name="stepNumber"></param>
        /// <returns>User account name</returns>
        private static string GetValueActorModified(string stepNumber, string wfid, object actorsModified)
        {
            try
            {
                string value = string.Empty;

                if (actorsModified != null)
                {
                    Dictionary<string, string> ControlKeys = (Dictionary<string, string>)actorsModified;
                    value = ControlKeys[stepNumber].ToString();
                }

                return value;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetValueActorModified() - " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Get if a workflow step responsible has been modified by step number. 
        /// </summary>
        /// <param name="stepNumber"></param>
        /// <returns>True if workflow step responsible has been modified, otherwise False.</returns>
        private static bool IsActorModified_byStep(string stepNumber, string wfid, object actorsModified)
        {
            try
            {
                bool isModified = false;

                if (actorsModified != null)
                {
                    Dictionary<string, string> ControlKeys = (Dictionary<string,string>)actorsModified;

                    if (ControlKeys.ContainsKey(stepNumber))
                        isModified = true;
                }

                return isModified;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "IsActorModified_byStep() - " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Get workflow step actor control ID.
        /// </summary>
        /// <param name="ddlID"></param>
        /// <returns>Workflow step actor list step number based on its control ID.</returns>
        public static string GetStepNumber_by_ddlID(string ddlID)
        {
            try
            {
                string stepNumber = string.Empty;

                if (ddlID.Contains("ddl"))
                {
                    stepNumber = ddlID.Replace("ddl", string.Empty);
                    stepNumber = stepNumber.Substring(0, (stepNumber.IndexOf("_")));
                }

                return stepNumber;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetStepNumber_by_ddlID() - " + ex.Message);
                return null;
            }
        }

        #endregion

        /// <summary>
        /// Edit actor lists IDs and Names
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="ddlGroup"></param>
        /// <param name="lblGroup"></param>
        /// <param name="loggedUser"></param>
        /// <param name="stepNumber"></param>
        /// <param name="first"></param>
        /// <param name="parameters"></param>
        private static void EditActorListIDsAndNames(string groupName, ref DropDownList ddlGroup, ref Label lblGroup, string stepNumber, Dictionary<string, string> parameters)
        {
            try
            {
                lblGroup.ID = "lbl" + stepNumber + "_" + groupName;
                groupName = groupName.Substring(0, 1).ToUpper() + groupName.Substring(1, groupName.Length-1);
                lblGroup.Text = General.GetGroupName(groupName, parameters) + ": ";
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "ConfigureActorList " + ex.Message);
            }
        }

        /// <summary>
        /// Set Actor lists and rejecting area.
        /// </summary>
        public static void SetActorArea(SPListItem item, string wfid, int currentStep, List<string> groupNames, string status, string wftype, object actorsModified, SPWeb Web, List<SPUser> groupOwners, List<DropDownList> groupDDLs, List<Label> groupLabels, Panel DynamicUserListsPanel, Panel DynamicRadioButtonListPanel, RadioButtonList groupRadioButtons, Dictionary<string, string> parameters, string userAD, string passwordAD)
        {
            try
            {
                Hashtable groupsTable = new Hashtable();
                bool allowNestedGroups = false;

                if (groupNames != null && groupNames.Count > 0)
                {
                    int count = 1;

                    //ESMA-CR28-Nested Groups
                    if (parameters["Nested Groups"].ToLower().Equals("true"))
                        allowNestedGroups = true;

                    foreach (string groupName in groupNames)
                    {
                        try
                        {
                            Dictionary<string, string> groupUsers = null;

                            if (!groupsTable.ContainsKey(groupName))
                            {
                                groupUsers = General.GetUsersFromActiveDirectory(parameters["Domain"], groupName, userAD, passwordAD, wfid, allowNestedGroups);
                                groupsTable.Add(groupName, groupUsers);
                            }
                            else
                            {
                                try { groupUsers = (Dictionary<string, string>)groupsTable[groupName]; }
                                catch (Exception ex) { General.saveErrorsLog(wfid, "SetActorArea-1 " + ex.Message); }
                            }

                            ControlManagement.PopulateActorLists(wfid, currentStep, groupName, actorsModified, groupUsers, ref groupDDLs, ref groupLabels, count.ToString(), Web, parameters, groupOwners);

                            count++;
                        }
                        catch (Exception ex)
                        {
                            General.saveErrorsLog(wfid, "SetActorArea-2 " + ex.Message);
                            continue;
                        }
                    }
                    
                }

                if (groupLabels != null && groupDDLs != null && groupLabels.Count > 0 && groupDDLs.Count > 0 && groupDDLs.Count.Equals(groupLabels.Count))
                {
                    for (int i = 0; i < groupLabels.Count; i++)
                    {
                        try
                        {
                            bool closedWF = status.ToUpper().Equals(parameters["Status Closed"].ToUpper()) || status.ToUpper().Equals(parameters["Status Deleted"].ToUpper());
                            if (i.Equals(currentStep - 1) && !closedWF)
                                groupLabels[i].CssClass = "current-step";
                            else
                            {
                                groupLabels[i].CssClass = "label-workflow";
                                groupLabels[i].CssClass += " fix-width";
                            }

                            groupDDLs[i].CssClass = "input_select_actors";
                            groupDDLs[i].CssClass += " chosen-actors";
                            DynamicUserListsPanel.Controls.Add(groupLabels[i]);
                            UpdatePanel updPanel = new UpdatePanel();
                            updPanel.ID = "UpdatePanel_" + wfid + "_" + groupLabels[i].ID;
                            updPanel.ChildrenAsTriggers = true;
                            updPanel.UpdateMode = UpdatePanelUpdateMode.Conditional;
                            updPanel.ContentTemplateContainer.Controls.Add(groupDDLs[i]);
                            DynamicUserListsPanel.Controls.Add(updPanel);
                        }
                        catch { continue; }
                    }
                }

                if (groupRadioButtons != null)
                {
                    groupRadioButtons.CssClass = "label_span";
                    DynamicRadioButtonListPanel.Controls.Add(groupRadioButtons);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetActorArea " + ex.Message);
            }
        }

        /// <summary>
        /// Get selected workflow step responsible during step signing.
        /// </summary>
        /// <param name="step"></param>
        /// <param name="Web"></param>
        /// <param name="DynamicUserListsPanel"></param>
        /// <param name="parameters"></param>
        /// <param name="item"></param>
        /// <param name="wfid"></param>
        /// <param name="userAD">Encrypted administrator user login name</param>
        /// <param name="passwordAD">Encrypted administrator user password</param>
        /// <returns>SharePoint SPUser object for selected workflow step responsible by step number.</returns>
        public static SPUser GetStepResponsible(int step, SPWeb Web, Panel DynamicUserListsPanel, Dictionary<string, string> parameters, SPListItem item, string wfid, string userAD, string passwordAD)
        {
            SPUser userToReturn = null;
            int count = 1;
            bool isConfidential = item["ConfidentialWorkflow"] != null && item["ConfidentialWorkflow"].ToString().ToUpper().Equals("RESTRICTED") ? true : false;
            SPUser administratorUser = General.GetAdministratorUser(parameters, Web, wfid);
            List<string> groupNames = WorkflowDataManagement.GetGroupNames(item["InitialSteps"] != null ? item["InitialSteps"].ToString() : string.Empty, Web, wfid);
            string domain = parameters["Domain"];

            try
            {
                foreach (Control control in DynamicUserListsPanel.Controls)
                {
                    if (control is UpdatePanel)
                    {
                        if (count.Equals(step))
                        {
                            UpdatePanel up = (UpdatePanel)control;
                            DropDownList ddl = (DropDownList)up.Controls[0].Controls[0];
                            if (ddl != null)
                            {
                                string userSelected = ddl.SelectedValue;
                                if (!string.IsNullOrEmpty(userSelected))
                                {
                                    if (isConfidential || General.ExistUserAD(userSelected, domain, wfid, userAD, passwordAD) == true)
                                    {
                                        string fieldName = "Step_x0020_" + step.ToString() + "_x0020_Assigned_x0020_To";

                                        try
                                        {
                                            userToReturn = Web.EnsureUser(domain + "\\" + userSelected);

                                            if (userToReturn == null)
                                            {
                                         
                                                userToReturn = General.GetSPUserObject(item, fieldName, wfid, Web);

                                                General.saveErrorsLog(wfid, "GetStepResponsible - User '" + userSelected + "'. Error in 'AssignedTo' column to add it. (Responsible is NULL)");
                                                General.saveErrorsLog(wfid, "Getting user from '" + fieldName + "' - User '" + userSelected + "'.");
                                            }
                                        }
                                        catch
                                        {
                                            General.saveErrorsLog(wfid, "GetStepResponsible Exception: Catch - '" + count + "' - userSelected: '" + userSelected + "'.");
                                            General.saveErrorsLog(wfid, "Getting user from '" + fieldName + "' - User '" + userSelected + "'.");
                                            count++;
                                            continue;
                                        }

                                       
                                    }
                                    else
                                    {
                                        string groupName = groupNames[(count - 1)].ToString();
                                        userToReturn = General.GetDefaultUserToReassign(groupName, parameters, Web, administratorUser, wfid,domain,userAD, passwordAD);
                                    }
                                }
                            }

                            break;
                        }

                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetStepResponsible " + ex.Message);
            }

            return userToReturn;
        }

        /// <summary>
        /// Get selected workflow step responsibles during step signing.
        /// </summary>
        /// <param name="DynamicUserListsPanel"></param>
        /// <param name="actorsModified"></param>
        /// <returns>String dicionary with all the step responsibles during step signing.</returns>
        public static Dictionary<string, string> GetStepResponsibles(Panel DynamicUserListsPanel, bool actorsModified, string wfid)
        {
            Dictionary<string, string> responsibles = new Dictionary<string, string>();

            try
            {
                int count = 1;
                int cont = 0;

                foreach (var ctrl in DynamicUserListsPanel.Controls)
                {
                    try
                    {
                        Control control = DynamicUserListsPanel.Controls[cont];

                        if (control is UpdatePanel)
                        {
                            UpdatePanel up = (UpdatePanel)control;

                            foreach (Control webctrl in up.Controls)
                            {
                                DropDownList ddl = (DropDownList)webctrl.Controls[0];

                                if (ddl != null && !string.IsNullOrEmpty(ddl.SelectedValue))
                                    responsibles.Add(count.ToString(), ddl.SelectedValue);
                                else if (ddl != null)
                                    responsibles.Add(count.ToString(), string.Empty);

                                if (actorsModified)
                                {
                                    string stepNumber = GetStepNumber_by_ddlID(ddl.ID);
                                    string actorSelected = string.Empty;

                                    if (!IsActorModified_byStep(stepNumber, wfid, responsibles))
                                        actorSelected = ddl.SelectedValue;
                                    else
                                        actorSelected = GetValueActorModified(stepNumber, wfid, responsibles);
                                }
                            }
                            count++;
                        }
                    }
                    catch (Exception ex)
                    {
                         General.saveErrorsLog(wfid, "GetStepResponsibles (1) " + ex.Message);
                         continue; 
                    }
                    cont++;
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetStepResponsibles (2) " + ex.Message);
            }

            return responsibles;
        }

        public static Dictionary<string, string> GetStepBackupResponsibles(SPListItem item, string wfid, SPWeb web)
        {
            Dictionary<string, string> backupResponsibles = new Dictionary<string, string>();

            try
            {
               if (item["InitialStepBackupGroups"] != null)
               {
                   string[] groups = Regex.Split(item["InitialStepBackupGroups"].ToString(), "&#");
                   string groupReference = string.Empty;

                   foreach (string group in groups)
                   {
                       try
                       {
                           string[] groupRecord = Regex.Split(group, ";#");
                           groupReference = groupRecord[2].Split('\\')[1];
                           backupResponsibles.Add(groupRecord[0], groupReference);
                       }
                       catch (Exception ex)
                       {
                           General.saveErrorsLog(wfid, "GetStepBackupResponsibles() - FOR - Group: '" + groupReference + "'. " + ex.Message);
                           continue;
                       }
                   }
               }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetStepBackupResponsibles() " + ex.Message);
            }

            return backupResponsibles;
        }

        /// <summary>
        /// Get selected actor to send e-mail after step signing.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="Web"></param>
        /// <param name="DynamicUserListsPanel"></param>
        /// <param name="parameters"></param>
        /// <param name="item"></param>
        /// <param name="WFID"></param>
        /// <param name="userAD">Encrypted administrator user login name</param>
        /// <param name="passwordAD">Encrypted administrator user password</param>
        /// <returns>SharePoint SPUser object with the workflow step responsible to send the e-mail</returns>
        public static SPUser GetEmailReceiverUser(string groupName, SPWeb Web, Panel DynamicUserListsPanel, Dictionary<string, string> parameters, SPListItem item, string WFID, string userAD, string passwordAD)
        {
            SPUser user = null;
            try
            {
                int count = 1;
                foreach (Control webctrl in DynamicUserListsPanel.Controls)
                {
                    try
                    {
                        if (webctrl is Label)
                        {
                            Label lbl = (Label)webctrl;
                            string userName = groupName;
                            string userLoginName = groupName;
                            string labelText = lbl.Text;
                            General.GetUserData(ref userLoginName, ref userName);

                            if (labelText.Contains(":"))
                                labelText = lbl.Text.Replace(":", string.Empty);

                            if (parameters[userLoginName].ToString().ToUpper().Trim().Equals(labelText.ToUpper().Trim()))
                            {
                                user = ControlManagement.GetStepResponsible(count, Web, DynamicUserListsPanel, parameters, item, WFID, userAD, passwordAD);
                                break;
                            }

                            count++;
                        }
                    }
                    catch { continue; }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(WFID, "GetEmailReceiverUser() " + ex.Message);
            }
            return user;
        }

      

        #endregion

        #region Steps

        /// <summary>
        /// Get the next workflow step number to achieve.
        /// </summary>
        /// <param name="currentStep"></param>
        /// <param name="Web"></param>
        /// <param name="DynamicUserListsPanel"></param>
        /// <param name="isReassigningOrSaving"></param>
        /// <returns>Number of the next step to achieve.</returns>
        public static int GetNextStep(int currentStep, SPWeb Web, Panel DynamicUserListsPanel, bool isReassigningOrSaving)
        {
            int stepToReturn = 1;
            try
            {
                int count = 1;
                int cont = 0;
                bool isNextStep = false;

                foreach (var ctrl in DynamicUserListsPanel.Controls)
                {
                    try
                    {
                        Control control = DynamicUserListsPanel.Controls[cont];

                        if (control is UpdatePanel)
                        {
                            UpdatePanel up = (UpdatePanel)control;

                            foreach (Control webctrl in up.Controls)
                            {
                                DropDownList ddl = (DropDownList)webctrl.Controls[0];
                                if (ddl != null)
                                {
                                    //If workflow step actor list selected value is not null, its selected value is the next workflow step responsible and step number.
                                    if (!string.IsNullOrEmpty(ddl.SelectedValue) && ((isReassigningOrSaving && count >= currentStep) || count > currentStep))
                                    {
                                        isNextStep = true;
                                        break;
                                    }
                                }
                                count++;
                            }
                        }
                    }

                    catch { continue; }

                    if (isNextStep)
                        break;

                    cont++;
                }
                stepToReturn = count;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetNextStep " + ex.Message);
            }

            return stepToReturn;
        }
        #endregion

        #region InitializeControls

        /// <summary>
        /// Initiate confidential configuration list control. 
        /// </summary>
        /// <param name="ddlConfidential"></param>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <param name="isPostback"></param>
        public static void InitConfidentialDDL(string wfid, ref DropDownList ddlConfidential, SPListItem item, SPWeb Web, Dictionary<string,string> parameters, bool isPostback, string selectedConfidentiality)
        {
            try
            {
                ddlConfidential.AutoPostBack = true;
                ddlConfidential.SelectedIndexChanged += new EventHandler(ddlConfidential_SelectedIndexChanged);
                if (ddlConfidential.Items.Count.Equals(0))
                {
                    ddlConfidential.Items.Add(new ListItem("Non Restricted", "Non Restricted"));
                    ddlConfidential.Items.Add(new ListItem("Restricted", "Restricted"));
                    //COMMENTED TO REMOVE CR19
                    //ddlConfidential.Items.Add(new ListItem("Group Restricted", "Group Restricted"));
                    
                    if (item != null)
                    {
                        string confidential = (item["InitialConfidential"] == null || item["InitialConfidential"].ToString() == "") ? WorkflowDataManagement.GetWorkflowConfidentialValue(item, Web) : item["InitialConfidential"].ToString();
                        ddlConfidential.SelectedIndex = ddlConfidential.Items.IndexOf(new ListItem(confidential, confidential)) >= 0 ? ddlConfidential.Items.IndexOf(new ListItem(confidential, confidential)) : 0;
                    }
                }

                if (!string.IsNullOrEmpty(selectedConfidentiality) && !isPostback)
                    ddlConfidential.SelectedIndex = ddlConfidential.Items.IndexOf(ddlConfidential.Items.FindByValue(selectedConfidentiality));
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "InitConfidentialDDL " + ex.Message);
            }
        }

        // CR 24
        /// <summary>
        /// Store Workflows to Link information in hidden control and session variable
        /// </summary>
        public static void InitLinkToWorkFlow(string wfid, ref HiddenField hddLinkToWorkFlow, SPListItem item, SPWeb Web, Dictionary<string, string> parameters, bool isPostback)
        {
            List<string> wfidsToLink = new List<string>();
            try
            {
                string dataLinkToWorkFlow = string.Empty;

                if (item != null)
                {
                    if (!isPostback)
                    {
                        if (string.IsNullOrEmpty(HttpContext.Current.Session["FormLinkToWorkFlowModified" + wfid].ToString()))
                            dataLinkToWorkFlow = WorkflowDataManagement.GetWorkflowLinktoWorkFlowValue(item, Web, wfid);
                        else
                            dataLinkToWorkFlow = HttpContext.Current.Session["FormLinkToWorkFlowModified" + wfid].ToString();                                              

                        if (!string.IsNullOrEmpty(dataLinkToWorkFlow))
	                    {
		                    string[] data = dataLinkToWorkFlow.Split('|');
                            
                            foreach (string wfiddatabase in data)
                            {
                                string wfidbase = wfiddatabase.Split(':')[0].Trim();                               
                                string wwfid = WorkflowDataManagement.GetWorkflowTypeByWFID(wfidbase, Web);
                                if (!string.IsNullOrEmpty(wwfid)) 
                                    wfidsToLink.Add(wfidbase + ":" + wwfid);
                            }
	                    }

                        hddLinkToWorkFlow.Value = string.Join("|", wfidsToLink.ToArray());
                        HttpContext.Current.Session["FormLinkToWorkFlowModified" + wfid] = dataLinkToWorkFlow;
                    }
                    else {
                        hddLinkToWorkFlow.Value = HttpContext.Current.Session["FormLinkToWorkFlowModified" + wfid].ToString();
                    }                    
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "InitLinkToWorkFlow " + ex.Message);
            }
        }

        // FIN CR 24

        /// <summary>
        /// Initiate the radio buttons to show during rejection.
        /// </summary>
        /// <param name="groupRadioButtons"></param>
        /// <param name="DynamicUserListsPanel"></param>
        /// <param name="DynamicRadioButtonListPanel"></param>
        /// <param name="btnAssign"></param>
        /// <param name="btnAssign2"></param>
        /// <param name="currentStep"></param>
        /// <param name="isConfidential"></param>
        /// <param name="parameters"></param>
        /// <param name="Web"></param>
        /// <param name="item"></param>
        /// <param name="wfid"></param>
        public static void InitRadioButtons(ref RadioButtonList groupRadioButtons, ref Panel DynamicUserListsPanel, ref Panel DynamicRadioButtonListPanel, Button btnAssign, Button btnAssign2, Label lblCommentRequired, int currentStep, bool isConfidential, Dictionary<string, string> parameters, SPWeb Web, SPListItem item, string wfid, string wftypeName, string confidentialValue)
        {
            try
            {
                int stepCount = 1;
                groupRadioButtons.Items.Clear();

                string domain = parameters["Domain"];
                string userAD = General.Decrypt(parameters["AD User"]);
                string passwordAD = General.Decrypt(parameters["AD Password"]);
                SPUser administratorUser = General.GetAdministratorUser(parameters, Web, wfid);
                List<string> groupNames = WorkflowDataManagement.GetGroupNames(item["InitialSteps"]!=null?item["InitialSteps"].ToString():string.Empty, Web, wfid);
                SPUser stepResponsible = WorkflowDataManagement.GetWorkflowCurrentStepResponsible(item, Web, wfid, domain);
                string mandatoryCommentMessage = "It is mandatory to introduce the reason for rejecting the workflow.";

                foreach (Control control in DynamicUserListsPanel.Controls)
                {
                    try
                    {
                       

                        if (control is UpdatePanel)
                        {
                            UpdatePanel up = (UpdatePanel)control;
                            DropDownList ddl = (DropDownList)up.Controls[0].Controls[0];

                            //Show the actors of previous steps
                            if (ddl != null && (stepCount < currentStep))
                            {
                                ListItem radioButton = null;
                                    
                                //No selected actor for previous step
                                if (string.IsNullOrEmpty(ddl.SelectedValue) || (isConfidential && ddl.SelectedItem.Text.ToLower().Contains("(deleted)")))
                                {
                                    string groupName = groupNames[(stepCount - 1)].ToString();
                                    radioButton = new ListItem(General.GetGroupName(groupName, parameters) + ": No Actor");
                                    radioButton.Enabled = false;
                                }
                                //Selected actor not null
                                else
                                {
                                    //Previous step responsible has been deleted^from Active Directory
                                    if (!ddl.SelectedItem.Text.ToLower().Contains("(deleted)"))
                                    {
                                        string groupName = groupNames[(stepCount - 1)].ToString();
                                        radioButton = new ListItem(General.GetGroupName(groupName, parameters) + ": " + ddl.SelectedItem.Text, stepCount.ToString());
                                    }
                                    else
                                    {

                                        string groupName = groupNames[(stepCount - 1)].ToString();
                                        SPUser defaultUser = General.GetDefaultUserToReassign(groupName, parameters, Web, administratorUser, wfid, domain, userAD, passwordAD);
                                        string defaultUserName = defaultUser.Name;
                                        radioButton = new ListItem(General.GetGroupName(groupName, parameters) + ": " + defaultUserName, stepCount.ToString());

                                    }
                                }

                                groupRadioButtons.Items.Add(radioButton);
                            }
                            stepCount++;
                        }
                    }

                    catch { continue; }
                }

                foreach(ListItem radio in groupRadioButtons.Items)
                {
                    if(radio.Enabled)
                        radio.Attributes.Add("onclick", "var radioButtonPanel = document.getElementById('" + DynamicRadioButtonListPanel.ClientID + "');if (radioButtonPanel!=null){var newComment = document.getElementById('NewCommentsArea'); var newTextArea = newComment.getElementsByTagName('textarea'); var comment = newTextArea[0].innerHTML.trim(); document.getElementById('RejectionUserSelected').innerHTML = 'UserSelected';document.getElementById('RejectionUserSelected').style.display = 'none';if(document.getElementById('RejectionUserSelected').innerHTML !== '' && comment !==''){document.getElementById('" + btnAssign.ClientID + "').disabled = false; document.getElementById('" + btnAssign.ClientID + "').className = 'btn_blue'; document.getElementById('" + btnAssign2.ClientID + "').disabled = false; document.getElementById('" + btnAssign2.ClientID + "').className = 'btn_blue'; }else{document.getElementById('" + btnAssign.ClientID + "').disabled = true; document.getElementById('" + btnAssign.ClientID + "').className = 'aspNetDisabled btn_blue'; document.getElementById('" + btnAssign2.ClientID + "').disabled = true; document.getElementById('" + btnAssign2.ClientID + "').className = 'aspNetDisabled btn_blue';}if (comment !==''){document.getElementById('" + lblCommentRequired.ClientID + "').innerHTML = '';}else{document.getElementById('" + lblCommentRequired.ClientID + "').innerHTML = '" + mandatoryCommentMessage + "';}}");
                }

                DynamicRadioButtonListPanel.Visible = true;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "InitRadioButtons " + ex.Message);
            }
        }
        
        /// <summary>
        /// Initiate and document library web parts and process its drawing.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="wfid"></param>
        /// <param name="docType"></param>
        /// <param name="viewShortName"></param>
        /// <param name="libraryPlaceHolder"></param>
        /// <param name="libraryButtonsPlaceHolder"></param>
        /// <param name="listViewWebPart"></param>
        /// <param name="list"></param>
        /// <param name="loggedUser"></param>
        /// <param name="view"></param>
        /// <param name="Web"></param>
        /// <param name="tabButton"></param>
        public static void InitDocumentLibrary(string wfid, string docType, string viewShortName, ref PlaceHolder libraryPlaceHolder, ref PlaceHolder libraryButtonsPlaceHolder, ref ListViewWebPart listViewWebPart, SPList list, SPUser loggedUser, SPView view, SPWeb Web, ref HtmlInputButton tabButton, ref Panel DocumentArea, Dictionary<string, string> parameters, string wftypeOrder, ref string strUrls, ref string strViews, string webURL)
        {
            try
            {
                string folderURL = list.DefaultViewUrl.ToLower();
                int urlIndex = folderURL.ToLower().IndexOf("/forms/");
                string subfolderURL = folderURL.Substring(0, urlIndex) + "/" + wfid + "/" + docType + "/";
                SPFolder folder = Web.GetFolder(subfolderURL);
                string oldDocType = string.Empty;
                string newDocType = string.Empty;

                //CR37 - Move docs between tans -> Documentation Type values updated
                if (!folder.Exists)
                {
                    if ((docType.Equals("ABAC")) || (docType.Equals("To be signed in ABAC")))
                    {
                        oldDocType = "To be signed in ABAC";
                        newDocType = "ABAC";
                    }
                    else if ((docType.Equals("Paper signed docs")) || (docType.Equals("Signed")))
                    {
                        oldDocType = "Signed";
                        newDocType = "Paper signed docs";
                    }


                    subfolderURL = folderURL.Substring(0, urlIndex) + "/" + wfid + "/" + oldDocType + "/";
                    folder = Web.GetFolder(subfolderURL);

                    if (!folder.Exists)
                    {
                        subfolderURL = folderURL.Substring(0, urlIndex) + "/" + wfid + "/" + newDocType + "/";
                        folder = Web.GetFolder(subfolderURL);
                    }
                }


                tabButton.Value = String.Format(tabButton.Value, folder.ItemCount.ToString());

                SPListItemCollection collListItems = WorkflowDataManagement.GetDocumentsFromSpecifFolder(folder, list, wfid);
                WorkflowDataManagement.GetDocumentURLArray(ref strUrls, collListItems, webURL, wfid);
                strViews += "View" + viewShortName + ";";

                listViewWebPart = new ListViewWebPart();
                listViewWebPart.ListName = list.ID.ToString("B").ToUpperInvariant();
                listViewWebPart.TitleUrl = view.Url;
                listViewWebPart.WebId = list.ParentWeb.ID;
                listViewWebPart.ViewGuid = view.ID.ToString("B").ToUpperInvariant();

                SetPrivateFieldValue(ref listViewWebPart, "rootFolder", folder.Url);

                foreach (Control ctrl in listViewWebPart.Controls)
                {
                    if (ctrl is ViewToolBar)
                    {
                        ViewToolBar vtb = (ViewToolBar)ctrl;
                        ctrl.Visible = false;
                        break;
                    }
                }

                RenderDocumentLibraryAsHTML(ref libraryPlaceHolder, listViewWebPart, view.ID.ToString(), viewShortName, loggedUser, Web);
                InitDocumentLibraryCustomButtons(ref libraryButtonsPlaceHolder, ref DocumentArea, folder, Web, list, parameters, wfid, wftypeOrder, docType);
            }
            catch (Exception ex)
            {
                DocumentArea.Visible = false;
                tabButton.Visible = false;
                General.saveErrorsLog(wfid, "InitDocumentLibrary() - Folder: '" + docType + "'." + ex.Message);
            }
        }

        /// <summary>
        /// Render document library web part as HTML
        /// </summary>
        /// <param name="libraryPlaceHolder"></param>
        /// <param name="listViewWebPart"></param>
        /// <param name="viewID"></param>
        /// <param name="viewShortName"></param>
        /// <param name="Web"></param>
        private static void RenderDocumentLibraryAsHTML(ref PlaceHolder libraryPlaceHolder, ListViewWebPart listViewWebPart, string viewID, string viewShortName, SPUser loggedUser, SPWeb Web)
        {
            try
            {
                Table table = new Table();
                TableRow row = new TableRow();
                table.ID = viewID;

                libraryPlaceHolder.Controls.Clear();
                libraryPlaceHolder.Controls.Add(table);

                TableCell cell = new TableCell();
                cell.Text = listViewWebPart.GetDesignTimeHtml();

                string[] stringSeparators = new string[] { "<td class=\"ms-vb-icon\">" };
                var result = cell.Text.Split(stringSeparators, StringSplitOptions.None);

                for (int i = 1; i < result.Length; i++)
                {
                    if (result[i].Contains("icjpg.gif") || result[i].Contains("icjpeg.gif") || result[i].Contains("icgif.gif") || result[i].Contains("icbmp.gif") || result[i].Contains("icpng.gif") || result[i].Contains("ictif.gif"))
                    {
                        result[i] = result[i].Replace("return DispEx(", "return DispExNoCall(");
                        result[i] = result[i].Replace("<a onfocus=\"OnLink(this)\"", "<a target =\"_blank\"");
                        result[i] = Regex.Replace(result[i], "^<a href=", "<a target=\"_blank\" href=");
                    }
                }

                for (int i = 0; i < result.Length; i++)
                {
                    if (i == 0)
                        cell.Text = result[i];
                    else
                        cell.Text = cell.Text + "<td class=\"ms-vb-icon\">" + result[i];
                }

                row.Cells.Add(cell);
                table.Rows.Add(row);


            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "RenderDocumentLibraryAsHTML " + ex.Message);
            }
        }


        /// <summary>
        /// Initiate document library web part buttons.
        /// </summary>
        /// <param name="libraryButtonsPlaceHolder"></param>
        /// <param name="folder"></param>
        /// <param name="Web"></param>
        /// <param name="list"></param>
        private static void InitDocumentLibraryCustomButtons(ref PlaceHolder libraryButtonsPlaceHolder, ref Panel DocumentArea, SPFolder folder, SPWeb Web, SPList list, Dictionary<string, string> parameters, string wfid, string wftypeOrder, string docType)
        {
            try
            {
                Button btnNewDoc = new Button();
                btnNewDoc.Text = "New Document";
                btnNewDoc.CssClass = "btn_grey";
                btnNewDoc.ToolTip = "Upload new workflow document.";

                string escapedRootFolder = string.Empty;
                if(!Web.ServerRelativeUrl.Equals("/"))
                    escapedRootFolder = Uri.EscapeDataString(Web.ServerRelativeUrl + "/" + folder.Url);
                else
                    escapedRootFolder = Uri.EscapeDataString("/" + folder.Url);

                string wfDocContentTypeID = "0x010000bbe2cb30b8ae48f8a39bd6d1f94b8df0";
                btnNewDoc.OnClientClick = "javascript:NewItem2(event, \"" + Web.Url + "/_layouts/15/Upload.aspx?List={" + list.ID.ToString().ToUpper() + "}&RootFolder=" + escapedRootFolder + "&ContentTypeId=" + wfDocContentTypeID.ToUpper() + "\");javascript:return false;";

                Button btnUploadMultipleDocs = new Button();
                btnUploadMultipleDocs.Text = "Multiple Uploading";
                btnUploadMultipleDocs.CssClass = "btn_grey";
                btnUploadMultipleDocs.OnClientClick = "javascript:OpenPopUpPageWithTitle(\"" + Web.Url + parameters["Upload Multiple Documents Page"] + "?wfid=" + wfid + "&wftype=" + wftypeOrder + "&wfdoctype=" + docType + "\",RefreshOnDialogClose, 800, 580,'Upload Multiple Documents');javascript:return false;";
                btnUploadMultipleDocs.ToolTip = "Upload multiple workflow documents.";

                libraryButtonsPlaceHolder.Controls.Clear();
                libraryButtonsPlaceHolder.Controls.Add(btnNewDoc);
                libraryButtonsPlaceHolder.Controls.Add(btnUploadMultipleDocs);
            }
            catch 
            {
                DocumentArea.Visible = false;
            }
        }

        /// <summary>
        /// Set document library web part to show custom folder as root folder
        /// </summary>
        /// <param name="listViewWebPart"></param>
        /// <param name="fieldName"></param>
        /// <param name="val"></param>
        private static void SetPrivateFieldValue(ref ListViewWebPart listViewWebPart, string fieldName, string val)
        {
            FieldInfo fi = listViewWebPart.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            fi.SetValue(listViewWebPart, val);
        }

        #endregion

        #region DisableEnableInterface

        /// <summary>
        /// Enable and disable all interface controls except actor selection controls.
        /// </summary>
        /// <param name="btnSign"></param>
        /// <param name="btnSign2"></param>
        /// <param name="signText"></param>
        /// <param name="visibleSign"></param>
        /// <param name="btnSave"></param>
        /// <param name="btnSave2"></param>
        /// <param name="visibleSave"></param>
        /// <param name="btnCancel"></param>
        /// <param name="btnCancel2"></param>
        /// <param name="visibleCancel"></param>
        /// <param name="btnDelete"></param>
        /// <param name="btnDelete2"></param>
        /// <param name="visibleDelete"></param>
        /// <param name="btnClose"></param>
        /// <param name="btnClose2"></param>
        /// <param name="visibleClose"></param>
        /// <param name="HyperLinkPrint"></param>
        /// <param name="visiblePrint"></param>
        /// <param name="btnAssign"></param>
        /// <param name="btnAssign2"></param>
        /// <param name="visibleAssign"></param>
        /// <param name="enableAssign"></param>
        /// <param name="btnReject"></param>
        /// <param name="btnReject2"></param>
        /// <param name="visibleReject"></param>
        /// <param name="ddlConfidential"></param>
        /// <param name="visibleAndEnabledConfidential"></param>
        /// <param name="groupRadioButtons"></param>
        /// <param name="visibleRadios"></param>
        /// <param name="prevComments"></param>
        /// <param name="generalFields"></param>
        /// <param name="enableRestOfControls"></param>
        /// <param name="DocsMainButtons"></param>
        /// <param name="DocsAbacButtons"></param>
        /// <param name="DocsSupportingButtons"></param>
        /// <param name="DocsPaperButtons"></param>
        /// <param name="DocsSignedButtons"></param>
        /// <param name="libraryButtonsVisible"></param>
        /// <param name="wfid"></param>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <param name="wftypeName"></param>
        public static void EnableDisableControls(ref Button btnSign, ref Button btnSign2, string signText, bool visibleSign, ref Button btnSave, ref Button btnSave2, bool visibleSave, ref Button btnOnHold, ref Button btnOnHold2, bool visibleOnHold, ref Button btnCancel, ref Button btnCancel2, bool visibleCancel, ref Button btnDelete, ref Button btnDelete2, bool visibleDelete, ref Button btnClose, ref Button btnClose2, bool visibleClose, ref HyperLink HyperLinkPrint, bool visiblePrint, ref Button btnAssign, ref Button btnAssign2, bool visibleAssign, bool enableAssign, ref Button btnReject, ref Button btnReject2, bool visibleReject, ref DropDownList ddlConfidential, bool visibleAndEnabledConfidential, ref RadioButtonList groupRadioButtons, bool visibleRadios, ref PlaceHolder prevComments, ref PlaceHolder newComments, ref Label lblCommentRequired, bool enableNewComments, ref PlaceHolder generalFields, bool enableRestOfControls, ref PlaceHolder DocsMainButtons, ref PlaceHolder DocsAbacButtons, ref PlaceHolder DocsSupportingButtons, ref PlaceHolder DocsPaperButtons, ref PlaceHolder DocsSignedButtons, bool libraryButtonsVisible, string wfid, string wftypeOrder, SPListItem item, SPWeb Web, Dictionary<string,string> parameters, string wftypeName, ref TextBox WFID_Textbox, ref Button WFID_ButtonAdd, Boolean enabledLinkToWF)
        {
            try
            {
                btnSign.Text = signText;
                btnSign2.Text = signText;
                btnSign.Visible = visibleSign;
                btnSign2.Visible = visibleSign;
                btnSave.Visible = visibleSave;
                btnSave2.Visible = visibleSave;
                btnOnHold.Visible = visibleOnHold;
                btnOnHold2.Visible = visibleOnHold;
                btnCancel.Visible = visibleCancel;
                btnCancel2.Visible = visibleCancel;
                btnDelete.Visible = visibleDelete;
                btnDelete2.Visible = visibleDelete;
                btnClose.Visible = visibleClose;
                btnClose2.Visible = visibleClose;
                btnAssign.Visible = visibleAssign;
                btnAssign.Enabled = enableAssign;
                btnAssign2.Visible = visibleAssign;
                btnAssign2.Enabled = enableAssign;
                btnReject.Visible = visibleReject;
                btnReject2.Visible = visibleReject;

                string wfConfidentialEnabled = (item["InitialConfidential"] == null) ? String.Empty : item["InitialConfidential"].ToString();
                ddlConfidential.Enabled = (String.IsNullOrEmpty(wfConfidentialEnabled)) ? visibleAndEnabledConfidential : false;

                // CR 24 enable textbox and Button add for Link to workflows
                WFID_Textbox.Enabled = enabledLinkToWF;
                WFID_ButtonAdd.Enabled = enabledLinkToWF;
                // FIN de CR 24

                groupRadioButtons.Visible = visibleRadios;

                EnableDisableGeneralFields(ref generalFields, enableRestOfControls);
                EnableDisableComments(ref prevComments, ref lblCommentRequired, enableRestOfControls, false);
                EnableDisableComments(ref newComments, ref lblCommentRequired, enableNewComments, visibleAssign);
                EnableDisableDocLibrariesButtons(ref DocsMainButtons, libraryButtonsVisible);
                EnableDisableDocLibrariesButtons(ref DocsAbacButtons, libraryButtonsVisible);
                EnableDisableDocLibrariesButtons(ref DocsSupportingButtons, libraryButtonsVisible);
                EnableDisableDocLibrariesButtons(ref DocsPaperButtons, libraryButtonsVisible);
                EnableDisableDocLibrariesButtons(ref DocsSignedButtons, libraryButtonsVisible);
                EnableDisablePrint(visiblePrint, wfid, wftypeOrder, wftypeName, item, Web, ref  HyperLinkPrint);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "EnableDisableControls() - " + ex.Message);
            }
        }

        /// <summary>
        /// Shows print button if printed document exists, otherwise hides print button.
        /// </summary>
        /// <param name="visiblePrint"></param>
        /// <param name="wfid"></param>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <param name="HyperLinkPrint"></param>
        private static void EnableDisablePrint(bool visiblePrint, string wfid, string wftypeOrder, string wftypeName, SPListItem item, SPWeb Web, ref HyperLink HyperLinkPrint)
        {
            try
            {
                if (visiblePrint)
                {

                    string urlPrintedFile = WorkflowDataManagement.GetURLPrintedDocument(item.ParentList, wfid, wftypeName, Web, wftypeOrder);

                    if (WorkflowDataManagement.ExistPrintDocument(urlPrintedFile, Web, wfid))
                        visiblePrint = true;
                    else
                        visiblePrint = false;
                }

                HyperLinkPrint.Visible = visiblePrint;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "EnableDisablePrint() - " + ex.Message);
            }
        }

        /// <summary>
        /// Enable and disable document management buttons
        /// </summary>
        /// <param name="DocsPlaceHolderButtons"></param>
        /// <param name="libraryButtonsVisible"></param>
        public static void EnableDisableDocLibrariesButtons(ref PlaceHolder DocsPlaceHolderButtons, bool libraryButtonsVisible)
        {
            try
            {
                foreach (Control ctrl in DocsPlaceHolderButtons.Controls)
                {
                    try
                    {
                        if (ctrl is Button)
                        {
                            Button btn = (Button)ctrl;
                            if (!btn.Text.ToUpper().Contains("OPEN"))
                                btn.Visible = libraryButtonsVisible;
                        }
                    }
                    catch { continue; }
                }
            }
            catch { }
        }

        /// <summary>
        /// Disable previous comment area
        /// </summary>
        /// <param name="comments"></param>
        /// <param name="enableRestOfControls"></param>
        public static void EnableDisableComments(ref PlaceHolder comments, ref Label lblCommentRequired, bool enableRestOfControls, bool isRejection)
        {
            try
            {
                foreach (Control ctrl in comments.Controls)
                {
                    try
                    {
                        if (ctrl is Label)
                        {
                            Label lbl = (Label)ctrl;
                            lbl.Enabled = enableRestOfControls;
                        }
                        else
                        {
                            try
                            {
                                if (ctrl.Controls[0].Controls[0] is TextBox)
                                {
                                    TextBox txt = (TextBox)ctrl.Controls[0].Controls[0];
                                    txt.Enabled = enableRestOfControls;

                                    if (isRejection && txt.Text.Trim().Equals(string.Empty))
                                        lblCommentRequired.Text = "It is mandatory to introduce the reason for rejecting the workflow";
                                    else
                                        lblCommentRequired.Text = string.Empty;
                                }
                            }
                            catch { }
                        }
                    }
                    catch { continue; }
                }
            }
            catch { }
        }

        /// <summary>
        /// Enable and disable general field controls.
        /// </summary>
        /// <param name="generalFields"></param>
        /// <param name="enableRestOfControls"></param>
        public static void EnableDisableGeneralFields(ref PlaceHolder generalFields, bool enableRestOfControls)
        {
            try
            {
                foreach (Control ctrl in generalFields.Controls)
                {
                    try
                    {
                        if (ctrl is UpdatePanel)
                        {
                            UpdatePanel up = (UpdatePanel)ctrl;

                            foreach (Control ctrl2 in up.Controls)
                            {
                                if (ctrl2.Controls[1] is TextBox)
                                {
                                    TextBox tb = (TextBox)ctrl2.Controls[1];
                                    tb.Enabled = enableRestOfControls;
                                }
                                else if (ctrl2.Controls[1] is CheckBox)
                                {
                                    CheckBox cb = (CheckBox)ctrl2.Controls[1];
                                    cb.Enabled = enableRestOfControls;
                                }
                                else if (ctrl2.Controls[1] is RadioButtonList)
                                {
                                    RadioButtonList rbl = (RadioButtonList)ctrl2.Controls[1];
                                    rbl.Enabled = enableRestOfControls;
                                }
                                else if (ctrl2.Controls[1] is PeopleEditor)
                                {
                                    PeopleEditor pe = (PeopleEditor)ctrl2.Controls[1];
                                    
                                    //if (parameters.ContainsKey("GeneralColumn_5") && GeneralFields.FormatColumnName(parameters["GeneralColumn_5"]).ToUpper().Equals(pe.ID.ToUpper()) )
                                    //{
                                    //    if(pe.Enabled)
                                    //        HttpContext.Current.Session["WFConfidentialPeople"] = pe.ResolvedEntities.ToString();

                                    //    pe.Enabled = isInitiator;
                                    //    //pe.Enabled = isInitiator;
                                    //    //pe.Visible = isGroupConfidential;
                                    //}
                                    //else
                                        pe.Enabled = enableRestOfControls;
                                        //pe.Enabled = enableRestOfControls;
                                }
                                else if (ctrl2.Controls[1] is DateTimeControl)
                                {
                                    DateTimeControl dtc = (DateTimeControl)ctrl2.Controls[1];

                                    //if (parameters.ContainsKey("GeneralDeadline") && parameters.ContainsKey("GeneralAmount") && (parameters["GeneralDeadline"].ToUpper().Equals(dtc.ID.ToUpper()) || parameters["GeneralAmount"].ToUpper().Equals(dtc.ID.ToUpper())))
                                    //{
                                    //    dtc.Enabled = isInitiator;
                                    //    //dtc.Visible = isGroupConfidential;
                                    //}
                                    //else
                                        dtc.Enabled = enableRestOfControls;
                                }
                                else if (ctrl2.Controls[1] is DropDownList)
                                {
                                    DropDownList ddl = (DropDownList)ctrl2.Controls[1];
                                    ddl.Enabled = enableRestOfControls;
                                }

                                if (ctrl2.Controls[0] is Label)
                                {
                                    Label label = (Label)ctrl2.Controls[0];
                                    string formatedID = label.ID.Replace("FieldLabelID_", string.Empty);

                                    //if (parameters.ContainsKey("GeneralDeadline") && parameters.ContainsKey("GeneralAmount") && parameters.ContainsKey("GeneralColumn_5") && (parameters["GeneralDeadline"].ToUpper().Equals(formatedID.ToUpper()) || parameters["GeneralAmount"].ToUpper().Equals(formatedID.ToUpper()) || parameters["GeneralColumn_5"].ToUpper().Equals(formatedID.ToUpper())))
                                    //    label.Visible = isGroupConfidential;
                                }
                            }

                            //up.UpdateMode = UpdatePanelUpdateMode.Conditional;
                            //up.Update();
                        }
                    }
                    catch { continue; }
                }
            }
            catch { }
        }

        /// <summary>
        /// Manage interface controls according to workflow status and logged user responsibility
        /// </summary>
        /// <param name="status"></param>
        /// <param name="currentStep"></param>
        /// <param name="DynamicUserListsPanel"></param>
        /// <param name="DynamicRadioButtonListPanel"></param>
        /// <param name="btnSign"></param>
        /// <param name="btnSign2"></param>
        /// <param name="btnSave"></param>
        /// <param name="btnSave2"></param>
        /// <param name="btnCancel"></param>
        /// <param name="btnCancel2"></param>
        /// <param name="btnDelete"></param>
        /// <param name="btnDelete2"></param>
        /// <param name="btnClose"></param>
        /// <param name="btnClose2"></param>
        /// <param name="HyperLinkPrint"></param>
        /// <param name="btnAssign"></param>
        /// <param name="btnAssign2"></param>
        /// <param name="btnReject"></param>
        /// <param name="btnReject2"></param>
        /// <param name="ddlConfidential"></param>
        /// <param name="groupRadioButtons"></param>
        /// <param name="loggedUser"></param>
        /// <param name="initiator"></param>
        /// <param name="parameters"></param>
        /// <param name="itemExists"></param>
        /// <param name="rejecting"></param>
        /// <param name="prevComments"></param>
        /// <param name="generalFields"></param>
        /// <param name="DocsMainButtons"></param>
        /// <param name="DocsAbacButtons"></param>
        /// <param name="DocsSupportingButtons"></param>
        /// <param name="DocsPaperButtons"></param>
        /// <param name="DocsSignedButtons"></param>
        /// <param name="Web"></param>
        /// <param name="item"></param>
        /// <param name="wfid"></param>
        /// <param name="wftypeName"></param>
        public static void EnableDisableUserInterface(string status, int currentStep, ref Panel DynamicUserListsPanel, ref Panel DynamicRadioButtonListPanel, ref Button btnSign, ref Button btnSign2, ref Button btnSave, ref Button btnSave2, ref Button btnOnHold, ref Button btnOnHold2, ref Button btnCancel, ref Button btnCancel2, ref Button btnDelete, ref Button btnDelete2, ref Button btnClose, ref Button btnClose2, ref HyperLink HyperLinkPrint, ref Button btnAssign, ref Button btnAssign2, ref Button btnReject, ref Button btnReject2, ref DropDownList ddlConfidential, ref RadioButtonList groupRadioButtons, SPUser loggedUser, SPUser initiator, Dictionary<string, string> parameters, bool itemExists, bool rejecting, ref PlaceHolder prevComments, ref PlaceHolder PlaceHolder_NewComments, ref Label lblCommentRequired, ref PlaceHolder generalFields, ref PlaceHolder DocsMainButtons, ref PlaceHolder DocsAbacButtons, ref PlaceHolder DocsSupportingButtons, ref PlaceHolder DocsPaperButtons, ref PlaceHolder DocsSignedButtons, ref Label lblDocumentsCheckedOutWarning, ref Panel PanelCheckedOutWarning, SPWeb Web, SPListItem item, string wfid, string wftypeOrder, string wftypeName, ref TextBox WFID_Textbox, ref Button WFID_buttonAdd, Dictionary<string, string> actorsBackupDictionary, string domainName,string userAD, string passwordAD)
        {
            try
            {
                bool isBackupInitiator = WorkflowDataManagement.IsMemberOfBackupInitiatorGroup(wfid, loggedUser, domainName, actorsBackupDictionary, userAD, passwordAD, currentStep.ToString(), parameters);
                bool isBackupResponsible = WorkflowDataManagement.IsMemberOfBackupResponsibleGroup(wfid, loggedUser, domainName, actorsBackupDictionary, userAD, passwordAD, currentStep.ToString(), parameters);

                
                #region <Rejection>

                if (rejecting)
                {
                    //Disable all controls and show a radio button list with workflow previous actors
                    InitRadioButtons(ref groupRadioButtons, ref DynamicUserListsPanel, ref DynamicRadioButtonListPanel, btnAssign, btnAssign2, lblCommentRequired, currentStep, !ddlConfidential.SelectedValue.ToUpper().Equals("NON RESTRICTED"), parameters, Web, item, wfid, wftypeName,ddlConfidential.Text);
                    EnableDisableControls(ref btnSign, ref btnSign2, "Sign", false, ref btnSave, ref btnSave2, false, ref btnOnHold, ref btnOnHold2, false, ref btnCancel, ref btnCancel2, true, ref btnDelete, ref btnDelete2, false, ref btnClose, ref btnClose2, false, ref HyperLinkPrint, false, ref btnAssign, ref btnAssign2, true, false, ref btnReject, ref btnReject2, false, ref ddlConfidential, false, ref groupRadioButtons, true, ref prevComments, ref PlaceHolder_NewComments, ref lblCommentRequired, true, ref generalFields, false, ref DocsMainButtons, ref DocsAbacButtons, ref DocsSupportingButtons, ref DocsPaperButtons, ref DocsSignedButtons, false, wfid, wftypeOrder, item, Web, parameters, wftypeName, ref  WFID_Textbox, ref WFID_buttonAdd, false);
                    EnableDisableIfCheckedOut(item, wfid, Web, ref btnSign, ref btnSign2, ref btnAssign, ref btnAssign2, ref btnReject, ref btnReject2, ref DynamicUserListsPanel, ref lblDocumentsCheckedOutWarning, ref PanelCheckedOutWarning, ref groupRadioButtons, true, false);
                }

                #endregion

                #region <Not Rejection>
                else
                {
                    if (parameters.ContainsKey("Status Closed") && parameters.ContainsKey("Status Deleted") && parameters.ContainsKey("Status Draft") && parameters.ContainsKey("Status In Progress") && parameters.ContainsKey("Status Rejected") && parameters.ContainsKey("Status On Hold"))
                    {
                        #region <ClosedOrDeleted>
                        //All controls disabled except confidentiality controls for initiator users
                        if (status.ToUpper().Equals(parameters["Status Closed"].ToUpper()) || status.ToUpper().Equals(parameters["Status Deleted"].ToUpper()))
                        {
                            DynamicUserListsPanel.Enabled = false;

                            if((loggedUser.ID.Equals(initiator.ID)) || isBackupInitiator.Equals(true))
                                EnableDisableControls(ref btnSign, ref btnSign2, "Sign", false, ref btnSave, ref btnSave2, true, ref btnOnHold, ref btnOnHold2, false, ref btnCancel, ref btnCancel2, false, ref btnDelete, ref btnDelete2, false, ref btnClose, ref btnClose2, true, ref HyperLinkPrint, true, ref btnAssign, ref btnAssign2, false, false, ref btnReject, ref btnReject2, false, ref ddlConfidential, true, ref groupRadioButtons, false, ref prevComments, ref PlaceHolder_NewComments, ref lblCommentRequired, false, ref generalFields, false, ref DocsMainButtons, ref DocsAbacButtons, ref DocsSupportingButtons, ref DocsPaperButtons, ref DocsSignedButtons, false, wfid, wftypeOrder, item, Web, parameters, wftypeName, ref  WFID_Textbox, ref WFID_buttonAdd, false);
                            else
                                EnableDisableControls(ref btnSign, ref btnSign2, "Sign", false, ref btnSave, ref btnSave2, false, ref btnOnHold, ref btnOnHold2, false, ref btnCancel, ref btnCancel2, false, ref btnDelete, ref btnDelete2, false, ref btnClose, ref btnClose2, true, ref HyperLinkPrint, true, ref btnAssign, ref btnAssign2, false, false, ref btnReject, ref btnReject2, false, ref ddlConfidential, false, ref groupRadioButtons, false, ref prevComments, ref PlaceHolder_NewComments, ref lblCommentRequired, false, ref generalFields, false, ref DocsMainButtons, ref DocsAbacButtons, ref DocsSupportingButtons, ref DocsPaperButtons, ref DocsSignedButtons, false, wfid, wftypeOrder, item, Web, parameters, wftypeName, ref  WFID_Textbox, ref WFID_buttonAdd, false);
                        }
                        #endregion

                        #region <Draft>
                        else if (status.ToUpper().Equals(parameters["Status Draft"].ToUpper()))
                        {
                            //Enable all controls
                            EnableDisableUIDraft(currentStep, loggedUser, itemExists, ref DynamicUserListsPanel, ref btnSign, ref btnSign2, ref btnSave, ref btnSave2,ref btnOnHold, ref btnOnHold2, ref btnCancel, ref btnCancel2, ref btnDelete, ref btnDelete2, ref btnClose, ref btnClose2, ref HyperLinkPrint, ref btnAssign, ref btnAssign2, ref btnReject, ref btnReject2, ref ddlConfidential, ref groupRadioButtons, ref prevComments, ref PlaceHolder_NewComments, ref lblCommentRequired, ref generalFields, ref DocsMainButtons, ref DocsAbacButtons, ref DocsSupportingButtons, ref DocsPaperButtons, ref DocsSignedButtons, wfid, wftypeOrder, item, Web, parameters, wftypeName,  ref  WFID_Textbox, ref WFID_buttonAdd, isBackupInitiator);
                            EnableDisableIfCheckedOut(item, wfid, Web, ref btnSign, ref btnSign2, ref btnAssign, ref btnAssign2, ref btnReject, ref btnReject2, ref DynamicUserListsPanel, ref lblDocumentsCheckedOutWarning, ref PanelCheckedOutWarning, ref groupRadioButtons, false, true);
                        }
                        #endregion

                        #region <InProgress, Rejected, On Hold>

                        else if ((status.ToUpper().Equals(parameters["Status In Progress"].ToUpper())) || (status.ToUpper().Equals(parameters["Status Rejected"].ToUpper())) || (status.ToUpper().Equals(parameters["Status On Hold"].ToUpper())))
                        {
                            //Enable controls according to logged user profile. Disable all workflow actors for confidential workflows.
                            if (ddlConfidential.SelectedValue.ToUpper().Equals("NON RESTRICTED"))
                                EnableDisableUIInProgressNonConfidential(currentStep, loggedUser, initiator, parameters, itemExists, ref DynamicUserListsPanel, ref btnSign, ref btnSign2, ref btnSave, ref btnSave2, ref btnOnHold, ref btnOnHold2 , ref btnCancel, ref btnCancel2, ref btnDelete, ref btnDelete2, ref btnClose, ref btnClose2, ref HyperLinkPrint, ref btnAssign, ref btnAssign2, ref btnReject, ref btnReject2, ref ddlConfidential, ref groupRadioButtons, ref prevComments, ref PlaceHolder_NewComments, ref lblCommentRequired, ref generalFields, ref DocsMainButtons, ref DocsAbacButtons, ref DocsSupportingButtons, ref DocsPaperButtons, ref DocsSignedButtons, wfid, wftypeOrder, item, Web, wftypeName,  ref  WFID_Textbox, ref WFID_buttonAdd, status, false,isBackupInitiator,isBackupResponsible, userAD, passwordAD, domainName);
                            else if (ddlConfidential.SelectedValue.ToUpper().Equals("RESTRICTED"))
                                EnableDisableUIInProgressConfidential(currentStep, loggedUser, initiator, parameters, itemExists, ref DynamicUserListsPanel, ref btnSign, ref btnSign2, ref btnSave, ref btnSave2, ref btnOnHold, ref btnOnHold2, ref btnCancel, ref btnCancel2, ref btnDelete, ref btnDelete2, ref btnClose, ref btnClose2, ref HyperLinkPrint, ref btnAssign, ref btnAssign2, ref btnReject, ref btnReject2, ref ddlConfidential, ref groupRadioButtons, ref prevComments, ref PlaceHolder_NewComments, ref lblCommentRequired, ref generalFields, ref DocsMainButtons, ref DocsAbacButtons, ref DocsSupportingButtons, ref DocsPaperButtons, ref DocsSignedButtons, wfid, wftypeOrder, item, Web, wftypeName, ref  WFID_Textbox, ref WFID_buttonAdd, status, true, isBackupInitiator, isBackupResponsible);
                           
                            EnableDisableIfCheckedOut(item, wfid, Web, ref btnSign, ref btnSign2, ref btnAssign, ref btnAssign2, ref btnReject, ref btnReject2, ref DynamicUserListsPanel, ref lblDocumentsCheckedOutWarning, ref PanelCheckedOutWarning, ref groupRadioButtons, false, false);
                        }
                        #endregion
                        
                        #region <DisableAll>
                        else
                        {
                            //Disable all controls.
                            DynamicUserListsPanel.Enabled = false;
                            EnableDisableControls(ref btnSign, ref btnSign2, "Sign", false, ref btnSave, ref btnSave2, false, ref btnOnHold, ref btnOnHold2, false , ref btnCancel, ref btnCancel2, false, ref btnDelete, ref btnDelete2, false, ref btnClose, ref btnClose2, true, ref HyperLinkPrint, true, ref btnAssign, ref btnAssign2, false, false, ref btnReject, ref btnReject2, false, ref ddlConfidential, false, ref groupRadioButtons, false, ref prevComments, ref PlaceHolder_NewComments, ref lblCommentRequired, false, ref generalFields, false, ref DocsMainButtons, ref DocsAbacButtons, ref DocsSupportingButtons, ref DocsPaperButtons, ref DocsSignedButtons, false, wfid, wftypeOrder, item, Web, parameters, wftypeName, ref  WFID_Textbox, ref WFID_buttonAdd, false);
                        }
                        #endregion
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "EnableDisableUserInterface " + ex.Message);
            }
        }

        /// <summary>
        /// Enable all user interface controls.
        private static void EnableDisableUIDraft(int currentStep, SPUser loggedUser, bool itemExists, ref Panel DynamicUserListsPanel, ref Button btnSign, ref Button btnSign2, ref Button btnSave, ref Button btnSave2, ref Button btnOnHold, ref Button btnOnHold2, ref Button btnCancel, ref Button btnCancel2, ref Button btnDelete, ref Button btnDelete2, ref Button btnClose, ref Button btnClose2, ref HyperLink btnPrint, ref Button btnAssign, ref Button btnAssign2, ref Button btnReject, ref Button btnReject2, ref DropDownList ddlConfidential, ref RadioButtonList groupRadioButtons, ref PlaceHolder prevComments, ref PlaceHolder PlaceHolder_NewComments, ref Label lblCommentRequired, ref PlaceHolder generalFields, ref PlaceHolder DocsMainButtons, ref PlaceHolder DocsAbacButtons, ref PlaceHolder DocsSupportingButtons, ref PlaceHolder DocsPaperButtons, ref PlaceHolder DocsSignedButtons, string wfid, string wftypeOrder, SPListItem item, SPWeb Web, Dictionary<string, string> parameters, string wftypeName, ref TextBox WFID_Textbox, ref Button WFID_buttonAdd, bool isBackupInitiator)
        {
            bool isWorkflowInitiator = false;
            string userLoginName = loggedUser.LoginName;

            try
            {
                if (currentStep.Equals(1))
                {
                    string userName = loggedUser.Name;
                    General.GetUserData(ref userLoginName, ref userName);


                    int count = 1;

                    foreach (Control control in DynamicUserListsPanel.Controls)
                    {
                        try
                        {
                            if (control is UpdatePanel)
                            {
                                UpdatePanel actorUpdatePanel = (UpdatePanel)control;
                                DropDownList ddl = (DropDownList)actorUpdatePanel.Controls[0].Controls[0];
                            
                                if (ddl != null)
                                {
                                    if (count.Equals(1))
                                    {
                                        ddl.Enabled = false;

                                        if (ddl.SelectedValue.ToLower().Equals(userLoginName.ToLower()))
                                            isWorkflowInitiator = true;
                                    }
                                    else if ((isWorkflowInitiator.Equals(true)) || isBackupInitiator.Equals(true))
                                        ddl.Enabled = true;
                                    else
                                        ddl.Enabled = false;
                                }

                                count++;
                                
                            }
                        }
                        catch (Exception ex)
                        {
                            General.saveErrorsLog(wfid, "EnableDisableUIDraft - Cont: '" + count + "' - " + ex.Message);
                            continue;
                        }
                    }


                    if (isWorkflowInitiator || isBackupInitiator)
                        EnableDisableControls(ref btnSign, ref btnSign2, "Launch", true, ref btnSave, ref btnSave2, true, ref btnOnHold, ref btnOnHold2, false, ref btnCancel, ref btnCancel2, true, ref btnDelete, ref btnDelete2, true, ref btnClose, ref btnClose2, false, ref btnPrint, false, ref btnAssign, ref btnAssign2, false, false, ref btnReject, ref btnReject2, false, ref ddlConfidential, true, ref groupRadioButtons, false, ref prevComments, ref PlaceHolder_NewComments, ref lblCommentRequired, true, ref generalFields, true, ref DocsMainButtons, ref DocsAbacButtons, ref DocsSupportingButtons, ref DocsPaperButtons, ref DocsSignedButtons, true, wfid, wftypeOrder, item, Web, parameters, wftypeName, ref  WFID_Textbox, ref WFID_buttonAdd, true);
                    else
                        EnableDisableControls(ref btnSign, ref btnSign2, "Sign", false, ref btnSave, ref btnSave2, false, ref btnOnHold, ref btnOnHold2, false, ref btnCancel, ref btnCancel2, false, ref btnDelete, ref btnDelete2, false, ref btnClose, ref btnClose2, true, ref btnPrint, false, ref btnAssign, ref btnAssign2, false, false, ref btnReject, ref btnReject2, false, ref ddlConfidential, false, ref groupRadioButtons, false, ref prevComments, ref PlaceHolder_NewComments, ref lblCommentRequired, false, ref generalFields, false, ref DocsMainButtons, ref DocsAbacButtons, ref DocsSupportingButtons, ref DocsPaperButtons, ref DocsSignedButtons, false, wfid, wftypeOrder, item, Web, parameters, wftypeName, ref  WFID_Textbox, ref WFID_buttonAdd, false);
                }
            }
            catch (Exception ex)
            {
                EnableDisableControls(ref btnSign, ref btnSign2, "Sign", false, ref btnSave, ref btnSave2, false, ref btnOnHold, ref btnOnHold2, false, ref btnCancel, ref btnCancel2, false, ref btnDelete, ref btnDelete2, false, ref btnClose, ref btnClose2, true, ref btnPrint, true, ref btnAssign, ref btnAssign2, false, false, ref btnReject, ref btnReject2, false, ref ddlConfidential, false, ref groupRadioButtons, false, ref prevComments, ref PlaceHolder_NewComments, ref lblCommentRequired, false, ref generalFields, false, ref DocsMainButtons, ref DocsAbacButtons, ref DocsSupportingButtons, ref DocsPaperButtons, ref DocsSignedButtons, false, wfid, wftypeOrder, item, Web, parameters, wftypeName, ref  WFID_Textbox, ref WFID_buttonAdd, false);
                DynamicUserListsPanel.Enabled = false;
                General.saveErrorsLog(wfid, "EnableDisableUIDraft()" + ex.Message);
            }
        }

        /// <summary>
        /// Enable and disable user interface controls according to logged user profile and not confidential workflow rules.
        private static void EnableDisableUIInProgressNonConfidential(int currentStep, SPUser loggedUser, SPUser initiator, Dictionary<string, string> parameters, bool itemExists, ref Panel DynamicUserListsPanel, ref Button btnSign, ref Button btnSign2, ref Button btnSave, ref Button btnSave2, ref Button btnOnHold, ref Button btnOnHold2, ref Button btnCancel, ref Button btnCancel2, ref Button btnDelete, ref Button btnDelete2, ref Button btnClose, ref Button btnClose2, ref HyperLink HyperLinkPrint, ref Button btnAssign, ref Button btnAssign2, ref Button btnReject, ref Button btnReject2, ref DropDownList ddlConfidential, ref RadioButtonList groupRadioButtons, ref PlaceHolder prevComments, ref PlaceHolder PlaceHolder_NewComments, ref Label lblCommentRequired, ref PlaceHolder generalFields, ref PlaceHolder DocsMainButtons, ref PlaceHolder DocsAbacButtons, ref PlaceHolder DocsSupportingButtons, ref PlaceHolder DocsPaperButtons, ref PlaceHolder DocsSignedButtons, string wfid, string wftypeOrder, SPListItem item, SPWeb Web, string wftypeName, ref TextBox WFID_Textbox, ref Button WFID_buttonAdd, string status, bool isConfidential, bool isBackupInitiator, bool isBackupResponsible, string userAD, string passwordAD, string domain)
        {
            string userLoginName = loggedUser.LoginName;
            bool isStepResponsible = false;
            bool isWorkflowInitiator = false;
            bool isMemberOfReassigningGroup = false;
            bool isMemberOfGroup = false;

            try
            {
                
                string userName = loggedUser.Name;
                General.GetUserData(ref userLoginName, ref userName);
                
                isWorkflowInitiator = initiator.ID.Equals(loggedUser.ID);
                isStepResponsible = WorkflowDataManagement.IsStepResponsible(DynamicUserListsPanel, currentStep, userLoginName, wfid);
                isMemberOfReassigningGroup = General.IsMemberOfReassigningGroup(parameters, userLoginName, wfid, currentStep.ToString(), userAD, passwordAD, domain); //Only can reassign the current Step

                if (!(isBackupInitiator || isBackupResponsible))
                    isMemberOfGroup = WorkflowDataManagement.IsMemberOfCurrentGroup(DynamicUserListsPanel, currentStep, userLoginName, wfid); //Only can reassign the current Step
                bool isWorkflowActor = isStepResponsible || WorkflowDataManagement.IsWorkflowActor(DynamicUserListsPanel, userLoginName, wfid) || isBackupResponsible; //Responsible of any step.
                bool isCanReassign = (isWorkflowInitiator || isWorkflowActor || isBackupInitiator);

                EnableDisableActorsNonConfidential(ref DynamicUserListsPanel, currentStep, userLoginName, isMemberOfReassigningGroup, isCanReassign, wfid, isMemberOfGroup);
                EnableDisableUIInProgressLogic(isStepResponsible, currentStep, status, parameters, isWorkflowInitiator, ref btnSign, ref btnSign2, ref btnSave, ref  btnSave2, ref btnOnHold, ref btnOnHold2, ref btnCancel, ref  btnCancel2, ref  btnDelete, ref  btnDelete2, ref  btnClose, ref btnClose2, ref btnAssign, ref btnAssign2, ref btnReject, ref btnReject2, ref  HyperLinkPrint, ref  ddlConfidential, ref  groupRadioButtons, ref prevComments, ref  PlaceHolder_NewComments, ref lblCommentRequired, ref generalFields, ref  DocsMainButtons, ref  DocsAbacButtons, ref  DocsSupportingButtons, ref  DocsPaperButtons, ref  DocsSignedButtons, wfid, wftypeOrder, item, Web, wftypeName, ref  WFID_Textbox, ref  WFID_buttonAdd, userLoginName, isMemberOfReassigningGroup, isConfidential, isMemberOfGroup, isBackupInitiator, isBackupResponsible);

                if (isCanReassign)
                {
                    btnSave.Visible = btnSave2.Visible = true;
                    btnSave.Enabled = btnSave2.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                EnableDisableUIInProgressLogic(isStepResponsible, currentStep, status, parameters, isWorkflowInitiator, ref btnSign, ref btnSign2, ref btnSave, ref  btnSave2, ref btnOnHold, ref btnOnHold2, ref btnCancel, ref  btnCancel2, ref  btnDelete, ref  btnDelete2, ref  btnClose, ref btnClose2, ref btnAssign, ref btnAssign2, ref btnReject, ref btnReject2, ref  HyperLinkPrint, ref  ddlConfidential, ref  groupRadioButtons, ref prevComments, ref  PlaceHolder_NewComments, ref lblCommentRequired, ref generalFields, ref  DocsMainButtons, ref  DocsAbacButtons, ref  DocsSupportingButtons, ref  DocsPaperButtons, ref  DocsSignedButtons, wfid, wftypeOrder, item, Web, wftypeName, ref  WFID_Textbox, ref  WFID_buttonAdd, userLoginName, isMemberOfReassigningGroup, isConfidential, isMemberOfGroup, isBackupInitiator, isBackupResponsible);
                DynamicUserListsPanel.Enabled = false;
                General.saveErrorsLog(wfid, "EnableDisableUIInProgressNonConfidential()" + ex.Message);
            }
        }

        //ESMA-CR39-Save Button available
        private static void EnableDisableActorsNonConfidential(ref Panel DynamicUserListsPanel, int currentStep, string userLoginName, bool isMemberOfReassigningGroup, bool isCanReassign, string wfid, bool isMemberOfGroup)
        {
            int count = 1;

            foreach (Control control in DynamicUserListsPanel.Controls)
            {
                try
                {
                    if (control is UpdatePanel)
                    {
                        UpdatePanel actorUpdatePanel = (UpdatePanel)control;
                        DropDownList actorListDDL = (DropDownList)actorUpdatePanel.Controls[0].Controls[0];

                        if (actorListDDL != null)
                        {

                           
                            if (count.Equals(currentStep) && !count.Equals(1) && (isCanReassign || isMemberOfReassigningGroup || isMemberOfGroup)) 
                                actorListDDL.Enabled = true;
                            else if ((count > currentStep) && (isCanReassign))
                                actorListDDL.Enabled = true;
                            else
                                actorListDDL.Enabled = false;
                        }

                        count++;
                    }
                }
                catch (Exception ex)
                {
                    General.saveErrorsLog(wfid, "EnableDisableActorsNonConfidential - Count: '" + count + "' - " + ex.Message);
                    continue;
                }
            }
        }

        //ESMA-CR21-Permissions in Restricted workflows (Save Button available)
        private static void EnableDisableUIInProgressConfidential(int currentStep, SPUser loggedUser, SPUser initiator, Dictionary<string, string> parameters, bool itemExists, ref Panel DynamicUserListsPanel, ref Button btnSign, ref Button btnSign2, ref Button btnSave, ref Button btnSave2, ref Button btnOnHold, ref Button btnOnHold2, ref Button btnCancel, ref Button btnCancel2, ref Button btnDelete, ref Button btnDelete2, ref Button btnClose, ref Button btnClose2, ref HyperLink HyperLinkPrint, ref Button btnAssign, ref Button btnAssign2, ref Button btnReject, ref Button btnReject2, ref DropDownList ddlConfidential, ref RadioButtonList groupRadioButtons, ref PlaceHolder prevComments, ref PlaceHolder PlaceHolder_NewComments, ref Label lblCommentRequired, ref PlaceHolder generalFields, ref PlaceHolder DocsMainButtons, ref PlaceHolder DocsAbacButtons, ref PlaceHolder DocsSupportingButtons, ref PlaceHolder DocsPaperButtons, ref PlaceHolder DocsSignedButtons, string wfid, string wftypeOrder, SPListItem item, SPWeb Web, string wftypeName, ref TextBox WFID_Textbox, ref Button WFID_buttonAdd, string status, bool isConfidential, bool isBackupInitiator, bool isBackupResponsible)
        {
            bool isStepResponsible = false;
            bool isWorkflowInitiator = false;
            string userLoginName = loggedUser.LoginName;

            try
            {
                
                string userName = loggedUser.Name;
                General.GetUserData(ref userLoginName, ref userName);

                isWorkflowInitiator = initiator.ID.Equals(loggedUser.ID);
                isStepResponsible = WorkflowDataManagement.IsStepResponsible(DynamicUserListsPanel, currentStep, userLoginName, wfid);
                bool isWorkflowActor = isStepResponsible || WorkflowDataManagement.IsWorkflowActor(DynamicUserListsPanel, userLoginName, wfid) || isBackupResponsible;
                bool isCanReassign = (isWorkflowInitiator || isWorkflowActor || isBackupInitiator);

                EnableDisableActorsConfidential(ref DynamicUserListsPanel, isCanReassign, currentStep, wfid);
                EnableDisableUIInProgressLogic(isStepResponsible, currentStep, status, parameters, isWorkflowInitiator, ref btnSign, ref btnSign2, ref btnSave, ref  btnSave2, ref btnOnHold, ref btnOnHold2, ref btnCancel, ref  btnCancel2, ref  btnDelete, ref  btnDelete2, ref  btnClose, ref btnClose2, ref btnAssign, ref btnAssign2, ref btnReject, ref btnReject2, ref  HyperLinkPrint, ref  ddlConfidential, ref  groupRadioButtons, ref prevComments, ref  PlaceHolder_NewComments, ref lblCommentRequired, ref generalFields, ref  DocsMainButtons, ref  DocsAbacButtons, ref  DocsSupportingButtons, ref  DocsPaperButtons, ref  DocsSignedButtons, wfid, wftypeOrder, item, Web, wftypeName, ref  WFID_Textbox, ref  WFID_buttonAdd, userLoginName, false, isConfidential, false, isBackupInitiator, isBackupResponsible);
            
                if (isCanReassign)
                {
                    btnSave.Visible = btnSave2.Visible = true;
                    btnSave.Enabled = btnSave2.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                EnableDisableUIInProgressLogic(isStepResponsible, currentStep, status, parameters, isWorkflowInitiator, ref btnSign, ref btnSign2, ref btnSave, ref  btnSave2, ref btnOnHold, ref btnOnHold2, ref btnCancel, ref  btnCancel2, ref  btnDelete, ref  btnDelete2, ref  btnClose, ref btnClose2, ref btnAssign, ref btnAssign2, ref btnReject, ref btnReject2, ref  HyperLinkPrint, ref  ddlConfidential, ref  groupRadioButtons, ref prevComments, ref  PlaceHolder_NewComments, ref lblCommentRequired, ref generalFields, ref  DocsMainButtons, ref  DocsAbacButtons, ref  DocsSupportingButtons, ref  DocsPaperButtons, ref  DocsSignedButtons, wfid, wftypeOrder, item, Web, wftypeName, ref  WFID_Textbox, ref  WFID_buttonAdd, userLoginName, false, isConfidential, false, isBackupInitiator, isBackupResponsible);
                DynamicUserListsPanel.Enabled = false;
                General.saveErrorsLog(wfid, "EnableDisableUIInProgressConfidential -userLoginName: '" + userLoginName + "' - " + ex.Message);
            }
        }

        private static void EnableDisableActorsConfidential(ref Panel DynamicUserListsPanel, bool isCanReassign, int currentStep, string wfid)
        {
            int count = 1;

            foreach (Control control in DynamicUserListsPanel.Controls)
            {
                try
                {
                    if (control is UpdatePanel)
                    {
                        UpdatePanel actorUpdatePanel = (UpdatePanel)control;
                        DropDownList actorList = (DropDownList)actorUpdatePanel.Controls[0].Controls[0];

                        if (count.Equals(1))
                            actorList.Enabled = false;
                        else if (isCanReassign && count >= currentStep)
                            actorList.Enabled = true;
                        else
                            actorList.Enabled = false;

                        count++;
                    }
                }
                catch (Exception ex)
                {
                    General.saveErrorsLog(wfid, "EnableDisableActorsConfidential - Cont: '" + count + "' - " + ex.Message);
                    continue; 
                }
            }
        }
 
        private static void EnableDisableUIInProgressLogic(bool isStepResponsible, int currentStep, string status, Dictionary<string, string> parameters, bool isWorkflowInitiator, ref Button btnSign, ref Button btnSign2, ref Button btnSave, ref Button btnSave2, ref Button btnOnHold, ref Button btnOnHold2, ref Button btnCancel, ref Button btnCancel2, ref Button btnDelete, ref Button btnDelete2, ref Button btnClose, ref Button btnClose2, ref Button btnAssign, ref Button btnAssign2, ref Button btnReject, ref Button btnReject2, ref HyperLink HyperLinkPrint, ref DropDownList ddlConfidential, ref RadioButtonList groupRadioButtons, ref PlaceHolder prevComments, ref PlaceHolder PlaceHolder_NewComments, ref Label lblCommentRequired, ref PlaceHolder generalFields, ref PlaceHolder DocsMainButtons, ref PlaceHolder DocsAbacButtons, ref PlaceHolder DocsSupportingButtons, ref PlaceHolder DocsPaperButtons, ref PlaceHolder DocsSignedButtons, string wfid, string wftypeOrder, SPListItem item, SPWeb Web, string wftypeName, ref TextBox WFID_Textbox, ref Button WFID_buttonAdd, string userLoginName, bool isMemberOfReassigningGroup, bool isConfidential, bool isMemberOfGroup, bool isBackupInitiator, bool isBackupResponsible)
        {
            try
            {
                //Buttons - Actions
                bool visibleBtnSign = false;
                bool visibleBtnSave = false;
                bool visibleBtnOnHold = false;
                bool visibleBtnCancel = false;
                bool visibleBtnDelete = false;
                bool visibleBtnClose = false;
                bool visibleBtnAssign = false;
                bool visibleBtnReject = false;

                bool visibleHyperLinkPrint = false;
                bool visibleRadioButtons = false;
                bool visibleBtnDocuments = false;

                //Enabled
                bool enableBtnAssign = false;
                bool enableDDLConfidential = false;
                bool enableGeneralFields = false;
                bool enableLinkToWF = false;
                bool enableComments = false;

                if ((isStepResponsible && isWorkflowInitiator) || (isBackupResponsible && isBackupInitiator))
                {

                    visibleBtnSign = visibleBtnSave = visibleBtnCancel = true;
                    visibleBtnDocuments = true;
                    enableDDLConfidential = enableGeneralFields = enableLinkToWF = enableComments = true;

                    //the WF can be rejected in the Step 1
                    if (currentStep != 1)
                    {
                        visibleHyperLinkPrint = true;
                        visibleBtnReject = true;
                        visibleBtnDelete = false;
                    }
                    else
                        visibleBtnDelete = true;

                  

                    //If the Status is On Hold, not display this button
                    if (!status.ToUpper().Equals(parameters["Status On Hold"].ToUpper()))
                        visibleBtnOnHold = true;
                   

                }
                else if ((isStepResponsible && !isWorkflowInitiator) || (isBackupResponsible && !isBackupInitiator))
                {
                   
                        visibleBtnSign = visibleBtnSave = visibleBtnCancel = true;
                        visibleBtnDocuments = true;
                        enableGeneralFields = enableLinkToWF = enableComments = true;

                        //the WF can be rejected in the Step 1
                        if (currentStep != 1)
                        {
                            visibleHyperLinkPrint = true;
                            visibleBtnReject = true;
                        }

                        //If the Status is On Hold, not display this button
                        if (!status.ToUpper().Equals(parameters["Status On Hold"].ToUpper()))
                            visibleBtnOnHold = true;
                    

                }
                else if ((!isStepResponsible && isWorkflowInitiator) || (!isBackupResponsible && isBackupInitiator))
                {
                    visibleBtnSave = visibleBtnClose = visibleBtnCancel = true;
                    enableDDLConfidential = enableComments = true;

                    if (currentStep != 1)
                        visibleHyperLinkPrint = true;

                    if (!isConfidential.Equals(true))
                        enableLinkToWF = true;

                }
                else if ((isMemberOfReassigningGroup || isMemberOfGroup) && isConfidential.Equals(false))
                {
                    visibleBtnClose = true;
                    enableComments = true;

                    //the WF can be rejected in the Step 1
                    if (currentStep != 1)
                        visibleHyperLinkPrint = true;
                }
                else
                {
                    //Readers (no Initiator, no responsible, no MemberOfReassigningGroup)
                    visibleBtnClose = true;
                    //the WF can be rejected in the Step 1
                    if (currentStep != 1)
                        visibleHyperLinkPrint = true;
                }

 
                EnableDisableControls(ref btnSign, ref btnSign2, "Sign", visibleBtnSign, ref btnSave, ref btnSave2, visibleBtnSave, ref btnOnHold, ref btnOnHold2, visibleBtnOnHold, ref btnCancel, ref btnCancel2, visibleBtnCancel, ref btnDelete, ref btnDelete2, visibleBtnDelete, ref btnClose, ref btnClose2, visibleBtnClose, ref HyperLinkPrint, visibleHyperLinkPrint, ref btnAssign, ref btnAssign2, visibleBtnAssign, enableBtnAssign, ref btnReject, ref btnReject2, visibleBtnReject, ref ddlConfidential, enableDDLConfidential, ref groupRadioButtons, visibleRadioButtons, ref prevComments, ref PlaceHolder_NewComments, ref lblCommentRequired, enableComments, ref generalFields, enableGeneralFields, ref DocsMainButtons, ref DocsAbacButtons, ref DocsSupportingButtons, ref DocsPaperButtons, ref DocsSignedButtons, visibleBtnDocuments, wfid, wftypeOrder, item, Web, parameters, wftypeName, ref  WFID_Textbox, ref WFID_buttonAdd, enableLinkToWF);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "EnableDisableUIInProgressLogic: '" + userLoginName + "' - " + ex.Message);
            }
        }

        /// <summary>
        /// Disable controls if any document is checked out avoiding any step signing.
        private static void EnableDisableIfCheckedOut(SPListItem item, string wfid, SPWeb Web, ref Button btnSign, ref Button btnSign2, ref Button btnAssign, ref Button btnAssign2, ref Button btnReject, ref Button btnReject2, ref Panel DynamicUserListsPanel, ref Label lblDocumentsCheckedOutWarning, ref Panel PanelCheckedOutWarning, ref RadioButtonList groupRadioButtons, bool rejecting, bool draft)
        {
            try
            {
                if (draft.Equals(false))
                {
                    bool allIsCheckIn = DocumentLibraries.EverythingCheckedIn(item.ParentList, wfid, Web);

                    if (allIsCheckIn.Equals(false))
                    {
                        EnableDisableActorsControlIfCheckedOut(wfid, allIsCheckIn, ref DynamicUserListsPanel);

                        //Reject Button
                        btnReject.Enabled = btnReject2.Enabled = allIsCheckIn;

                        if (rejecting && (btnAssign.Enabled || btnAssign2.Enabled))
                            btnAssign.Enabled = btnAssign2.Enabled = allIsCheckIn;
                        else if (rejecting)
                        {
                            groupRadioButtons.Enabled = allIsCheckIn;

                            if (allIsCheckIn && groupRadioButtons.SelectedItem != null)
                                btnAssign.Enabled = btnAssign2.Enabled = allIsCheckIn;
                        }

                        btnSign.Enabled = btnSign2.Enabled = allIsCheckIn;

                        //CR27
                        //lblDocumentsCheckedOutWarning.Visible = !enable;

                        lblDocumentsCheckedOutWarning.Visible = !allIsCheckIn;
                        PanelCheckedOutWarning.Visible = !allIsCheckIn;
                    }
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "EnableDisableIfCheckedOut() " + ex.Message);
            }
        }

        private static void EnableDisableActorsControlIfCheckedOut(string wfid, bool enable, ref Panel DynamicUserListsPanel)
        {
            try
            {

                int count = 0;

                foreach (Control control in DynamicUserListsPanel.Controls)
                {
                    try
                    {
                        if (control is UpdatePanel)
                        {
                            UpdatePanel actorUpdatePanel = (UpdatePanel)control;
                            DropDownList actorList = (DropDownList)actorUpdatePanel.Controls[0].Controls[0];

                            if (actorList != null && actorList.Enabled)
                                actorList.Enabled = enable;
                            
                            count++;
                        }
                    }
                    catch (Exception ex)
                    {
                        General.saveErrorsLog(wfid, "EnableDisableActorsControlIfCheckedOut() - Count: '" + count + "' - " + ex.Message);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "EnableDisableActorsControlIfCheckedOut() " + ex.Message);
            }
        }


        #endregion
        
       
    }
}
