using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Prequel
{
    public class Input
    {
        public string Path { get; set; }

        private Stream stream;
        public Stream Stream
        {
            get
            {
                if (stream == null)
                {
                    stream = File.OpenRead(Path);
                }
                return stream;
            }

            private set { stream = value; }
        }

        public static Input FromString(string inlineInput)
        {
            return new Input() { Path = "<inline>", Stream = new MemoryStream(Encoding.UTF8.GetBytes(inlineInput)) }; // correct encoding? 
        }

        public static Input FromFile(string path)
        {
            return new Input() { Path = path }; // defer opening the file until we want to read it
        }
    }

    /// <summary>
    /// Encapsulates a command line flag such as /?
    /// </summary>
    public class Flag
    {
        public string ShortName
        {
            get; private set;
        }
        public string LongName { get; private set; }
        public string HelpText { get; private set; }
        public bool AcceptsValue { get; private set; }
        public Action<Arguments, string> Process { get; private set; }

        private Flag(string shortName, string longName, string helpText, bool acceptsValue, Action<Arguments, string> action)
        {
            this.ShortName = shortName; this.LongName = longName; this.HelpText = helpText;
            this.AcceptsValue = acceptsValue;
            this.Process = action;
        }

        public bool Handles(string flag, Arguments arguments)
        {
            string value;
            if (TryMatch(flag, out value))
            {
                Process(arguments, value);
                return true;
            }
            return false;
        }

        public bool TryMatch(string parameter, out string value)
        {
            if(AcceptsValue)
            {
                if (parameter.StartsWith(ShortName))
                {
                    value = parameter.Substring(ShortName.Length);
                }
                else if (parameter.StartsWith(LongName))
                {
                    value = parameter.Substring(LongName.Length);
                }
                else
                {
                    value = null;
                    return false;
                }

                if (value.StartsWith(":"))
                {
                    value = value.Substring(1);
                }
            }
            else
            {
                // just look for the exact name
                value = null;
                return parameter == ShortName || parameter == LongName;
            }
            return true;            
        }

        private static Flag Help()
        {
            return new Flag("?", "help", "Print this message and exit", false,
                (arguments, value) =>
                {
                    throw new ProgramTerminatingException("Usage Information Requested", ExitReason.Success);
                });
        }

        private static Flag SqlVersion()
        {
            return new Flag("v", "sqlversion", "Specify the version of SQL to target", true,
                (arguments, value) =>
                {
                    try
                    {
                        arguments.SqlParserType = SqlParserFactory.Type(value);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new ProgramTerminatingException(ex.Message);
                    }
                });
        }

        private static Flag InlineSql()
        {
            return new Flag("i", "inline", "xxx", true,
                (arguments, value) =>
                {
                    arguments.Inputs.Add(Input.FromString(value));
                });
        }

        public static Flag NoLogo()
        {
            return new Flag("q", "nologo", "xxx", false,
                (arguments, value) =>
                {
                    arguments.DisplayLogo = false;
                });
        }

        public static IEnumerable<Flag> AllFlags()
        {
            yield return Help();
            yield return SqlVersion();
            yield return InlineSql();
        }
        
    }

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
                return String.Format(@"Usage: Prequel.exe [flags] file1.sql [file2.sql ...]
/?:                 Print this message and exit
/v:version          Specify the SQL dialect to use. Default 2014, options: {0}
/i:'select ...'     Give a string of inline sql to parse and check
/nologo             Don't print program name and version info
/warn:level         0-3. 0 = syntax errors only, 1 critical warnings, 2 = serious warnings, 3 = all warnings
",
                        SqlParserFactory.AllVersions);
            }
        }

        public bool DisplayLogo { get; internal set; }
        public WarningLevel WarningLevel { get; set; }

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
            foreach(var f in Flag.AllFlags())
            {
                if(f.Handles(flag, this))
                {
                    return;
                }
            }
                        
            if (flag.StartsWith("warn:"))
            {
                string levelString = flag.Substring(5);
                try
                {
                    int level = (Convert.ToInt32(levelString));
                    if (level < 0 || level > (int)WarningLevel.Max)
                    {
                        throw new ProgramTerminatingException(String.Format("Invalid Warning Level '{0}'", levelString));
                    }
                    WarningLevel = (WarningLevel)level;
                }
                catch (FormatException)
                {
                    throw new ProgramTerminatingException(String.Format("Invalid Warning Level '{0}'", levelString));
                }
            }
        }
    }
}
