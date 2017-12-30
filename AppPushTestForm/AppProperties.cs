using System.Reflection;
using System.IO;
using Xattacker.Utility;

namespace AppPush
{
    public class AppProperties
    {
        private static Assembly assembly = null;

        static AppProperties()
        {
            // lazy initial
            assembly = Assembly.GetExecutingAssembly();
        }

        private AppProperties()
        {
        }

        public static string AppName
        {
            get
            {
                return assembly.GetName().Name;
            }
        }

        public static string Version
        {
            get
            {
                return assembly.GetName().Version.ToString();
            }
        }

        public static string Copyright
        {
            get
            {
                object[] r = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);

                AssemblyCopyrightAttribute ct = (AssemblyCopyrightAttribute)(r[0]);

                return ct.Copyright;
            }
        }

        public static string Company
        {
            get
            {
                object[] r = assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);

                AssemblyCompanyAttribute ct = (AssemblyCompanyAttribute)(r[0]);

                return ct.Company;
            }
        }

        public static string AppPath
        {
            get
            {
                return FileUtility.GetAppPath();
            }
        }

        public static string AppStoragePath
        {
            get
            {
                string path = Path.Combine(AppPath, "storage");

                DirectoryInfo dir = new DirectoryInfo(path);
                if (!dir.Exists)
                {
                    dir.Create();
                }

                return path;
            }
        }

        public static string AppTempPath
        {
            get
            {
                string path = Path.Combine(AppPath, "temp");

                DirectoryInfo dir = new DirectoryInfo(path);
                if (!dir.Exists)
                {
                    dir.Create();
                }

                return path;
            }
        }
    }
}
