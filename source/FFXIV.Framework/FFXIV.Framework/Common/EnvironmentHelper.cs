using System;
using System.IO;
using System.Reflection;

namespace FFXIV.Framework.Common
{
    public static class EnvironmentHelper
    {
        public static string GetAppDataPath()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                GetCompanyName() + "\\" + GetProductName());

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        public static string GetProductName()
        {
            var atr = (AssemblyProductAttribute)Attribute.GetCustomAttribute(
                Assembly.GetEntryAssembly(),
                typeof(AssemblyProductAttribute));

            return atr != null ? atr.Product : "UNKNOWN";
        }

        public static string GetCompanyName()
        {
            var atr = (AssemblyCompanyAttribute)Attribute.GetCustomAttribute(
                Assembly.GetEntryAssembly(),
                typeof(AssemblyCompanyAttribute));

            return atr != null ? atr.Company : "UNKNOWN";
        }

        public static Version GetVersion()
        {
            return Assembly.GetEntryAssembly().GetName().Version;
        }

        public static string ToStringShort(
            this Version version)
        {
            var v =
                "v" +
                version.Major.ToString() + "." +
                version.Minor.ToString() + "." +
                version.Revision.ToString();

            return v;
        }
    }
}
