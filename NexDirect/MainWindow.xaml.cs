using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using NAudio.Wave;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NexDirect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Import some hotkey stuff from user32.dll - once again have no idea how this stuff works but will try to understand soonTM
        // https://stackoverflow.com/questions/11377977/global-hotkeys-in-wpf-working-from-every-window
        [DllImport("User32.dll")]
        private static extern bool RegisterHotKey(
            [In] IntPtr hWnd,
            [In] int id,
            [In] uint fsModifiers,
            [In] uint vk);
        [DllImport("User32.dll")]
        private static extern bool UnregisterHotKey(
            [In] IntPtr hWnd,
            [In] int id);

        private HwndSource _source; // hotkey related ???
        public ObservableCollection<Structures.BeatmapSet> beatmaps = new ObservableCollection<Structures.BeatmapSet>(); // ObservableCollection: will send updates to other objects when updated (will update the datagrid binding)
        public ObservableCollection<Structures.BeatmapDownload> downloadProgress = new ObservableCollection<Structures.BeatmapDownload>();
        public WaveOut audioWaveOut = new WaveOut(); // For playing beatmap previews and stuff
        public WaveOut audioDoong = new WaveOut(); // Specific interface for playing doong, so if previews are playing it doesnt take over
        private System.Windows.Forms.NotifyIcon notifyIcon = null; // fullscreen overlay indicator
        public string[] alreadyDownloaded;
        public System.Net.CookieContainer officialCookieJar; // for official osu

        public string osuFolder = Properties.Settings.Default.osuFolder;
        public string osuSongsFolder { get { return Path.Combine(osuFolder, "Songs"); } }
        public bool overlayMode = Properties.Settings.Default.overlayMode;
        public bool audioPreviews = Properties.Settings.Default.audioPreviews;
        public string beatmapMirror = Properties.Settings.Default.beatmapMirror;
        public string uiBackground = Properties.Settings.Default.customBgPath;
        public bool launchOsu = Properties.Settings.Default.launchOsu;
        public bool useOfficialOsu = Properties.Settings.Default.useOfficialOsu;
        public string officialOsuCookies = Properties.Settings.Default.officialOsuCookies;
        public bool fallbackActualOsu = false;
        public string officialOsuUsername = Properties.Settings.Default.officialOsuUsername;
        public string officialOsuPassword = Properties.Settings.Default.officialOsuPassword;

        public MainWindow(string[] startupArgs)
        {
            InitializeComponent();
            dataGrid.ItemsSource = beatmaps;
            progressGrid.ItemsSource = downloadProgress;
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
            {
                (new Dialogs.OsuLoginCheck(this)).ShowDialog();
            }

            HandleURIArgs(startupArgs);
        }


        public void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CleanUpOldTemps();
            CheckOrPromptForSongsDir();
            LoadDoongPlayer(); // load into memory ready to play
            ReloadAlreadyDownloadedMaps();

            if (!string.IsNullOrEmpty(uiBackground))
            {
                SetFormCustomBackground(uiBackground);
            }

            if (overlayMode)
            {
                // register hotkey -- ID: 9000, 2|4: CONTROL|SHIFT, 36: HOME
                RegisterHotKey((new WindowInteropHelper(this)).Handle, 9000, 2 | 4, 36);
            }
        }

        // Class for the selectboxes. Key/Value item so can just access the SelectedItem's value
        private class KVItem
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public KVItem(string k, string v)
            {
                Key = k;
                Value = v;
            }

            public override string ToString()
            {
                return Key;
            }
        }

        private async void searchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                searchingLoading.Visibility = Visibility.Visible;

                dynamic _beatmaps;
                if (useOfficialOsu)
                {
                    var beatmapsData = await Osu.Search(this,
                        searchBox.Text,
                        (rankedStatusBox.SelectedItem as KVItem).Value,
                        (modeSelectBox.SelectedItem as KVItem).Value
                    );
                    _beatmaps = beatmapsData;
                }
                else
                {
                    var beatmapsData = await Bloodcat.Search(searchBox.Text,
                        (rankedStatusBox.SelectedItem as KVItem).Value,
                        (modeSelectBox.SelectedItem as KVItem).Value,
                        searchViaSelectBox.Visibility == Visibility.Hidden ? null : (searchViaSelectBox.SelectedItem as KVItem).Value
                    );
                    _beatmaps = new List<Structures.BeatmapSet>();
                    foreach (JObject b in beatmapsData) _beatmaps.Add(Bloodcat.StandardizeToSetStruct(this, b));
                }
                populateBeatmaps(_beatmaps);
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
                var beatmapsData = await Bloodcat.Popular();
                var _beatmaps = new List<Structures.BeatmapSet>();
                foreach (JObject b in beatmapsData) _beatmaps.Add(Bloodcat.StandardizeToSetStruct(this, b));
                populateBeatmaps(_beatmaps);
            }
            catch (Exception ex) { MessageBox.Show("There was an error loading the popular beatmaps...\n\n" + ex.ToString()); }
            finally { searchingLoading.Visibility = Visibility.Hidden; }
        }

        private Regex onlyNumbersReg = new Regex(@"^\d+$");
        private void searchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // not for official osu search
            if (useOfficialOsu) return;

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

        private void populateBeatmaps(IEnumerable<Structures.BeatmapSet> beatmapsData)
        {
            beatmaps.Clear();
            foreach (Structures.BeatmapSet beatmap in beatmapsData)
            {
                beatmaps.Add(beatmap);
            }
        }

        private void dataGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = Tools.getGridViewSelectedRowItem(sender, e);
            if (row == null) return;
            var beatmap = row as Structures.BeatmapSet;

            DownloadBeatmapSet(beatmap, false);
        }

        private async void dataGrid_LeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!audioPreviews) return;

            var row = Tools.getGridViewSelectedRowItem(sender, e);
            if (row == null) return;
            var set = row as Structures.BeatmapSet;

            audioWaveOut.Stop(); // if already playing something just stop it
            await Task.Delay(150);
            if (downloadProgress.Any(d => d.BeatmapSetId == set.Id)) return; // check for if already d/l'ing overlaps
            Osu.PlayPreviewAudio(set, audioWaveOut);
        }

        private void dataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // shortcut to stop playing
            if (!audioPreviews) return;
            audioWaveOut.Stop();
        }

        private void progressGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = Tools.getGridViewSelectedRowItem(sender, e);
            if (row == null) return;
            var download = row as Structures.BeatmapDownload;

            MessageBoxResult cancelPrompt = MessageBox.Show("Are you sure you wish to cancel the current download for: " + download.BeatmapSetName + "?", "NexDirect - Cancel Download", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (cancelPrompt == MessageBoxResult.No) return;

            Web.CancelDownload(download);
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

        public async void DownloadBeatmapSet(Structures.BeatmapSet set, bool forcedBloodcat)
        {
            // check for already downloading
            if (downloadProgress.Any(b => b.BeatmapSetId == set.Id))
            {
                MessageBox.Show("This beatmap is already being downloaded!");
                return;
            }

            if (!forcedBloodcat && !CheckAndPromptIfHaveMap(set)) return;
            else
            {
                // check
                var resolvedSet = await Bloodcat.ResolveSetId(this, set.Id);
                if (resolvedSet == null)
                {
                    MessageBox.Show("Could not find the beatmap on Bloodcat. Cannot proceed to download :(");
                    return;
                }
            }

            // start dl
            if (useOfficialOsu && !forcedBloodcat)
            {
                await Osu.DownloadSet(this, set, downloadProgress, osuFolder, audioDoong, launchOsu);
            }
            else
            {
                await Bloodcat.DownloadSet(set, beatmapMirror, downloadProgress, osuFolder, audioDoong, launchOsu);
            }
            
            if (downloadProgress.Count < 1) ReloadAlreadyDownloadedMaps(); // reload only when theres nothing left
        }

        private void CleanUpOldTemps()
        {
            try
            {
                string[] cleanup = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.nexd", SearchOption.TopDirectoryOnly);
                foreach (string c in cleanup)
                {
                    File.Delete(c);
                }
            }
            catch { } // dont really care as we are just getting rid of temp files, doesnt matter if it screws up
        }

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
                if (Directory.Exists(osuFolder)) return;
                newSetup = false;
                osuFolder = "";
                MessageBox.Show("Your osu! songs folder seems to have moved... please reselect the new one!", "NexDirect - Folder Update");
            }
            else
            {
                MessageBox.Show("Welcome to NexDirect: the cheap man's o--!direct. (we don't name names here.)", "NexDirect - First Time Welcome");
                MessageBox.Show("It seems like it is your first time here. Please point towards your osu! directory so we can get the beatmap download location set up.", "NexDirect - First Time Setup");
            }

            while (string.IsNullOrEmpty(osuFolder))
            {
                var dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;
                CommonFileDialogResult result = dialog.ShowDialog();

                try
                {
                    if (!string.IsNullOrEmpty(dialog.FileName))
                    {
                        if (File.Exists(Path.Combine(dialog.FileName, "osu!.exe")))
                        {
                            osuFolder = dialog.FileName;
                        }
                        else
                        {
                            MessageBox.Show("This does not seem like a valid osu! songs folder. Please try again.", "NexDirect - Error");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Could not detect your osu! folder being selected. Please try again.", "NexDirect - Error");
                    }
                }
                catch // catch thrown error on dialog.FileName access when nothing selected
                {
                    MessageBox.Show("You need to select your osu! folder, please try again!", "NexDirect - Error");
                }
            }

            Properties.Settings.Default.osuFolder = osuFolder;
            Properties.Settings.Default.Save();

            if (newSetup == true) MessageBox.Show("Welcome to NexDirect! Your folder has been registered and we are ready to go!", "NexDirect - Welcome");
            else MessageBox.Show("Your NexDirect osu! folder registration has been updated.", "NexDirect - Saved");
        }

        public void ReloadAlreadyDownloadedMaps()
        {
            alreadyDownloaded = Directory.GetDirectories(osuSongsFolder);
        }

        private bool CheckAndPromptIfHaveMap(Structures.BeatmapSet set)
        {
            // check for already have
            if (set.AlreadyHave)
            {
                MessageBoxResult prompt = MessageBox.Show(string.Format("You already have this beatmap {0} ({1}). Do you wish to redownload it?", set.Title, set.Mapper), "NexDirect - Cancel Download", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (prompt == MessageBoxResult.No) return false;

                return true;
            }
            return true;
        }

        private void LoadDoongPlayer()
        {
            var reader = new WaveFileReader(Properties.Resources.doong);
            audioDoong.Init(reader);
            audioDoong.PlaybackStopped += (o, e) => reader.Position = 0;
        }

        Regex uriReg = new Regex(@"nexdirect:\/\/(\d+)\/");
        public void HandleURIArgs(IList<string> args)
        {
            if (args.Count < 1) return; // no args
            string fullArgs = string.Join(" ", args);

            Match m = uriReg.Match(fullArgs);
            Application.Current.Dispatcher.Invoke(async () =>
            {
                Structures.BeatmapSet set;
                if (useOfficialOsu)
                {
                    set = await Osu.ResolveSetId(this, m.Groups[1].ToString());
                    if (set == null)
                    {
                        MessageBox.Show("Could not find the beatmap on the official osu! directory. Cannot proceed to download :(");
                        return;
                    }
                }
                else
                {
                    set = await Bloodcat.ResolveSetId(this, m.Groups[1].ToString());
                    if (set == null)
                    {
                        MessageBox.Show("Could not find the beatmap on Bloodcat. Cannot proceed to download :(");
                        return;
                    }
                }
                MessageBoxResult confirmPrompt = MessageBox.Show(string.Format("Are you sure you wish to download: {0} - {1} (mapped by {2})?", set.Artist, set.Title, set.Mapper), "NexDirect - Confirm Download", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirmPrompt == MessageBoxResult.No) return;
                DownloadBeatmapSet(set, false);
            });
        }
        
        public void SetFormCustomBackground(string inPath)
        {
            dynamic[] changedElements = { searchBox, searchButton, popularLoadButton, rankedStatusBox, modeSelectBox, progressGrid, dataGrid };

            if (inPath == null)
            {
                SolidColorBrush myBrush = new SolidColorBrush();
                myBrush.Color = Color.FromRgb(255, 255, 255);
                Background = myBrush;
                foreach (var element in changedElements)
                {
                    element.Opacity = 1;
                }
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
                foreach (var element in changedElements)
                {
                    element.Opacity = 0.85;
                }
            }
            catch { } // meh doesnt exist
        }

        private void HotkeyPressed()
        {
            // toggle window visibility
            Visibility = IsVisible ? Visibility.Hidden : Visibility.Visible;
        }

        // hotkey stuff
        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312) // WM_HOTKEY thing
            {
                if (wParam.ToInt32() == 9000) // hotkey ID -- we look for 9000
                {
                    HotkeyPressed();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (overlayMode)
            {
                // hotkey init stuff
                var helper = new WindowInteropHelper(this);
                _source = HwndSource.FromHwnd(helper.Handle);
                _source.AddHook(HwndHook);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (overlayMode)
            {
                // unload hotkey stuff
                _source.RemoveHook(HwndHook);
                _source = null;
                UnregisterHotKey((new WindowInteropHelper(this)).Handle, 0);

                // unload tray icon to prevent it sticking there
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }
            base.OnClosed(e);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            if (overlayMode)
            {
                // https://stackoverflow.com/questions/1472633/wpf-application-that-only-has-a-tray-icon
                notifyIcon = new System.Windows.Forms.NotifyIcon();
                notifyIcon.DoubleClick += (o, e1) => { HotkeyPressed(); }; // it is like pressing the hotkey
                notifyIcon.Icon = Properties.Resources.logo;
                notifyIcon.Text = "NexDirect (Overlay Mode)";
                notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[]
                {
                    new System.Windows.Forms.MenuItem("Show", (o, e1) => { HotkeyPressed(); }),
                    new System.Windows.Forms.MenuItem("E&xit", (o, e1) => { Application.Current.Shutdown(); })
                });
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(1500, "NexDirect (Overlay Mode)", "Press CTRL+SHIFT+HOME to toggle the NexDirect overlay...", System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        private void overlayModeExit_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
