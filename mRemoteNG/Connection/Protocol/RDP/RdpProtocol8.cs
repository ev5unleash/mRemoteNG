using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using AxMSTSCLib;
using mRemoteNG.App;
using mRemoteNG.Messages;
using mRemoteNG.Resources.Language;
using MSTSCLib;

namespace mRemoteNG.Connection.Protocol.RDP
{
    /* RDP v8 requires Windows 7 with:
		* https://support.microsoft.com/en-us/kb/2592687 
		* OR
		* https://support.microsoft.com/en-us/kb/2923545
		* 
		* Windows 8+ support RDP v8 out of the box.
		*/

    public delegate void ReconnectNotification(); //Reconnect request from RDP protocol

    public class RdpProtocol8 : RdpProtocol7
    {
        private MsRdpClient8NotSafeForScripting RdpClient8 => (MsRdpClient8NotSafeForScripting)((AxHost)Control).GetOcx();
        private Size _controlBeginningSize;
        private static System.Timers.Timer ResizeTimer = new System.Timers.Timer(250);
        private static Boolean ResizeSet = false;
        //private Messenger messenger = new Messenger();

        protected override RdpVersion RdpProtocolVersion => RdpVersion.Rdc8;


        private void SetResizeTimer()
        {
            ResizeTimer.Elapsed += FireResize; //Function to run on fire
            ResizeTimer.AutoReset = false; //Only fire once
            ResizeTimer.Enabled = true; //Enable
        }

        private void ResizingFire()
        {
            if(!ResizeSet)
            {
                ResizeSet = true;
                SetResizeTimer();
            }
            ResizeTimer.Stop();
            ResizeTimer.Start();
        }

        public override bool SmartSize
        {
            get => base.SmartSize;
            protected set
            {
                base.SmartSize = value;
                ReconnectForResize();
            }
        }

        public override bool Fullscreen
        {
            get => base.Fullscreen;
            protected set
            {
                base.Fullscreen = value;
                ReconnectForResize();
            }
        }

        public override void ResizeBegin(object sender, EventArgs e)
        {
            _controlBeginningSize = Control.Size;
        }

        public override void Resize(object sender, EventArgs e)
        {
            if (DoResize() && _controlBeginningSize.IsEmpty)
            {
                //Wait a bit before reconnect on resize
                ResizingFire();
            }
            base.Resize(sender, e);
        }

        private void FireResize(object sender, EventArgs e)
        {
            ReconnectForResize();
        }

        public override void ResizeEnd(object sender, EventArgs e)
        {
            DoResize();
            if (!(Control.Size == _controlBeginningSize))
            {
                ReconnectForResize();
            }
            _controlBeginningSize = Size.Empty;
        }

        protected override AxHost CreateActiveXRdpClientControl()
        {
            return new AxMsRdpClient8NotSafeForScripting();
        }



        private void ReconnectForResize()
        {
            if (!loginComplete)
                return;

            if (!InterfaceControl.Info.AutomaticResize)
                return;

            if (!(InterfaceControl.Info.Resolution == RDPResolutions.FitToWindow ||
                  InterfaceControl.Info.Resolution == RDPResolutions.Fullscreen))
                return;

            if (SmartSize)
                return;

            Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                $"Resizing RDP connection to host '{connectionInfo.Hostname}'");

            try
            {
                //Disconnect();
                //ReconnectManual();
                //Connect();
                //Should we sleep here and wait for a final resize?
                 var size = Fullscreen
                    ? Screen.FromControl(Control).Bounds.Size
                    : Control.Size;

                //RdpClient8.Reconnect((uint)size.Width, (uint)size.Height);



                RdpClient8.Disconnect();
                Thread.Sleep(100); //Don't reconnect too fast
                SetResolution(true);
                Connect();
                return;

            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage(
                    string.Format(Language.ChangeConnectionResolutionError,
                        connectionInfo.Hostname),
                    ex, MessageClass.WarningMsg, false);
            }
        }

        private bool DoResize()
        {
            Control.Location = InterfaceControl.Location;
            // kmscode - this doesn't look right to me. But I'm not aware of any functionality issues with this currently...
            if (!(Control.Size == InterfaceControl.Size) && !(InterfaceControl.Size == Size.Empty))
            {
                Control.Size = InterfaceControl.Size;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static implicit operator ReconnectNotification(RdpProtocol8 v)
        {
            throw new NotImplementedException();
        }
    }
}
