using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Collections.Generic;
using Microsoft.SharePoint;
using System.Web;
using System.Text;
using System.Linq;
using System.IO;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Administration;

namespace ESMA.Paperless.Reports.v16.RSWorkflowReports
{
    public partial class RSWorkflowReportsUserControl : UserControl
    {
        public RSWorkflowReports WebPart { get; set; }
        public Dictionary<string, string> parameters;
        Dictionary<string, string> wftypeCodes;
        Dictionary<string, string> columnsDefaultReport;
        Dictionary<string, string> columnsStepsReport;
        Dictionary<string, string> columnsOrder;

        public static IEnumerable<string> argumentEnum;

        DataTable resultTable;
        int numMaxSteps = 0;

        #region EVENTS

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                this.Page.Response.Cache.SetCacheability(HttpCacheability.NoCache);

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                    {
                        SPWeb Web = Site.OpenWeb();
                        parameters = Methods.GetConfigurationParameters(Web);
                        wftypeCodes = Methods.GetWorkflowTypeOrder(Web);

                        //string groupName = (parameters.ContainsKey("RS Staff Ext Group")) ? parameters["RS Staff Ext Group"] : String.Empty;
                        SPUser loggedUser = Permissions.GetRealCurrentSpUser(this.Page);
                        string userLoginName = loggedUser.LoginName;

                        if (!this.Page.IsPostBack)
                            lblResults.Visible = false;

                        //[ESMA-PP11] It is not allowed to generate reports to Staff-ext (Removed this restriction)
                        //if (!Permissions.UserBelongToGroup(groupName, userLoginName, parameters))
                        //{

                            if (!this.Page.IsPostBack)
                            {
                                InitializeWFTypes(Web);
                                InitializeWFStatus(Web);
                                InitializeWFRoles(Web);
                                InitializeDdlUsers(Web);

                                if (Session["ReportColumnsSteps"] == null)
                                    Session["ReportColumnsSteps"] = DataManagement.GetStepsColumns(Web, parameters);

                                LoadControls(Web);

                                if (String.IsNullOrEmpty(HttpContext.Current.Request.QueryString["templateID"]))
                                {
                                    // New Report
                                    ClearFields();
                                    ClearSessionState();
                                    VisibleControls(DataManagement.STATE_REPORT_NEW);
                                }
                                else
                                {
                                    // Report from Template
                                    bool showSteps = (Session["ReportShowSteps"] != null && Session["ReportShowSteps"].ToString().Equals("True"));
                                    GetResults(showSteps);
                                    if (Session["ReportFromTemplate"] != null && Session["ReportFromTemplate"] != "")
                                    {
                                        PanelMandatory.Visible = false;
                                        PanelCriteria.Visible = false;
                                        PanelButtonsBottom.Visible = false;
                                        btnSaveTemplate.Visible = false;
                                        Session["ReportFromTemplate"] = "";
                                    }
                                }
                                //Methods.SetDateControlKeyEvents(dtFirst);
                                //Methods.SetDateControlKeyEvents(dtLast);
                            }

                        //}
                        //else
                        //{
                        //    VisibleControls(DataManagement.STATE_NOACCESS);
                        //}

                        Web.Close();
                        Web.Dispose();
                    }
                });

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("Page_Load() - " + ex.Source, ex.Message);
            }
        }


        /// <summary>
        /// Export results grid to Excel 
        /// </summary>
        protected void btnExportExcel_Click(object sender, EventArgs e)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                    {
                        SPWeb Web = Site.OpenWeb();

                        Dictionary<String, String> columnsOrder = new Dictionary<String, String>();
                        Dictionary<String, String> columnsStepsReport = new Dictionary<String, String>();
                        DataTable resultTable = new DataTable();
                        DataTable exportTable = new DataTable();
                        DataRow newRow;
                        DataView viewTable;

                        //SPWeb Web = SPContext.Current.Web;

                        if (Session["ReportOrderColumns"] != null)
                            columnsOrder = (Dictionary<String, String>)Session["ReportUserColumns"];

                        if (Session["ReportResultDatatable"] != null)
                            resultTable = (DataTable)Session["ReportResultDatatable"];

                        //Sort Rows
                        if (ViewState["ReportSortingField"] != null && ViewState["ReportSortingField"] != null)
                            resultTable.DefaultView.Sort = ViewState["ReportSortingField"] + " " + ViewState["ReportSortingDirection"];
                        else
                            resultTable.DefaultView.Sort = ddlColumnSort.SelectedValue + " " + rblOrder.SelectedValue;

                        int totalCols = gvReport.Columns.Count;
                        int totalRows = resultTable.Rows.Count;
                        var headerRow = gvReport.HeaderRow;

                        //Add columns in Grid Report. Only the columns selected by the user
                        foreach (KeyValuePair<String, String> kvp in columnsOrder)
                        {
                            exportTable.Columns.Add(kvp.Key.ToString());
                        }

                        if (Session["ReportShowSteps"].ToString() == "True")
                        {
                            int numStep = 0;
                            if (Session["ReportStepColumns"] != null)
                                columnsStepsReport = (Dictionary<String, String>)Session["ReportStepColumns"];

                            if (Session["ReportNumStepColumns"] != null)
                                numMaxSteps = int.Parse(Session["ReportNumStepColumns"].ToString());

                            //Add Steps
                            foreach (KeyValuePair<String, String> step in columnsStepsReport)
                            {
                                if (numStep < numMaxSteps)
                                {
                                    exportTable.Columns.Add(step.Key);
                                    numStep++;
                                }
                                else
                                    break;
                            }
                        }

                        viewTable = resultTable.DefaultView;

                        for (int j = 0; j < totalRows; j++)
                        {
                            newRow = exportTable.NewRow();

                            for (int i = 0; i < gvReport.Columns.Count - 1; i++)
                            {
                                newRow[i] = viewTable[j][exportTable.Columns[i].ColumnName];
                            }

                            exportTable.Rows.Add(newRow);
                        }

                        SPUser loggedUser = Permissions.GetRealCurrentSpUser(this.Page);
                        DataManagement.ReportExport(Web, exportTable, gvReport, loggedUser, parameters);

                        Response.Redirect(Web.Url + parameters["My Reports Page"], false);
                        Session["ReportInformationMessage"] = "Report generated successfully.";

                    }
                });
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("btnExportExcel_Click() - " + ex.Source, ex.Message);
            }
        }


        /// <summary>
        /// Customize the Results Grid - Columns to show and sorting
        /// </summary>
        protected void btnOrderColumns_Click(object sender, EventArgs e)
        {
            try
            {                
                GetOrderColumns();
                ViewState["ReportSortingField"] = ddlColumnSort.SelectedValue;
                ViewState["ReportSortingDirection"] = rblOrder.SelectedValue;

                if (cbColumnsSettingsSave.Checked)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        using (SPSite elevatedSite = new SPSite(SPContext.Current.Web.Url))
                        {
                            SPWeb elevatedWeb = elevatedSite.OpenWeb();
                            SaveCustomColumns(elevatedWeb);
                        }
                    });
                }                    

                bool showSteps = !(Session["ReportShowSteps"] != null && Session["ReportShowSteps"].ToString().Equals("False"));
                GetResults(showSteps);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("btnOrderColumns_Click: " + ex.Message, ex.StackTrace);
            }

        }        

        protected void btnShowSteps_Click(object sender, EventArgs e)
        {
            Session["ReportShowSteps"] = "True";
            GetResults(true);                
        }


        /// <summary>
        /// Clear Controls And Session Variables
        /// </summary>
        protected void btnClearFields_Click(object sender, EventArgs e)
        {
            ClearFields();
            PanelMandatory.Visible = false;
        }
        

        /// <summary>
        /// Search the results of the filters and displays them on the grid  
        /// </summary>
        protected void btnCreateReport_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateCriteria())
                {
                    SaveControls();
                    Session["ReportShowSteps"] = "False";
                    GetResults(false);                    
                }

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("btnCreateReport_Click: " + ex.Message, ex.StackTrace);
            }

        }


        /// <summary>
        /// "Save as Template" button  
        /// </summary>
        protected void btnSaveTemplate_Click(object sender, EventArgs e)
        {
            try
            {
                string redirectPage = (parameters.ContainsKey("Report Templates Page")) ? parameters["Report Templates Page"] : "/Pages/reporttemplates.aspx";
                string redirectUrl = String.Format("{0}?new=1", SPContext.Current.Web.Url + redirectPage);
                Response.Redirect(redirectUrl, false);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("btnSaveTemplate_Click() - " + ex.Source, ex.Message);
            }
        }

        /// <summary>
        /// Manage results grid paging
        /// </summary>
        protected void gvReport_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            try
            {
                gvReport.PageIndex = e.NewPageIndex;
                gvReport.DataSource = (DataTable)Session["ReportResultDatatable"];
                gvReport.DataBind();
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("gvReport_PageIndexChanging() - " + ex.Source, ex.Message);
            }
        } 

        protected void gvReport_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                try
                {
                    DataRow drv = ((DataRowView)e.Row.DataItem).Row;

                    string wfid = drv["WFID"].ToString();

                    // Set WFID Link
                    HyperLink hlWFID = new HyperLink();
                    hlWFID.Text = wfid;
                    hlWFID.NavigateUrl = String.Format("{0}?wfid={1}&wftype={2}", SPContext.Current.Web.Url + parameters["Interface Page"].ToString(), wfid, wftypeCodes[drv["WFType"].ToString().ToUpper()].ToString());
                    hlWFID.Target = "_blank";

                    DataControlFieldCell wfidCell = GetCellByName(e.Row, "WFID");
                    wfidCell.Controls.Clear();
                    wfidCell.Controls.Add(hlWFID);

                    // Change Row color to delayed WFs
                    if (drv["Delayed"].ToString() == "Yes")
                    {
                        e.Row.BackColor = System.Drawing.ColorTranslator.FromHtml("#f2dede");
                    }

                    // Set daystoclose column
                    if (drv["DaysToClose"] != null && drv["WFStatus"] != null && drv["WFStatus"].ToString().Equals("Closed"))
                    {
                        DataControlFieldCell daystocloseCell = GetCellByName(e.Row, "DaysToClose");
                        TimeSpan ts = DateTime.Parse(drv["Modified"].ToString()).Date - DateTime.Parse(drv["Created"].ToString()).Date;
                        daystocloseCell.Text = ts.Days.ToString();
                    }
                }
                catch (Exception ex)
                {
                    Methods.SaveErrorsLog("gvReport_RowDataBound() " + ex.Source, ex.Message);
                }
            }
        }

        protected void gvReport_Sorting(object sender, GridViewSortEventArgs e)
        {
            if (ViewState["SearchResultData"] != null)
            {

                if (ViewState["ReportSortingField"] != null && e.SortExpression.ToUpper().Equals(ViewState["ReportSortingField"].ToString().ToUpper()))
                {
                    ViewState["ReportSortingDirection"] = (ViewState["ReportSortingDirection"].ToString().ToUpper().Equals("ASC")) ? "DESC" : "ASC";
                }
                else
                {
                    ViewState["ReportSortingDirection"] = "ASC";
                }

                ViewState["ReportSortingField"] = e.SortExpression;

                DataTable dataTable = (DataTable)ViewState["SearchResultData"];
                DataView dataView = new DataView(dataTable);
                String sortField = ViewState["ReportSortingField"] as String;
                String sortDirection = ViewState["ReportSortingDirection"] as String;

                dataView.Sort = sortField + " " + sortDirection;

                gvReport.DataSource = dataView;
                gvReport.DataBind();
            }
        }

        #endregion

        #region EXPORT AND UPLOAD
       
        public void SaveReportPending()
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                    {
                        SPWeb Web = Site.OpenWeb();
                        Web.AllowUnsafeUpdates = true;

                        SPList pendingList = Web.Lists["RS Create Reports"];
                        SPListItem newItem;

                        newItem = pendingList.AddItem();
                        if (Session["ReportFirstDate"] != null)
                            newItem["RPFirstDate"] = Session["ReportFirstDate"].ToString();

                        if (Session["ReportLastDate"] != null)
                            newItem["RPLastDate"] = Session["ReportLastDate"].ToString();

                        if (Session["ReportType"] != null)
                            newItem["RPTypes"] = Session["ReportType"];

                        if (Session["ReportStatus"] != null)
                            newItem["RPStatus"] = Session["ReportStatus"];

                        if (Session["ReportRoles"] != null)
                            newItem["RPRoles"] = Session["ReportRoles"];

                        if (Session["ReportActor"] != null)
                            newItem["RPActors"] = Session["ReportActor"];

                        if (Session["ReportCreated"] != null)
                            newItem["RPCreatedBy"] = Session["ReportCreated"];

                        if (Session["ReportGFPersonalFile"] != null)
                            newItem["GFPersonalFile"] = Session["ReportGFPersonalFile"];

                        if (Session["ReportOpenAmountRAL"] != null)
                            newItem["GFOpenAmountRAL"] = Session["ReportOpenAmountRAL"];

                        if (Session["ReportAmountCurrentYear"] != null)
                            newItem["GFAmountCurrentYear"] = Session["ReportAmountCurrentYear"];

                        if (Session["ReportAmountNextYear"] != null)
                            newItem["GFAmountNextYear"] = Session["ReportAmountNextYear"];

                        if (Session["ReportAmountToCancel"] != null)
                            newItem["GFAmountToCancel"] = Session["ReportAmountToCancel"];

                        if (Session["ReportJustification"] != null)
                            newItem["GFJustification"] = Session["ReportJustification"];

                        if (Session["ReportGLAccount"] != null)
                            newItem["GFGLAccount"] = Session["ReportGLAccount"];

                        if (Session["ReportBudgetLine"] != null)
                            newItem["GFBudgetLine"] = Session["ReportBudgetLine"];

                        if (Session["ReportWFSubject"] != null)
                            newItem["WFSubject"] = Session["ReportWFSubject"];

                        if (Session["ReportFreeText"] != null)
                            newItem["RPFreeText"] = Session["ReportFreeText"];

                        SPUser loggedUser = Permissions.GetRealCurrentSpUser(this.Page);
                        SPFieldUserValue oUser = new SPFieldUserValue(Web, loggedUser.ID, Permissions.GetUsernameFromClaim(loggedUser.LoginName));
                        newItem["Author"] = oUser;

                        newItem.Update();
                        pendingList.Update();
                    }
                });

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("SaveReportPending() - " + ex.Source, ex.Message);

            }

        }


        public void SaveCustomColumns(SPWeb web)
        {
            try
            {
                SPList lstCustomColumns = web.Lists["RS Reports Custom Columns"];
                SPListItem newItem;
                SPUser currentUser = Permissions.GetRealCurrentSpUser(this.Page);

                string strCustom = "";

                if (Session["ReportOrderColumns"] != null)
                {
                    columnsOrder = (Dictionary<string, string>)Session["ReportOrderColumns"];

                    foreach (KeyValuePair<String, String> kvp in columnsOrder)
                    {
                        strCustom = strCustom + kvp.Key + "," + kvp.Value + "#";
                    }
                }

                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='RPUser' LookupId='True'/><Value Type='Integer'>" + currentUser.ID + "</Value></Eq></Where>";
                SPListItemCollection collColumns = lstCustomColumns.GetItems(query);

                if (collColumns != null && collColumns.Count > 0)
                {
                    newItem = collColumns[0];
                }
                else
                {
                    newItem = lstCustomColumns.AddItem();
                    newItem["Title"] = currentUser.Name;
                    newItem["RPUser"] = currentUser;
                }

                newItem["RPCustomColumns"] = strCustom;
                newItem["RPCustomOrder"] = ViewState["ReportSortingField"] + ";" + ViewState["ReportSortingDirection"];

                bool allowUnsafeUpdates = web.AllowUnsafeUpdates;
                web.AllowUnsafeUpdates = true;
                newItem.Update();
                lstCustomColumns.Update();
                web.AllowUnsafeUpdates = allowUnsafeUpdates;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("SaveCustomColumns() - " + ex.Source, ex.Message);
            }
        }

        #endregion

        #region INITIALIZE

        /// <summary>
        /// Loads the controls with the proper values according to the browser session states as well as it loads the result message and result table
        /// </summary>
        /// <param name="Web"></param>
        protected void LoadControls(SPWeb Web)
        {
            try
            {
                ////if (!this.Page.IsPostBack && parameters.ContainsKey("Interface Page") && HttpContext.Current.Request.UrlReferrer.ToString() != null && HttpContext.Current.Request.UrlReferrer.ToString().ToUpper().Contains(parameters["Interface Page"].ToUpper()))
                ////{
                ////    //DO NOTHING
                ////}
                ////else if (this.Page.IsPostBack && IsSearchingPostback(this.Page))
                ////{
                ////    int sessionCount = Session.Count;
                ////    for (int i = 0; i < sessionCount; i++) { try { if (Session.Keys[i].ToUpper().StartsWith("REPORT")) Session[i] = null; } catch { continue; } }
                ////}
                ////else if (!this.Page.IsPostBack)
                ////{
                ////    int sessionCount = Session.Count;
                ////    for (int i = 0; i < sessionCount; i++) { try { if (Session.Keys[i].ToUpper().StartsWith("REPORT")) Session[i] = null; } catch { continue; } }
                ////}

                //if (Session["SearchTitle"] != null)
                //    txtTitle.Text = Session["SearchTitle"].ToString();

                if (Session["ReportFreeText"] != null)
                    txtFreeText.Text = Session["ReportFreeText"].ToString();


                if (Session["ReportFirstDate"] != null && !string.IsNullOrEmpty(Session["ReportFirstDate"].ToString()))
                {
                    string[] splt = Session["ReportFirstDate"].ToString().Split('/');
                    DateTime date = new DateTime(Int32.Parse(splt[2]), Int32.Parse(splt[1]), Int32.Parse(splt[0]));
                    dtFirst.SelectedDate = DateTime.Parse(Session["ReportFirstDate"].ToString());

                    foreach (Control ctrl in dtFirst.Controls)
                    {
                        if (ctrl is TextBox)
                        {
                            TextBox txtBox = (TextBox)ctrl;
                            txtBox.Text = date.ToShortDateString();
                            ////txtBox.TextChanged += new EventHandler(dt_DeleteDateChanged);
                            break;
                        }
                    }


                    //txtHTML.AutoPostBack = true;


                    //string javascriptCode = "var radioButtonPanel = document.getElementById('dtFirst');";
                    //javascriptCode = "if((event.KeyCode >= 48 || event.KeyCode <= 57) || event.KeyCode==220) {}else{patron = /\d/; te = String.fromCharCode(event.KeyCode); return patron.test(te);}

                    //txtHTML.Attributes.Add("onkeydown", javascriptCode + "return (event.keyCode!=13);");
                    //txtHTML.Attributes.Add("onkeypress", javascriptCode);
                    //txtHTML.Attributes.Add("onkeyup", javascriptCode);

                }



                if (Session["ReportLastDate"] != null && !string.IsNullOrEmpty(Session["ReportLastDate"].ToString()))
                {
                    string[] splt = Session["ReportLastDate"].ToString().Split('/');
                    DateTime date = new DateTime(Int32.Parse(splt[2]), Int32.Parse(splt[1]), Int32.Parse(splt[0]));
                    dtLast.SelectedDate = date;

                    foreach (Control ctrl in dtLast.Controls)
                    {
                        if (ctrl is TextBox)
                        {
                            TextBox txtBox = (TextBox)ctrl;
                            txtBox.Text = date.ToShortDateString();
                            ////txtBox.TextChanged += new EventHandler(dt_DeleteDateChanged);
                            break;
                        }
                    }

                }

                foreach (ListItem item in ddlType.Items)
                {
                    item.Selected = false;
                }
                if (Session["ReportType"] != null)
                {
                    string[] selectItems = Session["ReportType"].ToString().Split(';');
                    for (int i = 0; i < selectItems.Length; i++)
                    {
                        foreach (ListItem item in ddlType.Items)
                        {

                            if (item.Value == selectItems[i])
                            {
                                item.Selected = true;

                                break;
                            }
                        }
                    }
                }

                foreach (ListItem item in ddlStatus.Items)
                {
                    item.Selected = false;
                }

                if (Session["ReportStatus"] != null)
                {
                    string[] selectItems = Session["ReportStatus"].ToString().Split(';');

                    for (int i = 0; i < selectItems.Length; i++)
                    {
                        foreach (ListItem item in ddlStatus.Items)
                        {
                            if (item.Value == selectItems[i])
                            {
                                item.Selected = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    ListItem item = ddlStatus.Items[0];
                    item.Selected = true;
                }

                foreach (ListItem item in ddlRole.Items)
                {
                    item.Selected = false;
                }

                if (Session["ReportRoles"] != null)
                {
                    string[] selectItems = Session["ReportRoles"].ToString().Split(';');
                    for (int i = 0; i < selectItems.Length; i++)
                    {
                        foreach (ListItem item in ddlRole.Items)
                        {
                            if (item.Value == selectItems[i])
                            {
                                item.Selected = true;
                                break;
                            }
                        }
                    }

                }
                else
                {
                    ListItem item = ddlRole.Items[0];
                    item.Selected = true;
                }

                if (Session["ReportActor"] != null)
                    peActor.SelectedValue = Session["ReportActor"].ToString();
                else
                    peActor.SelectedIndex = 0;

                if (Session["ReportConfidential"] != null)
                    ddlConfidential.SelectedValue = Session["ReportConfidential"].ToString();
                else
                    ddlConfidential.SelectedIndex = -1;

                if (Session["ReportCreated"] != null)
                    peCreated.SelectedValue = Session["ReportCreated"].ToString();
                else
                    peCreated.SelectedIndex = 0;

                if (Session["ReportGFPersonalFile"] != null)
                    cbPersonalFile.Checked = Session["ReportGFPersonalFile"].ToString() == "Yes";

                if (Session["ReportOpenAmountRAL"] != null)
                    txtOpenAmountRAL.Text = Session["ReportOpenAmountRAL"].ToString();

                if (Session["ReportAmountCurrentYear"] != null)
                    txtAmountCurrentYear.Text = Session["ReportAmountCurrentYear"].ToString();

                if (Session["ReportAmountNextYear"] != null)
                    txtAmountNextYear.Text = Session["ReportAmountNextYear"].ToString();

                if (Session["ReportAmountToCancel"] != null)
                    txtAmountToCancel.Text = Session["ReportAmountToCancel"].ToString();

                if (Session["ReportJustification"] != null)
                    txtJustification.Text = Session["ReportJustification"].ToString();

                if (Session["ReportGLAccount"] != null)
                    txtGLAccount.Text = Session["ReportGLAccount"].ToString();

                if (Session["ReportBudgetLine"] != null)
                    txtBudgetLine.Text = Session["ReportBudgetLine"].ToString();

                if (Session["ReportWFSubject"] != null)
                    txtWFSubject.Text = Session["ReportWFSubject"].ToString();

                if (Session["ReportResultMessage"] != null)
                {
                    lblResults.Text = Session["ReportResultMessage"].ToString();
                    lblResults.Visible = true;
                }

                if (Session["ReportOrderColumns"] != null)
                {
                    columnsOrder = (Dictionary<String, String>)Session["ReportOrderColumns"];
                }

                if (Session["ReportSortGrid"] != null)
                {
                    gvSort.DataSource = Session["ReportSortGrid"];
                    gvSort.DataBind();

                }

                if (Session["ReportColumnsDefault"] != null)
                    columnsDefaultReport = (Dictionary<String, String>)Session["ReportColumnsDefault"];
                else
                    columnsDefaultReport = DataManagement.GetHeaderColumns(Web, parameters);

                if (Session["ReportColumnsSteps"] != null)
                    columnsStepsReport = (Dictionary<String, String>)Session["ReportColumnsSteps"];
                else
                    columnsStepsReport = DataManagement.GetStepsColumns(Web, parameters);


                if (Session["ReportResultGrid"] != null)
                {
                    gvReport.DataSource = Session["ReportResultGrid"];

                    gvReport.AllowPaging = true;
                    gvReport.AllowSorting = true;
                    gvReport.PageSize = 50;
                    gvReport.PageIndex = 0;
                    gvReport.PagerSettings.Visible = true;
                    //gvReport.PagerSettings.Mode = PagerButtons.NumericFirstLast;
                    //gvReport.PagerStyle.Width = gvReport.Width;
                    gvReport.PagerStyle.HorizontalAlign = HorizontalAlign.Left;
                    gvReport.PagerStyle.SetDirty();
                    gvReport.EnableSortingAndPagingCallbacks = false;
                    gvReport.PagerStyle.HorizontalAlign = HorizontalAlign.Center;
                    gvReport.PagerSettings.Position = PagerPosition.Bottom;
                    gvReport.PagerSettings.NextPageText = "Next page";
                    gvReport.PagerSettings.PreviousPageText = "Previous page";
                    gvReport.HorizontalAlign = HorizontalAlign.Center;

                    resultTable = (DataTable)Session["ReportResultGrid"];
                    ViewState["ReportResultData"] = resultTable;

                    if (ViewState["ReportSortingField"] == null && ViewState["ReportSortingDirection"] == null)
                    {
                        ViewState["ReportSortingField"] = "WFID";
                        ViewState["ReportSortingDirection"] = "DESC";
                    }

                    //gvReport.DefaultView.Sort = ViewState["ReportSortingField"] + " " + ViewState["ReportSortingDirection"];

                    gvReport.DataBind();

                    //gvReport.Width = Unit.Pixel(1024);
                    gvReport.HeaderStyle.CssClass = "header_background";
                    gvReport.AlternatingRowStyle.CssClass = "result_grid_even";


                    gvReport.DataBind();
                    gvReport.Visible = true;
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("LoadControls() - " + ex.Source, ex.Message);
            }
        }



        /// <summary>
        /// Load possible workflow groups in roles control
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="domain"></param>
        protected void InitializeWFRoles(SPWeb Web)
        {
            try
            {
                if (!this.Page.IsPostBack)
                {
                    SPList configList = Web.Lists["RS Workflow Step Definitions"];

                    List<ListItem> lbRoleItems = new List<ListItem>();
                    List<ListItem> lbRoleItemsAux = new List<ListItem>();
                    foreach (SPListItem item in configList.Items)
                    {
                        if (item["WFGroup"] != null)
                        {
                            SPFieldUserValue groupValue = new SPFieldUserValue(Web, item["WFGroup"].ToString());
                            string groupAD = Methods.RemoveDomain(groupValue.LookupValue);
                            string groupName = DataManagement.GetDefinitionGroupName(groupAD, parameters);

                            ListItem lbItem = new ListItem(groupName);
                            ListItem lbItemAux = new ListItem(groupName.ToLower());

                            if (!lbRoleItemsAux.Contains(lbItemAux))
                            {
                                lbRoleItems.Add(lbItem);
                                lbRoleItemsAux.Add(lbItemAux);
                            }
                        }
                    }

                    lbRoleItems = lbRoleItems.OrderBy(o => o.Value).ToList();
                    ddlRole.Items.AddRange(lbRoleItems.ToArray());
                    string defaultValue = "All";
                    ddlRole.Items.Insert(0, defaultValue);
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("InitializeWFRoles() - " + ex.Source, ex.Message);
            }
        }

        /// <summary>
        /// Load possible workflow status in workflow status control.
        /// </summary>
        /// <param name="Web"></param>
        protected void InitializeWFStatus(SPWeb Web)
        {
            try
            {
                if (!this.Page.IsPostBack)
                {
                    SPFieldChoice statusField = null;

                    try
                    {
                        statusField = new SPFieldChoice(Web.Fields, "WFStatus");
                    }
                    catch
                    {
                        statusField = new SPFieldChoice(Web.Site.RootWeb.Fields, "WFStatus");
                    }

                    if (statusField != null)
                    {
                        List<ListItem> ddlStatusItems = new List<ListItem>();
                        foreach (string choice in statusField.Choices)
                        {
                            ListItem ddlItem = new ListItem(choice);
                            if (!ddlStatusItems.Contains(ddlItem))
                                ddlStatusItems.Add(ddlItem);
                        }

                        ddlStatusItems.Sort((x, y) => string.Compare(x.Value, y.Value));
                        ddlStatus.Items.AddRange(ddlStatusItems.ToArray());
                        ListItem defaultItem = new ListItem("All");
                        ddlStatus.Items.Insert(0, defaultItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("InitializeWFStatus: " + ex.Message, string.Empty);
            }
        }

        /// <summary>
        /// Load possible workflow status in workflow status control.
        /// </summary>
        /// <param name="Web"></param>
        protected void InitializeDdlUsers(SPWeb Web)
        {
            ListItem lItem;
            try
            {
                if (!this.Page.IsPostBack)
                {
                    SPUserCollection users = Web.AllUsers;

                    foreach (SPUser user in users)
                    {
                        if (!user.IsDomainGroup)
                        {
                            lItem = new ListItem();
                            lItem.Text = user.Name;
                            lItem.Value = user.LoginName;

                            peActor.Items.Add(lItem);
                            peCreated.Items.Add(lItem);
                        }
                    }


                    peActor.Items.Insert(0, "");
                    peCreated.Items.Insert(0, "");
                }

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("InitializeDdlUsers() - " + ex.Source, ex.Message);

            }
        }

        /// <summary>
        /// Load workflow types in workflow type control
        /// </summary>
        /// <param name="Web"></param>
        protected void InitializeWFTypes(SPWeb Web)
        {
            try
            {
                if (!this.Page.IsPostBack)
                {
                    SPList configList = Web.Lists["RS Workflow Configuration"];
                    SPQuery query = new SPQuery();
                    query.Query = "<OrderBy><FieldRef Name='Title' Ascending='True' /></OrderBy>";
                    query.ViewFields = string.Concat(
                                  "<FieldRef Name='Title' />");
                    query.ViewFieldsOnly = true; // Fetch only the data that we need
                    SPListItemCollection itemCollection = configList.GetItems(query);

                    List<ListItem> lbTypeItems = new List<ListItem>();
                    foreach (SPListItem item in itemCollection)
                    {
                        if (item["Title"] != null)
                        {
                            ListItem lbItem = new ListItem(item["Title"].ToString().ToUpper());
                            if (lbTypeItems.IndexOf(lbItem) <= 0)
                                lbTypeItems.Add(lbItem);
                        }
                    }

                    //lbType.Items..Sort((x, y) => string.Compare(x.Value, y.Value));
                    ddlType.Items.AddRange(lbTypeItems.ToArray());
                    ListItem defaultItem = new ListItem("All");
                    ddlType.Items.Insert(0, defaultItem);
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("InitializeWFTypes() - " + ex.Source, ex.Message);

            }
        }

        /// <summary>
        /// Create the Initial columns in the gridview of results
        /// </summary>
        /// <param name="Web"></param>
        protected void InitializeColumnsReportGrid(SPWeb Web)
        {
            try
            {
                BoundField newColumn;
                int cont = 0;
                gvReport.Columns.Clear();

                if (GetUserCustomColumns(Web))  // User Custom Columns
                {
                    foreach (KeyValuePair<String, String> kvp in columnsOrder)
                    {
                        newColumn = new BoundField();
                        newColumn.HeaderText = kvp.Value;
                        newColumn.DataField = kvp.Key;
                        newColumn.HtmlEncode = false;
                        gvReport.Columns.Add(newColumn);

                        if (Session["ReportNumMaxSteps"] != null)
                            numMaxSteps = int.Parse(Session["ReportNumMaxSteps"].ToString());

                        if (kvp.Value == "Steps")
                        {
                            foreach (KeyValuePair<String, String> kvp2 in columnsStepsReport)
                            {
                                if (cont < numMaxSteps)
                                {
                                    newColumn = new BoundField();
                                    newColumn.HeaderText = kvp2.Value;
                                    newColumn.DataField = kvp2.Key;
                                    newColumn.HtmlEncode = false;

                                    gvReport.Columns.Add(newColumn);
                                    cont++;
                                }
                                else
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    columnsDefaultReport = DataManagement.GetHeaderColumns(Web, parameters); // Default Report Columns
                    columnsStepsReport = DataManagement.GetStepsColumns(Web, parameters);   // Default Steps Columns
                    columnsOrder = new Dictionary<string, string>();

                    foreach (KeyValuePair<String, String> kvp in columnsDefaultReport)
                    {
                        newColumn = new BoundField();
                        newColumn.HeaderText = kvp.Value;
                        newColumn.DataField = kvp.Key;
                        newColumn.HtmlEncode = false;
                        switch (kvp.Key)
                        {
                            case "Created":
                            case "WFDeadline":
                                newColumn.DataFormatString = "{0:d}";
                                break;
                        }
                        gvReport.Columns.Add(newColumn);


                        //Add Initial Column Order
                        columnsOrder.Add(kvp.Key, kvp.Value);
                    }
                    columnsOrder.Add("Steps", "Steps");

                    if (Session["ReportNumMaxSteps"] != null)
                        numMaxSteps = int.Parse(Session["ReportNumMaxSteps"].ToString());

                    foreach (KeyValuePair<String, String> kvp in columnsStepsReport)
                    {
                        if (cont < numMaxSteps)
                        {
                            newColumn = new BoundField();
                            newColumn.HeaderText = kvp.Value;
                            newColumn.DataField = kvp.Key;
                            newColumn.HtmlEncode = false;

                            gvReport.Columns.Add(newColumn);
                            cont++;
                        }
                        else
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("InitializeColumnsReportGrid() - " + ex.Source, ex.Message);
            }
        }
       
        #endregion

        #region ORDER METHODS

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Web"></param>
        /// <returns></returns>
        protected bool GetUserCustomColumns(SPWeb Web)
        {
            bool isCustomColumns = false;

            try
            {
                SPList lstCustomColumns = Web.Lists["RS Reports Custom Columns"];
                SPUser currentUser = Permissions.GetRealCurrentSpUser(this.Page);

                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='RPUser' LookupId='True'/><Value Type='Integer'>" + currentUser.ID + "</Value></Eq></Where>";

                SPListItemCollection itemCollection = lstCustomColumns.GetItems(query);

                if (columnsOrder == null)
                    columnsOrder = new Dictionary<string, string>();

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
                        ViewState["ReportSortingField"] = itemCollection[0]["RPCustomOrder"].ToString().Split(';')[0];
                        ViewState["ReportSortingDirection"] = itemCollection[0]["RPCustomOrder"].ToString().Split(';')[1];
                    }
                    else
                    {
                        ViewState["ReportSortingField"] = "WFID";
                        ViewState["ReportSortingDirection"] = "DESC";
                    }
                    ddlColumnSort.SelectedValue = ViewState["ReportSortingField"].ToString();
                    rblOrder.SelectedValue = ViewState["ReportSortingDirection"].ToString();

                    Session["ReportOrderColumns"] = columnsOrder;
                    isCustomColumns = true;
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GetUSerCustomColumns() - " + ex.Source, ex.Message);
            }

            return isCustomColumns;
        }


        /// <summary>
        /// Sort the columns in the selected order by the user
        /// </summary>
        private void SetOrderColumnsGrid()
        {
            try
            {
                if (Session["ReportOrderColumns"] != null)
                    columnsOrder = (Dictionary<String, String>)Session["ReportOrderColumns"];

                for (int i = 0; i < gvSort.Rows.Count; i++)
                {
                    if (columnsOrder.ContainsKey(gvSort.DataKeys[i].Value.ToString()))
                    {
                        CheckBox cbSelect = (CheckBox)gvSort.Rows[i].FindControl("cbSelect");
                        cbSelect.Checked = true;
                    }
                }

                if (ViewState["ReportSortingField"] != null && ViewState["ReportSortingField"].ToString() != "")
                    ddlColumnSort.SelectedValue = ViewState["ReportSortingField"].ToString();
                if (ViewState["ReportSortingDirection"] != null)
                {
                    if (ViewState["ReportSortingDirection"].ToString() == "DESC")
                        rblOrder.SelectedIndex = 1;
                    else
                        rblOrder.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("SetOrderColumnsGrid() - " + ex.Source, ex.Message);
            }
        }

        /// <summary>
        /// Stores the order of the columns selected by the user
        /// </summary>
        private void GetOrderColumns()
        {
            try
            {
                Dictionary<string[], int> columnsWithOrder = new Dictionary<string[], int>();
                DataKeyArray colsNoVisible = this.gvSort.DataKeys;

                //Get all checked rows
                for (int i = 0; i < gvSort.Rows.Count; i++)
                {
                    CheckBox ctlSelect = (CheckBox)gvSort.Rows[i].FindControl("cbSelect");

                    if (ctlSelect.Checked)
                    {
                        DropDownList ctlDdl = (DropDownList)gvSort.Rows[i].FindControl("ddlOrder");
                        columnsWithOrder.Add(new string[] { (string)colsNoVisible[i].Value, gvSort.Rows[i].Cells[1].Text }, Convert.ToInt32(ctlDdl.SelectedValue));
                    }
                }

                // Order 
                var sortedDict = from entry in columnsWithOrder orderby entry.Value ascending select entry;

                if (columnsOrder != null)
                    columnsOrder.Clear();
                else
                    columnsOrder = new Dictionary<string, string>();

                foreach (KeyValuePair<string[], int> entry in sortedDict)
                {
                    columnsOrder.Add(entry.Key[0], entry.Key[1]);
                }
                Session["ReportOrderColumns"] = columnsOrder;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GetOrderColumns() - " + ex.Source, ex.Message);
            }
        }

        #endregion


        #region SAVE SESSION VARIABLES
        /// <summary>
        /// Saves the status of each control including the result message and the result table
        /// </summary>
        protected void SaveControls()
        {
            try
            {
                Session["ReportFirstDate"] = ((TextBox)dtFirst.Controls[0]).Text;
                Session["ReportLastDate"] = ((TextBox)dtLast.Controls[0]).Text;


                string multiple = "";

                foreach (ListItem item in ddlType.Items)
                {
                    if (item.Selected)
                        multiple = multiple + item.Value + ";";
                }

                if (multiple.Substring(multiple.Length - 1, 1) == ";")
                    multiple = multiple.Substring(0, multiple.Length - 1);

                Session["ReportType"] = multiple;


                multiple = "";

                foreach (ListItem item in ddlStatus.Items)
                {
                    if (item.Selected)
                        multiple = multiple + item.Value + ";";

                }
                if (multiple.Substring(multiple.Length - 1, 1) == ";")
                    multiple = multiple.Substring(0, multiple.Length - 1);

                Session["ReportStatus"] = multiple;

                Session["ReportActor"] = (peActor.SelectedValue == "") ? "" : Permissions.GetUsernameFromClaim(peActor.SelectedValue);
                //Session["ReportActor"] = peActor.CommaSeparatedAccounts;

                multiple = "";


                foreach (ListItem item in ddlRole.Items)
                {
                    if (item.Selected)
                        multiple = multiple + item.Value + ";";

                }
                if (multiple.Substring(multiple.Length - 1, 1) == ";")
                    multiple = multiple.Substring(0, multiple.Length - 1);

                Session["ReportRoles"] = multiple;

                Session["ReportConfidential"] = ddlConfidential.SelectedValue;
                Session["ReportCreated"] = (peCreated.SelectedValue == "") ? "" : Permissions.GetUsernameFromClaim(peCreated.SelectedValue);

                Session["ReportGFPersonalFile"] = cbPersonalFile.Checked ? "Yes" : "No";
                Session["ReportOpenAmountRAL"] = txtOpenAmountRAL.Text;
                Session["ReportAmountCurrentYear"] = txtAmountCurrentYear.Text;
                Session["ReportAmountNextYear"] = txtAmountNextYear.Text;
                Session["ReportAmountToCancel"] = txtAmountToCancel.Text;
                Session["ReportJustification"] = txtJustification.Text;
                Session["ReportGLAccount"] = txtGLAccount.Text;
                Session["ReportBudgetLine"] = txtBudgetLine.Text;
                Session["ReportWFSubject"] = txtWFSubject.Text;

                Session["ReportFreeText"] = txtFreeText.Text;
                Session["ReportResultMessage"] = lblResults.Text;
                Session["ReportOrderColumns"] = columnsOrder;
                Session["ReportResultGrid"] = gvReport.DataSource;
                Session["ReportSortGrid"] = gvSort.DataSource;
                Session["ReportColumnsDefault"] = columnsDefaultReport;
                Session["ReportColumnsSteps"] = columnsStepsReport;

                Session["ReportNumMaxSteps"] = numMaxSteps;

                Session["ReportFreeText"] = txtFreeText.Text;


            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("SaveControls() - " + ex.Source, ex.Message);

            }
        }

        #endregion

        #region Validate

        protected bool ValidateCriteria()
        {
            bool error = false;
            try
            {
                TextBox firstDateTB = dtFirst.Controls[0] as TextBox;
                TextBox lastDateTB = dtLast.Controls[0] as TextBox;

                //FirstDate Field is mandatory
                if (string.IsNullOrEmpty(firstDateTB.Text) || string.IsNullOrEmpty(lastDateTB.Text))
                {
                    lblMandatory.Text = "\"Created\" field is mandatory.";
                    error = true;
                }
                else
                {
                    if (dtFirst.SelectedDate > DateTime.Today)
                    {
                        lblMandatory.Text = "\"From Date\" can not be greater than the current date.";
                        error = true;
                    }
                    else if (dtFirst.SelectedDate > dtLast.SelectedDate)
                    {
                        lblMandatory.Text = "\"From Date\" can not be greater than \"To date\".";
                        error = true;
                    }
                    else
                    {
                        TimeSpan ts = dtLast.SelectedDate - dtFirst.SelectedDate;
                        if (ts.Days > 366)
                        {
                            lblMandatory.Text = "Creation period may not exceed one year";
                            error = true;
                        }
                    }
                }

                if (error)
                    PanelMandatory.Visible = true;
                else
                    PanelMandatory.Visible = false;

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("ValidateCriteria() - " + ex.Source, ex.Message);
            }
            return error;
        }

        #endregion

        #region Controls
        private void EnableControls(bool value)
        {
            this.PanelButtonsBottom.Enabled = value;
            this.PanelCriteria.Enabled = value;
            this.PanelButtonsReport.Enabled = value;

            this.PanelOrder.Enabled = value;

        }

        private void VisibleControls(string state)
        {
            try
            {
                List<Control> ctrlsTrue = new List<Control>();
                List<Control> ctrlsFalse = new List<Control>();



                switch (state)
                {
                    case DataManagement.STATE_REPORT_NEW:
                        ctrlsTrue.Add(PanelButtonsBottom);
                        ctrlsTrue.Add(PanelCriteria);

                        ctrlsFalse.Add(PanelButtonsReport);
                        ctrlsFalse.Add(ResultsPanel);
                        ctrlsFalse.Add(PanelMandatory);
                        //ctrlsFalse.Add(PanelOrder);
                        // ctrlsFalse.Add(btnSave);
                        break;
                    case DataManagement.STATE_REPORT_RESULT:
                        ctrlsTrue.Add(PanelButtonsReport);
                        ctrlsTrue.Add(ResultsPanel);

                        ctrlsTrue.Add(PanelButtonsBottom);
                        ctrlsTrue.Add(PanelCriteria);
                        ctrlsFalse.Add(PanelMandatory);

                        //ctrlsFalse.Add(PanelMyTemplatesAuto);


                        //ctrlsFalse.Add(PanelOrder);

                        break;
                    case DataManagement.STATE_REPORT_CUSTOM:

                        ctrlsTrue.Add(PanelButtonsReport);

                        ctrlsTrue.Add(ResultsPanel);
                        //ctrlsTrue.Add(PanelOrder);

                        ctrlsFalse.Add(PanelButtonsBottom);
                        ctrlsFalse.Add(PanelCriteria);
                        ctrlsFalse.Add(PanelMandatory);


                        //ctrlsFalse.Add(PanelMyTemplatesAuto);

                        //ctrlsFalse.Add(btnSave);

                        break;
                    case DataManagement.STATE_NOACCESS:
                        ctrlsTrue.Add(PanelMandatory);
                        lblMandatory.Text = "The user has no permissions for creating reports";

                        ctrlsFalse.Add(PanelCriteria);
                        //ctrlsFalse.Add(btnSave);

                        ctrlsFalse.Add(PanelButtonsBottom);
                        ctrlsFalse.Add(PanelButtonsReport);
                        ctrlsFalse.Add(ResultsPanel);
                        //ctrlsFalse.Add(PanelOrder);

                        //ctrlsFalse.Add(PanelMyTemplatesAuto);



                        ctrlsFalse.Add(lblTitleReports);
                        break;
                }


                ctrlsFalse.Add(lblTimerJob);



                foreach (Control ctrl in ctrlsTrue)
                {
                    ctrl.Visible = true;
                }

                foreach (Control ctrl in ctrlsFalse)
                {
                    ctrl.Visible = false;
                }


            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("VisibleControls() - " + ex.Source, ex.Message);
            }
        }

        protected void ClearFields()
        {
            try
            {
                dtFirst.ClearSelection();
                dtLast.ClearSelection();
                ddlType.ClearSelection();
                ddlStatus.ClearSelection();
                peActor.ClearSelection();
                ddlRole.ClearSelection();
                ddlConfidential.ClearSelection();
                peCreated.ClearSelection();
                txtOpenAmountRAL.Text = string.Empty;
                txtAmountCurrentYear.Text = string.Empty;
                txtAmountNextYear.Text = string.Empty;
                txtAmountToCancel.Text = string.Empty;
                txtJustification.Text = string.Empty;
                txtGLAccount.Text = string.Empty;
                txtBudgetLine.Text = string.Empty;
                txtWFSubject.Text = string.Empty;
                txtFreeText.Text = String.Empty;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("ClearFields() - " + ex.Source, ex.Message);
            }

        }

        protected void ClearSessionState()
        {
            Session["ReportFirstDate"] = null;
            Session["ReportLastDate"] = null;
            Session["ReportType"] = null;
            Session["ReportStatus"] = null;
            Session["ReportActor"] = null;
            Session["ReportRoles"] = null;
            Session["ReportConfidential"] = null;
            Session["ReportCreated"] = null;
            Session["ReportOpenAmountRAL"] = null;
            Session["ReportAmountCurrentYear"] = null;
            Session["ReportAmountNextYear"] = null;
            Session["ReportAmountToCancel"] = null;
            Session["ReportJustification"] = null;
            Session["ReportGLAccount"] = null;
            Session["ReportBudgetLine"] = null;
            Session["ReportWFSubject"] = null;
            Session["ReportFreeText"] = "";
        }

        protected DataControlFieldCell GetCellByName(GridViewRow Row, String CellName)
        {
            foreach (DataControlFieldCell Cell in Row.Cells)
            {
                if (Cell.ContainingField.SortExpression.ToString() == CellName)
                    return Cell;
            }
            return null;
        }

        #endregion


        /// <summary>
        /// Send notification e-mails according to urgent not urgent rules
        /// </summary>
        /// <param name="user"></param>
        /// <param name="web"></param>
        /// <param name="wfid"></param>
        /// <param name="subject"></param>
        /// <param name="parameters"></param>
        /// <param name="urgentCode"></param>
        private void SendEmail(string users, SPWeb web, string idTemplate, string loggedUser, Dictionary<string, string> parameters)
        {
            string errorMessage = string.Empty;

            try
            {
                if (users != null)
                {
                    if (SPUtility.IsEmailServerSet(web))
                    {
                        if (parameters.ContainsKey("E-mail Report Shared Text") && parameters.ContainsKey("E-mail Report Shared Subject"))
                        {
                            string emailSubject = parameters["E-mail Report Shared Subject"];

                            string emailText = parameters["E-mail Report Shared Text"];

                            string[] nameUsers = users.Split(';');

                            foreach (string nameUser in nameUsers)
                            {
                                SPUser user = web.EnsureUser(nameUser);

                                string linkAccept = "<a href='" + web.CurrentUser.ParentWeb.Url + parameters["Reports Page"] + "?idtemp=" + idTemplate + "&user=" + user.LoginName + "&action=ok'>Accept</a>";
                                string linkCancel = "<a href='" + web.CurrentUser.ParentWeb.Url + parameters["Reports Page"] + "?idtemp=" + idTemplate + "&user=" + user.LoginName + "&action=cancel'>Reject</a>";
                                emailText = string.Format(emailText, loggedUser, linkAccept, linkCancel);

                                if (!SPUtility.SendEmail(web, false, false, user.Email, emailSubject, emailText))
                                {
                                    errorMessage = ". E-mail not sent to " + user.Name + " (" + user.Email + ").";
                                    Methods.SaveErrorsLog("SendEmail() ", errorMessage);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("SendEmail() " + ex.Source, ex.Message);
            }
        }
        

        #region QUERY
        protected void GetResults(bool showSteps)
        {
            try
            {
                using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                {
                    SPWeb Web = Site.OpenWeb();

                    Dictionary<string, string> reportGeneralColumns = ReportsResults.GetHeaderColumns(Web, parameters);
                    Dictionary<string, string> reportStepsColumns = new Dictionary<string,string>();
                    if (showSteps)
                    {
                        if (Session["ReportColumnsSteps"] != null)
                            reportStepsColumns = (Dictionary<String, String>)Session["ReportColumnsSteps"];
                        else
                        {
                            SPSecurity.RunWithElevatedPrivileges(delegate()
                            {
                                using (SPSite elevatedSite = new SPSite(SPContext.Current.Web.Url.ToString()))
                                {
                                    SPWeb elevatedWeb = elevatedSite.OpenWeb();
                                    reportStepsColumns = ReportsResults.GetStepsColumns(elevatedWeb, parameters);
                                }
                            });                            
                        }                        
                    }
                     
                    Dictionary<string, string> reportUserColumns = null; 
                    string sortField = null;
                    string sortDirection = null;
                    if (Session["ReportOrderColumns"] != null)
                        reportUserColumns = (Dictionary<String, String>)Session["ReportOrderColumns"];
                    else 
                    {
                        ReportsResults.GetUserCustomColumns(Web, SPContext.Current.Web.CurrentUser, ref reportUserColumns, ref sortField, ref sortDirection);
                        if (reportUserColumns == null)
                            reportUserColumns = reportGeneralColumns;
                        Session["ReportOrderColumns"] = reportUserColumns;
                        ViewState["ReportSortingField"] = (sortField != null) ? sortField : "WFID";
                        ViewState["ReportSortingDirection"] = (sortDirection != null) ? sortDirection : "DESC";
                    }

                    InitializeCustomizeColumnsForm();

                    //Create DataTable
                    ReportsResults.CreateResultTable(ref resultTable, reportGeneralColumns, reportStepsColumns);

                    //Get WFs (getting information from all DLs)
                    string queryCommonToExecute = CreateUIQueryModule(Web);
                    ReportsResults.UIValuesSearch(queryCommonToExecute, Web, ref resultTable, reportGeneralColumns);

                    //Search By Keyword
                    if (!string.IsNullOrEmpty(txtFreeText.Text.Trim()))
                        ReportsResults.GetResultTableKeywords(Web, ref resultTable, txtFreeText.Text.Trim(), reportGeneralColumns);

                    int numSteps = 0;

                    if (showSteps && resultTable.Rows.Count > 200)
                    {
                        ResultsPanel.Visible = true;
                        lblTimerJob.Visible = true;
                        gvReport.Visible = false;
                        btnCustomize.Visible = false;
                        btnShowSteps.Visible = false;
                        btnExportExcel.Visible = false;

                        SaveReportPending();
                        SPSecurity.RunWithElevatedPrivileges(delegate()
                        {
                            using (SPSite elevatedSite = new SPSite(SPContext.Current.Web.Url.ToString()))
                            {
                                LaunchJob(elevatedSite);
                            }
                        });
                        
                    }
                    else
                    {
                        if (showSteps)
                            ReportsResults.AddStepsData(ref resultTable, ref numSteps, Web, parameters, SPContext.Current.Web.Url);

                        DrawResults(resultTable, reportUserColumns, reportStepsColumns, numSteps);
                    }
                    Session["ReportUserColumns"] = reportUserColumns;
                    Session["ReportResultDatatable"] = resultTable;
                    Session["ReportStepColumns"] = reportStepsColumns;
                    Session["ReportNumStepColumns"] = numSteps * 6;
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GetResults() - " + ex.Message, null);
            }
        }

        protected string CreateUIQueryModule(SPWeb Web)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                List<string> queryList = new List<string>();
                queryList.Add("<Eq><FieldRef Name='ContentType' /><Value Type='Computed'>Workflow</Value></Eq>");
                queryList.Add("<IsNotNull><FieldRef Name='WFActorsSignedRole' /></IsNotNull>");

                //Queries                
                ReportsQuery.CreateQuery_DateTimeFromTo(ref queryList, dtFirst, dtLast);
                ReportsQuery.CreateQuery_WFType(ref queryList, ddlType);
                ReportsQuery.CreateQuery_WFStatus(ref queryList, ddlStatus);

                // Open Amount RAL
                if (!string.IsNullOrEmpty(txtOpenAmountRAL.Text))
                    ReportsQuery.CreateQueryKeyword_OpenAmountRAL(ref queryList, txtOpenAmountRAL.Text.Trim());

                // Amount Current Year
                if (!string.IsNullOrEmpty(txtAmountCurrentYear.Text))
                    ReportsQuery.CreateQueryKeyword_AmountCurrentYear(ref queryList, txtAmountCurrentYear.Text.Trim());

                // Amount Next Year
                if (!string.IsNullOrEmpty(txtAmountNextYear.Text))
                    ReportsQuery.CreateQueryKeyword_AmountNextYear(ref queryList, txtAmountNextYear.Text.Trim());

                // Amount to Cancel
                if (!string.IsNullOrEmpty(txtAmountToCancel.Text))
                    ReportsQuery.CreateQueryKeyword_AmountToCancel(ref queryList, txtAmountToCancel.Text.Trim());

                // Justification
                if (!string.IsNullOrEmpty(txtJustification.Text))
                    ReportsQuery.CreateQueryKeyword_Justification(ref queryList, txtJustification.Text.Trim());

                // GL Account
                if (!string.IsNullOrEmpty(txtGLAccount.Text))
                    ReportsQuery.CreateQueryKeyword_GLAccount(ref queryList, txtGLAccount.Text.Trim());

                // Budget Line
                if (!string.IsNullOrEmpty(txtBudgetLine.Text))
                    ReportsQuery.CreateQueryKeyword_BudgetLine(ref queryList, txtBudgetLine.Text.Trim());

                // Workflow Subject
                if (!string.IsNullOrEmpty(txtWFSubject.Text))
                    ReportsQuery.CreateQueryKeyword_WFSubject(ref queryList, txtWFSubject.Text.Trim());

                // Personal File
                if (cbPersonalFile.Checked)
                    ReportsQuery.CreateQuery_PersonalFile(ref queryList, cbPersonalFile.Checked);

                //Actor + Role -> WFActorsSignedRole
                if (!ddlRole.SelectedValue.Equals("All"))
                {
                    string adGroupName = Permissions.GetADGroupName(ddlRole.SelectedValue, parameters);

                    if (string.IsNullOrEmpty(peActor.SelectedValue))
                        ReportsQuery.CreateQuery_Role(ref queryList, Web, ddlRole, adGroupName);
                    else
                        ReportsQuery.CreateQuery_ActorRole(ref queryList, Web, peActor, ddlRole, parameters, adGroupName);
                }
                else if (!string.IsNullOrEmpty(peActor.SelectedValue))
                    ReportsQuery.CreateQuery_Actor(ref queryList, Web, peActor);

                ReportsQuery.CreateQuery_WFRestricted(ref queryList, ddlConfidential);
                if (!String.IsNullOrEmpty(peCreated.SelectedValue))
                {
                    SPUser user = Web.EnsureUser(peCreated.SelectedValue);
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

        protected void DrawResults(DataTable resultTableGeneral, Dictionary<string, string> reportGeneralColumns, Dictionary<string, string> reportStepsColumns, int numSteps)
        {
            try
            {
                if (resultTableGeneral != null && resultTableGeneral.Rows.Count > 0) 
                {
                    InitializeColumnsReportGrid(reportGeneralColumns, reportStepsColumns, numSteps);
                    DrawGridviewSettings(resultTableGeneral);                        
                }                    
                else
                {
                    ResultsPanel.Visible = true;
                    gvReport.Visible = false;
                    lblResults.Visible = true;
                    btnCustomize.Visible = false;
                    btnShowSteps.Visible = false;
                    btnExportExcel.Visible = false;
                    lblResults.Text = "No results found matching your query.";
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("DrawResults(): " + ex.Message, ex.StackTrace);
            }
        }

        protected void DrawGridviewSettings(DataTable resultFilteredTable)
        {
            try
            {
                gvReport.AllowPaging = true;
                gvReport.AllowSorting = true;
                gvReport.PageSize = 50;
                gvReport.PageIndex = 0;
                gvReport.PagerSettings.Visible = true;
                gvReport.PagerSettings.Mode = PagerButtons.NumericFirstLast;
                gvReport.PagerStyle.Width = gvReport.Width;
                gvReport.PagerStyle.HorizontalAlign = HorizontalAlign.Left;
                gvReport.PagerStyle.SetDirty();
                gvReport.EnableSortingAndPagingCallbacks = false;
                gvReport.PagerStyle.HorizontalAlign = HorizontalAlign.Center;
                gvReport.PagerSettings.Position = PagerPosition.TopAndBottom;
                gvReport.PagerSettings.NextPageText = "Next page";
                gvReport.PagerSettings.PreviousPageText = "Previous page";
                gvReport.HorizontalAlign = HorizontalAlign.Center;

                ViewState["SearchResultData"] = resultFilteredTable;

                if (ViewState["ReportSortingField"] == null && ViewState["ReportSortingDirection"] == null)
                {
                    ViewState["ReportSortingField"] = "WFID";
                    ViewState["ReportSortingDirection"] = "DESC";
                }

                resultFilteredTable.DefaultView.Sort = ViewState["ReportSortingField"] + " " + ViewState["ReportSortingDirection"];

                ResultsPanel.Visible = true;
                gvReport.Visible = true;
                gvReport.DataSource = resultFilteredTable;
                gvReport.DataBind();

                gvReport.Width = Unit.Pixel(1024);
                gvReport.HeaderStyle.CssClass = "header_background";
                gvReport.AlternatingRowStyle.CssClass = "result_grid_even";

                lblResults.Visible = true;

                if (resultFilteredTable.Rows.Count.Equals(1))
                    lblResults.Text = resultFilteredTable.Rows.Count.ToString() + " workflow found matching your query.";
                else
                    lblResults.Text = resultFilteredTable.Rows.Count.ToString() + " workflows found matching your query.";

                //SaveControls();

                VisibleControls(DataManagement.STATE_REPORT_RESULT);
                btnCustomize.Visible = true;
                btnShowSteps.Visible = true;
                btnExportExcel.Visible = true;

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("DrawGridviewSettings(): " + ex.Message, ex.StackTrace);
            }

        }

        protected void InitializeColumnsReportGrid(Dictionary<string, string> reportGeneralColumns, Dictionary<string, string> reportStepsColumns, int numSteps)
        {
            try
            {
                BoundField newColumn;
                int cont = 0;
                gvReport.Columns.Clear();

                foreach (KeyValuePair<String, String> kvp in reportGeneralColumns)
                {
                    newColumn = new BoundField();
                    newColumn.HeaderText = kvp.Value;
                    newColumn.DataField = kvp.Key;
                    newColumn.HtmlEncode = false;
                    newColumn.SortExpression = kvp.Key;
                    switch (kvp.Key)
                    {
                        case "Created":
                        case "WFDeadline":
                            newColumn.DataFormatString = "{0:d}";
                            break;
                    }
                    gvReport.Columns.Add(newColumn);
                }

                foreach (KeyValuePair<String, String> kvp in reportStepsColumns)
                {
                    if (cont < numSteps * 6)
                    {
                        newColumn = new BoundField();
                        newColumn.HeaderText = kvp.Value;
                        newColumn.DataField = kvp.Key;
                        newColumn.HtmlEncode = false;
                        newColumn.SortExpression = kvp.Key;
                        gvReport.Columns.Add(newColumn);
                        cont++;
                    }
                    else
                        break;
                }

                newColumn = new BoundField();
                newColumn.HeaderText = "Delayed";
                newColumn.DataField = "Delayed";
                newColumn.HtmlEncode = false;
                newColumn.HeaderStyle.CssClass = "hideColumn";
                newColumn.ItemStyle.CssClass = "hideColumn";
                gvReport.Columns.Add(newColumn);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("InitializeColumnsReportGrid() - " + ex.Source, ex.Message);
            }
        }

        private void InitializeCustomizeColumnsForm()
        {
            DataTable sortTable = new DataTable();
            DataRow newRow;

            DropDownList ctlDdl;
            CheckBox cbSelect;

            try
            {
                SPWeb Web = SPContext.Current.Web;
                if (Session["ReportOrderColumns"] != null)
                    columnsOrder = (Dictionary<String, String>)Session["ReportOrderColumns"];

                columnsDefaultReport = ReportsResults.GetHeaderColumns(Web, parameters);

                sortTable.Columns.Add("Column");
                sortTable.Columns.Add("DataField");

                var sortGridColumns = (columnsOrder == null) ? columnsDefaultReport : columnsOrder.Concat(columnsDefaultReport.Except(columnsOrder).ToDictionary(x => x.Key, x => x.Value).OrderBy(x => x.Value));

                foreach (KeyValuePair<String, String> kvp in sortGridColumns)
                {
                    if (kvp.Key != "Steps")
                    {
                        newRow = sortTable.NewRow();
                        newRow["Column"] = kvp.Value;
                        newRow["DataField"] = kvp.Key;
                        sortTable.Rows.Add(newRow);
                    }
                }

                gvSort.DataSource = sortTable;
                gvSort.DataBind();

                for (int i = 0; i < sortTable.Rows.Count; i++)
                {
                    ctlDdl = (DropDownList)gvSort.Rows[i].FindControl("ddlOrder");
                    ctlDdl.Attributes.Add("data-order", i.ToString());
                    cbSelect = (CheckBox)gvSort.Rows[i].FindControl("cbSelect");
                    cbSelect.Checked = false;

                    for (int aux = 0; aux < sortTable.Rows.Count; aux++)
                    {
                        ctlDdl.Items.Add(new ListItem((aux + 1).ToString(), (aux + 1).ToString()));
                    }
                    ctlDdl.SelectedIndex = i;
                }


                // Initialize "Sort by" dropdown
                ListItem newItem;

                ddlColumnSort.Items.Clear();
                foreach (KeyValuePair<String, String> kvp in columnsDefaultReport)
                {
                    if (kvp.Key != "Steps")
                    {
                        newItem = new ListItem();
                        newItem.Text = kvp.Value;
                        newItem.Value = kvp.Key;
                        ddlColumnSort.Items.Add(newItem);
                    }
                }
                ddlColumnSort.SelectedIndex = 0;
               
                SetOrderColumnsGrid();
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("InicializeRowsSortGrid() - " + ex.Source, ex.Message);

            }

        }

        protected void LaunchJob(SPSite site)
        {
            //Execute TimerJob
            SPWebApplication webApplication = site.WebApplication;
            foreach (SPJobDefinition jobDefinition in webApplication.JobDefinitions)
            {
                if (jobDefinition.Name == "RSReportsCreateTimerJob")
                {
                    var remoteAdministratorAccessDenied = SPWebService.ContentService.RemoteAdministratorAccessDenied;
                    try
                    {
                        if (remoteAdministratorAccessDenied == true)
                        {
                            SPWebService.ContentService.RemoteAdministratorAccessDenied = false;
                            SPWebService.ContentService.Update();
                        }
                        jobDefinition.RunNow();
                    }
                    catch (Exception ex)
                    {
                        Methods.SaveErrorsLog("LaunchJob() - " + ex.Source, ex.Message);
                    }
                    finally
                    {
                        SPWebService.ContentService.RemoteAdministratorAccessDenied = remoteAdministratorAccessDenied;
                    }
                    break;
                }
            }
        }
        #endregion
    }
}
