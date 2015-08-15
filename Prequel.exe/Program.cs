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
            try {
                arguments = new Arguments(args);
            }
            catch (UsageException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return ex.ExitCode;
            }
            var checker = new Checker(arguments);
            var results = checker.Run();

            return results.ExitCode;
        }
    }
}
