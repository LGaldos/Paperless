using System;
using Microsoft.SharePoint;
using System.Collections.Generic;
using System.Globalization;

namespace ESMA.Paperless.DailyProcess.v16
{
    class ValidateAssignedPerson
    {
        public static void ValidateAssignedPersonModule(SPListItem wfLibraryItem, SPFieldUserValue userLibraryValue, string stepNumber, string wfid, SPWeb web, string wfStatus, Dictionary<string, string> parameters, SPList wfHistoryList)
        {

            bool isValid = false;
            string assignedHistoryPerson = string.Empty;
            SPFieldUserValue userHistoryValue = null;
            SPUser assignedPersonHistory = null;
            

            try
            {

                //Item - WFHistory
                SPListItem wfHistoryItem = SP.GetWorkflowHistoryRecord(wfHistoryList, wfid);
               

                if (wfHistoryItem != null)
                {
                    if (wfHistoryItem["AssignedPerson"] != null)
                    {
                        assignedHistoryPerson = wfHistoryItem["AssignedPerson"].ToString();
                        //Assigned Person -  History (This value used to be wrong)
                        userHistoryValue = new SPFieldUserValue(web, assignedHistoryPerson);
                        assignedPersonHistory = userHistoryValue.User;
                    }

                    if ((userLibraryValue != null) && (userHistoryValue == null))
                        isValid = false;
                    else if ((userLibraryValue == null) && (userHistoryValue != null))
                        isValid = false;
                    else if ((userLibraryValue == null) && (userHistoryValue == null))
                        isValid = true;
                    else if (userLibraryValue.User.ToString().Equals(userHistoryValue.User.ToString()))
                        isValid = true;
                    else if (!(userLibraryValue.User.ToString().Equals(userHistoryValue.User.ToString())))
                        isValid = false;

                    if (isValid == false)
                        UpdateWFHistory(stepNumber, userLibraryValue, wfid, wfHistoryItem, assignedPersonHistory, wfStatus, parameters);
                }
                else
                    General.SaveErrorsLog(wfid, "This WF does not exist in the WF History.");




            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(wfid, "ValidateAssignedPersonModule() - AssignedPerson History: " + assignedHistoryPerson + " - " + ex.Message.ToString());
            }

        }

        public static void UpdateWFHistory(string stepNumber, SPFieldUserValue assignedPersonLibrary, string wfid, SPListItem wfHistoryItem, SPUser assignedPersonHistory, string wfStatus, Dictionary<string, string> parameters)
        {
            try
            {
                if (assignedPersonLibrary == null)
                    wfHistoryItem["AssignedPerson"] = string.Empty;
                else
                    wfHistoryItem["AssignedPerson"] = assignedPersonLibrary.User;

                wfHistoryItem["StepNumber"] = stepNumber;
                wfHistoryItem["WFStatus"] = wfStatus;

                using (new DisabledItemEventsScope())
                {
                    wfHistoryItem.SystemUpdate();
                }

                if (assignedPersonLibrary == null)
                    General.SaveErrorsLog(wfid, "It has been replaced the AssignedPerson (History)'" + assignedPersonHistory + " to EMPTY.");
                else
                    General.SaveErrorsLog(wfid, "It has been replaced the AssignedPerson (History)'" + assignedPersonHistory + " to '" + assignedPersonLibrary.User + "'.");
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(wfid, "UpdateWFHistory() - " + ex.Message.ToString());
            }
        }

        public static void FormatUserLogin(string wfid, ref string userLogin)
        {
            try
            {
                if (userLogin.Contains("#"))
                {
                    string[] inf = userLogin.Split('#');
                    userLogin = inf[1];
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(wfid, "FormatUserLogin() - " + ex.Message.ToString());
            }
        }

        public static void RemoveDuplicatedInstances(List<string> WFIDDuplicatedList, SPList wfHistoryList)
        {
            try
            {
                if (WFIDDuplicatedList != null && WFIDDuplicatedList.Count > 0)
                {
                    List<int> IDItemList = new List<int>();

                    foreach(string wfid in WFIDDuplicatedList)
                    {
                        SPListItemCollection itemCollection = SP.GetWorkflowHistoryRecordCollection(wfHistoryList, wfid, ref  WFIDDuplicatedList);
                        int refTotal = 0;

                            foreach(SPListItem item in itemCollection)
                            {
                                try
                                {
                                    if (!refTotal.Equals(0))
                                        IDItemList.Add(item.ID);

                                   refTotal++;
                                }
                                catch (Exception ex)
                                {
                                    General.SaveErrorsLog(wfid, "RemoveDuplicatedInstances() - FOREACH - " + ex.Message.ToString());
                                    continue;
                                }
                            }
                        

                    }

                    DeleteItemsByID(wfHistoryList, IDItemList);
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(string.Empty, "RemoveDuplicatedInstances() - " + ex.Message.ToString());
            }
        }

        private static void DeleteItemsByID(SPList wfHistoryList, List<int> IDItemList)
        {
            try
            {
                foreach(int id in IDItemList)
                {
                    wfHistoryList.GetItemById(id).Delete();
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(string.Empty, "DeleteItemsByID() - " + ex.Message.ToString());
            }
        }
       
    }
}
