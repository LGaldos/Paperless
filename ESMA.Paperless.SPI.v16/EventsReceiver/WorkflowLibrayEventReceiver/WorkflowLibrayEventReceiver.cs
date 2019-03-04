using System;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Workflow;

namespace ESMA.Paperless.EventsReceiver.v16.EventsReceiver.WorkflowLibrayEventReceiver
{
    /// <summary>
    /// List Item Events
    /// </summary>
    public class WorkflowLibrayEventReceiver : SPItemEventReceiver
    {
        /// <summary>
        /// An item was added
        /// </summary>
        public override void ItemAdded(SPItemEventProperties properties)
        {
            string wfid = string.Empty;

            try
            {
                base.ItemAdded(properties);
                SPSite site = properties.Web.Site as SPSite;
                SPListItem item = properties.ListItem;
                SPUser editorUser = properties.Web.CurrentUser;


                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite elevatedSite = new SPSite(site.ID))
                    {
                        SPWeb web = elevatedSite.RootWeb;
                        web.AllowUnsafeUpdates = true;

                        string fileName = item.File.Name;
                        wfid = item["WFID"].ToString();

                        if (item.ContentType.Name.Equals("Workflow Document") || item.ContentType.Name.Equals("Link to a Document") || item.ContentType.Name.Equals("Document"))
                        {
                            
                            string actionTaken = classLibraryEventReceiver.GetActionDescription(classLibraryEventReceiver.ActionsEnum.NewDocument.ToString());
                            string actionDetails = "New document: " + item.File.Name;

                            classLibraryEventReceiver.CreateWorkflowLogModule(actionTaken, actionDetails, item, web, wfid, properties, editorUser);
                        }

                        web.AllowUnsafeUpdates = false;
                    }


                });
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, wfid, "WorkflowLibrayEventReceiver - ItemAdded() " + ex.Message);
            }
        }

        /// <summary>
        /// An item is being added.
        /// </summary>
        public override void ItemAdding(SPItemEventProperties properties)
        {
            base.ItemAdding(properties);
            string wfid = string.Empty;

            try
            {
                   using (new DisabledItemEventsScope())
                    {
                
                        //If new item is a document
                        if (properties.AfterUrl.Contains("."))
                        {
                            //If new item is a link or a workflow document
                            if (properties.AfterUrl.ToUpper().Contains(".ASPX"))
                                properties.AfterProperties["ContentTypeId"] = properties.Web.Site.RootWeb.ContentTypes["Link to a Document"].Id.ToString();
                            else
                                properties.AfterProperties["ContentTypeId"] = "0x010000bbe2cb30b8ae48f8a39bd6d1f94b8df0";
                            string folderURL = properties.AfterUrl.Substring(0, properties.AfterUrl.LastIndexOf("/"));
                            SPFile file = properties.OpenWeb().GetFile(folderURL);

                            if (file != null)
                            {
                                //Add minimum metadata
                                SPFolder wfFolder = file.ParentFolder;
                                SPListItem wfItem = wfFolder.Item;
                                SPWeb web = properties.Web.Site.RootWeb;
                                wfid = wfItem["WFID"].ToString();

                                if (wfItem["StepNumber"] != null)
                                    properties.AfterProperties["StepNumber"] = wfItem["StepNumber"];
                                if (wfItem["WFID"] != null)
                                    properties.AfterProperties["WFID"] = wfItem["WFID"];
                                properties.AfterProperties["DocumentationType"] = file.Name;
                                //properties.AfterProperties["Title"] = file.Name;
                                properties.AfterProperties["WFDocumentPreview"] = web.Url + "/_layouts/15/ESMA.Paperless.Design.v16/images/RSPreview.png";

                               

                            }
                        }
                }
            }
            catch (Exception ex) 
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, wfid, "WorkflowLibrayEventReceiver - ItemAdding() " + ex.Message);
            }
        }

        /// <summary>
        /// An item is being updated.
        /// </summary>
        public override void ItemUpdating(SPItemEventProperties properties)
        {
            base.ItemUpdating(properties);
            string wfid = string.Empty;

            try
            {
                using (new DisabledItemEventsScope())
                {

                    SPListItem item = properties.ListItem;
                    SPSite site = properties.Web.Site as SPSite;
                    SPUser editorUser = properties.Web.CurrentUser;

                    //If workflow document
                    if (properties.ListItem != null && (item.ContentType.Name.Equals("Workflow Document") || item.ContentType.Name.Equals("Link to a Document") || item.ContentType.Name.Equals("Document")))
                    {
                        //Add minimum metadata to document from its parent workflow item or folder                    
                        SPFile file = item.Web.GetFile(item.Url);

                        if (file != null)
                        {
                            SPFolder folder = file.ParentFolder;


                            //properties.AfterProperties["DocumentationType"] = folder.Name;
                            if (folder.ParentFolder != null)
                            {
                                wfid = folder.ParentFolder.Name;
                                properties.AfterProperties["WFID"] = wfid;

                                SPListItem wfItem = folder.ParentFolder.Item;

                                if (wfItem.Fields.ContainsFieldWithStaticName("StepNumber"))
                                    properties.AfterProperties["StepNumber"] = wfItem["StepNumber"];

                                //ESMA-CR37
                                if (properties.AfterProperties["DocumentationType"] != null)
                                {
                                    string documentationTypeAfter = properties.AfterProperties["DocumentationType"].ToString();
                                    string documentationTypeBefore = folder.Name;

                                    if (!documentationTypeAfter.ToLower().Equals("(empty)"))
                                        classLibraryEventReceiver.MoveDocumentModule(properties, wfid, documentationTypeAfter, file, folder, site, editorUser, documentationTypeBefore);
                                    else
                                    {
                                        classLibraryEventReceiver.UpdateFileMetadata(item.Url, site.RootWeb, wfid, properties, editorUser, folder.Name);
                                        classLibraryEventReceiver.RecordTraceUpdated(wfid, site, item, editorUser, properties);

                                        //Cancel Event
                                        properties.Cancel = true;
                                        properties.Status = SPEventReceiverStatus.CancelNoError;

                                    }


                                }
                                

                            }

                        }
                        

                    }
                }
            }
            catch (Exception ex) 
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, wfid, "WorkflowLibrayEventReceiver - ItemUpdating() " + ex.Message);
            }
        }

        /// <summary>
        /// An item is being deleted.
        /// </summary>
        public override void ItemDeleting(SPItemEventProperties properties)
        {
            base.ItemDeleting(properties);
            string wfid = string.Empty;

            try
            {
             
                SPSite site = properties.Web.Site as SPSite;
                SPListItem item = properties.ListItem;
                SPUser editorUser = properties.Web.CurrentUser;
                

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite elevatedSite = new SPSite(site.ID))
                    {
                        SPWeb web = elevatedSite.RootWeb;
                        web.AllowUnsafeUpdates = true;

                        wfid = item["WFID"].ToString();

                        if (item.ContentType.Name.Equals("Workflow Document") || item.ContentType.Name.Equals("Link to a Document"))
                        {

                            if (classLibraryEventReceiver.PermissionsForRemovingDocument(item, editorUser, web, wfid, properties))
                                //Save Log Action -> Can remove document
                                classLibraryEventReceiver.SaveLogsDeletingDocuments(wfid, properties, web, editorUser, item, true);
                            else
                            {
                                //Save Log Action -> Tried to remove document
                                classLibraryEventReceiver.SaveLogsDeletingDocuments(wfid, properties, web, editorUser, item, false);

                                //Cancel Event
                                properties.Cancel = true;
                                properties.Status = SPEventReceiverStatus.CancelNoError;
                            }

                           

                             
                        }

                        web.AllowUnsafeUpdates = false;
                    }

                });
            }
            catch (Exception ex)
            {
                classLibraryEventReceiver.SaveErrorsLog_EventsReceiver(properties, wfid, "WorkflowLibrayEventReceiver - ItemDeleting() " + ex.Message);
            }

        }

        

 

    }
}