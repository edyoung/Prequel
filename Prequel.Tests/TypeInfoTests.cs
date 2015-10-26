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
        public void Assign_Unknown_To_Unknown_NoWarn()
        {
            AssignmentResult result = SqlTypeInfo.Unknown.CheckAssignment(0, "x", SqlTypeInfo.Unknown);
            Assert.True(result.IsOK);
        }

        [Fact]
        public void Assign_UnknownType_To_Known_NoWarn()
        {
            SqlTypeInfo knownTypeInfo = new SqlTypeInfo(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.Bit });
            AssignmentResult result = knownTypeInfo.CheckAssignment(0, "x", SqlTypeInfo.Unknown);
            Assert.True(result.IsOK);
        }

        [Fact]
        public void Assign_KnownType_To_Unknown_NoWarn()
        {
            SqlTypeInfo knownTypeInfo = new SqlTypeInfo(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.Bit });
            AssignmentResult result = SqlTypeInfo.Unknown.CheckAssignment(0, "x", knownTypeInfo);
            Assert.True(result.IsOK);
        }

        [Fact]
        public void Assign_ShortString_To_LongString_NoWarn()
        {
            SqlTypeInfo longStringInfo = new SqlTypeInfo(CharOfLength(10));
            SqlTypeInfo shortStringInfo = new SqlTypeInfo(CharOfLength(5));

            AssignmentResult result = longStringInfo.CheckAssignment(0, "x", shortStringInfo);
            Assert.True(result.IsOK);
        }

        [Fact]
        public void Assign_LongString_To_ShortString_Warns()
        {
            SqlTypeInfo longStringInfo = new SqlTypeInfo(CharOfLength(10));
            SqlTypeInfo shortStringInfo = new SqlTypeInfo(CharOfLength(5));

            AssignmentResult result = shortStringInfo.CheckAssignment(0, "x", longStringInfo);
            Assert.False(result.IsOK);
            Assert.Contains(result.Warnings, (w) => w.Number == WarningID.StringTruncated);
        }

        [Fact]
        public void Assign_MaxString_To_ShortString_Warns()
        {
            SqlTypeInfo maxStringInfo = new SqlTypeInfo(CharOfMaxLength());
            SqlTypeInfo shortStringInfo = new SqlTypeInfo(CharOfLength(5));

            AssignmentResult result = shortStringInfo.CheckAssignment(0, "x", maxStringInfo);
            Assert.False(result.IsOK);
        }

        [Fact]
        public void Assign_ShortString_To_MaxString_NoWarn()
        {
            SqlTypeInfo maxStringInfo = new SqlTypeInfo(CharOfMaxLength());
            SqlTypeInfo shortStringInfo = new SqlTypeInfo(CharOfLength(5));

            AssignmentResult result = maxStringInfo.CheckAssignment(0, "x", shortStringInfo);
            Assert.True(result.IsOK);
        }

        [Fact]
        public void Assign_NarrowString_To_WideString_NoWarn()
        {
            SqlTypeInfo wideStringInfo = new SqlTypeInfo(NCharOfLength(10));
            SqlTypeInfo narrowStringInfo = new SqlTypeInfo(CharOfLength(10));

            AssignmentResult result = wideStringInfo.CheckAssignment(0, "x", narrowStringInfo);
            Assert.True(result.IsOK);
        }

        [Fact]
        public void Assign_WideString_To_NarrowString_Warns()
        {
            SqlTypeInfo wideStringInfo = new SqlTypeInfo(NCharOfLength(10));
            SqlTypeInfo narrowStringInfo = new SqlTypeInfo(CharOfLength(10));

            AssignmentResult result = narrowStringInfo.CheckAssignment(0, "x", wideStringInfo);
            Assert.False(result.IsOK);
            Assert.Contains(result.Warnings, (w) => w.Number == WarningID.StringConverted);
        }

        [Fact]
        public void Assign_LongWideString_To_NarrowShortString_WarnsTwice()
        {
            SqlTypeInfo wideStringInfo = new SqlTypeInfo(NCharOfLength(20));
            SqlTypeInfo narrowStringInfo = new SqlTypeInfo(CharOfLength(10));

            AssignmentResult result = narrowStringInfo.CheckAssignment(0, "x", wideStringInfo);
            Assert.False(result.IsOK);
            Assert.Contains(result.Warnings, (w) => w.Number == WarningID.StringConverted);
            Assert.Contains(result.Warnings, (w) => w.Number == WarningID.StringTruncated);
        }

        [Fact]
        public void Assign_String_To_Int_Warns()
        {
            SqlTypeInfo intInfo = new SqlTypeInfo(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.Int });
            AssignmentResult result = intInfo.CheckAssignment(0, "x", new SqlTypeInfo(NCharOfLength(5)));
            Assert.False(result.IsOK);
            Assert.Contains(result.Warnings, (w) => w.Number == WarningID.ImplicitConversion);
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
