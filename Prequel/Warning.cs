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

    public enum WarningLevel
    {
        None = 0,
        Critical = 1,
        Serious = 2,
        Minor = 3,
        Max = 3
    }

    public class WarningInfo
    {
        public WarningLevel Level
        {
            get; private set;
        }

        public WarningID ID
        {
            get; private set;
        }

        public WarningInfo(WarningID id, WarningLevel level)
        {
            ID = id;
            Level = level;
        }
    }

    public class Warning
    {
        static readonly IDictionary<WarningID, WarningInfo> infoMap = InitWarningLevelMap();

        public int Line { get; private set; }
        public string Message { get; private set; }
        public WarningID Number { get; private set; }
        public WarningInfo Info { get; private set; }

        public Warning(int line, WarningID number, string message)
        {
            this.Line = line;
            this.Number = number;
            this.Message = message;
            this.Info = infoMap[number];
        }

        private static IDictionary<WarningID, WarningInfo> InitWarningLevelMap()
        {
            IDictionary<WarningID, WarningInfo> warningInfo = new Dictionary<WarningID, WarningInfo>();
            warningInfo[WarningID.UndeclaredVariableUsed] = new WarningInfo(WarningID.UndeclaredVariableUsed, WarningLevel.Critical);
            warningInfo[WarningID.UnusedVariableDeclared] = new WarningInfo(WarningID.UnusedVariableDeclared, WarningLevel.Minor);

            return warningInfo;
        }
    }
}
