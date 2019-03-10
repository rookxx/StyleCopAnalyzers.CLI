namespace StyleCopAnalyzersCmd
{
    using System.IO;

    public static class ArgumentPathHelper
    {
        public static string GetAbsolutePath(string path)
        {
            return Path.GetFullPath(path);
        }

        public static string GetAbsoluteOrDefaultFilePath(string path, string defaultPath)
        {
            if (File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
            else
            {
                return Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, defaultPath);
            }
        }
    }
}