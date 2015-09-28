namespace Prequel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.SqlServer.TransactSql.ScriptDom;

    /// <summary>
    /// Visits all the sql nodes from the parser and performs the actual code analysis
    /// </summary>
    internal class CheckVisitor : TSqlFragmentVisitor
    {
        private IDictionary<string, Variable> DeclaredVariables { get; set; }

        public IList<Warning> Warnings { get; private set; }

        private string executeParameterVariable;

        private bool noCountSet;

        public CheckVisitor()
        {
            Warnings = new List<Warning>();
            DeclaredVariables = new Dictionary<string, Variable>(StringComparer.OrdinalIgnoreCase);
        }

        public override void ExplicitVisit(DeclareVariableElement node)
        {
            DeclaredVariables[node.VariableName.Value] = new Variable(node.DataType) { Node = node.VariableName };
            CheckForImplicitLength(node);
            base.ExplicitVisit(node);
            CheckForValidAssignment(node.VariableName.Value, node.DataType, node.Value);
        }

        public override void ExplicitVisit(SetVariableStatement node)
        {
            base.ExplicitVisit(node);
            Variable target = DeclaredVariables[node.Variable.Name];
            CheckForValidAssignment(node.Variable.Name, target.SqlTypeInfo.DataType, node.Expression);
        }

        private void CheckForValidAssignment(string variableName, DataTypeReference dataType, ScalarExpression value)
        {
            if (null == value)
            {
                return; // no expression, nothing to check
            }

            var stringLiteralValue = value as StringLiteral;
            if (null == stringLiteralValue)
            {
                return; // not a string literal, can't check
            }

            int sourceLength = stringLiteralValue.Value.Length; // any weird corner cases where C# length != SQL length?

            var typeReference = dataType as SqlDataTypeReference;

            if (typeReference == null)
            {
                return; // not a sql data type, can't check
            }

            int targetLength = DeclaredVariables[variableName].SqlTypeInfo.Length; 

            if (targetLength == -1)
            {
                return; // can't figure out how long the string is. TODO: shouldn't that be impossible in this case? 
            }

            if (targetLength < sourceLength)
            {
                Warnings.Add(Warning.StringTruncated(dataType.StartLine, variableName, targetLength, sourceLength));
            }
        }

        private void CheckForImplicitLength(DeclareVariableElement node)
        {
            var typeReference = node.DataType as SqlDataTypeReference;

            if (typeReference != null)
            {
                var typeOption = typeReference.SqlDataTypeOption;
                if (typeOption == SqlDataTypeOption.Char ||
                    typeOption == SqlDataTypeOption.VarChar ||
                    typeOption == SqlDataTypeOption.NChar ||
                    typeOption == SqlDataTypeOption.NVarChar)
                {
                    if (typeReference.Parameters.Count == 0)
                    {
                        // I believe the only valid for param for any of these types is the length, so if there's
                        // no params we haven't specified the length
                        Warnings.Add(Warning.CharVariableWithImplicitLength(node.StartLine, node.VariableName.Value));
                    }
                }
            }
        }

        public override void ExplicitVisit(DeclareTableVariableBody node)
        {
            DeclaredVariables[node.VariableName.Value] = new Variable(null) { Node = node.VariableName };
            base.ExplicitVisit(node);
        }

        // Remove all the warnings which the user doesn't want to see
        internal void FilterWarnings(WarningLevel warningLevel)
        {
            this.Warnings = this.Warnings.Where(warning => warning.Info.Level <= warningLevel).ToList();
        }

        public override void ExplicitVisit(ProcedureParameter node)
        {
            DeclaredVariables[node.VariableName.Value] = new Variable(node.DataType) { Node = node.VariableName };
            CheckForImplicitLength(node);
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(ExecuteParameter node)
        {
            if (node.Variable != null)
            {
                executeParameterVariable = node.Variable.Name;
            }

            base.ExplicitVisit(node);
            executeParameterVariable = null;
        }

        public override void ExplicitVisit(CreateProcedureStatement node)
        {
            var currentProcedure = node.ProcedureReference.Name.BaseIdentifier.Value;

            if (currentProcedure.StartsWith("sp_"))
            {
                Warnings.Add(Warning.ProcedureWithSPPrefix(node.StartLine, currentProcedure));
            }

            this.noCountSet = false;
            base.ExplicitVisit(node);
            if (!this.noCountSet)
            {
                Warnings.Add(Warning.ProcedureWithoutNoCount(node.StartLine, currentProcedure));
            }
        }

        public override void ExplicitVisit(PredicateSetStatement node)
        {
            if (node.Options.HasFlag(SetOptions.NoCount) && node.IsOn)
            {
                // we found a 'set nocount on' in this sproc
                this.noCountSet = true;
            }

            base.ExplicitVisit(node);
        }

        private void CheckVariableReference(VariableReference node)
        {
            string targetVariable = node.Name;

            if (string.Equals(targetVariable, executeParameterVariable, StringComparison.OrdinalIgnoreCase))
            {
                // in "exec foo @param = value", @param is allowed without being declared, though really it should be checked against params of foo
                return;
            }

            Variable target;
            if (DeclaredVariables.TryGetValue(targetVariable, out target))
            {
                target.Referenced = true;
            }
            else
            {
                Warnings.Add(Warning.UndeclaredVariableUsed(node.StartLine, targetVariable));
            }
        }

        public override void ExplicitVisit(VariableReference node)
        {
            CheckVariableReference(node);
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
            foreach (var kv in DeclaredVariables)
            {
                if (!kv.Value.Referenced)
                {
                    TSqlFragment node = kv.Value.Node;
                    Warnings.Add(Warning.UnusedVariableDeclared(node.StartLine, kv.Key));
                }
            }
        }
    }
}