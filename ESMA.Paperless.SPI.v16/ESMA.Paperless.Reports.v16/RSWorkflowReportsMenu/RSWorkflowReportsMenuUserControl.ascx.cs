using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Collections.Generic;
using Microsoft.SharePoint;
using ESMA.Paperless.Reports.v16.RSWorkflowReports;
using ESMA.Paperless.Reports.v16.RSWorkflowReportTemplates;

namespace ESMA.Paperless.Reports.v16.RSWorkflowReportsMenu
{
    public partial class RSWorkflowReportsMenuUserControl : UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                int myReportsCount = GetMyReportsCount();
                int myTemplatesCount = ReportTemplates.GetReportTemplates(SPContext.Current.Web, Permissions.GetRealCurrentSpUser(this.Page)).Count;

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                    {
                        SPWeb web = Site.OpenWeb();
                        Dictionary<string, string> parameters = Methods.GetConfigurationParameters(web);
                        if (parameters.ContainsKey("Reports Page"))
                            ReportsMenuNew.NavigateUrl = web.Url + parameters["Reports Page"].ToString();

                        if (parameters.ContainsKey("Report Templates Page"))
                            ReportsMenuTemplates.NavigateUrl = web.Url + parameters["Report Templates Page"].ToString();
                        if (myTemplatesCount > 0)
                            ReportsMenuTemplates.Text = String.Format("Report Templates ({0})", myTemplatesCount);


                        if (parameters.ContainsKey("My Reports Page"))
                            ReportsMenuMyReports.NavigateUrl = web.Url + parameters["My Reports Page"].ToString();
                        if (myReportsCount > 0)
                            ReportsMenuMyReports.Text = String.Format("My Reports ({0})", myReportsCount);

                        //Set active menu item
                        if (Page.Request.Url.ToString().IndexOf(ReportsMenuNew.NavigateUrl, StringComparison.OrdinalIgnoreCase) >= 0)
                            ReportsMenuNew.CssClass = "activeMenu";
                        else if (Page.Request.Url.ToString().IndexOf(ReportsMenuTemplates.NavigateUrl, StringComparison.OrdinalIgnoreCase) >= 0)
                            ReportsMenuTemplates.CssClass = "activeMenu";
                        else if (Page.Request.Url.ToString().IndexOf(ReportsMenuMyReports.NavigateUrl, StringComparison.OrdinalIgnoreCase) >= 0)
                            ReportsMenuMyReports.CssClass = "activeMenu";
                    }
                });
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("ReportsMenu_PageLoad() - " + ex.Source, ex.Message);
            }
        }

        /// <summary>
        /// Get number of reports saved in the "Reports Library" for the current user. It counts from "My Reports" view
        /// </summary>
        protected int GetMyReportsCount()
        {
            int reportsCount = 0;
            SPSite spSite = null;
            SPWeb spWeb = null;

            try
            {
                spSite = new SPSite(SPContext.Current.Web.Url.ToString());
                spWeb = spSite.OpenWeb();
                SPList list = spWeb.GetListFromWebPartPageUrl(spWeb.Url + "/Lists/ReportsLibrary/Forms/AllItems.aspx");
                SPUser currentUser = Permissions.GetRealCurrentSpUser(this.Page);
                if (list != null && currentUser != null)
                {
                    SPView view = list.Views["My Reports"];
                    SPQuery query = new SPQuery(view);
                    reportsCount = list.GetItems(query).Count;
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GetMyReportsCount() - " + ex.Source, ex.Message);
            }
            finally
            {
                if (spWeb != null)
                    spWeb.Dispose();

                if (spSite != null)
                    spSite.Dispose();
            }
            return reportsCount;
        }
    }
}
