using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

using NexDirectLib;

namespace NexDirect.Dialogs
{
    public partial class OsuLoginCheck : Form
    {
        private MainWindow _parent;

        public OsuLoginCheck(MainWindow mw)
        {
            InitializeComponent();
            _parent = mw;
        }

        private async void TestCookies()
        {
            System.Net.CookieContainer cookies = null;
            try
            {
                cookies = await Osu.DeserializeCookies(_parent.officialOsuCookies);
            }
            catch
            {
                // Corrupted cookie store, deleting corruption
                Properties.Settings.Default.officialOsuCookies = null;
                Properties.Settings.Default.Save();
                cookies = new System.Net.CookieContainer();
            }

            try
            {
                if (!(await Osu.CheckLoginCookie(cookies, _parent.officialOsuUsername, _parent.officialOsuPassword)))
                {
                    MessageBoxResult cookiePrompt = System.Windows.MessageBox.Show("There was an error logging in to your account. Maybe you have changed your password, etc... to update that click NO and visit settings.\nClick YES to retry connection, click NO to fall back to Bloodcat for this session.", "NexDirect - Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (cookiePrompt == MessageBoxResult.Yes)
                    {
                        TestCookies();
                        return;
                    }
                    _parent.useOfficialOsu = false;
                    _parent.fallbackActualOsu = true;
                    Close();
                    return;
                }

                // store to parent & just persist them incase something new changed
                _parent.officialOsuCookies = await Osu.SerializeCookies(Osu.Cookies);
                Properties.Settings.Default.officialOsuCookies = _parent.officialOsuCookies;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("There was an error connecting to osu! servers, falling back to Bloodcat for this session...\n\n" + ex.ToString());
                _parent.useOfficialOsu = false;
                _parent.fallbackActualOsu = true;
            }

            Close();
        }

        private void OsuLoginCheck_Shown(object sender, EventArgs e)
        {
            TestCookies();
        }
    }
}
