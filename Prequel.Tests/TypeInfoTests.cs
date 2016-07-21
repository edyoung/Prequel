using Microsoft.SqlServer.TransactSql.ScriptDom;
using Xunit;
using FluentAssertions;

namespace Prequel.Tests
{
    public class TypeInfoTests
    {
        [Fact]
        public void Assign_Unknown_To_Unknown_NoWarn()
        {
            AssignmentResult result = SqlTypeInfo.Unknown.CheckAssignment(0, "x", SqlTypeInfo.Unknown);
            result.IsOK.Should().BeTrue();
        }

        [Fact]
        public void Assign_UnknownType_To_Known_NoWarn()
        {
            SqlTypeInfo knownTypeInfo = SqlTypeInfo.Create(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.Bit });
            AssignmentResult result = knownTypeInfo.CheckAssignment(0, "x", SqlTypeInfo.Unknown);
            result.IsOK.Should().BeTrue();
        }

        [Fact]
        public void Assign_KnownType_To_Unknown_NoWarn()
        {
            SqlTypeInfo knownTypeInfo = SqlTypeInfo.Create(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.Bit });
            AssignmentResult result = SqlTypeInfo.Unknown.CheckAssignment(0, "x", knownTypeInfo);
            result.IsOK.Should().BeTrue();
        }

        [Fact]
        public void Assign_ShortString_To_LongString_NoWarn()
        {            
            AssignmentResult result = Check(CharOfLength(10), CharOfLength(5));
            result.IsOK.Should().BeTrue();
        }

        [Fact]
        public void Assign_LongString_To_ShortString_Warns()
        {            
            AssignmentResult result = Check(CharOfLength(5), CharOfLength(10)); 
            result.IsOK.Should().BeFalse();
            Assert.Contains(result.Warnings, (w) => w.Number == WarningID.StringTruncated);
        }

        [Fact]
        public void Assign_MaxString_To_ShortString_Warns()
        {            
            AssignmentResult result = Check(CharOfLength(5), CharOfMaxLength());
            result.IsOK.Should().BeFalse();
        }

        [Fact]
        public void Assign_ShortString_To_MaxString_NoWarn()
        {
            AssignmentResult result = Check(CharOfMaxLength(), CharOfLength(5));
            result.IsOK.Should().BeTrue();
        }

        [Fact]
        public void Assign_NarrowString_To_WideString_NoWarn()
        {            
            AssignmentResult result = Check(NCharOfLength(10), CharOfLength(10));
            result.IsOK.Should().BeTrue();
        }

        [Fact]
        public void Assign_WideString_To_NarrowString_Warns()
        {            
            AssignmentResult result = Check(CharOfLength(10), NCharOfLength(10));
            result.IsOK.Should().BeFalse();
            result.Warnings.Should().Contain(w => w.Number == WarningID.StringConverted);
        }

        [Fact]
        public void Assign_LongWideString_To_NarrowShortString_WarnsTwice()
        {
            AssignmentResult result = Check(CharOfLength(5), NCharOfLength(10));
            result.IsOK.Should().BeFalse();
            result.Should().WarnAbout(WarningID.StringConverted);
            result.Should().WarnAbout(WarningID.StringTruncated);
        }

        [Fact]
        public void Assign_String_To_Int_Warns()
        {
            SqlTypeInfo intInfo = SqlTypeInfo.Create(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.Int });
            AssignmentResult result = Check(intInfo, NCharOfLength(5));
            result.IsOK.Should().BeFalse();
            result.Should().WarnAbout(WarningID.ImplicitConversion);
        }

        [Fact]
        public void Assign_Int_To_SmallInt_Warns()
        {
            SqlTypeInfo smallIntInfo = SqlTypeInfo.Create(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.SmallInt });
            SqlTypeInfo intInfo = SqlTypeInfo.Create(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.Int });
            AssignmentResult result = Check(smallIntInfo, intInfo);
            result.IsOK.Should().BeFalse();
            result.Should().WarnAbout(WarningID.NumericOverflow);
        }

        [Fact]
        public void Assign_Int_To_TinyInt_Warns()
        {
            SqlTypeInfo smallIntInfo = SqlTypeInfo.Create(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.TinyInt });
            SqlTypeInfo intInfo = SqlTypeInfo.Create(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.Int });
            AssignmentResult result = Check(smallIntInfo, intInfo);
            result.IsOK.Should().BeFalse();
            result.Should().WarnAbout(WarningID.NumericOverflow);
        }

        [Fact]
        public void Assign_SmallInt_To_Int_NoWarning()
        {
            SqlTypeInfo smallIntInfo = SqlTypeInfo.Create(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.SmallInt });
            SqlTypeInfo intInfo = SqlTypeInfo.Create(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.Int });
            AssignmentResult result = Check(intInfo, smallIntInfo);
            result.IsOK.Should().BeTrue(); 
        }

        [Fact]
        public void Assign_Int_To_BigInt_NoWarning()
        {
            SqlTypeInfo bigIntInfo = SqlTypeInfo.Create(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.BigInt });
            SqlTypeInfo intInfo = SqlTypeInfo.Create(new SqlDataTypeReference() { SqlDataTypeOption = SqlDataTypeOption.Int });
            AssignmentResult result = Check(bigIntInfo, intInfo);
            result.IsOK.Should().BeTrue();
        }

        private static AssignmentResult Check(SqlTypeInfo to, SqlTypeInfo from)
        {
            return to.CheckAssignment(0, "x", from);
        }

        private static SqlTypeInfo CharOfLength(int len)
        {            
            var dataRef = new SqlDataTypeReference()
            {
                SqlDataTypeOption = SqlDataTypeOption.Char
            };

            dataRef.Parameters.Add(new IntegerLiteral() { Value = len.ToString() });
            return SqlTypeInfo.Create(dataRef);
        }

        private static SqlTypeInfo NCharOfLength(int len)
        {
            var dataRef = new SqlDataTypeReference()
            {
                SqlDataTypeOption = SqlDataTypeOption.NChar
            };

            dataRef.Parameters.Add(new IntegerLiteral() { Value = len.ToString() });
            return SqlTypeInfo.Create(dataRef);
        }

        private static SqlTypeInfo CharOfMaxLength()
        {
            var dataRef = new SqlDataTypeReference()
            {
                SqlDataTypeOption = SqlDataTypeOption.Char
            };

            dataRef.Parameters.Add(new MaxLiteral());
            return SqlTypeInfo.Create(dataRef);
        }
    }
}
