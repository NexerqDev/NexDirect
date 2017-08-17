using NexDirectLib;
using NexDirectLib.Structures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NexDirect.Dialogs
{
    /// <summary>
    /// Interaction logic for Captcha.xaml
    /// </summary>
    public partial class Captcha : Window
    {
        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool InternetGetCookieEx(string pchURL, string pchCookieName, StringBuilder pchCookieData, ref uint pcchCookieData, int dwFlags, IntPtr lpReserved);
        private const int INTERNET_COOKIE_HTTPONLY = 0x00002000;

        private BeatmapSet set;

        public Captcha(BeatmapSet set)
        {
            InitializeComponent();
            this.set = set;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (getCookieData()) // hey, if we already have em why are we here.
                return;

            (formsBrowser.ActiveXInstance as SHDocVw.ShellBrowserWindow).FileDownload += browser_blockDownloading;
            formsBrowser.Navigate("http://bloodcat.com/osu/s/" + set.Id);
        }

        private void browser_blockDownloading(bool ActiveDocument, ref bool Cancel)
        {
            if (ActiveDocument)
                return;

            Cancel = true;

            // download is meant to be starting, that means we are outta here boiiiii
            // but lets verify cookies first.
            getCookieData();
        }

        private bool getCookieData()
        {
            string cookieUri = "http://bloodcat.com/osu/";

            uint datasize = 1024;
            StringBuilder cookieData = new StringBuilder((int)datasize);
            if (InternetGetCookieEx(cookieUri, null, cookieData, ref datasize, INTERNET_COOKIE_HTTPONLY, IntPtr.Zero) && cookieData.Length > 0)
            {
                Bloodcat.Cookies.SetCookies(new Uri(cookieUri), cookieData.ToString().Replace(';', ','));
                Close();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
