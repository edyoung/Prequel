namespace Prequel
{
    using System;
    using System.Collections.Generic;
    using Microsoft.SqlServer.TransactSql.ScriptDom;

    /// <summary>
    /// Summarizes SQL's type info in a handier form.
    /// </summary>
    public class SqlTypeInfo
    {
        public DataTypeReference DataType
        {
            get; private set;
        }

        // If we assign type other to this, report any possible issues
        public AssignmentResult CheckAssignment(int startLine, string variableName, SqlTypeInfo other)
        {
            if (this == SqlTypeInfo.Unknown)
            {
                return AssignmentResult.OK;
            }

            if (other == SqlTypeInfo.Unknown)
            {
                return AssignmentResult.OK;
            }

            if (this.TypeOption == null || other.TypeOption == null)
            {
                // this is not correct, but we don't implement these type checks currently,
                // so fall back to claiming it's OK
                return AssignmentResult.OK;
            }

            var fromType = other.TypeOption.Value;
            var toType = this.TypeOption.Value;

            List<Warning> warnings = new List<Warning>();
            var conversionResult = TypeConversionHelper.GetConversionResult(fromType, toType);
            if (0 != (conversionResult & TypeConversionResult.CheckLength))
            {
                if (this.Length < other.Length)
                {
                    warnings.Add(Warning.StringTruncated(startLine, variableName, this.Length, other.Length));
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

        /// <summary>
        /// A special value for when we don't know the value of the expression
        /// </summary>
        private static SqlTypeInfo unknown = new SqlTypeInfo(null);

        public static SqlTypeInfo Unknown
        {
            get { return unknown; }
        }

        // NB dataType can be null
        public SqlTypeInfo(DataTypeReference dataType)
        {
            DataType = dataType;

            var sqlDataType = dataType as SqlDataTypeReference;
            if (null != sqlDataType)
            {
                this.TypeOption = sqlDataType.SqlDataTypeOption;

                Length = GetMaxLengthOfStringVariable(sqlDataType);
            }
        }

        private SqlDataTypeOption? TypeOption { get; set; }

        private int Length { get; set; } = -1;

        public bool IsImplicitLengthString()
        {
            var sqlDataType = DataType as SqlDataTypeReference;
            if (null == DataType)
            {
                return false;
            }

            if (!IsStringLike(sqlDataType.SqlDataTypeOption))
            {
                return false;
            }

            return ExplicitLength(sqlDataType) == null;
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

            return length;
        }
    }
}