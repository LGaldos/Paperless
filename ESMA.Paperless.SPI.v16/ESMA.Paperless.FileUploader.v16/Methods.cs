using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using System.Collections;
using System.Web.UI;
using System.Text.RegularExpressions;
using Microsoft.SharePoint.Utilities;


namespace ESMA.Paperless.FileUploader.v16
{
    class Methods
    {
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
            catch (Exception ex)
            {
                SaveErrorsLog("GetConfigurationParameters() - " + ex.Source, ex.Message);
            }

            return parameters;
        }

        /// <summary>
        /// Get Silverlight visor parameters
        /// </summary>
        /// <param name="Web"></param>
        /// <returns>Configuration parameters string dictionary</returns>
        public static void GetSilverlightVisorParameters(ref StringBuilder initParamms, SPWeb Web, string wfLibraryURL, string wfdoctype, Dictionary<string, string> parameters, string wfid, string wfLibraryName)
        {


            try
            {
                string types = string.Empty;
                types = parameters["Silverlight Visor - AllowTypes"];

                if (types.ToLower().Equals("all"))
                    types = string.Empty;

                initParamms.Append("WebUrl=" + Web.Url);
                initParamms.Append(",LibraryURL=" + wfLibraryURL);
                initParamms.Append(",LibraryName=" + wfLibraryName);
                initParamms.Append(",SubfolderName=" + wfid + "/" + wfdoctype);
                initParamms.Append(",AllowTypes=" + types);
                initParamms.Append(",MaxFileSize=" + parameters["Silverlight Visor - MaxFileSize"]);
                initParamms.Append(",MaxSize=" + parameters["Silverlight Visor - MaxSize"]);
                initParamms.Append(",MaxFiles=" + parameters["Silverlight Visor - MaxFiles"]);
            }
            catch (Exception ex)
            {
                SaveErrorsLog("GetSilverlightVisorParameters() - " + ex.Source, ex.Message);
            }

        }

        /// <summary>
        /// Get workflow type name by worfklow identifier.
        /// </summary>
        /// <param name="wforder"></param>
        /// <param name="Web"></param>
        /// <returns>Workflow type title</returns>
        public static string GetWorkflowTypeName(string wforder, SPWeb Web)
        {
            string wftype = string.Empty;
            string errorMessage = string.Empty;

            try
            {
                SPList list = Web.Lists["RS Workflow Configuration"];

                if (list != null && list.Fields.ContainsFieldWithStaticName("WFOrder"))
                {
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><Eq><FieldRef Name='WFOrder'/><Value Type='Text'>" + wforder + "</Value></Eq></Where>";

                    SPListItemCollection itemCollection = list.GetItems(query);

                    if (itemCollection != null && itemCollection.Count.Equals(1))
                        wftype = itemCollection[0].Title;
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(string.Empty, "GetWorkflowTypeName " + ex.Message);
            }

            return wftype;
        }

        /// <summary>
        /// Get workflow SharePoint list by workflow type name.
        /// </summary>
        /// <param name="wfType"></param>
        /// <param name="Web"></param>
        /// <returns>SPList which stores all workflow documentation by workflow type</returns>
        public static string GetWorkflowLibraryURL(string wfType, SPWeb Web)
        {
            string wfLibraryURL = null;

            try
            {
                SPList list = Web.Lists["RS Workflow Configuration"];

                if (list != null)
                {
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + wfType + "</Value></Eq></Where>";

                    SPListItemCollection itemCollection = list.GetItems(query);

                    if (itemCollection != null && itemCollection.Count.Equals(1))
                    {
                        SPListItem item = itemCollection[0];

                        if (item.Fields.ContainsFieldWithStaticName("WFLibraryURL") && item["WFLibraryURL"] != null)
                            wfLibraryURL = item["WFLibraryURL"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(string.Empty, "GetWorkflowLibraryURL" + ex.Message);
            }
            return wfLibraryURL;
        }

        public static SPList GetWorkflowLibrary(string wfLibraryURL, SPWeb Web)
        {
            SPList list = null;

            try
            {
                list = Web.GetListFromUrl(wfLibraryURL);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(string.Empty, "GetWorkflowLibrary" + ex.Message);
            }
            return list;
        }

        /// <summary>
        /// Log Errors in Error Log SharePoint List
        /// </summary>
        /// <param name="source"></param>
        /// <param name="message"></param>
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

                        if (myList != null)
                        {
                            message = "[RSWFFileUploader '" + userAccount + "'] " + source + " - " + message;

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
    }
}
