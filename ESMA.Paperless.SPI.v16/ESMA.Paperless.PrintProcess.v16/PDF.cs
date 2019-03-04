using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PdfSharp;
using msPdfSharp = PdfSharp.Pdf;
using PdfSharp.Drawing;

using msTextSharp = iTextSharp.text;
using iTextSharp.text.pdf;

namespace ESMA.Paperless.PrintProcess.v16
{
    class PDF
    {
        

        public static msTextSharp.Font GetBoldFontPDF(string WFID, float size)
        {
            try
            {
                msTextSharp.Font calibriTitle = msTextSharp.FontFactory.GetFont("Calibri", size, msTextSharp.Font.BOLD);
                calibriTitle.Color = msTextSharp.BaseColor.BLACK;

                return calibriTitle;
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetBoldFontPDF() - " + ex.Message.ToString());
                return null;
            }

        }

        public static msTextSharp.Font GetNormalFontPDF(string WFID, float size)
        {
            try
            {
                msTextSharp.Font calibriNormal = msTextSharp.FontFactory.GetFont("Calibri", size);
                calibriNormal.Color = msTextSharp.BaseColor.BLACK;

                return calibriNormal;
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "GetNormalFontPDF() - " + ex.Message.ToString());
                return null;
            }

        }

        #region <HEADER>

        public static void DrawHeaderPrincipalPDF(string WFID, msTextSharp.Document doc, string wfName, PdfWriter wri, msTextSharp.Rectangle page, msTextSharp.Font calibriBoldTitle, string title)
        {

            try
            {
               
               
                PdfPTable headHeader = new PdfPTable(2);
                headHeader.TotalWidth = page.Width;

                msTextSharp.Paragraph paragraphTitleWF = new msTextSharp.Paragraph(wfName.ToUpper().ToString(), calibriBoldTitle);
                msTextSharp.Paragraph paragraphWFID = new msTextSharp.Paragraph(title.ToUpper() + ": " + WFID, calibriBoldTitle);

                //---------------------------------------------------------------------------------------------------------
                //Design MAIN Title
                //---------------------------------------------------------------------------------------------------------
                PdfPCell cTitle = new PdfPCell(paragraphTitleWF);
                cTitle.Border = msTextSharp.Rectangle.NO_BORDER;
                cTitle.VerticalAlignment = msTextSharp.Element.ALIGN_TOP;
                cTitle.HorizontalAlignment = msTextSharp.Element.ALIGN_LEFT;
                cTitle.PaddingBottom = 5f;
                cTitle.PaddingTop = 4f;

                headHeader.AddCell(cTitle);


                //--------------------------------------------------------
                //WFID
                //--------------------------------------------------------
                PdfPCell cWFID = new PdfPCell(paragraphWFID);
                cWFID.Border = msTextSharp.Rectangle.NO_BORDER;
                cWFID.VerticalAlignment = msTextSharp.Element.ALIGN_TOP;
                cWFID.HorizontalAlignment = msTextSharp.Element.ALIGN_RIGHT;
                cWFID.PaddingBottom = 5f;
                cWFID.PaddingTop = 4f;
                headHeader.AddCell(cWFID);

                //SUPERIOR LINE
                //--------------------------
                DrawSuperiorLinePDF(WFID, doc, wri);
                
                doc.Add(headHeader);// add paragraph to the document

                //INFERIOR LINE
                //--------------------------
                DrawInferiorLinePDF(WFID, doc, wri);
                
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawHeaderPrincipalPDF() - " + ex.Message.ToString());
            }

        }

