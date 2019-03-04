using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;

namespace ESMA.Paperless.DailyProcess.v16
{
    class Notifications
    {
      
        public static void NotificationsModule(SPWeb web, Dictionary<string, string> parameters)
        {
            DateTime date = System.DateTime.Now.Date;

            try
            {
                if (parameters.ContainsKey("Status In Progress") && parameters.ContainsKey("Notifications Days") && parameters.ContainsKey("Status On Hold") && parameters.ContainsKey("Status Rejected"))
                {
                    string statusInProgress = parameters["Status In Progress"];
                    string statusRejected = parameters["Status Rejected"];
                    string statusOnHold = parameters["Status On Hold"];
                    string dayOfWeek = parameters["Notifications On Hold Frequency"];
                    bool sendOnHoldWorkflows = false;
                    date = date.AddDays(-int.Parse(parameters["Notifications Days"]));

                    if ((!string.IsNullOrEmpty(dayOfWeek)) && !(dayOfWeek.ToLower().Equals("none")))
                    {
                        if (dayOfWeek.ToLower().Equals("daily"))
                            sendOnHoldWorkflows = true;
                        else if (date.DayOfWeek.ToString().ToLower().Equals(dayOfWeek.ToLower()))
                            sendOnHoldWorkflows = true;
                    }
                    
                    SPList historyList = web.GetListFromWebPartPageUrl(web.Url + "/Lists/WFHistory/AllItems.aspx");
                    Dictionary<string, SPUser> usersDictionary = GetUsersToSendNotificationsList(web, historyList, date, statusInProgress, statusOnHold, statusRejected, sendOnHoldWorkflows);

                    if (usersDictionary.Keys.Count > 0)
                    {
                        string emailSubject = string.Empty;
                        string emailText = string.Empty;

                        GetConfigurationParametersNotification(ref emailSubject, ref emailText, parameters);

                        foreach (SPUser assignedPerson in usersDictionary.Values)
                        {
                            List<SPListItem> urgentWFsList = new List<SPListItem>();
                            List<SPListItem> nonUrgentWFsList = new List<SPListItem>();

                            GetWFsAssignedTo(ref urgentWFsList, ref nonUrgentWFsList, web, historyList, date, statusInProgress, statusOnHold, statusRejected, sendOnHoldWorkflows, assignedPerson);
                            SendEmailModule(assignedPerson, urgentWFsList, nonUrgentWFsList, web, emailSubject, emailText);
                        }
                    }

                   
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "NotificationsModule()  - " + ex.Message.ToString());
            }
        }

