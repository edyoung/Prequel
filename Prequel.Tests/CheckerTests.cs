using System;
using Xunit;

namespace Prequel.Tests
{       
    public static class MyAssert
    {
        public static void NoErrorsOrWarnings(CheckResults results)
        {
            Assert.Empty(results.Errors);
            Assert.Empty(results.Warnings);
            Assert.Equal(0, results.ExitCode);
        }
    }
    public class CheckerTests
    {        
        [Fact]
        public void ParseSimpleString()
        {
            Checker c = new Checker(new Arguments("/i:select * from foo"));
            var results = c.Run();
            MyAssert.NoErrorsOrWarnings(results);
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
        public void SetUndeclaredVariableRaisesWarning()
        {
            Checker c = new Checker(new Arguments("/i:set @undeclared = 1"));
            var results = c.Run();
            Assert.Equal(1, results.Warnings.Count);            
        }

        [Fact]
        public void FindDeclaredVariableNoWarning()
        {
            Checker c = new Checker(new Arguments("/i:declare @declared as int; set @declared = 1"));
            var results = c.Run();
            MyAssert.NoErrorsOrWarnings(results);
        }

        [Fact]
        public void DeclaredVariablesArePerBatch()
        {
            Checker c = new Checker(new Arguments(@"/i:
declare @declared as int; 
GO 
set @declared = 1"));
            var results = c.Run();
            Assert.Equal(1, results.Warnings.Count);
        }

        [Fact]
        public void MultipleDeclarationsWork()
        {
            Checker c = new Checker(new Arguments("/i:declare @a as int, @b as nvarchar; set @b = 'x'; set @a = 3"));
            var results = c.Run();
            MyAssert.NoErrorsOrWarnings(results);
        }

        [Fact]
        public void SelectUndeclaredVariableRaisesWarning()
        {
            Checker c = new Checker(new Arguments("/i:select X from Y where X = @foo"));
            var results = c.Run();
            Assert.Equal(1, results.Warnings.Count);
        }

        [Fact]
        public void UndeclaredGlobalVariableNoWarning()
        {
            Checker c = new Checker(new Arguments("/i:select X from Y where X = @@cpu_busy"));
            var results = c.Run();
            MyAssert.NoErrorsOrWarnings(results);
        }
    }
}
