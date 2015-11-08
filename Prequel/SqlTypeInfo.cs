namespace Prequel
{
    using System;
    using System.Collections.Generic;
    using Microsoft.SqlServer.TransactSql.ScriptDom;

    /// <summary>
    /// Summarizes SQL's type info in a handier form.
    /// </summary>
    public abstract class SqlTypeInfo
    {
        /// <summary>
        /// A special value for when we don't know the value of the expression
        /// </summary>
        public static SqlTypeInfo Unknown { get; } = new UnknownSqlTypeInfo();

        // Factory to create the appropriate subtype
        public static SqlTypeInfo Create(DataTypeReference dataType)
        {
            if (dataType == null)
            {
                return Unknown;
            }

            var sqlDataType = dataType as SqlDataTypeReference;

            if (sqlDataType == null)
            {
                // we don't yet handle any other type - treat them as as unknown
                return Unknown;
            }

            return new FullSqlTypeInfo(sqlDataType);
        }
        
        public virtual bool IsImplicitLengthString()
        {
            return false;
        }

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

            var fullOther = (FullSqlTypeInfo)other;
            return ((FullSqlTypeInfo)this).CheckFullAssignment(startLine, variableName, fullOther);
        }

        public abstract string TypeName { get; }

        internal static SqlTypeInfo CreateFromBinaryExpression(SqlTypeInfo leftType, SqlTypeInfo rightType, BinaryExpressionType binaryExpressionType)
        {
            if (leftType == SqlTypeInfo.Unknown || rightType == SqlTypeInfo.Unknown)
            {
                return SqlTypeInfo.Unknown;
            }

            if (binaryExpressionType != BinaryExpressionType.Add)
            {
                return SqlTypeInfo.Unknown; // TODO: support other expressions
            }

            SqlTypeInfo higherPrecedenceType = ((FullSqlTypeInfo)leftType).GetHigherPrecedenceType((FullSqlTypeInfo)rightType);
            return higherPrecedenceType;
        }
    }
}