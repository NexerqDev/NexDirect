using System;
using System.Windows.Forms;

namespace NexDirect
{
    static class TrayManager
    {
        public static NotifyIcon Icon = null;
        public static bool Loaded = false;

        public class NotifyIconInteractedArgs : EventArgs
        {
            public InteractionType Type;
            public NotifyIconInteractedArgs(InteractionType _type) { Type = _type; }
        }
        public delegate void NotifyIconInteractedHandler(NotifyIconInteractedArgs e);
        public static event NotifyIconInteractedHandler NotifyIconInteracted;

        public static void Init()
        {
            // https://stackoverflow.com/questions/1472633/wpf-application-that-only-has-a-tray-icon
            Icon = new NotifyIcon();
            Icon.Icon = Properties.Resources.logo;
            Icon.Text = "NexDirect";
            Icon.DoubleClick += (o, e1) => NotifyIconInteracted(new NotifyIconInteractedArgs(InteractionType.DoubleClick));
            Icon.ContextMenu = new ContextMenu(new MenuItem[]
            {
                    new MenuItem("Show", (o, e1) => NotifyIconInteracted(new NotifyIconInteractedArgs(InteractionType.CtxShow))),
                    new MenuItem("E&xit", (o, e1) => NotifyIconInteracted(new NotifyIconInteractedArgs(InteractionType.CtxExit)))
            });
            Icon.Visible = true;
            Loaded = true;
        }

        public static void Unload()
        {
            Icon.Visible = false;
            Icon.Dispose();
            Icon = null;
            Loaded = false;
        }

        public enum InteractionType
        {
            DoubleClick,
            CtxShow,
            CtxExit
        }
    }
}
