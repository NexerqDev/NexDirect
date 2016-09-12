using CosmosKey.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NexDirectLinker
{
    class Program
    {
        // shell command import from the windows shell for default program updates
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern void SHChangeNotify(
            long wEventId,
            uint uFlags,
            string dwItem1,
            IntPtr dwItem2);

        static Regex osuReg = new Regex(@"^https?:\/\/osu\.ppy\.sh\/(s|b)\/(\d+)", RegexOptions.IgnoreCase);
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                InitialSetup();
                Environment.Exit(0);
            }

            String link = String.Join(" ", args);
            if (ProcessParent.ParentProcessUtilities.GetParentProcess().ProcessName != "osu!")
                RunBrowser(link);

            Match osuMatch = osuReg.Match(link);
            if (osuMatch.Success)
            {
                bool isSetId = osuMatch.Groups[1].ToString() == "s";
                string nexUrl = $"nexdirect://{osuMatch.Groups[2]}/" + (!isSetId ? "b" : "");
                RunOsu(nexUrl);
            }
            else
            {
                RunBrowser(link);
            }
        }

        static void RunOsu(string url)
        {
            string loc = TryGetNexdirectLocation();
            if (loc != null)
            {
                Process.Start(new ProcessStartInfo(loc, url));
                Environment.Exit(0);
            }
        }

        static void RunBrowser(string url)
        {
            Process.Start(new ProcessStartInfo("chrome.exe", url));
            Environment.Exit(0);
        }

        static string TryGetNexdirectLocation()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\nexdirect\DefaultIcon")) // we taking the easy way out
                    return key.GetValue("").ToString().Replace(",1", "");
            }
            catch
            {
                MessageBox.Show("You have not configured the URI handler in NexDirect itself! Please open NexDirect, open the settings menu and enable it before trying again.");
                Environment.Exit(1);
                return null;
            }
        }

        // consts for assocchanged shell call
        const long SHCNE_ASSOCCHANGED = 0x08000000;
        const uint SHCNF_DWORD = 3;
        const uint SHCNF_FLUSH = 0x1000;
        static void InitialSetup()
        {
            if (!UacHelper.IsProcessElevated)
            {
                MessageBox.Show("You need to re-run me as administrator to be able to set me up as default browser!");
                return;
            }

            MessageBoxResult m = MessageBox.Show("Are you sure you want to set up NexDirectLinker as your default browser?", "NexDirectLinker", MessageBoxButton.YesNo);
            if (m == MessageBoxResult.No)
                return;

            // try see if nexdirect even exists
            TryGetNexdirectLocation();

            // setup progids
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Classes\NexDirectLinker.Url"))
                {
                    key.SetValue("", "NexDirectLinker http interceptor");

                    //using (RegistryKey capibilitySubkey = key.CreateSubKey(@"Capabilities"))
                    //{
                    //    capibilitySubkey.SetValue("ApplicationDescription", "Intercepts browser requests so that relevant ones can be sent to NexDirect.");
                    //    capibilitySubkey.SetValue("ApplicationName", "NexDirectLinker");
                    //}

                    string appLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    using (RegistryKey shellOpenKey = key.CreateSubKey(@"shell\open\command"))
                        shellOpenKey.SetValue("", $"\"{appLocation}\" \"%1\"");
                }
            }
            catch (Exception ex) { MessageBox.Show($"An error occured whilst registering the progid...\n\n{ex.ToString()}"); return; }

            // setup capabilities itself
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Clients\StartMenuInternet\NexDirectLinker"))
                {
                    key.SetValue("", "NexDirectLinker");

                    using (RegistryKey capibilitySubkey = key.CreateSubKey(@"Capabilities"))
                    {
                        capibilitySubkey.SetValue("ApplicationDescription", "Intercepts browser requests so that relevant ones can be sent to NexDirect.");
                        capibilitySubkey.SetValue("ApplicationName", "NexDirectLinker");

                        using (RegistryKey urlAssocKey = capibilitySubkey.CreateSubKey(@"URLAssociations"))
                        {
                            urlAssocKey.SetValue("http", "NexDirectLinker.Url");
                            urlAssocKey.SetValue("https", "NexDirectLinker.Url");
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"An error occured whilst registering the capability...\n\n{ex.ToString()}"); return; }

            // setup as registered app
            bool fail = false;
            try
            {
                // we dont have perms, we should grant then take away to avoid security risk
                // https://stackoverflow.com/questions/38448390/how-do-i-programmatically-give-ownership-of-a-registry-key-to-administrators
                TokenManipulator.AddPrivilege("SeTakeOwnershipPrivilege");
                RegistryAccessRule rule = null;
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\RegisteredApplications", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.TakeOwnership))
                {
                    RegistrySecurity sec = key.GetAccessControl();
                    rule = new RegistryAccessRule(
                        WindowsIdentity.GetCurrent().User,
                        RegistryRights.WriteKey,
                        AccessControlType.Allow);
                    sec.AddAccessRule(rule);
                    key.SetAccessControl(sec);
                }

                // granted perms, reopen the key and we can write!
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\RegisteredApplications", RegistryKeyPermissionCheck.ReadWriteSubTree))
                    key.SetValue("NexDirectLinker", @"SOFTWARE\Clients\StartMenuInternet\NexDirectLinker\Capabilities");

                // now we take away the perms again.
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\RegisteredApplications", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.TakeOwnership))
                {
                    RegistrySecurity sec = key.GetAccessControl();
                    sec.RemoveAccessRule(rule);
                    key.SetAccessControl(sec);
                }
            }
            catch (Exception ex) { MessageBox.Show($"An error occured whilst registering the app...\n\n{ex.ToString()}"); fail = true; }
            finally { TokenManipulator.RemovePrivilege("SeTakeOwnershipPrivilege"); }
            if (fail)
                return;

            MessageBox.Show("OK");
            // prompt to change as default browser ~ https://msdn.microsoft.com/en-us/library/windows/desktop/cc144154(v=vs.85).aspx#install
            //SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_DWORD | SHCNF_FLUSH, null, IntPtr.Zero);
            //Thread.Sleep(350);
        }
    }
}
