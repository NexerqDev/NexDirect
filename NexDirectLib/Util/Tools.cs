using System;
using System.IO;

namespace NexDirectLib.Util
{
    public static class Tools
    {
        // https://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name sol #2
        static public string SanitizeFilename(string filename)
        {
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            return string.Join("_", filename.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        public static byte[] StreamToByteArray(Stream s)
        {
            using (var ms = new MemoryStream())
            {
                s.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
