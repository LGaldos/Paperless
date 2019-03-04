using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint;
using System.Data;
using Microsoft.SharePoint.Utilities;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Mail;
using System.Net;
using System.Globalization;
using ESMA.Paperless.Reports.v16.RSWorkflowReports;

namespace ESMA.Paperless.Reports.v16.TimerJobs
{
    class ReportsSendMail : SPJobDefinition
    {
        public const string ACTION_LAUNCHED = "Launched";
        public const string ACTION_REJECTED = "Rejected";
        public const string ACTION_SIGNED = "Signed";


        public const string JobName = "Routing Slip Scheduled Reports"; 

        public ReportsSendMail() : base() { }

        public ReportsSendMail(string jobName, SPService service): base(jobName, service, null, SPJobLockType.None)
        {
            this.Title = JobName;
        }

        public ReportsSendMail(string jobName, SPWebApplication webapp)
            : base(jobName, webapp, null, SPJobLockType.Job)
        {
            this.Title = JobName;
        }

        public override string Description
        {
            get
            {
                return "Send Reports Regularly";
            }
        }

        public override void Execute(Guid targetInstanceId)
        {
            SPWebApplication webApp = this.Parent as SPWebApplication;
            SPWeb web = webApp.Sites[0].RootWeb;

            ReportsSendMailProcess(web);
        }

        /// <summary>
        /// Main process
        /// </summary>
        private void ReportsSendMailProcess(SPWeb web)
        {
            try
            {
                web.AllowUnsafeUpdates = true;

                Dictionary<string, string> parameters = JobUtilities.GetConfigurationParameters(web);

                SPList templateList = web.Lists["RS Reports Templates"];
                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='RPAutomatic' /><Value Type='Boolean'>1</Value></Eq></Where><OrderBy><FieldRef Name='Created' Ascending='FALSE' /></OrderBy>";
                SPListItemCollection itemsCol = templateList.GetItems(query);
               
                string pattern = "dd/MM/yyyy";

                foreach (SPListItem item in itemsCol)
                {                                                            
                    // Start Date
                    DateTime startDate;
                    DateTime.TryParseExact(item["RP Start Date"].ToString(), pattern, null, DateTimeStyles.None, out startDate);

                    // End Date
                    DateTime endDate = new DateTime();
                    if (item["RP End Date"] != null)
                        DateTime.TryParseExact(item["RP End Date"].ToString(), pattern, null, DateTimeStyles.None, out endDate);

                    // Send Date
                    DateTime sendDate = new DateTime();
                    if (item["RP Date Send"] != null)
                        DateTime.TryParseExact(item["RP Date Send"].ToString(), pattern, null, DateTimeStyles.None, out sendDate);
                    else
                        sendDate = GetInitialSendDate(DateTime.Parse(item["Created"].ToString()), item["RP Frequency"].ToString());

                    if (startDate <= DateTime.Now )
                    {
                        if (endDate == DateTime.MinValue || endDate >= DateTime.Now)
                        {
                            DateTime firstDate = new DateTime();
                            DateTime lastDate = new DateTime();
                            
                            if(IsDateToSend(sendDate, ref firstDate, ref lastDate, item["RP Frequency"].ToString()))
                            {
                                ExportAndSendReport(item, web, firstDate, lastDate, parameters);
                                item["RP Date Send"] = DateTime.Now.ToString("dd/MM/yyyy");
                                item.Update();
                                templateList.Update ();
                            }
                        }
                    }
                }

                web.AllowUnsafeUpdates = false;
            }
            catch (Exception ex)
            {
                JobUtilities.SaveErrorsLog(web, "ReportsSendMailProcess", ex.Message);
            }
        }

