using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prequel
{
    public class Arguments
    {
        private string[] args;

        public Arguments(string[] args)
        {
            if (args.Length == 0)
            {
                throw new UsageException("You must specify at least one file to check");
            }
            this.args = args;
        }
    }
}
