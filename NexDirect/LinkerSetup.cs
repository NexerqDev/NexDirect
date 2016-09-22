using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace NexDirect
{
    public static class LinkerSetup
    {
        public static void Setup()
        {
            string defaultBrowser;
            try { defaultBrowser = getDefaultBrowser(); }
            catch (Exception ex) { MessageBox.Show($"An error occured whilst getting the original browser...\n\n{ex.ToString()}"); return; }
            // persist the default browser to settings
            Properties.Settings.Default.linkerDefaultBrowser = defaultBrowser;
            Properties.Settings.Default.Save();

            try { setProgId(); }
            catch (Exception ex) { MessageBox.Show($"An error occured whilst registering the progid...\n\n{ex.ToString()}"); return; }

            try { registerCapabilities(); }
            catch (Exception ex) { MessageBox.Show($"An error occured whilst registering the capability...\n\n{ex.ToString()}"); return; }

            try { registerApp(); }
            catch (Exception ex) { MessageBox.Show($"An error occured whilst registering the app...\n\n{ex.ToString()}"); return; }

            MessageBox.Show("Successfully registered as a browser. You can now go to Default Programs in the Control Panel to set NexDirect as your default browser for the passthrough.");
        }

        private static Regex shellOpenRegex = new Regex("\"(.*?)\"");
        private static string getDefaultBrowser()
        {
            // get their default browser
            // fetch the set progid as default browser
            string progid;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice"))
                progid = key.GetValue("ProgId").ToString();

            // based on the progid retrieve the goods
            if (progid != "NexDirect.Url") // already setup but they might want to resetup, so skip this section if it already setup.
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\" + progid + @"\shell\open\command"))
                    return shellOpenRegex.Match(key.GetValue("").ToString()).Groups[1].ToString();
            else
                return null;
        }

        private static void setProgId()
        {
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Classes\NexDirect.Url"))
            {
                key.SetValue("", "NexDirect http passthrough");

                string appLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                using (RegistryKey shellOpenKey = key.CreateSubKey(@"shell\open\command"))
                    shellOpenKey.SetValue("", $"\"{appLocation}\" \"/link:%1\"");
            }
        }

        private static void registerCapabilities()
        {
            string appLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Clients\StartMenuInternet\NexDirect"))
            {
                key.SetValue("", "NexDirect");

                using (RegistryKey capibilitySubkey = key.CreateSubKey(@"Capabilities"))
                {
                    capibilitySubkey.SetValue("ApplicationDescription", "Intercepts browser requests so that relevant ones can be sent to NexDirect.");
                    capibilitySubkey.SetValue("ApplicationName", "NexDirect");

                    using (RegistryKey urlAssocKey = capibilitySubkey.CreateSubKey(@"URLAssociations"))
                    {
                        urlAssocKey.SetValue("http", "NexDirect.Url");
                        urlAssocKey.SetValue("https", "NexDirect.Url");
                    }
                }

                using (RegistryKey iconSubkey = key.CreateSubKey(@"DefaultIcon"))
                    iconSubkey.SetValue("", $"{appLocation},0");
            }
        }

        private static RegistryAccessRule _getWriteOnKey(RegistryKey key)
        {
            RegistrySecurity sec = key.GetAccessControl();

            RegistryAccessRule rule = new RegistryAccessRule(
                WindowsIdentity.GetCurrent().User,
                RegistryRights.WriteKey,
                AccessControlType.Allow);

            sec.AddAccessRule(rule);
            key.SetAccessControl(sec);
            return rule;
        }

        private static void _removeRuleOnKey(RegistryKey key, RegistryAccessRule rule)
        {
            RegistrySecurity sec = key.GetAccessControl();
            sec.RemoveAccessRule(rule);
            key.SetAccessControl(sec);
        }

        private static void registerApp()
        {
            // we dont have perms, we should grant then take away to avoid security risk
            // https://stackoverflow.com/questions/38448390/how-do-i-programmatically-give-ownership-of-a-registry-key-to-administrators
            RegistryAccessRule rule;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\RegisteredApplications", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.TakeOwnership))
                rule = _getWriteOnKey(key);

            // granted perms, reopen the key and we can write!
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\RegisteredApplications", RegistryKeyPermissionCheck.ReadWriteSubTree))
                key.SetValue("NexDirect", @"SOFTWARE\Clients\StartMenuInternet\NexDirect\Capabilities");

            // now we take away the perms again.
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\RegisteredApplications", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.TakeOwnership))
                _removeRuleOnKey(key, rule);
        }
    }
}
