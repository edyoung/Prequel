using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;

namespace Prequel
{
    internal class CheckVisitor : TSqlFragmentVisitor
    {
        public IList<Warning> Warnings { get; private set; }
        public CheckVisitor() 
        {
            Warnings = new List<Warning>();
        }

        public override void ExplicitVisit(SetVariableStatement node)
        {
            string targetVariable = node.Variable.Name;
            if (targetVariable == "@undeclared")
            {
                Warnings.Add(new Warning());
            }
            base.ExplicitVisit(node);
        }
    }
}