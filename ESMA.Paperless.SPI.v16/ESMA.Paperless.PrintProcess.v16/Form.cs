using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using data = System.Data;

using Microsoft.SharePoint;

using msTextSharp = iTextSharp.text;
using iTextSharp.text.pdf;


namespace ESMA.Paperless.PrintProcess.v16
{
    class Form
    {


        public static void GeneratePDF_Form(string WFID, SPListItem item, string WFIDPath, string printedDocumentName, SPWeb MyWeb, msTextSharp.Font calibriBold, msTextSharp.Font calibriNormal, string wfName, Dictionary<string, string> parameters, SPList logList, List<string> groupNamesList)
        {
            try
            {

                string PDFFormName = printedDocumentName.Replace(".pdf", "_form.pdf");
                string PDFFormPath = System.IO.Path.Combine(WFIDPath, PDFFormName);

                Dictionary<string, string> generalFieldsDictionary = new Dictionary<string, string>();
                SP.GetInitialGeneralFieldsValues(WFID, ref  generalFieldsDictionary,  item);

                //------------------------------------------------------------------------------------
                ExportDataToPDF_Form(PDFFormPath, PDFFormName, WFID, item, generalFieldsDictionary, MyWeb, calibriBold, calibriNormal, wfName, parameters, logList, groupNamesList);
                //------------------------------------------------------------------------------------

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GeneratePDF_Form() - " + ex.Message.ToString());
            }


        }

        public static void ExportDataToPDF_Form(string PDFFormPath, string PDFFormName, string WFID, SPListItem item, Dictionary<string, string> generalFieldsDictionary, SPWeb Web, msTextSharp.Font calibriBold, msTextSharp.Font calibriNormal, string wfName, Dictionary<string, string> parameters, SPList logsList, List<string> groupNamesList)
        {
            msTextSharp.Document doc = new msTextSharp.Document(iTextSharp.text.PageSize.LETTER, 40, 20, 42, 35);

            try
            {
                //Generate Document class object and set its size to letter and give space left, right, Top, Bottom Margin
                PdfWriter wri = PdfWriter.GetInstance(doc, new FileStream(PDFFormPath, FileMode.Create));
                doc.Open();//Open Document to write

                msTextSharp.Rectangle page = doc.PageSize;
                
               //------------------------------------------------------------
               //PDF HEADER
               //------------------------------------------------------------
                PDF.DrawHeaderPrincipalPDF(WFID, doc, wfName, wri, page, calibriBold, "WFID");

                if (generalFieldsDictionary != null) 
                {
                    //General Fields
                    DrawGeneralFields(generalFieldsDictionary, WFID, Web, wfName, doc, page, calibriNormal);
                }

                // Links to workflows (CR24)
                DrawLinkToWorkFlow(WFID, item, doc, calibriBold, wri, page, calibriNormal, parameters, wfName);
          

                //Actors
               DrawActors(WFID, item,  doc, calibriBold,  wri,  page, calibriNormal, parameters, groupNamesList);

                //Comments
               DrawComments(WFID, item, doc, calibriBold, wri, page, calibriNormal, wfName, Web, calibriBold, parameters, logsList, groupNamesList);

                //RS37 - Comments Closure
               DrawCommentsClosure(WFID, item, doc, calibriBold, wri, page, calibriNormal, wfName, Web, calibriBold, parameters, logsList);


            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "ExportDataToPDF_Form() - " + ex.Message.ToString());
            }

            finally
            {
                //Close document and writer
                doc.Close();
                doc.Dispose();

            }
        }

        #region <GENERAL FIELDS>

        public static void DrawGeneralFields(Dictionary<string,string>  generalFieldsDictionary, string WFID, SPWeb Web, string WFName, msTextSharp.Document doc, msTextSharp.Rectangle page, msTextSharp.Font calibriNormal)
        {
            string columnName = string.Empty;

            try
            {

                int fieldsTotal = generalFieldsDictionary.Count;
               int numFields = 0;

                // Now iterate through the table and add your controls 
                foreach (KeyValuePair<String, String> kvp in generalFieldsDictionary)
                {

                    columnName = kvp.Key;
                    string value = kvp.Value;

                    SPFieldType fieldType = SP.GetColumnType(Web, columnName, WFName, WFID);

                    if (fieldType != 0)
                        DrawControlsType(columnName, fieldType, value, WFID, doc, calibriNormal, numFields, fieldsTotal, page);


                    numFields++;
                }

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawGeneralFields() - ColumnName: '" + columnName + "' - " + ex.Message);
            }
        }

