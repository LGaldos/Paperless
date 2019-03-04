using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;


namespace ESMA.Paperless.Webparts.v16.RSWorkflow
{
    public static class DocumentLibraries
    {
        /// <summary>
        ///Remove all the documents added during step signing
        /// </summary>
        /// <param name="FirstDateTime"></param>
        /// <param name="wfid"></param>
        /// <param name="wftypename"></param>
        /// <param name="Web"></param>
        public static void RemoveRecentlyCreatedDocs(SPList list, string wfid, SPWeb Web, SPUser loggedUser, int currentStep, SPListItem item)
        {
            try
            {
                SPFolder folder = Web.GetFolder(item.Url);
                SPContentType contentType = list.ContentTypes["Workflow Document"];
                DateTime lastModifiedDate = Convert.ToDateTime(item["Modified"].ToString());

                SPQuery query = new SPQuery();
                query.Folder = folder;
                query.Query = "<Where>"
                    + "<And><Geq><FieldRef Name='Created' /><Value  IncludeTimeValue='TRUE' Type='DateTime'>" + SPUtility.CreateISO8601DateTimeFromSystemDateTime(lastModifiedDate) + "</Value></Geq>"
                    + "<And><Eq><FieldRef Name='WFID' /><Value Type='Text'>" + wfid + "</Value></Eq>"
                    + "<And><Eq><FieldRef Name='StepNumber' /><Value Type='Number'>" + currentStep + "</Value></Eq>"
                    + "<Eq><FieldRef Name='ContentType' /><Value Type='Computed'>" + contentType.Name + "</Value></Eq>"
                    + "</And></And></And></Where>";
                query.ViewAttributes = "Scope=\"Recursive\"";

                SPListItemCollection documentCol = list.GetItems(query);

                if (documentCol != null && documentCol.Count > 0)
                {
                    List<int> idsToRemove = new List<int>();

                    foreach (SPListItem docItem in documentCol)
                    {
                        
                        if ((docItem.ContentType.Name.Equals("Workflow Document") || docItem.ContentType.Name.Equals("Link to a Document")))
                        {
                             if (CheckDocumentUploadedCurrently(wfid, docItem, loggedUser,  currentStep))
                                idsToRemove.Add(docItem.ID);
                        }
                    }

                    foreach(int id in idsToRemove)
                    {
                        string fileName = string.Empty;

                        try
                        {
                            SPListItem  docItem = list.Items.GetItemById(id);
                            fileName = docItem.File.Name;

                            if (docItem.File != null && docItem.File.CheckedOutByUser != null)
                                docItem.File.CheckIn(string.Empty);

                            using (new DisabledItemEventsScope())
                            {
                                docItem.File.Recycle();
                            }


                        }
                        catch (Exception ex)
                        {
                            General.saveErrorsLog(wfid, "RemoveRecentlyCreatedDocs - File Name: '" + fileName + "'. " + ex.Message);
                            continue; 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "RemoveRecentlyCreatedDocs " + ex.Message);
            }
        }

        public static bool CheckDocumentUploadedCurrently(string wfid, SPListItem docItem, SPUser loggedUser, int currentStep)
        {
            bool removeDocument = true;

            try
            {
                SPFile docFile = docItem.File;

                foreach (SPFileVersion v in docFile.Versions)
                {
                    SPUser versionCreatedUser = v.CreatedBy;
                    int stepNumberVersion = Convert.ToInt32(v.Properties["StepNumber"].ToString().Substring(0, v.Properties["StepNumber"].ToString().IndexOf('.')));

                    if (!(currentStep.Equals(stepNumberVersion) && loggedUser.LoginName.Equals(versionCreatedUser.LoginName)))
                      return removeDocument = false;
                }

                if ((removeDocument.Equals(true)) ||  docFile.Versions.Count.Equals(0))
                {

                    if (docItem["StepNumber"].ToString().Equals(currentStep.ToString()) && docItem["Author"].ToString().Equals(loggedUser.ToString()))
                        return removeDocument = true;
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "CheckDocumentUploadedCurrently() " + ex.Message);
            }

            return removeDocument;
        }

        /// <summary>
        /// Restore all the document that have been modified during step signing to last previous versions
        /// </summary>
        /// <param name="FirstDateTime"></param>
        /// <param name="wfid"></param>
        /// <param name="wftypename"></param>
        /// <param name="Web"></param>
        /// <param name="loggedUser"></param>
        public static void RestoreRecentDocVersionsModule(SPList list, string wfid, SPWeb Web, SPUser loggedUser, int currentStep, SPListItem item)
        {
            try 
            {
                SPFolder folder = Web.GetFolder(item.Url);
                SPContentType contentType = list.ContentTypes["Workflow Document"];
                DateTime lastModifiedDate = Convert.ToDateTime(item["Modified"].ToString());

                SPQuery query = new SPQuery();
                query.Folder = folder;
                query.Query = "<Where>"
                    + "<And><Geq><FieldRef Name='Modified' /><Value  IncludeTimeValue='TRUE' Type='DateTime'>" + SPUtility.CreateISO8601DateTimeFromSystemDateTime(lastModifiedDate) + "</Value></Geq>"
                    + "<And><Eq><FieldRef Name='WFID' /><Value Type='Text'>" + wfid + "</Value></Eq>"
                    + "<And><Eq><FieldRef Name='StepNumber' /><Value Type='Number'>" + currentStep + "</Value></Eq>"
                    + "<Eq><FieldRef Name='ContentType' /><Value Type='Computed'>" + contentType.Name + "</Value></Eq>"
                    + "</And></And></And></Where>";
                query.ViewAttributes = "Scope=\"Recursive\"";

               
                SPListItemCollection documentCol = list.GetItems(query);

                foreach (SPListItem docItem in documentCol)
                    {
                        SPFile docFile = null;

                        try
                        {
                            int indexToRestore = 1;
                            int stepNumberToRestore = 1;
                            docFile = docItem.File;

                            if ((docFile != null) && (docFile.Versions.Count > 0))
                            {
                                Dictionary<int, int> newIndexVersionsList = new Dictionary<int, int>();
                                int totalVersions = docFile.Versions.Count;

                                //The current version is excluded
                                foreach (SPFileVersion v in docFile.Versions)
                                {
                                    int docVersion = Convert.ToInt32(v.VersionLabel.Replace(".0", null).Trim());
                                    int stepNumberVersion = Convert.ToInt32(v.Properties["StepNumber"].ToString().Substring(0, v.Properties["StepNumber"].ToString().IndexOf('.')));

                                    if (!newIndexVersionsList.ContainsKey(docVersion))
                                        newIndexVersionsList.Add(docVersion, stepNumberVersion);
                                }
                                

                                //Check versions
                                if (newIndexVersionsList.Count > 0)
                                {
                                    var items = from pair in newIndexVersionsList
                                                orderby pair.Key descending
                                                select pair;


                                    foreach (KeyValuePair<int, int> pair in items)
                                    {
                                        if (!pair.Value.Equals(Convert.ToInt32(currentStep)))
                                        {
                                            indexToRestore = pair.Key;
                                            stepNumberToRestore = pair.Value;
                                            break;
                                        }
                                    }


                                    RestoreDocVersion(docFile, wfid, indexToRestore, loggedUser, stepNumberToRestore);
                                  

                                }
                                

                            }
                        }
                        catch (Exception ex)
                        {
                            General.saveErrorsLog(wfid, "RestoreRecentDocVersions - Document: '" + docFile.Name + "'. " + ex.Message);
                            continue; 
                        }
                    
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "RestoreRecentDocVersionsModule() " + ex.Message);
            }
        }

        private static void RestoreDocVersion(SPFile docFile, string wfid, int indexToRestore, SPUser loggedUser, int stepNumberToRestore)
        {

            try
            {
                using (new DisabledItemEventsScope())
                {
                      

                if (docFile.CheckedOutByUser != null)
                    docFile.CheckIn("Rollback process checked in this document.");

                    docFile.CheckOut();
                    docFile.Versions.Restore(indexToRestore - 1);
                    docFile.CheckIn("Step cancelled by " + loggedUser.Name + ". Rollback to version: {v." + indexToRestore + ".0}");

                     docFile.Item["StepNumber"] = stepNumberToRestore;
                        docFile.Item["Editor"] = loggedUser;
                        docFile.Item.UpdateOverwriteVersion();
                    }
                

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "RestoreRecentDocVersion() " + ex.Message);
            }
        }

      
        /// <summary>
        /// Check in all the document that have been checked out
        /// </summary>
        /// <param name="wfid"></param>
        /// <param name="Web"></param>
        /// <param name="loggedUser"></param>
        public static void CheckInDocs(SPList list, string wfid, SPWeb Web, SPUser loggedUser)
        {
            try
            {
                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='WFID' /><Value Type='Text'>" + wfid + "</Value></Eq></Where>";
                query.ViewAttributes = "Scope=\"RecursiveAll\"";

                SPListItemCollection itemCol = list.GetItems(query);

                    foreach (SPListItem item in itemCol)
                    {
                        try
                        {
                            SPFile docFile = item.File;

                            if (docFile != null)
                            {
                                
                                if (docFile.CheckedOutByUser != null)
                                    docFile.CheckIn("Document automatically checked in by " + loggedUser.Name);

                                list.Update();
                            }
                        }
                        catch { continue; }
                    }
                
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "CheckInDocs " + ex.Message);
            }
        }

