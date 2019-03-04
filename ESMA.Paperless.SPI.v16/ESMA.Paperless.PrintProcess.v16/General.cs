using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using System.ComponentModel;

using System.Configuration;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;


//-----------------IMPERSONACION-------------------//

using System.Security.Principal;        // Needed for Impersonation
using Microsoft.Win32;                  // Needed for access to the Registry
using System.Diagnostics;
//------------------------------------------------------------------------

namespace ESMA.Paperless.PrintProcess.v16
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
                General.SaveErrorsLog(string.Empty, "WriteEventLog() - " + ex.Message.ToString());
               
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

        public static bool IsValidExtension(string titleDocument, string WFID, string[] documentsExtensionList)
        {
              bool isValid = false;

            try
            {
                

                for (int i = 0; i < documentsExtensionList.Length; i++)
                {
                    if (titleDocument.ToLower().Contains(documentsExtensionList[i]))
                    {
                        isValid = true;
                        break;
                    }
                }

            }
            catch (Exception ex)
            {
                SaveErrorsLog(WFID, "IsValidExtension() - " + ex.Message.ToString());
            }

           return isValid;
        }

        //http://augusto:2022/paperless2/COMMITMENT%20OF%20CATERING%20%20SUPPLIES%20PURCHASE%20ORDERS%20L/Forms/AllItems.aspx -> /http://augusto:2022/paperless2/COMMITMENT%20OF%20CATERING%20%20SUPPLIES%20PURCHASE%20ORDERS%20L
        public static string GetDocumentLibraryURL(string defaultViewURL)
        {
            string urlLibrary = string.Empty;

            try
            {
               
                if (defaultViewURL.Contains("/Forms/AllItems.aspx"))
                    urlLibrary = defaultViewURL.Replace("/Forms/AllItems.aspx", null);
                else
                    urlLibrary = defaultViewURL;
            }
            catch (Exception ex)
            {
                SaveErrorsLog(null, "GetDocumentLibraryURL() - " + ex.Message.ToString());
            }
            return urlLibrary;
        }

        public static string GetWFURL(string WFID, SPList myList)
        {
            string urlWF = string.Empty;

            try
            {
                string urlList = General.GetDocumentLibraryURL(myList.DefaultViewUrl);
                urlWF = CombineURL(WFID, urlList, WFID);
            }
            catch (Exception ex)
            {
                SaveErrorsLog(null, "GetWFURL() - " + ex.Message.ToString());
            }

            return urlWF;
        }

        private static bool HasInvalidCharacterListName(string listName, string WFID)
        {
            bool invalid = false;

            try
            {
                string[] listValues = new string[17] { "\\", "/", ":", "*", "?", "\"", "<", ">", "|", "{", "}", "#", "%", "~", "&amp;", "&", "." };
                string character = string.Empty;

                for (int i = 0; i < listValues.Length; i++)
                {
                    character = listValues[i].ToString();

                    if (listName.Contains(character))
                    {
                        invalid = true;
                        break;

                    }
                }
            }
            catch (Exception ex)
            {
                SaveErrorsLog(WFID, "HasInvalidCharacter_ListName() - " + ex.Message.ToString());
            }

            return invalid;
        }

        private static string ReplaceInvalidCharacterListName(string listName, string WFID)
        {
            string finalName = string.Empty;

            try
            {

                string[] listValues = new string[17] { "\\", "/", ":", "*", "?", "\"", "<", ">", "|", "{", "}", "#", "%", "~", "&amp;", "&", "." };
                string character = string.Empty;
                string listNameReplaced = listName;
                bool modified = false;
                
              

                if (listNameReplaced.Contains(".."))
                {
                    listNameReplaced = listNameReplaced.Replace("..", ".");
                    modified = true;
                }

                for (int i = 0; i < listValues.Length; i++)
                {
                    character = listValues[i].ToString();

                    if (listNameReplaced.Contains(character))
                    {

                        #region <RULES>

                        if (listNameReplaced.StartsWith("~") || listNameReplaced.Contains("~"))
                        {
                            listNameReplaced = listNameReplaced.Replace("~", "_");
                            modified = true;
                        }

                        if ((listNameReplaced.Contains("&amp;")) || (listNameReplaced.Contains("&")))
                        {
                            if (listNameReplaced.Contains("&amp;"))
                                listNameReplaced = listNameReplaced.Replace("&amp;", "and");
                            else
                                listNameReplaced = listNameReplaced.Replace("&", "and");
                            

                            modified = true;
                        }



                        if (listNameReplaced.Contains("#"))
                        {
                            listNameReplaced = listNameReplaced.Replace("#", null);
                            modified = true;
                        }

                        if (listNameReplaced.Contains("/"))
                        {
                            listNameReplaced = listNameReplaced.Replace("/", "-");
                            modified = true;
                        }

                        if (listNameReplaced.Contains("\\"))
                        {
                            listNameReplaced = listNameReplaced.Replace("\\", "-");
                            modified = true;
                        }

                        if (listNameReplaced.Contains(":"))
                        {
                            listNameReplaced = listNameReplaced.Replace(":", "-");
                            modified = true;
                        }

                        if (listNameReplaced.Contains("*"))
                        {
                            listNameReplaced = listNameReplaced.Replace("*", "-");
                            modified = true;
                        }

                        if (listNameReplaced.Contains("|"))
                        {
                            listNameReplaced = listNameReplaced.Replace("|", "-");
                            modified = true;
                        }

                        if (listNameReplaced.Contains("?"))
                        {
                            listNameReplaced = listNameReplaced.Replace("?", null);
                            modified = true;
                        }

                        if ((listNameReplaced.Contains("[")) || (listNameReplaced.Contains("]")))
                        {
                            if (listNameReplaced.Contains("["))
                                listNameReplaced = listNameReplaced.Replace("[", "(");
                            else
                                listNameReplaced = listNameReplaced.Replace("]", ")");
                            

                            modified = true;
                        }

                        if ((listNameReplaced.Contains("<")) || (listNameReplaced.Contains(">")))
                        {
                            if (listNameReplaced.Contains("<"))
                                listNameReplaced = listNameReplaced.Replace("<", "(");
                            else
                                listNameReplaced = listNameReplaced.Replace(">", ")");
                            

                            modified = true;
                        }



                        if ((listNameReplaced.Contains("{")) || (listNameReplaced.Contains("}")))
                        {
                            if (listNameReplaced.Contains("{"))
                                listNameReplaced = listNameReplaced.Replace("{", "(");
                            
                            if (listNameReplaced.Contains("}"))
                                listNameReplaced = listNameReplaced.Replace("}", ")");
                            

                            modified = true;
                        }


                        if (listNameReplaced.Contains("\""))
                        {
                            listNameReplaced = listNameReplaced.Replace("\"", "'");
                            modified = true;
                        }

                        if (listNameReplaced.Contains("%"))
                        {
                            listNameReplaced = listNameReplaced.Replace("%", null);
                            modified = true;
                        }


                        #endregion

                    }
                }

                if (modified == true)
                    finalName = listNameReplaced;
                else
                    finalName = listName;
                
            }
            catch (Exception ex)
            {
                SaveErrorsLog(WFID, "ReplaceInvalidCharacterListName() - " + ex.Message.ToString());
            }


            return finalName.Trim();
        }

        public static string GeneratePrintDocumentName(string printedDocumentName, string WFID)
        {

            try
            {
                if (HasInvalidCharacterListName(printedDocumentName, WFID) == true)
                    printedDocumentName = ReplaceInvalidCharacterListName(printedDocumentName, WFID);
                

            }
            catch (Exception ex)
            {
                SaveErrorsLog(WFID, "GeneratePrintDocumentName() - " + ex.Message.ToString());
            }

            return printedDocumentName;
        }

        public static void GenerateWFDirectory(string WFID, string WFIDPath)
        {
            try
            {
                //"C:\temp\RSPrintedDocuments
                if (!(System.IO.Directory.Exists(WFIDPath)))
                    System.IO.Directory.CreateDirectory(WFIDPath);
               
            }
            catch (Exception ex)
            {
                SaveErrorsLog(WFID, "GenerateWFDirectory() - " + ex.Message.ToString());
            }

        }     

        public static string FormatCheckBoxValue(string WFID, string value)
        {
            try
            {

                if (value.ToLower() == "false")
                    value = "no";
                else
                    value = "yes";
                

            }
            catch (Exception ex)
            {
                SaveErrorsLog(WFID, "FormatCheckBoxValue() - " + ex.Message.ToString());
            }

            return value;
        }

        public static string FormatDateValue(string WFID, string value)
        {
            try
            {
                if (!string.IsNullOrEmpty(value))
                {
                    DateTime date = Convert.ToDateTime(value);
                    value = date.ToString("dd-MM-yyyy");
                }

            }
            catch (Exception ex)
            {
                SaveErrorsLog(WFID, "FormatDateValue() - " + ex.Message.ToString());
            }

            return value;
        }

        public static string FormatDateTimeValue(string WFID, string value)
        {
            try
            {
                if (!string.IsNullOrEmpty(value))
                {
                    DateTime date = Convert.ToDateTime(value);
                    value = date.Day + "/" + date.Month + "/" + date.Year + " " + date.TimeOfDay.ToString();
                }
             
            }
            catch (Exception ex)
            {
                SaveErrorsLog(WFID, "FormatDateTimeValue() - " + ex.Message.ToString());
            }

            return value;

        }

        public static string FormatHTML(string input, string WFID)
        {
            try
            {
                return Regex.Replace(input, "<.*?>", String.Empty);
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "StripHTML() " + ex.Message);
                return input;
            }

        }

        //269;#Vanessa Roiseux -> Vanessa Roiseux
        //------------------------------------------------
        public static string FormatUserValue(string WFID, string value)
        {

            try
            {

                if (value.Contains("#"))
                {
                    string[] inf = value.Split('#');
                    value = inf[1];
                }  
            }
            catch (Exception ex)
            {
                SaveErrorsLog(WFID, "FormatUserValue() - " + ex.Message.ToString());
            }

            return value;
        }

        public static string CombineURL(string WFID, string urlPath1, string urlPath2)
        {
            string url = string.Empty;

            try
            {
                url = urlPath1 + "/" + urlPath2;
                return url;
            }
            catch (Exception ex)
            {
                SaveErrorsLog(WFID, "CombineURL() - " + ex.Message.ToString());
                return "/";
            }
        }

        //AssignedPerson[User] -> AssignedPerson
        public static string GetColumnNameOnly(string WFID, string value)
        {
            string columnName = string.Empty;

            try
            {
                
                if (value.Contains("["))
                    columnName = value.Substring(0, (value.IndexOf("["))).TrimEnd();

            }
            catch (Exception ex)
            {
                SaveErrorsLog(WFID, "CombineURL() - " + ex.Message.ToString());
            }

            return columnName;
        }

        //AssignedPerson[User] -> User
        public static string GetColumnTypeOnly(string WFID, string value)
        {
            string columnType = string.Empty;

            try
            {
             
                if (value.Contains("["))
                    columnType = value.Substring((value.IndexOf("[") + 1), (value.Length - (value.IndexOf("[") + 1)) );
                

                if (columnType.Contains("]"))
                    columnType = columnType.Replace("]", null);
                
            }
            catch (Exception ex)
            {
                SaveErrorsLog(WFID, "CombineURL() - " + ex.Message.ToString());
            }

            return columnType;
        }

        public static string FormatComment(string WFID, string value)
        {
            try
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (value.Contains("<DIV>"))
                        value = value.Replace("<DIV>", null).Trim();
                    

                    if (value.Contains("</DIV>"))
                        value = value.Replace("</DIV>", null).Trim();
                    
                }
            }
            catch (Exception ex)
            {
                SaveErrorsLog(WFID, "FormatComment() - " + ex.Message.ToString());
            }

            return value;

        }

        public static string GetGroupADEquivalence(string WFID, string value)
        {
            string equivalence = string.Empty;

            try
            {
                
                try
                {
                    equivalence = General.GetAppSettings(value).ToUpper();
                }
                catch
                {
                    equivalence = value.ToUpper();
                }

                
            }
            catch (Exception ex)
            {
                SaveErrorsLog(WFID, "GetGroupADEquivalence() - " + ex.Message.ToString());
            }

            return equivalence;
        }

        /// <summary>
        /// Get all Routing Slip configuration parameters.
        /// </summary>
        /// <param name="web"></param>
        /// <returns>String dictionary with all Routing Slip configuration parameters</returns>
        public static Dictionary<string, string> GetConfigurationParameters(SPWeb web)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            try
            {
                SPList WFConfigurationList = web.GetListFromWebPartPageUrl(web.Url + "/Lists/WFConfigParameters/AllItems.aspx");

                foreach (SPListItem item in WFConfigurationList.Items)
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
                SaveErrorsLog(string.Empty, "GetConfigurationParameters() - " + ex.Message.ToString());
            }
            return parameters;
        }

        public static void CreateRSPrintingDirectory(string pathDirectory)
        {
            try
            {
                if (!System.IO.Directory.Exists(pathDirectory))
                    System.IO.Directory.CreateDirectory(pathDirectory);
                else if (System.IO.Directory.GetDirectories(pathDirectory).Length > 0)
                {
                    //Delete Dir
                    //AttachDocuments.DeleteDirectory(pathDirectory);

                    try
                    {
                        System.IO.Directory.Delete(pathDirectory, true);
                    }
                    catch 
                    {
                        System.IO.Directory.Delete(pathDirectory, true);
                    }

                    System.IO.Directory.CreateDirectory(pathDirectory);
                }
            }
            catch (Exception ex)
            {
                SaveErrorsLog(string.Empty, "CreateRSPrintingDirectory() - " + ex.Message.ToString());
            }

        }

        public static void DeleteRSPrintingDirectory(string pathDirectory)
        {
            try
            {
                try
                {
                    System.IO.Directory.Delete(pathDirectory, true);
                }
                catch
                {
                    System.IO.Directory.Delete(pathDirectory, true);
                }
            }
            catch (Exception ex)
            {
                SaveErrorsLog(string.Empty, "DeleteRSPrintingDirectory() - " + ex.Message.ToString());
            }
        }

        #endregion

        #region <IMPERSONATION>

        //------------------------------------------------------------------------------------------------
        //FUNCTION: We are going to get the values of User, Password and Domain from the web.config. 
        //We are going to use these values for the impersonation.
        //-----------------------------------------------------------------------------------------------
        public static string[] GetConfigurationParameters(Dictionary<string, string> parameters)
        {
            try
            {


                string[] _params = new string[3];

                if (parameters.ContainsKey("AD User") && parameters.ContainsKey("AD Password") && parameters.ContainsKey("Domain"))
                {

                    string strDomain = parameters["Domain"];
                    string userAD = General.Decrypt(parameters["AD User"]);
                    string passwordAD = General.Decrypt(parameters["AD Password"]);

                    _params.SetValue(strDomain, 0);
                    _params.SetValue(userAD, 1);
                    _params.SetValue(passwordAD, 2);
                }
                else
                {
                    string message = "The 'AD User' or 'AD Password' parameters are empty.";
                    General.SaveErrorsLog("GetConfigurationParameters() " + null, message);
                }

                return _params;
            }
            catch (Exception ex)
            {
                SaveErrorsLog(string.Empty, "FGetConfigurationParameters() - " + ex.Message.ToString());
                return null;
            }
        }

        //---------------------------------------------------------------------------------
        //START IMPERSONATION
        //---------------------------------------------------------------------------------
        public static WindowsImpersonationContext StartImpersonation(string ADomain, string AName, string APwd)
        {
            WindowsImpersonationContext WinImpContext = null;
            try
            {
                WinImpContext = GenerateIdentity(AName, ADomain, APwd).Impersonate();
            }
            catch { }
            return WinImpContext;
        }


        //---------------------------------------------------------------------------------
        //END IMPERSONATION
        //---------------------------------------------------------------------------------
        public static void EndImpersonation(WindowsImpersonationContext WinImpContext)
        {
            try
            {
                WinImpContext.Undo();
            }
            catch { }


        }


        //---------------------------------------------------------------------------------
        //Generate INDENTITY
        //---------------------------------------------------------------------------------
        public static WindowsIdentity GenerateIdentity(string User, string Domain, string Password)
        {

            IntPtr tokenHandle = new IntPtr(0);
            const int LOGON32_PROVIDER_DEFAULT = 0;
            const int LOGON32_LOGON_NETWORK = 3;
            tokenHandle = IntPtr.Zero;
            bool returnValue = LogonUser(User, Domain, Password, LOGON32_LOGON_NETWORK, LOGON32_PROVIDER_DEFAULT, ref tokenHandle);
            if (!returnValue)
            {
                int ret = Marshal.GetLastWin32Error();
                WriteEventLog("GSA_ApplicationPrint", "GenerateIdentityFailed", EventLogEntryType.Information, 667);
            }
            WindowsIdentity id = new WindowsIdentity(tokenHandle);
            CloseHandle(tokenHandle);
            return id;
        }


        //---------------------------------------------------------------------------------
        //SEVERAL
        //---------------------------------------------------------------------------------
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref System.IntPtr phToken);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool CloseHandle(System.IntPtr handle);

        #endregion

        #region <ERRORS / TRACES>

        public static void SaveErrorsLog(string WFID, string message)
        {
            try
            {               
                string inf = string.Empty;
                string urlWeb = General.GetAppSettings("urlSite");

                inf = (string.IsNullOrEmpty(WFID)) ? "RSPrintApplication() - '" + message : "RSPrintApplication()[" + WFID + "] - '" + message;

                if (inf.Length > 128)
                    inf = inf.Substring(0, 127);

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite colsit = new SPSite(urlWeb))
                    {
                        SPWeb web = colsit.OpenWeb();

                        SPList errorList = web.GetListFromWebPartPageUrl(web.Url + "/Lists/ErrorLog/AllItems.aspx");
                        SPQuery query = new SPQuery();
                        query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + inf + "</Value></Eq></Where>";
                        //query.ViewFields = string.Concat("<FieldRef Name='Title' />", "<FieldRef Name='RSQueryLog' />");
                        //query.ViewFieldsOnly = true; // Fetch only the data that we need

                        SPListItemCollection itemCollection = errorList.GetItems(query);
                        SPListItem itm = null;

                        if (itemCollection.Count > 0)
                        {
                            itm = itemCollection[0];
                            itm["RSQueryLog"] = message;
                        }
                        else
                        {
                            itm = errorList.Items.Add();
                            itm["Title"] = inf;
                            itm["RSQueryLog"] = message;
                        }

                        web.AllowUnsafeUpdates = true;
                        itm.Update();

                        web.Close();
                        web.Dispose();
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
