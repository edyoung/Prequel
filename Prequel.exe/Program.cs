using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

                if (results.Errors.Count > 0)
                {
                    Console.Error.WriteLine("SQL Parse Errors:");
                    foreach (var error in results.Errors)
                    {
                        Console.Error.WriteLine(results.FormatError(error));
                    }
                }

                if (results.Warnings.Count > 0)
                {
                    Console.Error.WriteLine("Warnings:");
                    foreach (var warning in results.Warnings)
                    {
                        Console.Error.WriteLine(results.FormatWarning(warning));
                    }
                }
                return (int)results.ExitCode;
            }
            catch (ProgramTerminatingException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(Arguments.UsageDescription);
                return (int)ex.ExitCode;
            }
        }
    }
}
