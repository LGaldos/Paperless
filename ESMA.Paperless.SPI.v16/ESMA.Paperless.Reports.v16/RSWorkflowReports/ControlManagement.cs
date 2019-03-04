using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.WebControls;

namespace ESMA.Paperless.Reports.v16.RSWorkflowReports
{
    class ControlManagement
    {
        #region <INTERFACE>

        public static void SetReportError(string smsError, Label lbl)
        {
            try
            {
                lbl.Visible = true;
                lbl.Text = smsError;
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("SetReportError() - " + ex.Source, ex.Message);
            }
        }

        #endregion

        #region <CONTROLS>

        public static void EnableControls(bool enableValue, Label lblActor, Label lblType, Label lblIntro, Label lblAnd, PlaceHolder peActorPlaceHolder, DropDownList ddlType, DateTimeControl dtFirst, DateTimeControl dtLast)
        {
            try
            {
                lblActor.Enabled = enableValue;
                lblType.Enabled = enableValue;
                lblIntro.Enabled = enableValue;
                lblAnd.Enabled = enableValue;

                PeopleEditor peActor = (PeopleEditor)peActorPlaceHolder.Controls[0];
                peActor.Enabled = enableValue;
                ddlType.Enabled = enableValue;
                dtFirst.Enabled = enableValue;
                dtLast.Enabled = enableValue;

                if (enableValue == false)
                {
                    ClearControls(peActor, ddlType, dtFirst, dtLast);
                }
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" EnableControls() - " + ex.Source, ex.Message);
            }
        }

        public static void ClearControls(PeopleEditor peActor, DropDownList ddlType, DateTimeControl dtFirst, DateTimeControl dtLast)
        {
            try
            {

                peActor.CommaSeparatedAccounts = null;

                if (ddlType.Items.Count > 0)
                {
                    ddlType.Text = ddlType.Items[0].ToString();
                }

                //((System.Web.UI.Page)System.Web.HttpContext.Current.Handler).ViewState["SelectedWFType"] = "All";
                dtFirst.ClearSelection();
                dtLast.ClearSelection();

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" ClearControls() - " + ex.Source, ex.Message);
            }
        }


        //ALI. 
        /// <summary>
        /// Method shows or hides the controls
        /// </summary>
        /// <param name="visibleValue"></param>
        /// <param name="lblIntro"></param>
        /// <param name="lblAnd"></param>
        /// <param name="lblType"></param>
        /// <param name="lblStatus"></param>
        /// <param name="lblRole"></param>
        /// <param name="lblActor"></param>
        /// <param name="lblConfidential"></param>
        /// <param name="lblCreated"></param>
        /// <param name="lblFreeText"></param>
        /// <param name="dtFirst"></param>
        /// <param name="dtLast"></param>
        /// <param name="ddlType"></param>
        /// <param name="ddlStatus"></param>
        /// <param name="ddlRole"></param>
        /// <param name="peActor"></param>
        /// <param name="cbConfidential"></param>
        /// <param name="peCreated"></param>
        /// <param name="rblistMenuReports"></param>
        /// <param name="btnSearch"></param>
        public static void VisibleControls(bool visibleValue, Label lblIntro, Label lblAnd, Label lblType, Label lblStatus, Label lblRole, Label lblActor, Label lblConfidential, Label lblCreated, Label lblFreeText, DateTimeControl dtFirst, DateTimeControl dtLast, DropDownList ddlType, DropDownList ddlStatus, DropDownList ddlRole, PeopleEditor peActor, CheckBox cbConfidential, PeopleEditor peCreated, RadioButtonList rblistMenuReports, Button btnSearch)
        {
            try
            {

                rblistMenuReports.Visible = visibleValue;

                lblIntro.Visible = visibleValue;
                lblAnd.Visible = visibleValue;
                lblType.Visible = visibleValue;
                lblStatus.Visible = visibleValue;
                lblRole.Visible = visibleValue;
                lblActor.Visible = visibleValue;
                lblConfidential.Visible = visibleValue;
                lblCreated.Visible = visibleValue;
                lblFreeText.Visible = visibleValue;

                dtFirst.Visible = visibleValue;
                dtLast.Visible = visibleValue;
                ddlType.Visible = visibleValue;
                ddlStatus.Visible = visibleValue;
                ddlRole.Visible = visibleValue;
                peActor.Visible = visibleValue;
                cbConfidential.Visible = visibleValue;
                peCreated.Visible = visibleValue;
                //t.Visible = visibleValue;


                btnSearch.Visible = visibleValue;

            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" VisibleControls() - " + ex.Source, ex.Message);
            }
        }

        /*public static void InitControls(ref PlaceHolder peActorPlaceHolder, ref UpdatePanel updatePanel)
        {
            try
            {
                //<SharePoint:PeopleEditor ID="peActor" runat="server" SelectionSet="User" IsValid="true" AllowTypeIn="true" MultiSelect="false" ShowEntityDisplayTextInTextBox="true" AutoPostBack="true"/>
                PeopleEditor peActor = new PeopleEditor();
                peActor.IsValid = true;
                peActor.AllowTypeIn = true;
                peActor.MultiSelect = false;
                peActor.ShowEntityDisplayTextInTextBox = true;
                peActor.AutoPostBack = false;
                peActorPlaceHolder.Controls.Clear();
                peActorPlaceHolder.Controls.Add(peActor);
                updatePanel.Controls.Add(peActorPlaceHolder);
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog("VisibleControls() - " + ex.Source, ex.Message);
            }
        }*/

        #endregion

        #region <CAML QUERY>

        /// <summary>
        /// Create CAML query for keyword search in log lists.
        /// </summary>
        /// <param name="queryList"></param>
        /// <param name="logicConditionStart"></param>
        /// <param name="logicConditionEnd"></param>
        /// <returns></returns>
        protected string GenerateQuery(List<string> queryList, string logicConditionStart, string logicConditionEnd)
        {
            try
            {
                StringBuilder sb = new StringBuilder(string.Empty);

                if (queryList.Count.Equals(1))
                {
                    sb.Append("<Where><And>");
                    sb.Append(queryList[0]);
                    sb.Append("<Eq><FieldRef Name='FSObjType' /><Value Type='Integer'>1</Value></Eq></And></Where>");
                }
                else if (queryList.Count > 0)
                {
                    int count1 = 1;
                    int count2 = 1;

                    while (count1 <= queryList.Count)
                    {
                        int count3 = count2;
                        if (count3 % 2 != 0 && count3 > 1)
                            sb.Insert(0, logicConditionStart);

                        if (queryList.Count >= (count1 + 1))
                        {
                            sb.Append(logicConditionStart);
                            sb.Append(queryList[count1 - 1]);
                            sb.Append(queryList[count1]);
                            sb.Append(logicConditionEnd);
                            count1 += 2;
                            count2 += 2;
                        }
                        else
                        {
                            sb.Append(queryList[count1 - 1]);
                            count1++;
                            count2++;
                        }

                        if (count3 % 2 != 0 && count3 > 1)
                            sb.Append(logicConditionEnd);
                    }

                    sb.Insert(0, "<Where><And>");
                    sb.Append("<Eq><FieldRef Name='FSObjType' /><Value Type='Integer'>1</Value></Eq></And></Where>");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Methods.SaveErrorsLog(" GenerateQuery() - " + ex.Source, ex.Message);
                return string.Empty;
            }
        }



        #endregion
    }
}
