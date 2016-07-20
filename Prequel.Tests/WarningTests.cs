using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

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
            foreach(var id in System.Enum.GetValues(typeof(WarningID)))
            {
                WarningID warningID = (WarningID)id;
                warningID.Should().Be(Warning.WarningTypes[warningID].ID); // make sure every warning is in the map and has correct id
            }
        }
    }
}
