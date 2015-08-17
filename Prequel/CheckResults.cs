using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prequel
{
    public class CheckResults
    {
        public CheckResults(Input input, IList<ParseError> errors, IList<Warning> warnings)
        {
            this.Errors = errors;
            if(! (errors.Count == 0))
            {
                ExitCode = 1;
            }
            this.Warnings = warnings;
            this.input = input;
        }
        private Input input;
        public IList<ParseError> Errors { get; set; }
        public int ExitCode { get; set; }
        public IList<Warning> Warnings { get; set; }

        public string FormatError(ParseError error)
        {
            return String.Format("{0}({1}) : ERROR {2} : {3}", input.Path, error.Line, error.Number, error.Message);
        }

        public string FormatWarning(Warning warning)
        {
            return String.Format("{0}({1}) : WARNING {2} : {3}", input.Path, warning.Line, warning.Number, warning.Message);
        }
    }
}
