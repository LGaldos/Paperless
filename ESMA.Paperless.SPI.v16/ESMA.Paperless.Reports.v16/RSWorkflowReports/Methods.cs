using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.WebControls;

namespace ESMA.Paperless.Reports.v16.RSWorkflowReports
{
    class Methods
    {
        public static Dictionary<string, string> GetConfigurationParameters(SPWeb Web)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
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
                SPQuery query = new SPQuery();
                query.Query = "<Where><IsNotNull><FieldRef Name='Title'/></IsNotNull></Where>";
                query.ViewFields = string.Concat(
                                   "<FieldRef Name='Title' />",
                                   "<FieldRef Name='WFOrder' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need
                SPListItemCollection itemCollection = list.GetItems(query);


                foreach (SPListItem item in list.Items)
                {
                    if (item["WFOrder"] != null)
                        wftypes.Add(item.Title.ToUpper(), item["WFOrder"].ToString());
                }

            }
            catch (Exception ex)
            {
                SaveErrorsLog("GetWorkflowTypeOrder() - " + ex.Source, ex.Message);
            }

            return wftypes;
        }

        public static string GetOnlyUserAccount(string userAccount)
        {
            try
            {
                string account = string.Empty;

                if (userAccount.Contains("\\"))
                {
                    int pos = userAccount.IndexOf("\\");
                    account = userAccount.Substring(pos, (userAccount.Length - pos));

                    if (account.Contains("\\"))
                        account = account.Replace("\\", null);

                }

                return account;
            }
            catch (Exception ex)
            {
                SaveErrorsLog("GetOnlyUserAccount() - " + ex.Source, ex.Message);
                return null;
            }
        }

        public static void Number2String(int index, ref string value1)
        {

            try
            {
                const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

                if (index >= letters.Length)
                {
                    value1 += letters[index / letters.Length - 1];

                    if ((index - (letters.Length + 1)) > 0)
                    {
                        Number2String((index - (letters.Length + 1)), ref value1);
                    }
                }
                else
                {
                    value1 += letters[index % letters.Length];
                }


            }
            catch (Exception ex)
            {
                SaveErrorsLog("Number2String() - " + ex.Source, ex.Message);
            }

        }

        public static Dictionary<string, string> MergeDictionary(Dictionary<string, string> dictionary1, Dictionary<string, string> dictionary2)
        {
            try
            {
                Dictionary<string, string> totalDictionary = new Dictionary<string, string>();
                totalDictionary = totalDictionary.Union(dictionary1).ToDictionary(i => i.Key, i => i.Value);

                foreach (KeyValuePair<String, String> kvp in dictionary2)
                {

                    if (!totalDictionary.ContainsValue(kvp.Value))
                        totalDictionary.Add(kvp.Key, kvp.Value);

                }

                return totalDictionary;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("MergeDictionary() - " + ex.Source, ex.Message);
                return null;
            }

        }

        public static string ExtractDiferentValue(string columnName, Dictionary<string, string> parameters, string wfOrder, string wfType, SPListItem item)
        {
            try
            {
                string value = string.Empty;

                if (columnName == parameters["Reports Field [01]"]) //Workflow Order
                    value = wfOrder;

                else if (columnName == parameters["Reports Field [02]"]) //Workflow Type
                    value = wfType;

                //else if (columnName == parameters["Reports Field [11]"]) //Link to Workflow
                //{
                //    value = DataManagement.GetWFsInformation(item);
                //}
                else if ((columnName == parameters["Reports Field [08]"]) || (columnName == parameters["Reports Field [10]"])) //Author + Editor
                {
                    if (item[columnName] != null)
                        value = FormatUser(item[columnName].ToString());

                }
                else if ((columnName == parameters["Reports Field [07]"]) || (columnName == parameters["Reports Field [09]"])) //Created + Modified
                {
                    if (item[columnName] != null)
                        value = FormatDateTimeValue(item[columnName].ToString());

                }

                return value;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("ExtractDiferentValue() - " + ex.Source, ex.Message);
                return null;
            }

        }

        public static bool ExcludeColumn(string columnName, IEnumerable<string> argumentEnum)
        {
            bool different = false;

            try
            {

                if (argumentEnum.Contains(columnName))
                    different = true;

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("ExcludeColumn() - " + ex.Source, ex.Message);
            }


            return different;
        }

        public static string GetFormatedValues(string value, string fieldType)
        {
            try
            {
                switch (fieldType.ToLower())
                {
                    case "datetime":
                        value = FormatDateValue(value);
                        break;

                    case "boolean":
                        value = FormatCheckBoxValue(value);
                        break;

                    case "user":
                        value = FormatUser(value);
                        break;


                    default:
                        break;

                }

                return value;

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GetFormatedValues() - " + ex.Source, ex.Message);
                return string.Empty;
            }

        }

        //258;#Deborah Meunier -> Deborah Meunier
        //-----------------------------------------------------------------------------
        public static string FormatUser(string fullName)
        {
            try
            {
                string name = string.Empty;
                int pos = 0;


                if (!string.IsNullOrEmpty(fullName))
                {
                    if (fullName.Contains("#"))
                    {
                        pos = fullName.IndexOf("#");
                        //#Deborah Meunier
                        name = fullName.Substring(pos, (fullName.Length - pos));

                        if (name.Contains("#"))
                        {
                            //Deborah Meunier
                            name = name.Replace("#", null);
                        }
                    }

                    if (name.Contains("\\"))
                        name = GetOnlyUserAccount(name);


                }

                if (string.IsNullOrEmpty(name))
                {
                    name = fullName;
                }

                return name;

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("FormatedUser() - " + ex.Source, ex.Message);
                return "";
            }
        }