        private static void DrawControlsType(string columnName, SPFieldType fieldType, string value, string WFID, msTextSharp.Document doc, msTextSharp.Font calibriNormal, int numFields, int fieldsTotal, msTextSharp.Rectangle page)
         {
             try
             {
                 switch (fieldType)
                 {

                     case SPFieldType.Text:
                         PDF.DrawUserControlsPDF(WFID, doc, columnName, value, calibriNormal,  numFields, fieldsTotal, page);
                         break;

                     case SPFieldType.DateTime:
                         value = General.FormatDateValue(WFID, value);
                         PDF.DrawUserControlsPDF(WFID, doc, columnName, value, calibriNormal, numFields, fieldsTotal, page);
                         break;

                     case SPFieldType.Boolean:

                         value = General.FormatCheckBoxValue(WFID, value);
                         PDF.DrawCheckboxControlPDF(WFID, doc, columnName, value, calibriNormal, page);
                         break;

                     case SPFieldType.User:
                         value = General.FormatUserValue(WFID, value);
                         PDF.DrawUserControlsPDF(WFID, doc, columnName, value, calibriNormal, numFields, fieldsTotal, page);
                         break;

                     case SPFieldType.Choice:
                         PDF.DrawUserControlsPDF(WFID, doc, columnName, value, calibriNormal, numFields, fieldsTotal, page);
                         break;
                     
                  
                     case SPFieldType.Note:
                         PDF.DrawUserControlsPDF(WFID, doc, columnName, value, calibriNormal, numFields, fieldsTotal, page);
                         break;
                  

                     default:
                         string message = "This type of field is not implemented -> " + fieldType.ToString();
                         General.SaveErrorsLog(string.Empty, "DrawControlsType() - " + message);
                         break;
                 }
             }
             catch (Exception ex)
             {
                 General.SaveErrorsLog(WFID, "DrawControlsType() - " + ex.Message);
             }
         }

        //--------------------------------------------
        // LINK TO WORKFLOW (CR24)
        //--------------------------------------------
        private static void DrawLinkToWorkFlow(string WFID, SPListItem item, msTextSharp.Document doc, msTextSharp.Font calibriBoldTitle, PdfWriter wri, msTextSharp.Rectangle page, msTextSharp.Font calibriNormal, Dictionary<string, string> parameters, string wfName)
        {
            try
            {
                string columnToPdf = "Linked to WFID(s)";
                string linkToWFInformation = string.Empty;
                SPFieldType fieldType = SPFieldType.Note;

                if (item["LinkToWorkflow"] != null)
                {
                    linkToWFInformation = item["LinkToWorkflow"].ToString();

                    if (linkToWFInformation.Contains("|"))
                        linkToWFInformation = linkToWFInformation.ToString().Replace("|", ", ");

                    DrawControlsType(columnToPdf, fieldType, linkToWFInformation, WFID, doc, calibriNormal, 1, 1, page);
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawLinkToWorkFlow() - " + ex.Message);
            }

        }

        #endregion

        #region <ACTORS>

        //--------------------------------------------
        //ACTORS + Step Descriptions (CR37)
        //--------------------------------------------
        private static void DrawActors(string WFID, SPListItem item, msTextSharp.Document doc, msTextSharp.Font calibriBoldTitle, PdfWriter wri, msTextSharp.Rectangle page, msTextSharp.Font calibriNormal, Dictionary<string, string> parameters, List<string> groupsNameList)
        {
            try
            {
   
              
                    int numSteps = groupsNameList.Count;

                    // CR37 -> Getting all Descriptions 
                    Dictionary<int, string> descriptionDictionary = SP.GetWorkflowInitialStepDescriptions(item,WFID);

                    //------------------------------------------------------------
                    //REGION HEADER
                    //------------------------------------------------------------
                    PDF.DrawHeaderSubregionPDF(WFID, doc, wri, "ACTORS", calibriBoldTitle);

                    for (int i = 0; i < numSteps; i++)
                    {
                        string groupName = string.Empty;
                        string userName = string.Empty;

                        //Group
                        groupName = GetGroupNameDefinition(groupsNameList[i].ToString(), parameters, WFID);

                        if (string.IsNullOrEmpty(groupName))
                            groupName = groupsNameList[i].ToString();
                        
                        //User
                        userName = SP.GetWorkflowStepAssignedTo(item, WFID, (i + 1));

                        if (!string.IsNullOrEmpty(userName))
                            userName = General.FormatUserValue(WFID, userName);
                        

                        //Description of specific Step
                        string description = descriptionDictionary[i + 1];
                        string[] stringSeparators = new string[] { "\r\n" };
                        string[] stepDescriptionLines = description.Split(stringSeparators, StringSplitOptions.None);


                        PDF.DrawActorPDF(WFID, groupName, userName, calibriNormal, doc, page, i, numSteps, stepDescriptionLines);

                    }
                }
            
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawActors() - " + ex.Message.ToString());
            }

        }

