namespace Prequel
{
    using System;
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

            if (IsStringLike(this.TypeOption.Value) && IsStringLike(other.TypeOption.Value))
            {
                if (this.Length < other.Length)
                {
                    var result = new AssignmentResult(false);
                    result.Warnings.Add(Warning.StringTruncated(startLine, variableName, this.Length, other.Length));
                    return result;
                }

                if (IsNarrowString(this.TypeOption.Value) && IsWideString(other.TypeOption.Value))
                {
                    var result = new AssignmentResult(false);
                    result.Warnings.Add(Warning.StringConverted(startLine, variableName));
                    return result;
                }

                return AssignmentResult.OK;
            }

            throw new NotImplementedException();
        }

        private bool IsWideString(SqlDataTypeOption typeOption)
        {
            return typeOption == SqlDataTypeOption.NChar || typeOption == SqlDataTypeOption.NVarChar;
        }

        private bool IsNarrowString(SqlDataTypeOption typeOption)
        {
            return typeOption == SqlDataTypeOption.Char || typeOption == SqlDataTypeOption.VarChar;
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
            Length = -1;

            var sqlDataType = dataType as SqlDataTypeReference;
            if (null != sqlDataType)
            {
                this.TypeOption = sqlDataType.SqlDataTypeOption;

                Length = GetMaxLengthOfStringVariable(sqlDataType);
            }
        }
        
        private SqlDataTypeOption? TypeOption { get; set; }

        public int Length { get; private set; }        

        private int GetMaxLengthOfStringVariable(SqlDataTypeReference typeReference)
        {
            int length = -1;
            if (IsStringLike(typeReference.SqlDataTypeOption))
            {
                bool foundLength = false;
                foreach (var param in typeReference.Parameters)
                {
                    if (param.LiteralType == LiteralType.Integer)
                    {
                        length = Convert.ToInt32(param.Value);
                        foundLength = true;
                    }
                    else if (param.LiteralType == LiteralType.Max)
                    {
                        length = int.MaxValue; // isn't it different for char(max)?
                        foundLength = true;
                    }
                }

                if (!foundLength)
                {
                    // TODO: is this really right? 
                    length = 1;
                }
            }

            return length;
        }
    }    
}