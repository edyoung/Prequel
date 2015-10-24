namespace Prequel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public enum WarningID
    {
        UndeclaredVariableUsed = 1,
        UnusedVariableDeclared,
        ProcedureWithoutNoCount,
        ProcedureWithSPPrefix,        
        StringTruncated,
        StringConverted,
    }
    
    public enum WarningLevel
    {
        None = 0,
        Critical = 1,
        Serious = 2,
        Minor = 3,
        Max = 3
    }
    
    public class Warning
    {        
        private static IDictionary<WarningID, WarningInfo> warningTypes = CreateWarningInfoMap();

        public static IDictionary<WarningID, WarningInfo> WarningTypes
        {
            get { return warningTypes; }
        }

        public int Line { get; private set; }

        public string Message { get; private set; }

        public WarningID Number { get; private set; }

        public WarningInfo Info { get; private set; }

        // use the per-warning factory methods instead
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
                string.Format("Procedure {0} should not start with sp_", procedureName));
        }

        public static Warning ProcedureWithoutNoCount(int line, string procedureName)
        {
            return new Warning(line, WarningID.ProcedureWithoutNoCount,
                string.Format("Procedure {0} does not SET NOCOUNT ON", procedureName));
        }

        public static Warning UndeclaredVariableUsed(int line, string variableName)
        {
            return new Warning(line, WarningID.UndeclaredVariableUsed,
                string.Format("Variable {0} used before being declared", variableName));
        }

        public static Warning UnusedVariableDeclared(int line, string variableName)
        {
            return new Warning(line, WarningID.UnusedVariableDeclared, string.Format("Variable {0} declared but never used", variableName));
        }
        
        public static Warning StringTruncated(int line, string variableName, int targetLength, int sourceLength)
        {
            return new Warning(line, WarningID.StringTruncated, string.Format("Variable {0} has length {1} and is assigned a value with length {2}, which will be truncated", variableName, targetLength, sourceLength));
        }

        public static Warning StringConverted(int line, string variableName)
        {
            return new Warning(line, WarningID.StringConverted, string.Format("Variable {0} is of 8-bit (char or varchar) type but is assigned a unicode value.", variableName));
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:ParameterMustNotSpanMultipleLines", Justification = "Long doc strings")]
        private static IDictionary<WarningID, WarningInfo> CreateWarningInfoMap()
        {
            IDictionary<WarningID, WarningInfo> warningInfo = new Dictionary<WarningID, WarningInfo>();
            WarningInfo[] warnings = new[] 
            {
                new WarningInfo(
                    WarningID.UndeclaredVariableUsed,
                    WarningLevel.Critical,
                    "Undeclared Variable used",
                    "A variable which was not declared was referenced or set. Declare it before use, for example 'DECLARE @variable AS INT'"),

                new WarningInfo(
                    WarningID.UnusedVariableDeclared,
                    WarningLevel.Minor,
                    "Unused Variable declared",
                    "A variable or parameter was declared, but never referenced. It could be removed without affecting the procedure's logic, or this could indicate a typo or logical error"),

                new WarningInfo(
                    WarningID.ProcedureWithoutNoCount,
                    WarningLevel.Minor,
                    "Procedure without SET NOCOUNT ON",
                    @"Performance for stored procedures can be increased with the SET NOCOUNT ON option. The difference can range from tiny to substantial depending on the nature of the sproc. 
    Some SQL tools require the rowcount to be returned - if you use one of those, suppress this warning."),

                new WarningInfo(
                    WarningID.ProcedureWithSPPrefix,
                    WarningLevel.Serious,
                    "Procedure name begins with sp_",
                    "sp_ is a reserved prefix in SQL server. Even a sproc which does not clash with any system procedure incurs a performance penalty when using this prefix. Rename the procedure."),

                new WarningInfo(
                    WarningID.StringTruncated,
                    WarningLevel.Serious,
                    "Fixed-length or variable-length variable assigned a value greater than it can hold",
                    "A variable was assigned a string which is too large for it to hold. The string will be truncated, which is probably not desired."),

                new WarningInfo(
                    WarningID.StringConverted,
                    WarningLevel.Serious,
                    "8-bit variable assigned a unicode value",
                    "A variable is of 8-bit (char or varchar) type but is assigned a unicode value. This will mangle the text if it contains characters which can't be represented. Use CONVERT to explicitly indicate how you want this handled.")
            };

            foreach (var w in warnings)
            {
                warningInfo[w.ID] = w;
            }

            return warningInfo;
        }
    }
}
