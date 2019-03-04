using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;

namespace ESMA.Paperless.EventsReceiver.v16
{
    public class PermissionsManagement
    {


        #region <LEVEL PERMISSIONS>

        /// <summary>
        /// Create minimum required role definitions (Full Control)
        /// </summary>
        public static void CreateRoleDefinitionsRSFullControl(SPWeb site, SPRoleDefinition roleFullControl)
        {
            try
            {
                string roleDefinitionName = "RS Full Control";

                if (!RoleDefinitionExistsInSite(roleDefinitionName, site))
                {

                    //Read copy
                    SPRoleDefinition newFullControlRD = new SPRoleDefinition(roleFullControl);
                    newFullControlRD.Name = roleDefinitionName;
                    newFullControlRD.Description = "Full control permissions for Routing Slip.";

                    if (roleFullControl.BasePermissions.ToString().Equals(newFullControlRD.BasePermissions.ToString()))
                    {
                        //Remove not desired base permissionss
                        newFullControlRD.BasePermissions &= ~SPBasePermissions.ManagePersonalViews;
                        newFullControlRD.BasePermissions &= ~SPBasePermissions.AddDelPrivateWebParts;
                        newFullControlRD.BasePermissions &= ~SPBasePermissions.UpdatePersonalWebParts;
                    }

                    site.RoleDefinitions.Add(newFullControlRD);
                    site.Update();
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("CreateRoleDefinitionsRSFullControl(): " + ex.Source, ex.Message);
            }
        }

        /// <summary>
        /// Create minimum required role definitions (Contribute)
        /// </summary>
        public static void CreateRoleDefinitionsRSContribute(SPWeb site, SPRoleDefinition roleContribute)
        {
            try
            {
                string roleDefinitionName = "RS Contribute";

                if (!RoleDefinitionExistsInSite(roleDefinitionName, site))
                {

                    //Read copy
                    SPRoleDefinition newContributeRD = new SPRoleDefinition(roleContribute);
                    newContributeRD.Name = roleDefinitionName;
                    newContributeRD.Description = "Contribution permissions with some extra restrictions.";

                    if (roleContribute.BasePermissions.ToString().Equals(newContributeRD.BasePermissions.ToString()))
                    {
                        //Remove not desired base permissionss
                        newContributeRD.BasePermissions &= ~SPBasePermissions.DeleteVersions;
                        newContributeRD.BasePermissions &= ~SPBasePermissions.BrowseDirectories;
                        newContributeRD.BasePermissions &= ~SPBasePermissions.CreateSSCSite;
                        newContributeRD.BasePermissions &= ~SPBasePermissions.BrowseUserInfo;
                        newContributeRD.BasePermissions &= ~SPBasePermissions.EditMyUserInfo;
                        newContributeRD.BasePermissions &= ~SPBasePermissions.ManagePersonalViews;
                        newContributeRD.BasePermissions &= ~SPBasePermissions.AddDelPrivateWebParts;
                        newContributeRD.BasePermissions &= ~SPBasePermissions.UpdatePersonalWebParts;
                    }

                    site.RoleDefinitions.Add(newContributeRD);
                    site.Update();
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("CreateRoleDefinitionsRSFullControl(): " + ex.Source, ex.Message);
            }
        }

        /// <summary>
        /// Create minimum required role definitions (Read)
        /// </summary>
        public static void CreateRoleDefinitionsRSRead(SPWeb site, SPRoleDefinition roleRead)
        {
            try
            {
                string roleDefinitionName = "RS Read";

                if (!RoleDefinitionExistsInSite(roleDefinitionName, site))
                {

                    //Read copy
                    SPRoleDefinition newReadRD = new SPRoleDefinition(roleRead);
                    newReadRD.Name = roleDefinitionName;
                    newReadRD.Description = "Limited contribute permissions with some extra restrictions for the Paperless.";

                    if (roleRead.BasePermissions.ToString().Equals(newReadRD.BasePermissions.ToString()))
                    {
                        //Remove not desired base permissionss
                        newReadRD.BasePermissions &= ~SPBasePermissions.CreateSSCSite;
                    }

                    site.RoleDefinitions.Add(newReadRD);
                    site.Update();
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("CreateRoleDefinitionsRSFullControl(): " + ex.Source, ex.Message);
            }
        }


        /// <summary>
        /// Create minimum required role definitions (Overwrite)
        /// </summary>
        public static void CreateRoleDefinitionsRSOverwrite(SPWeb site, SPRoleDefinition roleRead)
        {
            try
            {
                string roleDefinitionName = "RS Overwrite";

                if (!RoleDefinitionExistsInSite(roleDefinitionName, site))
                {

                    //Read copy
                    SPRoleDefinition newOverwriteRD = new SPRoleDefinition(roleRead);
                    newOverwriteRD.Name = roleDefinitionName;
                    newOverwriteRD.Description = "Limited contribute permissions with some extra restrictions for the Paperless.";

                    if (roleRead.BasePermissions.ToString().Equals(newOverwriteRD.BasePermissions.ToString()))
                    {
                        //Remove not desired base permissionss
                        newOverwriteRD.BasePermissions &= ~SPBasePermissions.CreateSSCSite;
                        newOverwriteRD.BasePermissions &= SPBasePermissions.AddListItems;
                        newOverwriteRD.BasePermissions &= SPBasePermissions.EditListItems;
                    }

                    site.RoleDefinitions.Add(newOverwriteRD);
                    site.Update();
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("CreateRoleDefinitions(): " + ex.Source, ex.Message);
            }
        }

        #endregion

        #region <LIST PERMISSIONS>

        public static void GrantListPermission(SPList list, SPWeb web, Dictionary<string, SPRoleDefinition> permissionsDictionary)
        {
            string groupName = string.Empty;

            try
            {

                list.ResetRoleInheritance();

                if (!list.HasUniqueRoleAssignments)
                    list.BreakRoleInheritance(false);

                foreach (KeyValuePair<String, SPRoleDefinition> kvp in permissionsDictionary)
                {
                    groupName = kvp.Key;
                    SPRoleDefinition roleDefinition = kvp.Value;

                    try
                    {
                        SPUser ensuredAdminGroup = GetSPUser(groupName, web);

                        //Grant full control permissions to administration group
                        if (ensuredAdminGroup != null)
                        {
                            SPRoleAssignment roleAssignment = new SPRoleAssignment(ensuredAdminGroup);
                            UpdatePermissions(roleAssignment, roleDefinition, list, web);
                        }
                        else
                            General.SaveErrorsLogArchitecture("GrantListPermission() - Group Name: " + groupName + " not exist.", null);
                    }
                    catch (Exception ex)
                    {
                        General.SaveErrorsLogArchitecture("GrantListPermission() - Group Name: " + groupName + " " + ex.Source, ex.Message);
                        continue; 
                    }
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("GrantListPermission() - List: " + list.Title + " " + ex.Source, ex.Message);
            }
        }

        /// <summary>
        /// Avoid any workflow library content edition by non administrator users.
        /// </summary>
        /// <param name="list"></param>
        public static void GrantReadPermission(SPList list, SPWeb web)
        {
            try
            {
                list.ResetRoleInheritance();
                if (!list.HasUniqueRoleAssignments)
                    list.BreakRoleInheritance(true);

                SPRoleDefinition roleDefinitionFullControl = web.Site.RootWeb.RoleDefinitions["Full Control"];
                SPRoleDefinition roleDefinitionRSRead = web.Site.RootWeb.RoleDefinitions["RS Read"];

                int count = 0;

                foreach (SPRoleAssignment roleAssignment in list.RoleAssignments)
                {
                    try
                    {
                        //All users are granted with just read permissions
                        if (!list.RoleAssignments[count].RoleDefinitionBindings.Contains(roleDefinitionFullControl))
                        {
                            list.RoleAssignments[count].RoleDefinitionBindings.RemoveAll();
                            list.RoleAssignments[count].RoleDefinitionBindings.Add(roleDefinitionRSRead);
                            list.RoleAssignments[count].Update();
                        }

                        count++;
                    }
                    catch (Exception ex)
                    {
                        General.SaveErrorsLogArchitecture("GrantReadPermission() - " + ex.Source, ex.Message); 
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("GrantReadPermission() - " + ex.Source, ex.Message);
            }
        }

        /// <summary>
        /// Add role to list
        /// </summary>
        protected static void UpdatePermissions(SPRoleAssignment roleAssignment, SPRoleDefinition roleDefinition, SPList list, SPWeb web)
        {
            try
            {
                roleAssignment.RoleDefinitionBindings.Add(roleDefinition);
                list.RoleAssignments.Add(roleAssignment);
                list.Update();
            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("UpdatePermissions() - " + ex.Source, ex.Message);
            }

        }

        #endregion

        #region <CHECKINGS>

        /// <summary>
        /// Check if the permission level exists in the Root Site
        /// </summary>
        /// <param name="roleDefinitionName"></param>
        /// <param name="site"></param>
        /// <returns></returns>
        protected static bool RoleDefinitionExistsInSite(string roleDefinitionName, SPWeb site)
        {
            bool exist = false;

            try
            {
                SPRoleDefinitionCollection roleDefinitionCol = site.RoleDefinitions;

                foreach (SPRoleDefinition item in roleDefinitionCol)
                {
                    if (item.Name.Equals(roleDefinitionName))
                    {
                        return true;

                    }
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("RoleDefinitionExistsInSite() - " + ex.Source, ex.Message);
            }

            return exist;

        }


        #endregion

        #region <USERS - GROUPS>

        public static string GetAdministratorGroup(Dictionary<string, string> parameters)
        {
            string adminGroup = string.Empty;

            try
            {
                if ((parameters.ContainsKey("RS Admin Group")) && (parameters.ContainsKey("Domain")))
                {
                    string rsAdminGroupValue = parameters["RS Admin Group"];
                    string domain = parameters["Domain"];

                    if ((!string.IsNullOrEmpty(rsAdminGroupValue)) && (!string.IsNullOrEmpty(domain)))
                    {

                        if (!rsAdminGroupValue.ToUpper().Contains(domain.ToUpper() + "\\"))
                            adminGroup = domain + "\\" + rsAdminGroupValue;
                        else
                            adminGroup = rsAdminGroupValue;

                    }
                }
                else
                    General.SaveErrorsLogArchitecture("GetAdministratorGroup()- Parameter `'RS Admin Group' does not exist in RS Configuration Parameters List.",null);

            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("GetAdministratorGroup() - " + ex.Source, ex.Message);
            }

            return adminGroup;

        }

        public static string GetAuditorsGroup(Dictionary<string, string> parameters)
        {
            string auditorGroup = string.Empty;

            try
            {
                if ((parameters.ContainsKey("RS Auditors Group")) && (parameters.ContainsKey("Domain")))
                {
                    string rsAuditorGroupValue = parameters["RS Auditors Group"];
                    string domain = parameters["Domain"];

                    if ((!string.IsNullOrEmpty(rsAuditorGroupValue)) && (!string.IsNullOrEmpty(domain)))
                    {

                        if (!rsAuditorGroupValue.ToUpper().Contains(domain.ToUpper() + "\\"))
                            auditorGroup = domain + "\\" + rsAuditorGroupValue;
                        else
                            auditorGroup = rsAuditorGroupValue;

                    }
                }

            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("GetAuditorsGroup() - " + ex.Source, ex.Message);
            }

            return auditorGroup;

        }

        public static List<string> GetPaperlessGroupList(Dictionary<string, string> parameters, SPWeb web, ref Dictionary<string, SPRoleDefinition> permissionsDictionary, SPRoleDefinition roleRSContributeControl, string auditorGroup)
        {
            List<string> paperlessGroupList = new List<string>();

            try
            {
                foreach (SPRoleAssignment roleAssignment in web.RoleAssignments)
                {
                    SPPrincipal member = roleAssignment.Member;
                    string groupName = member.Name;

                    if (groupName.ToLower().Contains(" visitors"))
                    {
                        SPGroup group = web.Groups.GetByName(groupName);

                        foreach (SPUser user in group.Users)
                        {
                            if ((!(user.LoginName.Contains(auditorGroup))) && (!permissionsDictionary.ContainsKey(user.LoginName)))
                                permissionsDictionary.Add(user.LoginName, roleRSContributeControl);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("GetPaperlessGroupList() - " + ex.Source, ex.Message);
            }

            return paperlessGroupList;

        }

        public static SPUser GetSPUser(string userLogin, SPWeb web)
        {
            SPUser user = null;

            try
            {

                try
                {
                    user = web.EnsureUser(userLogin);
                }
                catch
                {
                    user = web.Site.RootWeb.EnsureUser(userLogin);
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("GetSPUser() - '" + userLogin + "' " +  ex.Source, ex.Message);
            }

            return user;

        }

        #endregion



    }
}
