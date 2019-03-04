using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESMA.Paperless.Reports.v16.RSWorkflowReports
{
    class ReportsResults
    {
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
                reportColumnsDictionary.Add("Created", "Launch Date");

                reportColumnsDictionary.Add("AssignedPerson", "Assigned Person");
                reportColumnsDictionary.Add("StepNumber", "Step Number");
                reportColumnsDictionary.Add("DaysToClose", "Days To Close");
                reportColumnsDictionary.Add("ConfidentialWorkflow", "Restricted");
                reportColumnsDictionary.Add("Urgent", "Urgent");
                reportColumnsDictionary.Add("WFDeadline", "Deadline");

                reportColumnsDictionary.Add("GFPersonalFile", "Personal File");
                reportColumnsDictionary.Add("GFOpenAmountRAL", "Open Amount RAL");
                reportColumnsDictionary.Add("GFAmountCurrentYear", "Amount Current Year");
                reportColumnsDictionary.Add("GFAmountNextYear", "Amount Next Year");
                reportColumnsDictionary.Add("GFAmountToCancel", "Amount To Cancel");
                reportColumnsDictionary.Add("GFJustification", "Justification");
                reportColumnsDictionary.Add("GFGLAccount", "GL Account");
                reportColumnsDictionary.Add("GFBudgetLine", "Budget Line");

                return reportColumnsDictionary;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" GetHeaderColumns() - " + ex.Source, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Get Default Report Columns from the RS Configuration Parameter
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
        /// Create result grid columns and datatable structure
        /// </summary>
        public static void CreateResultTable(ref DataTable resultTable, Dictionary<string, string> columnsNameDictionary, Dictionary<string, string> columnsStepsDictionary)
        {

            try
            {
                resultTable = new DataTable();

                foreach (KeyValuePair<String, String> kvp in columnsNameDictionary)
                {
                    CreateColumn(ref resultTable, kvp.Key, kvp.Value);
                }

                if (columnsStepsDictionary != null)
                {
                    foreach (KeyValuePair<String, String> kvp in columnsStepsDictionary)
                    {
                        CreateColumn(ref resultTable, kvp.Key, kvp.Value);
                    }
                }

                CreateColumn(ref resultTable, "Modified", "Modified");
                CreateColumn(ref resultTable, "Delayed", "Delayed");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" CreateResultTable() - " + ex.Source, ex.Message);
            }
        }

        private static void CreateColumn(ref DataTable resultTable, string columnName, string columnCaption)
        {

            try
            {
                DataColumn column = new DataColumn();
                column.ColumnName = columnName;
                column.Caption = columnCaption;

                switch (columnName)
                {
                    case "Created":
                    case "WFDeadline":
                        column.DataType = Type.GetType("System.DateTime");
                        break;
                }
                /*if (columnName.Equals("WFID"))
                    column.DataType = Type.GetType("System.Int32");
                else if (columnName.Equals("WFDeadline") || columnName.Equals("LaunchDate"))
                    column.DataType = Type.GetType("System.DateTime");
                else
                    column.DataType = Type.GetType("System.String");*/

                resultTable.Columns.Add(column);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" CreateColumn() - " + ex.Source, ex.Message);
            }
        }

        //WF Libraries -> Filters from UI
        public static void UIValuesSearch(string queryToExecute, SPWeb Web, ref DataTable resultTableGeneral, Dictionary<string, string> resultColumns)
        {
            try
            {
                SPSiteDataQuery siteDataQuery = new SPSiteDataQuery();
                siteDataQuery.Lists = "<Lists ServerTemplate='906' />"; //Workflow Libraries
                siteDataQuery.ViewFields = "";
                foreach (KeyValuePair<String, String> kvp in resultColumns)
                {
                    siteDataQuery.ViewFields += String.Format("<FieldRef Name='{0}' Nullable='TRUE'/>", kvp.Key);
                }

                siteDataQuery.ViewFields += "<FieldRef Name='Modified' />";
                siteDataQuery.Webs = "<Webs Scope='SiteCollection' />";

                siteDataQuery.Query = queryToExecute; // + "<OrderBy><FieldRef Name='WFID' Ascending='FALSE' /></OrderBy>";

                siteDataQuery.QueryThrottleMode = SPQueryThrottleOption.Override;

                DataTable dtAux = Web.GetSiteData(siteDataQuery);

                foreach (DataRow drow in dtAux.Rows)
                {
                    string wfid = Double.Parse(drow["WFID"].ToString()).ToString();
                    AddNewRow_DataRowView_Common(ref resultTableGeneral, Web, drow, wfid, resultColumns);
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("UIValuesSearch(): " + ex.Message, ex.StackTrace);
            }
        }


        public static void GetResultTableKeywords(SPWeb Web, ref DataTable resultTableCommon, string keyword, Dictionary<string, string> resultColumns)
        {

            try
            {
                //All General Fields (WF Library)
                Dictionary<string, SPField> GFieldsDictionary = ReportsQuery.GetGFsDictionary();
                string queryCommonToExecute = CreateGFsQueryModule(Web, keyword, GFieldsDictionary);

                KeyWordOnGFsSearch(queryCommonToExecute, Web, ref resultTableCommon, resultColumns, GFieldsDictionary);
                
                KeyWordOnCommentsSearch(Web, ref resultTableCommon, keyword, resultColumns); //Comments (Logs List)
                KeyWordOnDocumentSearch(Web, ref resultTableCommon, keyword, resultColumns); //File Name (WF Library)

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GetResultTableKeywords: " + ex.Message, null);
            }

        }

        private static string CreateGFsQueryModule(SPWeb web, string keyword, Dictionary<string, SPField> GFieldsDictionary)
        {
            string queryToExecute = string.Empty;
            StringBuilder sb = new StringBuilder();

            try
            {
                List<string> queryList = new List<string>();


                //-----------------------------------------------------------
                //Comun GFs
                //-----------------------------------------------------------
                ReportsQuery.CreateQueryKeyword_Restricted(ref queryList, keyword); //Restricted

                //Urgent -> Use the UI
                ReportsQuery.CreateQueryKeyword_WFSubject(ref queryList, keyword); //WF Subject
                ReportsQuery.CreateQueryKeyword_Amount(ref queryList, keyword); //Amount
                ReportsQuery.CreateQueryKeyword_LinkToWF(ref queryList, keyword); //Link To WF

                //ReportsQuery.CreateQuery_PersonalFile(ref queryList, keyword);  // Personal File?
                ReportsQuery.CreateQueryKeyword_OpenAmountRAL(ref queryList, keyword); // Open Amount RAL
                ReportsQuery.CreateQueryKeyword_AmountCurrentYear(ref queryList, keyword); // Amount Current Year
                ReportsQuery.CreateQueryKeyword_AmountNextYear(ref queryList, keyword); // Amount Next Year
                ReportsQuery.CreateQueryKeyword_AmountToCancel(ref queryList, keyword); // Amount To Cancel
                
                ReportsQuery.CreateQueryKeyword_BudgetLine(ref queryList, keyword); // Budget Line
                ReportsQuery.CreateQueryKeyword_GLAccount(ref queryList, keyword); // GL Account
                ReportsQuery.CreateQueryKeyword_Justification(ref queryList, keyword); // Justification

                
                //-----------------------------------------------------------
                //Especific GFs
                //-----------------------------------------------------------
                ReportsQuery.CreateDinamicQueryForEspecificGFs(ref queryList, GFieldsDictionary, keyword);

                //Concat the query
                sb.Append("<Where>");
                sb.Append(ReportsQuery.CreateWhereClause("Or", queryList));
                sb.Append("</Where>");

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateGFsQueryModule: " + ex.Message, sb.ToString());
            }

            return queryToExecute;
        }

        //WF Libraries -> GFs
        private static void KeyWordOnGFsSearch(string queryToExecute, SPWeb Web, ref DataTable resultTableGeneral, Dictionary<string, string> resultColumns, Dictionary<string, SPField> GFieldsDictionary)
        {

            try
            {

                SPSiteDataQuery siteDataQuery = new SPSiteDataQuery();
                siteDataQuery.Lists = "<Lists ServerTemplate='906' />"; //Workflow Libraries

                siteDataQuery.ViewFields = "<FieldRef Name='WFID' Type='Number' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='WFSubject' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='Amount' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='WFStatus' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='WFType' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='Created' Type='DateTime' Nullable='TRUE' />";
                siteDataQuery.ViewFields += "<FieldRef Name='Urgent' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='WFDeadline' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='GFABACCommitment' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='Modified' Type='DateTime'  Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='WFActorsSignedRole' Nullable='TRUE'  />";
                siteDataQuery.ViewFields += "<FieldRef Name='GFStaffName' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='GFVAT' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='ConfidentialWorkflow' Nullable='TRUE'/>";

                /*siteDataQuery.ViewFields += "<FieldRef Name='GFOpenAmountRAL' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='GFAmountCurrentYear' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='GFAmountNextYear' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='GFAmountToCancel' Nullable='TRUE'/>";*/

                foreach (KeyValuePair<string, SPField> entry in GFieldsDictionary)
                {
                    siteDataQuery.ViewFields += "<FieldRef Name='" + entry.Key + "' Nullable='TRUE'/>";
                }


                siteDataQuery.Webs = "<Webs Scope='SiteCollection' />";
                siteDataQuery.Query = queryToExecute + "<OrderBy><FieldRef Name='WFID' Ascending='FALSE' /></OrderBy>";

                siteDataQuery.QueryThrottleMode = SPQueryThrottleOption.Override;

                DataTable dtAux = Web.GetSiteData(siteDataQuery);
                DataView dvAux = dtAux.AsDataView();

                foreach (DataRow drow in dtAux.Rows)
                {
                    string wfid = Double.Parse(drow["WFID"].ToString()).ToString();
                    AddNewRow_DataRowView_Common(ref resultTableGeneral, Web, drow, wfid, resultColumns);
                }

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("KeyWordOnGFsSearch: " + ex.Message, ex.StackTrace);
            }
        }

        //Logs Lits -> Comments
        private static void KeyWordOnCommentsSearch(SPWeb Web, ref DataTable resultTableGeneral, string keyword, Dictionary<string, string> resultColumns)
        {

            try
            {

                DataTable commentTableAux = SearchComments(keyword);
                DataView dvCommentAux = commentTableAux.AsDataView();


                foreach (DataRowView drow in dvCommentAux)
                {
                    string wfid = Methods.FormatWFID(drow[3].ToString());
                    bool existsRow = CheckIfExistsRow(wfid, resultTableGeneral);

                    if (existsRow.Equals(false))
                    {
                        SPListItem item = Methods.GetWFInformationByWFID(wfid, Web);

                        if (item != null)
                        {
                            AddNewRow_ListItem_Keyword(ref resultTableGeneral, Web, item, wfid, resultColumns);
                        }
                        else
                            Methods.SaveErrorsLog("Error adding WFID '" + wfid + "'. It does not exist in the RS Workflow History List.", string.Empty);
                    }

                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("KeyWordOnCommentsSearch(): " + ex.Message, "Text -> " + keyword + " - " + ex.StackTrace);
            }
        }

        //WF Libraries -> Document Title
        private static void KeyWordOnDocumentSearch(SPWeb Web, ref DataTable resultTableGeneral, string keyword, Dictionary<string, string> resultColumns)
        {
            try
            {
                SPSiteDataQuery siteDataQuery = new SPSiteDataQuery();
                siteDataQuery.Lists = "<Lists ServerTemplate='906' />";
                siteDataQuery.ViewFields = "<FieldRef Name='WFID' Type='Number' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='FileLeafRef'/><FieldRef Name='ContentType'/>";
                siteDataQuery.Webs = "<Webs Scope='RecursiveAll'/>"; //Recursive: Current site and any subsite (Show all files and all subfolders of all folders.)
                siteDataQuery.Query = "<Where><And><Contains><FieldRef Name='FileLeafRef'/><Value Type='Text'>" + keyword + "</Value></Contains>"
                                     + "<Eq><FieldRef Name='ContentType' /><Value Type='Text'>Workflow Document</Value></Eq></And></Where>"
                                     + "<OrderBy><FieldRef Name='WFID' Ascending='FALSE' /></OrderBy>";
                siteDataQuery.QueryThrottleMode = SPQueryThrottleOption.Override;
                DataTable documentTableAux = Web.GetSiteData(siteDataQuery);
                DataView dvDocumentAux = documentTableAux.AsDataView();

                Methods.SaveErrorsLog("KeyWordOnDocumentSearch - Results: " + documentTableAux.Rows.Count.ToString(), string.Empty);

                foreach (DataRowView drow in dvDocumentAux)
                {
                    string wfid = Methods.FormatWFID(drow[3].ToString());
                    bool existsRow = CheckIfExistsRow(wfid, resultTableGeneral);

                    if (existsRow.Equals(false))
                    {
                        SPListItem item = Methods.GetWFInformationByWFID(wfid, Web);

                        if (item != null)
                        {
                            AddNewRow_ListItem_Keyword(ref resultTableGeneral, Web, item, wfid, resultColumns);
                        }
                        else
                            Methods.SaveErrorsLog("Error adding WFID '" + wfid + "'. It does not exist in the RS Workflow History List.", string.Empty);
                    }

                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("KeyWordOnDocumentSearch: " + ex.Message, ex.StackTrace);
            }
        }

        private static DataTable SearchComments(string keyword)
        {
            DataTable commentTableAux = null;

            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                    {
                        SPWeb Web = Site.OpenWeb();

                        SPSiteDataQuery siteDataQuery = new SPSiteDataQuery();
                        siteDataQuery.Lists = "<Lists ServerTemplate='905' />";
                        siteDataQuery.ViewFields = "<FieldRef Name='WFID' Type='Number' Nullable='TRUE'/>";
                        siteDataQuery.ViewFields += "<FieldRef Name='WorkflowComment' />";
                        siteDataQuery.ViewFields += "<FieldRef Name='ActionTaken' />";
                        siteDataQuery.ViewFields += "<FieldRef Name='ConfidentialWorkflow' />";

                        siteDataQuery.Webs = "<Webs Scope='SiteCollection' />";

                        siteDataQuery.Query = "<Where><And>"
                                             + "<Contains><FieldRef Name='ActionTaken' /><Value Type='Choice'>Commented</Value></Contains>"
                                             + "<Contains><FieldRef Name='WorkflowComment' /><Value Type='Note'>" + keyword + "</Value></Contains></And></Where>";

                        commentTableAux = Web.GetSiteData(siteDataQuery);

                        Methods.SaveErrorsLog("SearchComments - Results: " + commentTableAux.Rows.Count.ToString(), string.Empty);

                        Web.Close();
                        Web.Dispose();
                    }
                });
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("SearchComments(): " + ex.Message, "Text -> " + keyword + " - " + ex.StackTrace);
            }

            return commentTableAux;
        }


        //---------------------------------------------------------------------------
        //ADD ROWS
        //---------------------------------------------------------------------------
        public static void AddNewRow_DataRowView_Common(ref DataTable resultTable, SPWeb Web, DataRow drv, string wfid, Dictionary<string, string> resultColumns)
        {

            try
            {
                DataRow newRecord = resultTable.NewRow();
                foreach (KeyValuePair<String, String> kvp in resultColumns)
                {
                    switch (kvp.Key)
                    {
                        case "WFID":
                            newRecord[kvp.Key] = wfid;
                            break;
                        case "StepNumber":
                            newRecord[kvp.Key] = (drv[kvp.Key].ToString().IndexOf('.') > -1) ? drv[kvp.Key].ToString().Split('.')[0] : drv[kvp.Key].ToString();
                            break;
                        case "AssignedPerson":
                        case "Author":
                            newRecord[kvp.Key] = (drv[kvp.Key] != null && drv[kvp.Key].ToString() != "") ? drv[kvp.Key].ToString().Split('#')[1] : "";
                            break;
                        case "Urgent":
                            newRecord[kvp.Key] = (drv[kvp.Key].ToString().Equals("0")) ? "No" : "Yes";
                            break;
                        case "WFDeadline":
                        case "Created":
                            if (drv[kvp.Key] != null && drv[kvp.Key].ToString() != "")
                                newRecord[kvp.Key] = DateTime.Parse(drv[kvp.Key].ToString());
                            break;
                        case "DaysToClose":
                            // Set daystoclose column
                            /*if (drv["WFStatus"] != null && drv["WFStatus"].ToString().Equals("Closed"))
                            {
                                SPListItem item = Methods.GetWFInformationByWFID(wfid, Web);
                                if (item["DaysToClose"] != null)
                                    newRecord[kvp.Key] = item["DaysToClose"].ToString();
                            }*/
                            break;
                        default:
                            newRecord[kvp.Key] = drv[kvp.Key].ToString();
                            break;
                    }                    
                }

                //Compare deadline date and last date (Modified - Deadline) 
                if (drv["WFDeadline"] != null && drv["WFDeadline"].ToString() != "")
                {
                    TimeSpan ts = DateTime.Parse(drv["Modified"].ToString()) - DateTime.Parse(drv["WFDeadline"].ToString());
                    if (ts.Days > 0)
                        newRecord["Delayed"] = "Yes";
                }
                newRecord["Modified"] = drv["Modified"];

                resultTable.Rows.Add(newRecord);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("AddNewRow_DataRowView_Common(): WFID '" + wfid + "' " + ex.Message, ex.StackTrace);
            }
        }       

        private static void AddNewRow_ListItem_Keyword(ref DataTable resultTableAux, SPWeb Web, SPListItem item, string wfid, Dictionary<string, string> resultColumns)
        {

            try
            {

                DataRow newRecord = resultTableAux.NewRow();
                foreach (KeyValuePair<String, String> kvp in resultColumns)
                {
                    switch (kvp.Key)
                    {
                        case "WFID":
                            newRecord[kvp.Key] = wfid;
                            break;
                        case "StepNumber":
                            newRecord[kvp.Key] = (item[kvp.Key].ToString().IndexOf('.') > -1) ? item[kvp.Key].ToString().Split('.')[0] : item[kvp.Key].ToString();
                            break;
                        case "AssignedPerson":
                        case "Author":
                            newRecord[kvp.Key] = (item[kvp.Key] != null) ? item[kvp.Key].ToString().Split('#')[1] : "";
                            break;
                        case "Urgent":
                            newRecord[kvp.Key] = (item[kvp.Key].ToString().Equals("0")) ? "No" : "Yes";
                            break;
                        case "WFDeadline":
                        case "Created":
                            if (item[kvp.Key] != null && item[kvp.Key].ToString() != "")
                                newRecord[kvp.Key] = DateTime.Parse(item[kvp.Key].ToString());
                            break;
                        default:
                            if (item[kvp.Key] != null)
                                newRecord[kvp.Key] = item[kvp.Key].ToString();
                            break;
                    }
                }

                //Compare deadline date and last date (Modified - Deadline) 
                if (item["WFDeadline"] != null && item["WFDeadline"].ToString() != "")
                {
                    TimeSpan ts = DateTime.Parse(item["Modified"].ToString()) - DateTime.Parse(item["WFDeadline"].ToString());
                    if (ts.Days > 0)
                        newRecord["Delayed"] = "Yes";
                }
                newRecord["Modified"] = item["Modified"];

                resultTableAux.Rows.Add(newRecord);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("AddNewRow_ListItem_Keyword():  WFID '" + wfid + "' " + ex.Message, null);
            }
        }

        private static bool CheckIfExistsRow(string wfid, DataTable resultTableAux)
        {
            bool exist = false;

            try
            {
                if (resultTableAux != null && resultTableAux.Rows.Count > 0)
                {
                    DataRow[] foundWFID = resultTableAux.Select("WFID = '" + wfid.Trim() + "'");

                    if (foundWFID.Length != 0)
                        exist = true;
                }

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CheckIfExistsRow():  WFID '" + wfid + "' " + ex.Message, ex.StackTrace);
            }

            return exist;
        }

        public static void AddStepsData(ref DataTable tableResults, ref int numMaxSteps, SPWeb Web, Dictionary<string, string>parameters, string webUrl)
        {
            int StepActual = 1;
            int StepPrevious = 1;
            int aux = 0;
            DateTime dateActual;
            DateTime datePrevious = new DateTime();
            
            string queryLog;

            string[] arrAssigned = null;
            string[] arrRole = null;
            string[] arrDaysToSign = null;
            string[] arrSignedDate = null;
            string[] arrRejectedDate = null;
            string[] arrRejectedComment = null;

            string groupName = String.Empty;
            string assignedPerson = String.Empty;

            try
            {
                queryLog = "<Where><Eq><FieldRef Name='ActionTaken'/><Value Type='Choice'>" + DataManagement.ActionsEnum.Commented.ToString() + "</Value></Eq>" +
                                    "</Where><OrderBy><FieldRef Name='WFID' Ascending='False' /><FieldRef Name='Created' Ascending='True' /></OrderBy>";

                DataTable logsTable = DataManagement.GetLogsResultsTable(queryLog, webUrl);

                DataView dvLogs = logsTable.AsDataView();
                if (logsTable.Rows.Count > 0)
                {

                    foreach (DataRow row in tableResults.Rows)
                    {
                        //Initialize arrays
                        arrAssigned = null;
                        arrRole = null;
                        arrDaysToSign = null;
                        arrSignedDate = null;
                        arrRejectedDate = null;
                        arrRejectedComment = null;

                        aux = 0;

                        dvLogs.RowFilter = "WFID = '" + row["WFID"].ToString() + "'";
                        if (dvLogs.Count == 0)
                            dvLogs.RowFilter = "WFID LIKE '" + row["WFID"].ToString() + ".*'";

                        dvLogs.Sort = "Created ASC";
                        StepPrevious = 1;
                        StepActual = 1;
                        dateActual = new DateTime();
                        datePrevious = new DateTime();

                        foreach (DataRowView drvLogs in dvLogs)
                        {

                            StepActual = int.Parse(drvLogs[4].ToString().Split('.')[0]);
                            dateActual = DateTime.Parse(drvLogs[8].ToString());
                            if (datePrevious.Year == 1)
                                datePrevious = DateTime.Parse(drvLogs[8].ToString());

                            if (drvLogs[9].ToString().ToLower() == DataManagement.ActionsEnum.Rejected.ToString().ToLower() || drvLogs[9].ToString().ToLower() == DataManagement.ActionsEnum.Launched.ToString().ToLower() || drvLogs[9].ToString().ToLower() == DataManagement.ActionsEnum.Signed.ToString().ToLower())
                            {
                                if (aux == 0 || StepActual > StepPrevious) 
                                {
                                    int resize = (aux == 0) ? 1 : StepActual;

                                    Array.Resize(ref arrAssigned, resize);
                                    Array.Resize(ref arrRole, resize);
                                    Array.Resize(ref arrDaysToSign, resize);
                                    Array.Resize(ref arrSignedDate, resize);
                                    Array.Resize(ref arrRejectedDate, resize);
                                    Array.Resize(ref arrRejectedComment, resize);
                                }

                                if (arrAssigned[StepActual - 1] == null)
                                    arrAssigned[StepActual - 1] = "";

                                if (arrRole[StepActual - 1] == null)
                                    arrRole[StepActual - 1] = "";

                                if (arrDaysToSign[StepActual - 1] == null)
                                    arrDaysToSign[StepActual - 1] = "";

                                if (arrSignedDate[StepActual - 1] == null)
                                    arrSignedDate[StepActual - 1] = "";

                                if (arrRejectedDate[StepActual - 1] == null)
                                    arrRejectedDate[StepActual - 1] = "";

                                if (arrRejectedComment[StepActual - 1] == null)
                                    arrRejectedComment[StepActual - 1] = "";

                                groupName = " - ";
                                assignedPerson = " - ";
                                if (drvLogs[6] != null && drvLogs[6].ToString() != "")
                                {
                                    assignedPerson = drvLogs[6].ToString().Split('#')[1];
                                    SPSecurity.RunWithElevatedPrivileges(delegate()
                                    {
                                        using (SPSite elevatedSite = new SPSite(Web.Url.ToString()))
                                        {
                                            SPWeb elevatedWeb = elevatedSite.OpenWeb();
                                            groupName = DataManagement.GetActorGroup(row["WFType"].ToString(), StepActual.ToString(), parameters, elevatedWeb);
                                        }
                                    });                                    
                                }
                                arrAssigned[StepActual - 1] += assignedPerson + "<br/>";
                                arrRole[StepActual - 1] += groupName + "<br/>";

                                bool isRejectAction = (drvLogs[9].ToString().ToLower() == DataManagement.ActionsEnum.Rejected.ToString().ToLower());
                                string signedDate = (isRejectAction || drvLogs[8] == null || drvLogs[8].ToString() == "") ? " - " : DateTime.Parse(drvLogs[8].ToString()).ToShortDateString();
                                string rejectDate = (isRejectAction && drvLogs[8] != null && drvLogs[8].ToString() != "") ? DateTime.Parse(drvLogs[8].ToString()).ToShortDateString() : " - ";
                                string rejectedComment = (isRejectAction) ? drvLogs[10].ToString() : " - ";
                                TimeSpan ts = dateActual.Date - datePrevious.Date;

                                arrSignedDate[StepActual - 1] += signedDate + "<br/>";
                                arrRejectedComment[StepActual - 1] += rejectedComment + "<br/>";
                                arrRejectedDate[StepActual - 1] += rejectDate + "<br/>";
                                arrDaysToSign[StepActual - 1] += ts.Days.ToString() + "<br/>";

                                aux++;

                                datePrevious = DateTime.Parse(drvLogs[8].ToString());
                                if (StepActual > numMaxSteps)
                                    numMaxSteps = StepActual;
                            }
                        }
                        //[StepNumber]_1 = AssignedPerson
                        //[StepNumber]_2 = Role
                        //[StepNumber]_3 = Days to sign/reject
                        //[StepNumber]_4 = Signed date
                        //[StepNumber]_5 = Rejected date
                        //[StepNumber]_6 = Rejected comment

                        if (arrAssigned != null)
                        {
                            for (int j = 0; j < arrAssigned.Length; j++)
                            {
                                row[(j + 1).ToString() + "_1"] = arrAssigned[j];
                                row[(j + 1).ToString() + "_2"] = arrRole[j];
                                row[(j + 1).ToString() + "_3"] = arrDaysToSign[j];
                                row[(j + 1).ToString() + "_4"] = arrSignedDate[j];
                                row[(j + 1).ToString() + "_5"] = arrRejectedDate[j];
                                row[(j + 1).ToString() + "_6"] = arrRejectedComment[j];
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("AddStepsData() - " + ex.Source, ex.Message);

            }

        }

        public static void GetUserCustomColumns(SPWeb Web, SPUser currentUser, ref Dictionary<string, string> columnsOrder, ref string sortField, ref string sortDirection)
        {
            try
            {
                SPList lstCustomColumns = Web.Lists["RS Reports Custom Columns"];
                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='RPUser' LookupId='True'/><Value Type='Integer'>" + currentUser.ID + "</Value></Eq></Where>";

                SPListItemCollection itemCollection = lstCustomColumns.GetItems(query);

                if (!(itemCollection == null || itemCollection.Count == 0))
                {
                    columnsOrder = new Dictionary<string, string>();
                    string[] splt = itemCollection[0]["RPCustomColumns"].ToString().Split('#');
                    for (int i = 0; i < splt.Length; i++)
                    {
                        string[] spltColumn = splt[i].Split(',');

                        if (spltColumn.Length == 2)
                        {
                            columnsOrder.Add(spltColumn[0], spltColumn[1]);
                        }
                    }

                    if (itemCollection[0]["RPCustomOrder"] != null && itemCollection[0]["RPCustomOrder"].ToString() != "")
                    {
                        sortField = itemCollection[0]["RPCustomOrder"].ToString().Split(';')[0];
                        sortDirection = itemCollection[0]["RPCustomOrder"].ToString().Split(';')[1];
                    }
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GetUSerCustomColumns() - " + ex.Source, ex.Message);
            }            
        }

    }
}