        public static void DrawHeaderSubregionPDF(string WFID, msTextSharp.Document doc, PdfWriter wri, string regionTitle, msTextSharp.Font calibriBoldTitle)
        {
            try
            {
                msTextSharp.Rectangle page = doc.PageSize;
                PdfPTable headTable = new PdfPTable(1);
                headTable.TotalWidth = page.Width;

                msTextSharp.Paragraph paragraphTitle = new msTextSharp.Paragraph(regionTitle.ToUpper(), calibriBoldTitle);
                paragraphTitle.SpacingBefore = 20f;
             
                PdfPCell cHead = new PdfPCell(paragraphTitle);
                cHead.Border = msTextSharp.Rectangle.BOTTOM_BORDER;
                cHead.VerticalAlignment = msTextSharp.Element.ALIGN_TOP;
                cHead.HorizontalAlignment = msTextSharp.Element.ALIGN_LEFT;
                cHead.PaddingBottom = 5f;
                cHead.PaddingTop = 15f;
                headTable.AddCell(cHead);
                doc.Add(headTable);
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawHeaderSubregionPDF() - " + ex.Message.ToString());
            }

        }

        private static void DrawSuperiorLinePDF(string WFID, msTextSharp.Document doc, PdfWriter wri)
        {
            try
            {
                //Will hold our current x,y coordinates;
                float curY;
                float curX;

                //Get the current Y value
                curY = wri.GetVerticalPosition(true);

                //The current X is just the left margin
                curX = doc.LeftMargin;

                //Set a color fill
                wri.DirectContent.SetRGBColorStroke(0, 0, 0);
                //Set the x,y of where to start drawing
                wri.DirectContent.MoveTo(curX, curY);
                //Draw a line
                wri.DirectContent.LineTo(doc.PageSize.Width - doc.RightMargin, curY);
                //Fill the line in
                wri.DirectContent.Stroke();
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawSuperiorLinePDF() - " + ex.Message.ToString());
            }

        }

        private static void DrawInferiorLinePDF(string WFID, msTextSharp.Document doc, PdfWriter wri)
        {
            try
            {
                //Will hold our current x,y coordinates;
                float curY;
                float curX;

                //Repeat the above. curX never really changes unless you modify the document's margins
                curY = wri.GetVerticalPosition(true);
                //The current X is just the left margin
                curX = doc.LeftMargin;

                wri.DirectContent.SetRGBColorStroke(0, 0, 0);
                wri.DirectContent.MoveTo(curX, curY);
                wri.DirectContent.LineTo(doc.PageSize.Width - doc.RightMargin, curY);
                wri.DirectContent.Stroke();
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawInferiorLinePDF() - " + ex.Message.ToString());
            }

        }

        #endregion

        #region <FORM>

        //GENERAL FIELDS
        //--------------------------------------------------------------------------------------------
        public static void DrawCheckboxControlPDF(string WFID, msTextSharp.Document doc, string columnNameSP, string value, msTextSharp.Font calibriNormal, msTextSharp.Rectangle page)
        {
            try
            {
                PdfPTable tableElements = new PdfPTable(1);
                tableElements.TotalWidth = page.Width;

                msTextSharp.Paragraph paragraphCheckBox = new msTextSharp.Paragraph();

                msTextSharp.Phrase phraseGF = new msTextSharp.Phrase();
                msTextSharp.Chunk columnName = new msTextSharp.Chunk(columnNameSP + " : ", calibriNormal);
                msTextSharp.Chunk columnValue = new msTextSharp.Chunk(value, calibriNormal);
                phraseGF.Add(columnName);
                phraseGF.Add(columnValue);
                paragraphCheckBox.Add(phraseGF);
                paragraphCheckBox.Alignment = msTextSharp.Element.ALIGN_LEFT;
                paragraphCheckBox.SpacingBefore = 20f;
               
                PdfPCell cCheckBox = new PdfPCell(paragraphCheckBox);
                cCheckBox.Border = msTextSharp.Rectangle.NO_BORDER;
                cCheckBox.VerticalAlignment = msTextSharp.Element.ALIGN_TOP;
                cCheckBox.HorizontalAlignment = msTextSharp.Element.ALIGN_LEFT;
                cCheckBox.PaddingBottom = 5f;
                cCheckBox.PaddingTop = 4f;
                tableElements.AddCell(cCheckBox);
                doc.Add(tableElements);

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawCheckboxControlPDF() - " + ex.Message.ToString());
            }

        }

