namespace Prequel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.SqlServer.TransactSql.ScriptDom;

    /// <summary>
    /// The top-level object which checks a set of sql files
    /// </summary>
    public class Checker
    {
        private Arguments arguments;

        public Checker(Arguments arguments)
        {
            this.arguments = arguments;
        }

        public CheckResults Run()
        {
            var parser = (TSqlParser)Activator.CreateInstance(arguments.SqlParserType, new object[] { true });

            Input input = arguments.Inputs[0];
            try
            {
                TextReader reader = new StreamReader(input.Stream);

                IList<ParseError> errors;
                TSqlFragment sqlFragment = parser.Parse(reader, out errors);

                CheckVisitor checkVisitor = new CheckVisitor();
                sqlFragment.Accept(checkVisitor);

                checkVisitor.FilterWarnings(arguments.WarningLevel);
                return new CheckResults(input, errors, checkVisitor.Warnings);
            }
            catch (IOException ex)
            {
                throw new ProgramTerminatingException(
                    string.Format("Error reading file {0}: {1}", input.Path, ex.Message), ex, ExitReason.IOError);
            }
        }
    }
}