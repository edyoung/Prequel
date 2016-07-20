namespace Prequel
{
    using System.Collections.Generic;
    using Microsoft.SqlServer.TransactSql.ScriptDom;

    /// <summary>
    /// Holds all the currently known variables
    /// </summary>
    internal class VariableSet
    {
        private IDictionary<string, Variable> DeclaredVariables { get; set; } = new Dictionary<string, Variable>();

        public VariableSet()
        {            
        }

        public void Clear()
        {
            DeclaredVariables.Clear();
        }

        // If the variable is found, mark referenced and return true; if not found, false
        public bool TryReference(string name)
        {
            Variable target;
            if (DeclaredVariables.TryGetValue(name, out target))
            {
                target.Referenced = true;
                return true;
            }

            return false;            
        }

        public void Declare(DeclareVariableElement node)
        {
            DeclaredVariables[node.VariableName.Value] = new Variable(node.DataType) { Node = node.VariableName };
        }

        public void Declare(DeclareTableVariableBody node)
        {
            DeclaredVariables[node.VariableName.Value] = new Variable(null) { Node = node.VariableName };
        }

        public void Declare(SelectSetVariable node)
        {
            DeclaredVariables[node.Variable.Name] = new Variable(null) { Node = node };
        }

        public SqlTypeInfo GetTypeInfoIfPossible(string name)
        {
            Variable variable;
            if (!DeclaredVariables.TryGetValue(name, out variable))
            {
                return SqlTypeInfo.Unknown; // can't find a variable declaration
            }

            return variable.SqlTypeInfo;
        }

        public IEnumerable<KeyValuePair<string, Variable>> GetUnreferencedVariables()
        {
            foreach (var kv in DeclaredVariables)
            {
                if (!kv.Value.Referenced)
                {
                    yield return kv;                    
                }
            }
        }
    }
}
