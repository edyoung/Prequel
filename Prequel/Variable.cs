namespace Prequel
{
    using Microsoft.SqlServer.TransactSql.ScriptDom;

    internal class Variable
    {
        // initially unset, set to true if some other statement dereferences it
        internal bool Referenced { get; set; }

        // where the variable was defined
        internal Identifier Node { get; set; }

        // a convenient version of the bits of the SQL typesystem we (currently) care about
        internal SqlTypeInfo SqlTypeInfo { get; set;  }

        public Variable(DataTypeReference dataType)
        {
            SqlTypeInfo = new SqlTypeInfo(dataType);
        }
    }
}