        public static string GetGroupNameDefinition(string ADGroupName, Dictionary<string, string> parameters, string WFID)
        {
            string groupname = string.Empty;

            try
            {
                List<string> keyList = new List<string>(parameters.Keys);

                if (keyList.Contains(ADGroupName.ToLower()))
                    groupname = parameters.FirstOrDefault(x => x.Key == ADGroupName.ToLower()).Value;
                else
                    groupname = ADGroupName.ToLower();
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetGroupNameDefinition() " + ex.Message);
            }

            return groupname;
        }

        #endregion

        #region <COMMENTS>

        public static void DrawComments(string WFID, SPListItem item, msTextSharp.Document doc, msTextSharp.Font calibriBoldTitle, PdfWriter wri, msTextSharp.Rectangle page, msTextSharp.Font calibriNormal, string wfName, SPWeb Web, msTextSharp.Font calibriBoldNormal, Dictionary<string, string> parameters, SPList logList,List<string> groupNames)
        {
            try
            {
                
                    List<List<string>> commentsList = new List<List<string>>();
                    commentsList = SP.GetPreviousComments(Web, WFID, logList, parameters, groupNames);

                    if (commentsList.Count > 0)
                    {
                        //------------------------------------------------------------
                        //REGION HEADER
                        //------------------------------------------------------------
                        PDF.DrawHeaderSubregionPDF(WFID, doc, wri, "COMMENTS", calibriBoldTitle);

                        foreach (List<string> commentToPaint in commentsList)
                        {
                            PDF.DrawCommentsPDF(WFID, calibriNormal, calibriBoldNormal, doc, page, commentToPaint);
                        }
                    }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawComments() - " + ex.Message.ToString());
            }

        }
        //--------------------------------------------
        //COMMENTS -> CLOSURE RS37
        //--------------------------------------------
        public static void DrawCommentsClosure(string WFID, SPListItem item, msTextSharp.Document doc, msTextSharp.Font calibriBoldTitle, PdfWriter wri, msTextSharp.Rectangle page, msTextSharp.Font calibriNormal, string wfName, SPWeb Web, msTextSharp.Font calibriBoldNormal, Dictionary<string, string> parameters, SPList logList)
        {
            try
            {
                    List<List<string>> commentsList = new List<List<string>>();
                    commentsList = SP.GetPreviousCommentsClosure(WFID, logList);

                    if (commentsList.Count > 0)
                    {
                        //------------------------------------------------------------
                        //REGION HEADER
                        //------------------------------------------------------------
                        PDF.DrawHeaderSubregionPDF(WFID, doc, wri, "COMMENTS AFTER CLOSURE", calibriBoldTitle);

                        foreach (List<string> commentToPaint in commentsList)
                        {
                            PDF.DrawCommentsClosurePDF(WFID, calibriNormal, calibriBoldNormal, doc, page, commentToPaint);
                        }
                    }
            
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawComments() - " + ex.Message.ToString());
            }

        }


        #endregion

    }
}
