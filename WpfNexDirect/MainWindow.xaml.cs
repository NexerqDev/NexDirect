using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.ComponentModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using NAudio.Wave;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Web;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;

// TODO
// Multiple same beatmap d/l -- DONE
// Cancel download -- DONE
// Beatmap download completion sound -- DONE
// Beatmap audio preview -- DONE
// Search by ranked status -- DONE
// Overlay interface -- DONE
// Settings pane to toggle that stuff -- DONE
// URI + Userscript
// Username/password official website d/l or mirror field -- MIRROR DONE
// Already downloaded maps -- DONE

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
        private ObservableCollection<BeatmapSet> beatmaps = new ObservableCollection<BeatmapSet>(); // ObservableCollection: will send updates to other objects when updated (will update the datagrid binding)
        private ObservableCollection<DownloadingStatus> downloadProgress = new ObservableCollection<DownloadingStatus>();
        private WaveOut audioWaveOut = new WaveOut(); // For playing beatmap previews and stuff
        private WaveOut audioDoong = new WaveOut(); // Specific interface for playing doong, so if previews are playing it doesnt take over
        private string[] alreadyDownloaded;
        public string osuFolder = Properties.Settings.Default.osuFolder;
        public bool overlayMode = Properties.Settings.Default.overlayMode;
        public bool audioPreviews = Properties.Settings.Default.audioPreviews;
        public string beatmapMirror = Properties.Settings.Default.beatmapMirror;
        public string uiBackground = Properties.Settings.Default.customBgPath;
        public bool launchOsu = Properties.Settings.Default.launchOsu;

        public MainWindow()
        {
            InitializeComponent();
            dataGrid.ItemsSource = beatmaps;
            progressGrid.ItemsSource = downloadProgress;

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

            // test grid row
            //var beatmap = new BeatmapSet();
            //beatmap.Artist = "Reol";
            //beatmap.Title = "No Title";
            //beatmap.Mapper = "Nexerq";
            //beatmap.RankingStatus = "Ranked";
            //beatmap.AlreadyHave = false;
            //beatmaps.Add(beatmap);
        }

        public class BeatmapSet
        {
            public string Id { get; set; }
            public string Artist { get; set; }
            public string Title { get; set; }
            public string Mapper { get; set; }
            public string RankingStatus { get; set; }
            public bool AlreadyHave { get; set; }
            public Uri PreviewImage { get; set; }
            public JObject BloodcatData { get; set; }
        }

        // i dont even 100% know how this notifypropertychanged works
        // but i get why i need it i guess
        // https://stackoverflow.com/questions/5051530/wpf-gridview-not-updating-on-observable-collection-change
        private class DownloadingStatus : INotifyPropertyChanged
        {
            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;

            protected void Notify(string propName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propName));
                }
            }
            #endregion

            private string _percent;

            public string BeatmapSetName { get; set; }
            public string ProgressPercent
            {
                get { return _percent; }
                set
                {
                    _percent = value;
                    Notify("ProgressPercent");
                }
            }
            public string BeatmapSetId { get; set; }
            public WebClient DownloadClient { get; set; }
            public string TempDownloadPath { get; set; }
            public bool DownloadCancelled { get; set; }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            cleanUpOldTemps();
            checkOrPromptSongsDir();
            loadDoong(); // load into memory ready to play
            loadAlreadyDownloadedMaps();

            if (!string.IsNullOrEmpty(uiBackground))
            {
                setCustomBackground(uiBackground);
            }

            if (overlayMode)
            {
                // register hotkey -- ID: 9000, 2|4: CONTROL|SHIFT, 36: HOME
                RegisterHotKey((new WindowInteropHelper(this)).Handle, 9000, 2 | 4, 36);
            }
        }

        private async void searchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                searchingLoading.Visibility = Visibility.Visible;
                var beatmapsData = await getBloodcatSearch<JArray>(searchBox.Text, rankedStatusBox.Text.ToString(), modeSelectBox.Text.ToString());
                populateBeatmaps(beatmapsData);
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
                var beatmapsData = await getBloodcatPopular<JArray>();
                populateBeatmaps(beatmapsData);
            }
            catch (Exception ex) { MessageBox.Show("There was an error loading the popular beatmaps...\n\n" + ex.ToString()); }
            finally { searchingLoading.Visibility = Visibility.Hidden; }
        }

        private void populateBeatmaps(JArray beatmapsData)
        {
            beatmaps.Clear();
            foreach (JObject beatmapData in beatmapsData)
            {
                var beatmapSet = new BeatmapSet();
                beatmapSet.Id = (string)beatmapData.Property("id").Value;
                beatmapSet.Artist = (string)beatmapData.Property("artist").Value;
                beatmapSet.Title = (string)beatmapData.Property("title").Value;
                beatmapSet.Mapper = (string)beatmapData.Property("creator").Value;
                beatmapSet.RankingStatus = Tools.resolveRankingStatus((string)beatmapData.Property("status").Value);
                beatmapSet.PreviewImage = new Uri(string.Format("http://b.ppy.sh/thumb/{0}l.jpg", (string)beatmapData.Property("id").Value));
                beatmapSet.AlreadyHave = alreadyDownloaded.Any(b => b.Contains(beatmapSet.Id + " ")); // .StartsWith doesnt work as intended for some reason
                beatmapSet.BloodcatData = beatmapData;

                beatmaps.Add(beatmapSet);
            }
        }

        private async void dataGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = Tools.getGridViewSelectedRowItem(sender, e);
            if (row == null) return;
            var beatmap = row as BeatmapSet;

            // check for already downloading
            if (downloadProgress.Any(b => b.BeatmapSetId == beatmap.Id))
            {
                MessageBox.Show("This beatmap is already being downloaded!");
                return;
            }

            // check for already have
            if (beatmap.AlreadyHave)
            {
                MessageBoxResult prompt = MessageBox.Show(string.Format("You already have this beatmap {0} ({1}). Do you wish to redownload it?", beatmap.Title, beatmap.Mapper), "NexDirect - Cancel Download", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (prompt == MessageBoxResult.No) return;
            }

            // start dl
            await downloadBloodcatSet(beatmap);
        }

        private async void dataGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!audioPreviews) return;

            var row = Tools.getGridViewSelectedRowItem(sender, e);
            if (row == null) return;
            var beatmap = row as BeatmapSet;

            await playPreviewAudio(beatmap);
        }

        private void dataGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            audioWaveOut.Stop(); // crap workaround
        }

        private void progressGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = Tools.getGridViewSelectedRowItem(sender, e);
            if (row == null) return;
            var download = row as DownloadingStatus;

            MessageBoxResult cancelPrompt = MessageBox.Show("Are you sure you wish to cancel the current download for: " + download.BeatmapSetName + "?", "NexDirect - Cancel Download", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (cancelPrompt == MessageBoxResult.No) return;

            cancelDownload(download);
        }

        private void logoImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var settingsWindow = new Settings(this);
            settingsWindow.ShowDialog();
        }

        private void cleanUpOldTemps()
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

        public void checkOrPromptSongsDir()
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

                if (!string.IsNullOrEmpty(dialog.FileName))
                {
                    if (File.Exists(Path.Combine(dialog.FileName, "osu!.exe")))
                    {
                        osuFolder = Path.Combine(dialog.FileName, "Songs");
                    }
                }
                else
                {
                    MessageBox.Show("This does not seem like a valid osu! songs folder. Please try again.", "NexDirect - Error");
                }
            }

            Properties.Settings.Default.osuFolder = osuFolder;
            Properties.Settings.Default.Save();

            if (newSetup == true) MessageBox.Show("Welcome to NexDirect! Your folder has been registered and we are ready to go!", "NexDirect - Welcome");
            else MessageBox.Show("Your NexDirect osu! folder registration has been updated.", "NexDirect - Saved");
        }

        private void loadAlreadyDownloadedMaps()
        {
            alreadyDownloaded = Directory.GetDirectories(osuFolder);
        }

        private async Task<T> getBloodcatSearch<T>(string query, string selectRanked, string selectMode)
        {
            // build query string -- https://stackoverflow.com/questions/17096201/build-query-string-for-system-net-httpclient-get
            var qs = HttpUtility.ParseQueryString(string.Empty);
            qs["mod"] = "json";
            qs["q"] = query;
            if (selectRanked != "All") qs["s"] = Tools.resolveRankingComboBox(selectRanked);
            if (selectMode != "All") qs["m"] = Tools.resolveModeComboBox(selectMode);

            return await getJson<T>("http://bloodcat.com/osu/?" + qs.ToString());
        }

        private async Task<T> getBloodcatPopular<T>()
        {
            return await getJson<T>("http://bloodcat.com/osu/popular.php?mod=json");
        }

        private async Task<T> getJson<T>(string url)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string body = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(body);
            }
        }

        private async Task downloadBloodcatSet(BeatmapSet set)
        {
            Uri downloadUri;
            if (string.IsNullOrEmpty(beatmapMirror))
            {
                downloadUri = new Uri("http://bloodcat.com/osu/s/" + set.Id);
            }
            else
            {
                downloadUri = new Uri(beatmapMirror.Replace("%s", set.Id));
            }
            
            string filename = Tools.sanitizeFilename(string.Format("{0} {1} - {2}.osz", set.Id, set.Artist, set.Title));

            var progressObj = new DownloadingStatus();
            progressObj.BeatmapSetId = set.Id;
            progressObj.BeatmapSetName = string.Format("{0} ({1})", set.Title, set.Mapper);
            progressObj.ProgressPercent = "0";
            progressObj.TempDownloadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename + ".nexd");
            downloadProgress.Add(progressObj);

            using (var client = new WebClient())
            {
                client.DownloadProgressChanged += (o, e) =>
                {
                    progressObj.ProgressPercent = e.ProgressPercentage.ToString();
                };
                client.DownloadFileCompleted += (o, e) =>
                {
                    if (e.Cancelled)
                    {
                        File.Delete(progressObj.TempDownloadPath);
                        return;
                    }

                    if (launchOsu && Process.GetProcessesByName("osu!").Length > 0) // https://stackoverflow.com/questions/262280/how-can-i-know-if-a-process-is-running - ensure osu! is running dont want to just launch the game lol
                    {
                        string newPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
                        File.Move(progressObj.TempDownloadPath, newPath); // rename to .osz
                        Process.Start(Path.Combine(osuFolder, "osu!.exe"), newPath);
                    }
                    else
                    {
                        File.Move(progressObj.TempDownloadPath, Path.Combine(osuFolder, "Songs", filename));
                    }
                    
                    audioDoong.Play();
                };
                progressObj.DownloadClient = client;

                try { await client.DownloadFileTaskAsync(downloadUri, progressObj.TempDownloadPath); } // appdomain.etc is a WPF way of getting startup dir... stupid :(
                catch (Exception ex)
                {
                    if (progressObj.DownloadCancelled == true) return;
                    MessageBox.Show(string.Format("An error has occured whilst downloading {0} ({1}).\n\n{2}", set.Title, set.Mapper, ex.ToString()));
                }
                finally
                {
                    downloadProgress.Remove(progressObj);
                    if (downloadProgress.Count < 1) loadAlreadyDownloadedMaps(); // reload only when theres nothing left
                }
            }
        }

        private void cancelDownload(DownloadingStatus statusObj)
        {
            statusObj.DownloadCancelled = true;
            statusObj.DownloadClient.CancelAsync();
        }

        private void loadDoong()
        {
            var reader = new WaveFileReader(Properties.Resources.doong);
            audioDoong.Init(reader);
            audioDoong.PlaybackStopped += (o, e) => reader.Position = 0;
        }

        private async Task<System.Drawing.Image> getPreviewImage(string setId)
        {
            try
            {
                using (var client = new WebClient())
                {
                    var response = await client.DownloadDataTaskAsync(string.Format("http://b.ppy.sh/thumb/{0}l.jpg", setId));
                    return System.Drawing.Image.FromStream(new MemoryStream(response));
                }
            }
            catch { }

            return null;
        }

        private async Task playPreviewAudio(BeatmapSet set)
        {
            audioWaveOut.Stop(); // if already playing something just stop it

            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync("http://b.ppy.sh/preview/" + set.Id + ".mp3");
                    response.EnsureSuccessStatusCode();
                    Stream audioData = await response.Content.ReadAsStreamAsync();

                    // https://stackoverflow.com/questions/2488426/how-to-play-a-mp3-file-using-naudio sol #2
                    var reader = new Mp3FileReader(audioData);
                    audioWaveOut.Init(reader);
                    audioWaveOut.Stop(); // in case spam
                    audioWaveOut.Play();
                }
                catch { } // meh audio previews arent that important, and sometimes they dont exist
            }
        }
        
        public void setCustomBackground(string inPath)
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
            }
            base.OnClosed(e);
        }

        private void overlayModeExit_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