        private DateTime GetInitialSendDate(DateTime created, string frecuency)
        {
            DateTime result = new DateTime();
            try
            {
                TimeSpan ts = DateTime.Today - created;

                switch (frecuency)
                {
                    case Constants.FREC_DAILY:
                        result = DateTime.Today.AddDays(-1);
                        break;
                    case Constants.FREC_WEEKLY:
                        for (int i = 0; i < 7; i++)
                        {
                            if (DateTime.Today.AddDays(-i).DayOfWeek == DayOfWeek.Monday)
                            {
                                result = DateTime.Today.AddDays(-i);
                                break;
                            }
                        }

                        break;
                    case Constants.FREC_MONTHLY:
                        result = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        break;

                    case Constants.FREC_YEARLY:
                        result = new DateTime(DateTime.Today.Year, 1, 1);
                        break;
                }

            }
            catch (Exception ex)
            {
                JobUtilities.ExceptionRecording("IsDateToSend", ex.Message);
                
            }

            return result;
        }

        private bool IsDateToSend(DateTime lastSend, ref DateTime firsDate, ref DateTime lastDate, string frecuency)
        {
            bool result = true;
            try
            {
                TimeSpan ts = DateTime.Today - lastSend;

                switch(frecuency)
                {
                    case Constants.FREC_DAILY:
                        if (ts.Days == 1)
                        {
                            firsDate = DateTime.Today.AddDays(-1);
                            lastDate = DateTime.Today;
                        }
                        else
                            result = false;
                        break;
                    case Constants.FREC_WEEKLY:
                        if (ts.Days == 7)
                        {
                            firsDate = DateTime.Today.AddDays(-7);
                            lastDate = DateTime.Today;
                        }
                        else
                            result = false;

                        break;
                    case Constants.FREC_MONTHLY:

                        if (ts.Days == DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month))
                        {
                            firsDate = DateTime.Today.AddDays(DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month) * -1);
                            lastDate = DateTime.Today;
                        }
                        else
                            result = false;

                        break;
                    case Constants.FREC_YEARLY:
                        double totalDays = new DateTime(DateTime.Today.Year, 12, 31).Subtract(DateTime.Today).TotalDays;
                        if(ts.Days == totalDays)
                        {
                            firsDate = new DateTime(DateTime.Today.Year, 1, 1);
                            lastDate = DateTime.Today;
                        }
                        else
                            result = false;

                        break;

                    default:
                        result = false;
                        break;
                }

            }
            catch (Exception ex)
            {
                JobUtilities.ExceptionRecording("IsDateToSend", ex.Message);
                result = false;
            }

