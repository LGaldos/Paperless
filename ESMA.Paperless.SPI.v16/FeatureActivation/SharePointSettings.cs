using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;
using System.Globalization;


namespace ESMA.Paperless.EventsReceiver.v16
{
    class SharePointSettings
    {
        //------------------------------------------------------------------------------
        //REGIONAL SETTINGS
        //------------------------------------------------------------------------------
        public static void ChangeCulture(SPWeb web)
        {
            try
            {
                //Initialize CultureInfo
                CultureInfo ci = new CultureInfo("en-GB");

                if (ci != null)
                {
                    web.Locale = ci;
                    web.RegionalSettings.Time24 = true;
                    web.RegionalSettings.TimeZone.ID = 4; //W. Europe Standard Time (UTC+01:00) Amsterdam, Berlin, Bern, Rome, Stockholm, Vienna)
                    web.RegionalSettings.FirstDayOfWeek = 1; //Monday
                    web.Update();
                }

            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("ChangeCulture(): " + ex.Source, ex.Message);
            }
        }

        //------------------------------------------------------------------------------
        //REQUEST ACCESS EMAIL
        //------------------------------------------------------------------------------
        public static void DisableRequestAccess(SPWeb web)
        {
            try
            {
                web.RequestAccessEmail = "";
                web.Update();

            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("DisableRequestAccess(): " + ex.Source, ex.Message);
            }
        }

        //------------------------------------------------------------------------------
        //SYNC
        //------------------------------------------------------------------------------
        public static void DisableSyncOption(SPWeb web)
        {
            try
            {
                web.ExcludeFromOfflineClient = false;
                web.Update();

            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("DisableSyncOption(): " + ex.Source, ex.Message);
            }
        }

        //------------------------------------------------------------------------------
        //Updating of CustomUploadPage
        //------------------------------------------------------------------------------
        public static void UpdateUploadPage(SPWeb web)
        {
            try
            {
                web.CustomUploadPage = null;
                web.Update();

            }
            catch (Exception ex)
            {
                General.SaveErrorsLogArchitecture("UpdateUploadPage(): " + ex.Source, ex.Message);
            }
        }

    }
}
