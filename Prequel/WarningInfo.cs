namespace Prequel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class WarningInfo
    {
        public const int MinWarningID = (int)WarningID.UndeclaredVariableUsed;
        public const int MaxWarningID = (int)WarningID.ProcedureWithSPPrefix;

        public WarningLevel Level
        {
            get; private set;
        }

        public WarningID ID
        {
            get; private set;
        }

        public string Name
        {
            get; private set;
        }

        public string Description
        {
            get; private set;
        }

        public WarningInfo(WarningID id, WarningLevel level, string name, string description)
        {
            ID = id;
            Level = level;
            Name = name;
            Description = description;
        }
    }
}
