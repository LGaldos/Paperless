using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace ESMA.Paperless.Reports.v16.RSWorkflowReports
{
    class ReportsQuery
    {        

        //Created - From + Modified - To
        public static void CreateQuery_DateTimeFromTo(ref List<string> queryList, DateTimeControl dtFrom, DateTimeControl dtTo)
        {
            try
            {
                CreateQuery_DateTimeFromTo(ref queryList, dtFrom.SelectedDate, dtTo.SelectedDate);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQuery_DateTimeFromTo: " + ex.Message, null);
            }
        }

        public static void CreateQuery_DateTimeFromTo(ref List<string> queryList, DateTime fromDateValue, DateTime toDateValue)
        {
            try
            {
                //From
                if (!string.IsNullOrEmpty(fromDateValue.ToShortDateString()))
                    queryList.Add(DateTimeQuery(fromDateValue, true));
                //To
                if (!string.IsNullOrEmpty(toDateValue.ToShortDateString()))
                    queryList.Add(DateTimeQuery(toDateValue, false));
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQuery_DateTimeFromTo: " + ex.Message, null);
            }
        }

        private static string DateTimeQuery(DateTime date, bool isFirst)
        {
            string dateQuery = string.Empty;

            try
            {
                if (isFirst)
                    dateQuery = "<Geq><FieldRef Name='Created' /><Value Type='DateTime'>" + date.ToString("yyyy-MM-ddThh:mm:ssZ") + "</Value></Geq>";
                else
                    dateQuery = "<Leq><FieldRef Name='Created' /><Value Type='DateTime'>" + date.ToString("yyyy-MM-ddThh:mm:ssZ") + "</Value></Leq>";
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("DateTimeQuery: " + ex.Message, dateQuery);
            }

            return dateQuery;
        }

        //WFType
        public static void CreateQuery_WFType(ref List<string> queryList, DropDownList ddlType)
        {
            CreateQuery_WFType(ref queryList, ddlType.SelectedValue);
        }

        public static void CreateQuery_WFType(ref List<string> queryList, string wfTypeValue)
        {
            try
            {
                if (!wfTypeValue.Equals("All") && !string.IsNullOrEmpty(wfTypeValue))
                {
                    if (wfTypeValue.Contains("/"))
                        wfTypeValue = wfTypeValue.Split('/')[0];

                    queryList.Add("<Contains><FieldRef Name='WFType' /><Value Type='Text'>" + "<![CDATA[" + wfTypeValue + "]]>" + "</Value></Contains>");
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQuery_WFType: " + ex.Message, null);
            }
        }

        //WFStatus
        public static void CreateQuery_WFStatus(ref List<string> queryList, DropDownList ddlStatus)
        {
            CreateQuery_WFStatus(ref queryList, ddlStatus.SelectedValue);
        }

        public static void CreateQuery_WFStatus(ref List<string> queryList, string wfStatusValue)
        {
            try
            {
                if (!wfStatusValue.Equals("All") && !string.IsNullOrEmpty(wfStatusValue))
                    queryList.Add("<Eq><FieldRef Name='WFStatus' /><Value Type='Text'>" + wfStatusValue + "</Value></Eq>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQuery_WFStatus: " + ex.Message, null);
            }
        }

        //Role
        public static void CreateQuery_Role(ref List<string> queryList, SPWeb Web, DropDownList ddlRole, string adGroupName)
        {
            CreateQuery_Role(ref queryList, Web, ddlRole.SelectedValue, adGroupName);
        }

        public static void CreateQuery_Role(ref List<string> queryList, SPWeb Web, string roleValue, string adGroupName)
        {
            try
            {
                queryList.Add("<Contains><FieldRef Name='WFActorsSignedRole' /><Value Type='Note'>" + roleValue + "</Value></Contains>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQuery_Role: " + ex.Message, null);
            }
        }

        //Actor
        public static void CreateQuery_Actor(ref List<string> queryList, SPWeb Web, DropDownList ddlActor)
        {
            CreateQuery_Actor(ref queryList, Web, ddlActor.SelectedValue);
        }

        public static void CreateQuery_Actor(ref List<string> queryList, SPWeb Web, string actorValue)
        {
            try
            {
                string loginName = Permissions.GetUserAccountFromActorSelected(Web, actorValue);
                queryList.Add("<Contains><FieldRef Name='WFActorsSignedRole' /><Value Type='Note'>" + loginName + "</Value></Contains>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQuery_Actor: " + ex.Message, null);
            }
        }

        //Role + Actor
        public static void CreateQuery_ActorRole(ref List<string> queryList, SPWeb Web, DropDownList ddlActor, DropDownList ddlRole, Dictionary<string, string> parameters, string adGroupName)
        {
            CreateQuery_ActorRole(ref queryList, Web, ddlActor.SelectedValue, ddlRole.SelectedValue, parameters, adGroupName);
        }
        
        public static void CreateQuery_ActorRole(ref List<string> queryList, SPWeb Web, string actorValue, string roleValue, Dictionary<string, string> parameters, string adGroupName)
        {
            try
            {
                //1;#defaultfia;#sp-paperless-local-staff
                string loginName = Permissions.GetUserAccountFromActorSelected(Web, actorValue);
                string concatInf = loginName + ";#" + adGroupName;

                queryList.Add("<Contains><FieldRef Name='WFActorsSignedRole' /><Value Type='Note'>" + concatInf + "</Value></Contains>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQuery_ActorRole: " + ex.Message, null);
            }
        }

        //ConfidentialWorkflow
        public static void CreateQuery_WFRestricted(ref List<string> queryList, DropDownList ddlRestricted)
        {
            CreateQuery_WFRestricted(ref queryList, ddlRestricted.SelectedValue);
        }

        public static void CreateQuery_WFRestricted(ref List<string> queryList, string wfRestrictedValue)
        {
            try
            {
                if (!wfRestrictedValue.Equals("All") && !string.IsNullOrEmpty(wfRestrictedValue))
                {
                    queryList.Add("<Eq><FieldRef Name='ConfidentialWorkflow' /><Value Type='Choice'>" + wfRestrictedValue + "</Value></Eq>");
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQuery_WFRestricted: " + ex.Message, null);
            }
        }

        //Created by
        public static void CreateQuery_WFCreatedBy(ref List<string> queryList, SPUser userCreatedBy)
        {
            try
            {
                queryList.Add("<Eq><FieldRef Name='Author' Nullable='TRUE' LookupId='True' /><Value Type='Integer'>" + userCreatedBy.ID + "</Value></Eq>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQuery_WFCreatedBy: " + ex.Message, null);
            }
        }

        public static void CreateQuery_PersonalFile(ref List<string> queryList, CheckBox cbPersonalFile)
        {
            CreateQuery_PersonalFile(ref queryList, cbPersonalFile.Checked);
        }

        //Personal File
        public static void CreateQuery_PersonalFile(ref List<string> queryList, bool cbValue)
        {
            try
            {

                    if (cbValue)
                        queryList.Add("<Eq><FieldRef Name='GFPersonalFile' /><Value Type='Boolean'>1</Value></Eq>");            

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQuery_PersonalFile: " + ex.Message, null);
            }
        }

        //Open Amount RAL 
        public static void CreateQueryKeyword_OpenAmountRAL(ref List<string> queryList, string keyword)
        {
            try
            {
                queryList.Add("<Eq><FieldRef Name='GFOpenAmountRAL' /><Value Type='Text'>" + keyword + "</Value></Eq>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQueryKeyword_OpenAmountRAL: " + ex.Message, null);
            }

        }

        //Amount Current Year 
        public static void CreateQueryKeyword_AmountCurrentYear(ref List<string> queryList, string keyword)
        {
            try
            {
                queryList.Add("<Eq><FieldRef Name='GFAmountCurrentYear' /><Value Type='Text'>" + keyword + "</Value></Eq>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQueryKeyword_AmountCurrentYear: " + ex.Message, null);
            }

        }

        //Amount Next Year 
        public static void CreateQueryKeyword_AmountNextYear(ref List<string> queryList, string keyword)
        {
            try
            {
                queryList.Add("<Eq><FieldRef Name='GFAmountNextYear' /><Value Type='Text'>" + keyword + "</Value></Eq>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQueryKeyword_AmountNextYear: " + ex.Message, null);
            }

        }

        //Amount To Cancel (by keywords) 
        public static void CreateQueryKeyword_AmountToCancel(ref List<string> queryList, string keyword)
        {
            try
            {
                queryList.Add("<Eq><FieldRef Name='GFAmountToCancel' /><Value Type='Text'>" + keyword + "</Value></Eq>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQueryKeyword_AmountToCancel: " + ex.Message, null);
            }

        }

        //Justification (by keywords) 
        public static void CreateQueryKeyword_Justification(ref List<string> queryList, string keyword)
        {
            try
            {
                queryList.Add("<Contains><FieldRef Name='GFJustification' /><Value Type='Text'>" + keyword + "</Value></Contains>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQueryKeyword_Justification: " + ex.Message, null);
            }

        }

        //GL Account (by keywords) 
        public static void CreateQueryKeyword_GLAccount(ref List<string> queryList, string keyword)
        {
            try
            {
                queryList.Add("<Eq><FieldRef Name='GFGLAccount' /><Value Type='Text'>" + keyword + "</Value></Eq>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQueryKeyword_GLAccount: " + ex.Message, null);
            }

        }

        //Budget Line (by keywords) 
        public static void CreateQueryKeyword_BudgetLine(ref List<string> queryList, string keyword)
        {
            try
            {
                queryList.Add("<Contains><FieldRef Name='GFBudgetLine' /><Value Type='Text'>" + keyword + "</Value></Contains>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQueryKeyword_BudgetLine: " + ex.Message, null);
            }

        }

        //Restricted (by Keywords)
        public static void CreateQueryKeyword_Restricted(ref List<string> queryList, string keyword)
        {
            try
            {
                queryList.Add("<Eq><FieldRef Name='ConfidentialWorkflow' /><Value Type='Choice'>" + keyword + "</Value></Eq>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQueryKeyword_Restricted: " + ex.Message, null);
            }

        }

        //WF Subject (by Keywords)
        public static void CreateQueryKeyword_WFSubject(ref List<string> queryList, string keyword)
        {
            try
            {
                queryList.Add("<Eq><FieldRef Name='WFSubject' /><Value Type='Text'>" + keyword + "</Value></Eq>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" CreateQueryKeyword_WFSubject: " + ex.Message, null);
            }

        }

        //Amount (by Keywords)
        public static void CreateQueryKeyword_Amount(ref List<string> queryList, string keyword)
        {
            try
            {
                queryList.Add("<Contains><FieldRef Name='Amount' /><Value Type='Text'>" + keyword + "</Value></Contains>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQueryKeyword_Amount: " + ex.Message, null);
            }

        }

        //Link to WF (by Keywords)
        public static void CreateQueryKeyword_LinkToWF(ref List<string> queryList, string keyword)
        {
            try
            {
                queryList.Add("<Contains><FieldRef Name='LinkToWorkflow' /><Value Type='Note'>" + keyword + "</Value></Contains>");
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateQueryKeyword_LinkToW: " + ex.Message, null);
            }

        }


        public static string CreateWhereClause(string OperatorClause, List<string> queryList)
        {
            string query = string.Empty;

            try
            {

                string MiddleQuery = string.Empty;
                string firstquery = string.Empty;
                string lastquery = string.Empty;
                string firstOperator = string.Empty;
                string lastOperator = string.Empty;
                int cont = 0;

                if (OperatorClause == "Or")
                {
                    firstOperator = "<Or>";
                    lastOperator = "</Or>";
                }
                else
                {
                    firstOperator = "<And>";
                    lastOperator = "</And>";
                }

                if (queryList.Count > 1)
                    firstquery = firstOperator;

                if (queryList.Count > 1)
                    lastquery = lastOperator;

                foreach (string field in queryList)
                {
                    cont++;
                    if (queryList.Count > 2 && cont > 1 && cont < queryList.Count)
                    {
                        MiddleQuery = MiddleQuery + firstOperator;
                        lastquery = lastOperator + lastquery;
                    }

                    MiddleQuery = MiddleQuery + field;
                }

                query = firstquery + MiddleQuery + lastquery;

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateWhereClausule: " + ex.Message, query);
            }

            return query;
        }

        public static void CreateDinamicQueryForEspecificGFs(ref List<string> queryList, Dictionary<string, SPField> GFieldsDictionary, string valueToFind)
        {

            try
            {


                foreach (KeyValuePair<string, SPField> entry in GFieldsDictionary)
                {
                    string internalName = entry.Key;
                    SPField field = entry.Value;
                    string fieldType = field.Type.ToString();


                    switch (fieldType)
                    {
                        case "Text":
                            queryList.Add("<Contains><FieldRef Name='" + internalName + "' /><Value Type='Text'>" + valueToFind + "</Value></Contains>");
                            break;

                        case "Note":
                            queryList.Add("<Contains><FieldRef Name='" + internalName + "' /><Value Type='Note'>" + valueToFind + "</Value></Contains>");
                            break;

                        case "Choice":
                            queryList.Add("<Contains><FieldRef Name='" + internalName + "' /><Value Type='Choice'>" + valueToFind + "</Value></Contains>");
                            break;


                    }
                }

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("CreateDinamicQueryForEspecificGFs: " + ex.Message, string.Empty);
            }

        }

        public static List<string> GetAllEspecificGeneralFieldsFromList(SPWeb Web)
        {
            List<string> fieldsList = new List<string>();

            try
            {

                SPList list = Web.GetListFromWebPartPageUrl(Web.Url + "/Lists/WFGeneralFields/AllItems.aspx");

                SPQuery query = new SPQuery();
                query.Query = "<Where><IsNotNull><FieldRef Name='Title' /></IsNotNull></Where>";
                query.ViewFields = string.Concat(
                               "<FieldRef Name='Title' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need
                SPListItemCollection itemCollection = list.GetItems(query);


                foreach (SPListItem item in itemCollection)
                {
                    fieldsList.Add(item["Title"].ToString());
                }


            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GetAllEspecificGeneralFieldsFromList(): " + ex.Message, ex.StackTrace);
            }

            return fieldsList;
        }

        public static Dictionary<string, SPField> GetGFsDictionary()
        {
            Dictionary<string, SPField> GFieldsDictionary = new Dictionary<string, SPField>();

            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite Site = new SPSite(SPContext.Current.Web.Url.ToString()))
                    {
                        SPWeb Web = Site.OpenWeb();
                        SPFieldCollection allFields = Web.Fields;

                        //Get All GFs fro "RS Workflow GFs" list
                        List<string> GFsList = GetAllEspecificGeneralFieldsFromList(Web);

                        foreach (SPField field in allFields)
                        {
                            if (field.Group.Equals("RS Columns"))
                            {
                                string displayName = field.Title;
                                string internalName = field.InternalName;

                                if (GFsList.Contains(displayName))
                                    GFieldsDictionary.Add(internalName, field);

                            }

                        }


                        Web.Close();
                        Web.Dispose();
                    }

                });

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("GetSiteColumnsFromRSGroup(): " + ex.Message, ex.StackTrace);
            }

            return GFieldsDictionary;
        }

    }
}
