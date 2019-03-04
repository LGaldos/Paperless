using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;

namespace ESMA.Paperless.Design.v16
{
    public class DesignModule
    {

        public static void ApplyMasterModule(SPWeb web, string masterURL, string systemMasterURL)
        {
            try
            {

                Uri masterUri = new Uri(web.Url + masterURL);
                web.CustomMasterUrl = masterUri.AbsolutePath;

                if (!String.IsNullOrEmpty(systemMasterURL))
                {
                    Uri systemMasterUri = new Uri(web.Url + systemMasterURL);
                    web.MasterUrl = systemMasterUri.AbsolutePath;
                }
                web.Update();

            }
            catch (Exception ex)
            {
                //Logger.LogErrorLocal(EventId.Error, CategoryId.Design, ErrorLevel.High, "<Easo.COI.SPI.Design> - MasterPageModule: [" + ex.Message + "] " + ex.InnerException, ex);
            }

        }

        public static void SaveErrorsLog_Design(SPWeb web, string wfid, string message)
        {
            try
            {
                string listErrorName = "RS Error Log";
                SPList myList = web.Lists[listErrorName];

                string _message = "[" + wfid + "][Design] " + message;

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
                    }
                    else
                    {
                        itm = myList.Items.Add();
                        itm["Title"] = _message;
                    }

                    try
                    {
                        itm.Update();
                    }
                    catch { }
                }

            }
            catch
            {

            }
        }
    }
}
