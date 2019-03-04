using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using ESMA.Paperless.Reports.v16.RSWorkflowReports;
using System.Data;
using System.Globalization;
using System.IO;

namespace ESMA.Paperless.Reports.v16.TimerJobs
{
    class ReportsCreate : SPJobDefinition
    {
        public const string JobName = "Routing Slip Reports Creation";

        public ReportsCreate() : base() { }

        public ReportsCreate(string jobName, SPService service)
            : base(jobName, service, null, SPJobLockType.None)
        {
            this.Title = JobName;
        }

        public ReportsCreate(string jobName, SPWebApplication webapp)
            : base(jobName, webapp, null, SPJobLockType.Job)
        {
            this.Title = JobName;
        }

        public override string Description
        {
            get
            {
                return "Create Long Reports Asynchronously";
            }
        }

        public override void Execute(Guid targetInstanceId)
        {
            SPWebApplication webApp = this.Parent as SPWebApplication;
            SPWeb web = webApp.Sites[0].RootWeb;

            ReportsCreateProcess(web);
        }

        private void ReportsCreateProcess(SPWeb web)
        {
            try
            {
                web.AllowUnsafeUpdates = true;

                Dictionary<string, string> parameters = JobUtilities.GetConfigurationParameters(web);

                SPList templateList = web.Lists["RS Create Reports"];
                SPQuery query = new SPQuery();
                query.Query = "<OrderBy><FieldRef Name='Created' Ascending='FALSE' /></OrderBy>";
                SPListItemCollection itemsCol = templateList.GetItems(query);

                int itemCount = itemsCol.Count;
                for (int i = 0; i < itemCount; i++)
                {
                    SPListItem item = itemsCol[0];
                    ExportAndSendReport(item, web, parameters);
                    itemsCol.Delete(0);
                    templateList.Update();
                }

                web.AllowUnsafeUpdates = false;
            }
            catch (Exception ex)
            {
                JobUtilities.SaveErrorsLog(web, "ReportsCreateProcess", ex.Message);
                JobUtilities.ExceptionRecording("ReportsCreateProcess", ex.Message);
            }

        }

        private void ExportAndSendReport(SPListItem itemReport, SPWeb web, Dictionary<string, string> parameters)
        {
            try 
            {
                Dictionary<string, string> reportGeneralColumns = ReportsResults.GetHeaderColumns(web, parameters);
                Dictionary<string, string> reportStepsColumns = ReportsResults.GetStepsColumns(web, parameters);
                Dictionary<string, string> reportTotalColumns = reportGeneralColumns;

                string strTypes = (itemReport["RPTypes"] != null) ? itemReport["RPTypes"].ToString() : String.Empty;
                string strStatus = (itemReport["RPStatus"] != null) ? itemReport["RPStatus"].ToString() : String.Empty;
                string strConfidential = (itemReport["RPConfidential"] != null) ? itemReport["RPConfidential"].ToString() : String.Empty;
                string strCreated = (itemReport["RPCreatedBy"] != null) ? itemReport["RPCreatedBy"].ToString() : String.Empty;
                string textSearch = (itemReport["RPFreeText"] != null) ? itemReport["RPFreeText"].ToString() : String.Empty;
                string peActor = (itemReport["RPActors"] != null) ? itemReport["RPActors"].ToString() : String.Empty;
                string strRoles = (itemReport["RPRoles"] != null) ? itemReport["RPRoles"].ToString() : String.Empty;

                DateTime firstDate = new DateTime();
                DateTime lastDate = new DateTime();

                string pattern = "dd/MM/yyyy";

                if (itemReport["RPFirstDate"] != null)
                    DateTime.TryParseExact(itemReport["RPFirstDate"].ToString(), pattern, null, DateTimeStyles.None, out firstDate);

                if (itemReport["RPLastDate"] != null)
                    DateTime.TryParseExact(itemReport["RPLastDate"].ToString(), pattern, null, DateTimeStyles.None, out lastDate);

                DataTable resultTable = new DataTable();

                ReportsResults.CreateResultTable(ref resultTable, reportGeneralColumns, reportStepsColumns);

                //Get WFs (getting information from all DLs)
                string queryCommonToExecute = CreateUIQueryModule(web, parameters, firstDate, lastDate, strTypes, strStatus, strConfidential, strCreated, peActor, strRoles);

                // Create a context of the user who created the report 
                SPFieldUserValue authorUser = new SPFieldUserValue(web, itemReport["Author"].ToString());
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
                    int numSteps = 0;
                    ReportsResults.AddStepsData(ref resultTable, ref numSteps, web, parameters, web.Url);

                    for (int i = 0; i < numSteps * 6; i++)
                    {
                        KeyValuePair<String, String> step = reportStepsColumns.ElementAt(i);
                        reportTotalColumns.Add(step.Key, step.Value);
                    }

                    SPFieldUserValue userAuthor = new SPFieldUserValue(web, itemReport["Author"].ToString());

                    DataManagement.ReportExport(web, resultTable, reportTotalColumns, userAuthor.User, parameters, itemReport);
                    /*resultTable.DefaultView.Sort = "WFID DESC";*/

                }
            }
            catch (Exception ex)
            {
                JobUtilities.ExceptionRecording("ExportAndSendReport", ex.Message);


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

    }

}
