using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.SharePoint;

using PdfSharp;
using msPdfSharp = PdfSharp.Pdf;
using PdfSharp.Drawing;

using msTextSharp = iTextSharp.text;
using iTextSharp.text.pdf;

using msWord = Microsoft.Office.Interop.Word;
using msExcel = Microsoft.Office.Interop.Excel;

namespace ESMA.Paperless.PrintProcess.v16
{
    class AttachDocuments
    {

        #region <DELETE FOLDERS>

        public static void DeleteDirectory(string pathDirectory)
        {
            try
            {
                var dir = new DirectoryInfo(pathDirectory);
                dir.Attributes = dir.Attributes & ~FileAttributes.ReadOnly;
                dir.Delete(true);

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(string.Empty, "DeleteDirectory() - " + ex.Message.ToString());
            }

        }

        #endregion

        #region <DOWNLOAD DOCUMENTS>

        public static void GetDocumentsFromDocumentLibrary(string WFID, SPList myList, SPFolder folder, string pathWFID)
        {
            try
            {

                foreach (SPFolder subfolder in folder.SubFolders)
                {
                    //Main
                    string subFolderName = subfolder.Name.ToString();

                    if (subfolder.ItemCount > 0)
                    {
                        //"C:\temp\_logsGSA\PDF_DOCUMENTS\2\Main"
                        string pathSubfolder = System.IO.Path.Combine(pathWFID, subFolderName);
                        General.GenerateWFDirectory(WFID, pathSubfolder);

                        foreach (SPFile file in subfolder.Files)
                        {
                            string fileName = file.Item.Name;
                 

                            //"C:\temp\_logsGSA\PDF_DOCUMENTS\2\Main\test.docx"
                            string pathDocument = System.IO.Path.Combine(pathSubfolder, fileName);

                            //-----------------------------------------
                            DownloadDocuments(file, pathDocument, WFID);
                            //-----------------------------------------

                        }

                    }
     
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetDocumentsFromDocumentLibrary() - " + ex.Message.ToString());
            }
        }

        public static void DownloadDocuments(SPFile File, string pathDocument, string WFID)
        {
            FileStream fstream = null;

            try
            {

                ////Start the Impersonation
                //WindowsImpersonationContext GlobalWIC = General.StartImpersonation(strDomain, strUser, strPassword);

                byte[] binfile = File.OpenBinary();
                fstream = new FileStream(pathDocument, FileMode.Create, FileAccess.ReadWrite);
                fstream.Write(binfile, 0, binfile.Length);
                fstream.Close();

                //General.EndImpersonation(GlobalWIC);

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DownloadDocuments() - " + ex.Message.ToString());
            }
            finally
            {

                fstream.Dispose();
            }
        }

        #endregion

        #region <INDEX>

        public static void GeneratePDF_Index(string WFID, SPListItem item, string WFIDPath, string printedDocumentName, SPWeb Web, msTextSharp.Font calibriBold, msTextSharp.Font calibriNormal, SPFolder folder, string wfName, Dictionary<string, string> parameters, List<string> headerSectionIndexList, string[] documentsExtensionLists)
        {
            try
            {

                string indexPDFDocument = printedDocumentName.Replace(".pdf", null) + "_index.pdf";
                string indexPDFPath = System.IO.Path.Combine(WFIDPath, indexPDFDocument);

                SPList myList = item.ParentList;
                int numDocumentsType = SP.CountSubFolders(WFID, Web, myList);

                if (numDocumentsType > 0)
                {
                    //------------------------------------------------------------------------------------
                    ExportDataToPDF_Index(indexPDFPath, WFID, WFIDPath, item, Web, calibriBold, calibriNormal, numDocumentsType, folder, wfName, parameters, headerSectionIndexList, documentsExtensionLists);
                    //------------------------------------------------------------------------------------
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GeneratePDF_Index() - " + ex.Message.ToString());
            }


        }

        public static void ExportDataToPDF_Index(string indexPDFPath, string WFID, string WFIDPath, SPListItem item, SPWeb Web, msTextSharp.Font calibriBold, msTextSharp.Font calibriNormal, int numDocumentsType, SPFolder folder, string wfName, Dictionary<string, string> parameters, List<string> headerSectionIndexList, string[] documentsExtensionList)
        {
            msTextSharp.Document doc = new msTextSharp.Document(iTextSharp.text.PageSize.LETTER, 40, 20, 42, 35);

            try
            {

                    //Generate Document class object and set its size to letter and give space left, right, Top, Bottom Margin
                PdfWriter wri = PdfWriter.GetInstance(doc, new FileStream(indexPDFPath, FileMode.Create));
                    doc.Open();//Open Document to write

                    msTextSharp.Rectangle page = doc.PageSize;

                    //------------------------------------------------------------
                    //PDF HEADER
                    //------------------------------------------------------------
                    PDF.DrawHeaderPrincipalPDF(WFID, doc, wfName, wri, page, calibriBold, "WFID");
                   
                   

                    for (int i = 0; i < headerSectionIndexList.Count; i++)
                    {
                       int contDocuments = 0;
                       bool GeneratePage = false;
                       string documentTypeSearched = headerSectionIndexList[i].ToString();
                      string documentTypeIndexTitle =string.Empty;

                       if (parameters.ContainsKey("RS Print Title - " + documentTypeSearched))
                           documentTypeIndexTitle = parameters["RS Print Title - " + documentTypeSearched];
                       else
                           documentTypeIndexTitle = documentTypeSearched;

                        string subFolderPath = System.IO.Path.Combine(WFIDPath, documentTypeSearched);

                        if (System.IO.Directory.Exists(subFolderPath))
                        {

                            //Index Region Header
                            PDF.DrawHeaderIndexPDF(WFID, doc, wri, calibriBold, documentTypeIndexTitle, page);

                            //------------------------------------------------------------------------
                            List<string> documentsTitleList = new List<string>();
                            documentsTitleList = SP.GetTitleDocuments(WFID, folder, documentTypeSearched);
                            //------------------------------------------------------------------------

                            for (int j = 0; j < documentsTitleList.Count; j++)
                            {
                                string documentTitle = documentsTitleList[j].ToString();
                                contDocuments++;

                                if (!(General.IsValidExtension(documentsTitleList[j], WFID, documentsExtensionList)))
                                    documentTitle = documentTitle + " (not printed)";
                                else
                                {
                                    if (IsPasswordProtectedDocument(System.IO.Path.Combine(subFolderPath, documentTitle), WFID))
                                        documentTitle = documentTitle + " (not printed - password protected)";
                                    GeneratePage = true;
                                }
                                
                                //---------------------------------------------------------------------------
                                PDF.DrawDocumentTitlesPDF(WFID, doc, calibriNormal, documentTitle, page, contDocuments.ToString());
                                //---------------------------------------------------------------------------
                            }

                            if (GeneratePage == true)
                            {
                                //---------------------------------------------------------------------------
                                GeneratePDF_Page(WFIDPath, documentTypeSearched, WFID, documentTypeIndexTitle);
                                //---------------------------------------------------------------------------
                            }
                        }
                        
                    }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "ExportDataToPDF_Index() - " + ex.Message.ToString());
            }

            finally
            {
                if (doc != null)
                {
                    //Close document and writer
                    doc.Close();
                    doc.Dispose();
                }

            }
        }
             
        #endregion

        #region <PAGES>

        private static void GeneratePDF_Page(string WFIDPath, string documentTypeSearched, string WFID, string documentTypeIndexTitle)
        {
            try
            {
                string pageNamePDF = "page_" + documentTypeSearched + ".pdf";
                string pdfPagePath = System.IO.Path.Combine(WFIDPath, pageNamePDF);

                //------------------------------------------------------------------------------------
                ExportDataToPDF_Pages(pdfPagePath, documentTypeIndexTitle, WFID);
                //------------------------------------------------------------------------------------
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GeneratePDF_Page() - " + ex.Message.ToString());
            }


        }

        public static void ExportDataToPDF_Pages(string pdfFilePath, string typeDocument, string WFID)
        {
            msTextSharp.Document doc = new msTextSharp.Document(iTextSharp.text.PageSize.LETTER, 40, 20, 42, 35);

            try
            {
                //Generate Document class object and set its size to letter and give space left, right, Top, Bottom Margin
                PdfWriter wri = PdfWriter.GetInstance(doc, new FileStream(pdfFilePath, FileMode.Create));
                doc.Open();//Open Document to write

                //TITLE BOLD
                msTextSharp.Font calibriBoldTitle = PDF.GetBoldFontPDF(WFID, 60f);

                //--------------------------------------------------------------------------------------------------------
                PDF.DrawPagePDF(WFID, doc, calibriBoldTitle, typeDocument);
                //--------------------------------------------------------------------------------------------------------

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "ExportDataToPDF_Pages() - " + ex.Message.ToString());
            }

            finally
            {
                //Close document and writer
                doc.Close();
                doc.Dispose();

            }
        }

