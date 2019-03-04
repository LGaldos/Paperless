using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint;
using System.Web;
using System.Data;
using System.Collections.Generic;


namespace ESMA.Paperless.Views.RSWorkflowViewSigned
{
    public partial class RSWorkflowViewSignedUserControl : UserControl
    {
        private ObjectDataSource gridDS;
        const string URL_IMG_CONFIDENTIAL = "/_layouts/15/ESMA.Paperless.Design.v16/images/RSConfidential.gif";
        const string NONCONFIDENTIAL = "Non Restricted";

        private char[] _sep = { ',' };
        private string[] _ssep = { "AND" };

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                this.Page.Response.Cache.SetCacheability(HttpCacheability.NoCache);

                BindGrid();

                if (!IsPostBack)
                {
                    ViewState["FilterExpression"] = "";
                    gvSigned.Sort("SignedDate", SortDirection.Descending);
                }

            }
            catch (Exception ex)
            {
                SaveErrorsLog("Page_Load() - " + ex.Source, ex.Message);
            }
        }

        protected void gridDS_ObjectCreating(object sender, ObjectDataSourceEventArgs e)
        {
            try
            {
                e.ObjectInstance = this;
            }
            catch (Exception ex)
            {
                SaveErrorsLog("gridDS_ObjectCreating() - " + ex.Source, ex.Message);

            }

        }

        protected override void OnPreRender(EventArgs e)
        {
            try
            {
                if (!Page.IsPostBack)
                    gvSigned.DataBind();

                if ((!string.IsNullOrEmpty(gridDS.FilterExpression)) && (gvSigned.FilterFieldValue.Contains("'")))
                {
                    gridDS.FilterExpression = string.Format(
                    gvSigned.FilteredDataSourcePropertyFormat,
                    gvSigned.FilterFieldValue.Replace("'", "''"),
                    gvSigned.FilterFieldName);
                }
                else
                    buildFilterView(gridDS.FilterExpression);
            }
            catch (Exception ex)
            {
                SaveErrorsLog("OnPreRender() - " + ex.Source, ex.Message);

            }
        }

        protected sealed override void Render(HtmlTextWriter writer)
        {
            try
            {
                gvSigned.DataBind();
                base.Render(writer);
            }
            catch (Exception ex)
            {
                SaveErrorsLog("Render() - " + ex.Source, ex.Message);

            }
        }

        private void BindGrid()
        {
            try
            {
                //Use a ObjectDataSource to bind to the data table
                gridDS = new ObjectDataSource();
                gridDS.ID = "gridDS";
                //We select the method the data is pulled from
                gridDS.SelectMethod = "GetDataWorkflows";
                gridDS.TypeName = this.GetType().AssemblyQualifiedName;
                gridDS.ObjectCreating += new ObjectDataSourceObjectEventHandler(gridDS_ObjectCreating);

                gridDS.Filtering += new ObjectDataSourceFilteringEventHandler(gridDS_Filtering);
                gvSigned.Sorting += new GridViewSortEventHandler(gvSigned_Sorting);
                gvSigned.PageIndexChanging += new GridViewPageEventHandler(gvSigned_PageIndexChanging);
                gvSigned.RowDataBound += new GridViewRowEventHandler(gvSigned_RowDataBound);

                //****C98*******
                //this resets the dropdown options for other columns after a filter is selected
                gridDS.FilterExpression = FilterExpression;

                this.Controls.Add(gridDS);

                //Set the datasource of the grid to the instance of ObjectDataSource
                gvSigned.DataSourceID = gridDS.ID;
                gvSigned.AutoGenerateColumns = false;

                //Turns on Paging along with setting the default page size
                gvSigned.AllowPaging = true;
                gvSigned.PageSize = 30;

                //Allows sorting
                gvSigned.AllowSorting = true;

                //Filtering
                gvSigned.AllowFiltering = true;
                //Allows filtering on the Created and ListName columns
                gvSigned.FilterDataFields = "WFID,WFLink,WFSubject,SignedDateText,Amount,Rejection,WFStatus,WFType,AssignedPerson,Urgent,CreatedText,Author,ConfidentialWorkflow";
                gvSigned.FilteredDataSourcePropertyName = "FilterExpression";
                gvSigned.FilteredDataSourcePropertyFormat = "{1} = '{0}'";

            }
            catch (Exception ex)
            {
                SaveErrorsLog("BindGrid() - " + ex.Source, ex.Message);
            }

        }

        protected sealed override void LoadViewState(object savedState)
        {
            try
            {

                base.LoadViewState(savedState);

                if (Context.Request.Form["__EVENTARGUMENT"] != null &&
                     Context.Request.Form["__EVENTARGUMENT"].EndsWith("__ClearFilter__"))
                {
                    // Clear FilterExpression
                    ViewState.Remove("FilterExpression");
                }
            }
            catch (Exception ex)
            {
                SaveErrorsLog("LoadViewState() - " + ex.Source, ex.Message);
            }
        }

        #region <FILTERING>

        void buildFilterView(string filterExp)
        {
            try
            {
                string lastExp = filterExp;
                if (lastExp.Contains("AND"))
                {
                    if (lastExp.Length < lastExp.LastIndexOf("AND") + 4)
                    { lastExp = lastExp.Substring(lastExp.LastIndexOf("AND") + 4); }
                    else
                    { lastExp = string.Empty; }
                }

                //update the filter
                if (!string.IsNullOrEmpty(lastExp))
                    FilterExpression = lastExp;


                //Reset object dataset filter
                if ((!string.IsNullOrEmpty(gridDS.FilterExpression)) && (gvSigned.FilterFieldValue.Contains("'")))
                {
                    gridDS.FilterExpression = string.Format(
                    FilterExpression,
                    gvSigned.FilterFieldValue.Replace("'", "''"),
                    gvSigned.FilterFieldName);
                }
                else if (!string.IsNullOrEmpty(FilterExpression))
                    gridDS.FilterExpression = FilterExpression;

            }
            catch (Exception ex)
            {
                SaveErrorsLog("buildFilterView - " + ex.Source, ex.Message);
            }
        }

        string FilterExpression
        {

            get
            {
                try
                {
                    if (ViewState["FilterExpression"] == null)
                    { ViewState["FilterExpression"] = ""; }

                    return (string)ViewState["FilterExpression"];
                }

                catch (Exception ex)
                {
                    SaveErrorsLog("FilterExpression(GET) - " + ex.Source, ex.Message);
                    return "";
                }
            }

            set
            {

                try
                {
                    string thisFilterExpression = "(" + value.ToString() + ")";
                    List<string> fullFilterExpression = new List<string>();

                    if (ViewState["FilterExpression"] != null)
                    {
                        string[] fullFilterExp = ViewState["FilterExpression"].ToString().Split(_ssep, StringSplitOptions.RemoveEmptyEntries);
                        fullFilterExpression.AddRange(fullFilterExp);

                        //if the filter is gone expression already exist?
                        int index = fullFilterExpression.FindIndex(s => s.Contains(thisFilterExpression));
                        if (index == -1)
                            fullFilterExpression.Add(thisFilterExpression);
                    }
                    else
                    {
                        fullFilterExpression.Add(thisFilterExpression);
                    }

                    //loop through the list<T> and serialize to string
                    string filterExp = string.Empty;
                    fullFilterExpression.ForEach(s => filterExp += s + " AND ");
                    filterExp = filterExp.Remove(filterExp.LastIndexOf(" AND "));

                    if (!filterExp.EndsWith("))") && filterExp.Contains("AND"))
                        filterExp = "(" + filterExp + ")";

                    ViewState["FilterExpression"] = filterExp;
                }
                catch (Exception ex)
                {
                    SaveErrorsLog("FilterExpression(SET) - " + ex.Source, ex.Message);
                }
            }


        }

        private void gridDS_Filtering(object sender, ObjectDataSourceFilteringEventArgs e)
        {
            ViewState["FilterExpression"] = ((ObjectDataSourceView)sender).FilterExpression;
        }


        #endregion

        #region <SORTING>

        void gvSigned_Sorting(object sender, GridViewSortEventArgs e)
        {
            try
            {
                string lastExpression = "";
                if (ViewState["SortExpression"] != null)
                    lastExpression = ViewState["SortExpression"].ToString();
                string lastDirection = "asc";
                if (ViewState["SortDirection"] != null)
                    lastDirection = ViewState["SortDirection"].ToString();
                string newDirection = string.Empty;
                if (e.SortExpression == lastExpression)
                {
                    e.SortDirection = (lastDirection == "asc") ? System.Web.UI.WebControls.SortDirection.Descending : System.Web.UI.WebControls.SortDirection.Ascending;
                } newDirection = (e.SortDirection == System.Web.UI.WebControls.SortDirection.Descending) ? "desc" : "asc";

                ViewState["SortExpression"] = e.SortExpression;
                ViewState["SortDirection"] = newDirection;
                gvSigned.DataBind();
                //********C98********

                //keep the filter
                //keep the filter
                if ((!string.IsNullOrEmpty(gridDS.FilterExpression)) && (gvSigned.FilterFieldValue.Contains("'")))
                {
                    gridDS.FilterExpression = string.Format(
                    gvSigned.FilteredDataSourcePropertyFormat,
                    gvSigned.FilterFieldValue.Replace("'", "''"),
                    gvSigned.FilterFieldName);
                }
                else if (!string.IsNullOrEmpty(FilterExpression))
                    gridDS.FilterExpression = FilterExpression;
                //********C98********
            }
            catch (Exception ex)
            {
                SaveErrorsLog("gvSigned_Sorting() - " + ex.Source, ex.Message);
            }
        }

        #endregion

        #region <PAGING>

        void gvSigned_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            try
            {
                gvSigned.PageIndex = e.NewPageIndex;
                gvSigned.DataSourceID = gridDS.ID;

                if ((ViewState["FilterExpression"] != null) && (gvSigned.FilterFieldValue.Contains("'")))
                {
                    gridDS.FilterExpression = string.Format(
                    (string)ViewState["FilterExpression"],
                    gvSigned.FilterFieldValue.Replace("'", "''"),
                    gvSigned.FilterFieldName);
                }
                else if (ViewState["FilterExpression"] != null)
                    gridDS.FilterExpression = (string)ViewState["FilterExpression"];


                gvSigned.DataBind();


            }
            catch (Exception ex)
            {
                SaveErrorsLog("gridDS_PageIndexChanging() - " + ex.Source, ex.Message);
            }
        }
        #endregion

        #region <ROW>

        private void gvSigned_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            try
            {
                if (e.Row.RowType == DataControlRowType.DataRow)
                {
                    //TableCell linkCell = e.Row.Cells[1];

                    //Change column Link
                    HyperLink linkCell = e.Row.Cells[1].Controls[0] as HyperLink;
                    Label labelCellID = e.Row.Cells[0].Controls[0] as Label;

                    Label labelCell;


                    labelCell = e.Row.Cells[13].Controls[0] as Label;
                    linkCell = e.Row.Cells[1].Controls[0] as HyperLink;
                    if (linkCell != null)
                    {

                        linkCell.NavigateUrl = labelCell.Text.Replace("amp;", "");
                        linkCell.Text = labelCellID.Text;
                    }

                    //Change column Rejection
                    labelCell = e.Row.Cells[14].Controls[0] as Label;

                    if (labelCell != null && labelCell.Text != "")
                    {
                        Image imageCell = new Image();
                        imageCell.ImageUrl = labelCell.Text.Split(',')[0];

                        e.Row.Cells[5].Controls.Add(imageCell);
                        labelCell.Text = "";
                    }


                    //Change column Confidential
                    labelCell = e.Row.Cells[12].Controls[0] as Label;
                    if (labelCell != null && labelCell.Text != "")
                    {
                        if (labelCell.Text != NONCONFIDENTIAL)
                        {
                            Image imageCell = new Image();
                            imageCell.ImageUrl = URL_IMG_CONFIDENTIAL;
                            e.Row.Cells[12].Controls.Add(imageCell);
                        }
                        labelCell.Text = "";
                    }

                }
            }
            catch (Exception ex)
            {
                SaveErrorsLog("gvSigned_RowDataBound() - " + ex.Source, ex.Message);
            }


        }

        #endregion

        #region <INFORMATION>

        protected DataTable CreateDataTable()
        {
            DataTable dtData = new DataTable();
            try
            {
                dtData.Columns.Add("WFID");
                dtData.Columns.Add("WFLink");
                dtData.Columns.Add("WFSubject");
                dtData.Columns.Add("SignedDateText");
                dtData.Columns.Add("Amount");
                dtData.Columns.Add("Rejection");
                dtData.Columns.Add("WFStatus");
                dtData.Columns.Add("ConfidentialWorkflow");
                dtData.Columns.Add("WFType");
                dtData.Columns.Add("CreatedText");
                dtData.Columns.Add("Author");
                dtData.Columns.Add("AssignedPerson");
                dtData.Columns.Add("Urgent");
                dtData.Columns.Add("WFLinkText");
                dtData.Columns.Add("WFRejectionText");
                dtData.Columns.Add("SignedDate", typeof(DateTime));
                dtData.Columns.Add("Created", typeof(DateTime));
            }
            catch (Exception ex)
            {
                SaveErrorsLog("CreateDataTable() - " + ex.Source, ex.Message);

            }
            return dtData;
        }

        public DataTable GetDataWorkflows()
        {
            DataTable tableWF = CreateDataTable();
            string WFID = string.Empty;

            try
            {

                this.Page.Response.Cache.SetCacheability(HttpCacheability.NoCache);


                using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                {
                    SPWeb Web = Site.OpenWeb();

                    SPList listHistory = Web.GetListFromWebPartPageUrl(Web.Url + "/Lists/WFHistory/AllItems.aspx");
                    SPListItemCollection collWF;
                    SPUser user = GetRealCurrentSPUser(this.Page);

                    if (user != null)
                    {
                        string nameUser = user.LoginName.Split('\\')[1].Replace(@"\", "");
                        DataRow newRow;
                        collWF = GetWFSignedByMeQuery(Web, listHistory, nameUser);
                        int position;
                        string signedDate;


                        foreach (SPListItem item in collWF)
                        {

                            try
                            {
                                newRow = tableWF.NewRow();

                                //WFID
                                WFID = item["WFID"].ToString();
                                newRow["WFID"] = WFID;
                                newRow["WFLink"] = WFID;

                                if (item["WFLink"] != null)
                                    newRow["WFLinkText"] = item["WFLink"].ToString().Split(',')[0];

                                newRow["WFSubject"] = item["WFSubject"];
                                signedDate = "";

                                if (item["AllActorsSign"] != null && item["AllActorsSign"].ToString() != "")
                                {
                                    position = item["AllActorsSign"].ToString().IndexOf(nameUser);

                                    if (position > -1)
                                        signedDate = item["AllActorsSign"].ToString().Substring(position + 1 + nameUser.Length, 10);

                                }
                                newRow["SignedDate"] = DateTime.Parse(signedDate);
                                newRow["SignedDateText"] = signedDate;
                                newRow["Amount"] = item["Amount"];
                                newRow["Rejection"] = "";
                                newRow["WFRejectionText"] = item["Rejection"];
                                newRow["WFStatus"] = item["WFStatus"];
                                newRow["ConfidentialWorkflow"] = item["ConfidentialWorkflow"];
                                newRow["WFType"] = item["WFType"];
                                newRow["Created"] = item["Created"];
                                newRow["CreatedText"] = DateTime.Parse(item["Created"].ToString()).ToString("dd/MM/yyyy");

                                //Author
                                string author = item[SPBuiltInFieldId.Author].ToString();

                                if (!author.ToLower().Contains("system account"))
                                    newRow["Author"] = author.Split('#')[1];
                                else
                                {
                                    //Initiated By
                                    author = item["InitiatedBy"].ToString();
                                    newRow["Author"] = author.Split('#')[1];
                                }


                                //Assigned Person
                                if (item["AssignedPerson"] != null && item["AssignedPerson"].ToString() != "")
                                    newRow["AssignedPerson"] = item["AssignedPerson"].ToString().Split('#')[1];

                                if ((bool)item["Urgent"] == true)
                                    newRow["Urgent"] = "Yes";
                                else
                                    newRow["Urgent"] = "No";

                                tableWF.Rows.Add(newRow);
                            }
                            catch
                            {
                                SaveErrorsLog("GetDataWorkflows() - WFID: '" + WFID + "' not showed - User: " + user, null);
                                continue;
                            }

                        }
                    }
                    else
                        SaveErrorsLog("GetDataWorkflows() - SignedWFs - User: '" + SPContext.Current.Web.CurrentUser.LoginName + "' not found.", null);

                }


            }
            catch (Exception ex)
            {
                SaveErrorsLog("GetDataWorkflows() -  User: '" + SPContext.Current.Web.CurrentUser.LoginName + "' " + ex.Source, ex.Message);

            }

            return tableWF;
        }

        protected SPListItemCollection GetWFSignedByMeQuery(SPWeb Web, SPList lstHistory, string user)
        {
            SPListItemCollection itemCollection = null;

            try
            {
                SPQuery query = new SPQuery();
                query.ViewFields = string.Concat(
                                 "<FieldRef Name='WFID' />",
                                 "<FieldRef Name='WFLink' />",
                                 "<FieldRef Name='WFSubject' />",
                                 "<FieldRef Name='Amount' />",
                                  "<FieldRef Name='Rejection' />",
                                  "<FieldRef Name='WFRejectionText' />",
                                  "<FieldRef Name='WFStatus' />",
                                  "<FieldRef Name='ConfidentialWorkflow' />",
                                  "<FieldRef Name='WFType' />",
                                  "<FieldRef Name='Created' />",
                                  "<FieldRef Name='Author' />",
                                  "<FieldRef Name='InitiatedBy' />",
                                  "<FieldRef Name='AssignedPerson' />",
                                  "<FieldRef Name='AllActorsSign' />",
                                  "<FieldRef Name='SignedBy' />",
                                  "<FieldRef Name='Urgent' />",
                                  "<FieldRef Name='Modified' />");

                query.ViewFieldsOnly = true; // Fetch only the data that we need.
                query.Query = "<Where><And><Contains><FieldRef Name='AllActorsSign' /><Value Type='Text'>" + user + "</Value></Contains>"
                    + "<Neq><FieldRef Name='WFStatus' /><Value Type='Choice'>Draft</Value></Neq>"
                    + "</And></Where><OrderBy><FieldRef Name='Modified' Ascending='False' /></OrderBy>";
                itemCollection = lstHistory.GetItems(query);


            }
            catch (Exception ex)
            {
                SaveErrorsLog("GetWFSignedByMeQuery() - User: " + user + " " + ex.Source, ex.Message);

            }
            return itemCollection;
        }

        #endregion

        #region <PERMISSIONS>

        public static SPUser GetRealCurrentSPUser(Page currPage)
        {
            SPUser userLogin = null;

            try
            {
                userLogin = SPContext.Current.Web.CurrentUser;

                if (userLogin.ToString().ToLower().Equals(@"sharepoint\system"))
                    return SPContext.Current.Web.SiteUsers[userLogin.LoginName];
                else
                {
                    SPUser user = SPContext.Current.Web.EnsureUser(userLogin.LoginName);
                    return user;
                }

            }
            catch
            {
                SaveErrorsLog("GetRealCurrentSPUser() " + userLogin.LoginName.ToString(), string.Empty);
                return SPContext.Current.Web.SiteUsers[userLogin.LoginName];

            }
        }

        #endregion

        #region <ERRORS>

        public static void SaveErrorsLog(string source, string message)
        {
            try
            {
                string userAccount = SPContext.Current.Web.CurrentUser.LoginName.ToString();

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite colsit = new SPSite(SPContext.Current.Web.Url.ToString()))
                    {
                        SPWeb MyWeb = colsit.OpenWeb();

                        if (!MyWeb.AllowUnsafeUpdates)
                            MyWeb.AllowUnsafeUpdates = true;

                        string listErrorName = "RS Error Log";
                        SPList myList = MyWeb.Lists[listErrorName];
                        string messageToSave = "[RSWorkflowViewSigned '" + userAccount + "'] " + source + " - " + message;


                        if (myList != null)
                        {
                            SPQuery query = new SPQuery();
                            query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + message + "</Value></Eq></Where>";

                            SPListItemCollection itemCollection = myList.GetItems(query);
                            SPListItem itm = null;

                            if (itemCollection.Count > 0)
                            {
                                itm = itemCollection[0];
                                itm["Title"] = messageToSave;
                            }
                            else
                            {
                                itm = myList.Items.Add();
                                itm["Title"] = messageToSave;
                            }


                            try
                            {
                                itm.Update();
                            }
                            catch { }

                        }

                        if (MyWeb.AllowUnsafeUpdates)
                            MyWeb.AllowUnsafeUpdates = false;

                        MyWeb.Close();
                        MyWeb.Dispose();


                    }

                });

            }
            catch
            {

            }
        }

        //------------------------------------------------------------------------------------------------
        //FUNCTION: We are going to use this function to decrypt the values of the fields user and password,
        //which they are encrypted in the web.config.
        //-----------------------------------------------------------------------------------------------
        public static string Decrypt(string data)
        {
            string result = string.Empty;

            try
            {
                System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
                System.Text.Decoder utf8Decode = encoder.GetDecoder();

                byte[] todecode_byte = Convert.FromBase64String(data);
                int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
                char[] decoded_char = new char[charCount];
                utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
                result = new String(decoded_char);
            }
            catch (Exception ex)
            {
                SaveErrorsLog("Decrypt() - " + ex.Source, ex.Message);
            }
            return result;
        }

        #endregion
    }
}
