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
using System.Net;
using System.Net.Sockets;
using Microsoft.SharePoint.Utilities;
using System.Collections.Generic;
using Microsoft.SharePoint.Administration.Claims;



//-----------------IMPERSONACION-------------------//

using System.Security.Principal;        // Needed for Impersonation
using Microsoft.Win32;                  // Needed for access to the Registry
using System.Diagnostics;

namespace ESMA.Paperless.MaintenanceTasks.v16
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
                TraceException(ex);
            }
        }


        //--------------------------------------------------------------------------------------
        //LOGS - C:\temp
        //--------------------------------------------------------------------------------------
        public static void SaveInformationIn(string texto)
        {

            try
            {
                #region <PARAMETERS IMPERSONATION>

                string strDomain = string.Empty;
                string strUser = string.Empty;
                string strPassword = string.Empty;

                string[] paramsConfig = getConfigurationParameters();
                strDomain = paramsConfig.GetValue(0).ToString();
                strUser = paramsConfig.GetValue(1).ToString();
                strPassword = paramsConfig.GetValue(2).ToString();

                #endregion

                string strFolderName = ConfigurationManager.AppSettings["pathLOGs"];

                //Start the Impersonation
                WindowsImpersonationContext GlobalWIC = StartImpersonation(strDomain, strUser, strPassword);

                //If the path doesn´t exist we will create it.
                if (!System.IO.File.Exists(strFolderName))
                    System.IO.Directory.CreateDirectory(strFolderName);


                string strFileName = ConfigurationManager.AppSettings["fileInformation"];
                string strYear = DateTime.Now.Year.ToString();
                string strMonth = DateTime.Now.Month.ToString();
                string strDay = DateTime.Now.Day.ToString();
                string strTimeError = strYear + strMonth + strDay;

                string strFileTotalName = strFileName + strTimeError + ".log";

                string path = System.IO.Path.Combine(strFolderName, strFileTotalName);

                StreamWriter sw = new StreamWriter(path, true);
                sw.WriteLine(texto);
                sw.Flush();
                sw.Close();


                //End Impersonation
                //--------------------------------------------------------------------------
                EndImpersonation(GlobalWIC);

            }

            catch (Exception ex)
            {
                TraceException(ex);
            }

        }

        public static void SaveExceptionIn(string texto)
        {

            try
            {
                #region <PARAMETERS IMPERSONATION>

                string strDomain = string.Empty;
                string strUser = string.Empty;
                string strPassword = string.Empty;

                string[] paramsConfig = getConfigurationParameters();
                strDomain = paramsConfig.GetValue(0).ToString();
                strUser = paramsConfig.GetValue(1).ToString();
                strPassword = paramsConfig.GetValue(2).ToString();

                #endregion

                string strFolderName = ConfigurationManager.AppSettings["pathLOGs"];

                //Start the Impersonation
                WindowsImpersonationContext GlobalWIC = StartImpersonation(strDomain, strUser, strPassword);

                //If the path doesn´t exist we will create it.
                if (!System.IO.File.Exists(strFolderName))
                    System.IO.Directory.CreateDirectory(strFolderName);


                string strFileName = ConfigurationManager.AppSettings["fileExceptions"];
                string strYear = DateTime.Now.Year.ToString();
                string strMonth = DateTime.Now.Month.ToString();
                string strDay = DateTime.Now.Day.ToString();
                string strTimeError = strYear + strMonth + strDay;

                string strFileTotalName = strFileName + strTimeError + ".log";

                string path = System.IO.Path.Combine(strFolderName, strFileTotalName);

                StreamWriter sw = new StreamWriter(path, true);
                sw.WriteLine(texto);
                sw.Flush();
                sw.Close();


                //End Impersonation
                //--------------------------------------------------------------------------
                EndImpersonation(GlobalWIC);

            }

            catch (Exception ex)
            {
                TraceException(ex);
            }

        }

        public static void TraceException(Exception ex)
        {

            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("SOURCE: " + ex.Source + " - MESSAGE: " + ex.Message + " - TRACE: " + ex.StackTrace);
                SaveExceptionIn("SOURCE: " + ex.Source + " - MESSAGE: " + ex.Message + " - TRACE: " + ex.StackTrace);

            }

            catch (Exception ex1)
            {
                TraceException(ex1);
            }

        }

        public static void TraceInformation(string message, ConsoleColor colour)
        {

            try
            {
                Console.ForegroundColor = colour;
                Console.WriteLine(message);
                SaveInformationIn(message);

            }

            catch (Exception ex)
            {
                TraceException(ex);
            }

        }

        public static void TraceHeader(string message, ConsoleColor colour)
        {

            try
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("-------------------------------------------------------------------------------");
                Console.WriteLine(message);
                Console.WriteLine("-------------------------------------------------------------------------------");

                General.SaveInformationIn("----------------------------------------------------------------");
                General.SaveInformationIn(message);
                General.SaveInformationIn("-----------------------------------------------------------------");
                General.SaveInformationIn(" ");

            }

            catch (Exception ex)
            {
                TraceException(ex);
            }

        }

        #endregion

        #region <IMPERSONATION>

        //------------------------------------------------------------------------------------------------
        //FUNCTION: We are going to get the values of User, Password and Domain from the web.config. 
        //We are going to use these values to the impersonation.
        //-----------------------------------------------------------------------------------------------
        public static string[] getConfigurationParameters()
        {
            try
            {
                string[] paramsLogin = new string[3];

                string strDomain = getAppSettings("domain").ToString();
                string strUser = getAppSettings("user").ToString();
                string strPassword = getAppSettings("password").ToString();

                paramsLogin.SetValue(strDomain, 0);
                paramsLogin.SetValue(strUser, 1);
                paramsLogin.SetValue(strPassword, 2);

                return paramsLogin;
            }
            catch (Exception ex)
            {
                TraceException(ex);
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
                WinImpContext = CreateIdentity(AName, ADomain, APwd).Impersonate();
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
        //CREATE INDENTITY
        //---------------------------------------------------------------------------------
        public static WindowsIdentity CreateIdentity(string User, string Domain, string Password)
        {

            IntPtr tokenHandle = new IntPtr(0);
            const int LOGON32_PROVIDER_DEFAULT = 0;
            const int LOGON32_LOGON_NETWORK = 3;
            tokenHandle = IntPtr.Zero;
            bool returnValue = LogonUser(User, Domain, Password, LOGON32_LOGON_NETWORK, LOGON32_PROVIDER_DEFAULT, ref tokenHandle);
            if (!returnValue)
            {
                int ret = Marshal.GetLastWin32Error();
                WriteEventLog("ESMA.Paperless.Maintenance", "CreateIdentityFailed", EventLogEntryType.Information, 667);
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
        private extern static bool CloseHandle(System.IntPtr handle);

        #endregion

        #region <OTHER FUNCTIONS>

        //--------------------------------------------------------------------------------------
        //FUNCTION: Get the value from the <APPSETTINGS> (web.config)
        //--------------------------------------------------------------------------------------
        public static string getAppSettings(string key)
        {

            return System.Configuration.ConfigurationManager.AppSettings[key].ToString();

        }


        //------------------------------------------------------------------------------------------------
        //FUNCTION: We are going to use this function to decrypt the values of the fields user and password,
        //which they are encrypted in the web.config.
        //-----------------------------------------------------------------------------------------------
        public static string decrypt(string data)
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


        public static void CreateFolderXML(string folderName)
        {
            try
            {
                if (!System.IO.Directory.Exists(folderName))
                {
                    Directory.CreateDirectory(folderName);
                }

            }
            catch (Exception ex)
            {
                TraceException(ex);
            }
        }

        public static string GetLocalIPAddress()
        {
            string ipServer = string.Empty;

            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());

                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        ipServer.ToString();

                }

                if (string.IsNullOrEmpty(ipServer))
                    ipServer = ConfigurationManager.AppSettings["serverName"];
            }
            catch
            {
                ipServer = ConfigurationManager.AppSettings["serverName"];
            }

            return ipServer;
        }


        #endregion
    }
}
