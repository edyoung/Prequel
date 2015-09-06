using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prequel
{
    public enum WarningID
    {
        UndeclaredVariableUsed = 1,
        UnusedVariableDeclared
    }

    public enum WarningFilterLevel
    {
        None = 0,
        Critical = 1,
        Serious = 2,
        Minor = 3
    }

    public class Warning
    {
        public int Line { get; private set; }
        public string Message { get; private set; }
        public WarningID Number { get; private set; }

        public Warning(int line, WarningID number, string message)
        {
            this.Line = line;
            this.Number = number;
            this.Message = message;
        }
    }
}
