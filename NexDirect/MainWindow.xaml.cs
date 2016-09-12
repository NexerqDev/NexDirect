using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using NexDirectLib;
using NexDirectLib.Structures;
using WPFFolderBrowser;
using Microsoft.Win32;

namespace NexDirect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<BeatmapSet> beatmaps = new ObservableCollection<BeatmapSet>(); // ObservableCollection: will send updates to other objects when updated (will update the datagrid binding)

        public string osuFolder = Properties.Settings.Default.osuFolder;
        public string osuSongsFolder => Path.Combine(osuFolder, "Songs");
        public bool overlayMode = Properties.Settings.Default.overlayMode;
        public bool audioPreviews = Properties.Settings.Default.audioPreviews;
        public string beatmapMirror = Properties.Settings.Default.beatmapMirror;
        public string uiBackground = Properties.Settings.Default.customBgPath;
        public bool minimizeToTray = Properties.Settings.Default.minimizeToTray;
        public bool firstTrayMinimize = true;
        public bool launchOsu = Properties.Settings.Default.launchOsu;
        public bool useOfficialOsu = Properties.Settings.Default.useOfficialOsu;
        public string officialOsuCookies = Properties.Settings.Default.officialOsuCookies;
        public bool fallbackActualOsu = false;
        public string officialOsuUsername = Properties.Settings.Default.officialOsuUsername;
        public string officialOsuPassword = Properties.Settings.Default.officialOsuPassword;

        private System.Windows.Controls.Control[] dynamicElements;

        public MainWindow(string[] startupArgs)
        {
            InitializeComponent();
            dataGrid.ItemsSource = beatmaps;
            progressGrid.ItemsSource = DownloadManager.Downloads;
            DownloadManager.SpeedUpdated += DownloadManager_SpeedUpdated;

            System.Windows.Controls.Control[] _dynamicElements = { searchBox, searchButton, popularLoadButton, rankedStatusBox, modeSelectBox, progressGrid, dataGrid };
            dynamicElements = _dynamicElements;
            InitComboBoxes();

            // overlay mode window settings
            if (overlayMode)
            {
                Topmost = true;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                ShowInTaskbar = true;
                Opacity = 0.85;
                AllowsTransparency = true;
                overlayModeNotice.Visibility = Visibility.Visible;
                overlayModeExit.Visibility = Visibility.Visible;
            }

            if (useOfficialOsu)
                (new Dialogs.OsuLoginCheck(this)).ShowDialog();

            HandleURIArgs(startupArgs);
        }

        private bool limitSpeedUpdates = false; // slow down
        private async void DownloadManager_SpeedUpdated(DownloadManager.SpeedUpdatedEventArgs e)
        {
            if (limitSpeedUpdates && e.Speed != 0)
                return;
            downloadSpeedLabel.Content = $"Average download speed (per download): {e.Speed.ToString("0.0")}kB/s"; // 0.0 = one dp
            limitSpeedUpdates = true;
            await Task.Delay(500);
            limitSpeedUpdates = false;
        }

        public void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CleanUpOldTemps();
            CheckOrPromptForSongsDir();
            AudioManager.Init(Properties.Resources.doong); // load into memory ready to play
            MapsManager.Init(osuSongsFolder);

            if (!string.IsNullOrEmpty(uiBackground))
                SetFormCustomBackground(uiBackground);

            if (overlayMode)
            {
                // register hotkey, 2|4: CONTROL|SHIFT, 36: HOME
                HotkeyManager.Register(HotkeyManager.GetRuntimeHandle(this), 2 | 4, 36);
                // sub to hotkey press event
                HotkeyManager.HotkeyPressed += HotkeyPressed;
            }

            CheckForUpdates();
        }

        private async void searchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                searchingLoading.Visibility = Visibility.Visible;

                IEnumerable<BeatmapSet> _beatmaps;
                if (useOfficialOsu)
                {
                    _beatmaps = await Osu.Search(searchBox.Text,
                        (rankedStatusBox.SelectedItem as KVItem).Value,
                        (modeSelectBox.SelectedItem as KVItem).Value
                    );
                }
                else
                {
                    _beatmaps = await Bloodcat.Search(searchBox.Text,
                        (rankedStatusBox.SelectedItem as KVItem).Value,
                        (modeSelectBox.SelectedItem as KVItem).Value,
                        searchViaSelectBox.Visibility == Visibility.Hidden ? null : (searchViaSelectBox.SelectedItem as KVItem).Value
                    );
                }
                populateBeatmaps(_beatmaps);
            }
            catch (Osu.SearchNotSupportedException) { MessageBox.Show("Sorry, this mode of Ranking search is currently not supported via the official osu! servers."); }
            catch (Osu.CookiesExpiredException)
            {
                if (await TryRenewOsuCookies()) searchButton_Click(sender, e); // success, try at it again
                return;
            }
            catch (Exception ex) { MessageBox.Show("There was an error searching for beatmaps...\n\n" + ex.ToString()); }
            finally { searchingLoading.Visibility = Visibility.Hidden; }
        }

        private async void popularLoadButton_Click(object sender, RoutedEventArgs e)
        {
            // meh reset these to avoid confusion
            rankedStatusBox.SelectedIndex = 0;
            modeSelectBox.SelectedIndex = 0;

            try
            {
                searchingLoading.Visibility = Visibility.Visible;
                var beatmaps = await Bloodcat.Popular();
                populateBeatmaps(beatmaps);
            }
            catch (Exception ex) { MessageBox.Show("There was an error loading the popular beatmaps...\n\n" + ex.ToString()); }
            finally { searchingLoading.Visibility = Visibility.Hidden; }
        }

        private Regex onlyNumbersReg = new Regex(@"^\d+$");
        private void searchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // not for official osu search
            if (useOfficialOsu)
                return;

            // if only numbers show the search via, just like the bloodcat website
            if (onlyNumbersReg.IsMatch(searchBox.Text))
            {
                if (searchViaLabel.Visibility == Visibility.Hidden)
                {
                    searchViaLabel.Visibility = Visibility.Visible;
                    searchViaSelectBox.Visibility = Visibility.Visible;
                }
            }
            else if (searchViaLabel.Visibility == Visibility.Visible)
            {
                searchViaLabel.Visibility = Visibility.Hidden;
                searchViaSelectBox.Visibility = Visibility.Hidden;
            }
        }

        private void populateBeatmaps(IEnumerable<BeatmapSet> beatmapsData)
        {
            beatmaps.Clear();
            foreach (BeatmapSet beatmap in beatmapsData)
                beatmaps.Add(beatmap);
        }

        private void dataGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var beatmap = WinTools.GetGridViewSelectedRowItem<BeatmapSet>(sender, e);
            if (beatmap == null)
                return;

            DownloadBeatmapSet(beatmap);
        }

        private void dataGrid_LeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!audioPreviews)
                return;

            var set = WinTools.GetGridViewSelectedRowItem<BeatmapSet>(sender, e);
            if (set == null)
                return;

            Osu.PlayPreviewAudio(set);
        }

        private void dataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // shortcut to stop playing
            if (!audioPreviews)
                return;
            AudioManager.ForceStopPreview();
        }

        private void progressGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var download = WinTools.GetGridViewSelectedRowItem<BeatmapDownload>(sender, e);
            if (download == null)
                return;

            MessageBoxResult cancelPrompt = MessageBox.Show("Are you sure you wish to cancel the current download for: " + download.FriendlyName + "?", "NexDirect - Cancel Download", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (cancelPrompt == MessageBoxResult.No)
                return;

            DownloadManager.CancelDownload(download);
        }

        private void logoImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var settingsWindow = new Settings(this);
            settingsWindow.ShowDialog();
        }

        private void InitComboBoxes()
        {
            // Search filters
            rankedStatusBox.Items.Add(new KVItem("All", null));
            rankedStatusBox.Items.Add(new KVItem("Ranked / Approved", "1,2"));
            rankedStatusBox.Items.Add(new KVItem("Qualified", "3"));
            rankedStatusBox.Items.Add(new KVItem("Unranked", "0,-1,-2"));

            // Modes
            modeSelectBox.Items.Add(new KVItem("All", null));
            modeSelectBox.Items.Add(new KVItem("osu!", "0"));
            modeSelectBox.Items.Add(new KVItem("Catch the Beat", "2"));
            modeSelectBox.Items.Add(new KVItem("Taiko", "1"));
            modeSelectBox.Items.Add(new KVItem("osu!mania", "3"));

            // ID lookup thingys
            searchViaSelectBox.Items.Add(new KVItem("Beatmap Set ID (/s)", "s"));
            searchViaSelectBox.Items.Add(new KVItem("Beatmap ID (/b)", "b"));
            searchViaSelectBox.Items.Add(new KVItem("Mapper User ID", "u"));
            searchViaSelectBox.Items.Add(new KVItem("Normal (Title/Artist)", "o"));
        }

        public async void DownloadBeatmapSet(BeatmapSet set)
        {
            // check for already downloading
            if (DownloadManager.Downloads.Any(b => b.Set.Id == set.Id))
            {
                MessageBox.Show("This beatmap is already being downloaded!");
                return;
            }

            if (!CheckAndPromptIfHaveMap(set))
                return;

            // get dl obj
            BeatmapDownload download;
            if (!fallbackActualOsu && useOfficialOsu)
            {
                try
                {
                    download = await Osu.PrepareDownloadSet(set);
                }
                catch (Osu.IllegalDownloadException)
                {
                    MessageBoxResult bloodcatAsk = MessageBox.Show("Sorry, this map seems like it has been taken down from the official osu! servers due to a DMCA request to them. Would you like to check if a copy off Bloodcat is available, and if so download it?", "NexDirect - Mirror?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (bloodcatAsk == MessageBoxResult.No)
                        return;

                    BeatmapSet newSet = await Bloodcat.TryResolveSetId(set.Id);
                    if (newSet == null)
                        MessageBox.Show("Sorry, this map could not be found on Bloodcat. Download has been aborted.", "NexDirect - Could not find beatmap", MessageBoxButton.OK, MessageBoxImage.Error);

                    download = Bloodcat.PrepareDownloadSet(set, beatmapMirror);
                }
                catch (Osu.CookiesExpiredException)
                {
                    if (await TryRenewOsuCookies())
                        download = await Osu.PrepareDownloadSet(set);
                    return;
                }
            }
            else
            {
                download = Bloodcat.PrepareDownloadSet(set, beatmapMirror);
            }

            if (download == null)
                return;

            // start dl
            try
            {
                await DownloadManager.DownloadSet(download);

                if (launchOsu && Process.GetProcessesByName("osu!").Length > 0)
                {
                    string newPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, download.FileName);
                    File.Move(download.TempPath, newPath); // rename to .osz
                    Process.Start(Path.Combine(osuFolder, "osu!.exe"), newPath);
                }
                else
                {
                    string path = Path.Combine(osuSongsFolder, download.FileName);
                    if (File.Exists(path)) File.Delete(path); // overwrite if exist
                    File.Move(download.TempPath, path);
                }

                TrayManager.Pop($"Download complete: {set.Artist} - {set.Title} <{set.Mapper}>");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error has occured whilst downloading {set.Title} ({set.Mapper}).\n\n{ex.ToString()}");
            }
        }

        private async Task<bool> TryRenewOsuCookies()
        {
            try // try renew
            {
                System.Net.CookieContainer _cookies = await Osu.LoginAndGetCookie(officialOsuUsername, officialOsuPassword);
                Properties.Settings.Default.officialOsuCookies = officialOsuCookies = await Osu.SerializeCookies(_cookies);
                Osu.Cookies = _cookies;
                Properties.Settings.Default.Save(); // success
                return true;
            }
            catch (Osu.InvalidPasswordException)
            {
                MessageBoxResult fallback = MessageBox.Show("It seems like your osu! login password has changed. Press yes to fallback the session to Bloodcat for now and you can go update your password, or press no to permanently use Bloodcat. You will have to retry the download again either way.", "NexDirect - Download Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (fallback == MessageBoxResult.Yes)
                {
                    // just fallback
                    useOfficialOsu = false;
                    fallbackActualOsu = true;
                    return false;
                }
                else
                {
                    // disable perma
                    Properties.Settings.Default.useOfficialOsu = useOfficialOsu = false;
                    Properties.Settings.Default.officialOsuCookies = officialOsuCookies = null;
                    Properties.Settings.Default.officialOsuUsername = officialOsuUsername = "";
                    Properties.Settings.Default.officialOsuPassword = officialOsuPassword = "";
                    Properties.Settings.Default.Save();
                    return false;
                }
            }
        }

        private void CleanUpOldTemps()
        {
            try
            {
                string[] cleanup = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.nexd", SearchOption.TopDirectoryOnly);
                foreach (string c in cleanup)
                    File.Delete(c);
            }
            catch { } // dont really care as we are just getting rid of temp files, doesnt matter if it screws up
        }

        private const string osuRegKey = @"SOFTWARE\Classes\osu!";
        private Regex osuValueRegex = new Regex(@"""(.*)\\osu!\.exe"" ""%1""");
        public void CheckOrPromptForSongsDir()
        {
            bool newSetup = true;
            if (osuFolder == "forced_update")
            {
                newSetup = false;
                osuFolder = "";
            }
            else if (!string.IsNullOrEmpty(osuFolder))
            {
                // just verify this folder actually still exists
                if (Directory.Exists(osuFolder))
                    return;
                newSetup = false;
                osuFolder = "";
                MessageBox.Show("Your osu! songs folder seems to have moved... please reselect the new one!", "NexDirect - Folder Update");
            }
            else
            {
                MessageBox.Show("Welcome to NexDirect: the cheap man's o--!direct. (we don't name names here.)", "NexDirect - First Time Welcome");
                MessageBox.Show("It seems like it is your first time here. We will first try to auto-detect your osu! folder, but if we cannot find it, please point towards your osu! directory so we can get the beatmap download location set up.", "NexDirect - First Time Setup");
            }

            if (newSetup)
            {
                // attempt auto-detection via osu! uri registry keys
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(osuRegKey, false))
                {
                    if (key != null && key.GetValue("", "").ToString() == "osu! beatmap") // make sure key exists and the GetValue() of the default is indeed for osu! beatmap
                    {
                        using (RegistryKey cmdKey = key.OpenSubKey(@"shell\open\command"))
                        {
                            string osuValue;
                            if ((osuValue = (string)cmdKey.GetValue("", null)) != null)
                            {
                                Match m = osuValueRegex.Match(osuValue);
                                if (!String.IsNullOrEmpty(m.Value))
                                {
                                    string theFolder = m.Groups[1].ToString();
                                    MessageBoxResult mbr = MessageBox.Show($"We have attempted to auto-detect your osu! folder and seemed to have found it here:\n{theFolder}\n\nIs this the right path, and would you like to save it as the configuration?", "NexDirect - Found Path", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                    if (mbr == MessageBoxResult.Yes)
                                        osuFolder = theFolder;
                                }
                            }
                        }
                    }
                }
            }

            while (string.IsNullOrEmpty(osuFolder))
            {
                var dialog = new WPFFolderBrowserDialog();
                if (!(bool)dialog.ShowDialog())
                {
                    // ShowDialog returns false on no folder picked
                    MessageBox.Show("You need to select your osu! folder, please try again!", "NexDirect - Error");
                    continue;
                }

                if (!string.IsNullOrEmpty(dialog.FileName))
                {
                    if (File.Exists(Path.Combine(dialog.FileName, "osu!.exe")))
                        osuFolder = dialog.FileName;
                    else
                        MessageBox.Show("This does not seem like a valid osu! songs folder. Please try again.", "NexDirect - Error");
                }
                else
                {
                    MessageBox.Show("Could not detect your osu! folder being selected. Please try again.", "NexDirect - Error");
                }
            }

            Properties.Settings.Default.osuFolder = osuFolder;
            Properties.Settings.Default.Save();

            if (newSetup == true)
                MessageBox.Show("Welcome to NexDirect! Your folder has been registered and we are ready to go!", "NexDirect - Welcome");
            else
                MessageBox.Show("Your NexDirect osu! folder registration has been updated.", "NexDirect - Saved");
        }


        private bool CheckAndPromptIfHaveMap(BeatmapSet set)
        {
            // check for already have
            if (set.AlreadyHave)
            {
                MessageBoxResult prompt = MessageBox.Show($"You already have this beatmap {set.Title} ({set.Mapper}). Do you wish to redownload it?", "NexDirect - Cancel Download", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (prompt == MessageBoxResult.No)
                    return false;

                return true;
            }
            return true;
        }

        private bool uriHandling = false; // one at a time please
        private Regex uriReg = new Regex(@"nexdirect:\/\/(\d+)\/(b?)");
        public bool HandleURIArgs(IList<string> args)
        {
            if (args.Count < 1 || uriHandling)
                return false; // no args or handling
            string fullArgs = string.Join(" ", args);

            Match m = uriReg.Match(fullArgs);
            if (String.IsNullOrEmpty(m.Value)) // no match found
                return false;
            bool isSetId = m.Groups[2].ToString() != "b";

            Application.Current.Dispatcher.Invoke(async () =>
            {
                uriHandling = true;
                foreach (var element in dynamicElements)
                    element.IsEnabled = false;

                BeatmapSet set;
                if (isSetId)
                {
                    if (useOfficialOsu)
                        set = await Osu.TryResolveSetId(m.Groups[1].ToString());
                    else
                        set = await Bloodcat.TryResolveSetId(m.Groups[1].ToString());
                }
                else
                {
                    if (useOfficialOsu)
                        set = await Osu.TryResolveBeatmapId(m.Groups[1].ToString());
                    else
                        set = await Bloodcat.TryBeatmapId(m.Groups[1].ToString());
                }
                

                if (set == null)
                    MessageBox.Show($"Could not find the beatmap on {(useOfficialOsu ? "the official osu! directory" : "Bloodcat")}. Cannot proceed to download :(");
                else
                    (new Dialogs.DirectDownload(this, set)).ShowDialog();

                foreach (var element in dynamicElements)
                    element.IsEnabled = true;
                uriHandling = false;
            });

            return true;
        }
        
        public void SetFormCustomBackground(string inPath)
        {
            if (inPath == null)
            {
                SolidColorBrush myBrush = new SolidColorBrush();
                myBrush.Color = Color.FromRgb(255, 255, 255);
                Background = myBrush;
                foreach (var element in dynamicElements)
                    element.Opacity = 1;
                return;
            }

            try
            {
                Uri file = new Uri(inPath);
                // https://stackoverflow.com/questions/4009775/change-wpf-window-background-image-in-c-sharp-code
                ImageBrush myBrush = new ImageBrush();
                myBrush.ImageSource = new BitmapImage(file);
                myBrush.Stretch = Stretch.UniformToFill;
                Background = myBrush;
                foreach (var element in dynamicElements)
                    element.Opacity = 0.85;
            }
            catch { } // meh doesnt exist
        }

        private async void CheckForUpdates()
        {
            UpdateChecker.Update update = await UpdateChecker.Check(WinTools.GetGitStyleVersion(), UpdateChecker.Platform.Windows);
            if (update == null)
                return;

            MessageBoxResult downloadNew = MessageBox.Show($"There is a new update available for NexDirect (version {update.Version}).\nIt was published on GitHub at {update.PublishedAt.ToString("g")}.\n\nOpen your browser now to download the latest update?", "NexDirect - Update Available", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (downloadNew == MessageBoxResult.No)
                return;
            Process.Start(update.Url);
        }

        private void HotkeyPressed()
        {
            // toggle window visibility
            Visibility = IsVisible ? Visibility.Hidden : Visibility.Visible;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (overlayMode)
                HotkeyManager.Init(HotkeyManager.GetRuntimeHandle(this));
        }

        protected override void OnClosed(EventArgs e)
        {
            if (overlayMode)
                HotkeyManager.Unregister(HotkeyManager.GetRuntimeHandle(this)); // unload hotkey stuff

            // unload tray icon to prevent it sticking there
            TrayManager.Unload();
            base.OnClosed(e);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            TrayManager.Init();
            TrayManager.NotifyIconInteracted += (ex) =>
            {
                if (ex.Type == TrayManager.InteractionType.CtxExit)
                {
                    Application.Current.Shutdown();
                    return;
                }

                if (overlayMode)
                    HotkeyPressed();
                else
                    RestoreWindow();
            };

            if (overlayMode) // Always show and ready to pop
                TrayManager.Pop("Press CTRL+SHIFT+HOME to toggle the NexDirect overlay...", "NexDirect (Overlay Mode)");
        }

        private void overlayModeExit_MouseUp(object sender, MouseButtonEventArgs e) => Application.Current.Shutdown();

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (minimizeToTray)
            {
                if (WindowState == WindowState.Minimized)
                {
                    Hide();

                    if (firstTrayMinimize)
                    {
                        TrayManager.Icon.ShowBalloonTip(1500, "NexDirect", "NexDirect has been minimized to the tray. Double click the icon in the tray to restore.", System.Windows.Forms.ToolTipIcon.Info);
                        firstTrayMinimize = false;
                    }
                }
            }
        }

        public void RestoreWindow()
        {
            Show();
            WindowState = WindowState.Normal; // get outta minimize
            Activate(); // bring to front
        }
    }
}
