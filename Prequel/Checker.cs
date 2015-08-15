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

            var parser = new TSql120Parser(false);

            TextReader reader = new StringReader("Select * from");

            IList<ParseError> errors;
            TSqlFragment sqlFragment = parser.Parse(reader, out errors);

            return new CheckResults();
        }
    }
}