        private static Dictionary<string, SPUser> GetUsersToSendNotificationsList(SPWeb web, SPList historyList, DateTime date, string statusInProgress, string statusOnHold, string statusRejected,  bool sendOnHoldWorkflows)
        {
            Dictionary<string, SPUser> usersDictionary = new Dictionary<string, SPUser>();
            string wfid = string.Empty;

            try
            {

                SPQuery query = new SPQuery();
                query.ViewFields = string.Concat("<FieldRef Name='Modified' />", "<FieldRef Name='WFStatus' />", "<FieldRef Name='AssignedPerson' />", "<FieldRef Name='WFID' />");

                if (sendOnHoldWorkflows.Equals(false))
                {
                    query.Query = "<Where><And>"
                        + "<Leq><FieldRef Name='Modified' /><Value Type='DateTime'>" + date.ToString("yyyy-MM-ddThh:mm:ssZ") + "</Value></Leq>"
                        + "<Or><Eq><FieldRef Name='WFStatus' /><Value Type='Text'>" + statusInProgress + "</Value></Eq>"
                        + "<Eq><FieldRef Name='WFStatus' /><Value Type='Text'>" + statusRejected + "</Value></Eq>"
                        + "</Or></And></Where>";
                }
                else
                {
                    query.Query = "<Where><And>"
                        + "<Leq><FieldRef Name='Modified' /><Value Type='DateTime'>" + date.ToString("yyyy-MM-ddThh:mm:ssZ") + "</Value></Leq>"
                        + "<Or><Eq><FieldRef Name='WFStatus' /><Value Type='Text'>" + statusOnHold + "</Value></Eq>"
                        + "<Or><Eq><FieldRef Name='WFStatus' /><Value Type='Text'>" + statusInProgress + "</Value></Eq>"
                        + "<Eq><FieldRef Name='WFStatus' /><Value Type='Text'>" + statusRejected + "</Value></Eq>"
                        + "</Or></Or></And></Where>";
                }

                SPListItemCollection itemCol = historyList.GetItems(query);

                foreach (SPListItem item in itemCol)
                {
                    try
                    {
                        wfid = item["WFID"].ToString();

                        if (item["AssignedPerson"] != null)
                        {

                            SPFieldUserValue userValue = new SPFieldUserValue(web, item["AssignedPerson"].ToString());
                            SPUser user = userValue.User;

                            if (user != null)
                            {
                                if (!usersDictionary.ContainsKey(user.LoginName))
                                    usersDictionary.Add(user.LoginName, user);
                            }

                        }
                        else
                            General.SaveErrorsLog(wfid, "RSDailyProcess - WF '" + wfid + "is 'In Progress' without any AssignedPerson.");

                    }
                    catch
                    {
                        General.SaveErrorsLog(wfid, "RSDailyProcess - GetUsersToSendNotificationsList: Exception WFID: '" + wfid + "'.");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(wfid, "GetUsersToSendNotificationsList() " + ex.Message);
            }

            return usersDictionary;
        }

        private static void GetWFsAssignedTo(ref List<SPListItem> urgentWFsList, ref List<SPListItem> nonUrgentWFsList, SPWeb web, SPList historyList, DateTime date, string statusInProgress, string statusOnHold, string statusRejected, bool sendOnHoldWorkflows, SPUser assignedPerson)
        {
            string wfid = string.Empty;

            try
            {

                SPQuery query = new SPQuery();

                if (sendOnHoldWorkflows.Equals(false))
                {
                    query.Query = "<Where><And>"
                        + "<Leq><FieldRef Name='Modified' /><Value Type='DateTime'>" + date.ToString("yyyy-MM-ddThh:mm:ssZ") + "</Value></Leq>"
                        + "<And>"
                        + "<Eq><FieldRef Name='AssignedPerson' Nullable='TRUE' LookupId='True' /><Value Type='Integer'>" + assignedPerson.ID + "</Value></Eq>"
                        + "<Or><Eq><FieldRef Name='WFStatus' /><Value Type='Text'>" + statusInProgress + "</Value></Eq>"
                        + "<Eq><FieldRef Name='WFStatus' /><Value Type='Text'>" + statusRejected + "</Value></Eq>"
                        + "</Or></And></And></Where>";
                }
                else
                {
                    query.Query = "<Where><And>"
                        + "<Leq><FieldRef Name='Modified' /><Value Type='DateTime'>" + date.ToString("yyyy-MM-ddThh:mm:ssZ") + "</Value></Leq>"
                        + "<And>"
                        + "<Eq><FieldRef Name='AssignedPerson' Nullable='TRUE' LookupId='True' /><Value Type='Integer'>" + assignedPerson.ID + "</Value></Eq>"
                        + "<Or>"
                        + "<Eq><FieldRef Name='WFStatus' /><Value Type='Text'>" + statusOnHold + "</Value></Eq>"
                        + "<Or><Eq><FieldRef Name='WFStatus' /><Value Type='Text'>" + statusInProgress + "</Value></Eq>"
                        + "<Eq><FieldRef Name='WFStatus' /><Value Type='Text'>" + statusRejected + "</Value></Eq>"
                        + "</Or></Or></And></And></Where>";
                }

                SPListItemCollection itemCol = historyList.GetItems(query);

                foreach (SPListItem item in itemCol)
                {
                    try
                    {
                        wfid = item["WFID"].ToString();

                        bool urgentValue = Convert.ToBoolean(item["Urgent"].ToString());

                        if (urgentValue.Equals(true))
                        {
                            if (!urgentWFsList.Contains(item))
                                urgentWFsList.Add(item);
                        }
                        else
                        {
                            if (!nonUrgentWFsList.Contains(item))
                                nonUrgentWFsList.Add(item);
                        }
                    }
                    catch
                    {
                        General.SaveErrorsLog(wfid, "RSDailyProcess - GetWFsAssignedTo: Exception WFID: '" + wfid + "'.");
                        continue;
                    }

                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(wfid, "GetWFsAssignedTo() " + ex.Message);
            }

        }

        #region <SENDING of EMAILS MODULE>

        /// <summary>
        /// Send notification e-mails according to urgent not urgent rules
        /// </summary>
        /// <param name="assignedPerson"></param>
        /// <param name="web"></param>
        /// <param name="wfid"></param>
        /// <param name="subject"></param>
        /// <param name="parameters"></param>
        /// <param name="urgentCode"></param>
        private static void SendEmailModule(SPUser assignedPerson, List<SPListItem> urgentWFsList, List<SPListItem> nonUrgentWFsList, SPWeb web, string emailSubject, string emailText)
        {
           
            try
            {
                string userName = assignedPerson.Name;
                string body = string.Empty;
                string urgentBody = string.Empty;
                string nonUrgentBody = string.Empty;
               
                List<string> emailSectionsList = SplitSubjectEmail(emailText);

                if (emailSectionsList.Count.Equals(1))
                {
                    urgentBody = FormatRecursiveBodyEmail(emailSectionsList[0], urgentWFsList, userName);
                    nonUrgentBody = FormatRecursiveBodyEmail(emailSectionsList[0], nonUrgentWFsList, userName);
                    body = urgentBody + nonUrgentBody;
                }
                else if ((emailSectionsList.Count.Equals(2)) && (!emailSectionsList[0].StartsWith("{")))
                {
                    urgentBody = FormatRecursiveBodyEmail(emailSectionsList[1], urgentWFsList, userName);
                    nonUrgentBody = FormatRecursiveBodyEmail(emailSectionsList[1], nonUrgentWFsList, userName);
                    string header = ConfigureParametersHeaderEmail(emailSectionsList[0], userName);
                    body = header + urgentBody + nonUrgentBody;
                }
                else if ((emailSectionsList.Count.Equals(2)) && (emailSectionsList[0].StartsWith("{")))
                {
                    urgentBody = FormatRecursiveBodyEmail(emailSectionsList[0], urgentWFsList, userName);
                    nonUrgentBody = FormatRecursiveBodyEmail(emailSectionsList[0], nonUrgentWFsList, userName);
                    string header = ConfigureParametersHeaderEmail(emailSectionsList[1], userName);
                    body = urgentBody + nonUrgentBody + header;
                }
                else if (emailSectionsList.Count.Equals(3))
                {
                    urgentBody = FormatRecursiveBodyEmail(emailSectionsList[1], urgentWFsList, userName);
                    nonUrgentBody = FormatRecursiveBodyEmail(emailSectionsList[1], nonUrgentWFsList, userName);
                    string header = ConfigureParametersHeaderEmail(emailSectionsList[0], userName);
                    body = header + urgentBody + nonUrgentBody + emailSectionsList[2];
                }

                
                if (!string.IsNullOrEmpty(body))
                    SendEmail(emailSubject, body, assignedPerson, web);
                else
                    General.SaveErrorsLog(null, "SendEmail() - Email Body is empty. No email was sent to: " + assignedPerson.Email);

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "SendEmail()  - " + ex.Message.ToString());
            }
        }

       
        private static List<string> SplitSubjectEmail(string emailText)
        {

            List<string> emailSectionsList = new List<string>();
            string[] separatingChars = { "{", "}" }; 
      
            try
            {
                if (emailText.Contains("{") && emailText.Contains("}"))
                {
                    string[] emailSections = emailText.Split(separatingChars, System.StringSplitOptions.None); 

                    if ((emailSections != null) || (emailSections.Length > 0))
                    {
                        foreach(string inf in emailSections)
                        {
                            emailSectionsList.Add(inf);
                        }
                    }
                }
                else
                    General.SaveErrorsLog(null, "RSDailyProcess - SplitSubjectEmail(). The configuration parameter is wrong (Characters missed: {,}");

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "SplitSubjectEmail() - " + ex.Message.ToString());
            }

            return emailSectionsList;
        }

        private static string FormatRecursiveBodyEmail(string emailParamText, List<SPListItem> wfList, string userName)
        {
            string body = string.Empty;
            string wfid = string.Empty;
            bool urgentValue = false;
            

            try
            {

                foreach (SPListItem wfItem in wfList)
                {
                  
                    try
                    {

                    string wfSubject = string.Empty;
                    string link = string.Empty;
                    string wfStatus = string.Empty;
                    wfid = wfItem["WFID"].ToString();
                    urgentValue = Convert.ToBoolean(wfItem["Urgent"].ToString());
                    

                    //WF Subject
                    if (wfItem["WFSubject"] != null)
                        wfSubject = wfItem["WFSubject"].ToString();
                    else
                        wfSubject =  "No subject";

                    //WF Status
                    if (wfItem["WFStatus"] != null)
                        wfStatus = wfItem["WFStatus"].ToString();

                    //WF Link
                      if (wfItem["WFLink"] != null)
                       link =  new SPFieldUrlValue(wfItem["WFLink"].ToString()).Url;

                    ConfigureParametersBodyEmail(wfid, emailParamText, userName, wfSubject,  wfStatus , link, ref body);

                    }
                    catch
                    {
                        General.SaveErrorsLog(wfid, "RSDailyProcess - FormatRecursiveBodyEmail: Exception WFID: '" + wfid + "'.");
                        continue;
                    }
                }

                if (wfList.Count > 0)
                {
                    if (urgentValue.Equals(true))
                        body = "<p><b>URGENT WORKFLOWS</b></p>" + body;
                    else
                        body = "<p><b>NON-URGENT WORKFLOWS</b></p>" + body;
                }

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(wfid, "FormatRecursiveBodyEmail()  - " + ex.Message.ToString());
            }

            return body;
        }

        private static void GetConfigurationParametersNotification(ref string emailSubject, ref string emailText, Dictionary<string, string> parameters)
        {
            try
            {
                if (parameters.ContainsKey("E-mail Notifications Subject") && parameters.ContainsKey("E-mail Notifications Text"))
                {
                    emailSubject = parameters["E-mail Notifications Subject"];
                    emailText = parameters["E-mail Notifications Text"];
                }
               
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "GetConfigurationParametersNotification()  - " + ex.Message.ToString());
            }
        }

