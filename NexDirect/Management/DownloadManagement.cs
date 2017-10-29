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
using NexDirectLib.Providers;
using NexDirectLib.Management;
using NexDirectLib.Util;

namespace NexDirect.Management
{
    static class DownloadManagement
    {
        static MainWindow mw;

        public static byte[] DownloadCompleteSound = Tools.StreamToByteArray(Properties.Resources.doong);

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
                if (SettingManager.Get("useOfficialOsu"))
                    set = await Osu.TryResolveSetId(id);
                else
                    set = await Bloodcat.TryResolveSetId(id);
            }
            else
            {
                if (SettingManager.Get("useOfficialOsu"))
                    set = await Osu.TryResolveBeatmapId(id);
                else
                    set = await Bloodcat.TryBeatmapId(id);
            }


            if (set == null)
            {
                MessageBox.Show($"Could not find the beatmap on {(SettingManager.Get("useOfficialOsu") ? "the official osu! directory" : "Bloodcat")}. Cannot proceed to download :(");
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
                System.Net.CookieContainer _cookies = await Osu.LoginAndGetCookie(SettingManager.Get("officialOsuUsername"), SettingManager.Get("officialOsuPassword"));
                SettingManager.Set("officialOsuCookies", await CookieStoreSerializer.SerializeCookies(_cookies));
                Osu.Cookies = _cookies;
                return true;
            }
            catch (Osu.InvalidPasswordException)
            {
                MessageBoxResult fallback = MessageBox.Show("It seems like your osu! login password has changed. Press yes to fallback the session to Bloodcat for now and you can go update your password, or press no to permanently use Bloodcat. You will have to retry the download again either way.", "NexDirect - Download Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (fallback == MessageBoxResult.Yes)
                {
                    // just fallback
                    SettingManager.Set("useOfficialOsu", false, true);
                    SettingManager.Set("fallbackActualOsu", true, true);
                    return false;
                }
                else
                {
                    // disable perma
                    SettingManager.Set("useOfficialOsu", false);
                    SettingManager.Set("officialOsuCookies", null);
                    SettingManager.Set("officialOsuUsername", "");
                    SettingManager.Set("officialOsuPassword", "");
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
            if (!String.IsNullOrEmpty(SettingManager.Get("beatmapMirror")))
            {
                // use mirror
                download = DownloadMirror.PrepareDownloadSet(set, SettingManager.Get("beatmapMirror"));
            }
            else if (!SettingManager.Get("fallbackActualOsu") && SettingManager.Get("useOfficialOsu"))
            {
                try
                {
                    download = await Osu.PrepareDownloadSet(set, SettingManager.Get("novidDownload"));
                }
                catch (Osu.IllegalDownloadException)
                {
                    MessageBoxResult bloodcatAsk = MessageBox.Show("Sorry, this map seems like it has been taken down from the official osu! servers due to a DMCA request to them. Would you like to check if a copy off Bloodcat is available, and if so download it?", "NexDirect - Mirror?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (bloodcatAsk == MessageBoxResult.No)
                        return;

                    BeatmapSet newSet = await Bloodcat.TryResolveSetId(set.Id);
                    if (newSet == null)
                    {
                        MessageBox.Show("Sorry, this map could not be found on Bloodcat. Download has been aborted.", "NexDirect - Could not find beatmap", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    download = await Bloodcat.PrepareDownloadSet(set);
                }
                catch (Osu.CookiesExpiredException)
                {
                    if (await TryRenewOsuCookies())
                        download = await Osu.PrepareDownloadSet(set, SettingManager.Get("novidDownload"));
                    else
                        return;
                }
            }
            else
            {
                try
                {
                    download = await Bloodcat.PrepareDownloadSet(set);
                }
                catch (Bloodcat.BloodcatCaptchaException)
                {
                    MessageBox.Show("It seems like Bloodcat has triggered CAPTCHA. Please click OK to verify and retry download...");
                    (new Dialogs.Captcha(set)).ShowDialog();

                    // persist those freshly baked cookies
                    SettingManager.Set("bloodcatCookies", await CookieStoreSerializer.SerializeCookies(Bloodcat.Cookies));

                    DownloadBeatmapSet(set); // hard retry
                    return;
                }
                
            }

            if (download == null)
                return;

            // start dl
            try
            {
                await DownloadManager.DownloadSet(download);
                if (download.Cancelled)
                    return;

                if (SettingManager.Get("launchOsu") && Process.GetProcessesByName("osu!").Length > 0)
                {
                    string newPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, download.FileName);
                    File.Move(download.TempPath, newPath); // rename to .osz
                    Process.Start(Path.Combine(SettingManager.Get("osuFolder"), "osu!.exe"), newPath);
                }
                else
                {
                    string path = Path.Combine(mw.osuSongsFolder, download.FileName);
                    if (File.Exists(path)) File.Delete(path); // overwrite if exist
                    File.Move(download.TempPath, path);
                }

                AudioManager.PlayWavBytes(DownloadCompleteSound, 0.85f);

                (new Dialogs.DownloadComplete(set)).Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error has occured whilst downloading {set.Title} ({set.Mapper}).\n\n{ex.ToString()}");
            }
        }
    }
}
