using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Prequel
{
    internal class Variable
    {
        // initially unset, set to true if some other statement dereferences it
        internal bool Referenced { get; set; }

        // where the variable was defined
        internal DeclareVariableElement Node { get; set; }
    }
}