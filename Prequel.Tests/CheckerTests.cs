using System;
using Xunit;

namespace Prequel.Tests
{       
    public class CheckerTests
    {
        [Fact]
        public void DoNothing()
        {
            Checker c = new Checker(new Arguments("test.sql"));
            c.Run();
        }

        [Fact]
        public void ParseSimpleString()
        {
            Checker c = new Checker(new Arguments("/i:select * from foo"));
            var results = c.Run();
            Assert.Equal(0, results.ExitCode);
        }
    }
}
