using System;
using System.Diagnostics;
using Microsoft.SharePoint;

namespace ESMA.Paperless.DailyProcess.v16
{
    class General
    {
        #region <LOGS>

        //------------------------------------------------------------------
        // Write an error in the Event Viewer
        //------------------------------------------------------------------
        public static void WriteEventLog(string ApplicationName, string ErrMessage, EventLogEntryType EventType, int ErrCode)
        {
            try
            {

                EventLog.WriteEntry(ApplicationName, ErrMessage, EventType, ErrCode, 4);
            }

            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "WriteEventLog() - " + ex.Message.ToString());

            }
        }

        #endregion

        #region <OTHER FUNCTIONS>

        //--------------------------------------------------------------------------------------
        //FUNCTION: Get the value from the <APPSETTINGS> (web.config)
        //--------------------------------------------------------------------------------------
        public static string GetAppSettings(string key)
        {

            return System.Configuration.ConfigurationManager.AppSettings[key].ToString();

        }

        //------------------------------------------------------------------------------------------------
        //FUNCTION: We are going to use this function to decrypt the values of the fields user and password,
        //which they are encrypted in the web.config.
        //-----------------------------------------------------------------------------------------------
        public static string Decrypt(string data)
        {

            System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
            System.Text.Decoder utf8Decode = encoder.GetDecoder();

            byte[] todecode_byte = Convert.FromBase64String(data);
            int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
            char[] decoded_char = new char[charCount];
            utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
            string result = new String(decoded_char);
            return result;
        }

        #endregion

        #region <ERRORS / TRACES>

        public static void SaveErrorsLog(string wfID, string message)
        {
            try
            {

                string inf = string.Empty;
                string urlWeb = General.GetAppSettings("RSSiteURL");

                if (string.IsNullOrEmpty(wfID))
                {

                    inf = "RSDailyProcess() - '" + System.DateTime.Now.ToString() + "'";
                }
                else
                {
                    inf = "RSDailyProcess()  - [" + wfID + "] - '" + System.DateTime.Now.ToString() + "'";
                }

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite colsit = new SPSite(urlWeb))
                    {
                        SPWeb MyWeb = colsit.OpenWeb();

                        if (!MyWeb.AllowUnsafeUpdates)
                            MyWeb.AllowUnsafeUpdates = true;

                        string listErrorName = "RS Error Log";
                        SPList myList = MyWeb.Lists[listErrorName];


                        if (myList != null)
                        {
                            SPQuery query = new SPQuery();
                            //query.Query = "<Where><And><Eq><FieldRef Name='Title'/><Value Type='Text'>" + inf + "</Value></Eq>"
                            //  + "<Eq><FieldRef Name='Message' /><Value Type='Note'>" + message + "</Value></Eq></And></Where>";

                            query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + inf + "</Value></Eq></Where>";

                            SPListItemCollection itemCollection = myList.GetItems(query);
                            SPListItem itm = null;

                            if (itemCollection.Count > 0)
                            {
                                itm = itemCollection[0];
                                itm["Title"] = inf + " - " + message;
                                //itm["Message"] = message;
                            }
                            else
                            {
                                itm = myList.Items.Add();
                                itm["Title"] = inf + " - " + message;
                                //itm["Message"] = message;
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

       

        #endregion
    }
}
