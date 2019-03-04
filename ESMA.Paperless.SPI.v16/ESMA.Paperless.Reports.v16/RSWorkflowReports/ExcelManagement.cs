using System;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using System.Data;
using System.Web.UI.WebControls;
using System.Collections.Generic;

namespace ESMA.Paperless.Reports.v16.RSWorkflowReports
{
    class ExcelManagement
    {
        public static void GenerateExcel(DataTable dt, GridView gvResults, string path, string filename)
        {
            // Open the document for editing.
            using (SpreadsheetDocument spreadSheet =
               SpreadsheetDocument.Open(path + filename, true))
            {

                BoundField columnResults;


                Workbook workBook = spreadSheet.WorkbookPart.Workbook;

                WorksheetPart worksheetPart = workBook.WorkbookPart.WorksheetParts.First();

                SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                // If the worksheet does not contain a row with the specified
                // row index, insert one.

                Row row1 = new Row();
                Cell cell = null;

                //UInt32Value headerFillIndex = createFill(styleSheet, System.Drawing.Color.SlateGray);


                for (int i = 0; i < gvResults.Columns.Count; i++)
                {

                    cell = new Cell();
                    //cell.StyleIndex = headerFillIndex;

                    columnResults = (BoundField)gvResults.Columns[i];

                    cell.InlineString = new InlineString { Text = new Text(columnResults.HeaderText.Replace("<br/>", " ")) };

                    //  excelSheet.Cells[1, contColumn] = columnResults.HeaderText;
                    row1.AppendChild(cell);
                }
                sheetData.Append(row1);


                foreach (DataRow dr in dt.Rows)
                {
                    row1 = new Row();

                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        cell = new Cell();

                        cell.InlineString = new InlineString { Text = new Text(dr[i].ToString()) };
                        // excelSheet.Cells[contRow, i + 1] = dr[i].ToString();
                        row1.AppendChild(cell);
                    }

                    sheetData.Append(row1);
                }

                //worksheet.Append(sheetData);

                //worksheetPart.Worksheet = worksheet;

            }
        }

