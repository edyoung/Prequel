using System;
using Xunit;

namespace Prequel.Tests
{       
    public class CheckerTests
    {
        [Fact]
        public void DoNothingTest()
        {
            Checker c = new Checker(new Arguments("test.sql"));
            c.Run();
        }
    }
}
