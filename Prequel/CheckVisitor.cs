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
            base.ExplicitVisit(node);
            CheckForValidAssignment(node.VariableName.Value, node.DataType, node.Value);
        }

        public override void ExplicitVisit(SetVariableStatement node)
        {
            base.ExplicitVisit(node);
            Variable target;
            if (DeclaredVariables.TryGetValue(node.Variable.Name, out target))
            {
                CheckForValidAssignment(node.Variable.Name, target.SqlTypeInfo.DataType, node.Expression);
            }
        }

        private void CheckForValidAssignment(string variableName, DataTypeReference dataType, ScalarExpression value)
        {
            SqlTypeInfo sourceType = GetTypeInfoForExpression(value);
            SqlTypeInfo targetType = GetTypeInfoForVariable(variableName);

            AssignmentResult result = targetType.CheckAssignment(sourceType);

            if (!result.IsOK)
            {
                foreach (var warning in result.Warnings)
                {
                    if (warning == WarningID.StringTruncated)
                    {
                        Warnings.Add(Warning.StringTruncated(value.StartLine, variableName, targetType.Length, sourceType.Length));
                    }
                    if (warning == WarningID.StringConverted)
                    {
                        Warnings.Add(Warning.StringConverted(value.StartLine, variableName));
                    }
                }
            }
        }

        private SqlTypeInfo GetTypeInfoForVariable(string variableName)
        {
            Variable variable;
            if (!DeclaredVariables.TryGetValue(variableName, out variable))
            {
                return SqlTypeInfo.Unknown; // can't find a variable declaration
            }

            return variable.SqlTypeInfo;
        }

        private SqlTypeInfo GetTypeInfoForExpression(ScalarExpression value)
        {
            if (null == value)
            {
                return SqlTypeInfo.Unknown; // no expression, nothing to check
            }

            var stringLiteralValue = value as StringLiteral;
            if (null != stringLiteralValue)
            {
                var dataRef = new SqlDataTypeReference()
                {
                    SqlDataTypeOption = stringLiteralValue.IsNational ? SqlDataTypeOption.NChar : SqlDataTypeOption.Char
                };
                dataRef.Parameters.Add(new IntegerLiteral() { Value = stringLiteralValue.Value.Length.ToString() }); // TODO: are there corner cases where C# length != SQL length?                
                return new SqlTypeInfo(dataRef);
            }

            var variableReference = value as VariableReference;
            if (null != variableReference)
            {
                Variable variable;
                if (DeclaredVariables.TryGetValue(variableReference.Name, out variable))
                {
                    return variable.SqlTypeInfo;
                }
            }

            return SqlTypeInfo.Unknown;
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