        private static void ConfigureParametersBodyEmail(string wfid, string emailText, string userName, string wfSubject,string wfStatus ,string link, ref string body)
        {
            try
            {
               

                //Email Body
                if (emailText.Contains("[WF ID]"))
                    emailText = emailText.Replace("[WF ID]", wfid);

                if (emailText.Contains("[WF Subject]"))
                    emailText = emailText.Replace("[WF Subject]", wfSubject);

                if (emailText.Contains("[WF Status]"))
                    emailText = emailText.Replace("[WF Status]", wfStatus);

                if (emailText.Contains("[WF Link]"))
                {
                    if (!string.IsNullOrEmpty(link))
                        emailText = emailText.Replace("[WF Link]", String.Format("<a href='{0}'>{1}</a>", link, wfid));
                    else
                        emailText = emailText.Replace("[WF Link]", wfid);
                }

                //WFLink - Not HTML
                if (emailText.Contains("[WF URL]"))
                {
                    if (!string.IsNullOrEmpty(link))
                        emailText = emailText.Replace("[WF URL]", link);
                    else
                        emailText = emailText.Replace("[WF URL]", wfid);
                }

                if (emailText.Contains("[User Name]"))
                    emailText = emailText.Replace("[User Name]", userName);

                if (string.IsNullOrEmpty(emailText))
                    body = emailText;
                else
                    body = body + emailText;

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(wfid, "ConfigureParametersBodyEmail()  - " + ex.Message.ToString());
            }
        }

