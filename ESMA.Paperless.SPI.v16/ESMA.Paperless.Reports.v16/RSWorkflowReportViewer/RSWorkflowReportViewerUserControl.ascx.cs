using System;
using System.Web.UI;
using Microsoft.SharePoint;
using HttpContext = System.Web.HttpContext;
using System.Collections.Generic;

namespace ESMA.Paperless.Reports.v16.RSWorkflowReportViewer
{
    public partial class RSWorkflowReportViewerUserControl : UserControl
    {
        public Dictionary<string, string> parameters;

        protected void Page_Load(object sender, EventArgs e)
        {
            string rpid = string.Empty;

            try
            {

                //RPID -> HTML file stored in "ReportsLibrary"
                if (!string.IsNullOrEmpty(HttpContext.Current.Request.QueryString["rpid"]))
                    rpid = HttpContext.Current.Request.QueryString["rpid"];


                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                    {
                        SPWeb Web = Site.OpenWeb();
                        parameters = Methods.GetConfigurationParameters(Web);

                        string htmlFilePath = Web.Url + "/Lists/ReportsLibrary/" + rpid + ".html";

                        //ControlContainer.Controls.Add(new LiteralControl("<iframe width='1600px' height='800px'  src='" + htmlFilePath + "' runat='server'></iframe> "));
                        ControlContainer.Controls.Add(new LiteralControl("<iframe src='" + htmlFilePath + "' runat='server' onload='resizeIframe(this)' frameBorder='0'></iframe> "));

                        Web.Close();
                        Web.Dispose();
                    }
                });
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("[" + rpid + "] Page_Load() - " + ex.Source, ex.Message);
            }
        }
    }
}
