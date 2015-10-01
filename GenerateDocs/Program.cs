using Prequel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateDocs
{
    /// <summary>
    /// Output markdown text for info about the program's options, to keep docs in sync with code
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            using (var stream = new StreamWriter("warnings.md"))
            {
                foreach (var id in System.Enum.GetValues(typeof(WarningID)))
                {
                    WarningID warningID = (WarningID)id;
                    WarningInfo info = Warning.WarningTypes[warningID];

                    stream.WriteLine(@"
### Warning {0} : {1}
{2}

{3}
", (int)info.ID, info.Name, info.Level, info.Description);
                }
            }

            foreach(var f in Flag.AllFlags())
            {
                Console.WriteLine(@"
### Option /{0} (/{1}) {2} {3}
{4}
", f.LongName, f.ShortName, (f.AcceptsValue ? ":" : ""), f.ExampleValue, f.HelpText);
            }
        }
    }
}
