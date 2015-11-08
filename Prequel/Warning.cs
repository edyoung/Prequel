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
        ImplicitConversion,
        ConvertToVarCharOfUnspecifiedLength,
        ConvertToTooShortString
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
            return new Warning(line, WarningID.ProcedureWithSPPrefix, $"Procedure {procedureName} should not start with sp_");
        }

        public static Warning ProcedureWithoutNoCount(int line, string procedureName)
        {
            return new Warning(line, WarningID.ProcedureWithoutNoCount, $"Procedure {procedureName} does not SET NOCOUNT ON");
        }

        public static Warning UndeclaredVariableUsed(int line, string variableName)
        {
            return new Warning(line, WarningID.UndeclaredVariableUsed, $"Variable {variableName} used before being declared");
        }

        internal static Warning ImplicitConversion(int line, string variableName, string destinationType, string sourceType)
        {
            return new Warning(line, WarningID.ImplicitConversion, 
                $"Variable {variableName} has type {destinationType} and is assigned a value of type {sourceType}. Consider CAST or CONVERT to make the conversion explicit");
        }

        public static Warning UnusedVariableDeclared(int line, string variableName)
        {
            return new Warning(line, WarningID.UnusedVariableDeclared, $"Variable {variableName} declared but never used");
        }
        
        public static Warning StringTruncated(int line, string variableName, int targetLength, int sourceLength)
        {
            return new Warning(line, WarningID.StringTruncated, $"Variable {variableName} has length {targetLength} and is assigned a value with length up to {sourceLength}, which might be truncated");
        }

        public static Warning StringConverted(int line, string variableName)
        {
            return new Warning(line, WarningID.StringConverted, $"Variable {variableName} is of 8-bit (char or varchar) type but is assigned a unicode value.");
        }

        public static Warning ConvertToVarCharOfUnspecifiedLength(int line, string operation, string type)
        {
            return new Warning(line, WarningID.ConvertToVarCharOfUnspecifiedLength, $"{operation} to type {type} without specifying length may lead to unexpected truncation.");
        }

        public static Warning ConvertToTooShortString(int line, string variableName, int targetLength, int sourceLength)
        {
            return new Warning(line, WarningID.ConvertToTooShortString, $"Variable {variableName} has length {targetLength} and is assigned a value which could be up to {sourceLength} characters when converted to a string");
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
                    "A variable was assigned a string which could be too large for it to hold. The string will be truncated, which is probably not desired."),

                new WarningInfo(
                    WarningID.StringConverted,
                    WarningLevel.Serious,
                    "8-bit variable assigned a unicode value",
                    "A variable is of 8-bit (char or varchar) type but is assigned a unicode value. This will mangle the text if it contains characters which can't be represented. Use CONVERT to explicitly indicate how you want this handled."),

                new WarningInfo(
                    WarningID.ImplicitConversion,
                    WarningLevel.Minor,
                    "Suspicious implicit type conversion",
                    @"A variable of one type is assigned a value of a different type (such as assigning an Int to a VarChar). 
SQL will implicitly convert them, but it's possible this wasn't what you wanted. Use CONVERT to explicitly indicate how you want this handled."),

                new WarningInfo(
                    WarningID.ConvertToVarCharOfUnspecifiedLength,
                    WarningLevel.Serious,
                    "CONVERT to variable with implicit length",
                    @"CONVERT(char, ...) implicitly truncates values longer than 30 characters, which is often unexpected. If you really want 30, use CONVERT(char(30), ...)"),
                
                new WarningInfo(
                    WarningID.ConvertToTooShortString,
                    WarningLevel.Serious,
                    "Conversion to string might be too long",
                    "A variable was assigned to a string which could be too large for it to hold when converted to a string.")                    
            };

            foreach (var w in warnings)
            {
                warningInfo[w.ID] = w;
            }

            return warningInfo;
        }
    }
}
