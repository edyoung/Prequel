using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;


namespace Prequel.Tests
{
    public class WarningTests
    {
        /// <summary>
        /// Check that the warning map contains all the IDs. Ideally should be enforced at compile time
        /// </summary>
        [Fact]
        public void AllWarningsInMap()
        {
            for (int id = (int)WarningID.Min; id < (int)WarningID.Max ; id++)
            {
                WarningID warningID = (WarningID)id;
                Assert.Equal(warningID, Warning.WarningTypes[warningID].ID); 
            }
        }
    }
}
