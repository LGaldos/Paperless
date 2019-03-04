using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using Microsoft.SharePoint.WebControls;
using System.Data;
using Microsoft.SharePoint;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using Microsoft.SharePoint.Utilities;
using System.ComponentModel;
using System.IO;


namespace ESMA.Paperless.Reports.v16.RSWorkflowReports
{
    class DataManagement
    {
        public enum ActionsEnum
        {
            [Description("Action re-assigned")]
            ActorReAssigned,
            [Description("Cancelled")]
            Cancelled,
            [Description("Commented")]
            Commented,
            [Description("Deleted")]
            Deleted,
            [Description("Field changed")]
            FieldChanged,
            [Description("Finished")]
            Finished,
            [Description("Launched")]
            Launched,
            [Description("Rejected")]
            Rejected,
            [Description("Saved")]
            Saved,
            [Description("Signed")]
            Signed,
            [Description("New document version")]
            NewDocumentVersion,
            [Description("Document uploaded")]
            NewDocument,
            [Description("Document removed")]
            DocumentRemoved,
            [Description("All")]
            All,
            [Description("Closed")]
            Closed

        }


        public const string STATE_REPORT_NEW = "ReportNew";
        public const string STATE_REPORT_RESULT = "ReportResult";
        public const string STATE_REPORT_CUSTOM = "ReportCustom";
        public const string STATE_TEMPLATE_NEW = "TemplateNew";
        public const string STATE_TEMPLATE_SELECT = "TemplateSelect";
        public const string STATE_TEMPLATE_SAVE = "TemplateSave";
        public const string STATE_NOACCESS = "NoAccess";


        #region <COLUMNS REPORTS>

        public static Dictionary<string, string> GetStepsColumns(SPWeb Web, Dictionary<string, string> parameters)
        {
            Dictionary<string, string> assignedToDictionary = new Dictionary<string, string>();
            int cont = 0;

            try
            {

                SPList list = Web.GetListFromWebPartPageUrl(Web.Url + "/Lists/WFStepDefinitions/AllItems.aspx");

                if (list != null)
                {
                    SPQuery query = new SPQuery();
                    query.ViewFields = "<FieldRef Name=\"StepNumber\"/>";
                    query.Query = "<Where><IsNotNull><FieldRef Name='StepNumber' /></IsNotNull></Where><OrderBy><FieldRef Name='StepNumber' Ascending='False' /></OrderBy>";
                    SPListItemCollection itemCollection = list.GetItems(query);
                    SPListItem item = null;

                    if (itemCollection.Count > 0)
                    {
                        item = itemCollection[0];

                        if (item["StepNumber"] != null)
                            cont = Convert.ToInt32(item["StepNumber"].ToString());
                    }
                }

                for (int i = 1; i <= cont; i++)
                {
                    assignedToDictionary.Add(i.ToString() + "_1", "Step " + i.ToString() + "<br/> Assigned To");
                    assignedToDictionary.Add(i.ToString() + "_2", "<br/>Role");
                    assignedToDictionary.Add(i.ToString() + "_3", "<br/>Days to sign/reject");
                    assignedToDictionary.Add(i.ToString() + "_4", "<br/>Signed date");
                    assignedToDictionary.Add(i.ToString() + "_5", "<br/>Rejected date");
                    assignedToDictionary.Add(i.ToString() + "_6", "<br/>Rejected comment");
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("  GetStepsColumns() - Max. StepNumber " + cont + " " + ex.Source, ex.Message);
            }

            return assignedToDictionary;
        }

        /// <summary>
        /// Default Report Columns in case not defined as a configuration parameter
        /// </summary>
        public static Dictionary<string, string> GetHeaderColumns()
        {
            try
            {
                Dictionary<string, string> reportColumnsDictionary = new Dictionary<string, string>();

                reportColumnsDictionary.Add("WFID", "Workflow ID");
                reportColumnsDictionary.Add("WFSubject", "Workflow Subject");
                reportColumnsDictionary.Add("WFStatus", "Workflow Status");
                reportColumnsDictionary.Add("WFType", "Workflow Type");
                reportColumnsDictionary.Add("Author", "Created by");
                reportColumnsDictionary.Add("Created", "Created");
                reportColumnsDictionary.Add("GFPersonalFile", "Personal File");
                reportColumnsDictionary.Add("GFOpenAmountRAL", "Open Amount RAL");
                reportColumnsDictionary.Add("GFAmountCurrentYear", "Amount Current Year");
                reportColumnsDictionary.Add("GFAmountNextYear", "Amount Next Year");
                reportColumnsDictionary.Add("GFAmountToCancel", "Amount To Cancel");
                reportColumnsDictionary.Add("GFJustification", "GFJustification");
                reportColumnsDictionary.Add("GFGLAccount", "GL Account");
                reportColumnsDictionary.Add("GFBudgetLine", "Budget Line");
                reportColumnsDictionary.Add("Created", "Created");
                reportColumnsDictionary.Add("AssignedPerson", "Assigned Person");
                reportColumnsDictionary.Add("StepNumber", "Step Number");                
                reportColumnsDictionary.Add("DaysToClose", "Days To Close");
                reportColumnsDictionary.Add("ConfidentialWorkflow", "Restricted");
                reportColumnsDictionary.Add("Urgent", "Urgent");
                reportColumnsDictionary.Add("WFDeadline", "Deadline");

                return reportColumnsDictionary;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" GetHeaderColumns() - " + ex.Source, ex.Message);
                return null;
            }

        }


