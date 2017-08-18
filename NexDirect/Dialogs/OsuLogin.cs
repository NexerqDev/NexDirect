using System;
using System.Windows.Forms;

using NexDirectLib;
using NexDirectLib.Providers;

namespace NexDirect.Dialogs
{
    public partial class OsuLogin : Form
    {
        private Settings _s;

        public OsuLogin(Settings s)
        {
            InitializeComponent();
            _s = s;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false; // disable no spam plz

            try
            {
                System.Net.CookieContainer _cookies = await Osu.LoginAndGetCookie(usernameBox.Text, passwordBox.Text);
                SettingManager.Set("officialOsuCookies", await CookieStoreSerializer.SerializeCookies(_cookies));
                SettingManager.Set("useOfficialOsu", true);
                //SettingManager.Set("fallbackActualOsu", true);
                SettingManager.Set("officialOsuUsername", usernameBox.Text);
                SettingManager.Set("officialOsuPassword", passwordBox.Text);
                _s.officialLoggedInAs.Content = "Currently logged in as: " + usernameBox.Text;
                MessageBox.Show("Logged in to osu! servers and login data saved.");
                Close();
                return;
            }
            catch (Osu.InvalidPasswordException) { MessageBox.Show("You have specified an incorrect password. Please try again."); }
            catch (Exception ex) { MessageBox.Show("There was an error logging in to the osu! servers...\n\n" + ex); }

            button1.Enabled = true;
        }
    }
}
