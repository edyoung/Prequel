using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prequel
{
    public class Warning
    {
        public int Line { get; private set; }
        public string Message { get; private set; }
        public int Number { get; private set; }

        public Warning(int line, int number, string message)
        {
            this.Line = line;
            this.Number = number;
            this.Message = message;
        }
    }
}
