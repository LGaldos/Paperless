using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
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
using Microsoft.SharePoint.WebControls;

namespace ESMA.Paperless.Webparts.v16.RSWorkflowAdvancedSearch
{
    class Methods
    {
        /// <summary>
        /// Get all Routing Slip configuration parameters.
        /// </summary>
        /// <param name="Web"></param>
        /// <returns>String dictionary with all Routing Slip configuration parameters</returns>
        public static Dictionary<string, string> GetConfigurationParameters(SPWeb Web)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            try
            {
                SPList list = Web.GetListFromWebPartPageUrl(Web.Url + "/Lists/WFConfigParameters/AllItems.aspx");
                SPQuery query = new SPQuery();
                query.Query = "<Where><IsNotNull><FieldRef Name='Title'/></IsNotNull></Where>";
                query.ViewFields = string.Concat(
                                   "<FieldRef Name='Title' />",
                                   "<FieldRef Name='Value1' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need
                SPListItemCollection itemCollection = list.GetItems(query);

                foreach (SPListItem item in itemCollection)
                {
                    try
                    {
                        if (item["Value1"] != null)
                            parameters.Add(item.Title, item["Value1"].ToString().Trim());
                        else
                            parameters.Add(item.Title, string.Empty);
                    }
                    catch { continue; }
                }
            }
            catch (Exception ex)
            {
                saveErrorsLog(string.Empty, "GetConfigurationParameters " + ex.Message);
            }
            return parameters;
        }

        public static string GetDefinitionGroupName(string ADGroupName, Dictionary<string, string> parameters)
        {
            string groupname = string.Empty;

            try
            {
                List<string> keyList = new List<string>(parameters.Keys);

                if (keyList.Contains(ADGroupName))
                    groupname = parameters[ADGroupName];
                else
                    groupname = ADGroupName;
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog(string.Empty, "GetDefinitionGroupName: ADGroupName - '" + ADGroupName + "' " + ex.Message);
            }

            return groupname;
        }

        public static string GetADGroupName(string DefinitionGroupName, Dictionary<string, string> parameters)
        {
            string groupname = string.Empty;

            try
            {
                List<string> valuesList = new List<string>(parameters.Values);

                if (valuesList.Contains(DefinitionGroupName))
                    groupname = parameters.FirstOrDefault(x => x.Value == DefinitionGroupName).Key;
                else
                    groupname = DefinitionGroupName;
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog(string.Empty, "GetADGroupName() - DefinitionGroupName: '" + DefinitionGroupName  + "' " + ex.Message);
            }

            return groupname;
        }

