using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;

namespace ESMA.Paperless.EventsReceiver.v16.EventsReceiver
{
    public class DisabledItemEventsScope : SPItemEventReceiver, IDisposable
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
