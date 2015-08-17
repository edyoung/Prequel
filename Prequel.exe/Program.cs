using System;
using System.Collections.Generic;
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
            }
            catch (UsageException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return ex.ExitCode;
            }

            var checker = new Checker(arguments);
            var results = checker.Run();

            if (results.Errors.Count > 0)
            {
                Console.Error.WriteLine("SQL Parse Errors:");
                foreach(var error in results.Errors)
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
            return results.ExitCode;
        }
    }
}
