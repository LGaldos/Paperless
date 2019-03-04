using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESMA.Paperless.Webparts.v16.RSWorkflowInitiation
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
