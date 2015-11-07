using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Prequel.Tests
{          
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

        [Fact]
        public void InvalidFileNameRaisesError()
        {
            var c = new Checker(new Arguments("??"));
            var ex = Assert.Throws<ProgramTerminatingException>(() => c.Run());
        }

        /// <summary>
        /// This parses in one SQL file but not when run through prequel. why?
        /// </summary>
        [Fact(Skip="Not sure why this string works in sample sql file")]
        public void ThisShouldParse()
        {
            CheckResults results = Check("grant SELECT on User to ServerRole");
            MyAssert.NoErrors(results);
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
            Assert.Equal("<inline>(2): WARNING PQL0001: Variable @undeclared used before being declared", warningMessage);
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
            Assert.Contains("Variable @tooshort has length 1 and is assigned a value with length up to 5", w.Message);
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
            Assert.Contains("Variable @tooshort has length 1 and is assigned a value with length up to 5", w.Message);
        }

        [Fact]
        public void DeclareVarCharStringWithImplicitLengthAndLongerLiteralRaisesWarning()
        {
            var results = Check("declare @tooshort as varchar = 'hello'");
            Warning w = MyAssert.OneWarningOfType(WarningID.StringTruncated, results);
            Assert.Contains("Variable @tooshort has length 1 and is assigned a value with length up to 5", w.Message);
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
            Assert.Contains("Variable @tooshort has length 1 and is assigned a value with length up to 5", w.Message);
        }

        [Fact]
        public void AssignVariablesWithLongerLengthRaisesWarning()
        {
            var results = Check(@"
declare @tooshort as varchar(10);
declare @toolong as varchar(20) = '01234567890123456789'
set @tooshort = @toolong
");
            Warning w = MyAssert.OneWarningOfType(WarningID.StringTruncated, results);
            Assert.Contains("Variable @tooshort has length 10 and is assigned a value with length up to 20", w.Message);
            Assert.Equal(4, w.Line); // warning should come from the line with the assignment
        }

        [Fact]
        public void AssignVariablesFromConvertRaisesWarning()
        {
            var results = Check(@"declare @tooshort as varchar(10) = CONVERT(varchar(20), '01234567890123456789')");
            Warning w = MyAssert.OneWarningOfType(WarningID.StringTruncated, results);
            Assert.Contains("Variable @tooshort has length 10 and is assigned a value with length up to 20", w.Message);
        }

        [Fact]
        public void AssignVariablesFromCastRaisesWarning()
        {
            var results = Check(@"declare @tooshort as varchar(10) = CAST('01234567890123456789' as varchar(20))");
            Warning w = MyAssert.OneWarningOfType(WarningID.StringTruncated, results);
            Assert.Contains("Variable @tooshort has length 10 and is assigned a value with length up to 20", w.Message);
        }

        #endregion

        #region String type narrowing

        [Fact] 
        public void DeclareVarCharWithNLiteralRaisesWarning()
        {
            var results = Check("declare @wrongtype as varchar(5) = N'hello'");
            Warning w = MyAssert.OneWarningOfType(WarningID.StringConverted, results);
            Assert.Contains("Variable @wrongtype", w.Message);
            Assert.Contains("assigned a unicode value", w.Message);
        }

        [Fact]
        public void AssignNCharToCharRaisesWarning()
        {
            var results = Check("declare @wide as nchar; declare @narrow as char; set @narrow = @wide");
            MyAssert.OneWarningOfType(WarningID.StringConverted, results);
        }

        [Fact]
        public void AssignConvertedNCharToCharRaisesWarning()
        {
            var results = Check("declare @narrow as char; set @narrow = convert(nchar, 'x')");
            MyAssert.OneWarningOfType(WarningID.StringConverted, results);
        }

        [Fact]
        public void AssignCharToNCharIsOK()
        {
            var results = Check("declare @wide as nchar; declare @narrow as char; set @wide = @narrow");
            MyAssert.NoWarningsOfType(WarningID.StringConverted, results);
        }

        #endregion

        #region Convert to string without length spec

        [Fact]
        public void ConvertToVarCharWithoutLengthWarns()
        {
            var results = Check("DECLARE @myVariable AS varchar(50) = convert(varchar, '01234567890123456789012345678901234567890123456789');");
            Warning w = MyAssert.OneWarningOfType(WarningID.ConvertToVarCharOfUnspecifiedLength, results);
        }

        [Fact]
        public void ConvertToNVarCharWithoutLengthWarns()
        {
            var results = Check("DECLARE @myVariable AS nvarchar(50) = convert(nvarchar, '01234567890123456789012345678901234567890123456789');");
            Warning w = MyAssert.OneWarningOfType(WarningID.ConvertToVarCharOfUnspecifiedLength, results);
            Assert.Contains("CONVERT to type NVarChar without specifying length", w.Message);
        }

        [Fact]
        public void ConvertToVarCharWithLengthNoWarning()
        {
            var results = Check("DECLARE @myVariable AS varchar(50) = convert(varchar(30), '01234567890123456789012345678901234567890123456789');");
            MyAssert.NoWarningsOfType(WarningID.ConvertToVarCharOfUnspecifiedLength, results);
        }

        [Fact]
        public void CastToVarCharWithoutLengthWarns()
        {
            var results = Check("DECLARE @myVariable AS varchar(50) = cast('01234567890123456789012345678901234567890123456789' as varchar);");
            MyAssert.OneWarningOfType(WarningID.ConvertToVarCharOfUnspecifiedLength, results);
        }

        #endregion

        #region int to string conversions
        [Fact]
        public void ConvertIntToSmallCharWarns()
        {
            var results = Check("DECLARE @x as char(2); declare @y as int; set @x = @y");
            MyAssert.OneWarningOfType(WarningID.ConvertToTooShortString, results);            
        }

        [Fact]
        public void ConvertIntToLongCharNoWarning()
        {
            var results = Check("DECLARE @x as char(12); declare @y as int; set @x = @y");
            MyAssert.NoWarningsOfType(WarningID.ConvertToTooShortString, results);
        }

        [Fact]
        public void ConvertSmallintToSmallVarcharWarns()
        {
            var results = Check("DECLARE @x as varchar(2); declare @y as smallint; set @x = @y");
            MyAssert.OneWarningOfType(WarningID.ConvertToTooShortString, results);
        }

        #endregion
    }
}