        public static void DrawUserControlsPDF(string WFID, msTextSharp.Document doc, string columnNameSP, string value, msTextSharp.Font calibriNormal, int numFields, int fieldsTotal, msTextSharp.Rectangle page)
        {
            try
            {
                PdfPTable tableGeneralFields = new PdfPTable(1);
                tableGeneralFields.TotalWidth = page.Width;

                msTextSharp.Paragraph paragraphGeneralFields = new msTextSharp.Paragraph();

                msTextSharp.Phrase phraseGF = new msTextSharp.Phrase();
                msTextSharp.Chunk columnName = new msTextSharp.Chunk(columnNameSP + ": ", calibriNormal);
                msTextSharp.Chunk columnValue = new msTextSharp.Chunk(value, calibriNormal);
                phraseGF.Add(columnName);
                phraseGF.Add(columnValue);
                paragraphGeneralFields.Add(phraseGF);
                paragraphGeneralFields.Alignment = msTextSharp.Element.ALIGN_LEFT;


                if ((numFields + 1) == fieldsTotal)
                    paragraphGeneralFields.SpacingAfter = 30f;
                


                PdfPCell cGeneralFields = new PdfPCell(paragraphGeneralFields);
                cGeneralFields.Border = msTextSharp.Rectangle.NO_BORDER;
                cGeneralFields.VerticalAlignment = msTextSharp.Element.ALIGN_TOP;
                cGeneralFields.HorizontalAlignment = msTextSharp.Element.ALIGN_LEFT;
                cGeneralFields.PaddingBottom = 5f;
                cGeneralFields.PaddingTop = 4f;
                tableGeneralFields.AddCell(cGeneralFields);
                doc.Add(tableGeneralFields);

            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawUserControlsPDF() - " + ex.Message.ToString());
            }

        }


        //ACTORS
        //--------------------------------------------------------------------------------------------
        public static void DrawActorPDF(string WFID, string groupName, string userName, msTextSharp.Font calibriNormal, msTextSharp.Document doc, msTextSharp.Rectangle page, int i, int totalGroups, string[] StepDescription)
        {
            try
            {
                PdfPTable tableActors = new PdfPTable(1);
                tableActors.TotalWidth = page.Width;
                msTextSharp.Paragraph paragraphActors = new msTextSharp.Paragraph();
                msTextSharp.Paragraph paragraphStepDescription = new msTextSharp.Paragraph();

                msTextSharp.Phrase phraseActor = phraseActor = new msTextSharp.Phrase();
                msTextSharp.Phrase phraseStepDescription = phraseStepDescription = new msTextSharp.Phrase();

                msTextSharp.Chunk columnName = new msTextSharp.Chunk(General.GetGroupADEquivalence(WFID,groupName) + ": ", calibriNormal);
                msTextSharp.Chunk columnValue = new msTextSharp.Chunk(userName, calibriNormal);
                phraseActor.Add(columnName);
                phraseActor.Add(columnValue);

                paragraphActors.Add(phraseActor);
                paragraphActors.Alignment = msTextSharp.Element.ALIGN_LEFT;

                foreach (string step in StepDescription)
                {
                    if (!string.IsNullOrEmpty(step))
                    {
                        msTextSharp.Chunk columnDescription = new msTextSharp.Chunk(step, calibriNormal);
                        phraseStepDescription.Add(columnDescription);
                        phraseStepDescription.Add(new msTextSharp.Chunk("\n"));
                    }
                }
                
                paragraphStepDescription.Add(phraseStepDescription);
                paragraphStepDescription.Alignment = msTextSharp.Element.ALIGN_RIGHT;

   
                if ((i + 1).Equals(totalGroups))
                    paragraphActors.SpacingAfter = 15f;
                

                PdfPCell cActors = new PdfPCell(paragraphActors);
                cActors.Border = msTextSharp.Rectangle.NO_BORDER;
                cActors.VerticalAlignment = msTextSharp.Element.ALIGN_TOP;
                cActors.PaddingBottom = 5f;
                cActors.PaddingTop = 4f;

                //Step decription
                PdfPCell cStepDescription = new PdfPCell(paragraphStepDescription);
                cStepDescription.Border = msTextSharp.Rectangle.NO_BORDER;
                cStepDescription.VerticalAlignment = msTextSharp.Element.ALIGN_TOP;
                cStepDescription.PaddingBottom = 5f;
                //cStepDescription.PaddingTop = 1f;
                cStepDescription.PaddingLeft = 20f;

                tableActors.AddCell(cActors);
                tableActors.AddCell(cStepDescription);


                doc.Add(tableActors);
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawActorPDF() - " + ex.Message.ToString());
            }

        }

