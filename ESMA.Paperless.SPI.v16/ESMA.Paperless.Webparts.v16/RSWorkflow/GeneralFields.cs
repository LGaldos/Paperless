using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using System.ComponentModel;

using System.Configuration;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using System.Drawing;
using Microsoft.SharePoint.Utilities;




namespace ESMA.Paperless.Webparts.v16.RSWorkflow
{
    static class GeneralFields
    {

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wfid"></param>
        /// <param name="Web"></param>
        /// <param name="item"></param>
        /// <param name="wftypeName"></param>
        /// <param name="itemExists"></param>
        /// <param name="PlaceHolder_GFTable"></param>
        public static void LoadGeneralFields(string wfid, SPWeb Web, SPListItem item, string wftypeName, string wftypeOrder, Dictionary<string, string> generalFieldsSessionDictionary, object refreshing, bool itemExists, PlaceHolder PlaceHolder_GFTable, Dictionary<string, string> parameters)
        {
            try
            {
                if (((generalFieldsSessionDictionary != null) && (refreshing == null))  || (HttpContext.Current.Session["ShowCloseWarningPopUp" + wfid] != null))
                    LoadGeneralFieldsValues(wfid, wftypeName, wftypeOrder, Web, itemExists, item.ParentList, true, PlaceHolder_GFTable, item, generalFieldsSessionDictionary, parameters);
                else if ((refreshing != null) || generalFieldsSessionDictionary == null)
                    LoadGeneralFieldsValues(wfid, wftypeName, wftypeOrder, Web, itemExists, item.ParentList, false, PlaceHolder_GFTable, item, generalFieldsSessionDictionary, parameters);

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "LoadGeneralFields() - " + ex.Message);
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wfid"></param>
        /// <param name="wftypeName"></param>
        /// <param name="wforder"></param>
        /// <param name="Web"></param>
        /// <param name="exists"></param>
        /// <param name="wfLibrary"></param>
        /// <param name="fieldsModified"></param>
        /// <param name="PlaceHolder_GFTable"></param>
        /// <param name="item"></param>
        private static void LoadGeneralFieldsValues(string wfid, string wftypeName, string wforder, SPWeb Web, bool exists, SPList wfLibrary, bool fieldsModified, PlaceHolder PlaceHolder_GFTable, SPListItem item, Dictionary<string, string> generalFieldsSessionDictionary, Dictionary<string, string> parameters)
        {
            try
            {
                Dictionary<string, string> generalFieldsDictionary = new Dictionary<string, string>();
                Dictionary<string, string> generalFieldsValuesDictionary = new Dictionary<string, string>();
                SearchInitialGeneralFields(wfid, ref generalFieldsDictionary, item, wfid);
                generalFieldsValuesDictionary = SearchGeneralFieldsValues(item, generalFieldsDictionary, wfid, fieldsModified, Web, generalFieldsSessionDictionary);

                DrawGeneralFields(generalFieldsValuesDictionary, parameters, PlaceHolder_GFTable, Web, wftypeName, wfid);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "LoadGeneralFields() - " + ex.Message);
            }
        }


