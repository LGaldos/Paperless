using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;

namespace ESMA.Paperless.Webparts.v16.RSWorkflowInitiation
{
    public partial class RSWorkflowInitiationUserControl : UserControl
    {
        public RSWorkflowInitiation WebPart { get; set; }
        public Hashtable wfTable;
        Dictionary<string, string> parameters;
        public LinkButton lnkBtn;
        Dictionary<string, List<LinkButton>> controlsDictionary;

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected override void OnInit(EventArgs e)
        {
            try
            {
                base.CreateChildControls();
                this.Controls.Clear();

                controlsDictionary = new Dictionary<string, List<LinkButton>>();

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                    {
                        SPWeb Web = Site.OpenWeb();
                        parameters = Methods.GetConfigurationParameters(Web);
                        SPUser loggedUser = Methods.GetRealCurrentSPUser(this.Page);


                        if (parameters.ContainsKey("Domain") && parameters.ContainsKey("AD User") && parameters.ContainsKey("AD Password"))
                        {
                            string domainName = parameters["Domain"];
                            string userAD = Methods.Decrypt(parameters["AD User"]);
                            string passwordAD = Methods.Decrypt(parameters["AD Password"]);

                            if (!loggedUser.LoginName.ToString().ToLower().Equals(@"sharepoint\system"))
                                controlsDictionary = GetLinksDictionary(loggedUser, lnkBtn, Web, domainName, userAD, passwordAD);
                            
                        }

                        else
                        {
                            string message = "Invalid credentials to access A.D (domain, user or password)";
                            Methods.SaveErrorsLog("Render()" + null, message);
                        }

                        Web.Close();
                        Web.Dispose();
                    }

                });

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("OnInit() - " + ex.Source, ex.Message);
            }
        }

        /// <summary>
        /// Load and display all retrieved workflow definition data about the workflows logged can created.
        /// </summary>
        /// <param name="writer"></param>
        protected override void Render(HtmlTextWriter writer)
        {

            try
            {
                writer.Write("<h1>Initiation Workflows:</h1>");
                writer.Write("<div>");

                if (controlsDictionary != null && controlsDictionary.Count > 0)
                {
                    foreach (KeyValuePair<string, List<LinkButton>> kvp in controlsDictionary)
                    {
                        string category = kvp.Key;
                        List<LinkButton> items = kvp.Value;

                        if (items.Count > 0)
                        {

                            writer.Write("<table>");
                            writer.Write("<tr>");
                            writer.Write("<td align=\"left\" class=\"ms-formlabel\">");
                            writer.Write("<b>" + category.Trim() + "</b>");
                            writer.Write("</td>");
                            writer.Write("</tr>");
                            writer.Write("<tr>");
                            writer.Write("<td>");

                            foreach (LinkButton item in items)
                            {

                                writer.Write("<table style=\"padding-left:20\">");
                                writer.WriteLine("<tr>");
                                writer.WriteLine("<td align=\"left\" class=\"ms-formlabel\"><img style='margin: 0 5px 0 0' src='/_layouts/15/ESMA.Paperless.Design.v16/images/RSAdd.png' align=left width=12 height=12>");

                                item.RenderControl(writer);

                                writer.WriteLine("</td>");
                                writer.WriteLine("</tr>");
                                writer.Write("</table>");
                            }

                            writer.Write("</td>");
                            writer.Write("</tr>");
                            writer.Write("</table>");
                        }

                    }

                }
                else
                {
                    writer.Write("There are no workflows available to initiate.");
                }

                writer.Write("</div>");

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("Render() - " + ex.Source, ex.Message);
            }

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Dictionary<string, List<LinkButton>> GetLinksDictionary( SPUser loggedUser, LinkButton lnkBtn, SPWeb Web, string domainName, string userAD, string passwordAD)
        {
            Dictionary<string, List<LinkButton>> controlsDictionary = new Dictionary<string, List<LinkButton>>();

            try
            {
                Dictionary<string, Dictionary<string, List<string>>>  WFsCategoryDictionary = new Dictionary<string, Dictionary<string, List<string>>> ();
                SPList configList = Web.GetListFromWebPartPageUrl(Web.Url + "/Lists/WFConfiguration/AllItems.aspx");

                //Load workflow definition in a hashtable sorted by workflow order
                Methods.GetWFInformationByCategory(ref WFsCategoryDictionary, Web, configList);
                Dictionary<string, bool> permissionsDictionary = new Dictionary<string, bool>();


                    foreach (string category in WFsCategoryDictionary.Keys)
                    {
                        
                            Dictionary<string, List<string>> listOfWFs = (Dictionary<string, List<string>>)WFsCategoryDictionary[category];
                            List<LinkButton> controlList = new List<LinkButton>();

                               
                            //Iterate all workflow definitions and check if logged user can create that type of workflows
                            foreach (string wfOrder in listOfWFs.Keys)
                            {
                                List<string> wfDetailsList = listOfWFs[wfOrder];

                                string wfTitle = wfDetailsList[0];
                                string groupName = wfDetailsList[1];

                               

                                    if (!permissionsDictionary.ContainsKey(groupName))
                                    {
                                        bool belongToGroup = Methods.UserBelongToGroup(domainName, groupName, loggedUser, userAD, passwordAD, parameters, wfTitle);
                                        permissionsDictionary.Add(groupName, belongToGroup);
                                    }

                                    if (permissionsDictionary[groupName].Equals(true))
                                    {
                                        CreateLinkButtonControl(ref lnkBtn, wfTitle, wfOrder, Web);
                                        controlList.Add(lnkBtn);

                                    }
                                
                            }


                        if (controlList.Count > 0)
                            controlsDictionary.Add(category, controlList);
                    }


            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GetLinksDictionary - " + ex.Source, ex.Message);
            }
            return controlsDictionary;
        }

        /// <summary>
        /// Format workflow creation link
        /// </summary>
        /// <param name="stringWriter"></param>
        /// <param name="wfName"></param>
        /// <param name="wfOrder"></param>
        /// <param name="MyWeb"></param>
        private void CreateLinkButtonControl(ref LinkButton lnkBtn, string wfName, string wfOrder, SPWeb MyWeb)
        {
            try
            {


                lnkBtn = new LinkButton();
                lnkBtn.ID = "lknBtn_" + wfOrder;
                lnkBtn.Text = wfName.ToUpper();
                lnkBtn.CssClass = "ms-formlabel";
                lnkBtn.Click += new EventHandler(lnkBtn_Click);
                //lnkBtn.PostBackUrl = finalURL;
                this.Controls.Add(lnkBtn);

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateLinkButtonControl() - " + ex.Source, ex.Message);
            }
        }

        public void lnkBtn_Click(object sender, EventArgs e)
        {
            try
            {
                LinkButton lnkBtn = (LinkButton)sender;
                string wfid = GenerateWFID();

                if (parameters["Interface Page"] != null)
                {
                    string finalURL = Methods.CreateURL(lnkBtn.ID.Replace("lknBtn_", null), parameters["Interface Page"]);
                    string newURL = string.Empty;

                    if (finalURL.Contains("XXXX"))
                        newURL = finalURL.Replace("XXXX", wfid);

                    //lnkBtn.PostBackUrl = newURL;
                    Page.Response.Redirect(newURL, false);
                }


            }
            catch (Exception ex)
            {
                //Methods.SaveErrorsLog("lnk_Click() - " + ex.Source, ex.Message);
            }

        }

        private string GenerateWFID()
        {

            string wfid = string.Empty;

            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                    {
                        SPWeb Web = Site.OpenWeb();

                        if (!Web.AllowUnsafeUpdates)
                            Web.AllowUnsafeUpdates = true;

                        if (parameters.ContainsKey("WFID Counter"))
                        {
                            wfid = Methods.IncreaseCounter(parameters["WFID Counter"]);

                            if (!string.IsNullOrEmpty(wfid))
                                Methods.SetConfigurationParameter("WFID Counter", ref  wfid, Web, ref parameters);
                            else
                                Methods.SaveErrorsLog("There was a problem increasing the counter. WFID: '" + wfid + "'", null);

                        }

                        if (Web.AllowUnsafeUpdates)
                            Web.AllowUnsafeUpdates = false;

                        Web.Close();
                        Web.Dispose();
                    }
                });
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GenerateWFID() - " + ex.Source, ex.Message);
            }

            return wfid;
        }


    }
}