        //COMMENTS
        //--------------------------------------------------------------------------------------------
        public static void DrawCommentsPDF(string WFID, msTextSharp.Font calibriNormal, msTextSharp.Font calibriBold, msTextSharp.Document doc, msTextSharp.Rectangle page, List<string> commentToPaint)
        {
            try
            {
                PdfPTable tableComments = new PdfPTable(1);
                tableComments.TotalWidth = page.Width;
                msTextSharp.Paragraph paragraphGeneralFieldComment = new msTextSharp.Paragraph();

                msTextSharp.Phrase phraseGFComment = new msTextSharp.Phrase();
                msTextSharp.Chunk columnDateHour = new msTextSharp.Chunk(commentToPaint[0] + " - ", calibriNormal);
                msTextSharp.Chunk columnAuthor = new msTextSharp.Chunk();
                if (!string.IsNullOrEmpty(commentToPaint[1]))
                {
                   columnAuthor = new msTextSharp.Chunk(commentToPaint[1] + " ", calibriBold);
                }

                phraseGFComment.Add(columnDateHour);
                phraseGFComment.Add(columnAuthor);

                if (commentToPaint.Count > 2)
                {
                    if (!string.IsNullOrEmpty(commentToPaint[2]))
                    {
                        string comment = General.FormatComment(WFID, commentToPaint[2]);

                        if (comment == "Restriction changed")
                        {
                            msTextSharp.Chunk columnAction = new msTextSharp.Chunk(commentToPaint[3] + ". ", calibriBold);
                            phraseGFComment.Add(columnAction);

                        }
                        else
                        {
                            msTextSharp.Chunk columnAction = new msTextSharp.Chunk(commentToPaint[3] + ": ", calibriBold);
                            msTextSharp.Chunk columnComment = new msTextSharp.Chunk(comment + ". ", calibriNormal);
                            phraseGFComment.Add(columnAction);
                            phraseGFComment.Add(columnComment);
                        }

                    }
                    else
                    {
                        msTextSharp.Chunk columnAction = new msTextSharp.Chunk(commentToPaint[3] + ". ", calibriBold);
                        phraseGFComment.Add(columnAction);
                    }
                }

                paragraphGeneralFieldComment.Add(phraseGFComment);

                PdfPCell cComments = new PdfPCell(paragraphGeneralFieldComment);
                cComments.Border = msTextSharp.Rectangle.NO_BORDER;
                cComments.VerticalAlignment = msTextSharp.Element.ALIGN_TOP;
                cComments.PaddingBottom = 5f;
                cComments.PaddingTop = 4f;
                tableComments.AddCell(cComments);


                //paragraphGeneralFieldComment.Alignment = 10;
                doc.Add(tableComments);
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawCommentsPDF() - " + ex.Message.ToString());
            }

        }


