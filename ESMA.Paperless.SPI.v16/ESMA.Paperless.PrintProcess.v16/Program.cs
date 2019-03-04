using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.SharePoint;
using System.Configuration;
using System.Xml;
using System.IO;

using msTextSharp = iTextSharp.text;
using iTextSharp.text.pdf;

//IMPERSONATION
using System.Security.Principal;        // Needed for Impersonation
using Microsoft.Win32;                  // Needed for access to the Registry
using System.Runtime.InteropServices;   // Needed for Event Log 

namespace ESMA.Paperless.PrintProcess.v16
{
    class Program
    {
       
        public static Dictionary<string, string> parameters;
       
        //--------------------------------------------------------------------
        //Application: ESMA.Paperless.PrintProcess.v16
        //Compatible: SharePoint 2016
        //Build Platform target: x86
        //Framework: .NET Framework 4.5
        //Release: v.2.0.0
        //Modified Date: 23/11/2018
        //--------------------------------------------------------------------

        static void Main(string[] args)
        {
            string pathDirectory = string.Empty;

            try
            {

                string urlSite = General.GetAppSettings("urlSite");
                //Extensions allowed
                string[] documentsExtensionList = new string[10] { ".doc", ".docx", ".xls", ".xlsx", ".pdf", ".png", ".gif", ".jpg", ".bmp", ".jpeg" };

                ProcessStartInfo startInfo = new ProcessStartInfo("PaperlessPrintProcess.exe");
                //startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite colsit = new SPSite(urlSite))
                    {
                        SPWeb Web = colsit.OpenWeb();
                        Web.AllowUnsafeUpdates = true;


                        //Parameters
                        parameters = General.GetConfigurationParameters(Web);

                        if (parameters != null)
                        {
                            pathDirectory = System.IO.Path.Combine(parameters["RS Path Temp"], "RSPrintedDocuments");

                            //Document Types
                            List<string> headerSectionIndexList = SP.GetDocumentationType(Web);
                            //Logs Columns
                            List<string> columnInternalNameLogsList = Logs.GetInternalColumnsNameLOGs();
                            //Save Record
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("-------------------------------------------------------------");
                            Console.WriteLine("Paperless Print Process - Running on '" + System.DateTime.Now);
                            Console.WriteLine("-------------------------------------------------------------");
                            Console.WriteLine("");
                            General.SaveErrorsLog(null, "Paperless Print Process - Running on '" + System.DateTime.Now);

                            if (!string.IsNullOrEmpty(pathDirectory))
                            {
                                General.CreateRSPrintingDirectory(pathDirectory);

                                //Create all printed documents (only if not exist)
                                SearchWorkflowsList(false, Web, pathDirectory, headerSectionIndexList, documentsExtensionList, columnInternalNameLogsList);

                                //Update all printed documents
                                //SearchWorkflowsList(true, Web, pathDirectory, headerSectionIndexList, documentsExtensionList,  columnInternalNameLogsList);
                            }
                            else
                                General.SaveErrorsLog(null, "The printing path does not exist in the Configuration Parameters List.");
                        }
                        else
                        {
                            General.SaveErrorsLog(null, "Configuration Parameters Dictionary null. It is not possible to create any print document.");
                        }

                        General.SaveErrorsLog(null, "Paperless Print Process - Finished at'" + System.DateTime.Now);

                        Web.AllowUnsafeUpdates = false;
                        Web.Close();
                        Web.Dispose();
                    }

                });

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "Main() - " + ex.Message.ToString());
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Main() - " + ex.Message.ToString());
            }
            finally
            {
                try
                {
                    General.DeleteRSPrintingDirectory(pathDirectory);
                }
                catch (Exception ex)
                {
                    General.SaveErrorsLog(null, "Main() - finally" + ex.Message.ToString());
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Main() - finally" + ex.Message.ToString());
                }
                
            }
        }

        public static void SearchWorkflowsList(bool updatePDFDocument, SPWeb web, string pathDirectory, List<string> headerSectionIndexList, string[] documentsExtensionLists, List<string> columnInternalNameLogsList)
        {
            try
            {
                SPList WFConfigurationList = web.GetListFromWebPartPageUrl(web.Url + "/Lists/WFConfiguration/AllItems.aspx");
                SPQuery query = new SPQuery();
                query.Query = "<Where><IsNotNull><FieldRef Name='Title' /></IsNotNull></Where><OrderBy><FieldRef Name='WFOrder' Ascending='True' /></OrderBy>";
                query.ViewFields = string.Concat(
                                 "<FieldRef Name='WFOrder' />",
                                 "<FieldRef Name='Title' />",
                                 "<FieldRef Name='WFLibraryURL' />",
                                 "<FieldRef Name='WFLogURL' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need
                SPListItemCollection itemColl = WFConfigurationList.GetItems(query);


                foreach (SPListItem item in itemColl)
                {

                    if (item["WFLibraryURL"] != null)
                    {
                        SPList WFLibrary = web.GetListFromUrl(item["WFLibraryURL"].ToString());
                        SPList logList = web.GetListFromUrl(item["WFLogURL"].ToString());

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("--------------------------------------------------");
                        Console.WriteLine(WFLibrary.Title.ToUpper());
                        Console.WriteLine("--------------------------------------------------");

                        //-------------------------------------------------
                        SearchClosedDeletedWorkflows(web, WFLibrary, logList, updatePDFDocument, pathDirectory, headerSectionIndexList, documentsExtensionLists,  columnInternalNameLogsList);
                        //-------------------------------------------------
                    }


                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "SearchWorkflowsList() - " + ex.Message.ToString());
            }

        }

        private static void SearchClosedDeletedWorkflows(SPWeb Web, SPList WFLibrary, SPList logList, bool updatePDFDocument, string pathDirectory, List<string> headerSectionIndexList, string[] documentsExtensionLists, List<string> columnInternalNameLogsList)
        {
            string WFID = string.Empty;

            try
            {
                List<string> printedWorkflows = SP.GetPrintDocuments(WFLibrary);

                SPQuery query = new SPQuery();
                query.Query = "<Where><Or><Eq><FieldRef Name= 'WFStatus' /><Value Type=\"Choice\">Closed</Value></Eq>"
                    + "<Eq><FieldRef Name= 'WFStatus'/><Value Type=\"Choice\">Deleted</Value></Eq></Or></Where>";

                SPListItemCollection itemColl = WFLibrary.GetItems(query);     

                foreach (SPListItem item in itemColl)
                {
                        if (item["WFID"] != null)
                        //if (item["WFID"] != null && item["WFID"].ToString() == "101833")
                        {

                        WFID = item["WFID"].ToString();
                        string wfName = item["WFType"].ToString();
                        string status = item["WFStatus"].ToString();
                        string printedDocumentName = General.GeneratePrintDocumentName(wfName.ToUpper().ToString(), WFID) + "_" + WFID + ".pdf";
                        string WFIDPath = System.IO.Path.Combine(pathDirectory, WFID);
                        string urlWF = item.Url;

                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("- WFID: " + WFID + "[" + status + "]");

                        if (updatePDFDocument.Equals(false))
                        {
                            //if (SP.ExistPrintDocument(printedDocumentName, Web, WFID, urlWF) == false)
                            if (!printedWorkflows.Contains(WFID))                            
                            {
                                General.GenerateWFDirectory(WFID, WFIDPath);
                                //-----------------------------------------------------------------------------------
                                GeneratePrintDocumentModule(item, Web, WFLibrary, logList, WFIDPath, WFID, printedDocumentName, wfName, headerSectionIndexList, documentsExtensionLists,  columnInternalNameLogsList);
                                //-----------------------------------------------------------------------------------
                            }
                        }
                        else
                        {
                            General.GenerateWFDirectory(WFID, WFIDPath);
                            //-----------------------------------------------------------------------------------
                            GeneratePrintDocumentModule(item, Web, WFLibrary, logList, WFIDPath, WFID, printedDocumentName, wfName, headerSectionIndexList,  documentsExtensionLists,  columnInternalNameLogsList);
                            //-----------------------------------------------------------------------------------
                        }


                    }
                }
            }

            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "SearchClosedWorkflows() - " + ex.Message.ToString());
            }

        }

        private static void GeneratePrintDocumentModule(SPListItem item, SPWeb Web, SPList WFList, SPList logList, string WFIDPath, string WFID, string printedDocumentName, string wfName, List<string> headerSectionIndexList, string[] documentsExtensionLists, List<string> columnInternalNameLogsList)
        {

            try
            {

                SPFolder folder = item.Folder;

                List<string> groupNameList = new List<string>();
                groupNameList = SP.GetWorkflowInitialSteps(item, WFID);

                GeneratePrintDocument(item, Web, WFList, logList, WFIDPath, WFID, printedDocumentName, wfName, folder, headerSectionIndexList, documentsExtensionLists, groupNameList,  columnInternalNameLogsList);

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, " GeneratePrintDocumentModule() - " + ex.Message.ToString());
            }
        }

        private static void GeneratePrintDocument(SPListItem item, SPWeb Web, SPList WFList, SPList logList, string WFIDPath, string WFID, string printedDocumentName, string wfName, SPFolder folder, List<string> headerSectionIndexList, string[] documentsExtensionLists, List<string> groupNamesList, List<string> columnInternalNameLogsList)
        {

            try
            {

                //-------------------------------------------------------------------------------------------------------------------------
                //(1) Download Documents from SP to the File System on Server
                //-------------------------------------------------------------------------------------------------------------------------
                AttachDocuments.GetDocumentsFromDocumentLibrary(WFID, WFList, folder, WFIDPath);

                //Font
                msTextSharp.Font calibriBold = PDF.GetBoldFontPDF(WFID, 10f);
                msTextSharp.Font calibriNormal = PDF.GetNormalFontPDF(WFID, 10f);

                //--------------------------------------------------------------------------------------------------------------------------
                //(2) Generate Form PDF
                //-------------------------------------------------------------------------------------------------------------------------
                Form.GeneratePDF_Form(WFID, item, WFIDPath, printedDocumentName, Web, calibriBold, calibriNormal, wfName, parameters, logList, groupNamesList);
                //-------------------------------------------------------------------------------------------------------------------------
                //(3) Generate Index PDF
                //-------------------------------------------------------------------------------------------------------------------------
                AttachDocuments.GeneratePDF_Index(WFID, item, WFIDPath, printedDocumentName, Web, calibriBold, calibriNormal, folder, wfName, parameters, headerSectionIndexList, documentsExtensionLists);
                //-------------------------------------------------------------------------------------------------------------------------
                //(4) Generate  Logs PDF
                //-------------------------------------------------------------------------------------------------------------------------
                Logs.GeneratePDF_Logs(WFID, item, WFIDPath, printedDocumentName, calibriBold, Web, wfName, parameters, logList, columnInternalNameLogsList);
                //-------------------------------------------------------------------------------------------------------------------------
                //(5) Generate the index + final PDF and upload this document to SP.
                //-------------------------------------------------------------------------------------------------------------------------
                AttachDocuments.GeneratePrintDocument(WFID, WFIDPath, item, Web, printedDocumentName, folder, headerSectionIndexList);

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, " GeneratePrintDocument() - " + ex.Message.ToString());
            }
        }

    }
}
