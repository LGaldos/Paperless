using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;

namespace ESMA.Paperless.EventsReceiver.v16
{
    public class General
    {
        /// <summary>
        /// Get all configuration parameters 
        /// </summary>
        /// <param name="Web"></param>
        /// <returns>String dictionary with all configuration parameters</returns>
        public static Dictionary<string, string> GetConfigurationParameters(SPWeb Web)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            try
            {
                SPList list = Web.Lists["RS Configuration Parameters"];

                foreach (SPListItem item in list.Items)
                {
                    try
                    {
                        if (item.Fields.ContainsFieldWithStaticName("Value1") && item["Value1"] != null)
                            parameters.Add(item.Title, item["Value1"].ToString().Trim());
                    }
                    catch { continue; }
                }
            }
            catch
            {
            }
            return parameters;
        }

        public static void SaveErrorsLogArchitecture(string source, string message)
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
                        string messageValue = "[EventsReceiver] " + source + " - " + message;

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
            catch
            {

            }
        }

        

    }
}
