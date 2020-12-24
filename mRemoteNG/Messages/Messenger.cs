using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static mRemoteNG.UI.Window.BaseWindow;

namespace mRemoteNG.Messages
{

    public class Messenger
    {
        public static event EventHandler Reconnect; // event

        public void RequestReconnect()
        {
            Reconnect?.Invoke(null, null);
        }
    }

}
