using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;
using System.Web.UI;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

namespace ESMA.Paperless.Webparts.v16.RSWorkflowInitiation
{
    class Methods
    {
        /// <summary>
        /// Get logged user
        /// </summary>
        /// <param name="currPage">Page object with web browser information</param>
        /// <returns>Logged user profile in SharePoint</returns>
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


        /// <summary>
        /// Get Workflow definitions grouped and ordered
        /// </summary>
        /// <param name="wfTable"></param>
        /// <param name="Web"></param>
        public static void GetWFInformationByCategory(ref Dictionary<string, Dictionary<string, List<string>>> WFsCategoryDictionary, SPWeb Web, SPList configList)
        {
           
            
            
            try
            {
               
                //Get all Categories
                List<string> categoryList = GetAllWFCategory(Web, configList);

                foreach (string category in categoryList)
                {
                    Dictionary<string, List<string>> WFsDictionary = new Dictionary<string, List<string>>();

                    SPQuery query = new SPQuery();
                    query.ViewFields = string.Concat(
                                          "<FieldRef Name='Title' />",
                                          "<FieldRef Name='WFEnabled' />",
                                          "<FieldRef Name='WFCategory' />",
                                          "<FieldRef Name='WFOrder' />",
                                          "<FieldRef Name='WFGroup' />");
                    query.ViewFieldsOnly = true; // Fetch only the data that we need
                    query.Query = "<Where><And><IsNotNull><FieldRef Name='Title'/></IsNotNull>"
                                  + "<And><Eq><FieldRef Name='WFCategory' /><Value Type='Choice'>" + category  + "</Value></Eq>"
                                  + "<Eq><FieldRef Name='WFEnabled' /><Value Type='Boolean'>1</Value></Eq>"
                                  + "</And></And></Where>"
                                  + "<OrderBy><FieldRef Name='WFOrder'/></OrderBy>";

                    SPListItemCollection itemCollection = configList.GetItems(query);

                    foreach (SPListItem item in itemCollection)
                    {
                        
                        if (item["WFGroup"] != null)
                        {
                                List<string> wfDetailsList = new List<string>();
                                string wfOrder = item["WFOrder"].ToString();
                                    wfDetailsList.Add(item["Title"].ToString()); //[0]
                                    wfDetailsList.Add(item["WFGroup"].ToString()); //[1]

                                    //Adding WF Information -> WFOrder is PK
                                    WFsDictionary.Add(wfOrder, wfDetailsList);
                                   
                        }
                    }


                    if (!WFsCategoryDictionary.ContainsKey(category))
                        WFsCategoryDictionary.Add(category, WFsDictionary);
          
                }
            }
            catch (Exception ex)
            {
                SaveErrorsLog("GetWFInformationByCategory() - " + ex.Source, ex.Message);
            }
        }


        public static List<string> GetAllWFCategory(SPWeb Web, SPList configList)
        {
            List<string> categoryList = new List<string>();

            try 
            { 
                    SPQuery query = new SPQuery();
                    query.ViewFields = string.Concat(
                                          "<FieldRef Name='Title' />",
                                          "<FieldRef Name='WFEnabled' />",
                                          "<FieldRef Name='WFCategory' />");
                    query.ViewFieldsOnly = true; // Fetch only the data that we need
                    query.Query = "<Where><And><IsNotNull><FieldRef Name='WFCategory'/></IsNotNull>"
                                  + "<Eq><FieldRef Name='WFEnabled' /><Value Type='Boolean'>1</Value></Eq></And></Where>"
                                  + "<OrderBy><FieldRef Name='WFCategory'/></OrderBy>";

                    SPListItemCollection itemCollection = configList.GetItems(query);

                    foreach (SPListItem item in itemCollection)
                    {
                        string categoryValue = item["WFCategory"].ToString();

                        if (!categoryList.Contains(categoryValue))
                            categoryList.Add(categoryValue);
                    }
            }
            catch (Exception ex)
            {
                SaveErrorsLog("GetAllWFCategory() - " + ex.Source, ex.Message);
            }

            return categoryList;
        }

        /// <summary>
        /// Get Workflow creation form URL
        /// </summary>
        /// <param name="wfType"></param>
        /// <param name="MyWeb"></param>
        /// <param name="parameters"></param>
        /// <returns>Get Workflow creation form URL</returns>
        public static string CreateURL(string wfType, string pageWebpart)
        {
            try
            {
                string urlToReturn = SPContext.Current.Web.Site.Url;


                //CR26. Add wfnew=1
                if (!string.IsNullOrEmpty(pageWebpart))
                    urlToReturn = SPContext.Current.Web.Url + pageWebpart + "?wfid=XXXX&wftype=" + wfType + "&wfnew=1";

                return urlToReturn;
            }
            catch (Exception ex)
            {
                SaveErrorsLog("CreateURL() - " + ex.Source, ex.Message);
                return "/";
            }
        }


        /// <summary>
        /// Get configuration parameters
        /// </summary>
        /// <param name="Web"></param>
        /// <returns>Configuration parameters string dictionary</returns>
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
                SaveErrorsLog("GetConfigurationParameters() - " + ex.Source, ex.Message);
            }

