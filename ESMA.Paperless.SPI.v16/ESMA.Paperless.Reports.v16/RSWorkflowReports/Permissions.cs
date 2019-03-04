using System;
using System.Web.UI;
using Microsoft.SharePoint;
using System.DirectoryServices.AccountManagement;
using Microsoft.SharePoint.Administration.Claims;
using System.Collections.Generic;
using System.Linq;

namespace ESMA.Paperless.Reports.v16.RSWorkflowReports
{
    class Permissions
    {
        #region <PERMISSIONS>

        public static SPUser GetRealCurrentSpUser(Page currPage)
        {
            if (SPContext.Current.Web.CurrentUser.ToString().ToUpper().Equals("SHAREPOINT\\SYSTEM"))
            {
                foreach (SPUser user in SPContext.Current.Web.SiteUsers)
                {
                    if (user.LoginName.Contains(currPage.User.Identity.Name))
                        return user;
                }
            }

            return SPContext.Current.Web.CurrentUser;
        }

        /// <summary>
        /// Get user login name without domain
        /// </summary>
        /// <param name="userAccount"></param>
        /// <returns>Get user login name without domain. String.</returns>
        private static string GetOnlyUserAccount(string userAccount)
        {
            try
            {
                string account = string.Empty;

                if (userAccount.Contains("\\"))
                    account = userAccount.Split('\\')[1];
                else
                    account = userAccount;

                return account;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" getOnlyUserAccount() - " + ex.Source, ex.Message);
                return null;
            }
        }

        public static bool UserBelongToGroup(string domainName, string groupName, string loginName, string userAD, string passwordAD)
        {
            bool belong = false;

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, domainName, userAD, passwordAD))
                {
                    if (context != null)
                    {
                        UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(context, GetOnlyUserAccount(loginName));
                        belong = userPrincipal.IsMemberOf(context, IdentityType.SamAccountName, GetOnlyUserAccount(groupName));
                    }
                    else
                    {
                        Methods.SaveErrorsLog(" UserBelongToGroup() - Problems to connect AD. User:", loginName);
                    }
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" UserBelongToGroup('" + groupName + "') " + ex.Source, ex.Message);
            }

            return belong;
        }

        public static bool UserBelongToGroup(string groupName, string loginName, Dictionary<string, string> parameters)
        {
            bool belong = false;

            if (!String.IsNullOrEmpty(groupName) && parameters != null && parameters.ContainsKey("AD User") && parameters.ContainsKey("AD Password") && parameters.ContainsKey("Domain"))
            {
                string userAD = Methods.Decrypt(parameters["AD User"]);
                string passwordAD = Methods.Decrypt(parameters["AD Password"]);
                string domainName = parameters["Domain"];

                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, domainName, userAD, passwordAD))
                    {
                        if (context != null)
                        {
                            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(context, Methods.RemoveDomain(loginName));
                            belong = userPrincipal.IsMemberOf(context, IdentityType.SamAccountName, Methods.RemoveDomain(groupName));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Methods.SaveErrorsLog(" UserBelongToGroup('" + groupName + "') " + ex.Source, ex.Message);
                }

            }

            return belong;
        }

        /// <summary>
        /// Return the normal domain/username without any claims identification data
        /// </summary>
        public static string GetUsernameFromClaim(string claimsEncodedUsername)
        {
            try
            {
                SPClaimProviderManager spClaimProviderManager = SPClaimProviderManager.Local;
                if (!String.IsNullOrEmpty(claimsEncodedUsername) && spClaimProviderManager != null)
                {
                    if (SPClaimProviderManager.IsEncodedClaim(claimsEncodedUsername))
                    {
                        return spClaimProviderManager.ConvertClaimToIdentifier(claimsEncodedUsername);
                    }
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" GetUsernameFromClaim() - " + claimsEncodedUsername + " - " + ex.Source, ex.Message);
                return claimsEncodedUsername;
            }

            // Return the original username value if it couldn't be resolved as a claims username
            return claimsEncodedUsername;
        }

        public static string GetSelectedUsersInPeoplePicker(string commaSeparatedAccounts)
        {
            string pickerUsers = String.Empty;

            if (!String.IsNullOrEmpty(commaSeparatedAccounts))
            {
                string[] accounts = commaSeparatedAccounts.Split(',');
                for (int i = 0; i < accounts.Length; i++)
                {
                    accounts[i] = GetUsernameFromClaim(accounts[i]);
                }
                pickerUsers = String.Join(",", accounts);
            }

            return pickerUsers;
        }

        public static string GetUserDisplayName(SPWeb web, string loginName)
        {
            try
            {
                web.AllowUnsafeUpdates = true;
                SPUser user = web.EnsureUser(loginName);
                return user.Name;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" GetUserDisplayName('" + loginName + "') " + ex.Source, ex.Message);
                return String.Empty;
            }
        }

        public static string GetDisplayNamesInPeoplePicker(SPWeb web, string commaSeparatedAccounts)
        {
            string[] accounts = commaSeparatedAccounts.Split(',');
            for (int i = 0; i < accounts.Length; i++)
            {
                accounts[i] = GetUserDisplayName(web, accounts[i]);
            }
            return String.Join(",", accounts);
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
                Methods.SaveErrorsLog(string.Empty, "GetUserAccountFromActorSelected(): " + ex.Message);
            }

            return userAccount;
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
                Methods.SaveErrorsLog(string.Empty, "GetUserData - '" + userLoginName + "' " + ex.Message);
            }
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
                Methods.SaveErrorsLog(string.Empty, "GetADGroupName: " + ex.Message);
            }

            return groupname;
        }

        #endregion
    }
}
