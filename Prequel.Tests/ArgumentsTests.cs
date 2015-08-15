using System;
using Xunit;

namespace Prequel.Tests
{
    internal static class TestData
    {
        
    }

    public class ArgumentsTests
    {
        [Fact]
        public void NoArgumentsRaisesUsageException()
        {
            Assert.Throws<UsageException>(() => new Arguments(new string[] { }));
        }
    }
}
