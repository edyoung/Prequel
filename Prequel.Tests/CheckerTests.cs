using System;
using Xunit;

namespace Prequel.Tests
{       
    public class CheckerTests
    {        
        [Fact]
        public void ParseSimpleString()
        {
            Checker c = new Checker(new Arguments("/i:select * from foo"));
            var results = c.Run();
            Assert.Empty(results.Errors);
            Assert.Equal(0, results.ExitCode);
        }

        [Fact]
        public void ParseInvalidStringProducesErrors()
        {
            Checker c = new Checker(new Arguments("/i:select >>>"));
            var results = c.Run();
            Assert.NotEmpty(results.Errors);
            Assert.Equal(1, results.ExitCode);
        }

        [Fact]
        public void ParseFile()
        {
            using (var t = new TempFile("select * from foo"))
            {
                Checker c = new Checker(new Arguments(t.FileName));
                var results = c.Run();
                Assert.Equal(0, results.ExitCode);
            }
        }

        [Fact]
        public void ParseInvalidFile()
        {
            using (var t = new TempFile("select >>>"))
            {
                Checker c = new Checker(new Arguments(t.FileName));
                var results = c.Run();
                Assert.NotEmpty(results.Errors);
                Assert.Equal(1, results.ExitCode);
            }
        }

        [Fact]
        public void FindUndeclaredVariableUse()
        {
            Checker c = new Checker(new Arguments("/i:set @undeclared = 1"));
            var results = c.Run();
            Assert.Equal(1, results.Warnings.Count);
            Assert.Equal(0, results.ExitCode);
        }
    }
}
