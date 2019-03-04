using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using System.DirectoryServices.AccountManagement;
using Microsoft.SharePoint.Administration.Claims;

namespace ESMA.Paperless.Webparts.v16.RSWorkflow
{
    public static class Permissions
    {
        /// <summary>
        /// Manage permissions according to confidentiality configuration.
        /// </summary>
        /// <param name="itemToEdit"></param>
        /// <param name="itemToRead"></param>
        /// <param name="responsibleUser"></param>
        /// <param name="realEditor"></param>
        /// <param name="parameters"></param>
        /// <param name="confidentialValue"></param>
        public static void SetUpWorkflowPermissions(ref SPListItem itemToEdit, SPListItem itemToRead, SPUser responsibleUser, SPUser realEditor, Dictionary<string, string> parameters, string confidentialValue, string wfid, Dictionary<string, string> actorsBackupDictionary, string status, bool reassignToBackupActor, int stepNumber, bool isSaving)
        {
            try
            {
                bool isConfidential = (string.IsNullOrEmpty(confidentialValue) || confidentialValue.ToUpper().Equals("NON RESTRICTED")) ? false : true;
                SPRoleDefinition roleDefinitionRSRead = SPContext.Current.Web.Site.RootWeb.RoleDefinitions["RS Read"];
                SPRoleDefinition roleDefinitionRSContributor = SPContext.Current.Web.Site.RootWeb.RoleDefinitions["RS Contribute"];
                SPRoleDefinition roleDefinitionRSFullControl = SPContext.Current.Web.Site.RootWeb.RoleDefinitions["RS Full Control"];

                if (isConfidential)
                    SetStepResponsiblePermissionsConfid(ref itemToEdit, itemToRead, responsibleUser, realEditor, parameters, false, actorsBackupDictionary, roleDefinitionRSRead, roleDefinitionRSContributor, roleDefinitionRSFullControl, status, reassignToBackupActor, stepNumber, isSaving);
                else
                    SetStepResponsiblePermissionsNotConfid(ref itemToEdit, responsibleUser, realEditor, parameters, wfid, roleDefinitionRSRead, roleDefinitionRSContributor, roleDefinitionRSFullControl, status, actorsBackupDictionary, reassignToBackupActor, stepNumber, isSaving);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetUpWorkflowPermissions() " + ex.Message);
            }
        }

