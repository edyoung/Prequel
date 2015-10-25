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
            AssignmentResult result = SqlTypeInfo.Unknown.CheckAssignment(0, "x", SqlTypeInfo.Unknown);
            Assert.True(result.IsOK);
        }

        [Fact]
        public void UnknownTypeInfoCanBeAssignedToKnown()
        {
            SqlTypeInfo knownTypeInfo = new SqlTypeInfo(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.Bit });
            AssignmentResult result = knownTypeInfo.CheckAssignment(0, "x", SqlTypeInfo.Unknown);
            Assert.True(result.IsOK);
        }        

        [Fact]
        public void ShortStringCanBeAssignedToLongString()
        {
            SqlTypeInfo longStringInfo = new SqlTypeInfo(CharOfLength(10));
            SqlTypeInfo shortStringInfo = new SqlTypeInfo(CharOfLength(5));

            AssignmentResult result = longStringInfo.CheckAssignment(0, "x", shortStringInfo);
            Assert.True(result.IsOK);

        }

        [Fact]
        public void LongStringCannotBeAssignedToShortString()
        {
            SqlTypeInfo longStringInfo = new SqlTypeInfo(CharOfLength(10));
            SqlTypeInfo shortStringInfo = new SqlTypeInfo(CharOfLength(5));

            AssignmentResult result = shortStringInfo.CheckAssignment(0, "x", longStringInfo);
            Assert.False(result.IsOK);
            Assert.Contains(result.Warnings, (w) => w.Number == WarningID.StringTruncated);
        }

        [Fact]
        public void ShortStringCannotBeAssignedFromMaxString()
        {
            SqlTypeInfo maxStringInfo = new SqlTypeInfo(CharOfMaxLength());
            SqlTypeInfo shortStringInfo = new SqlTypeInfo(CharOfLength(5));

            AssignmentResult result = shortStringInfo.CheckAssignment(0, "x", maxStringInfo);
            Assert.False(result.IsOK);
        }

        [Fact]
        public void MaxStringCanBeAssignedFromMaxString()
        {
            SqlTypeInfo maxStringInfo = new SqlTypeInfo(CharOfMaxLength());
            SqlTypeInfo shortStringInfo = new SqlTypeInfo(CharOfLength(5));

            AssignmentResult result = maxStringInfo.CheckAssignment(0, "x", shortStringInfo);
            Assert.True(result.IsOK);
        }

        [Fact]
        public void WideStringCanBeAssignedFromNarrowString()
        {
            SqlTypeInfo wideStringInfo = new SqlTypeInfo(NCharOfLength(10));
            SqlTypeInfo narrowStringInfo = new SqlTypeInfo(CharOfLength(10));

            AssignmentResult result = wideStringInfo.CheckAssignment(0, "x", narrowStringInfo);
            Assert.True(result.IsOK);
        }

        [Fact]
        public void NarrowStringCannotBeAssignedFromWideString()
        {
            SqlTypeInfo wideStringInfo = new SqlTypeInfo(NCharOfLength(10));
            SqlTypeInfo narrowStringInfo = new SqlTypeInfo(CharOfLength(10));

            AssignmentResult result = narrowStringInfo.CheckAssignment(0, "x", wideStringInfo);
            Assert.False(result.IsOK);
            Assert.Contains(result.Warnings, (w) => w.Number == WarningID.StringConverted);
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

        private static SqlDataTypeReference NCharOfLength(int len)
        {
            var dataRef = new SqlDataTypeReference()
            {
                SqlDataTypeOption = SqlDataTypeOption.NChar
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
