using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using ESMA.Paperless.Reports.v16.RSWorkflowReports;

namespace ESMA.Paperless.Reports.v16.RSWorkflowReportTemplates
{
    class ReportTemplates
    {
        /// <summary>
        /// Get the Report Templates for a user from the "RS Reports Templates" list sorted by creation date
        /// </summary>
        public static SPListItemCollection GetReportTemplates(SPWeb web, SPUser user = null)
        {
            try
            {
                SPList list = web.GetListFromWebPartPageUrl(web.Url + "/Lists/ReportsTemplates/AllItems.aspx");

                if (list != null)
                {
                    SPQuery query = new SPQuery();
                    if (user != null)
                    {
                        query.Query = "<Where><Or><Eq><FieldRef Name='Author' LookupId='True'/><Value Type='Integer'>" + user.ID + "</Value></Eq><Contains><FieldRef Name='RPShareUsers' /><Value Type='Text'>" + Permissions.GetUsernameFromClaim(user.LoginName) + "</Value></Contains></Or></Where>";
                        query.Query += "<OrderBy><FieldRef Name='Created' Ascending='False'></FieldRef></OrderBy>";
                    }
                    return list.GetItems(query);
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GetReportTemplates() - " + ex.Source, ex.Message);
            }
            return null;
        }

        public static SPListItem GetReportTemplate(SPWeb web, int templateID)
        {
            try
            {
                SPList list = web.GetListFromWebPartPageUrl(web.Url + "/Lists/ReportsTemplates/AllItems.aspx");
                return list.GetItemById(templateID);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GetReportTemplate() - " + ex.Source, ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Checks if the user created the report template
        /// </summary>
        public static bool IsTemplateAuthor(SPWeb web, SPListItem templateItem, SPUser user)
        {
            try
            {
                SPFieldUserValue templateAuthor = new SPFieldUserValue(web, templateItem["Author"].ToString());
                return (templateAuthor.User.ID == user.ID);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("IsTemplateAuthor() - " + ex.Source, ex.Message);
                return false;
            }
        }

        public static void DeleteReportTemplate(SPWeb web, int templateID, SPUser user)
        {
            try
            {
                SPListItem template = GetReportTemplate(web, templateID);
                bool isAuthor = IsTemplateAuthor(web, template, user);
                bool currenAllowUnsafeUpdates = web.AllowUnsafeUpdates;
                web.AllowUnsafeUpdates = true;
                if (isAuthor)
                {
                    template.Delete();
                }
                else
                {
                    string[] shareUsers = template["RP Share Users"].ToString().Split(',');
                    shareUsers = shareUsers.Where(val => val != Permissions.GetUsernameFromClaim(user.LoginName)).ToArray();
                    template["RP Share Users"] = String.Join(",", shareUsers);
                    template.Update();
                }
                web.AllowUnsafeUpdates = currenAllowUnsafeUpdates;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("DeleteReportTemplate() - " + ex.Source, ex.Message);
            }
        }


        /// <summary>
        /// Checks if a report template already exist with the same title
        /// </summary>
        public static bool ExistTemplate(SPWeb web, string nameTemplate, string templateID = null)
        {
            try
            {
                SPList listTemplates = web.GetListFromWebPartPageUrl(web.Url + "/Lists/ReportsTemplates/AllItems.aspx");
                SPQuery query = new SPQuery();
                if (templateID == null)
                    query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + nameTemplate + "</Value></Eq></Where>";
                else
                    query.Query = "<Where><And><Eq><FieldRef Name='Title'/><Value Type='Text'>" + nameTemplate + "</Value></Eq><Neq><FieldRef Name='ID'/><Value Type='Number'>" + templateID + "</Value></Neq></And></Where>";

                SPListItemCollection itemCollection = listTemplates.GetItems(query);

                return (itemCollection != null && itemCollection.Count > 0);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("ExistTemplate() - " + ex.Source, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Send notifications to the users shared the report template 
        /// </summary>
        public static void SharedUsersNotify(SPWeb web, string users, string idTemplate, string loggedUser, Dictionary<string, string> parameters)
        {
            string errorMessage = string.Empty;

            try
            {
                if (parameters.ContainsKey("E-mail Report Shared Text") && parameters.ContainsKey("E-mail Report Shared Subject"))
                {
                    if (SPUtility.IsEmailServerSet(web))
                    {
                        string emailSubject = parameters["E-mail Report Shared Subject"];
                        string emailText = parameters["E-mail Report Shared Text"];

                        string[] userNames = users.Split(',');

                        foreach (string userName in userNames)
                        {
                            web.AllowUnsafeUpdates = true;
                            SPUser user = web.EnsureUser(userName);

                            //string linkAccept = "<a href='" + web.CurrentUser.ParentWeb.Url + parameters["Report Templates Page"] + "?templateID=" + idTemplate + "&user=" + user.LoginName + "&action=ok'>Accept</a>";
                            string linkCancel = "<a href='" + web.CurrentUser.ParentWeb.Url + parameters["Report Templates Page"] + "?templateID=" + idTemplate + "&user=" + Permissions.GetUsernameFromClaim(user.LoginName) + "&action=cancel'>Reject</a>";
                            string linkView = "<a href='" + web.CurrentUser.ParentWeb.Url + parameters["Report Templates Page"] + "?templateID=" + idTemplate + "'>View Template</a>";

                            emailText = emailText.Replace("[Created by]", loggedUser);
                            emailText = emailText.Replace("[Reject Template Link]", linkCancel);
                            emailText = emailText.Replace("[View Template Link]", linkView);

                            if (!SPUtility.SendEmail(web, false, false, user.Email, emailSubject, emailText))
                            {
                                errorMessage = ". E-mail not sent to " + user.Name + " (" + user.Email + ").";
                                Methods.SaveErrorsLog("SharedUsersNotify() ", errorMessage);
                            }
                        }
                    }
                    else
                    {
                        errorMessage = "Mail server not configured. Unable to send notifications.";
                        Methods.SaveErrorsLog("SharedUsersNotify() ", errorMessage);
                    }
                }
                else
                {
                    errorMessage = "Report parameters not defined. Unable to send notifications.";
                    Methods.SaveErrorsLog("SharedUsersNotify() ", errorMessage);
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("SharedUsersNotify() " + ex.Source, ex.Message);
            }
        }
    }
}
