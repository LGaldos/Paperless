using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using System.Web;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using System.Web.UI;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Net;
using System.IO;
using System.Web.UI.WebControls;


namespace ESMA.Paperless.Webparts.v16.RSWorkflow
{
    public enum RSTemplateMessageType : int
    {
        Personalized = 0,

        AD_Parameters_Empty = 1,

        Permissions_Required = 2,

        Context_Url_No_Parameters = 3,

        Action_Not_Performed = 4
    }

    static class General
    {
        private static string contents = string.Empty;

        #region ConfigurationParameters

        /// <summary>
        /// Get all Routing Slip configuration parameters.
        /// </summary>
        /// <param name="Web"></param>
        /// <returns>String dictionary with all Routing Slip configuration parameters</returns>
        public static Dictionary<string, string> GetConfigurationParameters(SPWeb Web)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            try
            {
                SPList list = Web.Lists["RS Configuration Parameters"];
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
                General.saveErrorsLog(string.Empty, "GetConfigurationParameters " + ex.Message);
            }
            return parameters;
        }

        /// <summary>
        /// Set dinamically any configuration parameter value.
        /// </summary>
        /// <param name="parameterKey"></param>
        /// <param name="parameterValue"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        public static void SetConfigurationParameter(string parameterKey, string parameterValue, SPWeb Web, ref Dictionary<string, string> parameters)
        {
            string errorMessage = string.Empty;
            try
            {
                SPList list = Web.Lists["RS Configuration Parameters"];

                if (list != null)
                {
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + parameterKey + "</Value></Eq></Where>";

                    SPListItemCollection itemCollection = list.GetItems(query);

                    if (itemCollection != null && itemCollection.Count.Equals(1))
                    {
                        SPListItem item = itemCollection[0];
                        if (item.Fields.ContainsFieldWithStaticName("Value1"))
                        {
                            item["Value1"] = parameterValue;

                            using (new DisabledItemEventsScope())
                            {
                                item.Update();
                            }
                        }
                        parameters = GetConfigurationParameters(Web);
                    }
                    else
                        errorMessage = "Ambiguous parameter key.";
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "SetConfigurationParameter " + ex.Message);
            }
        }
        #endregion

        #region ComputerIdentification
        /// <summary>
        /// Get the name of the computer that is processing any workflow processing change.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Name of the host or client which is processing the workflow.</returns>
        public static string GetComputerName(HttpContext context)
        {
            try
            {
                System.Net.IPHostEntry host = new System.Net.IPHostEntry();
                host = System.Net.Dns.GetHostEntry(context.Request.ServerVariables["REMOTE_HOST"]);
                string hName = string.Empty;
                //Split out the host name from the FQDN

                return host.HostName.ToString();
            }
            catch
            {
                return System.Environment.MachineName;
            }
        }
        #endregion

        #region ActiveDirectory

