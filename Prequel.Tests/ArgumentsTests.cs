using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.IO;
using System.Linq;
using Xunit;

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
            Assert.Throws<ProgramTerminatingException>(() => new Arguments(new string[] { }));
        }

        [Fact]
        public void NoArgumentsSetsFailedExitCode()
        {
            var ex = Assert.Throws<ProgramTerminatingException>( () => new Arguments(new string[] { } ));
            Assert.Equal(ExitReason.GeneralFailure, ex.ExitCode);
        }

        [Fact]
        public void ArgsButNoFileRaisesUsageException()
        {
            Assert.Throws<ProgramTerminatingException>(() => new Arguments("/v:2008"));
        }
        
        [Fact]
        public void ArgumentsHasUsageDescription()
        {
            Assert.NotNull(Arguments.UsageDescription);
        }

        [InlineData("/help")]
        [InlineData("-?")]
        [InlineData("/?")]
        [Theory]
        public void UsageRequestUsageExceptionWithSuccessExitCode(string flag)
        {
            var ex = Assert.Throws<ProgramTerminatingException>(() => new Arguments(flag));
            Assert.Equal(ExitReason.Success, ex.ExitCode);
        }

        [Fact]
        public void FileNameIsRecorded()
        {
            var a = new Arguments("foo.sql");
            Assert.Equal(new string[] { "foo.sql" }, a.Inputs.Select(x => x.Path));
        }

        [Fact]
        public void SqlVersionFlagIsRecorded()
        {
            var a = new Arguments("/v:2008", "foo.sql");
            Assert.Equal(typeof(TSql100Parser), a.SqlParserType);
        }
        
        [InlineData("/v:", "")]
        [InlineData("/v:x", "x")]
        [InlineData("/v:8.4", "8.4")]
        [Theory]
        public void InvalidVersionStringIsReported(string version, string versionToComplainAbout)
        {
            var ex = Assert.Throws<ProgramTerminatingException>( () => new Arguments("foo.sql", version));
            Assert.Contains(string.Format("Unknown SQL version '{0}'", versionToComplainAbout), ex.Message);
        }

        [Fact]
        public void InlineSqlReadIntoStream()
        {
            var a = new Arguments("/i:foo bar");

            TextReader reader = new StreamReader(a.Inputs[0].Stream);
            string contents = reader.ReadToEnd();
            Assert.Equal("foo bar", contents);        
        }

        [Fact]
        public void DefaultIsToDisplayLogo()
        {
            var a = new Arguments("foo.sql");
            Assert.Equal(true, a.DisplayLogo);
        }

        [Fact]
        public void NoLogoSuppressesLogo()
        {
            var a = new Arguments("foo.sql", "/nologo");
            Assert.Equal(false, a.DisplayLogo);
        }

        [Fact]
        public void DefaultWarningLevelIsSerious()
        {
            var a = new Arguments("foo.sql");
            Assert.Equal(WarningLevel.Serious, a.WarningLevel);
        }

        [Fact]
        public void WarningLevelCanBeSet()
        {
            var a = new Arguments("foo.sql", "/warn:1");
            Assert.Equal((WarningLevel)1, a.WarningLevel);
        }        

        [InlineData("-1")]
        [InlineData("5")]
        [InlineData("")]
        [InlineData("foo")]
        [Theory]
        public void CrazyWarningLevelCausesError(string weirdLevel)
        {
            var ex = Assert.Throws<ProgramTerminatingException>(() => new Arguments("foo.sql", "/warn:" + weirdLevel));
            Assert.Contains(string.Format("Invalid Warning Level '{0}'", weirdLevel), ex.Message);
        }

        [Fact]
        public void UnknownFlagRaisesError()
        {
            var ex = Assert.Throws<ProgramTerminatingException>(() => new Arguments("foo.sql", "/z"));
            Assert.Contains("Unknown flag '/z'", ex.Message);
        }
    }
}