        #endregion

        #region <CONVERT DOCUMENTS TO PDF>

        public static void GeneratePrintDocument(string WFID, string WFIDPath, SPListItem item, SPWeb Web, string printedDocumentName, SPFolder folder, List<string> headerSectionIndexList)
        {
            bool isAnyError = false;
            List<string> pdfsPathList = new List<string>();

            try
            {

                bool indexAdded = false;

                #region <ADD PDF FORM>

                string PDFFormName = printedDocumentName.Replace(".pdf", "_form.pdf");
                string PDFFormPath = System.IO.Path.Combine(WFIDPath, PDFFormName);

                if (System.IO.File.Exists(PDFFormPath))
                    pdfsPathList.Add(PDFFormPath);

                #endregion

                #region <ADD INDEX + DOCUMENTS>

                for (int i = 0; i < headerSectionIndexList.Count; i++)
                {
                    string documentTypeSearched = headerSectionIndexList[i].ToString();

                    //------------------------------------------------------------------------
                    List<string> documentsTitleList = new List<string>();
                    documentsTitleList = SP.GetTitleDocuments(WFID, folder, documentTypeSearched);
                    //------------------------------------------------------------------------

                    if (documentsTitleList.Count > 0)
                    {
                        if (indexAdded == false)
                        {
                            #region <ADD PDF INDEX>

                            string indexPDFDocument = printedDocumentName.Replace(".pdf", null) + "_index.pdf";
                            string indexPDFPath = System.IO.Path.Combine(WFIDPath, indexPDFDocument);

                            if (System.IO.File.Exists(indexPDFPath))
                                pdfsPathList.Add(indexPDFPath);
                            
                            #endregion

                            indexAdded = true;
                        }

                        //Documents
                        try
                        {
                            #region <ADD DOCUMENTS>

                            string subFolderPath = System.IO.Path.Combine(WFIDPath, documentTypeSearched);

                            if (System.IO.Directory.Exists(subFolderPath))
                            {
                                string documentPagePath = System.IO.Path.Combine(WFIDPath, "page_" + documentTypeSearched + ".pdf");
                                int cont = 0;

                                if (System.IO.File.Exists(documentPagePath))
                                    pdfsPathList.Add(documentPagePath);
                                

                                foreach (string documenTitle in documentsTitleList)
                                {

                                    string documentPath = System.IO.Path.Combine(subFolderPath, documenTitle);
                                    string documentExtension = Path.GetExtension(documentPath);
                                    string PDFextension = ".pdf";

                                    if (documentExtension != PDFextension)
                                    {
                                        cont++;
                                        string newName = cont + "_" + Path.GetFileNameWithoutExtension(documentPath) + ".pdf";
                                        string documentPDFPath = System.IO.Path.Combine(subFolderPath, newName);

                                        //----------------------------------------------------------------------------
                                        isAnyError = AttachDocuments.ConvertDocumentToPDF(documentPath, documentPDFPath, documentExtension, cont, WFID);
                                        //----------------------------------------------------------------------------

                                        if (isAnyError)
                                            break;

                                        if (System.IO.File.Exists(documentPDFPath))
                                            pdfsPathList.Add(documentPDFPath);
                                        
                                    }
                                    else if (documentExtension == PDFextension)
                                    {
                                        if (System.IO.File.Exists(documentPath))
                                            pdfsPathList.Add(documentPath);
                                    }
                                }
                            }

                            #endregion
                        }
                        catch { }
                    }

                    if (isAnyError)
                        break;
                }

                #endregion

                #region <ADD PDF LOGs>

                string logsPDFDocument = printedDocumentName.Replace(".pdf", null) + "_logs.pdf";
                string logsPDFPath = System.IO.Path.Combine(WFIDPath, logsPDFDocument);

                if (System.IO.File.Exists(logsPDFPath))
                {
                    pdfsPathList.Add(logsPDFPath);

                }

                #endregion


                if ((pdfsPathList != null) && (!isAnyError))
                {
                    string printedDocumentPath = System.IO.Path.Combine(WFIDPath, printedDocumentName);

                    //MergeDocuments
                    AttachDocuments.MergeDocs(WFIDPath, pdfsPathList, printedDocumentName, WFID);

                    if (System.IO.File.Exists(printedDocumentPath))
                        UploadPrintDocument(printedDocumentPath, printedDocumentName, folder, WFID, Web);

                    else
                        General.SaveErrorsLog(WFID, "It has not been uploaded the document '" + printedDocumentName + "' because it does not exist.");
                }
                else
                {
                    General.SaveErrorsLog(WFID, "Error creating the PDF for the WF '" + WFID + "'.");
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, " GeneratePrintDocument() - " + ex.Message.ToString());
            }

        }

