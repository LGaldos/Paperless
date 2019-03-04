using System;
using Microsoft.SharePoint;

namespace ESMA.Paperless.DailyProcess.v16
{
    public class DisabledItemEventsScope : SPItemEventReceiver, IDisposable
    {
        private bool eventFiringEnabledStatus;

        /// <summary>
        /// Disable attached item event handlers
        /// </summary>
        public DisabledItemEventsScope()
        {
            eventFiringEnabledStatus = base.EventFiringEnabled;
            base.EventFiringEnabled = false;
        }

        #region IDisposable Members

        /// <summary>
        /// Enable attached item event handlers
        /// </summary>
        public void Dispose()
        {
            base.EventFiringEnabled = eventFiringEnabledStatus;
        }

        #endregion
    }
}
