using System.Xml;
using System.Xml.Linq;

namespace Eflatun.AndroidManifestHook
{
    public class AndroidManifest {
        public XElement ManifestElement { get; private set; }
        public XElement ApplicationElement { get; private set; }
        
        private static XNamespace android = "http://schemas.android.com/apk/res/android";
        public static XNamespace AndroidNamespace => android;

        private string _manifestPath;
        public string Path => _manifestPath;

        public XDocument AndroidManifestDocument { get; private set; }

        public AndroidManifest(string path)
        {
            Load(path);
        }

        public void Load(string path)
        {
            _manifestPath = path;
            AndroidManifestDocument = XDocument.Load(path);
            ManifestElement = AndroidManifestDocument.Element("manifest");
            ApplicationElement = ManifestElement?.Element("application");
        }

        public void Save()
        {
            AndroidManifestDocument.Save(_manifestPath);
        }

        /// <summary>
        /// returns <paramref name="value"/>
        /// </summary>
        public string SetAttributeWithAndroidNamespace(XmlElement element, string name, string value)
        {
            // return element.SetAttribute(name, AndroidNamespace, value);
            return "";
        }
    }
}
