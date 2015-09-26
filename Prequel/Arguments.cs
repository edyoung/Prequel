namespace Prequel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    public class Arguments
    {
        private string[] args;
        private IList<Input> inputs = new List<Input>();

        public Arguments(params string[] args)
        {
            SetDefaults();
            if (args.Length == 0)
            {
                throw new ProgramTerminatingException("You must specify at least one file to check");
            }

            this.args = args;

            for (int i = 0; i < args.Length; ++i)
            {
                ProcessArgument(i, args);
            }
        }

        private void SetDefaults()
        {
            SqlParserType = SqlParserFactory.DefaultType;
            DisplayLogo = true;
            WarningLevel = WarningLevel.Serious;
        }

        public IList<Input> Inputs
        {
            get { return inputs; }
        }

        public Type SqlParserType { get; internal set; }

        public static string UsageDescription
        {
            get
            {
                var flagDescriptions = Flag.AllFlags().Select(x => x.UsageText).ToList();
                return string.Format(
@"Usage: Prequel.exe [flags] file1.sql [file2.sql ...]

Flags:
{0}
", 
string.Join("\n", flagDescriptions));
            }
        }

        public bool DisplayLogo { get; internal set; }

        public WarningLevel WarningLevel { get; internal set; }

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
            inputs.Add(Input.FromFile(file));
        }

        private void ProcessFlag(string flag)
        {
            foreach (var f in Flag.AllFlags())
            {
                if (f.Handles(flag, this))
                {
                    return;
                }
            }
        }
    }
}
