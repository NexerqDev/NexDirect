using System;

namespace NexDirectLib
{
    public static class Tools
    {
        // https://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name sol #2
        static public string sanitizeFilename(string filename)
        {
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            return String.Join("_", filename.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
    }
}
