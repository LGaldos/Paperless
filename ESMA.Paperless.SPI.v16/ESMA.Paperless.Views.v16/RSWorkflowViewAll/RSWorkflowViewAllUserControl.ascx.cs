using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebPartPages;
using System.Web;
using System.Data;
using System.Collections.Generic;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Administration.Claims;

namespace ESMA.Paperless.Views.RSWorkflowViewAll
{
    public partial class RSWorkflowViewAllUserControl : UserControl
    {
        Dictionary<string, string> parameters;
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

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                    {
                        SPWeb Web = Site.OpenWeb();
                        parameters = GetConfigurationParameters(Web);

                        if ((parameters.ContainsKey("Domain")) && (parameters.ContainsKey("RS Auditors Group")))
                        {
                            string domain = parameters["Domain"];

                            if (parameters.ContainsKey("AD User") && parameters.ContainsKey("AD Password"))
                            {

                                string userAD = Decrypt(parameters["AD User"]);
                                string passwordAD = Decrypt(parameters["AD Password"]);

                                BindGrid();

                                if (!IsPostBack)
                                {
                                    ViewState["FilterExpression"] = "";
                                }


                            }
                        }
                    }

                });

            }
            catch (Exception ex)
            {
                SaveErrorsLog("Page_Load() - ViewAllWFs: " + ex.Source, ex.Message);
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
                SaveErrorsLog("gridDS_ObjectCreating() - ViewAllWFs: " + ex.Source, ex.Message);

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
                SaveErrorsLog("Render() - ViewAllWFs:" + ex.Source, ex.Message);
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
                

                //base.OnPreRender(e);

            }
            catch (Exception ex)
            {
                SaveErrorsLog("OnPreRender - ViewAllWFs:" + ex.Source, ex.Message);
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
                gvSigned.FilterDataFields = "WFID,WFLink,WFSubject,Amount,Rejection,WFStatus,WFType,AssignedPerson,Urgent,Created,Author,SignedBy,ConfidentialWorkflow";
                gvSigned.FilteredDataSourcePropertyName = "FilterExpression";
                gvSigned.FilteredDataSourcePropertyFormat = "{1} = '{0}'";

            }
            catch (Exception ex)
            {
                SaveErrorsLog("BindGrid() - ViewAllWFs:" + ex.Source, ex.Message);
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
                SaveErrorsLog("LoadViewState() - ViewAllWFs: " + ex.Source, ex.Message);
            }
        }

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
                SaveErrorsLog("gvSigned_Sorting() - ViewAllWFs: " + ex.Source, ex.Message);
            }
        }

        #endregion

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

                //Update the filter
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
                SaveErrorsLog("buildFilterView - ViewAllWFs: " + ex.Source, ex.Message);
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
                    SaveErrorsLog("FilterExpression(GET) - ViewAllWFs: " + ex.Source, ex.Message);
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
                    SaveErrorsLog("FilterExpression(SET) - ViewAllWFs: " + ex.Source, ex.Message);
                }
            }


        }

        private void gridDS_Filtering(object sender, ObjectDataSourceFilteringEventArgs e)
        {
            try
            {
                ViewState["FilterExpression"] = ((ObjectDataSourceView)sender).FilterExpression;
                
            }
            catch (Exception ex)
            {
                SaveErrorsLog("gridDS_Filtering() - ViewAllWFs: " + ex.Source, ex.Message);
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
                SaveErrorsLog("gridDS_PageIndexChanging() - ViewAllWFs: " + ex.Source, ex.Message);
            }
        }

        #endregion

        #region <INFORMATION>

        protected SPListItemCollection GetAllWorkflows(SPWeb Web, SPList lstHistory)
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
                query.Query = "<OrderBy><FieldRef Name='WFID' Ascending='False' /></OrderBy>";
                itemCollection = lstHistory.GetItems(query);

            }
            catch (Exception ex)
            {
                SaveErrorsLog("GetAllWorkflows() - ViewAllWFs: " + ex.Source, ex.Message);

            }
            return itemCollection;
        }

        protected DataTable CreateDataTable()
        {
            DataTable dtData = new DataTable();
            try
            {

                dtData.Columns.Add("WFID");
                dtData.Columns.Add("WFLink");
                dtData.Columns.Add("WFSubject");
                dtData.Columns.Add("Amount");
                dtData.Columns.Add("Rejection");
                dtData.Columns.Add("WFStatus");
                dtData.Columns.Add("ConfidentialWorkflow");
                dtData.Columns.Add("WFType");
                dtData.Columns.Add("Created", typeof(DateTime));
                dtData.Columns.Add("Author");
                dtData.Columns.Add("AssignedPerson");
                dtData.Columns.Add("SignedBy");
                dtData.Columns.Add("Urgent");
                dtData.Columns.Add("WFLinkText");
                dtData.Columns.Add("WFRejectionText");


            }
            catch (Exception ex)
            {
                SaveErrorsLog("CreateDataTable() - ViewAllWFs: " + ex.Source, ex.Message);

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
                        collWF = GetAllWorkflows(Web, listHistory);


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

                                newRow["Amount"] = item["Amount"];
                                newRow["Rejection"] = "";
                                newRow["WFRejectionText"] = item["Rejection"];
                                newRow["WFStatus"] = item["WFStatus"];
                                newRow["ConfidentialWorkflow"] = item["ConfidentialWorkflow"];
                                newRow["WFType"] = item["WFType"];
                                newRow["Created"] = DateTime.Parse(item["Created"].ToString());

                                //Author
                                string author = item[SPBuiltInFieldId.Author].ToString();

                                if (!author.ToLower().Contains("system account"))
                                    newRow["Author"] = author.Split('#')[1];
                                else
                                {
                                    if (item["InitiatedBy"] != null)
                                    {
                                        //Initiated By
                                        author = item["InitiatedBy"].ToString();
                                        newRow["Author"] = author.Split('#')[1];
                                    }
                                    else
                                        newRow["Author"] = "";
                                }

                                //Asigned Person
                                if (item["AssignedPerson"] != null && item["AssignedPerson"].ToString() != "")
                                    newRow["AssignedPerson"] = item["AssignedPerson"].ToString().Split('#')[1];

                                //AllActorsSign
                                if (item["AllActorsSign"] != null && item["AllActorsSign"].ToString() != "")
                                {
                                    if (!string.IsNullOrEmpty(nameUser) && (item["AllActorsSign"].ToString().Contains(nameUser)))
                                        newRow["SignedBy"] = user.Name;
                                    else
                                        newRow["SignedBy"] = "";
                                }
                                else
                                    newRow["SignedBy"] = "";

                                if ((bool)item["Urgent"] == true)
                                    newRow["Urgent"] = "Yes";
                                else
                                    newRow["Urgent"] = "No";

                                tableWF.Rows.Add(newRow);
                            }
                            catch
                            {
                                SaveErrorsLog("GetDataWorkflows() ViewAll - Error WFID: '" + WFID + "' not showed.", null);
                                continue;
                            }

                        }
                    }
                    else
                        SaveErrorsLog("GetDataWorkflows() - ViewAllWFs - User: '" + SPContext.Current.Web.CurrentUser.LoginName + "' not found.", null);


                }

            }
            catch (Exception ex)
            {
                SaveErrorsLog("GetDataWorkflows() - ViewAllWFs - WFID: '" + WFID + "' " + ex.Source, ex.Message);
            }

            return tableWF;
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

                        e.Row.Cells[4].Controls.Add(imageCell);
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
                SaveErrorsLog("gvSigned_RowDataBound() - ViewAllWFs: " + ex.Source, ex.Message);
            }


        }

        #endregion

        #region <PARAMETERS>

        public static Dictionary<string, string> GetConfigurationParameters(SPWeb Web)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            try
            {
                SPList list = Web.GetListFromWebPartPageUrl(Web.Url + "/Lists/WFConfigParameters/AllItems.aspx");

                SPQuery query = new SPQuery();
                query.ViewFields = string.Concat(
                                  "<FieldRef Name='Title' />",
                                  "<FieldRef Name='Value1' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need.
                SPListItemCollection itemCollection = list.GetItems(query);

                foreach (SPListItem item in itemCollection)
                {
                    try
                    {
                        if (item["Value1"] != null)
                            parameters.Add(item.Title, item["Value1"].ToString().Trim());
                    }
                    catch { continue; }
                }
            }
            catch (Exception ex)
            {
                SaveErrorsLog("GetConfigurationParameters() - ViewAllWFs: " + ex.Source, ex.Message);
            }

            return parameters;
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
                        string messageToSave = "[RSWorkflowViewAll '" + userAccount + "'] " + source + " - " + message;


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
