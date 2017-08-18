using NexDirect.Management;
using NexDirectLib;
using NexDirectLib.Providers;
using NexDirectLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for OsuLoginPop.xaml
    /// </summary>
    public partial class OsuLoginPop : Window
    {
        private Settings _s;

        public OsuLoginPop(Settings s)
        {
            InitializeComponent();
            _s = s;
        }

        private async void loginButton_Click(object sender, EventArgs e)
        {
            loginButton.IsEnabled = false; // disable no spam plz

            try
            {
                System.Net.CookieContainer _cookies = await Osu.LoginAndGetCookie(usernameTextBox.Text, passwordPasswordBox.Password);
                SettingManager.Set("officialOsuCookies", await CookieStoreSerializer.SerializeCookies(_cookies));
                SettingManager.Set("useOfficialOsu", true);
                //SettingManager.Set("fallbackActualOsu", true);
                SettingManager.Set("officialOsuUsername", usernameTextBox.Text);
                SettingManager.Set("officialOsuPassword", passwordPasswordBox.Password);
                _s.officialLoggedInAs.Content = "Currently logged in as: " + usernameTextBox.Text;
                MessageBox.Show("Logged in to osu! servers and login data saved.");
                Close();
                return;
            }
            catch (Osu.InvalidPasswordException) { MessageBox.Show("You have specified an incorrect password. Please try again."); }
            catch (Exception ex) { MessageBox.Show("There was an error logging in to the osu! servers...\n\n" + ex); }

            loginButton.IsEnabled = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            usernameTextBox.Focus();
        }
    }
}
