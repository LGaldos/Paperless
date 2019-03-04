using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using ESMA.Paperless.Reports.v16.TimerJobs;

namespace ESMA.Paperless.Reports.v16.Features.ESMA.Paperless.Reports.Timerjobs.v15
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>

    [Guid("7f659378-41b9-492c-9848-748deb900338")]
    public class ESMAPaperlessReportsTimerjobsEventReceiver : SPFeatureReceiver
    {
        public string ReportsSendMailJobName = "RSReportsSendMailTimerJob";
        public string ReportsCreateJobName = "RSReportsCreateTimerJob";

        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    SPWebApplication parentWebApp = (SPWebApplication)properties.Feature.Parent;
                    SPSite site = properties.Feature.Parent as SPSite;

                    //Send mails
                    DeleteExistingJob(ReportsSendMailJobName, parentWebApp);
                    CreateJob_DailyNotifications(parentWebApp);

                    //Create Reports timerjob
                    DeleteExistingJob(ReportsCreateJobName, parentWebApp);
                    CreateJob_ReportsCreate(parentWebApp);
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        {
            lock (this)
            {
                try
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        SPWebApplication parentWebApp = (SPWebApplication)properties.Feature.Parent;
                        
                        //Send mails timerjob
                        DeleteExistingJob(ReportsSendMailJobName, parentWebApp);

                        //Create Reports timerjob
                        DeleteExistingJob(ReportsCreateJobName, parentWebApp);
                    });
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }


        // Uncomment the method below to handle the event raised after a feature has been installed.

        //public override void FeatureInstalled(SPFeatureReceiverProperties properties)
        //{
        //}


        // Uncomment the method below to handle the event raised before a feature is uninstalled.

        //public override void FeatureUninstalling(SPFeatureReceiverProperties properties)
        //{
        //}

        // Uncomment the method below to handle the event raised when a feature is upgrading.

        //public override void FeatureUpgrading(SPFeatureReceiverProperties properties, string upgradeActionName, System.Collections.Generic.IDictionary<string, string> parameters)
        //{
        //}
        #region <METHODS>

        public bool DeleteExistingJob(string jobName, SPWebApplication site)
        {
            bool jobDeleted = false;

            try
            {
                foreach (SPJobDefinition job in site.JobDefinitions)
                {
                    if (job.Name.Equals(jobName))
                    {
                        job.Delete();
                        jobDeleted = true;
                    }
                }
            }
            catch (Exception)
            {
                return jobDeleted;
            }

            return jobDeleted;
        }

        //Daily Job
        private bool CreateJob_DailyNotifications(SPWebApplication site)
        {
            bool jobCreated = false;

            try
            {
                ReportsSendMail job = new ReportsSendMail(ReportsSendMailJobName, site);

                // Set the schedule - run once daily 
                SPDailySchedule schedule = new SPDailySchedule();
                schedule.BeginHour = 1;
                schedule.BeginMinute = 0;

                schedule.EndHour = 1;
                schedule.EndMinute = 59;

                job.Schedule = schedule;
                job.Update();
            }
            catch (Exception)
            {
                return jobCreated;
            }
            return jobCreated;
        }

        private bool CreateJob_ReportsCreate(SPWebApplication site)
        {
            bool jobCreated = false;

            try
            {
                ReportsCreate job = new ReportsCreate(ReportsCreateJobName, site);

                // Set the schedule - run every 5 minutes
                SPMinuteSchedule schedule = new SPMinuteSchedule();
                schedule.BeginSecond = 0;
                schedule.EndSecond = 59;
                schedule.Interval = 5;

                job.Schedule = schedule;
                job.Update();
            }
            catch (Exception)
            {
                return jobCreated;
            }
            return jobCreated;
        }

        #endregion

    }
}
