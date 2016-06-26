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
        /// A will be converted to string, check max length of that
        /// </summary>
        CheckConvertedLength = 32,

        /// <summary>
        /// A will be converted to a smaller numeric type, could lead to overflow
        /// </summary>
        NumericOverflow = 64,

        /// <summary>
        /// Prequel doesn't know what will happen. Claim everything is great
        /// </summary>
        NotImplemented = 1 << 16
    }

    public struct NumericTraits
    {
        public long Min { get; private set; }

        public long Max { get; private set; }

        /// <summary>
        /// if object A has a SizeClass >= object B's SizeClass, B can be assigned to A without risk of overlow
        /// </summary>
        public int SizeClass { get; private set;  } 

        public NumericTraits(long min, long max, int sizeClass)
        {
            Min = min;
            Max = max;
            SizeClass = sizeClass;
        }
    }

    public static class TypeConversionHelper
    {
        // Encodes the table in https://msdn.microsoft.com/en-us/library/ms191530.aspx showing which types can be converted 
        private static IDictionary<Tuple<SqlDataTypeOption, SqlDataTypeOption>, TypeConversionResult> conversionTable = CreateConversionTable();

        private static TypeConversionResult[,] CreateConversionTableArray()
        {
            var cl = TypeConversionResult.CheckLength;
            var nr = TypeConversionResult.Narrowing;
            var il = TypeConversionResult.ImplicitLossy;
            var cc = TypeConversionResult.CheckConvertedLength;
            var no = TypeConversionResult.NumericOverflow;
            var ok = TypeConversionResult.ImplicitSafe;
            var __ = TypeConversionResult.ImplicitSafe;  // self-conversion
            const int nTypes = 32;
            var conversions = new TypeConversionResult[nTypes, nTypes]
            {
                // to_>
                // binary
                { __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __, ok},
                { ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, ok, __},
            };

            return conversions;
        }

        private static IDictionary<Tuple<SqlDataTypeOption, SqlDataTypeOption>, TypeConversionResult> CreateConversionTable()
        {
            var conversions = new Dictionary<Tuple<SqlDataTypeOption, SqlDataTypeOption>, TypeConversionResult>();

            var cl = TypeConversionResult.CheckLength;
            var nr = TypeConversionResult.Narrowing;
            var il = TypeConversionResult.ImplicitLossy;
            var ccl = TypeConversionResult.CheckConvertedLength;
            var no = TypeConversionResult.NumericOverflow;

            // From Char
            conversions.Add(SqlDataTypeOption.Char, SqlDataTypeOption.Char, cl);
            conversions.Add(SqlDataTypeOption.Char, SqlDataTypeOption.VarChar, cl);
            conversions.Add(SqlDataTypeOption.Char, SqlDataTypeOption.NChar, cl);
            conversions.Add(SqlDataTypeOption.Char, SqlDataTypeOption.NVarChar, cl);

            // From NChar
            conversions.Add(SqlDataTypeOption.NChar, SqlDataTypeOption.Char, cl | nr);
            conversions.Add(SqlDataTypeOption.NChar, SqlDataTypeOption.VarChar, cl | nr);
            conversions.Add(SqlDataTypeOption.NChar, SqlDataTypeOption.NChar, cl);
            conversions.Add(SqlDataTypeOption.NChar, SqlDataTypeOption.NVarChar, cl);

            conversions.Add(SqlDataTypeOption.NChar, SqlDataTypeOption.Int, il);

            // From VarChar
            conversions.Add(SqlDataTypeOption.VarChar, SqlDataTypeOption.VarChar, cl);
            conversions.Add(SqlDataTypeOption.VarChar, SqlDataTypeOption.Char, cl);
            conversions.Add(SqlDataTypeOption.VarChar, SqlDataTypeOption.NChar, cl);
            conversions.Add(SqlDataTypeOption.VarChar, SqlDataTypeOption.NVarChar, cl);

            // from NVarChar
            conversions.Add(SqlDataTypeOption.NVarChar, SqlDataTypeOption.Char, cl | nr);
            conversions.Add(SqlDataTypeOption.NVarChar, SqlDataTypeOption.VarChar, cl | nr);
            conversions.Add(SqlDataTypeOption.NVarChar, SqlDataTypeOption.NChar, cl);
            conversions.Add(SqlDataTypeOption.NVarChar, SqlDataTypeOption.NVarChar, cl);

            // from Int
            conversions.Add(SqlDataTypeOption.Int, SqlDataTypeOption.Char, ccl);
            conversions.Add(SqlDataTypeOption.Int, SqlDataTypeOption.VarChar, ccl);
            conversions.Add(SqlDataTypeOption.Int, SqlDataTypeOption.NChar, ccl);
            conversions.Add(SqlDataTypeOption.Int, SqlDataTypeOption.NVarChar, ccl);

            conversions.Add(SqlDataTypeOption.Int, SqlDataTypeOption.TinyInt, no);
            conversions.Add(SqlDataTypeOption.Int, SqlDataTypeOption.SmallInt, no);            

            // from smallint
            conversions.Add(SqlDataTypeOption.SmallInt, SqlDataTypeOption.Char, ccl);
            conversions.Add(SqlDataTypeOption.SmallInt, SqlDataTypeOption.VarChar, ccl);
            conversions.Add(SqlDataTypeOption.SmallInt, SqlDataTypeOption.NChar, ccl);
            conversions.Add(SqlDataTypeOption.SmallInt, SqlDataTypeOption.NVarChar, ccl);

            // from bigint
            conversions.Add(SqlDataTypeOption.BigInt, SqlDataTypeOption.Char, ccl);
            conversions.Add(SqlDataTypeOption.BigInt, SqlDataTypeOption.VarChar, ccl);
            conversions.Add(SqlDataTypeOption.BigInt, SqlDataTypeOption.NChar, ccl);
            conversions.Add(SqlDataTypeOption.BigInt, SqlDataTypeOption.NVarChar, ccl);

            return conversions;
        }

        private static IDictionary<SqlDataTypeOption, int> precedenceTable = CreatePrecedenceTable();

        // Encodes the precedence rules in https://msdn.microsoft.com/en-us/library/ms190309.aspx
        private static IDictionary<SqlDataTypeOption, int> CreatePrecedenceTable()
        {            
            var table = new Dictionary<SqlDataTypeOption, int>();
            
            table[SqlDataTypeOption.None] = 0;
            
            // 1 == user defined
            table[SqlDataTypeOption.Sql_Variant] = 2;
            
            // 3 what happened to XML?
            table[SqlDataTypeOption.DateTimeOffset] = 4;
            table[SqlDataTypeOption.DateTime2] = 5;
            table[SqlDataTypeOption.DateTime] = 6;            
            table[SqlDataTypeOption.SmallDateTime] = 7;
            table[SqlDataTypeOption.Date] = 8;
            table[SqlDataTypeOption.Time] = 9;
            table[SqlDataTypeOption.Float] = 10;
            table[SqlDataTypeOption.Real] = 11;
            table[SqlDataTypeOption.Decimal] = 12;
            table[SqlDataTypeOption.Money] = 13;
            table[SqlDataTypeOption.SmallMoney] = 14;
            table[SqlDataTypeOption.BigInt] = 15;
            table[SqlDataTypeOption.Int] = 16;
            table[SqlDataTypeOption.SmallInt] = 17;
            table[SqlDataTypeOption.TinyInt] = 18;
            table[SqlDataTypeOption.Bit] = 19;
            table[SqlDataTypeOption.NText] = 20;
            table[SqlDataTypeOption.Text] = 21;
            table[SqlDataTypeOption.Image] = 22;
            table[SqlDataTypeOption.Timestamp] = 23;
            table[SqlDataTypeOption.UniqueIdentifier] = 24;
            table[SqlDataTypeOption.NVarChar] = 25;
            table[SqlDataTypeOption.NChar] = 26;
            table[SqlDataTypeOption.VarChar] = 27;
            table[SqlDataTypeOption.Char] = 28;
            table[SqlDataTypeOption.VarBinary] = 29;
            table[SqlDataTypeOption.Binary] = 30;
            return table;
        }

        private static IDictionary<SqlDataTypeOption, NumericTraits> numericLimitTable = CreateNumericLimitTable();

        // Encodes the size info from https://msdn.microsoft.com/en-us/library/ms187745.aspx
        private static IDictionary<SqlDataTypeOption, NumericTraits> CreateNumericLimitTable()
        {
            var table = new Dictionary<SqlDataTypeOption, NumericTraits>();

            table.Add(SqlDataTypeOption.Int, new NumericTraits(Int32.MinValue, Int32.MaxValue, sizeClass: 4));
            table.Add(SqlDataTypeOption.SmallInt, new NumericTraits(Int16.MinValue, Int16.MinValue, sizeClass: 2));
            return table;
        }

        internal static bool IsHigherPrecedence(SqlDataTypeOption t1, SqlDataTypeOption t2)
        {
            int precedenceOfType1 = 0;
            precedenceTable.TryGetValue(t1, out precedenceOfType1);
            int precedenceOfType2 = 0;
            precedenceTable.TryGetValue(t2, out precedenceOfType2);
            return precedenceOfType1 < precedenceOfType2;            
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