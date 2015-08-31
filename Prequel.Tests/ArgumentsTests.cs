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
            Assert.Throws<UsageException>(() => new Arguments(new string[] { }));
        }

        [Fact]
        public void NoArgumentsSetsExitCode()
        {
            var ex = Assert.Throws<UsageException>( () => new Arguments(new string[] { } ));
            Assert.Equal(1, ex.ExitCode);
        }

        [Fact]
        public void ArgumentsHasUsageDescription()
        {
            Assert.NotNull(Arguments.UsageDescription);
        }

        [Fact]
        public void SlashQuestionRaisesUsageExceptionWithZeroExitCode()
        {
            var ex = Assert.Throws<UsageException>(() => new Arguments("/?"));
            Assert.Equal(0, ex.ExitCode);
        }

        [Fact]
        public void MinusQuestionRaisesUsageExceptionWithZeroExitCode()
        {
            var ex = Assert.Throws<UsageException>(() => new Arguments("-?" ));
            Assert.Equal(0, ex.ExitCode);
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
            var ex = Assert.Throws<UsageException>( () => new Arguments("foo.sql", version));
            Assert.Contains(String.Format("Unknown SQL version '{0}'", versionToComplainAbout), ex.Message);
        }

        [Fact]
        public void InlineSqlReadIntoStream()
        {
            var a = new Arguments("/i:foo bar");

            TextReader reader = new StreamReader(a.Inputs[0].Stream);
            string contents = reader.ReadToEnd();
            Assert.Equal("foo bar", contents);        
        }
    }
}
