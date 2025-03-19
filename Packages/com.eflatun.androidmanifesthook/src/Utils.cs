using System.IO;
using System.Text;

namespace Eflatun.AndroidManifestHook
{
    internal static class Utils
    {
        static string launcherProjectName = "launcher";
        static string unityLibraryProjectName = "unityLibrary";
        
        static string manifestSubPath = Path.Combine("src", "main", "AndroidManifest.xml");
        
        public static string GetManifestPath(string basePath)
        {
            return Path.Combine(basePath, unityLibraryProjectName, manifestSubPath);
            // var pathBuilder = new StringBuilder(basePath);
            // pathBuilder.Append(Path.DirectorySeparatorChar).Append("src");
            // pathBuilder.Append(Path.DirectorySeparatorChar).Append("main");
            // pathBuilder.Append(Path.DirectorySeparatorChar).Append("AndroidManifest.xml");
            // return pathBuilder.ToString();
        }

        public static string ToManifestString(this bool val)
        {
            return val ? "true" : "false";
        }
    }
}
