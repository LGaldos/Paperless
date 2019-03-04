using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;

namespace ESMA.Paperless.PrintProcess.v16
{
    class DisabledItemEventsScope : SPItemEventReceiver, IDisposable
    {
        private bool eventFiringEnabledStatus;

        public DisabledItemEventsScope()
        {
            eventFiringEnabledStatus = base.EventFiringEnabled;
            base.EventFiringEnabled = false;
        }

        #region IDisposable Members

        public void Dispose()
        {
            base.EventFiringEnabled = eventFiringEnabledStatus;
        }

        #endregion

    }
}
