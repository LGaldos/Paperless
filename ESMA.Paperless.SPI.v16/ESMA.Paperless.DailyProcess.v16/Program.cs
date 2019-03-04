using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;

namespace ESMA.Paperless.DailyProcess.v16
{
    class Program
    {
        //--------------------------------------------------------------------
        //Application: ESMA.Paperless.DailyProcess.v16
        //Compatible: SharePoint 2016
        //Build Platform target: x86
        //Framework: .NET Framework 4.5
        //Release: v.2.0.0
        //Modified Date: 23/11/2018
        //--------------------------------------------------------------------


        static void Main(string[] args)
        {
            RSDailyActivity();
        }

        /// <summary>
        /// Main process
        /// </summary>
        public static void RSDailyActivity()
        {
            try
            {
                string urlSite = ConfigurationManager.AppSettings["RSSiteURL"];
                 
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite site = new SPSite(urlSite))
                    {

                    SPWeb web = site.OpenWeb();
                    web.AllowUnsafeUpdates = true;

                    General.SaveErrorsLog(null, "Paperless Daily Process - Running on '" + System.DateTime.Now);

                    Dictionary<string, string> parameters = GetConfigurationParameters(web);

                    RemoveNotAssociatedWorkflows(web, parameters);
                    FixDuplicatedItemHistoryModule(web, parameters);
                    FixAssignedPersonModule(web, parameters);
                    


                    //CR33 - Merger regular notifications
                    Notifications.NotificationsModule(web, parameters);
             

                    General.SaveErrorsLog(null, "Paperless Daily Process - Finished at '" + System.DateTime.Now);

                    web.AllowUnsafeUpdates = false;
                    web.Close();
                    web.Dispose();
                    }

                });
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "RSDailyActivity() - " + ex.Message.ToString());
            }
        }

        /// <summary>
        /// Get Routing Slip configuration parameters
        /// </summary>
        /// <param name="web"></param>
        /// <returns>String dictionary with all configuration parameters</returns>
        public static Dictionary<string, string> GetConfigurationParameters(SPWeb web)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            try
            {
                SPList list = web.Lists["RS Configuration Parameters"];

                foreach (SPListItem item in list.Items)
                {
                    try
                    {
                        if (item.Fields.ContainsFieldWithStaticName("Value1") && item["Value1"] != null)
                            parameters.Add(item.Title, item["Value1"].ToString().Trim());
                    }
                    catch { continue; }
                }
            }
            catch
            {
            }
            return parameters;
        }

        /// <summary>
        /// Remove workflow garbage
        /// </summary>
        /// <param name="web"></param>
        /// <param name="parameters"></param>
        public static void RemoveNotAssociatedWorkflows(SPWeb web, Dictionary<string, string> parameters)
        {
            try
            {
                SPList historyList = web.Lists["RS Workflow History"];

                for (int i = 0; i < web.Lists.Count; i++)
                {
                    SPList list = web.Lists[i];
                    
                    if (list.BaseTemplate.GetHashCode().Equals(906))
                    {
                        List<SPListItem> itemsToRemove = new List<SPListItem>();

                        SPQuery query = new SPQuery();
                        query.Query = "<Where><And><Eq><FieldRef Name='FSObjType' /><Value Type='Integer'>1</Value></Eq><And><Eq><FieldRef Name='WFStatus' /><Value Type='Text'>" + parameters["Status Draft"] + "</Value></Eq><Geq><FieldRef Name='Created' /><Value Type='DateTime'><Today OffsetDays='-1' /></Value></Geq></And></And></Where>";

                        foreach (SPListItem item in list.GetItems(query))
                        {
                            string wfid = string.Empty;

                            try
                            {
                                if (item["WFID"] != null)
                                {
                                    wfid = item["WFID"].ToString();

                                    SPQuery historyQuery = new SPQuery();
                                    historyQuery.Query = "<Where><Eq><FieldRef Name='WFID'/><Value Type='Text'>" + wfid + "</Value></Eq></Where>";
                                    historyQuery.ViewAttributes = "Scope=\"RecursiveAll\"";

                                    if (historyList.GetItems(historyQuery).Count.Equals(0))
                                        itemsToRemove.Add(item);
                                }
                            }
                            catch (Exception ex)
                            {
                                General.SaveErrorsLog(wfid, "RemoveNotAssociatedWorkflows() - " + ex.Message.ToString());
                                continue;
                            }
                        }

                        if (itemsToRemove.Count > 0)
                        {
                            SPListItem wfConfiguration = null;
                            SPList logLibrary = null;
                            string wfid = string.Empty;
                            
                            foreach (SPListItem itemToRemove in itemsToRemove)
                            {
                                try
                                {
                                    wfid = itemToRemove["WFID"].ToString();

                                    // Clean Log Library
                                    if (logLibrary == null)
                                    {
                                        wfConfiguration = SP.GetWFsTypeInformation(web, itemToRemove["WFType"].ToString(), wfid);
                                        
                                        if (wfConfiguration.Fields.ContainsFieldWithStaticName("WFLogURL") && wfConfiguration["WFLogURL"] != null)
                                            logLibrary = web.GetListFromUrl(wfConfiguration["WFLogURL"].ToString());
                                        
                                    }

                                    SP.RemoveNotAssociatedLogs(logLibrary, wfid);

                                    //Remove Workflow Library folder
                                    SP.DeleteFolderWFID(itemToRemove.Url, web, wfid);
                                }
                                catch (Exception ex)
                                {
                                    General.SaveErrorsLog(wfid, "RemoveNotAssociatedWorkflows() - " + ex.Message.ToString());
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "RemoveNotAssociatedWorkflows()  - " + ex.Message.ToString());
            }
        }

        public static void FixAssignedPersonModule(SPWeb Web, Dictionary<string, string> parameters)
        {
          
            try
            {
                SPList wfHistoryList = Web.Lists["RS Workflow History"];


                for (int i = 0; i < Web.Lists.Count; i++)
                {
                    SPList list = Web.Lists[i];

                    if (list.BaseTemplate.GetHashCode().Equals(906))
                    {
    
                        SPQuery query = new SPQuery();
                        query.Query = "<Where>"
                            + "<And><Eq><FieldRef Name='FSObjType' /><Value Type='Integer'>1</Value></Eq>"
                            + "<And><Neq><FieldRef Name='WFStatus' /><Value Type='Choice'>Closed</Value></Neq>"
                            + "<And><Neq><FieldRef Name='WFStatus' /><Value Type='Choice'>Deleted</Value></Neq>"
                            + "<Geq><FieldRef Name='Modified' /><Value Type='DateTime'><Today OffsetDays='-1' /></Value></Geq>"
                            + "</And></And></And></Where>";


                        foreach (SPListItem wfLibraryItem in list.GetItems(query))
                        {
                            string wfid = string.Empty;
                            string stepNumber = string.Empty;
                            string wfStatus = string.Empty;
                            SPFieldUserValue userLibraryValue = null;

                            try
                            {
                                if (wfLibraryItem["WFID"] != null)
                                {
                                    wfid = wfLibraryItem["WFID"].ToString();

                                    if (wfLibraryItem["AssignedPerson"] != null)
                                        userLibraryValue = new SPFieldUserValue(Web, wfLibraryItem["AssignedPerson"].ToString());

                                    if (wfLibraryItem["StepNumber"] != null)
                                        stepNumber = wfLibraryItem["StepNumber"].ToString();

                                    if (wfLibraryItem["WFStatus"] != null)
                                        wfStatus = wfLibraryItem["WFStatus"].ToString();

                                    ValidateAssignedPerson.ValidateAssignedPersonModule(wfLibraryItem, userLibraryValue, stepNumber, wfid, Web, wfStatus, parameters,  wfHistoryList);


                                }
                            }
                            catch
                            {
                                General.SaveErrorsLog(wfid, "FixAssignedPersonModule()  - Error updating AssignedPerson. (Value: " + userLibraryValue.User + ")");
                                continue;
                            }
                        }


                    }

                }

                
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(string.Empty, "FixAssignedPersonModule()  - " + ex.Message.ToString());
            }


        }

        public static void FixDuplicatedItemHistoryModule(SPWeb Web, Dictionary<string, string> parameters)
        {
            List<string> WFIDDuplicatedList = new List<string>();

            try
            {
                SPList wfHistoryList = Web.Lists["RS Workflow History"];


                for (int i = 0; i < Web.Lists.Count; i++)
                {
                    SPList list = Web.Lists[i];

                    if (list.BaseTemplate.GetHashCode().Equals(906))
                    {

                        SPQuery query = new SPQuery();
                        query.Query = "<Where>"
                            + "<And><Eq><FieldRef Name='FSObjType' /><Value Type='Integer'>1</Value></Eq>"
                            + "<Geq><FieldRef Name='Modified' /><Value Type='DateTime'><Today OffsetDays='-1' /></Value></Geq>"
                            + "</And></Where>";


                        foreach (SPListItem wfLibraryItem in list.GetItems(query))
                        {
                            string wfid = string.Empty;
                            bool isDuplicated = false;
                            

                            try
                            {
                                if (wfLibraryItem["WFID"] != null)
                                {
                                    wfid = wfLibraryItem["WFID"].ToString();

                                    //Item - WFHistory
                                    isDuplicated = SP.IsWorkflowHistoryRecordDuplicated(wfHistoryList, wfid);

                                    if ((isDuplicated) && (!WFIDDuplicatedList.Contains(wfid)))
                                        WFIDDuplicatedList.Add(wfid);

                                }
                            }
                            catch
                            {
                                General.SaveErrorsLog(wfid, "FixDuplicatedItemHistoryModule()  - Error removing WFID '" + wfid + "'");
                                continue;
                            }
                        }


                    }

                }

                //Remove duplicated instances
                ValidateAssignedPerson.RemoveDuplicatedInstances(WFIDDuplicatedList, wfHistoryList);
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(string.Empty, "FixDuplicatedItemHistoryModule()  - " + ex.Message.ToString());
            }


        }
       
    }
}
