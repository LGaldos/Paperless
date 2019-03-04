using System;
using System.Collections.Generic;
using Microsoft.SharePoint;

namespace ESMA.Paperless.Reports.v16.RSWorkflowReportViewer
{
    class Methods
    {
        #region <ERRORS>

        public static Dictionary<string, string> GetConfigurationParameters(SPWeb Web)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
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
                SaveErrorsLog(string.Empty, "GetConfigurationParameters " + ex.Message);
            }
            return parameters;
        }

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
                        string messageValue = "[RSReportViewer '" + userAccount + "'] " + source + " - " + message;

                        if (messageValue.Length > 256)
                            messageValue = messageValue.Substring(0, 255);


                        if (myList != null)
                        {
                            SPQuery query = new SPQuery();
                            query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + messageValue + "</Value></Eq></Where>";

                            SPListItemCollection itemCollection = myList.GetItems(query);
                            SPListItem itm = null;

                            if (itemCollection.Count > 0)
                            {
                                itm = itemCollection[0];
                                itm["Title"] = messageValue;
                                itm["RSQueryLog"] = message;
                            }
                            else
                            {
                                itm = myList.Items.Add();
                                itm["Title"] = messageValue;
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
            catch (Exception ex)
            {
                SaveErrorsLog(string.Empty, "SaveErrorsLog " + ex.Message);
            }
        }

        #endregion
    }
}
