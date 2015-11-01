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

        [Fact]
        public void AllStringToStringConversionsCheckLength()
        {
            var stringTypes = new SqlDataTypeOption[] { SqlDataTypeOption.Char, SqlDataTypeOption.NChar, SqlDataTypeOption.VarChar, SqlDataTypeOption.NVarChar };
            foreach(SqlDataTypeOption t1 in stringTypes)
            {
                foreach(SqlDataTypeOption t2 in stringTypes)
                {
                    var result = TypeConversionHelper.GetConversionResult(t1, t2);
                    Assert.False(0 == (result & TypeConversionResult.CheckLength), String.Format("converting {0} to {1} doesn't check length", t1, t2));
                }
            }
        }
    }
}