        public static void CreatePackage_(DataTable dt, GridView gvResults, string path, string fileName)
        {
            try
            {
                using (SpreadsheetDocument package = SpreadsheetDocument.Create(path + fileName, SpreadsheetDocumentType.Workbook))
                {
                    CreateParts(package, dt, gvResults);
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreatePackage() - " + ex.Source, ex.Message);
            }
        }

        public static void CreatePackage(DataTable dt, GridView gvResults, string filePath)
        {
            try
            {
                Dictionary<string, string> gvResultsColumns = new Dictionary<string, string>();
                for (int i = 0; i < gvResults.Columns.Count; i++)
                {
                    BoundField columnResults = (BoundField)gvResults.Columns[i];
                    gvResultsColumns.Add(columnResults.DataField, columnResults.HeaderText);
                }
                CreatePackage(dt, gvResultsColumns, filePath);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreatePackage() - " + ex.Source, ex.Message);
            }
        }

        public static void CreatePackage(DataTable dt, Dictionary<string, string> columnsNameDictionary, string filePath)
        {
            try
            {
                using (SpreadsheetDocument package = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
                {
                    CreateParts(package, dt, columnsNameDictionary);
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreatePackage() - " + ex.Source, ex.Message);
            }
        }


        // Adds child parts and generates content of the specified part
        private static void CreateParts(SpreadsheetDocument document, DataTable dt, GridView gvResults)
        {
            try
            {
                WorkbookPart workbookPart1 = document.AddWorkbookPart();
                GenerateWorkbookPart1Content(workbookPart1);

                WorkbookStylesPart stylesPart = workbookPart1.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = GenerateStyleSheet();
                stylesPart.Stylesheet.Save();

                WorksheetPart worksheetPart1 = workbookPart1.AddNewPart<WorksheetPart>("rId1");
                GenerateWorksheetPart1Content(worksheetPart1, dt, gvResults);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateParts() - " + ex.Source, ex.Message);
            }
        }

        // Adds child parts and generates content of the specified part
        private static void CreateParts(SpreadsheetDocument document, DataTable dt, Dictionary<string, string> columnsNameDictionary)
        {
            try
            {
                WorkbookPart workbookPart1 = document.AddWorkbookPart();
                GenerateWorkbookPart1Content(workbookPart1);

                WorkbookStylesPart stylesPart = workbookPart1.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = GenerateStyleSheet();
                stylesPart.Stylesheet.Save();

                WorksheetPart worksheetPart1 = workbookPart1.AddNewPart<WorksheetPart>("rId1");
                GenerateWorksheetPart1Content(worksheetPart1, dt, columnsNameDictionary);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateParts() - " + ex.Source, ex.Message);
            }
        }


        // Generates content of workbookPart1. 
        private static void GenerateWorkbookPart1Content(WorkbookPart workbookPart1)
        {
            try
            {
                Workbook workbook1 = new Workbook();
                workbook1.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");

                Sheets sheets1 = new Sheets();
                Sheet sheet1 = new Sheet() { Name = "Report", SheetId = (UInt32Value)1U, Id = "rId1" };
                sheets1.Append(sheet1);

                workbook1.Append(sheets1);
                workbookPart1.Workbook = workbook1;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GenerateWorkbookPart1Content() - " + ex.Source, ex.Message);
            }
        }

        // Generates content of worksheetPart1. 
        private static void GenerateWorksheetPart1Content_(WorksheetPart worksheetPart1, DataTable dt, GridView gvResults)
        {
            try
            {
                Worksheet worksheet1 = new Worksheet();
                SheetData sheetData1 = new SheetData();
                BoundField columnResults;

                Row row1 = new Row();
                Cell cell = null;
                string text = "";

                CellValue cellvalue = null;

                for (int i = 0; i < gvResults.Columns.Count; i++)
                {
                    cell = new Cell();

                    columnResults = (BoundField)gvResults.Columns[i];

                    cellvalue = new CellValue();
                    text = columnResults.HeaderText.Replace("<br/>", Environment.NewLine);

                    cell.DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String;
                    cellvalue.Text = text;
                    cell.Append(cellvalue);

                    cell.StyleIndex = 1;
                    row1.AppendChild(cell);
                }
                sheetData1.Append(row1);

                foreach (DataRow dr in dt.Rows)
                {
                    row1 = new Row();

                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        cell = new Cell();

                        text = dr[i].ToString().Replace("<br/>", Environment.NewLine);

                        cellvalue = new CellValue();
                        cell.DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String;

                        cellvalue.Text = text;
                        cell.Append(cellvalue);
                        cell.StyleIndex = 5;

                        row1.AppendChild(cell);
                    }

                    sheetData1.Append(row1);
                }

                worksheet1.Append(sheetData1);
                worksheetPart1.Worksheet = worksheet1;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GenerateWorksheetPart1Content() - " + ex.Source, ex.Message);
            }
        }

        private static void GenerateWorksheetPart1Content(WorksheetPart worksheetPart1, DataTable dt, GridView gvResults)
        {
            Dictionary<string, string> gvResultsColumns = new Dictionary<string, string>();
            for (int i = 0; i < gvResults.Columns.Count; i++)
            {
                BoundField columnResults = (BoundField)gvResults.Columns[i];
                gvResultsColumns.Add(columnResults.DataField, columnResults.HeaderText);
            }
            GenerateWorksheetPart1Content(worksheetPart1, dt, gvResultsColumns);
        }

        // Generates content of worksheetPart1. 
        private static void GenerateWorksheetPart1Content(WorksheetPart worksheetPart1, DataTable dt, Dictionary<string, string> columnsNameDictionary)
        {
            try
            {
                Worksheet worksheet1 = new Worksheet();
                SheetData sheetData1 = new SheetData();
                string text;

                Row row1 = new Row();
                Cell cell = null;
                CellValue cellvalue;

                foreach (KeyValuePair<String, String> kvp in columnsNameDictionary)
                {
                    cell = new Cell();

                    cellvalue = new CellValue();
                    text = kvp.Value.Replace("<br/>", Environment.NewLine);

                    cell.DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String;
                    cellvalue.Text = text;
                    cell.Append(cellvalue);

                    cell.StyleIndex = 1;
                    row1.AppendChild(cell);
                }
                sheetData1.Append(row1);

                foreach (DataRow dr in dt.Rows)
                {
                    row1 = new Row();

                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        cell = new Cell();

                        text = dr[i].ToString().Replace("<br/>", Environment.NewLine);

                        cellvalue = new CellValue();
                        cell.DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String;

                        cellvalue.Text = DataManagement.DateFormat(text);
                        cell.Append(cellvalue);
                        cell.StyleIndex = 5;

                        row1.AppendChild(cell);
                    }

                    sheetData1.Append(row1);
                }

                worksheet1.Append(sheetData1);
                worksheetPart1.Worksheet = worksheet1;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GenerateWorksheetPart1Content() - " + ex.Source, ex.Message);
            }
        }

        private static Stylesheet GenerateStyleSheet()
        {
            return new Stylesheet(
                new Fonts(
                    new Font(                                                               // Index 0 - The default font.
                        new DocumentFormat.OpenXml.Spreadsheet.FontSize() { Val = 11 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Calibri" }),
                    new Font(                                                               // Index 1 - The bold font.
                        new Bold(),
                        new DocumentFormat.OpenXml.Spreadsheet.FontSize() { Val = 11 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Calibri" }),
                    new Font(                                                               // Index 2 - The Italic font.
                        new Italic(),
                        new DocumentFormat.OpenXml.Spreadsheet.FontSize() { Val = 11 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Calibri" }),
                    new Font(                                                               // Index 2 - The Times Roman font. with 16 size
                        new DocumentFormat.OpenXml.Spreadsheet.FontSize() { Val = 16 },
                        new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                        new FontName() { Val = "Times New Roman" })
                ),
                new Fills(
                    new Fill(                                                           // Index 0 - The default fill.
                        new PatternFill() { PatternType = PatternValues.None }),
                    new Fill(                                                           // Index 1 - The blue fill.
                        new PatternFill(
                            new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "9BC2E600" } }
                        ) { PatternType = PatternValues.Solid }),

                    new Fill(                                                           // Index 2 - The yellow fill.
                        new PatternFill(
                            new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "FFFFFF00" } }
                        ) { PatternType = PatternValues.Solid })
                ),
                new Borders(
                    new Border(                                                         // Index 0 - The default border.
                        new LeftBorder(),
                        new RightBorder(),
                        new TopBorder(),
                        new BottomBorder(),
                        new DiagonalBorder()),
                    new Border(                                                         // Index 1 - Applies a Left, Right, Top, Bottom border to a cell
                        new LeftBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new RightBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new TopBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new BottomBorder(
                            new Color() { Auto = true }
                        ) { Style = BorderStyleValues.Thin },
                        new DiagonalBorder())
                ),
                new CellFormats(
                    new CellFormat() { FontId = 0, FillId = 0, BorderId = 0 },                          // Index 0 - The default cell style.  If a cell does not have a style index applied it will use this style combination instead
                    new CellFormat(new Alignment() { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center, WrapText = true }) { FontId = 1, FillId = 0, BorderId = 0, ApplyFont = true },       // Index 1 - Bold 
                    new CellFormat() { FontId = 2, FillId = 0, BorderId = 0, ApplyFont = true },       // Index 2 - Italic
                    new CellFormat() { FontId = 3, FillId = 0, BorderId = 0, ApplyFont = true },       // Index 3 - Times Roman
                    new CellFormat() { FontId = 0, FillId = 2, BorderId = 0, ApplyFill = true },       // Index 4 - Yellow Fill
                    new CellFormat(                                                                   // Index 5 - Alignment
                        new Alignment() { Horizontal = HorizontalAlignmentValues.Left, Vertical = VerticalAlignmentValues.Top, WrapText = true }
                    ) { FontId = 0, FillId = 0, BorderId = 0, ApplyAlignment = true },
                    new CellFormat() { FontId = 0, FillId = 0, BorderId = 1, ApplyBorder = true }      // Index 6 - Border
                )
            ); // return
        }

        private static UInt32Value createFill(Stylesheet styleSheet, System.Drawing.Color fillColor)
        {
            Fill fill = new Fill(
                new PatternFill(
                     new ForegroundColor()
                     {
                         Rgb = new HexBinaryValue()
                         {
                             Value =
                             System.Drawing.ColorTranslator.ToHtml(
                                 System.Drawing.Color.FromArgb(
                                     fillColor.A,
                                     fillColor.R,
                                     fillColor.G,
                                     fillColor.B)).Replace("#", "")
                         }
                     })
                {
                    PatternType = PatternValues.Solid
                }
            );
            styleSheet.Fills.Append(fill);

            UInt32Value result = styleSheet.Fills.Count;
            styleSheet.Fills.Count++;
            return result;
        }

    }
}
