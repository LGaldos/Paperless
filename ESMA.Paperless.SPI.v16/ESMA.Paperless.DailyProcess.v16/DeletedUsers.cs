using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using Microsoft.SharePoint;

namespace ESMA.Paperless.DailyProcess.v16
{
    class DeletedUsers
    {
        public static void CheckDeletedUsers(SPWeb Web, Dictionary<string, string> parameters)
        {
            try
            {
                if (parameters.ContainsKey("AD User") && parameters.ContainsKey("AD Password") && parameters.ContainsKey("Domain"))
                {
                    string domain = parameters["Domain"];
                    string userAD = General.Decrypt(parameters["AD User"]);
                    string passwordAD = General.Decrypt(parameters["AD Password"]);

                    PrincipalContext context = new PrincipalContext(ContextType.Domain, domain, userAD, passwordAD);

                    if (context != null)
                        ReadAllUsers(Web, context);
                    else
                    {
                        string message = "Problem with the AD. Not possible to connect -> GetContext.";
                        General.SaveErrorsLog(null, message);
                    }

                }
                else
                {
                    string message = "The 'AD User' or 'AD Password' or 'Domain' parameters are empty.";
                    General.SaveErrorsLog(null, message);
                }

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "CheckDeletedUsers() - " + ex.Message.ToString());
            }

        }

        private static void ReadAllUsers(SPWeb Web, PrincipalContext context)
        {
            try
            {
                foreach (SPUser oUser in Web.Users)
                {
                    string userAccount = oUser.LoginName.ToString();
                    string userName = oUser.Name.ToString();

                    if (!userName.ToLower().Contains("(deleted)"))
                    {
                        GetUserData(ref userAccount);

                        if (!string.IsNullOrEmpty(userAccount))
                        {
                            if ((ExistUserAD(userAccount, context) == false) && (oUser.IsSiteAdmin == false) && (!userAccount.ToLower().Contains("system")) && (IsGroupAD(userAccount, context) == false))
                            {
                                UpdateUserNameToDeleted(Web, userAccount, oUser);
                                General.SaveErrorsLog(null, "The user '" + userAccount + "' has been deleted from Paperless System.");
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "ReadAllUsers() - " + ex.Message.ToString());
            }
        }

        //TBE
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="userAccount"></param>
        private static void UpdateUserNameToDeleted(SPWeb Web, string userAccount, SPUser userDeleted)
        {

            try
            {
                if (!userAccount.ToLower().Contains("(deleted)") && userDeleted != null)
                {
                    userAccount = userAccount + " (Deleted)";
                    userDeleted.Name = userAccount;

                    bool unsafeUpdates = Web.AllowUnsafeUpdates;
                    Web.AllowUnsafeUpdates = true;

                    try
                    {
                        userDeleted.Update();
                        Web.Update();
                    }
                    catch
                    {
                        General.SaveErrorsLog(null, "Error to update the user name to '" + userAccount + " (deleted).");
                    }

                    Web.AllowUnsafeUpdates = unsafeUpdates;
                }

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "UpdateUserNameToDeleted() " + ex.Message);
            }

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
        private static bool ExistUserAD(string userAccount, PrincipalContext context)
        {
            bool exist = false;

            try
            {

                    //UserPrincipal user = UserPrincipal.FindByIdentity(context, userAccount);
                    UserPrincipal user = new UserPrincipal(context);
                    user.SamAccountName = userAccount;
                    PrincipalSearcher searcher = new PrincipalSearcher(user);
                    user = searcher.FindOne() as UserPrincipal;

                    if (user != null)
                        exist = true;
            }
            catch (Exception ex)
            {
                string message = "Problem with the AD. Not possible to connect. User[Group]: '" + userAccount + "'.";
                General.SaveErrorsLog(null, "ExistUserAD() - " + message);
                General.SaveErrorsLog(null, "ExistUserAD() " + ex.Message);
            }

            return exist;
        }

        private static bool IsGroupAD(string groupAccount, PrincipalContext context)
        {
            bool isGroup = false;

            try
            {
                    //GroupPrincipal group = GroupPrincipal.FindByIdentity(context, groupAccount);
                    GroupPrincipal group = new GroupPrincipal(context);
                    group.SamAccountName = groupAccount;
                    PrincipalSearcher searcher = new PrincipalSearcher(group);
                    group = searcher.FindOne() as GroupPrincipal;


                    if (group != null)
                        isGroup = true;
 
            }
            catch (Exception ex)
            {
                string message = "Problem with the AD. Not possible to connect. Group: '" + groupAccount + "'.";
                General.SaveErrorsLog(null, "IsNotGroupAD() - " + message);
                 General.SaveErrorsLog(null, "IsNotGroupAD() " + ex.Message);
            }

            return isGroup;
        }

        /// <summary>
        /// Ger user login name and name without domain info.
        /// </summary>
        /// <param name="userLoginName"></param>
        /// <param name="userName"></param>
        private static void GetUserData(ref string userLoginName)
        {
            try
            {
                if (userLoginName.Contains("\\"))
                    userLoginName = userLoginName.Split('\\')[1].ToString();
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "GetUserData " + ex.Message);
            }
        }
    }
}
