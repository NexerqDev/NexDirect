using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NexDirect
{
    static class WinTools
    {
        // https://stackoverflow.com/questions/3120616/wpf-datagrid-selected-row-clicked-event sol #2
        static public object GetGridViewSelectedRowItem(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = ItemsControl.ContainerFromElement((DataGrid)sender, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row == null)
                return null;
            return row.Item;
        }

        static public T GetGridViewSelectedRowItem<T>(object sender, MouseButtonEventArgs e)
        {
            object item = GetGridViewSelectedRowItem(sender, e);
            return item == null ? default(T) : (T)item;
        }

        static public string GetOwnVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
            return info.FileVersion;
        }

        static public string GetGitStyleVersion()
        {
            // change version to github style semver
            string[] versionParts = GetOwnVersion().Split('.');
            string version = $"{versionParts[0]}.{versionParts[1]}.{versionParts[2]}";
            return version;
        }
    }
}
