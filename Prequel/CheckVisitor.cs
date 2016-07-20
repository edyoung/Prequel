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
        private VariableSet Variables { get; } = new VariableSet();
        
        public IList<Warning> Warnings { get; private set; }

        private string executeParameterVariable;

        private bool noCountSet;

        public CheckVisitor()
        {
            Warnings = new List<Warning>();
        }

        public override void ExplicitVisit(DeclareVariableElement node)
        {
            Variables.Declare(node); 
            base.ExplicitVisit(node);
            CheckForValidAssignment(node.VariableName.Value, node.Value);
        }

        public override void ExplicitVisit(SetVariableStatement node)
        {
            base.ExplicitVisit(node);
            CheckForValidAssignment(node.Variable.Name, node.Expression);            
        }

        public override void ExplicitVisit(ConvertCall node)
        {
            base.ExplicitVisit(node);
            SqlTypeInfo targetType = SqlTypeInfo.Create(node.DataType);
            if (targetType.IsImplicitLengthString())
            {
                Warnings.Add(Warning.ConvertToVarCharOfUnspecifiedLength(node.StartLine, "CONVERT", targetType.TypeName));
            }
        }

        public override void ExplicitVisit(CastCall node)
        {
            base.ExplicitVisit(node);
            SqlTypeInfo targetType = SqlTypeInfo.Create(node.DataType);
            if (targetType.IsImplicitLengthString())
            {
                Warnings.Add(Warning.ConvertToVarCharOfUnspecifiedLength(node.StartLine, "CAST", targetType.TypeName));
            }
        }

        private void CheckForValidAssignment(string variableName, ScalarExpression value)
        {
            if (null == value)
            {
                return; // no actual assignment, nothing to do
            }

            SqlTypeInfo sourceType = GetTypeInfoForExpression(value);
            SqlTypeInfo targetType = GetTypeInfoForVariable(variableName);

            AssignmentResult result = targetType.CheckAssignment(value.StartLine, variableName, sourceType);
            
            if (!result.IsOK)
            {
                foreach (var warning in result.Warnings)
                {
                    Warnings.Add(warning);
                }
            }
        }

        private SqlTypeInfo GetTypeInfoForVariable(string variableName)
        {
            return Variables.GetTypeInfoIfPossible(variableName);            
        }

        private SqlTypeInfo GetTypeInfoForExpression(ScalarExpression value)
        {            
            // consider - we fake a datatype for the string literal, which is a bit ugly. Is there a better way?
            var stringLiteralValue = value as StringLiteral;
            if (null != stringLiteralValue)
            {
                var dataRef = new SqlDataTypeReference()
                {
                    SqlDataTypeOption = stringLiteralValue.IsNational ? SqlDataTypeOption.NChar : SqlDataTypeOption.Char
                };
                dataRef.Parameters.Add(new IntegerLiteral() { Value = stringLiteralValue.Value.Length.ToString() }); // TODO: are there corner cases where C# length != SQL length?                
                return SqlTypeInfo.Create(dataRef);
            }

            var variableReference = value as VariableReference;
            if (null != variableReference)
            {
                return Variables.GetTypeInfoIfPossible(variableReference.Name);                
            }

            var convertCall = value as ConvertCall;
            if (null != convertCall)
            {
                return SqlTypeInfo.Create(convertCall.DataType);
            }

            var castCall = value as CastCall;
            if (null != castCall)
            {
                return SqlTypeInfo.Create(castCall.DataType);
            }

            var binaryExpression = value as BinaryExpression;
            if (null != binaryExpression)
            {
                var leftType = GetTypeInfoForExpression(binaryExpression.FirstExpression);
                var rightType = GetTypeInfoForExpression(binaryExpression.SecondExpression);
                return SqlTypeInfo.CreateFromBinaryExpression(leftType, rightType, binaryExpression.BinaryExpressionType);
            }

            var parenthesisExpression = value as ParenthesisExpression;
            if (null != parenthesisExpression)
            {
                return GetTypeInfoForExpression(parenthesisExpression.Expression);
            }

            return SqlTypeInfo.Unknown;
        }

        public override void ExplicitVisit(DeclareTableVariableBody node)
        {
            Variables.Declare(node); 
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(SelectSetVariable node)
        {
            Variables.Declare(node);
            base.ExplicitVisit(node);
        }

        // Remove all the warnings which the user doesn't want to see
        internal void FilterWarnings(WarningLevel warningLevel)
        {
            this.Warnings = this.Warnings.Where(warning => warning.Info.Level <= warningLevel).ToList();
        }

        public override void ExplicitVisit(ProcedureParameter node)
        {
            Variables.Declare(node);
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
            
            if (!Variables.TryReference(targetVariable))
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
            Variables.Clear(); // clear local vars from any previous batch
            base.ExplicitVisit(batch);
            LogUnreferencedVariables();
        }

        private void LogUnreferencedVariables()
        {
            foreach (var kv in Variables.GetUnreferencedVariables())
            {
                TSqlFragment node = kv.Value.Node;
                Warnings.Add(Warning.UnusedVariableDeclared(node.StartLine, kv.Key));                
            }
        }
    }
}