using System;

namespace NexDirectLib
{
    public static class Tools
    {
        // https://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name sol #2
        static public string SanitizeFilename(string filename)
        {
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            return string.Join("_", filename.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        static public string GetExecLocation() => System.Reflection.Assembly.GetExecutingAssembly().Location;
    }
}