        /// <summary>
        /// Get Default Report Columns
        /// </summary>
        public static Dictionary<string, string> GetHeaderColumns(SPWeb web, Dictionary<string, string> parameters)
        {
            try
            {
                Dictionary<string, string> reportColumnsDictionary = new Dictionary<string, string>();

                if (!parameters.ContainsKey("Report Columns"))
                {
                    reportColumnsDictionary = GetHeaderColumns();
                }
                else
                {
                    string columns = parameters["Report Columns"];
                    foreach (string column in columns.Split(new string[] { ";#" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] field = column.Split('|');
                        if (field.Length > 1)
                        {
                            reportColumnsDictionary.Add(field[0], field[1]);
                        }
                        else
                        {
                            SPField spField = web.Fields.GetFieldByInternalName(field[0]);
                            reportColumnsDictionary.Add(field[0], spField.Title);
                        }
                    }
                }

                return reportColumnsDictionary;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" GetHeaderColumns() - " + ex.Source, ex.Message);
                return null;
            }

        }

        #endregion


        #region <QUERYs (ALI)>        

        public static string GetActorGroup(string titleWF, string numStep, Dictionary<string, string> parameters, SPWeb Web)
        {
            string groupValue = "";
            string groupName = "";
            string domain = "";
            try
            {
                SPList list = Web.Lists["RS Workflow Step Definitions"];

                if (list != null)
                {
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><And><Eq><FieldRef Name='Title'/><Value Type='Text'>" + titleWF + "</Value></Eq><Eq><FieldRef Name='StepNumber'/><Value Type='Number'>" + numStep + "</Value></Eq></And></Where>";

                    SPListItemCollection itemCollection = list.GetItems(query);

                    if (itemCollection != null && itemCollection.Count.Equals(1))
                    {
                        groupValue = itemCollection[0]["WFGroup"].ToString().Split('#')[1];
                    }

                    groupName = groupValue.Replace(parameters["Domain"] + "\\", string.Empty);
                    groupName = GetDefinitionGroupName(groupName, parameters);
                }

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" GetActorGroup() - " + ex.Source, ex.Message);
            }
            return groupName;
        }

        public static string GetDefinitionGroupName(string ADGroupName, Dictionary<string, string> parameters)
        {
            string groupname = string.Empty;

            try
            {
                List<string> keyList = new List<string>(parameters.Keys);

                if (keyList.Contains(ADGroupName))
                    groupname = parameters.FirstOrDefault(x => x.Key == ADGroupName).Value;
                else
                    groupname = ADGroupName;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" GetDefinitionGroupName() - " + ex.Source, ex.Message);

            }

