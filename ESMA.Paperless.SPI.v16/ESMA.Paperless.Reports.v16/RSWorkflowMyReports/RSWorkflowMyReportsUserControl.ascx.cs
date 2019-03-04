using Microsoft.SharePoint;
using Microsoft.SharePoint.WebPartPages;
using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

namespace ESMA.Paperless.Reports.v16.RSWorkflowMyReports
{
    public partial class RSWorkflowMyReportsUserControl : UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                    {
                        SPWeb web = Site.OpenWeb();

                        SPList list = web.Lists["RS Reports Library"];
                        ListViewWebPart lvwp = new ListViewWebPart();
                        lvwp.ListName = list.ID.ToString("B").ToUpper();  // gets ID of List as string
                        lvwp.ViewGuid = list.Views["My Reports"].ID.ToString("B").ToUpper(); // gets ID of View as string
                        lvwp.ChromeType = PartChromeType.None;
                        lvwp.Visible = true;

                        MyReportsPanel.Controls.Add(lvwp);

                        if (Session["ReportInformationMessage"] != null && Session["ReportInformationMessage"].ToString() != "")
                        {
                            ShowMessage(Session["ReportInformationMessage"].ToString());
                            Session["ReportInformationMessage"] = "";
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                //Methods.SaveErrorsLog("ReportsMenu_PageLoad() - " + ex.Source, ex.Message);
            }
        }

        protected void ShowMessage(string messageText)
        {
            informationMesage.Text = messageText;
            informationMessagePanel.Visible = true;
        }
    }
}
