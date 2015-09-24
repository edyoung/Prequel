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
        UnusedVariableDeclared,
        ProcedureWithoutNoCount,
        ProcedureWithSPPrefix,        
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

    public class Warning
    {
        public static readonly IDictionary<WarningID, WarningInfo> WarningTypes = InitWarningLevelMap();

        public int Line { get; private set; }
        public string Message { get; private set; }
        public WarningID Number { get; private set; }
        public WarningInfo Info { get; private set; }

        // use the per-warning factpory methods instead
        private Warning(int line, WarningID number, string message)
        {
            this.Line = line;
            this.Number = number;
            this.Message = message;
            this.Info = WarningTypes[number];
        }

        public static Warning ProcedureWithSPPrefix(int line, string procedureName)
        {
            return new Warning(line, WarningID.ProcedureWithSPPrefix,
                String.Format("Procedure {0} does not SET NOCOUNT ON", procedureName));
        }

        public static Warning ProcedureWithoutNoCount(int line, string procedureName)
        {
            return new Warning(line, WarningID.ProcedureWithoutNoCount,
                String.Format("Procedure {0} does not SET NOCOUNT ON", procedureName));
        }

        public static Warning UndeclaredVariableUsed(int line, string variableName)
        {
            return new Warning(line, WarningID.UndeclaredVariableUsed,
                String.Format("Variable {0} used before being declared", variableName));
        }

        public static Warning UnusedVariableDeclared(int line, string variableName)
        {
            return new Warning(line, WarningID.UnusedVariableDeclared, String.Format("Variable {0} declared but never used", variableName));
        }

        private static IDictionary<WarningID, WarningInfo> InitWarningLevelMap()
        {
            IDictionary<WarningID, WarningInfo> warningInfo = new Dictionary<WarningID, WarningInfo>();
            warningInfo[WarningID.UndeclaredVariableUsed] = new WarningInfo(
                WarningID.UndeclaredVariableUsed, 
                WarningLevel.Critical,
                "Undeclared Variable used",
                "A variable which was not declared was referenced or set. Declare it before use, for example 'DECLARE @variable AS INT'");
            warningInfo[WarningID.UnusedVariableDeclared] = new WarningInfo(
                WarningID.UnusedVariableDeclared, 
                WarningLevel.Minor,
                "Unused Variable declared",
                "A variable or parameter was declared, but never referenced. It could be removed without affecting the procedure's logic, or this could indicate a typo or logical error");
            warningInfo[WarningID.ProcedureWithoutNoCount] = new WarningInfo(
                WarningID.ProcedureWithoutNoCount, 
                WarningLevel.Minor,
                "Procedure without SET NOCOUNT ON",
                @"Performance for stored procedures can be increased with the SET NOCOUNT ON option. The difference can range from tiny to substantial depending on the nature of the sproc. 
Some SQL tools require the rowcount to be returned - if you use one of those, suppress this warning.");
            warningInfo[WarningID.ProcedureWithSPPrefix] = new WarningInfo(
                WarningID.ProcedureWithSPPrefix, 
                WarningLevel.Serious,
                "Procedure name begins with sp_",
                "sp_ is a reserved prefix in SQL server. Even a sproc which does not clash with any system procedure incurs a performance penalty when using this prefix. Rename the procedure");
            return warningInfo;
        }
    }
}
