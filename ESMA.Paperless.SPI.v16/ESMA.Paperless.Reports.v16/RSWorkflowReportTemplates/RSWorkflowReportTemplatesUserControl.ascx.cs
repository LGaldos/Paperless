using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using System.Data;
using System.Collections.Generic;
using ESMA.Paperless.Reports.v16.RSWorkflowReports;

namespace ESMA.Paperless.Reports.v16.RSWorkflowReportTemplates
{
    public partial class RSWorkflowReportTemplatesUserControl : UserControl
    {
        private Dictionary<string, string> parameters;
        private SPUser currentUser;
        private bool newTemplate = false;

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                    {
                        SPWeb web = Site.OpenWeb();

                        parameters = Methods.GetConfigurationParameters(web);
                        currentUser = Permissions.GetRealCurrentSpUser(this.Page);
                        newTemplate = (Request.QueryString["new"] != null && Request.QueryString["new"] == "1");
                        if (!this.Page.IsPostBack)
                        {
                            Session["PrevPage"] = Request.UrlReferrer;
                            if (Session["TemplateInformationMessage"] != null && Session["TemplateInformationMessage"].ToString() != "")
                            {
                                ShowMessage(Session["TemplateInformationMessage"].ToString(), true);
                                Session["TemplateInformationMessage"] = "";
                            }

                            if (newTemplate) //New template
                            {
                                newTemplate = true;
                                ReportTemplatesResultsPanel.Visible = false;
                                templateDataEdit.Visible = true;
                                LoadReportData(web);
                            }
                            else if (Request.QueryString["templateID"] != null && Request.QueryString["edit"] != null && Request.QueryString["edit"] == "1") //Edit template
                            {
                                Session["currentTemplateID"] = Request.QueryString["templateID"];
                                ReportTemplatesResultsPanel.Visible = false;
                                templateDataEdit.Visible = true;
                                //Methods.SetDateControlKeyEvents(dtStart);
                                //Methods.SetDateControlKeyEvents(dtEnd);


                                SPListItem reportTemplate = ReportTemplates.GetReportTemplate(web, Convert.ToInt32(Request.QueryString["templateID"]));
                                bool isEditable = (reportTemplate != null && ReportTemplates.IsTemplateAuthor(web, reportTemplate, currentUser));
                                if (isEditable)
                                {
                                    LoadReportData(web, reportTemplate);
                                } 
                                else 
                                {
                                    // Template does not exist or user is not the author
                                    Response.Redirect(Page.Request.FilePath, false);
                                }
                            }
                            else
                            {
                                if (Request.QueryString["action"] != null && Request.QueryString["action"] == "cancel") //Reject shared template
                                {
                                    string idTemplate = Request.QueryString["templateID"];
                                    string user = Request.QueryString["user"];
                                    web.AllowUnsafeUpdates = true;
                                    ReportTemplates.DeleteReportTemplate(web, Convert.ToInt32(idTemplate), web.EnsureUser(user));
                                }
                                BindGrid(web);
                                if (Request.QueryString["templateID"] != null && Request.QueryString["templateID"] != "" && Request.QueryString["action"] == null)
                                {
                                    Session["currentTemplateID"] = Request.QueryString["templateID"];
                                    templateDataView.Visible = true;
                                    SPListItem reportTemplate = ReportTemplates.GetReportTemplate(web, Convert.ToInt32(Request.QueryString["templateID"]));
                                    if (reportTemplate != null)
                                    {
                                        templateEdit.Visible = ReportTemplates.IsTemplateAuthor(web, reportTemplate, currentUser);
                                        LoadTemplateData(web, reportTemplate);
                                    }
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("RSWorklowReportTemplates_PageLoad() - " + ex.Source, ex.Message);
            }
        }

        protected void BindGrid(SPWeb web)
        {
            SPListItemCollection items = ReportTemplates.GetReportTemplates(web, currentUser);
            if (items != null)
            {
                if (Session["ReportTemplatesGridCurrentPage"] != null && Session["ReportTemplatesGridCurrentPage"].ToString() != "")
                    gvReportTemplates.PageIndex = Int32.Parse(Session["ReportTemplatesGridCurrentPage"].ToString());
                gvReportTemplates.DataSource = items.GetDataTable();
                gvReportTemplates.DataBind();
            }
        }

        #region EVENTS

        protected void templateEdit_Click(object sender, EventArgs e)
        {
            string redirectUrl = String.Format("{0}?templateID={1}&edit=1", Page.Request.FilePath, Session["currentTemplateID"]);
            Response.Redirect(redirectUrl, false);
        }

        protected void templateDelete_Click(object sender, EventArgs e)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                    {
                        SPWeb web = Site.OpenWeb();
                        ReportTemplates.DeleteReportTemplate(web, Convert.ToInt32(Session["currentTemplateID"]), currentUser);
                        Session["TemplateInformationMessage"] = "Template deleted successfully.";
                        Response.Redirect(Page.Request.FilePath, false);
                    }
                });
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("templateDelete_Click() - " + ex.Source, ex.Message);
                ShowMessage("Error deleting template.", false);
            }
        }

        protected void templateUse_Click(object sender, EventArgs e)
        {
            Session["ReportFromTemplate"] = "1";
            string redirectPage = (parameters.ContainsKey("Reports Page")) ? parameters["Reports Page"] : "/Pages/Reports.aspx";
            string redirectUrl = String.Format("{0}?templateID={1}", SPContext.Current.Web.Url + redirectPage, Session["currentTemplateID"]);
            Response.Redirect(redirectUrl, false);
        }

        protected void templateEditAccept_Click(object sender, EventArgs e)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                    {
                        SPWeb web = Site.OpenWeb();

                        string templateID = (newTemplate) ? null : Session["currentTemplateID"].ToString();

                        //if (!ValidateCriteria() && !ValidateTemplate(web, templateID))
                        if (!ValidateTemplate(web, templateID))
                        {
                            SPList list = web.GetListFromWebPartPageUrl(web.Url + "/Lists/ReportsTemplates/AllItems.aspx");
                            SPListItem reportTemplate = (newTemplate) ? list.AddItem() : list.GetItemById(Convert.ToInt32(Session["currentTemplateID"]));

                            if (reportTemplate != null)
                            {
                                if (newTemplate)
                                {
                                    reportTemplate["RPFirstDate"] = Session["ReportFirstDate"].ToString();
                                    reportTemplate["RPLastDate"] = Session["ReportLastDate"].ToString();
                                    reportTemplate["RPTypes"] = Session["ReportType"].ToString();
                                    reportTemplate["RPStatus"] = Session["ReportStatus"].ToString();
                                    reportTemplate["RPRoles"] = Session["ReportRoles"].ToString();
                                    reportTemplate["RPActors"] = Session["ReportActor"].ToString();
                                    reportTemplate["RPConfidential"] = Session["ReportConfidential"].ToString();
                                    reportTemplate["RPCreatedBy"] = Session["ReportCreated"].ToString();
                                    reportTemplate["RPFreeText"] = Session["ReportFreeText"].ToString();
                                    reportTemplate["RPShowSteps"] = (Session["ReportShowSteps"] != null &&  Session["ReportShowSteps"].ToString().Equals("True"));
                                }

                                SPFieldUserValue oUser = new SPFieldUserValue(web, currentUser.ID, Permissions.GetUsernameFromClaim(currentUser.LoginName));
                                if (newTemplate)
                                    reportTemplate["Author"] = oUser;
                                reportTemplate["Editor"] = oUser;

                                reportTemplate["Title"] = txtNameTemplate.Text.Trim();
                                reportTemplate["RP Share Users"] = Permissions.GetSelectedUsersInPeoplePicker(peShareUsers.CommaSeparatedAccounts);
                                reportTemplate["RP Automatic"] = cbAutoReport.Checked;
                                if (cbAutoReport.Checked)
                                {
                                    reportTemplate["RP Start Date"] = ((TextBox)dtStart.Controls[0]).Text;
                                    reportTemplate["RP End Date"] = ((TextBox)dtEnd.Controls[0]).Text;
                                    reportTemplate["RP Frequency"] = rblFrecuency.SelectedValue;
                                    reportTemplate["RP Recipients"] = Permissions.GetSelectedUsersInPeoplePicker(peRecipients.CommaSeparatedAccounts);
                                }

                                web.AllowUnsafeUpdates = true;
                                reportTemplate.Update();
                                list.Update();
                                web.AllowUnsafeUpdates = false;

                                //Notify Shared Users
                                if (reportTemplate["RP Share Users"] != null && reportTemplate["RP Share Users"].ToString() != "")
                                {
                                    ReportTemplates.SharedUsersNotify(web, reportTemplate["RP Share Users"].ToString(), reportTemplate.ID.ToString(), currentUser.Name, parameters);
                                }

                                Session["TemplateInformationMessage"] = (newTemplate) ? "Template created successfully." : "Template updated successfully.";

                                string redirectUrl = String.Format("{0}?templateID={1}", Page.Request.FilePath, reportTemplate.ID.ToString());
                                Response.Redirect(redirectUrl, false);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("templateUpdate_Click() - " + ex.Source, ex.Message);
                ShowMessage("Error updating template.", false);
            }
        }

        protected void templateEditCancel_Click(object sender, EventArgs e)
        {
            //string redirectUrl = String.Format("{0}?templateID={1}", Page.Request.FilePath, Session["currentTemplateID"]);
            string redirectUrl = (Session["PrevPage"] != null) ? Session["PrevPage"].ToString() : Page.Request.FilePath;
            Response.Redirect(redirectUrl, false);
        }

        protected void gvReportTemplates_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRow drv = ((DataRowView)e.Row.DataItem).Row;
                HyperLink hlUserProfile = e.Row.FindControl("hlTemplateDetail") as HyperLink;
                hlUserProfile.Text = drv["Title"].ToString();
                hlUserProfile.NavigateUrl = String.Format("{0}?templateID={1}", Page.Request.FilePath, drv["ID"].ToString());
            }
        }

        protected void gvReportTemplates_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                    {
                        SPWeb web = Site.OpenWeb();

                        Session["ReportTemplatesGridCurrentPage"] = e.NewPageIndex;
                        BindGrid(web);
                    }
                });
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("gvReportTemplates_PageIndexChanging() - " + ex.Source, ex.Message);
            }
        }

        #endregion

        

        protected void LoadTemplateData(SPWeb web, SPListItem templateItem)
        {
            try
            {
                SaveReportSession(templateItem);
                lblTemplateName.Text = templateItem["Title"].ToString();
                lblLaunchPeriod.Text = String.Format("From {0} To {1}", templateItem["RPFirstDate"].ToString(), templateItem["RPLastDate"].ToString());
                lblWFType.Text = templateItem["RPTypes"].ToString();
                lblWFStatus.Text = templateItem["RPStatus"].ToString();
                lblWFRole.Text = (templateItem["RPRoles"] != null) ? templateItem["RPRoles"].ToString() : "";
                lblActor.Text = (templateItem["RPActors"] != null) ? Permissions.GetUserDisplayName(web, templateItem["RPActors"].ToString()) : "--";
                lblConfidential.Text = (templateItem["RPConfidential"] != null) ? templateItem["RPConfidential"].ToString() : "--";
                lblAuthor.Text = (templateItem["RPCreatedBy"] != null) ? Permissions.GetUserDisplayName(web, templateItem["RPCreatedBy"].ToString()) : "--";
                lblKeyword.Text = (templateItem["RPFreeText"] != null) ? templateItem["RPFreeText"].ToString() : "--";
                lblShowSteps.Text = ((Boolean)templateItem["RPShowSteps"]) ? "Yes" : "No";

                lblTemplateShare.Text = (templateItem["RP Share Users"] != null) ? Permissions.GetDisplayNamesInPeoplePicker(web, templateItem["RP Share Users"].ToString()) : "--";
                Boolean regularEmails = (Boolean)templateItem["RP Automatic"];
                lblTemplateNotify.Text = (regularEmails) ? "Yes" : "No";
                if (regularEmails)
                {
                    lblTemplateNotifyPeriod.Text = String.Format("From {0} To {1}", templateItem["RP Start Date"].ToString(), (templateItem["RP End Date"] != null) ? templateItem["RP End Date"].ToString() : "--");
                    lblTemplateNotifyFrequency.Text = (templateItem["RP Frequency"] != null) ? templateItem["RP Frequency"].ToString() : "--";
                    lblTemplateNotifyRecipients.Text = (templateItem["RP Recipients"] != null) ? Permissions.GetDisplayNamesInPeoplePicker(web, templateItem["RP Recipients"].ToString()) : "--";
                }
                else
                {
                    lblTemplateNotifyPeriod.Text = "--";
                    lblTemplateNotifyFrequency.Text = "--";
                    lblTemplateNotifyRecipients.Text = "--";
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("LoadTemplateData() - " + ex.Source, ex.Message);
            }
        }

        protected void SaveReportSession(SPListItem templateItem)
        {
            Session["ReportFirstDate"] = templateItem["RPFirstDate"];
            Session["ReportLastDate"] = templateItem["RPLastDate"];
            Session["ReportType"] = templateItem["RPTypes"];
            Session["ReportStatus"] = templateItem["RPStatus"];
            Session["ReportActor"] = templateItem["RPActors"];
            Session["ReportRoles"] = templateItem["RPRoles"];
            Session["ReportConfidential"] = templateItem["RPConfidential"];
            Session["ReportCreated"] = templateItem["RPCreatedBy"];
            Session["ReportFreeText"] = templateItem["RPFreeText"];
            Session["ReportShareUsers"] = templateItem["RPShareUsers"];
            Session["ReportAutomatic"] = templateItem["RPAutomatic"];
            Session["ReportStartDate"] = templateItem["RPStartDate"];
            Session["ReportEndDate"] = templateItem["RPEndDate"];
            Session["ReportFrecuency"] = templateItem["RPFrecuency"];
            Session["ReportRecipients"] = templateItem["RPRecipients"];

            Session["ReportShowSteps"] = templateItem["RPShowSteps"];
        }

        protected void LoadReportData(SPWeb web, SPListItem templateItem = null)
        {
            try
            {
                if (templateItem == null)
                {
                    lblCriteriaLaunchPeriod.Text = String.Format("From {0} to {1}", Session["ReportFirstDate"].ToString(), Session["ReportLastDate"].ToString());
                    lblCriteriaWFType.Text = Session["ReportType"].ToString();
                    lblCriteriaWFStatus.Text = Session["ReportStatus"].ToString();
                    lblCriteriaRole.Text = Session["ReportRoles"].ToString();
                    lblCriteriaActor.Text = (Session["ReportActor"] != null && Session["ReportActor"].ToString() != "") ? Permissions.GetUserDisplayName(web, Session["ReportActor"].ToString()) : "--";
                    lblCriteriaRestricted.Text = (Session["ReportConfidential"] != null && Session["ReportConfidential"].ToString() != "") ? Session["ReportConfidential"].ToString() : "--";
                    lblCriteriaCreatedBy.Text = (Session["ReportCreated"] != null && Session["ReportCreated"].ToString() != "") ? Permissions.GetUserDisplayName(web, Session["ReportCreated"].ToString()) : "--";
                    lblCriteriaKeyword.Text = (Session["ReportFreeText"] != null && Session["ReportFreeText"].ToString() != "") ? Session["ReportFreeText"].ToString() : "--";
                    lblCriteriaShowSteps.Text = (Session["ReportShowSteps"] != null && Session["ReportShowSteps"].ToString().Equals("True")) ? "Yes" : "No";

                    peRecipients.CommaSeparatedAccounts = currentUser.LoginName;
                }
                else
                {
                    lblCriteriaLaunchPeriod.Text = String.Format("From {0} to {1}", templateItem["RPFirstDate"].ToString(), templateItem["RPLastDate"].ToString()); 
                    lblCriteriaWFType.Text = templateItem["RPTypes"].ToString();
                    lblCriteriaWFStatus.Text = templateItem["RPStatus"].ToString();
                    lblCriteriaRole.Text = templateItem["RPRoles"].ToString();
                    lblCriteriaActor.Text = (templateItem["RPActors"] != null && templateItem["RPActors"].ToString() != "") ? Permissions.GetUserDisplayName(web, templateItem["RPActors"].ToString()) : "--";
                    lblCriteriaRestricted.Text = (templateItem["RPConfidential"] != null && templateItem["RPConfidential"].ToString() != "") ? templateItem["RPConfidential"].ToString() : "--";
                    lblCriteriaCreatedBy.Text = (templateItem["RPCreatedBy"] != null && templateItem["RPCreatedBy"].ToString() != "") ? Permissions.GetUserDisplayName(web, templateItem["RPCreatedBy"].ToString()) : "--";
                    lblCriteriaKeyword.Text = (templateItem["RPFreeText"] != null && templateItem["RPFreeText"].ToString() != "") ? templateItem["RPFreeText"].ToString() : "--";
                    lblCriteriaShowSteps.Text = ((Boolean)templateItem["RPShowSteps"]) ? "Yes" : "No";

                    txtNameTemplate.Text = templateItem["Title"].ToString();
                    if (templateItem["RP Share Users"] != null)
                        peShareUsers.CommaSeparatedAccounts = templateItem["RP Share Users"].ToString();
                    cbAutoReport.Checked = (bool)templateItem["RP Automatic"];
                    if (cbAutoReport.Checked)
                    {
                        PanelMyTemplatesAuto.Style.Add("display", "block");
                        dtStart.SelectedDate = DateTime.Parse(templateItem["RP Start Date"].ToString());
                        if (templateItem["RP End Date"] != null)
                            dtEnd.SelectedDate = DateTime.Parse(templateItem["RP End Date"].ToString());
                        lblTemplateNotify.Text = ((Boolean)templateItem["RP Automatic"]) ? "Yes" : "No";
                        if (templateItem["RP Frequency"] != null)
                            rblFrecuency.SelectedValue = templateItem["RP Frequency"].ToString();
                        if (templateItem["RP Recipients"] != null)
                            peRecipients.CommaSeparatedAccounts = templateItem["RP Recipients"].ToString();
                    }
                    else
                    {
                        peRecipients.CommaSeparatedAccounts = currentUser.LoginName;
                    }
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("LoadReportData() - " + ex.Source, ex.Message);
            }
        }

        private bool ValidateTemplate(SPWeb web, string templateID = null)
        {
            bool error = false;
            string errorMessage = "";

            try
            {
                if (txtNameTemplate.Text.Trim() == "")
                {
                    errorMessage = "\"Template Name\" is mandatory";
                }
                else if (txtNameTemplate.Text.Length > 256)
                {
                    errorMessage = "\"Template Name\" is too long";
                }
                else if (ReportTemplates.ExistTemplate(web, txtNameTemplate.Text.Trim(), templateID))
                {
                    errorMessage = "\"Template Name\" already exists";
                }
                else if (cbAutoReport.Checked)
                {
                    TextBox startDateTB = dtStart.Controls[0] as TextBox;
                    TextBox endDateTB = dtEnd.Controls[0] as TextBox;

                    if (String.IsNullOrEmpty(startDateTB.Text))
                    {
                        errorMessage = "\"Start Date\" is mandatory.";
                    }
                    else if (dtStart.SelectedDate < DateTime.Today)
                    {
                        errorMessage = "\"Start Date\" should be greater than the current date.";
                    }
                    else if (!String.IsNullOrEmpty(endDateTB.Text) && dtStart.SelectedDate > dtEnd.SelectedDate)
                    {
                        errorMessage = "\"Start Date\" can not be greater than the \"End Date\".";
                    }
                    else if (rblFrecuency.SelectedIndex == -1)
                    {
                        errorMessage = "\"Frequency\" is mandatory.";
                    }
                    else if (peRecipients.CommaSeparatedAccounts == "")
                    {
                        errorMessage = "\"Report Recipients\" is mandatory.";
                    }
                }

                if (String.IsNullOrEmpty(errorMessage))
                {
                    TamplateErrorMessagePanel.Visible = false;
                }
                else
                {
                    lblTemplateMandatory.Text = errorMessage;
                    TamplateErrorMessagePanel.Visible = true;
                    error = true;
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("ValidateTemplate() - " + ex.Source, ex.Message);
            }
            return error;
        }

        protected void ShowMessage(string messageText, bool success)
        {
            informationMesage.Text = messageText;
            informationMessagePanel.CssClass = (success) ? "information_message success" : "information_message error";
            informationMessagePanel.Visible = true;
        }
        

    }
}
