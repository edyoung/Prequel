using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Prequel.Tests
{
    public class TypeConversionTableTests
    {
        private static readonly SqlDataTypeOption[] stringTypes = new SqlDataTypeOption[] { SqlDataTypeOption.Char, SqlDataTypeOption.NChar, SqlDataTypeOption.VarChar, SqlDataTypeOption.NVarChar };

        private static readonly SqlDataTypeOption[] numericTypes = new SqlDataTypeOption[] {
            SqlDataTypeOption.BigInt,
            //SqlDataTypeOption.Decimal,
            //SqlDataTypeOption.Float,
            SqlDataTypeOption.Int,
            //SqlDataTypeOption.Money,
            //SqlDataTypeOption.Numeric,
            //SqlDataTypeOption.Real,
            SqlDataTypeOption.SmallInt,
            //SqlDataTypeOption.SmallMoney,
            //SqlDataTypeOption.TinyInt};
        };

        private static readonly SqlDataTypeOption[] allTypes = new SqlDataTypeOption[] {
            SqlDataTypeOption.Binary,
            SqlDataTypeOption.VarBinary,
            SqlDataTypeOption.Char,
            SqlDataTypeOption.VarChar,
            SqlDataTypeOption.NChar,
            SqlDataTypeOption.NVarChar,
            SqlDataTypeOption.DateTime,
            SqlDataTypeOption.SmallDateTime,
            SqlDataTypeOption.Date,
            SqlDataTypeOption.Time,
            SqlDataTypeOption.DateTimeOffset,
            SqlDataTypeOption.DateTime2,
            SqlDataTypeOption.Decimal,
            SqlDataTypeOption.Numeric,
            SqlDataTypeOption.Float,
            SqlDataTypeOption.Real,
            SqlDataTypeOption.BigInt,
            SqlDataTypeOption.Int,
            SqlDataTypeOption.SmallInt,
            SqlDataTypeOption.TinyInt,
            SqlDataTypeOption.Money,
            SqlDataTypeOption.SmallMoney,
            SqlDataTypeOption.Bit,
            SqlDataTypeOption.Timestamp,
            SqlDataTypeOption.UniqueIdentifier,
            SqlDataTypeOption.Image,
            SqlDataTypeOption.NText,
            SqlDataTypeOption.Text,
            SqlDataTypeOption.Sql_Variant
        };

        [Fact]
        public void AllStringToStringConversionsCheckLength()
        {
            foreach(SqlDataTypeOption t1 in stringTypes)
            {
                foreach(SqlDataTypeOption t2 in stringTypes)
                {
                    var result = TypeConversionHelper.GetConversionResult(t1, t2);
                    Assert.False(0 == (result & TypeConversionResult.CheckLength), String.Format("converting {0} to {1} doesn't check length", t1, t2));
                }
            }
        }

        [Fact]
        public void AllNumericToStringConversionsCheckConvertedLength()
        {
            foreach(SqlDataTypeOption numericType in numericTypes)
            {
                foreach(SqlDataTypeOption stringType in stringTypes)
                {
                    var result = TypeConversionHelper.GetConversionResult(numericType, stringType);
                    Assert.False(0 == (result & TypeConversionResult.CheckConvertedLength), String.Format("converting {0} to {1} doesn't check length", numericType, stringType));
                }
            }
        }
    }
}
