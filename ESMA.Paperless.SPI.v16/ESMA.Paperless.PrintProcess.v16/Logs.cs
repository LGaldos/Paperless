using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;

using PdfSharp;
using msPdfSharp = PdfSharp.Pdf;
using PdfSharp.Drawing;

using msTextSharp = iTextSharp.text;
using iTextSharp.text.pdf;

//using Microsoft.Office.Interop.Word;
//using msExcel = Microsoft.Office.Interop.Excel;


using System.Configuration;
using data = System.Data;
using System.IO;
using System.Globalization;

////-----------------IMPERSONACION-------------------//

//using System.Security.Principal;        // Needed for Impersonation
//using Microsoft.Win32;                  // Needed for access to the Registry
//using System.Diagnostics;
//using System.Security.Cryptography;
////------------------------------------------------------------------------

namespace ESMA.Paperless.PrintProcess.v16
{
    class Logs
    {

        public static void GeneratePDF_Logs(string WFID, SPListItem item, string WFIDPath, string printedDocumentName, msTextSharp.Font calibriBold, SPWeb Web, string wfName, Dictionary<string, string> parameters, SPList logsList, List<string> columnInternalNameLogsList)
        {
            try
            {
                string logsPDFDocument = printedDocumentName.Replace(".pdf", null) + "_logs.pdf";
                string logsPDFPath = System.IO.Path.Combine(WFIDPath, logsPDFDocument);

                //------------------------------------------------------------------------------------
                Logs.ExportDataToPDFTable_LOGs(logsPDFPath, WFID, calibriBold, item, Web, wfName, parameters, logsList,  columnInternalNameLogsList);
                //------------------------------------------------------------------------------------
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GeneratePDF_Logs() - " + ex.Message.ToString());
            }

        }

        public static void ExportDataToPDFTable_LOGs(string logsPDFPath, string WFID, msTextSharp.Font calibriBold, SPListItem item, SPWeb Web, string wfName, Dictionary<string, string> parameters, SPList logsList, List<string> columnInternalNameLogsList)
        {
            msTextSharp.Document doc = new msTextSharp.Document(iTextSharp.text.PageSize.A4.Rotate());

            try
            {

                //Generate Document class object and set its size to letter and give space left, right, Top, Bottom Margin
                PdfWriter wri = PdfWriter.GetInstance(doc, new FileStream(logsPDFPath, FileMode.Create));
                doc.Open();//Open Document to write

                msTextSharp.Rectangle page = doc.PageSize;

                //------------------------------------------------------------
                //PDF HEADER
                //------------------------------------------------------------
                PDF.DrawHeaderPrincipalPDF(WFID, doc, (wfName + " - " + "LOGS"), wri, page, calibriBold, "WFID");

                //Get Information
                data.DataTable dt = GetLOGSDataTable(WFID, columnInternalNameLogsList, Web, wfName, parameters, logsList);
            

                if (dt != null)
                {
                    //Font
                    msTextSharp.Font calibriLogsNormal = PDF.GetNormalFontPDF(WFID, 8f);

                    //Generate instance of the pdf table and set the number of column in that table
                    PdfPTable PdfTable = new PdfPTable(dt.Columns.Count);

                    //Add Header of the pdf table
                    //---------------------------------------------------------------------------------
                    for (int i = 0; i < columnInternalNameLogsList.Count; i++)
                    {
                        string displayName = GetDisplayNameColumn(Web, columnInternalNameLogsList[i], WFID);
                        PDF.DrawLOGSHeaderPDF(WFID, calibriBold, displayName, PdfTable, columnInternalNameLogsList[i]);
                    }


                    //Add the data from datatable to pdf table
                    for (int rows = 0; rows < dt.Rows.Count; rows++)
                    {
                        for (int column = 0; column < dt.Columns.Count; column++)
                        {
                            PdfPCell PdfPCell = new PdfPCell(new msTextSharp.Phrase(new msTextSharp.Chunk(dt.Rows[rows][column].ToString(), calibriLogsNormal)));
                            PdfPCell.HorizontalAlignment = msTextSharp.Element.ALIGN_LEFT;
                            PdfTable.AddCell(PdfPCell);
                        }
                    }

                    PdfTable.SpacingBefore = 15f; // Give some space after the text or it may overlap the table
                    //rs37
                    float[] columnsWidths = new float[] { 19f, 18f, 20f, 25f, 27f, 20f, 45f, 61f };
                    PdfTable.WidthPercentage = 100f;
                    PdfTable.SetWidths(columnsWidths);
                    //end rs37
                    doc.Add(PdfTable); // add pdf table to the document

                }


            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "ExportDataToPDFTable_LOGs() - " + ex.Message.ToString());
            }

