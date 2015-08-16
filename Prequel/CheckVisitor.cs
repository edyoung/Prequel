using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;

namespace Prequel
{
    internal class CheckVisitor : TSqlFragmentVisitor
    {
        private IDictionary<string, Variable> DeclaredVariables { get; set; }
        public IList<Warning> Warnings { get; private set; }
        public CheckVisitor() 
        {
            Warnings = new List<Warning>();
            DeclaredVariables = new Dictionary<string, Variable>();
        }

        public override void ExplicitVisit(DeclareVariableElement node)
        {
            DeclaredVariables[node.VariableName.Value] = new Variable();
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(ProcedureParameter node)
        {
            DeclaredVariables[node.VariableName.Value] = new Variable();
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(VariableReference node)
        {
            string targetVariable = node.Name;
            if (!DeclaredVariables.ContainsKey(targetVariable))
            {
                Warnings.Add(new Warning());
            }
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(TSqlBatch batch)
        {
            DeclaredVariables.Clear(); // clear local vars from any previous batch
            base.ExplicitVisit(batch);
        }
    }
}