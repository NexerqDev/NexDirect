using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NexDirect
{
    static class WinTools
    {
        [DllImport("user32.dll")] // https://msdn.microsoft.com/en-us/library/windows/desktop/ms646293(v=vs.85).aspx
        static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

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

        // must be in this assembly or not right
        static public string GetExecLocation() => Assembly.GetExecutingAssembly().Location;

        static public void SetHandleForeground(IntPtr handle) => SetForegroundWindow(handle);

        static public DateTime ToAustralianEasternTime(DateTime dt) => TimeZoneInfo.ConvertTime(dt, TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time"));

        static public bool IsKeyHeldDown(int vKey)
            => ((GetAsyncKeyState(vKey) & 0x8000) == 0x8000); // check for the MSB (most significant byte) and if that is set then the key is held down

        // https://stackoverflow.com/questions/394816/how-to-get-parent-process-in-net-in-managed-way
        // thank you!
        // no real idea how it all works.
        /// <summary>
        /// A utility class to determine a process parent.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct ParentProcessUtilities
        {
            // These members must match PROCESS_BASIC_INFORMATION
            internal IntPtr Reserved1;
            internal IntPtr PebBaseAddress;
            internal IntPtr Reserved2_0;
            internal IntPtr Reserved2_1;
            internal IntPtr UniqueProcessId;
            internal IntPtr InheritedFromUniqueProcessId;

            [DllImport("ntdll.dll")]
            private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessUtilities processInformation, int processInformationLength, out int returnLength);

            /// <summary>
            /// Gets the parent process of the current process.
            /// </summary>
            /// <returns>An instance of the Process class.</returns>
            public static Process GetParentProcess()
            {
                return GetParentProcess(Process.GetCurrentProcess().Handle);
            }

            /// <summary>
            /// Gets the parent process of specified process.
            /// </summary>
            /// <param name="id">The process id.</param>
            /// <returns>An instance of the Process class.</returns>
            public static Process GetParentProcess(int id)
            {
                Process process = Process.GetProcessById(id);
                return GetParentProcess(process.Handle);
            }

            /// <summary>
            /// Gets the parent process of a specified process.
            /// </summary>
            /// <param name="handle">The process handle.</param>
            /// <returns>An instance of the Process class or null if an error occurred.</returns>
            public static Process GetParentProcess(IntPtr handle)
            {
                ParentProcessUtilities pbi = new ParentProcessUtilities();
                int returnLength;
                int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
                if (status != 0)
                    return null;

                try
                {
                    return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
                }
                catch (ArgumentException)
                {
                    // not found
                    return null;
                }
            }
        }
    }
}
