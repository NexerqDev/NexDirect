using NexDirectLib;
using NexDirectLib.Structures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gecko;

namespace NexDirect.Dialogs
{
    public partial class BloodcatCaptcha : Form
    {
        private static BloodcatCaptcha _this;
        private GeckoWebBrowser browser;

        public BloodcatCaptcha(BeatmapSet set)
        {
            InitializeComponent();

            _this = this;

            // make our gecko browser
            browser = new GeckoWebBrowser();
            this.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;

            Gecko.LauncherDialog.Download += downloadHandler;
            browser.Navigate("http://bloodcat.com/osu/s/" + set.Id);
        }

        private void downloadHandler(object sender, LauncherDialogEvent e)
        {
            // we're not actually interested in download, just cookie hijack
            using (var cookies = CookieManager.GetEnumerator())
            {
                while (cookies.MoveNext()) // loop thru
                {
                    Cookie cookie = cookies.Current;
                    Bloodcat.Cookies.Add(new System.Net.Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Host));
                }
            }

            Close();
        }
    }
}
