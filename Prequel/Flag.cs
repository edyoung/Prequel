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

        public string ExampleValue { get; private set; }

        public string HelpText { get; private set; }

        public bool AcceptsValue { get; private set; }

        public Action<Arguments, string> Process { get; private set; }

        private Flag(string shortName, string longName,
            string exampleValue, string helpText,
            bool acceptsValue, Action<Arguments, string> action)
        {
            this.ShortName = shortName;
            this.LongName = longName;
            this.ExampleValue = exampleValue;
            this.HelpText = helpText;
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
            if (AcceptsValue)
            {
                if (parameter.StartsWith(ShortName + ":"))
                {
                    value = parameter.Substring(ShortName.Length + 1);
                }
                else if (parameter.StartsWith(LongName + ":"))
                {
                    value = parameter.Substring(LongName.Length + 1);
                }
                else
                {
                    value = null;
                    return false;
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

        public string UsageText
        {
            get
            {
                // like: /f /foo:wibble
                string firstPart = string.Format(
                    "/{0} /{1}{2}{3}", ShortName, LongName, AcceptsValue ? ":" : "", ExampleValue);

                // FIXME pad the first part with spaces once on the internet and can figure out syntax
                return string.Format("{0}{1}", firstPart, HelpText);
            }
        }

        private static Flag Help()
        {
            return new Flag("?", "help", "", "Print this message and exit", false,
                (arguments, value) =>
                {
                    throw new ProgramTerminatingException("Usage Information Requested", ExitReason.Success);
                });
        }

        private static Flag SqlVersion()
        {
            return new Flag("v", "sqlversion",
                "version",
                string.Format("Specify the SQL dialect to use. Default 2014, options: {0}", SqlParserFactory.AllVersions),
                true,
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
            return new Flag("i", "inline",
                "'select ...'", "Give a string of inline sql to parse and check", true,
                (arguments, value) =>
                {
                    arguments.Inputs.Add(Input.FromString(value));
                });
        }

        public static Flag NoLogo()
        {
            return new Flag("q", "nologo", "", "Don't print program name and version info", false,
                (arguments, value) =>
                {
                    arguments.DisplayLogo = false;
                });
        }

        public static Flag Warnings()
        {
            return new Flag("w", "warn", "level", "0-3. 0 = syntax errors only, 1 critical warnings, 2 = serious warnings, 3 = all warnings", true,
                (arguments, value) =>
                {
                    try
                    {
                        int level = Convert.ToInt32(value);
                        if (level < 0 || level > (int)WarningLevel.Max)
                        {
                            throw new ProgramTerminatingException(string.Format("Invalid Warning Level '{0}'", value));
                        }
                        arguments.WarningLevel = (WarningLevel)level;
                    }
                    catch (FormatException)
                    {
                        throw new ProgramTerminatingException(string.Format("Invalid Warning Level '{0}'", value));
                    }
                });
        }

        public static IEnumerable<Flag> AllFlags()
        {
            yield return Help();
            yield return SqlVersion();
            yield return InlineSql();
            yield return NoLogo();
            yield return Warnings();
        }
    }
}
