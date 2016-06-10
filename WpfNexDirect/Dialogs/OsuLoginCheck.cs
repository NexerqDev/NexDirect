using Newtonsoft.Json.Linq;
using System;
using System.Windows;
using System.Windows.Forms;

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

        private async void testCookies()
        {
            var cookies = new System.Collections.Specialized.StringDictionary();
            foreach (var kv in (JArray)Newtonsoft.Json.JsonConvert.DeserializeObject(_parent.officialOsuCookies))
            {
                cookies.Add(kv["Key"].ToString(), kv["Value"].ToString());
            }

            try
            {
                System.Net.CookieContainer _cookies = await Osu.CheckLoginCookie(_parent, cookies);

                if (_cookies == null)
                {
                    MessageBoxResult cookiePrompt = System.Windows.MessageBox.Show("There was an error logging in to your account. Maybe you have changed your password, etc... to update that click NO and visit settings.\nClick YES to retry connection, click NO to fall back to Bloodcat for this session.", "NexDirect - Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (cookiePrompt == MessageBoxResult.Yes)
                    {
                        testCookies();
                        return;
                    }
                    _parent.useOfficialOsu = false;
                    _parent.fallbackActualOsu = true;
                    Close();
                    return;
                }
                _parent.officialCookieJar = _cookies;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("There was an error connecting to osu! servers, falling back to Bloodcat for this session...\n" + ex.ToString());
                _parent.useOfficialOsu = false;
                _parent.fallbackActualOsu = true;
            }

            Close();
        }

        private void OsuLoginCheck_Shown(object sender, EventArgs e)
        {
            testCookies();
        }
    }
}
