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
            Assert.True(result.IsOK);
        }        

        [Fact]
        public void ShortStringCanBeAssignedToLongString()
        {
            SqlTypeInfo longStringInfo = new SqlTypeInfo(CharOfLength(10));
            SqlTypeInfo shortStringInfo = new SqlTypeInfo(CharOfLength(5));

            AssignmentResult result = longStringInfo.CheckAssignment(shortStringInfo);
            Assert.True(result.IsOK);

        }

        [Fact]
        public void LongStringCannotBeAssignedToShortString()
        {
            SqlTypeInfo longStringInfo = new SqlTypeInfo(CharOfLength(10));
            SqlTypeInfo shortStringInfo = new SqlTypeInfo(CharOfLength(5));

            AssignmentResult result = shortStringInfo.CheckAssignment(longStringInfo);
            Assert.False(result.IsOK);
            Assert.Contains(WarningID.StringTruncated, result.Warnings);
        }

        [Fact]
        public void MaxStringCanBeAssignedFromShortString()
        {
            SqlTypeInfo longStringInfo = new SqlTypeInfo(CharOfMaxLength());
            SqlTypeInfo shortStringInfo = new SqlTypeInfo(CharOfLength(5));

            AssignmentResult result = shortStringInfo.CheckAssignment(longStringInfo);
            Assert.False(result.IsOK);
        }

        private static SqlDataTypeReference CharOfLength(int len)
        {
            var dataRef = new SqlDataTypeReference()
            {
                SqlDataTypeOption = SqlDataTypeOption.Char
            };

            dataRef.Parameters.Add(new IntegerLiteral() { Value = len.ToString() });
            return dataRef;
        }

        private static SqlDataTypeReference CharOfMaxLength()
        {
            var dataRef = new SqlDataTypeReference()
            {
                SqlDataTypeOption = SqlDataTypeOption.Char
            };

            dataRef.Parameters.Add(new MaxLiteral());
            return dataRef;
        }
    }
}
