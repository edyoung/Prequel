namespace Prequel
{
    using Microsoft.SqlServer.TransactSql.ScriptDom;

    internal class Variable
    {
        // initially unset, set to true if some other statement dereferences it
        internal bool Referenced { get; set; }

        // where the variable was defined
        internal Identifier Node { get; set; }
    }
}