        /// <summary>
        /// Set workflow folder and documentation permissions for not confidential workflows
        /// </summary>
        /// <param name="item"></param>
        /// <param name="user"></param>
        /// <param name="realEditor"></param>
        /// <param name="parameters"></param>
        public static void SetStepResponsiblePermissionsNotConfid(ref SPListItem item, SPUser user, SPUser realEditor, Dictionary<string, string> parameters, string wfid, SPRoleDefinition roleDefinitionRSRead, SPRoleDefinition roleDefinitionRSContributor, SPRoleDefinition roleDefinitionRSFullControl, string status, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, int stepNumber, bool isSaving)
        {
            try
            {
                item.ResetRoleInheritance();

                if (!(status.ToUpper().Equals(parameters["Status Closed"].ToUpper().Trim())) && !(status.ToUpper().Equals(parameters["Status Deleted"].ToUpper().Trim())))
                {
                   
                    if (!item.HasUniqueRoleAssignments)
                        item.BreakRoleInheritance(true, true);

                    SPWeb Web = item.Web;
                    string WFID = item["WFID"].ToString();
                    string administratorGroup = parameters["Domain"].ToString() + "\\" + parameters["RS Admin Group"].ToString();
                    string author = GetAuthor(WFID, item, Web, realEditor);
                    SPUser responsible = null;
                    SPUser initiator = null;

                    RemoveStepResponsiblePermissions(ref item, realEditor, wfid, roleDefinitionRSRead);
                    
                    //Responsible
                    if (((!status.ToUpper().Equals(parameters["Status Closed"].ToUpper().Trim())) && (!status.ToUpper().Equals(parameters["Status Deleted"].ToUpper().Trim()))) && reassignToBackupActor.Equals(false))
                        SetResponsiblePermissions(wfid, user, ref item, Web, roleDefinitionRSRead, roleDefinitionRSContributor, ref responsible, status, parameters, stepNumber);
                    else if ((reassignToBackupActor.Equals(true)) && (!status.ToUpper().Equals(parameters["Status On Hold"].ToUpper())))
                        SetResponsiblePermissions(wfid, user, ref item, Web, roleDefinitionRSRead, roleDefinitionRSContributor, ref responsible, status, parameters, stepNumber);


                    //Initiator (Contributor/Read if WF Closed/Deleted)

                    try
                    {
                        string step1AssignedTo = GetActorStep1Assigned(wfid, item, Web);

                        if (!string.IsNullOrEmpty(step1AssignedTo))
                            SetInitiatorPermissions(wfid, ref item, Web, step1AssignedTo, roleDefinitionRSContributor, roleDefinitionRSRead, status, parameters, ref initiator);
                        else
                            SetInitiatorPermissions(wfid, ref item, Web, author, roleDefinitionRSContributor, roleDefinitionRSRead, status, parameters, ref initiator);
                    }
                    catch
                    {
                        General.saveErrorsLog(wfid, "SetStepResponsiblePermissionsNotConfid - Exception adding Initiator '");
                    }

                   
                    //Administrator Group (Full Control)
                    SetSuperAdministratorGroupPermissionsNotConfid(WFID, ref item, Web, administratorGroup, roleDefinitionRSFullControl, roleDefinitionRSRead, status, parameters);

                    //ESMA-CR31-BackupGroup  (Contributor - Allow to upload documents)
                    if (actorsBackupDictionary != null && actorsBackupDictionary.Count > 0)
                        SetBackupGroupPermissionsConfid(wfid, ref item, item, actorsBackupDictionary, Web, roleDefinitionRSContributor, roleDefinitionRSRead, status, parameters, stepNumber, isSaving);
                    

                    item["Editor"] = realEditor;

                    using (new DisabledItemEventsScope())
                    {
                        item.Update();
                    }
                }
                
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetStepResponsiblePermissionsNotConfid(): Item: " + item.Url + " - " + ex.Message + " - " + ex.InnerException);
            }
        }

