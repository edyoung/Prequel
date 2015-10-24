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

        /// <summary>
        /// If we assign type other to this, report any possible issues
        /// </summary>
        public AssignmentResult CheckAssignment(SqlTypeInfo other)
        {
            if (this == SqlTypeInfo.Unknown)
            {
                return AssignmentResult.OK;
            }

            if (other == SqlTypeInfo.Unknown)
            {
                return AssignmentResult.OK;
            }

            throw new NotImplementedException();
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

        public SqlDataTypeOption? TypeOption { get; private set; }

        public int Length { get; private set; }        

        private int GetMaxLengthOfStringVariable(SqlDataTypeReference typeReference)
        {
            int length = -1;
            if (TypeOption == SqlDataTypeOption.Char ||
                TypeOption == SqlDataTypeOption.VarChar ||
                TypeOption == SqlDataTypeOption.NChar ||
                TypeOption == SqlDataTypeOption.NVarChar)
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