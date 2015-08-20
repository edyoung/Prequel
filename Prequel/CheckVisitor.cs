using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;

namespace Prequel
{
    internal class CheckVisitor : TSqlFragmentVisitor
    {
        private IDictionary<string, Variable> DeclaredVariables { get; set; }
        public IList<Warning> Warnings { get; private set; }
        private bool inExecuteParameter;

        public CheckVisitor() 
        {
            Warnings = new List<Warning>();
            DeclaredVariables = new Dictionary<string, Variable>(StringComparer.OrdinalIgnoreCase);
        }

        public override void ExplicitVisit(DeclareVariableElement node)
        {
            DeclaredVariables[node.VariableName.Value] = new Variable() { Node = node.VariableName };
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(DeclareTableVariableBody node)
        {
            DeclaredVariables[node.VariableName.Value] = new Variable() { Node = node.VariableName };
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(ProcedureParameter node)
        {
            DeclaredVariables[node.VariableName.Value] = new Variable() { Node = node.VariableName };
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(ExecuteParameter node)
        {
            inExecuteParameter = true;
            base.ExplicitVisit(node);
            inExecuteParameter = false;
        }

        public override void ExplicitVisit(VariableReference node)
        {
            // exec foo @param = value is allowed without being declared, though really it should be checked against params of foo
            if (!inExecuteParameter)
            {
                string targetVariable = node.Name;
                Variable target;
                if (DeclaredVariables.TryGetValue(targetVariable, out target))
                {
                    target.Referenced = true;
                }
                else
                {
                    Warnings.Add(new Warning(node.StartLine, WarningID.UndeclaredVariableUsed, String.Format("Variable {0} used before being declared", targetVariable)));
                }
            }
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(TSqlBatch batch)
        {
            DeclaredVariables.Clear(); // clear local vars from any previous batch
            base.ExplicitVisit(batch);
            LogUnreferencedVariables();
        }

        private void LogUnreferencedVariables()
        {
            foreach(var kv in DeclaredVariables)
            {
                if(!kv.Value.Referenced)
                {
                    TSqlFragment node = kv.Value.Node;
                    Warnings.Add(new Warning(node.StartLine, WarningID.UnusedVariableDeclared, String.Format("Variable {0} declared but never used", kv.Key)));
                }
            }
        }
    }
}