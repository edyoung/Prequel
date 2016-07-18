using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.IO;
using System.Linq;
using Xunit;
using FluentAssertions;

namespace Prequel.Tests
{
    internal static class TestData
    {
        
    }

    public class ArgumentsTests
    {
        [Fact]
        public void NoArgumentsRaisesUsageException()
        {
            Action act = () => new Arguments(new string[] { });
            act.ShouldThrow<ProgramTerminatingException>();
        }

        [Fact]
        public void NoArgumentsSetsFailedExitCode()
        {
            Action act = () => new Arguments(new string[] { });
            act.ShouldThrow<ProgramTerminatingException>().And.ExitCode.Should().Be(ExitReason.GeneralFailure);            
        }

        [Fact]
        public void ArgsButNoFileRaisesUsageException()
        {
            Action act = () => new Arguments("/v:2008");
            act.ShouldThrow<ProgramTerminatingException>();
        }
        
        [Fact]
        public void ArgumentsHasUsageDescription()
        {
            Arguments.UsageDescription.Should().NotBeNullOrWhiteSpace();
        }

        [InlineData("/help")]
        [InlineData("-?")]
        [InlineData("/?")]
        [Theory]
        public void UsageRequestUsageExceptionWithSuccessExitCode(string flag)
        {
            Action act = () => new Arguments(flag);            
            act.ShouldThrow<ProgramTerminatingException>().And.ExitCode.Should().Be(ExitReason.Success); 
        }

        [Fact]
        public void FileNameIsRecorded()
        {
            var a = new Arguments("foo.sql");
            a.Inputs.Select(x => x.Path).Should().Equal(new string[] { "foo.sql" });            
        }

        [Fact]
        public void SqlVersionFlagIsRecorded()
        {
            var a = new Arguments("/v:2008", "foo.sql");
            a.SqlParserType.Should().Be(typeof(TSql100Parser));
        }
        
        [InlineData("/v:", "")]
        [InlineData("/v:x", "x")]
        [InlineData("/v:8.4", "8.4")]
        [Theory]
        public void InvalidVersionStringIsReported(string version, string versionToComplainAbout)
        {
            Action act = () => new Arguments("foo.sql", version);
            act.ShouldThrow<ProgramTerminatingException>().WithMessage($"Unknown SQL version '{versionToComplainAbout}'*");
        }

        [Fact]
        public void InlineSqlReadIntoStream()
        {
            var a = new Arguments("/i:foo bar");

            TextReader reader = new StreamReader(a.Inputs[0].Stream);
            string contents = reader.ReadToEnd();

            contents.Should().Be("foo bar");            
        }

        [Fact]
        public void DefaultIsToDisplayLogo()
        {
            var a = new Arguments("foo.sql");
            a.DisplayLogo.Should().BeTrue();
        }

        [Fact]
        public void NoLogoSuppressesLogo()
        {
            var a = new Arguments("foo.sql", "/nologo");
            a.DisplayLogo.Should().BeFalse();
        }

        [Fact]
        public void DefaultWarningLevelIsSerious()
        {
            var a = new Arguments("foo.sql");
            a.WarningLevel.Should().Be(WarningLevel.Serious);
        }

        [Fact]
        public void WarningLevelCanBeSet()
        {
            var a = new Arguments("foo.sql", "/warn:1");
            a.WarningLevel.Should().Be((WarningLevel)1);
        }        

        [InlineData("-1")]
        [InlineData("5")]
        [InlineData("")]
        [InlineData("foo")]
        [Theory]
        public void CrazyWarningLevelCausesError(string weirdLevel)
        {
            Action act = () => new Arguments("foo.sql", "/warn:" + weirdLevel);
            act.ShouldThrow<ProgramTerminatingException>().WithMessage($"Invalid Warning Level '{weirdLevel}'*");            
        }

        [Fact]
        public void UnknownFlagRaisesError()
        {
            Action act = () => new Arguments("foo.sql", "/z");
            act.ShouldThrow<ProgramTerminatingException>().WithMessage("Unknown flag '/z'");
        }
    }
}