        /// <summary>
        /// Set workflow folder and documentation permissions for confidential workflows.
        /// </summary>
        /// <param name="itemToEdit"></param>
        /// <param name="itemToRead"></param>
        /// <param name="user"></param>
        /// <param name="realEditor"></param>
        /// <param name="parameters"></param>
        public static void SetStepResponsiblePermissionsConfid(ref SPListItem itemToEdit, SPListItem itemToRead, SPUser user, SPUser realEditor, Dictionary<string, string> parameters, bool isGroupConfidential, Dictionary<string, string> actorsBackupDictionary, SPRoleDefinition roleDefinitionRSRead, SPRoleDefinition roleDefinitionRSContributor, SPRoleDefinition roleDefinitionRSFullControl, string status, bool reassignToBackupActor, int stepNumber, bool isSaving)
        {
            string wfid = string.Empty;

            try
            {
                //itemToRead (Folder WF)
                //itemToEdit (Item WF History)
                if (itemToRead["ConfidentialWorkflow"] != null)
                {
                    itemToEdit.ResetRoleInheritance();

                    if (!itemToEdit.HasUniqueRoleAssignments)
                        itemToEdit.BreakRoleInheritance(false);

                    SPWeb Web = itemToEdit.Web;
                    wfid = itemToEdit["WFID"].ToString();
                    string confidentialGroup = parameters["Domain"].ToString() + "\\" + parameters["RS Restricted Admin Group"].ToString();
                    string author = GetAuthor(wfid, itemToEdit, Web, realEditor);
                    SPUser responsible = null;
                    SPUser initiator = null;

                    //Responsible
                    if (((!status.ToUpper().Equals(parameters["Status Closed"].ToUpper().Trim())) && (!status.ToUpper().Equals(parameters["Status Deleted"].ToUpper().Trim()))) && reassignToBackupActor.Equals(false))
                        SetResponsiblePermissions(wfid, user, ref itemToEdit, Web, roleDefinitionRSRead, roleDefinitionRSContributor, ref responsible, status, parameters, stepNumber);
                    else if ((reassignToBackupActor.Equals(true)) && (!status.ToUpper().Equals(parameters["Status On Hold"].ToUpper())))
                        SetResponsiblePermissions(wfid, user, ref itemToEdit, Web, roleDefinitionRSRead, roleDefinitionRSContributor, ref responsible, status, parameters, stepNumber);
                    
                        //Confidential Group
                        SetConfidGroupPermissionsConfid(wfid, ref itemToEdit, itemToRead, confidentialGroup, Web, roleDefinitionRSRead);

                        //Initiator (Contributor/Read if WF Closed/Deleted)

                        try
                        {
                            string step1AssignedTo = GetActorStep1Assigned(wfid, itemToRead, Web);

                            if (!string.IsNullOrEmpty(step1AssignedTo))
                                SetInitiatorPermissions(wfid, ref itemToEdit, Web, step1AssignedTo, roleDefinitionRSContributor, roleDefinitionRSRead, status, parameters, ref initiator);
                            else
                                SetInitiatorPermissions(wfid, ref itemToEdit, Web, author, roleDefinitionRSContributor, roleDefinitionRSRead, status, parameters, ref initiator);
                        }
                        catch
                        {
                            General.saveErrorsLog(wfid, "SetStepResponsiblePermissionsConfid - Exception adding Initiator '");
                        }
                        
                    
                    //Actors
                    SetRSPermissionsForWFUsers(wfid, ref itemToEdit, itemToRead, parameters, author, Web, roleDefinitionRSRead, responsible, initiator, reassignToBackupActor, stepNumber);

                    //ESMA-CR31-BackupGroup (Contributor - Allow to upload documents)
                    if (actorsBackupDictionary != null && actorsBackupDictionary.Count > 0)
                        SetBackupGroupPermissionsConfid(wfid, ref itemToEdit, itemToRead, actorsBackupDictionary, Web, roleDefinitionRSContributor, roleDefinitionRSRead, status, parameters, stepNumber,   isSaving);

 
                    itemToEdit["Editor"] = realEditor;

                    using (new DisabledItemEventsScope())
                    {
                        itemToEdit.Update();
                    }
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetStepResponsiblePermissionsConfid " + ex.Message);
            }
        }


        public static string GetAuthor(string WFID, SPListItem item, SPWeb Web, SPUser user)
        {
            string author = string.Empty;
           

            try
            {
                SPListItem wfHistoryItem = WorkflowDataManagement.GetWorkflowHistoryRecord(WFID, Web);

                if (wfHistoryItem == null)
                    author = user.ToString();
                else
                {
                    if (!(item.ParentList.Title.Equals("RS Workflow History")))
                        item = wfHistoryItem;

                    SPUser userHistory = null;

                    if (item["InitiatedBy"] != null)
                        userHistory = General.GetSPUser(item, "InitiatedBy", WFID, Web);
                    else
                        userHistory = General.GetSPUser(item, "Author", WFID, Web);

                    author = userHistory.ToString();
                }
                  
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(WFID, "GetAuthor() " + ex.Message);
            }

            return author;
        }

        /// <summary>
        /// //Grant Contribute permissions to current step responsible 
        /// </summary>
        /// <param name="WFID"></param>
        public static void SetResponsiblePermissions(string WFID, SPUser user, ref SPListItem itemToEdit, SPWeb Web, SPRoleDefinition roleDefinitionRSRead, SPRoleDefinition roleDefinitionRSContributor, ref SPUser responsible, string status, Dictionary<string,string> parameters, int currentStep )
        {
           
            try
            {

                if (user != null)
                {
                    try
                    {
                        responsible = Web.EnsureUser(user.LoginName);
                    }
                    catch
                    {
                        responsible = Web.Site.RootWeb.EnsureUser(user.LoginName);
                    }
                }
                

                if (responsible != null)
                {

                    SPRoleAssignment roleAssignmentResp = new SPRoleAssignment(responsible);
                    SPRoleDefinition roleDefinitionRSResponsible = null;

                    if ((!status.ToUpper().Equals(parameters["Status Closed"].ToUpper().Trim()) && (itemToEdit.ContentType.Name.ToUpper().Equals("WORKFLOW")) && (!status.ToUpper().Equals(parameters["Status Deleted"].ToUpper().Trim()))))
                        roleDefinitionRSResponsible = roleDefinitionRSContributor;
                    else
                        roleDefinitionRSResponsible = roleDefinitionRSRead;


                    roleAssignmentResp.RoleDefinitionBindings.Add(roleDefinitionRSResponsible);
                    itemToEdit.RoleAssignments.Add(roleAssignmentResp);
                }
                else
                    General.saveErrorsLog(WFID, "SetResponsiblePermissions: Responsible NULL - Step: '" + currentStep + "' - " + itemToEdit.Url);

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(WFID, "SetResponsiblePermissions: EnsuredResponsible '" + responsible  + "' " + ex.Message  + " - " + itemToEdit.Url);
            }

        }

        /// <summary>
        /// //Grant Read permissions to Confidential Group
        /// </summary>
        /// <param name="WFID"></param>
        /// <param name="user"></param>
        public static void SetConfidGroupPermissionsConfid(string WFID, ref  SPListItem itemToEdit, SPListItem itemToRead, string confidentialGroup, SPWeb Web, SPRoleDefinition roleDefinitionRSRead)
        {
            SPUser ensuredAdminGroup = null;

            try
            {

                try
                {
                    ensuredAdminGroup = Web.EnsureUser(confidentialGroup);
                }
                catch
                {
                    ensuredAdminGroup = Web.Site.RootWeb.EnsureUser(confidentialGroup);
                }

                if (ensuredAdminGroup != null)
                {
                    SPRoleAssignment roleAssignmentAdmin = new SPRoleAssignment(ensuredAdminGroup);
                    roleAssignmentAdmin.RoleDefinitionBindings.Add(roleDefinitionRSRead);
                    itemToEdit.RoleAssignments.Add(roleAssignmentAdmin);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(WFID, "SetConfidGroupPermissionsConfid: EnsuredResponsible '" + ensuredAdminGroup + "' " + ex.Message + " - " + itemToEdit.Url);
            
            }

        }

        /// <summary>
        /// //Grant Read permissions to Confidential Group
        /// </summary>
        /// <param name="WFID"></param>
        /// <param name="user"></param>
        public static void SetBackupGroupPermissionsConfid(string WFID, ref  SPListItem itemToEdit, SPListItem itemToRead, Dictionary<string, string> actorsBackupDictionary, SPWeb Web, SPRoleDefinition roleDefinitionRSContributor, SPRoleDefinition roleDefinitionRSRead, string status, Dictionary<string, string> parameters, int stepNumber, bool isSaving)
        {
            SPUser ensuredBackupGroup = null;

            try
            {
                foreach (KeyValuePair<String, String> kvp in actorsBackupDictionary)
                {
                    try
                    {
                        string backupGroup = kvp.Value;
                        string stepNumberBackupGroup = kvp.Key;

                        try
                        {
                            ensuredBackupGroup = Web.EnsureUser(parameters["Domain"].ToString() + "\\"+ backupGroup);
                        }
                        catch
                        {
                            ensuredBackupGroup = Web.Site.RootWeb.EnsureUser(parameters["Domain"].ToString() + "\\" + backupGroup);
                        }

                        if (ensuredBackupGroup != null)
                        {
                            SPRoleAssignment roleAssignmentBackup = new SPRoleAssignment(ensuredBackupGroup);
                            SPRoleDefinition roleDefinitionRSResponsible = null;

                            if ((!status.ToUpper().Equals(parameters["Status Closed"].ToUpper().Trim()) && (itemToEdit.ContentType.Name.ToUpper().Equals("WORKFLOW")) && (!status.ToUpper().Equals(parameters["Status Deleted"].ToUpper().Trim()))))
                            {
                                if (stepNumberBackupGroup.Equals("1") ||  stepNumberBackupGroup.Equals(Convert.ToString(stepNumber)))
                                    roleDefinitionRSResponsible = roleDefinitionRSContributor;
                                else
                                    roleDefinitionRSResponsible = roleDefinitionRSRead;
                            }
                            else
                                roleDefinitionRSResponsible = roleDefinitionRSRead;

                            roleAssignmentBackup.RoleDefinitionBindings.Add(roleDefinitionRSResponsible);
                            itemToEdit.RoleAssignments.Add(roleAssignmentBackup);
                        }
                    }
                    catch
                    {
                        General.saveErrorsLog(WFID, "SetBackupGroupPermissionsConfid- Foreach: BackupGroup '" + ensuredBackupGroup + "' - " + itemToEdit.Url);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(WFID, "SetBackupGroupPermissionsConfid: BackupGroup '" + ensuredBackupGroup + "' " + ex.Message + " - " + itemToEdit.Url);
            }

        }

        /// <summary>
        ///  //Grant Read permissions to Initiator
        /// </summary>
        /// <param name="WFID"></param>
        /// <param name="user"></param>
        public static void SetInitiatorPermissions(string WFID, ref SPListItem itemToEdit, SPWeb Web, string author, SPRoleDefinition roleDefinitionRSContributor, SPRoleDefinition roleDefinitionRSRead, string status, Dictionary<string,string> parameters, ref SPUser initiator)
        {
           

            try
            {
                
                try
                {
                    initiator = Web.EnsureUser(author);
                }
                catch
                {
                    initiator = Web.Site.RootWeb.EnsureUser(author);
                }

                if (initiator != null)
                {
                    SPRoleAssignment roleAssignmentInit = new SPRoleAssignment(initiator);

                    if ((!(status.ToUpper().Equals(parameters["Status Closed"].ToUpper().Trim()))) && (itemToEdit.ContentType.Name.ToUpper().Equals("WORKFLOW")) && (!(status.ToUpper().Equals(parameters["Status Deleted"].ToUpper().Trim()))))
                        roleAssignmentInit.RoleDefinitionBindings.Add(roleDefinitionRSContributor);
                    else
                        roleAssignmentInit.RoleDefinitionBindings.Add(roleDefinitionRSRead);

                    itemToEdit.RoleAssignments.Add(roleAssignmentInit);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(WFID, "SetInitiatorPermissions: Initiator '" + initiator + "' " + ex.Message + " - " + itemToEdit.Url);
            }

        }

        /// <summary>
        ///  //Grant Full Control permissions to Super Administrator Group
        /// </summary>
        /// <param name="WFID"></param>
        /// <param name="user"></param>
        public static void SetSuperAdministratorGroupPermissionsNotConfid(string WFID, ref SPListItem item, SPWeb Web, string administratorGroup, SPRoleDefinition roleDefinitionRSFullControl, SPRoleDefinition roleDefinitionRSRead, string status, Dictionary<string, string> parameters)
        {
            try
            {

                SPUser ensuredAdminGroup;

                try
                {
                    ensuredAdminGroup = Web.EnsureUser(administratorGroup);
                }
                catch
                {
                    ensuredAdminGroup = Web.Site.RootWeb.EnsureUser(administratorGroup);
                }

                //Grant full control permissions to administration group
                if (ensuredAdminGroup != null)
                {
                    SPRoleAssignment roleAssignmentAdmin = new SPRoleAssignment(ensuredAdminGroup);
                    if ((!(status.ToUpper().Equals(parameters["Status Closed"].ToUpper().Trim()))) && (item.ContentType.Name.ToUpper().Equals("WORKFLOW")) && (!(status.ToUpper().Equals(parameters["Status Deleted"].ToUpper().Trim()))))
                        roleAssignmentAdmin.RoleDefinitionBindings.Add(roleDefinitionRSFullControl);
                    else
                        roleAssignmentAdmin.RoleDefinitionBindings.Add(roleDefinitionRSRead);
                    
                    item.RoleAssignments.Add(roleAssignmentAdmin);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(WFID, "SetSuperAdministratorGroupPermissionsNotConfid: Administrator Group '" + administratorGroup + "' " + ex.Message + " - " + item.Url);
            }

        }



        /// <summary>
        /// Set different workflow step reponsible permissions. Grant Read permissions to different step responsibles.
        /// </summary>
        /// <param name="itemToEdit"></param>
        /// <param name="itemToRead"></param>
        /// <param name="parameters"></param>
        public static void SetRSPermissionsForWFUsers(string wfid, ref SPListItem itemToEdit, SPListItem itemToRead, Dictionary<string, string> parameters, string author, SPWeb Web, SPRoleDefinition roleDefinitionRS, SPUser responsibleUser, SPUser initiator, bool reassignToBackupActor, int currentStep)
        {
            try
            {
                //itemToRead: WF Library Item
                string initialSteps = itemToRead["InitialSteps"] != null ? itemToRead["InitialSteps"].ToString() : string.Empty;
                int wfSteps = WorkflowDataManagement.GetGroupNames(initialSteps, Web, wfid) != null ? WorkflowDataManagement.GetGroupNames(initialSteps, Web, wfid).Count : 0;


                for (int i = 1; i <= wfSteps; i++)
                {
                    bool addUser = false;

                    try
                    {
                        if (itemToRead.Fields.ContainsField("Step " + i + " Assigned To"))
                        {
                            SPField field = itemToRead.Fields.GetField("Step " + i + " Assigned To");

                            if (itemToRead[field.InternalName] != null)
                            {


                                SPUser user = General.GetSPUser(itemToRead, field.InternalName, wfid, Web);

                                if (reassignToBackupActor.Equals(false))
                                {
                                    if ((!initiator.ID.Equals(user.ID)) && ((responsibleUser != null) && (!responsibleUser.ID.Equals(user.ID))))
                                        addUser = true;
                                    else if (responsibleUser == null)
                                        addUser = true;
                                }
                                else
                                {
                                    if (!(i.Equals(currentStep)))
                                        addUser = true;
                                }

                                if (addUser.Equals(true))
                                {
                                    SPUser ensuredActor;

                                    try
                                    {
                                        ensuredActor = Web.EnsureUser(user.LoginName);
                                    }
                                    catch
                                    {
                                        ensuredActor = Web.Site.RootWeb.EnsureUser(user.LoginName);
                                    }


                                    SPRoleAssignment roleAssignment = new SPRoleAssignment(ensuredActor);
                                    roleAssignment.RoleDefinitionBindings.Add(roleDefinitionRS);
                                    itemToEdit.RoleAssignments.Add(roleAssignment);
                                }
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        General.saveErrorsLog(wfid, "SetRSPermissionsForWFUsers() - For: " + ex.Message + " - Error Step ':" + wfSteps + "' - " + itemToEdit.Url);
                        continue;
                    }
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "(1) SetRSPermissionsForWFUsers: " + ex.Message + "' - Item to edit: " + itemToEdit.Url);
            }
        }

      
        /// <summary>
        /// Remove all user permission and grant them Read permissions.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="realEditor"></param>
        public static void RemoveStepResponsiblePermissions(ref SPListItem item, SPUser realEditor, string wfid, SPRoleDefinition roleDefinitionRead)
        {
            try
            {

                SPRoleAssignment roleAssignment = item.RoleAssignments.Cast<SPRoleAssignment>().FirstOrDefault(r => r.Member == realEditor);

                if (roleAssignment != null)
                {
                    roleAssignment.RoleDefinitionBindings.RemoveAll();
                    roleAssignment.RoleDefinitionBindings.Add(roleDefinitionRead);
                    roleAssignment.Update();
                }
                   

                item["Editor"] = realEditor;

                using (new DisabledItemEventsScope())
                {
                    item.Update();
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "RemoveStepResponsiblePermissions() - RealEditor: '" + realEditor.LoginName + "'. " + ex.Message);
            }
        }

        /// <summary>
        /// Break item role inheritance.
        /// </summary>
        /// <param name="item"></param>
        public static void ResetPermissions(ref SPListItem item, string wfid)
        {
            try
            {
                item.ResetRoleInheritance();
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "ResetPermissions " + ex.Message);
            }
        }

        //CR26 New Method
        /// <summary>
        /// Get user login name without domain
        /// </summary>
        /// <param name="userAccount"></param>
        /// <returns>Get user login name without domain. String.</returns>
        public static string GetOnlyUserAccount(string userAccount, string wfid)
        {
            string account = string.Empty;

            try
            {

                if (userAccount.Contains("\\"))
                    account = userAccount.Split('\\')[1];
                else
                    account = userAccount;

              
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetOnlyUserAccount " + ex.Message);
            }

            return account;
        }

        public static bool UserBelongToGroup(string domainName, string groupName, string loginName, string userAD, string passwordAD, string wfid, Dictionary<string,string> parameters, string stepNumber)
        {
            bool belong = false;

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, domainName, userAD, passwordAD))
                {
                    if (context != null)
                    {
                        groupName = GetOnlyUserAccount(groupName, wfid);

                        if (!string.IsNullOrEmpty(groupName))
                        {

                            //ESMA-CR28 - Nested Groups
                            using (UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(context, GetOnlyUserAccount(loginName, wfid)))
                            {

                                if (parameters["Nested Groups"].ToLower().Equals("false"))
                                    belong = userPrincipal.IsMemberOf(context, IdentityType.SamAccountName, groupName);
                                else
                                {
                                    using (PrincipalSearchResult<Principal> groups = userPrincipal.GetAuthorizationGroups())
                                    {
                                        return groups.OfType<GroupPrincipal>().Any(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
                                    }
                                }
                            }
                        }
                        else
                            General.saveErrorsLog(wfid, "UserBelongToGroup - Step: '" + stepNumber+ "' has the groupName NULL.");
                    }
                    else
                    {
                        General.saveErrorsLog(wfid, "UserBelongToGroup - Problems to connect AD. User: " + loginName);

                    }
                }
            }
            catch (System.DirectoryServices.DirectoryServicesCOMException ex)
            {
                General.saveErrorsLog(wfid, "UserBelongToGroup: " + groupName + " - Login: " + loginName + " - " + ex.Message);
            }

            return belong;
        }

        public static string GetActorStep1Assigned(string wfid, SPListItem item, SPWeb Web)
        {
            string account = string.Empty;
            string fieldName = "Step_x0020_1_x0020_Assigned_x0020_To";

            try
            {
                if (item[fieldName] != null)
                {

                    SPUser user = General.GetSPUser(item, fieldName, wfid, Web);
                    account = GetOnlyUserAccount(user.LoginName, wfid);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetActorStep1Assigned() " + ex.Message);
            }

            return account;
        }

        public static SPUser GetUserWithWindowsClaims(string wfid, string userAccount, SPWeb web)
        {
            SPUser userClaims = null;
            SPClaim userClaim = null;

            try
            {
                SPClaimProviderManager cpm = SPClaimProviderManager.Local;
                userClaim = cpm.ConvertIdentifierToClaim(userAccount, SPIdentifierTypes.WindowsSamAccountName);

                try
                {
                    userClaims = web.EnsureUser(userClaim.ToEncodedString());
                }
                catch
                {
                    userClaims = web.Site.RootWeb.EnsureUser(userClaim.ToEncodedString());
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetUserWithWindowsClaims-User() '" + userAccount + "'. Error in UserClaim: '" + userClaim + "'");
                General.saveErrorsLog(wfid, "GetUserWithWindowsClaims-User() " + ex.Message);
            }

            return userClaims;
        }

       
    }
}
