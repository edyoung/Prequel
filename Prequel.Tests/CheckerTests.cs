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
        public static CheckResults Check(string sqlToCheck)
        {
            Checker c = new Checker(new Arguments("/i:" + sqlToCheck));
            return c.Run();
        }

        #region basic functioning
        [Fact]
        public void ParseSimpleString()
        {
            var results = Check("select * from foo");
            MyAssert.NoErrorsOrWarnings(results);
        }
       
        [Fact]
        public void ParseInvalidStringProducesErrors()
        {            
            var results = Check("select >>>");
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
        public void ErrorFormattingLooksOK()
        {
            using (var t = new TempFile("\nselect >>>"))
            {
                Checker c = new Checker(new Arguments(t.FileName));
                var results = c.Run();
                var error = results.Errors[0];
                string errorMessage = results.FormatError(error);
                Assert.Contains(t.FileName, errorMessage, StringComparison.OrdinalIgnoreCase); // should contain filename
                Assert.Contains("(2)", errorMessage); // should contain line number in parens
                Assert.Contains("ERROR", errorMessage); // should contain ERROR for build tools which look for that
                Assert.Contains(error.Number.ToString(), errorMessage); // should contain SQL's error code
                Assert.Contains("Incorrect syntax near select", errorMessage); // should contain SQL's error message
            }
        }

        [Fact]
        public void WarningFormattingLooksOK()
        {
            var results = Check("\nset @undeclared = 7");
            var warning = results.Warnings[0];
            string warningMessage = results.FormatWarning(warning);
            Assert.Equal("<inline>(2) : WARNING 1 : Variable @undeclared used before being declared", warningMessage);
        }
        #endregion

        #region Undeclared Variable check
        [Fact]
        public void SetUndeclaredVariableRaisesWarning()
        {            
            var results = Check("set @undeclared = 1");
            Assert.Equal(1, results.Warnings.Count);            
        }

        [Fact]
        public void FindDeclaredVariableNoWarning()
        {            
            var results = Check("declare @declared as int; set @declared = 1");
            MyAssert.NoErrorsOrWarnings(results);
        }

        [Fact]
        public void DeclarationsAreCaseInsensitive()
        {
            var results = Check("declare @DECLARED as int; set @declared = 1");
            MyAssert.NoErrorsOrWarnings(results);
        }

        [Fact]
        public void DeclaredVariablesArePerBatch()
        {
            var results = Check(@"
declare @declared as int; 
GO 
set @declared = 1");            
            Assert.Equal(1, results.Warnings.Count);
        }

        [Fact]
        public void MultipleDeclarationsWork()
        {
            var results = Check("declare @a as int, @b as nvarchar; set @b = 'x'; set @a = 3");
            MyAssert.NoErrorsOrWarnings(results);
        }

        [Fact]
        public void SelectUndeclaredVariableRaisesWarning()
        {
            var results = Check("select X from Y where X = @foo");
            Assert.Equal(1, results.Warnings.Count);
        }

        [Fact]
        public void UndeclaredGlobalVariableNoWarning()
        {
            var results = Check("select X from Y where X = @@cpu_busy");
            MyAssert.NoErrorsOrWarnings(results);
        }

        [Fact]
        public void SetParameterInSprocSucceeds()
        {
            var results = Check(@"
create procedure foo @x INT
as
    set @x = 2
go");
            MyAssert.NoErrorsOrWarnings(results);
        }

        [Fact]
        public void SetUndeclaredVariableInSprocRaisesWarning()
        {
            var results = Check(@"
create procedure foo @x INT
as
    set @y = 2
go");
            Assert.Equal(1, results.Warnings.Count);
        }
        #endregion

    }
}
