namespace Prequel
{
    using System;
    using System.Collections.Generic;
    using Microsoft.SqlServer.TransactSql.ScriptDom;

    [Flags]
    public enum TypeConversionResult
    {
        /// <summary>
        /// SQL will convert from A to B implicitly and safely
        /// </summary>
        ImplicitSafe = 0,

        /// <summary>
        /// SQL will convert from A to B implicitly but data could be mangled in the process
        /// </summary>
        ImplicitLossy = 1,

        /// <summary>
        /// Only Explicit conversons allowed
        /// </summary>
        Explicit = 2,

        /// <summary>
        /// Will be implicitly converted but could be lossy depending on the size of the data (eg varchar)
        /// </summary>
        CheckLength = 4,

        /// <summary>
        /// String type will undergo unicode -> code page conversion
        /// </summary>
        Narrowing = 8,

        /// <summary>
        /// A cannot be converted to B
        /// </summary>
        NotAllowed = 16,

        /// <summary>
        /// Prequel doesn't know what will happen. Claim everything is great
        /// </summary>
        NotImplemented = 32
    }

    internal static class TypeConversionHelper
    {
        // Encodes the table in https://msdn.microsoft.com/en-US/library/ms191530(v=sql.120).aspx showing which types can be converted 
        private static IDictionary<Tuple<SqlDataTypeOption, SqlDataTypeOption>, TypeConversionResult> conversionTable = CreateConversionTable();

        private static IDictionary<Tuple<SqlDataTypeOption, SqlDataTypeOption>, TypeConversionResult> CreateConversionTable()
        {
            var conversions = new Dictionary<Tuple<SqlDataTypeOption, SqlDataTypeOption>, TypeConversionResult>();

            conversions.Add(SqlDataTypeOption.NChar, SqlDataTypeOption.Char, TypeConversionResult.CheckLength | TypeConversionResult.Narrowing);
            conversions.Add(SqlDataTypeOption.NChar, SqlDataTypeOption.VarChar, TypeConversionResult.CheckLength | TypeConversionResult.Narrowing);
            conversions.Add(SqlDataTypeOption.NVarChar, SqlDataTypeOption.Char, TypeConversionResult.CheckLength | TypeConversionResult.Narrowing);
            conversions.Add(SqlDataTypeOption.NVarChar, SqlDataTypeOption.VarChar, TypeConversionResult.CheckLength | TypeConversionResult.Narrowing);

            conversions.Add(SqlDataTypeOption.Char, SqlDataTypeOption.Char, TypeConversionResult.CheckLength);
            conversions.Add(SqlDataTypeOption.NChar, SqlDataTypeOption.NChar, TypeConversionResult.CheckLength);
            conversions.Add(SqlDataTypeOption.VarChar, SqlDataTypeOption.VarChar, TypeConversionResult.CheckLength);
            conversions.Add(SqlDataTypeOption.NVarChar, SqlDataTypeOption.NVarChar, TypeConversionResult.CheckLength);
            conversions.Add(SqlDataTypeOption.Char, SqlDataTypeOption.VarChar, TypeConversionResult.CheckLength);
            conversions.Add(SqlDataTypeOption.NChar, SqlDataTypeOption.NVarChar, TypeConversionResult.CheckLength);

            conversions.Add(SqlDataTypeOption.NChar, SqlDataTypeOption.Int, TypeConversionResult.ImplicitLossy);

            return conversions;
        }

        public static TypeConversionResult GetConversionResult(SqlDataTypeOption from, SqlDataTypeOption to)
        {
            TypeConversionResult result = TypeConversionResult.NotImplemented;
            conversionTable.TryGetValue(new Tuple<SqlDataTypeOption, SqlDataTypeOption>(from, to), out result);
            return result;
        }

        public static void Add(this IDictionary<Tuple<SqlDataTypeOption, SqlDataTypeOption>, TypeConversionResult> dictionary, SqlDataTypeOption from, SqlDataTypeOption to, TypeConversionResult result)
        {
            dictionary.Add(new Tuple<SqlDataTypeOption, SqlDataTypeOption>(from, to), result);
        }
    }
}