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
        public CheckResults(IList<ParseError> errors, IList<Warning> warnings)
        {
            this.Errors = errors;
            if(! (errors.Count == 0))
            {
                ExitCode = 1;
            }
            this.Warnings = warnings;
        }
        public IList<ParseError> Errors { get; set; }
        public int ExitCode { get; set; }
        public IList<Warning> Warnings { get; set; }
    }
}
