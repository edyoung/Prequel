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
            for (int id = WarningInfo.MinWarningID; id <= WarningInfo.MaxWarningID ; id++)
            {
                WarningID warningID = (WarningID)id;
                Assert.Equal(warningID, Warning.WarningTypes[warningID].ID); // make sure every warning is in the map and has correct id
            }

            // make sure min and max are up-to-date
            Assert.Equal(Warning.WarningTypes.Count, (WarningInfo.MaxWarningID - WarningInfo.MinWarningID) + 1);
        }
    }
}
