namespace Prequel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.SqlServer.TransactSql.ScriptDom;

    /// <summary>
    /// The set of warnings and errors created by a checker
    /// </summary>
    public class CheckResults
    {
        public CheckResults(Input input, IList<ParseError> errors, IList<Warning> warnings)
        {
            this.Errors = errors;
            if (!(errors.Count == 0))
            {
                ExitCode = ExitReason.GeneralFailure;
            }

            this.Warnings = warnings;
            this.input = input;
        }

        private Input input;

        public IList<ParseError> Errors { get; set; }

        public ExitReason ExitCode { get; set; }

        public IList<Warning> Warnings { get; set; }

        public string FormatError(ParseError error)
        {
            return string.Format("{0}({1}) : ERROR {2} : {3}", input.Path, error.Line, error.Number, error.Message);
        }

        public string FormatWarning(Warning warning)
        {
            return string.Format("{0}({1}) : WARNING {2} : {3}", input.Path, warning.Line, (int)warning.Number, warning.Message);
        }
    }
}
