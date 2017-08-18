using System;
using System.Windows;
using System.Windows.Forms;

using NexDirectLib;
using NexDirectLib.Providers;

namespace NexDirect.Dialogs
{
    public partial class OsuLoginCheck : Form
    {
        private MainWindow _mw;

        public OsuLoginCheck(MainWindow mw)
        {
            InitializeComponent();
            _mw = mw;
        }

        private async void TestCookies()
        {
            System.Net.CookieContainer cookies = null;
            try
            {
                cookies = await CookieStoreSerializer.DeserializeCookies(SettingManager.Get("officialOsuCookies"));
            }
            catch
            {
                // Corrupted cookie store, deleting corruption
                SettingManager.Set("officialOsuCookies", null);
                cookies = new System.Net.CookieContainer();
            }

            try
            {
                await Osu.CheckPassedLoginCookieElseUseNew(cookies, SettingManager.Get("officialOsuUsername"), SettingManager.Get("officialOsuPassword"));

                // store to parent & just persist them incase something new changed
                SettingManager.Set("officialOsuCookies", await CookieStoreSerializer.SerializeCookies(Osu.Cookies));
            }
            catch (Osu.InvalidPasswordException)
            {
                MessageBoxResult cookiePrompt = System.Windows.MessageBox.Show("There was an error logging in to your account. Maybe you have changed your password, etc... to update that click NO and visit settings.\nClick YES to retry connection, click NO to fall back to Bloodcat for this session.", "NexDirect - Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (cookiePrompt == MessageBoxResult.Yes)
                {
                    TestCookies();
                    return;
                }
                SettingManager.Set("useOfficialOsu", false, true);
                SettingManager.Set("fallbackActualOsu", true, true);
                Close();
                return;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("There was an error connecting to osu! servers, falling back to Bloodcat for this session...\n\n" + ex.ToString());
                SettingManager.Set("useOfficialOsu", false, true);
                SettingManager.Set("fallbackActualOsu", true, true);
            }

            Close();
        }

        private void OsuLoginCheck_Shown(object sender, EventArgs e)
        {
            TestCookies();
        }
    }
}