            return parameters;
        }

        /// <summary>
        /// Set dinamically any configuration parameter value.
        /// </summary>
        /// <param name="parameterKey"></param>
        /// <param name="wfid"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        public static void SetConfigurationParameter(string parameterKey, ref string wfid, SPWeb Web, ref Dictionary<string, string> parameters)
        {
            string errorMessage = string.Empty;

            try
            {
                SPList list = Web.GetListFromWebPartPageUrl(Web.Url + "/Lists/WFConfigParameters/AllItems.aspx");
                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + parameterKey.Trim() + "</Value></Eq></Where>";
                //query.ViewFields = string.Concat(
                //                  "<FieldRef Name='Title' />",
                //                  "<FieldRef Name='Value1' />");
                //query.ViewFieldsOnly = true; // Fetch only the data that we need.
                SPListItemCollection itemCollection = list.GetItems(query);

             
                    if (itemCollection != null && itemCollection.Count.Equals(1))
                    {
                        SPListItem item = itemCollection[0];

                 
                            if (item["Value1"].ToString().Equals(wfid))
                                wfid = IncreaseCounter(wfid);

                            item["Value1"] = wfid;

                            using (new DisabledItemEventsScope())
                            {
                                item.SystemUpdate();
                            }
                        

                        parameters = GetConfigurationParameters(Web);
                    }
                    else
                        errorMessage = "Ambiguous parameter key (" + parameterKey + "). Error increasing the counter.";
                }            
            catch (Exception ex)
            {
                SaveErrorsLog(wfid, "SetConfigurationParameter (WFID). Error increasing the counter...setting the WFID again." + ex.Message);
                Methods.SetConfigurationParameter(parameterKey, ref wfid, Web, ref parameters);
            }
        }

        public static string IncreaseCounter(string wfidPrev)
        {
            string wfidChecked = string.Empty;
            int wfid = 0;

            try
            {
                if (int.TryParse(wfidPrev, out wfid))
                {

                    wfid++;
                    wfidChecked = wfid.ToString();
                }

            }
            catch (Exception ex)
            {
                SaveErrorsLog(wfidPrev, "IncreaseCounter: " + ex.Message);
            }

            return wfidChecked;
        }

        public static bool UserBelongToGroup(string domainName, string groupName, SPUser user, string userAD, string passwordAD, Dictionary<string, string> parameters, string wfTitle)
        {
            bool belong = false;

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, domainName, userAD, passwordAD))
                {
                    if (context != null)
                    {
                        groupName = GetOnlyUserAccount(groupName);


                        if (!string.IsNullOrEmpty(groupName))
                        {
                            //ESMA-CR28 - Nested Groups
                            using (UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(context, GetOnlyUserAccount(user.LoginName)))
                            {
                                if (parameters["Nested Groups"].ToLower().Equals("false"))
                                    belong = userPrincipal.IsMemberOf(context, IdentityType.SamAccountName, groupName);
                                else
                                {
                                    using (PrincipalSearchResult<Principal> groups = userPrincipal.GetAuthorizationGroups())
                                    {
                                        return groups.OfType<GroupPrincipal>().Any(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
                                    }
                                }
                            }
                        }
                        else
                            SaveErrorsLog("UserBelongToGroup() - User: '" + user.LoginName + "'. GroupName NULL in WFType: '" + wfTitle + "'." , string.Empty);
                    }
                    else
                    {
                        SaveErrorsLog("UserBelongToGroup() - Problems to connect AD. User: '" + groupName + "'", string.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                SaveErrorsLog("UserBelongToGroup() - User: '" + user.LoginName + " ("+  groupName + ")' - " + ex.Source, ex.Message);
            }

            return belong;
        }

        /// <summary>
        /// Log Errors in Error Log SharePoint List
        /// </summary>
        /// <param name="wfID"></param>
        /// <param name="message"></param>
        public static void SaveErrorsLog(string wfID, string message)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite colsit = new SPSite(SPContext.Current.Web.Url.ToString()))
                    {
                        SPWeb MyWeb = colsit.OpenWeb();

                        if (!MyWeb.AllowUnsafeUpdates)
                            MyWeb.AllowUnsafeUpdates = true;

                        string listErrorName = "RS Error Log";
                        SPList myList = MyWeb.Lists[listErrorName];
                        message = "[RSInitiation] " + wfID + " - " + message;

                        if (myList != null)
                        {
                            SPQuery query = new SPQuery();
                            query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + message + "</Value></Eq></Where>";

                            SPListItemCollection itemCollection = myList.GetItems(query);
                            SPListItem itm = null;

                            if (itemCollection.Count > 0)
                            {
                                itm = itemCollection[0];
                                itm["Title"] = message;
                            }
                            else
                            {
                                itm = myList.Items.Add();
                                itm["Title"] = message;
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

        /// <summary>
        /// Get user login name without domain
        /// </summary>
        /// <param name="userAccount"></param>
        /// <returns>Get user login name without domain. String.</returns>
        private static string GetOnlyUserAccount(string userAccount)
        {
            string account = string.Empty;

            try
            {


                if (userAccount.Contains("\\"))
                    account = userAccount.Split('\\')[1];
                else
                    account = userAccount;

               
            }
            catch (Exception ex)
            {
                SaveErrorsLog("getOnlyUserAccount() - " + ex.Source, ex.Message);
            }

            return account;
        }

        /// <summary>
        /// Decrypt encrypted parameters and data from UTF8.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Decrypted parameters and data</returns>
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

    }
}