        //COMMENTS CLOSURE RS37
        //--------------------------------------------------------------------------------------------
        public static void DrawCommentsClosurePDF(string WFID, msTextSharp.Font calibriNormal, msTextSharp.Font calibriBold, msTextSharp.Document doc, msTextSharp.Rectangle page, List<string> commentToPaint)
        {
            try
            {
                PdfPTable tableComments = new PdfPTable(1);
                tableComments.TotalWidth = page.Width;
                msTextSharp.Paragraph paragraphGeneralFieldComment = new msTextSharp.Paragraph();

                msTextSharp.Phrase phraseGFComment = new msTextSharp.Phrase();
                msTextSharp.Chunk columnDateHour = new msTextSharp.Chunk(commentToPaint[0] + " - ", calibriNormal);
                msTextSharp.Chunk columnAuthor = new msTextSharp.Chunk(commentToPaint[1] + " - ", calibriBold);
                msTextSharp.Chunk columnCo = new msTextSharp.Chunk(commentToPaint[2] , calibriNormal);
                phraseGFComment.Add(columnDateHour);
                phraseGFComment.Add(columnAuthor);
                phraseGFComment.Add(columnCo);

                paragraphGeneralFieldComment.Add(phraseGFComment);

                PdfPCell cComments = new PdfPCell(paragraphGeneralFieldComment);
                cComments.Border = msTextSharp.Rectangle.NO_BORDER;
                cComments.VerticalAlignment = msTextSharp.Element.ALIGN_TOP;
                cComments.PaddingBottom = 5f;
                cComments.PaddingTop = 4f;
                tableComments.AddCell(cComments);


                //paragraphGeneralFieldComment.Alignment = 10;
                doc.Add(tableComments);
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawCommentsClosurePDF() - " + ex.Message.ToString());
            }

        }

        #endregion

        #region <INDEX>

        public static void DrawHeaderIndexPDF(string WFID, msTextSharp.Document doc, PdfWriter wri, msTextSharp.Font calibriBold, string headerIndexTitle, msTextSharp.Rectangle page)
        {
            try
            {
                PdfPTable tableDocumentType = new PdfPTable(1);
                tableDocumentType.TotalWidth = page.Width;

                msTextSharp.Paragraph paragraphDocumentType = new msTextSharp.Paragraph();

                //--------------------------------------------------------
                //Document Type
                //--------------------------------------------------------
                msTextSharp.Phrase phraseDT = new msTextSharp.Phrase();
                msTextSharp.Chunk columnName = new msTextSharp.Chunk(headerIndexTitle + ":", calibriBold);
                phraseDT.Add(columnName);
                paragraphDocumentType.Add(phraseDT);
                paragraphDocumentType.Alignment = msTextSharp.Element.ALIGN_LEFT;
                paragraphDocumentType.SpacingBefore = 20f;

                PdfPCell cDocumentType = new PdfPCell(paragraphDocumentType);
                cDocumentType.Border = msTextSharp.Rectangle.NO_BORDER;
                cDocumentType.VerticalAlignment = msTextSharp.Element.ALIGN_TOP;
                cDocumentType.HorizontalAlignment = msTextSharp.Element.ALIGN_LEFT;
                cDocumentType.PaddingBottom = 5f;
                cDocumentType.PaddingTop = 4f;
                tableDocumentType.AddCell(cDocumentType);
                doc.Add(tableDocumentType);
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawHeaderIndexPDF() - " + ex.Message.ToString());
            }

        }

