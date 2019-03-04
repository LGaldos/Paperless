using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.SharePoint;

namespace ESMA.Paperless.Design.v16.Features.ESMA.Paperless.Design.v16
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>

    [Guid("896acdee-c537-49ab-9bc9-7ebed7e6df35")]
    public class ESMAPaperlessDesignEventReceiver : SPFeatureReceiver
    {
        private const string DEF_Master = "/_catalogs/masterpage/seattle.master";
        private const string DEF_Master_RS = "/_catalogs/masterpage/RS.V16.master";

        // Uncomment the method below to handle the event raised after a feature has been activated.

        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            try
            {
                SPWeb web = properties.Feature.Parent as SPWeb;

                if (web != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        using (SPSite elevatedSite = new SPSite(web.Site.ID))
                        {
                            SPWeb elevatedWeb = elevatedSite.RootWeb;
                            elevatedWeb.AllowUnsafeUpdates = true;

                            //Design Master Page (Customization)
                            DesignModule.ApplyMasterModule(elevatedWeb, DEF_Master_RS, "");

                            elevatedWeb.AllowUnsafeUpdates = false;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                SPWeb web = properties.Feature.Parent as SPWeb;
                DesignModule.SaveErrorsLog_Design(web, "", ex.Message);
            }
        }


        // Uncomment the method below to handle the event raised before a feature is deactivated.

        public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        {
            try
            {
                SPWeb web = properties.Feature.Parent as SPWeb;

                if (web != null)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        using (SPSite elevatedSite = new SPSite(web.Site.ID))
                        {
                            SPWeb elevatedWeb = elevatedSite.RootWeb;
                            elevatedWeb.AllowUnsafeUpdates = true;

                            //Design Master Page (Default)
                            DesignModule.ApplyMasterModule(elevatedWeb, DEF_Master, DEF_Master);

                            elevatedWeb.AllowUnsafeUpdates = false;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                SPWeb web = properties.Feature.Parent as SPWeb;
                DesignModule.SaveErrorsLog_Design(web, "", ex.Message);
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
    }
}
