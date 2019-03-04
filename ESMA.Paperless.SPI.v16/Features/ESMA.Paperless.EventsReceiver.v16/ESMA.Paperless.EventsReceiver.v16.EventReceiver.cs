using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.SharePoint;
using ESMA.Paperless.EventsReceiver.v16;
using System.Collections.Generic;

namespace ESMA.Paperless.SPI.v15.Features.ESMA.Paperless.EventsReceiver.v16
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>

    [Guid("556230c0-d24c-4c85-bf6d-5791a5745bf6")]
    public class ESMAPaperlessEventsReceiverEventReceiver : SPFeatureReceiver
    {
        // Uncomment the method below to handle the event raised after a feature has been activated.

        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            SPSite site = SPContext.Current.Site;

            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite elevatedSite = new SPSite(site.ID))
                    {
                        SPWeb web = elevatedSite.RootWeb;
                        web.AllowUnsafeUpdates = true;

                       //Configuration Parameters
                        Dictionary<string, string> parameters = General.GetConfigurationParameters(web);

                        //Regional Settings
                        SharePointSettings.ChangeCulture(web);
                        //Disable Access Request
                        SharePointSettings.DisableRequestAccess(web);
                        //Disable Sync Option
                        SharePointSettings.DisableSyncOption(web);

                        //Permissions
                        PermissionManagementModule(elevatedSite, web, parameters);

                        //Updating of CustomUploadPage
                        SharePointSettings.UpdateUploadPage(web);

                        web.AllowUnsafeUpdates = false;
                    }
                });
            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("FeatureActivated() - " + ex.Source, ex.Message);
            }
        }

        protected void PermissionManagementModule(SPSite site, SPWeb web, Dictionary<string, string> parameters)
        {
            try
            {
               

                SPRoleDefinition roleFullControl = site.RootWeb.RoleDefinitions["Full Control"];
                SPRoleDefinition roleContribute = site.RootWeb.RoleDefinitions["Contribute"];
                SPRoleDefinition roleRead = site.RootWeb.RoleDefinitions["Read"];

                //------------------------------------------------------------------------------
                //Create custom roles
                //------------------------------------------------------------------------------
                //RS Full Control
                PermissionsManagement.CreateRoleDefinitionsRSFullControl(web, roleFullControl);
                //RS Contribute
                PermissionsManagement.CreateRoleDefinitionsRSContribute(web, roleContribute);
                //RS Read
                PermissionsManagement.CreateRoleDefinitionsRSRead(web, roleRead);
                //RS Overwrite
                PermissionsManagement.CreateRoleDefinitionsRSOverwrite(web, roleRead);


                SPRoleDefinition roleRSFullControl = site.RootWeb.RoleDefinitions["RS Full Control"];
                SPRoleDefinition roleRSContribute = site.RootWeb.RoleDefinitions["RS Contribute"];
                SPRoleDefinition roleRSRead = site.RootWeb.RoleDefinitions["RS Read"];


                //Get Information fro Configuration Parameters List
                string adminGroup = PermissionsManagement.GetAdministratorGroup(parameters);
                string auditorGroup = PermissionsManagement.GetAuditorsGroup(parameters);


                //RS Admin Group (RS Full Control)
                GroupOfLists1(roleRSFullControl, web, adminGroup);

                //ReportsLibrary (Admin (RS Full Control) + Paperless Group (RS Contribute) + Auditors (RS Read))
                GroupOfLists2(roleRSFullControl, roleRSContribute, roleRSRead, web, adminGroup, auditorGroup, parameters);

                
            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("PermissionManagementModule() - " + ex.Source, ex.Message);
            }
        }

        //Routing Slip administration group (RS FC)
        protected void GroupOfLists1(SPRoleDefinition roleRSFullControl, SPWeb web, string adminGroup)
        {
            try
            {
                
                
                Dictionary<string, SPRoleDefinition> permissionsDictionary = new Dictionary<string, SPRoleDefinition>();
                permissionsDictionary.Add(adminGroup, roleRSFullControl);

                //Custom Lists
                SPList WFConfigurationList = web.GetListFromWebPartPageUrl(web.Url + "/Lists/WFConfiguration/AllItems.aspx");
                SPList WFStepDefinitionsList = web.GetListFromWebPartPageUrl(web.Url + "/Lists/WFStepDefinitions/AllItems.aspx");
                SPList WFConfigParametersList = web.GetListFromWebPartPageUrl(web.Url + "/Lists/WFConfigParameters/AllItems.aspx");
                SPList WFGeneralFieldsList = web.GetListFromWebPartPageUrl(web.Url + "/Lists/WFGeneralFields/AllItems.aspx");
                SPList errorLogList = web.GetListFromWebPartPageUrl(web.Url + "/Lists/ErrorLog/AllItems.aspx");

                List<SPList> lists = new List<SPList>();
                lists.Add(WFConfigurationList);
                lists.Add(WFStepDefinitionsList);
                lists.Add(WFConfigParametersList);
                lists.Add(WFGeneralFieldsList);
                lists.Add(errorLogList);


                foreach (SPList list in lists)
                {
                    PermissionsManagement.GrantListPermission(list, web, permissionsDictionary);
                }

            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("GroupOfLists1() - " + ex.Source, ex.Message);
            }
        }

        //Routing Slip administration group (RS FC) + Auditors (RS Contribute) + Paperless Group (RS Read)
        protected void GroupOfLists2(SPRoleDefinition roleRSFullControl, SPRoleDefinition roleRSContributeControl, SPRoleDefinition roleRSReadControl, SPWeb web, string adminGroup, string auditorGroup, Dictionary<string, string> parameters)
        {
            try
            {


                Dictionary<string, SPRoleDefinition> permissionsDictionary = new Dictionary<string, SPRoleDefinition>();
                permissionsDictionary.Add(adminGroup, roleRSFullControl);
                permissionsDictionary.Add(auditorGroup, roleRSReadControl);

                PermissionsManagement.GetPaperlessGroupList(parameters, web, ref permissionsDictionary, roleRSContributeControl, auditorGroup);

                //Custom Lists
                SPList reportsLibrary = web.GetListFromWebPartPageUrl(web.Url + "/Lists/ReportsLibrary/Forms/AllItems.aspx");

                List<SPList> lists = new List<SPList>();
                lists.Add(reportsLibrary);


                foreach (SPList list in lists)
                {
                    PermissionsManagement.GrantListPermission(list, web, permissionsDictionary);
                }

            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("GroupOfLists2() - " + ex.Source, ex.Message);
            }
        }

    }
}
