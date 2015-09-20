using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Prequel.exe
{
    class Program
    {
        static int Main(string[] args)
        {
            Arguments arguments;
            try
            {
                arguments = new Arguments(args);

                var checker = new Checker(arguments);
                
                var results = checker.Run();

                if (arguments.DisplayLogo)
                {
                    Console.WriteLine("Prequel version {0}", GetProgramVersion());
                }

                if (results.Errors.Count > 0)
                {
                    Console.WriteLine("SQL Parse Errors:");
                    foreach (var error in results.Errors)
                    {
                        Console.WriteLine(results.FormatError(error));
                    }
                }

                if (results.Warnings.Count > 0)
                {
                    Console.WriteLine("Warnings:");
                    foreach (var warning in results.Warnings)
                    {
                        Console.WriteLine(results.FormatWarning(warning));
                    }
                }
                return (int)results.ExitCode;
            }
            catch (ProgramTerminatingException ex)
            {
                Console.Error.WriteLine("Prequel version {0}", GetProgramVersion());
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(Arguments.UsageDescription);
                return (int)ex.ExitCode;
            }
        }

        private static string GetProgramVersion()
        {
            // when code is run in xunit runner, there is no entryassembly
            var assembly = Assembly.GetEntryAssembly();
            if(null == assembly)
            {
                return "unknown";
            }
            return FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;
        }
    }
}