        public static void DrawDocumentTitlesPDF(string WFID, msTextSharp.Document doc, msTextSharp.Font calibriNormal, string documentTitle, msTextSharp.Rectangle page, string contDocuments)
        {
            try
            {
                PdfPTable tableDocuments = new PdfPTable(1);
                tableDocuments.TotalWidth = page.Width;

                msTextSharp.Paragraph paragraphDocuments = new msTextSharp.Paragraph();


                msTextSharp.Phrase phraseDocuments = new msTextSharp.Phrase();
                msTextSharp.Chunk columnNameDocuments = new msTextSharp.Chunk("          " + contDocuments + ".   " + documentTitle, calibriNormal);
                phraseDocuments.Add(columnNameDocuments);
                paragraphDocuments.Add(phraseDocuments);
                paragraphDocuments.Alignment = msTextSharp.Element.ALIGN_LEFT;


                PdfPCell cDocuments = new PdfPCell(paragraphDocuments);
                cDocuments.Border = msTextSharp.Rectangle.NO_BORDER;
                cDocuments.VerticalAlignment = msTextSharp.Element.ALIGN_TOP;
                cDocuments.HorizontalAlignment = msTextSharp.Element.ALIGN_LEFT;
                cDocuments.PaddingBottom = 5f;
                cDocuments.PaddingTop = 4f;
                tableDocuments.AddCell(cDocuments);

                doc.Add(tableDocuments);
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawDocumentTitlesPDF() - " + ex.Message.ToString());
            }

        }

        #endregion

        #region <PAGE>

        public static void DrawPagePDF(string WFID, msTextSharp.Document doc, msTextSharp.Font calibriBoldTitle, string typeDocument)
        {
            try
            {
                //--------------------------------------------------------------------------------
                //Blank Space
                //--------------------------------------------------------------------------------
                //Main documents + Supporting documents (3 blanks)
                //To be signed in ABAC + To be signed on paper (2 blanks)
                //--------------------------------------------------------------------------------
                msTextSharp.Paragraph paragraphTitleWF = new msTextSharp.Paragraph(" ", calibriBoldTitle);
                doc.Add(paragraphTitleWF);
                paragraphTitleWF = new msTextSharp.Paragraph(" ", calibriBoldTitle);
                doc.Add(paragraphTitleWF);

                if (!typeDocument.ToLower().Contains("to"))
                {
                    paragraphTitleWF = new msTextSharp.Paragraph(" ", calibriBoldTitle);
                    doc.Add(paragraphTitleWF);
                }


                msTextSharp.Rectangle page = doc.PageSize;
                PdfPTable headPage = new PdfPTable(1);
                headPage.TotalWidth = page.Width;

                paragraphTitleWF = new msTextSharp.Paragraph(typeDocument, calibriBoldTitle);


                //--------------------------------------------------------
                //Title
                //--------------------------------------------------------
                PdfPCell cTitle = new PdfPCell(paragraphTitleWF);
                cTitle.Border = msTextSharp.Rectangle.NO_BORDER;
                cTitle.VerticalAlignment = msTextSharp.Rectangle.ALIGN_MIDDLE;
                cTitle.HorizontalAlignment = msTextSharp.Rectangle.ALIGN_CENTER;
                cTitle.VerticalAlignment = msTextSharp.Element.ALIGN_MIDDLE;
                cTitle.HorizontalAlignment = msTextSharp.Element.ALIGN_CENTER;
                cTitle.PaddingBottom = 5f;
                cTitle.PaddingTop = 4f;

                headPage.AddCell(cTitle);


                doc.Add(headPage);
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawPagePDF() - " + ex.Message.ToString());
            }

        }


        #endregion

        #region <LOGS>

        public static void DrawLOGSHeaderPDF(string WFID, msTextSharp.Font calibriBold, string nameColumn, PdfPTable PdfTable, string internalNameColumn)
        {
            try
            {

                PdfPCell PdfPCell = new PdfPCell(new msTextSharp.Phrase(new msTextSharp.Chunk(nameColumn, calibriBold)));
                PdfPCell.HorizontalAlignment = msTextSharp.Element.ALIGN_CENTER;
                PdfTable.AddCell(PdfPCell);
               
            }
            catch (Exception ex)
            {
                General.SaveErrorsLog(WFID, "DrawLOGSHeaderPDF() - " + ex.Message.ToString());
            }

        }

        #endregion

   

    }
}