            return result;
        }

        private void ExportAndSendReport(SPListItem itemTemplate, SPWeb web, DateTime firstDate, DateTime lastDate, Dictionary<string, string> parameters)
        {
            try
            {
                Dictionary<string, string> reportGeneralColumns = ReportsResults.GetHeaderColumns(web, parameters);
                Dictionary<string, string> reportStepsColumns = ReportsResults.GetStepsColumns(web, parameters);
                Dictionary<string, string> reportTotalColumns = reportGeneralColumns;

                string strTypes = (itemTemplate["RPTypes"] != null) ? itemTemplate["RPTypes"].ToString() : String.Empty;
                string strStatus = (itemTemplate["RPStatus"] != null) ? itemTemplate["RPStatus"].ToString() : String.Empty;
                string strConfidential = (itemTemplate["RPConfidential"] != null) ? itemTemplate["RPConfidential"].ToString() : String.Empty;
                string strCreated = (itemTemplate["RPCreatedBy"] != null) ? itemTemplate["RPCreatedBy"].ToString() : String.Empty;
                string textSearch = (itemTemplate["RPFreeText"] != null) ? itemTemplate["RPFreeText"].ToString() : String.Empty;
                string peActor = (itemTemplate["RPActors"] != null) ? itemTemplate["RPActors"].ToString() : String.Empty;
                string strRoles = (itemTemplate["RPRoles"] != null) ? itemTemplate["RPRoles"].ToString() : String.Empty;
                bool ShowSteps = (Boolean)itemTemplate["RPShowSteps"];

                DataTable resultTable = new DataTable();
                ReportsResults.CreateResultTable(ref resultTable, reportGeneralColumns, reportStepsColumns);

                //Get WFs (getting information from all DLs)
                string queryCommonToExecute = CreateUIQueryModule(web, parameters, firstDate, lastDate, strTypes, strStatus, strConfidential, strCreated, peActor, strRoles);

                // Create a context of the user who created the template 
                SPFieldUserValue authorUser = new SPFieldUserValue(web, itemTemplate["Author"].ToString());
                SPSite authorUserSite = new SPSite(web.Site.Url, authorUser.User.UserToken);
                SPWeb authorUserWeb = authorUserSite.OpenWeb();

                ReportsResults.UIValuesSearch(queryCommonToExecute, authorUserWeb, ref resultTable, reportGeneralColumns);

                authorUserSite.Dispose();
                authorUserWeb.Dispose();

                //Search By Keyword
                if (!string.IsNullOrEmpty(textSearch.Trim()))
                    ReportsResults.GetResultTableKeywords(web, ref resultTable, textSearch.Trim(), reportGeneralColumns);

                if (resultTable != null && resultTable.Rows.Count > 0)
                {
                    resultTable.DefaultView.Sort = "WFID DESC";

                    string fileName = "Report " + DateTime.Now.ToShortDateString().Replace('/', '_') + "-" + DateTime.Now.ToShortTimeString().Replace(':', '-') + ".xlsx";

                    string period = firstDate.ToString("dd/MM/yyyy") + " - " + lastDate.ToString("dd/MM/yyyy");
                    
                    if (ShowSteps)
                    {
                        int numSteps = 0;                        
                        
                        ReportsResults.AddStepsData(ref resultTable, ref numSteps, web, parameters, web.Url);

                        for (int i = 0; i < numSteps * 6; i++)
                        {
                            KeyValuePair<String, String> step = reportStepsColumns.ElementAt(i);
                            reportTotalColumns.Add(step.Key, step.Value);
                        }                        
                    }

                    DataManagement.ExportToExcel(resultTable, reportTotalColumns, System.IO.Path.Combine(parameters["RS Path Temp"], fileName));
                    SendEmail(itemTemplate["RP Recipients"].ToString(), web, itemTemplate["Title"].ToString(), period, parameters["RS Path Temp"], fileName, parameters);
                }

            }
            catch (Exception ex)
            {
                JobUtilities.SaveErrorsLog(web, "ExportAndSendReport", ex.Message);
            }
        }
                               
              
        protected string CreateUIQueryModule(SPWeb Web, Dictionary<string, string> parameters, DateTime dtFirst, DateTime dtLast, string strTypes, string strStatus, string wfConfidential, string peCreated, string peActor, string strRole)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                List<string> queryList = new List<string>();
                queryList.Add("<Eq><FieldRef Name='ContentType' /><Value Type='Computed'>Workflow</Value></Eq>");

                //Queries                
                ReportsQuery.CreateQuery_DateTimeFromTo(ref queryList, dtFirst, dtLast);
                ReportsQuery.CreateQuery_WFType(ref queryList, strTypes);
                ReportsQuery.CreateQuery_WFStatus(ref queryList, strStatus);
                //Actor + Role -> WFActorsSignedRole
                if (!strRole.Equals("All"))
                {
                    string adGroupName = Permissions.GetADGroupName(strRole, parameters);

                    if (string.IsNullOrEmpty(peActor))
                        ReportsQuery.CreateQuery_Role(ref queryList, Web, strRole, adGroupName);
                    else
                        ReportsQuery.CreateQuery_ActorRole(ref queryList, Web, peActor, strRole, parameters, adGroupName);
                }
                else if (!string.IsNullOrEmpty(peActor))
                    ReportsQuery.CreateQuery_Actor(ref queryList, Web, peActor);

                ReportsQuery.CreateQuery_WFRestricted(ref queryList, wfConfidential);
                if (!String.IsNullOrEmpty(peCreated))
                {
                    SPUser user = Web.EnsureUser(peCreated);
                    ReportsQuery.CreateQuery_WFCreatedBy(ref queryList, user);
                }

                if (queryList.Count.Equals(0))
                    sb.Append("<Where><IsNotNull><FieldRef Name='FileRef' /></IsNotNull></Where>");
                else if (queryList.Count.Equals(1))
                {
                    sb.Append("<Where>");
                    sb.Append(queryList[0]);
                    sb.Append("</Where>");
                }
                else
                {
                    sb.Append("<Where>");
                    sb.Append(ReportsQuery.CreateWhereClause("And", queryList));
                    sb.Append("</Where>");
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateUIQueryModule: " + ex.Message, sb.ToString());
            }

            return sb.ToString();
        }
        
        /// <summary>
        /// Send notification e-mails according to urgent not urgent rules
        /// </summary>
        /// <param name="user"></param>
        /// <param name="web"></param>
        /// <param name="wfid"></param>
        /// <param name="subject"></param>
        /// <param name="parameters"></param>
        /// <param name="urgentCode"></param>
        private void SendEmail(string users, SPWeb web, string templateName, string period, string path, string fileName,  Dictionary<string, string> parameters)
        {
            string errorMessage = string.Empty;
            string[] usersSend;
            SPUser user;
            string emails = String.Empty;
            try
            {
                if (users != null)
                {
                    //Get the Sharepoint SMTP information from the SPAdministrationWebApplication
                    string smtpServer = SPAdministrationWebApplication.Local.OutboundMailServiceInstance.Server.Address;
                    string smtpFrom = SPAdministrationWebApplication.Local.OutboundMailSenderAddress;

                    //Create the mail message and supply it with from and to info
                    //Find users e-mail
                    usersSend = users.Split(',');
                    for(int i=0;i<usersSend.Length;i++)
                    {
                        user = web.EnsureUser(usersSend[i]);

                        if (user != null && !String.IsNullOrEmpty(user.Email))
                            emails += (String.IsNullOrEmpty(emails)) ? user.Email : "," + user.Email;
                    }
                    
                    if (!String.IsNullOrEmpty(emails) && parameters.ContainsKey("E-mail Report Template Text") && parameters.ContainsKey("E-mail Report Template Subject"))
                    {
                        MailMessage mailMessage = new MailMessage(smtpFrom, emails);
                        string emailSubject = parameters["E-mail Report Template Subject"];
                        string emailText = parameters["E-mail Report Template Text"];

                        emailSubject = emailSubject.Replace("[Template Name]", templateName);
                        emailText = emailText.Replace("[Template Name]", templateName).Replace("[Report Period]", period);

                        //Set the subject and body of the message
                        mailMessage.Subject = emailSubject;
                        mailMessage.Body = emailText;

                        //Download the content of the file with a WebClient
                        WebClient webClient = new WebClient();

                        //Supply the WebClient with the network credentials of our user
                            
                        webClient.Credentials = CredentialCache.DefaultNetworkCredentials;
                        
                        //Download the byte array of the file
                        //byte[] data = webClient.DownloadData(insert_ attachment_url);
                        
                        byte[] data = System.IO.File.ReadAllBytes(path + fileName);
                        //Dump the byte array in a memory stream because
                        //we can write it to our attachment
                        MemoryStream memoryStreamOfFile = new MemoryStream(data);

                        //Add the attachment
                        mailMessage.Attachments.Add(new System.Net.Mail.Attachment(memoryStreamOfFile, fileName));

                        //Create the SMTP client object and send the message
                        SmtpClient smtpClient = new SmtpClient(smtpServer);
                        smtpClient.Send(mailMessage);                    
                    }
    
                }                
            }
            catch (Exception ex)
            {
                JobUtilities.SaveErrorsLog(web, "SendEmail", ex.Message);                
            }
        }
    }
}
