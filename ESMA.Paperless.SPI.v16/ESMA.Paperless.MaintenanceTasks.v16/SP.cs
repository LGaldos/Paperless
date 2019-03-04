using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.SharePoint;
using System.Configuration;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;


namespace ESMA.Paperless.MaintenanceTasks.v16
{
    class SP
    {

        public static SPListItemCollection GetAllWFsTypeInformation(SPWeb web)
        {
            SPListItemCollection itemCollection = null;

            try
            {
                SPList list = web.GetListFromWebPartPageUrl(web.Url + "/Lists/WFConfiguration/AllItems.aspx");

                if (list != null)
                {
                    SPQuery query = new SPQuery();
                    query.ViewFields = "<FieldRef Name=\"Title\"/><FieldRef Name=\"WFLibraryURL\"/><FieldRef Name=\"WFLogURL\"/>";
                    itemCollection = list.GetItems(query);

                }

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
            return itemCollection;
        }

        public static int GetTotalAttachedDocuments(SPWeb web, SPList wfLibrary)
        {
            int totalDocuments = 0;
           
            try
            {

               
                SPQuery query = new SPQuery();
                query.ViewFields = "<FieldRef Name=\"ContentType\"/><FieldRef Name=\"Created\"/>";
                query.ViewAttributes = "Scope=\"Recursive\"";
                query.Query = "<Where><Eq><FieldRef Name='ContentType' /><Value Type='Computed'>Workflow Document</Value></Eq></Where>";
                SPListItemCollection itemCollection = wfLibrary.GetItems(query);
                totalDocuments = itemCollection.Count;


            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
            return totalDocuments;
        }

        public static int GetTotalItems(SPWeb web, SPList wfLibrary)
        {
            int totalWFs = 0;

            try
            {
                SPFolder oFolder = wfLibrary.RootFolder;

                SPQuery query = new SPQuery();
                query.Folder = oFolder;

                SPListItemCollection allitems = wfLibrary.GetItems(query);
                totalWFs = allitems.Count;

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
            return totalWFs;
        }

        //Get all items from 
        public static int GetItemsFromADate(SPWeb web, SPList list, string year, string month)
        {
            int totalItems = 0;

            try
            {
                string startedDate = GetStartedDate(year, Convert.ToInt32(month));
                string endedDate = GetFinishedDate(year, Convert.ToInt32(month));

                SPQuery query = new SPQuery();
                //query.ViewFields = "<FieldRef Name=\"WFID\"/><FieldRef Name=\"WFType\"/>";
                query.Query = "<Where><And>"
                    + "<Geq><FieldRef Name=\"Created\" /><Value IncludeTimeValue=\"FALSE\" Type=\"DateTime\">" + startedDate + "</Value></Geq>"
                    + "<Leq><FieldRef Name=\"Created\" /><Value IncludeTimeValue=\"FALSE\" Type=\"DateTime\">" + endedDate + "</Value></Leq>"
                    + "</And></Where>";

                SPListItemCollection itemCollection = list.GetItems(query);
                totalItems = itemCollection.Count;

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return totalItems;
        }

        //Get all attached documents from 
        public static int GetAttachedDocumentsFromADate(SPWeb web, SPList list, string year, string month)
        {
            int totalItems = 0;

            try
            {
                string startedDate = GetStartedDate(year, Convert.ToInt32(month));
                string endedDate = GetFinishedDate(year, Convert.ToInt32(month));

                SPQuery query = new SPQuery();
                query.ViewAttributes = "Scope=\"Recursive\"";
                query.Query = "<Where><And>"
                    + "<Eq><FieldRef Name='ContentType' /><Value Type='Computed'>Workflow Document</Value></Eq>"
                    + "<And>"
                    + "<Geq><FieldRef Name=\"Created\" /><Value IncludeTimeValue=\"FALSE\" Type=\"DateTime\">" + startedDate + "</Value></Geq>"
                    + "<Leq><FieldRef Name=\"Created\" /><Value IncludeTimeValue=\"FALSE\" Type=\"DateTime\">" + endedDate + "</Value></Leq>"
                    + "</And></And></Where>";

                SPListItemCollection itemCollection = list.GetItems(query);
                totalItems = itemCollection.Count;

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return totalItems;
        }

        public static SPList GetWFLibrary(SPWeb web, SPListItem wfTypeItem)
        {
            SPList wfLibrary = null;

            try
            {
                //RS WF Library
                SPFieldUrlValue typedValueLibrary = new SPFieldUrlValue(wfTypeItem["WFLibraryURL"].ToString());
                String urlWFLibrary = typedValueLibrary.Url;
                wfLibrary = web.GetListFromWebPartPageUrl(urlWFLibrary);

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return wfLibrary;
        }

        public static SPList GetWFLogList(SPWeb web, SPListItem wfTypeItem)
        {
            SPList wfLogsList = null;

            try
            {
                //RS WF Library

                if (wfTypeItem["WFLogURL"] != null)
                {
                    SPFieldUrlValue typedValueLogsList = new SPFieldUrlValue(wfTypeItem["WFLogURL"].ToString());
                    string urlWFLogsList = typedValueLogsList.Url;
                    wfLogsList = web.GetListFromWebPartPageUrl(urlWFLogsList);
                }
                else
                    General.TraceInformation(wfTypeItem.Title + " does not have WFLogURL", ConsoleColor.Red);

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }

            return wfLogsList;
        }

        public static SPListItem GetWFTypeConfiguration(SPWeb web, string wfOrder)
        {
            SPListItem item = null;

            try
            {
                SPList list = web.GetListFromWebPartPageUrl(web.Url + "/Lists/WFConfiguration/AllItems.aspx");

                if (list != null)
                {
                    SPQuery query = new SPQuery();
                    query.ViewFields = "<FieldRef Name=\"Title\"/><FieldRef Name=\"WFLibraryURL\"/><FieldRef Name=\"WFLogURL\"/><FieldRef Name=\"WFOrder\"/>";
                    query.Query = "<Where><Eq><FieldRef Name='WFOrder' /><Value Type='Number'>" + wfOrder + "</Value></Eq></Where>";
                    SPListItemCollection itemCollection = list.GetItems(query);

                    if (itemCollection.Count > 0)
                        item = itemCollection[0];

                }

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
            return item;
        }

        public static SPListItemCollection GetWFTypeStepDefinitions(SPWeb web, string title)
        {
            SPListItemCollection itemCollection = null;

            try
            {
                SPList list = web.GetListFromWebPartPageUrl(web.Url + "/Lists/WFStepDefinitions/AllItems.aspx");

                if (list != null)
                {
                    SPQuery query = new SPQuery();
                    query.ViewFields = "<FieldRef Name=\"Title\"/><FieldRef Name=\"WFOrder\"/><FieldRef Name=\"EmailReceiverGroup\"/><FieldRef Name=\"StepBackupGroup\"/><FieldRef Name=\"WFGroup\"/><FieldRef Name=\"StepNumber\"/><FieldRef Name=\"SendEmail\"/>";
                    query.Query = "<Where><Eq><FieldRef Name='LinkTitle' /><Value Type='Computed'>" + title + "</Value></Eq></Where><OrderBy><FieldRef Name='StepNumber' Ascending='True' /></OrderBy>";
                    itemCollection = list.GetItems(query);

                }

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
            return itemCollection;
        }

        public static SPListItemCollection GetWFItems(SPWeb web, SPList wfLibrary)
        {
            SPListItemCollection itemCollection = null;

            try
            {
                SPFolder oFolder = wfLibrary.RootFolder;

                SPQuery query = new SPQuery();
                query.Folder = oFolder;
                itemCollection = wfLibrary.GetItems(query);
               
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
            return itemCollection;
        }

        public static SPListItemCollection GetClosedDeletedWFs(SPWeb web, SPList wfLibrary)
        {
            SPListItemCollection itemCollection = null;

            try
            {
               

                SPQuery query = new SPQuery();
                query.ViewFields = "<FieldRef Name=\"Title\"/><FieldRef Name=\"WFID\"/><FieldRef Name=\"WFStatus\"/><FieldRef Name=\"WFOrder\"/><FieldRef Name=\"ConfidentialWorkflow\"/>";
                query.Query = "<Where><And>"
                    + "<Eq><FieldRef Name='ConfidentialWorkflow' /><Value Type='Choice'>Non Restricted</Value></Eq>"
                    + "<Or><Eq><FieldRef Name='WFStatus' /><Value Type='Choice'>Closed</Value></Eq>"
                    + "<Eq><FieldRef Name='WFStatus' /><Value Type='Choice'>Deleted</Value></Eq>"
                    + "</And></Or></Where>";
                itemCollection = wfLibrary.GetItems(query);

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
            return itemCollection;
        }

        public static SPUser GetSPUser(SPWeb Web, SPListItem item, string internalName)
        {
            SPUser user = null;

            try
            {
                SPFieldUserValue userValue = new SPFieldUserValue(Web, item[internalName].ToString());

                if (userValue != null)
                    user = userValue.User;
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
            return user;
        }

        public static SPListItem GetWFTHistoryItem(SPWeb web, string WFID)
        {
            SPListItem item = null;

            try
            {
                SPList list = web.GetListFromWebPartPageUrl(web.Url + "/Lists/WFHistory/AllItems.aspx");

                if (list != null)
                {
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><Eq><FieldRef Name='WFID' /><Value Type='Number'>" + WFID + "</Value></Eq></Where>";
                    SPListItemCollection itemCollection = list.GetItems(query);

                    if (itemCollection.Count > 0)
                        item = itemCollection[0];

                }

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
            return item;
        }
      


        //-----------------------------------------------------------
        //DATETIME
        //-----------------------------------------------------------
        public static string GetStartedDate(string year, int month)
        {
            string date = string.Empty;

            try
            {
                //2013-01-01
                date = year + "-" + Convert.ToString(month) + "-01";

            }
            catch (Exception ex)
            {
                General.TraceException(ex);

            }
            return date;
        }

        public static string GetFinishedDate(string year, int month)
        {
            string date = string.Empty;

            try
            {
                //2013-01-31
                string totalDays = Convert.ToString(System.DateTime.DaysInMonth(Convert.ToInt32(year), month));
                date = year + "-" + Convert.ToString(month) + "-" + totalDays;

            }
            catch (Exception ex)
            {
                General.TraceException(ex);

            }
            return date;
        }

    }
}
