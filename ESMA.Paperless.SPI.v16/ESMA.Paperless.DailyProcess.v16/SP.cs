using System;
using System.Collections.Generic;
using Microsoft.SharePoint;

namespace ESMA.Paperless.DailyProcess.v16
{
    class SP
    {
        public static SPListItem GetWFsTypeInformation(SPWeb web, string WFType, string wfid)
        {
            SPListItem item = null;

            try
            {
                SPList list = web.GetListFromWebPartPageUrl(web.Url + "/Lists/WFConfiguration/AllItems.aspx");

                if (list != null)
                {
                    SPQuery query = new SPQuery();
                    query.ViewFields = "<FieldRef Name=\"Title\"/><FieldRef Name=\"WFLibraryURL\"/><FieldRef Name=\"WFLogURL\"/>";
                    query.Query = "<Where><Eq><FieldRef Name =\"Title\"/><Value Type = \"Text\">" + WFType + "</Value></Eq></Where>";
                    SPListItemCollection itemCollection = list.GetItems(query);

                    if (itemCollection.Count > 0)
                        item = itemCollection[0];
                }

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(wfid, "GetWFsTypeInformation() - " + ex.Message.ToString());
            }
            return item;
        }

        public static SPListItem GetWorkflowHistoryRecord(SPList wfHistory, string wfid)
        {
            SPListItem item = null;


            try
            {

                SPQuery query = new SPQuery();
                query.ViewFields = "<FieldRef Name=\"Title\"/><FieldRef Name=\"WFID\"/><FieldRef Name=\"AssignedPerson\"/><FieldRef Name=\"ID\"/>";
                query.Query = "<Where><Eq><FieldRef Name='WFID'/><Value Type='Text'>" + wfid + "</Value></Eq></Where><OrderBy><FieldRef Name='ID' Ascending='False' /></OrderBy>";
                SPListItemCollection itemCollection = wfHistory.GetItems(query);

                if (itemCollection != null && itemCollection.Count.Equals(1))
                    item = itemCollection[0];
                else if (itemCollection != null && itemCollection.Count > 1)
                {
                    item = itemCollection[0];
                    General.SaveErrorsLog(wfid, "GetWorkflowHistoryRecord(). ERROR! There are more than one instance in the RS Workflow History.");
    
                }
                else
                    General.SaveErrorsLog(wfid, "GetWorkflowHistoryRecord(). ERROR! There is not any reference in the RS Workflow History.");

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(wfid, "GetWorkflowHistoryRecord() - " + ex.Message.ToString());
            }
            return item;
        }

        public static bool IsWorkflowHistoryRecordDuplicated(SPList wfHistory, string wfid)
        {
            bool isDuplicated = false;


            try
            {

                SPQuery query = new SPQuery();
                query.ViewFields = "<FieldRef Name=\"Title\"/><FieldRef Name=\"WFID\"/><FieldRef Name=\"AssignedPerson\"/><FieldRef Name=\"ID\"/>";
                query.Query = "<Where><Eq><FieldRef Name='WFID'/><Value Type='Text'>" + wfid + "</Value></Eq></Where><OrderBy><FieldRef Name='ID' Ascending='False' /></OrderBy>";
                SPListItemCollection itemCollection = wfHistory.GetItems(query);

                if (itemCollection != null && itemCollection.Count > 1)
                {
                    isDuplicated = true;
                    General.SaveErrorsLog(wfid, "IsWorkflowHistoryRecordDuplicated(). ERROR! There were more than one instance in the RS Workflow History.");
                }

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(wfid, "IsWorkflowHistoryRecordDuplicated() - " + ex.Message.ToString());
            }
            return isDuplicated;
        }

        public static void RemoveNotAssociatedLogs(SPList list, string wfid)
        {
            try
            {
                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='WFID' /><Value Type='Text'>" + wfid + "</Value></Eq></Where>";
                query.ViewFields = string.Concat(
                                   "<FieldRef Name='WFID' />",
                                   "<FieldRef Name='ID' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need

                SPListItemCollection itemCol = list.GetItems(query);


                if (itemCol != null && itemCol.Count > 0)
                {
                    List<int> idsToRemove = new List<int>();

                    foreach (SPListItem item in itemCol)
                        idsToRemove.Add(item.ID);

                    foreach (int id in idsToRemove)
                        try
                        {
                            list.Items.DeleteItemById(id);
                            list.Update();
                        }
                        catch { continue; }
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(wfid, "RemoveNotAssociatedLogs() " + ex.Message);
            }
        }

        public static void DeleteFolderWFID(string folderURL, SPWeb web, string wfid)
        {
            try
            {
                SPFolder oFolder = web.GetFolder(folderURL);

                if (oFolder != null)
                {
                    using (new DisabledItemEventsScope())
                    {
                        oFolder.Delete();
                    }

                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(wfid, "DeleteFolderWFID() " + ex.Message);
            }

        }

        public static SPListItemCollection GetWorkflowHistoryRecordCollection(SPList wfHistory, string wfid, ref List<string> WFIDDuplicatedList)
        {
            SPListItemCollection itemCollection = null;

            try
            {

                SPQuery query = new SPQuery();
                query.ViewFields = "<FieldRef Name=\"Title\"/><FieldRef Name=\"WFID\"/><FieldRef Name=\"AssignedPerson\"/><FieldRef Name=\"ID\"/>";
                query.Query = "<Where><Eq><FieldRef Name='WFID'/><Value Type='Text'>" + wfid + "</Value></Eq></Where><OrderBy><FieldRef Name='ID' Ascending='False' /></OrderBy>";
                itemCollection = wfHistory.GetItems(query);

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(wfid, "GetWorkflowHistoryRecordCollection() - " + ex.Message.ToString());
            }
            return itemCollection;
        }

       
    }
}

       


