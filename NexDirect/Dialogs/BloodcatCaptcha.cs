using CefSharp;
using CefSharp.WinForms;
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

namespace NexDirect.Dialogs
{
    public partial class BloodcatCaptcha : Form
    {
        private static BloodcatCaptcha _this;
        private ChromiumWebBrowser browser;

        public BloodcatCaptcha(BeatmapSet set)
        {
            InitializeComponent();

            _this = this;

            // make our chrome browser
            if (!Cef.IsInitialized)
            {
                CefSettings settings = new CefSettings();
                Cef.Initialize(settings);
            }

            browser = new ChromiumWebBrowser("http://bloodcat.com/osu/s/" + set.Id);
            this.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;

            browser.DownloadHandler = new downloadHandle();
        }

        // when it tries that means its done
        private class downloadHandle : IDownloadHandler
        {
            public void OnBeforeDownload(IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
            {
                // really just dont do jack shit about the download
                Cef.GetGlobalCookieManager().VisitAllCookies(new bloodcatCookieHandle());
                _this.Invoke(new Action(() => _this.Close()));
            }

            public void OnDownloadUpdated(IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback) { }
        }

        private class bloodcatCookieHandle : ICookieVisitor
        {
            public bool Visit(Cookie cookie, int count, int total, ref bool deleteCookie)
            {
                // just transfer all cookies over to handle
                Bloodcat.Cookies.Add(new System.Net.Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
                return true;
            }

            #region IDisposable Support
            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects).
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // TODO: set large fields to null.

                    disposedValue = true;
                }
            }

            // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            // ~bloodcatCookieHandle() {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //   Dispose(false);
            // }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: uncomment the following line if the finalizer is overridden above.
                // GC.SuppressFinalize(this);
            }
            #endregion
        }
    }
}