        private static string ConfigureParametersHeaderEmail(string emailText, string userName)
        {
            try
            {
                if (emailText.Contains("[User Name]"))
                    emailText = emailText.Replace("[User Name]", userName);

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "ConfigureParametersHeaderEmail() " + ex.Message.ToString());
            }

            return emailText;
        }

        private static void SendEmail( string emailSubject, string emailText, SPUser assignedPerson, SPWeb web)
        {
            try
            {
                if ((!String.IsNullOrEmpty(emailText)) && (SPUtility.IsEmailServerSet(web) && (assignedPerson != null)))
                {

                    if (!string.IsNullOrEmpty(assignedPerson.Email))
                    {
                        if (!SPUtility.SendEmail(web, false, false, assignedPerson.Email, emailSubject, emailText))
                        {
                            string errorMessage = "E-mail not sent to " + assignedPerson.Name + " (" + assignedPerson.Email + ").";
                            RecordEmailSending(errorMessage, web);
                        }
                    }
                    else
                    {
                        string errorMessage = "E-mail not sent to '" + assignedPerson.Name + "'. The user does not have an email address.";
                        RecordEmailSending(errorMessage, web);
                    }
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "SendEmail()  - " + ex.Message.ToString());
            }
        }

        /// <summary>
        /// Recordnot successful e-mail sending in error log
        /// </summary>
        /// <param name="message"></param>
        /// <param name="Web"></param>
        private static void RecordEmailSending(string message, SPWeb Web)
        {
            try
            {
                if (!string.IsNullOrEmpty(message))
                {
                    SPList errorList = Web.Lists["RS Error Log"];
                    SPListItem item = errorList.Items.Add();
                    item["Title"] = "Notifications " + message;
                    item.Update();
                    errorList.Update();
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "DailyProcess - RecordEmailSending()  - " + ex.Message.ToString());
            }
        }

        #endregion

       
    }
}
