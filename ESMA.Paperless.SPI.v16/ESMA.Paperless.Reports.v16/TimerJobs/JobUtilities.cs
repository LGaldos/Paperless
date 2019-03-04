using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESMA.Paperless.Reports.v16.TimerJobs
{
    class JobUtilities
    {
        /// <summary>
        /// Uncypher?
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Uncypher(string data)
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

        /// <summary>
        /// Get Routing Slip configuration parameters
        /// </summary>
        /// <param name="web"></param>
        /// <returns>String dictionary with all configuration parameters</returns>
        public static Dictionary<string, string> GetConfigurationParameters(SPWeb web)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            try
            {
                SPList list = web.Lists["RS Configuration Parameters"];

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
                //ExceptionRecording("GetConfigurationParameters", ex.Message);
            }

            return parameters;
        }


        /// <summary>
        /// Get workflow type orders    
        /// </summary>
        /// <param name="Web"></param>
        /// <returns>String dictionary with Workflow type orders</returns>
        public static Dictionary<string, string> GetWorkflowTypeOrder(SPWeb Web)
        {
            Dictionary<string, string> wftypes = new Dictionary<string, string>();

            try
            {
                SPList list = Web.Lists["RS Workflow Configuration"];

                if (list != null && list.Fields.ContainsFieldWithStaticName("WFOrder"))
                {
                    foreach (SPListItem item in list.Items)
                    {
                        if (item["WFOrder"] != null)
                            wftypes.Add(item.Title.ToUpper(), item["WFOrder"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return wftypes;
        }
        /// <summary>
        /// Recordnot successful e-mail sending in error log
        /// </summary>
        /// <param name="message"></param>
        /// <param name="Web"></param>
        public static void RecordEmailSending(string message, SPWeb Web)
        {
            try
            {
                if (!string.IsNullOrEmpty(message))
                {
                    SPList errorList = Web.Lists["RS Error Log"];
                    SPListItem item = errorList.Items.Add();
                    item["Title"] = "Notifications " + message;
                    item.Update();
                    errorList.Update();
                }
            }
            catch (Exception ex)
            {
                ExceptionRecording("RecordEmailSending", ex.Message);
            }
        }

        /// <summary>
        /// Record exceptions in system event log
        /// </summary>
        /// <param name="method"></param>
        /// <param name="message"></param>
        public static void ExceptionRecording(string method, string message)
        {
            try
            {
                if (!string.IsNullOrEmpty(message))
                {
                    string sourceName = "Routing Slip Timer Jobs";
                    string logName = "Application";
                    string eventName = method + " - " + message;

                    if (!EventLog.SourceExists(sourceName))
                        EventLog.CreateEventSource(sourceName, logName);

                    EventLog.WriteEntry(sourceName, eventName,
                        EventLogEntryType.Error, 14);
                }
            }
            catch { }
        }

        public static void SaveErrorsLog(SPWeb web, string source, string message)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    SPList errorsList = web.Lists["RS Error Log"];
                    string messageValue = "[RSReportsSendMail] " + source + " - " + message;

                    if (messageValue.Length > 256)
                        messageValue = messageValue.Substring(0, 255);

                    SPQuery query = new SPQuery();
                    query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + messageValue + "</Value></Eq></Where>";

                    SPListItemCollection itemCollection = errorsList.GetItems(query);
                    SPListItem itm = (itemCollection.Count > 0) ? itemCollection[0] : errorsList.Items.Add();

                    itm["Title"] = messageValue;
                    itm["RSQueryLog"] = message;

                    web.AllowUnsafeUpdates = true;
                    itm.Update();
                    web.AllowUnsafeUpdates = false;
                });

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region METHODS FILES
        /// <summary>
        /// Método que borra los ficheros de un directorio
        /// <param name="log">Los de lso administradores</param>
        /// </summary>
        /// <param name="ruta">ruta del directorio</param>
        public static void DeleteFile(string ruta, string fileDelete)
        {
            try
            {
                if (System.IO.Directory.Exists(ruta))
                {
                    string[] files = System.IO.Directory.GetFiles(ruta);
                    string fileName;

                    // Copia los ficheros.
                    foreach (string s in files)
                    {

                        // Obtenemos la dirección del fichero para borrarlo.
                        fileName = System.IO.Path.GetFileName(s);
                        if (fileName == fileDelete)
                        {
                            string sourceFile = System.IO.Path.Combine(ruta, fileName);
                            System.IO.File.Delete(sourceFile);
                            break;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                ExceptionRecording("DeleteFile", ex.Message);

            }

        }

        #endregion
    }
}
