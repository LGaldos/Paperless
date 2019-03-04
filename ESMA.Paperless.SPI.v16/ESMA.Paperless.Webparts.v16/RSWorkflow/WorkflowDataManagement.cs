using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Web;
using System.ComponentModel;
using System.Reflection;
using Microsoft.SharePoint.Utilities;
using System.Web.UI;
using System.Text.RegularExpressions;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Web.UI.WebControls;

namespace ESMA.Paperless.Webparts.v16.RSWorkflow
{
    static class WorkflowDataManagement
    {
        #region WorkflowConfiguration

        /// <summary>
        /// Get item from "RS Workflow Configuration" list with WFOrder (including item attachments)
        /// </summary>
        public static SPListItem GetWorkflowTypeConfiguration(string wforder, SPWeb Web)
        {
            SPListItem resultItem = null;

            try
            {
                SPList list = Web.Lists["RS Workflow Configuration"];

                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='WFOrder'/><Value Type='Text'>" + wforder + "</Value></Eq></Where>";
                query.ViewFields = string.Concat(
                    "<FieldRef Name='WFOrder' />",
                    "<FieldRef Name='DocumentationType' />",
                    "<FieldRef Name='Attachments' />",
                    "<FieldRef Name='ConfidentialWorkflow' />",
                    "<FieldRef Name='Title' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need
                query.IncludeAttachmentUrls = true;

                SPListItemCollection itemCollection = list.GetItems(query);

                if (itemCollection != null && itemCollection.Count.Equals(1))
                    resultItem = itemCollection[0];
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetWorkflowTypeConfiguration " + ex.Message);
            }

            return resultItem;
        }

        /// <summary>
        /// Get workflow type name by worfklow identifier.
        /// </summary>
        /// <param name="wforder"></param>
        /// <param name="Web"></param>
        /// <returns>Workflow type title</returns>
        public static string GetWorkflowTypeName(string wforder, SPWeb Web)
        {
            string wftype = string.Empty;
            string errorMessage = string.Empty;

            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate() {

                    using (SPSite site = new SPSite(Web.Url)){
                        using (SPWeb webi = site.OpenWeb()){

                            SPList list = webi.Lists["RS Workflow Configuration"];

                            if (list != null && list.Fields.ContainsFieldWithStaticName("WFOrder"))
                            {
                                SPQuery query = new SPQuery();
                                query.Query = "<Where><Eq><FieldRef Name='WFOrder'/><Value Type='Text'>" + wforder + "</Value></Eq></Where>";
                                query.ViewFields = string.Concat(
                                  "<FieldRef Name='WFOrder' />",
                                  "<FieldRef Name='Title' />");
                                query.ViewFieldsOnly = true; // Fetch only the data that we need

                                SPListItemCollection itemCollection = list.GetItems(query);

                                if (itemCollection != null && itemCollection.Count.Equals(1))
                                    wftype = itemCollection[0].Title;
                                }
                            }

                    
                    }
                });
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetWorkflowTypeName " + ex.Message);
            }

            return wftype;
        }

        #endregion

        #region WorkflowLibrary