        /// <summary>
        /// Check if every document is checked in
        /// </summary>
        /// <param name="list"></param>
        /// <param name="wfid"></param>
        /// <param name="Web"></param>
        public static bool EverythingCheckedIn(SPList list, string wfid, SPWeb Web)
        {
            bool checkedIn = true;

            try
            {
                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='WFID' /><Value Type='Text'>" + wfid + "</Value></Eq></Where>";
                query.ViewAttributes = "Scope=\"RecursiveAll\"";

                SPListItemCollection itemCol = list.GetItems(query);

                foreach (SPListItem item in itemCol)
                {
                    try
                    {
                        SPFile docFile = item.File;

                        if ((docFile != null) && (docFile.CheckedOutByUser != null))
                        {
                            checkedIn = false;
                            break;
                        }
                    }
                    catch { continue; }
                }
               
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "EverythingCheckedIn() " + ex.Message);
                return false;
            }

            return checkedIn;
        }


        /// <summary>
        /// Restore all the document that have been removed during step signing from the Recycle Bin
        /// </summary>
        /// <param name="FirstDateTime"></param>
        /// <param name="wfid"></param>
        /// <param name="wftypename"></param>
        /// <param name="Web"></param>
        /// <param name="loggedUser"></param>
        public static void RestoreDocFromRecycleBin( string wfid, SPUser loggedUser, List<string> docsRemovedList, SPSite Site)
        {
            try
            {


                       List<Guid> guidList = new List<Guid>();
                       SPRecycleBinItemCollection binItems = null;


                       SPRecycleBinQuery query = new SPRecycleBinQuery();
                       //query.ItemCollectionPosition = SPRecycleBinItemCollectionPosition.FirstPage;
                       query.ItemState = SPRecycleBinItemState.FirstStageRecycleBin;
                       query.RowLimit = 300;
                       query.OrderBy = SPRecycleBinOrderBy.Default;
                       query.OrderBy = SPRecycleBinOrderBy.DeletedDate;

                       binItems = Site.GetRecycleBinItems(query);


                       foreach (string fileName in docsRemovedList)
                       {

                           var filteredItems = from i in binItems.OfType<SPRecycleBinItem>()
                                               where i.ItemType == SPRecycleBinItemType.File && i.Title == fileName && i.DirName.Contains(wfid) && i.DeletedBy.Name == loggedUser.Name
                                               select i;


                           foreach (SPRecycleBinItem filteredItem in filteredItems)
                           {
                               if (!guidList.Contains(filteredItem.ID))
                                   guidList.Add(filteredItem.ID);
                           }
                       }

                       if (guidList.Count > 0)
                       {
                           using (new DisabledItemEventsScope())
                           {
                               Site.RecycleBin.Restore(guidList.ToArray());
                           }
                       }



                
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "RestoreDocFromRecycleBin " + ex.Message);
                General.saveErrorsLog(wfid, "RestoreDocFromRecycleBin " + ex.StackTrace);
            }
        }
    }
}
