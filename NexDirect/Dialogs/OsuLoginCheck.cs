using System;
using System.Windows;
using System.Windows.Forms;

using NexDirectLib;

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
                cookies = await Osu.DeserializeCookies(_mw.officialOsuCookies);
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
                await Osu.CheckLoginCookie(cookies, _mw.officialOsuUsername, _mw.officialOsuPassword);

                // store to parent & just persist them incase something new changed
                _mw.officialOsuCookies = await Osu.SerializeCookies(Osu.Cookies);
                Properties.Settings.Default.officialOsuCookies = _mw.officialOsuCookies;
                Properties.Settings.Default.Save();
            }
            catch (Osu.InvalidPasswordException)
            {
                MessageBoxResult cookiePrompt = System.Windows.MessageBox.Show("There was an error logging in to your account. Maybe you have changed your password, etc... to update that click NO and visit settings.\nClick YES to retry connection, click NO to fall back to Bloodcat for this session.", "NexDirect - Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (cookiePrompt == MessageBoxResult.Yes)
                {
                    TestCookies();
                    return;
                }
                _mw.useOfficialOsu = false;
                _mw.fallbackActualOsu = true;
                Close();
                return;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("There was an error connecting to osu! servers, falling back to Bloodcat for this session...\n\n" + ex.ToString());
                _mw.useOfficialOsu = false;
                _mw.fallbackActualOsu = true;
            }

            Close();
        }

        private void OsuLoginCheck_Shown(object sender, EventArgs e)
        {
            TestCookies();
        }
    }
}