            finally
            {
                //Close document and writer
                doc.Close();
                doc.Dispose();

            }
        }

        private static data.DataTable GetLOGSDataTable(string WFID, List<string> columnInternalNameList, SPWeb Web, string wfName, Dictionary<string, string> parameters, SPList logsList)
        {
            try
            {
                // Generate an object of DataTable class
                data.DataTable dataTable = new data.DataTable("MyDataTable");
                //Generate Rows
                data.DataRow dataRow;

               //Generate Columns
                for (int i = 0; i < columnInternalNameList.Count; i++)
                {
                    data.DataColumn dataColumn = new data.DataColumn(columnInternalNameList[i], typeof(string));
                    dataTable.Columns.Add(dataColumn);
                }

                    SPListItem itm = null;
                    SPListItemCollection itemCol = SP.SearchWorkflowLogs(WFID, Web,logsList);
                
             
                    for (int i = 0; i < itemCol.Count; i++)
                    {
                        itm = itemCol[i];
                        dataRow = dataTable.NewRow();

                        for (int j = 0; j < columnInternalNameList.Count; j++)
                        {
                            string columnName = columnInternalNameList[j].ToString();
                            string fieldType = string.Empty;

                            if (columnName != "Created")
                            {
                                SPField field = Web.Fields.GetFieldByInternalName(columnName);
                                fieldType = field.Type.ToString();
                            }
                            else
                            {
                                fieldType = "datetime";
                            }

                            if (!string.IsNullOrEmpty(fieldType))
                            {
                                string value = string.Empty;

                                if (itm[columnName] != null)
                                {
                                    value = GetFormatedValueLOGs(itm[columnName].ToString(), fieldType, WFID);
                                    dataRow[columnInternalNameList[j]] = value;
                                }
                                else
                                {
                                    dataRow[columnInternalNameList[j]] = "";
                                }
                                
                            }


                        }


                        dataTable.Rows.Add(dataRow);
                    }

                    dataTable.AcceptChanges();

                return dataTable;
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetLOGSDataTable() - " + ex.Message.ToString());
                return null;
            }
        }

        public static string GetFormatedValueLOGs(string value, string fieldType, string WFID)
        {
            try
            {
                switch (fieldType.ToLower())
                {
                    case "datetime":
                        value = General.FormatDateTimeValue(WFID, value);
                        break;

                    case "boolean":
                        value = General.FormatCheckBoxValue(WFID, value);
                        break;

                    case "user":
                        value = General.FormatUserValue(WFID, value);
                        break;
                    
                    default:
                        break;

                }

                return value;

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetLOGSDataTable() - " + ex.Message.ToString());
                return string.Empty;
            }

        }

        public static string GetDisplayNameColumn(SPWeb Web, string internalName, string WFID)
        {
            string displayName = string.Empty;

            try
            {
                if (internalName != "Created")
                {
                    SPField field = Web.Fields.GetFieldByInternalName(internalName);

                    if (field != null)
                        displayName = field.Title;
                    else
                        displayName = internalName;
                    
                }
                else
                {
                    displayName = "Created";
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetDisplayNameColumn() - " + ex.Message.ToString());
            }

            return displayName;
        }

        public static List<string> GetInternalColumnsNameLOGs()
        {
            List<string> columnInternalNameLogsList = new List<string>();

            try
            {
                columnInternalNameLogsList.Add("Created");
                columnInternalNameLogsList.Add("StepNumber");
                columnInternalNameLogsList.Add("WFStatus");
                columnInternalNameLogsList.Add("ActionTaken");
                columnInternalNameLogsList.Add("AssignedPerson");
                columnInternalNameLogsList.Add("ComputerName");
                columnInternalNameLogsList.Add("ActionDetails");
                columnInternalNameLogsList.Add("WorkflowComment");
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(null, "GetDisplayNameColumn() - " + ex.Message.ToString());
            }

            return columnInternalNameLogsList;

        }
    }
}