        /// <summary>
        /// Get workflow type orders    
        /// </summary>
        /// <param name="Web"></param>
        /// <returns>String dictionary with Workflow type orders</returns>
        public static Dictionary<string, string> GetWorkflowTypeOrder(SPWeb Web)
        {
            Dictionary<string, string> wftypes = new Dictionary<string, string>();

            try
            {
                SPList list = Web.GetListFromWebPartPageUrl(Web.Url + "/Lists/WFConfiguration/AllItems.aspx");
                SPQuery query = new SPQuery();
                query.Query = "<Where><IsNotNull><FieldRef Name='WFOrder'/></IsNotNull></Where>";
                query.ViewFields = string.Concat(
                                   "<FieldRef Name='WFOrder' />",
                                    "<FieldRef Name='Title' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need
                SPListItemCollection itemCollection = list.GetItems(query);

                foreach (SPListItem item in list.Items)
                {
                    if (item["WFOrder"] != null)
                        wftypes.Add(item.Title.ToUpper(), item["WFOrder"].ToString());
                }

            }
            catch (Exception ex)
            {
                saveErrorsLog(string.Empty, "GetWorkflowTypeOrder: " + ex.Message);
            }

            return wftypes;
        }

        //CR31
        public static SPUser GetRealCurrentSpUser(Page currPage)
        {
            if (SPContext.Current.Web.CurrentUser.ToString().ToUpper().Equals("SHAREPOINT\\SYSTEM"))
            {
                return SPContext.Current.Web.Users[currPage.User.Identity.Name];
            }

            return SPContext.Current.Web.CurrentUser;
        }

        /// <summary>
        /// Ger user login name and name without domain info.
        /// </summary>
        /// <param name="userLoginName"></param>
        /// <param name="userName"></param>
        public static void GetUserData(ref string userLoginName, ref string userName)
        {
            try
            {
                if (!userLoginName.Equals("SHAREPOINT\\System"))
                {
                    if (userLoginName.Contains("\\"))
                        userLoginName = userLoginName.Split('\\')[1].ToString();

                    if (userName.Contains("\\"))
                        userName = userName.Split('\\')[1].ToString();
                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog(string.Empty, "GetUserData - '" + userLoginName + "' " +  ex.Message);
            }
        }

        /// <summary>
        /// Get group names from workflow object
        /// </summary>
        /// <param name="stepInfo"></param>
        /// <param name="Web"></param>
        /// <returns>String list of group names</returns>
        public static List<string> GetGroupNames(string stepInfo, SPWeb Web)
        {
            List<string> groupNames = new List<string>();
            try
            {
                if (!string.IsNullOrEmpty(stepInfo))
                {
                    string[] steps = Regex.Split(stepInfo, "&#");

                    int count = 0;
                    foreach (string step in steps)
                    {
                        string[] stepRecord = Regex.Split(steps[count].ToString(), ";#");
                        groupNames.Add(stepRecord[2].Split('\\')[1]);
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog(string.Empty, "GetGroupNames: " + ex.Message);
            }

            return groupNames;
        }

        public static string GetUserAccountFromActorSelected(SPWeb Web, string selectedValue)
        {
            string userAccount = string.Empty;

            try
            {
                SPUser selectedActor = Web.EnsureUser(selectedValue);

                userAccount = selectedActor.LoginName;
                string userName = string.Empty;

                GetUserData(ref userAccount, ref userName);

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog(string.Empty, "GetUserAccountFromActorSelected(): " + ex.Message);
            }

            return userAccount;
        }

        public static SPListItem GetWFInformationByWFID(string wfid, SPWeb Web)
        {
            SPListItem item = null;

            try
            {
                SPList list = Web.GetListFromWebPartPageUrl(Web.Url + "/Lists/WFHistory/AllItems.aspx");
                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='WFID' /><Value Type='Number'>" + wfid.Trim() + "</Value></Eq></Where>";
                query.ViewFields = string.Concat(
                                   "<FieldRef Name='WFID' />", "<FieldRef Name='WFType' />", "<FieldRef Name='WFSubject' />", "<FieldRef Name='Created' />",
                                   "<FieldRef Name='Amount' />", "<FieldRef Name='WFStatus' />", "<FieldRef Name='Urgent' />", "<FieldRef Name='WFDeadline' />", "<FieldRef Name='ConfidentialWorkflow' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need
                SPListItemCollection itemCollection = list.GetItems(query);

                if (itemCollection.Count > 0)
                    item = itemCollection[0];

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog(wfid, "GetWFInformationByWFID() " + ex.Message);
            }

            return item;
        }

        public static string FormatWFID(string wfid)
        {
            try
            {
                if (wfid.Contains("."))
                {
                    string[] inf = wfid.Split('.');
                    wfid = inf[0];
                }

            }
            catch   (Exception ex)
            {
                Methods.saveErrorsLog(wfid, "FormatWFID() " + ex.Message);
            }

            return wfid;
        }

        public static List<string> GetAllEspecificGeneralFieldsFromList(SPWeb Web)
        {
            List<string> fieldsList = new List<string>();

            try
            {

                SPList list = Web.GetListFromWebPartPageUrl(Web.Url + "/Lists/WFGeneralFields/AllItems.aspx");

                SPQuery query = new SPQuery();
                query.Query = "<Where><IsNotNull><FieldRef Name='Title' /></IsNotNull></Where>";
                query.ViewFields = string.Concat(
                               "<FieldRef Name='Title' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need
                SPListItemCollection itemCollection = list.GetItems(query);


                foreach (SPListItem item in itemCollection)
                {
                    fieldsList.Add(item["Title"].ToString());
                }


            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("GetAllEspecificGeneralFieldsFromList(): " + ex.Message, ex.StackTrace);
            }

            return fieldsList;
        }

        public static Dictionary<string, SPField> GetGFsDictionary()
        {
            Dictionary<string, SPField> GFieldsDictionary = new Dictionary<string, SPField>();

            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
              {
                  using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                  {
                      SPWeb Web = Site.OpenWeb();
                      SPFieldCollection allFields = Web.Fields;

                      //Get All GFs fro "RS Workflow GFs" list
                       List<string> GFsList =  GetAllEspecificGeneralFieldsFromList(Web);

                      foreach (SPField field in allFields)
                      {
                          if (field.Group.Equals("RS Columns"))
                          {
                              string displayName = field.Title;
                              string internalName = field.InternalName;

                              if (GFsList.Contains(displayName))
                                  GFieldsDictionary.Add(internalName, field);
                              
                          }

                      }


                      Web.Close();
                      Web.Dispose();
                  }

              });

            }
            catch (Exception ex)
            {
                Methods.saveErrorsLog("GetSiteColumnsFromRSGroup(): " + ex.Message, ex.StackTrace);
            }

            return GFieldsDictionary;
        }
    


        #region <ERROR>

        public static void saveErrorsLog(string source, string message)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url))
                    {
                        SPWeb Web = Site.OpenWeb();
                        Web.AllowUnsafeUpdates = true;

                        SPList errorList = Web.GetListFromWebPartPageUrl(Web.Url + "/Lists/ErrorLog/AllItems.aspx");
                        SPUser user = SPContext.Current.Web.CurrentUser;

                        string messageValue = "[RSSearcher '" + user + "'] " + source + " - " + message;

                        if (messageValue.Length > 256)
                            messageValue = messageValue.Substring(0, 128);

                        if (message.Length > 570)
                            message = message.Substring(0, 570);

                            SPQuery query = new SPQuery();
                            query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + messageValue + "</Value></Eq></Where>";
                            query.ViewFields = string.Concat(
                                       "<FieldRef Name='Title' />",
                                       "<FieldRef Name='RSQueryLog' />");
                            query.ViewFieldsOnly = true; // Fetch only the data that we need
                            SPListItemCollection itemCollection = errorList.GetItems(query);

                            //if (itemCollection.Count > 0)
                            //{
                            //    SPListItem itm = itemCollection[0];
                            //    itm["Title"] = messageValue;
                            //    itm["RSQueryLog"] = message;
                            //    itm.Update();
                            //}
                            //else
                            //{
                                SPListItem itm = errorList.Items.Add();
                                itm["Title"] = messageValue;
                                itm["RSQueryLog"] = message;
                                itm.Update();
                            //}
    

                        Web.AllowUnsafeUpdates = false;
                        Web.Close();
                        Web.Dispose();
                    }
                });

            }
            catch (Exception ex)
            {
            }
        }

        #endregion
    }
}