        /// <summary>
        /// Get all active directory users.
        /// </summary>
        /// <param name="domainName"></param>
        /// <param name="groupName"></param>
        /// <param name="userAD">Encrypted administrator user login name</param>
        /// <param name="passwordAD">Encrypted administrator user password</param>
        /// <returns>Get all active directory users in a string directory. Key: SamAccount. Value:Name.</returns>
        public static Dictionary<string, string> GetUsersFromActiveDirectory(string domainName, string groupName, string userAD, string passwordAD, string wfid, bool allowNestedGroups)
        {
            Dictionary<string, string> membersToReturn = new Dictionary<string, string>();

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, domainName, userAD, passwordAD))
                {
                    if (context != null)
                    {
                        GroupPrincipal groupPrincipal = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, groupName);

                        ////ESMA-CR28-Nested Groups
                        foreach (UserPrincipal user in groupPrincipal.GetMembers(allowNestedGroups).OfType<UserPrincipal>())
                        {
                            //Don't display disabled accounts
                            if (user.Enabled != false)
                            {
                                if (!membersToReturn.ContainsKey(user.SamAccountName.ToUpper()))
                                    membersToReturn.Add(user.SamAccountName.ToUpper(), user.Name);
                            }
                        }

                    }
                    else
                    {
                        General.saveErrorsLog("GetUsersFromActiveDirectory() - Problems to connect AD. Group: '", groupName);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetUsersFromActiveDirectory " + ex.Message);
                return null;
            }

            return membersToReturn;
        }

        /// <summary>
        /// Get if user belongs to one of the groups which can change workflow step responsibility.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="userLoginName"></param>
        /// <returns>True if user belongs to any of reassigning groups.</returns>
        public static bool IsMemberOfReassigningGroup(Dictionary<string, string> parameters, string userLoginName, string wfid, string stepNumber, string userAD, string passwordAD, string domain)
        {
            bool enc = false;
            try
            {
                if (parameters.ContainsKey("RS Reassigning Group 1"))
                {
                    enc = Permissions.UserBelongToGroup(domain, parameters["RS Reassigning Group 1"], userLoginName, userAD, passwordAD, wfid, parameters, stepNumber);

                    if (!enc)
                        enc = Permissions.UserBelongToGroup(domain, parameters["RS Reassigning Group 2"], userLoginName, userAD, passwordAD, wfid, parameters, stepNumber);
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "IsMemberOfReassigningGroup " + ex.Message);
            }

            return enc;
        }

        /// <summary>
        /// If user exists in Active Directory.
        /// </summary>
        /// <param name="userAccount"></param>
        /// <param name="domainName"></param>
        /// <param name="WFID"></param>
        /// <param name="userAD">Encrypted administrator user login name</param>
        /// <param name="passwordAD">Encrypted administrator user password</param>
        /// <returns>True if user exists in Active Directory.</returns>
        public static bool ExistUserAD(string userAccount, string domainName, string WFID, string userAD, string passwordAD)
        {
            try
            {
                bool exist = false;

                PrincipalContext context = new PrincipalContext(ContextType.Domain, domainName, userAD, passwordAD);

                if (context != null)
                {
                    UserPrincipal user = UserPrincipal.FindByIdentity(context, userAccount);

                    if ((user != null) && (user.Enabled.Equals(true)))
                        exist = true;
                }
                else
                {
                    exist = true;
                    string message = "Problem with the AD. Not possible to connect. User[Group]: '" + userAccount + "'.";
                    General.saveErrorsLog(WFID, "ExistUserAD() " + message);
                }

                return exist;

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "ExistUserAD() " + ex.Message);
                return false;
            }
        }

        
        public static string GetGroupName(string ADGroupName, Dictionary<string, string> parameters)
        {
            string groupname = string.Empty;

            try
            {
                List<string> keyList = new List<string>(parameters.Keys);

                if (keyList.Contains(ADGroupName.ToLower()))
                    groupname = parameters.FirstOrDefault(x => x.Key == ADGroupName.ToLower()).Value;
                else
                    groupname = ADGroupName.ToLower();
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetGroupName " + ex.Message);
            }

            return groupname;
        }

        /// <summary>
        /// Get user login name without domain
        /// </summary>
        /// <param name="userAccount"></param>
        /// <returns>Get user login name without domain. String.</returns>
        public static string GetOnlyUserAccount(string userAccount)
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
                saveErrorsLog("GetOnlyUserAccount() - " + ex.Source, ex.Message);
                return null;
            }

            return account;
        }

        #endregion

        #region Email

     

        /// <summary>
        /// Process e-mail sending by SharePoint outgoing e-mail services.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="Web"></param>
        /// <param name="wfid"></param>
        /// <param name="subject"></param>
        /// <param name="parameters"></param>
        public static void SendEmail(SPUser user, SPWeb Web, string wfid, string emailSubject, string emailText)
        {
            string errorMessage = string.Empty;

            try
            {
                if (user != null)
                {
                    if (SPUtility.IsEmailServerSet(SPContext.Current.Web))
                    {
                        if (string.IsNullOrEmpty(user.Email))
                            errorMessage = "E-mail not sent. Please, review '" + user.Name + "' e-mail configuration.";
                        else if (!SPUtility.SendEmail(Web, false, false, user.Email, emailSubject, emailText))
                            errorMessage = "Please, check outgoing e-mail service configuration.";
                    }
                    else
                        errorMessage = "Outgoing e-mail service not configured.";
                }

                if (!string.IsNullOrEmpty(errorMessage))
                    General.saveErrorsLog(wfid, errorMessage);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SendEmail " + ex.Message);
            }
        }

        /// <summary>
        /// Send notifications for urgent workflows
        /// </summary>
        /// <param name="web"></param>
        /// <param name="parameters"></param>
        public static void SendUrgentNotification(SPWeb web, string wfid, string wfSubject, SPUser receiver, Dictionary<string, string> parameters)
        {
            try
            {
                string emailSubject = parameters["E-mail Pending Subject Urgent"];
                string emailText = parameters["E-mail Pending Text Urgent"];
                wfSubject = string.IsNullOrEmpty(wfSubject) ? "No subject" : wfSubject;
                string link = HttpContext.Current.Request.Url.AbsoluteUri;


                //Email Subject
                if (emailSubject.Contains("[WF ID]"))
                    emailSubject = emailSubject.Replace("[WF ID]", wfid);

                if (emailSubject.Contains("[WF Subject]"))
                    emailSubject = emailSubject.Replace("[WF Subject]", wfSubject);


                //Email Body
                if (emailText.Contains("[WF ID]"))
                    emailText = emailText.Replace("[WF ID]", wfid);

                if (emailText.Contains("[WF Subject]"))
                    emailText = emailText.Replace("[WF Subject]", wfSubject);

                if (emailText.Contains("[WF Link]"))
                {
                    if (!string.IsNullOrEmpty(link))
                        emailText = emailText.Replace("[WF Link]", String.Format("<a href='{0}'>{1}</a>", link, wfid));
                    else
                        emailText = emailText.Replace("[WF Link]", wfid);
                }

                //WFLink - Not HTML
                if (emailText.Contains("[WF URL]"))
                {
                    if (!string.IsNullOrEmpty(link))
                        emailText = emailText.Replace("[WF URL]", link);
                    else
                        emailText = emailText.Replace("[WF URL]", wfid);
                }

                if (emailText.Contains("[User Name]"))
                    emailText = emailText.Replace("[User Name]", receiver.Name);

                SendEmail(receiver, web, wfid, emailSubject, emailText);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "SendUrgentNotification() - " + ex.Message);
            }
        }

        /// <summary>
        /// Send rejection notification
        /// </summary>
        /// <param name="web"></param>
        /// <param name="parameters"></param>
        public static void SendRejectionNotification(SPWeb web, string wfid, string wfSubject, SPUser receiver, Dictionary<string, string> parameters)
        {
            try
            {
                string emailSubject = parameters["E-mail Rejection Subject"];
                string emailText = parameters["E-mail Rejection Text"];
                wfSubject = string.IsNullOrEmpty(wfSubject) ? "No subject" : wfSubject;
                string link = HttpContext.Current.Request.Url.AbsoluteUri;


                //Email Subject
                if (emailSubject.Contains("[WF ID]"))
                    emailSubject = emailSubject.Replace("[WF ID]", wfid);

                if (emailSubject.Contains("[WF Subject]"))
                    emailSubject = emailSubject.Replace("[WF Subject]", wfSubject);


                //Email Body
                if (emailText.Contains("[WF ID]"))
                    emailText = emailText.Replace("[WF ID]", wfid);

                if (emailText.Contains("[WF Subject]"))
                    emailText = emailText.Replace("[WF Subject]", wfSubject);

                if (emailText.Contains("[WF Link]"))
                {
                    if (!string.IsNullOrEmpty(link))
                        emailText = emailText.Replace("[WF Link]", String.Format("<a href='{0}'>{1}</a>", link, wfid));
                    else
                        emailText = emailText.Replace("[WF Link]", wfid);
                }

                //WFLink - Not HTML
                if (emailText.Contains("[WF URL]"))
                {
                    if (!string.IsNullOrEmpty(link))
                        emailText = emailText.Replace("[WF URL]", link);
                    else
                        emailText = emailText.Replace("[WF URL]", wfid);
                }

                if (emailText.Contains("[User Name]"))
                    emailText = emailText.Replace("[User Name]", receiver.Name);


                SendEmail(receiver, web, wfid, emailSubject, emailText);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SendRejectionNotification " + ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// 
        public static void SendEmailStepManagement(string wfid, string[] notificationsArray, SPListItem item, SPWeb Web, string wfSubject, string emailStepSubject, string emailStepText)
        {
            try
            {
                    
                    string internalNameColumn = notificationsArray[3];
                    SPUser userNotifications = null;

                    SPField field = Web.Fields.TryGetFieldByStaticName(internalNameColumn);
                    SPFieldType fieldType = field.Type;

                    if (fieldType.Equals(SPFieldType.User))
                    {

                        if (item[internalNameColumn] != null)
                        {

                            userNotifications = General.GetSPUser(item, internalNameColumn, wfid, Web);

                            General.SendNotification(Web, wfid, wfSubject, userNotifications, emailStepSubject, emailStepText);
                        }
                    }
  
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SendEmailStepManagement() " + ex.Message);
            }
        }

        public static void SendNotification(SPWeb web, string wfid, string wfSubject, SPUser receiver, string emailSubject, string emailText)
        {
            try
            {
                
                wfSubject = string.IsNullOrEmpty(wfSubject) ? "No subject" : wfSubject;
                string link = HttpContext.Current.Request.Url.AbsoluteUri;


                //Email Subject
                if (emailSubject.Contains("[WF ID]"))
                    emailSubject = emailSubject.Replace("[WF ID]", wfid);

                if (emailSubject.Contains("[WF Subject]"))
                    emailSubject = emailSubject.Replace("[WF Subject]", wfSubject);


                //Email Body
                if (emailText.Contains("[WF ID]"))
                    emailText = emailText.Replace("[WF ID]", wfid);

                if (emailText.Contains("[WF Subject]"))
                    emailText = emailText.Replace("[WF Subject]", wfSubject);

                if (emailText.Contains("[WF Link]"))
                {
                    if (!string.IsNullOrEmpty(link))
                        emailText = emailText.Replace("[WF Link]", String.Format("<a href='{0}'>{1}</a>", link, wfid));
                    else
                        emailText = emailText.Replace("[WF Link]", wfid);
                }

                //WFLink - Not HTML
                if (emailText.Contains("[WF URL]"))
                {
                    if (!string.IsNullOrEmpty(link))
                        emailText = emailText.Replace("[WF URL]", link);
                    else
                        emailText = emailText.Replace("[WF URL]", wfid);
                }

                if (emailText.Contains("[User Name]"))
                    emailText = emailText.Replace("[User Name]", receiver.Name);


                SendEmail(receiver, web, wfid, emailSubject, emailText);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SendStepNotification() " + ex.Message);
            }
        }

        public static void SendEmailGeneralManagement(string wfid, SPListItem item, SPWeb web, string wfSubject, string emailSubject, string emailText, string userAD, string passwordAD, Dictionary<string, string> parameters, Panel DynamicUserListsPanel, SPFieldUserValue receiverGroupValue)
        {
            try
            {
                if (receiverGroupValue != null)
                {
                    General.SendNotification(web, wfid, wfSubject, receiverGroupValue.User, emailSubject, emailText);

                    if (receiverGroupValue.User.IsDomainGroup)
                    {

                        SPUser receiver = ControlManagement.GetEmailReceiverUser(receiverGroupValue.User.Name, web, DynamicUserListsPanel, parameters, item, wfid, userAD, passwordAD);
                        if (receiver != null)
                            General.SendNotification(web, wfid, wfSubject, receiver, emailSubject, emailText);
                    }

                    
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SendEmailGeneralManagement() " + ex.Message);
            }
        }

        #endregion

        #region User Data Treatment

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="responsibleName"></param>
        public static void UpdateUserNameToDeleted(SPWeb Web, ref string responsibleName, string wfid)
        {

            try
            {
                if (!responsibleName.ToLower().Contains("(deleted)"))
                {
                    SPUser userDeleted = Web.EnsureUser(responsibleName);

                    if (userDeleted != null)
                    {
                        responsibleName = responsibleName + " (Deleted)";
                        userDeleted.Name = responsibleName;

                        bool allowUnsafeUpdates = Web.AllowUnsafeUpdates;
                        Web.AllowUnsafeUpdates = true;

                        try
                        {
                            userDeleted.Update();
                        }
                        catch
                        {
                            General.saveErrorsLog(string.Empty, "Error to update the user name to '" + responsibleName + " (deleted).");
                        }

                        Web.AllowUnsafeUpdates = allowUnsafeUpdates;
                    }
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "updateUserNameToDeleted() " + ex.Message);
            }

        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="userName"></param>
        public static void UpdateActiveUserName(SPWeb Web, ref string userName, string wfid)
        {
            try
            {
                if (userName.ToLower().Contains("(deleted)"))
                {
                    SPUser userActive = Web.EnsureUser(userName);

                    if (userActive != null)
                    {
                        userName = userName.Replace("(Deleted)", null);
                        userActive.Name = userName;

                        if (!Web.AllowUnsafeUpdates)
                            Web.AllowUnsafeUpdates = true;

                        try
                        {
                            userActive.Update();
                            General.saveErrorsLog(wfid, "User '" + userName + "'  was catalogued as (Deleted) incorrectly.");
                        }
                        catch
                        {
                            General.saveErrorsLog(string.Empty, "Error to update the active user name to '" + userName + ".");
                        }

                        if (Web.AllowUnsafeUpdates)
                            Web.AllowUnsafeUpdates = false;
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "UpdateActiveUserName() " + ex.Message);
            }
        }

        /// <summary>
        /// Get System Account real SharePoint user profile.
        /// </summary>
        /// <param name="currPage"></param>
        /// <returns>System Account SharePoint SPUser profile</returns>
        public static SPUser GetRealCurrentSPUser(Page currPage)
        {
            if (SPContext.Current.Web.CurrentUser.ToString().ToUpper().Equals("SHAREPOINT\\SYSTEM"))
            {
                try
                {
                    foreach (SPUser user in SPContext.Current.Web.SiteUsers)
                    {
                        if (user.LoginName.Contains(currPage.User.Identity.Name))
                            return user;
                    }
                    return SPContext.Current.Web.Users[currPage.User.Identity.Name];
                }
                catch
                {
                    return SPContext.Current.Web.SiteUsers[currPage.User.Identity.Name];
                }
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
                if (userLoginName.Contains("\\"))
                    userLoginName = userLoginName.Split('\\')[1].ToString();

                if (userName.Contains("\\"))
                    userName = userName.Split('\\')[1].ToString();
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetUserData " + ex.Message);
            }
        }

        /// <summary>
        /// Get workflow step group default actor.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="parameters"></param>
        /// <param name="Web"></param>
        /// <param name="administratorUser"></param>
        /// <param name="wfid"></param>
        /// <param name="userAD">Encrypted administrator user login name</param>
        /// <param name="passwordAD">Encrypted administrator user password</param>
        /// <returns>SPUser SharePoint user of workflow step default user.</returns>
        public static SPUser GetDefaultUserToReassign(string groupName, Dictionary<string, string> parameters, SPWeb Web, SPUser administratorUser, string wfid, string domain, string userAD, string passwordAD)
        {
            SPUser defaultUser = null;
            SPUser defaultUserToReassign = null;
            string defaultAccount = string.Empty;

            try
            {
                string defaultKey = "Default " + groupName;

                if (parameters.ContainsKey(defaultKey))
                {
                    if (!(parameters[defaultKey].Contains(domain)))
                        defaultAccount = parameters[defaultKey];
                    else
                        defaultAccount = parameters[defaultKey].Split('\\')[1].Replace(@"\", "");

                    try
                    {
                        
                            defaultUser = Web.EnsureUser(defaultAccount);

                            if (ExistUserAD(defaultAccount, domain, wfid, userAD, passwordAD))
                                defaultUserToReassign = defaultUser;
                            else
                            {
                                defaultUserToReassign = administratorUser;

                                if (defaultUser != null)
                                    General.saveErrorsLog(wfid, "GetDefaultUserToReassign() - Parameter '" + defaultKey + "' - User: '" + defaultUser.LoginName + "' not exist. Reassigned to administratorUser (" + administratorUser.LoginName + ")");
                                else
                                    General.saveErrorsLog(wfid, "GetDefaultUserToReassign() - Parameter '" + defaultKey + "' - DefaultUser NULL. Reassigning to administratorUser (" + administratorUser.LoginName + ")");
                            }
 
                    }
                    catch
                    {
                        defaultUserToReassign = administratorUser;
                        General.saveErrorsLog(wfid, "GetDefaultUserToReassign() - (EXCEPTION!). Parameter '" + defaultKey + "' - User: '" + defaultUser.LoginName + "' not exist. Reassigned to administratorUser (" + administratorUser.LoginName + ")");

                    }
                   
                }
                else
                {
                    defaultUserToReassign = administratorUser;
                    General.saveErrorsLog(wfid, "GetDefaultUserToReassign() - Parameter '" + defaultKey + "' does not exist in RS Configuration Parameters List. Reassigned to administratorUser (" + administratorUser.LoginName + ")");
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetDefaultUserToReassign() - " + ex.Message);
            }
            return defaultUser;
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="Web"></param>
        /// <returns></returns>
        public static SPUser GetAdministratorUser(Dictionary<string, string> parameters, SPWeb Web, string wfid)
        {
            SPUser administratorUser = null;
            string defaultAdministratorAccount = string.Empty;

            try
            {
                string domain = parameters["Domain"].ToString();

                if (parameters.ContainsKey("RS Default Administrator"))
                {
                    if (parameters["RS Default Administrator"].ToLower().Contains(domain.ToLower()))
                        defaultAdministratorAccount = parameters["RS Default Administrator"];
                    else
                        defaultAdministratorAccount = domain + "\\" + parameters["RS Default Administrator"];

                    administratorUser = Permissions.GetUserWithWindowsClaims(wfid, defaultAdministratorAccount, Web); //Claims (Windows + SAML)
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetAdministratorUser() - " + ex.Message);
            }
            return administratorUser;
        }

        #endregion

        //------------------------------------------------------------------------------------------------
        //FUNCTION: We are going to use this function to decrypt the values of the fields user and password,
        //which they are encrypted in the web.config.
        //-----------------------------------------------------------------------------------------------

        /// <summary>
        /// Decrypt data for its secure treatment.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Decrypted data.</returns>
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
                General.saveErrorsLog(string.Empty, "Decrypt() - " + ex.Message);
            }
            return result;
        }

        public static string ToUpperFirstLetter(string source)
        {
            try
            {

                char[] letters = source.ToCharArray();
                letters[0] = char.ToUpper(letters[0]);
                source = new string(letters);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "ToUpperFirstLetter() - " + ex.Message);
            }

            return source;
        }

        public static SPUser GetAuthor(string wfid, SPListItem item, SPWeb Web)
        {
            SPUser author = null;

            try
            {

                try
                {
                    author = General.GetSPUser(item, "Author", wfid, Web);
                }
                catch
                {
                    author = General.GetSPUser(item, "Step_x0020_1_x0020_Assigned_x0020_To", wfid, Web);
                }

 
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetAuthor() - " + ex.Message);
            }

            return author;
        }

        public static SPUser GetSPUser(SPListItem item, string fieldName, string wfid, SPWeb web)
        {
            try
            {
               

                SPFieldUser field = item.Fields.GetFieldByInternalName(fieldName) as SPFieldUser;

                if (field != null && item[fieldName] != null)
                {
                    SPFieldUserValue fieldValue = field.GetFieldValue(item[fieldName].ToString()) as SPFieldUserValue;

                    if (fieldValue != null)
                        return fieldValue.User;
                }
                else
                {
                    General.saveErrorsLog(wfid, "GetSPUser() - Column '" + fieldName + "' was not found. URL: " + item.Url);
                    return null;
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetSPUser() - " + ex.Message);
            }
            return null;

        }

        public static bool IsDictionaryEmpty(string wfid, Dictionary<string, string> dictionary)
        {
            bool isEmpty = true;

            try
            {
                try
                {
                    if (!(dictionary.Count == 0))
                        isEmpty = false;
                }
                catch
                {
                    if (dictionary != null)
                        isEmpty = false;
                }
                
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "IsDictionaryEmpty() - " + ex.Message);
            }

            return isEmpty;
        }

        public static SPUser GetSPUserObject(SPListItem spListItem, String fieldName, string wfid, SPWeb Web)
        {
            SPUser spUser = null;
            try
            {
                if (fieldName != string.Empty)
                {

                    SPFieldUser field = null;
                    
                    if (!fieldName.ToLower().Equals("editor"))
                        field = spListItem.Fields.GetFieldByInternalName(fieldName) as SPFieldUser;
                    else
                        field = spListItem.Fields["Editor"] as SPFieldUser;


                    if (field != null && spListItem.Fields.GetFieldByInternalName(fieldName) != null)
                        spUser = General.GetSPUser(spListItem, fieldName, wfid, Web);
                        
                    
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetSPUserObject() - " + ex.Message);
            }
            return spUser;
        }


        #region <ERRORS>

        /// <summary>
        /// Log any Routing Slip error.
        /// </summary>
        /// <param name="wfid"></param>
        /// <param name="message"></param>
        public static void saveErrorsLog(string wfid, string message)
        {
            try
            {
                if (SPContext.Current != null)
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

                            string _message = "[" + wfid + "] " + message;

                            if ((!string.IsNullOrEmpty(_message)) && (_message.Length > 128))
                                _message = _message.Substring(0, 127);

                            if (myList != null)
                            {
                                SPQuery query = new SPQuery();
                                query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + _message + "</Value></Eq></Where>";
                                query.ViewFields = string.Concat(
                                  "<FieldRef Name='Title' />");
                                query.ViewFieldsOnly = true; // Fetch only the data that we need

                                SPListItemCollection itemCollection = myList.GetItems(query);
                                SPListItem itm = null;

                                if (itemCollection.Count > 0)
                                {
                                    itm = itemCollection[0];
                                    itm["Title"] = _message;
                                    itm["RSQueryLog"] = message;
                                }
                                else
                                {
                                    itm = myList.Items.Add();
                                    itm["Title"] = _message;
                                    itm["RSQueryLog"] = message;
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
            }
            catch
            {

            }
        }


        public static void ShowErrorTemplate(RSTemplateMessageType templateType, [Optional][DefaultValue("An Error has ocurred, we are sorry to much !!!")]string mess)
        {

            try
            {
                contents = GetErrorTemplateFromLayoutFolder();
                contents = ApplyChangesInContentTemplate(contents, templateType, mess);
            }
            catch (Exception ex)
            {
                contents = contents = contents.Replace("[xxxxxxxxxx]", ex.Message);
            }

            HttpContext.Current.Response.Clear();
            HttpContext.Current.Response.Write(contents);
            HttpContext.Current.Response.Flush();
            HttpContext.Current.Response.End();
        }

        private static string ApplyChangesInContentTemplate(string contents, RSTemplateMessageType templateType, string mess)
        {
            HttpRequest req = HttpContext.Current.Request;

            string content = contents;
            switch (templateType)
            {
                case RSTemplateMessageType.Personalized:
                    content = content.Replace("[xxxxxxxxxx]", mess);
                    break;

                case RSTemplateMessageType.AD_Parameters_Empty:
                    content = content.Replace("[xxxxxxxxxx]", "The 'AD User' or 'AD Password' parameters are empty.");
                    break;

                case RSTemplateMessageType.Permissions_Required:
                    content = content.Replace("[xxxxxxxxxx]", "You don’t have access to this workflow.");
                    break;

                case RSTemplateMessageType.Context_Url_No_Parameters:
                    content = content.Replace("[xxxxxxxxxx]", "The current context URL has no parameters or its parameters are not the correct ones.");
                    break;

                case RSTemplateMessageType.Action_Not_Performed:
                    content = content.Replace("[xxxxxxxxxx]", "This action cannot be performed.");
                    break;

                default:
                    break;
            }

            content = content.Replace("[yyyyyyyyyy]", req.UrlReferrer.ToString());

            return content;
        }

        private static string GetErrorTemplateFromLayoutFolder()
        {
            string content = string.Empty;
            try
            {
                //string urlContext = SPContext.Current.Site.Url;

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    WebRequest request = WebRequest.Create(SPContext.Current.Site.Url + "/_layouts/15/ESMA.Paperless.Design.v16/RsErrorTemplates/message.htm");
                    
                    request.Credentials = CredentialCache.DefaultCredentials;
                    
                    WebResponse response = request.GetResponse();
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        content = reader.ReadToEnd();
                    }
                    response.Close();
                });
            }
            catch (Exception ex)
            {
                saveErrorsLog(string.Empty, "GetErrorTemplateFromLayoutFolder() " + ex.Message);
            }

            return content.Trim();
        }
        #endregion

        public class Controles
        {
            #region FINBD CONTROLS ON PAGE - WEBPART - CONTROL

            public static T FindControlRecursive<T>(Control control, string controlID) where T : Control
            {
                // Find the control.
                if (control != null)
                {
                    Control foundControl = control.FindControl(controlID);
                    if (foundControl != null)
                    {
                        // Return the Control
                        return foundControl as T;
                    }
                    // Continue the search
                    foreach (Control c in control.Controls)
                    {
                        foundControl = FindControlRecursive<T>(c, controlID);
                        if (foundControl != null)
                        {
                            // Return the Control
                            return foundControl as T;
                        }
                    }
                }
                return null;
            }

            #endregion
        }
    }
}
