using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Collections.Generic;
using Microsoft.SharePoint;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.ComponentModel;



namespace ESMA.Paperless.Webparts.v16.RSWorkflowAdvancedSearch
{
    public partial class RSWorkflowAdvancedSearchUserControl : UserControl
    {
        DataTable resultTable;

        private char[] _sep = { ',' };
        private string[] _ssep = { "AND" };
        public Dictionary<string, string> parameters;
        public Dictionary<string, string> wftypeCodes;


        #region <LOAD>

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                this.Page.Response.Cache.SetCacheability(HttpCacheability.NoCache);

                string siteURL = SPContext.Current.Web.Url;

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(siteURL))
                    {
                        SPWeb Web = Site.OpenWeb();

                        parameters = Methods.GetConfigurationParameters(Web);
                        wftypeCodes = Methods.GetWorkflowTypeOrder(Web);

                        if (parameters != null)
                        {
                            if (!this.Page.IsPostBack)
                                lblResults.Visible = false;

                            LoadControls(Web);

                            if (!this.Page.IsPostBack)
                            {
                                ViewState["SearchResultData"] = null;
                                ViewState["SearchSortingField"] = null;
                                ViewState["SearchSortingDirection"] = null;
                            }


                        }

                        Web.Close();
                        Web.Dispose();
                    }
                });
                btnSearch.Click += new EventHandler(btnSearch_Click);
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("Page_Load: " + ex.Message, string.Empty);
            }
        }

        /// <summary>
        /// Loads the controls with the proper values according to the browser session states as well as it loads the result message and result table
        /// </summary>
        /// <param name="Web"></param>
        protected void LoadControls(SPWeb Web)
        {
            try
            {
                btnSearch.OnClientClick = "var gridID = '" + gvResults.ClientID + "'; var gridElement = document.getElementById(gridID); var resultID = '" + lblResults.ClientID + "'; var resultElement = document.getElementById(resultID);  if(gridElement !== null) gridElement.style.display='none'; if(resultElement !== null) resultElement.style.display='none';";

                if (!this.Page.IsPostBack && parameters.ContainsKey("Interface Page") && HttpContext.Current.Request.UrlReferrer.ToString() != null && HttpContext.Current.Request.UrlReferrer.ToString().ToUpper().Contains(parameters["Interface Page"].ToUpper()))
                {
                    //DO NOTHING
                }
                else if (this.Page.IsPostBack && IsSearchingPostback(this.Page))
                {
                    int sessionCount = Session.Count;
                    for (int i = 0; i < sessionCount; i++) { try { if (Session.Keys[i].ToUpper().StartsWith("SEARCH")) Session[i] = null; } catch { continue; } }
                }
                else if (!this.Page.IsPostBack)
                {
                    int sessionCount = Session.Count;
                    for (int i = 0; i < sessionCount; i++) { try { if (Session.Keys[i].ToUpper().StartsWith("SEARCH")) Session[i] = null; } catch { continue; } }
                }


                //Load Values
                InitializeWFTypes(Web);
                InitializeWFStatus(Web);
                InitializeFWCReference(Web); //CR30
                InitializeWFRoles(Web, parameters["Domain"]);
                InitializeWFActor(Web, parameters["Domain"]); //RS33
                InitializeWFStaff(Web, parameters["Domain"]); //RS33
                InitializeDateTimeControls(Web);
                InitializeSession();

  
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("LoadControls: " + ex.Message, string.Empty);
            }
        }

        private bool IsSearchingPostback(Page page)
        {
            bool isSearching = false;
            Control control = null;
            string ctrlname = page.Request.Params.Get("__EVENTTARGET");
            if (ctrlname != null && ctrlname != string.Empty)
            {
                control = page.FindControl(ctrlname);
            }
            else
            {
                foreach (string ctl in page.Request.Form)
                {
                    Control mycontrol = page.FindControl(ctl);
                    if (mycontrol is System.Web.UI.WebControls.Button)
                    {
                        Button btn = (Button)mycontrol;
                        if (btn.Text.ToUpper().Contains("SEARCH"))
                            isSearching = true;
                        break;
                    }
                }
            }
            return isSearching;
        }

        /// <summary>
        /// Saves the status of each control including the result message and the result table
        /// </summary>
        protected void SaveControls()
        {
            try
            {
                foreach (Control ctrl in dtFrom.Controls)
                {
                    if (ctrl is TextBox)
                    {
                        Session["SearchFromDate"] = ((TextBox)ctrl).Text;
                        break;
                    }
                }

                foreach (Control ctrl in dtTo.Controls)
                {
                    if (ctrl is TextBox)
                    {
                        Session["SearchToDate"] = ((TextBox)ctrl).Text;
                        break;
                    }
                }

                foreach (Control ctrl in dtDeadlineFrom.Controls)
                {
                    if (ctrl is TextBox)
                    {
                        Session["SearchDeadlineFromDate"] = ((TextBox)ctrl).Text;
                        break;
                    }
                }

                foreach (Control ctrl in dtDeadlineTo.Controls)
                {
                    if (ctrl is TextBox)
                    {
                        Session["SearchDeadlineToDate"] = ((TextBox)ctrl).Text;
                        break;
                    }
                }

                Session["SearchTitle"] = txtTitle.Text;
                Session["SearchID"] = txtID.Text;
                Session["SearchType"] = ddlType.SelectedIndex;
                Session["SearchStatus"] = ddlStatus.SelectedIndex;
                //CR33
                Session["SearchActor"] = ddlActor.SelectedIndex;
                Session["SearchRole"] = ddlRole.SelectedIndex;
                Session["SearchStaff"] = ddlStaff.SelectedIndex;
                Session["SearchUrgent"] = ddlUrgent.SelectedIndex;
                Session["SearchVAT"] = ddlVAT.SelectedIndex;
                Session["SearchABAC"] = txtABAC.Text;
                Session["SearchIncident"] = ddlIncident.SelectedIndex;
                Session["SearchSignedWF"] = cbSignedByMe.Text;
                Session["SearchKeyword"] = txtKeyWord.Text;
                //CR30
                Session["SearchContractor"] = txtContractor.Text;
                Session["SearchFWCReference"] = ddlFWCReference.SelectedIndex;
                Session["SearchVacancyNo"] = txtVacancyNo.Text;
                Session["SearchPersonalFile"] = txtPersonalFile.Text;
                Session["SearchResultMessage"] = lblResults.Text;
                Session["SearchResultGrid"] = gvResults.DataSource;



            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("SaveControls: " + ex.Message, null);
            }
        }


        #region <INITIALIZE CONTROLS>

        protected void InitializeDateTimeControls(SPWeb Web)
        {
            try
            {
                //From
                dtFrom.LocaleId = Web.Locale.LCID;
                dtDeadlineFrom.LocaleId = Web.Locale.LCID;
                //To
                dtTo.LocaleId = Web.Locale.LCID;
                dtDeadlineTo.LocaleId = Web.Locale.LCID;
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("InitializeDateTimeControls() " + ex.Message, string.Empty);
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
                    SPList list = Web.Lists["RS Workflow Configuration"];
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><IsNotNull><FieldRef Name='Title'/></IsNotNull></Where>";
                    query.ViewFields = string.Concat(
                                       "<FieldRef Name='Title' />");
                    query.ViewFieldsOnly = true; // Fetch only the data that we need
                    SPListItemCollection itemCollection = list.GetItems(query);

                    List<ListItem> ddlTypeItems = new List<ListItem>();
                    foreach (SPListItem item in itemCollection)
                    {
                        if (item["Title"] != null)
                        {
                            ListItem ddlItem = new ListItem(item["Title"].ToString().ToUpper());
                            if (ddlTypeItems.IndexOf(ddlItem) <= 0)
                                ddlTypeItems.Add(ddlItem);
                        }
                    }

                    ddlTypeItems.Sort((x, y) => string.Compare(x.Value, y.Value));
                    ddlType.Items.AddRange(ddlTypeItems.ToArray());
                    ListItem defaultItem = new ListItem("All");
                    ddlType.Items.Insert(0, defaultItem);
                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("InitializeWFTypes: " + ex.Message, string.Empty);
            }
        }

        //CR33

        /// <summary>
        /// Load actors in DropDownList control
        /// </summary>
        /// <param name="Web"></param>
        protected void InitializeWFActor(SPWeb Web, string domain)
        {
            try
            {
                if (!this.Page.IsPostBack)
                {

                    Dictionary<string, string> groupUsers = new Dictionary<string, string>();
                    groupUsers.Add("", "");
                    foreach (SPUser user in Web.AllUsers)
                    {
                        if (!user.IsDomainGroup)
                        {
                            string userName = user.Name.Replace(domain + "\\", "");

                            if ((!userName.ToLower().Equals("system account")) && (!userName.ToLower().StartsWith("nt authority")))
                                groupUsers.Add(user.LoginName, userName);
                        }
                    }

                    ddlActor.DataSource = groupUsers;
                    ddlActor.DataTextField = "Value";
                    ddlActor.DataValueField = "Key";
                    ddlActor.DataBind();

                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("InitializeWFActor: " + ex.Message, string.Empty);
            }
        }

        //CR33
        /// <summary>
        /// Load actors in DropDownList control
        /// </summary>
        /// <param name="Web"></param>
        protected void InitializeWFStaff(SPWeb Web, string domain)
        {
            try
            {
                if (!this.Page.IsPostBack)
                {

                    Dictionary<string, string> groupUsers = new Dictionary<string, string>();
                    groupUsers.Add("", "");
                    foreach (SPUser user in Web.AllUsers)
                    {
                        if (!user.IsDomainGroup)
                        {
                            string userName = user.Name.Replace(domain + "\\", "");

                            if ((!userName.ToLower().Equals("system account")) && (!userName.ToLower().StartsWith("nt authority")))
                                groupUsers.Add(user.LoginName, userName);
                        }
                    }

                    ddlStaff.DataSource = groupUsers;
                    ddlStaff.DataTextField = "Value";
                    ddlStaff.DataValueField = "Key";
                    ddlStaff.DataBind();

                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("InitializeWFStaff: " + ex.Message, string.Empty);
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
                Methods.saveErrorsLog("InitializeWFStatus: " + ex.Message, string.Empty);
            }
        }

        /// <summary>
        /// Load possible FWC Reference in FWC Reference control.
        /// </summary>
        /// <param name="Web"></param>
        protected void InitializeFWCReference(SPWeb Web)
        {
            try
            {
                if (!this.Page.IsPostBack)
                {
                    SPFieldChoice FWCRefField = null;

                    try
                    {
                        FWCRefField = new SPFieldChoice(Web.Fields, "GFFWCRef");
                    }
                    catch
                    {
                        FWCRefField = new SPFieldChoice(Web.Site.RootWeb.Fields, "GFFWCRef");
                    }

                    if (FWCRefField != null)
                    {
                        List<ListItem> ddlFWCRefItemsList = new List<ListItem>();
                        foreach (string choice in FWCRefField.Choices)
                        {
                            ListItem ddlItem = new ListItem(choice);
                            if (!ddlFWCRefItemsList.Contains(ddlItem))
                                ddlFWCRefItemsList.Add(ddlItem);
                        }

                        ddlFWCRefItemsList.Sort((x, y) => string.Compare(x.Value, y.Value));
                        ddlFWCReference.Items.AddRange(ddlFWCRefItemsList.ToArray());
                        ListItem defaultItem = new ListItem("All");
                        ddlFWCReference.Items.Insert(0, defaultItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("InitializeFWCReference: " + ex.Message, string.Empty);
            }
        }

        /// <summary>
        /// Load possible workflow groups in roles control
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="domain"></param>
        protected void InitializeWFRoles(SPWeb Web, string domain)
        {
            try
            {
                if (!this.Page.IsPostBack)
                {
                    SPList configList = Web.GetListFromWebPartPageUrl(Web.Url + "/Lists/WFStepDefinitions/AllItems.aspx");
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><IsNotNull><FieldRef Name='WFGroup' /></IsNotNull></Where>";
                    query.ViewFields = string.Concat(
                                       "<FieldRef Name='WFGroup' />");
                    query.ViewFieldsOnly = true; // Fetch only the data that we need
                    SPListItemCollection itemCollection = configList.GetItems(query);

                    List<ListItem> ddlRoleItems = new List<ListItem>();
                    List<ListItem> ddlRoleItemsAux = new List<ListItem>();
                    
                    foreach (SPListItem item in configList.Items)
                    {
                        try
                        {
                            if (item["WFGroup"] != null)
                            {
                                SPFieldUserValue groupValue = new SPFieldUserValue(Web, item["WFGroup"].ToString());
                                string groupAD = groupValue.LookupValue.Replace(domain + "\\", string.Empty);

                                if (!string.IsNullOrEmpty(groupAD))
                                {
                                    string groupName = Methods.GetDefinitionGroupName(groupAD, parameters);

                                    ListItem ddlItem = new ListItem(groupName);
                                    ListItem ddlItemAux = new ListItem(groupName.ToLower());

                                    if (!ddlRoleItemsAux.Contains(ddlItemAux))
                                    {
                                        ddlRoleItems.Add(ddlItem);
                                        ddlRoleItemsAux.Add(ddlItemAux);
                                    }
                                }
                            }
                            else
                            {
                                Methods.saveErrorsLog("There is one step without WFGroup", item["Title"].ToString() + " - Step: " + item["StepNumber"].ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            Methods.saveErrorsLog("InitializeWFRoles: Role - " + item["WFGroup"].ToString(), ex.Message);
                            continue;
                        }
                        
                    }

                    ddlRoleItems = ddlRoleItems.OrderBy(o => o.Value).ToList();
                    ddlRole.Items.AddRange(ddlRoleItems.ToArray());

                    //ddlRoleItems.Sort((x, y) => string.Compare(x.Value, y.Value));
                    //ddlRole.Items.AddRange(ddlRoleItems.ToArray());
                    string defaultValue = "All";
                    ddlRole.Items.Insert(0, defaultValue);
                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("InitializeWFRoles: " + ex.Message, ex.StackTrace);
            }
        }


        protected void InitializeSession()
        {
            try
            {

                //WF Subject
                if (Session["SearchTitle"] != null)
                    txtTitle.Text = Session["SearchTitle"].ToString();


                //DateTime (From / To)
                if (Session["SearchFromDate"] != null && !string.IsNullOrEmpty(Session["SearchFromDate"].ToString()))
                    dtFrom.SelectedDate = DateTime.Parse(Session["SearchFromDate"].ToString());
                dtFrom.LocaleId = Convert.ToInt32(SPContext.Current.RegionalSettings.LocaleId);

                if (Session["SearchToDate"] != null && !string.IsNullOrEmpty(Session["SearchToDate"].ToString()))
                    dtTo.SelectedDate = DateTime.Parse(Session["SearchToDate"].ToString());
                dtTo.LocaleId = Convert.ToInt32(SPContext.Current.RegionalSettings.LocaleId);

                //DateTime (Deadline)
                if (Session["SearchDeadlineFromDate"] != null && !string.IsNullOrEmpty(Session["SearchDeadlineFromDate"].ToString()))
                    dtDeadlineFrom.SelectedDate = DateTime.Parse(Session["SearchDeadlineFromDate"].ToString());
                dtDeadlineFrom.LocaleId = Convert.ToInt32(SPContext.Current.RegionalSettings.LocaleId);

                if (Session["SearchDeadlineToDate"] != null && !string.IsNullOrEmpty(Session["SearchDeadlineToDate"].ToString()))
                    dtDeadlineTo.SelectedDate = DateTime.Parse(Session["SearchDeadlineToDate"].ToString());
                dtDeadlineTo.LocaleId = Convert.ToInt32(SPContext.Current.RegionalSettings.LocaleId);

                //WFID
                if (Session["SearchID"] != null)
                    txtID.Text = Session["SearchID"].ToString();
                txtID.Text.Trim();

                //WFType
                if (Session["SearchType"] != null)
                    ddlType.SelectedIndex = int.Parse(Session["SearchType"].ToString());

                //WFStatus
                if (Session["SearchStatus"] != null)
                    ddlStatus.SelectedIndex = int.Parse(Session["SearchStatus"].ToString());
                
                //Actor
                if (Session["SearchActor"] != null)
                    ddlActor.SelectedIndex = int.Parse(Session["SearchActor"].ToString());

                //Role
                if (Session["SearchRole"] != null)
                    ddlRole.SelectedIndex = int.Parse(Session["SearchRole"].ToString());                

                //Staff Name
                if (Session["SearchStaff"] != null)
                    ddlStaff.SelectedIndex = int.Parse(Session["SearchStaff"].ToString());

                //Urgent
                if (Session["SearchUrgent"] != null)
                    ddlUrgent.SelectedIndex = int.Parse(Session["SearchUrgent"].ToString());
               
                //VAT
                if (Session["SearchVAT"] != null)
                    ddlVAT.SelectedIndex = int.Parse(Session["SearchVAT"].ToString());

                //Incident
                if (Session["SearchIncident"] != null)
                    ddlIncident.SelectedIndex = int.Parse(Session["SearchIncident"].ToString());

                //ABAC Commitment
                if (Session["SearchABAC"] != null)
                    txtABAC.Text = Session["SearchABAC"].ToString();
                txtABAC.Text.Trim();

                //Contractor
                if (Session["SearchContractor"] != null)
                    txtContractor.Text = Session["SearchContractor"].ToString();
                txtContractor.Text.Trim();

                //FWC Reference
                if (Session["SearchFWCReference"] != null)
                    ddlFWCReference.Text = Session["SearchFWCReference"].ToString();
           

                //SignedWF By Me
                if (Session["SearchSignedWF"] != null)
                    cbSignedByMe.Text = Session["SearchSignedWF"].ToString();

                //Keyword
                if (Session["SearchKeyWord"] != null)
                    txtKeyWord.Text = Session["SearchKeyWord"].ToString();

                if (Session["SearchResultMessage"] != null)
                {
                    lblResults.Text = Session["SearchResultMessage"].ToString();
                    lblResults.Visible = true;
                }

                if (Session["SearchResultGrid"] != null)
                {
                    gvResults.DataSource = Session["SearchResultGrid"];
                    resultTable = (DataTable)Session["SearchResultGrid"];

                    DrawGridviewSettings(resultTable);

                }
                
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("InitializeSession: " + ex.Message, string.Empty);
            }
        }

     

        #endregion

        #endregion

        #region <QUERIES>

        /// <summary>
        /// Create CAML query according to general filtering controls.
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected string CreateUIQueryModule(SPWeb Web, string nameUserLogged)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                List<string> queryList = new List<string>();
                queryList.Add("<Eq><FieldRef Name='ContentType' /><Value Type='Computed'>Workflow</Value></Eq>");
                queryList.Add("<IsNotNull><FieldRef Name='WFActorsSignedRole' /></IsNotNull>");
             
                //Queries
                CreateQuery_WFSubject(ref queryList); //Title of WF
                CreateQuery_DateTimeFromTo(ref queryList);
                CreateQuery_DateTimeDeadlineFromTo(ref queryList);
                CreateQuery_WFID(ref queryList);
                CreateQuery_WFType(ref queryList);
                CreateQuery_StaffName(ref queryList, Web);
                CreateQuery_WFStatus(ref queryList);
                CreateQuery_Urgent(ref queryList);
                CreateQuery_VAT(ref queryList);
                CreateQuery_ABACCommitment(ref queryList);
                CreateQuery_Contractor(ref queryList);
                CreateQuery_FWCReference(ref queryList);
                CreateQuery_VacancyNo(ref queryList);
                CreateQuery_PersonalFile(ref queryList);
                CreateQuery_IncidentTick(ref queryList);
                CreateQuery_SignedByMe(ref queryList, nameUserLogged);


                //Actor + Role -> WFActorsSignedRole
                if (!ddlRole.SelectedValue.Equals("All"))
                {
                    string adGroupName = Methods.GetADGroupName(ddlRole.SelectedValue, parameters);

                   if (string.IsNullOrEmpty(ddlActor.SelectedValue))
                       CreateQuery_Role(ref queryList, Web, adGroupName);
                    else
                       CreateQuery_ActorRole(ref queryList, Web, adGroupName);
                }
                else if (!string.IsNullOrEmpty(ddlActor.SelectedValue))
                    CreateQuery_Actor(ref queryList, Web);


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
                    sb.Append(CreateWhereClause("And", queryList));
                    sb.Append("</Where>");

                }


            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateUIQueryModule: " + ex.Message, sb.ToString());
            }

            return sb.ToString();
        }

        //WFSubject (Keyword in workflow title)
        protected void CreateQuery_WFSubject(ref List<string> queryList)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtTitle.Text.Trim()))
                    queryList.Add("<Contains><FieldRef Name='WFSubject' /><Value Type='Text'>" + txtTitle.Text.Trim() + "</Value></Contains>");
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_WFSubject: " + ex.Message, null);
            }

        }

        //ABAC Commitment (Text)
        protected void CreateQuery_ABACCommitment(ref List<string> queryList)
        {
            try
            {

                 if (!string.IsNullOrEmpty(txtABAC.Text.Trim()))
                     queryList.Add("<Contains><FieldRef Name='GFABACCommitment' /><Value Type='Text'>" + txtABAC.Text.Trim() + "</Value></Contains>");
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_ABACCommitment: " + ex.Message, null);
            }

        }

        //Contractor (Text)
        protected void CreateQuery_Contractor(ref List<string> queryList)
        {
            try
            {

                if (!string.IsNullOrEmpty(txtContractor.Text.Trim()))
                    queryList.Add("<Contains><FieldRef Name='GFContractor' /><Value Type='Text'>" + txtContractor.Text.Trim() + "</Value></Contains>");
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_Contractor: " + ex.Message, null);
            }

        }


        //Created - From + Modified - To
        protected void CreateQuery_DateTimeFromTo(ref List<string> queryList)
        {
            try
            {
                TextBox firstDateTB = dtFrom.Controls[0] as TextBox;
                TextBox lastDateTB = dtTo.Controls[0] as TextBox;

                //From
                if (!string.IsNullOrEmpty(firstDateTB.Text))
                    queryList.Add(DateTimeQuery(dtFrom.SelectedDate, true));
                //To
                if (!string.IsNullOrEmpty(lastDateTB.Text))
                    queryList.Add(DateTimeQuery(dtTo.SelectedDate, false));
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_DateTimeFromTo: " + ex.Message, null);
            }

        }

        public string DateTimeQuery(DateTime date, bool isFirst)
        {
            string dateQuery = string.Empty;

            try
            {
                if (isFirst)
                    dateQuery = "<Geq><FieldRef Name='Created' /><Value Type='DateTime'>" + date.ToString("yyyy-MM-ddThh:mm:ssZ") + "</Value></Geq>";
                else
                    dateQuery = "<Leq><FieldRef Name='Modified' /><Value Type='DateTime'>" + date.ToString("yyyy-MM-ddThh:mm:ssZ") + "</Value></Leq>";
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("DateTimeQuery: " + ex.Message, dateQuery);
            }

            return dateQuery;
        }

        //Deadline (From + To)
        protected void CreateQuery_DateTimeDeadlineFromTo(ref List<string> queryList)
        {
            try
            {
                TextBox firstDateTB = dtDeadlineFrom.Controls[0] as TextBox;
                TextBox lastDateTB = dtDeadlineTo.Controls[0] as TextBox;

                //From
                if (!string.IsNullOrEmpty(firstDateTB.Text))
                    queryList.Add(DateTimeDeadlineQuery(dtDeadlineFrom.SelectedDate, true));
                //To
                if (!string.IsNullOrEmpty(lastDateTB.Text))
                    queryList.Add(DateTimeDeadlineQuery(dtDeadlineTo.SelectedDate, false));
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_DateTimeDeadlineFromTo: " + ex.Message, null);
            }

        }

        public string DateTimeDeadlineQuery(DateTime date, bool isFirst)
        {
            string dateQuery = string.Empty;

            try
            {
                if (isFirst)
                    dateQuery = "<Geq><FieldRef Name='WFDeadline' /><Value Type='DateTime'>" + date.ToString("yyyy-MM-ddThh:mm:ssZ") + "</Value></Geq>";
                else
                    dateQuery = "<Leq><FieldRef Name='WFDeadline' /><Value Type='DateTime'>" + date.ToString("yyyy-MM-ddThh:mm:ssZ") + "</Value></Leq>";
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("DateTimeDeadlineQuery: " + ex.Message, dateQuery);
            }

            return dateQuery;
        }

        //WFID
        protected void CreateQuery_WFID(ref List<string> queryList)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtID.Text.Trim()))
                    queryList.Add("<Eq><FieldRef Name='WFID' /><Value Type='Text'>" + txtID.Text.Trim() + "</Value></Eq>");
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_WFID: " + ex.Message, null);
            }

        }

        //WFType
        protected void CreateQuery_WFType(ref List<string> queryList)
        {
            try
            {

                if (!ddlType.SelectedValue.Equals("All") && !string.IsNullOrEmpty(ddlType.SelectedValue))
                {
                    string ddlTypeValue = ddlType.SelectedValue.Trim();

                    if (ddlTypeValue.Contains("/"))
                        ddlTypeValue = ddlTypeValue.Split('/')[0];

                    queryList.Add("<Contains><FieldRef Name='WFType' /><Value Type='Text'>" + "<![CDATA[" + ddlTypeValue + "]]>" + "</Value></Contains>");
                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_WFType: " + ex.Message, null);
            }

        }

        //FWC Reference (Choice)
        protected void CreateQuery_FWCReference(ref List<string> queryList)
        {
            try
            {
                if (!ddlFWCReference.SelectedValue.Equals("All") && !string.IsNullOrEmpty(ddlFWCReference.SelectedValue))
                    queryList.Add("<Eq><FieldRef Name='GFFWCRef' /><Value Type='Text'>" + ddlFWCReference.SelectedValue.Trim() + "</Value></Eq>");
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_FWCReference: " + ex.Message, null);
            }

        }

        //Vacancy No
        protected void CreateQuery_VacancyNo(ref List<string> queryList)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtVacancyNo.Text.Trim()))
                    queryList.Add("<Eq><FieldRef Name='VacancyNo' /><Value Type='Text'>" + txtVacancyNo.Text.Trim() + "</Value></Eq>");
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_VacancyNo: " + ex.Message, null);
            }

        }

        //Personal File
        protected void CreateQuery_PersonalFile(ref List<string> queryList)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtPersonalFile.Text.Trim()))
                    queryList.Add("<Contains><FieldRef Name='PersonalFile' /><Value Type='Text'>" + txtPersonalFile.Text.Trim() + "</Value></Contains>");
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_PersonalFile: " + ex.Message, null);
            }

        }



        //WFStatus
        protected void CreateQuery_WFStatus(ref List<string> queryList)
        {
            try
            {
                if (!ddlStatus.SelectedValue.Equals("All") && !string.IsNullOrEmpty(ddlStatus.SelectedValue))
                    queryList.Add("<Eq><FieldRef Name='WFStatus' /><Value Type='Text'>" + ddlStatus.SelectedValue.Trim() + "</Value></Eq>");
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_WFStatus: " + ex.Message, null);
            }

        }

        //Staff Name
        protected void CreateQuery_StaffName(ref List<string> queryList, SPWeb Web)
        {
            try
            {
                if (!string.IsNullOrEmpty(ddlStaff.SelectedValue))
                {
                    string selectedValue = ddlStaff.SelectedValue;
                    SPUser selectedActor = Web.EnsureUser(selectedValue);
                    
                    queryList.Add("<Eq><FieldRef Name='GFStaffName' Nullable='TRUE' LookupId='True' /><Value Type='Integer'>" + selectedActor.ID + "</Value></Eq>");
                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_StaffName: " + ex.Message, null);
            }

        }

        //Urgent
        protected void CreateQuery_Urgent(ref List<string> queryList)
        {
            try
            {
                if (!ddlUrgent.SelectedValue.Equals("All") && !string.IsNullOrEmpty(ddlUrgent.SelectedValue))
                {
                    if (ddlUrgent.SelectedValue.ToUpper().Trim().Equals("YES"))
                        queryList.Add("<Eq><FieldRef Name='Urgent' /><Value Type='Boolean'>1</Value></Eq>");
                    else if (ddlUrgent.SelectedValue.ToUpper().Trim().Equals("NO"))
                        queryList.Add("<Eq><FieldRef Name='Urgent' /><Value Type='Boolean'>0</Value></Eq>");
                }

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_Urgent: " + ex.Message, null);
            }
        }

        //VAT
        protected void CreateQuery_VAT(ref List<string> queryList)
        {
            try
            {

                if (!ddlVAT.SelectedValue.Equals("All") && !string.IsNullOrEmpty(ddlVAT.SelectedValue))
                {
                    if (ddlVAT.SelectedValue.ToUpper().Trim().Equals("YES"))
                        queryList.Add("<Eq><FieldRef Name='GFVAT' /><Value Type='Boolean'>1</Value></Eq>");
                    if (ddlVAT.SelectedValue.ToUpper().Trim().Equals("NO"))
                        queryList.Add("<Eq><FieldRef Name='GFVAT' /><Value Type='Boolean'>0</Value></Eq>");
                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_VAT: " + ex.Message, null);
            }

        }

        //Incident Tick
        protected void CreateQuery_IncidentTick(ref List<string> queryList)
        {
            try
            {

                if (!ddlIncident.SelectedValue.Equals("All") && !string.IsNullOrEmpty(ddlIncident.SelectedValue))
                {
                    if (ddlIncident.SelectedValue.ToUpper().Trim().Equals("YES"))
                        queryList.Add("<Eq><FieldRef Name='GFIncidentTick' /><Value Type='Boolean'>1</Value></Eq>");
                    if (ddlIncident.SelectedValue.ToUpper().Trim().Equals("NO"))
                        queryList.Add("<Eq><FieldRef Name='GFIncidentTick' /><Value Type='Boolean'>0</Value></Eq>");
                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_IncidentTick: " + ex.Message, null);
            }

        }

        //Signed By Me (excluding the Drafts)
        protected void CreateQuery_SignedByMe(ref List<string> queryList, string nameUserLogged)
        {
            try
            {
                if (cbSignedByMe.Checked)
                {
                    queryList.Add("<Contains><FieldRef Name='WFActorsSignedRole' /><Value Type='Note'>" + nameUserLogged + "</Value></Contains>");
                    queryList.Add("<Neq><FieldRef Name='WFStatus' /><Value Type='Choice'>Draft</Value></Neq>");
                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_SignedByMe: " + ex.Message, "Error user: " + nameUserLogged);
            }

        }

        //Role
        protected void CreateQuery_Role(ref List<string> queryList, SPWeb Web, string adGroupName)
        {
            try
            {

                queryList.Add("<Contains><FieldRef Name='WFActorsSignedRole' /><Value Type='Note'>" + adGroupName.Trim() + "</Value></Contains>");
                    
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_Role: " + ex.Message, null);
            }

        }

        //Actor
        protected void CreateQuery_Actor(ref List<string> queryList, SPWeb Web)
        {
            try
            {
                    string loginName = Methods.GetUserAccountFromActorSelected(Web, ddlActor.SelectedValue);
                    queryList.Add("<Contains><FieldRef Name='WFActorsSignedRole' /><Value Type='Note'>" + loginName.Trim() + "</Value></Contains>");
                
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_Actor: " + ex.Message, null);
            }

        }

        //Role + Actor
        protected void CreateQuery_ActorRole(ref List<string> queryList, SPWeb Web, string adGroupName)
        {
            try
            {

                //1;#defaultfia;#sp-paperless-local-staff
                string loginName = Methods.GetUserAccountFromActorSelected(Web, ddlActor.SelectedValue);
                string concatInf = loginName + ";#" + adGroupName;

                queryList.Add("<Contains><FieldRef Name='WFActorsSignedRole' /><Value Type='Note'>" + concatInf + "</Value></Contains>");

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQuery_ActorRole: " + ex.Message, null);
            }

        }

        //Restricted (by Keywords)
        protected void CreateQueryKeyword_Restricted(ref List<string> queryList)
        {
            try
            {
                queryList.Add("<Contains><FieldRef Name='ConfidentialWorkflow' /><Value Type='Choice'>" + txtKeyWord.Text.Trim() + "</Value></Contains>");  
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQueryKeyword_Restricted: " + ex.Message, null);
            }

        }

        //Amount (by Keywords)
        protected void CreateQueryKeyword_Amount(ref List<string> queryList)
        {
            try
            {
                queryList.Add("<Contains><FieldRef Name='Amount' /><Value Type='Text'>" + txtKeyWord.Text.Trim() + "</Value></Contains>");
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQueryKeyword_Amount: " + ex.Message, null);
            }

        }

        //Link to WF (by Keywords)
        protected void CreateQueryKeyword_LinkToWF(ref List<string> queryList)
        {
            try
            {
                queryList.Add("<Contains><FieldRef Name='LinkToWorkflow' /><Value Type='Note'>" + txtKeyWord.Text.Trim() + "</Value></Contains>");
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQueryKeyword_Amount: " + ex.Message, null);
            }

        }

        //WF Subject (by Keywords)
        protected void CreateQueryKeyword_WFSubject(ref List<string> queryList)
        {
            try
            {
                queryList.Add("<Contains><FieldRef Name='WFSubject' /><Value Type='Text'>" + txtKeyWord.Text.Trim() + "</Value></Contains>");
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateQueryKeyword_WFSubject: " + ex.Message, null);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="OperatorClause"></param>
        /// <param name="queryList"></param>
        /// <returns></returns>
        public static string CreateWhereClause(string OperatorClause, List<string> queryList)
        {
            string query = string.Empty;

            try
            {

                string MiddleQuery = string.Empty;
                string firstquery = string.Empty;
                string lastquery = string.Empty;
                string firstOperator = string.Empty;
                string lastOperator = string.Empty;
                int cont = 0;

                if (OperatorClause == "Or")
                {
                    firstOperator = "<Or>";
                    lastOperator = "</Or>";
                }
                else
                {
                    firstOperator = "<And>";
                    lastOperator = "</And>";
                }

                if (queryList.Count > 1)
                    firstquery = firstOperator;

                if (queryList.Count > 1)
                    lastquery = lastOperator;

                foreach (string field in queryList)
                {
                    cont++;
                    if (queryList.Count > 2 && cont > 1 && cont < queryList.Count)
                    {
                        MiddleQuery = MiddleQuery + firstOperator;
                        lastquery = lastOperator + lastquery;
                    }

                    MiddleQuery = MiddleQuery + field;
                }

                query = firstquery + MiddleQuery + lastquery;

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateWhereClausule: " + ex.Message, query);
            }

            return query;
        }


        /// <summary>
        /// Create CAML query for keyword search in log lists.
        /// </summary>
        /// <param name="queryList"></param>
        /// <param name="logicConditionStart"></param>
        /// <param name="logicConditionEnd"></param>
        /// <returns></returns>
        protected string GenerateQuery(List<string> queryList, string logicConditionStart, string logicConditionEnd)
        {
            StringBuilder sb = new StringBuilder(string.Empty);

            try
            {

                if (queryList.Count.Equals(1))
                {
                    sb.Append("<Where><And>");
                    sb.Append(queryList[0]);
                    sb.Append("<Eq><FieldRef Name='FSObjType' /><Value Type='Integer'>1</Value></Eq></And></Where>");
                }
                else if (queryList.Count > 0)
                {
                    int count1 = 1;
                    int count2 = 1;

                    while (count1 <= queryList.Count)
                    {
                        int count3 = count2;
                        if (count3 % 2 != 0 && count3 > 1)
                            sb.Insert(0, logicConditionStart);

                        if (queryList.Count >= (count1 + 1))
                        {
                            sb.Append(logicConditionStart);
                            sb.Append(queryList[count1 - 1]);
                            sb.Append(queryList[count1]);
                            sb.Append(logicConditionEnd);
                            count1 += 2;
                            count2 += 2;
                        }
                        else
                        {
                            sb.Append(queryList[count1 - 1]);
                            count1++;
                            count2++;
                        }

                        if (count3 % 2 != 0 && count3 > 1)
                            sb.Append(logicConditionEnd);
                    }

                    sb.Insert(0, "<Where><And>");
                    sb.Append("<Eq><FieldRef Name='FSObjType' /><Value Type='Integer'>1</Value></Eq></And></Where>");
                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("GenerateQuery: " + ex.Message, sb.ToString());
            }
            return sb.ToString();
        }

        protected string CreateGFsQueryModule(SPWeb web, Dictionary<string, SPField> GFieldsDictionary)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                List<string> queryList = new List<string>();
               

                //-----------------------------------------------------------
                //Comun GFs
                //-----------------------------------------------------------
                    CreateQueryKeyword_Restricted(ref queryList); //Restricted
                
                    //Urgent -> Use the UI
                    CreateQueryKeyword_WFSubject(ref queryList); //WF Subject
                    CreateQueryKeyword_Amount(ref queryList); //Amount
                    CreateQueryKeyword_LinkToWF(ref queryList); //Link To WF


                //-----------------------------------------------------------
                //Especific GFs
                //-----------------------------------------------------------
                 CreateDinamicQueryForEspecificGFs(ref queryList, GFieldsDictionary, txtKeyWord.Text.Trim());

                //Concat the query
                    sb.Append("<Where>");
                    sb.Append(CreateWhereClause("Or", queryList));
                    sb.Append("</Where>");

                    

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateGFsQueryModule: " + ex.Message, sb.ToString());
            }

            return sb.ToString();
        }

        protected void CreateDinamicQueryForEspecificGFs(ref List<string> queryList, Dictionary<string, SPField> GFieldsDictionary, string valueToFind)
        {

            try
            {


                foreach (KeyValuePair<string, SPField> entry in GFieldsDictionary)
                {
                    string internalName = entry.Key;
                    SPField field = entry.Value;
                    string fieldType = field.Type.ToString();


                    switch (fieldType)
                    {
                        case "Text":
                            queryList.Add("<Contains><FieldRef Name='" + internalName + "' /><Value Type='Text'>" + valueToFind + "</Value></Contains>");
                            break;

                        case "Note":
                            queryList.Add("<Contains><FieldRef Name='" + internalName + "' /><Value Type='Note'>" + valueToFind + "</Value></Contains>");
                            break;

                        case "Choice":
                            queryList.Add("<Contains><FieldRef Name='" + internalName + "' /><Value Type='Choice'>" + valueToFind + "</Value></Contains>");
                            break;


                    }
                }

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateDinamicQueryForEspecificGFs(): " + ex.Message, ex.StackTrace);
            }

        }
  
        #endregion

        #region <SEARCHS LOGICAL>

        protected bool AreAllFieldsEmpty()
        {
            bool areEmpty = false;

            try
            {
                if ((!string.IsNullOrEmpty(txtTitle.Text)) || (!dtFrom.IsDateEmpty) || (!dtDeadlineFrom.IsDateEmpty) || (!string.IsNullOrEmpty(txtID.Text))
                    || (!ddlType.SelectedValue.Equals("All")) || (!ddlStatus.SelectedValue.Equals("All")) || (!string.IsNullOrEmpty(ddlActor.SelectedValue)) || (!ddlRole.SelectedValue.Equals("All")) || (!string.IsNullOrEmpty(ddlStaff.SelectedValue)) || (!ddlUrgent.SelectedValue.Equals("All"))
                    || (!ddlVAT.SelectedValue.Equals("All")) || (!string.IsNullOrEmpty(txtABAC.Text)) || (!ddlIncident.SelectedValue.Equals("All")) || (cbSignedByMe.Checked) || (!string.IsNullOrEmpty(txtContractor.Text)) || (!ddlFWCReference.SelectedValue.Equals("All")) || (!string.IsNullOrEmpty(txtVacancyNo.Text)) || (!string.IsNullOrEmpty(txtPersonalFile.Text)))
                    areEmpty = false;
                else
                    areEmpty = true;

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog(string.Empty, "AreAllFieldsEmpty(): " + ex.Message);
            }

            return areEmpty;
        }

        //WF Libraries -> Filters from UI
        protected void UIValuesSearch(string queryToExecute, SPWeb Web, ref DataTable resultTableGeneral, string interfaceURL, bool allWFs)
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
                siteDataQuery.ViewFields += "<FieldRef Name='Created' Type='DateTime' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='Urgent' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='WFDeadline' Nullable='TRUE'/>";
                //siteDataQuery.ViewFields += "<FieldRef Name='GFABACCommitment' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='Modified' Type='DateTime'  Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='WFActorsSignedRole' Nullable='TRUE'/>";
                //siteDataQuery.ViewFields += "<FieldRef Name='GFStaffName' Nullable='TRUE'/>";
                //siteDataQuery.ViewFields += "<FieldRef Name='GFVAT' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='ConfidentialWorkflow' Nullable='TRUE' />";
                siteDataQuery.ViewFields += "<FieldRef Name='GFContractor' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='GFFWCRef' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='VacancyNo' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='PersonalFile' Nullable='TRUE'/>";


                siteDataQuery.Webs = "<Webs Scope='SiteCollection' />";

                if (allWFs.Equals(false))
                 siteDataQuery.Query = queryToExecute + "<OrderBy><FieldRef Name='WFID' Ascending='FALSE' /></OrderBy>";
                else
                    siteDataQuery.Query = "<Where><And>"
                                        + "<IsNotNull><FieldRef Name='WFActorsSignedRole' /></IsNotNull>"
                                        + "<Eq><FieldRef Name='ContentType' /><Value Type='Computed'>Workflow</Value></Eq>"
                                        + "</And></Where>"
                                        + "<OrderBy><FieldRef Name='WFID' Ascending='FALSE' /></OrderBy>";

                siteDataQuery.QueryThrottleMode = SPQueryThrottleOption.Override;

               DataTable dtAux = Web.GetSiteData(siteDataQuery);
               DataView dvAux = dtAux.AsDataView();

           
               foreach (DataRowView drow in dvAux)
               {
                   string wfType = drow[7].ToString().ToUpper();
                   string wfid = Double.Parse(drow[3].ToString()).ToString();

                   if (wftypeCodes.ContainsKey(wfType))
                   {
                       string wftypeorder = wftypeCodes[wfType];

                       AddNewRow_DataRowView_Common(ref resultTableGeneral, Web,  interfaceURL, wftypeorder, drow , wfid);
                   }
                   else
                       Methods.saveErrorsLog("Error adding WFID '" + wfid + "'.", "The WFType '" + wfType + "' does not exist in the RS Workflow Configuration List.");

                   
               }

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("UIValuesSearch(): " + ex.Message, ex.StackTrace);
            }
        }

        //WF Libraries -> GFs
        protected void KeyWordOnGFsSearch(string queryToExecute, SPWeb Web, ref DataTable resultTableGeneral, string interfaceURL, Dictionary<string, SPField> GFieldsDictionary)
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
                //siteDataQuery.ViewFields += "<FieldRef Name='GFABACCommitment' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='Modified' Type='DateTime'  Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='WFActorsSignedRole' Nullable='TRUE'  />";
                //siteDataQuery.ViewFields += "<FieldRef Name='GFStaffName' Nullable='TRUE'/>";
                //siteDataQuery.ViewFields += "<FieldRef Name='GFVAT' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='ConfidentialWorkflow' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='GFContractor' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='GFFWCRef' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='VacancyNo' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='PersonalFile' Nullable='TRUE'/>";

                foreach (KeyValuePair<string, SPField> entry in GFieldsDictionary)
                {
                    siteDataQuery.ViewFields += "<FieldRef Name='" + entry.Key + "' Nullable='TRUE'/>";
                }
      


                siteDataQuery.Webs = "<Webs Scope='SiteCollection' />";
                siteDataQuery.Query = queryToExecute + "<OrderBy><FieldRef Name='WFID' Ascending='FALSE' /></OrderBy>";

                siteDataQuery.QueryThrottleMode = SPQueryThrottleOption.Override;

                DataTable dtAux = Web.GetSiteData(siteDataQuery);
                DataView dvAux = dtAux.AsDataView();

                Methods.saveErrorsLog("SearchGFs - Results: " + dvAux.Count.ToString(), string.Empty);
                //Methods.saveErrorsLog("SearchGFs", queryToExecute);

                foreach (DataRowView drow in dvAux)
                {
                    string wfType = drow[7].ToString().ToUpper();
                    string wfid = Double.Parse(drow[3].ToString()).ToString();

                    if (wftypeCodes.ContainsKey(wfType))
                    {
                        string wftypeorder = wftypeCodes[wfType];

                        AddNewRow_DataRowView_Common(ref resultTableGeneral, Web, interfaceURL, wftypeorder, drow, wfid);
                    }
                    else
                        Methods.saveErrorsLog("Error adding WFID '" + wfid + "'.", "The WFType '" + wfType + "' does not exist in the RS Workflow Configuration List.");
                }
                
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("KeyWordOnGFsSearch: " + ex.Message, ex.StackTrace);
            }
        }
 
        //Logs Lits -> Comments
        protected void KeyWordOnCommentsSearch(SPWeb Web, ref DataTable resultTableGeneral, string interfaceURL)
        {

            try
            {

                DataTable commentTableAux = SearchComments();
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
                            string wfType = item["WFType"].ToString().ToUpper();

                            if (wftypeCodes.ContainsKey(wfType))
                            {
                                string wftypeorder = wftypeCodes[wfType];
                                AddNewRow_ListItem_Keyword(ref resultTableGeneral, Web, interfaceURL, wftypeorder, item, wfid);
                                
                            }
                            else
                                Methods.saveErrorsLog("Error adding WFID '" + wfid + "'.", "The WFType '" + wfType + "' does not exist in the RS Workflow Configuration List.");
                        }
                        else
                            Methods.saveErrorsLog("Error adding WFID '" + wfid + "'. It does not exist in the RS Workflow History List.", string.Empty);
                    }

                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("KeyWordOnCommentsSearch(): " + ex.Message, "Text -> " + txtKeyWord.Text.Trim() + " - " + ex.StackTrace);
            }
        }

        protected DataTable SearchComments()
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
                        siteDataQuery.ViewFields += "<FieldRef Name='WorkflowComment' Nullable='TRUE'/>";
                        siteDataQuery.ViewFields += "<FieldRef Name='ActionTaken' Nullable='TRUE'/>";
                        siteDataQuery.ViewFields += "<FieldRef Name='ConfidentialWorkflow' Nullable='TRUE' />";

                        siteDataQuery.Webs = "<Webs Scope='SiteCollection' />";

                        siteDataQuery.Query = "<Where><And>"
                                              + "<Contains><FieldRef Name='ActionTaken' /><Value Type='Choice'>Commented</Value></Contains>"
                                              + "<Contains><FieldRef Name='WorkflowComment' /><Value Type='Note'>" + txtKeyWord.Text.Trim() + "</Value></Contains></And></Where>";

                        commentTableAux = Web.GetSiteData(siteDataQuery);

                        Methods.saveErrorsLog("SearchComments - Results: " + commentTableAux.Rows.Count.ToString() , string.Empty);

                        Web.Close();
                        Web.Dispose();
                    }
                });
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("SearchComments(): " + ex.Message, "Text -> " + txtKeyWord.Text.Trim() + " - " + ex.StackTrace);
            }

            return commentTableAux;
        }

        //WF Libraries -> Document Title
        protected void KeyWordOnDocumentSearch(SPWeb Web, ref DataTable resultTableGeneral, string interfaceURL)
        {
            try
            {
                SPSiteDataQuery siteDataQuery = new SPSiteDataQuery();
                siteDataQuery.Lists = "<Lists ServerTemplate='906' />";
                siteDataQuery.ViewFields = "<FieldRef Name='WFID' Type='Number' Nullable='TRUE'/>";
                siteDataQuery.ViewFields += "<FieldRef Name='FileLeafRef'/><FieldRef Name='ContentType'/>";
                siteDataQuery.Webs = "<Webs Scope='RecursiveAll'/>"; //Recursive: Current site and any subsite (Show all files and all subfolders of all folders.)
                siteDataQuery.Query = "<Where><And><Contains><FieldRef Name='FileLeafRef'/><Value Type='Text'>" + txtKeyWord.Text.Trim() + "</Value></Contains>"
                                     + "<Eq><FieldRef Name='ContentType' /><Value Type='Text'>Workflow Document</Value></Eq></And></Where>"
                                     + "<OrderBy><FieldRef Name='WFID' Ascending='FALSE' /></OrderBy>";
                siteDataQuery.QueryThrottleMode = SPQueryThrottleOption.Override;
                DataTable documentTableAux = Web.GetSiteData(siteDataQuery);
                DataView dvDocumentAux = documentTableAux.AsDataView();

                Methods.saveErrorsLog("KeyWordOnDocumentSearch - Results: " + documentTableAux.Rows.Count.ToString(), string.Empty);

                    foreach (DataRowView drow in dvDocumentAux)
                    {
                        string wfid = Methods.FormatWFID(drow[3].ToString());
                        bool existsRow = CheckIfExistsRow(wfid, resultTableGeneral );

                        if (existsRow.Equals(false))
                        {
                            SPListItem item = Methods.GetWFInformationByWFID(wfid, Web);

                            if (item != null)
                            {
                                string wfType = item["WFType"].ToString().ToUpper();

                                if (wftypeCodes.ContainsKey(wfType))
                                {
                                    string wftypeorder = wftypeCodes[wfType];
                                    AddNewRow_ListItem_Keyword(ref resultTableGeneral, Web, interfaceURL, wftypeorder, item, wfid);
                                }
                                else
                                    Methods.saveErrorsLog("Error adding WFID '" + wfid + "'.", "The WFType '" + wfType + "' does not exist in the RS Workflow Configuration List.");
                            }
                            else
                                Methods.saveErrorsLog("Error adding WFID '" + wfid + "'. It does not exist in the RS Workflow History List.", string.Empty);
                        }

                    }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("KeyWordOnDocumentSearch: " + ex.Message, ex.StackTrace);
            }
        }

        #endregion

        #region <RESULTS (SPGRIDVIEW)>


        /// <summary>
        /// Search button actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                string userName = string.Empty;
                string loginName = SPContext.Current.Web.CurrentUser.LoginName;

                Methods.GetUserData(ref loginName, ref userName);

                GetResults(loginName);

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("btnSearch_Click: " + ex.Message, ex.StackTrace);
            }
        }

        public void GetResults(string nameUserLogged)
        {

            try
            {
                using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                {
                    SPWeb Web = Site.OpenWeb();
                    //Create DataTable
                    CreateResultTable();
                    string interfaceURL = parameters["Interface Page"];
                    bool allFieldsEmpty = AreAllFieldsEmpty();


                    // Display all WFs order by WFID
                    if ((allFieldsEmpty.Equals(true)) && (string.IsNullOrEmpty(txtKeyWord.Text.Trim())))
                    {
                        UIValuesSearch(null, Web, ref resultTable, interfaceURL, true);
                        DrawResults(resultTable);

                        //Methods.saveErrorsLog("Search by All WFs", null);
                    }
                    else
                    {
                        if (allFieldsEmpty.Equals(false))
                        {
                            //Get WFs (getting information from all DLs)
                            string queryCommonToExecute = CreateUIQueryModule(Web, nameUserLogged);
                            UIValuesSearch(queryCommonToExecute, Web, ref resultTable, interfaceURL, false);

                            //Methods.saveErrorsLog("Search by UI Fields", null);
                        }

                        //Search By Keyword
                        if (!string.IsNullOrEmpty(txtKeyWord.Text.Trim()))
                        {
                            GetResultTableKeywords(Web, ref resultTable, interfaceURL);
                            //Methods.saveErrorsLog("Search by Keyword", null);
                        }
 
                        DrawResults(resultTable);
                            

                    }
                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("GetResults() - " + ex.Message, null);
            }

        }

        /// <summary>
        /// Process and filter general search results
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="resultTableCommon"></param>
        /// <param name="interfaceURL"></param>
        protected void GetResultTableKeywords(SPWeb Web, ref DataTable resultTableCommon, string interfaceURL)
        {

            try
            {
                //All General Fields (WF Library)
                Dictionary<string, SPField> GFieldsDictionary = Methods.GetGFsDictionary();
                string queryCommonToExecute = CreateGFsQueryModule(Web, GFieldsDictionary);
                KeyWordOnGFsSearch(queryCommonToExecute, Web, ref resultTableCommon, interfaceURL, GFieldsDictionary);

                //Methods.saveErrorsLog("GetResultTableKeywords: queryCommonToExecute GFs", queryCommonToExecute);

                //Comments (Logs List)
                KeyWordOnCommentsSearch(Web, ref resultTableCommon, interfaceURL);
                //File Name (WF Library)
                KeyWordOnDocumentSearch(Web, ref resultTableCommon, interfaceURL); 

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("GetResultTableKeywords: " + ex.Message, null);
            }

        }

        protected void DrawResults(DataTable resultTableGeneral)
        {
            try
            {
                if (resultTableGeneral != null && resultTableGeneral.Rows.Count > 0)
                    DrawGridviewSettings(resultTableGeneral);
                else
                {
                    gvResults.Visible = false;
                    lblResults.Visible = true;
                    lblResults.Text = "No results found matching your query.";
                }

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("DrawResults(): " + ex.Message, ex.StackTrace);
            }
        }

        protected void DrawGridviewSettings(DataTable resultFilteredTable)
        {
            try
            {

                gvResults.AllowPaging = true;
                gvResults.AllowSorting = true;
                gvResults.PageSize = 50;
                gvResults.PageIndex = 0;
                gvResults.PagerSettings.Visible = true;
                gvResults.PagerSettings.Mode = PagerButtons.NumericFirstLast;
                gvResults.PagerStyle.Width = gvResults.Width;
                gvResults.PagerStyle.HorizontalAlign = HorizontalAlign.Left;
                gvResults.PagerStyle.SetDirty();
                gvResults.EnableSortingAndPagingCallbacks = false;
                gvResults.PagerStyle.HorizontalAlign = HorizontalAlign.Center;
                gvResults.PagerSettings.Position = PagerPosition.TopAndBottom;
                gvResults.PagerSettings.NextPageText = "Next page";
                gvResults.PagerSettings.PreviousPageText = "Previous page";
                gvResults.HorizontalAlign = HorizontalAlign.Center;

          

                ViewState["SearchResultData"] = resultFilteredTable;

                if (ViewState["SearchSortingField"] == null && ViewState["SearchSortingDirection"] == null)
                {
                    ViewState["SearchSortingField"] = "ID";
                    ViewState["SearchSortingDirection"] = "DESC";
                }

                resultFilteredTable.DefaultView.Sort = ViewState["SearchSortingField"] + " " + ViewState["SearchSortingDirection"];

                gvResults.Visible = true;
                gvResults.DataSource = resultFilteredTable;
                gvResults.DataBind();

                gvResults.Width = Unit.Pixel(1024);
                gvResults.HeaderStyle.CssClass = "header_background";
                gvResults.AlternatingRowStyle.CssClass = "result_grid_even";

                gvResults.HeaderRow.Cells[0].Text = "ID";
                gvResults.HeaderRow.Cells[0].Width = Unit.Percentage(10);
                gvResults.HeaderRow.Cells[0].HorizontalAlign = HorizontalAlign.Center;
                gvResults.HeaderRow.Cells[0].VerticalAlign = VerticalAlign.Middle;

                gvResults.HeaderRow.Cells[1].Text = "Workflow ID";
                gvResults.HeaderRow.Cells[1].Width = Unit.Percentage(10);
                gvResults.HeaderRow.Cells[1].HorizontalAlign = HorizontalAlign.Center;
                gvResults.HeaderRow.Cells[1].VerticalAlign = VerticalAlign.Middle;

                gvResults.HeaderRow.Cells[2].Text = "Workflow Subject";
                gvResults.HeaderRow.Cells[2].Width = Unit.Percentage(20);
                gvResults.HeaderRow.Cells[2].HorizontalAlign = HorizontalAlign.Center;
                gvResults.HeaderRow.Cells[2].VerticalAlign = VerticalAlign.Middle;

                gvResults.HeaderRow.Cells[3].Text = "Amount";
                gvResults.HeaderRow.Cells[3].Width = Unit.Percentage(10);
                gvResults.HeaderRow.Cells[3].HorizontalAlign = HorizontalAlign.Center;
                gvResults.HeaderRow.Cells[3].VerticalAlign = VerticalAlign.Middle;

                gvResults.HeaderRow.Cells[4].Text = "Workflow Status";
                gvResults.HeaderRow.Cells[4].Width = Unit.Percentage(10);
                gvResults.HeaderRow.Cells[4].HorizontalAlign = HorizontalAlign.Center;
                gvResults.HeaderRow.Cells[4].VerticalAlign = VerticalAlign.Middle;

                gvResults.HeaderRow.Cells[5].Text = "Workflow Type";
                gvResults.HeaderRow.Cells[5].Width = Unit.Percentage(20);
                gvResults.HeaderRow.Cells[5].HorizontalAlign = HorizontalAlign.Center;
                gvResults.HeaderRow.Cells[5].VerticalAlign = VerticalAlign.Middle;

                gvResults.HeaderRow.Cells[6].Text = "Created";
                gvResults.HeaderRow.Cells[6].Width = Unit.Percentage(12.5);
                gvResults.HeaderRow.Cells[6].HorizontalAlign = HorizontalAlign.Center;
                gvResults.HeaderRow.Cells[6].VerticalAlign = VerticalAlign.Middle;

                gvResults.HeaderRow.Cells[7].Text = "Urgent";
                gvResults.HeaderRow.Cells[7].Width = Unit.Percentage(5);
                gvResults.HeaderRow.Cells[7].HorizontalAlign = HorizontalAlign.Center;
                gvResults.HeaderRow.Cells[7].VerticalAlign = VerticalAlign.Middle;

                gvResults.HeaderRow.Cells[8].Text = "Deadline";
                gvResults.HeaderRow.Cells[8].Width = Unit.Percentage(12.5);
                gvResults.HeaderRow.Cells[8].HorizontalAlign = HorizontalAlign.Center;
                gvResults.HeaderRow.Cells[8].VerticalAlign = VerticalAlign.Middle;

                gvResults.HeaderRow.Cells[9].Text = "Contractor";
                gvResults.HeaderRow.Cells[9].Width = Unit.Percentage(12.5);
                gvResults.HeaderRow.Cells[9].HorizontalAlign = HorizontalAlign.Center;
                gvResults.HeaderRow.Cells[9].VerticalAlign = VerticalAlign.Middle;

                gvResults.HeaderRow.Cells[10].Text = "FWC Reference";
                gvResults.HeaderRow.Cells[10].Width = Unit.Percentage(20);
                gvResults.HeaderRow.Cells[10].HorizontalAlign = HorizontalAlign.Center;
                gvResults.HeaderRow.Cells[10].VerticalAlign = VerticalAlign.Middle;

                gvResults.HeaderRow.Cells[10].Text = "Vacancy Number";
                gvResults.HeaderRow.Cells[10].Width = Unit.Percentage(5);
                gvResults.HeaderRow.Cells[10].HorizontalAlign = HorizontalAlign.Center;
                gvResults.HeaderRow.Cells[10].VerticalAlign = VerticalAlign.Middle;

                gvResults.HeaderRow.Cells[10].Text = "Personal File";
                gvResults.HeaderRow.Cells[10].Width = Unit.Percentage(20);
                gvResults.HeaderRow.Cells[10].HorizontalAlign = HorizontalAlign.Center;
                gvResults.HeaderRow.Cells[10].VerticalAlign = VerticalAlign.Middle;

                gvResults.DataBind();

                lblResults.Visible = true;

                if (resultFilteredTable.Rows.Count.Equals(1))
                    lblResults.Text = resultFilteredTable.Rows.Count.ToString() + " workflow found matching your query.";
                else
                    lblResults.Text = resultFilteredTable.Rows.Count.ToString() + " workflows found matching your query.";

                SaveControls();

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("DrawGridviewSettings(): " + ex.Message, ex.StackTrace);
            }

        }


        //---------------------------------------------------------------------------
        //DATATABLE
        //---------------------------------------------------------------------------
        /// <summary>
        /// Create result grid columns and datatable structure
        /// </summary>
        protected void CreateResultTable()
        {
            try
            {
                resultTable = new DataTable();
                resultTable.Columns.Add("ID", typeof(int));

                // Workflow ID
                DataColumn colWFID = new DataColumn();
                colWFID.ColumnName = "Workflow ID";
                colWFID.Caption = "Workflow ID";
                resultTable.Columns.Add(colWFID);

                // Workflow Subject
                DataColumn colSubject = new DataColumn();
                colSubject.ColumnName = "Workflow Subject";
                colSubject.Caption = "Workflow Subject";
                resultTable.Columns.Add(colSubject);

                // Amount 
                DataColumn colAmount = new DataColumn();
                colAmount.ColumnName = "Amount";
                colAmount.Caption = "Amount";
                resultTable.Columns.Add(colAmount);

                // Workflow Status
                DataColumn colStatus = new DataColumn();
                colStatus.ColumnName = "Workflow Status";
                colStatus.Caption = "Workflow Status";
                resultTable.Columns.Add(colStatus);

                // Workflow type
                DataColumn colType = new DataColumn();
                colType.ColumnName = "Workflow Type";
                colType.Caption = "Workflow Type";
                resultTable.Columns.Add(colType);

                // Created
                DataColumn colCreated = new DataColumn();
                colCreated.ColumnName = "Created";
                colCreated.Caption = "Created";
                resultTable.Columns.Add(colCreated);
                resultTable.Columns.Add("Created2", typeof(DateTime));

                // Urgent
                DataColumn colUrgent = new DataColumn();
                colUrgent.ColumnName = "Urgent";
                colUrgent.Caption = "Urgent";
                resultTable.Columns.Add(colUrgent);

                // Deadline
                DataColumn colDeadline = new DataColumn();
                colDeadline.ColumnName = "Deadline";
                colDeadline.Caption = "Deadline";
                resultTable.Columns.Add(colDeadline);
                resultTable.Columns.Add("Deadline2", typeof(DateTime));

                // Contractor 
                DataColumn colContractor = new DataColumn();
                colContractor.ColumnName = "Contractor";
                colContractor.Caption = "Contractor";
                resultTable.Columns.Add(colContractor);

                // FWC Reference
                DataColumn colFWCReference = new DataColumn();
                colFWCReference.ColumnName = "FWC Reference";
                colFWCReference.Caption = "FWC Reference";
                resultTable.Columns.Add(colFWCReference);
                
                // Vacancy NO
                DataColumn colVacancyNo = new DataColumn();
                colVacancyNo.ColumnName = "Vacancy Number";
                colVacancyNo.Caption = "Vacancy Number";
                resultTable.Columns.Add(colVacancyNo);
                
                // Personal File
                DataColumn colPersonalFile = new DataColumn();
                colPersonalFile.ColumnName = "Personal File";
                colPersonalFile.Caption = "Personal File";
                resultTable.Columns.Add(colPersonalFile);

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CreateResultTable(): " + ex.Message, ex.StackTrace);
            }
        }

        //---------------------------------------------------------------------------
        //ADD ROWS
        //---------------------------------------------------------------------------
        protected void AddNewRow_DataRowView_Common(ref DataTable resultTable, SPWeb Web, string interfaceURL, string wftypeorder, DataRowView drv, string wfid)
        {

            try
            {
                DataRow newRecord = resultTable.NewRow();

                newRecord["ID"] = wfid;
                newRecord["Workflow ID"] = "<a href=\"" + Web.Url + interfaceURL + "?wfid=" + newRecord["ID"].ToString() + "&wftype=" + wftypeorder + "\" target=\"_self\"/>" + newRecord["ID"].ToString() + "</a>";
                newRecord["Workflow Subject"] = drv[4].ToString();
                newRecord["Amount"] = drv[5].ToString();
                newRecord["Workflow Status"] = drv[6].ToString();
                newRecord["Workflow Type"] = drv[7].ToString();
                newRecord["Created"] = DateTime.Parse(drv[8].ToString()).Date.ToString("dd/MM/yyyy");
                newRecord["Created2"] = DateTime.Parse(drv[8].ToString()).Date;
                newRecord["Urgent"] = drv[9].ToString().Equals("0") ? "No" : "Yes";

                // Deadline
                if (!string.IsNullOrEmpty(drv[10].ToString()))
                {
                    newRecord["Deadline"] = DateTime.Parse(drv[10].ToString()).Date.ToString("dd/MM/yyyy"); ;
                    newRecord["Deadline2"] = DateTime.Parse(drv[10].ToString()).Date;
                }

                // Contractor
                object contractor = drv[14];
                if (contractor != DBNull.Value)
                    newRecord["Contractor"] = drv[14].ToString();

                // FWC Reference
                object FWCReference = drv[15];
                if (FWCReference != DBNull.Value)
                    newRecord["FWC Reference"] = drv[15].ToString();
                
                // Vacancy No
                object VacancyNo = drv[16];
                if(VacancyNo != DBNull.Value)
                    newRecord["Vacancy Number"] = VacancyNo.ToString();

                // Personal File
                object PersonalFile = drv[17];
                if (PersonalFile != DBNull.Value)
                    newRecord["Personal File"] = PersonalFile.ToString();


                resultTable.Rows.Add(newRecord);
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("AddNewRow_DataRowView_Common(): WFID '" + wfid + "' " + ex.Message, ex.StackTrace);
            }
        }
      

        //protected void AddNewRow_DataRowView_Keyword(ref DataTable resultsCommonSearch, SPWeb Web, string interfaceURL, string wftypeorder, DataRowView drv, string wfid)
        //{

        //    try
        //    {
        //        DataRow newRecord = resultsCommonSearch.NewRow();
        //        newRecord["ID"] = wfid;
        //        newRecord["WFID"] = "<a href=\"" + Web.Url + interfaceURL + "?wfid=" + newRecord["ID"].ToString() + "&wftype=" + wftypeorder + "\" target=\"_self\"/>" + newRecord["ID"].ToString() + "</a>";
        //        newRecord["WFSubject"] = drv[4].ToString();
        //        newRecord["Amount"] = drv[5].ToString();
        //        newRecord["WFStatus"] = drv[6].ToString();
        //        newRecord["WFType"] = drv[7].ToString();
        //        newRecord["Created"] = DateTime.Parse(drv[8].ToString()).Date;
        //        newRecord["Urgent"] = drv[9].ToString().Equals("0") || drv[9].ToString().ToUpper().Equals("FALSE") ? "No" : "Yes";

        //        if (!string.IsNullOrEmpty(drv[10].ToString()))
        //            newRecord["WFDeadline"] = DateTime.Parse(drv[10].ToString()).Date;
                

        //        newRecord["UniqueId"] = drv[12].ToString();

        //        resultsCommonSearch.Rows.Add(newRecord);
        //    }
        //    catch (Exception ex)
        //    {
        //        Methods.saveErrorsLog("AddNewRow_DataRowView_Keyword(): WFID '" + wfid + "' " + ex.Message, ex.StackTrace);
        //    }
        //}

        protected void AddNewRow_ListItem_Keyword(ref DataTable resultTableAux, SPWeb Web, string interfaceURL, string wftypeorder, SPListItem item, string wfid)
        {

            try
            {

                DataRow newRecord = resultTableAux.NewRow();
                newRecord["ID"] = wfid;
                newRecord["Workflow ID"] = "<a href=\"" + Web.Url + interfaceURL + "?wfid=" + wfid + "&wftype=" + wftypeorder + "\" target=\"_self\"/>" + wfid + "</a>";

                if (item["WFSubject"] != null)
                    newRecord["Workflow Subject"] = item["WFSubject"].ToString();

                if (item["Amount"] != null)
                    newRecord["Amount"] = item["Amount"].ToString();

                if (item["WFStatus"] != null)
                    newRecord["Workflow Status"] = item["WFStatus"].ToString();

                if (item["WFType"] != null)
                    newRecord["Workflow Type"] = item["WFType"].ToString();

                if (item["Created"] != null)
                {
                    newRecord["Created"] = DateTime.Parse(item["Created"].ToString()).Date.ToString("dd/MM/yyyy");
                    newRecord["Created2"] = DateTime.Parse(item["Created"].ToString()).Date;
                }

                if (item["Urgent"] != null)
                    newRecord["Urgent"] = item["Urgent"].ToString().Equals("0") || item["Urgent"].ToString().ToUpper().Equals("FALSE") ? "No" : "Yes";

                if (item["WFDeadline"] != null)
                {
                    newRecord["Deadline"] = DateTime.Parse(item["WFDeadline"].ToString()).Date.ToString("dd/MM/yyyy");
                    newRecord["Deadline2"] = DateTime.Parse(item["WFDeadline"].ToString()).Date;
                }

                if (item.Fields.ContainsFieldWithStaticName("GFContractor"))
                    newRecord["Contractor"] = item["GFContractor"].ToString();
                else
                    newRecord["Contractor"] = null;

                if (item.Fields.ContainsFieldWithStaticName("GFFWCRef"))
                    newRecord["FWC Reference"] = item["GFFWCRef"].ToString();
                else
                    newRecord["FWC Reference"] = null;

                if (item.Fields.ContainsFieldWithStaticName("VacancyNo"))
                    newRecord["Vacancy Number"] = item["VacancyNo"].ToString();
                else
                    newRecord["Vacancy Number"] = null;

                if (item.Fields.ContainsFieldWithStaticName("PersonalFile"))
                    newRecord["Personal File"] = item["PersonalFile"].ToString();
                else
                    newRecord["Personal File"] = null;

                resultTableAux.Rows.Add(newRecord);
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("AddNewRow_ListItem_Keyword():  WFID '" + wfid + "' " + ex.Message, null);
            }
        }

        protected bool CheckIfExistsRow(string wfid, DataTable resultTableAux)
        {
            bool exist = false;

            try
            {
                if (resultTableAux != null && resultTableAux.Rows.Count > 0)
                {
                    DataRow[] foundWFID = resultTableAux.Select("ID = '" + wfid.Trim() + "'");

                    if (foundWFID.Length != 0)
                        exist = true;
                }

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("CheckIfExistsRow():  WFID '" + wfid + "' " + ex.Message, ex.StackTrace);
            }

            return exist;
        }


        #region <ROWS>
        /// <summary>
        /// Customize every result grid cell.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void gvResults_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            try
            {
                if (e.Row.RowType.Equals(DataControlRowType.DataRow))
                {
                    e.Row.Cells[0].Visible = false;
                    string decode = HttpUtility.HtmlDecode(e.Row.Cells[1].Text);
                    e.Row.Cells[1].Text = decode;
                    e.Row.Cells[1].HorizontalAlign = HorizontalAlign.Center;
                    e.Row.Cells[1].VerticalAlign = VerticalAlign.Middle;
                    e.Row.Cells[3].HorizontalAlign = HorizontalAlign.Center;
                    e.Row.Cells[3].VerticalAlign = VerticalAlign.Middle;
                    e.Row.Cells[4].HorizontalAlign = HorizontalAlign.Center;
                    e.Row.Cells[4].VerticalAlign = VerticalAlign.Middle;
                    e.Row.Cells[6].HorizontalAlign = HorizontalAlign.Center;
                    e.Row.Cells[6].VerticalAlign = VerticalAlign.Middle;
                    e.Row.Cells[7].Visible = false;
                    e.Row.Cells[8].HorizontalAlign = HorizontalAlign.Center;
                    e.Row.Cells[8].VerticalAlign = VerticalAlign.Middle;
                    e.Row.Cells[9].HorizontalAlign = HorizontalAlign.Center;
                    e.Row.Cells[9].VerticalAlign = VerticalAlign.Middle;
                    e.Row.Cells[10].Visible = false;
                    e.Row.Cells[11].HorizontalAlign = HorizontalAlign.Center;
                    e.Row.Cells[11].VerticalAlign = VerticalAlign.Middle;
                    e.Row.Cells[12].HorizontalAlign = HorizontalAlign.Center;
                    e.Row.Cells[12].VerticalAlign = VerticalAlign.Middle;
                }
                else if (e.Row.RowType.Equals(DataControlRowType.Header))
                {
                    e.Row.Cells[0].Visible = false;
                    gvResults.Width = Unit.Pixel(1000);
                    e.Row.Cells[1].Width = Unit.Percentage(10);
                    e.Row.Cells[2].Width = Unit.Percentage(20);
                    e.Row.Cells[3].Width = Unit.Percentage(10);
                    e.Row.Cells[4].Width = Unit.Percentage(10);
                    e.Row.Cells[5].Width = Unit.Percentage(20);
                    e.Row.Cells[6].Width = Unit.Percentage(12.5);
                    e.Row.Cells[7].Visible = false;
                    e.Row.Cells[8].Width = Unit.Percentage(5);
                    e.Row.Cells[9].Width = Unit.Percentage(12.5);
                    e.Row.Cells[10].Visible = false;
                    e.Row.Cells[11].Width = Unit.Percentage(5);
                    e.Row.Cells[12].Width = Unit.Percentage(12.5);
                }
                else if (e.Row.RowType.Equals(DataControlRowType.Pager))
                {
                    e.Row.Cells[0].HorizontalAlign = HorizontalAlign.Right;
                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("gvResults_RowDataBound: " + ex.Message, ex.StackTrace);
            }
        }

        #endregion

        #region <SORTING>

        /// <summary>
        /// Sort result grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void gvResults_Sorting(object sender, GridViewSortEventArgs e)
        {
            try
            {
                if (ViewState["SearchResultData"] != null)
                {
                    if (ViewState["SearchSortingField"] != null)
                    {
                        String previousValueField = ViewState["SearchSortingField"] as String;

                        if (e.SortExpression.ToUpper().Equals("WORKFLOW ID"))
                            e.SortExpression = "ID";

                        if (e.SortExpression.ToUpper().Equals("DEADLINE"))
                            e.SortExpression = "DEADLINE2";

                        if (e.SortExpression.ToUpper().Equals("CREATED"))
                            e.SortExpression = "CREATED2";

                        if (e.SortExpression.ToUpper().Equals(previousValueField.ToUpper()))
                        {
                            if (ViewState["SearchSortingDirection"] != null)
                            {
                                String previousValueDirection = ViewState["SearchSortingDirection"] as String;
                                if (previousValueDirection.ToUpper().Equals("ASC"))
                                    ViewState["SearchSortingDirection"] = "DESC";
                                else
                                    ViewState["SearchSortingDirection"] = "ASC";
                            }
                            else
                                ViewState["SearchSortingDirection"] = "ASC";
                        }
                        else
                        {
                            ViewState["SearchSortingField"] = e.SortExpression;
                            ViewState["SearchSortingDirection"] = "ASC";
                        }
                    }
                    else
                    {
                        ViewState["SearchSortingField"] = e.SortExpression;
                        ViewState["SearchSortingDirection"] = "ASC";
                    }

                    DataTable auxDataTable = (DataTable)ViewState["SearchResultData"];
                    String sortField = ViewState["SearchSortingField"] as String;
                    String sortDirection = ViewState["SearchSortingDirection"] as String;
                    auxDataTable.DefaultView.Sort = sortField + " " + sortDirection;
                    gvResults.DataSource = auxDataTable;
                    gvResults.Width = Unit.Pixel(1000);
                    gvResults.HeaderStyle.CssClass = "header_background";
                    gvResults.AlternatingRowStyle.CssClass = "result_grid_even";
                    gvResults.HeaderRow.Cells[0].Text = "ID";
                    gvResults.HeaderRow.Cells[0].Width = Unit.Percentage(10);
                    gvResults.HeaderRow.Cells[1].Text = "Workflow ID";
                    gvResults.HeaderRow.Cells[1].Width = Unit.Percentage(10);
                    gvResults.HeaderRow.Cells[2].Text = "Workflow Subject";
                    gvResults.HeaderRow.Cells[2].Width = Unit.Percentage(20);
                    gvResults.HeaderRow.Cells[3].Text = "Amount";
                    gvResults.HeaderRow.Cells[3].Width = Unit.Percentage(10);
                    gvResults.HeaderRow.Cells[4].Text = "Workflow Status";
                    gvResults.HeaderRow.Cells[4].Width = Unit.Percentage(10);
                    gvResults.HeaderRow.Cells[5].Text = "Workflow Type";
                    gvResults.HeaderRow.Cells[5].Width = Unit.Percentage(20);
                    gvResults.HeaderRow.Cells[6].Text = "Created";
                    gvResults.HeaderRow.Cells[6].Width = Unit.Percentage(12.5);
                    gvResults.HeaderRow.Cells[7].Text = "Urgent";
                    gvResults.HeaderRow.Cells[7].Width = Unit.Percentage(5);
                    gvResults.HeaderRow.Cells[8].Text = "Deadline";
                    gvResults.HeaderRow.Cells[8].Width = Unit.Percentage(12.5);
                    gvResults.HeaderRow.Cells[9].Text = "Contractor";
                    gvResults.HeaderRow.Cells[9].Width = Unit.Percentage(12.5);
                    gvResults.HeaderRow.Cells[10].Text = "FWC Reference";
                    gvResults.HeaderRow.Cells[10].Width = Unit.Percentage(12.5);
                    gvResults.DataBind();
                    auxDataTable.Dispose();
                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("gvResults_Sorting: " + ex.Message, ex.StackTrace);
            }
        }

        #endregion

        #region <PAGING>

        /// <summary>
        /// Manage result grid paging
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void gvResults_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            try
            {
                if (ViewState["SearchResultData"] != null)
                {
                    gvResults.PageIndex = e.NewPageIndex;
                    DataTable auxDataTable = (DataTable)ViewState["SearchResultData"];

                    if (ViewState["SearchSortingField"] != null && ViewState["SearchSortingDirection"] != null)
                    {
                        String sortField = ViewState["SearchSortingField"] as String;
                        String sortDirection = ViewState["SearchSortingDirection"] as String;
                        auxDataTable.DefaultView.Sort = sortField + " " + sortDirection;
                    }

                    gvResults.DataSource = auxDataTable;
                    gvResults.DataBind();
                    gvResults.Width = Unit.Pixel(1000);
                    gvResults.HeaderStyle.CssClass = "header_background";
                    gvResults.AlternatingRowStyle.CssClass = "result_grid_even";
                    gvResults.HeaderRow.Cells[0].Text = "ID";
                    gvResults.HeaderRow.Cells[0].Width = Unit.Percentage(10);
                    gvResults.HeaderRow.Cells[1].Text = "Workflow ID";
                    gvResults.HeaderRow.Cells[1].Width = Unit.Percentage(10);
                    gvResults.HeaderRow.Cells[2].Text = "Workflow Subject";
                    gvResults.HeaderRow.Cells[2].Width = Unit.Percentage(20);
                    gvResults.HeaderRow.Cells[3].Text = "Amount";
                    gvResults.HeaderRow.Cells[3].Width = Unit.Percentage(10);
                    gvResults.HeaderRow.Cells[4].Text = "Workflow Status";
                    gvResults.HeaderRow.Cells[4].Width = Unit.Percentage(10);
                    gvResults.HeaderRow.Cells[5].Text = "Workflow Type";
                    gvResults.HeaderRow.Cells[5].Width = Unit.Percentage(20);
                    gvResults.HeaderRow.Cells[6].Text = "Created";
                    gvResults.HeaderRow.Cells[6].Width = Unit.Percentage(12.5);
                    gvResults.HeaderRow.Cells[7].Text = "Urgent";
                    gvResults.HeaderRow.Cells[7].Width = Unit.Percentage(5);
                    gvResults.HeaderRow.Cells[8].Text = "Deadline";
                    gvResults.HeaderRow.Cells[8].Width = Unit.Percentage(12.5);
                    gvResults.HeaderRow.Cells[9].Text = "Contractor";
                    gvResults.HeaderRow.Cells[9].Width = Unit.Percentage(12.5);
                    gvResults.HeaderRow.Cells[10].Text = "FWC Reference";
                    gvResults.HeaderRow.Cells[10].Width = Unit.Percentage(12.5);
                    gvResults.DataBind();

                    auxDataTable.Dispose();
                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("gvResults_PageIndexChanging: " + ex.Message, ex.StackTrace);
            }
        }

        #endregion

        #endregion
    }
}
