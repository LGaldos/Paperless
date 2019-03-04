
using System;
using System.Linq;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Workflow;
using System.Collections.Generic;
using ESMA.Paperless.EventsReceiver.v16.EventsReceiver;

namespace ESMA.Paperless.EventsReceiver.v16.EventsReceiver.WorkflowConfigurationEventReceiver
{
    /// <summary>
    /// List Item Events
    /// </summary>
    public class WorkflowConfigurationEventReceiver : SPItemEventReceiver
    {
        /// <summary>
        /// An item is being added.
        /// </summary>
        public override void ItemAdding(SPItemEventProperties properties)
        {
            base.ItemAdding(properties);
            
            try
            {
                // INICIO Prueba de Luis
                EventFiringEnabled = false; // Evita llamada recursiva del evento
                // FIN Prueba de Luis 

                int userID = properties.CurrentUserId;
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                   
                    using (SPSite elevatedSite = new SPSite(properties.SiteId))
                    {
                        using (SPWeb Web = elevatedSite.OpenWeb(properties.Web.ID))
                        {
                            //SPUser user = site.OpenWeb().SiteUsers.GetByID(userID);

                            if (properties.AfterProperties["Title"] != null)
                            {
                                try
                                {
                                    SPList list = Web.Lists["RS Workflow Configuration"];
                                    SPQuery query = new SPQuery();
                                    query.Query = "<Where><Or><Eq><FieldRef Name='Title'/><Value Type='Text'>" + properties.AfterProperties["Title"] + "</Value></Eq><Eq><FieldRef Name='WFOrder'/><Value Type='Text'>" + properties.AfterProperties["WFOrder"] + "</Value></Eq></Or></Where>";
                                    SPListItemCollection itemCollection = list.GetItems(query);

                                    //If workflow definition is unique
                                    if (itemCollection.Count.Equals(0))
                                    {
                                        //Configuration Parameters
                                        Dictionary<string, string> parameters = General.GetConfigurationParameters(Web);

                                        //Create workflow library
                                        Guid libGUID = Web.Lists.Add(properties.AfterProperties["Title"].ToString() + " Library", string.Empty, properties.Web.ListTemplates["RS Workflow Library"]);
                                        properties.Web.Update();
                                        properties.AfterProperties["WFLibraryURL"] = Web.Lists[libGUID].DefaultViewUrl;

                                        //Change default permissions for workflow library
                                        ChangePermissionAssignmentFromWFLib(Web.Lists[libGUID], Web, properties);

                                        //Create workflow log
                                        Guid logGUID = Web.Lists.Add(properties.AfterProperties["Title"].ToString() + " Log", string.Empty, Web.ListTemplates["RS Workflow Log"]);
                                        Web.Update();
                                        properties.AfterProperties["WFLogURL"] = Web.Lists[logGUID].DefaultViewUrl;

                                        //Change default permissions for workflow log
                                        ChangePermissionAssignmentFromLogsList(Web.Lists[logGUID], properties, parameters, Web);

                                        //Add additional required fields to workflow library
                                        SPList docLibrary = Web.Lists[libGUID];
                                        AddLookedUpFields(properties, libGUID, Web);

                                        SPFieldChoice choices = new SPFieldChoice(Web.Lists[libGUID].Fields, "DocumentationType");

                                        foreach (string choice in choices.Choices)
                                        {
                                            if (choice != "(Empty)")
                                                CreateCustomLibraryView(docLibrary, choice, properties);
                                        }

                                        //Update Content Types Configuration
                                        string contentTypeName = Web.ContentTypes["Workflow Document"].Name;

                                        if (contentTypeName != null)
                                            UpdateContentTypesConfiguration(docLibrary, contentTypeName, properties, properties.Web);

                                        //Update Field Property


                                    }
                                    else
                                    {
                                        properties.ErrorMessage = "There are multiple workflow definitions with the same title or with the same workflow order in Workflow Configuration list.";
                                        properties.Status = SPEventReceiverStatus.CancelWithError;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    properties.ErrorMessage = ex.Message;
                                    properties.Status = SPEventReceiverStatus.CancelWithError;
                                    classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - ItemAdding() " + ex.Message);
                                }
                            }
                        }
                    }
                });

                // INICIO Prueba de Luis
                EventFiringEnabled = true;
                // FIN Prueba de Luis 
            }
            catch (Exception ex)
            {
                properties.ErrorMessage = ex.Message;
                properties.Status = SPEventReceiverStatus.CancelWithError;
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - ItemAdding() " + ex.Message);
            }
        }

