using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prequel
{
    public class Arguments
    {
        private string[] args;
        private IList<string> files = new List<string>();

        public Arguments(params string[] args)
        {
            SetDefaults();
            if (args.Length == 0)
            {
                throw new UsageException("You must specify at least one file to check");
            }
            this.args = args;

            for(int i = 0; i < args.Length; ++i)
            {
                ProcessArgument(i, args);
            }
        }

        private void SetDefaults()
        {
            SqlParserType = SqlParserFactory.DefaultType;
        }

        public IEnumerable<string> Files {
            get { return files; }
        }

        public Type SqlParserType { get; private set; }

        private void ProcessArgument(int i, string[] args)
        {
            string currentArg = args[i].ToLowerInvariant();
            if (currentArg.StartsWith("/") || currentArg.StartsWith("-"))
            {
                ProcessFlag(currentArg.Substring(1));
            }
            else
            {
                ProcessFile(currentArg);
            }
        }

        private void ProcessFile(string file)
        {
            files.Add(file);
        }

        private void ProcessFlag(string flag)
        { 
            if (flag == "?")
            {
                throw new UsageException() { ExitCode = 0 };
            }

            if (flag.StartsWith("v:"))
            {
                string versionString = flag.Substring(2);
                try {
                    SqlParserType = SqlParserFactory.Type(versionString);
                }
                catch(ArgumentException ex)
                {
                    throw new UsageException(ex.Message);
                }
            }
        }
    }
}
