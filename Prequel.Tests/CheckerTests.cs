using System;
using Xunit;

namespace Prequel.Tests
{
    

    public class CheckerTests
    {
        [Fact]
        public void DoNothingTest()
        {
            Checker c = new Checker(new Arguments(new string[] { "hello" }));
            c.Run();
        }
    }
}