        /// <summary>
        /// An item is being updated.
        /// </summary>
        public override void ItemUpdating(SPItemEventProperties properties)
        {
            base.ItemUpdating(properties);
            
            try
            {
                // INICIO Prueba de Luis
                EventFiringEnabled = false;
                // FIN Prueba de Luis 

                int userID = properties.CurrentUserId;
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite elevatedSite = new SPSite(properties.SiteId))
                    {
                        using (SPWeb Web = elevatedSite.OpenWeb(properties.Web.ID))
                        {
                            SPUser user = Web.SiteUsers.GetByID(userID);

                            if (properties.AfterProperties["Title"] != null && properties.ListItem["Title"] != null && !properties.ListItem["Title"].ToString().ToUpper().Equals(properties.AfterProperties["Title"].ToString().ToUpper()))
                            {
                                SPList list = Web.Lists["RS Workflow Configuration"];
                                SPQuery query = new SPQuery();
                                query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + properties.AfterProperties["Title"] + "</Value></Eq></Where>";
                                SPListItemCollection itemCollection = list.GetItems(query);

                                //If workflow definition is not unique
                                if (!itemCollection.Count.Equals(0))
                                {
                                    properties.ErrorMessage = "There are multiple workflow definitions with the same title in Workflow Configuration list.";
                                    properties.Status = SPEventReceiverStatus.CancelWithError;
                                }
                            }
                   

                            //Add new general fields
                            AddLookedUpFields(properties, Web);
                            SetWorkflowFirstStep(properties, Web);
                        }
                    }
                    
                });

                // INICIO Prueba de Luis
                EventFiringEnabled = true;
                // FIN Prueba de Luis 
            }
            catch (Exception ex)
            {
                properties.ErrorMessage = ex.Message;
                properties.Status = SPEventReceiverStatus.CancelWithError;
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - ItemUpdating() " + ex.Message);
            }

        }


        #region <PERMISSIONS>
        /// <summary>
        /// Avoid any workflow library content edition by non administrator users.
        /// </summary>
        /// <param name="list"></param> 
        protected void ChangePermissionAssignmentFromWFLib(SPList list, SPWeb Web, SPItemEventProperties properties)
        {
            try
            {
                // INICIO Prueba de Luis
                EventFiringEnabled = false;
                // FIN Prueba de Luis 

                list.ResetRoleInheritance();
                
                if (!list.HasUniqueRoleAssignments)
                    list.BreakRoleInheritance(true);

                SPRoleDefinition roleDefinitionRSFullControl;
                SPRoleDefinition roleDefinitionFullControl;
                SPRoleDefinition roleDefinitionRSRead;

                roleDefinitionRSFullControl = Web.Site.RootWeb.RoleDefinitions["RS Full Control"];
                roleDefinitionFullControl = Web.Site.RootWeb.RoleDefinitions["Full Control"];
                roleDefinitionRSRead = Web.Site.RootWeb.RoleDefinitions["RS Read"];


                foreach (SPRoleAssignment roleAssignment in list.RoleAssignments)
                {
                    try
                    {
                        //All users are granted with just read permissions
                        if ((!roleAssignment.RoleDefinitionBindings.Contains(roleDefinitionRSFullControl)) && (!roleAssignment.RoleDefinitionBindings.Contains(roleDefinitionFullControl)))
                        {
                            roleAssignment.RoleDefinitionBindings.RemoveAll();
                            roleAssignment.RoleDefinitionBindings.Add(roleDefinitionRSRead);
                            roleAssignment.Update();
                        }

                    }
                    catch { continue; }
                }

                // INICIO Prueba de Luis
                EventFiringEnabled = true;
                // FIN Prueba de Luis 
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - ChangePermissionAssignmentFromWFLib() " + ex.Message);
            }
        }

        /// <summary>
        /// Remove all permissions and grant Full Control permissions to administration groups
        /// </summary>
        /// <param name="list"></param>
        protected void ChangePermissionAssignmentFromLogsList(SPList list, SPItemEventProperties properties, Dictionary<string, string> parameters, SPWeb Web)
        {
            try
            {
                //Get Information from Configuration Parameters List
                string adminGroup = PermissionsManagement.GetAdministratorGroup(parameters);
                SPRoleDefinition roleRSFullControl = properties.Site.RootWeb.RoleDefinitions["RS Full Control"];

                Dictionary<string, SPRoleDefinition> permissionsDictionary = new Dictionary<string, SPRoleDefinition>();
                permissionsDictionary.Add(adminGroup, roleRSFullControl);

                GrantListPermission(list, Web, permissionsDictionary, properties);
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - ChangePermissionAssignmentFromLogsList() " + ex.Message);
            }
        }

        public static void GrantListPermission(SPList list, SPWeb web, Dictionary<string, SPRoleDefinition> permissionsDictionary, SPItemEventProperties properties)
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
                        SPUser ensuredAdminGroup = GetSPUser(groupName, web, properties);

                        //Grant full control permissions to administration group
                        if (ensuredAdminGroup != null)
                        {
                            SPRoleAssignment roleAssignment = new SPRoleAssignment(ensuredAdminGroup);
                            UpdatePermissions(roleAssignment, roleDefinition, list, web, properties);
                        }
                        else
                            classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - GrantListPermission() ");
                    }
                    catch (Exception ex)
                    {
                        classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - GrantListPermission() -Group: '" + groupName + ex.Message); 
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - GrantListPermission() - List: '" + list.Title + ex.Message); 
            }
        }

        protected static void UpdatePermissions(SPRoleAssignment roleAssignment, SPRoleDefinition roleDefinition, SPList list, SPWeb web, SPItemEventProperties properties)
        {
            try
            {
                roleAssignment.RoleDefinitionBindings.Add(roleDefinition);
                list.RoleAssignments.Add(roleAssignment);
                list.Update();
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - UpdatePermissions() " + ex.Message); 
            }

        }

        public static SPUser GetSPUser(string userLogin, SPWeb web, SPItemEventProperties properties)
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
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - GetSPUser() ");
            }

            return user;

        }

        #endregion

        #region <DL SETTINGS>

        /// <summary>
        /// Add new fields to workflow library
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="libGUID"></param>
        protected void AddLookedUpFields(SPItemEventProperties properties, Guid libGUID, SPWeb Web)
        {
            try
            {
                if (properties.List.Fields.ContainsField("Fields to Add"))
                {
                    SPField fieldsToAddField = properties.List.Fields.GetField("Fields to Add");
                    if (properties.AfterProperties[fieldsToAddField.InternalName] != null)
                    {
                        SPFieldLookupValueCollection lookups = new SPFieldLookupValueCollection(properties.AfterProperties[fieldsToAddField.InternalName].ToString());

                        foreach (SPFieldLookupValue value in lookups)
                        {
                            if (properties.Web.Fields.ContainsField(value.LookupValue) || properties.Web.Site.RootWeb.Fields.ContainsField(value.LookupValue))
                            {
                                SPList lib = Web.Lists[libGUID];
                                if (!lib.ContentTypes["Workflow"].Fields.ContainsField(value.LookupValue))
                                {
                                    SPField fieldAux;
                                    if (properties.Web.Fields.ContainsField(value.LookupValue))
                                        fieldAux = GetFieldInRSGroup(Web, value.LookupValue, properties);
                                    //fieldAux = properties.Web.Fields.GetField(value.LookupValue);
                                    else
                                        fieldAux = GetFieldInRSGroup(Web.Site.RootWeb, value.LookupValue, properties);
                                    //fieldAux = properties.Web.Site.RootWeb.Fields.GetField(value.LookupValue);

                                    SPFieldLink fieldLink = new SPFieldLink(fieldAux);
                                    lib.ContentTypes["Workflow"].FieldLinks.Add(fieldLink);
                                    lib.ContentTypes["Workflow"].Update();
                                    lib.Update();
                                    SPView view = Web.Lists[libGUID].DefaultView;
                                    view.ViewFields.Add(Web.Lists[libGUID].ContentTypes["Workflow"].Fields.GetField(fieldAux.InternalName));
                                    view.Update();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - AddLookedUpFields() " + ex.Message);
            }
        }

        /// <summary>
        /// Add new fields to workflow library
        /// </summary>
        /// <param name="properties"></param>
        protected void AddLookedUpFields(SPItemEventProperties properties, SPWeb Web)
        {
            try
            {

                if (properties.AfterProperties["WFLibraryURL"] != null)
                {
                    SPList lib = Web.GetListFromUrl(properties.AfterProperties["WFLibraryURL"].ToString());
                    
                    if (properties.List.Fields.ContainsField("Fields to Add"))
                    {
                        SPField fieldsToAddField = properties.List.Fields.GetField("Fields to Add");
                        if (properties.AfterProperties[fieldsToAddField.InternalName] != null)
                        {
                            SPFieldLookupValueCollection lookups = new SPFieldLookupValueCollection(properties.AfterProperties[fieldsToAddField.InternalName].ToString());

                            foreach (SPFieldLookupValue value in lookups)
                            {
                                if (Web.Fields.ContainsField(value.LookupValue) || Web.Site.RootWeb.Fields.ContainsField(value.LookupValue))
                                {
                                    if (!lib.ContentTypes["Workflow"].Fields.ContainsField(value.LookupValue))
                                    {
                                        SPField fieldAux;
                                        if (Web.Fields.ContainsField(value.LookupValue))
                                            fieldAux = GetFieldInRSGroup(Web, value.LookupValue, properties);
                                        else
                                            fieldAux = GetFieldInRSGroup(Web.Site.RootWeb, value.LookupValue, properties);
                                           
                                        SPFieldLink fieldLink = new SPFieldLink(fieldAux);
                                        lib.ContentTypes["Workflow"].FieldLinks.Add(fieldLink);
                                        lib.ContentTypes["Workflow"].Update();
                                        lib.Update();
                                        SPView view = lib.DefaultView;
                                        view.ViewFields.Add(lib.ContentTypes["Workflow"].Fields.GetField(fieldAux.InternalName));
                                        view.Update();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - AddLookedUpFields() " + ex.Message);
            }
        }


        /// <summary>
        /// Create custom library view needed in workflow user interface
        /// </summary>
        /// <param name="list"></param>
        /// <param name="wfid"></param>
        /// <param name="docType"></param>
        /// <param name="userID"></param>
        /// <param name="isRejecting"></param>
        /// <param name="isWebsio"></param>
        /// <returns>SPView object of the SharePoint view to be used in one of the workflow user interface tabs</returns>
        protected void CreateCustomLibraryView(SPList list, string docType, SPItemEventProperties properties)
        {
            string newViewName = docType;
           
            try
            {
                SPView view = null;
                try
                {
                    view = list.Views[newViewName];
                }
                catch
                {
                    System.Collections.Specialized.StringCollection strColl = new System.Collections.Specialized.StringCollection();
                    view = list.Views.Add(newViewName, strColl, string.Empty, 100, true, false, SPViewCollection.SPViewType.Html, false);
                }                    

                try
                {
                    SPField fieldType = list.Fields.GetFieldByInternalName("DocIcon");
                    SPField fieldName = list.Fields.GetFieldByInternalName("LinkFilename");
                    fieldName.ListItemMenu = true;
                    fieldName.ListItemMenuAllowed = SPField.ListItemMenuState.Required;

                    SPField fieldVersion = list.Fields.GetFieldByInternalName("_UIVersionString");
                    SPField fieldModifiedBy = list.Fields.GetFieldByInternalName("Editor");

                    view.ViewFields.DeleteAll();
                    view.Update();

                    if (list.Fields.ContainsFieldWithStaticName("WFDocumentPreview"))
                    {
                        SPField fieldWebsio = list.Fields.GetFieldByInternalName("WFDocumentPreview");
                        view.ViewFields.Add(fieldWebsio);
                    }

                    view.TabularView = false;
                    view.RenderAsHtml();
                    view.ViewFields.Add(fieldType);
                    view.ViewFields.Add(fieldName);
                    view.ViewFields.Add(fieldVersion);
                    view.ViewFields.Add(fieldModifiedBy);

                    view.Query = "<OrderBy><FieldRef Name='FileLeafRef' Ascending='True' /></OrderBy><QueryOptions><ViewAttributes Scope='RecursiveAll' /></QueryOptions>";
                    view.Scope = SPViewScope.RecursiveAll;
                    view.Update();
                
                }
                catch (Exception ex)
                {
                    classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - CreateCustomLibraryView() - Updatimg View: '" + newViewName + "'." + ex.Message);
                }
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - CreateCustomLibraryView(() " + ex.Message);
            }
        }

        /// <summary>
        /// Set workflow first step responsible group
        /// </summary>
        /// <param name="properties"></param>
        public void SetWorkflowFirstStep(SPItemEventProperties properties, SPWeb Web)
        {
            try
            {
                SPList stepDefinitions = Web.Lists["RS Workflow Step Definitions"];

                SPQuery query = new SPQuery();
                query.Query = "<Where><And><Eq><FieldRef Name='Title'/><Value Type='Text'>" + properties.AfterProperties["Title"] + "</Value></Eq><Eq><FieldRef Name='StepNumber'/><Value Type='Text'>" + "1" + "</Value></Eq></And></Where>";
                SPListItemCollection stepCol = stepDefinitions.GetItems(query);

                if (stepCol != null)
                {
                    SPListItem newStep = stepCol[0];
                    newStep["Title"] = properties.AfterProperties["Title"];
                    newStep["StepNumber"] = 1;
                    newStep["WFGroup"] = properties.AfterProperties["WFGroup"];
                    newStep.Update();
                    using (new DisabledItemEventsScope())
                    {
                        stepDefinitions.Update();
                    }

              
                }
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - SetWorkflowFirstStep() " + ex.Message);
            }
        }

        protected SPField GetFieldInRSGroup(SPWeb web, string fieldName, SPItemEventProperties properties)
        {
            try
            {
                foreach (SPField field in web.Fields)
                {
                    if (field.Group.Equals("RS Columns") && field.Title.Trim().ToLower().Equals(fieldName.Trim().ToLower()))
                    {
                        return field;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - GetFieldInRSGroup() " + ex.Message);
                return null;
            }
        }

        protected void UpdateContentTypesConfiguration(SPList docLibrary, string contentTypeName, SPItemEventProperties properties, SPWeb web)
        {
            try
            {
                IList<SPContentType> cTypes = new List<SPContentType>();
                SPFolder root = docLibrary.RootFolder;
                cTypes = root.ContentTypeOrder;
                SPContentType cType = cTypes.SingleOrDefault(hd => hd.Name == contentTypeName);
                List<int> indexList = new List<int>();
                int totalCTs = cTypes.Count;
               
                if (cType != null)
                {
                    for (int i = 0; i < totalCTs; i++)
                    {
                        cTypes.RemoveAt(0);
                    }

                    cTypes.Add(cType);
                    

                    using (new DisabledItemEventsScope())
                    {
                        try
                        {
                            web.AllowUnsafeUpdates = true;
                            root.UniqueContentTypeOrder = cTypes;
                            root.Update();
                        }
                        catch (Exception ex)
                        {
                            classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - UpdateContentTypesConfiguration() - DocLib: " + root.Name + " - " + ex.Message);
                        }
                        finally
                        {
                            web.AllowUnsafeUpdates = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - UpdateContentTypesConfiguration() " + ex.Message);
            }
        }

        protected void UpdateFieldVisibilityProperties(SPList docLibrary, SPItemEventProperties properties, SPWeb web, string columName)
        {
            try
            {
                SPField fieldDocType = properties.List.Fields.GetField("DocumentationType");

                fieldDocType.ShowInNewForm = true;
                fieldDocType.ShowInEditForm = true;
                fieldDocType.ShowInViewForms = true;
                
                using (new DisabledItemEventsScope())
                {
                    try
                    {
                        web.AllowUnsafeUpdates = true;
                        fieldDocType.Update();
                        docLibrary.Update();
                    }
                    catch (Exception ex)
                    {
                        classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - UpdateFieldVisibilityProperties() - DocLib: " + docLibrary.Title + " - " + ex.Message);
                    }
                    finally
                    {
                        web.AllowUnsafeUpdates = false;
                    }
                }


            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowConfigurationEventReceiver - UpdateFieldVisibilityProperties() " + ex.Message);
            }
        }

    

        #endregion
    }
}