        #region <GENERAL FIELDS>

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="WFID"></param>
        /// <param name="generalFieldsDictionary"></param>
        /// <param name="item"></param>
        public static void SearchInitialGeneralFields(string WFID, ref Dictionary<string, string> generalFieldsDictionary, SPListItem item, string wfid)
        {
            try
            {
                string[] generalFieldsColumnName = Regex.Split(item["InitialGeneralFields"].ToString(), ";#");

                foreach (string columnName in generalFieldsColumnName)
                {
                    if (item.Fields.ContainsFieldWithStaticName(columnName))
                    {
                        string displayName = item.Fields.GetFieldByInternalName(columnName).Title.ToString();

                        if (!generalFieldsDictionary.ContainsKey(displayName))
                        {
                            if (item[columnName] != null)
                                generalFieldsDictionary.Add(displayName, item[columnName].ToString());
                            else
                                generalFieldsDictionary.Add(displayName, string.Empty);
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SearchInitialGeneralFields() - " + ex.Message);
            }

        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="MyWeb"></param>
        /// <param name="order"></param>
        /// <param name="generalFieldsDictionary"></param>
        public static void SearchEspecificFieldsColumnNames(SPWeb MyWeb, string order, ref Dictionary<string, string> generalFieldsDictionary, string wfid)
        {
            try
            {
                List<string> _listFields = new List<string>();
                string listName = "RS Workflow Configuration";

                SPList myList = MyWeb.Lists[listName];
                SPListItemCollection myListItems;
                SPQuery myQuery = new SPQuery();
                myQuery.Query = "<Where><Eq><FieldRef Name='WFOrder'/><Value Type='Text'>" + order.Trim() + "</Value></Eq></Where>";
                myQuery.ViewFields = string.Concat(
                                  "<FieldRef Name='WFOrder' />",
                                  "<FieldRef Name='WFFieldsToAdd' />");
                myQuery.ViewFieldsOnly = true; // Fetch only the data that we need

                    myListItems = myList.GetItems(myQuery);

                    if (myListItems.Count > 0)
                    {
                        if (myListItems[0]["WFFieldsToAdd"] != null)
                        {
                            SPFieldLookupValueCollection values = myListItems[0]["WFFieldsToAdd"] as SPFieldLookupValueCollection;

                            foreach (SPFieldLookupValue value in values)
                                generalFieldsDictionary.Add(value.LookupValue, string.Empty);
                        }
                    }
                    
                
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SearchEspecificFieldsColumnNames() - " + ex.Message);
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="generalFieldsDictionary"></param>
        public static void SearchComunFieldsColumnNames(SPWeb Web, ref Dictionary<string, string> generalFieldsDictionary, string wfid)
        {
            try
            {
                string listName = "RS Configuration Parameters";
                string searchParameter = "GeneralColumn_";

                SPList myList = Web.Lists[listName];
                SPListItemCollection itmCol;
             
                    SPQuery myQuery = new SPQuery();
                    myQuery.Query = "<Where><Contains><FieldRef Name='Title'/><Value Type='Text'>" + searchParameter + "</Value></Contains></Where>";
                    myQuery.ViewFields = string.Concat(
                                  "<FieldRef Name='Title' />",
                                  "<FieldRef Name='Value1' />");
                    myQuery.ViewFieldsOnly = true; // Fetch only the data that we need
                    
                    itmCol = myList.GetItems(myQuery);

                    foreach (SPListItem itm in itmCol)
                    {
                        if (itm["Value1"] != null)
                            generalFieldsDictionary.Add(itm["Value1"].ToString(), string.Empty);
                    }
              
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SearchComunFieldsColumnNames() - " + ex.Message);
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="WFID"></param>
        /// <param name="generalFieldsDictionary"></param>
        /// <param name="wfLibrary"></param>
        /// <param name="fieldsModified"></param>
        /// <returns></returns>
        public static Dictionary<string, string> SearchGeneralFieldsValues(SPListItem item, Dictionary<string, string> generalFieldsDictionary, string wfid, bool fieldsModified, SPWeb Web, Dictionary<string, string> generalFieldsSessionDictionary)
        {
            Dictionary<string, string> generalFieldsValuesDictionary = new Dictionary<string, string>();

            try
            {
                foreach (KeyValuePair<String, String> kvp in generalFieldsDictionary)
                {
                    string columnName = kvp.Key;

                    if (!string.IsNullOrEmpty(columnName))
                    {
                        if (!fieldsModified)
                        {
                            if (item[columnName] != null)
                            {
                                SPFieldType fieldType = item.Fields[columnName].Type;
                                string value  = item[columnName].ToString();

                                generalFieldsValuesDictionary.Add(columnName, value);
                            }
                            else
                                generalFieldsValuesDictionary.Add(columnName, string.Empty);
                        }
                        else
                        {

                            string modifiedValue = GetValueGFModified(columnName, wfid, generalFieldsSessionDictionary);
                            string value = string.Empty;

                            if (item[columnName] != null)
                                value = item[columnName].ToString();
                            else
                                value = string.Empty;

                            if (string.IsNullOrEmpty(modifiedValue))
                                generalFieldsValuesDictionary.Add(columnName, value);
                            else if (modifiedValue.Equals("[fieldDelete]"))
                                generalFieldsValuesDictionary.Add(columnName, string.Empty);
                            else
                                generalFieldsValuesDictionary.Add(columnName, modifiedValue);
                        }
                    }
                }

                //HttpContext.Current.Session["FormGeneralFieldsDictionary" + wfid] = generalFieldsValuesDictionary;
                //generalFieldsSessionDictionary = generalFieldsValuesDictionary;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SearchGeneralFieldsValues() - " + ex.Message);
            }

            return generalFieldsValuesDictionary;
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="MyWeb"></param>
        /// <param name="columnName"></param>
        /// <param name="wfName"></param>
        /// <param name="wfid"></param>
        /// <returns></returns>
        private static SPFieldType GetColumnType(SPWeb MyWeb, string columnName, string wfName, string wfid)
        {
            try
            {
                SPField field = null;
                SPFieldType fieldType = 0;

                //Fields -> Column [Paperless]
                try
                {
                    field = WorkflowDataManagement.GetFieldInRSGroup(MyWeb, columnName); // MyWeb.Fields[columnName];
                }
                //Fields -> Column [GKMF]
                catch
                {
                    field = WorkflowDataManagement.GetFieldInRSGroup(MyWeb.Site.RootWeb, columnName); // MyWeb.Site.RootWeb.Fields[columnName];
                }

                if (field != null)
                    fieldType = field.Type;
                else
                {
                    string message = "The Column '" + columnName + "' does not exist in '" + wfName + "'.";
                    General.saveErrorsLog(wfid, "GetColumnType() - " + message);
                }

                return fieldType;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetColumnType() - " + ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Get Site Column for a General Field
        /// </summary>
        private static SPField GetGFSiteColumn(SPWeb web, string columnName, string wfid)
        {
            SPField field = null;

            try
            {
                field = WorkflowDataManagement.GetFieldInRSGroup(web, columnName);
                if (field == null)
                {
                    field = WorkflowDataManagement.GetFieldInRSGroup(web.Site.RootWeb, columnName);
                    if (field == null)
                    {
                        string message = "The Site Column '" + columnName + "' does not exist.";
                        General.saveErrorsLog(wfid, "GetGFSiteColumn() - " + message);
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetGFSiteColumn() - " + ex.Message);
            }

            return field;
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="MyWeb"></param>
        /// <param name="columnName"></param>
        /// <param name="wfid"></param>
        /// <returns></returns>
        private static List<string> GetSPFieldChoiceValues(SPWeb MyWeb, string columnName, string wfid)
        {

            try
            {
                List<string> fieldList = new List<string>();
                SPFieldChoice field = (SPFieldChoice)WorkflowDataManagement.GetFieldInRSGroup(MyWeb, columnName); // (SPFieldChoice)MyWeb.Fields[columnName];

                foreach (string value in field.Choices)
                    fieldList.Add(value);

                return fieldList;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetSPFieldChoiceValues() - " + ex.Message);
                return null;
            }

        }

        #endregion


        #region <DRAW CONTROLS - GF>

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="generalFieldsDictionary"></param>
        /// <param name="PlaceHolder_GFTable"></param>
        /// <param name="Web"></param>
        /// <param name="wfName"></param>
        /// <param name="wfid"></param>
        public static void DrawGeneralFields(Dictionary<string, string> generalFieldsDictionary, Dictionary<string, string> parameters, PlaceHolder PlaceHolder_GFTable, SPWeb Web, string wfName, string wfid)
        {
            try
            {
                Label lblTitle = null;
                UpdatePanel updPanel = null;

                foreach (KeyValuePair<String, String> kvp in generalFieldsDictionary)
                {
                    string columnName = kvp.Key;
                    string value = kvp.Value;


                    if (PlaceHolder_GFTable.FindControl(FormatColumnName(columnName)) == null)
                    {
                        //Control type
                        SPField gfSiteColumn = GetGFSiteColumn(Web, columnName, wfid);

                        if (gfSiteColumn != null)
                        {
                            SPFieldType fieldType = gfSiteColumn.Type;

                            //Column Name
                            lblTitle = DrawControlType_Label_ColumnName(columnName);

                            if (fieldType.ToString().ToUpper().Equals("USER"))
                                PlaceHolder_GFTable.Controls.Add(new LiteralControl("<div class=\"general_field_row general_field_user\" style=\"margin-top:1px; margin-bottom:3px\">"));
                            else
                                PlaceHolder_GFTable.Controls.Add(new LiteralControl("<div class=\"general_field_row\">"));

                            updPanel = DrawUpdatePanel(columnName);
                            Control controlGF = DrawGFControlsByType(columnName, wfid, gfSiteColumn, value, parameters, Web);

                            if (controlGF != null)
                            {
                                updPanel.ContentTemplateContainer.Controls.Add(lblTitle);
                                updPanel.ContentTemplateContainer.Controls.Add(controlGF);
                                updPanel.EnableViewState = true;
                                PlaceHolder_GFTable.Controls.Add(updPanel);
                            }

                            PlaceHolder_GFTable.Controls.Add(new LiteralControl("</div>"));
                        }

                    }
                }

                HttpContext.Current.Session["FormGeneralFieldsDictionary" + wfid] = generalFieldsDictionary;

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "DrawGeneralFields() - " + ex.Message);
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="fieldType"></param>
        /// <param name="value"></param>
        /// <param name="Web"></param>
        /// <returns></returns>
        private static Control DrawGFControlsByType(string columnName, string wfid, SPField gfColumn, string value, Dictionary<string, string> parameters, SPWeb Web)
        {
            Control control = null;

            try
            {
                SPFieldType fieldType = gfColumn.Type;
                switch (fieldType)
                {
                    case SPFieldType.Text:
                        control = DrawControlType_TextBox(columnName, value);
                        break;

                    case SPFieldType.DateTime:
                             control = DrawControlType_DateTime(columnName, value, wfid);
                        break;

                    case SPFieldType.Boolean:
                        control = DrawControlType_CheckBox(columnName, value);
                        break;

                    case SPFieldType.User:
                        control = DrawControlType_User(columnName, value, Web, parameters, wfid);
                        break;

                    case SPFieldType.Choice:
                        SPFieldChoice choiceColumn = (SPFieldChoice)gfColumn;
                        if (choiceColumn.EditFormat == SPChoiceFormatType.RadioButtons)
                            control = DrawControlType_RadioButtons(columnName, value, choiceColumn);
                        else if (choiceColumn.EditFormat == SPChoiceFormatType.Dropdown)
                            control = DrawControlType_Dropdown(columnName, value, choiceColumn);
                        break;

                    case SPFieldType.Note:
                        control = DrawControlType_Note(columnName, value);
                        break;

                    default:
                        string message = "This type of field is not implemented -> " + fieldType.ToString();
                        General.saveErrorsLog(string.Empty, "DrawControlsType() - " + message);
                        break;
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "DrawControlsType() - " + ex.Message);
            }

            return control;
        }


        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nameColumn"></param>
        /// <returns></returns>
        public static Panel DrawPanel(string nameColumn)
        {
            try
            {
                Panel up = new Panel();
                up.ID = "UpdatePanel_" + nameColumn;
                //up.ChildrenAsTriggers = true;
                //up.UpdateMode = UpdatePanelUpdateMode.Conditional;

                return up;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "DrawUpdatePanel() - " + ex.Message);
                return null;
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nameColumn"></param>
        /// <returns></returns>
        public static UpdatePanel DrawUpdatePanel(string nameColumn)
        {
            try
            {
                UpdatePanel up = new UpdatePanel();
                up.ID = "UpdatePanel_" + nameColumn;
                up.ChildrenAsTriggers = true;
                up.UpdateMode = UpdatePanelUpdateMode.Conditional;

                return up;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "DrawUpdatePanel() - " + ex.Message);
                return null;
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="isTextBoxOrDate"></param>
        /// <returns></returns>
        private static Label DrawControlType_Label_ColumnName(string columnName)
        {
            Label lbl = null;

            try
            {
                lbl = new Label();
                lbl.ID = "FieldLabelID_" + columnName;
                lbl.Text = columnName + ":  ";
                
                if (columnName.IndexOf("Staff") != -1)
                    lbl.CssClass = "label_span_general_field_staff label-workflow";
                else
                    lbl.CssClass = "label-workflow";
                
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "DrawControlType_Label_ColumnName() - " + ex.Message);
            }

            return lbl;
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static TextBox DrawControlType_TextBox(string columnName, string value)
        {
            TextBox txt = null;

            try
            {
                txt = new TextBox();
                txt.ID = columnName;
                txt.CssClass = "input_text_general_field";
                txt.Attributes.Add("onkeydown", "return (event.keyCode!=13);");
                txt.AutoPostBack = true;
                txt.TextChanged += new EventHandler(txt_TextChanged);
                txt.MaxLength = 250;

                if (!string.IsNullOrEmpty(value))
                    txt.Text = value;

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "DrawControlType_TextBox() - " + ex.Message);
            }

            return txt;
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void txt_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string wfid = HttpContext.Current.Session["FormWFID"].ToString();
                TextBox txt = (TextBox)sender;
                RetainControlValueGeneralFields(txt.ID, txt.Text, wfid);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "txt_TextChanged() - " + ex.Message);

            }
        }



        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static DateTimeControl DrawControlType_DateTime(string columnName, string value, string wfid)
        {
            DateTimeControl dt = null;

            try
            {
                dt = new DateTimeControl();
                dt.DateOnly = true;
                dt.Calendar = SPCalendarType.Gregorian;
                dt.UseTimeZoneAdjustment = false;
                dt.LocaleId = 2057;
                dt.EnableViewState = true;
                dt.ID = columnName;
                dt.AutoPostBack = true;
                //dt.DateChanged += new EventHandler(dt_DateChanged);
                dt.Visible = true;

                foreach (Control ctrl in dt.Controls)
                {
                    if (ctrl is TextBox)
                    {
                        TextBox txtBox = (TextBox)ctrl;
                        txtBox.CssClass = "input_text_general_field minor_width";
                        txtBox.Attributes.Add("onkeydown", "return (event.keyCode!=13);");
                        txtBox.ID = "txt_" + columnName;
                        txtBox.AutoPostBack = true;
                        txtBox.TextChanged += new EventHandler(dt_DeleteDateChanged);
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(value))
                {
                    DateTime dtv;
                    if (FormatDate(value, out dtv))
                    {
                        dt.SelectedDate = dtv;
                        RetainControlValueGeneralFields(dt.ID, value, wfid);
                    }
                    else
                    {
                        dt.ClearSelection();
                        RetainControlValueGeneralFields(dt.ID, string.Empty, wfid);
                    }

                }
                else
                    RetainControlValueGeneralFields(dt.ID, string.Empty, wfid);

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "DrawControlType_DateTime() - " + ex.Message);
            }

            return dt;
        }


        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void dt_DateChanged(object sender, EventArgs e)
        {
            try
            {
                string wfid = HttpContext.Current.Session["FormWFID"].ToString();
                DateTimeControl dt = (DateTimeControl)sender;
                string value = string.Empty;

                foreach (Control ctrl in dt.Controls)
                {
                    if (ctrl is TextBox)
                    {
                        TextBox txtBox = (TextBox)ctrl;
                        value = txtBox.Text;
                        txtBox.ID = "txt_" + dt.ID;
                        txtBox.AutoPostBack = true;
                        txtBox.TextChanged += new EventHandler(dt_DeleteDateChanged);
                        break;
                    }
                }

                DateTime dtv;
                if (!string.IsNullOrEmpty(value) && FormatDate(value, out dtv))
                {
                    dt.SelectedDate = dtv;
                    RetainControlValueGeneralFields(dt.ID, value, wfid);
                }
                else
                {
                    RetainControlValueGeneralFields(dt.ID, string.Empty, wfid);
                    dt.ClearSelection();
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "dt_DateChanged() - " + ex.Message);
            }
        }



        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void dt_DeleteDateChanged(object sender, EventArgs e)
        {
            try
            {
                TextBox txt = (TextBox)sender;
                string nameColumn = txt.ID.Replace("txt_", null);
                string value = txt.Text;
                bool updateDate = false;
                string wfid = HttpContext.Current.Session["FormWFID"].ToString();



                Dictionary<string, string> generalFieldsSessionDictionary = (Dictionary<string, string>)HttpContext.Current.Session["FormGeneralFieldsDictionary" + wfid];

                if (generalFieldsSessionDictionary.ContainsKey(nameColumn) && (!string.IsNullOrEmpty(generalFieldsSessionDictionary[nameColumn])) && (string.IsNullOrEmpty(value)))
                    updateDate = true;

                if (!string.IsNullOrEmpty(value))
                {
                    if (CheckDate(value))
                    {
                        if (updateDate)
                        {
                            DateTime dtv;
                            txt.Text = (FormatDate(value, out dtv)) ? Convert.ToString(dtv) : null;
                        }

                        RetainControlValueGeneralFields(nameColumn, value, wfid);

                    }
                    else
                    {
                        if (updateDate)
                            txt.Text = string.Empty;

                        RetainControlValueGeneralFields(nameColumn, string.Empty, wfid);

                    }
                }
                else
                {
                    if (updateDate)
                        txt.Text = string.Empty;

                    RetainControlValueGeneralFields(nameColumn, string.Empty, wfid);

                }


            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "dt_DeleteDateChanged() - " + ex.Message);
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static bool CheckDate(string date)
        {
            try
            {
                if (date.Contains(" "))
                    date = date.Split(' ')[0];

                DateTime dt;
                return (FormatDate(date, out dt)) ? true : false;    
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "CheckDate() - " + ex.Message);
                return false;
            }
        }

        private static Boolean FormatDate(string date, out DateTime dt)
        {
            try
            {                
                try
                {
                    if (date.Contains(" "))
                        date = date.Split(' ')[0];
                    

                    dt = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
                catch
                {
                    dt = Convert.ToDateTime(date);
                }
            }
            catch (Exception ex)
            {
                string wfid = HttpContext.Current.Session["FormWFID"].ToString();
                General.saveErrorsLog(wfid, "FormatDate() - " + ex.Message);
                dt = DateTime.MinValue;
                return false;
            }

            return true;
        }

        private static string FormatCheckBoxValue(bool checkValue)
        {
            string value = "0";

            try
            {
                if (checkValue.Equals(false))
                    value = "0";
                else
                    value = "1";

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "FormatCheckBoxValue() - " + ex.Message);
            }

            return value;
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static CheckBox DrawControlType_CheckBox(string columnName, string value)
        {
            CheckBox cb = null;
            try
            {
                cb = new CheckBox();
                cb.ID = columnName;
                cb.CssClass = "label_span";
                cb.AutoPostBack = true;
                cb.CheckedChanged += new EventHandler(cb_CheckChanged);

                if (string.IsNullOrEmpty(value) || value.ToLower().Equals("false"))
                    cb.Checked = false;
                else
                    cb.Checked = true;
                cb.Attributes.Add("onkeydown", "return (event.keyCode!=13);");
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "DrawControlType_CheckBox() - " + ex.Message);
            }

            return cb;
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="value"></param>
        /// <returns></returns>        
        private static RadioButtonList DrawControlType_RadioButtons(string columnName, string value, SPFieldChoice choiceField)
        {
            RadioButtonList rbl = null;
            try
            {
                rbl = new RadioButtonList();
                rbl.ID = columnName;
                rbl.AutoPostBack = true;
                rbl.SelectedIndexChanged += new EventHandler(rbl_SelectedIndexChanged);

                foreach (string choice in choiceField.Choices)
                    rbl.Items.Add(choice);

                if (!string.IsNullOrEmpty(value))
                    rbl.SelectedIndex = rbl.Items.IndexOf(new ListItem(value));

                rbl.Attributes.Add("onkeydown", "return (event.keyCode!=13);");
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "DrawControlType_RadioButtons() - " + ex.Message);
            }

            return rbl;
        }

        private static DropDownList DrawControlType_Dropdown(string columnName, string value, SPFieldChoice choiceField)
        {
            DropDownList ddl = null;
            try
            {
                ddl = new DropDownList();
                ddl.ID = columnName;
                ddl.CssClass = "chosen-select";
                ddl.Attributes.Add("style", "display:none");
                ddl.AutoPostBack = true;
                ddl.SelectedIndexChanged += new EventHandler(ddl_SelectedIndexChanged);

                ddl.Items.Insert(0, new ListItem(String.Empty, String.Empty));
                ddl.SelectedIndex = 0;
                foreach (string choice in choiceField.Choices)
                    ddl.Items.Add(choice);

                if (!string.IsNullOrEmpty(value))
                    ddl.SelectedIndex = ddl.Items.IndexOf(new ListItem(value));

                ddl.Attributes.Add("onkeydown", "return (event.keyCode!=13);");
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "DrawControlType_Dropdown() - " + ex.Message);
            }

            return ddl;
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void cb_CheckChanged(object sender, EventArgs e)
        {
            try
            {
                string wfid = HttpContext.Current.Session["FormWFID"].ToString();
                CheckBox cb = (CheckBox)sender;
                RetainControlValueGeneralFields(cb.ID, cb.Checked.ToString(), wfid);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "cb_CheckChanged() - " + ex.Message);
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void rbl_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string wfid = HttpContext.Current.Session["FormWFID"].ToString();
                RadioButtonList rbl = (RadioButtonList)sender;
                RetainControlValueGeneralFields(rbl.ID, rbl.SelectedValue, wfid);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "rbl_SelectedIndexChanged() - " + ex.Message);
            }
        }

        private static void ddl_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string wfid = HttpContext.Current.Session["FormWFID"].ToString();
                DropDownList ddl = (DropDownList)sender;
                RetainControlValueGeneralFields(ddl.ID, ddl.SelectedValue, wfid);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "ddl_SelectedIndexChanged- " + ex.Message);
            }
        }

        //TBC
        /// <summary>
        /// update for 33
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="value"></param>
        /// <param name="Web"></param>
        /// <returns></returns>
        /// 
        private static Control DrawControlType_User(string columnName, string value, SPWeb Web, Dictionary<string, string> parameters, string wfid)
        {
            Control controlUser = new Control();

            try
            {
                    string domain = parameters["Domain"];
  
                    DropDownList ddlGroup = new DropDownList();
                    Dictionary<string, string> groupUsers = new Dictionary<string, string>();
                    groupUsers.Add("", "");
                    
                    foreach (SPUser user in Web.AllUsers)
                    {
                        if (!user.IsDomainGroup)
                        {
                            string userName = user.Name.Replace(domain + "\\", "");
                            string userLogin = user.LoginName;

                            if ((!userName.ToLower().Contains("(deleted)")) && (!userName.ToLower().Equals("system account")) && (!userName.ToLower().StartsWith("nt authority")))
                                groupUsers.Add(user.LoginName, userName);
                        }
                    }

                    ddlGroup.ID = columnName; //FormatColumnName(columnName);
                    ddlGroup.AutoPostBack = true;
                    ddlGroup.SelectedIndexChanged += new EventHandler(user_changed);
                    ddlGroup.CssClass = "ddl_users";
                    ddlGroup.CssClass += " chosen-users";
                    ddlGroup.DataSource = groupUsers;
                    ddlGroup.DataTextField = "Value";
                    ddlGroup.DataValueField = "Key";
                    ddlGroup.DataBind();


                    if (value != null && value != "")
                    {
                        SPFieldUserValueCollection userValueCollection = new SPFieldUserValueCollection(Web, value);
                        SPUser userD = userValueCollection[0].User;
                        ddlGroup.SelectedValue = userD.LoginName;
                    }


                    controlUser = (Control)ddlGroup;
                    return controlUser;
  
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "DrawControlType_User() - " + ex.Message);

            }

            return controlUser;
        }

        private static void user_changed(object sender, EventArgs e)
        {
            try
            {
                DropDownList ddl = (DropDownList)sender;
                string selectedUser = (string)ddl.SelectedItem.Value;
                string wfid = HttpContext.Current.Session["FormWFID"].ToString();

                if (!String.IsNullOrEmpty(selectedUser))
                {
                    SPUser spSelectedUser = SPContext.Current.Web.EnsureUser(selectedUser);
                    RetainControlValueGeneralFields(ddl.ID, spSelectedUser.ID.ToString() + ";#" + spSelectedUser.Name, wfid);
                }
                else
                {
                    RetainControlValueGeneralFields(ddl.ID, String.Empty, wfid);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "user_changed() - " + ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void txtBoxUser_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string wfid = HttpContext.Current.Session["FormWFID"].ToString();
                TextBox txt = (TextBox)sender;
                RetainControlValueGeneralFields(txt.ID, txt.Text, wfid);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "txt_TextChanged() - " + ex.Message);

            }
        }

        private static TextBox DrawControlType_Note(string columnName, string value)
        {
            TextBox txt = null;

            try
            {
                txt = new TextBox();
                txt.ID = columnName;
                txt.CssClass = "input_textarea_general_field";
                txt.Attributes.Add("style", "resize: none;");
                txt.AutoPostBack = true;
                txt.TextChanged += new EventHandler(txt_TextChanged);
                txt.TextMode = TextBoxMode.MultiLine;
                txt.Rows = 6;

                if (!string.IsNullOrEmpty(value))
                    txt.Text = value;

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "DrawControlType_TextBox() - " + ex.Message);
            }

            return txt;
        }

        #endregion

        #region <SAVE>

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="WFID"></param>
        /// <param name="order"></param>
        /// <param name="placeHolder"></param>
        /// <param name="wfName"></param>
        /// <param name="itm"></param>
        /// <param name="MyWeb"></param>
        /// <param name="loggedUser"></param>
        public static void SaveGeneralFields(string WFID, string order, string wfName, SPListItem itm, SPWeb MyWeb, SPUser loggedUser, Dictionary<string, string> generalFieldsDictionary, PlaceHolder PlaceHolder_GFTable, Dictionary<string, string> parameters)
        {
            try
            {

                if (generalFieldsDictionary != null)
                {
                    SaveMetadatas(itm, generalFieldsDictionary, MyWeb, WFID, loggedUser, PlaceHolder_GFTable, parameters);
                    SaveWFSubject(WFID, PlaceHolder_GFTable, itm); //Force that the WF Subject was saved.
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(WFID, "SaveGeneralFields() - " + ex.Message);
            }
        }

        private static void SaveWFSubject(string wfid, PlaceHolder PlaceHolder_GFTable, SPListItem item)
        {
            try
            {
                SPField field = item.Fields.GetFieldByInternalName("WFSubject");

                if (field != null)
                {

                    TextBox txt = (TextBox)PlaceHolder_GFTable.FindControl(field.Title);

                    if (!string.IsNullOrEmpty(txt.Text))
                        item["WFSubject"] = txt.Text;

                    using (new DisabledItemEventsScope())
                    {
                        item.SystemUpdate();
                    }
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SaveWFSubject() - " + ex.Message);
            }

        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="itm"></param>
        /// <param name="generalFieldsDictionary"></param>
        /// <param name="generalFieldsValuesList"></param>
        /// <param name="Web"></param>
        /// <param name="wfid"></param>
        /// <param name="loggedUser"></param>
        private static void SaveMetadatas(SPListItem itm, Dictionary<string, string> generalFieldsDictionary, SPWeb Web, string wfid, SPUser loggedUser, PlaceHolder PlaceHolder_GFTable, Dictionary<string, string> parameters)
        {
            try
            {
              

                if (generalFieldsDictionary != null)
                {

                    foreach (KeyValuePair<String, String> kvp in generalFieldsDictionary)
                    {

                        string columnName = kvp.Key;
                        SPField column = itm.Fields[columnName];
                        string internalName = column.InternalName.ToString();
                        string fieldType = column.Type.ToString();

                        try
                        {
                            switch (fieldType)
                            {
                                case "Text":
                                case "Note":
                                    SaveGFTextField(wfid, internalName, columnName, ref itm, PlaceHolder_GFTable);
                                    break;

                                case "Choice":
                                    SaveGFChoiceField(wfid, internalName, columnName, ref itm, PlaceHolder_GFTable, column);
                                    break;

                                case "DateTime":
                                    SaveGFDateTimeField(wfid, internalName, columnName, ref itm, PlaceHolder_GFTable);
                                    break;

                                case "Boolean":
                                    SaveGFBooleanField(wfid, internalName, columnName, ref itm, PlaceHolder_GFTable);
                                    break;


                                case "User":
                                    SaveGFUserField(wfid, internalName, columnName, ref itm, PlaceHolder_GFTable, Web);
                                    break;

                            }
                        }
                        catch (Exception ex)
                        {
                            General.saveErrorsLog(wfid, "SaveMetadatas() - SiteColumn: " + internalName + " " + ex.Message);
                            continue;
                        }

                    }
                }

                itm["Editor"] = loggedUser;

                using (new DisabledItemEventsScope())
                {
                    itm.Update();
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SaveMetadatas() - " + ex.Message);
            }
        }

        private static void SaveGFTextField(string wfid, string internalName, string columnName, ref SPListItem itm, PlaceHolder PlaceHolder_GFTable)
        {
            try
            {
                TextBox txt = (TextBox)PlaceHolder_GFTable.FindControl(columnName);

                if (!string.IsNullOrEmpty(txt.Text))
                    itm[internalName] = txt.Text;
                else
                    itm[internalName] = null;

                using (new DisabledItemEventsScope())
                {
                    itm.SystemUpdate(false);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SaveGFTextField() - SiteColumn: " + internalName + " " + ex.Message);
            }

        }

        private static void SaveGFChoiceField(string wfid, string internalName, string columnName, ref SPListItem itm, PlaceHolder PlaceHolder_GFTable, SPField column)
        {
            try
            {
                SPFieldChoice choiceColumn = (SPFieldChoice)column;
                if (choiceColumn.EditFormat == SPChoiceFormatType.RadioButtons)
                {
                    RadioButtonList rb = (RadioButtonList)PlaceHolder_GFTable.FindControl(columnName);

                    if (rb != null)
                        itm[internalName] = rb.SelectedValue;
                    else
                        itm[internalName] = null;
                }
                else if (choiceColumn.EditFormat == SPChoiceFormatType.Dropdown)
                {
                    DropDownList ddl = (DropDownList)PlaceHolder_GFTable.FindControl(columnName);

                    if (ddl != null)
                        itm[internalName] = ddl.SelectedValue;
                    else
                        itm[internalName] = null;
                }

                using (new DisabledItemEventsScope())
                {
                    itm.SystemUpdate(false);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SaveGFChoiceField() - SiteColumn: " + internalName + " " + ex.Message);
            }

        }

        private static void SaveGFDateTimeField(string wfid, string internalName, string columnName, ref SPListItem itm, PlaceHolder PlaceHolder_GFTable)
        {
            try
            {
                DateTimeControl dt = (DateTimeControl)PlaceHolder_GFTable.FindControl(columnName);
                TextBox txtBox = (TextBox)dt.FindControl("txt_" + columnName);

                DateTime dtv;
                if (!string.IsNullOrEmpty(txtBox.Text) && FormatDate(txtBox.Text, out dtv))
                    itm[internalName] = dtv;
                else
                    itm[internalName] = null;

                using (new DisabledItemEventsScope())
                {
                    itm.SystemUpdate(false);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SaveGFDateTimeField() - SiteColumn: " + internalName + " " + ex.Message);
            }

        }

        private static void SaveGFBooleanField(string wfid, string internalName, string columnName, ref SPListItem itm, PlaceHolder PlaceHolder_GFTable)
        {
            try
            {
                CheckBox chk = (CheckBox)PlaceHolder_GFTable.FindControl(columnName);
                itm[internalName] = FormatCheckBoxValue(chk.Checked);

                using (new DisabledItemEventsScope())
                {
                    itm.SystemUpdate(false);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SaveGFBooleanField() - SiteColumn: " + internalName + " " + ex.Message);
            }

        }

        private static void SaveGFUserField(string wfid, string internalName, string columnName, ref SPListItem itm, PlaceHolder PlaceHolder_GFTable, SPWeb Web)
        {
            try
            {
                string controlID = columnName; //FormatColumnName(columnName);

                SPFieldUserValueCollection userValueCollection = new SPFieldUserValueCollection();
                DropDownList pplEditor = (DropDownList)PlaceHolder_GFTable.FindControl(controlID);
                string user = pplEditor.SelectedValue;

                if (!string.IsNullOrEmpty(user))
                {
                    SPUser userd = null;

                    try
                    {
                        userd = Web.EnsureUser(user);
                    }
                    catch
                    {
                        userd = Web.Site.RootWeb.EnsureUser(user);
                    }

                    if (userd != null)
                    {
                        SPFieldUserValue userValue = new SPFieldUserValue(Web, userd.ID, userd.LoginName);
                        userValueCollection.Add(userValue);
                    }

                    itm[internalName] = userValueCollection;
                }

                using (new DisabledItemEventsScope())
                {
                    itm.SystemUpdate(false);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SaveGFUserField() - SiteColumn: " + internalName + " " + ex.Message);
            }

        }

        #region <GET VALUES>

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ControlID"></param>
        /// <param name="placeHolder"></param>
        /// <returns></returns>
        public static string GetValue_TextBox(string ControlID, PlaceHolder placeHolder)
        {
            try
            {
                string value = string.Empty;
                TextBox txt = (TextBox)placeHolder.FindControl(ControlID);
                string txtAux = txt.Text.ToUpper().Trim();

                if (txt != null && !string.IsNullOrEmpty(txt.Text) && !txtAux.Equals("<DIV></DIV>"))
                    value = txt.Text;

                return value;
            }
            catch
            {
                return string.Empty;
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ControlID"></param>
        /// <param name="placeHolder"></param>
        /// <returns></returns>
        public static string GetValue_DateTime(string ControlID, PlaceHolder placeHolder)
        {
            string value = string.Empty;

            try
            {

                DateTimeControl dt = (DateTimeControl)placeHolder.FindControl(ControlID);

                foreach (Control ctrl in dt.Controls)
                {
                    if (ctrl is TextBox)
                    {
                        TextBox txtBox = (TextBox)ctrl;
                        value = txtBox.Text;

                        break;
                    }
                }

                if (!string.IsNullOrEmpty(value))
                {
                    DateTime dtv;
                    value = (FormatDate(value, out dtv)) ? Convert.ToString(dtv) : null;
                }

                return value;
            }
            catch (Exception ex)
            {
                //General.saveErrorsLog(string.Empty, "Value: " + value + " - GetValue_DateTime() - " + ex.Message);
            }

            return value;
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ControlID"></param>
        /// <param name="placeHolder"></param>
        /// <returns></returns>
        public static string GetValue_Choice(string ControlID, PlaceHolder placeHolder)
        {
            try
            {
                string value = string.Empty;
                RadioButtonList rbl = (RadioButtonList)placeHolder.FindControl(ControlID);

                if (rbl != null)
                    value = rbl.SelectedValue;

                return value;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetValue_Choice() - " + ex.Message);
                return string.Empty;
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ControlID"></param>
        /// <param name="placeHolder"></param>
        /// <returns></returns>
        public static bool GetValue_CheckBox(string ControlID, PlaceHolder placeHolder)
        {
            try
            {
                bool value = false;
                CheckBox chk = (CheckBox)placeHolder.FindControl(ControlID);

                if (chk != null)
                    value = chk.Checked;

                return value;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetValue_CheckBox() - " + ex.Message);
                return false;
            }
        }

        #endregion


        #endregion

        #region <STEP DESCRIPTION>

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="placeHolder"></param>
        /// <param name="wfid"></param>
        /// <param name="wftypeName"></param>
        /// <param name="currentStep"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        public static void LoadStepsDescription(SPListItem item, PlaceHolder placeHolder, string wfid, string wftypeName, int currentStep, SPWeb Web, Dictionary<string, string> parameters)
        {
            try
            {
                placeHolder.Controls.Clear();

                List<string> listStepDescription = WorkflowDataManagement.GetStepDescription(item, currentStep, parameters);

                if (listStepDescription != null)
                {
                    int rows = listStepDescription.Count;
                    Table tbl = new Table();

                    if (rows > 0)
                        tbl.CssClass = "step_description";

                    placeHolder.Controls.Add(tbl);

                    // Now iterate through the table and add your controls 
                    for (int i = 0; i < rows; i++)
                    {
                        TableRow tr = new TableRow();
                        TableCell tc = new TableCell();
                        string description = string.Empty;

                        if (!string.IsNullOrEmpty(listStepDescription[i]))
                        {
                            description = listStepDescription[i].ToString();
                            DrawControlType_Label_StepDescription(description, tr, tc);
                            tbl.Rows.Add(tr);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "LoadStepsDescription() - " + ex.Message);
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="description"></param>
        /// <param name="tr"></param>
        /// <param name="tc"></param>
        private static void DrawControlType_Label_StepDescription(string description, TableRow tr, TableCell tc)
        {
            try
            {
                Label lbl = new Label();
                lbl.Text = description;
                tc.Controls.Add(lbl);
                tr.Cells.Add(tc);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "DrawControlType_Label_StepDescription() - " + ex.Message);
            }
        }

        #endregion

        #region <RETAIN GENERAL FIELDS>

        public static void RetainControlValueGeneralFields(string ControlID, string value, string wfid)
        {
            try
            {

                Dictionary<string, string> GFDictionary = new Dictionary<string, string>();
                GFDictionary = (Dictionary<string, string>)HttpContext.Current.Session["FormGeneralFieldsDictionary" + wfid];

                if (GFDictionary.ContainsKey(ControlID))
                    GFDictionary[ControlID] = value;
                else
                    GFDictionary.Add(ControlID, value);

                HttpContext.Current.Session["FormGeneralFieldsDictionary" + wfid] = GFDictionary;

            }
            catch (Exception ex)
            {
                if (!(ControlID.Equals("Workflow Deadline")))
                    General.saveErrorsLog(wfid, "RetainControlValueGeneralFields() - GFs: '" + ControlID + "'" + ex.Message);
            }
        }

        private static string GetValueGFModified(string nameColumn, string wfid, Dictionary<string, string> generalFieldsSessionDictionary)
        {
            try
            {
                string value = string.Empty;

                if (generalFieldsSessionDictionary != null)
                {

                    int numKeys = generalFieldsSessionDictionary.Count;


                    if (generalFieldsSessionDictionary.ContainsKey(nameColumn) || generalFieldsSessionDictionary.ContainsKey(FormatColumnName(nameColumn)))
                    {
                        if (generalFieldsSessionDictionary.ContainsKey(nameColumn))
                            value = generalFieldsSessionDictionary[nameColumn].ToString();
                        else
                            value = generalFieldsSessionDictionary[FormatColumnName(nameColumn)].ToString();

                        //Value DELETE
                        if (string.IsNullOrEmpty(value))
                            value = "[fieldDelete]";
                    }

                }

                return value;

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetValueGFModified() - " + ex.Message);
                return null;
            }

        }

        #endregion

        #region <FORMAT DATAS>

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullDate"></param>
        /// <param name="wfid"></param>
        /// <returns></returns>
        public static string GetDateOnly(string fullDate, string wfid)
        {
            try
            {
                string date = string.Empty;

                if (fullDate.Contains(" "))
                    date = fullDate.Substring(0, fullDate.IndexOf(" ", 0));

                return date.Trim();
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, ex.Message);
                return null;
            }

        }

        //217;#Deborah Meunier -> Deborah Meunier
        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullUser"></param>
        /// <param name="wfid"></param>
        /// <returns></returns>
        public static string GetUserNameOnly(string fullUser, string wfid)
        {
            string name = string.Empty;
            string[] inf = null;

            try
            {
                if (!string.IsNullOrEmpty(fullUser))
                {
                    if (fullUser.Contains("#"))
                    {
                        inf = Regex.Split(fullUser, "#");
                        name = inf[1];
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, ex.Message);
            }

            return name.Trim();
        }

        //TESTING\sb -> sb
        //-----------------------------------------------------------------------------
        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullName"></param>
        /// <param name="wfid"></param>
        /// <returns></returns>
        public static string GetOnlyUserAccount(string fullName, string wfid)
        {
            string name = string.Empty;

            try
            {

                if (fullName.Contains("\\"))
                {
                    string[] info = fullName.Split('\\');
                    name = info[1];
                }
                else
                    name = fullName;

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, ex.Message);
            }

            return name.Trim();
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static string FormatColumnName(string columnName)
        {
            try
            {
                if (columnName.Contains(" "))
                    columnName = columnName.Replace(" ", string.Empty);

                return columnName;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, ex.Message);
                return null;
            }
        }

        #endregion

        public static Dictionary<string,string> GetGeneralFieldsDictionaryItem(string wfid, string initialGFs, SPWeb web)
        {
            Dictionary<string, string> internalGFsDictionary = new Dictionary<string, string>();

            try
            {
                if (initialGFs.Contains(";#"))
                {
                    string[] generalFieldsColumnName = Regex.Split(initialGFs, ";#");

                    foreach (string internalName in generalFieldsColumnName)
                    {
                        SPField field = web.Fields.GetFieldByInternalName(internalName);

                        if (!internalGFsDictionary.ContainsKey(internalName))
                            internalGFsDictionary.Add(internalName, field.Title);
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetSPFieldChoiceValues() - " + ex.Message);
            }

            return internalGFsDictionary;
        }
    }
}
