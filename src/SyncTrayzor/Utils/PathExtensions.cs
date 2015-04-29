using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SyncTrayzor.Utils
{
    public class PathExtensions
    {
        public static String GetShortPathName(String longPath)
        {
            StringBuilder shortPath = new StringBuilder(longPath.Length + 1);

            if (PathExtensions.GetShortPathName(longPath, shortPath, shortPath.Capacity) == 0)
            {
                return longPath;
            }

            return shortPath.ToString();
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern Int32 GetShortPathName(String path, StringBuilder shortPath, Int32 shortPathLength);

        public static String GetDirectoryName(String filePath)
        {
            //Recreated function for getting the path part of a file path
            //Primary as a workaround for Path.GetDirectoryName cannot handle long paths

            String[] pathParts = filePath.Split(Path.DirectorySeparatorChar);
            String[] directoryParts = pathParts.Take(pathParts.Count() - 1).ToArray();
            return String.Join(Path.DirectorySeparatorChar.ToString(), directoryParts);
        }
    }
}