        public static bool ConvertDocumentToPDF(string documentPath, string pathDocumentPDF, string documentExtension , int cont, string WFID)
        {
            bool isAnyError = false;

            try
            {
                if ((System.IO.File.Exists(documentPath)))
                {
                    #region <WORD>

                    // Verify the document added is a Word document before starting the conversion.
                    if ((documentExtension.ToLower() == ".docx") || (documentExtension.ToLower() == ".doc"))
                    {
                        isAnyError = ConvertWord(documentPath, cont, ".pdf", WFID);
                    }

                    #endregion

                    #region <EXCEL>

                    else if (documentExtension.ToLower().Contains(".xlsx") || documentExtension.ToLower().Contains(".xls"))
                    {
                        isAnyError = ConvertExcel(documentPath, cont, ".pdf", WFID);
                    }

                    #endregion

                    #region <IMAGES>

                    else if ((documentExtension.ToLower().Contains(".png") || documentExtension.ToLower().Contains(".gif") || documentExtension.ToLower().Contains(".jpg") || documentExtension.ToLower().Contains(".jpeg")
                         || documentExtension.ToLower().Contains(".bmp")))
                    {
                        ConvertImages(documentPath, pathDocumentPDF, WFID);
                    }

                    #endregion

                }
                else
                {
                    General.SaveErrorsLog(WFID, "The following document has not been downloaded: " + documentPath);
                }


            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "ConvertDocumentToPDF() - " + ex.Message.ToString());
            }

