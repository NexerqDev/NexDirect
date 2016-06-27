using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

using NexDirectLib;

namespace NexDirect.Dialogs
{
    public partial class OsuLogin : Form
    {
        private MainWindow _mw;
        private Settings _s;

        public OsuLogin(Settings s, MainWindow mw)
        {
            InitializeComponent();
            _mw = mw;
            _s = s;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false; // disable no spam plz

            try
            {
                System.Net.CookieContainer _cookies = await Osu.LoginAndGetCookie(usernameBox.Text, passwordBox.Text);
                _mw.officialOsuCookies = await Osu.SerializeCookies(_cookies);
                _mw.fallbackActualOsu = true;
                _mw.officialOsuUsername = usernameBox.Text;
                _mw.officialOsuPassword = passwordBox.Text;
                _s.officialLoggedInAs.Content = "Currently logged in as: " + usernameBox.Text;
                Properties.Settings.Default.officialOsuCookies = _mw.officialOsuCookies;
                Properties.Settings.Default.useOfficialOsu = true;
                Properties.Settings.Default.officialOsuUsername = usernameBox.Text;
                Properties.Settings.Default.officialOsuPassword = passwordBox.Text;
                Properties.Settings.Default.Save();
                MessageBox.Show("Logged in to osu! servers and login data saved. Restart NexDirect to begin using the official osu! servers.");
                Close();
                return;
            }
            catch (Osu.InvalidPasswordException)
            {
                MessageBox.Show("You have specified an incorrect password. Please try again.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error logging in to the osu! servers...\n" + ex);
            }

            button1.Enabled = true;
        }
    }
}
