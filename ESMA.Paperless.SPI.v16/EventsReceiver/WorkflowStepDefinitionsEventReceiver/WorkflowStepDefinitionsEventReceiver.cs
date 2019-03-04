using System;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Workflow;
using System.Collections;
using System.Collections.Generic;

namespace ESMA.Paperless.EventsReceiver.v16.EventsReceiver.WorkflowStepDefinitionsEventReceiver
{
    /// <summary>
    /// List Item Events
    /// </summary>
    public class WorkflowStepDefinitionsEventReceiver : SPItemEventReceiver
    {
        /// <summary>
        /// An item is being added.
        /// </summary>
        public override void ItemAdding(SPItemEventProperties properties)
        {
            base.ItemAdding(properties);
            try
            {
                int userID = properties.CurrentUserId;
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite elevatedSite = new SPSite(properties.SiteId))
                    {
                        using (SPWeb Web = elevatedSite.OpenWeb(properties.Web.ID))
                        {
                            SPUser user = Web.SiteUsers.GetByID(userID);
                            List<string> newStepsList = new List<string>();

                            if (properties.AfterProperties["StepNumber"] != null && properties.AfterProperties["Title"] != null)
                            {
                                string stepNumberAux = properties.AfterProperties["StepNumber"].ToString();


                                if (!int.Parse(stepNumberAux).Equals(1))
                                {
                                    //If step number is not the first one
                                    EditStepNumbers(properties, stepNumberAux, false, ref newStepsList);

                                }
                                else
                                {
                                    //If step definition is the first one and is unique
                                    SPQuery query = new SPQuery();
                                    query.Query = "<Where><And>"
                                        + "<Eq><FieldRef Name='StepNumber'/><Value Type='Text'>" + stepNumberAux + "</Value></Eq>"
                                        + "<Eq><FieldRef Name='Title'/><Value Type='Text'>" + properties.AfterProperties["Title"] + "</Value></Eq>"
                                        + "</And></Where>";
                                    SPListItemCollection stepCol = properties.List.GetItems(query);

                                    if (stepCol != null && stepCol.Count > 0)
                                    {
                                        stepNumberAux = "-1";
                                        properties.ErrorMessage = "Wrong step number. The first step of this workflow already exists.";
                                        properties.Status = SPEventReceiverStatus.CancelWithError;
                                    }
                                    else
                                    {
                                        if (!newStepsList.Contains(stepNumberAux))
                                            newStepsList.Add(stepNumberAux);
                                    }
                                }

                                if (!stepNumberAux.Equals("-1"))
                                    CreateStepInLibraryModule(properties, newStepsList, Web);
                            }
                        }
                    }
                    
                });
            }
            catch (Exception ex)
            {
                //ErrorMessage = ex.Message;
                properties.Status = SPEventReceiverStatus.CancelWithError;
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowStepDefinitionsEventReceiver- ItemAdding() " + ex.Message);
            }
        }

        /// <summary>
        /// An item is being updated.
        /// </summary>
        public override void ItemUpdating(SPItemEventProperties properties)
        {
            base.ItemAdding(properties);
            try
            {
                int userID = properties.CurrentUserId;
                string currentStepValue = properties.ListItem["StepNumber"].ToString();
                

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite elevatedSite = new SPSite(properties.SiteId))
                    {
                        using (SPWeb Web = elevatedSite.OpenWeb(properties.Web.ID))
                        {

                            SPUser user = Web.SiteUsers.GetByID(userID);
                            List<string> newStepsList = new List<string>();


                            if (properties.AfterProperties["StepNumber"] != null && properties.AfterProperties["Title"] != null)
                            {
                                string stepNumberAux = properties.AfterProperties["StepNumber"].ToString();

                                if (!stepNumberAux.Equals(currentStepValue))
                                {

                                    //If step number is not the first one
                                    EditStepNumbers(properties, stepNumberAux, false, ref newStepsList);
                                    CreateStepInLibraryModule(properties, newStepsList, Web);
                                }
                            }
                        }

                    }

                });
            }
            catch (Exception ex)
            {
                properties.ErrorMessage = ex.Message;
                properties.Status = SPEventReceiverStatus.CancelWithError;
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowStepDefinitionsEventReceiver- ItemUpdating() " + ex.Message);
            }
        }

        /// <summary>
        /// Step definition deletion
        /// </summary>
        public override void ItemDeleting(SPItemEventProperties properties)
        {
            try
            {
                int userID = properties.CurrentUserId;
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite site = new SPSite(properties.Web.Site.Url))
                    {
                        SPUser user = site.OpenWeb().SiteUsers.GetByID(userID);
                        List<string> newStepsList = new List<string>();
                        
                        if (properties.ListItem["StepNumber"] != null && properties.ListItem["Title"] != null)
                        {
                            string stepNumberAux = properties.ListItem["StepNumber"].ToString();

                            if (!int.Parse(stepNumberAux).Equals(1))
                            {
                                //Change other steps step number
                                EditStepNumbers(properties, stepNumberAux, true, ref newStepsList);
                            }
                            else
                            {
                                SPQuery query = new SPQuery();
                                query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + properties.ListItem["Title"].ToString() + "</Value></Eq></Where>";
                                SPListItemCollection stepCol = properties.List.GetItems(query);

                                //Avoid first step deletion if the other steps have not been deleted
                                if (stepCol != null && stepCol.Count > 1)
                                {
                                    stepNumberAux = "-1";
                                    properties.ErrorMessage = "Please, remove the other steps before delete the first step.";
                                    properties.Status = SPEventReceiverStatus.CancelWithError;
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                properties.ErrorMessage = ex.Message;
                properties.Status = SPEventReceiverStatus.CancelWithError;
            }
        }


        /// <summary>
        /// Change the step numbers for the workflow type
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="stepNumberAux"></param>
        /// <param name="isDeletion"></param>
        protected void EditStepNumbers(SPItemEventProperties properties, string stepNumberAux, bool isDeletion, ref List<string> newStepsList)
        {
            try
            {
                newStepsList.Add(stepNumberAux.ToString()); //We add the step added from UI

                SPQuery query = new SPQuery();
                if (isDeletion)
                    query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + properties.ListItem["Title"] + "</Value></Eq></Where><OrderBy><FieldRef Name='StepNumber' Ascending='TRUE' /></OrderBy>";
                else
                    query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + properties.AfterProperties["Title"] + "</Value></Eq></Where><OrderBy><FieldRef Name='StepNumber' Ascending='TRUE' /></OrderBy>";
                SPListItemCollection stepCol = properties.List.GetItems(query);

                SPQuery query2 = new SPQuery();
                if (isDeletion)
                    query2.Query = "<Where><And><Eq><FieldRef Name='Title'/><Value Type='Text'>" + properties.ListItem["Title"] + "</Value></Eq><Eq><FieldRef Name='StepNumber'/><Value Type='Text'>" + properties.ListItem["StepNumber"] + "</Value></Eq></And></Where>";
                else
                    query2.Query = "<Where><And><Eq><FieldRef Name='Title'/><Value Type='Text'>" + properties.AfterProperties["Title"] + "</Value></Eq><Eq><FieldRef Name='StepNumber'/><Value Type='Text'>" + properties.AfterProperties["StepNumber"] + "</Value></Eq></And></Where>";
                SPListItemCollection stepCol2 = properties.List.GetItems(query2);

                if (stepCol != null && stepCol.Count > 0 && stepCol2.Count > 0)
                {
                    foreach (SPListItem step in stepCol)
                    {
                        try
                        {
                            this.EventFiringEnabled = false;
                            
                            if (int.Parse(step["StepNumber"].ToString()) >= int.Parse(stepNumberAux))
                            {
                                int stepNumberToEdit = int.Parse(step["StepNumber"].ToString());
                                
                                if (isDeletion)
                                    stepNumberToEdit--;
                                else
                                {
                                    stepNumberToEdit++;

                                     if (!newStepsList.Contains(stepNumberToEdit.ToString()))
                                        newStepsList.Add(stepNumberToEdit.ToString());
                                }
                                
                                step["StepNumber"] = stepNumberToEdit.ToString();
                                step.Update();

                            }
                            
                            this.EventFiringEnabled = true;
                        }
                        catch
                        {
                            this.EventFiringEnabled = true;
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowStepDefinitionsEventReceiver - EditStepNumbers() " + ex.Message);
            }
        }

        

        /// <summary>
        /// Create step reference in proper workflow library
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="stepNumberAux"></param>
        protected void CreateStepInLibraryModule(SPItemEventProperties properties, List<string> newStepsList, SPWeb Web)
        {
            try
            {
                SPList configList = Web.Lists["RS Workflow Configuration"];
                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + properties.AfterProperties["Title"] + "</Value></Eq></Where>";
                SPListItemCollection itemCollection = configList.GetItems(query);

                if (itemCollection.Count.Equals(1))
                {
                    SPListItem item = itemCollection[0];

                    if (item["WFLibraryURL"] != null)
                    {
                        SPList wfLib = Web.GetListFromUrl(item["WFLibraryURL"].ToString());

                        if (wfLib != null)
                        {
                            foreach (string stepNumberAux in newStepsList)
                            {
                                if (!properties.Web.Fields.ContainsField("Step " + stepNumberAux + " Assigned To"))
                                {
                                    string strCol = properties.Web.Fields.Add("Step " + stepNumberAux + " Assigned To", SPFieldType.User, false);
                                    SPField field = properties.Web.Fields.GetField("Step " + stepNumberAux + " Assigned To");
                                    field.Group = "RS Columns";
                                    field.Update();
                                }

                                if (properties.Web.Fields.ContainsField("Step " + stepNumberAux + " Assigned To") && !wfLib.ContentTypes["Workflow"].Fields.ContainsField("Step " + stepNumberAux + " Assigned To"))
                                {
                                    SPFieldLink fieldLink = new SPFieldLink(properties.Web.Fields.GetField("Step " + stepNumberAux + " Assigned To"));
                                    wfLib.ContentTypes["Workflow"].FieldLinks.Add(fieldLink);
                                    wfLib.ContentTypes["Workflow"].Update();
                                    wfLib.Update();
                                }

                                if (wfLib.ContentTypes["Workflow"].Fields.ContainsField("Step " + stepNumberAux + " Assigned To") && !wfLib.DefaultView.ViewFields.Exists("Step_x0020_" + stepNumberAux + "_x0020_Assigned_x0020_To"))
                                {
                                    SPView view = wfLib.DefaultView;
                                    view.ViewFields.Add(wfLib.ContentTypes["Workflow"].Fields.GetField("Step " + stepNumberAux + " Assigned To"));
                                    view.Update();
                                }
                            }
                        }
                        else
                        {
                            properties.ErrorMessage = "Workflow library does not exist.";
                            properties.Status = SPEventReceiverStatus.CancelWithError;
                        }
                    }
                    else
                    {
                        properties.ErrorMessage = "Workflow library does not exist.";
                        properties.Status = SPEventReceiverStatus.CancelWithError;
                    }
                }
                else
                {
                    properties.ErrorMessage = "Wrong workflow title.";
                    properties.Status = SPEventReceiverStatus.CancelWithError;
                }
            }
            catch (Exception ex)
            {
                properties.ErrorMessage = ex.Message;
                properties.Status = SPEventReceiverStatus.CancelWithError;
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, null, "WorkflowStepDefinitionsEventReceiver - CreateStepInLibraryModule() " + ex.Message);
            }
        }

    }
}