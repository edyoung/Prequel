namespace Prequel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.SqlServer.TransactSql.ScriptDom;

    public static class SqlParserFactory
    {
        private static IDictionary<string, Type> supportedVersions = InitVersions();

        public static string AllVersions
        {
            get
            {
                return string.Join(",", supportedVersions.Keys.ToArray());
            }
        }

        public static Type DefaultType
        {
            get { return typeof(TSql120Parser); }
        }

        private static IDictionary<string, Type> InitVersions()
        {
            // sorted just so they get displayed in a nice order in the known version list
            var versions = new SortedDictionary<string, Type>();
            versions.Add("2000", typeof(TSql80Parser));
            versions.Add("2005", typeof(TSql90Parser));
            versions.Add("2008", typeof(TSql100Parser));
            versions.Add("2012", typeof(TSql110Parser));
            versions.Add("2014", typeof(TSql120Parser));
            return versions;
        }

        public static Type Type(string versionString)
        {
            Type type;
            if (string.IsNullOrEmpty(versionString) || !supportedVersions.TryGetValue(versionString, out type))
            {
                throw new ArgumentException(string.Format("Unknown SQL version '{0}'. Known versions are: {1}", versionString, AllVersions));
            }

            return type;
        }
    }
}
