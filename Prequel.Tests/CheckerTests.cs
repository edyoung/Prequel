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

        public static void NoErrors(CheckResults results)
        {
            Assert.Empty(results.Errors);
        }

        public static Warning OneWarningOfType(WarningID id, CheckResults results)
        {
            Assert.Empty(results.Errors);            
            Assert.Equal(1, results.Warnings.Count(warning => warning.Number == id));
            return results.Warnings.First(warning => warning.Number == id);
        }

        public static void NoWarningsOfType(WarningID id, CheckResults results)
        {
            Assert.Empty(results.Errors);
            Assert.Equal(0, results.Warnings.Count(warning => warning.Number == id));
        }
    }
    public class CheckerTests
    {        
        public static CheckResults Check(string sqlToCheck, params string[] arguments)
        {
            var allArguments = arguments.ToList();
            allArguments.Add("/i:" + sqlToCheck);
            Checker c = new Checker(new Arguments(allArguments.ToArray()));
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
        public void ParseFileWithTooRecentSyntax()
        {
            // IIF was introduced in sql 2012            
            var results = Check("select iif (1 > 0, 1, 2) from foo", "/v:2008");
            Assert.NotEmpty(results.Errors);
        }

        [Fact]
        public void ParseFileWithRecentSyntax()
        {
            // IIF was introduced in sql 2012
            var results = Check("select iif (1 > 0, 1, 2) from foo", "/v:2012");
            Assert.Empty(results.Errors);
        }

        [Fact]
        public void ParseMissingFile()
        {
            var c = new Checker(new Arguments("missing.xyz"));
            var ex = Assert.Throws<ProgramTerminatingException>(() => c.Run());
            Assert.Contains("missing.xyz", ex.Message);
            Assert.Equal(ExitReason.IOError, ex.ExitCode);
        }
        #endregion

        #region reporting
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

        [Fact]
        public void WarningLevelZeroNoWarnings()
        {
            var results = Check("\nset @undeclared = 7", "/warn:0");
            MyAssert.NoErrorsOrWarnings(results);
        }

        [Fact]
        public void WarningLevelZeroStillShowsErrors()
        {
            var results = Check("select >>>", "/warn:0");
            Assert.NotEmpty(results.Errors);
        }

        [Fact]
        public void WarningLevelCriticalHidesMinorErrors()
        {
            // assert that I haven't changed the level of these warnings
            Assert.Equal(WarningLevel.Critical, Warning.WarningTypes[WarningID.UndeclaredVariableUsed].Level);
            Assert.Equal(WarningLevel.Minor, Warning.WarningTypes[WarningID.UnusedVariableDeclared].Level);

            var results = Check("\nset @undeclared = 7\ndeclare @unused as int", "/warn:1");
            MyAssert.OneWarningOfType(WarningID.UndeclaredVariableUsed, results);
            MyAssert.NoWarningsOfType(WarningID.UnusedVariableDeclared, results);
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
            MyAssert.NoErrors(results);
            MyAssert.NoWarningsOfType(WarningID.UndeclaredVariableUsed, results);
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
            MyAssert.NoErrors(results);
            MyAssert.NoWarningsOfType(WarningID.UndeclaredVariableUsed, results);
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
            var results = Check("declare @foo as int", "/warn:3");
            MyAssert.OneWarningOfType(WarningID.UnusedVariableDeclared, results);
        }
        #endregion

        #region nocount check

        // complain if a procedure does not set nocount on
        [Fact]
        public void SprocWithoutNoCountOnRaisesWarning()
        {
            var results = Check(@"
                create procedure foo as
return 1
go
create procedure bar as
set nocount on
return 1
go
                ", "/warn:3");

            MyAssert.OneWarningOfType(WarningID.ProcedureWithoutNoCount, results);
        }
        #endregion

        #region sp_ check
        [Fact]
        public void SprocWithSpRaisesWarning()
        {
            var results = Check(@"
create procedure sp_foo as 
return 1
go");
            MyAssert.OneWarningOfType(WarningID.ProcedureWithSPPrefix, results);
        }

        // Is SP_ a problem too?

        #endregion

        #region implicit length char
        [InlineData("char")]
        [InlineData("varchar")]
        [InlineData("nchar")]
        [InlineData("nvarchar")]
        [Theory]       
        public void DeclareCharVariableWithNoLengthRaisesWarning(string type)
        {
            var results = Check(string.Format(@"declare @explicit_length as {0}(1); declare @implicit_length as {0}", type));
            Warning w = MyAssert.OneWarningOfType(WarningID.CharVariableWithImplicitLength, results);
            Assert.Contains("@implicit_length", w.Message);            
        }

        [InlineData("char")]
        [InlineData("varchar")]
        [InlineData("nchar")]
        [InlineData("nvarchar")]
        [Theory]
        public void DeclareCharParameterWithNoLengthRaisesWarning(string type)
        {
            var results = Check(string.Format("create procedure myproc(@myparam as {0}) as return @myparam", type));
            Warning w = MyAssert.OneWarningOfType(WarningID.CharVariableWithImplicitLength, results);
            Assert.Contains("@myparam", w.Message);
        }
        #endregion

        #region type checking
        [Fact]
        public void DeclareStringWithoutLiteralNoWarning()
        {
            var results = Check("declare @fine as char(1)");
            MyAssert.NoWarningsOfType(WarningID.StringTruncated, results);
        }

        [Fact]
        public void DeclareStringWithLongerLiteralRaisesWarning()
        {
            var results = Check("declare @tooshort as char(1) = 'hello'");
            Warning w = MyAssert.OneWarningOfType(WarningID.StringTruncated, results);
            Assert.Contains("Variable @tooshort has length 1 and is assigned a value with length 5", w.Message);
        }

        [Fact]
        public void DeclareStringWithSameLengthLiteralNoWarning()
        {
            var results = Check("declare @fine as char(5) = 'hello'");
            MyAssert.NoWarningsOfType(WarningID.StringTruncated, results);
        }

        [Fact]
        public void DeclareStringWithImplicitLengthAndLongerLiteralRaisesWarning()
        {
            var results = Check("declare @tooshort as char = 'hello'");
            Warning w = MyAssert.OneWarningOfType(WarningID.StringTruncated, results);
            Assert.Contains("Variable @tooshort has length 1 and is assigned a value with length 5", w.Message);
        }

        [Fact]
        public void DeclareVarCharStringWithImplicitLengthAndLongerLiteralRaisesWarning()
        {
            var results = Check("declare @tooshort as varchar = 'hello'");
            Warning w = MyAssert.OneWarningOfType(WarningID.StringTruncated, results);
            Assert.Contains("Variable @tooshort has length 1 and is assigned a value with length 5", w.Message);
        }

        [Fact]
        public void DeclareVarCharMaxNoWarning()
        {
            var results = Check("declare @fine as varchar(max) = 'hello'");
            MyAssert.NoWarningsOfType(WarningID.StringTruncated, results);
        }

        [Fact]
        public void DeclareAndSeparatelySetWithLiteralRaisesWarning()
        {
            var results = Check("declare @tooshort as varchar; set @tooshort = 'hello'");
            Warning w = MyAssert.OneWarningOfType(WarningID.StringTruncated, results);
            Assert.Contains("Variable @tooshort has length 1 and is assigned a value with length 5", w.Message);
        }

        
        #endregion
    }
}
