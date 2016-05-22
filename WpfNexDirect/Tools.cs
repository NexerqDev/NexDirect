using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NexDirect
{
    static class Tools
    {
        static public string resolveRankingStatus(string status)
        {
            switch (status)
            {
                case "3":
                    return "Qualified";
                case "2":
                    return "Approved";
                case "1":
                    return "Ranked";
                case "0":
                    return "Pending";
                case "-1":
                    return "WIP";
                case "-2":
                    return "Graveyard";
                default:
                    return "Unknown";
            }
        }

        static public string resolveRankingComboBox(string input)
        {
            switch (input)
            {
                case "Ranked":
                    return "1,2";
                case "Qualified":
                    return "3";
                case "Unranked":
                    return "0,-1,-2";
                default:
                    return null;
            }
        }

        static public string resolveModeComboBox(string input)
        {
            switch (input)
            {
                case "osu!":
                    return "0";
                case "Catch the Beat":
                    return "2";
                case "Taiko":
                    return "1";
                case "osu!mania":
                    return "3";
                default:
                    return null;
            }
        }

        // https://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name sol #2
        static public string sanitizeFilename(string filename)
        {
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            return String.Join("_", filename.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        // https://stackoverflow.com/questions/3120616/wpf-datagrid-selected-row-clicked-event sol #2
        static public object getGridViewSelectedRowItem(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = ItemsControl.ContainerFromElement((DataGrid)sender, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row == null) return null;
            return row.Item;
        }
    }
}
