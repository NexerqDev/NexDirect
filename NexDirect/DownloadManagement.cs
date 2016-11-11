using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NexDirectLib;
using NexDirectLib.Structures;
using System.Windows;
using System.Diagnostics;
using System.IO;

namespace NexDirect
{
    static class DownloadManagement
    {
        static MainWindow mw;

        public static void Init(MainWindow _mw)
        {
            mw = _mw;
        }

        private static bool checkAndPromptIfHaveMap(BeatmapSet set)
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

        public static async Task<bool> DirectDownload(bool isSetId, string id)
        {
            BeatmapSet set;
            if (isSetId)
            {
                if (mw.useOfficialOsu)
                    set = await Osu.TryResolveSetId(id);
                else
                    set = await Bloodcat.TryResolveSetId(id);
            }
            else
            {
                if (mw.useOfficialOsu)
                    set = await Osu.TryResolveBeatmapId(id);
                else
                    set = await Bloodcat.TryBeatmapId(id);
            }


            if (set == null)
            {
                MessageBox.Show($"Could not find the beatmap on {(mw.useOfficialOsu ? "the official osu! directory" : "Bloodcat")}. Cannot proceed to download :(");
                return false;
            }
            else
            {
                (new Dialogs.DirectDownload(mw, set)).ShowDialog();
                return true;
            }
        }

        public static async Task<bool> TryRenewOsuCookies()
        {
            try // try renew
            {
                System.Net.CookieContainer _cookies = await Osu.LoginAndGetCookie(mw.officialOsuUsername, mw.officialOsuPassword);
                Properties.Settings.Default.officialOsuCookies = mw.officialOsuCookies = await Osu.SerializeCookies(_cookies);
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
                    mw.useOfficialOsu = false;
                    mw.fallbackActualOsu = true;
                    return false;
                }
                else
                {
                    // disable perma
                    Properties.Settings.Default.useOfficialOsu = mw.useOfficialOsu = false;
                    Properties.Settings.Default.officialOsuCookies = mw.officialOsuCookies = null;
                    Properties.Settings.Default.officialOsuUsername = mw.officialOsuUsername = "";
                    Properties.Settings.Default.officialOsuPassword = mw.officialOsuPassword = "";
                    Properties.Settings.Default.Save();
                    return false;
                }
            }
        }

        public static async void DownloadBeatmapSet(BeatmapSet set)
        {
            // check for already downloading
            if (DownloadManager.Downloads.Any(b => b.Set.Id == set.Id))
            {
                MessageBox.Show("This beatmap is already being downloaded!");
                return;
            }

            if (!checkAndPromptIfHaveMap(set))
                return;

            // get dl obj
            BeatmapDownload download;
            if (!mw.fallbackActualOsu && mw.useOfficialOsu && String.IsNullOrEmpty(mw.beatmapMirror))
            {
                try
                {
                    download = await Osu.PrepareDownloadSet(set, mw.novidDownload);
                }
                catch (Osu.IllegalDownloadException)
                {
                    MessageBoxResult bloodcatAsk = MessageBox.Show("Sorry, this map seems like it has been taken down from the official osu! servers due to a DMCA request to them. Would you like to check if a copy off Bloodcat is available, and if so download it?", "NexDirect - Mirror?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (bloodcatAsk == MessageBoxResult.No)
                        return;

                    BeatmapSet newSet = await Bloodcat.TryResolveSetId(set.Id);
                    if (newSet == null)
                        MessageBox.Show("Sorry, this map could not be found on Bloodcat. Download has been aborted.", "NexDirect - Could not find beatmap", MessageBoxButton.OK, MessageBoxImage.Error);

                    download = Bloodcat.PrepareDownloadSet(set, mw.beatmapMirror);
                }
                catch (Osu.CookiesExpiredException)
                {
                    if (await TryRenewOsuCookies())
                        download = await Osu.PrepareDownloadSet(set, mw.novidDownload);
                    return;
                }
            }
            else
            {
                download = Bloodcat.PrepareDownloadSet(set, mw.beatmapMirror);
            }

            if (download == null)
                return;

            // start dl
            try
            {
                await DownloadManager.DownloadSet(download);
                if (download.Cancelled)
                    return;

                if (mw.launchOsu && Process.GetProcessesByName("osu!").Length > 0)
                {
                    string newPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, download.FileName);
                    File.Move(download.TempPath, newPath); // rename to .osz
                    Process.Start(Path.Combine(mw.osuFolder, "osu!.exe"), newPath);
                }
                else
                {
                    string path = Path.Combine(mw.osuSongsFolder, download.FileName);
                    if (File.Exists(path)) File.Delete(path); // overwrite if exist
                    File.Move(download.TempPath, path);
                }

                (new Dialogs.DownloadComplete(set)).Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error has occured whilst downloading {set.Title} ({set.Mapper}).\n\n{ex.ToString()}");
            }
        }
    }
}
