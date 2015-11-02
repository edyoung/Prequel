namespace Prequel
{
    using System;
    using System.Collections.Generic;
    using Microsoft.SqlServer.TransactSql.ScriptDom;

    public class UnknownSqlTypeInfo : SqlTypeInfo
    {
        public UnknownSqlTypeInfo()
        {
        }

        public override string TypeName
        {
            get
            {
                return "Unknown";
            }
        }
    }
}