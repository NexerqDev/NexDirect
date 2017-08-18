using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexDirect.Management
{
    static class SettingManager
    {
        public static bool fallbackActualOsu = false;

        private static Dictionary<string, Setting> store = new Dictionary<string, Setting>();

        public static string[] Keys => store.Keys.ToArray();

        static SettingManager()
        {
            // https://stackoverflow.com/questions/5872994/c-sharp-how-to-loop-through-properties-settings-default-properties-changing-the
            foreach (SettingsProperty prop in Properties.Settings.Default.Properties)
            {
                dynamic data = Properties.Settings.Default[prop.Name];
                dynamic s = new Setting(false, prop.Name, data);

                store.Add(prop.Name, s);
            }
        }

        private class Setting
        {
            public bool onlyInMemory;
            public string name;
            public dynamic data;

            public Setting(bool inMemory, string name, dynamic data)
            {
                onlyInMemory = inMemory;
                this.name = name;
                this.data = data;
            }

            public dynamic Get() { return data; }

            public void Set(dynamic newData, bool tempInMemory = false)
            {
                data = newData;

                if (!onlyInMemory && !tempInMemory)
                {
                    Properties.Settings.Default[name] = newData;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public static dynamic Get(string key, dynamic defaultValue = null)
        {
            if (defaultValue == null)
                defaultValue = false;
            if (!store.ContainsKey(key))
                return defaultValue;

            return store[key].Get();
        }

        public static void Set(string key, dynamic data, bool tempInMemory = false)
        {
            if (!store.ContainsKey(key))
            {
                store.Add(key, new Setting(true, key, data));
                return;
            }

            store[key].Set(data, tempInMemory);
        }
    }
}