        //dd-MM-yyyy hh:mm:ss -> dd/MM/yyyy hh:mm:ss
        public static string FormatDateTimeValue(string dateValue)
        {
            try
            {
                if (dateValue.Contains("-"))
                    dateValue = dateValue.Replace("-", "/");

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("FormatDateTimeValue() - " + ex.Source, ex.Message);
            }

            return dateValue;
        }

        //dd-MM-yyyy  -> dd/MM/yyyy 
        public static string FormatDateValue(string dateValue)
        {
            try
            {
                if (dateValue.Contains("-"))
                    dateValue = dateValue.Replace("-", "/");


                if (dateValue.Contains(" "))
                    dateValue = dateValue.Substring(0, dateValue.IndexOf(" "));

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("FormatDateTimeValue() - " + ex.Source, ex.Message);
            }

            return dateValue;
        }

        //False or True
        public static string FormatCheckBoxValue(string value)
        {
            try
            {
                if (value == null)
                    value = "False";


            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("FormatCheckBoxValue() - " + ex.Source, ex.Message);
            }

            return value;
        }

        //------------------------------------------------------------------------------------------------
        //FUNCTION: We are going to use this function to decrypt the values of the fields user and password,
        //which they are encrypted in the web.config.
        //-----------------------------------------------------------------------------------------------
        public static string Decrypt(string data)
        {
            string result = string.Empty;

            try
            {
                System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
                System.Text.Decoder utf8Decode = encoder.GetDecoder();

                byte[] todecode_byte = Convert.FromBase64String(data);
                int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
                char[] decoded_char = new char[charCount];
                utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
                result = new String(decoded_char);



            }
            catch (Exception ex)
            {
                SaveErrorsLog("Decrypt() - " + ex.Source, ex.Message);
            }
            return result;
        }

        //public static string Encrypt(string data)
        //{
        //    byte[] passwordInByte = UTF8Encoding.UTF8.GetBytes(data);
        //    //byte[] protectedPasswordByte = ProtectedData.Protect(passwordInByte, null);
        //    return Convert.ToBase64String(passwordInByte, 0, passwordInByte.Length);
        //}

        #region <ERRORS>

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
                        string messageValue = "[RSReport '" + userAccount + "'] " + source + " - " + message;

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

        #endregion


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
                SaveErrorsLog("DeleteFile() - " + ex.Source, ex.Message);

            }

        }
        public static void DeleteFile(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch (Exception ex)
            {
                SaveErrorsLog("DeleteFile() - " + ex.Source, ex.Message);

            }
        }

        #endregion

        #region PAGE CONTROLS
        public static void SetDateControlKeyEvents(DateTimeControl dtControl)
        {
            string javascriptCode = @"if((event.keyCode >= 48 && event.keyCode <= 57) || (event.keyCode >= 96 && event.keyCode <= 111) || event.keyCode==8 || event.keyCode==46 || event.keyCode==16 || event.keyCode==55 || event.keyCode==47) {return true;}else{return false;}";
            TextBox txtHTML = (TextBox)dtControl.Controls[0];
            txtHTML.Attributes.Add("onkeydown", javascriptCode);
            txtHTML.Attributes.Add("onkeypress", javascriptCode);
            txtHTML.Attributes.Add("onkeyup", javascriptCode);
        }
        #endregion

        public static string FormatWFID(string wfid)
        {
            try
            {
                if (wfid.Contains("."))
                {
                    string[] inf = wfid.Split('.');
                    wfid = inf[0];
                }

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(wfid, "FormatWFID() " + ex.Message);
            }

            return wfid;
        }

        public static SPListItem GetWFInformationByWFID(string wfid, SPWeb Web)
        {
            SPListItem item = null;

            try
            {
                SPList list = Web.GetListFromWebPartPageUrl(Web.Url + "/Lists/WFHistory/AllItems.aspx");
                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='WFID' /><Value Type='Number'>" + wfid.Trim() + "</Value></Eq></Where>";
                query.ViewFields = string.Concat(
                                   "<FieldRef Name='WFID' />", "<FieldRef Name='WFType' />", "<FieldRef Name='WFSubject' />", "<FieldRef Name='Created' />",
                                   "<FieldRef Name='Amount' />", "<FieldRef Name='WFStatus' />", "<FieldRef Name='Urgent' />", "<FieldRef Name='WFDeadline' />",
                                   "<FieldRef Name='ConfidentialWorkflow' />", "<FieldRef Name='DaysToClose' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need
                SPListItemCollection itemCollection = list.GetItems(query);

                if (itemCollection.Count > 0)
                    item = itemCollection[0];

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(wfid, "GetWFInformationByWFID() " + ex.Message);
            }

            return item;
        }

        /// <summary>
        /// Get user login name without domain
        /// </summary>
        /// <param name="userAccount"></param>
        /// <returns>Get user login name without domain. String.</returns>
        public static string RemoveDomain(string userAccount)
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

    }
}
