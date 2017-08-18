using NexDirectLib;
using NexDirectLib.Providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for OsuCredsOnLaunch.xaml
    /// </summary>
    public partial class OsuCredsOnLaunch : Window
    {
        public OsuCredsOnLaunch()
        {
            InitializeComponent();
        }

        bool done = false;

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!done)
                e.Cancel = true;
            else
                base.OnClosing(e);
        }

        public static async Task TestCookies()
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
                return;
            }
            catch (Osu.InvalidPasswordException)
            {
                MessageBoxResult cookiePrompt = MessageBox.Show("There was an error logging in to your account. Maybe you have changed your password, etc... to update that click NO and visit settings.\nClick YES to retry connection, click NO to fall back to Bloodcat for this session.", "NexDirect - Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (cookiePrompt == MessageBoxResult.Yes)
                {
                    await TestCookies();
                    return;
                }
                SettingManager.Set("useOfficialOsu", false, true);
                SettingManager.Set("fallbackActualOsu", true, true);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error connecting to osu! servers, falling back to Bloodcat for this session...\n\n" + ex.ToString());
                SettingManager.Set("useOfficialOsu", false, true);
                SettingManager.Set("fallbackActualOsu", true, true);
                return;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await TestCookies();
            done = true;
            Close();
        }
    }
}
