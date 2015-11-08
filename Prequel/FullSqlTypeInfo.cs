namespace Prequel
{
    using System;
    using System.Collections.Generic;
    using Microsoft.SqlServer.TransactSql.ScriptDom;

    /// <summary>
    /// For when we have a real SqlDataTypeReference for this type
    /// </summary>
    public class FullSqlTypeInfo : SqlTypeInfo
    {
        private SqlDataTypeReference DataType
        {
            get; set;
        }

        private SqlDataTypeOption TypeOption { get; set; }

        public override string TypeName
        {
            get { return TypeOption.ToString(); }
        }

        // If we assign type other to this, report any possible issues
        internal AssignmentResult CheckFullAssignment(int startLine, string variableName, FullSqlTypeInfo other)
        {
            var fromType = other.TypeOption;
            var toType = this.TypeOption;

            List<Warning> warnings = new List<Warning>();
            var conversionResult = TypeConversionHelper.GetConversionResult(fromType, toType);
            if (0 != (conversionResult & TypeConversionResult.CheckLength))
            {
                if (this.Length < other.Length)
                {
                    warnings.Add(Warning.StringTruncated(startLine, variableName, this.Length, other.Length));
                }
            }

            if (0 != (conversionResult & TypeConversionResult.CheckConvertedLength))
            {
                if (this.Length < other.Length)
                {
                    warnings.Add(Warning.ConvertToTooShortString(startLine, variableName, this.Length, other.Length));
                }
            }

            if (0 != (conversionResult & TypeConversionResult.Narrowing))
            {
                warnings.Add(Warning.StringConverted(startLine, variableName));
            }

            if (0 != (conversionResult & TypeConversionResult.ImplicitLossy))
            {
                warnings.Add(Warning.ImplicitConversion(startLine, variableName, toType.ToString(), fromType.ToString()));
            }

            return new AssignmentResult(warnings.Count == 0, warnings);
        }

        private static bool IsStringLike(SqlDataTypeOption typeOption)
        {
            return typeOption == SqlDataTypeOption.Char ||
                typeOption == SqlDataTypeOption.VarChar ||
                typeOption == SqlDataTypeOption.NChar ||
                typeOption == SqlDataTypeOption.NVarChar;
        }

        internal SqlTypeInfo GetHigherPrecedenceType(FullSqlTypeInfo rightType)
        {
            if (TypeConversionHelper.IsHigherPrecedence(this.TypeOption, rightType.TypeOption))
            {
                return this;
            }

            return rightType;
        }

        internal FullSqlTypeInfo(SqlDataTypeReference dataType)
        {
            DataType = dataType;

            this.TypeOption = dataType.SqlDataTypeOption;

            Length = GetMaxLengthOfStringVariable(dataType);
        }

        private int Length { get; set; } = -1;

        public override bool IsImplicitLengthString()
        {
            if (!IsStringLike(DataType.SqlDataTypeOption))
            {
                return false;
            }

            return ExplicitLength(DataType) == null;
        }

        private static int? ExplicitLength(SqlDataTypeReference typeReference)
        {
            foreach (var param in typeReference.Parameters)
            {
                if (param.LiteralType == LiteralType.Integer)
                {
                    return Convert.ToInt32(param.Value);
                }
                else if (param.LiteralType == LiteralType.Max)
                {
                    return int.MaxValue; // isn't it different for char(max)?                    
                }
            }

            return null;
        }

        private static int GetMaxLengthOfStringVariable(SqlDataTypeReference typeReference)
        {
            int length = -1;
            if (IsStringLike(typeReference.SqlDataTypeOption))
            {
                length = ExplicitLength(typeReference) ?? 1;
            }

            switch (typeReference.SqlDataTypeOption)
            {
                case SqlDataTypeOption.Int:
                    length = int.MinValue.ToString().Length;
                    break;
                case SqlDataTypeOption.SmallInt:
                    length = short.MinValue.ToString().Length;
                    break;
            }

            return length;
        }
    }
}