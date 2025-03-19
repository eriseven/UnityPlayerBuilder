using System;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Collections.Generic;

namespace Eflatun.AndroidManifestHook
{
    public static class AndroidManifestExtensions
    {
        private const string OriginalActivity = "com.unity3d.player.UnityPlayerActivity";

        private static XNamespace _ns => AndroidManifest.AndroidNamespace;

        public static XElement ReplaceCustomUnityPlayerActivity(this AndroidManifest manifest, string customActivity,
            string originalActivity = OriginalActivity)
        {
            if (!string.IsNullOrEmpty(customActivity))
            {
                var original = manifest.ApplicationElement.Elements("activity").FirstOrDefault(element =>
                    element.Attribute(_ns + "name")?.Value == originalActivity);

                original?.SetAttributeValue(_ns + "name", customActivity);
                return original;
            }

            return null;
        }

        public static bool SetAttributeValue(this AndroidManifest manifest, string elementPath, string attr, object value)
        {
            var element = manifest.AndroidManifestDocument.XPathSelectElement(elementPath);
            element?.SetAttributeValue(_ns + attr, value);
            return element != null;
        }

        private const string PermissionElementName = "uses-permission";

        static XElement CreatePermission(string permission)
        {
            var permissionElement = new XElement(PermissionElementName);
            permissionElement.SetAttributeValue(_ns + "name", permission);
            return permissionElement;
        }

        public static void AddElementWithAttrbutes(this XElement parent, string elementName,
            IEnumerable<Tuple<string, object>> attributes)
        {
            parent.Add(CreateElementWithAttrbutes(elementName, attributes));
        }
        
        public static XElement CreateElementWithAttrbutes(string elementName, IEnumerable<Tuple<string, object>> attributes)
        {
            var element = new XElement(elementName);
            foreach (var attr in attributes)
            {
                element.SetAttributeValue(_ns + attr.Item1, attr.Item2);
            }
            return element;
        }
        
        public static void AddPermission(this AndroidManifest manifest, string permission)
        {
            var attrName = _ns + "name";
            if (!manifest.ManifestElement.Elements(PermissionElementName)
                .Any(el => el.Attribute(attrName) != null && el.Attribute(attrName).Value == permission))
            {
                manifest.ManifestElement.Add(CreatePermission(permission));
            }
        }
        
    }
}