            return groupname;
        }

        //Logs List (905)
        public static DataTable GetLogsResultsTable(string camlQuery, string webUrl)
        {
            DataTable tblResult = null;

            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(webUrl))
                    {
                        SPWeb Web = Site.OpenWeb();

                        SPSiteDataQuery siteDataQueryLogs = new SPSiteDataQuery();
                        siteDataQueryLogs.Lists = "<Lists ServerTemplate='905' />";

                        siteDataQueryLogs.ViewFields = "<FieldRef Name='WFID' Nullable='TRUE'/>";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='StepNumber' Nullable='TRUE'/>";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='ActionTaken' Nullable='TRUE'/>";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='AssignedPerson' Nullable='TRUE'/>";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='WFStatus' Nullable='TRUE'/>";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='Created' Type='DateTime' Nullable='TRUE' />";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='GFPersonalFile' Nullable='TRUE' />";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='GFOpenAmountRAL' Nullable='TRUE' />";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='GFAmountCurrentYear' Nullable='TRUE' />";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='GFAmountNextYear' Nullable='TRUE' />";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='GFAmountNextYear' Nullable='TRUE' />";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='GFAmountToCancel' Nullable='TRUE' />";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='GFJustification' Nullable='TRUE' />";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='GFGLAccount' Nullable='TRUE' />";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='GFBudgetLine' Nullable='TRUE' />";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='WFSubject' Nullable='TRUE' />";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='ActionDetails' Nullable='TRUE'/>";
                        siteDataQueryLogs.ViewFields += "<FieldRef Name='WFComment' Nullable='TRUE'/>";

                        siteDataQueryLogs.Webs = "<Webs Scope='SiteCollection' />";
                        siteDataQueryLogs.Query = camlQuery;
                        siteDataQueryLogs.QueryThrottleMode = SPQueryThrottleOption.Override;

                        tblResult = Web.GetSiteData(siteDataQueryLogs);
                    }

                });
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GetLogsResultsTable() - " + ex.Source, ex.Message);
                tblResult = null;
            }

            return tblResult;
        }


        public static string GetLastReportID(SPWeb Web)
        {
            string rpId = string.Empty;

            try
            {
                SPList reportList = Web.Lists["RS Reports Library"];

                SPQuery query = new SPQuery();
                query.Query = "<Where><IsNotNull><FieldRef Name='RPID'/></IsNotNull></Where>"
                    + "<OrderBy><FieldRef Name='Created' Ascending='False' /></OrderBy>";

                SPListItemCollection itemCollection = reportList.GetItems(query);


                if (itemCollection.Count == 0)
                    rpId = "1";
                else
                {
                    SPFile file = itemCollection[0].File;
                    rpId = (Int32.Parse(file.Item["RPID"].ToString()) + 1).ToString();

                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GetLastReportID() - rpID: " + rpId + ". " + ex.Source, ex.Message);

            }
            return rpId;

        }

        #endregion


        #region <EXCEL>
        public static void ReportExport(SPWeb web, DataTable exportTable, GridView gvResults, SPUser loggedUser, Dictionary<string, string> parameters)
        {
            Dictionary<string, string> gvResultsColumns = new Dictionary<string, string>();
            for (int i = 0; i < gvResults.Columns.Count - 1; i++)
            {
                BoundField columnResults = (BoundField)gvResults.Columns[i];
                gvResultsColumns.Add(columnResults.DataField, columnResults.HeaderText);
            }
            ReportExport(web, exportTable, gvResultsColumns, loggedUser, parameters);
        }

        public static void ReportExport(SPWeb web, DataTable exportTable, Dictionary<string, string> resultsColumns, SPUser loggedUser, Dictionary<string, string> parameters, SPListItem reportItem = null)
        {
            try
            {
                string fileName = "Report " + DateTime.Now.ToShortDateString().Replace('/', '_') + "-" + DateTime.Now.ToLongTimeString().Replace(':', '-') + "-" + loggedUser.ID + ".xlsx";

                string rpId = DataManagement.GetLastReportID(web);
                string tempFolder = parameters["RS Path Temp"];
                string filePath = System.IO.Path.Combine(tempFolder, fileName);

                DataManagement.ExportToExcel(exportTable, resultsColumns, filePath);

                if (File.Exists(filePath))
                {
                    UploadFileToLibrary(filePath, rpId, false, loggedUser, web, parameters, reportItem);
                    Methods.DeleteFile(filePath);

                    fileName = rpId + ".html";
                    DataManagement.ExportToHTML(exportTable, resultsColumns, filePath);
                    UploadFileToLibrary(filePath, rpId, true, loggedUser, web, parameters, reportItem);
                    Methods.DeleteFile(filePath);
                }
                else
                    Methods.SaveErrorsLog("The path '" + filePath + "' does not exist.", null);

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("ReportExport() - " + ex.Source, ex.Message);
            }
        }

        public static void ExportToExcel(DataTable dt, GridView gvResults, string filePath, Dictionary<string, string> parameters)
        {
            try
            {
                ExcelManagement.CreatePackage(dt, gvResults, filePath);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("ExportToExcel() - " + ex.Source, ex.Message);
            }
        }

        public static void ExportToExcel(DataTable dt, Dictionary<string, string> resultsColumns, string filePath)
        {
            try
            {
                ExcelManagement.CreatePackage(dt, resultsColumns, filePath);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("ExportToExcel() - " + ex.Source, ex.Message);
            }
        }

        public static void ExportToHTML(DataTable dt, Dictionary<string, string> resultsColumns, string filePath)
        {
            try
            {
                StringBuilder sbHtml = new StringBuilder();
                StreamWriter sw = new StreamWriter(filePath);

                //Styles
                sbHtml.Append("<html><head>");
                sbHtml.Append("<link rel=\"stylesheet\" type=\"text/css\" href=\"/_layouts/15/1033/styles/Themable/corev15.css?rev=OqAycmyMLoQIDkAlzHdMhQ%3D%3D\"/>");
                sbHtml.Append("<link rel=\"stylesheet\" type=\"text/css\" href=\"/_layouts/15/ESMA.Paperless.Design.v16/css/RSComun.css\">");

                sbHtml.Append("<style>");
                sbHtml.Append(".result_grid tr {font-size: 13px;}");
                sbHtml.Append("</style>");

                /*sbHtml.Append("<style type=\"text/css\">");
                sbHtml.Append(".header_background{background:  url(\"/_layouts/images/RSTabBackground_big.png\");border-bottom:1px solid #a3a3a3 ! important;color: #5CAFCD; font-size: 1em; font-weight: bold; height: 20px;margin-bottom:0.5em;padding-left:0.6em;	padding-top:0.6em;text-align: left;}");
                sbHtml.Append(".result_grid_even{background-color:#eeeeee!important; text-decoration:none!important; border-style:solid; border-width:1px;}");
                sbHtml.Append(".result_grid table{ border-style:solid; border-width:1px;}");
                sbHtml.Append(".result_grid div table{border-style:solid; border-width:1px;}");
                sbHtml.Append(".result_grid div table tbody{ border-style:solid; border-width:1px;}");
                sbHtml.Append(".result_grid tr{border-style:solid;border-width:1px; border: gainsboro 1px;}");
                sbHtml.Append(".result_grid th{border-style:solid;border-width:1px;}");
                sbHtml.Append(".result_grid td{border-style:solid;border-width:1px;}");
                sbHtml.Append(".result_grid tbody{border-style:solid;border-width:1px;}");
                sbHtml.Append(".result_grid tr td{border-style:solid;border-width:1px;border:gainsboro 1px;}");
                sbHtml.Append(".result_grid tr th{border-style:solid;border-width:1px;border:solid 1px;}");
                sbHtml.Append(".grid_report{font-size:8pt; font-family: verdana,arial,helvetica,sans-serif;}");
                sbHtml.Append("</style>");*/
                sbHtml.Append("</head><body>");

                //Table gridview
                sbHtml.Append("<div class=\"result_grid\" style=\"height: 800px;\">");
                sbHtml.Append("<table class=\"grid_report\" cellspacing=\"0\" align=\"Center\" rules=\"all\" border=\"1\" ");
                sbHtml.Append("style=\"width:" + (resultsColumns.Count * 100) + "px;border-collapse:collapse;\">");
                sbHtml.Append("<tr class=\"header_background\">");

                foreach (KeyValuePair<String, String> kvp in resultsColumns)
                {
                    sbHtml.Append("<th scope=\"col\">");
                    sbHtml.Append(kvp.Value);
                    sbHtml.Append("</th>");
                }

                sbHtml.Append("</tr>");

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (i % 2 == 0)
                        sbHtml.Append("<tr>");
                    else
                        sbHtml.Append("<tr class=\"result_grid_even\">");
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        if (dt.Columns[j].ColumnName != "Steps")
                        {
                            if (dt.Rows[i][j].ToString() == "")
                                sbHtml.Append("<td>&nbsp;</td>");

                            else
                                sbHtml.Append("<td>" + DateFormat(dt.Rows[i][j].ToString()) + "</td>");

                        }
                    }
                    sbHtml.Append("</tr>");

                }
                sbHtml.Append("</table></div></body></html>");

                sw.Write(sbHtml.ToString());
                sw.Close();

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" ExportToHTML() - " + ex.Source, ex.Message);
            }
        }        

        public static Boolean UploadFileToLibrary(string filePath, string rpId, bool isHtml, SPUser loggedUser, SPWeb web, Dictionary<string, string> parameters, SPListItem reportItem = null)
        {
            FileStream fs = null;

            try
            {
                //Open file to read
                fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                fs.Flush();
                fs.Position = 0;
                byte[] content = new byte[(int)fs.Length];
                fs.Read(content, 0, (int)fs.Length);

                fs.Close();

                web.AllowUnsafeUpdates = true;

                SPList reportLibrary = web.Lists["RS Reports Library"];
                SPFolder folder = reportLibrary.RootFolder;
                SPFile newDocument = null;

                if (isHtml)
                {
                    newDocument = folder.Files.Add(rpId + ".html", content, true);
                    newDocument.Item["Report ID"] = rpId;
                }
                else
                {
                    //Upload Excel File and properties 
                    newDocument = folder.Files.Add(Path.GetFileName(filePath), content, true);

                    newDocument.Item["Report ID"] = rpId;

                    SPFieldUrlValue urlValue = new SPFieldUrlValue();
                    urlValue.Description = "View Report";
                    urlValue.Url = web.Url + parameters["View Reports Page"] + "?rpid=" + rpId;
                   

                    newDocument.Item["RPLink"] = urlValue;

                    if (reportItem == null)
                    {
                        if (HttpContext.Current.Session["ReportFirstDate"] != null)
                            newDocument.Item["RPFirstDate"] = HttpContext.Current.Session["ReportFirstDate"].ToString();

                        if (HttpContext.Current.Session["ReportLastDate"] != null && HttpContext.Current.Session["ReportLastDate"].ToString() != "")
                            newDocument.Item["RPLastDate"] = HttpContext.Current.Session["ReportLastDate"].ToString();

                        if (HttpContext.Current.Session["ReportType"] != null)
                            newDocument.Item["RPTypes"] = HttpContext.Current.Session["ReportType"];

                        if (HttpContext.Current.Session["ReportStatus"] != null)
                            newDocument.Item["RPStatus"] = HttpContext.Current.Session["ReportStatus"];

                        if (HttpContext.Current.Session["ReportRoles"] != null)
                            newDocument.Item["RPRoles"] = HttpContext.Current.Session["ReportRoles"];

                        if (HttpContext.Current.Session["ReportActor"] != null)
                            newDocument.Item["RPActors"] = HttpContext.Current.Session["ReportActor"];

                        if (HttpContext.Current.Session["ReportConfidential"] != null)
                            newDocument.Item["RPConfidential"] = HttpContext.Current.Session["ReportConfidential"];

                        if (HttpContext.Current.Session["ReportCreated"] != null)
                            newDocument.Item["RPCreatedBy"] = HttpContext.Current.Session["ReportCreated"];

                        /* NEW FIELDS */
                        /* ---------- */
                        if (HttpContext.Current.Session["ReportGFPersonalFile"] != null)
                            newDocument.Item["GFPersonalFile"] = HttpContext.Current.Session["ReportGFPersonalFile"];

                        if (HttpContext.Current.Session["ReportGFOpenAmountRAL"] != null)
                            newDocument.Item["GFOpenAmountRAL"] = HttpContext.Current.Session["ReportGFOpenAmountRAL"];

                        if (HttpContext.Current.Session["ReportAmountCurrentYear"] != null)
                            newDocument.Item["GFAmountCurrentYear"] = HttpContext.Current.Session["ReportAmountCurrentYear"];

                        if (HttpContext.Current.Session["ReportAmountNextYear"] != null)
                            newDocument.Item["GFAmountNextYear"] = HttpContext.Current.Session["ReportAmountNextYear"];

                        if (HttpContext.Current.Session["ReportAmountToCancel"] != null)
                            newDocument.Item["GFAmountToCancel"] = HttpContext.Current.Session["ReportAmountToCancel"];

                        if (HttpContext.Current.Session["ReportJustification"] != null)
                            newDocument.Item["GFJustification"] = HttpContext.Current.Session["ReportJustification"];

                        if (HttpContext.Current.Session["ReportGLAccount"] != null)
                            newDocument.Item["GFGLAccount"] = HttpContext.Current.Session["ReportGLAccount"];

                        if (HttpContext.Current.Session["ReportBudgetLine"] != null)
                            newDocument.Item["GFBudgetLine"] = HttpContext.Current.Session["ReportBudgetLine"];

                        if (HttpContext.Current.Session["ReportWFSubject"] != null)
                            newDocument.Item["WFSubject"] = HttpContext.Current.Session["ReportWFSubject"];


                        if (HttpContext.Current.Session["ReportFreeText"] != null)
                            newDocument.Item["RPFreeText"] = HttpContext.Current.Session["ReportFreeText"];

                    }
                    else
                    {
                        newDocument.Item["RPFirstDate"] = reportItem["RPFirstDate"];
                        newDocument.Item["RPLastDate"] = reportItem["RPLastDate"];
                        newDocument.Item["RPTypes"] = reportItem["RPTypes"];
                        newDocument.Item["RPStatus"] = reportItem["RPStatus"];
                        newDocument.Item["RPRoles"] = reportItem["RPRoles"];
                        newDocument.Item["RPActors"] = reportItem["RPActors"];
                        newDocument.Item["RPConfidential"] = reportItem["RPConfidential"];
                        newDocument.Item["RPCreatedBy"] = reportItem["RPCreatedBy"];

                        newDocument.Item["GFPersonalFile"] = reportItem["GFPersonalFile"];
                        newDocument.Item["GFOpenAmountRAL"] = reportItem["GFOpenAmountRAL"];
                        newDocument.Item["GFAmountCurrentYear"] = reportItem["GFAmountCurrentYear"];
                        newDocument.Item["GFAmountNextYear"] = reportItem["GFAmountNextYear"];
                        newDocument.Item["GFAmountToCancel"] = reportItem["GFAmountToCancel"];
                        newDocument.Item["GFJustification"] = reportItem["GFJustification"];
                        newDocument.Item["GFGLAccount"] = reportItem["GFGLAccount"];
                        newDocument.Item["GFBudgetLine"] = reportItem["GFBudgetLine"];
                        newDocument.Item["WFSubject"] = reportItem["WFSubject"];

                        newDocument.Item["RPFreeText"] = reportItem["RPFreeText"];
                    }
                }
                SPFieldUserValue oUser = new SPFieldUserValue(web, loggedUser.ID, Permissions.GetUsernameFromClaim(loggedUser.LoginName));
                newDocument.Item["Author"] = oUser;
                newDocument.Item["Editor"] = oUser;

                newDocument.Item.Update();
                newDocument.Update();
                return true;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("UploadFileToLibrary() - " + ex.Source, ex.Message);
                return false;
            }
        }

        #endregion


        public static void DeleteUserTemplate(SPWeb Web, string idTemplate, string userName)
        {
            try
            {
                SPList lstTemplate = Web.Lists["RS Reports Templates"];
                SPQuery query = new SPQuery();
                SPListItemCollection coll;
                string[] users;
                string newUsers = "";
                SPListItem itemTemplate;

                query.Query = "<Where><Eq><FieldRef Name='ID' LookupId='True' /><Value Type='Integer'>" + idTemplate + "</Value></Eq></Where>";
                coll = lstTemplate.GetItems(query);

                if (coll != null || coll.Count > 0)
                {
                    itemTemplate = coll[0];
                    if (itemTemplate["RP Share Users"] != null && itemTemplate["RP Share Users"].ToString() != "")
                    {
                        users = itemTemplate["RP Share Users"].ToString().Split(';');

                        foreach (string user in users)
                        {
                            if (user != userName)
                            {
                                newUsers = newUsers + user + ";";
                            }

                        }
                        if (newUsers != "")
                            newUsers = newUsers.Substring(0, newUsers.Length - 1);

                        itemTemplate["RP Share Users"] = newUsers;
                        itemTemplate.Update();
                        lstTemplate.Update();
                    }
                }



            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" DeleteUserTemplate() - " + ex.Source, ex.Message);
            }

        }

        public static string DateFormat(string value)
        {
            DateTime tempDate;
            bool isDate = DateTime.TryParse(value, out tempDate);
            return (isDate) ? tempDate.ToString("dd/MM/yyyy") : value;
        }
    }
}