            return isAnyError;
        }

        private static void ConvertImages(string documentPath, string documentPDFPath, string WFID)
        {
            msPdfSharp.PdfDocument doc = new msPdfSharp.PdfDocument();

            try
            {

                doc.Pages.Add(new msPdfSharp.PdfPage());
                XGraphics xgr = XGraphics.FromPdfPage(doc.Pages[0]);
                XImage img = XImage.FromFile(documentPath);

                xgr.DrawImage(img, 0, 0);
                doc.Save(documentPDFPath);

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "ConvertImages() - " + ex.Message.ToString());
            }
            finally
            {
                doc.Close();
                doc.Dispose();
            }
        }

        private static bool ConvertWord(string documentPath, int cont, string PDFextension, string WFID)
        {
            bool isAnyError = false;
            // Generate a new Microsoft Word application object
            Microsoft.Office.Interop.Word.Application word = new Microsoft.Office.Interop.Word.Application();

            // C# doesn't have optional arguments so we'll need a dummy value
            object oPasswordDocument = "RSPassword";
            object oMissing = System.Reflection.Missing.Value;

            msWord.Document doc = null;

            try
            {
                FileInfo info = new FileInfo(documentPath);
                word.Visible = false;
                word.ScreenUpdating = false;

                // Cast as Object for word Open method
                Object filename = (Object)info.FullName;

                // Use the dummy value as a placeholder for optional arguments
                doc = word.Documents.Open(ref filename, ref oMissing,
                     ref oMissing, ref oMissing, oPasswordDocument, ref oMissing, ref oMissing,
                   ref oMissing, ref oMissing, ref oMissing, ref oMissing, ref oMissing,
                     ref oMissing, ref oMissing, ref oMissing, ref oMissing);

                doc.Activate();


                object outputFileName = null;

                string nameDocument = Path.GetFileName(documentPath);
                string _newNameDocument = nameDocument.Replace(nameDocument, (cont + "_" + nameDocument));

                if (nameDocument.ToLower().Contains(".docx"))
                {
                    info = new FileInfo(info.FullName.ToLower().Replace(nameDocument.ToLower(), _newNameDocument.ToLower()));
                    outputFileName = info.FullName.ToLower().Replace(".docx", PDFextension);

                }
                else
                {
                    info = new FileInfo(info.FullName.ToLower().Replace(nameDocument.ToLower(), _newNameDocument.ToLower()));
                    outputFileName = info.FullName.ToLower().Replace(".doc", PDFextension);
                }


                object fileFormat = msWord.WdSaveFormat.wdFormatPDF;


                // Save document into PDF Format
                doc.SaveAs(ref outputFileName,
                    ref fileFormat, ref oMissing, ref oMissing,
                    ref oMissing, ref oMissing, ref oMissing, ref oMissing,
                    ref oMissing, ref oMissing, ref oMissing, ref oMissing,
                    ref oMissing, ref oMissing, ref oMissing, ref oMissing);



                // Close the Word document, but leave the Word application open.
                // doc has to be cast to type _Document so that it will find the
                // correct Close method.                
                object saveChanges = msWord.WdSaveOptions.wdDoNotSaveChanges;
                ((msWord._Document)doc).Close(ref saveChanges, ref oMissing, ref oMissing);
                doc = null;

                // word has to be cast to type _Application so that it will find
                // the correct Quit method.
                // ((_Application)word).Quit(ref oMissing, ref oMissing, ref oMissing);
                ((msWord._Application)word).Quit(ref oMissing, ref oMissing, ref oMissing);
                word = null;
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, " ConvertWord() - " + ex.Message.ToString());
                if (!ex.ToString().Contains("0x800A1520"))
                    isAnyError = true;
            }
            finally
            {
                //Cierra la aplicación de Word
                //((msWord._Application)word).NormalTemplate.Saved = true;

                // Close the workbook object.
                if ((((msWord._Document)doc)) != null)
                {
                    ((msWord._Document)doc).Close(false, oMissing, oMissing);
                    doc = null;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Quit Excel and release the ApplicationClass object.
                if (((msWord._Application)word) != null)
                {
                    ((msWord._Application)word).Quit(false, oMissing, oMissing);
                    word = null;
                }

            }

            return isAnyError;
        }

        private static bool ConvertExcel(string documentPath, int cont,  string PDFextension, string WFID)
        {
            bool isAnyError = false;
            msExcel.Application excel = new msExcel.Application();
            msExcel.Workbook wbk = null;

            // C# doesn't have optional arguments so we'll need a dummy value
            object oMissing = System.Reflection.Missing.Value;
            object oPasswordDocument = "RSPassword";

            try
            {
                //--------------------------------------------
                //Excel Metadata
                //--------------------------------------------
                //bool paramOpenAfterPublish = false;
                //bool paramIncludeDocProps = true;
                //bool paramIgnorePrintAreas = true;
                object paramFromPage = Type.Missing;
                object paramToPage = Type.Missing;

                FileInfo excelFile = new FileInfo(documentPath);

                excel.Visible = false;
                excel.ScreenUpdating = false;
                excel.DisplayAlerts = false;


                // Cast as Object for word Open method
                Object filename = (Object)excelFile.FullName;

                //excel.FileValidation = MsoFileValidationMode.msoFileValidationSkip;

                wbk = excel.Workbooks.Open(filename.ToString(), oMissing, oMissing, oMissing,
                    oPasswordDocument, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing,
                    oMissing, oMissing, oMissing);

                wbk.Activate();

                object outputFileName = null;

                msExcel.XlFixedFormatType fileFormat = msExcel.XlFixedFormatType.xlTypePDF;
                msExcel.XlFixedFormatQuality paramExportQuality = msExcel.XlFixedFormatQuality.xlQualityStandard;


                string nameDocument = Path.GetFileName(documentPath);
                string _newNameDocument = nameDocument.Replace(nameDocument, (cont + "_" + nameDocument));
                //FileInfo newExcelFile = new FileInfo(_newPathDocument);

                if (nameDocument.ToLower().Contains(".xlsx"))
                {
                    excelFile = new FileInfo(excelFile.FullName.ToLower().Replace(nameDocument.ToLower(), _newNameDocument.ToLower()));
                    outputFileName = excelFile.FullName.ToLower().Replace(".xlsx", PDFextension);
                }
                else
                {
                    excelFile = new FileInfo(excelFile.FullName.ToLower().Replace(nameDocument.ToLower(), _newNameDocument.ToLower()));
                    outputFileName = excelFile.FullName.ToLower().Replace(".xls", PDFextension);

                }

                if (wbk != null)
                {
                    //Save document into PDF Format
                    wbk.ExportAsFixedFormat(fileFormat, outputFileName,
                        paramExportQuality, oMissing, oMissing,
                        oMissing, oMissing, false,
                        oMissing);

                    //wbk.ExportAsFixedFormat(fileFormat, outputFileName, paramExportQuality,
                    //paramIncludeDocProps, paramIgnorePrintAreas, paramFromPage,
                    //paramToPage, paramOpenAfterPublish,
                    //oMissing);
                }

                object saveChanges = msExcel.XlSaveAction.xlDoNotSaveChanges;
                ((msExcel._Workbook)wbk).Close(saveChanges, oMissing, oMissing);
                wbk = null;
            }
            catch (Exception ex)
            {
                if (!ex.ToString().Contains("0x800A03EC"))
                    isAnyError = true;
                General.SaveErrorsLog(WFID, " ConvertExcel() - " + ex.Message.ToString());
            }
            finally
            {

                // Close the workbook object.
                if (((msExcel._Workbook)wbk) != null)
                {
                    ((msExcel._Workbook)wbk).Close(false, oMissing, oMissing);
                    wbk = null;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Quit Excel and release the ApplicationClass object.
                if (((msExcel._Application)excel) != null)
                {
                    ((msExcel._Application)excel).Quit();
                    excel = null;
                }
            }

            return isAnyError;
        }

        public static void StartExcelProccess()
        {
            try
            {
                System.Diagnostics.Process[] myProcesses;
                myProcesses = System.Diagnostics.Process.GetProcessesByName("EXCEL.EXE");

                foreach (System.Diagnostics.Process instance in myProcesses)
                {
                    instance.CloseMainWindow();
                    instance.Kill();
                    instance.Close();
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(string.Empty, "MergeDocs() - " + ex.Message.ToString());
            }

        }

        //Merge Documents
        public static void MergeDocs(string WFIDPath, List<string> filePathList, string namePDFDocument, string WFID)
        {
            msTextSharp.Document document = new msTextSharp.Document();

            try
            {
                string destinationfile = System.IO.Path.Combine(WFIDPath, namePDFDocument);

                PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(destinationfile, FileMode.Create));
                document.Open();

                PdfContentByte cb = writer.DirectContent;
                PdfImportedPage page;

                int n = 0;
                int rotation = 0;

                //Loops for each file that has been listed
                foreach (string filePath in filePathList)
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        string _realExtension = System.IO.Path.GetExtension(filePath);

                        // we Generate a reader for the document
                        PdfReader reader = new PdfReader(filePath);
                        PdfReader.unethicalreading = true; 

                        string fileName = System.IO.Path.GetFileName(filePath);

                        //--------------------------------------------------------------------------------------------
                        //PDF ORIENTATION
                        //--------------------------------------------------------------------------------------------
                        bool _notScanner = false;

                        msTextSharp.Rectangle Rect = reader.GetPageSize(1);
                        //Compare the dimensions of the rectangle returned. For simplicity I'm saying that a square object is portraint, too
                        if (Rect.Height >= Rect.Width)
                        {
                            _notScanner = true;
                        }
                        else
                        {
                            _notScanner = false;
                        }

                        //--------------------------------------------------------------------------------------------

                        //Gets the number of pages to process
                        n = reader.NumberOfPages;

                        int i = 0;

                        while (i < n)
                        {
                            i++;
                            document.SetPageSize(reader.GetPageSizeWithRotation(1));
                            document.NewPage();

                            //Insert to Destination on the first page
                            if (i == 1)
                            {
                                msTextSharp.Chunk fileRef = new msTextSharp.Chunk(" ");
                                fileRef.SetLocalDestination(fileName);
                                document.Add(fileRef);
                            }

                            page = writer.GetImportedPage(reader, i);
                            rotation = reader.GetPageRotation(i);



                            if (_notScanner == true)
                            {
                                if (rotation == 90 || rotation == 270)
                                {
                                    cb.AddTemplate(page, 0, -1f, 1f, 0, 0, reader.GetPageSizeWithRotation(i).Height);
                                }
                                else
                                {
                                    cb.AddTemplate(page, 1f, 0, 0, 1f, 0, 0);
                                }
                            }
                            else
                            {
                                cb.AddTemplate(page, 0, 1.0F, -1.0F, 0, reader.GetPageSizeWithRotation(1).Width, 0);
                            }


                        }
                    }
                    else
                    {
                        General.SaveErrorsLog(WFID, "MergeDocs() - The path '" + filePath + "' does not exist.");
                    }
                }


            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "MergeDocs() - " + ex.Message.ToString());
            }
            finally
            {
                document.Close();
                document.Dispose();
            }
        }

        #endregion

        #region <UPLOAD PRINT DOCUMENT>

        public static void UploadPrintDocument(string printedDocumentPath, string printedDocumentName, SPFolder oFolder, string WFID, SPWeb Web)
        {

            try
            {
                if (oFolder.Exists == true)
                {
                    System.IO.FileStream fileStream = System.IO.File.OpenRead(printedDocumentPath);
                    string documentURL = General.CombineURL(WFID, oFolder.Url, printedDocumentName);
                    string stepNumber = oFolder.Item["StepNumber"].ToString();

                    using (new DisabledItemEventsScope())
                    {
                        
                        Web.AllowUnsafeUpdates = true;

                        SPFile oFile = oFolder.Files.Add(documentURL, fileStream, true);
                        SPListItem oItem = oFile.Item;
                        SP.SetDocumentsMetadata(ref oItem, WFID, "Printed Document", stepNumber);

                        using (new DisabledItemEventsScope())
                        {
                            oItem.Update();
                            fileStream.Close();
                        }

                        Web.AllowUnsafeUpdates = false;
                    }
                }

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "UploadPrintDocument() - " + ex.Message.ToString());
            }
        }

        #endregion

        #region <PASSWORD PROTECTED>
        /// <summary>
        /// Checks if an office document (word or excel) is protected with password 
        /// </summary>
        private static bool IsPasswordProtectedDocument(string documentPath, string WFID)
        {
            bool isPasswordProtected = false;
            try
            {
                if ((System.IO.File.Exists(documentPath)))
                {
                    string documentExtension = Path.GetExtension(documentPath);
                    if ((documentExtension.ToLower() == ".docx") || (documentExtension.ToLower() == ".doc"))
                    {
                        isPasswordProtected = IsPasswordProtectedWord(documentPath, WFID);
                    }
                    else if (documentExtension.ToLower().Contains(".xlsx") || documentExtension.ToLower().Contains(".xls"))
                    {
                        isPasswordProtected = IsPasswordProtectedExcel(documentPath, WFID);
                    }

                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "IsPasswordProtectedDocument() - " + ex.Message.ToString());
            }

            return isPasswordProtected;
        }

        /// <summary>
        /// Checks if a Word document is protected with password 
        /// </summary>
        private static bool IsPasswordProtectedWord(string documentPath, string WFID)
        {
            bool isPasswordProtectedWord = false;
            Microsoft.Office.Interop.Word.Application word = new Microsoft.Office.Interop.Word.Application();

            object oMissing = System.Reflection.Missing.Value;
            object oPasswordDocument = "RSPassword";

            msWord.Document doc = null;

            try
            {
                FileInfo info = new FileInfo(documentPath);
                word.Visible = false;
                word.ScreenUpdating = false;

                // Cast as Object for word Open method
                Object filename = (Object)info.FullName;

                try
                {
                    doc = word.Documents.Open(ref filename, ref oMissing,
                         ref oMissing, ref oMissing, oPasswordDocument, ref oMissing, ref oMissing,
                       ref oMissing, ref oMissing, ref oMissing, ref oMissing, ref oMissing,
                         ref oMissing, ref oMissing, ref oMissing, ref oMissing);
                }
                catch (Exception ex)
                {
                    if (ex.ToString().Contains("0x800A1520"))
                        isPasswordProtectedWord = true;
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "IsPasswordProtectedWord() - " + ex.Message.ToString());
            }
            finally
            {
                //Cierra la aplicación de Word
                //((msWord._Application)word).NormalTemplate.Saved = true;

                // Close the workbook object.
                if ((((msWord._Document)doc)) != null)
                {
                    ((msWord._Document)doc).Close(false, oMissing, oMissing);
                    doc = null;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Quit Excel and release the ApplicationClass object.
                if (((msWord._Application)word) != null)
                {
                    ((msWord._Application)word).Quit(false, oMissing, oMissing);
                    word = null;
                }

            }
            return isPasswordProtectedWord;
        }

        /// <summary>
        /// Checks if an Excel document is protected with password 
        /// </summary>
        private static bool IsPasswordProtectedExcel(string documentPath, string WFID)
        {
            bool isPasswordProtectedExcel = false;

            msExcel.Application excel = new msExcel.Application();
            msExcel.Workbook wbk = null;

            object oMissing = System.Reflection.Missing.Value;
            object oPasswordDocument = "RSPassword";

            try
            {
                FileInfo excelFile = new FileInfo(documentPath);

                excel.Visible = false;
                excel.ScreenUpdating = false;
                excel.DisplayAlerts = false;

                Object filename = (Object)excelFile.FullName;

                try
                {
                    wbk = excel.Workbooks.Open(filename.ToString(), oMissing, oMissing, oMissing,
                        oPasswordDocument, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing,
                        oMissing, oMissing, oMissing);
                }
                catch (Exception ex)
                {
                    if (ex.ToString().Contains("0x800A03EC"))
                        isPasswordProtectedExcel = true;
                }
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "IsPasswordProtectedExcel() - " + ex.Message.ToString());
            }
            finally
            {
                // Close the workbook object.
                if (((msExcel._Workbook)wbk) != null)
                {
                    ((msExcel._Workbook)wbk).Close(false, oMissing, oMissing);
                    wbk = null;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Quit Excel and release the ApplicationClass object.
                if (((msExcel._Application)excel) != null)
                {
                    ((msExcel._Application)excel).Quit();
                    excel = null;
                }
            }
            return isPasswordProtectedExcel;
        }
        #endregion

    }
}
