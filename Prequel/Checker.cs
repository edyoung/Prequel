using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;

namespace Prequel
{
    public class Checker
    {
        private Arguments arguments;

        public Checker(Arguments arguments)
        {
            this.arguments = arguments;
        }

        public CheckResults Run()
        {
            var parser = (TSqlParser)Activator.CreateInstance(arguments.SqlParserType,new object[] { true });

            var input = arguments.Inputs[0];
            TextReader reader = new StreamReader(input.Stream);

            IList<ParseError> errors;
            TSqlFragment sqlFragment = parser.Parse(reader, out errors);

            CheckVisitor checkVisitor = new CheckVisitor();
            sqlFragment.Accept(checkVisitor);

            return new CheckResults(input, errors, checkVisitor.Warnings);
        }
    }
}