        /// <summary>
        /// Remove workflow before launching
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <param name="WFID"></param>
        /// <param name="wftypeName"></param>
        public static void RemoveWorkflowOnCreation(SPListItem item, SPList list, SPWeb Web, Dictionary<string, string> parameters, string wfid, string status)
        {
            try
            {
                if (item != null && parameters.ContainsKey("Status Draft") && status.ToUpper().Equals(parameters["Status Draft"].ToUpper()))
                {
                    using (new DisabledItemEventsScope())
                    {
                        item.Delete();
                    }
                }
                     
                
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "RemoveWorkflowOnCreation " + ex.Message);
            }
        }

        /// <summary>
        /// Ensure workflow existence.
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="item"></param>
        /// <param name="WFID"></param>
        /// <param name="wftype"></param>
        /// <param name="wftypeName"></param>
        /// <param name="sessionCreating"></param>
        /// <param name="itemIsOld"></param>
        /// <param name="itemExists"></param>
        /// <param name="parameters"></param>
        /// <param name="loggedUser"></param>
        public static void WorkflowSetUpOnLoad(SPList list, SPWeb Web, ref SPListItem item, string wfid, SPListItem wfTypeConfiguration, object sessionCreating, ref bool itemIsOld, ref bool itemExists, Dictionary<string, string> parameters, SPUser loggedUser, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, int currentStep, bool isSaving)
        {
            try
            {
                if (sessionCreating != null)
                    itemIsOld = false;

                if (item != null)
                    itemExists = true;
                else
                {
                    if (parameters.ContainsKey("Status Draft"))
                        item = EnsureWorkflowItemExistence(list, ref itemExists, wfid, wfTypeConfiguration, parameters["Status Draft"], string.Empty, false, string.Empty, string.Empty, null, "Non Restricted", loggedUser, loggedUser, Web, parameters, actorsBackupDictionary, reassignToBackupActor, currentStep, isSaving);
                    
                    if (item != null)
                        itemExists = true;
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "WorkflowSetUpOnLoad() " + ex.Message);
            }
        }

        /// <summary>
        /// Ensure workflow existence and return existing item.
        /// </summary>
        /// <param name="itemExists"></param>
        /// <param name="WFID"></param>
        /// <param name="wftypeName"></param>
        /// <param name="wftypeOrder"></param>
        /// <param name="status"></param>
        /// <param name="wflink"></param>
        /// <param name="urgent"></param>
        /// <param name="subject"></param>
        /// <param name="amount"></param>
        /// <param name="deadline"></param>
        /// <param name="confidential"></param>
        /// <param name="loggedUser"></param>
        /// <param name="realEditor"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <returns>Workflow main object</returns>
        public static SPListItem EnsureWorkflowItemExistence(SPList list, ref bool itemExists, string wfid, SPListItem wfTypeConfiguration, string status, string wflink, bool urgent, string subject, string amount, object deadline, string confidential, SPUser loggedUser, SPUser realEditor, SPWeb Web, Dictionary<string, string> parameters, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, int currentStep, bool isSaving)
        {
            SPListItem item = null;
            try
            {
                string wftypeName = wfTypeConfiguration["Title"].ToString();
                item = GetWorkflowItem(wfid, wftypeName, Web);

                if (item == null)
                    item = CreateWorkflowItem(list, wfTypeConfiguration, wfid, status, wflink, urgent, Web, loggedUser, subject, amount, deadline, confidential, parameters, realEditor, actorsBackupDictionary, reassignToBackupActor, currentStep, isSaving);
                else
                    itemExists = true;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "EnsureWorkflowItemExistence " + ex.Message);
            }
            return item;
        }

        /// <summary>
        /// Get workflow item by workflow ID.
        /// </summary>
        /// <param name="WFID"></param>
        /// <param name="wftype"></param>
        /// <param name="Web"></param>
        /// <returns>Workflow main object</returns>
        public static SPListItem GetWorkflowItem(string wfid, string wftype, SPWeb Web)
        {
            SPListItem item = null;
            try
            {
                SPList list = GetWorkflowLibrary(wftype, Web);

                if (list != null && list.Fields.ContainsFieldWithStaticName("WFID"))
                {
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><Eq><FieldRef Name='WFID'/><Value Type='Text'>" + wfid + "</Value></Eq></Where>";

                    SPListItemCollection itemCollection = list.GetItems(query);

                    if (itemCollection != null && itemCollection.Count.Equals(1))
                        item = itemCollection[0];
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetWorkflowItem " + ex.Message);
            }
            return item;
        }

        /// <summary>
        /// Gets if workflow exists in paperless system
        /// </summary>
        /// <param name="wfid"></param>
        /// <param name="wftype"></param>
        /// <param name="Web"></param>
        /// <returns></returns>
        public static bool DoesWorkflowExists(string wfid, string wftype, SPWeb Web)
        {
            bool exists = true;
            try
            {
                SPSiteDataQuery siteDataQuery = new SPSiteDataQuery();
                siteDataQuery.Lists = "<Lists ServerTemplate='906' />";

                siteDataQuery.Webs = "<Webs Scope='SiteCollection' />";
                siteDataQuery.Query = "<Where><And><Eq><FieldRef Name='WFID'/><Value Type='Text'>" + wfid + "</Value></Eq><Eq><FieldRef Name='WFType'/><Value Type='Text'>" + "<![CDATA[" + wftype + "]]>" + "</Value></Eq></And></Where>";
                siteDataQuery.QueryThrottleMode = SPQueryThrottleOption.Override;
                siteDataQuery.ViewFields = string.Concat(
                                   "<FieldRef Name='WFID' />",
                                   "<FieldRef Name='WFType' />");
               

                DataTable resultTableAux = Web.GetSiteData(siteDataQuery);

                if (!resultTableAux.Rows.Count.Equals(0))
                    exists = true;
                else
                    exists = false;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "DoesWorkflowExists " + ex.Message);
            }
            return exists;
        }

         //CR 24

        /// <summary>
        /// Gets if workflow exists in paperless system
        /// </summary>
        /// <param name="wfid"></param>
        /// <param name="wftype"></param>
        /// <param name="Web"></param>
        /// <returns></returns>
        public static bool DoesWorkflowExists(string wfid)
        {
            bool exists = false;

            if (!String.IsNullOrEmpty(wfid))
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    using (SPSite site = new SPSite(SPContext.Current.Web.Url))
                    {
                        using (SPWeb web = site.OpenWeb())
                        {
                            exists = DoesWorkflowExists(wfid, web);
                        }
                    }
                });
            }

            return exists;
        }

        public static bool DoesWorkflowExists(string wfid, SPWeb web)
        {
            bool exists = false;

            try
            {
                SPSiteDataQuery siteDataQuery = new SPSiteDataQuery();
                siteDataQuery.Lists = "<Lists ServerTemplate='906' />";

                siteDataQuery.ViewFields = "<FieldRef Name='WFID' Type='Number' Nullable='TRUE'/>";

                siteDataQuery.Webs = "<Webs Scope='SiteCollection' />";
                siteDataQuery.Query = "<Where><Eq><FieldRef Name='WFID'/><Value Type='Text'>" + wfid + "</Value></Eq></Where>";
                siteDataQuery.QueryThrottleMode = SPQueryThrottleOption.Override;

                DataTable resultTableAux = web.GetSiteData(siteDataQuery);

                if (!resultTableAux.Rows.Count.Equals(0))
                    exists = true;
                else
                    exists = false;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "DoesWorkflowExists " + ex.Message);
            }

            return exists;
        }

       

        /// <summary>
        /// Get workflow current step responsible.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <returns>SharePoint user object of the current step responsible</returns>
        public static SPUser GetWorkflowCurrentStepResponsible(SPListItem item, SPWeb Web, string wfid, string domain)
        {
            SPUser user = null;

            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsFieldWithStaticName("AssignedPerson") && item["AssignedPerson"] != null)
                    {
                        try
                        {
                            user = General.GetSPUser(item, "AssignedPerson", wfid, Web);
                        }
                        catch 
                        {
                            General.saveErrorsLog(wfid, "Error getting StepResponsable - AssignedPerson NOT exits: " + item["AssignedPerson"].ToString());
                            //If Assigned person does not exist -> Get Default
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetWorkflowCurrentStepResponsible() " + ex.Message);
            }

            return user;
        }


        /// <summary>
        /// Get workflow current step number.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <returns>Integer value of current step number.</returns>
        public static int GetWorkflowCurrentStep(SPListItem item, SPWeb Web, string wfid)
        {
            int step = 1;

            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsFieldWithStaticName("StepNumber") && item["StepNumber"] != null)
                        step = int.Parse(item["StepNumber"].ToString());
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetWorkflowCurrentStep " + ex.Message);
            }

            return step;
        }

        /// <summary>
        /// Set workflow current step.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="step"></param>
        /// <param name="web"></param>
        /// <param name="realEditor"></param>
        public static void SetWorkflowStep(ref SPListItem item, int step, SPWeb web, SPUser realEditor, string wfid)
        {
            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsFieldWithStaticName("StepNumber"))
                    {
                        item["StepNumber"] = step;
                        item["Editor"] = realEditor;

                        using (new DisabledItemEventsScope())
                        {
                            item.Update();
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetWorkflowStep " + ex.Message);
            }
        }

        /// <summary>
        /// Set workflow current step.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="currentStep"></param>
        /// <param name="web"></param>
        /// <param name="realEditor"></param>
        public static void SetWorkflowStepXAssignedTo(ref SPListItem item, int currentStep, SPWeb web, SPUser realEditor, string wfid, SPUser responsible)
        {
            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsFieldWithStaticName("Step_x0020_" + currentStep + "_x0020_Assigned_x0020_To"))
                    {
                        item["Step_x0020_" + currentStep + "_x0020_Assigned_x0020_To"] = responsible;
                        item["Editor"] = realEditor;

                        using (new DisabledItemEventsScope())
                        {
                            item.Update();
                        }

                       
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetWorkflowStepXAssignedTo() " + ex.Message);
            }
        }

        
        /// <summary>
        /// Set Actor who signed + Role. Used in the Advanced Search and Reporting.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="step"></param>
        /// <param name="Web"></param>
        /// <param name="realEditor"></param>
        public static void SetWorkflowActorsSignedRole(ref SPListItem item, SPUser realEditor, string wfid, int stepNumber, SPWeb Web)
        {
            try
            {
                //1;#84;#OFFICE\sp-paperless-local-staff&#2;#81;#OFFICE\sp-paperless-local-oia
                string initialSteps = item["InitialSteps"].ToString();
                string status = item["WFStatus"].ToString();
                List<string> groupList = GetGroupNames(initialSteps, Web, wfid);
                string userName = string.Empty;
                string loginName = realEditor.LoginName;


                General.GetUserData(ref loginName, ref userName);

                if (initialSteps.Contains(stepNumber + ";"))
                {
                    string inf = stepNumber + ";#" + loginName + ";#" + groupList[(stepNumber - 1)];

                    if (item["WFActorsSignedRole"] != null)
                    {
                        if (!(item["WFActorsSignedRole"].ToString().Contains(inf)))
                            item["WFActorsSignedRole"] = item["WFActorsSignedRole"].ToString() + "&#" + inf;
                    }

                    else
                        item["WFActorsSignedRole"] = inf;
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetWorkflowActorsSignedRole() " + ex.Message);
            }
        }

        /// <summary>
        /// Set workflow status to rejected.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="step"></param>
        /// <param name="Web"></param>
        /// <param name="realEditor"></param>
        public static void SetWorkflowStatus(SPListItem item, string newStatus, SPWeb Web, SPUser realEditor, string wfid)
        {
            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsFieldWithStaticName("WFStatus"))
                    {
                        item["WFStatus"] = newStatus;
                        item["Editor"] = realEditor;

                        using (new DisabledItemEventsScope())
                        {
                            item.Update();
                        }

                     
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetWorkflowStatus() " + ex.Message);
            }
        }

        /// <summary>
        /// Set workflow current step responsible.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="user"></param>
        /// <param name="realEditor"></param>
        /// <param name="parameters"></param>
        /// <param name="confidentialValue"></param>
        public static void SetAssignedPersonWorkflow(ref SPListItem item, SPUser stepResponsible, SPUser realEditor, Dictionary<string, string> parameters, string confidentialValue, string wfid, Dictionary<string, string> actorsBackupDictionary, string status, bool reassignToBackupActor, int currentStep, bool isSaving)
        {
            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsFieldWithStaticName("AssignedPerson"))
                    {
                        if (!status.ToLower().Equals(parameters["Status Closed"].ToLower()))
                        {
                            //ESMA-CR31-BackupGroup
                            if (reassignToBackupActor.Equals(true) && (!stepResponsible.ID.Equals(realEditor.ID)))
                            {
                                if (status.ToLower().Equals(parameters["Status On Hold"].ToLower()))
                                    item["AssignedPerson"] = realEditor;
                                else
                                    item["AssignedPerson"] = stepResponsible;
                            }
                            else
                                item["AssignedPerson"] = stepResponsible;
                        }
                        else
                            item["AssignedPerson"] = null;
                        
                        item["Editor"] = realEditor;

                        using (new DisabledItemEventsScope())
                        {
                            item.Update();
                        }


                        Permissions.SetUpWorkflowPermissions(ref item, item, stepResponsible, realEditor, parameters, confidentialValue, wfid, actorsBackupDictionary, status, reassignToBackupActor, currentStep, isSaving);
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, " SetWorkflowHistoryAssignedPerson() " + ex.Message);
            }
        }

        /// <summary>
        /// Set the responsible of a specific step.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="user"></param>
        /// <param name="realEditor"></param>
        /// <param name="fieldName"></param>
        /// <param name="modifiedDate"></param>
        /// <param name="parameters"></param>
        /// <param name="confidentialValue"></param>
        public static void SetWorkflowStepResponsible(ref SPListItem item, SPUser user, SPUser realEditor, string fieldName, DateTime modifiedDate, Dictionary<string, string> parameters, string confidentialValue, string wfid, Dictionary<string, string> actorsBackupDictionary, string status, bool reassignToBackupActor, int currentStep, bool isSaving)
        {
            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsField(fieldName))
                    {
                        item[fieldName] = user;
                        item["Editor"] = realEditor;
                        item["Modified"] = modifiedDate;

                        using (new DisabledItemEventsScope())
                        {
                            item.Update();
                        }

                        Permissions.SetUpWorkflowPermissions(ref item, item, user, realEditor, parameters, confidentialValue, wfid, actorsBackupDictionary, status, reassignToBackupActor, currentStep, isSaving);
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetWorkflowStepResponsible() - " + ex.Message);
            }
        }

        /// <summary>
        /// Get workflow current status.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <returns>Deleted, In Progress, Draft, Closed, or Rejected.</returns>
        public static string GetWorkflowStatus(SPListItem item, SPWeb Web, Dictionary<string, string> parameters, string wfid)
        {
            string status = string.Empty;

            if (parameters.ContainsKey("Status Draft"))
                status = parameters["Status Draft"];

            try
            {
                if (item != null)
                {
                    try
                    {
                        if (item.Fields.ContainsFieldWithStaticName("WFStatus") && item["WFStatus"] != null)
                            status = item["WFStatus"].ToString();
                    }
                    catch
                    {
                        if (parameters.ContainsKey("Status Deleted"))
                            status = parameters["Status Deleted"];
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetWorkflowStatus " + ex.Message);
            }

            return status;
        }



        /// <summary>
        /// Get workflow current urgency.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <returns>TRUE: Urgent FALSE: Urgent</returns>
        public static bool GetWorkflowUrgency(SPListItem item, SPWeb Web)
        {
            bool urgent = false;

            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsFieldWithStaticName("Urgent") && item["Urgent"] != null)
                        urgent = (item["Urgent"].ToString().ToUpper().Equals("TRUE") || item["Urgent"].ToString().ToUpper().Equals("YES") || item["Urgent"].ToString().ToUpper().Equals("1"))?true:false;
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetWorkflowStatus " + ex.Message);
            }

            return urgent;
        }

        /// <summary>
        /// Get workflow current amount.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <returns>Amount as string</returns>
        public static string GetWorkflowAmount(SPListItem item, SPWeb Web)
        {
            string amount = string.Empty;

            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsFieldWithStaticName("Amount") && item["Amount"] != null)
                        amount = item["Amount"].ToString();
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetWorkflowStatus " + ex.Message);
            }

            return amount;
        }

        /// <summary>
        /// Get workflow deadline.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <returns>Deadline as DateTime</returns>
        public static string GetWorkflowDeadline(SPListItem item, SPWeb Web)
        {
            string deadline = string.Empty;

            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsFieldWithStaticName("WFDeadline") && item["WFDeadline"] != null)
                    {
                        deadline = item["WFDeadline"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetWorkflowStatus " + ex.Message);
            }

            return deadline;
        }

        /// <summary>
        /// Set workflow current status.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="status"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <param name="realEditor"></param>
        public static void SetWorkflowStatus(ref SPListItem item, string status, SPWeb Web, Dictionary<string, string> parameters, SPUser realEditor)
        {
            try
            {
                if (item != null)
                {
                    item["WFStatus"] = status;
                    item["Editor"] = realEditor;
                    using (new DisabledItemEventsScope())
                    {
                        item.Update();
                    }
                    item.ParentList.Update();
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "SetWorkflowStatus " + ex.Message);
            }
        }

        /// <summary>
        /// Get workflow subject if it is filled
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <returns>Workflow subject</returns>
        public static string GetWorkflowSubject(SPListItem item, SPWeb Web, string wfid)
        {
            string subject = string.Empty;

            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsFieldWithStaticName("WFSubject") && item["WFSubject"] != null)
                        subject = item["WFSubject"].ToString();
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetWorkflowSubject() " + ex.Message);
            }

            return subject;
        }

        /// <summary>
        /// Get the confidentiality configuration for a workflow.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string GetWorkflowConfidentialValue(SPListItem item, SPWeb Web)
        {
            string confidential = string.Empty;

            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsFieldWithStaticName("ConfidentialWorkflow") && item["ConfidentialWorkflow"] != null)
                        confidential = item["ConfidentialWorkflow"].ToString();
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetWorkflowConfidentialValue " + ex.Message);
            }

            return confidential;
        }

        public static string GetWorkflowInitialStepNotifications(SPListItem item, SPWeb Web, string wfid)
        {
            string initialStepNotifications = string.Empty;

            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsFieldWithStaticName("InitialStepNotifications") && item["InitialStepNotifications"] != null)
                        initialStepNotifications = item["InitialStepNotifications"].ToString();
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetWorkflowInitialStepNotifications() " + ex.Message);
            }

            return initialStepNotifications;
        }

        /// <summary>
        /// Set workflow confidentiality configuration
        /// </summary>
        /// <param name="item"></param>
        /// <param name="confidential"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <param name="realEditor"></param>
        public static void SetWorkflowConfidentialValue(ref SPListItem item, string confidential, SPWeb Web, Dictionary<string, string> parameters, SPUser realEditor)
        {
            try
            {
                if (item != null)
                {
                    item["ConfidentialWorkflow"] = confidential;
                    item["Editor"] = realEditor;
                    using (new DisabledItemEventsScope())
                    {
                        item.Update();
                    }
                    item.ParentList.Update();
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "SetWorkflowConfidentialValue " + ex.Message);
            }
        }

        // CR 24
        /// <summary>
        /// Get the confidentiality configuration for a workflow.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string GetWorkflowLinktoWorkFlowValue(SPListItem item, SPWeb Web, string wfid)
        {
            string txtLinkToWorklflowData = string.Empty;

            try
            {
                if (item.Fields.ContainsFieldWithStaticName("LinkToWorkflow") && item["LinkToWorkflow"] != null)
                    txtLinkToWorklflowData = item["LinkToWorkflow"].ToString();
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetWorkflowLinktoWorkFlowValue " + ex.Message);
            }

            return txtLinkToWorklflowData;
        }

        // CR 24
        /// <summary>
        /// 
        /// </summary>
        /// <param name="txtLinkToWorlFlowData"></param>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <returns></returns>
        private static string GetLinksToWorkflowsWithTypeAndPermissions(string txtLinkToWorlFlowData, SPListItem item, SPWeb Web)
        {
            string[] listWFIDTOSearch = txtLinkToWorlFlowData.Split('|');
            for (int i = 0; i <= listWFIDTOSearch.Length-1; i++)
            {
                string type = "28"; //WorkflowDataManagement.GetWorkflowTypeByWFID(listWFIDTOSearch[i], Web);

                // aqui habria que buscar en este workflow si es confidencial y el usuario tiene acceso
                // porque si no lo tiene se pone el id del link pero sin vinculo

                // si no es confidencial y no es el actor del paso actual no puede hacer nada, pero
                listWFIDTOSearch[i] = listWFIDTOSearch[i].Trim() + ":" + type + ":1";
            }

            return string.Join("|", listWFIDTOSearch);
        }

        // CR 24
        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="Web"></param>
        /// <returns></returns>
        public static string GetWorkflowTypeByWFID(string wfid, SPWeb web)
        {
            string wftypeName = string.Empty;

            try
            {
                SPList configurationList = web.Lists["RS Workflow Configuration"];
                SPQuery myQuery = new SPQuery();
                myQuery.Query = "<Where><IsNotNull><FieldRef Name='Title' /></IsNotNull></Where><OrderBy><FieldRef Name='WFOrder' Ascending='True' /></OrderBy>";
                myQuery.ViewFields = string.Concat(
                                  "<FieldRef Name='WFOrder' />",
                                  "<FieldRef Name='WFLibraryURL' />",
                                  "<FieldRef Name='Title' />");


                myQuery.ViewFieldsOnly = true; // Fetch only the data that we need

                SPListItemCollection itemColl = configurationList.GetItems(myQuery);
               

                foreach (SPListItem itm in itemColl)
                {
                        if (itm["WFLibraryURL"] != null)
                        {
                            SPList WFLibrary = web.GetListFromUrl(itm["WFLibraryURL"].ToString());
                            wftypeName = SearchWorFlowID(wfid, WFLibrary, web);

                            if (wftypeName == itm.DisplayName)
                            {
                                wftypeName = itm["WFOrder"] + ":0";
                                break;

                            } 
                        }
                }
                    
                
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetWorkflowTypeByWFID() - " + ex.Message.ToString());
            }

            return wftypeName;

        }
        // FIN CR 24

        // CR 24
        public static string SearchWorFlowID(string wfid, SPList list, SPWeb web)
        {
            string wftype = string.Empty;

            try
            {

                SPQuery myQuery = new SPQuery();
                myQuery.Query = "<Where><Eq><FieldRef Name=\"WFID\" /><Value Type=\"Number\">" + wfid + "</Value></Eq></Where>";
                myQuery.ViewFields = string.Concat(
                                "<FieldRef Name='WFID' />",
                                "<FieldRef Name='WFType' />");


                myQuery.ViewFieldsOnly = true; // Fetch only the data that we need

                SPListItemCollection myListItems = list.GetItems(myQuery);
                SPListItem itm = null;

                if (myListItems.Count > 0)
                    wftype = myListItems[0]["WFType"].ToString();
 
            }

            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SearchWorFlowID() - " + ex.Message.ToString());
            }

            return wftype;
        }
        // FIN CR 24


        /// <summary>
        /// Generic method that sets the value of a workflow field
        /// </summary>
        /// <param name="item"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="Web"></param>
        /// <param name="realEditor"></param>
        public static void SetWorkflowItem(ref SPListItem item, string field, Object value, SPWeb Web, SPUser realEditor)
        {
            try
            {
                if (item.Fields.ContainsFieldWithStaticName(field))
                {
                    item[field] = value;
                    item["Editor"] = realEditor;
                    using (new DisabledItemEventsScope())
                    {
                        item.Update();
                    }
                    item.ParentList.Update();
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "SetWorkflowItem " + ex.Message);
            }
        }

        /// <summary>
        /// Get workflow SharePoint list by workflow type name.
        /// </summary>
        /// <param name="wfType"></param>
        /// <param name="Web"></param>
        /// <returns>SPList which stores all workflow documentation by workflow type</returns>
        public static SPList GetWorkflowLibrary(string wfType, SPWeb Web)
        {
            SPList list = null;
            try
            {
                list = Web.Lists["RS Workflow Configuration"];

                if (list != null)
                {
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + wfType + "</Value></Eq></Where>";
                    query.ViewFields = string.Concat(
                                   "<FieldRef Name='Title' />",
                                   "<FieldRef Name='WFLibraryURL' />");
                    query.ViewFieldsOnly = true; // Fetch only the data that we need


                    SPListItemCollection itemCollection = list.GetItems(query);

                    if (itemCollection != null && itemCollection.Count.Equals(1))
                    {
                        SPListItem item = itemCollection[0];

                        if (item.Fields.ContainsFieldWithStaticName("WFLibraryURL") && item["WFLibraryURL"] != null)
                            list = Web.GetListFromUrl(item["WFLibraryURL"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetWorkflowLibrary " + ex.Message);
            }
            return list;
        }

        /// <summary>
        /// Create workflow folder and its sub folder structure.
        /// </summary>
        /// <param name="wfType"></param>
        /// <param name="wfTypeCode"></param>
        /// <param name="WFID"></param>
        /// <param name="status"></param>
        /// <param name="wfLink"></param>
        /// <param name="urgent"></param>
        /// <param name="Web"></param>
        /// <param name="responsibleUser"></param>
        /// <param name="subject"></param>
        /// <param name="amount"></param>
        /// <param name="deadline"></param>
        /// <param name="confidential"></param>
        /// <param name="parameters"></param>
        /// <param name="realEditor"></param>
        public static SPListItem CreateWorkflowItem(SPList list, SPListItem wfTypeConfiguration, string wfid, string status, string wfLink, bool urgent, SPWeb Web, SPUser responsibleUser, string subject, string amount, Object deadline, string confidential, Dictionary<string, string> parameters, SPUser realEditor, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, int currentStep, bool isSaving)
        {
            try
            {
                if (list != null)
                {
                    SPContentTypeId id = new SPContentTypeId();
                    SPContentTypeId id2 = new SPContentTypeId();

                    foreach (SPContentType ct in list.ContentTypes)
                    {
                        try
                        {
                            if (ct.Name.ToUpper().Equals("WORKFLOW"))
                                id = ct.Id;
                            else if (ct.Name.ToUpper().Equals("FOLDER"))
                                id2 = ct.Id;
                        }
                        catch { continue; }
                    }

                    if (id != null && id2 != null)
                    {
                        string wfType = wfTypeConfiguration["Title"].ToString();
                        string wfTypeCode = wfTypeConfiguration["WFOrder"].ToString();
                        string initialGFs = string.Empty;
                        
                        //Create workflow main folder with the following metadata
                        SPListItem item = list.Items.Add(list.RootFolder.ServerRelativeUrl, SPFileSystemObjectType.Folder, wfid);

                        item["ContentTypeId"] = id;
                        item["WFID"] = wfid;
                        if (parameters.ContainsKey("Interface Page"))
                        {
                            SPFieldUrlValue urlValue = new SPFieldUrlValue();
                            urlValue.Description = wfid;
                            urlValue.Url = Web.Url + parameters["Interface Page"] + "?wfid=" + wfid + "&wftype=" + wfTypeCode;
                            item["WFLink"] = urlValue;
                        }
                        item["StepNumber"] = 1;
                        item["WFStatus"] = status;
                        if (urgent)
                            item["Urgent"] = 1;
                        else
                            item["Urgent"] = 0;
                        item["WFType"] = wfType;

                        if (!status.ToLower().Equals(parameters["Status Closed"].ToLower()))
                            item["AssignedPerson"] = responsibleUser;
                        else
                            item["AssignedPerson"] = null;

                        item["Step_x0020_1_x0020_Assigned_x0020_To"] = responsibleUser;


                        item["WFSubject"] = subject;
                        item["Amount"] = amount;

                        if (deadline != null)
                        {
                            try
                            {
                                DateTime deadlineDate = (DateTime)deadline;
                                item["WFDeadline"] = deadline;
                            }
                            catch 
                            { General.saveErrorsLog(wfid, "CreateWorkflowItem - Deadline: " + deadline); }
                        }

                        item["ConfidentialWorkflow"] = confidential;
                        SPListItemCollection stepCollection = GetInitialStepObjects(wfType, Web);
                        item["InitialSteps"] = GetInitialStepsGroups(stepCollection);
                        item["InitialElectronicStamps"] = GetInitialElectronicStamps(stepCollection);
                        item["InitialStepDescriptions"] = GetInitialStepDescription(stepCollection);
                        item["OtherInitialData"] = GetInitialEmailReceiverGroups(stepCollection, Web);
                        initialGFs = GetInitialFieldNames(wfType, Web, wfTypeCode, wfid);
                        item["InitialGeneralFields"] = initialGFs;
                        item["InitialConfidential"] = (wfTypeConfiguration["ConfidentialWorkflow"] == null || wfTypeConfiguration["ConfidentialWorkflow"].ToString() == "" || wfTypeConfiguration["ConfidentialWorkflow"].ToString().StartsWith("--")) ? String.Empty : wfTypeConfiguration["ConfidentialWorkflow"].ToString();
                        item["InitialStepNotifications"] = GetInitialStepNotifications(stepCollection, wfid, initialGFs, Web);//ESMA - CR26
                        item["InitialStepBackupGroups"] = GetInitialStepsBackupGroups(stepCollection); //ESMA-CR31

                        item["Author"] = realEditor;
                        item["Editor"] = realEditor;

                        using (new DisabledItemEventsScope())
                        {
                            item.Update();
                        }

                        //Create workflow sub folders
                        CreateSubFolders(list, item, status, id2, wfid);

                        if (wfTypeConfiguration["WFOrder"] != null)
                            AddPreloadedWFDocuments(item, wfTypeConfiguration, realEditor, wfid, Web);

                        Permissions.SetUpWorkflowPermissions(ref item, item, responsibleUser, realEditor, parameters, confidential, wfid, null, status, reassignToBackupActor, currentStep, isSaving);

                        //list.Update();

                        return item;
                    }
                    else return null;
                }
                else return null;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "CreateWorkflowItem " + ex.Message);
                return null;
            }
        }

        public static void SetWorkflowItemFields(SPListItem item, string status, int stepNumber, Hashtable usersToAssign, SPUser stepResponsible, string confidential, Dictionary<string, string> parameters, SPUser realEditor, string hddLinkToWorkFlow, string wfid, SPWeb web, int currentStep, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, string wftypeOrder, SPList logList, bool isSaving)
        {
            try
            {
                //Set most important workflow fields
                item["StepNumber"] = stepNumber;
                item["WFStatus"] = status;
                item["ConfidentialWorkflow"] = confidential;
                //CR24
                item["LinkToWorkflow"] = WorkflowDataManagement.GetWorkflowLinktoWorkFlowValueToReset(hddLinkToWorkFlow);

                if (!status.ToLower().Equals(parameters["Status Closed"].ToLower()))
                {
                    //ESMA-CR31-BackupGroup
                    if (reassignToBackupActor.Equals(true) && (!stepResponsible.ID.Equals(realEditor.ID)))
                    {
                        if (status.ToLower().Equals(parameters["Status On Hold"].ToLower()))
                            item["AssignedPerson"] = realEditor;
                        else
                            item["AssignedPerson"] = stepResponsible;
                    }
                    else
                        item["AssignedPerson"] = stepResponsible;
                }
                else
                    item["AssignedPerson"] = null;

                foreach (DictionaryEntry de in usersToAssign)
                {
                    string columName = de.Key.ToString();

                    try
                    {
                        if (de.Value != null)
                            item[columName] = (SPUser)de.Value;
                        else
                            item[columName] = null;

                    }
                    catch
                    {
                        General.saveErrorsLog(wfid, "Error updating column '" + de.Key.ToString() + "' - User: " + de.Value);
                        continue;
                    }
                }

                //ESMA-CR31-BackupGroup (Reassign User)
                if (((reassignToBackupActor.Equals(true))) && usersToAssign.Count > 0)
                   SetStepAssignedToFields(wfid, usersToAssign, ref item, actorsBackupDictionary, reassignToBackupActor, status, currentStep, parameters, realEditor, stepNumber, wftypeOrder, web, logList, confidential);


                try
                {
                    item["Editor"] = realEditor;
                }
                catch
                {
                    General.saveErrorsLog(wfid, "Error updating column 'Editor' - User: " + realEditor.LoginName);
                }

 
                //WFActorsSignedRole -> Save Actor who signed + Role. Used in the Advanced Search and Reporting.
                SetWorkflowActorsSignedRole(ref item, realEditor, wfid, currentStep, web);

                using (new DisabledItemEventsScope())
                {
                    item.Update();
                }

                Permissions.SetUpWorkflowPermissions(ref item, item, stepResponsible, realEditor, parameters, confidential, wfid, actorsBackupDictionary, status, reassignToBackupActor, stepNumber, isSaving);
               
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetWorkflowItemFields " + ex.Message);
            }


        }

        //ESMA-CR31-Backup Group
        public static void SetStepAssignedToFields(string wfid, Hashtable usersToAssign, ref SPListItem item, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, string status, int currentStep, Dictionary<string, string> parameters, SPUser realEditor, int stepNumber, string wftypeOrder, SPWeb Web, SPList logList, string confidentialValue)
        {
            try
            {
               
                if (status.ToLower().Equals(parameters["Status In Progress"].ToLower()) || status.ToLower().Equals(parameters["Status Rejected"].ToLower()) || status.ToLower().Equals(parameters["Status On Hold"].ToLower()))
                {
                    if (usersToAssign.ContainsKey("Step_x0020_" + currentStep.ToString() + "_x0020_Assigned_x0020_To"))
                    {

                        Comments.SetReassigningToBackupComment(wfid, status, parameters, wftypeOrder, currentStep, confidentialValue, logList, Web, realEditor, item);
                        
                        // Replace the current actor for the backupUser
                        item["Step_x0020_" + currentStep.ToString() + "_x0020_Assigned_x0020_To"] = realEditor;

                    }

                }
                 
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetStepAssignedToFields() " + ex.Message);
            }
        }
		
		// CR 24
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hddLinkToWorkFlow"></param>
        /// <returns></returns>
        private static string GetWorkflowLinktoWorkFlowValueToReset(string hddLinkToWorkFlow)
        {
            string[] data = hddLinkToWorkFlow.Split('|');
            List<string> listaFinal = new List<string>();

            foreach (string item in data)
            {
                string value = item.Split(':')[0];
                listaFinal.Add(value.Trim());
            }

            return string.Join("|", listaFinal.ToArray());

        }

        /// <summary>
        /// Create workflow documentation sub folders. This function takes longer depending on the number of documentation types.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="item"></param>
        /// <param name="status"></param>
        /// <param name="id"></param>
        public static void CreateSubFolders(SPList list, SPListItem item, string status, SPContentTypeId id, string wfid)
        {
            try
            {
                if (list.Fields.ContainsFieldWithStaticName("DocumentationType"))
                {
                    SPFieldChoice choices = new SPFieldChoice(list.Fields, list.Fields.GetFieldByInternalName("DocumentationType").InternalName);
                    foreach (string choice in choices.Choices)
                    {                        
                        try
                        {
                            if (choice != "(Empty)")
                            {
                                SPListItem subItem = list.Items.Add(item.Folder.ServerRelativeUrl, SPFileSystemObjectType.Folder, choice);
                                subItem["WFStatus"] = status;
                                subItem["ContentTypeId"] = id;
                                using (new DisabledItemEventsScope())
                                {
                                    subItem.Update();
                                }
                            }
                        }
                        catch { continue; }
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "CreateSubFolders " + ex.Message);
            }
        }

        public static void AddPreloadedWFDocuments(SPListItem wfItem, SPListItem wfTypeItem, SPUser author, string wfid, SPWeb web)
        {
            try
            {
                string docType = wfTypeItem["DocumentationType"].ToString();

                if (wfTypeItem.Attachments.Count > 0 && !string.IsNullOrEmpty(docType) && wfTypeItem["DocumentationType"].ToString() != "(Empty)")
                {
                    string folderURL = wfItem.ParentList.DefaultViewUrl.ToLower();
                    int urlIndex = folderURL.ToLower().IndexOf("/forms/");
                    string subfolderURL = folderURL.Substring(0, urlIndex) + "/" + wfid + "/" + docType + "/";
                    SPFolder folder = web.GetFolder(subfolderURL);

                    //CR37 - Move docs between tans -> Documentation Type values updated
                    if (!folder.Exists)
                    {
                        if (docType.Equals("ABAC"))
                            docType = "To be signed in ABAC";
                        else if (docType.Equals("Paper signed docs"))
                            docType = "Signed";

                        subfolderURL = folderURL.Substring(0, urlIndex) + "/" + wfid + "/" + docType + "/";
                        folder = web.GetFolder(subfolderURL);
                    }


                    if (folder.Exists)
                    {

                        SPFileCollection flColl = null;

                        foreach (string attachFileName in wfTypeItem.Attachments)
                        {
                            flColl = folder.Files;
                            SPFile FileCopy = wfTypeItem.ParentList.ParentWeb.GetFile(wfTypeItem.Attachments.UrlPrefix + attachFileName);

                            string destFile = flColl.Folder.Url + "/" + FileCopy.Name;
                            byte[] fileData = FileCopy.OpenBinary();

                            SPFile flAdded = flColl.Add(destFile, fileData);
                            
                            using (new DisabledItemEventsScope())
                            {
                                flAdded.Item.SystemUpdate(false);
                            }

                            //Update metadata preloaded document
                            UpdatePreloadedDocumentsMetadata(flAdded.Item, wfid, docType, web, author);
                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "AddPreloadedWFDocuments() " + ex.Message);
            }
        }

        public static void UpdatePreloadedDocumentsMetadata(SPListItem item, string wfid, string destFolderName, SPWeb web, SPUser author)
        {
            try
            {
                item["ContentTypeId"] = "0x010000bbe2cb30b8ae48f8a39bd6d1f94b8df0";
                item["StepNumber"] = "1";
                item["WFID"] = wfid;
                item["DocumentationType"] = destFolderName;
                item["WFDocumentPreview"] = web.Url + "/_layouts/15/ESMA.Paperless.Design.v16/images/RSPreview.png";

                item.ParentList.Fields[SPBuiltInFieldId.Author].ReadOnlyField = false;
                item.ParentList.Fields[SPBuiltInFieldId.Editor].ReadOnlyField = false;

                item[SPBuiltInFieldId.Author] = author;
                item[SPBuiltInFieldId.Editor] = author;

                item.ParentList.Fields[SPBuiltInFieldId.Author].ReadOnlyField = true;
                item.ParentList.Fields[SPBuiltInFieldId.Editor].ReadOnlyField = true;

                using (new DisabledItemEventsScope())
                {
                    item.UpdateOverwriteVersion();
                }
                
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "UpdatePreloadedDocumentsMetadata " + ex.Message);
            }
        }

        public static SPListItemCollection GetDocumentsFromSpecifFolder(SPFolder folder, SPList documentLibrary, string wfid)
        {
            SPListItemCollection collListItems = null;

            try
            {
                SPQuery query = new SPQuery();
                query.Folder = folder;
                query.ViewAttributes = "Scope=\"Recursive\"";
                query.Query = "<OrderBy><FieldRef Name='FileLeafRef' Ascending='True' /></OrderBy>";
                collListItems = documentLibrary.GetItems(query);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetDocumentsFromSpecifFolder " + ex.Message);
            }

            return collListItems;
        }

        public static void GetDocumentURLArray(ref string strUrls, SPListItemCollection collListItems, string webURL, string wfid)
        {
            try
            {

                string data = string.Empty;

                for (int i = 0; i < collListItems.Count; i++)
                {
                    string url = collListItems[i].File.Url;

                    if (!url.StartsWith(webURL))
                        url = webURL + "/" + url;

                    string name = collListItems[i].File.Name;
                    string part = url + "|" + name;

                    if (i.Equals(0))
                        data = part.Replace("\"", "\\\"");
                    else
                        data += "," + part.Replace("\"", "\\\"");
                }

                if (string.IsNullOrEmpty(strUrls) && string.IsNullOrEmpty(data))
                    strUrls += ";";
                else if (!string.IsNullOrEmpty(data))
                    strUrls += data + ";";
                else
                    strUrls += ";";

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetDocumentURLArray() " + ex.Message);
            }
        }

        #endregion

        #region WorkflowLog
        /// <summary>
        /// Get workflow log list by workflow type name.
        /// </summary>
        /// <param name="wfType"></param>
        /// <param name="Web"></param>
        /// <returns>SharePoint list which stores the actions taken for a specific workflow type</returns>
        public static SPList GetWorkflowLogList(string wfType, SPWeb Web)
        {
            SPList list = null;
            try
            {
                list = Web.Lists["RS Workflow Configuration"];

                if (list != null)
                {
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + wfType + "</Value></Eq></Where>";
                    query.ViewFields = string.Concat(
                                   "<FieldRef Name='Title' />",
                                   "<FieldRef Name='WFLogURL' />");
                    query.ViewFieldsOnly = true; // Fetch only the data that we need


                    SPListItemCollection itemCollection = list.GetItems(query);

                    if (itemCollection != null && itemCollection.Count.Equals(1))
                    {
                        SPListItem item = itemCollection[0];

                        if (item.Fields.ContainsFieldWithStaticName("WFLogURL") && item["WFLogURL"] != null)
                            list = Web.GetListFromUrl(item["WFLogURL"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetWorkflowLogList " + ex.Message);
            }
            return list;
        }

        /// <summary>
        /// Create a workflow log record
        /// </summary>
        /// <param name="wfType"></param>
        /// <param name="wfTypeCode"></param>
        /// <param name="WFID"></param>
        /// <param name="stepNumber"></param>
        /// <param name="status"></param>
        /// <param name="stepResponsible"></param>
        /// <param name="actionTaken"></param>
        /// <param name="actionDetails"></param>
        /// <param name="computerName"></param>
        /// <param name="workflowComment"></param>
        /// <param name="isOldComment"></param>
        /// <param name="confidential"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <param name="realEditor"></param>
        public static void CreateWorkflowLog(string wfTypeCode, string wfid, int stepNumber, string status, SPUser stepResponsible, string actionTaken, string actionDetails, string computerName, string workflowComment, string confidential, SPList list, SPWeb Web, Dictionary<string, string> parameters, SPUser realEditor, bool oldComment)
        {
            try
            {
                if (list != null)
                {

                    SPListItem item = list.Items.Add();

                    item["WFID"] = wfid;
                    if (parameters.ContainsKey("Interface Page"))
                    {
                        SPFieldUrlValue urlValue = new SPFieldUrlValue();
                        urlValue.Description = wfid;
                        urlValue.Url = Web.Url + parameters["Interface Page"] + "?wfid=" + wfid + "&wftype=" + wfTypeCode;
                        item["WFLink"] = urlValue;
                    }
                    item["StepNumber"] = stepNumber;
                    item["WFStatus"] = status;

                    if (!status.ToLower().Equals(parameters["Status Closed"].ToLower()))
                        item["AssignedPerson"] = stepResponsible;
                    else
                        item["AssignedPerson"] = null;

                    item["ActionTaken"] = actionTaken;
                    item["ActionDetails"] = actionDetails;
                    item["ComputerName"] = computerName;
                    item["WorkflowComment"] = workflowComment;

                    if(oldComment)
                        item["OldComment"] = 1;
                    else
                        item["OldComment"] = 0;

                    item["ConfidentialWorkflow"] = confidential;

                    item["Author"] = realEditor;
                    item["Editor"] = realEditor;


                    using (new DisabledItemEventsScope())
                    {
                        item.Update();
                    }
                    
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "CreateWorkflowLog " + ex.Message);
            }
        }


        //CR20

        public static bool CheckIfUserRemoveDocument(SPWeb web, string wfid, SPList logsList, string currentStep, SPUser editor, string actionTaken)
        {
            bool documentRemoved = false;

            //Removed or tried to delete a document
            try
            {
                SPQuery query = new SPQuery();
                query.Query = "<Where>"
                    + "<And><Eq><FieldRef Name='WFID' /><Value Type='Number'>" + wfid + "</Value></Eq>"
                    + "<And><Eq><FieldRef Name='ActionTaken' /><Value Type='Choice'>" + actionTaken + "</Value></Eq>"
                    + "<And><Neq><FieldRef Name='WFStatus' /><Value Type='Choice'>Draft</Value></Neq>"
                    + "<And><IsNull><FieldRef Name='WorkflowComment' /></IsNull>"
                    + "<And><Eq><FieldRef Name='StepNumber' /><Value Type='Number'>" + currentStep + "</Value></Eq>"
                    + "<Eq><FieldRef Name='Editor' Nullable='TRUE' LookupId='True' /><Value Type='Integer'>" + editor.ID + "</Value></Eq>"
                    + "</And></And></And></And></And></Where><OrderBy><FieldRef Name='ID' Ascending='True' /></OrderBy>";


                SPListItemCollection itemColl = logsList.GetItems(query);

                if (itemColl.Count > 0)
                    documentRemoved = true;

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "CheckIfUserRemoveDocument() " + ex.Message);
            }

            return documentRemoved;
        }

        public static void SetCommentDeleteFile(SPWeb web, string wfid, SPList logsList, string currentStep, SPUser loggedUser, string commentsDeletedFile, string actionTaken)
        {


            try
            {

                SPQuery query = new SPQuery();
                query.Query = "<Where>"
                    + "<And><Eq><FieldRef Name='WFID' /><Value Type='Number'>" + wfid + "</Value></Eq>"
                    + "<And><Eq><FieldRef Name='ActionTaken' /><Value Type='Choice'>" + actionTaken + "</Value></Eq>"
                    + "<And><Neq><FieldRef Name='WFStatus' /><Value Type='Choice'>Draft</Value></Neq>"
                    + "<And><IsNull><FieldRef Name='WorkflowComment' /></IsNull>"
                    + "<And><Eq><FieldRef Name='StepNumber' /><Value Type='Number'>" + currentStep + "</Value></Eq>"
                    + "<Eq><FieldRef Name='Editor' Nullable='TRUE' LookupId='True' /><Value Type='Integer'>" + loggedUser.ID + "</Value></Eq>"
                    + "</And></And></And></And></And></Where><OrderBy><FieldRef Name='ID' Ascending='True' /></OrderBy>";


                SPListItemCollection itemColl = logsList.GetItems(query);
          
                if (itemColl.Count > 0)
                {
                    SPListItem item = itemColl[0];

                    item["WorkflowComment"] = commentsDeletedFile;
                    item["Author"] = loggedUser;
                    item["Editor"] = loggedUser;

                    using (new DisabledItemEventsScope())
                    {
                        item.Update();
                    }
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetCommentDeleteFile() " + ex.Message);
            }
        }


        //CR23
        /// <summary>
        /// Create a workflow log record
        /// </summary>
        /// <param name="wfType"></param>
        /// <param name="wfTypeCode"></param>
        /// <param name="WFID"></param>
        /// <param name="stepNumber"></param>
        /// <param name="status"></param>
        /// <param name="stepResponsible"></param>
        /// <param name="actionTaken"></param>
        /// <param name="actionDetails"></param>
        /// <param name="computerName"></param>
        /// <param name="workflowComment"></param>
        /// <param name="isOldComment"></param>
        /// <param name="confidential"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <param name="realEditor"></param>
        /// WorkflowDataManagement.SetCommentClosed(wfid, loggedUser, ddlConfidential.SelectedValue, "Closed", computerName, WorkflowDataManagement.ActionsEnum.CommentedClosed.ToString(), Comments.GetMyCommentClosed(TextBoxNewCommentsClosed));

        public static void SetCommentClosed(SPWeb Web, SPList listLog, string wfid, string wfTypeCode, SPUser loggedUser, string confidential, string status, string computerName, string actionTaken, int stepNumber, string CommentsClosed, Dictionary<string, string> parameters)
        {
            try
            {
                if (listLog != null)
                {

                    SPListItem item = listLog.Items.Add();

                    item["WFID"] = wfid;
                    if (parameters.ContainsKey("Interface Page"))
                    {
                        SPFieldUrlValue urlValue = new SPFieldUrlValue();
                        urlValue.Description = wfid;
                        urlValue.Url = Web.Url + parameters["Interface Page"] + "?wfid=" + wfid + "&wftype=" + wfTypeCode;
                        item["WFLink"] = urlValue;
                    }
                    item["StepNumber"] = stepNumber;
                    item["WFStatus"] = status;
                    
                    item["ActionTaken"] = actionTaken;
                  
                    item["ComputerName"] = computerName;
                    item["WorkflowComment"] = CommentsClosed;

                    item["ConfidentialWorkflow"] = confidential;

                    item["Author"] = loggedUser;
                    item["Editor"] = loggedUser;

 
                    item.Update();
               
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "CreateWorkflowLog " + ex.Message);
            }
        }
        
        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="log"></param>
        /// <param name="isOld"></param>
        public static void SetComment(SPListItem log, bool isOld, string action, string comment, string wfid, SPUser loggedUser)
        {
            try
            {
                if (log.Fields.ContainsFieldWithStaticName("OldComment"))
                {
                    if (isOld)
                        log["OldComment"] = 1;
                    else
                        log["OldComment"] = 0;

                    log["ActionDetails"] = action;
                    log["WorkflowComment"] = comment;
                    log["Editor"] = loggedUser;

                }

                log.Update();
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetComment " + ex.Message);
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="wfid"></param>
        /// <param name="stepNumber"></param>
        /// <param name="logList"></param>
        /// <param name="loggedUser"></param>
        /// <returns></returns>
        public static string GetPreviousComment(SPWeb Web, string wfid, int stepNumber, SPList logList, SPUser loggedUser)
        {
            string comment = string.Empty;

            try
            {
                
                string actionTakenValue = GetActionDescription(ActionsEnum.Commented.ToString());

  
                        SPQuery query = new SPQuery();
                        query.Query = "<Where>"
                            + "<And><Eq><FieldRef Name='WFID'/><Value Type='Text'>" + wfid + "</Value></Eq>"
                            + "<And><Eq><FieldRef Name='ActionTaken'/><Value Type='Choice'>" + actionTakenValue + "</Value></Eq>"
                            + "<And><Eq><FieldRef Name='StepNumber' /><Value Type='Number'>" + stepNumber.ToString() + "</Value></Eq>"
                            + "<And><Eq><FieldRef Name='OldComment' /><Value Type='Integer'>0</Value></Eq>"
                            + "<Eq><FieldRef Name='Editor' Nullable='TRUE' LookupId='True' /><Value Type='Integer'>" + loggedUser.ID + "</Value></Eq>"
                            + "</And></And></And></And></Where>"
                            + "<OrderBy><FieldRef Name='ID' Ascending='False' /></OrderBy>";
                        query.ViewFields = string.Concat(
                                   "<FieldRef Name='WFID' />",
                                   "<FieldRef Name='ActionTaken' />",
                                   "<FieldRef Name='StepNumber' />",
                                   "<FieldRef Name='ID' />",
                                   "<FieldRef Name='WorkflowComment' />",
                                   "<FieldRef Name='OldComment' />");
                        query.ViewFieldsOnly = true; // Fetch only the data that we need

                        SPListItemCollection logRecordCollection = logList.GetItems(query);

                        if (!logRecordCollection.Count.Equals(0))
                        {
                            SPListItem logRecord = logRecordCollection[0];
                            comment = logRecord["WorkflowComment"] != null ? logRecord["WorkflowComment"].ToString() : string.Empty;
                        }
                 
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetPreviousComment() - " + ex.Message);
            }

            return comment;
        }
        //CR23
        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="wfid"></param>
        /// <param name="stepNumber"></param>
        /// <param name="logList"></param>
        /// <param name="loggedUser"></param>
        /// <returns></returns>
        public static string GetPreviousCommentClosed(SPWeb Web, string wfid, int stepNumber, SPList logList, SPUser loggedUser)
        {
            string comment = string.Empty;

            try
            {
                
                //string actionTakenValue = GetActionDescription(ActionsEnum.CommentedClosed.ToString());
                string actionTakenValue = ActionsEnum.CommentedClosed.ToString();


                        SPQuery query = new SPQuery();
                        query.Query = "<Where><And>"
                                     + "<Eq><FieldRef Name='WFID'/><Value Type='Text'>" + wfid + "</Value></Eq>"
                                     + "<Eq><FieldRef Name='ActionTaken'/><Value Type='Choice'>" + actionTakenValue + "</Value></Eq>"
                                     + "</And> </Where>"
                                     + "<OrderBy><FieldRef Name='Created' Ascending='False' /></OrderBy>";
                                            
                        SPListItemCollection logRecordCollection = logList.GetItems(query);

 
                            foreach (SPListItem item in logRecordCollection)
                            {

                                SPUser author = General.GetAuthor(wfid, item, Web);
                                comment = comment + DateTime.Parse(item["Created"].ToString()).ToShortDateString() + " " + DateTime.Parse(item["Created"].ToString()).ToLongTimeString ()  +
                                        " - <b>" + author.Name + " </b>- " + item["WorkflowComment"].ToString() + "<br />";
                            }
               
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetPreviousComment() - " + ex.Message);
            }

             return comment;
        }
        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="MyWeb"></param>
        /// <param name="wfid"></param>
        /// <param name="stepNumber"></param>
        /// <param name="logList"></param>
        /// <param name="loggedUser"></param>
        /// <returns></returns>
        public static SPListItem GetPreviousCommentObject(SPWeb MyWeb, string wfid, int stepNumber, SPList logList, SPUser loggedUser)
        {
            SPListItem commentItem = null;

            try
            {

                string actionTakenValue = GetActionDescription(ActionsEnum.Commented.ToString());


                SPQuery query = new SPQuery();
                query.Query = "<Where>"
                    + "<And><Eq><FieldRef Name='WFID'/><Value Type='Text'>" + wfid + "</Value></Eq>"
                    + "<And><Eq><FieldRef Name='ActionTaken'/><Value Type='Choice'>" + actionTakenValue + "</Value></Eq>"
                    + "<And><Eq><FieldRef Name='StepNumber' /><Value Type='Number'>" + stepNumber.ToString() + "</Value></Eq>"
                    + "<And><Eq><FieldRef Name='OldComment' /><Value Type='Integer'>0</Value></Eq>"
                    + "<Eq><FieldRef Name='Editor' Nullable='TRUE' LookupId='True' /><Value Type='Integer'>" + loggedUser.ID + "</Value></Eq>"
                    + "</And></And></And></And></Where>"
                    + "<OrderBy><FieldRef Name='ID' Ascending='False' /></OrderBy>";
                SPListItemCollection logRecordCollection = logList.GetItems(query);

                if (logRecordCollection.Count > 0)
                    commentItem = logRecordCollection[0];
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetPreviousCommentObject() - " + ex.Message);
            }

            return commentItem;
        }

        /// <summary>
        /// Log workflow activity during signing and control if workflow is being launched or signed.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="WFID"></param>
        /// <param name="wftypeName"></param>
        /// <param name="wftypeOrder"></param>
        /// <param name="prevStep"></param>
        /// <param name="Web"></param>
        /// <param name="responsible"></param>
        /// <param name="realEditor"></param>
        /// <param name="computerName"></param>
        /// <param name="confidential"></param>
        /// <param name="comment"></param>
        /// <param name="parameters"></param>
        /// <param name="nextStep"></param>
        public static void LogWorkflowActivityOnSigning(SPListItem item, string wfid, string wftypeName, string wftypeOrder, int prevStep, SPWeb Web, SPUser responsible, SPUser realEditor, string computerName, string confidential, string comment, Dictionary<string, string> parameters, int nextStep)
        {
            try
            {
                SPList logList = GetWorkflowLogList(wftypeName, Web);
                string electronicStamp = GetElectronicStamp(item, prevStep, parameters, wfid);
                string status = string.Empty;
                string action = string.Empty;
                bool isDraftWF = LogWorkflowIsDraft(wfid, Web, wftypeName);

                if (((Convert.ToInt32(nextStep) - 1) != 1) && (!isDraftWF))
                {
                    action = GetActionDescription(ActionsEnum.Signed.ToString());
                    status = parameters["Status In Progress"];
                }
                else if (isDraftWF)
                {
                    action = GetActionDescription(ActionsEnum.Launched.ToString());
                    status = parameters["Status Draft"];
                }
                else
                {
                    action = GetActionDescription(ActionsEnum.Signed.ToString());
                    status = parameters["Status In Progress"];
                }

                if (action.ToUpper().Equals(ActionsEnum.Launched.ToString().ToUpper()))
                {
                    SPListItem log = GetPreviousCommentObject(Web, wfid, prevStep, logList, realEditor);

                    if (log != null)
                        SetComment(log, true, action, comment, wfid, responsible);
                    else
                        CreateWorkflowLog(wftypeOrder, wfid, prevStep, status, responsible, GetActionDescription(ActionsEnum.Commented.ToString()), action, computerName, comment, confidential, logList, Web, parameters, realEditor, true);
                }
                else
                    CreateWorkflowLog(wftypeOrder, wfid, prevStep, status, responsible, GetActionDescription(ActionsEnum.Commented.ToString()), action, computerName, comment, confidential, logList, Web, parameters, realEditor, true);

                CreateWorkflowLog(wftypeOrder, wfid, prevStep, status, responsible, action, string.Empty, computerName, string.Empty, confidential, logList, Web, parameters, realEditor, true);
                
                if (!string.IsNullOrEmpty(electronicStamp))
                    CreateWorkflowLog(wftypeOrder, wfid, prevStep, status, responsible, GetActionDescription(ActionsEnum.Commented.ToString()), GetActionDescription(ActionsEnum.Signed.ToString()), computerName, electronicStamp, confidential, logList, Web, parameters, realEditor, true);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "LogWorkflowActivityOnSigning " + ex.Message);
            }
        }

  
        /// <summary>
        /// Get if workflow is in draft status according to logs
        /// </summary>
        /// <param name="WFID"></param>
        /// <param name="Web"></param>
        /// <param name="wfType"></param>
        /// <returns></returns>
        public static bool LogWorkflowIsDraft(string wfid, SPWeb Web, string wfType)
        {
            bool isDraft = false;

            try
            {
                SPList list = GetWorkflowLogList(wfType, Web);

                SPQuery query = new SPQuery();
                query.Query = "<Where><And><Eq><FieldRef Name='WFID' /><Value Type='Number'>" + wfid + "</Value></Eq><And><Eq><FieldRef Name='StepNumber' /><Value Type='Number'>1</Value></Eq>" +
            "<Eq><FieldRef Name='ActionTaken' /><Value Type='Choice'>Launched</Value></Eq></And></And></Where>";
                query.ViewFields = string.Concat(
                                  "<FieldRef Name='WFID' />",
                                  "<FieldRef Name='StepNumber' />",
                                  "<FieldRef Name='ActionTaken' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need

                SPListItemCollection itemCollection = list.GetItems(query);

                if (itemCollection != null && itemCollection.Count.Equals(0))
                    isDraft = true;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "LogWorkflowIsDraft() - " + ex.Message);
            }

            return isDraft;
        }

       

        

        /// <summary>
        /// Log confidentiality value change during workflow action execution
        /// </summary>
        /// <param name="item"></param>
        /// <param name="docList"></param>
        /// <param name="logList"></param>
        /// <param name="FirstDateTime"></param>
        /// <param name="wfid"></param>
        /// <param name="wftypeName"></param>
        /// <param name="wftypeOrder"></param>
        /// <param name="status"></param>
        /// <param name="stepNumber"></param>
        /// <param name="computerName"></param>
        /// <param name="prevConfidential"></param>
        /// <param name="responsible"></param>
        /// <param name="realEditor"></param>
        /// <param name="parameters"></param>
        /// <param name="Web"></param>
        public static void LogConfidentialityChanges(SPListItem item, SPList logList, string wfid, string wftypeOrder, string status, int stepNumber, string computerName, string prevConfidential, string newConfidential, SPUser responsible, SPUser realEditor, Dictionary<string, string> parameters, SPWeb Web)
        {
            try
            {
                if(!newConfidential.ToUpper().Equals(prevConfidential.ToUpper()))
                    CreateWorkflowLog(wftypeOrder, wfid, stepNumber, status, responsible, GetActionDescription(ActionsEnum.ConfidentialityChanged.ToString()), " turned this WF " + newConfidential.ToLower(), computerName, string.Empty, newConfidential, logList, Web, parameters, realEditor, true);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "LogConfidentialityChanges " + ex.Message);
            }
        }


       

      

        #endregion

        #region WorkflowHistory

        /// <summary>
        /// Get workflow record on Workflow history list.
        /// </summary>
        /// <param name="WFID"></param>
        /// <param name="Web"></param>
        /// <returns>SharePoint list item object in workflow history list</returns>
        public static SPListItem GetWorkflowHistoryRecord(string wfid, SPWeb Web)
        {
            SPListItem item = null;
            try
            {
                SPList list = Web.Lists["RS Workflow History"];

                if (list != null && list.Fields.ContainsFieldWithStaticName("WFID"))
                {
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><Eq><FieldRef Name='WFID'/><Value Type='Text'>" + wfid + "</Value></Eq></Where><OrderBy><FieldRef Name='ID' Ascending='False' /></OrderBy>";

                    SPListItemCollection itemCollection = list.GetItems(query);

                    if (itemCollection != null && itemCollection.Count.Equals(1))
                        item = itemCollection[0];
                    else if (itemCollection != null && itemCollection.Count > 1)
                    {
                        item = itemCollection[0];
                        General.saveErrorsLog(wfid, "GetWorkflowHistoryRecord(). ERROR! There are more than one instance in the RS Workflow History.");
                    }
                    else
                        General.saveErrorsLog(wfid, "GetWorkflowHistoryRecord(). ERROR! There is not any reference in the RS Workflow History.");
                    
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetWorkflowHistoryRecord " + ex.Message);
            }
            return item;
        }

        /// <summary>
        /// Create workflow record in workflow history list.
        /// </summary>
        /// <param name="wfItem"></param>
        /// <param name="wfType"></param>
        /// <param name="wfTypeCode"></param>
        /// <param name="WFID"></param>
        /// <param name="subject"></param>
        /// <param name="amount"></param>
        /// <param name="status"></param>
        /// <param name="stepResponsible"></param>
        /// <param name="initiator"></param>
        /// <param name="urgent"></param>
        /// <param name="deadline"></param>
        /// <param name="confidential"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <param name="realEditor"></param>
        public static void CreateWorkflowHistoryRecord(SPListItem wfItem, string wfType, string wfTypeCode, string wfid, string subject, string amount, string status, SPUser stepResponsible, SPUser initiator, bool urgent, Object deadline, string confidential, SPWeb Web, Dictionary<string, string> parameters, SPUser realEditor, int StepNumber, Dictionary<string, string> actorsBackupDictionary, SPRoleDefinition roleDefinitionRSRead, SPRoleDefinition roleDefinitionRSContributor, SPRoleDefinition roleDefinitionRSFullControl, bool isSaving, bool isReassigning, bool reassignToBackupActor, int currentStep)
        {
            try
            {
                SPList list = Web.Lists["RS Workflow History"];

                if (list != null)
                {
                    SPListItem item = list.Items.Add();

                    item["WFID"] = wfid;
                    if (parameters.ContainsKey("Interface Page"))
                    {
                        SPFieldUrlValue urlValue = new SPFieldUrlValue();
                        urlValue.Description = wfid;
                        urlValue.Url = Web.Url + parameters["Interface Page"] + "?wfid=" + wfid + "&wftype=" + wfTypeCode;
                        item["WFLink"] = urlValue;
                    }

                    item["WFSubject"] = subject;
                    item["Amount"] = amount;
                    item["WFStatus"] = status;
                    item["WFType"] = wfType;

                    if (!status.ToLower().Equals(parameters["Status Closed"].ToLower()))
                        item["AssignedPerson"] = stepResponsible;
                    else
                        item["AssignedPerson"] = null;

                    //CR31 - Excluded actions as Save and On Hold
                    if (!status.ToLower().Equals(parameters["Status Draft"].ToLower()) && !status.ToLower().Equals(parameters["Status On Hold"].ToLower()) && !isSaving && !isReassigning)
                        SetAllActorsSignValue(wfid, ref item, realEditor);

                    //CR32
                    item["StepNumber"] = StepNumber;

                    if (initiator != null)
                        item["InitiatedBy"] = initiator;
                    
                    if (urgent)
                        item["Urgent"] = 1;
                    else
                        item["Urgent"] = 0;
                    
                    if (deadline != null)
                    {
                        try
                        {
                            DateTime deadlineDate = (DateTime)deadline;
                            if (!deadlineDate.Year.Equals(1))
                                item["WFDeadline"] = deadline;
                            else
                                item["WFDeadline"] = null;
                        }
                        catch { General.saveErrorsLog(wfid, "CreateWorkflowHistoryRecord - Save 'WFDeadline' value."); }
                    }

                    if(status.ToUpper().Equals(parameters["Status Rejected"].ToUpper()))
                    {
                        SPFieldUrlValue urlValue = new SPFieldUrlValue();
                        urlValue.Url = "/_layouts/15/ESMA.Paperless.Design.v16/images/RSRejected.PNG";
                        item["Rejection"] = urlValue;
                    }
                    else
                        item["Rejection"] = null;

                    item["ConfidentialWorkflow"] = confidential;

                    if (confidential.ToUpper().Equals("NON RESTRICTED"))
                        item["ConfidentialCheck"] = null;
                    else
                        item["ConfidentialCheck"] = "/_layouts/15/ESMA.Paperless.Design.v16/images/RSConfidential.gif";               

                    item["Author"] = realEditor;
                    item["Editor"] = realEditor;

                    using (new DisabledItemEventsScope())
                    {
                        item.Update();
                    }

                    if (confidential.ToUpper().Equals("RESTRICTED"))
                        Permissions.SetStepResponsiblePermissionsConfid(ref item, wfItem, stepResponsible, realEditor, parameters, false, actorsBackupDictionary, roleDefinitionRSRead, roleDefinitionRSContributor, roleDefinitionRSFullControl, status, reassignToBackupActor, currentStep, isSaving);
                    else
                        Permissions.ResetPermissions(ref item, wfid);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "CreateWorkflowHistoryRecord" + ex.Message);
            }
        }

        /// <summary>
        /// Create a workflow history record if it does not exist in workflow history list, otherwise, get it and update it.
        /// </summary>
        /// <param name="wfItem"></param>
        /// <param name="WFID"></param>
        /// <param name="wftypeName"></param>
        /// <param name="wftypeOrder"></param>
        /// <param name="stepResponsible"></param>
        /// <param name="initiator"></param>
        /// <param name="realEditor"></param>
        /// <param name="Web"></param>
        /// <param name="status"></param>
        /// <param name="urgent"></param>
        /// <param name="amount"></param>
        /// <param name="subject"></param>
        /// <param name="deadline"></param>
        /// <param name="confidential"></param>
        /// <param name="parameters"></param>
        public static void CreateAndSetWorkflowHistory(SPListItem wfItem, string wfid, string wftypeName, string wftypeOrder, SPUser stepResponsible, SPUser initiator, SPUser realEditor, SPWeb Web, string status, bool urgent, string amount, string subject, string deadline, string confidential, Dictionary<string, string> parameters, int StepNumber, Dictionary<string, string> actorsBackupDictionary, bool reassignToBackupActor, bool isSaving, bool isReassigning, int currentStep)
        {
            try
            {

                SPRoleDefinition roleDefinitionRSRead = SPContext.Current.Web.Site.RootWeb.RoleDefinitions["RS Read"];
                SPRoleDefinition roleDefinitionRSContributor = SPContext.Current.Web.Site.RootWeb.RoleDefinitions["RS Contribute"];
                SPRoleDefinition roleDefinitionRSFullControl = SPContext.Current.Web.Site.RootWeb.RoleDefinitions["RS Full Control"];

                DateTime deadlineAux = new DateTime();
                if(!string.IsNullOrEmpty(deadline))
                    DateTime.TryParse(deadline, out deadlineAux);

                SPListItem historyRecord = GetWorkflowHistoryRecord(wfid, Web);

                if (historyRecord == null)
                    CreateWorkflowHistoryRecord(wfItem, wftypeName, wftypeOrder, wfid, subject, amount, status, stepResponsible, initiator, urgent, deadlineAux, confidential, Web, parameters, realEditor, StepNumber, actorsBackupDictionary, roleDefinitionRSRead, roleDefinitionRSContributor, roleDefinitionRSFullControl, isSaving, isReassigning, reassignToBackupActor, currentStep);
                else if (historyRecord["InitiatedBy"] != null)
                {

                    SPUser initiatedByUser = General.GetSPUser(historyRecord, "InitiatedBy", wfid, Web);

                    if (initiatedByUser != null)
                        UpdateWorkflowHistoryRecord(wfItem, ref historyRecord, wftypeName, wftypeOrder, wfid, subject, amount, status, stepResponsible, initiatedByUser, urgent, deadlineAux, confidential, Web, parameters, realEditor, StepNumber, actorsBackupDictionary, roleDefinitionRSRead, roleDefinitionRSContributor, roleDefinitionRSFullControl, reassignToBackupActor, isSaving, isReassigning, currentStep);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "CreateAndSetWorkflowHistory " + ex.Message);
            }
        }

        /// <summary>
        /// Update existing workflow history record with the new values.
        /// </summary>
        /// <param name="wfItem"></param>
        /// <param name="item"></param>
        /// <param name="wfType"></param>
        /// <param name="wfTypeCode"></param>
        /// <param name="WFID"></param>
        /// <param name="subject"></param>
        /// <param name="amount"></param>
        /// <param name="status"></param>
        /// <param name="stepResponsible"></param>
        /// <param name="initiator"></param>
        /// <param name="urgent"></param>
        /// <param name="deadline"></param>
        /// <param name="confidential"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        /// <param name="realEditor"></param>
        public static void UpdateWorkflowHistoryRecord(SPListItem wfItem, ref SPListItem item, string wfType, string wfTypeCode, string wfid, string subject, string amount, string status, SPUser stepResponsible, SPUser initiator, bool urgent, DateTime deadline, string confidential, SPWeb Web, Dictionary<string, string> parameters, SPUser realEditor, int StepNumber, Dictionary<string, string> actorsBackupDictionary, SPRoleDefinition roleDefinitionRSRead, SPRoleDefinition roleDefinitionRSContributor, SPRoleDefinition roleDefinitionRSFullControl, bool reassignToBackupActor, bool isSaving, bool isReassigning, int currentStep)
        {
            try
            {
                item["WFID"] = wfid;
                if (parameters.ContainsKey("Interface Page"))
                {
                    SPFieldUrlValue urlValue = new SPFieldUrlValue();
                    urlValue.Description = wfid;
                    urlValue.Url = Web.Url + parameters["Interface Page"] + "?wfid=" + wfid + "&wftype=" + wfTypeCode;
                    item["WFLink"] = urlValue;
                }
                item["WFSubject"] = subject;
                item["Amount"] = amount;
                item["WFStatus"] = status;
                item["WFType"] = wfType;

                if (!status.ToLower().Equals(parameters["Status Closed"].ToLower()))
                {
                    //ESMA-CR31-Backup Groups
                    if (reassignToBackupActor.Equals(true) && (!stepResponsible.ID.Equals(realEditor.ID)))
                    {
                        if (status.ToLower().Equals(parameters["Status On Hold"].ToLower()))
                            item["AssignedPerson"] = realEditor;
                        else
                            item["AssignedPerson"] = stepResponsible;
                    }
                    else
                        item["AssignedPerson"] = stepResponsible;
                }
                else
                    item["AssignedPerson"] = null;


                //CR31 - Excluded actions as Save and On Hold
                if (!status.ToLower().Equals(parameters["Status Draft"].ToLower()) && !status.ToLower().Equals(parameters["Status On Hold"].ToLower()) && !isSaving && !isReassigning)
                    SetAllActorsSignValue(wfid, ref item, realEditor);

                //CR32
                item["StepNumber"] = StepNumber;
                if (status.ToLower() == parameters["Status Closed"].ToLower())
                {
                    TimeSpan ts = DateTime.Today - DateTime.Parse(item["Created"].ToString());
                    item["DaysToClose"] = ts.Days;
                }

                //ESMA-CR31-Backup Groups (BackupInitiator)
                if (initiator != null)
                    item["InitiatedBy"] = initiator;
               
                if (urgent)
                    item["Urgent"] = 1;
                else
                    item["Urgent"] = 0;

                if (deadline != null)
                {
                    try
                    {
                        DateTime deadlineDate = (DateTime)deadline;
                        if(!deadlineDate.Year.Equals(1))
                            item["WFDeadline"] = deadline;
                        else
                            item["WFDeadline"] = null;
                    }
                    catch { General.saveErrorsLog(wfid, "UpdateWorkflowHistoryRecord - Save 'WFDeadline' value." ); }
                }

                if (status.ToUpper().Equals(parameters["Status Rejected"].ToUpper()))
                {
                    SPFieldUrlValue urlValue = new SPFieldUrlValue();
                    urlValue.Url = "/_layouts/15/ESMA.Paperless.Design.v16/images/RSRejected.PNG";
                    item["Rejection"] = urlValue;
                }
                else
                    item["Rejection"] = null;

                item["ConfidentialWorkflow"] = confidential;

                if (confidential.ToUpper().Equals("NON RESTRICTED"))
                    item["ConfidentialCheck"] = null;
                else
                    item["ConfidentialCheck"] = "/_layouts/15/ESMA.Paperless.Design.v16/images/RSConfidential.gif";

                item["Editor"] = realEditor;

                using (new DisabledItemEventsScope())
                {
                    item.Update();
                }

                if (confidential.ToUpper().Equals("RESTRICTED"))
                    Permissions.SetStepResponsiblePermissionsConfid(ref item, wfItem, stepResponsible, realEditor, parameters, false, actorsBackupDictionary, roleDefinitionRSRead, roleDefinitionRSContributor, roleDefinitionRSFullControl, status, reassignToBackupActor, currentStep, isSaving);
                else
                    Permissions.ResetPermissions(ref item, wfid);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "UpdateWorkflowHistoryRecord " + ex.Message);
            }
        }

        public static void SetAllActorsSignValue(string wfid, ref SPListItem item, SPUser realEditor)
        {
            try
            {
                //CR31 -> (administrator#13/10/2015;testfia1#31/05/2016;nbarrutia#31/05/2016;)
                string nameUser = realEditor.LoginName.Split('\\')[1].Replace(@"\", "");
                DateTime dt = DateTime.Parse(DateTime.Now.ToShortDateString());
                string date = dt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

                if (item["AllActorsSign"] != null)
                {

                    if (!item["AllActorsSign"].ToString().Contains(nameUser))
                        item["AllActorsSign"] = item["AllActorsSign"].ToString() + nameUser + "#" + date + ";";
                    else
                    {
                        int position = item["AllActorsSign"].ToString().IndexOf(nameUser);
                        string oldUser = item["AllActorsSign"].ToString().Substring(position, nameUser.Length + (date.Length + 1));
                        item["AllActorsSign"] = item["AllActorsSign"].ToString().Replace(oldUser, nameUser + "#" + date);
                    }
                }
                else
                    item["AllActorsSign"] = nameUser + "#" + date + ";";
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetAllActorsSignValue() - RealEditor: '" + realEditor + "'. " + ex.Message);
            }
        }

        /// <summary>
        /// Update workflow initiator in workflow history list.
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="stepResponsibleUpdated"></param>
        /// <param name="editorUser"></param>
        /// <param name="modifiedDate"></param>
        public static void SetAssignedPersonWorkflowHistory(SPWeb Web, SPUser stepResponsibleUpdated, SPUser editorUser, DateTime modifiedDate, string wfid, bool reassignToBackupActor, string status, Dictionary<string, string> parameters, string confidentialValue, Dictionary<string, string> actorsBackupDictionary, int currentStep, bool isSaving)
        {
            try
            {
                SPListItem historyRecord = GetWorkflowHistoryRecord(wfid, Web);

                if (historyRecord != null)
                {
                    if (historyRecord["AssignedPerson"] != null)
                    {

                        if (!status.ToLower().Equals(parameters["Status Closed"].ToLower()))
                        {
                            //ESMA-CR31-Backup Groups
                            if (reassignToBackupActor.Equals(true) && (!stepResponsibleUpdated.ID.Equals(editorUser.ID)))
                            {
                                if (status.ToLower().Equals(parameters["Status On Hold"].ToLower()))
                                    historyRecord["AssignedPerson"] = editorUser;
                                else
                                    historyRecord["AssignedPerson"] = stepResponsibleUpdated;
                            }
                            else
                                historyRecord["AssignedPerson"] = stepResponsibleUpdated;
                        }
                        else
                            historyRecord["AssignedPerson"] = null;


                            historyRecord["Editor"] = editorUser;
                            historyRecord["Modified"] = modifiedDate;

                            using (new DisabledItemEventsScope())
                            {
                                historyRecord.Update();
                            }
                        
                    }
                }

                Permissions.SetUpWorkflowPermissions(ref historyRecord, historyRecord, stepResponsibleUpdated, editorUser, parameters, confidentialValue, wfid, actorsBackupDictionary, status, reassignToBackupActor, currentStep, isSaving);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetAssignedPersonWorkflowHistory() " + ex.Message);
            }
        }

        /// <summary>
        /// Set current workflow step responsible in workflow history list.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="stepResponsible"></param>
        /// <param name="realEditor"></param>
        /// <param name="Web"></param>
        public static void SetWorkflowHistoryAssignedPerson(ref SPListItem item, SPUser stepResponsible, SPUser realEditor, SPWeb Web, string wfid, bool reassignToBackupActor, string status, Dictionary<string, string> parameters)
        {
            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsFieldWithStaticName("AssignedPerson"))
                    {

                        if (!status.ToLower().Equals(parameters["Status Closed"].ToLower()))
                        {
                            //ESMA-CR31-BackupGroup
                            if (reassignToBackupActor.Equals(true) && (!stepResponsible.ID.Equals(realEditor.ID)))
                            {
                                if (status.ToLower().Equals(parameters["Status On Hold"].ToLower()))
                                    item["AssignedPerson"] = realEditor;
                                else
                                    item["AssignedPerson"] = stepResponsible;
                            }
                            else
                                item["AssignedPerson"] = stepResponsible;
                        }
                        else
                            item["AssignedPerson"] = null;
                        
                        item["Editor"] = realEditor;

                        using (new DisabledItemEventsScope())
                        {
                            item.Update();
                        }

                        item.ParentList.Update();
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "SetWorkflowHistoryAssignedPerson " + ex.Message);
            }
        }

        /// <summary>
        /// Set workflow history record current status.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="status"></param>
        /// <param name="realEditor"></param>
        /// <param name="Web"></param>
        public static void SetWorkflowHistoryStatus(ref SPListItem item, string status, SPUser realEditor, SPWeb Web)
        {
            try
            {
                if (item != null)
                {
                    if (item.Fields.ContainsFieldWithStaticName("WFStatus"))
                    {
                        item["WFStatus"] = status;
                        item["Editor"] = realEditor;

                        using (new DisabledItemEventsScope())
                        {
                            item.Update();
                        }

                        item.ParentList.Update();
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "SetWorkflowHistoryStatus " + ex.Message);
            }
        }

        /// <summary>
        /// Remove historial workflow references
        /// </summary>
        /// <param name="item"></param>
        /// <param name="list"></param>
        /// <param name="Web"></param>
        /// <param name="parameters"></param>
        public static void RemoveWorkflowHistoryOnCreation(SPListItem item, SPList list, SPWeb Web, Dictionary<string, string> parameters, string wfid, string status )
        {
            try
            {
                if (item != null && parameters.ContainsKey("Status Draft") && status.ToUpper().Equals(parameters["Status Draft"].ToUpper()))
                {
                    SPListItem historyRecord = GetWorkflowHistoryRecord(wfid, Web);

                    if (historyRecord != null)
                    {
                        using (new DisabledItemEventsScope())
                        {
                            historyRecord.Delete();
                        }
                    }
                 
                    
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "RemoveWorkflowHistoryOnCreation " + ex.Message);
            }
        }

        #endregion

        #region ActionsEnum

        public  enum ActionsEnum
        {
            [Description("Action re-assigned")]
            ActorReAssigned,
            [Description("Cancelled")]
            Cancelled,
            [Description("Commented")]
            Commented,
            [Description("Commented Closed")]
            CommentedClosed,
            [Description("Deleted")]
            Deleted,
            [Description("Field changed")]
            FieldChanged,
            [Description("Finished")]
            Finished,
            [Description("Launched")]
            Launched,
            [Description("Rejected")]
            Rejected,
            [Description("Saved")]
            Saved,
            [Description("Put On hold")]
            OnHold,
            [Description("Signed")]
            Signed,
            [Description("New document version")]
            NewDocumentVersion,
            [Description("Document uploaded")]
            NewDocument,
            [Description("Document removed")]
            DocumentRemoved,
            [Description("Restriction changed")]
            ConfidentialityChanged,
            [Description("Try remove document")]
            TryRemoveDocument,
            [Description("Backup Put On hold")]
            BackupOnHold,
            [Description("Backup Signed")]
            BackupSigned,
            [Description("Backup Rejected")]
            BackupRejected
        }

        /// <summary>
        /// Get action full title.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetActionDescription(string value)
        {
            Type type = typeof(ActionsEnum);
            MemberInfo[] enumInfo = type.GetMember(value);
            object[] attributes = enumInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            string description = ((DescriptionAttribute)attributes[0]).Description;
            return description;
        }

        #endregion

        #region StepDefinition

        /// <summary>
        /// Get the general fields at workflow creation
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <returns>General fields at workflow creation. GeneralField1;#GeneralField2</returns>
        public static List<string> GetFieldNames(SPListItem item, SPWeb Web)
        {
            List<string> fieldNames = new List<string>();
            try
            {
                if (item["InitialGeneralFields"] != null)
                {
                    string[] fieldNamesAux = Regex.Split(item["InitialGeneralFields"].ToString(), ";#");
                    fieldNames = new List<string>(fieldNamesAux);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetFieldNames " + ex.Message);
            }

            return fieldNames;
        }

        /// <summary>
        /// Get workflow general fields that define workflow activity.
        /// </summary>
        /// <param name="wfName"></param>
        /// <param name="Web"></param>
        /// <param name="order"></param>
        /// <returns>General fields.</returns>
        public static string GetInitialFieldNames(string wfName, SPWeb Web, string order, string wfid)
        {
            string fields = string.Empty;
            string internalName = string.Empty;

            try
            {
                Dictionary<string, string> generalFieldsDictionary = new Dictionary<string, string>();
                GeneralFields.SearchComunFieldsColumnNames(Web, ref generalFieldsDictionary, wfid);
                GeneralFields.SearchEspecificFieldsColumnNames(Web, order, ref generalFieldsDictionary, wfid);

                if (generalFieldsDictionary != null)
                {
                    foreach (KeyValuePair<String, String> kvp in generalFieldsDictionary)
                    {
                        string columnName = kvp.Key;

                        try
                        {
                            try
                            {
                                internalName = GetFieldInRSGroup(Web, columnName).InternalName.ToString();
                                //internalName = Web.Fields[columnName].InternalName.ToString();
                            }
                            catch
                            {
                                internalName = GetFieldInRSGroup(Web.Site.RootWeb, columnName).InternalName.ToString();
                                //internalName = Web.Site.RootWeb.Fields[columnName].InternalName.ToString();
                            }
                        }
                        catch { General.saveErrorsLog(wfid, "GetInitialFieldNames() - '" + columnName + "' does not exist."); }

                        if (string.IsNullOrEmpty(fields))
                            fields += internalName;
                        else
                            fields += ";#" + internalName;
                    }

                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetInitialFieldNames " + ex.Message);
            }

            return fields;
        }

        /// <summary>
        /// Get workflow steps responsible groups at workflow creation.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="Web"></param>
        /// <returns>String list with the names of the groups and the related step number. StepNumber1;#GroupName1&#StepNumber2;#GroupName2.</returns>
        public static List<string> GetGroupNames(string initialSteps, SPWeb Web, string wfid)
        {
            List<string> groupNames = new List<string>();
            try
            {
                if (!string.IsNullOrEmpty(initialSteps))
                {
                    string[] steps = Regex.Split(initialSteps, "&#");

                    int count = 0;
                    foreach (string step in steps)
                    {
                        string[] stepRecord = Regex.Split(steps[count].ToString(), ";#");
                        groupNames.Add(stepRecord[2].Split('\\')[1]);
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetGroupNames " + ex.Message);
            }

            return groupNames;
        }

        /// <summary>
        /// Get workflow step objects in a list item collection for its metadata fast reading
        /// </summary>
        /// <param name="wfName"></param>
        /// <param name="Web"></param>
        /// <returns></returns>
        public static SPListItemCollection GetInitialStepObjects(string wfName,SPWeb Web)
        { 
            try
            {
                SPList list = Web.Lists["RS Workflow Step Definitions"];

                if (list != null)
                {
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><Eq><FieldRef Name='Title' /><Value Type='Text'>" + wfName + "</Value></Eq></Where><OrderBy><FieldRef Name='StepNumber' Ascending='TRUE'/></OrderBy>";

                    SPListItemCollection stepCollection = list.GetItems(query);
                    return stepCollection;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetInitialStepObjects " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Get workflow steps responsible groups to define workflow activity.
        /// </summary>
        /// <param name="StepCollection"></param>
        /// <returns>Workflow steps groups</returns>
        public static string GetInitialStepsGroups(SPListItemCollection stepCollection)
        {
            string groups = string.Empty;
            try
            {
                if (stepCollection != null)
                {
                    int count = 0;
                    foreach (SPListItem item in stepCollection)
                    {
                        if (item["WFGroup"] != null && item["StepNumber"] != null)
                        {
                            if (count.Equals(0))
                                groups += item["StepNumber"] + ";#" + item["WFGroup"].ToString();
                            else
                                groups += "&#" + item["StepNumber"] + ";#" + item["WFGroup"].ToString();
                        }

                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetInitialSetpsGroups " + ex.Message);
            }

            return groups;
        }

        /// <summary>
        /// Get workflow steps responsible groups to define workflow activity.
        /// </summary>
        /// <param name="StepCollection"></param>
        /// <returns>Workflow steps groups</returns>
        public static string GetInitialStepsBackupGroups(SPListItemCollection stepCollection)
        {
            string backupGroups = string.Empty;
           
            try
            {
                if (stepCollection != null)
                {
                    int count = 0;
                    foreach (SPListItem item in stepCollection)
                    {
                        if (item["StepBackupGroup"] != null && item["StepNumber"] != null)
                        {
                            if (count.Equals(0))
                                backupGroups += item["StepNumber"] + ";#" + item["StepBackupGroup"].ToString();
                            else
                                backupGroups += "&#" + item["StepNumber"] + ";#" + item["StepBackupGroup"].ToString();
                        }

                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetInitialStepsBackupGroups() " + ex.Message);
            }

            return backupGroups;
        }

        /// <summary>
        /// Get workflow steps description at workflow creation.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="stepNumber"></param>
        /// <param name="parameters"></param>
        /// <returns>Array of step descriptions</returns>
        public static List<string> GetStepDescription(SPListItem item, int stepNumber, Dictionary<string, string> parameters)
        {
            List<string> listDescription = new List<string>();

            try
            {
                if (item["InitialStepDescriptions"] != null)
                {
                    string[] descriptions = Regex.Split(item["InitialStepDescriptions"].ToString(), "%#");
                    string[] descriptionRecord = Regex.Split(descriptions[stepNumber - 1].ToString(), ";#");
                    if (descriptionRecord[0].Equals(stepNumber.ToString()))
                    {
                        string value1 = "\r\n";
                        string value2 = "\n";

                        if (descriptionRecord[1].Contains(value1))
                            listDescription = Regex.Split(descriptionRecord[1], value1).ToList();
                        else
                            listDescription = Regex.Split(descriptionRecord[1], value2).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetStepDescription() " + ex.Message);
            }

            return listDescription;
        }

        /// <summary>
        /// Get workflow step description to define workflow activity.
        /// </summary>
        /// <param name="StepCollection"></param>
        /// <returns>Step descriptions</returns>
        public static string GetInitialStepDescription(SPListItemCollection stepCollection)
        {
            string descriptions = string.Empty;

            try
            {
                if (stepCollection != null)
                {
                    int count = 0;
                    foreach (SPListItem item in stepCollection)
                    {
                        if (item["StepDescription"] != null && item["StepNumber"] != null)
                        {
                            if (count.Equals(0))
                                descriptions += item["StepNumber"] + ";#" + item["StepDescription"].ToString();
                            else
                                descriptions += "%#" + item["StepNumber"] + ";#" + item["StepDescription"].ToString();
                        }
                        else if (item["StepNumber"] != null)
                        {
                            if (count.Equals(0))
                                descriptions += item["StepNumber"] + ";#" + string.Empty;
                            else
                                descriptions += "%#" + item["StepNumber"] + ";#" + string.Empty;
                        }
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetInitialStepDescription " + ex.Message);
            }

            return descriptions;
        }


        /// <summary>
        /// Get workflow step description to define workflow activity. (ESMA - CR26)
        /// </summary>
        /// <param name="StepCollection"></param>
        /// <returns>Step descriptions</returns>
        public static string GetInitialStepNotifications(SPListItemCollection stepCollection, string wfid, string initialGFs, SPWeb web)
        {
            string descriptions = string.Empty;

            try
            {
                if (stepCollection != null)
                {
                    int count = 0;
                    Dictionary<string, string> internalGFsDictionary = GeneralFields.GetGeneralFieldsDictionaryItem(wfid, initialGFs, web);

                    foreach (SPListItem item in stepCollection)
                    {
                        string emailStepSubject = "(empty)";
                        string emailStepText = "(empty)";
                        string internalName = string.Empty;
                        string stepNumber = item["StepNumber"].ToString();

                        if (item["EmailStepSubject"] != null && item["EmailStepText"] != null && item["StepNumber"] != null)
                        {
                            emailStepSubject = item["EmailStepSubject"].ToString();
                            emailStepText = item["EmailStepText"].ToString();

                        }
                        
                        if (item["EmailReceiverUser"] != null)
                        {
                            string[] columnNameInf = item["EmailReceiverUser"].ToString().Split('#'); //55;#Name
                            string columnName = columnNameInf[1];
                            internalName = internalGFsDictionary.FirstOrDefault(x => x.Value == columnName).Key;
                        }


                        FormatStepNotifications(wfid, count, emailStepSubject, emailStepText, internalName, ref descriptions , stepNumber);


                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetInitialStepNotifications() " + ex.Message);
            }

            return descriptions;
        }

        public static void FormatStepNotifications(string wfid, int count, string emailStepSubject, string emailStepText, string internalName, ref string descriptions , string stepNumber)
        {
            try
            {
                if (!string.IsNullOrEmpty(internalName))
                {
                    if (count.Equals(0))
                        descriptions += stepNumber + ";#" + emailStepSubject + ";#" + emailStepText + ";#" + internalName;
                    else
                        descriptions += "%#" + stepNumber + ";#" + emailStepSubject + ";#" + emailStepText + ";#" + internalName;
                }
                else
                {
                    if (count.Equals(0))
                        descriptions += stepNumber + ";#" + emailStepSubject + ";#" + emailStepText;
                    else
                        descriptions += "%#" + stepNumber + ";#" + emailStepSubject + ";#" + emailStepText;
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "FormatStepNotifications() " + ex.Message);
            }
        }

        /// <summary>
        /// Get the electronic stamps at workflow creation
        /// </summary>
        /// <param name="StepCollection"></param>
        /// <returns>String with all electronic stamps</returns>
        public static string GetInitialElectronicStamps(SPListItemCollection stepCollection)
        {
            string electronicStamps = string.Empty;

            try
            {
                if (stepCollection != null)
                {
                    int count = 0;
                    foreach (SPListItem item in stepCollection)
                    {
                        if (item["ElectronicStamp"] != null && item["StepNumber"] != null)
                        {
                            if(count.Equals(0))
                                electronicStamps += item["StepNumber"] + ";#" + item["ElectronicStamp"].ToString();
                            else
                                electronicStamps += "&#" + item["StepNumber"] + ";#" + item["ElectronicStamp"].ToString();
                        }
                        else if (item["StepNumber"] != null)
                        {
                            if (count.Equals(0))
                                electronicStamps += item["StepNumber"] + ";#" + string.Empty;
                            else
                                electronicStamps += "&#" + item["StepNumber"] + ";#" + string.Empty;
                        }
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GeInitialtElectronicStamps " + ex.Message);
            }

            return electronicStamps;
        }

        /// <summary>
        /// Check if a specific step has electronic stamp attached.
        /// </summary>
        /// <param name="wfName"></param>
        /// <param name="Web"></param>
        /// <param name="stepNumber"></param>
        /// <param name="parameters"></param>
        /// <returns>True if the step has electronic stamp</returns>
        public static bool HasElectronicStamp(string wfName, SPWeb Web, string stepNumber, Dictionary<string, string> parameters)
        {
            bool electronicStamps = false;

            try
            {
                SPList list = Web.Lists["RS Workflow Step Definitions"];

                if (list != null)
                {
                    SPQuery query = new SPQuery();
                    query.Query = "<Where><And><Eq><FieldRef Name='Title' /><Value Type='Text'>" + wfName + "</Value></Eq>"
                        + "<Eq><FieldRef Name='StepNumber' /><Value Type='Number'>" + stepNumber + "</Value></Eq></And></Where>";

                    SPListItemCollection itemCollection = list.GetItems(query);
                    SPListItem item = null;

                    if (itemCollection.Count > 0)
                    {
                        item = itemCollection[0];

                        if ((item["ElectronicStamp"] != null) && (parameters.ContainsKey("Null Electronic Stamp")))
                        {
                            if (item["ElectronicStamp"].ToString() != parameters["Null Electronic Stamp"])
                                electronicStamps = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "HasElectronicStamp() " + ex.Message);
            }

            return electronicStamps;
        }

        /// <summary>
        /// Get electronic stamp by step number.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="stepNumber"></param>
        /// <param name="parameters"></param>
        /// <returns>Electronic stamp description</returns>
        public static string GetElectronicStamp(SPListItem item, int stepNumber, Dictionary<string,string> parameters, string wfid)
        {
            string electronicStamp = string.Empty;

            try
            {
                if (item["InitialElectronicStamps"] != null)
                {
                    string[] electronicStamps = Regex.Split(item["InitialElectronicStamps"].ToString(), "&#");
                    string[] electronicStampRecord = Regex.Split(electronicStamps[stepNumber - 1].ToString(), ";#");
                    if (electronicStampRecord[0].Equals(stepNumber.ToString()) && parameters.ContainsKey("Null Electronic Stamp") && !electronicStampRecord[1].ToUpper().Equals(parameters["Null Electronic Stamp"].ToUpper()))
                        electronicStamp = electronicStampRecord[1];
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetElectronicStamp " + ex.Message);
            }

            return electronicStamp;
        }

        /// <summary>
        /// Get which group responsible should recieve a notification.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="stepNumber"></param>
        /// <param name="Web"></param>
        /// <returns>SharePoint SPFieldUserValue which should recieve a notification</returns>
        public static SPFieldUserValue GetEmailReceiverGroup(SPListItem item, int stepNumber, SPWeb Web, string wfid, ref bool sendEmail)
        {
            SPFieldUserValue receiverGroup = null;

            try
            {
                if (item["OtherInitialData"] != null)
                {
                    //1;#False;#&#2;#True;#81&#3;#False;#&#4;#False;#&#5;#False;#&#6;#False;#
                    string[] mailingConfiguration = Regex.Split(item["OtherInitialData"].ToString(), "&#");
                    string[] mailingConfigurationRecord = Regex.Split(mailingConfiguration[stepNumber - 1].ToString(), ";#");
                    if (mailingConfigurationRecord[0].Equals(stepNumber.ToString()) && (mailingConfigurationRecord[1].ToUpper().Equals("TRUE") || mailingConfigurationRecord[1].Equals("1")))
                    {
                        sendEmail = true;

                        //True;#81
                        if (!string.IsNullOrEmpty(mailingConfigurationRecord[2]))
                        {
                            SPUser group = null;
                            try
                            {
                                group = Web.Users.GetByID(int.Parse(mailingConfigurationRecord[2]));
                            }
                            catch
                            {
                                group = Web.Site.RootWeb.SiteUsers.GetByID(int.Parse(mailingConfigurationRecord[2]));
                            }

                            receiverGroup = new SPFieldUserValue(Web, group.ID, group.Name);
                        }
                        else
                        {
                            General.saveErrorsLog(wfid,"WFTYpe: " + item["WFType"] + " - Step Number: " + stepNumber + " -  'E-mail Receiver Group' field not configured.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetEmailReceiverGroup " + ex.Message);
            }

            return receiverGroup;
        }

        /// <summary>
        /// Get the groups which should recieve a notification
        /// </summary>
        /// <param name="StepCollection"></param>
        /// <returns>StepNumber1;# GroupName1;&StepNumbe2;#GroupName2</returns>
        public static string GetInitialEmailReceiverGroups(SPListItemCollection stepCollection, SPWeb Web)
        {
            string receiverGroups = string.Empty;

            try
            {
                if (stepCollection != null)
                {
                    int count = 0;
                    foreach(SPListItem item in stepCollection)
                    {
                        if (item["SendEmail"] != null && item["EmailReceiverGroup"] != null)
                        {
                            SPFieldUserValue groupValue = new SPFieldUserValue(Web, item["EmailReceiverGroup"].ToString());
                                
                            if(count.Equals(0))
                                receiverGroups += item["StepNumber"] + ";#" + item["SendEmail"].ToString() + ";#" + groupValue.LookupId.ToString();
                            else
                                receiverGroups += "&#" + item["StepNumber"] + ";#" + item["SendEmail"].ToString() + ";#" + groupValue.LookupId.ToString();
                        }
                        else if (item["StepNumber"] != null)
                        {
                            if (count.Equals(0))
                                receiverGroups += item["StepNumber"] + ";#" + item["SendEmail"].ToString() + ";#" + string.Empty;
                            else
                                receiverGroups += "&#" + item["StepNumber"] + ";#" + item["SendEmail"].ToString() + ";#" + string.Empty;
                        }
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "GetInitialEmailReceiverGroups " + ex.Message);
            }

            return receiverGroups;
        }

        /// <summary>
        /// Get step owners
        /// </summary>
        /// <param name="Web"></param>
        /// <param name="wftype"></param>
        /// <returns></returns>
        public static List<SPUser> GetStepOwners(SPWeb Web, string wftype, string wfid)
        {
            List<SPUser> results = new List<SPUser>();
            
            try
            {
                SPList list = Web.Lists["RS Workflow Step Definitions"];
                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='Title' /><Value Type='Text'>" + wftype + "</Value></Eq></Where><OrderBy><FieldRef Name='StepNumber' Ascending='TRUE' /></OrderBy>";
                query.ViewFields = string.Concat(
                                  "<FieldRef Name='StepNumber' />",
                                  "<FieldRef Name='DefaultActor' />",
                                  "<FieldRef Name='Title' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need

                SPListItemCollection itemCol = list.GetItems(query);

                foreach (SPListItem item in itemCol)
                {
                    if (item["DefaultActor"] != null)
                    {

                        SPUser user = General.GetSPUser(item, "DefaultActor", wfid, Web);
                        results.Add(user);
                    }
                    else
                        results.Add(null);
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetStepOwners " + ex.Message);
            }

            return results;
        }

        #endregion

        #region Mixed methods


        public static void RemoveRecentlyLogs(string wfid, SPWeb Web, SPList logsList, int currentStepNumber, SPUser loggedUser, SPListItem item)
        {
            try
            {
                bool cancellLog = true;
                SPListItemCollection logsItemCol = null;
                string cancelledDefinition = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Cancelled.ToString());
                DateTime lastModifiedDate = Convert.ToDateTime(item["Modified"].ToString());
                string userAccount = string.Empty;

                if (loggedUser.ToString().Contains("\\"))
                    userAccount = loggedUser.ToString().Substring(loggedUser.ToString().IndexOf('\\') + 1);
                else
                    userAccount = loggedUser.ToString();

                SPQuery query = new SPQuery();
                query.Query = "<Where>"
                        + "<And><Geq><FieldRef Name='Created' /><Value  IncludeTimeValue='TRUE' Type='DateTime'>" + SPUtility.CreateISO8601DateTimeFromSystemDateTime(lastModifiedDate) + "</Value></Geq>"
                        + "<And><Eq><FieldRef Name='StepNumber' /><Value Type='Number'>" + currentStepNumber + "</Value></Eq>"
                        + "<And><Eq><FieldRef Name='WFID' /><Value Type='Text'>" + wfid + "</Value></Eq>"
                        + "<Eq><FieldRef Name='AssignedPerson' Nullable='TRUE' LookupId='True' /><Value Type='Integer'>" + loggedUser.ID + "</Value></Eq>"
                        + "</And></And></And></Where>";
               
              

                logsItemCol = logsList.GetItems(query);

                foreach (SPListItem logsItem in logsItemCol)
                {
                    
                    //ESMA-SHARE-1000 (Reassign current step + CANCEL)
                    if ((logsItem["ActionTaken"].ToString().Equals(WorkflowDataManagement.GetActionDescription(ActionsEnum.ActorReAssigned.ToString()))) && (logsItem["ActionDetails"].ToString().StartsWith("Step: " + currentStepNumber)) && (logsItem["ActionDetails"].ToString().Contains("Current actor: " + userAccount.ToUpper())))
                        cancellLog = false;
                    else
                        cancellLog = true;
                      
                       if (cancellLog.Equals(true))
                       {
                       if ((logsItem["ActionDetails"] != null) && !(logsItem["ActionDetails"].ToString().StartsWith(cancelledDefinition + ". ")))
                            logsItem["ActionDetails"] = cancelledDefinition + ". " + logsItem["ActionDetails"].ToString();
                        else
                            logsItem["ActionDetails"] = cancelledDefinition + ". ";

                        logsItem.SystemUpdate();
                       }
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "RemoveRecentlyLogs() " + ex.Message);
            }
        }

        public static bool HasRemovedRecentlyDocs(string wfid, SPList logsList, int currentStepNumber, ref List<string> docsRemovedList, SPUser loggedUser, SPListItem item)
        {
            bool hasRemovedDocs = false;
            string actionTakenDeletedFile = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.DocumentRemoved.ToString());
            DateTime lastModifiedDate = Convert.ToDateTime(item["Modified"].ToString());

            try
            {
               
                SPQuery query = new SPQuery();

  
                    query.Query = "<Where>"
                        + "<And><Geq><FieldRef Name='Created' /><Value  IncludeTimeValue='TRUE' Type='DateTime'>" + SPUtility.CreateISO8601DateTimeFromSystemDateTime(lastModifiedDate) + "</Value></Geq>"
                        + "<And><Eq><FieldRef Name='WFID' /><Value Type='Text'>" + wfid + "</Value></Eq>"
                        + "<And><Eq><FieldRef Name='StepNumber' /><Value Type='Number'>" + currentStepNumber + "</Value></Eq>"
                        + "<And><Eq><FieldRef Name='ActionTaken' /><Value Type='Choice'>" + actionTakenDeletedFile + "</Value></Eq>"
                        + "<Eq><FieldRef Name='AssignedPerson' Nullable='TRUE' LookupId='True' /><Value Type='Integer'>" + loggedUser.ID + "</Value></Eq>"
                        + "</And></And></And></And></Where>";
                
               

                query.ViewFields = string.Concat(
                                   "<FieldRef Name='Created' />",
                                   "<FieldRef Name='WFID' />",
                                   "<FieldRef Name='StepNumber' />",
                                   "<FieldRef Name='ActionTaken' />",
                                   "<FieldRef Name='ActionDetails' />",
                                   "<FieldRef Name='AssignedPerson' />",
                                   "<FieldRef Name='ID' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need

                SPListItemCollection logsItemCol = logsList.GetItems(query);


                if (logsItemCol != null && logsItemCol.Count > 0)
                {
                    hasRemovedDocs = true;

                    foreach (SPListItem logsItem in logsItemCol)
                    {
                            string fileName = string.Empty;
                      
                            if (logsItem["ActionDetails"] != null)
                                fileName = logsItem["ActionDetails"].ToString().Replace("Removed document:", null).Trim();

                            if ((!string.IsNullOrEmpty(fileName)) && (!(docsRemovedList.Contains(fileName))))
                                docsRemovedList.Add(fileName);
                       
                    }
                }
                
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "HasRemovedRecentlyDocs() " + ex.Message);
            }

            return hasRemovedDocs;
        }

        public static bool IsRejectedWF(string wfid, SPList logsList)
        {
            bool IsRejected = false;
            string actionTakenRejected = WorkflowDataManagement.GetActionDescription(WorkflowDataManagement.ActionsEnum.Rejected.ToString());

            try
            {

                SPQuery query = new SPQuery();
                query.Query = "<Where>"
                    + "<And><Eq><FieldRef Name='WFID' /><Value Type='Text'>" + wfid + "</Value></Eq>"
                    + "<Eq><FieldRef Name='ActionTaken' /><Value Type='Choice'>" + actionTakenRejected + "</Value></Eq>"
                    + "</And></Where>";
                query.ViewFields = string.Concat(
                                   "<FieldRef Name='WFID' />",
                                   "<FieldRef Name='ActionTaken' />",
                                   "<FieldRef Name='ID' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need

                SPListItemCollection itemCol = logsList.GetItems(query);


                if (itemCol != null && itemCol.Count > 0)
                    IsRejected = true;

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "IsRejectedWF() " + ex.Message);
            }

            return IsRejected;
        }

        /// <summary>
        /// Delete all workflow log records by workflow ID.
        /// </summary>
        /// <param name="WFID"></param>
        /// <param name="Web"></param>
        /// <param name="wfType"></param>
        public static void DeleteAllLogsByWFID(string wfid, SPWeb Web, string wfType, SPList logList)
        {
            try
            {
                

                SPQuery query = new SPQuery();
                query.Query = "<Where><Eq><FieldRef Name='WFID' /><Value Type='Text'>" + wfid + "</Value></Eq></Where>";
                query.ViewFields = string.Concat(
                                   "<FieldRef Name='WFID' />",
                                   "<FieldRef Name='ID' />");
                query.ViewFieldsOnly = true; // Fetch only the data that we need


                SPListItemCollection itemCol = logList.GetItems(query);

                List<int> idsToRemove = new List<int>();

                foreach (SPListItem item in itemCol)
                    idsToRemove.Add(item.ID);

                foreach (int auxID in idsToRemove)
                {
                    logList.Items.DeleteItemById(auxID);
                }


            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "DeleteAllLogsBywfid() - " + ex.Message);
            }
        }

        #endregion

        #region Print document

        /// <summary>
        /// Check if printed document exists.
        /// </summary>
        /// <param name="urlPrintedFile"></param>
        /// <param name="Web"></param>
        /// <param name="WFID"></param>
        /// <returns>True if printed document exists</returns>
        public static bool ExistPrintDocument(string urlPrintedFile, SPWeb Web, string wfid)
        {
            try
            {
                bool existDocument = false;

                SPFile file = Web.GetFile(urlPrintedFile);

                if (file.Exists)
                    existDocument = true;
                else
                    existDocument = false;

                return existDocument;

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "ExistPrintDocument() - " + ex.Message);
                return false;
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="WFID"></param>
        /// <param name="urlPath1"></param>
        /// <param name="urlPath2"></param>
        /// <returns></returns>
        public static string CombineURL(string wfid, string urlPath1, string urlPath2)
        {

            try
            {
                string url = urlPath1 + "/" + urlPath2;
                return url;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "CombineURL() - " + ex.Message);
                return "/";
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="documentLibraryURL"></param>
        /// <returns></returns>
        public static string FormatBlankSpacesURL(string documentLibraryURL)
        {
            try
            {
                if (documentLibraryURL.Contains(" "))
                    documentLibraryURL = documentLibraryURL.Replace(" ", "%20");

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(string.Empty, "FormatBlankSpacesURL() - " + ex.Message);
            }

            return documentLibraryURL;
        }

        /// <summary>
        /// Get printed document name
        /// </summary>
        /// <param name="printedDocumentName"></param>
        /// <param name="WFID"></param>
        /// <returns>Name of the printed document</returns>
        public static string GeneratePrintDocumentName(string printedDocumentName, string wfid)
        {

            try
            {
                if (HasInvalidCharacter_ListName(printedDocumentName, wfid) == true)
                    printedDocumentName = ReplaceInvalidCharacter_ListName(printedDocumentName, wfid);

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GeneratePrintDocumentName() - " + ex.Message);
            }

            return printedDocumentName;

        }

        //TBC
        /// <summary>
        /// Check if list has invalid characters
        /// </summary>
        /// <param name="listName"></param>
        /// <param name="WFID"></param>
        /// <returns>True if it has invalid characters</returns>
        private static bool HasInvalidCharacter_ListName(string listName, string wfid)
        {
            try
            {
                bool invalid = false;
                string[] listValues = new string[17] { "\\", "/", ":", "*", "?", "\"", "<", ">", "|", "{", "}", "#", "%", "~", "&amp;", "&", "." };
                string character = string.Empty;

                for (int i = 0; i < listValues.Length; i++)
                {
                    character = listValues[i].ToString();

                    if (listName.Contains(character))
                    {
                        invalid = true;
                        break;
                    }
                }

                return invalid;

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "HasInvalidCharacter_ListName() - " + ex.Message);
                return false;
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="listName"></param>
        /// <param name="WFID"></param>
        /// <returns></returns>
        private static string ReplaceInvalidCharacter_ListName(string listName, string wfid)
        {
            try
            {

                string[] listValues = new string[17] { "\\", "/", ":", "*", "?", "\"", "<", ">", "|", "{", "}", "#", "%", "~", "&amp;", "&", "." };
                string character = string.Empty;
                string listNameReplaced = listName;
                bool modified = false;

                string finalName = string.Empty;

                if (listNameReplaced.Contains(".."))
                {
                    listNameReplaced = listNameReplaced.Replace("..", ".");
                    modified = true;
                }

                for (int i = 0; i < listValues.Length; i++)
                {
                    character = listValues[i].ToString();

                    if (listNameReplaced.Contains(character))
                    {

                        #region <RULES>

                        if (listNameReplaced.StartsWith("~") || listNameReplaced.Contains("~"))
                        {
                            listNameReplaced = listNameReplaced.Replace("~", "_");
                            modified = true;
                        }

                        if ((listNameReplaced.Contains("&amp;")) || (listNameReplaced.Contains("&")))
                        {
                            if (listNameReplaced.Contains("&amp;"))
                                listNameReplaced = listNameReplaced.Replace("&amp;", "and");
                            else
                                listNameReplaced = listNameReplaced.Replace("&", "and");

                            modified = true;
                        }



                        if (listNameReplaced.Contains("#"))
                        {
                            listNameReplaced = listNameReplaced.Replace("#", null);
                            modified = true;
                        }

                        if (listNameReplaced.Contains("/"))
                        {
                            listNameReplaced = listNameReplaced.Replace("/", "-");
                            modified = true;
                        }

                        if (listNameReplaced.Contains("\\"))
                        {
                            listNameReplaced = listNameReplaced.Replace("\\", "-");
                            modified = true;
                        }

                        if (listNameReplaced.Contains(":"))
                        {
                            listNameReplaced = listNameReplaced.Replace(":", "-");
                            modified = true;
                        }

                        if (listNameReplaced.Contains("*"))
                        {
                            listNameReplaced = listNameReplaced.Replace("*", "-");
                            modified = true;
                        }

                        if (listNameReplaced.Contains("|"))
                        {
                            listNameReplaced = listNameReplaced.Replace("|", "-");
                            modified = true;
                        }

                        if (listNameReplaced.Contains("?"))
                        {
                            listNameReplaced = listNameReplaced.Replace("?", null);
                            modified = true;
                        }

                        if ((listNameReplaced.Contains("[")) || (listNameReplaced.Contains("]")))
                        {
                            if (listNameReplaced.Contains("["))
                            {
                                listNameReplaced = listNameReplaced.Replace("[", "(");
                            }
                            else
                            {
                                listNameReplaced = listNameReplaced.Replace("]", ")");
                            }

                            modified = true;
                        }

                        if ((listNameReplaced.Contains("<")) || (listNameReplaced.Contains(">")))
                        {
                            if (listNameReplaced.Contains("<"))
                            {
                                listNameReplaced = listNameReplaced.Replace("<", "(");
                            }
                            else
                            {
                                listNameReplaced = listNameReplaced.Replace(">", ")");
                            }

                            modified = true;
                        }

                        if ((listNameReplaced.Contains("{")) || (listNameReplaced.Contains("}")))
                        {
                            if (listNameReplaced.Contains("{"))
                            {
                                listNameReplaced = listNameReplaced.Replace("{", "(");
                            }
                            if (listNameReplaced.Contains("}"))
                            {
                                listNameReplaced = listNameReplaced.Replace("}", ")");
                            }

                            modified = true;
                        }


                        if (listNameReplaced.Contains("\""))
                        {
                            listNameReplaced = listNameReplaced.Replace("\"", "'");
                            modified = true;
                        }

                        if (listNameReplaced.Contains("%"))
                        {
                            listNameReplaced = listNameReplaced.Replace("%", null);
                            modified = true;
                        }


                        #endregion

                    }
                }

                if (modified == true)
                    finalName = listNameReplaced;
                else
                    finalName = listName;

                return finalName.Trim();
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "ReplaceInvalidCharacter_ListName() - " + ex.Message);
                return string.Empty;
            }
        }

        //TBC
        /// <summary>
        /// 
        /// </summary>
        /// <param name="defaultViewURL"></param>
        /// <param name="WFID"></param>
        /// <returns></returns>
        public static string GetDocumentLibraryURL(string defaultViewURL, string wfid)
        {
            string urlLibrary = string.Empty;

            try
            {
                

                if (defaultViewURL.Contains("/Forms/AllItems.aspx"))
                    urlLibrary = defaultViewURL.Replace("/Forms/AllItems.aspx", string.Empty);
                else
                    urlLibrary = defaultViewURL;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetDocumentLibraryURL() - " + ex.Message);
            }

            return urlLibrary;
        }

        /// <summary>
        /// Get the URL of the Printed document
        /// </summary>
        /// <param name="WFID"></param>
        /// <param name="Web"></param>
        /// <param name="wfOrder"></param>
        /// <returns></returns>
        public static string GetURLPrintedDocument(SPList docLib, string wfid, string wftypeName, SPWeb Web, string wfOrder)
        {
            string url = string.Empty;

            try
            {
                string printedDocumentName = GeneratePrintDocumentName(wftypeName.ToUpper().ToString(), wfid) + "_" + wfid + ".pdf";
                string WFURL = GetDocumentLibraryURL(docLib.DefaultViewUrl.ToString(), wfid);
                string urlPrintedFile = FormatBlankSpacesURL(CombineURL(wfid, WFURL, wfid));
                url = FormatBlankSpacesURL(CombineURL(wfid, urlPrintedFile, printedDocumentName));
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "OpenPrintDocument() - " + ex.Message);
            }

            return url;
        }

        #endregion
				
        /// <summary>
        /// Search site column in the "RS Columns" group
        /// </summary>
        public static SPField GetFieldInRSGroup(SPWeb web, string fieldName)
        {
            foreach (SPField field in web.Fields)
            {
              
                    if (field.Group.Equals("RS Columns") && field.Title.Trim().ToLower().Equals(fieldName.Trim().ToLower()))
                    {
                        return field;
                    }
                
            }
            return null;
        }


        //ESMA - CR26
        #region <NOTIFICATIONS>

        public static void NotificationsModule(string wfid, string initialStepNotifications, int stepNumber, SPFieldUserValue receiverGroupValue, SPWeb web, SPListItem item, string userAD, string passwordAD, Dictionary<string, string> parameters, Panel DynamicUserListsPanel)
        {
            try 
            {
                string wfSubject = WorkflowDataManagement.GetWorkflowSubject(item, web, wfid);
                //RS WF Step Definitions
                string[] notificationsArray = GetEmailNotificationValuesArray(wfid, initialStepNotifications, stepNumber);
                //RS Configuration Parameters
                string emailComunSubject = parameters["E-mail Signed Subject"];
                string emailComunBody = parameters["E-mail Signed Text"];

              //There is configured an specific mail for this current step.
                if (notificationsArray != null)
                {
                    string emailStepSubject = notificationsArray[1]; //Subject
                    string emailStepBody = notificationsArray[2]; //Body

                    if ((!emailStepSubject.Equals("(empty)")) && (!emailStepBody.Equals("(empty)")))
                    {
                        //E-mail Receiver User
                        if (notificationsArray.Count() > 3)
                        {
                            General.SendEmailStepManagement(wfid, notificationsArray, item, web, wfSubject, emailStepSubject, emailStepBody);
                            General.SendEmailGeneralManagement(wfid, item, web, wfSubject, emailComunSubject, emailComunBody, userAD, passwordAD, parameters, DynamicUserListsPanel, receiverGroupValue);
                        }
                        else
                            General.SendEmailGeneralManagement(wfid, item, web, wfSubject, emailStepSubject, emailStepBody, userAD, passwordAD, parameters, DynamicUserListsPanel, receiverGroupValue);
                        
                    }
                    else
                    {

                        //E-mail Receiver User
                        if (notificationsArray.Count() > 3)
                        {
                            General.SendEmailStepManagement(wfid, notificationsArray, item, web, wfSubject, emailComunSubject, emailComunBody);
                            General.SendEmailGeneralManagement(wfid, item, web, wfSubject, emailComunSubject, emailComunBody, userAD, passwordAD, parameters, DynamicUserListsPanel, receiverGroupValue);
                        }
                        else
                            General.SendEmailGeneralManagement(wfid, item, web, wfSubject, emailComunSubject, emailComunBody, userAD, passwordAD, parameters, DynamicUserListsPanel, receiverGroupValue);
                        
                    }
                }
                else
                {

                   
                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "NotificationsModule() " + ex.Message);
            }
        }

        public static string[] GetEmailNotificationValuesArray(string wfid, string initialStepNotifications, int stepNumber)
        {
            string[] notificationsArray = null;

            try
            {
                if (!string.IsNullOrEmpty(initialStepNotifications))
                {
                    string[] stepNotifAux = Regex.Split(initialStepNotifications, "%#");
                    string stepNotificationsValue = string.Empty;
                     
                    if (!stepNumber.Equals(1))
                       stepNotificationsValue  = stepNotifAux[stepNumber - 1];
                    else
                        stepNotificationsValue  = stepNotifAux[0];


                    if ((!string.IsNullOrEmpty(stepNotificationsValue)) && (stepNotificationsValue.Contains(";#")))
                        notificationsArray = Regex.Split(stepNotificationsValue, ";#");

                }

            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "GetEmailNotificationSteps() " + ex.Message);
            }

            return notificationsArray;
        }



        #endregion


        public static bool IsStepResponsible(Panel DynamicUserListsPanel, int currentStep, string userLoginName, string wfid)
        {
            bool isResponsible = false;
            int count = 1;

            try
            {
                foreach (Control control in DynamicUserListsPanel.Controls)
                {
                    if (control is UpdatePanel)
                    {
                        if (count.Equals(currentStep))
                        {
                            UpdatePanel actorUpdatePanel = (UpdatePanel)control;
                            DropDownList actorList = (DropDownList)actorUpdatePanel.Controls[0].Controls[0];
                            isResponsible = actorList.SelectedItem.Value.Equals(userLoginName.ToUpper());
                            break;
                        }
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "IsStepResponsible - UserLoginName: '" + userLoginName + "' - " + ex.Message);
            }

            return isResponsible;
        }

        public static bool IsWorkflowActor(Panel DynamicUserListsPanel, string userLoginName, string wfid)
        {
            bool isActor = false;

            try
            {
                foreach (Control control in DynamicUserListsPanel.Controls)
                {
                    if (control is UpdatePanel)
                    {
                        UpdatePanel actorUpdatePanel = (UpdatePanel)control;
                        DropDownList actorList = (DropDownList)actorUpdatePanel.Controls[0].Controls[0];
                        if (actorList.SelectedItem != null && actorList.SelectedItem.Value.Equals(userLoginName.ToUpper()))
                        {
                            isActor = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "IsWorkflowActor " + ex.Message);
            }

            return isActor;
        }

        public static bool IsMemberOfCurrentGroup(Panel DynamicUserListsPanel, int currentStep, string userLoginName, string wfid)
        {
            bool isMemberOfGroup = false;
            int count = 1;

            try
            {
                foreach (Control control in DynamicUserListsPanel.Controls)
                {
                    if (control is UpdatePanel)
                    {
                        if (count.Equals(currentStep))
                        {
                            UpdatePanel actorUpdatePanel = (UpdatePanel)control;
                            DropDownList actorList = (DropDownList)actorUpdatePanel.Controls[0].Controls[0];

                            if (actorList.Items.FindByValue(userLoginName.ToUpper()) != null)
                            {
                                if (!string.IsNullOrEmpty(actorList.Items.FindByValue(userLoginName.ToUpper()).Value))
                                    isMemberOfGroup = true;
                            }


                            break;
                        }
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "IsMemberOfCurrentGroup - UserLoginName: '" + userLoginName + "' - " + ex.Message);
            }

            return isMemberOfGroup;
        }


        //CR26-User member of Staff-Ext - Access Denied
        public static bool IsMemberOfStaffExtGroup(string wfid, Dictionary<string, string> parameters, SPUser loggedUser, string userAD, string passwordAD, Panel DynamicUserListsPanel, string stepNumber)
        {
            string userLoginName = string.Empty;
            bool isMember = false;

            try
            {
                //CR26. 
                //We do this here we know every step users
                //If the user belongs to Staff -Ext group you can only see WF where the user has an assigned step
                string domain = parameters["Domain"];
                string groupName = parameters["RS Staff Ext Group"];
                userLoginName = Permissions.GetOnlyUserAccount(loggedUser.LoginName, wfid).ToUpper();

                isMember = Permissions.UserBelongToGroup(domain, groupName, userLoginName, userAD, passwordAD, wfid, parameters, stepNumber);

                if (isMember)
                {
                    //Responsibles
                    Dictionary<string, string> actorsModified = ControlManagement.GetStepResponsibles(DynamicUserListsPanel, false, wfid);
                    //If is Member but belongs to other group, the user will have access.
                    if (actorsModified.ContainsValue(userLoginName))
                        isMember = false;
                }
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "IsMemberOfStaffExtGroup - UserLoginName: '" + userLoginName + "' - " + ex.Message);
            }

            return isMember;
        }


       

        //ESMA-CR31-Backup Groups
        public static bool IsMemberOfBackupResponsibleGroup(string wfid, SPUser loggedUser, string domainName, Dictionary<string, string> actorsBackupDictionary, string userAD, string passwordAD, string currentStep, Dictionary<string, string> parameters)
        {
            bool isBackupResponsible = false;
            string userLoginName = loggedUser.LoginName;

            try
            {
                string userName = loggedUser.Name;
                General.GetUserData(ref userLoginName, ref userName);

                string groupNameResponsible = actorsBackupDictionary.FirstOrDefault(x => x.Key == currentStep).Value;

                if (!string.IsNullOrEmpty(groupNameResponsible))
                    isBackupResponsible = Permissions.UserBelongToGroup(domainName, groupNameResponsible, userLoginName, userAD, passwordAD, wfid, parameters, currentStep);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "IsMemberOfBackupResponsibleGroup() - userLoginName: '" + userLoginName + "' - " + ex.Message);
            }

            return isBackupResponsible;
        }

        public static bool IsMemberOfBackupInitiatorGroup(string wfid, SPUser loggedUser, string domainName, Dictionary<string, string> actorsBackupDictionary, string userAD, string passwordAD, string currentStep, Dictionary<string, string> parameters)
        {
            bool isBackupInitiator = false;
            string userLoginName = loggedUser.LoginName;

            try
            {
                string userName = loggedUser.Name;
                General.GetUserData(ref userLoginName, ref userName);

                string groupNameInitiator = actorsBackupDictionary.FirstOrDefault(x => x.Key == "1").Value;

                if (!string.IsNullOrEmpty(groupNameInitiator))
                    isBackupInitiator = Permissions.UserBelongToGroup(domainName, groupNameInitiator, userLoginName, userAD, passwordAD, wfid, parameters, currentStep);
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "IsMemberOfBackupInitiatorGroup() - userLoginName: '" + userLoginName + "' - " + ex.Message);
            }

            return isBackupInitiator;
        }

        public static bool IsMemberOfBackupGroup(SPUser loggedUser, string wfid, string domainName, Dictionary<string, string> actorsBackupDictionary, string userAD, string passwordAD, ref bool isBackupInitiator, ref bool isBackupResponsible, string currentStep, Dictionary<string, string> parameters)
        {
            string userLoginName = loggedUser.LoginName;
            bool isMember = false;

            try
            {
                string userName = loggedUser.Name;
                General.GetUserData(ref userLoginName, ref userName);
                string groupNameInitiator = string.Empty;
                string groupNameResponsible = string.Empty;

                groupNameInitiator = actorsBackupDictionary.FirstOrDefault(x => x.Key == "1").Value;
                groupNameResponsible = actorsBackupDictionary.FirstOrDefault(x => x.Key == currentStep).Value;

                if (!string.IsNullOrEmpty(groupNameInitiator))
                    isBackupInitiator = Permissions.UserBelongToGroup(domainName, groupNameInitiator, userLoginName, userAD, passwordAD, wfid, parameters, currentStep);

                if (!string.IsNullOrEmpty(groupNameResponsible))
                    isBackupResponsible = Permissions.UserBelongToGroup(domainName, groupNameResponsible, userLoginName, userAD, passwordAD, wfid, parameters, currentStep);

                if (isBackupInitiator || isBackupResponsible)
                    isMember = true;
            }
            catch (Exception ex)
            {
                General.saveErrorsLog(wfid, "IsMemberOfBackupGroup - userLoginName: '" + userLoginName + "' - " + ex.Message);
            }

            return isMember;
        }

    }
}
