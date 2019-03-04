using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using System.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Office.Interop.Excel;
using System.Data;
using System.Drawing;

namespace ESMA.Paperless.MaintenanceTasks.v16
{
    class Bug1143
    {
        public static void FixPermissionsModule()
        {
            try
            {

                General.TraceHeader("*** [ESMA-1143] Restrict document(s) deletion permissions *** -    Started at: " + System.DateTime.Now.ToString(), ConsoleColor.Green);


                //E:\ENISA\UpdateProcess
                string pathLogs = ConfigurationManager.AppSettings["pathLOGS"];
                General.CreateFolderXML(pathLogs);
                string urlWeb = ConfigurationManager.AppSettings["url"];
                string wfOrderParameter = ConfigurationManager.AppSettings["WFOrderListToUpdated"];
       

                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite colsit = new SPSite(urlWeb))
                    {
                        SPWeb web = colsit.OpenWeb();
                        web.AllowUnsafeUpdates = true;

                        if (!string.IsNullOrEmpty(wfOrderParameter))
                        {

                            String[] wfOrderList = wfOrderParameter.Split(',');


                            foreach (var wfOrder in wfOrderList)
                            {
                                //RS WF Configuration
                                SPListItem wfTypeConf = SP.GetWFTypeConfiguration(web, wfOrder);
                                string wfTypeName = wfTypeConf["Title"].ToString().Trim();
                          

                                if (wfTypeConf["WFLibraryURL"] != null)
                                {

                                    General.TraceInformation("- WF Type to update: '" + wfTypeName + "'.", ConsoleColor.Yellow);
                                    InheritPermissions(web, wfTypeConf, wfTypeName, pathLogs, wfOrder);
                                }
                                else
                                    General.TraceInformation("- WFType: '" + wfTypeName + "' does not have 'WFLibraryURL'.", ConsoleColor.Red);


                            }





                        }
                        else
                            General.TraceInformation("No WFOrder has been especified to be analysed.", ConsoleColor.Red);


                        web.AllowUnsafeUpdates = false;
                        web.Close();
                        web.Dispose();
                    }

                });
            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }

        private static void InheritPermissions(SPWeb web, SPListItem wfTypeConf, string wfTypeName, string pathLogs, string wfOrder)
        {
            try
            {
                Dictionary<string, string> wfInformationDictionary = new Dictionary<string, string>();
                SPList wfLibrary = SP.GetWFLibrary(web, wfTypeConf);

                if (wfLibrary != null)
                {
                    
                    //Total WFs
                    SPListItemCollection wfsCollection = SP.GetClosedDeletedWFs(web, wfLibrary);
                    General.TraceInformation("- Total WFs to update: '" + wfsCollection.Count.ToString() + "'.", ConsoleColor.White);

                    foreach (SPListItem wfItem in wfsCollection)
                    {
                        string wfid = string.Empty;
                    

                        try
                        {
                     
                            wfid = wfItem["WFID"].ToString();
                            string status = wfItem["WFStatus"].ToString();
                            string confidentialValue = wfItem["ConfidentialWorkflow"].ToString();
        

                            General.TraceInformation("- WFID: " + wfid + " (" + status + ")", ConsoleColor.Gray);


                            wfItem.ResetRoleInheritance();

                            wfInformationDictionary.Add(wfid, status);

                        }
                        catch (Exception ex)
                        {
                            General.TraceException(ex);
                            General.TraceInformation("- Error WFID: '" + wfid + "' - URL: " + wfItem.Url, ConsoleColor.Red);
                        }

                    }

                    //Logs
                    CreateExcelFile(wfTypeName, wfInformationDictionary, pathLogs, wfOrder);

                }
                else
                    General.TraceInformation("- WFType: '" + wfTypeName + "' does not have 'WF Library'.", ConsoleColor.Red);

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
        }

        #region <EXCEL FILE>

        private static void CreateExcelFile(string wfTypeName, Dictionary<string, string> wfInformationDictionary, string pathLogs, string wfOrder)
        {
            Microsoft.Office.Interop.Excel.Application excel;
            Microsoft.Office.Interop.Excel.Workbook worKbooK;
            Microsoft.Office.Interop.Excel.Worksheet worKsheeT;
            Microsoft.Office.Interop.Excel.Range celLrangE;

            try
            {
                excel = new Microsoft.Office.Interop.Excel.Application();
                excel.Visible = false;
                excel.DisplayAlerts = false;
                worKbooK = excel.Workbooks.Add(Type.Missing);


                worKsheeT = (Microsoft.Office.Interop.Excel.Worksheet)worKbooK.ActiveSheet;
                worKsheeT.Name = wfTypeName;

                System.Data.DataTable wfTable = CreateTable(wfInformationDictionary);

                int rowcount = 2;

                foreach (DataRow datarow in wfTable.Rows)
                {
                    rowcount += 1;

                    for (int i = 1; i <= wfTable.Columns.Count; i++)
                    {

                        if (rowcount == 3)
                        {
                            worKsheeT.Cells[2, i] = wfTable.Columns[i - 1].ColumnName;

                        }

                        worKsheeT.Cells[rowcount, i] = datarow[i - 1].ToString();

                        if (rowcount > 3)
                        {
                            if (i == wfTable.Columns.Count)
                            {
                                if (rowcount % 2 == 0)
                                {
                                    celLrangE = worKsheeT.Range[worKsheeT.Cells[rowcount, 1], worKsheeT.Cells[rowcount, wfTable.Columns.Count]];
                                }

                            }
                        }

                    }

                }

                celLrangE = worKsheeT.Range[worKsheeT.Cells[1, 1], worKsheeT.Cells[rowcount, wfTable.Columns.Count]];
                celLrangE.EntireColumn.AutoFit();
                celLrangE = worKsheeT.Range[worKsheeT.Cells[1, 1], worKsheeT.Cells[2, wfTable.Columns.Count]];

                worKbooK.SaveAs(pathLogs + wfOrder + "_" + wfTypeName + ".xls");
                worKbooK.Close();
                excel.Quit();

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
            finally
            {
                worKsheeT = null;
                celLrangE = null;
                worKbooK = null;
            }
        }


        public static System.Data.DataTable CreateTable(Dictionary<string,string> wfInformationDictionary)
        {
            System.Data.DataTable table = new System.Data.DataTable();

            try
            {

                table.Columns.Add("WFID", typeof(string));
                table.Columns.Add("WFTitle", typeof(string));
             

                foreach (KeyValuePair<string, string> kvp in wfInformationDictionary)
                {
                    table.Rows.Add(kvp.Key, kvp.Value);
                }

            }
            catch (Exception ex)
            {
                General.TraceException(ex);
            }
            return table;
        }

        #endregion

    }
}
