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
        private static IDictionary<SqlDataTypeOption, int> indexOfDTO = CreateDtoIndex();

        private static IDictionary<SqlDataTypeOption, int> CreateDtoIndex()
        {
            var indexes = new Dictionary<SqlDataTypeOption, int>();
            indexes[SqlDataTypeOption.Binary] = 0;
            indexes[SqlDataTypeOption.VarBinary] = 1;
            indexes[SqlDataTypeOption.Char] = 2;
            indexes[SqlDataTypeOption.VarChar] = 3;
            indexes[SqlDataTypeOption.NChar] = 4;
            indexes[SqlDataTypeOption.NVarChar] = 5;
            indexes[SqlDataTypeOption.DateTime] = 6;
            indexes[SqlDataTypeOption.SmallDateTime] = 7;
            indexes[SqlDataTypeOption.Date] = 8;
            indexes[SqlDataTypeOption.Time] = 9;
            indexes[SqlDataTypeOption.DateTimeOffset] = 10;
            indexes[SqlDataTypeOption.DateTime2] = 11;
            indexes[SqlDataTypeOption.Decimal] = 12;
            indexes[SqlDataTypeOption.Numeric] = 13;
            indexes[SqlDataTypeOption.Float] = 14;
            indexes[SqlDataTypeOption.Real] = 15;
            indexes[SqlDataTypeOption.BigInt] = 16;
            indexes[SqlDataTypeOption.Int] = 17;
            indexes[SqlDataTypeOption.SmallInt] = 18;
            indexes[SqlDataTypeOption.TinyInt] = 19;
            indexes[SqlDataTypeOption.Money] = 20;
            indexes[SqlDataTypeOption.SmallMoney] = 21;
            indexes[SqlDataTypeOption.Bit] = 22;
            indexes[SqlDataTypeOption.Timestamp] = 23;
            indexes[SqlDataTypeOption.UniqueIdentifier] = 24;
            indexes[SqlDataTypeOption.Image] = 25;
            indexes[SqlDataTypeOption.NText] = 26;
            indexes[SqlDataTypeOption.Text] = 27;
            indexes[SqlDataTypeOption.Sql_Variant] = 28;
            // TODO: what's the deal with Xml? Not in SqlDataTypeOption = 29
            // TODO: what's the deal with CLR UDT? = 30
            // TODO: hierarchyId = 31
            return indexes;
        }

        private static int GetDtoIndex(SqlDataTypeOption dto)
        {
            return indexOfDTO[dto];
        }

        // Encodes the table in https://msdn.microsoft.com/en-us/library/ms191530.aspx showing which types can be converted 
        private static TypeConversionResult[,] conversionTableArray = CreateConversionTableArray();

        private static TypeConversionResult[,] CreateConversionTableArray()
        {
            var cl = TypeConversionResult.CheckLength;
            var nr = TypeConversionResult.Narrowing;
            var ln = TypeConversionResult.CheckLength | TypeConversionResult.Narrowing;
            var il = TypeConversionResult.ImplicitLossy;
            var cc = TypeConversionResult.CheckConvertedLength;
            var no = TypeConversionResult.NumericOverflow;
            var ok = TypeConversionResult.ImplicitSafe;
            var __ = TypeConversionResult.NotImplemented;  // self-conversion
            var ni = TypeConversionResult.NotImplemented;

            const int nTypes = 32;
            var conversions = new TypeConversionResult[nTypes, nTypes]
            {
                
#pragma warning disable SA1005 // Single line comments must begin with single space
                //                 to->   b   vb  ch  vc  nc  nv  dt  sd  d   t   do  d2  dc  nm  fl  re  bi  i   si  ti  $$  s$  bt  ts  ui  im  nt  tx  sv  xm  cu  hi
                // from
                /* binary            */ { __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* varbinary         */ { ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* char              */ { ni, ni, cl, cl, cl, cl, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* varchar           */ { ni, ni, cl, cl, cl, cl, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* nchar             */ { ni, ni, ln, ln, cl, cl, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, il, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* nvarchar          */ { ni, ni, ln, ln, cl, cl, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* datetime          */ { ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* smalldatetime     */ { ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* date              */ { ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* time              */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* datetimeoffset    */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* datetime2         */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* decimal           */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* numeric           */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* float             */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* real              */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* bigint            */ { ni, ni, cc, cc, cc, cc, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* int               */ { ni, ni, cc, cc, cc, cc, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, no, no, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* smallint          */ { ni, ni, cc, cc, cc, cc, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* tinyint           */ { ni, ni, cc, cc, cc, cc, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* money             */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* smallmoney        */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* bit               */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni, ni},
                /* timestamp         */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni, ni},
                /* uniqueid          */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni, ni},
                /* image             */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni, ni},
                /* ntext             */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni, ni},
                /* text              */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni, ni},
                /* sql_variant       */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni, ni},
                /* xml               */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni, ni},
                /* clr udt           */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __, ni},
                /* hierarchyid       */ { ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, ni, __},
            };

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
            try
            {
                int fromIndex = GetDtoIndex(from);
                int toIndex = GetDtoIndex(to);
                result = conversionTableArray[fromIndex, toIndex];
            }
            catch(KeyNotFoundException)
            {
                // an unknown sql data type option - default not implemented
                return result;
            }

            return result;
        }

        public static void Add(this IDictionary<Tuple<SqlDataTypeOption, SqlDataTypeOption>, TypeConversionResult> dictionary, SqlDataTypeOption from, SqlDataTypeOption to, TypeConversionResult result)
        {
            dictionary.Add(new Tuple<SqlDataTypeOption, SqlDataTypeOption>(from, to), result);
        }
    }
}