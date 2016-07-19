using System;
using System.IO;
using System.Linq;
using Xunit;
using FluentAssertions;

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
            results.Should().HaveNoErrorsOrWarnings();
        }
       
        [Fact]
        public void ParseInvalidStringProducesErrors()
        {            
            var results = Check("select >>>");
            results.Errors.Should().Contain(err => err.Message.Contains("Incorrect syntax"));
            results.ExitCode.Should().Be(ExitReason.GeneralFailure);
        }

        [Fact]
        public void ParseFile()
        {
            using (var t = new TempFile("select * from foo"))
            {
                Checker c = new Checker(new Arguments(t.FileName));
                var results = c.Run();
                results.ExitCode.Should().Be(ExitReason.Success);
            }
        }

        [Fact]
        public void ParseInvalidFile()
        {
            using (var t = new TempFile("select >>>"))
            {
                Checker c = new Checker(new Arguments(t.FileName));
                var results = c.Run();
                results.Errors.Should().NotBeEmpty();
                results.ExitCode.Should().Be(ExitReason.GeneralFailure);
            }
        }

        [Fact]
        public void ParseFileWithTooRecentSyntax()
        {
            // IIF was introduced in sql 2012            
            var results = Check("select iif (1 > 0, 1, 2) from foo", "/v:2008");
            results.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public void ParseFileWithRecentSyntax()
        {
            // IIF was introduced in sql 2012
            var results = Check("select iif (1 > 0, 1, 2) from foo", "/v:2012");
            results.Errors.Should().BeEmpty();
        }

        [Fact]
        public void ParseMissingFile()
        {
            var c = new Checker(new Arguments("missing.xyz"));
            Action act = () => c.Run();
            act.ShouldThrow<ProgramTerminatingException>().WithMessage("*missing.xyz*").And.ExitCode.Equals(ExitReason.IOError);            
        }

        [Fact]
        public void InvalidFileNameRaisesError()
        {
            var c = new Checker(new Arguments("??"));
            Action act = () => c.Run();
            act.ShouldThrow<ProgramTerminatingException>();            
        }

        /// <summary>
        /// This parses in one SQL file but not when run through prequel. why?
        /// </summary>
        [Fact(Skip="Not sure why this string works in sample sql file")]
        public void ThisShouldParse()
        {
            CheckResults results = Check("grant SELECT on User to ServerRole");
            results.Should().HaveNoErrors();
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
                errorMessage.Should()
                    .ContainEquivalentOf(t.FileName, "message should contain the filename")
                    .And.Contain("(2)", "message should contain the line number") 
                    .And.Contain("ERROR", "message should contain ERROR for build tools which look for that")
                    .And.Contain(error.Number.ToString(), "message should contain SQL's error code") 
                    .And.Contain("Incorrect syntax near select", "message should contain SQL's error message"); 
            }
        }

        [Fact]
        public void WarningFormattingLooksOK()
        {
            var results = Check("\nset @undeclared = 7");
            var warning = results.Warnings[0];
            string warningMessage = results.FormatWarning(warning);
            warningMessage.Should().Be("<inline>(2): WARNING PQL0001: Variable @undeclared used before being declared");
        }

        [Fact]
        public void WarningLevelZeroNoWarnings()
        {
            var results = Check("\nset @undeclared = 7", "/warn:0");
            results.Should().HaveNoErrorsOrWarnings();
        }

        [Fact]
        public void WarningLevelZeroStillShowsErrors()
        {
            var results = Check("select >>>", "/warn:0");
            results.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public void WarningLevelCriticalHidesMinorErrors()
        {
            // assert that I haven't changed the level of these warnings (otherwise test below may not be valid)
            Warning.WarningTypes[WarningID.UndeclaredVariableUsed].Level.Should().Be(WarningLevel.Critical);
            Warning.WarningTypes[WarningID.UnusedVariableDeclared].Level.Should().Be(WarningLevel.Minor);

            var results = Check("\nset @undeclared = 7\ndeclare @unused as int", "/warn:1");
            results.Should().WarnAbout(WarningID.UndeclaredVariableUsed);
            results.Should().NotWarnAbout(WarningID.UnusedVariableDeclared);
        }
        #endregion

        #region Undeclared Variable check
        [Fact]
        public void SetUndeclaredVariableRaisesWarning()
        {            
            var results = Check("set @undeclared = 1");
            results.Should().WarnAbout(WarningID.UndeclaredVariableUsed);           
        }

        [Fact]
        public void FindDeclaredVariableNoWarning()
        {            
            var results = Check("declare @declared as int; set @declared = 1");
            results.Should().HaveNoErrorsOrWarnings();
        }

        [Fact]
        public void DeclarationsAreCaseInsensitive()
        {
            var results = Check("declare @DECLARED as int; set @declared = 1");
            results.Should().HaveNoErrorsOrWarnings();
        }

        [Fact]
        public void AliasesAreCaseInsensitive()
        {
            var results = Check("declare @DECLARED as nvarchar; select @declared = Name from sys.Columns");
            results.Should().HaveNoErrors();
            results.Should().NotWarnAbout(WarningID.UndeclaredVariableUsed);
        }

        [Fact]
        public void DeclaredVariablesArePerBatch()
        {
            var results = Check(@"
declare @declared as int; 
GO 
set @declared = 1");
            results.Should().WarnAbout(WarningID.UndeclaredVariableUsed);
        }

        [Fact]
        public void MultipleDeclarationsWork()
        {
            var results = Check("declare @a as int, @b as nvarchar; set @b = 'x'; set @a = 3");
            results.Should().HaveNoErrors();
            results.Should().NotWarnAbout(WarningID.UndeclaredVariableUsed);
        }

        [Fact]
        public void SelectUndeclaredVariableRaisesWarning()
        {
            var results = Check("select X from Y where X = @foo");
           results.Should().WarnAbout(WarningID.UndeclaredVariableUsed);
        }

        [Fact]
        public void UndeclaredGlobalVariableNoWarning()
        {
            var results = Check("select X from Y where X = @@cpu_busy");
            results.Should().HaveNoErrorsOrWarnings();
        }

        [Fact]
        public void SetParameterInSprocSucceeds()
        {
            var results = Check(@"
create procedure foo @x INT
as
    set @x = 2
go");
            results.Should().HaveNoErrorsOrWarnings();
        }

        [Fact]
        public void SetUndeclaredVariableInSprocRaisesWarning()
        {
            var results = Check(@"
create procedure foo @x INT
as
    set @y = 2
go");
            results.Should().WarnAbout(WarningID.UndeclaredVariableUsed);            
        }

        [Fact]
        public void TableDeclarationPreventsWarning()
        {
            var results = Check(@"
declare @t table(Value int)
insert @t (Value)values(1)");
            results.Should().HaveNoErrorsOrWarnings();
        }

        [Fact]
        public void ExecParameterVariablesDoNotNeedDeclaration()
        {
            var results = Check(@"
exec foo @a = 1
");
            results.Should().HaveNoErrorsOrWarnings();
        }

        [Fact]
        public void ExecParameterValuesDoNeedDeclaration()
        {
            var results = Check(@"
exec foo @b
");
            results.Should().WarnAbout(WarningID.UndeclaredVariableUsed);
        }

        [Fact]
        public void VariableDeclaredWithCustomTypeNoWarning()
        {
            var results = Check("declare @a MyType, @b MyType2; set @a = @b");
            results.Should().NotWarnAbout(WarningID.UndeclaredVariableUsed);
        }


        // Used just to have a convenient place to paste snippets to try out
        [Fact]
        public void Experiment()
        {
            var results = Check(@"
");
            results.Should().HaveNoErrorsOrWarnings();
        }
        #endregion

        #region Unused variable check
        [Fact]
        public void UnusedVariableRaisesWarning()
        {
            var results = Check("declare @foo as int", "/warn:3");
            results.Should().WarnAbout(WarningID.UnusedVariableDeclared);
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

            results.Should().WarnAbout(WarningID.ProcedureWithoutNoCount);
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
            results.Should().WarnAbout(WarningID.ProcedureWithSPPrefix);
        }

        // Is SP_ a problem too?

        #endregion
        
        #region type checking
        [Fact]
        public void DeclareStringWithoutLiteralNoWarning()
        {
            var results = Check("declare @fine as char(1)");
            results.Should().NotWarnAbout(WarningID.StringTruncated);
        }

        [Fact]
        public void DeclareStringWithLongerLiteralRaisesWarning()
        {
            var results = Check("declare @tooshort as char(1) = 'hello'");
            results.Should().WarnAbout(
                warning => 
                    warning.Number == WarningID.StringTruncated && warning.Message.Contains("Variable @tooshort has length 1 and is assigned a value with length up to 5"));            
        }

        [Fact]
        public void DeclareStringWithSameLengthLiteralNoWarning()
        {
            var results = Check("declare @fine as char(5) = 'hello'");
            results.Should().NotWarnAbout(WarningID.StringTruncated);
        }

        [Fact]
        public void DeclareStringWithImplicitLengthAndLongerLiteralRaisesWarning()
        {
            var results = Check("declare @tooshort as char = 'hello'");
            results.Should().WarnAbout(
                warning =>
                    warning.Number == WarningID.StringTruncated && warning.Message.Contains("Variable @tooshort has length 1 and is assigned a value with length up to 5"));
        }

        [Fact]
        public void DeclareVarCharStringWithImplicitLengthAndLongerLiteralRaisesWarning()
        {
            var results = Check("declare @tooshort as varchar = 'hello'");
            results.Should().WarnAbout(
                warning =>
                    warning.Number == WarningID.StringTruncated && warning.Message.Contains("Variable @tooshort has length 1 and is assigned a value with length up to 5"));
        }

        [Fact]
        public void DeclareVarCharMaxNoWarning()
        {
            var results = Check("declare @fine as varchar(max) = 'hello'");
            results.Should().NotWarnAbout(WarningID.StringTruncated);
        }

        [Fact]
        public void DeclareAndSeparatelySetWithLiteralRaisesWarning()
        {
            var results = Check("declare @tooshort as varchar; set @tooshort = 'hello'");
            results.Should().WarnAbout(
                warning =>
                    warning.Number == WarningID.StringTruncated && warning.Message.Contains("Variable @tooshort has length 1 and is assigned a value with length up to 5"));
        }

        [Fact]
        public void AssignVariablesWithLongerLengthRaisesWarning()
        {
            var results = Check(@"
declare @tooshort as varchar(10);
declare @toolong as varchar(20) = '01234567890123456789'
set @tooshort = @toolong
");

            results.Should().WarnAbout(
                warning =>
                    warning.Number == WarningID.StringTruncated && 
                    warning.Message.Contains("Variable @tooshort has length 10 and is assigned a value with length up to 20") &&
                    warning.Line == 4);
        }

        [Fact]
        public void AssignVariablesFromConvertRaisesWarning()
        {
            var results = Check(@"declare @tooshort as varchar(10) = CONVERT(varchar(20), '01234567890123456789')");

            results.Should().WarnAbout(
                warning =>
                    warning.Number == WarningID.StringTruncated &&
                    warning.Message.Contains("Variable @tooshort has length 10 and is assigned a value with length up to 20"));
        }

        [Fact]
        public void AssignVariablesFromCastRaisesWarning()
        {
            var results = Check(@"declare @tooshort as varchar(10) = CAST('01234567890123456789' as varchar(20))");

            results.Should().WarnAbout(
                warning =>
                    warning.Number == WarningID.StringTruncated &&
                    warning.Message.Contains("Variable @tooshort has length 10 and is assigned a value with length up to 20"));
        }

        #endregion

        #region String type narrowing

        [Fact] 
        public void DeclareVarCharWithNLiteralRaisesWarning()
        {
            var results = Check("declare @wrongtype as varchar(5) = N'hello'");

            results.Should().WarnAbout(
                warning =>
                    warning.Number == WarningID.StringConverted &&
                    warning.Message.Contains("Variable @wrongtype") &&
                    warning.Message.Contains("assigned a unicode value"));
            
        }

        [Fact]
        public void AssignNCharToCharRaisesWarning()
        {
            var results = Check("declare @wide as nchar; declare @narrow as char; set @narrow = @wide");
            results.Should().WarnAbout(WarningID.StringConverted);

            
        }

        [Fact]
        public void AssignConvertedNCharToCharRaisesWarning()
        {
            var results = Check("declare @narrow as char; set @narrow = convert(nchar, 'x')");
            results.Should().WarnAbout(WarningID.StringConverted);
        }

        [Fact]
        public void AssignCharToNCharIsOK()
        {
            var results = Check("declare @wide as nchar; declare @narrow as char; set @wide = @narrow");
            results.Should().NotWarnAbout(WarningID.StringConverted);
        }

        #endregion

        #region Convert to string without length spec

        [Fact]
        public void ConvertToVarCharWithoutLengthWarns()
        {
            var results = Check("DECLARE @myVariable AS varchar(50) = convert(varchar, '01234567890123456789012345678901234567890123456789');");
            results.Should().WarnAbout(WarningID.ConvertToVarCharOfUnspecifiedLength);
            
        }

        [Fact]
        public void ConvertToNVarCharWithoutLengthWarns()
        {
            var results = Check("DECLARE @myVariable AS nvarchar(50) = convert(nvarchar, '01234567890123456789012345678901234567890123456789');");
            results.Should().WarnAbout(warning =>
                warning.Number == WarningID.ConvertToVarCharOfUnspecifiedLength &&
                warning.Message.Contains("CONVERT to type NVarChar without specifying length"));
        }

        [Fact]
        public void ConvertToVarCharWithLengthNoWarning()
        {
            var results = Check("DECLARE @myVariable AS varchar(50) = convert(varchar(30), '01234567890123456789012345678901234567890123456789');");
            results.Should().NotWarnAbout(WarningID.ConvertToVarCharOfUnspecifiedLength);
        }

        [Fact]
        public void CastToVarCharWithoutLengthWarns()
        {
            var results = Check("DECLARE @myVariable AS varchar(50) = cast('01234567890123456789012345678901234567890123456789' as varchar);");
            results.Should().WarnAbout(WarningID.ConvertToVarCharOfUnspecifiedLength);
        }

        #endregion

        #region int to string conversions
        [InlineData("int")]
        [InlineData("smallint")]
        [InlineData("bigint")]
        [InlineData("tinyint")]
        [Theory]
        public void ConvertIntToSmallCharWarns(string type)
        {
            var results = Check($"DECLARE @x as char(2); declare @y as {type}; set @x = @y");
            results.Should().WarnAbout(WarningID.ConvertToTooShortString);
        }

        [Fact]
        public void ConvertIntToLongCharNoWarning()
        {
            var results = Check("DECLARE @x as char(12); declare @y as int; set @x = @y");
            results.Should().NotWarnAbout(WarningID.ConvertToTooShortString);
        }

        #endregion

        #region parenthesis

        [Fact]
        public void AssignmentThroughParenthesisStillWarns()
        {
            var results = Check($"DECLARE @x as char(2); declare @y as int; set @x = (@y)");
            results.Should().WarnAbout(WarningID.ConvertToTooShortString);
        }

        #endregion

        #region datatypes from arithmetic

        [Fact]
        public void ConvertIntFromAddToSmallCharWarns()
        {
            var results = Check("DECLARE @x as varchar(6); declare @y as smallint; declare @z as int; set @x = @y + @z");
            results.Should().WarnAbout(WarningID.ConvertToTooShortString);
        }

        [Fact]
        public void ConvertIntFromSubToSmallCharWarns()
        {
            var results = Check("DECLARE @x as varchar(6); declare @y as smallint; declare @z as int; set @x = @y - @z");
            results.Should().WarnAbout(WarningID.ConvertToTooShortString);
        }

        [Fact]
        public void ConvertSmallIntFromArithmeticToSmallCharWarns()
        {
            var results = Check("DECLARE @x as varchar(2); declare @y as smallint; declare @z as smallint; set @x = @y + @z");
            results.Should().WarnAbout(WarningID.ConvertToTooShortString);
        }

        [Fact]
        public void ConvertSmallIntFromArithmeticToLargeEnoughCharNoWarning()
        {
            var results = Check("DECLARE @x as varchar(6); declare @y as smallint; declare @z as smallint; set @x = @y + @z");
            results.Should().NotWarnAbout(WarningID.ConvertToTooShortString);
        }
       
        [Fact]
        public void ConvertIntFromMoreArithmeticToSmallCharWarns()
        {
            var results = Check("DECLARE @x as varchar(6); declare @y as int; declare @z as smallint; set @x = ((@z + @z) * (@y- @y) / @z)");
            results.Should().WarnAbout(WarningID.ConvertToTooShortString);
        }

        #endregion
    }
}
