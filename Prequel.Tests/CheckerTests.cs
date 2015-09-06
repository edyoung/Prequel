using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Prequel.Tests
{       
    public static class MyAssert
    {
        public static void NoErrorsOrWarnings(CheckResults results)
        {
            Assert.Empty(results.Errors);
            Assert.Empty(results.Warnings);
            Assert.Equal(ExitReason.Success, results.ExitCode);
        }

        public static void OneWarningOfType(WarningID id, CheckResults results)
        {
            Assert.Empty(results.Errors);
            Assert.Equal(1, results.Warnings.Count(warning => warning.Number == id));
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
            Assert.Equal(ExitReason.GeneralFailure, results.ExitCode);
        }

        [Fact]
        public void ParseFile()
        {
            using (var t = new TempFile("select * from foo"))
            {
                Checker c = new Checker(new Arguments(t.FileName));
                var results = c.Run();
                Assert.Equal(ExitReason.Success, results.ExitCode);
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
                Assert.Equal<ExitReason>(ExitReason.GeneralFailure, results.ExitCode);
            }
        }

        [Fact]
        public void ParseMissingFile()
        {
            var c = new Checker(new Arguments("missing.xyz"));
            var ex = Assert.Throws<ProgramTerminatingException>(() => c.Run());
            Assert.Contains("missing.xyz", ex.Message);
            Assert.Equal(ExitReason.IOError, ex.ExitCode);
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
            MyAssert.OneWarningOfType(WarningID.UndeclaredVariableUsed, results);           
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
        public void AliasesAreCaseInsensitive()
        {
            var results = Check("declare @DECLARED as nvarchar; select @declared = Name from sys.Columns");
            MyAssert.NoErrorsOrWarnings(results);
        }

        [Fact]
        public void DeclaredVariablesArePerBatch()
        {
            var results = Check(@"
declare @declared as int; 
GO 
set @declared = 1");
            MyAssert.OneWarningOfType(WarningID.UndeclaredVariableUsed, results);
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
            MyAssert.OneWarningOfType(WarningID.UndeclaredVariableUsed, results);
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
            MyAssert.OneWarningOfType(WarningID.UndeclaredVariableUsed, results);
        }

        [Fact]
        public void TableDeclarationPreventsWarning()
        {
            var results = Check(@"
declare @t table(Value int)
insert @t (Value)values(1)");
            MyAssert.NoErrorsOrWarnings(results);
        }

        [Fact]
        public void ExecParameterVariablesDoNotNeedDeclaration()
        {
            var results = Check(@"
exec foo @a = 1
");
            MyAssert.NoErrorsOrWarnings(results);
        }

        [Fact]
        public void ExecParameterValuesDoNeedDeclaration()
        {
            var results = Check(@"
exec foo @b
");
            MyAssert.OneWarningOfType(WarningID.UndeclaredVariableUsed, results);
        }

        // Used just to have a convenient place to paste snippets to try out
        [Fact]
        public void Experiment()
        {
            var results = Check(@"
");
            MyAssert.NoErrorsOrWarnings(results);
        }
        #endregion

        #region Unused variable check
        [Fact]
        public void UnusedVariableRaisesWarning()
        {
            var results = Check("declare @foo as int");
            MyAssert.OneWarningOfType(WarningID.UnusedVariableDeclared, results);
        }
        #endregion

    }
}
