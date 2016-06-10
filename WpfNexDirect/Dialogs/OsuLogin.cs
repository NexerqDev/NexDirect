using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NexDirect.Dialogs
{
    public partial class OsuLogin : Form
    {
        private MainWindow _mw;

        public OsuLogin(MainWindow mw)
        {
            InitializeComponent();
            _mw = mw;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                System.Collections.Specialized.StringDictionary cookies = await Osu.LoginAndGetCookie(usernameBox.Text, passwordBox.Text);
                _mw.officialOsuCookies = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(cookies));
                _mw.fallbackActualOsu = true;
                _mw.officialOsuUsername = usernameBox.Text;
                _mw.officialOsuPassword = passwordBox.Text;
                Properties.Settings.Default.officialOsuCookies = _mw.officialOsuCookies;
                Properties.Settings.Default.useOfficialOsu = true;
                Properties.Settings.Default.officialOsuUsername = usernameBox.Text;
                Properties.Settings.Default.officialOsuPassword = passwordBox.Text;
                Properties.Settings.Default.Save();
                MessageBox.Show("Logged in to osu! servers and login data saved. Restart NexDirect to begin using the official osu! servers.");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error logging in...\n" + ex);
            }
        }
    }
}
