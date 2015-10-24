using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Prequel.Tests
{
    public class TypeInfoTests
    {
        [Fact]
        public void UnknownTypeInfoCanBeAssignedToUnknown()
        {
            AssignmentResult result = SqlTypeInfo.Unknown.CheckAssignment(SqlTypeInfo.Unknown);
            Assert.True(result.IsOK);
        }

        [Fact]
        public void UnknownTypeInfoCanBeAssignedToKnown()
        {
            SqlTypeInfo knownTypeInfo = new SqlTypeInfo(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.Bit });
            AssignmentResult result = knownTypeInfo.CheckAssignment(SqlTypeInfo.Unknown);
        }        
    }
}
